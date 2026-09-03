// ui/ は状態を読んでコマンドを投げるだけの薄い層。ゲームロジックを書かない (CLAUDE.md)。
// 見た目は静的なゲーム風UI (StS風配置・ダーク)。動く演出はやらない (CLAUDE.md「UIの見た目の方針」)。
import { deckChooseKindOf } from '../engine/combat.ts'
import { canUpgradeInHand } from '../engine/upgrade.ts'
import { canSetAsNormal, setFireCost, setWindowStage } from '../engine/setany.ts'
import { canSetCard } from '../engine/reactions/set-base.ts'
import { useEffect, useMemo, useRef, useState } from 'react'
import type { ReactElement } from 'react'
import {
  archiveBattle,
  cardName,
  inflictSuffix,
  intentText,
  logLine,
  buildReport,
  cardDraftToDefJson,
  dataFingerprint,
  ENEMY_TOP_FIELDS,
  LEADER_TOP_FIELDS,
  isEmptyMark,
  isEmptySimpleMark,
  buildOverrideDefs,
  describeRunChoice,
  replayStates,
  saveProposals,
  saveReport,
  saveRunFile,
  STATUS_LABEL,
  type BattleArchive,
  type BattleRating,
  type CardDraft,
  type CardProposalMark,
  type EffectDraft,
  type EnemyDraft,
  type EnemyMoveDraft,
  type LeaderDraft,
  type LogLine,
  type PlayNote,
  type ProposalBundle,
  type RelicDraft,
  type RunChoice,
  type RunJournal,
  type RunSaveFile,
  type SimpleMark,
} from './report.ts'
import {
  allCards,
  allDecks,
  allEncounters,
  allEnemies,
  allRelics,
  applyDebugOverrides,
  clearDebugOverrides,
  debugOverridesActive,
  allLeaders,
  deckAllowedForLeader,
  deckSize,
  encounterName,
  getEventDef,
  getCardDef,
  getDeckDef,
  getEnemyDef,
  getLeaderDef,
  getRelicDef,
} from '../engine/content.ts'
import { BLAZE_THRESHOLD, cardNeedsTarget, damageBreakdown, effectiveCost, effectiveIntent, isDamageEffect, isPlayableFromHand, playerCanSet, setBranchFlipRisks, setReactionIgnoresFreshness, usableSetCards, windowFromPending } from '../engine/effects.ts'
import { playableReactions } from '../engine/reactions/hold-manual.ts'
import { applyRunCommand, canUpgradeCard, createDebugCheckpointRun, createRun, currentNode, DEFAULT_DIFFICULTY, DIFFICULTY_TABLE, eventChoiceNeedsCard, isUpgraded, nextChoices, shopRemovalPrice, shopUpgradePrice, upgradeCard, workshopFusePrice } from '../engine/run.ts'
import { battleSummary, cardCostLabel, relicRarityTag, setBranchNote, summaryLine, turnsUntilHatch, worstIncomingFrom, worstIncomingTotal, xHitsSuffix } from '../engine/summary.ts'
import { GRID_COLS } from '../engine/map.ts'
import type { MapNode, MapNodeType } from '../engine/map.ts'
import { fuseBlockReason, fuseCards } from '../engine/fusion.ts'
import type { RunCommand, RunState } from '../engine/run.ts'
import { applyCommand, createInitialState } from '../engine/state.ts'
import { RESTRAIN_PLAY_CAP, startCombatWithOptions } from '../engine/combat.ts'
import type {
  CardColor,
  CardType,
  CardDef,
  CardInstance,
  Command,
  DeclarativeEffect,
  EnemyArchetype,
  EnemyDef,
  EnemyIntent,
  EnemyMove,
  GameEvent,
  GameState,
  LeaderDef,
  ReactionMode,
  RelicDef,
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
  brute: '脳筋型（筋力ループ）',
  charger: 'チャージ型（大技予告）',
  hexer: '妖術師型（状態異常）',
  flurry: '連撃型',
  regenerator: '再生型（HP半分で豹変）',
  taunter: '挑発型（伏せ無しに大振り）',
  enrager: '激昂型（毎ターン筋力+）',
  support: '応援型（味方全体の筋力+）',
  thorned: 'とげ型（攻撃ヒットごとに反射）',
  thief: '盗人型（盗んで逃げる）',
  bomber: '爆弾型（三拍子の大爆発）',
  healer: '回復役型（味方を癒す）',
  windup: '息切れ型（大技のあと隙）',
  shell: '甲殻型（積みながら殴る）',
  splitter: '分裂型（倒すと小型に分裂）',
  guardian: '護衛型（仲間への単体攻撃を庇う）',
  mimic: '物真似型（手数の鏡）',
  elite: 'エリート',
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
  mimic: '🪞',
  elite: '👑',
  thorned: '🦔',
  thief: '🪙',
  bomber: '🧨',
  healer: '🌿',
  windup: '🪓',
  shell: '🪨',
  splitter: '🫠',
  guardian: '🛡️',
}

// カードタイプの表示ラベル (2026-08-24決定。物理=武器・道具・身体/呪文=魔力の行使 → docs/card-power.md §0)
const TYPE_LABEL: Record<CardType, string> = {
  physical: '物理',
  spell: '呪文',
  reaction: 'リアクション',
  permanent: '置物',
}

const COLOR_LABEL: Record<CardColor, string> = { green: '🌿 緑', blue: '💧 青', red: '🔥 赤', white: '⚪ 白', black: '⚫ 黒' }

// ---- キーワード能力のツールチップ ----

import { KEYWORD_HELP } from './keywordHelp.ts'

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
  /** 現在のエナジー (X札の「今撃つなら」の見積り用。手札文脈だけが持つ) */
  energy?: number
  growth: number
  momentum: number
  energyMax: number
  cardsPlayed: number
  aether: number
  exhausted: number
  selfHpLost: number
  permanents: number
  damageTaken: number
  iceBlock: number
  randomPlayed: number
  handCards: number
}

const TRIGGER_LABEL: Record<CardDef['effects'][number]['trigger'], string> = {
  onPlay: '',
  onAttackIncoming: '被攻撃前: ',
  onAttacked: '被攻撃後: ',
  onCardPlayed: 'カードをプレイするたび: ',
  onBlockGained: 'ブロックを得るたび: ',
  onActionNegated: '敵の行動を打ち消すたび: ',
  onEnemyAction: '敵行動時: ',
  onEnemyBuffed: '敵強化時: ',
  onEnemyDefended: '敵防御時: ',
  onTurnStart: '毎ターン開始時: ',
  onCombatStart: '戦闘開始時: ',
  onAttackPlayed: '攻撃プレイ後: ',
  onSpellPlayed: '呪文をプレイした時: ',
  onSetDestroyed: 'この伏せが破壊された時: ',
  onHealed: 'HPが回復するたび (満タンでも誘発): ',
  onHpLost: 'カード効果でHPを失うたび: ',
  onCardExhausted: 'カードが消滅するたび: ',
  onCostExhausted: '消滅コストを支払うたび: ',
  onPermanentEntered: '置物が場に出るたび: ',
  onImpulsePlayed: '衝動カードをプレイするたび: ',
  onRandomPlayed: '運任せの札をプレイするたび: ',
  onAetherGained: '霊気を得るたび: ',
  onCardSet: 'カードを伏せるたび: ',
  onReactionFired: 'リアクションが発動するたび: ',
  onSelfExhausted: '亡骸 (この札がプレイ以外で消滅した時): ',
  onGrowthGained: '成長を得るたび: ',
  onMomentumGained: '勢いを得るたび: ',
}

/** 誘発の追加条件の表示 */
function conditionLabel(e: DeclarativeEffect): string {
  const c = e.condition
  if (!c) return ''
  const parts: string[] = []
  if (c.hpAtOrBelowRatio !== undefined) parts.push(`自分のHPが${Math.round(c.hpAtOrBelowRatio * 100)}%以下`)
  if (c.minDamageTaken !== undefined) parts.push(`${c.minDamageTaken}以上のダメージを受けた`)
  if (c.maxActionValue !== undefined) parts.push(`敵の行動の値が${c.maxActionValue}以下`)
  if (c.minActionValue !== undefined) parts.push(`敵の行動の値が${c.minActionValue}以上`)
  if (c.blaze === true) parts.push(`🔥猛り火(延焼合計${BLAZE_THRESHOLD}以上)`)
  if (c.minGrowth !== undefined) parts.push(`🌱成長${c.minGrowth}以上`)
  return parts.length > 0 ? `[${parts.join('かつ')}] ` : ''
}

function ctx2Block(e: DeclarativeEffect, _ctx: EffectCtx | undefined, trigger: string, pierce: string): string {
  return `${trigger}⚔️ ブロック×${e.amount}ダメージ${pierce}${e.spendBlock === true ? '（解決後にブロックを全て失う）' : ''}`
}

/** 効果1つを1行のテキストに変換する。忘却の刻 (しきい値) は達成状態を添えて表示する */
function renderEffectItem(e: DeclarativeEffect, ctx?: EffectCtx, holderType?: string): string {
  const t = e.exhaustThreshold
  if (t !== undefined) {
    const met = ctx !== undefined && ctx.exhausted >= t
    const shown = met ? { ...e, amount: e.amountMax } : e
    // amountMax < amount は「刻に達したら弱まる/止まる」安全弁 (冒涜の祭壇=ミル停止)。
    // 「強化」と表示すると壊れて見える (2026-09-01 黒Opusランの指摘)
    const weaker = (e.amountMax ?? 0) < (e.amount ?? 0)
    const note = weaker
      ? met
        ? `〔忘却の刻${t}: ${e.amountMax === 0 ? '停止中' : `${e.amountMax}に減少中`}〕`
        : `〔忘却の刻${t}: ${e.amountMax === 0 ? '以降は停止' : `${e.amountMax}に減少`}〕`
      : met
        ? `〔忘却の刻${t}: 発動中⚡〕`
        : `〔忘却の刻${t}: ${e.amountMax}に増える〕`
    return `${renderEffectItemCore(shown, ctx, holderType)} ${note}`
  }
  return renderEffectItemCore(e, ctx, holderType)
}

function renderEffectItemCore(e: DeclarativeEffect, ctx?: EffectCtx, holderType?: string): string {
  // 攻撃ダメージには成長+勢い、返しには成長のみ (勢いは自ターン終了でリセットされるため)
  const atkBonus = ctx ? ctx.growth + ctx.momentum : 0
  // 置物文脈の onPlay は「登場時」— 無印だと持続効果に見える (2026-08-30 Opus緑ランの誤読対処)
  const trigger =
    (e.trigger === 'onPlay' && holderType === 'permanent' ? '登場時: ' : TRIGGER_LABEL[e.trigger]) +
    conditionLabel(e)
  const pierce = e.pierce ? '(貫通)' : ''
  const aoe = e.target === 'all' ? '敵全体に' : ''
  // トータル先頭表記: 補正込みの実ダメージを先に出し、内訳を括弧で添える
  const atkBreak = atkBonus > 0 ? `（${'基礎'}${e.amount}+補正${atkBonus}）` : ''
  switch (e.effect) {
    case 'dealDamage': {
      // X札の見積り (2026-09-03 Opus D: 成長23・X=4の森羅の大嵐が136出た。手札段階で実ダメが分からない)
      const xNow =
        e.xHits === true && ctx?.energy !== undefined
          ? `［全部払うとX=${ctx.energy}: 計${((e.amount ?? 0) + atkBonus) * ctx.energy}${aoe ? '/体' : ''}］`
          : ''
      return `${trigger}⚔️ ${aoe}${(e.amount ?? 0) + atkBonus}ダメージ${pierce}${xHitsSuffix(e)}${atkBreak}${xNow}`
    }
    case 'dealDamagePerEnergyMax':
      return ctx
        ? `${trigger}ターン開始時のエナジー上限×${e.amount}ダメージ${pierce} [現在${(e.amount ?? 0) * ctx.energyMax + atkBonus}]`
        : `${trigger}ターン開始時のエナジー上限×${e.amount}ダメージ${pierce}`
    case 'dischargeGrowthBlock':
      return ctx
        ? `${trigger}🛡 成長×${e.amount}のブロックを得て、成長を全て失う [現在${(e.amount ?? 0) * ctx.growth}]`
        : `${trigger}🛡 成長×${e.amount}のブロックを得て、成長を全て失う`
    case 'dealDamagePerMomentum':
      return ctx
        ? `${trigger}勢い×${e.amount}ダメージ${pierce} [現在${(e.amount ?? 0) * ctx.momentum + atkBonus}]`
        : `${trigger}勢い×${e.amount}ダメージ${pierce}`
    case 'counter': {
      const cBonus = ctx ? ctx.growth : 0
      const cBreak = cBonus > 0 ? `（基礎${e.amount}+成長${cBonus}）` : ''
      return `${trigger}↩️ 返し${(e.amount ?? 0) + cBonus}ダメージ${pierce}${cBreak}`
    }
    case 'gainBlock':
      return `${trigger}🛡 ブロック+${e.amount}${xHitsSuffix(e)}`
    case 'gainIceBlock':
      return `${trigger}🧊 氷壁+${e.amount}`
    case 'dealDamagePerCardPlayedTotal':
      return `${trigger}${aoe}この戦闘でプレイした累計枚数×${e.amount}ダメージ${pierce}`
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
    case 'dealDamagePerHandCard':
      return ctx
        ? `${trigger}⚔️ 手札の枚数×${e.amount}ダメージ${pierce} [現在${(e.amount ?? 0) * ctx.handCards + atkBonus}]`
        : `${trigger}⚔️ 手札の枚数×${e.amount}ダメージ${pierce}（この札自身は数えない）`
    case 'gainIceBlockPerHandCard':
      return ctx
        ? `${trigger}🧊 手札の枚数×${e.amount}の氷壁 [現在${(e.amount ?? 0) * ctx.handCards}]`
        : `${trigger}🧊 手札の枚数×${e.amount}の氷壁`
    case 'addSpellEcho':
      return `${trigger}🔁 反復+${e.amount}（次に唱える呪文の効果を2回解決。自ターン終了時に消える。とげ反射も2回受ける）`
    case 'addCasts':
      return `${trigger}🌀 詠唱数+${e.amount}（激昂タイマーには数えない）`
    case 'blessRetainers':
      return `${trigger}✨ 【常在】従者の効果+${e.amount}`
    case 'addAether':
      return `${trigger}霊気+${e.amount}`
    case 'discountNext':
      return `${trigger}次にプレイするカードのコスト-${e.amount}`
    case 'applyBurn':
      return `${trigger}${aoe}延焼+${e.amount}`
    case 'applyBurnPerDamageTaken':
      return `${trigger}${aoe}直前の敵ターンに受けたダメージ×${e.amount}の延焼`
    case 'dealDamagePerRandomPlayed':
      return ctx
        ? `${trigger}${aoe}この戦闘で撃った運任せの札×${e.amount}ダメージ${pierce} [現在${ctx.randomPlayed}枚=${ctx.randomPlayed * (e.amount ?? 0) + atkBonus}]`
        : `${trigger}${aoe}この戦闘で撃った運任せの札×${e.amount}ダメージ${pierce}`
    case 'shatterBlock':
      return `${trigger}${aoe || '敵の'}ブロックを全て粉砕する`
    case 'confuse':
      return `${trigger}敵1体に混乱+${e.amount}（攻撃が仲間に向かう）`
    case 'exposeEnemy':
      return `${trigger}${aoe}急所+${e.amount}（次に受けるダメージ${e.amount}回が+50%）`
    case 'gainHp':
      return `${trigger}💚 HP${e.amount}回復`
    case 'dealDamageDrain':
      return `${trigger}🩸 ${aoe}${(e.amount ?? 0) + atkBonus}ダメージを与え、HP${Math.floor((e.amount ?? 0) / 2)}回復${atkBreak}`
    case 'exhaustFromDeck':
      return `${trigger}🕳 山札の上${e.amount}枚を消滅させる`
    case 'exhaustFromDeckChoose':
      return `${trigger}⚰️ 山札か捨て札から好きな${e.amount ?? 1}枚を選んで消滅させる（亡骸は発火する）`
    case 'retrieveFromDiscard':
      return `${trigger}🌱 捨て札から好きな${e.amount ?? 1}枚を手札に戻す`
    case 'searchDeck':
      return `${trigger}🔍 山札から好きな${e.amount ?? 1}枚を手札に加える（引き順は伏せたまま）`
    case 'addCopyToDiscard':
      return `${trigger}🌿 このカードのコピー${e.amount ?? 1}枚を捨て札に加える（この戦闘限り）`
    case 'growSelf':
      return `${trigger}📈 プレイするたび、この札の与ダメージがこの戦闘中+${e.amount}`
    case 'upgradeInHand':
      return `${trigger}🔨 手札の${e.amount ?? 1}枚をこの戦闘中鍛える（自身・レア・工房産は選べない）`
    case 'gainSetSlot':
      return `${trigger}🃏 この戦闘中、伏せ枠+${e.amount ?? 1}`
    case 'dealDamagePerExhaust':
      return ctx
        ? `${trigger}⚔️ ${aoe}消滅した枚数×${e.amount}ダメージ${pierce} [現在${(e.amount ?? 0) * ctx.exhausted + atkBonus}]`
        : `${trigger}⚔️ ${aoe}消滅した枚数×${e.amount}ダメージ${pierce}`
    case 'dealDamageDrainPerExhaust':
      return ctx
        ? `${trigger}🩸 消滅した枚数×${e.amount}ダメージ+半分回復 [現在${(e.amount ?? 0) * ctx.exhausted + atkBonus}]`
        : `${trigger}🩸 消滅した枚数×${e.amount}ダメージ+半分回復`
    case 'recycleExhaust':
      return `${trigger}♻️ 消滅置き場のカードを全て山札に混ぜて戻し、戻した枚数×${e.amount}ダメージ（刻・消滅数参照は0に戻る。亡骸はもう一度落とせる）`
    case 'dealDamagePerHeal':
      return `${trigger}⚔️ この戦闘で回復した回数×${e.amount}ダメージ${pierce}（過剰回復も数える）`
    case 'dealDamagePerSelfHpLost':
      return ctx
        ? `${trigger}⚔️ この戦闘でカード効果により失ったHP×${e.amount}ダメージ${pierce} [現在${(e.amount ?? 0) * ctx.selfHpLost + atkBonus}]`
        : `${trigger}⚔️ この戦闘でカード効果により失ったHP×${e.amount}ダメージ${pierce}`
    case 'retrieveFromExhaust':
      return `${trigger}⚰️ 消滅置き場からカード${e.amount ?? 1}枚を選んで手札に戻す（戻した札はこの戦闘中コスト0）`
    case 'playFromExhaust':
      return `${trigger}⚰️ 消滅置き場のカード1枚（リアクション以外）をコストを支払わず直接プレイ（そのカードは消滅置き場に残る）`
    case 'summonPermanent':
      return `${trigger}🏳️ ${cardName(e.summonId ?? '')}トークンを${e.amount ?? 1}体場に出す`
    case 'addCardToHand':
      return `${trigger}🗡️ ${cardName(e.summonId ?? '')}を${e.amount ?? 1}枚手札に加える（この戦闘限り）`
    case 'empowerShivs':
      return `${trigger}🗡️ 【常在】骨のナイフの与ダメージ+${e.amount}`
    case 'dischargeBurn':
      return `${trigger}💥 爆熱: 対象の延焼×${e.amount}ダメージを与え、延焼を全て失わせる`
    case 'shatterBlockConvert':
      return `${trigger}🔨 敵のブロックを全て破壊し、破壊した値と同じダメージ`
    case 'dealDamageExecute':
      return `${trigger}⚔️ ${e.amount}ダメージ（対象HPが25%以下なら${e.amountMax}）`
    case 'dealDamagePerIceBlock':
      return ctx
        ? `${trigger}⚔️ 現在の氷壁×${e.amount}ダメージ（氷壁は消費しない・急所は乗らない） [現在${(e.amount ?? 0) * ctx.iceBlock + atkBonus}]`
        : `${trigger}⚔️ 現在の氷壁×${e.amount}ダメージ（氷壁は消費しない・急所は乗らない）`
    case 'negateConvertIce':
      return `${trigger}🚫 敵の行動1回を打ち消し、その実値ぶん氷壁を得る`
    case 'dischargeAetherDraw':
      return ctx
        ? `${trigger}🔮 霊気を全て消費し、×${e.amount}枚ドロー [現在${(e.amount ?? 1) * ctx.aether}]`
        : `${trigger}🔮 霊気を全て消費し、×${e.amount}枚ドロー`
    case 'dealDamagePerDamageTaken':
      return ctx
        ? `${trigger}⚔️ 直前の敵フェーズで受けたダメージ×${e.amount}ダメージ [現在${(e.amount ?? 0) * ctx.damageTaken + atkBonus}]`
        : `${trigger}⚔️ 直前の敵フェーズで受けたダメージ×${e.amount}ダメージ`
    case 'dealDamagePerNegStrength':
      return `${trigger}⚔️ 対象の下げられた筋力×${e.amount}の追加ダメージ（威圧の換金）`
    case 'gainBlockPerEnergyMax':
      return ctx
        ? `${trigger}🛡 ターン開始時のエナジー上限×${e.amount}ブロック [現在${(e.amount ?? 0) * ctx.energyMax}]`
        : `${trigger}🛡 ターン開始時のエナジー上限×${e.amount}ブロック`
    case 'gainBlockPerExhaust':
      return ctx
        ? `${trigger}🛡 消滅した枚数×${e.amount}ブロック [現在${(e.amount ?? 0) * ctx.exhausted}]`
        : `${trigger}🛡 消滅した枚数×${e.amount}ブロック`
    case 'gainBlockPerPermanent':
      return ctx
        ? `${trigger}🛡 置物の数×${e.amount}ブロック [現在${(e.amount ?? 0) * ctx.permanents}]`
        : `${trigger}🛡 置物の数×${e.amount}ブロック`
    case 'strengthenEnemy':
      return `${trigger}💪 敵の筋力+${e.amount}`
    case 'weakenEnemy':
      return `${trigger}${aoe}威圧${e.amount}（敵の筋力-${e.amount}）`
    case 'dealDamagePerBlock':
      return ctx2Block(e, ctx, trigger, pierce)
    case 'dealDamagePerPermanent':
      return ctx
        ? `${trigger}⚔️ 置物の数×${e.amount}ダメージ${pierce}`
        : `${trigger}⚔️ 置物の数×${e.amount}ダメージ${pierce}`
    case 'dischargeGrowth':
      return ctx && ctx.growth > 0
        ? `${trigger}⚔️ 成長×${e.amount}ダメージを与え、成長を全て失う [現在${ctx.growth * (e.amount ?? 0) + ctx.growth}]`
        : `${trigger}⚔️ 成長×${e.amount}ダメージを与え、成長を全て失う`
    case 'dischargeMomentumBurn':
      return `${trigger}🔥 勢い×${e.amount}の延焼を与え、勢いを全て失う`
    case 'dischargeMomentumBlock':
      return `${trigger}🛡️ 勢い×${e.amount}のブロックを得て、勢いを全て失う`
    case 'dealDamageCleave':
      return `${trigger}⚔️ ${(e.amount ?? 0) + atkBonus}ダメージ${atkBreak}。倒したら別の敵にも同値`
    case 'dealDamageRandom':
      return `${trigger}⚔️ ${(e.amount ?? 0) + atkBonus}〜${(e.amountMax ?? 0) + atkBonus}ダメージ(ランダム)${pierce}${atkBonus > 0 ? `（補正+${atkBonus}込み）` : ''}`
    case 'impulseDraw':
      return `${trigger}衝動${e.amount}枚（山札の上から。このターン限り）`
    case 'loseHp':
      return `${trigger}HPを${e.amount}失う`
    case 'dischargeAether':
      return ctx
        ? `${trigger}${aoe}霊気×${e.amount}ダメージを与え、霊気を全て放出する [現在${(e.amount ?? 0) * ctx.aether + atkBonus}]`
        : `${trigger}${aoe}霊気×${e.amount}ダメージを与え、霊気を全て放出する`
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
    case 'doubleMomentum':
      return ctx && ctx.momentum > 0
        ? `${trigger}勢いを2倍にする [${ctx.momentum}→${ctx.momentum * 2}]`
        : `${trigger}勢いを2倍にする`
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
function effectItems(effects: readonly DeclarativeEffect[], ctx?: EffectCtx, holderType?: string): string[] {
  const lines: string[] = []
  const counts: number[] = []
  for (const e of effects) {
    const text = renderEffectItem(e, ctx, holderType)
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
  if (def.id === 'status_scald') return ['使えない。自ターン終了時に手札にあるとHP-2（この戦闘限り。捨て/消滅コストの支払いには使える）']
  if (def.id === 'status_brand') return ['使えない。自ターン終了時に手札にあるとHP-1（デッキに残る呪い。焚き火・ショップで除去できる）']
  if (def.id === 'status_guilt') return ['使えない。自ターン終了時に手札にあるとHP-1（仮初の呪い。5戦すると自然に消える）']
  const lines: string[] = []
  if ((def.discardCost ?? 0) > 0) lines.push(`追加コスト: 手札${def.discardCost}枚を捨てる`)
  if ((def.exhaustCost ?? 0) > 0) lines.push(`追加コスト: 手札${def.exhaustCost}枚を消滅させる`)
  if (def.modes && def.modes.length > 0) {
    def.modes.forEach((m, i) => lines.push(`選択${i + 1}: ${effectItems(m.effects, ctx).join('、')}`))
  } else {
    lines.push(...effectItems(def.effects, ctx, def.type))
  }
  if (def.exhaust) lines.push('消滅')
  if (def.retain) lines.push('保持（ターン終了時に手札に残る）')
  if (def.necroCost !== undefined) lines.push(`💀 亡骸プレイ${def.necroCost}E（消滅置き場から一度だけプレイできる。その後ゲームから消える）`)
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

/** 条件付き意図の表示: 両分岐を予告し、いまどちらが有効かを示す */
function conditionalIntentText(s: GameState, i: number): string {
  const intent = s.enemies[i]?.intent
  if (!intent) return '---'
  if (!intent.conditionalOn || !intent.alt) return intentText(intent)
  // 伏せられないデッキには「伏せ札あり」分岐を予告しない (到達不能な選択肢の常時表示は
  // 「お前にはこの選択肢は無い」の掲示になる — 2026-08-30 Opusラン報告)
  if (intent.conditionalOn === 'set' && !playerCanSet(s)) {
    return intentText({ ...intent, conditionalOn: undefined, alt: undefined })
  }
  // 表示も実値も同じ時だけ完全に畳む (2026-08-31: 実値だけ違う分岐を畳むと損分岐が不可視になる)
  const baseOnly = intentText({ ...intent, conditionalOn: undefined, alt: undefined })
  if (intentText({ ...intent.alt }) === baseOnly && intent.alt.actual === intent.actual) return baseOnly
  // 表示が同値で実値だけ違う: 2分岐で予告するとノイズ (探り屋のローテ替え等) なので1行+注記に
  // (2026-08-31 再検証ラン指摘③)。どちら向きかは判断材料なので添える (同日HP経済ラン指摘④)
  if (intentText({ ...intent.alt }) === baseOnly) {
    return `${baseOnly}（伏せると実値が${intent.alt.actual > intent.actual ? '上がる' : '下がる'}）`
  }
  const note = intent.conditionalOn === 'set' ? setBranchNote(getEnemyDef(s.enemies[i].enemyId)) : null
  const cond = intent.conditionalOn === 'set' ? `伏せ札あり${note ? `（${note}）` : ''}` : '従者あり'
  const active = effectiveIntent(s, i)!
  const isAlt = active.kind === intent.alt.kind && active.shownMin === intent.alt.shownMin
  return `【${cond}】${intentText({ ...intent.alt })}${isAlt ? '◀今これ' : ''} ／【なし】${intentText({ ...intent, conditionalOn: undefined, alt: undefined })}${isAlt ? '' : '◀今これ'}`
}

/** 誘発確認ウィンドウ用: 敵の行動は確定済みなので実値を公開する (確定済みルール「誘発確認時の情報」) */
function confirmedIntentText(intent: EnemyIntent | null): string {
  if (!intent) return '---'
  switch (intent.kind) {
    case 'attack': {
      const hits = (intent.hits ?? 1) > 1 ? `×${intent.hits}` : ''
      return `⚔️ 攻撃 ${intent.actual}${hits}（宣言 ${intent.shownMin}〜${intent.shownMax}）${inflictSuffix(intent)}`
    }
    case 'defend':
      return `🛡️ 防御 ${intent.actual}（宣言 ${intent.shownMin}〜${intent.shownMax}）${intent.alsoBuff !== undefined ? `＋💪筋力+${intent.alsoBuff}` : ''}`
    case 'destroy-set':
      return '💥 伏せ破壊'
    case 'destroy-token':
      return '🪓 従者狩り'
    case 'buff':
      return `💪 筋力 +${intent.actual}（宣言 +${intent.shownMin}〜+${intent.shownMax}）`
    case 'rally':
      return `📣 応援 +${intent.actual}（味方全体。宣言 +${intent.shownMin}〜+${intent.shownMax}）`
    case 'hex':
      return `🧿 呪い${inflictSuffix(intent)}`
    case 'heal':
      return `💚 回復 ${intent.actual}（宣言 ${intent.shownMin}〜${intent.shownMax}）`
    case 'steal-gold':
      return `💰 盗み ${intent.actual}G（宣言 ${intent.shownMin}〜${intent.shownMax}G）`
    case 'flee':
      return '🏃 逃走'
    case 'rest':
      return '😮‍💨 隙だらけ'
    case 'hatch':
      return '🐣 孵化する'
    case 'mill':
      return `📖 山札喰い ${intent.actual}枚（宣言 ${intent.shownMin}〜${intent.shownMax}枚。消滅置き場へ）`
  }
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
  // Xコスト札は割引の対象外なので「割引済み」表示にもしない
  const discounted =
    card.def.xCost !== true && displayCost !== undefined && displayCost !== card.def.cost
  return (
    <div className={`card card-role-${uiCardRole(card.def)}${dim ? ' card-dim' : ''}`}>
      <div className={`card-cost${discounted ? ' card-cost-discounted' : ''}`}>
        {cardCostLabel(card.def, displayCost)}
      </div>
      <div className="card-name">{card.def.name}</div>
      <div className={`card-category type-${card.def.type}`}>{TYPE_LABEL[card.def.type]}</div>
      <div className="card-text">
        <EffectLines def={card.def} ctx={ctx} />
        {(card.growBonus ?? 0) > 0 && (
          <div style={{ fontSize: 11, marginTop: 2 }}>
            <span style={{ color: 'var(--good, #8fd)' }}>📈 育ち: 与ダメ+{card.growBonus}（この戦闘中）</span>
          </div>
        )}
        {card.expiresAfterBattles !== undefined && (
          <>
            <br />
            <span style={{ color: 'var(--muted)' }}>⏳ あと{card.expiresAfterBattles}戦で自然に消える</span>
          </>
        )}
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
  /** デバッグ: 自分で組んだデッキ (cardId の配列)。指定時は deckId より優先する */
  customDeck?: readonly string[]
}

// ---- デバッグ用の開発者ツール (2026-08-30 ユーザー要望)。プレイ用の導線とは分けて畳んでおく ----

/** デッキビルダー: 単発戦で使うデッキを1枚ずつ組む */
function DeckBuilder({
  colors,
  deck,
  setDeck,
}: {
  colors: readonly CardColor[]
  deck: readonly string[]
  setDeck: (next: readonly string[]) => void
}) {
  const [filter, setFilter] = useState('')
  const pool = allCards.filter(
    (c) =>
      colors.includes(c.color) &&
      c.id !== 'status_wound' &&
      c.id !== 'status_junk' &&
      (filter === '' || c.name.includes(filter)),
  )
  const count = (id: string) => deck.filter((x) => x === id).length
  const add = (id: string) => setDeck([...deck, id])
  const remove = (id: string) => {
    const i = deck.lastIndexOf(id)
    if (i >= 0) setDeck([...deck.slice(0, i), ...deck.slice(i + 1)])
  }
  return (
    <div className="panel" style={{ marginTop: 12 }}>
      <div className="setup-section-title">
        🔧 デッキを自分で組む（{deck.length}枚）
      </div>
      <div className="choice-desc">
        単発戦で使うデッキを1枚ずつ選ぶ。0枚のままなら上で選んだプリセットデッキを使う。
      </div>
      <div style={{ margin: '8px 0' }}>
        <input
          value={filter}
          onChange={(ev) => setFilter(ev.target.value)}
          placeholder="カード名で絞り込み"
          size={18}
        />{' '}
        <button className="btn" onClick={() => setDeck([])} disabled={deck.length === 0}>
          全部外す
        </button>
      </div>
      {deck.length > 0 && (
        <div className="choice-desc" style={{ marginBottom: 8 }}>
          <b>いまのデッキ:</b>{' '}
          {[...new Set(deck)]
            .map((id) => `${getCardDef(id).name}×${count(id)}`)
            .join(' / ')}
        </div>
      )}
      <div style={{ maxHeight: 260, overflowY: 'auto' }}>
        {pool.map((c) => (
          <div key={c.id} style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '1px 0' }}>
            <button className="btn" onClick={() => remove(c.id)} disabled={count(c.id) === 0}>
              −
            </button>
            <span style={{ minWidth: 22, textAlign: 'right' }}>{count(c.id) || ''}</span>
            <button className="btn" onClick={() => add(c.id)}>
              ＋
            </button>
            <span className="pile-info" style={{ minWidth: 26 }}>{cardCostLabel(c)}E</span>
            <span style={{ minWidth: 120 }}>{c.name}</span>
            <span className="pile-info">{effectText(c)}</span>
          </div>
        ))}
      </div>
    </div>
  )
}

/** 合成ラボ: 任意の2枚の合成結果をその場で確かめる (工房の計算を検証するため) */
function FusionLab() {
  const [aId, setAId] = useState<string | null>(null)
  const [bId, setBId] = useState<string | null>(null)
  const [filter, setFilter] = useState('')
  const pool = allCards.filter(
    (c) => c.id !== 'status_wound' && c.id !== 'status_junk' && (filter === '' || c.name.includes(filter)),
  )
  const a = aId !== null ? { uid: 'lab_a', def: getCardDef(aId) } : null
  const b = bId !== null ? { uid: 'lab_b', def: getCardDef(bId) } : null
  let reason: string | null = null
  let result: CardDef | null = null
  if (a && b) {
    reason = fuseBlockReason(a, b)
    if (reason === null) {
      try {
        result = fuseCards(a, b)
      } catch (e) {
        reason = e instanceof Error ? e.message : String(e)
      }
    }
  }
  const pick = (label: string, cur: string | null, set: (v: string | null) => void) => (
    <div style={{ flex: 1, minWidth: 0 }}>
      <div className="setup-section-title">{label}</div>
      <select
        value={cur ?? ''}
        onChange={(ev) => set(ev.target.value === '' ? null : ev.target.value)}
        style={{ width: '100%' }}
      >
        <option value="">（選んでください）</option>
        {pool.map((c) => (
          <option key={c.id} value={c.id}>
            {c.color} {c.name} ({cardCostLabel(c)}E)
          </option>
        ))}
      </select>
    </div>
  )
  return (
    <div className="panel" style={{ marginTop: 12 }}>
      <div className="setup-section-title">🔬 合成ラボ</div>
      <div className="choice-desc">
        任意の2枚を選んで工房の合成結果をその場で確かめる（同じ色同士のみ。ランを回さずに検算できる）。
      </div>
      <div style={{ margin: '8px 0' }}>
        <input
          value={filter}
          onChange={(ev) => setFilter(ev.target.value)}
          placeholder="カード名で絞り込み"
          size={18}
        />
      </div>
      <div style={{ display: 'flex', gap: 8 }}>
        {pick('素材A', aId, setAId)}
        {pick('素材B', bId, setBId)}
      </div>
      {reason !== null && (
        <div className="choice-desc" style={{ marginTop: 8 }}>
          ❌ 合成できない: {reason}
        </div>
      )}
      {result && (
        <div style={{ marginTop: 8 }}>
          <div className="choice-desc">
            {result.id.startsWith('fusion_') ? '⭐ レシピ発見！ ' : ''}
            素材コスト {a!.def.cost}E + {b!.def.cost}E → <b>{result.cost}E</b>
            {result.exhaust ? '（消滅つき）' : ''}
          </div>
          <div className="hand-cards" style={{ marginTop: 8 }}>
            <CardFrame card={{ uid: 'lab_result', def: result }} dim={false} actions={null} />
          </div>
        </div>
      )}
    </div>
  )
}

/** デッキ構成の1行サマリ (例: 年輪×4 開花の儀×2 …) */
function deckComposition(deckId: string): string {
  return getDeckDef(deckId)
    .cards.map((e) => `${getCardDef(e.cardId).name}×${e.count}`)
    .join(' ')
}

/** チェックポイント開始 (2026-09-01 デバッグ): 幕2/3から代表デッキ+レリックで開始 = 谷・終盤の検証を幕1抜きで */
function CheckpointPanel({
  leaderId,
  difficulty,
  onStart,
}: {
  leaderId: string
  difficulty: number
  onStart: (opts: { seed: number; leaderId: string; act: number; deckId: string; relicIds: readonly string[]; hpRatio: number; gold: number; difficulty: number }) => void
}) {
  const leader = getLeaderDef(leaderId)
  const decks = allDecks.filter((d) => deckAllowedForLeader(leader, d))
  const [act, setAct] = useState(2)
  const [deckId, setDeckId] = useState(decks[0]?.id ?? '')
  const [relicIds, setRelicIds] = useState<readonly string[]>([])
  const [hpPct, setHpPct] = useState(100)
  const [gold, setGold] = useState(150)
  const effectiveDeck = decks.some((d) => d.id === deckId) ? deckId : (decks[0]?.id ?? '')
  const S = { fontSize: 12 } as const
  return (
    <div className="panel" style={{ marginTop: 12 }}>
      <div className="setup-section-title">🚩 チェックポイント開始（デバッグ: 幕2/3から検証）</div>
      <div className="choice-desc">
        選択中のリーダー（{leader.name}）と難易度🎚{difficulty}で、指定の幕から開始。レリックのB型効果（最大HP等）も適用される。
        幕2の想定 ≈ レリック2〜3個・幕3 ≈ 5〜6個
      </div>
      <div style={{ display: 'flex', gap: 10, alignItems: 'center', flexWrap: 'wrap', marginTop: 6 }}>
        <label style={S}>
          幕{' '}
          <select value={String(act)} onChange={(e) => setAct(Number(e.target.value))}>
            <option value="2">2</option>
            <option value="3">3</option>
          </select>
        </label>
        <label style={S}>
          デッキ{' '}
          <select value={effectiveDeck} onChange={(e) => setDeckId(e.target.value)}>
            {decks.map((d) => (
              <option key={d.id} value={d.id}>{d.name}（{deckSize(d)}枚）</option>
            ))}
          </select>
        </label>
        <label style={S}>
          HP% <input type="number" min={5} max={100} style={{ width: 52 }} value={hpPct} onChange={(e) => setHpPct(Number(e.target.value) || 100)} />
        </label>
        <label style={S}>
          💰 <input type="number" min={0} style={{ width: 60 }} value={gold} onChange={(e) => setGold(Number(e.target.value) || 0)} />
        </label>
      </div>
      <div style={{ marginTop: 6, display: 'flex', gap: 8, flexWrap: 'wrap' }}>
        {allRelics.map((r) => (
          <label key={r.id} style={{ fontSize: 11 }} title={r.description}>
            <input
              type="checkbox"
              checked={relicIds.includes(r.id)}
              onChange={(e) =>
                setRelicIds((prev) => (e.target.checked ? [...prev, r.id] : prev.filter((x) => x !== r.id)))
              }
            />{' '}
            {r.sprite}{r.name}
          </label>
        ))}
      </div>
      <button
        className="btn btn-primary"
        style={{ marginTop: 8 }}
        disabled={effectiveDeck === ''}
        onClick={() =>
          onStart({
            seed: Date.now() % 2 ** 32,
            leaderId,
            act,
            deckId: effectiveDeck,
            relicIds,
            hpRatio: Math.min(1, Math.max(0.05, hpPct / 100)),
            gold,
            difficulty,
          })
        }
      >
        🚩 幕{act}から開始（レリック{relicIds.length}個）
      </button>
    </div>
  )
}

function SetupScreen({
  onStart,
  onStartRun,
  resume,
  onLoadSave,
  onLoadReplay,
  onStartCheckpoint,
}: {
  onStart: (cfg: Config) => void
  onStartRun: (seed: number, leaderId: string, runDeckId?: string, difficulty?: number, revealIntents?: boolean, setAnyCards?: boolean) => void
  /** 「続きから」(localStorageバックアップにランがある時だけ非null) */
  resume?: { label: string; onResume: () => void } | null
  /** セーブファイル (.json) の読み込み */
  onLoadSave?: (f: File) => void
  /** リプレイ (journal付きセーブ) の読み込み → ビューアで再生 */
  onLoadReplay?: (f: File) => void
  /** チェックポイント開始 (デバッグ) */
  onStartCheckpoint?: (opts: { seed: number; leaderId: string; act: number; deckId: string; relicIds: readonly string[]; hpRatio: number; gold: number; difficulty: number }) => void
}) {
  const [enemyId, setEnemyId] = useState(allEnemies[0].id)
  const [leaderId, setLeaderId] = useState(allLeaders[0].id)
  // 難易度 (確定済みルール表「難易度」): 1〜10・既定3=現状維持
  const [difficulty, setDifficulty] = useState(DEFAULT_DIFFICULTY)
  const [revealIntents, setRevealIntents] = useState(false) // 判定実験: 意図を常時実値表示 (2026-09-02)
  const [setAnyCards, setSetAnyCards] = useState(false) // 実験: 全カード伏せ可 (2026-09-02)
  const leader = getLeaderDef(leaderId)
  const allowedDecks = allDecks.filter((d) => deckAllowedForLeader(leader, d))
  const [deckId, setDeckId] = useState(allowedDecks[0].id)
  const [seedInput, setSeedInput] = useState('')
  // デバッグ枠 (2026-08-30 ユーザー要望): 自分で組んだデッキと合成ラボ。既定は畳んでおく
  const [customDeck, setCustomDeck] = useState<readonly string[]>([])
  const [showDebug, setShowDebug] = useState(false)
  const [showCatalog, setShowCatalog] = useState(false)
  const parseSeed = () =>
    /^\d+$/.test(seedInput) ? Number(seedInput) >>> 0 : Date.now() % 2 ** 32
  // リーダー変更で使用可能デッキ外を選んでいたら先頭に戻す
  const effectiveDeckId = allowedDecks.some((d) => d.id === deckId) ? deckId : allowedDecks[0].id
  return (
    <div className="app setup">
      <h1>
        deck-rogue-proto{' '}
        <button className="btn" style={{ fontSize: 13, verticalAlign: 'middle' }} onClick={() => setShowCatalog(true)}>
          📚 図鑑
        </button>
      </h1>
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

      {(resume != null || onLoadSave !== undefined) && (
        <div className="panel" style={{ marginTop: 16 }}>
          <div className="choice-title">💾 セーブ</div>
          {resume != null && (
            <button className="btn btn-primary" style={{ marginTop: 6 }} onClick={resume.onResume}>
              ▶ 続きから（{resume.label}）
            </button>
          )}{' '}
          {onLoadSave !== undefined && (
            <label className="btn" style={{ marginTop: 6, display: 'inline-block', cursor: 'pointer' }}>
              📂 セーブファイルを読み込む
              <input
                type="file"
                accept=".json,application/json"
                style={{ display: 'none' }}
                onChange={(e) => {
                  const f = e.target.files?.[0]
                  if (f !== undefined) onLoadSave(f)
                  e.target.value = ''
                }}
              />
            </label>
          )}{' '}
          {onLoadReplay !== undefined && (
            <label className="btn" style={{ marginTop: 6, display: 'inline-block', cursor: 'pointer' }} title="💾で書き出したセーブ(記録付き)を1手ずつ再生。任意の地点から操作の引き継ぎもできる">
              🎬 リプレイを読み込む
              <input
                type="file"
                accept=".json,application/json"
                style={{ display: 'none' }}
                onChange={(e) => {
                  const f = e.target.files?.[0]
                  if (f !== undefined) onLoadReplay(f)
                  e.target.value = ''
                }}
              />
            </label>
          )}
          <div className="choice-desc" style={{ marginTop: 4 }}>
            進行は自動でこの端末に保存されます（単一スロット・最新のみ）。ファイルは💾ボタンで書き出せて、CLI（sim/play.ts）でもそのまま開けます
          </div>
        </div>
      )}
      <div className="panel" style={{ marginTop: 16 }}>
        <div className="choice-title">🏕 ドラフト連戦（マップラン・ボスまで16行）</div>
        <div className="choice-desc">
          {leader.name}の基本10枚から出発し、勝利ごとに{leader.colors.map((c) => COLOR_LABEL[c]).join('')}
          の{leader.rewardChoices}枚から1枚ピックして構築。敵は段階制でだんだん強くなり、HPは持ち越し。
        </div>
        <div style={{ marginTop: 10 }}>
          <span className="choice-title">🎚 難易度 </span>
          {DIFFICULTY_TABLE.map((_, i) => (
            <button
              key={i}
              className={`btn${difficulty === i + 1 ? ' btn-primary' : ''}`}
              style={{ minWidth: 34, marginRight: 4, marginBottom: 4 }}
              onClick={() => setDifficulty(i + 1)}
            >
              {i + 1}
            </button>
          ))}
          <div className="choice-desc">
            {difficulty === DEFAULT_DIFFICULTY
              ? `${DEFAULT_DIFFICULTY}: 標準（現状のバランス）`
              : `${difficulty}: 敵HP×${DIFFICULTY_TABLE[difficulty - 1].hp} / 敵打点×${DIFFICULTY_TABLE[difficulty - 1].atk}${difficulty < DEFAULT_DIFFICULTY ? '（易しめ）' : ''}`}
          </div>
        </div>
        {(leader.runDeckChoices ?? [leader.runDeckId]).length > 1 ? (
          <div className="choice-row" style={{ marginTop: 8 }}>
            {(leader.runDeckChoices ?? []).map((deckId) => {
              const deck = allDecks.find((d) => d.id === deckId)
              return (
                <button
                  key={deckId}
                  className="choice"
                  onClick={() => onStartRun(parseSeed(), leaderId, deckId, difficulty, revealIntents, setAnyCards)}
                >
                  <div className="choice-title">{leader.sprite} {deck?.name ?? deckId}で開始</div>
                  <div className="choice-desc">{deck?.description}</div>
                </button>
              )
            })}
          </div>
        ) : (
          <button
            className="btn btn-primary"
            style={{ marginTop: 8 }}
            onClick={() => onStartRun(parseSeed(), leaderId, undefined, difficulty, revealIntents, setAnyCards)}
          >
            {leader.sprite} {leader.name}でランを開始
          </button>
        )}
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
          onClick={() =>
            onStart({
              mode: ADOPTED_MODE,
              enemyId,
              deckId: effectiveDeckId,
              leaderId,
              seed: parseSeed(),
              ...(customDeck.length > 0 ? { customDeck } : {}),
            })
          }
        >
          ⚔️ 戦闘開始
          {customDeck.length > 0 ? `（自分で組んだ${customDeck.length}枚）` : ''}
        </button>
      </div>

      <div className="setup-section-title" style={{ marginTop: 24 }}>
        ── 資料 ──{' '}
        <button className="btn" onClick={() => setShowCatalog(true)}>
          📚 カード図鑑
        </button>
      </div>
      {showCatalog && <CardCatalogOverlay onClose={() => setShowCatalog(false)} />}

      <div className="setup-section-title" style={{ marginTop: 24 }}>
        ── 開発者ツール ──{' '}
        <button className="btn" onClick={() => setShowDebug((v) => !v)}>
          {showDebug ? '閉じる' : '開く'}
        </button>
      </div>
      {showDebug && (
        <>
          <DeckBuilder colors={leader.colors} deck={customDeck} setDeck={setCustomDeck} />
          <FusionLab />
          {onStartCheckpoint !== undefined && (
            <>
              <label className="hint" style={{ display: 'block', marginTop: 8 }} title="退屈診断④の判定実験: 幅あり意図（例: 攻撃6〜12）を常時実値にして遊び、幅表示の有無で体感がどう変わるかを比べる。仕様は変えず計測だけ（レポートに記録される）">
                <input type="checkbox" checked={revealIntents} onChange={(e) => setRevealIntents(e.target.checked)} /> 🔍 意図を常時実値表示（幅あり意図の判定実験）
              </label>
              <label className="hint" style={{ display: 'block', marginTop: 4 }} title="実験 (2026-09-02): 攻撃・防御の通常カードも1Eで伏せられる。誘発したら印字コストを敵ターンに持ち越したエナジーから払って発動。専用リアクションは従来どおり伏せ時に支払い・発動無料">
                <input type="checkbox" checked={setAnyCards} onChange={(e) => setSetAnyCards(e.target.checked)} /> 🃏 全カード伏せ可（通常カードは1Eで伏せ、発動時に印字コスト）
              </label>
              <CheckpointPanel leaderId={leaderId} difficulty={difficulty} onStart={onStartCheckpoint} />
            </>
          )}
        </>
      )}
    </div>
  )
}

// ---- 戦闘画面 ----

/** ホバー内訳の対象になる「基礎値=amountのダメージ効果」 */
const TIP_DAMAGE_EFFECTS = new Set(['dealDamage', 'dealDamageDrain', 'dealDamageCleave', 'dealDamageExecute', 'dealDamageRandom'])

/**
 * カードホバー時のダメージ内訳 (2026-09-01 ユーザー要望)。engineの damageBreakdown (実処理と同手順の
 * 純関数) を生存敵ごとに評価する。多段カード (効果の繰り返し) は各行を独立に見積もる =
 * 急所・ブロックの消費はヒット順に減るため、2発目以降は先頭ヒット基準の概算
 */
function damageTipLines(s: GameState, c: CardInstance): string[] {
  const dmgEffects = c.def.effects.filter((e) => TIP_DAMAGE_EFFECTS.has(e.effect) && typeof e.amount === 'number')
  if (dmgEffects.length === 0) return []
  const alive = s.enemies.map((e, i) => [e, i] as const).filter(([e]) => e.hp > 0)
  if (alive.length === 0) return []
  const lines: string[] = ['⚔ ダメージ内訳（成長・勢い・弱体・急所・装甲・敵ブロック込み）']
  dmgEffects.forEach((ef, k) => {
    const head = dmgEffects.length > 1 ? `効果${k + 1} ` : ''
    for (const [enemy, i] of alive) {
      const name = (() => {
        try {
          return getEnemyDef(enemy.enemyId).name
        } catch {
          return enemy.enemyId
        }
      })()
      const bd = damageBreakdown(s, i, ef.amount!, ef.pierce === true)
      if (bd === null) continue
      // ラベルと走り値は = で区切る (「成長+1」+値6 が「成長+16」に見えた崩れの修正 2026-09-01)
      const chain = bd.steps.map((st) => `${st.label}=${st.value}`).join(' → ')
      if (ef.effect === 'dealDamageRandom' && typeof ef.amountMax === 'number') {
        const bdMax = damageBreakdown(s, i, ef.amountMax, ef.pierce === true)
        lines.push(`${head}${alive.length > 1 ? `${name}: ` : ''}${chain} ⇒ HP減 ${bd.hpLoss}〜${bdMax?.hpLoss ?? bd.hpLoss}（ロール幅）`)
      } else {
        lines.push(`${head}${alive.length > 1 ? `${name}: ` : ''}${chain} ⇒ HP減 ${bd.hpLoss}`)
      }
    }
  })
  if (dmgEffects.length > 1) lines.push('※多段は各行独立の見積り（急所・敵ブロックの消費は先頭ヒット基準）')
  return lines
}

function BattleScreen({
  state: s,
  config,
  dispatch,
  onRestart,
  onBack,
  onExport,
  extraChip,
  backLabel,
  run,
}: {
  state: GameState
  config: Config
  dispatch: (c: Command) => void
  onRestart: (seed: number) => void
  onBack: () => void
  /** 状況をファイルに書き出す (プレイテストの事実をAIへ渡すため) */
  onExport: () => void
  extraChip?: string
  backLabel?: string
  /** ラン中の戦闘なら渡す (デッキ全体・マップの閲覧ビューに使う。単発検証戦では undefined) */
  run?: RunState | null
}) {
  const player = s.player
  const isSetMode = s.reactionMode !== 'hold-manual'
  const ended = s.phase === 'won' || s.phase === 'lost'
  const aliveCount = s.enemies.filter((e) => e.hp > 0).length
  // 誘発確認ウィンドウの対象敵 (pendingWindow の enemyIndex)
  const windowEnemy = s.pendingWindow ? s.enemies[s.pendingWindow.enemyIndex] : s.enemies[0]
  // 手札捨てコストの選択中状態 (UIローカル。対象カードが手札を離れたら自動で無効化)
  // カードホバーのダメージ内訳 (2026-09-01)
  const [hoverUid, setHoverUid] = useState<string | null>(null)
  // 結果の浮き数字 (2026-09-01 機能フィードバック演出)。eventLog の新着から導出する
  interface FloatNum { id: number; where: 'player' | number; text: string; cls: string }
  const [floats, setFloats] = useState<readonly FloatNum[]>([])
  const floatSeq = useRef(0)
  const prevLogLen = useRef(0)
  useEffect(() => {
    if (prevLogLen.current > s.eventLog.length) prevLogLen.current = 0 // 新しい戦闘でリセット
    const news = s.eventLog.slice(prevLogLen.current)
    prevLogLen.current = s.eventLog.length
    const add: FloatNum[] = []
    const push = (where: 'player' | number, text: string, cls: string) =>
      add.push({ id: ++floatSeq.current, where, text, cls })
    for (const e of news) {
      switch (e.type) {
        case 'DamageDealt':
          if (e.source === 'player' && e.enemyIndex !== undefined) {
            push(e.enemyIndex, e.hpLoss > 0 ? `-${e.hpLoss}` : 'ブロック', e.hpLoss > 0 ? 'float-dmg' : 'float-miss')
          } else if (e.source === 'enemy') {
            push('player', e.hpLoss > 0 ? `-${e.hpLoss}` : '完全に防いだ', e.hpLoss > 0 ? 'float-dmg' : 'float-miss')
          }
          break
        case 'BlockGained':
          if (e.target === 'player') push('player', `🛡+${e.amount}`, 'float-block')
          break
        case 'IceBlockGained':
          push('player', `❄+${e.amount}`, 'float-block')
          break
        case 'HpHealed':
          if (e.amount > 0) push('player', `+${e.amount}`, 'float-heal')
          break
        case 'HpLost':
          push('player', `-${e.amount}`, 'float-dmg')
          break
        case 'ThornsReflected':
          if (e.hpLoss > 0) push('player', `🦔-${e.hpLoss}`, 'float-dmg')
          break
      }
    }
    if (add.length > 0) {
      setFloats((f) => [...f, ...add].slice(-16))
      const ids = new Set(add.map((a) => a.id))
      setTimeout(() => setFloats((f) => f.filter((x) => !ids.has(x.id))), 950)
    }
  }, [s.eventLog.length]) // eslint-disable-line react-hooks/exhaustive-deps
  const floatsFor = (where: 'player' | number) =>
    floats.filter((f) => f.where === where).map((f, i) => (
      <span key={f.id} className={`float-num ${f.cls}`} style={{ marginLeft: `${(i % 3) * 26 - 26}px`, top: `${22 + (i % 2) * 18}%` }}>
        {f.text}
      </span>
    ))


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
  // 消滅コストの選択中状態 (複数枚対応: chosen に選択済み uid を貯める)
  const [pendingExhaust, setPendingExhaust] = useState<{
    cardUid: string
    modeIndex?: number
    chosen: string[]
  } | null>(null)
  const activeExhaust =
    pendingExhaust &&
    s.phase === 'player-turn' &&
    player.hand.some((c) => c.uid === pendingExhaust.cardUid)
      ? pendingExhaust
      : null
  // 消滅置き場ピッカー (屍集め・死者再生 の retrieveUid 選択中)
  const [pendingRetrieve, setPendingRetrieve] = useState<{
    cardUid: string
    modeIndex?: number
    exhaustUids?: string[]
  } | null>(null)
  const activeRetrieve =
    pendingRetrieve &&
    s.phase === 'player-turn' &&
    player.hand.some((c) => c.uid === pendingRetrieve.cardUid)
      ? pendingRetrieve
      : null
  // 引導 (2026-08-31): 山札か捨て札から消滅させる札を選んでいる状態
  const [pendingDeckChoose, setPendingDeckChoose] = useState<{
    cardUid: string
    modeIndex?: number
  } | null>(null)
  const [pendingUpgrade, setPendingUpgrade] = useState<{ cardUid: string; modeIndex?: number } | null>(null)
  const [pendingX, setPendingX] = useState<{ cardUid: string; modeIndex?: number } | null>(null)
  const activeX =
    pendingX && s.phase === 'player-turn' && player.hand.some((c) => c.uid === pendingX.cardUid) ? pendingX : null
  const activeUpgrade =
    pendingUpgrade && s.phase === 'player-turn' && player.hand.some((c) => c.uid === pendingUpgrade.cardUid)
      ? pendingUpgrade
      : null
  const activeDeckChoose =
    pendingDeckChoose &&
    s.phase === 'player-turn' &&
    player.hand.some((c) => c.uid === pendingDeckChoose.cardUid)
      ? pendingDeckChoose
      : null
  // StS式ターゲティング: 単体対象カードのプレイ時、敵タップ待ちの状態
  const [pendingTarget, setPendingTarget] = useState<{
    cardUid: string
    modeIndex?: number
    discardUids?: string[]
    exhaustUids?: string[]
    retrieveUid?: string
    deckUids?: string[]
    handUids?: string[]
    xAmount?: number
  } | null>(null)
  const activeTarget =
    pendingTarget &&
    s.phase === 'player-turn' &&
    player.hand.some((c) => c.uid === pendingTarget.cardUid)
      ? pendingTarget
      : null
  // 対象が要るカードなら敵タップ待ちへ、不要なら即プレイ
  const playOrTarget = (
    cardUid: string,
    modeIndex?: number,
    discardUids?: string[],
    exhaustUids?: string[],
    retrieveUid?: string,
    deckUids?: string[],
    handUids?: string[],
    xAmount?: number,
  ) => {
    const card = player.hand.find((c) => c.uid === cardUid)
    if (card && aliveCount > 1 && cardNeedsTarget(card, modeIndex)) {
      setPendingTarget({ cardUid, modeIndex, discardUids, exhaustUids, retrieveUid, deckUids, handUids, xAmount })
    } else {
      dispatch({ type: 'PlayCard', cardUid, modeIndex, discardUids, exhaustUids, retrieveUid, deckUids, handUids, xAmount })
    }
  }
  // 追加コスト・消滅置き場選択を済ませてからプレイに進む多段フロー:
  // 消滅コスト → 消滅置き場ピッカー → 対象選択 の順
  const startPlay = (cardUid: string, modeIndex?: number) => {
    const card = player.hand.find((c) => c.uid === cardUid)
    if (!card) return
    if ((card.def.discardCost ?? 0) > 0) {
      setPendingDiscard({ cardUid, modeIndex })
      return
    }
    if ((card.def.exhaustCost ?? 0) > 0) {
      setPendingExhaust({ cardUid, modeIndex, chosen: [] })
      return
    }
    if (card.def.effects.some((e) => e.effect === 'retrieveFromExhaust' || e.effect === 'playFromExhaust')) {
      setPendingRetrieve({ cardUid, modeIndex })
      return
    }
    // 引導/回収/サーチ: 選ぶ山が空でなければ札を選ばせる (空なら選択なしでプレイ)
    const chooseKind = deckChooseKindOf(card.def)
    const choosePoolSize =
      chooseKind === 'retrieveFromDiscard'
        ? player.discardPile.length
        : chooseKind === 'searchDeck'
          ? player.drawPile.length
          : player.drawPile.length + player.discardPile.length
    if (chooseKind !== null && choosePoolSize > 0) {
      setPendingDeckChoose({ cardUid, modeIndex })
      return
    }
    // Xコスト (2026-09-03): 払うXを1〜エナジーから選ばせる (1なら即プレイ)
    if (card.def.xCost === true && player.energy > 1) {
      setPendingX({ cardUid, modeIndex })
      return
    }
    // 手札で鍛える (研ぎ澄まし 2026-09-02): 自身以外に鍛えられる手札があれば選ばせる
    if (
      card.def.effects.some((e) => e.effect === 'upgradeInHand') &&
      player.hand.some((c) => c.uid !== cardUid && canUpgradeInHand(c))
    ) {
      setPendingUpgrade({ cardUid, modeIndex })
      return
    }
    playOrTarget(cardUid, modeIndex)
  }
  const lines = s.eventLog.map(logLine).filter((l): l is LogLine => l !== null)
  const setCard = player.setCards[0]
  // 閲覧ビュー (2026-08-31): 山札・捨て札・消滅置き場・デッキ全体・マップをいつでも確認できる
  const [pileView, setPileView] = useState<'draw' | 'discard' | 'exhaust' | 'deck' | 'map' | null>(
    null,
  )
  const pileCtx: EffectCtx = { growth: player.growth, momentum: player.momentum, energyMax: player.energyMaxAtTurnStart ?? player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether, exhausted: player.exhaustPile.length, selfHpLost: player.selfHpLost, permanents: player.permanents.length, damageTaken: player.damageTakenLastEnemyPhase, iceBlock: player.iceBlock, randomPlayed: player.randomPlayedThisCombat, energy: player.energy, handCards: Math.max(0, player.hand.length - 1) }

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
        <span>
          <button className="btn" onClick={onExport}>
            📄 状況を書き出す
          </button>{' '}
          <button className="btn" onClick={onBack}>
            {backLabel ?? '設定に戻る'}
          </button>
        </span>
      </div>

      {/* 敵ゾーン (1〜3体)。ターゲット選択中は敵をタップして対象決定 */}
      <div className="panel area-enemy">
        {activeTarget && (
          <div className="discard-banner">
            「{player.hand.find((c) => c.uid === activeTarget.cardUid)?.def.name}
            」の対象を選んでください（敵をタップ）{' '}
            <button className="btn" data-hotkey="cancel" onClick={() => setPendingTarget(null)}>
              キャンセル
            </button>
          </div>
        )}
        <div className="enemy-zone">
          {s.enemies.map((enemy, i) => {
            const enemyDef = getEnemyDef(enemy.enemyId)
            const dead = enemy.hp <= 0
            // 庇う (2026-09-02): 護衛の生存中、単体対象は護衛しか選べない (エンジンのリダイレクトと同じ判定)
            const guardIdx = s.enemies.findIndex(
              (e) => e.hp > 0 && getEnemyDef(e.enemyId).guardian === true,
            )
            const guarded = guardIdx >= 0 && guardIdx !== i
            const targetable = activeTarget !== null && !dead && !guarded
            return (
              <div
                key={i}
                className={`enemy-card${targetable ? ' enemy-targetable' : ''}${dead ? ' enemy-dead' : ''}`}
                style={{ position: 'relative' }}
                {...(targetable && i < 9 ? { 'data-hotkey': `num-${i + 1}` } : {})}
                onClick={() => {
                  if (!targetable || !activeTarget) return
                  dispatch({
                    type: 'PlayCard',
                    cardUid: activeTarget.cardUid,
                    modeIndex: activeTarget.modeIndex,
                    discardUids: activeTarget.discardUids,
                    exhaustUids: activeTarget.exhaustUids,
                    retrieveUid: activeTarget.retrieveUid,
                    deckUids: activeTarget.deckUids,
                    targetIndex: i,
                  })
                  setPendingTarget(null)
                }}
              >
                <div className="float-layer">{floatsFor(i)}</div>
                <div className="enemy-sprite">{enemy.fled ? '🏃' : dead ? '💀' : ARCHETYPE_SPRITE[enemyDef.archetype]}</div>
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
                    {enemy.strength !== 0 && (
                      <span className="chip chip-strength">💪 {kw('筋力')} {enemy.strength >= 0 ? '+' : ''}{enemy.strength}</span>
                    )}
                    {enemy.burn > 0 && (
                      <span className="chip chip-strength">🔥 {kw('延焼')} {enemy.burn}</span>
                    )}
                    {enemy.confusion > 0 && (
                      <span className="chip chip-aether">😵‍💫 {kw('混乱')} {enemy.confusion}</span>
                    )}
                    {enemy.exposed > 0 && (
                      <span className="chip chip-growth">🎯 {kw('急所')} {enemy.exposed}</span>
                    )}
                    {enemyDef.regen !== undefined && enemy.hp > enemy.maxHp * 0.5 && !dead && (
                      <span className="chip chip-strength">♻️ {kw('再生')} +{enemyDef.regen}</span>
                    )}
                    {enemyDef.regenBreak !== undefined && !dead && (
                      <span className="chip chip-strength">
                        💚 {kw('再生')} {enemyDef.regen}（このターン{enemyDef.regenBreak}以上削ると停止）
                      </span>
                    )}
                    {enemyDef.armor !== undefined && !dead && (
                      <span className="chip chip-strength">🛡 {kw('装甲')}{enemyDef.armor}</span>
                    )}
                    {enemyDef.burnResist !== undefined && !dead && (
                      <span className="chip chip-strength">💧 {kw('延焼耐性')} -{enemyDef.burnResist}</span>
                    )}
                    {enemyDef.angerOnBlock !== undefined && !dead && (
                      <span className="chip chip-strength">😤 あなたがカードでブロック・氷壁を得るたび筋力+{enemyDef.angerOnBlock}（パッシブ・レリックの自動分は除く）</span>
                    )}
                    {enemyDef.thorns !== undefined && !dead && (
                      <span className="chip chip-strength">🦔 {kw('とげ')} {enemyDef.thorns}</span>
                    )}
                    {enemyDef.splitInto !== undefined && !dead && (
                      <span className="chip chip-strength">
                        {enemyDef.splitInto.count === 1
                          ? <>♻️ {kw('残機')}: 倒すと{getEnemyDef(enemyDef.splitInto.enemyId).name}（HP{getEnemyDef(enemyDef.splitInto.enemyId).maxHp}）で再起動</>
                          : <>🫠 {kw('分裂')}: 倒すと{getEnemyDef(enemyDef.splitInto.enemyId).name}×{enemyDef.splitInto.count}{enemyDef.splitInto.stunned === true ? '（出現ターンは動かない）' : ''}</>}
                      </span>
                    )}
                    {enemyDef.hatchInto !== undefined && !dead && (
                      <span className="chip chip-strength">🥚 {kw('孵化')}: {getEnemyDef(enemyDef.hatchInto.enemyId).name}になる{(() => {
                        const t = turnsUntilHatch(s, i)
                        return t === null ? '' : t === 0 ? '（このフェーズで孵化！）' : `（あと${t}手）`
                      })()}</span>
                    )}
                    {enemyDef.mournStrength !== undefined && !dead && (
                      <span className="chip chip-strength">🕯️ {kw('弔い')}+{enemyDef.mournStrength}</span>
                    )}
                    {enemyDef.turnArmor !== undefined && !dead && (
                      <span className="chip chip-strength">🪨 {kw('ターン装甲')}{enemyDef.turnArmor}（残り{Math.max(0, enemyDef.turnArmor - (enemy.damageThisTurn ?? 0))}）{enemy.hp > Math.max(0, enemyDef.turnArmor - (enemy.damageThisTurn ?? 0)) ? ' ⚠今ターン倒せない' : ''}</span>
                    )}
                    {(enemy.artifact ?? 0) > 0 && !dead && (
                      <span className="chip chip-strength">🔮 {kw('アーティファクト')} {enemy.artifact}</span>
                    )}
                    {enemyDef.wakeOnDamage !== undefined && enemy.woken !== true && enemy.patternIndex < enemyDef.wakeOnDamage.resumeAt && !dead && (
                      <span className="chip">😴 {kw('眠り')}: 累計{enemyDef.wakeOnDamage.damage}ダメで目覚める（現在{enemy.damageTakenTotal ?? 0}）</span>
                    )}
                    {enemyDef.moves.some((m) => m.growPerUse !== undefined || m.growHitsPerUse !== undefined) && !dead && (
                      <span className="chip chip-strength">📈 {kw('育つ技')}: {enemyDef.moves.filter((m) => m.growPerUse !== undefined || m.growHitsPerUse !== undefined).map((m) => `${m.growPerUse ? `+${m.growPerUse}` : ''}${m.growHitsPerUse ? `ヒット+${m.growHitsPerUse}` : ''}/使用（現在${enemy.moveGrowth?.[m.id] ?? 0}回）`).join('・')}</span>
                    )}
                    {enemyDef.guardian === true && !dead && (
                      <span className="chip chip-strength">🛡️ {kw('庇う')}</span>
                    )}
                    {guarded && !dead && (
                      <span className="chip chip-block">🛡️ 庇われている（単体対象はこの敵を選べない）</span>
                    )}
                    {enemyDef.bondStrength !== undefined && !dead && (
                      <span className="chip chip-strength">🤝 {kw('連携')}+{enemyDef.bondStrength}</span>
                    )}
                    {enemyDef.aura !== undefined && !dead && (
                      <span className="chip chip-strength">🕸️ {kw('重圧')}: {enemyDef.aura.cardType !== undefined ? TYPE_LABEL[enemyDef.aura.cardType] : '全'}カード+{enemyDef.aura.costUp}</span>
                    )}
                    {(enemy.stolenGold ?? 0) > 0 && !dead && (
                      <span className="chip chip-growth">💰 {enemy.stolenGold}G 抱え込み（次の宣言で必ず逃走）</span>
                    )}
                    {enemy.fled === true && (
                      <span className="chip">
                        🏃 逃走済み{(enemy.stolenGold ?? 0) > 0 ? `（${enemy.stolenGold}G持ち逃げ）` : ''}
                      </span>
                    )}
                    {(enemyDef.movesBelowHalf || enemyDef.sequenceBelowHalf) &&
                      enemy.hp <= enemy.maxHp * 0.5 &&
                      !dead && <span className="chip chip-strength">😾 牙をむいている</span>}
                    {(enemyDef.movesBelowHalf || enemyDef.sequenceBelowHalf) &&
                      enemy.hp > enemy.maxHp * 0.5 &&
                      !dead && <span className="chip">😾 HP半分で豹変</span>}
                    {enemyDef.enrage !== undefined && !dead && (
                      <span className="chip chip-strength">
                        😡 {kw('激昂')} +{enemyDef.enrage}
                        {enemyDef.enrageEveryCards ? `/${enemyDef.enrageEveryCards}枚プレイ（あと${enemyDef.enrageEveryCards - (s.player.cardsPlayedTotal % enemyDef.enrageEveryCards)}枚）` : '/T'}
                        {enemyDef.enrageEveryDamage !== undefined ? `・+${enemyDef.enrage}/被ダメ${enemyDef.enrageEveryDamage}（あと${enemyDef.enrageEveryDamage - ((enemy.damageTakenTotal ?? 0) % enemyDef.enrageEveryDamage)}）` : ''}
                      </span>
                    )}
                  </div>
                  {!ended && !dead && (
                    <div className={`intent${enemy.intent?.kind === 'defend' ? ' intent-defend' : ''}`}>
                      {enemy.confusion > 0 && enemy.intent?.kind === 'attack' ? '😵仲間に向かう: ' : ''}
                      {kw(conditionalIntentText(s, i))}
                      {enemy.intent?.mirrorHits === true ? `（現在${player.cardsPlayedThisTurn + (player.setsThisTurn ?? 0)}枚。伏せも数える）` : ''}
                      {worstIncomingFrom(s, i) - (player.block + player.iceBlock) >= player.hp
                        ? ' 💀致死級'
                        : null}
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
              {player.setCards.map((c) => (
                <span key={c.uid}>
                  <div className="card card-back">伏</div>
                  <div className="set-slot-label">
                    {c.def.name}
                    {c.setFresh !== true &&
                      (() => {
                        // 罰型 (見切り無視) の敵が生存中なら「反応しない」は嘘になる (2026-09-03 Opusラン I 指摘)
                        const pun = s.enemies
                          .map((en, ei) => ({ en, ei }))
                          .filter((x) => x.en.hp > 0 && setReactionIgnoresFreshness(s, x.ei) && x.en.intent?.alt?.kind !== 'destroy-set')
                          .map((x) => getEnemyDef(x.en.enemyId).name)
                        return pun.length > 0 ? (
                          <span title={`罰型の敵は見切りを無視する: ${pun.join('・')}`}>（見切られ・{pun.join('・')}は反応）</span>
                        ) : (
                          <span title="敵はこの札に反応しない (織り込み済み)">（見切られ）</span>
                        )
                      })()}
                    {c.def.type !== 'reaction' && (
                      <span title="通常カードの伏せ (実験): 誘発したら印字コストを払って発動">（被攻撃{setWindowStage(c.def) === 'pre' ? '前' : '後'}・発動{setFireCost(c)}E）</span>
                    )}
                  </div>
                  {s.phase === 'player-turn' && (
                    <button
                      className="btn"
                      disabled={player.energy < 1}
                      title="1E払って手札に戻す (伏せコストは返らない)"
                      onClick={() => dispatch({ type: 'RetrieveSetCard', cardUid: c.uid })}
                    >
                      回収(1E)
                    </button>
                  )}
                </span>
              ))}
              {Array.from({ length: Math.max(0, player.setSlots - player.setCards.length) }).map((_, i) => (
                <span key={`empty${i}`}>
                  <div className="set-slot-empty">伏せ場</div>
                  <div className="set-slot-label">（なし）</div>
                </span>
              ))}
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
                    ctx={{ growth: player.growth, momentum: player.momentum, energyMax: player.energyMaxAtTurnStart ?? player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether, exhausted: player.exhaustPile.length, selfHpLost: player.selfHpLost, permanents: player.permanents.length, damageTaken: player.damageTakenLastEnemyPhase, iceBlock: player.iceBlock, randomPlayed: player.randomPlayedThisCombat, energy: player.energy, handCards: Math.max(0, player.hand.length - 1) }}
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
                {kw(confirmedIntentText(s.pendingWindow ? effectiveIntent(s, s.pendingWindow.enemyIndex) : (windowEnemy?.intent ?? null)))}
              </div>
              {s.pendingWindow?.stage === 'post' && (
                <div className="hint" style={{ marginBottom: 8 }}>
                  {effectiveIntent(s, s.pendingWindow.enemyIndex)?.kind === 'attack'
                    ? '※この攻撃はすでに解決済み——発動しても今回の被弾は取り消せない（返し・回復のための窓）'
                    : '※この行動はすでに解決済み（行動に反応するための窓）'}
                </div>
              )}
              {s.reactionMode === 'set-confirm' && setCard ? (
                <>
                  {(() => {
                    // 伏せ2枚 (かすみ): 窓に合致する伏せ札ごとに発動ボタンを出す
                    const win = windowFromPending(s)
                    const candidates = win ? usableSetCards(s, win) : []
                    return candidates.map((c, candIdx) => (
                      <div key={c.uid} style={{ marginBottom: 8 }}>
                        「{c.def.name}」（
                        {kw(
                          effectText(c.def, {
                            growth: player.growth,
                            momentum: player.momentum,
                            energyMax: player.energyMaxAtTurnStart ?? player.energyMax,
                            exhausted: player.exhaustPile.length,
                            selfHpLost: player.selfHpLost,
                            permanents: player.permanents.length,
                            damageTaken: player.damageTakenLastEnemyPhase,
                            randomPlayed: player.randomPlayedThisCombat,
                            iceBlock: player.iceBlock,
                            cardsPlayed: player.cardsPlayedThisTurn,
                            aether: player.aether,
                            energy: player.energy, handCards: Math.max(0, player.hand.length - 1),
                          }),
                        )}
                        ）{' '}
                        <button
                          className="btn btn-primary"
                          {...(candIdx < 9 ? { 'data-hotkey': `num-${candIdx + 1}` } : {})}
                          onClick={() => dispatch({ type: 'ConfirmReaction', fire: true, cardUid: c.uid })}
                        >
                          {setFireCost(c) > 0 ? `発動する（${setFireCost(c)}E・残り${player.energy}E）` : '発動する'}
                        </button>
                      </div>
                    ))
                  })()}
                  {setBranchFlipRisks(s).map((ri) => {
                    const it = s.enemies[ri].intent!
                    const threat = (k: string, mx: number, h?: number) => (k === 'attack' ? mx * (h ?? 1) : 0)
                    const after = threat(it.kind, it.shownMax, it.hits)
                    const before = it.alt ? threat(it.alt.kind, it.alt.shownMax, it.alt.hits) : after
                    const comparable = it.kind === 'attack' && it.alt?.kind === 'attack'
                    const gain = comparable && after < before
                    return (
                      <div key={ri} className="choice-desc" style={{ margin: '6px 0', color: gain ? 'var(--good, #7ec97e)' : 'var(--warn, #e0a458)' }}>
                        {gain ? '💡' : '⚠'} 発動すると伏せ枠が空く: {getEnemyDef(s.enemies[ri].enemyId).name}の行動が【伏せなし】分岐（{intentText(s.enemies[ri].intent)}）に変わる{gain ? '（弱くなる=利得）' : comparable && after > before ? '（強くなる）' : ''}
                      </div>
                    )
                  })}
                  <button className="btn" data-hotkey="hold" onClick={() => dispatch({ type: 'ConfirmReaction', fire: false })}>
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
                      {c.def.name}({cardCostLabel(c.def)}) —{' '}
                      {effectText(c.def, { growth: player.growth, momentum: player.momentum, energyMax: player.energyMaxAtTurnStart ?? player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether, exhausted: player.exhaustPile.length, selfHpLost: player.selfHpLost, permanents: player.permanents.length, damageTaken: player.damageTakenLastEnemyPhase, iceBlock: player.iceBlock, randomPlayed: player.randomPlayedThisCombat, energy: player.energy, handCards: Math.max(0, player.hand.length - 1) })}
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
              {s.phase === 'won' && (
                <p className="hint">⚔️ 戦いの記録: {summaryLine(battleSummary(s.eventLog))}</p>
              )}
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
      {/* 今フェーズの最悪被ダメ予測 (複数体の暗算を不要にする。2026-08-25 プレイテスト対応) */}
      {s.phase === 'player-turn' &&
        (() => {
          // 式は engine/summary.ts の worstIncomingTotal に1本化 (2026-09-02 レビュー是正:
          // フッター・💀バッジ・CLIで合成順が3通りに割れていた)
          const worst = worstIncomingTotal(s) // 0でも出す (2026-09-02: 非攻撃ターンに行が消えると表示漏れと迷う)
          const defense = player.block + player.iceBlock
          const through = Math.max(0, worst - defense)
          return (
            <div className={`panel forecast${through >= player.hp ? ' forecast-danger' : ''}`}>
              ⚠️ 最悪被ダメ {worst} − 防御 {defense} = <b>{through}</b>（HP {player.hp}）
            </div>
          )
        })()}

      <div className="panel area-player" style={{ position: 'relative' }}>
        <div className="float-layer">{floatsFor('player')}</div>
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
            {player.spellEchoes > 0 && (
              <span className="chip chip-aether">🔁 {kw('反復')} {player.spellEchoes}</span>
            )}
            {(() => {
              const anthem = player.permanents.reduce(
                (a, c) =>
                  a +
                  c.def.effects
                    .filter((e) => e.effect === 'blessRetainers')
                    .reduce((x, e) => x + (e.amount ?? 0), 0),
                0,
              )
              return anthem > 0 ? (
                <span className="chip chip-aether">✨ アンセム+{anthem}（従者の量つき効果に加算）</span>
              ) : null
            })()}
            {player.nextCardDiscount > 0 && (
              <span className="chip chip-aether">🔥 次のカード-{player.nextCardDiscount}</span>
            )}
            {player.weak > 0 && (
              <span className="chip chip-strength">😵 {kw('弱体')} {player.weak}</span>
            )}
            {player.vulnerable > 0 && (
              <span className="chip chip-strength">💔 {kw('脆弱')} {player.vulnerable}</span>
            )}
            {player.frail > 0 && (
              <span className="chip chip-strength">🦴 {kw('虚弱')} {player.frail}</span>
            )}
            {(player.mist ?? 0) > 0 && (
              <span className="chip chip-strength">🌫️ {kw('霞み')} {player.mist}</span>
            )}
            {(player.slow ?? 0) > 0 && (
              <span className="chip chip-strength">⚓ {kw('重り')} {player.slow}（今+{(player.playsThisTurn ?? 0) * 10}%）</span>
            )}
            {player.restrain > 0 && (
              <span className="chip chip-strength">⛓️ {kw('拘束')} {player.restrain}{(player.playsThisTurn ?? 0) >= RESTRAIN_PLAY_CAP ? '（このターンはもう出せない）' : `（あと${RESTRAIN_PLAY_CAP - (player.playsThisTurn ?? 0)}枚）`}</span>
            )}
          </div>
          <div className="pile-info">
            <button className="btn btn-mini" onClick={() => setPileView('draw')}>
              山札 {player.drawPile.length}枚
            </button>
            <br />
            <button className="btn btn-mini" onClick={() => setPileView('discard')}>
              捨て札 {player.discardPile.length}枚
            </button>
            {player.exhaustPile.length > 0 && (
              <>
                <br />
                <button className="btn btn-mini" onClick={() => setPileView('exhaust')}>
                  消滅 {player.exhaustPile.length}枚
                </button>
              </>
            )}
            {run != null && (
              <>
                <br />
                <button className="btn btn-mini" onClick={() => setPileView('deck')}>
                  🎴 デッキ
                </button>{' '}
                <button className="btn btn-mini" onClick={() => setPileView('map')}>
                  🗺 マップ
                </button>
              </>
            )}
            {/* 亡骸プレイ (黒 2026-08-31): 消滅置き場の necroCost 持ち札を一度だけプレイ */}
            {s.phase === 'player-turn' &&
              player.exhaustPile
                .filter((c) => c.def.necroCost !== undefined)
                .map((c) => {
                  const nCost = c.def.necroCost ?? 0
                  const alive = s.enemies
                    .map((e, i) => ({ e, i }))
                    .filter(({ e }) => e.hp > 0)
                  const needsPick = alive.length > 1 && cardNeedsTarget(c)
                  return (
                    <div key={c.uid} style={{ marginTop: 4 }}>
                      {needsPick ? (
                        alive.map(({ e, i }) => (
                          <button
                            key={i}
                            className="btn"
                            disabled={player.energy < nCost}
                            onClick={() => dispatch({ type: 'PlayNecro', cardUid: c.uid, targetIndex: i })}
                          >
                            💀 {c.def.name}({nCost}E)→{getEnemyDef(e.enemyId).name}
                          </button>
                        ))
                      ) : (
                        <button
                          className="btn"
                          disabled={player.energy < nCost}
                          onClick={() => dispatch({ type: 'PlayNecro', cardUid: c.uid })}
                        >
                          💀 {c.def.name} 亡骸プレイ({nCost}E)
                        </button>
                      )}
                    </div>
                  )
                })}
          </div>
        </div>
      </div>

      {/* 手札 */}
      <div className="panel area-hand">
        {activeExhaust && (
          <div className="discard-banner">
            「{player.hand.find((c) => c.uid === activeExhaust.cardUid)?.def.name}」の追加コスト:
            消滅させるカードを選んでください（
            {(player.hand.find((c) => c.uid === activeExhaust.cardUid)?.def.exhaustCost ?? 0) - activeExhaust.chosen.length}
            枚）{' '}
            <button className="btn" data-hotkey="cancel" onClick={() => setPendingExhaust(null)}>
              キャンセル
            </button>
          </div>
        )}
        {activeRetrieve && (
          <div className="discard-banner">
            「{player.hand.find((c) => c.uid === activeRetrieve.cardUid)?.def.name}」:
            消滅置き場からカードを選んでください{' '}
            <button className="btn" onClick={() => setPendingRetrieve(null)}>
              キャンセル
            </button>
          </div>
        )}
        {activeRetrieve && (
          <div className="hand-row">
            <div className="hand-cards">
              {player.exhaustPile.map((c) => {
                const src = player.hand.find((h) => h.uid === activeRetrieve.cardUid)
                const directPlay = src?.def.effects.some((e) => e.effect === 'playFromExhaust') ?? false
                // 直接プレイの制約 (combat.ts と同じ): リアクション・選択式・コスト再利用カードは選べない
                const eligible =
                  !directPlay ||
                  (c.def.type !== 'reaction' &&
                    (c.def.modes?.length ?? 0) === 0 &&
                    !c.def.effects.some(
                      (e) => e.effect === 'playFromExhaust' || e.effect === 'retrieveFromExhaust',
                    ))
                return (
                  <CardFrame
                    key={c.uid}
                    card={c}
                    ctx={{ growth: player.growth, momentum: player.momentum, energyMax: player.energyMaxAtTurnStart ?? player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether, exhausted: player.exhaustPile.length, selfHpLost: player.selfHpLost, permanents: player.permanents.length, damageTaken: player.damageTakenLastEnemyPhase, iceBlock: player.iceBlock, randomPlayed: player.randomPlayedThisCombat, energy: player.energy, handCards: Math.max(0, player.hand.length - 1) }}
                    dim={!eligible}
                    hint={eligible ? undefined : directPlay ? '直接プレイ不可' : undefined}
                    actions={
                      eligible && (
                        <button
                          className="btn"
                          onClick={() => {
                            setPendingRetrieve(null)
                            playOrTarget(
                              activeRetrieve.cardUid,
                              activeRetrieve.modeIndex,
                              undefined,
                              activeRetrieve.exhaustUids,
                              c.uid,
                            )
                          }}
                        >
                          {directPlay ? '直接プレイ' : '手札に戻す'}
                        </button>
                      )
                    }
                  />
                )
              })}
            </div>
          </div>
        )}
        {activeX && (
          <div className="discard-banner">
            「{player.hand.find((c) => c.uid === activeX.cardUid)?.def.name}」: 払うXを選んでください（最大{player.energy}＝全部。残したエナジーは他の札や伏せに使える）{' '}
            {Array.from({ length: player.energy }, (_, i) => i + 1).map((x) => (
              <button
                key={x}
                className={x === player.energy ? 'btn btn-primary' : 'btn'}
                {...(x <= 9 ? { 'data-hotkey': `num-${x}` } : {})}
                onClick={() => {
                  setPendingX(null)
                  playOrTarget(activeX.cardUid, activeX.modeIndex, undefined, undefined, undefined, undefined, undefined, x)
                }}
              >
                X={x}
              </button>
            ))}
            <button className="btn" onClick={() => setPendingX(null)}>
              キャンセル
            </button>
          </div>
        )}
        {activeUpgrade && (
          <div className="discard-banner">
            「{player.hand.find((c) => c.uid === activeUpgrade.cardUid)?.def.name}」: この戦闘中鍛える手札を選んでください（自身は選べない）{' '}
            <button className="btn" onClick={() => setPendingUpgrade(null)}>
              キャンセル
            </button>
          </div>
        )}
        {activeUpgrade && (
          <div className="hand-row">
            <div className="hand-cards">
              {player.hand
                .filter((c) => c.uid !== activeUpgrade.cardUid)
                .map((c) => {
                  const ok = canUpgradeInHand(c)
                  return (
                    <CardFrame
                      key={c.uid}
                      card={c}
                      dim={!ok}
                      hint={ok ? `鍛えると→ ${upgradeCard(c).def.name}` : c.def.rarity === 'rare' || c.def.id.startsWith('fus') ? 'レア・工房産は鍛えられない' : '鍛えられない'}
                      actions={
                        ok && (
                          <button
                            className="btn"
                            onClick={() => {
                              setPendingUpgrade(null)
                              playOrTarget(activeUpgrade.cardUid, activeUpgrade.modeIndex, undefined, undefined, undefined, undefined, [c.uid])
                            }}
                          >
                            鍛える
                          </button>
                        )
                      }
                    />
                  )
                })}
            </div>
          </div>
        )}
        {activeDeckChoose && (
          <div className="discard-banner">
            「{player.hand.find((c) => c.uid === activeDeckChoose.cardUid)?.def.name}」:
            {(() => {
              const k = deckChooseKindOf(player.hand.find((c) => c.uid === activeDeckChoose.cardUid)?.def ?? { effects: [] } as unknown as CardDef)
              return k === 'retrieveFromDiscard'
                ? '捨て札から手札に戻すカードを選んでください'
                : k === 'searchDeck'
                  ? '山札から手札に加えるカードを選んでください（並び替え表示＝引き順は伏せたまま）'
                  : '山札か捨て札から消滅させるカードを選んでください（山札は並び替え表示＝引き順は伏せたまま）'
            })()}{' '}
            <button className="btn" onClick={() => setPendingDeckChoose(null)}>
              キャンセル
            </button>
          </div>
        )}
        {activeDeckChoose && (
          <div className="hand-row">
            <div className="hand-cards">
              {(() => {
                const k = deckChooseKindOf(player.hand.find((c) => c.uid === activeDeckChoose.cardUid)?.def ?? ({ effects: [] } as unknown as CardDef))
                const drawSorted = [...player.drawPile]
                  .sort((a, b) => a.def.cost - b.def.cost || a.def.name.localeCompare(b.def.name, 'ja'))
                  .map((c) => ({ c, src: '山札' }))
                const disc = player.discardPile.map((c) => ({ c, src: '捨て札' }))
                const verb = k === 'retrieveFromDiscard' ? '手札に戻す' : k === 'searchDeck' ? '手札に加える' : '消滅させる'
                const items = k === 'retrieveFromDiscard' ? disc : k === 'searchDeck' ? drawSorted : [...drawSorted, ...disc]
                return items.map(({ c, src }) => ({ c, src, verb }))
              })().map(({ c, src, verb }) => (
                <CardFrame
                  key={c.uid}
                  card={c}
                  dim={false}
                  hint={src}
                  actions={
                    <button
                      className="btn"
                      onClick={() => {
                        setPendingDeckChoose(null)
                        playOrTarget(
                          activeDeckChoose.cardUid,
                          activeDeckChoose.modeIndex,
                          undefined,
                          undefined,
                          undefined,
                          [c.uid],
                        )
                      }}
                    >
                      {verb}（{src}）
                    </button>
                  }
                />
              ))}
            </div>
          </div>
        )}
        {activeDiscard && (
          <div className="discard-banner">
            「{player.hand.find((c) => c.uid === activeDiscard.cardUid)?.def.name}」の追加コスト:
            捨てるカードを選んでください{' '}
            <button className="btn" data-hotkey="cancel" onClick={() => setPendingDiscard(null)}>
              キャンセル
            </button>
          </div>
        )}
        <div className="hand-row">
          <div className="hand-cards">
            {s.phase === 'player-turn' &&
              player.hand.map((c, handIdx) => {
                const modes = c.def.modes ?? []
                const discardCost = c.def.discardCost ?? 0
                const exhaustCostN = c.def.exhaustCost ?? 0
                const effCost = effectiveCost(s, c)
                // 消滅置き場を参照するカード: 選べるカードがなければプレイ不可
                const needsPile = c.def.effects.some(
                  (e) => e.effect === 'retrieveFromExhaust' || e.effect === 'playFromExhaust',
                )
                const isDirectPlay = c.def.effects.some((e) => e.effect === 'playFromExhaust')
                const pileOk =
                  !needsPile ||
                  player.exhaustPile.some(
                    (p) =>
                      !isDirectPlay ||
                      (p.def.type !== 'reaction' &&
                        (p.def.modes?.length ?? 0) === 0 &&
                        !p.def.effects.some(
                          (e) => e.effect === 'playFromExhaust' || e.effect === 'retrieveFromExhaust',
                        )),
                  )
                const canPlay =
                  isPlayableFromHand(c) &&
                  !(player.restrain > 0 && (player.playsThisTurn ?? 0) >= RESTRAIN_PLAY_CAP) &&
                  effCost <= player.energy &&
                  player.hand.length - 1 >= discardCost &&
                  player.hand.length - 1 >= exhaustCostN &&
                  pileOk
                const settable = c.def.type === 'reaction' || (s.setAnyCards === true && canSetAsNormal(c.def))
                const canSet = isSetMode && settable && canSetCard(s, c.uid)
                const heldReaction = !isSetMode && c.def.type === 'reaction'
                // 消滅コスト選択中: 手札は「消滅させる」対象として振る舞う (複数枚は順に選ぶ)
                if (activeExhaust) {
                  const src = player.hand.find((h) => h.uid === activeExhaust.cardUid)
                  const need = src?.def.exhaustCost ?? 0
                  const isSource = c.uid === activeExhaust.cardUid
                  const isChosen = activeExhaust.chosen.includes(c.uid)
                  return (
                    <CardFrame
                      key={c.uid}
                      card={c}
                      ctx={{ growth: player.growth, momentum: player.momentum, energyMax: player.energyMaxAtTurnStart ?? player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether, exhausted: player.exhaustPile.length, selfHpLost: player.selfHpLost, permanents: player.permanents.length, damageTaken: player.damageTakenLastEnemyPhase, iceBlock: player.iceBlock, randomPlayed: player.randomPlayedThisCombat, energy: player.energy, handCards: Math.max(0, player.hand.length - 1) }}
                      dim={isSource || isChosen}
                      hint={isSource ? 'プレイするカード' : isChosen ? '消滅予定' : undefined}
                      actions={
                        !isSource &&
                        !isChosen && (
                          <button
                            className="btn"
                            onClick={() => {
                              const chosen = [...activeExhaust.chosen, c.uid]
                              if (chosen.length >= need) {
                                const srcCard = player.hand.find((h) => h.uid === activeExhaust.cardUid)
                                setPendingExhaust(null)
                                // 消滅コストの次に消滅置き場ピッカーが要るカードは現状ないが、順序は保険で維持
                                if (
                                  srcCard?.def.effects.some(
                                    (e) => e.effect === 'retrieveFromExhaust' || e.effect === 'playFromExhaust',
                                  )
                                ) {
                                  setPendingRetrieve({ cardUid: activeExhaust.cardUid, modeIndex: activeExhaust.modeIndex, exhaustUids: chosen })
                                } else {
                                  playOrTarget(activeExhaust.cardUid, activeExhaust.modeIndex, undefined, chosen)
                                }
                              } else {
                                setPendingExhaust({ ...activeExhaust, chosen })
                              }
                            }}
                          >
                            これを消滅
                          </button>
                        )
                      }
                    />
                  )
                }
                // 捨てコスト選択中: 手札は「捨てる」対象として振る舞う
                if (activeDiscard) {
                  const isSource = c.uid === activeDiscard.cardUid
                  return (
                    <CardFrame
                      key={c.uid}
                      card={c}
                      ctx={{ growth: player.growth, momentum: player.momentum, energyMax: player.energyMaxAtTurnStart ?? player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether, exhausted: player.exhaustPile.length, selfHpLost: player.selfHpLost, permanents: player.permanents.length, damageTaken: player.damageTakenLastEnemyPhase, iceBlock: player.iceBlock, randomPlayed: player.randomPlayedThisCombat, energy: player.energy, handCards: Math.max(0, player.hand.length - 1) }}
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
                const play = (modeIndex?: number) => startPlay(c.uid, modeIndex)
                const tip = hoverUid === c.uid ? damageTipLines(s, c) : []
                return (
                  <div
                    key={c.uid}
                    style={{ position: 'relative', display: 'flex' }}
                    onMouseEnter={() => setHoverUid(c.uid)}
                    onMouseLeave={() => setHoverUid((prev) => (prev === c.uid ? null : prev))}
                  >
                  {tip.length > 0 && (
                    <div
                      style={{
                        position: 'absolute', bottom: '100%', left: 0, zIndex: 70, marginBottom: 4,
                        background: 'rgba(13,16,24,0.97)', border: '1px solid #556', borderRadius: 6,
                        padding: '6px 8px', fontSize: 11, whiteSpace: 'pre', pointerEvents: 'none', lineHeight: 1.5,
                      }}
                    >
                      {tip.join('\n')}
                    </div>
                  )}
                  <CardFrame
                    card={c}
                    ctx={{ growth: player.growth, momentum: player.momentum, energyMax: player.energyMaxAtTurnStart ?? player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether, exhausted: player.exhaustPile.length, selfHpLost: player.selfHpLost, permanents: player.permanents.length, damageTaken: player.damageTakenLastEnemyPhase, iceBlock: player.iceBlock, randomPlayed: player.randomPlayedThisCombat, energy: player.energy, handCards: Math.max(0, player.hand.length - 1) }}
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
                            <button
                              className="btn"
                              disabled={!canPlay}
                              {...(handIdx < 9 && !activeTarget ? { 'data-hotkey': `num-${handIdx + 1}` } : {})}
                              onClick={() => play()}
                            >
                              プレイ
                            </button>
                          )
                        )}
                        {isSetMode && settable && (
                          <button
                            className="btn"
                            disabled={!canSet}
                            title={c.def.type !== 'reaction' ? `1Eで伏せる。被攻撃${setWindowStage(c.def) === 'pre' ? '前' : '後'}に誘発し、発動時に${c.def.cost}Eを払う` : undefined}
                            {...(handIdx < 9 && !activeTarget && !isPlayableFromHand(c) ? { 'data-hotkey': `num-${handIdx + 1}` } : {})}
                            onClick={() => dispatch({ type: 'SetCard', cardUid: c.uid })}
                          >
                            {c.def.type !== 'reaction' ? '伏せる(1E)' : '伏せる'}
                          </button>
                        )}
                      </>
                    }
                  />
                  </div>
                )
              })}
            {s.phase === 'awaiting-reaction' && (
              <div className="pile-info" style={{ alignSelf: 'center' }}>
                敵の行動に割り込み中…（上のパネルで選択）
              </div>
            )}
          </div>
          {s.phase === 'player-turn' && (
            <button className="btn btn-primary btn-endturn" data-hotkey="end-turn" onClick={() => dispatch({ type: 'EndTurn' })}>
              ターン終了 ▶
            </button>
          )}
          <div className="choice-desc" style={{ fontSize: 10, marginTop: 4 }} title="入力欄にフォーカスがある間は無効">
            ⌨ 1〜9=プレイ/伏せ/対象/発動候補・E=ターン終了・F=発動・H=温存・Esc=取消
          </div>
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

      {/* 閲覧ビュー (2026-08-31): 山札は引き順を伏せて並び替え表示 (本家準拠) */}
      {pileView === 'draw' && (
        <CardListOverlay
          title="🂠 山札"
          cards={player.drawPile}
          ctx={pileCtx}
          sorted
          onClose={() => setPileView(null)}
        />
      )}
      {pileView === 'discard' && (
        <CardListOverlay
          title="🗑 捨て札"
          cards={player.discardPile}
          ctx={pileCtx}
          onClose={() => setPileView(null)}
        />
      )}
      {pileView === 'exhaust' && (
        <CardListOverlay
          title="⚰️ 消滅置き場"
          cards={player.exhaustPile}
          ctx={pileCtx}
          note="消滅した札はこの戦闘には戻らない（💀亡骸プレイ・屍集めを除く）"
          onClose={() => setPileView(null)}
        />
      )}
      {pileView === 'deck' && run != null && (
        <CardListOverlay
          title="🎴 デッキ全体（ランのマスターデッキ）"
          cards={run.deck}
          onClose={() => setPileView(null)}
        />
      )}
      {pileView === 'map' && run != null && <MapOverlay run={run} onClose={() => setPileView(null)} />}
    </div>
  )
}

// ---- ランのマップ (StS式の道) ----
// ノードも接続線も 1枚の SVG の同一座標系に描く。DOM実測・ResizeObserver・オーバーレイを使わないので
// 「線とノードがずれる」経路が構造的に存在しない。viewBox が外側でクリップするため、
// 日本語名がどれだけ長くても・フォントが何であっても横スクロールを作れない (豆腐事故の構造的対策)。
// ★以下の定数は見た目とタップ判定の安全余裕を決める。変更したら scripts/verify-map-ui.ts を必ず再実行する★

const MAP_VW = 380 // SVG のユーザー単位の横幅 (実ピクセルは CSS の width:100% が決める)
const MAP_PAD_L = 34 // 行ラベルの溝
const MAP_PAD_R = 8
const MAP_PAD_T = 18
const MAP_PAD_B = 48 // 最下行のラベル(最大2行)のディセンダぶん (実測の必要量 1047.5 < VH 1056)
const MAP_ROW_H = 66 // 行ピッチ。小さくすると線がラベルに食われ、タップ判定も重なる (実測 62.0 > 2*HIT_R=46)
const MAP_NODE_R = 14
const MAP_TRIM = MAP_NODE_R + 2 // 線を円の外側で止める量
const MAP_HIT_R = 23 // 透明の当たり判定 (実寸 46px = タップ target 44px 以上。実測済み)
const MAP_NAME_FONT = 10
// 幕別行数 (15/14/13行化 2026-09-02): 高さと座標は実マップの行数から導出する
const mapVh = (rows: number): number => MAP_PAD_T + (rows - 1) * MAP_ROW_H + MAP_PAD_B

/** 決定的ジッタ (本家StS風に格子を崩す)。Math.random は使わない = 再レンダで揺れない */
function mapJitter(r: number, c: number, salt: number): number {
  const h = Math.sin(r * 12.9898 + c * 78.233 + salt * 37.719) * 43758.5453
  return h - Math.floor(h) // 0..1
}
const MAP_INNER_W = MAP_VW - MAP_PAD_L - MAP_PAD_R
/**
 * X座標は格子列 (MapNode.col 0〜6) で決める (2026-08-31 本家式パスウォーク化)。
 * 行内で等間隔に均すと蛇行が消えて全行が同じ扇に見える — 本家の「道の形」は格子座標が作る。
 * col を持たない旧セーブのマップは行内等間隔へフォールバック
 */
const mapNodeX = (r: number, c: number, row: readonly MapNode[]): number => {
  const col = row[c].col
  const t = col !== undefined ? (col + 0.5) / GRID_COLS : (c + 0.5) / row.length
  return MAP_PAD_L + t * MAP_INNER_W + (mapJitter(r, c, 1) - 0.5) * 10
}
/** ボス行を上に、行0を下に (rows = その幕のマップの行数) */
const mapNodeY = (rows: number, r: number, c: number): number =>
  MAP_PAD_T + (rows - 1 - r) * MAP_ROW_H + (mapJitter(r, c, 2) - 0.5) * 6
/** 各ノードのラベル幅: 隣ノードとの実距離から決める (格子座標では隣接列がかなり近い) */
const mapLabelWidths = (r: number, row: readonly MapNode[]): number[] => {
  const xs = row.map((_, c) => mapNodeX(r, c, row))
  return xs.map((x, i) => {
    const left = i > 0 ? x - xs[i - 1] : 2 * (x - MAP_PAD_L + 10)
    const right = i < xs.length - 1 ? xs[i + 1] - x : 2 * (MAP_VW - x)
    return Math.max(24, Math.min(left, right) - 8)
  })
}

const MAP_ICON: Record<MapNodeType, string> = {
  battle: '⚔️',
  elite: '👑',
  campfire: '🔥',
  workshop: '🔨',
  shop: '🛒',
  event: '❓',
  treasure: '🎁',
  boss: '💀',
}
const MAP_TYPE_LABEL: Record<MapNodeType, string> = {
  battle: '戦闘',
  elite: '強個体',
  campfire: '焚き火',
  workshop: '工房',
  shop: 'ショップ',
  event: '？？？',
  treasure: '宝箱',
  boss: '幕ボス',
}
const mapNodeName = (n: MapNode): string =>
  n.encounterId !== null ? encounterName(n.encounterId) : MAP_TYPE_LABEL[n.type]

/** テキスト幅の上限見積り (フォント計測に依存しない)。全角=1em / 絵文字=1.3em / それ以外=0.62em で
 *  必ず実幅以上に見積もるので、この値で textLength を掛ければはみ出しは起きない */
function mapTextWidth(s: string, size: number): number {
  let w = 0
  for (const ch of s) {
    const cp = ch.codePointAt(0) ?? 0
    w += cp > 0xffff ? size * 1.3 : cp > 0x2e7f ? size : size * 0.62
  }
  return w
}
/** 幅を超える時だけ字詰めする (切り捨てないので情報は落ちない) */
function mapFit(s: string, maxW: number, size: number): {
  textLength?: number
  lengthAdjust?: 'spacingAndGlyphs'
} {
  return mapTextWidth(s, size) > maxW
    ? { textLength: maxW, lengthAdjust: 'spacingAndGlyphs' }
    : {}
}

/** 現在地から前向きBFSで到達可能なノード集合 ("row:col")。旧実装のインラインBFSをそのまま移設 */
function mapReachable(run: RunState): ReadonlySet<string> {
  const reach = new Set<string>()
  let frontier: number[] =
    run.row < 0 ? run.map[0].map((_, c) => c) : [...(currentNode(run)?.next ?? [])]
  let r = run.row < 0 ? 0 : run.row + 1
  while (r < run.map.length && frontier.length > 0) {
    const nextF = new Set<number>()
    for (const c of frontier) {
      reach.add(`${r}:${c}`)
      for (const to of run.map[r][c].next) nextF.add(to)
    }
    frontier = [...nextF]
    r++
  }
  return reach
}

/**
 * 通ってきた経路の記憶 (UI専用の表示用メモ)。engine の RunState は (row,col) しか持たず経路を
 * 復元できないため、マップ画面を描くたびに現在地を書き留める。キーは seed:act なので幕が変われば
 * 自然にリセットされ、同一シードで再走しても row<=run.row の範囲は必ず上書きされる。
 * engine には一切触らない (純ロジック維持)。
 */
const mapTakenPath = new Map<string, Map<number, number>>()
function mapRecordVisit(run: RunState): ReadonlyMap<number, number> {
  const key = `${run.seed}:${run.act}`
  let m = mapTakenPath.get(key)
  if (m === undefined) {
    m = new Map()
    mapTakenPath.set(key, m)
  }
  if (run.row >= 0) m.set(run.row, run.col) // 冪等 (StrictMode の二重実行でも同じ値)
  return m
}

/** マップ本体。engine 無変更・純関数レイアウト (RunMap → 座標) */
function RunMapView({
  run,
  onChoose,
  interactive = true,
}: {
  run: RunState
  onChoose: (col: number) => void
  /** false = 閲覧のみ (戦闘中のマップ確認。ノードは押せない) */
  interactive?: boolean
}) {
  const cands = interactive ? nextChoices(run) : []
  const reach = mapReachable(run)
  const path = mapRecordVisit(run)
  // 全長 ~1050 units (実寸 ≈1050px) あるので、開いた時に現在地が画面中央に来るようにする。
  // run.row < 0 (幕の開始直後。1ランで3回通る) は現在地が無いので最下行=行0へ寄せる
  const focusRow = run.row < 0 ? 0 : run.row
  const focusCol = run.row < 0 ? 0 : run.col
  const focusRef = useRef<SVGGElement | null>(null)
  useEffect(() => {
    focusRef.current?.scrollIntoView({ block: 'center' })
  }, [run.act, run.row, run.col])

  // --- 接続線 (データのエッジ1本につき <line> 1本。装飾線を足すと verify-map-ui.ts が落ちる) ---
  const rows = run.map.length
  const edges: ReactElement[] = []
  for (let r = 0; r < rows - 1; r++) {
    const row = run.map[r]
    const rowNext = run.map[r + 1]
    for (let c = 0; c < row.length; c++) {
      const ax = mapNodeX(r, c, row)
      const ay = mapNodeY(rows, r, c)
      for (const to of row[c].next) {
        const bx = mapNodeX(r + 1, to, rowNext)
        const by = mapNodeY(rows, r + 1, to)
        const len = Math.hypot(bx - ax, by - ay) || 1
        const ux = ((bx - ax) / len) * MAP_TRIM
        const uy = ((by - ay) / len) * MAP_TRIM
        const cls =
          path.get(r) === c && path.get(r + 1) === to
            ? 'map-edge-taken' // 通ってきた道 (金)
            : r === run.row && c === run.col
              ? 'map-edge-open' // 現在地から進める道 (緑の実線)
              : r < run.row
                ? 'map-edge-past'
                : r === run.row || reach.has(`${r}:${c}`)
                  ? 'map-edge-live'
                  : 'map-edge-dead'
        edges.push(
          <line
            key={`e${r}:${c}:${to}`}
            className={`map-edge ${cls}`}
            x1={ax + ux}
            y1={ay + uy}
            x2={bx - ux}
            y2={by - uy}
          />,
        )
      }
    }
  }

  return (
    <svg
      className="map-svg"
      viewBox={`0 0 ${MAP_VW} ${mapVh(rows)}`}
      role="group"
      aria-label={`ランのマップ 第${run.act}幕`}
    >
      {edges}
      {run.map.map((row, r) => {
        const labelWs = mapLabelWidths(r, row)
        return (
          <g key={`row${r}`}>
            <text
              className={r === run.row ? 'map-rowlabel map-rowlabel-here' : 'map-rowlabel'}
              x={MAP_PAD_L - 10}
              y={MAP_PAD_T + (rows - 1 - r) * MAP_ROW_H + 3}
              textAnchor="end"
            >
              {r === rows - 1 ? 'ボス' : `行${r + 1}`}
            </text>
            {row.map((n, c) => {
              const here = r === run.row && c === run.col
              const onPath = !here && path.get(r) === c && r < run.row
              const passed = r <= run.row && !here && !onPath
              const clickable = r === run.row + 1 && cands.includes(c)
              const unreachable = !here && r > run.row && !reach.has(`${r}:${c}`)
              const name = mapNodeName(n)
              // 2行目のバッジ: 文字でも読めるようにする (👑/金枠だけだと初見・タッチ端末で伝わらない)
              const badge = here
                ? '現在地'
                : unreachable
                  ? '到達不可'
                  : n.type === 'elite'
                    ? '強個体'
                    : n.type === 'boss'
                      ? '幕ボス'
                      : null
              const tip =
                n.type === 'elite'
                  ? `強個体: ${name}（固有ギミックの専用敵・素の値／勝てばレリック3択+レア1枚確定）`
                  : n.type === 'boss'
                    ? `幕ボス: ${name}（撃破で全回復＋レリック3択）`
                    : n.type === 'campfire'
                      ? '焚き火: 休む(25%回復)/鍛える/取り除く の択一'
                      : n.type === 'workshop'
                        ? '工房: デッキの2枚を合成して1枚にする'
                        : n.type === 'shop'
                          ? 'ショップ: カード/レリック/除去/鍛える'
                          : n.type === 'event'
                            ? '？: 入るまで中身は分からない（イベント85%／戦闘10%／ショップ3%／宝箱2%。外れた種別は次から確率が上がる）'
                            : n.type === 'treasure'
                              ? '宝箱: レリック3択（本家の宝箱行。カード報酬は無い）'
                              : `戦闘: ${name}`
              const nextTip =
                n.next.length > 0 ? `／次の接続先: ${n.next.map((x) => x + 1).join('・')}` : ''
              const cls = [
                'map-node',
                `map-node-${n.type}`,
                here ? 'map-node-here' : '',
                onPath ? 'map-node-taken' : '',
                clickable ? 'map-node-open' : '',
                passed || unreachable ? 'map-node-dim' : '',
              ]
                .filter(Boolean)
                .join(' ')
              return (
                <g
                  key={`n${r}:${c}`}
                  ref={r === focusRow && c === focusCol ? focusRef : undefined}
                  className={cls}
                  transform={`translate(${mapNodeX(r, c, row).toFixed(2)} ${mapNodeY(rows, r, c).toFixed(2)})`}
                  role="button"
                  aria-disabled={!clickable}
                  tabIndex={clickable ? 0 : -1}
                  aria-label={`${MAP_TYPE_LABEL[n.type]} ${name}${badge !== null ? ` (${badge})` : ''}${clickable ? ' へ進む' : ''}`}
                  onClick={clickable ? () => onChoose(c) : undefined}
                  onKeyDown={
                    clickable
                      ? (ev) => {
                          if (ev.key === 'Enter' || ev.key === ' ') {
                            ev.preventDefault()
                            onChoose(c)
                          }
                        }
                      : undefined
                  }
                >
                  <title>{`${tip}${nextTip}`}</title>
                  {/* fill:none だと当たり判定が消える。CSS で transparent を指定している */}
                  <circle className="map-hit" r={MAP_HIT_R} />
                  {here ? <circle className="map-ring" r={MAP_NODE_R + 4} /> : null}
                  <circle className="map-disc" r={MAP_NODE_R} />
                  <text className="map-icon" y={5} textAnchor="middle">
                    {MAP_ICON[n.type]}
                  </text>
                  <text
                    className="map-label"
                    y={MAP_NODE_R + 11}
                    textAnchor="middle"
                    {...mapFit(name, labelWs[c], MAP_NAME_FONT)}
                  >
                    {name}
                  </text>
                  {badge !== null ? (
                    <text className="map-badge" y={MAP_NODE_R + 20} textAnchor="middle">
                      {badge}
                    </text>
                  ) : null}
                </g>
              )
            })}
          </g>
        )
      })}
      {run.row < 0 ? (
        <text
          className="map-start"
          x={MAP_VW / 2}
          y={MAP_PAD_T + (rows - 1) * MAP_ROW_H + 44}
          textAnchor="middle"
        >
          ▲ ここから登る
        </text>
      ) : null}
    </svg>
  )
}

// ---- 閲覧ビュー (2026-08-31 ユーザー要望「対戦中やマップ時にデッキ・墓地・マップを確認したい」) ----

/** カード一覧のオーバーレイ。本家準拠で「鍛えた姿 (+)」にも切り替えられる */
function CardListOverlay({
  title,
  cards,
  ctx,
  sorted,
  note,
  onClose,
}: {
  title: string
  cards: readonly CardInstance[]
  ctx?: EffectCtx
  /** 山札は引き順を伏せるため並び替えて表示する (本家準拠) */
  sorted?: boolean
  note?: string
  onClose: () => void
}) {
  const [showUpgraded, setShowUpgraded] = useState(false)
  const list = sorted
    ? [...cards].sort(
        (a, b) => a.def.cost - b.def.cost || a.def.name.localeCompare(b.def.name, 'ja'),
      )
    : cards
  return (
    <div className="viewer-overlay" onClick={onClose}>
      <div className="viewer-panel" onClick={(e) => e.stopPropagation()}>
        <div className="viewer-head">
          <span className="viewer-title">
            {title}（{cards.length}枚）
          </span>
          <label className="viewer-toggle">
            <input
              type="checkbox"
              checked={showUpgraded}
              onChange={(e) => setShowUpgraded(e.target.checked)}
            />{' '}
            鍛えた姿（+）で表示
          </label>
          <button className="btn" onClick={onClose}>
            ✕ 閉じる
          </button>
        </div>
        {sorted === true && (
          <p className="hint">※山札は引き順を伏せるため並び替えて表示（本家準拠）</p>
        )}
        {note !== undefined && <p className="hint">{note}</p>}
        <div className="hand-cards viewer-cards">
          {list.length === 0 && <p className="hint">（空）</p>}
          {list.map((c) => {
            const upgradable = canUpgradeCard(c)
            const shown = showUpgraded && upgradable ? upgradeCard(c) : c
            return (
              <CardFrame
                key={c.uid}
                card={shown}
                dim={false}
                ctx={ctx}
                actions={
                  showUpgraded && !upgradable ? (
                    <span className="hint">{isUpgraded(c) ? '鍛え済み' : '鍛えられない'}</span>
                  ) : null
                }
              />
            )
          })}
        </div>
      </div>
    </div>
  )
}

/** マップの閲覧オーバーレイ (戦闘中などマップフェーズ外から現在地と道を確認する) */
function MapOverlay({ run, onClose }: { run: RunState; onClose: () => void }) {
  return (
    <div className="viewer-overlay" onClick={onClose}>
      <div className="viewer-panel" onClick={(e) => e.stopPropagation()}>
        <div className="viewer-head">
          <span className="viewer-title">🗺 マップ — 第{run.act}幕/3（閲覧のみ）</span>
          <button className="btn" onClick={onClose}>
            ✕ 閉じる
          </button>
        </div>
        <div className="map-wrap">
          <RunMapView run={run} onChoose={() => {}} interactive={false} />
        </div>
      </div>
    </div>
  )
}

// ---- 敵・レリックの調整 (2026-09-01 ユーザー要望「敵やレリックもカード同様に」) ----

/** 実データ語彙 (現行の敵から抽出)。行動kind・アーキタイプ・状態異常 */
const ENEMY_VOCAB = (() => {
  const kinds = new Set<string>()
  const archetypes = new Set<string>()
  for (const e of allEnemies) {
    archetypes.add(e.archetype)
    for (const t of [e.moves, e.movesVsSet ?? [], e.movesVsTokens ?? [], e.movesBelowHalf ?? []]) {
      for (const m of t) kinds.add(m.kind)
    }
  }
  return { kinds: [...kinds].sort(), archetypes: [...archetypes].sort(), statuses: Object.keys(STATUS_LABEL) }
})()

const MOVE_FIELD_JA: Record<string, string> = { min: '最小', max: '最大', weight: '重み', hits: 'ヒット数', alsoDefend: '攻防一体🛡', alsoBuff: '同時筋力💪' }
const MOVE_KIND_ICON: Record<string, string> = { attack: '⚔️攻撃', defend: '🛡防御', buff: '💪筋力上げ', rally: '📣応援', hex: '🧿呪い', 'destroy-set': '💥伏せ破壊', 'destroy-token': '🪓従者狩り', heal: '💚回復', 'steal-gold': '💰盗み', flee: '🏃逃走', rest: '😮‍💨隙', mill: '📖山札喰い', hatch: '🐣孵化' }

function moveLine(mv: EnemyMove): string {
  const range = mv.min !== undefined ? `${mv.min}〜${mv.max}` : ''
  const inflict = mv.inflict ? ` ＋${STATUS_LABEL[mv.inflict.status] ?? mv.inflict.status}${mv.inflict.amount}` : ''
  return `${mv.id}: ${MOVE_KIND_ICON[mv.kind] ?? mv.kind}${range}${mv.hits !== undefined && mv.hits > 1 ? `×${mv.hits}` : ''}${mv.mirrorHits === true ? '×手数' : ''}${mv.alsoDefend !== undefined ? `+🛡${mv.alsoDefend}` : ''}${mv.alsoBuff !== undefined ? `+💪${mv.alsoBuff}` : ''}${inflict}${mv.setAlt !== undefined ? '【伏せ時分岐】' : ''}`
}

/** 敵の数値フィールド (実データのパス+現行値)。存在するものだけ編集対象 */
function enemyTunerFields(def: EnemyDef): { key: string; label: string; cur: number }[] {
  const out: { key: string; label: string; cur: number }[] = []
  for (const [k, ja] of ENEMY_TOP_FIELDS) {
    const v = (def as unknown as Record<string, unknown>)[k]
    if (typeof v === 'number') out.push({ key: k, label: ja, cur: v })
  }
  const tables: readonly (readonly [string, string, readonly EnemyMove[] | undefined])[] = [
    ['m', '', def.moves],
    ['vs', '伏せ反応', def.movesVsSet],
    ['tk', '従者反応', def.movesVsTokens],
    ['bh', '半分以下', def.movesBelowHalf],
  ]
  for (const [pfx, ja, tbl] of tables) {
    tbl?.forEach((mv, i) => {
      const base = `${ja}「${mv.id}」`
      for (const f of ['min', 'max', 'weight', 'hits', 'alsoDefend', 'alsoBuff'] as const) {
        const v = mv[f]
        if (typeof v === 'number') out.push({ key: `${pfx}${i}.${f}`, label: `${base}${MOVE_FIELD_JA[f]}`, cur: v })
      }
      if (mv.inflict) out.push({ key: `${pfx}${i}.inflict.amount`, label: `${base}${STATUS_LABEL[mv.inflict.status] ?? mv.inflict.status}量`, cur: mv.inflict.amount })
      const sa = mv.setAlt
      if (sa !== undefined) {
        for (const f of ['min', 'max', 'hits'] as const) {
          const v = sa[f]
          if (typeof v === 'number') out.push({ key: `${pfx}${i}.alt.${f}`, label: `${base}伏せ時${MOVE_FIELD_JA[f]}`, cur: v })
        }
        if (sa.inflict) out.push({ key: `${pfx}${i}.alt.inflict.amount`, label: `${base}伏せ時${STATUS_LABEL[sa.inflict.status] ?? sa.inflict.status}量`, cur: sa.inflict.amount })
      }
    })
  }
  return out
}

const RELIC_BONUS_JA: Record<string, string> = { maxHp: '最大HP+(現在HPも同量増える)', victoryHeal: '勝利時回復', rewardChoices: 'ピック候補+', campfireRatio: '焚き火回復率', goldPerVictory: '勝利ゴールド+', campfireForge: '鍛える追加回数' }

function relicTunerFields(def: RelicDef): { key: string; label: string; cur: number }[] {
  const out: { key: string; label: string; cur: number }[] = []
  def.effects?.forEach((e, i) => {
    if (typeof e.amount === 'number') out.push({ key: `e${i}.amount`, label: `効果〔${triggerJa(e.trigger)}: ${effectJa(e.effect)}〕の量`, cur: e.amount })
  })
  for (const [k, ja] of Object.entries(RELIC_BONUS_JA)) {
    const v = (def.bonus as unknown as Record<string, unknown> | undefined)?.[k]
    if (typeof v === 'number') out.push({ key: `bonus.${k}`, label: ja, cur: v })
  }
  if (typeof def.combatRule?.setDamageReduction === 'number') {
    out.push({ key: 'rule.setDamageReduction', label: '伏せ中の敵攻撃-N', cur: def.combatRule.setDamageReduction })
  }
  return out
}

/** 汎用の数値マーク編集 (敵・レリック共通)。現行値と違う値だけ提案として残る */
function SimpleMarkEditor({ fields, mark, onChange }: { fields: readonly { key: string; label: string; cur: number }[]; mark: SimpleMark; onChange: (m: SimpleMark) => void }) {
  const S = { fontSize: 11, opacity: 0.9 } as const
  const setField = (key: string, cur: number, raw: string) => {
    const next: Record<string, number> = { ...(mark.fields ?? {}) }
    const v = Number(raw)
    if (raw === '' || !Number.isFinite(v) || v === cur) delete next[key]
    else next[key] = v
    onChange({ ...mark, fields: next })
  }
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 2, marginTop: 4 }}>
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
        {fields.map((f) => (
          <label key={f.key} style={{ ...S, display: 'inline-flex', gap: 3, alignItems: 'center' }} title={f.key}>
            {f.label}
            <input type="number" step="any" value={mark.fields?.[f.key] ?? f.cur} onChange={(e) => setField(f.key, f.cur, e.target.value)} style={{ width: 52, fontSize: 11, background: mark.fields?.[f.key] !== undefined ? 'rgba(120,160,255,0.25)' : undefined }} />
          </label>
        ))}
      </div>
      <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
        <input value={mark.change ?? ''} onChange={(e) => onChange({ ...mark, change: e.target.value })} placeholder="補足 (自由記述)" style={{ flex: 1, fontSize: 11 }} />
        <label style={{ ...S, color: mark.remove === true ? '#f88' : undefined }}>
          <input type="checkbox" checked={mark.remove === true} onChange={(e) => onChange({ ...mark, remove: e.target.checked || undefined })} /> 削除案
        </label>
      </div>
    </div>
  )
}

function EnemyMoveDraftRow({ value, onChange, onDelete }: { value: EnemyMoveDraft; onChange: (m: EnemyMoveDraft) => void; onDelete: () => void }) {
  const S = { fontSize: 11 } as const
  return (
    <div style={{ display: 'flex', gap: 4, alignItems: 'center', flexWrap: 'wrap', border: '1px solid #334', borderRadius: 4, padding: 3 }}>
      <input placeholder="行動id" value={value.id} onChange={(e) => onChange({ ...value, id: e.target.value })} style={{ width: 80, fontSize: 11 }} />
      <select style={S} value={value.kind} title={value.kind} onChange={(e) => onChange({ ...value, kind: e.target.value })}>
        {ENEMY_VOCAB.kinds.map((k) => (
          <option key={k} value={k}>{MOVE_KIND_ICON[k] ?? k}</option>
        ))}
      </select>
      <label style={S}>最小 <input type="number" style={{ width: 44 }} value={value.min ?? ''} onChange={(e) => onChange({ ...value, min: numOrUndef(e.target.value) })} /></label>
      <label style={S}>最大 <input type="number" style={{ width: 44 }} value={value.max ?? ''} onChange={(e) => onChange({ ...value, max: numOrUndef(e.target.value) })} /></label>
      <label style={S}>×hits <input type="number" style={{ width: 38 }} value={value.hits ?? ''} onChange={(e) => onChange({ ...value, hits: numOrUndef(e.target.value) })} /></label>
      <label style={S}>重み <input type="number" style={{ width: 38 }} value={value.weight ?? ''} onChange={(e) => onChange({ ...value, weight: numOrUndef(e.target.value) })} /></label>
      <label style={S}>付与{' '}
        <select value={value.inflictStatus ?? ''} onChange={(e) => onChange({ ...value, inflictStatus: e.target.value === '' ? undefined : e.target.value })}>
          <option value="">なし</option>
          {ENEMY_VOCAB.statuses.map((k) => (
            <option key={k} value={k}>{STATUS_LABEL[k]}</option>
          ))}
        </select>
      </label>
      {(value.inflictStatus ?? '') !== '' && (
        <input type="number" style={{ width: 38 }} value={value.inflictAmount ?? 1} onChange={(e) => onChange({ ...value, inflictAmount: numOrUndef(e.target.value) })} />
      )}
      <label style={S}>+🛡 <input type="number" style={{ width: 38 }} value={value.alsoDefend ?? ''} onChange={(e) => onChange({ ...value, alsoDefend: numOrUndef(e.target.value) })} /></label>
      <label style={S}>+💪 <input type="number" style={{ width: 38 }} value={value.alsoBuff ?? ''} onChange={(e) => onChange({ ...value, alsoBuff: numOrUndef(e.target.value) })} /></label>
      <button className="chip chip-btn" onClick={onDelete}>✕</button>
    </div>
  )
}

function EnemyDraftEditor({ value, onChange, onDelete }: { value: EnemyDraft; onChange: (d: EnemyDraft) => void; onDelete: () => void }) {
  const S = { fontSize: 11 } as const
  return (
    <div style={{ border: '1px solid #556', borderRadius: 6, padding: 6, marginTop: 6, display: 'flex', flexDirection: 'column', gap: 4 }}>
      <div style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
        <label style={S}>名前 <input value={value.name} onChange={(e) => onChange({ ...value, name: e.target.value })} style={{ width: 110 }} /></label>
        <label style={S}>id <input value={value.id ?? ''} placeholder="空=実装時に命名" onChange={(e) => onChange({ ...value, id: e.target.value === '' ? undefined : e.target.value })} style={{ width: 120 }} /></label>
        <label style={S}>絵文字 <input value={value.sprite ?? ''} onChange={(e) => onChange({ ...value, sprite: e.target.value === '' ? undefined : e.target.value })} style={{ width: 40 }} /></label>
        <label style={S}>HP <input type="number" style={{ width: 52 }} value={value.maxHp} onChange={(e) => onChange({ ...value, maxHp: Number(e.target.value) || 0 })} /></label>
        <label style={S}>型{' '}
          <select value={value.archetype} title={value.archetype} onChange={(e) => onChange({ ...value, archetype: e.target.value })}>
            {ENEMY_VOCAB.archetypes.map((a) => (
              <option key={a} value={a}>{(ARCHETYPE_LABEL as Record<string, string>)[a] ?? a}</option>
            ))}
          </select>
        </label>
      </div>
      <div style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
        {ENEMY_TOP_FIELDS.filter(([k]) => k !== 'maxHp').map(([k, ja]) => (
          <label key={k} style={S}>{ja} <input type="number" style={{ width: 44 }} value={(value as unknown as Record<string, number | undefined>)[k] ?? ''} onChange={(e) => onChange({ ...value, [k]: numOrUndef(e.target.value) } as EnemyDraft)} /></label>
        ))}
      </div>
      {value.moves.map((mv, i) => (
        <EnemyMoveDraftRow
          key={i}
          value={mv}
          onChange={(nm) => onChange({ ...value, moves: value.moves.map((x, j) => (j === i ? nm : x)) })}
          onDelete={() => onChange({ ...value, moves: value.moves.filter((_, j) => j !== i) })}
        />
      ))}
      <div style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
        <button className="chip chip-btn" onClick={() => onChange({ ...value, moves: [...value.moves, { id: `move${value.moves.length + 1}`, kind: 'attack', min: 6, max: 9, weight: 1 }] })}>＋ 行動を追加</button>
        <label style={{ ...S, flex: 1 }}>ローテーション(idカンマ区切り・空=重み抽選) <input value={value.sequence ?? ''} onChange={(e) => onChange({ ...value, sequence: e.target.value === '' ? undefined : e.target.value })} style={{ width: '55%', fontSize: 11 }} /></label>
        <button className="chip chip-btn" onClick={onDelete}>🗑 この下書きを削除</button>
      </div>
      <div className="choice-desc" style={{ fontSize: 10 }}>高度な仕掛け (setAlt=伏せ時分岐・伏せ/従者反応テーブル・フェーズ変化) は補足/メモに書けば実装時に起こします</div>
    </div>
  )
}

function RelicDraftEditor({ value, onChange, onDelete }: { value: RelicDraft; onChange: (d: RelicDraft) => void; onDelete: () => void }) {
  const S = { fontSize: 11 } as const
  return (
    <div style={{ border: '1px solid #556', borderRadius: 6, padding: 6, marginTop: 6, display: 'flex', flexDirection: 'column', gap: 4 }}>
      <div style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
        <label style={S}>名前 <input value={value.name} onChange={(e) => onChange({ ...value, name: e.target.value })} style={{ width: 110 }} /></label>
        <label style={S}>id <input value={value.id ?? ''} placeholder="空=実装時に命名" onChange={(e) => onChange({ ...value, id: e.target.value === '' ? undefined : e.target.value })} style={{ width: 120 }} /></label>
        <label style={S}>絵文字 <input value={value.sprite ?? ''} onChange={(e) => onChange({ ...value, sprite: e.target.value === '' ? undefined : e.target.value })} style={{ width: 40 }} /></label>
        <label style={{ ...S, flex: 1 }}>説明 <input value={value.description} onChange={(e) => onChange({ ...value, description: e.target.value })} style={{ width: '70%', fontSize: 11 }} /></label>
      </div>
      <div className="choice-desc" style={{ fontSize: 10 }}>A型=戦闘内フック効果 (下の効果行。onCombatStart=戦闘開始時 / onTurnStart=毎ターン開始時 など)</div>
      {value.effects.map((ef, i) => (
        <EffectDraftRow
          key={i}
          value={ef}
          onChange={(ne) => onChange({ ...value, effects: value.effects.map((x, j) => (j === i ? ne : x)) })}
          onDelete={() => onChange({ ...value, effects: value.effects.filter((_, j) => j !== i) })}
        />
      ))}
      <div>
        <button className="chip chip-btn" onClick={() => onChange({ ...value, effects: [...value.effects, { trigger: 'onCombatStart', effect: 'gainBlock', amount: 5 }] })}>＋ 効果を追加</button>
      </div>
      <div style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
        <span style={S}>B型(ラン定数):</span>
        {Object.entries(RELIC_BONUS_JA).map(([k, ja]) => (
          <label key={k} style={S}>{ja} <input type="number" step="any" style={{ width: 44 }} value={(value as unknown as Record<string, number | undefined>)[k] ?? ''} onChange={(e) => onChange({ ...value, [k]: numOrUndef(e.target.value) } as RelicDraft)} /></label>
        ))}
        <label style={S}>伏せ中攻撃-N <input type="number" style={{ width: 38 }} value={value.setDamageReduction ?? ''} onChange={(e) => onChange({ ...value, setDamageReduction: numOrUndef(e.target.value) })} /></label>
        <label style={S}><input type="checkbox" checked={value.revealIntents === true} onChange={(e) => onChange({ ...value, revealIntents: e.target.checked || undefined })} /> 実値公開</label>
        <button className="chip chip-btn" onClick={onDelete}>🗑 この下書きを削除</button>
      </div>
    </div>
  )
}

/** リーダーの数値フィールド (存在するものだけ編集対象) */
function leaderTunerFields(def: LeaderDef): { key: string; label: string; cur: number }[] {
  const out: { key: string; label: string; cur: number }[] = []
  for (const [k, ja] of LEADER_TOP_FIELDS) {
    const v = (def as unknown as Record<string, unknown>)[k]
    if (typeof v === 'number') out.push({ key: k, label: ja, cur: v })
  }
  def.passive.forEach((e, i) => {
    if (typeof e.amount === 'number') out.push({ key: `p${i}.amount`, label: `パッシブ〔${triggerJa(e.trigger)}: ${effectJa(e.effect)}〕の量`, cur: e.amount })
  })
  return out
}

function LeaderDraftEditor({ value, onChange, onDelete }: { value: LeaderDraft; onChange: (d: LeaderDraft) => void; onDelete: () => void }) {
  const S = { fontSize: 11 } as const
  const COLOR_JA: Record<string, string> = { green: '緑', blue: '青', red: '赤', white: '白', black: '黒' }
  const toggleColor = (c: string, on: boolean) => {
    const set = new Set(value.colors)
    if (on) set.add(c)
    else set.delete(c)
    onChange({ ...value, colors: [...set] })
  }
  return (
    <div style={{ border: '1px solid #556', borderRadius: 6, padding: 6, marginTop: 6, display: 'flex', flexDirection: 'column', gap: 4 }}>
      <div style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
        <label style={S}>名前 <input value={value.name} onChange={(e) => onChange({ ...value, name: e.target.value })} style={{ width: 100 }} /></label>
        <label style={S}>id <input value={value.id ?? ''} placeholder="空=実装時に命名" onChange={(e) => onChange({ ...value, id: e.target.value === '' ? undefined : e.target.value })} style={{ width: 120 }} /></label>
        <label style={S}>絵文字 <input value={value.sprite ?? ''} onChange={(e) => onChange({ ...value, sprite: e.target.value === '' ? undefined : e.target.value })} style={{ width: 40 }} /></label>
        <span style={S}>色:</span>
        {(['green', 'blue', 'red', 'white', 'black'] as const).map((c) => (
          <label key={c} style={S}>
            <input type="checkbox" checked={value.colors.includes(c)} onChange={(e) => toggleColor(c, e.target.checked)} /> {COLOR_JA[c]}
          </label>
        ))}
      </div>
      <div style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
        <label style={S}>HP <input type="number" style={{ width: 48 }} value={value.maxHp} onChange={(e) => onChange({ ...value, maxHp: Number(e.target.value) || 0 })} /></label>
        <label style={S}>ドロー <input type="number" style={{ width: 40 }} value={value.drawPerTurn} onChange={(e) => onChange({ ...value, drawPerTurn: Number(e.target.value) || 0 })} /></label>
        <label style={S}>エナジー <input type="number" style={{ width: 40 }} value={value.energyMax} onChange={(e) => onChange({ ...value, energyMax: Number(e.target.value) || 0 })} /></label>
        <label style={S}>ピック候補 <input type="number" style={{ width: 40 }} value={value.rewardChoices} onChange={(e) => onChange({ ...value, rewardChoices: Number(e.target.value) || 0 })} /></label>
        <label style={S}>伏せ枠 <input type="number" style={{ width: 38 }} value={value.setSlots ?? ''} placeholder="1" onChange={(e) => onChange({ ...value, setSlots: numOrUndef(e.target.value) })} /></label>
        <label style={S}>初期デッキid <input value={value.runDeckId ?? ''} placeholder="run_basic" onChange={(e) => onChange({ ...value, runDeckId: e.target.value === '' ? undefined : e.target.value })} style={{ width: 110 }} /></label>
      </div>
      <label style={{ ...S, display: 'flex', gap: 4 }}>説明 <input value={value.description} onChange={(e) => onChange({ ...value, description: e.target.value })} style={{ flex: 1, fontSize: 11 }} /></label>
      <div className="choice-desc" style={{ fontSize: 10 }}>パッシブ (戦闘開始時から場にあるリーダー置物。onTurnStart=毎ターン / onAttackPlayed=攻撃プレイごと 等)</div>
      {value.passive.map((ef, i) => (
        <EffectDraftRow
          key={i}
          value={ef}
          onChange={(ne) => onChange({ ...value, passive: value.passive.map((x, j) => (j === i ? ne : x)) })}
          onDelete={() => onChange({ ...value, passive: value.passive.filter((_, j) => j !== i) })}
        />
      ))}
      <div>
        <button className="chip chip-btn" onClick={() => onChange({ ...value, passive: [...value.passive, { trigger: 'onTurnStart', effect: 'addGrowth', amount: 1 }] })}>＋ パッシブ効果を追加</button>{' '}
        <button className="chip chip-btn" onClick={onDelete}>🗑 この下書きを削除</button>
      </div>
      <div className="choice-desc" style={{ fontSize: 10 }}>HPは5刻み・アーキタイプの物語で正当化できる差のみ (基準75。確定済みルール表「リーダーの個性」)</div>
    </div>
  )
}

/** カード調整案の下書き置き場 (localStorage)。リロードしても作業が消えない */
const TUNER_STORAGE_KEY = 'deckRogueCardTuner'
interface TunerDraft {
  readonly marks: Record<string, CardProposalMark>
  readonly newCards: string
  /** 新カード案 (構造化。2026-09-01「カード1枚が実データとして作れるレベル」) */
  readonly newCardDefs: readonly CardDraft[]
  /** 敵・レリックのマークと新規案 (2026-09-01「敵やレリックもカード同様に」) */
  readonly enemyMarks: Record<string, SimpleMark>
  readonly newEnemyDefs: readonly EnemyDraft[]
  readonly relicMarks: Record<string, SimpleMark>
  readonly newRelicDefs: readonly RelicDraft[]
  readonly leaderMarks: Record<string, SimpleMark>
  readonly newLeaderDefs: readonly LeaderDraft[]
}
function loadTunerDraft(): TunerDraft {
  try {
    const raw = localStorage.getItem(TUNER_STORAGE_KEY)
    if (raw !== null) {
      const d = JSON.parse(raw) as Partial<TunerDraft>
      return {
        marks: d.marks ?? {}, newCards: d.newCards ?? '', newCardDefs: d.newCardDefs ?? [],
        enemyMarks: d.enemyMarks ?? {}, newEnemyDefs: d.newEnemyDefs ?? [],
        relicMarks: d.relicMarks ?? {}, newRelicDefs: d.newRelicDefs ?? [],
        leaderMarks: d.leaderMarks ?? {}, newLeaderDefs: d.newLeaderDefs ?? [],
      }
    }
  } catch {
    /* 壊れた下書きは捨てる */
  }
  return { marks: {}, newCards: '', newCardDefs: [], enemyMarks: {}, newEnemyDefs: [], relicMarks: {}, newRelicDefs: [], leaderMarks: {}, newLeaderDefs: [] }
}

/**
 * 実データ語彙 (現行カードから抽出)。カードビルダーのドロップダウンはこの語彙に限定する —
 * 新しい trigger/effect 名は engine 実装が要るため、機構の新設は「補足/メモ」で提案する
 */
// ---- 編集UIの日本語ラベル (2026-09-01 ユーザー要望「json定義の名称がそのままでは指摘者に分かりにくい」) ----
// データのキーは英語のまま (エクスポート・実装の語彙)。表示だけ日本語に寄せる

/** trigger → 日本語 (TRIGGER_LABEL の「: 」抜き。onPlay はプレイ時) */
function triggerJa(t: string): string {
  if (t === 'onPlay') return 'プレイ時'
  const label = (TRIGGER_LABEL as Record<string, string>)[t]
  return label !== undefined && label !== '' ? label.replace(/: $/, '') : t
}

/** effect → 短い日本語名 (量はNで示す)。未知の効果名は生のまま出す */
const EFFECT_JA: Record<string, string> = {
  dealDamage: 'ダメージN', dealDamageRandom: 'ランダムダメージN〜上限', dealDamageDrain: 'ドレインN(半分回復)',
  dealDamageCleave: 'キル連鎖N', dealDamageExecute: '処刑N(HP25%以下で上限)',
  dealDamagePerBlock: 'ブロック×Nダメ', dealDamagePerIceBlock: '氷壁×Nダメ', dealDamagePerCardPlayed: '詠唱数×Nダメ',
  dealDamagePerCardPlayedTotal: '累計プレイ数×Nダメ', dealDamagePerEnergyMax: '上限×Nダメ', dealDamagePerMomentum: '勢い×Nダメ(非消費)',
  dealDamagePerExhaust: '消滅数×Nダメ', dealDamagePerHandCard: '手札数×Nダメ', dealDamagePerHeal: '回復回数×Nダメ',
  dealDamagePerDamageTaken: '被ダメ×Nダメ', dealDamagePerSelfHpLost: '失ったHP×Nダメ', dealDamagePerPermanent: '置物数×Nダメ',
  dealDamagePerRandomPlayed: '運任せ数×Nダメ', dealDamagePerNegStrength: '下げた筋力×Nダメ',
  gainBlock: 'ブロックN', gainBlockPerEnergyMax: '上限×Nブロック', gainBlockPerPermanent: '置物数×Nブロック',
  gainIceBlock: '氷壁N(持ち越し)', gainIceBlockPerCardPlayed: '詠唱数×N氷壁', gainIceBlockPerHandCard: '手札数×N氷壁',
  gainHp: 'HP回復N', loseHp: '自傷HP-N', counter: '返しN(リアクション)', negate: '打ち消し', negateConvertIce: '打ち消し+実値ぶん氷壁',
  drawCards: 'Nドロー', drawCardsPerCardPlayed: '詠唱数×Nドロー', impulseDraw: '衝動ドローN(このターン限り)',
  gainEnergy: '一時マナ+N', gainEnergyMax: 'エナジー上限+N', discountNext: '次のカード-N',
  addGrowth: '成長+N', doubleGrowth: '成長2倍', dischargeGrowth: '成長放出(×Nダメ全消費)', dischargeGrowthBlock: '成長×Nブロック(全消費)',
  addMomentum: '勢い+N', doubleMomentum: '勢い2倍', dischargeMomentumBlock: '勢い×Nブロック(全消費)', dischargeMomentumBurn: '勢い×N延焼(全消費)',
  applyBurn: '延焼+N', applyBurnPerDamageTaken: '被ダメ×N延焼', dischargeBurn: '爆熱(延焼×Nダメ全消費)',
  addAether: '霊気+N', dischargeAether: '霊気放出(×Nダメ全消費)', dischargeAetherDraw: '霊気×Nドロー(全消費)',
  addCasts: '詠唱数+N', addSpellEcho: '反復+N(次の呪文2回解決)', confuse: '混乱+N', exposeEnemy: '急所+N', weakenEnemy: '威圧N(敵の筋力-N)',
  shatterBlock: '粉砕(敵ブロック全壊)', shatterBlockConvert: '粉砕+破壊値ダメ',
  exhaustFromDeck: '山札の上N枚を消滅(ミル)', exhaustFromDeckChoose: '選んでN枚消滅(引導型)', recycleExhaust: '輪廻(消滅を山札へ・×Nダメ)',
  retrieveFromExhaust: '消滅置き場から回収', playFromExhaust: '消滅置き場から直接プレイ',
  summonPermanent: '召喚N体(summonId)', addCardToHand: 'トークンN枚を手札へ(summonId)',
  blessRetainers: '【常在】従者の効果+N', empowerShivs: '【常在】ナイフ与ダメ+N',
  gainSetSlot: '伏せ枠+N(この戦闘中)', retrieveFromDiscard: '捨て札からN枚を手札へ(選ぶ)', searchDeck: '山札からN枚を手札へ(選ぶ)',
  strengthenEnemy: '敵の筋力+N', addCopyToDiscard: 'コピーN枚を捨て札へ', growSelf: 'プレイするたび与ダメ+N(この戦闘中)', upgradeInHand: '手札のN枚をこの戦闘中鍛える',
}
function effectJa(e: string): string {
  return EFFECT_JA[e] ?? e
}

/** 条件キー → 日本語 */
const COND_JA: Record<string, string> = {
  blaze: '猛り火(延焼計8以上)',
  hpAtOrBelowRatio: 'HPが比率以下(0.5=半分)',
  minActionValue: '敵の行動値が値以上',
  maxActionValue: '敵の行動値が値以下',
  minDamageTaken: '被ダメが値以上',
  minGrowth: '成長が値以上',
}
function condJa(k: string): string {
  return COND_JA[k] ?? k
}

const CARD_VOCAB: { triggers: readonly string[]; effects: readonly string[]; condKeys: readonly string[] } = (() => {
  const t = new Set<string>()
  const e = new Set<string>()
  const c = new Set<string>()
  for (const card of allCards) {
    const effs = [...card.effects, ...(card.modes ?? []).flatMap((m) => m.effects)]
    for (const ef of effs) {
      t.add(ef.trigger)
      e.add(ef.effect)
      for (const k of Object.keys(ef.condition ?? {})) c.add(k)
    }
  }
  return { triggers: [...t].sort(), effects: [...e].sort(), condKeys: [...c].sort() }
})()

/** 既存カード定義 → ビルダー下書き (定義ごと差し替えの初期値) */
function defToDraft(def: CardDef): CardDraft {
  return {
    id: def.id,
    name: def.name,
    color: def.color,
    cost: def.cost,
    ...(def.xCost === true ? { xCost: true } : {}),
    type: def.type,
    rarity: def.rarity ?? 'common',
    ...(def.exhaust === true ? { exhaust: true } : {}),
    ...(typeof def.exhaustCost === 'number' ? { exhaustCost: def.exhaustCost } : {}),
    ...(typeof def.discardCost === 'number' ? { discardCost: def.discardCost } : {}),
    ...(typeof def.necroCost === 'number' ? { necroCost: def.necroCost } : {}),
    effects: def.effects.map((e) => {
      const [ck, cv] = Object.entries(e.condition ?? {})[0] ?? []
      return {
        trigger: e.trigger,
        effect: e.effect,
        ...(typeof e.amount === 'number' ? { amount: e.amount } : {}),
        ...(typeof e.amountMax === 'number' ? { amountMax: e.amountMax } : {}),
        ...(e.target === 'all' ? { target: 'all' as const } : {}),
        ...(e.pierce === true ? { pierce: true } : {}),
        ...(typeof e.summonId === 'string' ? { summonId: e.summonId } : {}),
        ...(ck !== undefined ? { condKey: ck } : {}),
        ...(typeof cv === 'number' ? { condValue: cv } : {}),
      }
    }),
  }
}

const numOrUndef = (v: string): number | undefined =>
  v === '' || !Number.isFinite(Number(v)) ? undefined : Number(v)

/** 効果1行のエディタ (実データの DeclarativeEffect と1:1) */
function EffectDraftRow({
  value,
  onChange,
  onDelete,
}: {
  value: EffectDraft
  onChange: (e: EffectDraft) => void
  onDelete: () => void
}) {
  const S = { fontSize: 11 } as const
  return (
    <div style={{ display: 'flex', gap: 4, alignItems: 'center', flexWrap: 'wrap', border: '1px solid #334', borderRadius: 4, padding: 3 }}>
      <select style={S} value={value.trigger} title={value.trigger} onChange={(ev) => onChange({ ...value, trigger: ev.target.value })}>
        {CARD_VOCAB.triggers.map((t) => (
          <option key={t} value={t}>{triggerJa(t)}</option>
        ))}
      </select>
      <select style={S} value={value.effect} title={value.effect} onChange={(ev) => onChange({ ...value, effect: ev.target.value })}>
        {CARD_VOCAB.effects.map((t) => (
          <option key={t} value={t}>{effectJa(t)}</option>
        ))}
      </select>
      <label style={S}>
        量{' '}
        <input type="number" step="any" style={{ width: 48 }} value={value.amount ?? ''} onChange={(ev) => onChange({ ...value, amount: numOrUndef(ev.target.value) })} />
      </label>
      <label style={S} title="ランダム火力のロール上限 (dealDamageRandom用)">
        上限{' '}
        <input type="number" style={{ width: 44 }} value={value.amountMax ?? ''} onChange={(ev) => onChange({ ...value, amountMax: numOrUndef(ev.target.value) })} />
      </label>
      <label style={S}>
        <input type="checkbox" checked={value.target === 'all'} onChange={(ev) => onChange({ ...value, target: ev.target.checked ? 'all' : undefined })} /> 全体
      </label>
      <label style={S}>
        <input type="checkbox" checked={value.pierce === true} onChange={(ev) => onChange({ ...value, pierce: ev.target.checked || undefined })} /> 貫通
      </label>
      <label style={S}>
        条件{' '}
        <select value={value.condKey ?? ''} title={value.condKey ?? ''} onChange={(ev) => onChange({ ...value, condKey: ev.target.value === '' ? undefined : ev.target.value })}>
          <option value="">なし</option>
          {CARD_VOCAB.condKeys.map((k) => (
            <option key={k} value={k}>{condJa(k)}</option>
          ))}
        </select>
      </label>
      {(value.condKey ?? '') !== '' && value.condKey !== 'blaze' && (
        <input type="number" step="any" style={{ width: 52 }} value={value.condValue ?? ''} onChange={(ev) => onChange({ ...value, condValue: numOrUndef(ev.target.value) })} />
      )}
      {value.effect === 'summonPermanent' && (
        <input placeholder="summonId (置物カードのid)" style={{ width: 160, fontSize: 11 }} value={value.summonId ?? ''} onChange={(ev) => onChange({ ...value, summonId: ev.target.value === '' ? undefined : ev.target.value })} />
      )}
      <button className="chip chip-btn" onClick={onDelete} title="この効果行を削除">✕</button>
    </div>
  )
}

/** カード1枚のビルダー (実データとして作れるレベル)。右にゲーム内描画そのままのプレビュー */
function CardDraftEditor({
  value,
  onChange,
  onDelete,
  deleteLabel,
}: {
  value: CardDraft
  onChange: (d: CardDraft) => void
  onDelete?: () => void
  deleteLabel?: string
}) {
  const S = { fontSize: 11 } as const
  const COLOR_JA: Record<string, string> = { green: '緑', blue: '青', red: '赤', white: '白', black: '黒' }
  const previewDef = cardDraftToDefJson(value) as unknown as CardDef
  return (
    <div style={{ border: '1px solid #556', borderRadius: 6, padding: 6, marginTop: 6, display: 'flex', gap: 8, alignItems: 'flex-start', flexWrap: 'wrap' }}>
      <div style={{ flex: '1 1 380px', display: 'flex', flexDirection: 'column', gap: 4 }}>
        <div style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
          <label style={S}>
            名前 <input value={value.name} onChange={(e) => onChange({ ...value, name: e.target.value })} style={{ width: 110 }} />
          </label>
          <label style={S}>
            id <input value={value.id ?? ''} placeholder="空=実装時に命名" onChange={(e) => onChange({ ...value, id: e.target.value === '' ? undefined : e.target.value })} style={{ width: 140 }} />
          </label>
          <label style={S}>
            色{' '}
            <select value={value.color} onChange={(e) => onChange({ ...value, color: e.target.value })}>
              {(['green', 'blue', 'red', 'white', 'black'] as const).map((c) => (
                <option key={c} value={c}>{COLOR_JA[c]}</option>
              ))}
            </select>
          </label>
          <label style={S}>
            タイプ{' '}
            <select value={value.type} onChange={(e) => onChange({ ...value, type: e.target.value })}>
              {(Object.keys(TYPE_LABEL) as CardType[]).map((t) => (
                <option key={t} value={t}>{TYPE_LABEL[t]}</option>
              ))}
            </select>
          </label>
          <label style={S}>
            コスト{' '}
            <select value={String(value.cost)} onChange={(e) => onChange({ ...value, cost: Number(e.target.value) })}>
              {[0, 1, 2, 3, 4, 5].map((c) => (
                <option key={c} value={String(c)}>{c}</option>
              ))}
            </select>
          </label>
          <label style={S} title="Xコスト (プレイ時に現在エナジー全払い)">
            <input type="checkbox" checked={value.xCost === true} onChange={(e) => onChange({ ...value, xCost: e.target.checked || undefined })} /> X
          </label>
          <label style={S}>
            レア{' '}
            <select value={value.rarity} onChange={(e) => onChange({ ...value, rarity: e.target.value })}>
              <option value="common">コモン</option>
              <option value="uncommon">アンコ</option>
              <option value="rare">レア</option>
            </select>
          </label>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
          <label style={S}>
            <input type="checkbox" checked={value.exhaust === true} onChange={(e) => onChange({ ...value, exhaust: e.target.checked || undefined })} /> 消滅
          </label>
          <label style={S}>
            消滅コスト <input type="number" style={{ width: 40 }} value={value.exhaustCost ?? ''} onChange={(e) => onChange({ ...value, exhaustCost: numOrUndef(e.target.value) })} />
          </label>
          <label style={S}>
            捨てコスト <input type="number" style={{ width: 40 }} value={value.discardCost ?? ''} onChange={(e) => onChange({ ...value, discardCost: numOrUndef(e.target.value) })} />
          </label>
          <label style={S}>
            亡骸コスト <input type="number" style={{ width: 40 }} value={value.necroCost ?? ''} onChange={(e) => onChange({ ...value, necroCost: numOrUndef(e.target.value) })} />
          </label>
        </div>
        {value.effects.map((ef, i) => (
          <EffectDraftRow
            key={i}
            value={ef}
            onChange={(ne) => onChange({ ...value, effects: value.effects.map((x, j) => (j === i ? ne : x)) })}
            onDelete={() => onChange({ ...value, effects: value.effects.filter((_, j) => j !== i) })}
          />
        ))}
        <div>
          <button className="chip chip-btn" onClick={() => onChange({ ...value, effects: [...value.effects, { trigger: 'onPlay', effect: 'dealDamage', amount: 6 }] })}>
            ＋ 効果を追加
          </button>{' '}
          {onDelete && (
            <button className="chip chip-btn" onClick={onDelete}>🗑 {deleteLabel ?? 'この下書きを削除'}</button>
          )}
        </div>
      </div>
      <CardFrame card={{ uid: `draft_${value.id ?? value.name}`, def: previewDef }} dim={false} hint="プレビュー（ゲーム内描画そのまま）" actions={null} />
    </div>
  )
}

/** カード図鑑 (2026-09-01 ユーザー要望「カード一覧見れるページ」)。全カードを色/タイプ/レア/検索で絞り込み。
 * 🛠調整モード (同日 ユーザー要望「カード調整サイクル」): 図鑑上で変更案・削除案・新カード案をマークし、
 * 1枚のmdに書き出してAIレビュー→実装へ渡す。下書きは localStorage に自動保存 */
function CardCatalogOverlay({ onClose }: { onClose: () => void }) {
  const [color, setColor] = useState('all')
  const [ctype, setCtype] = useState('all')
  const [rarity, setRarity] = useState('all')
  const [q, setQ] = useState('')
  const [showUpgraded, setShowUpgraded] = useState(false)
  const [tuner, setTuner] = useState(false)
  const [markedOnly, setMarkedOnly] = useState(false)
  const [draft, setDraft] = useState<TunerDraft>(loadTunerDraft)
  useEffect(() => {
    try {
      localStorage.setItem(TUNER_STORAGE_KEY, JSON.stringify(draft))
    } catch {
      /* 容量超過等は無視 = 下書きは保険 */
    }
  }, [draft])
  const [tab, setTab] = useState<'cards' | 'enemies' | 'relics' | 'leaders'>('cards')
  const [overlayOn, setOverlayOn] = useState(debugOverridesActive())
  const bundleOf = (d: TunerDraft): ProposalBundle => ({ cardMarks: d.marks, newCards: d.newCards, newCardDefs: d.newCardDefs, enemyMarks: d.enemyMarks, newEnemyDefs: d.newEnemyDefs, relicMarks: d.relicMarks, newRelicDefs: d.newRelicDefs, leaderMarks: d.leaderMarks, newLeaderDefs: d.newLeaderDefs })
  const markOf = (id: string): CardProposalMark => draft.marks[id] ?? {}
  const setMark = (id: string, m: CardProposalMark) =>
    setDraft((d) => {
      const marks = { ...d.marks }
      if (isEmptyMark(m)) delete marks[id]
      else marks[id] = m
      return { ...d, marks }
    })
  const setEnemyMark = (id: string, m: SimpleMark) =>
    setDraft((d) => {
      const marks = { ...d.enemyMarks }
      if (isEmptySimpleMark(m)) delete marks[id]
      else marks[id] = m
      return { ...d, enemyMarks: marks }
    })
  const setRelicMark = (id: string, m: SimpleMark) =>
    setDraft((d) => {
      const marks = { ...d.relicMarks }
      if (isEmptySimpleMark(m)) delete marks[id]
      else marks[id] = m
      return { ...d, relicMarks: marks }
    })
  const setLeaderMark = (id: string, m: SimpleMark) =>
    setDraft((d) => {
      const marks = { ...d.leaderMarks }
      if (isEmptySimpleMark(m)) delete marks[id]
      else marks[id] = m
      return { ...d, leaderMarks: marks }
    })
  const markedCount =
    Object.keys(draft.marks).length + Object.keys(draft.enemyMarks).length + Object.keys(draft.relicMarks).length + Object.keys(draft.leaderMarks).length
  const colorOf = (id: string, c?: string) => c ?? id.split('_')[0]
  const COLOR_LABEL: Record<string, string> = { green: '緑', blue: '青', red: '赤', white: '白', black: '黒' }
  const RARITY_LABEL: Record<string, string> = { common: 'コモン', uncommon: '◆アンコモン', rare: '★レア' }
  const pool = allCards
    .filter((c) => !c.id.startsWith('status_'))
    .filter((c) => color === 'all' || colorOf(c.id, c.color) === color)
    .filter((c) => ctype === 'all' || c.type === ctype)
    .filter((c) => rarity === 'all' || (c.rarity ?? 'common') === rarity)
    .filter((c) => q === '' || c.name.includes(q))
    .filter((c) => !(tuner && markedOnly) || draft.marks[c.id] !== undefined)
    .sort(
      (a, b) =>
        colorOf(a.id, a.color).localeCompare(colorOf(b.id, b.color)) ||
        a.cost - b.cost ||
        a.name.localeCompare(b.name, 'ja'),
    )
  const chip = (active: boolean, label: string, onClick: () => void) => (
    <button key={label} className={`chip chip-btn${active ? ' chip-mode' : ''}`} onClick={onClick}>
      {label}
    </button>
  )
  return (
    <div className="viewer-overlay" onClick={onClose}>
      <div className="viewer-panel" onClick={(e) => e.stopPropagation()}>
        <div className="viewer-head">
          <span className="viewer-title">📚 図鑑</span>
          {chip(tab === 'cards', `カード（${allCards.filter((c) => !c.id.startsWith('status_')).length}）`, () => setTab('cards'))}
          {chip(tab === 'enemies', `敵（${allEnemies.length}）`, () => setTab('enemies'))}
          {chip(tab === 'relics', `レリック（${allRelics.length}）`, () => setTab('relics'))}
          {chip(tab === 'leaders', `リーダー（${allLeaders.length}）`, () => setTab('leaders'))}
          {tab === 'cards' && (
            <label className="viewer-toggle">
              <input
                type="checkbox"
                checked={showUpgraded}
                onChange={(e) => setShowUpgraded(e.target.checked)}
              />{' '}
              鍛えた姿（+）で表示
            </label>
          )}
          <label className="viewer-toggle" title="変更案・削除案・新規案をマークして1枚のjsonに書き出す (AIレビュー→実装のサイクル用)">
            <input type="checkbox" checked={tuner} onChange={(e) => setTuner(e.target.checked)} />{' '}
            🛠 調整モード{markedCount > 0 ? `（${markedCount}件）` : ''}
          </label>
          <button className="btn" onClick={onClose}>
            ✕ 閉じる
          </button>
        </div>
        {tuner && (
          <div className="panel" style={{ marginTop: 8 }}>
            <div className="choice-desc">
              各カードの下でコスト・レアリティ・消滅・数値（効果量/ヒット数/条件値/追加コスト）を
              実データと同じ形で直接編集（現行値と違う値だけが提案として記録される）。
              構造で表せない意図は「補足」に自由記述、チェックで削除案。
              下書きは自動保存（リロードしても残る）。書き出したjson（現行データ同梱・パスキー付き）をAIに渡すと査定レビュー→実装される。
            </div>
            <div style={{ marginTop: 6 }}>
              <label className="viewer-toggle">
                <input
                  type="checkbox"
                  checked={markedOnly}
                  onChange={(e) => setMarkedOnly(e.target.checked)}
                />{' '}
                マーク済みのみ表示
              </label>{' '}
              <button className="btn btn-primary" onClick={() => saveProposals(bundleOf(draft))}>
                📄 調整案を書き出す
              </button>{' '}
              <button
                className="btn"
                title="マークした変更・新規案をこの場のゲームデータに反映する (削除案は対象外。リロードで元に戻る)"
                onClick={() => {
                  const res = applyDebugOverrides(buildOverrideDefs(bundleOf(draft)))
                  setOverlayOn(debugOverridesActive())
                  alert(`⚡ 適用しました: 置換${res.replaced}件 / 追加${res.added}件\n次に始めるラン・戦闘から反映されます (リロードで元に戻る。削除案は適用されない)`)
                }}
              >
                ⚡ 適用して遊ぶ
              </button>{' '}
              {overlayOn && (
                <button
                  className="btn"
                  onClick={() => {
                    clearDebugOverrides()
                    setOverlayOn(false)
                  }}
                >
                  ↩ 適用を解除
                </button>
              )}{' '}
              <label className="btn" style={{ display: 'inline-block', cursor: 'pointer' }} title="書き出した tuning-proposals-*.json を読み戻して編集を続ける">
                📂 調整案を読み込む
                <input
                  type="file"
                  accept=".json,application/json"
                  style={{ display: 'none' }}
                  onChange={(e) => {
                    const f = e.target.files?.[0]
                    e.target.value = ''
                    if (f === undefined) return
                    f.text()
                      .then((text) => {
                        const doc = JSON.parse(text) as { kind?: string; sourceDraft?: ProposalBundle }
                        if (doc.kind !== 'deck-rogue-tuning-proposals' || doc.sourceDraft === undefined) {
                          alert('調整案ファイル (tuning-proposals-*.json) ではないか、古い形式です')
                          return
                        }
                        const b = doc.sourceDraft
                        setDraft({
                          marks: { ...b.cardMarks },
                          newCards: b.newCards,
                          newCardDefs: [...b.newCardDefs],
                          enemyMarks: { ...(b.enemyMarks ?? {}) },
                          newEnemyDefs: [...(b.newEnemyDefs ?? [])],
                          relicMarks: { ...(b.relicMarks ?? {}) },
                          newRelicDefs: [...(b.newRelicDefs ?? [])],
                          leaderMarks: { ...(b.leaderMarks ?? {}) },
                          newLeaderDefs: [...(b.newLeaderDefs ?? [])],
                        })
                        alert('調整案を読み込みました (下書きを上書き)')
                      })
                      .catch((err) => alert(`読み込みに失敗: ${String(err)}`))
                  }}
                />
              </label>{' '}
              <button
                className="btn"
                onClick={() => {
                  if (window.confirm('調整案の下書きをすべて消しますか？')) {
                    setDraft({ marks: {}, newCards: '', newCardDefs: [], enemyMarks: {}, newEnemyDefs: [], relicMarks: {}, newRelicDefs: [], leaderMarks: {}, newLeaderDefs: [] })
                  }
                }}
              >
                🗑 下書きを全消去
              </button>
            </div>
            {tab === 'cards' && (
            <div style={{ marginTop: 6 }}>
              <b style={{ fontSize: 12 }}>新カード案（{draft.newCardDefs.length}件）</b>{' '}
              <button
                className="chip chip-btn"
                onClick={() =>
                  setDraft((d) => ({
                    ...d,
                    newCardDefs: [
                      ...d.newCardDefs,
                      { name: '', color: 'green', cost: 1, type: 'physical', rarity: 'common', effects: [{ trigger: 'onPlay', effect: 'dealDamage', amount: 6 }] },
                    ],
                  }))
                }
              >
                ＋ 新カード案を追加
              </button>
            </div>
            )}
            {tab === 'cards' && draft.newCardDefs.map((d, i) => (
              <CardDraftEditor
                key={i}
                value={d}
                onChange={(nd) => setDraft((s2) => ({ ...s2, newCardDefs: s2.newCardDefs.map((x, j) => (j === i ? nd : x)) }))}
                onDelete={() => setDraft((s2) => ({ ...s2, newCardDefs: s2.newCardDefs.filter((_, j) => j !== i) }))}
              />
            ))}
            {tab === 'enemies' && (
              <div style={{ marginTop: 6 }}>
                <b style={{ fontSize: 12 }}>新しい敵案（{draft.newEnemyDefs.length}件）</b>{' '}
                <button
                  className="chip chip-btn"
                  onClick={() =>
                    setDraft((d) => ({
                      ...d,
                      newEnemyDefs: [
                        ...d.newEnemyDefs,
                        { name: '', archetype: ENEMY_VOCAB.archetypes[0] ?? 'wide-power', maxHp: 50, moves: [{ id: 'strike', kind: 'attack', min: 6, max: 9, weight: 1 }] },
                      ],
                    }))
                  }
                >
                  ＋ 新しい敵案を追加
                </button>
              </div>
            )}
            {tab === 'enemies' && draft.newEnemyDefs.map((d, i) => (
              <EnemyDraftEditor
                key={i}
                value={d}
                onChange={(nd) => setDraft((s2) => ({ ...s2, newEnemyDefs: s2.newEnemyDefs.map((x, j) => (j === i ? nd : x)) }))}
                onDelete={() => setDraft((s2) => ({ ...s2, newEnemyDefs: s2.newEnemyDefs.filter((_, j) => j !== i) }))}
              />
            ))}
            {tab === 'relics' && (
              <div style={{ marginTop: 6 }}>
                <b style={{ fontSize: 12 }}>新レリック案（{draft.newRelicDefs.length}件）</b>{' '}
                <button
                  className="chip chip-btn"
                  onClick={() =>
                    setDraft((d) => ({
                      ...d,
                      newRelicDefs: [...d.newRelicDefs, { name: '', description: '', effects: [] }],
                    }))
                  }
                >
                  ＋ 新レリック案を追加
                </button>
              </div>
            )}
            {tab === 'relics' && draft.newRelicDefs.map((d, i) => (
              <RelicDraftEditor
                key={i}
                value={d}
                onChange={(nd) => setDraft((s2) => ({ ...s2, newRelicDefs: s2.newRelicDefs.map((x, j) => (j === i ? nd : x)) }))}
                onDelete={() => setDraft((s2) => ({ ...s2, newRelicDefs: s2.newRelicDefs.filter((_, j) => j !== i) }))}
              />
            ))}
            {tab === 'leaders' && (
              <div style={{ marginTop: 6 }}>
                <b style={{ fontSize: 12 }}>新リーダー案（{draft.newLeaderDefs.length}件）</b>{' '}
                <button
                  className="chip chip-btn"
                  onClick={() =>
                    setDraft((d) => ({
                      ...d,
                      newLeaderDefs: [
                        ...d.newLeaderDefs,
                        { name: '', colors: ['green'], maxHp: 75, drawPerTurn: 5, energyMax: 3, rewardChoices: 4, description: '', passive: [] },
                      ],
                    }))
                  }
                >
                  ＋ 新リーダー案を追加
                </button>
              </div>
            )}
            {tab === 'leaders' && draft.newLeaderDefs.map((d, i) => (
              <LeaderDraftEditor
                key={i}
                value={d}
                onChange={(nd) => setDraft((s2) => ({ ...s2, newLeaderDefs: s2.newLeaderDefs.map((x, j) => (j === i ? nd : x)) }))}
                onDelete={() => setDraft((s2) => ({ ...s2, newLeaderDefs: s2.newLeaderDefs.filter((_, j) => j !== i) }))}
              />
            ))}
            <textarea
              value={draft.newCards}
              onChange={(e) => setDraft((d) => ({ ...d, newCards: e.target.value }))}
              placeholder={'メモ (自由記述。新しい機構の提案・狙いの説明など、構造で表せないものはここへ)'}
              rows={2}
              style={{ width: '100%', marginTop: 6, boxSizing: 'border-box' }}
            />
          </div>
        )}
        {tab === 'cards' && (
        <div style={{ marginTop: 8 }}>
          {chip(color === 'all', '全色', () => setColor('all'))}
          {(['green', 'blue', 'red', 'white', 'black'] as const).map((c) =>
            chip(color === c, COLOR_LABEL[c], () => setColor(c)),
          )}
          {' ｜ '}
          {chip(ctype === 'all', '全タイプ', () => setCtype('all'))}
          {(Object.keys(TYPE_LABEL) as CardType[]).map((t) =>
            chip(ctype === t, TYPE_LABEL[t], () => setCtype(t)),
          )}
          {' ｜ '}
          {chip(rarity === 'all', '全レア度', () => setRarity('all'))}
          {(['common', 'uncommon', 'rare'] as const).map((r) =>
            chip(rarity === r, RARITY_LABEL[r], () => setRarity(r)),
          )}{' '}
          <input
            placeholder="名前で検索"
            value={q}
            onChange={(e) => setQ(e.target.value)}
            style={{ marginLeft: 8 }}
          />
        </div>
        )}
        {tab === 'enemies' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 8 }}>
            {allEnemies
              .filter((e) => !(tuner && markedOnly) || draft.enemyMarks[e.id] !== undefined)
              .map((e) => {
                const dirty = draft.enemyMarks[e.id] !== undefined
                return (
                  <div key={e.id} className="panel" style={{ padding: 6, background: dirty ? 'rgba(120,160,255,0.10)' : undefined }}>
                    <b>{e.name}</b>{' '}
                    <span className="choice-desc">
                      {e.id} / HP{e.maxHp} / {ARCHETYPE_LABEL[e.archetype] ?? e.archetype}
                    </span>
                    <div className="choice-desc" style={{ fontSize: 11 }}>
                      {e.moves.map(moveLine).join('　')}
                      {e.movesVsSet !== undefined ? `　◆伏せ反応: ${e.movesVsSet.map(moveLine).join(' ')}` : ''}
                      {e.movesVsTokens !== undefined ? `　◆従者反応: ${e.movesVsTokens.map(moveLine).join(' ')}` : ''}
                      {e.movesBelowHalf !== undefined ? `　◆半分以下: ${e.movesBelowHalf.map(moveLine).join(' ')}` : ''}
                      {e.sequence !== undefined ? `　◇ローテ: ${e.sequence.join('→')}` : ''}
                    </div>
                    {tuner && (
                      <SimpleMarkEditor fields={enemyTunerFields(e)} mark={draft.enemyMarks[e.id] ?? {}} onChange={(m) => setEnemyMark(e.id, m)} />
                    )}
                  </div>
                )
              })}
          </div>
        )}
        {tab === 'relics' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 8 }}>
            {allRelics
              .filter((r) => !(tuner && markedOnly) || draft.relicMarks[r.id] !== undefined)
              .map((r) => {
                const dirty = draft.relicMarks[r.id] !== undefined
                return (
                  <div key={r.id} className="panel" style={{ padding: 6, background: dirty ? 'rgba(120,160,255,0.10)' : undefined }}>
                    <b>{r.sprite} {r.name}</b> <span className="choice-desc">{r.id}</span>
                    <div className="choice-desc" style={{ fontSize: 11 }}>{r.description}</div>
                    {tuner && (
                      <SimpleMarkEditor fields={relicTunerFields(r)} mark={draft.relicMarks[r.id] ?? {}} onChange={(m) => setRelicMark(r.id, m)} />
                    )}
                  </div>
                )
              })}
          </div>
        )}
        {tab === 'leaders' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 8 }}>
            {allLeaders
              .filter((l) => !(tuner && markedOnly) || draft.leaderMarks[l.id] !== undefined)
              .map((l) => {
                const dirty = draft.leaderMarks[l.id] !== undefined
                const COLOR_JA: Record<string, string> = { green: '緑', blue: '青', red: '赤', white: '白', black: '黒' }
                return (
                  <div key={l.id} className="panel" style={{ padding: 6, background: dirty ? 'rgba(120,160,255,0.10)' : undefined }}>
                    <b>{l.sprite} {l.name}</b>{' '}
                    <span className="choice-desc">
                      {l.id} / {l.colors.map((c) => COLOR_JA[c] ?? c).join('')} / HP{l.maxHp} / ドロー{l.drawPerTurn} / エナジー{l.energyMax} / ピック{l.rewardChoices}{(l.setSlots ?? 1) > 1 ? ` / 伏せ枠${l.setSlots}` : ''}
                    </span>
                    <div className="choice-desc" style={{ fontSize: 11 }}>{l.description}</div>
                    <div className="choice-desc" style={{ fontSize: 11 }}>
                      パッシブ: {l.passive.length > 0 ? l.passive.map((e) => renderEffectItem(e)).join('、') : '（なし）'} ／ 初期デッキ: {l.runDeckId}
                    </div>
                    {tuner && (
                      <SimpleMarkEditor fields={leaderTunerFields(l)} mark={draft.leaderMarks[l.id] ?? {}} onChange={(m) => setLeaderMark(l.id, m)} />
                    )}
                  </div>
                )
              })}
          </div>
        )}
        {tab === 'cards' && (
        <div className="hand-cards viewer-cards">
          {pool.length === 0 && <p className="hint">（該当なし）</p>}
          {pool.map((def) => {
            const inst = { uid: `cat_${def.id}`, def }
            const upgradable = canUpgradeCard(inst)
            const shown = showUpgraded && upgradable ? upgradeCard(inst) : inst
            const frame = (
              <CardFrame
                key={tuner ? undefined : def.id}
                card={shown}
                dim={tuner && markOf(def.id).remove === true}
                hint={`${COLOR_LABEL[colorOf(def.id, def.color)] ?? colorOf(def.id, def.color)}・${RARITY_LABEL[def.rarity ?? 'common']}${showUpgraded && !upgradable ? '・鍛えられない' : ''}`}
                actions={null}
              />
            )
            if (!tuner) return frame
            const mark = markOf(def.id)
            // 実データと同じ形の数値フィールド (2026-09-01 ユーザー指摘「実データと揃える形のほうが楽」)。
            // 現行値と違う値だけが提案として mark.fields に残る
            const numFields: { key: string; label: string; cur: number }[] = []
            def.effects.forEach((ef, i) => {
              const short = renderEffectItem(ef).slice(0, 14)
              if (typeof ef.amount === 'number') numFields.push({ key: `e${i}.amount`, label: short, cur: ef.amount })
              if (typeof ef.amountMax === 'number') numFields.push({ key: `e${i}.amountMax`, label: `${short}上限`, cur: ef.amountMax })
              for (const [ck, cv] of Object.entries(ef.condition ?? {})) {
                if (typeof cv === 'number') numFields.push({ key: `e${i}.cond.${ck}`, label: `条件〔${condJa(ck)}〕`, cur: cv })
              }
            })
            const COST_LABELS: Record<string, string> = { exhaustCost: '消滅コスト', discardCost: '捨てコスト', necroCost: '亡骸コスト' }
            for (const ck of ['exhaustCost', 'discardCost', 'necroCost'] as const) {
              const cv = def[ck]
              if (typeof cv === 'number') numFields.push({ key: ck, label: COST_LABELS[ck], cur: cv })
            }
            const setField = (key: string, cur: number, raw: string) => {
              const fields: Record<string, number> = { ...(mark.fields ?? {}) }
              const v = Number(raw)
              if (raw === '' || !Number.isFinite(v) || v === cur) delete fields[key]
              else fields[key] = v
              setMark(def.id, { ...mark, fields })
            }
            const curCost = def.xCost === true ? 'X' : String(def.cost)
            const curRarity = def.rarity ?? 'common'
            const dirty = draft.marks[def.id] !== undefined
            const S = { fontSize: 11, opacity: 0.9 } as const
            return (
              <div
                key={def.id}
                style={{ display: 'flex', flexDirection: 'column', gap: 2, padding: 3, borderRadius: 6, background: dirty ? 'rgba(120,160,255,0.12)' : 'transparent', width: mark.redef !== undefined ? '100%' : undefined }}
              >
                {frame}
                <div style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
                  <label style={S}>
                    コスト{' '}
                    <select
                      value={mark.cost ?? curCost}
                      onChange={(e) => setMark(def.id, { ...mark, cost: e.target.value === curCost ? undefined : e.target.value })}
                    >
                      {['0', '1', '2', '3', '4', '5', 'X'].map((c) => (
                        <option key={c} value={c}>{c}</option>
                      ))}
                    </select>
                  </label>
                  <label style={S}>
                    レア{' '}
                    <select
                      value={mark.rarity ?? curRarity}
                      onChange={(e) => setMark(def.id, { ...mark, rarity: e.target.value === curRarity ? undefined : e.target.value })}
                    >
                      <option value="common">コモン</option>
                      <option value="uncommon">アンコ</option>
                      <option value="rare">レア</option>
                    </select>
                  </label>
                  <label style={S}>
                    <input
                      type="checkbox"
                      checked={mark.exhaust ?? def.exhaust === true}
                      onChange={(e) => setMark(def.id, { ...mark, exhaust: e.target.checked === (def.exhaust === true) ? undefined : e.target.checked })}
                    />{' '}
                    消滅
                  </label>
                  <label style={{ ...S, color: mark.remove === true ? '#f88' : undefined }}>
                    <input
                      type="checkbox"
                      checked={mark.remove === true}
                      onChange={(e) => setMark(def.id, { ...mark, remove: e.target.checked || undefined })}
                    />{' '}
                    削除案
                  </label>
                  {def.modes === undefined ? (
                    <button
                      className="chip chip-btn"
                      title="効果構成ごと差し替える提案 (実データの形でフル編集)"
                      onClick={() => setMark(def.id, { ...mark, redef: mark.redef === undefined ? defToDraft(def) : undefined })}
                    >
                      ✏️ {mark.redef !== undefined ? '差し替えを破棄' : '定義編集'}
                    </button>
                  ) : (
                    <span style={{ fontSize: 10, opacity: 0.6 }} title="モード札のフル編集は未対応 — 補足に書く">モード札</span>
                  )}
                </div>
                {mark.redef !== undefined && (
                  <CardDraftEditor value={mark.redef} onChange={(d) => setMark(def.id, { ...mark, redef: d })} />
                )}
                {numFields.map((f) => (
                  <label key={f.key} style={{ ...S, display: 'flex', gap: 4, alignItems: 'center' }}>
                    <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={f.key}>
                      {f.label}
                    </span>
                    <input
                      type="number"
                      value={mark.fields?.[f.key] ?? f.cur}
                      onChange={(e) => setField(f.key, f.cur, e.target.value)}
                      style={{ width: 52, fontSize: 11, background: mark.fields?.[f.key] !== undefined ? 'rgba(120,160,255,0.25)' : undefined }}
                    />
                  </label>
                ))}
                <input
                  value={mark.change ?? ''}
                  onChange={(e) => setMark(def.id, { ...mark, change: e.target.value })}
                  placeholder="補足 (自由記述)"
                  style={{ width: '100%', boxSizing: 'border-box', fontSize: 11 }}
                />
              </div>
            )
          })}
        </div>
        )}
      </div>
    </div>
  )
}

/** 選択履歴チップ (2026-09-01)。ピック・鍛錬・合成・購入・イベントの意思決定を一覧するオーバーレイ */
function ChoiceLogChip({ choices }: { choices: readonly RunChoice[] }) {
  const [open, setOpen] = useState(false)
  return (
    <>
      <button className="chip chip-btn" onClick={() => setOpen(true)}>
        📜 選択 {choices.length}件
      </button>
      {open && (
        <div className="viewer-overlay" onClick={() => setOpen(false)}>
          <div className="viewer-panel" onClick={(e) => e.stopPropagation()}>
            <div className="viewer-head">
              <span className="viewer-title">📜 選択履歴（{choices.length}件）</span>
              <button className="btn" onClick={() => setOpen(false)}>✕ 閉じる</button>
            </div>
            <div style={{ marginTop: 8, display: 'flex', flexDirection: 'column', gap: 3 }}>
              {choices.length === 0 && <p className="hint">（このランの記録はまだありません。今のランがジャーナル記録前に始まった場合も空になります）</p>}
              {choices.map((c, i) => (
                <div key={i} className="choice-desc" style={{ fontSize: 12 }}>
                  <span style={{ opacity: 0.6 }}>[{c.at}]</span> {c.text}
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </>
  )
}

/** 「デッキN枚を見る」チップ (自前でオーバーレイの開閉を持つ = どの画面にも1行で置ける) */
function DeckChip({ run }: { run: RunState }) {
  const [open, setOpen] = useState(false)
  return (
    <>
      <button className="chip chip-btn" onClick={() => setOpen(true)}>
        🎴 デッキ {run.deck.length}枚を見る
      </button>
      {open && (
        <CardListOverlay title="🎴 デッキ" cards={run.deck} onClose={() => setOpen(false)} />
      )}
    </>
  )
}

// ---- ドラフト連戦 (ラン) 画面 ----

function RunScreen({
  run,
  dispatch,
  history,
  notes,
  journal,
  choices,
  onExit,
  onRestart,
  onReplay,
}: {
  run: RunState
  dispatch: (c: RunCommand) => void
  /** 決着済みの戦闘の履歴 (書き出しに含める) */
  history: readonly BattleArchive[]
  /** プレイ中メモ (書き出しに同梱) */
  notes: readonly PlayNote[]
  /** リプレイ・ジャーナル (セーブに同梱する。記録が無いラン=旧セーブ由来は null) */
  journal?: RunJournal | null
  /** 選択履歴 (ピック・鍛錬・合成・購入の意思決定ログ。書き出しに同梱) */
  choices?: readonly RunChoice[]
  onExit: () => void
  onRestart: (seed: number) => void
  /** このランのリプレイを開く (記録がある時だけ渡される) */
  onReplay?: () => void
}) {
  const isBoss = currentNode(run)?.type === 'boss'
  const progressChip = `幕${run.act}/3・${isBoss ? '👑 幕ボス戦' : run.currentElite ? `⚔️👑 強個体戦 (行${run.row + 1}/${run.map.length})` : `行${run.row + 1}/${run.map.length}・${run.battlesWon}勝`}・デッキ${run.deck.length}枚・🎚${run.difficulty ?? DEFAULT_DIFFICULTY}`
  // 報酬ピックの「鍛えた姿(+)で表示」(2026-08-31 ユーザー要望。本家のアップグレードプレビュー相当)
  const [showUpgradedPick, setShowUpgradedPick] = useState(false)
  const ctx = undefined
  // 所持レリックの表示行 (ホバーで効果説明)
  const relicChips =
    run.relics.length > 0 ? (
      <div style={{ marginTop: 6 }}>
        {run.relics.map((id) => {
          const r = getRelicDef(id)
          return (
            <span key={id} className="chip">
              <span className="kw">
                {r.sprite} {r.name}
                <span className="kw-tip">{r.description}</span>
              </span>
            </span>
          )
        })}
      </div>
    ) : null

  if (run.phase === 'map') {
    return (
      <div className="app setup">
        <h1>🗺 マップ — 第{run.act}幕/3</h1>
        <div className="panel">
          <div className="choice-desc">
            全体も道（接続線）も最初から見える。<b>緑の実線＝いま進める道</b>／金の線＝通ってきた道／薄い点線＝現在地から到達できない道（接続は前の行でどの列を選んだかで決まる）。👑強個体=固有ギミックの専用敵、勝てばレリック3択+レア1枚確定（逃がすとレア無し）。🔥焚き火=休む(25%回復)/鍛える/取り除く の択一。🔨工房=カード合成。🛒ショップ。🎁宝箱=レリック3択。❓=入るまで不明
          </div>
          <div style={{ marginTop: 6 }}>
            <span className="chip">HP {run.hp}/{run.maxHp}</span>
            <span className="chip">💰 {run.gold}G</span>
            <DeckChip run={run} />
            <ChoiceLogChip choices={choices ?? []} />
            <span className="chip">{run.battlesWon}勝</span>
            <span className="chip">🎚 難易度 {run.difficulty ?? DEFAULT_DIFFICULTY}</span>
          </div>
          {relicChips}
        </div>
        <div className="map-wrap">
          <RunMapView run={run} onChoose={(col) => dispatch({ type: 'ChooseNode', col })} />
        </div>
        <div style={{ marginTop: 12 }}>
          <button className="btn" onClick={() => saveReport(run, null, history, notes, choices ?? [])}>📄 状況を書き出す</button>{' '}
          <button className="btn" onClick={() => saveRunFile(run, history, notes, journal ?? null, choices ?? [])}>💾 セーブを書き出す</button>{' '}
          <button className="btn" onClick={onExit}>ランを放棄（自動保存は残る）</button>
        </div>
      </div>
    )
  }


  if (run.phase === 'relic-reward') {
    return (
      <div className="app setup">
        <h1>🏆 強個体撃破！ レリックを選べ</h1>
        {run.combat?.phase === 'won' && (
          <p className="hint">⚔️ 戦いの記録: {summaryLine(battleSummary(run.combat.eventLog))}</p>
        )}
        <div className="choice-row" style={{ marginTop: 12 }}>
          {(run.relicOptions ?? []).map((id, i) => {
            const r = getRelicDef(id)
            return (
              <button
                key={id}
                className="choice"
                {...(i < 9 ? { 'data-hotkey': `num-${i + 1}` } : {})}
                onClick={() => dispatch({ type: 'PickRelic', index: i })}
              >
                <div className="choice-title">
                  <span className="choice-sprite">{r.sprite}</span>
                  {r.name}
                  {relicRarityTag(r) && <span className="chip" style={{ marginLeft: 6 }}>{relicRarityTag(r)}</span>}
                </div>
                <div className="choice-desc">{r.description}</div>
              </button>
            )
          })}
        </div>
        <button
          className="btn"
          style={{ marginTop: 12 }}
          data-hotkey="skip"
          onClick={() => dispatch({ type: 'SkipRelic' })}
        >
          見送る（S）
        </button>
      </div>
    )
  }

  if (run.phase === 'shop' && run.shop) {
    return (
      <div className="app setup">
        <h1>🛒 ショップ</h1>
        <div className="panel">
          <span className="chip">💰 {run.gold}G</span>
          <span className="chip">HP {run.hp}/{run.maxHp}</span>
          <DeckChip run={run} />
          <div className="choice-desc" style={{ marginTop: 6 }}>
            買わずに出てもよい。除去・鍛えるは回数無制限（使うたび+50G逓増）。
          </div>
        </div>
        <div className="setup-section-title">カード</div>
        <div className="hand-cards" style={{ margin: '12px 0' }}>
          {run.shop.cards.map((item, i) => (
            <CardFrame
              key={`${item.id}_${i}`}
              card={{ uid: `shop${i}`, def: getCardDef(item.id) }}
              dim={item.sold === true}
              ctx={ctx}
              hint={getCardDef(item.id).rarity === 'rare' ? '★レア（確定枠）' : getCardDef(item.id).rarity === 'uncommon' ? '◆アンコモン' : undefined}
              actions={
                <button
                  className="btn btn-primary"
                  disabled={item.sold === true || run.gold < item.price}
                  onClick={() => dispatch({ type: 'ShopBuyCard', index: i })}
                >
                  {item.sold === true ? '売切' : `${item.price}G で買う`}
                </button>
              }
            />
          ))}
        </div>
        {run.shop.relicId !== null && (
          <div className="panel">
            <div className="setup-section-title">レリック</div>
            {(() => {
              const r = getRelicDef(run.shop!.relicId!)
              return (
                <div>
                  <span className="chip">{r.sprite} {r.name}</span>
                  <span className="choice-desc"> {r.description}</span>{' '}
                  <button
                    className="btn btn-primary"
                    disabled={run.gold < run.shop!.relicPrice}
                    onClick={() => dispatch({ type: 'ShopBuyRelic' })}
                  >
                    {run.shop!.relicPrice}G で買う
                  </button>
                </div>
              )
            })()}
          </div>
        )}
        <div className="panel">
          <div className="setup-section-title">
            サービス: 除去 {shopRemovalPrice(run)}G ／ 鍛える {shopUpgradePrice(run)}G（回数無制限・使うたび値上がり）
          </div>
          <div className="hand-cards" style={{ marginTop: 8 }}>
            {run.deck.map((c, i) => (
              <CardFrame
                key={c.uid}
                card={c}
                dim={false}
                ctx={ctx}
                actions={
                  <>
                    {canUpgradeCard(c) && (
                      <div className="choice-desc" style={{ marginBottom: 4 }}>
                        鍛えると→ {describeUpgrade(c)}
                      </div>
                    )}
                    <button
                      className="btn"
                      disabled={run.gold < shopRemovalPrice(run) || run.deck.length <= 5}
                      onClick={() => dispatch({ type: 'ShopRemove', index: i })}
                    >
                      除去 {shopRemovalPrice(run)}G
                    </button>{' '}
                    <button
                      className="btn"
                      disabled={run.gold < shopUpgradePrice(run) || !canUpgradeCard(c)}
                      onClick={() => dispatch({ type: 'ShopUpgrade', index: i })}
                    >
                      鍛える {shopUpgradePrice(run)}G
                    </button>
                  </>
                }
              />
            ))}
          </div>
        </div>
        <button className="btn" style={{ marginTop: 12 }} onClick={() => dispatch({ type: 'ShopLeave' })}>
          店を出る
        </button>
      </div>
    )
  }

  if (run.phase === 'event') {
    const ev = getEventDef(run.eventId!)
    return (
      <div className="app setup">
        <h1>{ev.sprite ?? '❓'} {ev.name}</h1>
        <div className="panel">
          <div className="choice-desc">{ev.flavor}</div>
          <div style={{ marginTop: 6 }}>
            <span className="chip">HP {run.hp}/{run.maxHp}</span>
            <span className="chip">💰 {run.gold}G</span>
            {/* デッキ確認 (2026-09-01 ユーザー指摘「イベント画面に強化確認する方法がない」。
                ビューアの「鍛えた姿(+)で表示」トグルで強化後の姿も確認できる) */}
            <DeckChip run={run} />
          </div>
        </div>
        <div style={{ marginTop: 12 }}>
          {ev.choices.map((c, i) => {
            const goldLocked = c.requireGold !== undefined && run.gold < c.requireGold
            const needsCard = eventChoiceNeedsCard(c)
            if (!needsCard) {
              return (
                <div key={i} style={{ margin: '6px 0' }}>
                  <button
                    className="btn btn-primary"
                    disabled={goldLocked}
                    onClick={() => dispatch({ type: 'EventChoice', index: i })}
                  >
                    {c.label}
                    {goldLocked ? '（G不足）' : ''}
                  </button>
                </div>
              )
            }
            return (
              <div key={i} className="panel" style={{ margin: '6px 0' }}>
                <div className="choice-title">{c.label} — 対象を選ぶ:</div>
                <div className="hand-cards" style={{ marginTop: 8 }}>
                  {run.deck.map((card, ci) => {
                    // 鍛える系の選択肢は結果をプレビューし、鍛えられない札は選べない
                    // (2026-09-01 ユーザー指摘「鍛えた後どうなるかチェックできないイベント」)
                    const isUpgradeChoice = c.upgradeCard === true
                    const locked = isUpgradeChoice && !canUpgradeCard(card)
                    return (
                      <CardFrame
                        key={card.uid}
                        card={card}
                        dim={locked}
                        ctx={ctx}
                        actions={
                          <>
                            {isUpgradeChoice && !locked && (
                              <div className="choice-desc" style={{ marginBottom: 4 }}>
                                鍛えると→ {describeUpgrade(card)}
                              </div>
                            )}
                            {c.transformCard === true && (
                              <div className="choice-desc" style={{ marginBottom: 4 }}>
                                同レアリティのランダムな別カードに変わる
                              </div>
                            )}
                            <button
                              className="btn"
                              disabled={locked}
                              onClick={() => dispatch({ type: 'EventChoice', index: i, cardIndex: ci })}
                            >
                              {locked ? '鍛えられない' : 'このカードを選ぶ'}
                            </button>
                          </>
                        }
                      />
                    )
                  })}
                </div>
              </div>
            )
          })}
        </div>
      </div>
    )
  }

  if (run.phase === 'workshop') {
    return <WorkshopScreen run={run} dispatch={dispatch} ctx={ctx} />
  }

  if (run.phase === 'campfire') {
    // 上限クランプ後の実回復量 (満タンで+41と表示される誤解を防ぐ 2026-08-30)
    const heal = Math.min(Math.floor(run.maxHp * run.campfireRatio), run.maxHp - run.hp)
    // 鍛えるが使えない焚き火 (この焚き火で使用済み) では強化UIを丸ごと畳む
    // (2026-08-31 再検証ラン指摘④)。幕1の通算制限は撤廃 (2026-09-01 ユーザー指示)
    const canForgeHere = 1 + (run.campfireForgeBonus ?? 0) - (run.campfireUpgradesUsed ?? 0) > 0
    return (
      <div className="app setup">
        <h1>🔥 焚き火</h1>
        <p className="hint">
          「休む」「鍛える」「取り除く」から1つを選ぶ（本家式の排他三択。2026-08-29復帰）。
          {(run.campfireForgeBonus ?? 0) > 0 &&
            Math.max(0, 1 + (run.campfireForgeBonus ?? 0) - (run.campfireUpgradesUsed ?? 0)) > 0 &&
            ` 🪨鍛冶の砥石: 鍛えるはあと${Math.max(0, 1 + (run.campfireForgeBonus ?? 0) - (run.campfireUpgradesUsed ?? 0))}枚（休む・除去とは併用不可）。`}
        </p>
        <div className="choice-row" style={{ marginTop: 12 }}>
          <button className="choice" onClick={() => dispatch({ type: 'CampfireRest' })}>
            <div className="choice-title">
              <span className="choice-sprite">🛌</span>
              {(run.campfireUpgradesUsed ?? 0) > 0 || heal <= 0 ? '立ち去る' : '休む'}
            </div>
            <div className="choice-desc">
              {(run.campfireUpgradesUsed ?? 0) > 0
                ? 'すでに鍛えたので回復はなし'
                : heal <= 0
                  ? `HP満タンなので回復はなし（現在 ${run.hp}/${run.maxHp}）`
                  : `HP+${heal} 回復して先へ（現在 ${run.hp}/${run.maxHp}）`}
            </div>
          </button>
        </div>
        <div className="setup-section-title" style={{ marginTop: 20 }}>
          {canForgeHere
            ? `デッキの1枚を「鍛える」（デッキ${run.deck.length}枚。除去はショップのみ）`
            : '鍛えるはこの焚き火では使用済み（除去はショップのみ）'}
        </div>
        <div className="hand-cards" style={{ margin: '12px 0' }}>
          {run.deck.map((c, i) => (
            <CardFrame
              key={c.uid}
              card={c}
              dim={false}
              ctx={ctx}
              actions={
                <>
                  {canForgeHere && canUpgradeCard(c) && (
                    <div className="choice-desc" style={{ marginBottom: 4 }}>
                      鍛えると→ {describeUpgrade(c)}
                    </div>
                  )}
                  {canForgeHere && (
                    <button
                      className="btn btn-primary"
                      disabled={!canUpgradeCard(c)}
                      onClick={() => dispatch({ type: 'CampfireUpgrade', index: i })}
                    >
                      {isUpgraded(c) ? '鍛済' : canUpgradeCard(c) ? '鍛える' : '鍛不可'}
                    </button>
                  )}
                </>
              }
            />
          ))}
        </div>
      </div>
    )
  }

  if (run.phase === 'combat' && run.combat) {
    return (
      <BattleScreen
        onExport={() => saveReport(run, null, history, notes, choices ?? [])}
        state={run.combat}
        config={{
          mode: run.mode,
          enemyId: currentNode(run)?.encounterId ?? 'enemy_probe',
          deckId: 'run_basic',
          leaderId: run.leaderId,
          seed: run.seed,
        }}
        dispatch={(command) => dispatch({ type: 'Combat', command })}
        onRestart={() => {}}
        onBack={onExit}
        extraChip={progressChip}
        backLabel="ランを放棄"
        run={run}
      />
    )
  }

  if (run.phase === 'reward') {
    return (
      <div className="app setup">
        <h1>🎴 報酬ピック</h1>
        {run.combat?.phase === 'won' && (
          <p className="hint">⚔️ 戦いの記録: {summaryLine(battleSummary(run.combat.eventLog))}</p>
        )}
        <div className="panel">
          <span className="chip">戦闘 {run.battlesWon}勝</span>
          <span className="chip">HP {run.hp}/{run.maxHp}</span>
          <DeckChip run={run} />
          {relicChips}
        </div>
        <div className="setup-section-title">
          1枚選んでデッキに加える（スキップ可）{' '}
          <label className="viewer-toggle">
            <input
              type="checkbox"
              checked={showUpgradedPick}
              onChange={(e) => setShowUpgradedPick(e.target.checked)}
            />{' '}
            鍛えた姿（+）で表示
          </label>
        </div>
        {run.currentElite && run.combat?.enemies.some((e) => e.fled === true) && (
          <div style={{ color: '#e6b422', margin: '6px 0' }}>
            ⚠ 逃走されたため、エリートのレア確定枠を失いました（レリック3択は残ります）
          </div>
        )}
        <div className="hand-cards" style={{ margin: '12px 0' }}>
          {(run.rewardOptions ?? []).map((cardId, i) => {
            const inst = { uid: `opt${i}`, def: getCardDef(cardId) }
            const upgradable = canUpgradeCard(inst)
            const shown = showUpgradedPick && upgradable ? upgradeCard(inst) : inst
            return (
              <CardFrame
                key={i}
                card={shown}
                dim={false}
                ctx={ctx}
                hint={showUpgradedPick && !upgradable ? '鍛えられない' : undefined}
                actions={
                  <button className="btn btn-primary" {...(i < 9 ? { 'data-hotkey': `num-${i + 1}` } : {})} onClick={() => dispatch({ type: 'PickReward', index: i })}>
                    獲得する{showUpgradedPick && upgradable ? '（獲得は無印のまま）' : ''}
                  </button>
                }
              />
            )
          })}
        </div>
        <button className="btn" data-hotkey="skip" onClick={() => dispatch({ type: 'SkipReward' })}>
          スキップして次へ（S）
        </button>{' '}
        <button className="btn" onClick={() => saveReport(run, null, history, notes, choices ?? [])}>
          📄 状況を書き出す
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
        {won ? '🏆 ラン制覇！' : `💀 ラン終了（行${run.row + 1}で敗北）`}
      </h1>
      <div className="panel">
        <div className="choice-desc">
          到達: {won ? 'ボス撃破' : `${run.battlesWon}戦クリア`} / seed {run.seed} /
          難易度 {run.difficulty ?? DEFAULT_DIFFICULTY} / 最終デッキ {run.deck.length}枚
        </div>
        <div className="choice-desc" style={{ marginTop: 6 }}>
          ピック履歴:{' '}
          {run.picks.length > 0 ? run.picks.map((id) => getCardDef(id).name).join('、') : '（なし）'}
        </div>
      </div>
      <div style={{ marginTop: 16 }}>
        <button className="btn btn-primary" onClick={() => saveReport(run, null, history, notes, choices ?? [])}>
          📄 状況を書き出す
        </button>{' '}
        <button className="btn" onClick={() => onRestart(Date.now() % 2 ** 32)}>
          新シードで再挑戦
        </button>{' '}
        <button className="btn" onClick={() => onRestart(run.seed)}>
          同シードで再挑戦
        </button>{' '}
        {onReplay !== undefined && (
          <button className="btn" onClick={onReplay}>
            🎬 このランをリプレイ
          </button>
        )}{' '}
        <ChoiceLogChip choices={choices ?? []} />{' '}
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
  // ラン全10戦の履歴。engine の RunState は combat を単一スロットで上書きするため UI 側で溜める
  const [runHistory, setRunHistory] = useState<readonly BattleArchive[]>([])
  // プレイ中メモ (2026-09-01): 気づきをその場で記録し、レポート書き出しに同梱する。新しいラン/戦闘の開始でリセット
  const [playNotes, setPlayNotes] = useState<readonly PlayNote[]>([])
  // 単発戦闘の評価 (ランは runHistory 側の rating で判定する)
  const [battleRated, setBattleRated] = useState<BattleRating | null>(null)
  // リプレイ・ジャーナル (2026-09-01): ランの全コマンドを記録。origin+commands で完全再現できる
  const [journal, setJournal] = useState<RunJournal | null>(null)
  // 表示中のリプレイ (nullでなければリプレイビューアを出す)
  const [replaying, setReplaying] = useState<RunJournal | null>(null)
  // 選択履歴 (2026-09-01): ピック・鍛錬・合成・購入などの意思決定を人間向けの1行で積む
  const [choiceLog, setChoiceLog] = useState<readonly RunChoice[]>([])

  // 書き出しの保険 (2026-08-30): ダウンロードもクリップボードも塞がれる環境向けに、
  // 開発者ツールのコンソールから常に最新レポートを取れる口を開けておく。
  //   copy(deckRogueReport())        ← クリップボードへ (DevToolsのcopy()はページ権限に関係なく動く)
  //   console.log(deckRogueReport()) ← 表示して手動コピー
  // さらに最新状態を localStorage に常時バックアップする (「リロードしたらログが消えた」への恒久対策)。
  //   copy(deckRogueRecoverReport()) ← リロード後・クラッシュ後でも直前の状態からレポートを復元
  // ※セーブ解禁範囲 (開発者向けの書き出しのみ・ゲーム内の中断復帰UXは作らない) の内側:
  //   これは復元プレイ用でなくプレイテストのデータ回収用で、読む口はコンソールだけ
  useEffect(() => {
    const w = window as unknown as {
      deckRogueReport?: () => string
      deckRogueRecoverReport?: () => string
    }
    w.deckRogueReport = () => buildReport(run, state, runHistory, '', playNotes, choiceLog)
    w.deckRogueRecoverReport = () => {
      try {
        const raw = localStorage.getItem('deckRogueBackup')
        if (raw === null) return '(バックアップなし)'
        const b = JSON.parse(raw) as {
          run: RunState | null
          state: GameState | null
          history: BattleArchive[]
          playNotes?: PlayNote[]
        }
        return buildReport(b.run ?? null, b.state ?? null, b.history ?? [], '', b.playNotes ?? [])
      } catch (e) {
        return `(復元失敗: ${String(e)})`
      }
    }
    try {
      // 容量超過 (QuotaExceeded) 等は握りつぶす = バックアップは保険であって本線ではない。
      // 進行が無い時は書かない (2026-09-01 修正: マウント直後や放棄後に null で上書きすると
      // リロード後の復元・「続きから」が消える — 従来この上書きでリロード復元が実は効いていなかった)
      if (run !== null || state !== null) {
        localStorage.setItem('deckRogueBackup', JSON.stringify({ run, state, history: runHistory, playNotes, journal, choices: choiceLog, fingerprint: dataFingerprint() }))
      }
    } catch {
      /* no-op */
    }
  }, [run, state, runHistory, playNotes, journal, choiceLog])

  /** セーブ復帰の共通処理 (続きから/ファイル読み込み)。データ指紋が違えば警告して選ばせる */
  const restoreRun = (r: RunState, history: readonly BattleArchive[], notes: readonly PlayNote[], fingerprint?: string, j: RunJournal | null = null, choices: readonly RunChoice[] = []): void => {
    if (fingerprint !== undefined && fingerprint !== dataFingerprint()) {
      const ok = window.confirm(
        'このセーブは別のデータバージョンで作られています。カード・敵の定義が変わっていると正しく動かない可能性がありますが、読み込みますか？',
      )
      if (!ok) return
    }
    setRunHistory(history)
    setPlayNotes(notes)
    setJournal(j)
    setChoiceLog(choices)
    setState(null)
    setConfig(null)
    setRun(r)
  }

  /** タイトル画面の「続きから」: localStorage バックアップからランを復帰 */
  const resumeFromBackup = (): void => {
    try {
      const raw = localStorage.getItem('deckRogueBackup')
      if (raw === null) return
      const b = JSON.parse(raw) as { run?: RunState | null; history?: BattleArchive[]; playNotes?: PlayNote[]; fingerprint?: string; journal?: RunJournal | null; choices?: RunChoice[] }
      if (b.run == null) return
      restoreRun(b.run, b.history ?? [], b.playNotes ?? [], b.fingerprint, b.journal ?? null, b.choices ?? [])
    } catch (e) {
      alert(`復帰に失敗しました: ${String(e)}`)
    }
  }

  /** セーブファイル (.json = sim/play.ts 互換) の読み込み */
  const loadSaveFile = (file: File): void => {
    file
      .text()
      .then((text) => {
        const sf = JSON.parse(text) as Partial<RunSaveFile> & { kind?: string }
        if (sf.kind !== 'run' || sf.run == null) {
          alert('ランのセーブファイル (kind:"run") ではありません。単発戦闘のセーブはCLI (sim/play.ts) で開けます')
          return
        }
        restoreRun(sf.run, sf.history ?? [], sf.playNotes ?? [], sf.fingerprint, sf.journal ?? null, sf.choices ?? [])
      })
      .catch((e) => alert(`読み込みに失敗しました: ${String(e)}`))
  }

  /** リプレイ (.json セーブのジャーナル) を読み込んでビューアを開く */
  const loadReplayFile = (file: File): void => {
    file
      .text()
      .then((text) => {
        const sf = JSON.parse(text) as Partial<RunSaveFile>
        if (sf.journal === undefined || sf.journal === null) {
          alert('このファイルにはリプレイ記録 (journal) がありません。記録付きのセーブは💾で書き出せます')
          return
        }
        setReplaying(sf.journal)
      })
      .catch((e) => alert(`読み込みに失敗しました: ${String(e)}`))
  }

  // 選択トースト (2026-09-01 機能フィードバック演出): ピック・鍛錬・合成・購入の結果を画面下部に一瞬出す
  const [choiceToast, setChoiceToast] = useState<{ id: number; text: string } | null>(null)
  const prevChoiceLen = useRef(0)
  useEffect(() => {
    if (choiceLog.length > prevChoiceLen.current) {
      setChoiceToast({ id: choiceLog.length, text: choiceLog[choiceLog.length - 1].text })
    }
    prevChoiceLen.current = choiceLog.length
  }, [choiceLog])

  const addNote = (text: string) => {
    const c = run?.combat ?? state
    const context = run
      ? `幕${run.act} 行${run.row + 1} ${run.phase}${run.phase === 'combat' && c ? ` T${c.turn}` : ''} ${run.battlesWon}勝 HP${run.phase === 'combat' && c ? c.player.hp : run.hp}`
      : c
        ? `単発 T${c.turn} HP${c.player.hp}`
        : 'セットアップ'
    setPlayNotes((p) => [...p, { at: new Date().toISOString(), context, text }])
  }

  const start = (cfg: Config) => {
    setConfig(cfg)
    setPlayNotes([])
    setBattleRated(null)
    // デバッグ: 自分で組んだデッキは cardId 列なので、実カードに起こして直接戦闘を開始する
    if (cfg.customDeck && cfg.customDeck.length > 0) {
      const deck = cfg.customDeck.map((id, i) => ({ uid: `custom${i}_${id}`, def: getCardDef(id) }))
      setState(
        startCombatWithOptions(cfg.seed, cfg.mode, cfg.enemyId, { deck, leaderId: cfg.leaderId }),
      )
      return
    }
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
    if (run === null) return
    let next: RunState
    try {
      next = applyRunCommand(run, command)
    } catch (err) {
      alert(err instanceof Error ? err.message : String(err))
      return
    }
    // リプレイ記録 (成功したコマンドだけ。journal が無いラン=旧セーブ由来は記録しない)
    setJournal((j) => (j === null ? j : { ...j, commands: [...j.commands, command] }))
    // 選択履歴 (戦闘外の意思決定だけが1行になる)
    {
      const line = describeRunChoice(run, command, next)
      if (line !== null) setChoiceLog((c) => [...c, line])
    }
    // 戦闘が決着した瞬間だけ履歴に積む (次戦の開始で combat が上書きされる前に捕まえる)。
    // setRun の更新関数の中で setRunHistory を呼ぶと StrictMode の二重実行で重複するため、外で行う
    const ended = next.combat?.phase === 'won' || next.combat?.phase === 'lost'
    if (ended && run.combat && run.combat.phase !== next.combat?.phase && next.combat) {
      // 敵IDはノードでなく戦闘ログから取る (2026-08-30 修正: ?マス発の戦闘はノードが
      // encounterId を持たず 'unknown' がアーカイブされ、レポート生成が
      // encounterName('unknown') で例外死していた = 「書き出しがうまくいかない」の真犯人)
      const started = next.combat.eventLog.find((e) => e.type === 'CombatStarted')
      const archived = archiveBattle(
        next.combat,
        run.battlesWon + 1,
        (started?.type === 'CombatStarted' ? started.enemyId : null) ??
          currentNode(run)?.encounterId ??
          'unknown',
        run.currentElite,
        run.hp,
        run.deck.length,
        { act: run.act, boss: currentNode(run)?.type === 'boss' },
      )
      // 同じ戦闘を二度積まない (再入や二重実行への保険)
      setRunHistory((h) => (h.some((a) => a.battleNo === archived.battleNo) ? h : [...h, archived]))
    }
    setRun(next)
  }

  if (replaying !== null) {
    return (
      <ReplayScreen
        journal={replaying}
        onExit={() => setReplaying(null)}
        onTakeover={(stateAt, truncated, choices) => {
          setRunHistory([])
          setPlayNotes([])
          setJournal(truncated)
          setChoiceLog(choices)
          setState(null)
          setConfig(null)
          setRun(stateAt)
          setReplaying(null)
        }}
      />
    )
  }
  if (run !== null) {
    return (
      <>
      <RunScreen
        run={run}
        dispatch={dispatchRun}
        history={runHistory}
        notes={playNotes}
        journal={journal}
        choices={choiceLog}
        onReplay={journal !== null ? () => setReplaying(journal) : undefined}
        onExit={() => {
          setRun(null)
          setRunHistory([])
        }}
        onRestart={(seed) => {
          setRunHistory([])
          setPlayNotes([])
          setChoiceLog([])
          setJournal(run !== null ? { origin: { kind: 'run', seed, leaderId: run.leaderId, difficulty: run.difficulty ?? DEFAULT_DIFFICULTY }, commands: [] } : null)
          // 難易度はリスタートでも引き継ぐ (旧セーブ由来の欠落は既定3)
          setRun((prev) => createRun(seed, ADOPTED_MODE, prev?.leaderId ?? 'leader_green', undefined, prev?.difficulty ?? DEFAULT_DIFFICULTY))
        }}
      />
      <NoteBar count={playNotes.length} onAdd={addNote} />
      <HotkeyClicker />
      {choiceToast !== null && (
        <div key={choiceToast.id} className="toast-choice">📜 {choiceToast.text}</div>
      )}
      {(() => {
        // 戦闘直後の評価入力 (2026-09-01)。決着直後のフェーズの間は出続け、点数の後からメモも追記できる
        const last = runHistory[runHistory.length - 1]
        const show = last !== undefined && ['reward', 'relic-reward', 'won', 'lost'].includes(run.phase)
        if (!show) return null
        let name = last.enemyId
        try {
          name = encounterName(last.enemyId)
        } catch {
          /* 未知IDは生のまま */
        }
        const patchRating = (patch: BattleRating) =>
          setRunHistory((h) => h.map((a, i) => (i === h.length - 1 ? { ...a, rating: { ...a.rating, ...patch } } : a)))
        return (
          <BattleRatingBar
            key={last.battleNo}
            label={`${last.battleNo}戦目 ${name}`}
            rated={last.rating ?? null}
            onRate={patchRating}
            onNote={(note) => patchRating({ note })}
            lost={last.result === 'lost'}
            onLossFeel={(feel) => patchRating({ lossFeel: feel })}
          />
        )
      })()}
      </>
    )
  }
  if (state === null || config === null) {
    let resume: { label: string; onResume: () => void } | null = null
    try {
      const raw = localStorage.getItem('deckRogueBackup')
      const b = raw !== null ? (JSON.parse(raw) as { run?: RunState | null }) : null
      if (b?.run != null) {
        const r = b.run
        const leaderName = allLeaders.find((l) => l.id === r.leaderId)?.name ?? r.leaderId
        resume = {
          label: `${leaderName} 幕${r.act} 行${r.row + 1} / HP${r.hp}/${r.maxHp} / ${r.battlesWon}勝 / 🎚${r.difficulty ?? 3}`,
          onResume: resumeFromBackup,
        }
      }
    } catch {
      /* 壊れたバックアップは無視 */
    }
    return <SetupScreen onStart={start} resume={resume} onLoadSave={loadSaveFile} onLoadReplay={loadReplayFile} onStartCheckpoint={(opts) => {
        setRunHistory([])
        setPlayNotes([])
        setChoiceLog([])
        setJournal({ origin: { kind: 'checkpoint', seed: opts.seed, leaderId: opts.leaderId, checkpoint: { act: opts.act, deckId: opts.deckId, relicIds: opts.relicIds, hpRatio: opts.hpRatio, gold: opts.gold, difficulty: opts.difficulty } }, commands: [] })
        setRun(createDebugCheckpointRun(opts.seed, ADOPTED_MODE, opts.leaderId, opts))
      }} onStartRun={(seed, leaderId, runDeckId, difficulty, revealIntents, setAnyCards) => {
        setRunHistory([])
        setPlayNotes([])
        setChoiceLog([])
        setJournal({ origin: { kind: 'run', seed, leaderId, deckId: runDeckId, difficulty, ...(revealIntents ? { revealIntents: true } : {}), ...(setAnyCards ? { setAnyCards: true } : {}) }, commands: [] })
        setRun(createRun(seed, ADOPTED_MODE, leaderId, runDeckId, difficulty, { ...(revealIntents ? { revealIntents: true } : {}), ...(setAnyCards ? { setAnyCards: true } : {}) }))
      }} />
  }
  return (
    <>
    <BattleScreen
      onExport={() => saveReport(null, state, [], playNotes)}
      state={state}
      config={config}
      dispatch={dispatch}
      onRestart={(seed) => start({ ...config, seed })}
      onBack={() => {
        setState(null)
        setConfig(null)
      }}
    />
    <NoteBar count={playNotes.length} onAdd={addNote} />
    <HotkeyClicker />
    {(state.phase === 'won' || state.phase === 'lost') && (
      <BattleRatingBar
        label="この戦闘"
        rated={battleRated}
        onRate={(r) => {
          addNote(`[戦闘評価] 強さ${r.strength} 面白さ${r.fun}${(r.note ?? '') !== '' ? ` — ${r.note}` : ''}`)
          setBattleRated(r)
        }}
        onNote={(note) => addNote(`[戦闘メモ] ${note}`)}
        lost={state.phase === 'lost'}
        onLossFeel={(feel) => addNote(`[敗因] ${feel === 'build' ? '構築の失敗' : '理不尽'}`)}
      />
    )}
    </>
  )
}

/**
 * 戦闘直後の5段階評価バー (2026-09-01 ユーザー要望)。強さ・面白さの両方を選んだ瞬間に記録され、
 * ひとことメモ (同日追補「フィードバックメモなくない？」) はEnterでいつでも保存/追記できる。
 * 入力は任意 — 次のノードへ進めば消える (評価しない自由)。記録はレポートの戦闘履歴テーブルに列で出る
 */
function BattleRatingBar({
  label,
  rated,
  onRate,
  onNote,
  lost,
  onLossFeel,
}: {
  label: string
  /** すでに記録済みの評価 (この戦闘のフェーズ内なら表示してメモ追記を受け付ける) */
  rated: BattleRating | null
  onRate: (r: BattleRating) => void
  onNote: (note: string) => void
  /** 敗北時だけ出る「敗因の感触」2択 (2026-09-02 作り直し基準の入力: 構築の失敗 / 理不尽) */
  lost?: boolean
  onLossFeel?: (feel: 'build' | 'unfair') => void
}) {
  const [lossFeel, setLossFeel] = useState<'build' | 'unfair' | null>(rated?.lossFeel ?? null)
  const [strength, setStrength] = useState<number | null>(rated?.strength ?? null)
  const [fun, setFun] = useState<number | null>(rated?.fun ?? null)
  const [note, setNote] = useState(rated?.note ?? '')
  const [flash, setFlash] = useState(false)
  const saveNote = () => {
    if (note.trim() === '') return
    onNote(note.trim())
    setFlash(true)
    setTimeout(() => setFlash(false), 800)
  }
  const pick = (kind: 'strength' | 'fun', n: number) => {
    const st = kind === 'strength' ? n : strength
    const fn = kind === 'fun' ? n : fun
    if (kind === 'strength') setStrength(n)
    else setFun(n)
    if (st !== null && fn !== null) onRate({ strength: st, fun: fn, ...(note.trim() !== '' ? { note: note.trim() } : {}) })
  }
  const committed = rated !== null && rated.strength !== undefined && rated.fun !== undefined
  // 入力導線 (2026-09-02 人間ラン#2: 21戦とも未入力=主観データ0への処方)。任意のまま、未入力の間だけ一言促す
  const nudge = !committed ? <span style={{ fontSize: 11, color: 'var(--accent, #fc6)' }}>◀ 次へ進む前に1タップ（任意・調整の材料になります）</span> : null
  const row = (title: string, kind: 'strength' | 'fun', val: number | null) => (
    <span style={{ display: 'inline-flex', gap: 2, alignItems: 'center', fontSize: 12 }}>
      {title}
      {[1, 2, 3, 4, 5].map((n) => (
        <button
          key={n}
          className="chip chip-btn"
          style={{ minWidth: 22, padding: '1px 5px', background: val === n ? 'rgba(120,160,255,0.45)' : undefined }}
          onClick={() => pick(kind, n)}
        >
          {n}
        </button>
      ))}
    </span>
  )
  return (
    <div
      style={{
        position: 'fixed', left: 12, bottom: 12, zIndex: 60,
        background: 'rgba(18,22,30,0.94)', border: '1px solid #445',
        borderRadius: 8, padding: '6px 10px', display: 'flex', gap: 10, alignItems: 'center', flexWrap: 'wrap',
      }}
      title="点数は両方選ぶと記録、メモはEnterで保存 (任意入力。レポートの戦闘履歴に載る)"
    >
      <span style={{ fontSize: 12 }}>⚔️ {label} の評価{committed ? ' ✅' : ''}</span>
      {row('強さ', 'strength', strength)}
      {row('面白さ', 'fun', fun)}
      {nudge}
      {lost && (
        <span style={{ display: 'inline-flex', gap: 2, alignItems: 'center', fontSize: 12 }} title="負けた理由の感触。理不尽が独立2本一致したら数値でなく構造を作り直す（balance-policy.md）">
          敗因
          {(['build', 'unfair'] as const).map((f) => (
            <button
              key={f}
              className="chip chip-btn"
              style={{ padding: '1px 6px', background: lossFeel === f ? 'rgba(255,120,120,0.45)' : undefined }}
              onClick={() => {
                setLossFeel(f)
                onLossFeel?.(f)
              }}
            >
              {f === 'build' ? '構築の失敗' : '理不尽'}
            </button>
          ))}
        </span>
      )}
      <input
        value={note}
        onChange={(e) => setNote(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter') saveNote()
        }}
        placeholder={flash ? '✅ 保存しました' : 'ひとことメモ (Enterで保存)'}
        style={{ width: 190, fontSize: 12 }}
      />
    </div>
  )
}

/**
 * プレイ中メモの入力バー (2026-09-01 ユーザー要望「気がついたことが揮発せずにいい」)。
 * 画面右下に常駐し、Enterで記録。記録は文脈 (幕/行/ターン/HP) 付きでレポート書き出しに同梱される
 */
/**
 * リプレイビューア (2026-09-01 ユーザー要望「ログからのリプレイ機能」)。
 * ジャーナル (開始条件+全コマンド) を再実行した状態列を1手/戦闘単位でシークする。
 * 盤面は本物の RunScreen を読み取り専用 (pointer-events: none) で再利用 = 表示の二重実装なし。
 * 「ここから再開」でその地点から操作を引き継げる (以降のリプレイは破棄 = 実質の巻き戻し)
 */
function ReplayScreen({
  journal,
  onExit,
  onTakeover,
}: {
  journal: RunJournal
  onExit: () => void
  onTakeover: (state: RunState, truncated: RunJournal, choices: readonly RunChoice[]) => void
}) {
  const replay = useMemo(() => replayStates(journal), [journal])
  const states = replay.states
  const [idx, setIdx] = useState(0)
  const [showChoices, setShowChoices] = useState(true)
  const cur = Math.max(0, Math.min(idx, states.length - 1))
  const run = states[cur]
  // 選択履歴 (2026-09-01 「リプレイで見直すのだからユーザー選択が分かるように」): クリックでその地点へ
  const choiceEntries = useMemo(() => {
    const out: { index: number; choice: RunChoice }[] = []
    for (let i = 0; i < journal.commands.length && i + 1 < states.length; i++) {
      const c = describeRunChoice(states[i], journal.commands[i], states[i + 1])
      if (c !== null) out.push({ index: i + 1, choice: c })
    }
    return out
  }, [states, journal])
  const battleStarts = useMemo(() => {
    const out: { index: number; label: string }[] = []
    for (let i = 1; i < states.length; i++) {
      if (states[i].phase === 'combat' && states[i - 1].phase !== 'combat') {
        out.push({ index: i, label: `${states[i].battlesWon + 1}戦目` })
      }
    }
    return out
  }, [states])
  const clamp = (n: number) => Math.max(0, Math.min(states.length - 1, n))
  const prevBattle = () => {
    const b = [...battleStarts].reverse().find((x) => x.index < cur)
    setIdx(b !== undefined ? b.index : 0)
  }
  const nextBattle = () => {
    const b = battleStarts.find((x) => x.index > cur)
    setIdx(b !== undefined ? b.index : states.length - 1)
  }
  return (
    <>
      <div
        style={{
          position: 'fixed', top: 0, left: 0, right: 0, zIndex: 80,
          background: 'rgba(13,16,24,0.97)', borderBottom: '1px solid #556',
          padding: '6px 12px', display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap',
        }}
      >
        <b>🎬 リプレイ</b>
        <span className="choice-desc">{cur}/{states.length - 1}手・{battleStarts.filter((b) => b.index <= cur).length}戦目</span>
        <button className="btn" onClick={() => setIdx(0)}>⏮</button>
        <button className="btn" onClick={prevBattle}>◀◀ 戦闘</button>
        <button className="btn" onClick={() => setIdx(clamp(cur - 1))}>◀ 1手</button>
        <button className="btn" onClick={() => setIdx(clamp(cur + 1))}>1手 ▶</button>
        <button className="btn" onClick={nextBattle}>戦闘 ▶▶</button>
        <button className="btn" onClick={() => setIdx(states.length - 1)}>⏭</button>
        <input
          type="range"
          min={0}
          max={states.length - 1}
          value={cur}
          onChange={(e) => setIdx(Number(e.target.value))}
          style={{ flex: 1, minWidth: 120 }}
        />
        <button className="btn" onClick={() => setShowChoices((v) => !v)}>📜 選択{showChoices ? 'を隠す' : `（${choiceEntries.length}）`}</button>
        <button
          className="btn btn-primary"
          onClick={() => {
            if (window.confirm('この地点から操作を引き継ぎますか？（以降のリプレイは破棄され、ここからの続きが新しい記録になります）')) {
              onTakeover(
                run,
                { origin: journal.origin, commands: journal.commands.slice(0, cur) },
                choiceEntries.filter((e) => e.index <= cur).map((e) => e.choice),
              )
            }
          }}
        >
          ▶ ここから再開
        </button>
        <button className="btn" onClick={onExit}>✕ 閉じる</button>
        {replay.error !== null && <span style={{ color: 'var(--warn, #e0a458)', fontSize: 12 }}>{replay.error}</span>}
      </div>
      {showChoices && (
        <div
          style={{
            position: 'fixed', top: 52, right: 8, bottom: 8, width: 330, zIndex: 75,
            background: 'rgba(13,16,24,0.96)', border: '1px solid #556', borderRadius: 8,
            padding: 8, overflowY: 'auto',
          }}
        >
          <b style={{ fontSize: 13 }}>📜 選択履歴（クリックでその地点へ）</b>
          {choiceEntries.map((e, i) => {
            const isPast = e.index <= cur
            const isCurrent = isPast && (i === choiceEntries.length - 1 || choiceEntries[i + 1].index > cur)
            return (
              <div
                key={i}
                onClick={() => setIdx(e.index)}
                style={{
                  fontSize: 12, padding: '3px 4px', borderRadius: 4, cursor: 'pointer',
                  opacity: isPast ? 1 : 0.45,
                  background: isCurrent ? 'rgba(120,160,255,0.22)' : undefined,
                }}
              >
                <span style={{ opacity: 0.6 }}>[{e.choice.at}]</span> {e.choice.text}
              </div>
            )
          })}
          {choiceEntries.length === 0 && <p className="hint">（選択の記録なし）</p>}
        </div>
      )}
      <div style={{ paddingTop: 52, paddingRight: showChoices ? 346 : 0, pointerEvents: 'none' }}>
        <RunScreen run={run} dispatch={() => {}} history={[]} notes={[]} onExit={() => {}} onRestart={() => {}} />
      </div>
    </>
  )
}

/**
 * キーボードショートカット (2026-09-01 ユーザー要望「UI側もうちょい遊びやすく」)。
 * data-hotkey 属性の付いた要素をクリックする方式 = 活性判定・ガードはボタン側の disabled が
 * そのまま効く (canPlay 等のロジックを二重化しない)。文脈ごとに num-N の意味が変わる
 * (手札プレイ/対象選択/発動候補/報酬ピック) が、同時に存在しないので衝突しない
 */
function HotkeyClicker() {
  useEffect(() => {
    const onKey = (ev: KeyboardEvent) => {
      if (ev.ctrlKey || ev.metaKey || ev.altKey || ev.repeat) return
      const tag = (document.activeElement?.tagName ?? '').toLowerCase()
      if (tag === 'input' || tag === 'textarea' || tag === 'select') return
      const key = ev.key.toLowerCase()
      const click = (sel: string): boolean => {
        const el = document.querySelector<HTMLElement>(`[data-hotkey="${sel}"]`)
        if (el === null) return false
        el.click()
        return true
      }
      let hit = false
      if (key >= '1' && key <= '9') hit = click(`num-${key}`)
      else if (key === 'e') hit = click('end-turn')
      else if (key === 'f') hit = click('num-1') // 発動候補が1つの時の速記
      else if (key === 'h') hit = click('hold')
      else if (key === 's') hit = click('skip')
      else if (key === 'escape') hit = click('cancel')
      if (hit) ev.preventDefault()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])
  return null
}

function NoteBar({ count, onAdd }: { count: number; onAdd: (text: string) => void }) {
  const [text, setText] = useState('')
  const [flash, setFlash] = useState(false)
  const submit = () => {
    const t = text.trim()
    if (t === '') return
    onAdd(t)
    setText('')
    setFlash(true)
    setTimeout(() => setFlash(false), 800)
  }
  return (
    <div
      style={{
        position: 'fixed', right: 12, bottom: 12, zIndex: 60,
        background: 'rgba(18,22,30,0.94)', border: '1px solid #445',
        borderRadius: 8, padding: '6px 8px', display: 'flex', gap: 6, alignItems: 'center',
      }}
    >
      <span title="レポート書き出し(📄)に「プレイメモ」として同梱されます">📝{count > 0 ? count : ''}</span>
      <input
        value={text}
        onChange={(e) => setText(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter') submit()
        }}
        placeholder={flash ? '✅ 記録しました' : '気づきメモ (Enterで記録)'}
        style={{ width: 200 }}
      />
    </div>
  )
}

/** 工房: 異なる2枚を選んで合成する (確定済みルール表「カード合成（工房）」) */
function WorkshopScreen({
  run,
  dispatch,
  ctx,
}: {
  run: RunState
  dispatch: (c: RunCommand) => void
  ctx?: EffectCtx
}) {
  const [selected, setSelected] = useState<number[]>([])
  const toggle = (i: number) =>
    setSelected((prev) =>
      prev.includes(i) ? prev.filter((x) => x !== i) : prev.length < 2 ? [...prev, i] : prev,
    )
  const a = selected.length === 2 ? run.deck[selected[0]] : null
  const b = selected.length === 2 ? run.deck[selected[1]] : null
  const reason = a && b ? fuseBlockReason(a, b) : null
  const preview = a && b && reason === null ? fuseCards(a, b) : null
  return (
    <div className="app setup">
      <h1>🔨 工房</h1>
      <p className="hint">
        デッキの2枚を選んで合成する（同名2枚は「真・」強化版になる）。素材2枚は消え、合成された1枚がデッキに入る（圧縮と強化が同時）。
        タイプの違う2枚も可 — 結果は持続する側（置物＞リアクション＞呪文＞物理）になり、置物化は量÷3で毎ターン化する。
        コストはVP査定からの逆算（素材コストの単純合算ではない）。特定の組み合わせは手書きレシピ⭐にヒットし、計算値より少し強い一品になる。
      </p>
      {preview && (
        <div className="panel">
          <div className="setup-section-title">
            {preview.id.startsWith('fusion_') ? '⭐ レシピ発見！ ' : ''}
            合成結果プレビュー{preview.exhaust ? '（消滅つき）' : ''}
          </div>
          <div className="hand-cards" style={{ marginTop: 8 }}>
            <CardFrame card={{ uid: 'fusion_preview', def: preview }} dim={false} ctx={ctx} actions={null} />
          </div>
        </div>
      )}
      {a && b && reason && (
        <div className="panel">
          <span className="chip">⚠ {reason}</span>
        </div>
      )}
      <div className="hand-cards" style={{ margin: '12px 0' }}>
        {run.deck.map((c, i) => (
          <CardFrame
            key={c.uid}
            card={c}
            dim={selected.length === 2 && !selected.includes(i)}
            ctx={ctx}
            actions={
              <button className={selected.includes(i) ? 'btn btn-primary' : 'btn'} onClick={() => toggle(i)}>
                {selected.includes(i) ? '選択中' : '選ぶ'}
              </button>
            }
          />
        ))}
      </div>
      <div className="choice-desc" style={{ margin: '6px 0' }}>
        合成1回 {workshopFusePrice(run)}G（所持 {run.gold}G）
        {run.gold < workshopFusePrice(run) ? '＝ゴールド不足で合成できません' : ''}
      </div>
      <button
        className="btn btn-primary"
        disabled={!preview || run.gold < workshopFusePrice(run)}
        onClick={() =>
          dispatch({ type: 'WorkshopFuse', indexA: selected[0], indexB: selected[1] })
        }
      >
        合成する（{workshopFusePrice(run)}G）
      </button>{' '}
      <button className="btn" onClick={() => dispatch({ type: 'WorkshopSkip' })}>
        見送る
      </button>
    </div>
  )
}

/** 焚き火プレビュー: 鍛えた後の姿を1行で (コスト変更ならコストを、量/単位なら効果行を出す) */
function describeUpgrade(card: CardInstance): string {
  const after = upgradeCard(card)
  if (after.def.cost !== card.def.cost) {
    return `コスト ${card.def.cost}E → ${after.def.cost}E（効果は据え置き）`
  }
  return effectLineStrings(after.def).join(' / ')
}
