using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_EnemyActionExecuting : GameEvent
{
	[JsonProperty("presentationEnemyAction", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyActionPresentation? PresentationEnemyAction { get; init; }

	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("kind")]
	public string Kind { get; init; }

	public const string TypeTag = "EnemyActionExecuting";

	public GameEvent_EnemyActionExecuting()
	{
		Type = "EnemyActionExecuting";
	}
}
