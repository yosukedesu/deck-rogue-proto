using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_ReactionHeld : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("stage")]
	public string Stage { get; init; }

	[JsonProperty("kind")]
	public string Kind { get; init; }

	[JsonProperty("value")]
	public int Value { get; init; }

	[JsonProperty("candidateIds")]
	public IReadOnlyList<string> CandidateIds { get; init; }

	public const string TypeTag = "ReactionHeld";

	public GameEvent_ReactionHeld()
	{
		Type = "ReactionHeld";
	}
}
