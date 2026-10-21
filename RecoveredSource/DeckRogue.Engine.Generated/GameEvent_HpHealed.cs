using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_HpHealed : GameEvent
{
	[JsonProperty("presentationReceipt", NullValueHandling = NullValueHandling.Ignore)]
	public CombatPresentationReceipt? PresentationReceipt { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	[JsonProperty("sourceUid", NullValueHandling = NullValueHandling.Ignore)]
	public string? SourceUid { get; init; }

	public const string TypeTag = "HpHealed";

	public GameEvent_HpHealed()
	{
		Type = "HpHealed";
	}
}
