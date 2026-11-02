using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record DifficultyScaleRow
{
	[JsonProperty("hp")]
	public IReadOnlyList<int> Hp { get; init; }

	[JsonProperty("atk")]
	public IReadOnlyList<int> Atk { get; init; }
}
