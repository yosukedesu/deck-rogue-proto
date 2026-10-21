using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record Command_EndTurn : Command
{
	[JsonProperty("hearthSparks", NullValueHandling = NullValueHandling.Ignore)]
	public int? HearthSparks { get; init; }

	[JsonProperty("retainUids", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? RetainUids { get; init; }

	[JsonProperty("keepUid", NullValueHandling = NullValueHandling.Ignore)]
	public string? KeepUid { get; init; }

	public const string TypeTag = "EndTurn";

	public Command_EndTurn()
	{
		Type = "EndTurn";
	}
}
