Shader "DeckRogue/LightningDischargeFx" {
 Properties {
  _BaseMap("Straight sRGB lightning source",2D)="white"{}
  _Kind("Branches / core / sparks",Float)=0
  _Age("Native continuous age",Float)=0
  _RoleShadow("Linear shadow",Vector)=(0,0,0,0)
  _RolePrimary("Linear primary",Vector)=(0,0,0,0)
  _RoleEdge("Linear edge",Vector)=(0,0,0,0)
  _RoleCore("Linear core",Vector)=(1,1,1,0)
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass {
   Name "LightningDischargeForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "LightningSurface.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _RoleShadow,_RolePrimary,_RoleEdge,_RoleCore;
   float _Kind,_Age;
   CBUFFER_END
   struct A { float4 p:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
   struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
   V vert(A input) { V output;output.p=TransformObjectToHClip(input.p.xyz);output.uv=input.uv;output.color=input.color;return output; }
   float4 frag(V input):SV_Target {
    float opacity=saturate(input.color.a);
    if(_Kind<0.5) {
     // Hardware sRGB sampling decodes RGB once; texture alpha remains linear coverage.
     float4 textureLinear=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
     float4 result=LightningFragment(textureLinear,input.uv,_Age,opacity,_RoleShadow.rgb,_RolePrimary.rgb,_RoleEdge.rgb,_RoleCore.rgb);
     result.rgb*=input.color.r; // Host bounded Emission once; native 2.35 is already inside the include.
     return result; // Frozen function already premultiplied once.
    }
    float coverage=SoftCoverage(input.uv);
    float alpha=coverage*opacity;
    float nativeGain=_Kind<1.5?3.0:2.1;
    float3 rgb=lerp(_RolePrimary.rgb,_RoleCore.rgb,coverage)*nativeGain*input.color.r;
    return float4(rgb*alpha,alpha); // Host Emission (core) / Particles (sparks) once.
   }
   ENDHLSL
  }
 }
}
