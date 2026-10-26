Shader "DeckRogue/ElementLuminousFx" {
 Properties { _BaseMap("Atlas",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1) _BodyGain("Linear scalar gain",Float)=1 _AtlasRect("Atlas rectangle",Vector)=(1,1,0,0) }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
  CBUFFER_START(UnityPerMaterial)
  float4 _Tint; float4 _AtlasRect; float _BodyGain;
  CBUFFER_END
  struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};
  struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float fog:TEXCOORD1;};
  V vert(A v){V o;o.p=TransformObjectToHClip(v.p.xyz);o.uv=v.uv*_AtlasRect.xy+_AtlasRect.zw;o.fog=ComputeFogFactor(o.p.z);return o;}
  half Body(half alpha){return alpha*smoothstep(.15h,.90h,alpha);}
  half4 core(V i):SV_Target {
   half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
   return half4(MixFog(saturate(c.rgb*_BodyGain),i.fog),Body(c.a)*_Tint.a);
  }
  half4 halo(V i):SV_Target {
   half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
   // Disjoint opacity partition: body + halo == source alpha. No full-image double draw.
   half energy=c.a-Body(c.a);
   return half4(c.rgb*energy*4.0h*_BodyGain*ComputeFogIntensity(i.fog)*_Tint.a,0);
  }
  ENDHLSL
  Pass {
   Name "AuthoredCore" Tags {"LightMode"="ElementAfterPost"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment core
   #pragma multi_compile_fog
   ENDHLSL
  }
  Pass {
   Name "EmissiveHalo" Tags {"LightMode"="UniversalForward"}
   Blend One One
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment halo
   #pragma multi_compile_fog
   ENDHLSL
  }
 }
}
