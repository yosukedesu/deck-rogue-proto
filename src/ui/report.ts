import { allCards, allEnemies, encounterName, getEnemyDef, getLeaderDef } from '../engine/content.ts'
import { effectiveIntent, effectiveCost, isPlayableFromHand } from '../engine/effects.ts'
import type { RunState } from '../engine/run.ts'
import type { CardInstance, GameEvent, GameState } from '../engine/types.ts'
import { cardName, intentText, logLine } from './log.ts'
export { STATUS_LABEL, inflictSuffix, intentText, cardName, logLine } from './log.ts'
export type { LogLine } from './log.ts'

export interface BattleArchive {
  readonly battleNo: number
  readonly enemyId: string
  readonly elite: boolean
  readonly result: 'won' | 'lost'
  readonly turns: number
  readonly hpBefore: number
  readonly hpAfter: number
  readonly deckSize: number
  readonly lines: readonly string[]
}

/** 1戦闘あたりの保管ログ行数の上限 (10戦ぶんでもファイルが読める範囲に収める) */
const ARCHIVE_LINES_CAP = 300

/** 決着した combat を保管形式に変換する */
export function archiveBattle(
  combat: GameState,
  battleNo: number,
  enemyId: string,
  elite: boolean,
  hpBefore: number,
  deckSize: number,
): BattleArchive {
  const all = combat.eventLog.map(reportLine).filter((x): x is string => x !== null)
  return {
    battleNo,
    enemyId,
    elite,
    result: combat.phase === 'won' ? 'won' : 'lost',
    turns: combat.turn,
    hpBefore,
    hpAfter: combat.player.hp,
    deckSize,
    lines: all.length > ARCHIVE_LINES_CAP ? all.slice(-ARCHIVE_LINES_CAP) : all,
  }
}

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

export function buildReport(
  run: RunState | null,
  state: GameState | null,
  history: readonly BattleArchive[] = [],
  note = '',
): string {
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
  if (history.length > 0) {
    L.push(`## これまでの戦闘（${history.length}戦）`)
    L.push('')
    L.push('| # | 敵 | 結果 | ターン | HP |')
    L.push('|---|---|---|---|---|')
    for (const h of history) {
      L.push(
        `| ${h.battleNo} | ${encounterName(h.enemyId)}${h.elite ? '（強個体）' : ''} | ${h.result === 'won' ? '勝利' : '敗北'} | ${h.turns} | ${h.hpBefore}→${h.hpAfter} |`,
      )
    }
    L.push('')
    for (const h of history) {
      L.push(
        `### ${h.battleNo}戦目 ${encounterName(h.enemyId)}${h.elite ? '（強個体）' : ''} — ${h.result === 'won' ? '勝利' : '敗北'} / ${h.turns}ターン / HP ${h.hpBefore}→${h.hpAfter} / デッキ${h.deckSize}枚`,
      )
      L.push(...h.lines)
      L.push('')
    }
  }
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
export function saveReport(
  run: RunState | null,
  state: GameState | null,
  history: readonly BattleArchive[] = [],
): void {
  const text = buildReport(run, state, history)
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
