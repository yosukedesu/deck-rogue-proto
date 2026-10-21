using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_StatusInflicted : GameEvent
{
	[JsonProperty("status")]
	public string Status { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "StatusInflicted";

	public GameEvent_StatusInflicted()
	{
		Type = "StatusInflicted";
	}
}
