using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_EnergyGained : GameEvent
{
	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "EnergyGained";

	public GameEvent_EnergyGained()
	{
		Type = "EnergyGained";
	}
}
