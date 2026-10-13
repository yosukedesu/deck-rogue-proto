using System;

namespace DeckRogue.GodotPort.UI;

public readonly record struct UiSizing(bool Phone, float LogicalHeight, float PixelHeight, float Dpi)
{
	public float SourceScale => Phone ? Math.Max(0.75f, LogicalHeight / 675f) : 1f;

	public float MinimumHit => Phone ? (48f * SourceScale) : 48f;

	public float MinimumFont => Phone ? (15f * SourceScale) : 13f;

	public float Edge => Phone ? (24f * SourceScale) : 18f;

	public float? EstimatedHitDp => (Dpi >= 72f && Dpi <= 1000f && PixelHeight > 0f && LogicalHeight > 0f) ? new float?(MinimumHit * PixelHeight / LogicalHeight * 160f / Dpi) : ((float?)null);

	public int Font(int requested, float userScale)
	{
		return (int)Math.Ceiling(Math.Max(requested, MinimumFont) * userScale);
	}
}
