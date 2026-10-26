using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using StrictGround;
namespace DeckRogue.Game {
 // Display-only snapshot of actual mesh triangles. Legacy FullStageGround.Height is independent.
 // Geometry is static after stage construction. A future writer MUST invalidate this provider
 // before editing in-place; cheap active-lease checks do not scan all mesh vertices every frame.
 internal sealed class GroundSurfaceProvider {
  const string ProofResource="FullStageIntegrated/StageBatch_02";
  const string ProofHash="3ace3b709322aa1280b2f524593db667685ddcfa3c2fd4dc5361210d3c5521d6";
  const int MaxProviders=1024,MaxSnapshotTriangles=131072,MaxQueryCells=4096;
  const float MaxWorldCoordinate=1000000f; // keep all spatial-cell casts/differences bounded
  static long nextGeneration;
  readonly long generation=++nextGeneration;
  readonly GameObject root;
  readonly List<Witness> witnesses=new List<Witness>();
  readonly List<Ancestor> ancestry=new List<Ancestor>();readonly HashSet<Transform> ancestorSet=new HashSet<Transform>();
  readonly List<SourceTriangle> triangles=new List<SourceTriangle>();
  readonly Dictionary<Vector2Int,List<int>> cells=new Dictionary<Vector2Int,List<int>>();
  readonly List<MeshFilter> membership=new List<MeshFilter>();
  readonly List<SourceTriangle> candidates=new List<SourceTriangle>();
  readonly HashSet<int> selected=new HashSet<int>();
  readonly CaptureWorkspace captureWorkspace=new CaptureWorkspace();
  readonly List<Material> currentMaterials=new List<Material>();
  readonly HashScratch hashScratch=new HashScratch();
  bool valid=true;string initialFailure;
  internal int CaptureAttempts {get;private set;}
  internal long Generation=>generation;
  sealed class Ancestor {internal Transform Node,Parent;internal Matrix4x4 Matrix;}
  sealed class Witness {
   internal MeshFilter Filter;internal Mesh Mesh;internal MeshRenderer Renderer;
   internal Matrix4x4 Matrix;internal Transform Parent;internal bool Active,Enabled;internal Material[] Materials;internal string[] Names;
   internal string GeometryHash;internal bool Proven;
  }
  internal sealed class Lease {
   internal readonly GroundSurfaceProvider Owner;internal readonly Capture Geometry;
   internal Lease(GroundSurfaceProvider owner,Capture geometry){Owner=owner;Geometry=geometry;}
   internal bool IsCurrent(out string reason){reason="ground_owner_missing";return Owner!=null&&Geometry!=null&&Geometry.Generation==Owner.Generation&&Owner.CheckCurrent(false,out reason);}
   internal bool VerifyContact(out string reason){reason="ground_owner_missing";return Owner!=null&&Geometry!=null&&Geometry.Generation==Owner.Generation&&Owner.CheckCurrent(true,out reason);}
  }
  internal GroundSurfaceProvider(GameObject source) {
   root=source;
   try { Build(); }catch(Exception){initialFailure="ground_snapshot_unavailable";valid=false;}
  }
  internal void Invalidate(){valid=false;}
  static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
  static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
  static bool WorldBounded(Vector3 v)=>Finite(v)&&Mathf.Abs(v.x)<=MaxWorldCoordinate&&Mathf.Abs(v.y)<=MaxWorldCoordinate&&Mathf.Abs(v.z)<=MaxWorldCoordinate;
  static bool MatrixFinite(Matrix4x4 m){for(int i=0;i<16;i++)if(!Finite(m[i]))return false;return true;}
  static Vector2Int Cell(float x,float z)=>new Vector2Int(Mathf.FloorToInt(x/2f),Mathf.FloorToInt(z/2f));
  static D3 D(Vector3 v)=>new D3(v.x,v.y,v.z);
  static bool Included(string n)=>n!=null&&!n.Contains("Cutout")&&!n.Contains("leaf")&&!n.Contains("water")&&!n.Contains("bronze")&&!n.Contains("lantern");
  void Build(){
   if(root==null){initialFailure="ground_root_missing";valid=false;return;}
   root.GetComponentsInChildren(true,membership);
   if(membership.Count==0||membership.Count>MaxProviders){initialFailure="ground_provider_count";valid=false;return;}
   var proofMesh=Resources.Load<Mesh>(ProofResource);
   // TriangleId is generation-scoped: keep EntityId opaque and assign collision-free local keys.
   var filterIds=new Dictionary<EntityId,int>();
   foreach(var filter in membership){
    int depth=0;for(var node=filter.transform;node!=null;node=node.parent){if(++depth>256||ancestry.Count>=16384){initialFailure="ground_ancestry_cap";valid=false;return;}if(ancestorSet.Add(node))ancestry.Add(new Ancestor{Node=node,Parent=node.parent,Matrix=node.localToWorldMatrix});}
    var mesh=filter.sharedMesh;var renderer=filter.GetComponent<MeshRenderer>();
    if(mesh==null||renderer==null){initialFailure="ground_provider_incomplete";valid=false;return;}
    var matrix=filter.transform.localToWorldMatrix;
    if(!MatrixFinite(matrix)||!Finite(matrix.determinant)||matrix.determinant<=0f){initialFailure="ground_transform_unproved";valid=false;return;}
    var mats=renderer.sharedMaterials;
    if(mats.Length<mesh.subMeshCount){initialFailure="ground_material_membership";valid=false;return;}
    var names=new string[mats.Length];for(int k=0;k<mats.Length;k++){if(mats[k]==null){initialFailure="ground_material_missing";valid=false;return;}names[k]=mats[k].name;}
    string hash=hashScratch.Hash(mesh);
    var witness=new Witness{Filter=filter,Mesh=mesh,Renderer=renderer,Matrix=matrix,Parent=filter.transform.parent,Active=filter.gameObject.activeInHierarchy,Enabled=renderer.enabled,Materials=mats,Names=names,GeometryHash=hash,Proven=mesh==proofMesh&&hash==ProofHash&&renderer.enabled};
    witnesses.Add(witness);
    if(!witness.Active)continue; // exactly the active-filter membership of legacy Height
    var entityId=filter.GetEntityId();
    if(!filterIds.TryGetValue(entityId,out int filterId)){filterId=filterIds.Count+1;filterIds.Add(entityId,filterId);}
    var vertices=mesh.vertices;
    for(int sm=0;sm<mesh.subMeshCount;sm++){
     if(!Included(names[sm]))continue;
     if(mesh.GetTopology(sm)!=MeshTopology.Triangles){initialFailure="ground_topology_unproved";valid=false;return;}
     var ids=mesh.GetTriangles(sm);
     if(ids.Length%3!=0){initialFailure="ground_indices_invalid";valid=false;return;}
     for(int k=0;k<ids.Length;k+=3){
      int ia=ids[k],ib=ids[k+1],ic=ids[k+2];
      if(ia<0||ib<0||ic<0||ia>=vertices.Length||ib>=vertices.Length||ic>=vertices.Length){initialFailure="ground_indices_invalid";valid=false;return;}
      var a=filter.transform.TransformPoint(vertices[ia]);var b=filter.transform.TransformPoint(vertices[ib]);var c=filter.transform.TransformPoint(vertices[ic]);
      if(!WorldBounded(a)||!WorldBounded(b)||!WorldBounded(c)){initialFailure="ground_vertex_nonfinite";valid=false;return;}
      if(Mathf.Abs(Vector3.Cross(b-a,c-a).y)<.00001f)continue; // identical Height candidate exclusion
      if(triangles.Count>=MaxSnapshotTriangles){initialFailure="ground_snapshot_cap";valid=false;return;}
      int index=triangles.Count;triangles.Add(new SourceTriangle(new TriangleId(generation,filterId,witness.Proven?1:0,sm,k/3),D(a),D(b),D(c),witness.Proven,ia,ib,ic));
      var lo=Cell(Mathf.Min(a.x,b.x,c.x),Mathf.Min(a.z,b.z,c.z));var hi=Cell(Mathf.Max(a.x,b.x,c.x),Mathf.Max(a.z,b.z,c.z));
      if((long)(hi.x-lo.x+1)*(hi.y-lo.y+1)>MaxQueryCells){initialFailure="ground_triangle_span_cap";valid=false;return;}
      for(int x=lo.x;x<=hi.x;x++)for(int z=lo.y;z<=hi.y;z++){var key=new Vector2Int(x,z);if(!cells.TryGetValue(key,out var list))cells[key]=list=new List<int>();list.Add(index);}
     }
    }
   }
  }
  internal bool CheckCurrent(bool strong,out string reason){
   reason=initialFailure;
   if(!valid||root==null){reason=reason??"ground_generation_invalidated";return false;}
   try {
    foreach(var a in ancestry){if(a.Node==null||a.Node.parent!=a.Parent){reason="ground_ancestor_parent_changed";return false;}var matrix=a.Node.localToWorldMatrix;for(int k=0;k<16;k++)if(matrix[k]!=a.Matrix[k]){reason="ground_ancestor_transform_changed";return false;}}
    membership.Clear();root.GetComponentsInChildren(true,membership);
    if(membership.Count!=witnesses.Count){reason="ground_provider_membership_changed";return false;}
    for(int i=0;i<witnesses.Count;i++){
     var w=witnesses[i];
     if(w.Filter==null||membership[i]!=w.Filter||w.Mesh==null||w.Filter.sharedMesh!=w.Mesh||w.Renderer==null||w.Filter.GetComponent<MeshRenderer>()!=w.Renderer){reason="ground_mesh_identity_changed";return false;}
     if(w.Filter.transform.parent!=w.Parent){reason="ground_provider_parent_changed";return false;}
     if(w.Filter.gameObject.activeInHierarchy!=w.Active||w.Renderer.enabled!=w.Enabled){reason="ground_provider_visibility_changed";return false;}
     var matrix=w.Filter.transform.localToWorldMatrix;
     for(int k=0;k<16;k++)if(matrix[k]!=w.Matrix[k]){reason="ground_provider_transform_changed";return false;}
     currentMaterials.Clear();w.Renderer.GetSharedMaterials(currentMaterials);var mats=currentMaterials;if(mats.Count!=w.Materials.Length){reason="ground_material_membership_changed";return false;}
     for(int k=0;k<mats.Count;k++)if(mats[k]==null||mats[k]!=w.Materials[k]||mats[k].name!=w.Names[k]){reason="ground_material_membership_changed";return false;}
     if(strong&&hashScratch.Hash(w.Mesh)!=w.GeometryHash){reason="ground_inplace_geometry_changed";return false;}
    }
    reason=null;return true;
   }catch(Exception){reason="ground_provider_check_unavailable";return false;}
  }
  internal bool TryCapture(Vector3 feet,float visibleHeight,float size,Family family,out Lease lease,out string reason){
   CaptureAttempts++;lease=null;
   if(!WorldBounded(feet)||!Finite(visibleHeight)||visibleHeight<=0f||!Finite(size)||size<=0f){reason="ground_request_nonfinite";return false;}
   if(!CheckCurrent(true,out reason))return false;
   double h=(double)visibleHeight*size;
   if(double.IsNaN(h)||double.IsInfinity(h)||h<=0||h>64){reason="ground_request_span_cap";return false;}
   // A unit orthonormal row projects the entire +/-1.25,+/-.85,+/-.18 prism
   // into <=sqrt(1.25²+.85²+.18²)H in either world X/Z. 1.6H is conservative.
   float reach=(float)(1.6*h)+.0001f;var lo=Cell(feet.x-reach,feet.z-reach);var hi=Cell(feet.x+reach,feet.z+reach);
   if((long)(hi.x-lo.x+1)*(hi.y-lo.y+1)>MaxQueryCells){reason="ground_query_cell_cap";return false;}
   candidates.Clear();selected.Clear();
   for(int x=lo.x;x<=hi.x;x++)for(int z=lo.y;z<=hi.y;z++)if(cells.TryGetValue(new Vector2Int(x,z),out var list))foreach(int id in list)if(selected.Add(id))candidates.Add(triangles[id]);
   var request=new Request(generation,generation,0,D(feet),visibleHeight,family,size);
   if(!GroundCapture.TryCapture(request,candidates,out var captured,out var failure,workspace:captureWorkspace)){reason="ground_"+failure.ToString();return false;}
   if(!CheckCurrent(false,out reason))return false;
   lease=new Lease(this,captured);reason=null;return true;
  }
  // Geometry-only proof: little-endian float32 xyz and applied-baseVertex triangle indices.
  // Uses reusable lists/stream buffers; called at snapshot, capture and Land, never per frame.
  sealed class HashScratch {
   readonly List<Vector3> vertices=new List<Vector3>();readonly List<int> indices=new List<int>();
   readonly MemoryStream stream=new MemoryStream();readonly BinaryWriter writer;
   internal HashScratch(){writer=new BinaryWriter(stream);}
   internal string Hash(Mesh mesh){
    vertices.Clear();mesh.GetVertices(vertices);stream.SetLength(0);stream.Position=0;writer.Write((uint)vertices.Count);
    foreach(var v in vertices){if(!Finite(v))throw new InvalidDataException("nonfinite mesh");writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}
    writer.Write((uint)mesh.subMeshCount);
    for(int sm=0;sm<mesh.subMeshCount;sm++){
     if(mesh.GetTopology(sm)!=MeshTopology.Triangles)throw new InvalidDataException("nontriangle mesh");
     indices.Clear();mesh.GetTriangles(indices,sm,true);writer.Write((uint)indices.Count);foreach(int index in indices)writer.Write((uint)index);
    }
    writer.Flush();stream.Position=0;using(var hash=SHA256.Create()){var bytes=hash.ComputeHash(stream);return BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();}
   }
  }
 }
}
