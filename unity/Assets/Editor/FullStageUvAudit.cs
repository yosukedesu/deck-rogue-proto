using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
namespace DeckRogue.EditorTools {
 public static class FullStageUvAudit {
  static JArray V(Vector3 v)=>new JArray(v.x,v.y,v.z);
  static JArray U(Vector2 v)=>new JArray(v.x,v.y);
  public static int Run(){
   var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Editor/FullStage/mossbound_full_stage.fbx");
   var report=new JArray();
   foreach(var mf in source.GetComponentsInChildren<MeshFilter>(true)){
    var mesh=mf.sharedMesh;var vs=mesh.vertices;var channels=new List<Vector2>[4];
    for(int c=0;c<4;c++){channels[c]=new List<Vector2>();mesh.GetUVs(c,channels[c]);}
    var row=new JObject{{"mesh",mf.name},{"vertexCount",vs.Length},{"uvChannelVertexCounts",new JArray(channels[0].Count,channels[1].Count,channels[2].Count,channels[3].Count)}};
    var subs=new JArray();row["submeshes"]=subs;report.Add(row);
    for(int sm=0;sm<mesh.subMeshCount;sm++){
     var ids=mesh.GetTriangles(sm);var counts=new int[4];var samples=new JArray();
     for(int t=0;t<ids.Length;t+=3){
      bool zero0=false;
      for(int c=0;c<4;c++)if(channels[c].Count==vs.Length){var uv=channels[c];var a=uv[ids[t+1]]-uv[ids[t]];var b=uv[ids[t+2]]-uv[ids[t]];if(Mathf.Abs(a.x*b.y-a.y*b.x)<1e-9f){counts[c]++;if(c==0)zero0=true;}}
      if(zero0&&samples.Count<12){
       var corners=new JArray();for(int k=0;k<3;k++){int index=ids[t+k];var w=mf.transform.TransformPoint(vs[index]);var corner=new JObject{{"index",index},{"unity_world",V(new Vector3(-w.x,w.y,-w.z))}};for(int c=0;c<4;c++)if(channels[c].Count==vs.Length)corner["uv"+c]=U(channels[c][index]);corners.Add(corner);}
       samples.Add(new JObject{{"triangle",t/3},{"corners",corners}});
      }
     }
     subs.Add(new JObject{{"material",mf.GetComponent<MeshRenderer>().sharedMaterials[sm].name},{"triangles",ids.Length/3},{"zeroAreaUvCounts",new JArray(counts)},{"uv0ZeroSamples",samples}});
     Debug.Log("[FullStageUV] "+mf.name+" sub="+sm+" zeroUV="+string.Join(",",counts)+" / "+ids.Length/3);
    }
   }
   File.WriteAllText("FullStageUvAudit.json",report.ToString());return 0;
  }
 }
}
