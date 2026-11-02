using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ActionNegated : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	public const string TypeTag = "ActionNegated";

	public GameEvent_ActionNegated()
	{
		Type = "ActionNegated";
	}
}
