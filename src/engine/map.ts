// engine/map.ts — ランのマップ生成 (純ロジック。確定済みルール表「マップ」)
// StS式DAG・全体可視。18行 (行0〜17)・行17=ボス固定・強制焚き火行=5/11/16。
//
// 部屋タイプは本家の重みテーブルで員数を作り、ノード単位で配る (2026-08-29 全面置換):
//   ショップ5%・工房5%・?マス22% を「幕の全ノード数」に掛けて枚数化し、
//   行0 (本家 floor1 は必ず戦闘)・強制焚き火行・ボス行を除いた自由ノードにだけ配る。
//   エリートだけは員数固定4 (重み8%だとレリック供給が-28%になるため設計約束を優先)。
// 配置制約は本家の3つ: ①エリートは ELITE_MIN_ROW 以降 ②親と同タイプ禁止 (エリート/ショップ/工房のみ。
//   ?と戦闘は縦に続いてよい) ③兄弟 (同じ親を共有する同行ノード) と同タイプ禁止 (全種)。
// どのパスも戦闘数の下限8を保証する (上限は設けない = 本家に戦闘数の保証は無く「何回戦うか」を選べる)。
// エッジは非交差 (連続区間分割) で、全ノードが開始から到達可能かつボスへ到達可能。
// ?マスの中身はここでは決めない — 入室時に run.ts が本家式の累積確率で解決する。
import { nextInt } from './rng.ts'

/** エリート個体化を禁じる編成 (盗み逃走は素の数字でだけレースが成立する) */
/**
 * エリート専用プール (2026-08-31 ユーザー指示「エリートはエリート専用敵に」)。本家StS方式:
 * エリートは通常敵の強個体でなく、固有のギミックを持つ専用敵 (Nob/Lagavulin/刺突の書型)。
 * ステータスは素の値で完成しているので、マップのエリート補正 (HP×1.35+強化) は掛けない
 */
export const ELITE_POOLS: readonly (readonly string[])[] = [
  ['enemy_elite_sergeant', 'enc_elite_sentries'], // 1幕: 鬼軍曹 (ブロックで怒る) / 歩哨の双子 (がらくた)
  ['enemy_elite_iron_egg', 'enemy_elite_slaver'], // 2幕: 眠れる鉄卵 (起こすか削るか) / 奴隷商 (デバフ漬け)
  ['enemy_elite_stab_book', 'enemy_elite_giant_face'], // 3幕: 刺突の書 (増える多段) / 巨面 (二拍子の死)
]
import type { RngState } from './types.ts'

export type MapNodeType = 'battle' | 'elite' | 'campfire' | 'workshop' | 'shop' | 'event' | 'boss'

export interface MapNode {
  readonly type: MapNodeType
  /** battle / elite / boss のみ。それ以外は null */
  readonly encounterId: string | null
  /** 次の行のどの列へ進めるか */
  readonly next: readonly number[]
}

export type RunMap = readonly (readonly MapNode[])[]

export const MAP_ROWS = 18
export const BOSS_ROW = MAP_ROWS - 1
/** 全パスが通る強制焚き火行 (16=ボス前休憩は本家準拠。焚き火間の最大連戦 5/5/4) */
export const FORCED_CAMPFIRE_ROWS: ReadonlySet<number> = new Set([5, 11, 16])

/**
 * 本家の部屋タイプ重み。本家の癖ごと写す = 「幕の全ノード数」に掛けて枚数を作り、
 * 自由ノードにだけ配る → 自由ノード内の実効密度が名目より上がる (本家も同じ構造)
 */
const ROOM_WEIGHTS: Readonly<Record<'shop' | 'workshop' | 'event', number>> = {
  shop: 0.05,
  workshop: 0.05,
  event: 0.22,
}
/** エリートだけは員数固定。重み8%だと幕3個=1パス1.31体でレリック供給が-28%になるため */
const ELITE_COUNT = 4
/** エリートを置ける最小行。本家は floor1〜5 禁止だが、序盤のレリック供給を守るため行2から */
const ELITE_MIN_ROW = 2
/** どのパスも最低これだけは戦闘する。上限は設けない (本家に戦闘数の保証は無い) */
const MIN_COMBAT_PER_PATH = 8
/** 親と同タイプを禁止する部屋 (本家準拠。?と戦闘は対象外 = ?の縦連続は許可) */
const PARENT_EXCLUSIVE: ReadonlySet<MapNodeType> = new Set(['elite', 'shop', 'workshop'])
/** 生成リトライの上限 (幅とエッジを引き直す単一段階。実測は平均70回・最大325) */
const MAX_PLACEMENT_TRIES = 5000

/** 幕プール制 (確定済みルール表「ランの敵並び」2026-08-29): 幕 → 抽選プール (ソロ敵IDと編成IDの混合) */
const ACT_POOLS: readonly (readonly string[])[] = [
  // 敵拡充+6体 (2026-08-29): 幕1が実質2種で単調だった。苔の癒し手は編成専用 (ソロ自己回復のスタール防止)。
  // 基本2体化 (2026-08-30 ユーザー指示「敵の数は基本2がいい」): ソロ率60%→35%前後へ。
  // 複数体はHPを盛らずにワンショットを止める自然な構造 (3幕フルラン実測: T1キルを唯一免れたのは
  // 2体編成 = HPが分散していたから)。ソロで残すのは芸のある個体だけ —
  // うねる獣(読みなし休符)・探り屋(読みの教師)・栗鼠(とげ芸)・伏せ警戒/罠壊し/樽(固有芸)・
  // 苔の主(再生)・斧鬼(大技→隙)・石殻(甲殻)・オーガ(元ボスの再登場)
  ['enemy_probe', 'enemy_wide_power', 'enemy_thorn_squirrel', 'enemy_apprentice_colossus', 'enemy_mimic_imp', 'enc_probe_pair', 'enc_thief_pair', 'enc_squirrel_probe', 'enc_beast_pair', 'enc_thief_beast'], // 1幕 (ソロ5/10。2026-08-31 反復感への処方+2: 見習い巨像=タイマー予習・物真似の子鬼=手数の鏡予習)
  ['enemy_set_wary', 'enemy_set_breaker', 'enemy_bomber', 'enc_probe_trio', 'enc_joker_drummer', 'enc_bomber_healer', 'enc_hexer_shadow', 'enc_joker_hexer', 'enc_wary_bomber', 'enc_bomber_drummer', 'enc_squirrel_pair', 'enemy_whetstone_colossus', 'enemy_mimic_jester'], // 2幕 (2026-08-31 緊張不足への処方+2: 砥石の巨像=タイマー・物真似の道化=手数の鏡) (ソロ3/11。2026-08-31 非伏せ系+2=伏せ反応の密度を薄める〔伏せ無し赤で読み合いゼロ戦闘が過密だった実測〕)
  ['enemy_brute', 'enemy_moss', 'enemy_axe_ogre', 'enemy_shell_guard', 'enc_wolf_drummer', 'enc_hexer_shadow', 'enc_breaker_hexer', 'enc_axe_drummer', 'enc_shell_hexer', 'enc_wolf_pair', 'enc_moss_healer'], // 3幕 (ソロ4/11)
]
/** 幕ボス (難度順固定。確定済みルール表「マップ」) */
export const ACT_BOSSES: readonly string[] = ['enemy_brute', 'enemy_turtle', 'enemy_warden']
export const ACT_COUNT = 3

/** 幕とマップ行 → 敵抽選プール。ボス行は幕ボス1体 */
export function tierFor(act: number, row: number): readonly string[] {
  if (row >= BOSS_ROW) return [ACT_BOSSES[act - 1]]
  return ACT_POOLS[act - 1]
}

/**
 * シードからマップを決定的に生成する (同じシード = 同じマップ)。
 * 戦闘数の下限8はリトライの中で最後まで緩めない (緩和段階そのものを持たないので静かに破れる経路が無い)。
 */
export function generateMap(
  rng0: RngState,
  act = 1,
  allowWorkshop = true,
): readonly [RunMap, RngState] {
  let rng = rng0
  for (let attempt = 0; attempt <= MAX_PLACEMENT_TRIES; attempt++) {
    // 1. 行の幅 (行0=2 / 強制焚き火行・ボス行=1 / それ以外は2〜3)
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

    // 2. エッジ: 連続区間分割 (非交差・全ノード到達保証) + 確率で隣へ1本追加
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
    // 親テーブル (本家の親/兄弟制約の判定に使う)
    const parents: number[][][] = widths.map((w) => Array.from({ length: w }, (): number[] => []))
    for (let r = 0; r < MAP_ROWS - 1; r++) {
      for (let c = 0; c < widths[r]; c++) for (const to of edges[r][c]) parents[r + 1][to].push(c)
    }

    // 3. 部屋タイプの員数を作り、自由ノードへ配る (本家式)
    const total = widths.reduce((a, b) => a + b, 0)
    const quota: readonly (readonly [MapNodeType, number])[] = [
      ['elite', ELITE_COUNT],
      // 工房 (合成v1は緑同士のみ) は緑を含まないランでは配置しない。枠は戦闘に置き換わる
      ['workshop', allowWorkshop ? Math.round(total * ROOM_WEIGHTS.workshop) : 0],
      ['shop', Math.round(total * ROOM_WEIGHTS.shop)],
      ['event', Math.round(total * ROOM_WEIGHTS.event)],
    ]
    const specialAt = new Map<string, MapNodeType>()
    const typeAt = (r: number, c: number): MapNodeType => specialAt.get(`${r}:${c}`) ?? 'battle'
    // 自由ノード = 行0 (本家 floor1 は全て通常戦闘)・強制焚き火行・ボス行 を除く全ノード
    const freeNodes: (readonly [number, number])[] = []
    for (let r = 1; r < BOSS_ROW; r++) {
      if (FORCED_CAMPFIRE_ROWS.has(r)) continue
      for (let c = 0; c < widths[r]; c++) freeNodes.push([r, c])
    }
    const assignable = (r: number, c: number, t: MapNodeType): boolean => {
      if (t === 'elite' && r < ELITE_MIN_ROW) return false
      // 兄弟同種禁止: 同じ親を共有する同行ノードと同タイプにしない (本家は全種が対象)
      for (let c2 = 0; c2 < widths[r]; c2++) {
        if (c2 === c || typeAt(r, c2) !== t) continue
        if (parents[r][c2].some((p) => parents[r][c].includes(p))) return false
      }
      // 親同種禁止: エリート・ショップ・工房のみ (?と戦闘は縦に続いてよい = 本家)。
      // 配置は員数を順に置いていくので、親側・子側の両方を見ないと
      // 「後から親に同タイプが置かれる」経路ですり抜ける (片方向チェックのバグ)
      if (PARENT_EXCLUSIVE.has(t)) {
        if (parents[r][c].some((p) => typeAt(r - 1, p) === t)) return false
        if (r + 1 < MAP_ROWS && edges[r][c].some((to) => typeAt(r + 1, to) === t)) return false
      }
      return true
    }
    let placementFailed = false
    for (const [t, n] of quota) {
      for (let k = 0; k < n; k++) {
        const cand = freeNodes.filter(([r, c]) => !specialAt.has(`${r}:${c}`) && assignable(r, c, t))
        if (cand.length === 0) {
          placementFailed = true
          break
        }
        const [i, next] = nextInt(rng, 0, cand.length - 1)
        rng = next
        specialAt.set(`${cand[i][0]}:${cand[i][1]}`, t)
      }
      if (placementFailed) break
    }
    if (placementFailed) continue

    // 4. DP検証: どのパスも戦闘数が MIN_COMBAT_PER_PATH 以上か (上限は設けない)
    const combatOf = (r: number, c: number): number => {
      if (r === BOSS_ROW || FORCED_CAMPFIRE_ROWS.has(r)) return 0
      const t = typeAt(r, c)
      return t === 'workshop' || t === 'shop' || t === 'event' ? 0 : 1
    }
    let minC = Array.from({ length: widths[0] }, (_, c) => combatOf(0, c))
    for (let r = 0; r < MAP_ROWS - 1; r++) {
      const next = new Array<number>(widths[r + 1]).fill(Infinity)
      for (let c = 0; c < widths[r]; c++) {
        for (const to of edges[r][c]) {
          next[to] = Math.min(next[to], minC[c] + combatOf(r + 1, to))
        }
      }
      minC = next
    }
    if (minC[0] < MIN_COMBAT_PER_PATH) continue // 戦闘数の下限は最後まで緩めない。作り直す

    // 5. ノードの実体化 (直前2行と同じ敵は避ける)。?の中身は持たせない (入室時に決まる)
    const recentEnemies: string[][] = []
    const map: MapNode[][] = []
    const usedElites = new Set<string>()
    for (let r = 0; r < MAP_ROWS; r++) {
      const rowNodes: MapNode[] = []
      const rowEnemies: string[] = []
      for (let c = 0; c < widths[r]; c++) {
        const type: MapNodeType =
          r === BOSS_ROW ? 'boss' : FORCED_CAMPFIRE_ROWS.has(r) ? 'campfire' : typeAt(r, c)
        let encounterId: string | null = null
        if (type === 'battle' || type === 'elite' || type === 'boss') {
          const basePool = tierFor(act, r)
          // こそ泥はエリートにしない (2026-08-31 ユーザー裁定)。エリート補正 (HP×1.35+強化) が乗ると
          // 「満タン34HP+ブロックを1ターンで抜け」が構造的に不可能 = 盗みが税に化ける実測への処方
          // エリートは幕内で未使用の個体を優先する (2026-08-31 緑ランで歩哨の双子が4枠中3回)
          const pool =
            type === 'elite'
              ? (() => {
                  const all = ELITE_POOLS[act - 1]
                  const fresh = all.filter((id) => !usedElites.has(id))
                  return fresh.length > 0 ? fresh : all
                })()
              : basePool
          const recent = [...recentEnemies.slice(-2).flat(), ...rowEnemies]
          const fresh = pool.filter((id) => !recent.includes(id))
          const candidates = fresh.length > 0 ? fresh : pool
          const [idx, next] = nextInt(rng, 0, candidates.length - 1)
          rng = next
          encounterId = candidates[idx]
          if (type === 'elite') usedElites.add(encounterId)
          rowEnemies.push(encounterId)
        }
        rowNodes.push({ type, encounterId, next: r < MAP_ROWS - 1 ? edges[r][c] : [] })
      }
      recentEnemies.push(rowEnemies)
      map.push(rowNodes)
    }
    return [map, rng]
  }
  throw new Error('マップ生成が収束しない')
}
