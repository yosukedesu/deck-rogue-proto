using System;
using Point=DeckRogue.Game.SystemOutcomeGeometry.Point;
using Uv=DeckRogue.Game.SystemOutcomeGeometry.Uv;
using MeshData=DeckRogue.Game.SystemOutcomeGeometry.MeshData;
using Buffer=DeckRogue.Game.SystemOutcomeGeometry.Buffer;
namespace DeckRogue.Game {
 // Two positive-loss system responses. Shape parts are not gameplay hits.
 internal static class SystemImpactGeometry {
  static double Clamp(double x)=>Math.Max(0,Math.Min(1,x));
  static double Smooth(double x){double q=Clamp(x);return q*q*(3-2*q);}
  static double Fade(double t,double h,double end)=>t>=0&&t<end?1-Smooth((t-h)/(end-h)):0;
  public static bool IsFamily(string family)=>family=="reflect"||family=="burn";
  public static double End(string family)=>family=="reflect"?.140:.238;
  public static double Light(string family,double t)=>family=="reflect"?.12*Fade(t,.008,.055):.14*Fade(t,.016,.070);
  static MeshData Mesh(int n,int tris,int kind)=>new MeshData{Kind=kind,Vertices=new Point[n],UV=new Uv[n],Alpha=new double[n],Gain=new double[n],Role=new int[n],Triangles=new int[tris*3]};
  static void Quad(MeshData m,int i,int a,int b,int c,int d){int j=i*6;m.Triangles[j]=a;m.Triangles[j+1]=b;m.Triangles[j+2]=c;m.Triangles[j+3]=a;m.Triangles[j+4]=c;m.Triangles[j+5]=d;}
  static void Vertex(MeshData m,int i,double x,double y,double z,double alpha,double gain,int role=0){m.Vertices[i]=new Point(x,y,z);m.Alpha[i]=alpha;m.Gain[i]=gain;m.Role[i]=role;}
  static void Piece(MeshData m,int start,double x,double y,double z,double length,double width,double angle,double alpha,int role,double gain){double c=Math.Cos(angle),s=Math.Sin(angle);for(int i=0;i<4;i++){double a=(i==0?-.5:i==2?.5:0)*length,b=(i==1?-.5:i==3?.5:0)*width;Vertex(m,start+i,x+c*a-s*b,y+s*a+c*b,z,alpha,gain,role);}}
  public static Buffer Create(string family){
   if(!IsFamily(family))throw new ArgumentException("Unknown positive-loss system family",nameof(family));
   bool reflect=family=="reflect";var b=new Buffer{Family=family,Batches=reflect?new[]{Mesh(100,96,4),Mesh(15,9,5),Mesh(24,12,0)}:new[]{Mesh(261,336,6),Mesh(28,14,0)}};
   if(reflect){
    for(int k=0;k<2;k++)for(int i=0;i<24;i++)Quad(b.Batches[0],k*24+i,k*50+i*2,k*50+i*2+1,k*50+(i+1)*2+1,k*50+(i+1)*2);
    for(int k=0;k<3;k++)for(int i=0;i<3;i++){int j=(k*3+i)*3;b.Batches[1].Triangles[j]=k*5;b.Batches[1].Triangles[j+1]=k*5+i+1;b.Batches[1].Triangles[j+2]=k*5+i+2;}
    for(int i=0;i<6;i++)Quad(b.Batches[2],i,i*4,i*4+1,i*4+2,i*4+3);
   }else{for(int k=0;k<3;k++)for(int i=0;i<28;i++)for(int j=0;j<2;j++)Quad(b.Batches[0],k*56+i*2+j,k*87+i*3+j,k*87+i*3+j+1,k*87+(i+1)*3+j+1,k*87+(i+1)*3+j);for(int i=0;i<7;i++)Quad(b.Batches[1],i,i*4,i*4+1,i*4+2,i*4+3);}
   return b;
  }
  static void Reflect(Buffer b,double t){
   double age=Math.Max(0,t),a=Fade(t,.006,.090),tail=-.43+.37*Smooth(age/.080);var trace=b.Batches[0];
   for(int k=0;k<2;k++)for(int i=0;i<25;i++){
    double u=i/24.0,x=tail+(-.025-tail)*u,y=(k*2-1)*(.023+.028*Math.Sin(u*Math.PI))*(1-u),w=(k==0?.0055:.0035)*Math.Pow(Math.Sin(Math.PI*u),.6);
    for(int j=0;j<2;j++){int n=k*50+i*2+j;Vertex(trace,n,x,y+(j==0?-w:w),.022+k*.004,a,1.15);trace.UV[n]=new Uv(u,j);}
   }
   for(int k=0;k<3;k++){
    double angle=k==0?-.28:k==1?.02:.30,L=k==0?.075:k==1?.16:.058,x=(k==0?-.08:k==1?-.008:.04)+age*.24,y=k==0?-.055:k==1?.004:.07,w=k==0?.009:k==1?.018:.008,c=Math.Cos(angle),s=Math.Sin(angle);
    for(int j=0;j<5;j++){
     double px=j==0?-L:j==1?-.10*L:j==2?.035:j==3?-.22*L:-.72*L,py=j==0||j==2?0:j==1?-w:j==3?w*.65:w*.25;
     int n=k*5+j;Vertex(b.Batches[1],n,x+c*px-s*py,y+s*px+c*py,.033+k*.002,Fade(t,.010,.078),.98);
     b.Batches[1].UV[n]=j==0?new Uv(0,.5):j==1?new Uv(.7,0):j==2?new Uv(1,.5):j==3?new Uv(.65,1):new Uv(.2,.7);
    }
   }
   for(int i=0;i<6;i++){double angle=(i-2.5)*.39,speed=.55+.12*(i%3),q=(1-Math.Exp(-7*age))/7;Piece(b.Batches[2],i*4,.018+Math.Cos(angle)*speed*q,.005+Math.Sin(angle)*speed*q-.3*age*age,.05,.018+.004*(i%3),.0045,angle+age*3,Smooth(t/.004)*Fade(t,.005,.140),1,1.3);}
  }
  static void Burn(Buffer b,double t){
   var body=b.Batches[0];
   for(int k=0;k<3;k++){
    double born=k==0?.035:k==1?0:.055,local=t-born,age=Math.Max(local,0),life=k==1?.200:.145,alpha=(k==1?.24+.76*Smooth(local/.014):Smooth(local/.014))*Fade(local,.025,life);
    double L=(k==0?.15:k==1?.28:.185)*(.65+.35*Smooth(age/.035)),cx=k==0?-.070:k==1?.003:.073,cy=k==0?-.10:k==1?-.15:-.115,phase=k*1.7;
    for(int i=0;i<29;i++){
     double u=i/28.0,y=cy+L*u+age*(k==0?.28:k==1?.36:.42),x=cx+.049*Math.Sin(6*u-(k==0?19:k==1?15:23)*age+phase)*u*u+.014*Math.Sin(11*age+phase)*u;
     double profile=Math.Pow(Math.Sin(Math.PI*u),.35)*Math.Pow(1-u,1.15),width=(k==0?.055:k==1?.078:.048)*profile*(1-.35*Smooth(age/life));
     for(int j=0;j<3;j++){double side=j-1,asym=side<0?.85+.15*Math.Sin(4*u+phase):1.06+.10*Math.Sin(5*u+phase);int n=k*87+i*3+j;Vertex(body,n,x+side*width*asym,y,.024+k*.004+.005*(1-side*side),alpha*.90,1.5);body.UV[n]=new Uv(u,(side+1)/2);}
    }
   }
   for(int i=0;i<7;i++){double born=.006*(i%3),age=Math.Max(0,t-born),life=.176+.010*i,phi=i*2.39996,cx=.07*Math.Sin(phi),cy=-.06+.055*Math.Cos(phi),vx=.16*Math.Sin(phi),vy=.65+.045*i,q=(1-Math.Exp(-2.5*age))/2.5;Piece(b.Batches[1],i*4,cx+vx*q,cy+vy*q,.04,.012+.003*(i%2),.005,Math.PI/2+.25*Math.Sin(phi),Smooth((t-born)/.006)*Fade(t-born,.016,life),i%3==0?2:1,1.5);}
  }
  public static void Sample(Buffer b,double t){if(b.Family=="reflect")Reflect(b,t);else Burn(b,t);}
  // Once after Sample. Missing source omits only the return residue, not the legitimate contact.
  public static bool FitReflection(Buffer b,double sourceX,double sourceY){
   if(b.Family!="reflect")return true;double length=Math.Sqrt(sourceX*sourceX+sourceY*sourceY);bool valid=!double.IsNaN(length)&&!double.IsInfinity(length)&&length>1e-5;
   if(!valid){Array.Clear(b.Batches[0].Alpha,0,b.Batches[0].Alpha.Length);return false;}
   double c=-sourceX/length,s=-sourceY/length;foreach(var m in b.Batches)for(int i=0;i<m.Vertices.Length;i++){var p=m.Vertices[i];m.Vertices[i]=new Point(c*p.X-s*p.Y,s*p.X+c*p.Y,p.Z);}return true;
  }
 }
}
