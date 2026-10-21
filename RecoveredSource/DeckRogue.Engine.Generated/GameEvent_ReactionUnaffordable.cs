using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ReactionUnaffordable : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("cost")]
	public int Cost { get; init; }

	[JsonProperty("energy")]
	public int Energy { get; init; }

	public const string TypeTag = "ReactionUnaffordable";

	public GameEvent_ReactionUnaffordable()
	{
		Type = "ReactionUnaffordable";
	}
}
