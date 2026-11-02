using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ArtifactBlocked : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("effect")]
	public string Effect { get; init; }

	public const string TypeTag = "ArtifactBlocked";

	public GameEvent_ArtifactBlocked()
	{
		Type = "ArtifactBlocked";
	}
}
