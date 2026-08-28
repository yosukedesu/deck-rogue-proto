// engine/map.ts — ランのマップ生成 (純ロジック。確定済みルール表「マップ」)
// StS式DAG・全体可視。16行 (行0〜15)・行15=ボス固定・強制焚き火行=5/10/14。
// 選択ノード: 工房×2・ショップ×1 (工房と同じ行に択一)・?マス×2・エリート×4 (行は隣接させない)。
// どのパスも戦闘数10〜12を保証する:
//   行0〜14の15ノードのうち強制焚き火3を除いた12が戦闘可能枠で、
//   パス上の非戦闘の選択ノード (工房/ショップ/?) が最大2つになるよう DP で検証しながら生成する
//   → 12−(0..2) = 10〜12。1ランで寄れる非戦闘スポットは最大2箇所 = どこに寄るかの悩みを作る。
// エッジは非交差 (連続区間分割) で、全ノードが開始から到達可能かつボスへ到達可能。
import { nextInt } from './rng.ts'
import type { RngState } from './types.ts'

export type MapNodeType = 'battle' | 'elite' | 'campfire' | 'workshop' | 'shop' | 'event' | 'boss'

export interface MapNode {
  readonly type: MapNodeType
  /** battle / elite / boss のみ。それ以外は null */
  readonly encounterId: string | null
  /** event のみ。生成時に確定 (リプレイ再現)。表示は「?」で中身は入るまで伏せる */
  readonly eventId: string | null
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
const EVENT_COUNT = 2
const EVENT_ROW_CANDIDATES = [2, 3, 4, 6, 7, 8, 9, 11, 12, 13]
const ELITE_COUNT = 4
const ELITE_ROW_CANDIDATES = [2, 3, 4, 6, 7, 8, 9, 11, 12, 13]
/** 生成リトライの上限 (DP検証で「非戦闘ピック最大2」を満たすまで配置し直す) */
const MAX_PLACEMENT_TRIES = 300

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
export function generateMap(
  rng0: RngState,
  eventPool: readonly string[],
): readonly [RunMap, RngState] {
  let rng = rng0
  // 配置リトライ: 「1パスで踏める非戦闘の選択ノードは最大2」を DP で検証し、満たすまで作り直す。
  // 最終試行では ?単独行を落とす (特別行が2つになり構造的に≤2が保証される保険)
  for (let attempt = 0; attempt <= MAX_PLACEMENT_TRIES; attempt++) {
    const dropStandaloneEvent = attempt === MAX_PLACEMENT_TRIES

    // 1. 特別行の選定 (1行に特別ノードは工房行の同居を除き1つまで)
    const usedRows = new Set<number>()
    const pickRows = (
      candidates: readonly number[],
      count: number,
      nonAdjacent: boolean,
    ): number[] => {
      const rows: number[] = []
      let pool = candidates.filter((r) => !usedRows.has(r))
      for (let i = 0; i < count && pool.length > 0; i++) {
        const [idx, next] = nextInt(rng, 0, pool.length - 1)
        rng = next
        const r = pool[idx]
        rows.push(r)
        usedRows.add(r)
        pool = pool.filter((x) => x !== r && (!nonAdjacent || Math.abs(x - r) > 1))
      }
      return rows
    }
    const workshopRows = pickRows(WORKSHOP_ROW_CANDIDATES, WORKSHOP_COUNT, false)
    const shopRow = workshopRows[0] // ショップは工房と同じ行に択一 (工房|ショップ|戦闘)
    const eventWithWorkshopRow = workshopRows[1] // 2つ目の工房行に?が同居 (工房|?|戦闘)
    const standaloneEventRows = dropStandaloneEvent
      ? []
      : pickRows(EVENT_ROW_CANDIDATES, EVENT_COUNT - 1, false)
    // エリート行は隣接させない (連続強制エリートの防止。2026-08-28 プレイテスト指摘)。
    // 非隣接プールが枯れて4行取れなかったら配置からやり直す (最終試行のみ隣接を許して数を保証)
    const eliteRows = pickRows(ELITE_ROW_CANDIDATES, ELITE_COUNT, !dropStandaloneEvent)
    if (eliteRows.length < ELITE_COUNT && !dropStandaloneEvent) continue
    if (eliteRows.length < ELITE_COUNT) {
      eliteRows.push(...pickRows(ELITE_ROW_CANDIDATES, ELITE_COUNT - eliteRows.length, false))
    }

    // 2. 行の幅: 同居行=3固定 / 行0=2 / 強制焚き火行・ボス行=1 / それ以外は2〜3
    const widths: number[] = []
    for (let r = 0; r < MAP_ROWS; r++) {
      if (r === BOSS_ROW || FORCED_CAMPFIRE_ROWS.has(r)) widths.push(1)
      else if (r === shopRow || r === eventWithWorkshopRow) widths.push(3)
      else if (r === 0) widths.push(2)
      else {
        const [w, next] = nextInt(rng, 2, 3)
        rng = next
        widths.push(w)
      }
    }

    // 3. 特別ノードの列を決める
    const specialAt = new Map<string, MapNodeType>() // "row:col" → type
    const placeTwo = (row: number, t1: MapNodeType, t2: MapNodeType): void => {
      // 幅3の行から異なる2列を選ぶ (残り1列は戦闘)
      const [c1, r1] = nextInt(rng, 0, 2)
      rng = r1
      const rest = [0, 1, 2].filter((c) => c !== c1)
      const [i2, r2] = nextInt(rng, 0, 1)
      rng = r2
      specialAt.set(`${row}:${c1}`, t1)
      specialAt.set(`${row}:${rest[i2]}`, t2)
    }
    placeTwo(shopRow, 'workshop', 'shop')
    placeTwo(eventWithWorkshopRow, 'workshop', 'event')
    for (const r of [...standaloneEventRows, ...eliteRows]) {
      const [c, next] = nextInt(rng, 0, widths[r] - 1)
      rng = next
      specialAt.set(`${r}:${c}`, standaloneEventRows.includes(r) ? 'event' : 'elite')
    }

    // 4. エッジ: 連続区間分割 (非交差・全ノード到達保証) + 確率で隣へ1本追加
    const edges: number[][][] = []
    for (let r = 0; r < MAP_ROWS - 1; r++) {
      const a = widths[r]
      const b = widths[r + 1]
      const rowEdges: number[][] = []
      for (let i = 0; i < a; i++) {
        const lo = Math.floor((i * b) / a)
        const hi = Math.floor(((i + 1) * b - 1) / a)
        const next: number[] = []
        for (let c = lo; c <= hi; c++) next.push(c)
        if (hi + 1 < b) {
          const [coin, nrng] = nextInt(rng, 0, 1)
          rng = nrng
          if (coin === 1) next.push(hi + 1)
        }
        rowEdges.push(next)
      }
      edges.push(rowEdges)
    }

    // 5. DP検証: どのパスも非戦闘の選択ノード (工房/ショップ/?) を最大2つしか踏めないか
    const nonCombatOf = (r: number, c: number): number => {
      const t = specialAt.get(`${r}:${c}`)
      return t === 'workshop' || t === 'shop' || t === 'event' ? 1 : 0
    }
    let maxNC = Array.from({ length: widths[0] }, (_, c) => nonCombatOf(0, c))
    for (let r = 0; r < MAP_ROWS - 1; r++) {
      const next = new Array(widths[r + 1]).fill(-Infinity)
      for (let c = 0; c < widths[r]; c++) {
        for (const to of edges[r][c]) {
          next[to] = Math.max(next[to], maxNC[c] + nonCombatOf(r + 1, to))
        }
      }
      maxNC = next
    }
    if (maxNC[0] > 2 && !dropStandaloneEvent) continue // 作り直し (rngは進んでいるので次は別の配置)

    // 6. ノードの実体化: タイプ・敵 (直前2行と同じ敵は避ける)・イベントID
    const eventIds: string[] = []
    {
      let pool = [...eventPool]
      const eventNodeCount = 1 + standaloneEventRows.length
      for (let i = 0; i < eventNodeCount && pool.length > 0; i++) {
        const [idx, next] = nextInt(rng, 0, pool.length - 1)
        rng = next
        const chosen = pool[idx]
        eventIds.push(chosen)
        pool = pool.filter((x) => x !== chosen)
      }
    }
    let eventCursor = 0
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
              : (specialAt.get(`${r}:${c}`) ?? 'battle')
        let encounterId: string | null = null
        let eventId: string | null = null
        if (type === 'battle' || type === 'elite' || type === 'boss') {
          const pool = tierForRow(r)
          const recent = [...recentEnemies.slice(-2).flat(), ...rowEnemies]
          const fresh = pool.filter((id) => !recent.includes(id))
          const candidates = fresh.length > 0 ? fresh : pool
          const [idx, next] = nextInt(rng, 0, candidates.length - 1)
          rng = next
          encounterId = candidates[idx]
          rowEnemies.push(encounterId)
        } else if (type === 'event') {
          eventId = eventIds[eventCursor % Math.max(1, eventIds.length)] ?? null
          eventCursor++
        }
        rowNodes.push({ type, encounterId, eventId, next: r < MAP_ROWS - 1 ? edges[r][c] : [] })
      }
      recentEnemies.push(rowEnemies)
      map.push(rowNodes)
    }
    return [map, rng]
  }
  throw new Error('マップ生成が収束しない') // 保険行で必ずreturnするため到達しない
}
