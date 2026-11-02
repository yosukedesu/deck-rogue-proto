using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_CampfireRemove : RunCommand
{
	[JsonProperty("index")]
	public int Index { get; init; }

	public const string TypeTag = "CampfireRemove";

	public RunCommand_CampfireRemove()
	{
		Type = "CampfireRemove";
	}
}
