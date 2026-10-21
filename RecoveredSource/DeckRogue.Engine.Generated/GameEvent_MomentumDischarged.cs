using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_MomentumDischarged : GameEvent
{
	[JsonProperty("spent")]
	public int Spent { get; init; }

	public const string TypeTag = "MomentumDischarged";

	public GameEvent_MomentumDischarged()
	{
		Type = "MomentumDischarged";
	}
}
