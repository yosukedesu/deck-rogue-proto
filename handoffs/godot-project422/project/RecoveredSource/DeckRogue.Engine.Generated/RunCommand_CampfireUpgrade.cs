using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_CampfireUpgrade : RunCommand
{
	[JsonProperty("index")]
	public int Index { get; init; }

	public const string TypeTag = "CampfireUpgrade";

	public RunCommand_CampfireUpgrade()
	{
		Type = "CampfireUpgrade";
	}
}
