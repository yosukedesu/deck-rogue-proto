// Stage41 Presenter.BlockGained + shared Tween.Float. Canvas positive Y is up;
// Godot logical viewport Y is down. No actor, receipt, engine or RNG mutation.
#nullable enable
using System;
using System.Globalization;
using Godot;
namespace DeckRogue.GodotPort.Presentation;
public static class SourceEnemyBlockFloatModel
{
    public const float Lifetime = .7f;
    public const float Rise = 36f;
    public const int FontSize = 28;
    public const string Color = "d6e6fa";
    public static string Text(int amount) => "+" + amount.ToString(CultureInfo.InvariantCulture);
    public static Vector2 Origin(Vector2 proxyFeet, float unscaledProxyHeight, float executionScaleY, float unit)
        // EC is the transformed sprite CENTER, unlike damage EF + .1H + 20.
        // The caller's +60,+30 offset is in unscaled FX canvas coordinates.
        => proxyFeet + new Vector2(60f * unit, -.5f * unscaledProxyHeight * executionScaleY - 30f * unit);
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
    public static float Scale(float age) => SourceCombatFloatModel.Scale(age);
    public static bool Expired(float age, int bornGeneration, int currentGeneration, string route)
        => age >= Lifetime || bornGeneration != currentGeneration || route != "combat";
}
