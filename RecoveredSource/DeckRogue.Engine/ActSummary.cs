using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed class ActSummary
{
	[JsonProperty("act")]
	public int Act;

	[JsonProperty("battles")]
	public int Battles;

	[JsonProperty("normalTurnsAvg")]
	public double? NormalTurnsAvg;

	[JsonProperty("bossTurns")]
	public int? BossTurns;

	[JsonProperty("eliteTurns")]
	public List<int> EliteTurns = new List<int>();

	[JsonProperty("t1Damage")]
	public T1DamageStat? T1Damage;

	[JsonProperty("hpLost")]
	public int HpLost;

	[JsonProperty("sets")]
	public int Sets;

	[JsonProperty("fires")]
	public int Fires;

	[JsonProperty("funAvg")]
	public double? FunAvg;

	[JsonProperty("strengthAvg")]
	public double? StrengthAvg;

	[JsonProperty("lateActionRate")]
	public double? LateActionRate;
}
