using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed class RunChoice
{
	[JsonProperty("at")]
	public string At = "";

	[JsonProperty("text")]
	public string Text = "";

	[JsonProperty("ctx", NullValueHandling = NullValueHandling.Ignore)]
	public RunChoiceCtx? Ctx;
}
