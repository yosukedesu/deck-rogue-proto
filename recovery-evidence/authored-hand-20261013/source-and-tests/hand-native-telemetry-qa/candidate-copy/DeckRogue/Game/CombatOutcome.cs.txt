using DeckRogue.Engine.Generated;

namespace DeckRogue.Game;

internal static class CombatOutcome
{
	public struct Decision
	{
		public string Kind;

		public string Reason;

		public bool Exact;

		public long Absorbed;
	}

	public static string PositiveSystemFamily(GameEvent ev, out string reason)
	{
		reason = "system_event_outside_scope";
		int loss;
		string family;
		if (ev is GameEvent_ThornsReflected th)
		{
			loss = th.HpLoss;
			family = "reflect";
		}
		else if (ev is GameEvent_BurnTick burn)
		{
			loss = burn.Amount;
			family = "burn";
		}
		else if (ev is GameEvent_HpLost lost)
		{
			loss = lost.Amount;
			family = "selfloss";
		}
		else
		{
			if (!(ev is GameEvent_ScaldTick scald))
			{
				return null;
			}
			loss = scald.Amount;
			family = "affliction";
		}
		if (loss <= 0)
		{
			reason = "system_no_positive_hp_loss";
			return null;
		}
		CombatPresentationReceipt receipt = CombatPresentationLedger.Read(ev);
		if (!CombatPresentationLedger.Valid(ev, receipt, out reason))
		{
			return null;
		}
		if ((long)receipt.Before.Hp - (long)receipt.After.Hp != loss || receipt.Before.MaxHp != receipt.After.MaxHp)
		{
			reason = "system_hp_delta_mismatch";
			return null;
		}
		long block = (long)receipt.Before.Block - (long)receipt.After.Block;
		long ice = (long)receipt.Before.IceBlock.GetValueOrDefault() - (long)receipt.After.IceBlock.GetValueOrDefault();
		if (block < 0 || ice < 0 || (family != "reflect" && (block != 0L || ice != 0)))
		{
			reason = "system_resource_delta_outside_scope";
			return null;
		}
		if (ev is GameEvent_ThornsReflected reflected && (reflected.Amount <= 0 || loss + block + ice != reflected.Amount))
		{
			reason = "system_reflection_amount_mismatch";
			return null;
		}
		reason = "exact_system_hp_receipt";
		return family;
	}

	public static Decision Read(GameEvent ev)
	{
		int loss;
		int legacy;
		if (ev is GameEvent_DamageDealt d)
		{
			loss = d.HpLoss;
			legacy = d.Blocked.GetValueOrDefault();
		}
		else if (ev is GameEvent_ConfusedAttack c)
		{
			loss = c.HpLoss;
			legacy = c.Blocked;
		}
		else
		{
			if (!(ev is GameEvent_ThornsReflected t))
			{
				return new Decision
				{
					Reason = "outcome_event_outside_scope"
				};
			}
			loss = t.HpLoss;
			legacy = t.Amount;
		}
		if (loss > 0)
		{
			return new Decision
			{
				Kind = "hit",
				Reason = "positive_hp_loss"
			};
		}
		Decision result = new Decision
		{
			Kind = ((legacy > 0) ? "guard" : "nullified"),
			Reason = "legacy_blocked_fallback"
		};
		CombatPresentationReceipt r = CombatPresentationLedger.Read(ev);
		if (!CombatPresentationLedger.Valid(ev, r, out string reason))
		{
			result.Reason = reason;
			return result;
		}
		if (loss < 0 || r.Before.Hp != r.After.Hp || r.Before.MaxHp != r.After.MaxHp)
		{
			result.Reason = "outcome_vitals_change_outside_scope";
			return result;
		}
		long block = (long)r.Before.Block - (long)r.After.Block;
		long ice = (long)r.Before.IceBlock.GetValueOrDefault() - (long)r.After.IceBlock.GetValueOrDefault();
		if (block < 0 || ice < 0)
		{
			result.Reason = "outcome_resource_increase_outside_scope";
			return result;
		}
		result.Absorbed = block + ice;
		result.Kind = ((result.Absorbed > 0) ? "guard" : "nullified");
		result.Exact = true;
		result.Reason = "exact_receipt_resource_delta";
		return result;
	}
}
