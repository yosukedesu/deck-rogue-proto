using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
namespace DeckRogue.EditorTools {
 public static class TreeTextureStudy {
  public static int Run(){
   const string input="Assets/Resources/FullStageTrees/tree_midground_v1.png";
   const string temp="Assets/Editor/TreeTextureStudy/source.png";
   Directory.CreateDirectory(Path.GetDirectoryName(temp));File.Copy(input,temp,true);AssetDatabase.Refresh();
   string output=Path.GetFullPath("TextureStudy");Directory.CreateDirectory(output);var report=new JArray();
   var ids=new[]{"reference","1024_astc6","512_astc6","512_astc8","256_astc6","512_etc2"};
   var sizes=new[]{2048,1024,512,512,256,512};
   try {for(int j=0;j<ids.Length;j++) {
    var ti=(TextureImporter)AssetImporter.GetAtPath(temp);ti.textureType=TextureImporterType.Default;ti.isReadable=true;ti.mipmapEnabled=true;ti.sRGBTexture=true;ti.alphaIsTransparency=true;ti.alphaSource=TextureImporterAlphaSource.FromInput;ti.npotScale=j==5?TextureImporterNPOTScale.ToNearest:TextureImporterNPOTScale.None;ti.wrapMode=TextureWrapMode.Clamp;ti.filterMode=FilterMode.Bilinear;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=sizes[j];ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Standalone",overridden=true,maxTextureSize=sizes[j],format=TextureImporterFormat.RGBA32});ti.SaveAndReimport();
    var t=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<Texture2D>(temp));var fmt=j==0?TextureFormat.RGBA32:j==5?TextureFormat.ETC2_RGBA8:j==3?TextureFormat.ASTC_8x8:TextureFormat.ASTC_6x6;
    if(j>0)EditorUtility.CompressTexture(t,fmt,TextureCompressionQuality.Best);
    if(t.format!=fmt)throw new Exception("Compression failed "+ids[j]+" actual="+t.format);
    string dir=Path.Combine(output,ids[j]);Directory.CreateDirectory(dir);long bytes=0;var levels=new JArray();
    for(int mip=0;mip<t.mipmapCount;mip++) {
     int width=Math.Max(1,t.width>>mip),height=Math.Max(1,t.height>>mip);var raw=t.GetPixelData<byte>(mip).ToArray();bytes+=raw.Length;
     if(j==0){var level=new Texture2D(width,height,TextureFormat.RGBA32,false,false);level.SetPixels32(t.GetPixels32(mip));level.Apply(false);File.WriteAllBytes(Path.Combine(dir,mip+".png"),level.EncodeToPNG());UnityEngine.Object.DestroyImmediate(level);}
     else if(j==5)File.WriteAllBytes(Path.Combine(dir,mip+".etc2"),raw);
     else {int block=j==3?8:6;using(var writer=new BinaryWriter(File.Create(Path.Combine(dir,mip+".astc")))){writer.Write(0x5CA1AB13u);writer.Write((byte)block);writer.Write((byte)block);writer.Write((byte)1);foreach(int value in new[]{width,height,1}){writer.Write((byte)value);writer.Write((byte)(value>>8));writer.Write((byte)(value>>16));}writer.Write(raw);}}
     levels.Add(new JObject{{"mip",mip},{"width",width},{"height",height},{"bytes",raw.Length}});
    }
    var record=new JObject{{"id",ids[j]},{"format",t.format.ToString()},{"width",t.width},{"height",t.height},{"mips",t.mipmapCount},{"bytes",bytes},{"levels",levels}};report.Add(record);File.WriteAllText(Path.Combine(dir,"manifest.json"),record.ToString());Debug.Log("[TextureStudy] "+ids[j]+" "+t.width+"x"+t.height+" "+t.format+" mipBytes="+bytes);UnityEngine.Object.DestroyImmediate(t);
   }}finally{AssetDatabase.DeleteAsset("Assets/Editor/TreeTextureStudy");}
   File.WriteAllText(Path.Combine(output,"report.json"),report.ToString());return 0;
  }
 }
}
