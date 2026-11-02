using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_RelicChooseCards : RunCommand
{
	[JsonProperty("indices")]
	public IReadOnlyList<int> Indices { get; init; }

	public const string TypeTag = "RelicChooseCards";

	public RunCommand_RelicChooseCards()
	{
		Type = "RelicChooseCards";
	}
}
