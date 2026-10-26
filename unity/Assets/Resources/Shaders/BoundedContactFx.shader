Shader "DeckRogue/BoundedContactFx" {
 Properties { _Primary("Linear primary",Color)=(.65,.65,.65,1) _Core("Linear core",Color)=(1,1,1,1) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass { Name "BoundedContactForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Primary,_Core;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 mode:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 mode:TEXCOORD1;float4 color:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.mode=i.mode;o.color=i.color;return o;}
   float4 frag(V i):SV_Target {
    float2 centered=i.uv*2-1;
    float radial=saturate(1-dot(centered,centered));
    float edge=i.mode.x>.5?radial*radial:saturate(min(i.uv.y,1-i.uv.y)*12);
    float alpha=saturate(i.color.a*edge);
    float3 rgb=lerp(_Primary.rgb,_Core.rgb,radial*.55)*max(0,i.color.r);
    return float4(rgb*alpha,alpha);
   }
   ENDHLSL
  }
 }
}
