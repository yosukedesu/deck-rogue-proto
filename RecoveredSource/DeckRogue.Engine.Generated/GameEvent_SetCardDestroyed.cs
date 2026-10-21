using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_SetCardDestroyed : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "SetCardDestroyed";

	public GameEvent_SetCardDestroyed()
	{
		Type = "SetCardDestroyed";
	}
}
