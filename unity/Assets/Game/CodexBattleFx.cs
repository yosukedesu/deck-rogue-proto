// Generated atlas UI VFX, plus one resource-independent bounded receipt ring.
// Boolean results mean a viable render submission, never proof of GPU-visible pixels.
using System;
using UnityEngine;
using UnityEngine.UI;
namespace DeckRogue.Game {
 public static class CodexBattleFx {
  const int Capacity=24;
  sealed class Slot {public RawImage Image;public bool Busy;public int Version;public long AllocationSerial;}
  sealed class RingSlot {public Image Image;public bool Busy;public int Version;public long AllocationSerial;}
  static readonly Slot[] Pool=new Slot[Capacity];static readonly RingSlot[] Rings=new RingSlot[Capacity];
  static Texture2D _atlas;static Sprite _ring,_neutral;static bool _loaded;static long _allocationSerial;
  public static int Submitted {get;private set;}public static int Recycled {get;private set;}
  public static int RingSubmitted {get;private set;}public static int RingRecycled {get;private set;}
  public static int SurfaceRejected {get;private set;}public static string LastFailure {get;private set;}
  public static bool Ready {get{if(!_loaded){_loaded=true;_atlas=Resources.Load<Texture2D>("Art/fx/codex-vfx-atlas");}return _atlas!=null;}}
  public static int ActiveCount {get{int n=0;foreach(var s in Pool)if(s!=null&&s.Busy)n++;foreach(var s in Rings)if(s!=null&&s.Busy)n++;return n;}}
  public static bool SurfaceAvailable(RectTransform layer){
   LastFailure=null;
   if(!Tween.SchedulingAllowed){LastFailure="ui_fx_scope_cancelled";return false;}
   if(layer==null || !layer.gameObject.activeInHierarchy){LastFailure="ui_fx_layer_inactive_or_missing";SurfaceRejected++;return false;}
   var canvas=layer.GetComponentInParent<Canvas>();
   if(canvas==null || !canvas.isActiveAndEnabled || !canvas.rootCanvas.isActiveAndEnabled){LastFailure="ui_fx_canvas_inactive_or_missing";SurfaceRejected++;return false;}
   var root=canvas.rootCanvas;
   if(root.renderMode==RenderMode.ScreenSpaceCamera && (root.worldCamera==null || !root.worldCamera.isActiveAndEnabled)){
    LastFailure="ui_fx_camera_inactive_or_missing";SurfaceRejected++;return false;
   }
   return true;
  }
  static void Retire(Slot s,int version){if(s.Version!=version)return;s.Busy=false;if(s.Image!=null)s.Image.gameObject.SetActive(false);}
  static void Retire(RingSlot s,int version){if(s.Version!=version)return;s.Busy=false;if(s.Image!=null)s.Image.gameObject.SetActive(false);}
  static void CancelSlot(Slot s){if(s==null)return;Retire(s,s.Version);s.Version++;}
  static void CancelSlot(RingSlot s){if(s==null)return;Retire(s,s.Version);s.Version++;}
  public static void Cancel(){foreach(var s in Pool)CancelSlot(s);foreach(var s in Rings)CancelSlot(s);}
  // Exact object ownership only, never a name prefix that could preserve unrelated UI.
  public static bool Owns(Transform child){
   if(child==null)return false;
   foreach(var s in Pool)if(s?.Image!=null && s.Image.transform==child)return true;
   foreach(var s in Rings)if(s?.Image!=null && s.Image.transform==child)return true;
   return false;
  }
  static int Limit(){int limit=GfxQuality.Level==GfxLevel.Low?12:Capacity;
   for(int i=limit;i<Capacity;i++){if(Pool[i]?.Busy==true)CancelSlot(Pool[i]);if(Rings[i]?.Busy==true)CancelSlot(Rings[i]);}return limit;}
  static bool Emit(RectTransform layer,Vector2 pos,int cell,Color color,float size,float duration,float angle,float sweep,float from,float to,float opacity=1f){
   if(!SurfaceAvailable(layer))return false;
   if(!Ready){LastFailure="ui_fx_atlas_missing";return false;}
   int limit=Limit();Slot slot=null;
   for(int i=0;i<limit;i++){if(Pool[i]==null)Pool[i]=new Slot();if(Pool[i].Image==null || !Pool[i].Busy){slot=Pool[i];break;}}
   if(slot==null){slot=Pool[0];for(int i=1;i<limit;i++)if(Pool[i].AllocationSerial<slot.AllocationSerial)slot=Pool[i];Recycled++;}
   Retire(slot,slot.Version);slot.Version++;slot.AllocationSerial=++_allocationSerial;
   if(slot.Image==null){var rt=UiKit.NewRect("codex-vfx",layer);slot.Image=rt.gameObject.AddComponent<RawImage>();}
   var image=slot.Image;var rect=image.rectTransform;
   rect.SetParent(layer,false);rect.SetAsLastSibling();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);
   rect.anchoredPosition=pos;rect.sizeDelta=Vector2.one*size;image.texture=_atlas;image.enabled=true;image.raycastTarget=false;
   float pad=.5f/_atlas.width;image.uvRect=new Rect((cell%2)*.5f+pad,cell<2?.5f+pad:pad,.5f-2*pad,.5f-2*pad);
   slot.Busy=true;int version=slot.Version;rect.gameObject.SetActive(true);
   Action finish=()=>Retire(slot,version);
   Tween.Run(duration,t=>{
    if(rect==null || slot.Version!=version)return;
    float travel=1f-Mathf.Pow(1f-t,3f),scale=Mathf.Lerp(from,to,travel);
    rect.localScale=new Vector3(scale,scale,1);rect.localRotation=Quaternion.Euler(0,0,angle+sweep*travel);
    float attack=Mathf.Min(1f,t/.055f),fade=1f-Mathf.SmoothStep(.12f,1f,t);
    image.color=new Color(color.r,color.g,color.b,color.a*opacity*attack*fade);
   },Ease.Linear,finish,finish,()=>slot.Version==version && rect!=null && image!=null);
   bool accepted=slot.Busy && slot.Version==version;
   if(accepted)Submitted++;else LastFailure="ui_fx_schedule_retired";return accepted;
  }
  public static bool Slash(RectTransform layer,Vector2 pos,float angle,Color color,bool big,float scale){
   if(!Ready){LastFailure="ui_fx_atlas_missing";return false;}
   float size=(big?330f:250f)*scale;
   bool body=Emit(layer,pos,0,Color.Lerp(color,Color.white,.35f),size,.25f,angle-65f,48f,.62f,1.1f);
   bool core=Emit(layer,pos,1,color,big?140f:100f,.16f,angle,8f,.45f,1.1f,.86f);
   return body||core; // Both layers execute; no short-circuit emission or false success.
  }
  public static bool Spell(RectTransform layer,Vector2 pos,Color color,bool big,float scale,bool spark=false){
   if(!Ready){LastFailure="ui_fx_atlas_missing";return false;}
   bool body=Emit(layer,pos,2,color,(big?280f:215f)*scale,spark?.23f:.4f,-18,35,.25f,1.18f,.85f);
   bool core=Emit(layer,pos,1,Color.Lerp(color,Color.white,.5f),big?125f:90f,.15f,20,-10,.3f,1);
   return body||core;
  }
  public static bool Guard(RectTransform layer,Vector2 pos,float side)=>Emit(layer,pos+new Vector2(48f*side,4),3,new Color(.7f,.9f,1,1),195,.32f,side<0?180:0,0,.72f,1.04f);
  public static bool Cast(RectTransform layer,Vector2 pos,Color color)=>Emit(layer,pos+new Vector2(26,12),2,color,125,.3f,0,-40,1,.25f,.7f);
  public static bool ReceiptFallback(RectTransform layer,Vector2 pos,Color color,float size,float duration)=>SpriteReceiptFallback(layer,pos,color,size,duration,false);
  // Existing achromatic soft glow carrier. No ring, slash, shield, material or weapon implication.
  public static bool NeutralReceiptFallback(RectTransform layer,Vector2 pos,float size,float duration)=>SpriteReceiptFallback(layer,pos,new Color(.82f,.82f,.82f,1f),size,duration,true);
  static bool SpriteReceiptFallback(RectTransform layer,Vector2 pos,Color color,float size,float duration,bool neutral){
   if(!SurfaceAvailable(layer))return false;
   if(neutral){if(_neutral==null)_neutral=ThemeFx.Glow();}else if(_ring==null)_ring=ThemeFx.Ring();
   var sprite=neutral?_neutral:_ring;
   if(sprite==null || sprite.texture==null){LastFailure=neutral?"ui_neutral_core_unavailable":"ui_receipt_ring_unavailable";return false;}
   int limit=Limit();RingSlot slot=null;
   for(int i=0;i<limit;i++){if(Rings[i]==null)Rings[i]=new RingSlot();if(Rings[i].Image==null || !Rings[i].Busy){slot=Rings[i];break;}}
   if(slot==null){slot=Rings[0];for(int i=1;i<limit;i++)if(Rings[i].AllocationSerial<slot.AllocationSerial)slot=Rings[i];RingRecycled++;}
   Retire(slot,slot.Version);slot.Version++;slot.AllocationSerial=++_allocationSerial;
   if(slot.Image==null){var rt=UiKit.NewRect("resolved-receipt-ring",layer);slot.Image=rt.gameObject.AddComponent<Image>();}
   var image=slot.Image;var rect=image.rectTransform;rect.SetParent(layer,false);rect.SetAsLastSibling();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);
   rect.anchoredPosition=pos;rect.sizeDelta=Vector2.one*size;rect.localRotation=Quaternion.identity;image.sprite=sprite;image.preserveAspect=true;image.enabled=true;image.raycastTarget=false;
   slot.Busy=true;int version=slot.Version;rect.gameObject.SetActive(true);Action finish=()=>Retire(slot,version);
   Tween.Run(duration,k=>{if(rect==null || slot.Version!=version)return;rect.localScale=Vector3.one*(neutral?.88f+.12f*Mathf.Sin(k*Mathf.PI):.3f+k);image.color=new Color(color.r,color.g,color.b,color.a*(1-k*k));},Ease.OutQuad,finish,finish,()=>slot.Version==version && rect!=null && image!=null);
   bool accepted=slot.Busy && slot.Version==version;
   if(accepted)RingSubmitted++;else LastFailure="ui_receipt_schedule_retired";return accepted;
  }
 }
}
