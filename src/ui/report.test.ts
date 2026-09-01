// レポート生成の頑健性 (2026-08-30)。
// 「書き出しがうまくいかない」の真犯人が「?マス発の戦闘が enemyId='unknown' でアーカイブされ、
// encounterName('unknown') が例外死する」だったための回帰テスト。
// レポートはプレイテストのデータ回収の道具なので、どんな状態でも絶対に落ちないことを固定する。
import { describe, expect, it } from 'vitest'
import { archiveBattle, buildCardProposals, buildReport } from './report.ts'
import { createRun } from '../engine/run.ts'
import { freshCombat } from '../engine/test-helpers.ts'

describe('レポートは絶対に落ちない', () => {
  it('未知の敵ID (旧バックアップの unknown 等) を含む履歴でも buildReport が throw しない', () => {
    const run = createRun(7, 'set-confirm')
    const combat = freshCombat('set-confirm', 'enemy_probe', 42)
    const legacy = archiveBattle({ ...combat, phase: 'won' }, 1, 'unknown', false, 80, 10)
    const text = buildReport(run, null, [legacy])
    expect(text).toContain('unknown') // 落ちずに生のIDで出力される
  })

  it('通常の履歴では敵名が解決される', () => {
    const run = createRun(7, 'set-confirm')
    const combat = freshCombat('set-confirm', 'enemy_probe', 42)
    const a = archiveBattle({ ...combat, phase: 'won' }, 1, 'enemy_probe', false, 80, 10)
    const text = buildReport(run, null, [a])
    expect(text).toContain('探り屋')
  })
})

describe('プレイメモの同梱 (2026-09-01)', () => {
  it('メモがある時だけ「## プレイメモ」節が出て、文脈と本文が並ぶ', () => {
    const run = createRun(7, 'set-confirm')
    const notes = [
      { at: '2026-09-01T12:34:56.000Z', context: '幕1 行3 combat T2 2勝 HP70', text: '樽の負傷が地味に痛い' },
    ]
    const text = buildReport(run, null, [], '', notes)
    expect(text).toContain('## プレイメモ（1件')
    expect(text).toContain('[12:34 幕1 行3 combat T2 2勝 HP70] 樽の負傷が地味に痛い')
    // メモ無しなら節ごと出ない
    expect(buildReport(run, null, [])).not.toContain('## プレイメモ')
  })
})

describe('カード調整案の提案書 (2026-09-01 カード調整サイクル)', () => {
  it('変更案・削除案・新カード案が現行スペック付きで並ぶ', () => {
    const text = buildCardProposals(
      {
        green_strike: { change: '6→7ダメにしたい' },
        green_fang: { remove: true },
        blue_strike: {}, // マーク無し = 無視される
      },
      '緑1E: 5ダメ+成長1の入口コモン',
    )
    expect(text).toContain('## 変更案（1件）')
    expect(text).toContain('`green_strike`')
    expect(text).toContain('提案: 6→7ダメにしたい')
    expect(text).toContain('## 削除案（1件）')
    expect(text).toContain('`green_fang`')
    expect(text).toContain('緑1E: 5ダメ+成長1の入口コモン')
    expect(text).not.toContain('blue_strike')
    // 現行スペックが機械可読で入る (レビューの根拠)
    expect(text).toContain('"dealDamage"')
  })

  it('現行データに存在しないID (統合で消えた札の下書き) でも落ちない', () => {
    const text = buildCardProposals({ black_agony_strike: { change: 'コスト下げたい' } }, '')
    expect(text).toContain('現行データに存在しない')
  })
})
