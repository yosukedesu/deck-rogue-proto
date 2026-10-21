using System;

namespace DeckRogue.GodotPort.UI;

public static class SourceEnemyHpFit
{
	public static SourceEnemyLedgerModel.HpPlacement ReserveShield(SourceEnemyLedgerModel.HpPlacement source, int hp, float width, float mark, bool hasShield, float textScale, float measuredMaxLabelWidth)
	{
		if (textScale <= 1f || !hasShield || (mark > 0f && mark < 1f))
		{
			return source;
		}
		float left = Math.Min(10f, width);
		float right = Math.Max(left, width - 8f);
		if (measuredMaxLabelWidth <= right - left)
		{
			return source;
		}
		return new SourceEnemyLedgerModel.HpPlacement(Math.Max(0, hp).ToString(), left, right, AlignRight: true, HpOnly: true);
	}

	public static float HorizontalScale(float interval, float nativeMinimum, float textScale, bool hpOnly)
	{
		if (textScale <= 1f || !hpOnly || interval <= 0f || nativeMinimum <= interval)
		{
			return 1f;
		}
		return interval / nativeMinimum;
	}
}
