using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ImpulseDrawn : GameEvent
{
	[JsonProperty("count")]
	public int Count { get; init; }

	public const string TypeTag = "ImpulseDrawn";

	public GameEvent_ImpulseDrawn()
	{
		Type = "ImpulseDrawn";
	}
}
