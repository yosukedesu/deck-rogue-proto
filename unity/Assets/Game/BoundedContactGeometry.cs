using System;
namespace DeckRogue.Game {
 // Receipt-local procedural carriers. All positions are fractions of captured visible height.
 // Neutral never has a directional stroke, weapon, ring, absorption path, or material inference.
 internal static class BoundedContactGeometry {
  internal enum Shape { Neutral,Point,Wind,Slash,Sweep,Blunt,Bind,Engulf,Pair,Column,Smoke,Curve,Break,Object,CurvedObject,Whip,Curse }
  internal enum Surface { Neutral,Water,Cold,Wood,Plant,Fire,Smoke,Dark }
  internal sealed class Profile { public string Id; public Shape Form; public Surface Material; public Profile(string id,Shape shape,Surface surface){Id=id;Form=shape;Material=surface;} }
  internal static readonly Profile[] Profiles={
   new Profile("minimal_neutral_contact",Shape.Neutral,Surface.Neutral),
   new Profile("neutral_point_contact",Shape.Point,Surface.Neutral),
   new Profile("neutral_wind_contact",Shape.Wind,Surface.Neutral),
   new Profile("water_slash_contact",Shape.Slash,Surface.Water),
   new Profile("cold_sweep_contact",Shape.Sweep,Surface.Cold),
   new Profile("ice_blunt_contact",Shape.Blunt,Surface.Cold),
   new Profile("plant_bind_contact",Shape.Bind,Surface.Plant),
   new Profile("neutral_engulf_contact",Shape.Engulf,Surface.Neutral),
   new Profile("wood_blunt_contact",Shape.Blunt,Surface.Wood),
   new Profile("plant_point_contact",Shape.Point,Surface.Plant),
   new Profile("plant_fang_contact",Shape.Pair,Surface.Plant),
   new Profile("ember_streak_contact",Shape.Sweep,Surface.Fire),
   new Profile("fire_blade_contact",Shape.Slash,Surface.Fire),
   new Profile("vertical_fire_contact",Shape.Column,Surface.Fire),
   new Profile("smoke_local_contact",Shape.Smoke,Surface.Smoke),
   new Profile("curved_vine_contact",Shape.Curve,Surface.Plant),
   new Profile("fire_point_contact",Shape.Point,Surface.Fire),
   new Profile("ice_break_contact",Shape.Break,Surface.Cold),
   new Profile("neutral_thrown_object_contact",Shape.Object,Surface.Neutral),
   new Profile("dark_slash_contact",Shape.Slash,Surface.Dark),
   new Profile("neutral_projectile_contact",Shape.Neutral,Surface.Neutral),
   new Profile("curved_object_contact",Shape.CurvedObject,Surface.Neutral),
   new Profile("local_curse_contact",Shape.Curse,Surface.Dark),
   new Profile("neutral_whip_contact",Shape.Whip,Surface.Neutral)
  };
  public static Profile Find(string family){foreach(var p in Profiles)if(p.Id==family)return p;return null;}
  public const double End=.180; public const int MaxQuads=40;
  public static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
  public static double Clamp(double x,double lo,double hi)=>Math.Max(lo,Math.Min(hi,x));
  public static int MoteCount(double particles)=>Math.Max(2,Math.Min(4,(int)Math.Round(3*Clamp(particles,0,1.34))));
  public sealed class Buffer {
   public readonly double[] X=new double[MaxQuads*4],Y=new double[MaxQuads*4],U=new double[MaxQuads*4],V=new double[MaxQuads*4],Alpha=new double[MaxQuads*4],Soft=new double[MaxQuads*4];
   public readonly int[] Triangles=new int[MaxQuads*6];public int Quads,Particles;
   public Buffer(){for(int i=0;i<MaxQuads;i++){int n=i*4,t=i*6;Triangles[t]=n;Triangles[t+1]=n+2;Triangles[t+2]=n+1;Triangles[t+3]=n;Triangles[t+4]=n+3;Triangles[t+5]=n+2;}}
  }
  static void Quad(Buffer b,double x0,double y0,double x1,double y1,double x2,double y2,double x3,double y3,double alpha,bool soft=false){
   if(b.Quads>=MaxQuads)throw new InvalidOperationException("bounded_contact_topology_overflow");int n=b.Quads++*4;
   b.X[n]=x0;b.Y[n]=y0;b.X[n+1]=x1;b.Y[n+1]=y1;b.X[n+2]=x2;b.Y[n+2]=y2;b.X[n+3]=x3;b.Y[n+3]=y3;
   for(int i=0;i<4;i++){b.U[n+i]=(i==1||i==2)?1:0;b.V[n+i]=i>=2?1:0;b.Alpha[n+i]=alpha;b.Soft[n+i]=soft?1:0;}
  }
  static void SoftBox(Buffer b,double x,double y,double w,double h,double alpha)=>Quad(b,x-w,y-h,x+w,y-h,x+w,y+h,x-w,y+h,alpha,true);
  static void Point(Buffer b,double x,double y,double dx,double dy,double w,double alpha){double len=Math.Sqrt(dx*dx+dy*dy),px=-dy/len*w,py=dx/len*w;Quad(b,x+px,y+py,x-px,y-py,x+dx,y+dy,x+dx,y+dy,alpha);}
  static void Segment(Buffer b,double x0,double y0,double x1,double y1,double width,double alpha){double dx=x1-x0,dy=y1-y0,len=Math.Sqrt(dx*dx+dy*dy);if(len<=1e-9)return;double px=-dy/len*width,py=dx/len*width;Quad(b,x0-px,y0-py,x1-px,y1-py,x1+px,y1+py,x0+px,y0+py,alpha);}
  static void Arc(Buffer b,double cx,double cy,double radius,double start,double span,double width,double alpha){for(int i=0;i<10;i++){double a=start+span*i/10,c=start+span*(i+1)/10;Segment(b,cx+Math.Cos(a)*radius,cy+Math.Sin(a)*radius,cx+Math.Cos(c)*radius,cy+Math.Sin(c)*radius,width*Math.Sin(Math.PI*(i+.5)/10),alpha);}}
  public static void Sample(Buffer b,Profile profile,double age,int motes){
   Array.Clear(b.Alpha,0,b.Alpha.Length);b.Quads=0;b.Particles=0;if(profile==null||!Finite(age)||age<0||age>=End)return;
   double q=age/End,fade=(1-q)*(1-q),s=.80+.20*Math.Sin(Math.PI*Math.Min(1,q*2));
   switch(profile.Form){
    case Shape.Neutral:SoftBox(b,0,0,.110*s,.100*s,.76*fade);break;
    case Shape.Point:Point(b,-.15*(1-q*.15),-.018,.235,.035,.055*s,.90*fade);break;
    case Shape.Pair:Point(b,-.12,.105*(1-q*.35),.165,-.090,.033,.9*fade);Point(b,.12,-.105*(1-q*.35),-.165,.090,.033,.9*fade);break;
    case Shape.Wind:case Shape.Sweep:for(int i=0;i<3;i++)SoftBox(b,-.025+q*.025,(i-1)*.052,.17*(1-.1*i),.021,.48*fade);break;
    case Shape.Slash:Arc(b,0,-.085,.20,-.10,2.05,.025,.88*fade);break;
    case Shape.Blunt:SoftBox(b,0,0,.15*(1-.22*q),.105*(1+.18*q),.86*fade);break;
    case Shape.Bind:case Shape.Engulf:double r=.16-.04*q;Arc(b,-.025,0,r,.70,1.75,profile.Form==Shape.Bind?.018:.035,.75*fade);Arc(b,.025,0,r,3.84,1.75,profile.Form==Shape.Bind?.018:.035,.75*fade);break;
    case Shape.Column:for(int i=0;i<3;i++)Point(b,(i-1)*.065,-.13,.018*Math.Sin(q*4+i),.30-Math.Abs(i-1)*.06,.05,.72*fade);break;
    case Shape.Smoke:for(int i=0;i<3;i++)SoftBox(b,(i-1)*.058,.025*i+q*.025,.090+q*.018,.080+q*.020,.20*fade);break;
    case Shape.Curve:Arc(b,0,0,.16,.20,2.50,.025,.80*fade);break;
    case Shape.Break:for(int i=0;i<3;i++){double a=i*2*Math.PI/3;double x=Math.Cos(a),y=Math.Sin(a);Point(b,x*(.025+.03*q),y*(.025+.03*q),x*.10,y*.10,.028,.82*fade);}break;
    case Shape.Object:Quad(b,-.085,-.060,.073,-.068,.095,.048,-.059,.082,.75*fade);break;
    case Shape.CurvedObject:Segment(b,-.13,-.05,0,.055,.032,.82*fade);Segment(b,0,.055,.13,-.05,.032,.82*fade);break;
    case Shape.Whip:for(int i=0;i<12;i++){double x=(i/12.0-.5)*.38,xx=((i+1)/12.0-.5)*.38;Segment(b,x,Math.Sin(i/12.0*Math.PI*2)*.055,xx,Math.Sin((i+1)/12.0*Math.PI*2)*.055,.009,.80*fade);}break;
    case Shape.Curse:SoftBox(b,0,0,.13*s,.115*s,.78*fade);break;
   }
   // Symmetric short motes have no caster axis and no gameplay or receipt meaning.
   motes=Math.Max(2,Math.Min(4,motes));b.Particles=motes;
   for(int i=0;i<motes;i++){double a=i*Math.PI*2/motes+.35,travel=.095+.045*q;SoftBox(b,Math.Cos(a)*travel,Math.Sin(a)*travel,.011*(1-.4*q),.011*(1-.4*q),.62*fade);}
  }
 }
}
