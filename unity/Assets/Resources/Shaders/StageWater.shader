// StageWater.shader — 舞台の水面 (2026-09-21 HD-2D 裁定「幕3の水面（反射）」)。
// 鏡像カメラ (Stage.Reflection) が描いた _ReflectionTex をスクリーン座標で重ね、さざ波のノイズで歪ませる。
// 水の色 (_Tint) と反射 (_ReflectAmount) を混ぜ、遠くは霧に溶かす。影も深度も書かない (透明)。
Shader "DeckRogue/StageWater"
{
    Properties
    {
        [MainTexture] _BaseMap ("Ripple Noise", 2D) = "gray" {}
        [MainColor] _BaseColor ("Water Tint", Color) = (0.08,0.14,0.2,0.85)
        _ReflectAmount ("Reflection", Range(0,1)) = 0.55
        _Distort ("Distortion", Range(0,0.1)) = 0.012
        _Speed ("Ripple Speed", Vector) = (0.02,0.035,0,0)
        _Sparkle ("Sparkle", Range(0,2)) = 0.6
        _SparkleColor ("Sparkle Color", Color) = (0.7,0.9,1,1)
        _Fog ("Fog", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            TEXTURE2D(_ReflectionTex); SAMPLER(sampler_ReflectionTex);
            half _ReflectAmount; half _Distort; float4 _Speed; half _Sparkle; half4 _SparkleColor; half _Fog; half _HasReflection;
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float fogFactor : TEXCOORD1; float4 screenPos : TEXCOORD2; float3 positionWS : TEXCOORD3; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.screenPos = ComputeScreenPos(p.positionCS);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                half n1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv + _Speed.xy * t).r;
                half n2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv * 1.7 - _Speed.yx * t * 0.8).r;
                half n = (n1 + n2) * 0.5;
                float2 suv = i.screenPos.xy / max(0.0001, i.screenPos.w);
                suv.x = 1.0 - suv.x;   // 鏡像カメラは回転で作る (行列式 +1) ので、真の鏡像に対して左右が反転している = ここで戻す
                suv += (n - 0.5) * _Distort;
                half3 refl = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, suv).rgb;
                half3 water = _BaseColor.rgb;
                half3 c = lerp(water, refl, _ReflectAmount * _HasReflection);
                // さざ波の頂 (n が高い所) にきらめき
                half sp = smoothstep(0.62, 0.8, n) * _Sparkle;
                c += _SparkleColor.rgb * sp;
                half a = _BaseColor.a;
                half3 fogged = MixFog(c, i.fogFactor);
                c = lerp(c, fogged, _Fog);
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
