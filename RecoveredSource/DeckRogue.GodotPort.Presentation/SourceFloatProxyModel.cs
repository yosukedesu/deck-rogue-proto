using Godot;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceFloatProxyModel
{
	public static float ProjectionUnit(float canvasScale, float logicalHeight, float renderHeight)
	{
		return canvasScale * logicalHeight / renderHeight;
	}

	public static float UnroundedHeight(int baseTextureHeight, float pixelScale, float canvasScale)
	{
		return (float)baseTextureHeight * pixelScale * canvasScale;
	}

	public static Vector2 PlayerDamageOrigin(Vector2 proxyFeet, float unscaledProxyHeight, float executionScaleY, float unit)
	{
		return proxyFeet - new Vector2(0f, unscaledProxyHeight * (0.5f * executionScaleY + 0.1f) + 20f * unit);
	}
}
