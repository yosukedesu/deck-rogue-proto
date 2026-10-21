using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_RetainerCopied : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("uid")]
	public string Uid { get; init; }

	[JsonProperty("fromUid")]
	public string FromUid { get; init; }

	public const string TypeTag = "RetainerCopied";

	public GameEvent_RetainerCopied()
	{
		Type = "RetainerCopied";
	}
}
