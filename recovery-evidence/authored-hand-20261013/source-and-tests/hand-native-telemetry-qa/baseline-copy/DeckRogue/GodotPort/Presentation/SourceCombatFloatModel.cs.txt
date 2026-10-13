// Exact timing and value policy from Stage41 Presenter.Show + shared Tween.Float.
#nullable enable
using System;
namespace DeckRogue.GodotPort.Presentation;
public static class SourceCombatFloatModel
{
    public const float Lifetime = .9f;
    public const float PopDuration = .18f;
    public const float Rise = 60f;
    public static int PlayerSize(int amount, bool exposed) => exposed ? 50 : amount >= 20 ? 46 : 36;
    public static string PlayerColor(int amount, int loss, int absorbed) => amount <= 0 ? "c4beb2" : loss <= 0 && absorbed > 0 ? "d6e6fa" : "ead08a";
    public static float Alpha(float age)
    {
        float k = Math.Clamp(age / Lifetime, 0, 1);
        return k < .6f ? 1 : 1 - (k - .6f) / .4f;
    }
    public static float Offset(float age)
    {
        float k = Math.Clamp(age / Lifetime, 0, 1);
        return Rise * (1 - (1-k)*(1-k));
    }
    public static float Scale(float age)
    {
        float k = Math.Clamp(age / PopDuration, 0, 1), u = k - 1;
        float outBack = 1 + 2.70158f*u*u*u + 1.70158f*u*u;
        return .6f + .4f * outBack;
    }
}
