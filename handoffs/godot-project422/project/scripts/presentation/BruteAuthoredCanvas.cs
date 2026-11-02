using System;
namespace DeckRogue.GodotPort.Presentation;
// Explicitly approved integer padding contracts only. No dynamic layout or scale.
public readonly record struct BruteAuthoredCanvas
{
 public int FrameWidth {get;}
 public int FrameHeight {get;}
 public int SourceOffsetX {get;}
 public int SourceOffsetY {get;}
 public int AnchorX {get;}
 public int AnchorY {get;}
 public int SpriteOffsetX {get;}
 public int SpriteOffsetY {get;}
 public int GroundMaxY {get;}
 private BruteAuthoredCanvas(int height,int top)
 {
  FrameWidth=144;FrameHeight=height;SourceOffsetX=32;SourceOffsetY=top;
  AnchorX=88;AnchorY=112+top;
  SpriteOffsetX=(FrameWidth-112)/2-SourceOffsetX;
  SpriteOffsetY=SourceOffsetY-(FrameHeight-112)/2;
  GroundMaxY=103+top;
 }
 public static BruteAuthoredCanvas Validate(int sourceWidth,int sourceHeight,int frameWidth,int frameHeight,
  int sourceX,int sourceY,int anchorX,int anchorY,float spriteOffsetX,float spriteOffsetY,int groundMaxY)
 {
  if(sourceWidth!=112||sourceHeight!=112||frameWidth!=144||sourceX!=32||
   !((frameHeight==112&&sourceY==0)||(frameHeight==128&&sourceY==16)))
   throw new ArgumentException("Unapproved brute source/frame padding contract");
  var value=new BruteAuthoredCanvas(frameHeight,sourceY);
  if(anchorX!=value.AnchorX||anchorY!=value.AnchorY||spriteOffsetX!=value.SpriteOffsetX||
   spriteOffsetY!=value.SpriteOffsetY||groundMaxY!=value.GroundMaxY)
   throw new ArgumentException("Authored frame anchor/offset/ground registration mismatch");
  return value;
 }
}
