using System;
using System.Runtime.InteropServices;
using UnityEngine;
using StrictGround;
namespace DeckRogue.Game {
 // R9-only support adapter. It changes the request domain, never terrain proofs.
 // The frozen floor paths have full-length segments and exactly two vertex states:
 // joined contact normals (0) and raw inactive normals (-.14). No shader expands them.
 internal static class LightningR9FloorSupport {
  internal struct Domain {
   internal Vector3 Center;
   internal float CertificateHeight;
   internal double MinX,MaxX,MinZ,MaxZ,Reach;
   internal int VerticesPerState;
  }
  [StructLayout(LayoutKind.Explicit)] struct FloatBits {
   [FieldOffset(0)] internal float Value;
   [FieldOffset(0)] internal int Bits;
  }
  [StructLayout(LayoutKind.Explicit)] struct DoubleBits {
   [FieldOffset(0)] internal double Value;
   [FieldOffset(0)] internal long Bits;
  }
  static double Up(double value){
   if(value==0)return double.Epsilon;
   var b=new DoubleBits{Value=value};b.Bits+=value>0?1:-1;return b.Value;
  }
  static double Down(double value)=>-Up(-value);
  static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
  static bool Finite(Vector3 p)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z);
  static bool Fail(string value,out string reason){reason=value;return false;}
  static float Outward(double value){
   float f=(float)value;
   if(!Finite(f)||f<=0)return float.NaN;
   // The smallest positive float strictly outside the computed real-valued bound.
   // This is directed rounding, not a spatial acceptance epsilon.
   if(f<=value){var b=new FloatBits{Value=f};b.Bits++;f=b.Value;}
   return f;
  }
  internal static Vector3 Project(Quaternion rotation,Vector3 bodyFeet,float floorAnchorY,float scale,float visibleHeight,Vector3 toward,LightningR9Geometry.Point p){
   var offset=rotation*new Vector3((float)p.X,(float)p.Y,-(float)p.Z)*scale;
   // Project offsets before translation. Changing only plane Y cannot perturb XZ
   // through subtracting and re-adding animated display-feet Y in float arithmetic.
   var flat=offset-toward*(offset.y/toward.y);
   return new Vector3(bodyFeet.x,floorAnchorY,bodyFeet.z)+flat+Vector3.up*(.0015f*visibleHeight);
  }
  internal static bool TryDomain(LightningR9Geometry.Buffer geometry,Quaternion rotation,Vector3 bodyFeet,float scale,float visibleHeight,Vector3 toward,out Domain domain,out string reason){
   domain=default;reason=null;
   if(geometry==null||!Finite(bodyFeet)||!Finite(scale)||scale<=0||!Finite(visibleHeight)||visibleHeight<=0||!Finite(toward)||Math.Abs(toward.y)<.0001f)return Fail("floor_view_basis_unproved",out reason);
   // Include the unmodified body-feet XZ so the later actor/surface association
   // uses the same complete, certified rectangle rather than a second query.
   double x0=bodyFeet.x,x1=bodyFeet.x,z0=bodyFeet.z,z1=bodyFeet.z,reach=0;int count=0;
   for(int state=0;state<2;state++){
    LightningR9Geometry.Sample(geometry,state==0?-.14:0);count=0;
    foreach(var batch in geometry.Batches)if(batch.Floor)foreach(var vertex in batch.Vertices){
     var p=Project(rotation,bodyFeet,bodyFeet.y,scale,visibleHeight,toward,vertex);
     if(!Finite(p))return Fail("floor_vertex_nonfinite",out reason);
     x0=Math.Min(x0,p.x);x1=Math.Max(x1,p.x);z0=Math.Min(z0,p.z);z1=Math.Max(z1,p.z);
     reach=Math.Max(reach,Math.Max(Math.Abs((double)p.x-bodyFeet.x),Math.Abs((double)p.z-bodyFeet.z)));count++;
    }
   }
   if(count==0||!Finite(reach)||reach>.80f*visibleHeight)return Fail("floor_projected_footprint_unproved",out reason);
   // Float-round the center FIRST; otherwise center rounding can shrink one edge.
   float cx=(float)((x0+x1)*.5),cz=(float)((z0+z1)*.5);
   double required=Math.Max(Math.Max(cx-x0,x1-cx)/GroundCapture.HalfX,Math.Max(cz-z0,z1-cz)/GroundCapture.HalfZ);
   float certH=Outward(Up(required));
   if(!Finite(cx)||!Finite(cz)||!Finite(certH)||certH<=0)return Fail("floor_support_domain_nonfinite",out reason);
   domain=new Domain{Center=new Vector3(cx,bodyFeet.y,cz),CertificateHeight=certH,MinX=x0,MaxX=x1,MinZ=z0,MaxZ=z1,Reach=reach,VerticesPerState=count};return true;
  }
  static void ProductInterval(double coordinate,double origin,double coefficient,out double lo,out double hi){
   double d=coordinate-origin,l=Down(d),h=Up(d);
   lo=Down((coefficient>=0?l:h)*coefficient);hi=Up((coefficient>=0?h:l)*coefficient);
  }
  static double DotAbsUpper(Vector3 world,D3 origin,D3 axis){
   ProductInterval(world.x,origin.X,axis.X,out double xl,out double xh);
   ProductInterval(world.y,origin.Y,axis.Y,out double yl,out double yh);
   ProductInterval(world.z,origin.Z,axis.Z,out double zl,out double zh);
   double lo=Down(Down(xl+yl)+zl),hi=Up(Up(xh+yh)+zh);
   return Math.Max(Math.Abs(lo),Math.Abs(hi));
  }
  internal static bool Contains(Capture g,Vector3 world){
   if(g==null||!Finite(world)||!g.Origin.Finite||!g.Right.Finite||!g.Forward.Finite||!Finite(g.EffectiveH)||g.EffectiveH<=0)return false;
   double x=DotAbsUpper(world,g.Origin,g.Right),z=DotAbsUpper(world,g.Origin,g.Forward);
   // Outward-rounded arithmetic bounds the exact dot product of represented
   // coordinates. Shrink the rounded limit, never expand support acceptance.
   // A tiny tilt or an uncertain edge rejects; no spatial tolerance is used.
   return Finite(x)&&Finite(z)&&x<=Down(GroundCapture.HalfX*g.EffectiveH)&&z<=Down(GroundCapture.HalfZ*g.EffectiveH);
  }
  internal static bool ContainsFloor(Capture g,Vector3 world){
   if(!Contains(g,world)||!g.Normal.Finite)return false;
   // Final GPU floor geometry must fit the full captured prism. Keep this
   // normal bound independent of the actor-association guard tolerance.
   double normal=DotAbsUpper(world,g.Origin,g.Normal);
   return Finite(normal)&&normal<=Down(GroundCapture.MaxHeight*g.EffectiveH);
  }
  static double PrismExtent(double right,double normal,double forward,double height){
   double x=Up(Math.Abs(right)*GroundCapture.HalfX);
   double y=Up(Math.Abs(normal)*GroundCapture.MaxHeight);
   double z=Up(Math.Abs(forward)*GroundCapture.HalfZ);
   return Up(Up(Up(x+y)+z)*height);
  }
  internal static bool QueryContainsPrism(Capture g,Domain domain){
   if(g==null||!g.Origin.Finite||!g.Right.Finite||!g.Normal.Finite||!g.Forward.Finite||!Finite(g.EffectiveH)||g.EffectiveH<=0)return false;
   // Match the unchanged provider's actual float cell arithmetic, not its ideal
   // 1.6H explanation. At large coordinates a float addition can round inward.
   float reach=(float)(1.6*(double)domain.CertificateHeight)+.0001f;
   if(!Finite(reach)||!Finite(domain.Center)||!Finite(domain.CertificateHeight)||domain.CertificateHeight<=0||domain.CertificateHeight>64||Math.Abs(domain.Center.x)>1000000||Math.Abs(domain.Center.y)>1000000||Math.Abs(domain.Center.z)>1000000)return false;
   int lx=Mathf.FloorToInt((domain.Center.x-reach)/2f),hx=Mathf.FloorToInt((domain.Center.x+reach)/2f);
   int lz=Mathf.FloorToInt((domain.Center.z-reach)/2f),hz=Mathf.FloorToInt((domain.Center.z+reach)/2f);
   if(lx>hx||lz>hz||((long)hx-lx+1)*((long)hz-lz+1)>4096)return false;
   double ex=PrismExtent(g.Right.X,g.Normal.X,g.Forward.X,g.EffectiveH);
   double ez=PrismExtent(g.Right.Z,g.Normal.Z,g.Forward.Z,g.EffectiveH);
   double x0=Down(g.Origin.X-ex),x1=Up(g.Origin.X+ex),z0=Down(g.Origin.Z-ez),z1=Up(g.Origin.Z+ez);
   // Every source triangle intersecting this full outward prism AABB necessarily
   // belongs to an enumerated cell. Unknown candidates keep all original proofs.
   return Finite(x0)&&Finite(x1)&&Finite(z0)&&Finite(z1)&&x0>=2.0*lx&&x1<2.0*((double)hx+1)&&z0>=2.0*lz&&z1<2.0*((double)hz+1);
  }
  static bool TrySourceNormal(SourceTriangle t,out D3 normal){
   // Same scale-conditioned normal construction as unchanged GroundCapture.
   var u=t.B-t.A;var v=t.C-t.A;
   double m=Math.Max(Math.Max(Math.Abs(u.X),Math.Max(Math.Abs(u.Y),Math.Abs(u.Z))),Math.Max(Math.Abs(v.X),Math.Max(Math.Abs(v.Y),Math.Abs(v.Z))));
   normal=default;if(!Finite(m)||m==0)return false;
   var cross=D3.Cross(u/m,v/m);double length=cross.Length;
   if(!Finite(length)||length<1e-15)return false;normal=cross/length;return normal.Finite;
  }
  internal static bool TryAnchor(Capture g,Vector3 bodyFeet,Domain domain,out float floorAnchorY,out TriangleId source,out string reason){
   floorAnchorY=float.NaN;source=default;reason=null;
   if(g==null||g.Indices.Count==0||g.UnbiasedVertices.Count<3||g.Sources.Count==0||!g.Normal.Finite||Math.Abs(g.Normal.Y-1)>1e-6)return Fail("nonhorizontal_floor",out reason);
   if(!Finite(domain.CertificateHeight)||g.EffectiveH!=(double)domain.CertificateHeight||g.Origin.X!=domain.Center.x||g.Origin.Z!=domain.Center.z||!Contains(g,bodyFeet))return Fail("floor_body_anchor_outside_certificate",out reason);
   if(!QueryContainsPrism(g,domain))return Fail("floor_query_envelope_unproved",out reason);
   // New bounded actor/surface association, using the existing certificate prism.
   // StrictGround's request Feet.Y itself does not establish this association.
   var delta=new D3(bodyFeet.x-g.Origin.X,bodyFeet.y-g.Origin.Y,bodyFeet.z-g.Origin.Z);
   double normalHeight=D3.Dot(delta,g.Normal)/g.EffectiveH;
   if(!Finite(normalHeight)||DotAbsUpper(bodyFeet,g.Origin,g.Normal)>Down((GroundCapture.MaxHeight+GroundCapture.GuardTolerance)*g.EffectiveH))return Fail("floor_body_anchor_height_unproved",out reason);
   bool found=false;double y=double.NegativeInfinity;D3 anchorNormal=default;
   var foot=new D3(bodyFeet.x,0,bodyFeet.z);
   // Sources are the actual proven triangles of the unchanged complete capture.
   // Reconstruct the real surface at immutable body-feet XZ, never default y=0
   // and never the shifted certificate center's representative height.
   foreach(var t in g.Sources){
    if(!t.Proven||!t.Id.Valid||t.Id.Proof==0||t.Id.Generation!=g.Generation||t.AIndex<0||t.BIndex<0||t.CIndex<0||t.AIndex==t.BIndex||t.AIndex==t.CIndex||t.BIndex==t.CIndex||!t.A.Finite||!t.B.Finite||!t.C.Finite)return Fail("floor_anchor_source_unproved",out reason);
    if(foot.X<Math.Min(t.A.X,Math.Min(t.B.X,t.C.X))||foot.X>Math.Max(t.A.X,Math.Max(t.B.X,t.C.X))||foot.Z<Math.Min(t.A.Z,Math.Min(t.B.Z,t.C.Z))||foot.Z>Math.Max(t.A.Z,Math.Max(t.B.Z,t.C.Z)))continue;
    int orientation=GroundCapture.Orient(t.A,t.B,t.C);if(orientation==0)continue;if(orientation==2)return Fail("floor_anchor_numerical_ambiguity",out reason);
    int a=GroundCapture.Orient(t.A,t.B,foot),b=GroundCapture.Orient(t.B,t.C,foot),c=GroundCapture.Orient(t.C,t.A,foot);
    if((a!=2&&a!=0&&a!=orientation)||(b!=2&&b!=0&&b!=orientation)||(c!=2&&c!=0&&c!=orientation))continue;
    if(a==2||b==2||c==2)return Fail("floor_anchor_numerical_ambiguity",out reason);
    double determinant=(t.B.Z-t.C.Z)*(t.A.X-t.C.X)+(t.C.X-t.B.X)*(t.A.Z-t.C.Z);
    if(!Finite(determinant)||determinant==0)return Fail("floor_anchor_numerical_ambiguity",out reason);
    double u=((t.B.Z-t.C.Z)*(foot.X-t.C.X)+(t.C.X-t.B.X)*(foot.Z-t.C.Z))/determinant;
    double v=((t.C.Z-t.A.Z)*(foot.X-t.C.X)+(t.A.X-t.C.X)*(foot.Z-t.C.Z))/determinant;
    double surfaceY=u*t.A.Y+v*t.B.Y+(1-u-v)*t.C.Y;
    if(!Finite(u)||!Finite(v)||!Finite(surfaceY))return Fail("floor_anchor_nonfinite",out reason);
    if(!TrySourceNormal(t,out var normal))return Fail("floor_anchor_normal_unproved",out reason);
    if(found){
     // Mirror the unchanged Winner's proven co-planar seam rule. Full-domain
     // coverage/overlap checks already proved these are actual adjacent faces.
     // Inconsistent multiple intersections still fail closed.
     if(Math.Abs(surfaceY-y)>GroundCapture.GuardTolerance*g.EffectiveH||D3.Dot(normal,anchorNormal)<1-1e-10)return Fail("floor_anchor_ambiguous",out reason);
     if(t.Id.CompareTo(source)>=0)continue;
    }
    found=true;y=surfaceY;source=t.Id;anchorNormal=normal;
   }
   if(!found)return Fail("floor_anchor_surface_missing",out reason);
   float anchor=(float)y;
   if(!Finite(anchor)||!Contains(g,new Vector3(bodyFeet.x,anchor,bodyFeet.z)))return Fail("floor_anchor_outside_certificate",out reason);
   // Original flatness tolerance, measured against real certified surface Y,
   // never against animated body/display feet. Keep bias tied to visible height.
   foreach(var p in g.UnbiasedVertices){var world=g.World(p);if(!world.Finite||Math.Abs(world.Y-y)>.00005)return Fail("nonplanar_or_offset_floor",out reason);}
   floorAnchorY=anchor;return true;
  }
 }
}
