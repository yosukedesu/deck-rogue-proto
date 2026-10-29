using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.EditorTools {
 public static class OwnedBoundedChecks {
  static object F(object o,string n)=>o.GetType().GetField(n).GetValue(o);
  static void Check(bool b,string why){if(!b)throw new Exception(why);}
  public static void Run(){try{
   int samples=0;
   foreach(var name in new[]{"WraithCurse","CrossbowAimed"}){
    var type=Type.GetType("DeckRogue.Game.Hd2d.Hd2d"+name+"Geometry, Assembly-CSharp",true);
    var effect=type.GetMethod("Create",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    var sample=(Action<double>)F(effect,"Sample");
    foreach(double age in new[]{0,.033,.066,.1,.15,double.NaN,double.PositiveInfinity,double.NegativeInfinity,-.0001,.18,0,-.1,0,.18}){
     sample(age);samples++;int positive=0;
     foreach(var batch in (Array)F(effect,"Batches")){
      int count=(int)F(batch,"Count");Check(count==160,"capacity changed");
      foreach(int index in (int[])F(batch,"Indices"))Check(index>=0&&index<count,"index outside buffer");
      var alpha=(Delegate)F(batch,"Alpha");var position=(Delegate)F(batch,"Position");
      for(int i=0;i<count;i++){
       float a=(float)alpha.DynamicInvoke(i);Check(float.IsFinite(a)&&a>=0,"invalid alpha");if(a>0)positive++;
       var p=position.DynamicInvoke(i);foreach(string axis in new[]{"X","Y","Z"})Check(float.IsFinite((float)F(p,axis)),"nonfinite position");
      }
     }
     if(!double.IsFinite(age)||age<0||age>=.18)Check(positive==0,"invalid/expired sampler leak");
     if(age==0)Check(positive>0,"empty actual contact");
    }
   }
   Content.Load(System.IO.Path.Combine(Application.streamingAssetsPath,"data"));
   foreach(var entry in new[]{("WraithCurse","enemy_nemesis_wraith",0,9,"weak"),("CrossbowAimed","enemy_crossbow_archer",2,14,"vulnerable")}){
    var type=Type.GetType("DeckRogue.Game."+entry.Item1+"Binding, Assembly-CSharp",true);int selected=-1;
    for(int seed=1;seed<=80;seed++){
     var run=DeckRogue.Engine.Run.DebugLaunchCombat(DeckRogue.Engine.Run.CreateRun(seed,ReactionModes.SetConfirm,"leader_green"),entry.Item2);
     var cmd=new RunCommand_Combat{Command=new Command_EndTurn()};
     for(int prior=0;prior<entry.Item3;prior++){
      var next=DeckRogue.Engine.Run.ApplyRunCommand(run,cmd);var oldLedger=new CombatPresentationLedger();var oldBatch=oldLedger.Register(run.Combat,next.Combat,cmd);
      foreach(var hit in next.Combat.EventLog.Skip(run.Combat.EventLog.Count).OfType<GameEvent_DamageDealt>()){
       var receipt=oldLedger.Find(Array.FindIndex(next.Combat.EventLog.ToArray(),e=>ReferenceEquals(e,hit)),hit);
       Check(type.GetMethod("Capture").Invoke(null,new object[]{oldBatch,receipt,new Func<int>(()=>oldLedger.Generation)})==null,"prior bolt selected as aimed");
      }run=next;
     }
     var after=DeckRogue.Engine.Run.ApplyRunCommand(run,cmd);var events=after.Combat.EventLog.Skip(run.Combat.EventLog.Count).ToArray();var hits=events.OfType<GameEvent_DamageDealt>().ToArray();
     if(hits.Length!=1||hits[0].HpLoss!=entry.Item4)continue;
     var ledger=new CombatPresentationLedger();var batch=ledger.Register(run.Combat,after.Combat,cmd);var h=hits[0];var rec=ledger.Find(Array.FindIndex(after.Combat.EventLog.ToArray(),e=>ReferenceEquals(e,h)),h);
     var binding=type.GetMethod("Capture").Invoke(null,new object[]{batch,rec,new Func<int>(()=>ledger.Generation)});
     Check(binding!=null,"actual owner rejected");Check(!(bool)type.GetProperty("Committed").GetValue(binding),"early commit");Check(ledger.Commit(rec),"receipt not committed");Check((bool)type.GetProperty("Committed").GetValue(binding),"actual commitment missing");ledger.Clear();Check(!(bool)type.GetProperty("Committed").GetValue(binding),"stale generation");
     Check(events.OfType<GameEvent_StatusInflicted>().Count(x=>x.Status==entry.Item5&&x.Amount==1)==1,"independent status missing");selected=seed;break;
    }
    Check(selected>0,"exact damage fixture not found");Debug.Log("[OwnedBounded] "+entry.Item1+" PASS seed="+selected+" damage="+entry.Item4+" status="+entry.Item5+"1 prior="+entry.Item3);
   }
   Debug.Log("[OwnedBounded] PASS samples="+samples+"; renderer/fragment quality not certified");EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
