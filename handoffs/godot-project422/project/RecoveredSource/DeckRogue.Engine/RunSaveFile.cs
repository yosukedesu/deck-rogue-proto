using System.Collections.Generic;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckRogue.Engine;

public sealed class RunSaveFile
{
	[JsonProperty("kind")]
	public string Kind = "run";

	[JsonProperty("run")]
	public RunState Run;

	[JsonProperty("logIndex")]
	public int LogIndex;

	[JsonProperty("fingerprint")]
	public string Fingerprint = "";

	[JsonProperty("history")]
	public List<BattleArchive> History = new List<BattleArchive>();

	[JsonProperty("playNotes")]
	public List<PlayNote> PlayNotes = new List<PlayNote>();

	[JsonProperty("journal", NullValueHandling = NullValueHandling.Ignore)]
	public RunJournal? Journal;

	[JsonProperty("choices", NullValueHandling = NullValueHandling.Ignore)]
	public List<RunChoice>? Choices;

	[JsonProperty("doodlesUnity", NullValueHandling = NullValueHandling.Ignore)]
	public JToken? DoodlesUnity;
}
