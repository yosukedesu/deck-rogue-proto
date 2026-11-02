using System;

namespace DeckRogue.GodotPort.UI;

public static class SourceHandReturnMotion
{
	public const double Duration = 0.15;

	private static double T(double elapsed)
	{
		if (!double.IsFinite(elapsed))
		{
			return 0.0;
		}
		return Math.Clamp(elapsed / 0.15, 0.0, 1.0);
	}

	public static float PositionFraction(double elapsed)
	{
		double u = 1.0 - T(elapsed);
		return (float)(1.0 - u * u * u);
	}

	public static float ScaleFraction(double elapsed)
	{
		double t = T(elapsed);
		if (t <= 0.0)
		{
			return 0f;
		}
		if (t >= 1.0)
		{
			return 1f;
		}
		double u = t - 1.0;
		return (float)(1.0 + 2.70158 * u * u * u + 1.70158 * u * u);
	}
}
