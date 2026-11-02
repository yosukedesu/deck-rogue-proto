using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_BurnTick : GameEvent
{
	[JsonProperty("presentationReceipt", NullValueHandling = NullValueHandling.Ignore)]
	public CombatPresentationReceipt? PresentationReceipt { get; init; }

	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "BurnTick";

	public GameEvent_BurnTick()
	{
		Type = "BurnTick";
	}
}
