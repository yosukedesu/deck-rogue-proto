using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed class MetricsRunSummary
{
	[JsonProperty("act")]
	public int Act;

	[JsonProperty("battlesWon")]
	public int BattlesWon;

	[JsonProperty("hp")]
	public int Hp;

	[JsonProperty("deckSize")]
	public int DeckSize;
}
