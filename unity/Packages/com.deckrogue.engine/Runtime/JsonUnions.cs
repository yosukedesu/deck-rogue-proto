// JsonUnions.cs — 判別共用体 (Command / RunCommand / GameEvent) を Newtonsoft Json.NET で読むためのコンバータ。
// 生成型 (Generated/Types.g.cs) は「抽象 record + 派生 record (public const string TypeTag と既定コンストラクタ)」なので、
// JSON の "type" フィールドを見て派生型を決め、その型へ流し込む。派生型の一覧は反射で作る (追加時に手作業が要らない)。
//
// 使い方:  var cmd = JsonUnions.Deserialize<RunCommand>(json)
//          JsonConvert.DeserializeObject<List<RunCommand>>(json, JsonUnions.Settings)
//          token.ToObject<RunCommand>(JsonUnions.Serializer)

using System;
using System.Collections.Generic;
using System.Reflection;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckRogue.Engine
{
    /// <summary>type タグで派生 record へ分岐するコンバータ (抽象基底 TBase 専用)</summary>
    public sealed class TaggedUnionConverter<TBase> : JsonConverter where TBase : class
    {
        private static readonly Dictionary<string, Type> ByTag = BuildMap();

        private static Dictionary<string, Type> BuildMap()
        {
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var t in typeof(TBase).Assembly.GetTypes())
            {
                if (!t.IsClass || t.IsAbstract) continue;
                if (!typeof(TBase).IsAssignableFrom(t)) continue;
                var field = t.GetField("TypeTag", BindingFlags.Public | BindingFlags.Static);
                if (field == null || field.FieldType != typeof(string)) continue;
                var tag = field.GetValue(null) as string;
                if (tag != null && !map.ContainsKey(tag)) map[tag] = t;
            }
            return map;
        }

        /// <summary>この共用体が知っている type タグの一覧 (診断用)</summary>
        public static IReadOnlyCollection<string> KnownTags => ByTag.Keys;

        // 宣言型が抽象基底の時だけ働く = 派生型への流し込みで再帰しない
        public override bool CanConvert(Type objectType) => objectType == typeof(TBase);

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var jo = JObject.Load(reader);
            var tag = (string)jo["type"];
            if (tag == null) throw new InvalidOperationException($"{typeof(TBase).Name} に type がない: {jo.ToString(Formatting.None)}");
            if (!ByTag.TryGetValue(tag, out var derived)) throw new InvalidOperationException($"未知の {typeof(TBase).Name}: {tag}");
            var target = Activator.CreateInstance(derived);
            using (var r = jo.CreateReader())
            {
                // Populate はルートのコンバータを見ないので再帰しない (入れ子の Command 等は通常どおり分岐する)
                serializer.Populate(r, target);
            }
            return target;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException("CanWrite=false");
        }
    }

    public static class JsonUnions
    {
        /// <summary>3つの判別共用体のコンバータを載せた設定 (JSON のキーは camelCase のまま)</summary>
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = new List<JsonConverter>
            {
                new TaggedUnionConverter<Command>(),
                new TaggedUnionConverter<RunCommand>(),
                new TaggedUnionConverter<GameEvent>(),
            },
        };

        public static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);

        public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);

        public static T FromToken<T>(JToken token) => token.ToObject<T>(Serializer);

        /// <summary>共用体を JSON へ (要約表示・ログ用。type は派生の既定コンストラクタが埋めている)</summary>
        public static string Serialize(object value) => JsonConvert.SerializeObject(value, Settings);
    }
}
