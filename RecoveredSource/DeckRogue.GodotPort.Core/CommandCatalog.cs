using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Core;

public static class CommandCatalog
{
	private static readonly IReadOnlyDictionary<string, Type> RunTypes = Build(typeof(RunCommand));

	private static readonly IReadOnlyDictionary<string, Type> CombatTypes = Build(typeof(Command));

	public static IReadOnlyCollection<string> RunTags { get; } = RunTypes.Keys.OrderBy((string x) => x, StringComparer.Ordinal).ToArray();

	public static IReadOnlyCollection<string> CombatTags { get; } = CombatTypes.Keys.OrderBy((string x) => x, StringComparer.Ordinal).ToArray();

	private static IReadOnlyDictionary<string, Type> Build(Type parent)
	{
		return (from t in parent.Assembly.GetTypes()
			where !t.IsAbstract && parent.IsAssignableFrom(t)
			select t).ToDictionary((Type t) => (string)t.GetField("TypeTag", BindingFlags.Static | BindingFlags.Public).GetValue(null), (Type t) => t, StringComparer.Ordinal);
	}

	public static RunCommand Parse(string json)
	{
		JObject jo = SessionJson.ParseObject(json);
		string tag = ((string?)jo["type"]) ?? throw new InvalidOperationException("Command requires a type.");
		if (RunTypes.TryGetValue(tag, out var runType))
		{
			ValidateJson(jo, runType);
			return JsonUnions.Deserialize<RunCommand>(json) ?? throw new InvalidOperationException("Empty run command.");
		}
		if (CombatTypes.TryGetValue(tag, out var combatType))
		{
			ValidateJson(jo, combatType);
			return new RunCommand_Combat
			{
				Command = JsonUnions.Deserialize<Command>(json)
			};
		}
		throw new InvalidOperationException("Unknown command: " + tag);
	}

	public static void Validate(RunCommand command)
	{
		if (command == null)
		{
			throw new ArgumentNullException("command");
		}
		if (!RunTypes.TryGetValue(command.Type ?? "", out var type) || command.GetType() != type)
		{
			throw new InvalidOperationException("Unknown or mismatched run command type.");
		}
		if (command is RunCommand_Combat combat && (combat.Command == null || !CombatTypes.TryGetValue(combat.Command.Type ?? "", out var ct) || combat.Command.GetType() != ct))
		{
			throw new InvalidOperationException("Unknown or mismatched combat command type.");
		}
		ValidateJson(SessionJson.ParseObject(JsonUnions.Serialize(command)), type);
	}

	private static void ValidateJson(JObject value, Type type)
	{
		PropertyInfo[] properties = type.GetProperties();
		foreach (PropertyInfo p in properties)
		{
			JsonPropertyAttribute attr = p.GetCustomAttribute<JsonPropertyAttribute>();
			if (attr != null && attr.NullValueHandling != NullValueHandling.Ignore)
			{
				string name = attr.PropertyName ?? p.Name;
				if (!value.TryGetValue(name, StringComparison.Ordinal, out JToken token) || token.Type == JTokenType.Null)
				{
					throw new InvalidOperationException("Command requires " + name + ".");
				}
				if (p.PropertyType == typeof(int) && token.Type != JTokenType.Integer)
				{
					throw new InvalidOperationException(name + " must be an integer.");
				}
				if (p.PropertyType == typeof(bool) && token.Type != JTokenType.Boolean)
				{
					throw new InvalidOperationException(name + " must be true or false.");
				}
				if (p.PropertyType == typeof(string) && token.Type != JTokenType.String)
				{
					throw new InvalidOperationException(name + " must be text.");
				}
			}
		}
		if (type == typeof(RunCommand_Combat))
		{
			JObject obj = (value["command"] as JObject) ?? throw new InvalidOperationException("Combat command requires an object.");
			string tag = ((string?)obj["type"]) ?? "";
			if (!CombatTypes.TryGetValue(tag, out var ct))
			{
				throw new InvalidOperationException("Unknown combat command: " + tag);
			}
			ValidateJson(obj, ct);
		}
	}
}
