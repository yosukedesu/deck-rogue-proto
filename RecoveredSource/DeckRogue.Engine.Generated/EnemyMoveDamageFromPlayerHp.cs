using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyMoveDamageFromPlayerHp
{
	[JsonProperty("divisor")]
	public int Divisor { get; init; }

	[JsonProperty("add")]
	public int Add { get; init; }
}
