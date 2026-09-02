// scripts/gen-csharp-types.ts — src/engine/types.ts (+ run.ts の主要型) から C# のデータ型を生成する。
// docs/unity-port.md §5「types.ts = C# クラス定義の一次資料」を機械化: 手変換の漏れと型のドリフトを止める。
// 使い方: npx tsx scripts/gen-csharp-types.ts  → unity/Packages/com.deckrogue.engine/Runtime/Generated/Types.g.cs
// 方針 (v1):
//   - interface → public sealed record (init-only プロパティ)。JSON のキー名は camelCase のまま [JsonPropertyName]
//   - 文字列リテラルの union 型 → string + 定数クラス (enum 化は移植側で判断)
//   - 判別共用体 (Command / GameEvent / RunCommand: `type` で分岐) → 抽象 record + メンバーごとの sealed record
//   - number → int (名前に ratio/scale/multiplier/premium を含むものは double)。関数型は生成しない
import { mkdirSync, writeFileSync } from 'node:fs'
import ts from 'typescript'

const SOURCES = ['src/engine/types.ts', 'src/engine/run.ts', 'src/engine/map.ts']
const OUT = 'unity/Packages/com.deckrogue.engine/Runtime/Generated/Types.g.cs'
const program = ts.createProgram(SOURCES, { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext, strict: true })
const checker = program.getTypeChecker()
void checker

const pascal = (s: string): string => s.replace(/^[a-z]/, (c) => c.toUpperCase()).replace(/[-_]([a-z])/g, (_, c: string) => c.toUpperCase())
const constName = (lit: string): string => {
  const p = lit.replace(/[^A-Za-z0-9]+/g, ' ').trim().split(' ').map(pascal).join('')
  return /^[0-9]/.test(p) ? `_${p}` : p || 'Empty'
}
const isDouble = (name: string): boolean => /ratio|scale|multiplier|premium|atkScale|hpScale/i.test(name)

/** 文字列リテラル union の別名 (CardType など) → 生成する定数クラス */
const literalUnions = new Map<string, string[]>()
/** 判別共用体 (type フィールドで分岐) */
const taggedUnions = new Map<string, ts.TypeLiteralNode[]>()
/** 生成したレコード名 (重複防止) */
const emitted = new Set<string>()
const out: string[] = []

function literalsOf(node: ts.TypeNode): string[] | null {
  if (ts.isUnionTypeNode(node)) {
    const lits: string[] = []
    for (const t of node.types) {
      if (ts.isLiteralTypeNode(t) && ts.isStringLiteral(t.literal)) lits.push(t.literal.text)
      else if (t.kind === ts.SyntaxKind.UndefinedKeyword) continue
      else return null
    }
    return lits
  }
  if (ts.isLiteralTypeNode(node) && ts.isStringLiteral(node.literal)) return [node.literal.text]
  return null
}

/** TS 型ノード → C# 型名 (nullable は呼び出し側で付ける) */
function csType(node: ts.TypeNode | undefined, propName: string, owner: string): string {
  if (!node) return 'object'
  if (ts.isParenthesizedTypeNode(node)) return csType(node.type, propName, owner)
  switch (node.kind) {
    case ts.SyntaxKind.StringKeyword: return 'string'
    case ts.SyntaxKind.NumberKeyword: return isDouble(propName) ? 'double' : 'int'
    case ts.SyntaxKind.BooleanKeyword: return 'bool'
    case ts.SyntaxKind.UnknownKeyword:
    case ts.SyntaxKind.AnyKeyword: return 'object'
    case ts.SyntaxKind.NullKeyword: return 'object'
    default: break
  }
  if (ts.isArrayTypeNode(node)) return `IReadOnlyList<${csType(node.elementType, propName, owner)}>`
  if (ts.isTypeOperatorNode(node)) return csType(node.type, propName, owner) // readonly T[]
  if (ts.isTupleTypeNode(node)) return `IReadOnlyList<${csType(node.elements[0], propName, owner)}>`
  if (ts.isLiteralTypeNode(node)) {
    if (ts.isStringLiteral(node.literal)) return 'string'
    if (ts.isNumericLiteral(node.literal)) return 'int'
    if (node.literal.kind === ts.SyntaxKind.TrueKeyword || node.literal.kind === ts.SyntaxKind.FalseKeyword) return 'bool'
    return 'object'
  }
  if (ts.isUnionTypeNode(node)) {
    const lits = literalsOf(node)
    if (lits) return 'string'
    const isNullish = (t: ts.TypeNode) =>
      t.kind === ts.SyntaxKind.UndefinedKeyword || t.kind === ts.SyntaxKind.NullKeyword || (ts.isLiteralTypeNode(t) && t.literal.kind === ts.SyntaxKind.NullKeyword)
    const nonNull = node.types.filter((t) => !isNullish(t))
    if (nonNull.length === 1) return csType(nonNull[0], propName, owner)
    // 判別共用体はインラインでは扱わない → object
    return 'object /* union */'
  }
  if (ts.isTypeLiteralNode(node)) {
    const name = `${owner}${pascal(propName)}`
    emitRecord(name, node.members, `${owner}.${propName} のインライン型`)
    return name
  }
  if (ts.isTypeReferenceNode(node)) {
    const name = node.typeName.getText()
    const args = node.typeArguments ?? []
    if (name === 'ReadonlyArray' || name === 'Array') return `IReadOnlyList<${csType(args[0], propName, owner)}>`
    if (name === 'Readonly' || name === 'Partial') return csType(args[0], propName, owner)
    if (name === 'Record') return `IReadOnlyDictionary<${csType(args[0], propName, owner)}, ${csType(args[1], propName, owner)}>`
    if (name === 'Set') return `IReadOnlyList<${csType(args[0], propName, owner)}>`
    if (literalUnions.has(name)) return 'string'
    return name // 他の interface / alias
  }
  if (ts.isFunctionTypeNode(node)) return 'object /* function */'
  if (ts.isIndexedAccessTypeNode(node) || ts.isTypeQueryNode(node) || ts.isConditionalTypeNode(node) || ts.isMappedTypeNode(node)) return 'object'
  return 'object'
}

function emitRecord(name: string, members: ts.NodeArray<ts.TypeElement>, doc: string, extra?: { discriminator?: string; base?: string }): void {
  if (emitted.has(name)) return
  emitted.add(name)
  const lines: string[] = []
  lines.push(`    /// <summary>${doc.replace(/\n/g, ' ')}</summary>`)
  lines.push(`    public sealed record ${name}${extra?.base ? ` : ${extra.base}` : ''}`)
  lines.push('    {')
  if (extra?.discriminator !== undefined) {
    lines.push(`        public const string TypeTag = "${extra.discriminator}";`)
  }
  for (const m of members) {
    if (!ts.isPropertySignature(m) || !m.name) continue
    const prop = m.name.getText().replace(/^['"]|['"]$/g, '')
    if (extra?.discriminator !== undefined && prop === 'type') continue
    const optional = m.questionToken !== undefined
    const t = csType(m.type, prop, name)
    const valueType = t === 'int' || t === 'double' || t === 'bool'
    const nullable =
      optional ||
      (m.type !== undefined &&
        ts.isUnionTypeNode(m.type) &&
        m.type.types.some((x) => x.kind === ts.SyntaxKind.NullKeyword || x.kind === ts.SyntaxKind.UndefinedKeyword || (ts.isLiteralTypeNode(x) && x.literal.kind === ts.SyntaxKind.NullKeyword)))
    const jsdoc = ts.getJSDocCommentsAndTags(m).map((d) => (ts.isJSDoc(d) ? (typeof d.comment === 'string' ? d.comment : '') : '')).filter(Boolean).join(' ')
    if (jsdoc) lines.push(`        /// <summary>${jsdoc.replace(/\n/g, ' ').replace(/</g, '&lt;').replace(/>/g, '&gt;')}</summary>`)
    lines.push(`        [JsonProperty("${prop}")]`)
    lines.push(`        public ${t}${nullable ? '?' : ''} ${pascal(prop)} { get; init; }${!nullable && !valueType ? ' = default!;' : ''}`)
  }
  lines.push('    }')
  lines.push('')
  out.push(lines.join('\n'))
}

// ---- 1st pass: 文字列リテラル union と判別共用体を集める ----
for (const file of SOURCES) {
  const sf = program.getSourceFile(file)
  if (!sf) continue
  sf.forEachChild((node) => {
    if (!ts.isTypeAliasDeclaration(node)) return
    const lits = literalsOf(node.type)
    if (lits) {
      literalUnions.set(node.name.text, lits)
      return
    }
    if (ts.isUnionTypeNode(node.type) && node.type.types.every((t) => ts.isTypeLiteralNode(t) || ts.isParenthesizedTypeNode(t))) {
      const members = node.type.types.map((t) => (ts.isParenthesizedTypeNode(t) ? t.type : t)).filter(ts.isTypeLiteralNode)
      if (members.every((m) => m.members.some((x) => ts.isPropertySignature(x) && x.name.getText() === 'type'))) taggedUnions.set(node.name.text, members)
    }
  })
}

// ---- 2nd pass: 出力 ----
out.push('    // ==== 文字列リテラル union (TS の型別名) → 定数クラス。プロパティ側は string ====')
for (const [name, lits] of literalUnions) {
  out.push(`    public static class ${name}s\n    {\n${lits.map((l) => `        public const string ${constName(l)} = "${l}";`).join('\n')}\n    }\n`)
}
for (const file of SOURCES) {
  const sf = program.getSourceFile(file)
  if (!sf) continue
  out.push(`    // ==== ${file} ====`)
  sf.forEachChild((node) => {
    if (ts.isInterfaceDeclaration(node)) {
      const doc = ts.getJSDocCommentsAndTags(node).map((d) => (ts.isJSDoc(d) && typeof d.comment === 'string' ? d.comment : '')).filter(Boolean).join(' ') || node.name.text
      emitRecord(node.name.text, node.members, doc)
    } else if (ts.isTypeAliasDeclaration(node) && taggedUnions.has(node.name.text)) {
      const base = node.name.text
      out.push(`    /// <summary>判別共用体 ${base} (TS: type フィールドで分岐)。移植側は Type を見て派生 record へ分岐する</summary>\n    public abstract record ${base}\n    {\n        [JsonProperty("type")]\n        public string Type { get; init; } = default!;\n    }\n`)
      for (const m of taggedUnions.get(base)!) {
        const typeProp = m.members.find((x) => ts.isPropertySignature(x) && x.name.getText() === 'type') as ts.PropertySignature
        const tag = typeProp.type && ts.isLiteralTypeNode(typeProp.type) && ts.isStringLiteral(typeProp.type.literal) ? typeProp.type.literal.text : 'unknown'
        emitRecord(`${base}_${constName(tag)}`, m.members, `${base}: type="${tag}"`, { discriminator: tag, base })
      }
    } else if (ts.isTypeAliasDeclaration(node) && ts.isTypeLiteralNode(node.type)) {
      emitRecord(node.name.text, node.type.members, node.name.text)
    }
  })
}

const header = `// <auto-generated>
// このファイルは scripts/gen-csharp-types.ts が src/engine/types.ts・run.ts・map.ts から生成した。手で編集しない。
// 再生成: npm run gen:csharp  (TS 側の型が一次資料 = docs/unity-port.md §5)
// 生成規則: interface→sealed record (init-only・JSON名は camelCase・Newtonsoft Json.NET 属性)、文字列リテラル union→string+定数クラス、
//           判別共用体 (type で分岐)→抽象 record+派生 record、number→int (ratio/scale 等は double)、関数型は object。
//           GameState などの「状態」型は移植側で不変 record として使う (with 式で遷移)。
// </auto-generated>
#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated
{
`
mkdirSync('unity/Packages/com.deckrogue.engine/Runtime/Generated', { recursive: true })
writeFileSync(OUT, header + out.join('\n') + '}\n')
console.log(`${OUT}: records ${emitted.size}, literal unions ${literalUnions.size}, tagged unions ${taggedUnions.size}`)
