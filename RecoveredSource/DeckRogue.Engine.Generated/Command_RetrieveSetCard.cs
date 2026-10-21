using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record Command_RetrieveSetCard : Command
{
	[JsonProperty("cardUid")]
	public string CardUid { get; init; }

	public const string TypeTag = "RetrieveSetCard";

	public Command_RetrieveSetCard()
	{
		Type = "RetrieveSetCard";
	}
}
