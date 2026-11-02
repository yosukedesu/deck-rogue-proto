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

	public static string Progress(RunState run)
	{
		return $"幕{run.Act}・" + ((run.Phase == "departure") ? "出立" : $"行{run.Row + 1}/{run.Map.Count}");
	}

	public static bool RareSlot(int index, int count)
	{
		if (count >= 6)
		{
			return index == count - 1;
		}
		return false;
	}

	public static string ManaOffer(RunState run)
	{
		return $"ギアを組む動力。1個ぶん（{10}）\n上限 {50}・いま {Gears.ManaLabel(run.Mana.GetValueOrDefault())}";
	}

	public static (RunState Run, bool IsBackup) ReadSavedSummary(SessionSaveStore store)
	{
		foreach (var candidate in store.ReadCandidates((string _) =>
		{
		}))
		{
			SessionController detached = new SessionController
			{
				AutoSave = false
			};
			if (detached.Load(candidate.Json).Success)
			{
				return (Run: detached.CurrentRun, IsBackup: candidate.IsBackup);
			}
		}
		return (Run: null, IsBackup: false);
	}

	public static string SaveSummary(RunState run)
	{
		string leader = run.LeaderId;
		try
		{
			leader = Content.GetLeaderDef(run.LeaderId).Name;
		}
		catch
		{
		}
		int hp = ((run.Phase == "combat" && run.Combat != null) ? run.Combat.Player.Hp : run.Hp);
		return $"{leader}\u3000{Progress(run)}\u3000HP {hp}/{run.MaxHp}\u3000{run.BattlesWon}勝\u3000難易度 {run.Difficulty}";
	}

	public static string SaveDetail(RunState run)
	{
		if (!(run.Phase == "combat") || !(run.Combat != null))
		{
			if (!(run.Phase == "departure"))
			{
				return FlowRules.PhaseName(run.Phase);
			}
			return "出立の店（坑口）にいる";
		}
		return $"戦闘中 ターン{run.Combat.Turn} の途中";
	}

	public static string QualityName(string mode)
	{
		return mode switch
		{
			"high" => "高", 
			"standard" => "標準", 
			"low" => "軽量", 
			_ => "自動", 
		};
	}

	public static IReadOnlyList<(int Width, int Height)> Resolutions(int width, int height)
	{
		if (width <= 0 || height <= 0)
		{
			return Array.Empty<(int, int)>();
		}
		return (from p in new (int, int)[5]
			{
				(1280, 720),
				(1600, 900),
				(1920, 1080),
				(2560, 1440),
				(3840, 2160)
			}.Where(((int, int) p) => p.Item1 <= width && p.Item2 <= height).Append((width, height)).Distinct()
			orderby p.Item1, p.Item2
			select p).ToArray();
	}
}
