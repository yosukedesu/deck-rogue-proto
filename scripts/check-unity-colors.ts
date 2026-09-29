// scripts/check-unity-colors.ts — Unity の UI に色の生の値が戻っていないかの検査 (2026-09-29 戦闘画面の見直し p26)
//
//   npx tsx scripts/check-unity-colors.ts        # 見つかれば一覧を出して exit 1
//   (npm test でも src/sim/unity-colors.test.ts が同じ検査を回す)
//
// 規約: docs/color-theme.md「実装」1〜2＝UI の色は PaperFx が唯一の出典。同じ役割の色が別の値で直書きされていると、
// 次に色を調整した時に一部だけ取り残される (例: 藤の紙が #eddbf7 と #e9def3 に、真鍮の墨が #7a4e12 と #634410 に分かれていた)。
//
// 落とすもの (unity/Assets/Game の *.cs。下の「絵の色の置き場」は除く):
//   A. 16進の直書き   … UiKit.Hex("#…")・<color=#…> (TMP のタグ)・"#rrggbb" の文字列。リッチテキストは UiKit.ColorTag(PaperFx.X, s) で作る
//   B. 役割の色の写し … new Color(r,g,b[,a])／new Color32(…) の数値が PaperFx の色とほぼ同じ (各成分の差が 6/255 以下)。
//                      PaperFx.X をそのまま使うか、透明度だけ変えるなら new Color(PaperFx.X.r, PaperFx.X.g, PaperFx.X.b, a)
//   C. 引退した色      … 数値が引退した役割の色 (RETIRED。旧・蜂蜜 #e0b25a など) とほぼ同じ (2026-09-30 F57: 規則B は今の表だけを見るので、
//                      小数で書いた旧・金の墨 #7a4e12 (BrassInk との差 23) や旧・蜂蜜の選択の輪 (0.88,0.7,0.35) が素通りした)
// 落とさないもの: 白・黒・灰の掛け算や透明度 (new Color(1,1,1,0.6) など＝役割の色ではない)、演出の光の色 (役割の色と離れている値)。
//
// 除くファイル (color-theme「実装」2 で生の値が残ってよいと決めた所):
//   PaperFx.cs＝出典そのもの／Theme.cs＝アイコンの字形と生成の絵／Stage*.cs＝舞台の材質と光／MapScreen.cs・MapDoodle.cs＝地図のノードと落書きのペン／
//   RunScreens.cs・CombatScreen.cs＝使われていない旧画面
import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join, relative, dirname, basename } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'

export const EXCLUDED_FILES = new Set(['PaperFx.cs', 'Theme.cs', 'MapScreen.cs', 'MapDoodle.cs', 'RunScreens.cs', 'CombatScreen.cs'])
const isExcluded = (name: string) => EXCLUDED_FILES.has(name) || /^Stage.*\.cs$/.test(name)
/** 役割の色の写しとみなす差 (各成分。0〜255) */
const NEAR = 6

export type ColorFinding = { file: string; line: number; rule: 'A' | 'B' | 'C'; text: string; note: string }

/** 引退した役割の色 (docs/color-theme.md「実装」10 と PaperFx の注記に記録した値)。行き先は今の名前 */
export const RETIRED: readonly { hex: string; name: string; to: string }[] = [
  { hex: 'e0b25a', name: '旧・蜂蜜', to: 'PaperFx.Brass' },
  { hex: '7a4e12', name: '旧・金の墨', to: 'PaperFx.BrassInk' },
  { hex: '574b48', name: '旧・中墨', to: 'PaperFx.InkSoft' },
  { hex: 'eddbf7', name: '旧・藤の紙', to: 'PaperFx.PlumLight' },
  { hex: '141228', name: '旧・窓の暗幕', to: 'PaperFx.ModalScrim' },
]
const retiredRgb = RETIRED.map((r) => ({ ...r, rgb: [0, 2, 4].map((i) => parseInt(r.hex.slice(i, i + 2), 16)) as [number, number, number] }))

function listCs(dir: string): string[] {
  const out: string[] = []
  for (const name of readdirSync(dir)) {
    const p = join(dir, name)
    if (statSync(p).isDirectory()) out.push(...listCs(p))
    else if (name.endsWith('.cs')) out.push(p)
  }
  return out.sort()
}

/** 行から // と /* … *\/ のコメントを除く (文字列の中の // は残す)。block は複数行のコメントの中にいるか */
function stripComments(line: string, state: { block: boolean }): string {
  let out = ''
  let inStr = false
  let verbatim = false
  for (let i = 0; i < line.length; i++) {
    const c = line[i]
    const n = line[i + 1]
    if (state.block) {
      if (c === '*' && n === '/') { state.block = false; i++ }
      continue
    }
    if (inStr) {
      out += c
      if (verbatim) {
        if (c === '"' && n === '"') { out += n; i++ } else if (c === '"') inStr = false
      } else if (c === '\\') { out += n ?? ''; i++ } else if (c === '"') inStr = false
      continue
    }
    if (c === '/' && n === '/') break
    if (c === '/' && n === '*') { state.block = true; i++; continue }
    if (c === '"') { inStr = true; verbatim = line[i - 1] === '@'; out += c; continue }
    if (c === "'") {   // 文字リテラル ('"' など) を文字列の始まりと取り違えない
      const end = line.indexOf("'", i + (line[i + 1] === '\\' ? 3 : 2))
      if (end > i) { out += line.slice(i, end + 1); i = end; continue }
    }
    out += c
  }
  return out
}

/** PaperFx.cs の H("#rrggbb") から役割の色の表を読む */
export function readPalette(paperFxSource: string): Map<string, [number, number, number]> {
  const pal = new Map<string, [number, number, number]>()
  for (const m of paperFxSource.matchAll(/public static readonly Color (\w+) = H\("#([0-9a-fA-F]{6})"\)/g)) {
    const h = m[2]
    pal.set(m[1], [parseInt(h.slice(0, 2), 16), parseInt(h.slice(2, 4), 16), parseInt(h.slice(4, 6), 16)])
  }
  return pal
}

/** 数値だけの引数 ("0.93f"・"20f / 255f"・"1") を値にする。数値でなければ null */
function numArg(s: string): number | null {
  const m = s.trim().match(/^(\d+(?:\.\d+)?|\.\d+)f?(?:\s*\/\s*(\d+(?:\.\d+)?)f?)?$/)
  if (!m) return null
  const v = parseFloat(m[1])
  return m[2] ? v / parseFloat(m[2]) : v
}

/** 行の中の new Color(…)／new Color32(…) の引数 (かっこの入れ子を数える) */
function colorCalls(code: string): { kind: 'Color' | 'Color32'; args: string[]; text: string }[] {
  const out: { kind: 'Color' | 'Color32'; args: string[]; text: string }[] = []
  const re = /new\s+(Color32|Color)\s*\(/g
  let m: RegExpExecArray | null
  while ((m = re.exec(code))) {
    let depth = 1
    let i = re.lastIndex
    const start = i
    for (; i < code.length && depth > 0; i++) {
      if (code[i] === '(') depth++
      else if (code[i] === ')') depth--
    }
    if (depth !== 0) continue   // 行をまたぐ呼び出しは見ない (この UI のコードには無い)
    const inner = code.slice(start, i - 1)
    const args: string[] = []
    let d = 0, cur = ''
    for (const ch of inner) {
      if (ch === '(') d++
      if (ch === ')') d--
      if (ch === ',' && d === 0) { args.push(cur); cur = '' } else cur += ch
    }
    args.push(cur)
    out.push({ kind: m[1] as 'Color' | 'Color32', args, text: code.slice(m.index, i) })
  }
  return out
}

export function scanUnityColors(repoRoot: string): ColorFinding[] {
  const gameDir = join(repoRoot, 'unity/Assets/Game')
  const palette = readPalette(readFileSync(join(gameDir, 'PaperFx.cs'), 'utf8'))
  const findings: ColorFinding[] = []
  for (const path of listCs(gameDir)) {
    const name = basename(path)
    if (isExcluded(name)) continue
    findings.push(...scanSource(relative(repoRoot, path), readFileSync(path, 'utf8'), palette))
  }
  return findings
}

/** 1ファイルぶんの中身を検査する (合成した入力のテストにも使う) */
export function scanSource(rel: string, source: string, palette: Map<string, [number, number, number]>): ColorFinding[] {
  const findings: ColorFinding[] = []
  {
    const state = { block: false }
    source.split(/\r?\n/).forEach((raw, idx) => {
      const code = stripComments(raw, state)
      if (!code.trim()) return
      // A. 16進の直書き
      const hexHits = [
        ...code.matchAll(/"#[0-9a-fA-F]{6}(?:[0-9a-fA-F]{2})?"/g),   // UiKit.Hex("#…")・文字列に置いた16進
        ...code.matchAll(/<color=#[0-9a-fA-F]{3,8}/g),                // TMP の色タグ
      ]
      for (const h of hexHits) findings.push({ file: rel, line: idx + 1, rule: 'A', text: h[0], note: '16進の直書き → PaperFx の名前 (リッチテキストは UiKit.ColorTag)' })
      // B. 役割の色の写し
      for (const call of colorCalls(code)) {
        if (call.args.length < 3) continue
        const vals = call.args.slice(0, 3).map(numArg)
        if (vals.some((v) => v === null)) continue
        const rgb = (vals as number[]).map((v) => Math.round(call.kind === 'Color32' ? v : v * 255))
        let best: string | null = null, bestD = 999
        for (const [role, c] of palette) {
          const d = Math.max(Math.abs(c[0] - rgb[0]), Math.abs(c[1] - rgb[1]), Math.abs(c[2] - rgb[2]))
          if (d < bestD) { bestD = d; best = role }
        }
        if (best && bestD <= NEAR) { findings.push({ file: rel, line: idx + 1, rule: 'B', text: call.text, note: `PaperFx.${best} の写し (差 ${bestD}/255)` }); continue }
        // C. 引退した色 (今の表には近くない値でも、引退した値のままなら落とす)
        for (const r of retiredRgb) {
          const d = Math.max(Math.abs(r.rgb[0] - rgb[0]), Math.abs(r.rgb[1] - rgb[1]), Math.abs(r.rgb[2] - rgb[2]))
          if (d <= NEAR) { findings.push({ file: rel, line: idx + 1, rule: 'C', text: call.text, note: `引退した色 ${r.name} #${r.hex} → ${r.to} (差 ${d}/255)` }); break }
        }
      }
    })
  }
  return findings
}

// 直接実行した時だけ一覧を出す (テストから import した時は何もしない)
if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const root = join(dirname(fileURLToPath(import.meta.url)), '..')
  const found = scanUnityColors(root)
  if (found.length === 0) {
    console.log('unity-colors: 生の色は見つからなかった (PaperFx が唯一の出典)')
  } else {
    for (const f of found) console.log(`${f.file}:${f.line}  [${f.rule}] ${f.text}  … ${f.note}`)
    console.log(`\nunity-colors: ${found.length} 件。docs/color-theme.md の役割の名前 (PaperFx) に置き換えること`)
    process.exit(1)
  }
}
