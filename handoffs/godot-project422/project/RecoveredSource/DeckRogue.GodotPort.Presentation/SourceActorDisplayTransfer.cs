using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Godot;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceActorDisplayTransfer
{
	public const string ProfileId = "act1-source-ldr32-gl-v1";

	public const string Act1LutSha256 = "c17d21ccd08f3b105e00b2effe3e34255aad524203c9c3d458b8ff4dae37cd46";

	public const string Act2LutSha256 = "42cf0b56ab4e69fe27cccd844464a8267fa4564588fa52ac9c5d977f91317261";

	private static readonly JObject[] ExpectedPosts = new JObject[3]
	{
		JObject.Parse("{\"tonemap\":\"aces\",\"exposure\":0.3,\"contrast\":14,\"saturation\":0,\"colorFilter\":[1.0,1.0,1.0],\"liftGammaGain\":{\"lift\":[0.955,1.01,1.04,-0.011],\"gamma\":[1,1,1,0],\"gain\":[1.02,1.0,0.97,0]},\"bloom\":{\"threshold\":1.3,\"intensity\":0.48,\"scatter\":0.65,\"tint\":[1.0,1.0,1.0]},\"vignette\":{\"intensity\":0.15,\"smoothness\":0.45,\"center\":[0.5,0.52],\"rounded\":false,\"color\":[0.0,0.025,0.04]},\"filmGrain\":false,\"chromaticAberration\":false}"),
		JObject.Parse("{\"tonemap\":\"aces\",\"exposure\":0.68,\"contrast\":14,\"saturation\":0,\"colorFilter\":[1.0,1.0,1.0],\"liftGammaGain\":{\"lift\":[0.95,1.0,1.02,-0.01],\"gamma\":[1,1,1,0],\"gain\":[1.02,1.0,0.97,0]},\"bloom\":{\"threshold\":1.15,\"intensity\":1.2,\"scatter\":0.8,\"tint\":[1.0,0.95,0.9]},\"vignette\":{\"intensity\":0.15,\"smoothness\":0.45,\"center\":[0.5,0.52],\"rounded\":false,\"color\":[0.0,0.02,0.025]},\"filmGrain\":false,\"chromaticAberration\":false}"),
		JObject.Parse("{\"tonemap\":\"aces\",\"exposure\":0.5,\"contrast\":14,\"saturation\":0,\"colorFilter\":[1.0,1.0,1.0],\"liftGammaGain\":{\"lift\":[0.955,1.01,1.04,-0.011],\"gamma\":[1,1,1,0],\"gain\":[1.02,1.0,0.97,0]},\"bloom\":{\"threshold\":1.2,\"intensity\":1.0,\"scatter\":0.8,\"tint\":[1.0,1.0,1.0]},\"vignette\":{\"intensity\":0.15,\"smoothness\":0.45,\"center\":[0.5,0.52],\"rounded\":false,\"color\":[0.0,0.02,0.03]},\"filmGrain\":false,\"chromaticAberration\":false}")
	};

	private static readonly Lazy<byte[]> FirstLut = new Lazy<byte[]>(() => GenerateVerifiedLut(1));

	private static readonly Lazy<byte[]> SecondLut = new Lazy<byte[]>(() => GenerateVerifiedLut(2));

	public static string GeneratedLutSha256 => GetGeneratedLutSha256(1);

	public static int LutKeyForAct(int act)
	{
		switch (act)
		{
		case 1:
		case 3:
			return 1;
		case 2:
			return 2;
		default:
			throw new ArgumentOutOfRangeException("act");
		}
	}

	public static string GetProfileId(int act)
	{
		if (act < 1 || act > 3)
		{
			return "legacy:unverified-act";
		}
		return $"act{act}-source-ldr32-gl-v1";
	}

	public static JObject CopyExpectedPost(int act)
	{
		return (JObject)ExpectedPosts[CheckedAct(act) - 1].DeepClone();
	}

	private static int CheckedAct(int act)
	{
		if (act < 1 || act > 3)
		{
			throw new ArgumentOutOfRangeException("act");
		}
		return act;
	}

	public static bool MatchesProfile(int act, JObject? post)
	{
		if (act >= 1 && act <= 3 && post != null)
		{
			return Equivalent(post, ExpectedPosts[act - 1]);
		}
		return false;
	}

	private static bool Equivalent(JToken actual, JToken expected)
	{
		if (actual is JObject a && expected is JObject e)
		{
			int count = 0;
			foreach (JProperty p in a.Properties())
			{
				if (!p.Name.StartsWith("_", StringComparison.Ordinal))
				{
					count++;
					if (!e.TryGetValue(p.Name, StringComparison.Ordinal, out JToken value) || !Equivalent(p.Value, value))
					{
						return false;
					}
				}
			}
			return count == e.Properties().Count((JProperty jProperty) => !jProperty.Name.StartsWith("_", StringComparison.Ordinal));
		}
		if (actual is JArray aa && expected is JArray ea)
		{
			if (aa.Count != ea.Count)
			{
				return false;
			}
			for (int i = 0; i < aa.Count; i++)
			{
				if (!Equivalent(aa[i], ea[i]))
				{
					return false;
				}
			}
			return true;
		}
		JTokenType type = actual.Type;
		bool flag = (uint)(type - 6) <= 1u;
		bool an = flag;
		type = expected.Type;
		flag = (uint)(type - 6) <= 1u;
		bool en = flag;
		if (an & en)
		{
			if (double.IsFinite(actual.Value<double>()))
			{
				return actual.Value<double>() == expected.Value<double>();
			}
			return false;
		}
		return JToken.DeepEquals(actual, expected);
	}

	public static SourcePost CreateSourcePost(int act)
	{
		CheckedAct(act);
		SourcePost post = new SourcePost();
		switch (act)
		{
		case 2:
			post.ExposureEv = 0.68f;
			post.Lift = new System.Numerics.Vector4(0.95f, 1f, 1.02f, -0.01f);
			post.VignetteColor = new System.Numerics.Vector3(0f, 0.02f, 0.025f);
			break;
		case 3:
			post.ExposureEv = 0.5f;
			post.VignetteColor = new System.Numerics.Vector3(0f, 0.02f, 0.03f);
			break;
		}
		return post;
	}

	private static byte[] GenerateVerifiedLut(int act)
	{
		byte[] lutRgb = new ActorDisplayTransfer(CreateSourcePost(act)).LutRgb;
		string hash = Convert.ToHexString(SHA256.HashData(lutRgb)).ToLowerInvariant();
		string expected = ((act == 2) ? "42cf0b56ab4e69fe27cccd844464a8267fa4564588fa52ac9c5d977f91317261" : "c17d21ccd08f3b105e00b2effe3e34255aad524203c9c3d458b8ff4dae37cd46");
		if (hash != expected)
		{
			throw new InvalidOperationException($"Actor display LUT {act} failed its source hash: {hash}");
		}
		return lutRgb;
	}

	private static byte[] Bytes(int act)
	{
		if (LutKeyForAct(act) != 2)
		{
			return FirstLut.Value;
		}
		return SecondLut.Value;
	}

	public static string GetGeneratedLutSha256(int act)
	{
		return Convert.ToHexString(SHA256.HashData(Bytes(act))).ToLowerInvariant();
	}

	public static byte[] CopyGeneratedLutBytes(int act)
	{
		return (byte[])Bytes(act).Clone();
	}

	public static ImageTexture CreateLut(int act = 1)
	{
		using Image image = Image.CreateFromData(1024, 32, useMipmaps: false, Image.Format.Rgb8, Bytes(act));
		return ImageTexture.CreateFromImage(image);
	}

	public static void BindProfile(ShaderMaterial material, int act)
	{
		SourcePost p = CreateSourcePost(act);
		material.SetShaderParameter("_SourceDisplayOtherProfile", act != 1);
		material.SetShaderParameter("_SourceDisplayExposureEv", p.ExposureEv);
		material.SetShaderParameter("_SourceDisplayVignetteColor", new Godot.Vector3(p.VignetteColor.X, p.VignetteColor.Y, p.VignetteColor.Z));
	}

	public static string EvaluateEnvironmentContract(string renderer, int toneMapper, float exposure, float white, bool adjustments, bool attributes)
	{
		if (renderer != "gl_compatibility")
		{
			return "legacy:unsupported-renderer";
		}
		if (attributes)
		{
			return "legacy:camera-attributes";
		}
		if (adjustments)
		{
			return "legacy:environment-adjustments";
		}
		if (toneMapper != 2 || !float.IsFinite(exposure) || !float.IsFinite(white) || Math.Abs(exposure - 1.12f) > 1E-06f || Math.Abs(white - 1f) > 1E-06f)
		{
			return "legacy:environment-transfer";
		}
		return "act1-source-ldr32-gl-v1";
	}

	public static string EvaluateEnvironment(Camera3D? camera)
	{
		if (camera == null || !GodotObject.IsInstanceValid(camera) || !camera.IsInsideTree())
		{
			return "legacy:no-live-camera";
		}
		World3D world = camera.GetWorld3D();
		Godot.Environment environment = camera.Environment ?? world?.Environment ?? world?.FallbackEnvironment;
		if (environment == null)
		{
			return "legacy:no-environment";
		}
		return EvaluateEnvironmentContract(RenderingServer.GetCurrentRenderingMethod(), (int)environment.TonemapMode, environment.TonemapExposure, environment.TonemapWhite, environment.AdjustmentEnabled, camera.Attributes != null || world?.CameraAttributes != null);
	}
}
