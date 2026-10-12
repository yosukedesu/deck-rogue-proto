using System;
namespace DeckRogue.Game {
 // Contact-owned physical delivery; all shards/trails belong to one actual receipt.
 internal static class PlayerContactGeometry {
  public const double Anticipation=.130;
  public struct Point {public double X,Y,Z;public Point(double x,double y,double z){X=x;Y=y;Z=z;}}
  public struct Uv {public double X,Y;public Uv(double x,double y){X=x;Y=y;}}
  public sealed class MeshData {public int Kind;public Point[] Vertices;public Uv[] UV;public double[] Alpha,Gain;public bool[] Particle;public int[] Role,Triangles;}
  public sealed class Buffer {public string Family;public MeshData[] Batches;internal EnemyContactGeometry.Buffer Blunt;}
  static readonly double[,] Knots={{-.13,-1.05,1.5},{-.060,-.79,7},{0,0,15},{.035,.36,5},{.095,.48,0},{.17,.48,0}};
  static double Clamp(double q)=>Math.Max(0,Math.Min(1,q));
  static double Smooth(double q){q=Clamp(q);return q*q*(3-2*q);}
  static double Fade(double t,double h,double end)=>t>=0&&t<end?1-Smooth((t-h)/(end-h)):0;
  static double Envelope(double t,double birth,double end,double fadein)=>birth<t&&t<end?Smooth((t-birth)/fadein)*(1-Smooth((t-birth)/(end-birth))):0;
  public static double End(string family)=>family=="pierce"?.178:.270;
  public static double Light(string family,double t)=>(t>=0&&t<.080?1-Smooth((t-.015)/.065):0)*(family=="pierce"?.8:1);
  public static double Clock(double age,double available){if(age>=0||available>=Anticipation||available<=0)return age;if(age < -available)return -Anticipation-1e-6;double q=Clamp((age+available)/available),ratio=available/Anticipation;return -Anticipation+Anticipation*(q+(ratio-1)*(q*q*q-q*q));}
  static MeshData Mesh(int n,int tris,int kind)=>new MeshData{Kind=kind,Vertices=new Point[n],UV=new Uv[n],Alpha=new double[n],Gain=new double[n],Particle=new bool[n],Role=new int[n],Triangles=new int[tris*3]};
  static void Quad(MeshData m,int i,int a,int b,int c,int d){int j=i*6;m.Triangles[j]=a;m.Triangles[j+1]=b;m.Triangles[j+2]=c;m.Triangles[j+3]=a;m.Triangles[j+4]=c;m.Triangles[j+5]=d;}
  static void Vertex(MeshData m,int i,double x,double y,double z,double alpha,double gain,int role=0){m.Vertices[i]=new Point(x,y,z);m.Alpha[i]=alpha;m.Gain[i]=gain;m.Role[i]=role;}
  static void Diamond(MeshData m,int start,double x,double y,double z,double length,double width,double angle,double alpha,int role,double gain){double c=Math.Cos(angle),s=Math.Sin(angle);for(int i=0;i<4;i++){double a=(i==0?-.5:i==2?.5:0)*length,b=(i==1?-.5:i==3?.5:0)*width;Vertex(m,start+i,x+c*a-s*b,y+s*a+c*b,z,alpha,gain,role);}}
  static void AppendTriangles(MeshData m,int[] src,int vertexOffset,ref int index){for(int i=0;i<src.Length;i++)m.Triangles[index++]=src[i]+vertexOffset;}
  public static Buffer Create(string family){
   if(family!="pierce"&&family!="meteor")throw new ArgumentException("Unknown physical delivery family");
   var b=new Buffer{Family=family,Batches=family=="pierce"?new[]{Mesh(123,160,1),Mesh(104,98,0),Mesh(32,16,0)}:new[]{Mesh(73,64,2),Mesh(4,2,3),Mesh(246,288,0),Mesh(144,116,0)}};
   if(family=="pierce"){
    for(int i=0;i<40;i++)for(int j=0;j<2;j++)Quad(b.Batches[0],i*2+j,i*3+j,i*3+j+1,(i+1)*3+j+1,(i+1)*3+j);
    for(int k=0;k<2;k++)for(int i=0;i<24;i++)Quad(b.Batches[1],k*24+i,k*50+i*2,k*50+i*2+1,k*50+(i+1)*2+1,k*50+(i+1)*2);Quad(b.Batches[1],48,100,101,102,103);
    for(int i=0;i<8;i++)Quad(b.Batches[2],i,i*4,i*4+1,i*4+2,i*4+3);for(int i=0;i<32;i++)b.Batches[2].Particle[i]=true;
   }else{
    int f=0,d=0,fi=0,di=0;foreach(var c in PlayerContactRockData.Chunks){AppendTriangles(b.Batches[0],c.FrontTriangles,f,ref fi);AppendTriangles(b.Batches[2],c.DepthTriangles,d,ref di);for(int i=0;i<c.UV.Length;i++)b.Batches[0].UV[f+i]=c.UV[i];f+=c.Front.Length;d+=c.Depth.Length;}
    for(int k=0;k<2;k++)for(int i=0;i<24;i++)Quad(b.Batches[2],di/6+k*24+i,d+k*50+i*2,d+k*50+i*2+1,d+k*50+(i+1)*2+1,d+k*50+(i+1)*2);
    b.Blunt=EnemyContactGeometry.Create("blunt");int ignored=0;AppendTriangles(b.Batches[1],b.Blunt.Batches[0].Triangles,0,ref ignored);ignored=0;AppendTriangles(b.Batches[3],b.Blunt.Batches[1].Triangles,0,ref ignored);AppendTriangles(b.Batches[3],b.Blunt.Batches[2].Triangles,104,ref ignored);for(int i=104;i<144;i++)b.Batches[3].Particle[i]=true;
   }return b;
  }
  static double Head(double t){if(t<=Knots[0,0])return Knots[0,1];if(t>=Knots[5,0])return Knots[5,1];int k=0;while(k<4&&t>Knots[k+1,0])k++;double dt=Knots[k+1,0]-Knots[k,0],q=(t-Knots[k,0])/dt,q2=q*q,q3=q2*q;return(2*q3-3*q2+1)*Knots[k,1]+(q3-2*q2+q)*dt*Knots[k,2]+(-2*q3+3*q2)*Knots[k+1,1]+(q3-q2)*dt*Knots[k+1,2];}
  static void Pierce(Buffer b,double t){
   double head=Head(t),growth=.24+.76*Smooth((t+Anticipation)/Anticipation),length=.95*growth,a=t>=-Anticipation&&t<.125?Smooth((t+Anticipation)/.020)*(1-Smooth((t-.02)/.105)):0;var body=b.Batches[0];
   for(int i=0;i<41;i++){double u=i/40.0,x=head-length*(1-u),y=-.035*x,w=.090*Math.Pow(1-u,.65)+.00005;for(int j=0;j<3;j++){double side=j-1;int n=i*3+j;Vertex(body,n,x,y+side*w,.016+.007*(1-side*side),a,1.55);body.UV[n]=new Uv((69+768*u)/1774,1-(523+(368-523)*(side+1)/2)/887);}}
   var solid=b.Batches[1];for(int k=0;k<2;k++)for(int i=0;i<25;i++){double u=i/24.0,start=head-.82*growth,end=head-.045,x=start+(end-start)*u,y=-.035*x+(k*2-1)*(.029*(1-u)+.008),w=.0038*Math.Sin(Math.PI*u);for(int j=0;j<2;j++)Vertex(solid,k*50+i*2+j,x,y+(j==0?-w:w),.011,a*.48,1.1,2);}
   Diamond(solid,100,.005,0,.043,.10,.011,-.035,Fade(t,.014,.052),3,2.8);
   for(int i=0;i<8;i++){double born=.004*(i%3),age=Math.Max(0,t-born),q=(1-Math.Exp(-5*age))/5,angle=-.035+(i-3.5)*.105,speed=1.5+.17*i;Diamond(b.Batches[2],i*4,Math.Cos(angle)*speed*q,Math.Sin(angle)*speed*q-.5*age*age,.05,.03+.005*(i%3),.0055,angle,Envelope(t,born,.17+born,.003),2,1.65);}
  }
  static Point Rotate(Point p,double zangle,double yangle=0){double c=Math.Cos(yangle),s=Math.Sin(yangle),x=c*p.X+s*p.Z,z=-s*p.X+c*p.Z;c=Math.Cos(zangle);s=Math.Sin(zangle);return new Point(c*x-s*p.Y,s*x+c*p.Y,z);}
  static Point RockPosition(Point p,Point center,double spin,double yaw,double zangle,double q,Point pre,Point travel){var v=Rotate(new Point(p.X-center.X,p.Y-center.Y,p.Z-center.Z),spin,yaw);v=Rotate(new Point(v.X+center.X,v.Y+center.Y,v.Z+center.Z),zangle,.08*q);return new Point(v.X+pre.X+travel.X,v.Y+pre.Y+travel.Y,v.Z+pre.Z+travel.Z);}
  static void CopyBlunt(EnemyContactGeometry.MeshData src,MeshData dst,int offset){for(int i=0;i<src.Vertices.Length;i++){var p=src.Vertices[i];Vertex(dst,offset+i,p.X*1.32,p.Y*1.32,p.Z+.040,src.Alpha[i],src.Gain[i],src.Role[i]+1);dst.UV[offset+i]=new Uv(src.UV[i].X,src.UV[i].Y);}}
  static void Meteor(Buffer b,double t){
   double q=Clamp((t+Anticipation)/Anticipation),progress=.2*q+.8*q*q,at=Math.Max(0,t),zangle=-.2+.8*q,a=t>=-Anticipation&&t<0?Smooth((t+Anticipation)/.025):Fade(t,.035,.270);var pre=new Point(-.38*(1-progress),.85*(1-progress),.022);int f=0,d=0;
   foreach(var chunk in PlayerContactRockData.Chunks){int k=chunk.Index;var center=chunk.Center;double angle=k==0?.3:Math.Atan2(center.Y,center.X),speed=1.4+.15*(k%3),yv=.6+Math.Abs(Math.Sin(angle))*1.4,spin=at*(k%2==1?5:-6),yaw=at*(k%3!=0?4:-5);var travel=new Point(Math.Cos(angle)*speed*at,yv*at-4.5*at*at,.035*at*(k%3-1));
    for(int i=0;i<chunk.Front.Length;i++){var p=RockPosition(chunk.Front[i],center,spin,yaw,zangle,q,pre,travel);Vertex(b.Batches[0],f++,p.X,p.Y,p.Z,a,.60);}
    for(int i=0;i<chunk.Depth.Length;i++){var p=RockPosition(chunk.Depth[i],center,spin,yaw,zangle,q,pre,travel);Vertex(b.Batches[2],d++,p.X,p.Y,p.Z,a,.75,0);}
   }
   double length=Math.Sqrt(.38*.38+.85*.85),nx=.85/length,ny=.38/length;
   for(int k=0;k<2;k++){double delay=k==0?.075:.055,alpha=t>=-Anticipation&&t<delay?Smooth((t+Anticipation)/.025)*(k==0?.32:.20)*(1-Smooth(Math.Max(t,0)/delay)):0;for(int i=0;i<25;i++){double u=i/24.0,qt=Clamp((t-delay*(1-u)+Anticipation)/Anticipation),prog=.2*qt+.8*qt*qt,x=-.38*(1-prog),y=.85*(1-prog),off=k==0?.018:-.025,w=(k==0?.016:.007)*Math.Pow(Math.Sin(Math.PI*u),.7);for(int j=0;j<2;j++){double side=j==0?-1:1;Vertex(b.Batches[2],d+k*50+i*2+j,x+nx*(off+side*w),y+ny*(off+side*w),-.02,alpha,.8,2);}}}
   EnemyContactGeometry.Sample(b.Blunt,t);CopyBlunt(b.Blunt.Batches[0],b.Batches[1],0);CopyBlunt(b.Blunt.Batches[1],b.Batches[3],0);CopyBlunt(b.Blunt.Batches[2],b.Batches[3],104);
  }
  public static void Sample(Buffer b,double t){if(b.Family=="pierce")Pierce(b,t);else if(b.Family=="meteor")Meteor(b,t);else throw new ArgumentException("Unknown physical delivery");}
  public static bool FitIncoming(Buffer b,double sourceX,double sourceY){if(b.Family=="meteor")return true;double length=Math.Sqrt(sourceX*sourceX+sourceY*sourceY);bool valid=!double.IsNaN(length)&&!double.IsInfinity(length)&&length>1e-5;double c=valid?-sourceX/length:1,s=valid?-sourceY/length:0;foreach(var m in b.Batches)for(int i=0;i<m.Vertices.Length;i++){var p=m.Vertices[i];m.Vertices[i]=new Point(c*p.X-s*p.Y,s*p.X+c*p.Y,p.Z);}return valid;}
 }
}
