using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_RetainersTriggered : GameEvent
{
	[JsonProperty("count")]
	public int Count { get; init; }

	public const string TypeTag = "RetainersTriggered";

	public GameEvent_RetainersTriggered()
	{
		Type = "RetainersTriggered";
	}
}
