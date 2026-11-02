using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed class BattleMetrics
{
	[JsonProperty("turns")]
	public int Turns;

	[JsonProperty("t1Damage")]
	public int T1Damage;

	[JsonProperty("totalDealt")]
	public int TotalDealt;

	[JsonProperty("totalTaken")]
	public int TotalTaken;

	[JsonProperty("maxTurnDamage")]
	public int MaxTurnDamage;

	[JsonProperty("sets")]
	public int Sets;

	[JsonProperty("fires")]
	public int Fires;

	[JsonProperty("holds")]
	public int Holds;

	[JsonProperty("expires")]
	public int Expires;

	[JsonProperty("dolls")]
	public int Dolls;

	[JsonProperty("dollsGone")]
	public int DollsGone;

	[JsonProperty("rallies")]
	public int Rallies;

	[JsonProperty("dollOnlyOpening")]
	public int DollOnlyOpening;

	[JsonProperty("perTurn")]
	public List<TurnMetrics> PerTurn = new List<TurnMetrics>();
}
