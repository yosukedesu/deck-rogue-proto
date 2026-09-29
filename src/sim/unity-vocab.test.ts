// unity-vocab.test.ts — Unity (絵本の肌) の画面の文に、プロトの語「伏せ」と固有名でない「罠」が混ざらないことを機械固定する (2026-09-29 戦闘画面のレビュー p15)。
// 裁定: Unity と data の文面は「からくり」の語彙 (置き場＝からくり・札＝仕込み札・動作＝発動／温存)、プロト (Web・CLI) だけ「伏せ」(2026-09-12)。
// 発端: 確認の窓の注記「罠は次の窓まで残る」・浮き文字「罠が鳴る準備完了」、用語解説 KeywordHelp.g.cs のキーが「伏せる／伏せ場／伏せ破壊」のままで
// Unity の文からは仕込みの用語が一度も当たらなかった。用語解説は scripts/gen-keyword-help.ts が写して生成する (TS 側の文が変わると生成が止まる)。
import { readdirSync, readFileSync } from 'node:fs'
import { describe, expect, it } from 'vitest'

const DIR = new URL('../../unity/Assets/Game/', import.meta.url)
/** 固有名 (カード・敵の名前) の「罠」は許す */
const PROPER_NAMES = ['弾け実の罠', '罠師の茂み', '囁きの罠', '罠壊し', '罠使い']

/** C# のソースから文字列リテラルだけを抜く (行コメント・ブロックコメント・文字リテラルは飛ばす。$"…" と @"…" も読む) */
function csStringLiterals(src: string): string[] {
  const out: string[] = []
  let i = 0
  const n = src.length
  while (i < n) {
    const c = src[i]
    const d = src[i + 1]
    if (c === '/' && d === '/') {
      while (i < n && src[i] !== '\n') i++
      continue
    }
    if (c === '/' && d === '*') {
      const end = src.indexOf('*/', i + 2)
      i = end < 0 ? n : end + 2
      continue
    }
    if (c === "'") {
      // 文字リテラル: '"' や '\'' を文字列の始まりと読まない
      i += src[i + 1] === '\\' ? 3 : 2
      while (i < n && src[i] !== "'") i++
      i++
      continue
    }
    const verbatim = (c === '@' && d === '"') || (c === '$' && d === '@' && src[i + 2] === '"') || (c === '@' && d === '$' && src[i + 2] === '"')
    if (verbatim) {
      i = src.indexOf('"', i) + 1
      let s = ''
      while (i < n) {
        if (src[i] === '"' && src[i + 1] === '"') { s += '"'; i += 2; continue }
        if (src[i] === '"') { i++; break }
        s += src[i++]
      }
      out.push(s)
      continue
    }
    if (c === '"' || (c === '$' && d === '"')) {
      i = src.indexOf('"', i) + 1
      let s = ''
      while (i < n) {
        if (src[i] === '\\') { s += src[i] + src[i + 1]; i += 2; continue }
        if (src[i] === '"' || src[i] === '\n') { i++; break }
        s += src[i++]
      }
      out.push(s)
      continue
    }
    i++
  }
  return out
}

const files = readdirSync(DIR).filter((f) => f.endsWith('.cs')).sort()

describe('Unity の画面の文は「からくり」の語彙 (p15)', () => {
  it('文字列リテラルの抜き出しがコメントを飛ばす', () => {
    const lits = csStringLiterals('// "伏せ" \n/* "罠" */ var a = "からくり"; var b = \'"\'; var c = @"x""y"; var d = $"n={n}";')
    expect(lits).toEqual(['からくり', 'x"y', 'n={n}'])
  })

  it('unity/Assets/Game/*.cs の文字列に「伏せ」が無く、「罠」は固有名にしか無い', () => {
    expect(files.length).toBeGreaterThan(20)
    expect(files).toContain('KeywordHelp.g.cs')
    const bad: string[] = []
    for (const f of files) {
      for (const lit of csStringLiterals(readFileSync(new URL(f, DIR), 'utf8'))) {
        let rest = lit
        for (const name of PROPER_NAMES) rest = rest.split(name).join('')
        if (rest.includes('伏せ') || rest.includes('罠')) bad.push(`${f}: "${lit.length > 60 ? lit.slice(0, 60) + '…' : lit}"`)
      }
    }
    expect(bad).toEqual([])
  })

  it('Unity の用語解説に仕込みの項目がある (KeywordHelp.g.cs は scripts/gen-keyword-help.ts で生成)', () => {
    const src = readFileSync(new URL('KeywordHelp.g.cs', DIR), 'utf8')
    const keys = [...src.matchAll(/^\s*\{ "([^"]+)", "/gm)].map((m) => m[1])
    for (const k of ['仕込む', '仕込み札', 'からくり', 'からくり壊し']) expect(keys, `キー「${k}」`).toContain(k)
    for (const k of ['伏せる', '伏せ場', '伏せ破壊']) expect(keys).not.toContain(k)
  })
})
