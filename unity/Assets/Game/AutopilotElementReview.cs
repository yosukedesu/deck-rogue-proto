using System;
using System.Collections;
using System.IO;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DeckRogue.Game {
 public partial class Autopilot {
  IEnumerator ElementReview(GameRoot g){
   string[] elements={"fire","water","ice","lightning","dark","light","heal","vine","earth"};
   string[] cards={"red_strike","blue_strike","blue_ice_lance","blue_aether_burst","black_grave_bolt","white_strike","white_mass_heal","green_double_lash","green_earth_roar"};
   string[] leaders={"leader_red","leader_blue","leader_blue","leader_izzet","leader_black","leader_white","leader_white","leader_green","leader_green"};
   var report=new JArray();var frames=Path.Combine(_dir,"frames");Directory.CreateDirectory(frames);int output=0;
   for(int c=0;c<elements.Length;c++){
    yield return StateJump(g,"phase=combat;leader="+leaders[c]+";enemy=enemy_probe;hand="+cards[c]+";penergy=20;dumplayout=1");
    var st=g.Rs.Combat;var enemies=st.Enemies.ToList();enemies[0]=enemies[0] with {Hp=200,MaxHp=200};
    g.Rs=g.Rs with {Combat=st with {Player=st.Player with {Hp=30,IceBlock=8,Aether=3},Enemies=enemies}};g.Rebuild();yield return null;
    Time.captureFramerate=30;Presenter.MarkSeen(g.Rs.Combat);ElementBattleFx.FrameMasks.Clear();int from=g.Rs.Combat.EventLog.Count,shown=ResolvedCombatFx.Presented;
    for(int f=0;f<75;f++){
     if(f==15){g.PreferredTarget=0;var hc=g.Battle.CardAt(0);TempoRequire(hc!=null && hc.Playable,"review card not playable "+cards[c]);ExecuteEvents.Execute(hc.Rt.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);TempoRequire(g.Error==null,"review command "+g.Error);}
     Tooltip.Hide();yield return new WaitForEndOfFrame();
     var shot=ScreenCapture.CaptureScreenshotAsTexture();var bytes=shot.EncodeToPNG();Destroy(shot);
     File.WriteAllBytes(Path.Combine(frames,"frame_"+(output++).ToString("D4")+".png"),bytes);
     if(f==21||f==25||f==35)File.WriteAllBytes(Path.Combine(_dir,elements[c]+"-"+f+".png"),bytes);
     yield return null;
    }
    int mask=ElementBattleFx.FrameMasks.TryGetValue("element-"+elements[c]+"-8f",out var value)?value:0;
    var events=g.Rs.Combat.EventLog.Skip(from).Where(ResolvedCombatFx.IsResolved).ToList();
    bool passed=mask==255&&ResolvedCombatFx.Presented-shown==events.Count&&events.Count>0;
    report.Add(new JObject{{"element",elements[c]},{"card",cards[c]},{"leader",leaders[c]},{"frameMask",mask},{"events",new JArray(events.Select(e=>e.Type))},{"presented",ResolvedCombatFx.Presented-shown},{"passed",passed}});
    File.WriteAllText(Path.Combine(_dir,"element-review.json"),report.ToString());
    if(!passed){Debug.LogError("[ElementReview] failed "+elements[c]);Application.Quit(1);yield break;}
   }
   Time.captureFramerate=DetFramerate;
   Debug.Log("[ElementReview] PASS nine distinct sheets through normal card UI; all eight frames observed");
  }
 }
}
