using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_TurnEnded : GameEvent
{
	[JsonProperty("turn")]
	public int Turn { get; init; }

	[JsonProperty("unplayed", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? Unplayed { get; init; }

	public const string TypeTag = "TurnEnded";

	public GameEvent_TurnEnded()
	{
		Type = "TurnEnded";
	}
}
