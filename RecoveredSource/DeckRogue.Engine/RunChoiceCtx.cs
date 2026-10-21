using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed class RunChoiceCtx
{
	[JsonProperty("hp")]
	public int Hp;

	[JsonProperty("maxHp")]
	public int MaxHp;

	[JsonProperty("deck")]
	public int Deck;

	[JsonProperty("gold")]
	public int Gold;
}
