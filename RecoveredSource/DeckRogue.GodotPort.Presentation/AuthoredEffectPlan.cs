using DeckRogue.Game;

namespace DeckRogue.GodotPort.Presentation;

public static class AuthoredEffectPlan
{
	public static (float lead, float end) Times(AttackRoute route, EffectTier tier)
	{
		string f = route.Family;
		string t = tier.ToString().ToLowerInvariant();
		var original = route.Renderer switch
		{
			"PhysicalSlashFx" => (lead: (float)PhysicalSlashGeometry.Lead(t), end: (float)PhysicalSlashGeometry.End(t)), 
			"VineSlashFx" => (lead: 1f / 15f, end: 0.182f), 
			"FireContinuousFx" => (lead: (float)FireContinuousGeometry.Lead((f == "fire") ? "projectile" : "localburst"), end: (float)FireContinuousGeometry.End((f == "fire") ? "projectile" : "localburst")), 
			"WaterContinuousFx" => (lead: (float)WaterContinuousGeometry.Lead(), end: (float)WaterContinuousGeometry.End()), 
			"IceCrystalFx" => (lead: (float)IceCrystalGeometry.Lead(), end: (float)IceCrystalGeometry.End()), 
			"LightRayFx" => (lead: (float)LightRayGeometry.Lead(), end: (float)LightRayGeometry.End()), 
			"DarkDrainFx" => (lead: (float)DarkDrainGeometry.Lead(), end: (float)DarkDrainGeometry.End()), 
			"LightningDischargeFx" => (lead: (float)LightningDischargeGeometry.Lead(), end: (float)LightningDischargeGeometry.End()), 
			"EnemyContactFx" => (lead: (f == "enemy_claw") ? 0.056f : ((f == "enemy_beam") ? 0.1f : 0f), end: (float)EnemyContactGeometry.End(f.Replace("enemy_", ""))), 
			"PlayerContactFx" => (lead: 0.13f, end: (float)PlayerContactGeometry.End((f == "physical_meteor") ? "meteor" : "pierce")), 
			"BoundedContactFx" => (lead: 0f, end: 0.18f), 
			"EarthStompFx" => (lead: (float)EarthStompGeometry.Lead((!(f == "earth_fissure")) ? 1 : 0), end: (float)EarthStompGeometry.End((!(f == "earth_fissure")) ? 1 : 0)), 
			"WideProfileFx" => (lead: (float)WideProfileGeometry.Lead(), end: (float)WideProfileGeometry.End((!(f == "wide_water")) ? WideProfileGeometry.Profile.Fire : WideProfileGeometry.Profile.Water)), 
			"ScatterProfileFx" => (lead: 0f, end: (float)ScatterProfileGeometry.End(Scatter(f))), 
			"LocalMaterialProfileFx" => (lead: 0f, end: (float)LocalMaterialProfileGeometry.End(Local(f))), 
			"SystemOutcomeFx" => (lead: 0f, end: (float)SystemOutcomeGeometry.End(f)), 
			"HealContinuousFx" => (lead: (float)HealContinuousGeometry.Lead(), end: (float)HealContinuousGeometry.End()), 
			_ => (lead: 0.1f, end: 0.24f), 
		};
        return Hd2dAttackPrototype.SupportsRoute(route) ? (Hd2dAttackPrototype.Lead(route.Key), Hd2dAttackPrototype.Tail(route.Key)) : original;
	}

	internal static ScatterProfileGeometry.Profile Scatter(string f)
	{
		return f switch
		{
			"scatter_ice" => ScatterProfileGeometry.Profile.Ice, 
			"scatter_spore" => ScatterProfileGeometry.Profile.Spore, 
			"scatter_warm_spark" => ScatterProfileGeometry.Profile.WarmSpark, 
			_ => ScatterProfileGeometry.Profile.Fire, 
		};
	}

	internal static LocalMaterialProfileGeometry.Profile Local(string f)
	{
		return f switch
		{
			"local_dark" => LocalMaterialProfileGeometry.Profile.Dark, 
			"local_light" => LocalMaterialProfileGeometry.Profile.Light, 
			"local_botanical" => LocalMaterialProfileGeometry.Profile.Botanical, 
			"local_water_flow" => LocalMaterialProfileGeometry.Profile.Water, 
			_ => LocalMaterialProfileGeometry.Profile.Physical, 
		};
	}
}
