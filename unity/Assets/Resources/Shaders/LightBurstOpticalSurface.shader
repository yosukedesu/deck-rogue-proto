Shader "DeckRogue/LightBurstOpticalSurface" {
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
    float u=input.uv.x,v=input.uv.y;float3 rgb;float coverage;
    if(_Kind>41.5){coverage=(1-smoothstep(.12,.43,abs(v-.5)))*smoothstep(0,.12,u)*(1-smoothstep(.72,1,u));rgb=_RoleCore.rgb;}
    else {
     // Unity hardware sRGB decode before bilinear filtering matches the source's explicit per-tap Compatibility decode.
     float4 optical=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,lerp(float2(.08,.12),float2(.92,.88),float2(u,v)));
     rgb=optical.rgb;coverage=optical.a;
    }
    float edge=min(min(u,1-u),min(v,1-v));float alpha=saturate(input.color.a)*coverage*smoothstep(0,.025,edge);
    return float4(rgb*input.color.r*alpha,alpha);
   }
   ENDHLSL
  }
 }
}
