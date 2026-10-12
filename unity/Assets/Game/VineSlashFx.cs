using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace DeckRogue.Game {
 // Continuous R1 candidate. Four pooled draws/receipt, no flipbook, no gameplay timer.
 // Blender AgX/glare and Principled accents are NOT reproduced by this unlit URP adapter.
 internal static class VineSlashFx {
  sealed class Draw {public Mesh Mesh;public MeshRenderer Renderer;public Vector3[] Vertices;public Color[] Colors;}
  sealed class Slot {
   public Transform Root;public Draw[] Draws;public VineSlashGeometry.Buffer Geometry;
   public bool Busy,Landed;public int Version;public long Serial;public float Expected,Started,Available;
   public bool Mirror;public float Emission=1,Particles=1;public string Tier;public StageFx.ContractLightPlayback Light;
  }
  // Provisional bounded mobile budget: low4/high8 receipts, at most32 mesh draws.
  // The usual two contacts need2 slots. Recycling is observable, not a pixel-visibility guarantee.
  static readonly Slot[] Pool=new Slot[8];static long _serial;static int _epoch;
  static Texture2D _texture;static Material _material;static WorldBattleFx.AttackSpec _lightSpec;
  static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  public static string LastFailure {get;private set;}
  public static int Submitted {get;private set;}public static int Recycled {get;private set;}
  public static int StaleContacts {get;private set;}public static int LateContacts {get;private set;}
  public static int ActiveCount {get{int n=0;foreach(var s in Pool)if(s!=null && s.Busy)n++;return n;}}
  static Vector4 LinearRole(JToken token){
   if(token==null || !ColorUtility.TryParseHtmlString((string)token,out var c))throw new FormatException("invalid palette role");
   c=c.linear;return new Vector4(c.r,c.g,c.b,1);
  }
  public static bool Ready(){
   LastFailure=null;
   if(!Tween.SchedulingAllowed){LastFailure="vine_scope_cancelled";return false;}
   if(!Stage.CombatWorld || Stage.Camera==null || !Stage.Camera.isActiveAndEnabled){LastFailure="vine_world_camera_unavailable";return false;}
   if(ElementBattleFx.ForceMissingForTest){LastFailure="vine_forced_resource_missing";return false;}
   if(QualitySettings.activeColorSpace!=ColorSpace.Linear){LastFailure="vine_requires_linear_project";return false;}
   if(_material!=null && _texture!=null)return true;
   var shader=Shader.Find("DeckRogue/VineSlashFx");
   if(shader==null || !shader.isSupported){LastFailure="vine_shader_unavailable";return false;}
   var texture=Resources.Load<Texture2D>("Art/fx/vine-slash-surface-r1");var palette=Resources.Load<TextAsset>("Data/vine-slash-palette-r1");
   if(texture==null || palette==null || texture.width!=2172 || texture.height!=724){LastFailure="vine_resource_missing_or_resized";return false;}
   try {
    var json=JObject.Parse(palette.text);var roles=json["roles_srgb"];
    var shadow=LinearRole(roles?["shadow"]);var primary=LinearRole(roles?["primary"]);var edge=LinearRole(roles?["edge"]);var core=LinearRole(roles?["core"]);var light=LinearRole(json["scene_light_srgb"]);
    _texture=texture;_material=new Material(shader);_material.SetTexture("_BaseMap",texture);
    _material.SetVector("_RoleShadow",shadow);_material.SetVector("_RolePrimary",primary);_material.SetVector("_RoleEdge",edge);_material.SetVector("_RoleCore",core);
    // New point-light gains are deliberately conservative provisional values. These are
    // not Blender's area-light watts. Target/floor maxima 6.3/3.36 before shared cap/power.
    var color=new Color(light.x,light.y,light.z);
    _lightSpec=new WorldBattleFx.AttackSpec(new[]{-(float)VineSlashGeometry.Anticipation,0f,0f,0f,0f,0f,0f,.080f,.182f},1,1,1,color,color,.045f,0,0){ContractLighting=true,StandardContractPower=true,TargetPeakMultiplier=.35f,FloorPeakMultiplier=.12f,MaxCombineGroup="vine-slash-r1",Envelope=age=>(float)VineSlashGeometry.Light(age)};
    return true;
   } catch(Exception e){LastFailure="vine_palette_invalid:"+e.GetType().Name;return false;}
  }
  static Draw CreateDraw(Transform root,VineSlashGeometry.MeshData data,int index){
   var go=new GameObject("vine-part-"+index);Mesh mesh=null;
   try {go.transform.SetParent(root,false);
   mesh=new Mesh{name="vine-r1-mutable-"+index};mesh.MarkDynamic();
   var vertices=new Vector3[data.Vertices.Length];var uv=new Vector2[data.UV.Length];var colors=new Color[data.Alpha.Length];
   for(int i=0;i<uv.Length;i++)uv[i]=new Vector2((float)data.UV[i].X,(float)data.UV[i].Y);
   mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=data.Triangles;
   // Independent golden scan is under2H in all axes. Conservative initial bounds;
   // SetVertices may recalculate them. Native allocation/CPU costs remain unmeasured.
   mesh.bounds=new Bounds(Vector3.zero,new Vector3(8,8,2));
   go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();
   renderer.sharedMaterial=_material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
   return new Draw{Mesh=mesh,Renderer=renderer,Vertices=vertices,Colors=colors};
   } catch {if(mesh!=null)UnityEngine.Object.Destroy(mesh);UnityEngine.Object.Destroy(go);throw;}
  }
  static void Retire(Slot s,int version){if(s.Version!=version)return;s.Busy=false;s.Light?.Cancel();s.Light=null;if(s.Root!=null)s.Root.gameObject.SetActive(false);}
  public static void Cancel(){_epoch++;foreach(var s in Pool)if(s!=null){Retire(s,s.Version);s.Version++;}}
  static Slot Acquire(){
   int capacity=GfxQuality.Level==GfxLevel.Low?4:8;Slot slot=null;
   for(int i=0;i<capacity;i++){if(Pool[i]==null)Pool[i]=new Slot();if(!Pool[i].Busy || Pool[i].Root==null){slot=Pool[i];break;}}
   if(slot==null){slot=Pool[0];for(int i=1;i<capacity;i++)if(Pool[i].Serial<slot.Serial)slot=Pool[i];Recycled++;}
   Retire(slot,slot.Version);slot.Version++;slot.Serial=++_serial;
   if(slot.Root==null){
    var root=new GameObject("vine-slash-r1").transform;var draws=new Draw[4];
    try {
     var geometry=VineSlashGeometry.Create();draws[0]=CreateDraw(root,geometry.Primary,0);draws[1]=CreateDraw(root,geometry.Secondary,1);draws[2]=CreateDraw(root,geometry.Core,2);draws[3]=CreateDraw(root,geometry.Particles,3);
     slot.Root=root;slot.Geometry=geometry;slot.Draws=draws; // publish only a complete slot
    } catch {foreach(var d in draws)if(d?.Mesh!=null)UnityEngine.Object.Destroy(d.Mesh);root.gameObject.SetActive(false);UnityEngine.Object.Destroy(root.gameObject);throw;}
   }
   return slot;
  }
  static bool Current(Slot s,int v,int epoch)=>epoch==_epoch && s.Version==v && s.Busy && s.Root!=null;
  static float Age(Slot s)=>s.Landed?Time.time-s.Started:Time.time-s.Expected;
  static float LightAge(Slot s){float age=Age(s);return !s.Landed && age>=0?.182f:(float)VineSlashGeometry.VisualClock(age,s.Available);}
  static void Hide(Slot s){foreach(var d in s.Draws)if(d.Renderer!=null)d.Renderer.enabled=false;}
  static void DrawMesh(Draw d,VineSlashGeometry.MeshData data,bool surface,float gain){
   bool active=false;
   for(int i=0;i<d.Vertices.Length;i++){
    var p=data.Vertices[i];d.Vertices[i]=new Vector3((float)p.X,(float)p.Y,-(float)p.Z); // +sourceZ is toward camera.
    float alpha=(float)data.Alpha[i];d.Colors[i]=new Color(1,1,1,alpha);active|=alpha>0;
   }
   d.Renderer.enabled=active;if(!active)return;
   d.Mesh.SetVertices(d.Vertices);d.Mesh.SetColors(d.Colors);
   Props.Clear();Props.SetFloat("_UseTexture",surface?1:0);Props.SetFloat("_Gain",gain);d.Renderer.SetPropertyBlock(Props);
  }
  static void DrawAt(Slot s,float age){
   VineSlashGeometry.Sample(s.Geometry,age,s.Mirror,s.Available);
   DrawMesh(s.Draws[0],s.Geometry.Primary,true,2.25f*s.Emission);DrawMesh(s.Draws[1],s.Geometry.Secondary,true,1.80f*s.Emission);
   DrawMesh(s.Draws[2],s.Geometry.Core,false,3.8f*s.Emission);DrawMesh(s.Draws[3],s.Geometry.Particles,false,2.2f*s.Particles);
  }
  static IEnumerator Life(Slot s,int version,int epoch){
   try {while(Current(s,version,epoch)){
    float age=Age(s);
    if(s.Landed){if(age>=VineSlashGeometry.VisibleEnd)yield break;DrawAt(s,age);}
    else {if(age>=.35f)yield break;if(age>=0)Hide(s);else DrawAt(s,age);}
    yield return null;
   }} finally {if(epoch==_epoch)Retire(s,version);}
  }
  public static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket ticket,float? contactAt=null){
   if(ticket==null || !ticket.HasWorld || ticket.Outcome!="hit" || ticket.AttackFamily!="vine_slash")return null;
   if(!Ready()){ticket.RenderFallbackReason=LastFailure;return null;}
   if(ticket.VineVisibleHeight<=0){ticket.RenderFallbackReason=ticket.VineHeightReason??"captured_base_geometry_unavailable";return null;}
   Slot s=null;
   try {
    s=Acquire();int version=s.Version,epoch=_epoch;var visual=ticket.Visual;
    s.Busy=true;s.Landed=false;s.Expected=contactAt??Time.time;s.Available=Mathf.Min((float)VineSlashGeometry.Anticipation,Mathf.Max(0,s.Expected-Time.time));
    s.Mirror=ticket.Index%2!=0;s.Emission=visual?.Emission??WorldBattleFx.AuthoredEmissionScale(ticket.AuthoredPresentation);s.Particles=visual?.Particles??1;s.Tier=ticket.Intensity?.Tier;
    s.Root.position=ticket.Position;s.Root.rotation=Stage.Camera.transform.rotation;s.Root.localScale=Vector3.one*ticket.VineVisibleHeight*(visual?.Size??WorldBattleFx.AuthoredSizeScale(ticket.AuthoredPresentation));s.Root.gameObject.SetActive(true);
    bool attached=Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact);
    if(attached)s.Light=StageFx.BeginContractAttackLight(ticket.Target,ticket.Impact?.Target,epoch,ticket.Position,ticket.Feet,ticket.VineVisibleHeight,_lightSpec,LightAge(s),false,()=>Current(s,version,epoch)&&Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact),WorldBattleFx.AuthoredLightScale(ticket.AuthoredPresentation),visual?.Light,()=>LightAge(s));
    Submitted++;
    var captured=s;
    var playback=new WorldBattleFx.AttackPlayback{Contact=()=>{
     if(!Current(captured,version,epoch)){StaleContacts++;return false;}
     if(!Tween.SchedulingAllowed){ticket.RenderFallbackReason="vine_scope_cancelled";Retire(captured,version);return false;}
     if(!Stage.CombatWorld || Stage.Camera==null || !Stage.Camera.isActiveAndEnabled){ticket.RenderFallbackReason="vine_world_camera_unavailable";Retire(captured,version);return false;}
     if(captured.Landed)return true;
     if(Time.time>captured.Expected+.002f)LateContacts++;
     captured.Landed=true;captured.Started=Time.time;DrawAt(captured,0f);captured.Light?.Land();return true;
    },Read=()=>Current(captured,version,epoch)?new{attack="vine_slash",renderer="continuous-mesh-r1",age=Age(captured),visualAge=captured.Geometry.VisualAge,landed=captured.Landed,mirror=captured.Mirror,height=ticket.VineVisibleHeight,damageTier=captured.Tier,version,serial=captured.Serial,visibleEnd=VineSlashGeometry.VisibleEnd}:null};
    Tween.RunPresentation(Life(s,version,epoch));return playback;
   } catch(Exception e){if(s!=null)Retire(s,s.Version);ticket.RenderFallbackReason="vine_construction_failed:"+e.GetType().Name;return null;}
  }
 }
}
