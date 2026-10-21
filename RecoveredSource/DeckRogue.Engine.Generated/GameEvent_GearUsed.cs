using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_GearUsed : GameEvent
{
	[JsonProperty("gearId")]
	public string GearId { get; init; }

	[JsonProperty("name")]
	public string Name { get; init; }

	public const string TypeTag = "GearUsed";

	public GameEvent_GearUsed()
	{
		Type = "GearUsed";
	}
}
