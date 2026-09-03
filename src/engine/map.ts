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
// 戦闘数の保証は無い (2026-08-31 床8を撤廃・本家完全準拠 —「何回戦うか」を選べるのがルート選択)。
// エッジは非交差 (格子空間のクランプで保証)、全ノードが開始から到達可能かつボスへ到達可能。
// ?マスの中身はここでは決めない — 入室時に run.ts が本家式の累積確率で解決する。
import { nextInt } from './rng.ts'
import { resolveEncounter } from './content.ts'

/** エリート個体化を禁じる編成 (盗み逃走は素の数字でだけレースが成立する) */
/**
 * エリート専用プール (2026-08-31 ユーザー指示「エリートはエリート専用敵に」)。本家StS方式:
 * エリートは通常敵の強個体でなく、固有のギミックを持つ専用敵 (Nob/Lagavulin/刺突の書型)。
 * ステータスは素の値で完成しているので、マップのエリート補正 (HP×1.35+強化) は掛けない
 */
/**
 * 幕内に必ず1回は現れる編成 (2026-09-02)。幕3=汚泥の大暴れ (ターン装甲45=多段バーストへの唯一の構造的回答)。
 * 門番のターン装甲75と対: 「量の器」が幕3で必ず1回は問われる
 */
export const ACT_MUST_APPEAR: readonly (readonly string[])[] = [[], [], ['enemy_sludge_berserker']]

export const ELITE_POOLS: readonly (readonly string[])[] = [
  // 各幕4種 (2026-08-31 再検証ラン「プール3種×4枠で同一個体が同一パスに2回=消化試合」への処方)
  ['enemy_elite_sergeant', 'enc_elite_sentries', 'enemy_elite_gold_raven', 'enemy_elite_devourer'], // 1幕: 鬼軍曹 (ブロックで怒る) / 歩哨の双子 (がらくた) / 金羽の大鴉 (金レース) / 大喰らいの蟲 (山札喰い=デッキが第二のHP)
  ['enemy_elite_iron_egg', 'enemy_elite_slaver', 'enemy_elite_mirror_djinn', 'enemy_elite_owl'], // 2幕: 眠れる鉄卵 (起こすか削るか) / 奴隷商 (デバフ漬け) / 写し身の魔人 (手数の鏡) / 読み手の梟 (伏せ読み=set-confirm検定)
  ['enemy_elite_stab_book', 'enemy_elite_giant_face', 'enemy_elite_doom_chanter', 'enemy_elite_deathless', 'enemy_elite_husk_3'], // 3幕 (2026-09-02 +骸兵=残機チェーン: オーバーキルが無駄になる=大技一撃デッキへの問い): 刺突の書 (増える多段) / 巨面 (二拍子) / 終焉の唱い手 (枚数タイマー) / 不滅の騎士 (再生バースト検定)
]
import type { RngState } from './types.ts'

export type MapNodeType =
  | 'battle'
  | 'elite'
  | 'campfire'
  | 'workshop'
  | 'shop'
  | 'event'
  | 'treasure'
  | 'boss'

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

/**
 * 15行化 (2026-09-01 ユーザー方針「はかりやすいように本家と同じ15行設計」):
 * 本家StS = マップ15階+ボス。行0〜14がマップ・行15=ボス。旧18行 (16→18の?席拡張) は
 * 1幕の経済量 (戦闘・ピック・焚き火・金) を本家より+2行ぶん膨らませ、難易度検証3本で
 * 「線形倍率×超線形デッキ成長」の谷を作る一因だった。本家値との1:1比較を測定の物差しにする
 */
export const MAP_ROWS = 16
export const BOSS_ROW = MAP_ROWS - 1
/**
 * 幕別の行数 (2026-09-02 ユーザー裁定「StS2式 15/14/13」): 本家StS2は部屋数15/14/13と
 * 幕が進むほど短くし「終盤は1戦を重く」を構造で作る (docs/sts2-reference.md §1)。
 * 行数 = 部屋数+ボス行。MAP_ROWS/BOSS_ROW/TREASURE_ROW は幕1 (最大) の値として残置
 * (sim の配列サイズ・旧セーブ互換用)。幕を知る場所では必ず *For(act) を使うこと
 */
export const ACT_MAP_ROWS: readonly number[] = [16, 15, 14]
export function mapRowsFor(act: number): number {
  return ACT_MAP_ROWS[act - 1] ?? MAP_ROWS
}
export function bossRowFor(act: number): number {
  return mapRowsFor(act) - 1
}
/** 宝箱行 = ボスの7行手前 (本家関係の維持: 幕1=8・幕2=7・幕3=6) */
export function treasureRowFor(act: number): number {
  // 幕1に宝箱行は無い (2026-09-03 曲線パッケージ: レリック供給源の削減。人間#3は幕1で5個)。幕2/3はボスの7行手前
  if (act <= 1) return -1
  return bossRowFor(act) - 7
}
/**
 * 全パスが通る強制焚き火行 (2026-08-31 本家配置化: ボス前休憩のみ = 本家15階の「最上段は全て
 * 休憩」準拠)。残りの焚き火は部屋タイプとして散布する — 旧5/11の強制行は廃止し、
 * 「どこで休むか」をルート選択に入れる (焚き火間の最大連戦保証も本家に無いので廃止)
 */
export const FORCED_CAMPFIRE_ROWS: ReadonlySet<number> = new Set([BOSS_ROW - 1])
/**
 * 宝箱行 (2026-08-31 ユーザー選択)。本家の「9階は全ノード宝箱」= 行8 (表示行9)。
 * 全パスが必ず1回通る = レリック供給+1/幕。進入するとレリック3択 (?→宝箱と同じ配管)
 */
export const TREASURE_ROW = 8

/**
 * 本家の部屋タイプ重み。本家の癖ごと写す = 「幕の全ノード数」に掛けて枚数を作り、
 * 自由ノードにだけ配る → 自由ノード内の実効密度が名目より上がる (本家も同じ構造)
 */
const ROOM_WEIGHTS: Readonly<Record<'shop' | 'workshop' | 'event' | 'campfire', number>> = {
  shop: 0.05,
  workshop: 0.05,
  event: 0.22,
  campfire: 0.08, // 本家Rest=0.12→0.08 (2026-08-31 ユーザー指示「焚き火減らして」。回復25%と合わせHP経済を絞る。散布5〜6個+ボス前全焚き火行)
}
/** 焚き火を置ける最小行 (本家「6階より下に休憩なし」= index 5 以降) */
const CAMPFIRE_MIN_ROW = 5
/** 幕1の工房は行5以降 (2026-09-03 ユーザー裁定。Opus J/K: 行3の工房は所持金79G/73Gで合成100Gが払えない) */
const WORKSHOP_MIN_ROW_ACT1 = 5
/**
 * 1本のパスで踏める焚き火の上限 (ボス前の全焚き火行の1回を含む。2026-08-31 ユーザー裁定
 * 「最良ルートで3〜4個に制限できていればok」)。ガード無しだと21.5%のマップで5個以上の
 * 焚き火ハシゴルートが成立していた (200シード実測: 3=17/4=140/5=42/6=1)
 */
const CAMPFIRE_PATH_MAX = 4
/** どのパスも工房は1幕に最大1回 (2026-09-03 ユーザー裁定「工房は全ルート1幕1回のみ」) */
const WORKSHOP_PATH_MAX = 1
/** エリートだけは員数固定。重み8%だと幕3個=1パス1.31体でレリック供給が-28%になるため */
const ELITE_COUNT = 4
/** ショップは固定3/幕 (2026-09-02 StS2 NumOfShops=3 準拠。ユーザー裁定) */
const SHOP_COUNT = 3
/** エリートを置ける最小行。本家は floor1〜5 禁止だが、序盤のレリック供給を守るため行2から */
const ELITE_MIN_ROW = 2
/** 幕別のエリート下限行 (2026-09-02 ユーザー裁定): 幕1は行4 = スターターデッキが立つ前の事故待ち配置を避ける。幕2/3はWeak帯2行の直後=行2 */
const ELITE_MIN_ROW_BY_ACT: readonly number[] = [4, 2, 2]
/**
 * 「エリートを狙うパス」で踏める最低数 (2026-08-31 パスウォーク化に伴う保証)。
 * 広い盤面ではエリート4個が散り、実測で中央値2 (最低1) しか1本のパスで拾えなくなった
 * = 員数固定4の目的だったレリック供給が配置の運で崩れる。3個踏める経路の存在を保証する
 * (完全回避可能なルートの成立は不変 — これは「狙えば拾える」側の保証)
 */
const ELITE_PATH_MIN = 3
/**
 * 親と同タイプを禁止する部屋 (本家準拠 = elite/shop/rest。?と戦闘は対象外 = ?の縦連続は許可)。
 * campfire を含めることで、行16 (全ノード焚き火) の親 = 行15 に焚き火が置かれない
 * = 本家の「13階に休憩なし」と同じ効果が自動で出る
 */
const PARENT_EXCLUSIVE: ReadonlySet<MapNodeType> = new Set(['elite', 'shop', 'workshop', 'campfire'])
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
  ['enemy_wide_power', 'enemy_thorn_squirrel', 'enemy_apprentice_colossus', 'enemy_cultist', 'enemy_slug', 'enemy_mud_lump', 'enc_probe_pair', 'enc_probe_trio', 'enc_squirrel_trio', 'enc_mud_mudlings', 'enc_mudling_swarm', 'enc_cultist_imp', 'enc_thief_pair', 'enc_squirrel_probe', 'enc_beast_pair', 'enc_thief_beast', 'enemy_gaping_maw', 'enemy_cog_construct', 'enemy_vine_walker', 'enemy_strangler_serpent', 'enc_serpent_fruit', 'enc_sporecap_fruit', 'enc_sporecap_mudlings', 'enc_serpent_mudlings', 'enc_snapfruit_trio'], // 1幕 (ソロ7/12。2026-09-01 敵圧監査+2: 狂信者=カルト型タイマー・蛞蝓=状態異常の教師〔幕1のデバフゼロを解消〕。2026-08-31 反復感への処方+2: 見習い巨像=タイマー予習・物真似の子鬼=手数の鏡予習)
  ['enemy_set_wary', 'enemy_set_breaker', 'enemy_bomber', 'enc_probe_trio', 'enc_joker_drummer', 'enc_bomber_healer', 'enc_hexer_shadow', 'enc_joker_hexer', 'enc_joker_hexer_drummer', 'enc_wary_bomber', 'enc_bomber_drummer', 'enemy_whetstone_colossus', 'enemy_mimic_jester', 'enemy_cinder_imp', 'enemy_rock_beetle', 'enemy_big_slime', 'enc_squire_archer', 'enc_raptor_nest', 'enemy_maw_hunter', 'enc_imp_jester', 'enc_chomper_pair', 'enc_scald_gnat_pair'], // 2幕 (2026-09-02 本家形: 重量級ソロ=HunterKiller枠) (2026-08-31 緊張不足への処方+2: 砥石の巨像=タイマー・物真似の道化=手数の鏡) (ソロ3/11。2026-08-31 非伏せ系+2=伏せ反応の密度を薄める〔伏せ無し赤で読み合いゼロ戦闘が過密だった実測〕)
  ['enemy_brute', 'enemy_moss', 'enemy_axe_ogre', 'enemy_shell_guard', 'enc_wolf_drummer', 'enc_hexer_shadow', 'enc_breaker_hexer', 'enc_axe_drummer', 'enc_shell_hexer', 'enc_moss_healer', 'enc_fang_twins', 'enemy_brood_toad', 'enc_mourn_beasts', 'enemy_sludge_berserker', 'enc_wolf_hexer_drummer', 'enc_mourn_healer', 'enc_axe_shadow', 'enc_axe_automatons', 'enemy_thunder_globe', 'enemy_frog_knight', 'enc_biting_scrolls_quad', 'enc_lost_forgotten'], // 3幕 (2026-09-02 本家形: 札汚染の大物=SlimedBerserker枠) (ソロ4/11)
]
/**
 * Weak帯 (2026-09-02 StS2式の構造保証。docs/sts2-reference.md §1「序盤に強敵が事故で出ない」):
 * 幕頭のN行 (本家のWeak戦数と同値: 幕1=3・幕2/3=2) は教師枠の弱プールからだけ抽選する。
 * ?→戦闘も tierFor 経由なので自動で追随。エリートは別プール (ELITE_POOLS) なので対象外
 */
const WEAK_ROWS: readonly number[] = [3, 2, 2]
const WEAK_POOLS: readonly (readonly string[])[] = [
  // 幕1: 読みの教師・手数の鏡の予習・状態異常の教師・タイマーの予習
  ['enemy_probe', 'enemy_slug', 'enc_probe_pair', 'enc_mudling_trio', 'enemy_sludge_spider', 'enemy_iron_clam', 'enc_spider_fruit'], // 2026-09-02 本家形: 弱枠にも群れ (本家Weakは小スライム×3を含む=平均1.5〜2.0体)。見習い巨像(70HP+殻8)はWeakでなく本帯へ
  // 幕2: 伏せ検定・固い小物の教師・手数の鏡
  ['enemy_set_wary', 'enemy_rock_beetle', 'enemy_mimic_jester', 'enc_imp_jester', 'enc_beetle_wary'], // 2026-09-02 本家形: 弱枠にも群れ
  // 幕3: 貫通の的・大技→隙の窓・伏せ罰の教師
  ['enemy_shell_guard', 'enemy_axe_ogre', 'enemy_set_breaker', 'enc_axe_shadow', 'enc_wolf_hexer_drummer', 'enemy_devoted_sculptor', 'enc_biting_scrolls_trio'], // 2026-09-02 本家形: 弱枠にも群れ
]

/**
 * 幕ボスのプール (2026-09-02 本家形: 各幕に複数のボスを持ち、ランごとにシードで1体を抽選。
 * 本家StS2は各幕3体+未撃破優先ローテ。うちはラン間の撃破記録を engine が持たないので純抽選)。
 * ACT_BOSSES は各幕の代表 (先頭) = 旧テスト・CLI表示の互換用
 */
export const ACT_BOSS_POOLS: readonly (readonly string[])[] = [
  ['enemy_brute', 'enc_kin_ritual'], // 幕1: 脳筋オーガ (装甲25・激昂) / 血族の儀式 (司祭+踊り手×2=キル順・従者全滅で司祭が本気) 2026-09-02 本家TheKin型
  ['enemy_turtle', 'enc_kaiser_crab'], // 幕2: 眠たがりの大亀 (チャージ・装甲30) / 双腕の巨蟹 (2腕・片腕を倒すと弔い+3=同時に削るか) 本家KaiserCrab型
  ['enemy_warden', 'enemy_chimera_1'], // 幕3: 門番 (激昂2タイマー) / 蘇る合成獣 (膨らむ残機3段=オーバーキル無効・爪が育つ・火傷) 本家TestSubject型
]
export const ACT_BOSSES: readonly string[] = ACT_BOSS_POOLS.map((p) => p[0])
export const ACT_COUNT = 3

/** 幕とマップ行 → 敵抽選プール。ボス行は幕ボス1体 */
export function tierFor(act: number, row: number): readonly string[] {
  if (row >= bossRowFor(act)) return ACT_BOSS_POOLS[act - 1]
  if (row < (WEAK_ROWS[act - 1] ?? 0)) return WEAK_POOLS[act - 1]
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
    // 幕別の行数 (2026-09-02 StS2式 15/14/13)
    const mapRows = mapRowsFor(act)
    const bossRow = mapRows - 1
    const treasureRow = treasureRowFor(act)
    const forcedCampfireRow = bossRow - 1 // ボス前休憩 (本家: 最上段は全て休憩)
    const walkRows = bossRow // ボス行はウォーク対象外
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

    // 2. 分岐の補強 (2026-08-31 Opusマップ検証「単一出口79%＝体験は一本道。行0の1手で
    //    8ノード先まで確定していた」への処方): 出次数1のノードに、隣列 (±1) の既存ノードへの
    //    非交差エッジを1本足す。本家アルゴリズムのウォークだけでは7列格子でレーンが分かれ、
    //    合流由来の分岐がほぼ出ない (実測: 本数を12まで増やしても出次数1.48で総ノード93に膨張。
    //    補強なら総ノード不変で出次数1.22→1.47・単一出口79%→54% = 2行に1回は選べる)
    for (let y = 0; y < walkRows - 1; y++) {
      for (const col of [...visited[y]].sort((a, b) => a - b)) {
        const cur = latEdges[y].get(col)
        if (cur === undefined || cur.size !== 1) continue
        const [coin, nextRng] = nextInt(rng, 0, 1)
        rng = nextRng
        const base = [...cur][0]
        const order = coin === 1 ? [base + 1, base - 1] : [base - 1, base + 1]
        for (const cand of order) {
          if (Math.abs(cand - col) > 1 || cand < 0 || cand >= GRID_COLS) continue
          if (!visited[y + 1].has(cand)) continue
          // 交差チェック: 同行の他ノードの既存エッジと交差しない場合だけ足す
          let ok = true
          for (const [c2, s2] of latEdges[y]) {
            if (c2 === col) continue
            for (const t2 of s2) {
              if ((c2 - col) * (t2 - cand) < 0) {
                ok = false
                break
              }
            }
            if (!ok) break
          }
          if (!ok) continue
          addLatEdge(y, col, cand)
          break
        }
      }
    }

    // 3. 格子 → 行内の詰めた添字へ変換 (ChooseNode.col の互換維持。col に格子列を残す)
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
    for (let r = 0; r < mapRows - 1; r++) {
      for (let c = 0; c < widths[r]; c++) for (const to of edges[r][c]) parents[r + 1][to].push(c)
    }

    // 3. 部屋タイプの員数を作り、自由ノードへ配る (本家式)
    const total = widths.reduce((a, b) => a + b, 0)
    const quota: readonly (readonly [MapNodeType, number])[] = [
      ['campfire', Math.round(total * ROOM_WEIGHTS.campfire)], // 本家Rest: 散布される休憩
      // 工房: 幕1はちょうど1個 (2026-08-31 ユーザー指示「合成1幕に1個つけて」= 供給集中を
      // 避けつつ合成の楽しみを前倒し)。幕2/3は重み5%。allowWorkshop=false は全面禁止 (テスト用)
      ['workshop', !allowWorkshop ? 0 : act === 1 ? 1 : Math.round(total * ROOM_WEIGHTS.workshop)], // 員数は据え置き。2026-09-03 裁定「全ルートで1幕に最大1回」は配置ガード (WORKSHOP_PATH_MAX) で保証
      ['shop', SHOP_COUNT], // 固定3/幕 (2026-09-02 StS2式。重み5%は総ノード数で3〜4に揺れ、ゴールドシンク量がシード次第だった)
      ['event', Math.round(total * ROOM_WEIGHTS.event)],
    ]
    // 型グリッド: 文字列キーのMapだと床ガードのDP (数千回) が文字列連結で律速する。
    // 強制行・ボス行も最初から正しい型を持つ (親同種禁止が「行15の焚き火」を弾くために必要)
    const typeGrid: MapNodeType[][] = widths.map((w, r) =>
      Array.from({ length: w }, (): MapNodeType =>
        r === bossRow
          ? 'boss'
          : r === forcedCampfireRow
            ? 'campfire'
            : r === treasureRow
              ? 'treasure'
              : 'battle',
      ),
    )
    const typeAt = (r: number, c: number): MapNodeType => typeGrid[r][c]
    // 自由ノード = 行0 (本家 floor1 は全て通常戦闘)・強制焚き火行・ボス行 を除く全ノード
    const freeNodes: (readonly [number, number])[] = []
    for (let r = 1; r < bossRow; r++) {
      if (r === forcedCampfireRow || r === treasureRow) continue
      for (let c = 0; c < widths[r]; c++) freeNodes.push([r, c])
    }
    const assignable = (r: number, c: number, t: MapNodeType): boolean => {
      if (t === 'elite' && r < (ELITE_MIN_ROW_BY_ACT[act - 1] ?? ELITE_MIN_ROW)) return false
      if (t === 'campfire' && r < CAMPFIRE_MIN_ROW) return false // 本家「6階より下に休憩なし」
      if (t === 'workshop' && act === 1 && r < WORKSHOP_MIN_ROW_ACT1) return false // 5戦ぶんの金が貯まってから
      // ボス前3行に散布焚き火を置かない (2026-09-02 本家StS2「最終3行以内にRestSiteなし」。
      // ボス前の全焚き火行と合わせ「ボス直前に休憩2連」経路を封じる = HP経済の絞りと同方向)
      if (t === 'campfire' && r >= bossRow - 3) return false
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
        if (r + 1 < mapRows && edges[r][c].some((to) => typeAt(r + 1, to) === t)) return false
      }
      return true
    }
    /** エリート供給: 1本のパスで踏める最大数 (DP最大値) */
    const maxElites = (): number => {
      let maxE = Array.from({ length: widths[0] }, () => 0)
      for (let r = 0; r < mapRows - 1; r++) {
        const next = new Array<number>(widths[r + 1]).fill(-Infinity)
        for (let c = 0; c < widths[r]; c++) {
          for (const to of edges[r][c]) {
            const gain = typeAt(r + 1, to) === 'elite' ? 1 : 0
            next[to] = Math.max(next[to], maxE[c] + gain)
          }
        }
        maxE = next
      }
      return maxE[0]
    }
    // 3a. エリート: 素のランダム配置 + 供給保証 (3個踏める経路の存在)。判定はここで行う —
    // 高価な床ガード付き配置の後に回すと、保証落ちのたびに全配置をやり直して生成が
    // 数百msに膨れる (2026-08-31 宝箱行追加後の実測345ms/枚への処方)。安価なので内側でループ
    let eliteOk = false
    for (let tryE = 0; tryE < 20 && !eliteOk; tryE++) {
      for (const [r, c] of freeNodes) if (typeGrid[r][c] === 'elite') typeGrid[r][c] = 'battle'
      let failed = false
      for (let k = 0; k < ELITE_COUNT; k++) {
        // 直前で必ず避けられる: 全ての親に出口2以上 (2026-08-31 Opus検証「行0の選択の副産物で
        // エリート2体が通行料になった」への処方 = 挑む/避けるが常にその場の選択になる)
        const cand = freeNodes.filter(
          ([r, c]) =>
            typeGrid[r][c] === 'battle' &&
            assignable(r, c, 'elite') &&
            parents[r][c].every((p) => edges[r - 1][p].length >= 2),
        )
        if (cand.length === 0) {
          failed = true
          break
        }
        const [i, next] = nextInt(rng, 0, cand.length - 1)
        rng = next
        typeGrid[cand[i][0]][cand[i][1]] = 'elite'
      }
      eliteOk = !failed && maxElites() >= ELITE_PATH_MIN
    }
    if (!eliteOk) continue

    // 3b. 部屋の配置: 本家準拠の素のランダム (制約は assignable のみ)。
    // 戦闘数の床は撤廃 (2026-08-31 ユーザー裁定「床を撤廃・本家完全準拠」——本家に戦闘数の
    // 保証は無く「何回戦うかを選べる」がルート選択の中身。戦闘の少ないパスは報酬・金も
    // 少ない自己均衡。旧・床8は分岐の充実と両立しなかった: 単一出口59%で生成300リトライ)
    // 部屋タイプ数の max-DP (前向き/後ろ向き)。焚き火: F+B+1 = そのノードを焚き火にした時に
    // そこを通るパスが踏める最大数 (ボス前の全焚き火行は typeGrid 経由で自動的に算入される)。
    // 工房ガード (幕1) はエリート数で同じDPを使う
    const typeMaxDP = (forward: boolean, t0: MapNodeType): number[][] => {
      const gain = (r: number, c: number): number => (typeGrid[r][c] === t0 ? 1 : 0)
      const out: number[][] = widths.map((w) => new Array<number>(w).fill(-Infinity))
      if (forward) {
        for (let c = 0; c < widths[0]; c++) out[0][c] = gain(0, c)
        for (let r = 0; r < mapRows - 1; r++) {
          for (let c = 0; c < widths[r]; c++) {
            for (const to of edges[r][c]) {
              out[r + 1][to] = Math.max(out[r + 1][to], out[r][c] + gain(r + 1, to))
            }
          }
        }
      } else {
        out[mapRows - 1][0] = gain(mapRows - 1, 0)
        for (let r = mapRows - 2; r >= 0; r--) {
          for (let c = 0; c < widths[r]; c++) {
            for (const to of edges[r][c]) {
              out[r][c] = Math.max(out[r][c], out[r + 1][to] + gain(r, c))
            }
          }
        }
      }
      return out
    }
    let placementFailed = false
    for (const [t, n] of quota) {
      for (let k = 0; k < n; k++) {
        // 焚き火だけは「置いた後もどのパスも上限4以下」の位置に限る (2026-08-31 ユーザー裁定
        // 「最良ルートで3〜4個」。通らないパスの最大は現在の全体最大(≤上限)以下なので
        // F+B+1 ≤ 上限 ⟺ 置いてよい、の帰納で全パスの上限が保たれる)
        const guard =
          t === 'campfire'
            ? (() => {
                const F = typeMaxDP(true, 'campfire')
                const B = typeMaxDP(false, 'campfire')
                return (r: number, c: number) => F[r][c] + B[r][c] + 1 <= CAMPFIRE_PATH_MAX
              })()
            : t === 'workshop'
              ? (() => {
                  // 2026-09-03 ユーザー裁定「工房は全ルートで1幕に最大1回」: 置いた後もどのパスも工房を
                  // 2回踏めない位置に限る (焚き火の上限4と同じ max-DP。員数は幕1=1・幕2/3=5%のまま)。
                  // 幕1はさらに「エリートも踏める経路の上」(2026-08-31 HP経済ラン: 工房・エリート・4焚き火が
                  // 同一分岐に固まり「工房を取ると幕1エリート全滅」の事故ルート化への処方)
                  const Fw = typeMaxDP(true, 'workshop')
                  const Bw = typeMaxDP(false, 'workshop')
                  const Fe = act === 1 ? typeMaxDP(true, 'elite') : null
                  const Be = act === 1 ? typeMaxDP(false, 'elite') : null
                  return (r: number, c: number) =>
                    Fw[r][c] + Bw[r][c] + 1 <= WORKSHOP_PATH_MAX &&
                    (Fe === null || Be === null || Fe[r][c] + Be[r][c] >= 1)
                })()
              : () => true
        const cand = freeNodes.filter(
          ([r, c]) => typeGrid[r][c] === 'battle' && assignable(r, c, t) && guard(r, c),
        )
        if (cand.length === 0) {
          placementFailed = true
          break
        }
        const [i, next] = nextInt(rng, 0, cand.length - 1)
        rng = next
        typeGrid[cand[i][0]][cand[i][1]] = t
      }
      if (placementFailed) break
    }
    if (placementFailed) continue
    // 3c. ショップ到達保証 (2026-09-02 ユーザー裁定「固定3+到達保証」): 行0のどの開始ノードからも
    // ショップを1回踏める経路が存在する = 「最初の1手でショップの可能性が消える」事故を封じる
    // (2026-08-28 到達事故・幕2継続ランの557G死蔵の機械封じ)。満たさなければ配置ごとやり直し
    {
      const F = typeMaxDP(true, 'shop')
      const B = typeMaxDP(false, 'shop')
      let ok = true
      for (let c = 0; c < widths[0]; c++) {
        if (F[0][c] + B[0][c] < 1) {
          ok = false
          break
        }
      }
      if (!ok) continue
    }

    // 5. ノードの実体化 (直前2行と同じ敵は避ける)。?の中身は持たせない (入室時に決まる)
    const recentEnemies: string[][] = []
    const map: MapNode[][] = []
    const usedElites = new Set<string>()
    const usedInAct = new Set<string>()
    for (let r = 0; r < mapRows; r++) {
      const rowNodes: MapNode[] = []
      const rowEnemies: string[] = []
      for (let c = 0; c < widths[r]; c++) {
        const type: MapNodeType = typeAt(r, c) // 強制行・ボス行も typeAt が正しい型を返す
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
          // 同族連続の回避 (2026-09-02 本家GrabBagの同タグ回避に相当): ID完全一致でなく
          // 「メンバー敵IDの交差」で判定する — 探り屋ソロ→次の行で探り屋の二人組、が素通りしていた。
          // タグのデータ追加なしで家族関係を編成定義そのものから導出する
          const membersOf = (encId: string): readonly string[] =>
            resolveEncounter(encId).map((m) => m.enemyId)
          const recentIds = [...recentEnemies.slice(-2).flat(), ...rowEnemies]
          const recentMembers = new Set(recentIds.flatMap(membersOf))
          const fresh = pool.filter((id) => !membersOf(id).some((m) => recentMembers.has(m)))
          // 幕内で未使用の編成を優先する (2026-09-02 人間ラン#2: 幕2が6戦で4種=走竜×2・甲虫×2。
          // エリート抽選と同じ規則を通常戦闘にも。プールが尽きたら同族回避だけで抽選)
          const unused = fresh.filter((id) => type === 'elite' || !usedInAct.has(id))
          const candidates = unused.length > 0 ? unused : fresh.length > 0 ? fresh : pool
          const [idx, next] = nextInt(rng, 0, candidates.length - 1)
          rng = next
          encounterId = candidates[idx]
          if (type === 'elite') usedElites.add(encounterId)
          if (type === 'battle') usedInAct.add(encounterId)
          rowEnemies.push(encounterId)
        }
        rowNodes.push({
          type,
          encounterId,
          next: r < mapRows - 1 ? edges[r][c] : [],
          col: r === bossRow ? Math.floor(GRID_COLS / 2) : colsOf[r][c],
        })
      }
      recentEnemies.push(rowEnemies)
      map.push(rowNodes)
    }
    // 出現保証 (2026-09-02 人間ラン#2): 幕の「量の器」(ターン装甲) が21戦で一度も出なかった実測への処方。
    // 本帯 (Weak帯より下・ボス行以外) の通常戦闘ノードに ACT_MUST_APPEAR の編成が1つも無ければ、
    // 通常戦闘ノードを1つシードRNGで選んで差し替える (存在保証 = 全パス保証ではない。ボス側の器と対で使う)
    for (const mustId of ACT_MUST_APPEAR[act - 1] ?? []) {
      const present = map.some((row) => row.some((n) => n.type === 'battle' && n.encounterId === mustId))
      if (present) continue
      const spots: [number, number][] = []
      for (let r = WEAK_ROWS[act - 1] ?? 0; r < bossRow; r++) {
        map[r].forEach((n, c) => {
          if (n.type === 'battle') spots.push([r, c])
        })
      }
      if (spots.length === 0) continue
      const [k, next2] = nextInt(rng, 0, spots.length - 1)
      rng = next2
      const [r, c] = spots[k]
      map[r][c] = { ...map[r][c], encounterId: mustId }
    }
    return [map, rng]
  }
  throw new Error('マップ生成が収束しない')
}
