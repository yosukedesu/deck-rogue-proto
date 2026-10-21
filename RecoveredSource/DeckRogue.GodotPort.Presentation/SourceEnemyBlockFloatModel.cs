using System;
using System.Globalization;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceEnemyBlockFloatModel
{
	public const float Lifetime = 0.7f;

	public const float Rise = 36f;

	public const int FontSize = 28;

	public const string Color = "d6e6fa";

	public static string Text(int amount)
	{
		return "+" + amount.ToString(CultureInfo.InvariantCulture);
	}

	public static Vector2 Origin(Vector2 proxyFeet, float unscaledProxyHeight, float executionScaleY, float unit)
	{
		return proxyFeet + new Vector2(60f * unit, -0.5f * unscaledProxyHeight * executionScaleY - 30f * unit);
	}

	public static float Alpha(float age)
	{
		float k = Math.Clamp(age / 0.7f, 0f, 1f);
		if (!(k < 0.6f))
		{
			return 1f - (k - 0.6f) / 0.4f;
		}
		return 1f;
	}

	public static float Offset(float age)
	{
		float k = Math.Clamp(age / 0.7f, 0f, 1f);
		return 36f * (1f - (1f - k) * (1f - k));
	}

	public static float Scale(float age)
	{
		return SourceCombatFloatModel.Scale(age);
	}

	public static bool Expired(float age, int bornGeneration, int currentGeneration, string route)
	{
		if (!(age >= 0.7f) && bornGeneration == currentGeneration)
		{
			return route != "combat";
		}
		return true;
	}
}
