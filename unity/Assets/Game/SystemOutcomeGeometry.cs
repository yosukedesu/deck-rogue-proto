using System;
namespace DeckRogue.Game {
 // Receipt-only resource response. Deterministic geometry, no gameplay/RNG or predictive windup.
 internal static class SystemOutcomeGeometry {
  public struct Point {public double X,Y,Z;public Point(double x,double y,double z){X=x;Y=y;Z=z;}}
  public struct Uv {public double X,Y;public Uv(double x,double y){X=x;Y=y;}}
  public sealed class MeshData {public int Kind;public Point[] Vertices;public Uv[] UV;public double[] Alpha,Gain,Phase;public int[] Role,Triangles;}
  public sealed class Buffer {public string Family;public MeshData[] Batches;}
  static readonly double[,] Ctrl={{-.17,.15},{0,.205},{.17,.15},{.145,-.055},{0,-.245},{-.145,-.055}};
  static double Clamp(double x)=>Math.Max(0,Math.Min(1,x));
  static double Smooth(double x){double q=Clamp(x);return q*q*(3-2*q);}
  static double Fade(double t,double hold,double end)=>t>=0 && t<end?1-Smooth((t-hold)/(end-hold)):0;
  public static double End(string family)=>SystemSelfLossGeometry.IsFamily(family)?SystemSelfLossGeometry.End(family):SystemImpactGeometry.IsFamily(family)?SystemImpactGeometry.End(family):family=="guard"?.180:.212;
  public static double Light(string family,double t)=>SystemSelfLossGeometry.IsFamily(family)?0:SystemImpactGeometry.IsFamily(family)?SystemImpactGeometry.Light(family,t):family=="guard"?Fade(t,.010,.065)*.33:0;
  static MeshData Mesh(int vertices,int triangles,int kind)=>new MeshData{Kind=kind,Vertices=new Point[vertices],UV=new Uv[vertices],Alpha=new double[vertices],Gain=new double[vertices],Role=new int[vertices],Triangles=new int[triangles*3]};
  static void Quad(MeshData m,int index,int a,int b,int c,int d){int k=index*6;m.Triangles[k]=a;m.Triangles[k+1]=b;m.Triangles[k+2]=c;m.Triangles[k+3]=a;m.Triangles[k+4]=c;m.Triangles[k+5]=d;}
  public static Buffer Create(string family){
   if(SystemSelfLossGeometry.IsFamily(family))return SystemSelfLossGeometry.Create(family);
   if(SystemImpactGeometry.IsFamily(family))return SystemImpactGeometry.Create(family);
   if(family!="guard" && family!="nullified")throw new ArgumentException("Unknown resource response",nameof(family));
   var b=new Buffer{Family=family,Batches=family=="guard"?new[]{Mesh(195,256,1),Mesh(65,64,2),Mesh(24,12,0)}:new[]{Mesh(150,144,3),Mesh(64,32,0)}};
   if(family=="guard"){
    for(int i=0;i<64;i++)for(int j=0;j<2;j++)Quad(b.Batches[0],i*2+j,i*3+j,i*3+j+1,(i+1)*3+j+1,(i+1)*3+j);
    for(int i=0;i<64;i++){b.Batches[1].Triangles[i*3]=0;b.Batches[1].Triangles[i*3+1]=i+1;b.Batches[1].Triangles[i*3+2]=(i+1)%64+1;}
    for(int i=0;i<6;i++)Quad(b.Batches[2],i,i*4,i*4+1,i*4+2,i*4+3);
   }else{
    for(int k=0;k<3;k++)for(int i=0;i<24;i++)Quad(b.Batches[0],k*24+i,k*50+i*2,k*50+i*2+1,k*50+(i+1)*2+1,k*50+(i+1)*2);
    for(int i=0;i<16;i++)Quad(b.Batches[1],i,i*4,i*4+1,i*4+2,i*4+3);
   }return b;
  }
  static void Vertex(MeshData m,int i,double x,double y,double z,double alpha,double gain,int role=0){m.Vertices[i]=new Point(x,y,z);m.Alpha[i]=alpha;m.Gain[i]=gain;m.Role[i]=role;}
  static void Diamond(MeshData m,int offset,double x,double y,double z,double length,double width,double angle,double alpha,int role,double gain){
   double c=Math.Cos(angle),s=Math.Sin(angle);for(int i=0;i<4;i++){double px=(i==0?-.5:i==2?.5:0)*length,py=(i==1?-.5:i==3?.5:0)*width;Vertex(m,offset+i,x+c*px-s*py,y+s*px+c*py,z,alpha,gain,role);}
  }
  static void Perimeter(double u,out double x,out double y){
   double v=(u-Math.Floor(u))*6;int k=(int)v;double q=v-k,q2=q*q,q3=q2*q;
   x=Curve(Ctrl[(k+5)%6,0],Ctrl[k%6,0],Ctrl[(k+1)%6,0],Ctrl[(k+2)%6,0],q,q2,q3);
   y=Curve(Ctrl[(k+5)%6,1],Ctrl[k%6,1],Ctrl[(k+1)%6,1],Ctrl[(k+2)%6,1],q,q2,q3);
  }
  static double Curve(double a,double b,double c,double d,double q,double q2,double q3)=>.5*(2*b+(-a+c)*q+(2*a-5*b+4*c-d)*q2+(-a+3*b-3*c+d)*q3);
  static void Guard(Buffer b,double t){
   double a=Fade(t,.025,.180),at=Math.Max(t,0),deflect=.025*Math.Exp(-at/.042)*Math.Sin(at*Math.PI/.045);
   var edge=b.Batches[0];var face=b.Batches[1];
   Vertex(face,0,deflect-.025,-.01,.040,a*.15,.72);face.UV[0]=new Uv(.5,.5);
   for(int i=0;i<65;i++){
    double u=i/64.0;Perimeter(u,out var x,out var y);double dist=Math.Min(Math.Abs(u-.87),1-Math.Abs(u-.87));x+=deflect+.014*Math.Exp(-Math.Pow(dist/.075,2))*Math.Exp(-at/.033)-.025;
    Perimeter(u-.001,out var x0,out var y0);Perimeter(u+.001,out var x1,out var y1);double dx=x1-x0,dy=y1-y0,len=Math.Sqrt(dx*dx+dy*dy),w=.0078*(.82+.18*Math.Sin(u*Math.PI*6+.2));
    for(int j=0;j<3;j++){double side=j-1;Vertex(edge,i*3+j,x-dy/len*w*side,y+dx/len*w*side,.020+.005*(1-side*side),a*.92,1.45);edge.UV[i*3+j]=new Uv(u,(side+1)/2);}
    if(i<64){Vertex(face,i+1,x,y,.020,a*.15,.72);face.UV[i+1]=new Uv(.5+x/.4,.5+y/.52);}
   }
   Perimeter(.87,out var px,out var py);px-=.025;var solid=b.Batches[2];
   Diamond(solid,0,px+deflect,py,.046,.070,.009,.32,Fade(t,.008,.044),2,2.1);
   for(int i=0;i<5;i++){
    double angle=(i-2)*.3+Math.PI*.52,speed=.65+.14*i,q=(1-Math.Exp(-7*at))/7,alpha=Smooth(t/.003)*Fade(t,0,.115);
    Diamond(solid,(i+1)*4,px+Math.Cos(angle)*speed*q,py+Math.Sin(angle)*speed*q-.8*at*at,.044,.022+.003*(i%2),.004,angle,alpha,1,1.3);
   }
  }
  static void Strand(int i,double u,out double x,out double y,out double z){x=-.21+.38*u;y=(i-1)*.062+.032*Math.Sin(Math.PI*u+i*.7);z=.023+.003*i;}
  static void Nullify(Buffer b,double t){
   double a=Fade(t,.030,.185),age=Math.Max(t,0);var traces=b.Batches[0];
   for(int k=0;k<3;k++)for(int i=0;i<25;i++){
    double u=i/24.0;Strand(k,u,out var x,out var y,out var z);x+=.007*Math.Sin(age*24+k)*Smooth(age/.03);y+=(k-1)*.16*age;
    double w=(k==1?.0095:.0068)*Math.Pow(Math.Sin(Math.PI*u),.55)*(1-.20*Smooth(age/.16));
    for(int j=0;j<2;j++){int n=k*50+i*2+j;Vertex(traces,n,x-.34,y+(j==0?-w:w),z,a*.96,1.28);traces.UV[n]=new Uv(u,j);}
   }
   for(int i=0;i<16;i++){
    double born=.006*(i%4),at=Math.Max(0,t-born),life=.178+.008*(i%3),alpha=Smooth((t-born)/.006)*Fade(t-born,.065,life);
    Strand(i%3,Math.Min(.95,.12+.053*i),out var x,out var y,out var z);double angle=(i%2==1?Math.PI/2:-Math.PI/2)+.24*Math.Sin(i*1.9),speed=.7+.08*(i%4),q=(1-Math.Exp(-5*at))/5;
    Diamond(b.Batches[1],i*4,x+Math.Cos(angle)*speed*q-.34,y+Math.Sin(angle)*speed*q,z+.013,(.023+.006*(i%4))*(1-.35*Clamp(at/life)),.0065,angle+at*(i%2==1?6:-5),alpha,i%4==0?2:1,1.18);
   }
  }
  public static void Sample(Buffer b,double t){if(SystemSelfLossGeometry.IsFamily(b.Family)){SystemSelfLossGeometry.Sample(b,t);return;}if(SystemImpactGeometry.IsFamily(b.Family))SystemImpactGeometry.Sample(b,t);else if(b.Family=="guard")Guard(b,t);else Nullify(b,t);}
  // Apply once after Sample. Source is target-relative in captured camera XY/H.
  // Degenerate direction explicitly uses camera-left incoming; no socket is inferred.
  public static bool FitIncoming(Buffer b,double sourceX,double sourceY){
   if(SystemSelfLossGeometry.IsFamily(b.Family))return true;
   if(SystemImpactGeometry.IsFamily(b.Family))return SystemImpactGeometry.FitReflection(b,sourceX,sourceY);
   double len=Math.Sqrt(sourceX*sourceX+sourceY*sourceY);bool valid=!double.IsNaN(len)&&!double.IsInfinity(len)&&len>1e-5;
   double c=valid?-sourceX/len:1,s=valid?-sourceY/len:0,mirror=valid&&sourceX>0?-1:1;
   foreach(var m in b.Batches)for(int i=0;i<m.Vertices.Length;i++){var p=m.Vertices[i];m.Vertices[i]=b.Family=="guard"?new Point(p.X*mirror,p.Y,p.Z):new Point(c*p.X-s*p.Y,s*p.X+c*p.Y,p.Z);}
   return valid;
  }
 }
}
