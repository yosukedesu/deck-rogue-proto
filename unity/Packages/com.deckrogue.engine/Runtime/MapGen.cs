// MapGen.cs — ランのマップ生成 (純ロジック。src/engine/map.ts の厳密移植)
// StS式DAG・全体可視。行数は幕別 (15/14/13+ボス行)・強制焚き火行=ボス前。
//
// 生成は本家式パスウォーク:
//   7列格子×6本のパスウォーク。開始列はランダム (最初の2本は別列 = 行0は必ず2ノード以上)、
//   各行で列±1移動。交差防止 = 左隣ノードの最大接続先と右隣ノードの最小接続先で挟むクランプ。
//   共通祖先が5行以内の合流は1回引き直す (小ひし形の抑制 = 本家 getCommonAncestor 準拠)。
//
// **RNG の消費順・回数は TS と1手もずらさないこと** (マップは全てのラン再現の土台)。
// -Infinity を使う DP は TS と同じく double で持つ (int の飽和で挙動が変わるのを避ける)。

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public static class MapGen
    {
        /// <summary>
        /// 幕内に必ず1回は現れる編成 (2026-09-02)。幕3=汚泥の大暴れ (ターン装甲45=多段バーストへの唯一の構造的回答)。
        /// 門番のターン装甲75と対: 「量の器」が幕3で必ず1回は問われる
        /// </summary>
        public static readonly IReadOnlyList<IReadOnlyList<string>> ACT_MUST_APPEAR = new IReadOnlyList<string>[]
        {
            new string[] { },
            new string[] { },
            new[] { "enemy_sludge_berserker" },
        };

        /// <summary>
        /// 量の器の経路保証 (2026-09-05)。幕ごとに「本帯の通常戦闘を K 戦以上踏む経路は、器
        /// (ターン装甲持ちの編成) を1回は踏む」の K。0 = 保証なし
        /// </summary>
        public static readonly IReadOnlyList<int> TURN_ARMOR_PATH_MIN = new[] { 0, 0, 1 };

        /// <summary>エリート専用プール (2026-08-31)。各幕4種 (幕3は5種)</summary>
        public static readonly IReadOnlyList<IReadOnlyList<string>> ELITE_POOLS = new IReadOnlyList<string>[]
        {
            // 1幕: 鬼軍曹 / 歩哨の双子 / 金羽の大鴉 / 大喰らいの蟲
            new[] { "enemy_elite_sergeant", "enc_elite_sentries", "enemy_elite_gold_raven", "enemy_elite_devourer" },
            // 2幕: 眠れる鉄卵 / 奴隷商 / 写し身の魔人 / 読み手の梟
            new[] { "enemy_elite_iron_egg", "enemy_elite_slaver", "enemy_elite_mirror_djinn", "enemy_elite_owl" },
            // 3幕: 刺突の書 / 巨面 / 終焉の唱い手 / 不滅の騎士 / 骸兵 (残機チェーン)
            new[] { "enemy_elite_stab_book", "enemy_elite_giant_face", "enemy_elite_doom_chanter", "enemy_elite_deathless", "enemy_elite_husk_3" },
        };

        /// <summary>
        /// 15行化 (2026-09-01): 本家StS = マップ15階+ボス。行0〜14がマップ・行15=ボス
        /// </summary>
        public const int MAP_ROWS = 16;
        public const int BOSS_ROW = MAP_ROWS - 1;

        /// <summary>幕別の行数 (2026-09-02 StS2式 15/14/13)。行数 = 部屋数+ボス行</summary>
        public static readonly IReadOnlyList<int> ACT_MAP_ROWS = new[] { 16, 15, 14 };

        public static int MapRowsFor(int act)
        {
            int i = act - 1;
            return (i >= 0 && i < ACT_MAP_ROWS.Count) ? ACT_MAP_ROWS[i] : MAP_ROWS;
        }

        public static int BossRowFor(int act) => MapRowsFor(act) - 1;

        /// <summary>宝箱行 = ボスの7行手前 (幕1は無し・幕2=7・幕3=6)</summary>
        public static int TreasureRowFor(int act)
        {
            // 幕1に宝箱行は無い (2026-09-03 曲線パッケージ: レリック供給源の削減)
            if (act <= 1) return -1;
            return BossRowFor(act) - 7;
        }

        /// <summary>全パスが通る強制焚き火行 (ボス前休憩のみ)</summary>
        public static readonly IReadOnlyCollection<int> FORCED_CAMPFIRE_ROWS = new HashSet<int> { BOSS_ROW - 1 };

        /// <summary>宝箱行 (本家の「9階は全ノード宝箱」= 行8)</summary>
        public const int TREASURE_ROW = 8;

        // 本家の部屋タイプ重み
        private const double ROOM_WEIGHT_SHOP = 0.05;
        private const double ROOM_WEIGHT_WORKSHOP = 0.05;
        private const double ROOM_WEIGHT_EVENT = 0.22;
        private const double ROOM_WEIGHT_CAMPFIRE = 0.08; // 本家Rest=0.12→0.08

        /// <summary>焚き火を置ける最小行 (本家「6階より下に休憩なし」= index 5 以降)</summary>
        private const int CAMPFIRE_MIN_ROW = 5;
        /// <summary>幕1の工房は行5以降 (2026-09-03 ユーザー裁定)</summary>
        private const int WORKSHOP_MIN_ROW_ACT1 = 5;
        /// <summary>1本のパスで踏める焚き火の上限 (ボス前の全焚き火行の1回を含む)</summary>
        private const int CAMPFIRE_PATH_MAX = 4;
        /// <summary>どのパスも工房は1幕に最大1回</summary>
        private const int WORKSHOP_PATH_MAX = 1;
        /// <summary>エリートだけは員数固定</summary>
        private const int ELITE_COUNT = 4;
        /// <summary>ショップは固定3/幕 (StS2 NumOfShops=3)</summary>
        private const int SHOP_COUNT = 3;
        /// <summary>エリートを置ける最小行 (既定)</summary>
        private const int ELITE_MIN_ROW = 2;
        /// <summary>幕別のエリート下限行: 幕1は行4 / 幕2・3はWeak帯2行の直後=行2</summary>
        private static readonly IReadOnlyList<int> ELITE_MIN_ROW_BY_ACT = new[] { 4, 2, 2 };
        /// <summary>「エリートを狙うパス」で踏める最低数</summary>
        private const int ELITE_PATH_MIN = 3;
        /// <summary>親と同タイプを禁止する部屋 (本家準拠 = elite/shop/rest。?と戦闘は対象外)</summary>
        private static readonly HashSet<string> PARENT_EXCLUSIVE = new HashSet<string>
        {
            MapNodeTypes.Elite, MapNodeTypes.Shop, MapNodeTypes.Workshop, MapNodeTypes.Campfire,
        };
        /// <summary>生成リトライの上限 (パスとエッジを引き直す単一段階)</summary>
        private const int MAX_PLACEMENT_TRIES = 5000;
        /// <summary>格子の列数と歩かせるパスの本数 (本家: 7列×6本)</summary>
        public const int GRID_COLS = 7;
        private const int PATH_WALKS = 6;
        /// <summary>共通祖先をこの行数まで遡って探す (本家 getCommonAncestor の maxDepth=5)</summary>
        private const int ANCESTOR_DEPTH = 5;

        /// <summary>幕プール制: 幕 → 抽選プール (ソロ敵IDと編成IDの混合)</summary>
        private static readonly IReadOnlyList<IReadOnlyList<string>> ACT_POOLS = new IReadOnlyList<string>[]
        {
            // 1幕
            new[]
            {
                "enemy_wide_power", "enemy_thorn_squirrel", "enemy_apprentice_colossus", "enemy_cultist", "enemy_slug",
                "enemy_mud_lump", "enc_probe_pair", "enc_probe_trio", "enc_squirrel_trio", "enc_mud_mudlings",
                "enc_mudling_swarm", "enc_cultist_imp", "enc_thief_pair", "enc_squirrel_probe", "enc_beast_pair",
                "enc_thief_beast", "enemy_gaping_maw", "enemy_cog_construct", "enemy_vine_walker", "enemy_strangler_serpent",
                "enc_serpent_fruit", "enc_sporecap_fruit", "enc_sporecap_mudlings", "enc_serpent_mudlings", "enc_snapfruit_trio",
            },
            // 2幕
            new[]
            {
                "enemy_set_wary", "enemy_set_breaker", "enemy_bomber", "enc_probe_trio", "enc_joker_drummer",
                "enc_bomber_healer", "enc_hexer_shadow", "enc_joker_hexer", "enc_joker_hexer_drummer", "enc_wary_bomber",
                "enc_bomber_drummer", "enemy_whetstone_colossus", "enemy_mimic_jester", "enemy_cinder_imp", "enemy_rock_beetle",
                "enemy_big_slime", "enc_squire_archer", "enc_raptor_nest", "enemy_maw_hunter", "enemy_bowl_bug",
                "enc_bowlbug_drummer", "enc_imp_jester", "enc_chomper_pair", "enc_scald_gnat_pair",
            },
            // 3幕
            new[]
            {
                "enemy_burrow_worm", "enemy_nemesis_wraith", "enemy_brute", "enemy_moss", "enemy_axe_ogre",
                "enemy_shell_guard", "enemy_set_breaker", "enemy_devoted_sculptor", "enc_globe_drummer", "enc_sculptor_shadow",
                "enc_wolf_drummer", "enc_hexer_shadow", "enc_breaker_hexer", "enc_axe_drummer", "enc_shell_hexer",
                "enc_moss_healer", "enc_fang_twins", "enemy_brood_toad", "enc_mourn_beasts", "enemy_sludge_berserker",
                "enc_wolf_hexer_drummer", "enc_mourn_healer", "enc_axe_shadow", "enc_axe_automatons", "enemy_thunder_globe",
                "enemy_frog_knight", "enc_biting_scrolls_quad", "enc_lost_forgotten",
            },
        };

        /// <summary>Weak帯 (幕頭のN行は教師枠の弱プールからだけ抽選する)</summary>
        private static readonly IReadOnlyList<int> WEAK_ROWS = new[] { 3, 2, 2 };

        private static readonly IReadOnlyList<IReadOnlyList<string>> WEAK_POOLS = new IReadOnlyList<string>[]
        {
            // 幕1: 読みの教師・手数の鏡の予習・状態異常の教師・タイマーの予習
            new[] { "enemy_probe", "enemy_slug", "enc_probe_pair", "enc_mudling_trio", "enemy_sludge_spider", "enemy_iron_clam", "enc_spider_fruit" },
            // 幕2: 伏せ検定・固い小物の教師・手数の鏡
            new[] { "enemy_set_wary", "enemy_rock_beetle", "enemy_mimic_jester", "enc_imp_jester", "enc_beetle_wary" },
            // 幕3: 貫通の的・大技→隙の窓・伏せ罰の教師
            new[] { "enemy_shell_guard", "enemy_axe_ogre", "enemy_set_breaker", "enc_axe_shadow", "enc_wolf_hexer_drummer", "enemy_devoted_sculptor", "enc_biting_scrolls_trio" },
        };

        /// <summary>幕ボスのプール (各幕に複数のボス。ランごとにシードで1体を抽選)</summary>
        public static readonly IReadOnlyList<IReadOnlyList<string>> ACT_BOSS_POOLS = new IReadOnlyList<string>[]
        {
            new[] { "enemy_brute", "enc_kin_ritual" },   // 幕1: 脳筋オーガ / 血族の儀式
            new[] { "enemy_turtle", "enc_kaiser_crab" }, // 幕2: 眠たがりの大亀 / 双腕の巨蟹
            new[] { "enemy_warden", "enemy_chimera_1" }, // 幕3: 門番 / 蘇る合成獣
        };

        /// <summary>各幕の代表ボス (先頭) = 旧テスト・CLI表示の互換用</summary>
        public static readonly IReadOnlyList<string> ACT_BOSSES = ACT_BOSS_POOLS.Select(p => p[0]).ToList();

        public const int ACT_COUNT = 3;

        /// <summary>幕とマップ行 → 敵抽選プール。ボス行は幕ボス</summary>
        public static IReadOnlyList<string> TierFor(int act, int row)
        {
            if (row >= BossRowFor(act)) return ACT_BOSS_POOLS[act - 1];
            int weak = (act - 1 >= 0 && act - 1 < WEAK_ROWS.Count) ? WEAK_ROWS[act - 1] : 0;
            if (row < weak) return WEAK_POOLS[act - 1];
            return ACT_POOLS[act - 1];
        }

        /// <summary>
        /// シードからマップを決定的に生成する (同じシード = 同じマップ)。
        /// </summary>
        public static (IReadOnlyList<IReadOnlyList<MapNode>> Map, RngState Rng) GenerateMap(
            RngState rng0, int act = 1, bool allowWorkshop = true)
        {
            var rng = rng0;
            for (int attempt = 0; attempt <= MAX_PLACEMENT_TRIES; attempt++)
            {
                // 1. 本家式パスウォーク: 7列格子を6本のパスが行0→ボス前行へ歩く
                int mapRows = MapRowsFor(act);
                int bossRow = mapRows - 1;
                int treasureRow = TreasureRowFor(act);
                int forcedCampfireRow = bossRow - 1; // ボス前休憩
                int walkRows = bossRow;              // ボス行はウォーク対象外

                var visited = new List<HashSet<int>>();
                for (int i = 0; i < walkRows; i++) visited.Add(new HashSet<int>());
                // latEdges[y] = 格子列 → 次の行の格子列の集合
                var latEdges = new List<Dictionary<int, HashSet<int>>>();
                for (int i = 0; i < walkRows - 1; i++) latEdges.Add(new Dictionary<int, HashSet<int>>());
                // latParents[y] = 格子列 → 前の行の格子列の集合 (共通祖先の探索用)
                var latParents = new List<Dictionary<int, HashSet<int>>>();
                for (int i = 0; i < walkRows; i++) latParents.Add(new Dictionary<int, HashSet<int>>());

                void AddLatEdge(int y, int from, int to)
                {
                    if (!latEdges[y].TryGetValue(from, out var s)) { s = new HashSet<int>(); latEdges[y][from] = s; }
                    s.Add(to);
                    if (!latParents[y + 1].TryGetValue(to, out var p)) { p = new HashSet<int>(); latParents[y + 1][to] = p; }
                    p.Add(from);
                }

                // (y,a) と (y,b) が ANCESTOR_DEPTH 行以内に共通祖先を持つか (本家 getCommonAncestor 準拠)
                bool NearCommonAncestor(int y, int a, int b)
                {
                    int l = Math.Min(a, b);
                    int r = Math.Max(a, b);
                    for (int row = y; row > 0 && row > y - ANCESTOR_DEPTH; row--)
                    {
                        latParents[row].TryGetValue(l, out var lp);
                        latParents[row].TryGetValue(r, out var rp);
                        if (lp == null || lp.Count == 0 || rp == null || rp.Count == 0) return false;
                        l = lp.Max();
                        r = rp.Min();
                        if (l == r) return true;
                    }
                    return false;
                }

                int ClampCol(int x) => Math.Max(0, Math.Min(GRID_COLS - 1, x));

                var startCols = new List<int>();
                for (int p = 0; p < PATH_WALKS; p++)
                {
                    int x;
                    // 最初の2本は別の列から (行0が必ず2ノード以上になり、開始の選択が生まれる)
                    for (; ; )
                    {
                        var (v, nx0) = Rng.NextInt(rng, 0, GRID_COLS - 1);
                        rng = nx0;
                        x = v;
                        if (p != 1 || x != startCols[0]) break;
                    }
                    startCols.Add(x);
                    visited[0].Add(x);
                    for (int y = 0; y < walkRows - 1; y++)
                    {
                        var (d, nx1) = Rng.NextInt(rng, -1, 1);
                        rng = nx1;
                        int nx = ClampCol(x + d);
                        // 小ひし形の抑制: 合流先の別の親と共通祖先が近いなら1回引き直す
                        var otherParents = new List<int>();
                        if (latParents[y + 1].TryGetValue(nx, out var pset))
                            foreach (var c in pset) if (c != x) otherParents.Add(c);
                        bool anyNear = false;
                        foreach (var c in otherParents) if (NearCommonAncestor(y, x, c)) { anyNear = true; break; }
                        if (anyNear)
                        {
                            var (d2, nx2) = Rng.NextInt(rng, -1, 1);
                            rng = nx2;
                            nx = ClampCol(x + d2);
                        }
                        // 交差防止: 左隣ノードの最大接続先より左へは行けない / 右隣ノードの最小接続先より右へは行けない
                        HashSet<int>? left = null;
                        if (x > 0) latEdges[y].TryGetValue(x - 1, out left);
                        if (left != null && left.Count > 0) nx = Math.Max(nx, left.Max());
                        HashSet<int>? right = null;
                        if (x < GRID_COLS - 1) latEdges[y].TryGetValue(x + 1, out right);
                        if (right != null && right.Count > 0) nx = Math.Min(nx, right.Min());
                        AddLatEdge(y, x, nx);
                        visited[y + 1].Add(nx);
                        x = nx;
                    }
                }

                // 2. 分岐の補強: 出次数1のノードに、隣列 (±1) の既存ノードへの非交差エッジを1本足す
                for (int y = 0; y < walkRows - 1; y++)
                {
                    var cols = visited[y].ToList();
                    cols.Sort();
                    foreach (var col in cols)
                    {
                        if (!latEdges[y].TryGetValue(col, out var cur) || cur.Count != 1) continue;
                        var (coin, nextRng) = Rng.NextInt(rng, 0, 1);
                        rng = nextRng;
                        int baseTo = cur.First();
                        var order = coin == 1 ? new[] { baseTo + 1, baseTo - 1 } : new[] { baseTo - 1, baseTo + 1 };
                        foreach (var cand in order)
                        {
                            if (Math.Abs(cand - col) > 1 || cand < 0 || cand >= GRID_COLS) continue;
                            if (!visited[y + 1].Contains(cand)) continue;
                            // 交差チェック: 同行の他ノードの既存エッジと交差しない場合だけ足す
                            bool ok = true;
                            foreach (var kv in latEdges[y])
                            {
                                int c2 = kv.Key;
                                if (c2 == col) continue;
                                foreach (var t2 in kv.Value)
                                {
                                    if ((c2 - col) * (t2 - cand) < 0) { ok = false; break; }
                                }
                                if (!ok) break;
                            }
                            if (!ok) continue;
                            AddLatEdge(y, col, cand);
                            break;
                        }
                    }
                }

                // 3. 格子 → 行内の詰めた添字へ変換 (ChooseNode.col の互換維持。col に格子列を残す)
                var colsOf = new List<List<int>>();
                foreach (var s in visited) { var l = s.ToList(); l.Sort(); colsOf.Add(l); }
                var widths = new List<int>();
                foreach (var c in colsOf) widths.Add(c.Count);
                widths.Add(1); // +ボス行

                var edges = new List<List<List<int>>>();
                for (int r = 0; r < walkRows - 1; r++)
                {
                    var rowEdges = new List<List<int>>();
                    foreach (var col in colsOf[r])
                    {
                        var tos = new List<int>();
                        if (latEdges[r].TryGetValue(col, out var set)) { tos = set.ToList(); tos.Sort(); }
                        rowEdges.Add(tos.Select(to => colsOf[r + 1].IndexOf(to)).ToList());
                    }
                    edges.Add(rowEdges);
                }
                // ボス前休憩行の全ノード → ボス
                {
                    var last = new List<List<int>>();
                    foreach (var _ in colsOf[walkRows - 1]) last.Add(new List<int> { 0 });
                    edges.Add(last);
                }
                // 親テーブル (本家の親/兄弟制約の判定に使う)
                var parents = new List<List<List<int>>>();
                foreach (var w in widths)
                {
                    var row = new List<List<int>>();
                    for (int i = 0; i < w; i++) row.Add(new List<int>());
                    parents.Add(row);
                }
                for (int r = 0; r < mapRows - 1; r++)
                    for (int c = 0; c < widths[r]; c++)
                        foreach (var to in edges[r][c]) parents[r + 1][to].Add(c);

                // 3. 部屋タイプの員数を作り、自由ノードへ配る (本家式)
                int total = 0;
                foreach (var w in widths) total += w;
                var quota = new List<(string Type, int Count)>
                {
                    (MapNodeTypes.Campfire, JsRound(total * ROOM_WEIGHT_CAMPFIRE)),
                    // 工房: 幕1はちょうど1個。幕2/3は重み5%。allowWorkshop=false は全面禁止 (テスト用)
                    (MapNodeTypes.Workshop, !allowWorkshop ? 0 : act == 1 ? 1 : JsRound(total * ROOM_WEIGHT_WORKSHOP)),
                    (MapNodeTypes.Shop, SHOP_COUNT),
                    (MapNodeTypes.Event, JsRound(total * ROOM_WEIGHT_EVENT)),
                };

                // 型グリッド: 強制行・ボス行も最初から正しい型を持つ
                var typeGrid = new List<string[]>();
                for (int r = 0; r < widths.Count; r++)
                {
                    var row = new string[widths[r]];
                    string t = r == bossRow ? MapNodeTypes.Boss
                        : r == forcedCampfireRow ? MapNodeTypes.Campfire
                        : r == treasureRow ? MapNodeTypes.Treasure
                        : MapNodeTypes.Battle;
                    for (int c = 0; c < widths[r]; c++) row[c] = t;
                    typeGrid.Add(row);
                }
                string TypeAt(int r, int c) => typeGrid[r][c];

                // 自由ノード = 行0・強制焚き火行・宝箱行・ボス行 を除く全ノード
                var freeNodes = new List<(int R, int C)>();
                for (int r = 1; r < bossRow; r++)
                {
                    if (r == forcedCampfireRow || r == treasureRow) continue;
                    for (int c = 0; c < widths[r]; c++) freeNodes.Add((r, c));
                }

                bool Assignable(int r, int c, string t)
                {
                    int eliteMin = (act - 1 >= 0 && act - 1 < ELITE_MIN_ROW_BY_ACT.Count) ? ELITE_MIN_ROW_BY_ACT[act - 1] : ELITE_MIN_ROW;
                    if (t == MapNodeTypes.Elite && r < eliteMin) return false;
                    if (t == MapNodeTypes.Campfire && r < CAMPFIRE_MIN_ROW) return false; // 本家「6階より下に休憩なし」
                    if (t == MapNodeTypes.Workshop && act == 1 && r < WORKSHOP_MIN_ROW_ACT1) return false;
                    // ボス前3行に散布焚き火を置かない
                    if (t == MapNodeTypes.Campfire && r >= bossRow - 3) return false;
                    // 兄弟同種禁止: 同じ親を共有する同行ノードと同タイプにしない
                    for (int c2 = 0; c2 < widths[r]; c2++)
                    {
                        if (c2 == c || TypeAt(r, c2) != t) continue;
                        foreach (var p in parents[r][c2]) if (parents[r][c].Contains(p)) return false;
                    }
                    // 親同種禁止: エリート・ショップ・工房・焚き火のみ (親側・子側の両方を見る)
                    if (PARENT_EXCLUSIVE.Contains(t))
                    {
                        foreach (var p in parents[r][c]) if (TypeAt(r - 1, p) == t) return false;
                        if (r + 1 < mapRows) foreach (var to in edges[r][c]) if (TypeAt(r + 1, to) == t) return false;
                    }
                    return true;
                }

                // エリート供給: 1本のパスで踏める最大数 (DP最大値)
                double MaxElites()
                {
                    var maxE = new double[widths[0]];
                    for (int r = 0; r < mapRows - 1; r++)
                    {
                        var next = new double[widths[r + 1]];
                        for (int i = 0; i < next.Length; i++) next[i] = double.NegativeInfinity;
                        for (int c = 0; c < widths[r]; c++)
                        {
                            foreach (var to in edges[r][c])
                            {
                                double gain = TypeAt(r + 1, to) == MapNodeTypes.Elite ? 1 : 0;
                                next[to] = Math.Max(next[to], maxE[c] + gain);
                            }
                        }
                        maxE = next;
                    }
                    return maxE[0];
                }

                // 3a. エリート: 素のランダム配置 + 供給保証 (3個踏める経路の存在)
                bool eliteOk = false;
                for (int tryE = 0; tryE < 20 && !eliteOk; tryE++)
                {
                    foreach (var (r, c) in freeNodes) if (typeGrid[r][c] == MapNodeTypes.Elite) typeGrid[r][c] = MapNodeTypes.Battle;
                    bool failed = false;
                    for (int k = 0; k < ELITE_COUNT; k++)
                    {
                        // 直前で必ず避けられる: 全ての親に出口2以上
                        var cand = new List<(int R, int C)>();
                        foreach (var (r, c) in freeNodes)
                        {
                            if (typeGrid[r][c] != MapNodeTypes.Battle) continue;
                            if (!Assignable(r, c, MapNodeTypes.Elite)) continue;
                            bool everyParentBranches = true;
                            foreach (var p in parents[r][c]) if (edges[r - 1][p].Count < 2) { everyParentBranches = false; break; }
                            if (!everyParentBranches) continue;
                            cand.Add((r, c));
                        }
                        if (cand.Count == 0) { failed = true; break; }
                        var (i, nx) = Rng.NextInt(rng, 0, cand.Count - 1);
                        rng = nx;
                        typeGrid[cand[i].R][cand[i].C] = MapNodeTypes.Elite;
                    }
                    eliteOk = !failed && MaxElites() >= ELITE_PATH_MIN;
                }
                if (!eliteOk) continue;

                // 3b. 部屋の配置: 本家準拠の素のランダム (制約は Assignable のみ)。
                // 部屋タイプ数の max-DP (前向き/後ろ向き)
                double[][] TypeMaxDP(bool forward, string t0)
                {
                    double Gain(int r, int c) => typeGrid[r][c] == t0 ? 1 : 0;
                    var outp = new double[widths.Count][];
                    for (int r = 0; r < widths.Count; r++)
                    {
                        outp[r] = new double[widths[r]];
                        for (int c = 0; c < widths[r]; c++) outp[r][c] = double.NegativeInfinity;
                    }
                    if (forward)
                    {
                        for (int c = 0; c < widths[0]; c++) outp[0][c] = Gain(0, c);
                        for (int r = 0; r < mapRows - 1; r++)
                            for (int c = 0; c < widths[r]; c++)
                                foreach (var to in edges[r][c])
                                    outp[r + 1][to] = Math.Max(outp[r + 1][to], outp[r][c] + Gain(r + 1, to));
                    }
                    else
                    {
                        outp[mapRows - 1][0] = Gain(mapRows - 1, 0);
                        for (int r = mapRows - 2; r >= 0; r--)
                            for (int c = 0; c < widths[r]; c++)
                                foreach (var to in edges[r][c])
                                    outp[r][c] = Math.Max(outp[r][c], outp[r + 1][to] + Gain(r, c));
                    }
                    return outp;
                }

                bool placementFailed = false;
                foreach (var (t, n) in quota)
                {
                    for (int k = 0; k < n; k++)
                    {
                        // 焚き火だけは「置いた後もどのパスも上限4以下」の位置に限る
                        Func<int, int, bool> guard;
                        if (t == MapNodeTypes.Campfire)
                        {
                            var F = TypeMaxDP(true, MapNodeTypes.Campfire);
                            var B = TypeMaxDP(false, MapNodeTypes.Campfire);
                            guard = (rr, cc) => F[rr][cc] + B[rr][cc] + 1 <= CAMPFIRE_PATH_MAX;
                        }
                        else if (t == MapNodeTypes.Workshop)
                        {
                            // 工房は全ルートで1幕に最大1回。幕1はさらに「エリートも踏める経路の上」
                            var Fw = TypeMaxDP(true, MapNodeTypes.Workshop);
                            var Bw = TypeMaxDP(false, MapNodeTypes.Workshop);
                            var Fe = act == 1 ? TypeMaxDP(true, MapNodeTypes.Elite) : null;
                            var Be = act == 1 ? TypeMaxDP(false, MapNodeTypes.Elite) : null;
                            guard = (rr, cc) =>
                                Fw[rr][cc] + Bw[rr][cc] + 1 <= WORKSHOP_PATH_MAX &&
                                (Fe == null || Be == null || Fe[rr][cc] + Be[rr][cc] >= 1);
                        }
                        else
                        {
                            guard = (rr, cc) => true;
                        }
                        var cand = new List<(int R, int C)>();
                        foreach (var (r, c) in freeNodes)
                        {
                            if (typeGrid[r][c] != MapNodeTypes.Battle) continue;
                            if (!Assignable(r, c, t)) continue;
                            if (!guard(r, c)) continue;
                            cand.Add((r, c));
                        }
                        if (cand.Count == 0) { placementFailed = true; break; }
                        var (i, nx) = Rng.NextInt(rng, 0, cand.Count - 1);
                        rng = nx;
                        typeGrid[cand[i].R][cand[i].C] = t;
                    }
                    if (placementFailed) break;
                }
                if (placementFailed) continue;

                // 3c. ショップ到達保証: 行0のどの開始ノードからもショップを1回踏める経路が存在する
                {
                    var F = TypeMaxDP(true, MapNodeTypes.Shop);
                    var B = TypeMaxDP(false, MapNodeTypes.Shop);
                    bool ok = true;
                    for (int c = 0; c < widths[0]; c++)
                    {
                        if (F[0][c] + B[0][c] < 1) { ok = false; break; }
                    }
                    if (!ok) continue;
                }

                // 5. ノードの実体化 (直前2行と同じ敵は避ける)。?の中身は持たせない (入室時に決まる)
                var recentEnemies = new List<List<string>>();
                var map = new List<List<MapNode>>();
                var usedElites = new HashSet<string>();
                var usedInAct = new HashSet<string>();
                for (int r = 0; r < mapRows; r++)
                {
                    var rowNodes = new List<MapNode>();
                    var rowEnemies = new List<string>();
                    for (int c = 0; c < widths[r]; c++)
                    {
                        string type = TypeAt(r, c);
                        string? encounterId = null;
                        if (type == MapNodeTypes.Battle || type == MapNodeTypes.Elite || type == MapNodeTypes.Boss)
                        {
                            var basePool = TierFor(act, r);
                            // エリートは幕内で未使用の個体を優先する
                            IReadOnlyList<string> pool;
                            if (type == MapNodeTypes.Elite)
                            {
                                var all = ELITE_POOLS[act - 1];
                                var freshE = all.Where(id => !usedElites.Contains(id)).ToList();
                                pool = freshE.Count > 0 ? (IReadOnlyList<string>)freshE : all;
                            }
                            else pool = basePool;
                            // 同族連続の回避: ID完全一致でなく「メンバー敵IDの交差」で判定する
                            var recentIds = new List<string>();
                            for (int k = Math.Max(0, recentEnemies.Count - 2); k < recentEnemies.Count; k++)
                                recentIds.AddRange(recentEnemies[k]);
                            recentIds.AddRange(rowEnemies);
                            var recentMembers = new HashSet<string>();
                            foreach (var id in recentIds) foreach (var m in MembersOf(id)) recentMembers.Add(m);
                            var fresh = pool.Where(id => !MembersOf(id).Any(m => recentMembers.Contains(m))).ToList();
                            // 幕内で未使用の編成を優先する
                            var unused = fresh.Where(id => type == MapNodeTypes.Elite || !usedInAct.Contains(id)).ToList();
                            IReadOnlyList<string> candidates = unused.Count > 0 ? (IReadOnlyList<string>)unused : fresh.Count > 0 ? (IReadOnlyList<string>)fresh : pool;
                            var (idx, nx) = Rng.NextInt(rng, 0, candidates.Count - 1);
                            rng = nx;
                            encounterId = candidates[idx];
                            if (type == MapNodeTypes.Elite) usedElites.Add(encounterId);
                            if (type == MapNodeTypes.Battle) usedInAct.Add(encounterId);
                            rowEnemies.Add(encounterId);
                        }
                        rowNodes.Add(new MapNode
                        {
                            Type = type,
                            EncounterId = encounterId,
                            Next = r < mapRows - 1 ? (IReadOnlyList<int>)edges[r][c] : new List<int>(),
                            Col = r == bossRow ? (int)Math.Floor(GRID_COLS / 2.0) : colsOf[r][c],
                        });
                    }
                    recentEnemies.Add(rowEnemies);
                    map.Add(rowNodes);
                }

                // 出現保証: 本帯の通常戦闘ノードに ACT_MUST_APPEAR の編成が1つも無ければ1つ差し替える
                var mustList = (act - 1 >= 0 && act - 1 < ACT_MUST_APPEAR.Count) ? ACT_MUST_APPEAR[act - 1] : (IReadOnlyList<string>)new string[0];
                foreach (var mustId in mustList)
                {
                    bool present = map.Any(row => row.Any(n => n.Type == MapNodeTypes.Battle && n.EncounterId == mustId));
                    if (present) continue;
                    var spots = new List<(int R, int C)>();
                    int weakStart = (act - 1 >= 0 && act - 1 < WEAK_ROWS.Count) ? WEAK_ROWS[act - 1] : 0;
                    for (int r = weakStart; r < bossRow; r++)
                        for (int c = 0; c < map[r].Count; c++)
                            if (map[r][c].Type == MapNodeTypes.Battle) spots.Add((r, c));
                    if (spots.Count == 0) continue;
                    var (k2, nx2) = Rng.NextInt(rng, 0, spots.Count - 1);
                    rng = nx2;
                    var (rr, cc) = spots[k2];
                    map[rr][cc] = map[rr][cc] with { EncounterId = mustId };
                }

                // 量の器の経路保証 (TURN_ARMOR_PATH_MIN)
                int minBattles = (act - 1 >= 0 && act - 1 < TURN_ARMOR_PATH_MIN.Count) ? TURN_ARMOR_PATH_MIN[act - 1] : 0;
                if (minBattles > 0)
                {
                    bool IsVessel(string? encId) =>
                        encId != null && Content.ResolveEncounter(encId).Any(m => Content.GetEnemyDef(m.EnemyId).TurnArmor.HasValue);
                    var vesselPool = ACT_POOLS[act - 1].Where(id => IsVessel(id)).ToList();
                    int weakRows = (act - 1 >= 0 && act - 1 < WEAK_ROWS.Count) ? WEAK_ROWS[act - 1] : 0;
                    IReadOnlyList<string> MembersOfN(string? encId) =>
                        encId == null ? (IReadOnlyList<string>)new string[0] : Content.ResolveEncounter(encId).Select(m => m.EnemyId).ToList();
                    int last = map.Count - 1;

                    // 器を通らない経路だけを辿り、その経路上の本帯の通常戦闘数の最大を前向き/後ろ向きに求める
                    (double[][] F, double[][] B) MaxBattlesDP(Func<int, int, bool> vessel)
                    {
                        double Battle(int r, int c) => (r >= weakRows && r < last && map[r][c].Type == MapNodeTypes.Battle) ? 1 : 0;
                        var F = new double[map.Count][];
                        var B = new double[map.Count][];
                        for (int r = 0; r < map.Count; r++)
                        {
                            F[r] = new double[map[r].Count];
                            B[r] = new double[map[r].Count];
                            for (int c = 0; c < map[r].Count; c++) { F[r][c] = double.NegativeInfinity; B[r][c] = double.NegativeInfinity; }
                        }
                        for (int c = 0; c < map[0].Count; c++) if (!vessel(0, c)) F[0][c] = Battle(0, c);
                        for (int r = 0; r < last; r++)
                        {
                            for (int c = 0; c < map[r].Count; c++)
                            {
                                if (double.IsNegativeInfinity(F[r][c])) continue;
                                foreach (var to in map[r][c].Next)
                                    if (!vessel(r + 1, to)) F[r + 1][to] = Math.Max(F[r + 1][to], F[r][c] + Battle(r + 1, to));
                            }
                        }
                        for (int c = 0; c < map[last].Count; c++) if (!vessel(last, c)) B[last][c] = Battle(last, c);
                        for (int r = last - 1; r >= 0; r--)
                        {
                            for (int c = 0; c < map[r].Count; c++)
                            {
                                if (vessel(r, c)) continue;
                                foreach (var to in map[r][c].Next)
                                    if (!double.IsNegativeInfinity(B[r + 1][to])) B[r][c] = Math.Max(B[r][c], B[r + 1][to] + Battle(r, c));
                            }
                        }
                        return (F, B);
                    }

                    List<(int R, int C)> Deficient(Func<int, int, bool> vessel)
                    {
                        var (F, B) = MaxBattlesDP(vessel);
                        var outp = new List<(int, int)>();
                        for (int r = weakRows; r < last; r++)
                        {
                            for (int c = 0; c < map[r].Count; c++)
                            {
                                var node = map[r][c];
                                if (node.Type == MapNodeTypes.Battle && !vessel(r, c)
                                    && !double.IsNegativeInfinity(F[r][c]) && !double.IsNegativeInfinity(B[r][c])
                                    && F[r][c] + B[r][c] - 1 >= minBattles) outp.Add((r, c));
                            }
                        }
                        return outp;
                    }

                    for (int iter = 0; iter < 80 && vesselPool.Count > 0; iter++)
                    {
                        Func<int, int, bool> vesselNow = (vr, vc) => map[vr][vc].Type == MapNodeTypes.Battle && IsVessel(map[vr][vc].EncounterId);
                        var spots = Deficient(vesselNow);
                        if (spots.Count == 0) break;
                        // 差し替え先は「そこを器にした時に残る不足ノードが最少」のノード (貪欲)。同点はシードRNG
                        var scored = new List<(int Score, int R, int C)>();
                        foreach (var (rr0, cc0) in spots)
                        {
                            int rr = rr0, cc = cc0;
                            scored.Add((Deficient((r2, c2) => (r2 == rr && c2 == cc) || vesselNow(r2, c2)).Count, rr, cc));
                        }
                        int best = scored.Min(x => x.Score);
                        var bestSpots = scored.Where(x => x.Score == best).Select(x => (x.R, x.C)).ToList();
                        var (k3, nx3) = Rng.NextInt(rng, 0, bestSpots.Count - 1);
                        rng = nx3;
                        var (r4, c4) = bestSpots[k3];
                        // 幕内未使用 → 隣の行と同族でない → 器なら何でも
                        var usedNow = new HashSet<string>();
                        foreach (var row in map) foreach (var node in row)
                            if (node.Type == MapNodeTypes.Battle && node.EncounterId != null) usedNow.Add(node.EncounterId);
                        var near = new HashSet<string>();
                        if (r4 - 1 >= 0) foreach (var node in map[r4 - 1]) foreach (var m in MembersOfN(node.EncounterId)) near.Add(m);
                        if (r4 + 1 < map.Count) foreach (var node in map[r4 + 1]) foreach (var m in MembersOfN(node.EncounterId)) near.Add(m);
                        var freshV = vesselPool.Where(id => !MembersOfN(id).Any(m => near.Contains(m))).ToList();
                        var unusedFresh = freshV.Where(id => !usedNow.Contains(id)).ToList();
                        var unusedAny = vesselPool.Where(id => !usedNow.Contains(id)).ToList();
                        List<string> cands = unusedFresh.Count > 0 ? unusedFresh : unusedAny.Count > 0 ? unusedAny : freshV.Count > 0 ? freshV : vesselPool;
                        var (v5, nx5) = Rng.NextInt(rng, 0, cands.Count - 1);
                        rng = nx5;
                        map[r4][c4] = map[r4][c4] with { EncounterId = cands[v5] };
                    }
                }

                var result = map.Select(row => (IReadOnlyList<MapNode>)row).ToList();
                return (result, rng);
            }
            throw new InvalidOperationException("マップ生成が収束しない");
        }

        /// <summary>編成IDのメンバー敵ID列 (同族回避の判定に使う)</summary>
        private static IReadOnlyList<string> MembersOf(string encId) =>
            Content.ResolveEncounter(encId).Select(m => m.EnemyId).ToList();

        /// <summary>JS の Math.round (0.5 は正の無限大方向)</summary>
        private static int JsRound(double x) => (int)Math.Floor(x + 0.5);
    }
}
