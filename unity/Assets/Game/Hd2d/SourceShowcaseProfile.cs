#nullable enable
// dot の Godot 版の SourceShowcaseProfile.cs をそのまま。CardVfxProfile.Showcase と同じ 11 の値 (接触・予備動作・間隔・戻り・余韻・踏み込み・止め×3・重い・当たりの家族)。
namespace DeckRogue.Game.Hd2d
{
public sealed record SourceShowcaseProfile(float Contact, float Windup, float Interval, float Recovery, float Residue, float Reach, float ImpactPause, float ChainPause, float FinalPause, bool Heavy = false, string? AuthoredAttack = null)
{
	public float ImpactPauseFor(int index, int total, bool finishing = false)
	{
		return finishing ? .14f : total > 1 ? (index < total - 1 ? ChainPause : FinalPause) : ImpactPause;
	}

	public float ImpactPauseFor(bool finishing, bool multihit, int index, int total)
	{
		return ImpactPauseFor(index, (!multihit) ? 1 : total, finishing);
	}
}
}
