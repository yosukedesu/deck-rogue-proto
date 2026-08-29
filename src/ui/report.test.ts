// レポート生成の頑健性 (2026-08-30)。
// 「書き出しがうまくいかない」の真犯人が「?マス発の戦闘が enemyId='unknown' でアーカイブされ、
// encounterName('unknown') が例外死する」だったための回帰テスト。
// レポートはプレイテストのデータ回収の道具なので、どんな状態でも絶対に落ちないことを固定する。
import { describe, expect, it } from 'vitest'
import { archiveBattle, buildReport } from './report.ts'
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
