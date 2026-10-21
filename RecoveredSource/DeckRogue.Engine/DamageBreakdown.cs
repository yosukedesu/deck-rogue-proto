using System.Collections.Generic;

namespace DeckRogue.Engine;

public sealed record DamageBreakdown
{
	public IReadOnlyList<DamageBreakdownStep> Steps { get; init; }

	public int Blocked { get; init; }

	public int HpLoss { get; init; }
}
