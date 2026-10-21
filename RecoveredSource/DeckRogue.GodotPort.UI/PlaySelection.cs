using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public sealed class PlaySelection
{
	public readonly List<string> Discard = new List<string>();

	public readonly List<string> Exhaust = new List<string>();

	public readonly List<string> Deck = new List<string>();

	public readonly List<string> Hand = new List<string>();

	public readonly string? DeckKind;

	public readonly int DeckNeed;

	public readonly int HandNeed;

	public readonly bool NeedRetrieve;

	public readonly bool NeedPermanent;

	public CardInstance Card { get; }

	public int? ModeIndex { get; set; }

	public bool? Kindle { get; set; }

	public int ImpulseNeed
	{
		get
		{
			if (Kindle == false && FlowRules.CanSkipKindle(Card.Def))
			{
				return 0;
			}
			return Card.Def.ImpulseCost.GetValueOrDefault();
		}
	}

	public int? XAmount { get; set; }

	public int? TargetIndex { get; set; }

	public string? RetrieveUid { get; set; }

	public string? PermanentUid { get; set; }

	public PlaySelection(GameState state, CardInstance card)
	{
		Card = card;
		DeckKind = Combat.DeckChooseKindOf(card.Def);
		DeckNeed = ((DeckKind != null) ? Math.Min(card.Def.Effects.Where((DeclarativeEffect e) => e.Effect == DeckKind).Sum((DeclarativeEffect e) => e.Amount ?? 1), Combat.DeckChoosePool(state, card.Def).Count) : 0);
		HandNeed = Math.Min(card.Def.Effects.Where((DeclarativeEffect e) => e.Trigger == "onPlay" && e.Effect == "upgradeInHand").Sum((DeclarativeEffect e) => e.Amount ?? 1), state.Player.Hand.Count((CardInstance c) => c.Uid != card.Uid && Upgrade.CanUpgradeInHand(c)));
		NeedRetrieve = card.Def.Effects.Any((DeclarativeEffect e) =>
		{
			string effect = e.Effect;
			return (effect == "retrieveFromExhaust" || effect == "playFromExhaust") ? true : false;
		});
		NeedPermanent = Combat.CardChoosesDoll(card.Def) || card.Def.Effects.Any((DeclarativeEffect e) => e.Trigger == "onPlay" && e.Effect == "sacrificeRetainer");
	}

	public string? Next(GameState state)
	{
		if ((Card.Def.Modes?.Count ?? 0) > 0 && !ModeIndex.HasValue)
		{
			return "mode";
		}
		if (Card.Def.ImpulseCost.GetValueOrDefault() > 0 && FlowRules.CanSkipKindle(Card.Def) && !Kindle.HasValue)
		{
			return "kindle";
		}
		if (Exhaust.Count < ImpulseNeed)
		{
			return "impulse";
		}
		if (Discard.Count < Card.Def.DiscardCost.GetValueOrDefault())
		{
			return "discard";
		}
		if (Exhaust.Count < Card.Def.ExhaustCost.GetValueOrDefault() + ImpulseNeed)
		{
			return "exhaust";
		}
		if (NeedRetrieve && RetrieveUid == null)
		{
			return "retrieve";
		}
		if (Deck.Count < DeckNeed)
		{
			return "deck";
		}
		if (Hand.Count < HandNeed)
		{
			return "hand";
		}
		if (NeedPermanent && PermanentUid == null)
		{
			return "permanent";
		}
		if (Effects.CardNeedsTarget(Card, ModeIndex) && state.Enemies.Count((EnemyState e) => e.Hp > 0) > 1 && !TargetIndex.HasValue)
		{
			return "target";
		}
		return null;
	}

	public IReadOnlyList<CardInstance> Pool(GameState state, string need)
	{
		switch (need)
		{
		case "impulse":
			return (from c in FlowRules.ImpulseFuel(state, Card)
				where !Discard.Contains(c.Uid) && !Exhaust.Contains(c.Uid)
				select c).ToArray();
		case "discard":
		case "exhaust":
			return state.Player.Hand.Where((CardInstance c) => c.Uid != Card.Uid && !Discard.Contains(c.Uid) && !Exhaust.Contains(c.Uid)).ToList();
		case "retrieve":
			return state.Player.ExhaustPile;
		case "deck":
			return (from c in Combat.DeckChoosePool(state, Card.Def)
				where !Deck.Contains(c.Uid)
				select c).OrderBy((CardInstance c) => c.Def.Name, StringComparer.Ordinal).ToList();
		case "hand":
			return state.Player.Hand.Where((CardInstance c) => c.Uid != Card.Uid && Upgrade.CanUpgradeInHand(c) && !Hand.Contains(c.Uid)).ToList();
		case "permanent":
			return state.Player.Permanents.Where((CardInstance c) => Effects.IsDoll(c) && (!Card.Def.Effects.Any((DeclarativeEffect e) =>
			{
				bool flag = e.Trigger == "onPlay";
				if (flag)
				{
					string effect = e.Effect;
					flag = ((effect == "extendRetainerLife" || effect == "persistRetainer") ? true : false);
				}
				return flag;
			}) || Effects.DollLifeTotal(c).HasValue)).ToList();
		default:
			return Array.Empty<CardInstance>();
		}
	}

	public void Pick(string need, string uid)
	{
		List<string> list;
		bool flag;
		switch (need)
		{
		case "retrieve":
			RetrieveUid = uid;
			return;
		case "permanent":
			PermanentUid = uid;
			return;
		case "discard":
			list = Discard;
			break;
		case "exhaust":
		case "impulse":
			flag = true;
			goto IL_0062;
		default:
			{
				flag = false;
				goto IL_0062;
			}
			IL_0062:
			if (flag)
			{
				list = Exhaust;
			}
			else
			{
				list = ((need == "deck") ? Deck : Hand);
			}
			break;
		}
		list.Add(uid);
	}

	public Command_PlayCard Command()
	{
		return new Command_PlayCard
		{
			CardUid = Card.Uid,
			ModeIndex = ModeIndex,
			XAmount = null,
			TargetIndex = TargetIndex,
			RetrieveUid = RetrieveUid,
			PermanentUid = PermanentUid,
			DiscardUids = ((Discard.Count > 0) ? Discard.ToArray() : null),
			ExhaustUids = ((Exhaust.Count > 0) ? Exhaust.ToArray() : null),
			DeckUids = ((Deck.Count > 0) ? Deck.ToArray() : null),
			HandUids = ((Hand.Count > 0) ? Hand.ToArray() : null)
		};
	}
}
