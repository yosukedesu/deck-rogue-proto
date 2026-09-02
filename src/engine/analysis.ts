// engine/analysis.ts — 戦闘ログとラン履歴の計測 (純関数)。プレイレポートの「機械可読ブロック」と scripts/analyze-run.ts が使う。
// 2026-09-03 ユーザー質問「md はあなたが分析しやすい形式か」→ 集計値を書き出し時に同梱し、レポートごとに regex で再導出しない。
import type { GameEvent } from './types.ts'
import { currentNode, replayStates, type RunJournal, type RunState } from './run.ts'

export interface TurnMetrics {
  readonly turn: number
  /** 自ターン中にプレイヤーが与えたダメージ (HP損失ベース) */
  readonly dealt: number
  /** 敵フェーズ中に返し (リアクション・置物) で与えたダメージ */
  readonly counter: number
  /** プレイヤーが受けたHP損失 (敵フェーズ) */
  readonly taken: number
  readonly plays: number
  readonly sets: number
  readonly fires: number
  readonly holds: number
}

export interface BattleMetrics {
  readonly turns: number
  /** 1ターン目に出したダメージ (HP損失ベース) = 曲線の物差し「初手火力」 */
  readonly t1Damage: number
  readonly totalDealt: number
  readonly totalTaken: number
  readonly maxTurnDamage: number
  readonly sets: number
  readonly fires: number
  readonly holds: number
  readonly perTurn: readonly TurnMetrics[]
}

export function battleMetrics(log: readonly GameEvent[]): BattleMetrics {
  const turns = new Map<number, { dealt: number; counter: number; taken: number; plays: number; sets: number; fires: number; holds: number }>()
  let cur = 0
  let enemyPhase = false
  const at = (t: number) => {
    let m = turns.get(t)
    if (!m) turns.set(t, (m = { dealt: 0, counter: 0, taken: 0, plays: 0, sets: 0, fires: 0, holds: 0 }))
    return m
  }
  for (const e of log) {
    switch (e.type) {
      case 'TurnStarted': cur = e.turn; enemyPhase = false; at(cur); break
      case 'TurnEnded': enemyPhase = true; break
      case 'DamageDealt':
        if (e.source === 'player') {
          if (enemyPhase) at(cur).counter += e.hpLoss
          else at(cur).dealt += e.hpLoss
        } else at(cur).taken += e.hpLoss
        break
      case 'CardPlayed': at(cur).plays++; break
      case 'CardSet': at(cur).sets++; break
      case 'ReactionTriggered': at(cur).fires++; break
      case 'ReactionHeld': at(cur).holds++; break
      default: break
    }
  }
  const perTurn = [...turns.entries()].sort((a, b) => a[0] - b[0]).map(([turn, m]) => ({ turn, ...m }))
  const sum = (k: keyof TurnMetrics) => perTurn.reduce((a, m) => a + (m[k] as number), 0)
  return {
    turns: perTurn.length,
    t1Damage: perTurn[0]?.dealt ?? 0,
    totalDealt: sum('dealt') + sum('counter'),
    totalTaken: sum('taken'),
    maxTurnDamage: perTurn.reduce((a, m) => Math.max(a, m.dealt), 0),
    sets: sum('sets'),
    fires: sum('fires'),
    holds: sum('holds'),
    perTurn,
  }
}

export interface BattleRow {
  readonly battleNo: number
  readonly act: number
  readonly enemyId: string
  readonly elite: boolean
  readonly boss: boolean
  readonly result: 'won' | 'lost'
  readonly hpBefore: number
  readonly hpAfter: number
  readonly metrics: BattleMetrics | null
  readonly rating?: { readonly strength?: number; readonly fun?: number; readonly note?: string; readonly lossFeel?: string }
}

export interface ActSummary {
  readonly act: number
  readonly battles: number
  readonly normalTurnsAvg: number | null
  readonly bossTurns: number | null
  readonly eliteTurns: readonly number[]
  readonly t1Damage: { readonly min: number; readonly median: number; readonly max: number } | null
  readonly hpLost: number
  readonly sets: number
  readonly fires: number
  readonly funAvg: number | null
  readonly strengthAvg: number | null
  /** 2ターン目以降に敵からHP損失があった、または伏せの発動/温存があった戦闘の割合 = 「2ターン目以降に何か起きた」 */
  readonly lateActionRate: number | null
}

const median = (xs: readonly number[]): number => {
  const s = [...xs].sort((a, b) => a - b)
  return s.length === 0 ? 0 : s[s.length >> 1]
}
const avg = (xs: readonly number[]): number | null => (xs.length === 0 ? null : Math.round((xs.reduce((a, b) => a + b, 0) / xs.length) * 100) / 100)

export function actSummaries(rows: readonly BattleRow[]): readonly ActSummary[] {
  const acts = [...new Set(rows.map((r) => r.act))].sort((a, b) => a - b)
  return acts.map((act) => {
    const rs = rows.filter((r) => r.act === act)
    const normals = rs.filter((r) => !r.boss && !r.elite && r.metrics)
    const t1 = rs.filter((r) => r.metrics).map((r) => r.metrics!.t1Damage)
    const late = rs.filter((r) => r.metrics)
    const lateHit = late.filter((r) => r.metrics!.perTurn.slice(1).some((m) => m.taken > 0 || m.fires > 0 || m.holds > 0)).length
    return {
      act,
      battles: rs.length,
      normalTurnsAvg: avg(normals.map((r) => r.metrics!.turns)),
      bossTurns: rs.find((r) => r.boss && r.metrics)?.metrics!.turns ?? null,
      eliteTurns: rs.filter((r) => r.elite && r.metrics).map((r) => r.metrics!.turns),
      t1Damage: t1.length > 0 ? { min: Math.min(...t1), median: median(t1), max: Math.max(...t1) } : null,
      hpLost: rs.reduce((a, r) => a + Math.max(0, r.hpBefore - r.hpAfter), 0),
      sets: rs.reduce((a, r) => a + (r.metrics?.sets ?? 0), 0),
      fires: rs.reduce((a, r) => a + (r.metrics?.fires ?? 0), 0),
      funAvg: avg(rs.map((r) => r.rating?.fun).filter((x): x is number => typeof x === 'number')),
      strengthAvg: avg(rs.map((r) => r.rating?.strength).filter((x): x is number => typeof x === 'number')),
      lateActionRate: late.length > 0 ? Math.round((lateHit / late.length) * 100) / 100 : null,
    }
  })
}

/** レポートの「計測（機械可読）」ブロックの形 (ui/report.ts の metricsExport と共有する契約) */
export interface MetricsExport {
  readonly schema: string
  readonly fingerprint?: string
  readonly run?: { readonly act: number; readonly battlesWon: number; readonly hp: number; readonly deckSize: number } | null
  readonly battles: readonly BattleRow[]
  readonly acts: readonly ActSummary[]
}

/** 物差し (2026-09-03 曲線パッケージの合格基準): 良いデッキで通常戦4〜6T・ボス6〜10T */
export const TURN_TARGET = { normal: [4, 6] as const, boss: [6, 10] as const }

const pct = (n: number, d: number): string => (d === 0 ? '-' : `${Math.round((n / d) * 100)}%`)

/**
 * 計測ブロックを人間/AIが読む表に展開する (純関数・I/Oなし)。
 * nameOf は敵IDの表示名解決 (省略時はIDのまま)
 */
export function formatAnalysis(exp: MetricsExport, nameOf: (id: string) => string = (id) => id): string {
  const L: string[] = []
  const rows = exp.battles
  const withM = rows.filter((r) => r.metrics)
  const normals = withM.filter((r) => !r.boss && !r.elite)
  L.push(`# ラン計測 (${rows.length}戦 / 計測あり${withM.length}戦${exp.fingerprint ? ` / ${exp.fingerprint}` : ''})`)
  L.push('')
  L.push('## 物差しとの比較')
  const short = normals.filter((r) => r.metrics!.turns <= 2).length
  const inBand = normals.filter((r) => r.metrics!.turns >= TURN_TARGET.normal[0] && r.metrics!.turns <= TURN_TARGET.normal[1]).length
  const long = normals.filter((r) => r.metrics!.turns > TURN_TARGET.normal[1]).length
  L.push(`- 通常戦 ${normals.length}戦: ≤2T ${short} (${pct(short, normals.length)}) / 3T ${normals.length - short - inBand - long} / 4〜6T(目標) ${inBand} (${pct(inBand, normals.length)}) / 7T+ ${long}`)
  const bosses = withM.filter((r) => r.boss)
  if (bosses.length > 0) L.push(`- ボス: ${bosses.map((r) => `${nameOf(r.enemyId)} ${r.metrics!.turns}T`).join(' / ')} (目標${TURN_TARGET.boss[0]}〜${TURN_TARGET.boss[1]}T)`)
  const totalSets = withM.reduce((a, r) => a + r.metrics!.sets, 0)
  const totalFires = withM.reduce((a, r) => a + r.metrics!.fires, 0)
  const lateFires = withM.reduce((a, r) => a + r.metrics!.perTurn.slice(1).reduce((b, m) => b + m.fires, 0), 0)
  L.push(`- 伏せ ${totalSets} / 発動 ${totalFires} (${pct(totalFires, totalSets)}) / うち2ターン目以降の発動 ${lateFires} (${pct(lateFires, totalFires)})`)
  const t1 = withM.map((r) => r.metrics!.t1Damage).sort((a, b) => a - b)
  if (t1.length > 0) L.push(`- 初手(T1)火力: 最小${t1[0]} / 中央${t1[t1.length >> 1]} / 最大${t1[t1.length - 1]}`)
  const zeroTaken = withM.filter((r) => r.metrics!.totalTaken === 0).length
  L.push(`- 被ダメ0の戦闘 ${zeroTaken} (${pct(zeroTaken, withM.length)})`)
  L.push('')
  L.push('## 幕別')
  L.push('| 幕 | 戦 | 通常avgT | ボスT | エリートT | T1火力 min/med/max | HP損失 | 伏せ/発動 | 2T目以降に何か起きた | 強さ | 面白さ |')
  L.push('|---|---|---|---|---|---|---|---|---|---|---|')
  for (const a of exp.acts) {
    L.push(`| ${a.act || '?'} | ${a.battles} | ${a.normalTurnsAvg ?? '-'} | ${a.bossTurns ?? '-'} | ${a.eliteTurns.join('/') || '-'} | ${a.t1Damage ? `${a.t1Damage.min}/${a.t1Damage.median}/${a.t1Damage.max}` : '-'} | ${a.hpLost} | ${a.sets}/${a.fires} | ${a.lateActionRate === null ? '-' : `${Math.round(a.lateActionRate * 100)}%`} | ${a.strengthAvg ?? '-'} | ${a.funAvg ?? '-'} |`)
  }
  L.push('')
  L.push('## 戦闘別')
  L.push('| # | 幕 | 敵 | 種 | 結果 | T | T1 | 最大T火力 | 与/返し/被 | HP | 伏せ/発動/温存 | 強さ | 面白さ | 敗因 | メモ |')
  L.push('|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|')
  for (const r of rows) {
    const m = r.metrics
    const counter = m ? m.perTurn.reduce((a, t) => a + t.counter, 0) : 0
    L.push(`| ${r.battleNo} | ${r.act || '?'} | ${nameOf(r.enemyId)} | ${r.boss ? 'ボス' : r.elite ? 'エリート' : '通常'} | ${r.result === 'won' ? '勝' : '敗'} | ${m?.turns ?? '-'} | ${m?.t1Damage ?? '-'} | ${m?.maxTurnDamage ?? '-'} | ${m ? `${m.totalDealt - counter}/${counter}/${m.totalTaken}` : '-'} | ${r.hpBefore}→${r.hpAfter} | ${m ? `${m.sets}/${m.fires}/${m.holds}` : '-'} | ${r.rating?.strength ?? ''} | ${r.rating?.fun ?? ''} | ${r.rating?.lossFeel ?? ''} | ${(r.rating?.note ?? '').replace(/\|/g, '/')} |`)
  }
  return L.join('\n')
}

/**
 * リプレイ・ジャーナルから戦闘行を復元する (CLI/Opusランの状態ファイル用。2026-09-03)。
 * ブラウザは決着時に BattleArchive を積むが、CLIは history を持たない——決定性 (同じシード+同じコマンド列)
 * で全状態を再生し、combat の phase が won/lost に変わった瞬間を拾う。UI (App.tsx) の archiveBattle と同じ判定。
 * データ変更で再現が分岐した場合は途中までの行と error を返す (throwしない)
 */
export function battleRowsFromJournal(journal: RunJournal): { rows: BattleRow[]; error: string | null } {
  const { states, error } = replayStates(journal)
  const rows: BattleRow[] = []
  for (let i = 1; i < states.length; i++) {
    const prev: RunState = states[i - 1]
    const next: RunState = states[i]
    const c = next.combat
    if (!c) continue
    const ended = c.phase === 'won' || c.phase === 'lost'
    if (!ended || !prev.combat || prev.combat.phase === c.phase) continue
    const started = c.eventLog.find((e) => e.type === 'CombatStarted')
    rows.push({
      battleNo: prev.battlesWon + 1,
      act: prev.act,
      enemyId: (started?.type === 'CombatStarted' ? started.enemyId : null) ?? currentNode(prev)?.encounterId ?? 'unknown',
      elite: prev.currentElite,
      boss: currentNode(prev)?.type === 'boss',
      result: c.phase === 'won' ? 'won' : 'lost',
      hpBefore: prev.hp,
      hpAfter: c.player.hp,
      metrics: battleMetrics(c.eventLog),
    })
  }
  return { rows, error }
}
