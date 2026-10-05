using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace DeckRogue.EditorTools {
 public static class VfxBuildAudit {
  public static void BuildAndroidCandidate(){
   var textures=new JArray();
   foreach(var element in new[]{"fire","water","ice","lightning","dark","light","heal","vine","earth"}){
    string path="Assets/Resources/Art/fx/element-"+element+"-8f.png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);var settings=importer.GetPlatformTextureSettings("Android");
    textures.Add(new JObject{{"element",element},{"override",settings.overridden},{"format",settings.format.ToString()},{"maxSize",settings.maxTextureSize},{"sRGB",importer.sRGBTexture},{"mipmap",importer.mipmapEnabled},{"wrap",importer.wrapMode.ToString()},{"filter",importer.filterMode.ToString()}});
   }
   var pipelines=new JArray();
   foreach(var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset")){
    string path=AssetDatabase.GUIDToAssetPath(guid);var asset=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);if(asset==null)continue;
    var so=new SerializedObject(asset);var list=so.FindProperty("m_RendererDataList");var renderers=new JArray();
    if(list!=null)for(int i=0;i<list.arraySize;i++){var data=list.GetArrayElementAtIndex(i).objectReferenceValue;renderers.Add(new JObject{{"name",data!=null?data.name:"missing"},{"type",data!=null?data.GetType().Name:"missing"},{"path",data!=null?AssetDatabase.GetAssetPath(data):""}});}
    pipelines.Add(new JObject{{"path",path},{"name",asset.name},{"depthTexture",asset.supportsCameraDepthTexture},{"msaaSamples",asset.msaaSampleCount},{"renderScale",asset.renderScale},{"renderers",renderers}});
   }
   var rg=GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
   var result=new JObject{{"activeTarget",EditorUserBuildSettings.activeBuildTarget.ToString()},{"renderGraphCompatibilityMode",rg!=null && rg.enableRenderCompatibilityMode},{"allowHDRDisplaySupport",PlayerSettings.allowHDRDisplaySupport},{"textures",textures},{"pipelines",pipelines},{"postShader",AssetDatabase.GetAssetPath(Shader.Find("DeckRogue/ElementBattleFx"))},{"legacyShader",AssetDatabase.GetAssetPath(Shader.Find("DeckRogue/WorldBattleFx"))},{"deviceExecuted",false}};
   Directory.CreateDirectory("Build");File.WriteAllText("Build/vfx-android-settings.json",result.ToString());
   BuildTools.BuildAndroid();
  }
 }
}
