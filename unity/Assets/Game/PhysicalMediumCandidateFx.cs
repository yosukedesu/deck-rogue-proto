using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace DeckRogue.Game {
 // Stage41, deliberately OFF until Unity/Android visual and frame-time QA.
 // One parametric medium shape. No per-frame topology arrays/materials/objects or meshcache.
 internal static class PhysicalMediumCandidateFx {
  internal const string RendererId="procedural-physical-medium-r2-candidate";
  internal static bool Enabled{get;private set;}
  internal static string LastFailure{get;private set;}
  internal static int Submitted{get;private set;}
  internal const int Capacity=2;
  // Last cold shard birth+life; source .48 is only a conservative sampling horizon.
  internal const double VisibleEnd=.4075290594653786;
  internal sealed class Slot {
   internal Transform Root;internal Mesh Mesh;internal MeshRenderer Renderer;
   internal SharedPhysicalMediumGeometry.Buffer Geometry;
   internal Vector3[] Vertices;internal Vector2[] UV;internal Color[] Attributes;
   internal int Version;internal bool Busy,Landed;internal float Started;
   internal StageFx.ContractLightPlayback Light;
  }
  internal sealed class Lease {
   readonly Slot slot;readonly int version;readonly ResolvedCombatFx.Ticket ticket;readonly Func<bool> committed;
   internal readonly WorldBattleFx.AttackPlayback Playback;
   internal bool Current=>slot.Busy&&slot.Version==version&&slot.Root!=null;
   internal bool Idle=>!Current;
   internal Lease(Slot s,ResolvedCombatFx.Ticket t,Func<bool> proof){slot=s;version=s.Version;ticket=t;committed=proof;Playback=new WorldBattleFx.AttackPlayback{Contact=Land,Read=()=>new{renderer=RendererId,active=Current,landed=s.Landed,age=s.Geometry.VisualAge,vertices=s.Geometry.VertexCount,triangles=s.Geometry.TriangleCount,capacity=SharedPhysicalMediumGeometry.VertexCapacity,defaultEnabled=false}};}
   internal void Retire(){if(slot.Version!=version)return;slot.Busy=false;slot.Light?.Cancel();slot.Light=null;if(slot.Renderer!=null)slot.Renderer.enabled=false;if(slot.Root!=null)slot.Root.gameObject.SetActive(false);}
   internal void Draw(double age){
    if(!Current)return;
    if(age>=VisibleEnd){Retire();return;}
    if(age>=0&&!slot.Landed){slot.Renderer.enabled=false;return;}
    try {Upload(slot,age);}catch(Exception e){ticket.RenderFallbackReason="physical_medium_upload_failed:"+e.GetType().Name;Retire();}
   }
   bool Land(){
    if(!Current||!committed()||!Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact)||!Tween.SchedulingAllowed||!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){ticket.RenderFallbackReason="physical_medium_contact_unavailable";Retire();return false;}
    if(slot.Landed)return true;
    try {
     slot.Landed=true;slot.Started=Time.time;Draw(0);if(!Current)return false;
     if(Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact)) {
      slot.Light=StageFx.BeginContractAttackLight(ticket.Target,ticket.Impact?.Target,version,ticket.Position,ticket.Feet,ticket.ContactVisibleHeight,lightSpec,0,false,()=>Current&&Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact),1f,null,()=>Time.time-slot.Started);slot.Light?.Land();
     }
     // Publish success only after all contact-time dependencies have succeeded.
     ticket.AuthoredRenderer=RendererId;ticket.AuthoredTier="medium";ticket.AuthoredTierReason="exact_medium_candidate_opt_in";return true;
    } catch(Exception e){ticket.RenderFallbackReason="physical_medium_contact_failed:"+e.GetType().Name;Retire();return false;}
   }
  }
  static readonly Slot[] pool=new Slot[Capacity];static Material material;static WorldBattleFx.AttackSpec lightSpec;
  internal static int ActiveCount{get{int n=0;foreach(var s in pool)if(s!=null&&s.Busy)n++;return n;}}
  internal static void Configure(bool enabled){Enabled=enabled;} // Existing admitted tails keep their owner.
  internal static void Cancel(){foreach(var s in pool)if(s!=null){s.Busy=false;s.Version++;s.Light?.Cancel();s.Light=null;if(s.Root!=null)s.Root.gameObject.SetActive(false);}}
  static Color Linear(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c.linear;}
  static float Light(float age){if(age<0)return 0;float q=Mathf.Clamp01((age-.016f)/.12f);return 1-q*q*(3-2*q);}
  static bool Ready(){
   LastFailure=null;
   if(!Enabled||!Tween.SchedulingAllowed||!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){LastFailure="physical_medium_disabled_or_world_unavailable";return false;}
   if(ElementBattleFx.ForceMissingForTest||QualitySettings.activeColorSpace!=ColorSpace.Linear){LastFailure="physical_medium_resource_or_linear_space_unavailable";return false;}
   if(material!=null)return true;
   var shader=Shader.Find("DeckRogue/PhysicalMediumCandidate");if(shader==null||!shader.isSupported){LastFailure="physical_medium_shader_unavailable";return false;}
   Material candidate=null;
   try {
    candidate=new Material(shader);candidate.SetColor("_Shadow",Linear("#535E69"));candidate.SetColor("_Primary",Linear("#C6C6BE"));candidate.SetColor("_Edge",Linear("#DEDCD1"));candidate.SetColor("_Core",Linear("#F2EEE4"));
    var color=Linear("#C6C6BE");var spec=new WorldBattleFx.AttackSpec(new[]{0f,0f,0f,0f,0f,0f,0f,.016f,.136f},1,1,1,color,color,0,0,0){ContractLighting=true,StandardContractPower=true,TargetPeakMultiplier=.35f,FloorPeakMultiplier=.12f,MaxCombineGroup="physical-contact-r1",Envelope=Light};
    material=candidate;lightSpec=spec;return true;
   }catch(Exception e){if(candidate!=null)UnityEngine.Object.Destroy(candidate);LastFailure="physical_medium_material_failed:"+e.GetType().Name;return false;}
  }
  static Slot Create(){
   var s=new Slot();try {
    s.Root=new GameObject("physical-medium-r2-candidate").transform;s.Geometry=SharedPhysicalMediumGeometry.Create();
    s.Vertices=new Vector3[SharedPhysicalMediumGeometry.VertexCapacity];s.UV=new Vector2[s.Vertices.Length];s.Attributes=new Color[s.Vertices.Length];
    s.Mesh=new Mesh{name="physical-medium-r2-reusable"};s.Mesh.MarkDynamic();s.Root.gameObject.AddComponent<MeshFilter>().sharedMesh=s.Mesh;
    s.Renderer=s.Root.gameObject.AddComponent<MeshRenderer>();s.Renderer.sharedMaterial=material;s.Renderer.shadowCastingMode=ShadowCastingMode.Off;s.Renderer.receiveShadows=false;s.Renderer.enabled=false;s.Root.gameObject.SetActive(false);return s;
   }catch{if(s.Mesh!=null)UnityEngine.Object.Destroy(s.Mesh);if(s.Root!=null)UnityEngine.Object.Destroy(s.Root.gameObject);throw;}
  }
  // Seven disjoint semantic batches share one blend contract and become one indexed mesh.
  // Numeric COLOR = coverage,gain,kind,0: never sRGB-convert this attribute.
  static void Upload(Slot s,double age){
   var g=s.Geometry;SharedPhysicalMediumGeometry.Sample(g,age);bool visible=false;
   Vector3 lo=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue),hi=new Vector3(float.MinValue,float.MinValue,float.MinValue);
   foreach(var b in g.Batches)for(int i=b.VertexStart;i<b.VertexStart+b.VertexCount;i++){
    var p=g.Vertices[i];var v=new Vector3((float)p.X,(float)p.Y,-(float)p.Z);s.Vertices[i]=v;s.UV[i]=new Vector2((float)g.UV[i].X,(float)g.UV[i].Y);s.Attributes[i]=new Color((float)g.Alpha[i],(float)g.Gain[i],b.Kind,0);visible|=g.Alpha[i]>0;
    lo=new Vector3(Mathf.Min(lo.x,v.x),Mathf.Min(lo.y,v.y),Mathf.Min(lo.z,v.z));hi=new Vector3(Mathf.Max(hi.x,v.x),Mathf.Max(hi.y,v.y),Mathf.Max(hi.z,v.z));
   }
   s.Renderer.enabled=visible&&g.IndexCount>0;if(!s.Renderer.enabled)return;
   // Fixed upload counts avoid changing native mesh capacities with every torn cell.
   // Unused index triples are degenerate (0,0,0); maximum submitted count is 2852 triangles.
   Array.Clear(g.Triangles,g.IndexCount,g.Triangles.Length-g.IndexCount);
   s.Mesh.SetVertices(s.Vertices);s.Mesh.SetUVs(0,s.UV);s.Mesh.SetColors(s.Attributes);s.Mesh.SetTriangles(g.Triangles,0,g.Triangles.Length,0,false,0);s.Mesh.bounds=new Bounds((lo+hi)*.5f,hi-lo+Vector3.one*.001f);
  }
  internal static Lease Reserve(ResolvedCombatFx.Ticket ticket,Func<bool> committed){
   Slot acquired=null;
   try {
    if(!Ready())return null;
    if(!AttackScenarioAnchor.TryCapture(ticket,out var anchor)){LastFailure="physical_medium_captured_anchor_invalid";return null;}
    for(int i=0;i<pool.Length;i++)if(pool[i]==null||!pool[i].Busy||pool[i].Root==null){
     if(pool[i]?.Root==null){if(pool[i]!=null){pool[i].Version++;pool[i].Busy=false;pool[i].Light?.Cancel();pool[i].Light=null;}if(pool[i]?.Mesh!=null)UnityEngine.Object.Destroy(pool[i].Mesh);pool[i]=Create();}var s=pool[i];acquired=s;s.Version++;s.Landed=false;s.Root.position=anchor.Position;s.Root.rotation=anchor.Rotation;s.Root.localScale=Vector3.one*anchor.Height;s.Root.gameObject.SetActive(true);s.Renderer.enabled=false;s.Busy=true;Submitted++;return new Lease(s,ticket,committed);
    }
    LastFailure="physical_medium_capacity_legacy_fallback";return null;
   }catch(Exception e){if(acquired!=null){acquired.Busy=false;acquired.Version++;acquired.Light?.Cancel();acquired.Light=null;try{if(acquired.Renderer!=null)acquired.Renderer.enabled=false;if(acquired.Root!=null)acquired.Root.gameObject.SetActive(false);}catch{}}LastFailure="physical_medium_prepare_failed:"+e.GetType().Name;return null;}
  }
 }
}
