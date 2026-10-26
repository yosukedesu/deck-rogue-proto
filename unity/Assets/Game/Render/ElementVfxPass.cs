using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
namespace DeckRogue.Game {
 // Only authored elemental quads are composited after stage bloom/tonemapping.
 // World lights, actor/floor receivers and physical effects keep the original pipeline.
 public sealed class ElementVfxPass:ScriptableRenderPass {
  sealed class Data {public readonly List<Renderer> Renderers=new List<Renderer>(24);}
  static ElementVfxPass pass;
  readonly List<Renderer> active=new List<Renderer>(24);
  internal static bool ForceUnavailableForTest,ForceSkipForTest;
  static bool faulted;static int requestedFrame=-1,executedFrame=-1;
  public static string FallbackReason {get;private set;}
  internal static void ResetTestFault(){faulted=false;FallbackReason=null;requestedFrame=executedFrame=-1;}
  public static bool CanUsePost(Camera camera){
   if(!ElementBattleFx.SeparateMaterial || faulted || ForceUnavailableForTest || camera==null)return false;
   if(!camera.TryGetComponent<UniversalAdditionalCameraData>(out var data) || !(data.scriptableRenderer is UniversalRenderer))return false;
   var settings=GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
   if(settings!=null && settings.enableRenderCompatibilityMode)return false;
   var shader=Shader.Find("DeckRogue/ElementBattleFx");return shader!=null && shader.isSupported;
  }
  static void Fail(string reason){if(!faulted){faulted=true;FallbackReason=reason;Debug.LogWarning("[ElementVfxFallback] "+reason);}WorldBattleFx.RestoreLegacyElements();}
  static void End(ScriptableRenderContext context,Camera camera){if(camera==Stage.Camera && requestedFrame==Time.frameCount && executedFrame!=Time.frameCount)Fail("post pass did not execute; retained quads use legacy forward material");}
  public static int DrawnFrames {get;private set;}
  public ElementVfxPass(){renderPassEvent=RenderPassEvent.AfterRenderingPostProcessing;requiresIntermediateTexture=true;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Install(){RenderPipelineManager.beginCameraRendering-=Begin;RenderPipelineManager.beginCameraRendering+=Begin;RenderPipelineManager.endCameraRendering-=End;RenderPipelineManager.endCameraRendering+=End;DrawnFrames=0;ResetTestFault();}
  static void Begin(ScriptableRenderContext context,Camera camera){
   if(!ElementBattleFx.SeparateMaterial || camera==null || camera!=Stage.Camera || !Stage.CombatWorld)return;
   if(!CanUsePost(camera)){WorldBattleFx.RestoreLegacyElements();return;}
   if(camera.TryGetComponent<UniversalAdditionalCameraData>(out var data)){if(pass==null)pass=new ElementVfxPass();WorldBattleFx.CollectElementRenderers(pass.active);if(pass.active.Count>0){requestedFrame=Time.frameCount;data.scriptableRenderer.EnqueuePass(pass);}}
  }
  public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame){
   if(ForceSkipForTest)return;
   WorldBattleFx.CollectElementRenderers(active);if(active.Count==0){executedFrame=Time.frameCount;return;}
   var resources=frame.Get<UniversalResourceData>();
   if(!resources.activeColorTexture.IsValid() || !resources.activeDepthTexture.IsValid()){Fail("missing color/depth attachment");return;}
   var colorDesc=graph.GetTextureDesc(resources.activeColorTexture);var depthDesc=graph.GetTextureDesc(resources.activeDepthTexture);
   if(colorDesc.msaaSamples!=depthDesc.msaaSamples){Fail("color/depth sample count mismatch");return;}
   using(var builder=graph.AddRasterRenderPass<Data>("Element VFX after stage bloom",out var data)){
    data.Renderers.Clear();data.Renderers.AddRange(active);
    builder.SetRenderAttachment(resources.activeColorTexture,0,AccessFlags.ReadWrite);
    builder.SetRenderAttachmentDepth(resources.activeDepthTexture,AccessFlags.Read);
    builder.AllowPassCulling(false);
    builder.SetRenderFunc((Data d,RasterGraphContext ctx)=>{
     foreach(var renderer in d.Renderers)if(renderer!=null)ctx.cmd.DrawRenderer(renderer,renderer.sharedMaterial,0,0);
     executedFrame=Time.frameCount;DrawnFrames++;
    });
   }
  }
 }
}
