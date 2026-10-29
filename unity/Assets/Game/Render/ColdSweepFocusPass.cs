using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
namespace DeckRogue.Game {
 // Only owned Cold297 is composited after TiltShift(550), before URP postprocessing.
 // In this URP version the custom550..599 range is recorded before built-in postprocessing.
 internal sealed class ColdSweepFocusPass:ScriptableRenderPass {
  sealed class Data {public readonly List<Renderer> Renderers=new List<Renderer>();}
  static ColdSweepFocusPass pass;static readonly List<Renderer> active=new List<Renderer>();
  internal static bool Enabled=true;static bool faulted;static int requested=-1;
  internal static int ExecutedFrame=-1,ExecutedDraws;internal static string Failure;
  ColdSweepFocusPass(){renderPassEvent=(RenderPassEvent)((int)RenderPassEvent.BeforeRenderingPostProcessing+1);requiresIntermediateTexture=true;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Install(){RenderPipelineManager.beginCameraRendering-=Begin;RenderPipelineManager.beginCameraRendering+=Begin;RenderPipelineManager.endCameraRendering-=End;RenderPipelineManager.endCameraRendering+=End;faulted=false;Failure=null;ExecutedFrame=requested=-1;}
  static void Fail(string why){faulted=true;Failure=why;BoundedContactFx.SetColdForward(true);Debug.LogWarning("[Cold297Focus] "+why);}
  static void End(ScriptableRenderContext c,Camera camera){if(camera==Stage.Camera && requested==Time.frameCount && ExecutedFrame!=Time.frameCount)Fail("dedicated pass not executed; restoring forward fallback");}
  static void Begin(ScriptableRenderContext c,Camera camera){
   if(camera!=Stage.Camera)return;
   var settings=GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
   bool usable=Enabled&&!faulted&&Stage.CombatWorld&&!(settings?.enableRenderCompatibilityMode??false)&&camera.TryGetComponent<UniversalAdditionalCameraData>(out var data)&&data.scriptableRenderer is UniversalRenderer;
   BoundedContactFx.SetColdForward(!usable);if(!usable)return;
   BoundedContactFx.CollectColdRenderers(active);if(active.Count==0)return;
   if(pass==null)pass=new ColdSweepFocusPass();requested=Time.frameCount;camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(pass);
  }
  public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame){
   BoundedContactFx.CollectColdRenderers(active);if(active.Count==0){ExecutedFrame=Time.frameCount;return;}
   var r=frame.Get<UniversalResourceData>();
   if(!r.activeColorTexture.IsValid()||!r.activeDepthTexture.IsValid()){Fail("missing color/depth");return;}
   if(graph.GetTextureDesc(r.activeColorTexture).msaaSamples!=graph.GetTextureDesc(r.activeDepthTexture).msaaSamples){Fail("color/depth sample mismatch");return;}
   using(var b=graph.AddRasterRenderPass<Data>("Cold297 after tilt, before tone",out var d)){
    d.Renderers.Clear();d.Renderers.AddRange(active);b.SetRenderAttachment(r.activeColorTexture,0,AccessFlags.ReadWrite);b.SetRenderAttachmentDepth(r.activeDepthTexture,AccessFlags.Read);b.AllowPassCulling(false);
    b.SetRenderFunc((Data p,RasterGraphContext ctx)=>{foreach(var renderer in p.Renderers)if(renderer!=null && renderer.enabled && renderer.gameObject.activeInHierarchy)ctx.cmd.DrawRenderer(renderer,renderer.sharedMaterial,0,0);ExecutedFrame=Time.frameCount;ExecutedDraws=p.Renderers.Count;});
   }
  }
 }
}
