// ui/log.ts — イベントログ・意図表示の純粋レンダラ (DOM 非依存)。
// report.ts (書き出し = DOM接触あり) と App.tsx とテストから共用する。
import { DEPARTURE_LEAD, DEPARTURE_TITLE, departureMasterLine, encounterName, getCardDef, getGearDef, getRelicDef } from '../engine/content.ts'
import { resolveFusedDef } from '../engine/fusion.ts'
import { displayedInflict } from '../engine/effects.ts'
import { GEAR_CARRY_MAX } from '../engine/gears.ts'
import { departureOfferAvailable, departureOfferBought, eventChoiceAvailable, eventChoiceNeedsCard, gearsOf } from '../engine/run.ts'
import type { RunCommand, RunState } from '../engine/run.ts'
import { relicRarityTag } from '../engine/summary.ts'
import type { CardDef, CardInstance, DepartureOffer, EnemyIntent, EnemyIntentBranch, EventChoiceDef, GameEvent, GameState, GearDef } from '../engine/types.ts'
import { webVocab } from './vocab.ts'
// プレイテストの状況をAIへ渡すためのテキスト書き出し (2026-08-26)。
// ui/ 層に置く純関数。engine には触らない。ダウンロードは App 側の1関数だけがDOMを使う。

const KIND_LABEL: Record<string, string> = {
  attack: '攻撃', defend: '防御', 'destroy-set': '伏せ破壊',
  'destroy-token': '人形狩り', buff: '筋力上げ', rally: '応援', hex: '呪い',
}

export const STATUS_LABEL: Record<string, string> = { weak: '弱体', vulnerable: '脆弱', frail: '虚弱', wound: '負傷', junk: 'がらくた', scald: '火傷', restrain: '拘束', mist: '霞み', slow: '重り' }

/**
 * 意図の rider「＋がらくた2(山札へ)」。state を渡すと死に札の上限 (がらくた4・負傷5・火傷5) で実際に増える枚数に畳み、
 * 0 枚なら出さない (2026-09-16 人間#12: 上限到達後も歩哨の意図が6回「+がらくた2」を予告し続けた。engine/effects.ts displayedInflict)
 */
export function inflictSuffix(intent: EnemyIntent | EnemyIntentBranch, state?: GameState): string {
  const inflict = state ? displayedInflict(state, intent.inflict) : intent.inflict
  if (!inflict) return ''
  // カード汚染は行き先まで予告する (2026-09-02 StS2のCardDebuff意図準拠 = 対処の計画が立つ)
  const dest =
    inflict.status === 'wound'
      ? '(捨て札へ)'
      : inflict.status === 'junk'
        ? '(山札へ)'
        : inflict.status === 'scald'
          ? '(手札へ)'
          : ''
  return ` ＋${STATUS_LABEL[inflict.status]}${inflict.amount}${dest}`
}

/**
 * 意図の1行 (実値公開 2026-09-14 本家形)。攻撃の数字は shownValue (威圧・脆弱・重り込みのライブ値。
 * engine/summary.ts displayedIntentValue) を渡す。省略時は宣言した実値 (ログ行など状態が無い場所)
 */
export function intentText(intent: EnemyIntent | EnemyIntentBranch | null, shownValue?: number, state?: GameState): string {
  if (!intent) return '---'
  const mirror = (intent as EnemyIntent).mirrorHits === true
  switch (intent.kind) {
    case 'attack': {
      const hits = mirror ? '×手数' : (intent.hits ?? 1) > 1 ? `×${intent.hits}` : ''
      const guard = intent.alsoDefend !== undefined ? `+🛡️${intent.alsoDefend}` : ''
      const buff = intent.alsoBuff !== undefined ? `+💪${intent.alsoBuff}` : ''
      const breaks = intent.alsoDestroySet === true ? '💥伏せ破壊+' : '' // 壊しつつ殴る (2026-09-14)
      return `${breaks}⚔️ 攻撃 ${shownValue ?? intent.actual}${hits}${guard}${buff}${inflictSuffix(intent, state)}`
    }
    case 'defend': return `🛡️ 防御 ${intent.actual}${intent.alsoBuff !== undefined ? `＋💪筋力+${intent.alsoBuff}` : ''}`
    case 'destroy-set': return '💥 伏せ破壊'
    case 'destroy-token': return '🪓 人形狩り'
    case 'buff': return `💪 筋力 +${intent.actual}`
    case 'rally': return `📣 応援 +${intent.actual}（味方全体の筋力）`
    case 'hex': return `🧿 呪い${inflictSuffix(intent, state)}`
    case 'heal': return `💚 回復 ${intent.actual}（最も傷んだ味方）`
    case 'steal-gold': return `💰 盗み ${intent.actual}G`
    case 'flee': return '🏃 逃走（倒すか打ち消せば阻止）'
    case 'rest': return '😮‍💨 隙だらけ'
    case 'hatch': return '🐣 孵化する'
    case 'mill': return `📖 山札喰い ${intent.actual}枚（消滅置き場へ。亡骸は発火する）`
    case 'summon': return `👶 召喚 ×${intent.actual}（場が4体なら出ない）`
  }
}

export function cardName(cardId: string): string {
  // 合成カード (fused_ / fusion_ 系ID) は静的カード表に居ないため、合成の解決器で復元する
  try {
    return getCardDef(cardId).name
  } catch {
    const fused = resolveFusedDef(cardId)
    return fused ? fused.name : cardId
  }
}

export interface LogLine { text: string; cls: string }

export function logLine(e: GameEvent): LogLine | null {
  switch (e.type) {
    case 'CombatStarted': return { text: `戦闘開始: ${encounterName(e.enemyId)}`, cls: 'log-turn' }
    case 'TurnStarted': return { text: `─── ターン ${e.turn} ───`, cls: 'log-turn' }
    case 'TurnEnded': return { text: 'ターン終了 → 敵の行動', cls: 'log-line' }
    case 'CardsDrawn': return { text: `${e.count}枚ドロー`, cls: 'log-line' }
    case 'CardPlayed': return { text: `プレイ: ${cardName(e.cardId)}`, cls: 'log-line' }
    case 'CardSet': return { text: `伏せた: ${cardName(e.cardId)}`, cls: 'log-line' }
    case 'SetCardExpired': return { text: `期限切れ: ${cardName(e.cardId)}（期限までに発動しなかったので${e.to === 'hand' ? '手札へ' : e.to === 'exhaust' ? '消滅置き場へ' : '捨て札へ'}）`, cls: 'log-line' }
    case 'Scried': return { text: `占術: ${e.looked.length}枚を見た${e.discarded.length > 0 ? `（捨て札へ: ${e.discarded.map(cardName).join('・')}）` : '（全部残した）'}`, cls: 'log-line' }
    case 'TrapLifeExtended': return { text: `伏せ札${e.count}枚の期限を${e.amount}ターン延ばした`, cls: 'log-good' }
    case 'EnemyIntentDeclared': return { text: `敵の意図: ${intentText(e.intent)}`, cls: 'log-line' }
    case 'EnemyActionExecuting':
    case 'EnemyActionResolved': return null
    case 'ActionNegated': return { text: '敵の行動は打ち消された！', cls: 'log-good' }
    case 'DamageDealt':
      return e.source === 'player'
        ? { text: `敵に${e.amount}ダメージ（HP-${e.hpLoss}）${e.exposed ? '【急所】' : ''}${e.pierced ? '【貫通】' : ''}${e.blocked ? `【ブロックで${e.blocked}】` : ''}${e.armorCut ? `【装甲で${e.armorCut}切り捨て】` : ''}${e.burrowCut ? `【潜伏の殻で${e.burrowCut}を捨てた】` : ''}${e.nemesisCut ? `【無形で${e.nemesisCut}消滅=1固定】` : ''}${e.turnArmorCut ? `【ターン装甲で${e.turnArmorCut}切り捨て】` : ''}`, cls: 'log-line' }
        : { text: `敵の攻撃${e.amount} → HP-${e.hpLoss}${e.blocked ? `（ブロックで${e.blocked}）` : ''}`, cls: 'log-bad' }
    case 'BlockGained': return { text: `${e.target === 'player' ? '自分' : '敵'}がブロック+${e.amount}`, cls: 'log-line' }
    case 'StrengthGained': {
      // 激昂の発火は理由を明示する (2026-09-01 検証ラン「跨いだ瞬間を後から確認できない」への処方)
      const ENRAGE_JA: Record<string, string> = { 'enrage-cards': '激昂〔プレイ枚数の節目〕', 'enrage-damage': '激昂〔累計被ダメの節目〕', 'enrage-phase': '激昂〔毎フェーズ〕', mourn: '弔い〔仲間が倒れた〕' }
      const why = e.reason !== undefined ? `😡 ${ENRAGE_JA[e.reason] ?? e.reason}: ` : ''
      return { text: `${why}敵の筋力 +${e.amount}（以降の攻撃に加算）`, cls: 'log-bad' }
    }
    case 'IceBlockGained': return { text: `氷壁+${e.amount}（持ち越しブロック）`, cls: 'log-line' }
    case 'AetherGained': return { text: `霊気+${e.amount}`, cls: 'log-good' }
    case 'SpellEchoed': return { text: `🔁 反復: ${cardName(e.cardId)} の効果が2回解決`, cls: 'log-good' }
    case 'NecroFired': return { text: `💀 亡骸: ${cardName(e.cardId)} が消滅して効果が発火`, cls: 'log-good' }
    case 'NecroPlayed': return { text: `💀 亡骸プレイ: ${cardName(e.cardId)} (ゲームから取り除かれた)`, cls: 'log-line' }
    case 'AetherDischarged': return { text: `霊気${e.spent}を全て放出！`, cls: 'log-good' }
    case 'LightGained': {
      const SRC: Record<string, string> = { heal: '回復', retainer: '人形', passive: '灯匠', card: 'カード', carry: '残り火' }
      return { text: `🕯 灯+${e.amount}（${SRC[e.source] ?? e.source}）`, cls: 'log-good' }
    }
    case 'LightDischarged':
      // 灯の火床 (sparks) は放出でなく「払って火種に変える」(2026-09-24 T15)
      // 灯の炉心 (paid) も放出でなく支払い (2026-09-24)
      return (e.sparks ?? 0) > 0
        ? { text: `🕯 灯${e.spent}を払って火種${e.sparks}を山札へ`, cls: 'log-line' }
        : e.paid === true
          ? { text: `🕯 灯${e.spent}を払った`, cls: 'log-line' }
          : { text: `🕯 灯${e.spent}を放出！`, cls: 'log-good' }
    case 'LightSpent': return { text: `🕯 灯-${e.amount}（${cardName(e.cardId)}）`, cls: 'log-line' }
    case 'DiscountGained': return { text: `次にプレイするカードのコスト-${e.amount}`, cls: 'log-line' }
    case 'BurnApplied': return { text: `敵に延焼+${e.amount}`, cls: 'log-good' }
    case 'BurnTick': return { text: `延焼で敵に${e.amount}ダメージ`, cls: 'log-good' }
    case 'EnemySplit': return { text: e.count === 1 ? '♻️ 再起動！ 倒した敵が次の姿で立ち上がった' : `🫠 分裂！ 倒した敵から${e.count}体が現れた`, cls: 'log-bad' }
    case 'EnemySummoned': return { text: e.count > 0 ? `👶 召喚！ ${e.count}体が現れた` : '👶 召喚したが場が満杯で出なかった', cls: 'log-bad' }
    case 'EnemyHatched': return { text: '🐣 孵化した！', cls: 'log-bad' }
    case 'GuardianRedirected': return { text: '🛡️ 庇われた！ 単体対象は護衛に向かった', cls: 'log-info' }
    case 'ArtifactBlocked': return { text: `🔮 アーティファクトが${({ weakenEnemy: '威圧', exposeEnemy: '急所', confuse: '混乱' } as Record<string, string>)[e.effect] ?? e.effect}を弾いた（チャージ-1・この効果は消えた）`, cls: 'log-bad' }
    case 'BurrowBroken': return { text: '🪺 潜伏の殻が割れた！ 次の行動は噛みつき', cls: 'log-bad' }
    case 'EnemyStaggered': return { text: '🌀 完全に防いだ！ 敵は体勢を崩し、次の行動は隙', cls: 'log-good' }
    case 'EnemyInterrupted': {
      // 割り込み (2026-09-14 行動グラフ): 自ターン中なら意図がその場で差し替わる (本家 Champ/Guardian/Lagavulin 形)
      const what =
        e.trigger === 'damageTaken' ? '👁️ 目を覚ました！ 眠りが終わった'
          : e.trigger === 'hpBelowHalf' ? '😾 HPが半分を切った！'
            : e.trigger === 'alone' ? '😤 仲間が全滅した！'
              : '😤 仲間が倒れた！'
      const pair = e.before !== undefined && e.after !== undefined ? `: ${intentText(e.before)} → ${intentText(e.after)}` : ''
      return { text: `${what} ${e.replaced ? `行動が変わった${pair}` : '次のターンから行動が変わる'}`, cls: 'log-bad' }
    }
    case 'ScaldTick': {
      // 内訳があれば出所を名指し (2026-09-23 人間ラン#17)
      const parts = [e.scalds ? `火傷${e.scalds}枚` : '', e.brands ? `烙印${e.brands}枚` : ''].filter(Boolean)
      return { text: `🔥 ${parts.length > 0 ? parts.join('・') : `火傷・烙印${e.count}枚`}が疼いた（HP-${e.amount}）`, cls: 'log-bad' }
    }
    case 'StatusInflicted':
      return { text: e.status === 'wound' ? `負傷${e.amount}枚が捨て札に混入した` : e.status === 'scald' ? `火傷${e.amount}枚が手札に押し込まれた（ターン終了時に手札にあるとHP-2）` : `${STATUS_LABEL[e.status]}${e.amount}を付与された`, cls: 'log-bad' }
    case 'RegenTicked': return { text: `敵は再生でHP+${e.amount}`, cls: 'log-bad' }
    case 'RegenBroken': return { text: '再生が止まった（このターンの削りが閾値を超えた）', cls: 'log-good' }
    case 'EnemyConfused': return { text: `敵に混乱+${e.amount}（攻撃が仲間に向かう）`, cls: 'log-good' }
    case 'ExposedApplied': return { text: `敵に急所+${e.amount}（次のダメージ${e.amount}回が+50%）`, cls: 'log-good' }
    case 'GrowthDischarged': return { text: `成長${e.spent}を全て放出した！`, cls: 'log-good' }
    case 'MomentumDischarged': return { text: `勢い${e.spent}を全て放出した！`, cls: 'log-good' }
    case 'HpHealed': return { text: `HP+${e.amount}回復`, cls: 'log-good' }
    case 'CardsMilled': return { text: `山札の上${e.count}枚が忘却された（この戦闘から除外・ランのデッキには残る）: ${(e.cardIds ?? []).map(cardName).join('・')}`, cls: 'log-line' }
    case 'EnemyWeakened': return { text: `敵を威圧（筋力-${e.amount}）`, cls: 'log-good' }
    case 'ConfusedAttack':
      return { text: e.enemyIndex === e.targetIndex ? `混乱した敵は自分自身に${e.amount}ダメージ！` : `仲間割れ！ 混乱した敵が味方に${e.amount}ダメージ`, cls: 'log-good' }
    case 'BlockShattered': return { text: `敵のブロック${e.amount}を粉砕！`, cls: 'log-good' }
    case 'ImpulseDrawn': return { text: `衝動${e.count}枚（このターン限り）`, cls: 'log-line' }
    case 'HpLost': return { text: `自傷でHP-${e.amount}`, cls: 'log-bad' }
    case 'EnergyGained': return { text: `エナジー+${e.amount}（このターン）`, cls: 'log-line' }
    case 'MomentumAdded': return { text: `勢い+${e.amount}`, cls: 'log-good' }
    case 'PermanentPlayed': return { text: `置物を設置: ${cardName(e.cardId)}`, cls: 'log-good' }
    case 'CardExhausted': return { text: `消滅: ${cardName(e.cardId)}（この戦闘から除外）`, cls: 'log-line' }
    case 'CardsAddedToHand': return { text: `🗡️ ${cardName(e.cardId)}を${e.count}枚手札に加えた`, cls: 'log-good' }
    case 'CardsAddedToDraw': return { text: `🔥 ${cardName(e.cardId)}を${e.count}枚山札に混ぜた`, cls: 'log-good' }
    case 'CardsAddedToDiscard': return { text: `🔥 ${cardName(e.cardId)}を${e.count}枚捨て札に加えた`, cls: 'log-good' }
    case 'DeckCardTransformed': return { text: `🔥 山札の${cardName(e.cardId)}が${cardName(e.into)}に変わった`, cls: 'log-good' }
    case 'ExhaustRecycled': return { text: `♻️ 輪廻: 消滅置き場${e.count}枚が山札へ還った`, cls: 'log-good' }
    case 'BurnDischarged': return { text: `爆熱: 延焼${e.amount}を全て解き放った`, cls: 'log-line' }
    case 'TokenDestroyed': return { text: `人形狩り: ${cardName(e.cardId)}が壊された`, cls: 'log-line' }
    case 'RetainerSacrificed': return { text: `🕯️ 人形を捧げた: ${cardName(e.cardId)}`, cls: 'log-line' }
    case 'RetainersDuplicated': return { text: `🏳️ 分列: 人形${e.count}体が複製された`, cls: 'log-line' }
    case 'RetainersTriggered': return { text: `📯 号令: 人形${e.count}体がトリガーを問わず今1回ずつ動いた`, cls: 'log-line' }
    case 'RetainerRushed': return { text: `🕯 点灯: ${cardName(e.cardId)}が出た瞬間に1回動いた`, cls: 'log-line' }
    case 'PermanentDismissed': return { text: `🕯 灯が足りず ${cardName(e.cardId)} が場を離れた（捨て札へ）`, cls: 'log-line' }
    case 'RetainerExpired': return { text: `🕯 期限切れ: ${cardName(e.cardId)}が消えた`, cls: 'log-line' }
    case 'RetainerCopied': return { text: `🪞 写し: ${cardName(e.cardId)}をコピーした（残りの期限も写す）`, cls: 'log-line' }
    case 'RetainerLifeExtended': return { text: e.persist === true ? `✨ 永遠の灯: ${cardName(e.cardId)}の期限が無くなった（消えなくなった）` : `🔥 継ぎ火: ${cardName(e.cardId)}の期限を${e.amount}ターン延ばした`, cls: 'log-line' }
    case 'ThornsReflected': return { text: `🦔 とげ反射: ${e.amount}（HP-${e.hpLoss}）`, cls: 'log-damage' }
    case 'GoldStolen': return { text: `💰 盗みを宣言して${e.amount}Gを先取りされた（宣言と同時に成立する。逃がす前に倒せば取り返せる）`, cls: 'log-damage' }
    case 'EnemyFled': return { text: '🏃 敵が逃走した', cls: 'log-line' }
    case 'EnemyHealed': return { text: `💚 敵が回復 +${e.amount}`, cls: 'log-line' }
    case 'CardRetrieved': return { text: `回収: ${cardName(e.cardId)}（消滅置き場から手札へ）`, cls: 'log-line' }
    case 'CardPlayedFromExhaust': return { text: `直接プレイ: ${cardName(e.cardId)}（消滅置き場から）`, cls: 'log-line' }
    case 'CardsDiscarded': return { text: `コストとして捨てた: ${e.cardIds.map(cardName).join('、')}`, cls: 'log-line' }
    case 'EnergyMaxGained': return { text: `エナジー上限+${e.amount}`, cls: 'log-line' }
    case 'GrowthAdded': return { text: `成長+${e.amount}`, cls: 'log-good' }
    case 'SetSlotGained': return { text: `🃏 伏せ枠+${e.amount}（この戦闘中）`, cls: 'log-good' }
    case 'MaxHpGained': return { text: `💗 最大HP+${e.amount}（この戦闘後も残る）`, cls: 'log-good' }
    case 'CardsMovedToHand': return { text: `${e.from === 'draw' ? '🔍 サーチ' : '🌱 回収'}: ${e.cardIds.map(cardName).join('・')}を手札に加えた`, cls: 'log-good' }
    case 'CardCopied': return { text: `🌿 ${cardName(e.cardId)}のコピー${e.count}枚を捨て札に加えた`, cls: 'log-line' }
    case 'CardGrew': return { text: `📈 ${cardName(e.cardId)}が育った（与ダメ+${e.bonus}）`, cls: 'log-good' }
    case 'CardUpgradedInHand': return { text: `🔨 ${cardName(e.cardId)}を鍛えた（この戦闘中）`, cls: 'log-good' }
    case 'ReactionTriggered': return { text: `動かした: ${cardName(e.cardId)}`, cls: 'log-good' }
    case 'ReactionWhiffed': return { text: `空振り: ${cardName(e.cardId)}`, cls: 'log-line' }
    case 'ReactionUnaffordable': return { text: `⚠ 伏せ札「${cardName(e.cardId)}」を発動するには${e.cost}E必要だが残り${e.energy}E＝窓は開かず温存`, cls: 'log-bad' }
    case 'ReactionHeld':
      return {
        text: `温存: ${e.candidateIds.map(cardName).join('、')}（敵${e.enemyIndex + 1}の${KIND_LABEL[e.kind] ?? e.kind} ${e.stage}窓 / 実値${e.value}）`,
        cls: 'log-line',
      }
    case 'SetCardDestroyed': return { text: `伏せ場を壊された: ${cardName(e.cardId)}`, cls: 'log-bad' }
    case 'EnemyPhaseEnded': return null
    case 'DeckShuffled': return { text: '山札を切り直した', cls: 'log-line' }
    case 'EnemyDied': return { text: `敵${e.enemyIndex + 1}を倒した`, cls: 'log-good' }
    case 'DeathSaved': return { text: e.source === 'gear' ? `蘇りの発条がはじけ、HP${e.hp}で踏みとどまった` : `蜥蜴の尾が砕け、HP${e.hp}で踏みとどまった`, cls: 'log-good' }
    case 'GearUsed': return { text: `⚙ ${e.name} を組んだ`, cls: 'log-good' }
    case 'HpLossCapped': return { text: e.left > 0 ? `💓 脈打つ欠片がHPの損失を20で止めた（あと${e.left}回）` : '💓 脈打つ欠片がHPの損失を20で止めた（これで最後。戦いの後に砕ける）', cls: 'log-good' }
    case 'PlayerArtifactBlocked': return { text: `時計仕掛けの土産が状態異常 (${e.status}) を弾いた`, cls: 'log-good' }
    case 'CombatEnded':
      return e.result === 'won' ? { text: '=== 勝利 ===', cls: 'log-good' } : { text: '=== 敗北 ===', cls: 'log-bad' }
  }
}

/**
 * 出来事の列をログ行へ (Web の戦闘ログ)。2つの出来事で1つのことを言う組は1行にまとめる (2026-09-24):
 * ①引く途中の切り直し: engine は引き終えてから DeckShuffled を出すので「引いた→切り直した」と逆順に見えていた
 *   = 直前が CardsDrawn・次が DeckShuffled の組を「山札を切り直して N 枚ドロー」の1行に (Opus ひなた T12)
 * ②灯の火床: 「灯を払った」(LightDischarged の sparks) と直後の「火種を山札に混ぜた」を1行に (T15)
 * 出来事の順と中身は変えない (表示だけ)。sim/play.ts の CLI ログも同じ規則
 */
export function logLines(events: readonly GameEvent[]): LogLine[] {
  const out: LogLine[] = []
  for (let k = 0; k < events.length; k++) {
    const e = events[k]
    const next = events[k + 1]
    if (e.type === 'CardsDrawn' && next?.type === 'DeckShuffled') {
      out.push({ text: `🔀 山札を切り直して${e.count}枚ドロー`, cls: 'log-line' })
      k++
      continue
    }
    const line = logLine(e)
    if (line) out.push(line)
    if (e.type === 'LightDischarged' && (e.sparks ?? 0) > 0 && next?.type === 'CardsAddedToDraw') k++
  }
  return out
}

// ---- 出立の店 (ラン開始 2026-09-24 docs/departure-proposal-2026-09-24.md) ----
// 坑口の行商の店 (上乗せの所持金＋100G・札3・遺物3・サービス4・何個でも買える・買わずに出てもよい)。
// 買わなかった物は行商が担いで降り、幕1のショップの棚に並ぶ (出立の店の画面では言わない。2026-09-24 ユーザー裁定)。
// Web の出立の画面・ショップの「行商が預かった支度」の棚と CLI (sim/play.ts) が同じ文を出すための純関数

/** 支度の文のうち「得る物」の側。「代わりに…」の代償は departureCostText が選択肢の中身から出す */
export function departureGainText(offer: Pick<DepartureOffer, 'text'>): string {
  const i = offer.text.search(/代わりに/u)
  if (i < 0) return offer.text
  return offer.text.slice(0, i).replace(/[。、＝\s]+$/u, '')
}

/** 支度の代償 (いまの台帳には無い。代償つきの品を足した時の備え)。台帳の文でなく選択肢の中身から組み立てる = 実際に起きることを出す。代償が無ければ null */
export function departureCostText(choice: EventChoiceDef): string | null {
  const parts: string[] = []
  if ((choice.brands ?? 0) > 0) parts.push(`呪いの烙印${choice.brands}枚`)
  if ((choice.timedCurses ?? 0) > 0) parts.push(`仮初の烙印${choice.timedCurses}枚`)
  if ((choice.wounds ?? 0) > 0) parts.push(`負傷${choice.wounds}枚`)
  if ((choice.maxHp ?? 0) < 0) parts.push(`最大HP${choice.maxHp}`)
  if ((choice.hp ?? 0) < 0) parts.push(`HP${choice.hp}`)
  if ((choice.hpRatio ?? 0) < 0) parts.push(`HP-${Math.round(-(choice.hpRatio ?? 0) * 100)}%（最大HP比）`)
  if ((choice.gold ?? 0) < 0) parts.push(`${choice.gold}G`)
  return parts.length > 0 ? parts.join('・') : null
}

/** 坑口の店の値段の書き方「N G で買う」 */
export function departurePriceLabel(offer: Pick<DepartureOffer, 'price'>): string {
  return `${offer.price}G で買う`
}

/** 支度の中身から見た「選べない理由」(G は見ない。店の棚でも使う)。選べるなら null。判定は engine の eventChoiceAvailable と同じ */
export function departureChoiceBlockedReason(run: RunState, choice: EventChoiceDef): string | null {
  if (eventChoiceAvailable(run, choice)) return null
  if (choice.requireGold !== undefined && run.gold < choice.requireGold) return `Gが足りない（${choice.requireGold}G）`
  if (choice.relicId !== undefined && run.relics.includes(choice.relicId)) return 'その遺物はもう持っている'
  if (choice.removeCard === true) return 'デッキが5枚以下なので取り除けない'
  if (choice.upgradeCard === true) return '鍛えられる札が無い'
  if (choice.transformCard === true) return '変成できる札が無い'
  if (eventChoiceNeedsCard(choice)) return '対象の札が無い'
  return 'いまは選べない'
}

/** 坑口の店で今買えない理由 (買えるなら null)。既に買った・Gが足りない・対象の札が無い。判定は engine の departureOfferAvailable と同じ */
export function departureUnavailableReason(run: RunState, offer: DepartureOffer): string | null {
  if (departureOfferAvailable(run, offer)) return null
  if (departureOfferBought(run, offer)) return '買った'
  if (run.gold < offer.price) return `Gが足りない（あと${offer.price - run.gold}G）`
  return departureChoiceBlockedReason(run, offer.choice)
}

/** 幕1のショップの棚の支度が今買えない理由 (買えるなら null) */
export function shopDepartureUnavailableReason(run: RunState, offer: DepartureOffer, price: number): string | null {
  if (run.gold < price) return `Gが足りない（あと${price - run.gold}G）`
  return departureChoiceBlockedReason(run, offer.choice)
}

/** 名指しの中身の名前 (遺物・ギア・札)。未定義IDでも落ちない (データ変更後の古いセーブ) */
function safeName(f: () => string, id: string): string {
  try {
    return f()
  } catch {
    return id
  }
}

/**
 * 支度の中身の1行「古代の遺物を1つ＝陽気な花」。ui/report.ts の departureDetail (選択履歴) と同じ文
 * (sim/play.ts は DOM を使う report.ts を取り込めないので、ここに同じ組み立てを持つ)
 */
export function departureDetailText(o: Pick<DepartureOffer, 'text' | 'choice'>): string {
  const parts = [o.text]
  if (o.choice.relicId !== undefined) parts.push(safeName(() => getRelicDef(o.choice.relicId!).name, o.choice.relicId))
  if (o.choice.gears !== undefined && o.choice.gears.length > 0) parts.push(o.choice.gears.map((id) => safeName(() => getGearDef(id).name, id)).join('・'))
  if (o.choice.addCardIds !== undefined && o.choice.addCardIds.length > 0) parts.push(o.choice.addCardIds.map((id) => safeName(() => getCardDef(id).name, id)).join('・'))
  return parts.join('＝')
}

/**
 * CLI (sim/play.ts) の cmd の後の1行: 出立の店で買った・店を出た・幕1のショップの棚で買った。
 * ui/report.ts describeRunChoice と同じ文。該当しなければ null
 */
export function departureChoiceLine(prev: RunState, cmd: RunCommand, next: RunState): string | null {
  const target = (i: number | undefined) => (i !== undefined ? prev.deck[i] : undefined)
  if (cmd.type === 'BuyDeparture') {
    const o = prev.departure?.offers[cmd.index]
    if (o === undefined) return null
    const t = target(cmd.cardIndex)
    return `出立の店: ${o.name}（${departureDetailText(o)}）を${o.price}Gで買った${t !== undefined ? `（対象: ${t.def.name}）` : ''}`
  }
  if (cmd.type === 'LeaveDeparture') {
    const bought = (prev.departure?.offers ?? []).filter((o) => (prev.departure?.bought ?? []).includes(o.id)).map((o) => o.name)
    const carried = (next.departure?.leftovers ?? []).map((o) => o.name)
    return `出立: 店を出て坑へ（買った: ${bought.join('・') || 'なし'}／行商が担いで降りる: ${carried.join('・') || 'なし'}）`
  }
  if (cmd.type === 'ShopBuyDeparture') {
    const slot = prev.shop?.departures?.[cmd.index]
    const o = slot !== undefined ? prev.departure?.leftovers.find((x) => x.id === slot.id) : undefined
    if (slot === undefined || o === undefined) return null
    const t = target(cmd.cardIndex)
    return `ショップ: 坑口で買わなかった品「${o.name}」（${departureDetailText(o)}）を${slot.price}Gで買った${t !== undefined ? `（対象: ${t.def.name}）` : ''}`
  }
  return null
}

/** CLI (sim/play.ts) の書式: 札・ギアの1行とデッキの行 (play.ts の cardLine・gearLine を渡す。テストは簡略版を渡す) */
export interface DepartureCliFormat {
  readonly card: (def: CardDef) => string
  readonly gear: (def: GearDef) => string
  /** デッキの1行。upgrading=鍛える支度がある時 (鍛えた後の姿を添える) */
  readonly deckLine: (card: CardInstance, upgrading: boolean) => string
}

/** 支度1つの CLI の行 (出立の店と幕1のショップの棚で共用): 種類・名前・得る物・名指しの中身・代償・買えない理由 */
function departureOfferCliLines(run: RunState, o: DepartureOffer, head: string, why: string | null, fmt: DepartureCliFormat): string[] {
  const needCard = why !== '買った' && eventChoiceNeedsCard(o.choice) ? ' 【要cardIndex(デッキ番号)】' : ''
  const L = [`${head}「${o.name}」: ${departureGainText(o)}${needCard}${why !== null ? ` 【${why === '買った' ? '買った' : `買えない: ${why}`}】` : ''}`]
  if (o.choice.relicId !== undefined) {
    const r = getRelicDef(o.choice.relicId)
    L.push(`       遺物: ${relicRarityTag(r) ? `${relicRarityTag(r)} ` : ''}${r.name}: ${webVocab(r.description)}`)
  }
  for (const id of o.choice.gears ?? []) L.push(`       ギア: ${fmt.gear(getGearDef(id))}`)
  if ((o.choice.mana ?? 0) > 0) L.push(`       魔素+${o.choice.mana}`)
  for (const id of o.choice.addCardIds ?? []) L.push(`       札: ${fmt.card(getCardDef(id))}`)
  const cost = departureCostText(o.choice)
  if (cost !== null) L.push(`       代償: ${cost}`)
  const room = departureGearRoomNote(run, o.choice)
  if (room !== null) L.push(`       ⚠ ${room}`)
  return L
}

/** CLI の出立の店: 見出し・行商の一言・所持金・品と値段と行方・コマンドの案内 (対象の札が要る支度が残っていればデッキ一覧) */
export function departureCliLines(run: RunState, fmt: DepartureCliFormat): string[] {
  const offers = run.departure?.offers ?? []
  const bought = offers.filter((o) => departureOfferBought(run, o)).map((o) => o.name)
  const L: string[] = [
    `🏪 ${DEPARTURE_TITLE} — ${DEPARTURE_LEAD}`,
    `   行商${departureMasterLine(run.colors)}`,
    `   所持 ${run.gold}G。何個でも買える。買わずに出てもよい`,
    ...(bought.length > 0 ? [`   買った: ${bought.join('・')}`] : []),
  ]
  offers.forEach((o, i) => {
    L.push(...departureOfferCliLines(run, o, ` [${i}] ${o.price}G: `, departureUnavailableReason(run, o), fmt))
  })
  const open = offers.filter((o) => !departureOfferBought(run, o))
  const needsCard = open.some((o) => eventChoiceNeedsCard(o.choice))
  L.push(
    `→ {"type":"BuyDeparture","index":N}${needsCard ? '（対象の札が要る品は "cardIndex":M を添える。M はデッキ番号）' : ''} / {"type":"LeaveDeparture"}（店を出て坑へ。何も買わずに出てもよい）`,
  )
  if (needsCard) {
    const upgrading = open.some((o) => o.choice.upgradeCard === true)
    L.push('   デッキ:')
    run.deck.forEach((c, i) => L.push(`   [${i}] ${fmt.deckLine(c, upgrading)}`))
  }
  return L
}

/** CLI のショップの「行商が預かった支度」の段 (幕1だけ。出立の店で買わなかった物)。棚が無ければ空 */
export function shopDepartureCliLines(run: RunState, fmt: DepartureCliFormat): string[] {
  const slots = run.shop?.departures ?? []
  if (slots.length === 0) return []
  const L: string[] = [' 坑口で買わなかった品 (幕1のショップだけ):']
  slots.forEach((slot, i) => {
    const o = slot.sold === true ? undefined : run.departure?.leftovers.find((x) => x.id === slot.id)
    if (o === undefined) {
      L.push(`  支度[${i}] 〔${slot.sold === true ? '売切' : '行商が持っていない'}〕`)
      return
    }
    L.push(...departureOfferCliLines(run, o, `  支度[${i}] ${slot.price}G: `, shopDepartureUnavailableReason(run, o, slot.price), fmt))
  })
  if (slots.some((x) => x.sold !== true)) L.push('  → {"type":"ShopBuyDeparture","index":N} (対象の札が要る品は "cardIndex":M を添える。M はデッキ番号)')
  return L
}

/** 持ち物の空きが足りない時の注記 (ギアは満杯なら入らない)。空きが足りていれば null */
export function departureGearRoomNote(run: RunState, choice: EventChoiceDef): string | null {
  const want = choice.gears?.length ?? 0
  if (want === 0) return null
  const room = Math.max(0, GEAR_CARRY_MAX - gearsOf(run).length)
  if (room >= want) return null
  return room === 0 ? `持ち物が満杯（${GEAR_CARRY_MAX}）なのでギアは入らない` : `持ち物の空きは${room}つ（入るのは${room}個まで）`
}

// ---- ここから下がエクスポート専用 ----

/**
 * 決着した戦闘の保管記録 (2026-08-26)。
 * engine の RunState は combat を単一スロットで持ち、次の戦闘開始時に上書きするため、
 * ラン全体の履歴を残すには UI 側で溜めるしかない
 * (engine に history を持たせると Unity移植面と不変遷移のコストが増えるので避ける)。
 */
