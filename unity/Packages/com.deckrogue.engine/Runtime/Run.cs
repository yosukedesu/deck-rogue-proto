// Run.cs — ドラフト連戦モード (純ロジック。src/engine/run.ts の厳密移植)
// マップラン: StS式DAGを1ノードずつ進む。戦闘勝利で提示から1枚ピック (スキップ可) → マップで次のノードを選ぶ。
// 敵は行の帯で深度スケーリング (強化+HP倍率) され、だんだん強くなる。HPは持ち越し、焚き火で回復。
// ラン専用RNGをシードから回すため、同じシード+同じコマンド列=同じラン (リプレイ可能)。
//
// **RNG の消費順・回数は TS と1手もずらさないこと**。
// JS の Math.round は floor(x+0.5)、Math.trunc は 0方向切り捨て、Math.floor は床。

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    /// <summary>createRun の第6引数 opts (TS: { revealIntents?, setAnyCards? })</summary>
    public sealed record RunOptions
    {
        public bool? RevealIntents { get; init; }
        public bool? SetAnyCards { get; init; }
    }

    /// <summary>難易度倍率 (DIFFICULTY_TABLE の1行)</summary>
    public sealed record DifficultyScaleEntry
    {
        public double Hp { get; init; }
        public double Atk { get; init; }
    }

    public static class Run
    {
        // ==== 小さな移植ヘルパ ====

        /// <summary>JS の Math.round (= floor(x + 0.5))。C# の Math.Round は銀行丸めなので使わない</summary>
        private static int JsRound(double v) => (int)Math.Floor(v + 0.5);
        /// <summary>JS の Math.floor (double で計算してから床)</summary>
        private static int JsFloor(double v) => (int)Math.Floor(v);
        /// <summary>JS の Math.trunc (0方向)</summary>
        private static int JsTrunc(double v) => (int)v;

        private static List<T> Append<T>(IReadOnlyList<T> a, T b)
        {
            var outp = new List<T>(a) { b };
            return outp;
        }

        private static List<T> Concat<T>(IReadOnlyList<T> a, IEnumerable<T> b)
        {
            var outp = new List<T>(a);
            outp.AddRange(b);
            return outp;
        }

        /// <summary>報酬プールから除外する基本札 (スターターに入っている素のカード)</summary>
        public static readonly HashSet<string> REWARD_EXCLUDED = new HashSet<string>
        {
            "black_shiv_token",  // 骨のナイフ: 生成トークン (この戦闘限り)
            "white_perm_page",   // 見習い: 見習いの列が召喚する従者トークン
            "green_strike",
            "green_guard",
            "green_basic_bash",  // 打ち据え (スターターのBash枠)
            // スターターのリアクション2枚 (中立スターター化の追随)
            "green_reaction_thorns",
            "blue_strike",
            "blue_guard",
            "blue_counterspell",
            "blue_frost_veil",
            // --- スターター個性注入の追随 ---
            "green_entangle",
            "green_vine_wedge",
            "blue_ice_lance",
            "blue_ponder",
            "blue_tide_drop",
            "red_spark",
            "red_ignite",
            "red_perm_flarecoat",
            "red_perm_thorn_flame",
            "white_shield_strike",
            "white_perm_squire",
            "white_service",
            "white_bodyslam",
            "black_dark_pact",
            "black_drain",
            "black_bursting_corpse",
            "black_shadow_blade",
            "white_reaction_ward",
            "white_reaction_retribution",
            "black_reaction_curse",
            "black_reaction_grudge",
            "red_strike",
            "red_guard",
            "white_strike",
            "white_guard",
            "black_strike",
            "black_guard",
        };

        /// <summary>焚き火の回復比率 (2026-09-04 0.25→0.3)</summary>
        private const double CAMPFIRE_HEAL_RATIO = 0.3;
        /// <summary>勝利ごとの自動回復は廃止 (回復は焚き火のみ)</summary>
        private const int VICTORY_HEAL = 0;
        /// <summary>エリート補正は廃止 (エリート専用敵化)</summary>
        private const int ELITE_STRENGTH = 0;

        // ゴールド
        private const int STARTING_GOLD = 50;
        private const int GOLD_PER_BATTLE_MIN = 12;
        private const int GOLD_PER_BATTLE_MAX = 18;
        private const int GOLD_ELITE_BONUS_MIN = 30;
        private const int GOLD_ELITE_BONUS_MAX = 40;
        /// <summary>盗人を逃がす前に倒した時の懸賞金</summary>
        private const int THIEF_BOUNTY = 10;

        /// <summary>?マスの累積確率の基礎値 (本家 monster10%/shop3%/treasure2%)。整数パーセントポイント</summary>
        private const int UNKNOWN_PITY_BASE_MONSTER = 10;
        private const int UNKNOWN_PITY_BASE_SHOP = 3;
        private const int UNKNOWN_PITY_BASE_TREASURE = 2;
        private static RunStateUnknownPity UnknownPityBase() => new RunStateUnknownPity
        {
            Monster = UNKNOWN_PITY_BASE_MONSTER,
            Shop = UNKNOWN_PITY_BASE_SHOP,
            Treasure = UNKNOWN_PITY_BASE_TREASURE,
        };

        /// <summary>イベント抽選で祠プールを引く確率 (本家 SHRINE_CHANCE = 0.25)</summary>
        private const int SHRINE_CHANCE_PERCENT = 25;
        private const int SHOP_CARD_COUNT = 5;
        private const int SHOP_RELIC_PRICE = 150;
        /// <summary>除去サービス: 回数無制限・使うたびラン通算で+25G</summary>
        private const int SHOP_REMOVAL_BASE = 50;
        private const int SHOP_REMOVAL_STEP = 25;
        /// <summary>強化サービス: 回数無制限・使うたびラン通算で+50G</summary>
        private const int SHOP_UPGRADE_BASE = 100;
        private const int SHOP_UPGRADE_STEP = 50;

        /// <summary>工房の合成1回の価格 (強化100Gと同額=圧縮+強化の対価)</summary>
        public const int WORKSHOP_FUSE_PRICE = 100;

        public static int WorkshopFusePrice(RunState run)
        {
            return Math.Max(0, WORKSHOP_FUSE_PRICE - RelicBonusSum(run, "fusionDiscount")); // 大工の道具
        }

        /// <summary>B型レリックの数値ボーナスの合計 (所持レリックから毎回導出 = RunState にフィールドを増やさない)</summary>
        public static int RelicBonusSum(RunState run, string key)
        {
            int a = 0;
            foreach (var id in run.Relics)
            {
                var b = Content.GetRelicDef(id).Bonus;
                if (b == null) continue;
                int? v = key switch
                {
                    "victoryHealFlat" => b.VictoryHealFlat,
                    "shopUpgradeDiscount" => b.ShopUpgradeDiscount,
                    "restMaxHp" => b.RestMaxHp,
                    "eliteGoldBonus" => b.EliteGoldBonus,
                    "fusionDiscount" => b.FusionDiscount,
                    "removalStepDelta" => b.RemovalStepDelta,
                    _ => null,
                };
                a += v ?? 0;
            }
            return a;
        }

        /// <summary>ショップの価格倍率 (会員証=0.5。複数所持は積)</summary>
        public static double ShopPriceRatio(RunState run)
        {
            double a = 1;
            foreach (var id in run.Relics) a *= Content.GetRelicDef(id).Bonus?.ShopPriceRatio ?? 1;
            return a;
        }

        /// <summary>勝利ゴールドの倍率 (金の靴=1.5)</summary>
        private static double GoldMultiplier(RunState run)
        {
            double a = 1;
            foreach (var id in run.Relics) a *= Content.GetRelicDef(id).Bonus?.GoldMultiplier ?? 1;
            return a;
        }

        public static int ShopRemovalPrice(RunState run)
        {
            int step = Math.Max(0, SHOP_REMOVAL_STEP + RelicBonusSum(run, "removalStepDelta")); // 除去の鑿
            return JsFloor((SHOP_REMOVAL_BASE + step * run.RemovalCount) * ShopPriceRatio(run));
        }

        public static int ShopUpgradePrice(RunState run)
        {
            int baseP = Math.Max(0, SHOP_UPGRADE_BASE - RelicBonusSum(run, "shopUpgradeDiscount")); // 砥石の欠片
            return JsFloor((baseP + SHOP_UPGRADE_STEP * run.UpgradeCount) * ShopPriceRatio(run));
        }

        /// <summary>深度スケーリング: 敵の初期強化 (若い個体補正は撤廃。幕ボスのみ+1)</summary>
        public static int DepthStrength(int row, int act = 1)
        {
            return row >= MapGen.BossRowFor(act) ? 1 : 0;
        }

        /// <summary>難易度10段階。段3＝現状維持 (×1.0/×1.0)。打点優先で伸ばす</summary>
        public static readonly IReadOnlyList<DifficultyScaleEntry> DIFFICULTY_TABLE = new[]
        {
            new DifficultyScaleEntry { Hp = 0.85, Atk = 0.85 }, // 1
            new DifficultyScaleEntry { Hp = 0.95, Atk = 0.95 }, // 2
            new DifficultyScaleEntry { Hp = 1.0, Atk = 1.0 },   // 3 = 既定
            new DifficultyScaleEntry { Hp = 1.05, Atk = 1.15 }, // 4
            new DifficultyScaleEntry { Hp = 1.1, Atk = 1.35 },  // 5
            new DifficultyScaleEntry { Hp = 1.15, Atk = 1.6 },  // 6
            new DifficultyScaleEntry { Hp = 1.2, Atk = 1.9 },   // 7
            new DifficultyScaleEntry { Hp = 1.25, Atk = 2.2 },  // 8
            new DifficultyScaleEntry { Hp = 1.3, Atk = 2.6 },   // 9
            new DifficultyScaleEntry { Hp = 1.35, Atk = 3.0 },  // 10
        };

        public const int DEFAULT_DIFFICULTY = 3;

        /// <summary>難易度→倍率。範囲外と旧セーブの欠落 (null) は既定3へ丸める</summary>
        public static DifficultyScaleEntry DifficultyScale(int? level)
        {
            int n = level.HasValue ? level.Value : DEFAULT_DIFFICULTY;
            return DIFFICULTY_TABLE[Math.Min(DIFFICULTY_TABLE.Count, Math.Max(1, n)) - 1];
        }

        /// <summary>深度スケーリング: 敵HP倍率 (幕×幕内前後半の2段スケール)</summary>
        public static double DepthHpScale(int row, int act = 1)
        {
            if (row >= MapGen.BossRowFor(act)) return 1.0; // 幕ボスは素のHP
            // 幕内前後半の境界 = ボス行の半分
            bool late = row >= (int)Math.Floor(MapGen.BossRowFor(act) / 2.0);
            double[][] table =
            {
                new[] { 0.62, 0.72 }, // 1幕
                new[] { 1.05, 1.15 }, // 2幕
                new[] { 1.2, 1.3 },   // 3幕
            };
            var pair = table[act - 1];
            return late ? pair[1] : pair[0];
        }

        /// <summary>現在いるノード (row=-1 の開始前は null)</summary>
        public static MapNode? CurrentNode(RunState run)
        {
            return run.Row >= 0 ? run.Map[run.Row][run.Col] : null;
        }

        /// <summary>マップで次に進めるノードの列リスト (開始前は行0の全ノード)</summary>
        public static IReadOnlyList<int> NextChoices(RunState run)
        {
            if (run.Row < 0)
            {
                var all = new List<int>();
                for (int c = 0; c < run.Map[0].Count; c++) all.Add(c);
                return all;
            }
            // 実マップの行数から判定する (旧セーブのソフトロック回避)
            if (run.Row >= run.Map.Count - 1) return new List<int>();
            var node = CurrentNode(run);
            return node?.Next ?? (IReadOnlyList<int>)new List<int>();
        }

        /// <summary>現在ノードの戦闘を開始する (戦闘シードはラン RNG から決定的に生成)。elite でエリート補正</summary>
        private static RunState LaunchCombat(RunState run, bool elite, string? encounterOverride = null)
        {
            var node = CurrentNode(run);
            // encounterOverride は ?マスが戦闘に解決した時の敵 (ノードは encounterId を持たない)
            string? encounterId = encounterOverride ?? node?.EncounterId;
            if (node == null || encounterId == null) throw new InvalidOperationException("戦闘ノードではない");
            // 戦闘シード: TS の nextInt(rng, 0, 2**31-1) と同じ1消費 (Rng.NextInt は幅を long で計算するので溢れない)
            var (combatSeed, rng) = Rng.NextInt(run.Rng, 0, int.MaxValue);
            // 難易度倍率: 全敵一律で既存スケールの上に乗算
            var diff = DifficultyScale(run.Difficulty);
            double[] bossHpByAct = { 1.35, 2.3, 2.4 };
            int[] bossStrByAct = { 1, 1, 2 };
            double[] atkByAct = { 1, 1.15, 1.15 };

            var relicPerms = new List<CardInstance>();
            foreach (var id in run.Relics)
            {
                var r = Content.GetRelicDef(id);
                if ((r.Effects?.Count ?? 0) <= 0) continue;
                // 鎖の首輪: エリート・ボス戦にだけ注入
                if (r.EliteBossOnly == true && !elite && node.Type != MapNodeTypes.Boss) continue;
                relicPerms.Add(Content.BuildRelicPermanent(r));
            }
            int setDamageReduction = 0;
            foreach (var id in run.Relics) setDamageReduction += Content.GetRelicDef(id).CombatRule?.SetDamageReduction ?? 0;
            bool revealIntents = run.DebugRevealIntents == true
                || run.Relics.Any(id => Content.GetRelicDef(id).CombatRule?.RevealIntents == true);
            bool revealOnSet = run.Relics.Any(id => Content.GetRelicDef(id).CombatRule?.RevealOnSet == true);
            bool retrieveFree = run.Relics.Any(id => Content.GetRelicDef(id).CombatRule?.RetrieveFree == true);
            int energyMaxRefBonus = 0;
            foreach (var id in run.Relics) energyMaxRefBonus += Content.GetRelicDef(id).CombatRule?.EnergyMaxRefBonus ?? 0;
            int harvestKeep = 0;
            foreach (var id in run.Relics) harvestKeep += Content.GetRelicDef(id).CombatRule?.HarvestKeep ?? 0;

            var combat = Combat.StartCombatWithOptions(combatSeed, run.Mode, encounterId, new CombatOptions
            {
                Deck = run.Deck,
                LeaderId = run.LeaderId,
                PlayerHp = run.Hp,
                PlayerMaxHp = run.MaxHp,
                // ボスの幕スケール。エリート専用敵は素の値で完成 = 幕内深度スケールも掛けない
                EnemyHpScale =
                    (elite
                        ? 1.0
                        : DepthHpScale(run.Row, run.Act) *
                          (node.Type == MapNodeTypes.Boss ? bossHpByAct[run.Act - 1] : 1.0)) * diff.Hp,
                EnemyStrength =
                    (node.Type == MapNodeTypes.Boss ? bossStrByAct[run.Act - 1] : 0) + (elite ? ELITE_STRENGTH : 0),
                // 幕2/3の通常敵は打点+15%。ボス・エリートは対象外 (難易度倍率は全敵一律)
                EnemyAtkScale = (elite || node.Type == MapNodeTypes.Boss ? 1.0 : atkByAct[run.Act - 1]) * diff.Atk,
                RelicPermanents = relicPerms,
                SetDamageReduction = setDamageReduction,
                RevealIntents = revealIntents,
                RevealOnSet = revealOnSet,
                RetrieveFree = retrieveFree,
                EnergyMaxRefBonus = energyMaxRefBonus,
                HarvestKeep = harvestKeep,
                SetAnyCards = run.SetAnyCards == true ? (bool?)true : null,
            });
            return run with
            {
                Rng = rng,
                Combat = combat,
                Phase = RunPhases.Combat,
                RewardOptions = null,
                CurrentElite = elite,
            };
        }

        /// <summary>選んだノードに入る: 戦闘ノードなら戦闘開始、焚き火なら回復、工房ならそのままフェーズへ</summary>
        private static RunState EnterNode(RunState run)
        {
            var next = EnterNodeInner(run);
            // 本家: 直前の部屋がショップなら ?→ショップ を抑止する
            return next with { LastRoomWasShop = next.Phase == RunPhases.Shop };
        }

        private static RunState EnterNodeInner(RunState run)
        {
            var node = CurrentNode(run);
            if (node == null) throw new InvalidOperationException("ノードにいない");
            switch (node.Type)
            {
                case MapNodeTypes.Battle:
                case MapNodeTypes.Boss:
                    return LaunchCombat(run, false);
                case MapNodeTypes.Elite:
                    return LaunchCombat(run, true);
                case MapNodeTypes.Campfire:
                    // 本家式の排他三択: 休む(30%回復) / 鍛える (除去はショップ専売)
                    return run with
                    {
                        Phase = RunPhases.Campfire,
                        Combat = null,
                        RewardOptions = null,
                        CampfireUpgradesUsed = 0,
                    };
                case MapNodeTypes.Workshop:
                    return run with { Phase = RunPhases.Workshop, Combat = null, RewardOptions = null };
                case MapNodeTypes.Shop:
                    return OpenShop(run);
                case MapNodeTypes.Event:
                    return ResolveUnknown(run);
                case MapNodeTypes.Treasure:
                    // 宝箱行: 本家の「9階は全ノード宝箱」。レリック3択のみ・カード報酬なし
                    return OpenTreasure(run);
            }
            return run;
        }

        private static string RelicRarity(string id) => Content.GetRelicDef(id).Rarity ?? RelicRaritys.Common;

        /// <summary>
        /// レリック抽選 (本家式の層×供給源)。
        /// - chest / elite / event: スロットごとに C/U/R を 50/33/17 でロールし、その層の残候補から1つ (空なら隣の層へ)
        /// - boss: boss 層のみ (尽きたら rare→uncommon→common)
        /// - shop: 先頭1枠は shop 層 (無ければ C/U/R ロール)
        /// </summary>
        public static (IReadOnlyList<string> Options, RngState Rng) DrawRelicOptions(RunState run, string source, int count = 3)
        {
            // actMax: 経済レリックは幕1〜2にしか出ない
            var pool = run.RelicQueue
                .Where(id => !run.Relics.Contains(id) && run.Act <= (Content.GetRelicDef(id).ActMax ?? 99))
                .ToList();
            var rng = run.Rng;
            var picked = new List<string>();
            bool Take(string tier)
            {
                var cands = pool.Where(id => RelicRarity(id) == tier && !picked.Contains(id)).ToList();
                if (cands.Count == 0) return false;
                var (i, next) = Rng.NextInt(rng, 0, cands.Count - 1);
                rng = next;
                picked.Add(cands[i]);
                return true;
            }
            for (int n = 0; n < count; n++)
            {
                if (source == RelicSources.Boss)
                {
                    if (Take(RelicRaritys.Boss) || Take(RelicRaritys.Rare) || Take(RelicRaritys.Uncommon) || Take(RelicRaritys.Common)) continue;
                    break;
                }
                if (source == RelicSources.Shop && n == 0 && Take(RelicRaritys.Shop)) continue;
                var (roll, r1) = Rng.NextInt(rng, 0, 99);
                rng = r1;
                string tier = roll < 50 ? RelicTiers.Common : roll < 83 ? RelicTiers.Uncommon : RelicTiers.Rare;
                string[] order =
                    tier == RelicTiers.Common ? new[] { RelicTiers.Common, RelicTiers.Uncommon, RelicTiers.Rare }
                    : tier == RelicTiers.Uncommon ? new[] { RelicTiers.Uncommon, RelicTiers.Common, RelicTiers.Rare }
                    : new[] { RelicTiers.Rare, RelicTiers.Uncommon, RelicTiers.Common };
                bool any = false;
                foreach (var t in order) if (Take(t)) { any = true; break; }
                if (!any) break;
            }
            return (picked, rng);
        }

        /// <summary>宝箱: レリック3択 (スキップ可)。候補列が尽きていれば素通りで map へ戻る</summary>
        private static RunState OpenTreasure(RunState run)
        {
            var (options, rng) = DrawRelicOptions(run, RelicSources.Chest);
            var baseRun = run with { Rng = rng, Combat = null, RewardOptions = null };
            if (options.Count == 0) return baseRun with { Phase = RunPhases.Map };
            return baseRun with { Phase = RunPhases.RelicReward, RelicOptions = options };
        }

        /// <summary>?→ショップが起きる所持金の下限 (除去の初回価格と同じ)</summary>
        public const int UNKNOWN_SHOP_MIN_GOLD = 50;

        /// <summary>?マスの解決 (本家 getEventRoomOutcomeHelper の整数版)</summary>
        private static RunState ResolveUnknown(RunState run)
        {
            var pity = run.UnknownPity ?? UnknownPityBase();
            // 固定ショップが次の行に見えているので先読みも要る
            var node = CurrentNode(run);
            bool nextHasShop = false;
            if (node != null)
            {
                foreach (var i in node.Next)
                {
                    if (run.Row + 1 < run.Map.Count && i < run.Map[run.Row + 1].Count && run.Map[run.Row + 1][i].Type == MapNodeTypes.Shop)
                    {
                        nextHasShop = true;
                        break;
                    }
                }
            }
            // 所持金が最安帯に届かないなら ?→ショップ は起きない (累積確率は据え置き)
            bool tooPoor = run.Gold < UNKNOWN_SHOP_MIN_GOLD;
            int shopPct = (run.LastRoomWasShop || nextHasShop || tooPoor) ? 0 : pity.Shop;
            var (roll, rng) = Rng.NextInt(run.Rng, 0, 99);
            RunStateUnknownPity Bump(string hit) => new RunStateUnknownPity
            {
                Monster = hit == "monster" ? UNKNOWN_PITY_BASE_MONSTER : pity.Monster + UNKNOWN_PITY_BASE_MONSTER,
                Shop = hit == "shop" ? UNKNOWN_PITY_BASE_SHOP : pity.Shop + UNKNOWN_PITY_BASE_SHOP,
                Treasure = hit == "treasure" ? UNKNOWN_PITY_BASE_TREASURE : pity.Treasure + UNKNOWN_PITY_BASE_TREASURE,
            };
            if (roll < pity.Monster)
            {
                // ?→戦闘: 敵はその行の帯から解決時に抽選する。直前2行の戦闘ノードとメンバーを共有する編成は避ける
                var fullPool = MapGen.TierFor(run.Act, run.Row);
                var recent = new HashSet<string>();
                foreach (var r in new[] { run.Row - 1, run.Row - 2 })
                {
                    if (r < 0 || r >= run.Map.Count) continue;
                    foreach (var n2 in run.Map[r])
                    {
                        if ((n2.Type == MapNodeTypes.Battle || n2.Type == MapNodeTypes.Elite) && !string.IsNullOrEmpty(n2.EncounterId))
                            foreach (var m in Content.ResolveEncounter(n2.EncounterId!)) recent.Add(m.EnemyId);
                    }
                }
                var filtered = fullPool.Where(encId => !Content.ResolveEncounter(encId).Any(m => recent.Contains(m.EnemyId))).ToList();
                IReadOnlyList<string> pool = filtered.Count > 0 ? (IReadOnlyList<string>)filtered : fullPool;
                var (i2, r2) = Rng.NextInt(rng, 0, pool.Count - 1);
                return LaunchCombat(run with { Rng = r2, UnknownPity = Bump("monster"), EventId = null }, false, pool[i2]);
            }
            if (roll < pity.Monster + shopPct)
            {
                return OpenShop(run with { Rng = rng, UnknownPity = Bump("shop"), EventId = null });
            }
            if (roll < pity.Monster + shopPct + pity.Treasure)
            {
                // ?→宝箱: レリック3択のみ (宝箱行と同じ配管。カード報酬は付かない)
                return OpenTreasure(run with { Rng = rng, UnknownPity = Bump("treasure"), EventId = null });
            }
            var (eventId, r3) = PickEvent(run, rng);
            var def = Content.GetEventDef(eventId);
            return run with
            {
                Rng = r3,
                UnknownPity = Bump("event"),
                EventId = eventId,
                SeenEventIds = Append(run.SeenEventIds ?? new List<string>(), eventId),
                SeenShrineIds = def.Kind == "shrine"
                    ? Append(run.SeenShrineIds ?? new List<string>(), eventId)
                    : (run.SeenShrineIds ?? new List<string>()),
                Phase = RunPhases.Event,
                Combat = null,
                RewardOptions = null,
            };
        }

        /// <summary>
        /// イベントの抽選 (本家 generateEvent): 25%で祠+ワンタイムのプール、75%で幕プール。
        /// 幕専用とワンタイムは引いたら二度と出ない / 祠は幕をまたぐと復活する。
        /// </summary>
        private static (string Id, RngState Rng) PickEvent(RunState run, RngState rng0)
        {
            var seen = run.SeenEventIds ?? new List<string>();
            var seenShrine = run.SeenShrineIds ?? new List<string>();
            bool InAct(EventDef e) => e.Act == null || e.Act == run.Act;
            var shrinePool = Content.AllEvents
                .Where(e => InAct(e) && ((e.Kind == "shrine" && !seenShrine.Contains(e.Id)) || (e.Kind == "oneTime" && !seen.Contains(e.Id))))
                .ToList();
            var actPool = Content.AllEvents
                .Where(e => (e.Kind ?? "act") == "act" && InAct(e) && !seen.Contains(e.Id))
                .ToList();
            var (roll, rng) = Rng.NextInt(rng0, 0, 99);
            bool useShrine = roll < SHRINE_CHANCE_PERCENT;
            var primary = useShrine ? shrinePool : actPool;
            var fallback = useShrine ? actPool : shrinePool;
            var pool = primary.Count > 0 ? primary : fallback;
            // 両方尽きた場合のみ既出から引き直す (実質到達しない)
            var final = pool.Count > 0 ? pool : Content.AllEvents.Where(InAct).ToList();
            var (i, r2) = Rng.NextInt(rng, 0, final.Count - 1);
            return (final[i].Id, r2);
        }

        /// <summary>ショップの在庫をシードから決定して開店する (export はテスト用: 会員証の適用範囲)</summary>
        public static RunState OpenShop(RunState run)
        {
            var leader = Content.GetLeaderDef(run.LeaderId);
            bool canRamp = run.Colors.Contains(CardColors.Green);
            int costCap = leader.EnergyMax + (canRamp ? 2 : 0);
            // 保持 (retain) つきの大型は +1 まで
            int CapFor(CardDef c) => c.Retain == true ? leader.EnergyMax + (canRamp ? 1 : 0) : costCap;
            var pool = Content.AllCards
                .Where(c => run.Colors.Contains(c.Color) && !REWARD_EXCLUDED.Contains(c.Id) && c.Cost <= CapFor(c))
                .ToList();
            var rng = run.Rng;
            var cards = new List<ShopStateCards>();
            var remaining = new List<CardDef>(pool);
            while (cards.Count < SHOP_CARD_COUNT && remaining.Count > 0)
            {
                var (idx, r1) = Rng.NextInt(rng, 0, remaining.Count - 1);
                rng = r1;
                var def = remaining[idx];
                remaining.RemoveAt(idx);
                // 価格 = 40 + コスト×10 + ロール0〜10。Xコスト札は典型のX=3として値付けする
                int pricedCost = def.XCost == true ? 3 : def.Cost;
                var (roll, r2) = Rng.NextInt(rng, 0, 10);
                rng = r2;
                cards.Add(new ShopStateCards { Id = def.Id, Price = JsFloor((40 + pricedCost * 10 + roll) * ShopPriceRatio(run)) });
            }
            // レア枠: 品揃えの6枠目はレア確定・高額 (120+コスト×10 ≈ 150G)
            var rarePool = remaining.Where(c => c.Rarity == "rare").ToList();
            if (rarePool.Count > 0)
            {
                var (ri, r3) = Rng.NextInt(rng, 0, rarePool.Count - 1);
                rng = r3;
                var def = rarePool[ri];
                int pricedCost = def.XCost == true ? 3 : def.Cost;
                cards.Add(new ShopStateCards { Id = def.Id, Price = JsFloor((120 + pricedCost * 10) * ShopPriceRatio(run)) }); // 会員証はレア枠にも効く
            }
            // ショップのレリック: shop 層を優先し、無ければ C/U/R
            var (shopRelics, rngS) = DrawRelicOptions(run with { Rng = rng }, RelicSources.Shop, 1);
            rng = rngS;
            string? relicId = shopRelics.Count > 0 ? shopRelics[0] : null;
            var shop = new ShopState
            {
                Cards = cards,
                RelicId = relicId,
                RelicPrice = JsFloor(SHOP_RELIC_PRICE * ShopPriceRatio(run)), // 会員証
            };
            return run with { Rng = rng, Shop = shop, Phase = RunPhases.Shop, Combat = null, RewardOptions = null };
        }

        /// <summary>ランの報酬プール (色アイデンティティ・基本札除外・リーダーのコスト上限)</summary>
        public static IReadOnlyList<CardDef> RewardPool(RunState run)
        {
            var leader = Content.GetLeaderDef(run.LeaderId);
            bool canRamp = run.Colors.Contains(CardColors.Green);
            int costCap = leader.EnergyMax + (canRamp ? 2 : 0);
            int CapFor(CardDef c) => c.Retain == true ? leader.EnergyMax + (canRamp ? 1 : 0) : costCap;
            return Content.AllCards
                .Where(c => run.Colors.Contains(c.Color) && !REWARD_EXCLUDED.Contains(c.Id) && c.Cost <= CapFor(c))
                .ToList();
        }

        /// <summary>この選択肢は cardIndex (デッキの対象カード) を要求するか</summary>
        public static bool EventChoiceNeedsCard(EventChoiceDef choice)
        {
            return choice.RemoveCard == true
                || choice.UpgradeCard == true
                || choice.TransformCard == true
                || choice.DuplicateCard == true;
        }

        private static RunState ApplyEventChoice(RunState run, int choiceIndex, int? cardIndex)
        {
            // ?は入った瞬間に中身が決まる。MapNode でなく RunState が持つ
            string? eventId = run.EventId;
            if (eventId == null) throw new InvalidOperationException("イベントノードではない");
            var def = Content.GetEventDef(eventId);
            EventChoiceDef? choice = (choiceIndex >= 0 && choiceIndex < def.Choices.Count) ? def.Choices[choiceIndex] : null;
            if (choice == null) throw new InvalidOperationException($"不正な選択肢: {choiceIndex}");
            if (choice.RequireGold != null && run.Gold < choice.RequireGold.Value)
            {
                throw new InvalidOperationException($"ゴールドが足りない (必要{choice.RequireGold.Value}G)");
            }
            RunState next = run;
            var rng = run.Rng;
            // TS の applyOutcome (choice / gamble の win・lose の両方から呼ばれる)。
            // JS の `if (o.gold)` は 0 と undefined を falsy として弾くので、!= 0 で判定する
            void ApplyOutcome(int? gold, int? hp, double? hpRatio, int? wounds, int? brands, int? timedCurses)
            {
                if (gold.HasValue && gold.Value != 0) next = next with { Gold = Math.Max(0, next.Gold + gold.Value) };
                if (hp.HasValue && hp.Value != 0) next = next with { Hp = Math.Min(next.MaxHp, next.Hp + hp.Value) };
                // 最大HP比の増減 (リーダー間で最大HPが違うので比率で持つ)。切り捨て
                if (hpRatio.HasValue && hpRatio.Value != 0)
                {
                    next = next with { Hp = Math.Min(next.MaxHp, next.Hp + JsTrunc(next.MaxHp * hpRatio.Value)) };
                }
                if (wounds.HasValue && wounds.Value != 0)
                {
                    var list = new List<CardInstance>();
                    for (int i = 0; i < wounds.Value; i++)
                        list.Add(new CardInstance { Uid = $"wound_a{run.Act}_r{run.Row}_{i}", Def = Content.WOUND_DEF });
                    next = next with { Deck = Concat(next.Deck, list) };
                }
                if (brands.HasValue && brands.Value != 0)
                {
                    // 呪いの烙印: 恒久のデッキ汚染と引き換えの大報酬
                    var list = new List<CardInstance>();
                    for (int i = 0; i < brands.Value; i++)
                        list.Add(new CardInstance { Uid = $"brand_a{run.Act}_r{run.Row}_{i}", Def = Content.BRAND_DEF });
                    next = next with { Deck = Concat(next.Deck, list) };
                }
                if (timedCurses.HasValue && timedCurses.Value != 0)
                {
                    // 仮初の烙印: 烙印と同じ滞留HP-1だが5戦で自然消滅する中間対価
                    var list = new List<CardInstance>();
                    for (int i = 0; i < timedCurses.Value; i++)
                        list.Add(new CardInstance { Uid = $"guilt_a{run.Act}_r{run.Row}_{i}", Def = Content.GUILT_DEF, ExpiresAfterBattles = 5 });
                    next = next with { Deck = Concat(next.Deck, list) };
                }
            }
            ApplyOutcome(choice.Gold, choice.Hp, choice.HpRatio, choice.Wounds, choice.Brands, choice.TimedCurses);
            if (choice.MaxHp.HasValue && choice.MaxHp.Value != 0)
            {
                next = next with { MaxHp = next.MaxHp + choice.MaxHp.Value, Hp = next.Hp + choice.MaxHp.Value };
            }
            if (choice.AddRandomCards.HasValue && choice.AddRandomCards.Value != 0)
            {
                var pool = RewardPool(run);
                for (int i = 0; i < choice.AddRandomCards.Value && pool.Count > 0; i++)
                {
                    var (idx, r1) = Rng.NextInt(rng, 0, pool.Count - 1);
                    rng = r1;
                    next = next with
                    {
                        Deck = Append(next.Deck, new CardInstance { Uid = $"event_a{run.Act}_r{run.Row}_{i}_{pool[idx].Id}", Def = pool[idx] }),
                    };
                }
            }
            if (choice.Relic == true)
            {
                // イベントのレリックは C/U/R から抽選 (boss/shop 層は出ない)
                var (drawn, rE) = DrawRelicOptions(next with { Rng = rng }, RelicSources.Event, 1);
                rng = rE;
                if (drawn.Count > 0)
                {
                    var relicId = drawn[0];
                    next = WithRelicGainBrands(ApplyRelicBonus(next with { Relics = Append(next.Relics, relicId) }, relicId), run);
                }
            }
            if (choice.RemoveCard == true)
            {
                var card = (cardIndex.HasValue && cardIndex.Value >= 0 && cardIndex.Value < next.Deck.Count) ? next.Deck[cardIndex.Value] : null;
                if (card == null) throw new InvalidOperationException("対象カードを cardIndex で指定する");
                if (next.Deck.Count <= 5) throw new InvalidOperationException("これ以上デッキを減らせない");
                next = next with { Deck = next.Deck.Where((_, i) => i != cardIndex!.Value).ToList() };
            }
            if (choice.UpgradeCard == true)
            {
                var card = (cardIndex.HasValue && cardIndex.Value >= 0 && cardIndex.Value < next.Deck.Count) ? next.Deck[cardIndex.Value] : null;
                if (card == null) throw new InvalidOperationException("対象カードを cardIndex で指定する");
                if (Upgrade.IsUpgraded(card)) throw new InvalidOperationException("すでに鍛えられている");
                if (Upgrade.UpgradeTier(card.Def) == Upgrade.UpgradeTiers.None) throw new InvalidOperationException($"{card.Def.Name} は鍛えられない");
                next = next with { Deck = next.Deck.Select((c, i) => i == cardIndex!.Value ? Upgrade.UpgradeCard(c) : c).ToList() };
            }
            if (choice.TransformCard == true)
            {
                // 変成 (本家 Transmogrifier): 1枚を除去し、同じレアリティの別カードへ置き換える
                var card = (cardIndex.HasValue && cardIndex.Value >= 0 && cardIndex.Value < next.Deck.Count) ? next.Deck[cardIndex.Value] : null;
                if (card == null) throw new InvalidOperationException("対象カードを cardIndex で指定する");
                string rarity = card.Def.Rarity ?? "common";
                var pool = RewardPool(run).Where(c => (c.Rarity ?? "common") == rarity && c.Id != card.Def.Id).ToList();
                if (pool.Count > 0)
                {
                    var (idx, r1) = Rng.NextInt(rng, 0, pool.Count - 1);
                    rng = r1;
                    var replacement = new CardInstance { Uid = $"trans_a{run.Act}_r{run.Row}_{pool[idx].Id}", Def = pool[idx] };
                    next = next with { Deck = next.Deck.Select((c, i) => i == cardIndex!.Value ? replacement : c).ToList() };
                }
            }
            if (choice.DuplicateCard == true)
            {
                // 複製 (本家 Duplicator): 同じ def が1枚増える。uid は一意にする
                var card = (cardIndex.HasValue && cardIndex.Value >= 0 && cardIndex.Value < next.Deck.Count) ? next.Deck[cardIndex.Value] : null;
                if (card == null) throw new InvalidOperationException("対象カードを cardIndex で指定する");
                var copy = new CardInstance { Uid = $"dup_a{run.Act}_r{run.Row}_{card.Uid}", Def = card.Def };
                next = next with { Deck = Append(next.Deck, copy) };
            }
            if (choice.UpgradeRandomCards.HasValue && choice.UpgradeRandomCards.Value != 0)
            {
                // ランダム強化 (本家 Shining Light): 強化可能な札からN枚。足りなければそこで打ち切る
                for (int i = 0; i < choice.UpgradeRandomCards.Value; i++)
                {
                    var idxs = new List<int>();
                    for (int j = 0; j < next.Deck.Count; j++)
                    {
                        var c = next.Deck[j];
                        if (!Upgrade.IsUpgraded(c) && Upgrade.UpgradeTier(c.Def) != Upgrade.UpgradeTiers.None) idxs.Add(j);
                    }
                    if (idxs.Count == 0) break;
                    var (pick, r1) = Rng.NextInt(rng, 0, idxs.Count - 1);
                    rng = r1;
                    int target = idxs[pick];
                    next = next with { Deck = next.Deck.Select((c, j) => j == target ? Upgrade.UpgradeCard(c) : c).ToList() };
                }
            }
            if (choice.RemoveAllWounds == true)
            {
                // 負傷の一掃 (本家 The Divine Fountain)。0枚でも何も起きないだけで throw しない
                next = next with { Deck = next.Deck.Where(c => c.Def.Id != Content.WOUND_DEF.Id).ToList() };
            }
            if (choice.Gamble != null)
            {
                // ロールはラン RNG = 決定的 (リプレイ再現)
                var (roll, r1) = Rng.NextInt(rng, 0, 999);
                rng = r1;
                var o = roll < choice.Gamble.Chance * 1000 ? choice.Gamble.Win : choice.Gamble.Lose;
                ApplyOutcome(o.Gold, o.Hp, null, o.Wounds, null, null);
            }
            if (next.Hp <= 0) return next with { Rng = rng, Hp = 0, Phase = RunPhases.Lost };
            return next with { Rng = rng, Phase = RunPhases.Map };
        }

        /// <summary>伏せ参照レリック (このランの報酬プールにリアクションが1枚も無い色では候補列から除く)</summary>
        private static readonly HashSet<string> SET_RELICS = new HashSet<string> { "relic_talisman_pouch", "relic_quiet_bell" };

        public static RunState CreateRun(
            int seed,
            string mode,
            string leaderId = "leader_green",
            string? deckId = null,
            int difficulty = DEFAULT_DIFFICULTY,
            RunOptions? opts = null)
        {
            var leader = Content.GetLeaderDef(leaderId);
            // 種の選択制: リーダーが許可する初期デッキのみ受け付ける
            IReadOnlyList<string> deckChoices = leader.RunDeckChoices ?? new List<string> { leader.RunDeckId };
            string chosenDeck = deckId ?? leader.RunDeckId;
            if (!deckChoices.Contains(chosenDeck))
            {
                throw new InvalidOperationException($"このリーダーでは選べない初期デッキ: {chosenDeck}");
            }
            var rng0 = Rng.Create(seed);
            // マップもレリック候補列もシードから確定 (リプレイ再現性)
            var (map, rngAfterMap) = MapGen.GenerateMap(rng0, 1, true);
            bool canSet = Content.AllCards.Any(c => leader.Colors.Contains(c.Color) && c.Type == CardTypes.Reaction);
            var relicIds = Content.AllRelics.Select(r => r.Id).Where(id => canSet || !SET_RELICS.Contains(id)).ToList();
            var (relicQueue, rngAfterRelics) = Rng.Shuffle(rngAfterMap, relicIds);
            return new RunState
            {
                Seed = seed,
                Mode = mode,
                LeaderId = leaderId,
                DebugRevealIntents = opts?.RevealIntents == true ? (bool?)true : null,
                SetAnyCards = opts?.SetAnyCards == true ? (bool?)true : null,
                Colors = leader.Colors,
                // 範囲外は表の端へ丸めて保存 (以降の読み取りも DifficultyScale が守る)
                Difficulty = Math.Min(DIFFICULTY_TABLE.Count, Math.Max(1, difficulty)),
                Rng = rngAfterRelics,
                Deck = Content.BuildDeck(chosenDeck),
                Hp = leader.MaxHp,
                MaxHp = leader.MaxHp,
                Act = 1,
                Map = map,
                Row = -1,
                Col = 0,
                BattlesWon = 0,
                Gold = STARTING_GOLD,
                RemovalCount = 0,
                UpgradeCount = 0,
                Shop = null,
                Phase = RunPhases.Map,
                Combat = null,
                RewardOptions = null,
                Picks = new List<string>(),
                Relics = new List<string>(),
                RelicQueue = relicQueue,
                RelicOptions = null,
                CurrentElite = false,
                VictoryHealBonus = 0,
                RewardChoicesBonus = 0,
                CampfireRatio = CAMPFIRE_HEAL_RATIO,
                GoldPerVictoryBonus = 0,
                CampfireForgeBonus = 0,
                CampfireUpgradesUsed = 0,
                UnknownPity = UnknownPityBase(),
                LastRoomWasShop = false,
                EventId = null,
                SeenEventIds = new List<string>(),
                SeenShrineIds = new List<string>(),
            };
        }

        /// <summary>origin からラン初期状態を再現する</summary>
        public static RunState ReplayInitialRun(ReplayOrigin origin)
        {
            if (origin.Kind == "checkpoint" && origin.Checkpoint != null)
            {
                return CreateDebugCheckpointRun(origin.Seed, ReactionModes.SetConfirm, origin.LeaderId, origin.Checkpoint);
            }
            return CreateRun(origin.Seed, ReactionModes.SetConfirm, origin.LeaderId, origin.DeckId,
                origin.Difficulty ?? DEFAULT_DIFFICULTY,
                new RunOptions
                {
                    RevealIntents = origin.RevealIntents == true ? (bool?)true : null,
                    SetAnyCards = origin.SetAnyCards == true ? (bool?)true : null,
                });
        }

        /// <summary>
        /// ジャーナル全体を再実行し、各コマンド後の状態列を返す (States[0]=初期状態、States[i]=iコマンド後)。
        /// データ定義が変わって再現が分岐した場合はそこで打ち切り、Error に理由を入れる
        /// </summary>
        public static (List<RunState> States, string? Error) ReplayStates(RunJournal journal)
        {
            var states = new List<RunState> { ReplayInitialRun(journal.Origin) };
            for (int i = 0; i < journal.Commands.Count; i++)
            {
                try
                {
                    states.Add(ApplyRunCommand(states[states.Count - 1], journal.Commands[i]));
                }
                catch (Exception e)
                {
                    return (states, $"コマンド{i + 1}/{journal.Commands.Count}で再現が分岐 (データ変更の可能性): {e.Message}");
                }
            }
            return (states, null);
        }

        /// <summary>
        /// チェックポイント開始 (幕2/幕3から代表デッキで開始)。
        /// 通常の CreateRun を土台に、幕・マップ・デッキ・レリック・HP・金だけ差し替える純関数
        /// </summary>
        public static RunState CreateDebugCheckpointRun(int seed, string mode, string leaderId, ReplayOriginCheckpoint opts)
        {
            var baseRun = CreateRun(seed, mode, leaderId, null, opts.Difficulty ?? DEFAULT_DIFFICULTY);
            int act = Math.Min(MapGen.ACT_COUNT, Math.Max(1, opts.Act));
            var (map, rng) = MapGen.GenerateMap(baseRun.Rng, act, true);
            RunState run = baseRun with
            {
                Act = act,
                Map = map,
                Rng = rng,
                Row = -1,
                Col = 0,
                Deck = Content.BuildDeck(opts.DeckId), // デッキ選択制の検証は通さない (デバッグ = 理想形も可)
                BattlesWon = act == 3 ? 19 : act == 2 ? 10 : 0,
                Gold = opts.Gold ?? 150,
            };
            foreach (var id in opts.RelicIds ?? new List<string>())
            {
                Content.GetRelicDef(id); // 未定義なら throw
                run = ApplyRelicBonus(
                    run with { Relics = Append(run.Relics, id), RelicQueue = run.RelicQueue.Where(q => q != id).ToList() },
                    id);
            }
            double ratio = Math.Min(1, Math.Max(0.05, opts.HpRatio ?? 1));
            return run with { Hp = Math.Max(1, JsRound(run.MaxHp * ratio)) };
        }

        /// <summary>効果名 → アーキタイプの軸 (確定済みルール表「軸の重み付け」)</summary>
        private static readonly Dictionary<string, string> EFFECT_AXIS = new Dictionary<string, string>
        {
            { "addGrowth", "growth" }, { "doubleGrowth", "growth" }, { "dischargeGrowth", "growth" }, { "dischargeGrowthBlock", "growth" },
            { "gainEnergyMax", "ramp" }, { "dealDamagePerEnergyMax", "ramp" }, { "gainBlockPerEnergyMax", "ramp" },
            { "addMomentum", "trample" }, { "dealDamagePerMomentum", "trample" }, { "doubleMomentum", "trample" },
            { "dischargeMomentumBurn", "burn" }, { "dischargeMomentumBlock", "trample" },
            { "applyBurn", "burn" }, { "dischargeBurn", "burn" },
            { "addAether", "aether" }, { "dischargeAether", "aether" }, { "dischargeAetherDraw", "aether" },
            { "gainIceBlock", "ice" }, { "dealDamagePerIceBlock", "ice" }, { "gainIceBlockPerCardPlayed", "ice" },
            { "negate", "permission" }, { "negateConvertIce", "permission" },
            { "summonPermanent", "retinue" }, { "dealDamagePerPermanent", "retinue" }, { "gainBlockPerPermanent", "retinue" },
            { "exhaustFromDeck", "graveyard" }, { "exhaustFromDeckChoose", "graveyard" }, { "dealDamagePerExhaust", "graveyard" }, { "retrieveFromExhaust", "graveyard" },
            { "playFromExhaust", "graveyard" }, { "gainBlockPerExhaust", "graveyard" }, { "dealDamageDrainPerExhaust", "graveyard" },
            { "loseHp", "selfharm" }, { "dealDamagePerSelfHpLost", "selfharm" },
            { "dealDamagePerCardPlayed", "storm" }, { "drawCardsPerCardPlayed", "storm" }, { "addCasts", "storm" },
            { "impulseDraw", "impulse" },
            { "weakenEnemy", "oppress" }, { "dealDamagePerNegStrength", "oppress" },
            { "gainHp", "heal" }, { "dealDamageDrain", "heal" },
            { "dealDamagePerDamageTaken", "wrath" }, { "applyBurnPerDamageTaken", "wrath" },
            { "dealDamagePerBlock", "fortress" },
            { "shatterBlock", "shatter" }, { "shatterBlockConvert", "shatter" },
            { "dealDamageRandom", "chaos" }, { "dealDamagePerRandomPlayed", "chaos" },
            { "dealDamageExecute", "execute" }, { "exposeEnemy", "execute" },
            { "confuse", "confuse" },
            { "dealDamagePerHandCard", "grimoire" }, { "gainIceBlockPerHandCard", "grimoire" }, // 抱え込み (青)
            { "addSpellEcho", "echo" }, // 反復 (青の呪文コピー)
        };

        /// <summary>誘発トリガー → 軸。置物の「接着剤」札はここでほぼ自動的に分類される</summary>
        private static readonly Dictionary<string, string> TRIGGER_AXIS = new Dictionary<string, string>
        {
            { "onPermanentEntered", "retinue" },
            { "onCardExhausted", "graveyard" },
            { "onCostExhausted", "graveyard" },
            { "onHealed", "heal" },
            { "onHpLost", "selfharm" },
            { "onAetherGained", "aether" },
            { "onImpulsePlayed", "impulse" },
            { "onRandomPlayed", "chaos" },
            { "onSpellPlayed", "storm" },
            { "onBlockGained", "fortress" },
            { "onActionNegated", "permission" },
            { "onSelfExhausted", "graveyard" }, // 亡骸効果 (黒)
        };

        /// <summary>この札が属する軸 (効果名・トリガー・フィールドからの自動導出 + JSONの明示宣言)。TS の Set と同じ挿入順を保つ</summary>
        public static IReadOnlyList<string> AxesOf(CardDef def)
        {
            var all = new List<DeclarativeEffect>(def.Effects);
            if (def.Modes != null) foreach (var m in def.Modes) all.AddRange(m.Effects);
            var order = new List<string>();
            var seen = new HashSet<string>();
            void Add(string s) { if (seen.Add(s)) order.Add(s); }
            if (def.Axis != null) foreach (var a in def.Axis) Add(a);
            foreach (var e in all)
            {
                if (EFFECT_AXIS.TryGetValue(e.Effect, out var byEffect) && !string.IsNullOrEmpty(byEffect)) Add(byEffect);
                if (TRIGGER_AXIS.TryGetValue(e.Trigger, out var byTrigger) && !string.IsNullOrEmpty(byTrigger)) Add(byTrigger);
                if (e.ExhaustThreshold != null) Add("graveyard"); // 忘却の刻の参照札
                if (e.Pierce == true) Add("trample");             // 貫通はトランプルの核
            }
            if (def.ExhaustCost != null) Add("graveyard"); // 消滅コストは墓地の燃料
            if (def.Retainer == true) Add("retinue");
            return order;
        }

        /// <summary>報酬を抽選 (リーダーの色アイデンティティのカードのみ・基本札除外・重複なし)</summary>
        private static RunState RollRewards(RunState run)
        {
            var leader = Content.GetLeaderDef(run.LeaderId);
            // 報酬プールはリーダーのエナジー上限を考慮する (緑はランプで伸ばせるので +2 まで許容)
            bool canRamp = run.Colors.Contains(CardColors.Green);
            int costCap = leader.EnergyMax + (canRamp ? 2 : 0);
            int CapFor(CardDef c) => c.Retain == true ? leader.EnergyMax + (canRamp ? 1 : 0) : costCap;
            var pool = Content.AllCards
                .Where(c => run.Colors.Contains(c.Color) && !REWARD_EXCLUDED.Contains(c.Id) && c.Cost <= CapFor(c))
                .ToList();
            // レアリティ抽選: スロットごとにコモン60%/アンコモン37%/レア3%の本家比率
            var remaining = new List<CardDef>(pool);
            var picked = new List<string>();
            var rng = run.Rng;
            int want = Math.Max(1, leader.RewardChoices + run.RewardChoicesBonus); // 王冠の欠片 (提示-1) でも最低1枚
            string RarityOf(CardDef c) => c.Rarity ?? "common";
            while (picked.Count < want && remaining.Count > 0)
            {
                var (roll, r1) = Rng.NextInt(rng, 0, 99);
                rng = r1;
                // エリート報酬はレア1枚確定 (先頭スロット)。逃がしたエリートはレア確定を失う
                bool eliteEscaped = run.Combat != null && run.Combat.Enemies.Any(e => e.Fled == true);
                string[] wanted =
                    run.CurrentElite && picked.Count == 0 && !eliteEscaped
                        ? new[] { "rare", "uncommon", "common" }
                        : roll < 3
                            ? new[] { "rare", "uncommon", "common" }
                            : roll < 40
                                ? new[] { "uncommon", "common" }
                                : new[] { "common" };
                // 希望レアリティの札が尽きていたら下の帯へフォールバック。それも無ければプール全体
                List<CardDef> candidates = new List<CardDef>();
                foreach (var r in wanted)
                {
                    candidates = remaining.Where(c => RarityOf(c) == r).ToList();
                    if (candidates.Count > 0) break;
                }
                if (candidates.Count == 0) candidates = remaining;
                var (idx, r2) = Rng.NextInt(rng, 0, candidates.Count - 1);
                rng = r2;
                var chosen = candidates[idx];
                picked.Add(chosen.Id);
                remaining.RemoveAt(remaining.FindIndex(x => ReferenceEquals(x, chosen))); // TS の indexOf は参照比較
            }
            return run with { Rng = rng, RewardOptions = picked, Phase = RunPhases.Reward };
        }

        /// <summary>戦闘勝利後の処理: HP持ち越し → (エリートならレリック報酬 →) カード報酬 or ラン勝利</summary>
        private static RunState AfterVictory(RunState run, GameState combat)
        {
            bool isBoss = CurrentNode(run)?.Type == MapNodeTypes.Boss;
            // 3幕目のボス撃破 = ラン走破
            if (isBoss && run.Act >= MapGen.ACT_COUNT)
            {
                // 走破画面のHPは戦闘終了時の値
                return run with { Combat = combat, Hp = combat.Player.Hp, BattlesWon = run.BattlesWon + 1, Phase = RunPhases.Won };
            }
            // 自動回復は狩人の恵み (victoryHealBonus) のみ。幕ボス撃破は全回復
            int rescueHeal = combat.Player.Hp <= run.MaxHp * 0.3 ? run.VictoryHealBonus : 0;
            int hp = isBoss
                ? run.MaxHp
                : Math.Min(run.MaxHp, combat.Player.Hp + VICTORY_HEAL + rescueHeal + RelicBonusSum(run, "victoryHealFlat")); // 薬草袋
            // ゴールド獲得 (通常12〜18G・エリート+30〜40G・幕ボス+40〜50G)
            var rng = run.Rng;
            var (baseGold, r1) = Rng.NextInt(rng, GOLD_PER_BATTLE_MIN, GOLD_PER_BATTLE_MAX);
            rng = r1;
            int gained = baseGold;
            if (run.CurrentElite)
            {
                var (bonus, r2) = Rng.NextInt(rng, GOLD_ELITE_BONUS_MIN, GOLD_ELITE_BONUS_MAX);
                rng = r2;
                gained += bonus;
            }
            if (isBoss)
            {
                var (bonus, r3) = Rng.NextInt(rng, 40, 50);
                rng = r3;
                gained += bonus;
            }
            // 商人の秤 (B型レリック): 戦闘勝利のゴールド加算
            gained += run.GoldPerVictoryBonus;
            // 戦利品袋: エリート勝利+N / 金の靴: 倍率 (盗みの精算より前)
            if (run.CurrentElite) gained += RelicBonusSum(run, "eliteGoldBonus");
            gained = JsFloor(gained * GoldMultiplier(run));
            // 盗みの精算: 逃走した盗人が抱えた額を失い、逃げる前に倒した盗人は全額戻る + 懸賞金
            int fledLoss = 0;
            foreach (var e in combat.Enemies) if (e.Fled == true) fledLoss += e.StolenGold ?? 0;
            int bounty = combat.Enemies.Count(e => e.Fled != true && (e.StolenGold ?? 0) > 0) * THIEF_BOUNTY;
            gained += bounty - fledLoss;
            // 時限呪い: 勝利ごとに残り戦数-1・0でデッキから自然消滅
            var deckAfterCurses = run.Deck
                .Select(c => c.ExpiresAfterBattles != null ? c with { ExpiresAfterBattles = c.ExpiresAfterBattles.Value - 1 } : c)
                .Where(c => c.ExpiresAfterBattles == null || c.ExpiresAfterBattles.Value > 0)
                .ToList();
            var next = run with
            {
                Rng = rng,
                Combat = combat,
                Hp = hp,
                Deck = deckAfterCurses,
                BattlesWon = run.BattlesWon + 1,
                // 盗みの喪失で負になりうるので0でクランプ
                Gold = Math.Max(0, run.Gold + gained),
            };
            // 幕ボス・エリート戦の勝利: レリック3択 (幕ボスは本家のボスレリック相当)
            if (run.CurrentElite || isBoss)
            {
                var (options, rng2) = DrawRelicOptions(next, isBoss ? RelicSources.Boss : RelicSources.Elite);
                if (options.Count > 0)
                {
                    return next with { Rng = rng2, Phase = RunPhases.RelicReward, RelicOptions = options };
                }
            }
            return RollRewards(next);
        }

        /// <summary>幕ボスのカード報酬を受け取った後、次の幕へ進む (新しいマップを生成して行0の選択から)</summary>
        private static RunState AdvanceActIfBossCleared(RunState run)
        {
            if (CurrentNode(run)?.Type != MapNodeTypes.Boss || run.Act >= MapGen.ACT_COUNT)
            {
                return run with { Phase = RunPhases.Map };
            }
            int nextAct = run.Act + 1;
            var (map, rng) = MapGen.GenerateMap(run.Rng, nextAct, true);
            return run with
            {
                Rng = rng,
                Act = nextAct,
                Map = map,
                Row = -1,
                Col = 0,
                Combat = null,
                Phase = RunPhases.Map,
                UnknownPity = UnknownPityBase(), // ピティは幕をまたがない
                SeenShrineIds = new List<string>(), // 祠は幕をまたぐと復活する
                EventId = null,
                LastRoomWasShop = false,
            };
        }

        /// <summary>
        /// 呪いの鍵: レリックを取るたび、取得前に持っていた鍵の数だけ烙印を受け取る。
        /// before = 取得前の RunState (鍵自身を取った瞬間は数えない)
        /// </summary>
        private static RunState WithRelicGainBrands(RunState next, RunState before)
        {
            int brands = 0;
            foreach (var id in before.Relics) brands += Content.GetRelicDef(id).Bonus?.BrandOnRelic ?? 0;
            if (brands <= 0) return next;
            var add = new List<CardInstance>();
            for (int i = 0; i < brands; i++)
                add.Add(new CardInstance { Uid = $"brand_key_a{next.Act}_r{next.Row}_{next.Relics.Count}_{i}", Def = Content.BRAND_DEF });
            return next with { Deck = Concat(next.Deck, add) };
        }

        /// <summary>B型レリックの取得時効果を適用する</summary>
        private static RunState ApplyRelicBonus(RunState run, string relicId)
        {
            var def = Content.GetRelicDef(relicId);
            var b = def.Bonus;
            if (b == null) return run;
            return run with
            {
                MaxHp = run.MaxHp + (b.MaxHp ?? 0),
                Hp = Math.Min(run.MaxHp + (b.MaxHp ?? 0), run.Hp + (b.MaxHp ?? 0)),
                VictoryHealBonus = run.VictoryHealBonus + (b.VictoryHeal ?? 0),
                RewardChoicesBonus = run.RewardChoicesBonus + (b.RewardChoices ?? 0),
                CampfireRatio = b.CampfireRatio ?? run.CampfireRatio,
                GoldPerVictoryBonus = run.GoldPerVictoryBonus + (b.GoldPerVictory ?? 0),
                CampfireForgeBonus = run.CampfireForgeBonus + (b.CampfireForge ?? 0),
            };
        }

        /// <summary>
        /// この焚き火で鍛えられる枚数 (1 + 砥石の追加回数)。砥石の追加回数は1幕に1回だけ有効。
        /// UI/CLI/engine が同じ式を読む (表示の嘘を作らない)
        /// </summary>
        public static int CampfireForgeAllowed(RunState run)
        {
            int bonus = run.CampfireForgeBonus;
            bool usable = bonus > 0 && run.ForgeBonusUsedAct != run.Act;
            return 1 + (usable ? bonus : 0);
        }

        public static RunState ApplyRunCommand(RunState run, RunCommand command)
        {
            switch (command)
            {
                case RunCommand_StartRun c:
                    return CreateRun(c.Seed, run.Mode, run.LeaderId);

                case RunCommand_Combat c:
                {
                    if (run.Phase != RunPhases.Combat || run.Combat == null)
                        throw new InvalidOperationException("戦闘中ではない（直前の戦闘は決着済み＝残りの手札は打てない。次はランのコマンド）");
                    if (c.Command.Type == Command_StartCombat.TypeTag)
                        throw new InvalidOperationException("ラン中の戦闘開始はランが管理する");
                    var combat = State.ApplyCommand(run.Combat, c.Command);
                    if (combat.Phase == CombatPhases.Lost) return run with { Combat = combat, Hp = 0, Phase = RunPhases.Lost };
                    if (combat.Phase == CombatPhases.Won) return AfterVictory(run, combat);
                    return run with { Combat = combat };
                }

                case RunCommand_PickReward c:
                {
                    if (run.Phase != RunPhases.Reward || run.RewardOptions == null) throw new InvalidOperationException("報酬フェーズではない");
                    string? cardId = (c.Index >= 0 && c.Index < run.RewardOptions.Count) ? run.RewardOptions[c.Index] : null;
                    if (cardId == null) throw new InvalidOperationException($"不正な報酬指定: {c.Index}");
                    // uid は行番号で一意化 (1行につき1ノードしか訪れないため衝突しない)
                    var card = new CardInstance { Uid = $"pick_a{run.Act}_r{run.Row}_{cardId}", Def = Content.GetCardDef(cardId) };
                    return AdvanceActIfBossCleared(run with
                    {
                        Deck = Append(run.Deck, card),
                        Picks = Append(run.Picks, cardId),
                        RewardOptions = null,
                    });
                }

                case RunCommand_SkipReward:
                {
                    if (run.Phase != RunPhases.Reward) throw new InvalidOperationException("報酬フェーズではない");
                    return AdvanceActIfBossCleared(run with { RewardOptions = null });
                }

                case RunCommand_ChooseNode c:
                {
                    if (run.Phase != RunPhases.Map) throw new InvalidOperationException("マップフェーズではない");
                    var candidates = NextChoices(run);
                    if (!candidates.Contains(c.Col)) throw new InvalidOperationException($"進めないノード: {c.Col}");
                    return EnterNode(run with { Row = run.Row + 1, Col = c.Col });
                }

                case RunCommand_PickRelic c:
                {
                    if (run.Phase != RunPhases.RelicReward || run.RelicOptions == null)
                        throw new InvalidOperationException("レリック報酬フェーズではない");
                    string? relicId = (c.Index >= 0 && c.Index < run.RelicOptions.Count) ? run.RelicOptions[c.Index] : null;
                    if (relicId == null) throw new InvalidOperationException($"不正なレリック指定: {c.Index}");
                    RunState next = run with { Relics = Append(run.Relics, relicId), RelicOptions = null };
                    next = ApplyRelicBonus(next, relicId);
                    // ?マスの宝箱はレリックのみでカード報酬は付かない
                    // combat===null が「戦闘勝利を経ていない=宝箱」の判別 (AfterVictory は必ず combat を渡す)
                    next = WithRelicGainBrands(next, run);
                    if (run.Combat == null) return next with { RelicOptions = null, Phase = RunPhases.Map };
                    return RollRewards(next);
                }

                case RunCommand_SkipRelic:
                {
                    if (run.Phase != RunPhases.RelicReward) throw new InvalidOperationException("レリック報酬フェーズではない");
                    if (run.Combat == null) return run with { RelicOptions = null, Phase = RunPhases.Map };
                    return RollRewards(run with { RelicOptions = null });
                }

                case RunCommand_CampfireRest:
                {
                    // 休む = 最大HPの30% (campfireRatio) を回復して次へ。鍛えるとは排他
                    if (run.Phase != RunPhases.Campfire) throw new InvalidOperationException("焚き火フェーズではない");
                    // 休めないレリック (古根の杯=本家 Coffee Dripper): 休むは回復なしの立ち去り
                    bool noRest = run.Relics.Any(id => Content.GetRelicDef(id).Bonus?.NoRest == true);
                    bool rested = run.CampfireUpgradesUsed == 0 && !noRest;
                    // 薬研: 実際に休んだ時だけ最大HP+N (現在HPも+N)
                    int grow = rested ? RelicBonusSum(run, "restMaxHp") : 0;
                    int maxHp = run.MaxHp + grow;
                    int hp = rested ? Math.Min(maxHp, run.Hp + grow + JsFloor(run.MaxHp * run.CampfireRatio)) : run.Hp;
                    return run with { Hp = hp, MaxHp = maxHp, Phase = RunPhases.Map };
                }

                case RunCommand_CampfireUpgrade c:
                {
                    if (run.Phase != RunPhases.Campfire) throw new InvalidOperationException("焚き火フェーズではない");
                    var card = (c.Index >= 0 && c.Index < run.Deck.Count) ? run.Deck[c.Index] : null;
                    if (card == null) throw new InvalidOperationException($"不正な強化指定: {c.Index}");
                    if (Upgrade.IsUpgraded(card)) throw new InvalidOperationException("すでに鍛えられている");
                    // 強化不可札 (上限ランプ) を受理して「+」だけ付ける事故の再発防止
                    if (Upgrade.UpgradeTier(card.Def) == Upgrade.UpgradeTiers.None)
                    {
                        throw new InvalidOperationException($"{card.Def.Name} は鍛えられない (エナジー上限を上げる札は強化対象外)");
                    }
                    // 鍛冶の砥石: 追加回数のぶん焚き火に留まり、もう1枚鍛えられる (1幕に1回だけ)
                    int used = run.CampfireUpgradesUsed + 1;
                    int allowed = CampfireForgeAllowed(run);
                    return run with
                    {
                        Deck = run.Deck.Select((x, i) => i == c.Index ? Upgrade.UpgradeCard(x) : x).ToList(),
                        CampfireUpgradesUsed = used,
                        ForgeBonusUsedAct = used > 1 ? run.Act : run.ForgeBonusUsedAct,
                        Phase = used < allowed ? RunPhases.Campfire : RunPhases.Map,
                    };
                }

                case RunCommand_WorkshopFuse c:
                {
                    if (run.Phase != RunPhases.Workshop) throw new InvalidOperationException("工房フェーズではない");
                    var a = (c.IndexA >= 0 && c.IndexA < run.Deck.Count) ? run.Deck[c.IndexA] : null;
                    var b = (c.IndexB >= 0 && c.IndexB < run.Deck.Count) ? run.Deck[c.IndexB] : null;
                    if (a == null || b == null) throw new InvalidOperationException("不正な合成指定");
                    var reason = Fusion.FuseBlockReason(a, b);
                    if (reason != null) throw new InvalidOperationException($"合成できない: {reason}");
                    int price = WorkshopFusePrice(run);
                    if (run.Gold < price) throw new InvalidOperationException($"ゴールドが足りない (合成{price}G・所持{run.Gold}G)");
                    var fusedDef = Fusion.FuseCards(a, b);
                    var fused = new CardInstance { Uid = $"fused_a{run.Act}_r{run.Row}_{fusedDef.Id}", Def = fusedDef };
                    // 素材2枚はデッキから消え、合成札1枚が入る = 圧縮と強化が同時に起きる
                    var deck = run.Deck.Where((_, i) => i != c.IndexA && i != c.IndexB).ToList();
                    return run with { Deck = Append(deck, fused), Gold = run.Gold - price, Phase = RunPhases.Map };
                }

                case RunCommand_WorkshopSkip:
                {
                    if (run.Phase != RunPhases.Workshop) throw new InvalidOperationException("工房フェーズではない");
                    return run with { Phase = RunPhases.Map };
                }

                case RunCommand_CampfireRemove:
                {
                    if (run.Phase != RunPhases.Campfire) throw new InvalidOperationException("焚き火フェーズではない");
                    // 除去はショップ専売。コマンド型は旧セーブ/ジャーナル互換のため残し、常に拒否する
                    throw new InvalidOperationException("焚き火では除去できない (除去はショップのみ。焚き火は 休む/鍛える の二択)");
                }

                case RunCommand_ShopBuyCard c:
                {
                    if (run.Phase != RunPhases.Shop || run.Shop == null) throw new InvalidOperationException("ショップではない");
                    var item = (c.Index >= 0 && c.Index < run.Shop.Cards.Count) ? run.Shop.Cards[c.Index] : null;
                    if (item == null) throw new InvalidOperationException($"不正な商品指定: {c.Index}");
                    if (item.Sold == true) throw new InvalidOperationException("その商品は売り切れ");
                    if (run.Gold < item.Price) throw new InvalidOperationException($"ゴールドが足りない ({item.Price}G)");
                    var card = new CardInstance { Uid = $"buy_a{run.Act}_r{run.Row}_{item.Id}", Def = Content.GetCardDef(item.Id) };
                    return run with
                    {
                        Gold = run.Gold - item.Price,
                        Deck = Append(run.Deck, card),
                        Picks = Append(run.Picks, item.Id),
                        // index を詰めない = 売切マーク
                        Shop = run.Shop with
                        {
                            Cards = run.Shop.Cards.Select((x, i) => i == c.Index ? x with { Sold = true } : x).ToList(),
                        },
                    };
                }

                case RunCommand_ShopBuyRelic:
                {
                    if (run.Phase != RunPhases.Shop || run.Shop == null) throw new InvalidOperationException("ショップではない");
                    if (run.Shop.RelicId == null) throw new InvalidOperationException("レリックの在庫がない");
                    if (run.Gold < run.Shop.RelicPrice) throw new InvalidOperationException($"ゴールドが足りない ({run.Shop.RelicPrice}G)");
                    string relicId = run.Shop.RelicId;
                    RunState next = run with
                    {
                        Gold = run.Gold - run.Shop.RelicPrice,
                        Relics = Append(run.Relics, relicId),
                        Shop = run.Shop with { RelicId = null },
                    };
                    next = ApplyRelicBonus(next, relicId);
                    // 会員証をその店で買ったら、まだ売れていない在庫もその場で値下げする
                    var ratio = Content.GetRelicDef(relicId).Bonus?.ShopPriceRatio;
                    if (ratio != null && ratio.Value != 1 && next.Shop != null)
                    {
                        next = next with
                        {
                            Shop = next.Shop with
                            {
                                Cards = next.Shop.Cards.Select(x => x.Sold == true ? x : x with { Price = JsFloor(x.Price * ratio.Value) }).ToList(),
                            },
                        };
                    }
                    return WithRelicGainBrands(next, run);
                }

                case RunCommand_ShopRemove c:
                {
                    if (run.Phase != RunPhases.Shop || run.Shop == null) throw new InvalidOperationException("ショップではない");
                    int price = ShopRemovalPrice(run);
                    if (run.Gold < price) throw new InvalidOperationException($"ゴールドが足りない ({price}G)");
                    var card = (c.Index >= 0 && c.Index < run.Deck.Count) ? run.Deck[c.Index] : null;
                    if (card == null) throw new InvalidOperationException($"不正な除去指定: {c.Index}");
                    if (run.Deck.Count <= 5) throw new InvalidOperationException("これ以上デッキを減らせない");
                    return run with
                    {
                        Gold = run.Gold - price,
                        Deck = run.Deck.Where((_, i) => i != c.Index).ToList(),
                        RemovalCount = run.RemovalCount + 1,
                    };
                }

                case RunCommand_ShopUpgrade c:
                {
                    if (run.Phase != RunPhases.Shop || run.Shop == null) throw new InvalidOperationException("ショップではない");
                    int price = ShopUpgradePrice(run);
                    if (run.Gold < price) throw new InvalidOperationException($"ゴールドが足りない ({price}G)");
                    var card = (c.Index >= 0 && c.Index < run.Deck.Count) ? run.Deck[c.Index] : null;
                    if (card == null) throw new InvalidOperationException($"不正な強化指定: {c.Index}");
                    if (Upgrade.IsUpgraded(card)) throw new InvalidOperationException("すでに鍛えられている");
                    if (Upgrade.UpgradeTier(card.Def) == Upgrade.UpgradeTiers.None)
                    {
                        throw new InvalidOperationException($"{card.Def.Name} は鍛えられない (エナジー上限を上げる札は強化対象外)");
                    }
                    return run with
                    {
                        Gold = run.Gold - price,
                        Deck = run.Deck.Select((x, i) => i == c.Index ? Upgrade.UpgradeCard(x) : x).ToList(),
                        UpgradeCount = run.UpgradeCount + 1,
                    };
                }

                case RunCommand_ShopLeave:
                {
                    if (run.Phase != RunPhases.Shop) throw new InvalidOperationException("ショップではない");
                    return run with { Shop = null, Phase = RunPhases.Map };
                }

                case RunCommand_EventChoice c:
                {
                    if (run.Phase != RunPhases.Event) throw new InvalidOperationException("イベントではない");
                    return ApplyEventChoice(run, c.Index, c.CardIndex);
                }

                default:
                    // 未知のコマンドは throw。旧実装は switch を素通りして undefined を返し、セーブを破壊した
                    throw new InvalidOperationException($"未知のランコマンド: {command.Type}");
            }
        }
    }
}
