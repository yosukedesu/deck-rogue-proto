using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_EnergyMaxGained : GameEvent
{
	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "EnergyMaxGained";

	public GameEvent_EnergyMaxGained()
	{
		Type = "EnergyMaxGained";
	}
}
