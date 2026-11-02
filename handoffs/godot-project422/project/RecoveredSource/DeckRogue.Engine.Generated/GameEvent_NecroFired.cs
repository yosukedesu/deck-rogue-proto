using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_NecroFired : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "NecroFired";

	public GameEvent_NecroFired()
	{
		Type = "NecroFired";
	}
}
