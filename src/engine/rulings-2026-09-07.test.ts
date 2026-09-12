// 2026-09-07 ピック監査 (docs/pick-review-2026-09-06.md) のユーザー裁定を機械固定する。
// ①勢い: 放出(消費)を「値を参照してバフ・勢いは失わない」に統一し、簡単に勢いをつける札を足す
// ②獲物=本家Feed型 (最大HP+3がランに残る) ③上限参照のしきい値化 ④伏せ札群 ⑤床の数値
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef } from './content.ts'
import { reactionMatches } from './effects.ts'
import { applyRunCommand } from './run.ts'
import { applyCommand } from './state.ts'
import { createRunInBattle, defendIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

const green = allCards.filter((c) => c.color === 'green')

describe('①勢い: 値を参照してバフ (非消費) と生成の床', () => {
  it('疾風の蔓 (風の棘の作り直し): 攻撃をプレイするたび勢い+1。風の棘は撤去', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_perm_gale_vine', 'green_strike', 'green_strike'])
    s = { ...s, player: { ...s.player, energy: 5 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_perm_gale_vine' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_strike', targetIndex: 0 })
    expect(s.player.momentum).toBe(1)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_green_strike', targetIndex: 0 })
    expect(s.player.momentum).toBe(2)
    expect(() => getCardDef('green_perm_wind_thorn')).toThrow()
  })
  it('風駆け (新・C1E): 勢い+4と1ドロー', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_wind_dash'])
    s = { ...s, player: { ...s.player, drawPile: [{ uid: 'd0', def: getCardDef('green_strike') }] } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_wind_dash' })
    expect(s.player.momentum).toBe(4)
    expect(s.player.hand.map((c) => c.uid)).toContain('d0')
  })
  it('風切りの一撃 (新・C1E): 4貫通・勢いが×2で乗る (勢い3 → 4+3+3=10)。勢いは失わない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_wind_cutter'])
    s = { ...s, player: { ...s.player, momentum: 3 }, enemies: s.enemies.map((e) => ({ ...e, block: 20 })) }
    const hp0 = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_wind_cutter', targetIndex: 0 })
    expect(hp0 - s.enemies[0].hp).toBe(10)
    expect(s.player.momentum).toBe(3)
  })
  it('踏み荒らし: 勢い+5してから18貫通 (=23)。角の一突き・嵐の角・風の壁・根付く勢いに放出効果は残っていない', () => {
    const t = getCardDef('green_sig_trample')
    expect(t.effects[0]).toEqual({ trigger: 'onPlay', effect: 'addMomentum', amount: 5 })
    expect(t.effects[1].amount).toBe(18)
    for (const id of ['green_horn_thrust', 'green_horn_storm', 'green_wind_wall', 'green_rooting_rush', 'green_horn_volley']) {
      expect(getCardDef(id).effects.some((e) => e.effect.startsWith('dischargeMomentum')), id).toBe(false)
    }
    expect(getCardDef('green_horn_volley').cost).toBe(1)
    expect(getCardDef('green_horn_volley').rarity).toBe('common')
  })
})

describe('②獲物=本家Feed型: 最大HPがランに残る', () => {
  it('戦闘中に得た最大HP+3は勝利後の run.maxHp に同期される', () => {
    let run = createRunInBattle(4242, 'set-confirm')
    const max0 = run.maxHp
    const c = run.combat!
    let surgical: GameState = { ...c, enemies: c.enemies.map((e, i) => ({ ...e, hp: i === 0 ? 1 : 0, block: 0, splitInto: undefined })) }
    surgical = withIntent(withHand(surgical, ['green_prey_strike']), defendIntent(0))
    surgical = { ...surgical, player: { ...surgical.player, energy: 3 } }
    run = applyRunCommand({ ...run, combat: surgical }, { type: 'Combat', command: { type: 'PlayCard', cardUid: 't0_green_prey_strike', targetIndex: 0 } })
    if (run.phase === 'combat') {
      // 分裂・残機で戦闘が続いた場合は全滅させて決着 (最大HPの同期だけを見る)
      const c2 = run.combat!
      let s2: GameState = { ...c2, enemies: c2.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
      s2 = withIntent(withHand(s2, ['green_sweep']), defendIntent(0))
      s2 = { ...s2, player: { ...s2.player, energy: 9 } }
      run = applyRunCommand({ ...run, combat: s2 }, { type: 'Combat', command: { type: 'PlayCard', cardUid: 't0_green_sweep' } })
    }
    expect(run.phase).not.toBe('combat')
    expect(run.maxHp).toBe(max0 + 3)
  })
})

describe('③上限参照のしきい値化 (若幹の一撃・大地の唸り)', () => {
  it('若幹の一撃: 6ダメ、ターン開始時の上限が5以上ならさらに6', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_sapling_strike'])
    s = { ...s, player: { ...s.player, energyMax: 5, energyMaxAtTurnStart: 4 } } // 今ターンにランプした分は数えない
    const hp0 = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_sapling_strike', targetIndex: 0 })
    expect(hp0 - s.enemies[0].hp).toBe(6)
    let t = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_sapling_strike'])
    t = { ...t, player: { ...t.player, energyMax: 5, energyMaxAtTurnStart: 5 } }
    const hp1 = t.enemies[0].hp
    t = applyCommand(t, { type: 'PlayCard', cardUid: 't0_green_sapling_strike', targetIndex: 0 })
    expect(hp1 - t.enemies[0].hp).toBe(12)
  })
})

describe('④伏せ札群', () => {
  it('共鳴する茨: 敵の強化・応援だけを打ち消せる (攻撃の窓では発動候補にならない)', () => {
    const s = freshCombat('set-confirm', 'enemy_probe', 42)
    const card = { uid: 'r0', def: getCardDef('green_reaction_resonance') }
    expect(reactionMatches(s, card, { stage: 'pre', kind: 'buff', actual: 0 })).toBe(true)
    expect(reactionMatches(s, card, { stage: 'pre', kind: 'rally', actual: 0 })).toBe(true)
    expect(reactionMatches(s, card, { stage: 'pre', kind: 'attack', actual: 10 })).toBe(false)
    expect(reactionMatches(s, card, { stage: 'pre', kind: 'defend', actual: 8 })).toBe(false)
    expect(card.def.effects.some((e) => e.effect === 'negate')).toBe(true)
    expect(card.def.effects.find((e) => e.effect === 'addGrowth')?.amount).toBe(3)
  })
  it('弾け実の罠=返し10／破壊時全体14、先制の蔦槍=被攻撃前16貫通', () => {
    const pod = getCardDef('green_reaction_powder_pod')
    expect(pod.effects.find((e) => e.trigger === 'onAttacked')?.amount).toBe(10)
    expect(pod.effects.find((e) => e.trigger === 'onSetDestroyed')).toMatchObject({ effect: 'dealDamage', amount: 14, target: 'all' })
    expect(getCardDef('green_reaction_preempt').effects[0]).toMatchObject({ trigger: 'onAttackIncoming', effect: 'dealDamage', amount: 16, pierce: true })
  })
})

describe('⑤床の数値と研ぎ澄まし', () => {
  it('双牙4×2(急所で3発目4)・急所突き8・落ち葉の刃9・増える蔦6', () => {
    expect(getCardDef('green_twin_fang_vine').effects.map((e) => e.amount)).toEqual([4, 4, 4])
    expect(getCardDef('green_weak_point').effects[0].amount).toBe(8)
    expect(getCardDef('green_leaf_blade').effects[0].amount).toBe(9)
    expect(getCardDef('green_spreading_vine').effects[0].amount).toBe(6)
  })
  it('研ぎ澄まし: ブロック5＋手札の全てをこの戦闘中鍛える (自身・レア・工房産は除く。選択不要)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_honing', 'green_strike', 'green_guard', 'green_perm_growth_tree'])
    s = { ...s, player: { ...s.player, energy: 3 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_honing' })
    expect(s.player.block).toBe(5)
    const names = s.player.hand.map((c) => c.def.name)
    expect(names).toContain('打撃+')
    expect(names).toContain('防御+')
    expect(names).toContain('年輪の大樹') // レアは鍛えない
  })
  it('プール: 緑95種・報酬対象の 連なる角/風駆け/風切りの一撃 はコモン', () => {
    expect(green).toHaveLength(95) // 2026-09-12 合成の触媒+4 (軽石の盾・谺の種・根付きの盾・胞子の風)
    for (const id of ['green_horn_volley', 'green_wind_dash', 'green_wind_cutter', 'green_perm_gale_vine']) expect(getCardDef(id).rarity, id).toBe('common')
  })
})
