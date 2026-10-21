using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Presentation;

public sealed class CardPresentationProfiles
{
	private readonly Dictionary<string, CardPresentationProfile> _profiles = new Dictionary<string, CardPresentationProfile>(StringComparer.Ordinal);

	public int Count => _profiles.Count;

	public IReadOnlyCollection<CardPresentationProfile> Profiles => _profiles.Values;

	public CardPresentationProfile? Find(string definitionId)
	{
		if (!_profiles.TryGetValue(definitionId, out var value))
		{
			return null;
		}
		return value;
	}

	public void Load(string json)
	{
		JObject jObject = JObject.Parse(json);
		_profiles.Clear();
		foreach (JProperty p in jObject.Properties())
		{
			if (!p.Name.StartsWith('_') && p.Value is JObject value)
			{
				string element = value["element"]?.Value<string>();
				string style = value["style"]?.Value<string>();
				if (!string.IsNullOrEmpty(element) && !string.IsNullOrEmpty(style))
				{
					_profiles.Add(p.Name, new CardPresentationProfile(p.Name, element, style));
				}
			}
		}
	}
}
