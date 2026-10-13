// Reconstructed presentation seam. StageFx.Cast/At/Where/Tick and the a7f86b5
// reference's cast:slash receipt are evidence, not an exact a7f86b5 source claim.
#nullable enable
using System;
using Godot;
namespace DeckRogue.GodotPort.Presentation;

public readonly record struct SourceCastLightProfile(float Intensity, float ReferenceDistance,
    float Duration, float Hold, float Range, float TowardCamera)
{
    public float Peak => Intensity * ReferenceDistance * ReferenceDistance;
    public bool Valid => float.IsFinite(Peak) && Intensity > 0 && ReferenceDistance > 0
        && float.IsFinite(Duration) && Duration > 0 && float.IsFinite(Hold) && Hold >= 0 && Hold <= .9f
        && float.IsFinite(Range) && Range > 0 && float.IsFinite(TowardCamera) && TowardCamera >= 0;
}

/// One player light, refreshed by a later eligible cast; no new nodes, shadows or RNG.
public sealed class SourceCastLight
{
    SourceCastLightProfile _profile;
    float _started;
    bool _active;
    public Vector3 Position { get; private set; }
    public Vector3 ColorSrgb { get; private set; }
    public float Range => _profile.Range;

    public bool Begin(float now, SourceCastLightProfile profile, Vector3 feet, float height,
        Vector3 right, Vector3 up, Vector3 cameraPosition, Vector3 srgb)
    {
        if (!float.IsFinite(now) || !profile.Valid || !float.IsFinite(height) || height <= 0
            || !feet.IsFinite() || !right.IsFinite() || !up.IsFinite() || !cameraPosition.IsFinite()
            || !srgb.IsFinite() || srgb.X < 0 || srgb.Y < 0 || srgb.Z < 0) return false;
        // Unity UnitPoint("player", (.78,.5)) assumes the board width equals height.
        Vector3 point = feet + up * (height * .5f) + right * (height * .28f);
        Vector3 toCamera = cameraPosition - point;
        if (toCamera.LengthSquared() > .000001f) point += toCamera.Normalized() * profile.TowardCamera;
        Position = point; ColorSrgb = srgb; _profile = profile; _started = now; _active = true;
        return true;
    }
    public float Strength(float now)
    {
        if (!_active || !float.IsFinite(now)) return 0;
        float age = now - _started;
        if (age < 0 || age >= _profile.Duration) return 0;
        float u = age / _profile.Duration;
        float envelope = u <= _profile.Hold ? 1 : (1 - u) / Math.Max(.0001f, 1 - _profile.Hold);
        return _profile.Peak * envelope * envelope;
    }
    public void Clear() => _active = false;

    // Stage41 Presenter suppresses generic cast for metadata-owned vine_slash
    // and contract-owned earth_fissure when authored resources are available.
    // Restore only the three unsuppressed showcase mappings; unknown cards
    // retain their previous path rather than inventing a second cast light.
    public static bool TryColor(string? cardId, out Vector3 color)
    {
        color = cardId switch {
            "green_strike" => new(.86f,.93f,1),
            "blue_strike" => new(.24f,.67f,1),
            "green_finisher_stomp" => new(.65f,.47f,.30f),
            _ => default
        };
        return cardId is "green_strike" or "blue_strike" or "green_finisher_stomp";
    }
}
