using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_RetainerSacrificed : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("uid", NullValueHandling = NullValueHandling.Ignore)]
	public string? Uid { get; init; }

	public const string TypeTag = "RetainerSacrificed";

	public GameEvent_RetainerSacrificed()
	{
		Type = "RetainerSacrificed";
	}
}
