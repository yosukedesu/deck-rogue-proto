using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record Command_StartCombat : Command
{
	[JsonProperty("seed")]
	public int Seed { get; init; }

	[JsonProperty("enemyId")]
	public string EnemyId { get; init; }

	[JsonProperty("deckId", NullValueHandling = NullValueHandling.Ignore)]
	public string? DeckId { get; init; }

	[JsonProperty("cardIds", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? CardIds { get; init; }

	[JsonProperty("leaderId", NullValueHandling = NullValueHandling.Ignore)]
	public string? LeaderId { get; init; }

	public const string TypeTag = "StartCombat";

	public Command_StartCombat()
	{
		Type = "StartCombat";
	}
}
