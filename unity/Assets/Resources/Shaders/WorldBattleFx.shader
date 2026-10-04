Shader "DeckRogue/WorldBattleFx"
{
    Properties { _BaseMap("Atlas",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1) _AtlasRect("Atlas rectangle",Vector)=(1,1,0,0) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Name "WorldBattleFx" Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest LEqual Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint; float4 _AtlasRect;
            CBUFFER_END
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; float fog:TEXCOORD1; half4 color:COLOR; };
            V vert(A v) { V o; o.p=TransformObjectToHClip(v.p.xyz); o.uv=v.uv*_AtlasRect.xy+_AtlasRect.zw; o.fog=ComputeFogFactor(o.p.z); o.color=v.color; return o; }
            half4 frag(V i):SV_Target { half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_Tint*i.color; c.rgb=MixFog(c.rgb,i.fog); return c; }
            ENDHLSL
        }
    }
}
