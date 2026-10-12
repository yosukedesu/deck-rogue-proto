Shader "DeckRogue/AttackGlowFx" {
 Properties { _BaseMap("Authored glow",2D)="black"{} _Tint("Emission gain",Color)=(1,1,1,1) _AtlasRect("Atlas rectangle",Vector)=(1,1,0,0) _NextAtlasRect("Release next frame",Vector)=(1,1,0,0) _FrameBlend("Release blend",Range(0,1))=0 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+11" "RenderType"="Transparent" }
  Pass {
   Name "AuthoredAttackGlow" Tags { "LightMode"="UniversalForward" }
   Blend One One
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _Tint; float4 _AtlasRect; float4 _NextAtlasRect; float _FrameBlend;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float fog:TEXCOORD1;float2 nextUv:TEXCOORD2;};
   V vert(A v){V o;o.p=TransformObjectToHClip(v.p.xyz);o.uv=v.uv*_AtlasRect.xy+_AtlasRect.zw;o.nextUv=v.uv*_NextAtlasRect.xy+_NextAtlasRect.zw;o.fog=ComputeFogFactor(o.p.z);return o;}
   half4 frag(V i):SV_Target {
    half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
    // Only dedicated glow pixels; straight alpha is multiplied exactly once for additive blending.
    half3 radiance=c.rgb*c.a;
    if(_FrameBlend>0){half4 n=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.nextUv);radiance=lerp(radiance,n.rgb*n.a,_FrameBlend);}
    return half4(radiance*_Tint.rgb*_Tint.a*ComputeFogIntensity(i.fog),0);
   }
   ENDHLSL
  }
 }
}
