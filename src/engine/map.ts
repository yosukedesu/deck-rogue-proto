// engine/map.ts — ランのマップ生成 (純ロジック。確定済みルール表「マップ」)
// StS式DAG・全体可視。16行 (行0〜15)・行15=ボス固定・強制焚き火行=5/10/14。
// 選択ノード: 工房×2 (行4〜12)・エリート×4 (行2〜13)。
// どのパスも戦闘数10〜12を構造的に保証する:
//   行0〜14の15ノードのうち強制焚き火3を除いた12が戦闘可能枠で、
//   パス上の工房 (0〜2個) だけが戦闘を置き換える → 12−(0..2) = 10〜12。パス列挙は不要。
// エッジは非交差 (連続区間分割) で、全ノードが開始から到達可能かつボスへ到達可能。
import { nextInt } from './rng.ts'
import type { RngState } from './types.ts'

export type MapNodeType = 'battle' | 'elite' | 'campfire' | 'workshop' | 'boss'

export interface MapNode {
  readonly type: MapNodeType
  /** battle / elite / boss のみ。焚き火・工房は null */
  readonly encounterId: string | null
  /** 次の行のどの列へ進めるか */
  readonly next: readonly number[]
}

export type RunMap = readonly (readonly MapNode[])[]

export const MAP_ROWS = 16
export const BOSS_ROW = MAP_ROWS - 1
/** 全パスが通る強制焚き火行 (14=ボス前休憩は本家準拠) */
export const FORCED_CAMPFIRE_ROWS: ReadonlySet<number> = new Set([5, 10, 14])
const WORKSHOP_COUNT = 2
const WORKSHOP_ROW_CANDIDATES = [4, 6, 7, 8, 9, 11, 12]
const ELITE_COUNT = 4
const ELITE_ROW_CANDIDATES = [2, 3, 4, 6, 7, 8, 9, 11, 12, 13]

/** 段階制の敵プール。行 → 抽選プール (ソロ敵IDと編成IDの混合) */
const ENEMY_TIERS: readonly (readonly string[])[] = [
  ['enemy_probe', 'enemy_wide_power', 'enc_probe_pair'], // 行0〜4 (Act1帯)
  ['enemy_set_wary', 'enemy_set_breaker', 'enemy_hexer', 'enemy_joker', 'enc_probe_trio', 'enc_joker_drummer'], // 行6〜9 (Act2帯)
  ['enemy_brute', 'enemy_wolf', 'enemy_moss', 'enemy_set_breaker', 'enc_wolf_drummer', 'enc_hexer_shadow', 'enc_breaker_hexer'], // 行11〜13 (Act3帯)
  ['enemy_brute', 'enemy_turtle', 'enemy_warden'], // 行15 (ボスは単体)
]

export function tierForRow(row: number): readonly string[] {
  if (row < 5) return ENEMY_TIERS[0]
  if (row < 10) return ENEMY_TIERS[1]
  if (row < BOSS_ROW) return ENEMY_TIERS[2]
  return ENEMY_TIERS[3]
}

/** シードからマップを決定的に生成する (同じシード = 同じマップ。リプレイ再現性) */
export function generateMap(rng0: RngState): readonly [RunMap, RngState] {
  let rng = rng0

  // 1. 行の幅: 行0=2 / 強制焚き火行・ボス行=1 / それ以外は2〜3
  const widths: number[] = []
  for (let r = 0; r < MAP_ROWS; r++) {
    if (r === BOSS_ROW || FORCED_CAMPFIRE_ROWS.has(r)) widths.push(1)
    else if (r === 0) widths.push(2)
    else {
      const [w, next] = nextInt(rng, 2, 3)
      rng = next
      widths.push(w)
    }
  }

  // 2. 特別ノードの行と列を決める (1行につき特別ノードは1つまで = 必ず戦闘の代替が同じ行にある)
  const pickRows = (candidates: readonly number[], count: number, used: Set<number>): number[] => {
    const rows: number[] = []
    const pool = candidates.filter((r) => !used.has(r))
    for (let i = 0; i < count && pool.length > 0; i++) {
      const [idx, next] = nextInt(rng, 0, pool.length - 1)
      rng = next
      rows.push(pool[idx])
      used.add(pool[idx])
      pool.splice(idx, 1)
    }
    return rows
  }
  const usedRows = new Set<number>()
  const workshopRows = pickRows(WORKSHOP_ROW_CANDIDATES, WORKSHOP_COUNT, usedRows)
  const eliteRows = pickRows(ELITE_ROW_CANDIDATES, ELITE_COUNT, usedRows)
  const specialCol = new Map<number, number>() // 行 → 特別ノードの列
  for (const r of [...workshopRows, ...eliteRows]) {
    const [c, next] = nextInt(rng, 0, widths[r] - 1)
    rng = next
    specialCol.set(r, c)
  }

  // 3. エッジ: 連続区間分割 (非交差・全ノード到達保証) + 確率で隣へ1本追加 (ルートの選択肢)
  const edges: number[][][] = [] // edges[row][col] = 次の行の列リスト
  for (let r = 0; r < MAP_ROWS - 1; r++) {
    const a = widths[r]
    const b = widths[r + 1]
    const rowEdges: number[][] = []
    for (let i = 0; i < a; i++) {
      const lo = Math.floor((i * b) / a)
      const hi = Math.floor(((i + 1) * b - 1) / a)
      const next: number[] = []
      for (let c = lo; c <= hi; c++) next.push(c)
      // 隣の区間の先頭にも1本 (50%)。区間は連続なので追加しても交差しない
      if (hi + 1 < b) {
        const [coin, nrng] = nextInt(rng, 0, 1)
        rng = nrng
        if (coin === 1) next.push(hi + 1)
      }
      rowEdges.push(next)
    }
    edges.push(rowEdges)
  }

  // 4. ノードタイプと敵の割り当て (直前2行と同じ敵は避ける)
  const recentEnemies: string[][] = []
  const map: MapNode[][] = []
  for (let r = 0; r < MAP_ROWS; r++) {
    const rowNodes: MapNode[] = []
    const rowEnemies: string[] = []
    for (let c = 0; c < widths[r]; c++) {
      const type: MapNodeType =
        r === BOSS_ROW
          ? 'boss'
          : FORCED_CAMPFIRE_ROWS.has(r)
            ? 'campfire'
            : workshopRows.includes(r) && specialCol.get(r) === c
              ? 'workshop'
              : eliteRows.includes(r) && specialCol.get(r) === c
                ? 'elite'
                : 'battle'
      let encounterId: string | null = null
      if (type === 'battle' || type === 'elite' || type === 'boss') {
        const pool = tierForRow(r)
        const recent = [...recentEnemies.slice(-2).flat(), ...rowEnemies]
        const fresh = pool.filter((id) => !recent.includes(id))
        const candidates = fresh.length > 0 ? fresh : pool
        const [idx, next] = nextInt(rng, 0, candidates.length - 1)
        rng = next
        encounterId = candidates[idx]
        rowEnemies.push(encounterId)
      }
      rowNodes.push({ type, encounterId, next: r < MAP_ROWS - 1 ? edges[r][c] : [] })
    }
    recentEnemies.push(rowEnemies)
    map.push(rowNodes)
  }
  return [map, rng]
}
