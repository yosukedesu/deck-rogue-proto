// Fusion.cs — src/engine/fusion.ts の厳密移植。カード合成 (工房)。確定済みルール表「カード合成（工房）」
// 同じ色のカード2枚 → 1枚の新カード。手書きレシピ (data/fusions.json) を優先し、
// それ以外は計算合成する。純関数・決定的 = 素材2枚の def だけから結果が決まる
// (リプレイ / Unity 移植に安全。RNG も時刻も使わない)。
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;

namespace DeckRogue.Engine
{
    /// <summary>data/fusions.json の1件 (手書きレシピ)。Content.AllFusions がこの型のリストを持つ</summary>
    public sealed record FusionRecipe
    {
        [JsonProperty("a")]
        public string A { get; init; } = default!;
        [JsonProperty("b")]
        public string B { get; init; } = default!;
        [JsonProperty("result")]
        public CardDef Result { get; init; } = default!;
    }

    public static class Fusion
    {
        private static IReadOnlyList<FusionRecipe> RECIPES => Content.AllFusions;

        private static readonly HashSet<string> REFILL = new HashSet<string>
        {
            "drawCards",
            "drawCardsPerCardPlayed",
            "dischargeAetherDraw",
            "impulseDraw",
            "retrieveFromExhaust",
            "playFromExhaust",
        };

        /// <summary>名前生成: 軸→語幹 (緑v1)。レシピ札は手書き名が優先される</summary>
        private static readonly (string Effect, string Word)[] WORD = new[]
        {
            ("applyBurn", "焔"),
            ("gainIceBlock", "氷"),
            ("negate", "封"),
            ("dealDamageDrain", "血"),
            ("gainHp", "光"),
            ("addGrowth", "蔦"),
            ("doubleGrowth", "花"),
            ("addMomentum", "角"),
            ("gainEnergy", "樹"),
            ("gainBlock", "皮"),
            ("drawCards", "葉"),
            ("gainHp", "露"),
            ("counter", "棘"),
            ("weakenEnemy", "根"),
            ("dealDamage", "牙"),
            ("dealDamageRandom", "賭"),
            ("impulseDraw", "閃"),
        };

        /// <summary>色別の語彙上書き (2026-08-31 白ラン指摘「白素材から牙葉の祭壇=緑語彙が生成」への是正)</summary>
        private static readonly Dictionary<string, (string Effect, string Word)[]> COLOR_WORD =
            new Dictionary<string, (string, string)[]>
            {
                ["black"] = new[]
                {
                    ("dealDamageDrain", "血"),
                    ("exhaustFromDeck", "墓"),
                    ("loseHp", "贄"),
                    ("gainBlock", "骨"),
                    ("drawCards", "冥"),
                    ("gainHp", "宵"),
                    ("dealDamage", "影"),
                },
                ["blue"] = new[]
                {
                    ("gainIceBlock", "氷"),
                    ("negate", "封"),
                    ("addAether", "霊"),
                    ("addSpellEcho", "谺"),
                    ("drawCards", "書"),
                    ("dealDamage", "潮"),
                },
                ["red"] = new[]
                {
                    ("applyBurn", "焔"),
                    ("dealDamageRandom", "賭"),
                    ("impulseDraw", "閃"),
                    ("addMomentum", "烈"),
                    ("gainEnergy", "儀"),
                    ("gainBlock", "炭"),
                    ("drawCards", "燼"),
                    ("dealDamage", "火"),
                },
                ["white"] = new[]
                {
                    ("summonPermanent", "旗"),
                    ("dealDamagePerPermanent", "列"),
                    ("gainHp", "光"),
                    ("weakenEnemy", "威"),
                    ("dealDamagePerBlock", "壁"),
                    ("gainBlock", "盾"),
                    ("dealDamage", "聖"),
                    ("drawCards", "典"),
                },
            };

        private static readonly Dictionary<string, string> NAME_FALLBACK = new Dictionary<string, string>
        {
            ["red"] = "火",
            ["blue"] = "水",
            ["white"] = "光",
            ["black"] = "影",
        };

        private static string WordOf(CardDef def)
        {
            var color = def.Color ?? "";
            if (COLOR_WORD.TryGetValue(color, out var table))
            {
                foreach (var (eff, w) in table)
                {
                    if (def.Effects.Any(e => e.Effect == eff)) return w;
                }
            }
            foreach (var (eff, w) in WORD)
            {
                if (def.Effects.Any(e => e.Effect == eff)) return w;
            }
            // フォールバックは色の語で (緑以外の合成が「樹」になる違和感への対処 2026-08-30)
            return NAME_FALLBACK.TryGetValue(color, out var f) ? f : "樹";
        }

        private static string SuffixOf(IReadOnlyList<DeclarativeEffect> effects)
        {
            var dmgs = effects.Where(e => e.Effect == "dealDamage" || e.Effect == "dealDamageRandom").ToList();
            bool blk = effects.Any(e => e.Effect == "gainBlock" || e.Effect == "gainIceBlock");
            // 特性が名前に出る: 多段=乱撃 / 全体=嵐 / 貫通=穿ち
            if (dmgs.Any(e => e.Target == "all")) return "嵐";
            if (dmgs.Count >= 2) return "乱撃";
            if (dmgs.Any(e => e.Pierce == true)) return "穿ち";
            if (dmgs.Count > 0 && blk) return "構え";
            if (blk) return "盾";
            if (dmgs.Count > 0) return "一撃";
            if (effects.Any(e => e.Effect == "applyBurn")) return "熾火";
            return "祝福";
        }

        private static CardDef? RecipeFor(CardDef a, CardDef b)
        {
            foreach (var r in RECIPES)
            {
                if ((r.A == a.Id && r.B == b.Id) || (r.A == b.Id && r.B == a.Id)) return r.Result;
            }
            return null;
        }

        /// <summary>タイプの支配順位 (確定済みルール表「カード合成（工房）」): 置物 &gt; リアクション &gt; 呪文 &gt; 物理</summary>
        private static int? TypeRank(string type) => type switch
        {
            "permanent" => 3,
            "reaction" => 2,
            "spell" => 1,
            "physical" => 0,
            _ => (int?)null, // TS の Record は undefined を返し、比較は常に false になる
        };

        private static bool RankGte(string x, string y)
        {
            var rx = TypeRank(x);
            var ry = TypeRank(y);
            if (rx == null || ry == null) return false;
            return rx.Value >= ry.Value;
        }

        private static bool RankLte(string x, string y)
        {
            var rx = TypeRank(x);
            var ry = TypeRank(y);
            if (rx == null || ry == null) return false;
            return rx.Value <= ry.Value;
        }

        private static readonly HashSet<string> REACTION_WINDOWS = new HashSet<string>
        {
            "onAttackIncoming",
            "onAttacked",
            "onEnemyAction",
            "onEnemyBuffed",
            "onEnemyDefended",
        };

        /// <summary>置物として誘発できる窓 (hooks.ts が置物にディスパッチするのはこの2つだけ)</summary>
        private static readonly HashSet<string> PERM_WINDOWS = new HashSet<string> { "onAttackIncoming", "onAttacked" };

        /// <summary>支配側 (結果タイプを与える側) と従属側を決める</summary>
        private static (CardInstance Dom, CardInstance Sub) Dominance(CardInstance a, CardInstance b)
        {
            return RankGte(a.Def.Type, b.Def.Type) ? (a, b) : (b, a);
        }

        private static readonly HashSet<string> PLAYCARD_ONLY = new HashSet<string>
        {
            "searchDeck", "retrieveFromDiscard", "upgradeInHand", "addCopyToDiscard", "exhaustFromDeckChoose",
            "retrieveFromExhaust", "playFromExhaust", "gainSetSlot", "sacrificeRetainer", "duplicateRetainers", "triggerRetainersNow",
        };

        private static readonly HashSet<string> DIES_IN_WINDOW = new HashSet<string>
        {
            "drawCards", "impulseDraw", "gainEnergy", "addCasts",
        };

        private static readonly HashSet<string> DEAD_ON_PERMANENT = new HashSet<string>
        {
            "negate", "growSelf", "momentumCarryHalf", "doubleGrowth", "doubleMomentum", "dischargeGrowth",
            "dischargeGrowthBlock", "dischargeMomentumDamage", "dischargeMomentumBlock", "dischargeMomentumBurn",
            "dischargeMomentumGrowth", "dischargeMomentumVolley", "dischargeAether", "dischargeAetherDraw", "dischargeBurn",
        };

        /// <summary>落とした効果の価値は最大の量効果へ振る (S2: 効果が落ちて素材より劣化する64件の是正。「合成不可」は増やさない)</summary>
        private static readonly Dictionary<string, double> DROP_VP = new Dictionary<string, double>
        {
            ["gainEnergy"] = 5, ["drawCards"] = 3, ["impulseDraw"] = 2, ["addCasts"] = 2.5, ["negate"] = 12,
            ["doubleGrowth"] = 8, ["doubleMomentum"] = 6, ["growSelf"] = 4, ["searchDeck"] = 6,
            ["retrieveFromDiscard"] = 5, ["upgradeInHand"] = 6, ["addCopyToDiscard"] = 3, ["exhaustFromDeckChoose"] = 3,
            ["retrieveFromExhaust"] = 5, ["playFromExhaust"] = 8, ["gainSetSlot"] = 6, ["momentumCarryHalf"] = 8,
        };

        private static readonly HashSet<string> DROP_AMOUNT_SCALED = new HashSet<string>
        {
            "gainEnergy", "drawCards", "impulseDraw", "addCasts",
        };

        private static readonly HashSet<string> QUANTITY = new HashSet<string>
        {
            "dealDamage", "counter", "gainBlock", "gainIceBlock", "gainHp", "applyBurn", "dealDamageDrain",
        };

        private static readonly Dictionary<string, double> FLAT_VP = new Dictionary<string, double>
        {
            ["negate"] = 12, ["shatterBlock"] = 4, ["shatterBlockConvert"] = 10,
        };

        /// <summary>軸一致ボーナス (2026-09-05 ユーザー裁定 A)</summary>
        private static readonly Dictionary<string, DeclarativeEffect> AXIS_BONUS = new Dictionary<string, DeclarativeEffect>
        {
            ["growth"] = new DeclarativeEffect { Trigger = "onPlay", Effect = "addGrowth", Amount = 1 },
            ["trample"] = new DeclarativeEffect { Trigger = "onPlay", Effect = "addMomentum", Amount = 2 },
            ["ramp"] = new DeclarativeEffect { Trigger = "onPlay", Effect = "discountNext", Amount = 1 },
            ["burn"] = new DeclarativeEffect { Trigger = "onPlay", Effect = "applyBurn", Amount = 2 },
            ["ice"] = new DeclarativeEffect { Trigger = "onPlay", Effect = "gainIceBlock", Amount = 2 },
            ["aether"] = new DeclarativeEffect { Trigger = "onPlay", Effect = "addAether", Amount = 1 },
            ["storm"] = new DeclarativeEffect { Trigger = "onPlay", Effect = "addCasts", Amount = 1 },
            ["heal"] = new DeclarativeEffect { Trigger = "onPlay", Effect = "gainHp", Amount = 2 },
            ["fortress"] = new DeclarativeEffect { Trigger = "onPlay", Effect = "gainBlock", Amount = 3 },
            ["retinue"] = new DeclarativeEffect { Trigger = "onPlay", Effect = "gainBlock", Amount = 2 },
            ["graveyard"] = new DeclarativeEffect { Trigger = "onPlay", Effect = "exhaustFromDeck", Amount = 1 },
        };

        private static readonly Dictionary<string, string> PERM_SUFFIX = new Dictionary<string, string>
        {
            ["red"] = "炉", ["blue"] = "泉", ["white"] = "祭壇", ["black"] = "柩",
        };

        /// <summary>JS の Math.round (半数は+∞方向へ)。C# の Math.Round は銀行家丸めなので使わない</summary>
        private static int JsRound(double v) => (int)Math.Floor(v + 0.5);

        /// <summary>TS の JSON.stringify(condition) 比較の等価物 (record の構造等価)</summary>
        private static bool SameCondition(EffectCondition? x, EffectCondition? y) => Equals(x, y);

        /// <summary>
        /// 合成の本体＝**効果の合体**（2026-09-05 ユーザー裁定「工房は全て合成できるようにしたい。設計から考え直そう」→ ask_user A/A/A/A）。
        /// 旧・価値保存（VP査定から量とコストを逆算）は査定表に無い効果が出るたび合成不可を増やし、緑49%・黒46%のペアが
        /// 合成できなくなっていた。新モデルは査定を使わない:
        ///  - 結果は2枚の効果を全部持つ札。同種効果は量を合算（同名2枚＝「真・」化が自然に成立）
        ///  - コスト＝合計−1（最低1・上限5。両方0Eなら0E。X札はX参照を保つ）＝本家形の「2枚を1枚に、1E得」
        ///  - 特性の伝播（多段合算〔上限5〕・貫通・全体）と支配順位（置物＞リアクション＞呪文＞物理）は従来どおり
        ///  - 置物化: 量のある効果は÷3で毎ターン化、量の無い効果（打ち消し・倍化・サーチ等）は「登場時に1回」
        ///  - 選択式は相手の効果を共通部（モードを問わず解決する effects）に足す。両方が選択式ならモードを連結
        ///  - 歯止めは現行のまま: 消滅の自動付与（0E/正味エナジー+補充・倍化・上限ランプ・衝動4以上・亡骸）・リアクション2E上限・
        ///    工房産の誘発ごと置物は鍛え不可（upgrade.ts）。「合成不可」は同じ札・色違いだけ
        /// </summary>
        private static CardDef MergeFusion(CardInstance x, CardInstance y)
        {
            // 引数の順序に依存しない (id順に正規化 = 決定性)
            CardInstance a0, b0;
            if (string.CompareOrdinal(x.Def.Id, y.Def.Id) <= 0) { a0 = x; b0 = y; } else { a0 = y; b0 = x; }
            bool sameName = a0.Def.Id == b0.Def.Id;
            // X札は「両方がX」の時だけXのまま。片方だけなら典型X=3の固定量に畳む (机上レビュー S 提案1)
            bool bothX = a0.Def.XCost == true && b0.Def.XCost == true;
            CardInstance Materialize(CardInstance c)
            {
                if (!(c.Def.XCost == true && !bothX)) return c;
                var eff = new List<DeclarativeEffect>();
                foreach (var e in c.Def.Effects)
                {
                    if (e.XHits == true)
                    {
                        for (int i = 0; i < 3; i++) eff.Add(e with { XHits = null });
                    }
                    else eff.Add(e);
                }
                return c with { Def = c.Def with { XCost = null, Cost = 3, Effects = eff } };
            }
            var a = Materialize(a0);
            var b = Materialize(b0);
            var (domi0, sub0) = Dominance(a, b);

            // --- コスト: 合計−1 (最低1・上限5)。0E素材は値引きにならない (Opusラン R)。X同士はXのまま ---
            int CostAsMaterial(CardDef d) => d.XCost == true ? 3 : d.Cost;
            int ca = CostAsMaterial(a.Def);
            int cb = CostAsMaterial(b.Def);
            int rawSumUncapped = (ca == 0 || cb == 0) ? Math.Max(ca, cb) : Math.Max(1, ca + cb - 1);
            int rawSum = Math.Min(5, rawSumUncapped);
            // 重い札は罠に収まらない (机上レビュー S2 提案2): 合計−1 が 2E を超えるならリアクション化せず、
            // 相手側のタイプで出してリアクションの効果をプレイ時へ変換する (旧「切り下げ分を量で払う」は量の無い効果の罠が無償で2Eになる穴)
            var domi = domi0;
            var sub = sub0;
            if (domi0.Def.Type == "reaction" && sub0.Def.Type != "reaction" && !bothX && rawSum > 2)
            {
                domi = sub0;
                sub = domi0;
            }
            string resultType = domi.Def.Type;
            bool keepModes = RankLte(resultType, "spell");

            string primaryWindow =
                resultType == "reaction"
                    ? (domi.Def.Effects.FirstOrDefault(e => REACTION_WINDOWS.Contains(e.Trigger))?.Trigger ?? "onAttacked")
                    : "onPlay";

            double droppedVp = 0;
            void Drop(DeclarativeEffect e)
            {
                bool has = DROP_VP.TryGetValue(e.Effect, out double vp);
                double baseVp = has ? vp : 4;
                double mult = (e.Amount != null && has && DROP_AMOUNT_SCALED.Contains(e.Effect)) ? e.Amount.Value : 1;
                droppedVp += baseVp * mult;
            }

            /// 素材1枚の効果列を結果タイプへ変換する (列の内部順序は保つ = 蔦の乱舞の交互構造を畳まない)
            List<DeclarativeEffect> ConvertAll(CardInstance c)
            {
                // モードを畳む時 (置物/リアクション化) は最初のモードだけ採る (S2: 「選ぶ」が「両方」になっていた)
                List<DeclarativeEffect> src;
                if (keepModes) src = new List<DeclarativeEffect>(c.Def.Effects);
                else
                {
                    src = new List<DeclarativeEffect>(c.Def.Effects);
                    if (c.Def.Modes != null && c.Def.Modes.Count > 0) src.AddRange(c.Def.Modes[0].Effects);
                }
                if (resultType == "permanent" && c.Def.Type != "permanent")
                {
                    var agg = new List<DeclarativeEffect>();
                    foreach (var e in src)
                    {
                        if (DEAD_ON_PERMANENT.Contains(e.Effect)) { Drop(e); continue; }
                        int twinIdx = -1;
                        for (int k = 0; k < agg.Count; k++)
                        {
                            var m = agg[k];
                            if (m.Effect == e.Effect && m.Target == e.Target && m.Pierce == e.Pierce && m.SummonId == e.SummonId
                                && SameCondition(m.Condition, e.Condition)
                                && m.GrowthMultiplier == e.GrowthMultiplier && m.MomentumMultiplier == e.MomentumMultiplier)
                            {
                                twinIdx = k;
                                break;
                            }
                        }
                        var twin = twinIdx >= 0 ? agg[twinIdx] : null;
                        if (twin != null && twin.Amount != null && e.Amount != null) agg[twinIdx] = twin with { Amount = twin.Amount + e.Amount };
                        else if (!(twin != null && twin.Amount == null && e.Amount == null)) agg.Add(e with { });
                    }
                    var outAgg = new List<DeclarativeEffect>();
                    foreach (var e in agg)
                    {
                        if (e.Amount == null || PLAYCARD_ONLY.Contains(e.Effect))
                        {
                            outAgg.Add(e with { Trigger = PERM_WINDOWS.Contains(e.Trigger) ? e.Trigger : "onPlay" });
                            continue;
                        }
                        int third = (int)Math.Floor((e.Amount ?? 0) / 3.0);
                        if (third <= 0)
                        {
                            outAgg.Add(e with { Trigger = PERM_WINDOWS.Contains(e.Trigger) ? e.Trigger : "onPlay" });
                            continue;
                        }
                        string trigger = PERM_WINDOWS.Contains(e.Trigger) ? e.Trigger : "onTurnStart";
                        var n = e with { Trigger = trigger, Amount = third };
                        if (e.AmountMax != null) n = n with { AmountMax = Math.Max(third, (int)Math.Floor((e.AmountMax ?? 0) / 3.0)) };
                        outAgg.Add(n);
                    }
                    return outAgg;
                }
                if (resultType == "reaction" && c.Def.Type != "reaction")
                {
                    var outList = new List<DeclarativeEffect>();
                    foreach (var e in src)
                    {
                        if (REACTION_WINDOWS.Contains(e.Trigger)) { outList.Add(e with { }); continue; }
                        if (PLAYCARD_ONLY.Contains(e.Effect) || DIES_IN_WINDOW.Contains(e.Effect) || e.Effect == "growSelf" || e.Effect == "momentumCarryHalf")
                        {
                            Drop(e);
                            continue;
                        }
                        if (e.Effect == "gainBlock" || e.Effect == "gainIceBlock") outList.Add(e with { Trigger = "onAttackIncoming" });
                        else outList.Add(e with { Trigger = primaryWindow });
                    }
                    return outList;
                }
                if (resultType == "reaction" && c.Def.Type == "reaction" && !ReferenceEquals(c, domi))
                {
                    // 従属側のリアクションの窓は支配側の主窓へ揃える (T2: 敵行動時に撃った罠に被攻撃前の効果も乗っていた)
                    return src.Select(e => REACTION_WINDOWS.Contains(e.Trigger) ? e with { Trigger = primaryWindow } : e with { }).ToList();
                }
                if (resultType != "reaction" && c.Def.Type == "reaction")
                {
                    // 重い札に吸われたリアクション: 窓の効果をプレイ時へ (返し→ダメージ・窓ブロック→ブロック・打ち消しは意味が無いので落とす)
                    var outList = new List<DeclarativeEffect>();
                    foreach (var e in src)
                    {
                        if (e.Effect == "negate") { Drop(e); continue; }
                        if (e.Effect == "counter") { outList.Add(e with { Effect = "dealDamage", Trigger = "onPlay" }); continue; }
                        outList.Add(e with { Trigger = "onPlay", Condition = null });
                    }
                    return outList;
                }
                return src.Select(e => e with { }).ToList();
            }

            // --- 合体: 列は素材の内部順序を保ち、ブロック単位で並べる。「準備 (成長・勢い・急所等) だけの札」を先に置く
            //     (S2: id順で勢いがダメージ行の後ろに落ち、素材より弱い合成品が183件) ---
            var blocks = new List<List<DeclarativeEffect>> { ConvertAll(domi), ConvertAll(sub) };
            bool HasDamage(IReadOnlyList<DeclarativeEffect> list) => list.Any(e => e.Effect == "dealDamage" && e.Trigger == "onPlay");
            var ordered = (blocks[1].Count > 0 && !HasDamage(blocks[1]) && HasDamage(blocks[0]))
                ? new List<List<DeclarativeEffect>> { blocks[1], blocks[0] }
                : blocks;
            var merged = new List<DeclarativeEffect>();
            var ownerOf = new List<int>();
            int TwinOf(DeclarativeEffect raw, int block)
            {
                for (int k = 0; k < merged.Count; k++)
                {
                    var m = merged[k];
                    if (ownerOf[k] != block // 同じ素材の中では畳まない (交互構造を保つ)
                        && m.Trigger == raw.Trigger && m.Effect == raw.Effect && m.Target == raw.Target && m.Pierce == raw.Pierce
                        && m.SummonId == raw.SummonId && SameCondition(m.Condition, raw.Condition)
                        && m.GrowthMultiplier == raw.GrowthMultiplier && m.MomentumMultiplier == raw.MomentumMultiplier && m.XHits == raw.XHits)
                    {
                        return k;
                    }
                }
                return -1;
            }
            double collapsedFlatVp = 0;
            int dmgSeen = 0;
            for (int block = 0; block < ordered.Count; block++)
            {
                var list = ordered[block];
                // ダメージ行を持つ札の効果は他方へ畳まない (交互構造・「ダメージの前に成長」の意味を保つ)。準備だけの札は相手の同種へ合算する
                bool canMerge = !HasDamage(list);
                foreach (var raw in list)
                {
                    if (raw.Effect == "dealDamage" && raw.Trigger != "onTurnStart")
                    {
                        bool consumed = false;
                        if (sameName && block == 1)
                        {
                            // 同名合成 (真・化) はダメージ行を対で合算 = 2枚ぶんを圧縮 (真・打撃12・真・二連10×2)
                            var rows = new List<(DeclarativeEffect M, int K)>();
                            for (int k = 0; k < merged.Count; k++)
                            {
                                if (merged[k].Effect == "dealDamage" && merged[k].Trigger == raw.Trigger) rows.Add((merged[k], k));
                            }
                            int di = dmgSeen++;
                            if (di >= 0 && di < rows.Count)
                            {
                                var hit = rows[di];
                                if (hit.M.Pierce == raw.Pierce && hit.M.Target == raw.Target && hit.M.Amount != null && raw.Amount != null)
                                {
                                    merged[hit.K] = hit.M with { Amount = hit.M.Amount + raw.Amount };
                                    consumed = true;
                                }
                            }
                        }
                        if (consumed) continue;
                        merged.Add(raw);
                        ownerOf.Add(block);
                        continue;
                    }
                    int t = canMerge ? TwinOf(raw, block) : -1;
                    if (t >= 0 && merged[t].Amount != null && raw.Amount != null)
                    {
                        var n = merged[t] with { Amount = (merged[t].Amount ?? 0) + raw.Amount };
                        if (merged[t].AmountMax != null && raw.AmountMax != null) n = n with { AmountMax = (merged[t].AmountMax ?? 0) + raw.AmountMax };
                        merged[t] = n;
                    }
                    else if (t >= 0 && merged[t].Amount == null && raw.Amount == null)
                    {
                        collapsedFlatVp += FLAT_VP.TryGetValue(raw.Effect, out var fv) ? fv : 0;
                    }
                    else
                    {
                        merged.Add(raw);
                        ownerOf.Add(block);
                    }
                }
            }
            bool IsShatter(DeclarativeEffect e) => e.Effect == "shatterBlock" || e.Effect == "shatterBlockConvert";
            void BoostLargest(List<DeclarativeEffect> list, int delta)
            {
                int best = -1;
                for (int i = 0; i < list.Count; i++)
                {
                    if (QUANTITY.Contains(list[i].Effect) && list[i].Amount != null && (best < 0 || (list[i].Amount ?? 0) > (list[best].Amount ?? 0))) best = i;
                }
                if (best >= 0) list[best] = list[best] with { Amount = Math.Max(1, (list[best].Amount ?? 0) + delta) };
            }
            // 軸一致ボーナス: 同じ軸の札同士を溶かすと、その軸の小さなおまけが乗る =
            // 「何と何を溶かすか」にデッキの軸の型が出る (S2: 得の95%が「1E札を重い札にタダで貼る」1パターンだった)
            var axesB = Run.AxesOf(b.Def);
            string? sharedAxis = Run.AxesOf(a.Def).FirstOrDefault(ax => axesB.Contains(ax) && AXIS_BONUS.ContainsKey(ax));
            if (sharedAxis != null)
            {
                var bonus = AXIS_BONUS[sharedAxis] with { Trigger = resultType == "reaction" ? primaryWindow : "onPlay" };
                int k2 = -1;
                for (int k = 0; k < merged.Count; k++)
                {
                    var m = merged[k];
                    if (m.Trigger == bonus.Trigger && m.Effect == bonus.Effect && m.Condition == null && m.Target == null && m.Amount != null)
                    {
                        k2 = k;
                        break;
                    }
                }
                if (k2 >= 0) merged[k2] = merged[k2] with { Amount = (merged[k2].Amount ?? 0) + (bonus.Amount ?? 0) };
                else
                {
                    merged.Insert(0, bonus);
                    ownerOf.Insert(0, -1);
                }
            }
            var effects = new List<DeclarativeEffect>();
            effects.AddRange(merged.Where(IsShatter));
            effects.AddRange(merged.Where(e => !IsShatter(e)));
            double unpaidVp = collapsedFlatVp + droppedVp;
            int costCut = 0;
            if (unpaidVp > 0)
            {
                if (resultType == "permanent")
                {
                    // 置物の補償: 登場時 (onPlay) の量効果へ等倍。無ければコストで返し (下限=素材の高い方。T2: 打ち消しが消えてコストだけ上がる下位互換)、
                    // 余りは登場時ブロックで返す (毎トリガー効果には乗せない。T3: 棘の蔓の毎攻撃ブロック2が6になっていた)
                    int onPlayQ = -1;
                    for (int i = 0; i < effects.Count; i++)
                    {
                        if (effects[i].Trigger == "onPlay" && QUANTITY.Contains(effects[i].Effect) && effects[i].Amount != null) { onPlayQ = i; break; }
                    }
                    if (onPlayQ >= 0)
                    {
                        effects[onPlayQ] = effects[onPlayQ] with { Amount = (effects[onPlayQ].Amount ?? 0) + JsRound(unpaidVp) };
                        unpaidVp = 0;
                    }
                    else
                    {
                        int slack = bothX ? 0 : Math.Max(0, rawSum - Math.Max(ca, cb));
                        costCut = Math.Min(slack, (int)Math.Floor(unpaidVp / 6.0));
                        unpaidVp -= costCut * 6;
                        if (unpaidVp >= 3)
                        {
                            effects.Add(new DeclarativeEffect { Trigger = "onPlay", Effect = "gainBlock", Amount = JsRound(unpaidVp) });
                        }
                        unpaidVp = 0;
                    }
                }
                else if (effects.Any(e => QUANTITY.Contains(e.Effect) && e.Amount != null))
                {
                    BoostLargest(effects, JsRound(unpaidVp));
                    unpaidVp = 0;
                }
            }
            // 5E上限で切った分は量を比例縮小して払う (S2: 真・巨獣の踏みつけ=5Eで100ダメ)
            if (!bothX && rawSumUncapped > 5)
            {
                double ratio = 5.0 / rawSumUncapped;
                for (int i = 0; i < effects.Count; i++)
                {
                    if (QUANTITY.Contains(effects[i].Effect) && effects[i].Amount != null)
                    {
                        effects[i] = effects[i] with { Amount = Math.Max(1, (int)Math.Floor((effects[i].Amount ?? 0) * ratio)) };
                    }
                }
            }

            // モード: 同名は各モードを対で合算、異なる選択式同士は連結。片方だけ選択式なら相手の効果は共通部に入る (上で並べ済み)
            IReadOnlyList<CardMode>? modes = null;
            if (keepModes && ((domi.Def.Modes != null && domi.Def.Modes.Count > 0) || (sub.Def.Modes != null && sub.Def.Modes.Count > 0)))
            {
                if (sameName && a.Def.Modes != null)
                {
                    var list = new List<CardMode>();
                    for (int i = 0; i < a.Def.Modes.Count; i++)
                    {
                        var m = a.Def.Modes[i];
                        var eff = new List<DeclarativeEffect>();
                        for (int k = 0; k < m.Effects.Count; k++)
                        {
                            var e = m.Effects[k];
                            DeclarativeEffect? o = null;
                            if (b.Def.Modes != null && i < b.Def.Modes.Count && k < b.Def.Modes[i].Effects.Count) o = b.Def.Modes[i].Effects[k];
                            eff.Add(o != null && o.Effect == e.Effect && e.Amount != null && o.Amount != null
                                ? e with { Amount = e.Amount + o.Amount }
                                : e);
                        }
                        list.Add(m with { Effects = eff });
                    }
                    modes = list;
                }
                else
                {
                    var list = new List<CardMode>();
                    if (domi.Def.Modes != null) list.AddRange(domi.Def.Modes);
                    if (sub.Def.Modes != null) list.AddRange(sub.Def.Modes);
                    modes = list;
                }
            }

            int cost = bothX ? 1 : rawSum;
            if (resultType == "reaction" && !bothX && cost > 2)
            {
                // リアクション同士で2Eを超えた分は出力で払う (切り下げ1Eにつき最大の量効果−6)
                for (int cut = cost - 2; cut > 0; cut--) BoostLargest(effects, -6);
                cost = 2;
            }
            // 補償先の量効果が無い時はコストで返す (T2: 打ち消しが跡形もなく消えてコストだけ上がる下位互換)。下限は素材の高い方のコスト
            if (unpaidVp >= 6 && !bothX) cost = Math.Max(Math.Max(ca, cb), cost - (int)Math.Floor(unpaidVp / 6.0));
            if (costCut > 0) cost = Math.Max(Math.Max(ca, cb), cost - costCut);

            // --- 歯止め (現行のまま) ---
            var all = new List<DeclarativeEffect>(effects);
            if (modes != null)
            {
                foreach (var m in modes) all.AddRange(m.Effects);
            }
            // 消滅の継承: 効果が1つも残らなかった素材の消滅は引き継がない (S2: 茨の返し×樹液=茨の返し+消滅の劣化)
            bool Contributed(CardInstance c) => ConvertAll(c).Count > 0;
            bool exhaust = (a.Def.Exhaust == true && Contributed(a)) || (b.Def.Exhaust == true && Contributed(b));
            int? necroCost = (a.Def.NecroCost != null || b.Def.NecroCost != null)
                ? Math.Min(a.Def.NecroCost ?? 99, b.Def.NecroCost ?? 99)
                : (int?)null;
            if (necroCost != null) exhaust = true;
            if (all.Any(e => e.Effect == "doubleGrowth" || e.Effect == "doubleMomentum")) exhaust = true;
            if (effects.Any(e => e.Effect == "gainEnergyMax")) exhaust = true;
            if (all.Where(e => e.Effect == "impulseDraw").Aggregate(0, (acc, e) => acc + (e.Amount ?? 0)) >= 4) exhaust = true;
            int net = all.Where(e => e.Effect == "gainEnergy" || e.Effect == "discountNext").Aggregate(0, (acc, e) => acc + (e.Amount ?? 0));
            bool refills = all.Any(e => REFILL.Contains(e.Effect));
            bool freeIfPhysical = a.Def.FreeIfHandAllPhysical == true || b.Def.FreeIfHandAllPhysical == true;
            // 手札参照の0E (年輪=物理／大城壁=呪文 2026-09-06)。両方が違うタイプを要求するなら先頭 (id順) の型
            string? freeIfHandAll = a.Def.FreeIfHandAll ?? b.Def.FreeIfHandAll ?? (freeIfPhysical ? "physical" : null);
            var freeIfMomentum = new List<int>();
            if (a.Def.FreeIfMomentumAtLeast != null) freeIfMomentum.Add(a.Def.FreeIfMomentumAtLeast.Value);
            if (b.Def.FreeIfMomentumAtLeast != null) freeIfMomentum.Add(b.Def.FreeIfMomentumAtLeast.Value);
            bool conditionalFree = freeIfHandAll != null || freeIfMomentum.Count > 0;
            if (!bothX && refills && (net - cost >= 0 || conditionalFree))
            {
                if (resultType != "permanent") exhaust = true;
                else while (net - cost >= 0 && cost < 5) cost++;
            }
            if (resultType == "permanent") exhaust = false;

            string suffix =
                resultType == "permanent"
                    ? (PERM_SUFFIX.TryGetValue(a.Def.Color ?? "", out var ps) ? ps : "大樹")
                    : resultType == "reaction" ? "罠" : SuffixOf(effects);
            string StemOf(CardDef d)
            {
                if (!(d.Id.StartsWith("fused_", StringComparison.Ordinal) || d.Id.StartsWith("fusion_", StringComparison.Ordinal))) return WordOf(d);
                var n = d.Name;
                if (n.StartsWith("真・", StringComparison.Ordinal)) n = n.Substring(2);
                if (n.EndsWith("+", StringComparison.Ordinal)) n = n.Substring(0, n.Length - 1);
                int idx = n.IndexOf("の", StringComparison.Ordinal);
                if (idx >= 0) n = n.Substring(0, idx);
                return n.Length > 3 ? n.Substring(0, 3) : n;
            }
            string wa = StemOf(a.Def);
            string wb = StemOf(b.Def);
            string uniq = UniqueCodePoints(wa, wb);
            // 語の重複は畳む (T1: 角牙牙の乱撃)。相手の語が何も足さない時は「大」を冠して素材と同名になるのを避ける (角牙の嵐×落ち葉の刃=大角牙の嵐)
            string stem0 = (wa == wb || uniq == wa || uniq == wb) ? "大" + uniq : uniq;
            string stem = stem0.Length > 4 ? stem0.Substring(0, 4) : stem0;
            string name = sameName ? "真・" + a.Def.Name : stem + "の" + suffix;
            var ids = new[] { a0.Def.Id, b0.Def.Id };
            var def = new CardDef
            {
                Id = "fused_" + ids[0] + "__" + ids[1],
                Name = name,
                Cost = cost,
                Type = resultType,
                Color = a.Def.Color!,
                Effects = effects,
                Modes = modes,
                XCost = bothX ? true : (bool?)null,
                Exhaust = exhaust ? true : (bool?)null,
                Retain = ((a.Def.Retain == true || b.Def.Retain == true) && resultType != "permanent") ? true : (bool?)null,
                DiscardCost = ((a.Def.DiscardCost ?? 0) != 0 || (b.Def.DiscardCost ?? 0) != 0)
                    ? (a.Def.DiscardCost ?? 0) + (b.Def.DiscardCost ?? 0)
                    : (int?)null,
                ExhaustCost = ((a.Def.ExhaustCost ?? 0) != 0 || (b.Def.ExhaustCost ?? 0) != 0)
                    ? (a.Def.ExhaustCost ?? 0) + (b.Def.ExhaustCost ?? 0)
                    : (int?)null,
                NecroCost = necroCost,
                FreeIfHandAllPhysical = freeIfPhysical ? true : (bool?)null,
                FreeIfHandAll = freeIfHandAll,
                RequiresRetainer = (a.Def.RequiresRetainer == true || b.Def.RequiresRetainer == true) ? true : (bool?)null,
                FreeIfMomentumAtLeast = freeIfMomentum.Count > 0 ? freeIfMomentum.Min() : (int?)null,
                BlazeDiscount = (a.Def.BlazeDiscount != null || b.Def.BlazeDiscount != null)
                    ? Math.Max(a.Def.BlazeDiscount ?? 0, b.Def.BlazeDiscount ?? 0)
                    : (int?)null,
                ExhaustUnlessExposedEnemy = (a.Def.ExhaustUnlessExposedEnemy == true || b.Def.ExhaustUnlessExposedEnemy == true) ? true : (bool?)null,
                Axis = (a.Def.Axis != null || b.Def.Axis != null) ? UniqueStrings(a.Def.Axis, b.Def.Axis) : null,
            };
            return def;
        }

        /// <summary>[...new Set([...wa, ...wb])].join('') — JS の文字列スプレッドはコードポイント単位</summary>
        private static string UniqueCodePoints(string wa, string wb)
        {
            var seen = new HashSet<int>();
            var sb = new StringBuilder();
            foreach (var s in new[] { wa, wb })
            {
                foreach (var r in s.EnumerateRunes())
                {
                    if (seen.Add(r.Value)) sb.Append(r.ToString());
                }
            }
            return sb.ToString();
        }

        private static IReadOnlyList<string> UniqueStrings(IReadOnlyList<string>? xa, IReadOnlyList<string>? xb)
        {
            var seen = new HashSet<string>();
            var outList = new List<string>();
            foreach (var src in new[] { xa, xb })
            {
                if (src == null) continue;
                foreach (var s in src)
                {
                    if (seen.Add(s)) outList.Add(s);
                }
            }
            return outList;
        }

        /// <summary>合成できない理由。null = 合成可。効果の合体モデルでは「同じ札」「色違い」だけが不可</summary>
        public static string? FuseBlockReason(CardInstance a, CardInstance b)
        {
            if (a.Uid == b.Uid) return "同じカードは選べない";
            if (RecipeFor(a.Def, b.Def) != null) return null;
            if (a.Def.Id.StartsWith("status_", StringComparison.Ordinal) || b.Def.Id.StartsWith("status_", StringComparison.Ordinal))
            {
                return "負傷・呪い・火傷は合成できない (使えない札)"; // T1: 「色違い」と出ていた
            }
            if (a.Def.Color != b.Def.Color) return "合成は同じ色のカード同士のみ";
            return null;
        }

        /// <summary>
        /// 計算合成: 効果の合体 + 特性の掛け合わせ + タイプの支配順位 (確定済みルール表「カード合成（工房）」)。
        /// 手書きレシピ (data/fusions.json) が最優先。純関数・決定的 (素材2枚の def だけから結果が決まる)
        /// </summary>
        public static CardDef FuseCards(CardInstance a, CardInstance b)
        {
            // 鍛えの引き継ぎ (2026-09-05 ユーザー裁定 C。T3 で作り直し): 素材のどちらかが鍛え済み (+) なら、
            // 鍛えていない方の素材を先に鍛えてから合体し、結果を鍛え済み (+) として出す = 鍛えた値は消えず、鍛えが結果全体に乗る。
            // (旧「素に戻して結果を1回鍛える」は結果のティアが倍率/単位を拾うとダメージ行が素に戻る穴 = T3 不具合b)
            bool anyUpgraded = Upgrade.IsUpgraded(a) || Upgrade.IsUpgraded(b);
            CardInstance Lift(CardInstance c) =>
                (anyUpgraded && !Upgrade.IsUpgraded(c) && Upgrade.UpgradeTier(c.Def) != Upgrade.UpgradeTiers.None) ? Upgrade.UpgradeCard(c) : c;
            var recipe = RecipeFor(a.Def, b.Def);
            if (recipe != null)
            {
                // レシピ産にも鍛えを引き継ぐ (T3 不具合a: 守りの蔓+×茨の返し=茨の砦が素材1枚より弱かった)
                return (anyUpgraded && Upgrade.UpgradeTier(recipe) != Upgrade.UpgradeTiers.None)
                    ? Upgrade.UpgradeCard(new CardInstance { Uid = "recipe", Def = recipe }).Def
                    : recipe;
            }
            var merged = MergeFusion(Lift(a), Lift(b));
            if (!anyUpgraded) return merged;
            return merged.Name.EndsWith("+", StringComparison.Ordinal) ? merged : merged with { Name = merged.Name + "+" };
        }

        private static readonly Dictionary<string, string> AXIS_JA = new Dictionary<string, string>
        {
            ["growth"] = "成長+1", ["trample"] = "勢い+2", ["ramp"] = "次のカード-1", ["burn"] = "延焼+2",
            ["ice"] = "氷壁+2", ["aether"] = "霊気+1", ["storm"] = "詠唱+1", ["heal"] = "回復+2",
            ["fortress"] = "ブロック+3", ["retinue"] = "ブロック+2", ["graveyard"] = "ミル1",
        };

        /// <summary>
        /// 合成の注記 (2026-09-05 T2/T3: 「効果の合体」と言いつつ量が黙って変わる・軸一致がなぜ乗ったか分からない)。
        /// CLI の FusePreview と UI の工房プレビューが同じ文言を出す
        /// </summary>
        public static IReadOnlyList<string> FusionNotes(CardInstance a, CardInstance b)
        {
            var notes = new List<string>();
            if (RecipeFor(a.Def, b.Def) != null) notes.Add("⭐レシピ: 手書きの一品");
            var axesB = Run.AxesOf(b.Def);
            string? shared = Run.AxesOf(a.Def).FirstOrDefault(ax => axesB.Contains(ax));
            if (shared != null && AXIS_JA.TryGetValue(shared, out var ja)) notes.Add($"軸一致 ({shared}): {ja} のおまけ");
            if (Upgrade.IsUpgraded(a) || Upgrade.IsUpgraded(b)) notes.Add("鍛えの引き継ぎ: 鍛えていない側の素材も鍛えてから合体 (結果は+)");
            int ca = a.Def.XCost == true ? 3 : a.Def.Cost;
            int cb = b.Def.XCost == true ? 3 : b.Def.Cost;
            if ((a.Def.XCost == true) != (b.Def.XCost == true)) notes.Add("X札は片方だけなら X=3 の固定量に畳む");
            if (ca == 0 || cb == 0) notes.Add("0E素材は値引きにならない (高い方のコスト)");
            if (ca + cb - 1 > 5 && ca > 0 && cb > 0) notes.Add("5E上限: 超えた分だけ量を比例縮小");
            var types = new[] { a.Def.Type, b.Def.Type };
            bool anyModes = (a.Def.Modes != null && a.Def.Modes.Count > 0) || (b.Def.Modes != null && b.Def.Modes.Count > 0);
            if (anyModes && (types.Contains("permanent") || types.Contains("reaction"))) notes.Add("選択式は置物化・罠化では最初のモードだけを採る");
            if (types.Contains("permanent") && !(a.Def.Type == "permanent" && b.Def.Type == "permanent"))
            {
                notes.Add("置物化: 量は同種を合計して÷3で毎ターン化 (3未満は登場時1回)。打ち消し・倍化・放出は落ちて価値を振り替え");
            }
            if (types.Contains("reaction") && !(a.Def.Type == "reaction" && b.Def.Type == "reaction"))
            {
                notes.Add(ca + cb - 1 > 2
                    ? "重い札は罠に収まらない: 合計−1が2Eを超えるので相手側のタイプで出る (返し→ダメージ・窓ブロック→ブロック)"
                    : "リアクション化: 相手の効果は罠の窓で解決 (ドロー・一時マナ・サーチは落ちて価値を振り替え)");
            }
            if (a.Def.Type == "reaction" && b.Def.Type == "reaction") notes.Add("罠同士: 窓は支配側の主窓に揃う。2Eを超える分は量で払う");
            return notes;
        }

        private static readonly Regex FUSED_ID = new Regex("^fused_(.+)__(.+)$", RegexOptions.None);

        /// <summary>
        /// 合成カードの定義をIDから復元する (見つからなければ null)。
        /// 合成IDは決定的 (fused_&lt;素材A&gt;__&lt;素材B&gt;) なので、素材を引いて再合成すれば同じ定義が返る。
        /// レシピ産 (fusion_*) はレシピ表から引く。
        /// </summary>
        public static CardDef? ResolveFusedDef(string id)
        {
            foreach (var r in RECIPES)
            {
                if (r.Result.Id == id) return r.Result;
            }
            var m = FUSED_ID.Match(id);
            if (!m.Success) return null;
            try
            {
                var a = Content.GetCardDef(m.Groups[1].Value);
                var b = Content.GetCardDef(m.Groups[2].Value);
                return FuseCards(new CardInstance { Uid = "resolve_a", Def = a }, new CardInstance { Uid = "resolve_b", Def = b });
            }
            catch
            {
                return null;
            }
        }
    }
}
