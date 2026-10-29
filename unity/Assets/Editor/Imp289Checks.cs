using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.EditorTools {
 public static class Imp289Checks {
  static object F(object o,string n)=>o.GetType().GetField(n).GetValue(o);
  static void Check(bool b,string why){if(!b)throw new Exception(why);}
  public static void Run(){try{
   var gt=Type.GetType("DeckRogue.Game.FireContinuousGeometry, Assembly-CSharp",true);var it=Type.GetType("DeckRogue.Game.ImpSparkGeometry, Assembly-CSharp",true);
   var old=gt.GetMethod("Create").Invoke(null,new object[]{"projectile"});var imp=Activator.CreateInstance(it,new[]{old});
   gt.GetMethod("ConfigureSource").Invoke(null,new object[]{old,-2.0,.3,0.0,true});
   int samples=0;foreach(bool committed in new[]{false,true,false,true,false})foreach(double age in new[]{-.1,0,.033,.066,.1,.18,.22,.276}){
    gt.GetMethod("Sample").Invoke(null,new[]{old,(object)age,.13});it.GetField("Committed").SetValue(imp,committed);it.GetMethod("Sample").Invoke(imp,new[]{old});samples++;
    var batches=(Array)F(imp,"Batches");Check(batches.Length==8,"Imp capacity");
    for(int k=0;k<8;k++){var b=batches.GetValue(k);var vs=(Array)F(b,"Vertices");foreach(int ix in (int[])F(b,"Triangles"))Check(ix>=0&&ix<vs.Length,"Imp indices");foreach(double a in (double[])F(b,"Alpha"))Check(double.IsFinite(a)&&a>=0,"Imp alpha");}
    double hot=((double[])F(F(imp,"Hot"),"Alpha")).Sum();if(!committed||age<0||age>=.22)Check(hot==0,"Imp pending/expired leak");if(committed&&age==0)Check(hot>0,"Imp missing contact");
   }
   it.GetMethod("Clear").Invoke(imp,null);Check(!(bool)F(imp,"Committed"),"Imp reset");
   Content.Load(System.IO.Path.Combine(Application.streamingAssetsPath,"data"));var bt=Type.GetType("DeckRogue.Game.ImpSparkBinding, Assembly-CSharp",true);
   int selected=-1;for(int seed=1;seed<=20;seed++){
    var run=DeckRogue.Engine.Run.DebugLaunchCombat(DeckRogue.Engine.Run.CreateRun(seed,ReactionModes.SetConfirm,"leader_green"),"enemy_cinder_imp");
    var cmd=new RunCommand_Combat{Command=new Command_EndTurn()};var after=DeckRogue.Engine.Run.ApplyRunCommand(run,cmd);
    var hits=after.Combat.EventLog.Skip(run.Combat.EventLog.Count).OfType<GameEvent_DamageDealt>().ToArray();
    if(hits.Length!=1||hits[0].HpLoss!=7)continue;
    var ledger=new CombatPresentationLedger();var batch=ledger.Register(run.Combat,after.Combat,cmd);var hit=hits[0];var receipt=ledger.Find(Array.FindIndex(after.Combat.EventLog.ToArray(),e=>ReferenceEquals(e,hit)),hit);
    var binding=bt.GetMethod("Capture").Invoke(null,new object[]{batch,receipt,new Func<int>(()=>ledger.Generation)});Check(binding!=null,"Imp actual owner rejected");
    Check(!(bool)bt.GetProperty("Committed").GetValue(binding),"Imp early commitment");Check(ledger.Commit(receipt),"Imp numeric commit");Check((bool)bt.GetProperty("Committed").GetValue(binding),"Imp missing receipt commit");ledger.Clear();Check(!(bool)bt.GetProperty("Committed").GetValue(binding),"Imp stale generation");
    var ev=after.Combat.EventLog.Skip(run.Combat.EventLog.Count).ToArray();Check(ev.OfType<GameEvent_StatusInflicted>().Count(x=>x.Status=="scald"&&x.Amount==1)==1&&!ev.OfType<GameEvent_ScaldTick>().Any(),"Scald rider/tick separation");selected=seed;break;
   }
   Check(selected>0,"No exact7 seed found");Debug.Log("[Imp289] PASS samples="+samples+" actual7+Scald1; seed="+selected+"; receipt/reset verified. Visual parity not certified.");EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
