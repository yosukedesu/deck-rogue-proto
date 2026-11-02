using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunCommand_ShopRemove : RunCommand
{
	[JsonProperty("index")]
	public int Index { get; init; }

	public const string TypeTag = "ShopRemove";

	public RunCommand_ShopRemove()
	{
		Type = "ShopRemove";
	}
}
