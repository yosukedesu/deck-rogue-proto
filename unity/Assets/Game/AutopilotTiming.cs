using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DeckRogue.Game {
 public partial class Autopilot {
  IEnumerator TimingReview(GameRoot g){
   var cases=Arg("-timing-sequence")=="1" ? new[]{"enemy","enemy-shield","enemy-fast","enemy-rebuild"} : new[]{"idle","single","multi","rapid","spell","enemy"};
   foreach(var name in cases){
    string hand=name=="multi"?"green_double_lash":name=="spell"?"blue_strike":"green_strike,green_strike,green_strike,green_strike";
    yield return StateJump(g,"phase=combat;leader="+(name=="idle"||name=="spell"?"leader_blue":"leader_green")+";enemy=enemy_probe;hand="+hand+";penergy=20"+(name=="enemy-shield"?";pblock=10":""));
    Presenter.MarkSeen(g.Rs.Combat);yield return new WaitForSecondsRealtime(1);
    CombatTimingProbe.Begin();
    for(int f=0;f<180;f++){
     if(name!="idle"&&(f==30||(name=="rapid"&&(f==33||f==36||f==39)))){
      g.PreferredTarget=0;
      if(name.StartsWith("enemy")){CombatTimingProbe.Event("input","endTurn");g.DoCombat(new DeckRogue.Engine.Generated.Command_EndTurn());}
      else {var card=g.Battle.CardAt(0);CombatTimingProbe.Event("input",card.Card.Def.Id);ExecuteEvents.Execute(card.Rt.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);}
      CombatTimingProbe.Event("inputReturned");
     }
     if(name=="enemy-rebuild" && f==45){g.Rebuild();CombatTimingProbe.Event("rebuild","sameState");}
     if(name=="enemy-fast" && f==45){
      var before=g.Rs.Combat;var hc=g.Battle.CardAt(0);TempoRequire(hc!=null && hc.Playable,"early next-turn card unavailable");
      CombatTimingProbe.Event("input","earlyCard:"+hc.Card.Def.Id);
      ExecuteEvents.Execute(hc.Rt.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
      TempoRequire(g.Error==null && !ReferenceEquals(before,g.Rs.Combat),"early input blocked");
      CombatTimingProbe.Event("inputReturned","earlyCard");
     }
     yield return new WaitForEndOfFrame();CombatTimingProbe.Sample();
     if(Arg("-timing-capture")=="1" && name.StartsWith("enemy")) {var frames=Path.Combine(_dir,name+"-frames");Directory.CreateDirectory(frames);var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(frames,"frame_"+f.ToString("D4")+".png"),shot.EncodeToPNG());Destroy(shot);}
     yield return null;
    }
    if(name.StartsWith("enemy")){
     TempoRequire(g.Battle.ShownPlayerBlock==g.Rs.Combat.Player.Block && g.Battle.ShownPlayerIce==g.Rs.Combat.Player.IceBlock,"stale defense display "+name);
     var bar=g.Battle.SelfArea.GetComponentInChildren<BattleScreen.HpBarInfo>();
     TempoRequire(bar!=null && bar.Value==g.Rs.Combat.Player.Hp,"stale HP display "+name);
     CombatTimingProbe.Event("displayVerified",name);
    }
    CombatTimingProbe.Save(Path.Combine(_dir,"timing-"+name+".json"));
   }
  }
 }
}
