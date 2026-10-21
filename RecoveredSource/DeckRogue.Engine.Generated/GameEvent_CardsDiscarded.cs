using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardsDiscarded : GameEvent
{
	[JsonProperty("cardIds")]
	public IReadOnlyList<string> CardIds { get; init; }

	public const string TypeTag = "CardsDiscarded";

	public GameEvent_CardsDiscarded()
	{
		Type = "CardsDiscarded";
	}
}
