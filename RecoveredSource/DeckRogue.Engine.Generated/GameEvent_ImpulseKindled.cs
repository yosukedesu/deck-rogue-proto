using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ImpulseKindled : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("cardIds")]
	public IReadOnlyList<string> CardIds { get; init; }

	public const string TypeTag = "ImpulseKindled";

	public GameEvent_ImpulseKindled()
	{
		Type = "ImpulseKindled";
	}
}
