using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record Command_ReactManual : Command
{
	[JsonProperty("cardUid")]
	public string CardUid { get; init; }

	public const string TypeTag = "ReactManual";

	public Command_ReactManual()
	{
		Type = "ReactManual";
	}
}
