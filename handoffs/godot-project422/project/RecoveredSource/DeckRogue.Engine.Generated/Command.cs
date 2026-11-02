using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public abstract record Command
{
	[JsonProperty("type")]
	public string Type { get; init; }
}
