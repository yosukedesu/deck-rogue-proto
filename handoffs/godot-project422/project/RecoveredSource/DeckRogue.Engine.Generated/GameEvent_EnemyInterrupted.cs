using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_EnemyInterrupted : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("trigger")]
	public string Trigger { get; init; }

	[JsonProperty("replaced")]
	public bool Replaced { get; init; }

	[JsonProperty("before", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyIntent? Before { get; init; }

	[JsonProperty("after", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyIntent? After { get; init; }

	public const string TypeTag = "EnemyInterrupted";

	public GameEvent_EnemyInterrupted()
	{
		Type = "EnemyInterrupted";
	}
}
