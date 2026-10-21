using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_ShopBuyCard : RunCommand
{
	[JsonProperty("index")]
	public int Index { get; init; }

	public const string TypeTag = "ShopBuyCard";

	public RunCommand_ShopBuyCard()
	{
		Type = "ShopBuyCard";
	}
}
