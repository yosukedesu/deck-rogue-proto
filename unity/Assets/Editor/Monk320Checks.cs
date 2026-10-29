using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.EditorTools {
 public static class Monk320Checks {
  static void Check(bool b,string why){if(!b)throw new Exception(why);}
  public static void Run(){try{
   Content.Load(System.IO.Path.Combine(Application.streamingAssetsPath,"data"));var type=Type.GetType("DeckRogue.Game.MonkSelfHealBinding, Assembly-CSharp",true);var capture=type.GetMethod("Capture");
   foreach(bool full in new[]{false,true}){
    var run=DeckRogue.Engine.Run.DebugLaunchCombat(DeckRogue.Engine.Run.CreateRun(2,ReactionModes.SetConfirm,"leader_white"),"enemy_probe");
    var monk=new CardInstance{Uid="monk320",Def=Content.GetCardDef("white_perm_monk")};var squire=new CardInstance{Uid="squire320",Def=Content.GetCardDef("white_perm_squire")};
    run=run with {Combat=run.Combat with {Player=run.Combat.Player with {Hand=new[]{monk,squire},Energy=3,Hp=run.Combat.Player.MaxHp-(full?0:8)}}};
    var cmd=new RunCommand_Combat{Command=new Command_PlayCard{CardUid=monk.Uid}};var after=DeckRogue.Engine.Run.ApplyRunCommand(run,cmd);var ev=after.Combat.EventLog.Skip(run.Combat.EventLog.Count).ToArray();var heal=ev.OfType<GameEvent_HpHealed>().Single();
    Check(heal.Amount==(full?0:1)&&heal.SourceUid==monk.Uid,"actual self heal");Check(ev.OfType<GameEvent_LightGained>().Single().Amount==1,"separate Light1");Check(after.Combat.Player.Energy==2,"cost1");
    var ledger=new CombatPresentationLedger();var batch=ledger.Register(run.Combat,after.Combat,cmd);var receipt=ledger.Find(Array.FindIndex(after.Combat.EventLog.ToArray(),x=>ReferenceEquals(x,heal)),heal);var binding=capture.Invoke(null,new object[]{batch,receipt,new Func<int>(()=>ledger.Generation)});
    Check(full?binding==null:binding!=null,"self/fullHP boundary");
    if(binding!=null){Check((bool)type.GetProperty("Current").GetValue(binding),"precommit admission missing");Check(!receipt.Committed,"admission should not mutate commit");ledger.Clear();Check(!(bool)type.GetProperty("Current").GetValue(binding),"stale generation");}
    if(!full){
     run=after;cmd=new RunCommand_Combat{Command=new Command_PlayCard{CardUid=squire.Uid,TargetIndex=0}};after=DeckRogue.Engine.Run.ApplyRunCommand(run,cmd);ev=after.Combat.EventLog.Skip(run.Combat.EventLog.Count).ToArray();heal=ev.OfType<GameEvent_HpHealed>().Single();Check(heal.SourceUid==monk.Uid&&heal.Amount==1,"nonself Monk heal");
     batch=ledger.Register(run.Combat,after.Combat,cmd);receipt=ledger.Find(Array.FindIndex(after.Combat.EventLog.ToArray(),x=>ReferenceEquals(x,heal)),heal);Check(capture.Invoke(null,new object[]{batch,receipt,new Func<int>(()=>ledger.Generation)})==null,"nonself Squire incorrectly admitted");
     var squireType=Type.GetType("DeckRogue.Game.MonkSquireHealBinding, Assembly-CSharp",true);var selected=squireType.GetMethod("Capture").Invoke(null,new object[]{batch,receipt,new Func<int>(()=>ledger.Generation)});Check(selected!=null,"321 Squire admission missing");Check((bool)squireType.GetProperty("Current").GetValue(selected),"321 precommit current");ledger.Clear();Check(!(bool)squireType.GetProperty("Current").GetValue(selected),"321 stale generation");
     Check(ev.OfType<GameEvent_DamageDealt>().Any(x=>x.HpLoss==3)&&ev.Any(x=>x is GameEvent_RetainerRushed),"Squire damage3/rush missing");
    }
   }
   Debug.Log("[Monk320] PASS actual self Heal1/Light1 cost1; precommit admission, generation reset, nonself Squire heal/rush3 exclusion, fullHP Heal0/Light1 exclusion. GPU and craft require native capture.");EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
