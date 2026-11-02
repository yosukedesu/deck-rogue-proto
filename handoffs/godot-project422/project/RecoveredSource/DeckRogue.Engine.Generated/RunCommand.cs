using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public abstract record RunCommand
{
	[JsonProperty("type")]
	public string Type { get; init; }
}
