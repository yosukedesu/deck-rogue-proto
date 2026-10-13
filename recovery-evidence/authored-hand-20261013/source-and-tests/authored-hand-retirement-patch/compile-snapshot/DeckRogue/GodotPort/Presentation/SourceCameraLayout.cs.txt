using System;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceCameraLayout
{
	private const float SourceFov = 22f;

	private const float SourcePlaneHeight = 10.8f;

	private const float PcContainmentAspect = 1.7777778f;

	public static SourceCameraLayoutResult Compute(Vector2 viewportSize, bool phone)
	{
		if (!float.IsFinite(viewportSize.X) || !float.IsFinite(viewportSize.Y) || viewportSize.X <= 0f || viewportSize.Y <= 0f)
		{
			throw new ArgumentOutOfRangeException("viewportSize", "Camera layout requires a positive, finite viewport size.");
		}
		float aspect = viewportSize.X / viewportSize.Y;
		float planeHeight = (phone ? (10.8f / Mathf.Clamp(aspect / 1.8f, 1f, 1.2f)) : 10.8f);
		float pitch = (phone ? 7f : 5f);
		float groundLine = (phone ? 0.525f : 0.407f);
		float tanHalfFov = Mathf.Tan(Mathf.DegToRad(22f) * 0.5f);
		float referenceDistance = planeHeight * 0.5f / tanHalfFov;
		float unitsPerPixel = planeHeight / viewportSize.Y;
		float dy = (viewportSize.Y * groundLine - viewportSize.Y * 0.5f) * unitsPerPixel;
		float radians = Mathf.DegToRad(pitch);
		float sinPitch = Mathf.Sin(radians);
		float cosPitch = Mathf.Cos(radians);
		Vector3 forward = new Vector3(0f, 0f - sinPitch, 0f - cosPitch);
		Vector3 up = new Vector3(0f, cosPitch, 0f - sinPitch);
		Vector3 position = -up * dy - forward * referenceDistance;
		float fov = 22f;
		if (!phone)
		{
			float containment = Mathf.Max(1f, 1.7777778f / aspect);
			fov = Mathf.RadToDeg(2f * Mathf.Atan(tanHalfFov * containment));
		}
		return new SourceCameraLayoutResult(position, new Vector3(0f - pitch, 0f, 0f), fov, referenceDistance, planeHeight, groundLine);
	}
}
