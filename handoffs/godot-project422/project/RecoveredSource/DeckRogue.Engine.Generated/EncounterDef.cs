using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EncounterDef
{
	[JsonProperty("id")]
	public string Id { get; init; }

	[JsonProperty("name")]
	public string Name { get; init; }

	[JsonProperty("members")]
	public IReadOnlyList<EncounterMember> Members { get; init; }
}
