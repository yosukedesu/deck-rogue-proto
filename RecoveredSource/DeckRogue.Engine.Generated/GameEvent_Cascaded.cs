using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_Cascaded : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("hitId", NullValueHandling = NullValueHandling.Ignore)]
	public string? HitId { get; init; }

	[JsonProperty("revealed")]
	public IReadOnlyList<string> Revealed { get; init; }

	public const string TypeTag = "Cascaded";

	public GameEvent_Cascaded()
	{
		Type = "Cascaded";
	}
}
