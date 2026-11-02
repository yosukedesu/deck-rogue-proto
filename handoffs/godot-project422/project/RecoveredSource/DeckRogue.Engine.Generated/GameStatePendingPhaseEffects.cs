using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameStatePendingPhaseEffects
{
	[JsonProperty("effect")]
	public DeclarativeEffect Effect { get; init; }

	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("presentationSource", NullValueHandling = NullValueHandling.Ignore)]
	public DamagePresentationSource? PresentationSource { get; init; }
}
