using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CascadeRetargeted : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("fromIndex")]
	public int FromIndex { get; init; }

	[JsonProperty("toIndex")]
	public int ToIndex { get; init; }

	public const string TypeTag = "CascadeRetargeted";

	public GameEvent_CascadeRetargeted()
	{
		Type = "CascadeRetargeted";
	}
}
