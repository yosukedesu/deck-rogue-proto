using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_TokenDestroyed : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("uid", NullValueHandling = NullValueHandling.Ignore)]
	public string? Uid { get; init; }

	public const string TypeTag = "TokenDestroyed";

	public GameEvent_TokenDestroyed()
	{
		Type = "TokenDestroyed";
	}
}
