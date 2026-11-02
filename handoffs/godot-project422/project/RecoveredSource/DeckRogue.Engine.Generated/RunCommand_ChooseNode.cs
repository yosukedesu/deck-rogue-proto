using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_ChooseNode : RunCommand
{
	[JsonProperty("col")]
	public int Col { get; init; }

	public const string TypeTag = "ChooseNode";

	public RunCommand_ChooseNode()
	{
		Type = "ChooseNode";
	}
}
