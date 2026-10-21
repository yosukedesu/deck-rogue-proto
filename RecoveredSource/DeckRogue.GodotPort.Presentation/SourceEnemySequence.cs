using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceEnemySequence
{
	private static readonly HashSet<string> Status = new HashSet<string>(new string[12]
	{
		"StatusInflicted", "ExposedApplied", "EnemyWeakened", "BurnApplied", "StrengthGained", "GrowthAdded", "MomentumAdded", "LightGained", "LightDischarged", "LightSpent",
		"ArtifactBlocked", "PlayerArtifactBlocked"
	});

	private static readonly HashSet<string> Trap = new HashSet<string>(new string[14]
	{
		"ReactionTriggered", "ReactionHeld", "SetCardExpired", "SetCardDestroyed", "ReactionWhiffed", "ActionNegated", "GearUsed", "DeathSaved", "RetainerRushed", "TokenDestroyed",
		"RetainerSacrificed", "RetainerExpired", "RetainerCopied", "RetainerLifeExtended"
	});

	private static readonly HashSet<string> EnemyAct = new HashSet<string>(new string[13]
	{
		"EnemyActionExecuting", "EnemyHealed", "GoldStolen", "CardsMilled", "ThornsReflected", "ScaldTick", "BurnTick", "RegenTicked", "BlockShattered", "EnemyStaggered",
		"EnemyInterrupted", "EnemyDied", "EnemyFled"
	});

	public static bool HasPresentation(GameEvent e, ISet<string>? soundKeys = null)
	{
		bool flag = ((e is GameEvent_DamageDealt || e is GameEvent_BlockGained || e is GameEvent_IceBlockGained || e is GameEvent_HpHealed || e is GameEvent_TurnStarted || e is GameEvent_TurnEnded || e is GameEvent_EnemyIntentDeclared || e is GameEvent_HpLost) ? true : false);
		if (!flag && !Status.Contains(e.Type) && !Trap.Contains(e.Type) && !EnemyAct.Contains(e.Type))
		{
			if (e.Type != "CardsDrawn" && e.Type != "CardSet")
			{
				return soundKeys?.Contains(e.Type) ?? false;
			}
			return false;
		}
		return true;
	}

	public static float Gap(GameEvent e)
	{
		switch (e.Type)
		{
		case "DamageDealt":
			return 0.4f;
		case "TurnStarted":
		case "TurnEnded":
			return 0.6f;
		case "BlockGained":
		case "IceBlockGained":
		case "HpHealed":
			return 0.15f;
		case "ReactionTriggered":
			return 0.55f;
		case "EnemyActionExecuting":
			return 0.32f;
		case "EnemyInterrupted":
			return 0.55f;
		default:
		{
			float result;
			if (Status.Contains(e.Type) || Trap.Contains(e.Type))
			{
				result = 0.3f;
			}
			else
			{
				result = (EnemyAct.Contains(e.Type) ? 0.25f : 0.12f);
			}
			return result;
		}
		}
	}

	public static (float[] Onsets, float End) Plan(IReadOnlyList<GameEvent> events, ISet<string>? soundKeys = null)
	{
		float[] times = new float[events.Count];
		float delay = 0.02f;
		for (int i = 0; i < events.Count; i++)
		{
			times[i] = ((events[i] is GameEvent_CardPlayed) ? 0f : delay);
			if (HasPresentation(events[i], soundKeys))
			{
				delay += Gap(events[i]);
			}
		}
		return (Onsets: times, End: delay + 0.01f);
	}
}
