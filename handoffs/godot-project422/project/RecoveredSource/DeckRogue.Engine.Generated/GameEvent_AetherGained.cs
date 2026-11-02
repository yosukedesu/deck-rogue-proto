using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_AetherGained : GameEvent
{
	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "AetherGained";

	public GameEvent_AetherGained()
	{
		Type = "AetherGained";
	}
}
