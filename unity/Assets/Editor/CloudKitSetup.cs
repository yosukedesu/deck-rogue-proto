using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace DeckRogue.EditorTools {
 public class CloudKitImporter : AssetPostprocessor {
  void OnPreprocessModel() { if(!assetPath.Contains("/CloudKit/"))return; var i=(ModelImporter)assetImporter;i.isReadable=true;i.importAnimation=false;i.materialImportMode=ModelImporterMaterialImportMode.None; }
  void OnPreprocessTexture() {if(!assetPath.Contains("/CloudKit/"))return;var i=(TextureImporter)assetImporter;i.textureType=TextureImporterType.Default;i.filterMode=FilterMode.Point;i.mipmapEnabled=true;i.sRGBTexture=true;i.textureCompression=TextureImporterCompression.Uncompressed;i.isReadable=false;}
 }
 public static class CloudKitSetup {
  const string Root="Assets/Resources/CloudKit";
  public static void Run() {
   try {
    var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/mossbound_modular_kit.fbx");
    if(source==null)throw new Exception("FBX missing");
    var shader=Shader.Find("DeckRogue/StageModule");
    var mat=new Material(shader);mat.name="Cloud limestone";mat.EnableKeyword("_UV_MESH");mat.SetFloat("_MeshArraySlice",-1);mat.SetFloat("_NormalArrayOn",0);mat.SetFloat("_NormalAtlasOn",0);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/moss_stone_pixel_128.png"));
    mat.SetColor("_BaseColor",new Color(.8f,.85f,.85f,1));
    Save(mat,Root+"/CloudLimestone.mat");mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/CloudLimestone.mat");
    var stone=new Material(mat);stone.name="Cloud weathered stone";stone.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/stone_pixel_128.png"));Save(stone,Root+"/CloudStone.mat");stone=AssetDatabase.LoadAssetAtPath<Material>(Root+"/CloudStone.mat");
    var root=new GameObject("Cloud masonry");
    Build(source,root,"Gate",Vector3.zero,new Vector3(-3.3f,0,8.4f),.84f,mat);
    Build(source,root,"Pillar",new Vector3(4.7f,0,0),new Vector3(2.8f,0,9.8f),1.15f,stone);
    Build(source,root,"Stair",new Vector3(-4.9f,0,0),new Vector3(-4.2f,-.08f,5.7f),1.2f,stone);
    Build(source,root,"Rock",new Vector3(4.65f,0,2.65f),new Vector3(5.8f,-.06f,5.9f),1.3f,mat);
    PrefabUtility.SaveAsPrefabAsset(root,Root+"/ForestMasonry.prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
    Debug.Log("[CloudKit] PASS: imported modules with mesh UV, URP lighting/depth/shadow/fog, two shared materials");
   }catch(Exception e){Debug.LogError(e);EditorApplication.Exit(1);return;}EditorApplication.Exit(0);
  }
  static void Save(UnityEngine.Object obj,string path){var old=AssetDatabase.LoadMainAssetAtPath(path);if(old==null)AssetDatabase.CreateAsset(obj,path);else {EditorUtility.CopySerialized(obj,old);UnityEngine.Object.DestroyImmediate(obj);}}
  static void Build(GameObject source,GameObject root,string prefix,Vector3 origin,Vector3 position,float scale,Material mat){
   var instances=new List<CombineInstance>();
   foreach(var mf in source.GetComponentsInChildren<MeshFilter>(true))if(mf.name.StartsWith(prefix,StringComparison.Ordinal)){
    instances.Add(new CombineInstance{mesh=mf.sharedMesh,transform=Matrix4x4.Translate(-origin)*source.transform.worldToLocalMatrix*mf.transform.localToWorldMatrix});
   }
   if(instances.Count==0)throw new Exception("No module "+prefix);
   var mesh=new Mesh{name="Cloud "+prefix};mesh.CombineMeshes(instances.ToArray(),true,true);mesh.RecalculateBounds();
   // FBX coordinate conversion may mirror source X; recenter from imported bounds.
   var center=mesh.bounds.center;var vertices=mesh.vertices;for(int n=0;n<vertices.Length;n++)vertices[n]-=new Vector3(center.x,0,center.z);mesh.vertices=vertices;mesh.RecalculateBounds();
   Save(mesh,Root+"/"+prefix+".asset");
   var go=new GameObject(prefix);go.transform.SetParent(root.transform,false);go.transform.localPosition=position;if(prefix=="Stair")go.transform.localRotation=Quaternion.Euler(0,180,0);go.transform.localScale=Vector3.one*scale;go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/"+prefix+".asset");go.AddComponent<MeshRenderer>().sharedMaterial=mat;
   Debug.Log("[CloudKit] "+prefix+" meshes="+instances.Count+" bounds="+go.GetComponent<MeshFilter>().sharedMesh.bounds);
  }
 }
}
