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
   public bool UseCold;public Hd2d.NativeEffect ColdEffect;
   public bool Busy,Landed;public int Version,Particles;public long Serial;public float Started,Expected,Emission;public G.Profile Profile;public ResolvedCombatFx.Ticket Ticket;
  }
  static readonly Slot[] Pool=new Slot[24];static readonly MaterialPropertyBlock Props=new MaterialPropertyBlock();
  static Material _material;static Color[] _primary,_core;static int _epoch;static long _serial;
  static Material _coldMaterial;static Texture2D _coldDensity;
  internal static int ColdReviewMode;
  internal const float ColdProjectionScale=1.36f;
  internal static float ColdReviewScale=ColdProjectionScale;
  internal static void SetColdForward(bool enabled){if(_coldMaterial!=null)_coldMaterial.SetShaderPassEnabled("ColdForward",enabled);}
  internal static void CollectColdRenderers(System.Collections.Generic.List<Renderer> result){result.Clear();foreach(var s in Pool)if(s?.Busy==true&&s.UseCold&&s.Renderer.enabled&&s.Root.gameObject.activeInHierarchy)result.Add(s.Renderer);}
  static JObject ColdGpuProbe(Material material,Texture2D texture,Color core){
   var mesh=new Mesh();var rt=new RenderTexture(8,8,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);var read=new Texture2D(8,8,TextureFormat.RGBAFloat,false,true);var old=RenderTexture.active;
   var cmd=new CommandBuffer();
   try{
    mesh.vertices=new[]{new Vector3(-1,-1,0),new Vector3(1,-1,0),new Vector3(1,1,0),new Vector3(-1,1,0)};mesh.triangles=new[]{0,1,2,0,2,3};
    mesh.uv=new[]{Vector2.one*.5f,Vector2.one*.5f,Vector2.one*.5f,Vector2.one*.5f};mesh.uv2=new[]{new Vector2(0,2),new Vector2(0,2),new Vector2(0,2),new Vector2(0,2)};
    mesh.colors=new[]{new Color(1,1,1,.5f),new Color(1,1,1,.5f),new Color(1,1,1,.5f),new Color(1,1,1,.5f)};
    var props=new MaterialPropertyBlock();props.SetTexture("_BaseMap",texture);props.SetVector("_Core",new Vector4(core.r,core.g,core.b,core.a));props.SetFloat("_Age",0);props.SetFloat("_Gain",.72f);props.SetFloat("_ReviewMode",0);
    rt.Create();cmd.SetRenderTarget(rt);cmd.ClearRenderTarget(true,true,Color.clear);cmd.SetViewProjectionMatrices(Matrix4x4.identity,GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-1,1,-1,1,-1,1),true));cmd.DrawMesh(mesh,Matrix4x4.identity,material,0,0,props);Graphics.ExecuteCommandBuffer(cmd);
    RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,8,8),0,0);read.Apply();var gpu=read.GetPixel(4,4);var raw=texture.GetPixelBilinear(.39f,.46f);var linear=raw.linear;float alpha=.5f*raw.a;var expected=Color.Lerp(linear,core,.55f)*(.72f*alpha);
    return new JObject{["scope"]="isolated real shader role2 UV.5 age0 gain.72 alpha.5; float linear RT, no stage/postprocess",["gpuPremultLinear"]=new JArray(gpu.r,gpu.g,gpu.b,gpu.a),["cpuApproxPremultLinear"]=new JArray(expected.r,expected.g,expected.b,alpha),["rawGamma"]=new JArray(raw.r,raw.g,raw.b,raw.a),["coreLinear"]=new JArray(core.r,core.g,core.b)};
   }finally{RenderTexture.active=old;cmd.Release();rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(mesh);UnityEngine.Object.Destroy(read);}
  }
  static bool ReadyCold() {
   if(_coldMaterial!=null)return true;
   var shader=Resources.Load<Shader>("Shaders/Hd2dColdSweepSurface");
   _coldDensity=Resources.Load<Texture2D>("Art/fx/cold-frost-density-v1");
   if(shader==null || !shader.isSupported || _coldDensity==null || _coldDensity.width!=1774 || _coldDensity.height!=887 || _coldDensity.mipmapCount!=1){LastFailure="cold297_shader_or_original_density_unavailable";return false;}
   _coldMaterial=new Material(shader);return true;
  }
  public static string LastFailure {get;private set;}
  public static int Submitted {get;private set;}public static int Recycled {get;private set;}public static int StaleContacts {get;private set;}
  public static int ActiveCount {get{int n=0;foreach(var s in Pool)if(s!=null&&s.Busy)n++;return n;}}
  internal static int VisibleReviewDraws {get{int n=0;foreach(var s in Pool)if(s!=null&&s.Renderer.enabled&&s.Root.gameObject.activeInHierarchy)n++;return n;}}
  internal static JArray CaptureReviewState(){
   var result=new JArray();foreach(var s in Pool)if(s?.Busy==true){
    int visible=0;float maxAlpha=0;foreach(var c in s.Colors){if(c.a>0)visible++;maxAlpha=Mathf.Max(maxAlpha,c.a);}
    var actual=new MaterialPropertyBlock();s.Renderer.GetPropertyBlock(actual);
    var parameters=new JObject{["age"]=actual.GetFloat("_Age"),["gain"]=actual.GetFloat("_Gain"),["review"]=actual.GetFloat("_ReviewMode"),["maxVertexAlpha"]=maxAlpha};
    if(s.UseCold){var tex=(Texture2D)actual.GetTexture("_BaseMap");var c=tex.GetPixelBilinear(.39f,.46f);parameters["textureFormat"]=tex.graphicsFormat.ToString();parameters["cpuSampleRaw"]=new JArray(c.r,c.g,c.b,c.a);parameters["zTest"]=s.Renderer.sharedMaterial.GetFloat("_ZTest");}
    if(s.UseCold){
     var vertices=new JArray();
     for(int i=64;i<72;i++){var w=s.Root.TransformPoint(s.Vertices[i]);var screen=Stage.Camera.WorldToScreenPoint(w);var view=Stage.Camera.worldToCameraMatrix.MultiplyPoint3x4(w);vertices.Add(new JObject{["index"]=i,["imageXY"]=new JArray(screen.x,Screen.height-screen.y),["viewZ"]=view.z,["uv"]=new JArray(s.UV[i].x,s.UV[i].y),["mode"]=new JArray(s.Mode[i].x,s.Mode[i].y),["alpha"]=s.Colors[i].a});}
     parameters["role2Vertices"]=vertices;
     var receiver=s.Ticket?.Impact?.Target as Component;
     if(receiver!=null){var w=receiver.transform.TransformPoint(new Vector3(0,.5f,0));parameters["receiverCenterViewZ"]=Stage.Camera.worldToCameraMatrix.MultiplyPoint3x4(w).z;var rr=receiver.GetComponent<MeshRenderer>();parameters["receiverQueue"]=rr?.sharedMaterial?.renderQueue;}
     parameters["effectQueue"]=s.Renderer.sharedMaterial.renderQueue; parameters["focusPassFrame"]=ColdSweepFocusPass.ExecutedFrame; parameters["focusDraws"]=ColdSweepFocusPass.ExecutedDraws; parameters["forwardPassEnabled"]=s.Renderer.sharedMaterial.GetShaderPassEnabled("ColdForward"); parameters["focusFailure"]=ColdSweepFocusPass.Failure;
     if(s.Landed && Time.time-s.Started<.001f)parameters["isolatedGpuProbe"]=ColdGpuProbe(s.Renderer.sharedMaterial,_coldDensity,_core[(int)s.Profile.Material]);
    }
    var lo=new Vector2(float.PositiveInfinity,float.PositiveInfinity);var hi=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
    for(int i=0;i<s.Vertices.Length;i++)if(s.Colors[i].a>0){var p=Stage.Camera.WorldToScreenPoint(s.Root.TransformPoint(s.Vertices[i]));lo=Vector2.Min(lo,p);hi=Vector2.Max(hi,p);}

    result.Add(new JObject{["materialParameters"]=parameters,["scale"]=s.Root.localScale.x,["emission"]=s.Emission,["pixelBounds"]=visible>0?new JArray(lo.x,lo.y,hi.x,hi.y):new JArray(),["triangleIndices"]=s.Mesh.triangles.Length,["serial"]=s.Serial,["family"]=s.Profile.Id,["cold297"]=s.UseCold,["committed"]=s.Ticket?.ColdSweepBinding?.Committed==true,["landed"]=s.Landed,["age"]=s.Landed?Time.time-s.Started:Time.time-s.Expected,["enabled"]=s.Renderer.enabled && s.Root.gameObject.activeInHierarchy,["verticesWithAlpha"]=visible,["shader"]=s.Renderer.sharedMaterial?.shader?.name,["textureWidth"]=s.UseCold?_coldDensity.width:0,["textureHeight"]=s.UseCold?_coldDensity.height:0});
   }return result;
  }
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
  static void Retire(Slot s,int version){if(s==null||s.Version!=version)return;s.Busy=false;s.UseCold=false;s.ColdEffect?.ConfigureColdSweepCommittedContact?.Invoke(false);s.Ticket=null;if(s.Renderer!=null)s.Renderer.enabled=false;if(s.Root!=null)s.Root.gameObject.SetActive(false);}
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
   Hd2d.NativeBatch cold=null;
   if(s.UseCold){s.ColdEffect.ConfigureColdSweepCommittedContact(s.Ticket?.ColdSweepBinding?.Committed==true);s.ColdEffect.Sample(age);cold=s.ColdEffect.Batches[0];}
   else G.Sample(s.Geometry,s.Profile,age,s.Particles);
   var b=s.Geometry;bool visible=false;
   for(int i=0;i<s.Vertices.Length;i++){
    if(cold!=null){
     var p=cold.Position(i);float alpha=cold.Alpha(i);var uv=cold.Uv(i);var mode=cold.Effect(i);
     if(!p.IsFinite() || !G.Finite(alpha))throw new InvalidOperationException("cold297_nonfinite_gpu_input");
     s.Vertices[i]=p;s.UV[i]=uv;s.Mode[i]=mode;s.Colors[i]=new Color(1,1,1,alpha);visible|=alpha>0;continue;
    }
    if(!G.Finite(b.X[i])||!G.Finite(b.Y[i])||!G.Finite(b.Alpha[i]))throw new InvalidOperationException("nonfinite_gpu_input");
    s.Vertices[i]=new Vector3((float)b.X[i],(float)b.Y[i],0);s.UV[i]=new Vector2((float)b.U[i],(float)b.V[i]);s.Mode[i]=new Vector2((float)b.Soft[i],0);s.Colors[i]=new Color(s.Emission,s.Emission,s.Emission,(float)b.Alpha[i]);visible|=b.Alpha[i]>0;
   }
   s.Mesh.SetVertices(s.Vertices);s.Mesh.SetUVs(0,s.UV);s.Mesh.SetUVs(1,s.Mode);s.Mesh.SetColors(s.Colors);s.Renderer.enabled=visible;
   // Palette values are already linear. SetColor would decode sRGB a second time.
   var primary=_primary[(int)s.Profile.Material];var core=_core[(int)s.Profile.Material];
   Props.Clear();Props.SetVector("_Primary",new Vector4(primary.r,primary.g,primary.b,primary.a));Props.SetVector("_Core",new Vector4(core.r,core.g,core.b,core.a));s.Renderer.SetPropertyBlock(Props);
   if(s.UseCold){Props.SetTexture("_BaseMap",_coldDensity);Props.SetFloat("_Age",age);Props.SetFloat("_Gain",s.Emission);Props.SetFloat("_ReviewMode",ColdReviewMode==1||ColdReviewMode==3?1:ColdReviewMode>=4?ColdReviewMode:0);s.Renderer.SetPropertyBlock(Props);_coldMaterial.SetFloat("_ZTest",ColdReviewMode==2||ColdReviewMode==3?8:4);}
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
   bool ownedCold=!neutralFallback && t.ColdSweepBinding?.Eligible(t)==true;
   if(ownedCold)scale*=ColdReviewScale;
   if(ownedCold && !ReadyCold()){t.RenderFallbackReason=LastFailure;return null;}
   var basis=Stage.Camera.transform.rotation;if(!CaptureValid(t.Position,scale,basis)){t.RenderFallbackReason="bounded_contact_invalid_camera_basis";return null;}
   Slot s=null;
   try{
    s=Acquire();s.Busy=true;s.Landed=false;s.Expected=Time.time;s.Emission=emission;s.Profile=G.Find(neutralFallback?"minimal_neutral_contact":t.AttackFamily);s.Particles=G.MoteCount(particles);s.Ticket=t;
    s.Root.position=t.Position;s.Root.rotation=basis;s.Root.localScale=Vector3.one*scale;s.Root.gameObject.SetActive(true);s.Renderer.sharedMaterial=_material;
    s.UseCold=ownedCold;
    if(ownedCold){
     if(s.ColdEffect==null)s.ColdEffect=Hd2d.Hd2dColdSweepGeometry.Create();
     s.ColdEffect.ConfigureParticleGain?.Invoke(particles);
     s.ColdEffect.ConfigureSource?.Invoke(Quaternion.Inverse(basis)*(t.BeamOrigin-t.Position)/scale);
     s.ColdEffect.ConfigureColdSweepCommittedContact(false);
     s.Renderer.sharedMaterial=_coldMaterial;
     Debug.Log("[ColdSweep297] owned contact uid="+t.SourceUid+" target="+t.Target+" texture="+_coldDensity.width+"x"+_coldDensity.height);
    }
    int version=s.Version,epoch=_epoch;var captured=s;
    var handle=new WorldBattleFx.AttackPlayback{
     Contact=()=>{
      if(!Current(captured,version,epoch)){StaleContacts++;t.RenderFallbackReason="bounded_contact_stale_pool_lease";return false;}
      if(captured.Landed)return true;
      if(!G.Finite(Time.time)||!Tween.SchedulingAllowed||!Stage.CombatWorld||Stage.Camera==null||!Stage.Camera.isActiveAndEnabled){t.RenderFallbackReason="bounded_contact_environment_unavailable";Retire(captured,version);return false;}
      try{captured.Started=Time.time;captured.Landed=true;Draw(captured,0);t.AuthoredRenderer=ownedCold?"cold-sweep-contact-r297":neutralFallback?"bounded-neutral-fallback-r1":"bounded-contact-r1";t.AuthoredTier=v?.Tier;t.AuthoredTierReason=neutralFallback?"neutral_receipt_fallback":"composed_receipt_contact";return true;}
      catch(Exception e){t.RenderFallbackReason="bounded_contact_upload_failed:"+e.GetType().Name;Retire(captured,version);return false;}
     },
     Read=()=>Current(captured,version,epoch)?new{family=captured.Profile.Id,shape=captured.Profile.Form.ToString(),surface=captured.Profile.Material.ToString(),renderer="bounded-contact-r1",landed=captured.Landed,age=captured.Landed?Time.time-captured.Started:0f,visibleEnd=G.End,particleCount=captured.Particles,worldTransport=false,anticipation=0f,sceneLights=0,version,serial=captured.Serial,damageTier=t.Intensity?.Tier,renderTier=v?.Tier}:null
    };
    Tween.RunPresentation(Life(s,version,epoch));if(!Current(s,version,epoch)){t.RenderFallbackReason="bounded_contact_scope_cancelled";return null;}Submitted++;return handle;
   }catch(Exception e){if(s!=null)Retire(s,s.Version);t.RenderFallbackReason="bounded_contact_construction_failed:"+e.GetType().Name;return null;}
  }
 }
}
