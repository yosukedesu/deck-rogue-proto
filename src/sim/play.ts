// sim/play.ts — LLM/人間がテキストで1手ずつプレイするためのCLIハーネス
//
// 使い方 (状態はJSONファイルに保存され、1コマンド=1プロセスで進める):
//   npx tsx src/sim/play.ts new-run <leaderId> <seed> <stateFile> [deckId] [difficulty]  (deckId省略時はリーダー既定。difficulty=1〜10・省略時3=現状)
//   npx tsx src/sim/play.ts new-battle <deckId> <enemyId> <seed> <stateFile>
//   npx tsx src/sim/play.ts cmd <stateFile> '<コマンドJSON>'
//   npx tsx src/sim/play.ts show <stateFile>
//
// 状態ファイルにはリプレイ・ジャーナル (journal) が自動で記録される (2026-09-01)。
// ブラウザのタイトル画面「🎬 リプレイを読み込む」にこのファイルを渡すと、CLI/Opusランを
// 1手ずつ観戦でき、任意の地点から人間が操作を引き継げる
//
// コマンドJSON例:
//   {"type":"PlayCard","cardUid":"c12","targetIndex":0}
//   {"type":"SetCard","cardUid":"c3"} / {"type":"RetrieveSetCard","cardUid":"c3"} (1E) / {"type":"EndTurn"}
//   {"type":"ConfirmReaction","fire":true,"cardUid":"c3"} / {"type":"ConfirmReaction","fire":false}
//   ラン専用: {"type":"PickReward","index":0} / {"type":"SkipReward"}
//            {"type":"ChooseNode","col":0} (マップで次のノードを選ぶ) / {"type":"PickRelic","index":0} / {"type":"SkipRelic"}
//            {"type":"CampfireRest"} / {"type":"CampfireRemove","index":0} / {"type":"CampfireUpgrade","index":0}  ← 焚き火

import { readFileSync, writeFileSync } from 'node:fs'
import { encounterName, getCardDef, getEnemyDef, getEventDef, getLeaderDef, getRelicDef } from '../engine/content.ts'
import { fuseBlockReason, fuseCards, resolveFusedDef } from '../engine/fusion.ts'
import { canUpgradeInHand } from '../engine/upgrade.ts'
import { canSetAsNormal, setFireCost, setWindowStage } from '../engine/setany.ts'
import { canSetCard } from '../engine/reactions/set-base.ts'

/** 合成カード (fused_ / fusion_ 系ID) も引ける安全な名前解決 */
const INTENT_KIND_JA: Record<string, string> = { attack: '攻撃', defend: '防御', buff: '筋力上げ', rally: '応援', heal: '回復', hex: '状態異常', 'destroy-set': '伏せ破壊', 'destroy-token': '従者狩り', 'steal-gold': '盗み', flee: '逃走', mill: '山札喰い', rest: '隙', hatch: '孵化' }

function cname(cardId: string): string {
  try {
    return getCardDef(cardId).name
  } catch {
    return resolveFusedDef(cardId)?.name ?? cardId
  }
}
import { applyEnemyWeak, cardNeedsTarget, damageBreakdown, effectiveCost, effectiveIntent, isPlayableFromHand, playerCanSet, setBranchFlipRisks, setReactionIgnoresFreshness, usableSetCards, windowFromPending } from '../engine/effects.ts'
import { applyRunCommand, canUpgradeCard, createDebugCheckpointRun, createRun, currentNode, eventChoiceNeedsCard, nextChoices, shopRemovalPrice, shopUpgradePrice, upgradeCard, workshopFusePrice } from '../engine/run.ts'
import { battleSummary, cardCostLabel, enemyPunishesSet, relicRarityTag, setBranchNote, summaryLine, worstIncomingFrom, xHitsSuffix } from '../engine/summary.ts'
import { enemyTraitTags } from '../engine/traits.ts'
import { applyCommand, createInitialState } from '../engine/state.ts'
import type { CardDef, Command, DeclarativeEffect, GameState } from '../engine/types.ts'
import type { RunCommand, RunJournal, RunState } from '../engine/run.ts'

interface SaveFile {
  kind: 'run' | 'battle'
  run?: RunState
  battle?: GameState
  logIndex: number
  /** リプレイ・ジャーナル (2026-09-01)。ブラウザの🎬リプレイと同形式 = Opusランも観戦・引き継ぎできる */
  journal?: RunJournal
}

// ---- 効果の短文レンダラ (UIの簡易版) ----
function fx(e: DeclarativeEffect, holderType?: string): string {
  const a = e.amount ?? 0
  const all = e.target === 'all' ? '敵全体に' : ''
  // amountMax < amount は弱まる/止まる側の安全弁 (冒涜の祭壇=刻5でミル停止)。「強化」と書かない
  const th = e.exhaustThreshold !== undefined
    ? `〔忘却の刻${e.exhaustThreshold}: ${(e.amountMax ?? 0) < (e.amount ?? 0) ? (e.amountMax === 0 ? '以降は停止' : `${e.amountMax}に減少`) : `${e.amountMax}に増える`}〕`
    : ''
  const base: Record<string, string> = {
    dealDamage: `${all}${a}ダメージ${e.pierce === true ? '(貫通)' : ''}${e.growthMultiplier !== undefined ? `(成長が×${e.growthMultiplier}で乗る)` : ''}${xHitsSuffix(e)}`, dealDamagePerAttackPlayed: `${all}このターンにプレイした攻撃×${a}ダメ`, gainBlock: `ブロック${a}${xHitsSuffix(e)}`, gainIceBlock: `氷壁${a}(持ち越し)`,
    drawCards: `${a}ドロー`, gainEnergy: `一時マナ+${a}`, gainEnergyMax: `エナジー上限+${a}`,
    addGrowth: `成長+${a}`, doubleGrowth: '成長2倍', addMomentum: `勢い+${a}`,
    counter: `返し${a}`, negate: '打ち消し', addAether: `霊気+${a}`,
    dischargeAether: `${all}霊気×${a}ダメ(全消費)`, dischargeGrowth: `成長×${a}ダメ(全消費)`, dischargeGrowthBlock: `成長×${a}ブロック(全消費)`, dischargeBurn: `延焼×${a}ダメ(全消費)`, dischargeMomentumBurn: `勢い×${a}延焼(全消費)`, dischargeMomentumBlock: `勢い×${a}ブロック(全消費)`,
    applyBurn: `${all}延焼+${a}`, shatterBlock: '敵ブロック全破壊', shatterBlockConvert: '敵ブロック全破壊+破壊値ダメ',
    dealDamageRandom: `${all}${a}〜${e.amountMax}ロールダメ`, dealDamageExecute: `${a}ダメ(敵HP25%以下なら${e.amountMax})`,
    impulseDraw: `衝動${a}枚(このターン限り)`, loseHp: `自分HP-${a}`, discountNext: `次のカード-${a}`,
    confuse: `混乱+${a}`, exposeEnemy: `急所+${a}`, gainHp: `HP回復${a}`, weakenEnemy: `威圧${a}(次の${a}回の攻撃行動の与ダメ-25%)`,
    dealDamagePerBlock: `ブロック×${a}ダメ(急所は乗らない)${e.spendBlock === true ? '。解決後にブロックを全て失う' : ''}`, dealDamagePerPermanent: `${all}置物数×${a}ダメ`, gainBlockPerPermanent: `置物数×${a}ブロック`,
    dealDamageDrain: `${all}${a}ダメ+半分回復`, dealDamagePerCardPlayed: `${all}詠唱数×${a}ダメ`, dealDamagePerCardPlayedTotal: `${all}この戦闘の累計プレイ数×${a}ダメ`,
    gainIceBlockPerCardPlayed: `詠唱数×${a}氷壁`, drawCardsPerCardPlayed: `詠唱数×${a}ドロー`,
    strengthenEnemy: `敵の筋力+${a}`, dealDamagePerEnergyMax: `ターン開始時の上限×${a}ダメ`, gainBlockPerEnergyMax: `ターン開始時の上限×${a}ブロック`,
    dealDamagePerMomentum: `勢い×${a}ダメ(勢いは消費しない)`, doubleMomentum: '勢い2倍',
    gainSetSlot: `伏せ枠+${a}(この戦闘中)`, retrieveFromDiscard: `捨て札から${a}枚を選んで手札へ(要deckUids)`, searchDeck: `山札から${a}枚を選んで手札へ(要deckUids)`,
    addCopyToDiscard: `このカードのコピー${a}枚を捨て札へ`, growSelf: `プレイするたび、この札自身の与ダメ+${a}(この戦闘中。他の札には乗らない)`, upgradeInHand: `手札の${a}枚をこの戦闘中鍛える(要handUids)`,
    exhaustFromDeck: `山札の上${a}枚を消滅`, exhaustFromDeckChoose: `山札か捨て札から好きな${a}枚を選んで消滅(亡骸は発火。要deckUids)`, dealDamagePerExhaust: `${all}消滅数×${a}ダメ`,
    dealDamageDrainPerExhaust: `消滅数×${a}ダメ+半分回復`, gainBlockPerExhaust: `消滅数×${a}ブロック`,
    recycleExhaust: `消滅置き場を全て山札に還して混ぜ、還した枚数×${a}ダメ(刻・消滅数参照は0に戻る)`, dealDamagePerSelfHpLost: `失ったHP×${a}ダメ`, dealDamagePerHeal: `この戦闘で回復した回数×${a}ダメ(過剰回復も数える)`, dealDamagePerDamageTaken: `直前敵フェーズ被ダメ×${a}ダメ`,
    applyBurnPerDamageTaken: `直前敵フェーズ被ダメ×${a}延焼`, dealDamagePerRandomPlayed: `${all}この戦闘の運任せ札×${a}ダメ`,
    dealDamagePerIceBlock: `氷壁×${a}ダメ(氷壁は消費しない・急所は乗らない)`, negateConvertIce: '打ち消し+実値ぶん氷壁',
    dischargeAetherDraw: `霊気×${a}ドロー(全消費)`, dealDamageCleave: `${a}ダメ(倒せば別の敵にも同値)`,
    dealDamagePerHandCard: `${all}手札の枚数×${a}ダメ(自身は数えない)`, gainIceBlockPerHandCard: `手札の枚数×${a}氷壁`,
    addSpellEcho: `反復+${a}(次に唱える呪文の効果を2回解決。ターン終了時に消える。とげ反射も2回受ける)`, addCasts: `詠唱数+${a}(激昂タイマーには数えない)`, blessRetainers: `【常在】従者の効果+${a}`,
    addCardToHand: `${e.summonId ? getCardDef(e.summonId).name : ''}${a}枚を手札に加える(この戦闘限り)`, empowerShivs: `【常在】骨のナイフの与ダメ+${a}`,
    dealDamagePerNegStrength: `対象の威圧×${a}追加ダメ`, dealDamagePerWeak: `対象の威圧×${a}追加ダメ`, retrieveFromExhaust: '消滅置き場から1枚を手札へ(この戦闘中0E)',
    playFromExhaust: '消滅置き場から1枚を直接プレイ', summonPermanent: `${e.summonId ? getCardDef(e.summonId).name : ''}トークン${a}体を召喚`,
  }
  const trig: Record<string, string> = {
    // 置物文脈の onPlay は「登場時」— 無印だと持続効果に見える (2026-08-30 Opus緑ランの誤読対処)
    onPlay: holderType === 'permanent' ? '登場時:' : '', onAttackIncoming: '被攻撃前:', onAttacked: '被攻撃後:', onEnemyAction: '敵行動時:',
    onEnemyBuffed: '敵の筋力上げ時:', onEnemyDefended: '敵防御時:', onTurnStart: '毎T開始:', onCombatStart: '開幕:',
    onAttackPlayed: '攻撃プレイごと:', onGrowthGained: '成長獲得ごと:', onMomentumGained: '勢い獲得ごと:', onSpellPlayed: '呪文プレイごと:', onSetDestroyed: '伏せ破壊時:', onCardPlayed: 'カードプレイごと:', onBlockGained: 'ブロック獲得ごと:', onActionNegated: '打ち消し成功時:',
    onHealed: '回復ごと(満タンでも誘発):', onHpLost: 'HP損失ごと:', onCardExhausted: '消滅ごと:', onCostExhausted: '消滅コストごと:',
    onPermanentEntered: '置物登場ごと:', onImpulsePlayed: '衝動プレイごと:', onRandomPlayed: '運任せプレイごと:', onAetherGained: '霊気獲得ごと:',
    onCardSet: '伏せるごと:', onReactionFired: 'リアクション発動ごと:', onSelfExhausted: '亡骸(プレイ以外で消滅した時):',
  }
  const cond = e.condition
    ? `[${e.condition.hpAtOrBelowRatio !== undefined ? `HP${Math.round(e.condition.hpAtOrBelowRatio * 100)}%以下` : ''}${e.condition.minDamageTaken !== undefined ? `被ダメ${e.condition.minDamageTaken}以上` : ''}${e.condition.maxActionValue !== undefined ? `行動値${e.condition.maxActionValue}以下` : ''}${e.condition.minActionValue !== undefined ? `行動値${e.condition.minActionValue}以上` : ''}${e.condition.blaze === true ? '猛り火=延焼計8以上' : ''}${e.condition.minGrowth !== undefined ? `成長${e.condition.minGrowth}以上` : ''}${e.condition.enemyIntent !== undefined ? `対象の意図が${INTENT_KIND_JA[e.condition.enemyIntent] ?? e.condition.enemyIntent}なら` : ''}${e.condition.enemyExposed === true ? '対象が急所持ちなら' : ''}${e.condition.perfectBlockLastPhase === true ? '直前の敵フェーズを完全に凌いでいたら' : ''}${e.condition.targetDead === true ? 'とどめなら' : ''}${e.condition.lastActionNoHpLoss === true ? '完全に凌いだ時' : ''}]`
    : ''
  return `${trig[e.trigger] ?? e.trigger}${cond}${base[e.effect] ?? `${e.effect}${a || ''}`}${th}`
}

function cardLine(def: CardDef): string {
  const extras = [
    def.exhaust ? '消滅' : '',
    def.retain ? '保持(全捨てで手札に残る)' : '',
    def.discardCost ? `捨てコスト${def.discardCost}` : '',
    def.exhaustCost ? `消滅コスト${def.exhaustCost}` : '',
    def.necroCost !== undefined ? `💀亡骸プレイ${def.necroCost}E(消滅置き場から一度だけ)` : '',
    def.retainer ? '従者' : '',
  ].filter(Boolean).join('・')
  const body = def.modes?.length
    ? def.modes.map((m, i) => `選択${i}:${m.effects.map((e) => fx(e, def.type)).join('+')}`).join(' / ')
    : def.effects.map((e) => fx(e, def.type)).join('、')
  const costLabel = cardCostLabel(def)
  return `${def.name}(${costLabel}E/${def.type})${extras ? `【${extras}】` : ''} ${body}`
}

function branchText(it: { kind: string; shownMin: number; shownMax: number; hits?: number; mirrorHits?: boolean; inflict?: { status: string; amount: number }; alsoDefend?: number; alsoBuff?: number }, weak = 0): string {
  const hits = it.mirrorHits === true ? '×手数(このターンにプレイした枚数ぶん・最低1)' : (it.hits ?? 1) > 1 ? `×${it.hits}回(値は1発あたり)` : ''
  const inflict = it.inflict ? `+状態異常(${it.inflict.status}${it.inflict.amount})` : ''
  const guard = it.alsoDefend !== undefined ? `+防御${it.alsoDefend}` : ''
  const buff = it.alsoBuff !== undefined ? `+筋力${it.alsoBuff}` : ''
  const kinds: Record<string, string> = {
    attack: `攻撃${it.shownMin}〜${it.shownMax}${weak > 0 ? `→威圧で${applyEnemyWeak(it.shownMin, weak)}〜${applyEnemyWeak(it.shownMax, weak)}` : ''}${hits}${guard}${buff}`,
    defend: `防御${it.shownMin}〜${it.shownMax}${buff}`,
    'destroy-set': '伏せ破壊',
    'destroy-token': '従者狩り',
    buff: `筋力+${it.shownMin}〜${it.shownMax}`,
    rally: `応援+${it.shownMin}〜${it.shownMax}(味方全体)`,
    hex: '呪い',
    heal: `回復${it.shownMin}〜${it.shownMax}(最も傷んだ味方)`,
    'steal-gold': `盗み${it.shownMin}〜${it.shownMax}G`,
    flee: '逃走(倒すか打ち消せば阻止)',
    rest: '隙だらけ',
    hatch: '🐣孵化する(打ち消しで1ターン遅延可)',
    mill: `📖山札喰い${it.shownMin}〜${it.shownMax}枚(消滅置き場へ。亡骸は発火する)`,
  }
  return `${kinds[it.kind] ?? it.kind}${inflict}`
}

function intentLine(s: GameState, i: number): string {
  const e = s.enemies[i]
  if (!e.intent) return '---'
  // 条件付き意図: 両分岐を予告する (プレイヤーが自ターン中にどちらを選ばせるか決められる)
  if (e.intent.conditionalOn === 'set' && e.intent.alt && !playerCanSet(s)) {
    // 伏せられないデッキには到達不能な分岐を予告しない (2026-08-30)
    return branchText(e.intent, e.weak ?? 0) // branchText は素の値だけを読む
  }
  if (
    e.intent.conditionalOn &&
    e.intent.alt &&
    branchText(e.intent.alt, e.weak ?? 0) === branchText(e.intent, e.weak ?? 0) &&
    e.intent.alt.actual === e.intent.actual
  ) {
    // 表示も実値も同じ時だけ完全に畳む (2026-08-31 黒ラン: 幅が同じで実値だけ違う分岐を畳むと
    // 「伏せると実値が上がる」損分岐が不可視になっていた。実値が違えば分岐予告を残す)
    return branchText(e.intent, e.weak ?? 0)
  }
  if (
    e.intent.conditionalOn &&
    e.intent.alt &&
    branchText(e.intent.alt, e.weak ?? 0) === branchText(e.intent, e.weak ?? 0)
  ) {
    // 表示が同値で実値だけ違う: 2分岐の予告はノイズ (探り屋のローテ替え等) なので1行+注記。
    // どちら向きに変わるかは判断材料なので添える (2026-08-31 HP経済ラン指摘④)
    const dir = e.intent.alt.actual > e.intent.actual ? '上がる' : '下がる'
    // 罰型 (罠壊し等) や順番崩し (探り屋) は「今回たまたま同じ行動を引いた」だけ (2026-09-03 Opusラン K:
    // 「伏せると下がる」が旧弱腰型の文言に見えた)。別のターンは伏せ破壊や大技に化けることを添える
    const def = getEnemyDef(e.enemyId)
    const why = enemyPunishesSet(def)
      ? '。※罰型=ターンによって伏せ破壊や大技の分岐になる'
      : setBranchNote(def) ? `。※${setBranchNote(def)}` : ''
    return `${branchText(e.intent, e.weak ?? 0)}(伏せ札ありでも今回は同じ行動・実値は${dir}${why})`
  }
  if (e.intent.conditionalOn && e.intent.alt) {
    const note = e.intent.conditionalOn === 'set' ? setBranchNote(getEnemyDef(e.enemyId)) : null
    const cond = e.intent.conditionalOn === 'set' ? `伏せ札あり${note ? `(${note})` : ''}` : '従者あり'
    const now = effectiveIntent(s, i)!
    // 破壊分岐は見切り (setFresh) を無視して発動する既存則。汎用の「伏せ直せば変わる」を
    // 破壊分岐に出すと嘘になる (2026-08-31 HP経済ラン指摘①: 伏せ場の「敵は反応しない」と矛盾表示)
    const staleNow =
      e.intent.conditionalOn === 'set' &&
      s.player.setCards.length > 0 &&
      s.player.setCards.every((c) => c.setFresh !== true)
    const stale = !staleNow
      ? ''
      : e.intent.alt.kind === 'destroy-set'
        ? ' (破壊分岐は見切りを無視する=置きっぱなしでも壊しに来る)'
        : setReactionIgnoresFreshness(s, i)
          ? ' (この敵は罰型=見切りを無視する。伏せ札がある限りこの分岐)'
          : ' (伏せ札は見切られ中=まだ伏せたことのない別の札を1E以上で伏せれば変わる。同じ札の伏せ直しは見切られたまま)'
    return `【${cond}】${branchText(e.intent.alt, e.weak ?? 0)} ／【なし】${branchText(e.intent, e.weak ?? 0)} → 今は「${branchText(now, e.weak ?? 0)}」${stale}`
  }
  const it = e.intent
  const hits =
    it.mirrorHits === true
      ? `×手数(あなたが今ターンプレイした枚数+伏せた枚数ぶん。現在${Math.max(1, s.player.cardsPlayedThisTurn + (s.player.setsThisTurn ?? 0))}${s.player.cardsPlayedThisTurn + (s.player.setsThisTurn ?? 0) === 0 ? '=最低値' : ''})`
      : (it.hits ?? 1) > 1
        ? `×${it.hits}回`
        : ''
  const inflict = it.inflict ? `+状態異常(${it.inflict.status}${it.inflict.amount})` : ''
  const guard = it.alsoDefend !== undefined ? `+防御${it.alsoDefend}` : ''
  const kinds: Record<string, string> = {
    attack: `攻撃${it.shownMin}〜${it.shownMax}${hits ? (it.mirrorHits === true ? hits : `${hits}(値は1発あたり)`) : ''}${guard}`, defend: `防御${it.shownMin}〜${it.shownMax}`,
    'destroy-set': '伏せ破壊', 'destroy-token': '従者狩り', buff: `筋力+${it.shownMin}〜${it.shownMax}`,
    rally: `応援+${it.shownMin}〜${it.shownMax}(味方全体)`, hex: '呪い',
    heal: `回復${it.shownMin}〜${it.shownMax}(最も傷んだ味方)`, 'steal-gold': `盗み${it.shownMin}〜${it.shownMax}G`, mill: `📖山札喰い${it.shownMin}〜${it.shownMax}枚(消滅)`,
    flee: '逃走(倒すか打ち消せば阻止)', rest: '隙だらけ', hatch: '🐣孵化する(打ち消しで1ターン遅延可)',
  }
  return `${kinds[it.kind] ?? it.kind}${inflict}`
}

function renderBattle(s: GameState, logFrom: number): string {
  const p = s.player
  const L: string[] = []
  // 直近のイベントログ
  const events = s.eventLog.slice(logFrom)
  if (events.length > 0) {
    L.push('--- 直近の出来事 ---')
    for (const e of events) {
      if (e.type === 'DamageDealt') L.push(` ${e.source === 'player' ? '与ダメ' : '被ダメ'}${e.amount}(HP損失${'hpLoss' in e ? e.hpLoss : '?'})${e.armorCut ? `【装甲で${e.armorCut}切り捨て=本来${e.amount + e.armorCut}】` : ''}${e.burrowCut ? `【潜伏の殻で${e.burrowCut}を捨てた】` : ''}${e.nemesisCut ? `【無形で${e.nemesisCut}消滅=1固定】` : ''}${e.turnArmorCut ? `【ターン装甲で${e.turnArmorCut}切り捨て】` : ''}`)
      else if (e.type === 'CardPlayed') L.push(` プレイ:${cname(e.cardId)}`)
      else if (e.type === 'CardSet') L.push(` 伏せた:${cname(e.cardId)}`)
      else if (e.type === 'ReactionTriggered') L.push(` リアクション発動:${cname(e.cardId)}`)
      else if (e.type === 'CardExhausted') L.push(` 消滅:${cname(e.cardId)}`)
      else if (e.type === 'CardsMilled') L.push(` 忘却${e.count}枚→消滅置き場: ${(e.cardIds ?? []).map(cname).join('・')}`)
      else if (e.type === 'NecroFired') L.push(` 💀亡骸発火:${cname(e.cardId)}`)
      else if (e.type === 'NecroPlayed') L.push(` 💀亡骸プレイ:${cname(e.cardId)}(ゲームから消えた)`)
      else if (e.type === 'SpellEchoed') L.push(` 🔁反復:${cname(e.cardId)}の効果が2回解決`)
      else if (e.type === 'TokenDestroyed') L.push(` 従者狩り:${cname(e.cardId)}が倒された`)
      else if (e.type === 'SetCardDestroyed') L.push(` 伏せ破壊:${cname(e.cardId)}が壊された`)
      else if (e.type === 'TurnStarted') L.push(` === ターン${e.turn} ===`)
      else if (e.type === 'HpHealed') L.push(e.amount > 0 ? ` 回復${e.amount}` : ' 回復0(満タン。onHealedは誘発)')
      else if (e.type === 'HpLost') L.push(` 自傷${e.amount}`)
      else if (e.type === 'StatusInflicted') L.push(` 状態異常:${e.status}${e.amount}`)
      else if (e.type === 'CombatEnded') L.push(` ★戦闘${e.result === 'won' ? '勝利' : '敗北'}★`)
      else if (e.type === 'ThornsReflected') L.push(` 🦔とげ反射${e.amount}(HP損失${e.hpLoss}。ブロックで吸収した分は損失に出ない)`)
      else if (e.type === 'EnemySplit') L.push(` 🫠分裂! 倒した敵から${e.count}体が現れた`)
      else if (e.type === 'EnemyHatched') L.push(' 🐣孵化した!')
      else if (e.type === 'GuardianRedirected') L.push(' 🛡️庇われた! 単体対象は護衛に向かった')
      else if (e.type === 'ArtifactBlocked') L.push(' 🔮アーティファクトがデバフを弾いた(チャージ-1)')
      else if (e.type === 'BurrowBroken') L.push(' 🪺潜伏の殻が割れた! 次の行動は噛みつきに差し替わる')
      else if (e.type === 'EnemyWoken') L.push(' 👁️目を覚ました! 眠りの前奏が打ち切られた')
      else if (e.type === 'GoldStolen') L.push(` 💰${e.amount}G盗まれた(逃がす前に倒せば取り返す)`)
      else if (e.type === 'EnemyFled') L.push(` 🏃敵${e.enemyIndex}が逃走した`)
      else if (e.type === 'EnemyHealed') L.push(` 💚敵${e.enemyIndex}が敵${e.targetIndex}を回復+${e.amount}`)
      else if (e.type === 'ExhaustRecycled') L.push(` ♻️輪廻: 消滅置き場${e.count}枚が山札へ還った`)
    }
  }
  L.push(`--- 盤面 (ターン${s.turn} / phase=${s.phase}) ---`)
  const st = [
    `HP ${Math.max(0, p.hp)}/${p.maxHp}`, `ブロック${p.block}`, p.iceBlock ? `氷壁${p.iceBlock}` : '',
    `エナジー${p.energy}/${p.energyMax}`, p.growth ? `成長${p.growth}` : '', p.momentum ? `勢い${p.momentum}` : '',
    p.aether ? `霊気${p.aether}` : '', p.spellEchoes ? `反復${p.spellEchoes}` : '', p.nextCardDiscount ? `次-${p.nextCardDiscount}` : '',
    `消滅置き場${p.exhaustPile.length}枚`, p.weak ? `弱体${p.weak}` : '', p.vulnerable ? `脆弱${p.vulnerable}` : '', p.frail ? `虚弱${p.frail}(カードのブロック25%減)` : '', p.restrain ? `拘束${p.restrain}(1ターン3枚まで・このターンあと${Math.max(0, 3 - (p.playsThisTurn ?? 0))}枚)` : '', (p.mist ?? 0) ? `霞み${p.mist}(ドロー-2)` : '', (p.slow ?? 0) ? `重り${p.slow}(被ダメ+10%×プレイ枚数。今+${(p.playsThisTurn ?? 0) * 10}%)` : '',
    p.selfHpLost ? `自傷累計${p.selfHpLost}` : '', p.damageTakenLastEnemyPhase ? `直前被ダメ${p.damageTakenLastEnemyPhase}` : '',
    // 運任せカウンタは参照札 (×N換金/onRandomPlayed) を持つデッキでだけ意味を持つ — ノイズ抑制
    p.randomPlayedThisCombat && [...p.hand, ...p.drawPile, ...p.discardPile, ...p.setCards, ...p.permanents].some((c) => c.def.effects.some((e) => e.effect === 'dealDamagePerRandomPlayed' || e.trigger === 'onRandomPlayed')) ? `運任せ札${p.randomPlayedThisCombat}枚` : '',
    p.energyMaxAtTurnStart !== undefined && p.energyMaxAtTurnStart !== p.energyMax ? `上限参照は${p.energyMaxAtTurnStart}を読む(今ターンのランプは次Tから)` : '',
    `山札${p.drawPile.length}/捨て札${p.discardPile.length}`,
  ].filter(Boolean).join(' | ')
  L.push(`自分: ${st}`)
  // 予測被ダメ (最悪値): 複数体の同時攻撃を暗算しなくて済むように総量を出す
  let worst = 0
  s.enemies.forEach((e, i) => {
    void e
    worst += worstIncomingFrom(s, i) // 式は engine/summary.ts に1本化 (2026-09-02)
  })
  {
    // 0でも行を出す (2026-09-02 Opusラン: 非攻撃ターンに行ごと消えると「表示漏れ」と迷う)
    const defense = p.block + p.iceBlock
    const through = Math.max(0, worst - defense)
    L.push(
      `⚠️ 今フェーズの最悪被ダメ予測: ${worst}（現在の防御 ${defense} → 貫通 ${through} / HP ${p.hp}）`,
    )
  }
  s.enemies.forEach((e, i) => {
    if (e.hp <= 0) { L.push(`敵${i}: ${getEnemyDef(e.enemyId).name} ${e.fled ? `🏃逃走済み${e.stolenGold ? `(${e.stolenGold}G持ち逃げ)` : ''}` : '💀撃破済み'}`); return }
    const def = getEnemyDef(e.enemyId)
    const tags = [
      e.block ? `ブロック${e.block}` : '', e.strength ? `筋力${e.strength > 0 ? '+' : ''}${e.strength}` : '',
      e.burn ? `延焼${e.burn}` : '', e.confusion ? `混乱${e.confusion}` : '', e.exposed ? `急所${e.exposed}` : '', (e.weak ?? 0) > 0 ? `威圧${e.weak}(次の${e.weak}回の攻撃-25%)` : '',
      ...enemyTraitTags(s, i),
      e.stolenGold ? `💰${e.stolenGold}G抱え込み(逃す前に倒せば取り返す)` : '',
    ].filter(Boolean).join(' ')
    L.push(`敵${i}: ${def.name} HP${Math.max(0, e.hp)}/${e.maxHp} ${tags} → 意図: ${intentLine(s, i)}`)
  })
  // 消滅置き場・亡骸は伏せの有無と無関係に出す (旧実装は伏せ条件の if に巻き込まれていた)
  if (p.exhaustPile.length > 0) {
    L.push(`消滅置き場(${p.exhaustPile.length}枚): ${p.exhaustPile.map((c) => c.def.name).join('・')}`)
  }
  const necroList = p.exhaustPile.filter((c) => c.def.necroCost !== undefined)
  if (necroList.length > 0) {
    L.push(`亡骸プレイ可(消滅置き場): ${necroList.map((c) => `[${c.uid}] ${c.def.name}(${c.def.necroCost}E)`).join(' / ')} ※{"type":"PlayNecro","cardUid":"..."} 一度きり・ゲームから消える`)
  }
  // 引導 (exhaustFromDeckChoose): 手札に選択消滅札がある時だけ候補を出す。山札は名前順=引き順は伏せたまま
  const hasFx = (k: string) => p.hand.some((c) => c.def.effects.some((e) => e.effect === k))
  const drawList = () =>
    [...p.drawPile].sort((a, b) => a.def.name.localeCompare(b.def.name, 'ja')).map((c) => `[${c.uid}]${c.def.name}(山)`)
  const discList = () => p.discardPile.map((c) => `[${c.uid}]${c.def.name}(捨)`)
  if (hasFx('exhaustFromDeckChoose')) {
    L.push(`引導の選択候補(deckUids): ${[...drawList(), ...discList()].join(' ') || 'なし'} ※山札は名前順表示`)
  }
  // 緑のカード操作 (2026-09-02): 回収=捨て札から / サーチ=山札から / 手札で鍛える=自身以外の鍛えられる手札
  if (hasFx('retrieveFromDiscard')) L.push(`回収の選択候補(deckUids・捨て札): ${discList().join(' ') || 'なし'}`)
  if (hasFx('searchDeck')) L.push(`サーチの選択候補(deckUids・山札): ${drawList().join(' ') || 'なし'} ※名前順表示`)
  if (hasFx('upgradeInHand')) {
    const src = p.hand.filter((c) => c.def.effects.some((e) => e.effect === 'upgradeInHand')).map((c) => c.uid)
    const cands = p.hand.filter((c) => !src.includes(c.uid) && canUpgradeInHand(c)).map((c) => `[${c.uid}]${c.def.name}`)
    L.push(`手札で鍛える候補(handUids): ${cands.join(' ') || 'なし(省略可)'}`)
  }
  // 罰型 (見切り無視) の敵が生存中なら「敵は反応しない」は嘘になる (2026-09-03 Opusラン I 指摘)
  // 静的判定 (敵定義) にする: 動的判定だと確認ウィンドウで意図が確定した後に「反応しない」へ戻り矛盾した (Opusラン J)
  const stalePun = s.enemies
    .filter((en) => en.hp > 0 && enemyPunishesSet(getEnemyDef(en.enemyId)))
    .map((en) => getEnemyDef(en.enemyId).name)
  const staleTag =
    stalePun.length > 0
      ? '【見切られ中。ただし罰型の' + stalePun.join('・') + 'は伏せ札がある限り反応する。破壊は来る】'
      : '【見切られ=敵は反応しない。破壊は来る】'
  if (p.setCards.length > 0 || p.setSlots > 1) {
    L.push(`伏せ場(${p.setCards.length}/${p.setSlots}): ${p.setCards.map((c) => `[${c.uid}] ${cardLine(c.def)}${c.def.type !== 'reaction' ? `【通常札: 被攻撃${setWindowStage(c.def) === 'pre' ? '前' : '後'}に解決・発動に${setFireCost(c)}E】` : ''}${c.setFresh === true ? '' : staleTag}`).join(' / ') || 'なし'}${p.setCards.length > 0 ? ' ※回収={"type":"RetrieveSetCard","cardUid":"..."} (1E)' : ''}`)
  }
  if (p.permanents.length > 0) {
    // アンセム (blessRetainers): 従者の量つき効果は解決時に+Nされる。表示にも現在値を出す (2026-08-31)
    const anthem = p.permanents.reduce((a, c) => a + c.def.effects.filter((e) => e.effect === 'blessRetainers').reduce((x, e) => x + (e.amount ?? 0), 0), 0)
    L.push(`置物: ${p.permanents.map((c) => `${c.def.name}${c.token ? '(トークン)' : ''}(${c.def.effects.map((e) => fx(e, 'permanent')).join('、')})${anthem > 0 && c.def.retainer === true ? `【アンセム+${anthem}=量つき効果に加算】` : ''}`).join(' / ')}`)
    if (anthem > 0) L.push(`✨アンセム合計+${anthem} (従者の量つき効果すべてに加算)`)
  }
  if (s.phase === 'awaiting-reaction' && s.pendingWindow) {
    const enemy = s.enemies[s.pendingWindow.enemyIndex]
    // 条件付き意図の解決後の分岐を表示する (素の intent を出すと実値が幅表示と食い違う)
    const it = effectiveIntent(s, s.pendingWindow.enemyIndex)
    L.push(`!! 確認ウィンドウ (${s.pendingWindow.stage === 'pre' ? '行動実行前' : '行動解決後'}): ${getEnemyDef(enemy.enemyId).name}の「${it ? branchText(it) : '---'}」実値=${it?.actual}${(it?.hits ?? 1) > 1 ? `×${it?.hits}回` : ''}`)
    const win = windowFromPending(s)
    const cands = win ? usableSetCards(s, win) : []
    L.push(`   発動候補: ${cands.map((c) => `[${c.uid}] ${c.def.name}${setFireCost(c) > 0 ? `(発動${setFireCost(c)}E・残${p.energy}E)` : ''}`).join(' / ') || 'なし'}`)
    // post窓の誤認防止 (2026-08-29 検証ラン: 瀕死時に返し札を「防御」と誤認して発動→敗死の報告)。
    // 文言は攻撃窓のみ (2026-08-31 再検証ラン指摘②: 敵強化時の窓に「被弾は取り消せない」が出ていた)
    if (s.pendingWindow.stage === 'post') {
      L.push(
        it?.kind === 'attack'
          ? '   ※この攻撃はすでに解決済み——発動しても今回の被弾は取り消せない (返し・回復のための窓)'
          : '   ※この行動はすでに解決済み (行動に反応するための窓)',
      )
    }
    // 後続の敵の条件付き分岐が「伏せなし」側に化ける警告 (2026-08-28)
    for (const ri of setBranchFlipRisks(s)) {
      const rEnemy = s.enemies[ri]
      const it = rEnemy.intent!
      const threat = (k: string, mx: number, h?: number) => (k === 'attack' ? mx * (h ?? 1) : 0)
      const after = threat(it.kind, it.shownMax, it.hits)
      const before = it.alt ? threat(it.alt.kind, it.alt.shownMax, it.alt.hits) : after
      // 攻撃同士の比較のみ方向を出す (2026-08-31 白ラン: 応援+2を「弱くなる=利得」と誤表示)
      const comparable = it.kind === 'attack' && it.alt?.kind === 'attack'
      const mark = !comparable ? '⚠' : after < before ? '💡(弱くなる=利得)' : after > before ? '⚠(強くなる)' : '⚠'
      L.push(`   ${mark} 発動すると伏せ枠が空く: ${getEnemyDef(rEnemy.enemyId).name}の行動が【伏せなし】分岐 (${branchText(it)}) に変わる`)
    }
    L.push(`   → {"type":"ConfirmReaction","fire":true,"cardUid":"..."} か {"type":"ConfirmReaction","fire":false} (温存)`)
  }
  if (s.phase === 'player-turn') {
    L.push('手札:')
    for (const c of p.hand) {
      const cost = effectiveCost(s, c)
      const playable = isPlayableFromHand(c) && cost <= p.energy
      const settable = c.def.type === 'reaction' || (s.setAnyCards === true && canSetAsNormal(c.def))
      const canSet = settable && canSetCard(s, c.uid)
      const marks = [
        c.def.id.startsWith('status_') // 負傷・がらくた・火傷・烙印・仮初の烙印 (2026-09-02 Opusラン: 火傷が「エナジー不足」と誤表示)
          ? c.def.id === 'status_scald'
            ? '使用不可(死に札)・自ターン終了時に手札にあるとHP-2'
            : c.def.id === 'status_brand' || c.def.id === 'status_guilt'
              ? '使用不可(死に札)・自ターン終了時に手札にあるとHP-1'
              : '使用不可(死に札)'
          : playable
            ? 'プレイ可'
            : c.def.type === 'reaction'
              ? ''
              : 'エナジー不足',
        c.def.exhaustCost ? '要exhaustUids' : '',
        c.def.discardCost ? '要discardUids' : '',
        c.def.effects.some((e) => e.effect === 'retrieveFromExhaust' || e.effect === 'playFromExhaust')
          ? '要retrieveUid'
          : '',
        c.def.effects.some((e) => e.effect === 'exhaustFromDeckChoose') &&
        p.drawPile.length + p.discardPile.length > 0
          ? '要deckUids(下の選択候補から)'
          : '',
        c.def.effects.some((e) => e.effect === 'retrieveFromDiscard') && p.discardPile.length > 0 ? '要deckUids(捨て札から)' : '',
        c.def.effects.some((e) => e.effect === 'searchDeck') && p.drawPile.length > 0 ? '要deckUids(山札から)' : '',
        c.def.effects.some((e) => e.effect === 'upgradeInHand') &&
        p.hand.some((h) => h.uid !== c.uid && canUpgradeInHand(h))
          ? '要handUids(下の候補から)'
          : '',
        canSet
          ? (c.def.type !== 'reaction' ? `伏せ可(1E・発動時に${c.def.cost}E)` : '伏せ可')
          : c.def.type === 'reaction'
            ? p.setCards.length >= p.setSlots
              ? '伏せ枠が満杯(回収{"type":"RetrieveSetCard"}で空く)'
              : c.def.cost > p.energy
                ? '伏せるエナジー不足'
                : ''
            : '',
        cardNeedsTarget(c) && s.enemies.filter((e) => e.hp > 0).length > 1 ? '要targetIndex' : '',
        p.impulseUids.includes(c.uid) ? '衝動(このターン限り)' : '',
      ].filter(Boolean).join('・')
      const xEff = c.def.xCost === true ? c.def.effects.filter((e) => e.xHits === true && e.effect === 'dealDamage') : []
      const xCap = p.energy
      // 全部払った時の見積り: engine の damageBreakdown で敵ごとの1ヒット実値 (成長・勢い・弱体・急所・装甲・敵ブロック) ×X
      // (2026-09-03 Opusラン I: 旧表示は成長・勢いしか乗せておらず急所×1.5が抜けていた)
      const xNow =
        xEff.length > 0
          ? (() => {
              const per = s.enemies
                .map((en, ei) => ({ en, ei }))
                .filter((x) => x.en.hp > 0)
                .map((x) => {
                  const hit = xEff.reduce((a, e) => a + (damageBreakdown(s, x.ei, e.amount ?? 0, e.pierce === true)?.hpLoss ?? 0), 0)
                  return `敵${x.ei}:${hit}×${xCap}=${hit * xCap}`
                })
              return ` ［X=1〜${xCap}を xAmount で指定 (省略=全部)。全部なら ${per.join(' / ')}${xEff.some((e) => e.target === 'all') ? '(全体)' : '(単体=対象1体)'}。先頭ヒット基準=急所・敵ブロックは1ヒット目にだけ乗る］`
            })()
          : ''
      // 印字コストと実コストが違う時だけ注記 (2026-09-03 Opusラン G: 重圧で2E消費なのに「1E」表示のまま手順を組んで滑った)
      const costNote =
        c.def.xCost === true || cost === c.def.cost
          ? ''
          : ` ⚠実コスト${cost}E(印字${c.def.cost}E${cost > c.def.cost ? '・重圧' : '・割引/無料'})`
      // 上限参照はターン開始時のスナップショットを読む (T1は素の上限)。その場の実値を出す (同ラン指摘②)
      const capEff = c.def.effects.filter((e) => e.effect === 'dealDamagePerEnergyMax' || e.effect === 'gainBlockPerEnergyMax')
      const capNow =
        capEff.length > 0
          ? ` ［上限参照=今${p.energyMaxAtTurnStart}: ${capEff
              .map((e) => {
                const base = (e.amount ?? 0) * p.energyMaxAtTurnStart
                if (e.effect !== 'dealDamagePerEnergyMax') return `ブロック${base}`
                // 弱体・成長・急所・装甲・敵ブロックを実処理と同じ手順で通す (2026-09-03 Opusラン H: 注記が弱体を通していなかった)
                const per = s.enemies
                  .map((en, i) => ({ en, i, b: damageBreakdown(s, i, base, e.pierce === true) }))
                  .filter((x) => x.b !== null)
                  .map((x) => `敵${x.i}:${x.b!.hpLoss}${x.b!.steps.length > 1 ? `(${x.b!.steps.slice(1).map((st) => st.label).join('・')})` : ''}`)
                return `ダメ基礎${base}${per.length > 0 ? ` → ${per.join(' / ')}` : ''}`
              })
              .join(' ／ ')}］`
          : ''
      // 素のダメージ札の実値注記 (2026-09-03 Opusラン L: 弱体1+成長5で巨象の突進が27になり「貫通のバグ」と誤読)。
      // 補正 (成長・勢い・弱体・急所・装甲・潜伏・因縁・敵ブロック) が1つでも掛かる時だけ、先頭のダメージ効果の敵ごとの実値を出す
      const plainDmg = c.def.xCost === true ? undefined : c.def.effects.find((e) => e.trigger === 'onPlay' && e.effect === 'dealDamage' && e.amount !== undefined)
      const dmgNow = (() => {
        if (!plainDmg) return ''
        const base = (plainDmg.amount ?? 0) + (plainDmg.growthMultiplier !== undefined ? p.growth * (plainDmg.growthMultiplier - 1) : 0)
        const per = s.enemies
          // 粉砕を持つ札は自分の粉砕で敵ブロックが消えてからダメージが入る (2026-09-04 Opusラン M: 蔦の楔が常に0表示)
          .map((en, ei) => ({ en, ei, b: damageBreakdown(s, ei, base, plainDmg.pierce === true || c.def.effects.some((e) => e.effect === 'shatterBlock')) }))
          .filter((x) => x.b !== null && x.b!.steps.length > 1)
          .map((x) => `敵${x.ei}:${x.b!.hpLoss}(${x.b!.steps.slice(1).map((st) => st.label).join('・')})`)
        return per.length > 0 ? ` ［実値: ${per.join(' / ')}${c.def.effects.filter((e) => e.effect === 'dealDamage').length > 1 ? '。先頭ヒット基準' : ''}］` : ''
      })()
      L.push(` [${c.uid}] ${cardLine(c.def)}${costNote} 〈${marks || 'プレイ不可'}〉${xNow}${capNow}${dmgNow}`)
    }
  }
  if (s.phase === 'won') L.push(`★★ 勝利 ★★  ⚔️ 戦いの記録: ${summaryLine(battleSummary(s.eventLog))}`)
  if (s.phase === 'lost') L.push('★★ 敗北 ★★')
  return L.join('\n')
}

const NODE_ICON: Record<string, string> = {
  battle: '⚔', elite: '👑', campfire: '🔥', workshop: '🔨', shop: '🛒', event: '❓', treasure: '🎁', boss: '💀',
}

/** マップ全体をテキスト描画 (全体可視・現在地と次の選択肢を明示) */
/** 現在地から到達可能なノード集合 (row -> Set<col>)。開始前は行0の全ノードから前向きに広げる */
/** 指定ノードから先の到達可能集合 (選択肢ごとの内訳表示用) */
function reachableFrom(run: RunState, row0: number, col0: number): Set<string> {
  const out = new Set<string>()
  let frontier: number[] = [col0]
  let row = row0
  while (row < run.map.length && frontier.length > 0) {
    const next = new Set<number>()
    for (const c of frontier) {
      out.add(`${row}:${c}`)
      for (const to of run.map[row][c].next) next.add(to)
    }
    frontier = [...next]
    row++
  }
  return out
}

function reachableSet(run: RunState): Set<string> {
  const out = new Set<string>()
  let frontier: number[] = run.row < 0 ? run.map[0].map((_, c) => c) : [...(currentNode(run)?.next ?? [])]
  let row = run.row < 0 ? 0 : run.row + 1
  while (row < run.map.length && frontier.length > 0) {
    const next = new Set<number>()
    for (const c of frontier) {
      out.add(`${row}:${c}`)
      for (const to of run.map[row][c].next) next.add(to)
    }
    frontier = [...next]
    row++
  }
  return out
}

const NODE_LABEL: Record<string, string> = { campfire: '焚き火', workshop: '工房', shop: 'ショップ', event: '?', treasure: '宝箱' }

/**
 * 簡易マップ (既定。トークン節約のため近傍だけ表示):
 * 現在地の次の3行 + この先の特別ノードの行要約。全図は `show <file> full`
 */
function renderMapBrief(run: RunState): string {
  const L: string[] = []
  const cands = nextChoices(run)
  const reach = reachableSet(run)
  L.push(`🗺 マップ簡易表示 (幕${run.act}/3。全図は show <file> full)`)
  const from = run.row + 1
  for (let r = Math.min(run.map.length - 1, from + 2); r >= Math.max(0, from); r--) {
    const cells = run.map[r].map((n, c) => {
      const label = n.encounterId !== null ? `${NODE_ICON[n.type]}${encounterName(n.encounterId)}` : `${NODE_ICON[n.type]}${NODE_LABEL[n.type] ?? n.type}`
      const edges = n.next.length > 0 ? `→${n.next.join('·')}` : ''
      const unreachable = r > run.row && !reach.has(`${r}:${c}`) ? '(到達不可)' : ''
      const choice = r === run.row + 1 && cands.includes(c) ? `←選べる[col:${c}]` : ''
      return `[${c}]${label}${edges}${unreachable}${choice}`
    })
    L.push(` ${r === from ? '→' : '  '}行${String(r + 1).padStart(2)}: ${cells.join(' | ')}`)
  }
  // この先の特別ノード要約 (計画用の最小情報)
  const ahead: string[] = []
  const kinds: [string, string][] = [['campfire','🔥'],['shop','🛒'],['workshop','🔨'],['event','❓'],['elite','👑'],['boss','💀']]
  for (const [t, icon] of kinds) {
    const rows: number[] = []
    run.map.forEach((row, r) => {
      if (r > run.row && row.some((n, c) => n.type === t && reach.has(`${r}:${c}`))) rows.push(r)
    })
    if (rows.length > 0) ahead.push(`${icon}行${rows.map((x) => x + 1).join(',')}`)
  }
  L.push(`   この先(いずれかの経路で到達可能): ${ahead.join(' ') || 'なし'}`)
  // 選択肢ごとの内訳 (2026-08-30 Opusテスターの指摘: 「この先(到達可能)」を「これから行ける」と
  // 読んでゴールド系レリックを取った直後、選んだ枝でショップへの経路が消え421Gが死んだ)。
  // どの枝を選ぶと何が残るかを、選ぶ前に見せる
  for (const c of cands) {
    const sub = reachableFrom(run, run.row + 1, c)
    const marks = kinds
      .filter(([t]) => [...sub].some((k) => {
        const [r2, c2] = k.split(':').map(Number)
        return run.map[r2][c2].type === t
      }))
      .map(([, icon]) => icon)
    L.push(`   [col:${c}]を選ぶと → ${marks.join('') || '戦闘のみ'}`)
  }
  L.push('→ {"type":"ChooseNode","col":N} で「←選べる」のノードへ進む')
  return L.join('\n')
}

function renderMap(run: RunState): string {
  const L: string[] = []
  const cands = nextChoices(run)
  const reach = reachableSet(run)
  L.push('🗺 マップ (下から上へ。全体もエッジ(→接続先col)も最初から見える。エリート👑=固有ギミックの専用敵〔素の値・補正なし〕、勝てばレリック3択+レア1枚確定)')
  L.push('   ※現在地から到達できないノードは (到達不可) 付き。接続は前の行でどの列を選んだかで決まる')
  for (let r = run.map.length - 1; r >= 0; r--) {
    const cells = run.map[r].map((n, c) => {
      // ?マスの中身 (eventId) は入るまで伏せる (確定済みルール表「?マス（イベント）」)
      const typeLabel: Record<string, string> = { campfire: '焚き火', workshop: '工房', shop: 'ショップ', event: '?', treasure: '宝箱(レリック3択)' }
      const label = n.encounterId !== null ? `${NODE_ICON[n.type]}${encounterName(n.encounterId)}` : `${NODE_ICON[n.type]}${typeLabel[n.type] ?? n.type}`
      const edges = n.next.length > 0 ? `→${n.next.join('·')}` : ''
      const here = r === run.row && c === run.col ? '【現在地】' : ''
      const unreachable = !here && r > run.row && !reach.has(`${r}:${c}`) ? '(到達不可)' : ''
      const choice = r === run.row + 1 && cands.includes(c) ? `←選べる[col:${c}]` : ''
      return `[${c}]${label}${edges}${here}${unreachable}${choice}`
    })
    const mark = r === run.row + 1 ? '→' : '  '
    L.push(` ${mark}行${String(r + 1).padStart(2)}: ${cells.join(' | ')}`)
  }
  L.push('→ {"type":"ChooseNode","col":N} で「←選べる」のノードへ進む')
  return L.join('\n')
}

function renderRun(run: RunState, logFrom: number, fullMap = false): string {
  const L: string[] = []
  const leader = getLeaderDef(run.leaderId)
  // 盗まれ中の額をヘッダに出す (2026-08-30 白ラン指摘「今いくら残っているか分からない」)
  // 倒した盗人 (逃走前) の抱えた金は勝利時に戻るので「盗まれ中」に数えない (2026-08-31 再検証ラン指摘①)
  const stolenNow = run.phase === 'combat' ? (run.combat?.enemies.reduce((a, e) => a + (e.hp > 0 || e.fled === true ? (e.stolenGold ?? 0) : 0), 0) ?? 0) : 0 // 精算後の残留表示を防ぐ (2026-08-31 白ラン指摘)
  L.push(`=== ラン: ${leader.name}${run.setAnyCards === true ? ' | 🃏全カード伏せ可(実験)' : ''} | 難易度${run.difficulty ?? 3} | 幕${run.act}/3 ${run.row < 0 ? '開始前' : `行${run.row + 1}/${run.map.length}`} | 戦闘${run.battlesWon}勝 | HP持ち越し${run.hp} | 💰${run.gold}G${stolenNow > 0 ? `(うち${stolenNow}G盗まれ中・実損は所持${run.gold}Gが上限)` : ''} | フェーズ:${run.phase} | レリック:${run.relics.map((r) => getRelicDef(r).name).join('、') || 'なし'} ===`)
  if (run.phase === 'combat' && run.combat) {
    L.push(renderBattle(run.combat, logFrom))
  } else if (run.phase === 'reward' && run.rewardOptions) {
    if (run.combat?.phase === 'won') L.push('🏆 戦闘に勝利 (残りの手札は打てない)')
    {
      // 逃走した盗人の持ち逃げ額 (2026-09-03 Opusラン K: 「56G盗まれた→55G」の並びが取り返せたように見えた)
      const lost = (run.combat?.enemies ?? []).filter((e) => e.fled === true).reduce((a, e) => a + (e.stolenGold ?? 0), 0)
      if (lost > 0) L.push(`💸 逃走した盗人に ${lost}G 持ち逃げされた (所持金から精算済み。逃げる前に倒せば戻っていた)`)
    }
    if (run.combat?.phase === 'won') L.push(`⚔️ 戦いの記録: ${summaryLine(battleSummary(run.combat.eventLog))}`)
    L.push('報酬ピック (1枚選ぶ or スキップ):')
    if (run.currentElite && run.combat?.enemies.some((e) => e.fled === true)) {
      L.push('⚠ 逃走されたため、エリートのレア確定枠を失った (レリック3択は残る)')
    }
    const RARITY_TAG: Record<string, string> = { common: '', uncommon: '◆', rare: '★レア ' }
    run.rewardOptions.forEach((id, i) => {
      const def = getCardDef(id)
      L.push(` [${i}] ${RARITY_TAG[def.rarity ?? 'common']}${cardLine(def)}`)
    })
    L.push('→ {"type":"PickReward","index":N} か {"type":"SkipReward"}')
  } else if (run.phase === 'map') {
    L.push(fullMap ? renderMap(run) : renderMapBrief(run))
  } else if (run.phase === 'campfire') {
    L.push(`🔥 焚き火: 「休む/鍛える/除去」から1つ選ぶ (排他三択。現在 ${run.hp}/${run.maxHp})`)
    if ((run.campfireForgeBonus ?? 0) > 0) {
      const forgeLeft = Math.max(0, 1 + (run.campfireForgeBonus ?? 0) - (run.campfireUpgradesUsed ?? 0))
      if (forgeLeft > 0) L.push(`  🪨鍛冶の砥石: 鍛えるはあと${forgeLeft}枚 (休む・除去とは併用不可)`)
    }
    // 上限クランプ後の実回復量を表示する (2026-08-30 Opusラン指摘: 満タンでも+41と出ていた)
    const heal = Math.min(Math.floor(run.maxHp * run.campfireRatio), run.maxHp - run.hp)
    const noRest = run.relics.some((id) => getRelicDef(id).bonus?.noRest === true)
    L.push(
      noRest ? '  休む (CampfireRest) → 休めない (古根の杯)。回復なしの立ち去り'
      : (run.campfireUpgradesUsed ?? 0) > 0 ? '  休む (CampfireRest) → 鍛えた後なので回復なしの立ち去り (1種類の原則)'
      : heal <= 0 ? '  休む (CampfireRest) → HP満タンなので回復なしの立ち去り'
      : `  休む (CampfireRest) → HP+${heal} 回復して次へ`)
    // 鍛えるが使えない焚き火 (幕1のラン通算1回を使用済み等) では強化UIを丸ごと畳む
    // (2026-08-31 再検証ラン指摘④「残り0と書いてあるのに全カードの鍛えるプレビューが並ぶ」)
    const forgeLeftHere = Math.max(0, 1 + (run.campfireForgeBonus ?? 0) - (run.campfireUpgradesUsed ?? 0))
    L.push(
      forgeLeftHere > 0
        ? '  強化 (CampfireUpgrade) → デッキの1枚を鍛える (量の効果が+50%。同じ札は1回だけ)'
        : '  強化 (CampfireUpgrade) はこの焚き火では使えない (使用済み)',
    )
    L.push('  除去はショップのみ (2026-09-03 焚き火の「取り除く」は廃止。休む/鍛えるの二択)')
    run.deck.forEach((c, i) => {
      const mark =
        forgeLeftHere <= 0 ? '' : canUpgradeCard(c) ? ` → 鍛えると: ${cardLine(upgradeCard(c).def)}` : ' 【鍛えられない】'
      L.push(`   [${i}] ${cardLine(c.def)}${mark}`)
    })
    L.push(`→ ${forgeLeftHere > 0 ? '{"type":"CampfireUpgrade","index":N} / ' : ''}{"type":"CampfireRest"}(休む=回復して次へ)`)
  } else if (run.phase === 'shop' && run.shop) {
    L.push(`🛒 ショップ (所持 ${run.gold}G。買わずに出てもよい)`)
    // レア表記は報酬ピックと同じ (2026-08-31 検証ラン指摘: 6枠目がレア確定枠だと分からない)
    const SHOP_RARITY: Record<string, string> = { common: '', uncommon: '◆', rare: '★レア ' }
    run.shop.cards.forEach((item, i) => L.push(item.sold === true ? ` [${i}] 〔売切〕` : ` [${i}] ${item.price}G: ${SHOP_RARITY[getCardDef(item.id).rarity ?? 'common']}${cardLine(getCardDef(item.id))}`))
    if (run.shop.relicId !== null) {
      const r = getRelicDef(run.shop.relicId)
      L.push(` レリック ${run.shop.relicPrice}G: ${relicRarityTag(r) ? `${relicRarityTag(r)} ` : ''}${r.name} (${r.description})`)
    }
    L.push(` カード除去サービス ${shopRemovalPrice(run)}G (回数無制限・使うたび+50G)`)
    L.push(` カード強化サービス ${shopUpgradePrice(run)}G (回数無制限・使うたび+50G。焚き火の「鍛える」と同じ)`)
    L.push(`→ {"type":"ShopBuyCard","index":N} / {"type":"ShopBuyRelic"} / {"type":"ShopRemove","index":N}(デッキ番号) / {"type":"ShopUpgrade","index":N}(デッキ番号) / {"type":"ShopLeave"}`)
    L.push('   デッキ:')
    run.deck.forEach((c, i) => L.push(`   [${i}] ${cardLine(c.def)}`))
  } else if (run.phase === 'event') {
    const ev = getEventDef(run.eventId!)
    L.push(`❓ ${ev.sprite ?? ''} ${ev.name}`)
    L.push(`   ${ev.flavor}`)
    ev.choices.forEach((c, i) => {
      const locked = c.requireGold !== undefined && run.gold < c.requireGold ? ' 【G不足で選べない】' : ''
      const needCard = eventChoiceNeedsCard(c) ? ' 【要cardIndex(デッキ番号)】' : ''
      L.push(` [${i}] ${c.label}${locked}${needCard}`)
    })
    L.push('→ {"type":"EventChoice","index":N} (対象カードが要る選択肢は {"type":"EventChoice","index":N,"cardIndex":M})')
    L.push('   デッキ:')
    run.deck.forEach((c, i) => L.push(`   [${i}] ${cardLine(c.def)}`))
  } else if (run.phase === 'workshop') {
    L.push(`🔨 工房 (合成1回 ${workshopFusePrice(run)}G・所持 ${run.gold}G${run.gold < workshopFusePrice(run) ? '=ゴールド不足で合成不可' : ''})`)
    L.push('🔨 工房: デッキの2枚を合成して1枚の新カードにできる (同名2枚は「真・」強化版。素材は消える)。見送りも可')
    L.push(
      '   タイプ跨ぎも可: 結果は持続する側 (置物＞リアクション＞呪文＞物理)。置物化は量÷3で毎ターン化',
    )
    run.deck.forEach((c, i) => L.push(`   [${i}] ${cardLine(c.def)}`))
    L.push('→ {"type":"WorkshopFuse","indexA":N,"indexB":M} か {"type":"WorkshopSkip"}')
    L.push('   確定前の確認: {"type":"FusePreview","indexA":N,"indexB":M} (状態を変えずに結果を表示)')
    L.push('   (同じ色同士。同名2枚は「真・」化=2枚ぶんを圧縮した強化版。コストはVP査定からの逆算=素材コストの単純合算ではない)')
    L.push('   特定の組み合わせは手書きレシピ(⭐)にヒットし、計算値より少し強い一品になる')
  } else if (run.phase === 'relic-reward' && run.relicOptions) {
    if (run.combat?.phase === 'won') L.push(`⚔️ 戦いの記録: ${summaryLine(battleSummary(run.combat.eventLog))}`)
    L.push('レリック報酬 (1つ選ぶ or スキップ):')
    run.relicOptions.forEach((id, i) => {
      const def = getRelicDef(id)
      L.push(` [${i}] ${relicRarityTag(def) ? `${relicRarityTag(def)} ` : ''}${def.name}: ${def.description}`)
    })
    L.push('→ {"type":"PickRelic","index":N} か {"type":"SkipRelic"}')
  } else if (run.phase === 'won') L.push('★★★ ラン走破！ ★★★')
  else if (run.phase === 'lost') L.push('★★★ ラン敗北 ★★★')
  return L.join('\n')
}

// ---- CLI ----
function load(file: string): SaveFile {
  return JSON.parse(readFileSync(file, 'utf-8')) as SaveFile
}
function save(file: string, data: SaveFile): void {
  writeFileSync(file, JSON.stringify(data))
}
function currentLogLength(sf: SaveFile): number {
  const combat = sf.kind === 'run' ? sf.run?.combat : sf.battle
  return combat?.eventLog.length ?? 0
}

const [, , mode, ...args] = process.argv
if (mode === 'new-run') {
  const [leaderId, seed, file, deckId, difficulty] = args
  // フラグ: 6番目以降に 'reveal' (実値常時表示) / 'set-any' (全カード伏せ可の実験) を任意の順で置ける
  const flags = new Set(process.argv.slice(8))
  const runOpts = {
    ...(flags.has('reveal') ? { revealIntents: true } : {}),
    ...(flags.has('set-any') ? { setAnyCards: true } : {}),
  }
  const run = createRun(Number(seed), 'set-confirm', leaderId, deckId || undefined, difficulty ? Number(difficulty) : undefined, runOpts)
  const journal: RunJournal = {
    origin: {
      kind: 'run',
      seed: Number(seed),
      leaderId,
      ...(deckId ? { deckId } : {}),
      ...(difficulty ? { difficulty: Number(difficulty) } : {}),
      ...runOpts,
    },
    commands: [],
  }
  const sf: SaveFile = { kind: 'run', run, logIndex: 0, journal }
  save(file, sf)
  console.log(renderRun(run, 0))
} else if (mode === 'new-checkpoint') {
  // チェックポイント開始 (2026-09-02): 幕2/3から代表デッキ+レリックで開始。UIの🚩と同じ createDebugCheckpointRun。
  // 使い方: new-checkpoint <leaderId> <seed> <file> <act> <deckId> [hpRatio] [gold] [difficulty] [relicIds(カンマ区切り)]
  const [leaderId, seed, file, act, deckId, hpRatio, gold, difficulty, relicCsv] = args
  const checkpoint = {
    act: Number(act),
    deckId,
    ...(relicCsv ? { relicIds: relicCsv.split(',').filter(Boolean) } : {}),
    ...(hpRatio ? { hpRatio: Number(hpRatio) } : {}),
    ...(gold ? { gold: Number(gold) } : {}),
    ...(difficulty ? { difficulty: Number(difficulty) } : {}),
  }
  const run = createDebugCheckpointRun(Number(seed), 'set-confirm', leaderId, checkpoint)
  const journal: RunJournal = { origin: { kind: 'checkpoint', seed: Number(seed), leaderId, checkpoint }, commands: [] }
  const sf: SaveFile = { kind: 'run', run, logIndex: 0, journal }
  save(file, sf)
  console.log(renderRun(run, 0))
} else if (mode === 'new-battle') {
  const [deckId, enemyId, seed, file] = args
  let s = createInitialState(Number(seed), 'set-confirm')
  s = applyCommand(s, { type: 'StartCombat', seed: Number(seed), enemyId, deckId })
  const sf: SaveFile = { kind: 'battle', battle: s, logIndex: 0 }
  save(file, sf)
  console.log(renderBattle(s, 0))
} else if (mode === 'cmd') {
  const [file, json] = args
  const sf = load(file)
  const cmd = JSON.parse(json) as Command | RunCommand
  const logFrom = sf.logIndex
  // 合成プレビュー (ハーネス限定・状態は変更しない): 確定前にコスト・消滅・効果を確認できる
  if ((cmd as { type: string }).type === 'FusePreview' && sf.kind === 'run') {
    const c = cmd as unknown as { indexA: number; indexB: number }
    const a = sf.run!.deck[c.indexA]
    const b = sf.run!.deck[c.indexB]
    if (!a || !b) {
      console.log('不正な添字')
      process.exit(1)
    } else {
      const reason = fuseBlockReason(a, b)
      if (reason !== null) {
        console.log(`合成不可: ${reason}`)
      } else {
        const def = fuseCards(a, b)
        const recipe = def.id.startsWith('fusion_') ? '⭐レシピ発見! ' : ''
        console.log(`プレビュー: ${recipe}${cardLine(def)} (素材は消費されていない)`)
        if (sf.run!.gold < workshopFusePrice(sf.run!)) console.log(`※所持金不足: 合成${workshopFusePrice(sf.run!)}G / 所持${sf.run!.gold}G (確定は拒否される)`)
        console.log('  ※コストはVP査定からの逆算 (素材コストの単純合算ではない)')
      }
    }
    process.exit(0)
  }
  if (sf.kind === 'run') {
    // 戦闘コマンドは自動で Combat に包む (エルゴノミクス)
    const runCmd: RunCommand =
      ['PickReward', 'SkipReward', 'ChooseNode', 'PickRelic', 'SkipRelic', 'StartRun', 'ShopBuyCard', 'ShopBuyRelic', 'ShopRemove', 'ShopUpgrade', 'ShopLeave', 'EventChoice',
        'CampfireRest', 'CampfireRemove', 'CampfireUpgrade', 'WorkshopFuse', 'WorkshopSkip'].includes(cmd.type)
        ? (cmd as RunCommand)
        : { type: 'Combat', command: cmd as Command }
    try {
      sf.run = applyRunCommand(sf.run!, runCmd)
      // リプレイ記録 (成功したコマンドだけ。旧ファイル=journal無しは記録しない)
      if (sf.journal !== undefined) sf.journal = { ...sf.journal, commands: [...sf.journal.commands, runCmd] }
    } catch (err) {
      // 不正なコマンドはスタックトレースでなく1行のエラーで返し、exit 1 にする
      // (2026-08-31 検証ラン指摘: exit 0 だとスクリプト/LLMがエラーを検知できず計算がずれる)
      console.log(`エラー: ${err instanceof Error ? err.message : String(err)}`)
      process.exit(1)
    }
    sf.logIndex = currentLogLength(sf)
    save(file, sf)
    console.log(renderRun(sf.run, logFrom))
  } else {
    try {
      sf.battle = applyCommand(sf.battle!, cmd as Command)
    } catch (err) {
      console.log(`エラー: ${err instanceof Error ? err.message : String(err)}`)
      process.exit(1)
    }
    sf.logIndex = currentLogLength(sf)
    save(file, sf)
    console.log(renderBattle(sf.battle, logFrom))
  }
} else if (mode === 'show') {
  // show <file> [full] — full でマップ全図 (既定は簡易=トークン節約)
  const [file, opt] = args
  const sf = load(file)
  // 直近10件のログだけ出す (全ログだと確認ウィンドウが画面外に流れてしまう)
  const tail = (g: GameState | undefined) => Math.max(0, (g?.eventLog.length ?? 0) - 10)
  console.log(
    sf.kind === 'run'
      ? renderRun(sf.run!, tail(sf.run!.combat ?? undefined), opt === 'full')
      : renderBattle(sf.battle!, tail(sf.battle)),
  )
} else {
  console.log('usage: play.ts new-run <leaderId> <seed> <file> [deckId] [difficulty] | new-checkpoint <leaderId> <seed> <file> <act> <deckId> [hpRatio] [gold] [difficulty] [relicIds] | new-battle <deckId> <enemyId> <seed> <file> | cmd <file> <json> | show <file> [full]')
}
