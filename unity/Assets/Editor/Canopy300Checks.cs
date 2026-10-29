using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.EditorTools {
 public static class Canopy300Checks {
  static object F(object o,string n)=>o.GetType().GetField(n).GetValue(o);
  static void Check(bool b,string why){if(!b)throw new Exception(why);}
  public static void Run(){try{
   var ft=Type.GetType("DeckRogue.Game.Hd2d.Hd2dAttackPrototype, Assembly-CSharp",true);var effect=ft.GetMethod("CreateCanopyContact",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
   var sample=(Action<double>)F(effect,"Sample");var commit=(Action<bool>)F(effect,"ConfigureCanopyCommittedContact");var light=(Func<float>)F(effect,"Light");var batches=(Array)F(effect,"Batches");Check(batches.Length==4,"Canopy batches");int samples=0;
   foreach(bool state in new[]{false,true,false,true,false})foreach(double age in new[]{-.1,0,.033,.0833,.11,.17,.32,.4,double.NaN,double.PositiveInfinity,double.NegativeInfinity}){
    commit(state);sample(age);samples++;float sum=0;
    foreach(var meshBatch in batches){int n=(int)F(meshBatch,"Count");var alpha=(Func<int,float>)F(meshBatch,"Alpha");var pos=(Delegate)F(meshBatch,"Position");foreach(int ix in (int[])F(meshBatch,"Indices"))Check(ix>=0&&ix<n,"Canopy indices");for(int i=0;i<n;i++){Check(float.IsFinite(alpha(i))&&alpha(i)>=0,"Canopy alpha");sum+=alpha(i);var p=pos.DynamicInvoke(i);foreach(string axis in new[]{"X","Y","Z"})Check(float.IsFinite((float)F(p,axis)),"Canopy vertex");}}
    if(!state || !double.IsFinite(age) || age<0 || age>.32)Check(sum==0,"Canopy gated geometry");if(!state || !double.IsFinite(age) || age<0)Check(light()==0,"Canopy gated light");if(state&&age==0)Check(sum>0,"Canopy absent");
   }
   Content.Load(System.IO.Path.Combine(Application.streamingAssetsPath,"data"));
   var run=DeckRogue.Engine.Run.DebugLaunchCombat(DeckRogue.Engine.Run.CreateRun(2,ReactionModes.SetConfirm,"leader_green"),"enc_probe_trio");
   var canopy=new CardInstance{Uid="installed-canopy",Def=Content.GetCardDef("green_perm_thorn_canopy")};var sprout=new CardInstance{Uid="trigger-sprout",Def=Content.GetCardDef("green_wild_sprout")};
   run=run with {Combat=run.Combat with {Player=run.Combat.Player with {Hand=new[]{canopy,sprout},Energy=3,EnergyMax=3,EnergyMaxAtTurnStart=3,Growth=0}}};
   var installed=DeckRogue.Engine.Run.ApplyRunCommand(run,new RunCommand_Combat{Command=new Command_PlayCard{CardUid=canopy.Uid}});Check(installed.Combat.Player.Energy==1&&installed.Combat.Player.Permanents.Any(p=>p.Uid==canopy.Uid),"Canopy real cost2 install");
   var cmd=new RunCommand_Combat{Command=new Command_PlayCard{CardUid=sprout.Uid}};var after=DeckRogue.Engine.Run.ApplyRunCommand(installed,cmd);Check(after.Combat.Player.Energy==1&&after.Combat.Player.Growth==1,"Sprout cost0 Growth1");
   var ledger=new CombatPresentationLedger();var batch=ledger.Register(installed.Combat,after.Combat,cmd);var bt=Type.GetType("DeckRogue.Game.CanopyBinding, Assembly-CSharp",true);int hits=0;
   for(int i=batch.From;i<batch.To;i++)if(after.Combat.EventLog[i] is GameEvent_DamageDealt hit){hits++;Check(hit.HpLoss==3 && hit.SourceUid==canopy.Uid && hit.PresentationSource.Uid==canopy.Uid && hit.PresentationSource.Kind=="permanent","Installed UID / actual3");var receipt=ledger.Find(i,hit);var binding=bt.GetMethod("Capture").Invoke(null,new object[]{batch,receipt,new Func<int>(()=>ledger.Generation)});Check(binding!=null,"Canopy owner");Check(!(bool)bt.GetProperty("Committed").GetValue(binding),"Early canopy");Check(ledger.Commit(receipt),"Canopy commit");Check((bool)bt.GetProperty("Committed").GetValue(binding),"Canopy receipt");}
   Check(hits==3,"Canopy all three enemies");Debug.Log("[Canopy300] PASS "+samples+" geometry samples; real cost2 install, Sprout0 Growth1, three actual3, installed UID separate from trigger. No native parity claim.");EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
