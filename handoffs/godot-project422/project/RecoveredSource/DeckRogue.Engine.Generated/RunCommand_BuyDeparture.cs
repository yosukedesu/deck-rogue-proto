using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_BuyDeparture : RunCommand
{
	[JsonProperty("index")]
	public int Index { get; init; }

	[JsonProperty("cardIndex", NullValueHandling = NullValueHandling.Ignore)]
	public int? CardIndex { get; init; }

	public const string TypeTag = "BuyDeparture";

	public RunCommand_BuyDeparture()
	{
		Type = "BuyDeparture";
	}
}
