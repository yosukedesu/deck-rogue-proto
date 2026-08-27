// かすみ (ディミア・伏せ同時2枚) のテスト。第2波 (2026-08-25) で15人ロースター完成。
// 確定済みルール表「伏せ枚数」「リアクション回数」を固定する。
import { describe, expect, it } from 'vitest'
import { getLeaderDef } from './content.ts'
import { createRun } from './run.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'

describe('かすみ (ディミア): 伏せ同時2枚', () => {
  it('2枚まで伏せられ、3枚目は拒否される', () => {
    const run = createRun(7, 'set-confirm', 'leader_dimir')
    expect(getLeaderDef('leader_dimir').setSlots).toBe(2)
    let s = withHand(run.combat!, ['blue_frost_veil', 'black_reaction_curse', 'blue_mana_leak'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_frost_veil' })
    s = applyCommand(s, { type: 'SetCard', cardUid: 't1_black_reaction_curse' })
    expect(s.player.setCards).toHaveLength(2)
    // 3枚目は canHandle 層で拒否される
    expect(() => applyCommand(s, { type: 'SetCard', cardUid: 't2_blue_mana_leak' })).toThrow()
  })

  it('窓に合致する伏せが2枚あれば cardUid で選んで発動し、残りは伏せたまま', () => {
    const run = createRun(7, 'set-confirm', 'leader_dimir')
    let s = withHand(run.combat!, ['blue_frost_veil', 'black_reaction_curse'])
    // 霜の帳 (被攻撃前) と呪詛返し (被攻撃後) を両方伏せる
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_frost_veil' })
    s = applyCommand(s, { type: 'SetCard', cardUid: 't1_black_reaction_curse' })
    s = withIntent(s, attackIntent(6))
    s = applyCommand(s, { type: 'EndTurn' })
    // pre窓: 霜の帳だけが合致
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true, cardUid: 't0_blue_frost_veil' })
    expect(s.player.iceBlock).toBeGreaterThan(0)
    // 発動済みフラグにより同一行動のpost窓 (呪詛返し) は開かない = 1行動1回
    expect(s.player.setCards.map((c) => c.uid)).toEqual(['t1_black_reaction_curse'])
  })

  it('pre窓で温存すれば、同じ行動のpost窓でもう1枚が発動できる', () => {
    const run = createRun(7, 'set-confirm', 'leader_dimir')
    let s = withHand(run.combat!, ['blue_frost_veil', 'black_reaction_curse'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_frost_veil' })
    s = applyCommand(s, { type: 'SetCard', cardUid: 't1_black_reaction_curse' })
    s = withIntent(s, attackIntent(6))
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction') // pre窓 (霜の帳)
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false }) // 温存
    expect(s.phase).toBe('awaiting-reaction') // post窓 (呪詛返し)
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true, cardUid: 't1_black_reaction_curse' })
    expect(s.enemies[0].hp).toBe(enemyHp - 5) // ドレイン5
    expect(s.player.setCards.map((c) => c.uid)).toEqual(['t0_blue_frost_veil']) // 帳は温存継続
  })

  it('単色リーダーは従来通り1枠のまま', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_frost_veil',
      'blue_mana_leak',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_frost_veil' })
    expect(() => applyCommand(s, { type: 'SetCard', cardUid: 't1_blue_mana_leak' })).toThrow()
  })

  it('伏せ破壊は「1枚だけ逃がして」残り全部を破壊する (2枠のリスクと救出の選択)', () => {
    // 2026-08-27 伏せ破壊への応答: 窓は開くが、1行動1回制限により逃がせるのは1枚だけ。
    // かすみの2枠は「どちらを救うか」の選択になる
    const run = createRun(7, 'set-confirm', 'leader_dimir')
    let s = withHand(run.combat!, ['blue_frost_veil', 'black_reaction_curse'])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_frost_veil' })
    s = applyCommand(s, { type: 'SetCard', cardUid: 't1_black_reaction_curse' })
    s = withIntent(s, { kind: 'destroy-set', shownMin: 0, shownMax: 0, actual: 0 })
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    // 霜の帳を発動して逃がす → 黒の呪いは残されて破壊される
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true, cardUid: 't0_blue_frost_veil' })
    expect(s.player.setCards).toHaveLength(0)
    expect(s.player.discardPile.some((c) => c.def.id === 'blue_frost_veil')).toBe(true)
    const destroyed = s.eventLog.filter((e) => e.type === 'SetCardDestroyed')
    expect(destroyed).toHaveLength(1) // 破壊されたのは逃がさなかった1枚だけ
  })
})
