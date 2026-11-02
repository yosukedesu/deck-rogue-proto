using System;

namespace DeckRogue.GodotPort.UI;

public static class SourceEnemyUiAnchor
{
	public static float Height(int width, int height, int top, bool phone, bool phone96, string kind)
	{
		float k = (phone ? 2.4f : 4f);
		if (!phone && width == 64 && height == 64)
		{
			k *= ((kind == "boss") ? 1.5f : ((kind == "elite") ? 1.25f : 1f));
		}
		float value = (float)(height - top) * k;
		if (!phone || phone96)
		{
			return value;
		}
		return Math.Min(184f, value);
	}
}
