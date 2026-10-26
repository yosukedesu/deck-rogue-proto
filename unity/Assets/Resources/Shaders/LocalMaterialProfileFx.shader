Shader "DeckRogue/LocalMaterialProfileFx" {
 Properties {
  _BaseMap("Existing straight sRGB band",2D)="white"{}
  _LookupMap("Existing straight sRGB waterdrop lookup",2D)="white"{}
  _Kind("Mapped / water / drop / core / spore / pod / facet / solid",Float)=0
  _Family("Dark / light / botanical / physical / water",Float)=0
  _RoleShadow("Linear shadow",Vector)=(0,0,0,0)
  _RolePrimary("Linear primary",Vector)=(0,0,0,0)
  _RoleEdge("Linear edge",Vector)=(0,0,0,0)
  _RoleCore("Linear core",Vector)=(1,1,1,0)
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass {
   Name "LocalMaterialProfileForward" Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "FrozenLocalSurface.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   TEXTURE2D(_LookupMap);SAMPLER(sampler_LookupMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _RoleShadow,_RolePrimary,_RoleEdge,_RoleCore;
   float _Kind,_Family;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 phaseRole:TEXCOORD1;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 phaseRole:TEXCOORD1;float4 color:COLOR;};
   V vert(A input) {V output;output.p=TransformObjectToHClip(input.p.xyz);output.uv=input.uv;output.phaseRole=input.phaseRole;output.color=input.color;return output;}
   float4 frag(V input):SV_Target {
    // Importers provide hardware sRGB decode BEFORE bilinear interpolation; alpha is linear.
    // No global re-tint. Native gain times host Emission is already in vertex.r.
    // Phase and role remain per vertex. Each authored triangle has one constant role.
    float roleIndex=input.phaseRole.y;
    float3 role=roleIndex<.5?_RoleShadow.rgb:roleIndex<1.5?_RolePrimary.rgb:roleIndex<2.5?_RoleEdge.rgb:_RoleCore.rgb;
    float4 sampleRGBA=float4(1,1,1,1);
    if(_Kind<1.5)sampleRGBA=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
    else if(_Kind<2.5)sampleRGBA=SAMPLE_TEXTURE2D(_LookupMap,sampler_LookupMap,input.uv);
    return LocalFragment(input.uv,(int)_Kind,sampleRGBA,saturate(input.color.a),input.color.r,input.phaseRole.x,_RoleShadow.rgb,_RolePrimary.rgb,_RoleEdge.rgb,_RoleCore.rgb,role);
   }
   ENDHLSL
  }
 }
}
