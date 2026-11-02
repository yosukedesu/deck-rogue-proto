using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_RetainerExpired : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("uid")]
	public string Uid { get; init; }

	public const string TypeTag = "RetainerExpired";

	public GameEvent_RetainerExpired()
	{
		Type = "RetainerExpired";
	}
}
