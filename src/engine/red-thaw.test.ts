// 赤の解凍 (2026-10-10。docs/red-archetype-audit-2026-10-10.md → docs/red-cascade-design-2026-10-10.md。
// ユーザー裁定: 軸＝バーン／刹那／チェイン・猛火の儀式=1E・鍛えるの例外=焔の目録/火だるま・
// チェインの当たりは親と同じ敵で選択式/X/仕込み札は飛ばす・カオスは味付け4/撤去8・キーワード名「チェイン」) を機械固定
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef, getDeckDef } from './content.ts'
import { axesOf, createRun, REWARD_EXCLUDED } from './run.ts'
import { applyCommand, createInitialState } from './state.ts'
import { freshCombat, restIntent, withHand, withIntent } from './test-helpers.ts'
import { upgradeCard, upgradeTier } from './upgrade.ts'
import { fuseCards } from './fusion.ts'
import { isPlayableFromHand } from './effects.ts'
import type { CardInstance, GameState } from './types.ts'

const inst = (id: string, n = 0): CardInstance => ({ uid: `d${n}_${id}`, def: getCardDef(id) })
const up = (id: string) => upgradeCard({ uid: 'u', def: getCardDef(id) }).def
/** 山札を指定の並び (左が上) に差し替え、捨て札を空にする */
const withDraw = (s: GameState, ids: readonly string[], discard: readonly string[] = []): GameState => ({
  ...s,
  player: { ...s.player, drawPile: ids.map((id, i) => inst(id, i)), discardPile: discard.map((id, i) => inst(id, 100 + i)) },
})
const withPerms = (s: GameState, ids: readonly string[]): GameState => ({
  ...s,
  player: { ...s.player, permanents: [...s.player.permanents, ...ids.map((id, i) => ({ uid: `p${i}_${id}`, def: getCardDef(id) }))] },
})
const withBurn = (s: GameState, burn: number): GameState => ({ ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, burn } : e)) })
const base = () => freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red')
const hibana = () =>
  applyCommand(createInitialState(42, 'set-confirm'), { type: 'StartCombat', seed: 42, enemyId: 'enemy_brute', deckId: 'run_basic_red', leaderId: 'leader_red' })
const play = (s: GameState, uid: string) => applyCommand(s, { type: 'PlayCard', cardUid: uid, targetIndex: 0 })
const hp = (s: GameState) => s.enemies[0].hp
const ids = (cs: readonly CardInstance[]) => cs.map((c) => c.def.id)
const cascaded = (s: GameState) => s.eventLog.filter((e) => e.type === 'Cascaded')

describe('チェイン (赤の第3の軸。MTG の続唱)', () => {
  it('山札の上から、この札よりコストの低い札が出るまでめくり、その札をコストを払わずにプレイする', () => {
    let s = withDraw(withHand(base(), ['red_cascade_bolt']), ['red_strike', 'red_inferno'])
    const before = hp(s)
    s = play(s, 't0_red_cascade_bolt')
    expect(before - hp(s)).toBe(11 + 6) // 連火弾11 + 火弾6 (火弾はタダ)
    expect(s.player.energy).toBe(3 - 2)
    expect(ids(s.player.drawPile)).toEqual(['red_inferno'])
    expect(ids(s.player.discardPile).sort()).toEqual(['red_cascade_bolt', 'red_strike'])
    expect(cascaded(s)).toEqual([{ type: 'Cascaded', cardId: 'red_cascade_bolt', hitId: 'red_strike', revealed: [] }])
    expect(s.player.cascadesThisCombat).toBe(1)
  })

  it('めくって外れた札は山札の底へ (順はシードRNG)。最初に出た安い札が当たり', () => {
    let s = withDraw(withHand(base(), ['red_cascade_bolt']), ['red_inferno', 'red_meteor', 'red_spark', 'red_strike'])
    const before = hp(s)
    s = play(s, 't0_red_cascade_bolt')
    expect(before - hp(s)).toBe(11 + 4) // 当たりは火花 (0E)。火弾 (1E) も当たれるが火花が先
    const pile = ids(s.player.drawPile)
    expect(pile[0]).toBe('red_strike')
    expect(pile.slice(1).sort()).toEqual(['red_inferno', 'red_meteor'])
    expect(cascaded(s)[0]).toMatchObject({ hitId: 'red_spark', revealed: ['red_inferno', 'red_meteor'] })
  })

  it('1E のチェインは 0E の札にしか当たらない (同じコストは飛ばす)', () => {
    let s = withDraw(withHand(base(), ['red_cascade_strike']), ['red_strike', 'red_spark'])
    const before = hp(s)
    s = play(s, 't0_red_cascade_strike')
    expect(before - hp(s)).toBe(7 + 4)
    expect(ids(s.player.drawPile)).toEqual(['red_strike'])
  })

  it('当たりが無ければ空振り: 何も起きず、めくった札は底へ戻る (回数には数えない)', () => {
    let s = withDraw(withHand(base(), ['red_cascade_bolt']), ['red_inferno', 'red_meteor'])
    const before = hp(s)
    s = play(s, 't0_red_cascade_bolt')
    expect(before - hp(s)).toBe(11)
    expect(ids(s.player.drawPile).sort()).toEqual(['red_inferno', 'red_meteor'])
    expect(cascaded(s)[0]).toEqual({ type: 'Cascaded', cardId: 'red_cascade_bolt', revealed: ['red_inferno', 'red_meteor'] })
    expect(s.player.cascadesThisCombat ?? 0).toBe(0)
  })

  it('山札が尽きたら捨て札を切り直して続ける (通常のドローと同じ)', () => {
    let s = withDraw(withHand(base(), ['red_cascade_bolt']), [], ['red_spark'])
    const before = hp(s)
    s = play(s, 't0_red_cascade_bolt')
    expect(before - hp(s)).toBe(11 + 4)
    expect(s.eventLog.some((e) => e.type === 'DeckShuffled')).toBe(true)
    expect(ids(s.player.discardPile).sort()).toEqual(['red_cascade_bolt', 'red_spark'])
  })

  it('選択式・X・仕込み札・使えない札は飛ばす (底へ)', () => {
    const xCard = allCards.find((c) => c.xCost === true)!
    let s = withDraw(withHand(base(), ['red_cascade_bolt']), ['red_mode_firecontrol', 'green_reaction_vine', xCard.id, 'status_wound', 'red_spark'])
    const before = hp(s)
    s = play(s, 't0_red_cascade_bolt')
    expect(before - hp(s)).toBe(11 + 4)
    expect(cascaded(s)[0]).toMatchObject({ hitId: 'red_spark' })
    expect(ids(s.player.drawPile).sort()).toEqual(['green_reaction_vine', 'red_mode_firecontrol', 'status_wound', xCard.id].sort())
  })

  it('連鎖: 当たった札がチェインを持てば続く (3E→2E→1E)。回数は当たりごとに数える', () => {
    let s = withDraw(withHand(base(), ['red_cascade_great']), ['red_cascade_bolt', 'red_strike'])
    const before = hp(s)
    s = play(s, 't0_red_cascade_great')
    expect(before - hp(s)).toBe(16 + 11 + 6)
    expect(s.player.energy).toBe(0)
    expect(s.player.cascadesThisCombat).toBe(2)
    expect(cascaded(s).map((e) => (e.type === 'Cascaded' ? e.hitId : ''))).toEqual(['red_cascade_bolt', 'red_strike']) // 外側の当たり → その札の連鎖、の順にログに載る
  })

  it('連火の炉心: 毎ターン最初にプレイする札にチェインがつく。チェインは本体より先に解決する (当たりで点けて本体の猛り火で刈る)', () => {
    // 延焼弾 (2E・8ダメ・延焼6・猛り火なら1ドロー) にチェインがつき、くすぶる残り火 (0E・延焼4) が先に解決 → 本体の延焼6で合計10 ≥ 8 → ドローが鳴る
    let s = withPerms(withDraw(withHand(base(), ['red_spreading_fire', 'red_strike']), ['red_ember', 'red_guard']), ['red_perm_cascade_core'])
    s = play(s, 't0_red_spreading_fire')
    expect(s.enemies[0].burn).toBe(10)
    expect(ids(s.player.hand)).toEqual(['red_strike', 'red_guard']) // 猛り火のドローで灰の盾を引いた
    expect(s.player.cascadesThisCombat).toBe(1)
    // 2枚目にはつかない
    s = withDraw(s, ['red_spark'])
    s = play(s, 't1_red_strike')
    expect(cascaded(s)).toHaveLength(1)
    expect(ids(s.player.drawPile)).toEqual(['red_spark'])
  })

  it('チェインでプレイした札も「プレイ」: ひばなの勢い・攻撃の誘発・詠唱数に数える。エナジーは払わない', () => {
    let s = withDraw(withHand(hibana(), ['red_cascade_bolt']), ['red_strike'])
    const before = hp(s)
    s = play(s, 't0_red_cascade_bolt')
    expect(s.player.momentum).toBe(4) // 火弾 (+2) → 連火弾 (+2)
    expect(before - hp(s)).toBe(6 + (11 + 2)) // 火弾は勢い0で6・連火弾は火弾の勢い+2を乗せて13
    expect(s.player.cardsPlayedThisTurn).toBe(2)
    expect(s.player.energy).toBe(1)
  })

  it('火の渦: この戦闘でチェインが当たった回数×6。0回なら不発', () => {
    let s = withHand(base(), ['red_cascade_vortex'])
    const before = hp(s)
    s = play({ ...s, player: { ...s.player, cascadesThisCombat: 3 } }, 't0_red_cascade_vortex')
    expect(before - hp(s)).toBe(18)
    s = withHand(base(), ['red_cascade_vortex'])
    expect(hp(play(s, 't0_red_cascade_vortex'))).toBe(hp(s))
  })

  it('連なる火花・火継ぎの炉: チェインで札がプレイされるたび 3ダメ・延焼+2。空振りでは鳴らない', () => {
    let s = withPerms(withDraw(withHand(base(), ['red_cascade_bolt']), ['red_strike']), ['red_perm_cascade_sparks', 'red_perm_cascade_hearth'])
    const before = hp(s)
    s = play(s, 't0_red_cascade_bolt')
    expect(before - hp(s)).toBe(11 + 6 + 3)
    expect(s.enemies[0].burn).toBe(2)
    let t = withPerms(withDraw(withHand(base(), ['red_cascade_bolt']), ['red_inferno']), ['red_perm_cascade_sparks', 'red_perm_cascade_hearth'])
    const b2 = hp(t)
    t = play(t, 't0_red_cascade_bolt')
    expect(b2 - hp(t)).toBe(11)
    expect(t.enemies[0].burn).toBe(0)
  })

  it('野火 (チェイン×3・効果なし) はプレイできる。双つ火は2回めくる', () => {
    expect(isPlayableFromHand(inst('red_brushfire'))).toBe(true)
    // 当たりは消滅札 (捨て札に戻らない) にして、連火弾の連鎖が切り直しで当たり直さないようにする
    let s = withDraw(withHand(base(), ['red_brushfire']), ['red_ember', 'red_ritual', 'red_cascade_bolt', 'red_inferno'])
    const before = hp(s)
    s = play(s, 't0_red_brushfire')
    // 1回目=くすぶる残り火 (延焼4)・2回目=猛火の儀式 (エナジー+2)・3回目=連火弾 (11。その連鎖は業火しか残らず空振り)
    expect(before - hp(s)).toBe(11)
    expect(s.enemies[0].burn).toBe(4)
    expect(s.player.energy).toBe(0 + 2)
    expect(s.player.cascadesThisCombat).toBe(3)
    expect(cascaded(s)).toHaveLength(4) // 野火×3 + 連火弾の空振り1
    expect(ids(s.player.exhaustPile).sort()).toEqual(['red_ember', 'red_ritual'])
  })

  it('チェインは軸 cascade・工房で継承される (反復と同じ)', () => {
    expect(axesOf(getCardDef('red_cascade_bolt'))).toContain('cascade')
    expect(axesOf(getCardDef('red_cascade_vortex'))).toContain('cascade')
    const fused = fuseCards(inst('red_cascade_bolt', 1), inst('red_strike', 2))
    expect(fused.cascade).toBe(1)
    const twin = fuseCards(inst('red_twin_fire', 1), inst('red_cascade_bolt', 2))
    expect(twin.cascade).toBe(2)
  })
})

describe('バーンと刹那の穴埋め', () => {
  it('燻り: この置物がある間、延焼は敵フェーズで減らない', () => {
    let s = withIntent(withBurn(withPerms(withHand(base(), []), ['red_perm_smolder']), 5), restIntent())
    const before = hp(s)
    s = applyCommand(s, { type: 'EndTurn' })
    expect(before - hp(s)).toBe(5)
    expect(s.enemies[0].burn).toBe(5)
    let t = withIntent(withBurn(withHand(base(), []), 5), restIntent())
    t = applyCommand(t, { type: 'EndTurn' })
    expect(t.enemies[0].burn).toBe(4)
  })

  it('火勢倍し: 対象の延焼を2倍にする (消滅)。延焼0なら不発', () => {
    let s = withBurn(withHand(base(), ['red_burn_double']), 6)
    s = play(s, 't0_red_burn_double')
    expect(s.enemies[0].burn).toBe(12)
    expect(ids(s.player.exhaustPile)).toEqual(['red_burn_double'])
    let t = withHand(base(), ['red_burn_double'])
    t = play(t, 't0_red_burn_double')
    expect(t.enemies[0].burn).toBe(0)
    expect(t.eventLog.some((e) => e.type === 'BurnApplied')).toBe(false)
  })

  it('灰かぶり・火の舌: 対象の延焼×1 のブロック／ダメージ。延焼は失わない (爆熱と違う)', () => {
    let s = withBurn(withHand(base(), ['red_ash_cover', 'red_fire_tongue']), 6)
    s = play(s, 't0_red_ash_cover')
    expect(s.player.block).toBe(3 + 6)
    expect(s.enemies[0].burn).toBe(6)
    const before = hp(s)
    s = play(s, 't1_red_fire_tongue')
    expect(before - hp(s)).toBe(6)
    expect(s.enemies[0].burn).toBe(8)
  })

  it('燃え残り・灰の衣: 衝動の札が失効するたび延焼+2・ブロック3 (失効1枚ごと。失効は衝動の出来事なので消滅より先)', () => {
    let s = withPerms(withHand(base(), ['red_strike', 'red_guard']), ['red_perm_embers_left', 'red_perm_ash_robe'])
    s = withIntent({ ...s, player: { ...s.player, impulseUids: ['t0_red_strike', 't1_red_guard'] } }, restIntent())
    const before = hp(s)
    s = applyCommand(s, { type: 'EndTurn' })
    expect(ids(s.player.exhaustPile).sort()).toEqual(['red_guard', 'red_strike'])
    expect(before - hp(s)).toBe(4) // 延焼4 (2枚×2) が敵フェーズ開始で焼く
    expect(s.enemies[0].burn).toBe(3)
    expect(s.eventLog.filter((e) => e.type === 'BlockGained' && e.target === 'player' && e.amount === 3)).toHaveLength(2)
  })

  it('燠の守り: 失効する衝動の札のうちコストが最も高い1枚を手札に残し、敵フェーズ後の全捨ても1回だけ生き残る', () => {
    let s = withPerms(withHand(base(), ['red_strike', 'red_inferno', 'red_guard']), ['red_perm_ember_keeper'])
    s = withIntent({ ...s, player: { ...s.player, impulseUids: ['t0_red_strike', 't1_red_inferno'] } }, restIntent())
    s = applyCommand(s, { type: 'EndTurn' })
    expect(ids(s.player.exhaustPile)).toEqual(['red_strike'])
    expect(s.eventLog.some((e) => e.type === 'ImpulseKept' && e.cardId === 'red_inferno')).toBe(true)
    const kept = s.player.hand.find((c) => c.uid === 't1_red_inferno')
    expect(kept).toBeDefined() // 次の自ターンの手札に残っている (灰の盾は普通に捨てられた)
    expect(kept?.keptImpulse).toBeUndefined() // 旗は全捨てで下ろした = 次の全捨てでは普通に捨てられる
    expect(s.player.impulseUids).toEqual([])
    expect(s.player.hand.some((c) => c.uid === 't2_red_guard')).toBe(false)
  })

})

describe('鍛える (本家形へ。例外2枚)', () => {
  it('赤は本家形: 連火弾+ 17ダメ (量+50%)・火の渦+ ×7 (参照倍率+1)・熾火の一閃+ 9ダメ+衝動3 (単位+1)・野火+ 2E (数字が無いのでコスト-1)', () => {
    expect(up('red_cascade_bolt').effects[0].amount).toBe(17)
    expect(up('red_cascade_vortex').effects[0].amount).toBe(7)
    expect(up('red_ember_slash').effects.map((e) => e.amount)).toEqual([9, 3])
    expect(up('red_brushfire').cost).toBe(2)
    expect(up('red_brushfire').cascade).toBe(3)
  })

  it('例外: 焔の目録はコスト-1のまま (勢い+4 に戻さない)・火だるまは旧3段 (延焼3・被弾×1 のまま)', () => {
    expect(upgradeTier(getCardDef('red_perm_ledger'))).toBe('cost')
    const ledger = up('red_perm_ledger')
    expect(ledger.cost).toBe(1)
    expect(ledger.effects[0].amount).toBe(2)
    const pyre = up('red_perm_pyre')
    expect(pyre.effects.map((e) => [e.effect, e.amount])).toEqual([['applyBurn', 3], ['applyBurnPerDamageTaken', 1]])
  })
})

describe('データとラン', () => {
  it('赤 97種・報酬91 (チェイン14・穴埋め7・触媒2 を足し、カオス8と重なり2を撤去)。撤去した札は無い', () => {
    const red = allCards.filter((c) => c.color === 'red')
    expect(red.length).toBe(97)
    expect(red.filter((c) => !REWARD_EXCLUDED.has(c.id)).length).toBe(91)
    for (const id of ['red_long_shot', 'red_streak_bet', 'red_all_in', 'red_double_dice', 'red_leap_arrow', 'red_double_down', 'red_grand_fireworks', 'red_perm_gambler', 'red_perm_ward', 'red_blaze_storm']) {
      expect(red.some((c) => c.id === id), id).toBe(false)
    }
    for (const id of ['red_gamble', 'red_fluke_spark', 'red_bomb', 'red_chaos_bolt']) expect(red.some((c) => c.id === id), id).toBe(true) // カオスの味付け4
    expect(red.filter((c) => (c.cascade ?? 0) > 0)).toHaveLength(10) // 1E×2・2E×4 (置物1含む)・3E×4 (野火・双つ火含む)
    expect(red.filter((c) => c.fusionCatalyst !== undefined).map((c) => c.fusionCatalyst).sort()).toEqual(['aoe', 'cheaper'])
    expect(getCardDef('red_leaping_flame').name).toBe('飛び火') // 既存の札はそのまま (キーワード名は「チェイン」)
  })

  it('チェインを持つ札は赤だけで、コストは1以上 (0E のチェインは当たりが無い)', () => {
    for (const c of allCards.filter((c) => (c.cascade ?? 0) > 0)) {
      expect(c.color, c.id).toBe('red')
      expect(c.cost, c.id).toBeGreaterThanOrEqual(1)
    }
  })

  it('理想形: 飛び火理想形 (deck_cascade) がカオスの運任せ理想形を置き換えた。刹那理想形は改名だけ', () => {
    const d = getDeckDef('deck_cascade')
    expect(d.cards.some((c) => c.cardId === 'red_cascade_vortex')).toBe(true)
    expect(() => getDeckDef('deck_gamble')).toThrow()
    expect(getDeckDef('deck_chaos').name).toBe('刹那理想形')
  })

  it('伏せ参照レリック (符師の懐・静かな鈴・二重の符・読みの眼鏡・回収の紐) は赤単の候補列に出ない。緑には出る', () => {
    const red = createRun(3, 'set-confirm', 'leader_red').relicQueue
    for (const id of ['relic_talisman_pouch', 'relic_quiet_bell', 'relic_double_talisman', 'relic_reading_glasses', 'relic_retrieve_cord']) {
      expect(red, id).not.toContain(id)
    }
    const green = createRun(3, 'set-confirm', 'leader_green').relicQueue
    expect(green).toContain('relic_double_talisman')
    expect(green).toContain('relic_reading_glasses')
  })
})
