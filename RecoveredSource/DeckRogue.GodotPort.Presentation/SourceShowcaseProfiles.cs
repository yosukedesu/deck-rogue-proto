namespace DeckRogue.GodotPort.Presentation;

public static class SourceShowcaseProfiles
{
	public static readonly SourceShowcaseProfile Slash = new SourceShowcaseProfile(0.13f, 0.045f, 0.12f, 0.14f, 0.36f, 0.48f, 0.065f, 0.025f, 0.095f, Heavy: false, "slash");

	public static readonly SourceShowcaseProfile Vine = new SourceShowcaseProfile(0.16f, 0.065f, 0.12f, 0.16f, 0.44f, 0.42f, 0.065f, 0.025f, 0.095f);

	public static readonly SourceShowcaseProfile Water = new SourceShowcaseProfile(0.24f, 0.14f, 0.12f, 0.18f, 0.48f, 0.1f, 0.075f, 0.025f, 0.1f, Heavy: false, "water");

	public static readonly SourceShowcaseProfile Earth = new SourceShowcaseProfile(0.3f, 0.19f, 0.15f, 0.22f, 0.58f, 0.1f, 0.11f, 0.025f, 0.11f);

	public static readonly SourceShowcaseProfile Stomp = new SourceShowcaseProfile(0.34f, 0.22f, 0.16f, 0.24f, 0.65f, 0.62f, 0.12f, 0.03f, 0.12f, Heavy: true, "stomp");

	public static SourceShowcaseProfile? ForCard(string? id)
	{
        if (Hd2dAttackPrototype.Enabled && PhysicalVfxProfiles.ForCard(id) is PhysicalVfxProfile physical)
            return physical.Motion;
        if (Hd2dAttackPrototype.Enabled && ElementalVfxProfiles.ForCard(id) is ElementalVfxProfile elemental)
            return elemental.Motion;
		return id switch
		{
			"green_strike" => Hd2dAttackPrototype.Enabled ? PhysicalVfxProfiles.DirectionalCut.Motion : Slash,
			"red_strike" when Hd2dAttackPrototype.Enabled => new SourceShowcaseProfile(.40f,.14f,.12f,.23f,.55f,.06f,.055f,.025f,.075f,false,"fire"),
			"green_sweep" when Hd2dAttackPrototype.Enabled => PhysicalVfxProfiles.Sweep.Motion,
			"green_triple_horn" when Hd2dAttackPrototype.Enabled => PhysicalVfxProfiles.TripleHorn.Motion,
			"green_weak_point" when Hd2dAttackPrototype.Enabled => PhysicalVfxProfiles.AxialThrust.Motion,
			"green_basic_bash" when Hd2dAttackPrototype.Enabled => PhysicalVfxProfiles.BluntImpact.Motion,
			"green_double_lash" => Vine, 
			"blue_strike" => Water, 
			"green_earth_roar" => Earth, 
			"green_finisher_stomp" => Hd2dAttackPrototype.Enabled ? PhysicalVfxProfiles.GroundPressure.Motion : Stomp,
			_ => null, 
		};
	}

	public static float ImpactPauseFor(SourceShowcaseProfile? profile, int index, int total, bool finishing = false)
	{
		if ((object)profile != null)
		{
			if (!finishing)
			{
				if (total <= 1)
				{
					return profile.ImpactPause;
				}
				if (index >= total - 1)
				{
					return profile.FinalPause;
				}
				return profile.ChainPause;
			}
			return 0.14f;
		}
		return 0f;
	}
}
