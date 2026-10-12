Shader "DeckRogue/ScatterProfileFx" {
 Properties {
  _Kind("Core / particles",Float)=0
  _Family("Fire / warm / ice / spore",Float)=0
  _RolePrimary("Linear primary",Vector)=(0,0,0,0)
  _RoleEdge("Linear edge",Vector)=(0,0,0,0)
  _RoleCore("Linear core",Vector)=(1,1,1,0)
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass {
   Name "ScatterProfileForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "FrozenScatterSurface.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _RolePrimary,_RoleEdge,_RoleCore;
   float _Kind,_Family;
   CBUFFER_END
   struct A { float4 p:POSITION; float2 uv:TEXCOORD0; float2 phase:TEXCOORD1; float4 color:COLOR; };
   struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; float phase:TEXCOORD1; float4 color:COLOR; };
   V vert(A input) { V output;output.p=TransformObjectToHClip(input.p.xyz);output.uv=input.uv;output.phase=input.phase.x;output.color=input.color;return output; }
   float4 frag(V input):SV_Target {
    // Phase stays per particle through every vertex; no global phase uniform.
    // Vertex gain already contains native Gain times host Emission, each exactly once.
    return ScatterFragment(input.uv,(int)_Family,_Kind<0.5,input.phase,saturate(input.color.a),input.color.r,_RolePrimary.rgb,_RoleEdge.rgb,_RoleCore.rgb);
   }
   ENDHLSL
  }
 }
}
