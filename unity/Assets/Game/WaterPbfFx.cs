using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;
namespace DeckRogue.Game {
 // Default-OFF, blue_strike-only cache experiment. No fluid solve, mesh lerp, actor clock or receipt writes.
 internal static class WaterPbfFx {
  const string Resource="Art/fx/water-pbf-r4-stage40",ExpectedSha="c76656df7412d7f66b9f2af4dd43fd69ed51856425ab4858764bb18b7ff94445";
  internal const long WindowBudget=12L*1024*1024;
  const int MaxVertices=32768,MaxIndices=180000;
  internal static bool Enabled{get;private set;}internal static bool Ready=>Enabled&&cache!=null&&material!=null;
  internal static long ModeledWindowBytes{get;private set;}
  internal static string LastFailure{get;private set;}internal static int Submitted{get;private set;}
  internal static readonly Dictionary<string,int> Rejections=new Dictionary<string,int>();
  static WaterStage40Cache cache;static WaterStage40FrameBuffer tailBase;static WaterStage40Tail tail;static WaterStage40DecodeWorkspace workspace;
  static Material material;static bool loading;static int epoch,loadGeneration;
  static readonly Slot[] Slots=new Slot[1];static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  sealed class Slot {
   internal bool Busy,Landed;internal int Version;internal Transform Root;internal Mesh Mesh;internal MeshRenderer Renderer;
   internal WaterStage40FrameBuffer Frame,Landing;internal WaterPbfBinding Binding;internal GroundSurfaceProvider.Lease Ground;
   internal readonly List<Vector3> V=new List<Vector3>(MaxVertices),N=new List<Vector3>(MaxVertices);
   internal readonly List<Vector2> K=new List<Vector2>(MaxVertices);internal readonly List<int> I=new List<int>(MaxIndices);
   internal StageFx.ContractLightPlayback Light,FloorLight;internal float Height;
  }
  internal static int ActiveCount{get{int n=0;foreach(var s in Slots)if(s!=null&&s.Busy)n++;return n;}}
  static bool Fail(string why){LastFailure=why;Rejections.TryGetValue(why,out int n);Rejections[why]=n+1;return false;}
  // Explicit comparison API only. No PlayerPrefs, quality preset, normal launch or other family enables this.
  internal static void Configure(bool enabled){Enabled=enabled;if(!enabled){loadGeneration++;Cancel();ReleaseResources();return;}if(!loading&&cache==null)Tween.I.StartCoroutine(Preload());}
  static void ReleaseResources(){
   for(int i=0;i<Slots.Length;i++){var s=Slots[i];if(s==null)continue;if(s.Mesh!=null)UnityEngine.Object.Destroy(s.Mesh);if(s.Root!=null)UnityEngine.Object.Destroy(s.Root.gameObject);Slots[i]=null;}
   if(material!=null)UnityEngine.Object.Destroy(material);material=null;cache=null;tailBase=null;tail=null;workspace=null;ModeledWindowBytes=0;
  }
  static IEnumerator Preload(){
   loading=true;int generation=loadGeneration;ResourceRequest request=null;
   try{request=Resources.LoadAsync<TextAsset>(Resource);}catch(Exception ex){Fail("pbf_request_failed:"+ex.GetType().Name);loading=false;}
   if(request==null)yield break;yield return request;var asset=request.asset as TextAsset;
   try{if(Enabled&&generation==loadGeneration)Load(asset);}catch(Exception ex){Fail("pbf_preload_failed:"+ex.GetType().Name);}
   finally{if(asset!=null)Resources.UnloadAsset(asset);loading=false;}
   // A board reset cancels playback, not shared resource preparation. OFF->ON invalidates
   // an old load but starts one replacement; persistent failure never spins retry loops.
   if(Enabled&&cache==null&&generation!=loadGeneration)Tween.I.StartCoroutine(Preload());
  }
  static void Load(TextAsset asset){
   if(asset==null)throw new InvalidOperationException("resource_missing");
   var bytes=asset.bytes;if(bytes.Length>12*1024*1024)throw new FormatException("deployment_cap");
   using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=ExpectedSha)throw new FormatException("content_hash");
   var decoded=WaterStage40Cache.Open(bytes);var scratch=decoded.CreateDecodeWorkspace();var last=decoded.CreateFrameBuffer();decoded.DecodeFrameInto(156,last,scratch);var ending=decoded.DecodeTail();
   var shader=Shader.Find("DeckRogue/WaterPbfCandidate");if(shader==null||!shader.isSupported)throw new InvalidOperationException("shader_unavailable");
   long estimate=(MaxVertices*32L+MaxIndices*4L)*3+last.EstimatedManagedBytes*3+decoded.GetRecordInfo(157).EstimatedDecodedManagedBytes+scratch.EstimatedRetainedManagedBytes+WaterStage40Cache.DeflateWorkingAllowanceBytes+2L*1024*1024;
   if(estimate>WindowBudget)throw new InvalidOperationException("decoded_window_budget");ModeledWindowBytes=estimate;
   material=new Material(shader);cache=decoded;tailBase=last;tail=ending;workspace=scratch;LastFailure=null;
  }
  // Keep exceptions inside the explicit asynchronous warmup, never inside the contact owner.
  internal static bool Candidate(ResolvedCombatFx.Ticket t)=>Ready&&t?.PbfBinding!=null&&t.SourceKey=="blue_strike"&&t.SourceKind=="card"&&t.ExactSource&&t.SourceUnit=="player"&&t.Outcome=="hit"&&t.Amount>0&&t.Total==1&&t.Index==0;
  static Vector3 U(WaterStage40Vector3 p)=>new Vector3(p.X,p.Z,p.Y); // Z-up RH -> Y-up LH; indices reversed below.
  static bool Finite(Vector3 p)=>WaterPbfClock.Finite(p.x)&&WaterPbfClock.Finite(p.y)&&WaterPbfClock.Finite(p.z);
  static Vector3 D(StrictGround.D3 p)=>new Vector3((float)p.X,(float)p.Y,(float)p.Z);
  static bool Placement(ResolvedCombatFx.Ticket t,out GroundSurfaceProvider.Lease lease,out Quaternion yaw,out float scale,out string reason){
   lease=null;yaw=Quaternion.identity;scale=t.ContactVisibleHeight/2.3f;reason="pbf_geometry_unproved";
   if(!Finite(t.Feet)||!Finite(t.BeamOrigin)||!t.HasBeamOrigin||!WaterPbfClock.Finite(scale)||scale<=0)return false;
   var direction=t.Feet-t.BeamOrigin;direction.y=0;if(direction.sqrMagnitude<.000001f){reason="pbf_source_direction_missing";return false;}direction.Normalize();
   yaw=Quaternion.AngleAxis(Mathf.Atan2(-direction.z,direction.x)*Mathf.Rad2Deg,Vector3.up);
   // Do not compound damage Size into a baked collision proxy. Tier remains standard for this trial.
   float radius=.04f*Mathf.Sqrt(2.8f);var lo=U(cache.PositionMin)-Vector3.one*radius;var hi=U(cache.PositionMax)+Vector3.one*radius;
   float reach=0;for(int i=0;i<8;i++){var p=yaw*new Vector3((i&1)==0?lo.x:hi.x,0,(i&2)==0?lo.z:hi.z)*scale;reach=Mathf.Max(reach,Mathf.Abs(p.x),Mathf.Abs(p.z));}
   // Existing complete-support certificate, enlarged before capture; never scale captured ground vertices.
   float proofH=Mathf.Max(t.ContactVisibleHeight,reach/.80f);
   if(!Stage.TryCaptureSurface(t.Feet,proofH,1,StrictGround.Family.Stomp,out lease,out reason))return false;
   var g=lease.Geometry;const double tolerance=.00005; // numerical tolerance in world meters, not artistic terrain flattening.
   foreach(var p in g.UnbiasedVertices){var world=g.World(p);if(Math.Abs(world.Y-t.Feet.y)>tolerance){reason="pbf_nonplanar_or_offset_floor";return false;}}
   if(g.Indices.Count==0||Math.Abs(g.Normal.Y-1)>1e-6){reason="pbf_nonhorizontal_floor";return false;}
   for(int i=0;i<4;i++){
    var p=t.Feet+yaw*new Vector3((i&1)==0?lo.x:hi.x,0,(i&2)==0?lo.z:hi.z)*scale;
    var q=new StrictGround.D3(p.x-g.Origin.X,p.y-g.Origin.Y,p.z-g.Origin.Z);
    if(Math.Abs(StrictGround.D3.Dot(q,g.Right)/g.EffectiveH)>1.25||Math.Abs(StrictGround.D3.Dot(q,g.Forward)/g.EffectiveH)>.85){reason="pbf_footprint_outside_certificate";return false;}
   }
   reason=null;return true;
  }
  static bool EnvironmentReady()=>Tween.SchedulingAllowed&&Stage.CombatWorld&&Stage.Camera!=null&&Stage.Camera.isActiveAndEnabled&&QualitySettings.activeColorSpace==ColorSpace.Linear&&!ElementBattleFx.ForceMissingForTest;
  static bool Current(Slot s,int v,int e)=>e==epoch&&Enabled&&s.Busy&&s.Version==v&&s.Root!=null;
  static void Retire(Slot s,int v){if(s.Version!=v)return;s.Busy=false;s.Light?.Cancel();s.Light=null;s.FloorLight?.Cancel();s.FloorLight=null;s.Binding=null;s.Ground=null;if(s.Root!=null)s.Root.gameObject.SetActive(false);}
  internal static void Cancel(){epoch++;foreach(var s in Slots)if(s!=null){Retire(s,s.Version);s.Version++;}}
  static Slot Acquire(){foreach(var s in Slots)if(s!=null&&!s.Busy)return s;for(int i=0;i<Slots.Length;i++)if(Slots[i]==null){
    var s=new Slot{Frame=cache.CreateFrameBuffer(),Landing=cache.CreateFrameBuffer()};var go=new GameObject("water-pbf-r4-candidate");s.Root=go.transform;s.Mesh=new Mesh{name="water-pbf-window"};s.Mesh.MarkDynamic();go.AddComponent<MeshFilter>().sharedMesh=s.Mesh;s.Renderer=go.AddComponent<MeshRenderer>();s.Renderer.sharedMaterial=material;s.Renderer.shadowCastingMode=ShadowCastingMode.Off;s.Renderer.receiveShadows=false;go.SetActive(false);return Slots[i]=s;
   }return null;}
  internal static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket t,float contactAt){
   if(!Candidate(t))return null;string why=null;Slot s=null;
   try{
    if(!EnvironmentReady())throw new InvalidOperationException("environment_unavailable");
    if(!Stage.ImpactTargetCurrent(t.Target,t.Impact)){Fail("pbf_target_lease_unavailable");t.RenderFallbackReason=LastFailure;return null;}
    if(t.PbfBinding.BeforeContact?.Invoke()!=true){Fail("pbf_receipt_owner_unready");return null;}
    if(!Placement(t,out var ground,out var yaw,out float scale,out why)){Fail(why);t.RenderFallbackReason=why;return null;}
    s=Acquire();if(s==null){Fail("pbf_one_slot_cap");t.RenderFallbackReason=LastFailure;return null;}
    s.Version++;s.Busy=true;s.Landed=false;s.Binding=t.PbfBinding;s.Ground=ground;s.Height=t.ContactVisibleHeight;
    cache.DecodeFrameInto(21,s.Landing,workspace); // Prepared before contact. Never perform resource I/O at Land.
    s.Root.position=t.Feet;s.Root.rotation=yaw;s.Root.localScale=Vector3.one*scale;s.Root.gameObject.SetActive(true);s.Renderer.enabled=false;
    int v=s.Version,e=epoch;var captured=s;
    var playback=new WorldBattleFx.AttackPlayback{Contact=()=>{
     if(!Current(captured,v,e))return false;if(captured.Landed)return true;
     if(!EnvironmentReady()){Retire(captured,v);t.RenderFallbackReason="pbf_contact_environment_unavailable";Fail(t.RenderFallbackReason);return false;}
     if(!Stage.ImpactTargetCurrent(t.Target,t.Impact)){Retire(captured,v);t.RenderFallbackReason="pbf_target_lease_replaced";Fail(t.RenderFallbackReason);return false;}
     if(captured.Binding.AfterCommit?.Invoke()!=true||!captured.Ground.VerifyContact(out why)){Retire(captured,v);t.RenderFallbackReason=why??"pbf_commit_unproved";Fail(t.RenderFallbackReason);return false;}
     try{captured.Landed=true;captured.Binding.Clock.Land(Time.time);var previous=captured.Frame;captured.Frame=captured.Landing;captured.Landing=previous;Draw(captured,WaterPbfClock.Contact);
      StartLight(captured,t,v,e);t.AuthoredRenderer="cached-water-pbf-r4-candidate";return true;
     }catch(Exception ex){Retire(captured,v);t.RenderFallbackReason="pbf_contact_failed:"+ex.GetType().Name;Fail(t.RenderFallbackReason);return false;}
    },Read=()=>Current(captured,v,e)?new{renderer="cached-water-pbf-r4-candidate",source=captured.Binding.Clock.Source(Time.time),landed=captured.Landed,visibleEnd=WaterPbfClock.Residue,proxyAdaptation="approximate_capsule",flatFloorOnly=true}:null};
    Tween.RunPresentation(Life(s,v,e));if(!Current(s,v,e))return null;Submitted++;return playback;
   }catch(Exception ex){if(s!=null)Retire(s,s.Version);t.RenderFallbackReason="pbf_begin_failed:"+ex.GetType().Name;Fail(t.RenderFallbackReason);return null;}
  }
  static IEnumerator Life(Slot s,int v,int e){try{while(Current(s,v,e)){
    if(!EnvironmentReady()||s.Binding.ResidueCurrent?.Invoke()!=true||!s.Ground.IsCurrent(out _))yield break;
    double source=s.Binding.Clock.Source(Time.time);if(s.Landed&&source>=WaterPbfClock.End)yield break;
    if(!s.Landed&&(s.Binding.BeforeContact?.Invoke()!=true||Time.time-s.Binding.Clock.Plan.FirstContactAt>.35))yield break;
    if(source<0||(!s.Landed&&Time.time>=s.Binding.Clock.Plan.FirstContactAt))s.Renderer.enabled=false;else{
     bool failed=false;try{Draw(s,source);}catch(Exception ex){Fail("pbf_playback_failed:"+ex.GetType().Name);failed=true;}if(failed)yield break;
    }yield return null;
   }}finally{if(e==epoch)Retire(s,v);}}
  static void StartLight(Slot s,ResolvedCombatFx.Ticket t,int v,int e){
   // Blender watts are not copied to Unity. Independent bounded, uncalibrated max-composed anchors.
   var color=new Color(.78f,.88f,.96f);
   WorldBattleFx.AttackSpec Spec(bool floor)=>new WorldBattleFx.AttackSpec(new[]{0f,0f,0f,0f,0f,0f,0f,.1f,.3425f},1,1,1,color,color,0,0,0){ContractLighting=true,StandardContractPower=true,TargetPeakMultiplier=floor?0:.22f,FloorPeakMultiplier=floor?.035f:0,MaxCombineGroup="water-pbf-r4",Envelope=age=>{double t0=age+WaterPbfClock.Contact;return (float)(floor?Pulse(t0,13.0/60,.29,.43):Pulse(t0,.10,.175,.30));}};
   Func<bool> current=()=>Current(s,v,e)&&Stage.ImpactTargetCurrent(t.Target,t.Impact);Func<float> age=()=>Time.time-(float)s.Binding.Clock.ActualLand;
   s.Light=StageFx.BeginContractAttackLight(t.Target,t.Impact?.Target,e,t.Position,t.Feet,s.Height,Spec(false),0,false,current,1,1,age);s.Light?.Land();
   s.FloorLight=StageFx.BeginContractAttackLight(t.Target,t.Impact?.Target,e,t.Position,t.Feet,s.Height,Spec(true),0,false,current,1,1,age);s.FloorLight?.Land();
  }
  static double Pulse(double t,double a,double peak,double end){if(t<=a||t>=end)return 0;double q=t<peak?(t-a)/(peak-a):(end-t)/(end-peak);return q*q*(3-2*q);}
  static readonly Vector3[] Icosa={new Vector3(0f,0f,-1f),new Vector3(0.72359997f,-0.52572f,-0.447214991f),new Vector3(-0.276385009f,-0.850639999f,-0.447214991f),new Vector3(-0.894424975f,0f,-0.447214991f),new Vector3(-0.276385009f,0.850639999f,-0.447214991f),new Vector3(0.72359997f,0.52572f,-0.447214991f),new Vector3(0.276385009f,-0.850639999f,0.447214991f),new Vector3(-0.72359997f,-0.52572f,0.447214991f),new Vector3(-0.72359997f,0.52572f,0.447214991f),new Vector3(0.276385009f,0.850639999f,0.447214991f),new Vector3(0.894424975f,0f,0.447214991f),new Vector3(0f,0f,1f)};
  static readonly int[] Faces={0,2,1,1,5,0,0,3,2,0,4,3,0,5,4,1,10,5,2,6,1,3,7,2,4,8,3,5,9,4,1,6,10,2,7,6,3,8,7,4,9,8,5,10,9,6,11,10,7,11,6,8,11,7,9,11,8,10,11,9};
  static Vector3 TailPosition(Vector3 p,Vector3 v,double dt){p+=v*(float)dt;p.y-=.5f*9.81f*(float)(dt*dt);p.y=Mathf.Max(.045f,p.y);var center=new Vector3(0,Mathf.Clamp(p.y,.4f,1.7f),0);var delta=p-center;float d=delta.magnitude;if(d<.36f){p=center+(d>1e-8?delta/d:Vector3.left)*.36f;p.y=Mathf.Max(.045f,p.y);center.y=Mathf.Clamp(p.y,.4f,1.7f);delta=p-center;if(delta.sqrMagnitude<.36f*.36f){float rr=Mathf.Sqrt(Mathf.Max(0,.36f*.36f-delta.y*delta.y));var flat=new Vector3(p.x,0,p.z);p=center+(flat.sqrMagnitude>1e-12?flat.normalized:Vector3.left)*rr+Vector3.up*delta.y;}}return p;}
  static Vector3 TailVelocity(Vector3 p,Vector3 v,double dt){var free=p+v*(float)dt;free.y=Mathf.Max(.045f,free.y-.5f*9.81f*(float)(dt*dt));var projected=TailPosition(p,v,dt);if((free-projected).sqrMagnitude>1e-14)return (TailPosition(p,v,dt+1e-5)-TailPosition(p,v,dt-1e-5))/.00002f;v.y-=9.81f*(float)dt;if(free.y<=.045f)v.y=0;return v;}
  static void AddDrop(Slot s,Vector3 p,Vector3 velocity,float radius,float stretch,bool foam){
   if(radius<=0)return;Vector3 dir=velocity.sqrMagnitude>1e-12?velocity.normalized:Vector3.right;var reference=Mathf.Abs(dir.y)>.9f?Vector3.forward:Vector3.up;var side=-Vector3.Cross(dir,reference).normalized;var up=-Vector3.Cross(dir,side);int start=s.V.Count;float transverse=1/Mathf.Sqrt(stretch);
   foreach(var q in Icosa){s.V.Add(p+radius*(q.x*dir*stretch+(q.y*side+q.z*up)*transverse));s.N.Add((q.x*dir/stretch+(q.y*side+q.z*up)/transverse).normalized);s.K.Add(new Vector2(foam?2:1,0));}
   for(int k=0;k<Faces.Length;k++)s.I.Add(start+Faces[k]);
  }
  static void Draw(Slot s,double source){
   if(source>=WaterPbfClock.End){s.Renderer.enabled=false;return;}
   bool ending=source>WaterPbfClock.MainEnd;int tick=WaterPbfClock.Tick(source);
   if(!ending&&s.Frame.Tick!=tick)cache.DecodeFrameInto(tick,s.Frame,workspace);
   var frame=ending?tailBase:s.Frame;if(frame.VertexCount+12L*frame.ParticleCount>MaxVertices||frame.TriangleCount*3L+60L*frame.ParticleCount>MaxIndices)throw new InvalidOperationException("expanded_geometry_cap");s.V.Clear();s.N.Clear();s.K.Clear();s.I.Clear();double dt=Math.Max(0,source-.65);
   int key=Math.Min(11,Math.Max(0,(int)(dt*60)));float normalQ=(float)(dt*60-key);
   for(int i=0;i<frame.VertexCount;i++){
    var p=U(frame.Positions[i]);p.y=Mathf.Max(.008f,p.y);var n=U(frame.Normals[i]);if(ending){var v=U(tail.Velocities[i]);p=new Vector3((float)((double)p.x+v.x*dt),Mathf.Max(.008f,(float)((double)p.y+v.y*dt-.5*9.81*dt*dt)),(float)((double)p.z+v.z*dt));var blend=Vector3.Lerp(U(tail.NormalKeys[key][i]),U(tail.NormalKeys[key+1][i]),normalQ);n=blend.sqrMagnitude>1e-12f?blend.normalized:U(tail.NormalKeys[key][i]);}
    s.V.Add(p);s.N.Add(n);s.K.Add(Vector2.zero);
   }
   for(int i=0;i<frame.TriangleCount*3;i+=3){s.I.Add(frame.Triangles[i]);s.I.Add(frame.Triangles[i+2]);s.I.Add(frame.Triangles[i+1]);}
   for(int i=0;i<frame.ParticleCount;i++){
    var a=frame.Particles[i];var p=U(a.Position);var vel=U(a.Velocity);float radius=a.Radius,stretch=a.Stretch;
    if(ending){p=TailPosition(p,vel,dt);vel=TailVelocity(U(a.Position),vel,dt);stretch=Mathf.Clamp(1+vel.magnitude*.14f,1,2.8f);}

    p.y=Mathf.Max(.045f,p.y);AddDrop(s,p,vel,radius,stretch,a.Flags==2);
   }
   if(s.V.Count>MaxVertices||s.I.Count>MaxIndices)throw new InvalidOperationException("expanded_geometry_cap");
   s.Mesh.Clear();s.Mesh.SetVertices(s.V);s.Mesh.SetNormals(s.N);s.Mesh.SetUVs(0,s.K);s.Mesh.SetTriangles(s.I,0);s.Mesh.RecalculateBounds();
   Props.Clear();Props.SetFloat("_Fade",(float)WaterPbfClock.Fade(source));s.Renderer.SetPropertyBlock(Props);s.Renderer.enabled=true;
  }
 }
}
