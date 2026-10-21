using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_DeathSaved : GameEvent
{
	[JsonProperty("presentationReceipt", NullValueHandling = NullValueHandling.Ignore)]
	public CombatPresentationReceipt? PresentationReceipt { get; init; }

	[JsonProperty("hp")]
	public int Hp { get; init; }

	[JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
	public string? Source { get; init; }

	public const string TypeTag = "DeathSaved";

	public GameEvent_DeathSaved()
	{
		Type = "DeathSaved";
	}
}
