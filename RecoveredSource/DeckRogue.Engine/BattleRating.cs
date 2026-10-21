using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed class BattleRating
{
	[JsonProperty("strength", NullValueHandling = NullValueHandling.Ignore)]
	public int? Strength;

	[JsonProperty("fun", NullValueHandling = NullValueHandling.Ignore)]
	public int? Fun;

	[JsonProperty("note", NullValueHandling = NullValueHandling.Ignore)]
	public string? Note;

	[JsonProperty("lossFeel", NullValueHandling = NullValueHandling.Ignore)]
	public string? LossFeel;

	public BattleRating Clone()
	{
		return new BattleRating
		{
			Strength = Strength,
			Fun = Fun,
			Note = Note,
			LossFeel = LossFeel
		};
	}
}
