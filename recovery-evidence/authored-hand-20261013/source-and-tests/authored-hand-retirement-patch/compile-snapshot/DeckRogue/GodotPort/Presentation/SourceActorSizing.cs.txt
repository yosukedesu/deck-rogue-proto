using System;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceActorSizing
{
	public static SourceActorSizingResult Compute(string category, int textureWidth, int textureHeight, int topMarginRows, bool phone96, string nodeKind, Vector2 viewportSize, float cameraFovDegrees, float referenceDistance, float seatDepth, bool phone, float phoneUiScale = 1.6f)
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
			float nodeTarget = ((nodeKind == "boss") ? 384f : ((nodeKind == "elite") ? 320f : 256f));
			float artDots = MathF.Max(nodeTarget, (float)textureWidth * 4f);
			if (textureHeight < 64)
			{
				artDots = (float)textureHeight * 4f;
			}
			target = artDots * artScale;
			if (phone && !phone96)
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
		float screenHeight = MathF.Round((float)textureHeight * pixelScale * canvasScale, MidpointRounding.ToEven);
		float referencePlaneHeight = 2f * referenceDistance * MathF.Tan(Mathf.DegToRad(cameraFovDegrees) * 0.5f);
		float unitsPerPixel = referencePlaneHeight / viewportSize.Y * seatDepth / referenceDistance;
		float worldHeight = MathF.Max(0.01f, screenHeight * unitsPerPixel);
		return new SourceActorSizingResult(target, pixelScale, screenHeight, worldHeight, canvasScale);
	}

	private static bool PositiveFinite(float value)
	{
		return float.IsFinite(value) && value > 0f;
	}
}
