namespace DeckRogue.GodotPort.Presentation;

public sealed record SourceShowcaseProfile(float Contact, float Windup, float Interval, float Recovery, float Residue, float Reach, float ImpactPause, float ChainPause, float FinalPause, bool Heavy = false, string? AuthoredAttack = null)
{
	public float ImpactPauseFor(int index, int total, bool finishing = false)
	{
		return SourceShowcaseProfiles.ImpactPauseFor(this, index, total, finishing);
	}

	public float ImpactPauseFor(bool finishing, bool multihit, int index, int total)
	{
		return SourceShowcaseProfiles.ImpactPauseFor(this, index, (!multihit) ? 1 : total, finishing);
	}
}
