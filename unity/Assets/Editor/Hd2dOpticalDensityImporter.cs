using UnityEditor;
namespace DeckRogue.EditorTools {
 sealed class Hd2dOpticalDensityImporter:AssetPostprocessor {
  void OnPreprocessTexture() {
   if(assetPath!="Assets/Resources/Art/fx/crossbow-pressure-density-v1.png" && assetPath!="Assets/Resources/Art/fx/wraith-pressure-density-v1.png" && assetPath!="Assets/Resources/Art/fx/light-bolt-pressure-density-v1.png" && assetPath!="Assets/Resources/Art/fx/cold-frost-density-v1.png" && assetPath!="Assets/Resources/Art/fx/hd2d-fire-volume-powder-density-v1.png" && assetPath!="Assets/Resources/Art/fx/canopy-organic-density-v1.png")return;
   var t=(TextureImporter)assetImporter;
   t.textureType=TextureImporterType.Default;t.sRGBTexture=true;
   t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;
   t.textureCompression=TextureImporterCompression.Uncompressed;
   t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=false;
   t.filterMode=UnityEngine.FilterMode.Bilinear;t.wrapMode=UnityEngine.TextureWrapMode.Clamp;
   t.maxTextureSize=2048;
  }
 }
}
