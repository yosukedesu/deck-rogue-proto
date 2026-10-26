using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeckRogue.Game {
 public partial class Autopilot {
  // Every rendered frame is captured. This is a fixed-step game recording, not interpolated stills.
  IEnumerator StrikeReview(GameRoot g) {
   bool mizore=Arg("-review")=="mizore";
   string leader=mizore?"leader_blue":"leader_green";
   string hand=mizore?"blue_strike,blue_guard,blue_frost_thorn,red_flame_wave":"green_strike,green_strike,green_strike,green_strike";
   yield return StateJump(g,"phase=combat;leader="+leader+";enemy=enemy_probe;hand="+hand+";penergy=10;dumplayout=1");
   Presenter.MarkSeen(g.Rs.Combat);
   Time.captureFramerate=30;
   var frames=Path.Combine(_dir,"frames");Directory.CreateDirectory(frames);
   for(int f=0;f<(mizore?240:180);f++) {
    // Isolated strike at 1s, followed by three accepted cards on consecutive frames at 3s.
    if(mizore?(f==30||f==75||f==120||f==165):(f==30 || f==90 || f==91 || f==92)) {
     g.PreferredTarget=0;
     var card=g.Battle.CardAt(0);
     string played=card.Card.Def.Id;
     ExecuteEvents.Execute(card.Rt.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
     Debug.Log("[StrikeReview] card="+played+" frame="+f+" energy="+g.Rs.Combat.Player.Energy+" enemyHp="+g.Rs.Combat.Enemies[0].Hp);
    }
    if(mizore)Debug.Log("[MizoreReview] frame="+f+" anim="+Stage.DebugAnim("player")+" particles="+WorldBattleFx.ParticleCount);
    yield return new WaitForEndOfFrame();
    var shot=ScreenCapture.CaptureScreenshotAsTexture();
    File.WriteAllBytes(Path.Combine(frames,"frame_"+f.ToString("D4")+".png"),shot.EncodeToPNG());
    Destroy(shot);
    yield return null;
   }
   File.WriteAllText(Path.Combine(_dir,"recording.txt"),(mizore?"Mizore: 240 frames; water at30, guard75, ice120, fire165. ":"")+"Actual Windows player, phone rendering profile; consecutive rendered frames at fixed 30 Hz; silent; strike frame 30, rapid input frames 90/91/92. Not Android device performance evidence.");
   Time.captureFramerate=DetFramerate;
  }
 }
}
