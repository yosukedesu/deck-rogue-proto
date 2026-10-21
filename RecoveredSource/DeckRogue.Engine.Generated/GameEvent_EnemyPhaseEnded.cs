using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_EnemyPhaseEnded : GameEvent
{
	[JsonProperty("turn")]
	public int Turn { get; init; }

	public const string TypeTag = "EnemyPhaseEnded";

	public GameEvent_EnemyPhaseEnded()
	{
		Type = "EnemyPhaseEnded";
	}
}
