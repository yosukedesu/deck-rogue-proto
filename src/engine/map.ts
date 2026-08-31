// engine/map.ts — ランのマップ生成 (純ロジック。確定済みルール表「マップ」)
// StS式DAG・全体可視。18行 (行0〜17)・行17=ボス固定・強制焚き火行=5/11/16。
//
// 生成は本家式パスウォーク (2026-08-31 ユーザー指示「本家並みのマップ生成分岐」で全面置換):
//   7列格子×6本のパスウォーク。開始列はランダム (最初の2本は別列 = 行0は必ず2ノード以上)、
//   各行で列±1移動。交差防止 = 左隣ノードの最大接続先と右隣ノードの最小接続先で挟むクランプ。
//   共通祖先が5行以内の合流は1回引き直す (小ひし形の抑制 = 本家 getCommonAncestor 準拠)。
//   行の幅は訪問列数で可変 (実測2〜5)。強制焚き火行・行0も複数ノード化 (全ノードが
//   焚き火/戦闘なので「全パスが通る」保証は不変)。ボスは行16の全ノードから接続。
//   旧「行幅2〜3の連続区間分割」は分岐がほぼ梯子で、本家の蛇行・合流・複数スタートが無かった。
//
// 部屋タイプは本家の重みテーブルで員数を作り、ノード単位で配る (2026-08-29 全面置換):
//   ショップ5%・工房5%・?マス22% を「幕の全ノード数」に掛けて枚数化し、
//   行0 (本家 floor1 は必ず戦闘)・強制焚き火行・ボス行を除いた自由ノードにだけ配る。
//   エリートだけは員数固定4 (重み8%だとレリック供給が-28%になるため設計約束を優先)。
// 配置制約は本家の3つ: ①エリートは ELITE_MIN_ROW 以降 ②親と同タイプ禁止 (エリート/ショップ/工房のみ。
//   ?と戦闘は縦に続いてよい) ③兄弟 (同じ親を共有する同行ノード) と同タイプ禁止 (全種)。
// どのパスも戦闘数の下限8を保証する (上限は設けない = 本家に戦闘数の保証は無く「何回戦うか」を選べる)。
// エッジは非交差 (格子空間のクランプで保証)、全ノードが開始から到達可能かつボスへ到達可能。
// ?マスの中身はここでは決めない — 入室時に run.ts が本家式の累積確率で解決する。
import { nextInt } from './rng.ts'

/** エリート個体化を禁じる編成 (盗み逃走は素の数字でだけレースが成立する) */
/**
 * エリート専用プール (2026-08-31 ユーザー指示「エリートはエリート専用敵に」)。本家StS方式:
 * エリートは通常敵の強個体でなく、固有のギミックを持つ専用敵 (Nob/Lagavulin/刺突の書型)。
 * ステータスは素の値で完成しているので、マップのエリート補正 (HP×1.35+強化) は掛けない
 */
export const ELITE_POOLS: readonly (readonly string[])[] = [
  ['enemy_elite_sergeant', 'enc_elite_sentries', 'enemy_elite_gold_raven'], // 1幕: 鬼軍曹 (ブロックで怒る) / 歩哨の双子 (がらくた) / 金羽の大鴉 (大金を盗んで逃げるレース)
  ['enemy_elite_iron_egg', 'enemy_elite_slaver', 'enemy_elite_mirror_djinn'], // 2幕: 眠れる鉄卵 (起こすか削るか) / 奴隷商 (デバフ漬け) / 写し身の魔人 (エリート級の手数の鏡)
  ['enemy_elite_stab_book', 'enemy_elite_giant_face', 'enemy_elite_doom_chanter'], // 3幕: 刺突の書 (増える多段) / 巨面 (二拍子の死) / 終焉の唱い手 (プレイ6枚ごと強化+2の時限歌)
]
import type { RngState } from './types.ts'

export type MapNodeType = 'battle' | 'elite' | 'campfire' | 'workshop' | 'shop' | 'event' | 'boss'

export interface MapNode {
  readonly type: MapNodeType
  /** battle / elite / boss のみ。それ以外は null */
  readonly encounterId: string | null
  /** 次の行のどの列 (行内の詰めた添字) へ進めるか */
  readonly next: readonly number[]
  /**
   * 格子列 (0〜GRID_COLS-1)。表示専用 — UIが本家の蛇行を再現するための座標で、
   * コマンド (ChooseNode.col) は従来どおり行内の詰めた添字を使う
   */
  readonly col?: number
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
/**
 * 「エリートを狙うパス」で踏める最低数 (2026-08-31 パスウォーク化に伴う保証)。
 * 広い盤面ではエリート4個が散り、実測で中央値2 (最低1) しか1本のパスで拾えなくなった
 * = 員数固定4の目的だったレリック供給が配置の運で崩れる。3個踏める経路の存在を保証する
 * (完全回避可能なルートの成立は不変 — これは「狙えば拾える」側の保証)
 */
const ELITE_PATH_MIN = 3
/** 親と同タイプを禁止する部屋 (本家準拠。?と戦闘は対象外 = ?の縦連続は許可) */
const PARENT_EXCLUSIVE: ReadonlySet<MapNodeType> = new Set(['elite', 'shop', 'workshop'])
/** 生成リトライの上限 (パスとエッジを引き直す単一段階) */
const MAX_PLACEMENT_TRIES = 5000
/** 格子の列数と歩かせるパスの本数 (本家: 7列×6本) */
export const GRID_COLS = 7
const PATH_WALKS = 6
/** 共通祖先をこの行数まで遡って探す (本家 getCommonAncestor の maxDepth=5) */
const ANCESTOR_DEPTH = 5

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
    // 1. 本家式パスウォーク: 7列格子を6本のパスが行0→行16へ歩く。
    //    訪問したマスがノードになり、歩いた区間がエッジになる (合流 = 自然なマージ)
    const walkRows = BOSS_ROW // 行0〜16 (ボス行はウォーク対象外)
    const visited: Set<number>[] = Array.from({ length: walkRows }, () => new Set<number>())
    // latEdges[y] = 格子列 → 次の行の格子列の集合 (y: 0〜15)
    const latEdges: Map<number, Set<number>>[] = Array.from(
      { length: walkRows - 1 },
      () => new Map<number, Set<number>>(),
    )
    // latParents[y] = 格子列 → 前の行の格子列の集合 (共通祖先の探索用)
    const latParents: Map<number, Set<number>>[] = Array.from(
      { length: walkRows },
      () => new Map<number, Set<number>>(),
    )
    const addLatEdge = (y: number, from: number, to: number): void => {
      let s = latEdges[y].get(from)
      if (s === undefined) latEdges[y].set(from, (s = new Set()))
      s.add(to)
      let p = latParents[y + 1].get(to)
      if (p === undefined) latParents[y + 1].set(to, (p = new Set()))
      p.add(from)
    }
    /**
     * (y,a) と (y,b) が ANCESTOR_DEPTH 行以内に共通祖先を持つか (本家 getCommonAncestor 準拠)。
     * 左ノードは「右端の親」、右ノードは「左端の親」だけを辿る = 内側のチェーンが閉じる
     * 実際のひし形だけを検出する。集合ベースの全祖先比較にすると、6本のパスは数行で
     * ほぼ全員が祖先を共有するため合流がほぼ全て弾かれ、行が痩せず総ノードが膨張する (実測69)
     */
    const nearCommonAncestor = (y: number, a: number, b: number): boolean => {
      let l = Math.min(a, b)
      let r = Math.max(a, b)
      for (let row = y; row > 0 && row > y - ANCESTOR_DEPTH; row--) {
        const lp = latParents[row].get(l)
        const rp = latParents[row].get(r)
        if (lp === undefined || lp.size === 0 || rp === undefined || rp.size === 0) return false
        l = Math.max(...lp)
        r = Math.min(...rp)
        if (l === r) return true
      }
      return false
    }
    const clampCol = (x: number): number => Math.max(0, Math.min(GRID_COLS - 1, x))
    const startCols: number[] = []
    for (let p = 0; p < PATH_WALKS; p++) {
      let x = 0
      // 最初の2本は別の列から (本家準拠 = 行0が必ず2ノード以上になり、開始の選択が生まれる)
      for (;;) {
        const [v, next] = nextInt(rng, 0, GRID_COLS - 1)
        rng = next
        x = v
        if (p !== 1 || x !== startCols[0]) break
      }
      startCols.push(x)
      visited[0].add(x)
      for (let y = 0; y < walkRows - 1; y++) {
        const [d, next] = nextInt(rng, -1, 1)
        rng = next
        let nx = clampCol(x + d)
        // 小ひし形の抑制: 合流先の別の親と共通祖先が近い (=直前に分かれた道と即再合流) なら1回引き直す
        const otherParents = [...(latParents[y + 1].get(nx) ?? [])].filter((c) => c !== x)
        if (otherParents.some((c) => nearCommonAncestor(y, x, c))) {
          const [d2, next2] = nextInt(rng, -1, 1)
          rng = next2
          nx = clampCol(x + d2)
        }
        // 交差防止 (本家準拠): 左隣ノードの最大接続先より左へは行けない /
        // 右隣ノードの最小接続先より右へは行けない (合流=同一点は許す)
        const left = x > 0 ? latEdges[y].get(x - 1) : undefined
        if (left !== undefined && left.size > 0) nx = Math.max(nx, Math.max(...left))
        const right = x < GRID_COLS - 1 ? latEdges[y].get(x + 1) : undefined
        if (right !== undefined && right.size > 0) nx = Math.min(nx, Math.min(...right))
        addLatEdge(y, x, nx)
        visited[y + 1].add(nx)
        x = nx
      }
    }

    // 2. 格子 → 行内の詰めた添字へ変換 (ChooseNode.col の互換維持。col に格子列を残す)
    const colsOf: number[][] = visited.map((s) => [...s].sort((a, b) => a - b))
    const widths: number[] = [...colsOf.map((c) => c.length), 1] // +ボス行
    const edges: number[][][] = []
    for (let r = 0; r < walkRows - 1; r++) {
      edges.push(
        colsOf[r].map((col) =>
          [...(latEdges[r].get(col) ?? [])].sort((a, b) => a - b).map((to) => colsOf[r + 1].indexOf(to)),
        ),
      )
    }
    // 行16 (ボス前休憩) の全ノード → ボス (本家: 最上段は全てボスへ)
    edges.push(colsOf[walkRows - 1].map(() => [0]))
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
    // 戦闘数DP: 全パスの最小戦闘数 (エリートは戦闘に数える。焚き火・ボスは0)
    const combatOf = (r: number, c: number): number => {
      if (r === BOSS_ROW || FORCED_CAMPFIRE_ROWS.has(r)) return 0
      const t = typeAt(r, c)
      return t === 'workshop' || t === 'shop' || t === 'event' ? 0 : 1
    }
    const minCombats = (): number => {
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
      return minC[0]
    }
    let placementFailed = false
    for (const [t, n] of quota) {
      // 非戦闘ノードは「置くと戦闘数の床を割る位置」を候補から外す (2026-08-31 パスウォーク化に伴う)。
      // 旧・棄却サンプリングは分岐の濃いDAGで「特別ノードを7個以上通せるパス」がほぼ必ず存在し
      // 生成が数百回リトライしていた。床はここで構成的に守り、最後のDP検証は保険として残す
      const guardsFloor = t === 'workshop' || t === 'shop' || t === 'event'
      for (let k = 0; k < n; k++) {
        const cand = freeNodes.filter(([r, c]) => {
          const key = `${r}:${c}`
          if (specialAt.has(key) || !assignable(r, c, t)) return false
          if (!guardsFloor) return true
          specialAt.set(key, t)
          const ok = minCombats() >= MIN_COMBAT_PER_PATH
          specialAt.delete(key)
          return ok
        })
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

    // 4. DP検証 (保険): どのパスも戦闘数が MIN_COMBAT_PER_PATH 以上か (上限は設けない)
    if (minCombats() < MIN_COMBAT_PER_PATH) continue // 戦闘数の下限は最後まで緩めない。作り直す
    // エリート供給の保証: 3個以上踏める経路が存在するか (DP最大値)
    {
      let maxE = Array.from({ length: widths[0] }, () => 0)
      for (let r = 0; r < MAP_ROWS - 1; r++) {
        const next = new Array<number>(widths[r + 1]).fill(-Infinity)
        for (let c = 0; c < widths[r]; c++) {
          for (const to of edges[r][c]) {
            const gain = typeAt(r + 1, to) === 'elite' ? 1 : 0
            next[to] = Math.max(next[to], maxE[c] + gain)
          }
        }
        maxE = next
      }
      if (maxE[0] < ELITE_PATH_MIN) continue
    }

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
        rowNodes.push({
          type,
          encounterId,
          next: r < MAP_ROWS - 1 ? edges[r][c] : [],
          col: r === BOSS_ROW ? Math.floor(GRID_COLS / 2) : colsOf[r][c],
        })
      }
      recentEnemies.push(rowEnemies)
      map.push(rowNodes)
    }
    return [map, rng]
  }
  throw new Error('マップ生成が収束しない')
}
