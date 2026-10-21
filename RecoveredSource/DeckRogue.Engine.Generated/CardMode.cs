using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record CardMode
{
	[JsonProperty("name")]
	public string Name { get; init; }

	[JsonProperty("effects")]
	public IReadOnlyList<DeclarativeEffect> Effects { get; init; }
}
