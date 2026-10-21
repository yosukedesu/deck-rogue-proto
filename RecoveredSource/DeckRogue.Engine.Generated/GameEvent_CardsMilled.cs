using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardsMilled : GameEvent
{
	[JsonProperty("count")]
	public int Count { get; init; }

	[JsonProperty("cardIds", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? CardIds { get; init; }

	public const string TypeTag = "CardsMilled";

	public GameEvent_CardsMilled()
	{
		Type = "CardsMilled";
	}
}
