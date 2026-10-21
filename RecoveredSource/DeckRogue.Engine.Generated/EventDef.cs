using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EventDef
{
	[JsonProperty("id")]
	public string Id { get; init; }

	[JsonProperty("kind", NullValueHandling = NullValueHandling.Ignore)]
	public string? Kind { get; init; }

	[JsonProperty("act", NullValueHandling = NullValueHandling.Ignore)]
	public int? Act { get; init; }

	[JsonProperty("name")]
	public string Name { get; init; }

	[JsonProperty("sprite", NullValueHandling = NullValueHandling.Ignore)]
	public string? Sprite { get; init; }

	[JsonProperty("flavor")]
	public string Flavor { get; init; }

	[JsonProperty("choices")]
	public IReadOnlyList<EventChoiceDef> Choices { get; init; }
}
