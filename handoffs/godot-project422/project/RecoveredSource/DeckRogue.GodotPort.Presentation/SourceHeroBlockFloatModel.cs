using System;
using System.Globalization;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceHeroBlockFloatModel
{
	public const float Lifetime = 0.7f;

	public const float Rise = 40f;

	public const int FontSize = 30;

	public const string Color = "6f95b8";

	public static bool Ordinary(string? target, string? sourceUid, string? receiptTarget, int amount)
	{
		if (target == "player" && sourceUid == null && receiptTarget == "player")
		{
			return amount > 0;
		}
		return false;
	}

	public static bool ExactGain(int amount, int blockBefore, int blockAfter, int hpBefore, int hpAfter, int maxBefore, int maxAfter, int? iceBefore, int? iceAfter)
	{
		if (amount > 0 && (long)blockAfter - (long)blockBefore == amount && hpBefore == hpAfter && maxBefore == maxAfter && iceBefore.HasValue && iceAfter.HasValue)
		{
			return iceBefore == iceAfter;
		}
		return false;
	}

	public static string Text(int amount)
	{
		return "+" + amount.ToString(CultureInfo.InvariantCulture);
	}

	public static Vector2 Origin(Vector2 proxyFeet, float height, float executionScaleY, float unit)
	{
		return proxyFeet + new Vector2(80f * unit, (0f - height) * (0.5f * executionScaleY + 0.3f) - 10f * unit);
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
		return 40f * (1f - (1f - k) * (1f - k));
	}

	public static float Scale(float age)
	{
		return SourceCombatFloatModel.Scale(age);
	}

	public static bool Expired(float age, int bornGeneration, int currentGeneration, string? route)
	{
		if (float.IsFinite(age) && !(age >= 0.7f) && bornGeneration == currentGeneration)
		{
			return route != "combat";
		}
		return true;
	}
}
