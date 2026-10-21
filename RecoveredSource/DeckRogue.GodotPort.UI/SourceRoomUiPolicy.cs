// Descriptive room UI policy from pinned Unity 7a84644 Campfire/Event/WorkshopScreen.
// Never mutates a run, draws randomness, or dispatches a command.
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class SourceRoomUiPolicy
{
    public static (int A, int B) SelectMaterial(int a, int b, int picked)
    {
        if (a == picked) return (-1, b);
        if (b == picked) return (a, -1);
        if (a < 0) return (picked, b);
        if (b < 0) return (a, picked);
        return (b, picked);
    }

    public static bool RecipeMaterial(IReadOnlyList<Fusion.RecipePair> pairs, int a, int b, int index) =>
        a < 0 && b < 0 ? pairs.Any(p => p.IndexA == index || p.IndexB == index) :
        a >= 0 && b < 0 && pairs.Any(p => p.IndexA == a && p.IndexB == index || p.IndexB == a && p.IndexA == index);

    public static string PartnerGuide(RunState run, IReadOnlyList<Fusion.RecipePair> pairs, int a, int b)
    {
        if (a < 0 || b >= 0) return pairs.Count > 0 ? "デッキから2枚選ぶ（★の札はレシピの素材）" : "デッキから2枚選ぶ";
        var partners = pairs.Select(p => new { Pair = p, Other = p.IndexA == a ? p.IndexB : p.IndexB == a ? p.IndexA : -1 })
            .Where(p => p.Other >= 0)
            .GroupBy(p => p.Pair.Recipe.Name + "（相手: " + run.Deck[p.Other].Def.Name)
            .Select(g => "★ " + g.Key + (g.Count() > 1 ? " ×" + g.Count() : "") + "）").ToArray();
        return partners.Length == 0 ? "同じ色の札をもう1枚選ぶ" : "この札で作れる一品:\n" + string.Join("\n", partners);
    }

    public static float WorkshopResultScale(float viewportHeight, float otherContentHeight, bool phone)
    {
        if (!float.IsFinite(viewportHeight) || !float.IsFinite(otherContentHeight)) return .5f;
        return Math.Clamp((viewportHeight - otherContentHeight - 18) / 290f, .5f, phone ? .82f : 1.1f);
    }

    public static string WorkshopBlocked(RunState run, int a, int b)
    {
        if (a < 0 || b < 0 || a >= run.Deck.Count || b >= run.Deck.Count) return "素材を2枚選んでください";
        string reason = Fusion.FuseBlockReason(run.Deck[a], run.Deck[b]);
        if (reason != null) return "合成できない: " + reason;
        int price = Run.WorkshopFusePrice(run);
        return run.Gold < price ? $"G不足（所持 {run.Gold} / 必要 {price}）" : null;
    }
}
