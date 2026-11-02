using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_SpellEchoed : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "SpellEchoed";

	public GameEvent_SpellEchoed()
	{
		Type = "SpellEchoed";
	}
}
