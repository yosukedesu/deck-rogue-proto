Shader "DeckRogue/WaterContinuousFx" {
 Properties {_BodyMap("Straight sRGB flow source",2D)="white"{} _LookupMap("sRGB droplet color field",2D)="white"{} _Kind("Body / core / droplet",Float)=0}
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass {
   Name "WaterContinuousForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BodyMap);SAMPLER(sampler_BodyMap);TEXTURE2D(_LookupMap);SAMPLER(sampler_LookupMap);
   CBUFFER_START(UnityPerMaterial)
   float _Kind;
   CBUFFER_END
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.color=i.color;return o;}
   float4 frag(V i):SV_Target {float a=i.color.a;float3 c=i.color.rgb;float inside=1;
    if(_Kind<.5){float4 tex=SAMPLE_TEXTURE2D(_BodyMap,sampler_BodyMap,i.uv);c*=tex.rgb;a*=tex.a;inside=step(0,i.uv.x)*step(i.uv.x,1)*step(0,i.uv.y)*step(i.uv.y,1);}
    else if(_Kind>1.5){float3 lookup=SAMPLE_TEXTURE2D(_LookupMap,sampler_LookupMap,i.uv).rgb;c*=lookup;}
    a=saturate(a)*inside;return float4(c*a,a);
   }
   ENDHLSL
  }
 }
}
