using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyDefAura
{
	[JsonProperty("cardType", NullValueHandling = NullValueHandling.Ignore)]
	public string? CardType { get; init; }

	[JsonProperty("costUp")]
	public int CostUp { get; init; }

	[JsonProperty("attacksOnly", NullValueHandling = NullValueHandling.Ignore)]
	public bool? AttacksOnly { get; init; }
}
