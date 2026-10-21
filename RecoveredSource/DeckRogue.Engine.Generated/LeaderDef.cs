using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record LeaderDef
{
	[JsonProperty("id")]
	public string Id { get; init; }

	[JsonProperty("name")]
	public string Name { get; init; }

	[JsonProperty("colors")]
	public IReadOnlyList<string> Colors { get; init; }

	[JsonProperty("maxHp")]
	public int MaxHp { get; init; }

	[JsonProperty("drawPerTurn")]
	public int DrawPerTurn { get; init; }

	[JsonProperty("energyMax")]
	public int EnergyMax { get; init; }

	[JsonProperty("rewardChoices")]
	public int RewardChoices { get; init; }

	[JsonProperty("runDeckId")]
	public string RunDeckId { get; init; }

	[JsonProperty("runDeckChoices", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? RunDeckChoices { get; init; }

	[JsonProperty("sprite")]
	public string Sprite { get; init; }

	[JsonProperty("description")]
	public string Description { get; init; }

	[JsonProperty("passive")]
	public IReadOnlyList<DeclarativeEffect> Passive { get; init; }

	[JsonProperty("setSlots", NullValueHandling = NullValueHandling.Ignore)]
	public int? SetSlots { get; init; }
}
