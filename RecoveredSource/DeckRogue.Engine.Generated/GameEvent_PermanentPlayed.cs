using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_PermanentPlayed : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "PermanentPlayed";

	public GameEvent_PermanentPlayed()
	{
		Type = "PermanentPlayed";
	}
}
