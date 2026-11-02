using System;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class SetAny
{
	public const int SET_ANY_FEE = 1;

	private static readonly HashSet<string> EXCLUDED = new HashSet<string>
	{
		"drawCards", "impulseDraw", "gainEnergy", "gainEnergyMax", "discountNext", "retrieveFromDiscard", "searchDeck", "upgradeInHand", "addCopyToDiscard", "growSelf",
		"exhaustFromDeck", "exhaustFromDeckChoose", "recycleExhaust", "addCardToHand", "summonPermanent", "gainSetSlot", "addSpellEcho", "addCasts", "retrieveFromExhaust", "playFromExhaust",
		"gainHp", "negate", "negateConvertIce", "confuse", "drawCardsPerCardPlayed", "dischargeAetherDraw"
	};

	private static readonly HashSet<string> PRE_WINDOW = new HashSet<string>
	{
		"gainBlock", "gainIceBlock", "gainBlockPerEnergyMax", "gainBlockPerPermanent", "gainIceBlockPerCardPlayed", "gainIceBlockPerHandCard", "dischargeGrowthBlock", "dischargeMomentumBlock", "shatterBlock", "shatterBlockConvert",
		"weakenEnemy"
	};

	public static bool CanSetAsNormal(CardDef def)
	{
		if (def.Type != "physical" && def.Type != "spell")
		{
			return false;
		}
		if (def.XCost == true || (def.Modes?.Count ?? 0) > 0)
		{
			return false;
		}
		if (def.DiscardCost.GetValueOrDefault() != 0 || def.ExhaustCost.GetValueOrDefault() != 0 || def.ImpulseCost.GetValueOrDefault() != 0 || def.NecroCost.HasValue || def.ShivToken == true)
		{
			return false;
		}
		if (def.Effects.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < def.Effects.Count; i++)
		{
			if (def.Effects[i].Trigger != "onPlay")
			{
				return false;
			}
		}
		for (int j = 0; j < def.Effects.Count; j++)
		{
			if (EXCLUDED.Contains(def.Effects[j].Effect))
			{
				return false;
			}
		}
		for (int k = 0; k < def.Effects.Count; k++)
		{
			string e = def.Effects[k].Effect;
			if (e.StartsWith("dealDamage", StringComparison.Ordinal) || PRE_WINDOW.Contains(e))
			{
				return true;
			}
		}
		return false;
	}

	public static string SetWindowStage(CardDef def)
	{
		for (int i = 0; i < def.Effects.Count; i++)
		{
			if (PRE_WINDOW.Contains(def.Effects[i].Effect))
			{
				return "pre";
			}
		}
		return "post";
	}

	public static IReadOnlyList<DeclarativeEffect> SetEffectsOf(CardInstance card)
	{
		if (card.Def.Type == "reaction")
		{
			return card.Def.Effects;
		}
		string trigger = ((SetWindowStage(card.Def) == "pre") ? "onAttackIncoming" : "onAttacked");
		List<DeclarativeEffect> effects = new List<DeclarativeEffect>(card.Def.Effects.Count);
		for (int i = 0; i < card.Def.Effects.Count; i++)
		{
			effects.Add(card.Def.Effects[i]with
			{
				Trigger = trigger
			});
		}
		return effects;
	}

	public static int SetFireCost(CardInstance card)
	{
		if (!(card.Def.Type == "reaction"))
		{
			return card.Def.Cost;
		}
		return 0;
	}
}
