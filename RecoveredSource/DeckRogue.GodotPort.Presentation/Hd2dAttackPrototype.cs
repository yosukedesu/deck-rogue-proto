using System;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

// Reviewed HD2D effects. Physical profiles opt in explicitly; fire remains separate.
// No engine events or combat state are owned here.
// The caller supplies age relative to the existing authoritative contact.
internal static class Hd2dAttackPrototype
{
    internal static bool Enabled = !Array.Exists(OS.GetCmdlineUserArgs(), x => x == "--hd2d-baseline");
    internal static bool Supports(string key) => Enabled && (PhysicalVfxProfiles.ForRoute(key) != null || ElementalVfxProfiles.ForRoute(key) != null);
    internal static bool SupportsRoute(AttackRoute? route) => route?.SourceKind=="card" && Supports(route.Key);
    internal static bool UsesWeaponSource(AttackRoute? route) => SupportsRoute(route) && UsesWeaponRoute(route!.Key);
    internal static bool UsesCastSource(AttackRoute? route) => SupportsRoute(route) && UsesCastRoute(route!.Key);
    internal static bool UsesGroundPressure(string? card) => Enabled && PhysicalVfxProfiles.ForCard(card)?.Family==PhysicalVfxFamily.GroundPressure;
    internal static bool UsesWeaponContact(string? card) => Enabled && PhysicalVfxProfiles.ForCard(card)?.Anchor == PhysicalVfxAnchor.ActualWeaponToReachableSurface;
    internal static bool UsesWeaponRoute(string? key) => Enabled && PhysicalVfxProfiles.ForRoute(key)?.Anchor == PhysicalVfxAnchor.ActualWeaponToReachableSurface;
    internal static bool UsesCastEmitter(string? card) => Enabled && (ElementalVfxProfiles.ForCard(card)!=null || PhysicalVfxProfiles.ForCard(card)?.Anchor==PhysicalVfxAnchor.ActualCastToSelectedReceiver);
    internal static bool UsesCastRoute(string? key) => Enabled && (ElementalVfxProfiles.ForRoute(key)!=null || PhysicalVfxProfiles.ForRoute(key)?.Anchor==PhysicalVfxAnchor.ActualCastToSelectedReceiver);
    internal static bool UsesFire(string? card) => Enabled && ElementalVfxProfiles.ForCard(card)?.Family==ElementalVfxFamily.Fire;
    internal static bool UsesFireSource(AttackRoute? route) => SupportsRoute(route) && UsesFire(route!.DefinitionId);
    internal static bool HasAuthoredAudio(string? card) => Enabled && (PhysicalVfxProfiles.ForCard(card)!=null || UsesFire(card));
    internal static float CastTravel(string key,float t) { float lead=ElementalVfxProfiles.ForRoute(key)?.LaunchLead??.17f; return Ease((t+lead)/lead); }
    internal static string AudioCard(string? card) => PhysicalVfxProfiles.ForCard(card)?.AudioCardId ?? (UsesFire(card)?"red_strike":card) ?? "";
    internal static float Contact(string card) => PhysicalVfxProfiles.ForCard(card)?.Phases.Contact ?? ElementalVfxProfiles.ForCard(card)?.Phases.Contact ?? .32f;
    internal static float PoseLead(string card) => PhysicalVfxProfiles.ForCard(card)?.Phases.PoseLead ?? ElementalVfxProfiles.ForCard(card)?.Phases.PoseLead ?? .22f;
    internal static float Lead(string key) => PhysicalVfxProfiles.ForRoute(key)?.Phases.Anticipation ?? ElementalVfxProfiles.ForRoute(key)?.Phases.Anticipation ?? .22f;
    internal static float Tail(string key) => PhysicalVfxProfiles.ForRoute(key)?.Phases.Release ?? ElementalVfxProfiles.ForRoute(key)?.Phases.Release ?? .50f;

    private sealed class Layer
    {
        internal readonly Vector3[] P = new Vector3[512];
        internal readonly Vector2[] UV = new Vector2[512];
        internal readonly float[] A = new float[512];
        internal readonly float[] G = new float[512];
        internal readonly int[] R = new int[512];
        internal int N;
        internal NativeBatch Batch;
        internal Layer(int space, int kind, string texture="")
        {
            int[] indices = new int[P.Length];
            for (int i=0;i<indices.Length;i++) indices[i]=i;
            Batch = new NativeBatch { Count=P.Length, Space=space, Kind=kind, Texture=texture, Indices=indices,
                Position=i=>P[i], Uv=i=>UV[i], Alpha=i=>A[i], Gain=i=>G[i], Role=i=>R[i],
                Normal=i=>space==1?Vector3.Up:Vector3.Back };
        }
        internal void Reset() { Array.Clear(A); N=0; }
        internal void V(Vector3 p, Vector2 uv, float a, float g, int role)
        { if (N>=P.Length) throw new InvalidOperationException("Prototype geometry capacity exceeded"); P[N]=p;UV[N]=uv;A[N]=Math.Clamp(a,0,1);G[N]=g;R[N]=role;N++; }
        internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,float opacity,int role,float gain=1)
        { V(a,new(0,0),opacity,gain,role);V(b,new(1,0),opacity,gain,role);V(c,new(1,1),opacity,gain,role);V(a,new(0,0),opacity,gain,role);V(c,new(1,1),opacity,gain,role);V(d,new(0,1),opacity,gain,role); }
        internal void Stroke(Vector3 a,Vector3 b,float width,float opacity,int role,float gain=1,bool ground=false)
        { Vector3 v=(b-a).Normalized();Vector3 n=(ground?new Vector3(-v.Z,0,v.X):new Vector3(-v.Y,v.X,0))*width;
          Quad(a-n,b-n,b+n,a+n,opacity,role,gain); }
        internal void Taper(Vector3 a,Vector3 b,float width,float opacity,int role,float gain=1)
        { Vector3 v=(b-a).Normalized(),n=new(-v.Y*width,v.X*width,0);Quad(a-n,b,b,a+n,opacity,role,gain); }
        internal void Sprite(Vector3 c,float rx,float ry,float angle,float opacity,int role,float gain=1)
        { Vector3 along=new(MathF.Cos(angle)*rx,MathF.Sin(angle)*rx,0),across=new(-MathF.Sin(angle)*ry,MathF.Cos(angle)*ry,0);
          Quad(c-along-across,c+along-across,c+along+across,c-along+across,opacity,role,gain); }
        internal void MaterialSprite(Vector3 c,float rx,float ry,float angle,float opacity,float gain=1,int regionTag=0)
        { int start=N; Sprite(c,rx,ry,angle,opacity,3,gain); for(int i=start;i<N;i++) { UV[i].Y=1-UV[i].Y; UV[i].X+=regionTag*2; } }
        internal void Anchored(Vector3 anchor,Vector2 uvAnchor,float rx,float ry,float angle,float opacity,float gain=1,int regionTag=0)
        {
            Vector3 along=new(MathF.Cos(angle)*rx,MathF.Sin(angle)*rx,0), across=new(-MathF.Sin(angle)*ry,MathF.Cos(angle)*ry,0);
            MaterialSprite(anchor-along*(2*uvAnchor.X-1)-across*(1-2*uvAnchor.Y),rx,ry,angle,opacity,gain,regionTag);
        }
        internal void MaterialRegion(Vector3 c,float rx,float ry,float angle,float opacity,Vector2 uv0,Vector2 uvSize,float gain=1,int regionTag=0)
        { int start=N; MaterialSprite(c,rx,ry,angle,opacity,gain); for(int i=start;i<N;i++) UV[i]=uv0+UV[i]*uvSize+new Vector2(regionTag*2,0); }
        internal void Ribbon(Vector3 center,float rx,float ry,float start,float end,float width,float opacity,int role=3,bool trailingFade=true)
        {
            for(int i=0;i<24;i++)
            {
                float u=i/24f,v=(i+1)/24f,a=start+(end-start)*u,b=start+(end-start)*v;
                Vector3 pa=center+new Vector3(MathF.Cos(a)*rx,MathF.Sin(a)*ry,0),pb=center+new Vector3(MathF.Cos(b)*rx,MathF.Sin(b)*ry,0);
                Vector3 na=new(MathF.Cos(a),MathF.Sin(a),0),nb=new(MathF.Cos(b),MathF.Sin(b),0);
                float wa=width*MathF.Sin(u*MathF.PI),wb=width*MathF.Sin(v*MathF.PI);
                int first=N;
                Quad(pa-na*wa,pb-nb*wb,pb+nb*wb,pa+na*wa,opacity*(trailingFade?(.12f+.88f*v*v):(.45f+.55f*v)),role,1.2f);
                // Continuous ribbon UV: per-segment 0..1 edge fade made a serrated comb.
                UV[first]=new(u,0);UV[first+1]=new(v,0);UV[first+2]=new(v,1);
                UV[first+3]=new(u,0);UV[first+4]=new(v,1);UV[first+5]=new(u,1);
            }
        }
        internal void WeaponTrace(Vector3 from,Vector3 to,float trim,float width,float opacity,int role,Vector3? controlOverride=null)
        {
            Vector3 control=controlOverride??((from+to)*.5f+new Vector3(0,.10f,0));
            Vector3 At(float q)=>(1-q)*(1-q)*from+2*(1-q)*q*control+q*q*to;
            for(int i=0;i<12;i++)
            {
                float u=i/12f,v=(i+1)/12f;
                Vector3 a=At(trim+(1-trim)*u),b=At(trim+(1-trim)*v),d=(b-a).Normalized();
                Vector3 n=new(-d.Y,d.X,0);
                float wa=width*MathF.Sin(u*MathF.PI),wb=width*MathF.Sin(v*MathF.PI);
                int first=N;Quad(a-n*wa,b-n*wb,b+n*wb,a+n*wa,opacity*(.15f+.85f*v),role,1.2f);
                UV[first]=new(u,0);UV[first+1]=new(v,0);UV[first+2]=new(v,1);
                UV[first+3]=new(u,0);UV[first+4]=new(v,1);UV[first+5]=new(u,1);
            }
        }
        internal void FluidRibbon(Vector3 start,Vector3 end,float tail,float head,float width,float opacity)
        {
            if(head-tail<.005f || opacity<=0)return;
            Vector3 At(float q)=>start.Lerp(end,q)+Vector3.Up*(MathF.Sin(q*MathF.PI)*.16f);
            for(int i=0;i<18;i++)
            {
                float u=i/18f,v=(i+1)/18f,q=tail+(head-tail)*u,r=tail+(head-tail)*v;
                Vector3 a=At(q),b=At(r),d=(b-a).Normalized(),n=new(-d.Y,d.X,0);
                float wa=width*(.18f+.82f*MathF.Sin(u*MathF.PI)),wb=width*(.18f+.82f*MathF.Sin(v*MathF.PI));
                V(a-n*wa,new(u,1),opacity,1.05f,3);V(b-n*wb,new(v,1),opacity,1.05f,3);V(b+n*wb,new(v,0),opacity,1.05f,3);
                V(a-n*wa,new(u,1),opacity,1.05f,3);V(b+n*wb,new(v,0),opacity,1.05f,3);V(a+n*wa,new(u,0),opacity,1.05f,3);
            }
        }
        internal void GroundShadow(float rx,float rz,float opacity)
        { int start=N; Quad(new(-rx,0,-rz),new(rx,0,-rz),new(rx,0,rz),new(-rx,0,rz),opacity,0,.55f); for(int i=start;i<N;i++) UV[i].X+=2; }
        internal void Dust(Vector3 c,float rx,float ry,float opacity)
        { int start=N; Sprite(c,rx,ry,0,opacity,1); for(int i=start;i<N;i++) UV[i].X+=2; }
        internal void Leaf(Vector3 c,float rx,float ry,float angle,float opacity)
        {
            Vector3 along=new(MathF.Cos(angle)*rx,MathF.Sin(angle)*rx,0),across=new(-MathF.Sin(angle)*ry,MathF.Cos(angle)*ry,0);
            V(c-along,new(.02f,.5f),opacity,1,1);V(c+across,new(.43f,.95f),opacity,1,1);V(c+along,new(.98f,.5f),opacity,1,1);
            V(c-along,new(.02f,.5f),opacity,1,2);V(c+along,new(.98f,.5f),opacity,1,2);V(c-across*.75f,new(.59f,.05f),opacity,1,2);
        }
        internal void Petal(Vector3 c, float rx,float ry,float angle,float opacity,int role,float gain=1)
        { Vector3 along=new(MathF.Cos(angle)*rx,MathF.Sin(angle)*rx,0), across=new(-MathF.Sin(angle)*ry,MathF.Cos(angle)*ry,0);
          Quad(c-along,c-across,c+along,c+across,opacity,role,gain); }
    }
    internal static float FireTravel(float t)=>Ease((t+.17f)/.17f);
    internal static NativeEffect? Create(AttackRoute route)
    {
        if(!SupportsRoute(route)) return null;
        if(PhysicalVfxProfiles.ForRoute(route.Key)?.Material==PhysicalVfxMaterial.Wood)return CreateWoodBlunt(route);
        if(ElementalVfxProfiles.ForRoute(route.Key)?.Family==ElementalVfxFamily.NeutralPressure)return CreateNeutralCastPressure(route);
        if(ElementalVfxProfiles.ForRoute(route.Key)?.Family==ElementalVfxFamily.WaterWide)return CreateRollingWater(route);
        if(PhysicalVfxProfiles.ForRoute(route.Key)?.Family==PhysicalVfxFamily.ConvergingPressure)return CreateOpposedPressure(route);
        if(PhysicalVfxProfiles.ForRoute(route.Key)?.Family==PhysicalVfxFamily.OpticalBind)return CreateOpticalBind(route);
        if(ElementalVfxProfiles.ForRoute(route.Key)?.Family==ElementalVfxFamily.EarthFracture)return CreateEarthFracture(route);
        if(ElementalVfxProfiles.ForRoute(route.Key)?.Family==ElementalVfxFamily.LightScatter)return CreateLightScatter(route);
        if(ElementalVfxProfiles.ForRoute(route.Key)?.Family==ElementalVfxFamily.Dark)return CreateDark(route);
        if(ElementalVfxProfiles.ForRoute(route.Key)?.Family==ElementalVfxFamily.Light)return CreateLight(route);
        if(ElementalVfxProfiles.ForRoute(route.Key)?.Family==ElementalVfxFamily.Water)return CreateWater(route);
        if(ElementalVfxProfiles.ForRoute(route.Key)?.Family==ElementalVfxFamily.IceScatter)return CreateIceScatter(route);
        if(ElementalVfxProfiles.ForRoute(route.Key)?.Family==ElementalVfxFamily.Ice)return CreateIce(route);
        bool fire=UsesFireSource(route),stomp=PhysicalVfxProfiles.ForRoute(route.Key)?.Family==PhysicalVfxFamily.GroundPressure;
        bool blunt=PhysicalVfxProfiles.ForRoute(route.Key)?.Family==PhysicalVfxFamily.BluntImpact;
        bool thrust=PhysicalVfxProfiles.ForRoute(route.Key)?.Family==PhysicalVfxFamily.AxialThrust;
        bool rake=PhysicalVfxProfiles.ForRoute(route.Key)?.Family==PhysicalVfxFamily.OpticalRake;
        bool vegetalMaterial=PhysicalVfxProfiles.ForRoute(route.Key)?.Material==PhysicalVfxMaterial.Vegetal;
        bool vegetalCast=vegetalMaterial && PhysicalVfxProfiles.ForRoute(route.Key)?.Anchor==PhysicalVfxAnchor.ActualCastToSelectedReceiver;
        bool emberMaterial=PhysicalVfxProfiles.ForRoute(route.Key)?.Material==PhysicalVfxMaterial.Ember;
        var body=new Layer(stomp?1:0,vegetalMaterial?19:!fire&&!stomp?7:0,fire?"hd2d-fire-projectile-v1":"");
        var edge=new Layer(stomp?1:0,1); var particles=new Layer(stomp?2:0,2,stomp?"hd2d-earth-rock-v1":"");
        var dust=new Layer(stomp?2:0,fire?0:4,stomp?"hd2d-earth-dust-v1":fire?"hd2d-fire-projectile-phase-03":"");
        var foot=new Layer(stomp?2:0,vegetalMaterial?20:fire?0:5,stomp?"hd2d-stomp-aura-v2":fire?"hd2d-fire-projectile-phase-02":"");
        var pressureAir=new Layer(2,3,"hd2d-stomp-pressure-v1");
        var phases=new Layer[4];
        for(int i=0;i<4;i++) phases[i]=new Layer(stomp?(i==1?1:2):0,stomp&&i==1?8:vegetalMaterial&&i==1?17:vegetalMaterial&&i==2?18:emberMaterial&&i==2?15:!fire&&!stomp&&i==0?6:!fire&&!stomp&&i==2?9:stomp&&i==3?4:3,fire?"hd2d-fire-volume-"+(i+1).ToString("00"):stomp?(i==0?"hd2d-earth-impact-v1":i==1?"":i==2?"":"hd2d-earth-dust-v1"):(emberMaterial&&i==2?"hd2d-fire-volume-04":i==1||i==2?(blunt?"hd2d-stomp-pressure-v1":"hd2d-physical-slash-v1"):""));
        Vector3 source=new(-1,0,0),weaponOrigin=source;float age=0,weaponTraceLength=.16f,stompTop=2.5f;bool remote=false,sourceGesture=true;int weaponFrame=0;
        var effect=new NativeEffect { Shader="Hd2dPrototype",Palette=fire?"hd2d-fire":stomp?"hd2d-stomp":emberMaterial?"hd2d-ember-thrust":vegetalMaterial?"hd2d-vegetal-cut":"hd2d-strike",
            Batches=stomp?new[]{body.Batch,dust.Batch,phases[1].Batch,foot.Batch,phases[2].Batch,phases[0].Batch,edge.Batch,particles.Batch,pressureAir.Batch,phases[3].Batch}:fire?new[]{body.Batch,foot.Batch,phases[0].Batch,phases[1].Batch,phases[2].Batch,phases[3].Batch,edge.Batch,particles.Batch,dust.Batch}:new[]{phases[0].Batch,body.Batch,foot.Batch,phases[1].Batch,phases[2].Batch,phases[3].Batch,edge.Batch,particles.Batch,dust.Batch},
            TargetPeak=fire?.105f:stomp?.10f:.11f,FloorPeak=fire?.34f:stomp?.30f:.29f,
            ConfigureSource=v=>source=v,ConfigureRemoteStrike=vegetalCast?null:v=>remote=v,ConfigureSourceGesture=v=>sourceGesture=v,ConfigureWeaponFrame=vegetalCast?null:v=>weaponFrame=v,ConfigureWeaponOrigin=vegetalCast?null:v=>weaponOrigin=v,ConfigureWeaponTraceLength=vegetalCast?null:v=>weaponTraceLength=v,ConfigureStompTop=v=>stompTop=v,VisualAge=()=>age,
            StageFocus=()=>Ease((age+Lead(route.Key))/.12f)*(1-Ease((age-.12f)/.38f)),
            Light=()=>age<0?(fire?.3f*Ease((age+.28f)/.11f):0):fire?FireImpactLight(age):(1-Ease((age-.045f)/(stomp?.16f:.12f))),
            FloorLight=()=>age<0?(fire?.18f*Ease((age+.28f)/.12f):stomp?.07f*Ease((age+.5f)/.25f):0):MathF.Exp(-age/(stomp?.30f:.25f)) };
        effect.Sample=t=>{
            age=(float)t;pressureAir.Reset();body.Reset();edge.Reset();particles.Reset();dust.Reset();foot.Reset();foreach(var phase in phases)phase.Reset();
            if(age < -Lead(route.Key)-.01f || age>Tail(route.Key))return;
            if(vegetalCast)
            {
                if(age<0)VegetalCastLead(body,edge,particles,phases[1],source,age,sourceGesture);
                else Strike(body,edge,particles,phases[0],phases[1],phases[2],source,source,0,false,0,age,false,true,foot);
            }
            else if(fire)Fire(body,foot,dust,phases,edge,particles,source,age);else if(stomp)Stomp(body,phases,edge,particles,dust,foot,pressureAir,stompTop,age<0?age*(.50f/Lead(route.Key)):age);else if(rake)Rake(body,edge,particles,phases[0],phases[1],phases[2],source,weaponOrigin,weaponTraceLength,remote,weaponFrame,age,sourceGesture);else if(thrust)Thrust(body,edge,particles,dust,phases[0],phases[1],phases[2],source,weaponOrigin,weaponTraceLength,remote,weaponFrame,age,sourceGesture,emberMaterial);else if(blunt)Blunt(body,edge,particles,dust,phases[0],phases[1],phases[2],source,weaponOrigin,weaponTraceLength,remote,weaponFrame,age,sourceGesture);else Strike(body,edge,particles,phases[0],phases[1],phases[2],source,weaponOrigin,weaponTraceLength,remote,weaponFrame,age,sourceGesture,vegetalMaterial,foot);
            float impactScale=PhysicalVfxProfiles.ForRoute(route.Key)?.ImpactScale??1f;
            if(impactScale!=1f && (stomp || age>=0))
            {
                void Scale(Layer layer){for(int k=0;k<layer.N;k++)layer.P[k]*=impactScale;}
                Scale(body);Scale(edge);Scale(particles);Scale(dust);Scale(foot);Scale(pressureAir);
                foreach(var phase in phases)Scale(phase);
            }
        };
        return effect;
    }
    // Caster-local optical fibres, not a weapon trace or a rooted creature.
    // Target material is sampled only at positive age by the existing Strike.
    private static void VegetalCastLead(Layer body,Layer edge,Layer motes,Layer sheet,Vector3 source,float t,bool gesture)
    {
        // Unequal curved fibres taper at BOTH ends. Continuous UVs feather the
        // whole strand, rather than exposing a rectangular base or radial Y.
        void Strand(Layer layer,Vector3 from,Vector3 to,float bend,float width,float alpha,int role)
        {
            if(alpha<=0 || from.DistanceSquaredTo(to)<.00001f)return;
            Vector3 direction=(to-from).Normalized(),normal=new(-direction.Y,direction.X,0);
            Vector3 At(float q)=>from.Lerp(to,q)+normal*(MathF.Sin(q*MathF.PI)*bend);
            for(int k=0;k<8;k++)
            {
                float u=k/8f,v=(k+1)/8f;
                float wa=width*MathF.Pow(MathF.Max(0,MathF.Sin(u*MathF.PI)),.8f);
                float wb=width*MathF.Pow(MathF.Max(0,MathF.Sin(v*MathF.PI)),.8f);
                Vector3 x=At(u),y=At(v);int first=layer.N;
                layer.Quad(x-normal*wa,y-normal*wb,y+normal*wb,x+normal*wa,alpha,role,1.05f);
                layer.UV[first]=new(u,0);layer.UV[first+1]=new(v,0);layer.UV[first+2]=new(v,1);
                layer.UV[first+3]=new(u,0);layer.UV[first+4]=new(v,1);layer.UV[first+5]=new(u,1);
            }
        }
        if(gesture)
        {
            float load=Ease((t+.28f)/.07f)*(1-Ease((t+.16f)/.05f));
            for(int i=0;i<2;i++)
            {
                float angle=.7f+i*2.35f,radius=(.19f+i*.035f)*(1-Ease((t+.28f)/.15f));
                Vector3 axis=new(MathF.Cos(angle),MathF.Sin(angle),0),normal=new(-axis.Y,axis.X,0);
                Vector3 from=source+axis*radius+normal*(.045f+i*.018f);
                Vector3 to=source+axis*(radius*.20f)+normal*(i==0?-.015f:.025f);
                Strand(body,from,to,i==0?.055f:-.035f,.020f-i*.004f,load*(.70f-i*.12f),1);
                Strand(edge,from+normal*.008f,to+normal*.004f,i==0?.05f:-.03f,.006f,load*.45f,2);
            }
        }
        float travel=Ease((t+.17f)/.17f);if(travel<=0)return;
        Vector3 target=new(-.08f,.12f,.015f),delta=target-source;
        if(delta.LengthSquared()<.0001f)return;
        Vector3 direction=delta.Normalized(),side=new(-direction.Y,direction.X,0);
        Vector3 head=source.Lerp(target,travel)+side*(MathF.Sin(travel*MathF.PI)*.10f);
        float available=MathF.Max(.015f,head.DistanceTo(source)),alpha=Ease(travel/.12f);
        for(int i=0;i<3;i++)
        {
            float length=MathF.Min(i==0?.30f:i==1?.21f:.26f,available);
            float offset=(i-1)*.032f;
            Vector3 tip=head+side*offset-direction*(i==1?.015f:i==2?.045f:0f);
            Vector3 tail=tip-direction*length+side*(i==1?.025f:-.018f);
            Strand(body,tail,tip,(i==1?-.045f:.035f)*(1+.25f*MathF.Sin(travel*4+i)),.022f-i*.004f,alpha*(.72f-i*.12f),1);
            if(i==0)Strand(edge,tail+side*.006f,tip+side*.003f,.035f,.006f,alpha*.48f,2);
        }
    }

    private static NativeEffect CreateWoodBlunt(AttackRoute route)
    {
        var trace=new Layer(0,7);var edge=new Layer(0,1);var chips=new Layer(0,2);
        var dust=new Layer(0,4);var core=new Layer(0,6);
        var compression=new Layer(0,3,"hd2d-wood-contact-01");
        var split=new Layer(0,3,"hd2d-wood-contact-02");
        Vector3 source=new(-1,0,0),weaponOrigin=source;
        float age=0,traceLength=.16f;bool remote=false,gesture=true;int frame=0;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-wood",
            Batches=new[]{core.Batch,trace.Batch,compression.Batch,split.Batch,edge.Batch,chips.Batch,dust.Batch},
            TargetPeak=.10f,FloorPeak=.25f,VisualAge=()=>age,
            ConfigureSource=v=>source=v,ConfigureRemoteStrike=v=>remote=v,
            ConfigureSourceGesture=v=>gesture=v,ConfigureWeaponFrame=v=>frame=v,
            ConfigureWeaponOrigin=v=>weaponOrigin=v,ConfigureWeaponTraceLength=v=>traceLength=v,
            StageFocus=()=>Ease((age+Lead(route.Key))/.12f)*(1-Ease((age-.10f)/.30f)),
            Light=()=>age<0?0:1-Ease((age-.035f)/.10f),
            FloorLight=()=>age<0?0:MathF.Exp(-age/.20f)
        };
        effect.Sample=t=>{
            age=(float)t;trace.Reset();edge.Reset();chips.Reset();dust.Reset();core.Reset();compression.Reset();split.Reset();
            if(age < -Lead(route.Key)-.01f || age>Tail(route.Key))return;
            if(age<0) {
                // Reuse only the actual weapon-led approach, not the Cut contact.
                Strike(trace,edge,chips,core,compression,split,source,weaponOrigin,traceLength,remote,frame,age,gesture);
                return;
            }
            float open=Ease((age-.025f)/.095f),clear=1-Ease((age-.16f)/.19f);
            float punch=1-Ease((age-.025f)/.055f);
            core.Sprite(new(0,0,.02f),.15f,.11f,-.18f,punch*.55f,3);
            // Original 1254-square canvases preserve measured common pivot (.34,.56).
            Vector2 pivot=new(.34f,.56f);
            compression.Anchored(new(0,0,.025f),pivot,.78f,.78f,-.12f,(1-open)*.96f,1.05f);
            split.Anchored(new(.035f*open,-.055f*open,.027f),pivot,.78f+.055f*open,.78f+.055f*open,-.12f-.055f*open,open*clear*.94f,1.0f);
            for(int i=0;i<5;i++) {
                float a=age-.025f-.007f*i;if(a<0)continue;
                float side=i%2==0?1:-1;
                Vector3 c=new(.04f+(.45f+.13f*i)*a,side*.06f+(1.0f+.17f*(i%3))*a-4.5f*a*a,.035f);
                float fade=(1-Ease((a-.13f)/.20f))*Ease(a/.025f);
                float angle=.25f+1.1f*i+side*a*(7.0f+i);
                Vector3 along=new(MathF.Cos(angle),MathF.Sin(angle),0),across=new(-along.Y,along.X,0);
                float length=.075f+.019f*(i%3),width=(.010f+.004f*(i%2))*(.22f+.78f*MathF.Abs(MathF.Cos(a*11+i)));
                // Unequal split faces, elongated and dry: no rocks or leaf diamonds.
                chips.Quad(c-along*length-across*width,c+along*length*.83f-across*width*.35f,c+along*length+across*width*.15f,c-along*length*.65f+across*width,fade,1,.9f);
                edge.Stroke(c-along*length*.7f,c+along*length*.75f,width*.18f,fade*.65f,2,.85f);
            }
            float haze=Ease((age-.06f)/.09f)*(1-Ease((age-.17f)/.22f));
            dust.Dust(new(.10f,-.24f,.01f),.23f+age*.35f,.075f+age*.12f,haze*.13f);
        };
        return effect;
    }
    internal static NativeEffect CreateLightningDischarge()
    {
        var shoulder=new Layer(0,21);var channel=new Layer(0,1);var forks=new Layer(0,21);
        var focus=new Layer(0,14);var contact=new Layer(0,3,"lightning-discharge-surface-r2");
        var broken=new Layer(0,3,"lightning-discharge-surface-r2");
        Vector3 source=new(-1,0,0);float age=0;
        float[] stations={0,.055f,.13f,.19f,.225f,.34f,.43f,.51f,.565f,.65f,.77f,.86f,.94f,1};
        float[] offsets={0,.065f,-.025f,.155f,.115f,-.085f,.015f,-.185f,-.105f,.055f,.115f,-.045f,.035f,0};
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-lightning",
            Batches=new[]{focus.Batch,shoulder.Batch,contact.Batch,broken.Batch,forks.Batch,channel.Batch},
            ConfigureSource=v=>source=v,VisualAge=()=>age,
            TargetPeak=.35f,FloorPeak=.12f,StageFocus=()=>0,
            Light=()=>age<0?0:1-Ease(age/.085f)
        };
        effect.Sample=t=>{
            age=(float)t;shoulder.Reset();channel.Reset();forks.Reset();focus.Reset();contact.Reset();broken.Reset();
            // Preserve original LightningDischarge lead .10 and tail .20.
            if(age<-.10f || age>.20f)return;
            float front=Ease((age+.075f)/.075f),conduct=Ease((age+.085f)/.022f)*(1-Ease((age-.008f)/.055f));
            Vector3 end=new(0,0,.025f),delta=end-source;
            Vector3 normal=new(-delta.Y,delta.X,0);normal=normal.Length()>.001f?normal.Normalized():Vector3.Up;
            Vector3 axis=delta.Length()>.001f?delta.Normalized():Vector3.Right;
            Vector3 Point(int i)=>source.Lerp(end,stations[i])+normal*offsets[i]+new Vector3(0,0,.012f);
            void DrawChannel(Vector3 a,Vector3 b,float wa,float wb,float alpha)
            {
                Vector3 d=(b-a).Normalized(),n=new(-d.Y,d.X,0);
                shoulder.Stroke(a,b,(wa+wb)*2.8f,alpha*.68f,1);
                channel.Quad(a-n*wa,b-n*wb,b+n*wb,a+n*wa,alpha,3,1.08f);
            }
            for(int i=0;i<stations.Length-1;i++) {
                float u=stations[i],v=stations[i+1];if(front<=u)break;
                Vector3 a=Point(i),b=Point(i+1),d=(b-a).Normalized(),n=new(-d.Y,d.X,0);
                Vector3 Sub(int j) {
                    float q=j==0?0:j==1?.29f:j==2?.73f:1;
                    float kink=j==0||j==3?0:((i+j)%2==0?1:-1)*(.025f+.018f*MathF.Sin(i*1.93f+j)*MathF.Sin(i*1.93f+j));
                    return a.Lerp(b,q)+n*kink;
                }
                float live=Ease((age+.085f)/.022f)*(1-Ease((age-(.007f+.004f*(i%4)))/.048f));
                for(int j=0;j<3;j++) {
                    float q0=j==0?0:j==1?.29f:.73f,q1=j==0?.29f:j==1?.73f:1;
                    float t0=u+(v-u)*q0,t1=u+(v-u)*q1;if(front<=t0)break;
                    Vector3 pa=Sub(j),pb=Sub(j+1);if(front<t1)pb=pa.Lerp(pb,(front-t0)/(t1-t0));
                    float w0=.005f+.0035f*MathF.Pow(MathF.Sin(t0*27f+.4f),2),w1=.005f+.0035f*MathF.Pow(MathF.Sin(t1*27f+.4f),2);
                    DrawChannel(pa,pb,w0,w1,live);
                }
                if((i==3||i==8) && front>=v) {
                    float side=i==3?1:-1;
                    Vector3 tip=b+axis*(i==3?.48f:.34f)+normal*side*(i==3?.24f:.29f);
                    Vector3 branch=tip-b,bn=new(-branch.Y,branch.X,0);bn=bn.Normalized();
                    Vector3 BranchPoint(int j)=>b.Lerp(tip,j/4f)+bn*(j==0||j==4?0:(j%2==0?-.032f:.024f));
                    for(int j=0;j<4;j++) {
                        Vector3 pa=BranchPoint(j),pb=BranchPoint(j+1);
                        float w0=.0065f*(1-j/4.5f),w1=.0065f*(1-(j+1)/4.5f);
                        forks.Stroke(pa,pb,(w0+w1)*2.2f,live*.64f,2);
                        Vector3 bd=(pb-pa).Normalized(),nn=new(-bd.Y,bd.X,0);
                        channel.Quad(pa-nn*w0,pb-nn*w1,pb+nn*w1,pa+nn*w0,live*.82f,3);
                    }
                }
            }
            if(age<0) {
                focus.Sprite(source,.095f,.095f,0,conduct*.3f,2);
                return;
            }
            float release=Ease((age-.025f)/.065f),clear=1-Ease((age-.075f)/.105f);
            contact.MaterialRegion(new(.015f,.035f,.025f),.52f,.48f,.11f,(1-release)*.92f,new(0,0),new(.5f,1),1.05f);
            broken.MaterialRegion(new(.015f,.035f,.027f),.52f+.045f*release,.48f+.03f*release,.11f,release*clear*.8f,new(.5f,0),new(.5f,1),1.0f);
            for(int i=0;i<5;i++) {
                float a=-1.1f+i*1.19f,r=.17f+.045f*(i%3),fade=clear*(.55f+.08f*(i%3));
                Vector3 dir=new(MathF.Cos(a),MathF.Sin(a),0),side=new(-dir.Y,dir.X,0);
                Vector3 p=dir*(r+.07f*release)+new Vector3(.02f,.025f,.035f);
                Vector3 q=p+dir*(.12f+.035f*(i%2))+side*.055f;
                Vector3 z=q+dir*.075f-side*.065f;
                forks.Stroke(p,q,.013f,fade*.48f,1);forks.Stroke(q,z,.009f,fade*.36f,2);
                channel.Stroke(p,q,.0035f,fade*.75f,3);channel.Stroke(q,z,.0022f,fade*.5f,2);
            }
        };
        return effect;
    }
    internal static NativeEffect CreatePrayerHeal()
    {
        var breath=new Layer(0,14);var streams=new Layer(0,0);var pearls=new Layer(0,14);var cores=new Layer(0,14);
        float age=0;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-prayer",
            Batches=new[]{breath.Batch,streams.Batch,pearls.Batch,cores.Batch},VisualAge=()=>age,
            TargetPeak=.045f,FloorPeak=.045f,StageFocus=()=>0f,
            Light=()=>.35f*Ease(age/.16f)*(1-Ease((age-.26f)/.28f)),
            FloorLight=()=>.12f*Ease(age/.16f)*(1-Ease((age-.28f)/.25f))
        };
        effect.Sample=t=>{
            age=(float)t;breath.Reset();streams.Reset();pearls.Reset();cores.Reset();
            if(age<0 || age>.66f)return;
            float release=Ease((age-.23f)/.32f),pulse=Ease(age/.14f)*(1-Ease((age-.28f)/.28f));
            breath.Sprite(new(0,-.025f,.02f),.31f+.07f*pulse,.35f,0,pulse*.13f,2);
            for(int i=0;i<7;i++)
            {
                float local=age-i*.016f;if(local<0)continue;
                float q=Ease(local/.25f),side=i%2==0?-1:1;
                Vector3 start=new(side*(.50f+.055f*(i%3)),-.27f+.12f*(i%3),.035f);
                Vector3 end=new(side*.17f,.04f+.038f*(i%3),.04f);
                Vector3 At(float u)=>start.Lerp(end,u)+new Vector3(side*.095f*MathF.Sin(u*MathF.PI),.15f*MathF.Sin(u*MathF.PI),0);
                Vector3 center=At(q)+new Vector3(side*release*(i%2==0?.20f:.12f),release*(.56f+.033f*i),0);
                float alpha=Ease(local/.06f)*(1-Ease((local-.39f)/.23f));
                float radius=.031f+.007f*(i%3);
                pearls.Sprite(center,radius*1.8f,radius*1.8f,0,alpha*.72f,2);
                cores.Sprite(center,radius*.42f,radius*.42f,0,alpha*.9f,3,1.05f);
                if(local<.30f)
                {
                    for(int j=0;j<6;j++)
                    {
                        float u=MathF.Max(0,q-.25f)+.25f*j/6f,v=MathF.Min(q,u+.25f/6f);
                        if(v<=u)continue;
                        streams.Stroke(At(u),At(v),.007f*(j+1)/6f,alpha*.38f*(j+1)/6f,1);
                    }
                }
            }
        };
        return effect;
    }
    internal static NativeEffect CreatePermanentPulse()
    {
        var body=new Layer(0,21);var rim=new Layer(0,21);var core=new Layer(0,14);var remnants=new Layer(0,2);
        float age=0;
        var effect=new NativeEffect { Shader="Hd2dPrototype",Palette="hd2d-ordinary-guard",
            Batches=new[]{body.Batch,rim.Batch,core.Batch,remnants.Batch},VisualAge=()=>age,
            TargetPeak=.025f,FloorPeak=.025f,StageFocus=()=>0f,
            Light=()=>age<0?0:MathF.Exp(-age/.07f),FloorLight=()=>0f };
        effect.Sample=t=>{
            age=(float)t;body.Reset();rim.Reset();core.Reset();remnants.Reset();
            if(age<-.055f || age>.28f)return;
            // BoundedContact has zero lead: display the open material AFTER the
            // authoritative seed, then consume it once. No delayed numeric hit.
            float close=Ease(age/.075f),release=Ease((age-.055f)/.12f);
            float opacity=Ease((age+.055f)/.035f)*(1-Ease((age-.055f)/.15f));
            Vector3 center=new(0,.07f,.035f);
            for(int i=0;i<3;i++)
            {
                float angle=i==0?.24f:i==1?2.48f:4.57f;
                Vector3 outward=new(MathF.Cos(angle),MathF.Sin(angle),0);
                float gap=(i==0?.48f:i==1?.39f:.33f)*(1-close)+.075f+release*.05f;
                float length=(i==0?.28f:i==1?.22f:.18f)*(1-.80f*release);
                Vector3 tip=center+outward*gap,tail=tip+outward*length;
                body.Taper(tail,tip,(i==0?.115f:.085f)*(1-.65f*release),opacity*.85f,1);
                rim.Taper(tail-outward*.025f,tip,.018f,opacity*.8f,2);
                if(age>.070f)
                {
                    float dt=age-.070f;
                    Vector3 pos=center+outward*(.28f+i*.035f+dt*.25f)+Vector3.Down*(dt*dt*1.5f);
                    remnants.Taper(pos+outward*.045f,pos,.009f,(1-Ease((dt-.045f)/.13f))*.60f,2);
                }
            }
            if(age>=0)core.Sprite(center,.145f,.105f,.1f,MathF.Exp(-age/.045f)*.38f,3);
        };
        return effect;
    }

    // Material reuse only: no Burn event/stack or caster source is manufactured.
    // Exact installed botanical source: local material only. No weapon/caster
    // callbacks, approach, projectile, bind loop or precontact body is created.
    internal static NativeEffect CreateInstalledPlantContact()
    {
        var fibres=new Layer(0,20);var edge=new Layer(0,20);
        var leaves=new Layer(0,19);var core=new Layer(0,14);float age=0;
        var effect=new NativeEffect {Shader="Hd2dPrototype",Palette="hd2d-vegetal-cut",
            Batches=new[]{fibres.Batch,edge.Batch,leaves.Batch,core.Batch},VisualAge=()=>age,
            TargetPeak=.035f,FloorPeak=0,StageFocus=()=>0f,
            Light=()=>age<0?0:MathF.Exp(-age/.065f),FloorLight=()=>0f};
        effect.Sample=t=>{
            age=(float)t;fibres.Reset();edge.Reset();leaves.Reset();core.Reset();
            if(age<0 || age>.32f)return;
            float close=Ease(age/.075f),release=Ease((age-.055f)/.15f);
            float alpha=1-Ease((age-.065f)/.16f);
            Vector3 center=new(.025f,.075f,.035f);
            for(int i=0;i<3;i++)
            {
                // Enemy receiver's near-side exterior, not a green stripe
                // across the hurt-lit neck. The inward tips stay at contact.
                Vector3 reach=i==0?new(-.76f,.24f,0):i==1?new(-.62f,-.28f,0):new(.16f,.49f,0);
                Vector3 start=center+reach*(1-.14f*close);
                Vector3 end=center+new Vector3(i==0?-.045f:i==1?-.065f:.015f,i==0?.025f:i==1?-.025f:.045f,0);
                Vector3 bend=(start+end)*.5f+new Vector3(i==2?-.10f:-.045f,i==0?.17f:i==1?-.15f:.055f,0);
                start=start.Lerp(end,release*.33f);
                Vector3 At(float u)=>(1-u)*(1-u)*start+2*(1-u)*u*bend+u*u*end;
                for(int j=0;j<10;j++)
                {
                    float u=j/10f,v=(j+1)/10f;
                    Vector3 a=At(u),b=At(v),d=(b-a).Normalized(),n=new(-d.Y,d.X,0);
                    float width=(i==0?.058f:i==1?.049f:.038f)*(1-.65f*release);
                    float wa=width*MathF.Max(0,MathF.Sin(u*MathF.PI));
                    float wb=width*MathF.Max(0,MathF.Sin(v*MathF.PI));
                    int first=fibres.N;
                    fibres.Quad(a-n*wa,b-n*wb,b+n*wb,a+n*wa,alpha*(.94f-i*.08f),1,1.05f);
                    // One continuous fibre UV, not ten independently faded tiles.
                    fibres.UV[first]=new(u,0);fibres.UV[first+1]=new(v,0);fibres.UV[first+2]=new(v,1);
                    fibres.UV[first+3]=new(u,0);fibres.UV[first+4]=new(v,1);fibres.UV[first+5]=new(u,1);
                    if(j>3 && j<9)edge.Stroke(a,b,.0035f*(1-release),alpha*.40f,2);
                }
            }
            core.Sprite(center,.085f,.060f,.2f,MathF.Exp(-age/.035f)*.42f,3);
            for(int i=0;i<2;i++)
            {
                float dt=age-(.065f+i*.025f);if(dt<0)continue;
                float side=i==0?1:-1;
                Vector3 pos=center+new Vector3((i==0?-.64f:-.47f)-dt*.16f,
                    (i==0?.20f:-.19f)-dt*.16f-dt*dt*1.7f,.014f);
                float fade=Ease(dt/.025f)*(1-Ease((dt-.095f)/.14f));
                float turn=.28f+.72f*MathF.Abs(MathF.Cos(dt*14+i));
                leaves.Leaf(pos,.065f,.027f*turn,side*(.6f+dt*7),fade*.72f);
            }
        };
        return effect;
    }

    // Passive installed Static reuses only selected lightning's receiving
    // surface and cooling forks. The Izzet card factory remains independent:
    // no source channel, caster/weapon callback, negative body or floor field.
    internal static NativeEffect CreateInstalledLightningContact()
    {
        var contact=new Layer(0,3,"lightning-discharge-surface-r2");
        var broken=new Layer(0,3,"lightning-discharge-surface-r2");
        var forks=new Layer(0,21);var channel=new Layer(0,1);float age=0;
        var effect=new NativeEffect {Shader="Hd2dPrototype",Palette="hd2d-lightning",
            Batches=new[]{contact.Batch,broken.Batch,forks.Batch,channel.Batch},VisualAge=()=>age,
            TargetPeak=.035f,FloorPeak=0,StageFocus=()=>0f,
            Light=()=>age<0?0:1-Ease(age/.085f),FloorLight=()=>0f};
        effect.Sample=t=>{
            age=(float)t;contact.Reset();broken.Reset();forks.Reset();channel.Reset();
            if(age<0 || age>.20f)return;
            float release=Ease((age-.025f)/.065f),clear=1-Ease((age-.075f)/.105f);
            contact.MaterialRegion(new(.015f,.035f,.025f),.52f,.48f,.11f,(1-release)*.92f,new(0,0),new(.5f,1),1.05f);
            broken.MaterialRegion(new(.015f,.035f,.027f),.52f+.045f*release,.48f+.03f*release,.11f,release*clear*.8f,new(.5f,0),new(.5f,1),1.0f);
            for(int i=0;i<5;i++) {
                float a=-1.1f+i*1.19f,r=.17f+.045f*(i%3),fade=clear*(.55f+.08f*(i%3));
                Vector3 dir=new(MathF.Cos(a),MathF.Sin(a),0),side=new(-dir.Y,dir.X,0);
                Vector3 p=dir*(r+.07f*release)+new Vector3(.02f,.025f,.035f);
                Vector3 q=p+dir*(.12f+.035f*(i%2))+side*.055f;
                Vector3 z=q+dir*.075f-side*.065f;
                forks.Stroke(p,q,.013f,fade*.48f,1);forks.Stroke(q,z,.009f,fade*.36f,2);
                channel.Stroke(p,q,.0035f,fade*.75f,3);channel.Stroke(q,z,.0022f,fade*.5f,2);
            }
        };
        return effect;
    }

    internal static NativeEffect CreateInstalledFireContact()=>CreateBurnStatus();
    internal static NativeEffect CreateInstalledDarkContact(AttackRoute route)=>CreateDark(route,true);

    internal static NativeEffect CreateBurnStatus()
    {
        var body=new Layer(0,0,"hd2d-fire-volume-02");var cooling=new Layer(0,15,"hd2d-fire-volume-04");
        var embers=new Layer(0,2);var warmth=new Layer(0,14);float age=0;
        var effect=new NativeEffect { Shader="Hd2dPrototype",Palette="hd2d-fire",
            Batches=new[]{warmth.Batch,body.Batch,cooling.Batch,embers.Batch},VisualAge=()=>age,
            TargetPeak=.035f,FloorPeak=.015f,StageFocus=()=>0f,
            Light=()=>age<0?0:MathF.Exp(-age/.085f),FloorLight=()=>0f };
        effect.Sample=t=>{
            age=(float)t;body.Reset();cooling.Reset();embers.Reset();warmth.Reset();
            if(age<0 || age>.212f)return;
            float rise=Ease(age/.16f),pulse=1-Ease((age-.025f)/.13f);
            Vector3 center=new(.035f,-.055f,.035f);
            body.MaterialSprite(center+Vector3.Up*(rise*.13f),.40f*(1-.25f*rise),.37f+.10f*rise,-.09f,pulse*.85f);
            float opened=Ease(age/.035f)*(1-Ease((age-.075f)/.125f));
            cooling.MaterialSprite(center+new Vector3(-.10f*rise,.08f+.28f*rise,.008f),.32f,.34f,-.14f-rise*.10f,opened*.70f);
            warmth.Sprite(center,.18f,.16f,0,MathF.Exp(-age/.055f)*.22f,2);
            for(int i=0;i<5;i++)
            {
                float dt=age-i*.011f;if(dt<0)continue;
                float side=i%2==0?-1:1;
                Vector3 start=center+new Vector3(side*(.13f+.045f*(i%3)),.06f+i*.025f,.015f);
                Vector3 velocity=new(side*(.20f+.045f*i),.80f+.10f*(i%3),0);
                Vector3 pos=start+velocity*dt+Vector3.Down*(dt*dt*.8f);
                float opacity=Ease(dt/.018f)*(1-Ease((age-.09f)/.11f));
                embers.Taper(pos-velocity.Normalized()*(i==0?.045f:.027f),pos,i==0?.013f:.009f,opacity*.75f,2);
            }
        };
        return effect;
    }

    private static NativeEffect CreateNeutralCastPressure(AttackRoute route)
    {
        var packet=new Layer(0,21);var sheets=new Layer(0,21);var edge=new Layer(0,21);
        var core=new Layer(0,14);var flecks=new Layer(0,22);
        var pressure=new Layer(0,3,"hd2d-stomp-pressure-v1");
        Vector3 source=new(-1,0,0);float age=0;bool gesture=true;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-strike",
            Batches=new[]{packet.Batch,core.Batch,pressure.Batch,sheets.Batch,edge.Batch,flecks.Batch},
            ConfigureSource=v=>source=v,ConfigureSourceGesture=v=>gesture=v,VisualAge=()=>age,
            TargetPeak=.075f,FloorPeak=.09f,
            StageFocus=()=>Ease((age+.25f)/.12f)*(1-Ease((age-.08f)/.25f)),
            Light=()=>age<0?.12f*Ease((age+.26f)/.10f):MathF.Exp(-age/.085f),
            FloorLight=()=>age<0?0:MathF.Exp(-age/.15f)*.28f
        };
        void Curve(Layer layer,Vector3 a,Vector3 c,Vector3 b,float width,float opacity,int role)
        {
            Vector3 At(float u)=>(1-u)*(1-u)*a+2*(1-u)*u*c+u*u*b;
            for(int k=0;k<10;k++)
            {
                float u=k/10f,v=(k+1)/10f;Vector3 p=At(u),q=At(v),d=(q-p).Normalized(),n=new(-d.Y,d.X,0);
                float w=width*MathF.Sin(u*MathF.PI),z=width*MathF.Sin(v*MathF.PI);
                int first=layer.N;layer.Quad(p-n*w,q-n*z,q+n*z,p+n*w,opacity,role,1f);
                layer.UV[first]=new(u,0);layer.UV[first+1]=new(v,0);layer.UV[first+2]=new(v,1);
                layer.UV[first+3]=new(u,0);layer.UV[first+4]=new(v,1);layer.UV[first+5]=new(u,1);
            }
        }
        void Lens(Vector3 center,Vector3 axis,Vector3 normal,float length,float width,float opacity)
        {
            // A soft, filled pressure packet supports the curved edge. Unequal
            // shoulders and double-ended taper avoid a flying slash or solid orb.
            for(int k=0;k<12;k++)
            {
                float u=k/12f,v=(k+1)/12f;
                Vector3 a=center+axis*((u-.5f)*length),b=center+axis*((v-.5f)*length);
                float wa=width*MathF.Pow(MathF.Max(0f,MathF.Sin(u*MathF.PI)),.65f),wb=width*MathF.Pow(MathF.Max(0f,MathF.Sin(v*MathF.PI)),.65f);
                int first=packet.N;
                packet.Quad(a-normal*wa*.72f,b-normal*wb*.72f,b+normal*wb,a+normal*wa,opacity,2,.95f);
                packet.UV[first]=new(u,0);packet.UV[first+1]=new(v,0);packet.UV[first+2]=new(v,1);
                packet.UV[first+3]=new(u,0);packet.UV[first+4]=new(v,1);packet.UV[first+5]=new(u,1);
            }
        }
        effect.Sample=t=>{
            age=(float)t;packet.Reset();sheets.Reset();edge.Reset();core.Reset();flecks.Reset();pressure.Reset();
            if(age<-.29f||age>.43f)return;
            Vector3 axis=new(-source.X,-source.Y,0);if(axis.LengthSquared()<.001f)axis=Vector3.Right;else axis=axis.Normalized();
            Vector3 normal=new(-axis.Y,axis.X,0);
            if(age<0)
            {
                if(!gesture)return;
                if(age<-.18f)
                {
                    float gather=Ease((age+.28f)/.10f),span=.18f-.065f*gather;
                    Lens(source+axis*.085f+normal*.015f,axis,normal,.19f,.085f,gather*.62f);
                    Curve(sheets,source-axis*span-normal*.10f,source+axis*.10f-normal*.02f,source-axis*.025f+normal*.13f,
                        .019f,gather*.64f,2);
                    Curve(edge,source-axis*.12f+normal*.075f,source+axis*.045f+normal*.015f,source+axis*.025f-normal*.085f,
                        .011f,gather*.44f,3);
                    core.Sprite(source,.09f,.065f,0,gather*.13f,2,.9f);return;
                }
                float u=Ease((age+.18f)/.18f);
                Vector3 head=source.Lerp(new Vector3(0,.015f,.035f),u)+Vector3.Up*(MathF.Sin(u*MathF.PI)*.09f);
                float packetSpread=.075f+.025f*MathF.Sin(u*MathF.PI);
                Lens(head-axis*.065f+normal*.012f,axis,normal,.32f,.135f,.74f);
                Curve(sheets,head-axis*.25f-normal*packetSpread,head-axis*.045f+normal*packetSpread*.35f,head+axis*.045f,
                    .026f,.82f,2);
                Curve(sheets,head-axis*.18f+normal*packetSpread*.8f,head+normal*.02f,head+axis*.035f-normal*.025f,
                    .018f,.60f,1);
                Curve(edge,head-axis*.15f-normal*.025f,head-axis*.025f+normal*.045f,head+axis*.035f,
                    .009f,.76f,3);
                core.Sprite(head-axis*.055f,.13f,.065f,MathF.Atan2(axis.Y,axis.X),.16f,2,.95f);
                return;
            }
            float peak=1-Ease((age-.035f)/.115f),open=Ease(age/.095f);
            Vector3 center=new(0,.025f,.035f);float angle=MathF.Atan2(axis.Y,axis.X);
            pressure.Anchored(center,new(.50f,.78f),.28f+.12f*open,.23f+.12f*open,angle-.45f,peak*.75f,1.05f);
            Curve(sheets,center-axis*.13f-normal*(.12f+.15f*open),center+axis*(.16f+.08f*open),center-axis*.09f+normal*(.15f+.13f*open),
                .043f*(1-.4f*open),peak*.80f,2);
            Curve(edge,center-axis*.075f-normal*.10f,center+axis*.12f,center-axis*.035f+normal*.12f,
                .019f,MathF.Exp(-age/.052f)*.75f,3);
            core.Sprite(center,.21f,.16f,angle,MathF.Exp(-age/.05f)*.28f,2,1f);
            for(int i=0;i<4;i++)
            {
                float dt=age-.045f-i*.013f;if(dt<0)continue;
                Vector3 velocity=axis*(.25f+.13f*(i%2))+normal*(i%2==0?.65f:-.43f);
                Vector3 pos=center+normal*((i-1.5f)*.055f)+velocity*dt+Vector3.Down*(dt*dt*.7f);
                flecks.Sprite(pos,.014f+(i==0?.008f:0),.009f,i*.61f,(1-Ease(dt/.20f))*.48f,2,.9f);
            }
        };
        return effect;
    }

    internal static NativeEffect CreateEnemyNeutralContact()
    {
        // Exact receiving-side neutral pressure; the real enemy action supplies
        // anticipation. No breath, sonic ring, projectile or negative-age body.
        var body=new Layer(0,21);
        var core=new Layer(0,14);var remnants=new Layer(0,2);
        Vector3 source=Vector3.Right;float age=0;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-ordinary-guard",
            Batches=new[]{body.Batch,core.Batch,remnants.Batch},
            ConfigureSource=v=>source=v,VisualAge=()=>age,TargetPeak=.065f,FloorPeak=0f,
            StageFocus=()=>0f,Light=()=>age<0?0:MathF.Exp(-age/.065f),FloorLight=()=>0f
        };
        effect.Sample=t=>{
            age=(float)t;body.Reset();core.Reset();remnants.Reset();
            if(age<0||age>.28f)return;
            Vector3 outward=new(source.X,source.Y,0);
            if(outward.LengthSquared()<.001f)outward=Vector3.Right;else outward=outward.Normalized();
            Vector3 normal=new(-outward.Y,outward.X,0);
            Vector3 center=outward*.15f+new Vector3(0,.045f,.035f);
            // The seed/core stays at the committed receiver point. Register the
            // soft shoulder outside the attacker-facing silhouette, not over hurt-white.
            Vector3 shoulder=center+outward*.40f;
            float close=Ease(age/.065f),release=Ease((age-.045f)/.14f);
            float opacity=1-Ease((age-.035f)/.16f);
            // One connected soft pressure body bridges the locked seed to its
            // exterior shoulder. The old separated flecks are release only.
            float length=.61f-.12f*close,width=.14f*(1-.60f*release);
            for(int k=0;k<12;k++)
            {
                float u=k/12f,v=(k+1)/12f;
                Vector3 p=center+outward*(u*length),q=center+outward*(v*length);
                float wp=width*MathF.Pow(MathF.Max(0f,MathF.Sin(u*MathF.PI)),.65f);
                float wq=width*MathF.Pow(MathF.Max(0f,MathF.Sin(v*MathF.PI)),.65f);
                int first=body.N;
                body.Quad(p-normal*wp*.66f,q-normal*wq*.66f,q+normal*wq,p+normal*wp,opacity*.78f,1);
                body.UV[first]=new(u,0);body.UV[first+1]=new(v,0);body.UV[first+2]=new(v,1);
                body.UV[first+3]=new(u,0);body.UV[first+4]=new(v,1);body.UV[first+5]=new(u,1);
            }
            if(age>.065f)
            {
                float dt=age-.065f;
                for(int i=0;i<3;i++)
                {
                    float angle=i==0?.63f:i==1?-.82f:.08f;
                    Vector3 direction=outward*MathF.Cos(angle)+normal*MathF.Sin(angle);
                    Vector3 pos=shoulder+direction*(.12f+i*.020f+dt*.24f)+Vector3.Down*(dt*dt*1.3f);
                    remnants.Taper(pos+direction*.035f,pos,.008f,(1-Ease((dt-.025f)/.13f))*.48f,2);
                }
            }
            core.Sprite(center,.12f,.085f,0,MathF.Exp(-age/.04f)*.30f,3);
        };
        return effect;
    }

    internal static NativeEffect CreateEnemyBlunt()
    {
        // Receiving-side pressure only. The real enemy animation supplies all
        // anticipation; no weapon trace, remote projectile or negative-age hit.
        var pressure=new Layer(0,3,"hd2d-stomp-pressure-v1");
        var body=new Layer(0,7);var edge=new Layer(0,1);
        var chips=new Layer(0,2);var air=new Layer(0,4);var warmth=new Layer(0,14);
        Vector3 source=new(1,0,0);float age=0;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-strike",
            Batches=new[]{air.Batch,warmth.Batch,pressure.Batch,body.Batch,edge.Batch,chips.Batch},
            ConfigureSource=v=>source=v,VisualAge=()=>age,TargetPeak=.10f,FloorPeak=0f,
            StageFocus=()=>0f,Light=()=>age<0?0:MathF.Exp(-age/.075f),FloorLight=()=>0f
        };
        effect.Sample=t=>{
            age=(float)t;pressure.Reset();body.Reset();edge.Reset();chips.Reset();air.Reset();warmth.Reset();
            if(age<0||age>.31f)return;
            Vector3 outward=new(source.X,source.Y,0);
            if(outward.LengthSquared()<.001f)outward=Vector3.Right;else outward=outward.Normalized();
            Vector3 normal=new(-outward.Y,outward.X,0);
            Vector3 center=outward*.17f+new Vector3(0,.025f,.035f);
            Vector3 At(float x,float y)=>center+outward*x+normal*y;
            float peak=1-Ease((age-.035f)/.115f),open=Ease(age/.055f);
            float angle=MathF.Atan2(outward.Y,outward.X);
            pressure.Anchored(center,new(.50f,.78f),.31f+.10f*open,.24f+.08f*open,
                angle-.52f,peak*.88f,1.05f);
            warmth.Sprite(At(.06f,.07f),.25f,.18f,angle+.35f,
                MathF.Exp(-age/.045f)*.30f,2,1f);
            body.Taper(At(-.08f,-.07f),At(.25f,.18f),.075f,peak*.85f,3,1.05f);
            body.Taper(At(-.04f,.025f),At(.08f,.27f),.048f,peak*.58f,2,1f);
            edge.Taper(At(-.06f,-.015f),At(.17f,.09f),.030f,
                MathF.Exp(-age/.045f)*.8f,3,1.1f);
            for(int i=0;i<5;i++)
            {
                float birth=.028f+i*.009f,dt=age-birth;if(dt<0)continue;
                float a=i==0?.31f:i==1?1.03f:i==2?-.76f:i==3?1.64f:-.22f;
                Vector3 direction=outward*MathF.Cos(a)+normal*MathF.Sin(a);
                Vector3 pos=At(.07f,.025f)+direction*(.06f+dt*(i<2?1.35f:.83f))
                    +Vector3.Down*(dt*dt*1.15f);
                float fade=1-Ease(dt/(i<2?.19f:.14f));
                chips.Taper(pos-direction*(i<2?.115f:.040f)*(1-Ease(dt/.22f)),pos,
                    i<2?.017f:.007f,fade*(i<2?.72f:.38f),i<2?3:2,1f);
            }
            float haze=Ease((age-.075f)/.045f)*(1-Ease((age-.12f)/.15f));
            air.Dust(At(.13f+age*.15f,-.06f-age*.12f),.12f+age*.14f,.065f+age*.10f,haze*.10f);
        };
        return effect;
    }

    internal static NativeEffect CreateEnemyPoke()
    {
        var shoulder=new Layer(0,21);var core=new Layer(0,21);var residue=new Layer(0,2);var glow=new Layer(0,14);
        Vector3 source=new(1,0,0);float age=0;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-strike",Batches=new[]{glow.Batch,shoulder.Batch,core.Batch,residue.Batch},
            ConfigureSource=v=>source=v,VisualAge=()=>age,TargetPeak=.09f,FloorPeak=.09f,
            StageFocus=()=>0f,Light=()=>age<0?0:MathF.Exp(-age/.085f),FloorLight=()=>age<0?0:MathF.Exp(-age/.14f)
        };
        effect.Sample=t=>{
            age=(float)t;shoulder.Reset();core.Reset();residue.Reset();glow.Reset();
            if(age<-.07f||age>.28f)return;
            Vector3 direction=new(-source.X,-source.Y,0);if(direction.LengthSquared()<.001f)direction=Vector3.Left;else direction=direction.Normalized();
            Vector3 normal=new(-direction.Y,direction.X,0),center=new(0,.025f,.035f);
            if(age<0)
            {
                // Facing-side shoulder stays outside the hero silhouette and converges
                // on the unchanged contact, never a gap-spanning beam. The enemy's existing
                // authored pose/lunge remains the source, not a new ranged spear.
                float advance=Ease((age+.065f)/.065f);
                Vector3 tip=center-direction*(.70f*(1-advance)+.025f);
                shoulder.Taper(tip-direction*.30f,tip,.040f,advance*.68f,1,1f);
                core.Taper(tip-direction*.22f,tip,.012f,advance*.88f,2,1.08f);
                return;
            }
            float release=Ease(age/.16f),alpha=1-Ease((age-.025f)/.13f);
            Vector3 tipPoint=center-direction*(.025f+.16f*release);
            Vector3 tail=center-direction*(.76f-.18f*release);
            shoulder.Taper(tail,tipPoint,.073f*(1-.55f*release),alpha,1,1.05f);
            core.Taper(tail+direction*.05f,tipPoint,.024f,alpha*(1-Ease(age/.09f)),3,1.15f);
            glow.Sprite(center,.20f,.13f,MathF.Atan2(direction.Y,direction.X),MathF.Exp(-age/.055f)*.24f,2,1f);
            for(int i=0;i<3;i++)
            {
                float dt=age-.025f-i*.008f;if(dt<0)continue;
                Vector3 spread=-direction*(.35f+i*.12f)+normal*(i==0?.65f:i==1?-.45f:.20f);
                Vector3 pos=center-direction*(.31f+i*.09f)+spread*dt+Vector3.Down*(dt*dt*1.8f);
                residue.Taper(pos-spread.Normalized()*(i==0?.05f:.03f),pos,.0065f,
                    (1-Ease((dt-.07f)/.12f))*(i==0?.60f:.40f),2,.85f);
            }
        };
        return effect;
    }

    internal static NativeEffect CreateOrdinaryGuard()
    {
        var bands=new Layer(0,21);var edges=new Layer(0,21);var settle=new Layer(0,14);
        float age=0;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-ordinary-guard",Batches=new[]{settle.Batch,bands.Batch,edges.Batch},VisualAge=()=>age,
            TargetPeak=.028f,FloorPeak=.018f,StageFocus=()=>0f,
            Light=()=>.30f*Ease(age/.10f)*(1-Ease((age-.20f)/.25f)),FloorLight=()=>0f
        };
        void Band(Layer layer,Vector3 center,float rx,float ry,float start,float end,float width,float alpha,int role)
        {
            for(int i=0;i<16;i++)
            {
                float u=i/16f,v=(i+1)/16f,a=start+(end-start)*u,b=start+(end-start)*v;
                Vector3 n=new(MathF.Cos(a),MathF.Sin(a),0),m=new(MathF.Cos(b),MathF.Sin(b),0);
                Vector3 x=center+new Vector3(n.X*rx,n.Y*ry,0),y=center+new Vector3(m.X*rx,m.Y*ry,0);
                int first=layer.N;float wa=width*MathF.Sin(u*MathF.PI),wb=width*MathF.Sin(v*MathF.PI);
                layer.Quad(x-n*wa,y-m*wb,y+m*wb,x+n*wa,alpha,role,.9f);
                layer.UV[first]=new(u,0);layer.UV[first+1]=new(v,0);layer.UV[first+2]=new(v,1);
                layer.UV[first+3]=new(u,0);layer.UV[first+4]=new(v,1);layer.UV[first+5]=new(u,1);
            }
        }
        effect.Sample=t=>{
            age=(float)t;bands.Reset();edges.Reset();settle.Reset();if(age<0||age>.50f)return;
            float brace=Ease(age/.11f),release=Ease((age-.20f)/.27f);
            float alpha=Ease(age/.045f)*(1-release);
            // Neutral optical bracing on the receiving actor's front, not a
            // literal metal shield, crystalline ice plane or outward attack.
            // One broad, feathered front shoulder takes the load; unequal
            // companions converge and stop rather than reading as scan rings.
            Vector3 shoulder=new(.005f+.29f*(1-brace)-release*.045f,-.045f,.038f);
            float load=.70f+.30f*brace;
            Band(bands,shoulder,.285f,.295f,-.98f,.89f,.155f*load,alpha*.82f,1);
            Band(edges,shoulder+new Vector3(.015f,0,.004f),.305f,.295f,-.76f,.72f,.027f,alpha*.90f,2);
            for(int i=0;i<2;i++)
            {
                float localBrace=Ease((age-i*.015f)/.10f);
                Vector3 c=new(.005f+(.20f+i*.08f)*(1-localBrace)-release*.025f,-.075f-i*.035f,.045f+i*.004f);
                float rx=i==0?.19f:.115f,ry=i==0?.245f:.18f;
                Band(bands,c,rx,ry,i==0?-.91f:-.62f,i==0?.65f:.91f,i==0?.038f:.025f,alpha*(i==0?.49f:.32f),1);
                Band(edges,c,rx+.009f,ry,-.49f,i==0?.55f:.72f,.011f,alpha*(i==0?.55f:.35f),2);
            }
            settle.Sprite(new(.13f,-.04f,.025f),.23f,.28f,0,alpha*.075f,1);
            settle.Sprite(new(.08f,-.40f,.025f),.25f,.043f,0,alpha*.13f,1);
        };
        return effect;
    }

    internal static NativeEffect CreateIceGuard()
    {
        var faces=new Layer(0,13);var edges=new Layer(0,1);var halo=new Layer(0,6);var motes=new Layer(0,2);
        float age=0;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-ice",
            Batches=new[]{halo.Batch,faces.Batch,edges.Batch,motes.Batch},VisualAge=()=>age,
            TargetPeak=.055f,FloorPeak=.08f,
            Light=()=>.45f*Ease(age/.08f)*(1-Ease((age-.16f)/.18f)),
            FloorLight=()=>.18f*Ease(age/.10f)*(1-Ease((age-.18f)/.18f)),StageFocus=()=>0f
        };
        effect.Sample=t=>{
            age=(float)t;faces.Reset();edges.Reset();halo.Reset();motes.Reset();
            if(age<0 || age>.52f)return;
            float build=Ease(age/.11f),fade=1-Ease((age-.28f)/.23f);
            halo.Sprite(new(0,0,-.015f),.48f,.38f,0,build*fade*.20f,2,.85f);
            for(int i=0;i<3;i++)
            {
                float local=age-i*.015f;if(local<0)continue;
                float assembly=Ease(local/.085f),alpha=assembly*fade;
                float x=(i-1)*(.20f+.05f*build),rise=-.12f*(1-assembly);
                float lean=(i-1)*.035f;
                Vector3 c=new(x,rise+(i==1?.025f:-.025f),.025f+i*.003f);
                float w=(i==1?.16f:.14f)*(.35f+.65f*assembly),h=i==1?.31f:.27f;
                // Clipped unequal mineral facets: no straight three-pane box.
                // Their shared inward assembly is protective, not a shattering hit.
                Vector3[] rim={
                    c+new Vector3(-w*.52f,-h*.92f,0),
                    c+new Vector3(w*.62f,-h*.73f,0),
                    c+new Vector3(w,h*.18f,0),
                    c+new Vector3(w*.35f+lean,h*(i==1?1.08f:.92f),0),
                    c+new Vector3(-w*.48f+lean,h*.79f,0),
                    c+new Vector3(-w,h*.08f,0)};
                Vector2 Uv(Vector3 v)=>new((v.X-c.X)/(2*w)+.5f,(v.Y-c.Y)/(2*h)+.5f);
                for(int k=0;k<rim.Length;k++)
                {
                    Vector3 a=rim[k],b=rim[(k+1)%rim.Length];
                    float facetAlpha=alpha*(i==1?.27f:.22f)*(k%2==0?1f:.70f);
                    faces.V(c,new(.5f,.5f),facetAlpha,.95f,1);
                    faces.V(a,Uv(a),facetAlpha,.95f,1);faces.V(b,Uv(b),facetAlpha,.95f,1);
                    edges.Stroke(a,b,k==2||k==3?.008f:.0045f,alpha*(k==2||k==3?.74f:.35f),k==3?3:2,1.05f);
                }
                edges.Stroke(c+new Vector3(-w*.20f,-h*.35f,0),rim[3],.003f,alpha*.24f,2);
                edges.Stroke(c+new Vector3(-w*.20f,-h*.35f,0),rim[5],.0025f,alpha*.20f,2);
            }
            float release=Ease((age-.20f)/.10f);
            for(int i=0;i<6;i++)
            {
                float dt=System.Math.Max(0,age-.20f);
                Vector3 p=new((i%3-1)*.20f+(i%2==0?1:-1)*dt*.20f,
                    -.16f+(i/3)*.32f+dt*.45f,.035f);
                motes.Petal(p,.012f,.007f,i*.41f,release*fade*.60f,2);
            }
        };
        return effect;
    }
    private static NativeEffect CreateOpposedPressure(AttackRoute route)
    {
        var lead=new Layer(0,7);var edge=new Layer(0,1);var chips=new Layer(0,2);
        var body=new Layer(0,21);var rim=new Layer(0,21);var glow=new Layer(0,14);
        var unused=new Layer(0,18,"hd2d-physical-slash-v1");
        Vector3 source=new(-1,0,0),origin=source;float age=0,trace=.16f;int frame=0;bool remote=false,gesture=true;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-strike",Batches=new[]{lead.Batch,body.Batch,rim.Batch,edge.Batch,chips.Batch,glow.Batch},
            ConfigureSource=v=>source=v,ConfigureWeaponOrigin=v=>origin=v,ConfigureWeaponFrame=v=>frame=v,
            ConfigureWeaponTraceLength=v=>trace=v,ConfigureRemoteStrike=v=>remote=v,ConfigureSourceGesture=v=>gesture=v,VisualAge=()=>age,
            TargetPeak=.085f,FloorPeak=.23f,
            StageFocus=()=>Ease((age+.14f)/.10f)*(1-Ease((age-.10f)/.25f)),
            Light=()=>age<0?0:MathF.Exp(-age/.12f),FloorLight=()=>age<0?0:MathF.Exp(-age/.19f)
        };
        void Wedge(Layer layer,Vector3 outer,Vector3 tip,float width,float alpha,int role,float gain)
        {
            Vector3 d=(tip-outer).Normalized(),n=new(-d.Y,d.X,0);
            for(int k=0;k<6;k++)
            {
                float u=k/6f,v=(k+1)/6f;
                Vector3 a=outer+(tip-outer)*u+n*(MathF.Sin(u*MathF.PI)*width*.22f);
                Vector3 b=outer+(tip-outer)*v+n*(MathF.Sin(v*MathF.PI)*width*.22f);
                float wa=width*MathF.Sin(u*MathF.PI)*(1-.48f*u),wb=width*MathF.Sin(v*MathF.PI)*(1-.48f*v);
                int first=layer.N;layer.Quad(a-n*wa,b-n*wb,b+n*wb,a+n*wa,alpha,role,gain);
                layer.UV[first]=new(u,0);layer.UV[first+1]=new(v,0);layer.UV[first+2]=new(v,1);
                layer.UV[first+3]=new(u,0);layer.UV[first+4]=new(v,1);layer.UV[first+5]=new(u,1);
            }
        }
        effect.Sample=t=>{
            age=(float)t;lead.Reset();edge.Reset();chips.Reset();body.Reset();rim.Reset();glow.Reset();unused.Reset();
            if(age<-.22f||age>.40f)return;
            if(age<0)Strike(lead,edge,chips,glow,unused,unused,source,origin,trace,remote,frame,age,gesture);
            if(age<-.10f)return;
            // Visible open gap BEFORE the single authoritative contact. Opposing
            // short bodies stop at the center; no long tangential slash/loop.
            float close=Ease((age+.09f)/.09f),release=Ease((age-.045f)/.16f);
            float alpha=Ease((age+.10f)/.035f)*(1-release);
            Vector3 center=new(.15f,.24f,.035f);
            for(int i=0;i<2;i++)
            {
                float sign=i==0?1:-1;
                // Begin partly outside the receiving silhouette, so approach
                // crosses negative space rather than pale chest/shin equipment.
                Vector3 dir=i==0?new(-.58f,.82f,0):new(.72f,-.70f,0);dir=dir.Normalized();
                float distance=(i==0?.36f:.31f)*(1-close)+.040f+release*.025f;
                Vector3 tip=center+dir*distance+new Vector3(0,0,.006f*i);
                float length=(i==0?.36f:.29f)*(1-.74f*release);
                Vector3 outer=tip+dir*length;
                float width=(i==0?.150f:.115f)*(1-.65f*release);
                Wedge(body,outer,tip,width,alpha*(i==0?.96f:.79f),1,1.10f);
                // Short ivory shoulder, not a solid white tooth/central bridge.
                Vector3 side=new(-dir.Y,dir.X,0);
                Wedge(rim,outer+side*width*.25f,tip+side*.012f,width*.28f,alpha*.88f,2,1.18f);
            }
            if(age>=0)
            {
                float glint=1-Ease(age/.038f);
                glow.Sprite(center,.095f,.065f,0,glint*.60f,3,1.15f);
                float tail=Ease((age-.055f)/.035f)*(1-Ease((age-.12f)/.16f));
                for(int i=0;i<3;i++)
                {
                    float side=i%2==0?1:-1;Vector3 pos=center+new Vector3((i-1)*.085f,side*(.07f+release*.08f),.045f);
                    chips.Taper(pos+new Vector3(-.018f,side*.045f,0),pos,.007f,tail*.48f,2,.8f);
                }
            }
        };
        return effect;
    }

    private static NativeEffect CreateOpticalBind(AttackRoute route)
    {
        var lead=new Layer(0,20);var edge=new Layer(0,1);var leaves=new Layer(0,19);
        var body=new Layer(0,20);var fibres=new Layer(0,20);
        var core=new Layer(0,14);var unused=new Layer(0,18,"hd2d-physical-slash-v1");
        Vector3 source=new(-1,0,0),origin=source;float age=0,trace=.16f;int frame=0;bool remote=false,gesture=true;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-vegetal-cut",Batches=new[]{lead.Batch,edge.Batch,body.Batch,fibres.Batch,leaves.Batch,core.Batch},
            ConfigureSource=v=>source=v,ConfigureWeaponOrigin=v=>origin=v,ConfigureWeaponFrame=v=>frame=v,
            ConfigureWeaponTraceLength=v=>trace=v,ConfigureRemoteStrike=v=>remote=v,ConfigureSourceGesture=v=>gesture=v,VisualAge=()=>age,
            TargetPeak=.085f,FloorPeak=.23f,
            StageFocus=()=>Ease((age+.14f)/.10f)*(1-Ease((age-.10f)/.25f)),
            Light=()=>age<0?0:MathF.Exp(-age/.12f),FloorLight=()=>age<0?0:MathF.Exp(-age/.19f)
        };
        void Arc(Layer layer,Vector3 center,float rx,float ry,float start,float end,float width,float alpha,int role,float gain,float fray=0)
        {
            for(int i=0;i<12;i++)
            {
                float u=i/12f,v=(i+1)/12f,a=start+(end-start)*u,b=start+(end-start)*v;
                Vector3 n=new(MathF.Cos(a),MathF.Sin(a),0),m=new(MathF.Cos(b),MathF.Sin(b),0);
                Vector3 x=center+new Vector3(n.X*rx,n.Y*ry,0),y=center+new Vector3(m.X*rx,m.Y*ry,0);
                float wa=width*MathF.Sin(u*MathF.PI),wb=width*MathF.Sin(v*MathF.PI);
                if(fray>0){wa*=.78f+.22f*MathF.Sin(u*19+fray);wb*=.78f+.22f*MathF.Sin(v*19+fray);}
                int first=layer.N;
                layer.Quad(x-n*wa,y-m*wb,y+m*wb,x+n*wa,alpha*(.65f+.35f*u),role,gain);
                layer.UV[first]=new(u,0);layer.UV[first+1]=new(v,0);layer.UV[first+2]=new(v,1);
                layer.UV[first+3]=new(u,0);layer.UV[first+4]=new(v,1);layer.UV[first+5]=new(u,1);
            }
        }
        effect.Sample=t=>{
            age=(float)t;lead.Reset();edge.Reset();leaves.Reset();body.Reset();fibres.Reset();core.Reset();unused.Reset();
            if(age<-.22f||age>.40f)return;
            if(age<0)
            {
                // Existing causal weapon lead/remote travel only; never reuse
                // the outward slash contact geometry for this bind family.
                Strike(lead,edge,leaves,core,body,unused,source,origin,trace,remote,frame,age,gesture,true,fibres);
                return;
            }
            float closure=Ease(age/.085f),release=Ease((age-.10f)/.17f);
            float alpha=Ease((age+.018f)/.035f)*(1-Ease((age-.11f)/.17f));
            Vector3 center=new(.12f,.20f,.025f);
            float rx=.57f-.23f*closure+.07f*release,ry=.39f-.13f*closure+.04f*release;
            core.Sprite(center,.10f*(1-.45f*closure),.065f,0,1-Ease((age-.012f)/.055f),3,1.05f);
            for(int i=0;i<2;i++)
            {
                float side=i==0?1:-1;
                Vector3 c=center+new Vector3(side*.035f*release,side*.045f,.008f*i);
                float start=(i==0?2.0f:3.8f)+side*.20f*release;
                float end=i==0?.45f-.75f*closure+.38f*release:4.95f+1.0f*closure-.45f*release;
                float sx=rx*(i==0?1:.88f),sy=ry*(i==0?1:.83f);
                // Three material strands occupy the former broad band's
                // envelope, with real gaps and unequal tapered advancing ends.
                // No opaque C-shaped plate or continuous bright parallel rim.
                for(int j=0;j<3;j++)
                {
                    float offset=(j-1)*.060f*(1+release*.65f);
                    float length=end-start;
                    float a=start+length*(.025f+j*.045f),b=end-length*(j*.055f);
                    float width=(j==0?.025f:j==1?.028f:.021f)*(i==0?1:.88f)*(1-.25f*release);
                    Arc(body,c,sx+offset,sy+offset,a,b,width,alpha*(i==0?1:.85f)*(1-j*.08f),1,.98f+j*.06f,1+i*3+j);
                }
                if(release>0)
                {
                    float length=end-start;
                    Arc(fibres,c+new Vector3(side*release*.10f,side*.04f*release,0),sx+.045f,sy+.025f,
                        start+length*.27f,end-length*.16f,.014f,alpha*release*.70f,2,.90f,2+i);
                }
            }
            for(int i=0;i<3;i++)
            {
                float dt=age-(.10f+i*.018f);if(dt<0)continue;
                float side=i%2==0?-1:1;
                Vector3 p=center+new Vector3(side*(.28f+dt*(.60f+i*.12f)),.12f+i*.075f+dt*.32f-dt*dt*2.7f,.04f);
                float fade=Ease(dt/.025f)*(1-Ease((dt-.12f)/.14f));
                leaves.Leaf(p,.055f+i*.009f,.022f,i*1.8f+side*dt*8,fade*.85f);
            }
        };
        return effect;
    }

    private static NativeEffect CreateEarthFracture(AttackRoute route)
    {
        var fissure=new Layer(1,3,"earth-ground-grain-r2");
        var lips=new Layer(1,1);var rocks=new Layer(2,2,"hd2d-earth-rock-v1");
        var dust=new Layer(2,4,"hd2d-earth-dust-v1");var focus=new Layer(0,14);
        Vector3 source=new(-1,0,0);float age=0,seed=0;bool sourceGesture=true;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-earth-fracture",Batches=new[]{fissure.Batch,lips.Batch,rocks.Batch,dust.Batch,focus.Batch},
            TargetPeak=.08f,FloorPeak=.24f,ConfigureSource=v=>source=v,ConfigureSourceGesture=v=>sourceGesture=v,ConfigureVisualSeed=v=>seed=v,VisualAge=()=>age,
            StageFocus=()=>Ease((age+.22f)/.12f)*(1-Ease((age-.14f)/.42f)),
            Light=()=>age<0?0:MathF.Exp(-age/.14f),FloorLight=()=>age<0?0:MathF.Exp(-age/.25f)
        };
        effect.Sample=t=>{
            age=(float)t;fissure.Reset();lips.Reset();rocks.Reset();dust.Reset();focus.Reset();
            if(age<-.28f||age>.68f)return;
            if(sourceGesture && age<0)
            {
                float gather=Ease((age+.28f)/.09f)*(1-Ease((age+.06f)/.065f));
                focus.Sprite(source,.10f,.075f,0,gather*.48f,2,.8f);
            }
            float reveal=Ease((age+.13f)/.18f),settle=1-Ease((age-.29f)/.37f);
            float rx=.80f+.18f*reveal,rz=1.25f+.15f*reveal;
            float phase=seed,angle=.22f*MathF.Sin(phase);
            Vector3 Ground(float x,float z)=>new(x*MathF.Cos(angle)-z*MathF.Sin(angle),.007f,x*MathF.Sin(angle)+z*MathF.Cos(angle)+.40f);
            int first=fissure.N;
            fissure.Quad(Ground(-rx,-rz),Ground(rx,-rz),Ground(rx,rz),Ground(-rx,rz),reveal*settle,3,1.0f);
            // Fixed useful rectangle of the left atlas panel: source alpha occupied
            // only1.74% of the full half, losing its cross-section under ground
            // foreshortening. Ground depth/opacity remain unchanged.
            for(int i=first;i<fissure.N;i++)fissure.UV[i]=new(.035f+fissure.UV[i].X*.42f,.39f+(1-fissure.UV[i].Y)*.40f);
            float crest=age<0?reveal*.12f:MathF.Exp(-age/.14f);
            for(int k=0;k<3;k++)
            {
                float a=angle+(k==0?-2.75f:k==1?-.25f:1.47f);
                Vector3 root=Ground(0,.02f),bend=Ground(MathF.Cos(a)*.32f,MathF.Sin(a)*.50f);
                Vector3 end=Ground(MathF.Cos(a)*(.62f+.10f*k),MathF.Sin(a)*(.90f+.08f*k));
                Vector3 advancing=root.Lerp(bend,reveal);
                Vector3 bank=new(-MathF.Sin(a),0,MathF.Cos(a));
                float soil=reveal*settle*.88f;
                lips.Stroke(root,advancing,.070f,soil,0,.9f,true);
                lips.Stroke(root+bank*.050f,advancing+bank*.050f,.016f,crest*.75f,2,1.0f,true);
                if(reveal>.5f)
                {
                    Vector3 tip=bend.Lerp(end,(reveal-.5f)*2);
                    lips.Stroke(bend,tip,.047f,soil*.85f,0,.9f,true);
                    lips.Stroke(bend+bank*.035f,tip+bank*.035f,.011f,crest*.65f,1,.9f,true);
                }
            }
            if(age<0)return;
            // Unequal independent stone pieces, not a replicated crown bitmap.
            for(int i=0;i<7;i++)
            {
                float variation=.5f+.5f*MathF.Sin(seed*1.31f+i*2.17f);
                float other=.5f+.5f*MathF.Sin(seed*2.03f+i*1.47f);
                float birth=.016f*other,dt=age-birth;if(dt<0)continue;
                float side=i%2==0?-1:1;
                float x=side*(.08f+(i%4)*.12f+other*.13f+dt*(.28f+variation*.55f));
                float vy=4.25f+variation*1.05f,gravity=12.5f,flight=vy/gravity;
                float y=MathF.Max(.012f,vy*dt-gravity*dt*dt);
                float size=.11f+other*.105f;
                // Complete the visible ballistic descent before a short ground
                // settle fade; never evaporate a hovering row near the apex.
                float fade=Ease(dt/.010f)*(1-Ease((dt-flight)/.08f));
                rocks.MaterialSprite(new(x,y+.025f,.015f+other*.065f),size,size*(.66f+variation*.33f),
                    seed*.71f+i*1.71f+side*dt*(3.0f+other*4),fade,1.02f,1);
            }
            float d=age-(.035f+.015f*(.5f+.5f*MathF.Sin(seed)));
            if(d>0)
            {
                float amount=Ease(d/.08f)*(1-Ease((d-.23f)/.32f));
                for(int i=0;i<2;i++)
                {
                    float side=i==0?-1:1;
                    dust.MaterialSprite(new(side*(.22f+d*.38f),.12f+d*.12f,.035f),
                        .42f+d*.65f,.18f+d*.18f,side*.12f,amount*.60f,.86f,i+1);
                }
            }
        };
        return effect;
    }

    private static NativeEffect CreateLightScatter(AttackRoute route)
    {
        var shoulder=new Layer(0,6);var rays=new Layer(0,21);var motes=new Layer(0,22);
        var glint=new Layer(0,3,"light-ray-surface-r2");
        Vector3 source=new(-1,0,0);float age=0;bool sourceGesture=true;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-light-scatter",Batches=new[]{shoulder.Batch,rays.Batch,glint.Batch,motes.Batch},
            TargetPeak=.09f,FloorPeak=.21f,ConfigureSource=v=>source=v,ConfigureSourceGesture=v=>sourceGesture=v,VisualAge=()=>age,
            StageFocus=()=>Ease((age+.30f)/.13f)*(1-Ease((age-.09f)/.30f)),
            Light=()=>age<0?0:1-Ease((age-.035f)/.14f),
            FloorLight=()=>age<0?0:MathF.Exp(-age/.19f)
        };
        void Feather(Layer layer,Vector3 start,Vector3 end,float width,float alpha,int role,float gain)
        {
            if(alpha<=0 || start.DistanceSquaredTo(end)<.00001f)return;
            Vector3 direction=(end-start).Normalized(),normal=new(-direction.Y,direction.X,0);
            float bend=MathF.Min(.055f,start.DistanceTo(end)*.10f);
            Vector3 At(float u)=>start.Lerp(end,u)+normal*(MathF.Sin(u*MathF.PI)*bend);
            for(int i=0;i<6;i++)
            {
                float u=i/6f,v=(i+1)/6f;
                Vector3 p=At(u),z=At(v);
                float wa=width*MathF.Sin(u*MathF.PI),wb=width*MathF.Sin(v*MathF.PI);
                int first=layer.N;
                layer.Quad(p-normal*wa,z-normal*wb,z+normal*wb,p+normal*wa,alpha,role,gain);
                layer.UV[first]=new(u,0);layer.UV[first+1]=new(v,0);layer.UV[first+2]=new(v,1);
                layer.UV[first+3]=new(u,0);layer.UV[first+4]=new(v,1);layer.UV[first+5]=new(u,1);
            }
        }
        effect.Sample=t=>{
            age=(float)t;shoulder.Reset();rays.Reset();glint.Reset();motes.Reset();
            if(age<-.31f||age>.44f)return;
            if(age<0)
            {
                if(sourceGesture)
                {
                    float gather=Ease((age+.30f)/.10f)*(1-Ease((age+.17f)/.05f));
                    for(int i=0;i<4;i++)
                    {
                        float angle=.3f+i*1.43f,radius=.27f*(1-Ease((age+.30f)/.16f));
                        Vector3 axis=new(MathF.Cos(angle),MathF.Sin(angle),0),point=source+axis*radius;
                        rays.Taper(point+axis*.10f,point,.009f,gather*(1-i*.1f),2,1.1f);
                        motes.Sprite(point,.025f,.012f,angle,gather,3,1.1f);
                    }
                }
                float flight=CastTravel(route.Key,age);
                if(flight>0)
                {
                    Vector3 target=new(.02f,.12f,0),direction=(target-source).Normalized();
                    Vector3 normal=new(-direction.Y,direction.X,0);
                    Vector3 head=source.Lerp(target,flight)+Vector3.Up*(MathF.Sin(flight*MathF.PI)*.10f);
                    float alpha=Ease(flight/.10f),length=.52f+.18f*MathF.Sin(flight*MathF.PI);
                    if(sourceGesture)
                    {
                        float opening=Ease(flight/.20f)*(1-Ease((flight-.25f)/.40f));
                        float aim=MathF.Atan2(direction.Y,direction.X);
                        for(int i=0;i<3;i++)
                        {
                            float angle=aim+(i-1)*.26f;
                            Vector3 axis=new(MathF.Cos(angle),MathF.Sin(angle),0);
                            Vector3 end=source+axis*(.30f+flight*(.70f-i*.10f));
                            Feather(shoulder,source+axis*.035f,end,.12f-i*.020f,opening*(.68f-i*.10f),1,1.05f);
                            Feather(rays,source+axis*.06f,end,.023f,opening*(.75f-i*.12f),2,1.15f);
                        }
                    }
                    Feather(rays,head-direction*length,head,.043f,alpha,3,1.2f);
                    Feather(shoulder,head-direction*(length+.10f),head,.17f,alpha*.78f,1,1.10f);
                    for(int i=0;i<3;i++)
                    {
                        Vector3 point=head-direction*(.09f+i*.10f)+normal*((i-1)*.09f*MathF.Sin(flight*MathF.PI));
                        motes.Sprite(point,.050f-i*.008f,.020f,MathF.Atan2(direction.Y,direction.X),alpha*(.85f-i*.12f),i==0?3:2,1.1f);
                    }
                }
                return;
            }
            Vector3 contact=new(.02f,.15f,.025f);
            float punch=1-Ease((age-.018f)/.055f);
            // Tiny refractive seed; scatter rays/descending motes carry the body,
            // not a held star icon or a repeated single-target full ray fan.
            Vector2 pivot=new(.595f,.57f);float size=.18f;
            Vector3 center=contact-new Vector3((2*pivot.X-1)*size,(1-2*pivot.Y)*size,0);
            glint.MaterialRegion(center,size,size,0,punch,new(0,0),new(.5f,1),1.1f);
            float orientation=MathF.Atan2(-source.Y,-source.X);
            float receiverPhase=source.Length()*1.71f;
            Vector3 incoming=new(MathF.Cos(orientation),MathF.Sin(orientation),0);
            float compress=1-Ease((age-.035f)/.11f);
            Feather(shoulder,contact-incoming*.30f,contact+incoming*.20f,.20f,compress*.75f,1,1.05f);
            Feather(rays,contact-incoming*.22f,contact+incoming*.16f,.028f,punch,3,1.25f);
            // Continue the incoming spray into unequal swept packets, not a
            // replicated radial LightRay star. Warm material outlives ivory.
            for(int i=0;i<3;i++)
            {
                float release=Ease((age-.04f)/.18f);
                float variation=MathF.Sin(receiverPhase+i*1.37f);
                float angle=orientation+(i==0?-.52f:i==1?.18f:.78f)+variation*.16f+release*((i-1)*.42f+.10f*variation);
                Vector3 axis=new(MathF.Cos(angle),MathF.Sin(angle),0);
                float grow=Ease(age/.055f);
                float length=(i==0?.65f:i==1?.48f:.80f)*grow*(.91f+.13f*variation)*(1-.35f*release);
                Vector3 tip=contact+axis*length+Vector3.Down*(age*age*(i==2?2.2f:1.2f));
                Vector3 start=contact+axis*(length*release*.80f);
                float fade=1-Ease((age-(.055f+i*.010f))/.15f);
                Feather(shoulder,start,tip,(i==1?.13f:.085f)*(1-.5f*release),fade*.68f,1,1.05f);
                Feather(rays,start,tip,.026f*(1-.6f*release),fade*.85f,2,1.15f);
            }
            for(int i=0;i<6;i++)
            {
                float angle=orientation-.70f+i*.29f+.15f*MathF.Sin(receiverPhase+i*1.13f);
                Vector3 axis=new(MathF.Cos(angle),MathF.Sin(angle),0);
                float birth=.025f+i*.006f,dt=age-birth;if(dt<0)continue;
                float distance=.14f+dt*(1.9f+(i%3)*.44f);
                Vector3 point=contact+axis*distance+Vector3.Down*(dt*dt*(2.0f+i*.17f));
                float fade=Ease(dt/.022f)*(1-Ease((dt-.12f)/(.18f+(i%2)*.05f)));
                Vector3 trail=point-axis*(.15f*(1-Ease(dt/.30f)))+Vector3.Up*(dt*.10f);
                Feather(rays,trail,point,.018f+(i%2)*.005f,fade*.8f,2,1.05f);
                motes.Sprite(point,.044f+(i%3)*.008f,.018f,angle+dt*(i%2==0?3:-4),fade*(.9f-i*.06f),2,1.05f);
            }
        };
        return effect;
    }
    internal static NativeEffect CreateNecroDark(AttackRoute route)=>CreateDark(route,true);
    internal static NativeEffect CreateReactionDark(AttackRoute route)=>CreateDark(route,true);

    private static NativeEffect CreateDark(AttackRoute route,bool localOnly=false)
    {
        var body0=new Layer(0,16,"hd2d-dark-contraction-01");
        var body1=new Layer(0,16,"hd2d-dark-contraction-02");
        var body2=new Layer(0,16,"hd2d-dark-contraction-03");
        var seed=new Layer(0,16,"hd2d-dark-contraction-01");
        var veil=new Layer(0,4);var edges=new Layer(0,1);var motes=new Layer(0,2);
        Vector3 source=new(-1,0,0);float age=0;bool sourceGesture=true;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-dark",
            Batches=new[]{veil.Batch,seed.Batch,body0.Batch,body1.Batch,body2.Batch,edges.Batch,motes.Batch},
            TargetPeak=localOnly?.028f:.045f,FloorPeak=localOnly?0f:.15f,ConfigureSource=localOnly?null:(v=>source=v),
            ConfigureSourceGesture=localOnly?null:(v=>sourceGesture=v),VisualAge=()=>age,
            StageFocus=()=>localOnly?0f:Ease((age+.30f)/.13f)*(1-Ease((age-.10f)/.30f)),
            Light=()=>age<0?(localOnly||!sourceGesture?0f:.10f*Ease((age+.30f)/.13f)):1-Ease((age-.025f)/.12f),
            FloorLight=()=>localOnly?0f:age<0?(sourceGesture?.03f*Ease((age+.30f)/.13f):0f):MathF.Exp(-age/.17f)
        };
        effect.Sample=t=>{
            age=(float)t;body0.Reset();body1.Reset();body2.Reset();seed.Reset();veil.Reset();edges.Reset();motes.Reset();
            if(age<-.31f||age>.44f)return;
            if(localOnly && age<0)return;
            if(age<0)
            {
                float gathering=Ease((age+.30f)/.12f)*(1-Ease((age+.17f)/.05f));
                if(sourceGesture)seed.Anchored(source,new(.60f,.58f),.18f,.16f,-.08f,gathering*.70f,.95f,2);
                float flight=CastTravel(route.Key,age);
                if(flight>0)
                {
                    Vector3 direction=(-source).Normalized();if(direction.LengthSquared()<.1f)direction=Vector3.Right;
                    Vector3 head=source.Lerp(Vector3.Zero,flight);
                    seed.Anchored(head,new(.60f,.58f),.24f+.08f*flight,.18f,MathF.Atan2(direction.Y,direction.X)*.30f,Ease(flight/.12f),1,2);
                    edges.Taper(head-direction*.19f,head,.014f,MathF.Sin(flight*MathF.PI)*.55f,2,1);
                }
                return;
            }
            Vector3 contact=new(.035f,.14f,.025f);
            // Shared original canvas/pivot across phases. Each unequal volume converges
            // independently through the empty contact gap, never a single shrinking plate.
            Layer[] bodies={body0,body1,body2};
            for(int part=0;part<2;part++)
            {
                float dt=(localOnly?age*1.25f:age)-part*.012f;if(dt<0)continue;
                float close=Ease(dt/.20f);
                Vector3 offset=part==0?new(-.10f,.08f,0):new(.08f,-.06f,0);
                Vector3 anchor=contact+offset*(1-close);
                float rx=(part==0?.88f:.83f)*(1-.22f*close)*(localOnly?.78f:1f);
                float ry=(part==0?.78f:.76f)*(1-.28f*close)*(localOnly?.78f:1f);
                float angle=part==0?.05f-.11f*close:-.04f+.13f*close;
                float a0=1-Ease((dt-.055f)/.035f);
                float a1=Ease((dt-.055f)/.035f)*(1-Ease((dt-.145f)/.050f));
                float a2=Ease((dt-.145f)/.050f)*(1-Ease((dt-.25f)/.10f));
                float onset=Ease(dt/.018f);
                float[] alpha={a0,a1,a2};
                for(int phase=0;phase<3;phase++)
                    if(alpha[phase]>.001f)bodies[phase].Anchored(anchor,new(.60f,.58f),rx,ry,angle,alpha[phase]*onset,1,part);
            }
            for(int i=0;i<7;i++)
            {
                float dt=(localOnly?age*1.25f:age)-i*.006f;if(dt<0)continue;
                float a=.2f+i*.89f;Vector3 axis=new(MathF.Cos(a),MathF.Sin(a),0);
                float pull=Ease(dt/.18f),radius=(.32f+(i%3)*.07f)*(1-pull);
                Vector3 point=contact+axis*radius;
                float alpha=MathF.Sin(pull*MathF.PI)*(1-Ease((dt-.13f)/.10f));
                motes.Taper(point+axis*.065f*(1-pull),point,.007f,alpha*.65f,2,.85f);
            }
            // No return or damage/heal pairing: system:heal stays independent.
        };
        return effect;
    }
    private static NativeEffect CreateLight(AttackRoute route)
    {
        var rays=new Layer(0,0);var core=new Layer(0,1);var shoulder=new Layer(0,6);
        var glint=new Layer(0,3,"light-ray-surface-r2");var remnants=new Layer(0,2);
        Vector3 source=new(-1,0,0);float age=0;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-light",
            Batches=new[]{shoulder.Batch,rays.Batch,glint.Batch,core.Batch,remnants.Batch},
            TargetPeak=.105f,FloorPeak=.22f,ConfigureSource=v=>source=v,VisualAge=()=>age,
            StageFocus=()=>Ease((age+.30f)/.13f)*(1-Ease((age-.08f)/.30f)),
            Light=()=>age<0?.14f*Ease((age+.30f)/.13f):1-Ease((age-.045f)/.13f),
            FloorLight=()=>age<0?.04f*Ease((age+.30f)/.13f):MathF.Exp(-age/.15f)
        };
        void Feather(Layer layer,Vector3 start,Vector3 end,float width,float alpha,int role,float gain)
        {
            Vector3 middle=start.Lerp(end,.57f);
            layer.Taper(middle,start,width,alpha*.78f,role,gain);
            layer.Taper(middle,end,width,alpha,role,gain);
        }
        effect.Sample=t=>{
            age=(float)t;rays.Reset();core.Reset();shoulder.Reset();glint.Reset();remnants.Reset();
            if(age<-.31f||age>.42f)return;
            Vector3 direction=(-source).Normalized();if(direction.LengthSquared()<.1f)direction=Vector3.Right;
            Vector3 normal=new(-direction.Y,direction.X,0);
            if(age<0)
            {
                float charge=Ease((age+.30f)/.11f)*(1-Ease((age+.15f)/.045f));
                for(int i=0;i<4;i++)
                {
                    float a=.35f+i*1.65f;Vector3 axis=new(MathF.Cos(a),MathF.Sin(a),0);
                    float close=Ease((age+.30f)/.16f),radius=.30f*(1-close)+.025f;
                    Vector3 head=source+axis*radius;
                    rays.Taper(head+axis*(.16f+i*.025f),head,.014f,charge*(.6f+i*.08f),2,1.1f);
                }
                shoulder.Sprite(source,.18f,.15f,0,charge*.28f,2);
                float flight=CastTravel(route.Key,age);
                if(flight>0)
                {
                    Vector3 head=source.Lerp(Vector3.Zero,flight);
                    float span=Math.Min(source.DistanceTo(head),.42f+.30f*flight);
                    Feather(rays,head-direction*span,head,.045f,Ease(flight/.10f)*.65f,2,1.15f);
                    Feather(core,head-direction*span*.85f,head,.012f,Ease(flight/.10f),3,1.35f);
                    shoulder.Sprite(head,.20f,.10f,MathF.Atan2(direction.Y,direction.X),MathF.Sin(flight*MathF.PI)*.32f,2);
                }
                return;
            }
            float peak=1-Ease((age-.025f)/.065f);
            // An optical connection collapses at contact; no held summoned spear.
            Feather(core,-direction*.42f,direction*.13f,.019f,peak,3,1.35f);
            Feather(rays,-direction*.48f,direction*.20f,.055f,peak*.6f,2,1.15f);
            shoulder.Sprite(new(.17f,.22f,-.02f),.56f,.46f,.35f,(1-Ease((age-.065f)/.14f))*.43f,2,1.1f);
            // Registered brief refraction only; the five-prong bitmap is not the main silhouette.
            float glintAlpha=1-Ease((age-.015f)/.040f);float size=.29f;
            Vector3 anchor=new(0,.035f,.025f),center=anchor-new Vector3((2*.595f-1)*size,(1-2*.57f)*size,0);
            glint.MaterialRegion(center,size,size,0,glintAlpha,new(0,0),new(.5f,1),1.05f);
            // Unequal refracted rays grow outside the receiver, then leave
            // separate optical remnants. No held symmetric star silhouette.
            float[] angles={-.78f,-.10f,.49f,1.19f,2.18f};
            float[] lengths={.48f,.67f,.88f,.76f,.47f};
            for(int i=0;i<5;i++)
            {
                float dt=age-i*.006f;if(dt<0)continue;
                Vector3 axis=new(MathF.Cos(angles[i]),MathF.Sin(angles[i]),0);
                float opening=Ease(dt/.055f),release=Ease((dt-.065f)/.12f);
                Vector3 basePoint=anchor+new Vector3(.015f,.09f,0)+axis*(.035f+release*.19f);
                Vector3 tip=basePoint+axis*(lengths[i]*(.26f+.74f*opening)-release*.22f);
                float alpha=(1-Ease((dt-.065f)/.15f))*(i==2?1f:.76f);
                Feather(rays,basePoint,tip,.033f*(1-.60f*release),alpha,2,1.18f);
                Feather(core,basePoint+axis*.06f,tip,.008f,alpha*(1-Ease((dt-.04f)/.075f)),3,1.35f);
                if(release>0)
                {
                    Vector3 remnant=tip+axis*release*.16f;
                    Feather(remnants,remnant-axis*(.17f*(1-release)+.015f),remnant,.009f,
                        alpha*release,.5f<release?1:2,1.05f);
                }
            }
        };
        return effect;
    }
    private static NativeEffect CreateIceScatter(AttackRoute route)
    {
        // Measured short regions, not repeated whole ice spears or Light fans.
        var crystals=new Layer(0,3,"ice-crystal-surface-r2");
        var fragments=new Layer(0,3,"ice-crystal-surface-r2");
        var cold=new Layer(0,6);var edge=new Layer(0,1);
        Vector3 source=new(-1,0,0);float age=0,seed=0;bool gesture=true;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-ice",
            Batches=new[]{cold.Batch,crystals.Batch,fragments.Batch,edge.Batch},
            TargetPeak=.10f,FloorPeak=.22f,ConfigureSource=v=>source=v,
            ConfigureSourceGesture=v=>gesture=v,ConfigureVisualSeed=v=>seed=v,VisualAge=()=>age,
            StageFocus=()=>Ease((age+.30f)/.14f)*(1-Ease((age-.07f)/.34f)),
            Light=()=>age<0?0:1-Ease((age-.02f)/.12f),
            FloorLight=()=>age<0?0:MathF.Exp(-age/.16f)
        };
        void Shard(Layer layer,Vector3 center,float halfHeight,float angle,float alpha,int part)
        {
            Vector2 uv,span;float aspect;
            if(part==0){uv=new(.74069899f,.13641488f);span=new(.04678692f,.24915445f);aspect=.3755656f;}
            else if(part==1){uv=new(.81679820f,.64036077f);span=new(.04171364f,.05749718f);aspect=1.4509804f;}
            else {uv=new(.89966178f,.32694476f);span=new(.02311161f,.03720406f);aspect=1.2424242f;}
            layer.MaterialRegion(center,halfHeight*aspect,halfHeight,angle,alpha,uv,span,1.02f);
        }
        // Stable captured receiver/volley seed, not frame noise or new event timing.
        float Pick(int index)
        {
            float n=MathF.Sin(seed*2.173f+index*12.9898f)*43758.5453f;
            return n-MathF.Floor(n);
        }
        effect.Sample=t=>{
            age=(float)t;crystals.Reset();fragments.Reset();cold.Reset();edge.Reset();
            if(age<-.32f||age>.60f)return;
            if(age<0)
            {
                float gather=Ease((age+.31f)/.09f)*(1-Ease((age+.20f)/.055f));
                if(gesture)
                {
                    for(int j=0;j<3;j++)
                        Shard(crystals,source+new Vector3((j-1)*.09f,.08f+j*.025f,0),.10f+j*.022f,(j-1)*.43f,gather,0);
                    cold.Sprite(source,.20f,.15f,0,gather*.24f,2);
                }
                float travel=CastTravel(route.Key,age);
                if(travel<=0)return;
                for(int j=0;j<4;j++)
                {
                    float shift=(j-1.5f)*.15f+(Pick(j+20)-.5f)*.13f;
                    Vector3 end=new(shift,.02f+Pick(j+30)*.28f,0);
                    Vector3 start=source+new Vector3(0,j*.025f,0);
                    Vector3 pos=start.Lerp(end,travel)+Vector3.Up*(MathF.Sin(travel*MathF.PI)*(.38f+Pick(j+40)*.38f));
                    float tilt=-.70f+travel*(.6f+Pick(j+50)*.6f)+(j-1.5f)*.14f;
                    Shard(crystals,pos,.16f+Pick(j+60)*.13f,tilt,Ease(travel/.12f),0);
                }
                return;
            }
            // All decorative arrivals share this ONE authoritative contact.
            float compression=1-Ease(age/.065f),release=Ease((age-.018f)/.065f);
            cold.Sprite(new(0,.08f,0),.34f,.24f,0,compression*.31f,2);
            for(int j=0;j<4;j++)
            {
                float side=(j-1.5f)*.15f+(Pick(j+20)-.5f)*.13f;
                float tilt=-.70f+(.6f+Pick(j+50)*.6f)+(j-1.5f)*.14f;
                Shard(crystals,new(side,.02f+Pick(j+30)*.28f,0),.16f+Pick(j+60)*.13f,tilt,compression,0);
            }
            for(int j=0;j<7;j++)
            {
                float u=MathF.Max(0,age-.018f);
                float x=(Pick(j+70)-.5f)*.64f,y=-.04f+Pick(j+80)*.35f;
                float side=x<0?-1:1;
                float vx=side*(.30f+Pick(j+90)*.82f),vy=-.25f+Pick(j+100)*.95f;
                Vector3 pos=new(x+vx*u,y+vy*u-(3.8f+Pick(j+110)*2.4f)*u*u,.012f);
                float alpha=release*(1-Ease((u-(.17f+Pick(j+120)*.12f))/.18f));
                float spin=(Pick(j+130)-.5f)*5.5f+u*side*(2.0f+Pick(j+140)*5.0f);
                Shard(fragments,pos,.053f+Pick(j+150)*.078f,spin,alpha,Pick(j+160)<.55f?1:2);
                if(j<3)edge.Taper(pos+new Vector3(.025f,.035f,0),pos,.009f,alpha*.48f,3);
            }
        };
        return effect;
    }

    private static NativeEffect CreateIce(AttackRoute route)
    {
        var gather=new Layer(0,3,"ice-crystal-surface-r2");
        var intact=new Layer(0,11,"ice-crystal-surface-r2");
        var broken=new Layer(0,12,"ice-crystal-surface-r2");
        var cold=new Layer(0,6);var edges=new Layer(0,1);var chips=new Layer(0,2);
        Vector3 source=new(-1,0,0);float age=0;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-ice",
            Batches=new[]{cold.Batch,gather.Batch,intact.Batch,broken.Batch,edges.Batch,chips.Batch},
            TargetPeak=.115f,FloorPeak=.26f,ConfigureSource=v=>source=v,VisualAge=()=>age,
            StageFocus=()=>Ease((age+.32f)/.14f)*(1-Ease((age-.10f)/.40f)),
            Light=()=>age<0?.16f*Ease((age+.32f)/.14f):1-Ease((age-.035f)/.145f),
            FloorLight=()=>age<0?.07f*Ease((age+.32f)/.14f):MathF.Exp(-age/.19f)
        };
        void Crystal(Layer layer,Vector3 anchor,float size,float angle,float alpha,bool shattered,int part=0)
        {
            Vector2 pivot=new(.54f,.50f);
            Vector3 along=new(MathF.Cos(angle)*size,MathF.Sin(angle)*size,0),across=new(-MathF.Sin(angle)*size,MathF.Cos(angle)*size,0);
            Vector3 center=anchor-along*(2*pivot.X-1)-across*(1-2*pivot.Y);
            layer.MaterialRegion(center,size,size,angle,alpha,new(shattered?.5f:0,0),new(.5f,1),1.07f,part);
        }
        effect.Sample=t=>{
            age=(float)t;gather.Reset();intact.Reset();broken.Reset();cold.Reset();edges.Reset();chips.Reset();
            if(age<-.33f || age>.60f)return;
            if(age<0)
            {
                float charge=Ease((age+.32f)/.12f)*(1-Ease((age+.16f)/.035f));
                Crystal(gather,source,.12f+.08f*charge,.15f,charge,false);
                cold.Sprite(source,.22f,.18f,0,charge*.33f,2);
                float flight=CastTravel(route.Key,age);
                if(flight>0)
                {
                    Vector3 head=source.Lerp(Vector3.Zero,flight);
                    Crystal(gather,head,.15f+.16f*flight,-.08f,Ease(flight/.12f),false);
                    Vector3 dir=(-source).Normalized();
                    edges.Taper(head-dir*.30f,head,.023f,MathF.Sin(flight*MathF.PI)*.70f,2,1.15f);
                }
                return;
            }
            Vector3 origin=new(0,.035f,.025f);
            float grow=Ease(age/.055f),solid=1-Ease((age-.065f)/.075f);
            Crystal(intact,origin,.58f+.27f*grow,0,solid,false);
            cold.Sprite(origin+new Vector3(.10f,.12f,-.02f),.62f,.48f,.12f,(1-Ease((age-.065f)/.17f))*.52f,2,1.08f);
            float shatter=Ease((age-.065f)/.045f)*(1-Ease((age-.23f)/.19f));
            float dt=MathF.Max(0,age-.075f);
            for(int part=0;part<3;part++)
            {
                Vector3 velocity=part==0?new(.90f,.20f,0):part==1?new(1.25f,-.10f,0):new(-.65f,-.30f,0);
                float turn=dt*(part==0?2.5f:part==1?-3.0f:1.8f);
                // Each rigid group spins around its own shard mass, not a
                // shared planted root. Initial registration remains unchanged.
                Vector3 centroid=part==0?new(.306f,.425f,0):part==1?new(.408f,-.170f,0):new(-.442f,-.408f,0);
                Vector3 rotated=new(centroid.X*MathF.Cos(turn)-centroid.Y*MathF.Sin(turn),
                    centroid.X*MathF.Sin(turn)+centroid.Y*MathF.Cos(turn),0);
                Vector3 p=origin+centroid-rotated+velocity*dt+new Vector3(0,-3.5f*dt*dt,part*.004f);
                Crystal(broken,p,.85f,turn,shatter*(part==0?1f:.85f),true,part);
            }
            for(int i=0;i<8;i++)
            {
                float dtChip=age-.055f-(i%3)*.009f;if(dtChip<0)continue;
                float a=-.40f+i*.43f,speed=1.25f+(i%3)*.32f;
                Vector3 p=origin+new Vector3(MathF.Cos(a)*speed*dtChip,MathF.Sin(a)*speed*dtChip-2.8f*dtChip*dtChip,.04f);
                float alpha=1-Ease((dtChip-.15f)/.19f);
                chips.Petal(p,i<3?.040f:.022f,i<3?.018f:.010f,a+dtChip*(i%2==0?2f:-2f),alpha*(i<3?.90f:.50f),i<3?3:2,1.08f);
            }
        };
        return effect;
    }
    private static NativeEffect CreateRollingWater(AttackRoute route)
    {
        var flow=new Layer(0,3,"water-flow-surface-r1");
        var phases=new[]{new Layer(0,3,"hd2d-waterwide-phase-01"),new Layer(0,3,"hd2d-waterwide-phase-02"),new Layer(0,3,"hd2d-waterwide-phase-03")};
        var local=new Layer(0,3,"hd2d-water-impact-01");var drops=new Layer(0,2);var wake=new Layer(1,3,"water-flow-surface-r1");
        Vector3 source=new(-1,0,0);float age=0;bool gesture=true;
        Vector3[] path=System.Array.Empty<Vector3>();float[] times=System.Array.Empty<float>();
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-water",Batches=new[]{wake.Batch,flow.Batch,phases[0].Batch,phases[1].Batch,phases[2].Batch,local.Batch,drops.Batch},
            ConfigureSource=v=>source=v,ConfigureSourceGesture=v=>gesture=v,
            ConfigureResolvedPath=(p,t)=>{path=(Vector3[])p.Clone();times=(float[])t.Clone();},VisualAge=()=>age,
            TargetPeak=.095f,FloorPeak=.26f,
            StageFocus=()=>Ease((age+.30f)/.12f)*(1-Ease((age-.10f)/.32f)),
            Light=()=>age<0?(gesture?.16f*Ease((age+.30f)/.12f):0):1-Ease((age-.055f)/.17f),
            FloorLight=()=>age<0?0:MathF.Exp(-age/.23f)
        };
        effect.Sample=t=>{
            age=(float)t;flow.Reset();local.Reset();drops.Reset();wake.Reset();foreach(var p in phases)p.Reset();
            if(age<-.36f||age>.72f)return;
            if(gesture)
            {
                Vector3 fallback=new(0,-.20f,.02f);
                Vector3 first=path.Length>0?path[0]+Vector3.Up*.025f:fallback,last=path.Length>0?path[^1]+Vector3.Up*.025f:fallback;
                float firstAt=times.Length>0?times[0]:0,lastAt=times.Length>0?times[^1]:0;
                float startAt=MathF.Min(-.30f,firstAt-.20f);
                Vector3 head=first,dir=(first-source).Normalized();
                if(age<firstAt)
                {
                    float flight=Ease((age-startAt)/(firstAt-startAt));
                    head=new Vector3(source.X,first.Y,first.Z).Lerp(first,flight);
                    flow.FluidRibbon(source,first,MathF.Max(0,flight-.70f),flight,.20f+.12f*flight,1f);
                }
                else
                {
                    bool located=false;
                    for(int i=1;i<path.Length;i++)
                    {
                        if(age<=times[i])
                        {
                            float fraction=Clamp((age-times[i-1])/MathF.Max(.001f,times[i]-times[i-1]));
                            head=path[i-1].Lerp(path[i],fraction)+Vector3.Up*.025f;dir=(path[i]-path[i-1]).Normalized();located=true;break;
                        }
                    }
                    if(!located)
                    {
                        dir=path.Length>1?(path[^1]-path[^2]).Normalized():(last-source).Normalized();
                        float dt=MathF.Max(0,age-lastAt);head=last+dir*(dt*1.35f);
                    }
                }
                float angle=MathF.Atan2(dir.Y,dir.X);
                float arrived=Ease((age-firstAt+.12f)/.14f),release=Ease((age-lastAt-.045f)/.24f);
                float visible=Ease((age-startAt)/.07f)*(1-Ease((age-lastAt-.25f)/.20f));
                // One full landscape canvas, with its real basal contact fixed at
                // (.88,.73). The upper lip rolls independently over a low trailing
                // body; no bbox fitting, per-target fan, or airborne jet pivot.
                float journey=Clamp((age-firstAt)/MathF.Max(.16f,lastAt-firstAt));
                float collapse=Ease((age-lastAt-.025f)/.21f);
                float swelling=Ease((age-startAt)/.16f);
                float overturn=Ease((age-firstAt+.025f)/.13f);
                float width=1.25f+.45f*swelling;
                float height=width/1.5f;
                void Body(Layer layer,float opacity,float phase)
                {
                    if(opacity<.001f)return;
                    Vector3 Point(float u,float v)
                    {
                        float upper=Clamp((.73f-v)/.34f);
                        float lip=Ease((u-.48f)/.34f)*upper;
                        float roll=.08f*MathF.Sin(journey*MathF.PI)+.10f*overturn;
                        float x=(u-.88f)*2*width+lip*roll;
                        float y=(.73f-v)*2*height;
                        y*=1-.30f*collapse*upper;
                        y+=upper*.035f*MathF.Sin(u*11-age*16);
                        x+=upper*.025f*MathF.Sin(v*13-age*13);
                        return head+new Vector3(x,y,.035f+phase*.002f);
                    }
                    for(int row=0;row<3;row++)for(int col=0;col<12;col++)
                    {
                        float u=col/12f,U=(col+1)/12f,v=row/3f,V=(row+1)/3f;
                        void Vertex(float x,float y)=>layer.V(Point(x,y),new Vector2(x,y),opacity,1f,3);
                        Vertex(u,v);Vertex(U,v);Vertex(U,V);Vertex(u,v);Vertex(U,V);Vertex(u,V);
                    }
                }
                Body(phases[0],visible*(1-overturn),0);
                Body(phases[1],visible*overturn*(1-collapse),1);
                Body(phases[2],visible*collapse,2);
            }
            if(age<0)return;
            // Each actual positive event adds only a small local rupture. The
            // travelling shared body is never duplicated on these receivers.
            float pulse=1-Ease((age-.025f)/.09f);
            local.Anchored(new(.05f,-.18f,.03f),new(.40f,.56f),.32f,.23f,.04f,pulse*.70f,1f);
            float settle=Ease((age-.04f)/.07f)*(1-Ease((age-.24f)/.22f));
            int n=wake.N;wake.Quad(new(-.65f,.033f,-.28f),new(.85f,.033f,-.28f),new(.85f,.033f,.30f),new(-.65f,.033f,.30f),settle*.24f,3,.85f);
            for(int k=n;k<wake.N;k++)wake.UV[k].Y=1-wake.UV[k].Y;
            for(int i=0;i<6;i++)
            {
                float dt=age-.018f-(i%3)*.014f;if(dt<0)continue;
                Vector3 velocity=new(.35f+(i%3)*.30f,.60f+(i%2)*.42f,0);
                Vector3 pos=new Vector3(-.22f+i*.08f,-.13f,.04f)+velocity*dt+Vector3.Down*(3.8f*dt*dt);
                Vector3 dir=(velocity+Vector3.Down*(7.6f*dt)).Normalized();
                float opacity=Ease(dt/.02f)*(1-Ease((dt-.13f)/.18f));
                drops.Taper(pos-dir*(i<2?.055f:.03f),pos,i<2?.013f:.008f,opacity*(i<2?.8f:.5f),2,1f);
            }
        };
        return effect;
    }

    private static NativeEffect CreateWater(AttackRoute route)
    {
        var flow=new Layer(0,3,"water-flow-surface-r1");
        var gather=new Layer(0,3,"water-flow-surface-r1");
        var mist=new Layer(0,6);
        var foam=new Layer(0,1);
        var drops=new Layer(0,2);
        var phases=new[]{new Layer(0,3,"hd2d-water-impact-01"),new Layer(0,3,"hd2d-water-impact-02"),new Layer(0,3,"hd2d-water-impact-03")};
        Vector3 source=new(-1,0,0);float age=0;
        var effect=new NativeEffect {
            Shader="Hd2dPrototype",Palette="hd2d-water",
            Batches=new[]{mist.Batch,flow.Batch,gather.Batch,phases[0].Batch,phases[1].Batch,phases[2].Batch,foam.Batch,drops.Batch},
            TargetPeak=.105f,FloorPeak=.28f,ConfigureSource=v=>source=v,VisualAge=()=>age,
            StageFocus=()=>Ease((age+.28f)/.12f)*(1-Ease((age-.12f)/.38f)),
            Light=()=>age<0?.22f*Ease((age+.28f)/.12f):MathF.Max(1-Ease((age-.04f)/.15f),.19f*(1-Ease((age-.18f)/.18f))),
            FloorLight=()=>age<0?.10f*Ease((age+.28f)/.13f):MathF.Exp(-age/.23f)
        };
        effect.Sample=t=>{
            age=(float)t;flow.Reset();gather.Reset();mist.Reset();foam.Reset();drops.Reset();foreach(var p in phases)p.Reset();
            if(age<-.29f || age>.52f)return;
            float flight=CastTravel(route.Key,age);
            if(age<.055f)
            {
                float tail=age<0?MathF.Max(0,flight-.70f):Ease(age/.055f);
                flow.FluidRibbon(source,Vector3.Zero,tail,flight,.17f+.08f*flight,1-Ease(age/.055f));
                float gatherAlpha=Ease((age+.28f)/.055f)*(1-Ease((age+.20f)/.055f));
                gather.MaterialSprite(source,.20f,.12f,-.20f,gatherAlpha,.95f);
                if(age<0 && flight>0)
                {
                    Vector3 head=source.Lerp(Vector3.Zero,flight)+Vector3.Up*(MathF.Sin(flight*MathF.PI)*.16f);
                    phases[0].Anchored(head,new(.40f,.56f),.27f+.30f*flight,.22f+.27f*flight,
                        -.10f*(1-flight),Ease(flight/.15f)*.90f,1.0f);
                }
            }
            if(age<0)return;
            float surge=Ease(age/.075f);
            Vector3 origin=new(0,0,.025f);
            Vector2 pivot=new(.40f,.56f);
            // Compressed liquid -> opening foamy fan -> torn sheets and drops.
            // Phase material preserves cyan/blue transmission, never a firecore.
            float compact=1-Ease((age-.035f)/.075f);
            float fan=Ease(age/.045f)*(1-Ease((age-.125f)/.105f));
            float broken=Ease((age-.105f)/.075f)*(1-Ease((age-.25f)/.13f));
            phases[0].Anchored(origin,pivot,.65f+.13f*surge,.57f+.12f*surge,0,compact,1.1f);
            phases[1].Anchored(origin+new Vector3(age*.12f,age*.07f,0),pivot,.80f+.14f*surge,.72f+.10f*surge,0,fan,1.05f);
            float drift=MathF.Max(0,age-.12f);
            for(int piece=0;piece<2;piece++)
            {
                // Registered full-canvas masks overlap irregularly at the neck;
                // neither fragment exposes a rectangular UV crop boundary.
                float gravity=piece==0?1.9f:3.5f;
                Vector3 fall=new(drift*(piece==0?.55f:.95f),-drift*drift*gravity,.008f+piece*.004f);
                float collapse=1-.20f*Ease(drift/.22f);
                phases[2].Anchored(origin+fall,pivot,.92f+drift*.20f,.82f*collapse,
                    drift*(piece==0?.20f:-.35f),broken,1f,piece);
            }
            mist.Sprite(new(.20f,.15f,-.012f),.68f+.30f*surge,.40f+.19f*surge,.15f,(1-Ease((age-.12f)/.20f))*.36f,2,.95f);
            // A few ballistic liquid drops continue the fan's momentum; no
            // extra contact flashes, damage callbacks or sustained spear shape.
            for(int i=0;i<9;i++)
            {
                float born=.035f+(i%3)*.014f,dt=age-born;if(dt<0)continue;
                float a=.18f+i*.24f,speed=1.2f+(i%4)*.30f;
                Vector3 velocity=new(MathF.Cos(a)*speed,MathF.Sin(a)*speed,0);
                Vector3 p=origin+velocity*dt+new Vector3(.06f,-2.4f*dt*dt,.02f);
                float opacity=(1-Ease((dt-.12f)/.18f))*(i<3?.90f:.55f);
                Vector3 direction=(velocity+new Vector3(0,-4.8f*dt,0)).Normalized();
                drops.Taper(p-direction*(i<3?.075f:.035f),p,i<3?.018f:.009f,opacity,2,1.05f);
                if(i<3)foam.Petal(p,.013f,.008f,a,opacity*.70f,3,1.05f);
            }
        };
        return effect;
    }
    private static float FireImpactLight(float age)
    {
        float crest=1-Ease((age-.045f)/.14f);
        // Incandescent released billows keep a lower local reflected-light tail;
        // the broad scene crest still ends at .190 and does not become a long flash.
        float burning=.32f*Ease((age-.10f)/.055f)*(1-Ease((age-.235f)/.135f));
        return MathF.Max(crest,burning);
    }
    private static float Clamp(float x)=>Math.Clamp(x,0,1);
    private static float Ease(float x){x=Clamp(x);return x*x*(3-2*x);}
    private static void Strike(Layer body,Layer edge,Layer sparks,Layer radiance,Layer sheet,Layer sheetTail,Vector3 source,Vector3 weaponOrigin,float weaponTraceLength,bool remote,int weaponFrame,float t,bool sourceGesture=true,bool vegetalMaterial=false,Layer? fibres=null)
    {
        // Weapon-local load and path reveal lead the target response; no held, detached blade image.
        if(t<.055f && weaponFrame>=2)
        {
            float swing=remote?Ease((t+.12f)/.06f):Ease((t+.045f)/.10f),head=2.15f-swing*3.5f;
            Vector3 pivot=source+new Vector3(.02f,.02f,.012f);
            if(remote && t>-.12f && t<0)
            {
                float flight=Ease((t+.12f)/.12f);
                // Travel is a narrow leading edge, NOT an impact crescent on an intervening actor.
                // Keep it behind actor depth; only the selected target gets the contact mass.
                Vector3 start=pivot,end=new(-.18f,0,-.05f);
                Vector3 leading=start.Lerp(end,flight)-new Vector3(0,0,.18f*MathF.Sin(flight*MathF.PI)),direction=(end-start).Normalized();
                edge.Taper(leading-direction*Math.Min(.22f,leading.DistanceTo(start)),leading,.008f,.85f,3,1.25f);
            }
            float alpha=remote?Ease((t+.12f)/.018f)*(1-Ease((t+.065f)/.035f)):Ease((t+.18f)/.08f)*(1-Ease((t+.005f)/.055f));
            if(remote && sourceGesture)
            {
                body.Ribbon(pivot,.33f,.40f,head+1.45f,head,.018f,alpha*.72f,3);
                edge.Ribbon(pivot,.35f,.42f,head+.80f,head,.010f,alpha,3);
            }
            else if(!remote && sourceGesture && weaponOrigin.DistanceTo(source)>.015f)
            {
                float trim=.28f+.62f*Ease((t+.015f)/.065f);
                // Keep a brief near-blade trace, not an extra shaft across the
                // torso. Short staff swings stay intact; long axe/hammer paths trim.
                trim=MathF.Max(trim,1-weaponTraceLength/weaponOrigin.DistanceTo(source));
                Vector3 origin=weaponOrigin+new Vector3(0,0,.018f),tipPoint=source+new Vector3(0,0,.018f);
                body.WeaponTrace(origin,tipPoint,trim,.024f,alpha*.72f,3);
                edge.WeaponTrace(origin,tipPoint,trim,.009f,alpha,3);
            }
        }
        if(t<0)return;
        float hit=MathF.Exp(-t/.060f);
        // A readable primary impulse survives 3–4 frames, then opens into the fine residue.
        // This is the contact mass, not another full-body flash or ring of identical needles.
        float impulse=1-Ease((t-.055f)/.155f),release=Ease(t/.19f);
        // A broad textured cut erupts from the reachable contact, then tears open.
        float cut=1-Ease((t-.055f)/.145f),surge=Ease(t/.065f);
        Vector3 cutCenter=new(.20f+surge*.22f,.22f+surge*.16f,.016f);
        float cutWidth=.85f+surge*.32f;
        if(vegetalMaterial)
            sheet.MaterialSprite(new(.12f,.20f,.016f),.43f,.33f,-.36f,1-Ease((t-.025f)/.055f),1.20f);
        else sheet.MaterialSprite(cutCenter,cutWidth,.65f,-.36f,cut,1.35f);
        // Two parts of the SAME cut peel apart after the hot hit; they do not
        // form another circular emblem above the weapon path.
        for(int piece=0;piece<(vegetalMaterial?0:2);piece++)
        {
            float birth=.075f+piece*.016f,age=MathF.Max(0,t-birth);
            float tail=Ease(age/.035f)*(1-Ease((t-(.17f+piece*.025f))/.14f));
            float u=piece==0?0:.44f,w=piece==0?.72f:.56f;
            float rotation=-.36f+(piece==0?.22f:-.20f)*Ease(age/.18f);
            Vector3 along=new(MathF.Cos(rotation),MathF.Sin(rotation),0);
            Vector3 drift=new(age*(piece==0?.55f:1.15f),age*(piece==0?.38f:-.14f),.008f);
            Vector3 segment=cutCenter+along*((2*(u+w*.5f)-1)*cutWidth)+drift;
            sheetTail.MaterialRegion(segment,cutWidth*w,.65f*(1-.30f*Ease((t-.15f)/.18f)),rotation,tail*(piece==0?.70f:.88f),new(u,0),new(w,1),1.15f,piece+1);
        }
        // The leading edge travels through the contact; a short trailing stroke
        // opens behind it. Never reveal a complete stationary badge around the target.
        float air=(1-Ease((t-.040f)/.14f))*.56f;
        // The colored shoulder shares the diagonal cut instead of an upper C.
        radiance.Sprite(cutCenter-new Vector3(0,0,.007f),cutWidth,.22f,-.36f,air,2,1.05f);
        float localCore=vegetalMaterial?1-Ease((t-.025f)/.055f):1;
        body.Taper(new(-.15f,.14f,.025f),new(.70f,-.12f,.025f),vegetalMaterial?.025f:.070f,impulse*(vegetalMaterial?.32f:.74f)*localCore,3,1.25f);
        edge.Taper(new(-.18f,.14f,.035f),new(.46f,-.05f,.035f),.065f,hit*localCore,3,1.6f);
        edge.Taper(new(-.16f,.21f,.035f),new(.08f,-.17f,.035f),.017f,hit*.8f*localCore,3);
        if(vegetalMaterial && fibres!=null)
        {
            // A compact hot seed tears into separated longitudinal fibres.
            // The late silhouette is no longer one polished green blade edge.
            float peel=Ease((t-.035f)/.15f);
            for(int i=0;i<4;i++)
            {
                float offset=(i-1.5f)*.095f;
                float fade=1-Ease((t-(.095f+i*.012f))/.14f);
                Vector3 start=new(-.10f+t*.22f,.19f+offset*.25f,.038f);
                Vector3 end=new(.70f+(i%2)*.14f+t*(.3f+i*.14f),-.02f+offset*(.3f+2.8f*peel)-t*t*(i%2==0?.8f:1.5f),.038f);
                float width=(.080f-i*.009f)*(1-.55f*peel);
                Vector3 curve=(start+end)*.5f+new Vector3(0,.10f+(i%2==0?.17f:-.10f)*peel,0);
                // The first punch may be ivory; separating material must not
                // retain a white zigzag that outshines its green fibre body.
                int role=t<.035f && i==0?3:1;
                fibres.WeaponTrace(start,end,Ease((t-.075f)/.15f)*.78f,width,fade*(.95f-i*.10f),role,curve);
            }
            for(int i=0;i<5;i++)
            {
                float born=.045f+i*.014f,leafAge=t-born;if(leafAge<0)continue;
                float a=-.4f+i*.47f;
                Vector3 velocity=new(.75f+i*.22f,.35f+(i%3)*.35f,0);
                Vector3 pos=new(.26f+i*.08f,.16f+(i%2)*.10f,.045f);
                pos+=velocity*leafAge+new Vector3(0,-3.8f*leafAge*leafAge,0);
                float fade=Ease(leafAge/.025f)*(1-Ease((leafAge-.12f)/.22f));
                float edgeOn=.10f+.90f*MathF.Abs(MathF.Cos(leafAge*(13+i*2)+i*.8f));
                sparks.Leaf(pos,.055f+(i%3)*.010f,.025f*edgeOn,a+leafAge*(i%2==0?8:-10),fade*.92f);
            }
            return;
        }
        // Authored hierarchy rather than a uniformly spaced white spoke fan.
        // Three decisive chips carry impact; irregular small gold fragments recede.
        float[] angles={-.12f,.58f,1.22f,-.38f,.18f,.92f,1.56f,.40f,1.82f,-.62f,1.04f,.70f,.04f,1.40f};
        for(int i=0;i<14;i++)
        {
            bool dominant=i<3;
            float birth=dominant?i*.012f:.014f+(i%4)*.009f,age=MathF.Max(0,t-birth);if(t<birth)continue;
            float a=angles[i],speed=dominant?3.2f-i*.25f:1.6f+(i%5)*.33f;
            Vector3 direction=new(MathF.Cos(a),MathF.Sin(a),0),p=direction*(.13f+speed*age)+new Vector3(-.10f,0,0)+new Vector3(0,-.65f*age*age,.025f);
            float f=1-Ease(age/(dominant?.23f+i*.025f:.17f+(i%4)*.035f));
            float length=dominant?(.25f-i*.040f)*(.40f+.60f*Ease(age/.045f)):.032f+(i%4)*.013f;
            float width=dominant?.024f-i*.004f:.003f+(i%3)*.001f;
            float opacity=f*(dominant?1-i*.13f:.25f+(i%3)*.08f);
            sparks.Taper(p-direction*length,p,width,opacity,dominant?3:2,dominant?1.25f:.90f);
            if(i==0 || i==7)sparks.Petal(p,.009f,.004f,a,opacity*.65f,dominant?3:2);
        }
    }
    private static void Rake(Layer body,Layer edge,Layer fragments,Layer radiance,Layer sheets,Layer unusedTail,
        Vector3 source,Vector3 weaponOrigin,float traceLength,bool remote,int weaponFrame,float t,bool sourceGesture=true)
    {
        if(t<0){Strike(body,edge,fragments,radiance,sheets,unusedTail,source,weaponOrigin,traceLength,remote,weaponFrame,t,sourceGesture);return;}
        // One contact, three unequal optical tearing paths. These are not
        // additional claws, weapon models, damage events or independent flashes.
        float crest=1-Ease((t-.045f)/.125f), travel=Ease(t/.085f);
        Vector3 axis=new(.90f,-.43f,0),normal=new(.43f,.90f,0);
        // Keep the seed at actual contact; the rake itself crosses the
        // receiving face, not the actor's head or the health-plate strip.
        Vector3 origin=new(.18f,.38f,.025f);
        radiance.Sprite(origin+axis*.26f,.56f,.32f,-.45f,crest*.67f,2,1.12f);
        for(int i=0;i<3;i++)
        {
            float born=i*.009f,age=t-born;if(age<0)continue;
            float strength=i==1?1f:i==0?.76f:.58f;
            float length=i==1?.70f:i==0?.55f:.47f;
            Vector3 offset=normal*((i-1)*.17f);
            Vector3 center=origin+offset+axis*(.35f+travel*.18f);
            float opacity=(1-Ease((age-.05f)/.13f))*strength;
            sheets.MaterialSprite(center,length,.22f,-.45f,opacity,1.22f);
            // Hot leading edges move together, then lose white before the
            // curved amber remnants break away along the same shared stroke.
            Vector3 head=origin+offset+axis*(.40f+travel*(length*.70f));
            edge.Taper(head-axis*.27f,head,.022f,opacity*(1-Ease(age/.12f)),3,1.35f);
            float released=Ease((age-.065f)/.055f)*(1-Ease((age-.16f)/.15f));
            Vector3 exit=head+axis*MathF.Max(0,age-.08f)*1.1f+normal*((i-1)*MathF.Max(0,age-.08f)*.35f);
            fragments.Taper(exit-axis*(.27f-.12f*Ease(age/.28f)),exit,.012f,released*strength*.75f,2,1.0f);
            if(i==1)body.Taper(new(0,0,.03f),origin+axis*.12f,.055f,MathF.Exp(-age/.045f),3,1.4f);
        }
    }
    private static void Blunt(Layer body,Layer edge,Layer chips,Layer dust,Layer radiance,Layer pressure,Layer unused,
        Vector3 source,Vector3 weaponOrigin,float traceLength,bool remote,int weaponFrame,float t,bool sourceGesture=true)
    {
        // Shared actual-weapon causality, not an illustrated summoned fist/hammer.
        if(t<0){Strike(body,edge,chips,radiance,pressure,unused,source,weaponOrigin,traceLength,remote,weaponFrame,t,sourceGesture);return;}
        float peak=1-Ease((t-.055f)/.125f),expand=Ease(t/.055f);
        // A compressed asymmetric optical fan pushes INTO the receiving surface.
        // Its short footprint deliberately differs from slash's long peeling sheet.
        pressure.Anchored(new(.015f,0,.025f),new(.50f,.78f),.48f+.22f*expand,.34f+.14f*expand,-.60f,peak,1.25f);
        radiance.Sprite(new(.20f,.18f,.008f),.55f,.35f,.55f,peak*.65f,2,1.05f);
        body.Taper(new(-.04f,-.02f,.035f),new(.48f,.34f,.035f),.13f,peak,3,1.35f);
        body.Taper(new(-.02f,.02f,.032f),new(.16f,.45f,.032f),.09f,peak*.72f,3,1.20f);
        edge.Taper(new(-.06f,.01f,.04f),new(.28f,.17f,.04f),.065f,MathF.Exp(-t/.045f),3,1.5f);
        float[] angle={.22f,.87f,1.48f,-.72f,.57f,1.72f,-.12f};
        for(int i=0;i<angle.Length;i++)
        {
            float birth=i<3?.035f+i*.012f:i*.006f;
            float age=MathF.Max(0,t-birth);if(t<birth)continue;
            Vector3 dir=new(MathF.Cos(angle[i]),MathF.Sin(angle[i]),0);
            Vector3 tip=new Vector3(.07f,0,.035f)+dir*(.07f+age*(i<3?2.3f:1.3f))+new Vector3(0,-age*age*.65f,0);
            float fade=1-Ease(age/(i<3?.22f:.16f));
            chips.Taper(tip-dir*(i<3?.23f-i*.04f:.045f)*(1-Ease(age/.26f)),tip,i<3?.035f-i*.006f:.008f,fade*(i<3?.9f:.4f),i<3?3:2,1.1f);
        }
        float air=Ease((t-.10f)/.07f)*(1-Ease((t-.12f)/.23f));
        dust.Dust(new(.15f+t*.35f,-.08f+t*.12f,-.015f),.18f+t*.3f,.09f+t*.15f,air*.16f);
    }
    private static void Thrust(Layer body,Layer edge,Layer fragments,Layer dust,Layer radiance,Layer unused,Layer unusedTail,
        Vector3 source,Vector3 weaponOrigin,float traceLength,bool remote,int weaponFrame,float t,bool sourceGesture=true,bool emberMaterial=false)
    {
        if(t<0)
        {
            if(weaponFrame>=2 && (sourceGesture || remote) && t>=(remote?-.12f:-.045f))
            {
                Vector3 dir=(-source).Normalized();if(dir.LengthSquared()<.1f)dir=Vector3.Right;
                float flight=Ease((t+(remote?.12f:.045f))/(remote?.12f:.045f));
                Vector3 head=remote?source.Lerp(Vector3.Zero,flight):source+dir*(.055f*flight);
                head+=new Vector3(0,0,remote?-.18f*MathF.Sin(flight*MathF.PI):.025f);
                float span=remote?Math.Min(.28f,head.DistanceTo(source)+.025f):.27f;
                body.Taper(head-dir*span,head,.019f,flight*.85f,3,1.20f);
                edge.Taper(head-dir*span*.8f,head,.006f,flight,3,1.35f);
            }
            return;
        }
        // Optical penetration axis, never a held summoned spear. A narrow hot
        // impulse advances from the actual contact, then retracts and breaks.
        float drive=Ease(t/.055f),cool=Ease((t-.045f)/.135f);
        float peak=1-Ease((t-.035f)/.10f),tail=1-Ease((t-.09f)/.17f);
        Vector3 axis=new Vector3(1,.30f,0).Normalized();
        Vector3 origin=new(-.06f,0,.035f),tip=origin+axis*(.36f+.59f*drive);
        Vector3 back=origin+axis*(.81f*Ease((t-.065f)/.15f));
        radiance.Stroke(origin-axis*.10f,tip,.13f*(1-.45f*cool),peak*.65f,2,1.10f);
        if(back.X<tip.X)
        {
            body.Taper(back,tip,.055f*(1-.72f*cool),tail*(1-cool),3,1.35f);
            body.Taper(back,tip,.055f*(1-.72f*cool),tail*cool*.65f,2,1.05f);
            edge.Taper(origin,tip-axis*.025f,.016f,peak,3,1.55f);
        }
        // Focused unequal pressure cracks, not a circular starburst.
        edge.Taper(new(.025f,-.12f,.04f),new(.055f,.17f,.04f),.028f,peak*.9f,3,1.3f);
        body.Taper(new(-.035f,.015f,.03f),new(.25f,.17f,.03f),.045f,peak*.75f,3,1.2f);
        for(int i=0;i<5;i++)
        {
            float age=MathF.Max(0,t-.025f-i*.008f);if(t<.025f+i*.008f)continue;
            Vector3 dir=new Vector3(1,i%2==0?.28f+i*.09f:-.24f-i*.055f,0).Normalized();
            Vector3 end=new Vector3(.07f,0,.035f)+dir*(.10f+age*(1.8f-i*.12f));
            float alpha=(1-Ease(age/.21f))*(i<2?.80f:.36f);
                        if(emberMaterial)end+=new Vector3(0,-age*age*2.3f,0);
            fragments.Taper(end-dir*(.15f-i*.018f)*(1-Ease(age/.24f)),end,i<2?.014f:.006f,alpha,emberMaterial&&age>.055f?2:i<2?3:2,1.1f);
        }
        if(emberMaterial)
        {
            float ember=Ease(t/.035f)*(1-Ease((t-.08f)/.18f));
            // One eroding residual lobe behind the physical axis, not a fireball.
            // UV boundaries are organically masked in mode23, never hard-cropped.
            unusedTail.MaterialRegion(new(.17f+t*.32f,.12f-t*t*.70f,.005f),
                .28f+t*.12f,.19f*(1-Ease(t/.35f)*.35f),-.15f-t*.40f,ember*.72f,
                new(0,.30f),new(.60f,.51f),.80f);
        }
        float air=Ease((t-.08f)/.05f)*(1-Ease((t-.14f)/.16f));
        dust.Dust(new(.22f+t*.28f,0,-.01f),.12f+t*.20f,.055f,air*.12f);
    }
    private static void Fire(Layer projectile,Layer flight2,Layer flight3,Layer[] phases,Layer core,Layer embers,Vector3 source,float t)
    {
        Vector3 dir=(-source).Normalized();if(dir.LengthSquared()<.1f)dir=Vector3.Right;
        float angle=MathF.Atan2(dir.Y,dir.X),travel=FireTravel(t);
        Vector3 head=t<0?source.Lerp(Vector3.Zero,travel):Vector3.Zero;
        if(t<-.17f)
        {
            float gather=Ease((t+.28f)/.11f);
            core.Petal(source,.025f+.055f*gather,.022f+.035f*gather,angle,gather,3,1.1f);
            for(int i=0;i<8;i++)
            {
                float a=i*.7854f+t*4f,dist=(1-gather)*.17f+.03f;
                Vector3 p=source+new Vector3(MathF.Cos(a)*dist,MathF.Sin(a)*dist,0);
                embers.Stroke(p,p-dir*.025f,.003f,gather*.7f,2);
            }
            return;
        }
        if(t<0)
        {
            float separated=Ease(travel/.32f);
            float elongation=.12f+(.33f+.10f*Ease(travel)-.12f)*separated;
            float height=.12f+(.24f+.055f*MathF.Sin(travel*MathF.PI)-.12f)*separated;
            Vector3 along=new(MathF.Cos(angle),MathF.Sin(angle),0),up=new(-MathF.Sin(angle),MathF.Cos(angle),0);
            Vector3 flightCenter=head-along*(elongation*.72f)+up*(height*.06f);
            float second=Ease((t+.115f)/.015f),third=Ease((t+.060f)/.015f);
            projectile.MaterialSprite(flightCenter,elongation,height,angle,1-second,1f);
            flight2.MaterialSprite(flightCenter,elongation,height,angle,second*(1-third),1f);
            flight3.MaterialSprite(flightCenter,elongation,height,angle,third,1f);
            for(int i=0;i<9;i++)
            {float u=i/9f;Vector3 p=head-dir*(.10f+u*.34f)+new Vector3(0,MathF.Sin(i*2.1f+t*48)*.025f,0);embers.Stroke(p,p-dir*.035f,.003f,(1-u)*.7f,2);}
            return;
        }
        // Four registered silhouettes: compress, rupture, open core, separated wisps.
        float[] starts={0,.035f,.115f,.27f},ends={.05f,.135f,.295f,.47f};
        // Low-frequency colored radiance is distinct from the detailed hot sheets.
        // It expands briefly and dies before the open tongues, not an ambient fog.
        float thermal=(1-Ease((t-.055f)/.14f));
        core.Dust(new(.08f,.24f,-.018f),.64f+.30f*Ease(t/.09f),.48f+.26f*Ease(t/.09f),thermal*.33f);
        float bloom=Ease(t/.095f);
        float rx=.83f+bloom*.26f,ry=.70f+bloom*.23f;
        Vector3 center=new(-.24f*rx,.22f*ry,0);
        for(int i=0;i<4;i++)
        {
            float onset=i==0?1:Ease((t-starts[i])/.018f),release=1-Ease((t-(ends[i]-.025f))/.025f);
            // The opened flame releases by region, not a shared one-frame phase cutoff.
            float a=onset*(i==2?1:release)*(i==3?.72f:1);
            float nodeScale=1f;
            if(a>0)
            {
                if(i<2)
                {
                    phases[i].MaterialSprite(center*nodeScale,rx*nodeScale,ry*nodeScale,0,a,1.15f);

                }
                else FireReleasedTongues(phases[i],center,rx,ry,t,a,i==2);
            }
        }
        for(int i=0;i<18;i++)
        {
            float birth=(i%5)*.012f,age=t-birth;if(age<0)continue;
            float a=-.9f+i*.115f,speed=1.05f+(i%4)*.25f;
            Vector3 v=new(MathF.Cos(a),MathF.Sin(a),0),p=v*(.035f+speed*age)+new Vector3(0,.18f*age,.025f);
            float f=1-Ease(age/(.26f+(i%4)*.05f));
            embers.Stroke(p-v*.035f,p,.003f,f,2);
            core.Petal(p,.008f,.004f,a,f,i%4==0?3:2);
        }
    }
    private static void FireReleasedTongues(Layer layer,Vector3 center,float rx,float ry,float t,float alpha,bool staggerRelease)
    {
        // New turbulent volume has three uneven lobes around an opened center.
        // Overlapping UV regions use soft irregular partitions, not rectangle cuts.
        float age=MathF.Max(0,t-.115f);
        Vector2[] origins={new(0,0),new(.55f,0),new(.55f,.61f)};
        Vector2[] sizes={new(.67f,1),new(.45f,.73f),new(.45f,.39f)};
        for(int part=0;part<3;part++)
        {
            Vector2 uv=origins[part]+sizes[part]*.5f;
            Vector3 offset=part switch {0=>new(-age*.34f,age*.40f,0),1=>new(age*.22f,age*.70f,0),_=>new(age*.30f,age*.20f,0)};
            float rotation=part switch {0=>age*.32f,1=>-age*.24f,_=>-age*.40f};
            Vector3 position=center+new Vector3((uv.X*2-1)*rx,(1-uv.Y*2)*ry,0)+offset;
            float fade=staggerRelease?Ease((t-(part==0?.215f:part==1?.245f:.195f))/.09f):0;
            float contract=1-.32f*fade;
            layer.MaterialRegion(position,rx*sizes[part].X*contract,ry*sizes[part].Y*contract,rotation,alpha*(1-fade),origins[part],sizes[part],1.03f*(1-.30f*fade),part);
        }
    }

    private static void Stomp(Layer ground,Layer[] phases,Layer edge,Layer rocks,Layer dust,Layer foot,Layer pressureAir,float viewportTop,float t)
    {
        Layer impact=phases[0],shadow=phases[1];
        // No literal summoned body: looming ground mass compresses, then ruptures.
        float reveal=Ease((t+.46f)/.17f),approach=Ease((t+.34f)/.34f);
        float compress=Ease((t+.12f)/.12f),release=Ease(t/.14f);
        float shadowAlpha=reveal*(1-release)*.68f;
        // Low camera pitch foreshortens a short ground footprint into the HP
        // line. Extend real ground depth, not a floating billboard or opacity.
        float shadowWidth=.72f+.64f*approach-.16f*compress;
        float shadowDepth=.90f+.95f*approach-.32f*compress;
        float shadowForward=.28f+.18f*approach;
        shadow.Quad(new(-shadowWidth,.004f,shadowForward-shadowDepth),new(shadowWidth,.004f,shadowForward-shadowDepth),
            new(shadowWidth,.004f,shadowForward+shadowDepth),new(-shadowWidth,.004f,shadowForward+shadowDepth),shadowAlpha,0,1);
        // A few incomplete light fragments imply descending pressure, never a
        // filled ghost foot. They converge into the same pressure origin and die.
        float auraOn=Ease((t+.18f)/.065f)*(1-Ease((t+.005f)/.065f));
        float descent=Clamp((t+.18f)/.18f),lift=.42f*(1-descent*descent);
        float auraWidth=1.05f-.16f*descent;
        foot.Anchored(new(0,lift+.035f,.025f),new(.50f,.83f),auraWidth,
            auraWidth,-.035f*(1-descent),auraOn*.90f,1.05f);
        if(t<0)return;
        float spread=Ease(t/.12f),fracture=1-Ease((t-.26f)/.47f);
        // A single authored pressure fan crosses the FRONT of the sole/stone
        // base during compression, then tears away before the opaque dust phase.
        float flash=1-Ease((t-.035f)/.115f),airSpread=Ease(t/.075f);
        pressureAir.Anchored(new(0,.025f,.10f),new(.50f,.78f),.85f+airSpread*.35f,.52f+airSpread*.18f,0,flash,1.20f);
        // A broken pressure front crosses the ground, then gives way to solid
        // fragments and soft air. It is presentation only: no extra impact events.
        float pressure=Ease(t/.025f)*(1-Ease((t-.045f)/.13f));
        float radius=.43f+Ease(t/.16f)*.85f;
        for(int k=0;k<28;k++)
        {
            if(k==4 || k==12 || k==21)continue;
            float a=k*MathF.Tau/28,b=(k+.84f)*MathF.Tau/28;
            Vector3 p=new(MathF.Cos(a)*radius,.006f,MathF.Sin(a)*radius*.52f);
            Vector3 q=new(MathF.Cos(b)*radius,.006f,MathF.Sin(b)*radius*.52f);
            edge.Stroke(p,q,.055f,pressure*.82f,3,1.25f,true);
            ground.Stroke(p,q,.11f,pressure*.27f,2,1,true);
        }
        for(int i=0;i<10;i++)
        {
            float a=i*2.39996f,reach=(.80f+(i%3)*.15f)*spread;
            Vector3 p=new(MathF.Cos(a)*.045f,0,MathF.Sin(a)*.035f),bend=new(MathF.Cos(a+.13f)*reach*.55f,0,MathF.Sin(a+.13f)*reach*.36f),end=new(MathF.Cos(a)*reach,0,MathF.Sin(a)*reach*.65f);
            ground.Stroke(p,bend,.017f,fracture*.9f,0,1,true);ground.Stroke(bend,end,.010f,fracture*.8f,0,1,true);
            edge.Stroke(p,bend,.0035f,MathF.Exp(-t/.13f),2,.8f,true);
            // A few broken soil lips remain after illumination, not a luminous radial web.
            if(i%3==0)ground.Stroke(bend,end,.006f,fracture*.55f,1,.65f,true);
        }
        float blast=Ease(t/.055f),baseAlpha=1-Ease((t-.10f)/.17f),height=.46f+.32f*blast;
        impact.MaterialSprite(new(0,height*.58f,0),.70f+.42f*blast,height,0,baseAlpha,1.12f);
        for(int i=0;i<10;i++)
        {
            float birth=i<4?.012f:.050f,age=t-birth;if(age<0)continue;
            float side=i%2==0?-1:1,speed=.90f+(i%4)*.18f;
            float y=MathF.Max(.01f,(2.8f+(i%3)*.35f)*age-4.8f*age*age);
            Vector3 p=new(side*(.34f+speed*age),y+.025f,.018f);float size=i<4?.10f+(i%2)*.025f:.045f+(i%3)*.010f;
            rocks.MaterialSprite(p,size,size*.82f,i*1.8f+side*age*5f,1-Ease((age-.32f)/.22f),1f,1);
        }
        // Settled chips sit outside the sole/HUD footprint and outlive the moving mass.
        // They share the existing cosmetic tail; no extra card lock or persistent node.
        float settled=Ease((t-.15f)/.15f)*(1-Ease((t-.52f)/.22f));
        for(int i=0;i<4;i++)
        {
            float side=i%2==0?-1:1,x=side*(.54f+(i/2)*.11f),size=.040f+(i%2)*.007f;
            rocks.MaterialSprite(new(x,size*.55f,.015f),size,size*.60f,i*1.7f,settled*.90f,.78f);
        }
        // Rear and front air have distinct depth, timing and density. The low
        // porous front veil catches the impact pulse without hiding the toe or HUD.
        float rearSmoke=Ease((t-.015f)/.08f)*(1-Ease((t-.27f)/.37f));
        float frontSmoke=Ease((t-.060f)/.09f)*(1-Ease((t-.24f)/.26f));
        for(int side=-1;side<=1;side+=2)
        {
            float disperse=Ease((t-.075f)/.22f);
            float rx=.44f+t*.25f,ry=.24f+.20f*disperse;
            dust.MaterialRegion(new(side*(.38f+t*.24f),ry*.76f,-.045f),rx,ry,
                side*t*.12f,rearSmoke*.57f,new(side<0?0:.5f,0),new(.5f,1),.72f);
            float frontWidth=side<0?.38f:.29f,frontHeight=.22f+.15f*disperse;
            // Lift actual dense powder, not just transparent texture padding.
            // Only the near-side lobe rises; the other front/rear lobes keep grounding.
            float loft=side<0?Ease((t-.09f)/.17f):0;
            phases[3].MaterialRegion(new(side*(.20f+t*.10f)-loft*.045f,frontHeight*.76f+loft*.09f,.045f),
                frontWidth+t*.075f,frontHeight,-side*t*.18f,
                frontSmoke*(side<0?.40f:.30f),new(side<0?0:.5f,0),new(.5f,1),
                .78f+.30f*MathF.Exp(-t/.13f),side<0?2:0);
        }
    }
}
