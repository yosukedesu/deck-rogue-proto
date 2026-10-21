using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CombatStarted : GameEvent
{
	[JsonProperty("enemyId")]
	public string EnemyId { get; init; }

	public const string TypeTag = "CombatStarted";

	public GameEvent_CombatStarted()
	{
		Type = "CombatStarted";
	}
}
