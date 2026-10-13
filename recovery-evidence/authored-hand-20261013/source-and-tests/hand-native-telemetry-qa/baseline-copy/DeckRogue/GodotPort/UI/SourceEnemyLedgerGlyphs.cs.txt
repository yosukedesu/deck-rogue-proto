// Exact Theme.IconArt rows; all other semantic glyphs reuse the existing source set.
using Godot;
using System;
using System.Collections.Generic;
namespace DeckRogue.GodotPort.UI;
public static class SourceEnemyLedgerGlyphs
{
 private static readonly Dictionary<string,Texture2D> Cache=new();
 private static readonly Dictionary<string,string[]> Rows=new(){
 {"counter",new[]{"................","................","....--..........","...-#-..........","..-#-----.......",".-######-.......","..-#-----#-.....","...-#-...-#-....","....--....-#-...","...........#-...","..........-#-...",".....---.-#-....",".....-###-......","......---.......","................","................"}},
 {"armor",new[]{"................","..###......###..","..####....####..","..############..","...##########...","...##########...","...##########...","................","...##########...","...##########...","................","...##########...","....########....","................","................","................"}},
 };
 // PaperFx NightTint: 32px source raster, radius8, slice10, line1.5.
 // Its white edge is tinted by fill/.72; no neutral-outline substitute.
 public static void PillPaper(Control parent,Color fill,float unit)
 {
  string key="pill:"+fill.ToHtml();
  if(!Cache.TryGetValue(key,out var texture))
  {
   using var image=Image.CreateEmpty(32,32,false,Image.Format.Rgba8);
   var edge=new Color(fill.R/.72f,fill.G/.72f,fill.B/.72f,fill.A);
   for(int y=0;y<32;y++)for(int x=0;x<32;x++)
   {
    float qx=Math.Abs(x+.5f-16)-8,qy=Math.Abs(y+.5f-16)-8;
    float d=8-MathF.Sqrt(Math.Max(qx,0)*Math.Max(qx,0)+Math.Max(qy,0)*Math.Max(qy,0))-Math.Min(Math.Max(qx,qy),0);
    Color c=d<0?Colors.Transparent:d<1.5f?edge:fill;
    if(d>=0&&d<1)c.A*=Math.Clamp(d+.5f,0,1);image.SetPixel(x,y,c);
   }
   Cache[key]=texture=ImageTexture.CreateFromImage(image);
  }
  var paper=new NinePatchRect{Name="PillPaper",Texture=texture,RegionRect=new Rect2(0,0,32,32),Size=parent.Size/unit,Scale=Vector2.One*unit,TextureFilter=CanvasItem.TextureFilterEnum.Linear,MouseFilter=Control.MouseFilterEnum.Ignore};
  foreach(Side side in Enum.GetValues<Side>())paper.SetPatchMargin(side,10);parent.AddChild(paper);
 }
 public static Texture2D Mono(string name)
 {
  if(!Rows.TryGetValue(name,out var rows))return SourceIntentGlyphs.Mono(name);
  if(Cache.TryGetValue(name,out var t))return t;
  using var image=Image.CreateEmpty(16,16,false,Image.Format.Rgba8);
  for(int y=0;y<16;y++)for(int x=0;x<16;x++)image.SetPixel(x,y,rows[y][x]=='#'||rows[y][x]=='+'?Colors.White:Colors.Transparent);
  return Cache[name]=ImageTexture.CreateFromImage(image);
 }
}
