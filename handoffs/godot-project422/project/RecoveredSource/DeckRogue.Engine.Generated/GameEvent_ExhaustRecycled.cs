using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ExhaustRecycled : GameEvent
{
	[JsonProperty("count")]
	public int Count { get; init; }

	public const string TypeTag = "ExhaustRecycled";

	public GameEvent_ExhaustRecycled()
	{
		Type = "ExhaustRecycled";
	}
}
