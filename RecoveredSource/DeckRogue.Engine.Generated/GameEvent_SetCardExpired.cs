using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_SetCardExpired : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("to")]
	public string To { get; init; }

	public const string TypeTag = "SetCardExpired";

	public GameEvent_SetCardExpired()
	{
		Type = "SetCardExpired";
	}
}
