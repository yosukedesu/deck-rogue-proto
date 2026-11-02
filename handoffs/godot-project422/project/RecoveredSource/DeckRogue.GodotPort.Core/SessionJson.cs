using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Core;

public static class SessionJson
{
	public static JObject ParseObject(string json)
	{
		using StringReader text = new StringReader(json);
		using JsonTextReader reader = new JsonTextReader(text)
		{
			DateParseHandling = DateParseHandling.None
		};
		return JObject.Load(reader);
	}
}
