// RECONSTRUCTED from the accepted behavior contract and recovered evaluation math.
// Not a byte-identical recovery of the lost b05a0da file. See bundle PROVENANCE.md.
using System;
using System.Linq;
using System.Security.Cryptography;
using Godot;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceActorDisplayTransfer
{
    public const string ProfileId = "act1-source-ldr32-gl-v1";
    public const string Act1LutSha256 = "c17d21ccd08f3b105e00b2effe3e34255aad524203c9c3d458b8ff4dae37cd46";
    public const string Act2LutSha256 = "42cf0b56ab4e69fe27cccd844464a8267fa4564588fa52ac9c5d977f91317261";
    private static readonly JObject[] ExpectedPosts = {
        JObject.Parse("""{"tonemap":"aces","exposure":0.3,"contrast":14,"saturation":0,"colorFilter":[1.0,1.0,1.0],"liftGammaGain":{"lift":[0.955,1.01,1.04,-0.011],"gamma":[1,1,1,0],"gain":[1.02,1.0,0.97,0]},"bloom":{"threshold":1.3,"intensity":0.48,"scatter":0.65,"tint":[1.0,1.0,1.0]},"vignette":{"intensity":0.15,"smoothness":0.45,"center":[0.5,0.52],"rounded":false,"color":[0.0,0.025,0.04]},"filmGrain":false,"chromaticAberration":false}"""),
        JObject.Parse("""{"tonemap":"aces","exposure":0.68,"contrast":14,"saturation":0,"colorFilter":[1.0,1.0,1.0],"liftGammaGain":{"lift":[0.95,1.0,1.02,-0.01],"gamma":[1,1,1,0],"gain":[1.02,1.0,0.97,0]},"bloom":{"threshold":1.15,"intensity":1.2,"scatter":0.8,"tint":[1.0,0.95,0.9]},"vignette":{"intensity":0.15,"smoothness":0.45,"center":[0.5,0.52],"rounded":false,"color":[0.0,0.02,0.025]},"filmGrain":false,"chromaticAberration":false}"""),
        JObject.Parse("""{"tonemap":"aces","exposure":0.5,"contrast":14,"saturation":0,"colorFilter":[1.0,1.0,1.0],"liftGammaGain":{"lift":[0.955,1.01,1.04,-0.011],"gamma":[1,1,1,0],"gain":[1.02,1.0,0.97,0]},"bloom":{"threshold":1.2,"intensity":1.0,"scatter":0.8,"tint":[1.0,1.0,1.0]},"vignette":{"intensity":0.15,"smoothness":0.45,"center":[0.5,0.52],"rounded":false,"color":[0.0,0.02,0.03]},"filmGrain":false,"chromaticAberration":false}"""),
    };
    private static readonly Lazy<byte[]> FirstLut = new(() => GenerateVerifiedLut(1));
    private static readonly Lazy<byte[]> SecondLut = new(() => GenerateVerifiedLut(2));
    public static string GeneratedLutSha256 => GetGeneratedLutSha256(1);
    public static int LutKeyForAct(int act) => act switch { 1 or 3 => 1, 2 => 2, _ => throw new ArgumentOutOfRangeException(nameof(act)) };
    public static string GetProfileId(int act) => act is >= 1 and <= 3 ? $"act{act}-source-ldr32-gl-v1" : "legacy:unverified-act";
    public static JObject CopyExpectedPost(int act) => (JObject)ExpectedPosts[CheckedAct(act)-1].DeepClone();
    private static int CheckedAct(int act) => act is >=1 and <=3 ? act : throw new ArgumentOutOfRangeException(nameof(act));

    // Parameter is the post object, not the full look. Unknown functional keys fail closed.
    public static bool MatchesProfile(int act, JObject? post)
        => act is >=1 and <=3 && post != null && Equivalent(post, ExpectedPosts[act-1]);
    private static bool Equivalent(JToken actual, JToken expected)
    {
        if (actual is JObject a && expected is JObject e) {
            int count = 0;
            foreach (JProperty p in a.Properties()) {
                if (p.Name.StartsWith("_", StringComparison.Ordinal)) continue;
                count++;
                if (!e.TryGetValue(p.Name, StringComparison.Ordinal, out JToken? value) || !Equivalent(p.Value,value)) return false;
            }
            return count == e.Properties().Count(p => !p.Name.StartsWith("_", StringComparison.Ordinal));
        }
        if (actual is JArray aa && expected is JArray ea) {
            if (aa.Count != ea.Count) return false;
            for (int i=0;i<aa.Count;i++) if (!Equivalent(aa[i],ea[i])) return false;
            return true;
        }
        bool an = actual.Type is JTokenType.Integer or JTokenType.Float;
        bool en = expected.Type is JTokenType.Integer or JTokenType.Float;
        if (an && en) return double.IsFinite(actual.Value<double>()) && actual.Value<double>() == expected.Value<double>();
        return JToken.DeepEquals(actual, expected);
    }
    public static SourcePost CreateSourcePost(int act)
    {
        CheckedAct(act);
        SourcePost post = new();
        if (act == 2) { post.ExposureEv = .68f; post.Lift = new(.95f,1f,1.02f,-.01f); post.VignetteColor = new(0f,.02f,.025f); }
        else if (act == 3) { post.ExposureEv = .5f; post.VignetteColor = new(0f,.02f,.03f); }
        return post;
    }
    private static byte[] GenerateVerifiedLut(int act)
    {
        byte[] bytes = new ActorDisplayTransfer(CreateSourcePost(act)).LutRgb;
        string hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        string expected = act == 2 ? Act2LutSha256 : Act1LutSha256;
        if (hash != expected) throw new InvalidOperationException($"Actor display LUT {act} failed its source hash: {hash}");
        return bytes;
    }
    private static byte[] Bytes(int act) => LutKeyForAct(act) == 2 ? SecondLut.Value : FirstLut.Value;
    public static string GetGeneratedLutSha256(int act) => Convert.ToHexString(SHA256.HashData(Bytes(act))).ToLowerInvariant();
    public static byte[] CopyGeneratedLutBytes(int act) => (byte[])Bytes(act).Clone();
    public static ImageTexture CreateLut(int act = 1)
    {
        using Image image = Image.CreateFromData(ActorDisplayTransfer.Width, ActorDisplayTransfer.Height, false, Image.Format.Rgb8, Bytes(act));
        return ImageTexture.CreateFromImage(image);
    }
    public static void BindProfile(ShaderMaterial material, int act)
    {
        SourcePost p = CreateSourcePost(act);
        material.SetShaderParameter("_SourceDisplayOtherProfile", act != 1);
        material.SetShaderParameter("_SourceDisplayExposureEv", p.ExposureEv);
        material.SetShaderParameter("_SourceDisplayVignetteColor", new Vector3(p.VignetteColor.X,p.VignetteColor.Y,p.VignetteColor.Z));
    }
    // Pure seam mirrors the live renderer/environment guard; tested without a native Godot host.
    public static string EvaluateEnvironmentContract(string renderer, int toneMapper, float exposure, float white, bool adjustments, bool attributes)
    {
        if (renderer != "gl_compatibility") return "legacy:unsupported-renderer";
        if (attributes) return "legacy:camera-attributes";
        if (adjustments) return "legacy:environment-adjustments";
        if (toneMapper != (int)Godot.Environment.ToneMapper.Filmic || !float.IsFinite(exposure) || !float.IsFinite(white)
            || Math.Abs(exposure-1.12f)>0.000001f || Math.Abs(white-1f)>0.000001f) return "legacy:environment-transfer";
        return ProfileId;
    }
    public static string EvaluateEnvironment(Camera3D? camera)
    {
        if (camera == null || !GodotObject.IsInstanceValid(camera) || !camera.IsInsideTree()) return "legacy:no-live-camera";
        World3D world = camera.GetWorld3D();
        Godot.Environment? environment = camera.Environment ?? world?.Environment ?? world?.FallbackEnvironment;
        if (environment == null) return "legacy:no-environment";
        return EvaluateEnvironmentContract(RenderingServer.GetCurrentRenderingMethod(), (int)environment.TonemapMode,
            environment.TonemapExposure, environment.TonemapWhite, environment.AdjustmentEnabled,
            camera.Attributes != null || world?.CameraAttributes != null);
    }
}
