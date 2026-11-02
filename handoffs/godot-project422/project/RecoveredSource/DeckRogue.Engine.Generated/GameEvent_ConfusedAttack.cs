using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ConfusedAttack : GameEvent
{
	[JsonProperty("presentationEnemyAction", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyActionPresentation? PresentationEnemyAction { get; init; }

	[JsonProperty("presentationReceipt", NullValueHandling = NullValueHandling.Ignore)]
	public CombatPresentationReceipt? PresentationReceipt { get; init; }

	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("targetIndex")]
	public int TargetIndex { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	[JsonProperty("hpLoss")]
	public int HpLoss { get; init; }

	[JsonProperty("blocked")]
	public int Blocked { get; init; }

	public const string TypeTag = "ConfusedAttack";

	public GameEvent_ConfusedAttack()
	{
		Type = "ConfusedAttack";
	}
}
