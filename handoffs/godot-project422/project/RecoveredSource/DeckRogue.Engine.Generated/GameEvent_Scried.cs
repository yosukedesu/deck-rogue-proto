using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_Scried : GameEvent
{
	[JsonProperty("looked")]
	public IReadOnlyList<string> Looked { get; init; }

	[JsonProperty("discarded")]
	public IReadOnlyList<string> Discarded { get; init; }

	public const string TypeTag = "Scried";

	public GameEvent_Scried()
	{
		Type = "Scried";
	}
}
