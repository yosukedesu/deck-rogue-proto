using System;
using Point=DeckRogue.Game.SystemOutcomeGeometry.Point;
using Uv=DeckRogue.Game.SystemOutcomeGeometry.Uv;
using MeshData=DeckRogue.Game.SystemOutcomeGeometry.MeshData;
using Buffer=DeckRogue.Game.SystemOutcomeGeometry.Buffer;
namespace DeckRogue.Game {
 // Local loss/affliction; geometry fragments are not extra HP receipts or attacks.
 internal static class SystemSelfLossGeometry {
  static double Clamp(double x)=>Math.Max(0,Math.Min(1,x));
  static double Smooth(double x){double q=Clamp(x);return q*q*(3-2*q);}
  static double Fade(double t,double h,double end)=>t>=0&&t<end?1-Smooth((t-h)/(end-h)):0;
  public static bool IsFamily(string family)=>family=="selfloss"||family=="affliction";
  public static double End(string family)=>family=="selfloss"?.182:.200;
  static MeshData Mesh(int n,int tris,int kind)=>new MeshData{Kind=kind,Vertices=new Point[n],UV=new Uv[n],Alpha=new double[n],Gain=new double[n],Phase=new double[n],Role=new int[n],Triangles=new int[tris*3]};
  static void Quad(MeshData m,int i,int a,int b,int c,int d){int j=i*6;m.Triangles[j]=a;m.Triangles[j+1]=b;m.Triangles[j+2]=c;m.Triangles[j+3]=a;m.Triangles[j+4]=c;m.Triangles[j+5]=d;}
  static void Vertex(MeshData m,int i,double x,double y,double z,double alpha,double gain,int role=0){m.Vertices[i]=new Point(x,y,z);m.Alpha[i]=alpha;m.Gain[i]=gain;m.Role[i]=role;}
  static void Piece(MeshData m,int start,double x,double y,double z,double length,double width,double angle,double alpha,int role,double gain){
   double c=Math.Cos(angle),s=Math.Sin(angle);for(int i=0;i<4;i++){double a=(i==0?-.5:i==1?-.12:i==2?.5:.12)*length,b=(i==1?-.5:i==3?.5:0)*width;Vertex(m,start+i,x+c*a-s*b,y+s*a+c*b,z,alpha,gain,role);m.UV[start+i]=new Uv(i==0?0:i==1?.38:i==2?1:.62,i==1?0:i==3?1:.5);}
  }
  public static Buffer Create(string family){
   if(!IsFamily(family))throw new ArgumentException("Unknown local loss family",nameof(family));
   bool self=family=="selfloss";var b=new Buffer{Family=family,Batches=self?new[]{Mesh(40,20,7)}:new[]{Mesh(114,108,8),Mesh(32,16,7)}};
   if(self){for(int i=0;i<10;i++)Quad(b.Batches[0],i,i*4,i*4+1,i*4+2,i*4+3);}
   else {for(int k=0;k<3;k++)for(int i=0;i<18;i++)Quad(b.Batches[0],k*18+i,k*38+i*2,k*38+i*2+1,k*38+(i+1)*2+1,k*38+(i+1)*2);for(int i=0;i<8;i++)Quad(b.Batches[1],i,i*4,i*4+1,i*4+2,i*4+3);}
   return b;
  }
  static readonly double[,] Facets={{-.19,.095,.140,-.65},{-.10,-.035,.095,-1.10},{.09,.035,.105,-1.90},{.19,.10,.150,-2.48}};
  static void SelfLoss(Buffer b,double t){
   var m=b.Batches[0];double age=Math.Max(0,t),q=Smooth(age/.085),a=Fade(t,.030,.180);
   for(int i=0;i<4;i++)Piece(m,i*4,Facets[i,0]*(1-.58*q),Facets[i,1]-.11*q,.030+i*.003,Facets[i,2]*(1-.32*q),i%2==0?.025:.021,Facets[i,3]+.12*q,a,1,1.3);
   for(int i=0;i<6;i++){double born=.022+.006*(i%3),at=Math.Max(0,t-born),x=(i-2.5)*.031,vx=-x*.6,y=-.035-.019*(i%2),life=.13+.009*(i%3);Piece(m,(i+4)*4,x+vx*at,y-.38*at-.45*at*at,.045,.015+.004*(i%2),.006,-Math.PI/2+.25*Math.Sin(i),Smooth((t-born)/.008)*Fade(t-born,.010,life),i%3==0?1:0,1.12);}
  }
  static readonly double[] Births={0,.025,.055},Lives={.19,.165,.145},Cx={-.15,.028,.16},Cy={.075,.14,.025},Lengths={.25,.18,.215},Widths={.016,.015,.018};
  static void Affliction(Buffer b,double t){
   var thread=b.Batches[0];
   for(int k=0;k<3;k++){
    double local=t-Births[k],age=Math.Max(0,local),a=(k==0?.24+.76*Smooth(local/.012):Smooth(local/.012))*Fade(local,.055,Lives[k]);
    for(int i=0;i<19;i++){double u=i/18.0,x=Cx[k]+.018*Math.Sin(5*u+k*1.8)+.026*Smooth(age/.11)*(k==2?1:-1),y=Cy[k]-Lengths[k]*u-age*.42,w=Widths[k]*Math.Pow(Math.Sin(Math.PI*u),.45);for(int j=0;j<2;j++){int n=k*38+i*2+j;Vertex(thread,n,x+(j==0?-w:w),y,.031+k*.003,a,1.3,1);thread.UV[n]=new Uv(u,j);thread.Phase[n]=k*.7;}}
   }
   for(int i=0;i<8;i++){double born=.012+.007*i,age=Math.Max(0,t-born),life=.125+.006*(i%3),cy=.025-.020*(i%4),vx=.09*Math.Sin(i*1.7);Piece(b.Batches[1],i*4,Cx[i%3]+vx*age,cy-.55*age-.6*age*age,.049,.028+.005*(i%3),.009,-1.3+.5*Math.Sin(i),Smooth((t-born)/.008)*Fade(t-born,.022,life),1,1.22);}
  }
  public static void Sample(Buffer b,double t){if(b.Family=="selfloss")SelfLoss(b,t);else if(b.Family=="affliction")Affliction(b,t);else throw new ArgumentException("Unknown local loss family");}
 }
}
