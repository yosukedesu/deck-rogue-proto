using System;
using UnityEngine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game {
 // Exactly one receipt per resolved engine event; card names/effect names cannot suppress it.
 public static class ResolvedCombatFx {
  public sealed class Ticket {
   public string Event,Target,Style="slash",Outcome,Element="physical"; public int Amount,Index,Total=1;
   public Color Color=CombatPalette.Silver; public Vector3 Position; public float Height;
   public Vector2 Ui; public bool HasWorld,Shown,Finishing,Heavy;
   public CardVfxProfile.Showcase Showcase; public Stage.ImpactCapture Impact; public float RequestedPause,AppliedPause;
  }
  public static int Received {get;private set;} public static int Presented {get;private set;}
  public static int WorldPresented {get;private set;} public static int FallbackPresented {get;private set;}
  public static int DuplicateSuppressed {get;private set;}
  public static bool IsResolved(GameEvent ev) => ev is GameEvent_DamageDealt || ev is GameEvent_BurnTick || ev is GameEvent_ScaldTick || ev is GameEvent_ThornsReflected || ev is GameEvent_HpLost || ev is GameEvent_HpHealed || ev is GameEvent_EnemyHealed || ev is GameEvent_RegenTicked;
  public static Ticket Capture(GameRoot g,GameEvent ev) {
   if(!IsResolved(ev))return null;
   var t=new Ticket{Event=ev.Type,Target="player",Outcome="hit"};
   switch(ev) {
    case GameEvent_HpHealed b:t.Amount=b.Amount;t.Element="heal";t.Outcome="heal";t.Color=ElementBattleFx.ColorFor("heal",Color.white);break;
    case GameEvent_EnemyHealed b:t.Target="enemy"+b.TargetIndex;t.Amount=b.Amount;t.Element="heal";t.Outcome="heal";t.Color=ElementBattleFx.ColorFor("heal",Color.white);break;
    case GameEvent_RegenTicked b:t.Target="enemy"+b.EnemyIndex;t.Amount=b.Amount;t.Element="heal";t.Outcome="heal";t.Color=ElementBattleFx.ColorFor("heal",Color.white);break;
    case GameEvent_DamageDealt d:
     t.Target=d.Source=="player"?"enemy"+(d.EnemyIndex??0):"player";t.Amount=d.HpLoss;t.Heavy=d.Amount>=15||d.Exposed==true;
     if(d.HpLoss<=0)t.Outcome=(d.Blocked??0)>0?"guard":"nullified";
     break;
    case GameEvent_BurnTick b:t.Target="enemy"+b.EnemyIndex;t.Amount=b.Amount;t.Style="spark";t.Element="fire";t.Color=new Color(1,.28f,.065f);break;
    case GameEvent_ScaldTick b:t.Amount=b.Amount;t.Style="spark";t.Element="fire";t.Color=new Color(1,.28f,.065f);break;
    case GameEvent_ThornsReflected b:t.Amount=b.HpLoss;t.Style="vine";if(b.HpLoss<=0)t.Outcome="guard";break;
    case GameEvent_HpLost b:t.Amount=b.Amount;t.Outcome="cost";t.Color=new Color(.65f,.25f,.45f);break;
   }
   if(t.Amount<=0 && t.Outcome=="hit")t.Outcome="nullified";
   t.HasWorld=Stage.TryGetUnitBox(t.Target,out var feet,out var h);t.Height=h;
   t.Position=feet+(Stage.LayoutRotation*Vector3.up)*h*.5f-(Stage.LayoutRotation*Vector3.forward)*.28f;
   var anchor=g.Anchor(t.Target);
   // A resolved impact can outlive its actor. Retain its position, never reselect another enemy.
   t.Ui=anchor!=null && g.FxLayer!=null?Tween.CenterIn(anchor,g.FxLayer):new Vector2(t.Target=="player"?-240:240,100);
   t.Impact=Stage.CaptureImpact(t.Target);
   Received++;return t;
  }
  public static void Show(GameRoot g,RectTransform fx,Ticket t) {
   if(t==null)return;if(t.Shown){DuplicateSuppressed++;return;}t.Shown=true;
   bool done=false;
   if(t.HasWorld && t.Outcome!="cost") {
    if(t.Outcome=="heal")done=WorldBattleFx.ElementAt(t.Position,t.Height,"heal");
    else if(t.Outcome=="guard" || t.Outcome=="nullified")done=WorldBattleFx.GuardAt(t.Position,t.Height,t.Target=="player"?1:-1);
    else done=WorldBattleFx.ElementAt(t.Position,t.Height,t.Element)||WorldBattleFx.HitAt(t.Position,t.Height,t.Style,t.Color,t.Heavy,t.Index,t.Target);
   }
   if(done){WorldPresented++;if(t.Outcome=="hit") {if(t.Element!="physical" && t.Element!="arcane")StageFx.ElementHit(t.Target,t.Element,t.Color,t.Heavy);else if(t.Target=="player")StageFx.FoeHit(t.Target,t.Color,t.Amount>=15);else if(t.Finishing)StageFx.Finish(t.Target,t.Color);else StageFx.PlayerHit(t.Target,t.Style,t.Heavy,t.Color);}else if(t.Outcome=="heal")StageFx.Cast(t.Target,"light",t.Color);else StageFx.Guard(t.Target,t.Target=="player"?1:-1);}
   else if(fx!=null){Tween.RingBurst(fx,t.Ui,t.Outcome=="hit"?t.Color:PaperFx.SkyLight,90,.22f);FallbackPresented++;done=true;}
   if(fx!=null && (t.Outcome=="nullified"||t.Outcome=="cost"))Tween.Float(fx,t.Ui,t.Outcome=="cost"?"HP −"+t.Amount:"無効",t.Color,24,24,.5f);
   if(done && t.Outcome=="hit" && t.Showcase!=null && t.HasWorld) {
    WorldBattleFx.ShowcaseResidue(t.Position,t.Height,t.Color,t.Showcase);
    Stage.Flash(t.Target,.075f);
    t.RequestedPause=CardVfxProfile.ImpactPauseFor(t.Showcase,t.Index,t.Total,t.Finishing);
    t.AppliedPause=Stage.ShowcaseImpact(t.Target,t.Impact,t.RequestedPause,t.Showcase.Heavy);
   }
   if(done)Presented++;else Debug.LogError("[ResolvedVfx] missing surface "+t.Event+" "+t.Target);
   if(Debug.isDebugBuild || Environment.GetCommandLineArgs().Length>1)Debug.Log("[ResolvedVfx] event="+t.Event+" target="+t.Target+" outcome="+t.Outcome+" style="+t.Style+" element="+t.Element+" visible="+done);
  }
 }
}
