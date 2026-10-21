using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_PickRelic : RunCommand
{
	[JsonProperty("index")]
	public int Index { get; init; }

	public const string TypeTag = "PickRelic";

	public RunCommand_PickRelic()
	{
		Type = "PickRelic";
	}
}
