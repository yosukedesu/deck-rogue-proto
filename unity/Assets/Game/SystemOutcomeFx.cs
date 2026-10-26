using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace DeckRogue.Game {
 // Resource and positive-loss system responses. Shared low4/high8 pool; 24 active draws at most.
 // Six lazy families use up to104 resident meshes. No texture, hitstop or predictive wait.
 internal static class SystemOutcomeFx {
  sealed class Draw {public Mesh Mesh;public MeshRenderer Renderer;public Vector3[] Vertices;public Vector2[] UV,PhaseUV;public Color[] Colors;}
  sealed class Set {public Transform Root;public Draw[] Draws;public SystemOutcomeGeometry.Buffer Geometry;}
  sealed class Slot {public Transform Root;public Set Guard,Nullified,Reflect,Burn,SelfLoss,Affliction,Active;public int Version;public long Serial;public bool Busy;public float Started;public double SourceX,SourceY;public string Family;public StageFx.ContractLightPlayback Light;}
  static readonly Slot[] Pool=new Slot[8];static int _epoch;static long _serial;
  static Material _material,_reflectMaterial,_burnMaterial;static Color[] _roles,_reflectRoles,_burnRoles;static WorldBattleFx.AttackSpec _guardLight,_reflectLight,_burnLight;static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  public static string LastFailure{get;private set;}public static int Submitted{get;private set;}public static int Recycled{get;private set;}
  public static int ActiveCount{get{int n=0;foreach(var s in Pool)if(s!=null&&s.Busy)n++;return n;}}
  static Color Role(JToken token){if(token==null||!ColorUtility.TryParseHtmlString((string)token,out var c))throw new FormatException("invalid outcome role");return c.linear;}
  static Vector4 Vector(Color c)=>new Vector4(c.r,c.g,c.b,1);
  static Material MaterialFor(string family)=>family=="reflect"?_reflectMaterial:family=="burn"?_burnMaterial:_material;
  static Color[] RolesFor(string family)=>family=="reflect"?_reflectRoles:family=="burn"?_burnRoles:_roles;
  static WorldBattleFx.AttackSpec LightFor(string family)=>family=="reflect"?_reflectLight:family=="burn"?_burnLight:_guardLight;
  public static bool Ready()=>Ready("guard");
  static bool Ready(string family){
   LastFailure=null;
   if(!Tween.SchedulingAllowed){LastFailure="outcome_scope_cancelled";return false;}
   if(!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){LastFailure="outcome_world_camera_unavailable";return false;}
   if(ElementBattleFx.ForceMissingForTest){LastFailure="outcome_forced_resource_missing";return false;}
   if(QualitySettings.activeColorSpace!=ColorSpace.Linear){LastFailure="outcome_requires_linear_project";return false;}
   if(MaterialFor(family)!=null)return true;
   bool positive=SystemImpactGeometry.IsFamily(family);var shader=Shader.Find("DeckRogue/SystemOutcomeFx");var palette=Resources.Load<TextAsset>(positive?"Data/system-impact-palettes-r1":"Data/system-outcome-palette-r1");
   if(shader==null||!shader.isSupported||palette==null){LastFailure="outcome_shader_or_palette_unavailable";return false;}
   try{
    var root=JObject.Parse(palette.text);JToken j=positive?root["families"]?[family]:root;var roles=j?["roles_srgb"];var shadow=Role(roles?["shadow"]);var primary=Role(roles?["primary"]);var edge=Role(roles?["edge"]);var core=Role(roles?["core"]);var light=Role(j?["scene_light_srgb"]);
    var material=new Material(shader);
    try{material.SetVector("_RoleShadow",Vector(shadow));material.SetVector("_RolePrimary",Vector(primary));material.SetVector("_RoleEdge",Vector(edge));material.SetVector("_RoleCore",Vector(core));}catch{UnityEngine.Object.Destroy(material);throw;}
    var colors=new[]{primary,edge,core};var spec=MakeLight(light,positive?family:"guard");
    if(family=="reflect"){_reflectMaterial=material;_reflectRoles=colors;_reflectLight=spec;}
    else if(family=="burn"){_burnMaterial=material;_burnRoles=colors;_burnLight=spec;}
    else{_material=material;_roles=colors;_guardLight=spec;}
    return true;
   }catch(Exception e){LastFailure="outcome_palette_invalid:"+e.GetType().Name;return false;}
  }
  static WorldBattleFx.AttackSpec MakeLight(Color color,string family)=>new WorldBattleFx.AttackSpec(new[]{0f,0f,0f,0f,0f,0f,0f,.065f,(float)SystemOutcomeGeometry.End(family)},1,1,1,color,color,.010f,0,0){ContractLighting=true,StandardContractPower=true,TargetPeakMultiplier=.10f,FloorPeakMultiplier=0,MaxCombineGroup="system-"+family+"-r1",Envelope=age=>(float)SystemOutcomeGeometry.Light(family,age)};
  static Set CreateSet(Transform parent,string family){
   var child=new GameObject("outcome-"+family).transform;child.SetParent(parent,false);var geometry=SystemOutcomeGeometry.Create(family);var set=new Set{Root=child,Geometry=geometry,Draws=new Draw[geometry.Batches.Length]};
   try{for(int i=0;i<set.Draws.Length;i++){
    var data=geometry.Batches[i];var go=new GameObject("outcome-part-"+i);go.transform.SetParent(child,false);var mesh=new Mesh{name="outcome-r1-mutable-"+family+"-"+i};var d=new Draw{Mesh=mesh};set.Draws[i]=d;
    mesh.MarkDynamic();d.Vertices=new Vector3[data.Vertices.Length];d.UV=new Vector2[data.Vertices.Length];d.PhaseUV=new Vector2[data.Vertices.Length];d.Colors=new Color[data.Vertices.Length];
    mesh.vertices=d.Vertices;mesh.uv=d.UV;mesh.SetUVs(1,d.PhaseUV);mesh.colors=d.Colors;mesh.triangles=data.Triangles;mesh.bounds=new Bounds(Vector3.zero,new Vector3(4,4,2));
    go.AddComponent<MeshFilter>().sharedMesh=mesh;d.Renderer=go.AddComponent<MeshRenderer>();d.Renderer.sharedMaterial=MaterialFor(family);d.Renderer.shadowCastingMode=ShadowCastingMode.Off;d.Renderer.receiveShadows=false;d.Renderer.enabled=false;
   }child.gameObject.SetActive(false);return set;}catch{DestroySet(set);throw;}
  }
  static void DestroySet(Set set){if(set==null)return;foreach(var d in set.Draws)if(d?.Mesh!=null)UnityEngine.Object.Destroy(d.Mesh);if(set.Root!=null){set.Root.gameObject.SetActive(false);UnityEngine.Object.Destroy(set.Root.gameObject);}}
  static void Retire(Slot s,int version){if(s.Version!=version)return;s.Busy=false;s.Light?.Cancel();s.Light=null;if(s.Root!=null)s.Root.gameObject.SetActive(false);}
  public static void Cancel(){_epoch++;foreach(var s in Pool)if(s!=null){Retire(s,s.Version);s.Version++;}}
  static Slot Acquire(string family){
   int limit=GfxQuality.Level==GfxLevel.Low?4:8;Slot s=null;
   for(int i=0;i<limit;i++){if(Pool[i]==null)Pool[i]=new Slot();if(!Pool[i].Busy||Pool[i].Root==null){s=Pool[i];break;}}
   if(s==null){s=Pool[0];for(int i=1;i<limit;i++)if(Pool[i].Serial<s.Serial)s=Pool[i];Recycled++;}
   Retire(s,s.Version);s.Version++;s.Serial=++_serial;
   if(s.Root==null){DestroySet(s.Guard);DestroySet(s.Nullified);DestroySet(s.Reflect);DestroySet(s.Burn);DestroySet(s.SelfLoss);DestroySet(s.Affliction);s.Guard=s.Nullified=s.Reflect=s.Burn=s.SelfLoss=s.Affliction=null;s.Root=new GameObject("system-outcome-r1").transform;}
   if(family=="selfloss"){if(s.SelfLoss==null)s.SelfLoss=CreateSet(s.Root,family);s.Active=s.SelfLoss;}else if(family=="affliction"){if(s.Affliction==null)s.Affliction=CreateSet(s.Root,family);s.Active=s.Affliction;}else if(family=="reflect"){if(s.Reflect==null)s.Reflect=CreateSet(s.Root,family);s.Active=s.Reflect;}else if(family=="burn"){if(s.Burn==null)s.Burn=CreateSet(s.Root,family);s.Active=s.Burn;}else if(family=="guard"){if(s.Guard==null)s.Guard=CreateSet(s.Root,family);s.Active=s.Guard;}else{if(s.Nullified==null)s.Nullified=CreateSet(s.Root,family);s.Active=s.Nullified;}
   if(s.Guard?.Root!=null)s.Guard.Root.gameObject.SetActive(family=="guard");if(s.Nullified?.Root!=null)s.Nullified.Root.gameObject.SetActive(family=="nullified");if(s.Reflect?.Root!=null)s.Reflect.Root.gameObject.SetActive(family=="reflect");if(s.Burn?.Root!=null)s.Burn.Root.gameObject.SetActive(family=="burn");if(s.SelfLoss?.Root!=null)s.SelfLoss.Root.gameObject.SetActive(family=="selfloss");if(s.Affliction?.Root!=null)s.Affliction.Root.gameObject.SetActive(family=="affliction");return s;
  }
  static bool Current(Slot s,int v,int epoch)=>_epoch==epoch&&s.Version==v&&s.Busy&&s.Root!=null;
  static void DrawAt(Slot s,float age){
   SystemOutcomeGeometry.Sample(s.Active.Geometry,age);SystemOutcomeGeometry.FitIncoming(s.Active.Geometry,s.SourceX,s.SourceY);
   for(int j=0;j<s.Active.Draws.Length;j++){
    var d=s.Active.Draws[j];var data=s.Active.Geometry.Batches[j];bool visible=false;
    for(int i=0;i<data.Vertices.Length;i++){var p=data.Vertices[i];d.Vertices[i]=new Vector3((float)p.X,(float)p.Y,-(float)p.Z);d.UV[i]=new Vector2((float)data.UV[i].X,(float)data.UV[i].Y);d.PhaseUV[i]=new Vector2(data.Phase==null?0:(float)data.Phase[i],0);float a=(float)data.Alpha[i],gain=(float)data.Gain[i];var c=(data.Kind==0||data.Kind==7)?RolesFor(s.Family)[data.Role[i]]:Color.white;d.Colors[i]=new Color(c.r*gain,c.g*gain,c.b*gain,a);visible|=a>0;}
    d.Renderer.enabled=visible;if(!visible)continue;d.Mesh.SetVertices(d.Vertices);d.Mesh.SetUVs(0,d.UV);d.Mesh.SetUVs(1,d.PhaseUV);d.Mesh.SetColors(d.Colors);Props.Clear();Props.SetFloat("_Kind",data.Kind);Props.SetFloat("_VisualAge",age);d.Renderer.SetPropertyBlock(Props);
   }
  }
  static IEnumerator Life(Slot s,int version,int epoch){try{while(Current(s,version,epoch)){float age=Time.time-s.Started;if(age>=SystemOutcomeGeometry.End(s.Family))yield break;DrawAt(s,age);yield return null;}}finally{if(epoch==_epoch)Retire(s,version);}}
  static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);
  static double Dot(Vector3 a,Vector3 b)=>(double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z;
  public static bool TryPlay(ResolvedCombatFx.Ticket ticket){
   if(ticket==null||!ticket.HasWorld||!ticket.OutcomeExact||(ticket.Outcome!="guard"&&ticket.Outcome!="nullified")){LastFailure="outcome_receipt_not_exact";return false;}
   return Play(ticket,ticket.Outcome);
  }
  public static bool TryPlayDamage(ResolvedCombatFx.Ticket ticket){
   if(ticket==null||!ticket.HasWorld||ticket.SystemTargetUnproved||(ticket.SystemResponseFamily=="selfloss"?ticket.Outcome!="cost":ticket.SystemResponseFamily=="affliction"?ticket.Outcome!="affliction":ticket.Outcome!="hit")||ticket.Amount<=0||(!SystemImpactGeometry.IsFamily(ticket.SystemResponseFamily)&&!SystemSelfLossGeometry.IsFamily(ticket.SystemResponseFamily))){LastFailure="system_positive_receipt_not_exact";return false;}
   return Play(ticket,ticket.SystemResponseFamily);
  }
  static bool Play(ResolvedCombatFx.Ticket ticket,string family){
   if(!Ready(family))return false;if(float.IsNaN(ticket.OutcomeVisibleHeight)||float.IsInfinity(ticket.OutcomeVisibleHeight)||ticket.OutcomeVisibleHeight<=0){LastFailure=ticket.OutcomeHeightReason??"outcome_base_geometry_unavailable";return false;}
   if(!Finite(ticket.Position)||!Finite(ticket.Feet)){LastFailure="outcome_target_position_invalid";return false;}
   Slot s=null;try{
    s=Acquire(family);s.Busy=true;s.Family=family;s.Started=Time.time;int version=s.Version,epoch=_epoch;float h=ticket.OutcomeVisibleHeight;
    s.Root.position=ticket.Position;s.Root.rotation=Stage.Camera.transform.rotation;s.Root.localScale=Vector3.one*h;s.Root.gameObject.SetActive(true);s.SourceX=double.NaN;s.SourceY=0;
    if(ticket.HasBeamOrigin){var delta=ticket.BeamOrigin-ticket.Position;s.SourceX=Dot(delta,s.Root.rotation*Vector3.right)/h;s.SourceY=Dot(delta,s.Root.rotation*Vector3.up)/h;}
    double span=Math.Sqrt(s.SourceX*s.SourceX+s.SourceY*s.SourceY);ticket.OutcomeIncomingReason=(family=="burn"||SystemSelfLossGeometry.IsFamily(family))?"target_local_no_direction":double.IsNaN(span)||double.IsInfinity(span)||span<=1e-5?(family=="reflect"?"source_unavailable_local_contact_only":"camera_left_incoming_fallback"):"captured_source_direction";
    DrawAt(s,0);
    if(s.Family!="nullified"&&!SystemSelfLossGeometry.IsFamily(s.Family)&&Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact)){var capture=s;s.Light=StageFx.BeginContractAttackLight(ticket.Target,ticket.Impact?.Target,epoch,ticket.Position,ticket.Feet,h,LightFor(family),0,false,()=>Current(capture,version,epoch)&&Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact),1f,1f,()=>Time.time-capture.Started);s.Light?.Land();}
    Tween.RunPresentation(Life(s,version,epoch));if(!Current(s,version,epoch)){LastFailure="outcome_scope_cancelled";return false;}Submitted++;return true;
   }catch(Exception e){if(s!=null)Retire(s,s.Version);LastFailure="outcome_construction_failed:"+e.GetType().Name;return false;}
  }
 }
}
