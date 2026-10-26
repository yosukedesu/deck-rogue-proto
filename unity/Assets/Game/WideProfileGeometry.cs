using System;

namespace DeckRogue.Game
{
    // Frozen wide R2 numerical port. Target-local camera basis and H transform belong to host.
    // No caster ray, target enumeration, receipt routing, actor hitstop, RNG, or gameplay damage.
    internal static class WideProfileGeometry
    {
        public enum Profile { Water = 0, Fire = 1 }
        public const int WaterBandKind = 0, FireBandKind = 1, DropKind = 2, EmberKind = 3, CoreKind = 4;
        public const int MaximumParticleCount = 16;
        public const double NativeLead = .100, CoreEnd = .065, LightEnd = .105, MaximumAbsoluteSampleAge = 1000000;
        public struct Point { public double X, Y, Z; public Point(double x,double y,double z){X=x;Y=y;Z=z;} }
        public struct Uv { public double X, Y; public Uv(double x,double y){X=x;Y=y;} }
        public struct Bounds { public Point Min, Max; public Bounds(Point min,Point max){Min=min;Max=max;} }
        public struct HeadSample { public double Position, Velocity; public HeadSample(double x,double v){Position=x;Velocity=v;} }
        public sealed class MeshData
        {
            public readonly int Kind;
            public readonly Point[] Vertices;
            public readonly Uv[] UV;
            public readonly double[] Alpha, Gain, Phase;
            public readonly int[] Triangles;
            internal MeshData(int vertices,int triangles,int kind)
            { Kind=kind;Vertices=new Point[vertices];UV=new Uv[vertices];Alpha=new double[vertices];Gain=new double[vertices];Phase=new double[vertices];Triangles=new int[triangles*3]; }
        }
        public sealed class Buffer
        {
            public readonly Profile Family;
            public readonly int MirrorRight, MaximumParticles = MaximumParticleCount;
            public readonly MeshData Bands, Tongues, Core, Particles;
            public readonly MeshData[] Batches;
            public readonly bool[] ParticleActive = new bool[MaximumParticleCount];
            public readonly Point[] ParticleCenters = new Point[MaximumParticleCount];
            public double VisualAge, LightEnvelope, TargetAreaW, FloorAreaW;
            public Bounds SampleBounds;
            internal Buffer(Profile profile,int mirror)
            {
                Family=profile;MirrorRight=mirror;bool water=profile==Profile.Water;
                Bands=new MeshData(410,640,water?WaterBandKind:FireBandKind);
                Tongues=water?null:new MeshData(225,288,FireBandKind);
                Core=new MeshData(4,2,CoreKind);Particles=new MeshData(water?96:64,water?64:32,water?DropKind:EmberKind);
                Batches=water?new[]{Bands,Core,Particles}:new[]{Bands,Tongues,Core,Particles};
            }
        }
        static readonly double[] KnotT={-.100,0,.080,.205,.360}, KnotX={-1,0,.88,1.40,1.60}, KnotV={2,13,7,2,.3};
        public static bool Finite(double x){return !double.IsNaN(x)&&!double.IsInfinity(x);}
        public static double Clamp(double x){return Math.Max(0,Math.Min(1,x));}
        public static double Smooth(double x){double q=Clamp(x);return q*q*(3-2*q);}
        public static void CheckProfile(Profile profile){if(profile!=Profile.Water&&profile!=Profile.Fire)throw new ArgumentOutOfRangeException("profile");}
        static void CheckAge(double t){if(!Finite(t)||Math.Abs(t)>MaximumAbsoluteSampleAge)throw new ArgumentOutOfRangeException("t");}
        public static string ProfileId(Profile p){CheckProfile(p);return p==Profile.Water?"wide_water":"wide_fire";}
        public static int Count(Profile p){CheckProfile(p);return MaximumParticleCount;}
        public static double Lead(){return NativeLead;}
        public static double End(Profile p){CheckProfile(p);return p==Profile.Water?.300:.262;}
        public static double End(Buffer b){if(b==null)throw new ArgumentNullException("b");return End(b.Family);}
        public static double BodyEnd(Profile p){CheckProfile(p);return p==Profile.Water?.205:.190;}
        public static Bounds ConservativeVisibleBounds { get {return new Bounds(new Point(-3,-.6,-.01),new Point(3,.6,.10));} }
        // Continuous Hermite position and analytic velocity, including contact (x=0,v=13 H/s).
        public static HeadSample Head(double t)
        {
            CheckAge(t);
            if(t<=KnotT[0])return new HeadSample(KnotX[0]+(t-KnotT[0])*KnotV[0],KnotV[0]);
            if(t>=KnotT[4])return new HeadSample(KnotX[4]+(t-KnotT[4])*KnotV[4],KnotV[4]);
            for(int i=0;i<4;i++)if(t<=KnotT[i+1])
            {
                double d=KnotT[i+1]-KnotT[i],q=(t-KnotT[i])/d,q2=q*q,q3=q2*q;
                return new HeadSample((2*q3-3*q2+1)*KnotX[i]+(q3-2*q2+q)*d*KnotV[i]+(-2*q3+3*q2)*KnotX[i+1]+(q3-q2)*d*KnotV[i+1],
                    ((6*q2-6*q)*KnotX[i]+(3*q2-4*q+1)*d*KnotV[i]+(-6*q2+6*q)*KnotX[i+1]+(3*q2-2*q)*d*KnotV[i+1])/d);
            }
            throw new InvalidOperationException();
        }
        public static double LightEnvelope(Profile p,double age)
        {
            CheckProfile(p);CheckAge(age);
            return 0<=age&&age<=.105?1-Smooth((age-.026)/.079):-.100<=age&&age<0?.018*Smooth((age+.100)/.100):0;
        }
        // Guard all inputs before postcontact fast path. P=0 suppresses all anticipation.
        // For 0<A<P: t -(P-A)*(t/A)^2; derivative at contact is exactly one.
        public static double? NativeAge(double age,double availableA,double nativeP=NativeLead)
        {
            CheckAge(age);
            if(!Finite(availableA)||!Finite(nativeP)||nativeP<0||nativeP>MaximumAbsoluteSampleAge)throw new ArgumentOutOfRangeException("clockInputs");
            if(age>=0)return age;
            double a=Math.Min(nativeP,Math.Max(0,availableA));if(a<=0)return null;
            if(age< -a)return -nativeP;
            double q=age/a;return age-(nativeP-a)*q*q;
        }
        static void Grid(MeshData m,int offset,int triangleOffset,int n,int cross)
        {
            int k=triangleOffset;
            for(int half=0;half<2;half++)for(int i=0;i<n-1;i++)for(int j=0;j<cross-1;j++)
            {
                int v=offset+i*cross+j;m.Triangles[k++]=v;m.Triangles[k++]=v+cross+(half==0?0:1);m.Triangles[k++]=v+(half==0?cross+1:1);
            }
        }
        public static Buffer Create(Profile profile,int mirrorRight=1)
        {
            CheckProfile(profile);if(mirrorRight!=1&&mirrorRight!=-1)throw new ArgumentOutOfRangeException("mirrorRight");
            var b=new Buffer(profile,mirrorRight);bool water=profile==Profile.Water;
            Grid(b.Bands,0,0,41,5);Grid(b.Bands,205,960,41,5);
            if(!water)for(int n=0;n<3;n++)Grid(b.Tongues,n*75,n*288,25,3);
            for(int i=0;i<4;i++){b.Core.Vertices[i]=new Point(mirrorRight*(i==0||i==3?-.05:.05),i<2?-.025:.025,.07);b.Core.UV[i]=new Uv(i==0||i==3?0:1,i<2?0:1);b.Core.Gain[i]=2;}
            int[] coreFaces={0,1,2,0,2,3};Array.Copy(coreFaces,b.Core.Triangles,6);
            int nv=water?6:4,nt=water?4:2;
            for(int i=0;i<MaximumParticleCount;i++)
            {
                int start=i*nv,k=i*nt*3;
                for(int f=1;f<=nt;f++){b.Particles.Triangles[k++]=start;b.Particles.Triangles[k++]=start+f;b.Particles.Triangles[k++]=start+f+1;}
                for(int j=0;j<nv;j++)
                {
                    double u=water?(j==0?0:j==1||j==5?.25:j==2||j==4?.75:1):(j==0?0:j==2?1:.5);
                    double v=j==0||j==(water?3:2)?.5:j< (water?3:2)?0:1;
                    b.Particles.UV[start+j]=new Uv(u,v);b.Particles.Gain[start+j]=water?1.25:1.9;
                    // Stable indexed birth-offset phase; shader does not consume it.
                    b.Particles.Phase[start+j]=i*2.399;
                }
            }
            Sample(b,0);return b;
        }
        // Caller owns buffers. Sampling never allocates, replaces arrays, or accumulates delta time.
        public static void Sample(Buffer b,double t,int count=MaximumParticleCount)
        {
            if(b==null)throw new ArgumentNullException("b");CheckAge(t);
            b.VisualAge=t;bool water=b.Family==Profile.Water;double end=BodyEnd(b.Family),h=Head(t).Position;
            int ncount=Math.Max(0,Math.Min(MaximumParticleCount,count));
            for(int layer=0;layer<2;layer++)
            {
                double length=(water?1.72:1.60)*(layer==1?.85:1),lead=h-(layer==1?.055:0);
                double alpha=-NativeLead<=t&&t<=end?Smooth((t+NativeLead)/.022)*(1-Smooth((t-.075)/(end-.075))):0;alpha*=layer==1?.40:.88;
                for(int i=0;i<41;i++)
                {
                    double u=i/40.0,x=lead-length*(1-u),su=Math.Sin(Math.PI*u);
                    double y=.085*Math.Sin(Math.PI*x/.95)+.027*Math.Sin(9*u-8*t)*su-(layer==1?.13*su:0);
                    double sw=Math.Sin(8*u-11*t);
                    double width=(water?.265:.195)*Math.Pow(Math.Max(0,su),.8)*(.87+.13*sw*sw)*(layer==1?.48:1)*(1-.4*Smooth((t-.045)/.14));
                    for(int j=0;j<5;j++)
                    {
                        double side=j/2.0-1;int v=layer*205+i*5+j;
                        b.Bands.Vertices[v]=new Point(b.MirrorRight*x,y+width*side,.022+.020*Math.Sin(10*u-9*t)*(1-side*side)+.016*layer);
                        b.Bands.UV[v]=water?new Uv(u+.12*t+.009*Math.Sin(6*t+u),.5+.18*side+.006*Math.Sin(7*u-9*t)):
                            new Uv((415+970*u+28*t*su)/1774,1-(475-45*u+side*175+.012*887*Math.Sin(7*u-24*t)*su)/887);
                        b.Bands.Alpha[v]=alpha;b.Bands.Gain[v]=water?1.30:1.65;
                    }
                }
            }
            if(!water)for(int n=0;n<3;n++)
            {
                double birth=n==0?-.015:n==1?.010:.035,life=n==0?.135:n==1?.150:.145,a=Math.Max(0,t-birth);
                double alpha=birth<=t&&t<=birth+life?Smooth(a/.012)*(1-Smooth((a-life*.35)/(life*.65))):0;
                for(int i=0;i<25;i++)
                {
                    double u=i/24.0,x=.35*h-.15+.20*n+.20*a+.035*Math.Sin(8*u-20*a+n)*u,y=.015+(.11+.035*n)*u*(1+.2*Smooth(a/.07));
                    double width=(.050+.008*n)*Math.Pow(Math.Sin(Math.PI*u),.7)*(1-.65*u);
                    for(int j=0;j<3;j++)
                    {double side=j-1;int v=n*75+i*3+j;b.Tongues.Vertices[v]=new Point(b.MirrorRight*(x+side*width),y,.060+.003*n);b.Tongues.UV[v]=new Uv((415+970*(1-u))/1774,1-(470+side*150)/887);b.Tongues.Alpha[v]=alpha*.60;b.Tongues.Gain[v]=1.65;}
                }
            }
            double core=0<=t&&t<=.065?1-Smooth((t-.023)/.042):0;for(int i=0;i<4;i++)b.Core.Alpha[i]=core*.70;
            for(int i=0;i<MaximumParticleCount;i++)
            {
                double birth=.006*(i%8)+(i>=8?.033:0),life=water?.225:.175+.009*(i%3),a=Math.Max(0,t-birth);
                double alpha=birth<=t&&t<=birth+life&&i<ncount?Smooth(a/.010)*(1-Smooth((a-life*.32)/(life*.68))):0;
                var atBirth=Head(birth);double vx=.20*atBirth.Velocity+.25+.22*Math.Sin(i*1.8),vy=.50+.26*Math.Sin(i*2.4),travel=(1-Math.Exp(-4*a))/4;
                double x=.55*atBirth.Position+.025*Math.Sin(i*2.399)+vx*travel,y=.016*Math.Cos(i*1.4)+vy*travel+(water?-1.6*a*a:.35*a*a);
                double angle=Math.Atan2(vy+(water?-3.2*a:.70*a),vx*Math.Exp(-4*a)),ca=Math.Cos(angle),sa=Math.Sin(angle);
                double length=(.025+.004*(i%4))*(1-.38*Smooth(a/life)),width=.009+.002*(i%3),z=.075+.001*i;int nv=water?6:4;
                for(int j=0;j<nv;j++)
                {
                    double u=water?(j==0?-length:j==1||j==5?-length*.5:j==2||j==4?length*.5:length):(j==0?-length:j==2?length:0);
                    double v=j==0||j==(water?3:2)?0:j<(water?3:2)?-width:width;int k=i*nv+j;
                    b.Particles.Vertices[k]=new Point(b.MirrorRight*(x+u*ca-v*sa),y+u*sa+v*ca,z);b.Particles.Alpha[k]=alpha;
                }
                b.ParticleActive[i]=alpha>0;b.ParticleCenters[i]=new Point(b.MirrorRight*x,y,z);
            }
            b.LightEnvelope=LightEnvelope(b.Family,t);b.TargetAreaW=(water?38:45)*b.LightEnvelope;b.FloorAreaW=(water?14:16)*b.LightEnvelope;
            Point min=b.Core.Vertices[0],max=min;
            for(int m=0;m<b.Batches.Length;m++)for(int i=0;i<b.Batches[m].Vertices.Length;i++)
            {var p=b.Batches[m].Vertices[i];min.X=Math.Min(min.X,p.X);min.Y=Math.Min(min.Y,p.Y);min.Z=Math.Min(min.Z,p.Z);max.X=Math.Max(max.X,p.X);max.Y=Math.Max(max.Y,p.Y);max.Z=Math.Max(max.Z,p.Z);}
            b.SampleBounds=new Bounds(min,max);
        }
        public static bool SampleReceipt(Buffer b,double age,double availableA=NativeLead,int count=MaximumParticleCount)
        {
            double? native=NativeAge(age,availableA);Sample(b,native??-NativeLead,count);
            if(native.HasValue)return true;
            // Deterministically refresh every buffer, then suppress stale alpha/light on absent anticipation.
            for(int m=0;m<b.Batches.Length;m++)Array.Clear(b.Batches[m].Alpha,0,b.Batches[m].Alpha.Length);
            Array.Clear(b.ParticleActive,0,b.ParticleActive.Length);b.LightEnvelope=b.TargetAreaW=b.FloorAreaW=0;return false;
        }
    }
}
