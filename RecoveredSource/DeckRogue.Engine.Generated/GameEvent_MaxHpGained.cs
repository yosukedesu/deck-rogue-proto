using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_MaxHpGained : GameEvent
{
	[JsonProperty("presentationReceipt", NullValueHandling = NullValueHandling.Ignore)]
	public CombatPresentationReceipt? PresentationReceipt { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "MaxHpGained";

	public GameEvent_MaxHpGained()
	{
		Type = "MaxHpGained";
	}
}
