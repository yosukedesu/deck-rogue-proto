using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Presentation;

public static class ActorMaterialBinding
{
	public readonly record struct PointLight(Vector3 Position, float Range, Vector3 LinearRadiance, bool IsAttack);

	private static readonly Dictionary<(int, bool), JObject> Looks = new Dictionary<(int, bool), JObject>();

	public static JObject LoadLook(int act, bool phone)
	{
		(int, bool) key = (Math.Clamp(act, 1, 3), phone);
		if (Looks.TryGetValue(key, out JObject cached))
		{
			return cached;
		}
		JObject result = new JObject();
		List<string> names = new List<string>
		{
			$"look_act{key.Item1}",
			$"look_act{key.Item1}.char",
			$"look_act{key.Item1}.dof"
		};
		if (key.Item1 == 1)
		{
			names.Add("look_act1_preview_night");
		}
		if (phone)
		{
			names.Add($"look_act{key.Item1}.phone");
		}
		if (key.Item1 == 1)
		{
			names.Add("look_act1_integrated");
		}
		foreach (string name in names)
		{
			string path = "res://assets/Stage/" + name + ".json";
			if (FileAccess.FileExists(path))
			{
				result.Merge(JObject.Parse(FileAccess.GetFileAsString(path)), new JsonMergeSettings
				{
					MergeArrayHandling = MergeArrayHandling.Replace,
					MergeNullValueHandling = MergeNullValueHandling.Ignore
				});
			}
		}
		Looks[key] = result;
		return result;
	}

	public static void Bind(ShaderMaterial material, ActorMaterialSettings.Values values, JObject look, bool warmKey = false, float distanceScale = 1f, bool sourceDisplayTransfer = false)
	{
		F("_Receive", values.Receive);
		F("_HeroLift", values.HeroLift);
		F("_AmbientScale", values.AmbientScale);
		F("_EmissionIntensity", values.EmissionIntensity);
		F("_Fog", values.Fog);
		F("_CharSat", values.Saturation);
		F("_LocalLights", values.LocalLights);
		F("_WhiteCap", values.WhiteCap);
		F("_KeyFlip", values.KeyFlip);
		F("_Rim", values.Rim);
		V("_CharTint", values.CharTint);
		V("_BlackLift", values.BlackLift);
		V("_OutlineFloor", values.OutlineFloor);
		material.SetShaderParameter("_ShadeLift", values.ShadeLift);
		material.SetShaderParameter("_BodyShade", values.BodyShade);
		JObject ch = look["char"] as JObject;
		Vector3 key = Linear(ch?[warmKey ? "keyWarm" : "key"], warmKey ? new Vector3(1f, 0.92f, 0.8f) : new Vector3(1f, 0.95f, 0.88f));
		V("_CharKeyColor", key * Math.Max(0f, Number(ch?["keyIntensity"], 1.15f)));
		V("_CharAmbTop", sourceDisplayTransfer ? Read3(ch?["ambTop"], new Vector3(0.63f, 0.66f, 0.75f)) : Linear(ch?["ambTop"], new Vector3(0.63f, 0.66f, 0.75f)));
		V("_CharAmbBottom", sourceDisplayTransfer ? Read3(ch?["ambBottom"], new Vector3(0.39f, 0.4f, 0.47f)) : Linear(ch?["ambBottom"], new Vector3(0.39f, 0.4f, 0.47f)));
		JObject lamp = look["lamp"] as JObject;
		Vector3 direction = Read3(lamp?["charKeyDir"], -Read3(lamp?["offset"], new Vector3(-8f, 17f, -14f)));
		if (direction.LengthSquared() < 1E-06f)
		{
			direction = Vector3.Down;
		}
		direction = direction.Normalized();
		V("_CharKeyDir", new Vector3(direction.X, direction.Y, 0f - direction.Z));
		V("_RimColor", Linear(ch?["rimColor"], new Vector3(0.78f, 0.82f, 0.95f)));
		JObject fog = look["fog"] as JObject;
		material.SetShaderParameter("_FogEnabled", fog?["on"]?.Value<bool>() ?? true);
		V("_FogColor", Linear(fog?["color"], new Vector3(0.36f, 0.44f, 0.56f)));
		material.SetShaderParameter("_FogRange", new Vector2(Number(fog?["start"], 22f) * distanceScale, Number(fog?["end"], 48f) * distanceScale));
		F("_KeyVisibility", 1f);
		void F(string text, float value)
		{
			material.SetShaderParameter(text, value);
		}
		void V(string text, Vector3 value)
		{
			material.SetShaderParameter(text, value);
		}
	}

	public static void BindLights(ShaderMaterial material, IReadOnlyList<PointLight> lights, Vector4[] positions, Vector4[] colors)
	{
		int count = Math.Min(8, lights.Count);
		for (int i = 0; i < count; i++)
		{
			PointLight l = lights[i];
			positions[i] = new Vector4(l.Position.X, l.Position.Y, l.Position.Z, l.Range);
			colors[i] = new Vector4(l.LinearRadiance.X, l.LinearRadiance.Y, l.LinearRadiance.Z, l.IsAttack ? 1 : 0);
		}
		material.SetShaderParameter("_ActorLightCount", count);
		material.SetShaderParameter("_ActorLightPositionRange", positions);
		material.SetShaderParameter("_ActorLightColorKind", colors);
	}

	public static void BindKeyFlipTables(ActorMaterialSettings.Context context, int act)
	{
		context.MirroredArts.Clear();
		context.MeasuredRightArts.Clear();
		int[] array = ((act != 1) ? new int[2]
		{
			1,
			Math.Clamp(act, 1, 3)
		} : new int[1] { 1 });
		foreach (int sourceAct in array)
		{
			string path = $"res://assets/Art/stage/act{sourceAct}/keyflip.json";
			if (!FileAccess.FileExists(path))
			{
				continue;
			}
			JObject table = JObject.Parse(FileAccess.GetFileAsString(path));
			if (table["keyflip"] is JArray flipped)
			{
				foreach (JToken name in flipped)
				{
					if (name.Type == JTokenType.String)
					{
						context.MirroredArts.Add((string?)name);
					}
				}
			}
			if (!(table["lightFromRight"] is JArray measured))
			{
				continue;
			}
			foreach (JToken name2 in measured)
			{
				if (name2.Type == JTokenType.String)
				{
					context.MeasuredRightArts.Add((string?)name2);
				}
			}
		}
	}

	public static Vector3 Read3(JToken? token, Vector3 fallback)
	{
		return (token is JArray { Count: >=3 } a) ? new Vector3((float)a[0], (float)a[1], (float)a[2]) : fallback;
	}

	public static Vector3 Linear(JToken? token, Vector3 fallback)
	{
		Vector3 c = Read3(token, fallback);
		return new Vector3(Srgb(c.X), Srgb(c.Y), Srgb(c.Z));
	}

	public static float Srgb(float x)
	{
		return ActorMaterialSettings.UnityGammaToLinearCompatible(x);
	}

	public static float Number(JToken? token, float fallback)
	{
		return (token != null && (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)) ? ((float)token) : fallback;
	}
}
