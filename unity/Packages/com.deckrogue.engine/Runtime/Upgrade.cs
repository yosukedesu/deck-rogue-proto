// Upgrade.cs — src/engine/upgrade.ts の厳密移植。
// カードを鍛える (焚き火・ショップ・手札で鍛える) の純関数。
// 2026-09-02 run.ts から移設: 戦闘内の「手札で鍛える」(upgradeInHand) が combat.ts から呼ぶため、
// run.ts (combat.ts を import する) との循環を避けて独立モジュールにした。
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public static class Upgrade
    {
        /// <summary>
        /// 強化の対象になる「量」の効果 (確定済みルール表「焚き火」)。
        /// 単位効果 (ドロー・成長など) はティア③で+1、per-X はティア②のコスト-1で強化される。
        /// </summary>
        private static readonly HashSet<string> UPGRADABLE_EFFECTS = new HashSet<string>
        {
            "dealDamage",
            "gainBlock",
            "gainIceBlock",
            "applyBurn",
            "counter",
            "gainHp",
            "dealDamageDrain",
            "dealDamageRandom",
            "dealDamageExecute",
        };

        /// <summary>
        /// ティア③で+1する「単位」の効果。
        /// dealDamagePerCardPlayed / exhaustFromDeck は 0E でコストを削れない参照札 (余波・墓暴き) の
        /// 受け皿として追加 (2026-08-28 全カード解放)。ドローしない×Nは有限なので倍率+1でも安全。
        /// drawCardsPerCardPlayed 等のドロー×Nは入れない (倍率+1=×2ドローは無限ループの危険地帯) — ④の例外表で受ける
        /// </summary>
        private static readonly HashSet<string> UNIT_EFFECTS = new HashSet<string>
        {
            "addCardToHand", // 骨刃の舞+ = ナイフ+1 (本家の+準拠)
            "empowerShivs", // 急所読み+ = 常在+1
            "drawCards",
            "impulseDraw",
            "addGrowth",
            "addMomentum",
            "addAether",
            "gainEnergy",
            "discountNext",
            "dealDamagePerCardPlayed",
            "exhaustFromDeck",
        };

        private static DeclarativeEffect Eff(string trigger, string effect, int? amount = null, string? summonId = null) =>
            new DeclarativeEffect { Trigger = trigger, Effect = effect, Amount = amount, SummonId = summonId };

        /// <summary>
        /// ティア④: 同軸おまけの手書き例外表 (2026-08-28 全カード解放)。
        /// ②コスト-1が規約違反 (0E+補充=消滅必須) になり、③の対象効果も持たない補充参照札の受け皿。
        /// おまけは札自身の軸から外れない (カラーパイ・報酬抽選の軸判定を動かさない)。先頭に挿入する
        /// (霊気の奔流は「霊気+2 → 放出」の順で解決されることに意味がある)
        /// </summary>
        private static readonly Dictionary<string, IReadOnlyList<DeclarativeEffect>> BONUS_UPGRADES =
            new Dictionary<string, IReadOnlyList<DeclarativeEffect>>
            {
                // 連鎖する思考+: 自分自身も詠唱数に数えるフレーバーの +1ドロー
                ["blue_chain_thought"] = new[] { Eff("onPlay", "drawCards", 1) },
                // 霊気の奔流+: 放出の前に霊気+2 (実質ドロー+2)
                ["blue_aether_torrent"] = new[] { Eff("onPlay", "addAether", 2) },
                // 木陰の守り+: 固定ブロック+4を追加 (上限参照のコスト-1は0Eに落とさない裁定の受け皿。
                // 倍率には触れない安全弁を守りつつ、上限5で 10→14 ≈ 量+50%相当)
                // ---- per-Xダメージ参照のコスト強化封じ (2026-08-31) の受け皿: 同軸のおまけを足す ----
                ["blue_storm_lash"] = new[] { Eff("onPlay", "dealDamage", 5) }, // 固定の初撃5
                // 抱え込み (2026-08-31): ドローは手札=弾を増やす同軸のおまけ
                ["blue_weight_of_wisdom"] = new[] { Eff("onPlay", "drawCards", 1) },
                ["blue_knowledge_torrent"] = new[] { Eff("onPlay", "drawCards", 1) },
                ["blue_ripple_blade"] = new[] { Eff("onPlay", "dealDamage", 3) },
                ["blue_storm_echo"] = new[] { Eff("onAttacked", "dealDamage", 4) },
                ["blue_ice_lance"] = new[] { Eff("onPlay", "gainIceBlock", 4) }, // 氷壁を足してから撃つ
                ["red_all_in"] = new[] { Eff("onPlay", "dealDamage", 6) },
                ["white_rally"] = new[] { Eff("onPlay", "gainBlock", 4) }, // 隊列を組んでから撃つ
                // プール拡充 (2026-08-31): per-X参照でコスト強化を封じた札の受け皿
                ["blue_page_wind"] = new[] { Eff("onPlay", "drawCards", 1) },
                ["blue_rolling_wave"] = new[] { Eff("onPlay", "drawCards", 1) },
                ["black_grave_pressure"] = new[] { Eff("onPlay", "exhaustFromDeck", 2) }, // 自分で燃料を足してから刈る
                ["white_rank_thrust"] = new[] { Eff("onPlay", "gainBlock", 4) },
                ["red_streak_bet"] = new[] { Eff("onPlay", "dealDamage", 3) }, // 固定の床3 (茨の報い型)
                // 刃の葬列+ = ナイフをもう1枚 (per-Exhaust参照はコストに触れない裁定の受け皿)
                ["black_blade_procession"] = new[] { Eff("onPlay", "addCardToHand", 1, "black_shiv_token") },
                // 滾る血汐+ = ドレイン4を追加 (回復回数の参照はコストに触れない裁定の受け皿。自分で1回鳴らせる)
                ["black_seething_blood"] = new[] { Eff("onPlay", "dealDamageDrain", 4) },
                // 上限参照の1E札はコストを0Eへ落とさない裁定 (2026-08-30) の受け皿
                ["green_sapling_strike"] = new[] { Eff("onPlay", "dealDamage", 4) },
            };

        /// <summary>手札を補充する効果 (0E+補充=消滅必須、の規約判定。cardrules.test.ts と同じ定義)</summary>
        private static readonly HashSet<string> REFILL_FOR_UPGRADE = new HashSet<string>
        {
            "addCardToHand", // トークン生成も手札の補充 (0E化の無限ループ規約対象)
            "drawCards",
            "drawCardsPerCardPlayed",
            "dischargeAetherDraw",
            "impulseDraw",
            "retrieveFromExhaust",
            "playFromExhaust",
        };

        private static IReadOnlyList<DeclarativeEffect> AllEffectsOf(CardDef def)
        {
            var list = new List<DeclarativeEffect>(def.Effects);
            if (def.Modes != null)
            {
                foreach (var m in def.Modes) list.AddRange(m.Effects);
            }
            return list;
        }

        /// <summary>コスト-1すると無限ループ規約 (0E+補充=消滅必須 / 正味エナジー) に違反するか</summary>
        private static bool CostCutViolates(CardDef def)
        {
            if (def.Exhaust == true) return false;
            int newCost = def.Cost - 1;
            var eff = AllEffectsOf(def);
            bool refill = eff.Any(e => REFILL_FOR_UPGRADE.Contains(e.Effect));
            if (!refill) return false;
            int net = eff
                .Where(e => e.Effect == "gainEnergy" || e.Effect == "discountNext")
                .Aggregate(0, (a, e) => a + (e.Amount ?? 0));
            return net - newCost >= 0;
        }

        /// <summary>
        /// どのティアで強化されるか。'none' = 強化不可。
        /// 2026-08-28 全カード解放: gainEnergyMax の一律ブロックを撤廃 (上限ランプはコスト-1で強化。
        /// gainEnergyMax は UPGRADABLE / UNIT のどちらにも無いので量は絶対に増えない = 複利安全弁は
        /// 「量を強化しない」形で維持)。現行データでは全カードがいずれかのティアに落ちる
        /// (テストで機械固定)。'none' は将来のデータ追加への防衛用に残す
        /// </summary>
        public static class UpgradeTiers
        {
            public const string Amount = "amount";
            public const string Cost = "cost";
            public const string Unit = "unit";
            public const string Bonus = "bonus";
            public const string None = "none";
            public const string Mult = "mult";
            public const string Threshold = "threshold";
        }

        /// <summary>
        /// 本家形の鍛える (2026-09-04 ユーザー裁定「ok」。StS2 507枚の OnUpgrade 集計: 量+1が最多185・ダメ102・
        /// コスト-1はパワー/スキル52・参照札は倍率そのもの〔Heavy Blade ×3→×5・Rampage +5→+8〕)。
        /// その札のアイデンティティの数字を伸ばす: ①参照倍率+1 → ②単位+1 (勢いは+2) と量≥5の+50% →
        /// ③しきい値-1 と量+50% → ④量+50% → ⑤コスト-1 (置物・呪文で数字の無い札)。
        /// 緑で先行 (id が green_)。他色は解凍時に切り替える = 旧3段仕様のまま
        /// </summary>
        private static readonly HashSet<string> MULT_EFFECTS = new HashSet<string>
        {
            "dealDamagePerBlock", "dealDamagePerPermanent", "gainBlockPerPermanent",
            "dealDamagePerEnergyMax", "gainBlockPerEnergyMax", "dealDamagePerAttackPlayed",
            "dealDamagePerWeak", "dealDamagePerNegStrength", "dealDamagePerDamageTaken", "applyBurnPerDamageTaken",
            "dealDamagePerRandomPlayed", "dealDamagePerHandCard", "gainIceBlockPerHandCard",
            "dischargeGrowth", "dischargeGrowthBlock", "dischargeMomentumDamage", "dischargeMomentumBlock", "dischargeMomentumBurn", "dischargeMomentumVolley",
            "dealDamagePerCardPlayed", "dealDamagePerExhaust", "dealDamageDrainPerExhaust", "gainBlockPerExhaust", "dealDamagePerSelfHpLost", "dealDamagePerHeal",
        };

        private static readonly HashSet<string> UNIT_EFFECTS_V2 = new HashSet<string>
        {
            "drawCards", "impulseDraw", "addGrowth", "addMomentum", "addAether", "addCasts", "gainEnergy",
            "exposeEnemy", "weakenEnemy", "summonPermanent", "upgradeInHand", "addCardToHand", "empowerShivs", "exhaustFromDeck",
        };

        private static readonly HashSet<string> AMOUNT_V2 = BuildAmountV2();

        private static HashSet<string> BuildAmountV2()
        {
            var s = new HashSet<string>(UPGRADABLE_EFFECTS);
            s.Add("growSelf");
            return s;
        }

        private static bool HasMult(DeclarativeEffect e) =>
            (MULT_EFFECTS.Contains(e.Effect) && e.Amount != null) || e.GrowthMultiplier != null || e.MomentumMultiplier != null;

        private static bool HasThreshold(DeclarativeEffect e) =>
            e.Condition?.MinGrowth != null || e.Condition?.MinMomentum != null;

        private static bool IsGreenRule(CardDef def) =>
            def.Id.StartsWith("green_", StringComparison.Ordinal) || def.Color == "green"; // 工房産 (fused_*) も色で判定

        /// <summary>効果列1つぶんの本家形ティア (モードごとにも使う)</summary>
        private static string TierV2(IReadOnlyList<DeclarativeEffect> effects, CardDef? def = null)
        {
            if (effects.Any(HasMult)) return UpgradeTiers.Mult;
            if (effects.Any(e => UNIT_EFFECTS_V2.Contains(e.Effect) && e.Amount != null)) return UpgradeTiers.Unit;
            if (effects.Any(HasThreshold) || def?.FreeIfMomentumAtLeast != null) return UpgradeTiers.Threshold;
            if (effects.Any(e => AMOUNT_V2.Contains(e.Effect) && e.Amount != null)) return UpgradeTiers.Amount;
            return UpgradeTiers.None;
        }

        private static DeclarativeEffect Boost50(DeclarativeEffect e, int min)
        {
            if (!(AMOUNT_V2.Contains(e.Effect) && e.Amount != null && e.Amount >= min)) return e;
            var n = e with { Amount = (int)Math.Ceiling((e.Amount ?? 0) * 1.5) };
            if (e.AmountMax != null) n = n with { AmountMax = (int)Math.Ceiling((e.AmountMax ?? 0) * 1.5) };
            return n;
        }

        /// <summary>効果列に本家形の強化を当てる (ティアは列ごとに判定 = 選択式は各モードが独立に上がる)</summary>
        private static IReadOnlyList<DeclarativeEffect> ApplyV2(IReadOnlyList<DeclarativeEffect> effects, CardDef def)
        {
            string tier = TierV2(effects, def);
            if (tier == UpgradeTiers.Mult)
            {
                // ×1 の参照を含む札は先頭の参照だけ+1 (×1→×2 は+100%。幹の構え=上限×1ダメ+×1ブロックの両方を倍にすると
                // 1Eで200%になる。本家 Body Slam+ が ×2 でなくコスト0なのと同じく「倍にしない」側で揃える)
                bool onlyFirst = effects.Any(e => MULT_EFFECTS.Contains(e.Effect) && e.Amount == 1);
                bool done = false;
                var outList = new List<DeclarativeEffect>();
                foreach (var e in effects)
                {
                    if (onlyFirst && done) { outList.Add(e); continue; }
                    var n = e;
                    if (MULT_EFFECTS.Contains(e.Effect) && e.Amount != null) n = n with { Amount = e.Amount + 1 };
                    if (e.GrowthMultiplier != null) n = n with { GrowthMultiplier = e.GrowthMultiplier + 1 };
                    if (e.MomentumMultiplier != null) n = n with { MomentumMultiplier = e.MomentumMultiplier + 1 };
                    if (!ReferenceEquals(n, e)) done = true;
                    outList.Add(n);
                }
                return outList;
            }
            if (tier == UpgradeTiers.Unit)
            {
                bool done = false;
                var outList = new List<DeclarativeEffect>();
                foreach (var e in effects)
                {
                    if (!done && UNIT_EFFECTS_V2.Contains(e.Effect) && e.Amount != null)
                    {
                        done = true;
                        outList.Add(e with { Amount = e.Amount + (e.Effect == "addMomentum" ? 2 : 1) });
                        continue;
                    }
                    // 量4以上は同時に+50% (2026-09-05 Opusラン Q: 打ち据え+が4ダメのまま=打撃6より低い、の是正。本家 Bash+ は両方伸びる)
                    outList.Add(Boost50(e, 4));
                }
                return outList;
            }
            if (tier == UpgradeTiers.Threshold)
            {
                var outList = new List<DeclarativeEffect>();
                foreach (var e in effects)
                {
                    var c = e.Condition;
                    var n = e;
                    if (c != null && (c.MinGrowth != null || c.MinMomentum != null))
                    {
                        var nc = c;
                        if (c.MinGrowth != null) nc = nc with { MinGrowth = Math.Max(1, (c.MinGrowth ?? 0) - 1) };
                        if (c.MinMomentum != null) nc = nc with { MinMomentum = Math.Max(1, (c.MinMomentum ?? 0) - 1) };
                        n = e with { Condition = nc };
                    }
                    outList.Add(Boost50(n, 1));
                }
                return outList;
            }
            if (tier == UpgradeTiers.Amount) return effects.Select(e => Boost50(e, 1)).ToList();
            return effects;
        }

        /// <summary>
        /// 工房産の「誘発ごとに量が出る置物」は鍛えられない (2026-09-05 ユーザー裁定「ok」。Opusラン Q: 真・棘の蔓+=攻撃ごとブロック8が
        /// 幕2の被ダメを一桁に固定し、手数の鏡系の問いを数学的に無効化)。毎ターン固定出力 (onTurnStart) と登場時 (onPlay) の置物は対象のまま
        /// </summary>
        public static bool IsFusedPerTriggerPermanent(CardDef def)
        {
            if (def.Type != "permanent") return false;
            if (!def.Id.StartsWith("fused_", StringComparison.Ordinal) && !def.Id.StartsWith("fusion_", StringComparison.Ordinal)) return false;
            return def.Effects.Any(e => e.Trigger != "onPlay" && e.Trigger != "onTurnStart");
        }

        public static string UpgradeTier(CardDef def)
        {
            if (IsFusedPerTriggerPermanent(def)) return UpgradeTiers.None;
            var eff = AllEffectsOf(def);
            if (IsGreenRule(def))
            {
                // 上限ランプはコスト-1が正史 (複利安全弁: gainEnergyMax の量は増えない)
                if (eff.Any(e => e.Effect == "gainEnergyMax") && def.Cost >= 1 && !CostCutViolates(def)) return UpgradeTiers.Cost;
                var t = TierV2(eff, def);
                if (t != UpgradeTiers.None) return t;
                if (def.Cost >= 1 && !CostCutViolates(def)) return UpgradeTiers.Cost;
                if (BONUS_UPGRADES.ContainsKey(def.Id)) return UpgradeTiers.Bonus;
                return UpgradeTiers.None;
            }
            // 上限ランプはコスト-1が正史 (確定済みルール表「焚き火」)。2026-08-29 品質パスで
            // ランプ札に副次効果 (ブロック等) が付いたため、amount ティアに吸われて
            // 「0E化の当たり枠」が「副次+50%のハズレ枠」に化けるのを防ぐ
            if (eff.Any(e => e.Effect == "gainEnergyMax") && def.Cost >= 1 && !CostCutViolates(def))
            {
                return UpgradeTiers.Cost;
            }
            // 成長エンジン置物 (2026-09-02 段6人間プレイ「年輪の大樹+はブロック3伸ばされましても」):
            // カードの魂=毎T成長は単位効果で量ティアに乗らず、おまけのブロックだけが+50%されていた。
            // コスト-1 (2E→1E) = 「軽くなって置きやすい」が成長置物の正しい伸び方
            if (def.Id == "green_perm_growth_tree" && def.Cost >= 1 && !CostCutViolates(def)) return UpgradeTiers.Cost;
            if (eff.Any(e => UPGRADABLE_EFFECTS.Contains(e.Effect) && e.Amount != null)) return UpgradeTiers.Amount;
            // 上限参照札 (per-EnergyMax) のコスト-1強化は0Eまで落とさない (2026-08-30 裁定)。
            // 木陰の守り+ が 0E・非消滅・上限×2ブロック = 引くたびタダで盾、の退化ケースを塞ぐ。
            // 1E札は同軸おまけ (BONUS_UPGRADES) の受け皿へ
            // per-Xダメージ参照はコスト強化で1E以下に落とさない (2026-08-31 ユーザー許可。上限参照裁定の拡張)。
            // 氷の槍 (2E・氷壁×1) が焚き火のコスト強化で1E化し「消費しない参照×毎ターン補充」の
            // 連射砲 = 幕を勝つボタンになっていた実測への処方。2E以下のper-Xはコストに触れない
            bool perXDmg = eff.Any(
                e => e.Effect.StartsWith("dealDamagePer", StringComparison.Ordinal) && e.Effect != "dealDamagePerEnergyMax"); // 上限参照は既存裁定 (capRef) に委ねる
            // 0E札は既存の「倍率/量+1」ティア (④') に委ねる — 有限参照なので安全と裁定済み
            if (perXDmg && def.Cost >= 1 && def.Cost <= 2) return BONUS_UPGRADES.ContainsKey(def.Id) ? UpgradeTiers.Bonus : UpgradeTiers.None;
            bool capRef = eff.Any(
                e => e.Effect == "dealDamagePerEnergyMax" || e.Effect == "gainBlockPerEnergyMax");
            if (capRef && def.Cost == 1) return BONUS_UPGRADES.ContainsKey(def.Id) ? UpgradeTiers.Bonus : UpgradeTiers.None;
            if (def.Cost >= 1 && !CostCutViolates(def)) return UpgradeTiers.Cost;
            if (eff.Any(e => UNIT_EFFECTS.Contains(e.Effect) && e.Amount != null)) return UpgradeTiers.Unit;
            if (BONUS_UPGRADES.ContainsKey(def.Id)) return UpgradeTiers.Bonus;
            return UpgradeTiers.None;
        }

        /// <summary>すでに鍛えられているか (同じカードは1回だけ)</summary>
        public static bool IsUpgraded(CardInstance card)
        {
            return card.Def.Name.EndsWith("+", StringComparison.Ordinal);
        }

        /// <summary>この札は鍛えられるか (UI のボタン活性判定)</summary>
        public static bool CanUpgradeCard(CardInstance card)
        {
            return !IsUpgraded(card) && UpgradeTier(card.Def) != UpgradeTiers.None;
        }

        /// <summary>
        /// カードを鍛える (確定済みルール表「焚き火」の3段仕様)。
        /// ①量+50%切り上げ → ②コスト-1 → ③単位+1。名前に「+」が付く。
        /// 自傷 (loseHp) などの対価は据え置き = 非対称強化を仕様として認める (StSのHemokinesis+と同じ)。
        /// def を作り直すので engine 側に強化用の分岐は要らない (id は据え置き = 軸判定も不変)。
        /// </summary>
        public static CardInstance UpgradeCard(CardInstance card)
        {
            string tier = UpgradeTier(card.Def);
            if (IsGreenRule(card.Def) && tier != UpgradeTiers.Cost && tier != UpgradeTiers.Bonus && tier != UpgradeTiers.None)
            {
                var baseDef = card.Def;
                CardDef greenDef = baseDef with
                {
                    Name = baseDef.Name + "+",
                    Effects = ApplyV2(baseDef.Effects, baseDef),
                };
                if (baseDef.Modes != null)
                {
                    greenDef = greenDef with
                    {
                        Modes = baseDef.Modes.Select(m => m with { Effects = ApplyV2(m.Effects, baseDef) }).ToList(),
                    };
                }
                if (baseDef.FreeIfMomentumAtLeast != null && TierV2(baseDef.Effects, baseDef) == UpgradeTiers.Threshold)
                {
                    greenDef = greenDef with { FreeIfMomentumAtLeast = Math.Max(1, (baseDef.FreeIfMomentumAtLeast ?? 0) - 1) };
                }
                return card with { Def = LegalizeUpgrade(greenDef) };
            }

            Func<DeclarativeEffect, DeclarativeEffect> boostAmount = e =>
            {
                if (!UPGRADABLE_EFFECTS.Contains(e.Effect) || e.Amount == null) return e;
                var n = e with { Amount = (int)Math.Ceiling((e.Amount ?? 0) * 1.5) };
                if (e.AmountMax != null) n = n with { AmountMax = (int)Math.Ceiling((e.AmountMax ?? 0) * 1.5) };
                return n;
            };
            Func<DeclarativeEffect, DeclarativeEffect> boostUnit = e =>
            {
                if (!UNIT_EFFECTS.Contains(e.Effect) || e.Amount == null) return e;
                return e with { Amount = e.Amount + 1 };
            };

            CardDef def = card.Def with { Name = card.Def.Name + "+" };
            if (tier == UpgradeTiers.Amount || tier == UpgradeTiers.Unit)
            {
                var fn = tier == UpgradeTiers.Amount ? boostAmount : boostUnit;
                def = def with { Effects = card.Def.Effects.Select(fn).ToList() };
                if (card.Def.Modes != null)
                {
                    def = def with { Modes = card.Def.Modes.Select(m => m with { Effects = m.Effects.Select(fn).ToList() }).ToList() };
                }
            }
            else if (tier == UpgradeTiers.Cost)
            {
                def = def with { Cost = card.Def.Cost - 1 };
            }
            else if (tier == UpgradeTiers.Bonus)
            {
                // 同種効果は合算する (2026-08-31 青ラン指摘: 巻き波+ が「1ドロー、詠唱×2、1ドロー」と分裂表示)
                var acc = new List<DeclarativeEffect>(card.Def.Effects);
                IReadOnlyList<DeclarativeEffect> bonuses =
                    BONUS_UPGRADES.TryGetValue(card.Def.Id, out var bl) ? bl : new List<DeclarativeEffect>();
                foreach (var b in bonuses)
                {
                    int i = -1;
                    for (int k = 0; k < acc.Count; k++)
                    {
                        var e = acc[k];
                        if (e.Effect == b.Effect && e.Trigger == b.Trigger && e.Target == b.Target && e.Amount != null && b.Amount != null)
                        {
                            i = k;
                            break;
                        }
                    }
                    if (i >= 0)
                    {
                        acc[i] = acc[i] with { Amount = (acc[i].Amount ?? 0) + (b.Amount ?? 0) };
                        continue;
                    }
                    var next = new List<DeclarativeEffect> { b };
                    next.AddRange(acc);
                    acc = next;
                }
                def = def with { Effects = acc };
            }

            def = LegalizeUpgradeModes(def, tier, boostUnit);
            return card with { Def = LegalizeUpgrade(def) };
        }

        /// <summary>選択式 (modes) の旧3段仕様: 量ティアで量が伸びなかったモードには単位+1 (2026-09-03 人間ラン#3)</summary>
        private static CardDef LegalizeUpgradeModes(CardDef def, string tier, Func<DeclarativeEffect, DeclarativeEffect> boostUnit)
        {
            if (tier == UpgradeTiers.Amount && def.Modes != null)
            {
                return def with
                {
                    Modes = def.Modes.Select(m =>
                        m.Effects.Any(e => UPGRADABLE_EFFECTS.Contains(e.Effect) && e.Amount != null)
                            ? m
                            : m with { Effects = m.Effects.Select(boostUnit).ToList() }).ToList(),
                };
            }
            return def;
        }

        /// <summary>正味エナジー増の規約を強化後の派生にも守らせる (違反したら消滅を自動付与)</summary>
        private static CardDef LegalizeUpgrade(CardDef def0)
        {
            var def = def0;
            // 正味エナジー増の規約 (確定済みルール表「正味エナジー増」) を強化後の派生にも守らせる
            // (2026-08-31 青Opusラン発見: 水鏡の書庫+ = 5ドロー+一時マナ2 = 正味0マナの補充札が
            // 非消滅で生成され「毎ターン実質タダで5ドロー」の壊れ性能だった)。
            // 合成 (fusion.ts) と同じ処方 = 違反したら消滅を自動付与して合法化する
            var REFILL_FOR_LEGALITY = new[]
            {
                "drawCards",
                "drawCardsPerCardPlayed",
                "dischargeAetherDraw",
                "impulseDraw",
                "retrieveFromExhaust",
                "playFromExhaust",
            };
            var allEffects = AllEffectsOf(def);
            int netGain = allEffects
                .Where(e => e.Effect == "gainEnergy" || e.Effect == "discountNext")
                .Aggregate(0, (a, e) => a + (e.Amount ?? 0));
            bool refills = allEffects.Any(e => REFILL_FOR_LEGALITY.Contains(e.Effect));
            if (netGain - def.Cost >= 0 && refills && def.Exhaust != true)
            {
                def = def with { Exhaust = true };
            }
            return def;
        }

        /// <summary>
        /// 手札で鍛える (研ぎ澄まし 2026-09-02 ユーザー裁定): レアと工房産 (fused_/fusion_) は対象外。
        /// Opusラン A で「森の導き(サーチ)+研ぎ澄まし(一時強化)+工房の一点物」が1枚コンボに収束したため、一点物への一時強化を切る
        /// </summary>
        public static bool CanUpgradeInHand(CardInstance card)
        {
            if (card.Def.Rarity == "rare") return false;
            if (card.Def.Id.StartsWith("fused_", StringComparison.Ordinal) || card.Def.Id.StartsWith("fusion_", StringComparison.Ordinal)) return false;
            return CanUpgradeCard(card);
        }
    }
}
