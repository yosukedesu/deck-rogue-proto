using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CombatEnded : GameEvent
{
	[JsonProperty("result")]
	public string Result { get; init; }

	public const string TypeTag = "CombatEnded";

	public GameEvent_CombatEnded()
	{
		Type = "CombatEnded";
	}
}
