// ui/ は状態を読んでコマンドを投げるだけの薄い層。ゲームロジックを書かない (CLAUDE.md)。
// 見た目は静的なゲーム風UI (StS風配置・ダーク)。動く演出はやらない (CLAUDE.md「UIの見た目の方針」)。
import { useEffect, useRef, useState } from 'react'
import type { ReactElement } from 'react'
import {
  archiveBattle,
  cardName,
  inflictSuffix,
  intentText,
  logLine,
  buildReport,
  saveReport,
  STATUS_LABEL,
  type BattleArchive,
  type LogLine,
} from './report.ts'
import {
  allCards,
  allDecks,
  allEncounters,
  allEnemies,
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
import {
  BLAZE_THRESHOLD,
  cardNeedsTarget,
  playerCanSet,
  effectiveIntent,
  reactionMatches,
  setBranchFlipRisks,
  windowFromPending,
  effectiveCost,
  isDamageEffect,
  isPlayableFromHand,
} from '../engine/effects.ts'
import { playableReactions } from '../engine/reactions/hold-manual.ts'
import { getReactionSystem } from '../engine/reactions/index.ts'
import { applyRunCommand, canUpgradeCard, createRun, currentNode, eventChoiceNeedsCard, isUpgraded, nextChoices, shopRemovalPrice, shopUpgradePrice, upgradeCard } from '../engine/run.ts'
import { battleSummary, cardCostLabel, summaryLine, xHitsSuffix } from '../engine/summary.ts'
import { BOSS_ROW, MAP_ROWS } from '../engine/map.ts'
import type { MapNode, MapNodeType } from '../engine/map.ts'
import { fuseBlockReason, fuseCards } from '../engine/fusion.ts'
import type { RunCommand, RunState } from '../engine/run.ts'
import { applyCommand, createInitialState } from '../engine/state.ts'
import { startCombatWithOptions } from '../engine/combat.ts'
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
  thorned: 'とげ型（攻撃ヒットごとに反射）',
  thief: '盗人型（盗んで逃げる）',
  bomber: '爆弾型（三拍子の大爆発）',
  healer: '回復役型（味方を癒す）',
  windup: '息切れ型（大技のあと隙）',
  shell: '甲殻型（積みながら殴る）',
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
  thorned: '🦔',
  thief: '🪙',
  bomber: '🧨',
  healer: '🌿',
  windup: '🪓',
  shell: '🪨',
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

/** キーワード能力の用語解説 (カーソルを当てると吹き出しで表示) */
const KEYWORD_HELP: Record<string, string> = {
  憤怒: '赤の受け: 被弾を次の攻撃の燃料に変える（例: 被攻撃後に勢い+2、受けたダメージ×2で殴り返す）',
  爆熱: '対象の延焼×Nのダメージを与え、延焼を全て失わせる。継続ダメージと焼き切りを手放してバーストに換金する',
  処刑: '対象のHPが最大の25%以下なら、ダメージが跳ね上がる',
  弱体: '与えるダメージが25%減る（切り捨て）。自分のターン終了時に1減る',
  脆弱: '敵の攻撃で受けるダメージが50%増える（切り捨て）。敵の行動フェーズ終了時に1減る',
  負傷: '使えない死に札。手札に来ても何もできず、ターン終了時に捨てられる（1戦闘で最大5枚まで）',
  再生: '敵フェーズ終了時にHPが回復する。HP半分以下になると止まる',
  激昂: '自動で強化が増えるタイマー。「/T」は敵フェーズごと、「/N枚プレイ」はカードをN枚プレイするたび',
  混乱: '混乱した敵の攻撃は、プレイヤーでなく他の生存敵（いなければ自分自身）に向かう。攻撃1回ごとに1減る',
  急所: 'その敵が次に受けるダメージN回が+50%（切り捨て）。1回ダメージを与えるごとに1減る',
  威圧: '敵の強化を下げる（攻撃の実値と幅表示が下がる。攻撃は最低1）',
  応援: '味方全体の強化を増やす。応援役を先に倒すか、無視して本体を叩くかの選択',
  とげ: '攻撃ヒット1回ごとに反射ダメージを受ける（ブロックで防げる）。そのヒットで倒せば反射しない',
  従者狩り: '敵が召喚トークンまたは従者（生き物の置物）1体をランダムに破壊する。道具・オーラ系の置物・リーダーの能力・レリックは対象外',
  延焼耐性: 'この敵の延焼は毎フェーズ追加で減っていく（バーンが効きにくい）',
  貫通: '敵のブロックを無視してダメージを与える（トランプル）',
  勢い: 'このターンの以降の攻撃ダメージに加算。自分のターン終了時に0に戻る',
  成長: 'この戦闘の間、与えるダメージすべてに加算される（戦闘ごとにリセット）',
  消滅: '使用後、この戦闘から取り除かれる（再シャッフルされない）。消滅置き場は黒の墓地参照・回収の燃料になる',
  消滅コスト: 'プレイするために手札をN枚消滅させる。捨てより重いが、消滅置き場の燃料が増える',
  墓地: '消滅置き場のこと。忘却・消滅コスト・消滅札・衝動の失効で増え、戦闘中は減らない（回収を除く）',
  忘却の刻: '消滅置き場が7枚以上のとき、このカードの効果が強化される。屍集めや死者再生で墓地を使うと刻が割れることもある',
  ドレイン: 'ダメージを与え、その半分（切り捨て）だけHPが回復する（黒の専売）',
  直接プレイ: '消滅置き場のカードをコストを支払わずプレイする。プレイ後もカードは消滅置き場に残る（置物は場に出る）',
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
  onHealed: 'HPが回復するたび: ',
  onHpLost: 'カード効果でHPを失うたび: ',
  onCardExhausted: 'カードが消滅するたび: ',
  onCostExhausted: '消滅コストを支払うたび: ',
  onPermanentEntered: '置物が場に出るたび: ',
  onImpulsePlayed: '衝動カードをプレイするたび: ',
  onRandomPlayed: '運任せの札をプレイするたび: ',
  onAetherGained: '霊気を得るたび: ',
  onCardSet: 'カードを伏せるたび: ',
  onReactionFired: 'リアクションが発動するたび: ',
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
  return parts.length > 0 ? `[${parts.join('かつ')}] ` : ''
}

function ctx2Block(e: DeclarativeEffect, _ctx: EffectCtx | undefined, trigger: string, pierce: string): string {
  return `${trigger}⚔️ ブロック×${e.amount}ダメージ${pierce}`
}

/** 効果1つを1行のテキストに変換する。忘却の刻 (しきい値) は達成状態を添えて表示する */
function renderEffectItem(e: DeclarativeEffect, ctx?: EffectCtx, holderType?: string): string {
  const t = e.exhaustThreshold
  if (t !== undefined) {
    const met = ctx !== undefined && ctx.exhausted >= t
    const shown = met ? { ...e, amount: e.amountMax } : e
    const note = met ? `〔忘却の刻${t}: 発動中⚡〕` : `〔忘却の刻${t}: ${e.amountMax}に強化〕`
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
    case 'dealDamage':
      return `${trigger}⚔️ ${aoe}${(e.amount ?? 0) + atkBonus}ダメージ${pierce}${xHitsSuffix(e)}${atkBreak}`
    case 'dealDamagePerEnergyMax':
      return ctx
        ? `${trigger}エナジー上限×${e.amount}ダメージ${pierce} [現在${(e.amount ?? 0) * ctx.energyMax + atkBonus}]`
        : `${trigger}エナジー上限×${e.amount}ダメージ${pierce}`
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
      return `${trigger}🔁 反復+${e.amount}（次に唱える呪文の効果を2回解決。自ターン終了時に消える）`
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
    case 'dealDamagePerExhaust':
      return ctx
        ? `${trigger}⚔️ 消滅した枚数×${e.amount}ダメージ${pierce} [現在${(e.amount ?? 0) * ctx.exhausted + atkBonus}]`
        : `${trigger}⚔️ 消滅した枚数×${e.amount}ダメージ${pierce}`
    case 'dealDamageDrainPerExhaust':
      return ctx
        ? `${trigger}🩸 消滅した枚数×${e.amount}ダメージ+半分回復 [現在${(e.amount ?? 0) * ctx.exhausted + atkBonus}]`
        : `${trigger}🩸 消滅した枚数×${e.amount}ダメージ+半分回復`
    case 'dealDamagePerSelfHpLost':
      return ctx
        ? `${trigger}⚔️ この戦闘でカード効果により失ったHP×${e.amount}ダメージ${pierce} [現在${(e.amount ?? 0) * ctx.selfHpLost + atkBonus}]`
        : `${trigger}⚔️ この戦闘でカード効果により失ったHP×${e.amount}ダメージ${pierce}`
    case 'retrieveFromExhaust':
      return `${trigger}⚰️ 消滅置き場からカード${e.amount ?? 1}枚を選んで手札に戻す`
    case 'playFromExhaust':
      return `${trigger}⚰️ 消滅置き場のカード1枚（リアクション以外）をコストを支払わず直接プレイ（そのカードは消滅置き場に残る）`
    case 'summonPermanent':
      return `${trigger}🏳️ ${cardName(e.summonId ?? '')}トークンを${e.amount ?? 1}体場に出す`
    case 'dischargeBurn':
      return `${trigger}💥 爆熱: 対象の延焼×${e.amount}ダメージを与え、延焼を全て失わせる`
    case 'shatterBlockConvert':
      return `${trigger}🔨 敵のブロックを全て破壊し、破壊した値と同じダメージ`
    case 'dealDamageExecute':
      return `${trigger}⚔️ ${e.amount}ダメージ（対象HPが25%以下なら${e.amountMax}）`
    case 'dealDamagePerIceBlock':
      return ctx
        ? `${trigger}⚔️ 現在の氷壁×${e.amount}ダメージ [現在${(e.amount ?? 0) * ctx.iceBlock + atkBonus}]`
        : `${trigger}⚔️ 現在の氷壁×${e.amount}ダメージ`
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
      return `${trigger}⚔️ 対象の下げられた強化×${e.amount}の追加ダメージ（威圧の換金）`
    case 'gainBlockPerEnergyMax':
      return ctx
        ? `${trigger}🛡 エナジー上限×${e.amount}ブロック [現在${(e.amount ?? 0) * ctx.energyMax}]`
        : `${trigger}🛡 エナジー上限×${e.amount}ブロック`
    case 'gainBlockPerExhaust':
      return ctx
        ? `${trigger}🛡 消滅した枚数×${e.amount}ブロック [現在${(e.amount ?? 0) * ctx.exhausted}]`
        : `${trigger}🛡 消滅した枚数×${e.amount}ブロック`
    case 'gainBlockPerPermanent':
      return ctx
        ? `${trigger}🛡 置物の数×${e.amount}ブロック [現在${(e.amount ?? 0) * ctx.permanents}]`
        : `${trigger}🛡 置物の数×${e.amount}ブロック`
    case 'weakenEnemy':
      return `${trigger}${aoe}威圧${e.amount}（敵の強化-${e.amount}）`
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
  const lines: string[] = []
  if ((def.discardCost ?? 0) > 0) lines.push(`追加コスト: 手札${def.discardCost}枚を捨てる`)
  if ((def.exhaustCost ?? 0) > 0) lines.push(`追加コスト: 手札${def.exhaustCost}枚を消滅させる`)
  if (def.modes && def.modes.length > 0) {
    def.modes.forEach((m, i) => lines.push(`選択${i + 1}: ${effectItems(m.effects, ctx).join('、')}`))
  } else {
    lines.push(...effectItems(def.effects, ctx, def.type))
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
  const cond = intent.conditionalOn === 'set' ? '伏せ札あり' : '従者あり'
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
      return `🛡️ 防御 ${intent.actual}（宣言 ${intent.shownMin}〜${intent.shownMax}）`
    case 'destroy-set':
      return '💥 伏せ破壊'
    case 'destroy-token':
      return '🪓 従者狩り'
    case 'buff':
      return `💪 強化 +${intent.actual}（宣言 +${intent.shownMin}〜+${intent.shownMax}）`
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

function SetupScreen({
  onStart,
  onStartRun,
}: {
  onStart: (cfg: Config) => void
  onStartRun: (seed: number, leaderId: string, runDeckId?: string) => void
}) {
  const [enemyId, setEnemyId] = useState(allEnemies[0].id)
  const [leaderId, setLeaderId] = useState(allLeaders[0].id)
  const leader = getLeaderDef(leaderId)
  const allowedDecks = allDecks.filter((d) => deckAllowedForLeader(leader, d))
  const [deckId, setDeckId] = useState(allowedDecks[0].id)
  const [seedInput, setSeedInput] = useState('')
  // デバッグ枠 (2026-08-30 ユーザー要望): 自分で組んだデッキと合成ラボ。既定は畳んでおく
  const [customDeck, setCustomDeck] = useState<readonly string[]>([])
  const [showDebug, setShowDebug] = useState(false)
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
        <div className="choice-title">🏕 ドラフト連戦（マップラン・ボスまで16行）</div>
        <div className="choice-desc">
          {leader.name}の基本10枚から出発し、勝利ごとに{leader.colors.map((c) => COLOR_LABEL[c]).join('')}
          の{leader.rewardChoices}枚から1枚ピックして構築。敵は段階制でだんだん強くなり、HPは持ち越し。
        </div>
        {(leader.runDeckChoices ?? [leader.runDeckId]).length > 1 ? (
          <div className="choice-row" style={{ marginTop: 8 }}>
            {(leader.runDeckChoices ?? []).map((deckId) => {
              const deck = allDecks.find((d) => d.id === deckId)
              return (
                <button
                  key={deckId}
                  className="choice"
                  onClick={() => onStartRun(parseSeed(), leaderId, deckId)}
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
            onClick={() => onStartRun(parseSeed(), leaderId)}
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
        ── 開発者ツール ──{' '}
        <button className="btn" onClick={() => setShowDebug((v) => !v)}>
          {showDebug ? '閉じる' : '開く'}
        </button>
      </div>
      {showDebug && (
        <>
          <DeckBuilder colors={leader.colors} deck={customDeck} setDeck={setCustomDeck} />
          <FusionLab />
        </>
      )}
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
  onExport,
  extraChip,
  backLabel,
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
  // StS式ターゲティング: 単体対象カードのプレイ時、敵タップ待ちの状態
  const [pendingTarget, setPendingTarget] = useState<{
    cardUid: string
    modeIndex?: number
    discardUids?: string[]
    exhaustUids?: string[]
    retrieveUid?: string
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
  ) => {
    const card = player.hand.find((c) => c.uid === cardUid)
    if (card && aliveCount > 1 && cardNeedsTarget(card, modeIndex)) {
      setPendingTarget({ cardUid, modeIndex, discardUids, exhaustUids, retrieveUid })
    } else {
      dispatch({ type: 'PlayCard', cardUid, modeIndex, discardUids, exhaustUids, retrieveUid })
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
    playOrTarget(cardUid, modeIndex)
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
                    exhaustUids: activeTarget.exhaustUids,
                    retrieveUid: activeTarget.retrieveUid,
                    targetIndex: i,
                  })
                  setPendingTarget(null)
                }}
              >
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
                    {enemy.strength > 0 && (
                      <span className="chip chip-strength">💪 {kw('強化')} +{enemy.strength}</span>
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
                      <span className="chip chip-strength" title="1ヒットで受けるダメージはこの値以下。延焼は装甲を無視する">🛡 装甲{enemyDef.armor}</span>
                    )}
                    {enemyDef.burnResist !== undefined && !dead && (
                      <span className="chip chip-strength">💧 {kw('延焼耐性')} -{enemyDef.burnResist}</span>
                    )}
                    {enemyDef.thorns !== undefined && !dead && (
                      <span className="chip chip-strength">🦔 {kw('とげ')} {enemyDef.thorns}</span>
                    )}
                    {(enemy.stolenGold ?? 0) > 0 && !dead && (
                      <span className="chip chip-growth">💰 {enemy.stolenGold}G 抱え込み</span>
                    )}
                    {enemy.fled === true && (
                      <span className="chip">
                        🏃 逃走済み{(enemy.stolenGold ?? 0) > 0 ? `（${enemy.stolenGold}G持ち逃げ）` : ''}
                      </span>
                    )}
                    {(enemyDef.movesBelowHalf || enemyDef.sequenceBelowHalf) &&
                      enemy.hp <= enemy.maxHp * 0.5 &&
                      !dead && <span className="chip chip-strength">😾 牙をむいている</span>}
                    {enemyDef.enrage !== undefined && !dead && (
                      <span className="chip chip-strength">
                        😡 {kw('激昂')} +{enemyDef.enrage}
                        {enemyDef.enrageEveryCards ? `/${enemyDef.enrageEveryCards}枚プレイ` : '/T'}
                      </span>
                    )}
                  </div>
                  {!ended && !dead && (
                    <div className={`intent${enemy.intent?.kind === 'defend' ? ' intent-defend' : ''}`}>
                      {kw(conditionalIntentText(s, i))}
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
                    {c.setFresh !== true && <span title="敵はこの札に反応しない (織り込み済み)">（見切られ）</span>}
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
                    ctx={{ growth: player.growth, momentum: player.momentum, energyMax: player.energyMaxAtTurnStart ?? player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether, exhausted: player.exhaustPile.length, selfHpLost: player.selfHpLost, permanents: player.permanents.length, damageTaken: player.damageTakenLastEnemyPhase, iceBlock: player.iceBlock, randomPlayed: player.randomPlayedThisCombat, handCards: Math.max(0, player.hand.length - 1) }}
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
                  ※この攻撃はすでに解決済み——発動しても今回の被弾は取り消せない（返し・回復のための窓）
                </div>
              )}
              {s.reactionMode === 'set-confirm' && setCard ? (
                <>
                  {(() => {
                    // 伏せ2枚 (かすみ): 窓に合致する伏せ札ごとに発動ボタンを出す
                    const win = windowFromPending(s)
                    const candidates = win
                      ? player.setCards.filter((c) => reactionMatches(s, c, win))
                      : []
                    return candidates.map((c) => (
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
                            handCards: Math.max(0, player.hand.length - 1),
                          }),
                        )}
                        ）{' '}
                        <button
                          className="btn btn-primary"
                          onClick={() => dispatch({ type: 'ConfirmReaction', fire: true, cardUid: c.uid })}
                        >
                          発動する
                        </button>
                      </div>
                    ))
                  })()}
                  {setBranchFlipRisks(s).map((ri) => (
                    <div key={ri} className="choice-desc" style={{ margin: '6px 0', color: 'var(--warn, #e0a458)' }}>
                      ⚠ 発動すると伏せ枠が空く: {getEnemyDef(s.enemies[ri].enemyId).name}の行動が【伏せなし】分岐（{intentText(s.enemies[ri].intent)}）に変わる
                    </div>
                  ))}
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
                      {c.def.name}({cardCostLabel(c.def)}) —{' '}
                      {effectText(c.def, { growth: player.growth, momentum: player.momentum, energyMax: player.energyMaxAtTurnStart ?? player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether, exhausted: player.exhaustPile.length, selfHpLost: player.selfHpLost, permanents: player.permanents.length, damageTaken: player.damageTakenLastEnemyPhase, iceBlock: player.iceBlock, randomPlayed: player.randomPlayedThisCombat, handCards: Math.max(0, player.hand.length - 1) })}
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
          let worst = 0
          s.enemies.forEach((e, i) => {
            if (e.hp <= 0) return
            const it = effectiveIntent(s, i)
            if (it?.kind === 'attack') worst += it.shownMax * (it.hits ?? 1)
          })
          if (worst <= 0) return null
          const defense = player.block + player.iceBlock
          const through = Math.max(0, worst - defense)
          return (
            <div className={`panel forecast${through >= player.hp ? ' forecast-danger' : ''}`}>
              ⚠️ 最悪被ダメ {worst} − 防御 {defense} = <b>{through}</b>（HP {player.hp}）
            </div>
          )
        })()}

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
        {activeExhaust && (
          <div className="discard-banner">
            「{player.hand.find((c) => c.uid === activeExhaust.cardUid)?.def.name}」の追加コスト:
            消滅させるカードを選んでください（
            {(player.hand.find((c) => c.uid === activeExhaust.cardUid)?.def.exhaustCost ?? 0) - activeExhaust.chosen.length}
            枚）{' '}
            <button className="btn" onClick={() => setPendingExhaust(null)}>
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
                    ctx={{ growth: player.growth, momentum: player.momentum, energyMax: player.energyMaxAtTurnStart ?? player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether, exhausted: player.exhaustPile.length, selfHpLost: player.selfHpLost, permanents: player.permanents.length, damageTaken: player.damageTakenLastEnemyPhase, iceBlock: player.iceBlock, randomPlayed: player.randomPlayedThisCombat, handCards: Math.max(0, player.hand.length - 1) }}
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
                  effCost <= player.energy &&
                  player.hand.length - 1 >= discardCost &&
                  player.hand.length - 1 >= exhaustCostN &&
                  pileOk
                const canSet = isSetMode && system.canHandle(s, { type: 'SetCard', cardUid: c.uid })
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
                      ctx={{ growth: player.growth, momentum: player.momentum, energyMax: player.energyMaxAtTurnStart ?? player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether, exhausted: player.exhaustPile.length, selfHpLost: player.selfHpLost, permanents: player.permanents.length, damageTaken: player.damageTakenLastEnemyPhase, iceBlock: player.iceBlock, randomPlayed: player.randomPlayedThisCombat, handCards: Math.max(0, player.hand.length - 1) }}
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
                      ctx={{ growth: player.growth, momentum: player.momentum, energyMax: player.energyMaxAtTurnStart ?? player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether, exhausted: player.exhaustPile.length, selfHpLost: player.selfHpLost, permanents: player.permanents.length, damageTaken: player.damageTakenLastEnemyPhase, iceBlock: player.iceBlock, randomPlayed: player.randomPlayedThisCombat, handCards: Math.max(0, player.hand.length - 1) }}
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
                return (
                  <CardFrame
                    key={c.uid}
                    card={c}
                    ctx={{ growth: player.growth, momentum: player.momentum, energyMax: player.energyMaxAtTurnStart ?? player.energyMax, cardsPlayed: player.cardsPlayedThisTurn, aether: player.aether, exhausted: player.exhaustPile.length, selfHpLost: player.selfHpLost, permanents: player.permanents.length, damageTaken: player.damageTakenLastEnemyPhase, iceBlock: player.iceBlock, randomPlayed: player.randomPlayedThisCombat, handCards: Math.max(0, player.hand.length - 1) }}
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
const MAP_VH = MAP_PAD_T + (MAP_ROWS - 1) * MAP_ROW_H + MAP_PAD_B

/** 決定的ジッタ (本家StS風に格子を崩す)。Math.random は使わない = 再レンダで揺れない */
function mapJitter(r: number, c: number, salt: number): number {
  const h = Math.sin(r * 12.9898 + c * 78.233 + salt * 37.719) * 43758.5453
  return h - Math.floor(h) // 0..1
}
const mapNodeX = (r: number, c: number, width: number): number =>
  MAP_PAD_L +
  ((c + 0.5) * (MAP_VW - MAP_PAD_L - MAP_PAD_R)) / width +
  (mapJitter(r, c, 1) - 0.5) * 10
/** 行15 (ボス) を上に、行0を下に */
const mapNodeY = (r: number, c: number): number =>
  MAP_PAD_T + (BOSS_ROW - r) * MAP_ROW_H + (mapJitter(r, c, 2) - 0.5) * 6
/** その行のノード1つに許すラベル幅 (隣のラベルとの最小ギャップが 34 units 残る値) */
const mapLabelMaxW = (width: number): number => (MAP_VW - MAP_PAD_L - MAP_PAD_R) / width - 22

const MAP_ICON: Record<MapNodeType, string> = {
  battle: '⚔️',
  elite: '👑',
  campfire: '🔥',
  workshop: '🔨',
  shop: '🛒',
  event: '❓',
  boss: '💀',
}
const MAP_TYPE_LABEL: Record<MapNodeType, string> = {
  battle: '戦闘',
  elite: '強個体',
  campfire: '焚き火',
  workshop: '工房',
  shop: 'ショップ',
  event: '？？？',
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
}: {
  run: RunState
  onChoose: (col: number) => void
}) {
  const cands = nextChoices(run)
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
  const edges: ReactElement[] = []
  for (let r = 0; r < MAP_ROWS - 1; r++) {
    const row = run.map[r]
    const wNext = run.map[r + 1].length
    for (let c = 0; c < row.length; c++) {
      const ax = mapNodeX(r, c, row.length)
      const ay = mapNodeY(r, c)
      for (const to of row[c].next) {
        const bx = mapNodeX(r + 1, to, wNext)
        const by = mapNodeY(r + 1, to)
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
      viewBox={`0 0 ${MAP_VW} ${MAP_VH}`}
      role="group"
      aria-label={`ランのマップ 第${run.act}幕`}
    >
      {edges}
      {run.map.map((row, r) => {
        const maxW = mapLabelMaxW(row.length)
        return (
          <g key={`row${r}`}>
            <text
              className={r === run.row ? 'map-rowlabel map-rowlabel-here' : 'map-rowlabel'}
              x={MAP_PAD_L - 10}
              y={MAP_PAD_T + (BOSS_ROW - r) * MAP_ROW_H + 3}
              textAnchor="end"
            >
              {r === BOSS_ROW ? 'ボス' : `行${r + 1}`}
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
                  ? `強個体: ${name}（強化+2・HP×1.35／勝てばレリック3択）`
                  : n.type === 'boss'
                    ? `幕ボス: ${name}（撃破で全回復＋レリック3択）`
                    : n.type === 'campfire'
                      ? '焚き火: 自動で30%回復＋「鍛える/取り除く/何もしない」'
                      : n.type === 'workshop'
                        ? '工房: デッキの2枚を合成して1枚にする'
                        : n.type === 'shop'
                          ? 'ショップ: カード/レリック/除去/強化'
                          : n.type === 'event'
                            ? '？: 入るまで中身は分からない（イベント85%／戦闘10%／ショップ3%／宝箱2%。外れた種別は次から確率が上がる）'
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
                  transform={`translate(${mapNodeX(r, c, row.length).toFixed(2)} ${mapNodeY(r, c).toFixed(2)})`}
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
                    {...mapFit(name, maxW, MAP_NAME_FONT)}
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
          y={MAP_PAD_T + (MAP_ROWS - 1) * MAP_ROW_H + 44}
          textAnchor="middle"
        >
          ▲ ここから登る
        </text>
      ) : null}
    </svg>
  )
}

// ---- ドラフト連戦 (ラン) 画面 ----

function RunScreen({
  run,
  dispatch,
  history,
  onExit,
  onRestart,
}: {
  run: RunState
  dispatch: (c: RunCommand) => void
  /** 決着済みの戦闘の履歴 (書き出しに含める) */
  history: readonly BattleArchive[]
  onExit: () => void
  onRestart: (seed: number) => void
}) {
  const isBoss = currentNode(run)?.type === 'boss'
  const progressChip = `幕${run.act}/3・${isBoss ? '👑 幕ボス戦' : run.currentElite ? `⚔️👑 強個体戦 (行${run.row + 1}/16)` : `行${run.row + 1}/16・${run.battlesWon}勝`}・デッキ${run.deck.length}枚`
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
            全体も道（接続線）も最初から見える。<b>緑の実線＝いま進める道</b>／金の線＝通ってきた道／薄い点線＝現在地から到達できない道（接続は前の行でどの列を選んだかで決まる）。👑強個体=強化+2/HP×1.35、勝てばレリック3択。🔥焚き火=休む(30%回復)/鍛える/取り除く の択一。🔨工房=カード合成。🛒ショップ。❓=入るまで不明
          </div>
          <div style={{ marginTop: 6 }}>
            <span className="chip">HP {run.hp}/{run.maxHp}</span>
            <span className="chip">💰 {run.gold}G</span>
            <span className="chip">デッキ {run.deck.length}枚</span>
            <span className="chip">{run.battlesWon}勝</span>
          </div>
          {relicChips}
        </div>
        <div className="map-wrap">
          <RunMapView run={run} onChoose={(col) => dispatch({ type: 'ChooseNode', col })} />
        </div>
        <div style={{ marginTop: 12 }}>
          <button className="btn" onClick={() => saveReport(run, null, history)}>📄 状況を書き出す</button>{' '}
          <button className="btn" onClick={onExit}>ランを放棄</button>
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
                onClick={() => dispatch({ type: 'PickRelic', index: i })}
              >
                <div className="choice-title">
                  <span className="choice-sprite">{r.sprite}</span>
                  {r.name}
                </div>
                <div className="choice-desc">{r.description}</div>
              </button>
            )
          })}
        </div>
        <button
          className="btn"
          style={{ marginTop: 12 }}
          onClick={() => dispatch({ type: 'SkipRelic' })}
        >
          見送る
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
          <span className="chip">デッキ {run.deck.length}枚</span>
          <div className="choice-desc" style={{ marginTop: 6 }}>
            買わずに出てもよい。除去サービスは1回まで。
          </div>
        </div>
        <div className="setup-section-title">カード</div>
        <div className="hand-cards" style={{ margin: '12px 0' }}>
          {run.shop.cards.map((item, i) => (
            <CardFrame
              key={`${item.id}_${i}`}
              card={{ uid: `shop${i}`, def: getCardDef(item.id) }}
              dim={false}
              ctx={ctx}
              actions={
                <button
                  className="btn btn-primary"
                  disabled={run.gold < item.price}
                  onClick={() => dispatch({ type: 'ShopBuyCard', index: i })}
                >
                  {item.price}G で買う
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
            サービス: 除去 {shopRemovalPrice(run)}G ／ 強化 {shopUpgradePrice(run)}G（回数無制限・使うたび値上がり）
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
                      強化 {shopUpgradePrice(run)}G
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
                  {run.deck.map((card, ci) => (
                    <CardFrame
                      key={card.uid}
                      card={card}
                      dim={false}
                      ctx={ctx}
                      actions={
                        <button
                          className="btn"
                          onClick={() => dispatch({ type: 'EventChoice', index: i, cardIndex: ci })}
                        >
                          このカードを選ぶ
                        </button>
                      }
                    />
                  ))}
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
    return (
      <div className="app setup">
        <h1>🔥 焚き火</h1>
        <p className="hint">
          「休む」「鍛える」「取り除く」から1つを選ぶ（本家式の排他三択。2026-08-29復帰）。
          {(run.campfireForgeBonus ?? 0) > 0 &&
            ` 🪨鍛冶の砥石: 鍛えるはあと${1 + (run.campfireForgeBonus ?? 0) - (run.campfireUpgradesUsed ?? 0)}枚（休む・除去とは併用不可）。`}
        </p>
        <div className="choice-row" style={{ marginTop: 12 }}>
          <button className="choice" onClick={() => dispatch({ type: 'CampfireRest' })}>
            <div className="choice-title">
              <span className="choice-sprite">🛌</span>
              {(run.campfireUpgradesUsed ?? 0) > 0 ? '立ち去る' : '休む'}
            </div>
            <div className="choice-desc">
              {(run.campfireUpgradesUsed ?? 0) > 0
                ? 'すでに鍛えたので回復はなし'
                : `HP+${heal} 回復して先へ（現在 ${run.hp}/${run.maxHp}）`}
            </div>
          </button>
        </div>
        <div className="setup-section-title" style={{ marginTop: 20 }}>
          デッキの1枚を「鍛える」か「取り除く」（デッキ{run.deck.length}枚・最低5枚は残る）
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
                  {canUpgradeCard(c) && (
                    <div className="choice-desc" style={{ marginBottom: 4 }}>
                      鍛えると→ {describeUpgrade(c)}
                    </div>
                  )}
                  <button
                    className="btn"
                    disabled={run.deck.length <= 5}
                    onClick={() => dispatch({ type: 'CampfireRemove', index: i })}
                  >
                    取り除く
                  </button>{' '}
                  <button
                    className="btn btn-primary"
                    disabled={!canUpgradeCard(c)}
                    onClick={() => dispatch({ type: 'CampfireUpgrade', index: i })}
                  >
                    {isUpgraded(c) ? '鍛済' : canUpgradeCard(c) ? '鍛える' : '鍛不可'}
                  </button>
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
        onExport={() => saveReport(run, null, history)}
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
          <span className="chip">デッキ {run.deck.length}枚</span>
          {relicChips}
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
        </button>{' '}
        <button className="btn" onClick={() => saveReport(run, null, history)}>
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
          最終デッキ {run.deck.length}枚
        </div>
        <div className="choice-desc" style={{ marginTop: 6 }}>
          ピック履歴:{' '}
          {run.picks.length > 0 ? run.picks.map((id) => getCardDef(id).name).join('、') : '（なし）'}
        </div>
      </div>
      <div style={{ marginTop: 16 }}>
        <button className="btn btn-primary" onClick={() => saveReport(run, null, history)}>
          📄 状況を書き出す
        </button>{' '}
        <button className="btn" onClick={() => onRestart(Date.now() % 2 ** 32)}>
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
  // ラン全10戦の履歴。engine の RunState は combat を単一スロットで上書きするため UI 側で溜める
  const [runHistory, setRunHistory] = useState<readonly BattleArchive[]>([])

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
    w.deckRogueReport = () => buildReport(run, state, runHistory)
    w.deckRogueRecoverReport = () => {
      try {
        const raw = localStorage.getItem('deckRogueBackup')
        if (raw === null) return '(バックアップなし)'
        const b = JSON.parse(raw) as {
          run: RunState | null
          state: GameState | null
          history: BattleArchive[]
        }
        return buildReport(b.run ?? null, b.state ?? null, b.history ?? [])
      } catch (e) {
        return `(復元失敗: ${String(e)})`
      }
    }
    try {
      // 容量超過 (QuotaExceeded) 等は握りつぶす = バックアップは保険であって本線ではない
      localStorage.setItem('deckRogueBackup', JSON.stringify({ run, state, history: runHistory }))
    } catch {
      /* no-op */
    }
  }, [run, state, runHistory])

  const start = (cfg: Config) => {
    setConfig(cfg)
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
      )
      // 同じ戦闘を二度積まない (再入や二重実行への保険)
      setRunHistory((h) => (h.some((a) => a.battleNo === archived.battleNo) ? h : [...h, archived]))
    }
    setRun(next)
  }

  if (run !== null) {
    return (
      <RunScreen
        run={run}
        dispatch={dispatchRun}
        history={runHistory}
        onExit={() => {
          setRun(null)
          setRunHistory([])
        }}
        onRestart={(seed) => {
          setRunHistory([])
          setRun((prev) => createRun(seed, ADOPTED_MODE, prev?.leaderId ?? 'leader_green'))
        }}
      />
    )
  }
  if (state === null || config === null) {
    return <SetupScreen onStart={start} onStartRun={(seed, leaderId, runDeckId) => {
        setRunHistory([])
        setRun(createRun(seed, ADOPTED_MODE, leaderId, runDeckId))
      }} />
  }
  return (
    <BattleScreen
      onExport={() => saveReport(null, state)}
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
        異なる2枚を選んで合成する。素材2枚は消え、合成された1枚がデッキに入る（圧縮と強化が同時）。
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
      <button
        className="btn btn-primary"
        disabled={!preview}
        onClick={() =>
          dispatch({ type: 'WorkshopFuse', indexA: selected[0], indexB: selected[1] })
        }
      >
        合成する
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
