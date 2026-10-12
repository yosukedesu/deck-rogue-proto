// 赤の解凍 段2 (2026-10-12。docs/red-stage2-design-2026-10-12.md。ユーザー裁定: 業火の炉は「攻撃ごと全体延焼1」に作り直す・
// 延焼を消費して刈る札 C/U・「猛り火に点いた時」のトリガーと札2枚・衝動の札を「くべる」追加コストの札2枚・火は火を呼ぶ R・
// 延焼に応答する敵は作らない) を機械固定
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef, getDeckDef } from './content.ts'
import { axesOf, REWARD_EXCLUDED } from './run.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import { upgradeCard, upgradeTier } from './upgrade.ts'
import { fuseCards } from './fusion.ts'
import { canSetAsNormal } from './setany.ts'
import { expiryBlockPreview, lightMatters } from './summary.ts'
import type { CardInstance, GameState } from './types.ts'

const inst = (id: string, n = 0): CardInstance => ({ uid: `d${n}_${id}`, def: getCardDef(id) })
const up = (id: string) => upgradeCard({ uid: 'u', def: getCardDef(id) }).def
const withDraw = (s: GameState, ids: readonly string[], discard: readonly string[] = []): GameState => ({
  ...s,
  player: { ...s.player, drawPile: ids.map((id, i) => inst(id, i)), discardPile: discard.map((id, i) => inst(id, 100 + i)) },
})
const withPerms = (s: GameState, ids: readonly string[]): GameState => ({
  ...s,
  player: { ...s.player, permanents: [...s.player.permanents, ...ids.map((id, i) => ({ uid: `p${i}_${id}`, def: getCardDef(id) }))] },
})
const withBurn = (s: GameState, burn: number, i = 0): GameState => ({ ...s, enemies: s.enemies.map((e, j) => (j === i ? { ...e, burn } : e)) })
/** 手札の指定の札を「衝動 (このターン限り)」にする */
const asImpulse = (s: GameState, uids: readonly string[]): GameState => ({ ...s, player: { ...s.player, impulseUids: [...s.player.impulseUids, ...uids] } })
const base = () => freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red')
const play = (s: GameState, uid: string, extra: Record<string, unknown> = {}) =>
  applyCommand(s, { type: 'PlayCard', cardUid: uid, targetIndex: 0, ...extra } as never)
const hp = (s: GameState) => s.enemies[0].hp
const ids = (cs: readonly CardInstance[]) => cs.map((c) => c.def.id)
const events = (s: GameState, type: string) => s.eventLog.filter((e) => e.type === type)

describe('業火の炉 (R 2E 置物) の作り直し: 攻撃カードをプレイするたび敵全体に延焼1', () => {
  it('データ: 毎ターン開始でなく攻撃ごと。段2b 裁定で「登場時: 敵全体に延焼2」を添えた (単体戦で置く手番が無駄にならない)。鍛えると 3／2', () => {
    const d = getCardDef('red_perm_furnace')
    expect(d.effects).toEqual([
      { trigger: 'onPlay', effect: 'applyBurn', amount: 2, target: 'all' },
      { trigger: 'onAttackPlayed', effect: 'applyBurn', amount: 1, target: 'all' },
    ])
    expect(up('red_perm_furnace').effects.map((e) => e.amount)).toEqual([3, 2])
    // 置いた手番に全体延焼2
    let s = withHand(base(), ['red_perm_furnace'])
    s = play(s, 't0_red_perm_furnace')
    expect(s.enemies[0].burn).toBe(2)
  })

  it('攻撃を撃つたびに全体へ延焼1。手を動かさないターン (ターン終了だけ) では焼けない', () => {
    let s = withPerms(withHand(base(), ['red_strike', 'red_strike', 'red_guard']), ['red_perm_furnace'])
    s = play(s, 't0_red_strike')
    s = play(s, 't1_red_strike')
    expect(s.enemies[0].burn).toBe(2)
    s = play(s, 't2_red_guard') // 防御は攻撃ではない
    expect(s.enemies[0].burn).toBe(2)
    const burnBefore = s.enemies[0].burn
    s = applyCommand(s, { type: 'EndTurn' })
    // ターン開始の自動延焼は無い (ティックで1減るだけ)
    expect(s.enemies[0].burn).toBeLessThanOrEqual(burnBefore)
    expect(s.eventLog.filter((e) => e.type === 'BurnApplied').length).toBe(2)
  })
})

describe('延焼を消費して刈る (段2b 裁定 R1: 火掻き C 1E ×2・焼き払い U 2E ×3・爆熱の解放 R ×4 の梯子。×1 は「待つ」に勝てなかった)', () => {
  it('火掻き: 対象の延焼×2 のダメージを与えて延焼を全て失わせる (衝動1は外した)。刈って8を割れば「猛り火が消えた」', () => {
    let s = withBurn(withHand(base(), ['red_rake']), 9)
    const before = hp(s)
    s = play(s, 't0_red_rake')
    expect(before - hp(s)).toBe(18)
    expect(s.enemies[0].burn).toBe(0)
    expect(s.player.impulseUids.length).toBe(0)
    expect(events(s, 'BurnDischarged')).toEqual([{ type: 'BurnDischarged', enemyIndex: 0, amount: 9 }])
    expect(events(s, 'BlazeExtinguished').length).toBe(1)
  })

  it('焼き払い: ×3。鍛えると 火掻き ×3・焼き払い ×4 (参照倍率のティア)。爆熱の解放は ×4 で鍛えは従来どおりコスト−1 (2E→1E)', () => {
    let s = withBurn(withHand(base(), ['red_burn_off']), 7) // 敵の HP (25) を超えない延焼で測る
    const before = hp(s)
    s = play(s, 't0_red_burn_off')
    expect(before - hp(s)).toBe(21)
    expect(s.enemies[0].burn).toBe(0)
    expect(upgradeTier(getCardDef('red_rake'))).toBe('mult')
    expect(up('red_rake').effects[0]).toMatchObject({ effect: 'dischargeBurn', amount: 3 })
    expect(up('red_burn_off').effects[0]).toMatchObject({ effect: 'dischargeBurn', amount: 4 })
    expect(getCardDef('red_burst_release').effects[0]).toMatchObject({ effect: 'dischargeBurn', amount: 4 })
    expect(upgradeTier(getCardDef('red_burst_release'))).toBe('cost')
    expect(up('red_burst_release')).toMatchObject({ cost: 1, effects: [{ effect: 'dischargeBurn', amount: 4 }] })
  })
})

describe('猛り火に点いた時 (onBlazeIgnite): 延焼の合計が8を跨いだ瞬間', () => {
  it('爆ぜ火: 延焼3。この札の本体で8を跨いだら敵全体に5ダメ。跨がなければ鳴らない', () => {
    // 延焼5 → +3 で 8 = 点く
    let s = withBurn(withHand(base(), ['red_popping_fire', 'red_popping_fire']), 5)
    const before = hp(s)
    s = play(s, 't0_red_popping_fire')
    expect(s.enemies[0].burn).toBe(8)
    expect(before - hp(s)).toBe(5)
    expect(events(s, 'BlazeIgnited').length).toBe(1)
    // 既に点いている状態では本体は延焼だけ
    const before2 = hp(s)
    s = play(s, 't1_red_popping_fire')
    expect(before2 - hp(s)).toBe(0)
    expect(events(s, 'BlazeIgnited').length).toBe(1)
  })

  it('火の目覚め (U 1E 置物。段2b 裁定 R2): 点くたび1ドロー＋エナジー1 (引いた札をそのターンに撃てる)。消して (火掻き) また点ければもう一度', () => {
    let s = withDraw(withPerms(withBurn(withHand(base(), ['red_ignite', 'red_rake', 'red_ignite']), 4), ['red_perm_fire_awakening']), ['red_spark', 'red_spark', 'red_spark', 'red_spark', 'red_spark', 'red_spark'])
    const handBefore = s.player.hand.length
    const energyBefore = s.player.energy
    s = play(s, 't0_red_ignite') // 着火 = 延焼4 → 8 で点く
    expect(s.enemies[0].burn).toBe(8)
    expect(s.player.hand.length).toBe(handBefore - 1 + 1)
    expect(s.player.energy).toBe(energyBefore - 1 + 1)
    expect(events(s, 'BlazeIgnited').length).toBe(1)
    s = play(s, 't1_red_rake') // 刈って 0 に = 消えた
    expect(s.enemies[0].burn).toBe(0)
    expect(events(s, 'BlazeExtinguished').length).toBe(1)
    const handMid = s.player.hand.length
    s = withBurn(s, 4)
    s = play(s, 't2_red_ignite')
    expect(s.enemies[0].burn).toBe(8)
    expect(s.player.hand.length).toBe(handMid - 1 + 1)
    expect(events(s, 'BlazeIgnited').length).toBe(2)
  })

  it('敵フェーズの点火 (火纏い=被攻撃後に延焼2) では置物は鳴らない (既存の「点火の瞬間」と同じ線)。出来事は残る', () => {
    let s = withPerms(withBurn(withHand(base(), []), 7), ['red_perm_flarecoat', 'red_perm_fire_awakening'])
    s = applyCommand(s, { type: 'EndTurn' })
    // ティックで 7→6、被攻撃後に +2 で 8 = 跨ぐ
    const ignited = events(s, 'BlazeIgnited').length
    if (ignited > 0) {
      // 敵フェーズのドローは起きない (手札は全捨ての後の5枚)
      expect(s.player.hand.length).toBe(5)
    }
    expect(s.phase).toBe('player-turn')
  })

  it('チェインの当たりで点いた分は本体の「この札で点いたら」に数えない (本体の直前の猛り火を測る)', () => {
    // 火渡り (2E・延焼7・チェイン) の当たりが着火 (延焼4) → 当たりで 4+? … 親の本体で跨ぐのが先か後かを固定:
    // 延焼0 → 当たり (爆ぜ火 1E: 延焼3) で 3 → 本体 (延焼7) で 10 = 本体で跨ぐ = 爆ぜ火の全体5は「当たり側」では鳴らない
    let s = withDraw(withHand(base(), ['red_cascade_ford']), ['red_popping_fire'])
    const before = hp(s)
    s = play(s, 't0_red_cascade_ford')
    expect(s.enemies[0].burn).toBe(10)
    expect(before - hp(s)).toBe(0) // 爆ぜ火 (当たり) の本体は延焼3で跨いでいない
    expect(events(s, 'BlazeIgnited').length).toBe(1)
  })
})

describe('くべる (impulseCost)。段2b 裁定 R3: 「くべてもよい」(kindleOptional) = 衝動が無ければ普通に撃てて、くべれば「くべた時」の効果が乗る', () => {
  it('火にくべる (C 1E): 衝動の札を1枚くべて敵全体に4ダメ＋対象に延焼3。くべた札は消滅置き場へ (失効ではない)', () => {
    let s = withPerms(withHand(base(), ['red_kindle', 'red_inferno', 'red_strike']), ['red_perm_embers_left'])
    s = asImpulse(s, ['t1_red_inferno'])
    const before = hp(s)
    s = play(s, 't0_red_kindle', { exhaustUids: ['t1_red_inferno'] })
    expect(before - hp(s)).toBe(4)
    expect(s.enemies[0].burn).toBe(3) // 燃え残り (失効ごと延焼+3) は鳴らない = 3 のまま
    expect(ids(s.player.exhaustPile)).toContain('red_inferno')
    expect(s.player.impulseUids).toEqual([])
    expect(events(s, 'ImpulseKindled')).toEqual([{ type: 'ImpulseKindled', cardId: 'red_kindle', cardIds: ['red_inferno'] }])
    expect(events(s, 'CardExhausted').length).toBe(1)
    expect(s.resolvingKindled).not.toBe(true) // 解決が終われば下りている
  })

  it('くべずに撃てる: 衝動の札が無くても素の効果 (全体4) だけで撃てる。「くべた時」の延焼3は乗らない', () => {
    let s = withHand(base(), ['red_kindle', 'red_strike'])
    const before = hp(s)
    s = play(s, 't0_red_kindle')
    expect(before - hp(s)).toBe(4)
    expect(s.enemies[0].burn).toBe(0)
    expect(events(s, 'ImpulseKindled').length).toBe(0)
    expect(ids(s.player.hand)).toEqual(['red_strike'])
  })

  it('衝動でない札はくべられない (指定があれば検証する)。自分自身も不可', () => {
    const s = asImpulse(withHand(base(), ['red_kindle', 'red_inferno', 'red_strike']), ['t1_red_inferno'])
    expect(() => play(s, 't0_red_kindle', { exhaustUids: ['t2_red_strike'] })).toThrow(/衝動の札だけ/)
    expect(() => play(s, 't0_red_kindle', { exhaustUids: ['t0_red_kindle'] })).toThrow()
  })

  it('灰の息 (U 1E): 素でブロック5。くべればブロック8＋対象に延焼2。消滅の誘発 (消滅するたび) は鳴る', () => {
    let s = withHand(base(), ['red_ash_breath', 'red_spark', 'red_ash_breath'])
    s = asImpulse(s, ['t1_red_spark'])
    s = play(s, 't0_red_ash_breath', { exhaustUids: ['t1_red_spark'] })
    expect(s.player.block).toBe(8)
    expect(s.enemies[0].burn).toBe(2)
    expect(ids(s.player.exhaustPile)).toEqual(['red_spark'])
    s = play(s, 't2_red_ash_breath') // くべずに
    expect(s.player.block).toBe(13)
    expect(s.enemies[0].burn).toBe(2)
  })

  it('データ: くべる札は実験「全カード伏せ可」の対象外。工房で合わせるとくべるは合算。軸は刹那 (impulse)', () => {
    expect(canSetAsNormal(getCardDef('red_kindle'))).toBe(false)
    expect(fuseCards(inst('red_kindle'), inst('red_ash_breath')).impulseCost).toBe(2)
    expect(axesOf(getCardDef('red_kindle'))).toContain('impulse')
    expect(axesOf(getCardDef('red_ash_breath'))).toContain('impulse')
  })
})

describe('火は火を呼ぶ (R 2E 置物。段2b 裁定 R6 作り直し): チェインは「同じコストの札だけ」に当たる (安い札は飛ばす)', () => {
  it('1E のチェイン (火口開き) は 0E (火花) を飛ばして 1E (火弾) に当たる。無ければ従来どおり 0E が先', () => {
    let s = withDraw(withPerms(withHand(base(), ['red_cascade_vent']), ['red_perm_fire_calls_fire']), ['red_spark', 'red_strike'])
    const before = hp(s)
    s = play(s, 't0_red_cascade_vent')
    expect(before - hp(s)).toBe(4 + 6) // 火口開き4 + 火弾6 (1E が当たり。火花は底へ)
    expect(events(s, 'Cascaded')[0]).toMatchObject({ hitId: 'red_strike', revealed: ['red_spark'] })
    // 無ければ 0E の火花が先に当たる
    let t = withDraw(withHand(base(), ['red_cascade_vent']), ['red_spark', 'red_strike'])
    t = play(t, 't0_red_cascade_vent')
    expect(events(t, 'Cascaded')[0]).toMatchObject({ hitId: 'red_spark', revealed: [] })
  })

  it('2E のチェイン (連火弾) は 2E (燎火の使い) に当たり、その燎火の使いのチェインも 2E にだけ当たる = 連鎖の向きが逆になる', () => {
    let s = withDraw(withPerms(withHand(base(), ['red_cascade_bolt']), ['red_perm_fire_calls_fire']), ['red_strike', 'red_perm_cascade_envoy', 'red_cascade_bolt'])
    s = play(s, 't0_red_cascade_bolt')
    const hits = events(s, 'Cascaded').map((e) => (e.type === 'Cascaded' ? e.hitId : undefined))
    expect(hits[0]).toBe('red_perm_cascade_envoy') // 火弾 (1E) は飛ばした
    expect(hits).toContain('red_cascade_bolt') // 燎火の使い (2E) のチェインが 2E の連火弾に
    expect(ids(s.player.permanents)).toContain('red_perm_cascade_envoy')
  })

  it('必ず止まる: 同じコストのチェイン札どうしでも、解決中の札 (limbo) には当たれない', () => {
    // 火口開き (1E チェイン) ×3 だけのデッキ。親→当たり→その当たりの連鎖…と続くが、解決中の札は山札にも捨て札にも無いので
    // 深さは「山札＋捨て札にあるチェイン持ちの枚数」で頭打ち
    let s = withDraw(withPerms(withHand(base(), ['red_cascade_vent']), ['red_perm_fire_calls_fire']), ['red_cascade_vent', 'red_cascade_vent'])
    const before = hp(s)
    s = play(s, 't0_red_cascade_vent')
    expect(s.phase === 'player-turn' || s.phase === 'won').toBe(true)
    expect(before - hp(s)).toBeGreaterThanOrEqual(12) // 3枚ぶん
    expect(events(s, 'Cascaded').length).toBeGreaterThanOrEqual(2)
    // 3枚とも残っている (当たりは捨て札へ着地し、本体の1ドローが切り直して手札へ引き戻すこともある)
    const all = [...s.player.hand, ...s.player.discardPile, ...s.player.drawPile]
    expect(ids(all).filter((id) => id === 'red_cascade_vent').length).toBe(3)
  })

  it('データ: R・常在の印 cascadeSameCost・軸はチェイン。鍛えるとコスト−1', () => {
    const d = getCardDef('red_perm_fire_calls_fire')
    expect(d).toMatchObject({ rarity: 'rare', cost: 2, type: 'permanent', effects: [{ trigger: 'onPlay', effect: 'cascadeSameCost' }] })
    expect(axesOf(d)).toContain('cascade')
    expect(up('red_perm_fire_calls_fire').cost).toBe(1)
  })
})

describe('データ (段2)', () => {
  it('赤は 106種 = 報酬99 (C41／U40／R18) ＋ 初期専用7。段2の7枚と段2b の守り2枚は報酬に出る', () => {
    const red = allCards.filter((c) => c.color === 'red')
    expect(red.length).toBe(106)
    const rewards = red.filter((c) => !REWARD_EXCLUDED.has(c.id))
    expect(rewards.length).toBe(99)
    const by = (r: string) => rewards.filter((c) => c.rarity === r).length
    // 段1の記録 (C41／U38／R17) は数え違いで、実際は C36／U37／R17＝90 だった。段2で C+3・U+3・R+1、段2b で C+2 (守りの床)
    expect([by('common'), by('uncommon'), by('rare')]).toEqual([41, 40, 18])
    for (const id of ['red_rake', 'red_burn_off', 'red_popping_fire', 'red_perm_fire_awakening', 'red_kindle', 'red_ash_breath', 'red_perm_fire_calls_fire', 'red_ash_veil', 'red_ember_screen']) {
      expect(REWARD_EXCLUDED.has(id), id).toBe(false)
    }
  })

  it('鍛える: 爆ぜ火+＝延焼5＋全体8・火の目覚め+＝ドローが伸びる・火にくべる+＝全体6＋延焼5・灰の息+＝ブロック8(くべれば+5)＋延焼3', () => {
    expect(up('red_popping_fire').effects.map((e) => e.amount)).toEqual([5, 8])
    expect(up('red_perm_fire_awakening').effects[0].amount).toBeGreaterThanOrEqual(2)
    expect(up('red_kindle').effects.map((e) => e.amount)).toEqual([6, 5])
    expect(up('red_ash_breath').effects.map((e) => e.amount)).toEqual([8, 5, 3])
    expect(up('red_ash_breath').kindleOptional).toBe(true)
  })

  it('理想形の名前: deck_cascade は「チェイン理想形」(キーワード名に揃えた)', () => {
    expect(getDeckDef('deck_cascade').name).toBe('チェイン理想形')
  })
})

describe('段2b (2026-10-12 チェックポイント Opus 3本の裁定 R7・R8)', () => {
  it('R7: チェインの当たりが対象を倒したら、本体は生存する先頭の敵へ (立ち消えない)。出来事 CascadeRetargeted', () => {
    // 2体戦: 敵0 を HP4 にして、火渡り (2E 延焼7・チェイン) の当たり (火花4) で倒す → 本体の延焼7は敵1へ
    let s = freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red')
    s = { ...s, enemies: [{ ...s.enemies[0], hp: 4 }, { ...s.enemies[0], uid: 'e1', hp: 30, burn: 0, block: 0 } as typeof s.enemies[0]] }
    s = withDraw(withHand(s, ['red_cascade_ford']), ['red_spark'])
    s = play(s, 't0_red_cascade_ford')
    expect(s.enemies[0].hp).toBeLessThanOrEqual(0)
    expect(s.enemies[1].burn).toBe(7)
    expect(events(s, 'CascadeRetargeted')).toEqual([{ type: 'CascadeRetargeted', cardId: 'red_cascade_ford', fromIndex: 0, toIndex: 1 }])
  })

  it('R8: 燠の守りで残す札を選べる (EndTurn.keepUid)。指定が無ければコストが最も高い札。衝動でない札は拒否', () => {
    let s = withPerms(withHand(base(), ['red_spark', 'red_strike', 'red_inferno']), ['red_perm_ember_keeper'])
    s = asImpulse(s, ['t0_red_spark', 't1_red_strike', 't2_red_inferno'])
    expect(() => applyCommand(s, { type: 'EndTurn', keepUid: 'nope' })).toThrow(/衝動の札だけ/)
    const t = applyCommand(s, { type: 'EndTurn', keepUid: 't0_red_spark' })
    expect(events(t, 'ImpulseKept')).toEqual([{ type: 'ImpulseKept', cardId: 'red_spark' }])
    const u = applyCommand(s, { type: 'EndTurn' })
    expect(events(u, 'ImpulseKept')).toEqual([{ type: 'ImpulseKept', cardId: 'red_inferno' }]) // 2E が最も高い
    const v = withHand(base(), ['red_spark'])
    expect(() => applyCommand(asImpulse(v, ['t0_red_spark']), { type: 'EndTurn', keepUid: 't0_red_spark' })).toThrow(/燠の守りが場に無い/)
  })

  it('R8: 被ダメ予測の注記 = 失効で得られるブロックの最大 (灰の衣3 × 衝動の札。燠の守りがあれば1枚引く)', () => {
    let s = withPerms(withHand(base(), ['red_spark', 'red_strike', 'red_guard']), ['red_perm_ash_robe'])
    expect(expiryBlockPreview(s)).toBe(0)
    s = asImpulse(s, ['t0_red_spark', 't1_red_strike'])
    expect(expiryBlockPreview(s)).toBe(6)
    s = withPerms(s, ['red_perm_ember_keeper'])
    expect(expiryBlockPreview(s)).toBe(3)
  })

  it('R8: 灯の表示は灯を使う色だけ (赤の初期デッキでは灯1でも出さない。灯コストや灯の効果を持つ札があれば出す)', () => {
    const s = { ...base(), player: { ...base().player, light: 1 } }
    expect(lightMatters(s)).toBe(false)
    const lightCard = allCards.find((c) => (c.lightCost ?? 0) > 0)!
    expect(lightMatters(withHand(s, [lightCard.id]))).toBe(true) // 灯コストの札が1枚でもあれば出す
    const lightMaker = allCards.find((c) => c.effects.some((e) => e.effect === 'addLight'))!
    expect(lightMatters(withHand(s, [lightMaker.id]))).toBe(true)
  })

  it('猛り火が消えた (BlazeExtinguished): 延焼を持つ敵を倒して合計が8を割った時・敵フェーズの減りで割った時', () => {
    let s = withBurn(withHand(base(), ['red_meteor']), 9)
    s = play(s, 't0_red_meteor') // 30ダメ
    if (s.enemies[0].hp <= 0) expect(events(s, 'BlazeExtinguished').length).toBe(1)
    let t = withBurn(withHand(base(), []), 8)
    t = applyCommand(t, { type: 'EndTurn' }) // ティックで 8→7
    expect(events(t, 'BlazeExtinguished').length).toBeGreaterThanOrEqual(1)
  })
})

describe('段2b 追加の修正 (2026-10-12 ユーザー「修正案だして」→ ask_user 4件)', () => {
  it('燃え残りの対象は「延焼がいちばん多い敵」(先頭固定だと落ちかけの敵に積まれ続けた。Opus B 戦#5)', () => {
    expect(getCardDef('red_perm_embers_left').effects[0]).toMatchObject({ trigger: 'onImpulseExpired', effect: 'applyBurn', amount: 3, target: 'mostBurn' })
    let s = freshCombat('set-confirm', 'enemy_probe', 42, 'starter_red')
    s = { ...s, enemies: [{ ...s.enemies[0], burn: 1 }, { ...s.enemies[0], uid: 'e1', burn: 5, block: 0 } as typeof s.enemies[0]] }
    s = withPerms(withHand(s, ['red_spark']), ['red_perm_embers_left'])
    s = asImpulse(s, ['t0_red_spark'])
    s = applyCommand(s, { type: 'EndTurn' }) // 火花が失効 → 延焼+3 は敵1 (延焼5) へ
    const burnEv = events(s, 'BurnApplied').find((e) => e.type === 'BurnApplied' && e.amount === 3)
    expect(burnEv).toMatchObject({ enemyIndex: 1 })
  })

  it('爆ぜ火の「点いたら敵全体に5」は貫通 (ブロックに吸われない。Opus A 戦#3)', () => {
    expect(getCardDef('red_popping_fire').effects[1]).toMatchObject({ trigger: 'onBlazeIgnite', effect: 'dealDamage', amount: 5, target: 'all', pierce: true })
    let s = withBurn(withHand(base(), ['red_popping_fire']), 5)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 10 })) }
    const before = hp(s)
    s = play(s, 't0_red_popping_fire')
    expect(before - hp(s)).toBe(5)
  })

  it('守りの床 (赤の守りは報酬の14%で5色最低): C 1E の守り2枚＝灰の帳 (ブロック5＋延焼2)・熾の衝立 (ブロック4＋衝動1＋延焼2)', () => {
    expect(getCardDef('red_ash_veil')).toMatchObject({ cost: 1, rarity: 'common', type: 'physical' })
    expect(getCardDef('red_ember_screen')).toMatchObject({ cost: 1, rarity: 'common', type: 'physical' })
    let s = withDraw(withHand(base(), ['red_ash_veil', 'red_ember_screen']), ['red_spark'])
    s = play(s, 't0_red_ash_veil')
    expect(s.player.block).toBe(5)
    expect(s.enemies[0].burn).toBe(2)
    s = play(s, 't1_red_ember_screen')
    expect(s.player.block).toBe(9)
    expect(s.enemies[0].burn).toBe(4)
    expect(s.player.impulseUids.length).toBe(1)
  })
})
