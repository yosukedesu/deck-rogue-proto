using System;
using System.Collections;
using System.IO;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace DeckRogue.Game {
 public partial class Autopilot {
  IEnumerator VfxSafety(GameRoot g){
   var report=new JArray();bool luminousTest=Arg("-vfx-safety-element")=="light";
   foreach(var name in new[]{"normal","unavailable","skippedPass","missingSheets","cameraPause","cameraSwap","occluded","uiOverlay","boardSwitch"}){
    ElementVfxPass.ForceUnavailableForTest=false;ElementVfxPass.ForceSkipForTest=false;ElementBattleFx.ForceMissingForTest=false;ElementVfxPass.ResetTestFault();
    yield return StateJump(g,luminousTest?"phase=combat;leader=leader_white;enemy=enemy_probe;hand=white_strike;penergy=10;dumplayout=1":"phase=combat;leader=leader_red;enemy=enemy_probe;hand=red_strike;penergy=10;dumplayout=1");
    Presenter.MarkSeen(g.Rs.Combat);Time.captureFramerate=30;
    ElementVfxPass.ForceUnavailableForTest=name=="unavailable";ElementVfxPass.ForceSkipForTest=name=="skippedPass";ElementBattleFx.ForceMissingForTest=name=="missingSheets";
    int luminousBefore=WorldBattleFx.LuminousEmittedCount;
    int received=ResolvedCombatFx.Received,shown=ResolvedCombatFx.Presented,drawn=ElementVfxPass.DrawnFrames,legacy=WorldBattleFx.LegacyElementUses,emitted=WorldBattleFx.EmittedCount;
    Stage.TryGetUnitBox("enemy0",out var feet,out var height);var target=feet+(Stage.LayoutRotation*Vector3.up)*height*.5f;
    var camera=Stage.Camera;var screen=camera.WorldToScreenPoint(target);GameObject cover=null;Material coverMaterial=null;
    if(name=="occluded"){
     cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.name="vfx-test-occluder";
     cover.transform.position=Vector3.Lerp(camera.transform.position,target,.8f);cover.transform.rotation=camera.transform.rotation;cover.transform.localScale=new Vector3(height*2,height*2,.2f);
     coverMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));coverMaterial.SetColor("_BaseColor",new Color(.03f,.045f,.06f));cover.GetComponent<Renderer>().sharedMaterial=coverMaterial;
    }
    if(name=="uiOverlay"){
     cover=new GameObject("vfx-test-overlay",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));var rt=cover.GetComponent<RectTransform>();rt.SetParent(g.FxLayer,false);
     RectTransformUtility.ScreenPointToLocalPointInRectangle(g.FxLayer,screen,null,out var local);rt.anchoredPosition=local;rt.sizeDelta=new Vector2(210,210);cover.GetComponent<Image>().color=new Color(.15f,.04f,.3f,1);rt.SetAsLastSibling();
    }
    Camera originalCamera=camera;GameObject replacementCamera=null;var cameraField=typeof(Stage).GetField("_cam",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
    Color before=Color.clear,after=Color.clear;
    for(int f=0;f<45;f++){
     if(f==10){g.PreferredTarget=0;var hc=g.Battle.CardAt(0);ExecuteEvents.Execute(hc.Rt.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);}
     if(name=="cameraSwap" && f==14){replacementCamera=UnityEngine.Object.Instantiate(camera.gameObject);camera=replacementCamera.GetComponent<Camera>();cameraField.SetValue(null,camera);originalCamera.enabled=false;camera.enabled=true;}
     if(name=="cameraSwap" && f==24){cameraField.SetValue(null,originalCamera);originalCamera.enabled=true;Destroy(replacementCamera);camera=originalCamera;}
     if(name=="cameraPause" && f==14)camera.enabled=false;
     if(name=="cameraPause" && f==16)camera.enabled=true;
     Tooltip.Hide();yield return new WaitForEndOfFrame();
     if(f==5||f==17){var shot=ScreenCapture.CaptureScreenshotAsTexture();Color sum=Color.clear;int x=Mathf.Clamp((int)screen.x,4,shot.width-5),y=Mathf.Clamp((int)screen.y,4,shot.height-5);for(int a=-3;a<=3;a++)for(int b=-3;b<=3;b++)sum+=shot.GetPixel(x+a,y+b);sum/=49;if(f==5)before=sum;else after=sum;File.WriteAllBytes(Path.Combine(_dir,name+(f==5?"-before":"-hit")+".png"),shot.EncodeToPNG());Destroy(shot);}
     if(name=="boardSwitch" && f==18){yield return StateJump(g,"act=2;phase=combat;enemy=enemy_probe;hand=green_strike;penergy=10;dumplayout=1");camera=Stage.Camera;}
     yield return null;
    }
    if(cover!=null)Destroy(cover);if(coverMaterial!=null)Destroy(coverMaterial);if(camera!=null)camera.enabled=true;
    int post=ElementVfxPass.DrawnFrames-drawn,fallback=WorldBattleFx.LegacyElementUses-legacy;float delta=Mathf.Max(Mathf.Abs(after.r-before.r),Mathf.Abs(after.g-before.g),Mathf.Abs(after.b-before.b));
    bool pass=g.Error==null && ResolvedCombatFx.Received-received==1 && ResolvedCombatFx.Presented-shown==1 && WorldBattleFx.EmittedCount>emitted;
    if(name=="unavailable"||name=="skippedPass")pass&=post==0 && fallback>0;
    else if(name=="missingSheets")pass&=post==0;
    else pass&=post>0;
    if(name=="occluded"||name=="uiOverlay")pass&=delta<.025f;
    if(name=="boardSwitch")pass&=WorldBattleFx.ActiveCount==0 && Presenter.PendingImpactCount==0;
    report.Add(new JObject{{"splitLuminous",luminousTest},{"luminousEmissions",WorldBattleFx.LuminousEmittedCount-luminousBefore},{"case",name},{"passed",pass},{"presented",ResolvedCombatFx.Presented-shown},{"postFrames",post},{"legacySwitches",fallback},{"sampleMaxRgbDelta",delta},{"fallbackReason",ElementVfxPass.FallbackReason},{"activeVfxAtEnd",WorldBattleFx.ActiveCount}});
    File.WriteAllText(Path.Combine(_dir,"vfx-safety.json"),report.ToString());
    if(!pass){Debug.LogError("[VfxSafety] failed "+report.Last);Application.Quit(1);yield break;}
   }
   ElementVfxPass.ForceUnavailableForTest=false;ElementVfxPass.ForceSkipForTest=false;ElementBattleFx.ForceMissingForTest=false;ElementVfxPass.ResetTestFault();Time.captureFramerate=DetFramerate;
   Debug.Log("[VfxSafety] PASS fallback, occlusion, overlay UI and camera/board lifecycle");
  }
 }
}
