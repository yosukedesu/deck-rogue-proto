using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_PickReward : RunCommand
{
	[JsonProperty("index")]
	public int Index { get; init; }

	public const string TypeTag = "PickReward";

	public RunCommand_PickReward()
	{
		Type = "PickReward";
	}
}
