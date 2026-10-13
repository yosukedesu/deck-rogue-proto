// Source-backed decision text from Unity CampfireScreen/EventScreen/RewardScreen.
// Availability and eligibility always remain owned by the existing engine predicates.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class SourceDecisionInfo
{
    public const string TransformLockReason = "状態異常・烙印は\n変成できない\n（除去で消す）";
    public const string ExclusiveCampfire = "休む・鍛えるとは排他";

    public static string CardVerb(EventChoiceDef c) => c.RemoveCard == true ? "取り除く" :
        c.UpgradeCard == true ? "鍛える" : c.TransformCard == true ? "変成する" :
        c.UnexhaustCard == true ? "消滅を外す" : c.DuplicateCard == true ? "複製する" : "選ぶ";

    public static string CardPickHint(EventChoiceDef c) => CardVerb(c) + "札を1枚選ぶ" +
        (c.TransformCard == true ? "（状態異常・烙印は変成できない）" : "");

    public static string CardLockReason(RunState run, EventChoiceDef c, CardInstance card)
    {
        // The transform-specific source warning is more helpful than a generic grey face.
        if (c.TransformCard == true && !Run.CanTransformCard(card)) return TransformLockReason;
        if (c.RemoveCard == true && run.Deck.Count <= 5) return "デッキが5枚以下のため\n取り除けない";
        if (c.UpgradeCard == true && !Upgrade.CanUpgradeCard(card)) return Upgrade.IsUpgraded(card) ? "鍛え済み" : "鍛えられない札";
        if (c.UnexhaustCard == true && !Run.CanUnexhaustCard(card)) return "消滅を外せない";
        return null;
    }

    public static string EventUnavailableReason(RunState run, EventChoiceDef c)
    {
        if (Run.EventChoiceAvailable(run, c)) return "";
        if (c.RequireGold.HasValue && run.Gold < c.RequireGold.Value) return $"G不足（所持 {run.Gold} / 必要 {c.RequireGold.Value}）";
        // This is a more precise explanation of the engine's duplicate-relic gate.
        if (c.RelicId != null && run.Relics.Contains(c.RelicId)) return "すでに所持: " + Content.GetRelicDef(c.RelicId).Name;
        return "対象がない";
    }

    public static bool IsFreeChoice(EventChoiceDef c) =>
        !c.Gold.HasValue && !c.RequireGold.HasValue && !c.Hp.HasValue && !c.MaxHp.HasValue && !c.HpRatio.HasValue &&
        !c.Wounds.HasValue && !c.Brands.HasValue && !c.TimedCurses.HasValue && !c.AddRandomCards.HasValue &&
        c.Relic != true && c.RemoveCard != true && c.UpgradeCard != true && c.TransformCard != true &&
        c.DuplicateCard != true && c.RemoveAllWounds != true && !c.UpgradeRandomCards.HasValue && c.Gamble == null &&
        c.UnexhaustCard != true && c.RelicId == null && c.Gears == null && !c.Mana.HasValue && c.AddCardIds == null;

    public static string EventLabel(RunState run, EventChoiceDef c)
    {
        string reason = EventUnavailableReason(run, c);
        return (IsFreeChoice(c) ? "" : "▶ ") + c.Label +
            (Run.EventChoiceNeedsCard(c) ? "（デッキから1枚選ぶ）" : "") +
            (reason.Length == 0 ? "" : " · " + reason);
    }

    public static string ChoiceHint(EventChoiceDef c)
    {
        var parts = new List<string>();
        string Signed(int v) => (v >= 0 ? "+" : "") + v;
        if (c.Gold.HasValue) parts.Add(Signed(c.Gold.Value) + "G");
        if (c.RequireGold.HasValue) parts.Add("要" + c.RequireGold.Value + "G");
        if (c.Hp.HasValue) parts.Add("HP" + Signed(c.Hp.Value));
        if (c.MaxHp.HasValue) parts.Add("最大HP" + Signed(c.MaxHp.Value));
        if (c.HpRatio.HasValue) parts.Add("最大HP比" + c.HpRatio.Value.ToString(CultureInfo.InvariantCulture));
        if (c.Wounds.HasValue) parts.Add("負傷" + c.Wounds.Value);
        if (c.Brands.HasValue) parts.Add("烙印" + c.Brands.Value);
        if (c.TimedCurses.HasValue) parts.Add("仮初の烙印" + c.TimedCurses.Value);
        if (c.AddRandomCards.HasValue) parts.Add("ランダムカード" + c.AddRandomCards.Value + "枚");
        if (c.Relic == true) parts.Add("レリック");
        if (c.RemoveAllWounds == true) parts.Add("負傷を全て除去");
        if (c.UpgradeRandomCards.HasValue) parts.Add("ランダムに" + c.UpgradeRandomCards.Value + "枚鍛える");
        if (c.Gamble != null) parts.Add("賭け");
        return string.Join(" / ", parts);
    }

    public static string CampfireRest(RunState run)
    {
        var names = run.Relics.Select(Content.GetRelicDef).Where(r => r.Bonus?.NoRest == true).Select(r => r.Name).ToArray();
        if (names.Length > 0) return string.Join("・", names) + $": 焚き火で休めない\n回復なしで立ち去る（HP {run.Hp}/{run.MaxHp} のまま）";
        if (run.CampfireUpgradesUsed > 0) return $"すでに鍛えたので回復なし\n回復なしで立ち去る（HP {run.Hp}/{run.MaxHp} のまま）";
        int heal = (int)Math.Floor(run.MaxHp * run.CampfireRatio);
        return $"HP +{heal}（最大HPの {Math.Round(run.CampfireRatio * 100)}%）\n今のHP {run.Hp} → {Math.Min(run.MaxHp, run.Hp + heal)} / {run.MaxHp}";
    }

    public static string CampfireForge(RunState run)
    {
        var opt = Run.CampfireOptions(run);
        if (!opt.Forge) {
            string names = string.Join("・", run.Relics.Select(Content.GetRelicDef).Where(r => r.Bonus?.NoForge == true).Select(r => r.Name));
            return (names.Length == 0 ? "融合の鎚" : names) + ": 焚き火では鍛えられない";
        }
        int remain = Math.Max(0, Run.CampfireForgeAllowed(run) - run.CampfireUpgradesUsed);
        if (remain == 0) return "この焚き火ではもう鍛えられない";
        if (!run.Deck.Any(Upgrade.CanUpgradeCard)) return "このデッキに鍛えられる札がない";
        return $"デッキの1枚を強化（残り {remain} 回）\n鍛えると数値が伸びる・コストが下がる";
    }

    public static string CampfireTrain(RunState run) =>
        $"重石: 以後の戦闘開始時の成長+1\n現在 +{Run.RelicStateOf(run, "train")}・あと {Run.CampfireOptions(run).TrainLeft} 回";
}
