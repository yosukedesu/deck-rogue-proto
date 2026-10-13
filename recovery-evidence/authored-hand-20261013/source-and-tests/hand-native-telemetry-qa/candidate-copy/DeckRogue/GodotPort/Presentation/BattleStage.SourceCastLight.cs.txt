// Presentation-only restoration of the source's bounded actor-side cast light.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json.Linq;
namespace DeckRogue.GodotPort.Presentation;
public partial class BattleStage
{
    readonly SourceCastLight _sourceCastLight = new();
    public bool SourceCastLightEnabled { get; set; } = true;
    public float SourceCastLightStrength => SourceCastLightEnabled && RealLightsEnabled ? _sourceCastLight.Strength(_clock) : 0;
    public int SourceCastLightCount => SourceCastLightStrength > 0 ? 1 : 0;
    public Vector3 SourceCastLightPosition => _sourceCastLight.Position;

    void BeginSourceCastLight(ActionPlayback action)
    {
        if (!SourceCastLightEnabled || !SourceShowcaseEnabled || action.Profile is null || _hero is null
            || !SourceCastLight.TryColor(action.Card?.CardId, out var color)) return;
        bool phone = OS.HasFeature("android") || OS.HasFeature("ios") || OS.GetCmdlineUserArgs().Contains("--ui-phone");
        JToken? tune = ActorMaterialBinding.LoadLook(_act, phone)["hitLight"];
        float Num(string key, float fallback)
        {
            JToken? v = tune?[key];
            if (v?.Type is not (JTokenType.Float or JTokenType.Integer)) return fallback;
            float f = v.Value<float>(); return float.IsFinite(f) && f >= 0 ? f : fallback;
        }
        var profile = new SourceCastLightProfile(Num("intensity", 1) * Num("castMul", .8f),
            Num("refDist", 1.5f), Num("castDur", .3f), Math.Clamp(Num("hold", .3f), 0, .9f),
            Math.Max(.1f, Num("range", 4)), Num("towardCamera", .6f));
        _sourceCastLight.Begin(_clock, profile, _hero.SourceMotionFeet, _hero.SourceMotionHeight,
            _world.Camera.GlobalBasis.X, _world.Camera.GlobalBasis.Y, _world.Camera.GlobalPosition, color);
    }
    void AppendSourceCastLight(List<ActorMaterialBinding.PointLight> lights)
    {
        float strength = SourceCastLightStrength;
        if (strength <= 0 || lights.Count >= 8) return;
        Vector3 c = _sourceCastLight.ColorSrgb;
        Color linear = new Color(c.X, c.Y, c.Z).SrgbToLinear();
        lights.Insert(Math.Min(2, lights.Count), new(_sourceCastLight.Position, _sourceCastLight.Range,
            new Vector3(linear.R, linear.G, linear.B) * strength, IsAttack: true));
    }
    void ClearSourceCastLight() => _sourceCastLight.Clear();
}
