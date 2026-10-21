using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ReactionWhiffed : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "ReactionWhiffed";

	public GameEvent_ReactionWhiffed()
	{
		Type = "ReactionWhiffed";
	}
}
