// sim/run.ts — ヘッドレス自動対戦 (Node 単体実行: npm run sim)
//
// 使い方:
//   npm run sim -- [1セルあたりの対戦数] [ベースシード]        # 単発戦闘: デッキ×敵の総当たり
//   npm run sim -- [対戦数] [シード] all                        # 3方式比較モード
//   npm run sim -- runs [ラン数] [ベースシード]                 # ドラフト連戦の走破率
//
// ボットは全構成共通の単純グリーディ (差がボットの賢さではなくルール・デッキから出るようにする):
//   - 自ターン: (set系) まずリアクションを1枚伏せる → ランプ→置物→成長→フィニッシャー→攻撃→防御
//     の優先順でエナジーの続く限りプレイ (hold-manual は最安リアクションのコストを温存)
//   - 勢い生成付き攻撃は他の攻撃より先に。成長0での doubleGrowth は打たない
//   - 割り込み: set-confirm は常に「発動」、hold-manual は発動可能な先頭カードを常に発動
//   - ランの報酬ピック: 常に先頭 (index 0)

import { canUpgradeCard } from '../engine/upgrade.ts'
import { allDecks, allEnemies, allLeaders, getCardDef, getEventDef } from '../engine/content.ts'
import { effectiveCost, isBlazing, isDamageEffect, isPlayableFromHand } from '../engine/effects.ts'
import { RESTRAIN_PLAY_CAP } from '../engine/combat.ts'
import { playableReactions } from '../engine/reactions/hold-manual.ts'
import { applyRunCommand, createRun, isUpgraded, nextChoices } from '../engine/run.ts'
import { BOSS_ROW } from '../engine/map.ts'
import { applyCommand, createInitialState } from '../engine/state.ts'
import type { CardDef, CardInstance, Command, GameState, ReactionMode } from '../engine/types.ts'

/**
 * ボット用の役割分類。カードタイプ廃止後は効果から導出する
 * (タイプは物理/呪文/リアクション/置物の機械的区分になったため)
 */
type BotRole = 'ramp' | 'draw' | 'handpayoff' | 'permanent' | 'growth' | 'echo' | 'bighit' | 'attack' | 'defend' | 'payoff' | 'reaction' | 'other'

function botRole(def: CardDef): BotRole {
  if (def.type === 'reaction') return 'reaction'
  if (def.type === 'permanent') return 'permanent'
  const effects = def.modes?.length ? def.modes[0].effects : def.effects
  const has = (...ids: string[]) => effects.some((e) => ids.includes(e.effect))
  if (has('gainEnergyMax', 'gainEnergy', 'discountNext', 'addCasts')) return 'ramp' // 焚べる=詠唱の前積みも早置き
  // 召喚 (白): トークンを場に出すカードは置物枠で早置き
  if (has('summonPermanent')) return 'permanent'
  // ブロック参照の換金札は「壁を積んでから」なので最後に回す (2026-08-26)
  if (has('dealDamagePerBlock')) return 'payoff'
  // 抱え込み (青 2026-08-31): 手札参照は「手札が厚いうちに」= ドローの直後・手札を減らす前に撃つ
  if (has('dealDamagePerHandCard', 'gainIceBlockPerHandCard')) return 'handpayoff'
  // 反復 (青): トークンは大呪文の直前に立てる (bighit/attack より先)
  if (has('addSpellEcho')) return 'echo'
  if (has('addGrowth', 'doubleGrowth')) return 'growth'
  if (effects.some(isDamageEffect)) return def.cost >= 3 ? 'bighit' : 'attack'
  // 純延焼 (火の粉の雨) と混乱 (幻惑の囁き) は攻撃系として運用する
  if (has('applyBurn', 'confuse')) return def.cost >= 3 ? 'bighit' : 'attack'
  // 緑のカード操作 (2026-09-02): 回収・サーチ・手札で鍛えるはカードアドバンテージ系としてドロー枠で早めに撃つ
  if (has('retrieveFromDiscard', 'searchDeck', 'upgradeInHand')) return 'draw'
  if (has('drawCards', 'impulseDraw', 'drawCardsPerCardPlayed', 'dischargeAetherDraw', 'exhaustFromDeck')) return 'draw'
  // コスト再利用 (黒): 死者再生・屍集めはカードアドバンテージ系としてドロー枠で運用する
  if (has('retrieveFromExhaust', 'playFromExhaust')) return 'draw'
  // 骨刃の生成 (黒 2026-09-01): ナイフを撒いてから殴る = ドロー枠で早めに
  if (has('addCardToHand')) return 'draw'
  if (has('gainBlock', 'gainIceBlock', 'gainIceBlockPerCardPlayed', 'gainBlockPerEnergyMax', 'gainBlockPerExhaust', 'gainHp', 'weakenEnemy')) return 'defend'
  return 'other'
}

const PLAY_PRIORITY: readonly BotRole[] = [
  'ramp',
  'draw', // ドローは先に (ストームの詠唱数も稼げる)
  'echo', // 反復トークンはペイオフの直前に立てる (手札参照×2が本命の結婚相手)
  'handpayoff', // 手札参照はドローの直後・手札を減らす前 (抱え込み 2026-08-31)
  'permanent', // 置物はエンジンなので早置き
  'growth',
  'bighit', // 旧フィニッシャー枠 (コスト3以上のダメージ札)
  'attack',
  'defend',
  // ブロック参照の換金札は最後 (2026-08-26)。attack が defend より先だったため、
  // 盾の乙女2体のターン開始ブロック4で城壁砕きがガードを通り、壁を積む前に4ダメで空撃ちしていた
  // = 要塞型の平均42.9ターンは測定器側の問題が混ざっていた
  'payoff',
]
const TURN_LIMIT = 50 // 無限戦闘の保険。超えたら敗北扱い

/** 成長0での doubleGrowth など、プレイしても無意味・不可能なカードを弾く */
function isWorthPlaying(state: GameState, card: CardInstance): boolean {
  if (card.def.effects.some((e) => e.effect === 'doubleGrowth')) return state.player.growth > 0
  // 成長放出: 純放出札は成長5まで積んでから刈る (2026-08-31 収穫軸化。2で刈ると雪だるまが育たない)。
  // 固定ダメージを併せ持つ札 (実りの一撃) は攻撃札なので2以上で撃ってよい
  if (card.def.effects.some((e) => e.effect === 'dischargeGrowth' || e.effect === 'dischargeGrowthBlock')) {
    const hasFlat = card.def.effects.some((e) => e.effect === 'dealDamage')
    return state.player.growth >= (hasFlat ? 2 : 4)
  }
  // 自傷カードはHPに余裕がないと自殺 (loseHp合計+5のマージン)
  const selfHarm = card.def.effects
    .filter((e) => e.effect === 'loseHp')
    .reduce((a, e) => a + (e.amount ?? 0), 0)
  if (selfHarm > 0 && state.player.hp <= selfHarm + 5) return false
  // 忘却 (ミル): 山札を食い尽くすと手が止まる (山札0+捨て札0=ドロー不能)。残りが薄いならミルしない
  const millAmount = card.def.effects
    .filter((e) => e.effect === 'exhaustFromDeck')
    .reduce((a, e) => a + (e.amount ?? 0), 0)
  // ダメージを併せ持つミル札 (亡霊の槍・絶望の重み) は攻撃札なので温存しない。
  // ブロック持ち (忘却の霧・墓石の盾) は対象のまま — 一度「ブロック持ちも除外」を試したところ、
  // ボットが山札を食い尽くして攻撃札まで全消滅させ「殺せない・死なない」526ターンの膠着を作った
  // (敵HP150/150のまま。2026-08-27 実測)。防御は他の札で足りるが、失った攻撃札は戻らない
  if (
    millAmount > 0 &&
    card.def.type !== 'reaction' &&
    !card.def.effects.some(isDamageEffect) &&
    state.player.drawPile.length < millAmount + 4
  ) {
    return false
  }
  // 墓地参照は消滅3枚以上でないと空撃ち (ドレイン版も同じ)
  if (
    card.def.effects.some(
      (e) => e.effect === 'dealDamagePerExhaust' || e.effect === 'dealDamageDrainPerExhaust',
    ) &&
    state.player.exhaustPile.length < 3
  ) {
    return false
  }
  // 輪廻 (黄泉還り) は消滅6枚以上でないと大損 (燃料の全放棄が対価)
  if (
    card.def.effects.some((e) => e.effect === 'recycleExhaust') &&
    state.player.exhaustPile.length < 6
  ) {
    return false
  }
  // 回復の換金 (滾る血汐) は回復2回以上でないと空撃ち
  if (
    card.def.effects.some((e) => e.effect === 'dealDamagePerHeal') &&
    state.player.healsThisCombat < 2
  ) {
    return false
  }
  // 自傷の換金 (背徳の収穫) は失ったHP5以上でないと空撃ち
  if (
    card.def.effects.some((e) => e.effect === 'dealDamagePerSelfHpLost') &&
    state.player.selfHpLost < 5
  ) {
    return false
  }
  // 猛り火 (2026-08-30): 点いていない時に撃つとおまけが空振りする。
  // 手札に「今払える延焼札」があるなら先にそちらを撃って点けてから使う
  if (
    card.def.effects.some((e) => e.condition?.blaze === true) &&
    // 自分で延焼を撒く札 (着火など) は自分で点けられるので温存しない
    !card.def.effects.some((e) => e.effect === 'applyBurn') &&
    !isBlazing(state)
  ) {
    const canIgniteFirst = state.player.hand.some(
      (c) =>
        c.uid !== card.uid &&
        c.def.effects.some((e) => e.effect === 'applyBurn') &&
        effectiveCost(state, c) <= state.player.energy &&
        !(state.player.restrain > 0 && (state.player.playsThisTurn ?? 0) >= RESTRAIN_PLAY_CAP),
    )
    if (canIgniteFirst) return false
  }
  // 勢いの変換器は勢い3以上でないと空撃ち (2026-08-30)
  if (
    card.def.effects.some(
      (e) => e.effect === 'dischargeMomentumBurn' || e.effect === 'dischargeMomentumBlock',
    ) &&
    state.player.momentum < 3
  ) {
    return false
  }
  // 爆熱は延焼3以上でないと換金損。逆上は被弾4以上、破城槌は敵ブロックがないと空撃ち
  if (card.def.effects.some((e) => e.effect === 'dischargeBurn')) {
    return state.enemies.some((e) => e.hp > 0 && e.burn >= 3)
  }
  // 被弾参照は「他に固定ダメージを持たない札」だけ温存する (2026-08-30 移管で床が付いた。
  // 茨の報い = 固定5 + 被弾×1 は被弾0でも5出るので、温存すると誤って腐らせる)
  if (
    card.def.effects.some((e) => e.effect === 'dealDamagePerDamageTaken') &&
    !card.def.effects.some((e) => e.effect === 'dealDamage') &&
    state.player.damageTakenLastEnemyPhase < 4
  ) {
    return false
  }
  if (card.def.effects.some((e) => e.effect === 'shatterBlockConvert')) {
    return state.enemies.some((e) => e.hp > 0 && e.block >= 3)
  }
  // 亡者の壁は消滅4枚以上でないと薄い
  if (card.def.effects.some((e) => e.effect === 'gainBlockPerExhaust')) {
    return state.player.exhaustPile.length >= 4
  }
  // 氷の槍は氷壁4以上、霊気の奔流は霊気2以上でないと空撃ち
  if (card.def.effects.some((e) => e.effect === 'dealDamagePerIceBlock')) {
    return state.player.iceBlock >= 4
  }
  if (
    card.def.effects.some((e) => e.effect === 'dischargeAetherDraw') &&
    state.player.aether < 2
  ) {
    return false
  }
  // 霊気放出系 (霊気の槍・満ちる霊気) は霊気2以上でないと換金損 (2026-08-31)
  if (card.def.effects.some((e) => e.effect === 'dischargeAether') && state.player.aether < 2) {
    return false
  }
  // 反復 (青): 手札に他のダメージ呪文がないとトークンが腐る (ターン終了で消えるため)
  if (
    card.def.effects.some((e) => e.effect === 'addSpellEcho') &&
    !state.player.hand.some(
      (c) =>
        c.uid !== card.uid && c.def.type === 'spell' && c.def.effects.some(isDamageEffect),
    )
  ) {
    return false
  }
  // 屍集め: 消滅置き場が空なら無意味。死者再生: 直接プレイできるコスト2以上のカードがないと損
  if (card.def.effects.some((e) => e.effect === 'retrieveFromExhaust')) {
    return state.player.exhaustPile.length > 0
  }
  if (card.def.effects.some((e) => e.effect === 'playFromExhaust')) {
    return pickDirectPlayTarget(state) !== null
  }
  const exhaustCostN = card.def.exhaustCost ?? 0
  if (exhaustCostN > 0 && state.player.hand.length - 1 < exhaustCostN) return false
  // ブロック変換はブロック4以上、集結・隊列は置物1体以上でないと空撃ち
  if (card.def.effects.some((e) => e.effect === 'dealDamagePerBlock')) {
    // 自前で積むブロックも算入する。壁を売り払う札 (spendBlock) はより厚い壁を要求する
    const selfBlock = card.def.effects
      .filter((e) => e.effect === 'gainBlock')
      .reduce((a, e) => a + (e.amount ?? 0), 0)
    const need = card.def.effects.some((e) => e.spendBlock) ? 8 : 4
    return state.player.block + selfBlock >= need
  }
  if (
    card.def.effects.some(
      (e) => e.effect === 'dealDamagePerPermanent' || e.effect === 'gainBlockPerPermanent',
    )
  ) {
    // 置物数参照はリーダーパッシブ・レリックを数えないので、ボットの空撃ち判定も揃える
    return state.player.permanents.filter((c) => c.innate !== true).length >= 1
  }
  // 回復はHPが減っていなければ無意味
  if (
    card.def.effects.every((e) => e.effect === 'gainHp') &&
    state.player.hp >= state.player.maxHp
  ) {
    return false
  }
  // ストーム系: 詠唱数0で撃っても無意味
  const stormEffects = ['dealDamagePerCardPlayed', 'gainIceBlockPerCardPlayed', 'drawCardsPerCardPlayed']
  if (
    card.def.effects.some((e) => stormEffects.includes(e.effect)) &&
    state.player.cardsPlayedThisTurn === 0
  ) {
    return false
  }
  // 霊気放出: 霊気3未満で撃つのはもったいない (単純ボットの閾値)
  if (card.def.effects.some((e) => e.effect === 'dischargeAether') && state.player.aether < 3) {
    return false
  }
  const discardCost = card.def.discardCost ?? 0
  if (discardCost > 0 && state.player.hand.length - 1 < discardCost) return false
  return true
}

/** 死者再生の直接プレイ対象: 制約 (リアクション・選択式・再利用カード以外) を満たす最高コスト。2E未満しかなければ損なので null */
function pickDirectPlayTarget(state: GameState): CardInstance | null {
  let best: CardInstance | null = null
  for (const c of state.player.exhaustPile) {
    if (c.def.type === 'reaction') continue
    if ((c.def.modes?.length ?? 0) > 0) continue
    if (c.def.effects.some((e) => e.effect === 'playFromExhaust' || e.effect === 'retrieveFromExhaust')) continue
    if (best === null || c.def.cost > best.def.cost) best = c
  }
  return best !== null && best.def.cost >= 2 ? best : null
}

/** PlayCard コマンドを組み立てる (選択式は先頭モード、捨てコストは手札の末尾から充当する単純方針) */
function buildPlayCommand(state: GameState, card: CardInstance): Command {
  const modeIndex = (card.def.modes?.length ?? 0) > 0 ? 0 : undefined
  const discardCost = card.def.discardCost ?? 0
  const discardUids =
    discardCost > 0
      ? state.player.hand
          .filter((c) => c.uid !== card.uid)
          .slice(-discardCost)
          .map((c) => c.uid)
      : undefined
  const exhaustCostN = card.def.exhaustCost ?? 0
  const exhaustUids =
    exhaustCostN > 0
      ? state.player.hand
          .filter((c) => c.uid !== card.uid)
          .slice(-exhaustCostN)
          .map((c) => c.uid)
      : undefined
  // コスト再利用 (黒): 屍集めは最高コスト、死者再生は制約を満たす最高コストを選ぶ
  let retrieveUid: string | undefined
  if (card.def.effects.some((e) => e.effect === 'retrieveFromExhaust')) {
    const best = [...state.player.exhaustPile].sort((a, b) => b.def.cost - a.def.cost)[0]
    retrieveUid = best?.uid
  }
  if (card.def.effects.some((e) => e.effect === 'playFromExhaust')) {
    retrieveUid = pickDirectPlayTarget(state)?.uid
  }
  // 引導 (黒 2026-08-31): 負傷・がらくた > 亡骸持ち (起爆) > 先頭、の順で消滅させる札を選ぶ
  let deckUids: string[] | undefined
  const deckChooseN = card.def.effects
    .filter((e) => e.effect === 'exhaustFromDeckChoose')
    .reduce((a, e) => a + (e.amount ?? 1), 0)
  if (deckChooseN > 0) {
    const rank = (c: (typeof state.player.drawPile)[number]): number =>
      c.def.id.startsWith('status_') ? 0 : c.def.effects.some((e) => e.trigger === 'onSelfExhausted') ? 1 : 2
    const pool = [...state.player.drawPile, ...state.player.discardPile].sort(
      (a, b) => rank(a) - rank(b),
    )
    deckUids = pool.slice(0, Math.min(deckChooseN, pool.length)).map((c) => c.uid)
  }
  // 回収 (捨て札) / サーチ (山札): 最もコストの高い非ステータス札を選ぶ (2026-09-02 緑のカード操作)
  const pickBest = (pool: readonly (typeof state.player.drawPile)[number][], n: number): string[] =>
    [...pool]
      .filter((c) => !c.def.id.startsWith('status_'))
      .sort((a, b) => b.def.cost - a.def.cost)
      .slice(0, n)
      .map((c) => c.uid)
  const retrieveN = card.def.effects.filter((e) => e.effect === 'retrieveFromDiscard').reduce((a, e) => a + (e.amount ?? 1), 0)
  if (retrieveN > 0) {
    const picked = pickBest(state.player.discardPile, retrieveN)
    const need = Math.min(retrieveN, state.player.discardPile.length)
    deckUids = picked.length >= need ? picked : state.player.discardPile.slice(0, need).map((c) => c.uid)
  }
  const searchN = card.def.effects.filter((e) => e.effect === 'searchDeck').reduce((a, e) => a + (e.amount ?? 1), 0)
  if (searchN > 0) {
    const picked = pickBest(state.player.drawPile, searchN)
    const need = Math.min(searchN, state.player.drawPile.length)
    deckUids = picked.length >= need ? picked : state.player.drawPile.slice(0, need).map((c) => c.uid)
  }
  // 手札で鍛える: 自身以外で最もコストの高い鍛えられる札
  let handUids: string[] | undefined
  const upgradeN = card.def.effects.filter((e) => e.effect === 'upgradeInHand').reduce((a, e) => a + (e.amount ?? 1), 0)
  if (upgradeN > 0) {
    const cands = state.player.hand.filter((c) => c.uid !== card.uid && canUpgradeCard(c)).sort((a, b) => b.def.cost - a.def.cost)
    handUids = cands.slice(0, Math.min(upgradeN, cands.length)).map((c) => c.uid)
  }
  // 集中砲火: 最低HPの生存敵を対象にする (確定済みルール表「ターゲティング」の単純ボット方針)
  let targetIndex: number | undefined
  let bestHp = Infinity
  for (let i = 0; i < state.enemies.length; i++) {
    const e = state.enemies[i]
    if (e.hp > 0 && e.hp < bestHp) {
      bestHp = e.hp
      targetIndex = i
    }
  }
  return { type: 'PlayCard', cardUid: card.uid, modeIndex, discardUids, exhaustUids, retrieveUid, deckUids, handUids, targetIndex }
}

/** 現在の戦闘状態に対するボットの次の一手 (単発戦闘・ラン共用の純関数) */
export function chooseCommand(s: GameState): Command {
  if (s.phase === 'awaiting-reaction') {
    if (s.reactionMode === 'set-confirm') return { type: 'ConfirmReaction', fire: true }
    const candidates = playableReactions(s)
    return candidates.length > 0
      ? { type: 'ReactManual', cardUid: candidates[0].uid }
      : { type: 'ConfirmReaction', fire: false }
  }
  if (s.phase !== 'player-turn') throw new Error(`ボットが手番でない: ${s.phase}`)

  // set系: まずリアクションを伏せる (伏せ枠が空いていて払えるなら常に。かすみは2枠)
  if (s.reactionMode !== 'hold-manual' && s.player.setCards.length < s.player.setSlots) {
    const reaction = s.player.hand.find(
      (c) => c.def.type === 'reaction' && c.def.cost <= s.player.energy,
    )
    if (reaction) return { type: 'SetCard', cardUid: reaction.uid }
  }

  // hold-manual: 敵ターンにリアクションを切るため、最安リアクション分のエナジーを温存する
  const reactionCosts = s.player.hand
    .filter((c) => c.def.type === 'reaction')
    .map((c) => c.def.cost)
  const reserve =
    s.reactionMode === 'hold-manual' && reactionCosts.length > 0 ? Math.min(...reactionCosts) : 0
  // 攻撃札が手札にあるなら最安攻撃分のエナジーを温存する
  // (ドロー・ミル系エンジンがエナジーを食い尽くして攻撃が一度も飛ばない病の防止。
  //  ストーム温存の一般化: 墓地型で顕在化した 2026-08-25)
  // 蓄積型ペイオフ (詠唱数/消滅数参照) があればその最安コストを、なければ最安攻撃札のコストを温存
  const burstCosts = s.player.hand
    .filter((c) =>
      c.def.effects.some((e) =>
        [
          'dealDamagePerCardPlayed',
          'dealDamagePerExhaust',
          'dealDamageDrainPerExhaust',
          'dealDamagePerSelfHpLost',
        ].includes(e.effect),
      ),
    )
    .map((c) => c.def.cost)
  const attackCosts = s.player.hand
    .filter((c) => isPlayableFromHand(c) && c.def.effects.some(isDamageEffect))
    .map((c) => c.def.cost)
  const payoffReserve =
    burstCosts.length > 0
      ? Math.min(...burstCosts)
      : attackCosts.length > 0
        ? Math.min(...attackCosts)
        : 0
  const spendable = s.player.energy - reserve

  // ダメージ札自身は温存分を使ってよい
  const isPayoff = (c: CardInstance) => c.def.effects.some(isDamageEffect)
  // 拘束 (2026-09-02 幕1新敵「巻きつく大蛇」の設計者が発見): 上限に達したら PlayCard/PlayNecro を出さない
  if (!(s.player.restrain > 0 && (s.player.playsThisTurn ?? 0) >= RESTRAIN_PLAY_CAP)) {
  for (const role of PLAY_PRIORITY) {
    let candidates = s.player.hand.filter((c) => {
      const budget = role === 'bighit' || isPayoff(c) ? spendable : spendable - payoffReserve
      return (
        botRole(c.def) === role &&
        isPlayableFromHand(c) &&
        effectiveCost(s, c) <= budget &&
        isWorthPlaying(s, c)
      )
    })
    // 勢い生成付きの攻撃 (突進の助走など) を同ターンの他の攻撃・大技より先に打つ
    if (role === 'bighit' || role === 'attack') {
      // isPlayableFromHand を必ず通す (2026-08-29 修正: 勢い+ダメージを持つリアクション
      // =跳ね返りの蔦をプレイしようとして落ちた。リアクションは伏せる札でプレイ不可)
      const momentumFirst = s.player.hand.filter(
        (c) =>
          isPlayableFromHand(c) &&
          c.def.effects.some((e) => e.effect === 'addMomentum') &&
          c.def.effects.some(isDamageEffect) &&
          effectiveCost(s, c) <= spendable,
      )
      if (momentumFirst.length > 0) candidates = momentumFirst
      // 急所付き攻撃 (打ち据え・急所突き) も同様に先に打つ = 後続の攻撃に+50%が乗る
      // (2026-08-29 テンポ再校正②。順番を間違えると乗算が空振りする=勢いと同じ分別)
      const exposeFirst = s.player.hand.filter(
        (c) =>
          isPlayableFromHand(c) &&
          c.def.effects.some((e) => e.effect === 'exposeEnemy') &&
          c.def.effects.some(isDamageEffect) &&
          effectiveCost(s, c) <= spendable,
      )
      if (momentumFirst.length === 0 && exposeFirst.length > 0) candidates = exposeFirst
    }
    // 成長カテゴリ内では addGrowth (年輪) を doubleGrowth (開花の儀) より先に
    const card =
      candidates.find((c) => c.def.effects.some((e) => e.effect === 'addGrowth')) ?? candidates[0]
    if (card) return buildPlayCommand(s, card)
  }
  // 亡骸プレイ (黒 2026-08-31): 手札を使い切ったら余りエナジーで消滅置き場の亡骸札を撃つ
  const necro = s.player.exhaustPile.find(
    (c) => c.def.necroCost !== undefined && c.def.necroCost <= s.player.energy - reserve,
  )
  if (necro) {
    const alive = s.enemies.map((e, i) => ({ e, i })).filter(({ e }) => e.hp > 0)
    const target =
      alive.length > 1 ? alive.reduce((a, b) => (b.e.hp < a.e.hp ? b : a)).i : undefined
    return { type: 'PlayNecro', cardUid: necro.uid, targetIndex: target }
  }
  } // 拘束ガード終わり
  return { type: 'EndTurn' }
}

interface BattleResult {
  readonly won: boolean
  readonly turns: number
  readonly triggered: number
  readonly whiffed: number
}

function runBattle(
  mode: ReactionMode,
  deckId: string,
  enemyId: string,
  seed: number,
  leaderId?: string,
): BattleResult {
  // leaderId を渡さないとリーダーパッシブが乗らない。天井デッキとドラフトを比較する時は
  // 必ず同じリーダーで揃えること (2026-08-26: 未指定だと比較が交絡するため引数を追加)
  let s = applyCommand(createInitialState(seed, mode), {
    type: 'StartCombat',
    seed,
    enemyId,
    deckId,
    leaderId,
  })
  let actions = 0
  while (s.phase !== 'won' && s.phase !== 'lost') {
    if (s.turn > TURN_LIMIT) break // 膠着 → 敗北扱い
    if (++actions > 3000) break // 1戦闘の行動数セーフガード (無限コンボの検知)
    s = applyCommand(s, chooseCommand(s))
  }
  return {
    won: s.phase === 'won',
    turns: Math.min(s.turn, TURN_LIMIT),
    triggered: s.eventLog.filter((e) => e.type === 'ReactionTriggered').length,
    whiffed: s.eventLog.filter((e) => e.type === 'ReactionWhiffed').length,
  }
}

// ============================================================
// モード1: ドラフト連戦の走破率 (npm run sim -- runs 200 42)
// ============================================================

/** ボットの報酬ピック: 攻撃系優先の単純方針 (カテゴリ優先順で最初に合致した提示を取る) */
const PICK_PRIORITY: readonly BotRole[] = [
  'permanent', // 置物は長いランで最も価値が高い (茨の茂み・賢者の泉など)
  'growth',
  'draw',
  'attack',
  'bighit',
  'defend',
  'ramp',
  'reaction',
]

function chooseReward(run: { rewardOptions: readonly string[] | null }): number {
  const options = run.rewardOptions ?? []
  for (const role of PICK_PRIORITY) {
    const idx = options.findIndex((id) => botRole(getCardDef(id)) === role)
    if (idx >= 0) return idx
  }
  return 0
}

function simulateRuns(count: number, baseSeed: number): void {
  console.error(`# ドラフト連戦 sim: ${count}ラン × ${allLeaders.length}リーダー, baseSeed=${baseSeed} (ピックはカテゴリ優先の単純方針)`)
  console.log('leader,runs,cleared,clearRate,avgBattlesCleared,avgFinalDeckSize')
  for (const leader of allLeaders) {
    const deathsByBattle = new Array<number>(BOSS_ROW + 1).fill(0)
    const deathsByEnemy = new Map<string, number>()
    let cleared = 0
    let totalBattlesCleared = 0
    let totalDeckSize = 0
    for (let i = 0; i < count; i++) {
      let run = createRun((baseSeed + i) >>> 0, 'set-confirm', leader.id)
      let aborted = false
      let actions = 0
      while (
        run.phase === 'combat' ||
        run.phase === 'reward' ||
        run.phase === 'map' ||
        run.phase === 'campfire' ||
        run.phase === 'workshop' ||
        run.phase === 'shop' ||
        run.phase === 'event' ||
        run.phase === 'relic-reward'
      ) {
        if (++actions > 30000) { aborted = true; break } // ラン全体の行動数セーフガード
        if (run.phase === 'map') {
          // ルートポリシー: HP60%未満なら焚き火優先 / それ以外はHP60%以上でエリート優先 → 戦闘
          const cands = nextChoices(run)
          const typeOf = (c: number) => run.map[run.row + 1][c].type
          const wantCamp = run.hp < run.maxHp * 0.6
          const wantElite = run.hp >= run.maxHp * 0.6
          const pick =
            (wantCamp ? cands.find((c) => typeOf(c) === 'campfire') : undefined) ??
            (wantElite ? cands.find((c) => typeOf(c) === 'elite') : undefined) ??
            cands.find((c) => typeOf(c) === 'battle' || typeOf(c) === 'boss') ??
            cands[0]
          run = applyRunCommand(run, { type: 'ChooseNode', col: pick })
          continue
        }
        if (run.phase === 'campfire') {
          // 回復は自動なので、常に「鍛える or 除去」を取りに行く (2026-08-26)。
          // 基本札が3枚以上あるうちは抜いて濃度を上げ、それ以降は伸びしろの大きい札を鍛える
          const basics = run.deck.filter(
            (c) => c.def.id.endsWith('_strike') || c.def.id.endsWith('_guard'),
          ).length
          const trimIdx =
            basics >= 3
              ? run.deck.findIndex((c) => c.def.id.endsWith('_strike') || c.def.id.endsWith('_guard'))
              : -1
          if (trimIdx >= 0 && run.deck.length > 5) {
            run = applyRunCommand(run, { type: 'CampfireRemove', index: trimIdx })
          } else {
            let best = -1
            let bestAmount = 0
            run.deck.forEach((c, i2) => {
              if (isUpgraded(c)) return
              const amt = c.def.effects.reduce((a, e) => a + (e.amount ?? 0), 0)
              if (amt > bestAmount) { bestAmount = amt; best = i2 }
            })
            run =
              best >= 0
                ? applyRunCommand(run, { type: 'CampfireUpgrade', index: best })
                : applyRunCommand(run, { type: 'CampfireRest' })
          }
          continue
        }
        if (run.phase === 'workshop') {
          run = applyRunCommand(run, { type: 'WorkshopSkip' }) // ボットは合成しない (判断が要るため)
          continue
        }
        if (run.phase === 'shop') {
          run = applyRunCommand(run, { type: 'ShopLeave' }) // ボットは買わない (判断が要るため)
          continue
        }
        if (run.phase === 'event') {
          // 規約: 最後の選択肢は常に安全な「立ち去る」(確定済みルール表「?マス（イベント）」)
          const ev = getEventDef(run.eventId!)
          run = applyRunCommand(run, { type: 'EventChoice', index: ev.choices.length - 1 })
          continue
        }
        if (run.phase === 'relic-reward') {
          run = applyRunCommand(run, { type: 'PickRelic', index: 0 })
          continue
        }
        if (run.phase === 'reward') {
          run = applyRunCommand(run, { type: 'PickReward', index: chooseReward(run) })
          continue
        }
        if (run.combat!.turn > TURN_LIMIT) {
          aborted = true // 膠着 → そこで敗北扱い
          break
        }
        run = applyRunCommand(run, { type: 'Combat', command: chooseCommand(run.combat!) })
      }
      const battlesCleared = run.battlesWon
      totalBattlesCleared += battlesCleared
      totalDeckSize += run.deck.length
      if (run.phase === 'won' && !aborted) cleared += 1
      else {
        deathsByBattle[Math.min(Math.max(run.row, 0), BOSS_ROW)] += 1
        // 死因の敵の集計 (2026-09-02 分散監視。StS2教訓「平均勝率でなく、どのデッキが
        // どの敵に詰むかの分散が炎上の火種」への一級指標化)
        const killer = run.combat?.enemies.map((e) => e.enemyId).join('+') ?? '(非戦闘)'
        deathsByEnemy.set(killer, (deathsByEnemy.get(killer) ?? 0) + 1)
      }
    }
    console.log(
      [
        leader.id,
        count,
        cleared,
        (cleared / count).toFixed(3),
        (totalBattlesCleared / count).toFixed(2),
        (totalDeckSize / count).toFixed(1),
      ].join(','),
    )
    console.error(
      `# ${leader.id} 敗北した戦闘の分布: ` +
        deathsByBattle.map((d, i) => (d > 0 ? `${i + 1}戦目:${d}` : null)).filter(Boolean).join(' '),
    )
    {
      const deaths = [...deathsByEnemy.entries()].sort((x, y) => y[1] - x[1])
      const totalDeaths = deaths.reduce((sum, [, n]) => sum + n, 0)
      if (totalDeaths > 0) {
        const top = deaths.slice(0, 5).map(([id, n]) => `${id}:${n}`).join(' ')
        const concentrated = deaths[0][1] / totalDeaths > 0.5 ? ' ⚠死因集中' : ''
        console.error(`# ${leader.id} 死因の敵 top5: ${top}${concentrated}`)
      }
    }
  }
}

// ============================================================
// モード2: 単発戦闘の総当たり (従来)
// ============================================================

interface Acc {
  battles: number
  wins: number
  turns: number
  triggered: number
  whiffed: number
}

function emptyAcc(): Acc {
  return { battles: 0, wins: 0, turns: 0, triggered: 0, whiffed: 0 }
}

function row(mode: string, deck: string, enemy: string, a: Acc): string {
  const reactions = a.triggered + a.whiffed
  return [
    mode,
    deck,
    enemy,
    a.battles,
    a.wins,
    (a.wins / a.battles).toFixed(3),
    (a.turns / a.battles).toFixed(2),
    reactions > 0 ? (a.triggered / reactions).toFixed(3) : '0.000',
    reactions > 0 ? (a.whiffed / reactions).toFixed(3) : '0.000',
  ].join(',')
}

function simulateBattles(battlesPerCell: number, baseSeed: number, compareModes: boolean): void {
  const MODES: readonly ReactionMode[] = compareModes
    ? ['set-auto', 'hold-manual', 'set-confirm']
    : ['set-confirm']
  const decks = allDecks.filter((d) => !d.id.startsWith('run_basic')) // ラン用基本デッキは対象外
  console.error(
    `# deck-rogue-proto sim: ${battlesPerCell}戦 × ${MODES.length}方式 × ${decks.length}デッキ × ${allEnemies.length}類型, baseSeed=${baseSeed}`,
  )
  console.error('# 発動率 = 発動 / (発動+空振り)。空振り = そのターン使われなかったリアクション')
  console.log('mode,deck,enemy,battles,wins,winRate,avgTurns,reactionFireRate,whiffRate')
  for (const mode of MODES) {
    for (const deck of decks) {
      const deckAcc = emptyAcc()
      for (const enemy of allEnemies) {
        const cell = emptyAcc()
        for (let b = 0; b < battlesPerCell; b++) {
          const r = runBattle(mode, deck.id, enemy.id, (baseSeed + b) >>> 0)
          cell.battles += 1
          cell.wins += r.won ? 1 : 0
          cell.turns += r.turns
          cell.triggered += r.triggered
          cell.whiffed += r.whiffed
        }
        console.log(row(mode, deck.id, enemy.id, cell))
        deckAcc.battles += cell.battles
        deckAcc.wins += cell.wins
        deckAcc.turns += cell.turns
        deckAcc.triggered += cell.triggered
        deckAcc.whiffed += cell.whiffed
      }
      console.log(row(mode, deck.id, 'ALL', deckAcc))
    }
  }
}

// ============================================================
// エントリポイント
// ============================================================

// import しただけでは走らないガード (デバッグ用にボット関数を re-export できるようにする)
if (process.argv[1]?.endsWith('sim/run.ts')) {
  if (process.argv[2] === 'runs') {
    simulateRuns(Number(process.argv[3] ?? 100), Number(process.argv[4] ?? 1))
  } else {
    simulateBattles(
      Number(process.argv[2] ?? 100),
      Number(process.argv[3] ?? 1),
      process.argv.includes('all'),
    )
  }
}
