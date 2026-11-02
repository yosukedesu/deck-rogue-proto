using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_BurrowBroken : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	public const string TypeTag = "BurrowBroken";

	public GameEvent_BurrowBroken()
	{
		Type = "BurrowBroken";
	}
}
