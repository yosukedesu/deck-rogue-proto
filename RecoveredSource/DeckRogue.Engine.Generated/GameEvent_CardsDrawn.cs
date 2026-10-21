using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardsDrawn : GameEvent
{
	[JsonProperty("count")]
	public int Count { get; init; }

	[JsonProperty("cards", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? Cards { get; init; }

	public const string TypeTag = "CardsDrawn";

	public GameEvent_CardsDrawn()
	{
		Type = "CardsDrawn";
	}
}
