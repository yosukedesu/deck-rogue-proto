// RECONSTRUCTED 2026-10-12 from recovered evaluation ancestor; not original production bytes.
// Mathematical equations unchanged; namespace adapted for candidate integration.
// Caller must gate this provisional model on supported source profile and GL environment.
// See reconstruction README and recovered PROVENANCE.md for attribution and limitations.
// No Unity Companion-licensed source implementation is copied into this file.
// Supported grade: neutral balance/filter/mixer/curves/saturation + LogC contrast/LGG.
using System;
using System.Numerics;

namespace DeckRogue.GodotPort.Presentation;

public sealed class SourcePost
{
    public float ExposureEv = .3f;
    public float ContrastPercent = 14f;
    public Vector4 Lift = new(.955f, 1.01f, 1.04f, -.011f);
    public Vector4 Gamma = new(1, 1, 1, 0);
    public Vector4 Gain = new(1.02f, 1, .97f, 0);
    public float VignetteIntensity = .15f;
    public float VignetteSmoothness = .45f;
    public Vector2 VignetteCenter = new(.5f, .52f);
    public bool VignetteRounded = false;
    public Vector3 VignetteColor = new(0, .025f, .04f);
    // Historical Unity exact-decode convention; verify native Mathf for >1.
    public bool GammaAboveOneUsesPower22 = true;
}

public sealed class ActorDisplayTransfer
{
    public const int Size = 32, Width = 1024, Height = 32;
    public readonly SourcePost Post;
    // Byte layout for Image.Format.Rgb8. x = r + 32*b; y = g. Linear LUT.
    // Use linear filtering, no source_color decode, no mipmaps, clamp.
    public readonly byte[] LutRgb;
    public readonly Vector3 PreparedLift, PreparedGamma, PreparedGain;
    private const float A=.88f, B=.6f, C=.1f, D=.2f, E=.01f, F=.3f;
    public static readonly float FilmicWhite = FilmicRaw(1f);
    private static readonly Vector3 Ap1Y = new(.272229f,.674082f,.0536895f);
    private static readonly Vector3 RecY = new(.2126729f,.7151522f,.072175f);
    private static float Clamp01(float x) => Math.Clamp(x,0f,1f);
    private static Vector3 Clip(Vector3 x) => Vector3.Clamp(x,Vector3.Zero,Vector3.One);
    private static Vector3 Map(Vector3 x,Func<float,float> f) => new(f(x.X),f(x.Y),f(x.Z));
    private static Vector3 Rows(Vector3 x, Vector3 r, Vector3 g, Vector3 b) => new(Vector3.Dot(x,r),Vector3.Dot(x,g),Vector3.Dot(x,b));
    private static Vector3 Ap1ToXyz(Vector3 x) => Rows(x,new(.6624541811f,.1340042065f,.1561876870f),new(.2722287168f,.6740817658f,.0536895174f),new(-.0055746495f,.0040607335f,1.0103391003f));
    private static Vector3 XyzToAp1(Vector3 x) => Rows(x,new(1.6410233797f,-.3248032942f,-.2364246952f),new(-.6636628587f,1.6153315917f,.0167563477f),new(.0117218943f,-.0082844420f,.9883948585f));
    private static Vector3 Desaturate(Vector3 x,float amount) => Vector3.Lerp(new(Vector3.Dot(x,Ap1Y)),x,amount);

    public ActorDisplayTransfer(SourcePost post)
    {
        Post=post ?? throw new ArgumentNullException(nameof(post));
        PreparedLift=Prepare(post.Lift,.15f,0);
        PreparedGamma=Prepare(post.Gamma,.8f,1);
        PreparedGain=Prepare(post.Gain,.8f,2);
        LutRgb=new byte[Width*Height*3];
        // Full 32^3 lattice, not a gray transfer. Full-color ACES precedes it.
        for(int b=0;b<Size;b++) for(int g=0;g<Size;g++) for(int r=0;r<Size;r++)
        {
            Vector3 x=GradeNeutral(new(r/31f,g/31f,b/31f));
            int o=3*(g*Width+b*Size+r);
            LutRgb[o]=Quantize(x.X);LutRgb[o+1]=Quantize(x.Y);LutRgb[o+2]=Quantize(x.Z);
        }
    }
    private static byte Quantize(float x) => (byte)MathF.Floor(Clamp01(x)*255f+.5f);
    private float DecodeTrackball(float x)
    {
        if(x<=.04045f)return x/12.92f;
        if(x>1 && Post.GammaAboveOneUsesPower22)return MathF.Pow(x,2.2f);
        return MathF.Pow((x+.055f)/1.055f,2.4f);
    }
    private Vector3 Prepare(Vector4 t,float scale,int kind)
    {
        Vector3 v=Map(new(t.X,t.Y,t.Z),DecodeTrackball)*scale;
        v-=new Vector3(Vector3.Dot(v,RecY)-t.W);
        if(kind==0)return v;
        v+=Vector3.One;
        return kind==1 ? Map(v,x=>1/MathF.Max(x,.001f)) : v;
    }
    public Vector3 GradeNeutral(Vector3 color)
    {
        // Neutral LMS/filter/SMH/split/mixer/curves are algebraic identities.
        // Does not model round-off from their source shader implementations.
        color=Map(color,x=>
        {
            float log=.244161f*MathF.Log10(5.555556f*x+.047996f)+.386036f;
            log=.4135884f+(log-.4135884f)*(1+Post.ContrastPercent/100f);
            return MathF.Max(0,(MathF.Pow(10,(log-.386036f)/.244161f)-.047996f)/5.555556f);
        });
        color=color*PreparedGain+PreparedLift;
        color=new(SignedPower(color.X,PreparedGamma.X),SignedPower(color.Y,PreparedGamma.Y),SignedPower(color.Z,PreparedGamma.Z));
        return Clip(color);
    }
    private static float SignedPower(float x,float gamma) => MathF.CopySign(MathF.Pow(MathF.Abs(x),gamma),x);
    private Vector3 Read(int r,int g,int b)
    {
        int i=3*(g*Width+b*Size+r);
        return new Vector3(LutRgb[i],LutRgb[i+1],LutRgb[i+2])/255f;
    }
    public Vector3 SampleLut(Vector3 mapped)
    {
        Vector3 p=Clip(mapped)*31;
        int r=Math.Min(30,(int)p.X),g=Math.Min(30,(int)p.Y),b=Math.Min(30,(int)p.Z);
        Vector3 f=p-new Vector3(r,g,b);
        Vector3 z0=Vector3.Lerp(Vector3.Lerp(Read(r,g,b),Read(r+1,g,b),f.X),Vector3.Lerp(Read(r,g+1,b),Read(r+1,g+1,b),f.X),f.Y);
        Vector3 z1=Vector3.Lerp(Vector3.Lerp(Read(r,g,b+1),Read(r+1,g,b+1),f.X),Vector3.Lerp(Read(r,g+1,b+1),Read(r+1,g+1,b+1),f.X),f.Y);
        return Vector3.Lerp(z0,z1,f.Z);
    }
    public Vector3 SourceLinearDisplay(Vector3 prepost,Vector2 sourceScreenUv,float aspect=1)
    {
        Vector2 delta=Vector2.Abs(sourceScreenUv-Post.VignetteCenter)*(3*Post.VignetteIntensity);
        if(Post.VignetteRounded)delta.X*=aspect;
        float falloff=MathF.Pow(Clamp01(1-delta.LengthSquared()),5*Post.VignetteSmoothness);
        Vector3 vignette=Vector3.Lerp(Post.VignetteColor,Vector3.One,falloff);
        Vector3 x=prepost*vignette*MathF.Pow(2,Post.ExposureEv);
        return SampleLut(Clip(AcesRgb(x)));
    }
    public Vector3 SourceDisplay(Vector3 prepost,Vector2 sourceScreenUv,float aspect=1) => Map(SourceLinearDisplay(prepost,sourceScreenUv,aspect),EncodeSrgb);
    public Vector3 ActorAlbedo(Vector3 prepost,Vector2 sourceScreenUv,float aspect=1) => Map(SourceDisplay(prepost,sourceScreenUv,aspect),CompensateChannel);
    public static float EncodeSrgb(float x) => x<=.0031308f ? x*12.92f : 1.055f*MathF.Pow(x,1/2.4f)-.055f;

    public static Vector3 AcesRgb(Vector3 rgb)
    {
        Vector3 a=Rows(rgb,new(.4397010f,.3829780f,.1773350f),new(.0897923f,.8134230f,.0967616f),new(.0175440f,.1115440f,.8707040f));
        float lo=MathF.Min(a.X,MathF.Min(a.Y,a.Z)),hi=MathF.Max(a.X,MathF.Max(a.Y,a.Z));
        float sat=(MathF.Max(hi,.0001f)-MathF.Max(lo,.0001f))/MathF.Max(hi,.01f);
        float chroma=MathF.Sqrt(MathF.Max(0,((a.X-a.Y)*(a.X-a.Y)+(a.Y-a.Z)*(a.Y-a.Z)+(a.Z-a.X)*(a.Z-a.X))*.5f));
        float yc=(a.X+a.Y+a.Z+1.75f*chroma)/3;
        float z=(sat-.4f)/.2f,t=MathF.Max(0,1-MathF.Abs(z)*.5f);
        float shape=(1+MathF.Sign(z)*(1-t*t))*.5f;
        float glow=.05f*shape*(yc<=.08f*2/3 ? 1 : yc>=.16f ? 0 : .08f/yc-.5f);
        a*=1+glow;
        float hue=MathF.Atan2(MathF.Sqrt(3)*(a.Y-a.Z),2*a.X-a.Y-a.Z)*180/MathF.PI;
        float h=Clamp01(1-MathF.Abs(hue)/67.5f),s=h*h*(3-2*h);
        a.X+=(.03f-a.X)*.18f*s*s*sat;
        Vector3 p=Rows(a,new(1.4514393161f,-.2365107469f,-.2149285693f),new(-.0765537734f,1.1762296998f,-.0996759264f),new(.0083161484f,-.0060324498f,.9977163014f));
        p=Desaturate(Vector3.Max(Vector3.Zero,p),.96f);
        p=Map(p,v=>(v*v+.0245786f*v-.000090537f)/(.983729f*v*v+.432951f*v+.238081f));
        Vector3 xyz=Ap1ToXyz(p);
        float total=MathF.Max(xyz.X+xyz.Y+xyz.Z,.0001f),cx=xyz.X/total,cy=xyz.Y/total;
        float lum=MathF.Pow(Math.Clamp(xyz.Y,0,65504),.9811f),mult=lum/MathF.Max(cy,.0001f);
        xyz=new(cx*mult,lum,(1-cx-cy)*mult);
        p=Desaturate(XyzToAp1(xyz),.93f);
        xyz=Ap1ToXyz(p);
        xyz=Rows(xyz,new(.98722400f,-.00611327f,.0159533f),new(-.00759836f,1.00186000f,.0053302f),new(.00307257f,-.00509595f,1.0816800f));
        return Rows(xyz,new(3.2409699419f,-1.5373831776f,-.4986107603f),new(-.9692436363f,1.8759675015f,.0415550574f),new(.0556300797f,-.2039769589f,1.0569715142f));
    }
    public static float FilmicRaw(float x) => (A*x*x+C*B*x+D*E)/(A*x*x+B*x+D*F)-E/F;
    public static float GlDecode(float s) => s*(s*(.305306011f*s+.682171111f)+.012522878f);
    public static float GlOutput(float albedo)
    {
        float y=FilmicRaw(1.12f*GlDecode(albedo))/FilmicWhite;
        return MathF.Max(0,1.055f*MathF.Pow(MathF.Max(0,y),.416666667f)-.055f);
    }
    public static float CompensateChannel(float display)
    {
        if(!float.IsFinite(display)||display<0||display>1.000001f)throw new ArgumentOutOfRangeException(nameof(display));
        if(display<=0)return 0;
        display=MathF.Min(1,display);
        float target=MathF.Pow((display+.055f)/1.055f,2.4f);
        float q=target*FilmicWhite+E/F;
        float aa=A*(1-q),bb=B*(C-q),cc=D*(E-F*q);
        if(aa<=0)throw new InvalidOperationException("Target outside Filmic inverse domain");
        float discriminant=bb*bb-4*aa*cc;
        if(discriminant<0)throw new InvalidOperationException("Invalid Filmic inverse discriminant");
        float root=MathF.Sqrt(discriminant);
        float exposed=bb>=0 ? -2*cc/(bb+root) : (-bb+root)/(2*aa);
        float linear=exposed/1.12f;
        if(!float.IsFinite(linear)||linear<0||linear>1)throw new InvalidOperationException("Target outside GL ALBEDO range");
        float x=EncodeSrgb(linear);
        for(int i=0;i<6;i++)x=Clamp01(x-(GlDecode(x)-linear)/(.915918033f*x*x+1.364342222f*x+.012522878f));
        return x;
    }
}
