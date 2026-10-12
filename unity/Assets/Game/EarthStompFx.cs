using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using StrictGround;
namespace DeckRogue.Game {
 // Receipt-owned floor effect. A certified actual-surface capture is mandatory; no flat proxy.
 internal static class EarthStompFx {
  sealed class Draw {public Mesh Mesh;public MeshRenderer Renderer;public Vector3[] Vertices,Normals;public Vector2[] UV,Effect;public Color[] Colors;}
  sealed class Set {public Transform Root;public Draw[] Draws;public EarthStompGeometry.Buffer Geometry;public WorldBattleFx.AttackSpec LightSpec;}
  sealed class Slot {public Transform Root;public Set Active;public GroundSurfaceProvider.Lease Lease;public ResolvedCombatFx.Ticket Ticket;public bool Busy,Landed;public int Version,Family;public long Serial;public float Started,Expected,Available,Height,Size,Emission,Particles,Alpha,LightPower;public Vector3 Right,Up,Toward;public StageFx.ContractLightPlayback Light;}
  static readonly Slot[] Pool=new Slot[8];static int _epoch;static long _serial;static Material _surface,_rock;static Texture2D _texture;static Color[][] _roles;static Color[] _lightColors;static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  public static string LastFailure{get;private set;}public static int Submitted{get;private set;}public static int Recycled{get;private set;}public static int StaleContacts{get;private set;}
  public static int ActiveCount{get{int n=0;foreach(var s in Pool)if(s!=null&&s.Busy)n++;return n;}}
  public static bool IsFamily(string family)=>family=="earth_fissure"||family=="stomp";
  public static int Family(string family)=>family=="stomp"?EarthStompGeometry.Stomp:EarthStompGeometry.Earth;
  public static bool Eligible(ResolvedCombatFx.Ticket t)=>t!=null&&((t.AttackFamily=="earth_fissure"&&t.Element=="earth")||(t.AttackFamily=="stomp"&&(t.Element=="physical"||t.Element=="earth")));
  public static float Lead(ResolvedCombatFx.Ticket t)=>Eligible(t)?(float)EarthStompGeometry.Lead(Family(t.AttackFamily)):0;
  static Color Role(JToken t){if(t==null||!ColorUtility.TryParseHtmlString((string)t,out var c))throw new FormatException("invalid ground role");return c.linear;}
  static Vector4 Vector(Color c)=>new Vector4(c.r,c.g,c.b,1);
  public static bool Ready(){
   LastFailure=null;if(!Tween.SchedulingAllowed){LastFailure="ground_scope_cancelled";return false;}
   if(!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){LastFailure="ground_world_camera_unavailable";return false;}
   if(ElementBattleFx.ForceMissingForTest){LastFailure="ground_forced_resource_missing";return false;}
   if(QualitySettings.activeColorSpace!=ColorSpace.Linear){LastFailure="ground_requires_linear_project";return false;}
   if(_surface!=null&&_rock!=null&&_texture!=null)return true;
   var shader=Shader.Find("DeckRogue/EarthStompSurfaceR2");var rockShader=Shader.Find("DeckRogue/EarthStompRockR2");var texture=Resources.Load<Texture2D>("Art/fx/earth-ground-grain-r2");var palette=Resources.Load<TextAsset>("Data/earth-stomp-palette-r2");
   if(shader==null||!shader.isSupported||rockShader==null||!rockShader.isSupported||texture==null||texture.width!=1774||texture.height!=887||palette==null){LastFailure="ground_shader_or_resource_unavailable";return false;}
   Material surface=null,rock=null;
   try{var j=JObject.Parse(palette.text);var colors=new Color[2][];var lights=new Color[2];for(int i=0;i<2;i++){var p=j[i==0?"earth":"stomp"];var r=p["roles_srgb"];colors[i]=new[]{Role(r["shadow"]),Role(r["primary"]),Role(r["edge"]),Role(r["core"])};lights[i]=Role(p["scene_light_srgb"]);}surface=new Material(shader);surface.SetTexture("_GrainTex",texture);rock=new Material(rockShader);_surface=surface;_rock=rock;_texture=texture;_roles=colors;_lightColors=lights;return true;}
   catch(Exception e){if(surface!=null)UnityEngine.Object.Destroy(surface);if(rock!=null)UnityEngine.Object.Destroy(rock);LastFailure="ground_palette_invalid:"+e.GetType().Name;return false;}
  }
  static WorldBattleFx.AttackSpec Spec(int family){bool earth=family==0;return new WorldBattleFx.AttackSpec(new[]{0f,0f,0f,0f,0f,0f,0f,.080f,(float)EarthStompGeometry.End(family)},1,1,1,_lightColors[family],_lightColors[family],earth?.026f:.018f,0,0){ContractLighting=true,StandardContractPower=true,TargetPeakMultiplier=.35f*((earth?35f:30f)/55f),FloorPeakMultiplier=.12f*((earth?23f:20f)/20f),MaxCombineGroup="earth-stomp-ground-r2",Envelope=age=>age<0?0f:earth?(age<=.210f?1f-(float)EarthStompGeometry.Smooth((age-.026)/.184):0f):(age<=.145f?1f-(float)EarthStompGeometry.Smooth((age-.018)/.127):0f)};}
  static Vector3 V(D3 p)=>new Vector3((float)p.X,(float)p.Y,(float)p.Z);
  static D3 D(EarthStompGeometry.Point p)=>new D3(p.X,p.Y,p.Z);
  static EarthStompGeometry.Point P(D3 p)=>new EarthStompGeometry.Point(p.X,p.Y,p.Z);
  static bool Finite(Vector3 p)=>EarthStompGeometry.Finite(p.x)&&EarthStompGeometry.Finite(p.y)&&EarthStompGeometry.Finite(p.z);
  static void DestroySet(Set s){if(s==null)return;foreach(var d in s.Draws)if(d?.Mesh!=null)UnityEngine.Object.Destroy(d.Mesh);if(s.Root!=null){s.Root.gameObject.SetActive(false);UnityEngine.Object.Destroy(s.Root.gameObject);}}
  static Draw CreateDraw(Transform root,int index,int count,int[] triangles,bool rock){
   var go=new GameObject("ground-part-"+index);Mesh mesh=null;
   try{go.transform.SetParent(root,false);mesh=new Mesh{name="earth-stomp-ground-r2-"+index};mesh.MarkDynamic();
    var d=new Draw{Mesh=mesh,Vertices=new Vector3[count],Normals=new Vector3[count],UV=new Vector2[count],Effect=new Vector2[count],Colors=new Color[count]};
    mesh.vertices=d.Vertices;mesh.normals=d.Normals;mesh.uv=d.UV;mesh.SetUVs(1,d.Effect);mesh.colors=d.Colors;mesh.triangles=triangles;
    go.AddComponent<MeshFilter>().sharedMesh=mesh;d.Renderer=go.AddComponent<MeshRenderer>();d.Renderer.sharedMaterial=rock?_rock:_surface;d.Renderer.sortingOrder=index;d.Renderer.shadowCastingMode=ShadowCastingMode.Off;d.Renderer.receiveShadows=rock;d.Renderer.enabled=false;return d;
   }catch{if(mesh!=null)UnityEngine.Object.Destroy(mesh);go.SetActive(false);UnityEngine.Object.Destroy(go);throw;}
  }
  static Set CreateSet(Transform root,int family,Capture ground){var rt=new GameObject("ground-capture").transform;rt.SetParent(root,false);var geometry=EarthStompGeometry.Create(family);var set=new Set{Root=rt,Geometry=geometry,Draws=new Draw[geometry.Batches.Length+1],LightSpec=Spec(family)};try{var ix=new int[ground.Indices.Count];for(int i=0;i<ix.Length;i++)ix[i]=ground.Indices[i];set.Draws[0]=CreateDraw(rt,0,ground.Vertices.Count,ix,false);for(int i=0;i<geometry.Batches.Length;i++){var data=geometry.Batches[i];set.Draws[i+1]=CreateDraw(rt,i+1,data.Vertices.Length,data.Triangles,data.Kind==EarthStompGeometry.RockKind);}return set;}catch{DestroySet(set);throw;}}
  static void Retire(Slot s,int v){if(s.Version!=v)return;s.Busy=false;s.Light?.Cancel();s.Light=null;s.Lease=null;s.Ticket=null;if(s.Root!=null)s.Root.gameObject.SetActive(false);}
  public static void Cancel(){_epoch++;foreach(var s in Pool)if(s!=null){Retire(s,s.Version);s.Version++;}}
  static Slot Acquire(int family,GroundSurfaceProvider.Lease lease){int limit=GfxQuality.Level==GfxLevel.Low?4:8;Slot s=null;for(int i=0;i<limit;i++){if(Pool[i]==null)Pool[i]=new Slot();if(!Pool[i].Busy||Pool[i].Root==null){s=Pool[i];break;}}if(s==null){s=Pool[0];for(int i=1;i<limit;i++)if(Pool[i].Serial<s.Serial)s=Pool[i];Recycled++;}Retire(s,s.Version);s.Version++;s.Serial=++_serial;if(s.Root==null)s.Root=new GameObject("earth-stomp-ground-r2").transform;
   // Capture topology can differ each receipt; bounded arrays/mesh are replaced, never stretched off terrain.
   try{DestroySet(s.Active);s.Active=null;s.Family=family;s.Lease=lease;s.Active=CreateSet(s.Root,family,lease.Geometry);var offsets=new EarthStompGeometry.Point[lease.Geometry.Births.Count];for(int i=0;i<offsets.Length;i++)offsets[i]=P(lease.Geometry.Births[i].LocalDeltaH);s.Active.Geometry.SetCapture(offsets,P(lease.Geometry.WorldUpLocal),lease.Geometry.EffectiveH);
   var floor=s.Active.Draws[0];var ground=lease.Geometry;float gain=family==0?1.55f:.95f;
   for(int i=0;i<floor.Vertices.Length;i++){floor.Vertices[i]=V(ground.World(ground.Vertices[i]));floor.Normals[i]=V(ground.Normal);floor.UV[i]=new Vector2((float)ground.UV[i].X,(float)ground.UV[i].Y);floor.Colors[i]=new Color(gain,gain,gain,1);}
   Upload(floor,false,true);s.Root.position=Vector3.zero;s.Root.rotation=Quaternion.identity;s.Root.localScale=Vector3.one;return s;}catch{DestroySet(s.Active);s.Active=null;s.Lease=null;s.Root.gameObject.SetActive(false);throw;}}
  static bool Current(Slot s,int v,int e)=>e==_epoch&&s.Version==v&&s.Busy&&s.Root!=null;
  static bool LeaseCurrent(Slot s,bool contact){string reason;bool ok=contact?s.Lease.VerifyContact(out reason):s.Lease.IsCurrent(out reason);if(!ok){s.Ticket.GroundCaptureReason=reason;s.Ticket.RenderFallbackReason=reason;}return ok;}
  static void Upload(Draw d,bool visible,bool force){d.Renderer.enabled=visible;if(!visible&&!force)return;Vector3 lo=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue),hi=new Vector3(float.MinValue,float.MinValue,float.MinValue);for(int i=0;i<d.Vertices.Length;i++){var v=d.Vertices[i];if(!Finite(v))throw new InvalidOperationException("nonfinite_gpu_vertex");lo=new Vector3(Mathf.Min(lo.x,v.x),Mathf.Min(lo.y,v.y),Mathf.Min(lo.z,v.z));hi=new Vector3(Mathf.Max(hi.x,v.x),Mathf.Max(hi.y,v.y),Mathf.Max(hi.z,v.z));}d.Mesh.SetVertices(d.Vertices);d.Mesh.SetNormals(d.Normals);d.Mesh.SetUVs(0,d.UV);d.Mesh.SetUVs(1,d.Effect);d.Mesh.SetColors(d.Colors);d.Mesh.bounds=new Bounds((lo+hi)*.5f,hi-lo+Vector3.one*.001f);}
  static void Configure(Slot s,Draw d,int kind,float emission){Props.Clear();Props.SetFloat("_Mode",s.Family);Props.SetFloat("_Kind",kind);Props.SetFloat("_Age",(float)s.Active.Geometry.VisualAge);Props.SetFloat("_Front",(float)s.Active.Geometry.Front);Props.SetFloat("_Emission",emission);Props.SetFloat("_Coverage",kind==0?(float)s.Active.Geometry.GroundAlpha*s.Alpha:1f);var r=_roles[s.Family];Props.SetVector("_RoleShadow",Vector(r[0]));Props.SetVector("_RolePrimary",Vector(r[1]));Props.SetVector("_RoleEdge",Vector(r[2]));Props.SetVector("_RoleCore",Vector(r[3]));d.Renderer.SetPropertyBlock(Props);}
  static void DrawAt(Slot s,float age,bool force=false){var ground=s.Lease.Geometry;var buffer=s.Active.Geometry;EarthStompGeometry.SampleReceipt(buffer,age,s.Available);var floor=s.Active.Draws[0];float alpha=(float)buffer.GroundAlpha*s.Alpha;
   floor.Renderer.enabled=alpha>0;if(force)Upload(floor,alpha>0,true);Configure(s,floor,0,s.Emission);
   for(int j=0;j<buffer.Batches.Length;j++){var data=buffer.Batches[j];var draw=s.Active.Draws[j+1];bool rock=data.Kind==EarthStompGeometry.RockKind,visible=false;for(int i=0;i<data.Vertices.Length;i++){var p=data.Vertices[i];draw.Vertices[i]=rock?V(ground.World(D(p))):V(ground.World(D(data.GroundAnchors[i])))+(s.Right*(float)p.X+s.Up*(float)p.Y+s.Toward*(float)p.Z)*(float)ground.EffectiveH;draw.Normals[i]=rock?V(ground.Right*data.Normals[i].X+ground.Normal*data.Normals[i].Y+ground.Forward*data.Normals[i].Z):s.Toward;draw.UV[i]=new Vector2((float)data.UV[i].X,(float)data.UV[i].Y);draw.Effect[i]=new Vector2((float)data.Phase[i],0);float a=(float)data.Alpha[i]*s.Alpha;var c=rock?Color.white:_roles[s.Family][2];float g=rock?1f:(float)data.Gain[i]*s.Particles;draw.Colors[i]=new Color(c.r*g,c.g*g,c.b*g,a);visible|=a>0;}Upload(draw,visible,force);Configure(s,draw,rock?2:1,rock?s.Particles:1f);}
  }
  static void Hide(Slot s){foreach(var d in s.Active.Draws)d.Renderer.enabled=false;}
  static IEnumerator Life(Slot s,int v,int e){try{while(Current(s,v,e)){if(!LeaseCurrent(s,false))yield break;if(s.Landed){float age=Time.time-s.Started;if(age>=EarthStompGeometry.End(s.Family))yield break;DrawAt(s,age);}else{float age=Time.time-s.Expected;if(age>=.35f)yield break;if(age<0)DrawAt(s,age);else Hide(s);}yield return null;}}finally{if(e==_epoch)Retire(s,v);}}
  static bool CaptureOnce(ResolvedCombatFx.Ticket t,float size){if(!t.GroundCaptureAttempted){t.GroundCaptureAttempted=true;Stage.TryCaptureSurface(t.Feet,t.ContactVisibleHeight,size,Family(t.AttackFamily)==0?StrictGround.Family.Earth:StrictGround.Family.Stomp,out t.GroundLease,out t.GroundCaptureReason);}if(t.GroundLease==null){t.RenderFallbackReason=t.GroundCaptureReason??"ground_capture_failed";return false;}if(!t.GroundLease.IsCurrent(out var reason)){t.GroundCaptureReason=reason;t.RenderFallbackReason=reason;return false;}return true;}
  public static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket t,float? contactAt=null){
   if(!Eligible(t)||!t.HasWorld||t.Outcome!="hit"||t.Amount<=0)return null;
   if(contactAt.HasValue&&!EarthStompGeometry.Finite(contactAt.Value)){t.RenderFallbackReason="ground_invalid_contact_clock";return null;}
   if(!EarthStompGeometry.Finite(t.ContactVisibleHeight)||t.ContactVisibleHeight<=0||!Finite(t.Position)||!Finite(t.Feet)){t.RenderFallbackReason="ground_invalid_captured_geometry";return null;}
   var visual=t.Visual;float size=visual?.Size??1f;if(!EarthStompGeometry.Finite(size)||size<=0){t.RenderFallbackReason="ground_invalid_composed_scale";return null;}
   if(!Ready()){t.RenderFallbackReason=LastFailure;return null;}if(!CaptureOnce(t,size))return null;
   Slot s=null;try{s=Acquire(Family(t.AttackFamily),t.GroundLease);s.Ticket=t;s.Busy=true;s.Landed=false;s.Height=t.ContactVisibleHeight;s.Size=size;s.Expected=contactAt??Time.time;s.Available=Mathf.Min(Lead(t),Mathf.Max(0,s.Expected-Time.time));s.Alpha=1f; // Current shared intensity has no alpha scalar; preserve authored coverage.
    s.Emission=visual?.Emission??1f;s.Particles=visual?.Particles??1f;s.LightPower=visual?.Light??1f;s.Right=Stage.Camera.transform.right;s.Up=Stage.Camera.transform.up;s.Toward=-Stage.Camera.transform.forward;s.Root.gameObject.SetActive(true);int v=s.Version,e=_epoch;var captured=s;t.OutcomeIncomingReason="captured_actual_ground_surface_r2";
    var playback=new WorldBattleFx.AttackPlayback{Contact=()=>{if(!Current(captured,v,e)){StaleContacts++;return false;}if(!Tween.SchedulingAllowed||!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){t.RenderFallbackReason="ground_contact_environment_unavailable";Retire(captured,v);return false;}if(captured.Landed)return true;if(!LeaseCurrent(captured,true)){Retire(captured,v);return false;}try{captured.Landed=true;captured.Started=Time.time;DrawAt(captured,0,true);if(Stage.ImpactTargetCurrent(t.Target,t.Impact)){var feet=V(captured.Lease.Geometry.Origin);var body=t.Feet+captured.Up*(captured.Height*(captured.Family==0?.30f:.20f));captured.Light=StageFx.BeginContractAttackLight(t.Target,t.Impact?.Target,e,body,feet,captured.Height,captured.Active.LightSpec,0,false,()=>Current(captured,v,e)&&captured.Lease.IsCurrent(out _)&&Stage.ImpactTargetCurrent(t.Target,t.Impact),1f,captured.LightPower,()=>Time.time-captured.Started);captured.Light?.Land();}t.AuthoredRenderer="continuous-ground-r2";return true;}catch(Exception ex){Retire(captured,v);t.RenderFallbackReason="ground_contact_failed:"+ex.GetType().Name;return false;}},Read=()=>Current(captured,v,e)?new{attack=t.AttackFamily,renderer="continuous-ground-r2",age=captured.Landed?Time.time-captured.Started:Time.time-captured.Expected,landed=captured.Landed,height=captured.Height,size=captured.Size,hfx=captured.Lease.Geometry.EffectiveH,captureGeneration=captured.Lease.Geometry.Generation,vertices=captured.Lease.Geometry.Vertices.Count,triangles=captured.Lease.Geometry.Indices.Count/3,version=v,serial=captured.Serial,anticipationUsed=captured.Available,visibleEnd=EarthStompGeometry.End(captured.Family)}:null};
    Tween.RunPresentation(Life(s,v,e));if(!Current(s,v,e)){if(t.RenderFallbackReason==null)t.RenderFallbackReason="ground_scope_cancelled";return null;}Submitted++;return playback;
   }catch(Exception ex){if(s!=null)Retire(s,s.Version);t.RenderFallbackReason="ground_construction_failed:"+ex.GetType().Name;return null;}
  }
 }
}
