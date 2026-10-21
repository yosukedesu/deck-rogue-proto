using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record DamagePresentationSource
{
	[JsonProperty("kind")]
	public string Kind { get; init; }

	[JsonProperty("definitionId")]
	public string DefinitionId { get; init; }

	[JsonProperty("uid", NullValueHandling = NullValueHandling.Ignore)]
	public string? Uid { get; init; }
}
