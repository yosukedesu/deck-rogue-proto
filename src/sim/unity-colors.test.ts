// unity-colors.test.ts — Unity の UI に色の生の値が戻っていないかを npm test でも見る (2026-09-29 戦闘画面の見直し p26)。
// 規約は docs/color-theme.md「実装」1〜2 (PaperFx が唯一の出典)。検査の中身と除くファイルは scripts/check-unity-colors.ts。
// 落ちたら: 一覧の値を PaperFx の役割の名前に置き換える (リッチテキストは UiKit.ColorTag(PaperFx.X, s))。
// 演出の光で役割の色と偶然近い値は ThemeFx に名前を置く (ThemeFx.SlashCore と同じ)。
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { readPalette, scanSource, scanUnityColors } from '../../scripts/check-unity-colors.ts'

const repoRoot = fileURLToPath(new URL('../../', import.meta.url))

describe('Unity の UI の色は PaperFx の名前から (docs/color-theme.md 実装1〜2)', () => {
  it('16進の直書き・役割の色の数値の写しが無い', () => {
    const found = scanUnityColors(repoRoot).map((f) => `${f.file}:${f.line} [${f.rule}] ${f.text} … ${f.note}`)
    expect(found).toEqual([])
  })

  it('引退した色は小数で書いても落とす (2026-09-30 F57)。灰の掛け算・白の透明度・名前つきの透明度は素通し', () => {
    const palette = readPalette(readFileSync(new URL('../../unity/Assets/Game/PaperFx.cs', import.meta.url), 'utf8'))
    const src = [
      'var a = new Color(0.478f, 0.306f, 0.071f);',            // 旧・金の墨 #7a4e12
      'var b = new Color(0.88f, 0.7f, 0.35f, 0.85f);',          // 旧・蜂蜜 #e0b25a
      'var c = new Color32(87, 75, 72, 255);',                  // 旧・中墨 #574b48
      'var d = new Color(0.8f, 0.8f, 0.8f, 1f);',               // 灰の掛け算
      'var e = new Color(1f, 1f, 1f, 0.6f);',                   // 白の透明度
      'var f = new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.6f);',
      '// new Color(0.88f, 0.7f, 0.35f) はコメントの中',
    ].join('\n')
    const found = scanSource('Fake.cs', src, palette)
    expect(found.map((f) => [f.line, f.rule])).toEqual([[1, 'C'], [2, 'C'], [3, 'C']])
  })
})
