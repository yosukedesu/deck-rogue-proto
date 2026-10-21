using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record CombatPresentationVitals
{
	[JsonProperty("hp")]
	public int Hp { get; init; }

	[JsonProperty("maxHp")]
	public int MaxHp { get; init; }

	[JsonProperty("block")]
	public int Block { get; init; }

	[JsonProperty("iceBlock", NullValueHandling = NullValueHandling.Ignore)]
	public int? IceBlock { get; init; }
}
