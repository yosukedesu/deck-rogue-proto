using UnityEngine;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
namespace DeckRogue.Game {
 // One dominant effect color feeds VFX, particles and world contact/bounce lights.
 public static class CombatPalette {
  public static readonly Color Silver=new Color(.86f,.93f,1f);
  public static Color FromCard(string style,CardDef def) {
   string element=CardVfxProfile.Element(def,GameRoot.I?.LeaderId);
   if(element=="fire"||element=="water"||element=="ice"||element=="lightning"||element=="dark"||element=="light"||element=="vine"||element=="earth")return ElementBattleFx.ColorFor(element,Silver);
   string name=def?.Name??"", id=def?.Id??"";
   if(def?.Color=="red")return new Color(1f,.28f,.065f);
   if(def?.Color=="blue")return name.Contains("氷")||name.Contains("霜")||name.Contains("凍") ? new Color(.42f,.64f,1f) : new Color(.35f,.78f,1f);
   if(style=="spark")return new Color(1f,.34f,.10f);
   if(style=="light")return ThemeFx.LampGlow;
   if(style=="spell")return PaperFx.Mana;
   return ForFx(style,Silver);
  }
  public static Color ForFx(string style,Color requested) {
   if(style=="slash"||style=="fang"||style=="horn")return Silver;
   if(style=="vine")return new Color(.48f,.86f,.55f);
   if(style=="stomp")return new Color(.82f,.66f,.44f);
   return new Color(requested.r,requested.g,requested.b,1);
  }
 }
}
