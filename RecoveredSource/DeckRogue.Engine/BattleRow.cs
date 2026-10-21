using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed class BattleRow
{
	[JsonProperty("battleNo")]
	public int BattleNo;

	[JsonProperty("act")]
	public int Act;

	[JsonProperty("enemyId")]
	public string EnemyId = "";

	[JsonProperty("elite")]
	public bool Elite;

	[JsonProperty("boss")]
	public bool Boss;

	[JsonProperty("result")]
	public string Result = "won";

	[JsonProperty("hpBefore")]
	public int HpBefore;

	[JsonProperty("hpAfter")]
	public int HpAfter;

	[JsonProperty("metrics")]
	public BattleMetrics? Metrics;

	[JsonProperty("rating", NullValueHandling = NullValueHandling.Ignore)]
	public BattleRating? Rating;
}
