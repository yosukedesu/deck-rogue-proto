using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Presentation;

public static class ActorMaterialSettings
{
	public sealed class Context
	{
		public bool SourceLinearColorSpace = true;

		public Func<float, float> GammaToLinear = UnityGammaToLinearCompatible;

		public float ReceiveOverride = -1f;

		public float HeroLiftOverride = -1f;

		public bool KeyFlipAuto = true;

		public ISet<string> MirroredArts = new HashSet<string>(StringComparer.Ordinal);

		public ISet<string> MeasuredRightArts = new HashSet<string>(StringComparer.Ordinal);

		public bool Diorama;

		public bool MsaaOn;

		public string ActorKey;

		public int EnemyGroup = -1;

		public int DollSlot = -1;

		public float FeetPad;

		public float FrameScaleY = 1f;

		public float EdgeLinear = -1f;

		public ViewInput View;

		public Post PostOverride;
	}

	public sealed class ViewInput
	{
		public float UvX = 0.5f;

		public float UvY = 0.5f;

		public float Aspect = 1.7777778f;

		public float Depth;

		public bool FogEnabled;

		public FogKind FogMode = FogKind.Exp2;

		public float FogStart;

		public float FogEnd;

		public float FogDensity;

		public Vector3 FogColorSrgb;
	}

	public enum FogKind
	{
		Linear,
		Exp,
		Exp2
	}

	public struct UnitView
	{
		public float Vignette;

		public float FogAmount;

		public float FogLum;

		public float CharCf;

		public float StageCf;

		public static UnitView Neutral => new UnitView
		{
			Vignette = 1f,
			CharCf = -1f,
			StageCf = -1f
		};
	}

	public sealed class Post
	{
		public string Tonemap = "aces";

		public float Exposure = 0.3f;

		public float Contrast = 14f;

		public Vector3 ColorFilter = Vector3.One;

		public Vector4 Lift = new Vector4(0.94f, 0.99f, 1.1f, -0.01f);

		public Vector4 Gamma = new Vector4(1f, 1f, 1f, 0f);

		public Vector4 Gain = new Vector4(1.02f, 1f, 0.97f, 0f);

		public float VignetteIntensity = 0.15f;

		public float VignetteSmoothness = 0.45f;

		public Vector2 VignetteCenter = new Vector2(0.5f, 0.52f);

		public bool VignetteRounded;

		public Vector3 VignetteColor = new Vector3(0.01f, 0.03f, 0.07f);
	}

	public sealed class Values
	{
		public string MatchedArtPrefix;

		public float ExposureScale;

		public float Receive;

		public float HeroLift;

		public float AmbientScale;

		public float EmissionIntensity;

		public float Fog;

		public float Saturation;

		public float LocalLights;

		public float WhiteCap;

		public float KeyFlip;

		public float ReceiveShadows;

		public float CookieOnKey;

		public float Rim;

		public float EdgeSoft;

		public float EdgeA2C;

		public float SeatAmbient = 1f;

		public Vector3 CharTint;

		public Vector3 BlackLift;

		public Vector3 OutlineFloor;

		public Vector2 ShadeLift;

		public Vector4 BodyShade;

		public UnitView View;

		public bool HasWhiteCap;

		public bool HasReceiveShadows;

		public bool HasCookieOnKey;

		public bool HasOutlineFloor;
	}

	public static Values Resolve(JObject fullLook, string artName, bool hero, Context context)
	{
		Context ctx = context ?? new Context();
		JObject c = fullLook?["char"] as JObject;
		string prefix;
		JObject a = LongestArt(c?["art"] as JObject, artName, out prefix);
		Post p = ctx.PostOverride ?? ReadPost(fullLook);
		bool hasPost = fullLook != null || ctx.PostOverride != null;
		Values v = new Values
		{
			MatchedArtPrefix = prefix
		};
		float exposureRef = Number(c, "exposureRef", float.NaN);
		v.ExposureScale = ((!hasPost || float.IsNaN(exposureRef)) ? 1f : Pow2(Clamp(exposureRef - p.Exposure, -2f, 2f)));
		float receive = ((ctx.ReceiveOverride >= 0f) ? ctx.ReceiveOverride : (Nonnegative(a, "receive") ? Number(a, "receive", 0f) : ((hero && StageNumber(c, "heroReceive", -1f) >= 0f) ? StageNumber(c, "heroReceive", -1f) : StageNumber(c, "receive", 0.6f))));
		v.Receive = receive * v.ExposureScale;
		float heroLift = ((ctx.HeroLiftOverride >= 0f) ? ctx.HeroLiftOverride : ((hero && Nonnegative(a, "heroLift")) ? Number(a, "heroLift", 0f) : StageNumber(c, "heroLift", 1.2f)));
		v.HeroLift = (hero ? heroLift : 1f);
		v.SeatAmbient = SeatAmbient(c, ctx);
		v.AmbientScale = Choice(a, "ambientScale", StageNumber(c, "ambientScale", 1f)) * v.SeatAmbient * v.ExposureScale;
		v.EmissionIntensity = Choice(a, "emission", Choice(c, "emissionIntensity", 1.6f)) * v.ExposureScale;
		v.Fog = Clamp01(Choice(a, "fog", Choice(c, "fog", 1f)));
		v.Saturation = Choice(a, "saturation", Choice(c, "saturation", 1f));
		v.LocalLights = Choice(a, "localLights", Choice(c, "localLights", 0f));
		v.Rim = Choice(a, "rim", Choice(c, hero ? "heroRim" : (IsDoll(ctx) ? "dollRim" : "rim"), 0.2f));
		v.HasWhiteCap = Nonnegative(a, "whiteCap") || Nonnegative(c, "whiteCap");
		v.WhiteCap = Choice(a, "whiteCap", Choice(c, "whiteCap", 1f));
		v.HasReceiveShadows = Nonnegative(c, "receiveShadows");
		v.ReceiveShadows = Choice(c, "receiveShadows", 0f);
		v.HasCookieOnKey = Nonnegative(c, "cookieOnKey");
		v.CookieOnKey = Choice(c, "cookieOnKey", 1f);
		v.HasOutlineFloor = Read3(c?["outlineFloor"], out var outline);
		v.OutlineFloor = Linear(v.HasOutlineFloor ? outline : new Vector3(0.094f, 0.086f, 0.118f), ctx);
		v.KeyFlip = ((ctx.KeyFlipAuto && ResolveKeyFlip(c, artName, ctx)) ? 1f : 0f);
		v.CharTint = CharTint(c, a, p, hasPost, ctx);
		v.ShadeLift = ShadeLift(c, a);
		v.BodyShade = BodyShade(c, a, hero, ctx);
		v.EdgeSoft = ((ctx.Diorama && ctx.MsaaOn) ? Clamp01(MathF.Max(0f, Number(c, "edgeSoft", -1f))) : 0f);
		v.EdgeA2C = ((v.EdgeSoft > 0f) ? 1f : 0f);
		float target = Number(c, "outlineTarget", float.NaN);
		v.View = ((!float.IsNaN(target) && hasPost && ctx.View != null) ? ViewFor(p, ctx.View, v.Fog, v.CharTint, ctx) : UnitView.Neutral);
		v.BlackLift = BlackLift(c, a, hero, p, hasPost, ctx.EdgeLinear, v.ExposureScale * v.HeroLift, v.ShadeLift, v.View, ctx);
		return v;
	}

	public static JObject LongestArt(JObject table, string artName, out string prefix)
	{
		prefix = null;
		JObject best = null;
		int length = -1;
		if (table == null || string.IsNullOrEmpty(artName))
		{
			return null;
		}
		foreach (JProperty property in table.Properties())
		{
			if (property.Value is JObject obj && !property.Name.StartsWith("_", StringComparison.Ordinal) && property.Name.Length > length && artName.StartsWith(property.Name, StringComparison.Ordinal))
			{
				length = property.Name.Length;
				prefix = property.Name;
				best = obj;
			}
		}
		return best;
	}

	private static bool ResolveKeyFlip(JObject c, string art, Context ctx)
	{
		if (string.IsNullOrEmpty(art))
		{
			return false;
		}
		if (Contains(c?["keyFlipRemove"], art))
		{
			return false;
		}
		if (Contains(c?["keyFlipAdd"], art))
		{
			return true;
		}
		string from = Text(c, "keyFlipFrom", "mirrored").Trim().ToLowerInvariant();
		bool measured = ctx.MeasuredRightArts != null && ctx.MeasuredRightArts.Contains(art);
		bool mirrored = ctx.MirroredArts != null && ctx.MirroredArts.Contains(art);
		return (from == "measured") ? measured : ((from == "both") ? (measured || mirrored) : mirrored);
	}

	private static Vector3 CharTint(JObject c, JObject a, Post p, bool hasPost, Context ctx)
	{
		Vector3 rgb;
		Vector3 t = (Read3(c?["tint"], out rgb) ? Linear(rgb, ctx) : Vector3.One);
		if (Read3(a?["tint"], out rgb))
		{
			t *= Linear(rgb, ctx);
		}
		if (Bool(c, "cancelColorFilter", fallback: false) && hasPost)
		{
			Vector3 cf = Linear(p.ColorFilter, ctx);
			float y = Lum(cf);
			if (cf.X > 0.0001f && cf.Y > 0.0001f && cf.Z > 0.0001f)
			{
				t *= new Vector3(y / cf.X, y / cf.Y, y / cf.Z);
			}
		}
		return t;
	}

	private static Vector2 ShadeLift(JObject c, JObject a)
	{
		if (!Read2(a?["shadeLift"], out var v) && !Read2(c?["shadeLift"], out v))
		{
			return Vector2.Zero;
		}
		return new Vector2(MathF.Max(0f, v.X), MathF.Max(0f, v.Y));
	}

	private static bool LiftColor(JObject c, JObject a, bool hero, out Vector3 value)
	{
		if (Read3(a?["blackLiftColor"], out value))
		{
			return true;
		}
		if (hero && Read3(c?["heroBlackLift"], out value))
		{
			return true;
		}
		return Read3(c?["blackLift"], out value);
	}

	private static Vector3 BlackLift(JObject c, JObject a, bool hero, Post p, bool hasPost, float edge, float litMul, Vector2 shade, UnitView view, Context ctx)
	{
		float target = Number(c, "outlineTarget", float.NaN);
		float autoLit = Number(c?["blackLiftAuto"] as JObject, "litRef", float.NaN);
		if (!float.IsNaN(autoLit))
		{
			autoLit = MathF.Max(0f, autoLit);
		}
		if (!float.IsNaN(target) && hasPost)
		{
			float stageCf = ((view.StageCf > 0f) ? view.StageCf : Lum(Linear(p.ColorFilter, ctx)));
			float charCf = ((view.CharCf > 0f) ? view.CharCf : stageCf);
			float xin = PostGrayInverse(Clamp(target, 0f, 255f), p, view.Vignette, stageCf, ctx);
			float phi = Clamp01(view.FogAmount);
			float z = (xin - phi * view.FogLum) / MathF.Max(0.0001f, (1f - phi) * charCf / MathF.Max(0.0001f, stageCf));
			float litRef = Number(c, "outlineLitRef", float.NaN);
			litRef = ((!float.IsNaN(litRef)) ? MathF.Max(0f, litRef) : ((!float.IsNaN(autoLit)) ? autoLit : 0.8f));
			float albedo = ((edge > 0f) ? (edge * litRef * MathF.Max(0f, litMul)) : 0f);
			if (shade.X > 0f)
			{
				float k = MathF.Max(0.001f, shade.Y);
				float q = k / (k + albedo);
				albedo *= 1f + shade.X * q * q;
			}
			float lift = ((albedo < 0.999f) ? MathF.Max(0f, (z - albedo) / (1f - albedo)) : 0f);
			if (Nonnegative(a, "blackLift"))
			{
				lift *= Number(a, "blackLift", 0f);
			}
			Vector3 hue = Vector3.One;
			if (LiftColor(c, a, hero, out var color))
			{
				Vector3 linear = Linear(color, ctx);
				float y = Lum(linear);
				if (y > 1E-06f)
				{
					hue = linear / y;
				}
			}
			return Clamp01(hue * lift);
		}
		if (!LiftColor(c, a, hero, out var baseColor))
		{
			return Vector3.Zero;
		}
		Vector3 lin = Linear(baseColor, ctx);
		float s = 1f;
		if (hasPost)
		{
			string tm = (p.Tonemap ?? "aces").Trim().ToLowerInvariant();
			if (c?["blackLiftTonemap"] is JObject scales)
			{
				foreach (JProperty prop in scales.Properties())
				{
					if (!prop.Name.StartsWith("_", StringComparison.Ordinal) && prop.Name.Trim().ToLowerInvariant() == tm && IsNumber(prop.Value))
					{
						s = MathF.Max(0f, (float)prop.Value);
					}
				}
			}
			float reference = Number(c, "blackLiftExposureRef", float.NaN);
			if (!float.IsNaN(reference))
			{
				s *= Pow2(Clamp(reference - p.Exposure, -3f, 3f));
			}
		}
		float artScale = 1f;
		if (Nonnegative(a, "blackLift"))
		{
			artScale = Number(a, "blackLift", 0f);
		}
		else if (!float.IsNaN(autoLit) && edge >= 0f)
		{
			float liftY = Lum(lin) * s;
			artScale = ((liftY > 1E-06f) ? Clamp01(1f - edge * autoLit * MathF.Max(0f, litMul) / liftY) : 0f);
		}
		return Clamp01(lin * (s * artScale));
	}

	public static Post ReadPost(JObject fullLook)
	{
		Post p = new Post();
		if (!(fullLook?["post"] is JObject o))
		{
			return p;
		}
		p.Tonemap = Text(o, "tonemap", p.Tonemap);
		p.Exposure = StageNumber(o, "exposure", p.Exposure);
		p.Contrast = StageNumber(o, "contrast", p.Contrast);
		if (ReadStageColor(o["colorFilter"], out var rgb))
		{
			p.ColorFilter = rgb;
		}
		JObject lgg = o["liftGammaGain"] as JObject;
		if (AllNumeric(lgg?["lift"]) && Read4(lgg?["lift"], out var q))
		{
			p.Lift = q;
		}
		else
		{
			q = default(Vector4);
		}
		if (AllNumeric(lgg?["gamma"]) && Read4(lgg?["gamma"], out q))
		{
			p.Gamma = q;
		}
		if (AllNumeric(lgg?["gain"]) && Read4(lgg?["gain"], out q))
		{
			p.Gain = q;
		}
		JObject v = o["vignette"] as JObject;
		p.VignetteIntensity = StageNumber(v, "intensity", p.VignetteIntensity);
		p.VignetteSmoothness = StageNumber(v, "smoothness", p.VignetteSmoothness);
		if (AllNumeric(v?["center"]) && Read2(v?["center"], out var xy))
		{
			p.VignetteCenter = xy;
		}
		p.VignetteRounded = StageBool(v, "rounded", p.VignetteRounded);
		if (ReadStageColor(v?["color"], out rgb))
		{
			p.VignetteColor = rgb;
		}
		return p;
	}

	public static UnitView ViewFor(Post p, ViewInput input, float fogOnUnit, Vector3 charTint, Context ctx)
	{
		UnitView v = UnitView.Neutral;
		if (p.VignetteIntensity > 0f)
		{
			float dx = MathF.Abs(input.UvX - p.VignetteCenter.X) * p.VignetteIntensity * 3f;
			float dy = MathF.Abs(input.UvY - p.VignetteCenter.Y) * p.VignetteIntensity * 3f;
			if (p.VignetteRounded)
			{
				dx *= input.Aspect;
			}
			float vf = MathF.Pow(Clamp01(1f - dx * dx - dy * dy), p.VignetteSmoothness * 5f);
			float color = Lum(Linear(p.VignetteColor, ctx));
			v.Vignette = color + (1f - color) * Clamp01(vf);
		}
		if (input.FogEnabled && fogOnUnit > 0f && input.Depth > 0f)
		{
			float amount;
			if (input.FogMode == FogKind.Linear)
			{
				amount = ((input.FogEnd > input.FogStart) ? Clamp01((input.Depth - input.FogStart) / (input.FogEnd - input.FogStart)) : 0f);
			}
			else if (input.FogMode == FogKind.Exp)
			{
				amount = 1f - MathF.Exp((0f - input.FogDensity) * input.Depth);
			}
			else
			{
				float dd = input.FogDensity * input.Depth;
				amount = 1f - MathF.Exp((0f - dd) * dd);
			}
			v.FogAmount = Clamp01(amount * fogOnUnit);
			v.FogLum = Lum(Linear(input.FogColorSrgb, ctx));
		}
		Vector3 cf = Linear(p.ColorFilter, ctx);
		v.StageCf = Lum(cf);
		v.CharCf = Lum(cf * charTint);
		return v;
	}

	public static float PostGrayInverse(float target, Post p, float vig, float cfScale, Context ctx)
	{
		float lo = -18f;
		float hi = 4f;
		if (PostGrayDisplay(Pow2(lo), p, vig, cfScale, ctx) >= target)
		{
			return 0f;
		}
		if (PostGrayDisplay(Pow2(hi), p, vig, cfScale, ctx) <= target)
		{
			return Pow2(hi);
		}
		for (int i = 0; i < 40; i++)
		{
			float mid = (lo + hi) * 0.5f;
			if (PostGrayDisplay(Pow2(mid), p, vig, cfScale, ctx) < target)
			{
				lo = mid;
			}
			else
			{
				hi = mid;
			}
		}
		return Pow2((lo + hi) * 0.5f);
	}

	public static float PostGrayDisplay(float x, Post p, float vig, float cfScale, Context ctx)
	{
		string tm = (p.Tonemap ?? "aces").Trim().ToLowerInvariant();
		float v = MathF.Max(0f, x) * MathF.Max(0f, vig) * Pow2(p.Exposure);
		float con = p.Contrast / 100f + 1f;
		v = ((!(tm == "aces")) ? MathF.Max(0f, LogCToLinear((LinearToLogC(v) - 0.4135884f) * con + 0.4135884f)) : (0.18f * MathF.Pow(MathF.Max(v, 1.5258789E-05f) / 0.18f, con)));
		Vector3 cf = ((cfScale >= 0f) ? new Vector3(cfScale, cfScale, cfScale) : Linear(p.ColorFilter, ctx));
		PrepareLiftGammaGain(p.Lift, p.Gamma, p.Gain, ctx, out var lift, out var gamma, out var gain);
		float y = 0f;
		for (int i = 0; i < 3; i++)
		{
			float ch = MathF.Max(0f, v * cf[i]);
			ch = ch * gain[i] + lift[i];
			ch = (float)MathF.Sign(ch) * MathF.Pow(MathF.Abs(ch), gamma[i]);
			ch = ((tm == "aces") ? MathF.Pow(AcesFit(ch), 0.9811f) : ((tm == "neutral") ? Neutral(ch) : MathF.Max(0f, ch)));
			y += i switch
			{
				1 => 0.7152f, 
				0 => 0.2126f, 
				_ => 0.0722f, 
			} * SrgbEncode01(ch) * 255f;
		}
		return y;
	}

	private static void PrepareLiftGammaGain(Vector4 il, Vector4 ig, Vector4 inn, Context ctx, out Vector3 lift, out Vector3 gamma, out Vector3 gain)
	{
		Vector3 l = new Vector3(ctx.GammaToLinear(il.X), ctx.GammaToLinear(il.Y), ctx.GammaToLinear(il.Z)) * 0.15f;
		lift = l - Vector3.One * Lum(l) + Vector3.One * il.W;
		Vector3 g = new Vector3(ctx.GammaToLinear(ig.X), ctx.GammaToLinear(ig.Y), ctx.GammaToLinear(ig.Z)) * 0.8f;
		float gy = Lum(g);
		float gw = ig.W + 1f;
		gamma = new Vector3(1f / MathF.Max(g.X - gy + gw, 0.001f), 1f / MathF.Max(g.Y - gy + gw, 0.001f), 1f / MathF.Max(g.Z - gy + gw, 0.001f));
		Vector3 n = new Vector3(ctx.GammaToLinear(inn.X), ctx.GammaToLinear(inn.Y), ctx.GammaToLinear(inn.Z)) * 0.8f;
		gain = n - Vector3.One * Lum(n) + Vector3.One * (inn.W + 1f);
	}

	private static float AcesFit(float x)
	{
		x = MathF.Max(0f, x);
		return MathF.Max(0f, (x * (x + 0.0245786f) - 9.0537E-05f) / (x * (0.983729f * x + 0.432951f) + 0.238081f));
	}

	private static float NeutralCurve(float t)
	{
		return (t * (0.2f * t + 0.06959999f) + 0.00544f) / (t * (0.2f * t + 0.29f) + 0.08160001f) - 0.06666666f;
	}

	private static float Neutral(float x)
	{
		float ws = 1f / NeutralCurve(5.3f);
		return NeutralCurve(MathF.Max(0f, x) * ws) * ws;
	}

	private static float LinearToLogC(float x)
	{
		return 0.244161f * MathF.Log10(MathF.Max(5.555556f * x + 0.047996f, 1E-06f)) + 0.386036f;
	}

	private static float LogCToLinear(float x)
	{
		return (MathF.Pow(10f, (x - 0.386036f) / 0.244161f) - 0.047996f) / 5.555556f;
	}

	private static float SrgbEncode01(float v)
	{
		v = Clamp01(v);
		return (v <= 0.0031308f) ? (v * 12.92f) : (1.055f * MathF.Pow(v, 5f / 12f) - 0.055f);
	}

	private static int EnemyIndex(Context ctx)
	{
		string key = ctx.ActorKey;
		if (key == null || key.Length <= 5 || !key.StartsWith("enemy", StringComparison.Ordinal))
		{
			return -1;
		}
		int n;
		return (int.TryParse(key.Substring(5), out n) && n >= 0) ? n : (-1);
	}

	private static bool IsDoll(Context ctx)
	{
		return ctx.ActorKey != null && ctx.ActorKey.StartsWith("doll:", StringComparison.Ordinal);
	}

	private static float SeatAmbient(JObject c, Context ctx)
	{
		if (!ctx.Diorama || ctx.ActorKey == null || ctx.ActorKey == "player")
		{
			return 1f;
		}
		int enemy = EnemyIndex(ctx);
		if (enemy >= 0 && c?["seatAmbient"] is JArray a)
		{
			List<float> values = new List<float>();
			foreach (JToken t in a)
			{
				if (IsNumber(t))
				{
					values.Add(MathF.Max(0f, (float)t));
				}
			}
			bool any = false;
			foreach (float x in values)
			{
				if (MathF.Abs(x - 1f) > 0.0001f)
				{
					any = true;
					break;
				}
			}
			if (any)
			{
				return values[Math.Min(enemy, values.Count - 1)];
			}
		}
		float back = Number(c, "dollBackAmbient", -1f);
		return (IsDoll(ctx) && ctx.DollSlot >= 5 && back >= 0f && MathF.Abs(back - 1f) > 0.0001f) ? back : 1f;
	}

	private static Vector4 BodyShade(JObject c, JObject a, bool hero, Context ctx)
	{
		float enemyShade = 0f;
		float dollShade = 0f;
		float top = 0.9f;
		float power = 1f;
		int minGroup = 3;
		JToken token = c?["bodyShade"];
		if (IsNumber(token))
		{
			enemyShade = (dollShade = Clamp01((float)token));
		}
		else if (token is JObject o && Bool(o, "on", fallback: true))
		{
			enemyShade = Clamp01(Number(o, "enemy", 0f));
			dollShade = Clamp01(Number(o, "doll", 0f));
			top = Clamp(Number(o, "top", top), 0.05f, 1f);
			power = Clamp(Number(o, "power", power), 0.05f, 8f);
			minGroup = Math.Max(1, (int)MathF.Round(Number(o, "minGroup", minGroup), MidpointRounding.ToEven));
		}
		float strength = 0f;
		int ei = EnemyIndex(ctx);
		int group = ((ctx.EnemyGroup < 0) ? (ei + 1) : ctx.EnemyGroup);
		if (ctx.Diorama && !hero && ctx.ActorKey != null && (IsDoll(ctx) || (ei >= 0 && group >= minGroup)))
		{
			strength = (Nonnegative(a, "bodyShade") ? Clamp01(Number(a, "bodyShade", 0f)) : (IsDoll(ctx) ? dollShade : enemyShade));
		}
		if (strength <= 0f)
		{
			return new Vector4(0f, 0f, 1f, 1f);
		}
		float fsy = MathF.Max(1f, ctx.FrameScaleY);
		float feet = Clamp01(ctx.FeetPad / fsy);
		float end = Clamp(feet + top * (1f - ctx.FeetPad) / fsy, feet + 0.01f, 1f);
		return new Vector4(strength, feet, end, power);
	}

	public static float EdgeDarkLinearFromRgba8(byte[] rgba, int width, int x0, int y0, int x1, int y1)
	{
		if (rgba == null || width <= 0)
		{
			return 0f;
		}
		int height = rgba.Length / (width * 4);
		x0 = Math.Max(0, x0);
		y0 = Math.Max(0, y0);
		x1 = Math.Min(width, x1);
		y1 = Math.Min(height, y1);
		List<float> values = new List<float>();
		for (int i = y0; i < y1; i++)
		{
			for (int j = x0; j < x1; j++)
			{
				if (Opaque(j, i) && (!Opaque(j - 1, i) || !Opaque(j + 1, i) || !Opaque(j, i - 1) || !Opaque(j, i + 1)))
				{
					int i2 = (i * width + j) * 4;
					values.Add(0.2126f * (float)(int)rgba[i2] + 0.7152f * (float)(int)rgba[i2 + 1] + 0.0722f * (float)(int)rgba[i2 + 2]);
				}
			}
		}
		if (values.Count == 0)
		{
			return 0f;
		}
		values.Sort();
		float pos = 0.1f * (float)(values.Count - 1);
		int lo = (int)MathF.Floor(pos);
		int hi = Math.Min(values.Count - 1, lo + 1);
		float v = (values[lo] + (values[hi] - values[lo]) * (pos - (float)lo)) / 255f;
		return (v <= 0.04045f) ? (v / 12.92f) : ((float)Math.Pow(((double)v + 0.055) / 1.055, 2.4));
		bool Opaque(int num, int num2)
		{
			return num >= x0 && num < x1 && num2 >= y0 && num2 < y1 && rgba[(num2 * width + num) * 4 + 3] > 127;
		}
	}

	public static float UnityGammaToLinearCompatible(float x)
	{
		return (x <= 0.04045f) ? (x / 12.92f) : ((x < 1f) ? MathF.Pow((x + 0.055f) / 1.055f, 2.4f) : MathF.Pow(x, 2.2f));
	}

	private static Vector3 Linear(Vector3 v, Context ctx)
	{
		return ctx.SourceLinearColorSpace ? new Vector3(ctx.GammaToLinear(v.X), ctx.GammaToLinear(v.Y), ctx.GammaToLinear(v.Z)) : v;
	}

	private static float Lum(Vector3 v)
	{
		return 0.2126f * v.X + 0.7152f * v.Y + 0.0722f * v.Z;
	}

	private static float Pow2(float v)
	{
		return MathF.Pow(2f, v);
	}

	private static float Clamp(float x, float lo, float hi)
	{
		return (x < lo) ? lo : ((x > hi) ? hi : x);
	}

	private static float Clamp01(float v)
	{
		return Clamp(v, 0f, 1f);
	}

	private static Vector3 Clamp01(Vector3 v)
	{
		return new Vector3(Clamp01(v.X), Clamp01(v.Y), Clamp01(v.Z));
	}

	private static float StageNumber(JObject o, string key, float fallback)
	{
		JToken t = o?[key];
		if (IsNumber(t))
		{
			return (float)t;
		}
		float value;
		return (t != null && t.Type == JTokenType.String && float.TryParse((string?)t, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) ? value : fallback;
	}

	private static bool StageBool(JObject o, string key, bool fallback)
	{
		JToken t = o?[key];
		if (t != null && t.Type == JTokenType.Boolean)
		{
			return (bool)t;
		}
		if (IsNumber(t))
		{
			return (double)t != 0.0;
		}
		if (t != null && t.Type == JTokenType.String)
		{
			string s = ((string?)t).Trim().ToLowerInvariant();
			switch (s)
			{
			default:
				if (!(s == "yes"))
				{
					break;
				}
				goto case "1";
			case "1":
			case "true":
			case "on":
				return true;
			}
			switch (s)
			{
			default:
				if (!(s == "no"))
				{
					break;
				}
				goto case "0";
			case "0":
			case "false":
			case "off":
				return false;
			}
		}
		return fallback;
	}

	private static bool AllNumeric(JToken t)
	{
		if (!(t is JArray a))
		{
			return false;
		}
		foreach (JToken x in a)
		{
			if (!IsNumber(x))
			{
				return false;
			}
		}
		return true;
	}

	private static bool ReadStageColor(JToken t, out Vector3 rgb)
	{
		rgb = Vector3.Zero;
		if (AllNumeric(t))
		{
			return Read3(t, out rgb);
		}
		if (t == null || t.Type != JTokenType.String)
		{
			return false;
		}
		string value = (string?)t;
		if (value == null || !value.StartsWith("#", StringComparison.Ordinal))
		{
			return false;
		}
		string hex = value.Substring(1);
		if (hex.Length == 3 || hex.Length == 4)
		{
			hex = hex[0].ToString() + hex[0] + hex[1] + hex[1] + hex[2] + hex[2];
		}
		else if (hex.Length == 8)
		{
			hex = hex.Substring(0, 6);
		}
		if (hex.Length != 6 || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var packed))
		{
			return false;
		}
		rgb = new Vector3((float)((packed >> 16) & 0xFF) / 255f, (float)((packed >> 8) & 0xFF) / 255f, (float)(packed & 0xFF) / 255f);
		return true;
	}

	private static bool IsNumber(JToken t)
	{
		return t != null && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer);
	}

	private static float Number(JObject o, string key, float fallback)
	{
		return IsNumber(o?[key]) ? ((float)o[key]) : fallback;
	}

	private static bool Nonnegative(JObject o, string key)
	{
		return Number(o, key, -1f) >= 0f;
	}

	private static float Choice(JObject o, string key, float fallback)
	{
		float n = Number(o, key, -1f);
		return (n >= 0f) ? n : fallback;
	}

	private static bool Bool(JObject o, string key, bool fallback)
	{
		return (o != null && o[key]?.Type == JTokenType.Boolean) ? ((bool)o[key]) : fallback;
	}

	private static string Text(JObject o, string key, string fallback)
	{
		return (o != null && o[key]?.Type == JTokenType.String) ? ((string?)o[key]) : fallback;
	}

	private static bool Contains(JToken t, string v)
	{
		if (t is JArray a)
		{
			foreach (JToken x in a)
			{
				if (x.Type == JTokenType.String && (string?)x == v)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool Read2(JToken t, out Vector2 v)
	{
		v = Vector2.Zero;
		if (!(t is JArray { Count: >=2 } a) || !IsNumber(a[0]) || !IsNumber(a[1]))
		{
			return false;
		}
		v = new Vector2((float)a[0], (float)a[1]);
		return true;
	}

	private static bool Read3(JToken t, out Vector3 v)
	{
		v = Vector3.Zero;
		if (!(t is JArray { Count: >=3 } a))
		{
			return false;
		}
		v = new Vector3((float)a[0], (float)a[1], (float)a[2]);
		return true;
	}

	private static bool Read4(JToken t, out Vector4 v)
	{
		v = default(Vector4);
		if (!(t is JArray { Count: >=4 } a))
		{
			return false;
		}
		v = new Vector4((float)a[0], (float)a[1], (float)a[2], (float)a[3]);
		return true;
	}
}
