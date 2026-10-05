using System.Collections.Generic;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace DeckRogue.Game {
 // Explicit presentation data. No gameplay properties and no runtime card-name matching.
 public static class CardVfxProfile {
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
