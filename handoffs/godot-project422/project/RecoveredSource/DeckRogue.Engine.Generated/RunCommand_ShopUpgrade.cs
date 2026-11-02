using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_ShopUpgrade : RunCommand
{
	[JsonProperty("index")]
	public int Index { get; init; }

	public const string TypeTag = "ShopUpgrade";

	public RunCommand_ShopUpgrade()
	{
		Type = "ShopUpgrade";
	}
}
