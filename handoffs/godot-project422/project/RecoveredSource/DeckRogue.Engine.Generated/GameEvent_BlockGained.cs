using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_BlockGained : GameEvent
{
	[JsonProperty("presentationReceipt", NullValueHandling = NullValueHandling.Ignore)]
	public CombatPresentationReceipt? PresentationReceipt { get; init; }

	[JsonProperty("target")]
	public string Target { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	[JsonProperty("sourceUid", NullValueHandling = NullValueHandling.Ignore)]
	public string? SourceUid { get; init; }

	public const string TypeTag = "BlockGained";

	public GameEvent_BlockGained()
	{
		Type = "BlockGained";
	}
}
