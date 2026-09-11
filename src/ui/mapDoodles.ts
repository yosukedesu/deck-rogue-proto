// mapDoodles.ts — マップへの落書き (2026-09-12。Slay the Spire 2 の移植。Unity 版 MapDoodle.cs と同じ仕様)。
// 線の座標 (SVG のユーザー単位) を幕ごとに持つ UI 層の状態。エンジンには無い。セーブ (RunSaveFile.doodles) と
// localStorage のバックアップに同梱し、続きから/ファイル読み込みで戻る。新しいランで白紙。
import { useSyncExternalStore } from 'react'

export interface DoodleStroke {
  /** 0=紙色 (メモ) 1=朱 (強調) */
  readonly color: 0 | 1
  readonly points: readonly (readonly [number, number])[]
}
/** 0=紙ペン 1=朱ペン 2=消しゴム */
export type DoodleTool = 0 | 1 | 2
/** 幕 (文字列キー) → 線の列 */
export type DoodleBook = Readonly<Record<string, readonly DoodleStroke[]>>

interface Snap {
  readonly book: DoodleBook
  readonly penMode: boolean
  readonly tool: DoodleTool
}

const EMPTY: readonly DoodleStroke[] = []
let snap: Snap = { book: {}, penMode: false, tool: 0 }
const subs = new Set<() => void>()
function emit(next: Snap): void {
  snap = next
  for (const s of subs) s()
}

export function subscribeDoodles(fn: () => void): () => void {
  subs.add(fn)
  return () => {
    subs.delete(fn)
  }
}
export function getDoodleSnap(): Snap {
  return snap
}
export function getDoodleBook(): DoodleBook {
  return snap.book
}
export function strokesFor(act: number): readonly DoodleStroke[] {
  return snap.book[String(act)] ?? EMPTY
}
export function setStrokes(act: number, strokes: readonly DoodleStroke[]): void {
  emit({ ...snap, book: { ...snap.book, [String(act)]: strokes } })
}
export function clearDoodles(act: number): void {
  setStrokes(act, EMPTY)
}
/** ペンボタン: 同じ道具をもう一度押すと解除 */
export function togglePen(tool: DoodleTool): void {
  if (snap.penMode && snap.tool === tool) emit({ ...snap, penMode: false })
  else emit({ ...snap, penMode: true, tool })
}
export function setPenMode(on: boolean): void {
  emit({ ...snap, penMode: on })
}
export function resetDoodles(): void {
  emit({ book: {}, penMode: false, tool: 0 })
}
export function restoreDoodles(book: DoodleBook | undefined | null): void {
  emit({ book: book ?? {}, penMode: false, tool: 0 })
}
/** 中身のある幕だけ (セーブに同梱する形。空なら undefined) */
export function doodleBookForSave(): DoodleBook | undefined {
  const out: Record<string, readonly DoodleStroke[]> = {}
  for (const [k, v] of Object.entries(snap.book)) if (v.length > 0) out[k] = v
  return Object.keys(out).length > 0 ? out : undefined
}

export function useDoodles(): Snap {
  return useSyncExternalStore(subscribeDoodles, getDoodleSnap, getDoodleSnap)
}

/** 点 p が線 s の近く (r 以内) か。消しゴムの判定 */
export function strokeNear(s: DoodleStroke, p: readonly [number, number], r: number): boolean {
  const pts = s.points
  for (let i = 0; i < pts.length; i++) {
    const [x, y] = pts[i]
    if (Math.hypot(x - p[0], y - p[1]) <= r) return true
    if (i > 0) {
      const [ax, ay] = pts[i - 1]
      const dx = x - ax
      const dy = y - ay
      const len2 = dx * dx + dy * dy
      const t = len2 > 0 ? Math.max(0, Math.min(1, ((p[0] - ax) * dx + (p[1] - ay) * dy) / len2)) : 0
      if (Math.hypot(ax + dx * t - p[0], ay + dy * t - p[1]) <= r) return true
    }
  }
  return false
}
