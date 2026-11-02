using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ImpulseReturned : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "ImpulseReturned";

	public GameEvent_ImpulseReturned()
	{
		Type = "ImpulseReturned";
	}
}
