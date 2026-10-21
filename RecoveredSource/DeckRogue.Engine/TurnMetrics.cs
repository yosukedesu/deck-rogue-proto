using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed class TurnMetrics
{
	[JsonProperty("turn")]
	public int Turn;

	[JsonProperty("dealt")]
	public int Dealt;

	[JsonProperty("counter")]
	public int Counter;

	[JsonProperty("taken")]
	public int Taken;

	[JsonProperty("plays")]
	public int Plays;

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

	[JsonIgnore]
	public int NonDollPlays;

	[JsonProperty("hand", NullValueHandling = NullValueHandling.Ignore)]
	public List<string>? Hand;

	[JsonProperty("unplayed", NullValueHandling = NullValueHandling.Ignore)]
	public List<string>? Unplayed;
}
