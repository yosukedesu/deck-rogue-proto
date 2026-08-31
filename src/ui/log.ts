// ui/log.ts — イベントログ・意図表示の純粋レンダラ (DOM 非依存)。
// report.ts (書き出し = DOM接触あり) と App.tsx とテストから共用する。
import { encounterName, getCardDef } from '../engine/content.ts'
import { resolveFusedDef } from '../engine/fusion.ts'
import type { EnemyIntent, GameEvent } from '../engine/types.ts'
// プレイテストの状況をAIへ渡すためのテキスト書き出し (2026-08-26)。
// ui/ 層に置く純関数。engine には触らない。ダウンロードは App 側の1関数だけがDOMを使う。

const KIND_LABEL: Record<string, string> = {
  attack: '攻撃', defend: '防御', 'destroy-set': '伏せ破壊',
  'destroy-token': '従者狩り', buff: '強化', rally: '応援', hex: '呪い',
}

export const STATUS_LABEL: Record<string, string> = { weak: '弱体', vulnerable: '脆弱', wound: '負傷', junk: 'がらくた' }

export function inflictSuffix(intent: EnemyIntent): string {
  if (!intent.inflict) return ''
  return ` ＋${STATUS_LABEL[intent.inflict.status]}${intent.inflict.amount}`
}

export function intentText(intent: EnemyIntent | null): string {
  if (!intent) return '---'
  switch (intent.kind) {
    case 'attack': {
      const hits = intent.mirrorHits === true ? '×手数' : (intent.hits ?? 1) > 1 ? `×${intent.hits}` : ''
      const guard = intent.alsoDefend !== undefined ? `+🛡️${intent.alsoDefend}` : ''
      return `⚔️ 攻撃 ${intent.shownMin}〜${intent.shownMax}${hits}${guard}${inflictSuffix(intent)}`
    }
    case 'defend': return `🛡️ 防御 ${intent.shownMin}〜${intent.shownMax}`
    case 'destroy-set': return '💥 伏せ破壊'
    case 'destroy-token': return '🪓 従者狩り'
    case 'buff': return `💪 強化 +${intent.shownMin}〜${intent.shownMax}`
    case 'rally': return `📣 応援 +${intent.shownMin}〜${intent.shownMax}（味方全体）`
    case 'hex': return `🧿 呪い${inflictSuffix(intent)}`
    case 'heal': return `💚 回復 ${intent.shownMin}〜${intent.shownMax}（最も傷んだ味方）`
    case 'steal-gold': return `💰 盗み ${intent.shownMin}〜${intent.shownMax}G`
    case 'flee': return '🏃 逃走（倒すか打ち消せば阻止）'
    case 'rest': return '😮‍💨 隙だらけ'
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
        ? { text: `敵に${e.amount}ダメージ (HP減 ${e.hpLoss})${e.armorCut ? `【装甲で${e.armorCut}切り捨て】` : ''}`, cls: 'log-line' }
        : { text: `敵の攻撃${e.amount} → HP減 ${e.hpLoss}`, cls: 'log-bad' }
    case 'BlockGained': return { text: `${e.target === 'player' ? '自分' : '敵'}がブロック+${e.amount}`, cls: 'log-line' }
    case 'StrengthGained': return { text: `敵が強化 +${e.amount}（以降の攻撃に加算）`, cls: 'log-bad' }
    case 'IceBlockGained': return { text: `氷壁+${e.amount}（持ち越しブロック）`, cls: 'log-line' }
    case 'AetherGained': return { text: `霊気+${e.amount}`, cls: 'log-good' }
    case 'SpellEchoed': return { text: `🔁 反復: ${cardName(e.cardId)} の効果が2回解決`, cls: 'log-good' }
    case 'NecroFired': return { text: `💀 亡骸: ${cardName(e.cardId)} が消滅して効果が発火`, cls: 'log-good' }
    case 'NecroPlayed': return { text: `💀 亡骸プレイ: ${cardName(e.cardId)} (ゲームから取り除かれた)`, cls: 'log-line' }
    case 'AetherDischarged': return { text: `霊気${e.spent}を全て放出！`, cls: 'log-good' }
    case 'DiscountGained': return { text: `次にプレイするカードのコスト-${e.amount}`, cls: 'log-line' }
    case 'BurnApplied': return { text: `敵に延焼+${e.amount}`, cls: 'log-good' }
    case 'BurnTick': return { text: `延焼で敵に${e.amount}ダメージ`, cls: 'log-good' }
    case 'StatusInflicted':
      return { text: e.status === 'wound' ? `負傷${e.amount}枚が捨て札に混入した` : `${STATUS_LABEL[e.status]}${e.amount}を付与された`, cls: 'log-bad' }
    case 'RegenTicked': return { text: `敵は再生でHP+${e.amount}`, cls: 'log-bad' }
    case 'RegenBroken': return { text: '再生が止まった（このターンの削りが閾値を超えた）', cls: 'log-good' }
    case 'EnemyConfused': return { text: `敵に混乱+${e.amount}（攻撃が仲間に向かう）`, cls: 'log-good' }
    case 'ExposedApplied': return { text: `敵に急所+${e.amount}（次のダメージ${e.amount}回が+50%）`, cls: 'log-good' }
    case 'GrowthDischarged': return { text: `成長${e.spent}を全て放出した！`, cls: 'log-good' }
    case 'HpHealed': return { text: `HP+${e.amount}回復`, cls: 'log-good' }
    case 'CardsMilled': return { text: `山札の上${e.count}枚が忘却された（消滅）: ${(e.cardIds ?? []).map(cardName).join('・')}`, cls: 'log-line' }
    case 'EnemyWeakened': return { text: `敵を威圧（強化-${e.amount}）`, cls: 'log-good' }
    case 'ConfusedAttack':
      return { text: e.enemyIndex === e.targetIndex ? `混乱した敵は自分自身に${e.amount}ダメージ！` : `仲間割れ！ 混乱した敵が味方に${e.amount}ダメージ`, cls: 'log-good' }
    case 'BlockShattered': return { text: `敵のブロック${e.amount}を粉砕！`, cls: 'log-good' }
    case 'ImpulseDrawn': return { text: `衝動${e.count}枚（このターン限り）`, cls: 'log-line' }
    case 'HpLost': return { text: `自傷でHP-${e.amount}`, cls: 'log-bad' }
    case 'EnergyGained': return { text: `エナジー+${e.amount}（このターン）`, cls: 'log-line' }
    case 'MomentumAdded': return { text: `勢い+${e.amount}`, cls: 'log-good' }
    case 'PermanentPlayed': return { text: `置物を設置: ${cardName(e.cardId)}`, cls: 'log-good' }
    case 'CardExhausted': return { text: `消滅: ${cardName(e.cardId)}（この戦闘から除外）`, cls: 'log-line' }
    case 'BurnDischarged': return { text: `爆熱: 延焼${e.amount}を全て解き放った`, cls: 'log-line' }
    case 'TokenDestroyed': return { text: `従者狩り: ${cardName(e.cardId)}が倒された`, cls: 'log-line' }
    case 'ThornsReflected': return { text: `🦔 とげ反射: ${e.amount}（HP-${e.hpLoss}）`, cls: 'log-damage' }
    case 'GoldStolen': return { text: `💰 ${e.amount}G を盗まれた（逃がす前に倒せば取り返せる）`, cls: 'log-damage' }
    case 'EnemyFled': return { text: '🏃 敵が逃走した', cls: 'log-line' }
    case 'EnemyHealed': return { text: `💚 敵が回復 +${e.amount}`, cls: 'log-line' }
    case 'CardRetrieved': return { text: `回収: ${cardName(e.cardId)}（消滅置き場から手札へ）`, cls: 'log-line' }
    case 'CardPlayedFromExhaust': return { text: `直接プレイ: ${cardName(e.cardId)}（消滅置き場から）`, cls: 'log-line' }
    case 'CardsDiscarded': return { text: `コストとして捨てた: ${e.cardIds.map(cardName).join('、')}`, cls: 'log-line' }
    case 'EnergyMaxGained': return { text: `エナジー上限+${e.amount}`, cls: 'log-line' }
    case 'GrowthAdded': return { text: `成長+${e.amount}`, cls: 'log-good' }
    case 'ReactionTriggered': return { text: `リアクション発動: ${cardName(e.cardId)}`, cls: 'log-good' }
    case 'ReactionWhiffed': return { text: `空振り: ${cardName(e.cardId)}`, cls: 'log-line' }
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
