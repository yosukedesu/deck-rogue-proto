using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_RetainersDuplicated : GameEvent
{
	[JsonProperty("count")]
	public int Count { get; init; }

	public const string TypeTag = "RetainersDuplicated";

	public GameEvent_RetainersDuplicated()
	{
		Type = "RetainersDuplicated";
	}
}
