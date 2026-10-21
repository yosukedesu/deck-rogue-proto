using System;

namespace DeckRogue.GodotPort.Presentation;

public static class SpiritProbeIdleMotion
{
	public readonly record struct Offsets(float BodyX, float BodyY, float FinX, float FinY, float TailX, float TailY);

	public const float Period = 2f;

	public static Offsets Sample(float age, bool still = false)
	{
		if (still || !float.IsFinite(age))
		{
			return default;
		}
		float num = (age / 2f - MathF.Floor(age / 2f)) * ((float)Math.PI * 2f);
		float body = MathF.Sin(num);
		float fin = MathF.Sin(num - (float)Math.PI / 3f);
		float tail = MathF.Sin(num - (float)Math.PI * 2f / 3f);
		return new Offsets(0.35f * body, 0.55f * body, 0.6f * fin, 1.2f * fin, 1.5f * tail, 0.7f * tail);
	}
}
