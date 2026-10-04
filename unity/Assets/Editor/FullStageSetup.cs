using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace DeckRogue.EditorTools {
 public class FullStageImporter : AssetPostprocessor {
  void OnPreprocessModel(){if(!assetPath.Contains("/FullStage/"))return;var i=(ModelImporter)assetImporter;i.isReadable=true;i.importAnimation=false;i.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;}
  void OnPreprocessTexture(){if(!assetPath.Contains("/FullStage/"))return;var i=(TextureImporter)assetImporter;i.filterMode=FilterMode.Bilinear;i.mipmapEnabled=true;i.alphaIsTransparency=true;i.sRGBTexture=true;i.textureCompression=TextureImporterCompression.Uncompressed;}
 }
 public static class FullStageSetup {
  const string Root="Assets/Resources/FullStage";
  public static int Inspect(){
   var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Editor/FullStage/mossbound_full_stage.fbx");
   if(source==null)throw new Exception("Missing full stage FBX");
   foreach(var t in source.GetComponentsInChildren<Transform>(true)) if(t.name.Contains("FOOT")||t.name.Contains("WORLD_")||t.name.Contains("SLOT"))Debug.Log("[FullStage] MARK "+t.name+" "+t.position.ToString("F5"));
   foreach(var r in source.GetComponentsInChildren<MeshRenderer>(true))Debug.Log("[FullStage] MESH "+r.name+" mats="+string.Join("|",r.sharedMaterials.Select(m=>m==null?"NULL":m.name))+" bounds="+r.bounds);
   return 0;
  }
  static void Save(UnityEngine.Object o,string p){var old=AssetDatabase.LoadMainAssetAtPath(p);if(old==null)AssetDatabase.CreateAsset(o,p);else{EditorUtility.CopySerialized(o,old);UnityEngine.Object.DestroyImmediate(o);}}
  public static int Run(){
   var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Editor/FullStage/mossbound_full_stage.fbx");
   if(source==null)throw new Exception("Missing full stage FBX");
   var ts=source.GetComponentsInChildren<Transform>(true);
   Func<string,Vector3> marker=n=>ts.Single(t=>t.name==n).position;
   var basis=Matrix4x4.identity;
   basis.SetColumn(0,new Vector4(marker("WORLD_RIGHT_1M").x,marker("WORLD_RIGHT_1M").y,marker("WORLD_RIGHT_1M").z,0));
   basis.SetColumn(1,new Vector4(marker("WORLD_UP_1M").x,marker("WORLD_UP_1M").y,marker("WORLD_UP_1M").z,0));
   basis.SetColumn(2,new Vector4(marker("WORLD_FORWARD_1M").x,marker("WORLD_FORWARD_1M").y,marker("WORLD_FORWARD_1M").z,0));
   if(Mathf.Abs(Mathf.Abs(basis.determinant)-1)>.001f)throw new Exception("Non-unit export basis");
   var correction=basis.inverse;
   var player=correction.MultiplyPoint3x4(marker("PLAYER_FOOT"));
   var enemy=correction.MultiplyPoint3x4(marker("ENEMY_1_FOOT"));
   if(Vector3.Distance(player,new Vector3(-4.973f,0,-1.039f))>.002f||Vector3.Distance(enemy,new Vector3(4.322f,0,1.207f))>.002f)throw new Exception("Foot marker mapping failed");
   Debug.Log("[FullStage] AXIS PASS basis="+basis+" player="+player.ToString("F5")+" enemy="+enemy.ToString("F5"));
   var textures=new Dictionary<string,string>{{"Moss pixel limestone","forest_moss_256"},{"Weathered grey limestone","broken_paving_256"},{"earth_128","forest_earth_256"},{"moss_turf_128","forest_moss_256"},{"cliff_strata_128","stratified_cliff_256"},{"leaf_2_128","leaf_2_128"},{"bronze_128","bronze_128"},{"TreeCutout_0","tree_cutout_0"},{"TreeCutout_1","tree_cutout_1"},{"TreeCutout_2","tree_cutout_2"},{"TreeCutout_3","tree_cutout_3"}};
   var mats=new Dictionary<string,Material>();var root=new GameObject("Mossbound full stage v2");int triangles=0;
   foreach(var mf in source.GetComponentsInChildren<MeshFilter>(true)){
    var imported=mf.GetComponent<MeshRenderer>().sharedMaterials;var output=new Material[imported.Length];
    for(int i=0;i<imported.Length;i++){
     var name=imported[i].name;
     if(!mats.TryGetValue(name,out var mat)){
      bool alpha=name.StartsWith("TreeCutout_")||name=="leaf_2_128";
      mat=new Material(Shader.Find(name=="lantern_glass_emission"?"Universal Render Pipeline/Unlit":"DeckRogue/StageModule")){name=name};
      if(textures.TryGetValue(name,out var tex))mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+tex+".png"));
      mat.SetColor("_BaseColor",name=="lantern_glass_emission"?new Color(1.8f,.7f,.16f,1):name=="Still forest water"?new Color(.07f,.22f,.22f,1):Color.white);
      if(name!="lantern_glass_emission"){
       mat.EnableKeyword("_UV_MESH");mat.SetFloat("_MeshArraySlice",-1);mat.SetFloat("_NormalArrayOn",0);mat.SetFloat("_NormalAtlasOn",0);mat.SetFloat("_VColorAO",0);mat.SetFloat("_HitReceive",alpha?1.5f:2f);mat.SetFloat("_Receive",alpha?.55f:.8f);
       if(alpha){mat.EnableKeyword("_ALPHATEST_ON");mat.SetFloat("_Cutoff",.4f);mat.SetFloat("_Cull",0);mat.renderQueue=2450;}
      }
      string path=Root+"/Surface_"+mats.Count+".mat";Save(mat,path);mat=AssetDatabase.LoadAssetAtPath<Material>(path);mats.Add(name,mat);
     }
     output[i]=mat;
    }
    var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);mesh.name=mf.name;
    var transform=correction*mf.transform.localToWorldMatrix;var vs=mesh.vertices;var ns=mesh.normals;
    for(int i=0;i<vs.Length;i++)vs[i]=transform.MultiplyPoint3x4(vs[i]);
    var nt=transform.inverse.transpose;for(int i=0;i<ns.Length;i++)ns[i]=nt.MultiplyVector(ns[i]).normalized;
    mesh.vertices=vs;mesh.normals=ns;
    if(transform.determinant<0)for(int sm=0;sm<mesh.subMeshCount;sm++){var ids=mesh.GetTriangles(sm);for(int i=0;i<ids.Length;i+=3){var a=ids[i];ids[i]=ids[i+2];ids[i+2]=a;}mesh.SetTriangles(ids,sm);}
    // The authored open cliff sheet faces inward (Blender preview is double-sided).
    // Repair that sheet explicitly, keeping opaque terrain single-sided elsewhere.
    if(mf.name=="StageBatch_13") {
     for(int sm=0;sm<mesh.subMeshCount;sm++){var ids=mesh.GetTriangles(sm);for(int i=0;i<ids.Length;i+=3){int a=ids[i];ids[i]=ids[i+2];ids[i+2]=a;}mesh.SetTriangles(ids,sm);}
     var normals=mesh.normals;for(int i=0;i<normals.Length;i++)normals[i]=-normals[i];mesh.normals=normals;
     Debug.Log("[FullStage] repaired inward-facing open front cliff");
    }
    // Joined Blender meshes can have blank UV0 on newly conformed inset stones.
    // Repair only degenerate textured faces, retaining original UVs everywhere else.
    var uv=mesh.uv; var originalUv=(Vector2[])uv.Clone(); int repaired=0;
    for(int sm=0;sm<mesh.subMeshCount;sm++) {
     string surface=output[sm].name;
     if(surface.Contains("TreeCutout")||surface.Contains("leaf")||surface.Contains("lantern")||surface.Contains("water"))continue;
     var ids=mesh.GetTriangles(sm);
     for(int i=0;i<ids.Length;i+=3) {
      int a=ids[i],b=ids[i+1],c=ids[i+2];var u=originalUv[b]-originalUv[a];var v=originalUv[c]-originalUv[a];
      if(Mathf.Abs(u.x*v.y-u.y*v.x)>1e-9f)continue;
      var n=Vector3.Cross(vs[b]-vs[a],vs[c]-vs[a]);if(n.sqrMagnitude<1e-10f)continue;
      foreach(int q in new[]{a,b,c})uv[q]=Mathf.Abs(n.y)>Mathf.Max(Mathf.Abs(n.x),Mathf.Abs(n.z))?new Vector2(vs[q].x,vs[q].z)*.125f:Mathf.Abs(n.z)>Mathf.Abs(n.x)?new Vector2(vs[q].x,vs[q].y)*.125f:new Vector2(vs[q].z,vs[q].y)*.125f;
      repaired++;
     }
    }
    mesh.uv=uv;if(repaired>0)Debug.Log("[FullStage] repaired degenerate UV faces "+mf.name+" count="+repaired);
    mesh.RecalculateBounds();triangles+=mesh.triangles.Length/3;
    var pathMesh=Root+"/"+mf.name+".asset";Save(mesh,pathMesh);
    var go=new GameObject(mf.name);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(pathMesh);go.AddComponent<MeshRenderer>().sharedMaterials=output;
   }
   PrefabUtility.SaveAsPrefabAsset(root,Root+"/ForestStage.prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
   Debug.Log("[FullStage] BUILD PASS triangles="+triangles+" materials="+mats.Count+"; cutouts use common StageModule forward/depth/shadow alpha");return 0;
  }
 }
}
