using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace DeckRogue.Game {
 // Two explicit physical card families. The existing receipt owns actual contact, never predicted HP.
 internal static class PlayerContactFx {
  sealed class Draw {public Mesh Mesh;public MeshRenderer Renderer;public Vector3[] Vertices;public Vector2[] UV;public Color[] Colors;}
  sealed class Set {public Transform Root;public Draw[] Draws;public PlayerContactGeometry.Buffer Geometry;}
  sealed class Slot {public Transform Root;public Set Pierce,Meteor,Active;public int Version;public long Serial;public bool Busy,Landed;public float Started,Expected,Available,Height,Size,Emission,Particles;public double SourceX,SourceY;public string Family,Tier;public StageFx.ContractLightPlayback Light;}
  // Separate bounded delivery pool: low4/high8 slots, <=32 active draws, <=56 resident lazy meshes.
  static readonly Slot[] Pool=new Slot[8];static int _epoch;static long _serial;
  static Material _material;static Texture2D _pierce,_rock,_blunt;static Color[] _roles;static WorldBattleFx.AttackSpec _pierceLight,_meteorLight;static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  public static string LastFailure{get;private set;}public static int Submitted{get;private set;}public static int Recycled{get;private set;}public static int StaleContacts{get;private set;}
  public static int ActiveCount{get{int n=0;foreach(var s in Pool)if(s!=null&&s.Busy)n++;return n;}}
  public static bool IsFamily(string attack)=>attack=="pierce_contact"||attack=="physical_meteor";
  public static string Family(string attack)=>attack=="pierce_contact"?"pierce":"meteor";
  static Color Role(JToken token){if(token==null||!ColorUtility.TryParseHtmlString((string)token,out var c))throw new FormatException("invalid physical role");return c.linear;}
  static Vector4 Vector(Color c)=>new Vector4(c.r,c.g,c.b,1);
  public static bool Ready(string attack){
   LastFailure=null;if(!IsFamily(attack)){LastFailure="delivery_family_unmapped";return false;}
   if(!Tween.SchedulingAllowed){LastFailure="delivery_scope_cancelled";return false;}
   if(!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){LastFailure="delivery_world_camera_unavailable";return false;}
   if(ElementBattleFx.ForceMissingForTest){LastFailure="delivery_forced_resource_missing";return false;}
   if(QualitySettings.activeColorSpace!=ColorSpace.Linear){LastFailure="delivery_requires_linear_project";return false;}
   if(attack=="pierce_contact"){
    if(_pierce==null)_pierce=Resources.Load<Texture2D>("Art/fx/physical-pierce-surface-r1");
    if(_pierce==null||_pierce.width!=1774||_pierce.height!=887){LastFailure="delivery_pierce_resource_missing_or_resized";return false;}
   }else{
    if(_rock==null)_rock=Resources.Load<Texture2D>("Art/fx/physical-meteor-intact-r1");if(_blunt==null)_blunt=Resources.Load<Texture2D>("Art/fx/enemy-blunt-contact-r1");
    if(_rock==null||_rock.width!=1254||_rock.height!=1254||_blunt==null||_blunt.width!=1774||_blunt.height!=887){LastFailure="delivery_meteor_resource_missing_or_resized";return false;}
   }
   if(_material!=null)return true;var shader=Shader.Find("DeckRogue/PlayerContactFx");var palette=Resources.Load<TextAsset>("Data/physical-contact-palette-r1");
   if(shader==null||!shader.isSupported||palette==null){LastFailure="delivery_shader_or_palette_unavailable";return false;}
   try{
    var json=JObject.Parse(palette.text);var roles=json["roles_srgb"];var shadow=Role(roles?["shadow"]);var primary=Role(roles?["primary"]);var edge=Role(roles?["edge"]);var core=Role(roles?["core"]);var light=Role(json["scene_light_srgb"]);var material=new Material(shader);
    try{material.SetVector("_RoleShadow",Vector(shadow));material.SetVector("_RolePrimary",Vector(primary));material.SetVector("_RoleEdge",Vector(edge));material.SetVector("_RoleCore",Vector(core));}catch{UnityEngine.Object.Destroy(material);throw;}
    _roles=new[]{shadow,primary,edge,core};_material=material;_pierceLight=Spec(light,"pierce");_meteorLight=Spec(light,"meteor");return true;
   }catch(Exception e){LastFailure="delivery_palette_invalid:"+e.GetType().Name;return false;}
  }
  static WorldBattleFx.AttackSpec Spec(Color color,string family)=>new WorldBattleFx.AttackSpec(new[]{0f,0f,0f,0f,0f,0f,0f,.080f,(float)PlayerContactGeometry.End(family)},1,1,1,color,color,.015f,0,0){ContractLighting=true,StandardContractPower=true,TargetPeakMultiplier=.35f,FloorPeakMultiplier=.12f,MaxCombineGroup="physical-contact-r1",Envelope=age=>(float)PlayerContactGeometry.Light(family,age)};
  static Set CreateSet(Transform parent,string family){
   var root=new GameObject("physical-delivery-"+family).transform;root.SetParent(parent,false);var geometry=PlayerContactGeometry.Create(family);var set=new Set{Root=root,Geometry=geometry,Draws=new Draw[geometry.Batches.Length]};
   try{for(int i=0;i<set.Draws.Length;i++){
    var data=geometry.Batches[i];var go=new GameObject("delivery-part-"+i);go.transform.SetParent(root,false);var mesh=new Mesh{name="physical-delivery-r1-"+family+"-"+i};var d=new Draw{Mesh=mesh};set.Draws[i]=d;mesh.MarkDynamic();d.Vertices=new Vector3[data.Vertices.Length];d.UV=new Vector2[data.Vertices.Length];d.Colors=new Color[data.Vertices.Length];mesh.vertices=d.Vertices;mesh.uv=d.UV;mesh.colors=d.Colors;mesh.triangles=data.Triangles;mesh.bounds=new Bounds(Vector3.zero,new Vector3(6,6,3));go.AddComponent<MeshFilter>().sharedMesh=mesh;d.Renderer=go.AddComponent<MeshRenderer>();d.Renderer.sharedMaterial=_material;d.Renderer.sortingOrder=family=="meteor"?(i==2?0:i==0?1:i==1?2:3):i;d.Renderer.shadowCastingMode=ShadowCastingMode.Off;d.Renderer.receiveShadows=false;d.Renderer.enabled=false;
   }root.gameObject.SetActive(false);return set;}catch{DestroySet(set);throw;}
  }
  static void DestroySet(Set s){if(s==null)return;foreach(var d in s.Draws)if(d?.Mesh!=null)UnityEngine.Object.Destroy(d.Mesh);if(s.Root!=null){s.Root.gameObject.SetActive(false);UnityEngine.Object.Destroy(s.Root.gameObject);}}
  static void Retire(Slot s,int v){if(s.Version!=v)return;s.Busy=false;s.Light?.Cancel();s.Light=null;if(s.Root!=null)s.Root.gameObject.SetActive(false);}
  public static void Cancel(){_epoch++;foreach(var s in Pool)if(s!=null){Retire(s,s.Version);s.Version++;}}
  static Slot Acquire(string family){
   int limit=GfxQuality.Level==GfxLevel.Low?4:8;Slot s=null;for(int i=0;i<limit;i++){if(Pool[i]==null)Pool[i]=new Slot();if(!Pool[i].Busy||Pool[i].Root==null){s=Pool[i];break;}}
   if(s==null){s=Pool[0];for(int i=1;i<limit;i++)if(Pool[i].Serial<s.Serial)s=Pool[i];Recycled++;}Retire(s,s.Version);s.Version++;s.Serial=++_serial;
   if(s.Root==null){DestroySet(s.Pierce);DestroySet(s.Meteor);s.Pierce=s.Meteor=null;s.Root=new GameObject("physical-delivery-r1").transform;}
   if(family=="pierce"){if(s.Pierce==null)s.Pierce=CreateSet(s.Root,family);s.Active=s.Pierce;}else{if(s.Meteor==null)s.Meteor=CreateSet(s.Root,family);s.Active=s.Meteor;}
   if(s.Pierce?.Root!=null)s.Pierce.Root.gameObject.SetActive(family=="pierce");if(s.Meteor?.Root!=null)s.Meteor.Root.gameObject.SetActive(family=="meteor");return s;
  }
  static bool Current(Slot s,int v,int epoch)=>_epoch==epoch&&s.Version==v&&s.Busy&&s.Root!=null;
  static void DrawAt(Slot s,float age){
   PlayerContactGeometry.Sample(s.Active.Geometry,PlayerContactGeometry.Clock(age,s.Available));PlayerContactGeometry.FitIncoming(s.Active.Geometry,s.SourceX,s.SourceY);
   for(int j=0;j<s.Active.Draws.Length;j++){
    var data=s.Active.Geometry.Batches[j];var d=s.Active.Draws[j];bool visible=false;Vector3 lo=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue),hi=new Vector3(float.MinValue,float.MinValue,float.MinValue);
    for(int i=0;i<data.Vertices.Length;i++){var p=data.Vertices[i];var v=new Vector3((float)p.X,(float)p.Y,-(float)p.Z);d.Vertices[i]=v;d.UV[i]=new Vector2((float)data.UV[i].X,(float)data.UV[i].Y);float a=(float)data.Alpha[i],gain=(float)data.Gain[i]*(data.Particle[i]?s.Particles:s.Emission);var c=data.Kind==0?_roles[data.Role[i]]:Color.white;d.Colors[i]=new Color(c.r*gain,c.g*gain,c.b*gain,a);visible|=a>0;lo=new Vector3(Mathf.Min(lo.x,v.x),Mathf.Min(lo.y,v.y),Mathf.Min(lo.z,v.z));hi=new Vector3(Mathf.Max(hi.x,v.x),Mathf.Max(hi.y,v.y),Mathf.Max(hi.z,v.z));}
    d.Renderer.enabled=visible;if(!visible)continue;d.Mesh.SetVertices(d.Vertices);d.Mesh.SetUVs(0,d.UV);d.Mesh.SetColors(d.Colors);d.Mesh.bounds=new Bounds((lo+hi)*.5f,hi-lo+Vector3.one*.001f);Props.Clear();Props.SetFloat("_Kind",data.Kind);Props.SetTexture("_BaseMap",data.Kind==1?_pierce:data.Kind==2?_rock:_blunt);d.Renderer.SetPropertyBlock(Props);
   }
  }
  static float LightAge(Slot s)=>s.Landed?Time.time-s.Started:(float)PlayerContactGeometry.End(s.Family);
  static void Hide(Slot s){foreach(var d in s.Active.Draws)d.Renderer.enabled=false;}
  static IEnumerator Life(Slot s,int v,int epoch){try{while(Current(s,v,epoch)){if(s.Landed){float age=Time.time-s.Started;if(age>=PlayerContactGeometry.End(s.Family))yield break;DrawAt(s,age);}else{float age=Time.time-s.Expected;if(age>=.35f)yield break;if(age<0)DrawAt(s,age);else Hide(s);}yield return null;}}finally{if(epoch==_epoch)Retire(s,v);}}
  static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);
  static double Dot(Vector3 a,Vector3 b)=>(double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z;
  public static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket ticket,float? contactAt=null){
   if(ticket==null||!ticket.HasWorld||ticket.Outcome!="hit"||ticket.Amount<=0||!IsFamily(ticket.AttackFamily))return null;
   if(contactAt.HasValue&&(float.IsNaN(contactAt.Value)||float.IsInfinity(contactAt.Value))){ticket.RenderFallbackReason="delivery_invalid_contact_clock";return null;}
   if(float.IsNaN(ticket.ContactVisibleHeight)||float.IsInfinity(ticket.ContactVisibleHeight)||ticket.ContactVisibleHeight<=0||!Finite(ticket.Position)||!Finite(ticket.Feet)){ticket.RenderFallbackReason="delivery_invalid_captured_geometry";return null;}
   float composedScale=ticket.ContactVisibleHeight*(ticket.Visual?.Size??1);if(float.IsNaN(composedScale)||float.IsInfinity(composedScale)||composedScale<=0){ticket.RenderFallbackReason="delivery_invalid_composed_scale";return null;}
   if(!Ready(ticket.AttackFamily)){ticket.RenderFallbackReason=LastFailure;return null;}
   Slot s=null;try{
    string family=Family(ticket.AttackFamily);s=Acquire(family);s.Busy=true;s.Landed=false;s.Family=family;s.Expected=contactAt??Time.time;s.Available=Mathf.Min((float)PlayerContactGeometry.Anticipation,Mathf.Max(0,s.Expected-Time.time));s.Height=ticket.ContactVisibleHeight;s.Size=ticket.Visual?.Size??1;s.Emission=ticket.Visual?.Emission??1;s.Particles=ticket.Visual?.Particles??1;s.Tier=ticket.Intensity?.Tier;int v=s.Version,epoch=_epoch;
    s.Root.position=ticket.Position;s.Root.rotation=Stage.Camera.transform.rotation;s.Root.localScale=Vector3.one*composedScale;s.Root.gameObject.SetActive(true);s.SourceX=double.NaN;s.SourceY=0;
    if(ticket.HasBeamOrigin){var delta=ticket.BeamOrigin-ticket.Position;s.SourceX=Dot(delta,s.Root.rotation*Vector3.right)/s.Height;s.SourceY=Dot(delta,s.Root.rotation*Vector3.up)/s.Height;}
    double span=Math.Sqrt(s.SourceX*s.SourceX+s.SourceY*s.SourceY);bool directionValid=!double.IsNaN(span)&&!double.IsInfinity(span)&&span>1e-5;ticket.OutcomeIncomingReason=family=="meteor"?"target_local_camera_up":directionValid?"captured_source_direction":"camera_right_direction_fallback";if(family=="pierce"&&!directionValid&&ticket.RenderFallbackReason==null)ticket.RenderFallbackReason="delivery_direction_unavailable_camera_right_fallback";
    var captured=s;var playback=new WorldBattleFx.AttackPlayback{Contact=()=>{
     if(!Current(captured,v,epoch)){StaleContacts++;return false;}if(!Tween.SchedulingAllowed||!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){ticket.RenderFallbackReason="delivery_contact_environment_unavailable";Retire(captured,v);return false;}if(captured.Landed)return true;
     try{captured.Landed=true;captured.Started=Time.time;DrawAt(captured,0);
      // No precontact contribution or slot ownership. Actual receipt alone enables the light.
      if(Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact)){captured.Light=StageFx.BeginContractAttackLight(ticket.Target,ticket.Impact?.Target,epoch,ticket.Position,ticket.Feet,captured.Height,family=="pierce"?_pierceLight:_meteorLight,0,false,()=>Current(captured,v,epoch)&&Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact),1f,ticket.Visual?.Light,()=>LightAge(captured));captured.Light?.Land();}
      return true;
     }catch(Exception e){Retire(captured,v);ticket.RenderFallbackReason="delivery_contact_failed:"+e.GetType().Name;return false;}
    },Read=()=>Current(captured,v,epoch)?new{attack=ticket.AttackFamily,renderer="continuous-physical-delivery-r1",age=captured.Landed?Time.time-captured.Started:Time.time-captured.Expected,landed=captured.Landed,height=captured.Height,damageTier=captured.Tier,version=v,serial=captured.Serial,anticipationUsed=captured.Available,visibleEnd=PlayerContactGeometry.End(captured.Family),predictedContactDoesNotCommit=true}:null};
    Tween.RunPresentation(Life(s,v,epoch));if(!Current(s,v,epoch)){ticket.RenderFallbackReason="delivery_scope_cancelled";return null;}Submitted++;return playback;
   }catch(Exception e){if(s!=null)Retire(s,s.Version);ticket.RenderFallbackReason="delivery_construction_failed:"+e.GetType().Name;return null;}
  }
 }
}
