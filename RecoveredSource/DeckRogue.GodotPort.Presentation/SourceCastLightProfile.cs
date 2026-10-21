namespace DeckRogue.GodotPort.Presentation;

public readonly record struct SourceCastLightProfile(float Intensity, float ReferenceDistance, float Duration, float Hold, float Range, float TowardCamera)
{
	public float Peak => Intensity * ReferenceDistance * ReferenceDistance;

	public bool Valid
	{
		get
		{
			if (float.IsFinite(Peak) && Intensity > 0f && ReferenceDistance > 0f && float.IsFinite(Duration) && Duration > 0f && float.IsFinite(Hold) && Hold >= 0f && Hold <= 0.9f && float.IsFinite(Range) && Range > 0f && float.IsFinite(TowardCamera))
			{
				return TowardCamera >= 0f;
			}
			return false;
		}
	}
}
