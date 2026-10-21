using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record CombatantState
{
	[JsonProperty("hp")]
	public int Hp { get; init; }

	[JsonProperty("maxHp")]
	public int MaxHp { get; init; }

	[JsonProperty("block")]
	public int Block { get; init; }
}
