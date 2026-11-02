namespace DeckRogue.Engine;

public sealed record RunOptions
{
	public bool? SetAnyCards { get; init; }

	public bool? Departure { get; init; }
}
