using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace DeckRogue.Game {
 // Authored shape/count/gain already encode intensity: never apply legacy tier scalars again.
 internal static class PhysicalSlashFx {
  sealed class Draw {public Mesh Mesh;public MeshRenderer Renderer;public Vector3[] Vertices;public Vector2[] UV;public Color[] Colors;}
  sealed class Set {public Transform Root;public Draw[] Draws;public PhysicalSlashGeometry.Buffer Geometry;}
  sealed class Slot {public Transform Root;public Set Small,Medium,Large,Active;public bool Busy,Landed,Mirror;public int Version;public long Serial;public float Started,Expected,Available,Height;public string Tier;public StageFx.ContractLightPlayback Light;}
  static readonly Slot[] Pool=new Slot[8];static int _epoch;static long _serial;
  static Texture2D _texture;static Material _material;static Color _core;static WorldBattleFx.AttackSpec _smallLight,_mediumLight,_largeLight;static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  public static string LastFailure{get;private set;}public static int Submitted{get;private set;}public static int Recycled{get;private set;}public static int StaleContacts{get;private set;}
  public static int ActiveCount{get{int n=0;foreach(var s in Pool)if(s!=null&&s.Busy)n++;return n;}}
  public static bool Eligible(ResolvedCombatFx.Ticket t)=>t!=null&&t.AttackFamily=="slash"&&t.Element=="physical";
  public static string SelectedTier(ResolvedCombatFx.Ticket t)=>t?.FixedSmallContact==true || t?.AuthoredPresentation=="small"?"small":PhysicalSlashGeometry.ResolveTier(t?.Intensity?.Tier);
  public static float Lead(ResolvedCombatFx.Ticket t)=>(float)PhysicalSlashGeometry.Lead(SelectedTier(t));
  static Color Role(JToken token){if(token==null||!ColorUtility.TryParseHtmlString((string)token,out var c))throw new FormatException("invalid physical role");return c.linear;}
  public static bool Ready(){
   LastFailure=null;if(!Tween.SchedulingAllowed){LastFailure="slash_tier_scope_cancelled";return false;}
   if(!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){LastFailure="slash_tier_world_camera_unavailable";return false;}
   if(ElementBattleFx.ForceMissingForTest){LastFailure="slash_tier_forced_resource_missing";return false;}
   if(QualitySettings.activeColorSpace!=ColorSpace.Linear){LastFailure="slash_tier_requires_linear_project";return false;}
   if(_material!=null&&_texture!=null)return true;
   var shader=Shader.Find("DeckRogue/PhysicalSlashFx");var texture=Resources.Load<Texture2D>("Art/fx/vine-slash-surface-r1");var palette=Resources.Load<TextAsset>("Data/physical-contact-palette-r1");
   if(shader==null||!shader.isSupported||texture==null||texture.width!=2172||texture.height!=724||palette==null){LastFailure="slash_tier_shader_or_resource_unavailable";return false;}
   try{var json=JObject.Parse(palette.text);var core=Role(json["roles_srgb"]?["core"]);var light=Role(json["scene_light_srgb"]);var material=new Material(shader);try{material.SetTexture("_BaseMap",texture);}catch{UnityEngine.Object.Destroy(material);throw;}
    _core=core;_texture=texture;_material=material;_smallLight=Spec("small",light,.35f*28/48,.12f*8/20);_mediumLight=Spec("medium",light,.35f,.12f);_largeLight=Spec("large",light,.35f*60/48,.12f*24/20);return true;
   }catch(Exception e){LastFailure="slash_tier_palette_invalid:"+e.GetType().Name;return false;}
  }
  static WorldBattleFx.AttackSpec Spec(string tier,Color color,float target,float floor)=>new WorldBattleFx.AttackSpec(new[]{0f,0f,0f,0f,0f,0f,0f,.080f,(float)PhysicalSlashGeometry.End(tier)},1,1,1,color,color,0f,0f,0f){ContractLighting=true,StandardContractPower=true,TargetPeakMultiplier=target,FloorPeakMultiplier=floor,MaxCombineGroup="physical-contact-r1",Envelope=age=>(float)PhysicalSlashGeometry.Light(tier,age)};
  static Set CreateSet(Transform parent,string tier){
   var root=new GameObject("physical-slash-"+tier).transform;root.SetParent(parent,false);var geometry=PhysicalSlashGeometry.Create(tier);var set=new Set{Root=root,Geometry=geometry,Draws=new Draw[geometry.Batches.Length]};
   try{for(int i=0;i<set.Draws.Length;i++){var data=geometry.Batches[i];if(data.Vertices.Length==0)continue;var go=new GameObject("slash-tier-part-"+i);go.transform.SetParent(root,false);var mesh=new Mesh{name="physical-slash-r1-"+tier+"-"+i};var d=new Draw{Mesh=mesh};set.Draws[i]=d;mesh.MarkDynamic();d.Vertices=new Vector3[data.Vertices.Length];d.UV=new Vector2[data.Vertices.Length];d.Colors=new Color[data.Vertices.Length];mesh.vertices=d.Vertices;mesh.uv=d.UV;mesh.colors=d.Colors;mesh.triangles=data.Triangles;mesh.bounds=new Bounds(Vector3.zero,new Vector3(8,8,2));go.AddComponent<MeshFilter>().sharedMesh=mesh;d.Renderer=go.AddComponent<MeshRenderer>();d.Renderer.sharedMaterial=_material;d.Renderer.sortingOrder=i;d.Renderer.shadowCastingMode=ShadowCastingMode.Off;d.Renderer.receiveShadows=false;d.Renderer.enabled=false;}root.gameObject.SetActive(false);return set;}catch{DestroySet(set);throw;}
  }
  static void DestroySet(Set s){if(s==null)return;foreach(var d in s.Draws)if(d?.Mesh!=null)UnityEngine.Object.Destroy(d.Mesh);if(s.Root!=null){s.Root.gameObject.SetActive(false);UnityEngine.Object.Destroy(s.Root.gameObject);}}
  static void Retire(Slot s,int v){if(s.Version!=v)return;s.Busy=false;s.Light?.Cancel();s.Light=null;if(s.Root!=null)s.Root.gameObject.SetActive(false);}
  public static void Cancel(){_epoch++;foreach(var s in Pool)if(s!=null){Retire(s,s.Version);s.Version++;}}
  static Slot Acquire(string tier){int limit=GfxQuality.Level==GfxLevel.Low?4:8;Slot s=null;for(int i=0;i<limit;i++){if(Pool[i]==null)Pool[i]=new Slot();if(!Pool[i].Busy||Pool[i].Root==null){s=Pool[i];break;}}if(s==null){s=Pool[0];for(int i=1;i<limit;i++)if(Pool[i].Serial<s.Serial)s=Pool[i];Recycled++;}Retire(s,s.Version);s.Version++;s.Serial=++_serial;
   if(s.Root==null){DestroySet(s.Small);DestroySet(s.Medium);DestroySet(s.Large);s.Small=s.Medium=s.Large=null;s.Root=new GameObject("physical-slash-tiers-r1").transform;}
   if(tier=="small"){if(s.Small==null)s.Small=CreateSet(s.Root,tier);s.Active=s.Small;}else if(tier=="large"){if(s.Large==null)s.Large=CreateSet(s.Root,tier);s.Active=s.Large;}else{if(s.Medium==null)s.Medium=CreateSet(s.Root,tier);s.Active=s.Medium;}
   if(s.Small?.Root!=null)s.Small.Root.gameObject.SetActive(tier=="small");if(s.Medium?.Root!=null)s.Medium.Root.gameObject.SetActive(tier=="medium");if(s.Large?.Root!=null)s.Large.Root.gameObject.SetActive(tier=="large");return s;
  }
  static bool Current(Slot s,int v,int epoch)=>epoch==_epoch&&s.Version==v&&s.Busy&&s.Root!=null;
  static void DrawAt(Slot s,float age){PhysicalSlashGeometry.Sample(s.Active.Geometry,age,s.Mirror,s.Available);for(int j=0;j<s.Active.Draws.Length;j++){var d=s.Active.Draws[j];if(d==null)continue;var data=s.Active.Geometry.Batches[j];bool visible=false;Vector3 lo=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue),hi=new Vector3(float.MinValue,float.MinValue,float.MinValue);for(int i=0;i<data.Vertices.Length;i++){var p=data.Vertices[i];var v=new Vector3((float)p.X,(float)p.Y,-(float)p.Z);d.Vertices[i]=v;d.UV[i]=new Vector2((float)data.UV[i].X,(float)data.UV[i].Y);float a=(float)data.Alpha[i],gain=(float)data.Gain[i];var c=data.Kind==0?Color.white:_core;d.Colors[i]=new Color(c.r*gain,c.g*gain,c.b*gain,a);visible|=a>0;lo=new Vector3(Mathf.Min(lo.x,v.x),Mathf.Min(lo.y,v.y),Mathf.Min(lo.z,v.z));hi=new Vector3(Mathf.Max(hi.x,v.x),Mathf.Max(hi.y,v.y),Mathf.Max(hi.z,v.z));}d.Renderer.enabled=visible;if(!visible)continue;d.Mesh.SetVertices(d.Vertices);d.Mesh.SetUVs(0,d.UV);d.Mesh.SetColors(d.Colors);d.Mesh.bounds=new Bounds((lo+hi)*.5f,hi-lo+Vector3.one*.001f);Props.Clear();Props.SetFloat("_UseTexture",data.Kind==0?1:0);d.Renderer.SetPropertyBlock(Props);}}
  static void Hide(Slot s){foreach(var d in s.Active.Draws)if(d!=null)d.Renderer.enabled=false;}
  static IEnumerator Life(Slot s,int v,int epoch){try{while(Current(s,v,epoch)){if(s.Landed){float age=Time.time-s.Started;if(age>=PhysicalSlashGeometry.End(s.Tier))yield break;DrawAt(s,age);}else{float age=Time.time-s.Expected;if(age>=.35f)yield break;if(age<0)DrawAt(s,age);else Hide(s);}yield return null;}}finally{if(epoch==_epoch)Retire(s,v);}}
  static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);
  public static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket ticket,float? contactAt=null){
   if(!Eligible(ticket)||!ticket.HasWorld||ticket.Outcome!="hit"||ticket.Amount<=0)return null;
   if(contactAt.HasValue&&(float.IsNaN(contactAt.Value)||float.IsInfinity(contactAt.Value))){ticket.RenderFallbackReason="slash_tier_invalid_contact_clock";return null;}
   if(float.IsNaN(ticket.ContactVisibleHeight)||float.IsInfinity(ticket.ContactVisibleHeight)||ticket.ContactVisibleHeight<=0||!Finite(ticket.Position)||!Finite(ticket.Feet)){ticket.RenderFallbackReason="slash_tier_invalid_captured_geometry";return null;}
   if(!Ready()){ticket.RenderFallbackReason=LastFailure;return null;}Slot s=null;try{
    string tier=SelectedTier(ticket);s=Acquire(tier);s.Busy=true;s.Landed=false;s.Tier=tier;s.Height=ticket.ContactVisibleHeight;s.Expected=contactAt??Time.time;s.Available=Mathf.Min((float)PhysicalSlashGeometry.Lead(tier),Mathf.Max(0,s.Expected-Time.time));s.Mirror=ticket.Index%2!=0;int v=s.Version,epoch=_epoch;
    ticket.AuthoredTier=tier;ticket.AuthoredTierReason=ticket.FixedSmallContact?"noncard_fixed_small_contact":ticket.AuthoredPresentation=="small"?"semantic_small_cap":ticket.Intensity?.Tier==tier?"frozen_damage_tier":"unknown_tier_medium_fallback";
    s.Root.position=ticket.Position;s.Root.rotation=Stage.Camera.transform.rotation;s.Root.localScale=Vector3.one*s.Height;s.Root.gameObject.SetActive(true);var captured=s;
    var playback=new WorldBattleFx.AttackPlayback{Contact=()=>{if(!Current(captured,v,epoch)){StaleContacts++;return false;}if(!Tween.SchedulingAllowed||!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){ticket.RenderFallbackReason="slash_tier_contact_environment_unavailable";Retire(captured,v);return false;}if(captured.Landed)return true;
     try{captured.Landed=true;captured.Started=Time.time;DrawAt(captured,0);if(Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact)){var spec=tier=="small"?_smallLight:tier=="large"?_largeLight:_mediumLight;captured.Light=StageFx.BeginContractAttackLight(ticket.Target,ticket.Impact?.Target,epoch,ticket.Position,ticket.Feet,captured.Height,spec,0,false,()=>Current(captured,v,epoch)&&Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact),1f,null,()=>Time.time-captured.Started);captured.Light?.Land();}ticket.AuthoredRenderer="continuous-physical-slash-tiers-r1";return true;}catch(Exception e){Retire(captured,v);ticket.RenderFallbackReason="slash_tier_contact_failed:"+e.GetType().Name;return false;}
    },Read=()=>Current(captured,v,epoch)?new{attack="slash",renderer="continuous-physical-slash-tiers-r1",requestedTier=ticket.Intensity?.Tier,tier=captured.Tier,tierReason=ticket.AuthoredTierReason,age=captured.Landed?Time.time-captured.Started:Time.time-captured.Expected,landed=captured.Landed,height=captured.Height,mirror=captured.Mirror,version=v,serial=captured.Serial,anticipationUsed=captured.Available,visibleEnd=PhysicalSlashGeometry.End(captured.Tier),legacyScalarApplied=false}:null};
    Tween.RunPresentation(Life(s,v,epoch));if(!Current(s,v,epoch)){ticket.RenderFallbackReason="slash_tier_scope_cancelled";return null;}Submitted++;return playback;
   }catch(Exception e){if(s!=null)Retire(s,s.Version);ticket.RenderFallbackReason="slash_tier_construction_failed:"+e.GetType().Name;return null;}
  }
 }
}
