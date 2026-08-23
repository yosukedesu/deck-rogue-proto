// 「確定済みルール」表の項目はテストで固定する (CLAUDE.md「テスト」)。
// ルール実装が進むごとにここへテストを追加していく。
import { describe, expect, it } from 'vitest'
import { applyCommand, createInitialState } from './state.ts'

describe('applyCommand', () => {
  it('StartCombat で戦闘が始まりイベントが記録される', () => {
    const s0 = createInitialState(42, 'set-auto')
    const s1 = applyCommand(s0, { type: 'StartCombat', seed: 42, enemyIds: [] })
    expect(s1.turn).toBe(1)
    expect(s1.eventLog.map((e) => e.type)).toEqual(['CombatStarted', 'TurnStarted'])
  })

  it('applyCommand は元の状態を変更しない (イミュータブル)', () => {
    const s0 = createInitialState(42, 'set-auto')
    applyCommand(s0, { type: 'StartCombat', seed: 42, enemyIds: [] })
    expect(s0.turn).toBe(0)
    expect(s0.eventLog).toHaveLength(0)
  })

  it('確定ルール: 戦闘開始時にランプ(energyMax)と成長カウンターはリセット状態', () => {
    const s = applyCommand(createInitialState(1, 'hold-manual'), {
      type: 'StartCombat',
      seed: 1,
      enemyIds: [],
    })
    expect(s.player.energyMax).toBe(3)
    expect(s.player.growth).toBe(0)
  })
})
