using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public abstract record GameEvent
{
	[JsonProperty("type")]
	public string Type { get; init; }
}
