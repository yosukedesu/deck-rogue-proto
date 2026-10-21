using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record DeclarativeEffect
{
	[JsonProperty("trigger")]
	public string Trigger { get; init; }

	[JsonProperty("condition", NullValueHandling = NullValueHandling.Ignore)]
	public EffectCondition? Condition { get; init; }

	[JsonProperty("every", NullValueHandling = NullValueHandling.Ignore)]
	public int? Every { get; init; }

	[JsonProperty("everyScope", NullValueHandling = NullValueHandling.Ignore)]
	public string? EveryScope { get; init; }

	[JsonProperty("once", NullValueHandling = NullValueHandling.Ignore)]
	public string? Once { get; init; }

	[JsonProperty("growthMultiplier", NullValueHandling = NullValueHandling.Ignore)]
	public double? GrowthMultiplier { get; init; }

	[JsonProperty("momentumMultiplier", NullValueHandling = NullValueHandling.Ignore)]
	public double? MomentumMultiplier { get; init; }

	[JsonProperty("volleyHits", NullValueHandling = NullValueHandling.Ignore)]
	public int? VolleyHits { get; init; }

	[JsonProperty("effect")]
	public string Effect { get; init; }

	[JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
	public int? Amount { get; init; }

	[JsonProperty("amountMax", NullValueHandling = NullValueHandling.Ignore)]
	public int? AmountMax { get; init; }

	[JsonProperty("pierce", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Pierce { get; init; }

	[JsonProperty("xHits", NullValueHandling = NullValueHandling.Ignore)]
	public bool? XHits { get; init; }

	[JsonProperty("spendBlock", NullValueHandling = NullValueHandling.Ignore)]
	public bool? SpendBlock { get; init; }

	[JsonProperty("target", NullValueHandling = NullValueHandling.Ignore)]
	public string? Target { get; init; }

	[JsonProperty("summonId", NullValueHandling = NullValueHandling.Ignore)]
	public string? SummonId { get; init; }

	[JsonProperty("cardType", NullValueHandling = NullValueHandling.Ignore)]
	public string? CardType { get; init; }

	[JsonProperty("exhaustThreshold", NullValueHandling = NullValueHandling.Ignore)]
	public int? ExhaustThreshold { get; init; }

	[JsonProperty("scriptId", NullValueHandling = NullValueHandling.Ignore)]
	public string? ScriptId { get; init; }
}
