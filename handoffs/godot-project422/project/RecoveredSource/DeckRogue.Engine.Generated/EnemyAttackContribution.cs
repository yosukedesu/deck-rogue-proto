using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyAttackContribution
{
	[JsonProperty("amountContribution")]
	public int AmountContribution { get; init; }

	[JsonProperty("hpLossContribution")]
	public int HpLossContribution { get; init; }

	[JsonProperty("blockContribution")]
	public int BlockContribution { get; init; }

	[JsonProperty("iceContribution")]
	public int IceContribution { get; init; }

	[JsonProperty("nullifiedContribution")]
	public int NullifiedContribution { get; init; }

	[JsonProperty("mitigatedContribution")]
	public int MitigatedContribution { get; init; }
}
