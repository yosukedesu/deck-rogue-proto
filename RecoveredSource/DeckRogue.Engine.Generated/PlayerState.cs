using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record PlayerState
{
	[JsonProperty("hp")]
	public int Hp { get; init; }

	[JsonProperty("maxHp")]
	public int MaxHp { get; init; }

	[JsonProperty("block")]
	public int Block { get; init; }

	[JsonProperty("energy")]
	public int Energy { get; init; }

	[JsonProperty("energyMax")]
	public int EnergyMax { get; init; }

	[JsonProperty("energyMaxAtTurnStart")]
	public int EnergyMaxAtTurnStart { get; init; }

	[JsonProperty("drawPerTurn")]
	public int DrawPerTurn { get; init; }

	[JsonProperty("hand")]
	public IReadOnlyList<CardInstance> Hand { get; init; }

	[JsonProperty("drawPile")]
	public IReadOnlyList<CardInstance> DrawPile { get; init; }

	[JsonProperty("discardPile")]
	public IReadOnlyList<CardInstance> DiscardPile { get; init; }

	[JsonProperty("setCards")]
	public IReadOnlyList<CardInstance> SetCards { get; init; }

	[JsonProperty("setSlots")]
	public int SetSlots { get; init; }

	[JsonProperty("permanents")]
	public IReadOnlyList<CardInstance> Permanents { get; init; }

	[JsonProperty("exhaustPile")]
	public IReadOnlyList<CardInstance> ExhaustPile { get; init; }

	[JsonProperty("growth")]
	public int Growth { get; init; }

	[JsonProperty("momentum")]
	public int Momentum { get; init; }

	[JsonProperty("iceBlock")]
	public int IceBlock { get; init; }

	[JsonProperty("cardsPlayedThisTurn")]
	public int CardsPlayedThisTurn { get; init; }

	[JsonProperty("playsThisTurn", NullValueHandling = NullValueHandling.Ignore)]
	public int? PlaysThisTurn { get; init; }

	[JsonProperty("setsThisTurn", NullValueHandling = NullValueHandling.Ignore)]
	public int? SetsThisTurn { get; init; }

	[JsonProperty("cardsPlayedTotal")]
	public int CardsPlayedTotal { get; init; }

	[JsonProperty("aether")]
	public int Aether { get; init; }

	[JsonProperty("light", NullValueHandling = NullValueHandling.Ignore)]
	public int? Light { get; init; }

	[JsonProperty("healsThisCombat")]
	public int HealsThisCombat { get; init; }

	[JsonProperty("healsThisTurn", NullValueHandling = NullValueHandling.Ignore)]
	public int? HealsThisTurn { get; init; }

	[JsonProperty("nextCardDiscount")]
	public int NextCardDiscount { get; init; }

	[JsonProperty("impulseUids")]
	public IReadOnlyList<string> ImpulseUids { get; init; }

	[JsonProperty("weak")]
	public int Weak { get; init; }

	[JsonProperty("vulnerable")]
	public int Vulnerable { get; init; }

	[JsonProperty("vulnerableFresh", NullValueHandling = NullValueHandling.Ignore)]
	public bool? VulnerableFresh { get; init; }

	[JsonProperty("scaldsThisCombat", NullValueHandling = NullValueHandling.Ignore)]
	public int? ScaldsThisCombat { get; init; }

	[JsonProperty("mist", NullValueHandling = NullValueHandling.Ignore)]
	public int? Mist { get; init; }

	[JsonProperty("slow", NullValueHandling = NullValueHandling.Ignore)]
	public int? Slow { get; init; }

	[JsonProperty("slowFresh", NullValueHandling = NullValueHandling.Ignore)]
	public bool? SlowFresh { get; init; }

	[JsonProperty("frail")]
	public int Frail { get; init; }

	[JsonProperty("restrain")]
	public int Restrain { get; init; }

	[JsonProperty("selfHpLost")]
	public int SelfHpLost { get; init; }

	[JsonProperty("randomPlayedThisCombat")]
	public int RandomPlayedThisCombat { get; init; }

	[JsonProperty("sparksPlayedThisCombat", NullValueHandling = NullValueHandling.Ignore)]
	public int? SparksPlayedThisCombat { get; init; }

	[JsonProperty("scriedThisCombat", NullValueHandling = NullValueHandling.Ignore)]
	public int? ScriedThisCombat { get; init; }

	[JsonProperty("scryDiscardedThisCombat", NullValueHandling = NullValueHandling.Ignore)]
	public int? ScryDiscardedThisCombat { get; init; }

	[JsonProperty("cascadesThisCombat", NullValueHandling = NullValueHandling.Ignore)]
	public int? CascadesThisCombat { get; init; }

	[JsonProperty("trapsFiredThisCombat", NullValueHandling = NullValueHandling.Ignore)]
	public int? TrapsFiredThisCombat { get; init; }

	[JsonProperty("damageTakenLastEnemyPhase")]
	public int DamageTakenLastEnemyPhase { get; init; }

	[JsonProperty("weakFreshThisPhase", NullValueHandling = NullValueHandling.Ignore)]
	public int? WeakFreshThisPhase { get; init; }

	[JsonProperty("attacksPlayedThisTurn", NullValueHandling = NullValueHandling.Ignore)]
	public int? AttacksPlayedThisTurn { get; init; }

	[JsonProperty("attacksReceivedThisPhase", NullValueHandling = NullValueHandling.Ignore)]
	public int? AttacksReceivedThisPhase { get; init; }

	[JsonProperty("perfectBlockLastPhase", NullValueHandling = NullValueHandling.Ignore)]
	public bool? PerfectBlockLastPhase { get; init; }

	[JsonProperty("spellEchoes")]
	public int SpellEchoes { get; init; }

	[JsonProperty("artifact", NullValueHandling = NullValueHandling.Ignore)]
	public int? Artifact { get; init; }

	[JsonProperty("hpLostThisTurn", NullValueHandling = NullValueHandling.Ignore)]
	public int? HpLostThisTurn { get; init; }
}
