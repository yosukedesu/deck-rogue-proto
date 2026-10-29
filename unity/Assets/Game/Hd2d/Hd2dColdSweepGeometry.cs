using System;
using DeckRogue.Game.Hd2dMath;
namespace DeckRogue.Game.Hd2d {
// Exact selected297 sampler; only factory name and Godot vector spelling adapted.
internal static class Hd2dColdSweepGeometry {
    internal static NativeEffect Create()
    {
        var b=new BoundedContactGeometry.Buffer();var profile=BoundedContactGeometry.Find("cold_sweep_contact");
        int motes=3;bool committed=false;Vector2 axis=new Vector2(1,0);
        var batch=new NativeBatch{Count=b.X.Length,Indices=b.Triangles,
            Position=i=>new Vector3((float)b.X[i],(float)b.Y[i],0),Uv=i=>new Vector2((float)b.U[i],(float)b.V[i]),
            Alpha=i=>(float)b.Alpha[i],Gain=i=>1f,Effect=i=>new Vector2((float)b.Soft[i],i<64?1:i<72?2:i<80?3:0)};
        return new NativeEffect{Batches=new[]{batch},Shader="ColdSweepSurfaceFx",Palette="bounded-contact-palette-r1",Texture="cold-frost-density-v1",
            Family=(int)BoundedContactGeometry.Surface.Cold,RouteFamily="cold_sweep_contact",TargetPeak=0,FloorPeak=0,
            ConfigureParticleGain=g=>motes=BoundedContactGeometry.MoteCount(g),
            ConfigureSource=v=>{var a=new Vector2(-v.X,-v.Y);axis=a.LengthSquared()>.0001f?a.Normalized():new Vector2(1,0);},
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

private static double SmoothMirror(double t){t=Math.Clamp(t,0,1);return t*t*(3-2*t);}
}}
