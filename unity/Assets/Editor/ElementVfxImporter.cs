using UnityEditor;
using UnityEngine;
namespace DeckRogue.EditorTools {
 // Atlas cells are authored with transparent gutters. Never auto-trim or rescale individual cells.
 public class ElementVfxImporter:AssetPostprocessor {
  void OnPreprocessTexture(){
   if((!assetPath.StartsWith("Assets/Resources/Art/fx/element-") && !assetPath.StartsWith("Assets/Resources/Art/fx/attack-")) || !assetPath.EndsWith("-8f.png"))return;
   var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.alphaSource=TextureImporterAlphaSource.FromInput;
   t.alphaIsTransparency=true;t.mipmapEnabled=false;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.npotScale=TextureImporterNPOTScale.None;
   t.maxTextureSize=1024;t.textureCompression=TextureImporterCompression.Uncompressed;
   // V2 initial QA keeps tiny drops/ridges uncompressed. Device compression requires visual review.
   if(assetPath.StartsWith("Assets/Resources/Art/fx/attack-"))t.ClearPlatformTextureSettings("Android");
   else t.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6,compressionQuality=100});
  }
 }
}
