using System.Collections;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace DeckRogue.Game {
 public partial class Autopilot {
  static JArray IdleBoxes(){var a=new JArray();foreach(var item in (List<object>)Stage.DebugUnitBoxes()){var d=(Dictionary<string,object>)item;var o=new JObject();foreach(var key in new[]{"key","anim","art","frameScale","rectPx","boardRawPx","combatOffset","recoilOffset","pxPerDot","feetPadPx","breathePx","heightWorld"})if(d.TryGetValue(key,out var v)){if(v is Vector2 v2)o[key]=new JArray(v2.x,v2.y);else if(v is Vector4 v4)o[key]=new JArray(v4.x,v4.y,v4.z,v4.w);else o[key]=JToken.FromObject(v);}a.Add(o);}return a;}
  IEnumerator IdleAnchorReview(GameRoot g){
   yield return StateJump(g,"phase=combat;leader=leader_blue;enemy=enemy_probe;hand=blue_strike;dumplayout=1");Presenter.MarkSeen(g.Rs.Combat);
   Time.captureFramerate=60;var rows=new JArray();var imports=new JArray();
   for(int i=0;i<8;i++){var spr=Theme.Art("leaders/anim","leader_blue_idle_"+i);imports.Add(new JObject{{"frame",i},{"width",spr.texture.width},{"height",spr.texture.height},{"pivot",new JArray(spr.pivot.x,spr.pivot.y)},{"rect",new JArray(spr.rect.x,spr.rect.y,spr.rect.width,spr.rect.height)}});}
   string previous=null;int observed=0;
   for(int f=0;f<145;f++){
    yield return new WaitForEndOfFrame();string anim=Stage.DebugAnim("player").Split(' ')[0];
    if(anim!=previous){previous=anim;Stage.TryGetUnitBox("player",out var feet,out var height);rows.Add(new JObject{{"frame",f},{"anim",anim},{"feet",new JArray(feet.x,feet.y,feet.z)},{"height",height},{"boxes",IdleBoxes()}});var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(_dir,"idle-"+observed.ToString("D2")+".png"),shot.EncodeToPNG());Destroy(shot);observed++;}
    yield return null;
   }
   File.WriteAllText(Path.Combine(_dir,"idle-runtime.json"),new JObject{{"imports",imports},{"observations",rows}}.ToString());Time.captureFramerate=DetFramerate;
  }
 }
}
