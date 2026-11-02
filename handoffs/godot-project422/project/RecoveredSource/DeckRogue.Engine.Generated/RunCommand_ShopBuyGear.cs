using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_ShopBuyGear : RunCommand
{
	[JsonProperty("index")]
	public int Index { get; init; }

	[JsonProperty("discardIndex", NullValueHandling = NullValueHandling.Ignore)]
	public int? DiscardIndex { get; init; }

	public const string TypeTag = "ShopBuyGear";

	public RunCommand_ShopBuyGear()
	{
		Type = "ShopBuyGear";
	}
}
