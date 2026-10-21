using System;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceCombatFloatModel
{
	public const float Lifetime = 0.9f;

	public const float PopDuration = 0.18f;

	public const float Rise = 60f;

	public static int PlayerSize(int amount, bool exposed)
	{
		if (!exposed)
		{
			if (amount < 20)
			{
				return 36;
			}
			return 46;
		}
		return 50;
	}

	public static string PlayerColor(int amount, int loss, int absorbed)
	{
		if (amount > 0)
		{
			if (loss > 0 || absorbed <= 0)
			{
				return "ead08a";
			}
			return "d6e6fa";
		}
		return "c4beb2";
	}

	public static float Alpha(float age)
	{
		float k = Math.Clamp(age / 0.9f, 0f, 1f);
		if (!(k < 0.6f))
		{
			return 1f - (k - 0.6f) / 0.4f;
		}
		return 1f;
	}

	public static float Offset(float age)
	{
		float k = Math.Clamp(age / 0.9f, 0f, 1f);
		return 60f * (1f - (1f - k) * (1f - k));
	}

	public static float Scale(float age)
	{
		float u = Math.Clamp(age / 0.18f, 0f, 1f) - 1f;
		float outBack = 1f + 2.70158f * u * u * u + 1.70158f * u * u;
		return 0.6f + 0.4f * outBack;
	}
}
