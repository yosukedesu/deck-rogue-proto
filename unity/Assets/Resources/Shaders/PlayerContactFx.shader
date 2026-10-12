Shader "DeckRogue/PlayerContactFx" {
 Properties {_BaseMap("Straight sRGB source",2D)="white"{} _Kind("Solid / pierce / rock / blunt",Float)=0 _RoleShadow("Linear shadow",Vector)=(0,0,0,0) _RolePrimary("Linear primary",Vector)=(0,0,0,0) _RoleEdge("Linear edge",Vector)=(0,0,0,0) _RoleCore("Linear core",Vector)=(1,1,1,0)}
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass {
   Name "PhysicalDeliveryForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _RoleShadow,_RolePrimary,_RoleEdge,_RoleCore;float _Kind;
   CBUFFER_END
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.color=a.color;return o;}
   float DeliverySmooth(float x){float q=saturate(x);return q*q*(3-2*q);}
   float4 frag(V i):SV_Target {
    float a=i.color.a;float3 c=i.color.rgb;
    if(_Kind>.5){float4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);float l=dot(tex.rgb,float3(.2126,.7152,.0722));float3 last=_Kind>2.5?_RoleCore.rgb:_RoleEdge.rgb;float3 role=l<.35?lerp(_RoleShadow.rgb,_RolePrimary.rgb,saturate(l/.35)):l<.8?lerp(_RolePrimary.rgb,_RoleEdge.rgb,(l-.35)/.45):lerp(_RoleEdge.rgb,last,saturate((l-.8)/.2));c=role*i.color.r;a*=tex.a;
     if(_Kind<1.5){float u=(i.uv.x-69.0/1774)/(768.0/1774);a*=DeliverySmooth(u/.12)*(1-DeliverySmooth((u-.985)/.015));}
    }
    return float4(c*a,a);
   }
   ENDHLSL
  }
 }
}
