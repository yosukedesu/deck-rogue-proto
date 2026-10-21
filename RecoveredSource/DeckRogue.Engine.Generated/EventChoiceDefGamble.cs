using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EventChoiceDefGamble
{
	[JsonProperty("chance")]
	public double Chance { get; init; }

	[JsonProperty("win")]
	public EventOutcome Win { get; init; }

	[JsonProperty("lose")]
	public EventOutcome Lose { get; init; }
}
