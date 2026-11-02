using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_RegenBroken : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	public const string TypeTag = "RegenBroken";

	public GameEvent_RegenBroken()
	{
		Type = "RegenBroken";
	}
}
