// ui/ は状態を読んでコマンドを投げるだけの薄い層。ゲームロジックを書かない (CLAUDE.md)。
// 見た目は静的なゲーム風UI (StS風配置・ダーク)。動く演出はやらない (CLAUDE.md「UIの見た目の方針」)。
import { useState } from 'react'
import {
  allDecks,
  allEncounters,
  allEnemies,
  allLeaders,
  deckAllowedForLeader,
  deckSize,
  encounterName,
  getCardDef,
  getDeckDef,
  getEnemyDef,
  getLeaderDef,
} from '../engine/content.ts'
import {
  cardNeedsTarget,
  effectiveCost,
  intimidatedActionValue,
  isDamageEffect,
  isPlayableFromHand,
} from '../engine/effects.ts'
import { playableReactions } from '../engine/reactions/hold-manual.ts'
import { getReactionSystem } from '../engine/reactions/index.ts'
import { applyRunCommand, createRun, RUN_BATTLES } from '../engine/run.ts'
import type { RunCommand, RunState } from '../engine/run.ts'
import { applyCommand, createInitialState } from '../engine/state.ts'
import type {
  CardColor,
  CardType,
  CardDef,
  CardInstance,
  Command,
  DeclarativeEffect,
  EnemyArchetype,
  EnemyIntent,
  GameEvent,
  GameState,
  ReactionMode,
} from '../engine/types.ts'
import './styles.css'

// 採用方式は set-confirm で確定 (2026-08-23。CLAUDE.md「リアクション方式」)。
// 他方式のエンジン実装は残置しているが、UI からは選択させない。
const ADOPTED_MODE: ReactionMode = 'set-confirm'

const ARCHETYPE_LABEL: Record<EnemyArchetype, string> = {
  'wide-power': '幅威力型',
  probe: '探り型（ローテーション）',
  'set-wary': '伏せ警戒型',
  'set-breaker': '伏せ破壊型',
  brute: '脳筋型（強化ループ）',
  charger: 'チャージ型（大技予告）',
  hexer: '妖術師型（状態異常）',
  flurry: '連撃型',
  regenerator: '再生型（HP半分で豹変）',
  taunter: '挑発型（伏せ無しに大振り）',
  enrager: '激昂型（毎ターン強化）',
  support: '応援型（味方全体強化）',
}
const ARCHETYPE_SPRITE: Record<EnemyArchetype, string> = {
  'wide-power': '🐍',
  probe: '🦂',
  'set-wary': '👁️',
  'set-breaker': '🔨',
  brute: '👹',
  charger: '🐢',
  hexer: '🧙',
  flurry: '🐺',
  regenerator: '🦥',
  taunter: '🤡',
  enrager: '🗿',
  support: '🥁',
}

// カードタイプの表示ラベル (2026-08-24決定。物理=武器・道具・身体/呪文=魔力の行使 → docs/card-power.md §0)
const TYPE_LABEL: Record<CardType, string> = {
  physical: '物理',
  spell: '呪文',
  reaction: 'リアクション',
  permanent: '置物',
}

const COLOR_LABEL: Record<CardColor, string> = { green: '🌿 緑', blue: '💧 青', red: '🔥 赤' }

// ---- キーワード能力のツールチップ ----

/** キーワード能力の用語解説 (カーソルを当てると吹き出しで表示) */
const KEYWORD_HELP: Record<string, string> = {
  威嚇: '延焼を持つ敵の攻撃は、延焼2につき実値-1（軽減上限-4・下限1）。表示中の攻撃値は威嚇適用後の数字',
  弱体: '与えるダメージが25%減る（切り捨て）。自分のターン終了時に1減る',
  脆弱: '敵の攻撃で受けるダメージが50%増える（切り捨て）。敵の行動フェーズ終了時に1減る',
  負傷: '使えない死に札。手札に来ても何もできず、ターン終了時に捨てられる（1戦闘で最大5枚まで）',
  再生: '敵フェーズ終了時にHPが回復する。HP半分以下になると止まる',
  激昂: '敵フェーズ終了時に自動で強化が増える。長引くほど攻撃が痛くなる',
  混乱: '混乱した敵の攻撃は、プレイヤーでなく他の生存敵（いなければ自分自身）に向かう。攻撃1回ごとに1減る',
  応援: '味方全体の強化を増やす。応援役を先に倒すか、無視して本体を叩くかの選択',
  貫通: '敵のブロックを無視してダメージを与える（トランプル）',
  勢い: 'このターンの以降の攻撃ダメージに加算。自分のターン終了時に0に戻る',
  成長: 'この戦闘の間、与えるダメージすべてに加算される（戦闘ごとにリセット）',
  消滅: '使用後、この戦闘から取り除かれる（再シャッフルされない）',
  置物: 'プレイすると場に残り、戦闘中ずっと効果を発揮する（破壊されない）',
  打ち消す: '敵の行動1回を完全に無効化する（攻撃・防御・伏せ破壊・強化すべて対象）',
  返し: '攻撃してきた敵にダメージを与える（敵の攻撃自体は受ける）',
  被攻撃前: '敵の攻撃でダメージを受ける直前に発動できる（軽減向き）',
  被攻撃後: '敵の攻撃でダメージを受けた後に発動できる（返し向き）',
  敵強化時: '敵が強化した直後に発動できる',
  敵防御時: '敵がブロックを得た直後に発動できる',
  敵行動時: '敵の行動が確定した直後（実行前）に発動できる。打ち消し向き',
  毎ターン開始時: '自分のターン開始時に自動で発動する',
  攻撃プレイ後: '攻撃カードをプレイするたび自動で発動する（そのカード自身には乗らない）',
  追加コスト: 'プレイするためにエナジーとは別に支払うコスト',
  エナジー上限: '毎ターン開始時に回復するエナジーの量。戦闘ごとに3へリセット',
  強化: '以降の攻撃の実値と幅表示に加算される（敵のバフ。打ち消しで無効化できる）',
  伏せ破壊: '伏せているカードを破壊して捨て札に送る',
  氷壁: 'このブロックはターン開始で消えず持ち越される。通常ブロックを使い切った後に消費される',
  詠唱数: 'このターンにプレイしたカードの枚数（そのカード自身は数えない）。ターン開始でリセット',
  霊気: '妨害やリアクションの成功で溜まるエネルギー（戦闘中持続）。霊気放出で一気に叩きつける',
  延焼: '敵に蓄積する継続ダメージ。毎敵ターン開始時に延焼値ぶんのダメージ（ブロック無視）を与えて1減る',
  衝動: '山札の上からめくった「このターン限り」の手札。使わずにターンを終えると消滅する',
  粉砕: '敵のブロックを全て破壊する（無視ではなく叩き割る）',
}

const KW_PATTERN = new RegExp(
  `(${Object.keys(KEYWORD_HELP)
    .sort((a, b) => b.length - a.length)
    .join('|')})`,
  'g',
)

/** テキスト中のキーワード能力を吹き出し付き <span> に置き換える */
function kw(text: string): React.ReactNode {
  return text.split(KW_PATTERN).map((part, i) =>
    KEYWORD_HELP[part] ? (
      <span key={i} className="kw">
        {part}
        <span className="kw-tip">{KEYWORD_HELP[part]}</span>
      </span>
    ) : (
      part
    ),
  )
}

// ---- 表示用テキスト整形 (プレゼンテーションの関心。エンジンには置かない) ----

/** 戦闘中のカード表示に使う文脈。成長・勢い・エナジー上限・詠唱数込みの実数値を出す */
interface EffectCtx {
  growth: number
  momentum: number
  energyMax: number
  cardsPlayed: number
  aether: number
}

const TRIGGER_LABEL: Record<CardDef['effects'][number]['trigger'], string> = {
  onPlay: '',
  onAttackIncoming: '被攻撃前: ',
  onAttacked: '被攻撃後: ',
  onEnemyAction: '敵行動時: ',
  onEnemyBuffed: '敵強化時: ',
  onEnemyDefended: '敵防御時: ',
  onTurnStart: '毎ターン開始時: ',
  onAttackPlayed: '攻撃プレイ後: ',
}

/** 誘発の追加条件の表示 */
function conditionLabel(e: DeclarativeEffect): string {
  const c = e.condition
  if (!c) return ''
  const parts: string[] = []
  if (c.hpAtOrBelowRatio !== undefined) parts.push(`自分のHPが${Math.round(c.hpAtOrBelowRatio * 100)}%以下`)
  if (c.minDamageTaken !== undefined) parts.push(`${c.minDamageTaken}以上のダメージを受けた`)
  if (c.maxActionValue !== undefined) parts.push(`敵の行動の値が${c.maxActionValue}以下`)
  return parts.length > 0 ? `[${parts.join('かつ')}] ` : ''
}

/** 効果1つを1行のテキストに変換する */
function renderEffectItem(e: DeclarativeEffect, ctx?: EffectCtx): string {
  // 攻撃ダメージには成長+勢い、返しには成長のみ (勢いは自ターン終了でリセットされるため)
  const atkBonus = ctx ? ctx.growth + ctx.momentum : 0
  const trigger = TRIGGER_LABEL[e.trigger] + conditionLabel(e)
  const pierce = e.pierce ? '(貫通)' : ''
  const aoe = e.target === 'all' ? '敵全体に' : ''
  // トータル先頭表記: 補正込みの実ダメージを先に出し、内訳を括弧で添える
  const atkBreak = atkBonus > 0 ? `（${'基礎'}${e.amount}+補正${atkBonus}）` : ''
  switch (e.effect) {
    case 'dealDamage':
      return `${trigger}⚔️ ${aoe}${(e.amount ?? 0) + atkBonus}ダメージ${pierce}${atkBreak}`
    case 'dealDamagePerEnergyMax':
      return ctx
        ? `${trigger}エナジー上限×${e.amount}ダメージ${pierce} [現在${(e.amount ?? 0) * ctx.energyMax + atkBonus}]`
        : `${trigger}エナジー上限×${e.amount}ダメージ${pierce}`
    case 'counter': {
      const cBonus = ctx ? ctx.growth : 0
      const cBreak = cBonus > 0 ? `（基礎${e.amount}+成長${cBonus}）` : ''
      return `${trigger}↩️ 返し${(e.amount ?? 0) + cBonus}ダメージ${pierce}${cBreak}`
    }
    case 'gainBlock':
      return `${trigger}🛡 ブロック+${e.amount}`
    case 'gainIceBlock':
      return `${trigger}🧊 氷壁+${e.amount}`
    case 'dealDamagePerCardPlayed':
      return ctx
        ? `${trigger}詠唱数×${e.amount}ダメージ${pierce} [現在${(e.amount ?? 0) * ctx.cardsPlayed + atkBonus}]`
        : `${trigger}詠唱数×${e.amount}ダメージ${pierce}`
    case 'gainIceBlockPerCardPlayed':
      return ctx
        ? `${trigger}詠唱数×${e.amount}の氷壁 [現在${(e.amount ?? 0) * ctx.cardsPlayed}]`
        : `${trigger}詠唱数×${e.amount}の氷壁`
    case 'drawCardsPerCardPlayed':
      return ctx
        ? `${trigger}詠唱数×${e.amount}枚ドロー [現在${(e.amount ?? 0) * ctx.cardsPlayed}]`
        : `${trigger}詠唱数×${e.amount}枚ドロー`
    case 'addAether':
      return `${trigger}霊気+${e.amount}`
    case 'discountNext':
      return `${trigger}次にプレイするカードのコスト-${e.amount}`
    case 'applyBurn':
      return `${trigger}${aoe}延焼+${e.amount}`
    case 'shatterBlock':
      return `${trigger}${aoe || '敵の'}ブロックを全て粉砕する`
    case 'confuse':
      return `${trigger}敵1体に混乱+${e.amount}（攻撃が仲間に向かう）`
    case 'dealDamageRandom':
      return `${trigger}⚔️ ${(e.amount ?? 0) + atkBonus}〜${(e.amountMax ?? 0) + atkBonus}ダメージ(ランダム)${pierce}${atkBonus > 0 ? `（補正+${atkBonus}込み）` : ''}`
    case 'impulseDraw':
      return `${trigger}衝動${e.amount}枚（山札の上から。このターン限り）`
    case 'loseHp':
      return `${trigger}HPを${e.amount}失う`
    case 'dischargeAether':
      return ctx
        ? `${trigger}霊気×${e.amount}ダメージを与え、霊気を全て放出する [現在${(e.amount ?? 0) * ctx.aether + atkBonus}]`
        : `${trigger}霊気×${e.amount}ダメージを与え、霊気を全て放出する`
    case 'gainEnergy':
      return `${trigger}このターン、エナジー+${e.amount}`
    case 'gainEnergyMax':
      return `${trigger}エナジー上限+${e.amount}(次のターンから)`
    case 'addGrowth':
      return `${trigger}成長+${e.amount}`
    case 'doubleGrowth':
      return ctx && ctx.growth > 0
        ? `${trigger}成長を2倍にする [${ctx.growth}→${ctx.growth * 2}]`
        : `${trigger}成長を2倍にする`
    case 'addMomentum':
      return `${trigger}勢い+${e.amount}`
    case 'drawCards':
      return `${trigger}${e.amount}枚ドロー`
    case 'negate':
      return `${trigger}行動を打ち消す`
    case 'script':
      return `${trigger}[script:${e.scriptId}]`
  }
}

/** 効果列を行の配列に変換。連続する同一行は「×N」にまとめる (多段ヒット対策) */
function effectItems(effects: readonly DeclarativeEffect[], ctx?: EffectCtx): string[] {
  const lines: string[] = []
  const counts: number[] = []
  for (const e of effects) {
    const text = renderEffectItem(e, ctx)
    if (lines.length > 0 && lines[lines.length - 1] === text) {
      counts[counts.length - 1] += 1
    } else {
      lines.push(text)
      counts.push(1)
    }
  }
  return lines.map((l, i) => (counts[i] > 1 ? `${l} ×${counts[i]}` : l))
}

/** カード全体を行の配列に変換 (カード枠は1行ずつ改行表示する) */
function effectLineStrings(def: CardDef, ctx?: EffectCtx): string[] {
  // 負傷 (状態異常カード): 効果を持たない死に札
  if (def.id === 'status_wound') return ['使えない（ターン終了時に捨てられる）']
  const lines: string[] = []
  if ((def.discardCost ?? 0) > 0) lines.push(`追加コスト: 手札${def.discardCost}枚を捨てる`)
  if (def.modes && def.modes.length > 0) {
    def.modes.forEach((m, i) => lines.push(`選択${i + 1}: ${effectItems(m.effects, ctx).join('、')}`))
  } else {
    lines.push(...effectItems(def.effects, ctx))
  }
  if (def.exhaust) lines.push('消滅')
  return lines
}

/** インライン (文章中) 用: 1行に結合 */
function effectText(def: CardDef, ctx?: EffectCtx): string {
  return effectLineStrings(def, ctx).join(' / ')
}

/** カード枠用: 1効果1行 + キーワード吹き出し */
function EffectLines({ def, ctx }: { def: CardDef; ctx?: EffectCtx }) {
  return (
    <>
      {effectLineStrings(def, ctx).map((line, i) => (
        <div key={i}>{kw(line)}</div>
      ))}
    </>
  )
}

const STATUS_LABEL: Record<string, string> = { weak: '弱体', vulnerable: '脆弱', wound: '負傷' }

/** 状態異常の付与予告 (意図表示に出す = フェアネス。確定済みルール表「状態異常」) */
function inflictSuffix(intent: EnemyIntent): string {
  if (!intent.inflict) return ''
  return ` ＋${STATUS_LABEL[intent.inflict.status]}${intent.inflict.amount}`
}

/** 敵の意図表示。burn を渡すと攻撃の幅に威嚇 (延焼の怯み) を反映する */
function intentText(intent: EnemyIntent | null, burn = 0): string {
  if (!intent) return '---'
  switch (intent.kind) {
    case 'attack': {
      const min = intimidatedActionValue('attack', intent.shownMin, burn)
      const max = intimidatedActionValue('attack', intent.shownMax, burn)
      const hits = (intent.hits ?? 1) > 1 ? `×${intent.hits}` : ''
      const reduction = intent.shownMax - max
      const mark = reduction > 0 ? `（威嚇で-${reduction}）` : ''
      return `⚔️ 攻撃 ${min}〜${max}${hits}${mark}${inflictSuffix(intent)}`
    }
    case 'defend':
      return `🛡️ 防御 ${intent.shownMin}〜${intent.shownMax}`
    case 'destroy-set':
      return '💥 伏せ破壊'
    case 'buff':
      return `💪 強化 +${intent.shownMin}〜${intent.shownMax}`
    case 'rally':
      return `📣 応援 +${intent.shownMin}〜${intent.shownMax}（味方全体）`
    case 'hex':
      return `🧿 呪い${inflictSuffix(intent)}`
  }
}

/** 誘発確認ウィンドウ用: 敵の行動は確定済みなので実値を公開する (確定済みルール「誘発確認時の情報」) */
function confirmedIntentText(intent: EnemyIntent | null, burn = 0): string {
  if (!intent) return '---'
  switch (intent.kind) {
    case 'attack': {
      const actual = intimidatedActionValue('attack', intent.actual, burn)
      const hits = (intent.hits ?? 1) > 1 ? `×${intent.hits}` : ''
      const reduction = intent.actual - actual
      const mark = reduction > 0 ? `・威嚇で-${reduction}適用済` : ''
      return `⚔️ 攻撃 ${actual}${hits}（宣言 ${intent.shownMin}〜${intent.shownMax}${mark}）${inflictSuffix(intent)}`
    }
    case 'defend':
      return `🛡️ 防御 ${intent.actual}（宣言 ${intent.shownMin}〜${intent.shownMax}）`
    case 'destroy-set':
      return '💥 伏せ破壊'
    case 'buff':
      return `💪 強化 +${intent.actual}（宣言 +${intent.shownMin}〜+${intent.shownMax}）`
    case 'rally':
      return `📣 応援 +${intent.actual}（味方全体。宣言 +${intent.shownMin}〜+${intent.shownMax}）`
    case 'hex':
      return `🧿 呪い${inflictSuffix(intent)}`
  }
}

function cardName(cardId: string): string {
  return getCardDef(cardId).name
}

interface LogLine {
  text: string
  cls: string
}

/**
 * 直前の敵フェーズの被害サマリー (ログを見なくても被ダメが分かるように盤面へ常設表示)。
 * 前回の TurnEnded 〜 最新の TurnStarted の間のイベントを集計する
 */
function lastEnemyPhaseSummary(
  log: readonly GameEvent[],
): { dealt: number; hpLoss: number; statuses: string[] } | null {
  let started = -1
  for (let i = log.length - 1; i >= 0; i--) {
    if (log[i].type === 'TurnStarted') {
      started = i
      break
    }
  }
  if (started <= 0) return null
  let ended = -1
  for (let i = started - 1; i >= 0; i--) {
    if (log[i].type === 'TurnEnded') {
      ended = i
      break
    }
  }
  if (ended < 0) return null
  let dealt = 0
  let hpLoss = 0
  const statuses: string[] = []
  for (let i = ended; i < started; i++) {
    const e = log[i]
    if (e.type === 'DamageDealt' && e.source === 'enemy') {
      dealt += e.amount
      hpLoss += e.hpLoss
    }
    if (e.type === 'StatusInflicted') statuses.push(`${STATUS_LABEL[e.status]}${e.amount}`)
  }
  return { dealt, hpLoss, statuses }
}

function logLine(e: GameEvent): LogLine | null {
  switch (e.type) {
    case 'CombatStarted':
      return { text: `戦闘開始: ${encounterName(e.enemyId)}`, cls: 'log-turn' }
    case 'TurnStarted':
      return { text: `─── ターン ${e.turn} ───`, cls: 'log-turn' }
    case 'TurnEnded':
      return { text: 'ターン終了 → 敵の行動', cls: 'log-line' }
    case 'CardsDrawn':
      return { text: `${e.count}枚ドロー`, cls: 'log-line' }
    case 'CardPlayed':
      return { text: `プレイ: ${cardName(e.cardId)}`, cls: 'log-line' }
    case 'CardSet':
      return { text: `伏せた: ${cardName(e.cardId)}`, cls: 'log-line' }
    case 'EnemyIntentDeclared':
      return { text: `敵の意図: ${intentText(e.intent)}`, cls: 'log-line' }
    case 'EnemyActionExecuting':
    case 'EnemyActionResolved':
      return null
    case 'ActionNegated':
      return { text: '敵の行動は打ち消された！', cls: 'log-good' }
    case 'DamageDealt':
      return e.source === 'player'
        ? { text: `敵に${e.amount}ダメージ (HP減 ${e.hpLoss})`, cls: 'log-line' }
        : { text: `敵の攻撃${e.amount} → HP減 ${e.hpLoss}`, cls: 'log-bad' }
    case 'BlockGained':
      return { text: `${e.target === 'player' ? '自分' : '敵'}がブロック+${e.amount}`, cls: 'log-line' }
    case 'StrengthGained':
      return { text: `敵が強化 +${e.amount}（以降の攻撃に加算）`, cls: 'log-bad' }
    case 'IceBlockGained':
      return { text: `氷壁+${e.amount}（持ち越しブロック）`, cls: 'log-line' }
    case 'AetherGained':
      return { text: `霊気+${e.amount}`, cls: 'log-good' }
    case 'AetherDischarged':
      return { text: `霊気${e.spent}を全て放出！`, cls: 'log-good' }
    case 'DiscountGained':
      return { text: `次にプレイするカードのコスト-${e.amount}`, cls: 'log-line' }
    case 'BurnApplied':
      return { text: `敵に延焼+${e.amount}`, cls: 'log-good' }
    case 'BurnTick':
      return { text: `延焼で敵に${e.amount}ダメージ`, cls: 'log-good' }
    case 'StatusInflicted':
      return {
        text:
          e.status === 'wound'
            ? `負傷${e.amount}枚が捨て札に混入した`
            : `${STATUS_LABEL[e.status]}${e.amount}を付与された`,
        cls: 'log-bad',
      }
    case 'RegenTicked':
      return { text: `敵は再生でHP+${e.amount}`, cls: 'log-bad' }
    case 'EnemyConfused':
      return { text: `敵に混乱+${e.amount}（攻撃が仲間に向かう）`, cls: 'log-good' }
    case 'ConfusedAttack':
      return {
        text:
          e.enemyIndex === e.targetIndex
            ? `混乱した敵は自分自身に${e.amount}ダメージ！`
            : `仲間割れ！ 混乱した敵が味方に${e.amount}ダメージ`,
        cls: 'log-good',
      }
    case 'BlockShattered':
      return { text: `敵のブロック${e.amount}を粉砕！`, cls: 'log-good' }
    case 'ImpulseDrawn':
      return { text: `衝動${e.count}枚（このターン限り）`, cls: 'log-line' }
    case 'HpLost':
      return { text: `自傷でHP-${e.amount}`, cls: 'log-bad' }
    case 'EnergyGained':
      return { text: `エナジー+${e.amount}（このターン）`, cls: 'log-line' }
    case 'MomentumAdded':
      return { text: `勢い+${e.amount}`, cls: 'log-good' }
    case 'PermanentPlayed':
      return { text: `置物を設置: ${cardName(e.cardId)}`, cls: 'log-good' }
    case 'CardExhausted':
      return { text: `消滅: ${cardName(e.cardId)}（この戦闘から除外）`, cls: 'log-line' }
    case 'CardsDiscarded':
      return { text: `コストとして捨てた: ${e.cardIds.map(cardName).join('、')}`, cls: 'log-line' }
    case 'EnergyMaxGained':
      return { text: `エナジー上限+${e.amount}`, cls: 'log-line' }
    case 'GrowthAdded':
      return { text: `成長+${e.amount}`, cls: 'log-good' }
    case 'ReactionTriggered':
      return { text: `リアクション発動: ${cardName(e.cardId)}`, cls: 'log-good' }
    case 'ReactionWhiffed':
      return { text: `空振り: ${cardName(e.cardId)}`, cls: 'log-line' }
    case 'SetCardDestroyed':
      return { text: `伏せカード破壊: ${cardName(e.cardId)}`, cls: 'log-bad' }
    case 'EnemyPhaseEnded':
      return null
    case 'CombatEnded':
      return e.result === 'won'
        ? { text: '=== 勝利 ===', cls: 'log-good' }
        : { text: '=== 敗北 ===', cls: 'log-bad' }
  }
}

// ---- 小物コンポーネント ----

function Bar({ value, max, green }: { value: number; max: number; green?: boolean }) {
  const pct = max > 0 ? Math.max(0, Math.min(100, (value / max) * 100)) : 0
  return (
    <div className="bar">
      <div className={`bar-fill${green ? ' bar-green' : ''}`} style={{ width: `${pct}%` }} />
      <span className="bar-label">
        {value}/{max}
      </span>
    </div>
  )
}

function EnergyOrbs({ energy, energyMax }: { energy: number; energyMax: number }) {
  // 一時マナで energy が energyMax を超えることがある (例: 5/3) ので多い方まで描く
  return (
    <span>
      {Array.from({ length: Math.max(energy, energyMax) }, (_, i) => (
        <span key={i} className={`orb ${i < energy ? 'orb-filled' : 'orb-empty'}`} />
      ))}
    </span>
  )
}

/**
 * カードの視覚アクセント用の役割 (UI専用の導出。タイプ体系には影響しない)。
 * 攻撃系=赤の左帯 / 防御系=青の左帯。両用 (絡み蔦等) は攻撃色を優先
 */
function uiCardRole(def: CardDef): 'attack' | 'defend' | 'other' {
  const all = [...def.effects, ...(def.modes ?? []).flatMap((m) => m.effects)]
  const hasAtk = all.some(
    (e) => isDamageEffect(e) || e.effect === 'applyBurn' || e.effect === 'confuse',
  )
  if (hasAtk) return 'attack'
  const hasDef = all.some((e) =>
    ['gainBlock', 'gainIceBlock', 'gainIceBlockPerCardPlayed'].includes(e.effect),
  )
  return hasDef ? 'defend' : 'other'
}

function CardFrame({
  card,
  dim,
  actions,
  hint,
  ctx,
  displayCost,
}: {
  card: CardInstance
  dim: boolean
  actions: React.ReactNode
  hint?: string
  ctx?: EffectCtx
  /** マナ軽減適用後の実効コスト (素のコストと違う時だけ渡す) */
  displayCost?: number
}) {
  const discounted = displayCost !== undefined && displayCost !== card.def.cost
  return (
    <div className={`card card-role-${uiCardRole(card.def)}${dim ? ' card-dim' : ''}`}>
      <div className={`card-cost${discounted ? ' card-cost-discounted' : ''}`}>
        {displayCost ?? card.def.cost}
      </div>
      <div className="card-name">{card.def.name}</div>
      <div className={`card-category type-${card.def.type}`}>{TYPE_LABEL[card.def.type]}</div>
      <div className="card-text">
        <EffectLines def={card.def} ctx={ctx} />
        {hint && (
          <>
            <br />
            <span style={{ color: 'var(--muted)' }}>{hint}</span>
          </>
        )}
      </div>
      <div className="card-actions">{actions}</div>
    </div>
  )
}

// ---- セットアップ画面 ----

interface Config {
  mode: ReactionMode
  enemyId: string
  deckId: string
  leaderId?: string
  seed: number
}

/** デッキ構成の1行サマリ (例: 年輪×4 開花の儀×2 …) */
function deckComposition(deckId: string): string {
  return getDeckDef(deckId)
    .cards.map((e) => `${getCardDef(e.cardId).name}×${e.count}`)
    .join(' ')
}

function SetupScreen({
  onStart,
  onStartRun,
}: {
  onStart: (cfg: Config) => void
  onStartRun: (seed: number, leaderId: string) => void
}) {
  const [enemyId, setEnemyId] = useState(allEnemies[0].id)
  const [leaderId, setLeaderId] = useState(allLeaders[0].id)
  const leader = getLeaderDef(leaderId)
  const allowedDecks = allDecks.filter((d) => deckAllowedForLeader(leader, d))
  const [deckId, setDeckId] = useState(allowedDecks[0].id)
  const [seedInput, setSeedInput] = useState('')
  const parseSeed = () =>
    /^\d+$/.test(seedInput) ? Number(seedInput) >>> 0 : Date.now() % 2 ** 32
  // リーダー変更で使用可能デッキ外を選んでいたら先頭に戻す
  const effectiveDeckId = allowedDecks.some((d) => d.id === deckId) ? deckId : allowedDecks[0].id
  return (
    <div className="app setup">
      <h1>deck-rogue-proto</h1>
      <div className="panel">
        <div className="choice-title">採用方式: set-confirm（伏せ+発動/温存の選択）</div>
        <div className="choice-desc">
          リアクションはコスト事前払いで伏せる。敵の行動が確定したら（実値公開後）、発動するか温存するかを選ぶ。
        </div>
      </div>

      <div className="setup-section-title">リーダー（色アイデンティティ＝使える色。統率者方式）</div>
      <div className="choice-row">
        {allLeaders.map((l) => (
          <button
            key={l.id}
            className={`choice${leaderId === l.id ? ' choice-selected' : ''}`}
            onClick={() => setLeaderId(l.id)}
          >
            <div className="choice-title">
              <span className="choice-sprite">{l.sprite}</span>
              {l.name}（{l.colors.map((c) => COLOR_LABEL[c]).join('')}）
            </div>
            <div className="choice-desc">
              HP {l.maxHp} / ドロー{l.drawPerTurn}枚 / ピック候補{l.rewardChoices}枚
            </div>
            <div className="choice-desc">{l.description}</div>
          </button>
        ))}
      </div>

      <div className="panel" style={{ marginTop: 16 }}>
        <div className="choice-title">🏕 ドラフト連戦（{RUN_BATTLES}戦ラン）</div>
        <div className="choice-desc">
          {leader.name}の基本10枚から出発し、勝利ごとに{leader.colors.map((c) => COLOR_LABEL[c]).join('')}
          の{leader.rewardChoices}枚から1枚ピックして構築。敵は段階制でだんだん強くなり、HPは持ち越し。
        </div>
        <button
          className="btn btn-primary"
          style={{ marginTop: 8 }}
          onClick={() => onStartRun(parseSeed(), leaderId)}
        >
          {leader.sprite} {leader.name}でランを開始
        </button>
      </div>

      <div className="setup-section-title">── 以下は単発戦闘（デッキ・敵を指定して1戦） ──</div>
      <div className="setup-section-title">デッキ（リーダーの色で使えるもののみ）</div>
      <div className="choice-row">
        {allowedDecks.map((d) => (
          <button
            key={d.id}
            className={`choice${effectiveDeckId === d.id ? ' choice-selected' : ''}`}
            onClick={() => setDeckId(d.id)}
          >
            <div className="choice-title">
              {COLOR_LABEL[d.color]} {d.name}（{deckSize(d)}枚）
            </div>
            <div className="choice-desc">{d.description}</div>
            <div className="choice-desc">{deckComposition(d.id)}</div>
          </button>
        ))}
      </div>
      <div className="setup-section-title">敵類型</div>
      <div className="choice-row">
        {allEnemies.map((e) => (
          <button
            key={e.id}
            className={`choice${enemyId === e.id ? ' choice-selected' : ''}`}
            onClick={() => setEnemyId(e.id)}
          >
            <div className="choice-title">
              <span className="choice-sprite">{ARCHETYPE_SPRITE[e.archetype]}</span>
              {e.name}
            </div>
            <div className="choice-desc">
              {ARCHETYPE_LABEL[e.archetype]} / HP {e.maxHp}
              {e.flavor && (
                <>
                  <br />
                  {e.flavor}
                </>
              )}
            </div>
          </button>
        ))}
      </div>
      <div className="setup-section-title">編成（複数体戦闘）</div>
      <div className="choice-row">
        {allEncounters.map((enc) => (
          <button
            key={enc.id}
            className={`choice${enemyId === enc.id ? ' choice-selected' : ''}`}
            onClick={() => setEnemyId(enc.id)}
          >
            <div className="choice-title">
              <span className="choice-sprite">
                {enc.members.map((m) => ARCHETYPE_SPRITE[getEnemyDef(m.enemyId).archetype]).join('')}
              </span>
              {enc.name}
            </div>
            <div className="choice-desc">
              {enc.members.map((m) => getEnemyDef(m.enemyId).name).join(' + ')}
            </div>
          </button>
        ))}
      </div>
      <div className="setup-section-title">シード（空欄なら時刻から生成。同じシード=同じ展開）</div>
      <input
        className="seed-input"
        value={seedInput}
        onChange={(ev) => setSeedInput(ev.target.value)}
        size={14}
        placeholder="例: 42"
      />
      <div style={{ marginTop: 20 }}>
        <button
          className="btn btn-primary btn-endturn"
          onClick={() => onStart({ mode: ADOPTED_MODE, enemyId, deckId: effectiveDeckId, leaderId, seed: parseSeed() })}
        >
          ⚔️ 戦闘開始
        </button>
      </div>
    </div>
  )
}

// ---- 戦闘画面 ----

function BattleScreen({
  state: s,
  config,
  dispatch,
  onRestart,
  onBack,
  extraChip,
  backLabel,
}: {
  state: GameState
  config: Config
  dispatch: (c: Command) => void
  onRestart: (seed: number) => void
  onBack: () => void
  extraChip?: string
  backLabel?: string
}) {
  const player = s.player
  const system = getReactionSystem(s.reactionMode)
  const isSetMode = s.reactionMode !== 'hold-manual'
  const ended = s.phase === 'won' || s.phase === 'lost'
  const aliveCount = s.enemies.filter((e) => e.hp > 0).length
  // 誘発確認ウィンドウの対象敵 (pendingWindow の enemyIndex)
  const windowEnemy = s.pendingWindow ? s.enemies[s.pendingWindow.enemyIndex] : s.enemies[0]
  // 手札捨てコストの選択中状態 (UIローカル。対象カードが手札を離れたら自動で無効化)
  const [pendingDiscard, setPendingDiscard] = useState<{
    cardUid: string
    modeIndex?: number
  } | null>(null)
  const activeDiscard =
    pendingDiscard &&
    s.phase === 'player-turn' &&
    player.hand.some((c) => c.uid === pendingDiscard.cardUid)
      ? pendingDiscard
      : null
  // StS式ターゲティング: 単体対象カードのプレイ時、敵タップ待ちの状態
  const [pendingTarget, setPendingTarget] = useState<{
    cardUid: string
    modeIndex?: number
    discardUids?: string[]
  } | null>(null)
  const activeTarget =
    pendingTarget &&
    s.phase === 'player-turn' &&
    player.hand.some((c) => c.uid === pendingTarget.cardUid)
      ? pendingTarget
      : null
  // 対象が要るカードなら敵タップ待ちへ、不要なら即プレイ
  const playOrTarget = (cardUid: string, modeIndex?: number, discardUids?: string[]) => {
    const card = player.hand.find((c) => c.uid === cardUid)
    if (card && aliveCount > 1 && cardNeedsTarget(card, modeIndex)) {
      setPendingTarget({ cardUid, modeIndex, discardUids })
    } else {
      dispatch({ type: 'PlayCard', cardUid, modeIndex, discardUids })
    }
  }
  const lines = s.eventLog.map(logLine).filter((l): l is LogLine => l !== null)
  const setCard = player.setCards[0]

  return (
    <div className="app battle">
      {/* 上部バー */}
      <div className="area-topbar">
        <span className="topbar-title">
          <span className="chip chip-mode">{s.reactionMode}</span>
          {config.leaderId && (
            <span className="chip">
              {getLeaderDef(config.leaderId).sprite} {getLeaderDef(config.leaderId).name}
            </span>
          )}
          {extraChip ? (
            <span className="chip">{extraChip}</span>
          ) : (
            <span className="chip">{getDeckDef(config.deckId).name}</span>
          )}
          <span className="chip">ターン {s.turn}</span>
          <span className="chip">seed {config.seed}</span>
        </span>
        <button className="btn" onClick={onBack}>
          {backLabel ?? '設定に戻る'}
        </button>
      </div>

      {/* 敵ゾーン (1〜3体)。ターゲット選択中は敵をタップして対象決定 */}
      <div className="panel area-enemy">
        {activeTarget && (
          <div className="discard-banner">
            「{player.hand.find((c) => c.uid === activeTarget.cardUid)?.def.name}
            」の対象を選んでください（敵をタップ）{' '}
            <button className="btn" onClick={() => setPendingTarget(null)}>
              キャンセル
            </button>
          </div>
        )}
        <div className="enemy-zone">
          {s.enemies.map((enemy, i) => {
            const enemyDef = getEnemyDef(enemy.enemyId)
            const dead = enemy.hp <= 0
            const targetable = activeTarget !== null && !dead
            return (
              <div
                key={i}
                className={`enemy-card${targetable ? ' enemy-targetable' : ''}${dead ? ' enemy-dead' : ''}`}
                onClick={() => {
                  if (!targetable || !activeTarget) return
                  dispatch({
                    type: 'PlayCard',
                    cardUid: activeTarget.cardUid,
                    modeIndex: activeTarget.modeIndex,
                    discardUids: activeTarget.discardUids,
                    targetIndex: i,
                  })
                  setPendingTarget(null)
                }}
              >
                <div className="enemy-sprite">{dead ? '💀' : ARCHETYPE_SPRITE[enemyDef.archetype]}</div>
                <div className="enemy-info">
                  <div className="enemy-name">{enemyDef.name}</div>
                  {s.enemies.length === 1 && (
                    <div className="enemy-archetype">
                      {ARCHETYPE_LABEL[enemyDef.archetype]}
                      {enemyDef.flavor && <> — {enemyDef.flavor}</>}
                    </div>
                  )}
                  <Bar value={Math.max(0, enemy.hp)} max={enemy.maxHp} />
                  <div style={{ marginTop: 6 }}>
                    {enemy.block > 0 && (
                      <span className="chip chip-block">🛡 ブロック {enemy.block}</span>
                    )}
                    {enemy.strength > 0 && (
                      <span className="chip chip-strength">💪 {kw('強化')} +{enemy.strength}</span>
                    )}
                    {enemy.burn > 0 && (
                      <span className="chip chip-strength">🔥 {kw('延焼')} {enemy.burn}</span>
                    )}
                    {enemy.confusion > 0 && (
                      <span className="chip chip-aether">😵‍💫 {kw('混乱')} {enemy.confusion}</span>
                    )}
                    {enemyDef.regen !== undefined && enemy.hp > enemy.maxHp * 0.5 && !dead && (
                      <span className="chip chip-strength">♻️ {kw('再生')} +{enemyDef.regen}</span>
                    )}
                    {(enemyDef.movesBelowHalf || enemyDef.sequenceBelowHalf) &&
                      enemy.hp <= enemy.maxHp * 0.5 &&
                      !dead && <span className="chip chip-strength">😾 牙をむいている</span>}
                    {enemyDef.enrage !== undefined && !dead && (
                      <span className="chip chip-strength">😡 {kw('激昂')} +{enemyDef.enrage}/T</span>
                    )}
                  </div>
                  {!ended && !dead && (
                    <div className={`intent${enemy.intent?.kind === 'defend' ? ' intent-defend' : ''}`}>
                      {kw(intentText(enemy.intent, enemy.burn))}
                    </div>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      </div>

      {/* 中央フィールド: 伏せ場 / 割り込み / 勝敗 */}
      <div className="panel area-field">
        <div className="field">
          {isSetMode && (
            <div>
              {setCard ? (
                <>
                  <div className="card card-back">伏</div>
                  <div className="set-slot-label">{setCard.def.name}</div>
                </>
              ) : (
                <>
                  <div className="set-slot-empty">伏せ場</div>
                  <div className="set-slot-label">（なし）</div>
                </>
              )}
            </div>
          )}

          {player.permanents.length > 0 && (
            <div className="permanents">
              <div className="stat-label">置物</div>
              {player.permanents.map((c) => (
                <div key={c.uid} className="permanent">
                  <b>{c.def.name}</b>
                  <EffectLines
                    def={c.def}
                    ctx={{ growth: player.growth, momentum: player.momentum, energyMax: player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether }}
                  />
                </div>
              ))}
            </div>
          )}

          {s.phase === 'awaiting-reaction' && (
            <div className="window-panel">
              <div className="window-title">
                {s.pendingWindow?.stage === 'post' ? '敵の行動（解決済み）: ' : '敵の行動（確定）: '}
                {s.enemies.length > 1 && windowEnemy && (
                  <>{getEnemyDef(windowEnemy.enemyId).name}の </>
                )}
                {kw(confirmedIntentText(windowEnemy?.intent ?? null, windowEnemy?.burn ?? 0))}
              </div>
              {s.reactionMode === 'set-confirm' && setCard ? (
                <>
                  <div style={{ marginBottom: 10 }}>
                    「{setCard.def.name}」（
                    {kw(
                      effectText(setCard.def, {
                        growth: player.growth,
                        momentum: player.momentum,
                        energyMax: player.energyMax,
                        cardsPlayed: player.cardsPlayedThisTurn,
                        aether: player.aether,
                      }),
                    )}
                    ）を——
                  </div>
                  <button
                    className="btn btn-primary"
                    onClick={() => dispatch({ type: 'ConfirmReaction', fire: true })}
                  >
                    発動する
                  </button>{' '}
                  <button className="btn" onClick={() => dispatch({ type: 'ConfirmReaction', fire: false })}>
                    温存する
                  </button>
                </>
              ) : (
                <>
                  <div style={{ marginBottom: 10 }}>
                    手札から発動（残エナジー <EnergyOrbs energy={player.energy} energyMax={player.energyMax} />）
                  </div>
                  {playableReactions(s).map((c) => (
                    <button
                      key={c.uid}
                      className="btn btn-primary"
                      style={{ margin: 3 }}
                      onClick={() => dispatch({ type: 'ReactManual', cardUid: c.uid })}
                    >
                      {c.def.name}({c.def.cost}) —{' '}
                      {effectText(c.def, { growth: player.growth, momentum: player.momentum, energyMax: player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether })}
                    </button>
                  ))}
                  <button
                    className="btn"
                    style={{ margin: 3 }}
                    onClick={() => dispatch({ type: 'ConfirmReaction', fire: false })}
                  >
                    パス
                  </button>
                </>
              )}
            </div>
          )}

          {ended && (
            <div className="result-panel">
              <div className={`result-title ${s.phase === 'won' ? 'result-won' : 'result-lost'}`}>
                {s.phase === 'won' ? '勝利！' : '敗北…'}
              </div>
              <button className="btn btn-primary" onClick={() => onRestart(Date.now() % 2 ** 32)}>
                新シードで再戦
              </button>{' '}
              <button className="btn" onClick={() => onRestart(config.seed)}>
                同シードで再戦
              </button>
            </div>
          )}
        </div>
      </div>

      {/* プレイヤーステータス */}
      <div className="panel area-player">
        <div className="player-row">
          <div className="player-hp">
            <div className="stat-label">プレイヤー HP</div>
            <Bar value={Math.max(0, player.hp)} max={player.maxHp} green />
            {(() => {
              const sum = lastEnemyPhaseSummary(s.eventLog)
              if (!sum) return null
              const st = sum.statuses.length > 0 ? `・${sum.statuses.join('・')}` : ''
              return (
                <div className={`phase-summary${sum.hpLoss > 0 ? ' phase-summary-bad' : ''}`}>
                  {sum.dealt > 0
                    ? `🩸 前の敵ターン: HP-${sum.hpLoss}（被弾${sum.dealt}・軽減${sum.dealt - sum.hpLoss}）${st}`
                    : `✅ 前の敵ターン: 被弾なし${st}`}
                </div>
              )
            })()}
          </div>
          <div>
            <div className="stat-label">エナジー</div>
            <EnergyOrbs energy={player.energy} energyMax={player.energyMax} />
          </div>
          <div>
            {player.block > 0 && <span className="chip chip-block">🛡 ブロック {player.block}</span>}
            {player.iceBlock > 0 && (
              <span className="chip chip-block">🧊 {kw('氷壁')} {player.iceBlock}</span>
            )}
            {player.growth > 0 && (
              <span className="chip chip-growth">🌿 {kw('成長')} +{player.growth}</span>
            )}
            {player.momentum > 0 && (
              <span className="chip chip-momentum">🐘 {kw('勢い')} +{player.momentum}</span>
            )}
            {s.phase === 'player-turn' && player.cardsPlayedThisTurn > 0 && (
              <span className="chip">🌀 {kw('詠唱数')} {player.cardsPlayedThisTurn}</span>
            )}
            {player.aether > 0 && (
              <span className="chip chip-aether">⚡ {kw('霊気')} {player.aether}</span>
            )}
            {player.nextCardDiscount > 0 && (
              <span className="chip chip-aether">🔥 次のカード-{player.nextCardDiscount}</span>
            )}
            {player.weak > 0 && (
              <span className="chip chip-strength">😵 {kw('弱体')} {player.weak}</span>
            )}
            {player.vulnerable > 0 && (
              <span className="chip chip-strength">💔 {kw('脆弱')} {player.vulnerable}</span>
            )}
          </div>
          <div className="pile-info">
            山札 {player.drawPile.length} 枚
            <br />
            捨て札 {player.discardPile.length} 枚
            {player.exhaustPile.length > 0 && (
              <>
                <br />
                消滅 {player.exhaustPile.length} 枚
              </>
            )}
          </div>
        </div>
      </div>

      {/* 手札 */}
      <div className="panel area-hand">
        {activeDiscard && (
          <div className="discard-banner">
            「{player.hand.find((c) => c.uid === activeDiscard.cardUid)?.def.name}」の追加コスト:
            捨てるカードを選んでください{' '}
            <button className="btn" onClick={() => setPendingDiscard(null)}>
              キャンセル
            </button>
          </div>
        )}
        <div className="hand-row">
          <div className="hand-cards">
            {s.phase === 'player-turn' &&
              player.hand.map((c) => {
                const modes = c.def.modes ?? []
                const discardCost = c.def.discardCost ?? 0
                const effCost = effectiveCost(s, c)
                const canPlay =
                  isPlayableFromHand(c) &&
                  effCost <= player.energy &&
                  player.hand.length - 1 >= discardCost
                const canSet = isSetMode && system.canHandle(s, { type: 'SetCard', cardUid: c.uid })
                const heldReaction = !isSetMode && c.def.type === 'reaction'
                // 捨てコスト選択中: 手札は「捨てる」対象として振る舞う
                if (activeDiscard) {
                  const isSource = c.uid === activeDiscard.cardUid
                  return (
                    <CardFrame
                      key={c.uid}
                      card={c}
                      ctx={{ growth: player.growth, momentum: player.momentum, energyMax: player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether }}
                      dim={isSource}
                      hint={isSource ? 'プレイするカード' : undefined}
                      actions={
                        !isSource && (
                          <button
                            className="btn"
                            onClick={() => {
                              playOrTarget(activeDiscard.cardUid, activeDiscard.modeIndex, [c.uid])
                              setPendingDiscard(null)
                            }}
                          >
                            これを捨てる
                          </button>
                        )
                      }
                    />
                  )
                }
                const play = (modeIndex?: number) => {
                  if (discardCost > 0) setPendingDiscard({ cardUid: c.uid, modeIndex })
                  else playOrTarget(c.uid, modeIndex)
                }
                return (
                  <CardFrame
                    key={c.uid}
                    card={c}
                    ctx={{ growth: player.growth, momentum: player.momentum, energyMax: player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether }}
                    displayCost={effCost}
                    dim={!canPlay && !canSet && !heldReaction}
                    hint={
                      player.impulseUids.includes(c.uid)
                        ? '⏳ 衝動: このターン限り（未使用なら消滅）'
                        : heldReaction
                          ? '敵ターンに手札から発動'
                          : undefined
                    }
                    actions={
                      <>
                        {modes.length > 0 ? (
                          modes.map((m, i) => (
                            <button key={i} className="btn" disabled={!canPlay} onClick={() => play(i)}>
                              {m.name}
                            </button>
                          ))
                        ) : (
                          isPlayableFromHand(c) && (
                            <button className="btn" disabled={!canPlay} onClick={() => play()}>
                              プレイ
                            </button>
                          )
                        )}
                        {isSetMode && c.def.type === 'reaction' && (
                          <button
                            className="btn"
                            disabled={!canSet}
                            onClick={() => dispatch({ type: 'SetCard', cardUid: c.uid })}
                          >
                            伏せる
                          </button>
                        )}
                      </>
                    }
                  />
                )
              })}
            {s.phase === 'awaiting-reaction' && (
              <div className="pile-info" style={{ alignSelf: 'center' }}>
                敵の行動に割り込み中…（上のパネルで選択）
              </div>
            )}
          </div>
          {s.phase === 'player-turn' && (
            <button className="btn btn-primary btn-endturn" onClick={() => dispatch({ type: 'EndTurn' })}>
              ターン終了 ▶
            </button>
          )}
        </div>
      </div>

      {/* ログ (新しい順) */}
      <div className="panel area-log">
        <div className="stat-label">戦闘ログ（新しい順）</div>
        <div className="log">
          {[...lines].reverse().map((l, i) => (
            <div key={i} className={l.cls}>
              {l.text}
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

// ---- ドラフト連戦 (ラン) 画面 ----

function RunScreen({
  run,
  dispatch,
  onExit,
  onRestart,
}: {
  run: RunState
  dispatch: (c: RunCommand) => void
  onExit: () => void
  onRestart: (seed: number) => void
}) {
  const battleNo = Math.min(run.battleIndex + 1, RUN_BATTLES)
  const isBoss = run.battleIndex === RUN_BATTLES - 1
  const progressChip = `${isBoss ? '👑 ボス戦' : `戦闘 ${battleNo}/${RUN_BATTLES}`}・デッキ${run.deck.length}枚`
  const ctx = undefined

  if (run.phase === 'combat' && run.combat) {
    return (
      <BattleScreen
        state={run.combat}
        config={{
          mode: run.mode,
          enemyId: run.enemyIds[run.battleIndex],
          deckId: 'run_basic',
          leaderId: run.leaderId,
          seed: run.seed,
        }}
        dispatch={(command) => dispatch({ type: 'Combat', command })}
        onRestart={() => {}}
        onBack={onExit}
        extraChip={progressChip}
        backLabel="ランを放棄"
      />
    )
  }

  if (run.phase === 'reward') {
    return (
      <div className="app setup">
        <h1>🎴 報酬ピック</h1>
        <div className="panel">
          <span className="chip">戦闘 {battleNo}/{RUN_BATTLES} クリア</span>
          <span className="chip">HP {run.hp}/{run.maxHp}</span>
          <span className="chip">デッキ {run.deck.length}枚</span>
          <span className="chip">次: {encounterName(run.enemyIds[run.battleIndex + 1])}</span>
        </div>
        <div className="setup-section-title">1枚選んでデッキに加える（スキップ可）</div>
        <div className="hand-cards" style={{ margin: '12px 0' }}>
          {(run.rewardOptions ?? []).map((cardId, i) => (
            <CardFrame
              key={i}
              card={{ uid: `opt${i}`, def: getCardDef(cardId) }}
              dim={false}
              ctx={ctx}
              actions={
                <button className="btn btn-primary" onClick={() => dispatch({ type: 'PickReward', index: i })}>
                  獲得する
                </button>
              }
            />
          ))}
        </div>
        <button className="btn" onClick={() => dispatch({ type: 'SkipReward' })}>
          スキップして次へ
        </button>
        {run.picks.length > 0 && (
          <div className="pile-info" style={{ marginTop: 16 }}>
            これまでのピック: {run.picks.map((id) => getCardDef(id).name).join('、')}
          </div>
        )}
      </div>
    )
  }

  // won / lost
  const won = run.phase === 'won'
  return (
    <div className="app setup">
      <h1 className={won ? 'result-won' : 'result-lost'}>
        {won ? '🏆 ラン制覇！' : `💀 ラン終了（${battleNo}戦目で敗北）`}
      </h1>
      <div className="panel">
        <div className="choice-desc">
          到達: {won ? `全${RUN_BATTLES}戦制覇` : `${run.battleIndex}戦クリア`} / seed {run.seed} /
          最終デッキ {run.deck.length}枚
        </div>
        <div className="choice-desc" style={{ marginTop: 6 }}>
          ピック履歴:{' '}
          {run.picks.length > 0 ? run.picks.map((id) => getCardDef(id).name).join('、') : '（なし）'}
        </div>
      </div>
      <div style={{ marginTop: 16 }}>
        <button className="btn btn-primary" onClick={() => onRestart(Date.now() % 2 ** 32)}>
          新シードで再挑戦
        </button>{' '}
        <button className="btn" onClick={() => onRestart(run.seed)}>
          同シードで再挑戦
        </button>{' '}
        <button className="btn" onClick={onExit}>
          設定に戻る
        </button>
      </div>
    </div>
  )
}

// ---- ルート ----

export default function App() {
  const [config, setConfig] = useState<Config | null>(null)
  const [state, setState] = useState<GameState | null>(null)
  const [run, setRun] = useState<RunState | null>(null)

  const start = (cfg: Config) => {
    setConfig(cfg)
    setState(
      applyCommand(createInitialState(cfg.seed, cfg.mode), {
        type: 'StartCombat',
        seed: cfg.seed,
        enemyId: cfg.enemyId,
        deckId: cfg.deckId,
        leaderId: cfg.leaderId,
      }),
    )
  }

  const dispatch = (command: Command) => {
    setState((prev) => {
      if (!prev) return prev
      try {
        return applyCommand(prev, command)
      } catch (err) {
        // ボタンの活性制御が正しければ来ないはず。来たら実装ミスなので見えるようにする
        alert(err instanceof Error ? err.message : String(err))
        return prev
      }
    })
  }

  const dispatchRun = (command: RunCommand) => {
    setRun((prev) => {
      if (!prev) return prev
      try {
        return applyRunCommand(prev, command)
      } catch (err) {
        alert(err instanceof Error ? err.message : String(err))
        return prev
      }
    })
  }

  if (run !== null) {
    return (
      <RunScreen
        run={run}
        dispatch={dispatchRun}
        onExit={() => setRun(null)}
        onRestart={(seed) => setRun((prev) => createRun(seed, ADOPTED_MODE, prev?.leaderId ?? 'leader_green'))}
      />
    )
  }
  if (state === null || config === null) {
    return <SetupScreen onStart={start} onStartRun={(seed, leaderId) => setRun(createRun(seed, ADOPTED_MODE, leaderId))} />
  }
  return (
    <BattleScreen
      state={state}
      config={config}
      dispatch={dispatch}
      onRestart={(seed) => start({ ...config, seed })}
      onBack={() => {
        setState(null)
        setConfig(null)
      }}
    />
  )
}
