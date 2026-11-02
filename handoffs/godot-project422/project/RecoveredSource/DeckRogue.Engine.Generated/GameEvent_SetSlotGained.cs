using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_SetSlotGained : GameEvent
{
	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "SetSlotGained";

	public GameEvent_SetSlotGained()
	{
		Type = "SetSlotGained";
	}
}
