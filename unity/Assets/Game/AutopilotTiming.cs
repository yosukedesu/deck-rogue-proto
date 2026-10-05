using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DeckRogue.Game {
 public partial class Autopilot {
  IEnumerator TimingReview(GameRoot g){
   var cases=new[]{"idle","single","multi","rapid","spell","enemy"};
   foreach(var name in cases){
    string hand=name=="multi"?"green_double_lash":name=="spell"?"blue_strike":"green_strike,green_strike,green_strike,green_strike";
    yield return StateJump(g,"phase=combat;leader="+(name=="idle"||name=="spell"?"leader_blue":"leader_green")+";enemy=enemy_probe;hand="+hand+";penergy=20");
    Presenter.MarkSeen(g.Rs.Combat);yield return new WaitForSecondsRealtime(1);
    CombatTimingProbe.Begin();
    for(int f=0;f<180;f++){
     if(name!="idle"&&(f==30||(name=="rapid"&&(f==33||f==36||f==39)))){
      g.PreferredTarget=0;
      if(name=="enemy"){CombatTimingProbe.Event("input","endTurn");g.DoCombat(new DeckRogue.Engine.Generated.Command_EndTurn());}
      else {var card=g.Battle.CardAt(0);CombatTimingProbe.Event("input",card.Card.Def.Id);ExecuteEvents.Execute(card.Rt.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);}
      CombatTimingProbe.Event("inputReturned");
     }
     yield return new WaitForEndOfFrame();CombatTimingProbe.Sample();
     if(Arg("-timing-capture")=="1" && name=="enemy") {var frames=Path.Combine(_dir,"enemy-frames");Directory.CreateDirectory(frames);var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(frames,"frame_"+f.ToString("D4")+".png"),shot.EncodeToPNG());Destroy(shot);}
     yield return null;
    }
    CombatTimingProbe.Save(Path.Combine(_dir,"timing-"+name+".json"));
   }
  }
 }
}
