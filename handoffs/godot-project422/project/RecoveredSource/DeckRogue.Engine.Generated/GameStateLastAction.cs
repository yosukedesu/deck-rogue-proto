using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameStateLastAction
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("kind")]
	public string Kind { get; init; }

	[JsonProperty("hpLoss")]
	public int HpLoss { get; init; }

	[JsonProperty("actual")]
	public int Actual { get; init; }
}
