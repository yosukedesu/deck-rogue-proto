using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardUpgradedInHand : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "CardUpgradedInHand";

	public GameEvent_CardUpgradedInHand()
	{
		Type = "CardUpgradedInHand";
	}
}
