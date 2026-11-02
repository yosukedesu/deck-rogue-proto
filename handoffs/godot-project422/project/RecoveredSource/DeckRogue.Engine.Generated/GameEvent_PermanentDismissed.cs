using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_PermanentDismissed : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("uid")]
	public string Uid { get; init; }

	public const string TypeTag = "PermanentDismissed";

	public GameEvent_PermanentDismissed()
	{
		Type = "PermanentDismissed";
	}
}
