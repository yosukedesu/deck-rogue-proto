using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardSealed : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "CardSealed";

	public GameEvent_CardSealed()
	{
		Type = "CardSealed";
	}
}
