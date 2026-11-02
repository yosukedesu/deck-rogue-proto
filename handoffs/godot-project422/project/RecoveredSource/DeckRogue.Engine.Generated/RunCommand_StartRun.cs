using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_StartRun : RunCommand
{
	[JsonProperty("seed")]
	public int Seed { get; init; }

	public const string TypeTag = "StartRun";

	public RunCommand_StartRun()
	{
		Type = "StartRun";
	}
}
