using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_WorkshopFuse : RunCommand
{
	[JsonProperty("indexA")]
	public int IndexA { get; init; }

	[JsonProperty("indexB")]
	public int IndexB { get; init; }

	public const string TypeTag = "WorkshopFuse";

	public RunCommand_WorkshopFuse()
	{
		Type = "WorkshopFuse";
	}
}
