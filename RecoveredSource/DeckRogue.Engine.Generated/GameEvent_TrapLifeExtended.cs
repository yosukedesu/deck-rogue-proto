using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_TrapLifeExtended : GameEvent
{
	[JsonProperty("amount")]
	public int Amount { get; init; }

	[JsonProperty("count")]
	public int Count { get; init; }

	public const string TypeTag = "TrapLifeExtended";

	public GameEvent_TrapLifeExtended()
	{
		Type = "TrapLifeExtended";
	}
}
