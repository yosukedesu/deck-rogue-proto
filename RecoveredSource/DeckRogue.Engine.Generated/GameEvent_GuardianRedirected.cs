using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_GuardianRedirected : GameEvent
{
	[JsonProperty("fromIndex")]
	public int FromIndex { get; init; }

	[JsonProperty("toIndex")]
	public int ToIndex { get; init; }

	public const string TypeTag = "GuardianRedirected";

	public GameEvent_GuardianRedirected()
	{
		Type = "GuardianRedirected";
	}
}
