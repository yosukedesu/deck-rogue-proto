using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace DeckRogue.Game {
 // Continuous enemy contact candidates. Only beam uses caller-reserved anticipation; actual Land owns contact0.
 internal static class EnemyContactFx {
  sealed class Draw {public Mesh Mesh;public MeshRenderer Renderer;public Vector3[] Vertices;public Vector2[] UV;public Color[] Colors;}
  sealed class Set {public Transform Root;public Draw[] Draws;public EnemyContactGeometry.Buffer Geometry;}
  sealed class Slot {public Transform Root;public Set Blunt,Claw,Fang,Beam,Active;public int Version;public long Serial;public bool Busy,Landed;public float Started,Expected,Emission,Particles,Height,Size,Available;public EnemyContactGeometry.Point SourceH;public bool HasRay;public string Family,Tier;public StageFx.ContractLightPlayback Light;}
  // One shared low4/high8 receipt pool. At most24 active draws. Two lazy sets per
  // slot avoid shape-switch allocations after warmup: four lazy families, at most96 resident meshes.
  static readonly Slot[] Pool=new Slot[8];static int _epoch;static long _serial;
  static Material _material,_beamMaterial;static Texture2D _slash,_blunt,_fang;static Color[] _solidRoles,_beamSolidRoles;
  static WorldBattleFx.AttackSpec _lightSpec,_fangLightSpec,_beamLightSpec;static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  public static string LastFailure {get;private set;}public static int Submitted {get;private set;}public static int Recycled {get;private set;}public static int StaleContacts {get;private set;}
  public static bool IsFamily(string attack)=>attack=="enemy_blunt" || attack=="enemy_claw" || attack=="enemy_fang" || attack=="enemy_beam";
  internal static string Family(string attack)=>attack=="enemy_claw"?"claw":attack=="enemy_fang"?"fang":attack=="enemy_beam"?"beam":"blunt";
  static WorldBattleFx.AttackSpec Spec(string family)=>family=="beam"?_beamLightSpec:family=="fang"?_fangLightSpec:_lightSpec;
  public static int ActiveCount {get{int count=0;foreach(var s in Pool)if(s!=null && s.Busy)count++;return count;}}
  static Color Role(JToken token){if(token==null || !ColorUtility.TryParseHtmlString((string)token,out var c))throw new FormatException("invalid physical role");return c.linear;}
  static Vector4 Vector(Color c)=>new Vector4(c.r,c.g,c.b,1);
  public static bool Ready(string attack){
   LastFailure=null;if(!IsFamily(attack)){LastFailure="physical_family_unmapped";return false;}
   if(!Tween.SchedulingAllowed){LastFailure="physical_scope_cancelled";return false;}
   if(!Stage.CombatWorld || Stage.Camera==null || !Stage.Camera.isActiveAndEnabled){LastFailure="physical_world_camera_unavailable";return false;}
   if(ElementBattleFx.ForceMissingForTest){LastFailure="physical_forced_resource_missing";return false;}
   if(QualitySettings.activeColorSpace!=ColorSpace.Linear){LastFailure="physical_requires_linear_project";return false;}
   if(attack=="enemy_claw"){
    if(_slash==null)_slash=Resources.Load<Texture2D>("Art/fx/vine-slash-surface-r1");
    if(_slash==null || _slash.width!=2172 || _slash.height!=724){LastFailure="physical_slash_resource_missing_or_resized";return false;}
   }else if(attack=="enemy_fang"){
    if(_fang==null)_fang=Resources.Load<Texture2D>("Art/fx/enemy-fang-row-r1");
    if(_fang==null || _fang.width!=1942 || _fang.height!=809){LastFailure="physical_fang_resource_missing_or_resized";return false;}
   }else if(attack=="enemy_blunt"){
    if(_blunt==null)_blunt=Resources.Load<Texture2D>("Art/fx/enemy-blunt-contact-r1");
    if(_blunt==null || _blunt.width!=1774 || _blunt.height!=887){LastFailure="physical_blunt_resource_missing_or_resized";return false;}
   }
   bool beam=attack=="enemy_beam";if((beam?_beamMaterial:_material)!=null)return true;
   var shader=Shader.Find("DeckRogue/PhysicalContactFx");var palette=Resources.Load<TextAsset>(beam?"Data/neutral-beam-palette-r1":"Data/physical-contact-palette-r1");
   if(shader==null || !shader.isSupported || palette==null){LastFailure="physical_shader_or_palette_unavailable";return false;}
   try{
    var json=JObject.Parse(palette.text);var roles=json["roles_srgb"];var shadow=Role(roles?["shadow"]);var primary=Role(roles?["primary"]);var edge=Role(roles?["edge"]);var core=Role(roles?["core"]);var light=Role(json["scene_light_srgb"]);
    var material=new Material(shader);
    try{material.SetVector("_RoleShadow",Vector(shadow));material.SetVector("_RolePrimary",Vector(primary));material.SetVector("_RoleEdge",Vector(edge));material.SetVector("_RoleCore",Vector(core));}
    catch{UnityEngine.Object.Destroy(material);throw;}
    // Provisional bounded point-light calibration, not copied Blender area watts.
    if(beam){_beamMaterial=material;_beamSolidRoles=new[]{primary,edge,core};_beamLightSpec=LightSpec(light,"beam",.16f);}
    else{_material=material;_solidRoles=new[]{primary,edge,core};_lightSpec=LightSpec(light,"blunt",.222f);_fangLightSpec=LightSpec(light,"fang",.14f);}
    return true;
   }catch(Exception e){LastFailure="physical_palette_invalid:"+e.GetType().Name;return false;}
  }
  static WorldBattleFx.AttackSpec LightSpec(Color color,string family,float end)=>new WorldBattleFx.AttackSpec(new[]{0f,0f,0f,0f,0f,0f,0f,.080f,end},1,1,1,color,color,.015f,0,0){ContractLighting=true,StandardContractPower=true,TargetPeakMultiplier=.35f,FloorPeakMultiplier=.12f,MaxCombineGroup=family=="beam"?"neutral-beam-r1":"physical-contact-r1",Envelope=age=>(float)EnemyContactGeometry.Light(family,age)};
  static Set CreateSet(Transform root,string family){
   var child=new GameObject("physical-"+family).transform;child.SetParent(root,false);var result=new Set{Root=child,Geometry=EnemyContactGeometry.Create(family),Draws=new Draw[3]};
   try{for(int j=0;j<3;j++){
    var data=result.Geometry.Batches[j];var go=new GameObject("physical-part-"+j);go.transform.SetParent(child,false);
    var mesh=new Mesh{name="physical-r1-mutable-"+family+"-"+j};var d=new Draw{Mesh=mesh};result.Draws[j]=d; // own mesh before any throwable initialization
    mesh.MarkDynamic();d.Vertices=new Vector3[data.Vertices.Length];d.Colors=new Color[data.Vertices.Length];
    d.UV=new Vector2[data.UV.Length];for(int i=0;i<d.UV.Length;i++)d.UV[i]=new Vector2((float)data.UV[i].X,(float)data.UV[i].Y);
    mesh.vertices=d.Vertices;mesh.colors=d.Colors;mesh.uv=d.UV;mesh.triangles=data.Triangles;mesh.bounds=new Bounds(Vector3.zero,new Vector3(8,8,2));
    go.AddComponent<MeshFilter>().sharedMesh=mesh;d.Renderer=go.AddComponent<MeshRenderer>();d.Renderer.sharedMaterial=family=="beam"?_beamMaterial:_material;d.Renderer.shadowCastingMode=ShadowCastingMode.Off;d.Renderer.receiveShadows=false;d.Renderer.enabled=false;
   }child.gameObject.SetActive(false);return result;}catch{DestroySet(result);throw;}
  }
  static void DestroySet(Set set){if(set==null)return;foreach(var d in set.Draws)if(d?.Mesh!=null)UnityEngine.Object.Destroy(d.Mesh);if(set.Root!=null){set.Root.gameObject.SetActive(false);UnityEngine.Object.Destroy(set.Root.gameObject);}}
  static void Retire(Slot s,int v){if(s.Version!=v)return;s.Busy=false;s.Light?.Cancel();s.Light=null;if(s.Root!=null)s.Root.gameObject.SetActive(false);}
  public static void Cancel(){_epoch++;foreach(var s in Pool)if(s!=null){Retire(s,s.Version);s.Version++;}}
  static Slot Acquire(string family){
   int capacity=GfxQuality.Level==GfxLevel.Low?4:8;Slot s=null;
   for(int i=0;i<capacity;i++){if(Pool[i]==null)Pool[i]=new Slot();if(!Pool[i].Busy || Pool[i].Root==null){s=Pool[i];break;}}
   if(s==null){s=Pool[0];for(int i=1;i<capacity;i++)if(Pool[i].Serial<s.Serial)s=Pool[i];Recycled++;}
   Retire(s,s.Version);s.Version++;s.Serial=++_serial;
   if(s.Root==null){DestroySet(s.Blunt);DestroySet(s.Claw);DestroySet(s.Fang);DestroySet(s.Beam);s.Blunt=s.Claw=s.Fang=s.Beam=null;s.Root=new GameObject("enemy-physical-r1").transform;}
   if(family=="claw"){if(s.Claw==null)s.Claw=CreateSet(s.Root,family);s.Active=s.Claw;}else if(family=="fang"){if(s.Fang==null)s.Fang=CreateSet(s.Root,family);s.Active=s.Fang;}else if(family=="beam"){if(s.Beam==null)s.Beam=CreateSet(s.Root,family);s.Active=s.Beam;}else{if(s.Blunt==null)s.Blunt=CreateSet(s.Root,family);s.Active=s.Blunt;}
   if(s.Claw?.Root!=null)s.Claw.Root.gameObject.SetActive(family=="claw");if(s.Blunt?.Root!=null)s.Blunt.Root.gameObject.SetActive(family=="blunt");
   if(s.Fang?.Root!=null)s.Fang.Root.gameObject.SetActive(family=="fang");if(s.Beam?.Root!=null)s.Beam.Root.gameObject.SetActive(family=="beam");
   return s;
  }
  static bool Current(Slot s,int version,int epoch)=>_epoch==epoch && s.Version==version && s.Busy && s.Root!=null;
  static void DrawAt(Slot s,float age){
   double visualAge=s.Family=="beam"?EnemyContactGeometry.BeamClock(age,s.Available):age;EnemyContactGeometry.Sample(s.Active.Geometry,visualAge);
   if(s.Family=="beam")s.HasRay=EnemyContactGeometry.FitBeam(s.Active.Geometry,s.SourceH,s.Size);
   for(int j=0;j<3;j++){
    var d=s.Active.Draws[j];var data=s.Active.Geometry.Batches[j];bool visible=false;Vector3 lo=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue),hi=new Vector3(float.MinValue,float.MinValue,float.MinValue);
    for(int i=0;i<data.Vertices.Length;i++){
     var p=data.Vertices[i];d.Vertices[i]=new Vector3((float)p.X,(float)p.Y,-(float)p.Z);
     float a=(float)data.Alpha[i],gain=(float)data.Gain[i];Color role=j==0?Color.white:(s.Family=="beam"?_beamSolidRoles:_solidRoles)[data.Role[i]];
     d.Colors[i]=new Color(role.r*gain,role.g*gain,role.b*gain,a);visible|=a>0;
     if(s.Family=="beam"){d.UV[i]=new Vector2((float)data.UV[i].X,(float)data.UV[i].Y);var v=d.Vertices[i];lo=new Vector3(Mathf.Min(lo.x,v.x),Mathf.Min(lo.y,v.y),Mathf.Min(lo.z,v.z));hi=new Vector3(Mathf.Max(hi.x,v.x),Mathf.Max(hi.y,v.y),Mathf.Max(hi.z,v.z));}
    }
    d.Renderer.enabled=visible;if(!visible)continue;d.Mesh.SetVertices(d.Vertices);d.Mesh.SetColors(d.Colors);
    if(s.Family=="beam"){d.Mesh.SetUVs(0,d.UV);d.Mesh.bounds=new Bounds((lo+hi)*.5f,hi-lo+Vector3.one*.001f);}
    Props.Clear();Props.SetTexture("_BaseMap",s.Family=="claw"?_slash:s.Family=="fang"?_fang:_blunt);Props.SetFloat("_UseTexture",j==0?1:0);Props.SetFloat("_BluntAccent",s.Family=="blunt"?1:0);Props.SetFloat("_UseBeam",s.Family=="beam" && j==0?1:0);Props.SetFloat("_VisualAge",(float)visualAge);Props.SetFloat("_Gain",j==2?s.Particles:s.Emission);d.Renderer.SetPropertyBlock(Props);
   }
  }
  static float LightAge(Slot s)=>s.Landed?Time.time-s.Started:.222f;
  static void Hide(Slot s){foreach(var d in s.Active.Draws)d.Renderer.enabled=false;}
  static double Dot(Vector3 a,Vector3 b)=>(double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z;
  static IEnumerator Life(Slot s,int version,int epoch){
   try{while(Current(s,version,epoch)){
    if(s.Landed){float age=Time.time-s.Started;if(age>=EnemyContactGeometry.End(s.Family))yield break;DrawAt(s,age);}
    else{float age=Time.time-s.Expected;if(age>=.35f)yield break;if(s.Family=="beam" && age<0)DrawAt(s,age);else Hide(s);}
    yield return null;
   }}finally{if(epoch==_epoch)Retire(s,version);}
  }
  public static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket ticket,float? contactAt=null){
   if(ticket==null || !ticket.HasWorld || ticket.Outcome!="hit" || !IsFamily(ticket.AttackFamily))return null;
   if(!Ready(ticket.AttackFamily)){ticket.RenderFallbackReason=LastFailure;return null;}
   if(ticket.ContactVisibleHeight<=0){ticket.RenderFallbackReason=ticket.ContactHeightReason??"captured_base_geometry_unavailable";return null;}
   Slot s=null;
   try{
    string family=Family(ticket.AttackFamily);s=Acquire(family);int version=s.Version,epoch=_epoch;var visual=ticket.Visual;
    s.Busy=true;s.Landed=false;s.Family=family;s.Expected=contactAt??Time.time;s.Height=ticket.ContactVisibleHeight;s.Size=visual?.Size??1;s.Available=Mathf.Min((float)EnemyContactGeometry.BeamAnticipation,Mathf.Max(0,s.Expected-Time.time));s.Emission=visual?.Emission??1;s.Particles=visual?.Particles??1;s.Tier=ticket.Intensity?.Tier;
    foreach(var d in s.Active.Draws)d.Renderer.enabled=false;
    s.Root.position=ticket.Position;s.Root.rotation=Stage.Camera.transform.rotation;s.Root.localScale=Vector3.one*s.Height*(family=="beam"?1:s.Size);s.Root.gameObject.SetActive(true);
    s.SourceH=new EnemyContactGeometry.Point(double.NaN,0,0);s.HasRay=false;
    if(family=="beam"){
     if(ticket.HasBeamOrigin){var delta=ticket.BeamOrigin-ticket.Position;var rotation=s.Root.rotation;s.SourceH=new EnemyContactGeometry.Point(Dot(delta,rotation*Vector3.right)/s.Height,Dot(delta,rotation*Vector3.up)/s.Height,-Dot(delta,rotation*Vector3.forward)/s.Height);}
     double length=Math.Sqrt(s.SourceH.X*s.SourceH.X+s.SourceH.Y*s.SourceH.Y);s.HasRay=!double.IsNaN(length)&&!double.IsInfinity(length)&&length>1e-5&&!double.IsNaN(s.SourceH.Z)&&!double.IsInfinity(s.SourceH.Z);
     if(!s.HasRay)ticket.RenderFallbackReason=ticket.HasBeamOrigin?"beam_zero_or_invalid_projected_span_endpoint_only":"beam_origin_unavailable_endpoint_only";
    }
    if(Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact))s.Light=StageFx.BeginContractAttackLight(ticket.Target,ticket.Impact?.Target,epoch,ticket.Position,ticket.Feet,s.Height,Spec(family),.222f,false,()=>Current(s,version,epoch)&&Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact),1f,visual?.Light,()=>LightAge(s));
    var captured=s;var playback=new WorldBattleFx.AttackPlayback{Contact=()=>{
     if(!Current(captured,version,epoch)){StaleContacts++;return false;}
     if(!Tween.SchedulingAllowed || !Stage.CombatWorld || Stage.Camera==null || !Stage.Camera.isActiveAndEnabled){ticket.RenderFallbackReason=!Tween.SchedulingAllowed?"physical_scope_cancelled":"physical_world_camera_unavailable";Retire(captured,version);return false;}
     if(captured.Landed)return true;captured.Landed=true;captured.Started=Time.time;DrawAt(captured,0);captured.Light?.Land();return true;
    },Read=()=>Current(captured,version,epoch)?new{attack=ticket.AttackFamily,renderer="continuous-physical-r1",age=captured.Landed?Time.time-captured.Started:0,landed=captured.Landed,height=captured.Height,damageTier=captured.Tier,version,serial=captured.Serial,visibleEnd=EnemyContactGeometry.End(captured.Family),anticipationUsed=captured.Family=="beam"?captured.Available:0,ray=captured.HasRay,anchor=ticket.BeamAnchorReason}:null};
    Tween.RunPresentation(Life(s,version,epoch));if(!Current(s,version,epoch)){ticket.RenderFallbackReason="physical_scope_cancelled";return null;}Submitted++;return playback;
   }catch(Exception e){if(s!=null)Retire(s,s.Version);ticket.RenderFallbackReason="physical_construction_failed:"+e.GetType().Name;return null;}
  }
 }
}
