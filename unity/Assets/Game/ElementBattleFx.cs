using System.Collections.Generic;
using UnityEngine;
namespace DeckRogue.Game {
 public static class ElementBattleFx {
  // Review-only switches cannot alter normal player behavior.
  public static readonly string LightReviewMode=ReadReviewMode();
  static string ReadReviewMode(){var a=System.Environment.GetCommandLineArgs();if(System.Array.IndexOf(a,"-autopilot")<0)return "separated";int i=System.Array.IndexOf(a,"-vfx-light-review");return i>=0&&i+1<a.Length?a[i+1]:"separated";}
  internal static bool Luminous(string element) => (element=="lightning" || element=="light") && Autopilot.Arg("-vfx-composite-review")!="post";
  public static bool SeparateMaterial=>LightReviewMode!="baseline"&&LightReviewMode!="weak";
  static readonly Dictionary<string,Texture2D> Textures=new Dictionary<string,Texture2D>();
  public static readonly Dictionary<string,int> FrameMasks=new Dictionary<string,int>();
  public static int Frame(float time)=>Mathf.Clamp(Mathf.FloorToInt(time*8),0,7);
  public static void Observe(string name,int frame){FrameMasks.TryGetValue(name,out var mask);FrameMasks[name]=mask|(1<<frame);}
  internal static bool ForceMissingForTest;
  public static bool TryGet(string element,out Texture2D texture,out float duration){
   if(ForceMissingForTest){texture=null;duration=0;return false;}
   duration=element=="lightning"?.24f:element=="ice"?.32f:element=="dark"?.40f:element=="light"?.30f:element=="heal"?.50f:.36f;
   if(!Textures.TryGetValue(element,out texture)){texture=Resources.Load<Texture2D>("Art/fx/element-"+element+"-8f");Textures[element]=texture;}
   return texture!=null;
  }
  public static Color ColorFor(string element,Color fallback){
   switch(element){case "vine":return new Color(.48f,.86f,.55f);case "earth":return new Color(.65f,.47f,.30f);case "fire":return new Color(1,.28f,.065f);case "water":return new Color(.24f,.67f,1);case "ice":return new Color(.52f,.78f,1);case "lightning":return new Color(.7f,.66f,1);case "dark":return new Color(.43f,.18f,.75f);case "light":return new Color(1,.96f,.86f);case "heal":return new Color(.5f,1,.7f);default:return fallback;}
  }
 }
}
