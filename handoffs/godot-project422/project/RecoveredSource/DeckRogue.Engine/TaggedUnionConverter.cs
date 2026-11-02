using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckRogue.Engine;

public sealed class TaggedUnionConverter<TBase> : JsonConverter where TBase : class
{
	private static readonly Dictionary<string, Type> ByTag = BuildMap();

	public static IReadOnlyCollection<string> KnownTags => ByTag.Keys;

	public override bool CanWrite => false;

	private static Dictionary<string, Type> BuildMap()
	{
		Dictionary<string, Type> map = new Dictionary<string, Type>(StringComparer.Ordinal);
		Type[] types = typeof(TBase).Assembly.GetTypes();
		foreach (Type t in types)
		{
			if (t.IsClass && !t.IsAbstract && typeof(TBase).IsAssignableFrom(t))
			{
				FieldInfo field = t.GetField("TypeTag", BindingFlags.Static | BindingFlags.Public);
				if (!(field == null) && !(field.FieldType != typeof(string)) && field.GetValue(null) is string tag && !map.ContainsKey(tag))
				{
					map[tag] = t;
				}
			}
		}
		return map;
	}

	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(TBase);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}
		JObject jo = JObject.Load(reader);
		string tag = (string?)jo["type"];
		if (tag == null)
		{
			throw new InvalidOperationException(typeof(TBase).Name + " に type がない: " + jo.ToString(Formatting.None));
		}
		if (!ByTag.TryGetValue(tag, out var derived))
		{
			throw new InvalidOperationException("未知の " + typeof(TBase).Name + ": " + tag);
		}
		object target = Activator.CreateInstance(derived);
		using JsonReader r = jo.CreateReader();
		serializer.Populate(r, target);
		return target;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		throw new NotSupportedException("CanWrite=false");
	}
}
