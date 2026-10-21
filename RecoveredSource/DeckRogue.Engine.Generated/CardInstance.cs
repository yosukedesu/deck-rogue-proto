using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record CardInstance
{
	[JsonProperty("scaldFresh", NullValueHandling = NullValueHandling.Ignore)]
	public bool? ScaldFresh { get; init; }

	[JsonProperty("expiresAfterBattles", NullValueHandling = NullValueHandling.Ignore)]
	public int? ExpiresAfterBattles { get; init; }

	[JsonProperty("uid")]
	public string Uid { get; init; }

	[JsonProperty("def")]
	public CardDef Def { get; init; }

	[JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Token { get; init; }

	[JsonProperty("freeThisCombat", NullValueHandling = NullValueHandling.Ignore)]
	public bool? FreeThisCombat { get; init; }

	[JsonProperty("keptImpulse", NullValueHandling = NullValueHandling.Ignore)]
	public bool? KeptImpulse { get; init; }

	[JsonProperty("growBonus", NullValueHandling = NullValueHandling.Ignore)]
	public int? GrowBonus { get; init; }

	[JsonProperty("setTurn", NullValueHandling = NullValueHandling.Ignore)]
	public int? SetTurn { get; init; }

	[JsonProperty("trapLifeBonus", NullValueHandling = NullValueHandling.Ignore)]
	public int? TrapLifeBonus { get; init; }

	[JsonProperty("retainDiscount", NullValueHandling = NullValueHandling.Ignore)]
	public int? RetainDiscount { get; init; }

	[JsonProperty("innate", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Innate { get; init; }

	[JsonProperty("triggerCounts", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyDictionary<string, int>? TriggerCounts { get; init; }

	[JsonProperty("enteredTurn", NullValueHandling = NullValueHandling.Ignore)]
	public int? EnteredTurn { get; init; }

	[JsonProperty("lifeBonus", NullValueHandling = NullValueHandling.Ignore)]
	public int? LifeBonus { get; init; }

	[JsonProperty("lifePersist", NullValueHandling = NullValueHandling.Ignore)]
	public bool? LifePersist { get; init; }

	[JsonProperty("turnTriggerCounts", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyDictionary<string, int>? TurnTriggerCounts { get; init; }
}
