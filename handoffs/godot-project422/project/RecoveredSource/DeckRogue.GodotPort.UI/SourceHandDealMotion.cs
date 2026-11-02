using System;

namespace DeckRogue.GodotPort.UI;

public static class SourceHandDealMotion
{
	public readonly record struct Pose(float PositionFraction, float ScaleX, float ScaleY, float Degrees, bool BackVisible, bool Complete);

	public const double Duration = 0.34;

	public const double Stagger = 0.05;

	public const double AfterRemovalDelay = 0.28;

	public const float InitialScale = 0.35f;

	public const float InitialGodotDegrees = 20f;

	public const float MinimumFlipWidth = 0.03f;

	public static double Delay(int newOrdinal, bool removedCards, bool removedPlayedCard)
	{
		return ((removedCards && !removedPlayedCard) ? 0.28 : 0.0) + (double)Math.Max(0, newOrdinal) * 0.05;
	}

	public static Pose Sample(double elapsed, float startScale, float endScale, float startDegrees, float endDegrees)
	{
		double t = Math.Clamp(elapsed / 0.34, 0.0, 1.0);
		double inverse = 1.0 - t;
		float positionFraction = (float)(1.0 - inverse * inverse * inverse);
		float quad = (float)(1.0 - inverse * inverse);
		float scale = startScale + (endScale - startScale) * quad;
		float angle = (endDegrees - startDegrees) % 360f;
		if (angle > 180f)
		{
			angle -= 360f;
		}
		else if (angle < -180f)
		{
			angle += 360f;
		}
		return new Pose(positionFraction, scale * Math.Max(0.03f, (float)Math.Abs(Math.Cos(t * Math.PI))), scale, startDegrees + angle * quad, t < 0.5, t >= 1.0);
	}
}
