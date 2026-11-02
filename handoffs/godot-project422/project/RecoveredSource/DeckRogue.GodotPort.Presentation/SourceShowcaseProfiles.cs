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

// Actual direct ColdSweep card, separate from its two later defensive Ice receipts.
internal static class ColdSweepContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null
        &&r.Key=="card:blue_cold_sweep"&&r.DefinitionId=="blue_cold_sweep"&&r.SourceKind=="card"
        &&r.Family=="cold_sweep_contact"&&r.Renderer==(selected?"ColdSweepContactFx":"BoundedContactFx")
        &&r.Element=="ice"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,DeckRogue.Engine.Generated.GameEvent_DamageDealt hit,bool owned)
    {
        var receipt=DeckRogue.Game.CombatPresentationLedger.Read(hit);var source=hit.PresentationSource;
        if(!Hd2dAttackPrototype.Enabled||!owned||!OriginalRoute(r)||hit.Source!="player"||hit.HpLoss<=0
            ||source is not {Kind:"card",DefinitionId:"blue_cold_sweep"}||string.IsNullOrEmpty(source.Uid)
            ||receipt==null||receipt.Target!="enemy"||receipt.EnemyIndex!=hit.EnemyIndex
            ||!DeckRogue.Game.CombatPresentationLedger.Valid(hit,receipt,out _))return r;
        return r with{Renderer="ColdSweepContactFx",PortStatus="exact real ColdSweep direct card contact; separate Ice4/passive1 retained"};
    }
}


internal static class LightBoltContactVfxProfiles
{
    //Exact authored light-projectile family; retained renderer/API name for compatibility.
    internal static bool Card(string? id)=>id is "white_light_bolt" or "white_light_dart" or "white_light_volley";
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null
        &&Card(r.DefinitionId)&&r.Key=="card:"+r.DefinitionId&&r.SourceKind=="card"
        &&r.Family=="light"&&r.Renderer==(selected?"LightBoltContactFx":"LightRayFx")
        &&r.Element=="light"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,DeckRogue.Engine.Generated.GameEvent_DamageDealt hit,bool owned)
    {
        var receipt=DeckRogue.Game.CombatPresentationLedger.Read(hit);var source=hit.PresentationSource;
        if(!Hd2dAttackPrototype.Enabled||!owned||!OriginalRoute(r)||hit.Source!="player"||hit.HpLoss<=0
            ||hit.SourceUid!=null||source is not {Kind:"card"}||!Card(source.DefinitionId)
            ||source.DefinitionId!=r.DefinitionId||string.IsNullOrEmpty(source.Uid)
            ||receipt==null||receipt.Target!="enemy"||receipt.EnemyIndex!=hit.EnemyIndex
            ||!DeckRogue.Game.CombatPresentationLedger.Valid(hit,receipt,out _))return r;
        return r with{Renderer="LightBoltContactFx",PortStatus="actual owned light-projectile family receipt contact; original negative cast/travel retained"};
    }
}

internal static class VineGraspContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null
        &&r.Key=="enemy:enemy_vine_walker/grasping_vines"&&r.DefinitionId=="grasping_vines"
        &&r.SourceKind=="enemy_actual_execution"&&r.Family=="plant_bind_contact"
        &&r.Renderer==(selected?"VineGraspContactFx":"BoundedContactFx")&&r.Element=="vine"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,bool owned)=>owned&&OriginalRoute(r)
        ?r with{Renderer="VineGraspContactFx",PortStatus="exact owned VineWalker local organic pressure; actual contact clock"}:r;
}

internal static class WraithCurseContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null
        &&r.Key=="enemy:enemy_nemesis_wraith/curse_touch"&&r.DefinitionId=="curse_touch"
        &&r.SourceKind=="enemy_actual_execution"&&r.Family=="local_curse_contact"
        &&r.Renderer==(selected?"WraithCurseContactFx":"BoundedContactFx")&&r.Element=="dark"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,bool owned)=>owned&&OriginalRoute(r)
        ?r with{Renderer="WraithCurseContactFx",PortStatus="exact owned Wraith local dark pressure; actual contact clock"}:r;
}

internal static class CrossbowAimedContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null
        &&r.Key=="enemy:enemy_crossbow_archer/aimed_shot"&&r.DefinitionId=="aimed_shot"
        &&r.SourceKind=="enemy_actual_execution"&&r.Family=="neutral_projectile_contact"
        &&r.Renderer==(selected?"CrossbowAimedContactFx":"BoundedContactFx")&&r.Element=="neutral"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,bool owned)=>owned&&OriginalRoute(r)
        ?r with{Renderer="CrossbowAimedContactFx",PortStatus="exact owned Crossbow local neutral pressure; actual contact clock"}:r;
}

//320 only an actual self-entry owner may tag the ordinary receiver-local heal.
internal static class MonkSelfEntryHealVfxProfiles
{
    internal static bool OriginalRoute(AttackRoute? r)=>r!=null && r.Key=="system:heal" && r.DefinitionId==""
        && r.SourceKind=="system" && r.Family=="heal" && r.Renderer=="HealContinuousFx" && r.Element=="light" && r.FixedSmall;
    internal static bool SelectedRoute(AttackRoute? r)=>r!=null && r.Key=="permanent:white_perm_monk/self-entry-heal"
        && r.DefinitionId=="white_perm_monk" && r.SourceKind=="permanent" && r.Family=="heal"
        && r.Renderer=="HealContinuousFx" && r.Element=="light" && r.FixedSmall;
    internal static AttackRoute Select(AttackRoute r,bool owned)=>owned&&OriginalRoute(r)
        ?r with{Key="permanent:white_perm_monk/self-entry-heal",DefinitionId="white_perm_monk",SourceKind="permanent"}:r;
    internal const float HeroReceive=0.08f;
}

//321 distinct prior-Monk / newly entered Squire context; never all Monk triggers.
internal static class MonkSquireEntryHealVfxProfiles
{
    internal static bool SelectedRoute(AttackRoute? r)=>r!=null && r.Key=="permanent:white_perm_monk/squire-entry-heal"
        && r.DefinitionId=="white_perm_monk" && r.SourceKind=="permanent" && r.Family=="heal"
        && r.Renderer=="HealContinuousFx" && r.Element=="light" && r.FixedSmall;
    internal static AttackRoute Select(AttackRoute r,bool owned)=>owned&&MonkSelfEntryHealVfxProfiles.OriginalRoute(r)
        ?r with{Key="permanent:white_perm_monk/squire-entry-heal",DefinitionId="white_perm_monk",SourceKind="permanent"}:r;
    internal const float HeroReceive=0.08f;
}

//324 direct LightDischarged1 / three live contacts / three weakening events only.
internal static class LightBurstContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null&&r.Key=="card:white_light_burst"
        &&r.DefinitionId=="white_light_burst"&&r.SourceKind=="card"&&r.Family=="local_light"
        &&r.Renderer==(selected?"LightBurstContactFx":"LocalMaterialProfileFx")&&r.Element=="light"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,bool owned)=>Hd2dAttackPrototype.Enabled&&owned&&OriginalRoute(r)
        ?r with{Renderer="LightBurstContactFx",PortStatus="exact direct LightBurst three-target committed local optical shoulder"}:r;
}

//327 exact direct pristine TripleHorn; analytic receive only, geometry remains old physical.
internal static class TripleHornReceiveVfxProfiles
{
    internal const float Receive=.12f;
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null&&r.Key=="card:green_triple_horn"
        &&r.DefinitionId=="green_triple_horn"&&r.SourceKind=="card"&&r.Family=="pierce_contact"
        &&r.Renderer==(selected?"TripleHornReceiverFx":"PlayerContactFx")&&r.Element=="physical"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,bool owned)=>Hd2dAttackPrototype.Enabled&&owned&&OriginalRoute(r)
        ?r with{Renderer="TripleHornReceiverFx",PortStatus="exact owned TripleHorn analytic hero/actual target/forest receive .12"}:r;
}

//343 exact direct pristine DoubleLash; analytic receive only, geometry remains old physical.
internal static class DoubleLashReceiveVfxProfiles
{
    internal const float Receive=.16f;
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null&&r.Key=="card:green_double_lash"
        &&r.DefinitionId=="green_double_lash"&&r.SourceKind=="card"&&r.Family=="vine_slash"
        &&r.Renderer==(selected?"DoubleLashReceiverFx":"VineSlashFx")&&r.Element=="vine"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,bool owned)=>Hd2dAttackPrototype.Enabled&&owned&&OriginalRoute(r)
        ?r with{Renderer="DoubleLashReceiverFx",PortStatus="exact owned DoubleLash analytic hero/actual target/forest receive .16"}:r;
}

//346 exact registered neutral reaction counter, not a named vine body.
internal static class VineFormationCounterVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null
        &&r.Key=="card:green_reaction_vine_formation"&&r.DefinitionId=="green_reaction_vine_formation"&&r.SourceKind=="reaction"
        &&r.Family=="minimal_neutral_contact"&&r.Renderer==(selected?"VineFormationCounterFx":"BoundedContactFx")
        &&r.Element=="neutral"&&r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,bool owned)=>owned&&OriginalRoute(r)
        ?r with{Renderer="VineFormationCounterFx",PortStatus="exact owned neutral counter pressure core; committed receipt only"}:r;
}

//350 exact registered Aether2 -> Spent2 -> single8 -> passiveIce1 contact.
internal static class AetherLanceContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null
        &&r.Key=="card:blue_aether_lance"&&r.DefinitionId=="blue_aether_lance"&&r.SourceKind=="card"
        &&r.Family=="neutral_point_contact"&&r.Renderer==(selected?"AetherLanceContactFx":"BoundedContactFx")
        &&r.Element=="neutral"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,bool owned)=>owned&&OriginalRoute(r)
        ?r with{Renderer="AetherLanceContactFx",PortStatus="exact spent2 single8 translucent aether contact; committed receipt only"}:r;
}

//353 exact registered SparkBurst3 + token3, not shared Scatter promotion.
internal static class SparkBurstContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null&&r.Key=="card:white_spark_burst"
        &&r.DefinitionId=="white_spark_burst"&&r.SourceKind=="card"&&r.Family=="scatter_warm_spark"
        &&r.Renderer==(selected?"SparkBurstContactFx":"ScatterProfileFx")&&r.Element=="light"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,bool owned)=>owned&&OriginalRoute(r)?r with{Renderer="SparkBurstContactFx",PortStatus="exact registered spark3 token3 contact hierarchy; committed receipt only"}:r;
}

//356 selected material for actual installed MomentumWhip damage; catalog provenance remains untouched.
internal static class MomentumWhipContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null
        &&r.Key=="relic:relic_momentum_whip"&&r.DefinitionId=="relic_momentum_whip"&&r.SourceKind=="permanent"
        &&r.Family=="neutral_whip_contact"&&r.Renderer==(selected?"MomentumWhipContactFx":"BoundedContactFx")
        &&r.Element=="neutral"&&r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,bool owned)=>owned&&OriginalRoute(r)
        ?r with{Renderer="MomentumWhipContactFx",PortStatus="exact installed relic authored pressure; actual committed receipt only"}:r;
}

//359 exact selected Cultist receiving pressure; ownership remains in BattleStage.
internal static class CultistDarkBladeContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null
        &&r.Key=="enemy:enemy_cultist/dark_blade"&&r.DefinitionId=="dark_blade"&&r.SourceKind=="enemy_actual_execution"
        &&r.Family=="dark_slash_contact"&&r.Renderer==(selected?"CultistDarkBladeContactFx":"BoundedContactFx")
        &&r.Element=="dark"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,bool owned)=>owned&&OriginalRoute(r)
        ?r with{Renderer="CultistDarkBladeContactFx",PortStatus="exact owned Cultist authored dark pressure; actual committed receipt only"}:r;
}

//334 exact direct Maul attack receive comparison. Its separate heal route is never selected here.
internal static class HolyMaulReceiveVfxProfiles
{
    internal const float Receive=.10f;
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null&&r.Key=="card:white_holy_maul"
        &&r.DefinitionId=="white_holy_maul"&&r.SourceKind=="card"&&r.Family=="enemy_blunt"
        &&r.Renderer==(selected?"HolyMaulReceiverFx":"EnemyContactFx")&&r.Element=="physical"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,bool owned)=>Hd2dAttackPrototype.Enabled&&owned&&OriginalRoute(r)
        ?r with{Renderer="HolyMaulReceiverFx",PortStatus="exact owned HolyMaul attack analytic hero/current target/forest receive .10"}:r;
}

//336 system Heal has no source UID; this local marker means admitted registered Maul ownership only.
internal static class HolyMaulHealReceiveVfxProfiles
{
    internal const float HeroReceive=.08f;
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null
        &&r.Key==(selected?"system:holy-maul-owned-heal":"system:heal")
        &&r.DefinitionId==""&&r.SourceKind=="system"&&r.Family=="heal"
        &&r.Renderer=="HealContinuousFx"&&r.Element=="light";
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,bool owned)=>owned&&OriginalRoute(r)
        ?r with{Key="system:holy-maul-owned-heal",PortStatus="registered primary HolyMaul actual heal exacthero analytical receive .08"}:r;
}

// Owned mineral axial contact family. Material/profile semantics, not card fixtures,
// select this path. Ember/wood/vegetal and elemental families retain their authored path.
internal static class AxialFamilyContactVfxProfiles
{
    internal static bool Eligible(AttackRoute? r) => r!=null && r.SourceKind=="card"
        && r.Key=="card:"+r.DefinitionId && r.Family=="pierce_contact" && r.Element=="physical" && !r.FixedSmall
        && (r.Renderer=="PlayerContactFx" || r.Renderer=="TripleHornReceiverFx")
        && PhysicalVfxProfiles.ForRoute(r.Key) is {Family:PhysicalVfxFamily.AxialThrust,
            Material:PhysicalVfxMaterial.Mineral,Anchor:PhysicalVfxAnchor.ActualWeaponToReachableSurface};
    internal static bool Selected(AttackRoute? r) => r!=null && r.Renderer=="OwnedAxialContactFx"
        && Eligible(r with {Renderer="PlayerContactFx"});
    internal static AttackRoute Select(AttackRoute r,bool owned) => Hd2dAttackPrototype.Enabled && owned && Eligible(r)
        ? r with {Renderer="OwnedAxialContactFx",PortStatus="owned mineral axial family compact directional contact; normalized bounded lighting"}:r;
}

// Scoped to authored WaterWide card routes; actual command/receipt ownership is supplied by Stage.
internal static class WaterWideContactVfxProfiles
{
    internal static bool Eligible(AttackRoute? r)=>r!=null&&r.SourceKind=="card"&&r.Key=="card:"+r.DefinitionId
        &&r.Family=="wide_water"&&r.Renderer=="WideProfileFx"&&r.Element=="water"&&!r.FixedSmall
        &&ElementalVfxProfiles.ForRoute(r.Key)?.Family==ElementalVfxFamily.WaterWide;
    internal const float TargetRange=1.10f,FloorRange=1.50f,NearRadius=.30f,FloorHeight=.12f;
    // Receiver-normalized local irradiance, not wave emission/exposure. Preserve a soft wet footprint.
    internal const float TargetGain=.45f,FloorGain=.12f;
}

//381 exact owned paid-discard Gulp only; original tuple remains the authoritative beat.
internal static class GreenGulpContactVfxProfiles {
 internal static bool OriginalRoute(AttackRoute? r)=>r is {Key:"card:green_serpent_gulp",DefinitionId:"green_serpent_gulp",SourceKind:"card",Family:"neutral_engulf_contact",Renderer:"BoundedContactFx",Element:"neutral",FixedSmall:false};
 internal static bool SelectedRoute(AttackRoute? r)=>r is {Key:"card:green_serpent_gulp",DefinitionId:"green_serpent_gulp",SourceKind:"card",Family:"neutral_engulf_contact",Renderer:"GreenGulpContactFx",Element:"neutral",FixedSmall:false};
 internal static AttackRoute Select(AttackRoute r,bool owned)=>owned&&OriginalRoute(r)?r with{Renderer="GreenGulpContactFx",PortStatus="381 actual-owned paid-discard organic inward closure"}:r;
}

//384 bounded direct NeutralPressure source family. Original packet, palette and timing stay unchanged.
//Blue/green7 actual commands tested; four black family members remain canonical-actor unverified.
internal static class NeutralPressureContactVfxProfiles
{
    internal static bool Eligible(AttackRoute? route)
    {
        return route != null && route.SourceKind == "card" && route.Key == "card:" + route.DefinitionId
            && route.Element == "neutral" && !route.FixedSmall
            && ElementalVfxProfiles.ForRoute(route.Key)?.Family == ElementalVfxFamily.NeutralPressure
            && route.DefinitionId is "blue_page_wind" or "blue_weight_of_wisdom" or "blue_knowledge_torrent"
                or "blue_omniscient_blow" or "blue_aftermath" or "green_rooting_rush" or "green_catalyst_echo"
                or "black_grave_burst" or "black_withering_curse" or "black_whispering_ghost" or "black_groaning_skull";
    }
    internal const float TargetRange = .80f;
    internal const float FloorRange = .90f;
    internal const float NearRadius = .24f;
    internal const float FloorHeight = .12f;
    internal const float TargetGain = .65f;
    internal const float FloorGain = .10f;
}

//385 only an actual registered Catalyst Spore hit may select this contact material.
internal static class SporeContactVfx385 {
 internal static bool OriginalRoute(AttackRoute? r)=>r is {Key:"card:green_catalyst_spore",DefinitionId:"green_catalyst_spore",SourceKind:"card",Family:"scatter_spore",Renderer:"ScatterProfileFx",Element:"arcane",FixedSmall:false};
 internal static bool SelectedRoute(AttackRoute? r)=>r is {Key:"card:green_catalyst_spore",DefinitionId:"green_catalyst_spore",SourceKind:"card",Family:"scatter_spore",Renderer:"OwnedSpore385Fx",Element:"arcane",FixedSmall:false};
 internal static AttackRoute Select(AttackRoute r,bool owned)=>owned&&OriginalRoute(r)?r with{Renderer="OwnedSpore385Fx",PortStatus="385 actual-owned localized botanical spore contact"}:r;
}

//389 lighting-only policy for the original EarthRoar factory. No body or timing selection.
internal static class EarthRoarContactVfxProfiles
{
    internal static bool Eligible(AttackRoute? route)
    {
        return route is { Key: "card:green_earth_roar", DefinitionId: "green_earth_roar", SourceKind: "card", Family: "earth_fissure", Renderer: "EarthStompFx", Element: "earth", FixedSmall: false }
            && ElementalVfxProfiles.ForRoute(route.Key)?.Family == ElementalVfxFamily.EarthFracture;
    }
    internal const float TargetRange = .80f;
    internal const float FloorRange = .90f;
    internal const float NearRadius = .24f;
    internal const float FloorHeight = .12f;
    internal const float TargetGain = .65f;
    internal const float FloorGain = .10f;
}

//391 light-only localization for three retained support bodies, never a new factory.
internal static class SupportLightVfx391
{
    internal static bool Eligible(AttackRoute? route)
    {
        if (!Hd2dAttackPrototype.Enabled || route is not { SourceKind: "support", Renderer: "Hd2dSupportFx", FixedSmall: true })
        {
            return false;
        }
        return route is { Key: "support:white_prayer_verse/heal", DefinitionId: "white_prayer_verse", Family: "prayer_heal", Element: "light" }
            or { Key: "support:white_mass_heal/heal", DefinitionId: "white_mass_heal", Family: "prayer_heal", Element: "light" }
            or { Key: "support:blue_ice_wall/ice-gain", DefinitionId: "blue_ice_wall", Family: "ice_guard_gain", Element: "ice" };
    }
    internal const float TargetRange = .80f;
    internal const float FloorRange = .90f;
    internal const float NearRadius = .30f;
    internal const float FloorHeight = .12f;
    internal const float TargetGain = .55f;
    internal const float FloorGain = .12f;
}

//392 light-only bounds for five explicit green IDs across three original physical materials. No route/body/timing replacement.
internal static class GreenPhysicalContactVfxProfiles
{
    internal static bool Eligible(AttackRoute? route)
    {
        //417 exact Sprint prototype is nominally neutral; do not widen the existing physical routes.
        if (route is { Key: "card:green_sprint", DefinitionId: "green_sprint", SourceKind: "card", Family: "minimal_neutral_contact", Renderer: "BoundedContactFx", Element: "neutral", FixedSmall: false })
        {
            return PhysicalVfxProfiles.ForRoute(route.Key) is { CardId: "green_sprint", Family: PhysicalVfxFamily.BluntImpact };
        }
        if (route is not { SourceKind: "card", Element: "physical", FixedSmall: false }
            || route.Key != "card:" + route.DefinitionId)
        {
            return false;
        }
        return route.DefinitionId switch
        {
            "green_basic_bash" => route.Family == "enemy_blunt" && route.Renderer == "EnemyContactFx"
                && PhysicalVfxProfiles.ForRoute(route.Key) is { Family: PhysicalVfxFamily.BluntImpact, Material: PhysicalVfxMaterial.Mineral },
            "green_sapling_strike" => route.Family == "wood_blunt_contact" && route.Renderer == "BoundedContactFx"
                && PhysicalVfxProfiles.ForRoute(route.Key) is { Family: PhysicalVfxFamily.BluntImpact, Material: PhysicalVfxMaterial.Wood },
            "green_fang" or "green_growing_fang" or "green_harvest_strike" => route.Family == "enemy_fang" && route.Renderer == "EnemyContactFx"
                && PhysicalVfxProfiles.ForRoute(route.Key)?.Family == PhysicalVfxFamily.ConvergingPressure,
            _ => false
        };
    }
    internal const float TargetRange = .80f;
    internal const float FloorRange = .90f;
    internal const float NearRadius = .24f;
    internal const float FloorHeight = .12f;
    internal const float TargetGain = .65f;
    internal const float FloorGain = .10f;
}

//396 light-only policy for six remaining White Light attacks; original bodies/clocks preserved.
internal static class WhiteLight396ContactVfxProfiles
{
    internal static bool Eligible(AttackRoute? route)
    {
        return route is { SourceKind: "card", Family: "light", Renderer: "LightRayFx", Element: "light", FixedSmall: false }
            && route.Key == "card:" + route.DefinitionId
            && route.DefinitionId is "white_strike" or "white_shield_strike" or "white_mode_crossroad"
                or "white_light_glint" or "white_light_lance" or "white_light_torrent";
    }
    internal const float TargetRange = .80f;
    internal const float FloorRange = .90f;
    internal const float NearRadius = .24f;
    internal const float FloorHeight = .12f;
    internal const float TargetGain = .65f;
    internal const float FloorGain = .10f;
}

//404 exact RootingVine cast-owned positive contact lighting; original body/lead/clocks retained.
internal static class RootingVine404ContactVfxProfiles
{
    internal static bool Eligible(AttackRoute? route)
    {
        return route is { SourceKind: "card", Family: "vine_slash", Renderer: "VineSlashFx", Element: "vine", FixedSmall: false }
            && route.Key == "card:fusion_rooting_vine" && route.DefinitionId == "fusion_rooting_vine";
    }
    internal const float TargetRange = .80f;
    internal const float FloorRange = .90f;
    internal const float NearRadius = .24f;
    internal const float FloorHeight = .12f;
    internal const float TargetGain = .65f;
    internal const float FloorGain = .10f;
}

//419 exact legacy enemy contact light scopes; original material factories remain unchanged.
internal static class LegacyContactVfx419
{
    internal static bool Eligible(AttackRoute? route)
    {
        return route is { SourceKind: "enemy_actual_execution", Element: "physical", FixedSmall: false }
            && ((route.Key == "enemy:enemy_bowl_bug/tackle" && route.DefinitionId == "tackle" && route.Family == "enemy_blunt" && route.Renderer == "EnemyContactFx")
                || (route.Key == "enemy:enemy_crab_crusher/bug_sting" && route.DefinitionId == "bug_sting" && route.Family == "pierce_contact" && route.Renderer == "PlayerContactFx"));
    }
    internal const float TargetRange = .80f;
    internal const float FloorRange = .90f;
    internal const float NearRadius = .24f;
    internal const float FloorHeight = .12f;
    internal const float TargetGain = .65f;
    internal const float FloorGain = .10f;
}
