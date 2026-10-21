using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_BurnDischarged : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "BurnDischarged";

	public GameEvent_BurnDischarged()
	{
		Type = "BurnDischarged";
	}
}
