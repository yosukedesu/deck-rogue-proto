using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_PlayerArtifactBlocked : GameEvent
{
	[JsonProperty("status")]
	public string Status { get; init; }

	public const string TypeTag = "PlayerArtifactBlocked";

	public GameEvent_PlayerArtifactBlocked()
	{
		Type = "PlayerArtifactBlocked";
	}
}
