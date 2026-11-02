using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed class T1DamageStat
{
	[JsonProperty("min")]
	public int Min;

	[JsonProperty("median")]
	public int Median;

	[JsonProperty("max")]
	public int Max;
}
