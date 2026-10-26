using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
namespace DeckRogue.Game {
 // Stage42 optical candidate, OFF by default. One real card/one medium receipt.
 // No texture frames, frame images, topology allocation, extra receipts or scene RNG.
 internal static class LightningR9CandidateFx {
  internal const string RendererId="procedural-lightning-r9-medium-candidate";
  internal const int Capacity=2;
  // Numerical conditioning only; geometric support is bounded independently below.
  const float FloorMinimumAbsTowardY=.0001f,FloorMaximumReachOverHeight=.80f;
  internal static bool Enabled{get;private set;}
  internal static string LastFailure{get;private set;}
  internal static int Submitted{get;private set;}
  sealed class Draw {
   internal Mesh Mesh;internal MeshRenderer Renderer;internal Vector3[] Vertices;
   internal Vector4[] UV0,UV1,UV2;
  }
  sealed class Slot {
   internal Transform Root;internal Draw[] Draws;internal LightningR9Geometry.Buffer Geometry;
   internal LightningR9BodyUnion.Buffer Union;
   internal LightningR9Binding Binding;internal GroundSurfaceProvider.Lease Ground;
   internal LightningR9FloorSupport.Domain Support;internal float FloorAnchorY;internal StrictGround.TriangleId FloorAnchorSource;
   internal Vector3 Contact,Feet,Toward,Correction;internal Quaternion Rotation;internal float Scale,Height,Emission,LightPower;
   internal StageFx.ContractLightPlayback Light;internal int Version;internal bool Busy,Landed;
  }
  static readonly Slot[] Pool=new Slot[Capacity];
  static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  static Material material,bodyMaterial;static int epoch;static WorldBattleFx.AttackSpec lightSpec;
  internal static int ActiveCount{get{int n=0;foreach(var s in Pool)if(s!=null&&s.Busy)n++;return n;}}
  // Changing admission leaves previously admitted tails alone. Explicit presentation
  // cancellation is the only reset owner and invalidates every lease/version.
  internal static void Configure(bool enabled){Enabled=enabled;}
  static bool Fail(ResolvedCombatFx.Ticket t,string reason){LastFailure="lightning_r9_"+reason;if(t!=null)t.RenderFallbackReason=LastFailure;return false;}
  static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
  static bool Finite(Vector3 p)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z);
  static bool ActorCurrent(ResolvedCombatFx.Ticket t){
   // Two bounded identity captures per admitted cast, never a per-frame lookup.
   var now=Stage.CaptureImpact(t.Target);return t.Impact?.Actor!=null&&now.Actor==t.Impact.Actor&&now.MotionVersion==t.Impact.MotionVersion;
  }
  static bool EnvironmentReady()=>Tween.SchedulingAllowed&&Stage.CombatWorld&&Stage.Camera!=null&&Stage.Camera.isActiveAndEnabled&&QualitySettings.activeColorSpace==ColorSpace.Linear&&!ElementBattleFx.ForceMissingForTest;
  static Color Linear(string hex){if(!ColorUtility.TryParseHtmlString(hex,out var c))throw new InvalidOperationException("r9_palette_invalid");return c.linear;}
  static WorldBattleFx.AttackSpec Spec()=>new WorldBattleFx.AttackSpec(new[]{0f,0f,0f,0f,0f,0f,0f,0f,(float)LightningR9Clock.VisibleEnd},1,1,1,Linear("#BAB4E3"),Linear("#928CC6"),0,.043f,.058f){ContractLighting=true,StandardContractPower=true,TargetPeakMultiplier=.35f*(42f/55f),FloorPeakMultiplier=0f,MaxCombineGroup="lightning-r9-medium"};
  internal static Action<Shader,bool> ReadinessForReview;
  static void ObserveReadiness(Shader shader,bool accepted){
   var observer=ReadinessForReview;if(observer==null)return;
   try{observer(shader,accepted);}catch(Exception){/* Observation must not alter rendering. */}
  }
  static bool Ready(ResolvedCombatFx.Ticket t){
   LastFailure=null;
   if(!Enabled||!EnvironmentReady())return Fail(t,"disabled_or_environment_unavailable");
   if(material!=null&&bodyMaterial!=null)return true;
   var shader=Shader.Find("DeckRogue/LightningR9Candidate");if(shader==null||!shader.isSupported){ObserveReadiness(shader,false);return Fail(t,"shader_unavailable");}
   ObserveReadiness(shader,true);
   var bodyShader=Shader.Find("DeckRogue/LightningR9BodyUnion");if(bodyShader==null||!bodyShader.isSupported){ObserveReadiness(bodyShader,false);return Fail(t,"body_shader_unavailable");}
   ObserveReadiness(bodyShader,true);
   Material next=null,nextBody=null;
   try {next=new Material(shader);nextBody=new Material(bodyShader);var high=Spec();material=next;bodyMaterial=nextBody;lightSpec=high;return true;}
   catch(Exception e){if(next!=null)UnityEngine.Object.Destroy(next);if(nextBody!=null)UnityEngine.Object.Destroy(nextBody);return Fail(t,"material_failed:"+e.GetType().Name);}
  }
  static void Retire(Slot s,int version){
   if(s.Version!=version)return;s.Busy=false;s.Light?.Cancel();s.Light=null;s.Binding=null;s.Ground=null;
   if(s.Draws!=null)foreach(var d in s.Draws)if(d?.Renderer!=null)d.Renderer.enabled=false;
   if(s.Root!=null)s.Root.gameObject.SetActive(false);
  }
  internal static void Cancel(){epoch++;foreach(var s in Pool)if(s!=null){Retire(s,s.Version);s.Version++;}}
  // Auto quality can fall to Low between frames. Retiring only the excess render
  // lease preserves the original receipt: pre-contact falls back, post-contact
  // merely shortens this optical tail. No scene light or gameplay owner is reset.
  static void EnforceQualityLimit(){if(GfxQuality.Level==GfxLevel.Low&&Pool[1]?.Busy==true)Retire(Pool[1],Pool[1].Version);}
  static void DestroySlot(Slot s){if(s==null)return;if(s.Draws!=null)foreach(var d in s.Draws)if(d?.Mesh!=null)UnityEngine.Object.Destroy(d.Mesh);if(s.Root!=null)UnityEngine.Object.Destroy(s.Root.gameObject);}
  static Slot Create(){
   var s=new Slot();try {
    s.Root=new GameObject("lightning-r9-medium-candidate").transform;s.Geometry=LightningR9Geometry.Create();s.Union=LightningR9BodyUnion.Create();s.Draws=new Draw[s.Geometry.Batches.Length];
    for(int i=0;i<s.Draws.Length;i++){
     var b=s.Geometry.Batches[i];var go=new GameObject("r9-"+(b.Floor?"ground":"channels"));go.transform.SetParent(s.Root,false);
     var d=new Draw();s.Draws[i]=d;d.Mesh=new Mesh{name="lightning-r9-fixed-"+i};d.Mesh.MarkDynamic();
     int count=b.Floor?b.Vertices.Length:s.Union.Body.Vertices.Length;
     d.Vertices=new Vector3[count];d.UV0=new Vector4[count];d.UV1=new Vector4[count];d.UV2=new Vector4[count];
     d.Mesh.vertices=d.Vertices;d.Mesh.triangles=b.Floor?b.Triangles:s.Union.Body.Triangles;go.AddComponent<MeshFilter>().sharedMesh=d.Mesh;d.Renderer=go.AddComponent<MeshRenderer>();d.Renderer.sharedMaterial=b.Floor?material:bodyMaterial;d.Renderer.shadowCastingMode=ShadowCastingMode.Off;d.Renderer.receiveShadows=false;d.Renderer.enabled=false;
    }
    s.Root.gameObject.SetActive(false);return s;
   }catch{DestroySlot(s);throw;}
  }
  static Slot Acquire(){
   EnforceQualityLimit();int limit=GfxQuality.Level==GfxLevel.Low?1:Capacity;
   for(int i=0;i<limit;i++)if(Pool[i]==null||!Pool[i].Busy||Pool[i].Root==null){
    if(Pool[i]?.Root==null){if(Pool[i]!=null){Retire(Pool[i],Pool[i].Version);Pool[i].Version++;DestroySlot(Pool[i]);}Pool[i]=Create();}
    var s=Pool[i];s.Version++;s.Busy=true;s.Landed=false;return s;
   }
   return null; // No eviction of an already-owned receipt/tail.
  }
  static bool Current(Slot s,int v,int e)=>e==epoch&&s.Version==v&&s.Busy&&s.Root!=null;
  static Vector3 Local(LightningR9Geometry.Point p)=>new Vector3((float)p.X,(float)p.Y,-(float)p.Z);
  static Vector4 V4(LightningR9Geometry.Quad q)=>new Vector4((float)q.X,(float)q.Y,(float)q.Z,(float)q.W);
  static Vector3 Position(Slot s,LightningR9Geometry.Point p,double weight,bool floor){
   if(floor)return LightningR9FloorSupport.Project(s.Rotation,s.Feet,s.FloorAnchorY,s.Scale,s.Height,s.Toward,p);
   var offset=s.Rotation*Local(p)*s.Scale;
   return s.Contact+offset+s.Correction*(float)weight;
  }
  // Explicit diagnostic only. Never capture terrain or mutate the admitted slot.
  // All geometry work uses a separate buffer, and observer failures cannot affect rendering.
  internal static Action<object> PlacementForReview;
  static float[] ReviewPoint(Vector3 p)=>new[]{p.x,p.y,p.z};
  static double[] ReviewPoint(StrictGround.D3 p)=>new[]{p.X,p.Y,p.Z};
  static void ObservePlacementBasis(Slot s,ResolvedCombatFx.Ticket t){
   var observer=PlacementForReview;if(observer==null)return;object observation;
   try {
    var camera=Stage.Camera;var q=s.Rotation;
    bool scaleFinite=Finite(s.Scale),towardFinite=Finite(s.Toward);
    float denominator=Mathf.Abs(s.Toward.y),reach=0f;
    bool calculable=scaleFinite&&s.Scale>0f&&towardFinite&&denominator>1e-6f,projectedFinite=true;
    var lo=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue);var hi=new Vector3(float.MinValue,float.MinValue,float.MinValue);
    var contactLo=lo;var contactHi=hi;int verticesPerSample=0,samples=0;
    if(calculable){
     var diagnostic=LightningR9Geometry.Create(); // isolated scratch, never s.Geometry
     // Frozen floor paths have full-length fixed segments; fragmentation only
     // switches joined contact normals to raw inactive normals. These two states
     // therefore bound every floor vertex for this geometry, without time sampling.
     for(int sample=0;sample<2;sample++){
      double age=sample==0?0:-.14;
      LightningR9Geometry.Sample(diagnostic,age);int count=0;
      foreach(var batch in diagnostic.Batches)if(batch.Floor)foreach(var point in batch.Vertices){
       var world=LightningR9FloorSupport.Project(s.Rotation,s.Feet,s.Feet.y,s.Scale,s.Height,s.Toward,point);count++;
       if(!Finite(world)){projectedFinite=false;continue;}
       var relative=world-s.Feet;
       lo=new Vector3(Mathf.Min(lo.x,relative.x),Mathf.Min(lo.y,relative.y),Mathf.Min(lo.z,relative.z));
       hi=new Vector3(Mathf.Max(hi.x,relative.x),Mathf.Max(hi.y,relative.y),Mathf.Max(hi.z,relative.z));
       if(sample==0){contactLo=new Vector3(Mathf.Min(contactLo.x,relative.x),Mathf.Min(contactLo.y,relative.y),Mathf.Min(contactLo.z,relative.z));contactHi=new Vector3(Mathf.Max(contactHi.x,relative.x),Mathf.Max(contactHi.y,relative.y),Mathf.Max(contactHi.z,relative.z));}
       reach=Mathf.Max(reach,Mathf.Abs(relative.x),Mathf.Abs(relative.z));
      }
      verticesPerSample=count;samples++;
     }
    }
    bool validProjection=calculable&&projectedFinite&&verticesPerSample>0;
    observation=new{phase="basis-before-production-gate",frame=Time.frameCount,time=Time.time,target=t.Target,
     height=s.Height,visibleHeight=s.Height,scale=s.Scale,scaleFinite,towardFinite,toward=ReviewPoint(s.Toward),absTowardY=denominator,
     originalGateMinimumAbsTowardY=.10f,originalBasisGateWouldAccept=scaleFinite&&s.Scale>0f&&towardFinite&&denominator>=.10f,
     boundedGateMinimumAbsTowardY=FloorMinimumAbsTowardY,boundedBasisGateWouldAccept=scaleFinite&&s.Scale>0f&&towardFinite&&denominator>=FloorMinimumAbsTowardY,
     boundedGateMaximumReachOverHeight=FloorMaximumReachOverHeight,boundedFootprintGateWouldAccept=validProjection&&reach<=FloorMaximumReachOverHeight*s.Height,
     capturedRotation=new[]{q.x,q.y,q.z,q.w},capturedRotationMagnitudeSquared=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w,capturedRight=ReviewPoint(q*Vector3.right),capturedUp=ReviewPoint(q*Vector3.up),
     cameraPosition=camera!=null?ReviewPoint(camera.transform.position):null,cameraFieldOfView=camera!=null?camera.fieldOfView:0f,
     anchorPosition=ReviewPoint(t.Position),feet=ReviewPoint(s.Feet),
     projectionCalculated=validProjection,projectionNumericDenominatorMinimum=1e-6f,sampleCount=samples,floorVerticesPerSample=verticesPerSample,
     envelopeRelativeMin=validProjection?ReviewPoint(lo):null,envelopeRelativeMax=validProjection?ReviewPoint(hi):null,
     contactRelativeMin=validProjection?ReviewPoint(contactLo):null,contactRelativeMax=validProjection?ReviewPoint(contactHi):null,
     envelopeMaxWorldXZReach=validProjection?reach:0f,envelopeReachOverHeight=validProjection?reach/s.Height:0f,
     projectionExtentMethod="union of contact and fully inactive fixed-floor normals",sampleAges=new[]{0.0,-.14},
     certificateHalfExtentsAtTargetHeight=new[]{1.25f*s.Height,.85f*s.Height},
     insideUnitHeightCertificate=validProjection&&Mathf.Max(Mathf.Abs(lo.x),Mathf.Abs(hi.x))<=1.25f*s.Height&&Mathf.Max(Mathf.Abs(lo.z),Mathf.Abs(hi.z))<=.85f*s.Height,
     hypotheticalProofHeight=validProjection?Mathf.Max(s.Height,reach/.80f):0f,
     boundedFootprintProposal=.80f,proposalFootprintWithinBound=validProjection&&reach<=.80f*s.Height,
     terrainCaptureAttemptedByDiagnostic=false,terrainSupportProvedByDiagnostic=false,
     note="Pure frozen-geometry projection bound only, not terrain acceptance. Original .10 angle fields are historical; boundedGate fields describe current admission."};
   }catch(Exception ex){observation=new{phase="observation-error",location="basis-before-production-gate",errorType=ex.GetType().Name,errorMessage=ex.Message};}
   try{observer(observation);}catch(Exception){/* A failing observer is never retried and cannot alter the route. */}
  }
  static void ObservePlacementCapture(Slot s,ResolvedCombatFx.Ticket t,float reach,float proofH,bool accepted,GroundSurfaceProvider.Lease lease,string reason){
   var observer=PlacementForReview;if(observer==null)return;object observation;
   try{var g=lease?.Geometry;observation=new{phase="after-original-terrain-capture",frame=Time.frameCount,time=Time.time,target=t.Target,reach,proofHeight=proofH,
    visibleHeight=s.Height,certificateHeight=s.Support.CertificateHeight,captureCenter=ReviewPoint(s.Support.Center),
    supportWorldXZBounds=new[]{s.Support.MinX,s.Support.MaxX,s.Support.MinZ,s.Support.MaxZ},
    originalTerrainCaptureAccepted=accepted,originalTerrainCaptureReason=reason,terrainCaptureAttemptedByDiagnostic=false,
    normal=g!=null?ReviewPoint(g.Normal):null,origin=g!=null?ReviewPoint(g.Origin):null,right=g!=null?ReviewPoint(g.Right):null,forward=g!=null?ReviewPoint(g.Forward):null,
    effectiveHeight=g!=null?g.EffectiveH:0,triangleIndexCount=g!=null?g.Indices.Count:0,
    note="Unmodified terrain capture with an R9-derived support domain; final anchor and exact-frame containment are checked next."};}
   catch(Exception ex){observation=new{phase="observation-error",location="after-original-terrain-capture",errorType=ex.GetType().Name,errorMessage=ex.Message};}
   try{observer(observation);}catch(Exception){/* A failing observer is never retried and cannot alter the route. */}
  }
  static void ObservePlacementSupport(Slot s,ResolvedCombatFx.Ticket t,StrictGround.Capture g){
   var observer=PlacementForReview;if(observer==null)return;object observation;
   try{var delta=new StrictGround.D3(s.Feet.x-g.Origin.X,s.Feet.y-g.Origin.Y,s.Feet.z-g.Origin.Z);
    observation=new{phase="floor-support-admitted",frame=Time.frameCount,time=Time.time,target=t.Target,
     visibleHeight=s.Height,certificateHeight=s.Support.CertificateHeight,bodyFeet=ReviewPoint(s.Feet),floorAnchorY=s.FloorAnchorY,
     floorAnchorSource=s.FloorAnchorSource.ToString(),displayToGroundDeltaY=(double)s.Feet.y-s.FloorAnchorY,
     bodyNormalHeightOverCertificate=StrictGround.D3.Dot(delta,g.Normal)/g.EffectiveH,
     bodyNormalHeightLimit=StrictGround.GroundCapture.MaxHeight+StrictGround.GroundCapture.GuardTolerance,
     captureCenter=ReviewPoint(s.Support.Center),supportWorldXZBounds=new[]{s.Support.MinX,s.Support.MaxX,s.Support.MinZ,s.Support.MaxZ},
     floorVerticesPerState=s.Support.VerticesPerState,groundBias=.0015f*s.Height,
     terrainCaptureAttemptedByDiagnostic=false,allLifetimeVerticesContained=true,
     note="Actual certified source intersection at body-feet XZ; body/contact/light unchanged. Exact-frame containment is also required before each GPU floor upload."};
   }catch(Exception ex){observation=new{phase="observation-error",location="floor-support-admitted",errorType=ex.GetType().Name,errorMessage=ex.Message};}
   try{observer(observation);}catch(Exception){/* Observation cannot alter rendering. */}
  }
  static bool Placement(Slot s,ResolvedCombatFx.Ticket t){
   if(!AttackScenarioAnchor.TryCapture(t,out var a)||!Stage.ImpactTargetCurrent(t.Target,t.Impact)||!ActorCurrent(t))return Fail(t,"captured_anchor_invalid");
   s.Rotation=a.Rotation;s.Feet=a.Feet;s.Height=a.Height;s.Scale=a.Height*.75f;s.Toward=s.Rotation*Vector3.back;
   ObservePlacementBasis(s,t);
   if(!Finite(s.Scale)||s.Scale<=0||!Finite(s.Toward)||Mathf.Abs(s.Toward.y)<FloorMinimumAbsTowardY)return Fail(t,"floor_view_basis_unproved");
   // There is no anatomical shoulder socket in StageUnit. This is a captured,
   // explicit upper-left body proxy, requiring native sprite alignment approval.
   s.Contact=a.Position+(s.Rotation*Vector3.up)*(.25f*a.Height)-(s.Rotation*Vector3.right)*(.20f*a.Height);
   s.Correction=s.Feet-s.Contact-s.Rotation*Local(LightningR9Geometry.ReferenceFoot)*s.Scale;
   // AttackScenarioAnchor captured this exact camera rotation. Capture position
   // and projection mode once as well; active tails never reread camera placement.
   LightningR9BodyUnion.Capture(s.Union,Stage.Camera.transform.position,s.Rotation,s.Contact,s.Correction,s.Scale,Stage.Camera.orthographic);
   if(!LightningR9FloorSupport.TryDomain(s.Geometry,s.Rotation,s.Feet,s.Scale,s.Height,s.Toward,out s.Support,out var domainReason))return Fail(t,domainReason);
   float proofH=s.Support.CertificateHeight;
   bool captured=Stage.TryCaptureSurface(s.Support.Center,proofH,1,StrictGround.Family.Stomp,out var lease,out var reason);
   ObservePlacementCapture(s,t,(float)s.Support.Reach,proofH,captured,lease,reason);
   if(!captured||lease?.Geometry==null)return Fail(t,reason??"floor_capture_unavailable");
   var g=lease.Geometry;
   if(!LightningR9FloorSupport.TryAnchor(g,s.Feet,s.Support,out s.FloorAnchorY,out s.FloorAnchorSource,out reason))return Fail(t,reason);
   // The actual returned frame can be slightly tilted. Check final world-space
   // float vertices, including real-plane Y and visible-H bias, in that frame.
   // The closed convex support prism then contains every triangle interior.
   for(int state=0;state<2;state++){
    LightningR9Geometry.Sample(s.Geometry,state==0?-.14:0);
    foreach(var batch in s.Geometry.Batches)if(batch.Floor)foreach(var p in batch.Vertices)
     if(!LightningR9FloorSupport.ContainsFloor(g,Position(s,p,0,true)))return Fail(t,"floor_outside_certificate");
   }
   if(!lease.IsCurrent(out reason))return Fail(t,reason??"floor_lease_unavailable");
   ObservePlacementSupport(s,t,g);
   s.Ground=lease;return true;
  }
  static void Upload(Slot s,double age,bool validateInvisible=false){
   LightningR9Geometry.Sample(s.Geometry,age);
   // The frozen sampler still owns timing/energy and the unchanged all-lifetime
   // floor certificate. Its folded ribbons are never submitted to the GPU.
   LightningR9BodyUnion.Sample(s.Union,s.Geometry);
   for(int j=0;j<s.Draws.Length;j++){
    var b=s.Geometry.Batches[j];var d=s.Draws[j];var lo=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue);var hi=new Vector3(float.MinValue,float.MinValue,float.MinValue);
    var body=s.Union.Body;
    for(int i=0;i<d.Vertices.Length;i++){
     var p=b.Floor?Position(s,s.Union.FloorVertices[i],0,true):body.Vertices[i];
     if(!Finite(p))throw new InvalidOperationException("r9_gpu_vertex_nonfinite");
     if(b.Floor&&!LightningR9FloorSupport.ContainsFloor(s.Ground?.Geometry,p))throw new InvalidOperationException("r9_gpu_floor_outside_certificate");
     d.Vertices[i]=p;d.UV0[i]=b.Floor?s.Union.FloorUV0[i]:body.UV0[i];
     d.UV1[i]=b.Floor?V4(b.UV1[i]):body.UV1[i];d.UV2[i]=b.Floor?V4(b.UV2[i]):body.UV2[i];
     lo=new Vector3(Mathf.Min(lo.x,p.x),Mathf.Min(lo.y,p.y),Mathf.Min(lo.z,p.z));hi=new Vector3(Mathf.Max(hi.x,p.x),Mathf.Max(hi.y,p.y),Mathf.Max(hi.z,p.z));
    }
    bool visible=b.Floor?b.Visible:body.Visible;
    d.Renderer.enabled=visible;if(!visible&&!validateInvisible)continue;
    d.Mesh.SetVertices(d.Vertices);d.Mesh.SetUVs(0,d.UV0);d.Mesh.SetUVs(1,d.UV1);d.Mesh.SetUVs(2,d.UV2);d.Mesh.bounds=new Bounds((lo+hi)*.5f,hi-lo+Vector3.one*.001f);
    Props.Clear();Props.SetFloat("_Emission",s.Emission);
    if(!b.Floor)LightningR9BodyUnion.Apply(Props,s.Union);
    d.Renderer.SetPropertyBlock(Props);
   }
  }
  static IEnumerator Life(Slot s,int v,int e,ResolvedCombatFx.Ticket t){
   try {while(Current(s,v,e)){
    EnforceQualityLimit();if(!Current(s,v,e))yield break;
    if(!EnvironmentReady()||s.Binding.ResidueCurrent?.Invoke()!=true||!s.Ground.IsCurrent(out _))yield break;
    double age=s.Binding.Clock.Age(Time.time);
    if(s.Landed&&age>=LightningR9Clock.VisibleEnd)yield break;
    if(!s.Landed&&(s.Binding.BeforeContact?.Invoke()!=true||Time.time-s.Binding.Clock.Plan.FirstContactAt>.35))yield break;
    try{Upload(s,age);}catch(Exception ex){Fail(t,"upload_failed:"+ex.GetType().Name);yield break;}
    yield return null;
   }}finally{if(e==epoch)Retire(s,v);}
  }
  internal static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket t,float? contactAt=null){
   if(!Enabled||t?.LightningR9Binding==null)return null;
   if(!Ready(t)||t.LightningR9Binding.BeforeContact?.Invoke()!=true)return null;
   if(!contactAt.HasValue||!Finite(contactAt.Value)||Time.time>=t.LightningR9Binding.Clock.Plan.FirstContactAt){Fail(t,"preview_clock_unavailable");return null;}
   Slot s=null;
   try {
    s=Acquire();if(s==null){Fail(t,"capacity_legacy_fallback");return null;}
    s.Binding=t.LightningR9Binding;s.Emission=t.Visual?.Emission??1;s.LightPower=t.Visual?.Light??1;
    if(!Placement(s,t)){Retire(s,s.Version);return null;}
    int v=s.Version,e=epoch;var captured=s;s.Root.position=Vector3.zero;s.Root.rotation=Quaternion.identity;s.Root.localScale=Vector3.one;s.Root.gameObject.SetActive(true);
    // Validate every upload before publishing the preview lease, even dormant pieces.
    Upload(s,s.Binding.Clock.Age(Time.time),true);
    var playback=new WorldBattleFx.AttackPlayback{Contact=()=>{
     EnforceQualityLimit();if(!Current(captured,v,e))return false;if(captured.Landed)return true;
     if(!EnvironmentReady()||!Stage.ImpactTargetCurrent(t.Target,t.Impact)||!ActorCurrent(t)||captured.Binding.AfterCommit?.Invoke()!=true||!captured.Ground.VerifyContact(out _)){Fail(t,"contact_owner_or_ground_unavailable");Retire(captured,v);return false;}
     try {
      if(!captured.Binding.Clock.Land(Time.time))throw new InvalidOperationException("r9_land_clock_invalid");captured.Landed=true;Upload(captured,0,true);
      // One target endpoint per slot; no floor point light. At most two R9
      // endpoints overall (Low one), without evicting existing scene effects.
      if(StageFx.CanAdmitContractTargetWithoutEviction(t.Target,t.Impact?.Target,e,lightSpec.MaxCombineGroup)) {
       captured.Light=StageFx.BeginContractAttackLight(t.Target,t.Impact?.Target,e,captured.Contact,captured.Feet,captured.Height,lightSpec,0,false,()=>Current(captured,v,e)&&Stage.ImpactTargetCurrent(t.Target,t.Impact),1f,captured.LightPower,()=>(float)captured.Binding.Clock.Age(Time.time));captured.Light?.Land();
      }
      t.AuthoredRenderer=RendererId;t.AuthoredTier="medium";t.AuthoredTierReason="exact_aether1_medium_opt_in";return true;
     }catch(Exception ex){Fail(t,"contact_failed:"+ex.GetType().Name);Retire(captured,v);return false;}
    },Read=()=>Current(captured,v,e)?new{renderer=RendererId,age=captured.Binding.Clock.Age(Time.time),landed=captured.Landed,visibleEnd=LightningR9Clock.VisibleEnd,capacity=Capacity,defaultEnabled=false,anatomicalSocket=false,anchor="captured_upper_left_proxy",flatFloorOnly=true}:null};
    Tween.RunPresentation(Life(s,v,e,t));if(!Current(s,v,e))return null;Submitted++;return playback;
   }catch(Exception ex){if(s!=null)Retire(s,s.Version);Fail(t,"prepare_failed:"+ex.GetType().Name);return null;}
  }
 }
}
