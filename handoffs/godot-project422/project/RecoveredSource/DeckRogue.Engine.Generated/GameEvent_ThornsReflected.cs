using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ThornsReflected : GameEvent
{
	[JsonProperty("presentationReceipt", NullValueHandling = NullValueHandling.Ignore)]
	public CombatPresentationReceipt? PresentationReceipt { get; init; }

	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	[JsonProperty("hpLoss")]
	public int HpLoss { get; init; }

	public const string TypeTag = "ThornsReflected";

	public GameEvent_ThornsReflected()
	{
		Type = "ThornsReflected";
	}
}
