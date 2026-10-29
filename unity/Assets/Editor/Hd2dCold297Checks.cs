using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.EditorTools {
 public static class Hd2dCold297Checks {
  static object F(object o,string n)=>o.GetType().GetField(n).GetValue(o);
  static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
  public static void Run(){try{
   var factory=Type.GetType("DeckRogue.Game.Hd2d.Hd2dColdSweepGeometry, Assembly-CSharp",true);
   var effect=factory.GetMethod("Create",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
   var sample=(Action<double>)F(effect,"Sample");var commit=(Action<bool>)F(effect,"ConfigureColdSweepCommittedContact");
   var b=((Array)F(effect,"Batches")).GetValue(0);int count=(int)F(b,"Count");Check(count==160,"Original buffer capacity changed");
   var alpha=(Func<int,float>)F(b,"Alpha");var pos=(Delegate)F(b,"Position");int samples=0;
   foreach(bool committed in new[]{false,true,false,true,false}){
    commit(committed);
    foreach(double age in new[]{-.1,0,.033,.066,.1,.145,.175,.18,.3,double.NaN,double.NegativeInfinity,double.PositiveInfinity}){
     sample(age);samples++;float visible=0;
     for(int i=0;i<count;i++){
      var p=pos.DynamicInvoke(i);foreach(var axis in new[]{"X","Y","Z"})Check(float.IsFinite((float)F(p,axis)),"Nonfinite vertex");
      Check(float.IsFinite(alpha(i)) && alpha(i)>=0,"Invalid alpha");visible+=alpha(i);
     }
     if(!committed || !(age>=0 && age<.18))Check(visible==0,"Pending/invalid/expired Cold geometry leaked");
     if(committed && age==0)Check(visible>0,"Cold contact absent");
    }
   }
   foreach(int i in (int[])F(b,"Indices"))Check(i>=0 && i<count,"Cold triangle index invalid");
   Content.Load(System.IO.Path.Combine(Application.streamingAssetsPath,"data"));
   var run=DeckRogue.Engine.Run.DebugLaunchCombat(DeckRogue.Engine.Run.CreateRun(4242,"set_confirm","leader_blue"),"enemy_probe");
   var card=new CardInstance{Uid="blue_cold_sweep#owned-test",Def=Content.GetCardDef("blue_cold_sweep")};
   run=run with {Combat=run.Combat with {Player=run.Combat.Player with {Hand=new[]{card},Energy=3}}};
   var cmd=new RunCommand_Combat{Command=new Command_PlayCard{CardUid=card.Uid,TargetIndex=0}};
   var after=DeckRogue.Engine.Run.ApplyRunCommand(run,cmd);var ledger=new CombatPresentationLedger();var batch=ledger.Register(run.Combat,after.Combat,cmd);
   var bindingType=Type.GetType("DeckRogue.Game.Hd2dColdSweepBinding, Assembly-CSharp",true);var capture=bindingType.GetMethod("Capture");
   int hits=0;var ice=new List<GameEvent_IceBlockGained>();
   for(int i=batch.From;i<batch.To;i++){
    var ev=after.Combat.EventLog[i];Debug.Log("[Cold297 actual] "+Newtonsoft.Json.JsonConvert.SerializeObject(ev));
    if(ev is GameEvent_IceBlockGained gained)ice.Add(gained);
    if(!(ev is GameEvent_DamageDealt hit))continue;
    Check(hit.HpLoss==4,"Cold actual damage changed");hits++;
    var receipt=ledger.Find(i,ev);var binding=capture.Invoke(null,new object[]{batch,receipt,new Func<int>(()=>ledger.Generation)});
    Check(binding!=null,"Actual Cold ownership rejected");Check(!(bool)bindingType.GetProperty("Committed").GetValue(binding),"Cold commit early");
    Check(ledger.Commit(receipt),"Cold numeric commit failed");Check((bool)bindingType.GetProperty("Committed").GetValue(binding),"Cold exact commit absent");
    ledger.Clear();Check(!(bool)bindingType.GetProperty("Committed").GetValue(binding),"Cold generation survived reset");
   }
   Check(hits==1,"Cold damage count changed");
   Check(ice.Count==2 && ice[0].Amount==4 && ice[1].Amount==1 && ice[0].PresentationReceipt.Before.IceBlock==0 && ice[0].PresentationReceipt.After.IceBlock==4 && ice[1].PresentationReceipt.Before.IceBlock==4 && ice[1].PresentationReceipt.After.IceBlock==5,"IceBlock4 and passiveIceBlock1 not distinct");
   var texture=Resources.Load<Texture2D>("Art/fx/cold-frost-density-v1");Check(texture!=null && texture.width==1774 && texture.height==887 && texture.mipmapCount==1,"Cold original texture resized or mipmapped");
   Debug.Log($"[Cold297] PASS {samples} geometry samples; actual4 damage / Ice4 / passiveIce1 separate; commit/reset; original1774x887 no mips. No visual parity claim.");EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
