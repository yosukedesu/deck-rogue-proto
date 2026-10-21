using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Presentation;

public sealed class EnemyAttackRigProfile
{
	public readonly record struct Pose(float Rotation, Vector2 Translation, Vector2 Scale)
	{
		public static Pose Identity => new Pose(0f, Vector2.Zero, Vector2.One);

		public static Pose Lerp(Pose a, Pose b, float t)
		{
			return new Pose(a.Rotation + (b.Rotation - a.Rotation) * t, a.Translation.Lerp(b.Translation, t), a.Scale.Lerp(b.Scale, t));
		}
	}

	public sealed record Joint(string Name, Vector2 Pivot, Vector2[][] Polygons, Vector2[][] Exclusions, float Feather, float PinRadius, float Lag, Pose Windup, Pose Strike, Pose Recoil, Pose Settle);

	public sealed record LayerPart(string Name, int JointIndex, int Channel, float Depth);

	public sealed record LayerArt(string Mask, string Underpaint, LayerPart[] Parts, float LungeScale, bool StableRest);

	public sealed record Clip(Pose[] Windup, Pose[] Strike, Pose[] Recoil, Pose[] Settle, float Recovery);

	private readonly Dictionary<string, EnemyAttackRigProfile> _variants = new Dictionary<string, EnemyAttackRigProfile>(StringComparer.Ordinal);

	private const string Path = "res://assets/presentation-data/enemy-attack-rigs.json";

	private static Dictionary<string, EnemyAttackRigProfile>? _profiles;

	public LayerArt? Layers { get; private set; }

	public string Id { get; }

	public string TexturePath { get; }

	public int TextureWidth { get; }

	public int TextureHeight { get; }

	public Joint[] Joints { get; }

	public Clip DefaultClip { get; }

	public IReadOnlyDictionary<string, Clip> Moves { get; }

	public int Subdivisions { get; }

	public string ReleaseMode { get; }

	private EnemyAttackRigProfile(string id, Joint[] joints, Clip clip, Dictionary<string, Clip> moves, int subdivisions, string releaseMode, string texturePath, int textureWidth, int textureHeight)
	{
		Id = id;
		Joints = joints;
		DefaultClip = clip;
		Moves = moves;
		Subdivisions = subdivisions;
		ReleaseMode = releaseMode;
		TexturePath = texturePath;
		TextureWidth = textureWidth;
		TextureHeight = textureHeight;
	}

	public Clip ForMove(string? moveId)
	{
		if (moveId == null || !Moves.TryGetValue(moveId, out Clip clip))
		{
			return DefaultClip;
		}
		return clip;
	}

	public float Weight(int joint, Vector2 point)
	{
		Joint j = Joints[joint];
		float distance = float.NegativeInfinity;
		Vector2[][] polygons = j.Polygons;
		foreach (Vector2[] polygon in polygons)
		{
			distance = Math.Max(distance, SignedDistance(point, polygon));
		}
		float weight = Smooth((distance + j.Feather) / Math.Max(1E-05f, j.Feather));
		polygons = j.Exclusions;
		foreach (Vector2[] polygon2 in polygons)
		{
			weight *= 1f - Smooth((SignedDistance(point, polygon2) + j.Feather) / Math.Max(1E-05f, j.Feather));
		}
		if (j.PinRadius > 0f)
		{
			weight *= Smooth(point.DistanceTo(j.Pivot) / j.PinRadius);
		}
		return weight;
	}

	private static float SignedDistance(Vector2 p, Vector2[] polygon)
	{
		float distance = float.PositiveInfinity;
		bool inside = false;
		int i = 0;
		int last = polygon.Length - 1;
		while (i < polygon.Length)
		{
			Vector2 a = polygon[last];
			Vector2 b = polygon[i];
			Vector2 ab = b - a;
			float q = Math.Clamp((p - a).Dot(ab) / Math.Max(1E-08f, ab.LengthSquared()), 0f, 1f);
			distance = Math.Min(distance, p.DistanceTo(a + ab * q));
			if (a.Y > p.Y != b.Y > p.Y && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
			{
				inside = !inside;
			}
			last = i++;
		}
		if (!inside)
		{
			return 0f - distance;
		}
		return distance;
	}

	internal static float Smooth(float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		return t * t * (3f - 2f * t);
	}

	public static bool TryGet(string id, out EnemyAttackRigProfile? profile)
	{
		if (_profiles == null)
		{
			_profiles = Load();
		}
		return _profiles.TryGetValue(id, out profile);
	}

	public static bool TryGet(string id, string texturePath, int width, int height, out EnemyAttackRigProfile? profile)
	{
		profile = null;
		if (!TryGet(id, out EnemyAttackRigProfile root) || root == null)
		{
			return false;
		}
		EnemyAttackRigProfile candidate = ((root.TexturePath == texturePath) ? root : root._variants.GetValueOrDefault(texturePath));
		if (candidate == null || candidate.TextureWidth != width || candidate.TextureHeight != height)
		{
			return false;
		}
		profile = candidate;
		return true;
	}

	private static Dictionary<string, EnemyAttackRigProfile> Load()
	{
		try
		{
			if (FileAccess.FileExists("res://assets/presentation-data/enemy-attack-rigs.json"))
			{
				return Parse(FileAccess.GetFileAsString("res://assets/presentation-data/enemy-attack-rigs.json"));
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning("Enemy attack rigs rejected; preserving original motion: " + ex.Message);
		}
		return new Dictionary<string, EnemyAttackRigProfile>(StringComparer.Ordinal);
	}

	public static Dictionary<string, EnemyAttackRigProfile> Parse(string json)
	{
		JObject root = JObject.Parse(json);
		JToken? jToken = root["format_version"];
		if (jToken == null || jToken.Type != JTokenType.Integer || root["format_version"].Value<int>() != 1 || !(root["profiles"] is JArray entries))
		{
			throw new FormatException("Invalid enemy attack rig version.");
		}
		Dictionary<string, EnemyAttackRigProfile> result = new Dictionary<string, EnemyAttackRigProfile>(StringComparer.Ordinal);
		foreach (JToken entry in entries)
		{
			string id = ((string?)entry["id"]) ?? "";
			if (!id.StartsWith("enemy_", StringComparison.Ordinal) || id.Length > 80)
			{
				throw new FormatException("Invalid attack rig ID.");
			}
			string text = id;
			foreach (char c in text)
			{
				if ((c < 'a' || c > 'z') && (c < '0' || c > '9') && c != '_')
				{
					throw new FormatException("Invalid attack rig ID character.");
				}
			}
			string texturePath = ((string?)entry["texture_path"]) ?? "";
			int textureWidth = ((int?)entry["texture_width"]).GetValueOrDefault();
			int textureHeight = ((int?)entry["texture_height"]).GetValueOrDefault();
			if (!texturePath.StartsWith("res://assets/Art/enemies/", StringComparison.Ordinal) || !texturePath.EndsWith(".png", StringComparison.Ordinal) || texturePath.Contains("..", StringComparison.Ordinal) || textureWidth < 32 || textureWidth > 512 || textureHeight != textureWidth)
			{
				throw new FormatException("Invalid attack texture registration.");
			}
			if (entry["coordinate_space"]?.Value<string>() != "normalized_native_top_left")
			{
				throw new FormatException("Attack rig coordinates must be native PNG coordinates.");
			}
			if (!(entry["joints"] is JArray { Count: >=1, Count: <=6 } rows))
			{
				throw new FormatException("Attack rigs require one to six joints.");
			}
			string releaseMode = ((string?)entry["release_mode"]) ?? "contact";
			switch (releaseMode)
			{
			default:
				throw new FormatException("Invalid attack release mode.");
			case "contact":
			case "preview_recoil":
			case "preview_hold":
			{
				int subdivisions = ((int?)entry["subdivisions"]) ?? 64;
				if (subdivisions < 32 || subdivisions > 96)
				{
					throw new FormatException("Invalid attack mesh density.");
				}
				HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
				Joint[] joints = new Joint[rows.Count];
				for (int j = 0; j < rows.Count; j++)
				{
					JToken row = rows[j];
					string name = ((string?)row["name"]) ?? "";
					if (name.Length < 1 || name.Length > 80 || !names.Add(name))
					{
						throw new FormatException("Invalid or duplicate attack joint.");
					}
					joints[j] = new Joint(name, Point(row["pivot"]), Polygons(row["polygons"], required: true), Polygons(row["exclusions"], required: false), Number(row, "feather", 0.012f, 0.001f, 0.2f), Number(row, "pin_radius", 0.02f, 0f, 0.15f), Number(row, "lag_seconds", 0f, 0f, 0.08f), ReadPose(row["windup"]), ReadPose(row["strike"]), ReadPose(row["recoil"]), ReadPose(row["settle"]));
				}
				float recovery = Number(entry, "recovery_seconds", 0.22f, 0.12f, 0.28f);
				Clip clip = MakeClip(joints, recovery);
				Dictionary<string, Clip> moves = new Dictionary<string, Clip>(StringComparer.Ordinal);
				if (entry["move_overrides"] is JObject overrides)
				{
					foreach (JProperty prop in overrides.Properties())
					{
						if (prop.Name.Length < 1 || prop.Name.Length > 80 || !(prop.Value is JObject value))
						{
							throw new FormatException("Invalid attack move override.");
						}
						Pose[] w = (Pose[])clip.Windup.Clone();
						Pose[] s = (Pose[])clip.Strike.Clone();
						Pose[] r = (Pose[])clip.Recoil.Clone();
						Pose[] z = (Pose[])clip.Settle.Clone();
						foreach (JProperty part in ((value["joints"] as JObject) ?? throw new FormatException("Attack move override requires named joints.")).Properties())
						{
							int index = Array.FindIndex(joints, (Joint joint2) => joint2.Name == part.Name);
							if (index < 0)
							{
								throw new FormatException("Attack move overrides an unknown joint.");
							}
							JToken a = part.Value["windup"];
							if (a != null)
							{
								w[index] = ReadPose(a);
							}
							JToken b = part.Value["strike"];
							if (b != null)
							{
								s[index] = ReadPose(b);
							}
							JToken c2 = part.Value["recoil"];
							if (c2 != null)
							{
								r[index] = ReadPose(c2);
							}
							JToken d = part.Value["settle"];
							if (d != null)
							{
								z[index] = ReadPose(d);
							}
						}
						moves.Add(prop.Name, new Clip(w, s, r, z, Number(value, "recovery_seconds", recovery, 0.12f, 0.28f)));
					}
				}
				EnemyAttackRigProfile profile = new EnemyAttackRigProfile(id, joints, clip, moves, subdivisions, releaseMode, texturePath, textureWidth, textureHeight);
				if (entry["variants"] != null && !(entry["variants"] is JArray))
				{
					throw new FormatException("Invalid attack variants.");
				}
				if (entry["variants"] is JArray variants)
				{
					if (variants.Count > 3)
					{
						throw new FormatException("Too many attack variants.");
					}
					foreach (JToken item in variants)
					{
						if (!(item is JObject fields) || fields["variants"] != null || fields["id"] != null)
						{
							throw new FormatException("Invalid nested attack variant.");
						}
						JObject merged = (JObject)entry.DeepClone();
						merged.Remove("variants");
						foreach (JProperty field in fields.Properties())
						{
							merged[field.Name] = field.Value.DeepClone();
						}
						EnemyAttackRigProfile parsed = Parse(new JObject
						{
							["format_version"] = 1,
							["profiles"] = new JArray(merged)
						}.ToString())[id];
						if (parsed.TexturePath == texturePath || !profile._variants.TryAdd(parsed.TexturePath, parsed))
						{
							throw new FormatException("Duplicate attack texture variant.");
						}
						if (parsed.Joints.Length != joints.Length)
						{
							throw new FormatException("Attack variant changes joint topology.");
						}
						for (int j2 = 0; j2 < joints.Length; j2++)
						{
							if (parsed.Joints[j2].Name != joints[j2].Name)
							{
								throw new FormatException("Attack variant changes joint identity.");
							}
						}
					}
				}
				if (entry["layered_art"] != null)
				{
					JToken jToken2 = entry["layered_art"];
					JObject art = jToken2 as JObject;
					if (art == null || !(art["parts"] is JArray { Count: >=1, Count: <=3 } parts))
					{
						throw new FormatException("Invalid layered attack artwork.");
					}
					LayerPart[] parsedParts = new LayerPart[parts.Count];
					HashSet<int> channels = new HashSet<int>();
					for (int i2 = 0; i2 < parts.Count; i2++)
					{
						JToken part2 = parts[i2];
						string name2 = ((string?)part2["name"]) ?? "";
						string joint = ((string?)part2["joint"]) ?? "";
						int channel = ((int?)part2["channel"]) ?? (-1);
						int index2 = Array.FindIndex(joints, (Joint joint2) => joint2.Name == joint);
						if (name2.Length < 1 || name2.Length > 80 || index2 < 0 || channel < 0 || channel > 2 || !channels.Add(channel))
						{
							throw new FormatException("Invalid layered attack part.");
						}
						parsedParts[i2] = new LayerPart(name2, index2, channel, Number(part2, "depth", 0.0002f, 0.0001f, 0.005f));
					}
					if (art["stable_rest"] != null && art["stable_rest"].Type != JTokenType.Boolean)
					{
						throw new FormatException("Invalid layered rest mode.");
					}
					// Recovered from original <Parse>g__Asset|47_1 IL (RVA 0xbf308).
                    string Asset(string key)
                    {
                        string value = (string?)art[key] ?? "";
                        if (!value.StartsWith("res://assets/Art/enemies/attack_layers/" + id + "/", StringComparison.Ordinal) || !value.EndsWith(".png", StringComparison.Ordinal) || value.Contains("..", StringComparison.Ordinal))
                            throw new FormatException("Invalid layered attack resource path.");
                        return value;
                    }
                    profile.Layers = new LayerArt(Asset("mask"), Asset("underpaint"), parsedParts, Number(art, "visual_lunge_scale", 1f, 0f, 1f), (bool?)art["stable_rest"] == true);
				}
				if (!result.TryAdd(id, profile))
				{
					throw new FormatException("Duplicate attack rig ID.");
				}
				break;
			}
			}
		}
		return result;
	}

	private static Clip MakeClip(Joint[] joints, float recovery)
	{
		Pose[] w = new Pose[joints.Length];
		Pose[] s = new Pose[joints.Length];
		Pose[] r = new Pose[joints.Length];
		Pose[] z = new Pose[joints.Length];
		for (int i = 0; i < joints.Length; i++)
		{
			w[i] = joints[i].Windup;
			s[i] = joints[i].Strike;
			r[i] = joints[i].Recoil;
			z[i] = joints[i].Settle;
		}
		return new Clip(w, s, r, z, recovery);
	}

	private static Pose ReadPose(JToken? t)
	{
		if (t == null)
		{
			return Pose.Identity;
		}
		if (!(t is JObject))
		{
			throw new FormatException("Invalid attack pose.");
		}
		float num = Number(t, "rotation_degrees", 0f, -30f, 30f);
		JToken tr = t["translation_64"];
		Vector2 translation = ((tr != null) ? (Pair(tr, -8f, 8f) / 64f) : Vector2.Zero);
		JToken sc = t["scale"];
		return new Pose(Scale: (sc != null) ? Pair(sc, 0.72f, 1.28f) : Vector2.One, Rotation: num * (float)Math.PI / 180f, Translation: translation);
	}

	private static Vector2 Point(JToken? t)
	{
		return Pair(t, 0f, 1f);
	}

	private static Vector2 Pair(JToken? t, float min, float max)
	{
		if (!(t is JArray { Count: 2 } a))
		{
			throw new FormatException("Invalid attack coordinate pair.");
		}
		return new Vector2(Scalar(a[0], min, max), Scalar(a[1], min, max));
	}

	private static Vector2[][] Polygons(JToken? t, bool required)
	{
		if (t == null && !required)
		{
			return Array.Empty<Vector2[]>();
		}
		if (!(t is JArray rows) || rows.Count < (required ? 1 : 0) || rows.Count > 24)
		{
			throw new FormatException("Invalid attack polygons.");
		}
		Vector2[][] result = new Vector2[rows.Count][];
		for (int i = 0; i < rows.Count; i++)
		{
			if (!(rows[i] is JArray { Count: >=3, Count: <=80 } points))
			{
				throw new FormatException("Invalid attack polygon points.");
			}
			result[i] = new Vector2[points.Count];
			for (int j = 0; j < points.Count; j++)
			{
				result[i][j] = Pair(points[j], -0.25f, 1.25f);
			}
		}
		return result;
	}

	private static float Number(JToken token, string key, float fallback, float min, float max)
	{
		JToken value = token[key];
		if (value == null)
		{
			return fallback;
		}
		return Scalar(value, min, max);
	}

	private static float Scalar(JToken t, float min, float max)
	{
		JTokenType type = t.Type;
		if (type != JTokenType.Float && type != JTokenType.Integer)
		{
			throw new FormatException("Non-numeric attack rig field.");
		}
		float value = t.Value<float>();
		if (!float.IsFinite(value) || value < min || value > max)
		{
			throw new FormatException("Out-of-bounds attack rig field.");
		}
		return value;
	}
}
