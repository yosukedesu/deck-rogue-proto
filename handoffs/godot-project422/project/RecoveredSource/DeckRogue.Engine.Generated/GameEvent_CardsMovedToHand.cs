using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardsMovedToHand : GameEvent
{
	[JsonProperty("cardIds")]
	public IReadOnlyList<string> CardIds { get; init; }

	[JsonProperty("from")]
	public string From { get; init; }

	public const string TypeTag = "CardsMovedToHand";

	public GameEvent_CardsMovedToHand()
	{
		Type = "CardsMovedToHand";
	}
}
