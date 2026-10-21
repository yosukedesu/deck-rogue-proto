using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ScaldTick : GameEvent
{
	[JsonProperty("presentationReceipt", NullValueHandling = NullValueHandling.Ignore)]
	public CombatPresentationReceipt? PresentationReceipt { get; init; }

	[JsonProperty("count")]
	public int Count { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	[JsonProperty("scalds", NullValueHandling = NullValueHandling.Ignore)]
	public int? Scalds { get; init; }

	[JsonProperty("brands", NullValueHandling = NullValueHandling.Ignore)]
	public int? Brands { get; init; }

	public const string TypeTag = "ScaldTick";

	public GameEvent_ScaldTick()
	{
		Type = "ScaldTick";
	}
}
