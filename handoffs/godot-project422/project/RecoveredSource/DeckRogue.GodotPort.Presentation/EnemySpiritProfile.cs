using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Presentation;

public sealed class EnemySpiritProfile
{
	public sealed record Variant(string Stem, int Width, int Height, int Top, int Bottom, int RenderScale)
	{
		public int RenderWidth => Width * RenderScale;

		public int RenderHeight => Height * RenderScale;
	}

	public sealed record Channel(float X, float Y, float Phase);

	private const string ManifestPath = "res://assets/presentation-data/enemy-spirit-profiles.json";

	private static Dictionary<string, EnemySpiritProfile>? _profiles;

	public string Id { get; }

	public Variant Desktop { get; }

	public Variant? Phone { get; }

	public float Period { get; }

	public IReadOnlyList<Channel> Channels { get; }

	private EnemySpiritProfile(string id, Variant desktop, Variant? phone, float period, Channel[] channels)
	{
		Id = id;
		Desktop = desktop;
		Phone = phone;
		Period = period;
		Channels = channels;
	}

	public SpiritProbeIdleMotion.Offsets Sample(float age, bool still = false)
	{
		if (still || !float.IsFinite(age))
		{
			return default;
		}
		double num = (double)age / (double)Period;
		float num2 = (float)((num - Math.Floor(num)) * (Math.PI * 2.0));
		float a = MathF.Sin(num2 + Channels[0].Phase);
		float b = MathF.Sin(num2 + Channels[1].Phase);
		float c = MathF.Sin(num2 + Channels[2].Phase);
		return new SpiritProbeIdleMotion.Offsets(Channels[0].X * a, Channels[0].Y * a, Channels[1].X * b, Channels[1].Y * b, Channels[2].X * c, Channels[2].Y * c);
	}

	public static bool TryGet(string id, out EnemySpiritProfile? profile)
	{
		if (_profiles == null)
		{
			_profiles = Load();
		}
		return _profiles.TryGetValue(id, out profile);
	}

	public static Dictionary<string, EnemySpiritProfile> Parse(string json)
	{
		JObject root = JObject.Parse(json);
		if (ReadInt(root, "format_version") != 1 || !(root["profiles"] is JArray entries))
		{
			throw new FormatException("Invalid enemy spirit manifest version or profiles.");
		}
		Dictionary<string, EnemySpiritProfile> result = new Dictionary<string, EnemySpiritProfile>(StringComparer.Ordinal);
		foreach (JToken entry in entries)
		{
			string id = ((string?)entry["id"]) ?? "";
			Variant desktop;
			Variant phone;
			float period;
			Channel[] parsed;
			if (id.StartsWith("enemy_", StringComparison.Ordinal) && id.Length <= 80)
			{
				switch (id)
				{
				case "enemy_probe":
				case "enemy_iron_clam":
				case "enemy_maw_hunter":
					break;
				default:
				{
					string text = id;
					foreach (char ch in text)
					{
						if ((ch < 'a' || ch > 'z') && (ch < '0' || ch > '9') && ch != '_')
						{
							throw new FormatException("Invalid enemy spirit ID character.");
						}
					}
					desktop = ParseVariant(entry["desktop"], id, phone: false);
					JToken phoneToken = entry["phone"];
					if (phoneToken != null && phoneToken.Type != JTokenType.Null && !(phoneToken is JObject))
					{
						throw new FormatException("Malformed spirit phone registration.");
					}
					phone = ((entry["phone"] is JObject) ? ParseVariant(entry["phone"], id, phone: true) : null);
					if (!EnemySpiritSourceContract.Accepts(id, desktop, phone))
					{
						throw new FormatException("Unknown enemy or changed original registration.");
					}
					if ((object)phone != null && (desktop.Width < 128 || phone.Width != 96 || phone.Height != 96))
					{
						throw new FormatException("Invalid legacy phone registration.");
					}
					period = ReadNumber(entry, "period_seconds");
					if (!float.IsFinite(period) || period < 0.6f || period > 8f)
					{
						throw new FormatException("Invalid spirit idle period.");
					}
					if (!(entry["channels"] is JArray { Count: 3 } channels))
					{
						throw new FormatException("Exactly three spirit channels are required.");
					}
					parsed = new Channel[3];
					for (int j = 0; j < 3; j++)
					{
						float x = ReadNumber(channels[j], "x");
						float y = ReadNumber(channels[j], "y");
						float phase = ReadNumber(channels[j], "phase");
						if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(phase) || MathF.Abs(x) > 3f || MathF.Abs(y) > 3f || MathF.Abs(phase) > (float)Math.PI * 2f)
						{
							throw new FormatException("Invalid spirit local displacement.");
						}
						parsed[j] = new Channel(x, y, phase);
					}
					goto IL_02e7;
				}
				}
			}
			throw new FormatException("Invalid or reserved enemy spirit ID.");
			IL_02e7:
			if (!result.TryAdd(id, new EnemySpiritProfile(id, desktop, phone, period, parsed)))
			{
				throw new FormatException("Duplicate enemy spirit profile.");
			}
		}
		return result;
	}

	private static Variant ParseVariant(JToken? token, string id, bool phone)
	{
		if (!(token is JObject))
		{
			throw new FormatException("Missing spirit registration.");
		}
		string expected = "res://assets/Art/enemies/rpg_spirits/" + id + "/" + id + (phone ? "_phone" : "");
		string? obj = ((string?)token["stem"]) ?? "";
		int width = ReadInt(token, "width");
		int height = ReadInt(token, "height");
		int top = ReadInt(token, "top");
		int bottom = ReadInt(token, "bottom");
		int scale = ReadInt(token, "render_scale");
		if (obj != expected || width < 16 || width > 256 || height != width || scale != 2 || top < 0 || bottom < 0 || top + bottom >= height)
		{
			throw new FormatException("Invalid spirit texture registration.");
		}
		return new Variant(obj, width, height, top, bottom, scale);
	}

	private static int ReadInt(JToken token, string key)
	{
		JToken value = token[key];
		if (value == null || value.Type != JTokenType.Integer)
		{
			throw new FormatException("Expected integer spirit field: " + key);
		}
		return value.Value<int>();
	}

	private static float ReadNumber(JToken token, string key)
	{
		JToken value = token[key];
		bool flag;
		if (value != null)
		{
			JTokenType type = value.Type;
			if ((uint)(type - 6) <= 1u)
			{
				flag = true;
				goto IL_001e;
			}
		}
		flag = false;
		goto IL_001e;
		IL_001e:
		if (!flag)
		{
			throw new FormatException("Expected numeric spirit field: " + key);
		}
		return value.Value<float>();
	}

	private static Dictionary<string, EnemySpiritProfile> Load()
	{
		try
		{
			if (FileAccess.FileExists("res://assets/presentation-data/enemy-spirit-profiles.json"))
			{
				return Parse(FileAccess.GetFileAsString("res://assets/presentation-data/enemy-spirit-profiles.json"));
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning("Enemy spirit manifest rejected; using original artwork: " + ex.Message);
		}
		return new Dictionary<string, EnemySpiritProfile>(StringComparer.Ordinal);
	}
}
