// プレイテストの状況をAIへ渡すためのテキスト書き出し (2026-08-26)。
// ui/ 層に置く純関数。engine には触らない。ダウンロードは App 側の1関数だけがDOMを使う。
import { allCards, allEnemies, encounterName, getCardDef, getEnemyDef, getLeaderDef } from '../engine/content.ts'
import { effectiveIntent, effectiveCost, isPlayableFromHand } from '../engine/effects.ts'
import type { RunState } from '../engine/run.ts'
import type { CardInstance, EnemyIntent, GameEvent, GameState } from '../engine/types.ts'

export const STATUS_LABEL: Record<string, string> = { weak: '弱体', vulnerable: '脆弱', wound: '負傷', junk: 'がらくた' }

export function inflictSuffix(intent: EnemyIntent): string {
  if (!intent.inflict) return ''
  return ` ＋${STATUS_LABEL[intent.inflict.status]}${intent.inflict.amount}`
}

export function intentText(intent: EnemyIntent | null): string {
  if (!intent) return '---'
  switch (intent.kind) {
    case 'attack': {
      const hits = (intent.hits ?? 1) > 1 ? `×${intent.hits}` : ''
      return `⚔️ 攻撃 ${intent.shownMin}〜${intent.shownMax}${hits}${inflictSuffix(intent)}`
    }
    case 'defend': return `🛡️ 防御 ${intent.shownMin}〜${intent.shownMax}`
    case 'destroy-set': return '💥 伏せ破壊'
    case 'destroy-token': return '🪓 従者狩り'
    case 'buff': return `💪 強化 +${intent.shownMin}〜${intent.shownMax}`
    case 'rally': return `📣 応援 +${intent.shownMin}〜${intent.shownMax}（味方全体）`
    case 'hex': return `🧿 呪い${inflictSuffix(intent)}`
  }
}

export function cardName(cardId: string): string {
  return getCardDef(cardId).name
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
    case 'EnemyIntentDeclared': return { text: `敵の意図: ${intentText(e.intent)}`, cls: 'log-line' }
    case 'EnemyActionExecuting':
    case 'EnemyActionResolved': return null
    case 'ActionNegated': return { text: '敵の行動は打ち消された！', cls: 'log-good' }
    case 'DamageDealt':
      return e.source === 'player'
        ? { text: `敵に${e.amount}ダメージ (HP減 ${e.hpLoss})`, cls: 'log-line' }
        : { text: `敵の攻撃${e.amount} → HP減 ${e.hpLoss}`, cls: 'log-bad' }
    case 'BlockGained': return { text: `${e.target === 'player' ? '自分' : '敵'}がブロック+${e.amount}`, cls: 'log-line' }
    case 'StrengthGained': return { text: `敵が強化 +${e.amount}（以降の攻撃に加算）`, cls: 'log-bad' }
    case 'IceBlockGained': return { text: `氷壁+${e.amount}（持ち越しブロック）`, cls: 'log-line' }
    case 'AetherGained': return { text: `霊気+${e.amount}`, cls: 'log-good' }
    case 'AetherDischarged': return { text: `霊気${e.spent}を全て放出！`, cls: 'log-good' }
    case 'DiscountGained': return { text: `次にプレイするカードのコスト-${e.amount}`, cls: 'log-line' }
    case 'BurnApplied': return { text: `敵に延焼+${e.amount}`, cls: 'log-good' }
    case 'BurnTick': return { text: `延焼で敵に${e.amount}ダメージ`, cls: 'log-good' }
    case 'StatusInflicted':
      return { text: e.status === 'wound' ? `負傷${e.amount}枚が捨て札に混入した` : `${STATUS_LABEL[e.status]}${e.amount}を付与された`, cls: 'log-bad' }
    case 'RegenTicked': return { text: `敵は再生でHP+${e.amount}`, cls: 'log-bad' }
    case 'EnemyConfused': return { text: `敵に混乱+${e.amount}（攻撃が仲間に向かう）`, cls: 'log-good' }
    case 'ExposedApplied': return { text: `敵に急所+${e.amount}（次のダメージ${e.amount}回が+50%）`, cls: 'log-good' }
    case 'GrowthDischarged': return { text: `成長${e.spent}を全て放出した！`, cls: 'log-good' }
    case 'HpHealed': return { text: `HP+${e.amount}回復`, cls: 'log-good' }
    case 'CardsMilled': return { text: `山札の上${e.count}枚が忘却された（消滅）`, cls: 'log-line' }
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

/** カードデータの指紋。エクスポートを読む側が「同じビルドか」を判定する */
function dataFingerprint(): string {
  let h = 5381
  for (const c of allCards) {
    const s = `${c.id}:${c.cost}:${c.effects.length}`
    for (let i = 0; i < s.length; i++) h = ((h * 33) ^ s.charCodeAt(i)) >>> 0
  }
  return `cards${allCards.length}/enemies${allEnemies.length}/${h.toString(36)}`
}

const LOG_CAP = 600

const names = (cs: readonly CardInstance[]) => (cs.length ? cs.map((c) => c.def.name).join('、') : '（なし）')

const KIND_LABEL: Record<string, string> = {
  attack: '攻撃', defend: '防御', 'destroy-set': '伏せ破壊',
  'destroy-token': '従者狩り', buff: '強化', rally: '応援', hex: '呪い',
}

/**
 * ログ行。敵行動フック (Executing/Resolved) は全行動で必ず出る定型ノイズなので描かない。
 * 「窓が開いたか」は ReactionTriggered (発動) と ReactionHeld (温存) の和で厳密に分かる
 */
function reportLine(e: GameEvent): string | null {
  if (e.type === 'EnemyPhaseEnded') return '敵フェーズ終了'
  return logLine(e)?.text ?? null
}

function renderBoard(s: GameState): string[] {
  const p = s.player
  const out: string[] = []
  const st = [
    p.growth ? `成長${p.growth}` : '', p.momentum ? `勢い${p.momentum}` : '',
    p.iceBlock ? `氷壁${p.iceBlock}` : '', p.aether ? `霊気${p.aether}` : '',
    p.nextCardDiscount ? `次コスト-${p.nextCardDiscount}` : '', p.weak ? `弱体${p.weak}` : '',
    p.vulnerable ? `脆弱${p.vulnerable}` : '',
  ].filter(Boolean).join(' ')
  out.push(`自分: HP ${p.hp}/${p.maxHp} ブロック${p.block} エナジー${p.energy}/${p.energyMax} ${st}`)
  out.push(`手札(${p.hand.length}): ${p.hand.map((c) => {
    const cost = effectiveCost(s, c)
    const ok = isPlayableFromHand(c) && cost <= p.energy
    return `${c.def.name}(${cost})${ok ? '' : '✕'}`
  }).join('、') || '（なし）'}`)
  out.push(`伏せ(${p.setCards.length}/${p.setSlots}): ${names(p.setCards)}`)
  out.push(`置物: ${names(p.permanents)}`)
  out.push(`山札${p.drawPile.length} / 捨札${p.discardPile.length} / 消滅${p.exhaustPile.length}`)
  s.enemies.forEach((e, i) => {
    if (e.hp <= 0) { out.push(`敵${i + 1} ${getEnemyDef(e.enemyId).name}: 撃破済み`); return }
    const dbg = [e.strength ? `強化${e.strength > 0 ? '+' : ''}${e.strength}` : '', e.block ? `ブロック${e.block}` : '',
      e.burn ? `延焼${e.burn}` : '', e.confusion ? `混乱${e.confusion}` : '', e.exposed ? `急所${e.exposed}` : '']
      .filter(Boolean).join(' ')
    out.push(`敵${i + 1} ${getEnemyDef(e.enemyId).name}: HP ${e.hp}/${e.maxHp} ${dbg} → ${intentText(effectiveIntent(s, i))}`)
  })
  if (s.pendingWindow) {
    const w = s.pendingWindow
    const en = s.enemies[w.enemyIndex]
    out.push(`★確認ウィンドウ待ち: 敵${w.enemyIndex + 1} ${getEnemyDef(en.enemyId).name} / ${w.stage}窓 / 実値 ${en.intent?.actual}（宣言 ${en.intent?.shownMin}〜${en.intent?.shownMax}）`)
  }
  return out
}

/** スナップショットの eventLog 上限。engine は eventLog を読まないので切り詰めても再開挙動は不変 */
const SNAPSHOT_LOG_CAP = 400

function trimLog(s: GameState): GameState {
  if (s.eventLog.length <= SNAPSHOT_LOG_CAP) return s
  // 先頭の CombatStarted は編成IDの唯一の記録なので必ず残す
  return { ...s, eventLog: [s.eventLog[0], ...s.eventLog.slice(-(SNAPSHOT_LOG_CAP - 1))] }
}

export function buildReport(run: RunState | null, state: GameState | null, note = ''): string {
  const s = run ? run.combat : state
  const L: string[] = []
  L.push(`# プレイ状況レポート`)
  L.push(`書き出し: ${new Date().toISOString()} / データ指紋: ${dataFingerprint()}`)
  if (note) L.push(`メモ: ${note}`)
  L.push('')
  L.push('## いまの状況')
  if (run) {
    const leader = getLeaderDef(run.leaderId)
    L.push(`ラン ${leader.name}（${run.leaderId}） / seed ${run.seed} / mode ${run.mode}`)
    L.push(`進行: ${run.phase} / 戦闘 ${Math.min(run.battleIndex + 1, run.enemyIds.length)}/${run.enemyIds.length}${run.currentElite ? '（強個体）' : ''} / HP ${run.hp}/${run.maxHp} / デッキ${run.deck.length}枚`)
    L.push(`敵の並び: ${run.enemyIds.map((id, i) => `${i + 1}.${encounterName(id)}${i < run.battleIndex ? '✓' : ''}`).join(' ')}`)
    L.push(`レリック: ${run.relics.length ? run.relics.join('、') : '（なし）'}`)
    L.push(`ピック履歴: ${run.picks.length ? run.picks.map(cardName).join('、') : '（なし）'}`)
    if (run.rewardOptions) L.push(`報酬候補（いま提示中）: ${run.rewardOptions.map(cardName).join(' / ')}`)
    if (run.relicOptions) L.push(`レリック候補（いま提示中）: ${run.relicOptions.join(' / ')}`)
    L.push(`デッキ全体: ${names(run.deck)}`)
  } else if (state) {
    L.push(`単発検証 / mode ${state.reactionMode} / seed ${state.rng.seed}`)
  } else {
    L.push('（戦闘・ランともに未開始）')
  }
  L.push('')
  if (s) {
    L.push(`## 盤面（ターン ${s.turn} / ${s.phase}）`)
    L.push(...renderBoard(s))
    L.push('')
    const all = s.eventLog.map(reportLine).filter((x): x is string => x !== null)
    const lines = all.length > LOG_CAP ? all.slice(-LOG_CAP) : all
    L.push(`## この戦闘のログ（${lines.length}行${all.length > lines.length ? ` / 冒頭${all.length - lines.length}行は省略` : ''}）`)
    L.push(...lines)
  } else {
    L.push('## 盤面')
    L.push('（進行中の戦闘なし）')
  }
  L.push('')
  L.push('## 再開用スナップショット（sim/play.ts 互換。`npx tsx src/sim/play.ts show` で開ける）')
  L.push('```json')
  const snapshot = run
    ? { kind: 'run' as const, run: run.combat ? { ...run, combat: trimLog(run.combat) } : run,
        logIndex: run.combat ? trimLog(run.combat).eventLog.length : 0 }
    : { kind: 'battle' as const, battle: state ? trimLog(state) : null,
        logIndex: state ? trimLog(state).eventLog.length : 0 }
  L.push(JSON.stringify(snapshot))
  L.push('```')
  return L.join('\n')
}

/**
 * 書き出し実行。ダウンロードとクリップボードコピーを両方やる
 * (スマホの Safari は a[download] が不安定なため、貼り付けでも渡せるようにする)
 */
export function saveReport(run: RunState | null, state: GameState | null): void {
  const text = buildReport(run, state)
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  const stamp = `${d.getFullYear()}${pad(d.getMonth() + 1)}${pad(d.getDate())}-${pad(d.getHours())}${pad(d.getMinutes())}`
  const url = URL.createObjectURL(new Blob([text], { type: 'text/markdown' }))
  const a = document.createElement('a')
  a.href = url
  a.download = `play-${stamp}.md`
  a.click()
  URL.revokeObjectURL(url)
  navigator.clipboard?.writeText(text).catch(() => {})
}
