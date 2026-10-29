using System;
using DeckRogue.Game.Hd2dMath;
namespace DeckRogue.Game.Hd2d {
internal static class Hd2dWraithCurseGeometry {
static double SmoothMirror(double x){x=Math.Max(0,Math.Min(1,x));return x*x*(3-2*x);}
    internal static NativeEffect Create()
    {
        var b=new BoundedContactGeometry.Buffer();var profile=BoundedContactGeometry.Find("local_curse_contact");
        int motes=3;Vector2 axis=new Vector2(1,0);
        var batch=new NativeBatch{Count=b.X.Length,Indices=b.Triangles,
            Position=i=>new Vector3((float)b.X[i],(float)b.Y[i],0),Uv=i=>new Vector2((float)b.U[i],(float)b.V[i]),
            Alpha=i=>(float)b.Alpha[i],Gain=i=>1f,Effect=i=>new Vector2((float)b.Soft[i],i<64?1:0)};
        return new NativeEffect{Batches=new[]{batch},Shader="BoundedContactFx",Palette="bounded-contact-palette-r1",
            Texture="wraith-pressure-density-v1",Family=(int)BoundedContactGeometry.Surface.Dark,RouteFamily="local_curse_contact",
            ConfigureParticleGain=g=>motes=BoundedContactGeometry.MoteCount(g),
            ConfigureSource=v=>{var a=new Vector2(-v.X,-v.Y);axis=a.LengthSquared()>.0001f?a.Normalized():new Vector2(1,0);},
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

}
}
