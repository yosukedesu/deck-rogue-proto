using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GearInstance
{
	[JsonProperty("uid")]
	public string Uid { get; init; }

	[JsonProperty("gearId")]
	public string GearId { get; init; }

	[JsonProperty("charges")]
	public int Charges { get; init; }
}
