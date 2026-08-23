// applyCommand の入口テスト。ルールの詳細は combat.test.ts / reactions.test.ts を参照。
import { describe, expect, it } from 'vitest'
import { applyCommand, createInitialState } from './state.ts'

describe('applyCommand', () => {
  it('StartCombat で戦闘が始まりイベントが記録される', () => {
    const s0 = createInitialState(42, 'set-auto')
    const s1 = applyCommand(s0, { type: 'StartCombat', seed: 42, enemyId: 'enemy_brute' })
    expect(s1.turn).toBe(1)
    const eventTypes = s1.eventLog.map((e) => e.type)
    expect(eventTypes[0]).toBe('CombatStarted')
    expect(eventTypes).toContain('TurnStarted')
    expect(eventTypes).toContain('EnemyIntentDeclared')
  })

  it('確定ルール: 戦闘開始時にランプ(energyMax)と成長カウンターはリセット状態', () => {
    const s = applyCommand(createInitialState(1, 'hold-manual'), {
      type: 'StartCombat',
      seed: 1,
      enemyId: 'enemy_probe',
    })
    expect(s.player.energyMax).toBe(3)
    expect(s.player.growth).toBe(0)
  })

  it('未知の敵IDは拒否される', () => {
    const s0 = createInitialState(1, 'set-auto')
    expect(() => applyCommand(s0, { type: 'StartCombat', seed: 1, enemyId: 'no_such' })).toThrow(
      /未定義の敵/,
    )
  })

  it('方式が受け付けないコマンドは canHandle で拒否される', () => {
    const s = applyCommand(createInitialState(7, 'set-auto'), {
      type: 'StartCombat',
      seed: 7,
      enemyId: 'enemy_brute',
    })
    // set-auto に割り込み確認はない
    expect(() => applyCommand(s, { type: 'ConfirmReaction', fire: true })).toThrow(/受け付けない/)
    expect(() => applyCommand(s, { type: 'ReactManual', cardUid: 'x' })).toThrow(/受け付けない/)
  })
})
