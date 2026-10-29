using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace DeckRogue.Game {
// Explicit QA-only constant-UV draw. No live material, light, clock or game state is modified.
internal static class CanopyGpuProbe {
 static JArray RGBA(Color c)=>new JArray(c.r,c.g,c.b,c.a);
 internal static void Run(string directory,Texture bound,Shader production,bool encodedCandidate=false){
  if(bound==null||production==null||Stage.Camera==null)throw new InvalidOperationException("Missing actual Canopy binding");
  if(!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))throw new NotSupportedException("RGBA32F render target unavailable");
  Directory.CreateDirectory(directory);
  var diagnostic=Resources.Load<Shader>("Shaders/CanopyNumericProbe");
  if(diagnostic==null||!diagnostic.isSupported)throw new NotSupportedException("Canopy numeric shader unavailable");
  var material=new Material(production);var probe=new Material(diagnostic);var mesh=new Mesh();
  var rt=new RenderTexture(64,64,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear){antiAliasing=1,useMipMap=false,autoGenerateMips=false};rt.Create();
  var read=new Texture2D(64,64,TextureFormat.RGBAFloat,false,true);var cam=Stage.Camera;
  var previous=RenderTexture.active;var rows=new JArray();
  try{
   var xy=new[]{new Vector3(0,0,10),new Vector3(1,0,10),new Vector3(1,1,10),new Vector3(0,1,10)};
   mesh.vertices=Array.ConvertAll(xy,p=>cam.ViewportToWorldPoint(p));mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
   foreach(var m in new[]{material,probe}){m.SetFloat("_SourceEncoded",encodedCandidate?1:0);m.SetTexture("_BaseMap",bound);m.SetFloat("_Age",.0333333f);m.SetFloat("_ReviewSolid",0);m.SetFloat("_ReviewZTest",8);}
   Func<string,Material,Color,JObject> draw=(name,mat,background)=>{
    var cb=new CommandBuffer{name="QA Canopy constant UV "+name};
    try{cb.SetRenderTarget(rt);cb.SetViewport(new Rect(0,0,64,64));cb.ClearRenderTarget(false,true,background);cb.SetViewProjectionMatrices(cam.worldToCameraMatrix,GL.GetGPUProjectionMatrix(cam.projectionMatrix,true));cb.DrawMesh(mesh,Matrix4x4.identity,mat,0,0);Graphics.ExecuteCommandBuffer(cb);}finally{cb.Release();}
    RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,64,64),0,0);read.Apply();
    // Unity ReadPixels uses lower-left coordinates. Top-origin (32,32) maps to (32,31).
    var c=read.GetPixel(32,31);File.WriteAllBytes(Path.Combine(directory,name+".exr"),read.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));
    var bytes=read.GetRawTextureData<byte>().ToArray();File.WriteAllBytes(Path.Combine(directory,name+".rgba32f"),bytes);
    return new JObject{["name"]=name,["rgba"]=RGBA(c),["background"]=RGBA(background),["rawOrder"]="row-major lower-left, RGBA float32 native little-endian",["topOriginPixel"]=new JArray(32,32)};
   };
   foreach(float alpha in new[]{1f,.5f})foreach(float v in new[]{.25f,.5f,.75f})foreach(float u in new[]{.25f,.5f,.75f}){
    mesh.uv=new[]{new Vector2(u,v),new Vector2(u,v),new Vector2(u,v),new Vector2(u,v)};mesh.uv2=new[]{new Vector2(.72f,1),new Vector2(.72f,1),new Vector2(.72f,1),new Vector2(.72f,1)};
    var color=new Color(1,1,1,alpha);mesh.colors=new[]{color,color,color,color};
    var name="a"+(alpha==1?"1":"05")+"-u"+(int)(u*100)+"-v"+(int)(v*100);var passes=new JArray();
    foreach(int mode in new[]{0,1,2,3}){probe.SetFloat("_Mode",mode);passes.Add(draw(name+"-intermediate"+mode,probe,Color.clear));}
    passes.Add(draw(name+"-black",material,new Color(0,0,0,1)));passes.Add(draw(name+"-gray",material,new Color(.18f,.18f,.18f,1)));passes.Add(draw(name+"-transparent",material,Color.clear));
    rows.Add(new JObject{["uv"]=new JArray(u,v),["textureUv"]=new JArray(.18f+.64f*u,1-(.22f+.56f*v)),["vertexAlpha"]=alpha,["gain"]=.72f,["age"]=.0333333f,["passes"]=passes});
   }
   var calibration=new JArray();probe.SetFloat("_Mode",4);
   foreach(float c in new[]{0f,.18f,.5f,1f}){probe.SetFloat("_Constant",c);calibration.Add(draw("calibration-"+calibration.Count,probe,Color.clear));}
   var report=new JObject{["sourceEncodedCandidate"]=encodedCandidate,["backend"]=SystemInfo.graphicsDeviceType.ToString(),["device"]=SystemInfo.graphicsDeviceName,["deviceVersion"]=SystemInfo.graphicsDeviceVersion,["activeColorSpace"]=QualitySettings.activeColorSpace.ToString(),["rtGraphicsFormat"]=rt.graphicsFormat.ToString(),["rtSRGB"]=rt.sRGB,["readbackFormat"]=read.graphicsFormat.ToString(),["texture"]=bound.name,["textureFormat"]=bound.graphicsFormat.ToString(),["filter"]=bound.filterMode.ToString(),["wrap"]=bound.wrapMode.ToString(),["mipmapCount"]=(bound as Texture2D)?.mipmapCount,["productionShader"]=production.name,["diagnosticShader"]=diagnostic.name,["unityOutputConversion"]="No output_rgb shader branch; linear float RT, no post. GPU source sRGB sampling active via bound format.",["intermediates"]="0 sampled organic RGBA; 1 gainRGB/finalAlpha; 2 Unity outputRGB/finalAlpha (same expression); 3 edge/consume/remaining/finalAlpha. Blend Off.",["finalBlend"]="Original material: Blend One OneMinusSrcAlpha for RGB and alpha, original fragment output premultiplied RGB.",["calibration"]=calibration,["samples"]=rows};
   File.WriteAllText(Path.Combine(directory,"measurements.json"),report.ToString());
  }finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(read);UnityEngine.Object.Destroy(mesh);UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(probe);}
 }
}
}
