using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_UseGear : RunCommand
{
	[JsonProperty("index")]
	public int Index { get; init; }

	[JsonProperty("targetIndex", NullValueHandling = NullValueHandling.Ignore)]
	public int? TargetIndex { get; init; }

	[JsonProperty("cardUid", NullValueHandling = NullValueHandling.Ignore)]
	public string? CardUid { get; init; }

	[JsonProperty("asGearId", NullValueHandling = NullValueHandling.Ignore)]
	public string? AsGearId { get; init; }

	public const string TypeTag = "UseGear";

	public RunCommand_UseGear()
	{
		Type = "UseGear";
	}
}
