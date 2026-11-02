using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ReactionTriggered : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("mode")]
	public string Mode { get; init; }

	public const string TypeTag = "ReactionTriggered";

	public GameEvent_ReactionTriggered()
	{
		Type = "ReactionTriggered";
	}
}
