using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using G=DeckRogue.Game.BoundedContactGeometry;
namespace DeckRogue.Game {
 // Shared compact semantic adapters. No transport, anticipation, gameplay, RNG or actor work.
 internal static class BoundedContactFx {
  sealed class Slot {
   public Transform Root;public Mesh Mesh;public MeshRenderer Renderer;public G.Buffer Geometry;
   public Vector3[] Vertices;public Vector2[] UV,Mode;public Color[] Colors;
   public bool Busy,Landed;public int Version,Particles;public long Serial;public float Started,Expected,Emission;public G.Profile Profile;public ResolvedCombatFx.Ticket Ticket;
  }
  static readonly Slot[] Pool=new Slot[24];static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  static Material _material;static Color[] _primary,_core;static int _epoch;static long _serial;
  public static string LastFailure {get;private set;}
  public static int Submitted {get;private set;}public static int Recycled {get;private set;}public static int StaleContacts {get;private set;}
  public static int ActiveCount {get{int n=0;foreach(var s in Pool)if(s!=null&&s.Busy)n++;return n;}}
  public static bool IsFamily(string family)=>G.Find(family)!=null;
  public static bool Eligible(ResolvedCombatFx.Ticket ticket)=>ticket!=null&&IsFamily(ticket.AttackFamily);
  public static float Duration(string family)=>IsFamily(family)?(float)G.End:0f;
  static Color ColorRole(JToken t){if(t==null||!ColorUtility.TryParseHtmlString((string)t,out var c))throw new FormatException("invalid contact palette");return c.linear;}
  public static bool Ready(){
   LastFailure=null;
   if(!Tween.SchedulingAllowed){LastFailure="bounded_contact_scope_cancelled";return false;}
   if(!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){LastFailure="bounded_contact_world_camera_unavailable";return false;}
   if(ElementBattleFx.ForceMissingForTest){LastFailure="bounded_contact_forced_resource_missing";return false;}
   if(QualitySettings.activeColorSpace!=ColorSpace.Linear){LastFailure="bounded_contact_requires_linear_project";return false;}
   if(_material!=null)return true;
   Material material=null;
   try {
    var shader=Shader.Find("DeckRogue/BoundedContactFx");var asset=Resources.Load<TextAsset>("Data/bounded-contact-palette-r1");
    if(shader==null||!shader.isSupported||asset==null){LastFailure="bounded_contact_shader_or_palette_unavailable";return false;}
    var j=JObject.Parse(asset.text);if((string)j["schema"]!="deckrogue.bounded-contact-palette/1")throw new FormatException("invalid palette schema");
    var primary=new Color[8];var core=new Color[8];
    foreach(G.Surface s in Enum.GetValues(typeof(G.Surface))){var e=j["surfaces"]?[s.ToString()];primary[(int)s]=ColorRole(e?["primary"]);core[(int)s]=ColorRole(e?["core"]);}
    // Neutral surface must stay genuinely achromatic, including its soft edge.
    foreach(var c in new[]{primary[0],core[0]})if(Mathf.Abs(c.r-c.g)>1e-6f||Mathf.Abs(c.g-c.b)>1e-6f)throw new FormatException("neutral_palette_not_achromatic");
    material=new Material(shader);_primary=primary;_core=core;_material=material;return true;
   }catch(Exception e){if(material!=null)UnityEngine.Object.Destroy(material);LastFailure="bounded_contact_resource_failed:"+e.GetType().Name;return false;}
  }
  static bool Finite(Vector3 p)=>G.Finite(p.x)&&G.Finite(p.y)&&G.Finite(p.z);
  static bool CaptureValid(Vector3 point,float scale,Quaternion basis){
   var r=basis*Vector3.right;var u=basis*Vector3.up;var f=basis*Vector3.forward;
   if(!Finite(r)||!Finite(u)||!Finite(f))return false;
   Func<Vector3,Vector3,double> dot=(a,b)=>(double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z;
   if(Math.Abs(dot(r,r)-1)>1e-4||Math.Abs(dot(u,u)-1)>1e-4||Math.Abs(dot(f,f)-1)>1e-4||Math.Abs(dot(r,u))>1e-4||Math.Abs(dot(r,f))>1e-4||Math.Abs(dot(u,f))>1e-4)return false;
   for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)if(!Finite(point+basis*(new Vector3(x*.5f,y*.5f,0)*scale)))return false;return true;
  }
  static void Retire(Slot s,int version){if(s==null||s.Version!=version)return;s.Busy=false;if(s.Renderer!=null)s.Renderer.enabled=false;if(s.Root!=null)s.Root.gameObject.SetActive(false);}
  public static void Cancel(){_epoch++;foreach(var s in Pool)if(s!=null){Retire(s,s.Version);s.Version++;}}
  static Slot Acquire(){
   int limit=GfxQuality.Level==GfxLevel.Low?12:24;Slot slot=null;
   for(int i=limit;i<Pool.Length;i++)if(Pool[i]?.Busy==true){Retire(Pool[i],Pool[i].Version);Pool[i].Version++;}
   for(int i=0;i<limit;i++){if(Pool[i]==null)Pool[i]=new Slot();if(!Pool[i].Busy||Pool[i].Root==null){slot=Pool[i];break;}}
   if(slot==null){slot=Pool[0];for(int i=1;i<limit;i++)if(Pool[i].Serial<slot.Serial)slot=Pool[i];Recycled++;}
   Retire(slot,slot.Version);slot.Version++;slot.Serial=++_serial;
   if(slot.Root==null){
    if(slot.Mesh!=null)UnityEngine.Object.Destroy(slot.Mesh);
    var go=new GameObject("bounded-contact-r1");Mesh mesh=null;
    try{mesh=new Mesh{name="bounded-contact-r1"};mesh.MarkDynamic();slot.Geometry=new G.Buffer();int n=G.MaxQuads*4;slot.Vertices=new Vector3[n];slot.UV=new Vector2[n];slot.Mode=new Vector2[n];slot.Colors=new Color[n];
     mesh.vertices=slot.Vertices;mesh.uv=slot.UV;mesh.uv2=slot.Mode;mesh.colors=slot.Colors;mesh.triangles=slot.Geometry.Triangles;mesh.bounds=new Bounds(Vector3.zero,Vector3.one);
     go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=_material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enabled=false;
     slot.Root=go.transform;slot.Mesh=mesh;slot.Renderer=renderer;
    }catch{if(mesh!=null)UnityEngine.Object.Destroy(mesh);go.SetActive(false);UnityEngine.Object.Destroy(go);throw;}
   }
   return slot;
  }
  static bool Current(Slot s,int version,int epoch)=>s!=null&&s.Busy&&s.Version==version&&epoch==_epoch&&s.Root!=null;
  static void Draw(Slot s,float age){
   G.Sample(s.Geometry,s.Profile,age,s.Particles);var b=s.Geometry;bool visible=false;
   for(int i=0;i<s.Vertices.Length;i++){
    if(!G.Finite(b.X[i])||!G.Finite(b.Y[i])||!G.Finite(b.Alpha[i]))throw new InvalidOperationException("nonfinite_gpu_input");
    s.Vertices[i]=new Vector3((float)b.X[i],(float)b.Y[i],0);s.UV[i]=new Vector2((float)b.U[i],(float)b.V[i]);s.Mode[i]=new Vector2((float)b.Soft[i],0);s.Colors[i]=new Color(s.Emission,s.Emission,s.Emission,(float)b.Alpha[i]);visible|=b.Alpha[i]>0;
   }
   s.Mesh.SetVertices(s.Vertices);s.Mesh.SetUVs(0,s.UV);s.Mesh.SetUVs(1,s.Mode);s.Mesh.SetColors(s.Colors);s.Renderer.enabled=visible;
   // Palette values are already linear. SetColor would decode sRGB a second time.
   var primary=_primary[(int)s.Profile.Material];var core=_core[(int)s.Profile.Material];
   Props.Clear();Props.SetVector("_Primary",new Vector4(primary.r,primary.g,primary.b,primary.a));Props.SetVector("_Core",new Vector4(core.r,core.g,core.b,core.a));s.Renderer.SetPropertyBlock(Props);
  }
  static IEnumerator Life(Slot s,int version,int epoch){
   try{while(Current(s,version,epoch)){
    if(s.Landed){float age=Time.time-s.Started;if(age>=(float)G.End)yield break;Draw(s,age);}else if(Time.time-s.Expected>.25f)yield break;yield return null;
   }}finally{if(epoch==_epoch)Retire(s,version);}
  }
  public static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket t,float? contactAt=null)=>BeginCore(t,contactAt,false);
  // A display fallback never certifies source metadata or assigns an attack family.
  public static WorldBattleFx.AttackPlayback BeginNeutralFallback(ResolvedCombatFx.Ticket t)=>BeginCore(t,null,true);
  static WorldBattleFx.AttackPlayback BeginCore(ResolvedCombatFx.Ticket t,float? contactAt,bool neutralFallback){
   if(t==null)return null;
   if((!neutralFallback&&(!Eligible(t)||!t.ExactSource))||t.Event!="DamageDealt"||t.Outcome!="hit"||t.Amount<=0||!t.HasWorld){t.RenderFallbackReason="bounded_contact_ineligible_receipt";return null;}
   if(!G.Finite(Time.time)||contactAt.HasValue&&(!G.Finite(contactAt.Value)||contactAt.Value>Time.time)){t.RenderFallbackReason="bounded_contact_requires_actual_contact";return null;}
   float height=t.ContactVisibleHeight;var v=t.Visual;float size=v?.Size??1f,emission=v?.Emission??1f,particles=v?.Particles??1f;float scale=height*size;
   if(!Finite(t.Position)||!G.Finite(height)||height<=0||!G.Finite(scale)||scale<=0||!G.Finite(emission)||emission<0||!G.Finite(particles)){t.RenderFallbackReason="bounded_contact_invalid_captured_geometry";return null;}
   if(!Ready()){t.RenderFallbackReason=LastFailure;return null;}
   var basis=Stage.Camera.transform.rotation;if(!CaptureValid(t.Position,scale,basis)){t.RenderFallbackReason="bounded_contact_invalid_camera_basis";return null;}
   Slot s=null;
   try{
    s=Acquire();s.Busy=true;s.Landed=false;s.Expected=Time.time;s.Emission=emission;s.Profile=G.Find(neutralFallback?"minimal_neutral_contact":t.AttackFamily);s.Particles=G.MoteCount(particles);s.Ticket=t;
    s.Root.position=t.Position;s.Root.rotation=basis;s.Root.localScale=Vector3.one*scale;s.Root.gameObject.SetActive(true);s.Renderer.sharedMaterial=_material;
    int version=s.Version,epoch=_epoch;var captured=s;
    var handle=new WorldBattleFx.AttackPlayback{
     Contact=()=>{
      if(!Current(captured,version,epoch)){StaleContacts++;t.RenderFallbackReason="bounded_contact_stale_pool_lease";return false;}
      if(captured.Landed)return true;
      if(!G.Finite(Time.time)||!Tween.SchedulingAllowed||!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){t.RenderFallbackReason="bounded_contact_environment_unavailable";Retire(captured,version);return false;}
      try{captured.Started=Time.time;captured.Landed=true;Draw(captured,0);t.AuthoredRenderer=neutralFallback?"bounded-neutral-fallback-r1":"bounded-contact-r1";t.AuthoredTier=v?.Tier;t.AuthoredTierReason=neutralFallback?"neutral_receipt_fallback":"composed_receipt_contact";return true;}
      catch(Exception e){t.RenderFallbackReason="bounded_contact_upload_failed:"+e.GetType().Name;Retire(captured,version);return false;}
     },
     Read=()=>Current(captured,version,epoch)?new{family=captured.Profile.Id,shape=captured.Profile.Form.ToString(),surface=captured.Profile.Material.ToString(),renderer="bounded-contact-r1",landed=captured.Landed,age=captured.Landed?Time.time-captured.Started:0f,visibleEnd=G.End,particleCount=captured.Particles,worldTransport=false,anticipation=0f,sceneLights=0,version,serial=captured.Serial,damageTier=t.Intensity?.Tier,renderTier=v?.Tier}:null
    };
    Tween.RunPresentation(Life(s,version,epoch));if(!Current(s,version,epoch)){t.RenderFallbackReason="bounded_contact_scope_cancelled";return null;}Submitted++;return handle;
   }catch(Exception e){if(s!=null)Retire(s,s.Version);t.RenderFallbackReason="bounded_contact_construction_failed:"+e.GetType().Name;return null;}
  }
 }
}
