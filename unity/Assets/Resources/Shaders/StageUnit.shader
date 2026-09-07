// StageUnit — HD-2D 舞台のドット絵用 (2026-09-07)。
// 陰影を付けず絵の色をそのまま出す (アンリット) が、影は落とし (ShadowCaster)、被写界深度のために深度も書く (DepthOnly)。
// _Flash で白く光る (被弾)。_Fog=0 で霧を無視 (空・月)。
Shader "DeckRogue/StageUnit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.4
        _Flash ("Flash", Range(0,1)) = 0
        _Fog ("Fog", Range(0,1)) = 1
        _Cull ("Cull", Float) = 0
        _Ambient ("Ambient", Color) = (1,1,1,1)
        _LampPos ("Lamp Position", Vector) = (0,0,0,0)
        _LampColor ("Lamp Color", Color) = (0,0,0,0)
        _LampFalloff ("Lamp Falloff", Float) = 14
        _SunDir2 ("Sun Direction (uv)", Vector) = (0.7,0.7,0,0)
        _SunAmount ("Sun Gradient", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            half _Flash;
            half _Fog;
            half4 _Ambient;
            float4 _LampPos;
            half4 _LampColor;
            float _LampFalloff;
            float4 _SunDir2;
            float _SunAmount;
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float fogFactor : TEXCOORD1; float3 positionWS : TEXCOORD2; float2 uv0 : TEXCOORD3; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.uv0 = i.uv;
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                clip(c.a - _Cutoff);
                // 夜の環境光 + ランタン (距離で減衰。板の中でランタンに近い側が暖かくなる)
                float d2 = dot(i.positionWS - _LampPos.xyz, i.positionWS - _LampPos.xyz);
                half3 light = _Ambient.rgb + _LampColor.rgb * (1.0 / (1.0 + d2 / max(0.01, _LampFalloff)));
                // 月光の向き: 板の中で光源側 (右上) が明るく、反対側 (左下) が暗い
                float g = dot(i.uv0 - 0.5, _SunDir2.xy);
                light *= (1.0 + g * _SunAmount);
                c.rgb *= light;
                c.rgb = lerp(c.rgb, half3(1, 1, 1), _Flash);
                half3 fogged = MixFog(c.rgb, i.fogFactor);
                c.rgb = lerp(c.rgb, fogged, _Fog);
                return half4(c.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #define _ALPHATEST_ON 1
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Off
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #define _ALPHATEST_ON 1
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
