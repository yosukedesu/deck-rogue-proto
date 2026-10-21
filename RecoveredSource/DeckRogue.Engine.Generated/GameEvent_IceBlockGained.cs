using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_IceBlockGained : GameEvent
{
	[JsonProperty("presentationReceipt", NullValueHandling = NullValueHandling.Ignore)]
	public CombatPresentationReceipt? PresentationReceipt { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "IceBlockGained";

	public GameEvent_IceBlockGained()
	{
		Type = "IceBlockGained";
	}
}
