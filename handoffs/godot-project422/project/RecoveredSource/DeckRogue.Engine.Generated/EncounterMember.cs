using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EncounterMember
{
	[JsonProperty("enemyId")]
	public string EnemyId { get; init; }

	[JsonProperty("hpScale", NullValueHandling = NullValueHandling.Ignore)]
	public double? HpScale { get; init; }

	[JsonProperty("strength", NullValueHandling = NullValueHandling.Ignore)]
	public int? Strength { get; init; }

	[JsonProperty("start", NullValueHandling = NullValueHandling.Ignore)]
	public string? Start { get; init; }

	[JsonProperty("noReactTable", NullValueHandling = NullValueHandling.Ignore)]
	public bool? NoReactTable { get; init; }
}
