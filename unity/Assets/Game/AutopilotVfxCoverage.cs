using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DeckRogue.Game {
 public partial class Autopilot {
  IEnumerator VfxCoverage(GameRoot g) {
   var report=new JArray();
   var cases=new[]{"single","fire","multi","aoe","iceScale","drain","aether","conditional","guard","zero","enemy","enemyGuard","burn","thorns","cost","cancel","lethal","doll","scald","reaction","heal","missingTarget","unknownProfile","fusion","proceduralFusion"};
   foreach(var name in cases) {
    string card=name=="heal"?"white_mass_heal":name=="fire"?"red_strike":name=="multi"?"red_double_tap":name=="aoe"||name=="conditional"?"green_earth_roar":name=="iceScale"||name=="zero"?"blue_ice_lance":name=="drain"?"black_drain":name=="aether"?"blue_aether_burst":name=="cost"?"black_dark_pact":"green_strike";
    yield return StateJump(g,"phase=combat;leader=leader_green;enemy="+(name=="aoe"||name=="cancel"?"enc_biting_scrolls_quad":"enemy_probe")+";hand="+card+";penergy=20;dumplayout=1");
    var st=g.Rs.Combat;var foes=st.Enemies.ToList();
    foes[0]=foes[0] with {Block=name=="guard"?100:0,Hp=name=="lethal"?1:foes[0].Hp,Burn=name=="burn"?5:0,Thorns=name=="thorns"?3:0};
    var pl=st.Player with {Hp=30,IceBlock=name=="iceScale"?8:0,Aether=3,Block=name=="enemyGuard"?100:0,EnergyMax=name=="conditional"?5:st.Player.EnergyMax};
    if(name=="doll")pl=pl with {Permanents=new[]{new CardInstance{Uid="vfx-doll",Def=Content.GetCardDef("white_perm_squire")}}};
    if(name=="fusion")pl=pl with {Hand=new[]{new CardInstance{Uid="vfx-fusion",Def=Content.AllFusions.First(x=>x.Result.Id=="fusion_vine_wheel").Result}}};
    if(name=="proceduralFusion")pl=pl with {Hand=new[]{new CardInstance{Uid="vfx-procedural",Def=Fusion.FuseCards(new CardInstance{Uid="a",Def=Content.GetCardDef("blue_strike")},new CardInstance{Uid="b",Def=Content.GetCardDef("blue_ice_lance")})}}};
    if(name=="unknownProfile")pl=pl with {Hand=new[]{new CardInstance{Uid="vfx-unknown",Def=Content.GetCardDef("blue_strike") with {Id="future_unknown_attack"}}}};
    if(name=="scald")pl=pl with {Hand=new[]{new CardInstance{Uid="vfx-scald",Def=Content.SCALD_DEF}}};
    if(name=="reaction")pl=pl with {SetCards=new[]{new CardInstance{Uid="vfx-reaction",Def=Content.GetCardDef("green_reaction_preempt"),SetTurn=st.Turn-1}}};
    g.Rs=g.Rs with {Combat=st with {Player=pl,Enemies=foes,ReactionMode=ReactionModes.SetAuto}};g.Rebuild();yield return null;
    Presenter.MarkSeen(g.Rs.Combat);int from=g.Rs.Combat.EventLog.Count, received=ResolvedCombatFx.Received,shown=ResolvedCombatFx.Presented,duplicates=ResolvedCombatFx.DuplicateSuppressed;
    string error=null;
    try {
     if(name=="cancel"){g.PreferredTarget=-1;g.BeginPlay(g.Rs.Combat.Player.Hand[0],null);TempoRequire(g.Pending!=null,"cancel must open selection");g.CancelPending();}
     else if(name=="enemy"||name=="enemyGuard"||name=="burn"||name=="doll"||name=="scald"||name=="reaction")g.DoCombat(new Command_EndTurn());
     else {g.PreferredTarget=0;var hc=g.Battle.CardAt(0);ExecuteEvents.Execute(hc.Rt.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);}
     if(name=="missingTarget")g.Battle.KillEnemy(g,0,false);
     TempoRequire(g.Error==null,"command error "+g.Error);
     TempoRequire(GameObject.Find("inputblock")==null && GameObject.Find("inputblock2")==null,"input lock");
    }catch(Exception e){error=e.ToString();}
    var events=g.Rs.Combat.EventLog.Skip(from).Where(ResolvedCombatFx.IsResolved).ToList();
    int frames=0;do {yield return null;frames++;}while((Presenter.PendingImpactCount>0||Presenter.PendingVisualCount>0||WorldBattleFx.ActiveCount>0)&&frames<360);
    int actual=ResolvedCombatFx.Presented-shown;
    bool expectedEvents=name=="cancel"?events.Count==0:events.Count>0;
    bool pass=error==null&&expectedEvents&&actual==events.Count&&ResolvedCombatFx.Received-received==events.Count&&ResolvedCombatFx.DuplicateSuppressed==duplicates&&frames<360;
    report.Add(new JObject{{"case",name},{"card",card},{"passed",pass},{"events",new JArray(events.Select(e=>e.Type))},{"expected",events.Count},{"presented",actual},{"received",ResolvedCombatFx.Received-received},{"drainFrames",frames},{"pendingVisuals",Presenter.PendingVisualCount},{"pendingImpacts",Presenter.PendingImpactCount},{"activeFx",WorldBattleFx.ActiveCount},{"drainBudgetFrames",360},{"error",error}});
    File.WriteAllText(Path.Combine(_dir,"vfx-coverage.json"),report.ToString());
    if(!pass){Debug.LogError("[VfxCoverage] failed "+name+" "+report.Last);Application.Quit(1);yield break;}
   }
   TempoRequire(CardVfxProfile.Style(new CardDef{Id="future_unknown",Type="spell"})=="spell","unknown spell fallback");
   TempoRequire(CardVfxProfile.Style(new CardDef{Id="future_unknown",Type="physical"})=="slash","unknown physical fallback");
   int fireMask=ElementBattleFx.FrameMasks.TryGetValue("element-fire-8f",out var mask)?mask:0;
   if(fireMask!=255){Debug.LogError("[VfxCoverage] fire frames missing mask="+fireMask);Application.Quit(1);yield break;}
   File.WriteAllText(Path.Combine(_dir,"vfx-frame-check.json"),new JObject{{"fireMask",fireMask},{"firstFrame",ElementBattleFx.Frame(0)},{"lastFrame",ElementBattleFx.Frame(.999f)}}.ToString());
   var known=Content.AllCards.Concat(Content.AllFusions.Select(x=>x.Result)).Concat(new[]{Content.WoundDef,Content.ScaldDef,Content.BrandDef,Content.GuiltDef,Content.JunkDef}).ToList();
   var missing=known.Where(c=>!CardVfxProfile.Contains(c.Id)).Select(c=>c.Id).ToArray();
   File.WriteAllText(Path.Combine(_dir,"vfx-profile-check.json"),new JObject{{"cards",known.Count},{"missing",new JArray(missing)}}.ToString());
   if(missing.Length>0){Debug.LogError("[VfxCoverage] missing profiles");Application.Quit(1);yield break;}
   Debug.Log("[VfxCoverage] PASS resolved engine events through actual UI commands; not exhaustive all-card execution");
  }
 }
}
