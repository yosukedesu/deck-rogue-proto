#nullable enable
// Hd2dGeometryBridge.cs — dot の Godot 版 Project422 の NativeGeometryBridge (RecoveredSource/DeckRogue.GodotPort.Presentation/NativeGeometryBridge.cs) の写し (2026-11-02)。
// 経路 (選ばれた経路) から NativeEffect (層の束＋時刻で頂点を組み直す関数) を作る。選ばれた経路の工場 16 (Spore385・GreenGulp・LightBurst・CultistDarkBlade・MomentumWhip・
// SparkBurst・AetherLance・VineFormationCounter・CrossbowAimed・VineGrasp・WraithCurse・ColdSweep・ImpSpark・MirrorReturn・WarmSpark397・BlueContact379) と、
// それらが土台にする旧い幾何 (DeckRogue.Game の 25 本) の包み (Wrap) と Create の分岐 (BoundedContactFx・ScatterProfileFx・LocalMaterialProfileFx・FireContinuousFx・WaterContinuousFx・
// HealContinuousFx・VineSlashFx・EnemyContactFx・PlayerContactFx) を持ってきた。幾何と時間の数値は一切変えていない。
// 変えた所: using Godot → Hd2dMath、名前空間、Array.Clear(1引数)、EffectTier を Hd2dEffectTier に (Godot の EffectTier と同じ 3 段)。
// Unity では旧い描き手 (BoundedContactFx.cs ほか) が「元の経路」を描くので、ここの Create は選ばれた経路 (Hd2dOwnership.IsHd2dRenderer) の時だけ呼ばれる。
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Game.Hd2dMath;

namespace DeckRogue.Game.Hd2d
{
    internal enum Hd2dEffectTier { Small, Medium, Large }

    internal static class Hd2dGeometryBridge
    {
        private static NativeEffect WarmSpark397Material(NativeEffect effect)
        {
            // Preserve the original sampler for world-light envelopes and receipt age.
            // Only geometry/material change: original .19s endpoint and .05s core remain.
            var original=effect.Sample;
            var p=new Vector3[56];var uv=new Vector2[56];var alpha=new float[56];
            NativeBatch Batch(int offset,int count,int kind)=>new NativeBatch(){Count=count,Kind=kind,Space=0,Indices=Enumerable.Range(0,count/4).SelectMany(i=>new[]{i*4,i*4+1,i*4+2,i*4,i*4+2,i*4+3}).ToArray(),Position=i=>p[offset+i],Uv=i=>uv[offset+i],Alpha=i=>alpha[offset+i],Gain=i=>kind==0?1.05f:(i/4<2?1.30f:i/4<5?1.12f:.92f),Effect=i=>new Vector2(kind==0?0:i/4<2?1:i/4<5?2:3,0)};
            effect.Batches=new[]{Batch(0,8,0),Batch(8,48,1)};
            effect.Shader="WarmSpark397Surface";
            var velocity=new[]{new Vector2(-8.5f,3.0f),new Vector2(9.2f,2.2f),new Vector2(-1.85f,-.76f),new Vector2(1.9f,-1.0f),new Vector2(-1.05f,1.8f),new Vector2(.8f,1.45f),new Vector2(-2.2f,.20f),new Vector2(1.6f,.14f),new Vector2(-.9f,-1.1f),new Vector2(.52f,-.76f),new Vector2(-1.32f,.77f),new Vector2(1.22f,1.0f)};
            float[] lengths={.090f,.078f,.044f,.037f,.033f,.022f,.019f,.025f,.018f,.016f,.020f,.017f};
            float[] widths={.014f,.012f,.010f,.009f,.009f,.007f,.006f,.007f,.006f,.005f,.006f,.005f};
            void V(int i,Vector2 xy,Vector2 tex,float a){p[i]=new Vector3(xy.X,xy.Y,.03f);uv[i]=tex;alpha[i]=a;}
            void Fleck(int n,Vector2 center,Vector2 direction,float length,float width,float opacity){var normal=new Vector2(-direction.Y,direction.X);V(n,center-direction*length,new Vector2(0,.5f),opacity);V(n+1,center-direction*(length*.10f)-normal*width,new Vector2(.42f,0),opacity);V(n+2,center+direction*(length*.48f),new Vector2(1,.5f),opacity);V(n+3,center-direction*(length*.18f)+normal*(width*.72f),new Vector2(.39f,1),opacity);}
            effect.Sample=t=>{
                Array.Clear(alpha,0,alpha.Length);original(t);if(!double.IsFinite(t)||t<0||t>=.19)return;
                float core=t<=.05?(float)(.7*(1-ScatterProfileGeometry.Smooth((t-.008)/.042))):0;
                Fleck(0,new Vector2(-.018f,.012f),new Vector2(1,.22f).Normalized(),.077f,.011f,core*.82f);
                Fleck(4,new Vector2(-.026f,.014f),new Vector2(-.25f,1).Normalized(),.040f,.007f,core*.52f);
                for(int i=0;i<12;i++){
                    int k=i%4;double birth=.004*k,life=.16+.006*k,a=Math.Max(0,t-birth);
                    // v2 only the two main flecks: earlier outward launch, then strong braking.
                    float drag=i<2?22f:6f;float travel=(float)((1-Math.Exp(-drag*a))/drag);
                    var v=velocity[i];float bend=(i%2==0?1:-1)*(float)(.8*a*a);
                    var center=new Vector2(-.014f+.007f*(i%3-1),.012f+.006f*(i%4-1))+v*travel+new Vector2(bend,(float)(-.85*a*a));
                    if(i<2)center+=v.Normalized()*(float)(.12*ScatterProfileGeometry.Smooth(a/.025));
                    var tangent=(v+new Vector2((i%2==0?1:-1)*(float)(1.6*a),(float)(-1.7*a))).Normalized();
                    float release=(float)ScatterProfileGeometry.Smooth(a/life);
                    float opacity=t>=birth&&t<=birth+life?(float)(ScatterProfileGeometry.Smooth(a/.008)*(1-ScatterProfileGeometry.Smooth((a-life*.28)/(life*.72)))):0;
                    if(i<2&&t>=birth&&t<=birth+life)opacity=(float)(ScatterProfileGeometry.Smooth(a/.008)*(1-ScatterProfileGeometry.Smooth((a-life*.48)/(life*.52))));
                    opacity*=i<2?.98f:i<5?.82f:.58f;
                    Fleck(8+i*4,center,tangent,lengths[i]*(1-(i<2?.45f:.62f)*release),widths[i]*(1-.32f*release),opacity);
                }
            };
            return effect;
        }

        private static NativeEffect CreateSpore385Contact(){
            const int quads=8,n=quads*4;bool committed=false;float age=0;
            var p=new Vector3[n];var uv=new Vector2[n];var alpha=new float[n];var indices=new int[quads*6];
            for(int q=0;q<quads;q++){int v=q*4,k=q*6;indices[k]=v;indices[k+1]=v+1;indices[k+2]=v+2;indices[k+3]=v;indices[k+4]=v+2;indices[k+5]=v+3;}
            Vector2[] centers={Vector2.Zero,new Vector2(-.17f,.055f),new Vector2(-.09f,.15f),new Vector2(.035f,.10f),new Vector2(.17f,.055f),new Vector2(.105f,-.10f),new Vector2(-.035f,-.115f),new Vector2(-.20f,-.07f)};
            float[] sizes={.54f,.13f,.105f,.15f,.115f,.10f,.135f,.09f};
            var batch=new NativeBatch{Count=n,Kind=101,Space=0,Indices=indices,Position=i=>p[i],Uv=i=>uv[i],Alpha=i=>alpha[i],Gain=i=>1f,Effect=i=>new Vector2(i/4==0?0:1,i/4)};
            return new NativeEffect{Shader="ScatterProfileFx",Palette="scatter-profile-palette-r2",RouteFamily="scatter_spore",Family=3,Batches=new[]{batch},TargetPeak=0,FloorPeak=0,Light=()=>0,FloorLight=()=>0,VisualAge=()=>age,
             ConfigureSpore385CommittedContact=v=>committed=v,Sample=t=>{age=(float)t;Array.Clear(alpha,0,alpha.Length);if(!committed||!double.IsFinite(t)||t<0||t>=.32)return;
              float release=(float)t/.32f,fade=1-release*release*(3-2*release);
              for(int q=0;q<quads;q++){
               float size=sizes[q]*(1+.10f*release),angle=q==0?-.18f:(q*.91f-.4f),cs=MathF.Cos(angle),sn=MathF.Sin(angle);
               var center=centers[q]+(q==0?Vector2.Zero:new Vector2(.014f*MathF.Sin(q*2.3f)*release,.045f*release));
               float opacity=q==0?.20f*MathF.Max(0,1-(float)t/.14f):(.84f-.045f*(q%3))*fade;
               for(int k=0;k<4;k++){int j=q*4+k;float u=(k==1||k==2)?1:0,v=k<2?0:1;float x=(u-.5f)*size,y=(v-.5f)*size*(q==0?.68f:1.05f+.12f*(q%3));
                p[j]=new Vector3(center.X+x*cs-y*sn,center.Y+x*sn+y*cs,.035f+q*.001f);uv[j]=new Vector2(u,v);alpha[j]=opacity;
               }
              }
             }};
        }

        //381v2 receipt-onset compressed organic bite, then release only. No timing changes.
        private static NativeEffect CreateGreenGulpContact(){
            const int segments=12,count=segments*8;bool committed=false;float age=0;
            var pos=new Vector3[count];var uv=new Vector2[count];var al=new float[count];
            var roots=new Vector3[count];var ru=new Vector2[count];var ra=new float[count];
            var indices=new int[segments*12];for(int q=0;q<segments*2;q++){int v=q*4,n=q*6;indices[n]=v;indices[n+1]=v+1;indices[n+2]=v+2;indices[n+3]=v;indices[n+4]=v+2;indices[n+5]=v+3;}
            var fang=new NativeBatch{Count=count,Kind=91,Space=0,Texture="enemy-fang-row-r1",Indices=indices,Position=i=>pos[i],Uv=i=>uv[i],Alpha=i=>al[i],Gain=i=>1f,Effect=i=>new Vector2(91,0)};
            var root=new NativeBatch{Count=count,Kind=92,Space=0,Texture="canopy-organic-density-v1",Indices=indices,Position=i=>roots[i],Uv=i=>ru[i],Alpha=i=>ra[i],Gain=i=>1f,Effect=i=>new Vector2(92,0)};
            return new NativeEffect{Shader="BoundedContactFx",Palette="bounded-contact-palette-r1",Family=0,Batches=new[]{root,fang},TargetPeak=0,FloorPeak=0,VisualAge=()=>age,
              ConfigureGreenGulpCommittedContact=v=>committed=v,
              Sample=t=>{age=(float)t;Array.Clear(al,0,al.Length);Array.Clear(ra,0,ra.Length);if(!committed||!double.IsFinite(t)||t<0||t>=.18)return;
                float release=Math.Clamp(((float)t-.018f)/.162f,0,1),fade=1-release*release*(3-2*release);
                //Maximum compression exists at receipt onset. Later motion is tiny outward release,
                //never a second closing peak after the actual damage label.
                for(int j=0;j<2;j++)for(int k=0;k<segments;k++){
                  float sign=j==0?1:-1,width=j==0?.76f:.64f,shift=j==0?-.035f:.045f,tilt=j==0?-.10f:.16f;
                  int n=(j*segments+k)*4;
                  void Vertex(int i,float u,float v){
                    float edge=(float)Math.Sin(u*Math.PI),wave=.025f*(float)Math.Sin(9*u+j*1.7f)+.011f*(float)Math.Sin(23*u+.8f*j);
                    float x=(u-.5f)*width+shift;
                    float rootY=.135f+.045f*edge+wave;
                    float tipY=-.085f+.018f*(float)Math.Sin(8*u+j)+.018f*release;
                    float y=sign*(tipY+(rootY-tipY)*v)+tilt*x;
                    pos[i]=new Vector3(x,y,.032f+j*.002f);uv[i]=new Vector2(j==0?u:1-u,v);al[i]=.86f*fade;
                    //A narrow textured root follows the same irregular connected jaw contour.
                    roots[i]=new Vector3(x,sign*(rootY-.018f+v*(.065f+.014f*edge))+tilt*x,.025f);ru[i]=new Vector2(j==0?u:1-u,v);ra[i]=.72f*fade;
                  }
                  float u0=k/(float)segments,u1=(k+1)/(float)segments;
                  Vertex(n,u0,1);Vertex(n+1,u1,1);Vertex(n+2,u1,0);Vertex(n+3,u0,0);
                }
              }};
        }

        private static NativeEffect CreateLightBurstContact(AttackRoute route,Hd2dEffectTier tier)
        {
            var e=Create(route with{Renderer="LocalMaterialProfileFx"},tier)!;
            var oldSample=e.Sample;var oldSource=e.ConfigureSource;var oldBatches=e.Batches;
            bool committed=false;float age=0;Vector2 axis=Vector2.Right;
            var pos=new Vector3[64];var uv=new Vector2[64];var al=new float[64];
            var cp=new Vector3[4];var cu=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};var ca=new float[4];
            int[] indices=new int[96];for(int q=0;q<16;q++){int v=q*4,n=q*6;indices[n]=v;indices[n+1]=v+1;indices[n+2]=v+2;indices[n+3]=v;indices[n+4]=v+2;indices[n+5]=v+3;}
            var main=new NativeBatch{Count=64,Kind=41,Space=0,Indices=indices,Texture="light-burst-pressure-density-v1",
                Position=i=>pos[i],Uv=i=>uv[i],Alpha=i=>al[i],Gain=i=>2.15f,Role=i=>1,Effect=i=>new Vector2(0,1)};
            var core=new NativeBatch{Count=4,Kind=42,Space=0,Indices=new[]{0,1,2,0,2,3},
                Position=i=>cp[i],Uv=i=>cu[i],Alpha=i=>ca[i],Gain=i=>2.7f,Role=i=>3,Effect=i=>new Vector2(0,3)};
            var particles=oldBatches[2];var oldParticleAlpha=particles.Alpha;
            particles.Alpha=i=>committed&&float.IsFinite(age)&&age>=0?oldParticleAlpha(i):0;
            e.Batches=new[]{main,core,particles};
            e.ConfigureSource=v=>{oldSource?.Invoke(v);var a=new Vector2(-v.X,-v.Y);axis=a.LengthSquared()>.0001f?a.Normalized():Vector2.Right;};
            e.ConfigureLightBurstCommittedContact=v=>committed=v;
            e.Sample=t=>{
                age=(float)t;Array.Clear(al,0,al.Length);Array.Clear(ca,0,ca.Length);
                if(!double.IsFinite(t))return;
                oldSample(t);if(!committed||t<0||t>=.172)return;
                double fade=1-SmoothMirror((t-.035)/.137),front=SmoothMirror((t-.025)/.115)*.84;
                Vector3 At(double along,double across,double z)=>new Vector3((float)(axis.X*along-axis.Y*across),(float)(axis.Y*along+axis.X*across),(float)z);
                void Vertex(int n,double u,double side){
                    double belly=Math.Pow(Math.Max(0,Math.Sin(Math.PI*u)),.70);
                    double width=(.009+.16*belly*(1-.4*u))*(1-.32*SmoothMirror((t-.04)/.12));
                    double along=u<=.625 ? -.48+.592*u : -.11+.145*((u-.625)/.375);
                    double across=.035*Math.Sin(Math.PI*u)+side*width*(side>0?1:.55);
                    double density=.74*fade*SmoothMirror((u-front)/.14)*(.45+.55*belly)*(side==0?1:side>0?.48:.28);
                    pos[n]=At(along,across,.030);uv[n]=new Vector2((float)u,(float)((side+1)*.5));al[n]=(float)density;
                }
                for(int k=0;k<8;k++)for(int j=0;j<2;j++){int n=(k*2+j)*4;double u=k/8.0,v=(k+1)/8.0;Vertex(n,u,j-1);Vertex(n+1,v,j-1);Vertex(n+2,v,j);Vertex(n+3,u,j);}
                double peak=1-SmoothMirror((t-.014)/.060);
                cp[0]=At(-.09,-.018,.080);cp[1]=At(.035,-.010,.080);cp[2]=At(.020,.015,.080);cp[3]=At(-.075,.022,.080);
                for(int i=0;i<4;i++)ca[i]=(float)(.62*peak);
            };
            //Original LocalLight Light/FloorLight/peaks/palette/age semantics remain; only private body/core replaced.
            return e;
        }

        //359 full authored A bbox; one planar receiving mass and unchanged old slash motes.
        private static NativeEffect CreateCultistDarkBladeContact()
        {
            var old=new BoundedContactGeometry.Buffer();var profile=BoundedContactGeometry.Find("dark_slash_contact");
            var xyz=new Vector3[160];var uv=new Vector2[160];var alpha=new float[160];var role=new float[160];
            int motes=3;bool committed=false;float sampledAge=0;
            var batch=new NativeBatch{Count=160,Indices=old.Triangles,Kind=81,Texture="cultist-dark-pressure-v1",
                Position=i=>xyz[i],Uv=i=>uv[i],Alpha=i=>alpha[i],Gain=i=>1f,Effect=i=>new Vector2(role[i],0)};
            return new NativeEffect{Shader="BoundedContactFx",Palette="bounded-contact-palette-r1",Family=(int)BoundedContactGeometry.Surface.Dark,
                Batches=new[]{batch},TargetPeak=0,FloorPeak=0,VisualAge=()=>sampledAge,
                ConfigureParticleGain=g=>motes=BoundedContactGeometry.MoteCount(g),
                ConfigureCultistDarkBladeCommittedContact=value=>committed=value,
                Sample=t=>{
                    sampledAge=(float)t;Array.Clear(alpha,0,alpha.Length);BoundedContactGeometry.Sample(old,profile,t,motes);
                    if(!committed||!BoundedContactGeometry.Finite(t)||t<0||t>=BoundedContactGeometry.End)return;
                    int count=Math.Max(0,old.Quads*4-40);
                    for(int i=0;i<count;i++){int src=i+40,dst=i+80;xyz[dst]=new Vector3((float)old.X[src],(float)old.Y[src],0);
                        uv[dst]=new Vector2((float)old.U[src],(float)old.V[src]);alpha[dst]=(float)old.Alpha[src];role[dst]=(float)old.Soft[src];}
                    float q=(float)(t/.18),fade=(1-q)*(1-q),release=Math.Clamp((float)((t-.055)/.095),0f,1f);
                    // One envelope replaces old main fade after .020; never multiplies it twice.
                    float mainAlpha=t<=.020?.88f*fade:(.88f*(1f-.020f/.18f)*(1f-.020f/.18f))*(1f-release*release*(3f-2f*release));
                    const float unit=.72f/1588f;
                    Vector3 P(float u,float vBottom)=>new Vector3((33f+1588f*u-110f)*unit,(840f-(23f+874f*(1-vBottom)))*unit,0);
                    int n=0;
                    for(int y=0;y<4;y++)for(int z=0;z<4;z++){
                        float u0=z/4f,u1=(z+1)/4f,v0=y/4f,v1=(y+1)/4f;
                        xyz[n]=P(u0,v0);xyz[n+1]=P(u1,v0);xyz[n+2]=P(u1,v1);xyz[n+3]=P(u0,v1);
                        uv[n]=new Vector2(u0,v0);uv[n+1]=new Vector2(u1,v0);uv[n+2]=new Vector2(u1,v1);uv[n+3]=new Vector2(u0,v1);
                        for(int j=0;j<4;j++){alpha[n+j]=mainAlpha;role[n+j]=81f;}n+=4;
                    }
                }};
        }

        //346: one exact owned neutral reaction counter; old shared bounded sampler supplies only unchanged motes.
        //356 full authored alpha-bbox, rigid plane; no old wave, separate core or geometry pulse.
        private static NativeEffect CreateMomentumWhipContact()
        {
            var old=new BoundedContactGeometry.Buffer();var profile=BoundedContactGeometry.Find("neutral_whip_contact");
            var xyz=new Vector3[160];var uv=new Vector2[160];var alpha=new float[160];var role=new float[160];
            int motes=2;bool committed=false;
            var batch=new NativeBatch{Count=160,Indices=old.Triangles,Kind=71,Texture="momentum-whip-pressure-v1",
                Position=i=>xyz[i],Uv=i=>uv[i],Alpha=i=>alpha[i],Gain=i=>1f,Effect=i=>new Vector2(role[i],0)};
            return new NativeEffect{Shader="BoundedContactFx",Palette="bounded-contact-palette-r1",Family=0,
                Batches=new[]{batch},TargetPeak=0,FloorPeak=0,
                ConfigureParticleGain=g=>motes=BoundedContactGeometry.MoteCount(g),
                ConfigureMomentumWhipCommittedContact=value=>committed=value,
                Sample=t=>{
                    Array.Clear(alpha,0,alpha.Length);BoundedContactGeometry.Sample(old,profile,t,motes);
                    if(!committed||!BoundedContactGeometry.Finite(t)||t<0||t>=BoundedContactGeometry.End)return;
                    int count=Math.Max(0,old.Quads*4-48);
                    for(int i=0;i<count;i++){int src=i+48,dst=i+80;xyz[dst]=new Vector3((float)old.X[src],(float)old.Y[src],0);
                        uv[dst]=new Vector2((float)old.U[src],(float)old.V[src]);alpha[dst]=(float)old.Alpha[src];role[dst]=(float)old.Soft[src];}
                    float q=(float)(t/BoundedContactGeometry.End),fade=(1-q)*(1-q);
                    //357 main-only envelope; old mote sample remains untouched. Continuous, not C1 at .020.
                    float release=Math.Clamp((float)((t-.055)/.095),0f,1f);
                    float mainAlpha=t<=.020?.8f*fade:(.8f*(1f-.020f/.18f)*(1f-.020f/.18f))*(1f-release*release*(3f-2f*release));
                    const float unit=(66f/1605f)/151.6518f;
                    const float c=.9271838546f,ss=.3746065934f; //screen +22deg; basis local Y points up.
                    Vector3 P(float u,float vBottom){float dx=(1605f*u-1440f)*unit,dy=(660f-(12f+959f*(1-vBottom)))*unit;
                        return new Vector3(c*dx+ss*dy,-ss*dx+c*dy,0);}
                    int n=0;
                    for(int y=0;y<4;y++)for(int z=0;z<4;z++){
                        float u0=z/4f,u1=(z+1)/4f,v0=y/4f,v1=(y+1)/4f;
                        xyz[n]=P(u0,v0);xyz[n+1]=P(u1,v0);xyz[n+2]=P(u1,v1);xyz[n+3]=P(u0,v1);
                        uv[n]=new Vector2(u0,v0);uv[n+1]=new Vector2(u1,v0);uv[n+2]=new Vector2(u1,v1);uv[n+3]=new Vector2(u0,v1);
                        for(int j=0;j<4;j++){alpha[n+j]=mainAlpha;role[n+j]=71f;}n+=4;
                    }
                }};
        }

        //352: entire authored A alpha-bbox; high-alpha contact texel maps to the same frozen root.
        private static NativeEffect CreateSparkBurstContact(AttackRoute route,Hd2dEffectTier tier)
        {
            var e=Create(route with{Renderer="ScatterProfileFx"},tier)!;var oldSample=e.Sample;
            bool committed=false;var xyz=new Vector3[80];var uv=new Vector2[80];var alpha=new float[80];var role=new float[80];
            var indices=new List<int>();for(int i=0;i<18;i++)indices.Add(i);
            for(int i=18;i<70;i+=4){indices.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});}
            var batch=new NativeBatch{Count=80,Kind=61,Space=0,Indices=indices.ToArray(),Position=i=>xyz[i],Uv=i=>uv[i],Alpha=i=>alpha[i],Gain=i=>i<22?1.7f:1.45f,Effect=i=>new Vector2(role[i],0)};
            e.Batches=new[]{batch};e.ConfigureSparkBurstCommittedContact=v=>committed=v;
            var rim=new[]{new Vector2(0,0),new Vector2(-.028f,-.018f),new Vector2(-.076f,-.008f),new Vector2(-.10f,.027f),new Vector2(-.062f,.055f),new Vector2(-.012f,.037f)};
            var start=new[]{new Vector2(-.10f,.055f),new Vector2(-.075f,.085f),new Vector2(-.04f,.025f),new Vector2(-.025f,-.005f),new Vector2(-.012f,.018f),new Vector2(.008f,.012f),new Vector2(-.018f,-.01f),new Vector2(.015f,-.006f),new Vector2(-.027f,.03f),new Vector2(-.005f,.04f),new Vector2(.005f,-.02f),new Vector2(-.035f,.008f)};
            var velocity=new[]{new Vector2(-3,.85f),new Vector2(-1.32f,1.11f),new Vector2(-2,-.25f),new Vector2(1.6f,.45f),new Vector2(-1.1f,1.05f),new Vector2(.9f,.8f),new Vector2(-1.7f,-.65f),new Vector2(1.3f,-.7f),new Vector2(-.7f,.7f),new Vector2(.55f,1.2f),new Vector2(.65f,-.9f),new Vector2(-1.2f,.2f)};
            double[] lengths={.041,.036,.027,.025,.014,.012,.011,.015,.013,.010,.012,.014},widths={.018,.016,.012,.011,.005,.004,.004,.005,.0045,.0035,.004,.0045};
            double[] births={0,.002,.004,.006,.004,.008,.012,.008,.012,.004,.008,.012},lives={.17,.18,.16,.17,.14,.15,.15,.16,.14,.16,.15,.17};
            e.Sample=t=>{
                Array.Clear(alpha,0,alpha.Length);if(!double.IsFinite(t))return;oldSample(t);if(!committed||t<0||t>=.19)return;
                float core=t<=.05?(float)(.7*(1-SmoothMirror((t-.020)/.030))):0;
                void Put(int n,Vector2 p,Vector2 tex,float a,float r){xyz[n]=new Vector3(p.X,p.Y,.02f);uv[n]=tex;alpha[n]=a;role[n]=r;}
                Vector2 CoreUv(Vector2 p)=>new Vector2((p.X+.10f)/.10f,(p.Y+.018f)/.073f);
                var center=new Vector2(-.046f,.016f);
                for(int i=0;i<6;i++){int n=i*3;Put(n,center,CoreUv(center),core,61);Put(n+1,rim[i],CoreUv(rim[i]),core,61);Put(n+2,rim[(i+1)%6],CoreUv(rim[(i+1)%6]),core,61);}
                var a0=new Vector2(-.008f,.010f);var a1=new Vector2(-.049f,.032f);var tangent=(a1-a0).Normalized();var normal=new Vector2(-tangent.Y,tangent.X);
                Put(18,a0-normal*.008f,new Vector2(0,0),core*.85f,62);Put(19,a1-normal*.004f,new Vector2(1,0),core*.85f,62);Put(20,a1+normal*.004f,new Vector2(1,1),core*.85f,62);Put(21,a0+normal*.008f,new Vector2(0,1),core*.85f,62);
                for(int i=0;i<12;i++){
                    double a=Math.Max(0,t-births[i]),life=lives[i],travel=(1-Math.Exp(-9*a))/9;
                    var c=start[i]+velocity[i]*(float)travel+new Vector2(0,(float)(-.25*a*a));var dir=new Vector2(velocity[i].X,velocity[i].Y-(float)(.5*a)).Normalized();var side=new Vector2(-dir.Y,dir.X);
                    float len=(float)(lengths[i]*(1-.60*SmoothMirror(a/life))),width=(float)(widths[i]*(1-.20*SmoothMirror(a/life)));
                    float al=t>=births[i]&&t<=births[i]+life?(float)(SmoothMirror(a/.008)*(1-SmoothMirror((a-life*.28)/(life*.72)))*(i<2?1:i<4?.85:.55)):0;
                    int n=22+i*4;
                    Put(n,c-dir*len,new Vector2(0,.5f),al,63);Put(n+1,c-dir*(len*.16f)-side*width,new Vector2(.42f,0),al,63);Put(n+2,c+dir*len,new Vector2(1,.5f),al,63);Put(n+3,c-dir*(len*.10f)+side*width,new Vector2(.45f,1),al,63);
                }
            };
            return e; // old Scatter sampler retains exactly the old light envelope/peaks/palette.
        }

        private static NativeEffect CreateAetherLanceContact()
        {
            var old=new BoundedContactGeometry.Buffer();var profile=BoundedContactGeometry.Find("neutral_point_contact");
            var xyz=new Vector3[160];var uv=new Vector2[160];var alpha=new float[160];var role=new float[160];
            int motes=3;bool committed=false;
            var batch=new NativeBatch{Count=160,Indices=old.Triangles,Kind=51,Texture="aether-piercing-pressure-v1",
                Position=i=>xyz[i],Uv=i=>uv[i],Alpha=i=>alpha[i],Gain=i=>1f,Effect=i=>new Vector2(role[i],0)};
            return new NativeEffect{Shader="BoundedContactFx",Palette="bounded-contact-palette-r1",Family=0,
                Batches=new[]{batch},TargetPeak=0,FloorPeak=0,
                ConfigureParticleGain=g=>motes=BoundedContactGeometry.MoteCount(g),
                ConfigureAetherLanceCommittedContact=value=>committed=value,
                Sample=t=>{
                    Array.Clear(alpha,0,alpha.Length);BoundedContactGeometry.Sample(old,profile,t,motes);
                    if(!committed||!BoundedContactGeometry.Finite(t)||t<0||t>=BoundedContactGeometry.End)return;
                    int count=Math.Max(0,old.Quads*4-4);
                    for(int i=0;i<count;i++){int src=i+4,dst=i+80;xyz[dst]=new Vector3((float)old.X[src],(float)old.Y[src],0);
                        uv[dst]=new Vector2((float)old.U[src],(float)old.V[src]);alpha[dst]=(float)old.Alpha[src];role[dst]=(float)old.Soft[src];}
                    float q=(float)(t/BoundedContactGeometry.End),fade=(1-q)*(1-q);
                    const float rootU=(1460f-48f)/1472f,rootVTop=(556f-182f)/664f;
                    // Fixed initial main extent, deliberately not old geometry pulse. Old mote sample is unchanged.
                    Vector3 P(float u,float vBottom)=>new Vector3((u-rootU)*.46f,
                        (vBottom-(1f-rootVTop))*.21f+(rootU-u)*.08f,0);
                    int n=0;
                    for(int y=0;y<2;y++)for(int z=0;z<8;z++){
                        float u0=z/8f,u1=(z+1)/8f,v0=y/2f,v1=(y+1)/2f;
                        xyz[n]=P(u0,v0);xyz[n+1]=P(u1,v0);xyz[n+2]=P(u1,v1);xyz[n+3]=P(u0,v1);
                        uv[n]=new Vector2(u0,v0);uv[n+1]=new Vector2(u1,v0);uv[n+2]=new Vector2(u1,v1);uv[n+3]=new Vector2(u0,v1);
                        for(int j=0;j<4;j++){alpha[n+j]=.9f*fade;role[n+j]=58f;}n+=4;
                    }
                }};
        }

        private static NativeEffect CreateVineFormationCounter()
        {
            var old=new BoundedContactGeometry.Buffer();
            var profile=BoundedContactGeometry.Find("minimal_neutral_contact");
            var xyz=new Vector3[160];var uv=new Vector2[160];var alpha=new float[160];var role=new float[160];var pressureU=new float[160];
            int motes=3;bool committed=false;
            var batch=new NativeBatch{Count=160,Indices=old.Triangles,Kind=41,
                Position=i=>xyz[i],Uv=i=>uv[i],Alpha=i=>alpha[i],Gain=i=>1f,Effect=i=>new Vector2(role[i],pressureU[i])};
            return new NativeEffect{Shader="BoundedContactFx",Palette="bounded-contact-palette-r1",Family=0,
                Batches=new[]{batch},ConfigureParticleGain=g=>motes=BoundedContactGeometry.MoteCount(g),
                ConfigureVineFormationCounterCommittedContact=value=>committed=value,
                Sample=t=>{
                    Array.Clear(alpha,0,alpha.Length);BoundedContactGeometry.Sample(old,profile,t,motes);
                    if(!committed || !BoundedContactGeometry.Finite(t) || t<0 || t>=BoundedContactGeometry.End)return;
                    // Preserve old positive mote coordinates/UV/alpha/soft role; body allocation remains below80.
                    int count=Math.Max(0,old.Quads*4-4);
                    for(int i=0;i<count;i++){int src=i+4,dst=i+80;xyz[dst]=new Vector3((float)old.X[src],(float)old.Y[src],0);
                        uv[dst]=new Vector2((float)old.U[src],(float)old.V[src]);alpha[dst]=(float)old.Alpha[src];role[dst]=(float)old.Soft[src];}
                    float q=(float)(t/BoundedContactGeometry.End),fade=(1-q)*(1-q);
                    float pulse=.8f+.2f*MathF.Sin(MathF.PI*Math.Min(1f,q*2f));int n=0;
                    Vector3 V(float x,float y)=>new Vector3(x*pulse,y*pulse,0);
                    void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float a0,float kind,float u0=0,float u1=1){
                        if(n+4>80)throw new InvalidOperationException("Vine counter body capacity");
                        xyz[n]=a;xyz[n+1]=b;xyz[n+2]=c;xyz[n+3]=d;
                        uv[n]=new Vector2(u0,0);uv[n+1]=new Vector2(u1,0);uv[n+2]=new Vector2(u1,1);uv[n+3]=new Vector2(u0,1);
                        for(int j=0;j<4;j++){alpha[n+j]=a0;role[n+j]=kind;pressureU[n+j]=kind==41?uv[n+j].X:0f;}n+=4;
                    }
                    //349: one nonphysical green counter-pressure curve, not a small neutral plate.
                    Vector3 Curve(float u)=>V(-.32f*(1-u)*u-.38f*u*u,.48f*(1-u)*u+.10f*u*u);
                    Vector3 Normal(float u){var tangent=new Vector3(-.32f-.12f*u,.48f-.76f*u,0).Normalized();return new Vector3(-tangent.Y,tangent.X,0);}
                    float Width(float u)=>pulse*(.018f*(1-u)+.071f*MathF.Exp(-MathF.Pow((u-.43f)/.23f,2)))*(1f-.85f*MathF.Pow(u,6));
                    for(int i=0;i<12;i++){
                        float u0=i/12f,u1=(i+1)/12f;var a=Curve(u0);var b=Curve(u1);var na=Normal(u0);var nb=Normal(u1);float wa=Width(u0),wb=Width(u1);
                        Quad(a-na*wa*.75f,b-nb*wb*.75f,b+nb*wb*1.1f,a+na*wa*1.1f,.76f*fade,41,u0,u1);
                    }
                    // One short broken yellow-green contact crease; never a complete blade outline.
                    void Edge(float u0,float u1,float wa,float wb){var a=Curve(u0)+Normal(u0)*Width(u0)*.22f;var b=Curve(u1)+Normal(u1)*Width(u1)*.22f;
                        var tangent=(b-a).Normalized();var normal=new Vector3(-tangent.Y,tangent.X,0);
                        Quad(a-normal*wa*pulse,b-normal*wb*pulse,b+normal*wb*pulse,a+normal*wa*pulse,.65f*fade,42,u0,u1);}
                    Edge(.21f,.31f,.005f,.014f);Edge(.31f,.40f,.014f,.010f);Edge(.40f,.47f,.010f,.002f);
                }};
        }

        private static NativeEffect CreateCrossbowAimedContact()
        {
            var b=new BoundedContactGeometry.Buffer();var profile=BoundedContactGeometry.Find("neutral_projectile_contact");
            int motes=3;Vector2 axis=Vector2.Right;
            var batch=new NativeBatch{Count=b.X.Length,Indices=b.Triangles,
                Position=i=>new Vector3((float)b.X[i],(float)b.Y[i],0),Uv=i=>new Vector2((float)b.U[i],(float)b.V[i]),
                Alpha=i=>(float)b.Alpha[i],Gain=i=>1f,Effect=i=>new Vector2((float)b.Soft[i],i<64?1:i<72?2:i<80?3:0)};
            return new NativeEffect{Batches=new[]{batch},Shader="BoundedContactFx",Palette="bounded-contact-palette-r1",
                Texture="crossbow-pressure-density-v1",
                Family=(int)BoundedContactGeometry.Surface.Neutral,RouteFamily="neutral_projectile_contact",
                ConfigureParticleGain=g=>motes=BoundedContactGeometry.MoteCount(g),
                ConfigureSource=v=>{var a=new Vector2(-v.X,-v.Y);axis=a.LengthSquared()>.0001f?a.Normalized():Vector2.Right;},
                Sample=t=>{
                    BoundedContactGeometry.Sample(b,profile,t,motes);
                    if(!BoundedContactGeometry.Finite(t)||t<0||t>=BoundedContactGeometry.End)return;
                    int particleVertices=b.Particles*4;
                    Array.Copy(b.X,4,b.X,80,particleVertices);Array.Copy(b.Y,4,b.Y,80,particleVertices);
                    Array.Copy(b.U,4,b.U,80,particleVertices);Array.Copy(b.V,4,b.V,80,particleVertices);
                    Array.Copy(b.Alpha,4,b.Alpha,80,particleVertices);Array.Copy(b.Soft,4,b.Soft,80,particleVertices);
                    Array.Clear(b.Alpha,0,80);b.Quads=20+b.Particles;
                    double q=t/BoundedContactGeometry.End,fade=1-SmoothMirror((t-.035)/.115);
                    void Put(int n,double along,double across,double u,double v,double alpha){
                        b.X[n]=axis.X*along-axis.Y*across;b.Y[n]=axis.Y*along+axis.X*across;
                        b.U[n]=u;b.V[n]=v;b.Alpha[n]=alpha;b.Soft[n]=0;
                    }
                    void Shoulder(int n,double u,double side){
                        //318 external air is subordinate. The actual chest crease is the receiving primary.
                        double belly=Math.Pow(Math.Max(0,Math.Sin(Math.PI*u)),.70);
                        double along=-.45*(1-u)+.015*q*(1-u);
                        double center=.025*(1-u);
                        double width=(.008+.095*belly)*(1-.2*q);
                        double sideWidth=side<0?.55:.80;
                        double cross=side<0?.18:side>0?.32:1;
                        double alpha=.20*fade*SmoothMirror(u/.12)*(1-.48*SmoothMirror((u-.82)/.18))*cross;
                        Put(n,along,center+side*width*sideWidth,u,(side+1)*.5,alpha);
                    }
                    for(int segment=0;segment<8;segment++)for(int row=0;row<2;row++){
                        int n=(segment*2+row)*4;double u=segment/8.0,v=(segment+1)/8.0;
                        Shoulder(n,u,row-1);Shoulder(n+1,v,row-1);Shoulder(n+2,v,row);Shoulder(n+3,u,row);
                    }
                    void Quad(int n,double cx,double cy,double halfX,double halfY,double tilt,double alpha){
                        Put(n,cx-halfX,cy-halfY-tilt,0,0,alpha);
                        Put(n+1,cx+halfX,cy-halfY+tilt,1,0,alpha);
                        Put(n+2,cx+halfX*.65,cy+halfY+tilt,1,1,alpha);
                        Put(n+3,cx-halfX*.85,cy+halfY-tilt,0,1,alpha);
                    }
                    double core=1-SmoothMirror(t/.075);
                    Quad(64,-.040,0,.075,.065,.020,.88*core);
                    Quad(68,-.120,.025,.025,.015,-.008,.40*core);
                    double residue=SmoothMirror(t/.025)*(1-SmoothMirror((t-.035)/.105));
                    Quad(72,-.20-.04*q,-.055-.025*q,.047,.012,.018,.32*residue);
                    Quad(76,-.09-.02*q,.095+.02*q,.025,.009,-.012,.20*residue);
                }};
        }

        private static NativeEffect CreateVineGraspContact()
        {
            var b=new BoundedContactGeometry.Buffer();var profile=BoundedContactGeometry.Find("plant_bind_contact");
            int motes=3;Vector2 axis=Vector2.Right;
            //331 open the positive half at two unequal adjacent sections. Opposing half remains continuous.
            var indices=new List<int>(228);
            for(int quad=0;quad<40;quad++){if(quad==5||quad==7)continue;for(int k=0;k<6;k++)indices.Add(b.Triangles[quad*6+k]);}

            var batch=new NativeBatch{Count=b.X.Length,Indices=indices.ToArray(),
                Position=i=>new Vector3((float)b.X[i],(float)b.Y[i],0),Uv=i=>new Vector2((float)b.U[i],(float)b.V[i]),
                Alpha=i=>(float)b.Alpha[i],Gain=i=>1f,Effect=i=>new Vector2((float)b.Soft[i],i<64?1:0)};
            return new NativeEffect{Batches=new[]{batch},Shader="BoundedContactFx",Palette="bounded-contact-palette-r1",
                TargetPeak=0,FloorPeak=0,Texture="canopy-organic-density-v1",Family=(int)BoundedContactGeometry.Surface.Plant,RouteFamily="plant_bind_contact",
                ConfigureParticleGain=g=>motes=BoundedContactGeometry.MoteCount(g),
                ConfigureSource=v=>{var a=new Vector2(-v.X,-v.Y);float length=a.LengthSquared();axis=a.IsFinite()&&float.IsFinite(length)&&length>.0001f?a.Normalized():Vector2.Right;},
                Sample=t=>{
                    BoundedContactGeometry.Sample(b,profile,t,motes);
                    if(!BoundedContactGeometry.Finite(t)||t<0||t>=BoundedContactGeometry.End)return;
                    int particleVertices=b.Particles*4;
                    //Old Bind has exactly20 arc quads before motes. Copy all6 arrays before clearing.
                    Array.Copy(b.X,80,b.X,64,particleVertices);Array.Copy(b.Y,80,b.Y,64,particleVertices);
                    Array.Copy(b.U,80,b.U,64,particleVertices);Array.Copy(b.V,80,b.V,64,particleVertices);
                    Array.Copy(b.Alpha,80,b.Alpha,64,particleVertices);Array.Copy(b.Soft,80,b.Soft,64,particleVertices);
                    Array.Clear(b.Alpha,0,64);Array.Clear(b.Alpha,64+particleVertices,b.Alpha.Length-64-particleVertices);b.Quads=16+b.Particles;
                    double q=t/BoundedContactGeometry.End,fade=(1-q)*(1-q);
                    void Vertex(int n,double u,double side){
                        double belly=Math.Pow(Math.Max(0,Math.Sin(Math.PI*u)),.70);
                        double along=-.90+.30*u+.60*u*u*u+.04*q*(1-u);
                        double center=-.025*(1-u)-.03*Math.Sin(Math.PI*u);
                        double middle=(u-.64)/.25;
                        double width=(.010+.25*Math.Exp(-middle*middle)*Math.Pow(Math.Max(0,Math.Sin(Math.PI*u)),.35))*(1-.25*q);
                        double notch=(u-.50)/.18;
                        double sideWidth=side<0?.85:(.70-.48*Math.Exp(-notch*notch));
                        double across=center+side*width*sideWidth;
                        b.X[n]=axis.X*along-axis.Y*across;b.Y[n]=axis.Y*along+axis.X*across;
                        b.U[n]=u;b.V[n]=(side+1)*.5;b.Soft[n]=0;
                        b.Alpha[n]=.75*fade*SmoothMirror(u/.12)*(side<0?.42:side>0?.25:1);
                    }
                    for(int segment=0;segment<8;segment++)for(int row=0;row<2;row++){
                        int n=(segment*2+row)*4;double u=segment/8.0,v=(segment+1)/8.0;
                        Vertex(n,u,row-1);Vertex(n+1,v,row-1);Vertex(n+2,v,row);Vertex(n+3,u,row);
                    }
                }};
        }

        private static NativeEffect CreateWraithCurseContact()
        {
            var b=new BoundedContactGeometry.Buffer();var profile=BoundedContactGeometry.Find("local_curse_contact");
            int motes=3;Vector2 axis=Vector2.Right;
            var batch=new NativeBatch{Count=b.X.Length,Indices=b.Triangles,
                Position=i=>new Vector3((float)b.X[i],(float)b.Y[i],0),Uv=i=>new Vector2((float)b.U[i],(float)b.V[i]),
                Alpha=i=>(float)b.Alpha[i],Gain=i=>1f,Effect=i=>new Vector2((float)b.Soft[i],i<64?1:0)};
            return new NativeEffect{Batches=new[]{batch},Shader="BoundedContactFx",Palette="bounded-contact-palette-r1",
                Texture="wraith-pressure-density-v1",Family=(int)BoundedContactGeometry.Surface.Dark,RouteFamily="local_curse_contact",
                ConfigureParticleGain=g=>motes=BoundedContactGeometry.MoteCount(g),
                ConfigureSource=v=>{var a=new Vector2(-v.X,-v.Y);axis=a.LengthSquared()>.0001f?a.Normalized():Vector2.Right;},
                Sample=t=>{
                    BoundedContactGeometry.Sample(b,profile,t,motes);
                    if(!BoundedContactGeometry.Finite(t)||t<0||t>=BoundedContactGeometry.End)return;
                    int particleVertices=b.Particles*4;
                    Array.Copy(b.X,4,b.X,64,particleVertices);Array.Copy(b.Y,4,b.Y,64,particleVertices);
                    Array.Copy(b.U,4,b.U,64,particleVertices);Array.Copy(b.V,4,b.V,64,particleVertices);
                    Array.Copy(b.Alpha,4,b.Alpha,64,particleVertices);Array.Copy(b.Soft,4,b.Soft,64,particleVertices);
                    Array.Clear(b.Alpha,0,64);b.Quads=16+b.Particles;
                    double q=t/BoundedContactGeometry.End;double fade=1-SmoothMirror((t-.055)/.12);
                    void Put(int n,double along,double across,double u,double v,double alpha){
                        b.X[n]=axis.X*along-axis.Y*across;b.Y[n]=axis.Y*along+axis.X*across;
                        b.U[n]=u;b.V[n]=v;b.Alpha[n]=alpha;b.Soft[n]=0;
                    }
                    //311 one continuous density field: unequal external belly folds down into the real contact.
                    //No separate darkened root role, detached orb, or new effect origin.
                    void Vertex(int n,double u,double side){
                        double belly=Math.Pow(Math.Max(0,Math.Sin(Math.PI*u)),.70);
                        double along=-.94*(1-u)+.025*q*(1-u);
                        double center=-.30*(1-u)-.075*Math.Sin(Math.PI*u);
                        double narrowing=1-.75*SmoothMirror((u-.32)/.60);
                        double width=(.008+.26*belly*narrowing)*(1-.18*q);
                        double notch=(u-.50)/.16;
                        double sideWidth=side<0?(.72-.62*Math.Exp(-notch*notch)):.95;
                        double across=center+side*width*sideWidth;
                        double cross=side<0?.15:side>0?.35:1;
                        double density=.84-.54*SmoothMirror((u-.38)/.62);
                        double alpha=density*fade*SmoothMirror(u/.12)*cross;
                        Put(n,along,across,u,(side+1)*.5,alpha);
                    }
                    for(int segment=0;segment<8;segment++)for(int row=0;row<2;row++){
                        int n=(segment*2+row)*4;double u=segment/8.0,v=(segment+1)/8.0;
                        Vertex(n,u,row-1);Vertex(n+1,v,row-1);Vertex(n+2,v,row);Vertex(n+3,u,row);
                    }
                    //All64 main vertices share310 authored material; original motes remain at64 unchanged.
                }};
        }

        private static NativeEffect CreateColdSweepContact()
        {
            var b=new BoundedContactGeometry.Buffer();var profile=BoundedContactGeometry.Find("cold_sweep_contact");
            int motes=3;bool committed=false;Vector2 axis=Vector2.Right;
            var batch=new NativeBatch{Count=b.X.Length,Indices=b.Triangles,
                Position=i=>new Vector3((float)b.X[i],(float)b.Y[i],0),Uv=i=>new Vector2((float)b.U[i],(float)b.V[i]),
                Alpha=i=>(float)b.Alpha[i],Gain=i=>1f,Effect=i=>new Vector2((float)b.Soft[i],i<64?1:i<72?2:i<80?3:0)};
            return new NativeEffect{Batches=new[]{batch},Shader="ColdSweepSurfaceFx",Palette="bounded-contact-palette-r1",Texture="cold-frost-density-v1",
                Family=(int)BoundedContactGeometry.Surface.Cold,RouteFamily="cold_sweep_contact",TargetPeak=0,FloorPeak=0,
                ConfigureParticleGain=g=>motes=BoundedContactGeometry.MoteCount(g),
                ConfigureSource=v=>{var a=new Vector2(-v.X,-v.Y);axis=a.LengthSquared()>.0001f?a.Normalized():Vector2.Right;},
                ConfigureColdSweepCommittedContact=v=>committed=v,
                Sample=t=>{
                    BoundedContactGeometry.Sample(b,profile,t,motes);
                    if(!committed){Array.Clear(b.Alpha,0,b.Alpha.Length);return;}
                    if(!BoundedContactGeometry.Finite(t)||t<0||t>=BoundedContactGeometry.End)return;
                    double q=t/BoundedContactGeometry.End;
                    // Retain the already sampled motes verbatim; new body uses20 quads before them.
                    int particleVertices=b.Particles*4;
                    Array.Copy(b.X,12,b.X,80,particleVertices);Array.Copy(b.Y,12,b.Y,80,particleVertices);
                    Array.Copy(b.U,12,b.U,80,particleVertices);Array.Copy(b.V,12,b.V,80,particleVertices);
                    Array.Copy(b.Alpha,12,b.Alpha,80,particleVertices);Array.Copy(b.Soft,12,b.Soft,80,particleVertices);
                    Array.Clear(b.Alpha,0,80);b.Quads=20+b.Particles;
                    void Vertex(int n,double along,double across,double u,double v,double alpha,bool soft){
                        b.X[n]=axis.X*along-axis.Y*across;b.Y[n]=axis.Y*along+axis.X*across;
                        b.U[n]=u;b.V[n]=v;b.Alpha[n]=alpha;b.Soft[n]=soft?1:0;
                    }
                    void BandVertex(int n,double u,double side){
                        double belly=Math.Pow(Math.Max(0,Math.Sin(Math.PI*u)),.8);
                        double x=-.58+.62*u+.035*q*(1-u);
                        double y=.10*(1-u)+.030*Math.Sin(Math.PI*u)-.025*u;
                        double width=(.012+.190*belly*(1-.45*u))*(1-.30*q);
                        if(side<0)width*=1-.35*belly; // lower edge opens transparently; endpoint widths unchanged
                        double endFade=SmoothMirror(u/.13)*(1-.78*SmoothMirror((u-.86)/.14));
                        double consume=1-.8*SmoothMirror((q-.25)/.65)*(1-u);
                        double cross=side<0?.08:side>0?.38:1;
                        double alpha=.55*Math.Pow(1-q,1.1)*endFade*consume*cross;
                        Vertex(n,x, y+side*width,u,(side+1)/2,alpha,false);
                    }
                    // One continuous UV field, unequal width and edge density; no separately masked row ellipses.
                    for(int i=0;i<8;i++)for(int j=0;j<2;j++){
                        int n=(i*2+j)*4;double u=i/8.0,v=(i+1)/8.0,lo=j-1,hi=j;
                        BandVertex(n,u,lo);BandVertex(n+1,v,lo);BandVertex(n+2,v,hi);BandVertex(n+3,u,hi);
                    }
                    //296: a single broken folded frost front built from two joined unequal faces,
                    // then a small attached cool fold and one peeling chip. Not a row of tiny triangles.
                    double frostAlpha=.62*Math.Pow(1-q,1.3);
                    void FrostVertex(int n,double x,double y,double u,double v,double a){
                        double anchor=-.32;
                        x=anchor+(x-anchor)*(1-.32*q)+.025*q;
                        y=.075+(y-.075)*(1-.28*q);
                        Vertex(n,x,y,u,v,frostAlpha*a,false);
                    }
                    FrostVertex(64,-.51,.10,0,.20,.75);
                    FrostVertex(65,-.39,.075,.48,0,.68);
                    FrostVertex(66,-.35,.16,.48,.85,1);
                    FrostVertex(67,-.48,.19,0,1,.84);
                    FrostVertex(68,-.39,.075,.48,0,.68);
                    FrostVertex(69,-.25,.09,1,.20,.36);
                    FrostVertex(70,-.29,.16,1,.82,.66);
                    FrostVertex(71,-.35,.16,.48,.85,1);
                    // Cool underside stays attached initially and retreats independently, not a new impact.
                    FrostVertex(72,-.38,.073,0,0,.40);
                    FrostVertex(73,-.27,.085,1,0,.22);
                    FrostVertex(74,-.29,.115,1,1,.42);
                    FrostVertex(75,-.36,.12,0,1,.52);
                    double peel=SmoothMirror((q-.12)/.18);
                    FrostVertex(76,-.29+.015*q,.14+.015*q,0,0,.32*peel);
                    FrostVertex(77,-.23+.025*q,.13+.02*q,1,0,.18*peel);
                    FrostVertex(78,-.25+.02*q,.16+.025*q,.55,1,.38*peel);
                    FrostVertex(79,-.25+.02*q,.16+.025*q,.55,1,.38*peel);
                    // Original particle coordinates/UV/alpha and the entire analytic shader are retained.
                }};
        }

        private static NativeEffect CreateImpSparkContact()
        {
            var old=FireContinuousGeometry.Create("projectile");
            var hot=FireContinuousGeometry.Create("projectile").Flight;
            var cool=FireContinuousGeometry.Create("projectile").Flight;
            var oldBatches=Batches(old.Batches,Wrap);var all=new NativeBatch[oldBatches.Length+2];
            Array.Copy(oldBatches,all,oldBatches.Length);
            all[oldBatches.Length]=Wrap(hot);all[oldBatches.Length+1]=Wrap(cool);
            all[oldBatches.Length].Texture="hd2d-fire-volume-powder-density-v1";
            all[oldBatches.Length+1].Texture=all[oldBatches.Length].Texture;
            bool committed=false;Vector2 axis=Vector2.Right;
            return new NativeEffect{Batches=all,Shader="FireContinuousFx",Palette="fire-continuous-palette-r1",
                Texture="fire-continuous-surface-r1",RouteFamily="fire",
                ConfigureSource=v=>{FireContinuousGeometry.ConfigureSource(old,v.X,v.Y,v.Z);
                    axis=new Vector2((float)-old.Source.X,(float)-old.Source.Y).Normalized();},
                ConfigureImpSparkCommittedContact=v=>committed=v,
                Light=()=>(float)old.LightEnvelope,
                Sample=age=>{
                    // Same original sample preserves flight, core, particles and both light envelopes.
                    FireContinuousGeometry.Sample(old,age,FireContinuousGeometry.Lead(old));
                    Array.Clear(hot.Alpha,0,hot.Alpha.Length);Array.Clear(cool.Alpha,0,cool.Alpha.Length);
                    double t=old.VisualAge;
                    if(t<0||!committed)return; // never promote an uncommitted or negative phase
                    for(int k=0;k<old.TongueCount;k++)Array.Clear(old.Batches[old.TongueOffset+k].Alpha,0,99);
                    double open=.82+.18*SmoothMirror(t/.04),consume=SmoothMirror((t-.045)/.16);
                    double heat=1-SmoothMirror((t-.035)/.145);
                    double cooling=SmoothMirror((t-.065)/.04)*(1-SmoothMirror((t-.12)/.10));
                    for(int layer=0;layer<2;layer++){
                        var m=layer==0?hot:cool;double width=(layer==0?.82:.74)*open,height=layer==0?.62:.52;
                        double opacity=layer==0?heat*.88:cooling*.34;
                        for(int i=0;i<33;i++)for(int j=0;j<5;j++){
                            int n=i*5+j;double u=i/32.0,v=j/4.0;
                            double along=(u-.38)*width,across=(.58-v)*height;
                            double edge=Math.Sqrt((u-.38)*(u-.38)+(v-.58)*(v-.58));
                            m.Vertices[n]=new FireContinuousGeometry.Point(axis.X*along-axis.Y*across,
                                axis.Y*along+axis.X*across,.025+(layer==1?.004:0));
                            m.UV[n]=new FireContinuousGeometry.Uv(u,1-v);
                            m.Alpha[n]=opacity*(1-consume*SmoothMirror((edge-.12)/.55));
                            m.Gain[n]=layer==0?1.0:.55;m.Erosion[n]=0;
                        }
                    }
                }};
        }

        private static NativeEffect CreateMirrorReturn()
        {
            var old=WaterContinuousGeometry.Create();var local=WaterContinuousGeometry.Create();bool mirror=false,committed=false;
            var batches=Batches(old.Batches,Wrap);
            var main=Wrap(local.Primary);var tail=Wrap(local.Secondary);
            main.Texture="maelstrom-water-density-v1";tail.Texture=main.Texture;
            var all=new NativeBatch[batches.Length+2];Array.Copy(batches,all,batches.Length);all[batches.Length]=main;all[batches.Length+1]=tail;
            return new NativeEffect{Batches=all,Shader="WaterContinuousFx",Palette="water-continuous-palette-r1",
                Texture="water-flow-surface-r1",Lookup="water-flow-droplet-lookup-r1",
                ConfigureSource=v=>mirror=IncomingFromRight(v),Light=()=>(float)old.LightEnvelope,
                ConfigureMirrorCommittedContact=v=>committed=v,
                Sample=age=>{
                    WaterContinuousGeometry.Sample(old,age,mirror,WaterContinuousGeometry.Lead());
                    double t=old.VisualAge;
                    foreach(var m in new[]{local.Primary,local.Secondary})Array.Clear(m.Alpha,0,m.Alpha.Length);
                    if(!committed)return; // no new contact sheet without this exact actual receipt
                    t=Math.Max(0,t); // body phase only; old VisualAge/core/particles/light stay untouched
                    Array.Clear(old.Primary.Alpha,0,old.Primary.Alpha.Length);Array.Clear(old.Secondary.Alpha,0,old.Secondary.Alpha.Length);
                    double fade=1-SmoothMirror((t-.08)/.07);if(fade<=0)return;
                    double release=SmoothMirror(t/.15);
                    for(int k=0;k<2;k++){
                        var m=k==0?local.Primary:local.Secondary;
                        for(int i=0;i<49;i++){
                            double u=i/48.0,belly=Math.Pow(Math.Max(0,Math.Sin(Math.PI*u)),.8);
                            double x=k==0?-.62+.88*u+.06*release*u:-.37+.45*u;
                            double y=k==0?.025+.14*Math.Sin(Math.PI*u)-.10*u:.025-.065*Math.Sin(Math.PI*u);
                            double width=(k==0?.15:.035)*belly*(1-.6*u)*(1-.45*release);
                            for(int j=0;j<3;j++){
                                int n=i*3+j;double side=j-1;
                                double px=x+side*width*.22,py=y+side*width;
                                m.Vertices[n]=new WaterContinuousGeometry.Point(mirror?-px:px,py,.018+.015*belly*(1-side*side)+(k==1?.006:0));
                                // Water shader flips V; use the already inspected raw interior, not its island silhouette.
                                m.UV[n]=new WaterContinuousGeometry.Uv(.28+.48*u,1-(.42+.16*side));
                                m.Alpha[n]=fade*(k==0?.9:.52)*belly*(j==1?1:.08)*(1-.25*u);
                            }
                        }
                    }
                }};
        }
        private static double SmoothMirror(double t){t=Math.Clamp(t,0,1);return t*t*(3-2*t);}
        // SourceAttackOrientation.IncomingFromRight (Godot) の写し
        internal static bool IncomingFromRight(Vector3 sourceLocal)=>sourceLocal.IsFinite() && sourceLocal.X>1E-05f;

        // ---------- BlueContactVfx379 (RecoveredSource/DeckRogue.GodotPort.Presentation/BlueContactVfx379.cs の写し) ----------
        //379: two owned receiving materials only. Original catalog, beat clock and all other contact families are unchanged.
        private sealed class Blue379MeshBuffer
        {
            internal readonly Vector3[] Position = new Vector3[256];
            internal readonly Vector2[] Uv = new Vector2[256];
            internal readonly Vector2[] Role = new Vector2[256];
            internal readonly float[] Alpha = new float[256];
            internal readonly int[] Indices = new int[384];
            private int count;
            internal Blue379MeshBuffer()
            {
                for (int i = 0; i < 64; i++)
                {
                    int v = i * 4, t = i * 6;
                    Indices[t] = v; Indices[t + 1] = v + 2; Indices[t + 2] = v + 1;
                    Indices[t + 3] = v; Indices[t + 4] = v + 3; Indices[t + 5] = v + 2;
                }
            }
            internal void Reset() { count = 0; Array.Clear(Alpha, 0, Alpha.Length); }
            internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uv0, Vector2 uv1, float alpha, float role)
            {
                int i = count++ * 4;
                Position[i] = a; Position[i + 1] = b; Position[i + 2] = c; Position[i + 3] = d;
                Uv[i] = uv0; Uv[i + 1] = new Vector2(uv1.X, uv0.Y); Uv[i + 2] = uv1; Uv[i + 3] = new Vector2(uv0.X, uv1.Y);
                for (int j = 0; j < 4; j++) { Alpha[i + j] = alpha; Role[i + j] = new Vector2(role, 0); }
            }
            internal void IceCell(Vector2[] polygon, Vector2 displacement, float alpha, float tone)
            {
                // Cell triangulation shares one continuous body-space raster mapping.
                // Internal triangle boundaries have identical UV/tone and are not drawn as spokes.
                for (int v = 1; v < polygon.Length - 1; v++)
                {
                    int i = count++ * 4;
                    var points = new[] { polygon[0], polygon[v], polygon[v + 1], polygon[0] };
                    for (int j = 0; j < 4; j++)
                    {
                        Vector2 point = points[j];
                        Position[i + j] = new Vector3(point.X + displacement.X, point.Y + displacement.Y, .045f);
                        Uv[i + j] = new Vector2(.5f + point.X / .60f, .5f + point.Y / .44f);
                        Alpha[i + j] = alpha;
                        Role[i + j] = new Vector2(tone, 0);
                    }
                }
            }
            internal NativeBatch Batch(string texture) => new NativeBatch
            {
                Kind = 91, Texture = texture, Count = Position.Length, Indices = Indices,
                Position = i => Position[i], Uv = i => Uv[i], Alpha = i => Alpha[i],
                Gain = _ => 1f, Effect = i => Role[i]
            };
        }
        private static float Ease379(float t) { t = Math.Clamp(t, 0f, 1f); return t * t * (3f - 2f * t); }
        internal static NativeEffect CreateBlueContact379(AttackRoute route)
        {
            bool ripple = route.DefinitionId == "blue_ripple_blade";
            var mesh = new Blue379MeshBuffer();
            bool committed = false;
            float age = -1f;
            var effect = new NativeEffect
            {
                Shader = "BoundedContactFx", Palette = "bounded-contact-palette-r1", Family = ripple ? 1 : 2,
                Batches = new[] { mesh.Batch(ripple ? "water-flow-surface-r1" : "hd2d-ice-wall-density-v1") },
                TargetPeak = 0f, FloorPeak = 0f, Light = () => 0f, FloorLight = () => 0f,
                VisualAge = () => age, ConfigureBlueCommittedContact = value => committed = value
            };
            effect.Sample = time =>
            {
                age = (float)time;
                mesh.Reset();
                if (!committed || !float.IsFinite(age) || age < 0f || age >= .18f) return;
                if (ripple) SampleRipple379(mesh, age); else SampleMaul379(mesh, age);
            };
            return effect;
        }
        private static void SampleRipple379(Blue379MeshBuffer mesh, float age)
        {
            float release = Ease379((age - .035f) / .135f);
            float opacity = 1f - Ease379((age - .075f) / .10f);
            Vector3 Curve(float u)
            {
                float x = -.40f + .80f * u;
                return new Vector3(x, .22f * MathF.Sin(u * MathF.PI) + .20f * x - .06f, .035f);
            }
            // The flowing transparent raster is bent along a narrow cutting sheet, not stamped as a static sprite.
            for (int i = 0; i < 28; i++)
            {
                float u = i / 28f, v = (i + 1) / 28f;
                float tip = MathF.Sin(MathF.PI * (u + v) * .5f);
                float thickness = (.045f + .075f * tip) * (1f - release * .50f);
                float local = 1f - Ease379((release - u * .55f) / .45f);
                Vector3 a = Curve(u), b = Curve(v);
                Vector3 normal = new Vector3(-(b.Y - a.Y), b.X - a.X, 0).Normalized();
                mesh.Quad(a - normal * thickness, b - normal * thickness, b + normal * thickness, a + normal * thickness,
                    new Vector2(u, 0), new Vector2(v, 1), opacity * local, 0);
            }
            // A few separated liquid fragments follow the same cut and fall; never a second impact.
            for (int i = 0; i < 7; i++)
            {
                float u = .22f + .105f * i;
                Vector3 p = Curve(u) + new Vector3(age * (.26f + .06f * i), .02f + age * (.45f - .07f * i) - age * age * 3f, .005f);
                float size = (.008f + .003f * (i % 3)) * (1f - release * .65f);
                mesh.Quad(p + new Vector3(-size, -size, 0), p + new Vector3(size, -size, 0), p + new Vector3(size, size, 0), p + new Vector3(-size, size, 0),
                    Vector2.Zero, Vector2.One, opacity * .70f, 1);
            }
        }
        private static void SampleMaul379(Blue379MeshBuffer mesh, float age)
        {
            float fracture = Ease379((age - .018f) / .10f);
            float opacity = 1f - Ease379((age - .070f) / .105f);
            // Asymmetric connected ice mass: two offset crack junctions, clipped perimeter,
            // unequal cells. No central hub or equal angular wedges.
            Vector2 a = new Vector2(-.225f, -.035f), b = new Vector2(-.188f, .095f), c = new Vector2(-.100f, .138f);
            Vector2 d = new Vector2(.012f, .112f), e = new Vector2(.102f, .133f), f = new Vector2(.205f, .065f);
            Vector2 g = new Vector2(.236f, -.010f), h = new Vector2(.167f, -.094f), k = new Vector2(.042f, -.126f);
            Vector2 l = new Vector2(-.080f, -.112f), m = new Vector2(-.191f, -.082f);
            Vector2 p = new Vector2(-.075f, .030f), q = new Vector2(.055f, -.020f), n = new Vector2(-.046f, -.054f);
            float crack = .0015f + .0065f * fracture;
            float fall = age * age * 1.1f;
            mesh.IceCell(new[] { a, b, c, p, n, m }, new Vector2(-crack * 1.1f, -fall), opacity * .88f, 3.08f);
            mesh.IceCell(new[] { c, d, e, q, p }, new Vector2(-crack * .25f, crack * .50f - fall), opacity * .86f, 3.14f);
            mesh.IceCell(new[] { p, q, k, l, n }, new Vector2(crack * .25f, -crack * .9f - fall), opacity * .88f, 3.04f);
            mesh.IceCell(new[] { e, f, g, q }, new Vector2(crack * 1.4f, -crack * .30f - fall), opacity * .84f, 3.18f);
            mesh.IceCell(new[] { q, g, h, k }, new Vector2(crack * 1.65f, -crack * 1.4f - fall), opacity * .86f, 3.10f);
            mesh.IceCell(new[] { m, n, l }, new Vector2(-crack * 1.5f, -crack * 1.8f - fall), opacity * .80f, 3.03f);
            // Uneven, short edge chips peel down and toward the compressed right edge.
            mesh.IceCell(new[] { new Vector2(.211f,.061f), new Vector2(.238f,.046f), new Vector2(.252f,.012f), new Vector2(.226f,.025f) },
                new Vector2(.006f + fracture * .020f, -fall * 1.2f), opacity * .75f, 3.16f);
            mesh.IceCell(new[] { new Vector2(.131f,-.108f), new Vector2(.164f,-.101f), new Vector2(.157f,-.128f), new Vector2(.139f,-.135f) },
                new Vector2(fracture * .012f, -.006f - fall * 1.5f), opacity * .78f, 3.12f);
            mesh.IceCell(new[] { new Vector2(-.183f,-.092f), new Vector2(-.156f,-.102f), new Vector2(-.170f,-.121f) },
                new Vector2(-fracture * .009f, -.004f - fall * 1.7f), opacity * .72f, 3.06f);
        }

        // ---------- 旧い幾何の包み (Wrap) ----------
        private static NativeBatch Wrap(WaterContinuousGeometry.MeshData m)=>new NativeBatch{Count=m.Vertices.Length,Kind=m.Kind,Space=0,Indices=m.Triangles,
            Position=i=>new Vector3((float)m.Vertices[i].X,(float)m.Vertices[i].Y,(float)m.Vertices[i].Z),Uv=i=>new Vector2((float)m.UV[i].X,(float)m.UV[i].Y),
            Alpha=i=>(float)m.Alpha[i],Gain=i=>(float)m.Gain[i],Role=i=>-1,Effect=i=>Vector2.One,Normal=i=>Vector3.Back,Anchor=i=>Vector3.Zero};
        private static NativeBatch Wrap(FireContinuousGeometry.MeshData m)=>new NativeBatch{Count=m.Vertices.Length,Kind=m.Kind,Space=0,Indices=m.Triangles,
            Position=i=>new Vector3((float)m.Vertices[i].X,(float)m.Vertices[i].Y,(float)m.Vertices[i].Z),Uv=i=>new Vector2((float)m.UV[i].X,(float)m.UV[i].Y),
            Alpha=i=>(float)m.Alpha[i],Gain=i=>(float)m.Gain[i],Role=i=>m.Role[i],Effect=i=>new Vector2((float)m.Erosion[i],0f),Normal=i=>Vector3.Back,Anchor=i=>Vector3.Zero};
        private static NativeBatch Wrap(EnemyContactGeometry.MeshData m)=>new NativeBatch{Count=m.Vertices.Length,Kind=0,Space=0,Indices=m.Triangles,
            Position=i=>new Vector3((float)m.Vertices[i].X,(float)m.Vertices[i].Y,(float)m.Vertices[i].Z),Uv=i=>new Vector2((float)m.UV[i].X,(float)m.UV[i].Y),
            Alpha=i=>(float)m.Alpha[i],Gain=i=>(float)m.Gain[i],Role=i=>m.Role[i],Effect=i=>Vector2.One,Normal=i=>Vector3.Back,Anchor=i=>Vector3.Zero};
        private static NativeBatch Wrap(VineSlashGeometry.MeshData m)=>new NativeBatch{Count=m.Vertices.Length,Kind=0,Space=0,Indices=m.Triangles,
            Position=i=>new Vector3((float)m.Vertices[i].X,(float)m.Vertices[i].Y,(float)m.Vertices[i].Z),Uv=i=>new Vector2((float)m.UV[i].X,(float)m.UV[i].Y),
            Alpha=i=>(float)m.Alpha[i],Gain=i=>1f,Role=i=>-1,Effect=i=>Vector2.One,Normal=i=>Vector3.Back,Anchor=i=>Vector3.Zero};
        private static NativeBatch Wrap(PlayerContactGeometry.MeshData m)=>new NativeBatch{Count=m.Vertices.Length,Kind=m.Kind,Space=0,Indices=m.Triangles,
            Position=i=>new Vector3((float)m.Vertices[i].X,(float)m.Vertices[i].Y,(float)m.Vertices[i].Z),Uv=i=>new Vector2((float)m.UV[i].X,(float)m.UV[i].Y),
            Alpha=i=>(float)m.Alpha[i],Gain=i=>(float)m.Gain[i],Role=i=>m.Role[i],Particle=i=>m.Particle[i],Effect=i=>Vector2.One,Normal=i=>Vector3.Back,Anchor=i=>Vector3.Zero};
        private static NativeBatch Wrap(ScatterProfileGeometry.MeshData m)=>new NativeBatch{Count=m.Vertices.Length,Kind=m.Kind,Space=0,Indices=m.Triangles,
            Position=i=>new Vector3((float)m.Vertices[i].X,(float)m.Vertices[i].Y,(float)m.Vertices[i].Z),Uv=i=>new Vector2((float)m.UV[i].X,(float)m.UV[i].Y),
            Alpha=i=>(float)m.Alpha[i],Gain=i=>(float)m.Gain[i],Role=i=>-1,Effect=i=>new Vector2((float)m.Phase[i],0f),Normal=i=>Vector3.Back,Anchor=i=>Vector3.Zero};
        private static NativeBatch Wrap(LocalMaterialProfileGeometry.MeshData m)=>new NativeBatch{Count=m.Vertices.Length,Kind=m.Kind,Space=0,Indices=m.Triangles,
            Position=i=>new Vector3((float)m.Vertices[i].X,(float)m.Vertices[i].Y,(float)m.Vertices[i].Z),Uv=i=>new Vector2((float)m.UV[i].X,(float)m.UV[i].Y),
            Alpha=i=>(float)m.Alpha[i],Gain=i=>(float)m.Gain[i],Role=i=>m.Role[i],Effect=i=>new Vector2((float)m.Phase[i],m.Role[i]),Normal=i=>Vector3.Back,Anchor=i=>Vector3.Zero};
        private static NativeBatch Wrap(HealContinuousGeometry.MeshData m)=>new NativeBatch{Count=m.Vertices.Length,Kind=m.Kind,Space=0,Indices=m.Triangles,
            Position=i=>new Vector3((float)m.Vertices[i].X,(float)m.Vertices[i].Y,(float)m.Vertices[i].Z),Uv=i=>new Vector2((float)m.UV[i].X,(float)m.UV[i].Y),
            Alpha=i=>(float)m.Alpha[i],Gain=i=>(float)m.Gain[i],Role=i=>m.Role[i],Effect=i=>new Vector2((float)m.Coverage[i],0f),Normal=i=>Vector3.Back,Anchor=i=>Vector3.Zero};
        private static NativeBatch[] Batches<T>(T[] data,Func<T,NativeBatch> wrap){var result=new NativeBatch[data.Length];for(int i=0;i<data.Length;i++)result[i]=wrap(data[i]);return result;}

        // ---------- Create (Godot の NativeGeometryBridge.Create の分岐。選ばれた経路と、その土台になる旧い幾何) ----------
        public static NativeEffect? Create(AttackRoute route, Hd2dEffectTier tier)
        {
            if (BlueContactVfx379Routes.SelectedRoute(route)) return CreateBlueContact379(route);
            if(NamelessKegContactVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.CreateKegContact();
            if(NamelessPowderContactVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.CreatePowderContact();
            if(GearContactVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.CreatePowderContact();
            if(GearKegContactVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.CreateKegContact();
            if(EnemyContactVfxProfiles.ProbePokeRoute(route))return Hd2dAttackPrototype.CreateProbePokeContact();
            if(EnemyContactVfxProfiles.SelectedRoute(route))return route.Family=="minimal_neutral_contact"
                ?Hd2dAttackPrototype.CreateEnemyNeutralContact():route.Family=="enemy_blunt"
                ?Hd2dAttackPrototype.CreateEnemyBlunt():Hd2dAttackPrototype.CreateEnemyPoke();
            if(PermanentContactVfxProfiles.CanopyRoute(route))return Hd2dAttackPrototype.CreateCanopyContact();
            if(PermanentContactVfxProfiles.ThornFlameRoute(route))return Hd2dAttackPrototype.CreateThornFlameContact();
            if(PermanentContactVfxProfiles.SelectedRoute(route))return route.Family=="fire_localburst"
                ?Hd2dAttackPrototype.CreateInstalledFireContact():route.Family=="dark_drain"
                ?Hd2dAttackPrototype.CreateInstalledDarkContact(route):route.Family=="plant_point_contact"
                ?Hd2dAttackPrototype.CreateInstalledPlantContact():route.Family=="lightning_discharge"
                ?Hd2dAttackPrototype.CreateInstalledLightningContact():Hd2dAttackPrototype.CreatePermanentPulse();
            if(SparkBurstContactVfxProfiles.SelectedRoute(route))return CreateSparkBurstContact(route,tier);
            if(GreenGulpContactVfxProfiles.SelectedRoute(route))return CreateGreenGulpContact();
            if(SporeContactVfx385.SelectedRoute(route))return CreateSpore385Contact();
            if(CultistDarkBladeContactVfxProfiles.SelectedRoute(route))return CreateCultistDarkBladeContact();
            if(MomentumWhipContactVfxProfiles.SelectedRoute(route))return CreateMomentumWhipContact();
            if(AetherLanceContactVfxProfiles.SelectedRoute(route))return CreateAetherLanceContact();
            if(VineFormationCounterVfxProfiles.SelectedRoute(route))return CreateVineFormationCounter();
            if(DoubleLashReceiveVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.Create(route with{Renderer="VineSlashFx"},true)
                ?? Create(route with{Renderer="VineSlashFx"},tier);
            if(HolyMaulReceiveVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.Create(route with{Renderer="EnemyContactFx"}, ownedHolyMaulPressure:true)
                ?? Create(route with{Renderer="EnemyContactFx"},tier);
            if(AxialFamilyContactVfxProfiles.Selected(route))return Hd2dAttackPrototype.Create(route with{Renderer="PlayerContactFx"},ownedAxialContact:true)
                ?? Create(route with{Renderer="PlayerContactFx"},tier);
            if(TripleHornReceiveVfxProfiles.SelectedRoute(route))return Create(route with{Renderer="PlayerContactFx"},tier);
            if(LightBurstContactVfxProfiles.SelectedRoute(route))return CreateLightBurstContact(route,tier);
            if(LightBoltContactVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.CreateOwnedLightBoltContact(route);
            if(CrossbowAimedContactVfxProfiles.SelectedRoute(route))return CreateCrossbowAimedContact();
            if(VineGraspContactVfxProfiles.SelectedRoute(route))return CreateVineGraspContact();
            if(WraithCurseContactVfxProfiles.SelectedRoute(route))return CreateWraithCurseContact();
            if(ColdSweepContactVfxProfiles.SelectedRoute(route))return CreateColdSweepContact();
            if(ImpSparkContactVfxProfiles.SelectedRoute(route))return CreateImpSparkContact();
            if(MirrorContactVfxProfiles.SelectedRoute(route))return CreateMirrorReturn();
            if(StatusContactVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.CreateBurnTickContact();
            if(NecroContactVfxProfiles.GroaningSkullRoute(route))return Hd2dAttackPrototype.CreateGroaningSkullContact();
            if(NecroContactVfxProfiles.CorpseKissRoute(route))return Hd2dAttackPrototype.CreateCorpseKissContact();
            if(NecroContactVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.CreateNecroDark(route);
            if(ReactionContactVfxProfiles.UnderworldRoute(route))return Hd2dAttackPrototype.CreateUnderworldContact();
            if(ReactionContactVfxProfiles.GrudgeRoute(route))return Hd2dAttackPrototype.CreateGrudgeContact();
            if(ReactionContactVfxProfiles.LastStandRoute(route))return Hd2dAttackPrototype.CreateLastStandContact();
            if(ReactionContactVfxProfiles.CurseRoute(route))return Hd2dAttackPrototype.CreateCurseReturnContact();
            if(ReactionContactVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.CreateReactionDark(route);
            if(LightningContactVfxProfiles.SelectedRoute(route))return Hd2dAttackPrototype.CreateLightningDischarge();
            if(route.Renderer=="Hd2dSupportFx" && route.Family=="ordinary_guard_gain")return Hd2dAttackPrototype.CreateOrdinaryGuard();
            if(SupportVfxProfiles.IceWallRoute(route))return Hd2dAttackPrototype.CreateIceWall();
            if(route.Renderer=="Hd2dSupportFx" && route.Family=="ice_guard_gain")return Hd2dAttackPrototype.CreateIceGuard();
            if(SupportVfxProfiles.MassHealRoute(route))return Hd2dAttackPrototype.CreateMassHeal();
            if(route.Renderer=="Hd2dSupportFx" && route.Family=="prayer_heal")return Hd2dAttackPrototype.CreatePrayerHeal();
            if (Hd2dAttackPrototype.SupportsRoute(route)) return Hd2dAttackPrototype.Create(route);
            string family = route.Family;
            double sampledAge = 0.0;
            bool mirror = false;
            NativeEffect result;
            switch (route.Renderer)
            {
            case "WaterContinuousFx":
            {
                var b7 = WaterContinuousGeometry.Create();
                result = new NativeEffect
                {
                    Batches = Batches(b7.Batches, Wrap),
                    Sample = (double age) => { WaterContinuousGeometry.Sample(b7, age, mirror, WaterContinuousGeometry.Lead()); },
                    ConfigureSource = (Vector3 v) => { mirror = IncomingFromRight(v); },
                    Light = () => (float)b7.LightEnvelope,
                    Shader = "WaterContinuousFx", Palette = "water-continuous-palette-r1", Texture = "water-flow-surface-r1", Lookup = "water-flow-droplet-lookup-r1"
                };
                break;
            }
            case "FireContinuousFx":
            {
                var b17 = FireContinuousGeometry.Create((family == "fire") ? "projectile" : "localburst");
                result = new NativeEffect
                {
                    Batches = Batches(b17.Batches, Wrap),
                    Sample = (double age) => { FireContinuousGeometry.Sample(b17, age, FireContinuousGeometry.Lead(b17)); },
                    ConfigureSource = (Vector3 v) => { FireContinuousGeometry.ConfigureSource(b17, v.X, v.Y, v.Z); },
                    Light = () => (float)b17.LightEnvelope,
                    Shader = "FireContinuousFx", Palette = "fire-continuous-palette-r1", Texture = "fire-continuous-surface-r1"
                };
                break;
            }
            case "EnemyContactFx":
            {
                string f10 = family.Replace("enemy_", "");
                var b25 = EnemyContactGeometry.Create(f10);
                var origin = new EnemyContactGeometry.Point(0.0, 0.0, 0.0);
                result = new NativeEffect
                {
                    Batches = Batches(b25.Batches, Wrap),
                    Sample = (double age) => { EnemyContactGeometry.Sample(b25, age); if (f10 == "beam") EnemyContactGeometry.FitBeam(b25, origin); },
                    ConfigureSource = (Vector3 v) => { origin = new EnemyContactGeometry.Point(v.X, v.Y, v.Z); },
                    Light = () => (float)b25.LightEnvelope,
                    Shader = "PhysicalContactFx",
                    Palette = ((f10 == "beam") ? "neutral-beam-palette-r1" : "physical-contact-palette-r1"),
                    Texture = ((f10 == "fang") ? "enemy-fang-row-r1" : ((f10 == "claw") ? "vine-slash-surface-r1" : "enemy-blunt-contact-r1"))
                };
                break;
            }
            case "VineSlashFx":
            {
                var b21 = VineSlashGeometry.Create();
                result = new NativeEffect
                {
                    Batches = new NativeBatch[4] { Wrap(b21.Primary), Wrap(b21.Secondary), Wrap(b21.Core), Wrap(b21.Particles) },
                    Sample = (double age) => { VineSlashGeometry.Sample(b21, age, mirror, 0.066666667); },
                    ConfigureMirror = (bool value) => { mirror = value; },
                    Light = () => (float)b21.LightEnvelope,
                    Shader = "VineSlashFx", Palette = "vine-slash-palette-r1", Texture = "vine-slash-surface-r1"
                };
                break;
            }
            case "PlayerContactFx":
            {
                string f8 = ((family == "physical_meteor") ? "meteor" : "pierce");
                var b19 = PlayerContactGeometry.Create(f8);
                result = new NativeEffect
                {
                    Batches = Batches(b19.Batches, Wrap),
                    Sample = (double age) => { PlayerContactGeometry.Sample(b19, age); },
                    Light = () => (float)PlayerContactGeometry.Light(f8, sampledAge),
                    Shader = "PlayerContactFx", Palette = "physical-contact-palette-r1", Texture = "enemy-blunt-contact-r1"
                };
                break;
            }
            case "ScatterProfileFx":
            {
                var f6 = Scatter(family);
                var b11 = ScatterProfileGeometry.Create(f6);
                result = new NativeEffect
                {
                    Batches = Batches(b11.Batches, Wrap),
                    Sample = (double age) => { ScatterProfileGeometry.SampleReceipt(b11, age); },
                    Light = () => (float)b11.LightEnvelope,
                    Shader = "ScatterProfileFx", Palette = "scatter-profile-palette-r2", Family = (int)f6
                };
                break;
            }
            case "LocalMaterialProfileFx":
            {
                var f9 = Local(family);
                var b23 = LocalMaterialProfileGeometry.Create(f9);
                var nativeEffect4 = new NativeEffect();
                nativeEffect4.Batches = Batches(b23.Batches, Wrap);
                nativeEffect4.Sample = (double age) =>
                {
                    if (ObjectContact415Routes.IsExactMaelstromRoute(route)) LocalMaterialProfileGeometry.SampleMaelstromReceipt(b23, age);
                    else LocalMaterialProfileGeometry.SampleReceipt(b23, age);
                };
                nativeEffect4.Light = () => (float)b23.LightEnvelope;
                nativeEffect4.Shader = "LocalMaterialProfileFx";
                nativeEffect4.Palette = "local-material-palette-r1";
                nativeEffect4.Family = (int)f9;
                nativeEffect4.Texture = f9 switch
                {
                    LocalMaterialProfileGeometry.Profile.Water => ObjectContact415Routes.IsExactMaelstromRoute(route) ? "maelstrom-water-density-v1" : "water-flow-surface-r1",
                    LocalMaterialProfileGeometry.Profile.Light => "light-ray-surface-r2",
                    LocalMaterialProfileGeometry.Profile.Dark => "dark-drain-surface-r1",
                    LocalMaterialProfileGeometry.Profile.Botanical => "",
                    _ => "enemy-blunt-contact-r1",
                };
                nativeEffect4.Lookup = "water-flow-droplet-lookup-r1";
                result = nativeEffect4;
                break;
            }
            case "HealContinuousFx":
            {
                var b18 = HealContinuousGeometry.Create();
                bool ownedMaulHeal=HolyMaulHealReceiveVfxProfiles.SelectedRoute(route);
                double wrapScale=route.FixedSmall?1.0/.72:1.0;
                result = new NativeEffect
                {
                    Batches = Batches(b18.Batches, Wrap),
                    Sample = (double age) => { HealContinuousGeometry.Sample(b18, age, ownedMaulHeal, wrapScale); },
                    Light = () => (float)b18.LightEnvelope,
                    Shader = "HealContinuousFx",
                    Palette = ownedMaulHeal?"holy-maul-recovery-wrap-r1":"heal-uplift-palette-r1",
                    TargetPeak = ownedMaulHeal?.10f:.35f,
                    FloorPeak = ownedMaulHeal?.045f:.12f,
                    Texture = "heal-uplift-surface-r1"
                };
                break;
            }
            case "BoundedContactFx":
            {
                bool object415 = ObjectContact415Routes.ObjectContact415Route(route);
                int motes = 3;
                var b22 = new BoundedContactGeometry.Buffer();
                var p = BoundedContactGeometry.Find(family);
                if (p == null) return null;
                var batch = new NativeBatch
                {
                    Count = b22.X.Length,
                    Indices = b22.Triangles,
                    Position = (int i) => {
                        //415-v2: lower/right receiver margin, smaller chipped chunk;
                        //two adjacent private rectangles carry one continuous curved blade.
                        //No extra batch, alpha, lifetime, hit or mote changes.
                        bool curve=p.Form==BoundedContactGeometry.Shape.CurvedObject;
                        int mainVertices=curve?8:4;
                        if(object415 && i<mainVertices){
                            if(curve){int corner=i%4;float u=(i/4)*.5f+((corner==1||corner==2)?.5f:0f);
                                float v=corner>=2?1f:0f;
                                return new Vector3(.14f+u*.52f,-.13f+v*.24f,0f);}
                            return new Vector3((float)b22.X[i]*2.00f+.36f,(float)b22.Y[i]*1.65f+.025f,0f);
                        }
                        return new Vector3((float)b22.X[i],(float)b22.Y[i],0f);
                    },
                    Uv = (int i) => object415 && p.Form==BoundedContactGeometry.Shape.CurvedObject && i<8
                        ?new Vector2((i/4)*.5f+(float)b22.U[i]*.5f,(float)b22.V[i])
                        :new Vector2((float)b22.U[i],(float)b22.V[i]),
                    Alpha = (int i) => (float)b22.Alpha[i],
                    Gain = (int i) => 1f,
                    Effect = (int i) => new Vector2((float)b22.Soft[i],object415 && i<(p.Form==BoundedContactGeometry.Shape.CurvedObject?8:4)?(p.Form==BoundedContactGeometry.Shape.CurvedObject?2f:1f):0f)
                };
                var nativeEffect3 = new NativeEffect();
                nativeEffect3.Batches = new NativeBatch[1] { batch };
                nativeEffect3.Sample = (double age) => { BoundedContactGeometry.Sample(b22, p, age, motes); };
                nativeEffect3.ConfigureParticleGain = (float gain) => { motes = BoundedContactGeometry.MoteCount(gain); };
                nativeEffect3.Shader = "BoundedContactFx";
                nativeEffect3.Palette = "bounded-contact-palette-r1";
                nativeEffect3.Family = (int)p.Material;
                result = nativeEffect3;
                break;
            }
            default:
                return null;
            }
            switch (result.Shader)
            {
            case "FireContinuousFx":
                if (family != "fire") { result.TargetPeak *= 1.1272727f; result.FloorPeak *= 1.1f; }
                break;
            case "ScatterProfileFx":
                result.TargetPeak *= 32f / 55f; result.FloorPeak *= 0.4f; break;
            case "LocalMaterialProfileFx":
                result.TargetPeak *= 558f / 1023f; result.FloorPeak *= 0.5f; break;
            case "HealContinuousFx":
                result.TargetPeak *= 28f / 55f; result.FloorPeak *= 0.5f; break;
            case "BoundedContactFx":
                result.TargetPeak = (result.FloorPeak = 0f); break;
            }
            result.RouteFamily = family;
            Action<double> sample = result.Sample;
            result.Sample = (double age) => { sampledAge = age; sample(age); };
            return ObjectContact415Routes.WarmSpark397Route(route) ? WarmSpark397Material(result) : result;
        }

        // AuthoredEffectPlan.Scatter / Local (Godot) の写し
        internal static ScatterProfileGeometry.Profile Scatter(string f) => f switch
        {
            "scatter_ice" => ScatterProfileGeometry.Profile.Ice,
            "scatter_spore" => ScatterProfileGeometry.Profile.Spore,
            "scatter_warm_spark" => ScatterProfileGeometry.Profile.WarmSpark,
            _ => ScatterProfileGeometry.Profile.Fire,
        };
        internal static LocalMaterialProfileGeometry.Profile Local(string f) => f switch
        {
            "local_dark" => LocalMaterialProfileGeometry.Profile.Dark,
            "local_light" => LocalMaterialProfileGeometry.Profile.Light,
            "local_botanical" => LocalMaterialProfileGeometry.Profile.Botanical,
            "local_water_flow" => LocalMaterialProfileGeometry.Profile.Water,
            _ => LocalMaterialProfileGeometry.Profile.Physical,
        };

        /// <summary>Godot の AuthoredEffectPlan.Times: 描き手ごとの (予備動作の長さ, 見える終わり)。登録のある札は Hd2dAttackPrototype の Lead/Tail</summary>
        public static (float lead, float end) Times(AttackRoute route, Hd2dEffectTier tier)
        {
            string f = route.Family;
            string t = tier.ToString().ToLowerInvariant();
            (float lead, float end) original = route.Renderer switch
            {
                "PhysicalSlashFx" => ((float)PhysicalSlashGeometry.Lead(t), (float)PhysicalSlashGeometry.End(t)),
                "VineSlashFx" => (1f / 15f, 0.182f),
                "FireContinuousFx" or "ImpSparkContactFx" => ((float)FireContinuousGeometry.Lead((f == "fire") ? "projectile" : "localburst"), (float)FireContinuousGeometry.End((f == "fire") ? "projectile" : "localburst")),
                "WaterContinuousFx" or "MirrorWaterContactFx" => ((float)WaterContinuousGeometry.Lead(), (float)WaterContinuousGeometry.End()),
                "IceCrystalFx" => ((float)IceCrystalGeometry.Lead(), (float)IceCrystalGeometry.End()),
                "LightRayFx" or "LightBoltContactFx" => ((float)LightRayGeometry.Lead(), (float)LightRayGeometry.End()),
                "DarkDrainFx" => ((float)DarkDrainGeometry.Lead(), (float)DarkDrainGeometry.End()),
                "LightningDischargeFx" => ((float)LightningDischargeGeometry.Lead(), (float)LightningDischargeGeometry.End()),
                "EnemyContactFx" => ((f == "enemy_claw") ? 0.056f : ((f == "enemy_beam") ? 0.1f : 0f), (float)EnemyContactGeometry.End(f.Replace("enemy_", ""))),
                "PlayerContactFx" => (0.13f, (float)PlayerContactGeometry.End((f == "physical_meteor") ? "meteor" : "pierce")),
                "BoundedContactFx" or "ColdSweepContactFx" or "WraithCurseContactFx" or "CrossbowAimedContactFx" => (0f, 0.18f),
                "EarthStompFx" => ((float)EarthStompGeometry.Lead((!(f == "earth_fissure")) ? 1 : 0), (float)EarthStompGeometry.End((!(f == "earth_fissure")) ? 1 : 0)),
                "WideProfileFx" => ((float)WideProfileGeometry.Lead(), (float)WideProfileGeometry.End((!(f == "wide_water")) ? WideProfileGeometry.Profile.Fire : WideProfileGeometry.Profile.Water)),
                "ScatterProfileFx" => (0f, (float)ScatterProfileGeometry.End(Scatter(f))),
                "LocalMaterialProfileFx" => (0f, (float)LocalMaterialProfileGeometry.End(Local(f))),
                "SystemOutcomeFx" => (0f, (float)SystemOutcomeGeometry.End(f)),
                "HealContinuousFx" => ((float)HealContinuousGeometry.Lead(), (float)HealContinuousGeometry.End()),
                _ => (0.1f, 0.24f),
            };
            return Hd2dAttackPrototype.SupportsRoute(route) ? (Hd2dAttackPrototype.Lead(route.Key), Hd2dAttackPrototype.Tail(route.Key)) : original;
        }
    }
}
