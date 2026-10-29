using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.EditorTools {
 public static class RootWeave319Checks {
  static void Check(bool b,string why){if(!b)throw new Exception(why);}
  public static void Run(){try{
   Content.Load(System.IO.Path.Combine(Application.streamingAssetsPath,"data"));
   var type=Type.GetType("DeckRogue.Game.RootWeaveNegationForecast, Assembly-CSharp",true);var method=type.GetMethod("Remaining");
   foreach(bool attack in new[]{false,true}){
    var run=DeckRogue.Engine.Run.DebugLaunchCombat(DeckRogue.Engine.Run.CreateRun(2,ReactionModes.SetConfirm,"leader_green"),"enemy_probe");
    var card=new CardInstance{Uid="qa-root319",Def=Content.GetCardDef("green_reaction_root_weave")};
    run=run with {Combat=run.Combat with {Player=run.Combat.Player with {Hand=new[]{card},Energy=3}}};
    run=DeckRogue.Engine.Run.ApplyRunCommand(run,new RunCommand_Combat{Command=new Command_SetCard{CardUid=card.Uid}});Check(run.Combat.Player.Energy==1,"Set cost2");
    var end=new RunCommand_Combat{Command=new Command_EndTurn()};run=DeckRogue.Engine.Run.ApplyRunCommand(run,end);Check(run.Combat.Phase==CombatPhases.PlayerTurn,"age0 should not arm");run=DeckRogue.Engine.Run.ApplyRunCommand(run,end);
    if(attack){run=DeckRogue.Engine.Run.ApplyRunCommand(run,new RunCommand_Combat{Command=new Command_ConfirmReaction{Fire=false}});run=DeckRogue.Engine.Run.ApplyRunCommand(run,end);}
    Check(run.Combat.Phase==CombatPhases.AwaitingReaction,"actual prewindow missing");int incoming=Effects.IncomingTotal(run.Combat);Check(attack?incoming==6:incoming==0,"unexpected real action");
    var cmd=new RunCommand_Combat{Command=new Command_ConfirmReaction{Fire=true,CardUid=card.Uid}};var after=DeckRogue.Engine.Run.ApplyRunCommand(run,cmd);var events=after.Combat.EventLog.Skip(run.Combat.EventLog.Count).ToArray();
    Check(events[0] is GameEvent_ReactionTriggered&&events[1] is GameEvent_GrowthAdded gr&&gr.Amount==2&&events[2] is GameEvent_ActionNegated,"actual ordered events");
    Check(!events.Any(x=>x is GameEvent_DamageDealt||x is GameEvent_EnemyActionResolved)&&run.Combat.Player.Hp==after.Combat.Player.Hp,"negation caused damage");
    var ledger=new CombatPresentationLedger();var batch=ledger.Register(run.Combat,after.Combat,cmd);var neg=(GameEvent_ActionNegated)events[2];
    Check((int?)method.Invoke(null,new object[]{batch,neg,ledger.Generation})==0,"cancelled incoming remains");
    Check(method.Invoke(null,new object[]{batch,new GameEvent_ActionNegated{EnemyIndex=0},ledger.Generation})==null,"unregistered equal event accepted");
    Check(method.Invoke(null,new object[]{batch,null,ledger.Generation})==null,"null accepted");ledger.Clear();Check(method.Invoke(null,new object[]{batch,neg,ledger.Generation})==null,"stale generation accepted");
    Debug.Log("[RootWeave319] PASS "+(attack?"attack6 after guard decline":"guard")+" hp="+run.Combat.Player.Hp+" unchanged; remaining0; registered ownership and generation");
   }
   EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
