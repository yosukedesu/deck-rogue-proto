using System;

namespace DeckRogue.Game
{
    // Pure binary64 port of the frozen five-local-material R1 formulas.
    // Camera basis, captured alpha-visible H, placement, receipt routing and hitstop remain host-owned.
    internal static class LocalMaterialProfileGeometry
    {
        public enum Profile { Dark = 0, Light = 1, Botanical = 2, Physical = 3, Water = 4 }
        public const int MappedKind=0, WaterKind=1, WaterdropKind=2, CoreKind=3, SporeKind=4, PodKind=5, FacetKind=6, SolidKind=7;
        public const int ShadowRole=0, PrimaryRole=1, EdgeRole=2, CoreRole=3, MaximumParticleCount=12, BatchCount=3;
        public const double NativeLead=0, CoreHold=.014, CoreEnd=.056, LightHold=.018, LightEnd=.095, MaximumAbsoluteSampleAge=1000000;
        public struct Point { public double X,Y,Z; public Point(double x,double y,double z){X=x;Y=y;Z=z;} }
        public struct Uv { public double X,Y; public Uv(double x,double y){X=x;Y=y;} }
        public struct Bounds { public Point Min,Max; public Bounds(Point min,Point max){Min=min;Max=max;} }
        public sealed class MeshData
        {
            public readonly int Kind;
            public readonly Point[] Vertices;
            public readonly Uv[] UV;
            public readonly double[] Alpha,Gain,Phase;
            public readonly int[] Role,Triangles;
            // Alpha is the authored object opacity/vertex coverage before fragment carrier coverage.
            internal MeshData(int nv,int nt,int kind){Kind=kind;Vertices=new Point[nv];UV=new Uv[nv];Alpha=new double[nv];Gain=new double[nv];Phase=new double[nv];Role=new int[nv];Triangles=new int[nt*3];}
        }
        public sealed class ObjectRange
        {
            public readonly string Name,Material;
            public readonly int Batch,VertexOffset,VertexCount,TriangleOffset,TriangleCount;
            internal ObjectRange(string name,string material,int batch,int vo,int nv,int to,int nt){Name=name;Material=material;Batch=batch;VertexOffset=vo;VertexCount=nv;TriangleOffset=to;TriangleCount=nt;}
        }
        public sealed class Buffer
        {
            public readonly Profile Family;
            public readonly int MirrorRight, MaximumParticles=MaximumParticleCount;
            public readonly MeshData Body,Core,Particles;
            public readonly MeshData[] Batches;
            public readonly ObjectRange[] Objects;
            public readonly bool[] ParticleActive=new bool[MaximumParticleCount];
            public readonly Point[] ParticleCenters=new Point[MaximumParticleCount];
            public double VisualAge,LightEnvelope,TargetAreaW,FloorAreaW;
            public Bounds SampleBounds;
            internal Buffer(Profile p,int mirror)
            {
                Family=p;MirrorRight=mirror;
                int n=BodyObjectCount(p),nv=BodyObjectVertices(p),nt=BodyObjectTriangles(p),kind=p==Profile.Botanical?PodKind:p==Profile.Water?WaterKind:MappedKind;
                Body=new MeshData(n*nv,n*nt,kind);Core=new MeshData(4,2,CoreKind);
                Particles=new MeshData(48,24,p==Profile.Botanical?SporeKind:p==Profile.Physical?FacetKind:p==Profile.Water?WaterdropKind:SolidKind);
                Batches=new[]{Body,Core,Particles};Objects=new ObjectRange[n+13];
                for(int k=0;k<n;k++)Objects[k]=new ObjectRange(p==Profile.Dark?"tear_"+k:p==Profile.Light?"ray_"+k:p==Profile.Botanical?"pod_shell_"+k:p==Profile.Physical?"compression_accent":"water_band_"+k,p==Profile.Dark?"dark":p==Profile.Light?"light":p==Profile.Botanical?"pod":p==Profile.Physical?"blunt":"water",0,k*nv,nv,k*nt,nt);
                Objects[n]=new ObjectRange("core","core",1,0,4,0,2);
                for(int i=0;i<12;i++)Objects[n+1+i]=new ObjectRange("particle_"+i,p==Profile.Botanical?"spore":p==Profile.Physical?"facet":p==Profile.Water?"waterdrop":"solid",2,i*4,4,i*2,2);
            }
        }
        static readonly double[] TearBirth={0,.003,.009,.014,.021},TearLife={.18,.205,.172,.198,.17},TearAngle={-.20,1.42,2.82,3.45,5.78},TearLength={.34,.22,.30,.18,.27};
        static readonly double[] RayBirth={0,.002,.005,.009,.014,.018,.023},RayAngle={-.15,.27,1.02,2.45,3.27,4.48,5.55},RaySpeed={5.8,4.2,3.8,4.5,5.5,3.8,4.1},RayLength={.42,.28,.30,.37,.39,.26,.29};
        static readonly double[] PodAngle={.28,2.80,4.90},PodSpin={-9,7,-6};
        public static bool Finite(double x){return !double.IsNaN(x)&&!double.IsInfinity(x);}
        public static double Clamp(double x){return Math.Max(0,Math.Min(1,x));}
        public static double Smooth(double x){double q=Clamp(x);return q*q*(3-2*q);}
        public static void CheckProfile(Profile p){if((int)p<0||(int)p>4)throw new ArgumentOutOfRangeException("profile");}
        static void CheckAge(double t){if(!Finite(t)||Math.Abs(t)>MaximumAbsoluteSampleAge)throw new ArgumentOutOfRangeException("age");}
        static double Fade(double t,double hold,double end){return 0<=t&&t<end?1-Smooth((t-hold)/(end-hold)):0;}
        static double Envelope(double t,double born,double life,double rise=.008){return Smooth((t-born)/rise)*Fade(t-born,.024,life);}
        public static string ProfileId(Profile p){CheckProfile(p);return p==Profile.Dark?"local_dark":p==Profile.Light?"local_light":p==Profile.Botanical?"local_botanical":p==Profile.Physical?"local_physical":"local_water_flow";}
        public static int Count(Profile p){CheckProfile(p);return MaximumParticleCount;}
        public static int BodyObjectCount(Profile p){CheckProfile(p);return p==Profile.Dark?5:p==Profile.Light?7:p==Profile.Botanical?3:p==Profile.Physical?1:2;}
        public static int BodyObjectVertices(Profile p){CheckProfile(p);return p==Profile.Dark||p==Profile.Light?75:p==Profile.Botanical?39:p==Profile.Physical?4:165;}
        public static int BodyObjectTriangles(Profile p){CheckProfile(p);return p==Profile.Dark||p==Profile.Light?96:p==Profile.Botanical?48:p==Profile.Physical?2:256;}
        public static double Lead(){return NativeLead;}
        public static double End(Profile p){CheckProfile(p);return p==Profile.Dark?.218:p==Profile.Light?.198:p==Profile.Botanical?.273:p==Profile.Physical?.238:.263;}
        public static double End(Buffer b){if(b==null)throw new ArgumentNullException("b");return End(b.Family);}
        // Dark metadata says .208, but normative tear_3 (.014 + .198) ends at .212. Preserve motion.
        public static double BodyEnd(Profile p){CheckProfile(p);return p==Profile.Dark?.212:p==Profile.Light?.172:p==Profile.Botanical?.212:p==Profile.Physical?.055:.170;}
        public static double LightEnvelope(Profile p,double t){CheckProfile(p);CheckAge(t);return Fade(t,LightHold,LightEnd)*(p==Profile.Dark||p==Profile.Physical?.45:p==Profile.Light?.60:p==Profile.Botanical?.30:.40);}
        public static double? NativeAge(double age,double availableA=0){CheckAge(age);if(!Finite(availableA))throw new ArgumentOutOfRangeException("availableA");return age<0?(double?)null:age;}
        // Includes every authored carrier (including invisible entries), either mirror, 0 <= age <= End.
        // Conservative analytic envelope: particle q<=.2, max speed<=3.31, plus offset and half-size;
        // bodies include the full 1.05-H physical carrier, pod shells and <=.8-H water bands.
        public static Bounds ConservativeVisibleBounds {get{return new Bounds(new Point(-.85,-.80,.015),new Point(.85,.80,.085));}}
        static void QuadTriangles(MeshData m,int vo,int to){int k=to*3;m.Triangles[k]=vo;m.Triangles[k+1]=vo+1;m.Triangles[k+2]=vo+2;m.Triangles[k+3]=vo;m.Triangles[k+4]=vo+2;m.Triangles[k+5]=vo+3;}
        static void Grid(MeshData m,int vo,int to,int rows,int cross)
        {
            int f=to*3;
            for(int i=0;i<rows-1;i++)for(int j=0;j<cross-1;j++)
            {int a=vo+i*cross+j;m.Triangles[f++]=a;m.Triangles[f++]=a+1;m.Triangles[f++]=a+cross+1;m.Triangles[f++]=a;m.Triangles[f++]=a+cross+1;m.Triangles[f++]=a+cross;}
        }
        public static Buffer Create(Profile p,int mirrorRight=1)
        {
            CheckProfile(p);if(mirrorRight!=1&&mirrorRight!=-1)throw new ArgumentOutOfRangeException("mirrorRight");var b=new Buffer(p,mirrorRight);
            int n=BodyObjectCount(p),nv=BodyObjectVertices(p),nt=BodyObjectTriangles(p);
            for(int k=0;k<n;k++){if(p==Profile.Physical)QuadTriangles(b.Body,0,0);else Grid(b.Body,k*nv,k*nt,p==Profile.Botanical?13:p==Profile.Water?33:25,p==Profile.Water?5:3);}
            QuadTriangles(b.Core,0,0);for(int i=0;i<12;i++)QuadTriangles(b.Particles,i*4,i*2);
            Sample(b,0);return b;
        }
        static void Put(Buffer b,MeshData m,int i,double x,double y,double z,double u,double v,double alpha,double gain,int role=EdgeRole,double phase=0)
        {m.Vertices[i]=new Point(b.MirrorRight*x,y,z);m.UV[i]=new Uv(u,v);m.Alpha[i]=alpha;m.Gain[i]=gain;m.Role[i]=role;m.Phase[i]=phase;}
        public static void Sample(Buffer b,double t,int count=MaximumParticleCount)
        {
            if(b==null)throw new ArgumentNullException("b");CheckAge(t);b.VisualAge=t;Profile p=b.Family;int ncount=Math.Max(0,Math.Min(12,count));
            if(p==Profile.Dark||p==Profile.Light)for(int k=0;k<BodyObjectCount(p);k++)
            {
                bool dark=p==Profile.Dark;double born=dark?TearBirth[k]:RayBirth[k],a=Math.Max(0,t-born),ang=dark?TearAngle[k]:RayAngle[k],ca=Math.Cos(ang),sa=Math.Sin(ang),q=(1-Math.Exp(-(dark?12:18)*a))/(dark?12:18);
                double lead=(dark?3.3+.55*(k%3):RaySpeed[k])*q,L=dark?TearLength[k]*(.2+.8*(1-Math.Exp(-40*a))):RayLength[k]*(.25+.75*(1-Math.Exp(-75*a)));
                double alpha=dark?Envelope(t,born,TearLife[k])*.9:Envelope(t,born,.145+.009*(k%2),.005);
                for(int i=0;i<25;i++)
                {
                    double u=i/24.0,s=lead-L*(1-u),bend=dark?.035*Math.Sin(Math.PI*u)*Math.Sin(12*a+k):0,w=(dark?.026+.006*(k%2):(k==0||k==4?.018:.010))*Math.Pow(Math.Sin(Math.PI*u),.6);
                    for(int j=0;j<3;j++)
                    {int side=j-1;double dx=bend+side*w;Put(b,b.Body,k*75+i*3+j,ca*s-sa*dx,sa*s+ca*dx,dark?.032+k*.003:.026+k*.002,dark?(560+300*u-150*u*u+side*62)/1774:(528+side*32)/1774.0,dark?1-(470-420*u+60*u*u+side*32)/887:1-(835-740*u)/887,alpha,dark?1.9:2.15);}
                }
            }
            else if(p==Profile.Botanical)for(int k=0;k<3;k++)
            {
                double a=Math.Max(0,t-.004*k),q=(1-Math.Exp(-8*a))/8,ang=PodAngle[k],cx=Math.Cos(ang)*(2.7+.4*k)*q,cy=Math.Sin(ang)*(2.7+.4*k)*q-1.2*a*a,theta=ang+PodSpin[k]*a,ca=Math.Cos(theta),sa=Math.Sin(theta),L=.105-.008*k;
                double alpha=Envelope(t,.004*k,.180+.012*k);
                for(int i=0;i<13;i++){double u=i/12.0,x=(u-.5)*L,mid=.023*Math.Sin(Math.PI*u),hw=.022*Math.Pow(Math.Sin(Math.PI*u),.8);for(int j=0;j<3;j++){int side=j-1;Put(b,b.Body,k*39+i*3+j,cx+ca*x-sa*(mid+side*hw),cy+sa*x+ca*(mid+side*hw),.03+k*.002,u,(side+1)/2.0,alpha,1.5,EdgeRole,k*1.3);}}
            }
            else if(p==Profile.Physical)for(int i=0;i<4;i++){double u=i==0||i==3?0:1,v=i<2?0:1;Put(b,b.Body,i,(u-.607)*1.05,(v-.487)*1.05,.02,u*.5,v,Fade(t,.012,.055)*.9,1.3);}
            else for(int k=0;k<2;k++)
            {
                double a=Math.Max(0,t),lead=(5.8-1.1*k)*(1-Math.Exp(-12*a))/12,L=(k==0?.80:.62)*(.22+.78*(1-Math.Exp(-65*a))),alpha=Envelope(t,0,.170,.008)*(k==0?.9:.42);
                for(int i=0;i<33;i++){double u=i/32.0,x=lead-L*(1-u),y=.050*Math.Sin(5*u-7*a)+(k==0?-.025:.06),w=(k==0?.15:.080)*Math.Pow(Math.Sin(Math.PI*u),.8)*(1-.6*Smooth((a-.04)/.10));for(int j=0;j<5;j++){double side=j*.5-1;Put(b,b.Body,k*165+i*5+j,x,y+side*w,.027+k*.008+.01*Math.Sin(9*u-11*a)*(1-side*side),u+.12*t,.50+.18*side,alpha,1.40);}}
            }
            double coreW=p==Profile.Dark?.095:p==Profile.Light?.20:p==Profile.Botanical?.125:p==Profile.Physical?.13:.16;
            for(int i=0;i<4;i++){double u=i==0||i==3?0:1,v=i<2?0:1;Put(b,b.Core,i,(u-.5)*coreW,(v-.5)*coreW*.25,.08,u,v,Fade(t,CoreHold,CoreEnd),2.7,CoreRole);}
            for(int i=0;i<12;i++)
            {
                double born=.004*(i%6),a=Math.Max(0,t-born),life=(p==Profile.Dark?.18:p==Profile.Light?.16:p==Profile.Botanical?.235:p==Profile.Physical?.20:.225)+.009*(i%3),ang=i*2.399963+.18,speed=(p==Profile.Dark?2.2:p==Profile.Light?2.8:p==Profile.Botanical?1.6:p==Profile.Physical?2.5:2.1)+.17*(i%4),q=(1-Math.Exp(-5*a))/5;
                if(p==Profile.Water){ang=-.7+.25*(i%7);speed+=.7;}
                double cx=Math.Cos(ang)*speed*q,cy=Math.Sin(ang)*speed*q*(p==Profile.Botanical?.65:.8)-(p==Profile.Physical||p==Profile.Water?1.9:.55)*a*a,alpha=Envelope(t,born,life,.007),theta=ang+a*(i%2!=0?5:-6);
                double L=p==Profile.Botanical?.050+.005*(i%3):p==Profile.Water?.032+.007*(i%3):.037+.005*(i%3),w=p==Profile.Botanical?.034:p==Profile.Water?.012:.009;
                double gain=p==Profile.Physical?.78:p==Profile.Botanical?1.75:p==Profile.Water?1.1:p==Profile.Light?1.9:1.7;int role=p==Profile.Physical||p==Profile.Water?EdgeRole:i%4==0?CoreRole:EdgeRole;double phase=p==Profile.Botanical?i*1.713:0;
                if(p==Profile.Botanical)alpha*=.82;if(i>=ncount||t<0)alpha=0;
                if(p==Profile.Water){cx+=.050*Math.Sin(i*2.3);cy+=.030*Math.Cos(i*1.7);}
                double ca=Math.Cos(p==Profile.Botanical||p==Profile.Physical?theta:ang),sa=Math.Sin(p==Profile.Botanical||p==Profile.Physical?theta:ang);
                for(int j=0;j<4;j++)
                {
                    double x,y,u,v;
                    if(p==Profile.Physical){double sz=.026+.005*(i%3);x=(j==0?-.65:j==1?.16:j==2?.62:-.2)*sz;y=(j==0?-.2:j==1?-.50:j==2?.18:.36+.06*(i%2))*sz;u=j==0||j==3?0:1;v=j<2?0:1;}
                    else if(p==Profile.Botanical){x=(j==0||j==3?-.5:.5)*L;y=(j<2?-.5:.5)*w;u=j==0||j==3?0:1;v=j<2?0:1;}
                    else{x=(j==0?-.5:j==2?.5:0)*L;y=(j==1?-.5:j==3?.5:0)*w;u=j==0?0:j==2?1:.5;v=j==1?0:j==3?1:.5;}
                    Put(b,b.Particles,i*4+j,cx+ca*x-sa*y,cy+sa*x+ca*y,.05,u,v,alpha,gain,role,phase);
                }
                b.ParticleCenters[i]=new Point(b.MirrorRight*cx,cy,.05);b.ParticleActive[i]=alpha>0;
            }
            if(t<0){Array.Clear(b.Body.Alpha,0,b.Body.Alpha.Length);Array.Clear(b.Core.Alpha,0,4);}
            b.LightEnvelope=LightEnvelope(p,t);b.TargetAreaW=30*b.LightEnvelope;b.FloorAreaW=10*b.LightEnvelope;
            Point min=b.Core.Vertices[0],max=min;
            for(int k=0;k<3;k++)for(int i=0;i<b.Batches[k].Vertices.Length;i++){Point v=b.Batches[k].Vertices[i];min.X=Math.Min(min.X,v.X);min.Y=Math.Min(min.Y,v.Y);min.Z=Math.Min(min.Z,v.Z);max.X=Math.Max(max.X,v.X);max.Y=Math.Max(max.Y,v.Y);max.Z=Math.Max(max.Z,v.Z);}
            b.SampleBounds=new Bounds(min,max);
        }
        public static bool SampleReceipt(Buffer b,double age,double availableA=0,int count=MaximumParticleCount){double? native=NativeAge(age,availableA);Sample(b,age,count);return native.HasValue;}
    }
}
