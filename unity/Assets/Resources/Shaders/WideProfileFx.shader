Shader "DeckRogue/WideProfileFx" {
 Properties {
  _BaseMap("Existing straight sRGB band",2D)="white"{}
  _LookupMap("Existing straight sRGB waterdrop lookup",2D)="white"{}
  _Kind("Water band / fire band / drop / ember / core",Float)=0
  _Family("Water / fire",Float)=0
  _RoleShadow("Linear shadow",Vector)=(0,0,0,0)
  _RolePrimary("Linear primary",Vector)=(0,0,0,0)
  _RoleEdge("Linear edge",Vector)=(0,0,0,0)
  _RoleCore("Linear core",Vector)=(1,1,1,0)
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass {
   Name "WideProfileForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "FrozenWideSurface.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   TEXTURE2D(_LookupMap);SAMPLER(sampler_LookupMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _RoleShadow,_RolePrimary,_RoleEdge,_RoleCore;
   float _Kind,_Family;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 phase:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float phase:TEXCOORD1;float4 color:COLOR;};
   V vert(A input) {V output;output.p=TransformObjectToHClip(input.p.xyz);output.uv=input.uv;output.phase=input.phase.x;output.color=input.color;return output;}
   float4 frag(V input):SV_Target {
    // Importers provide hardware sRGB decode BEFORE bilinear interpolation; alpha is linear.
    // No global re-tint. Native gain times host Emission is already in vertex.r.
    // Stable phase is transported per vertex; frozen R2 wide surface is phase-independent.
    float4 sampleRGBA=float4(1,1,1,1);
    if(_Kind<1.5)sampleRGBA=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
    else if(_Kind<2.5)sampleRGBA=SAMPLE_TEXTURE2D(_LookupMap,sampler_LookupMap,input.uv);
    return WideFragment(input.uv,(int)_Kind,sampleRGBA,saturate(input.color.a),input.color.r,_RoleShadow.rgb,_RolePrimary.rgb,_RoleEdge.rgb,_RoleCore.rgb);
   }
   ENDHLSL
  }
 }
}
