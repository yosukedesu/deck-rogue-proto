#if RECOVERY_QA
#nullable enable
using System;using System.Collections;using System.Collections.Generic;using System.Linq;using System.Threading.Tasks;
using Godot;using DeckRogue.Engine;using DeckRogue.Engine.Generated;using DeckRogue.GodotPort.Presentation;
namespace DeckRogue.GodotPort.RecoveryQA;
public static partial class RecoveryMotionQa {
 static async Task RunR5DelayOnly(){
  foreach(string scenario in new[]{"boundary-variable","stall250ms"}){
   var fixture=Fixture(Array.Empty<string>(),new[]{1000});
   fixture=fixture with{Combat=fixture.Combat! with{Enemies=fixture.Combat!.Enemies.Select(e=>e with{
    EnemyId="enemy_slug",Node="acid_spit",IntentMoveId="acid_spit",IntentNode="acid_spit",
    Intent=new EnemyIntent{Kind="attack",Actual=6,Base=6,Inflict=new StatusInflict{Status="frail",Amount=1}}
   }).ToArray()}};
   await Load(fixture,"r5-"+scenario);
   var enemy=Desc(stage).OfType<BattleActor>().Single(a=>a.EnemyIndex==0);int beforeVersion=enemy.UiLunge.Version;
   var hitTimes=new List<float>();Action<GameEvent> collect=e=>{if(e is GameEvent_DamageDealt {Source:"enemy"})hitTimes.Add(stage.PresentationTime);};
   stage.NumericContactPresented+=collect;
   try{
    Dispatch(new RunCommand_Combat{Command=new Command_EndTurn()});
    Until(()=>enemy.UiLunge.Version!=beforeVersion,600,1f/60);
    Check(hitTimes.Count==0,"r5-outer-callback-does-not-land-"+scenario);
    float outerAt=stage.PresentationTime;var oracle=new LiteralAfterCo(.22f);
    await Capture("r5-"+scenario+"-outer");
    float[] deltas=scenario=="boundary-variable"?new[]{.04f,.04f,.04f,.04f,.04f,.02f,1f/60,1f/60}:new[]{.01f,.25f,1f/60};
    int first=-1;
    for(int i=0;i<deltas.Length;i++){
     oracle.Tick(deltas[i]);stage.AdvancePresentation(deltas[i]);
     if(hitTimes.Count>0&&first<0)first=i+1;
     rows.Add(new{kind="r5-inner-delay-step",scenario,step=i+1,delta=deltas[i],outerAt,clock=stage.PresentationTime,sourceFired=oracle.Fired,numericContacts=hitTimes.Count,enemyLungeVersion=enemy.UiLunge.Version,heroLungeVersion=Hero().UiLunge.Version,heroPoseStarts=Hero().PoseStartCount});
     Check(hitTimes.Count==(oracle.Fired?1:0),"r5-literal-afterco-contact-"+scenario+"-"+(i+1));
    }
    Check(first==(scenario=="boundary-variable"?7:2),"r5-exact-firing-step-"+scenario);
    await Capture("r5-"+scenario+"-contact");
    Drain();Check(hitTimes.Count==1,"r5-contact-exactly-once-"+scenario);
   }finally{stage.NumericContactPresented-=collect;}
  }
  Check(diagnostics.Count==0,"r5-no-session-diagnostics");
  rows.Add(new{kind="r5-proof-scope",nativeEngine=DisplayServer.GetName()!="headless",manualAdvancePresentation=true,continuousRenderedMotionProof=false,source="Stage41 Tween.cs:273-279 literal AfterCo",scope="Real command queue and actor callback ordering with isolated session and paused automatic clock; not live Android cadence or continuous render capture."});
 }
sealed class LiteralAfterCo {
sealed class PresentationWorkScope {public bool Cancelled,Completed;}
static class Time {public static float deltaTime;}
static void InPresentation(PresentationWorkScope scope,Action action)=>action();
static void ReleasePresentation(PresentationWorkScope scope){}
readonly IEnumerator coroutine;public bool Fired;
public LiteralAfterCo(float duration){coroutine=AfterCo(duration,()=>Fired=true,null);coroutine.MoveNext();}
public void Tick(float dt){Time.deltaTime=dt;coroutine.MoveNext();}
static IEnumerator AfterCo(float delay, Action action, PresentationWorkScope scope)
        {
            try {
                float t=0f;
                while(t<delay) {if(scope!=null && (scope.Cancelled || scope.Completed))yield break;yield return null;t+=Time.deltaTime;}
                InPresentation(scope,action);
            } finally { ReleasePresentation(scope); }
        }
}

}
#endif
