using System;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceActorSizing
{
	public static SourceActorSizingResult Compute(string category, int textureWidth, int textureHeight, int topMarginRows, bool phone96, string nodeKind, Vector2 viewportSize, float cameraFovDegrees, float referenceDistance, float seatDepth, bool phone, float phoneUiScale = 1.6f, bool commonPixelGrid = false)
	{
		if (textureWidth <= 0)
		{
			throw new ArgumentOutOfRangeException("textureWidth");
		}
		if (textureHeight <= 0)
		{
			throw new ArgumentOutOfRangeException("textureHeight");
		}
		if (topMarginRows < 0 || topMarginRows > textureHeight)
		{
			throw new ArgumentOutOfRangeException("topMarginRows");
		}
		if (!PositiveFinite(viewportSize.X) || !PositiveFinite(viewportSize.Y))
		{
			throw new ArgumentOutOfRangeException("viewportSize");
		}
		if (!PositiveFinite(cameraFovDegrees) || cameraFovDegrees >= 180f)
		{
			throw new ArgumentOutOfRangeException("cameraFovDegrees");
		}
		if (!PositiveFinite(referenceDistance))
		{
			throw new ArgumentOutOfRangeException("referenceDistance");
		}
		if (!PositiveFinite(seatDepth))
		{
			throw new ArgumentOutOfRangeException("seatDepth");
		}
		if (!PositiveFinite(phoneUiScale))
		{
			throw new ArgumentOutOfRangeException("phoneUiScale");
		}
		float artScale = (phone ? 0.6f : 1f);
		float heroScale = (phone ? 1f : 0.75f);
		float target;
		switch (category)
		{
		case "leader":
		case "leaders":
			target = (float)textureWidth * 4f * artScale * heroScale;
			break;
		case "doll":
		case "dolls":
			target = MathF.Max(32f, MathF.Max(textureWidth, textureHeight)) * 4f * artScale * heroScale;
			break;
		case "enemy":
		case "enemies":
		{
			float x;
			if (nodeKind == "boss")
			{
				x = 384f;
			}
			else
			{
				x = ((nodeKind == "elite") ? 320f : 256f);
			}
			float artDots = commonPixelGrid ? (float)textureWidth * 4f : MathF.Max(x, (float)textureWidth * 4f);
			if (!commonPixelGrid && textureHeight < 64)
			{
				artDots = (float)textureHeight * 4f;
			}
			target = artDots * artScale;
			if (phone && !phone96 && !commonPixelGrid)
			{
				float bodyRows = textureHeight - topMarginRows;
				if (bodyRows > 0f)
				{
					target = MathF.Min(target, 184f * (float)textureWidth / bodyRows);
				}
			}
			break;
		}
		default:
			throw new ArgumentException("Unknown source actor category.", "category");
		}
		float pixelScale = (phone ? MathF.Max(0.5f, target / (float)textureWidth) : MathF.Max(1f, MathF.Round(target / (float)textureWidth, MidpointRounding.ToEven)));
		float ui = (phone ? phoneUiScale : 1f);
		float logWidth = MathF.Log(viewportSize.X / (1920f / ui), 2f);
		float logHeight = MathF.Log(viewportSize.Y / (1080f / ui), 2f);
		float logScale = (phone ? logHeight : (logWidth + (logHeight - logWidth) * 0.5f));
		float canvasScale = MathF.Pow(2f, logScale);
		float screenHeight = commonPixelGrid ? (float)textureHeight * pixelScale * canvasScale : MathF.Round((float)textureHeight * pixelScale * canvasScale, MidpointRounding.ToEven);
		float unitsPerPixel = 2f * referenceDistance * MathF.Tan(Mathf.DegToRad(cameraFovDegrees) * 0.5f) / viewportSize.Y * seatDepth / referenceDistance;
		float worldHeight = MathF.Max(0.01f, screenHeight * unitsPerPixel);
		return new SourceActorSizingResult(target, pixelScale, screenHeight, worldHeight, canvasScale);
	}

	private static bool PositiveFinite(float value)
	{
		if (float.IsFinite(value))
		{
			return value > 0f;
		}
		return false;
	}
}
