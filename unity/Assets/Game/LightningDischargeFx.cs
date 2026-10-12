using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeckRogue.Game {
 // Receipt-owned local R2 discharge. Source-family routing is owned by the host.
 // Fixed +1 orientation preserves the decorative leader; this is not world transport.
 internal static class LightningDischargeFx {
  sealed class Draw { public Mesh Mesh; public MeshRenderer Renderer; public Vector3[] Vertices; public Vector2[] UV; public Color[] Colors; }
  sealed class Set { public Transform Root; public Draw[] Draws; public LightningDischargeGeometry.Buffer Geometry; public WorldBattleFx.AttackSpec LightSpec; }
  sealed class Slot {
   public Transform Root; public Set Active; public bool Busy,Landed; public int Version; public long Serial;
   public float Started,Expected,Available,Height,Size,Emission,Particles,LightPower; public string Tier;
   public StageFx.ContractLightPlayback Light;
  }
  static readonly Slot[] Pool=new Slot[8];
  static int _epoch; static long _serial; static Material _material; static Texture2D _texture;
  static Color[] _roles; static Color _lightColor;
  static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  public static string LastFailure { get; private set; }
  public static int Submitted { get; private set; }
  public static int Recycled { get; private set; }
  public static int StaleContacts { get; private set; }
  public static int ActiveCount { get { int n=0; foreach(var s in Pool)if(s!=null&&s.Busy)n++; return n; } }
  public static bool IsFamily(string family)=>family=="lightning_discharge";
  public static bool Eligible(ResolvedCombatFx.Ticket t)=>t!=null&&IsFamily(t.AttackFamily)&&t.Element=="lightning";
  public static float Lead(ResolvedCombatFx.Ticket t)=>Eligible(t)?(float)LightningDischargeGeometry.Lead():0f;
  static Color Role(JToken token) {
   if(token==null||!ColorUtility.TryParseHtmlString((string)token,out var c))throw new FormatException("invalid lightning role");
   return c.linear; // Palette/scene sRGB decode once. Texture import performs its own decode once.
  }
  static Vector4 Vector(Color c)=>new Vector4(c.r,c.g,c.b,1);
  static bool Finite(float x)=>LightningDischargeGeometry.Finite(x);
  static bool Finite(Vector3 p)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z);
  // Diagnostic observers are installed only by the explicit R9 review scenario.
  // The original gate evaluates first; observers cannot change its result/fallback.
  internal static Action<Shader,Texture2D,TextAsset,bool> ReadinessForReview;
  static void ObserveReadiness(Shader shader,Texture2D texture,TextAsset palette,bool accepted) {
   var observer=ReadinessForReview;if(observer==null)return;
   try{observer(shader,texture,palette,accepted);}catch(Exception){/* Observation must not alter rendering. */}
  }
  public static bool Ready() {
   LastFailure=null;
   if(!Tween.SchedulingAllowed){LastFailure="lightning_discharge_scope_cancelled";return false;}
   if(!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){LastFailure="lightning_discharge_world_camera_unavailable";return false;}
   if(ElementBattleFx.ForceMissingForTest){LastFailure="lightning_discharge_forced_resource_missing";return false;}
   if(QualitySettings.activeColorSpace!=ColorSpace.Linear){LastFailure="lightning_discharge_requires_linear_project";return false;}
   if(_material!=null&&_texture!=null)return true;
   var shader=Shader.Find("DeckRogue/LightningDischargeFx");
   var texture=Resources.Load<Texture2D>("Art/fx/lightning-discharge-surface-r2");
   var palette=Resources.Load<TextAsset>("Data/lightning-discharge-palette-r2");
   if(shader==null||!shader.isSupported||texture==null||texture.width!=1774||texture.height!=887||palette==null){ObserveReadiness(shader,texture,palette,false);LastFailure="lightning_discharge_shader_or_resource_unavailable";return false;}
   ObserveReadiness(shader,texture,palette,true);
   Material material=null;
   try {
    var j=JObject.Parse(palette.text);var roles=j["roles_srgb"];
    var colors=new[]{Role(roles?["shadow"]),Role(roles?["primary"]),Role(roles?["edge"]),Role(roles?["core"])};
    var light=Role(j["scene_light_srgb"]);
    material=new Material(shader);material.SetTexture("_BaseMap",texture);
    material.SetVector("_RoleShadow",Vector(colors[0]));material.SetVector("_RolePrimary",Vector(colors[1]));
    material.SetVector("_RoleEdge",Vector(colors[2]));material.SetVector("_RoleCore",Vector(colors[3]));
    _material=material;_texture=texture;_roles=colors;_lightColor=light;return true;
   } catch(Exception e) {
    if(material!=null)UnityEngine.Object.Destroy(material);
    LastFailure="lightning_discharge_palette_invalid:"+e.GetType().Name;return false;
   }
  }
  // Proposed bounded Unity mapping, not calibrated Blender-W to Unity intensity equivalence.
  // Actual Land owns the only light. Native decorative prelight is intentionally zero here.
  static WorldBattleFx.AttackSpec Spec()=>new WorldBattleFx.AttackSpec(
   new[]{0f,0f,0f,0f,0f,0f,0f,.075f,(float)LightningDischargeGeometry.End()},1,1,1,_lightColor,_lightColor,.015f,0f,0f) {
    ContractLighting=true,StandardContractPower=true,
    TargetPeakMultiplier=.35f*(42f/55f),FloorPeakMultiplier=.12f*(14f/20f),
    MaxCombineGroup="lightning-discharge-continuous-r2",
    Envelope=age=>age<0f||age>.075f?0f:1f-(float)LightningDischargeGeometry.Smooth((age-.015)/.060)
   };
  static void DestroySet(Set s) {
   if(s==null)return;
   if(s.Draws!=null)foreach(var d in s.Draws)if(d?.Mesh!=null)UnityEngine.Object.Destroy(d.Mesh);
   if(s.Root!=null){s.Root.gameObject.SetActive(false);UnityEngine.Object.Destroy(s.Root.gameObject);}
  }
  static Draw CreateDraw(Transform root,int index,LightningDischargeGeometry.MeshData data) {
   var go=new GameObject("lightning-part-"+index);Mesh mesh=null;
   try {
    go.transform.SetParent(root,false);mesh=new Mesh{name="lightning-discharge-continuous-r2-"+index};mesh.MarkDynamic();
    var d=new Draw{Mesh=mesh,Vertices=new Vector3[data.Vertices.Length],UV=new Vector2[data.Vertices.Length],Colors=new Color[data.Vertices.Length]};
    mesh.vertices=d.Vertices;mesh.uv=d.UV;mesh.colors=d.Colors;mesh.triangles=data.Triangles;
    go.AddComponent<MeshFilter>().sharedMesh=mesh;d.Renderer=go.AddComponent<MeshRenderer>();
    d.Renderer.sharedMaterial=_material;d.Renderer.sortingOrder=index;d.Renderer.shadowCastingMode=ShadowCastingMode.Off;
    d.Renderer.receiveShadows=false;d.Renderer.enabled=false;return d;
   } catch {
    // Own even a mesh that failed before this Draw could be installed into its Set.
    if(mesh!=null)UnityEngine.Object.Destroy(mesh);go.SetActive(false);UnityEngine.Object.Destroy(go);throw;
   }
  }
  static Set CreateSet(Transform parent) {
   var root=new GameObject("lightning-discharge").transform;
   var set=new Set{Root=root};
   try {
    root.SetParent(parent,false);set.Geometry=LightningDischargeGeometry.Create(1);
    set.Draws=new Draw[set.Geometry.Batches.Length];set.LightSpec=Spec();
    for(int i=0;i<set.Draws.Length;i++)set.Draws[i]=CreateDraw(root,i,set.Geometry.Batches[i]);
    root.gameObject.SetActive(false);return set;
   } catch { DestroySet(set);throw; }
  }
  static void Retire(Slot s,int version) {
   if(s.Version!=version)return;s.Busy=false;s.Light?.Cancel();s.Light=null;
   if(s.Root!=null)s.Root.gameObject.SetActive(false);
  }
  public static void Cancel() { _epoch++;foreach(var s in Pool)if(s!=null){Retire(s,s.Version);s.Version++;} }
  static Slot Acquire() {
   int limit=GfxQuality.Level==GfxLevel.Low?4:8;Slot s=null;
   for(int i=0;i<limit;i++){if(Pool[i]==null)Pool[i]=new Slot();if(!Pool[i].Busy||Pool[i].Root==null){s=Pool[i];break;}}
   if(s==null){s=Pool[0];for(int i=1;i<limit;i++)if(Pool[i].Serial<s.Serial)s=Pool[i];Recycled++;}
   Retire(s,s.Version);s.Version++;s.Serial=++_serial;
   if(s.Root==null){DestroySet(s.Active);s.Active=null;s.Root=new GameObject("lightning-discharge-continuous-r2").transform;}
   if(s.Active==null)s.Active=CreateSet(s.Root);
   s.Active.Root.gameObject.SetActive(true);return s;
  }
  static bool Current(Slot s,int version,int epoch)=>epoch==_epoch&&s.Version==version&&s.Busy&&s.Root!=null;
  static void DrawAt(Slot s,float age,bool validateUpload=false) {
   var geometry=s.Active.Geometry;
   LightningDischargeGeometry.SampleReceipt(geometry,age,s.Available);
   for(int j=0;j<s.Active.Draws.Length;j++) {
    var d=s.Active.Draws[j];var data=geometry.Batches[j];bool visible=false;
    var lo=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue);var hi=new Vector3(float.MinValue,float.MinValue,float.MinValue);
    // Native gains are applied in the shader only, including 2.35 inside frozen LightningFragment.
    float hostGain=data.Kind==LightningDischargeGeometry.SparkKind?s.Particles:s.Emission;
    for(int i=0;i<data.Vertices.Length;i++) {
     var p=data.Vertices[i];var v=new Vector3((float)p.X,(float)p.Y,-(float)p.Z);
     if(!Finite(v))throw new InvalidOperationException("nonfinite_gpu_vertex");
     d.Vertices[i]=v;d.UV[i]=new Vector2((float)data.UV[i].X,(float)data.UV[i].Y);
     float alpha=(float)data.Alpha[i];d.Colors[i]=new Color(hostGain,hostGain,hostGain,alpha);visible|=alpha>0f;
     lo=new Vector3(Mathf.Min(lo.x,v.x),Mathf.Min(lo.y,v.y),Mathf.Min(lo.z,v.z));
     hi=new Vector3(Mathf.Max(hi.x,v.x),Mathf.Max(hi.y,v.y),Mathf.Max(hi.z,v.z));
    }
    d.Renderer.enabled=visible;if(!visible&&!validateUpload)continue;
    d.Mesh.SetVertices(d.Vertices);d.Mesh.SetUVs(0,d.UV);d.Mesh.SetColors(d.Colors);
    d.Mesh.bounds=new Bounds((lo+hi)*.5f,hi-lo+Vector3.one*.001f);
    Props.Clear();Props.SetFloat("_Kind",data.Kind);Props.SetFloat("_Age",(float)geometry.VisualAge);d.Renderer.SetPropertyBlock(Props);
   }
  }
  static void Hide(Slot s) { foreach(var d in s.Active.Draws)d.Renderer.enabled=false; }
  static IEnumerator Life(Slot s,int version,int epoch) {
   try {
    while(Current(s,version,epoch)) {
     if(s.Landed){float age=Time.time-s.Started;if(age>=LightningDischargeGeometry.End())yield break;DrawAt(s,age);}
     else {float age=Time.time-s.Expected;if(age>=.35f)yield break;if(age<0)DrawAt(s,age);else Hide(s);}
     yield return null;
    }
   } finally { if(epoch==_epoch)Retire(s,version); }
  }
  public static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket ticket,float? contactAt=null) {
   if(!Eligible(ticket)||!ticket.HasWorld||ticket.Outcome!="hit"||ticket.Amount<=0)return null;
   if(contactAt.HasValue&&!Finite(contactAt.Value)){ticket.RenderFallbackReason="lightning_discharge_invalid_contact_clock";return null;}
   if(!Finite(ticket.ContactVisibleHeight)||ticket.ContactVisibleHeight<=0f||!Finite(ticket.Position)||!Finite(ticket.Feet)){ticket.RenderFallbackReason="lightning_discharge_invalid_captured_geometry";return null;}
   var visual=ticket.Visual;float size=visual?.Size??1f;float scale=ticket.ContactVisibleHeight*size;
   if(!Finite(scale)||scale<=0f){ticket.RenderFallbackReason="lightning_discharge_invalid_composed_scale";return null;}
   if(!Ready()){ticket.RenderFallbackReason=LastFailure;return null;}
   // Capture once. Later camera, actor canvas, or source movement cannot retarget the discharge.
   var cameraRotation=Stage.Camera.transform.rotation;var position=ticket.Position;var feet=ticket.Feet;var height=ticket.ContactVisibleHeight;
   Slot s=null;
   try {
    s=Acquire();s.Busy=true;s.Landed=false;s.Height=height;s.Size=size;s.Expected=contactAt??Time.time;
    s.Available=Mathf.Min((float)LightningDischargeGeometry.Lead(),Mathf.Max(0,s.Expected-Time.time));
    s.Emission=visual?.Emission??1f;s.Particles=visual?.Particles??1f;s.LightPower=visual?.Light??1f;s.Tier=ticket.Intensity?.Tier;
    int version=s.Version,epoch=_epoch;
    s.Root.position=position;s.Root.rotation=cameraRotation;s.Root.localScale=Vector3.one*scale;s.Root.gameObject.SetActive(true);
    ticket.OutcomeIncomingReason="captured_camera_local_lightning_discharge_not_world_transport";
    var captured=s;
    var playback=new WorldBattleFx.AttackPlayback {
     Contact=()=> {
      if(!Current(captured,version,epoch)){StaleContacts++;return false;}
      if(!Tween.SchedulingAllowed||!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){ticket.RenderFallbackReason="lightning_discharge_contact_environment_unavailable";Retire(captured,version);return false;}
      if(captured.Landed)return true;
      try {
       captured.Landed=true;captured.Started=Time.time;
       // Validate branches, core AND zero-opacity sparks before accepting the receipt.
       DrawAt(captured,0f,true);
       if(Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact)) {
        captured.Light=StageFx.BeginContractAttackLight(ticket.Target,ticket.Impact?.Target,epoch,position,feet,height,captured.Active.LightSpec,0f,false,
         ()=>Current(captured,version,epoch)&&Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact),1f,captured.LightPower,()=>Time.time-captured.Started);
        captured.Light?.Land();
       }
       ticket.AuthoredRenderer="continuous-lightning-discharge-r2";return true;
      } catch(Exception e) { Retire(captured,version);ticket.RenderFallbackReason="lightning_discharge_contact_failed:"+e.GetType().Name;return false; }
     },
     Read=()=>Current(captured,version,epoch)?new {
      attack=ticket.AttackFamily,renderer="continuous-lightning-discharge-r2",worldCasterBeam=false,
      age=captured.Landed?Time.time-captured.Started:Time.time-captured.Expected,nativeAge=captured.Active.Geometry.VisualAge,
      landed=captured.Landed,height=captured.Height,size=captured.Size,damageTier=captured.Tier,version,serial=captured.Serial,
      anticipationUsed=captured.Available,visibleEnd=LightningDischargeGeometry.End(),capturedCameraBasis=true,mirrorRight=1
     }:null
    };
    Tween.RunPresentation(Life(s,version,epoch));
    if(!Current(s,version,epoch)){ticket.RenderFallbackReason="lightning_discharge_scope_cancelled";return null;}
    Submitted++;return playback;
   } catch(Exception e) { if(s!=null)Retire(s,s.Version);ticket.RenderFallbackReason="lightning_discharge_construction_failed:"+e.GetType().Name;return null; }
  }
 }
}
