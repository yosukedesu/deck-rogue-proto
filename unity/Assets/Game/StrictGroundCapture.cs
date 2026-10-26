// Standalone capture-only prototype. No Unity, fallback, rendering, or per-frame work.
// Trust/membership, complete candidate enumeration and snapshot freshness are HOST duties.
using System;
using System.Collections.Generic;
using System.Linq;
namespace StrictGround
{
internal static class Num { internal static bool Finite(double d) => !double.IsNaN(d) && !double.IsInfinity(d); }

public readonly struct D2 : IEquatable<D2>
{
    public readonly double X, Y;
    public D2(double x, double y) { X=x; Y=y; }
    public bool Equals(D2 p) => X==p.X && Y==p.Y;
    public override bool Equals(object? p) => p is D2 q && Equals(q);
    public override int GetHashCode() => HashCode.Combine(X,Y);
}
public readonly struct D3 : IEquatable<D3>
{
    public readonly double X,Y,Z;
    public D3(double x,double y,double z) { X=x;Y=y;Z=z; }
    public bool Finite => Num.Finite(X)&&Num.Finite(Y)&&Num.Finite(Z);
    public double Length => Math.Sqrt(X*X+Y*Y+Z*Z);
    public static D3 operator +(D3 a,D3 b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
    public static D3 operator -(D3 a,D3 b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
    public static D3 operator *(D3 a,double b)=>new(a.X*b,a.Y*b,a.Z*b);
    public static D3 operator /(D3 a,double b)=>new(a.X/b,a.Y/b,a.Z/b);
    public static double Dot(D3 a,D3 b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
    public static D3 Cross(D3 a,D3 b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
    public bool Equals(D3 p)=>X==p.X&&Y==p.Y&&Z==p.Z;
    public override bool Equals(object? p)=>p is D3 q&&Equals(q);
    public override int GetHashCode()=>HashCode.Combine(X,Y,Z);
    public override string ToString()=>$"({X:R},{Y:R},{Z:R})";
}
public readonly struct TriangleId : IEquatable<TriangleId>, IComparable<TriangleId>
{
    public readonly long Generation;
    public readonly int Filter,Proof,Submesh,Ordinal;
    public TriangleId(long generation,int filter,int proof,int submesh,int ordinal){Generation=generation;Filter=filter;Proof=proof;Submesh=submesh;Ordinal=ordinal;}
    public bool Valid => Generation>0&&Filter!=0&&Proof>=0&&Submesh>=0&&Ordinal>=0;
    public int CompareTo(TriangleId b){int q=Generation.CompareTo(b.Generation);if(q!=0)return q;q=Filter.CompareTo(b.Filter);if(q!=0)return q;q=Proof.CompareTo(b.Proof);if(q!=0)return q;q=Submesh.CompareTo(b.Submesh);return q!=0?q:Ordinal.CompareTo(b.Ordinal);}
    public bool Equals(TriangleId b)=>CompareTo(b)==0;
    public override bool Equals(object? b)=>b is TriangleId t&&Equals(t);
    public override int GetHashCode()=>HashCode.Combine(Generation,Filter,Proof,Submesh,Ordinal);
    public override string ToString()=>$"{Generation}:{Filter}:{Proof}:{Submesh}:{Ordinal}";
}
public readonly struct SourceTriangle
{
    public readonly TriangleId Id;
    public readonly int AIndex,BIndex,CIndex;
    public readonly D3 A,B,C;
    public readonly bool Proven;
    public SourceTriangle(TriangleId id,D3 a,D3 b,D3 c,bool proven=true,int ai=0,int bi=1,int ci=2){Id=id;A=a;B=b;C=c;Proven=proven;AIndex=ai;BIndex=bi;CIndex=ci;}
    public bool Same(SourceTriangle b)=>Id.Equals(b.Id)&&Proven==b.Proven&&AIndex==b.AIndex&&BIndex==b.BIndex&&CIndex==b.CIndex&&A.Equals(b.A)&&B.Equals(b.B)&&C.Equals(b.C);
}
public enum Family { Earth, Stomp }
public enum Failure { None,InvalidRequest,InvalidInput,StaleGeneration,ConflictingDuplicate,MissingSurface,UnprovedSurface,AmbiguousSurface,InvalidNormal,InvalidBasis,HeightEnvelope,HeightRange,SurfaceGap,LayerOverlap,MissingBirthSurface,GeometryCap,NumericalAmbiguity,InputCap,ProviderException }
public readonly struct Request
{
    public readonly long OwnerId,Generation,Revision;
    public readonly D3 Feet;
    public readonly double HeightH,ShapeScale;
    public readonly Family Family;
    public Request(long ownerId,long generation,long revision,D3 feet,double heightH,Family family,double shapeScale=1){OwnerId=ownerId;Generation=generation;Revision=revision;Feet=feet;HeightH=heightH;Family=family;ShapeScale=shapeScale;}
}
public readonly struct Birth
{
    public readonly TriangleId Source;
    public readonly D3 LocalDeltaH,Barycentric,SurfaceWorld;
    public readonly D2 TangentXZ;
    public Birth(TriangleId source,D3 delta,D3 bary,D3 world,D2 tangent){Source=source;LocalDeltaH=delta;Barycentric=bary;SurfaceWorld=world;TangentXZ=tangent;}
}
public sealed class Capture
{
    public readonly long OwnerId,Generation,Revision;
    public readonly double EffectiveH;
    public readonly D3 Origin,Right,Normal,Forward,WorldUpLocal;
    public readonly IReadOnlyList<D3> Vertices,UnbiasedVertices;
    public readonly IReadOnlyList<D2> UV;
    public readonly IReadOnlyList<int> Indices;
    public readonly IReadOnlyList<TriangleId> FaceSources;
    public readonly IReadOnlyList<SourceTriangle> Sources;
    public readonly IReadOnlyList<Birth> Births;
    internal Capture(Request r,double h,Frame f,List<D3> v,List<D3> u,List<D2> uv,List<int> ix,List<TriangleId> fs,SourceTriangle[] sources,Birth[] births)
    {OwnerId=r.OwnerId;Generation=r.Generation;Revision=r.Revision;EffectiveH=h;Origin=f.O;Right=f.R;Normal=f.N;Forward=f.F;WorldUpLocal=f.LocalVector(new D3(0,1,0));Vertices=Array.AsReadOnly(v.ToArray());UnbiasedVertices=Array.AsReadOnly(u.ToArray());UV=Array.AsReadOnly(uv.ToArray());Indices=Array.AsReadOnly(ix.ToArray());FaceSources=Array.AsReadOnly(fs.ToArray());Sources=Array.AsReadOnly(sources);Births=Array.AsReadOnly(births);}
    public D3 World(D3 local)=>Origin+(Right*local.X+Normal*local.Y+Forward*local.Z)*EffectiveH;
}
internal readonly struct Frame
{
    internal readonly D3 O,R,N,F;
    internal Frame(D3 o,D3 r,D3 n,D3 f){O=o;R=r;N=n;F=f;}
    internal D3 LocalVector(D3 d)=>new(D3.Dot(d,R),D3.Dot(d,N),D3.Dot(d,F));
    internal D3 Local(D3 w,double h)=>LocalVector((w-O)/h);
    internal D3 World(D3 local,double h)=>O+(R*local.X+N*local.Y+F*local.Z)*h;
}
// Optional caller-owned capture scratch. Reuse sequentially; concurrent/reentrant use
// rejects. Returned Capture owns independent arrays and survives any later capture.
public sealed class CaptureWorkspace
{
    internal readonly Dictionary<TriangleId,int> ById;
    internal readonly List<SourceTriangle> All;
    internal int Busy;
    public CaptureWorkspace(int expectedRecords=0)
    {
        if(expectedRecords<0||expectedRecords>GroundCapture.MaxInputRecords)throw new ArgumentOutOfRangeException(nameof(expectedRecords));
        ById=new Dictionary<TriangleId,int>(expectedRecords);All=new List<SourceTriangle>(expectedRecords);
    }
    internal void Clear(){ById.Clear();All.Clear();}
}
public static class GroundCapture
{
    public const int MaxVertices=4096,MaxTriangles=2048,MaxInputRecords=65536;
    public const double HalfX=1.25,HalfZ=.85,MaxHeight=.18,MaxRange=.24,Bias=.0015,MinNormal=.35;
    // No spatial epsilon or position welding: any actual represented gap is unsupported.
    // Value guard tolerance applies ONLY to normal/height/range thresholds, not coverage.
    public const double GuardTolerance=1e-10;
    const double MachineEpsilon=2.2204460492503131e-16;
    static readonly D2[] EarthBirths={new(.27,.06),new(.57,.01),new(.46,.25),new(.72,-.18),new(-.28,.04),new(-.55,-.06),new(-.42,.26),new(-.09,-.44)};
    public static D2[] AuthoredBirths(Family family)
    {
        if(family==Family.Earth)return (D2[])EarthBirths.Clone();
        if(family!=Family.Stomp)return Array.Empty<D2>();
        var b=new D2[12];for(int i=0;i<4;i++){double a=.35+.8*i;b[i]=new(.16*Math.Cos(a),.16*Math.Sin(a));}
        for(int i=0;i<8;i++){double a=.20+.38*i;b[i+4]=new(.14*Math.Cos(a),.14*Math.Sin(a));}return b;
    }
    sealed class Patch
    {
        internal SourceTriangle Source;
        internal List<D3> P=new();
        internal D3 Normal,A,B,C;
        internal double X0,X1,Z0,Z1;
        internal Patch(SourceTriangle t,List<D3> p,D3 n,D3 a,D3 b,D3 c){Source=t;P=p;Normal=n;A=a;B=b;C=c;X0=p.Min(a=>a.X);X1=p.Max(a=>a.X);Z0=p.Min(a=>a.Z);Z1=p.Max(a=>a.Z);}
    }
    // Public API publishes NOTHING on failure. Host must discard previous buffer/lease itself.
    public static bool TryCapture(Request request,IReadOnlyList<SourceTriangle>? records,out Capture? capture,out Failure failure,int vertexCap=MaxVertices,int triangleCap=MaxTriangles,CaptureWorkspace? workspace=null)
    {
        capture=null;failure=Failure.None;bool owned=false;
        try
        {
            if(workspace==null)workspace=new CaptureWorkspace(Math.Min(records?.Count??0,MaxInputRecords));
            if(System.Threading.Interlocked.CompareExchange(ref workspace.Busy,1,0)!=0)return Fail(Failure.InvalidRequest,out failure);
            owned=true;workspace.Clear();
            return TryCaptureCore(request,records,out capture,out failure,vertexCap,triangleCap,workspace);
        }
        catch(Exception e) when(e is not OutOfMemoryException && e is not StackOverflowException)
        { capture=null;failure=Failure.ProviderException;return false; }
        finally { if(owned&&workspace!=null){workspace.Clear();System.Threading.Interlocked.Exchange(ref workspace.Busy,0);} }
    }
    static bool TryCaptureCore(Request request,IReadOnlyList<SourceTriangle>? records,out Capture? capture,out Failure failure,int vertexCap,int triangleCap,CaptureWorkspace workspace)
    {
        capture=null;failure=Failure.None;
        if(request.OwnerId==0||request.Generation<=0||request.Revision<0||!request.Feet.Finite||!Num.Finite(request.HeightH)||request.HeightH<=0||!Num.Finite(request.ShapeScale)||request.ShapeScale<=0||!Enum.IsDefined(typeof(Family),request.Family)||vertexCap<3||vertexCap>MaxVertices||triangleCap<1||triangleCap>MaxTriangles)return Fail(Failure.InvalidRequest,out failure);
        double h=request.HeightH*request.ShapeScale;
        if(!Num.Finite(h)||h<=0||records==null)return Fail(Failure.InvalidRequest,out failure);
        if(records.Count>MaxInputRecords)return Fail(Failure.InputCap,out failure);
        var byId=workspace.ById;var all=workspace.All;
        for(int i=0;i<records.Count;i++)
        {
            var t=records[i];if(!t.Id.Valid||!t.A.Finite||!t.B.Finite||!t.C.Finite||t.AIndex<0||t.BIndex<0||t.CIndex<0||t.AIndex==t.BIndex||t.AIndex==t.CIndex||t.BIndex==t.CIndex)return Fail(Failure.InvalidInput,out failure);
            if(t.Proven&&t.Id.Proof==0)return Fail(Failure.UnprovedSurface,out failure);
            if(t.Id.Generation!=request.Generation)return Fail(Failure.StaleGeneration,out failure);
            if(byId.TryGetValue(t.Id,out int old)){if(!all[old].Same(t))return Fail(Failure.ConflictingDuplicate,out failure);}else {byId.Add(t.Id,all.Count);all.Add(t);}
        }
        all.Sort((a,b)=>a.Id.CompareTo(b.Id));
        if(!Winner(all,request.Feet.X,request.Feet.Z,h,out var anchor,out var point,out _,out failure))return false;
        if(!anchor.Proven)return Fail(Failure.UnprovedSurface,out failure);
        if(!TryNormal(anchor.A,anchor.B,anchor.C,out var n)||n.Y<MinNormal-GuardTolerance)return Fail(Failure.InvalidNormal,out failure);
        D3 right=new D3(1,0,0)-n*n.X;double rl=right.Length;
        if(!Num.Finite(rl)||rl<1e-12)return Fail(Failure.InvalidBasis,out failure);
        right/=rl;var forward=D3.Cross(right,n);var frame=new Frame(point,right,n,forward);
        if(!right.Finite||!forward.Finite||Math.Abs(forward.Length-1)>1e-10)return Fail(Failure.InvalidBasis,out failure);
        // Unknown competitor broadphase includes the transformed full normal-height prism.
        // Its projected AABB is deliberately a superset of the prism polygon.
        double wx0=double.PositiveInfinity,wx1=double.NegativeInfinity,wz0=double.PositiveInfinity,wz1=double.NegativeInfinity;
        foreach(double x in new[]{-HalfX,HalfX})foreach(double y in new[]{-MaxHeight,MaxHeight})foreach(double z in new[]{-HalfZ,HalfZ}){var w=frame.World(new(x,y,z),h);if(!w.Finite)return Fail(Failure.InvalidBasis,out failure);wx0=Math.Min(wx0,w.X);wx1=Math.Max(wx1,w.X);wz0=Math.Min(wz0,w.Z);wz1=Math.Max(wz1,w.Z);}
        var patches=new List<Patch>();
        foreach(var t in all)
        {
            if(!t.Proven)
            {
                if(Outside(t.A,t.B,t.C,wx0,wx1,wz0,wz1))continue;
                var wp=ClipRect(new List<D3>{t.A,t.B,t.C},wx0,wx1,wz0,wz1);
                if(HasArea(wp,out bool unknownUncertain)||unknownUncertain)return Fail(Failure.UnprovedSurface,out failure);
                continue;
            }
            var a=frame.Local(t.A,h);var b=frame.Local(t.B,h);var c=frame.Local(t.C,h);
            if(!a.Finite||!b.Finite||!c.Finite)return Fail(Failure.InvalidInput,out failure);
            if(Outside(a,b,c,-HalfX,HalfX,-HalfZ,HalfZ))continue;
            var p=ClipRect(new List<D3>{a,b,c},-HalfX,HalfX,-HalfZ,HalfZ);
            if(!HasArea(p,out bool uncertain)){if(uncertain)return Fail(Failure.NumericalAmbiguity,out failure);continue;}
            if(!TryNormal(a,b,c,out var ln)||ln.Y<MinNormal-GuardTolerance||!TryNormal(t.A,t.B,t.C,out var wn)||wn.Y<MinNormal-GuardTolerance)return Fail(Failure.InvalidNormal,out failure);
            if(Math.Max(Math.Abs(a.Y),Math.Max(Math.Abs(b.Y),Math.Abs(c.Y)))>MaxHeight+GuardTolerance)return Fail(Failure.HeightEnvelope,out failure);
            patches.Add(new(t,p,ln,a,b,c));
        }
        if(patches.Count==0)return Fail(Failure.SurfaceGap,out failure);
        // Complete topology certificate: no epsilon gaps, no unmatched internal edges,
        // and each rectangle boundary is covered exactly once. Nonconforming T junctions reject.
        if(!Coverage(patches,out failure))return false;
        // Strict convex SAT with error-bounded orientations. Uncertain overlap rejects.
        var sorted=patches.OrderBy(p=>p.X0).ToArray();
        for(int i=0;i<sorted.Length;i++)for(int j=i+1;j<sorted.Length&&sorted[j].X0<sorted[i].X1;j++)
        {
            var a=sorted[i];var b=sorted[j];if(a.Z1<=b.Z0||b.Z1<=a.Z0)continue;
            if(!Separated(a.P,b.P)&&!Separated(b.P,a.P))return Fail(Failure.LayerOverlap,out failure);
        }
        var vertices=new List<D3>();var unbiased=new List<D3>();var uv=new List<D2>();var indices=new List<int>();var faceSources=new List<TriangleId>();var usedSources=new List<SourceTriangle>();
        var vertexMap=new Dictionary<(D3,D3),int>();
        double low=double.PositiveInfinity,high=double.NegativeInfinity;
        foreach(var patch in patches)
        {
            bool used=false;
            for(int k=1;k+1<patch.P.Count;k++)
            {
                D3 a=patch.P[0],b=patch.P[k],c=patch.P[k+1];int s=Orient(a,b,c);
                if(s==0)continue;if(s==2)return Fail(Failure.NumericalAmbiguity,out failure);
                if(s>0)return Fail(Failure.InvalidNormal,out failure);
                for(int j=0;j<3;j++)
                {
                    var p=j==0?a:j==1?b:c;var biased=p+patch.Normal*Bias;if(!biased.Finite)return Fail(Failure.InvalidInput,out failure);
                    if(!vertexMap.TryGetValue((p,biased),out int ix)){ix=vertices.Count;vertexMap.Add((p,biased),ix);vertices.Add(biased);unbiased.Add(p);uv.Add(new(p.X/(2*HalfX)+.5,p.Z/(2*HalfZ)+.5));low=Math.Min(low,biased.Y);high=Math.Max(high,biased.Y);}
                    indices.Add(ix);
                }
                faceSources.Add(patch.Source.Id);used=true;
                if(vertices.Count>vertexCap||faceSources.Count>triangleCap)return Fail(Failure.GeometryCap,out failure);
            }
            if(used)usedSources.Add(patch.Source);
        }
        if(faceSources.Count==0)return Fail(Failure.SurfaceGap,out failure);
        if(high-low>MaxRange+GuardTolerance)return Fail(Failure.HeightRange,out failure);
        var authored=AuthoredBirths(request.Family);var births=new Birth[authored.Length];
        for(int i=0;i<authored.Length;i++)
        {
            var b=authored[i];var tangent=frame.World(new(b.X,0,b.Y),h);
            if(!tangent.Finite)return Fail(Failure.InvalidInput,out failure);
            if(!Winner(all,tangent.X,tangent.Z,h,out var source,out var world,out var bary,out var birthFailure))return Fail(birthFailure==Failure.MissingSurface?Failure.MissingBirthSurface:birthFailure,out failure);
            if(!source.Proven)return Fail(Failure.UnprovedSurface,out failure);
            if(!TryNormal(source.A,source.B,source.C,out var bn)||bn.Y<MinNormal-GuardTolerance||D3.Dot(bn,n)<MinNormal-GuardTolerance)return Fail(Failure.InvalidNormal,out failure);
            var delta=frame.LocalVector((world-tangent)/h);if(!delta.Finite||Math.Abs(frame.Local(world,h).Y)>MaxHeight+GuardTolerance)return Fail(Failure.HeightEnvelope,out failure);
            births[i]=new(source.Id,delta,bary,world,b);
        }
        capture=new Capture(request,h,frame,vertices,unbiased,uv,indices,faceSources,usedSources.ToArray(),births);return true;
    }
    static bool Fail(Failure f,out Failure failure){failure=f;return false;}
    static bool TryNormal(D3 a,D3 b,D3 c,out D3 normal)
    {
        var u=b-a;var v=c-a;double m=Math.Max(Math.Max(Math.Abs(u.X),Math.Max(Math.Abs(u.Y),Math.Abs(u.Z))),Math.Max(Math.Abs(v.X),Math.Max(Math.Abs(v.Y),Math.Abs(v.Z))));
        normal=default;if(!Num.Finite(m)||m==0)return false;var n=D3.Cross(u/m,v/m);double length=n.Length;if(!Num.Finite(length)||length<1e-15)return false;normal=n/length;return normal.Finite;
    }
    // Sign of the exact determinant of supplied doubles when arithmetic certifies it.
    // 2 = uncertain, never treated as zero. Structural equality/axis lines prove exact zero.
    internal static int Orient(D3 a,D3 b,D3 p)
    {
        if((a.X==p.X&&a.Z==p.Z)||(b.X==p.X&&b.Z==p.Z)||(a.X==b.X&&p.X==a.X)||(a.Z==b.Z&&p.Z==a.Z))return 0;
        double x=b.X-a.X,z=b.Z-a.Z,u=p.X-a.X,v=p.Z-a.Z,l=x*v,r=z*u,d=l-r;
        double error=(Math.Abs(l)+Math.Abs(r))*16*MachineEpsilon;
        if(!Num.Finite(d)||!Num.Finite(error))return 2;
        if(Math.Abs(d)>error)return d>0?1:-1;
        return ExactOrientation(a,b,p);
    }
    // Adaptive error-free double expansions only for otherwise uncertain orientation.
    // This is not rational clipping. Coordinates outside a deliberately narrow safe
    // exponent range reject; it avoids underflow/overflow in splitter products.
    static int ExactOrientation(D3 a,D3 b,D3 p)
    {
        foreach(double d in new[]{a.X,a.Z,b.X,b.Z,p.X,p.Z})
            if(d!=0&&(Math.Abs(d)<1e-50||Math.Abs(d)>1e50))return 2;
        var ax=Diff(b.X,a.X);var az=Diff(b.Z,a.Z);var px=Diff(p.X,a.X);var pz=Diff(p.Z,a.Z);
        var sum=new List<double>();
        foreach(double x in ax)foreach(double y in pz){Product(x,y,out double hi,out double lo);Grow(sum,lo);Grow(sum,hi);}
        foreach(double x in az)foreach(double y in px){Product(x,y,out double hi,out double lo);Grow(sum,-lo);Grow(sum,-hi);}
        for(int i=sum.Count-1;i>=0;i--)if(sum[i]!=0)return sum[i]>0?1:-1;return 0;
    }
    static double[] Diff(double a,double b)
    {
        double x=a-b,bv=a-x,av=x+bv,br=bv-b,ar=a-av,y=ar+br;return new[]{y,x};
    }
    static void Product(double a,double b,out double hi,out double lo)
    {
        const double splitter=134217729;
        hi=a*b;double c=splitter*a,abig=c-a,ahi=c-abig,alo=a-ahi;
        c=splitter*b;double bbig=c-b,bhi=c-bbig,blo=b-bhi;
        double e1=hi-ahi*bhi,e2=e1-alo*bhi,e3=e2-ahi*blo;lo=alo*blo-e3;
    }
    static void Grow(List<double> e,double b)
    {
        if(b==0)return;double q=b;int n=e.Count,write=0;
        for(int i=0;i<n;i++){double a=e[i],x=q+a,bv=x-q,av=x-bv,br=a-bv,ar=q-av,tail=ar+br;if(tail!=0)e[write++]=tail;q=x;}
        if(write<n)e.RemoveRange(write,n-write);if(q!=0||e.Count==0)e.Add(q);
    }
    static bool HasArea(List<D3> p,out bool uncertain)
    {
        uncertain=false;if(p.Count<3)return false;
        for(int i=1;i+1<p.Count;i++){int s=Orient(p[0],p[i],p[i+1]);if(s==1||s==-1)return true;if(s==2)uncertain=true;}return false;
    }
    static bool Outside(D3 a,D3 b,D3 c,double x0,double x1,double z0,double z1)=>Math.Max(a.X,Math.Max(b.X,c.X))<=x0||Math.Min(a.X,Math.Min(b.X,c.X))>=x1||Math.Max(a.Z,Math.Max(b.Z,c.Z))<=z0||Math.Min(a.Z,Math.Min(b.Z,c.Z))>=z1;
    static List<D3> ClipRect(List<D3> p,double x0,double x1,double z0,double z1)
    {p=Clip(p,0,x0,true);p=Clip(p,0,x1,false);p=Clip(p,2,z0,true);return Clip(p,2,z1,false);}
    static double Axis(D3 a,int axis)=>axis==0?a.X:a.Z;
    static int Compare(D3 a,D3 b){int q=a.X.CompareTo(b.X);if(q!=0)return q;q=a.Y.CompareTo(b.Y);return q!=0?q:a.Z.CompareTo(b.Z);}
    static D3 Intersect(D3 a,D3 b,int axis,double bound)
    {
        // Same input edge, either orientation, produces bit-identical output.
        if(Compare(a,b)>0)(a,b)=(b,a);double t=(bound-Axis(a,axis))/(Axis(b,axis)-Axis(a,axis));var p=a+(b-a)*t;
        return axis==0?new D3(bound,p.Y,p.Z):new D3(p.X,p.Y,bound);
    }
    static List<D3> Clip(List<D3> p,int axis,double bound,bool lower)
    {
        if(p.Count==0)return p;int inside=0;foreach(var a in p)if(lower?Axis(a,axis)>=bound:Axis(a,axis)<=bound)inside++;
        if(inside==p.Count)return p;var o=new List<D3>(p.Count+1);if(inside==0)return o;
        for(int i=0;i<p.Count;i++){var a=p[i];var b=p[(i+1)%p.Count];bool ai=lower?Axis(a,axis)>=bound:Axis(a,axis)<=bound;bool bi=lower?Axis(b,axis)>=bound:Axis(b,axis)<=bound;if(ai)AppendUnique(o,a);if(ai!=bi)AppendUnique(o,Intersect(a,b,axis,bound));}
        if(o.Count>1&&o[0].Equals(o[^1]))o.RemoveAt(o.Count-1);return o;
    }
    static void AppendUnique(List<D3> p,D3 v){if(p.Count==0||!p[^1].Equals(v))p.Add(v);}
    static bool Separated(List<D3> a,List<D3> b)
    {
        // All admitted polygons have clockwise XZ orientation (upward 3D normal).
        for(int i=0;i<a.Count;i++){bool outside=true;foreach(var p in b){int s=Orient(a[i],a[(i+1)%a.Count],p);if(s==-1||s==2){outside=false;break;}}if(outside)return true;}return false;
    }
    // Interior adjacency must also name the SAME unmodified world-space source
    // edge. Rounded local projection coincidence cannot weld distinct world edges.
    static bool OriginalEdge(Patch p,D3 a,D3 b,out D3 worldA,out D3 worldB)
    {
        worldA=default;worldB=default;
        for(int i=0;i<3;i++)
        {
            D3 u=i==0?p.A:i==1?p.B:p.C,v=i==0?p.B:i==1?p.C:p.A;
            if(!ClipSegment(ref u,ref v,0,-HalfX,true)||!ClipSegment(ref u,ref v,0,HalfX,false)||!ClipSegment(ref u,ref v,2,-HalfZ,true)||!ClipSegment(ref u,ref v,2,HalfZ,false))continue;
            if(!(a.Equals(u)&&b.Equals(v))&&!(a.Equals(v)&&b.Equals(u)))continue;
            worldA=i==0?p.Source.A:i==1?p.Source.B:p.Source.C;worldB=i==0?p.Source.B:i==1?p.Source.C:p.Source.A;
            if(Compare(worldA,worldB)>0)(worldA,worldB)=(worldB,worldA);return true;
        }
        return false;
    }
    static bool ClipSegment(ref D3 a,ref D3 b,int axis,double bound,bool lower)
    {
        bool ai=lower?Axis(a,axis)>=bound:Axis(a,axis)<=bound,bi=lower?Axis(b,axis)>=bound:Axis(b,axis)<=bound;
        if(!ai&&!bi)return false;if(ai!=bi){var p=Intersect(a,b,axis,bound);if(ai)b=p;else a=p;}return true;
    }
    static bool Coverage(List<Patch> patches,out Failure failure)
    {
        failure=Failure.None;var inside=new Dictionary<(D3,D3),(int count,int direction,D3 sourceA,D3 sourceB)>();var sides=new List<(double a,double b)>[4];for(int i=0;i<4;i++)sides[i]=new();
        foreach(var patch in patches)for(int i=0;i<patch.P.Count;i++)
        {
            D3 a=patch.P[i],b=patch.P[(i+1)%patch.P.Count];if(a.Equals(b))continue;
            int side=a.X==-HalfX&&b.X==-HalfX?0:a.X==HalfX&&b.X==HalfX?1:a.Z==-HalfZ&&b.Z==-HalfZ?2:a.Z==HalfZ&&b.Z==HalfZ?3:-1;
            if(side>=0){double u=side<2?a.Z:a.X,v=side<2?b.Z:b.X;if(u==v)continue;sides[side].Add((Math.Min(u,v),Math.Max(u,v)));}
            else
            {
                int sign=Compare(a,b)<0?1:-1;var key=sign==1?(a,b):(b,a);
                if(!OriginalEdge(patch,a,b,out var sourceA,out var sourceB))return Fail(Failure.NumericalAmbiguity,out failure);
                if(inside.TryGetValue(key,out var value)&&(!value.sourceA.Equals(sourceA)||!value.sourceB.Equals(sourceB)))return Fail(Failure.SurfaceGap,out failure);
                inside[key]=(value.count+1,value.direction+sign,sourceA,sourceB);
            }
        }
        foreach(var e in inside.Values)if(e.count!=2||e.direction!=0)return Fail(e.count>2||Math.Abs(e.direction)>1?Failure.LayerOverlap:Failure.SurfaceGap,out failure);
        for(int i=0;i<4;i++)
        {
            sides[i].Sort((a,b)=>a.a.CompareTo(b.a));double cursor=i<2?-HalfZ:-HalfX,end=-cursor;
            foreach(var s in sides[i]){if(s.a!=cursor)return Fail(s.a<cursor?Failure.LayerOverlap:Failure.SurfaceGap,out failure);cursor=s.b;}
            if(cursor!=end)return Fail(Failure.SurfaceGap,out failure);
        }
        return true;
    }
    static bool Winner(List<SourceTriangle> all,double x,double z,double h,out SourceTriangle winner,out D3 world,out D3 bary,out Failure failure)
    {
        winner=default;world=default;bary=default;failure=Failure.None;bool found=false;double best=double.NegativeInfinity;
        foreach(var t in all)
        {
            if(x<Math.Min(t.A.X,Math.Min(t.B.X,t.C.X))||x>Math.Max(t.A.X,Math.Max(t.B.X,t.C.X))||z<Math.Min(t.A.Z,Math.Min(t.B.Z,t.C.Z))||z>Math.Max(t.A.Z,Math.Max(t.B.Z,t.C.Z)))continue;
            var p=new D3(x,0,z);int orientation=Orient(t.A,t.B,t.C);if(orientation==0)continue;if(orientation==2)return Fail(Failure.NumericalAmbiguity,out failure);
            int s0=Orient(t.A,t.B,p),s1=Orient(t.B,t.C,p),s2=Orient(t.C,t.A,p);
            if((s0!=2&&s0!=0&&s0!=orientation)||(s1!=2&&s1!=0&&s1!=orientation)||(s2!=2&&s2!=0&&s2!=orientation))continue;
            // Adaptive expansion handles exact diagonal seams; unresolved numeric cases reject.
            if(s0==2||s1==2||s2==2)return Fail(Failure.NumericalAmbiguity,out failure);
            double det=(t.B.Z-t.C.Z)*(t.A.X-t.C.X)+(t.C.X-t.B.X)*(t.A.Z-t.C.Z);
            if(!Num.Finite(det)||det==0)return Fail(Failure.NumericalAmbiguity,out failure);
            double u=((t.B.Z-t.C.Z)*(x-t.C.X)+(t.C.X-t.B.X)*(z-t.C.Z))/det,v=((t.C.Z-t.A.Z)*(x-t.C.X)+(t.A.X-t.C.X)*(z-t.C.Z))/det,w=1-u-v;
            var b=new D3(u,v,w);double y=u*t.A.Y+v*t.B.Y+w*t.C.Y;if(!Num.Finite(y)||!b.Finite)return Fail(Failure.InvalidInput,out failure);
            if(found&&Math.Abs(y-best)<=GuardTolerance*h)
            {
                if(!t.Proven||!winner.Proven)return Fail(Failure.UnprovedSurface,out failure);
                if(!TryNormal(t.A,t.B,t.C,out var n1)||!TryNormal(winner.A,winner.B,winner.C,out var n2)||D3.Dot(n1,n2)<1-1e-10)return Fail(Failure.AmbiguousSurface,out failure);
                // IDs are sorted: use lower canonical ID. Almost-equal different layers
                // cannot be admitted because subsequent full-domain overlap check rejects.
                continue;
            }
            if(!found||y>best){found=true;best=y;winner=t;world=new(x,y,z);bary=b;}
        }
        return found||Fail(Failure.MissingSurface,out failure);
    }
}

}
