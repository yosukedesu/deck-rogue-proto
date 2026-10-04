using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeckRogue.Game {
 public partial class Autopilot {
  static void TempoRequire(bool ok,string why){if(!ok)throw new Exception("[TempoCheck] "+why);}
  IEnumerator TempoCheck(GameRoot g) {
   var report=new JArray();
   var cases=new[]{"rapid5","sameTarget","deathMidChain","aoeThenSingle","insufficient","endTurn","dragCancel","selectionCancel"};
   foreach(var name in cases) {
    string hand=name=="aoeThenSingle"?"green_earth_roar,green_strike,green_strike":string.Join(",",Enumerable.Repeat("green_strike",5));
    string enemy=name=="deathMidChain"||name=="aoeThenSingle"||name=="selectionCancel"?"enc_biting_scrolls_quad":"enemy_probe";
    yield return StateJump(g,"phase=combat;enemy="+enemy+";hand="+hand+";penergy="+(name=="insufficient"?"1":"20")+(name=="deathMidChain"?";ehp=1":"")+";dumplayout=1");
    Presenter.MarkSeen(g.Rs.Combat);
    var row=new JObject{{"case",name},{"actions",new JArray()}};report.Add(row);var actions=(JArray)row["actions"];
    bool failed=false;string error=null;float visualStart=Time.realtimeSinceStartup;
    try {
     if(name=="dragCancel") {
      var hc=g.Battle.CardAt(0);var pd=new PointerEventData(EventSystem.current){position=new Vector2(500,10),button=PointerEventData.InputButton.Left};
      ExecuteEvents.Execute(hc.Rt.gameObject,pd,ExecuteEvents.beginDragHandler);ExecuteEvents.Execute(hc.Rt.gameObject,pd,ExecuteEvents.endDragHandler);
      var f=typeof(BattleScreen).GetField("_dragging",BindingFlags.NonPublic|BindingFlags.Static);
      TempoRequire(!(bool)f.GetValue(null) && g.Pending==null,"drag/selection not restored");
     }
     if(name=="selectionCancel") {g.PreferredTarget=-1;g.BeginPlay(g.Rs.Combat.Player.Hand[0],null);TempoRequire(g.Pending!=null,"target selection missing");g.CancelPending();TempoRequire(g.Pending==null,"cancel failed");}
     int count=name=="rapid5"||name=="sameTarget"?5:name=="deathMidChain"?4:name=="aoeThenSingle"?3:2;
     for(int n=0;n<count;n++) {
      var before=g.Rs;var st=before.Combat;var card=st.Player.Hand[0];int target=st.Enemies.ToList().FindIndex(e=>e.Hp>0);g.PreferredTarget=target;
      TempoRequire(GameObject.Find("inputblock")==null && GameObject.Find("inputblock2")==null,"visual blocked next card");
      var hc=g.Battle.CardAt(0);TempoRequire(hc!=null && hc.Card.Uid==card.Uid,"hand identity stale");
      bool can=hc.Playable;RunState expected=null;
      if(can)expected=DeckRogue.Engine.Run.ApplyRunCommand(before,new RunCommand_Combat{Command=new Command_PlayCard{CardUid=card.Uid,TargetIndex=target}});
      int frame=Time.frameCount;var clock=System.Diagnostics.Stopwatch.StartNew();
      ExecuteEvents.Execute(hc.Rt.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
      clock.Stop();TempoRequire(Time.frameCount==frame,"card waited for animation");
      if(can){TempoRequire(g.Error==null,"play failed: "+g.Error);TempoRequire(!g.Rs.Combat.Player.Hand.Any(c=>c.Uid==card.Uid),"card not consumed synchronously");TempoRequire(g.Rs.Combat.Player.Energy==expected.Combat.Player.Energy,"energy mismatch");TempoRequire(g.Rs.Combat.Enemies.Select(e=>e.Hp).SequenceEqual(expected.Combat.Enemies.Select(e=>e.Hp)),"HP/order mismatch");}
      else TempoRequire(ReferenceEquals(before,g.Rs),"illegal play mutated state");
      TempoRequire(g.Pending==null,"pending target stuck");TempoRequire(Time.timeScale==1,"global hitstop");
      actions.Add(new JObject{{"index",n},{"accepted",can},{"inputToEffectMs",clock.Elapsed.TotalMilliseconds},{"sameFrame",Time.frameCount==frame},{"pendingVisuals",Presenter.PendingVisualCount},{"target",target},{"hp",new JArray(g.Rs.Combat.Enemies.Select(e=>e.Hp))}});
     }
     if(name=="deathMidChain")TempoRequire(g.Rs.Combat.Enemies[0].Hp<=0 && !g.Anchor("enemy0").GetComponent<UnityEngine.UI.Button>().interactable,"dead target remains clickable");
     if(name=="endTurn"){g.DoCombat(new Command_EndTurn());TempoRequire(GameObject.Find("inputblock")==null,"end turn presentation lock");TempoRequire(g.Rs.Combat.Phase==CombatPhases.PlayerTurn,"turn did not resolve");}
    }catch(Exception e){failed=true;error=e.ToString();Debug.LogError(e);}
    int frames=0;
    do {yield return null;frames++;} while((Presenter.PendingVisualCount>0 || WorldBattleFx.ActiveCount>0 || Stage.HasCombatMotion) && frames<150);
    row["visualCompletionMs"]=1000*(Time.realtimeSinceStartup-visualStart);row["visualFrames"]=frames;row["visualsDrained"]=frames<150;row["passed"]=!failed && frames<150;row["error"]=error;
    File.WriteAllText(Path.Combine(_dir,"tempo-check.json"),report.ToString());
    if(failed||frames>=150){Application.Quit(1);yield break;}
   }
   Debug.Log("[TempoCheck] PASS 8 scenarios: synchronous UI card effects, bounded independent visuals");
  }
 }
}
