// sim/play.ts — LLM/人間がテキストで1手ずつプレイするためのCLIハーネス
//
// 使い方 (状態はJSONファイルに保存され、1コマンド=1プロセスで進める):
//   npx tsx src/sim/play.ts new-run <leaderId> <seed> <stateFile> [deckId]  (deckId省略時はリーダー既定。このは: run_basic=大樹の道 / run_trample=荒角の道)
//   npx tsx src/sim/play.ts new-battle <deckId> <enemyId> <seed> <stateFile>
//   npx tsx src/sim/play.ts cmd <stateFile> '<コマンドJSON>'
//   npx tsx src/sim/play.ts show <stateFile>
//
// コマンドJSON例:
//   {"type":"PlayCard","cardUid":"c12","targetIndex":0}
//   {"type":"SetCard","cardUid":"c3"} / {"type":"EndTurn"}
//   {"type":"ConfirmReaction","fire":true,"cardUid":"c3"} / {"type":"ConfirmReaction","fire":false}
//   ラン専用: {"type":"PickReward","index":0} / {"type":"SkipReward"}
//            {"type":"ChooseNode","col":0} (マップで次のノードを選ぶ) / {"type":"PickRelic","index":0} / {"type":"SkipRelic"}
//            {"type":"CampfireRest"} / {"type":"CampfireRemove","index":0} / {"type":"CampfireUpgrade","index":0}  ← 焚き火

import { readFileSync, writeFileSync } from 'node:fs'
import { encounterName, getCardDef, getEnemyDef, getEventDef, getLeaderDef, getRelicDef } from '../engine/content.ts'
import { fuseBlockReason, fuseCards, resolveFusedDef } from '../engine/fusion.ts'

/** 合成カード (fused_ / fusion_ 系ID) も引ける安全な名前解決 */
function cname(cardId: string): string {
  try {
    return getCardDef(cardId).name
  } catch {
    return resolveFusedDef(cardId)?.name ?? cardId
  }
}
import {
  cardNeedsTarget,
  effectiveCost,
  effectiveIntent,
  isPlayableFromHand,
  reactionMatches,
  setBranchFlipRisks,
  windowFromPending,
} from '../engine/effects.ts'
import { applyRunCommand, canUpgradeCard, createRun, currentNode, nextChoices, shopRemovalPrice, shopUpgradePrice, upgradeCard } from '../engine/run.ts'
import { battleSummary, summaryLine } from '../engine/summary.ts'
import { applyCommand, createInitialState } from '../engine/state.ts'
import type { CardDef, Command, DeclarativeEffect, GameState } from '../engine/types.ts'
import type { RunCommand, RunState } from '../engine/run.ts'

interface SaveFile {
  kind: 'run' | 'battle'
  run?: RunState
  battle?: GameState
  logIndex: number
}

// ---- 効果の短文レンダラ (UIの簡易版) ----
function fx(e: DeclarativeEffect): string {
  const a = e.amount ?? 0
  const all = e.target === 'all' ? '敵全体に' : ''
  const th = e.exhaustThreshold !== undefined ? `〔忘却の刻${e.exhaustThreshold}: ${e.amountMax}に強化〕` : ''
  const base: Record<string, string> = {
    dealDamage: `${all}${a}ダメージ`, gainBlock: `ブロック${a}`, gainIceBlock: `氷壁${a}(持ち越し)`,
    drawCards: `${a}ドロー`, gainEnergy: `一時マナ+${a}`, gainEnergyMax: `エナジー上限+${a}`,
    addGrowth: `成長+${a}`, doubleGrowth: '成長2倍', addMomentum: `勢い+${a}`,
    counter: `返し${a}`, negate: '打ち消し', addAether: `霊気+${a}`,
    dischargeAether: `霊気×${a}ダメ(全消費)`, dischargeGrowth: `成長×${a}ダメ(全消費)`, dischargeBurn: `延焼×${a}ダメ(全消費)`,
    applyBurn: `${all}延焼+${a}`, shatterBlock: '敵ブロック全破壊', shatterBlockConvert: '敵ブロック全破壊+破壊値ダメ',
    dealDamageRandom: `${all}${a}〜${e.amountMax}ロールダメ`, dealDamageExecute: `${a}ダメ(敵HP25%以下なら${e.amountMax})`,
    impulseDraw: `衝動${a}枚(このターン限り)`, loseHp: `自分HP-${a}`, discountNext: `次のカード-${a}`,
    confuse: `混乱+${a}`, exposeEnemy: `急所+${a}`, gainHp: `HP回復${a}`, weakenEnemy: `威圧${a}(敵強化-${a})`,
    dealDamagePerBlock: `ブロック×${a}ダメ`, dealDamagePerPermanent: `${all}置物数×${a}ダメ`,
    dealDamageDrain: `${all}${a}ダメ+半分回復`, dealDamagePerCardPlayed: `${all}詠唱数×${a}ダメ`,
    gainIceBlockPerCardPlayed: `詠唱数×${a}氷壁`, drawCardsPerCardPlayed: `詠唱数×${a}ドロー`,
    dealDamagePerEnergyMax: `上限×${a}ダメ`, gainBlockPerEnergyMax: `上限×${a}ブロック`,
    dealDamagePerMomentum: `勢い×${a}ダメ(勢いは消費しない)`, doubleMomentum: '勢い2倍',
    exhaustFromDeck: `山札の上${a}枚を消滅`, dealDamagePerExhaust: `消滅数×${a}ダメ`,
    dealDamageDrainPerExhaust: `消滅数×${a}ダメ+半分回復`, gainBlockPerExhaust: `消滅数×${a}ブロック`,
    dealDamagePerSelfHpLost: `失ったHP×${a}ダメ`, dealDamagePerDamageTaken: `直前敵フェーズ被ダメ×${a}ダメ`,
    dealDamagePerIceBlock: `氷壁×${a}ダメ`, negateConvertIce: '打ち消し+実値ぶん氷壁',
    dischargeAetherDraw: `霊気×${a}ドロー(全消費)`, dealDamageCleave: `${a}ダメ(倒せば別の敵にも同値)`,
    dealDamagePerNegStrength: `下げた敵強化×${a}追加ダメ`, retrieveFromExhaust: '消滅置き場から1枚を手札へ',
    playFromExhaust: '消滅置き場から1枚を直接プレイ', summonPermanent: `${e.summonId ? getCardDef(e.summonId).name : ''}トークン${a}体を召喚`,
  }
  const trig: Record<string, string> = {
    onPlay: '', onAttackIncoming: '被攻撃前:', onAttacked: '被攻撃後:', onEnemyAction: '敵行動時:',
    onEnemyBuffed: '敵強化時:', onEnemyDefended: '敵防御時:', onTurnStart: '毎T開始:', onCombatStart: '開幕:',
    onAttackPlayed: '攻撃プレイごと:', onSpellPlayed: '呪文プレイごと:', onSetDestroyed: '伏せ破壊時:', onCardPlayed: 'カードプレイごと:', onBlockGained: 'ブロック獲得ごと:', onActionNegated: '打ち消し成功時:',
    onHealed: '回復ごと:', onHpLost: 'HP損失ごと:', onCardExhausted: '消滅ごと:', onCostExhausted: '消滅コストごと:',
    onPermanentEntered: '置物登場ごと:', onImpulsePlayed: '衝動プレイごと:', onAetherGained: '霊気獲得ごと:',
    onCardSet: '伏せるごと:', onReactionFired: 'リアクション発動ごと:',
  }
  const cond = e.condition
    ? `[${e.condition.hpAtOrBelowRatio !== undefined ? `HP${Math.round(e.condition.hpAtOrBelowRatio * 100)}%以下` : ''}${e.condition.minDamageTaken !== undefined ? `被ダメ${e.condition.minDamageTaken}以上` : ''}${e.condition.maxActionValue !== undefined ? `行動値${e.condition.maxActionValue}以下` : ''}]`
    : ''
  return `${trig[e.trigger] ?? e.trigger}${cond}${base[e.effect] ?? `${e.effect}${a || ''}`}${th}`
}

function cardLine(def: CardDef): string {
  const extras = [
    def.exhaust ? '消滅' : '',
    def.discardCost ? `捨てコスト${def.discardCost}` : '',
    def.exhaustCost ? `消滅コスト${def.exhaustCost}` : '',
    def.retainer ? '従者' : '',
  ].filter(Boolean).join('・')
  const body = def.modes?.length
    ? def.modes.map((m, i) => `選択${i}:${m.effects.map(fx).join('+')}`).join(' / ')
    : def.effects.map(fx).join('、')
  const costLabel = def.xCost ? 'X' : `${def.cost}`
  return `${def.name}(${costLabel}E/${def.type})${extras ? `【${extras}】` : ''} ${body}`
}

function branchText(it: { kind: string; shownMin: number; shownMax: number; hits?: number; inflict?: { status: string; amount: number }; alsoDefend?: number }): string {
  const hits = (it.hits ?? 1) > 1 ? `×${it.hits}回(値は1発あたり)` : ''
  const inflict = it.inflict ? `+状態異常(${it.inflict.status}${it.inflict.amount})` : ''
  const guard = it.alsoDefend !== undefined ? `+防御${it.alsoDefend}` : ''
  const kinds: Record<string, string> = {
    attack: `攻撃${it.shownMin}〜${it.shownMax}${hits}${guard}`,
    defend: `防御${it.shownMin}〜${it.shownMax}`,
    'destroy-set': '伏せ破壊',
    'destroy-token': '従者狩り',
    buff: `強化+${it.shownMin}〜${it.shownMax}`,
    rally: `応援+${it.shownMin}〜${it.shownMax}(味方全体)`,
    hex: '呪い',
    heal: `回復${it.shownMin}〜${it.shownMax}(最も傷んだ味方)`,
    'steal-gold': `盗み${it.shownMin}〜${it.shownMax}G`,
    flee: '逃走(倒すか打ち消せば阻止)',
    rest: '隙だらけ',
  }
  return `${kinds[it.kind] ?? it.kind}${inflict}`
}

function intentLine(s: GameState, i: number): string {
  const e = s.enemies[i]
  if (!e.intent) return '---'
  // 条件付き意図: 両分岐を予告する (プレイヤーが自ターン中にどちらを選ばせるか決められる)
  if (e.intent.conditionalOn && e.intent.alt) {
    const cond = e.intent.conditionalOn === 'set' ? '伏せ札あり' : '従者あり'
    const now = effectiveIntent(s, i)!
    return `【${cond}】${branchText(e.intent.alt)} ／【なし】${branchText(e.intent)} → 今は「${branchText(now)}」`
  }
  const it = e.intent
  const hits = (it.hits ?? 1) > 1 ? `×${it.hits}回` : ''
  const inflict = it.inflict ? `+状態異常(${it.inflict.status}${it.inflict.amount})` : ''
  const guard = it.alsoDefend !== undefined ? `+防御${it.alsoDefend}` : ''
  const kinds: Record<string, string> = {
    attack: `攻撃${it.shownMin}〜${it.shownMax}${hits ? `${hits}(値は1発あたり)` : ''}${guard}`, defend: `防御${it.shownMin}〜${it.shownMax}`,
    'destroy-set': '伏せ破壊', 'destroy-token': '従者狩り', buff: `強化+${it.shownMin}〜${it.shownMax}`,
    rally: `応援+${it.shownMin}〜${it.shownMax}(味方全体)`, hex: '呪い',
    heal: `回復${it.shownMin}〜${it.shownMax}(最も傷んだ味方)`, 'steal-gold': `盗み${it.shownMin}〜${it.shownMax}G`,
    flee: '逃走(倒すか打ち消せば阻止)', rest: '隙だらけ',
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
      if (e.type === 'DamageDealt') L.push(` ${e.source === 'player' ? '与ダメ' : '被ダメ'}${e.amount}(HP損失${'hpLoss' in e ? e.hpLoss : '?'})`)
      else if (e.type === 'CardPlayed') L.push(` プレイ:${cname(e.cardId)}`)
      else if (e.type === 'CardSet') L.push(` 伏せた:${cname(e.cardId)}`)
      else if (e.type === 'ReactionTriggered') L.push(` リアクション発動:${cname(e.cardId)}`)
      else if (e.type === 'CardExhausted') L.push(` 消滅:${cname(e.cardId)}`)
      else if (e.type === 'TokenDestroyed') L.push(` 従者狩り:${cname(e.cardId)}が倒された`)
      else if (e.type === 'SetCardDestroyed') L.push(` 伏せ破壊:${cname(e.cardId)}が壊された`)
      else if (e.type === 'TurnStarted') L.push(` === ターン${e.turn} ===`)
      else if (e.type === 'HpHealed') L.push(` 回復${e.amount}`)
      else if (e.type === 'HpLost') L.push(` 自傷${e.amount}`)
      else if (e.type === 'StatusInflicted') L.push(` 状態異常:${e.status}${e.amount}`)
      else if (e.type === 'CombatEnded') L.push(` ★戦闘${e.result === 'won' ? '勝利' : '敗北'}★`)
      else if (e.type === 'ThornsReflected') L.push(` 🦔とげ反射${e.amount}(HP損失${e.hpLoss}。ブロックで吸収した分は損失に出ない)`)
      else if (e.type === 'GoldStolen') L.push(` 💰${e.amount}G盗まれた(逃がす前に倒せば取り返す)`)
      else if (e.type === 'EnemyFled') L.push(` 🏃敵${e.enemyIndex}が逃走した`)
      else if (e.type === 'EnemyHealed') L.push(` 💚敵${e.enemyIndex}が敵${e.targetIndex}を回復+${e.amount}`)
    }
  }
  L.push(`--- 盤面 (ターン${s.turn} / phase=${s.phase}) ---`)
  const st = [
    `HP ${Math.max(0, p.hp)}/${p.maxHp}`, `ブロック${p.block}`, p.iceBlock ? `氷壁${p.iceBlock}` : '',
    `エナジー${p.energy}/${p.energyMax}`, p.growth ? `成長${p.growth}` : '', p.momentum ? `勢い${p.momentum}` : '',
    p.aether ? `霊気${p.aether}` : '', p.nextCardDiscount ? `次-${p.nextCardDiscount}` : '',
    `消滅置き場${p.exhaustPile.length}枚`, p.weak ? `弱体${p.weak}` : '', p.vulnerable ? `脆弱${p.vulnerable}` : '',
    p.selfHpLost ? `自傷累計${p.selfHpLost}` : '', p.damageTakenLastEnemyPhase ? `直前被ダメ${p.damageTakenLastEnemyPhase}` : '',
    `山札${p.drawPile.length}/捨て札${p.discardPile.length}`,
  ].filter(Boolean).join(' | ')
  L.push(`自分: ${st}`)
  // 予測被ダメ (最悪値): 複数体の同時攻撃を暗算しなくて済むように総量を出す
  let worst = 0
  s.enemies.forEach((e, i) => {
    if (e.hp <= 0) return
    const it = effectiveIntent(s, i)
    if (it?.kind === 'attack') worst += it.shownMax * (it.hits ?? 1)
  })
  if (worst > 0) {
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
      e.block ? `ブロック${e.block}` : '', e.strength ? `強化${e.strength > 0 ? '+' : ''}${e.strength}` : '',
      e.burn ? `延焼${e.burn}` : '', e.confusion ? `混乱${e.confusion}` : '', e.exposed ? `急所${e.exposed}` : '',
      def.burnResist ? `延焼耐性${def.burnResist}` : '', def.thorns ? `とげ${def.thorns}(攻撃ヒットごとに反射。倒せば無傷)` : '',
      e.stolenGold ? `💰${e.stolenGold}G抱え込み(逃す前に倒せば取り返す)` : '',
      def.regen && e.hp > e.maxHp * 0.5 ? `再生${def.regen}${def.regenBreak ? `(このターン${def.regenBreak}以上削ると停止)` : ''}` : '',
      def.enrage ? (def.enrageEveryCards ? `激昂+${def.enrage}/${def.enrageEveryCards}枚プレイ` : `激昂+${def.enrage}/T`) : '',
    ].filter(Boolean).join(' ')
    L.push(`敵${i}: ${def.name} HP${Math.max(0, e.hp)}/${e.maxHp} ${tags} → 意図: ${intentLine(s, i)}`)
  })
  if (p.setCards.length > 0 || p.setSlots > 1) {
    L.push(`伏せ場(${p.setCards.length}/${p.setSlots}): ${p.setCards.map((c) => `[${c.uid}] ${cardLine(c.def)}`).join(' / ') || 'なし'}`)
  }
  if (p.permanents.length > 0) {
    L.push(`置物: ${p.permanents.map((c) => `${c.def.name}${c.token ? '(トークン)' : ''}(${c.def.effects.map(fx).join('、')})`).join(' / ')}`)
  }
  if (s.phase === 'awaiting-reaction' && s.pendingWindow) {
    const enemy = s.enemies[s.pendingWindow.enemyIndex]
    // 条件付き意図の解決後の分岐を表示する (素の intent を出すと実値が幅表示と食い違う)
    const it = effectiveIntent(s, s.pendingWindow.enemyIndex)
    L.push(`!! 確認ウィンドウ (${s.pendingWindow.stage === 'pre' ? '行動実行前' : '行動解決後'}): ${getEnemyDef(enemy.enemyId).name}の「${it ? branchText(it) : '---'}」実値=${it?.actual}${(it?.hits ?? 1) > 1 ? `×${it?.hits}回` : ''}`)
    const win = windowFromPending(s)
    const cands = win ? p.setCards.filter((c) => reactionMatches(s, c, win)) : []
    L.push(`   発動候補: ${cands.map((c) => `[${c.uid}] ${c.def.name}`).join(' / ') || 'なし'}`)
    // post窓の誤認防止 (2026-08-29 検証ラン: 瀕死時に返し札を「防御」と誤認して発動→敗死の報告)
    if (s.pendingWindow.stage === 'post') {
      L.push('   ※この攻撃はすでに解決済み——発動しても今回の被弾は取り消せない (返し・回復のための窓)')
    }
    // 後続の敵の条件付き分岐が「伏せなし」側に化ける警告 (2026-08-28)
    for (const ri of setBranchFlipRisks(s)) {
      const rEnemy = s.enemies[ri]
      L.push(`   ⚠ 発動すると伏せ枠が空く: ${getEnemyDef(rEnemy.enemyId).name}の行動が【伏せなし】分岐 (${branchText(rEnemy.intent!)}) に変わる`)
    }
    L.push(`   → {"type":"ConfirmReaction","fire":true,"cardUid":"..."} か {"type":"ConfirmReaction","fire":false} (温存)`)
  }
  if (s.phase === 'player-turn') {
    L.push('手札:')
    for (const c of p.hand) {
      const cost = effectiveCost(s, c)
      const playable = isPlayableFromHand(c) && cost <= p.energy
      const canSet = c.def.type === 'reaction' && p.setCards.length < p.setSlots && c.def.cost <= p.energy
      const marks = [
        c.def.id === 'status_wound' || c.def.id === 'status_junk'
          ? '使用不可(死に札)'
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
        canSet ? '伏せ可' : '',
        cardNeedsTarget(c) && s.enemies.filter((e) => e.hp > 0).length > 1 ? '要targetIndex' : '',
        p.impulseUids.includes(c.uid) ? '衝動(このターン限り)' : '',
      ].filter(Boolean).join('・')
      L.push(` [${c.uid}] ${cardLine(c.def)} 〈${marks || 'プレイ不可'}〉`)
    }
  }
  if (s.phase === 'won') L.push(`★★ 勝利 ★★  ⚔️ 戦いの記録: ${summaryLine(battleSummary(s.eventLog))}`)
  if (s.phase === 'lost') L.push('★★ 敗北 ★★')
  return L.join('\n')
}

const NODE_ICON: Record<string, string> = {
  battle: '⚔', elite: '👑', campfire: '🔥', workshop: '🔨', shop: '🛒', event: '❓', boss: '💀',
}

/** マップ全体をテキスト描画 (全体可視・現在地と次の選択肢を明示) */
/** 現在地から到達可能なノード集合 (row -> Set<col>)。開始前は行0の全ノードから前向きに広げる */
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

const NODE_LABEL: Record<string, string> = { campfire: '焚き火', workshop: '工房', shop: 'ショップ', event: '?' }

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
    L.push(` ${r === from ? '→' : '  '}行${String(r).padStart(2)}: ${cells.join(' | ')}`)
  }
  // この先の特別ノード要約 (計画用の最小情報)
  const ahead: string[] = []
  const kinds: [string, string][] = [['campfire','🔥'],['shop','🛒'],['workshop','🔨'],['event','❓'],['elite','👑'],['boss','💀']]
  for (const [t, icon] of kinds) {
    const rows: number[] = []
    run.map.forEach((row, r) => {
      if (r > run.row && row.some((n, c) => n.type === t && reach.has(`${r}:${c}`))) rows.push(r)
    })
    if (rows.length > 0) ahead.push(`${icon}行${rows.join(',')}`)
  }
  L.push(`   この先(到達可能): ${ahead.join(' ') || 'なし'}`)
  L.push('→ {"type":"ChooseNode","col":N} で「←選べる」のノードへ進む')
  return L.join('\n')
}

function renderMap(run: RunState): string {
  const L: string[] = []
  const cands = nextChoices(run)
  const reach = reachableSet(run)
  L.push('🗺 マップ (下から上へ。全体もエッジ(→接続先col)も最初から見える。エリート👑=強化+2/HP×1.35、勝てばレリック3択)')
  L.push('   ※現在地から到達できないノードは (到達不可) 付き。接続は前の行でどの列を選んだかで決まる')
  for (let r = run.map.length - 1; r >= 0; r--) {
    const cells = run.map[r].map((n, c) => {
      // ?マスの中身 (eventId) は入るまで伏せる (確定済みルール表「?マス（イベント）」)
      const typeLabel: Record<string, string> = { campfire: '焚き火', workshop: '工房', shop: 'ショップ', event: '?' }
      const label = n.encounterId !== null ? `${NODE_ICON[n.type]}${encounterName(n.encounterId)}` : `${NODE_ICON[n.type]}${typeLabel[n.type] ?? n.type}`
      const edges = n.next.length > 0 ? `→${n.next.join('·')}` : ''
      const here = r === run.row && c === run.col ? '【現在地】' : ''
      const unreachable = !here && r > run.row && !reach.has(`${r}:${c}`) ? '(到達不可)' : ''
      const choice = r === run.row + 1 && cands.includes(c) ? `←選べる[col:${c}]` : ''
      return `[${c}]${label}${edges}${here}${unreachable}${choice}`
    })
    const mark = r === run.row + 1 ? '→' : '  '
    L.push(` ${mark}行${String(r).padStart(2)}: ${cells.join(' | ')}`)
  }
  L.push('→ {"type":"ChooseNode","col":N} で「←選べる」のノードへ進む')
  return L.join('\n')
}

function renderRun(run: RunState, logFrom: number, fullMap = false): string {
  const L: string[] = []
  const leader = getLeaderDef(run.leaderId)
  L.push(`=== ラン: ${leader.name} | 幕${run.act}/3 行${run.row + 1}/16 | 戦闘${run.battlesWon}勝 | HP持ち越し${run.hp} | 💰${run.gold}G | フェーズ:${run.phase} | レリック:${run.relics.map((r) => getRelicDef(r).name).join('、') || 'なし'} ===`)
  if (run.phase === 'combat' && run.combat) {
    L.push(renderBattle(run.combat, logFrom))
  } else if (run.phase === 'reward' && run.rewardOptions) {
    if (run.combat?.phase === 'won') L.push(`⚔️ 戦いの記録: ${summaryLine(battleSummary(run.combat.eventLog))}`)
    L.push('報酬ピック (1枚選ぶ or スキップ):')
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
      L.push(`  🪨鍛冶の砥石: 鍛えるはあと${1 + (run.campfireForgeBonus ?? 0) - (run.campfireUpgradesUsed ?? 0)}枚 (休む・除去とは併用不可)`)
    }
    L.push(`  休む (CampfireRest) → HP+${Math.floor(run.maxHp * run.campfireRatio)} 回復して次へ${(run.campfireUpgradesUsed ?? 0) > 0 ? ' ※鍛えた後なので回復なしの立ち去りになる' : ''}`)
    L.push('  強化 (CampfireUpgrade) → デッキの1枚を鍛える (量の効果が+50%。同じ札は1回だけ)')
    L.push('  除去 (CampfireRemove) → デッキから1枚を永久に取り除く')
    run.deck.forEach((c, i) => {
      const mark = canUpgradeCard(c) ? ` → 鍛えると: ${cardLine(upgradeCard(c).def)}` : ' 【鍛えられない】'
      L.push(`   [${i}] ${cardLine(c.def)}${mark}`)
    })
    L.push('→ {"type":"CampfireUpgrade","index":N} / {"type":"CampfireRemove","index":N} / {"type":"CampfireRest"}(何もしない)')
  } else if (run.phase === 'shop' && run.shop) {
    L.push(`🛒 ショップ (所持 ${run.gold}G。買わずに出てもよい)`)
    run.shop.cards.forEach((item, i) => L.push(` [${i}] ${item.price}G: ${cardLine(getCardDef(item.id))}`))
    if (run.shop.relicId !== null) {
      const r = getRelicDef(run.shop.relicId)
      L.push(` レリック ${run.shop.relicPrice}G: ${r.name} (${r.description})`)
    }
    L.push(` カード除去サービス ${shopRemovalPrice(run)}G (回数無制限・使うたび+25G)`)
    L.push(` カード強化サービス ${shopUpgradePrice(run)}G (回数無制限・使うたび+30G。焚き火の「鍛える」と同じ)`)
    L.push('→ {"type":"ShopBuyCard","index":N} / {"type":"ShopBuyRelic"} / {"type":"ShopRemove","index":N}(デッキ番号) / {"type":"ShopUpgrade","index":N}(デッキ番号) / {"type":"ShopLeave"}')
    L.push('   デッキ:')
    run.deck.forEach((c, i) => L.push(`   [${i}] ${cardLine(c.def)}`))
  } else if (run.phase === 'event') {
    const node = run.map[run.row][run.col]
    const ev = getEventDef(node.eventId!)
    L.push(`❓ ${ev.sprite ?? ''} ${ev.name}`)
    L.push(`   ${ev.flavor}`)
    ev.choices.forEach((c, i) => {
      const locked = c.requireGold !== undefined && run.gold < c.requireGold ? ' 【G不足で選べない】' : ''
      const needCard = c.removeCard || c.upgradeCard ? ' 【要cardIndex(デッキ番号)】' : ''
      L.push(` [${i}] ${c.label}${locked}${needCard}`)
    })
    L.push('→ {"type":"EventChoice","index":N} (対象カードが要る選択肢は {"type":"EventChoice","index":N,"cardIndex":M})')
    L.push('   デッキ:')
    run.deck.forEach((c, i) => L.push(`   [${i}] ${cardLine(c.def)}`))
  } else if (run.phase === 'workshop') {
    L.push('🔨 工房: 異なる2枚を合成して1枚の新カードにできる (素材は消える)。見送りも可')
    L.push(
      '   タイプ跨ぎも可: 結果は持続する側 (置物＞リアクション＞呪文＞物理)。置物化は量÷3で毎ターン化',
    )
    run.deck.forEach((c, i) => L.push(`   [${i}] ${cardLine(c.def)}`))
    L.push('→ {"type":"WorkshopFuse","indexA":N,"indexB":M} か {"type":"WorkshopSkip"}')
    L.push('   確定前の確認: {"type":"FusePreview","indexA":N,"indexB":M} (状態を変えずに結果を表示)')
    L.push('   (緑同士のみ。同名2枚は「真・」化=2枚ぶんを圧縮した強化版。コストはVP査定からの逆算=素材コストの単純合算ではない)')
    L.push('   特定の組み合わせは手書きレシピ(⭐)にヒットし、計算値より少し強い一品になる')
  } else if (run.phase === 'relic-reward' && run.relicOptions) {
    if (run.combat?.phase === 'won') L.push(`⚔️ 戦いの記録: ${summaryLine(battleSummary(run.combat.eventLog))}`)
    L.push('レリック報酬 (1つ選ぶ or スキップ):')
    run.relicOptions.forEach((id, i) => {
      const def = getRelicDef(id)
      L.push(` [${i}] ${def.name}: ${def.description}`)
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
  const [leaderId, seed, file, deckId] = args
  const run = createRun(Number(seed), 'set-confirm', leaderId, deckId || undefined)
  const sf: SaveFile = { kind: 'run', run, logIndex: 0 }
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
    } else {
      const reason = fuseBlockReason(a, b)
      if (reason !== null) {
        console.log(`合成不可: ${reason}`)
      } else {
        const def = fuseCards(a, b)
        const recipe = def.id.startsWith('fusion_') ? '⭐レシピ発見! ' : ''
        console.log(`プレビュー: ${recipe}${cardLine(def)} (素材は消費されていない)`)
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
    } catch (err) {
      // 不正なコマンドはスタックトレースでなく1行のエラーで返す (状態は保存しない)
      console.log(`エラー: ${err instanceof Error ? err.message : String(err)}`)
      process.exit(0)
    }
    sf.logIndex = currentLogLength(sf)
    save(file, sf)
    console.log(renderRun(sf.run, logFrom))
  } else {
    try {
      sf.battle = applyCommand(sf.battle!, cmd as Command)
    } catch (err) {
      console.log(`エラー: ${err instanceof Error ? err.message : String(err)}`)
      process.exit(0)
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
  console.log('usage: play.ts new-run <leaderId> <seed> <file> [deckId] | new-battle <deckId> <enemyId> <seed> <file> | cmd <file> <json> | show <file> [full]')
}
