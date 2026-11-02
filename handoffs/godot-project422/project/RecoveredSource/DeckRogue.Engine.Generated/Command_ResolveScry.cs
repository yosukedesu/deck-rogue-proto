using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record Command_ResolveScry : Command
{
	[JsonProperty("discardUids")]
	public IReadOnlyList<string> DiscardUids { get; init; }

	public const string TypeTag = "ResolveScry";

	public Command_ResolveScry()
	{
		Type = "ResolveScry";
	}
}
