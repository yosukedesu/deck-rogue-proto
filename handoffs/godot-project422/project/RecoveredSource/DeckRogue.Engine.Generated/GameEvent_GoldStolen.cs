using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_GoldStolen : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "GoldStolen";

	public GameEvent_GoldStolen()
	{
		Type = "GoldStolen";
	}
}
