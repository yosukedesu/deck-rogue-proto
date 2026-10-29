using System;
using DeckRogue.Game.Hd2dMath;
namespace DeckRogue.Game.Hd2d {
internal static class Hd2dCrossbowAimedGeometry {
static double SmoothMirror(double x){x=Math.Max(0,Math.Min(1,x));return x*x*(3-2*x);}
    internal static NativeEffect Create()
    {
        var b=new BoundedContactGeometry.Buffer();var profile=BoundedContactGeometry.Find("neutral_projectile_contact");
        int motes=3;Vector2 axis=new Vector2(1,0);
        var batch=new NativeBatch{Count=b.X.Length,Indices=b.Triangles,
            Position=i=>new Vector3((float)b.X[i],(float)b.Y[i],0),Uv=i=>new Vector2((float)b.U[i],(float)b.V[i]),
            Alpha=i=>(float)b.Alpha[i],Gain=i=>1f,Effect=i=>new Vector2((float)b.Soft[i],i<64?1:i<72?2:i<80?3:0)};
        return new NativeEffect{Batches=new[]{batch},Shader="BoundedContactFx",Palette="bounded-contact-palette-r1",
            Texture="crossbow-pressure-density-v1",
            Family=(int)BoundedContactGeometry.Surface.Neutral,RouteFamily="neutral_projectile_contact",
            ConfigureParticleGain=g=>motes=BoundedContactGeometry.MoteCount(g),
            ConfigureSource=v=>{var a=new Vector2(-v.X,-v.Y);axis=a.LengthSquared()>.0001f?a.Normalized():new Vector2(1,0);},
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

}
}
