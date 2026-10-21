using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_DiscardGear : RunCommand
{
	[JsonProperty("index")]
	public int Index { get; init; }

	public const string TypeTag = "DiscardGear";

	public RunCommand_DiscardGear()
	{
		Type = "DiscardGear";
	}
}
