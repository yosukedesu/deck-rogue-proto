using System;

namespace DeckRogue.GodotPort.UI;

public static class SourceHandHoverMotion
{
	public const double Duration = 0.12;

	public static float Fraction(double elapsed)
	{
		double t = (double.IsFinite(elapsed) ? Math.Clamp(elapsed / 0.12, 0.0, 1.0) : 0.0);
		return (float)(1.0 - (1.0 - t) * (1.0 - t));
	}
}
