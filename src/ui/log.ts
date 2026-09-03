// ui/log.ts — イベントログ・意図表示の純粋レンダラ (DOM 非依存)。
// report.ts (書き出し = DOM接触あり) と App.tsx とテストから共用する。
import { encounterName, getCardDef } from '../engine/content.ts'
import { resolveFusedDef } from '../engine/fusion.ts'
import type { EnemyIntent, GameEvent } from '../engine/types.ts'
// プレイテストの状況をAIへ渡すためのテキスト書き出し (2026-08-26)。
// ui/ 層に置く純関数。engine には触らない。ダウンロードは App 側の1関数だけがDOMを使う。

const KIND_LABEL: Record<string, string> = {
  attack: '攻撃', defend: '防御', 'destroy-set': '伏せ破壊',
  'destroy-token': '従者狩り', buff: '筋力上げ', rally: '応援', hex: '呪い',
}

export const STATUS_LABEL: Record<string, string> = { weak: '弱体', vulnerable: '脆弱', frail: '虚弱', wound: '負傷', junk: 'がらくた', scald: '火傷', restrain: '拘束', mist: '霞み', slow: '重り' }

export function inflictSuffix(intent: EnemyIntent): string {
  if (!intent.inflict) return ''
  // カード汚染は行き先まで予告する (2026-09-02 StS2のCardDebuff意図準拠 = 対処の計画が立つ)
  const dest =
    intent.inflict.status === 'wound'
      ? '(捨て札へ)'
      : intent.inflict.status === 'junk'
        ? '(山札へ)'
        : intent.inflict.status === 'scald'
          ? '(手札へ)'
          : ''
  return ` ＋${STATUS_LABEL[intent.inflict.status]}${intent.inflict.amount}${dest}`
}

export function intentText(intent: EnemyIntent | null): string {
  if (!intent) return '---'
  switch (intent.kind) {
    case 'attack': {
      const hits = intent.mirrorHits === true ? '×手数' : (intent.hits ?? 1) > 1 ? `×${intent.hits}` : ''
      const guard = intent.alsoDefend !== undefined ? `+🛡️${intent.alsoDefend}` : ''
      const buff = intent.alsoBuff !== undefined ? `+💪${intent.alsoBuff}` : ''
      return `⚔️ 攻撃 ${intent.shownMin}〜${intent.shownMax}${hits}${guard}${buff}${inflictSuffix(intent)}`
    }
    case 'defend': return `🛡️ 防御 ${intent.shownMin}〜${intent.shownMax}${buff}`
    case 'destroy-set': return '💥 伏せ破壊'
    case 'destroy-token': return '🪓 従者狩り'
    case 'buff': return `💪 筋力 +${intent.shownMin}〜${intent.shownMax}`
    case 'rally': return `📣 応援 +${intent.shownMin}〜${intent.shownMax}（味方全体の筋力）`
    case 'hex': return `🧿 呪い${inflictSuffix(intent)}`
    case 'heal': return `💚 回復 ${intent.shownMin}〜${intent.shownMax}（最も傷んだ味方）`
    case 'steal-gold': return `💰 盗み ${intent.shownMin}〜${intent.shownMax}G`
    case 'flee': return '🏃 逃走（倒すか打ち消せば阻止）'
    case 'rest': return '😮‍💨 隙だらけ'
    case 'hatch': return '🐣 孵化する'
    case 'mill': return `📖 山札喰い ${intent.shownMin}〜${intent.shownMax}枚（消滅置き場へ。亡骸は発火する）`
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
    case 'SetCardRetrieved': return { text: `回収: ${cardName(e.cardId)} (1E払って手札へ)`, cls: 'log-line' }
    case 'EnemyIntentDeclared': return { text: `敵の意図: ${intentText(e.intent)}`, cls: 'log-line' }
    case 'EnemyActionExecuting':
    case 'EnemyActionResolved': return null
    case 'ActionNegated': return { text: '敵の行動は打ち消された！', cls: 'log-good' }
    case 'DamageDealt':
      return e.source === 'player'
        ? { text: `敵に${e.amount}ダメージ (HP減 ${e.hpLoss})${e.armorCut ? `【装甲で${e.armorCut}切り捨て】` : ''}${e.turnArmorCut ? `【ターン装甲で${e.turnArmorCut}切り捨て】` : ''}`, cls: 'log-line' }
        : { text: `敵の攻撃${e.amount} → HP減 ${e.hpLoss}`, cls: 'log-bad' }
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
    case 'DiscountGained': return { text: `次にプレイするカードのコスト-${e.amount}`, cls: 'log-line' }
    case 'BurnApplied': return { text: `敵に延焼+${e.amount}`, cls: 'log-good' }
    case 'BurnTick': return { text: `延焼で敵に${e.amount}ダメージ`, cls: 'log-good' }
    case 'EnemySplit': return { text: `🫠 分裂！ 倒した敵から${e.count}体が現れた`, cls: 'log-bad' }
    case 'EnemyHatched': return { text: '🐣 孵化した！', cls: 'log-bad' }
    case 'GuardianRedirected': return { text: '🛡️ 庇われた！ 単体対象は護衛に向かった', cls: 'log-info' }
    case 'ArtifactBlocked': return { text: '🔮 アーティファクトがデバフを弾いた（チャージ-1）', cls: 'log-bad' }
    case 'EnemyWoken': return { text: '👁️ 目を覚ました！ 眠りの前奏が打ち切られた', cls: 'log-bad' }
    case 'ScaldTick': return { text: `🔥 火傷・烙印${e.count}枚が疼いた（HP-${e.amount}）`, cls: 'log-bad' }
    case 'StatusInflicted':
      return { text: e.status === 'wound' ? `負傷${e.amount}枚が捨て札に混入した` : e.status === 'scald' ? `火傷${e.amount}枚が手札に押し込まれた（ターン終了時に手札にあるとHP-2）` : `${STATUS_LABEL[e.status]}${e.amount}を付与された`, cls: 'log-bad' }
    case 'RegenTicked': return { text: `敵は再生でHP+${e.amount}`, cls: 'log-bad' }
    case 'RegenBroken': return { text: '再生が止まった（このターンの削りが閾値を超えた）', cls: 'log-good' }
    case 'EnemyConfused': return { text: `敵に混乱+${e.amount}（攻撃が仲間に向かう）`, cls: 'log-good' }
    case 'ExposedApplied': return { text: `敵に急所+${e.amount}（次のダメージ${e.amount}回が+50%）`, cls: 'log-good' }
    case 'GrowthDischarged': return { text: `成長${e.spent}を全て放出した！`, cls: 'log-good' }
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
    case 'ExhaustRecycled': return { text: `♻️ 輪廻: 消滅置き場${e.count}枚が山札へ還った`, cls: 'log-good' }
    case 'BurnDischarged': return { text: `爆熱: 延焼${e.amount}を全て解き放った`, cls: 'log-line' }
    case 'TokenDestroyed': return { text: `従者狩り: ${cardName(e.cardId)}が倒された`, cls: 'log-line' }
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
    case 'CardsMovedToHand': return { text: `${e.from === 'draw' ? '🔍 サーチ' : '🌱 回収'}: ${e.cardIds.map(cardName).join('・')}を手札に加えた`, cls: 'log-good' }
    case 'CardCopied': return { text: `🌿 ${cardName(e.cardId)}のコピー${e.count}枚を捨て札に加えた`, cls: 'log-line' }
    case 'CardGrew': return { text: `📈 ${cardName(e.cardId)}が育った（与ダメ+${e.bonus}）`, cls: 'log-good' }
    case 'CardUpgradedInHand': return { text: `🔨 ${cardName(e.cardId)}を鍛えた（この戦闘中）`, cls: 'log-good' }
    case 'ReactionTriggered': return { text: `リアクション発動: ${cardName(e.cardId)}`, cls: 'log-good' }
    case 'ReactionWhiffed': return { text: `空振り: ${cardName(e.cardId)}`, cls: 'log-line' }
    case 'ReactionUnaffordable': return { text: `⚠ 伏せ札「${cardName(e.cardId)}」は発動に${e.cost}E必要だが残り${e.energy}E＝窓は開かず温存`, cls: 'log-bad' }
    case 'ReactionHeld':
      return {
        text: `温存: ${e.candidateIds.map(cardName).join('、')}（敵${e.enemyIndex + 1}の${KIND_LABEL[e.kind] ?? e.kind} ${e.stage}窓 / 実値${e.value}）`,
        cls: 'log-line',
      }
    case 'SetCardDestroyed': return { text: `伏せカード破壊: ${cardName(e.cardId)}`, cls: 'log-bad' }
    case 'EnemyPhaseEnded': return null
    case 'CombatEnded':
      return e.result === 'won' ? { text: '=== 勝利 ===', cls: 'log-good' } : { text: '=== 敗北 ===', cls: 'log-bad' }
  }
}

// ---- ここから下がエクスポート専用 ----

/**
 * 決着した戦闘の保管記録 (2026-08-26)。
 * engine の RunState は combat を単一スロットで持ち、次の戦闘開始時に上書きするため、
 * ラン全体の履歴を残すには UI 側で溜めるしかない
 * (engine に history を持たせると Unity移植面と不変遷移のコストが増えるので避ける)。
 */
