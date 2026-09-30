// StageModule — HD-2D 見本の舞台の部品 (地形・3Dの部品・半立体) 用 (2026-09-30 P03。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-2・§2-4・§2-5)。
// 箱庭 (Diorama。P04 が材質を作る) だけが使う。今の舞台 (stage=old) は使わない。
//
// 光の形 (二重の陰影で泥やプラスチックに見えないよう、受光と影を分ける。計画 §7-7)
//   直接光 D = Σ 光の色 × 距離の減衰 × max(0, 法線・光)   (月・舞台の灯・逆光・技の光。クッキー = 木漏れ日 は光の色に掛かる)
//   色      = 絵の色 × (環境光 A ＋ _Receive × D) × 影
//   影      = 1 − _ShadowStrength × (影で失った光の明るさ ÷ (A ＋ D) の明るさ)   … 受光を下げても、落ちる影は同じ割合で読める
//   A = 場の環境光 (Trilight → SH) × SSAO。_Receive の既定: 地形 0.8・半立体 0.35・札 0.25 (P04/P22 が材質に書く)
//   光と影は _TexelLighting=1 (既定) の時「ドットの中心」で計算する = 1つのドットの中で明るさが割れない (影の縁もドットの格子に揃う)
//
// 絵の貼り方
//   (a) キーワード無し = 道の座標で1軸だけ選んで投影 (3軸の混ぜはしない)。道の座標 = 世界の xz を _PathYaw (既定 −22°＝Stage.PathYaw) だけ逆に回した (t, s)。
//       面の法線の向きでいちばん近い軸を選ぶ: 天面 (y) は (t, s)、奥/手前の面 (s) は (±t, y)、横の面 (t) は (±s, y)。外から見て左右が反転しない符号にしてある。
//       _Albedo・_Normal は Texture2DArray (1枚 = 1タイル。既定 64×64 = 2.56 unit。_TexelsPerUnit=25 で1ドットが世界の 0.04)。
//       _MatSlices = (x 横の面の最初の段, y 種の数, z 横の面の回し方, w 天面の回し方)。回し方 0 = なし・1 = 左右の反転だけ (草)・2 = 90° ずつ4通り (土・岩の天面)。
//       タイルごとに種 (x〜x+y−1) と回し方を乱数で選ぶ。_TopSlice = 天面 (苔) の最初の段 (負なら横と同じ = 苔なし)。
//       _TopCount = 天面の種の数 (0 以下なら横と同じ y)。
//       天面の苔: 天面へ投影される面のうち、法線の y が _TopThreshold 付近 (幅 _TopBlend) で、ドットごとの閾値 (ディザ) で苔 ↔ 横の材質を段々に切り替える。
//       頂点色の a を掛ける (0 = 苔を生やさない = 天面にも横の材質が出る。例: 地面の材質を「横 = 土・天面 = 草」にして、道の所だけ a=0 にすると土の道になり、
//       a の中間は境がドット単位のディザになる)。_TopNoise = 閾値を大きい斑で揺らす量 (斜めの面の境だけが揺れ、平らな天面は揺れない)。
//   (b) _UV_MESH = メッシュの UV (半立体・幹の筒・櫓)。_MeshArraySlice < 0 なら _BaseMap (Bilinear・ミップあり) を、
//       0 以上なら _Albedo の配列のその段から (UV はタイル単位・_BaseMap_ST が掛かる・種と回し方は _MatSlices.y/z) 読む。苔は無し。
//   どちらも「ドットの縁だけを1画素なじませる」読み方 (HD2D_FatTexel)＋ミップ (勾配つき)。配列と _BaseMap は双線形で作ること (Point だと縁のなじみが消えるだけ)。
//   _AlbedoDecode: 0 (既定) = 色の配列は sRGB (linear:false) で作った。1 = linear:true で作って sRGB の色を入れた → Linear の色空間でだけ sRGB→線形に戻す
//     (Gamma では何もしない)。_BaseMap (メッシュの UV) は sRGB で作ること (戻さない)。
//   _NormalArrayOn=1 (既定): _Normal (linear:true・tex×2−1 でほどく・x=右・y=上) を同じタイル・同じ回し方でドットの中心で読む。強さ _NormalStrength。
//     配列が無い・真っ黒なら平らとみなす。読まない時は 0。
//   頂点色の rgb = 凹みの暗さ (AO)。_VColorAO で効かせる量 (0〜1)。法線は面ごとに平ら (flat) にすること (投影の軸が三角形の中で変わらないため)。
// 切り替え: multi_compile_local の _UV_MESH・_ALPHATEST_ON (アルファで切る。MSAA の時は _AlphaToMask=1 で alpha-to-coverage) と、URP のパイプラインのキーワード
//   (Lit.shader 17.6 から、ここで使う物を写す)。shader_feature は使わない (ビルドで版が削られる)。
// パス: Forward・ShadowCaster・DepthOnly・DepthNormals。影と深度のパスが読むアルファは _BaseMap (メッシュの UV) だけ = 配列のタイルは不透明の前提。
Shader "DeckRogue/StageModule"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap ("Texture (mesh UV)", 2D) = "white" {}
        _Albedo ("Albedo (array)", 2DArray) = "" {}
        _Normal ("Normal (array)", 2DArray) = "" {}
        _TexelsPerUnit ("Texels Per Unit", Float) = 25
        _TileTexels ("Tile Texels (fallback)", Float) = 64
        _MatSlices ("Material Slices (side base, count, side rot, top rot)", Vector) = (0,1,0,0)
        _TopSlice ("Top Slice", Float) = 0
        _TopCount ("Top Count (0 = same as side)", Float) = 0
        _TopThreshold ("Top Threshold", Float) = 0.65
        _TopBlend ("Top Blend", Float) = 0.15
        _TopNoise ("Top Noise", Float) = 0.25
        _VColorAO ("Vertex Color AO", Float) = 1
        _NormalStrength ("Normal Strength", Float) = 0.6
        _NormalArrayOn ("Normal Array On", Float) = 1
        _AlbedoDecode ("Albedo Array Is sRGB In Linear", Float) = 0
        _MeshArraySlice ("Mesh UV Array Slice", Float) = -1
        _PathYaw ("Path Yaw (deg)", Float) = -22
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.4
        _Receive ("Receive (direct light)", Range(0,1)) = 0.8
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 1
        _TexelLighting ("Texel Lighting", Float) = 1
        _Fog ("Fog", Range(0,1)) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        [HideInInspector] _AlphaToMask ("Alpha To Mask", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D_ARRAY(_Albedo); SAMPLER(sampler_Albedo);
        TEXTURE2D_ARRAY(_Normal); SAMPLER(sampler_Normal);
        // 全部のパスで同じ並び (SRP Batcher)
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseMap_TexelSize;
            float4 _Albedo_TexelSize;
            half4 _BaseColor;
            float _TexelsPerUnit;
            float4 _MatSlices;
            float _TopSlice, _TopThreshold, _TopBlend, _VColorAO, _NormalStrength, _Cutoff, _Receive, _ShadowStrength, _TexelLighting;
            float _TileTexels, _TopNoise, _NormalArrayOn, _AlbedoDecode, _MeshArraySlice, _PathYaw, _Fog, _Cull, _AlphaToMask, _TopCount;
        CBUFFER_END

        // 影と深度のパスのアルファ (メッシュの UV の _BaseMap だけ。配列のタイルは不透明の前提)
        half HD2D_ModuleAlpha(float2 uv)
        {
            half a = _BaseColor.a;
        #if defined(_UV_MESH)
            if (_MeshArraySlice < 0.0) a *= SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a;
        #endif
            return a;
        }
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull [_Cull]
            AlphaToMask [_AlphaToMask]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ _UV_MESH
            #pragma multi_compile_local _ _ALPHATEST_ON
            // URP のパイプラインのキーワード (Lit.shader 17.6 から、ここで使う物を写す)
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Include/HD2DPixel.hlsl"

            // 見本の霧の形 (2026-09-30 W3 P22)。StageLook が全体値で書き、Restore で 0 に戻す (0 = 使わない = URP の霧のまま)
            float4 _HD2DFogLobePos;      // xyz = 霧の光の芯 (世界。坑口の奥の脈)・w = 芯の絞り (cos の乗数。0 = 使わない)
            float4 _HD2DFogLobeColor;    // rgb = 芯を向いた時に霧へ足す色 (線形)・a = 芯から外れた所の霧の色の倍率 (0〜1)
            float4 _HD2DHeightFogColor;  // rgb = 高さの霧の色 (線形)・a = 濃さ
            float4 _HD2DHeightFogRange;  // x = 下の高さ・y = 上の高さ・z = 1 なら有効
            float4 _HD2DHeightFogDepth;  // x = かかり始める深さ・y = 濃さが満ちる深さ (カメラからの視線の深さ)
            // 舞台の色の寄せ (2026-09-30 W3 の統合・本家の色彩 docs/design/hd2d-slice/honke-color.md)。StageLook が look の envGrade から書く。
            // xyz = 色の倍率 − 1 (0 = そのまま)・w = 彩度を落とす量 (0 = そのまま・1 = 灰)。全部 0 なら何も変わらない (キャラの板は別のシェーダ = 掛からない)
            float4 _HD2DEnvGrade;
            // 舞台だけの周辺減光 (2026-09-30 W3b P22・ユーザー「本家っぽく」: 座席の帯が明るく、画面の端と上が沈む)。StageLook が look の stageVignette から書く。
            // キャラの板 (StageUnitLit) と UI には掛からない = 右端の敵や主人公は暗くならない。画面の位置はカメラの視線の空間で求める (描く先の上下の反転に左右されない)。
            // x = 横の減光の始まり (画面の中央からの距離 0〜0.5)・y = 横の端での強さ (0〜1)・z = 上の減光の帯の幅 (画面の上から 0〜1)・w = 上端での強さ。y と w が 0 なら何もしない
            float4 _HD2DStageVignette;

            half HD2D_StageVignette(float3 posWS)
            {
                if (_HD2DStageVignette.y <= 0.0 && _HD2DStageVignette.w <= 0.0) return 1.0h;
                float3 pv = TransformWorldToView(posWS);
                float iz = 1.0 / max(1e-3, -pv.z);
                float4x4 proj = UNITY_MATRIX_P;
                float nx = pv.x * iz * abs(proj._m00);   // −1 (左端) 〜 1 (右端)
                float ny = pv.y * iz * abs(proj._m11);   // −1 (下端) 〜 1 (上端)
                float side = smoothstep(_HD2DStageVignette.x, 0.5, abs(nx) * 0.5);
                float fromTop = 0.5 - ny * 0.5;                     // 0 (上端) 〜 1 (下端)
                float top = _HD2DStageVignette.z > 0.0 ? 1.0 - smoothstep(0.0, _HD2DStageVignette.z, fromTop) : 0.0;
                return half(saturate((1.0 - _HD2DStageVignette.y * side) * (1.0 - _HD2DStageVignette.w * top)));
            }

            half3 HD2D_EnvGrade(half3 c)
            {
                half g = Luminance(c);
                c = lerp(c, half3(g, g, g), half(saturate(_HD2DEnvGrade.w)));
                return c * max(half3(0.0h, 0.0h, 0.0h), 1.0h + half3(_HD2DEnvGrade.xyz));
            }

            // 霧の光の芯を向くほど 1 (視線と芯の向きの cos の w 乗)。使わない時は 1
            half HD2D_FogLobe(float3 posWS)
            {
                if (_HD2DFogLobePos.w <= 0.0) return 1.0h;
                float3 v = SafeNormalize(posWS - _WorldSpaceCameraPos);
                float3 l = SafeNormalize(_HD2DFogLobePos.xyz - _WorldSpaceCameraPos);
                return half(pow(saturate(dot(v, l)), _HD2DFogLobePos.w));
            }

            // 霧の色: 芯を向くほど脈の光で明るく、外れるほど暗い (中央が明るく端が暗い夜の霧)。使わない時は URP の霧の色
            half3 HD2D_FogColor(half lobe)
            {
                half3 fc = unity_FogColor.rgb;
                if (_HD2DFogLobePos.w <= 0.0) return fc;
                return fc * lerp(half(_HD2DFogLobeColor.a), 1.0h, lobe) + half3(_HD2DFogLobeColor.rgb) * lobe;
            }

            // 高さの霧: 低い所 (下〜上の高さ) に、深さ x〜y で満ちる霧。色は芯の向きで同じく明暗が付く。使わない時は元の色のまま
            half3 HD2D_HeightFog(half3 col, float3 posWS, half lobe)
            {
                if (_HD2DHeightFogRange.z < 0.5 || _HD2DHeightFogColor.a <= 0.0) return col;
                half h = half(saturate((_HD2DHeightFogRange.y - posWS.y) / max(_HD2DHeightFogRange.y - _HD2DHeightFogRange.x, 1e-3)));
                float depth = -TransformWorldToView(posWS).z;
                half dd = half(saturate((depth - _HD2DHeightFogDepth.x) / max(_HD2DHeightFogDepth.y - _HD2DHeightFogDepth.x, 1e-3)));
                half amt = saturate(half(_HD2DHeightFogColor.a) * h * dd);
                half3 hc = half3(_HD2DHeightFogColor.rgb);
                if (_HD2DFogLobePos.w > 0.0) hc *= lerp(half(_HD2DFogLobeColor.a), 1.0h, lobe);
                return lerp(col, hc, amt);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half4 color : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half3 vertexLight : TEXCOORD5;
            #endif
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
                VertexNormalInputs n = GetVertexNormalInputs(i.normalOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.color = i.color;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                o.vertexLight = VertexLighting(p.positionWS, n.normalWS);
            #endif
                return o;
            }

            // 1枚のタイルの読み方 (配列)。pT = タイル単位の連続した座標、seedAxis = 面の軸ごとに乱数をずらす値。
            // 出力: uvS = 縁をなじませた読む座標、uvC = ドットの中心、gx/gy = 勾配 (ミップ)、M = 回し方、slice = 段
            void HD2D_TileSample(float2 pT, float size, float baseSlice, float count, float rotMode, float seedAxis,
                                 out float2 uvS, out float2 uvC, out float2 gx, out float2 gy, out float2x2 M, out float slice)
            {
                float2 tile = floor(pT);
                float r = HD2D_Hash21(tile + seedAxis * 57.0);
                float rv = HD2D_Hash21(tile * 1.7 + seedAxis * 13.0 + 31.0);
                M = HD2D_TileXform(rotMode, r);
                slice = baseSlice + min(floor(rv * max(count, 1.0)), max(count, 1.0) - 1.0);
                float2 q = mul(M, frac(pT) - 0.5) + 0.5;
                float2 dpx = ddx(pT), dpy = ddy(pT);
                gx = mul(M, dpx); gy = mul(M, dpy);
                float2 fw = (abs(gx) + abs(gy)) * size;
                float2 fat = HD2D_FatTexelW(q * size, fw);
                // タイルの外 (隣のタイルは別の種と回し方) を混ぜない
                fat = clamp(fat, 0.5, size - 0.5);
                uvS = fat / size;
                uvC = clamp(floor(q * size) + 0.5, 0.5, size - 0.5) / size;
            }

            void Frag(Varyings i
                , out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 N = SafeNormalize(i.normalWS);
                float3 posWS = i.positionWS;
                float arraySize = _Albedo_TexelSize.z > 0.5 ? _Albedo_TexelSize.z : _TileTexels;
                half4 albedo;
                half3 nPerturbed = half3(N);
                float2 texelForShift;

            #if defined(_UV_MESH)
                // (b) メッシュの UV
                float2 baseSize = max(_BaseMap_TexelSize.zw, float2(1, 1));
                float2 texB = i.uv * baseSize;
                float2 fatB = HD2D_FatTexel(texB);
                float2 gxB = ddx(i.uv), gyB = ddy(i.uv);
                float2 uvS, uvC, gx, gy; float2x2 M; float slice;
                HD2D_TileSample(i.uv, arraySize, max(_MeshArraySlice, 0.0), _MatSlices.y, _MatSlices.z, 3.0, uvS, uvC, gx, gy, M, slice);
                bool useArray = _MeshArraySlice >= 0.0;
                texelForShift = useArray ? i.uv * arraySize : texB;
                float3 shift = HD2D_TexelShift(texelForShift, posWS);
                if (useArray)
                {
                    albedo = SAMPLE_TEXTURE2D_ARRAY_GRAD(_Albedo, sampler_Albedo, uvS, slice, gx, gy);
                    albedo.rgb = HD2D_DecodeAlbedo(albedo.rgb, _AlbedoDecode);
                }
                else
                {
                    albedo = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, fatB / baseSize, gxB, gyB);
                }
            #else
                // (a) 道の座標で1軸だけ選んで投影
                float yaw = radians(_PathYaw);
                float cs = cos(yaw), sn = sin(yaw);
                float3 Tw = float3(cs, 0.0, -sn);   // 道の座標 t の向き (世界)
                float3 Sw = float3(sn, 0.0, cs);    // s (奥) の向き
                float t = dot(posWS, Tw), s = dot(posWS, Sw);
                float nt = dot(N, Tw), ns = dot(N, Sw), ny = N.y;
                float at = abs(nt), as_ = abs(ns), ay = abs(ny);
                bool top = ay >= at && ay >= as_;
                bool sSide = !top && as_ >= at;
                float sgnS = ns < 0.0 ? 1.0 : -1.0;     // 手前を向く面 (−s) は u = +t
                float sgnT = nt > 0.0 ? 1.0 : -1.0;     // +t を向く面は u = +s
                float2 p = top ? float2(t, s) : (sSide ? float2(t * sgnS, posWS.y) : float2(s * sgnT, posWS.y));
                float3 Uw = top ? Tw : (sSide ? Tw * sgnS : Sw * sgnT);
                float3 Vw = top ? Sw : float3(0.0, 1.0, 0.0);
                float axisSeed = top ? 0.0 : (sSide ? 1.0 : 2.0);
                float tileUnits = arraySize / max(_TexelsPerUnit, 1e-3);
                float2 pT = p / tileUnits;
                texelForShift = p * _TexelsPerUnit;
                float3 shift = HD2D_TexelShift(texelForShift, posWS);

                // 苔: 天面へ投影される上向きの面で、ドットごとの閾値で段々に切り替える
                // 大きい斑は閾値のほうを揺らす (平らな天面は揺れの外 = 全部苔のまま。斜めの面の境だけが揺れる)
                float thr = _TopThreshold + (HD2D_ValueNoise(p * 0.35) - 0.5) * _TopNoise;
                float cover = saturate((ny - thr) / max(_TopBlend, 1e-3) + 0.5) * i.color.a;
                float dotN = HD2D_DotNoise(floor(texelForShift) + axisSeed * 331.0);
                bool moss = top && ny > 0.0 && cover > dotN;
                bool useTop = moss && _TopSlice >= 0.0;
                float baseSlice = useTop ? _TopSlice : _MatSlices.x;
                float count = (useTop && _TopCount > 0.5) ? _TopCount : _MatSlices.y;
                float rotMode = useTop ? _MatSlices.w : _MatSlices.z;

                float2 uvS, uvC, gx, gy; float2x2 M; float slice;
                HD2D_TileSample(pT, arraySize, baseSlice, count, rotMode, axisSeed, uvS, uvC, gx, gy, M, slice);
                albedo = SAMPLE_TEXTURE2D_ARRAY_GRAD(_Albedo, sampler_Albedo, uvS, slice, gx, gy);
                albedo.rgb = HD2D_DecodeAlbedo(albedo.rgb, _AlbedoDecode);

                // 法線 (配列) をドットの中心で読む。回し方は Mᵀ で面へ戻す
                if (_NormalArrayOn > 0.5)
                {
                    half3 nt3 = HD2D_DecodeNormal(SAMPLE_TEXTURE2D_ARRAY_GRAD(_Normal, sampler_Normal, uvC, slice, gx, gy), half(_NormalStrength));
                    float2 nxy = mul(transpose(M), float2(nt3.xy));
                    float3 T = SafeNormalize(Uw - N * dot(N, Uw));
                    float3 B = SafeNormalize(Vw - N * dot(N, Vw) - T * dot(T, Vw));
                    nPerturbed = half3(SafeNormalize(T * nxy.x + B * nxy.y + N * nt3.z));
                }
            #endif

                half alpha = AlphaDiscard(albedo.a * _BaseColor.a, _Cutoff);
                half3 col = HD2D_EnvGrade(albedo.rgb * _BaseColor.rgb);   // 本家の色彩への寄せ (全体値 0 = そのまま)
                col *= lerp(half3(1.0h, 1.0h, 1.0h), i.color.rgb, half(_VColorAO));

                // 光の位置 (ドットの中心)
                float3 posL = _TexelLighting > 0.5 ? posWS + shift : posWS;
                InputData inputData = (InputData)0;
                inputData.positionWS = posL;
                inputData.normalWS = nPerturbed;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(posWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
            #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(posL);
            #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
            #endif
                half4 shadowMask = CalculateShadowMask(inputData);
                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, 1.0h);
                uint meshRenderingLayers = GetMeshRenderingLayer();

                half3 amb = max(half3(0.0h, 0.0h, 0.0h), half3(SampleSH(nPerturbed))) * aoFactor.indirectAmbientOcclusion;
                half3 direct = 0;
                half3 lost = 0;

                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
            #endif
                {
                    half3 d = mainLight.color * (mainLight.distanceAttenuation * saturate(dot(nPerturbed, mainLight.direction)));
                    direct += d;
                    lost += d * (1.0h - mainLight.shadowAttenuation);
                }

            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
            #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint li = 0; li < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); li++)
                {
                    Light dl = GetAdditionalLight(li, inputData, shadowMask, aoFactor);
                #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(dl.layerMask, meshRenderingLayers))
                #endif
                    {
                        half3 d = dl.color * (dl.distanceAttenuation * saturate(dot(nPerturbed, dl.direction)));
                        direct += d;
                        lost += d * (1.0h - dl.shadowAttenuation);
                    }
                }
            #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light al = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(al.layerMask, meshRenderingLayers))
                #endif
                    {
                        half3 d = al.color * (al.distanceAttenuation * saturate(dot(nPerturbed, al.direction)));
                        direct += d;
                        lost += d * (1.0h - al.shadowAttenuation);
                    }
                LIGHT_LOOP_END
            #endif
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                direct += i.vertexLight;
            #endif

                half fullL = Luminance(amb + direct);
                half lostL = Luminance(lost);
                half darken = 1.0h - half(_ShadowStrength) * saturate(lostL / max(fullL, 1e-4h));
                col = col * (amb + half(_Receive) * direct) * darken;

                // 霧 (W3 P22): 高さの霧 → 距離の霧。距離の霧の色は芯の向きで明暗が付く (全体値が 0 なら URP の MixFog と同じ)
                half lobe = HD2D_FogLobe(posWS);
                half3 hazed = HD2D_HeightFog(col, posWS, lobe);
                half3 fogged = MixFogColor(hazed, HD2D_FogColor(lobe), InitializeInputDataFog(float4(posWS, 1.0), i.fogFactor));
                col = lerp(col, fogged, half(_Fog));
                col *= HD2D_StageVignette(posWS);   // 舞台だけの周辺減光 (W3b P22。全体値 0 = そのまま)
                outColor = half4(col, OutputAlpha(alpha, false));
            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
            #endif
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
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_local _ _UV_MESH
            #pragma multi_compile_local _ _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            Varyings ShadowVert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                float3 positionWS = TransformObjectToWorld(i.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(i.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                o.positionCS = ApplyShadowClamping(positionCS);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                return o;
            }
            half4 ShadowFrag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
            #if defined(_ALPHATEST_ON)
                clip(HD2D_ModuleAlpha(i.uv) - _Cutoff);
            #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_local _ _UV_MESH
            #pragma multi_compile_local _ _ALPHATEST_ON
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            Varyings DepthVert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                return o;
            }
            half DepthFrag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            #if defined(_ALPHATEST_ON)
                clip(HD2D_ModuleAlpha(i.uv) - _Cutoff);
            #endif
                return i.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DNVert
            #pragma fragment DNFrag
            #pragma multi_compile_local _ _UV_MESH
            #pragma multi_compile_local _ _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            Varyings DNVert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                return o;
            }
            void DNFrag(Varyings i
                , out half4 outNormalWS : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            #if defined(_ALPHATEST_ON)
                clip(HD2D_ModuleAlpha(i.uv) - _Cutoff);
            #endif
            #if defined(_GBUFFER_NORMALS_OCT)
                float3 n = normalize(i.normalWS);
                float2 oct = PackNormalOctQuadEncode(n);
                outNormalWS = half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0.0);
            #else
                outNormalWS = half4(NormalizeNormalPerPixel(i.normalWS), 0.0);
            #endif
            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
            #endif
            }
            ENDHLSL
        }
    }
}
