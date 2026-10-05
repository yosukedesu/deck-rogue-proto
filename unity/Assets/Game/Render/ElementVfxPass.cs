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
  public static int DrawnFrames {get;private set;}
  public ElementVfxPass(){renderPassEvent=RenderPassEvent.AfterRenderingPostProcessing;requiresIntermediateTexture=true;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Install(){RenderPipelineManager.beginCameraRendering-=Begin;RenderPipelineManager.beginCameraRendering+=Begin;DrawnFrames=0;}
  static void Begin(ScriptableRenderContext context,Camera camera){
   if(!ElementBattleFx.SeparateMaterial || camera==null || camera!=Stage.Camera || !Stage.CombatWorld)return;
   if(camera.TryGetComponent<UniversalAdditionalCameraData>(out var data)){if(pass==null)pass=new ElementVfxPass();WorldBattleFx.CollectElementRenderers(pass.active);if(pass.active.Count>0)data.scriptableRenderer.EnqueuePass(pass);}
  }
  public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame){
   WorldBattleFx.CollectElementRenderers(active);if(active.Count==0)return;
   var resources=frame.Get<UniversalResourceData>();
   using(var builder=graph.AddRasterRenderPass<Data>("Element VFX after stage bloom",out var data)){
    data.Renderers.Clear();data.Renderers.AddRange(active);
    builder.SetRenderAttachment(resources.activeColorTexture,0,AccessFlags.ReadWrite);
    builder.SetRenderAttachmentDepth(resources.activeDepthTexture,AccessFlags.Read);
    builder.AllowPassCulling(false);
    builder.SetRenderFunc((Data d,RasterGraphContext ctx)=>{
     foreach(var renderer in d.Renderers)if(renderer!=null)ctx.cmd.DrawRenderer(renderer,renderer.sharedMaterial,0,0);
     DrawnFrames++;
    });
   }
  }
 }
}
