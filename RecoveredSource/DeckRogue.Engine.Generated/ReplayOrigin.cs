using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record ReplayOrigin
{
	[JsonProperty("kind")]
	public string Kind { get; init; }

	[JsonProperty("seed")]
	public int Seed { get; init; }

	[JsonProperty("leaderId")]
	public string LeaderId { get; init; }

	[JsonProperty("deckId", NullValueHandling = NullValueHandling.Ignore)]
	public string? DeckId { get; init; }

	[JsonProperty("difficulty", NullValueHandling = NullValueHandling.Ignore)]
	public int? Difficulty { get; init; }

	[JsonProperty("setAnyCards", NullValueHandling = NullValueHandling.Ignore)]
	public bool? SetAnyCards { get; init; }

	[JsonProperty("checkpoint", NullValueHandling = NullValueHandling.Ignore)]
	public ReplayOriginCheckpoint? Checkpoint { get; init; }
}
