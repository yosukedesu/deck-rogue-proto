using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record Command_PlayCard : Command
{
	[JsonProperty("cardUid")]
	public string CardUid { get; init; }

	[JsonProperty("modeIndex", NullValueHandling = NullValueHandling.Ignore)]
	public int? ModeIndex { get; init; }

	[JsonProperty("discardUids", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? DiscardUids { get; init; }

	[JsonProperty("targetIndex", NullValueHandling = NullValueHandling.Ignore)]
	public int? TargetIndex { get; init; }

	[JsonProperty("exhaustUids", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? ExhaustUids { get; init; }

	[JsonProperty("retrieveUid", NullValueHandling = NullValueHandling.Ignore)]
	public string? RetrieveUid { get; init; }

	[JsonProperty("deckUids", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? DeckUids { get; init; }

	[JsonProperty("handUids", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? HandUids { get; init; }

	[JsonProperty("xAmount", NullValueHandling = NullValueHandling.Ignore)]
	public int? XAmount { get; init; }

	[JsonProperty("permanentUid", NullValueHandling = NullValueHandling.Ignore)]
	public string? PermanentUid { get; init; }

	public const string TypeTag = "PlayCard";

	public Command_PlayCard()
	{
		Type = "PlayCard";
	}
}
