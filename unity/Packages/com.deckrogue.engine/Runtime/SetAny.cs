// SetAny.cs — 実験「全カード伏せ可」(2026-09-02)。src/engine/setany.ts の移植。
// 通常カード (物理/呪文) を 1E で伏せ、誘発時に印字コストを持ち越しエナジーから払って発動する。
// 専用リアクションは従来どおり (伏せ時に印字コスト・発動無料 = 「伏せた時点で撃てる保証」が特権)。
// 変換は**トリガーだけ**: 防御系を含む札は被攻撃前 (onAttackIncoming)、それ以外は被攻撃後 (onAttacked) に
// onPlay 効果をそのまま解決する (dealDamage は行動してきた敵へ)。

using System;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public static class SetAny
    {
        /// <summary>通常カードを伏せる時に払う固定の手数料</summary>
        public const int SET_ANY_FEE = 1;

        /// <summary>敵フェーズに解決しても意味が無い・悪用になる効果は伏せ不可</summary>
        private static readonly HashSet<string> EXCLUDED = new HashSet<string>
        {
            "drawCards", "impulseDraw", "gainEnergy", "gainEnergyMax", "discountNext",
            "retrieveFromDiscard", "searchDeck", "upgradeInHand", "addCopyToDiscard", "growSelf",
            "exhaustFromDeck", "exhaustFromDeckChoose", "recycleExhaust", "addCardToHand", "summonPermanent",
            "gainSetSlot", "addSpellEcho", "addCasts", "retrieveFromExhaust", "playFromExhaust", "gainHp",
            "negate", "negateConvertIce", "confuse", "drawCardsPerCardPlayed", "dischargeAetherDraw",
        };

        /// <summary>これを含む札は被攻撃前 (pre) 窓で解決する = 守りとして構える</summary>
        private static readonly HashSet<string> PRE_WINDOW = new HashSet<string>
        {
            "gainBlock", "gainIceBlock", "gainBlockPerEnergyMax", "gainBlockPerPermanent", "gainIceBlockPerCardPlayed",
            "gainIceBlockPerHandCard", "dischargeGrowthBlock", "dischargeMomentumBlock", "shatterBlock", "shatterBlockConvert", "weakenEnemy",
        };

        /// <summary>通常カードとして伏せられるか (実験フラグが立っている時のみ意味を持つ)</summary>
        public static bool CanSetAsNormal(CardDef def)
        {
            if (def.Type != CardTypes.Physical && def.Type != CardTypes.Spell) return false;
            if (def.XCost == true || (def.Modes?.Count ?? 0) > 0) return false;
            // TS の truthy 判定: 0 は偽 (discardCost/exhaustCost)、necroCost は undefined でなければ真
            if ((def.DiscardCost ?? 0) != 0 || (def.ExhaustCost ?? 0) != 0 || def.NecroCost != null || def.ShivToken == true) return false;
            if (def.Effects.Count == 0) return false;
            for (int i = 0; i < def.Effects.Count; i++)
                if (def.Effects[i].Trigger != "onPlay") return false;
            for (int i = 0; i < def.Effects.Count; i++)
                if (EXCLUDED.Contains(def.Effects[i].Effect)) return false;
            for (int i = 0; i < def.Effects.Count; i++)
            {
                var e = def.Effects[i].Effect;
                if (e.StartsWith("dealDamage", StringComparison.Ordinal) || PRE_WINDOW.Contains(e)) return true;
            }
            return false;
        }

        /// <summary>伏せた通常カードが解決する窓 ("pre" / "post")</summary>
        public static string SetWindowStage(CardDef def)
        {
            for (int i = 0; i < def.Effects.Count; i++)
                if (PRE_WINDOW.Contains(def.Effects[i].Effect)) return "pre";
            return "post";
        }

        /// <summary>伏せ札として読む効果列。専用リアクションはそのまま、通常カードはトリガーだけを窓に差し替える</summary>
        public static IReadOnlyList<DeclarativeEffect> SetEffectsOf(CardInstance card)
        {
            if (card.Def.Type == CardTypes.Reaction) return card.Def.Effects;
            var trigger = SetWindowStage(card.Def) == "pre" ? "onAttackIncoming" : "onAttacked";
            var effects = new List<DeclarativeEffect>(card.Def.Effects.Count);
            for (int i = 0; i < card.Def.Effects.Count; i++) effects.Add(card.Def.Effects[i] with { Trigger = trigger });
            return effects;
        }

        /// <summary>伏せから発動する時に払うコスト (専用リアクションは伏せ時に支払い済み=0)</summary>
        public static int SetFireCost(CardInstance card)
        {
            return card.Def.Type == CardTypes.Reaction ? 0 : card.Def.Cost;
        }
    }
}
