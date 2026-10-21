using System.Numerics;

namespace DeckRogue.GodotPort.Presentation;

public sealed class SourcePost
{
	public float ExposureEv = 0.3f;

	public float ContrastPercent = 14f;

	public Vector4 Lift = new Vector4(0.955f, 1.01f, 1.04f, -0.011f);

	public Vector4 Gamma = new Vector4(1f, 1f, 1f, 0f);

	public Vector4 Gain = new Vector4(1.02f, 1f, 0.97f, 0f);

	public float VignetteIntensity = 0.15f;

	public float VignetteSmoothness = 0.45f;

	public Vector2 VignetteCenter = new Vector2(0.5f, 0.52f);

	public bool VignetteRounded;

	public Vector3 VignetteColor = new Vector3(0f, 0.025f, 0.04f);

	public bool GammaAboveOneUsesPower22 = true;
}
