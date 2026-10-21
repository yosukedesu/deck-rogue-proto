using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardUnsealed : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "CardUnsealed";

	public GameEvent_CardUnsealed()
	{
		Type = "CardUnsealed";
	}
}
