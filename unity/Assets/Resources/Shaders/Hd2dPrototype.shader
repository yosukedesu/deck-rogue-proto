// Hd2dPrototype.shader — dot の Godot 版 shaders/authored_Hd2dPrototype.gdshader (checkpoint 192) を URP の HLSL に写したもの (2026-10-27)。
// 変えた所: 頂点の色は COLOR (役割の色・線形) × TEXCOORD1.x (明るさ) で HDR を保つ (Unity の頂点色は 8bit なので明るさを別の口で渡す)。
// Godot の blend_mix = SrcAlpha/OneMinusSrcAlpha、unshaded、cull_disabled、depth_draw_never。OUTPUT_IS_SRGB の分岐は線形の URP では不要。
// _Kind・_TextureMotion・_Age などの番号と式は Godot と同じ値 (Hd2dAttackPrototype が組む層の意味に対応)。
Shader "DeckRogue/Hd2dPrototype" {
 Properties {
  _BaseMap("Authored RGBA / material",2D)="white"{}
  _Kind("Layer kind",Float)=0
  _TextureEnabled("Texture enabled",Float)=0
  _AuthoredRgba("Authored RGBA (per layer)",Float)=0
  _UseTexture("Use texture",Float)=0
  _TextureMotion("Texture motion id",Float)=0
  _Age("Visual age",Float)=0
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass {
   Name "Hd2dPrototypeForward" Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float _Kind,_TextureEnabled,_AuthoredRgba,_UseTexture,_TextureMotion,_Age;
   CBUFFER_END
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float2 gain:TEXCOORD1;float4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 tint:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.tint=float4(i.color.rgb*i.gain.x,i.color.a);return o;}
   float hash2(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise2(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3.0-2.0*f);return lerp(lerp(hash2(i),hash2(i+float2(1,0)),f.x),lerp(hash2(i+float2(0,1)),hash2(i+float2(1,1)),f.x),f.y);}
   bool Motion(float lo,float hi){return _TextureMotion>lo && _TextureMotion<hi;}
   bool Kind(float lo,float hi){return _Kind>lo && _Kind<hi;}
   float4 Tex(float2 uv){return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv);}
   float4 frag(V i):SV_Target {
    float4 tint=i.tint;
    float2 encoded_uv=i.uv;
    float released_region=0.0;
    if(_TextureEnabled>.5 && (Motion(26.5,27.5)||Motion(24.5,25.5)||Motion(21.5,22.5)||Motion(19.5,20.5)||Motion(15.5,16.5)||Motion(12.5,13.5)||Motion(7.5,8.5)||Motion(3.5,4.5)||Motion(9.5,10.5))){
     released_region=floor(encoded_uv.x/2.0);
     encoded_uv.x-=released_region*2.0;
    }
    bool dust=encoded_uv.x>1.5;
    float2 uv=dust?encoded_uv-float2(2.0,0.0):encoded_uv;
    float edge=min(min(uv.x,1.0-uv.x),min(uv.y,1.0-uv.y));
    float soft=smoothstep(0.0,_Kind>1.5?.10:.035,edge);
    if(dust) soft=1.0-smoothstep(.15,.50,length(uv-float2(.5,.5)));
    float3 rgb=tint.rgb; float alpha=tint.a*soft;
    bool textured=_TextureEnabled>.5;
    if(!textured && Kind(13.5,14.5)){
     float radius=length((uv-float2(.5,.5))*2.0);
     alpha=tint.a*exp(-radius*radius*5.5)*(1.0-smoothstep(.65,1.0,radius));
    }
    // Ground-only looming shadow: broad unequal lobes, no literal paw drawing.
    if(!textured && Kind(7.5,8.5)){
     float2 q=uv*2.0-1.0;
     float body=1.0-smoothstep(.48,1.0,length(q*float2(1.05,1.15)));
     float lobe0=1.0-smoothstep(.25,.63,length((q-float2(-.48,-.10))*float2(1.0,1.6)));
     float lobe1=1.0-smoothstep(.18,.49,length((q-float2(-.36,.36))*float2(1.2,1.0)));
     float lobe2=1.0-smoothstep(.20,.53,length((q-float2(.25,-.33))*float2(1.4,1.0)));
     float shape=max(body,max(lobe0,max(lobe1,lobe2)));
     float rupture=smoothstep(0.0,.12,_Age);
     float cracks=noise2(q*float2(9.0,13.0)+float2(_Age*2.0,0));
     alpha=tint.a*shape*(.82+.18*noise2(q*17.0))*smoothstep(rupture-.18,rupture+.18,cracks);
     rgb=float3(.014,.018,.024);
    }
    // Untextured primary ribbons have a luminous shoulder, not flat polygon fill.
    if(!textured && !dust && _Kind<.5)
     alpha*=smoothstep(0.0,.35,min(uv.y,1.0-uv.y));
    // Strike-only optical bands. Geometry retains its weapon/contact path;
    // the broad layer is warm transparent air, not another opaque crescent.
    if(!textured && !dust && Kind(5.5,6.5)){
     float across=abs(uv.y-.5)*2.0;
     alpha=tint.a*exp(-across*across*5.0)*smoothstep(0.0,.08,edge);
    }
    if(!textured && !dust && Kind(6.5,7.5)){
     float across=abs(uv.y-.5)*2.0;
     float hot=1.0-smoothstep(.08,.48,across);
     float shoulder=exp(-across*across*3.8);
     rgb=lerp(float3(.78,.32,.065),tint.rgb,hot);
     alpha=tint.a*(.36*shoulder+.64*hot)*smoothstep(0.0,.045,edge);
    }
    if(!textured && Kind(12.5,13.5)){
     // Receiver-local frosted defensive facets, not an offensive flash.
     float grain=.60+.40*noise2(uv*float2(43.0,31.0));
     float plane=.82+.22*step(uv.x,uv.y*.68+.16);
     rgb=tint.rgb*plane;
     alpha=tint.a*grain*smoothstep(0.0,.035,edge);
    }
    if(!textured && Kind(19.5,20.5)){
     float across=abs(uv.y-.5)*2.0;
     float grain=noise2(uv*float2(33.0,7.0)+float2(_Age*1.8,0));
     float erode=smoothstep(.075,.28,_Age)*.58;
     alpha*=smoothstep(erode-.16,erode+.16,grain)*(1.0-smoothstep(.30,1.0,across));
    }
    if(!textured && Kind(20.5,21.5)){
     float across=(uv.y-.5)*2.0;
     alpha=tint.a*exp(-across*across*7.0)*smoothstep(0.0,.20,uv.x)*(1.0-smoothstep(.78,1.0,uv.x));
    }
    if(!textured && Kind(21.5,22.5)){
     float2 q=(uv-.5)*2.0;
     alpha=tint.a*exp(-dot(q,q)*3.4)*(1.0-smoothstep(.65,1.0,length(q)));
    }
    if(textured && _UseTexture>.5){
     float2 sample_uv=uv;
     if(Motion(24.5,25.5)){
      float move=smoothstep(0.0,.24,_Age);
      sample_uv+=float2(.009*sin(uv.y*19.0+_Age*17.0),.008*sin(uv.x*23.0-_Age*13.0))*move;
     }
     if(Motion(.5,1.5)){
      float tail=1.0-smoothstep(.45,.88,uv.x);
      sample_uv.y+=tail*(.025*sin(uv.x*21.0+_Age*95.0)+.025*(noise2(uv*12.0+float2(_Age*14.0,0))-.5));
     }else if(Motion(1.5,2.5)){
      sample_uv+=float2(.023*sin(uv.y*19.0-_Age*40.0),_Age*.08);
     }
     if(Motion(3.5,4.5)){
      sample_uv+=float2(.018*sin(uv.y*16.0+_Age*10.0),.015*sin(uv.x*20.0-_Age*12.0));
     }
     if(Motion(7.5,8.5)){
      float release=smoothstep(.115,.24,_Age);
      // Differential flow bends the dominant curl, instead of sliding a rigid decal.
      float upper=1.0-smoothstep(.18,.65,uv.y);
      sample_uv.x+=release*.035*upper*sin(uv.y*11.0-(_Age-.115)*18.0);
      sample_uv.y+=release*.015*sin(uv.x*15.0+_Age*16.0);
     }
     if(Motion(11.5,13.5)){
      float peripheral=clamp(length(uv-float2(.62,.61))*1.5,0.0,1.0);
      sample_uv+=peripheral*float2(.012*sin(uv.y*17.0-_Age*24.0),.010*sin(uv.x*21.0+_Age*19.0));
     }
     if(Motion(17.5,20.5)){
      // Wet folds advect gently; unlike fire, liquid keeps a cool
      // translucent interior and bright specular crests from the RGBA art.
      sample_uv+=float2(.008*sin(uv.y*19.0-_Age*21.0),.009*sin(uv.x*23.0-_Age*17.0));
     }
     if(Motion(29.5,30.5)){
      // Full registered wave art: only fine wet advection, never a
      // luminance recolor or a crop that turns the body into a thin seam.
      float crest=1.0-smoothstep(.60,.79,uv.y);
      sample_uv+=crest*float2(.003*sin(uv.y*25.0-_Age*16.0),.003*sin(uv.x*29.0-_Age*14.0));
     }
     float release_mask=1.0;
     float4 material=Tex(sample_uv);
     if(Motion(2.5,3.5)){
      float erosion=smoothstep(.12,.32,_Age);
      material.a*=smoothstep(erosion-.18,erosion+.18,noise2(uv*22.0));
     }
     if(Motion(4.5,5.5)){
      float erase=smoothstep(.015,.12,_Age);
      material.a*=smoothstep(erase-.12,erase+.12,uv.x);
     }
     if(Motion(1.5,2.5)){
      float breakup=noise2(uv*17.0+float2(0,-_Age*3.0));
      float erosion=smoothstep(.055,.34,_Age)*.88;
      material.a*=smoothstep(erosion-.12,erosion+.12,breakup);
     }
     if(Motion(5.5,6.5)){
      float dissolve=smoothstep(.20,.40,_Age);
      material.a*=smoothstep(dissolve-.16,dissolve+.16,noise2(uv*18.0));
      material.a*=smoothstep(.07,.29,uv.y);
      material.rgb*=float3(.80,.87,.96);
     }
     if(Motion(6.5,8.5)){
      float erode=smoothstep(.27,.44,_Age)*.55;
      material.a*=smoothstep(erode-.16,erode+.16,noise2(uv*19.0+float2(0,-_Age*2.0)));
     }
     if(Motion(7.5,8.5)){
      float tear=smoothstep(.155,.215,_Age);
      // Overlapping source regions share a ragged sloping neck partition.
      // Its burn-through lies inside BOTH quads, never on a rectangle edge.
      float boundary=.32+.42*(uv.x-.90)+.014*sin(uv.x*53.0)
       +.025*(noise2(uv*float2(35.0,19.0))-.5);
      float gap=.041*tear;
      if(released_region>.5 && released_region<1.5)
       release_mask*=1.0-smoothstep(boundary-gap-.012,boundary-gap+.012,uv.y);
      if(released_region>1.5 && released_region<2.5){
       release_mask*=smoothstep(boundary+gap-.012,boundary+gap+.012,uv.y);
       float burn=smoothstep(.205,.35,_Age)*.48;
       release_mask*=smoothstep(burn-.15,burn+.15,noise2(uv*18.0+float2(0,-_Age*2.5)));
      }
      material.a*=release_mask;
     }
     if(Motion(8.5,9.5)){
      // Upper armor receives less near-ground flash; compress its painted
      // bright trim smoothly, retaining the original material/color identity.
      float upper=1.0-smoothstep(.25,.85,uv.y);
      float highlight=smoothstep(.25,.75,max(max(material.r,material.g),material.b));
      material.rgb*=1.0-.22*upper*highlight;
     }
     if(Motion(3.5,4.5) && released_region>1.5){
      float dispersion=smoothstep(.14,.42,_Age)*.65;
      material.a*=smoothstep(dispersion-.20,dispersion+.20,
       noise2(uv*14.0+float2(-_Age*1.5,_Age*2.0)));
     }
     if(Motion(9.5,10.5) && released_region>.5){
      // Airborne debris leaves the warm contact illumination. Preserve its
      // facets while the painted golden highlights cool; grounded chips keep
      // their separate, already-muted material and visibility.
      float cooling=smoothstep(.16,.38,_Age);
      float value=dot(material.rgb,float3(.25,.60,.15));
      float3 neutral=float3(value,value,value)*float3(.93,.98,1.05);
      material.rgb=lerp(material.rgb,neutral,cooling*.55)*lerp(1.0,.90,cooling);
     }
     // Compatibility renderer has no bloom pass: author a restrained, local radiance fringe.
     // Preserve the real RGBA core; this is not a replacement silhouette or a screen glow.
     bool fire_material=Motion(11.5,13.5)||Motion(.5,1.5)||Motion(6.5,8.5);
     if(fire_material){
      float2 d=float2(.009,0.0);
      float halo=(Tex(sample_uv+d).a+Tex(sample_uv-d).a+
       Tex(sample_uv+d.yx).a+Tex(sample_uv-d.yx).a+
       Tex(sample_uv+float2(.006,.006)).a+Tex(sample_uv-float2(.006,.006)).a+
       Tex(sample_uv+float2(.006,-.006)).a+Tex(sample_uv+float2(-.006,.006)).a)*.125;
      float fringe=halo*.24*(1.0-material.a)*release_mask;
      float combined=material.a+fringe;
      material.rgb=(material.rgb*material.a+float3(1.0,.34,.035)*fringe)/max(combined,.0001);
      material.a=combined;
     }
     if(Motion(12.5,13.5)){
      float splitx=.61+.018*sin(uv.y*19.0)+.015*(noise2(uv*24.0)-.5);
      float splity=.67+.018*sin(uv.x*23.0);
      float mask=released_region<.5 ? 1.0-smoothstep(splitx-.012,splitx+.012,uv.x) : smoothstep(splitx-.012,splitx+.012,uv.x);
      if(released_region>.5 && released_region<1.5)mask*=1.0-smoothstep(splity-.012,splity+.012,uv.y);
      if(released_region>1.5)mask*=smoothstep(splity-.012,splity+.012,uv.y);
      float erosion=smoothstep(.23,.46,_Age)*.60;
      material.a*=mask*smoothstep(erosion-.18,erosion+.18,noise2(uv*25.0+float2(0,-_Age*2.0)));
     }
     if(Motion(22.5,23.5)){
      float boundary=.53+.025*(noise2(uv*29.0)-.5);
      float isolated=(1.0-smoothstep(boundary-.035,boundary+.035,uv.x))
       *smoothstep(.30,.37,uv.y)*(1.0-smoothstep(.75,.81,uv.y));
      float cooling=smoothstep(.065,.26,_Age);
      material.a*=isolated*smoothstep(cooling*.82-.16,cooling*.82+.16,noise2(uv*float2(29.0,37.0)+float2(_Age*2.0,0)));
      material.rgb*=1.0-.52*cooling;
     }
     if(Motion(16.5,17.5)){
      float breakaway=smoothstep(-.025,.055,_Age);
      float porous=noise2(uv*float2(39.0,31.0)+float2(_Age*5.0,-_Age*9.0));
      material.a*=smoothstep(breakaway*.85-.14,breakaway*.85+.14,porous);
     }
     if(Motion(13.5,14.5)){
      float opening=smoothstep(.025,.14,_Age);
      float porous=noise2(uv*float2(31.0,23.0)+float2(_Age*3.0,-_Age*8.0));
      material.a*=smoothstep(opening*.65-.18,opening*.65+.18,porous);
     }
     if(Motion(14.5,16.5)||Motion(25.5,27.5)){
      bool vegetal=_TextureMotion>25.5;
      bool tail_stroke=vegetal?_TextureMotion>26.5:_TextureMotion>15.5;
      if(tail_stroke){
       // Both quads overlap the complete ragged neck. Their separation
       // exposes eroded energy contours, never the UV rectangle edge.
       float boundary=.58+.045*sin(uv.y*31.0)+.025*(noise2(uv*31.0)-.5);
       float tear=smoothstep(.09,.20,_Age)*.018;
       if(released_region<1.5)
        material.a*=1.0-smoothstep(boundary-tear-.025,boundary-tear+.025,uv.x);
       else material.a*=smoothstep(boundary+tear-.025,boundary+tear+.025,uv.x);
      }
      float erase=tail_stroke?smoothstep(.15,.34,_Age):smoothstep(.055,.23,_Age);
      float ragged=.025*(noise2(uv*float2(37.0,19.0))-.5);
      material.a*=smoothstep(erase-.09+ragged,erase+.09+ragged,uv.x);
      float value=dot(material.rgb,float3(.25,.60,.15));
      float3 warm=(vegetal?float3(.20,.64,.16):float3(1.0,.46,.10))*value*1.35;
      if(vegetal){
       float fibres=.48+.52*smoothstep(.20,.78,.5+.5*sin(uv.y*95.0+uv.x*17.0+noise2(uv*16.0)*4.0));
       material.a*=fibres;
       material.rgb=lerp(float3(.11,.32,.075)*value*1.4,float3(.84,1.0,.69)*value,smoothstep(.32,.92,value));
      }
      material.rgb=lerp(warm,material.rgb,smoothstep(.55,.94,value));
      if(tail_stroke){
       // After the directional split, the hot tip cools before the
       // amber outline: it must not look like another live projectile.
       float cooling=smoothstep(.14,.30,_Age);
       material.rgb=lerp(material.rgb,(vegetal?float3(.16,.48,.055):float3(1.0,.34,.045))*value*.85,cooling*.90);
       material.a*=1.0-cooling*.40*smoothstep(.70,.98,value);
      }
     }
     if(Motion(17.5,18.5)){
      float dispersal=smoothstep(.19,.39,_Age)*.55;
      material.a*=smoothstep(dispersal-.20,dispersal+.20,
       noise2(uv*float2(27.0,31.0)+float2(-_Age*2.5,_Age*2.0)));
     }
     if(Motion(19.5,20.5)){
      float boundary=.58+.15*(uv.x-.60)+.018*sin(uv.x*31.0)
       +.018*(noise2(uv*float2(29.0,23.0))-.5);
      float lower=smoothstep(boundary-.04,boundary+.04,uv.y);
      material.a*=released_region<.5?1.0-lower:lower;
      float release=smoothstep(.15,.34,_Age);
      float neck=1.0-smoothstep(.02,.11,abs(uv.y-boundary));
      float erode=release*(.50+.28*neck);
      material.a*=smoothstep(erode-.20,erode+.20,
       noise2(uv*float2(31.0,27.0)+float2(-_Age*2.5,_Age*3.0)));
      material.rgb*=1.0-.35*release;
     }
     if(Motion(20.5,21.5)){
      float2 q=float2(uv.x*2.0,uv.y)-float2(.54,.50);
      float growth=lerp(.08,.80,smoothstep(-.01,.055,_Age));
      material.a*=1.0-smoothstep(growth-.045,growth+.025,length(q));
     }
     if(Motion(21.5,22.5)){
      float2 q=float2((uv.x-.5)*2.0,uv.y);
      float top=1.0-smoothstep(.46+.12*(q.x-.55)-.025,.46+.12*(q.x-.55)+.025,q.y);
      float right=smoothstep(.53-.025,.53+.025,q.x);
      float mask=released_region<.5?top:released_region<1.5?(1.0-top)*right:(1.0-top)*(1.0-right);
      material.a*=mask;
      float detach=.20*smoothstep(.11,.24,_Age);
      material.a*=smoothstep(detach-.025,detach+.025,length(q-float2(.54,.50)));
      material.rgb*=1.0-.28*smoothstep(.14,.38,_Age);
     }
     if(Motion(24.5,25.5)){
      // No rectangular crop: complementary porous split through the empty gap.
      float boundary=1.18+.022*sin(uv.x*19.0)+.025*(noise2(uv*23.0)-.5);
      float lower=smoothstep(boundary-.025,boundary+.025,uv.x+uv.y);
      if(released_region<1.5)material.a*=released_region<.5?1.0-lower:lower;
      float consumed=smoothstep(.06,.34,_Age);
      float distance_to_gap=length((uv-float2(.60,.58))*float2(1.0,.85));
      float outer=smoothstep(.08,.55,distance_to_gap);
      float threshold=consumed*(.20+.54*outer);
      material.a*=smoothstep(threshold-.13,threshold+.13,noise2(uv*float2(31.0,27.0)+float2(_Age*2.0,-_Age*1.5)));
      material.rgb*=1.0-.35*smoothstep(.16,.35,_Age);
     }
     if(Motion(27.5,28.5)){
      float2 local=(uv-float2(.035,.39))/float2(.42,.40);
      float radius=length((local-float2(.52,.475))*float2(1.0,.82));
      float front=lerp(.02,.76,smoothstep(-.13,.055,_Age));
      material.a*=1.0-smoothstep(front-.06,front+.05,radius);
      float erosion=smoothstep(.28,.64,_Age);
      material.a*=smoothstep(erosion-.18,erosion+.18,noise2(local*float2(29.0,23.0)));
      material.rgb*=.80;
     }
     if(Motion(30.5,31.5)){
      // Dry grain separates along its length; never advect it like fluid.
      float release=smoothstep(.085,.30,_Age);
      float grain=noise2(uv*float2(13.0,71.0));
      float knot=1.0-smoothstep(.035,.14,length((uv-float2(.34,.56))*float2(1.0,1.1)));
      material.a*=1.0-knot*smoothstep(.035,.105,_Age);
      material.a*=smoothstep(release*.70-.16,release*.70+.16,grain);
      material.rgb*=1.0-.24*smoothstep(.11,.32,_Age);
     }
     if(Motion(31.5,32.5)){
      // Preserve real low-alpha electrical shoulder, without old L^.65 loss.
      float release=smoothstep(.035,.17,_Age);
      material.a*=smoothstep(release*.68-.15,release*.68+.15,noise2(uv*float2(37.0,29.0)));
      material.rgb*=1.0-.22*release;
     }
     // 線形の URP: sRGB の絵はサンプルの時点で線形になっている (Godot の OUTPUT_IS_SRGB の分岐は要らない)
     float3 sampled=material.rgb;
     rgb = _AuthoredRgba>.5 ? sampled * max(max(tint.r,tint.g),tint.b) : rgb * sampled * 1.4;
     alpha *= material.a;
    }
    return float4(rgb,saturate(alpha));
   }
   ENDHLSL
  }
 }
}
