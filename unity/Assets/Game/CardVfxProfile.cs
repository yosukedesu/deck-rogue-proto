using System.Collections.Generic;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace DeckRogue.Game {
 // Explicit presentation data. No gameplay properties and no runtime card-name matching.
 public static class CardVfxProfile {
  // Five explicit comparison samples. Baseline/unknown cards keep their existing timing and art.
  public sealed class Showcase {
   public readonly float Contact, Windup, Interval, Recovery, Residue, Reach;
   public readonly bool Heavy;
   public readonly string AuthoredAttack;
   public readonly float ImpactPause, ChainPause, FinalPause;
   public Showcase(float contact,float windup,float interval,float recovery,float residue,float reach,float impactPause,float chainPause,float finalPause,bool heavy=false,string authoredAttack=null) {
    AuthoredAttack=authoredAttack;Contact=contact;Windup=windup;Interval=interval;Recovery=recovery;Residue=residue;Reach=reach;Heavy=heavy;
    ImpactPause=impactPause;ChainPause=chainPause;FinalPause=finalPause;
   }
  }
  // Seconds of local character hitstop; contacts, HP, lights, particles and input keep their clock.
  public static float ImpactPauseFor(Showcase profile,int index,int total,bool finishing=false) {
   if(profile==null)return 0f;
   return finishing ? .14f : total>1 ? (index<total-1 ? profile.ChainPause : profile.FinalPause) : profile.ImpactPause;
  }
  static readonly Showcase Slash=new Showcase(.13f,.045f,.12f,.14f,.36f,.48f,.065f,.025f,.095f,false,"slash");
  static readonly Showcase Vine=new Showcase(.16f,.065f,.12f,.16f,.44f,.42f,.065f,.025f,.095f);
  static readonly Showcase Water=new Showcase(.24f,.14f,.12f,.18f,.48f,.10f,.075f,.025f,.100f,false,"water");
  static readonly Showcase Earth=new Showcase(.30f,.19f,.15f,.22f,.58f,.10f,.110f,.025f,.110f);
  static readonly Showcase Stomp=new Showcase(.34f,.22f,.16f,.24f,.65f,.62f,.120f,.030f,.120f,true,"stomp");
  public static bool ShowcaseOn => HD2DFlags.CombatPresentationShowcase && Stage.CombatWorld;
  public static Showcase Comparison(CardDef def) => Comparison(def?.Id);
  public static Showcase Comparison(string id) {
   if(!ShowcaseOn)return null;
   var hd2d=Hd2dAttackFx.Showcase(id);if(hd2d!=null)return hd2d;   // HD-2D の攻撃演出 (2026-10-27): dot の表 (PhysicalVfxProfiles/ElementalVfxProfiles) の時刻が踏み込みと当たりを決める
   switch(id) {
    case "green_strike":return Slash;
    case "green_double_lash":return Vine;
    case "blue_strike":return Water;
    case "green_earth_roar":return Earth;
    case "green_finisher_stomp":return Stomp;
    default:return null;
   }
  }
  static JObject _entries;
  static JObject Entries {get {if(_entries==null){var a=Resources.Load<TextAsset>("Data/card-vfx");_entries=a!=null?JObject.Parse(a.text):new JObject();}return _entries;}}
  public static CardDef ResolveDef(string id) {
   var state=GameRoot.I?.Rs?.Combat;
   if(state!=null)foreach(var pile in new[]{state.Player.Hand,state.Player.DiscardPile,state.Player.ExhaustPile,state.Player.DrawPile,state.Player.SetCards,state.Player.Permanents})
    if(pile!=null)foreach(var card in pile)if(card.Def.Id==id)return card.Def;
   try{return Content.GetCardDef(id);}catch(System.Exception){}
   return Fusion.ResolveFusedDef(id);
  }
  public static bool Contains(string id)=>id!=null && Entries[id]!=null;
  public static string Style(CardDef def)=> (string)Entries[def?.Id??""]?["style"] ?? (def?.Type=="spell"?"spell":"slash");
  public static string Element(CardDef def,string leader=null) {
   if(def?.Id=="blue_aether_burst" && leader=="leader_izzet")return "lightning";
   return (string)Entries[def?.Id??""]?["element"] ?? (def?.Type=="spell"?"arcane":"physical");
  }
  // Presentation-only routing data shares the existing resource/GUID. Reserved sections
  // are never gameplay definitions. Unknown provenance is reported, never name-guessed.
  public sealed class Route {
   public string SourceKey,Style,Element,FallbackReason,AuthoredAttack,SourceKind,SourceUid,AuthoredPresentation,AuthoredFallbackReason,AuthoredSemanticReason;
   public bool ExactSource; public CardDef Def;
   public Route(string key,string style,string element,bool exact,string fallback=null,string attack=null,CardDef def=null) {
    SourceKey=key;Style=style;Element=element;ExactSource=exact;FallbackReason=fallback;AuthoredAttack=attack;Def=def;
   }
  }
  static Route ReadRoute(string key,JToken entry,bool exact,CardDef def=null,string fallback=null) {
   string style=(string)entry?["style"]??(def?.Type=="spell"?"spell":"slash");
   string element=(string)entry?["element"]??(def?.Type=="spell"?"arcane":"physical");
   return new Route(key,style,element,exact,fallback??(string)entry?["fallbackReason"],(string)entry?["authoredAttack"],def) {AuthoredPresentation=(string)entry?["authoredPresentation"],AuthoredFallbackReason=(string)entry?["authoredFallbackReason"],AuthoredSemanticReason=(string)entry?["authoredSemanticReason"]};
  }
  public static Route Unknown(string reason,string key="unknown")=>new Route(key,"slash","physical",false,reason);
  public static Route Enemy(string enemyId,string moveId,bool executedMoveMetadataValidated=false) {
   string key=(enemyId??"unknown")+"/"+(moveId??"unknown");
   var entry=Entries["_enemyMoves"]?[key];
   var required=entry?["requiresExecutedMoveMetadata"];
   string reason=required!=null && required.Type!=JTokenType.Boolean?"invalid_enemy_execution_policy":
    required!=null && (bool)required && !executedMoveMetadataValidated?"requires_executed_move_metadata":null;
   if(reason!=null) {
    // The key is only the rejected snapshot candidate, not a proven execution.
    var unknown=Unknown(reason,"enemy:"+key);unknown.AuthoredFallbackReason=reason;return unknown;
   }
   return entry!=null?ReadRoute("enemy:"+key,entry,true):Unknown("unmapped_enemy_move","enemy:"+key);
  }
  public static Route EnemyFromSnapshot(GameState state,int index) {
   if(state==null || index<0 || index>=state.Enemies.Count)return Unknown("missing_enemy_snapshot");
   var enemy=state.Enemies[index];
   // IntentMoveId retains the primary ID even if a conditional Alt branch executes.
   // This snapshot-only path lacks validated execution metadata; never label it exact.
   if(enemy.Intent?.ConditionalOn!=null && enemy.Intent.Alt!=null)
    return Unknown("ambiguous_enemy_conditional_branch","enemy:"+enemy.EnemyId+"/conditional");
   return Enemy(enemy.EnemyId,enemy.IntentMoveId);
  }
  public static Route Source(string id,CardDef def=null,string leader=null) {
   var entry=Entries["_sources"]?[id??""]??Entries[id??""];
   var route=ReadRoute(id??"unknown",entry,id!=null && (entry!=null||def!=null),def,entry==null?"unmapped_source":null);
   if(id=="blue_aether_burst" && leader=="leader_izzet")route.Element="lightning";
   route.AuthoredAttack=(string)entry?["authoredAttackByLeader"]?[leader??""]??route.AuthoredAttack;
   return route;
  }
  public static CardDef PermanentDef(string uid,GameState before,GameState after) {
   if(uid==null)return null;
   foreach(var state in new[]{before,after})if(state?.Player?.Permanents!=null)
    foreach(var permanent in state.Player.Permanents)if(permanent.Uid==uid)return permanent.Def;
   return null;
  }
  public static string PermanentSourceUid(GameEvent_DamageDealt hit)=>hit.PresentationSource!=null
   ? (hit.PresentationSource.Kind=="permanent"?hit.PresentationSource.Uid:null) : hit.SourceUid;
  public static bool IsCardDamage(GameEvent_DamageDealt hit,string definitionId,bool reaction=false) {
   if(hit.Source!="player")return false;
   var source=hit.PresentationSource;
   return source==null?hit.SourceUid==null:source.Kind==(reaction?"reaction":"card") && source.DefinitionId==definitionId;
  }
  public static Route FromPresentationSource(DamagePresentationSource source,GameState before,GameState after,string leader=null) {
   if(source==null)return null;
   bool validKind=source.Kind=="card"||source.Kind=="reaction"||source.Kind=="necro"||source.Kind=="permanent"||source.Kind=="gear";
   if(!validKind || string.IsNullOrEmpty(source.DefinitionId))return Unknown("invalid_presentation_source");
   var def=PermanentDef(source.Uid,before,after);
   if(def?.Id!=source.DefinitionId)def=ResolveDef(source.DefinitionId);
   var route=Source(source.DefinitionId,def,leader);
   // Prefer the explicit owner, but keep unresolved future definitions as fallback.
   route.SourceKind=source.Kind;route.SourceUid=source.Uid;
   // A selected metadata-only entry overlays this new route, never the shared root.
   // UIDs are carried verbatim; kind + definition are sufficient and are not lineage proof.
   if(ApplyPresentationOverride(source,route))return route;
   // Exact source identity alone is not a shape opt-in. Non-card contexts need a
   // reviewed per-definition allowlist; invalid lists reject the entire entry.
   if(!AuthoredSourceKindAllowed(source.DefinitionId,source.Kind,out var kindReason)) {
    if(route.AuthoredAttack!=null || PresentationEntry(source.DefinitionId)?["authoredSourceKinds"]!=null)
     route.AuthoredFallbackReason=kindReason;
    route.AuthoredAttack=null;route.AuthoredPresentation=null;
   }
   return route;
  }
  static JToken SourceOverrideEntry(string id,string kind) {
   var table=Entries["_presentationSourceRoutes"] as JObject;
   var definition=table?[id??""] as JObject;
   return definition?[kind??""];
  }
  public static bool HasCardPresentationOverride(string id)=>SourceOverrideEntry(id,"card")!=null;
  static bool OptionalString(JObject entry,string key,out string value) {
   var token=entry[key];value=null;if(token==null)return true;
   if(token.Type!=JTokenType.String)return false;value=(string)token;return !string.IsNullOrEmpty(value);
  }
  static bool KnownDisplayElement(string value)=>value=="physical"||value=="arcane"||value=="neutral"||value=="fire"||value=="ice"||value=="water"||value=="lightning"||value=="dark"||value=="light"||value=="earth"||value=="vine";
  static bool ApplyPresentationOverride(DamagePresentationSource source,Route route) {
   var selected=SourceOverrideEntry(source.DefinitionId,source.Kind);if(selected==null)return false;
   var entry=selected as JObject;string attack=null,element=null,presentation=null,semantic=null;
   bool valid=route.ExactSource && entry!=null
    && OptionalString(entry,"authoredAttack",out attack) && attack!=null
    && OptionalString(entry,"element",out element) && KnownDisplayElement(element)
    && OptionalString(entry,"authoredPresentation",out presentation)
    && (source.Kind=="card"?(presentation==null || presentation=="small" || DamageVfxIntensity.IsFixedSmallContact(presentation)):DamageVfxIntensity.IsFixedSmallContact(presentation))
    && OptionalString(entry,"authoredSemanticReason",out semantic);
   if(valid)foreach(var property in entry.Properties())if(property.Name!="authoredAttack"&&property.Name!="element"&&property.Name!="authoredPresentation"&&property.Name!="authoredSemanticReason"){valid=false;break;}
   if(!valid){route.AuthoredAttack=null;route.AuthoredPresentation=null;route.AuthoredSemanticReason=null;route.AuthoredFallbackReason="invalid_presentation_source_override";return true;}
   route.AuthoredAttack=attack;route.Element=element;route.AuthoredPresentation=presentation;route.AuthoredSemanticReason=semantic;route.AuthoredFallbackReason=null;return true;
  }
  static JToken PresentationEntry(string id)=>Entries["_sources"]?[id??""]??Entries[id??""];
  static bool KnownPresentationKind(string kind)=>kind=="card"||kind=="reaction"||kind=="necro"||kind=="permanent"||kind=="gear";
  static bool AuthoredSourceKindAllowed(string id,string kind,out string reason) {
   reason="authored_source_kind_not_opted_in";
   var kinds=PresentationEntry(id)?["authoredSourceKinds"];
   if(kinds==null)return kind=="card";
   var list=kinds as JArray;
   if(list==null){reason="authored_source_kinds_invalid";return false;}
   bool allowed=false;
   foreach(var token in list) {
    if(token.Type!=JTokenType.String || !KnownPresentationKind((string)token)){reason="authored_source_kinds_invalid";return false;}
    if((string)token==kind)allowed=true;
   }
   return allowed;
  }
  public static bool RequiresSourceMetadata(string id) {
   var entry=PresentationEntry(id);var flag=entry?["authoredRequiresSourceMetadata"];
   // A kind allowlist cannot be bypassed by an omitted/false separate flag.
   // Malformed flag values fail closed for metadata-free historical payloads.
   return HasCardPresentationOverride(id) || entry?["authoredSourceKinds"]!=null || (flag!=null && (flag.Type!=JTokenType.Boolean || (bool)flag));
  }
  public static bool RequiresCardMetadata(string id)=>HasCardPresentationOverride(id) || (bool?)Entries[id??""]?["authoredRequiresCardMetadata"]==true;
  static Route RestrictLegacyAuthored(Route route) {
   if(route==null)return route;
   if(route.AuthoredAttack==null) {
    // A new scoped-only profile never authorizes an old marker-only material.
    // Preserve established root authored art, but diagnose missing metadata for
    // the new table so the receipt owner can use its neutral non-weapon fallback.
    if((Entries["_presentationSourceRoutes"] as JObject)?[route.SourceKey??""] is JObject)
     return new Route(route.SourceKey,route.Style,route.Element,route.ExactSource,route.FallbackReason,null,route.Def) {
      SourceKind=route.SourceKind,SourceUid=route.SourceUid,AuthoredFallbackReason="authored_requires_source_metadata"
     };
    return route;
   }
   var entry=PresentationEntry(route.SourceKey);var flag=entry?["authoredRequiresSourceMetadata"];
   bool requiresSource=entry?["authoredSourceKinds"]!=null || (flag!=null && (flag.Type!=JTokenType.Boolean || (bool)flag));
   if(!requiresSource && (bool?)Entries[route.SourceKey??""]?["authoredRequiresCardMetadata"]!=true)return route;
   // Keep the proved source/element, but a marker-only payload cannot authorize a
   // newly opted-in full card effect. Do not mutate earlier routes sharing current.
   return new Route(route.SourceKey,route.Style,route.Element,route.ExactSource,route.FallbackReason,null,route.Def) {
    SourceKind=route.SourceKind,SourceUid=route.SourceUid,AuthoredFallbackReason=requiresSource?"authored_requires_source_metadata":"authored_requires_card_metadata"
   };
  }
  // This does not change the engine state, event objects, order, hit amounts or RNG.
  // Explicit per-hit metadata wins. For older payloads, NecroFired has no closing
  // marker: the nested and resumed outer hit use auditable neutral fallback for
  // the rest of this slice. A new source start is not a nested end marker.
  public static Dictionary<int,Route> PlanSources(IReadOnlyList<GameEvent> log,int from,GameState before,GameState after,string leader=null) {
   var result=new Dictionary<int,Route>();Route current=Unknown("no_source_marker");bool ambiguousScope=false;
   for(int i=System.Math.Max(0,from);i<log.Count;i++) {
    var ev=log[i];
    if(ev is GameEvent_CardPlayed cp){if(!ambiguousScope)current=Source(cp.CardId,ResolveDef(cp.CardId),leader);}
    else if(ev is GameEvent_ReactionTriggered rt){if(!ambiguousScope)current=Source(rt.CardId,ResolveDef(rt.CardId),leader);}
    else if(ev is GameEvent_GearUsed gear){if(!ambiguousScope)current=Source(gear.GearId);}
    else if(ev is GameEvent_NecroFired necro){ambiguousScope=true;current=Unknown("ambiguous_necro_scope","necro:"+necro.CardId);}
    else if(ev is GameEvent_CardPlayedFromExhaust nested){ambiguousScope=true;current=Unknown("ambiguous_nested_card_scope","nested:"+nested.CardId);}
    else if(ev is GameEvent_TurnEnded || ev is GameEvent_TurnStarted || ev is GameEvent_EnemyActionExecuting || ev is GameEvent_EnemyPhaseEnded){ambiguousScope=false;current=Unknown("no_source_marker");}
    else if(ev is GameEvent_DamageDealt hit && hit.Source=="player") {
     if(hit.PresentationSource!=null)result[i]=FromPresentationSource(hit.PresentationSource,before,after,leader);
     else if(hit.SourceUid==null)result[i]=RestrictLegacyAuthored(current);
     else {
      var def=PermanentDef(hit.SourceUid,before,after);
      // Innate factory UIDs have exact registry aliases; ordinary doll UIDs
      // are opaque and must be looked up. No prefix/name inference for their element.
      var explicitSource=Entries["_sources"]?[hit.SourceUid];
      result[i]=RestrictLegacyAuthored(def!=null?Source(def.Id,def,leader):explicitSource!=null?Source(hit.SourceUid):Unknown("unresolved_source_uid","uid:"+hit.SourceUid));
     }
    }
   }
   return result;
  }
 }
}
