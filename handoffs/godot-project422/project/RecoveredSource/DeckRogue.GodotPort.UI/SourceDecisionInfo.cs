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

	public static string CardVerb(EventChoiceDef c)
	{
		if (c.RemoveCard != true)
		{
			if (c.UpgradeCard != true)
			{
				if (c.TransformCard != true)
				{
					if (c.UnexhaustCard != true)
					{
						if (c.DuplicateCard != true)
						{
							return "選ぶ";
						}
						return "複製する";
					}
					return "消滅を外す";
				}
				return "変成する";
			}
			return "鍛える";
		}
		return "取り除く";
	}

	public static string CardPickHint(EventChoiceDef c)
	{
		return CardVerb(c) + "札を1枚選ぶ" + ((c.TransformCard == true) ? "（状態異常・烙印は変成できない）" : "");
	}

	public static string CardLockReason(RunState run, EventChoiceDef c, CardInstance card)
	{
		if (c.TransformCard == true && !Run.CanTransformCard(card))
		{
			return "状態異常・烙印は\n変成できない\n（除去で消す）";
		}
		if (c.RemoveCard == true && run.Deck.Count <= 5)
		{
			return "デッキが5枚以下のため\n取り除けない";
		}
		if (c.UpgradeCard == true && !Upgrade.CanUpgradeCard(card))
		{
			if (!Upgrade.IsUpgraded(card))
			{
				return "鍛えられない札";
			}
			return "鍛え済み";
		}
		if (c.UnexhaustCard == true && !Run.CanUnexhaustCard(card))
		{
			return "消滅を外せない";
		}
		return null;
	}

	public static string EventUnavailableReason(RunState run, EventChoiceDef c)
	{
		if (Run.EventChoiceAvailable(run, c))
		{
			return "";
		}
		if (c.RequireGold.HasValue && run.Gold < c.RequireGold.Value)
		{
			return $"G不足（所持 {run.Gold} / 必要 {c.RequireGold.Value}）";
		}
		if (c.RelicId != null && run.Relics.Contains(c.RelicId))
		{
			return "すでに所持: " + Content.GetRelicDef(c.RelicId).Name;
		}
		return "対象がない";
	}

	public static bool IsFreeChoice(EventChoiceDef c)
	{
		if (!c.Gold.HasValue && !c.RequireGold.HasValue && !c.Hp.HasValue && !c.MaxHp.HasValue && !c.HpRatio.HasValue && !c.Wounds.HasValue && !c.Brands.HasValue && !c.TimedCurses.HasValue && !c.AddRandomCards.HasValue && c.Relic != true && c.RemoveCard != true && c.UpgradeCard != true && c.TransformCard != true && c.DuplicateCard != true && c.RemoveAllWounds != true && !c.UpgradeRandomCards.HasValue && c.Gamble == null && c.UnexhaustCard != true && c.RelicId == null && c.Gears == null && !c.Mana.HasValue)
		{
			return c.AddCardIds == null;
		}
		return false;
	}

	public static string EventLabel(RunState run, EventChoiceDef c)
	{
		string reason = EventUnavailableReason(run, c);
		return (IsFreeChoice(c) ? "" : "▶ ") + c.Label + (Run.EventChoiceNeedsCard(c) ? "（デッキから1枚選ぶ）" : "") + ((reason.Length == 0) ? "" : (" · " + reason));
	}

	public static string ChoiceHint(EventChoiceDef c)
	{
		List<string> parts = new List<string>();
		if (c.Gold.HasValue)
		{
			parts.Add(Signed(c.Gold.Value) + "G");
		}
		if (c.RequireGold.HasValue)
		{
			parts.Add("要" + c.RequireGold.Value + "G");
		}
		if (c.Hp.HasValue)
		{
			parts.Add("HP" + Signed(c.Hp.Value));
		}
		if (c.MaxHp.HasValue)
		{
			parts.Add("最大HP" + Signed(c.MaxHp.Value));
		}
		if (c.HpRatio.HasValue)
		{
			parts.Add("最大HP比" + c.HpRatio.Value.ToString(CultureInfo.InvariantCulture));
		}
		if (c.Wounds.HasValue)
		{
			parts.Add("負傷" + c.Wounds.Value);
		}
		if (c.Brands.HasValue)
		{
			parts.Add("烙印" + c.Brands.Value);
		}
		if (c.TimedCurses.HasValue)
		{
			parts.Add("仮初の烙印" + c.TimedCurses.Value);
		}
		if (c.AddRandomCards.HasValue)
		{
			parts.Add("ランダムカード" + c.AddRandomCards.Value + "枚");
		}
		if (c.Relic == true)
		{
			parts.Add("レリック");
		}
		if (c.RemoveAllWounds == true)
		{
			parts.Add("負傷を全て除去");
		}
		if (c.UpgradeRandomCards.HasValue)
		{
			parts.Add("ランダムに" + c.UpgradeRandomCards.Value + "枚鍛える");
		}
		if (c.Gamble != null)
		{
			parts.Add("賭け");
		}
		return string.Join(" / ", parts);
		static string Signed(int v)
		{
			return ((v >= 0) ? "+" : "") + v;
		}
	}

	public static string CampfireRest(RunState run)
	{
		string[] names = (from r in run.Relics.Select(Content.GetRelicDef)
			where r.Bonus?.NoRest == true
			select r.Name).ToArray();
		if (names.Length != 0)
		{
			return string.Join("・", names) + $": 焚き火で休めない\n回復なしで立ち去る（HP {run.Hp}/{run.MaxHp} のまま）";
		}
		if (run.CampfireUpgradesUsed <= 0)
		{
			int heal = (int)Math.Floor((double)run.MaxHp * run.CampfireRatio);
			return $"HP +{heal}（最大HPの {Math.Round(run.CampfireRatio * 100.0)}%）\n今のHP {run.Hp} → {Math.Min(run.MaxHp, run.Hp + heal)} / {run.MaxHp}";
		}
		return $"すでに鍛えたので回復なし\n回復なしで立ち去る（HP {run.Hp}/{run.MaxHp} のまま）";
	}

	public static string CampfireForge(RunState run)
	{
		if (!Run.CampfireOptions(run).Forge)
		{
			string names = string.Join("・", from r in run.Relics.Select(Content.GetRelicDef)
				where r.Bonus?.NoForge == true
				select r.Name);
			return ((names.Length == 0) ? "融合の鎚" : names) + ": 焚き火では鍛えられない";
		}
		int remain = Math.Max(0, Run.CampfireForgeAllowed(run) - run.CampfireUpgradesUsed);
		if (remain == 0)
		{
			return "この焚き火ではもう鍛えられない";
		}
		if (!run.Deck.Any(Upgrade.CanUpgradeCard))
		{
			return "このデッキに鍛えられる札がない";
		}
		return $"デッキの1枚を強化（残り {remain} 回）\n鍛えると数値が伸びる・コストが下がる";
	}

	public static string CampfireTrain(RunState run)
	{
		return $"重石: 以後の戦闘開始時の成長+1\n現在 +{Run.RelicStateOf(run, "train")}・あと {Run.CampfireOptions(run).TrainLeft} 回";
	}
}
