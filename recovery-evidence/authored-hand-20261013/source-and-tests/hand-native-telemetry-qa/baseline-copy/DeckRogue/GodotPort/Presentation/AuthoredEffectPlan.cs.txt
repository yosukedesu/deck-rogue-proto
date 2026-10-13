using DeckRogue.Game;

namespace DeckRogue.GodotPort.Presentation;

public static class AuthoredEffectPlan
{
	public static (float lead, float end) Times(AttackRoute route, EffectTier tier)
	{
		string f = route.Family;
		string t = tier.ToString().ToLowerInvariant();
		string renderer = route.Renderer;
		if (1 == 0)
		{
		}
		(float, float) result = renderer switch
		{
			"PhysicalSlashFx" => ((float)PhysicalSlashGeometry.Lead(t), (float)PhysicalSlashGeometry.End(t)), 
			"VineSlashFx" => (1f / 15f, 0.182f), 
			"FireContinuousFx" => ((float)FireContinuousGeometry.Lead((f == "fire") ? "projectile" : "localburst"), (float)FireContinuousGeometry.End((f == "fire") ? "projectile" : "localburst")), 
			"WaterContinuousFx" => ((float)WaterContinuousGeometry.Lead(), (float)WaterContinuousGeometry.End()), 
			"IceCrystalFx" => ((float)IceCrystalGeometry.Lead(), (float)IceCrystalGeometry.End()), 
			"LightRayFx" => ((float)LightRayGeometry.Lead(), (float)LightRayGeometry.End()), 
			"DarkDrainFx" => ((float)DarkDrainGeometry.Lead(), (float)DarkDrainGeometry.End()), 
			"LightningDischargeFx" => ((float)LightningDischargeGeometry.Lead(), (float)LightningDischargeGeometry.End()), 
			"EnemyContactFx" => ((f == "enemy_claw") ? 0.056f : ((f == "enemy_beam") ? 0.1f : 0f), (float)EnemyContactGeometry.End(f.Replace("enemy_", ""))), 
			"PlayerContactFx" => (0.13f, (float)PlayerContactGeometry.End((f == "physical_meteor") ? "meteor" : "pierce")), 
			"BoundedContactFx" => (0f, 0.18f), 
			"EarthStompFx" => ((float)EarthStompGeometry.Lead((!(f == "earth_fissure")) ? 1 : 0), (float)EarthStompGeometry.End((!(f == "earth_fissure")) ? 1 : 0)), 
			"WideProfileFx" => ((float)WideProfileGeometry.Lead(), (float)WideProfileGeometry.End((!(f == "wide_water")) ? WideProfileGeometry.Profile.Fire : WideProfileGeometry.Profile.Water)), 
			"ScatterProfileFx" => (0f, (float)ScatterProfileGeometry.End(Scatter(f))), 
			"LocalMaterialProfileFx" => (0f, (float)LocalMaterialProfileGeometry.End(Local(f))), 
			"SystemOutcomeFx" => (0f, (float)SystemOutcomeGeometry.End(f)), 
			"HealContinuousFx" => ((float)HealContinuousGeometry.Lead(), (float)HealContinuousGeometry.End()), 
			_ => (0.1f, 0.24f), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	internal static ScatterProfileGeometry.Profile Scatter(string f)
	{
		if (1 == 0)
		{
		}
		ScatterProfileGeometry.Profile result = f switch
		{
			"scatter_ice" => ScatterProfileGeometry.Profile.Ice, 
			"scatter_spore" => ScatterProfileGeometry.Profile.Spore, 
			"scatter_warm_spark" => ScatterProfileGeometry.Profile.WarmSpark, 
			_ => ScatterProfileGeometry.Profile.Fire, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	internal static LocalMaterialProfileGeometry.Profile Local(string f)
	{
		if (1 == 0)
		{
		}
		LocalMaterialProfileGeometry.Profile result = f switch
		{
			"local_dark" => LocalMaterialProfileGeometry.Profile.Dark, 
			"local_light" => LocalMaterialProfileGeometry.Profile.Light, 
			"local_botanical" => LocalMaterialProfileGeometry.Profile.Botanical, 
			"local_water_flow" => LocalMaterialProfileGeometry.Profile.Water, 
			_ => LocalMaterialProfileGeometry.Profile.Physical, 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
