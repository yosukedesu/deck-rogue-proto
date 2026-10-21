using System;

namespace DeckRogue.GodotPort.UI;

public static class SourceHandExitMotion
{
	public readonly record struct Pose(float PositionFraction, float ScaleFraction, float Alpha, bool Complete);

	public const double Duration = 0.3;

	public const double SyncStagger = 0.04;

	public const float EndScale = 0.3f;

	public static bool IsOrdinaryDiscard(bool played, bool inSet, bool inPermanent, bool inExhaust)
	{
		if (!played && !inSet && !inPermanent)
		{
			return !inExhaust;
		}
		return false;
	}

	public static double Delay(int removedOrdinal)
	{
		return (double)Math.Max(0, removedOrdinal) * 0.04;
	}

	public static Pose Sample(double elapsed)
	{
		double t = (double.IsFinite(elapsed) ? Math.Clamp(elapsed / 0.3, 0.0, 1.0) : 0.0);
		return new Pose((float)((t < 0.5) ? (2.0 * t * t) : (1.0 - Math.Pow(-2.0 * t + 2.0, 2.0) / 2.0)), (float)(t * t), (float)((t < 0.7) ? 1.0 : Math.Clamp(1.0 - (t - 0.7) / 0.3, 0.0, 1.0)), t >= 1.0);
	}
}
