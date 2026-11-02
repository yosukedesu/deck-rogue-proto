using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_Combat : RunCommand
{
	[JsonProperty("command")]
	public Command Command { get; init; }

	public const string TypeTag = "Combat";

	public RunCommand_Combat()
	{
		Type = "Combat";
	}
}
