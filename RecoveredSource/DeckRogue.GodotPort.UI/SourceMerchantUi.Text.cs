// NEW IMPLEMENTATION, not byte-identical recovery of the missing completed helper.
// Text and failure-reason branches adapted from verified Unity 7a84644
// DepartureScreen.cs:293-351. The Godot caller displays plain labels.
// Availability decisions remain exclusively in the engine/caller.
using System;
using System.Collections.Generic;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static partial class SourceMerchantUi
{
        public static string Summary(DepartureOffer offer)
        {
            var ch = offer.Choice;
            var parts = new List<string>();
            if (ch.RelicId != null)
            {
                string rn = ch.RelicId;
                try { rn = Content.GetRelicDef(ch.RelicId).Name; } catch (Exception) { }
                parts.Add("古代の遺物「" + rn + "」");
            }
            if (ch.Gears != null && ch.Gears.Count > 0)
            {
                var sb = new System.Text.StringBuilder("ギア");
                foreach (var id in ch.Gears) { var gd = GearDefOrNull(id); sb.Append("「" + (gd != null ? gd.Name : id) + "」"); }
                parts.Add(sb.ToString());
            }
            if (ch.Mana.HasValue) parts.Add("魔素" + ch.Mana.Value);
            if (ch.AddCardIds != null) foreach (var id in ch.AddCardIds) parts.Add("札「" + CardNameOrId(id) + "」");
            if (parts.Count == 0) return offer.Text ?? "";
            return string.Join("＋", parts.ToArray());
        }

        /// <summary>説明文 (ホバー・スマホはタップで固定パネル): 名前・種類・文・中身の本文</summary>
        public static string Details(DepartureOffer offer)
        {
            var ch = offer.Choice;
            var sb = new System.Text.StringBuilder();
            sb.Append(offer.Name + "  " + offer.Price + "G\n" + offer.Text);
            if (ch.RelicId != null)
            {
                try { var rd = Content.GetRelicDef(ch.RelicId); sb.Append("\n" + rd.Name + ": " + rd.Description); } catch (Exception) { }
            }
            if (ch.Gears != null) foreach (var id in ch.Gears) { var gd = GearDefOrNull(id); if (gd != null) sb.Append("\n" + gd.Name + ": " + gd.Text); }
            if (ch.AddCardIds != null)
                foreach (var id in ch.AddCardIds)
                {
                    try { var cd = Content.GetCardDef(id); sb.Append("\n" + cd.Name + ": " + CardText.Body(cd)); } catch (Exception) { }
                }
            return sb.ToString();
        }

        /// <summary>出立の店で買えない理由 (買った・G・対象の札)</summary>
        public static string UnavailableReason(RunState run, DepartureOffer offer)
        {
            if (Run.DepartureOfferBought(run, offer)) return "買った";
            if (run.Gold < offer.Price) return "G が足りない";
            return UnavailableReason(run, offer.Choice);
        }

        /// <summary>選べない理由 (engine の EventChoiceAvailable が false の時)</summary>
        public static string UnavailableReason(RunState run, EventChoiceDef ch)
        {
            if (ch.RequireGold.HasValue && run.Gold < ch.RequireGold.Value) return "G が足りない";
            if (ch.RelicId != null && System.Linq.Enumerable.Contains(run.Relics, ch.RelicId)) return "その遺物はもう持っている";
            if (ch.RemoveCard == true && run.Deck.Count <= 5) return "デッキが5枚以下";
            if (ch.UpgradeCard == true) return "鍛えられる札がない";
            if (ch.TransformCard == true) return "変成できる札がない";
            return "対象の札がない";
        }


    private static GearDef GearDefOrNull(string id)
    {
        try { return Content.GetGearDef(id); }
        catch (Exception) { return null; }
    }

    private static string CardNameOrId(string id)
    {
        try { return Content.GetCardDef(id).Name; }
        catch (Exception) { return id; }
    }
}
