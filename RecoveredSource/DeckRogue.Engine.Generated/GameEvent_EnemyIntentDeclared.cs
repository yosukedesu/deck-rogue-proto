using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_EnemyIntentDeclared : GameEvent
{
	[JsonProperty("presentationMove", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyMoveDeclarationPresentation? PresentationMove { get; init; }

	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("intent")]
	public EnemyIntent Intent { get; init; }

	public const string TypeTag = "EnemyIntentDeclared";

	public GameEvent_EnemyIntentDeclared()
	{
		Type = "EnemyIntentDeclared";
	}
}
