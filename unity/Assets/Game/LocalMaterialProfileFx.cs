using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using G = DeckRogue.Game.LocalMaterialProfileGeometry;

namespace DeckRogue.Game {
 // Receipt-owned P0 target-local material contact. Exact source-card provenance belongs to the host.
 internal static class LocalMaterialProfileFx {
  sealed class Draw { public Mesh Mesh; public MeshRenderer Renderer; public Vector3[] Vertices; public Vector2[] UV,Phase; public Color[] Colors; }
  sealed class Set { public Transform Root; public Draw[] Draws; public G.Buffer Geometry; public G.MeshData[] DrawData; public LightBurstGeometry Burst; public WorldBattleFx.AttackSpec LightSpec; }
  sealed class Slot {
   public Transform Root; public readonly Set[] Sets=new Set[6]; public Set Active;
   public bool Busy,Landed; public int Version,ParticleCount; public long Serial;
   public float Started,Expected,Height,Size,Emission,Particles,LightPower; public string Tier;
   public StageFx.ContractLightPlayback Light; public LightBurstBinding BurstBinding; public string Target;
  }
  static readonly Slot[] Pool=new Slot[8];
  static int _epoch; static long _serial; static readonly Material[] Materials=new Material[5];
  static readonly Texture2D[] Textures=new Texture2D[5]; static Texture2D _lookup; static Material BurstMaterial; static Texture2D BurstTexture;
  public static bool ReviewDisableBurst;
  static Color[][] _roles; static Color[] _lightColors;
  static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  public static string LastFailure { get; private set; }
  public static int Submitted { get; private set; }
  public static int Recycled { get; private set; }
  public static int StaleContacts { get; private set; }
  public static int ActiveCount { get { int n=0; foreach(var s in Pool)if(s!=null&&s.Busy)n++; return n; } }
  public static bool IsFamily(string family)=>family=="local_dark"||family=="local_light"||family=="local_botanical"||family=="local_physical"||family=="local_water_flow";
  public static bool Eligible(ResolvedCombatFx.Ticket t)=>t!=null&&IsFamily(t.AttackFamily);
  public static float Lead(ResolvedCombatFx.Ticket t)=>0f;
  static G.Profile Profile(string family) {
   switch(family){case "local_dark":return G.Profile.Dark;case "local_light":return G.Profile.Light;case "local_botanical":return G.Profile.Botanical;case "local_physical":return G.Profile.Physical;case "local_water_flow":return G.Profile.Water;default:throw new ArgumentException("unknown local material family");}
  }
  public static float Duration(string family)=>IsFamily(family)?(float)G.End(Profile(family)):0f;
  public static float Duration(ResolvedCombatFx.Ticket t)=>Duration(t?.AttackFamily);
  public static bool Available(string family)=>IsFamily(family)&&Ready(family);
  public static bool Land(WorldBattleFx.AttackPlayback handle)=>handle!=null&&handle.Land();
  static Color Role(JToken token) {
   if(token==null||!ColorUtility.TryParseHtmlString((string)token,out var c))throw new FormatException("invalid local material role");
   return c.linear; // Surface and independent scene palette sRGB decode once.
  }
  static Vector4 Vector(Color c)=>new Vector4(c.r,c.g,c.b,1);
  static bool Finite(float x)=>G.Finite(x);
  static bool Finite(Vector3 p)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z);
  static double Dot(Vector3 a,Vector3 b)=>(double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z;
  static bool CaptureValid(Vector3 point,float scale,Quaternion rotation) {
   var r=rotation*Vector3.right;var u=rotation*Vector3.up;var f=rotation*Vector3.forward;
   if(!Finite(r)||!Finite(u)||!Finite(f)||Math.Abs(Dot(r,r)-1)>1e-4||Math.Abs(Dot(u,u)-1)>1e-4||Math.Abs(Dot(f,f)-1)>1e-4||Math.Abs(Dot(r,u))>1e-4||Math.Abs(Dot(r,f))>1e-4||Math.Abs(Dot(u,f))>1e-4)return false;
   // The conservative carrier bounds in captured world space must remain finite, including hidden birth carriers.
   var b=G.ConservativeVisibleBounds;
   for(int i=0;i<8;i++) {
    var v=new Vector3((float)(i%2==0?b.Min.X:b.Max.X),(float)((i&2)==0?b.Min.Y:b.Max.Y),-(float)((i&4)==0?b.Min.Z:b.Max.Z));
    if(!Finite(point+rotation*(v*scale)))return false;
   }
   return true;
  }
  public static bool Ready()=>Ready("local_dark")&&Ready("local_light")&&Ready("local_botanical")&&Ready("local_physical")&&Ready("local_water_flow");
  static string TexturePath(G.Profile p)=>p==G.Profile.Dark?"Art/fx/dark-drain-surface-r1":p==G.Profile.Light?"Art/fx/light-ray-surface-r2":p==G.Profile.Physical?"Art/fx/enemy-blunt-contact-r1":p==G.Profile.Water?"Art/fx/water-flow-surface-r1":null;
  public static bool Ready(string family) {
   LastFailure=null;
   if(!IsFamily(family)){LastFailure="local_material_unknown_family";return false;}
   if(!Tween.SchedulingAllowed){LastFailure="local_material_scope_cancelled";return false;}
   if(!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){LastFailure="local_material_world_camera_unavailable";return false;}
   if(ElementBattleFx.ForceMissingForTest){LastFailure="local_material_forced_resource_missing";return false;}
   if(QualitySettings.activeColorSpace!=ColorSpace.Linear){LastFailure="local_material_requires_linear_project";return false;}
   var profile=Profile(family);int index=(int)profile;bool botanical=profile==G.Profile.Botanical,water=profile==G.Profile.Water;
   if(Materials[index]!=null&&(botanical||Textures[index]!=null)&&(!water||_lookup!=null))return true;
   Material material=null;
   try {
    var shader=Shader.Find("DeckRogue/LocalMaterialProfileFx");
    // Exact preserved existing imports. Botanical loads no PNG. Never mutate shared sampler/importer state.
    var texture=botanical?null:Resources.Load<Texture2D>(TexturePath(profile));
    var lookup=water?Resources.Load<Texture2D>("Art/fx/water-flow-droplet-lookup-r1"):null;
    var palette=Resources.Load<TextAsset>("Data/local-material-palette-r1");
    if(shader==null||!shader.isSupported||(!botanical&&(texture==null||texture.width!=(water?2172:1774)||texture.height!=(water?724:887)))||water&&(lookup==null||lookup.width!=256||lookup.height!=128)||palette==null){LastFailure="local_material_shader_or_resource_unavailable";return false;}
    var j=JObject.Parse(palette.text);
    if((string)j["schema"]!="deckrogue.local-material-palette/1")throw new FormatException("invalid local material palette schema");
    // Each profile parses only its own palette. An unrelated missing profile does not disable this one.
    var p=j["profiles"]?[family];var roles=p?["roles_srgb"];
    var colors=new[]{Role(roles?["shadow"]),Role(roles?["primary"]),Role(roles?["edge"]),Role(roles?["core"])};
    var light=Role(p?["scene_light_srgb"]);
    material=new Material(shader);if(!botanical)material.SetTexture("_BaseMap",texture);
    if(water)material.SetTexture("_LookupMap",lookup);
    if(_roles==null)_roles=new Color[5][];if(_lightColors==null)_lightColors=new Color[5];
    _roles[index]=colors;_lightColors[index]=light;Materials[index]=material;Textures[index]=texture;if(water)_lookup=lookup;return true;
   } catch(Exception e) {
    if(material!=null)UnityEngine.Object.Destroy(material);
    LastFailure="local_material_resource_or_material_failed:"+e.GetType().Name;return false;
   }
  }
  static bool ReadyBurst(){
   if(BurstMaterial!=null&&BurstTexture!=null)return true;
   var shader=Resources.Load<Shader>("Shaders/LightBurstOpticalSurface");var texture=Resources.Load<Texture2D>("Art/fx/light-burst-pressure-density-v1");
   if(shader==null||!shader.isSupported||texture==null||texture.width!=1536||texture.height!=1024)return false;
   BurstTexture=texture;BurstMaterial=new Material(shader);BurstMaterial.SetTexture("_BaseMap",texture);return true;
  }
  public static JArray CaptureReviewState(){
   var rows=new JArray();foreach(var s in Pool)if(s?.Active!=null){
    var draws=new JArray();for(int j=0;j<s.Active.Draws.Length;j++){
     var d=s.Active.Draws[j];var data=s.Active.DrawData[j];var verts=new JArray();
     if(s.Busy&&(s.Active.Burst!=null||j==2))for(int i=0;i<d.Vertices.Length;i++){var world=s.Active.Root.TransformPoint(d.Vertices[i]);var screen=Stage.Camera.WorldToScreenPoint(world);verts.Add(new JArray(i,d.Vertices[i].x,d.Vertices[i].y,d.Vertices[i].z,d.UV[i].x,d.UV[i].y,d.Colors[i].r,d.Colors[i].a,d.Phase[i].y,screen.x,screen.y,screen.z));}
     draws.Add(new JObject{["kind"]=data.Kind,["visible"]=d.Renderer.enabled&&d.Renderer.gameObject.activeInHierarchy,["vertices"]=d.Mesh.vertexCount,["indexCount"]=d.Mesh.subMeshCount>0?d.Mesh.GetIndexCount(0):0,["shader"]=d.Renderer.sharedMaterial.shader.name,["samples"]=verts});
    }
    rows.Add(new JObject{["serial"]=s.Serial,["target"]=s.Target,["receiverProjection"]=s.Busy&&s.Active.Burst!=null?Hd2dAttackFx.CaptureReceiverProjection(s.Target,s.Active.Root.position,Stage.Camera):null,["rootScale"]=s.Active.Root.localScale.x,["busy"]=s.Busy,["landed"]=s.Landed,["ownedBurst"]=s.Active.Burst!=null,["committed"]=s.BurstBinding?.Committed==true,["age"]=s.Landed?Time.time-s.Started:0,["axis"]=s.Active.Burst!=null?new JArray(s.Active.Burst.Axis.x,s.Active.Burst.Axis.y):null,["height"]=s.Height,["size"]=s.Size,["emission"]=s.Emission,["particles"]=s.ParticleCount,["draws"]=draws});
   }return rows;
  }
  // Bounded initial Unity mapping. These are UNCALIBRATED ratios, not Blender-W equivalence.
  // No scene contribution exists until actual Land; negative-age envelope is zero defensively.
  static WorldBattleFx.AttackSpec Spec(G.Profile profile)=>new WorldBattleFx.AttackSpec(
   new[]{0f,0f,0f,0f,0f,0f,0f,(float)G.LightEnd,(float)G.End(profile)},1,1,1,_lightColors[(int)profile],_lightColors[(int)profile],.025f,0f,0f) {
    ContractLighting=true,StandardContractPower=true,
    TargetPeakMultiplier=.35f*(30f/55f),FloorPeakMultiplier=.12f*(10f/20f),
    MaxCombineGroup="local-material-continuous-r1",
    Envelope=age=>age<0?0f:(float)G.LightEnvelope(profile,age)
   };
  static void DestroySet(Set s) {
   if(s==null)return;
   if(s.Draws!=null)foreach(var d in s.Draws)if(d?.Mesh!=null)UnityEngine.Object.Destroy(d.Mesh);
   if(s.Root!=null){s.Root.gameObject.SetActive(false);UnityEngine.Object.Destroy(s.Root.gameObject);}
  }
  static Draw CreateDraw(Transform root,int index,G.MeshData data,G.Profile profile) {
   var go=new GameObject("local-material-part-"+index);Mesh mesh=null;
   try {
    go.transform.SetParent(root,false);mesh=new Mesh{name="local-material-continuous-r1-"+index};mesh.MarkDynamic();
    var d=new Draw{Mesh=mesh,Vertices=new Vector3[data.Vertices.Length],UV=new Vector2[data.Vertices.Length],Phase=new Vector2[data.Vertices.Length],Colors=new Color[data.Vertices.Length]};
    mesh.vertices=d.Vertices;mesh.uv=d.UV;mesh.uv2=d.Phase;mesh.colors=d.Colors;mesh.triangles=data.Triangles;
    go.AddComponent<MeshFilter>().sharedMesh=mesh;d.Renderer=go.AddComponent<MeshRenderer>();
    d.Renderer.sharedMaterial=Materials[(int)profile];d.Renderer.sortingOrder=index;d.Renderer.shadowCastingMode=ShadowCastingMode.Off;
    d.Renderer.receiveShadows=false;d.Renderer.enabled=false;return d;
   } catch {
    // Own the mesh even if construction fails before the Draw can be attached to its Set.
    if(mesh!=null)UnityEngine.Object.Destroy(mesh);go.SetActive(false);UnityEngine.Object.Destroy(go);throw;
   }
  }
  static Set CreateSet(Transform parent,G.Profile profile,bool burst) {
   var root=new GameObject(G.ProfileId(profile)).transform;var set=new Set{Root=root};
   try {
    root.SetParent(parent,false);set.Geometry=G.Create(profile,1);set.DrawData=set.Geometry.Batches;if(burst){set.Burst=new LightBurstGeometry();set.DrawData=new[]{set.Burst.Main,set.Burst.Core,set.Geometry.Particles};}
    set.Draws=new Draw[set.Geometry.Batches.Length];set.LightSpec=Spec(profile);
    for(int i=0;i<set.Draws.Length;i++){set.Draws[i]=CreateDraw(root,i,set.DrawData[i],profile);if(burst&&i<2)set.Draws[i].Renderer.sharedMaterial=BurstMaterial;}
    root.gameObject.SetActive(false);return set;
   } catch { DestroySet(set);throw; }
  }
  static void Retire(Slot s,int version) {
   if(s.Version!=version)return;s.Busy=false;s.Light?.Cancel();s.Light=null;s.BurstBinding=null;
   if(s.Active?.Burst!=null){s.Active.Burst.Clear();foreach(var d in s.Active.Draws){if(d.Renderer!=null)d.Renderer.enabled=false;if(d.Mesh!=null)d.Mesh.Clear(false);}}
   if(s.Active?.Root!=null)s.Active.Root.gameObject.SetActive(false);
   if(s.Root!=null)s.Root.gameObject.SetActive(false);
  }
  public static void Cancel() { _epoch++;foreach(var s in Pool)if(s!=null){Retire(s,s.Version);s.Version++;} }
  static Slot Acquire(G.Profile profile,bool burst) {
   int limit=GfxQuality.Level==GfxLevel.Low?4:8;Slot s=null;
   // A downgrade immediately enforces the Low live-slot bound without changing particle topology.
   for(int i=limit;i<Pool.Length;i++)if(Pool[i]!=null&&Pool[i].Busy){Retire(Pool[i],Pool[i].Version);Pool[i].Version++;}
   for(int i=0;i<limit;i++){if(Pool[i]==null)Pool[i]=new Slot();if(!Pool[i].Busy||Pool[i].Root==null){s=Pool[i];break;}}
   if(s==null){s=Pool[0];for(int i=1;i<limit;i++)if(Pool[i].Serial<s.Serial)s=Pool[i];Recycled++;}
   Retire(s,s.Version);s.Version++;s.Serial=++_serial;
   if(s.Root==null){for(int i=0;i<s.Sets.Length;i++){DestroySet(s.Sets[i]);s.Sets[i]=null;}s.Active=null;s.Root=new GameObject("local-material-continuous-r1").transform;}
   int p=burst?5:(int)profile;if(s.Sets[p]==null)s.Sets[p]=CreateSet(s.Root,profile,burst);
   for(int i=0;i<s.Sets.Length;i++)if(s.Sets[i]?.Root!=null)s.Sets[i].Root.gameObject.SetActive(i==p);
   s.Active=s.Sets[p];return s;
  }
  static bool Current(Slot s,int version,int epoch)=>epoch==_epoch&&s.Version==version&&s.Busy&&s.Root!=null;
  static void DrawAt(Slot s,float age,bool validateUpload=false) {
   var geometry=s.Active.Geometry;G.SampleReceipt(geometry,age,0,s.ParticleCount);
   if(s.Active.Burst!=null){bool committed=s.BurstBinding?.Committed==true;s.Active.Burst.Sample(age,committed);if(!committed)Array.Clear(geometry.Particles.Alpha,0,geometry.Particles.Alpha.Length);}
   var roles=_roles[(int)geometry.Family];
   for(int j=0;j<s.Active.Draws.Length;j++) {
    var d=s.Active.Draws[j];var data=s.Active.DrawData[j];bool visible=false;
    var lo=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue);var hi=new Vector3(float.MinValue,float.MinValue,float.MinValue);
    for(int i=0;i<data.Vertices.Length;i++) {
     var p=data.Vertices[i];var v=new Vector3((float)p.X,(float)p.Y,-(float)p.Z);
     float alpha=(float)data.Alpha[i],gain=(float)data.Gain[i]*s.Emission,phase=(float)data.Phase[i];
     if(!Finite(v)||!Finite((float)data.UV[i].X)||!Finite((float)data.UV[i].Y)||!Finite(alpha)||!Finite(gain)||!Finite(phase)||data.Role[i]<0||data.Role[i]>3)throw new InvalidOperationException("nonfinite_gpu_input");
     d.Vertices[i]=v;d.UV[i]=new Vector2((float)data.UV[i].X,(float)data.UV[i].Y);d.Phase[i]=new Vector2(phase,data.Role[i]);
     d.Colors[i]=new Color(gain,gain,gain,alpha);visible|=alpha>0f;
     lo=new Vector3(Mathf.Min(lo.x,v.x),Mathf.Min(lo.y,v.y),Mathf.Min(lo.z,v.z));
     hi=new Vector3(Mathf.Max(hi.x,v.x),Mathf.Max(hi.y,v.y),Mathf.Max(hi.z,v.z));
    }
    d.Renderer.enabled=visible;if(!visible&&!validateUpload)continue;
    bool restoreTriangles=d.Mesh.vertexCount!=d.Vertices.Length;d.Mesh.SetVertices(d.Vertices);d.Mesh.SetUVs(0,d.UV);d.Mesh.SetUVs(1,d.Phase);d.Mesh.SetColors(d.Colors);if(restoreTriangles)d.Mesh.triangles=data.Triangles;
    d.Mesh.bounds=new Bounds((lo+hi)*.5f,hi-lo+Vector3.one*.001f);
    Props.Clear();Props.SetFloat("_Kind",data.Kind);Props.SetFloat("_Family",(int)geometry.Family);
    Props.SetVector("_RoleShadow",Vector(roles[0]));Props.SetVector("_RolePrimary",Vector(roles[1]));Props.SetVector("_RoleEdge",Vector(roles[2]));Props.SetVector("_RoleCore",Vector(roles[3]));
    d.Renderer.SetPropertyBlock(Props);
   }
  }
  static void Hide(Slot s) { foreach(var d in s.Active.Draws)d.Renderer.enabled=false; }
  static IEnumerator Life(Slot s,int version,int epoch) {
   try {
    while(Current(s,version,epoch)) {
     if(s.Landed){float age=Time.time-s.Started;if(age>=(float)G.End(s.Active.Geometry))yield break;DrawAt(s,age);}
     else {if(Time.time-s.Expected>=.35f)yield break;Hide(s);}
     yield return null;
    }
   } finally { if(epoch==_epoch)Retire(s,version); }
  }
  public static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket ticket,float? contactAt=null) {
   if(ticket==null)return null;
   if(!Eligible(ticket)||ticket.Event!="DamageDealt"||!ticket.HasWorld||ticket.Outcome!="hit"||ticket.Amount<=0){ticket.RenderFallbackReason="local_material_ineligible_receipt";return null;}
   if(!Finite(Time.time)||contactAt.HasValue&&!Finite(contactAt.Value)){ticket.RenderFallbackReason="local_material_invalid_contact_clock";return null;}
   // P0: no preview, added wait or inferred contact. Host calls Begin then actual Land in its existing receipt path.
   if(contactAt.HasValue&&contactAt.Value>Time.time){ticket.RenderFallbackReason="local_material_contact_only";return null;}
   float height=ticket.ContactVisibleHeight;var point=ticket.Position;
   if(!Finite(height)||height<=0f||!Finite(point)||!Finite(ticket.Feet)){ticket.RenderFallbackReason="local_material_invalid_captured_geometry";return null;}
   var visual=ticket.Visual;float size=visual?.Size??1f,emission=visual?.Emission??1f,particles=visual?.Particles??1f,light=visual?.Light??1f;
   float scale=height*size;
   if(!Finite(scale)||scale<=0f||!Finite(emission)||emission<0||!Finite(particles)||!Finite(light)||light<0){ticket.RenderFallbackReason="local_material_invalid_composed_visual";return null;}
   if(!Ready(ticket.AttackFamily)){ticket.RenderFallbackReason=LastFailure;return null;}
   var profile=Profile(ticket.AttackFamily);var cameraRotation=Stage.Camera.transform.rotation;
   bool ownedBurst=!ReviewDisableBurst&&ticket.LightBurstBinding?.Eligible(ticket)==true&&ticket.HasBeamOrigin&&Finite(ticket.BeamOrigin);
   if(ownedBurst&&!ReadyBurst()){ticket.RenderFallbackReason="light_burst_private_resource_unavailable";return null;}
   if(!CaptureValid(point,scale,cameraRotation)){ticket.RenderFallbackReason="local_material_invalid_camera_or_world_bounds";return null;}
   var position=point;var feet=ticket.Feet;var target=ticket.Target;var impact=ticket.Impact;
   Slot s=null;
   try {
    s=Acquire(profile,ownedBurst);s.Busy=true;s.Landed=false;s.Height=height;s.Size=size;s.Expected=Time.time;s.Target=ticket.Target;s.BurstBinding=ownedBurst?ticket.LightBurstBinding:null;
    if(ownedBurst){s.Active.Burst.Clear();s.Active.Burst.Configure(Quaternion.Inverse(cameraRotation)*(ticket.BeamOrigin-ticket.Position)/scale);}
    s.Emission=emission;s.Particles=Mathf.Clamp01(particles);s.LightPower=light;s.Tier=visual?.Tier;
    s.ParticleCount=Mathf.FloorToInt(G.Count(profile)*s.Particles);
    int version=s.Version,epoch=_epoch;
    s.Root.position=position;s.Root.rotation=cameraRotation;s.Root.localScale=Vector3.one*scale;s.Root.gameObject.SetActive(true);
    ticket.OutcomeIncomingReason="captured_camera_local_material_contact_only";
    var captured=s;
    var playback=new WorldBattleFx.AttackPlayback {
     Contact=()=> {
      if(!Current(captured,version,epoch)){StaleContacts++;return false;}
      if(!Finite(Time.time)||!Tween.SchedulingAllowed||!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){ticket.RenderFallbackReason="local_material_contact_environment_unavailable";Retire(captured,version);return false;}
      if(captured.Landed)return true;
      try {
       captured.Landed=true;captured.Started=Time.time;
       // Every batch uploads at actual contact, including zero-opacity body/particles.
       DrawAt(captured,0f,true);
       if(Stage.ImpactTargetCurrent(target,impact)) {
        captured.Light=StageFx.BeginContractAttackLight(target,impact?.Target,epoch,position,feet,height,captured.Active.LightSpec,0f,false,
         ()=>Current(captured,version,epoch)&&Stage.ImpactTargetCurrent(target,impact),1f,captured.LightPower,()=>Time.time-captured.Started);
        captured.Light?.Land();
       }
       ticket.AuthoredRenderer="continuous-local-material-r1";return true;
      } catch(Exception e) { Retire(captured,version);ticket.RenderFallbackReason="local_material_contact_failed:"+e.GetType().Name;return false; }
     },
     Read=()=>Current(captured,version,epoch)?new {
      attack=G.ProfileId(profile),renderer=ownedBurst?"owned-light-burst326":"continuous-local-material-r1",ownedBurst,committed=captured.BurstBinding?.Committed==true,worldCasterBeam=false,
      age=captured.Landed?Time.time-captured.Started:0f,nativeAge=captured.Active.Geometry.VisualAge,
      landed=captured.Landed,height=captured.Height,size=captured.Size,damageTier=captured.Tier,version,serial=captured.Serial,
      anticipationUsed=0f,visibleEnd=G.End(profile),particleCount=captured.ParticleCount,capturedCameraBasis=true,mirrorRight=1
     }:null
    };
    Tween.RunPresentation(Life(s,version,epoch));
    if(!Current(s,version,epoch)){ticket.RenderFallbackReason="local_material_scope_cancelled";return null;}
    Submitted++;return playback;
   } catch(Exception e) { if(s!=null)Retire(s,s.Version);ticket.RenderFallbackReason="local_material_construction_failed:"+e.GetType().Name;return null; }
  }
 }
}
