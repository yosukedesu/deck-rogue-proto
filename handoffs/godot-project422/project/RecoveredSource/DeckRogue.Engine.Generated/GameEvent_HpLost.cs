using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_HpLost : GameEvent
{
	[JsonProperty("presentationReceipt", NullValueHandling = NullValueHandling.Ignore)]
	public CombatPresentationReceipt? PresentationReceipt { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "HpLost";

	public GameEvent_HpLost()
	{
		Type = "HpLost";
	}
}
