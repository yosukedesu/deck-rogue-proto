using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_EnemyHatched : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("fromId")]
	public string FromId { get; init; }

	[JsonProperty("intoId")]
	public string IntoId { get; init; }

	public const string TypeTag = "EnemyHatched";

	public GameEvent_EnemyHatched()
	{
		Type = "EnemyHatched";
	}
}
