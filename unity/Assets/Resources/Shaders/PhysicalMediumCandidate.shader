Shader "DeckRogue/PhysicalMediumCandidate" {
 Properties {_Shadow("Linear shadow",Color)=(.0865,.1119,.1413,1) _Primary("Linear steel",Color)=(.565,.565,.515,1) _Edge("Linear edge",Color)=(.73,.716,.638,1) _Core("Linear core",Color)=(.887,.855,.776,1)}
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
 Pass {Name "PhysicalMediumForward" Tags {"LightMode"="UniversalForward"}
 Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _Shadow,_Primary,_Edge,_Core;
 CBUFFER_END
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float4 data:COLOR;};
 struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 data:TEXCOORD1;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.data=i.data;return o;}
 float hash21(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float grain(float2 p){float2 c=floor(p),r=frac(p);r=r*r*(3-2*r);return lerp(lerp(hash21(c),hash21(c+float2(1,0)),r.x),lerp(hash21(c+float2(0,1)),hash21(c+1),r.x),r.y);}
 float4 frag(V i):SV_Target {
  float a=saturate(i.data.r),gain=max(0,i.data.g),kind=i.data.b;float3 c;
  if(kind<1.5 || (kind>3.5 && kind<4.5)) {
   // Portable fixed optical grain approximation, not a claim of Blender Noise/Principled parity.
   float2 p=i.uv*float2(16,110);float n=(grain(p)+.70*grain(p*2.03)) / 1.70;
   c=n<.48?lerp(_Shadow.rgb,_Primary.rgb,saturate((n-.18)/.30)):lerp(_Primary.rgb,_Edge.rgb,saturate((n-.48)/.30));
   c*=.28+gain*(kind<.5?.86:.62);a*=clamp((n-.20)*1.75,.15,1);
  } else if(kind>5.5) {float rad=length(i.uv*2-1);float f=max(0,1-rad);a*=f*f*f;c=_Edge.rgb*1.5;}
  else c=(kind>2.5&&kind<3.5?_Core.rgb:_Edge.rgb)*gain*(kind<2.5?2:kind<3.5?4.5:1.6);
  a=saturate(a);return float4(c*a,a);
 }
 ENDHLSL
 }}
}
