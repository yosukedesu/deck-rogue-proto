using System;
using System.Collections.Generic;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    /// <summary>
    /// 人形の灯り＝寿命と火勢 (2026-09-21。docs/white-doll-life-proposal-2026-09-21.md) の UI 側の読み。
    /// TS の src/engine/effects.ts (dollAge / dollLifeTotal / dollLifeLeft / dollGrowth / dollEffectAmount) と同じ式。
    /// エンジン (Effects.cs) に同名の関数が入ったら、ここは薄い転送にしてよい (表示と実処理が同じ式を読むための写し)
    /// </summary>
    public static class DollUi
    {
        /// <summary>人形 = 従者 (Retainer) で生得 (Innate) でない置物</summary>
        public static bool IsDoll(CardInstance p)
        {
            return p != null && p.Def.Retainer == true && p.Innate != true;
        }

        /// <summary>齢 = 場に出てから経ったターン数 (点灯したターンは0)</summary>
        public static int Age(GameState st, CardInstance p)
        {
            if (st == null || p == null || p.EnteredTurn == null) return 0;
            return Math.Max(0, st.Turn - p.EnteredTurn.Value);
        }

        /// <summary>寿命の合計 (life+継ぎ火)。期限なしは null。life 未指定 (工房産など) は3</summary>
        public static int? LifeTotal(CardInstance p)
        {
            if (p == null) return null;
            if (p.LifePersist == true || p.Def.LifePersist == true) return null;
            return (p.Def.Life ?? 3) + (p.LifeBonus ?? 0);
        }

        /// <summary>残りのターン数 (今のターンを含む。1=このターンの敵フェーズが終わると消える)。期限なしは null</summary>
        public static int? LifeLeft(GameState st, CardInstance p)
        {
            var total = LifeTotal(p);
            if (total == null) return null;
            return Math.Max(0, total.Value - Age(st, p));
        }

        /// <summary>火勢 = 人形なら齢ぶん (ダメージ・ブロックの量に加算)</summary>
        public static int Growth(GameState st, CardInstance p)
        {
            return IsDoll(p) ? Age(st, p) : 0;
        }

        /// <summary>火勢が乗る効果 (TS DOLL_GROWTH_EFFECTS): ダメージとブロックだけ。回復・灯・率・ドローは増えない</summary>
        public static readonly HashSet<string> GrowthEffects = new HashSet<string> { "dealDamage", "dealDamageRandom", "dealDamageCleave", "gainBlock" };

        /// <summary>人形の効果の「いまの量」= 素の量 + アンセム (ANTHEM_EFFECTS) + 火勢。表示が実処理と同じ式を読む</summary>
        public static int? EffectAmount(GameState st, CardInstance p, DeclarativeEffect e, int anthem)
        {
            if (e.Amount == null) return null;
            if (p.Def.Retainer != true) return e.Amount;
            int a = anthem > 0 && Effects.ANTHEM_EFFECTS.Contains(e.Effect) ? anthem : 0;
            int g = GrowthEffects.Contains(e.Effect) ? Growth(st, p) : 0;
            return e.Amount.Value + a + g;
        }

        /// <summary>「人形1体を選ぶ」札 (写し灯・継ぎ火・永遠の灯 2026-09-21)。殉教の誓い (sacrificeRetainer) は別の旗</summary>
        public static bool CardChoosesDoll(CardDef def)
        {
            if (def == null) return false;
            foreach (var e in def.Effects)
                if (e.Trigger == "onPlay" && (e.Effect == "copyRetainer" || e.Effect == "extendRetainerLife" || e.Effect == "persistRetainer")) return true;
            return false;
        }

        /// <summary>灯りに触る札 (継ぎ火・永遠の灯) か: 期限なしの人形には効かないので選べない</summary>
        public static bool CardTouchesLife(CardDef def)
        {
            if (def == null) return false;
            foreach (var e in def.Effects)
                if (e.Trigger == "onPlay" && (e.Effect == "extendRetainerLife" || e.Effect == "persistRetainer")) return true;
            return false;
        }

        /// <summary>この札の対象としてその人形を選べるか (期限なしの人形に継ぎ火・永遠の灯は無駄)</summary>
        public static bool Eligible(GameState st, CardDef card, CardInstance p)
        {
            if (!IsDoll(p)) return false;
            if (CardTouchesLife(card) && LifeTotal(p) == null) return false;
            return true;
        }

        /// <summary>残りの灯りの一言: 「あとN」「尽きない」</summary>
        public static string LifeText(GameState st, CardInstance p)
        {
            var left = LifeLeft(st, p);
            return left == null ? "尽きない" : "あと" + left.Value;
        }
    }
}
