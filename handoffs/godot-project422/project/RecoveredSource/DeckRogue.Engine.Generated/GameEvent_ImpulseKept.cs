using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ImpulseKept : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "ImpulseKept";

	public GameEvent_ImpulseKept()
	{
		Type = "ImpulseKept";
	}
}
