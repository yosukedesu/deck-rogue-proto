Shader "DeckRogue/VineSlashFx" {
 Properties { _BaseMap("Straight sRGB source",2D)="white"{} _UseTexture("Surface ramp or solid accent",Float)=1 _Gain("Linear HDR gain",Float)=1 _RoleShadow("Linear shadow",Vector)=(0,0,0,0) _RolePrimary("Linear primary",Vector)=(0,0,0,0) _RoleEdge("Linear edge",Vector)=(0,0,0,0) _RoleCore("Linear core",Vector)=(1,1,1,0) }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent" }
  Pass {
   Name "VineSlashForward" Tags { "LightMode"="UniversalForward" }
   Blend One OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _RoleShadow,_RolePrimary,_RoleEdge,_RoleCore;float _UseTexture,_Gain;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float alpha:TEXCOORD1;};
   V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.alpha=a.color.a;return o;}
   float4 frag(V i):SV_Target {
    float coverage=i.alpha;float3 role=_RoleCore.rgb;
    if(_UseTexture>.5){
     // In this linear project the sRGB texture sampler decodes RGB once; alpha is linear.
     float4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
     float inside=step(0,i.uv.x)*step(i.uv.x,1)*step(0,i.uv.y)*step(i.uv.y,1);
     coverage*=tex.a*inside;
     float y=dot(tex.rgb,float3(.2126,.7152,.0722));
     role=y<.35?lerp(_RoleShadow.rgb,_RolePrimary.rgb,saturate(y/.35)):lerp(_RolePrimary.rgb,_RoleEdge.rgb,saturate((y-.35)/.45));
    }
    coverage=saturate(coverage);
    // One coverage multiplication only. No second additive copy or palette tint.
    return float4(role*_Gain*coverage,coverage);
   }
   ENDHLSL
  }
 }
}
