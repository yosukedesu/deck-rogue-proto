using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_LightGained : GameEvent
{
	[JsonProperty("amount")]
	public int Amount { get; init; }

	[JsonProperty("source")]
	public string Source { get; init; }

	[JsonProperty("sourceUid", NullValueHandling = NullValueHandling.Ignore)]
	public string? SourceUid { get; init; }

	public const string TypeTag = "LightGained";

	public GameEvent_LightGained()
	{
		Type = "LightGained";
	}
}
