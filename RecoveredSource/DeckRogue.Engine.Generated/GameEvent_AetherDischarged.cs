using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_AetherDischarged : GameEvent
{
	[JsonProperty("spent")]
	public int Spent { get; init; }

	public const string TypeTag = "AetherDischarged";

	public GameEvent_AetherDischarged()
	{
		Type = "AetherDischarged";
	}
}
