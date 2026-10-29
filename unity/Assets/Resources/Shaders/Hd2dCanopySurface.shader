Shader "DeckRogue/Hd2dCanopySurface" {
 Properties {_BaseMap("Original organic density",2D)="white"{} _Age("Actual contact age",Float)=0 _SourceEncoded("QA source encoded sampling",Float)=0 _ReviewSolid("QA solid",Float)=0 _ReviewZTest("QA depth test",Float)=4}
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest [_ReviewZTest] Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float _Age;float _ReviewSolid;float _SourceEncoded;
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float2 gain:TEXCOORD1;float4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float gain:TEXCOORD1;float alpha:TEXCOORD2;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.gain=i.gain.x;o.alpha=i.color.a;return o;}
   float4 frag(V i):SV_Target{
    if(_ReviewSolid>.5){clip(i.alpha-.00001);return float4(.05,1,.15,1);}
    float u=i.uv.x,v=i.uv.y;
    // Godot's direct-v image crop: v0=image .22, v1=image .78.
    // Only the image-to-Unity texture-origin conversion is applied, unlike Cold's (1-v) crop.
    float4 organic=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(lerp(.18,.82,u),1-lerp(.22,.78,v)));
    // Canopy-only opt-in source Compatibility interpretation; no raw/import changes.
    float3 candidateRgb=organic.rgb;
    if(_SourceEncoded>.5){float3 c=max(organic.rgb,0);candidateRgb=lerp(1.055*pow(c,1.0/2.4)-.055,12.92*c,step(c,.0031308));}
    float edge=min(min(u,1-u),min(v,1-v));
    float release=smoothstep(.065,.225,_Age),irregular=u+.12*sin(7*v+4*u);
    float consume=smoothstep(release*.80-.18,release*.80+.02,irregular);
    float bodyPhase=smoothstep(.050,.135,_Age),rootPhase=smoothstep(.100,.170,_Age);
    float tissue=organic.g-organic.r*.25;
    float contour=u+.42*(tissue-.30)+.14*sin(8*v+3*u)+.10*sin(14*v-5*u);
    float bodyFront=lerp(-.55,1.70,bodyPhase),rootFront=lerp(-.55,1.70,rootPhase);
    float remaining=lerp(smoothstep(bodyFront-.045,bodyFront+.045,contour),smoothstep(rootFront-.045,rootFront+.045,contour),smoothstep(.76,.92,u));
    float a=saturate(i.alpha*smoothstep(0,.025,edge)*consume*organic.a*remaining);
    return float4(candidateRgb*i.gain*a,a);
   }
   ENDHLSL
  }
 }
}
