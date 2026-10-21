using System;

namespace DeckRogue.GodotPort.UI;

public readonly record struct UiSizing(bool Phone, float LogicalHeight, float PixelHeight, float Dpi)
{
	public float SourceScale
	{
		get
		{
			if (!Phone)
			{
				return 1f;
			}
			return Math.Max(0.75f, LogicalHeight / 675f);
		}
	}

	public float MinimumHit
	{
		get
		{
			if (!Phone)
			{
				return 48f;
			}
			return 48f * SourceScale;
		}
	}

	public float MinimumFont
	{
		get
		{
			if (!Phone)
			{
				return 13f;
			}
			return 15f * SourceScale;
		}
	}

	public float Edge
	{
		get
		{
			if (!Phone)
			{
				return 18f;
			}
			return 24f * SourceScale;
		}
	}

	public float? EstimatedHitDp
	{
		get
		{
			if (!(Dpi >= 72f) || !(Dpi <= 1000f) || !(PixelHeight > 0f) || !(LogicalHeight > 0f))
			{
				return null;
			}
			return MinimumHit * PixelHeight / LogicalHeight * 160f / Dpi;
		}
	}

	public int Font(int requested, float userScale)
	{
		return (int)Math.Ceiling(Math.Max(requested, MinimumFont) * userScale);
	}
}
