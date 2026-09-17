// Gears.cs — ギア (消耗品) の戦闘内解決 (src/engine/gears.ts の移植)。
// 骨格: 拾って持ち歩き (10個)、自ターンに1個だけ「魔素」1で組む。カードではないので
// 虚弱 (カードのプレイで得るブロック-25%) も勢い (カードのプレイで与えるダメージ) も乗らない。
// 成長は「与ダメ全てに乗る」既存則どおり乗る (置物トリガーと同じ扱い)。
// 1行ずつ TS に忠実に (unity/PORTING.md)。魔素・持ち物の減算は Run 層の担当。

using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public static class Gears
    {
        /// <summary>持ち歩ける個数 (裁定 2026-09-17)。死蔵は腕なので絞らない</summary>
        public const int GEAR_CARRY_MAX = 10;
        /// <summary>魔素の上限 (単位10の裁定 2026-09-17: 1個を組む値段が10 なので上限50＝ギア5個ぶん)</summary>
        public const int MANA_MAX = 50;
        /// <summary>1個を組む値段 (一律)。魔素の単位＝この値が「1個ぶん」</summary>
        public const int GEAR_MANA_COST = 10;
        /// <summary>レア度の抽選比 (本家形 C65／U25／R10。裁定 2026-09-17)</summary>
        public const int GEAR_RARITY_WEIGHT_COMMON = 65, GEAR_RARITY_WEIGHT_UNCOMMON = 25, GEAR_RARITY_WEIGHT_RARE = 10;

        public static GearDef GearDefOf(string gearId) => Content.GetGearDef(gearId);

        /// <summary>新しい持ち物を1個作る (残り回数は def.charges ?? 1)</summary>
        public static GearInstance MakeGear(string gearId, string uid)
        {
            return new GearInstance { Uid = uid, GearId = gearId, Charges = Content.GetGearDef(gearId).Charges ?? 1 };
        }

        /// <summary>ギアを組むのに必要な選択 (UI/CLI はこれを見て入力を集める)</summary>
        public sealed record GearNeedsInfo(bool Target, string? Card, bool Gear);

        public static GearNeedsInfo GearNeeds(GearDef def)
        {
            return new GearNeedsInfo(def.NeedsTarget == true, def.NeedsCard, def.Special == "nameless");
        }

        /// <summary>この盤面でギアを組めるか。理由つき (null = 組める)</summary>
        public static string? GearBlockedReason(GameState? state, int mana, GearInstance gear)
        {
            if (mana < GEAR_MANA_COST) return "魔素がない";
            if (gear.Charges <= 0) return "使い切っている";
            if (state == null) return "戦闘中でない";
            if (state.Phase != CombatPhases.PlayerTurn) return "自分の番ではない";
            if (state.EnemyPhase == true) return "敵の番には使えない";
            if (state.GearUsedThisTurn == true) return "このターンはもう組んだ";
            return null;
        }

        /// <summary>選択の対象になる手札 (写し・化けの粉・砥ぎ油)</summary>
        public static IReadOnlyList<CardInstance> GearCardChoices(GameState state, GearDef def)
        {
            switch (def.NeedsCard)
            {
                case "hand":
                {
                    // 砥ぎ油だけは「鍛えられる札」に限る (レア・工房産は対象外の既存裁定)
                    bool upgrade = false;
                    foreach (var e in def.Effects) if (e.Effect == "upgradeInHand") upgrade = true;
                    if (!upgrade) return state.Player.Hand;
                    var list = new List<CardInstance>();
                    foreach (var c in state.Player.Hand) if (Upgrade.CanUpgradeInHand(c)) list.Add(c);
                    return list;
                }
                case "discard":
                    return state.Player.DiscardPile;
                case "draw":
                {
                    // 山札の並びは伏せたまま = 名前順で見せる (サーチ札と同じ規約)。表示専用なので RNG に触れない
                    var list = new List<CardInstance>(state.Player.DrawPile);
                    list.Sort((a, b) => string.Compare(a.Def.Name, b.Def.Name, StringComparison.Ordinal));
                    return list;
                }
                default:
                    return new List<CardInstance>();
            }
        }

        static List<int> AliveIndices(GameState state)
        {
            var alive = new List<int>();
            for (int i = 0; i < state.Enemies.Count; i++) if (state.Enemies[i].Hp > 0) alive.Add(i);
            return alive;
        }

        /// <summary>
        /// ギアのダメージの「実際に与える値」(2026-09-17 プレイテスト J2 の直接の死因への処方)。
        /// カードの setCardLiveDamage と同じく純関数にして Web/CLI/Unity が共有する。全体ダメージは生存する敵ごとに並べる。変化が無ければ null
        /// </summary>
        public static string? GearLiveDamage(GameState state, GearDef def, int? targetIndex = null)
        {
            var alive = AliveIndices(state);
            var parts = new List<string>();
            foreach (var e in def.Effects)
            {
                if (e.Effect != "dealDamage" || e.Amount == null) continue;
                int live = Effects.PlayerDamageAfterModifiers(state, e.Amount.Value);
                List<int> targets = e.Target == "all" ? alive : targetIndex.HasValue ? new List<int> { targetIndex.Value } : alive.Take(1).ToList();
                var each = new List<string>();
                foreach (var i in targets)
                {
                    var bd = Effects.DamageBreakdownOf(state, i, e.Amount.Value, e.Pierce == true, true, false);
                    each.Add(bd != null ? $"敵{i}:{bd.HpLoss}" : $"敵{i}:{live}");
                }
                parts.Add($"{e.Amount.Value}→{string.Join(" / ", each)}");
            }
            if (parts.Count == 0) return null;
            string growth = state.Player.Growth > 0 ? $"成長+{state.Player.Growth}・" : "";
            return $"実際に与える値: {string.Join("、", parts)}（{growth}急所・装甲・敵ブロック込み。勢いは乗らない）";
        }

        /// <summary>魔素の表記「25/50（あと2個）」(単位が10になったので個数を添える。2026-09-17)</summary>
        public static string ManaLabel(int mana)
        {
            return $"{mana}/{MANA_MAX}（あと{(int)Math.Floor(mana / (double)GEAR_MANA_COST)}個）";
        }

        static readonly HashSet<string> StatusCardIds = new HashSet<string> { "status_wound", "status_scald", "status_junk", "status_brand", "status_guilt" };

        /// <summary>
        /// このギアを組んでも何も起きない時の理由 (2026-09-17 ユーザー裁定「組めるままにし、画面に出すだけ」)。null = 効く見込みがある。表示専用
        /// </summary>
        public static string? GearNoEffectReason(GameState state, GearDef def, int? targetIndex = null)
        {
            var alive = AliveIndices(state);
            int target = targetIndex.HasValue ? targetIndex.Value : alive.Count == 1 ? alive[0] : -1;
            EnemyState? e = target >= 0 && target < state.Enemies.Count ? state.Enemies[target] : null;
            var p = state.Player;
            foreach (var eff in def.Effects)
            {
                switch (eff.Effect)
                {
                    case "blockEnemySummon":
                    {
                        if (e == null) return null; // 対象未定 = 判定しない
                        var d = Content.GetEnemyDef(e.EnemyId);
                        // 残機 (count=1 の連鎖=再起動) は止められない (2026-09-18 裁定 A)。止まるのは召喚・分裂 (複数体)・孵化
                        bool splits = d.SplitInto != null && d.SplitInto.Count > 1;
                        bool summons = splits || d.HatchInto != null || (d.Moves ?? new List<EnemyMove>()).Any(m => m.Kind == "summon");
                        if (summons) return null;
                        return d.SplitInto != null ? "この敵の残機（再起動）は止められない（止まるのは召喚・分裂・孵化）" : "この敵は召喚も分裂も孵化もしない";
                    }
                    case "blockEnemyInterrupt":
                    {
                        if (e == null) return null;
                        var d = Content.GetEnemyDef(e.EnemyId);
                        var fired = e.FiredInterrupts ?? new List<int>();
                        int left = 0;
                        var ints = d.Interrupts ?? new List<EnemyInterrupt>();
                        for (int i = 0; i < ints.Count; i++) if (!fired.Contains(i)) left++;
                        return left > 0 ? null : "この敵には残っている割り込み（豹変・目覚め）が無い";
                    }
                    case "clearEnemyStrength":
                        if (e == null) return null;
                        return e.Strength > 0 ? null : "この敵の筋力は0以下（マイナスは戻さない）";
                    case "shatterBlock":
                        if (e == null) return null;
                        return e.Block > 0 || e.BurrowActive == true ? null : "この敵はブロックも殻も持っていない";
                    case "cleanseStatuses":
                        return p.Weak == 0 && p.Vulnerable == 0 && p.Frail == 0 && p.Restrain == 0 && (p.Mist ?? 0) == 0 && (p.Slow ?? 0) == 0
                            ? "消せる状態異常を受けていない"
                            : null;
                    case "purgeHandStatus":
                        return p.Hand.Any(c => StatusCardIds.Contains(c.Def.Id)) ? null : "手札に負傷・火傷・がらくた・烙印が無い";
                    case "gainHpRatio":
                    case "gainHp":
                        return p.Hp >= p.MaxHp ? "HPは満タン" : null;
                    case "retrieveFromDiscard":
                        return p.DiscardPile.Count == 0 ? "捨て札が無い" : null;
                    case "searchDeck":
                        return p.DrawPile.Count == 0 ? "山札が空" : null;
                    default:
                        break;
                }
            }
            return null;
        }

        public sealed class UseGearOptions
        {
            public int? TargetIndex;
            /// <summary>選んだカードの uid (needsCard の時)</summary>
            public string? CardUid;
            /// <summary>無銘の部品: 化ける先のギアID</summary>
            public string? AsGearId;
            /// <summary>無銘の部品が選べる候補 (このランで拾ったことのあるギアID)。run 層が渡す</summary>
            public IReadOnlyList<string>? SeenGearIds;
        }

        /// <summary>
        /// ギアの効果を盤面へ解決する (魔素・持ち物の減算は run 層の担当)。
        /// 'flee' (煙玉) はここでは何もしない = run 層が戦闘を離脱させる
        /// </summary>
        public static GameState ResolveGear(GameState state, GearDef def, UseGearOptions? opts = null)
        {
            opts ??= new UseGearOptions();
            // 組んだ事実をログに残す (2026-09-17 O)
            state = Events.Emit(state, new GameEvent_GearUsed { GearId = def.Id, Name = def.Name });
            if (def.Special == "flee") return state;
            if (def.Special == "nameless")
            {
                // 無銘の部品 (白紙の巻物): このランで拾ったことのあるギアのどれかになる
                var id = opts.AsGearId;
                if (id == null) throw new InvalidOperationException("無銘の部品には化ける先 (asGearId) が要る");
                if (opts.SeenGearIds != null && !opts.SeenGearIds.Contains(id))
                {
                    throw new InvalidOperationException("まだ拾ったことのないギアには化けられない");
                }
                var inner = Content.GetGearDef(id);
                if (inner.Special == "nameless") throw new InvalidOperationException("無銘の部品には化けられない");
                return ResolveGear(state, inner, new UseGearOptions { TargetIndex = opts.TargetIndex, CardUid = opts.CardUid, AsGearId = null, SeenGearIds = opts.SeenGearIds });
            }
            // 単体対象: 生存が1体なら自動 (StS式ターゲティングと同じ規約)
            var alive = AliveIndices(state);
            int target = opts.TargetIndex ?? -1;
            if (def.NeedsTarget == true)
            {
                if (target < 0)
                {
                    if (alive.Count != 1) throw new InvalidOperationException($"{def.Name} には対象が要る");
                    target = alive[0];
                }
                if (target >= state.Enemies.Count || state.Enemies[target].Hp <= 0)
                {
                    throw new InvalidOperationException("倒れた敵は対象にできない");
                }
            }
            else
            {
                target = alive.Count > 0 ? alive[0] : 0;
            }

            var s = state;
            foreach (var effect in def.Effects)
            {
                switch (effect.Effect)
                {
                    case "retrieveFromDiscard":
                    case "searchDeck":
                        s = MoveChosenCard(s, effect.Effect, opts.CardUid, def);
                        break;
                    case "upgradeInHand":
                        s = UpgradeChosenCard(s, opts.CardUid, def);
                        break;
                    case "copyCardInHand":
                        s = CopyChosenCard(s, opts.CardUid, def);
                        break;
                    case "transformInHand":
                        s = TransformChosenCard(s, opts.CardUid, def);
                        break;
                    default:
                        s = Effects.ResolveEffectTargeted(s, effect, target);
                        break;
                }
            }
            // ギアで敵が倒れうる (火薬・火薬樽)。カードのプレイと同じく決着処理を通す
            return Combat.CheckCombatEnd(s);
        }

        /// <summary>掘り出し (捨て札から) / 目当ての品 (山札から): 選んだ1枚を手札へ</summary>
        static GameState MoveChosenCard(GameState state, string kind, string? cardUid, GearDef def)
        {
            var from = kind == "searchDeck" ? state.Player.DrawPile : state.Player.DiscardPile;
            if (from.Count == 0) return state;
            CardInstance? card = cardUid == null ? from[0] : from.FirstOrDefault(c => c.Uid == cardUid);
            if (card == null) throw new InvalidOperationException($"{def.Name}: 選んだ札が見つからない");
            var rest = from.Where(c => c.Uid != card.Uid).ToList();
            var hand = new List<CardInstance>(state.Player.Hand) { card };
            var player = kind == "searchDeck"
                ? state.Player with { Hand = hand, DrawPile = rest }
                : state.Player with { Hand = hand, DiscardPile = rest };
            return state with { Player = player };
        }

        /// <summary>砥ぎ油: 手札1枚をこの戦闘中鍛える</summary>
        static GameState UpgradeChosenCard(GameState state, string? cardUid, GearDef def)
        {
            var choices = state.Player.Hand.Where(c => Upgrade.CanUpgradeInHand(c)).ToList();
            if (choices.Count == 0) return state;
            CardInstance? card = cardUid == null ? choices[0] : choices.FirstOrDefault(c => c.Uid == cardUid);
            if (card == null) throw new InvalidOperationException($"{def.Name}: 鍛えられない札は選べない");
            return state with
            {
                Player = state.Player with
                {
                    Hand = state.Player.Hand.Select(c => c.Uid == card.Uid ? Upgrade.UpgradeCard(c) : c).ToList(),
                },
            };
        }

        /// <summary>写し: 手札1枚のコピーを手札に加える (この戦闘限りのトークン)</summary>
        static GameState CopyChosenCard(GameState state, string? cardUid, GearDef def)
        {
            var hand = state.Player.Hand;
            if (hand.Count == 0) return state;
            CardInstance? card = cardUid == null ? hand[0] : hand.FirstOrDefault(c => c.Uid == cardUid);
            if (card == null) throw new InvalidOperationException($"{def.Name}: 選んだ札が見つからない");
            var copy = card with { Uid = $"{card.Uid}_copy{hand.Count}", Token = true };
            return state with { Player = state.Player with { Hand = new List<CardInstance>(hand) { copy } } };
        }

        /// <summary>化けの粉: 手札1枚を同じ色・同じレア度の別の札に変える (この戦闘限り)。RNG を1回消費</summary>
        static GameState TransformChosenCard(GameState state, string? cardUid, GearDef def)
        {
            var hand = state.Player.Hand;
            if (hand.Count == 0) return state;
            CardInstance? card = cardUid == null ? hand[0] : hand.FirstOrDefault(c => c.Uid == cardUid);
            if (card == null) throw new InvalidOperationException($"{def.Name}: 選んだ札が見つからない");
            string baseId = card.Def.Id.EndsWith("+", StringComparison.Ordinal) ? card.Def.Id.Substring(0, card.Def.Id.Length - 1) : card.Def.Id;
            var src = Content.AllCards.FirstOrDefault(c => c.Id == baseId) ?? card.Def;
            string srcRarity = src.Rarity ?? "common";
            var pool = Content.AllCards.Where(c => c.Color == src.Color && (c.Rarity ?? "common") == srcRarity && c.Id != src.Id).ToList();
            if (pool.Count == 0) return state;
            var (i, rng) = Rng.NextInt(state.Rng, 0, pool.Count - 1);
            var into = new CardInstance { Uid = $"{card.Uid}_morph", Def = Content.GetCardDef(pool[i].Id), Token = true };
            return state with
            {
                Rng = rng,
                Player = state.Player with { Hand = hand.Select(c => c.Uid == card.Uid ? into : c).ToList() },
            };
        }

        // 灰落とし (purgeHandStatus)・引き直し (redrawHand) は Effects.ResolveEffect 側で解決する
    }
}
