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
   public readonly float ImpactPause, ChainPause, FinalPause;
   public Showcase(float contact,float windup,float interval,float recovery,float residue,float reach,float impactPause,float chainPause,float finalPause,bool heavy=false) {
    Contact=contact;Windup=windup;Interval=interval;Recovery=recovery;Residue=residue;Reach=reach;Heavy=heavy;
    ImpactPause=impactPause;ChainPause=chainPause;FinalPause=finalPause;
   }
  }
  // Seconds of local character hitstop; contacts, HP, lights, particles and input keep their clock.
  public static float ImpactPauseFor(Showcase profile,int index,int total,bool finishing=false) {
   if(profile==null)return 0f;
   return finishing ? .14f : total>1 ? (index<total-1 ? profile.ChainPause : profile.FinalPause) : profile.ImpactPause;
  }
  static readonly Showcase Slash=new Showcase(.13f,.045f,.12f,.14f,.36f,.48f,.065f,.025f,.095f);
  static readonly Showcase Vine=new Showcase(.16f,.065f,.12f,.16f,.44f,.42f,.065f,.025f,.095f);
  static readonly Showcase Water=new Showcase(.24f,.14f,.12f,.18f,.48f,.10f,.075f,.025f,.100f);
  static readonly Showcase Earth=new Showcase(.30f,.19f,.15f,.22f,.58f,.10f,.110f,.025f,.110f);
  static readonly Showcase Stomp=new Showcase(.34f,.22f,.16f,.24f,.65f,.62f,.120f,.030f,.120f,true);
  public static bool ShowcaseOn => HD2DFlags.CombatPresentationShowcase && Stage.CombatWorld;
  public static Showcase Comparison(CardDef def) => Comparison(def?.Id);
  public static Showcase Comparison(string id) {
   if(!ShowcaseOn)return null;
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
 }
}
