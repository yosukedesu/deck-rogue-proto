using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_ShopBuyDeparture : RunCommand
{
	[JsonProperty("index")]
	public int Index { get; init; }

	[JsonProperty("cardIndex", NullValueHandling = NullValueHandling.Ignore)]
	public int? CardIndex { get; init; }

	public const string TypeTag = "ShopBuyDeparture";

	public RunCommand_ShopBuyDeparture()
	{
		Type = "ShopBuyDeparture";
	}
}
