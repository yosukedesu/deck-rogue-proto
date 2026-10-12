using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using G = DeckRogue.Game.WideProfileGeometry;

namespace DeckRogue.Game {
 // Receipt-owned P100 target-local broad passage. Exact source-card provenance belongs to the host.
 internal static class WideProfileFx {
  sealed class Draw { public Mesh Mesh; public MeshRenderer Renderer; public Vector3[] Vertices; public Vector2[] UV,Phase; public Color[] Colors; }
  sealed class Set { public Transform Root; public Draw[] Draws; public G.Buffer Geometry; public WorldBattleFx.AttackSpec LightSpec; }
  sealed class Slot {
   public Transform Root; public readonly Set[] Sets=new Set[2]; public Set Active;
   public bool Busy,Landed; public int Version,ParticleCount; public long Serial;
   public float Started,Expected,Available,Height,Size,Emission,Particles,LightPower; public string Tier;
   public StageFx.ContractLightPlayback Light;
  }
  static readonly Slot[] Pool=new Slot[8];
  static int _epoch; static long _serial; static readonly Material[] Materials=new Material[2];
  static readonly Texture2D[] Textures=new Texture2D[2]; static Texture2D _lookup;
  static Color[][] _roles; static Color[] _lightColors;
  static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  public static string LastFailure { get; private set; }
  public static int Submitted { get; private set; }
  public static int Recycled { get; private set; }
  public static int StaleContacts { get; private set; }
  public static int ActiveCount { get { int n=0; foreach(var s in Pool)if(s!=null&&s.Busy)n++; return n; } }
  public static bool IsFamily(string family)=>family=="wide_water"||family=="wide_fire";
  public static bool Eligible(ResolvedCombatFx.Ticket t)=>t!=null&&IsFamily(t.AttackFamily)&&t.Element==(t.AttackFamily=="wide_water"?"water":"fire");
  public static float Lead(ResolvedCombatFx.Ticket t)=>Eligible(t)?(float)G.NativeLead:0f;
  static G.Profile Profile(string family) {
   switch(family){case "wide_water":return G.Profile.Water;case "wide_fire":return G.Profile.Fire;default:throw new ArgumentException("unknown wide family");}
  }
  public static float Duration(string family)=>IsFamily(family)?(float)G.End(Profile(family)):0f;
  public static float Duration(ResolvedCombatFx.Ticket t)=>Duration(t?.AttackFamily);
  public static bool Available(string family)=>IsFamily(family)&&Ready(family);
  public static bool Land(WorldBattleFx.AttackPlayback handle)=>handle!=null&&handle.Land();
  static Color Role(JToken token) {
   if(token==null||!ColorUtility.TryParseHtmlString((string)token,out var c))throw new FormatException("invalid wide role");
   return c.linear; // Surface and independent scene palette sRGB decode once.
  }
  static Vector4 Vector(Color c)=>new Vector4(c.r,c.g,c.b,1);
  static bool Finite(float x)=>G.Finite(x);
  static bool Finite(Vector3 p)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z);
  public static bool Ready()=>Ready("wide_water")&&Ready("wide_fire");
  public static bool Ready(string family) {
   LastFailure=null;
   if(!IsFamily(family)){LastFailure="wide_profile_unknown_family";return false;}
   if(!Tween.SchedulingAllowed){LastFailure="wide_profile_scope_cancelled";return false;}
   if(!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){LastFailure="wide_profile_world_camera_unavailable";return false;}
   if(ElementBattleFx.ForceMissingForTest){LastFailure="wide_profile_forced_resource_missing";return false;}
   if(QualitySettings.activeColorSpace!=ColorSpace.Linear){LastFailure="wide_profile_requires_linear_project";return false;}
   int index=(int)Profile(family);bool water=index==0;
   if(Materials[index]!=null&&Textures[index]!=null&&(!water||_lookup!=null))return true;
   var shader=Shader.Find("DeckRogue/WideProfileFx");
   // Exact preserved existing imports. Never duplicate PNGs or mutate their importer/sampler state.
   var texture=Resources.Load<Texture2D>(water?"Art/fx/water-flow-surface-r1":"Art/fx/fire-continuous-surface-r1");
   var lookup=water?Resources.Load<Texture2D>("Art/fx/water-flow-droplet-lookup-r1"):null;
   var palette=Resources.Load<TextAsset>("Data/wide-profile-palette-r2");
   if(shader==null||!shader.isSupported||texture==null||texture.width!=(water?2172:1774)||texture.height!=(water?724:887)||water&&(lookup==null||lookup.width!=256||lookup.height!=128)||palette==null){LastFailure="wide_profile_shader_or_resource_unavailable";return false;}
   Material material=null;
   try {
    var j=JObject.Parse(palette.text);
    if((string)j["schema"]!="deckrogue.wide-profile-palette/2")throw new FormatException("invalid wide palette schema");
    var colors=new Color[2][];var lights=new Color[2];
    for(int i=0;i<2;i++) {
     var p=j["profiles"]?[G.ProfileId((G.Profile)i)];var roles=p?["roles_srgb"];
     colors[i]=new[]{Role(roles?["shadow"]),Role(roles?["primary"]),Role(roles?["edge"]),Role(roles?["core"])};
     lights[i]=Role(p?["scene_light_srgb"]);
    }
    material=new Material(shader);material.SetTexture("_BaseMap",texture);
    if(water)material.SetTexture("_LookupMap",lookup);
    _roles=colors;_lightColors=lights;Materials[index]=material;Textures[index]=texture;if(water)_lookup=lookup;return true;
   } catch(Exception e) {
    if(material!=null)UnityEngine.Object.Destroy(material);
    LastFailure="wide_profile_palette_invalid:"+e.GetType().Name;return false;
   }
  }
  // Bounded initial Unity mapping. These are UNCALIBRATED ratios, not Blender-W equivalence.
  // No scene contribution exists until actual Land; negative-age envelope is zero defensively.
  static WorldBattleFx.AttackSpec Spec(G.Profile profile)=>new WorldBattleFx.AttackSpec(
   new[]{0f,0f,0f,0f,0f,0f,0f,(float)G.LightEnd,(float)G.End(profile)},1,1,1,_lightColors[(int)profile],_lightColors[(int)profile],.025f,0f,0f) {
    ContractLighting=true,StandardContractPower=true,
    TargetPeakMultiplier=.35f*((profile==G.Profile.Water?38f:45f)/55f),FloorPeakMultiplier=.12f*((profile==G.Profile.Water?14f:16f)/20f),
    MaxCombineGroup="wide-profile-continuous-r2",
    Envelope=age=>age<0?0f:(float)G.LightEnvelope(profile,age)
   };
  static void DestroySet(Set s) {
   if(s==null)return;
   if(s.Draws!=null)foreach(var d in s.Draws)if(d?.Mesh!=null)UnityEngine.Object.Destroy(d.Mesh);
   if(s.Root!=null){s.Root.gameObject.SetActive(false);UnityEngine.Object.Destroy(s.Root.gameObject);}
  }
  static Draw CreateDraw(Transform root,int index,G.MeshData data,G.Profile profile) {
   var go=new GameObject("wide-part-"+index);Mesh mesh=null;
   try {
    go.transform.SetParent(root,false);mesh=new Mesh{name="wide-profile-continuous-r2-"+index};mesh.MarkDynamic();
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
  static Set CreateSet(Transform parent,G.Profile profile) {
   var root=new GameObject(G.ProfileId(profile)).transform;var set=new Set{Root=root};
   try {
    root.SetParent(parent,false);set.Geometry=G.Create(profile,1);
    set.Draws=new Draw[set.Geometry.Batches.Length];set.LightSpec=Spec(profile);
    for(int i=0;i<set.Draws.Length;i++)set.Draws[i]=CreateDraw(root,i,set.Geometry.Batches[i],profile);
    root.gameObject.SetActive(false);return set;
   } catch { DestroySet(set);throw; }
  }
  static void Retire(Slot s,int version) {
   if(s.Version!=version)return;s.Busy=false;s.Light?.Cancel();s.Light=null;
   if(s.Active?.Root!=null)s.Active.Root.gameObject.SetActive(false);
   if(s.Root!=null)s.Root.gameObject.SetActive(false);
  }
  public static void Cancel() { _epoch++;foreach(var s in Pool)if(s!=null){Retire(s,s.Version);s.Version++;} }
  static Slot Acquire(G.Profile profile) {
   int limit=GfxQuality.Level==GfxLevel.Low?4:8;Slot s=null;
   // A downgrade immediately enforces the Low live-slot bound without changing particle topology.
   for(int i=limit;i<Pool.Length;i++)if(Pool[i]!=null&&Pool[i].Busy){Retire(Pool[i],Pool[i].Version);Pool[i].Version++;}
   for(int i=0;i<limit;i++){if(Pool[i]==null)Pool[i]=new Slot();if(!Pool[i].Busy||Pool[i].Root==null){s=Pool[i];break;}}
   if(s==null){s=Pool[0];for(int i=1;i<limit;i++)if(Pool[i].Serial<s.Serial)s=Pool[i];Recycled++;}
   Retire(s,s.Version);s.Version++;s.Serial=++_serial;
   if(s.Root==null){for(int i=0;i<s.Sets.Length;i++){DestroySet(s.Sets[i]);s.Sets[i]=null;}s.Active=null;s.Root=new GameObject("wide-profile-continuous-r2").transform;}
   int p=(int)profile;if(s.Sets[p]==null)s.Sets[p]=CreateSet(s.Root,profile);
   for(int i=0;i<s.Sets.Length;i++)if(s.Sets[i]?.Root!=null)s.Sets[i].Root.gameObject.SetActive(i==p);
   s.Active=s.Sets[p];return s;
  }
  static bool Current(Slot s,int version,int epoch)=>epoch==_epoch&&s.Version==version&&s.Busy&&s.Root!=null;
  static void DrawAt(Slot s,float age,bool validateUpload=false) {
   var geometry=s.Active.Geometry;G.SampleReceipt(geometry,age,s.Available,s.ParticleCount);
   var roles=_roles[(int)geometry.Family];
   for(int j=0;j<s.Active.Draws.Length;j++) {
    var d=s.Active.Draws[j];var data=geometry.Batches[j];bool visible=false;
    var lo=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue);var hi=new Vector3(float.MinValue,float.MinValue,float.MinValue);
    for(int i=0;i<data.Vertices.Length;i++) {
     var p=data.Vertices[i];var v=new Vector3((float)p.X,(float)p.Y,-(float)p.Z);
     float alpha=(float)data.Alpha[i],gain=(float)data.Gain[i]*s.Emission,phase=(float)data.Phase[i];
     if(!Finite(v)||!Finite((float)data.UV[i].X)||!Finite((float)data.UV[i].Y)||!Finite(alpha)||!Finite(gain)||!Finite(phase))throw new InvalidOperationException("nonfinite_gpu_input");
     d.Vertices[i]=v;d.UV[i]=new Vector2((float)data.UV[i].X,(float)data.UV[i].Y);d.Phase[i]=new Vector2(phase,0f);
     d.Colors[i]=new Color(gain,gain,gain,alpha);visible|=alpha>0f;
     lo=new Vector3(Mathf.Min(lo.x,v.x),Mathf.Min(lo.y,v.y),Mathf.Min(lo.z,v.z));
     hi=new Vector3(Mathf.Max(hi.x,v.x),Mathf.Max(hi.y,v.y),Mathf.Max(hi.z,v.z));
    }
    d.Renderer.enabled=visible;if(!visible&&!validateUpload)continue;
    d.Mesh.SetVertices(d.Vertices);d.Mesh.SetUVs(0,d.UV);d.Mesh.SetUVs(1,d.Phase);d.Mesh.SetColors(d.Colors);
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
     else {float age=Time.time-s.Expected;if(age>=.35f)yield break;if(age<0)DrawAt(s,age);else Hide(s);}
     yield return null;
    }
   } finally { if(epoch==_epoch)Retire(s,version); }
  }
  public static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket ticket,float? contactAt=null) {
   float expected=contactAt??Time.time;
   return BeginCaptured(ticket,ticket?.Position??Vector3.zero,ticket?.ContactVisibleHeight??0f,Mathf.Max(0,expected-Time.time),expected);
  }
  // Explicit target-local capture API. A is available anticipation, never a new game wait.
  public static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket ticket,Vector3 point,float height,float availableA,float? contactAt=null) {
   float expected=contactAt??(Time.time+Mathf.Min((float)G.NativeLead,Mathf.Max(0,availableA)));
   return BeginCaptured(ticket,point,height,availableA,expected);
  }
  static WorldBattleFx.AttackPlayback BeginCaptured(ResolvedCombatFx.Ticket ticket,Vector3 point,float height,float availableA,float expected) {
   if(!Eligible(ticket)||!ticket.HasWorld||ticket.Outcome!="hit"||ticket.Amount<=0)return null;
   if(!Finite(expected)||!Finite(availableA)){ticket.RenderFallbackReason="wide_profile_invalid_contact_clock";return null;}
   if(!Finite(height)||height<=0f||!Finite(point)||!Finite(ticket.Feet)){ticket.RenderFallbackReason="wide_profile_invalid_captured_geometry";return null;}
   var visual=ticket.Visual;float size=visual?.Size??1f,emission=visual?.Emission??1f,particles=visual?.Particles??1f,light=visual?.Light??1f;
   float scale=height*size;
   if(!Finite(scale)||scale<=0f||!Finite(emission)||emission<0||!Finite(particles)||!Finite(light)||light<0){ticket.RenderFallbackReason="wide_profile_invalid_composed_visual";return null;}
   if(!Ready(ticket.AttackFamily)){ticket.RenderFallbackReason=LastFailure;return null;}
   var profile=Profile(ticket.AttackFamily);var cameraRotation=Stage.Camera.transform.rotation;
   var position=point;var feet=ticket.Feet;var target=ticket.Target;var impact=ticket.Impact;
   Slot s=null;
   try {
    s=Acquire(profile);s.Busy=true;s.Landed=false;s.Height=height;s.Size=size;s.Expected=expected;s.Available=Mathf.Min((float)G.NativeLead,Mathf.Min(Mathf.Max(0,availableA),Mathf.Max(0,expected-Time.time)));
    s.Emission=emission;s.Particles=Mathf.Clamp01(particles);s.LightPower=light;s.Tier=visual?.Tier;
    s.ParticleCount=Mathf.FloorToInt(G.Count(profile)*s.Particles);
    int version=s.Version,epoch=_epoch;
    s.Root.position=position;s.Root.rotation=cameraRotation;s.Root.localScale=Vector3.one*scale;s.Root.gameObject.SetActive(true);
    ticket.OutcomeIncomingReason="captured_camera_target_local_wide_passage";
    var captured=s;
    var playback=new WorldBattleFx.AttackPlayback {
     Contact=()=> {
      if(!Current(captured,version,epoch)){StaleContacts++;return false;}
      if(!Tween.SchedulingAllowed||!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){ticket.RenderFallbackReason="wide_profile_contact_environment_unavailable";Retire(captured,version);return false;}
      if(captured.Landed)return true;
      try {
       captured.Landed=true;captured.Started=Time.time;
       // Every batch uploads at actual contact, including zero-opacity future tongues/particles.
       DrawAt(captured,0f,true);
       if(Stage.ImpactTargetCurrent(target,impact)) {
        captured.Light=StageFx.BeginContractAttackLight(target,impact?.Target,epoch,position,feet,height,captured.Active.LightSpec,0f,false,
         ()=>Current(captured,version,epoch)&&Stage.ImpactTargetCurrent(target,impact),1f,captured.LightPower,()=>Time.time-captured.Started);
        captured.Light?.Land();
       }
       ticket.AuthoredRenderer="continuous-wide-profile-r2";return true;
      } catch(Exception e) { Retire(captured,version);ticket.RenderFallbackReason="wide_profile_contact_failed:"+e.GetType().Name;return false; }
     },
     Read=()=>Current(captured,version,epoch)?new {
      attack=G.ProfileId(profile),renderer="continuous-wide-profile-r2",worldCasterBeam=false,
      age=captured.Landed?Time.time-captured.Started:Time.time-captured.Expected,nativeAge=captured.Active.Geometry.VisualAge,
      landed=captured.Landed,height=captured.Height,size=captured.Size,damageTier=captured.Tier,version,serial=captured.Serial,
      anticipationUsed=captured.Available,visibleEnd=G.End(profile),particleCount=captured.ParticleCount,capturedCameraBasis=true,mirrorRight=1
     }:null
    };
    Tween.RunPresentation(Life(s,version,epoch));
    if(!Current(s,version,epoch)){ticket.RenderFallbackReason="wide_profile_scope_cancelled";return null;}
    Submitted++;return playback;
   } catch(Exception e) { if(s!=null)Retire(s,s.Version);ticket.RenderFallbackReason="wide_profile_construction_failed:"+e.GetType().Name;return null; }
  }
 }
}
