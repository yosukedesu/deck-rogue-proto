// src/sim/event-scan.ts — ジャーナルを TS エンジンで再生し、指定した出来事が「どのコマンドで・どんな順で・誰に」出たかを拾う純関数。
// scripts/dump-events.ts（実例の JSON）と scripts/find-event-journals.ts（実例を含むジャーナルの探索）が共用する。
// Godot 移植（dot）への実例（2026-10-12 dot の返信 3）。
import { replayStates } from '../engine/run.ts'
import type { RunCommand, RunJournal, RunState } from '../engine/run.ts'
import type { GameEvent } from '../engine/types.ts'

/** 赤の解凍（2026-10-10〜12）で増えた出来事 */
export const RED_EVENTS = ['Cascaded', 'CascadeRetargeted', 'ImpulseKindled', 'ImpulseReturned', 'ImpulseKept', 'BlazeIgnited', 'BlazeExtinguished'] as const

export interface EventExample {
  readonly commandIndex: number // 1 始まり（verify-goldens の「コマンドN」と同じ数え方）
  readonly commandIndex0: number // 0 始まり（journal.commands の添字）
  readonly command: RunCommand
  readonly matched: readonly string[] // この手で出た対象の出来事の種類
  readonly combat: { readonly turn: number | null; readonly enemies: readonly { index: number; id: string; hp: number; burn: number }[] } | null
  readonly events: readonly (GameEvent & { readonly n: number; readonly matched?: true })[] // この手で新しく出た出来事（eventLog の添字 n つき・順序どおり）
}

export interface EventScan {
  readonly commands: number
  readonly replayed: number
  readonly replayError: string | null
  readonly counts: Readonly<Record<string, number>>
  readonly firstAt: Readonly<Record<string, number | null>> // 種類 → 最初に出た commandIndex（1 始まり）
  readonly examples: readonly EventExample[]
}

/** 状態は不変で、出来事は末尾に足されるだけ。前の列が参照ごと先頭に残っていれば「続き」、残っていなければ新しい戦闘 */
function freshEvents(before: RunState, after: RunState): { start: number; fresh: readonly GameEvent[] } {
  const a = after.combat?.eventLog ?? []
  const b = before.combat?.eventLog ?? []
  if (a.length >= b.length && b.every((e, k) => a[k] === e)) return { start: b.length, fresh: a.slice(b.length) }
  return { start: 0, fresh: a }
}

function combatContext(s: RunState): EventExample['combat'] {
  const c = s.combat
  if (!c) return null
  return { turn: c.turn, enemies: c.enemies.map((e, index) => ({ index, id: e.enemyId, hp: e.hp, burn: e.burn })) }
}

/**
 * ジャーナルを再生し、wanted の出来事が出た手を集める。
 * maxPerType = 種類ごとの実例の上限（数は全部数える。Infinity で全部）
 */
export function scanEvents(journal: RunJournal, wanted: readonly string[], maxPerType = 3): EventScan {
  const want = new Set(wanted)
  const r = replayStates(journal)
  const counts: Record<string, number> = {}
  const firstAt: Record<string, number | null> = {}
  for (const t of wanted) { counts[t] = 0; firstAt[t] = null }
  const perType: Record<string, number> = {}
  const examples: EventExample[] = []
  for (let i = 0; i + 1 < r.states.length; i++) {
    const { start, fresh } = freshEvents(r.states[i], r.states[i + 1])
    const matched = [...new Set(fresh.map((e) => e.type).filter((t) => want.has(t)))]
    if (matched.length === 0) continue
    for (const t of matched) {
      counts[t] += fresh.filter((e) => e.type === t).length
      if (firstAt[t] === null) firstAt[t] = i + 1
    }
    if (!matched.some((t) => (perType[t] ?? 0) < maxPerType)) continue
    for (const t of matched) perType[t] = (perType[t] ?? 0) + 1
    examples.push({
      commandIndex: i + 1,
      commandIndex0: i,
      command: journal.commands[i],
      matched,
      combat: combatContext(r.states[i + 1]),
      events: fresh.map((e, k) => (want.has(e.type) ? { n: start + k, ...e, matched: true as const } : { n: start + k, ...e })),
    })
  }
  return { commands: journal.commands.length, replayed: r.states.length - 1, replayError: r.error, counts, firstAt, examples }
}

/** ゴールデン・セーブ・CLI の状態ファイルのどれからでもジャーナルを取り出す */
export function journalOf(raw: unknown): RunJournal | null {
  const d = raw as { journal?: RunJournal; run?: { journal?: RunJournal }; origin?: unknown; commands?: unknown }
  if (d.journal) return d.journal
  if (d.run?.journal) return d.run.journal
  if (d.origin && Array.isArray(d.commands)) return d as unknown as RunJournal
  return null
}
