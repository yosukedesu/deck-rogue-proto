// StageShaft — HD-2D 見本の光の筋・光る霧の面・地面の光の輪 用 (2026-09-30 P03。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-4・§2-5)。
// 箱庭 (Diorama・StageLook) だけが使う。加算 (Blend One One) で、影も深度も書かない。
//   色 = 絵 × _Tint (rgb × a) × _Intensity × 深度のなじみ × 近くの薄め × 面の向きの薄め、を霧に沈めて足す
//   _SoftDepth … 後ろの物との深さの差がこの距離 (unit) より小さい所ほど薄くする = 地面や崖に刺さる縁の線を消す (0 以下で切る)。
//                カメラの深度テクスチャが要る (StageLook.Apply が requiresDepthTexture=true にする)。
//   _NearFade  … カメラの近くの面からこの距離 (unit) までは薄くする (手前を横切る筋が画面を覆わない)
//   _EdgeFade  … 0 より大きいと、面を真横から見るほど薄くする (|法線・視線| の _EdgeFade 乗)。板の筋が線に見えるのを防ぐ
//   _Scroll    … xy = 絵の流れる速さ (UV/秒。揺れる霧)。det の撮影では時間が固定の刻みなので決定的
//   _Fog       … 霧に沈む割合 (1 = 遠いほど霧に消えて足されない)
//   _LobeFloor … 霧の光の芯 (全体値 _HD2DFogLobePos。StageLook が書く) から外れた所の明るさの倍率 (0〜1。1 = 芯を見ない = 今まで)。
//                芯 (坑口の奥の脈) の方を向く面ほど明るく、画面の端ほど暗い = 中央の奥が光り、左右の端が沈む (W3 P22)
//   _SkipTopVig … 1 なら頂点色の a (設計図の lobe) が 0 の面 = 月光の筋は舞台の上の減光を受けない (霧の面は a 1 で今まで)。0 = 今まで (二周目 段2 R2B。StageLook が look の materials.glow.shaftSkipTopVig から書く)
Shader "DeckRogue/StageShaft"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _Tint ("Tint", Color) = (1,1,1,1)
        _Intensity ("Intensity", Float) = 1
        _SoftDepth ("Soft Depth Fade", Float) = 1
        _NearFade ("Near Fade Distance", Float) = 2
        _EdgeFade ("Edge Fade Power", Float) = 0
        _Scroll ("UV Scroll (xy)", Vector) = (0,0,0,0)
        _Fog ("Fog", Range(0,1)) = 1
        _LobeFloor ("Lobe Floor", Range(0,1)) = 1
        _SkipTopVig ("Skip Top Vignette by Vertex Alpha", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Tint;
                half _Intensity, _SoftDepth, _NearFade, _Fog;
                half _EdgeFade, _Cull, _LobeFloor;
                half _SkipTopVig;   // 二周目 段2 (R2B): 1 = 頂点色の a が 0 の面 (月光の筋) は上の減光を受けない (0 = 今まで。StageLook が look の materials.glow.shaftSkipTopVig から書く)
                float4 _Scroll;
            CBUFFER_END
            float4 _HD2DFogLobePos;   // 全体値 (StageLook。xyz = 霧の光の芯・w = 絞り。0 = 使わない)
            float4 _HD2DFogLobeAniso; // 全体値 (直しの輪1。x = 縦・y = 横の絞り。どちらかが 0 なら cos の w 乗 = 今まで。StageModule と同じ式)
            float4 _HD2DStageVignette; // 全体値 (StageLook。舞台だけの周辺減光。StageModule と同じ式・0 = 使わない。W3b P22)
            // topW = 上の減光の効き (1 = 今まで・0 = 上の減光なし。二周目 段2 R2B)
            half HD2D_StageVignette(float3 posWS, half topW)
            {
                if (_HD2DStageVignette.y <= 0.0 && _HD2DStageVignette.w <= 0.0) return 1.0h;
                float3 pv = TransformWorldToView(posWS);
                float iz = 1.0 / max(1e-3, -pv.z);
                float4x4 proj = UNITY_MATRIX_P;
                float nx = pv.x * iz * abs(proj._m00);
                float ny = pv.y * iz * abs(proj._m11);
                float side = smoothstep(_HD2DStageVignette.x, 0.5, abs(nx) * 0.5);
                float fromTop = 0.5 - ny * 0.5;
                float top = _HD2DStageVignette.z > 0.0 ? 1.0 - smoothstep(0.0, _HD2DStageVignette.z, fromTop) : 0.0;
                top *= topW;
                return half(saturate((1.0 - _HD2DStageVignette.y * side) * (1.0 - _HD2DStageVignette.w * top)));
            }
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;      // 部品ごとの明るさ (rgb) と霧の光の芯の効き (a)。既定は白 = 今まで (W3b P22・Diorama.GlowVertexColor)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
                float eyeDepth : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float3 normalWS : TEXCOORD4;
                half4 color : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.eyeDepth = -p.positionVS.z;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.color = i.color;
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap) + _Scroll.xy * _Time.y;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _Tint;
                c.rgb *= i.color.rgb;   // 部品ごとの明るさ (設計図の gain。既定 1)
                half k = c.a * _Intensity;
                // 深度のなじみ (後ろの物に刺さる縁を消す)
                if (_SoftDepth > 0.0h)
                {
                    float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);
                    float scene = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                    k *= half(saturate((scene - i.eyeDepth) / _SoftDepth));
                }
                // カメラの近く
                if (_NearFade > 0.0h) k *= half(saturate((i.eyeDepth - _ProjectionParams.y) / _NearFade));
                // 面を真横から見るほど薄く
                if (_EdgeFade > 0.0h)
                {
                    float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                    k *= half(pow(saturate(abs(dot(SafeNormalize(i.normalWS), v))), _EdgeFade));
                }
                // 霧の光の芯から外れるほど薄く (W3 P22。全体値が 0 か _LobeFloor=1 なら今まで)
                if (_HD2DFogLobePos.w > 0.0 && _LobeFloor < 1.0h)
                {
                    float3 v = SafeNormalize(i.positionWS - _WorldSpaceCameraPos);
                    float3 l = SafeNormalize(_HD2DFogLobePos.xyz - _WorldSpaceCameraPos);
                    half lobe = half(pow(saturate(dot(v, l)), _HD2DFogLobePos.w));
                    if (_HD2DFogLobeAniso.x > 0.0 && _HD2DFogLobeAniso.y > 0.0)
                    {
                        float dv = asin(clamp(v.y, -1.0, 1.0)) - asin(clamp(l.y, -1.0, 1.0));
                        float dh = atan2(v.x, v.z) - atan2(l.x, l.z);
                        lobe = half(exp(-0.5 * (_HD2DFogLobeAniso.x * dv * dv + _HD2DFogLobeAniso.y * dh * dh)));
                    }
                    // 部品ごとの芯の効き (設計図の lobe = 頂点色の a。1 = 今まで・0 = 芯を見ない = 光の筋は画面の左右でも薄めない)
                    k *= lerp(1.0h, lerp(_LobeFloor, 1.0h, lobe), i.color.a);
                }
                // 舞台だけの周辺減光 (W3b P22。0 = そのまま)。頂点色の a は2役 = 霧の光の芯の効き (上) と、_SkipTopVig の時の上の減光の効き (二周目 段2 R2B: 月光の筋 a 0 は上の減光を受けない・霧の面 a 1 は今まで)
                half3 col = c.rgb * k * HD2D_StageVignette(i.positionWS, lerp(1.0h, i.color.a, _SkipTopVig));
                // 霧に沈む (加算なので、霧の色でなく黒へ寄せる = 遠いほど足されない)
                half3 sunk = MixFogColor(col, half3(0.0h, 0.0h, 0.0h), InitializeInputDataFog(float4(i.positionWS, 1.0), i.fogFactor));
                col = lerp(col, sunk, _Fog);
                return half4(col, 0.0h);
            }
            ENDHLSL
        }
    }
}
