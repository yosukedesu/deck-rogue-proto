using System;

namespace DeckRogue.GodotPort.UI;

public static class SourceAuthoredHandMotion
{
	public readonly record struct Pose(float PositionFraction, float ScaleFraction, float Alpha, float EndScale);

	public static Pose Sample(double normalized, bool exhaust, float startAlpha)
	{
		double t = (double.IsFinite(normalized) ? Math.Clamp(normalized, 0.0, 1.0) : 0.0);
		float scale = (float)(t * t);
		if (exhaust)
		{
			return new Pose(0f, scale, startAlpha * (float)(1.0 - t), 0.25f);
		}
		return new Pose((float)((t < 0.5) ? (2.0 * t * t) : (1.0 - Math.Pow(-2.0 * t + 2.0, 2.0) / 2.0)), scale, (float)((t < 0.7) ? 1.0 : Math.Clamp(1.0 - (t - 0.7) / 0.3, 0.0, 1.0)), 0.3f);
	}
}
