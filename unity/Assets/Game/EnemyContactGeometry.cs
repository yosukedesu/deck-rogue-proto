using System;
namespace DeckRogue.Game {
 // Frozen enemy contact R1 families, evaluated in stable receiver-visible H units.
 // Three claw ribbons are geometry of ONE receipt, never gameplay hits.
 internal static class EnemyContactGeometry {
  public const double ClawAnticipation=.066666667, ClawEnd=.172, BluntEnd=.222, FangAnticipation=.09, FangEnd=.14, BeamAnticipation=.08, BeamEnd=.16;
  public struct Point {public double X,Y,Z;public Point(double x,double y,double z){X=x;Y=y;Z=z;}}
  public struct Uv {public double X,Y;public Uv(double x,double y){X=x;Y=y;}}
  public sealed class MeshData {public Point[] Vertices;public Uv[] UV;public double[] Alpha,Gain;public int[] Role,Triangles;}
  public sealed class Buffer {public string Family;public MeshData[] Batches;public double LightEnvelope;}
  static readonly double[,] UVProfile={{0.0,0.5,0.05},{0.020833333333333332,0.37430939226519333,0.013},{0.041666666666666664,0.3604972375690608,0.013},{0.0625,0.34806629834254144,0.013},{0.08333333333333333,0.3370165745856354,0.013},{0.10416666666666666,0.3287292817679558,0.013},{0.125,0.32182320441988954,0.013},{0.14583333333333331,0.3162983425414365,0.015469613259668507},{0.16666666666666666,0.31353591160220995,0.01740331491712707},{0.1875,0.3107734806629834,0.020303867403314916},{0.20833333333333331,0.3107734806629834,0.04157458563535912},{0.22916666666666666,0.3121546961325967,0.04640883977900552},{0.25,0.3149171270718232,0.050276243093922646},{0.2708333333333333,0.3204419889502762,0.05414364640883977},{0.29166666666666663,0.324585635359116,0.08218232044198895},{0.3125,0.3287292817679558,0.0841160220994475},{0.3333333333333333,0.3328729281767956,0.08701657458563536},{0.35416666666666663,0.33563535911602205,0.08701657458563536},{0.375,0.33977900552486184,0.08798342541436464},{0.3958333333333333,0.34392265193370164,0.0947513812154696},{0.41666666666666663,0.35635359116022103,0.12859116022099448},{0.4375,0.3715469613259669,0.10732044198895027},{0.4583333333333333,0.3825966850828729,0.11505524861878452},{0.47916666666666663,0.39364640883977897,0.12182320441988949},{0.5,0.40607734806629836,0.12569060773480664},{0.5208333333333333,0.4116022099447514,0.12762430939226518},{0.5416666666666666,0.419889502762431,0.13439226519337016},{0.5625,0.425414364640884,0.13632596685082873},{0.5833333333333333,0.4350828729281768,0.14212707182320442},{0.6041666666666666,0.4475138121546961,0.10055248618784529},{0.625,0.46685082872928174,0.11505524861878452},{0.6458333333333333,0.4875690607734806,0.1102209944751381},{0.6666666666666666,0.5013812154696133,0.10635359116022099},{0.6875,0.5151933701657458,0.15566298342541435},{0.7083333333333333,0.5193370165745856,0.15759668508287292},{0.7291666666666666,0.5276243093922652,0.10828729281767954},{0.75,0.5441988950276243,0.10828729281767954},{0.7708333333333333,0.569060773480663,0.11312154696132597},{0.7916666666666666,0.5883977900552486,0.11215469613259668},{0.8125,0.6049723756906078,0.10732044198895027},{0.8333333333333333,0.6174033149171271,0.08218232044198895},{0.8541666666666666,0.638121546961326,0.02803867403314917},{0.875,0.6574585635359116,0.025138121546961323},{0.8958333333333333,0.6781767955801106,0.025138121546961323},{0.9166666666666666,0.7002762430939227,0.022237569060773482},{0.9375,0.7265193370165746,0.019337016574585635},{0.9583333333333333,0.7596685082872928,0.013535911602209943},{0.9791666666666666,0.8024861878453039,0.013},{1.0,0.5,0.05}};
  static readonly double[,] Knots={{-ClawAnticipation,-.8,1},{-.035,-.65,10},{0,0,22},{.035,.67,10},{.11,1.05,2},{.19,1.15,0}};
  static double Clamp(double v)=>Math.Max(0,Math.Min(1,v));
  static double Smooth(double v){double q=Clamp(v);return q*q*(3-2*q);}
  static double Envelope(double t,double birth,double end,double fadein)=>birth<t && t<end?Smooth((t-birth)/fadein)*(1-Smooth((t-birth)/(end-birth))):0;
  public static double Light(double t)=>t>=0 && t<.08?1-Smooth((t-.015)/.065):0;
  public static double End(string family)=>family=="claw"?ClawEnd:family=="fang"?FangEnd:family=="beam"?BeamEnd:BluntEnd;
  public static double Light(string family,double t)=>family=="fang"||family=="beam"?(t>=0 && t<.074?1-Smooth((t-.014)/.060):0):Light(t);
  public static double BeamClock(double age,double available){
   if(age>=0 || available>=BeamAnticipation || available<=0)return age;
   if(age < -available)return -BeamAnticipation-1e-6;
   double q=Clamp((age+available)/available),ratio=available/BeamAnticipation;return -BeamAnticipation+BeamAnticipation*(q+(ratio-1)*(q*q*q-q*q));
  }
  static MeshData Mesh(int n,int quads)=>new MeshData{Vertices=new Point[n],UV=new Uv[n],Alpha=new double[n],Gain=new double[n],Role=new int[n],Triangles=new int[quads*6]};
  static void Quad(int[] a,int quad,int v0,int v1,int v2,int v3){int j=quad*6;a[j]=v0;a[j+1]=v1;a[j+2]=v2;a[j+3]=v0;a[j+4]=v2;a[j+5]=v3;}
  public static Buffer Create(string family){
   if(family=="fang" || family=="beam")return CreateFangBeam(family);
   if(family!="blunt" && family!="claw")throw new ArgumentException("Unknown physical family",nameof(family));
   bool claw=family=="claw";var b=new Buffer{Family=family,Batches=claw?new[]{Mesh(441,288),Mesh(12,3),Mesh(32,8)}:new[]{Mesh(4,1),Mesh(104,48),Mesh(40,10)}};
   var surface=b.Batches[0];
   if(claw){for(int k=0;k<3;k++){
    for(int i=0;i<49;i++)for(int j=0;j<3;j++)surface.UV[k*147+i*3+j]=new Uv(UVProfile[i,0],UVProfile[i,1]+UVProfile[i,2]*(j-1));
    for(int i=0;i<48;i++)for(int j=0;j<2;j++)Quad(surface.Triangles,k*96+i*2+j,k*147+i*3+j,k*147+i*3+j+1,k*147+(i+1)*3+j+1,k*147+(i+1)*3+j);
   }}else {surface.UV[0]=new Uv(0,0);surface.UV[1]=new Uv(.5,0);surface.UV[2]=new Uv(.5,1);surface.UV[3]=new Uv(0,1);Quad(surface.Triangles,0,0,1,2,3);}
   var solid=b.Batches[1];if(claw){for(int i=0;i<3;i++)Quad(solid.Triangles,i,i*4,i*4+1,i*4+2,i*4+3);}
   else for(int j=0;j<4;j++)for(int i=0;i<12;i++)Quad(solid.Triangles,j*12+i,j*26+i*2,j*26+i*2+1,j*26+(i+1)*2+1,j*26+(i+1)*2);
   var particles=b.Batches[2];for(int i=0;i<(claw?8:10);i++)Quad(particles.Triangles,i,i*4,i*4+1,i*4+2,i*4+3);
   return b;
  }
  static void Vertex(MeshData m,int i,double x,double y,double z,double alpha,double gain,int role=0){m.Vertices[i]=new Point(x,y,z);m.Alpha[i]=alpha;m.Gain[i]=gain;m.Role[i]=role;}
  static void Diamond(MeshData m,int o,double cx,double cy,double cz,double length,double width,double angle,double alpha,int role,double gain){
   double c=Math.Cos(angle),s=Math.Sin(angle);
   for(int j=0;j<4;j++){double x=(j==0?-.5:j==2?.5:0)*length,y=(j==1?-.5:j==3?.5:0)*width;Vertex(m,o+j,cx+c*x-s*y,cy+s*x+c*y,cz,alpha,gain,role);}
  }
  static double Lead(double t){
   if(t<=Knots[0,0])return Knots[0,1];if(t>=Knots[5,0])return Knots[5,1];int i=0;while(i<4 && t>Knots[i+1,0])i++;
   double dt=Knots[i+1,0]-Knots[i,0],q=(t-Knots[i,0])/dt,q2=q*q,q3=q2*q;
   return (2*q3-3*q2+1)*Knots[i,1]+(q3-2*q2+q)*dt*Knots[i,2]+(-2*q3+3*q2)*Knots[i+1,1]+(q3-q2)*dt*Knots[i+1,2];
  }
  static Point Center(double lead,double length,double grow,double off,int i){double u=i/48.0,s=lead-length*grow*(1-u),x=2*Math.Sin(s/2),y=2*(1-Math.Cos(s/2)),ca=Math.Cos(-.60),sa=Math.Sin(-.60);return new Point(ca*x-sa*y-sa*off,sa*x+ca*y+ca*off,0);}
  static void Claw(Buffer b,double t){
   var surface=b.Batches[0];double ang=-.60,ca=Math.Cos(ang),sa=Math.Sin(ang),grow=.05+.95*Smooth((t+.056)/.056),lead=Lead(t);
   double alpha=t>=-ClawAnticipation && t<.115?Smooth((t+ClawAnticipation)/.016)*(1-Smooth((t-.025)/.090)):0;
   for(int k=0;k<3;k++){
    double length=k==0?.66:k==1?.80:.60,w=k==0?.024:k==1?.032:.023,off=k==0?.13:k==1?0:-.13;
    for(int i=0;i<49;i++){
     double u=i/48.0;Point p=Center(lead,length,grow,off,i),p0=Center(lead,length,grow,off,Math.Max(0,i-1)),p1=Center(lead,length,grow,off,Math.Min(48,i+1));
     double dx=p1.X-p0.X,dy=p1.Y-p0.Y,r=Math.Sqrt(dx*dx+dy*dy),width=w*Math.Pow(Math.Sin(Math.PI*u),.82)*Math.Sqrt(grow);
     for(int j=0;j<3;j++){double side=j-1;Vertex(surface,k*147+i*3+j,p.X-dy/r*width*side,p.Y+dx/r*width*side,.012+.005*k+.006*(1-side*side),alpha*(k==1?1:.83),k==1?1.8:1.55);}
    }
    double core=t>=0 && t<.04?1-Smooth((t-.014)/.026):0;
    Diamond(b.Batches[1],k*4,-sa*off,ca*off,.052,k==1?.085:.058,.008,ang,core,2,3.0);
   }
   for(int i=0;i<8;i++){
    double birth=.006*(i%3),age=t-birth,at=Math.Max(0,age),q=(1-Math.Exp(-5*at))/5,v=3.2+.21*i,angle=ang+.21*Math.Sin(i*2.9),off=i%3==0?.13:i%3==1?0:-.13;
    double x=-sa*off+Math.Cos(angle)*v*q,y=ca*off+Math.Sin(angle)*v*q-.9*at*at;
    Diamond(b.Batches[2],i*4,x,y,.058,(.043+.006*(i%3))*(1-.4*Clamp(age/.16)),.007,angle,Envelope(t,birth,birth+.16,.003),i%3==0?2:1,1.7);
   }
  }
  static void Blunt(Buffer b,double t){
   double alpha=t>=0 && t<.049?1-Smooth((t-.012)/.037):0;
   for(int j=0;j<4;j++){double u=j==1||j==2?1:0,v=j>=2?1:0;Vertex(b.Batches[0],j,(u-.607)*1.15,(v-.487)*1.15,.018,alpha*.95,1.30);}
   double age=Math.Max(0,t),q=1-Math.Exp(-9*age),radius=.065+.28*q;
   for(int j=0;j<4;j++){
    double start=j==0?-.52:j==1?1.10:j==2?2.60:4.20,end=j==0?.28:j==1?1.75:j==2?3.32:4.71;
    for(int i=0;i<13;i++){
     double u=i/12.0,angle=start+(end-start)*u,w=.014*(1-.50*q)*Math.Sin(Math.PI*u);
     for(int k=0;k<2;k++){double side=k==0?-1:1,r=radius+.007*q*Math.Sin(angle*17+j*1.7)+side*w;Vertex(b.Batches[1],j*26+i*2+k,Math.Cos(angle)*r,Math.Sin(angle)*r*.58,.025,Envelope(t,0,.165,.004)*.43,.75);}
    }
   }
   for(int i=0;i<10;i++){
    double birth=.003*(i%3),at=Math.Max(0,t-birth),life=.18+.013*(i%4),angle=2*Math.PI*i/10+.13*Math.Sin(i*3.1),speed=2.0+.19*(i%4),travel=(1-Math.Exp(-4*at))/4;
    double x=Math.Cos(angle)*speed*travel,y=Math.Sin(angle)*speed*travel*.66-1.65*at*at;
    Diamond(b.Batches[2],i*4,x,y,.034+.002*(i%3),(.028+.005*(i%3))*(1-.25*Clamp(at/life)),.016,angle+at*(i%2!=0?8:-6),Envelope(t,birth,birth+life,.004),i%3==0?1:0,i%3==0?1.1:.72);
   }
  }
  static Buffer CreateFangBeam(string family){
   bool fang=family=="fang";var b=new Buffer{Family=family,Batches=fang?new[]{Mesh(8,2),Mesh(8,2),Mesh(32,8)}:new[]{Mesh(287,240),Mesh(12,3),Mesh(36,9)}};
   var surface=b.Batches[0];if(fang){for(int i=0;i<2;i++){int o=i*4;Quad(surface.Triangles,i,o,o+1,o+2,o+3);surface.UV[o]=new Uv(0,0);surface.UV[o+1]=new Uv(1,0);surface.UV[o+2]=new Uv(1,1);surface.UV[o+3]=new Uv(0,1);}}
   else for(int i=0;i<40;i++)for(int j=0;j<6;j++)Quad(surface.Triangles,i*6+j,i*7+j,i*7+j+1,(i+1)*7+j+1,(i+1)*7+j);
   for(int j=1;j<3;j++){var m=b.Batches[j];for(int i=0;i<m.Vertices.Length/4;i++)Quad(m.Triangles,i,i*4,i*4+1,i*4+2,i*4+3);}
   return b;
  }
  static void Fang(Buffer b,double t){
   double q=Clamp((t+FangAnticipation)/FangAnticipation),gape=t<0?.18*(1-Math.Pow(q,2.5)):.050*Smooth((t-.010)/.055);
   double a=t>=-FangAnticipation && t<.14?Smooth((t+FangAnticipation)/.018)*(1-Smooth((t-.040)/.100)):0;
   for(int i=0;i<2;i++){
    double side=i==0?1:-1,width=i==0?.62:.57,height=width*809/1942.0;
    for(int j=0;j<4;j++){double u=j==1||j==2?1:0,v=j>=2?1:0;Vertex(b.Batches[0],i*4+j,side*width*(u-.5),side*(height*(v-.14833127317676142)+gape),.016,a*.91,.96);}
   }
   double core=t>=0 && t<.035?1-Smooth((t-.010)/.025):0;
   for(int i=0;i<2;i++)Diamond(b.Batches[1],i*4,i==0?-.174:.174,0,.05,.055,.007,0,core,2,2.3);
   for(int i=0;i<8;i++){
    double age=Math.Max(t,0),direction=i%2!=0?1:-1,angle=(direction==1?0:Math.PI)+.28*Math.Sin(i*1.7),travel=(1-Math.Exp(-5*age))/5,speed=1.6+.2*(i%3);
    Diamond(b.Batches[2],i*4,direction*.174+Math.Cos(angle)*speed*travel,Math.Sin(angle)*speed*travel-1.6*age*age,.045,.022+.003*(i%3),.008,angle+age*(i%2!=0?7:-7),Envelope(t,0,.135,.004),1,1.3);
   }
  }
  static void Beam(Buffer b,double t){
   double q=Smooth((t+BeamAnticipation)/BeamAnticipation),head=t<0?-1.08*(1-q):0,tail=t<.020?-1.08:-1.08*(1-Smooth((t-.020)/.085));
   double alpha=t>=-BeamAnticipation && t<.105?Smooth((t+BeamAnticipation)/.016)*(1-Smooth((t-.080)/.025)):0;
   for(int i=0;i<41;i++){
    double u=i/40.0,x=tail+(head-tail)*u,y=-x*.065,w=.030*Math.Pow(Math.Sin(Math.PI*u),.32)*Math.Min(1,Math.Sqrt(Math.Max(0,head-tail)/.25));
    for(int j=0;j<7;j++){double v=j/6.0,side=v*2-1;int k=i*7+j;Vertex(b.Batches[0],k,x,y+w*side,.020+.006*(1-side*side),alpha,1.8);b.Batches[0].UV[k]=new Uv((x+1.08)/1.08,v);}
   }
   double core=t>=0 && t<.06?1-Smooth((t-.012)/.048):0;
   for(int i=0;i<3;i++)Diamond(b.Batches[1],i*4,.018+.018*i,.014*(i-1),.051,i==0?.095:i==1?.070:.058,.007,i==0?.10:i==1?-.15:.25,core,2,2.7);
   for(int i=0;i<9;i++){
    double birth=.005*(i%3),age=Math.Max(0,t-birth),angle=-.9+.22*i,speed=1.9+.20*(i%4),travel=(1-Math.Exp(-4*age))/4;
    Diamond(b.Batches[2],i*4,Math.Cos(angle)*speed*travel,Math.Sin(angle)*speed*travel-.6*age*age,.048,.026+.005*(i%3),.005,angle,Envelope(t,birth,birth+.15,.004),i%3==0?2:1,1.4);
   }
  }
  static bool Finite(double v)=>!double.IsNaN(v) && !double.IsInfinity(v);
  // General world-pair adapter. Call once AFTER Sample; it intentionally differs from
  // reference camera-up width by using the actual projected ray's perpendicular.
  // Source and target span are never scaled by a damage-tier width multiplier.
  public static bool FitBeam(Buffer b,Point sourceH,double size=1){
   if(b.Family!="beam")return false;
   double length=Math.Sqrt(sourceH.X*sourceH.X+sourceH.Y*sourceH.Y);
   bool valid=Finite(length)&&Finite(sourceH.Z)&&length>1e-5;
   size=Finite(size)?Math.Max(.5,Math.Min(1.5,size)):1;
   if(!valid){for(int i=0;i<b.Batches[0].Alpha.Length;i++)b.Batches[0].Alpha[i]=0;
    for(int j=1;j<3;j++)for(int i=0;i<b.Batches[j].Vertices.Length;i++){var v=b.Batches[j].Vertices[i];b.Batches[j].Vertices[i]=new Point(v.X*size,v.Y*size,v.Z*size);}
    return false;
   }
   double dx=-sourceH.X/length,dy=-sourceH.Y/length,px=-dy,py=dx;
   var surface=b.Batches[0];for(int i=0;i<surface.Vertices.Length;i++){
    var p=surface.Vertices[i];double f=-p.X/1.08,width=(p.Y+p.X*.065)*size;
    surface.Vertices[i]=new Point(sourceH.X*f+px*width,sourceH.Y*f+py*width,sourceH.Z*f+p.Z);
    surface.UV[i]=new Uv(1-f*length/1.08,surface.UV[i].Y); // fixed distance density; never normalize current shrinking head/tail.
   }
   double angle=Math.Atan2(dy,dx)-Math.Atan2(-.065,1),c=Math.Cos(angle),s=Math.Sin(angle);
   for(int batch=1;batch<3;batch++){var m=b.Batches[batch];for(int i=0;i<m.Vertices.Length;i++){var p=m.Vertices[i];m.Vertices[i]=new Point(size*(c*p.X-s*p.Y),size*(s*p.X+c*p.Y),p.Z*size);}}
   return true;
  }
  public static void Sample(Buffer b,double age){if(b.Family=="claw")Claw(b,age);else if(b.Family=="fang")Fang(b,age);else if(b.Family=="beam")Beam(b,age);else Blunt(b,age);b.LightEnvelope=Light(b.Family,age);}
 }
}
