// Source-backed nonbattle information policy. No command dispatch or RNG access.
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Core;

namespace DeckRogue.GodotPort.UI;

public static class SourceRunScreenPolicy
{
    public const string ServicePriceNote = "除去・鍛えるは使うたび値上がり（ラン通算）";
    public static string Progress(RunState run) => $"幕{run.Act}・" +
        (run.Phase == RunPhases.Departure ? "出立" : $"行{run.Row + 1}/{run.Map.Count}");
    public static bool RareSlot(int index, int count) => count >= 6 && index == count - 1;
    public static string ManaOffer(RunState run) => $"ギアを組む動力。1個ぶん（{Gears.GEAR_MANA_COST}）\n上限 {Gears.MANA_MAX}・いま {Gears.ManaLabel(run.Mana.GetValueOrDefault())}";
    public static (RunState Run, bool IsBackup) ReadSavedSummary(SessionSaveStore store)
    {
        foreach (var candidate in store.ReadCandidates(_ => { }))
        {
            // Storage is intentionally absent, autosave is off, and no active session
            // listeners are attached. Validation cannot rewrite the user's save.
            var detached = new SessionController { AutoSave = false };
            if (detached.Load(candidate.Json).Success) return (detached.CurrentRun, candidate.IsBackup);
        }
        return (null, false);
    }
    public static string SaveSummary(RunState run)
    {
        string leader = run.LeaderId;
        try { leader = Content.GetLeaderDef(run.LeaderId).Name; } catch { }
        int hp = run.Phase == RunPhases.Combat && run.Combat != null ? run.Combat.Player.Hp : run.Hp;
        return $"{leader}　{Progress(run)}　HP {hp}/{run.MaxHp}　{run.BattlesWon}勝　難易度 {run.Difficulty}";
    }
    public static string SaveDetail(RunState run) => run.Phase == RunPhases.Combat && run.Combat != null
        ? $"戦闘中 ターン{run.Combat.Turn} の途中"
        : run.Phase == RunPhases.Departure ? "出立の店（坑口）にいる" : FlowRules.PhaseName(run.Phase);
    public static string QualityName(string mode) => mode switch { "high" => "高", "standard" => "標準", "low" => "軽量", _ => "自動" };
    public static IReadOnlyList<(int Width, int Height)> Resolutions(int width, int height)
    {
        if (width <= 0 || height <= 0) return Array.Empty<(int, int)>();
        var presets = new[] { (1280, 720), (1600, 900), (1920, 1080), (2560, 1440), (3840, 2160) };
        return presets.Where(p => p.Item1 <= width && p.Item2 <= height).Append((width, height))
            .Distinct().OrderBy(p => p.Item1).ThenBy(p => p.Item2).ToArray();
    }
}
