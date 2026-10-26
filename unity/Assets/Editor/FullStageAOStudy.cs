using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
namespace DeckRogue.EditorTools {
 public static class FullStageAOStudy {
  struct Sample { public Vector3 p,n; public Color c; }
  static string Key(Vector3 v) => Mathf.RoundToInt(v.x*1000)+","+Mathf.RoundToInt(v.y*1000)+","+Mathf.RoundToInt(v.z*1000);
  public static int Build() {
   var src=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Editor/FullStage/ao_study.fbx");
   if(src==null)throw new Exception("Missing AO study FBX");
   var ts=src.GetComponentsInChildren<Transform>(true);
   Func<string,Vector3> marker=n=>ts.Single(t=>t.name==n).position;
   var basis=Matrix4x4.identity;
   basis.SetColumn(0,(Vector4)marker("WORLD_RIGHT_1M"));basis.SetColumn(1,(Vector4)marker("WORLD_UP_1M"));basis.SetColumn(2,(Vector4)marker("WORLD_FORWARD_1M"));
   var correction=basis.inverse;
   var result=new JObject();int total=0;float minimum=1;float maxError=0;
   foreach(var mf in src.GetComponentsInChildren<MeshFilter>(true)) {
    var target=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/FullStage/"+mf.name+".asset");
    if(target==null)throw new Exception("No original batch: "+mf.name);
    var source=mf.sharedMesh;var colors=source.colors;
    if(colors.Length!=source.vertexCount)throw new Exception("AO colors missing: "+mf.name);
    var transform=correction*mf.transform.localToWorldMatrix;var nt=transform.inverse.transpose;
    var sv=source.vertices;var sn=source.normals;var buckets=new Dictionary<string,List<Sample>>();
    for(int i=0;i<sv.Length;i++) {
     var sample=new Sample{p=transform.MultiplyPoint3x4(sv[i]),n=nt.MultiplyVector(sn[i]).normalized,c=colors[i]};
     if(Mathf.Abs(sample.c.a-1)>.001f || Mathf.Abs(sample.c.r-sample.c.g)>.001f || Mathf.Abs(sample.c.r-sample.c.b)>.001f)throw new Exception("Invalid AO color");
     var key=Key(sample.p);if(!buckets.TryGetValue(key,out var list))buckets[key]=list=new List<Sample>();list.Add(sample);
    }
    var tv=target.vertices;var tn=target.normals;var values=new JArray();float localMin=1;double sum=0;
    for(int i=0;i<tv.Length;i++) {
     // Search adjacent quantization cells as exporters differ at rounding boundaries.
     Sample best=default;float score=float.MaxValue;
     for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++) {
      if(!buckets.TryGetValue(Key(tv[i]+new Vector3(x,y,z)*.001f),out var list))continue;
      foreach(var c in list){float distance=(c.p-tv[i]).sqrMagnitude;float cost=distance+(1-Mathf.Clamp(Vector3.Dot(c.n,tn[i]),-1,1))*.01f;if(cost<score){score=cost;best=c;}}
     }
     float error=Vector3.Distance(best.p,tv[i]);
     if(score==float.MaxValue||error>.0005f||Vector3.Dot(best.n,tn[i])<.99f)throw new Exception("AO correspondence failed "+mf.name+" vertex="+i+" distance="+error+" normal="+Vector3.Dot(best.n,tn[i]));
     float v=best.c.r;if(v<.28f||v>1.001f)throw new Exception("AO import range invalid "+v);
     values.Add(Mathf.RoundToInt(v*255));localMin=Mathf.Min(localMin,v);sum+=v;maxError=Mathf.Max(maxError,error);
    }
    result[mf.name]=values;total+=tv.Length;minimum=Mathf.Min(minimum,localMin);
    Debug.Log("[AOStudy] "+mf.name+" vertices="+tv.Length+" min="+localMin+" mean="+(sum/tv.Length));
   }
   System.IO.File.WriteAllText("Assets/Resources/FullStage/ao_study.json",result.ToString(Newtonsoft.Json.Formatting.None));AssetDatabase.Refresh();
   Debug.Log("[AOStudy] PASS batches="+result.Count+" vertices="+total+" min="+minimum+" correspondenceMaxError="+maxError+" original meshes unchanged");return 0;
  }
 }
}
