using System.Collections.Generic;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckRogue.Engine;

public static class JsonUnions
{
	public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
	{
		MissingMemberHandling = MissingMemberHandling.Ignore,
		DateParseHandling = DateParseHandling.None,
		Converters = new List<JsonConverter>
		{
			new TaggedUnionConverter<Command>(),
			new TaggedUnionConverter<RunCommand>(),
			new TaggedUnionConverter<GameEvent>(),
			new ReadOnlyDictionaryAsDictionaryConverter()
		}
	};

	public static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);

	public static T Deserialize<T>(string json)
	{
		return JsonConvert.DeserializeObject<T>(json, Settings);
	}

	public static T FromToken<T>(JToken token)
	{
		return token.ToObject<T>(Serializer);
	}

	public static string Serialize(object value)
	{
		return JsonConvert.SerializeObject(value, Settings);
	}
}
