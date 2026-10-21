using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_DamageDealt : GameEvent
{
	[JsonProperty("presentationEnemyAction", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyActionPresentation? PresentationEnemyAction { get; init; }

	[JsonProperty("presentationEnemyAttack", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyAttackPresentation? PresentationEnemyAttack { get; init; }

	[JsonProperty("presentationReceipt", NullValueHandling = NullValueHandling.Ignore)]
	public CombatPresentationReceipt? PresentationReceipt { get; init; }

	[JsonProperty("source")]
	public string Source { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	[JsonProperty("hpLoss")]
	public int HpLoss { get; init; }

	[JsonProperty("enemyIndex", NullValueHandling = NullValueHandling.Ignore)]
	public int? EnemyIndex { get; init; }

	[JsonProperty("armorCut", NullValueHandling = NullValueHandling.Ignore)]
	public int? ArmorCut { get; init; }

	[JsonProperty("turnArmorCut", NullValueHandling = NullValueHandling.Ignore)]
	public int? TurnArmorCut { get; init; }

	[JsonProperty("burrowCut", NullValueHandling = NullValueHandling.Ignore)]
	public int? BurrowCut { get; init; }

	[JsonProperty("nemesisCut", NullValueHandling = NullValueHandling.Ignore)]
	public int? NemesisCut { get; init; }

	[JsonProperty("slipperyCut", NullValueHandling = NullValueHandling.Ignore)]
	public int? SlipperyCut { get; init; }

	[JsonProperty("exposed", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Exposed { get; init; }

	[JsonProperty("pierced", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Pierced { get; init; }

	[JsonProperty("blocked", NullValueHandling = NullValueHandling.Ignore)]
	public int? Blocked { get; init; }

	[JsonProperty("nullified", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Nullified { get; init; }

	[JsonProperty("sourceUid", NullValueHandling = NullValueHandling.Ignore)]
	public string? SourceUid { get; init; }

	[JsonProperty("presentationSource", NullValueHandling = NullValueHandling.Ignore)]
	public DamagePresentationSource? PresentationSource { get; init; }

	public const string TypeTag = "DamageDealt";

	public GameEvent_DamageDealt()
	{
		Type = "DamageDealt";
	}
}
