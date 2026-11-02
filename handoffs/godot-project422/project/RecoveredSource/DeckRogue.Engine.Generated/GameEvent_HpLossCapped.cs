using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_HpLossCapped : GameEvent
{
	[JsonProperty("left")]
	public int Left { get; init; }

	public const string TypeTag = "HpLossCapped";

	public GameEvent_HpLossCapped()
	{
		Type = "HpLossCapped";
	}
}
