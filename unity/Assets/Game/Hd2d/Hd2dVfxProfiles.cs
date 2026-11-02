#nullable enable
// Hd2dVfxProfiles.cs — dot の Godot 版 Project422 (2026-11-02) の「経路を選ぶ門」の写し。
// 出典: RecoveredSource/DeckRogue.GodotPort.Presentation/{SourceShowcaseProfiles (後半の *ContactVfxProfiles), PermanentContactVfxProfiles, MirrorContactVfxProfiles,
//   NecroContactVfxProfiles, ReactionContactVfxProfiles, LightningContactVfxProfiles, EnemyContactVfxProfiles, ImpSparkContactVfxProfiles, StatusContactVfxProfiles,
//   GearContactVfxProfiles, GearKegContactVfxProfiles, NamelessPowderContactVfxProfiles, NamelessKegContactVfxProfiles, SupportVfxProfiles (経路の部分), NativeGeometryBridge (ObjectContact415Route・WarmSpark397Route)}。
// 変えた所: 名前空間・using だけ。経路の組 (Key・DefinitionId・SourceKind・Family・Renderer・Element・FixedSmall) と「元の経路 → 選ばれた経路」の書き換えは一字も変えない。
// 所有権 (どの出来事が本物の当たりか) の判定は Godot では BattleStage が握っていた。Unity では Hd2dOwnership が Ticket と出来事の記録から同じ意味で判定する。
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game.Hd2d
{
// Actual direct ColdSweep card, separate from its two later defensive Ice receipts.
internal static class ColdSweepContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null
        &&r.Key=="card:blue_cold_sweep"&&r.DefinitionId=="blue_cold_sweep"&&r.SourceKind=="card"
        &&r.Family=="cold_sweep_contact"&&r.Renderer==(selected?"ColdSweepContactFx":"BoundedContactFx")
        &&r.Element=="ice"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,GameEvent_DamageDealt hit,bool owned)
    {
        var receipt=CombatPresentationLedger.Read(hit);var source=hit.PresentationSource;
        if(!Hd2dAttackPrototype.Enabled||!owned||!OriginalRoute(r)||hit.Source!="player"||hit.HpLoss<=0
            ||source is not {Kind:"card",DefinitionId:"blue_cold_sweep"}||string.IsNullOrEmpty(source.Uid)
            ||receipt==null||receipt.Target!="enemy"||receipt.EnemyIndex!=hit.EnemyIndex
            ||!CombatPresentationLedger.Valid(hit,receipt,out _))return r;
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
    internal static AttackRoute Select(AttackRoute r,GameEvent_DamageDealt hit,bool owned)
    {
        var receipt=CombatPresentationLedger.Read(hit);var source=hit.PresentationSource;
        if(!Hd2dAttackPrototype.Enabled||!owned||!OriginalRoute(r)||hit.Source!="player"||hit.HpLoss<=0
            ||hit.SourceUid!=null||source is not {Kind:"card"}||!Card(source.DefinitionId)
            ||source.DefinitionId!=r.DefinitionId||string.IsNullOrEmpty(source.Uid)
            ||receipt==null||receipt.Target!="enemy"||receipt.EnemyIndex!=hit.EnemyIndex
            ||!CombatPresentationLedger.Valid(hit,receipt,out _))return r;
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

//379: two owned receiving materials only. Original catalog, beat clock and all other contact families are unchanged.
// (経路の部分。幾何 Create は Hd2dGeometryBridge.BlueContact379)
internal static class BlueContactVfx379Routes
{
    private static bool Tuple(AttackRoute? route, bool selected)
    {
        if (route == null || route.SourceKind != "card" || route.FixedSmall
            || route.Renderer != (selected ? "BlueOwnedContact379Fx" : "BoundedContactFx"))
        {
            return false;
        }
        return (route.Key == "card:blue_ripple_blade" && route.DefinitionId == "blue_ripple_blade"
                && route.Family == "water_slash_contact" && route.Element == "water")
            || (route.Key == "card:blue_glacial_maul" && route.DefinitionId == "blue_glacial_maul"
                && route.Family == "ice_blunt_contact" && route.Element == "ice");
    }
    internal static bool OriginalRoute(AttackRoute? route) => Tuple(route, selected: false);
    internal static bool SelectedRoute(AttackRoute? route) => Tuple(route, selected: true);
    internal static AttackRoute Select(AttackRoute route, bool owned)
    {
        return owned && OriginalRoute(route)
            ? route with { Renderer = "BlueOwnedContact379Fx", PortStatus = "379 exact owned blue receipt-bound water cut / crystalline blunt contact" }
            : route;
    }
}

// Explicit non-retainer installed sources. Each definition owns one exact
// source-kind/key/material tuple; sharing an owner gate is not a generic route.
internal static class PermanentContactVfxProfiles
{
    internal static bool NeutralDefinition(string? id)=>id is "black_blood_candle"
        or "black_perm_pain_core" or "black_perm_bone_mill" or "black_perm_chorus" or "black_crimson_feast"
        or "white_perm_banner" or "fusion_raging_chief" or "fusion_red_wrath_mail" or "fusion_ringed_bark";
    internal static bool FireDefinition(string? id)=>id is "red_perm_runfire" or "red_perm_thorn_flame" or "red_perm_cascade_sparks";
    internal static bool DarkDefinition(string? id)=>id is "black_perm_tithe" or "black_perm_seedbed";
    internal static bool PlantDefinition(string? id)=>id is "green_perm_thorn_leaves" or "green_perm_thorn_canopy" or "fusion_bramble_fence";
    internal static bool LightningDefinition(string? id)=>id=="blue_perm_static";
    internal static bool SelectedDefinition(string? id)=>NeutralDefinition(id)||FireDefinition(id)||DarkDefinition(id)||PlantDefinition(id)||LightningDefinition(id);
    private static bool RouteTuple(AttackRoute? route,bool selected)
    {
        if(route==null || route.SourceKind!="permanent" || !route.FixedSmall || !SelectedDefinition(route.DefinitionId))return false;
        string prefix=route.DefinitionId=="red_perm_cascade_sparks"?"permanent:":"card:";
        if(route.Key!=prefix+route.DefinitionId)return false;
        string family,renderer,element;
        if(NeutralDefinition(route.DefinitionId)){family="minimal_neutral_contact";renderer="BoundedContactFx";element="neutral";}
        else if(FireDefinition(route.DefinitionId)){family="fire_localburst";renderer="FireContinuousFx";element="fire";}
        else if(DarkDefinition(route.DefinitionId)){family="dark_drain";renderer="DarkDrainFx";element="dark";}
        else if(PlantDefinition(route.DefinitionId)){family="plant_point_contact";renderer="BoundedContactFx";element="vine";}
        else {family="lightning_discharge";renderer="LightningDischargeFx";element="lightning";}
        return route.Family==family && route.Element==element
            && route.Renderer==(selected?"Hd2dPermanentContactFx":renderer);
    }
    internal static bool CanopyRoute(AttackRoute? route)=>route!=null&&SelectedRoute(route)&&route.DefinitionId=="green_perm_thorn_canopy";
    internal static bool ThornFlameRoute(AttackRoute route)=>SelectedRoute(route) && route.DefinitionId=="red_perm_thorn_flame";
    internal static bool OriginalRoute(AttackRoute? route)=>RouteTuple(route,false);
    internal static bool SelectedRoute(AttackRoute route)=>RouteTuple(route,true);
    internal static bool OwnsInstalled(GameEvent_DamageDealt hit,bool installedOwner)
    {
        var source=hit.PresentationSource;var receipt=CombatPresentationLedger.Read(hit);
        return Hd2dAttackPrototype.Enabled && installedOwner && hit.Source=="player"
            && source is {Kind:"permanent"} && SelectedDefinition(source.DefinitionId)
            && !string.IsNullOrEmpty(source.Uid) && hit.SourceUid==source.Uid
            && receipt!=null && receipt.Target=="enemy" && receipt.EnemyIndex==hit.EnemyIndex
            && CombatPresentationLedger.Valid(hit,receipt,out _);
    }
    // Keep historical neutral/Candle diagnostics narrow after material expansion.
    internal static bool OwnsInstalledNeutral(GameEvent_DamageDealt hit,bool installedOwner)
        =>NeutralDefinition(hit.PresentationSource?.DefinitionId) && OwnsInstalled(hit,installedOwner);
    // Preserve this historical narrow helper's semantics for versioned QA.
    internal static bool OwnsInstalledCandle(GameEvent_DamageDealt hit,bool installedOwner)
        =>hit.PresentationSource?.DefinitionId=="black_blood_candle" && OwnsInstalledNeutral(hit,installedOwner);
    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,bool installedOwner)
    {
        if(!OriginalRoute(route) || !OwnsInstalled(hit,installedOwner) || hit.HpLoss<=0
            || hit.PresentationSource?.DefinitionId!=route.DefinitionId)return route;
        return route with {Renderer="Hd2dPermanentContactFx",PortStatus=NeutralDefinition(route.DefinitionId)?"validated exact installed neutral permanent UID; target-local trigger contact":"validated exact installed permanent UID and material tuple; target-local trigger contact"};
    }
}

// Exact installed Mirror after a proven real Mana Leak negation. No generic water admission.
internal static class MirrorContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null&&r.Key=="card:blue_perm_mirror"
        &&r.DefinitionId=="blue_perm_mirror"&&r.SourceKind=="permanent"&&r.Family=="water"
        &&r.Renderer==(selected?"MirrorWaterContactFx":"WaterContinuousFx")&&r.Element=="water"&&r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,GameEvent_DamageDealt h,bool owned)
    {
        var p=h.PresentationSource;var receipt=CombatPresentationLedger.Read(h);
        if(!Hd2dAttackPrototype.Enabled||!owned||!OriginalRoute(r)||h.Source!="player"||h.HpLoss<=0
            ||p is not {Kind:"permanent",DefinitionId:"blue_perm_mirror"}||string.IsNullOrEmpty(p.Uid)
            ||h.SourceUid!=p.Uid||receipt==null||receipt.Target!="enemy"||receipt.EnemyIndex!=h.EnemyIndex
            ||!CombatPresentationLedger.Valid(h,receipt,out _))return r;
        return r with{Renderer="MirrorWaterContactFx",PortStatus="exact installed Mirror UID after real Mana Leak negation; local return sheet"};
    }
}

// Explicit censer deck-exhaustion definitions. SourceUid is the ambient permanent;
// PresentationSource.Uid is the exhausted effect card. They MUST NOT be conflated.
internal static class NecroContactVfxProfiles
{
    internal static int ExpectedDrainAmount(string? definition)=>definition switch {
        "black_corpse_kiss"=>3, "black_groaning_skull"=>4, _=>-1 };
    internal static bool OriginalRoute(AttackRoute? route)=>route!=null
        && route.SourceKind=="necro" && ExpectedDrainAmount(route.DefinitionId)>0
        && route.Key=="card:"+route.DefinitionId && route.Family=="dark_drain"
        && route.Renderer=="DarkDrainFx" && route.Element=="dark" && route.FixedSmall;
    internal static bool SelectedRoute(AttackRoute route)=>route.SourceKind=="necro"
        && ExpectedDrainAmount(route.DefinitionId)>0 && route.Key=="card:"+route.DefinitionId
        && route.Family=="dark_drain" && route.Renderer=="Hd2dNecroContactFx"
        && route.Element=="dark" && route.FixedSmall;
    internal static bool GroaningSkullRoute(AttackRoute route)=>SelectedRoute(route) && route.DefinitionId=="black_groaning_skull";
    internal static bool CorpseKissRoute(AttackRoute route)=>SelectedRoute(route) && route.DefinitionId=="black_corpse_kiss";
    internal static bool OwnsCenserDeckExhaustion(GameEvent_DamageDealt hit,bool instanceMoved,bool ambientCenser,bool orderedEvents)
    {
        var source=hit.PresentationSource;var receipt=CombatPresentationLedger.Read(hit);
        return Hd2dAttackPrototype.Enabled && instanceMoved && ambientCenser && orderedEvents
            && hit.Source=="player" && source is { Kind:"necro" } && ExpectedDrainAmount(source.DefinitionId)>0
            && !string.IsNullOrEmpty(source.Uid) && !string.IsNullOrEmpty(hit.SourceUid)
            && source.Uid!=hit.SourceUid && receipt!=null && receipt.Target=="enemy"
            && receipt.EnemyIndex==hit.EnemyIndex && CombatPresentationLedger.Valid(hit,receipt,out _);
    }
    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,bool ownedInstance)
    {
        if(!ownedInstance || hit.HpLoss<=0 || !OriginalRoute(route)
            || hit.PresentationSource is not {Kind:"necro"} source
            || source.DefinitionId!=route.DefinitionId)return route;
        return route with { Renderer="Hd2dNecroContactFx",PortStatus="validated censer deck-exhausted card UID; target-local dark necro contact" };
    }
}

// Explicitly selected, confirmed armed post-attack reaction instances. This is not
// a passive source, ordinary PlayCard, auto reaction or generic reaction opt-in.
internal static class ReactionContactVfxProfiles
{
    internal static bool SelectedDefinition(string? definition)=>definition is
        "black_reaction_curse" or "black_underworld_hand" or "black_reaction_grudge" or "black_reaction_last_stand";
    internal static bool UnderworldRoute(AttackRoute route)=>SelectedRoute(route) && route.DefinitionId=="black_underworld_hand";
    internal static bool GrudgeRoute(AttackRoute route)=>SelectedRoute(route) && route.DefinitionId=="black_reaction_grudge";
    internal static bool LastStandRoute(AttackRoute route)=>SelectedRoute(route) && route.DefinitionId=="black_reaction_last_stand";
    internal static bool CurseRoute(AttackRoute route)=>SelectedRoute(route) && route.DefinitionId=="black_reaction_curse";
    internal static bool OriginalRoute(AttackRoute? route)=>route!=null
        && route.SourceKind=="reaction" && SelectedDefinition(route.DefinitionId)
        && route.Key=="card:"+route.DefinitionId && route.Family=="dark_drain"
        && route.Renderer=="DarkDrainFx" && route.Element=="dark" && route.FixedSmall;
    internal static bool SelectedRoute(AttackRoute route)=>route.SourceKind=="reaction"
        && SelectedDefinition(route.DefinitionId) && route.Key=="card:"+route.DefinitionId
        && route.Family=="dark_drain" && route.Renderer=="Hd2dReactionContactFx"
        && route.Element=="dark" && route.FixedSmall;
    internal static bool OwnsConfirmedInstance(GameEvent_DamageDealt hit,bool confirmedInstance)
    {
        var source=hit.PresentationSource;var receipt=CombatPresentationLedger.Read(hit);
        return Hd2dAttackPrototype.Enabled && confirmedInstance && hit.Source=="player"
            && source is {Kind:"reaction"} && SelectedDefinition(source.DefinitionId)
            && !string.IsNullOrEmpty(source.Uid) && receipt!=null && receipt.Target=="enemy"
            && receipt.EnemyIndex==hit.EnemyIndex && CombatPresentationLedger.Valid(hit,receipt,out _);
    }
    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,bool owned)
    {
        if(!OriginalRoute(route) || hit.PresentationSource?.DefinitionId!=route.DefinitionId
            || hit.HpLoss<=0 || !OwnsConfirmedInstance(hit,owned))return route;
        return route with {Renderer="Hd2dReactionContactFx",PortStatus="validated explicitly confirmed armed reaction UID; local Dark contact, existing counter pose preserved"};
    }
}

// Context-specific adapter, deliberately NOT an Elemental.ForCard admission.
// The same card key is neutral for other actual leaders.
internal static class LightningContactVfxProfiles
{
    internal static bool SelectedRoute(AttackRoute route)=>route.SourceKind=="card"
        && route.Key=="card:blue_aether_burst" && route.DefinitionId=="blue_aether_burst"
        && route.Family=="lightning_discharge" && route.Element=="lightning" && !route.FixedSmall
        && route.Renderer=="Hd2dLightningContactFx";
    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,bool actualIzzetCardOwner)
    {
        if(!Hd2dAttackPrototype.Enabled || !actualIzzetCardOwner || hit.Source!="player" || hit.HpLoss<=0
            || hit.PresentationSource is not {Kind:"card",DefinitionId:"blue_aether_burst"} source
            || string.IsNullOrEmpty(source.Uid) || route.SourceKind!="card"
            || route.Key!="card:blue_aether_burst" || route.DefinitionId!="blue_aether_burst"
            || route.Family!="lightning_discharge" || route.Renderer!="LightningDischargeFx"
            || route.Element!="lightning" || route.FixedSmall)return route;
        var receipt=CombatPresentationLedger.Read(hit);
        if(receipt==null || receipt.Target!="enemy" || receipt.EnemyIndex!=hit.EnemyIndex
            || !CombatPresentationLedger.Valid(hit,receipt,out _))return route;
        return route with {Renderer="Hd2dLightningContactFx",PortStatus="actual Izzet owning card UID and resolved Lightning tuple; original motion/timing"};
    }
}

// Exact actor + move pairs. This refines local receiving contact only; each
// enemy retains its actual authored action, timing and gameplay contribution.
internal static class EnemyContactVfxProfiles
{
    private static bool SelectedPair(string? actor,string? move,string family)
    {
        if(family=="minimal_neutral_contact")return (actor,move) switch {
            ("enemy_wide_power","surge") or ("enemy_thief","mug")
            or ("enemy_crab_crusher","thrash") or ("enemy_devoted_sculptor","savage")
            or ("enemy_forgotten_soul","dread")
            or ("enemy_forgotten_soul","miasma")
            or ("enemy_devoted_sculptor","carve_rite")
            or ("enemy_lost_soul","debilitating_smog")
            or ("enemy_hexer","mud")
            or ("enemy_mud_lump","slop")
            or ("enemy_spore_cap","frail_spores")
            or ("enemy_strangler_serpent","constrict")
            or ("enemy_sludge_spider","oil_spray")
            or ("enemy_apprentice_colossus","swing")
            or ("enemy_slug","lick")
            or ("enemy_slug","acid_spit")
            or ("enemy_cog_construct","repeater")
            or ("enemy_iron_clam","jet")
            or ("enemy_frog_knight","tongue_lash")
            or ("enemy_frog_knight","strike_down")
            or ("enemy_warden","bell_hushed")
            or ("enemy_elite_sergeant","whip")
            or ("enemy_elite_slaver","chain")
            or ("enemy_brood_toad","tongue")
            or ("enemy_elite_owl","wing")
            or ("enemy_axe_ogre","great_swing")
            or ("enemy_warden","bell_strike")
            or ("enemy_warden","gate_guard")
            or ("enemy_strangler_serpent","lash")=>true,_=>false };
        if(family=="enemy_blunt")return (actor,move) switch {
            ("enemy_brute","club") or ("enemy_brute","club_wild")
            or ("enemy_set_breaker","smash") or ("enemy_moss","slam")
            or ("enemy_big_slime","slam") or ("enemy_mudling","bump")
            or ("enemy_elite_gold_raven","dive")
            or ("enemy_moss_slime","bump")
            or ("enemy_shield_squire","shield_bash")
            or ("enemy_mourn_beast","leap")
            or ("enemy_bowl_bug","roll")
            or ("enemy_elite_giant_face","crush")
            or ("enemy_drummer","thump")
            or ("enemy_chimera_1","skull_bash")
            or ("enemy_turtle","crush")=>true,_=>false };
        if(family!="pierce_contact")return false;
        return (actor,move) switch {
        ("enemy_probe","poke") or ("enemy_probe","lunge")
        or ("enemy_rock_beetle","horn_jab") or ("enemy_bond_wolf","lunge")
        or ("enemy_elite_gold_raven","peck") or ("enemy_haze_stag","antler_jab")
        or ("enemy_haze_stag","antler_charge")=>true,_=>false };
    }
    private static bool RouteTuple(AttackRoute route,string renderer)
    {
        if(route.SourceKind!="enemy_actual_execution"
            || (route.Family!="pierce_contact" && route.Family!="enemy_blunt" && route.Family!="minimal_neutral_contact")
            || route.Renderer!=renderer || route.Element!=(route.Family=="minimal_neutral_contact"?"neutral":"physical") || route.FixedSmall)return false;
        const string prefix="enemy:";
        if(!route.Key.StartsWith(prefix,System.StringComparison.Ordinal))return false;
        int split=route.Key.IndexOf('/',prefix.Length);
        if(split<=prefix.Length || split==route.Key.Length-1)return false;
        string actor=route.Key.Substring(prefix.Length,split-prefix.Length),move=route.Key.Substring(split+1);
        return route.DefinitionId==move && SelectedPair(actor,move,route.Family);
    }
    internal static bool SelectedRoute(AttackRoute route)=>RouteTuple(route,"Hd2dEnemyContactFx");
    internal static bool ProbePokeRoute(AttackRoute route)=>SelectedRoute(route) && route.Key=="enemy:enemy_probe/poke" && route.DefinitionId=="poke" && route.Family=="pierce_contact";

    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,
        string? actorId,string? provenMove,IReadOnlyList<GameEvent>? actualLog)
    {
        if(!Hd2dAttackPrototype.Enabled || !RouteTuple(route,route.Family=="minimal_neutral_contact"?"BoundedContactFx":route.Family=="enemy_blunt"?"EnemyContactFx":"PlayerContactFx")
            || !SelectedPair(actorId,provenMove,route.Family) || route.Key!="enemy:"+actorId+"/"+provenMove
            || route.DefinitionId!=provenMove || hit.Source!="enemy" || hit.HpLoss<=0)return route;
        var action=hit.PresentationEnemyAction;var multi=hit.PresentationEnemyAttack;
        if(action==null || action.Version!=1 || action.EnemyId!=actorId || action.MoveId!=provenMove
            || action.UnresolvedReason!=null || action.ExecutionEventIndex<0
            || (action.Branch!="primary" && action.Branch!="alternate")
            || multi==null || multi.Version!=1 || multi.EnemyId!=actorId
            || multi.Resolution!="aggregate-final-mutation" || multi.LoopBound!=1 || multi.ExecutedHits!=1
            || multi.Contributions?.Count!=1 || multi.EffectiveActual!=hit.Amount)return route;
        // The owning actual combat log, not a renderer label or move-name
        // collision, proves this execution and source actor.
        bool execution=actualLog!=null && actualLog.Any(e=>e is GameEvent_EnemyActionExecuting x
            && x.EnemyIndex==hit.EnemyIndex && x.Kind=="attack"
            && x.PresentationEnemyAction is {Version:1,UnresolvedReason:null} a
            && a.EnemyId==actorId && a.MoveId==provenMove
            && a.ExecutionEventIndex==action.ExecutionEventIndex && a.Branch==action.Branch);
        var receipt=CombatPresentationLedger.Read(hit);
        if(!execution || receipt==null || receipt.Target!="player"
            || !CombatPresentationLedger.Valid(hit,receipt,out _))return route;
        return route with {Renderer="Hd2dEnemyContactFx",PortStatus="validated exact single-hit enemy actor/move contact; original motion/timing retained"};
    }
}

// One actual enemy execution, not a family-wide Fire replacement.
internal static class ImpSparkContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null
        &&r.Key=="enemy:enemy_cinder_imp/spark_toss"&&r.DefinitionId=="spark_toss"
        &&r.SourceKind=="enemy_actual_execution"&&r.Family=="fire"
        &&r.Renderer==(selected?"ImpSparkContactFx":"FireContinuousFx")&&r.Element=="fire"&&!r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,GameEvent_DamageDealt h,bool owned)
    {
        var receipt=CombatPresentationLedger.Read(h);var a=h.PresentationEnemyAction;var m=h.PresentationEnemyAttack;
        if(!Hd2dAttackPrototype.Enabled||!owned||!OriginalRoute(r)||h.Source!="enemy"||h.HpLoss<=0
            ||a is not {Version:1,EnemyId:"enemy_cinder_imp",MoveId:"spark_toss",UnresolvedReason:null}
            ||a.ExecutionEventIndex<0||(a.Branch!="primary"&&a.Branch!="alternate")
            ||m is not {Version:1,EnemyId:"enemy_cinder_imp",Resolution:"aggregate-final-mutation",LoopBound:1,ExecutedHits:1}
            ||m.Contributions?.Count!=1||m.EffectiveActual!=h.Amount
            ||receipt==null||receipt.Target!="player"||!CombatPresentationLedger.Valid(h,receipt,out _))return r;
        return r with{Renderer="ImpSparkContactFx",PortStatus="exact actual Cinder spark contact; prior projectile and rules preserved"};
    }
}

// Positive actual status receipt only: no damage-event/card-source substitution.
internal static class StatusContactVfxProfiles
{
    internal static bool SelectedRoute(AttackRoute route)=>route.SourceKind=="system"
        && route.Key=="system:burn" && route.Family=="burn" && route.Renderer=="Hd2dBurnStatusFx";
    internal static AttackRoute Select(AttackRoute route,GameEvent actual,bool ownedEvent)
    {
        if(!Hd2dAttackPrototype.Enabled || !ownedEvent || route.SourceKind!="system" || route.Key!="system:burn"
            || route.Family!="burn" || route.Element!="fire" || route.Renderer!="SystemOutcomeFx" || !route.FixedSmall
            || actual is not GameEvent_BurnTick { Amount: >0 } burn
            || CombatOutcome.PositiveSystemFamily(actual,out _)!="burn")return route;
        var receipt=CombatPresentationLedger.Read(actual);
        if(receipt==null || receipt.Target!="enemy" || receipt.EnemyIndex!=burn.EnemyIndex)return route;
        return route with { Renderer="Hd2dBurnStatusFx",PortStatus="validated actual BurnTick; local thermal status pulse" };
    }
}

// Direct use of one real held Powder instance only. Presentation source has no UID.
internal static class GearContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null && r.Key=="gear:gear_powder"
        && r.DefinitionId=="gear_powder" && r.SourceKind=="gear" && r.Family=="fire_localburst"
        && r.Renderer==(selected?"Hd2dGearContactFx":"FireContinuousFx") && r.Element=="fire" && r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,GameEvent_DamageDealt hit,bool owned)
    {
        var receipt=CombatPresentationLedger.Read(hit);
        if(!Hd2dAttackPrototype.Enabled || !OriginalRoute(r) || !owned || hit.Source!="player" || hit.HpLoss<=0
            || hit.PresentationSource is not {Kind:"gear",DefinitionId:"gear_powder"} source
            || !string.IsNullOrEmpty(source.Uid) || receipt==null || receipt.Target!="enemy"
            || receipt.EnemyIndex!=hit.EnemyIndex || !CombatPresentationLedger.Valid(hit,receipt,out _))return r;
        return r with {Renderer="Hd2dGearContactFx",PortStatus="exact direct UseGear previous instance/index and consumed gear; UID-less receiving contact"};
    }
}
// Direct use of one real held Keg instance only. Presentation source has no UID.
internal static class GearKegContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null && r.Key=="gear:gear_powder_keg"
        && r.DefinitionId=="gear_powder_keg" && r.SourceKind=="gear" && r.Family=="fire_localburst"
        && r.Renderer==(selected?"Hd2dGearKegContactFx":"FireContinuousFx") && r.Element=="fire" && r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,GameEvent_DamageDealt hit,bool owned)
    {
        var receipt=CombatPresentationLedger.Read(hit);
        if(!Hd2dAttackPrototype.Enabled || !OriginalRoute(r) || !owned || hit.Source!="player" || hit.HpLoss<=0
            || hit.PresentationSource is not {Kind:"gear",DefinitionId:"gear_powder_keg"} source
            || !string.IsNullOrEmpty(source.Uid) || receipt==null || receipt.Target!="enemy"
            || receipt.EnemyIndex!=hit.EnemyIndex || !CombatPresentationLedger.Valid(hit,receipt,out _))return r;
        return r with {Renderer="Hd2dGearKegContactFx",PortStatus="exact direct UseGear previous instance/index and consumed gear; UID-less receiving contact"};
    }
}
// Exact real held Nameless -> explicitly chosen Powder, not direct Powder or arbitrary recursion.
internal static class NamelessPowderContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null && r.Key=="gear:gear_powder"
        && r.DefinitionId=="gear_powder" && r.SourceKind=="gear" && r.Family=="fire_localburst"
        && r.Renderer==(selected?"Hd2dNamelessPowderContactFx":"FireContinuousFx") && r.Element=="fire" && r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,GameEvent_DamageDealt hit,bool owned)
    {
        var receipt=CombatPresentationLedger.Read(hit);
        if(!Hd2dAttackPrototype.Enabled || !OriginalRoute(r) || !owned || hit.Source!="player" || hit.HpLoss<=0
            || hit.PresentationSource is not {Kind:"gear",DefinitionId:"gear_powder"} source
            || !string.IsNullOrEmpty(source.Uid) || receipt==null || receipt.Target!="enemy"
            || receipt.EnemyIndex!=hit.EnemyIndex || !CombatPresentationLedger.Valid(hit,receipt,out _))return r;
        return r with {Renderer="Hd2dNamelessPowderContactFx",PortStatus="exact Nameless UseGear previous instance/index, chosen Powder and ordered outer/inner GearUsed; UID-less contact"};
    }
}
// Exact real held Nameless -> explicitly chosen Keg, not direct Keg or arbitrary recursion.
internal static class NamelessKegContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null && r.Key=="gear:gear_powder_keg"
        && r.DefinitionId=="gear_powder_keg" && r.SourceKind=="gear" && r.Family=="fire_localburst"
        && r.Renderer==(selected?"Hd2dNamelessKegContactFx":"FireContinuousFx") && r.Element=="fire" && r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,GameEvent_DamageDealt hit,bool owned)
    {
        var receipt=CombatPresentationLedger.Read(hit);
        if(!Hd2dAttackPrototype.Enabled || !OriginalRoute(r) || !owned || hit.Source!="player" || hit.HpLoss<=0
            || hit.PresentationSource is not {Kind:"gear",DefinitionId:"gear_powder_keg"} source
            || !string.IsNullOrEmpty(source.Uid) || receipt==null || receipt.Target!="enemy"
            || receipt.EnemyIndex!=hit.EnemyIndex || !CombatPresentationLedger.Valid(hit,receipt,out _))return r;
        return r with {Renderer="Hd2dNamelessKegContactFx",PortStatus="exact Nameless UseGear previous instance/index, chosen Keg and ordered outer/inner GearUsed; UID-less contact"};
    }
}

// Exact owning-card allowlists; these are receiver-local support events, never hits.
// (経路の部分だけ。出来事から支えの経路を選ぶ Describe は Unity の出来事の流れに合わせて Hd2dOwnership.DescribeSupport に書いた)
internal static class SupportVfxProfiles
{
    internal static bool IceWallRoute(AttackRoute? route)=>Hd2dAttackPrototype.Enabled && route is not null
        && route.Key=="support:blue_ice_wall/ice-gain" && route.DefinitionId=="blue_ice_wall"
        && route.SourceKind=="support" && route.Family=="ice_guard_gain"
        && route.Renderer=="Hd2dSupportFx" && route.Element=="ice" && route.FixedSmall;
    internal static bool MassHealRoute(AttackRoute? route)=>Hd2dAttackPrototype.Enabled && route is not null
        && route.Key=="support:white_mass_heal/heal" && route.DefinitionId=="white_mass_heal"
        && route.SourceKind=="support" && route.Family=="prayer_heal"
        && route.Renderer=="Hd2dSupportFx" && route.Element=="light" && route.FixedSmall;
    internal static bool IceOwner(string? id)=>id is "blue_guard" or "blue_thick_ice" or "blue_ice_wall" or "blue_frost_chain";
    internal static bool RecoveryOwner(string? id)=>id is "white_prayer_verse" or "white_mass_heal";
    internal static bool OrdinaryGuardOwner(string? card,int? modeIndex)=>card!=null
        && (card is "green_guard" or "white_guard" or "green_catalyst_light" or "red_catalyst_light" or "white_light_hoard" || (card=="green_entangle" && modeIndex==0));
    // SourceHeroBlockFloatModel.ExactGain (Godot) の写し
    internal static bool ExactGain(int amount, int blockBefore, int blockAfter, int hpBefore, int hpAfter, int maxBefore, int maxAfter, int? iceBefore, int? iceAfter)
    {
        if (amount > 0 && (long)blockAfter - (long)blockBefore == amount && hpBefore == hpAfter && maxBefore == maxAfter && iceBefore.HasValue && iceAfter.HasValue)
            return iceBefore == iceAfter;
        return false;
    }
    internal static AttackRoute GuardRoute(string ownerCard)=>new AttackRoute($"support:{ownerCard}/block",ownerCard,"support","ordinary_guard_gain","Hd2dSupportFx","physical",true,"validated primary-command ordinary block");
    internal static AttackRoute HealRoute(string ownerCard)=>new AttackRoute($"support:{ownerCard}/heal",ownerCard,"support","prayer_heal","Hd2dSupportFx","light",true,"validated owner-action recovery");
    internal static AttackRoute IceRoute(string ownerCard)=>new AttackRoute($"support:{ownerCard}/ice-gain",ownerCard,"support","ice_guard_gain","Hd2dSupportFx","ice",true,"validated owner-action defensive gain");
    // EffectScenario(Small,0,.016,sustain,release) の Duration = .016+sustain+release (Godot の SupportVfxProfiles.Describe の値)
    internal const float GuardDuration=.016f+.174f+.31f, HealDuration=.016f+.244f+.40f, IceDuration=.016f+.144f+.36f;
}

// NativeGeometryBridge の経路の判定 (Godot では描き手の橋にあった 2 つ)
internal static class ObjectContact415Routes
{
    internal static bool ObjectContact415Route(AttackRoute r) =>
        r.SourceKind=="enemy_actual_execution" && r.Renderer=="BoundedContactFx" && r.Element=="neutral" && !r.FixedSmall &&
        ((r.Key=="enemy:enemy_set_breaker/toss_junk" && r.DefinitionId=="toss_junk" && r.Family=="neutral_thrown_object_contact") ||
         (r.Key=="enemy:enemy_kin_follower/boomerang" && r.DefinitionId=="boomerang" && r.Family=="curved_object_contact"));
    //385 compact botanical seeds; no terrain/actor illumination, no gameplay tick.
    //397: exact generic WarmSpark card material only; no shared Scatter family edit.
    internal static bool WarmSpark397Route(AttackRoute? route)
        =>route is not null && route.SourceKind=="card" && route.Family=="scatter_warm_spark"
        && route.Renderer=="ScatterProfileFx" && route.Element=="light" && !route.FixedSmall
        && route.Key=="card:"+route.DefinitionId
        && route.DefinitionId is "white_spark_storm" or "white_spark_scatter" or "white_light_cinders";
    internal static bool IsExactMaelstromRoute(AttackRoute? route) =>
        route != null && route.Key == "card:blue_maelstrom"
        && route.DefinitionId == "blue_maelstrom" && route.SourceKind == "card"
        && route.Family == "local_water_flow" && route.Renderer == "LocalMaterialProfileFx"
        && route.Element == "water" && !route.FixedSmall;
}
}
