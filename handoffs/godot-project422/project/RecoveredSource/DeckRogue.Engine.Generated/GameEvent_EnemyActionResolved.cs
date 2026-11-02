using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_EnemyActionResolved : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("kind")]
	public string Kind { get; init; }

	[JsonProperty("hpLoss")]
	public int HpLoss { get; init; }

	[JsonProperty("actual")]
	public int Actual { get; init; }

	public const string TypeTag = "EnemyActionResolved";

	public GameEvent_EnemyActionResolved()
	{
		Type = "EnemyActionResolved";
	}
}
