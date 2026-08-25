// sim/play.ts — LLM/人間がテキストで1手ずつプレイするためのCLIハーネス
//
// 使い方 (状態はJSONファイルに保存され、1コマンド=1プロセスで進める):
//   npx tsx src/sim/play.ts new-run <leaderId> <seed> <stateFile>
//   npx tsx src/sim/play.ts new-battle <deckId> <enemyId> <seed> <stateFile>
//   npx tsx src/sim/play.ts cmd <stateFile> '<コマンドJSON>'
//   npx tsx src/sim/play.ts show <stateFile>
//
// コマンドJSON例:
//   {"type":"PlayCard","cardUid":"c12","targetIndex":0}
//   {"type":"SetCard","cardUid":"c3"} / {"type":"EndTurn"}
//   {"type":"ConfirmReaction","fire":true,"cardUid":"c3"} / {"type":"ConfirmReaction","fire":false}
//   ラン専用: {"type":"PickReward","index":0} / {"type":"SkipReward"}
//            {"type":"ChooseElite","elite":true} / {"type":"PickRelic","index":0} / {"type":"SkipRelic"}

import { readFileSync, writeFileSync } from 'node:fs'
import { getCardDef, getEnemyDef, getLeaderDef, getRelicDef } from '../engine/content.ts'
import { effectiveCost, isPlayableFromHand, cardNeedsTarget, reactionMatches, windowFromPending } from '../engine/effects.ts'
import { applyRunCommand, createRun } from '../engine/run.ts'
import { applyCommand, createInitialState } from '../engine/state.ts'
import type { CardDef, CardInstance, Command, DeclarativeEffect, GameState } from '../engine/types.ts'
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
    onAttackPlayed: '攻撃プレイごと:', onSpellPlayed: '呪文プレイごと:', onSetDestroyed: '伏せ破壊時:',
    onHealed: '回復ごと:', onHpLost: 'HP損失ごと:', onCardExhausted: '消滅ごと:', onCostExhausted: '消滅コストごと:',
    onPermanentEntered: '置物登場ごと:', onImpulsePlayed: '衝動プレイごと:', onAetherGained: '霊気獲得ごと:',
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
  return `${def.name}(${def.cost}E/${def.type})${extras ? `【${extras}】` : ''} ${body}`
}

function intentLine(s: GameState, i: number): string {
  const e = s.enemies[i]
  if (!e.intent) return '---'
  const it = e.intent
  const hits = (it.hits ?? 1) > 1 ? `×${it.hits}回` : ''
  const inflict = it.inflict ? `+状態異常(${it.inflict.status}${it.inflict.amount})` : ''
  const kinds: Record<string, string> = {
    attack: `攻撃${it.shownMin}〜${it.shownMax}${hits}`, defend: `防御${it.shownMin}〜${it.shownMax}`,
    'destroy-set': '伏せ破壊', 'destroy-token': '従者狩り', buff: `強化+${it.shownMin}〜${it.shownMax}`,
    rally: `応援+${it.shownMin}〜${it.shownMax}(味方全体)`, hex: '呪い',
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
      else if (e.type === 'CardPlayed') L.push(` プレイ:${getCardDef(e.cardId).name}`)
      else if (e.type === 'CardSet') L.push(` 伏せた:${getCardDef(e.cardId).name}`)
      else if (e.type === 'ReactionTriggered') L.push(` リアクション発動:${getCardDef(e.cardId).name}`)
      else if (e.type === 'CardExhausted') L.push(` 消滅:${getCardDef(e.cardId).name}`)
      else if (e.type === 'TokenDestroyed') L.push(` 従者狩り:${getCardDef(e.cardId).name}が倒された`)
      else if (e.type === 'TurnStarted') L.push(` === ターン${e.turn} ===`)
      else if (e.type === 'HpHealed') L.push(` 回復${e.amount}`)
      else if (e.type === 'HpLost') L.push(` 自傷${e.amount}`)
      else if (e.type === 'StatusInflicted') L.push(` 状態異常:${e.status}${e.amount}`)
      else if (e.type === 'CombatEnded') L.push(` ★戦闘${e.result === 'won' ? '勝利' : '敗北'}★`)
    }
  }
  L.push(`--- 盤面 (ターン${s.turn} / phase=${s.phase}) ---`)
  const st = [
    `HP ${p.hp}/${p.maxHp}`, `ブロック${p.block}`, p.iceBlock ? `氷壁${p.iceBlock}` : '',
    `エナジー${p.energy}/${p.energyMax}`, p.growth ? `成長${p.growth}` : '', p.momentum ? `勢い${p.momentum}` : '',
    p.aether ? `霊気${p.aether}` : '', p.nextCardDiscount ? `次-${p.nextCardDiscount}` : '',
    `消滅置き場${p.exhaustPile.length}枚`, p.weak ? `弱体${p.weak}` : '', p.vulnerable ? `脆弱${p.vulnerable}` : '',
    p.selfHpLost ? `自傷累計${p.selfHpLost}` : '', p.damageTakenLastEnemyPhase ? `直前被ダメ${p.damageTakenLastEnemyPhase}` : '',
    `山札${p.drawPile.length}/捨て札${p.discardPile.length}`,
  ].filter(Boolean).join(' | ')
  L.push(`自分: ${st}`)
  s.enemies.forEach((e, i) => {
    if (e.hp <= 0) { L.push(`敵${i}: ${getEnemyDef(e.enemyId).name} 💀撃破済み`); return }
    const def = getEnemyDef(e.enemyId)
    const tags = [
      e.block ? `ブロック${e.block}` : '', e.strength ? `強化${e.strength > 0 ? '+' : ''}${e.strength}` : '',
      e.burn ? `延焼${e.burn}` : '', e.confusion ? `混乱${e.confusion}` : '', e.exposed ? `急所${e.exposed}` : '',
      def.burnResist ? `延焼耐性${def.burnResist}` : '', def.regen && e.hp > e.maxHp * 0.5 ? `再生${def.regen}` : '',
      def.enrage ? `激昂+${def.enrage}/T` : '',
    ].filter(Boolean).join(' ')
    L.push(`敵${i}: ${def.name} HP${e.hp}/${e.maxHp} ${tags} → 意図: ${intentLine(s, i)}`)
  })
  if (p.setCards.length > 0 || p.setSlots > 1) {
    L.push(`伏せ場(${p.setCards.length}/${p.setSlots}): ${p.setCards.map((c) => `[${c.uid}] ${cardLine(c.def)}`).join(' / ') || 'なし'}`)
  }
  if (p.permanents.length > 0) {
    L.push(`置物: ${p.permanents.map((c) => `${c.def.name}${c.token ? '(トークン)' : ''}(${c.def.effects.map(fx).join('、')})`).join(' / ')}`)
  }
  if (s.phase === 'awaiting-reaction' && s.pendingWindow) {
    const enemy = s.enemies[s.pendingWindow.enemyIndex]
    const it = enemy?.intent
    L.push(`!! 確認ウィンドウ (${s.pendingWindow.stage === 'pre' ? '行動実行前' : '行動解決後'}): ${getEnemyDef(enemy.enemyId).name}の実値=${it?.actual}${(it?.hits ?? 1) > 1 ? `×${it?.hits}回` : ''}`)
    const win = windowFromPending(s)
    const cands = win ? p.setCards.filter((c) => reactionMatches(s, c, win)) : []
    L.push(`   発動候補: ${cands.map((c) => `[${c.uid}] ${c.def.name}`).join(' / ') || 'なし'}`)
    L.push(`   → {"type":"ConfirmReaction","fire":true,"cardUid":"..."} か {"type":"ConfirmReaction","fire":false} (温存)`)
  }
  if (s.phase === 'player-turn') {
    L.push('手札:')
    for (const c of p.hand) {
      const cost = effectiveCost(s, c)
      const playable = isPlayableFromHand(c) && cost <= p.energy
      const canSet = c.def.type === 'reaction' && p.setCards.length < p.setSlots && c.def.cost <= p.energy
      const marks = [
        playable ? 'プレイ可' : c.def.type === 'reaction' ? '' : 'エナジー不足',
        canSet ? '伏せ可' : '',
        cardNeedsTarget(c) && s.enemies.filter((e) => e.hp > 0).length > 1 ? '要targetIndex' : '',
        p.impulseUids.includes(c.uid) ? '衝動(このターン限り)' : '',
      ].filter(Boolean).join('・')
      L.push(` [${c.uid}] ${cardLine(c.def)} 〈${marks || 'プレイ不可'}〉`)
    }
  }
  if (s.phase === 'won') L.push('★★ 勝利 ★★')
  if (s.phase === 'lost') L.push('★★ 敗北 ★★')
  return L.join('\n')
}

function renderRun(run: RunState, logFrom: number): string {
  const L: string[] = []
  const leader = getLeaderDef(run.leaderId)
  L.push(`=== ラン: ${leader.name} | 戦闘${run.battleIndex + 1}/10 | HP持ち越し${run.hp} | フェーズ:${run.phase} | レリック:${run.relics.map((r) => getRelicDef(r).name).join('、') || 'なし'} ===`)
  if (run.phase === 'combat' && run.combat) {
    L.push(renderBattle(run.combat, logFrom))
  } else if (run.phase === 'reward' && run.rewardOptions) {
    L.push('報酬ピック (1枚選ぶ or スキップ):')
    run.rewardOptions.forEach((id, i) => L.push(` [${i}] ${cardLine(getCardDef(id))}`))
    L.push('→ {"type":"PickReward","index":N} か {"type":"SkipReward"}')
  } else if (run.phase === 'offer') {
    L.push('エリート挑戦オファー: 挑めば敵が強化+2/HP×1.35、勝てばレリック3択が付く')
    L.push('→ {"type":"ChooseElite","elite":true} か {"type":"ChooseElite","elite":false}')
  } else if (run.phase === 'relic-reward' && run.relicOptions) {
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
  const [leaderId, seed, file] = args
  const run = createRun(Number(seed), 'set-confirm', leaderId)
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
  if (sf.kind === 'run') {
    // 戦闘コマンドは自動で Combat に包む (エルゴノミクス)
    const runCmd: RunCommand =
      ['PickReward', 'SkipReward', 'ChooseElite', 'PickRelic', 'SkipRelic', 'StartRun'].includes(cmd.type)
        ? (cmd as RunCommand)
        : { type: 'Combat', command: cmd as Command }
    sf.run = applyRunCommand(sf.run!, runCmd)
    sf.logIndex = currentLogLength(sf)
    save(file, sf)
    console.log(renderRun(sf.run, logFrom))
  } else {
    sf.battle = applyCommand(sf.battle!, cmd as Command)
    sf.logIndex = currentLogLength(sf)
    save(file, sf)
    console.log(renderBattle(sf.battle, logFrom))
  }
} else if (mode === 'show') {
  const [file] = args
  const sf = load(file)
  console.log(sf.kind === 'run' ? renderRun(sf.run!, 0) : renderBattle(sf.battle!, 0))
} else {
  console.log('usage: play.ts new-run <leaderId> <seed> <file> | new-battle <deckId> <enemyId> <seed> <file> | cmd <file> <json> | show <file>')
}
