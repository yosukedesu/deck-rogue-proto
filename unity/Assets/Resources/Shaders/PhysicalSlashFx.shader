Shader "DeckRogue/PhysicalSlashFx" {
 Properties {_BaseMap("Straight sRGB source",2D)="white"{} _UseTexture("Direct RGB surface / linear core",Float)=1}
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass {
   Name "PhysicalSlashForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float _UseTexture;
   CBUFFER_END
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.color=i.color;return o;}
   float4 frag(V i):SV_Target {float a=i.color.a;float3 c=i.color.rgb;if(_UseTexture>.5){float4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);c*=tex.rgb;a*=tex.a;}a=saturate(a);return float4(c*a,a);}
   ENDHLSL
  }
 }
}
