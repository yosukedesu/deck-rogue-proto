using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed class MetricsExport
{
	[JsonProperty("schema")]
	public string Schema = "deck-rogue-metrics/1";

	[JsonProperty("fingerprint")]
	public string Fingerprint = "";

	[JsonProperty("run")]
	public MetricsRunSummary? Run;

	[JsonProperty("battles")]
	public List<BattleRow> Battles = new List<BattleRow>();

	[JsonProperty("acts")]
	public List<ActSummary> Acts = new List<ActSummary>();
}
