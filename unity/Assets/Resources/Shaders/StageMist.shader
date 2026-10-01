// StageMist — HD-2D 見本 三周目の霧の板 (2026-10-01・レーン S。計画 docs/design/hd2d-round3-plan-2026-10-01.md §2 S・分析 R8)。
// 箱庭 (Diorama) の部品 "kind": "mist" だけが使う。今の舞台 (stage=old) は使わない。
// 加算の霧の面 (StageShaft) と違い α合成 (Blend SrcAlpha OneMinusSrcAlpha) = 後ろを「明るく足す」でなく「霧の色で覆う」。影も深度も書かない (1パス)。
//   色   = 霧の色 (距離の霧と同じ = unity_FogColor を霧の光の芯の向きで明暗。全体値 _HD2DFogLobePos/_HD2DFogLobeColor は StageModule と同じ式)、
//          部品が tint を持てばその色 (芯から外れるほど暗くなる割合は同じ)。舞台だけの周辺減光 (_HD2DStageVignette・座席の帯) も StageModule と同じ式で掛ける
//   濃さ = alpha × 縦の形 × 横の形 × 手続きの値ノイズ (テクスチャを使わない・2段) × 深度のなじみ × 近くの薄め。
//          縦と横の縁はノイズで揺らしてから柔らかく消す = 真っ直ぐな横線にしない。ノイズは板の上を flow (unit/秒) で横へ流れる
// 部品ごとの値は MaterialPropertyBlock (Diorama が板ごとに書く。材質は全部の霧の板で1つ):
//   _MistParams = (x alpha 0〜1・y flow unit/秒・z ノイズの細かさ 横 (回/unit)・w 縦 (回/unit))
//   _MistSize   = (x 幅 unit・y 高さ unit・z v0 (縦の形の下の切り。形は UV の v で読むので記録だけ)・w 乱数の種 0〜1)
//   _MistTint   = (rgb 霧の色 (線形)・a 1 なら rgb を使う / 0 なら霧の色)
// 板の形: 足元の中心が原点・幅 w・高さ h の縦の板 (DioramaMesh.GlowQuad)。UV = (0〜1, v0〜1)。表は −z (両面を描く)
Shader "DeckRogue/StageMist"
{
    Properties
    {
        _MistParams ("Mist (alpha, flow, noise u, noise v)", Vector) = (0.3, 0.02, 0.3, 0.8)
        _MistSize ("Mist Size (w, h, v0, seed)", Vector) = (20, 4, 0, 0)
        _MistTint ("Mist Tint (rgb, a = use)", Vector) = (0, 0, 0, 0)
        _SoftDepth ("Soft Depth Fade (unit)", Float) = 1.2
        _NearFade ("Near Fade Distance (unit)", Float) = 2
        _EdgeU ("Edge Fade (u)", Range(0.01, 0.5)) = 0.22
        _EdgeV ("Edge Fade (v)", Range(0.01, 0.5)) = 0.3
        _Wiggle ("Edge Wiggle", Range(0, 0.6)) = 0.28
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
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"   // HD2DPixel の SRGBToLinear (StageModule は Lighting.hlsl から入る)
            #include "Include/HD2DPixel.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _MistParams;
                float4 _MistSize;
                float4 _MistTint;
                float _SoftDepth, _NearFade, _EdgeU, _EdgeV, _Wiggle, _Fog;
            CBUFFER_END
            // 全体値 (StageLook が書く。StageModule と同じ名前と意味。0 = 使わない)
            float4 _HD2DFogLobePos;
            float4 _HD2DFogLobeColor;
            float4 _HD2DFogLobeAniso;
            float4 _HD2DStageVignette;
            float4 _HD2DSeatBand;
            float4 _HD2DSeatBandParam;

            // 霧の光の芯 (StageModule の HD2D_FogLobe と同じ式)
            half HD2D_MistLobe(float3 posWS)
            {
                if (_HD2DFogLobePos.w <= 0.0) return 1.0h;
                float3 v = SafeNormalize(posWS - _WorldSpaceCameraPos);
                float3 l = SafeNormalize(_HD2DFogLobePos.xyz - _WorldSpaceCameraPos);
                if (_HD2DFogLobeAniso.x > 0.0 && _HD2DFogLobeAniso.y > 0.0)
                {
                    float dv = asin(clamp(v.y, -1.0, 1.0)) - asin(clamp(l.y, -1.0, 1.0));
                    float dh = atan2(v.x, v.z) - atan2(l.x, l.z);
                    return half(exp(-0.5 * (_HD2DFogLobeAniso.x * dv * dv + _HD2DFogLobeAniso.y * dh * dh)));
                }
                return half(pow(saturate(dot(v, l)), _HD2DFogLobePos.w));
            }

            // 舞台だけの周辺減光 (StageModule の HD2D_StageVignette と同じ式。座席の帯は横の減光を受けない)
            half HD2D_MistVignette(float3 posWS)
            {
                if (_HD2DStageVignette.y <= 0.0 && _HD2DStageVignette.w <= 0.0) return 1.0h;
                float3 pv = TransformWorldToView(posWS);
                float iz = 1.0 / max(1e-3, -pv.z);
                float4x4 proj = UNITY_MATRIX_P;
                float nx = pv.x * iz * abs(proj._m00);
                float ny = pv.y * iz * abs(proj._m11);
                float side = smoothstep(_HD2DStageVignette.x, 0.5, abs(nx) * 0.5);
                if (_HD2DSeatBandParam.w > 0.5)
                {
                    float bt = posWS.x * _HD2DSeatBandParam.x - posWS.z * _HD2DSeatBandParam.y;
                    float bs = posWS.x * _HD2DSeatBandParam.y + posWS.z * _HD2DSeatBandParam.x;
                    float be = max(_HD2DSeatBandParam.z, 1e-3);
                    float dt = max(max(_HD2DSeatBand.x - bt, bt - _HD2DSeatBand.y), 0.0);
                    float ds = max(max(_HD2DSeatBand.z - bs, bs - _HD2DSeatBand.w), 0.0);
                    side *= 1.0 - (1.0 - smoothstep(0.0, be, dt)) * (1.0 - smoothstep(0.0, be, ds));
                }
                float fromTop = 0.5 - ny * 0.5;
                float top = _HD2DStageVignette.z > 0.0 ? 1.0 - smoothstep(0.0, _HD2DStageVignette.z, fromTop) : 0.0;
                return half(saturate((1.0 - _HD2DStageVignette.y * side) * (1.0 - _HD2DStageVignette.w * top)));
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;          // 板の UV (u 0〜1・v v0〜1)
                float2 noiseP : TEXCOORD1;      // ノイズの座標 (板の上の unit × 細かさ。流れ込み)
                float3 positionWS : TEXCOORD2;
                float eyeDepth : TEXCOORD3;
                float fogFactor : TEXCOORD4;
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
                o.uv = i.uv;
                // 板の上の位置 (unit。物体の xy = 足元の中心から) × 細かさ。横へ flow で流れる。種で板ごとにずらす
                float2 seedOff = float2(_MistSize.w * 37.1, _MistSize.w * 11.7);
                o.noiseP = (i.positionOS.xy + float2(-_MistParams.y * _Time.y, 0.0)) * max(_MistParams.zw, float2(1e-3, 1e-3)) + seedOff;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // 手続きの値ノイズ 2段 (0〜1)
                float n1 = HD2D_ValueNoise(i.noiseP);
                float n2 = HD2D_ValueNoise(i.noiseP * 2.03 + float2(17.1, -5.3));
                float n = n1 * 0.65 + n2 * 0.35;
                // 縦の形 (0〜1): 下は短く立ち上がり、上はゆっくり消える。縁の位置をノイズで揺らす (横線にしない)。
                // UV の v は v0〜1 (霧の面 kind fog の v0 と同じ = 形の下の立ち上がりを切る。切った下の縁は地面や段の後ろに埋めて深度のなじみで消す)
                float v = i.uv.y;
                float wig = (n - 0.5) * _Wiggle;
                float ve = v + wig;
                float vert = smoothstep(0.0, _EdgeV * 0.6, ve) * (1.0 - smoothstep(1.0 - _EdgeV * 1.6, 1.0, ve));
                // 横の形: 左右の端を柔らかく (これもノイズで揺らす)
                float ue = i.uv.x + wig * 0.5;
                float horiz = smoothstep(0.0, _EdgeU, ue) * smoothstep(0.0, _EdgeU, 1.0 - ue);
                float dens = saturate(_MistParams.x) * vert * horiz * lerp(0.45, 1.2, n);
                // 深度のなじみ (後ろの地面や幹に刺さる縁を消す。カメラの深度テクスチャ = StageShaft と同じ前提)
                if (_SoftDepth > 0.0)
                {
                    float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);
                    float scene = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                    dens *= saturate((scene - i.eyeDepth) / _SoftDepth);
                }
                // カメラの近く
                if (_NearFade > 0.0) dens *= saturate((i.eyeDepth - _ProjectionParams.y) / _NearFade);
                // 色: 霧の色 (芯の向きで明暗) か、部品の tint
                half lobe = HD2D_MistLobe(i.positionWS);
                half3 col = half3(unity_FogColor.rgb);
                if (_HD2DFogLobePos.w > 0.0) col = col * lerp(half(_HD2DFogLobeColor.a), 1.0h, lobe) + half3(_HD2DFogLobeColor.rgb) * lobe;
                if (_MistTint.a > 0.5)
                {
                    col = half3(_MistTint.rgb);
                    if (_HD2DFogLobePos.w > 0.0) col *= lerp(half(_HD2DFogLobeColor.a), 1.0h, lobe);
                }
                // 遠いほど距離の霧の色へ (霧の板そのものは霧の色なので差は小さい = 揃えるだけ)
                half3 fogC = half3(unity_FogColor.rgb);
                if (_HD2DFogLobePos.w > 0.0) fogC = fogC * lerp(half(_HD2DFogLobeColor.a), 1.0h, lobe) + half3(_HD2DFogLobeColor.rgb) * lobe;
                half3 fogged = MixFogColor(col, fogC, InitializeInputDataFog(float4(i.positionWS, 1.0), i.fogFactor));
                col = lerp(col, fogged, half(_Fog));
                col *= HD2D_MistVignette(i.positionWS);
                return half4(col, half(saturate(dens)));
            }
            ENDHLSL
        }
    }
}
