// StageDust — HD-2D 見本の月の塵 (moondust-slice) の粒だけが使う (2026-10-01 二周目 直しの輪2・反証 medium「空気の粒が無い」)。
// 今までの粒の材質 (ParticleSprite = 加算・深度を書かない・透明の列) では、ぼかし (TiltShift) が粒の後ろの物の深さ (奥の霧・背景の板) で
// 錯乱円を決めるので、ピントの帯の中に浮かぶ粒でも直径 15〜20px のぼけた輪になり、霧より +5 しか明るくならなかった。
// そこで粒の芯 (絵の α × 頂点の α が _Cutoff より大きい所) だけ深さを書く:
//   - 不透明の列 (AlphaTest 2450) で描く = 深さのテクスチャ (不透明の後の写し) に芯の深さが入り、ぼかしが芯を帯の中と読む = 芯はくっきり
//   - 芯は加算で深さを書く (パス "Core")。暈 (α が _Cutoff 以下) は深さを書かずに加算 (パス "Halo") = 後ろの深さでぼけて柔らかい光の輪になる
//   - 粒の系の sortingOrder (5) で不透明の物の後に描く = 加算の下地 (地形・幹・背景の板) が先に描かれている
//   - 粒の頂点の色は 0〜1 に丸められる (ParticleSystem は粒の色を Color32 で持つ) ので、HDR の明るさは _Intensity で掛ける
//     (R2B_MoondustSlice が look の色を最大の成分で割って頂点の色にし、その最大の成分を _Intensity に書く。ブルームの閾値 1.3 を越える)
//   - 影は落とさない (ShadowCaster のパスが無い)。霧はかけない (粒は座席の帯の中で霧が 0)
// 使うのは Stage.R2B_MoondustSlice だけ (光の設計図 look の moondust.sharp が true の時)。今の舞台 (stage=old) の粒は今までの材質のまま
Shader "DeckRogue/StageDust"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Core Cutoff", Range(0,1)) = 0.5
        _Intensity ("Intensity (HDR)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _Cutoff;
            float _Intensity;
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
        struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
        Varyings vert(Attributes i)
        {
            Varyings o;
            o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
            o.color = i.color;
            o.uv = TRANSFORM_TEX(i.uv, _MainTex);
            return o;
        }
        half Alpha(Varyings i) { return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a * i.color.a; }
        ENDHLSL

        Pass
        {
            Name "Core"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend One One
            ZWrite On
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(Varyings i) : SV_Target
            {
                half a = Alpha(i);
                clip(a - _Cutoff);
                return half4(i.color.rgb * a * _Intensity, 0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Halo"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(Varyings i) : SV_Target
            {
                half a = Alpha(i);
                clip(_Cutoff - a);
                return half4(i.color.rgb * a * _Intensity, 0);
            }
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
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(Varyings i) : SV_Target
            {
                clip(Alpha(i) - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
