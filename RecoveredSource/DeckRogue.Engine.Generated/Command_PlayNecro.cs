using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record Command_PlayNecro : Command
{
	[JsonProperty("cardUid")]
	public string CardUid { get; init; }

	[JsonProperty("targetIndex", NullValueHandling = NullValueHandling.Ignore)]
	public int? TargetIndex { get; init; }

	public const string TypeTag = "PlayNecro";

	public Command_PlayNecro()
	{
		Type = "PlayNecro";
	}
}
