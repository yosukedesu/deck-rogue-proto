using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_NecroPlayed : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "NecroPlayed";

	public GameEvent_NecroPlayed()
	{
		Type = "NecroPlayed";
	}
}
