using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EventChoiceDef
{
	[JsonProperty("label")]
	public string Label { get; init; }

	[JsonProperty("gold", NullValueHandling = NullValueHandling.Ignore)]
	public int? Gold { get; init; }

	[JsonProperty("hp", NullValueHandling = NullValueHandling.Ignore)]
	public int? Hp { get; init; }

	[JsonProperty("maxHp", NullValueHandling = NullValueHandling.Ignore)]
	public int? MaxHp { get; init; }

	[JsonProperty("wounds", NullValueHandling = NullValueHandling.Ignore)]
	public int? Wounds { get; init; }

	[JsonProperty("brands", NullValueHandling = NullValueHandling.Ignore)]
	public int? Brands { get; init; }

	[JsonProperty("timedCurses", NullValueHandling = NullValueHandling.Ignore)]
	public int? TimedCurses { get; init; }

	[JsonProperty("addRandomCards", NullValueHandling = NullValueHandling.Ignore)]
	public int? AddRandomCards { get; init; }

	[JsonProperty("relic", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Relic { get; init; }

	[JsonProperty("removeCard", NullValueHandling = NullValueHandling.Ignore)]
	public bool? RemoveCard { get; init; }

	[JsonProperty("upgradeCard", NullValueHandling = NullValueHandling.Ignore)]
	public bool? UpgradeCard { get; init; }

	[JsonProperty("requireGold", NullValueHandling = NullValueHandling.Ignore)]
	public int? RequireGold { get; init; }

	[JsonProperty("gamble", NullValueHandling = NullValueHandling.Ignore)]
	public EventChoiceDefGamble? Gamble { get; init; }

	[JsonProperty("hpRatio", NullValueHandling = NullValueHandling.Ignore)]
	public double? HpRatio { get; init; }

	[JsonProperty("transformCard", NullValueHandling = NullValueHandling.Ignore)]
	public bool? TransformCard { get; init; }

	[JsonProperty("duplicateCard", NullValueHandling = NullValueHandling.Ignore)]
	public bool? DuplicateCard { get; init; }

	[JsonProperty("upgradeRandomCards", NullValueHandling = NullValueHandling.Ignore)]
	public int? UpgradeRandomCards { get; init; }

	[JsonProperty("removeAllWounds", NullValueHandling = NullValueHandling.Ignore)]
	public bool? RemoveAllWounds { get; init; }

	[JsonProperty("unexhaustCard", NullValueHandling = NullValueHandling.Ignore)]
	public bool? UnexhaustCard { get; init; }

	[JsonProperty("relicId", NullValueHandling = NullValueHandling.Ignore)]
	public string? RelicId { get; init; }

	[JsonProperty("gears", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? Gears { get; init; }

	[JsonProperty("mana", NullValueHandling = NullValueHandling.Ignore)]
	public int? Mana { get; init; }

	[JsonProperty("addCardIds", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? AddCardIds { get; init; }
}
