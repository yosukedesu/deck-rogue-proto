using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed class ReadOnlyDictionaryAsDictionaryConverter : JsonConverter
{
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType)
	{
		if (objectType.IsGenericType)
		{
			return objectType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<, >);
		}
		return false;
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}
		Type[] args = objectType.GetGenericArguments();
		Type dictType = typeof(Dictionary<, >).MakeGenericType(args[0], args[1]);
		return serializer.Deserialize(reader, dictType);
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		throw new NotSupportedException("CanWrite=false");
	}
}
