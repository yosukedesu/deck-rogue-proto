using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_EventChoice : RunCommand
{
	[JsonProperty("index")]
	public int Index { get; init; }

	[JsonProperty("cardIndex", NullValueHandling = NullValueHandling.Ignore)]
	public int? CardIndex { get; init; }

	public const string TypeTag = "EventChoice";

	public RunCommand_EventChoice()
	{
		Type = "EventChoice";
	}
}
