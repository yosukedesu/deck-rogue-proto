using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunJournal
{
	[JsonProperty("origin")]
	public ReplayOrigin Origin { get; init; }

	[JsonProperty("commands")]
	public IReadOnlyList<RunCommand> Commands { get; init; }

	[JsonProperty("times", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<long>? Times { get; init; }
}
