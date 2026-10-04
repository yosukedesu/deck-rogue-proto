using UnityEditor;
namespace DeckRogue.EditorTools {
 public static class TreeAndroidTextureSettings {
  public static void Apply(TextureImporter importer, BuildTarget target) {
   // Unity 6000.6 imported the NPOT 341x512 mip chain as RGBA32 even with
   // explicit ASTC. POT resampling is Android-only; preserve desktop source size.
   importer.npotScale=target==BuildTarget.Android?TextureImporterNPOTScale.ToNearest:TextureImporterNPOTScale.None;
   var android=importer.GetPlatformTextureSettings("Android");
   android.overridden=true;android.maxTextureSize=512;
   android.format=TextureImporterFormat.ASTC_6x6;android.textureCompression=TextureImporterCompression.Compressed;
   android.compressionQuality=100;android.ignorePlatformSupport=false;
   importer.SetPlatformTextureSettings(android);
  }
 }
}
