import { describe, expect, it } from 'vitest'
import { actSummaries, battleMetrics } from './analysis.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'

describe('戦闘計測 (battleMetrics)', () => {
  it('ターンごとの与ダメ・被ダメ・プレイ数・伏せ/発動を数え、初手火力と最大ターン火力を出す', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 5), ['green_strike', 'green_strike', 'green_reaction_thorns'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_strike' })
    s = applyCommand(s, { type: 'SetCard', cardUid: 't2_green_reaction_thorns' })
    s = withIntent(s, attackIntent(7))
    s = applyCommand(s, { type: 'EndTurn' })
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    const m = battleMetrics(s.eventLog)
    expect(m.t1Damage).toBe(12)
    expect(m.perTurn[0]).toMatchObject({ turn: 1, dealt: 12, counter: 10, taken: 7, plays: 2, sets: 1, fires: 1, holds: 0 })
    expect(m.totalTaken).toBe(7)
    expect(m.turns).toBeGreaterThanOrEqual(2)
  })
})

describe('幕ごとの集計 (actSummaries)', () => {
  it('通常戦の平均ターン・初手火力の帯・2ターン目以降に何か起きた割合', () => {
    const mk = (n: number, turns: number, t1: number, takenLater: number) => ({
      battleNo: n, act: 1, enemyId: 'e', elite: false, boss: false, result: 'won' as const, hpBefore: 80, hpAfter: 80 - takenLater,
      metrics: { turns, t1Damage: t1, totalDealt: 0, totalTaken: takenLater, maxTurnDamage: t1, sets: 0, fires: 0, holds: 0,
        perTurn: Array.from({ length: turns }, (_, i) => ({ turn: i + 1, dealt: i === 0 ? t1 : 0, counter: 0, taken: i === 1 ? takenLater : 0, plays: 0, sets: 0, fires: 0, holds: 0 })) },
      rating: { fun: 3 },
    })
    const rows = [mk(1, 2, 20, 0), mk(2, 4, 12, 9), mk(3, 3, 30, 0)]
    const [a] = actSummaries(rows)
    expect(a.normalTurnsAvg).toBe(3)
    expect(a.t1Damage).toEqual({ min: 12, median: 20, max: 30 })
    expect(a.lateActionRate).toBe(0.33)
    expect(a.funAvg).toBe(3)
  })
})
