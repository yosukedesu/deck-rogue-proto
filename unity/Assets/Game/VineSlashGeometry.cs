using System;
namespace DeckRogue.Game {
 // Frozen native vine-slash R1 contract. Pure, deterministic, double precision math;
 // no gameplay RNG, Unity clock, allocation per sample or invented second contact.
 internal static class VineSlashGeometry {
  public const double Anticipation=.066666667, VisibleEnd=.182, Horizon=.333333333;
  public struct Point { public double X,Y,Z; public Point(double x,double y,double z){X=x;Y=y;Z=z;} }
  public struct Uv { public double X,Y; public Uv(double x,double y){X=x;Y=y;} }
  public sealed class MeshData { public Point[] Vertices;public Uv[] UV;public double[] Alpha;public int[] Triangles; }
  public sealed class Buffer { public MeshData Primary,Secondary,Core,Particles;public double VisualAge,LightEnvelope; }
  static readonly double[,] Knots={{-Anticipation,-.80,1},{-.035,-.65,10},{0,0,22},{.035,.67,10},{.110,1.05,2},{Horizon,1.25,0}};
  static readonly double[,] UVProfile={{0.0,0.5,0.05},{0.020833333333333332,0.37430939226519333,0.013},{0.041666666666666664,0.3604972375690608,0.013},{0.0625,0.34806629834254144,0.013},{0.08333333333333333,0.3370165745856354,0.013},{0.10416666666666666,0.3287292817679558,0.013},{0.125,0.32182320441988954,0.013},{0.14583333333333331,0.3162983425414365,0.015469613259668507},{0.16666666666666666,0.31353591160220995,0.01740331491712707},{0.1875,0.3107734806629834,0.020303867403314916},{0.20833333333333331,0.3107734806629834,0.04157458563535912},{0.22916666666666666,0.3121546961325967,0.04640883977900552},{0.25,0.3149171270718232,0.050276243093922646},{0.2708333333333333,0.3204419889502762,0.05414364640883977},{0.29166666666666663,0.324585635359116,0.08218232044198895},{0.3125,0.3287292817679558,0.0841160220994475},{0.3333333333333333,0.3328729281767956,0.08701657458563536},{0.35416666666666663,0.33563535911602205,0.08701657458563536},{0.375,0.33977900552486184,0.08798342541436464},{0.3958333333333333,0.34392265193370164,0.0947513812154696},{0.41666666666666663,0.35635359116022103,0.12859116022099448},{0.4375,0.3715469613259669,0.10732044198895027},{0.4583333333333333,0.3825966850828729,0.11505524861878452},{0.47916666666666663,0.39364640883977897,0.12182320441988949},{0.5,0.40607734806629836,0.12569060773480664},{0.5208333333333333,0.4116022099447514,0.12762430939226518},{0.5416666666666666,0.419889502762431,0.13439226519337016},{0.5625,0.425414364640884,0.13632596685082873},{0.5833333333333333,0.4350828729281768,0.14212707182320442},{0.6041666666666666,0.4475138121546961,0.10055248618784529},{0.625,0.46685082872928174,0.11505524861878452},{0.6458333333333333,0.4875690607734806,0.1102209944751381},{0.6666666666666666,0.5013812154696133,0.10635359116022099},{0.6875,0.5151933701657458,0.15566298342541435},{0.7083333333333333,0.5193370165745856,0.15759668508287292},{0.7291666666666666,0.5276243093922652,0.10828729281767954},{0.75,0.5441988950276243,0.10828729281767954},{0.7708333333333333,0.569060773480663,0.11312154696132597},{0.7916666666666666,0.5883977900552486,0.11215469613259668},{0.8125,0.6049723756906078,0.10732044198895027},{0.8333333333333333,0.6174033149171271,0.08218232044198895},{0.8541666666666666,0.638121546961326,0.02803867403314917},{0.875,0.6574585635359116,0.025138121546961323},{0.8958333333333333,0.6781767955801106,0.025138121546961323},{0.9166666666666666,0.7002762430939227,0.022237569060773482},{0.9375,0.7265193370165746,0.019337016574585635},{0.9583333333333333,0.7596685082872928,0.013535911602209943},{0.9791666666666666,0.8024861878453039,0.013},{1.0,0.5,0.05}};
  // Birth, extra angle, extra speed, unscaled size: frozen seed output, not runtime randomness.
  static readonly double[,] ParticleSpecs={{0.0,0.029539525758989438,0.8404689841383481,0.04461985664440854},{0.0,0.44553787567599534,1.0254498667804997,0.053233251352732006},{0.0,0.05997113211962557,1.334334127933852,0.038769154668414345},{0.022,0.5538025156552036,0.781381770579271,0.0552109878293629},{0.022,-0.6328030518934236,0.9867369945327584,0.030297687296996265},{0.022,0.061664644824827586,0.8502843849475583,0.043524457601980884}};
  static readonly Point[] ParticleStarts=new Point[6],ParticleVelocities=new Point[6];
  static VineSlashGeometry(){for(int i=0;i<6;i++){
   double born=ParticleSpecs[i,0],eps=1e-5;Point p=Center(born,false,46),a=Center(born+eps,false,46),b=Center(born-eps,false,46);
   double x=(a.X-b.X)/(2*eps)*.30+Math.Cos(ParticleSpecs[i,1])*ParticleSpecs[i,2]*.45;
   double y=(a.Y-b.Y)/(2*eps)*.30+Math.Sin(ParticleSpecs[i,1])*ParticleSpecs[i,2]+.45;
   double z=(a.Z-b.Z)/(2*eps)*.30+.04*(i%3-1),r=Math.Sqrt(x*x+y*y),sign=i%2==1?2.1:-2.1;
   ParticleStarts[i]=p;ParticleVelocities[i]=new Point(x-y/r*sign,y+x/r*sign,z);
  }}
  static double Clamp(double q)=>Math.Max(0,Math.Min(1,q));
  static double Smooth(double q){q=Clamp(q);return q*q*(3-2*q);}
  public static double VisualClock(double age,double available=-1){
   if(age>=0 || available<0 || available>=Anticipation || available<=0)return age;
   if(age < -available)return -Anticipation-1e-6;
   double q=Clamp((age+available)/available),r=available/Anticipation;
   return -Anticipation+Anticipation*(q+(r-1)*(q*q*q-q*q));
  }
  static double Lead(double t){
   if(t<=Knots[0,0])return Knots[0,1];if(t>=Horizon)return Knots[5,1];
   int i=0;while(i<4 && t>Knots[i+1,0])i++;
   double d=Knots[i+1,0]-Knots[i,0],q=(t-Knots[i,0])/d,q2=q*q,q3=q2*q;
   return (2*q3-3*q2+1)*Knots[i,1]+(q3-2*q2+q)*d*Knots[i,2]+(-2*q3+3*q2)*Knots[i+1,1]+(q3-q2)*d*Knots[i+1,2];
  }
  static Point Center(double t,bool secondary,int i){
   double u=i/48.0,s=Lead(t-(secondary?.028:0))-1.10*(secondary?.55:1)*(1-u);
   double x=2*Math.Sin(s/2),y=2*(1-Math.Cos(s/2)),c=Math.Cos(-.60),a=Math.Sin(-.60);
   return new Point(c*x-a*y,a*x+c*y-(secondary?.11*Math.Sin(Math.PI*u):0),.016*Math.Sin(u*5+t*6)+(secondary?.013:0));
  }
  static double BaseAlpha(double t)=>t>=-Anticipation && t<Horizon?Smooth((t+Anticipation)/Math.Min(Anticipation*.25,.025))*(1-Smooth((t-.08)/(Horizon-.08))):0;
  public static double CoreAlpha(double t)=>t>=0 && t<Horizon?1-Smooth((t-.045)/.035):0;
  public static double Light(double t){double v=t>=0?CoreAlpha(t):t>=-Anticipation?.025*Math.Pow(Smooth((t+Anticipation)/Anticipation),3):0;return v*(1-Smooth((t-.10)/.15));}
  static MeshData Mesh(int count,int quads){return new MeshData{Vertices=new Point[count],UV=new Uv[count],Alpha=new double[count],Triangles=new int[quads*6]};}
  static void Quad(int[] t,int q,int a,int b,int c,int d){int k=q*6;t[k]=a;t[k+1]=b;t[k+2]=c;t[k+3]=a;t[k+4]=c;t[k+5]=d;}
  public static Buffer Create(){
   var b=new Buffer{Primary=Mesh(147,96),Secondary=Mesh(147,96),Core=Mesh(12,3),Particles=Mesh(24,6)};
   foreach(var m in new[]{b.Primary,b.Secondary}) {
    for(int i=0;i<49;i++)for(int j=0;j<3;j++)m.UV[i*3+j]=new Uv(UVProfile[i,0],UVProfile[i,1]+UVProfile[i,2]*(j-1));
    for(int i=0;i<48;i++)for(int j=0;j<2;j++)Quad(m.Triangles,i*2+j,i*3+j,i*3+j+1,(i+1)*3+j+1,(i+1)*3+j);
   }
   for(int i=0;i<3;i++)Quad(b.Core.Triangles,i,i*4,i*4+1,i*4+2,i*4+3);
   for(int i=0;i<6;i++)Quad(b.Particles.Triangles,i,i*4,i*4+1,i*4+2,i*4+3);
   return b;
  }
  static void Ribbon(MeshData m,double t,bool secondary,bool mirror){
   double alpha=BaseAlpha(t)*(secondary?.55*Smooth((t-.018)/.016)*(1-Smooth((t-.055)/.080)):.92*(1-Smooth((t-.040)/.065)));
   for(int i=0;i<49;i++){
    Point p=Center(t,secondary,i),a=Center(t,secondary,Math.Max(0,i-1)),b=Center(t,secondary,Math.Min(48,i+1));
    double dx=b.X-a.X,dy=b.Y-a.Y,r=Math.Sqrt(dx*dx+dy*dy),width=.045*Math.Pow(Math.Max(0,Math.Sin(Math.PI*i/48)),.85)*(secondary?.24:1);
    for(int j=0;j<3;j++){double side=j-1,x=p.X-dy/r*width*side;
     m.Vertices[i*3+j]=new Point(mirror?-x:x,p.Y+dx/r*width*side,p.Z+.008*(1-side*side));m.Alpha[i*3+j]=alpha;
    }
   }
  }
  static void Diamond(MeshData m,int offset,Point p,double size,double angle,double halfWidth,double shrink,double alpha,bool mirror){
   double c=Math.Cos(angle),s=Math.Sin(angle);
   for(int j=0;j<4;j++){
    double x=j==0?-.5*shrink:j==2?.5*shrink:0,y=j==1?-halfWidth:j==3?halfWidth:0;
    double vx=p.X+size*(c*x-s*y);m.Vertices[offset+j]=new Point(mirror?-vx:vx,p.Y+size*(s*x+c*y),p.Z);m.Alpha[offset+j]=alpha;
   }
  }
  public static void Sample(Buffer b,double age,bool mirrorX=false,double availableAnticipation=-1){
   double t=VisualClock(age,availableAnticipation);b.VisualAge=t;b.LightEnvelope=Light(t);
   Ribbon(b.Primary,t,false,mirrorX);Ribbon(b.Secondary,t,true,mirrorX);
   for(int i=0;i<3;i++)Diamond(b.Core,i*4,new Point(.012*i,.008*(i-1),.07/2.3),.12*(1-.24*i),i==0?-.44:i==1?-.51:-.36,.045,1,CoreAlpha(t),mirrorX);
   for(int i=0;i<6;i++){
    double a=t-ParticleSpecs[i,0];int o=i*4;
    if(t < -Anticipation || t>=Horizon || a<0 || a>=.16){for(int j=0;j<4;j++){b.Particles.Vertices[o+j]=default;b.Particles.Alpha[o+j]=0;}continue;}
    Point p=ParticleStarts[i],v=ParticleVelocities[i];double k=2.4,q=(1-Math.Exp(-k*a))/k;
    var center=new Point(p.X+v.X*q,p.Y+v.Y*q-1.6*(a/k-q/k),p.Z+v.Z*q+.06/2.3);
    Diamond(b.Particles,o,center,ParticleSpecs[i,3]*1.45,Math.Atan2(v.Y-1.6*a,v.X),.12,1-.35*a/.16,1-Smooth(a/.16),mirrorX);
   }
  }
 }
}
