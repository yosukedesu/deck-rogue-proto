using System.Collections.Generic;
using UnityEngine;
namespace DeckRogue.Game {
 // Spatial buckets keep floor lookup independent of render mesh batching.
 public sealed class FullStageGround {
  struct Tri { public Vector3 A,B,C; }
  readonly Dictionary<Vector2Int,List<Tri>> cells=new Dictionary<Vector2Int,List<Tri>>();
  static Vector2Int Cell(float x,float z)=>new Vector2Int(Mathf.FloorToInt(x/2),Mathf.FloorToInt(z/2));
  internal readonly GroundSurfaceProvider CaptureProvider;
  internal void InvalidateCapture()=>CaptureProvider?.Invalidate();
  public FullStageGround(GameObject root){
   foreach(var mf in root.GetComponentsInChildren<MeshFilter>()){
    var mats=mf.GetComponent<MeshRenderer>().sharedMaterials;var mesh=mf.sharedMesh;var vs=mesh.vertices;
    for(int sm=0;sm<mesh.subMeshCount;sm++){
     string n=mats[sm].name;if(n.Contains("Cutout")||n.Contains("leaf")||n.Contains("water")||n.Contains("bronze")||n.Contains("lantern"))continue;
     var ids=mesh.GetTriangles(sm);
     for(int i=0;i<ids.Length;i+=3){var t=new Tri{A=mf.transform.TransformPoint(vs[ids[i]]),B=mf.transform.TransformPoint(vs[ids[i+1]]),C=mf.transform.TransformPoint(vs[ids[i+2]])};
      if(Mathf.Abs(Vector3.Cross(t.B-t.A,t.C-t.A).y)<.00001f)continue;
      var lo=Cell(Mathf.Min(t.A.x,t.B.x,t.C.x),Mathf.Min(t.A.z,t.B.z,t.C.z));var hi=Cell(Mathf.Max(t.A.x,t.B.x,t.C.x),Mathf.Max(t.A.z,t.B.z,t.C.z));
      for(int x=lo.x;x<=hi.x;x++)for(int z=lo.y;z<=hi.y;z++){var c=new Vector2Int(x,z);if(!cells.TryGetValue(c,out var list))cells[c]=list=new List<Tri>();list.Add(t);}
     }
    }
   }
   CaptureProvider=new GroundSurfaceProvider(root); // Display proof only; legacy Height above/below is unchanged.
  }
  public float Height(float x,float z){
   float best=float.NegativeInfinity;if(!cells.TryGetValue(Cell(x,z),out var list))return float.NaN;
   foreach(var t in list){float ax=t.B.x-t.A.x,az=t.B.z-t.A.z,bx=t.C.x-t.A.x,bz=t.C.z-t.A.z,dx=x-t.A.x,dz=z-t.A.z;
    float det=ax*bz-az*bx,u=(dx*bz-dz*bx)/det,v=(ax*dz-az*dx)/det;
    if(u>=-.0001f&&v>=-.0001f&&u+v<=1.0001f)best=Mathf.Max(best,t.A.y+u*(t.B.y-t.A.y)+v*(t.C.y-t.A.y));
   }return float.IsNegativeInfinity(best)?float.NaN:best;
  }
 }
}
