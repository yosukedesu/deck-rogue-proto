using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
namespace DeckRogue.EditorTools {
 public class FullStageImporter : AssetPostprocessor {
  void OnPreprocessModel(){if(!assetPath.Contains("/FullStage/"))return;var i=(ModelImporter)assetImporter;i.isReadable=true;i.importAnimation=false;i.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;}
  void OnPreprocessTexture(){if(!assetPath.Contains("/FullStage/")&&!assetPath.Contains("/FullStageTrees/"))return;var i=(TextureImporter)assetImporter;i.filterMode=FilterMode.Bilinear;i.mipmapEnabled=true;i.alphaIsTransparency=true;i.sRGBTexture=true;i.textureCompression=TextureImporterCompression.Uncompressed;if(assetPath.Contains("/FullStageTrees/")){i.npotScale=TextureImporterNPOTScale.None;i.wrapMode=TextureWrapMode.Clamp;if(assetPath.EndsWith("/tree_midground_v1.png"))TreeAndroidTextureSettings.Apply(i,context.selectedBuildTarget);}}
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
  static void Save(UnityEngine.Object o,string p){
   // CreateAsset can replace a material's semantic name with the filename.
   // Runtime night grading and lantern creation rely on the semantic label.
   string label=o.name;var old=AssetDatabase.LoadMainAssetAtPath(p);
   if(old==null){AssetDatabase.CreateAsset(o,p);o.name=label;EditorUtility.SetDirty(o);}
   else{EditorUtility.CopySerialized(o,old);old.name=label;EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(o);}
  }
  public static int Run() => RunAt("Assets/Editor/FullStage", Root);
  public static int CohesionStudy() => RunAt("Assets/Editor/FullStage/cohesion", "Assets/Resources/FullStageCohesion");
  public static int TreeStudy() => RunAt("Assets/Editor/FullStage/trees", "Assets/Resources/FullStageTrees");
  public static int NaturalBanksStudy() => RunAt("Assets/Editor/FullStage/banks", "Assets/Resources/FullStageBanks");
  public static int IntegratedForest() => RunAt("Assets/Editor/FullStage/integrated", "Assets/Resources/FullStageIntegrated");
  static int RunAt(string sourceRoot, string outputRoot){
   string Root=outputRoot;
   const string textureRoot="Assets/Resources/FullStage";
   System.IO.Directory.CreateDirectory(outputRoot);AssetDatabase.Refresh();
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourceRoot+"/mossbound_full_stage.fbx");
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
   var textures=new Dictionary<string,string>{{"Moss pixel limestone","moss_soft_v5"},{"Weathered grey limestone","paving_soft_v5"},{"earth_128","soil_soft_v5"},{"moss_turf_128","moss_soft_v5"},{"cliff_strata_128","rock_soft_v5"},{"leaf_2_128","leaf_2_128"},{"bronze_128","bronze_128"},{"TreeCutout_0","tree_cutout_0"},{"TreeCutout_1","tree_cutout_1"},{"TreeCutout_2","tree_cutout_2"},{"TreeCutout_3","tree_cutout_3"},{"FoliageCutout_FernAtlas","fern_cutout_atlas"}};
   if(outputRoot.EndsWith("Trees")||outputRoot.EndsWith("Banks")||outputRoot.EndsWith("Integrated"))textures.Add("TreeCutout_Midground_Study","tree_midground_v1");
   Func<string,string> texturePath=tex=>(tex=="tree_midground_v1"?"Assets/Resources/FullStageTrees":textureRoot)+"/"+tex+".png";
   var sourceMap=JArray.Parse(System.IO.File.ReadAllText(sourceRoot+"/batch_source_map.json"));
   var frontEntry=sourceMap.FirstOrDefault(b=>((JArray)b["sources"]).Any(n=>(string)n=="Front_Cliff_Strata"));
   var frontBatch=(string)frontEntry?["mesh"];
   var mats=new Dictionary<string,Material>();var root=new GameObject(outputRoot.EndsWith("Integrated")?"Mossbound integrated forest":outputRoot.EndsWith("Banks")?"Mossbound natural banks integrated candidate":outputRoot.EndsWith("Cohesion")?"Mossbound cohesion study":outputRoot.EndsWith("Trees")?"Mossbound tree study":"Mossbound full stage v5");int triangles=0;
   foreach(var mf in source.GetComponentsInChildren<MeshFilter>(true)){
    var imported=mf.GetComponent<MeshRenderer>().sharedMaterials;var output=new Material[imported.Length];
    for(int i=0;i<imported.Length;i++){
     var name=imported[i].name;
     if(!mats.TryGetValue(name,out var mat)){
      bool alpha=name.StartsWith("TreeCutout_")||name.StartsWith("FoliageCutout_")||name=="leaf_2_128";
      mat=new Material(Shader.Find(name=="lantern_glass_emission"?"Universal Render Pipeline/Unlit":"DeckRogue/StageModule")){name=name};
      if(textures.TryGetValue(name,out var tex))mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath(tex)));
      mat.SetColor("_BaseColor",name=="lantern_glass_emission"?new Color(1.8f,.7f,.16f,1):name=="Still forest water"?new Color(.07f,.22f,.22f,1):Color.white);
      if(name!="lantern_glass_emission"){
       mat.EnableKeyword("_UV_MESH");mat.SetFloat("_MeshArraySlice",-1);mat.SetFloat("_NormalArrayOn",0);mat.SetFloat("_NormalAtlasOn",0);mat.SetFloat("_VColorAO",0);mat.SetFloat("_HitReceive",alpha?1.5f:2f);mat.SetFloat("_Receive",alpha?.55f:.8f);
       mat.SetFloat("_NaturalSurface",1);mat.SetFloat("_StaticNeutral",.78f);mat.SetFloat("_GradeWeight",.12f);mat.SetFloat("_VignetteWeight",.4f);
       mat.SetVector("_AmbientFill",alpha?new Vector4(.32f,.34f,.28f,0):new Vector4(.17f,.18f,.16f,0));
       mat.SetFloat("_ShadowStrength",alpha?.65f:.85f);mat.SetFloat("_TwoSidedNormals",alpha?1:0);
       mat.SetFloat("_TextureContrast",alpha?.94f:1f);
       if(textures.TryGetValue(name,out var colorTexture)) {
        var readable=new Texture2D(2,2);readable.LoadImage(System.IO.File.ReadAllBytes(texturePath(colorTexture)));
        Vector3 sum=Vector3.zero;int count=0;
        foreach(var pixel in readable.GetPixels32()) if(pixel.a>=102) {var c=((Color)pixel).linear;sum+=new Vector3(c.r,c.g,c.b);count++;}
        if(count>0)sum/=count;mat.SetVector("_TextureMean",new Vector4(sum.x,sum.y,sum.z,0));UnityEngine.Object.DestroyImmediate(readable);
       }
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
    // Older source revisions had an inward open cliff sheet. Identify via source map
    // and test orientation: v3 is already corrected and must not be flipped twice.
    var frontNormal=Quaternion.Euler(0,-22,0)*Vector3.back;
    var averageNormal=Vector3.zero;foreach(var normal in mesh.normals)averageNormal+=normal;
    if(mf.name==frontBatch && Vector3.Dot(averageNormal,frontNormal)<0) {
     for(int sm=0;sm<mesh.subMeshCount;sm++){var ids=mesh.GetTriangles(sm);for(int i=0;i<ids.Length;i+=3){int a=ids[i];ids[i]=ids[i+2];ids[i+2]=a;}mesh.SetTriangles(ids,sm);}
     var normals=mesh.normals;for(int i=0;i<normals.Length;i++)normals[i]=-normals[i];mesh.normals=normals;
     Debug.Log("[FullStage] repaired inward-facing open front cliff");
    }
    // Joined Blender meshes can have blank UV0 on newly conformed inset stones.
    // Repair only degenerate textured faces, retaining original UVs everywhere else.
    var uv=mesh.uv; var originalUv=(Vector2[])uv.Clone(); var alternateUv=mesh.uv2; int repaired=0, recovered=0;
    for(int sm=0;sm<mesh.subMeshCount;sm++) {
     string surface=output[sm].name;
     if(surface.Contains("Cutout")||surface.Contains("leaf")||surface.Contains("lantern")||surface.Contains("water"))continue;
     var ids=mesh.GetTriangles(sm);
     for(int i=0;i<ids.Length;i+=3) {
      int a=ids[i],b=ids[i+1],c=ids[i+2];var u=originalUv[b]-originalUv[a];var v=originalUv[c]-originalUv[a];
      if(Mathf.Abs(u.x*v.y-u.y*v.x)>1e-9f)continue;
      var n=Vector3.Cross(vs[b]-vs[a],vs[c]-vs[a]);if(n.sqrMagnitude<1e-10f)continue;
      if(alternateUv.Length==uv.Length) {
       var au=alternateUv[b]-alternateUv[a];var av=alternateUv[c]-alternateUv[a];
       if(Mathf.Abs(au.x*av.y-au.y*av.x)>1e-9f) {uv[a]=alternateUv[a];uv[b]=alternateUv[b];uv[c]=alternateUv[c];recovered++;continue;}
      }
      foreach(int q in new[]{a,b,c})uv[q]=Mathf.Abs(n.y)>Mathf.Max(Mathf.Abs(n.x),Mathf.Abs(n.z))?new Vector2(vs[q].x,vs[q].z)*.125f:Mathf.Abs(n.z)>Mathf.Abs(n.x)?new Vector2(vs[q].x,vs[q].y)*.125f:new Vector2(vs[q].z,vs[q].y)*.125f;
      repaired++;
     }
    }
    mesh.uv=uv;if(recovered>0)Debug.Log("[FullStage] original UV1 recovered "+mf.name+" faces="+recovered);if(repaired>0)Debug.Log("[FullStage] repaired degenerate UV faces "+mf.name+" count="+repaired);
    mesh.RecalculateBounds();triangles+=mesh.triangles.Length/3;
    var pathMesh=Root+"/"+mf.name+".asset";Save(mesh,pathMesh);
    var sourceEntry=sourceMap.FirstOrDefault(b=>(string)b["mesh"]==mf.name);
    bool farfield=sourceEntry!=null && ((JArray)sourceEntry["sources"]).All(n=>((string)n).StartsWith("Farfield_"));
    var go=new GameObject(mf.name+(farfield?"__Farfield":""));go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(pathMesh);go.AddComponent<MeshRenderer>().sharedMaterials=output;
   }
   // Opaque distant atmosphere behind the alpha forest: holes must reveal air, not a black clear color.
   var skyMat=new Material(Shader.Find("DeckRogue/StageModule")){name="Distant forest atmosphere"};
   skyMat.EnableKeyword("_UV_MESH");skyMat.SetFloat("_MeshArraySlice",-1);skyMat.SetFloat("_NormalArrayOn",0);skyMat.SetFloat("_VColorAO",0);
   skyMat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot+"/distant_forest_haze.png"));
   skyMat.SetColor("_BaseColor",new Color(.5f,.65f,.7f,1));skyMat.SetFloat("_NaturalSurface",1);skyMat.SetVector("_AmbientFill",new Vector4(.2f,.2f,.2f,0));
   skyMat.SetFloat("_Receive",0);skyMat.SetFloat("_HitReceive",0);skyMat.SetFloat("_ShadowStrength",0);skyMat.SetFloat("_Fog",.35f);skyMat.SetFloat("_GradeWeight",.15f);skyMat.SetFloat("_VignetteWeight",.2f);
   Save(skyMat,Root+"/DistantAtmosphere.mat");
   var skyMesh=new Mesh{name="Distant atmosphere"};skyMesh.vertices=new[]{new Vector3(-80,-7,85),new Vector3(-80,63,85),new Vector3(80,63,85),new Vector3(80,-7,85)};
   skyMesh.uv=new[]{Vector2.zero,new Vector2(0,2),new Vector2(4,2),new Vector2(4,0)};skyMesh.triangles=new[]{0,1,2,0,2,3};skyMesh.RecalculateNormals();skyMesh.RecalculateBounds();Save(skyMesh,Root+"/DistantAtmosphere.asset");
   var sky=new GameObject("DistantAtmosphere");sky.transform.SetParent(root.transform,false);sky.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/DistantAtmosphere.asset");
   var skyRenderer=sky.AddComponent<MeshRenderer>();skyRenderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/DistantAtmosphere.mat");skyRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;skyRenderer.receiveShadows=false;
   PrefabUtility.SaveAsPrefabAsset(root,Root+"/ForestStage.prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
   Debug.Log("[FullStage] BUILD PASS triangles="+triangles+" materials="+mats.Count+"; cutouts use common StageModule forward/depth/shadow alpha");return 0;
  }
 }
}
