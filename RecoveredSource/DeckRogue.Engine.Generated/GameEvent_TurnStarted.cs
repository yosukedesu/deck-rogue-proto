using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_TurnStarted : GameEvent
{
	[JsonProperty("turn")]
	public int Turn { get; init; }

	[JsonProperty("hand", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? Hand { get; init; }

	public const string TypeTag = "TurnStarted";

	public GameEvent_TurnStarted()
	{
		Type = "TurnStarted";
	}
}
