using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record Command_SetCard : Command
{
	[JsonProperty("cardUid")]
	public string CardUid { get; init; }

	public const string TypeTag = "SetCard";

	public Command_SetCard()
	{
		Type = "SetCard";
	}
}
