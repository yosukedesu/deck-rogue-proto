using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace DeckRogue.Game {
 // Receipt-owned local light passage. Rebuilt R2 source, no world caster beam or extra hits.
 internal static class LightRayFx {
  sealed class Draw {public Mesh Mesh;public MeshRenderer Renderer;public Vector3[] Vertices;public Vector2[] UV,Effect;public Color[] Colors;}
  sealed class Set {public Transform Root;public Draw[] Draws;public LightRayGeometry.Buffer Geometry;public WorldBattleFx.AttackSpec LightSpec;}
  sealed class Slot {public Transform Root;public Set Active;public bool Busy,Landed;public int Version;public long Serial;public float Started,Expected,Available,Height,Size,Emission,Particles,LightPower;public string Tier;public StageFx.ContractLightPlayback Light;}
  static readonly Slot[] Pool=new Slot[8];static int _epoch;static long _serial;static Material _material;static Texture2D _texture;static Color[] _roles;static Color _lightColor;static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  public static string LastFailure{get;private set;}public static int Submitted{get;private set;}public static int Recycled{get;private set;}public static int StaleContacts{get;private set;}
  public static int ActiveCount{get{int n=0;foreach(var s in Pool)if(s!=null&&s.Busy)n++;return n;}}
  public static bool IsFamily(string family)=>family=="light";
  public static bool Eligible(ResolvedCombatFx.Ticket t)=>t!=null&&IsFamily(t.AttackFamily)&&t.Element=="light";
  public static float Lead(ResolvedCombatFx.Ticket t)=>Eligible(t)?(float)LightRayGeometry.Lead():0;
  static Color Role(JToken token){if(token==null||!ColorUtility.TryParseHtmlString((string)token,out var c))throw new FormatException("invalid light role");return c.linear;}
  static Vector4 Vector(Color c)=>new Vector4(c.r,c.g,c.b,1);
  public static bool Ready(){
   LastFailure=null;if(!Tween.SchedulingAllowed){LastFailure="light_ray_scope_cancelled";return false;}
   if(!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){LastFailure="light_ray_world_camera_unavailable";return false;}
   if(ElementBattleFx.ForceMissingForTest){LastFailure="light_ray_forced_resource_missing";return false;}
   if(QualitySettings.activeColorSpace!=ColorSpace.Linear){LastFailure="light_ray_requires_linear_project";return false;}
   if(_material!=null&&_texture!=null)return true;
   var shader=Shader.Find("DeckRogue/LightRayFx");var texture=Resources.Load<Texture2D>("Art/fx/light-ray-surface-r2");var palette=Resources.Load<TextAsset>("Data/light-ray-palette-r2");
   if(shader==null||!shader.isSupported||texture==null||texture.width!=1774||texture.height!=887||palette==null){LastFailure="light_ray_shader_or_resource_unavailable";return false;}
   try{var j=JObject.Parse(palette.text);var roles=j["roles_srgb"];var colors=new[]{Role(roles?["shadow"]),Role(roles?["primary"]),Role(roles?["edge"]),Role(roles?["core"])};var light=Role(j["scene_light_srgb"]);var material=new Material(shader);
    try{material.SetTexture("_BaseMap",texture);material.SetVector("_RoleShadow",Vector(colors[0]));material.SetVector("_RolePrimary",Vector(colors[1]));material.SetVector("_RoleEdge",Vector(colors[2]));material.SetVector("_RoleCore",Vector(colors[3]));}catch{UnityEngine.Object.Destroy(material);throw;}
    _material=material;_texture=texture;_roles=colors;_lightColor=light;return true;
   }catch(Exception e){LastFailure="light_ray_palette_invalid:"+e.GetType().Name;return false;}
  }
  static WorldBattleFx.AttackSpec Spec(){return new WorldBattleFx.AttackSpec(new[]{0f,0f,0f,0f,0f,0f,0f,.080f,(float)LightRayGeometry.End()},1,1,1,_lightColor,_lightColor,.022f,0f,0f){ContractLighting=true,StandardContractPower=true,TargetPeakMultiplier=.35f*(60f/55f),FloorPeakMultiplier=.12f*(20f/20f),MaxCombineGroup="light-ray-continuous-r2",Envelope=age=>(float)LightRayGeometry.Light(age)};}
  static void DestroySet(Set s){if(s==null)return;foreach(var d in s.Draws)if(d?.Mesh!=null)UnityEngine.Object.Destroy(d.Mesh);if(s.Root!=null){s.Root.gameObject.SetActive(false);UnityEngine.Object.Destroy(s.Root.gameObject);}}
  static Set CreateSet(Transform parent){var root=new GameObject("light-ray").transform;root.SetParent(parent,false);var geometry=LightRayGeometry.Create();var set=new Set{Root=root,Geometry=geometry,Draws=new Draw[geometry.Batches.Length],LightSpec=Spec()};try{for(int i=0;i<set.Draws.Length;i++){var data=geometry.Batches[i];var go=new GameObject("light-part-"+i);go.transform.SetParent(root,false);var mesh=new Mesh{name="light-ray-continuous-r2-"+i};var d=new Draw{Mesh=mesh};set.Draws[i]=d;mesh.MarkDynamic();d.Vertices=new Vector3[data.Vertices.Length];d.UV=new Vector2[data.Vertices.Length];d.Effect=new Vector2[data.Vertices.Length];d.Colors=new Color[data.Vertices.Length];mesh.vertices=d.Vertices;mesh.uv=d.UV;mesh.SetUVs(1,d.Effect);mesh.colors=d.Colors;mesh.triangles=data.Triangles;mesh.bounds=new Bounds(Vector3.zero,new Vector3(8,8,3));go.AddComponent<MeshFilter>().sharedMesh=mesh;d.Renderer=go.AddComponent<MeshRenderer>();d.Renderer.sharedMaterial=_material;d.Renderer.sortingOrder=i;d.Renderer.shadowCastingMode=ShadowCastingMode.Off;d.Renderer.receiveShadows=false;d.Renderer.enabled=false;}root.gameObject.SetActive(false);return set;}catch{DestroySet(set);throw;}}
  static void Retire(Slot s,int v){if(s.Version!=v)return;s.Busy=false;s.Light?.Cancel();s.Light=null;if(s.Root!=null)s.Root.gameObject.SetActive(false);}
  public static void Cancel(){_epoch++;foreach(var s in Pool)if(s!=null){Retire(s,s.Version);s.Version++;}}
  static Slot Acquire(){int limit=GfxQuality.Level==GfxLevel.Low?4:8;Slot s=null;for(int i=0;i<limit;i++){if(Pool[i]==null)Pool[i]=new Slot();if(!Pool[i].Busy||Pool[i].Root==null){s=Pool[i];break;}}if(s==null){s=Pool[0];for(int i=1;i<limit;i++)if(Pool[i].Serial<s.Serial)s=Pool[i];Recycled++;}Retire(s,s.Version);s.Version++;s.Serial=++_serial;if(s.Root==null){DestroySet(s.Active);s.Active=null;s.Root=new GameObject("light-ray-continuous-r2").transform;}if(s.Active==null)s.Active=CreateSet(s.Root);s.Active.Root.gameObject.SetActive(true);return s;}
  static bool Current(Slot s,int v,int epoch)=>epoch==_epoch&&s.Version==v&&s.Busy&&s.Root!=null;
  static void DrawAt(Slot s,float age,bool validateUpload=false){LightRayGeometry.Sample(s.Active.Geometry,age,s.Available);for(int j=0;j<s.Active.Draws.Length;j++){var d=s.Active.Draws[j];var data=s.Active.Geometry.Batches[j];bool visible=false;Vector3 lo=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue),hi=new Vector3(float.MinValue,float.MinValue,float.MinValue);for(int i=0;i<data.Vertices.Length;i++){var p=data.Vertices[i];var v=new Vector3((float)p.X,(float)p.Y,-(float)p.Z);if(!Finite(v))throw new InvalidOperationException("nonfinite_gpu_vertex");d.Vertices[i]=v;d.UV[i]=new Vector2((float)data.UV[i].X,(float)data.UV[i].Y);d.Effect[i]=new Vector2(1f,0);float a=(float)data.Alpha[i],gain=(float)data.Gain[i]*(data.Kind==LightRayGeometry.SolidKind?s.Particles:s.Emission);var c=data.Kind==LightRayGeometry.SolidKind?_roles[data.Role[i]]:Color.white;d.Colors[i]=new Color(c.r*gain,c.g*gain,c.b*gain,a);visible|=a>0;lo=new Vector3(Mathf.Min(lo.x,v.x),Mathf.Min(lo.y,v.y),Mathf.Min(lo.z,v.z));hi=new Vector3(Mathf.Max(hi.x,v.x),Mathf.Max(hi.y,v.y),Mathf.Max(hi.z,v.z));}d.Renderer.enabled=visible;if(!visible&&!validateUpload)continue;d.Mesh.SetVertices(d.Vertices);d.Mesh.SetUVs(0,d.UV);d.Mesh.SetUVs(1,d.Effect);d.Mesh.SetColors(d.Colors);d.Mesh.bounds=new Bounds((lo+hi)*.5f,hi-lo+Vector3.one*.001f);Props.Clear();Props.SetFloat("_Kind",data.Kind);d.Renderer.SetPropertyBlock(Props);}}
  static void Hide(Slot s){foreach(var d in s.Active.Draws)d.Renderer.enabled=false;}
  static IEnumerator Life(Slot s,int v,int epoch){try{while(Current(s,v,epoch)){if(s.Landed){float age=Time.time-s.Started;if(age>=LightRayGeometry.End())yield break;DrawAt(s,age);}else{float age=Time.time-s.Expected;if(age>=.35f)yield break;if(age<0)DrawAt(s,age);else Hide(s);}yield return null;}}finally{if(epoch==_epoch)Retire(s,v);}}
  static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);
  public static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket ticket,float? contactAt=null){
   if(!Eligible(ticket)||!ticket.HasWorld||ticket.Outcome!="hit"||ticket.Amount<=0)return null;
   if(contactAt.HasValue&&(float.IsNaN(contactAt.Value)||float.IsInfinity(contactAt.Value))){ticket.RenderFallbackReason="light_ray_invalid_contact_clock";return null;}
   if(float.IsNaN(ticket.ContactVisibleHeight)||float.IsInfinity(ticket.ContactVisibleHeight)||ticket.ContactVisibleHeight<=0||!Finite(ticket.Position)||!Finite(ticket.Feet)){ticket.RenderFallbackReason="light_ray_invalid_captured_geometry";return null;}
   var visual=ticket.Visual;float scale=ticket.ContactVisibleHeight*(visual?.Size??1);if(float.IsNaN(scale)||float.IsInfinity(scale)||scale<=0){ticket.RenderFallbackReason="light_ray_invalid_composed_scale";return null;}
   if(!Ready()){ticket.RenderFallbackReason=LastFailure;return null;}Slot s=null;try{
    s=Acquire();s.Busy=true;s.Landed=false;s.Height=ticket.ContactVisibleHeight;s.Expected=contactAt??Time.time;s.Available=Mathf.Min((float)LightRayGeometry.Lead(),Mathf.Max(0,s.Expected-Time.time));s.Size=visual?.Size??1;s.Emission=visual?.Emission??1;s.Particles=visual?.Particles??1;s.LightPower=visual?.Light??1;s.Tier=ticket.Intensity?.Tier;int v=s.Version,epoch=_epoch;
    s.Root.position=ticket.Position;s.Root.rotation=Stage.Camera.transform.rotation;s.Root.localScale=Vector3.one*scale;s.Root.gameObject.SetActive(true);ticket.OutcomeIncomingReason="captured_camera_local_light_passage_not_world_beam";
    var captured=s;var playback=new WorldBattleFx.AttackPlayback{Contact=()=>{if(!Current(captured,v,epoch)){StaleContacts++;return false;}if(!Tween.SchedulingAllowed||!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){ticket.RenderFallbackReason="light_ray_contact_environment_unavailable";Retire(captured,v);return false;}if(captured.Landed)return true;
     try{captured.Landed=true;captured.Started=Time.time;DrawAt(captured,0,true);if(Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact)){captured.Light=StageFx.BeginContractAttackLight(ticket.Target,ticket.Impact?.Target,epoch,ticket.Position,ticket.Feet,captured.Height,captured.Active.LightSpec,0,false,()=>Current(captured,v,epoch)&&Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact),1f,captured.LightPower,()=>Time.time-captured.Started);captured.Light?.Land();}ticket.AuthoredRenderer="continuous-light-ray-r2";return true;}catch(Exception e){Retire(captured,v);ticket.RenderFallbackReason="light_ray_contact_failed:"+e.GetType().Name;return false;}
    },Read=()=>Current(captured,v,epoch)?new{attack=ticket.AttackFamily,renderer="continuous-light-ray-r2",worldCasterBeam=false,age=captured.Landed?Time.time-captured.Started:Time.time-captured.Expected,landed=captured.Landed,height=captured.Height,size=captured.Size,damageTier=captured.Tier,version=v,serial=captured.Serial,anticipationUsed=captured.Available,visibleEnd=LightRayGeometry.End(),capturedCameraBasis=true}:null};
    Tween.RunPresentation(Life(s,v,epoch));if(!Current(s,v,epoch)){ticket.RenderFallbackReason="light_ray_scope_cancelled";return null;}Submitted++;return playback;
   }catch(Exception e){if(s!=null)Retire(s,s.Version);ticket.RenderFallbackReason="light_ray_construction_failed:"+e.GetType().Name;return null;}
  }
 }
}
