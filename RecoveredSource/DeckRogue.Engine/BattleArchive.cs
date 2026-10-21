using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed class BattleArchive
{
	[JsonProperty("battleNo")]
	public int BattleNo;

	[JsonProperty("act", NullValueHandling = NullValueHandling.Ignore)]
	public int? Act;

	[JsonProperty("boss", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Boss;

	[JsonProperty("metrics", NullValueHandling = NullValueHandling.Ignore)]
	public BattleMetrics? Metrics;

	[JsonProperty("enemyId")]
	public string EnemyId = "";

	[JsonProperty("elite")]
	public bool Elite;

	[JsonProperty("result")]
	public string Result = "won";

	[JsonProperty("turns")]
	public int Turns;

	[JsonProperty("hpBefore")]
	public int HpBefore;

	[JsonProperty("hpAfter")]
	public int HpAfter;

	[JsonProperty("deckSize")]
	public int DeckSize;

	[JsonProperty("lines")]
	public List<string> Lines = new List<string>();

	[JsonProperty("rating", NullValueHandling = NullValueHandling.Ignore)]
	public BattleRating? Rating;
}
