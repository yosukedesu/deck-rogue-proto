using System.Collections.Generic;

namespace DeckRogue.Engine;

public sealed record DifficultyScaleRow
{
	public IReadOnlyList<double> Hp { get; init; }

	public IReadOnlyList<double> Atk { get; init; }
}
