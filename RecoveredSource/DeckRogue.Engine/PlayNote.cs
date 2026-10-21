using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed class PlayNote
{
	[JsonProperty("at")]
	public string At = "";

	[JsonProperty("context")]
	public string Context = "";

	[JsonProperty("text")]
	public string Text = "";
}
