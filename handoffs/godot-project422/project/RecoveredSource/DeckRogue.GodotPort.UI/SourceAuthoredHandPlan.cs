namespace DeckRogue.GodotPort.UI;

public sealed record SourceAuthoredHandPlan(long Epoch, long Revision, string CardUid, string DefinitionId, SourceAuthoredHandStyle Style, double Duration, bool Exhaust, int? AdmittedTargetIndex)
{
	public bool Valid
	{
		get
		{
			if (Epoch >= 0 && Revision >= 0 && Style.Matches(Revision, CardUid, DefinitionId) && double.IsFinite(Duration))
			{
				return Duration > 0.0;
			}
			return false;
		}
	}
}
