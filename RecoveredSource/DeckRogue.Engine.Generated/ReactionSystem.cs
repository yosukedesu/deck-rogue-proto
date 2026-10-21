using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record ReactionSystem
{
	[JsonProperty("mode")]
	public string Mode { get; init; }
}
