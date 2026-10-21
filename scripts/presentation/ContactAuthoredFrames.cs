using System;
using System.Collections.Generic;
using System.Linq;
namespace DeckRogue.GodotPort.Presentation;
// Pure registered contact selector; Owned reactions are scoped to the two small enemies, wary shadow, thief, probe and completed turtle and colossus. Completed profiles explicitly opt into strict support token cancellation. Snapshot shape reuses the published brute QA contract.
// Every closing jaw requires an already accepted real contact. This owns no timers/fences.
public sealed class ContactAuthoredFrames
{
 private readonly IReadOnlyDictionary<string,float[]> _clips;private readonly string _profile,_allowedMove,_prepare,_impact;
 public readonly record struct ReactionToken(long InstanceId,long Epoch,int OwnerVersion){public bool IsValid=>InstanceId>0&&Epoch>0&&OwnerVersion>=0;}
 private readonly bool _furnace,_chimera3,_winch,_chimera1,_chimera2,_sealOrb,_warden,_breaker,_priest,_spawner,_cannon,_kin,_crusher,_healer,_globe,_raptorEgg,_berserker,_djinn,_devourer,_mimic,_ownedReactions,_hasGuard,_thief,_probe,_turtle,_tree,_drummer,_spore,_hexer,_colossus,_cultist,_mossSlime,_goldRaven,_owl,_wideBeast,_bigSlime,_squire,_bondWolf,_sergeant,_mournBeast,_sculptor,_broodRaptor,_bowlBug,_broodToad,_broodling,_chick,_reboot,_shellGuard,_moss,_beetle,_deathless,_worm,_wraith,_axeOgre,_forgottenSoul,_husk,_thirdHusk,_mudling,_lostSoul,_serpent,_stabBook,_mudLump,_sludgeSpider,_whetstone,_ironClam,_giantFace,_doomChanter,_axeAutomaton,_bitingScroll,_slug,_hazeStag,_cinderImp,_scaldGnat,_bomber,_chomper,_mawHunter,_cogConstruct,_gapingMaw,_joker,_frogKnight,_eliteSlaver,_ironEgg,_sentry;
 public bool UsesStrictSupportOwnership=>_furnace||_chimera3||_winch||_chimera1||_chimera2||_sealOrb||_warden||_breaker||_priest||_spawner||_cannon||_kin||_crusher||_healer||_globe||_raptorEgg||_berserker||_devourer||_ironEgg||_sentry||_mimic||_frogKnight||_eliteSlaver||_gapingMaw||_joker||_mawHunter||_cogConstruct||_bomber||_chomper||_cinderImp||_scaldGnat||_slug||_hazeStag||_axeAutomaton||_bitingScroll||_giantFace||_doomChanter||_whetstone||_ironClam||_mudLump||_sludgeSpider||_serpent||_stabBook||_mudling||_lostSoul||_husk||_axeOgre||_forgottenSoul||_worm||_wraith||_beetle||_deathless||_shellGuard||_moss||_chick||_reboot||_broodToad||_broodling||_broodRaptor||_bowlBug||_mournBeast||_sculptor||_bondWolf||_sergeant||_bigSlime||_squire||_wideBeast||_turtle||_tree||_drummer||_spore||_hexer||_colossus||_cultist||_mossSlime||_goldRaven||_owl;
 public bool RequiresActualOwnBlockPulse=>_furnace||_winch||_sealOrb||_warden||_breaker||_priest||_spawner||_cannon||_kin||_crusher||_healer||_globe||_raptorEgg||_berserker||_djinn||_ironEgg||_sentry||_frogKnight||_eliteSlaver||_gapingMaw||_joker||_mawHunter||_cogConstruct||_bomber||_chomper||_cinderImp||_scaldGnat||_slug||_hazeStag||_axeAutomaton||_bitingScroll||_giantFace||_doomChanter||_whetstone||_ironClam||_mudLump||_sludgeSpider||_serpent||_stabBook||_mudling||_lostSoul||_thirdHusk||_axeOgre||_forgottenSoul||_worm||_wraith||_beetle||_deathless||_shellGuard||_moss||_chick||_reboot||_broodToad||_broodling||_broodRaptor||_bowlBug||_mournBeast||_sculptor||_bondWolf||_sergeant||_bigSlime||_squire||_wideBeast||_colossus||_cultist||_mossSlime||_goldRaven||_owl;
 // Only explicitly registered profiles opt into real move-typed Block/self-Strength pulses.
 // The old Block-only gate is retained byte-for-byte for every earlier profile.
 public bool UsesMoveTypedSupportPulse=>_furnace||_winch||_sealOrb||_warden||_breaker||_priest||_spawner||_cannon||_kin||_crusher||_healer||_globe||_raptorEgg||_berserker||_djinn||_ironEgg||_sentry||_frogKnight||_eliteSlaver||_gapingMaw||_joker||_mawHunter||_cogConstruct||_bomber||_chomper||_cinderImp||_scaldGnat||_slug||_hazeStag||_axeAutomaton||_bitingScroll||_giantFace||_doomChanter||_whetstone||_ironClam;
 public bool AcceptsMoveTypedSupportPulse(Style04EnemySupportMotion.Token token,Style04EnemySupportMotion.Snapshot s,bool ownBlock,bool ownStrength,bool ownSummon=false,bool ownTokenDestroyed=false,bool ownCardSealed=false)=>UsesMoveTypedSupportPulse&&OwnsSupportToken(token,s)&&s.AcceptedPulses==0&&(_sealOrb&&s.Kind=="seal"?ownCardSealed&&!ownBlock&&!ownStrength&&!ownSummon&&!ownTokenDestroyed:!ownCardSealed&&(_breaker&&s.Kind=="destroy-token"?ownTokenDestroyed&&!ownBlock&&!ownStrength&&!ownSummon:!ownTokenDestroyed&&((_winch||_spawner)&&s.Kind=="summon"?ownSummon&&!ownBlock&&!ownStrength:s.Kind=="defend"?(ownBlock&&!ownStrength&&!ownSummon):(s.Kind=="buff"&&ownStrength&&!ownBlock&&!ownSummon))));
 private float _restResolved=-1,_resolved=-1,_fledAt=-1,_fledUntil=-1;
 public bool HasFleeTerminal(int owner)=>_thief&&!_cancelled&&_fledAt>=0&&owner==_registeredOwner;
 public float FleeProgress(float now,int owner)=>HasFleeTerminal(owner)?Math.Clamp((now-_fledAt)/Math.Max(.000001f,_fledUntil-_fledAt),0,1):0;
 private bool OwnsSupport(Style04EnemySupportMotion.Snapshot s)=>s.ProfileId==_profile&&s.OwnerVersion==_registeredOwner&&s.InstanceId==_supportInstance&&s.Version==_supportVersion&&s.ExecutionIndex==_supportExecution;
 private bool SupportMove(Style04EnemySupportMotion.Snapshot s)=>_furnace?(s.Kind=="defend"&&s.MoveId is "stoke" or "inflame"):_chimera3?(s.Kind=="hex"&&s.MoveId=="burning_growl"):_winch?((s.Kind=="summon"&&s.MoveId=="release_orbs")||(s.Kind=="defend"&&s.MoveId=="wind_up")||(s.Kind=="rest"&&s.MoveId=="rewind")):(_chimera1||_chimera2)?false:_sealOrb?((s.Kind=="seal"&&s.MoveId=="seal")||(s.Kind=="defend"&&s.MoveId=="guard")):_warden?(s.Kind=="defend"&&s.MoveId=="guard"):_breaker?((s.Kind=="defend"&&s.MoveId=="ready_tools")||(s.Kind=="destroy-token"&&s.MoveId=="break_token")):_priest?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="buff"&&s.MoveId=="ritual")):_spawner?((s.Kind=="summon"&&s.MoveId=="spawn")||(s.Kind=="defend"&&s.MoveId=="guard")):_cannon?((s.Kind=="buff"&&s.MoveId=="charge")||(s.Kind=="rest"&&s.MoveId=="recharge")):_kin?((s.Kind=="buff"&&s.MoveId=="dance")||(s.Kind=="defend"&&s.MoveId=="guard")):_crusher?((s.Kind=="buff"&&s.MoveId=="adapt")||(s.Kind=="defend"&&s.MoveId=="guard")):_healer?((s.Kind=="heal"&&s.MoveId=="mend")||(s.Kind=="defend"&&s.MoveId=="guard")):_globe?(s.Kind=="defend"&&s.MoveId=="guard"):_raptorEgg?((s.Kind=="defend"&&s.MoveId=="quiver")||(s.Kind=="hatch"&&s.MoveId=="hatch")):_berserker?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="hex"&&s.MoveId=="vomit")):_djinn?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="hex"&&s.MoveId=="gaze")):_devourer?(s.Kind=="mill"&&s.MoveId=="devour"):_ironEgg?((s.Kind=="defend"&&s.MoveId=="sleep")||(s.Kind=="buff"&&s.MoveId=="awaken")):_sentry?(s.Kind=="defend"&&s.MoveId=="guard"):_frogKnight?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="buff"&&s.MoveId=="for_the_queen")):_eliteSlaver?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="hex"&&s.MoveId=="net")):_gapingMaw?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="hex"&&s.MoveId=="roar")):_joker?(s.Kind=="defend"&&s.MoveId=="dodge_prance"):_mawHunter?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="hex"&&s.MoveId=="web")):_cogConstruct?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="buff"&&s.MoveId=="charge_up")):_bomber?(s.Kind=="defend"&&s.MoveId=="shield_barrel"):_chomper?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="hex"&&s.MoveId=="screech")):(_cinderImp||_scaldGnat)?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="hex"&&s.MoveId==(_cinderImp?"fan_flames":"scald_spit"))):_slug?(s.Kind=="defend"&&s.MoveId=="guard"):_hazeStag?(s.Kind=="defend"&&s.MoveId=="fade_in_mist"):_axeAutomaton?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="buff"&&s.MoveId=="sharpen")):_bitingScroll?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="buff"&&s.MoveId=="more_teeth")):_giantFace?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="buff"&&s.MoveId=="glare")):_doomChanter?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="buff"&&s.MoveId=="crescendo")):_whetstone?((s.Kind=="defend"&&s.MoveId=="brace")||(s.Kind=="buff"&&s.MoveId=="hone")):_ironClam?((s.Kind=="defend"&&s.MoveId=="guard")||(s.Kind=="buff"&&s.MoveId=="pressurize")):_axeOgre?(s.Kind=="defend"&&s.MoveId=="windup")||(s.Kind=="rest"&&s.MoveId=="gasp"):_thief?((s.Kind=="defend"&&s.MoveId=="sneak")||(s.Kind=="steal-gold"&&s.MoveId=="snatch")||(s.Kind=="flee"&&s.MoveId=="run_off")):((_hasGuard&&s.Kind=="defend"&&s.MoveId==(_mudLump?"mud_guard":_worm?"burrow_down":(_beetle||_shellGuard)?"harden":_moss?"moss_shell":_reboot?"boot_up":_broodToad?"squat":_broodRaptor?"shelter":_bowlBug?"curl":_mournBeast?"lick":_bondWolf?"prowl":_bigSlime?"ooze_guard":_squire?"brace":_wideBeast?"coil":_owl?"brace":_goldRaven?"soar":_mossSlime?"ooze":_colossus?"brace":_turtle?"shell":"guard"))||(_drummer&&s.Kind=="rally"&&s.MoveId=="war_drum")||(_spore&&s.Kind=="hex"&&s.MoveId=="vuln_spores")||(_hexer&&s.Kind=="hex"&&s.MoveId=="curse")||(_mudling&&s.Kind=="hex"&&s.MoveId=="slop"));
 // Separate axe-rest receipt path keeps the existing thief resolver unchanged.
 public bool AcceptedRestResolved(Style04EnemySupportMotion.Token token,Style04EnemySupportMotion.Snapshot s){if(!(_winch||_axeOgre||_cannon)||_cancelled||_restResolved>=0||!s.Active||s.Kind!="rest"||s.MoveId!=(_winch?"rewind":_cannon?"recharge":"gasp")||!OwnsSupport(s)||!token.IsValid||token.InstanceId!=s.InstanceId||token.Serial!=s.Version||token.ExecutionIndex!=s.ExecutionIndex||token.OwnerVersion!=s.OwnerVersion||!float.IsFinite(s.EndRequestedAt)||s.EndRequestedAt<0||s.EndRequestedAt<s.StartedAt||s.EndRequestedAt>=s.EndAt)return false;_restResolved=s.EndRequestedAt;return true;}
 public bool AcceptedResolved(Style04EnemySupportMotion.Token token,Style04EnemySupportMotion.Snapshot s){if(!_thief||_cancelled||_resolved>=0||!s.Active||s.Kind!="steal-gold"||s.MoveId!="snatch"||!OwnsSupport(s)||!token.IsValid||token.InstanceId!=s.InstanceId||token.Serial!=s.Version||token.ExecutionIndex!=s.ExecutionIndex||token.OwnerVersion!=s.OwnerVersion||!float.IsFinite(s.EndRequestedAt)||s.EndRequestedAt<0||s.EndRequestedAt<s.StartedAt||s.EndRequestedAt>=s.EndAt)return false;_resolved=s.EndRequestedAt;return true;}
 // Called only from the current Stage EnemyFled callback, before Dead is applied.
 public bool AcceptedFled(Style04EnemySupportMotion.Token token,Style04EnemySupportMotion.Snapshot s,float now,float duration){if(!_thief||_cancelled||_fledAt>=0||!s.Active||s.Kind!="flee"||s.MoveId!="run_off"||!OwnsSupport(s)||!token.IsValid||token.InstanceId!=s.InstanceId||token.Serial!=s.Version||token.ExecutionIndex!=s.ExecutionIndex||token.OwnerVersion!=s.OwnerVersion||!float.IsFinite(now)||now<s.StartedAt||now>=s.Deadline||!float.IsFinite(duration)||duration<=0||duration>.55f)return false;_fledAt=now;_fledUntil=now+duration;_reaction="";return true;}
 private static long _nextReactionInstance;private readonly long _reactionInstance=System.Threading.Interlocked.Increment(ref _nextReactionInstance);
 private long _supportInstance=-1,_supportVersion=-1;private int _supportExecution=-1;
 private long _reactionEpoch;private int _registeredOwner=-1;private float _epochStarted=-1;
 public ReactionToken CaptureReactionToken()=>_ownedReactions&&!_cancelled&&_registeredOwner>=0?new(_reactionInstance,_reactionEpoch,_registeredOwner):default;
 private void RegisterReactionOwner(float now,int owner){ClearHeal();ClearMill();_mimicProof=null;_mimicAccepted=0;_mimicLastAt=-1;_restResolved=_resolved=_fledAt=_fledUntil=-1;_supportInstance=_supportVersion=-1;_supportExecution=-1;_reactionEpoch++;_registeredOwner=owner;_epochStarted=now;_reaction="";_reactionAt=-1;_reactionOwner=-1;if(UsesStrictSupportOwnership){_started=-1;_owner=_version=_execution=-1;_count=0;_move="";}}
private MossHealAppliedOncePlan? _healPlan;
private int _healGeneration=-1;
private float _healOutcomeAt=-1,_healAppliedAt=-1;
public float HealOutcomeAt=>_healOutcomeAt;
public float HealAppliedAt=>_healAppliedAt;
public int HealAmount=>_healOutcomeAt>=0?_healPlan?.Amount??0:0;
public int HealTargetIndex=>_healOutcomeAt>=0?_healPlan?.TargetIndex??-1:-1;
public int HealOutcomeEventIndex=>_healOutcomeAt>=0?_healPlan?.OutcomeIndex??-1:-1;
private void ClearHeal(){_healPlan=null;_healGeneration=-1;_healOutcomeAt=_healAppliedAt=-1;}
public bool ReserveHeal(Style04EnemySupportMotion.Token token,
 Style04EnemySupportMotion.Snapshot s,MossHealAppliedOncePlan plan,int generation){
 if(!_healer||_cancelled||_healPlan!=null||generation<0||!s.Active||
    s.Kind!="heal"||s.MoveId!="mend"||s.EndRequestedAt>=0||
    !OwnsSupportToken(token,s)||plan.ExecutionIndex!=s.ExecutionIndex)return false;
 _healPlan=plan;_healGeneration=generation;return true;
}
public bool AcceptHealOutcome(Style04EnemySupportMotion.Token token,
 Style04EnemySupportMotion.Snapshot s,DeckRogue.Engine.Generated.GameEvent e,
 int index,int generation,float now){
 if(!_healer||_cancelled||_healOutcomeAt>=0||_healPlan==null||
    generation!=_healGeneration||!_healPlan.IsOutcome(e,index)||
    !OwnsSupportToken(token,s)||!s.Active||s.EndRequestedAt>=0||
    !float.IsFinite(now)||now<s.StartedAt||now>=s.Deadline||now>=s.EndAt)return false;
 _healOutcomeAt=now;if(_healPlan.Positive)_healAppliedAt=now;return true;
}

public Style04EnemySupportMotion.Token HealOutcomeToken=>_healOutcomeAt>=0?new(_supportInstance,_supportVersion,_supportExecution,_registeredOwner):default;

 private DevourerMillAppliedOncePlan? _millPlan;private int _millGeneration=-1;private float _millAt=-1;
 public float MillAppliedAt=>_millAt;
 public int MillAppliedCount=>_millAt>=0?_millPlan?.Count??0:0;
 public int MillAppliedEventIndex=>_millAt>=0?_millPlan?.AppliedIndex??-1:-1;
 public Style04EnemySupportMotion.Token MillAppliedToken=>_millAt>=0?new(_supportInstance,_supportVersion,_supportExecution,_registeredOwner):default;
 private void ClearMill(){_millPlan=null;_millGeneration=-1;_millAt=-1;}
 public bool ReserveMill(Style04EnemySupportMotion.Token token,Style04EnemySupportMotion.Snapshot s,DevourerMillAppliedOncePlan plan,int generation){
  if(!_devourer||_cancelled||_millPlan!=null||_millAt>=0||generation<0||!s.Active||s.EndRequestedAt>=0||!OwnsSupportToken(token,s)||plan.ExecutionIndex!=s.ExecutionIndex)return false;
  _millPlan=plan;_millGeneration=generation;return true;
 }
 public bool AcceptMill(Style04EnemySupportMotion.Token token,Style04EnemySupportMotion.Snapshot s,DeckRogue.Engine.Generated.GameEvent e,int index,int generation,float now){
  if(!_devourer||_cancelled||_millAt>=0||_millPlan==null||generation!=_millGeneration||!_millPlan.IsApplied(e,index)||!OwnsSupportToken(token,s)||!s.Active||s.EndRequestedAt>=0||!float.IsFinite(now)||now<s.StartedAt||now>=s.Deadline||now>=s.EndAt)return false;
  _millAt=now;return true;
 }
 private MimicAuthoredCountProof? _mimicProof;private int _mimicAccepted;private float _mimicLastAt=-1;
 public int AuthenticatedEngineHits=>_mimicProof?.EngineHits??0;
 public int AuthenticatedPresentationContacts=>_mimicProof?.PresentationContacts??0;
 public string? AuthenticatedPresentationReason=>_mimicProof?.PresentationReason;
 private bool _cancelled;private float _started=-1;private int _owner=-1,_version=-1,_execution=-1,_count;
 private string _move="",_reaction="";private float _reactionAt=-1;private int _reactionOwner=-1;
 public BruteAuthoredFrames.Snapshot State{get;private set;}=BruteAuthoredFrames.Snapshot.Disabled;
 public ContactAuthoredFrames(string profile,IReadOnlyDictionary<string,float[]> clips){_profile=profile;_allowedMove=profile switch{"enemy_ember_furnace"=>"devour_ember","enemy_chimera_3"=>"lacerate","enemy_winch_warden"=>"flail","enemy_chimera_1"=>"bite","enemy_chimera_2"=>"swelling_claws","enemy_seal_orb"=>"beam","enemy_warden"=>"bell_strike","enemy_set_breaker"=>"smash","enemy_kin_priest"=>"orb_frail","enemy_moss_spawner"=>"lash","enemy_crab_cannon"=>"reticle","enemy_kin_follower"=>"slash","enemy_crab_crusher"=>"thrash","enemy_moss_healer"=>"staff_poke","enemy_thunder_globe"=>"shock_slap","enemy_raptor_egg"=>"","enemy_sludge_berserker"=>"pummel","enemy_elite_mirror_djinn"=>"mirror","enemy_elite_devourer"=>"bite","enemy_elite_iron_egg"=>"tail","enemy_elite_sentry"=>"bolt","enemy_mimic_imp"=>"mimic","enemy_mimic_jester"=>"mirror_dance","enemy_frog_knight"=>"tongue_lash","enemy_elite_slaver"=>"chain","enemy_gaping_maw"=>"claw","enemy_joker"=>"wild_swing","enemy_maw_hunter"=>"bite","enemy_cog_construct"=>"repeater","enemy_bomber"=>"toss_ember","enemy_chomper"=>"clamp","enemy_cinder_imp" or "enemy_scald_gnat"=>"bite","enemy_slug"=>"lick","enemy_haze_stag"=>"antler_jab","enemy_axe_automaton"=>"one_two","enemy_biting_scroll"=>"chomp","enemy_elite_giant_face"=>"crush","enemy_elite_doom_chanter"=>"dual_chant","enemy_whetstone_colossus"=>"guillotine","enemy_iron_clam"=>"jet","enemy_mud_lump" or "enemy_sludge_spider"=>"slam","enemy_strangler_serpent"=>"constrict","enemy_elite_stab_book"=>"stab2","enemy_mudling"=>"bump","enemy_lost_soul"=>"debilitating_smog","enemy_elite_husk_3" or "enemy_elite_husk_2" or "enemy_elite_husk_1"=>"slash","enemy_axe_ogre"=>"great_swing","enemy_forgotten_soul"=>"miasma","enemy_burrow_worm"=>"tremor","enemy_nemesis_wraith"=>"curse_touch","enemy_rock_beetle"=>"horn_jab","enemy_elite_deathless"=>"cleave","enemy_shell_guard"=>"shell_bash","enemy_moss"=>"slam","enemy_raptor_chick"=>"peck","enemy_axe_automaton_reboot"=>"one_two","enemy_brood_toad"=>"tongue","enemy_broodling"=>"gnaw","enemy_brood_raptor"=>"claw","enemy_bowl_bug"=>"roll","enemy_mourn_beast"=>"bite","enemy_devoted_sculptor"=>"carve_rite","enemy_bond_wolf"=>"twin_bite","enemy_elite_sergeant"=>"whip","enemy_big_slime"=>"slam","enemy_shield_squire"=>"shield_bash","enemy_wide_power"=>"surge","enemy_elite_owl"=>"wing","enemy_elite_gold_raven"=>"dive","enemy_moss_slime"=>"bump","enemy_cultist"=>"dark_blade","enemy_apprentice_colossus"=>"swing","enemy_hexer"=>"slap","enemy_spore_cap"=>"smash","enemy_drummer"=>"thump","enemy_probe"=>"poke","enemy_turtle"=>"awake_bite","enemy_vine_walker"=>"swipe","enemy_thorn_squirrel"=>"gnaw","enemy_snap_fruit"=>"snap","enemy_set_wary"=>"slash","enemy_thief"=>"mug",_=>throw new ArgumentException("Unregistered contact-authored profile")};_mimic=MimicAuthoredCountProof.Supports(profile);_furnace=profile=="enemy_ember_furnace";_chimera3=profile=="enemy_chimera_3";_winch=profile=="enemy_winch_warden";_chimera1=profile=="enemy_chimera_1";_chimera2=profile=="enemy_chimera_2";_sealOrb=profile=="enemy_seal_orb";_warden=profile=="enemy_warden";_breaker=profile=="enemy_set_breaker";_priest=profile=="enemy_kin_priest";_spawner=profile=="enemy_moss_spawner";_cannon=profile=="enemy_crab_cannon";_kin=profile=="enemy_kin_follower";_crusher=profile=="enemy_crab_crusher";_healer=profile=="enemy_moss_healer";_globe=profile=="enemy_thunder_globe";_raptorEgg=profile=="enemy_raptor_egg";_berserker=profile=="enemy_sludge_berserker";_djinn=profile=="enemy_elite_mirror_djinn";_devourer=profile=="enemy_elite_devourer";_ironEgg=profile=="enemy_elite_iron_egg";_sentry=profile=="enemy_elite_sentry";_frogKnight=profile=="enemy_frog_knight";_eliteSlaver=profile=="enemy_elite_slaver";_gapingMaw=profile=="enemy_gaping_maw";_joker=profile=="enemy_joker";_mawHunter=profile=="enemy_maw_hunter";_cogConstruct=profile=="enemy_cog_construct";_bomber=profile=="enemy_bomber";_chomper=profile=="enemy_chomper";_cinderImp=profile=="enemy_cinder_imp";_scaldGnat=profile=="enemy_scald_gnat";_slug=profile=="enemy_slug";_hazeStag=profile=="enemy_haze_stag";_axeAutomaton=profile=="enemy_axe_automaton";_bitingScroll=profile=="enemy_biting_scroll";_giantFace=profile=="enemy_elite_giant_face";_doomChanter=profile=="enemy_elite_doom_chanter";_whetstone=profile=="enemy_whetstone_colossus";_ironClam=profile=="enemy_iron_clam";_mudLump=profile=="enemy_mud_lump";_sludgeSpider=profile=="enemy_sludge_spider";_serpent=profile=="enemy_strangler_serpent";_stabBook=profile=="enemy_elite_stab_book";_mudling=profile=="enemy_mudling";_lostSoul=profile=="enemy_lost_soul";_probe=profile=="enemy_probe";_turtle=profile=="enemy_turtle";_tree=profile=="enemy_vine_walker";_drummer=profile=="enemy_drummer";_spore=profile=="enemy_spore_cap";_hexer=profile=="enemy_hexer";_colossus=profile=="enemy_apprentice_colossus";_cultist=profile=="enemy_cultist";_mossSlime=profile=="enemy_moss_slime";_goldRaven=profile=="enemy_elite_gold_raven";_owl=profile=="enemy_elite_owl";_wideBeast=profile=="enemy_wide_power";_bigSlime=profile=="enemy_big_slime";_squire=profile=="enemy_shield_squire";_bondWolf=profile=="enemy_bond_wolf";_sergeant=profile=="enemy_elite_sergeant";_mournBeast=profile=="enemy_mourn_beast";_sculptor=profile=="enemy_devoted_sculptor";_broodRaptor=profile=="enemy_brood_raptor";_bowlBug=profile=="enemy_bowl_bug";_broodToad=profile=="enemy_brood_toad";_broodling=profile=="enemy_broodling";_chick=profile=="enemy_raptor_chick";_reboot=profile=="enemy_axe_automaton_reboot";_shellGuard=profile=="enemy_shell_guard";_moss=profile=="enemy_moss";_beetle=profile=="enemy_rock_beetle";_deathless=profile=="enemy_elite_deathless";_worm=profile=="enemy_burrow_worm";_wraith=profile=="enemy_nemesis_wraith";_axeOgre=profile=="enemy_axe_ogre";_forgottenSoul=profile=="enemy_forgotten_soul";_thirdHusk=profile=="enemy_elite_husk_3";_husk=_thirdHusk||profile is "enemy_elite_husk_2" or "enemy_elite_husk_1";_ownedReactions=_furnace||_chimera3||_winch||_chimera1||_chimera2||_sealOrb||_warden||_breaker||_priest||_spawner||_cannon||_kin||_crusher||_healer||_globe||_raptorEgg||_berserker||_devourer||_ironEgg||_sentry||_mimic||_frogKnight||_eliteSlaver||_gapingMaw||_joker||_mawHunter||_cogConstruct||_bomber||_chomper||_cinderImp||_scaldGnat||_slug||_hazeStag||_axeAutomaton||_bitingScroll||_giantFace||_doomChanter||_whetstone||_ironClam||_mudLump||_sludgeSpider||_serpent||_stabBook||_mudling||_lostSoul||_husk||_axeOgre||_forgottenSoul||_worm||_wraith||_beetle||_deathless||_shellGuard||_moss||_chick||_reboot||_broodToad||_broodling||_broodRaptor||_bowlBug||_mournBeast||_sculptor||_bondWolf||_sergeant||_bigSlime||_squire||_wideBeast||_owl||_goldRaven||_mossSlime||_cultist||_colossus||_probe||_turtle||_tree||_drummer||_spore||_hexer||profile is "enemy_thorn_squirrel" or "enemy_snap_fruit" or "enemy_set_wary" or "enemy_thief";_thief=profile=="enemy_thief";_hasGuard=_furnace||_winch||_sealOrb||_warden||_breaker||_priest||_spawner||_kin||_crusher||_healer||_globe||_raptorEgg||_berserker||_djinn||_ironEgg||_sentry||_frogKnight||_eliteSlaver||_gapingMaw||_joker||_mawHunter||_cogConstruct||_bomber||_chomper||_cinderImp||_scaldGnat||_slug||_hazeStag||_axeAutomaton||_bitingScroll||_giantFace||_doomChanter||_whetstone||_ironClam||_mudLump||_sludgeSpider||_serpent||_stabBook||_mudling||_lostSoul||_thirdHusk||_axeOgre||_forgottenSoul||_worm||_beetle||_deathless||_shellGuard||_moss||_reboot||_broodToad||_broodRaptor||_bowlBug||_mournBeast||_sculptor||_bondWolf||_sergeant||_bigSlime||_squire||_wideBeast||_owl||_goldRaven||_mossSlime||_cultist||_colossus||_probe||_turtle||_tree||_drummer||_spore||_hexer||profile is "enemy_snap_fruit" or "enemy_set_wary" or "enemy_thief";_prepare=_furnace?"devourprepare":_chimera3?"clawprepare":_winch?"flailprepare":_chimera1?"biteprepare":_chimera2?"clawprepare":_sealOrb?"beamprepare":_warden?"strikeprepare":_breaker?"smashprepare":_priest?"frailprepare":_spawner?"lashprepare":_cannon?"reticleprepare":_kin?"slashprepare":_crusher?"thrashprepare":_healer?"pokeprepare":_globe?"slapprepare":_raptorEgg?"quiverprepare":_berserker?"pummelprepare":_djinn?"mirrorprepare":_devourer?"biteprepare":_ironEgg?"tailprepare":_sentry?"boltprepare":_frogKnight?"tongueprepare":_eliteSlaver?"chainprepare":_gapingMaw?"clawprepare":_joker?"wildprepare":_mawHunter?"biteprepare":_cogConstruct?"repeaterprepare":_bomber?"emberprepare":_chomper?"clampprepare":(_cinderImp||_scaldGnat)?"biteprepare":_slug?"lickprepare":_hazeStag?"jabprepare":_axeAutomaton?"twoprepare":_bitingScroll?"chompprepare":_giantFace?"crushprepare":_doomChanter?"chantprepare":_whetstone?"guillotineprepare":_ironClam?"jetprepare":(_mudLump||_sludgeSpider)?"slamprepare":_serpent?"constrictprepare":_stabBook?"stabprepare":_mudling?"bumpprepare":_lostSoul?"smogprepare":_axeOgre?"swingprepare":_forgottenSoul?"miasmaprepare":_worm?"tremorprepare":_wraith?"curseprepare":_beetle?"hornprepare":_deathless?"cleaveprepare":_shellGuard?"bashprepare":_moss?"slamprepare":_chick?"peckprepare":_reboot?"twoprepare":_broodToad?"tongueprepare":_broodling?"gnawprepare":_broodRaptor?"clawprepare":_bowlBug?"rollprepare":_mournBeast?"biteprepare":_sculptor?"carveprepare":_bondWolf?"biteprepare":_sergeant?"whipprepare":_bigSlime?"slamprepare":_squire?"bashprepare":_wideBeast?"surgeprepare":_owl?"wingprepare":_goldRaven?"diveprepare":_mossSlime?"bumpprepare":_cultist?"bladeprepare":_colossus?"attackprepare":_hexer?"slapprepare":_spore?"smashprepare":_drummer?"thumpprepare":profile=="enemy_vine_walker"||(_ownedReactions&&!_probe&&!_turtle)?"prepare":"windup";_impact=_furnace?"devourcontact":_chimera3?"clawcontacta":_winch?"flailcontacta":_chimera1?"bitecontact":_chimera2?"clawcontacta":_sealOrb?"beamrelease":_warden?"strikecontact":_breaker?"smashcontact":_priest?"frailrelease":_spawner?"lashcontact":_cannon?"reticlecontact":_kin?"slashcontact":_crusher?"thrashcontact":_healer?"pokecontact":_globe?"slapcontact":_raptorEgg?"quiverhold":_berserker?"contacta":_devourer?"bitecontact":_ironEgg?"tailcontact":_sentry?"boltrelease":_mimic?"contacta":_frogKnight?"tonguecontact":_eliteSlaver?"chaincontact":_gapingMaw?"clawcontact1":_joker?"wildcontact":_mawHunter?"bitecontact":_cogConstruct?"repeatercontact":_bomber?"emberrelease":_chomper?"clampcontact1":(_cinderImp||_scaldGnat)?"bitecontact":_slug?"lickcontact":_hazeStag?"jabcontact":_axeAutomaton?"axecontact":_bitingScroll?"chompcontact":_giantFace?"crushcontact":_doomChanter?"chantcontact1":_whetstone?"guillotinecontact":_ironClam?"jetrelease":(_mudLump||_sludgeSpider)?"slamcontact":_serpent?"constrictcontact":_stabBook?"stabcontact1":_mudling?"bumpcontact":_lostSoul?"smogcontact":_axeOgre?"swingcontact":_forgottenSoul?"miasmacontact":_worm?"tremorcontact":_wraith?"cursecontact":_beetle?"horncontact":_deathless?"cleavecontact":_shellGuard?"bashcontact":_moss?"slamcontact":_chick?"peckcontact":_reboot?"axecontact":_broodToad?"tonguecontact":_broodling?"gnawcontact":_broodRaptor?"clawcontact":_bowlBug?"rollcontact":_mournBeast?"bitecontact":_sculptor?"carvecontact":_bondWolf?"bitecontact":_sergeant?"whipcontact":_bigSlime?"slamcontact":_squire?"bashcontact":_wideBeast?"surgecontact":_owl?"wingcontact":_goldRaven?"divecontact":_mossSlime?"bumpcontact":_cultist?"bladecontact":_colossus?"attackcontact":_hexer?"slapcontact":_spore?"smashcontact":_drummer?"thumpimpact":profile=="enemy_vine_walker"||(_ownedReactions&&!_probe&&!_turtle)?"contact":"impact";if(!new[]{"rest",_prepare,_impact}.All(clips.ContainsKey)||clips.Any(p=>p.Value.Length==0||p.Value.Any(x=>!float.IsFinite(x)||x<=0)))throw new ArgumentException("Invalid contact clip durations");_clips=clips.ToDictionary(x=>x.Key,x=>(float[])x.Value.Clone());}
 public void Begin(){if(!_ownedReactions)_cancelled=false;}
 public void BeginSupport(float now,Style04EnemySupportMotion.Snapshot support){
  if(!_ownedReactions){Begin();return;}
  if(!SupportMove(support)||!float.IsFinite(now)||now<0||!support.Active||support.ProfileId!=_profile||support.OwnerVersion<0||(UsesStrictSupportOwnership&&(support.InstanceId<=0||support.Version<=0||support.ExecutionIndex<0||now<support.StartedAt||now>=support.Deadline)))return;
  _cancelled=false;RegisterReactionOwner(now,support.OwnerVersion);_supportInstance=support.InstanceId;_supportVersion=support.Version;_supportExecution=support.ExecutionIndex;
 }
 public void BeginAttack(float now,Style04EnemyMotion.Snapshot a,MimicAuthoredCountProof? mimicProof=null){if(_raptorEgg)return;if(_mimic&&(mimicProof==null||!mimicProof.Matches(a))){Cancel();return;}if(!float.IsFinite(now)||now<0||!a.Active||a.ProfileId!=_profile||(a.MoveId!=_allowedMove&&!(_furnace&&a.MoveId is "sear" or "tackle" or "inferno")&&!(_chimera3&&a.MoveId=="pounce")&&!(_winch&&a.MoveId=="hyper_beam")&&!(_chimera1&&a.MoveId=="skull_bash")&&!(_warden&&a.MoveId is "gate_guard" or "bell_hushed" or "bell_frenzy")&&!(_breaker&&a.MoveId is "toss_junk" or "break_trap")&&!(_priest&&a.MoveId is "orb_weak" or "beam" or "judgment_orb" or "blood_beam")&&!(_cannon&&a.MoveId is "precision_beam" or "laser")&&!(_kin&&a.MoveId=="boomerang")&&!(_crusher&&a.MoveId is "swell_strike" or "bug_sting" or "guarded_strike")&&!(_globe&&a.MoveId is "thunder_strike" or "galvanic_burst")&&!(_berserker&&a.MoveId is "hug" or "smother")&&!(_sentry&&a.MoveId=="beam")&&!(_frogKnight&&a.MoveId is "strike_down" or "beetle_charge")&&!(_eliteSlaver&&a.MoveId is "weight" or "lash")&&!(_gapingMaw&&a.MoveId=="rend")&&!(_joker&&a.MoveId is "cautious_jab" or "call_bluff")&&!(_mawHunter&&a.MoveId=="claws")&&!(_cogConstruct&&a.MoveId=="expel")&&!(_bomber&&a.MoveId=="big_boom")&&!(_cinderImp&&a.MoveId=="spark_toss")&&!(_scaldGnat&&a.MoveId=="suck")&&!(_slug&&a.MoveId is "acid_spit" or "tackle")&&!(_hazeStag&&a.MoveId is "hoof_flurry" or "antler_charge")&&!(_axeAutomaton&&a.MoveId=="hammer_upper")&&!(_bitingScroll&&a.MoveId=="chew")&&!(_mudLump&&a.MoveId is "slop" or "mud_toss")&&!(_sludgeSpider&&a.MoveId is "oil_spray" or "rage")&&!(_serpent&&a.MoveId is "lash" or "thwack")&&!(_stabBook&&a.MoveId is "stab3" or "stab4")&&!(_lostSoul&&a.MoveId=="eye_lasers")&&!(_forgottenSoul&&a.MoveId=="dread")&&!(_worm&&a.MoveId=="bite")&&!(_wraith&&a.MoveId=="scythe")&&!(_beetle&&a.MoveId=="bite")&&!(_deathless&&a.MoveId=="stance")&&!(_shellGuard&&a.MoveId=="cracked_rage")&&!(_moss&&a.MoveId=="bite")&&!(_reboot&&a.MoveId=="hammer_upper")&&!(_bowlBug&&a.MoveId=="tackle")&&!(_mournBeast&&a.MoveId=="leap")&&!(_sculptor&&a.MoveId=="savage")&&!(_bondWolf&&a.MoveId=="lunge")&&!(_sergeant&&a.MoveId=="double_whip")&&!(_squire&&a.MoveId=="avenging_rush")&&!(_owl&&a.MoveId=="talon_dive")&&!(_goldRaven&&a.MoveId=="peck")&&!(_spore&&a.MoveId=="frail_spores")&&!(_hexer&&a.MoveId=="mud")&&!(_probe&&a.MoveId=="lunge")&&!(_turtle&&a.MoveId=="crush")&&!(_tree&&a.MoveId is "grasping_vines" or "chomp"))||(_ownedReactions&&a.OwnerVersion<0))return;_cancelled=false;if(_ownedReactions)RegisterReactionOwner(now,a.OwnerVersion);_started=now;_owner=a.OwnerVersion;_version=a.Version;_execution=a.ExecutionIndex;_move=a.MoveId;_count=0;if(_mimic){_mimicProof=mimicProof;_count=mimicProof!.PresentationContacts;}}
 private bool Owns(Style04EnemyMotion.Snapshot a)=>a.ProfileId==_profile&&a.OwnerVersion==_owner&&a.Version==_version&&a.ExecutionIndex==_execution&&a.MoveId==_move;
 // M55 preflight runs before Motion mutates. Legacy profiles retain their old gate.
 public bool CanAcceptMimicContact(Style04EnemyMotion.Snapshot a,float now,int owner,int ordinal,int count){
  if(!_mimic)return true;
  return !_cancelled&&_mimicProof!=null&&_mimicProof.Matches(a)&&Owns(a)&&
   owner==_owner&&owner==_registeredOwner&&float.IsFinite(now)&&now>=_started&&
   (_mimicLastAt<0||now>=_mimicLastAt)&&count==_mimicProof.PresentationContacts&&
   ordinal==_mimicAccepted&&ordinal>=0&&ordinal<count&&
   a.AcceptedContacts==_mimicAccepted&&a.LastOrdinal==_mimicAccepted-1;
 }
 private void AcceptedMimicContact(Style04EnemyMotion.Snapshot a,int count){
  if(_cancelled||_mimicProof==null||!_mimicProof.Matches(a)||!Owns(a)||
   a.OwnerVersion!=_registeredOwner||count!=_mimicProof.PresentationContacts||
   a.LastOrdinal!=_mimicAccepted||a.LastOrdinal<0||a.LastOrdinal>=count||
   a.AcceptedContacts!=_mimicAccepted+1||!float.IsFinite(a.LastContactAt)||
   a.LastContactAt<_started||(_mimicLastAt>=0&&a.LastContactAt<_mimicLastAt))return;
  _mimicAccepted++;_mimicLastAt=a.LastContactAt;
 }
 public void AcceptedContact(Style04EnemyMotion.Snapshot a,int count){if(_mimic){AcceptedMimicContact(a,count);return;}if(!_cancelled&&Owns(a)&&(_furnace?count==(_move is "devour_ember" or "inferno"?6:_move=="tackle"?2:1):_chimera3?count==(_move=="lacerate"?3:1):_winch?count==(_move=="flail"?2:1):_chimera1?count==1:_sealOrb?count==1:_warden?count==(_move=="bell_frenzy"?2:1):_breaker?count==1:_priest?count==(_move is "beam" or "blood_beam"?3:1):(_spawner||_cannon)?count==1:_kin?count==(_move=="boomerang"?2:1):_crusher?count==(_move=="bug_sting"?2:1):_globe?count==(_move=="thunder_strike"?3:1):_berserker?count==(_move=="pummel"?4:1):((_ironEgg||_sentry||_frogKnight||_eliteSlaver)?count==1:_gapingMaw?count==(_move=="claw"?2:1):_joker?count==1:_mawHunter?count==(_move=="claws"?3:1):_cogConstruct?count==(_move=="expel"?2:1):_bomber?count==1:_chomper?count==2:(_cinderImp||_scaldGnat)?count==1:_slug?count==1:_hazeStag?count==(_move=="hoof_flurry"?2:1):_axeAutomaton?count==(_move=="one_two"?2:1):_bitingScroll?count==(_move=="chew"?2:1):_doomChanter?count==2:(_giantFace||_whetstone||_ironClam||_mudLump||_sludgeSpider)?count==1:_serpent?count==1:_stabBook?count==(_move=="stab2"?2:_move=="stab3"?3:4):_lostSoul?count==(_move=="eye_lasers"?2:1):(_mudling||_husk||_axeOgre||_forgottenSoul)?count==1:_worm?count==(_move=="tremor"?2:1):_wraith?count==1:_reboot?count==(_move=="one_two"?2:1):(_beetle||_deathless||_shellGuard||_moss||_chick||_broodToad||_broodling)?count==1:_broodRaptor?count==1:_bowlBug?count==(_move=="tackle"?2:1):_mournBeast?count==(_move=="bite"?2:1):_sculptor?count==1:_bondWolf?count==(_move=="twin_bite"?2:1):_sergeant?count==(_move=="double_whip"?2:1):_tree?count==(_move=="swipe"?2:1):_turtle?count==(_move=="awake_bite"?2:1):(!_ownedReactions||count==1)))&&count>=1&&count<=12&&a.LastOrdinal>=0&&a.LastOrdinal<count&&a.AcceptedContacts==a.LastOrdinal+1&&(_count==0||_count==count))_count=count;}
 public void ResumeIdleForPlacement(float now=0,int owner=0){if(_ownedReactions){if(!float.IsFinite(now)||now<0||owner<0)return;RegisterReactionOwner(now,owner);}_cancelled=false;_reaction="";_reactionAt=-1;_reactionOwner=-1;}
 public bool OwnsSupportToken(Style04EnemySupportMotion.Token token,Style04EnemySupportMotion.Snapshot s)=>UsesStrictSupportOwnership&&!_cancelled&&SupportMove(s)&&OwnsSupport(s)&&token.IsValid&&token.InstanceId==s.InstanceId&&token.Serial==s.Version&&token.ExecutionIndex==s.ExecutionIndex&&token.OwnerVersion==s.OwnerVersion;
 public bool CancelSupport(Style04EnemySupportMotion.Token token,Style04EnemySupportMotion.Snapshot s){if(!OwnsSupportToken(token,s))return false;Cancel();return true;}
 public void Cancel(){ClearHeal();ClearMill();_mimicProof=null;_mimicAccepted=0;_mimicLastAt=-1;_restResolved=_resolved=_fledAt=_fledUntil=-1;if(_ownedReactions){_reactionEpoch++;_registeredOwner=-1;_epochStarted=-1;}_cancelled=true;_started=-1;_owner=_version=_execution=-1;_count=0;_move=_reaction="";_reactionAt=-1;_reactionOwner=-1;}
 public void NotifyPose(string name,float now,int owner){
  if(_ownedReactions){NotifyPose(name,now,owner,CaptureReactionToken());return;}
  if(name is not ("hurt" or "block")||!float.IsFinite(now)||now<0)return;_reaction=name;_reactionAt=now;_reactionOwner=owner;_cancelled=false;
 }
 // Token-bearing direct callbacks must carry the token captured when they were scheduled.
 // Stage's synchronous PlayPose path separately validates callbackGeneration before calling.
 public bool NotifyPose(string name,float now,int owner,ReactionToken token){
  if(!_ownedReactions||_cancelled||!token.IsValid||token.InstanceId!=_reactionInstance||token.Epoch!=_reactionEpoch||token.OwnerVersion!=_registeredOwner||owner!=_registeredOwner||name is not ("hurt" or "block")||!float.IsFinite(now)||now<_epochStarted||now<0)return false;
  _reaction=name;_reactionAt=now;_reactionOwner=owner;return true;
 }
 private float Duration(string clip,float fallback)=>_clips.TryGetValue(clip,out var a)?a.Sum():fallback;
 private BruteAuthoredFrames.Snapshot Pick(string requested,float age,bool loop,string reason,bool active,int owner,Style04EnemyMotion.Snapshot a){bool missing=!_clips.TryGetValue(requested,out var d);string clip=missing?"rest":requested;if(missing)d=_clips["rest"];float t=Math.Max(0,age),length=d!.Sum();if(loop)t%=length;int index=0;while(index<d.Length-1&&t>=d[index]){t-=d[index];index++;}return State=new(requested,clip,index,reason,missing,active,owner,a.Version,a.ExecutionIndex,a.AcceptedContacts,a.LastOrdinal,a.LastContactAt);}
 public BruteAuthoredFrames.Snapshot Sample(float now,Style04EnemyMotion.Snapshot a,Style04EnemySupportMotion.Snapshot support,int owner,bool dead){
  if(!float.IsFinite(now)||now<0)return Pick("rest",0,false,"invalid-clock",false,owner,a);
  if(HasFleeTerminal(owner)&&dead){float age=Math.Max(0,now-_fledAt);return Pick("flee",now<_fledUntil?age:Duration("flee",.24f),now<_fledUntil,now<_fledUntil?"accepted-real-fled":"fled-terminal",now<_fledUntil,owner,a);}
  if(dead)return Pick("rest",0,false,"dead",false,owner,a);
  if(_cancelled)return Pick("rest",0,false,"cancelled",false,owner,a);
  if(UsesStrictSupportOwnership&&owner!=_registeredOwner)return Pick("rest",0,false,"unowned-owner",false,owner,a);
  float reactionAge=now-_reactionAt;
  if(_reaction=="hurt"&&_reactionOwner==owner&&reactionAge>=0&&reactionAge<Duration("hurt",.18f))return Pick("hurt",reactionAge,false,"hurt-notification",true,owner,a);
  if(a.Active&&a.OwnerVersion==owner){
   if(!Owns(a))return Pick("rest",0,false,"unowned-attack",false,owner,a);
   if(_mimic&&(_mimicProof==null||!_mimicProof.Matches(a)||a.AcceptedContacts!=_mimicAccepted||a.LastOrdinal!=_mimicAccepted-1||(_mimicAccepted>0&&a.LastContactAt!=_mimicLastAt)))return Pick("rest",0,false,"unproved-mimic-contact",false,owner,a);
if(_globe&&(_move is "thunder_strike" or "galvanic_burst")){
 if(a.AcceptedContacts==0)
  return Pick("rest",0,false,"release-overlay",true,owner,a);
 if(!float.IsFinite(a.ReleaseAt)||a.ReleaseAt<_started)
  return Pick("rest",0,false,"missing-actual-release",true,owner,a);
}

   if(_priest&&(_move is "beam" or "blood_beam")){
    if(a.AcceptedContacts==0)return Pick("rest",0,false,"release-overlay",true,owner,a);
    if(!float.IsFinite(a.ReleaseAt)||a.ReleaseAt<_started)return Pick("rest",0,false,"missing-actual-release",true,owner,a);
   }
   if(_winch&&_move=="hyper_beam")return Pick("rest",0,false,"release-overlay",true,owner,a);
   if(_sealOrb&&_move=="beam")return Pick("rest",0,false,"release-overlay",true,owner,a);
   if(_cannon&&_move is "precision_beam" or "laser")return Pick("rest",0,false,"release-overlay",true,owner,a);
   if(_kin&&_move=="boomerang")return Pick("rest",0,false,"release-overlay",true,owner,a);
   if((_sentry&&_move is "bolt" or "beam")||(_bomber&&_move is "toss_ember" or "big_boom")||(_cinderImp&&_move=="spark_toss")||(_slug&&_move=="acid_spit")||(_ironClam&&_move=="jet")||(_mudLump&&_move is "slop" or "mud_toss")||(_sludgeSpider&&_move=="oil_spray")||(_lostSoul&&_move=="eye_lasers")||(_spore&&_move=="frail_spores")||(_hexer&&_move=="mud"))return Pick("rest",0,false,"release-overlay",true,owner,a);
   string prepare=_furnace?(_move=="sear"?"searprepare":_move=="tackle"?"tackleprepare":_move=="inferno"?"infernoprepare":"devourprepare"):_chimera3?(_move=="pounce"?"pounceprepare":"clawprepare"):_winch?"flailprepare":_chimera1&&_move=="skull_bash"?"bashprepare":_warden?(_move=="gate_guard"?"gateprepare":_move=="bell_hushed"?"hushprepare":_move=="bell_frenzy"?"frenzyprepare":"strikeprepare"):_breaker?(_move=="toss_junk"?"tossprepare":_move=="break_trap"?"trapprepare":"smashprepare"):_priest?(_move=="orb_weak"?"weakprepare":_move=="beam"?"beamprepare":_move=="judgment_orb"?"judgmentprepare":_move=="blood_beam"?"bloodprepare":"frailprepare"):_crusher?(_move=="swell_strike"?"swellprepare":_move=="bug_sting"?"stingprepare":_move=="guarded_strike"?"guardedprepare":"thrashprepare"):_globe?(_move=="thunder_strike"?"thunderprepare":_move=="galvanic_burst"?"burstprepare":"slapprepare"):_berserker?(_move=="hug"?"hugprepare":_move=="smother"?"smotherprepare":"pummelprepare"):_frogKnight?(_move=="strike_down"?"strikeprepare":_move=="beetle_charge"?"chargeprepare":"tongueprepare"):_eliteSlaver?(_move=="weight"?"weightprepare":_move=="lash"?"lashprepare":"chainprepare"):_gapingMaw&&_move=="rend"?"rendprepare":_joker?(_move=="cautious_jab"?"jabprepare":_move=="call_bluff"?"bluffprepare":"wildprepare"):_mawHunter&&_move=="claws"?"clawsprepare":_cogConstruct&&_move=="expel"?"expelprepare":_scaldGnat&&_move=="suck"?"suckprepare":_slug&&_move=="tackle"?"tackleprepare":_hazeStag?(_move=="hoof_flurry"?"flurryprepare":_move=="antler_charge"?"chargeprepare":"jabprepare"):_axeAutomaton&&_move=="hammer_upper"?"upperprepare":_bitingScroll&&_move=="chew"?"chewprepare":_sludgeSpider&&_move=="rage"?"rageprepare":_serpent?(_move=="lash"?"lashprepare":_move=="thwack"?"thwackprepare":"constrictprepare"):_forgottenSoul&&_move=="dread"?"dreadprepare":_worm&&_move=="bite"?"biteprepare":_wraith&&_move=="scythe"?"scytheprepare":_beetle&&_move=="bite"?"biteprepare":_deathless&&_move=="stance"?"stanceprepare":_shellGuard&&_move=="cracked_rage"?"rageprepare":_moss&&_move=="bite"?"biteprepare":_reboot&&_move=="hammer_upper"?"upperprepare":_bowlBug&&_move=="tackle"?"tackleprepare":_mournBeast&&_move=="leap"?"leapprepare":_sculptor&&_move=="savage"?"savageprepare":_bondWolf&&_move=="lunge"?"lungeprepare":_sergeant&&_move=="double_whip"?"whipprepare":_squire&&_move=="avenging_rush"?"rushprepare":_owl&&_move=="talon_dive"?"diveprepare":_goldRaven&&_move=="peck"?"peckprepare":_tree&&_move=="grasping_vines"?"graspingprepare":_tree&&_move=="chomp"?"chompprepare":_turtle&&_move=="crush"?"crushprepare":_probe&&_move=="lunge"?"lungeprepare":_prepare,impact=_furnace?(_move=="sear"?"searcontact":_move=="tackle"?(a.LastOrdinal==0?"tacklecontacta":"tacklecontactb"):_move=="inferno"?"infernocontact":"devourcontact"):_chimera3?(_move=="pounce"?"pouncecontact":a.LastOrdinal%2==0?"clawcontacta":"clawcontactb"):_winch?(a.LastOrdinal==0?"flailcontacta":"flailcontactb"):_chimera1&&_move=="skull_bash"?"bashcontact":_chimera2?(a.LastOrdinal%2==0?"clawcontacta":"clawcontactb"):_warden?(_move=="gate_guard"?"gatecontact":_move=="bell_hushed"?"hushcontact":_move=="bell_frenzy"?(a.LastOrdinal==1?"frenzycontactb":"frenzycontacta"):"strikecontact"):_breaker?(_move=="toss_junk"?"tosscontact":_move=="break_trap"?"trapcontact":"smashcontact"):_priest?(_move=="orb_weak"?"weakrelease":_move=="beam"?"beamrelease":_move=="judgment_orb"?"judgmentrelease":_move=="blood_beam"?"bloodrelease":"frailrelease"):_crusher?(_move=="swell_strike"?"swellcontact":_move=="bug_sting"?(a.LastOrdinal==1?"stingcontactb":"stingcontacta"):_move=="guarded_strike"?"guardedcontact":"thrashcontact"):_globe?(_move=="thunder_strike"?"thunderrelease":_move=="galvanic_burst"?"burstrelease":"slapcontact"):_berserker?(_move=="hug"?"hugcontact":_move=="smother"?"smothercontact":a.LastOrdinal%2==0?"contacta":"contactb"):_mimic?(a.LastOrdinal%2==0?"contacta":"contactb"):_frogKnight?(_move=="strike_down"?"strikecontact":_move=="beetle_charge"?"chargecontact":"tonguecontact"):_eliteSlaver?(_move=="weight"?"weightcontact":_move=="lash"?"lashcontact":"chaincontact"):_gapingMaw?(_move=="rend"?"rendcontact":a.LastOrdinal==1?"clawcontact2":"clawcontact1"):_joker?(_move=="cautious_jab"?"jabcontact":_move=="call_bluff"?"bluffcontact":"wildcontact"):_mawHunter&&_move=="claws"?(a.LastOrdinal%2==0?"clawscontact1":"clawscontact2"):_cogConstruct&&_move=="expel"?(a.LastOrdinal==1?"expelcontact2":"expelcontact1"):_chomper?(a.LastOrdinal==1?"clampcontact2":"clampcontact1"):_scaldGnat&&_move=="suck"?"suckcontact":_slug&&_move=="tackle"?"tacklecontact":_hazeStag?(_move=="antler_charge"?"chargecontact":_move=="hoof_flurry"?(a.LastOrdinal==1?"flurrycontact2":"flurrycontact1"):"jabcontact"):_axeAutomaton?(_move=="hammer_upper"?"uppercontact":a.LastOrdinal==1?"fistcontact":"axecontact"):_bitingScroll?(_move=="chomp"?"chompcontact":a.LastOrdinal==1?"chewcontact2":"chewcontact1"):_doomChanter?(a.LastOrdinal==1?"chantcontact2":"chantcontact1"):_sludgeSpider&&_move=="rage"?"ragecontact":_serpent?(_move=="lash"?"lashcontact":_move=="thwack"?"thwackcontact":"constrictcontact"):_stabBook?(a.LastOrdinal%2==0?"stabcontact1":"stabcontact2"):_forgottenSoul&&_move=="dread"?"dreadcontact":_worm?(_move=="bite"?"bitecontact":a.LastOrdinal==1?"tremorcontact2":"tremorcontact"):_wraith&&_move=="scythe"?"scythecontact":_beetle&&_move=="bite"?"bitecontact":_deathless&&_move=="stance"?"stancecontact":_shellGuard&&_move=="cracked_rage"?"ragecontact":_moss&&_move=="bite"?"bitecontact":_reboot?(_move=="hammer_upper"?"uppercontact":a.LastOrdinal==1?"fistcontact":"axecontact"):_bowlBug?(_move=="roll"?"rollcontact":a.LastOrdinal==1?"tacklecontact2":"tacklecontact1"):_mournBeast?(_move=="leap"?"leapcontact":a.LastOrdinal==1?"bitecontact2":"bitecontact"):_sculptor?(_move=="savage"?"savagecontact":"carvecontact"):_bondWolf?(_move=="lunge"?"lungecontact":a.LastOrdinal==1?"bitecontact2":"bitecontact"):_sergeant?(_move=="double_whip"&&a.LastOrdinal==1?"doublecontact2":_move=="double_whip"?"doublecontact1":"whipcontact"):_squire&&_move=="avenging_rush"?"rushcontact":_owl&&_move=="talon_dive"?"divecontact":_goldRaven&&_move=="peck"?"peckcontact":_tree&&_move=="grasping_vines"?"graspingcontact":_tree&&_move=="chomp"?"chompcontact":_turtle&&_move=="crush"?"crushimpact":_probe&&_move=="lunge"?"lungecontact":_impact;
   if(a.AcceptedContacts==0||a.LastContactAt<0)return Pick(prepare,Math.Max(0,now-_started),false,"awaiting-real-contact",true,owner,a);
   if(_count<1)return Pick("rest",0,false,"unregistered-contact-count",false,owner,a);
   float age=Math.Max(0,now-a.LastContactAt),hold=(!_turtle&&(_profile=="enemy_vine_walker"||_ownedReactions))?Math.Min(Duration(impact,.04f),.055f):Duration(impact,.04f);
   if(age<hold)return Pick(impact,age,false,"accepted-real-contact",true,owner,a);
   if(a.LastOrdinal<_count-1)return Pick(_furnace?(_move=="tackle"?"tacklerebound":_move=="inferno"?"infernorebound":"devourrebound"):_chimera3?"clawrebound":_winch?"flailrebound":_chimera2?"clawrebound":_warden?"frenzyrebound":_priest?(_move=="blood_beam"?"bloodrebound":"beamrebound"):_crusher?"stingrebound":_globe?"thunderrebound":_berserker?"rebound":_mimic?"rebound":_gapingMaw?"clawrebound":_mawHunter?"clawsrebound":_cogConstruct?"expelrebound":_chomper?"clamprebound":_hazeStag?"flurryrebound":_axeAutomaton?"tworebound":_bitingScroll?"chewrebound":_doomChanter?"chantrebound":_stabBook?"stabrebound":_worm?"tremorrebound":_reboot?"tworebound":_bowlBug?"tacklerebound":_mournBeast?"bitereopen":_bondWolf?"bitereopen":_sergeant?"rebound":_clips.ContainsKey("reopen")?"reopen":prepare,age-hold,false,"accepted-contact-reopen",true,owner,a);
   return Pick("recovery",age-hold,false,"final-contact-recovery",true,owner,a);
  }
// M63 existing bell* style metadata uses the ranged body clock. Keep the
// last real contact visible for a bounded art tail without changing that clock.
if(_warden&&(_move is "bell_strike" or "bell_hushed" or "bell_frenzy")&&
   !a.Active&&Owns(a)&&a.OwnerVersion==owner&&_count==(_move=="bell_frenzy"?2:1)&&
   a.AcceptedContacts==_count&&a.LastOrdinal==_count-1&&float.IsFinite(a.LastContactAt)&&
   a.LastContactAt>=_started){
 float age=now-a.LastContactAt;string key=_move=="bell_frenzy"?"frenzycontactb":_move=="bell_hushed"?"hushcontact":"strikecontact";
 float hold=Math.Min(Duration(key,.04f),.055f),ret=Math.Min(Duration("recovery",.2f),.2f);
 if(age>=0&&age<hold+ret)return Pick(age<hold?key:"recovery",Math.Max(0,age<hold?age:age-hold),false,age<hold?"accepted-final-contact-tail":"final-contact-visual-recovery",true,owner,a);
}
// M62 real final-contact tail; no timer, callback, damage or fence ownership.
if(((_priest&&(_move is "beam" or "blood_beam"))||(_breaker&&_move=="toss_junk"))&&
   !a.Active&&Owns(a)&&a.OwnerVersion==owner&&_count==(_priest?3:1)&&
   a.AcceptedContacts==_count&&a.LastOrdinal==_count-1&&float.IsFinite(a.LastContactAt)&&
   a.LastContactAt>=_started&&float.IsFinite(a.ReleaseAt)&&a.ReleaseAt>=_started){
 float age=now-a.LastContactAt;string key=_breaker?"tosscontact":_move=="blood_beam"?"bloodrelease":"beamrelease";
 float hold=Math.Min(Duration(key,.04f),.055f),ret=Math.Min(Duration("recovery",.2f),.2f);
 if(age>=0&&age<hold+ret)return Pick(age<hold?key:"recovery",Math.Max(0,age<hold?age:age-hold),false,age<hold?"accepted-final-contact-tail":"final-contact-visual-recovery",true,owner,a);
}
if(_globe&&!a.Active&&Owns(a)&&a.OwnerVersion==owner&&
   (_move is "thunder_strike" or "galvanic_burst")&&
   _count==(_move=="thunder_strike"?3:1)&&a.AcceptedContacts==_count&&
   a.LastOrdinal==_count-1&&float.IsFinite(a.LastContactAt)&&
   a.LastContactAt>=_started&&float.IsFinite(a.ReleaseAt)&&a.ReleaseAt>=_started){
 float age=now-a.LastContactAt;
 string finalKey=_move=="thunder_strike"?"thunderrelease":"burstrelease";
 float hold=Math.Min(Duration(finalKey,.04f),.055f);
 float ret=Math.Min(Duration("recovery",.2f),.2f);
 if(age>=0&&age<hold+ret)
  return Pick(age<hold?finalKey:"recovery",Math.Max(0,age<hold?age:age-hold),
   false,age<hold?"accepted-final-contact-tail":"final-contact-visual-recovery",
   true,owner,a);
}
  if(_reaction=="block"&&_reactionOwner==owner&&reactionAge>=0&&reactionAge<(_ownedReactions?Math.Min(Duration("block",.18f),.18f):Duration("block",.18f)))return Pick((_chimera3||_chimera1||_cannon||_mimic||_devourer)?"rest":"block",_hasGuard?Duration("block",.18f):reactionAge,false,"block-notification",true,owner,a);
  // This finite rest drawing owns no gameplay clock, pulse or fence. Its deadline is
  // measured from the original support start. Only the same real Resolved receipt
  // permits a visual tail after support End; every new owner/cancel clears it.
  if((_winch||_cannon)&&support.Kind=="rest"&&support.MoveId==(_winch?"rewind":"recharge")&&OwnsSupport(support)){
   float age=now-support.StartedAt,hold=Math.Min(Duration(_winch?"rewind":"recharge",.5f),.5f),ret=Math.Min(Duration("recovery",.18f),.2f);
   bool ownedWindow=now>=support.StartedAt&&now<support.Deadline&&((support.Active&&support.EndRequestedAt<0)||_restResolved>=0);
   if(ownedWindow&&age<hold+ret)return Pick(age<hold?(_winch?"rewind":"recharge"):"recovery",Math.Max(0,age<hold?age:age-hold),false,age<hold?"owned-rest-recharge":"owned-rest-return",true,owner,a);
   return Pick("idle",now,true,"idle",false,owner,a);
  }
  if(_axeOgre&&support.Kind=="rest"&&support.MoveId=="gasp"&&OwnsSupport(support)){
   float age=now-support.StartedAt,prepare=Duration("gaspprepare",.12f),gasp=Duration("gasp",.6f),ret=Duration("return",.24f);
   bool ownedWindow=now>=support.StartedAt&&now<support.Deadline&&((support.Active&&support.EndRequestedAt<0)||_restResolved>=0);
   if(ownedWindow&&age<prepare)return Pick("gaspprepare",age,false,"owned-rest-prepare",true,owner,a);
   if(ownedWindow&&age<prepare+gasp)return Pick("gasp",age-prepare,false,"owned-rest-gasp",true,owner,a);
   if(ownedWindow&&age<prepare+gasp+ret)return Pick("return",age-prepare-gasp,false,"owned-rest-return",true,owner,a);
   return Pick("idle",now,true,"idle",false,owner,a);
  }
  if(support.Active&&support.OwnerVersion==owner&&(!_ownedReactions||(SupportMove(support)&&OwnsSupport(support)))){
   if(_thief&&support.Kind=="steal-gold"){
    if(_resolved<0)return Pick(support.EndRequestedAt>=0||support.Phase=="deadline-recovery"?"recovery":"sneak",0,false,"awaiting-real-steal-resolved",true,owner,a);
    float age=Math.Max(0,now-_resolved);return Pick(age<.04f?"reach":"stow",age<.04f?age:age-.04f,false,age<.04f?"accepted-real-steal-resolved":"steal-stow",true,owner,a);
   }
   if(_thief&&support.Kind=="flee")return Pick("turn",Math.Max(0,now-support.StartedAt),false,"awaiting-real-fled",true,owner,a);
   // Egg is support-only. This draws preparation on the old actor, never a child or a hatch result.
   // EngineHatched replaces the actor; Stage's existing instance/ID/owner fence rejects stale callbacks.
   if(_raptorEgg&&support.Kind=="hatch"){
    if(now<support.StartedAt||now>=support.Deadline||now>=support.EndAt)return Pick("idle",now,true,"idle",false,owner,a);
    bool returning=support.EndRequestedAt>=0||support.Phase=="deadline-recovery";
    return Pick(returning?"recovery":"hatchprepare",Math.Max(0,now-(returning?(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f):support.StartedAt)),false,returning?"unhatched-support-recovery":"owned-hatch-preparation",true,owner,a);
   }
if(_healer&&support.Kind=="heal"){
 if(now<support.StartedAt||now>=support.Deadline||now>=support.EndAt)
  return Pick("idle",now,true,"idle",false,owner,a);
 if(_healAppliedAt>=0){
  float age=now-_healAppliedAt,hold=Math.Min(Duration("mendapplied",.12f),.12f);
  return Pick(age<hold?"mendapplied":"recovery",Math.Max(0,age<hold?age:age-hold),
    false,age<hold?"accepted-real-positive-heal-once":"heal-applied-recovery",true,owner,a);
 }
 if(support.EndRequestedAt>=0||support.Phase=="deadline-recovery")
  return Pick("recovery",Math.Max(0,now-(support.EndRequestedAt>=0?
    support.EndRequestedAt:support.Deadline-.18f)),false,
    _healOutcomeAt>=0?"zero-heal-recovery":"unapplied-heal-recovery",true,owner,a);
 return Pick("mendprepare",Math.Max(0,now-support.StartedAt),false,
   _healOutcomeAt>=0?"observed-zero-heal":"awaiting-real-heal",true,owner,a);
}

   if(_devourer){
    if(now<support.StartedAt||now>=support.Deadline||now>=support.EndAt)return Pick("idle",now,true,"idle",false,owner,a);
    if(_millAt>=0){float age=now-_millAt,hold=Duration("devourapplied",.12f);return Pick(age<hold?"devourapplied":"recovery",Math.Max(0,age<hold?age:age-hold),false,age<hold?"accepted-real-mill-once":"mill-applied-recovery",true,owner,a);}
    if(support.EndRequestedAt>=0||support.Phase=="deadline-recovery")return Pick("recovery",Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"unapplied-mill-recovery",true,owner,a);
    return Pick("devourprepare",Math.Max(0,now-support.StartedAt),false,"awaiting-real-mill",true,owner,a);
   }
   if(_ownedReactions){
    if((_chimera3||_berserker||_djinn||_eliteSlaver||_gapingMaw||_mawHunter||_chomper||_cinderImp||_scaldGnat||_mudling||_spore||_hexer)&&support.Kind=="hex")return Pick("rest",0,false,"hex-overlay",true,owner,a);
    if(_drummer&&support.Kind=="rally")return Pick("rest",0,false,"rally-overlay",true,owner,a);
    if(UsesStrictSupportOwnership&&(now<support.StartedAt||now>=support.Deadline||now>=support.EndAt))return Pick("idle",now,true,"idle",false,owner,a);
    bool pulse= support.AcceptedPulses>0&&support.LastPulseAt>=0&&(!UsesStrictSupportOwnership||(support.LastPulseAt>=support.StartedAt&&now>=support.LastPulseAt));
    bool pulseHold=pulse&&now-support.LastPulseAt>=0&&now-support.LastPulseAt<.055f;
    if(_ironEgg||_sentry){
     bool buff=_ironEgg&&support.Kind=="buff";
     string prepare=_sentry?"guardprepare":buff?"awakenprepare":"sleepprepare";
     string applied=_sentry?"guardhold":buff?"awakenhold":"sleephold";
     string ret=_sentry?"recovery":buff?"awakenreturn":"sleepreturn";
     if((support.Phase=="recovery"||support.Phase=="deadline-recovery")&&!pulseHold)return Pick(ret,Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"support-recovery",true,owner,a);
     return Pick(pulse?applied:prepare,Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?(buff?"accepted-real-own-strength":"accepted-real-block-gained"):(buff?"awaiting-real-own-strength":"awaiting-real-block-gained"),true,owner,a);
    }
    if(_furnace){
     bool inflame=support.MoveId=="inflame";string prepare=inflame?"inflameprepare":"stokeprepare",applied=inflame?"inflamehold":"stokehold";
     if((support.Phase=="recovery"||support.Phase=="deadline-recovery")&&!pulseHold)return Pick("recovery",Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"support-recovery",true,owner,a);
     return Pick(pulse?applied:prepare,Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    }
    if(_winch){
     bool summon=support.Kind=="summon";string prepare=summon?"summonprepare":"windprepare",applied=summon?"summonapplied":"windhold";
     if((support.Phase=="recovery"||support.Phase=="deadline-recovery")&&!pulseHold)return Pick("recovery",Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"support-recovery",true,owner,a);
     return Pick(pulse?applied:prepare,Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?(summon?"accepted-real-own-summon":"accepted-real-block-gained"):(summon?"awaiting-real-own-summon":"awaiting-real-block-gained"),true,owner,a);
    }
    if(_sealOrb||_warden){
     bool seal=_sealOrb&&support.Kind=="seal";
     string prepare=seal?"sealprepare":"guardprepare",applied=seal?"sealapplied":"guardhold";
     if((support.Phase=="recovery"||support.Phase=="deadline-recovery")&&!pulseHold)return Pick("recovery",Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"support-recovery",true,owner,a);
     return Pick(pulse?applied:prepare,Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?(seal?"accepted-real-own-card-sealed":"accepted-real-block-gained"):(seal?"awaiting-real-own-card-sealed":"awaiting-real-block-gained"),true,owner,a);
    }
    if(_breaker||_priest){
     bool token=_breaker&&support.Kind=="destroy-token",ritual=_priest&&support.Kind=="buff";
     string prepare=token?"tokenprepare":ritual?"ritualprepare":"guardprepare",applied=token?"tokenapplied":ritual?"ritualapplied":"guardhold";
     if((support.Phase=="recovery"||support.Phase=="deadline-recovery")&&!pulseHold)return Pick("recovery",Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"support-recovery",true,owner,a);
     return Pick(pulse?applied:prepare,Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?(token?"accepted-real-own-token-destroyed":ritual?"accepted-real-own-strength":"accepted-real-block-gained"):(token?"awaiting-real-own-token-destroyed":ritual?"awaiting-real-own-strength":"awaiting-real-block-gained"),true,owner,a);
    }
    if(_spawner||_cannon){
     bool spawn=_spawner&&support.Kind=="summon",buff=_cannon&&support.Kind=="buff";
     string prepare=spawn?"spawnprepare":buff?"chargeprepare":"guardprepare";
     string applied=spawn?"spawnapplied":buff?"chargeapplied":"guardhold";
     if((support.Phase=="recovery"||support.Phase=="deadline-recovery")&&!pulseHold)return Pick("recovery",Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"support-recovery",true,owner,a);
     return Pick(pulse?applied:prepare,Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?(spawn?"accepted-real-own-summon":buff?"accepted-real-own-strength":"accepted-real-block-gained"):(spawn?"awaiting-real-own-summon":buff?"awaiting-real-own-strength":"awaiting-real-block-gained"),true,owner,a);
    }
    if(_kin||_crusher){
     bool buff=support.Kind=="buff";
     string prepare=buff?(_kin?"danceprepare":"adaptprepare"):"guardprepare";
     string applied=buff?(_kin?"dancehold":"adapthold"):"guardhold";
     if((support.Phase=="recovery"||support.Phase=="deadline-recovery")&&!pulseHold)return Pick("recovery",Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"support-recovery",true,owner,a);
     return Pick(pulse?applied:prepare,Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?(buff?"accepted-real-own-strength":"accepted-real-block-gained"):(buff?"awaiting-real-own-strength":"awaiting-real-block-gained"),true,owner,a);
    }
    if(_healer||_globe){
     if((support.Phase=="recovery"||support.Phase=="deadline-recovery")&&!pulseHold)return Pick("recovery",Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"support-recovery",true,owner,a);
     return Pick(pulse?"guardhold":"guardprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    }
    if(_raptorEgg||_berserker){
     if((support.Phase=="recovery"||support.Phase=="deadline-recovery")&&!pulseHold)return Pick("recovery",Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"support-recovery",true,owner,a);
     return Pick(pulse?(_raptorEgg?"quiverhold":"guardhold"):(_raptorEgg?"quiverprepare":"guardprepare"),Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    }
    if(_djinn){
     if((support.Phase=="recovery"||support.Phase=="deadline-recovery")&&!pulseHold)return Pick("recovery",Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"support-recovery",true,owner,a);
     return Pick(pulse?"guardhold":"guardprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    }
    if(_frogKnight||_eliteSlaver||_gapingMaw||_joker||_mawHunter||_cogConstruct||_bomber||_chomper||_cinderImp||_scaldGnat||_slug||_hazeStag||_axeAutomaton||_bitingScroll||_giantFace||_doomChanter||_whetstone||_ironClam){
     bool buff=support.Kind=="buff";
     string prepare=buff?(_frogKnight?"queenprepare":_cogConstruct?"chargeprepare":_axeAutomaton?"sharpenprepare":_bitingScroll?"teethprepare":_giantFace?"glareprepare":_doomChanter?"crescendoprepare":_whetstone?"honeprepare":"pressureprepare"):_hazeStag?"fadeprepare":"guardprepare";
     string applied=buff?(_frogKnight?"queenhold":_cogConstruct?"chargepulse":_axeAutomaton?"sharpenpulse":_bitingScroll?"teethpulse":_giantFace?"glarepulse":_doomChanter?"crescendopulse":_whetstone?"honepulse":"pressurepulse"):_hazeStag?"fadehold":"guardhold";
     string ret=buff?(_frogKnight?"queenreturn":_cogConstruct?"chargereturn":_axeAutomaton?"sharpenreturn":_bitingScroll?"teethreturn":_giantFace?"glarereturn":_doomChanter?"crescendoreturn":_whetstone?"honereturn":"pressurereturn"):_hazeStag?"fadereturn":"guardreturn";
     if((support.Phase=="recovery"||support.Phase=="deadline-recovery")&&!pulseHold)return Pick(ret,Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"support-recovery",true,owner,a);
     return Pick(pulse?applied:prepare,Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?(buff?"accepted-real-own-strength":"accepted-real-block-gained"):(buff?"awaiting-real-own-strength":"awaiting-real-block-gained"),true,owner,a);
    }
    if((support.Phase=="recovery"||support.Phase=="deadline-recovery")&&!pulseHold)return Pick(_axeOgre?"return":(_mudLump||_sludgeSpider||_serpent||_stabBook||_mudling||_lostSoul||_thirdHusk||_forgottenSoul||_worm||_beetle||_deathless||_shellGuard||_moss)?"guardreturn":_broodToad?"squatprepare":_cultist?"guardprepare":_turtle?"shellrecovery":"recovery",Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"support-recovery",true,owner,a);
    if(_axeOgre)return Pick(pulse?"winduphold":"windupprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_mudLump||_sludgeSpider||_serpent||_stabBook||_mudling||_lostSoul||_thirdHusk||_forgottenSoul||_worm||_beetle||_deathless||_shellGuard||_moss)return Pick(pulse?"guardhold":"guardprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_reboot)return Pick(pulse?"boothold":"bootprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_broodToad)return Pick(pulse?"squathold":"squatprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_broodRaptor)return Pick(pulse?"shelterhold":"shelterprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_bowlBug)return Pick(pulse?"curlhold":"curlprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_mournBeast)return Pick(pulse?"lickhold":"lickprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_sculptor)return Pick(pulse?"guardhold":"guardprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_bondWolf)return Pick(pulse?"prowlhold":"prowlprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_sergeant)return Pick(pulse?"guardhold":"guardprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_bigSlime)return Pick(pulse?"guardhold":"guardprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_squire)return Pick(pulse?"bracehold":"braceprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_wideBeast)return Pick(pulse?"coilhold":"coilprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_owl)return Pick(pulse?"bracehold":"braceprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_goldRaven)return Pick(pulse?"soarhold":"soarprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_mossSlime)return Pick(pulse?"oozehold":"oozeprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_colossus)return Pick(pulse?"bracehold":"braceprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_turtle)return Pick(pulse?"shellhold":"shellprepare",Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    if(_cultist||_probe||_tree||_drummer||_spore||_hexer)return Pick(pulse?"guardhold":"guardprepare",pulse?Math.Max(0,now-support.LastPulseAt):Math.Max(0,now-support.StartedAt),false,pulse?"accepted-real-block-gained":"awaiting-real-block-gained",true,owner,a);
    return Pick("block",pulse?Duration("block",.18f):0,false,"support-action",true,owner,a);
   }
   return Pick(support.Kind=="defend"?"block":"support",Math.Max(0,now-support.StartedAt),false,"support-action",true,owner,a);
  }
  return Pick("idle",now,true,"idle",false,owner,a);
 }
}
