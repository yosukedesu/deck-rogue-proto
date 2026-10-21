using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record Command_ConfirmReaction : Command
{
	[JsonProperty("fire")]
	public bool Fire { get; init; }

	[JsonProperty("cardUid", NullValueHandling = NullValueHandling.Ignore)]
	public string? CardUid { get; init; }

	public const string TypeTag = "ConfirmReaction";

	public Command_ConfirmReaction()
	{
		Type = "ConfirmReaction";
	}
}
