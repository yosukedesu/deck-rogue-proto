using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_StrengthGained : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	[JsonProperty("reason", NullValueHandling = NullValueHandling.Ignore)]
	public string? Reason { get; init; }

	public const string TypeTag = "StrengthGained";

	public GameEvent_StrengthGained()
	{
		Type = "StrengthGained";
	}
}
