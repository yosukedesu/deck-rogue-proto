using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_TakeGear : RunCommand
{
	[JsonProperty("discardIndex", NullValueHandling = NullValueHandling.Ignore)]
	public int? DiscardIndex { get; init; }

	public const string TypeTag = "TakeGear";

	public RunCommand_TakeGear()
	{
		Type = "TakeGear";
	}
}
