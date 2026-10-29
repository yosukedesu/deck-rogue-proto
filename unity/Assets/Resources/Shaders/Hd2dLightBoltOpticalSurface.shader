Shader "DeckRogue/Hd2dLightBoltOpticalSurface" {
 Properties {
  _BaseMap("Original optical density",2D)="white"{}
  _Kind("Layer",Float)=41
  _RoleCore("Linear core",Color)=(1,1,1,1)
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float _Kind;float4 _RoleCore;
   CBUFFER_END
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float2 gain:TEXCOORD1;float4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 density:TEXCOORD1;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.density=float2(i.gain.x,i.color.a);return o;}
   float4 frag(V i):SV_Target {
    float coverage;float3 rgb;
    if(_Kind>41.5){
     coverage=(1-smoothstep(.12,.43,abs(i.uv.y-.5)))*smoothstep(0,.16,i.uv.x)*(1-smoothstep(.65,1,i.uv.x));
     rgb=_RoleCore.rgb;
    }else{
     float2 imageUV=lerp(float2(.15,.34),float2(.86,.72),float2(i.uv.x,1-i.uv.y));
     float4 optical=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,float2(imageUV.x,1-imageUV.y));
     coverage=optical.a;rgb=optical.rgb;
    }
    float edge=min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y));
    return float4(rgb*i.density.x,i.density.y*coverage*smoothstep(0,.025,edge));
   }
   ENDHLSL
  }
 }
}
