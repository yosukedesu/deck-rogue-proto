// StageUnitLit — HD-2D 見本のキャラの板 (光を受ける板) 用 (2026-09-30 P03。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-4・§2-5)。
// 今の StageUnit (アンリット) の代わりに、litunits=1 の時に P11 が材質をこれに差し替える (旗が既定のままなら、どこからも使われない)。
//
// 光の形 (本家の作法: ドットには陰影が描き込み済み。写実的な光は当てず、全員に同じ固定のキーライトだけ)
//   lit  = 上下の環境光 × _AmbientScale ＋ _Receive × (キーの色 × max(0, 法線・キー) × 月の影 × 木漏れ日 ＋ 近くの点光源 × _LocalLights)
//   色   = 絵の色 × lit × _HeroLift       … _Receive=0 で「絵の色 × 環境光」の平らな色に戻る (光の戻しのスイッチ。計画 §2-5・§7-7)
//   順序 = 光 → 主役の持ち上げ → 暗い色の持ち上げ → 輪郭の床 → 輪郭の持ち上げ → 白の上限 → 発光 → 鮮やかさ → リム → キャラの色の掛け算 → 点滅 → 霧
//   キー・環境光は全体値 (Include/HD2DCharLight.hlsl の _CharKeyDir・_CharKeyColor・_CharAmbTop・_CharAmbBottom。StageLook.Apply が書く)。
//   全体値が未設定 (0) の時は場の主光 (月) と環境光 (SH) を使う = 黒くならない。
//   法線と光の位置は「ドットの中心」で読む/計算する (1つのドットの中で明るさが割れない)。
// 材質の値 (P11 が書く)
//   _BaseMap/_BaseColor … 絵とその色 (Img.color = 生死・点滅の色)。_Cutoff … アルファで切る (キャラの板に AA は掛けない。中のドットはぼけない)
//   _NormalMap + _HasNormal=1 … コマごとの法線 (*_n.png。sRGB なし・Point)。向きは x=右・y=上・z=手前 (OpenGL 形)。y が逆 (DirectX 形) の絵なら _NormalYSign=-1
//   _EmissionMap + _HasEmission=1 … 発光マスク (*_e.png。光る画素 = 元の色・ほか = 黒。sRGB・Point)。黒でない所 (最大チャンネル×8 で 0〜1) は
//     環境の暗さを受けず、絵の色 × _EmissionIntensity (既定 1.6 = 今の「灯りの目 ×1.6」と同じ。ブルームに乗る) へ寄る
//   _KeyFlip=1 … 描き込まれた光が右から来る絵 (art-lint の測った向き。2026-09-30 P23: 「画像ファイルを左右反転した絵」の表は16枚中9枚が左からの光だったので、
//     反転の有無でなく測った向きで選ぶ = StageUnits の CharExtras)。キーの左右を絵の描き込みに合わせる
//   _Receive … 受光率 (既定 0.6。主役 0.5〜0.6 から)。_AmbientScale … 環境光の倍率。_HeroLift … 主役の持ち上げ (全体の倍率。既定 1)
//   _LocalLights … 技の光など近くの点光源をどれだけ受けるか (0 = 受けない。影は受けない = 自分の影で汚れない)
//   _ReceiveShadows … 月の影 (地形・大物が落とす) を受ける割合 (既定 0 = 受けない)。_CookieOnKey … 月の木漏れ日のクッキーをキーに掛ける割合 (既定 1)
//   _OutlineFloor … 光で暗くしても、この色より暗くしない (ただし元の絵がそれより暗い所は元の絵のまま) = 輪郭の墨を夜に潰さない
//   _BlackLift … 輪郭の持ち上げ (2026-09-30 P23。計画 P23 手順2)。xyz = 真っ黒の画素がなる色 (線形・後処理の前)。col = L + col×(1−L) で、
//     順序と明るい所はほぼそのまま、墨の輪郭と黒鉄の暗部だけを持ち上げる (ACES の足が暗部を 1/3 に沈めるので、墨線が画面で 4〜13 に潰れていた。
//     目標は画面で 20〜30 = トライアングルストラテジーの輪郭 RGB 24)。StageUnits がトーンマップと露出で割り戻して SetVector で書く。既定 0 = 何もしない
//     2周目 (本家っぽく): StageUnits が「画面の輪郭の暗さ」(look の char の outlineTarget) から今の後処理 (露出・コントラスト・colorFilter・LGG・トーンマップ・周辺減光) を
//     逆にたどって絵ごとに求める = 舞台 (P22) が後処理を変えても輪郭は目標の暗さのまま
//   _ShadeLift … 暗い色の持ち上げ (2026-09-30 P23 2周目)。x = 最大の倍率 −1 (gain)・y = knee (線形の明るさ)。倍率 = 1 + gain × (knee/(knee+Y))²。既定 0 = 何もしない
//   _CharTint … キャラの色の掛け算 (2026-09-30 P23 2周目)。リムの後・点滅の前に掛ける。後処理の colorFilter の打ち消し × 暖かさ。既定 (1,1,1) = そのまま
//   _CharSat … キャラの鮮やかさ (2026-10-01 二周目 レーン E の 4。char C9・N16)。発光の後・リムの前に col = 輝度 + (col − 輝度) × _CharSat (輝度は変えない・負は 0 で止める)。
//     発光の後なので狼の白い毛 (発光) の暖かさ (b*) も同じ割合で保つ。既定 1 = そのまま (W5)。StageUnits が look の char の saturation (art ごとの上書きあり) を書く
//   _WhiteCap … 1 未満なら、発光以外の出力の明るさ Y を、上限の 0.6 倍から上限へ漸近する柔らかい肩で縮める (色相は保つ。2026-10-01 直しの輪1。旧はチャンネルごとの頭打ち)。既定 1 = 使わない
//   _Flash (被弾の白)・_Dissolve (撃破の崩れ)・_Rim (右上の縁の1ドット)・_Fog (霧を受ける割合) は StageUnit と同じ式
// パス: Forward・ShadowCaster (舞台の灯の影。Cull Off)・DepthOnly・DepthNormals (SSAO とぼかしの深度にキャラを載せる)
// 切り替え: URP のパイプラインのキーワードだけ (multi_compile。Lit.shader から写した物のうち、ここで使う物)。自前のキーワードは無く、float の分岐だけ。
Shader "DeckRogue/StageUnitLit"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
        [NoScaleOffset] _NormalMap ("Normal Map (x=right y=up)", 2D) = "bump" {}
        [NoScaleOffset] _EmissionMap ("Emission Mask", 2D) = "black" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.4
        _Flash ("Flash", Range(0,1)) = 0
        _Dissolve ("Dissolve", Range(0,1)) = 0
        _Fog ("Fog", Range(0,1)) = 1
        _Rim ("Rim Light", Float) = 0.2
        _RimColor ("Rim Color", Color) = (0.78,0.82,0.95,1)
        _Receive ("Receive", Range(0,1)) = 0.6
        _AmbientScale ("Ambient Scale", Float) = 1
        _HeroLift ("Hero Lift", Float) = 1
        _KeyFlip ("Key Flip (mirrored art)", Float) = 0
        _LocalLights ("Local Lights", Float) = 0
        _OutlineFloor ("Outline Floor", Color) = (0.094,0.086,0.118,1)
        _WhiteCap ("White Cap", Float) = 1
        _BlackLift ("Black Lift (linear rgb pure black becomes)", Vector) = (0,0,0,0)
        _ShadeLift ("Shade Lift (x gain, y knee linear)", Vector) = (0,0,0,0)
        _CharTint ("Char Tint (linear rgb multiplier)", Vector) = (1,1,1,0)
        _CharSat ("Char Saturation", Float) = 1
        _HasNormal ("Has Normal", Float) = 0
        _HasEmission ("Has Emission", Float) = 0
        _EmissionIntensity ("Emission Intensity", Float) = 1.6
        _NormalYSign ("Normal Y Sign", Float) = 1
        _ReceiveShadows ("Receive Moon Shadows", Range(0,1)) = 0
        _CookieOnKey ("Cookie On Key", Range(0,1)) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
        TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
        // 全部のパスで同じ並び (SRP Batcher)
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseMap_TexelSize;
            half4 _BaseColor;
            half _Cutoff, _Flash, _Dissolve, _Fog, _Rim;
            half4 _RimColor;
            half _Receive, _AmbientScale, _HeroLift, _KeyFlip, _LocalLights;
            half4 _OutlineFloor;
            half _WhiteCap, _HasNormal, _HasEmission, _Cull;
            half _EmissionIntensity, _NormalYSign, _ReceiveShadows, _CookieOnKey;
            half4 _BlackLift;
            half4 _ShadeLift, _CharTint;
            half _CharSat;
        CBUFFER_END

        // 撃破の崩れ (StageUnit と同じ式): ドット単位の乱数で消えていく。頭 (上) から先に、足元は最後
        void HD2D_UnitDissolve(float2 uv, float2 uv0)
        {
            if (_Dissolve > 0.0)
            {
                float2 px = floor(uv * _BaseMap_TexelSize.zw);
                float n = frac(sin(dot(px, float2(12.9898, 78.233))) * 43758.5453);
                clip(n * 0.6 + (1.0 - uv0.y) * 0.4 - _Dissolve * 1.02);
            }
        }
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            // URP のパイプラインのキーワード (Lit.shader 17.6 から、ここで使う物だけを写す)
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Include/HD2DPixel.hlsl"
            #include "Include/HD2DCharLight.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float2 uv0 : TEXCOORD3;
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
                o.uv0 = i.uv;
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
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
                float2 size = _BaseMap_TexelSize.zw;
                float2 texel = i.uv * size;
                // ドットの中心 (光と影と法線はここで読む)。微分は分岐より前に取る
                float3 shift = HD2D_TexelShift(texel, i.positionWS);
                float2 uvC = HD2D_TexelCenter(texel) / max(size, float2(1, 1));

                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                clip(c.a - _Cutoff);
                HD2D_UnitDissolve(i.uv, i.uv0);
                half3 albedo = c.rgb;
                float3 posC = i.positionWS + shift;

                // 板の向き (世界): 絵の右 = 物体の +X、上 = +Y、見る人の方 = 物体の −Z (Stage の板の四角形の向き)
                float4x4 m = GetObjectToWorldMatrix();
                half3 R = half3(SafeNormalize(float3(m._m00, m._m10, m._m20)));
                half3 U = half3(SafeNormalize(float3(m._m01, m._m11, m._m21)));
                half3 F = half3(TransformObjectToWorldNormal(float3(0, 0, -1)));

                // 法線 (板の接空間: x=右・y=上・z=見る人の方) をドットの中心で読む
                half3 nS = half3(0.0h, 0.0h, 1.0h);
                if (_HasNormal > 0.5h)
                {
                    nS = HD2D_DecodeNormal(SAMPLE_TEXTURE2D_LOD(_NormalMap, sampler_NormalMap, uvC, 0), 1.0h);
                    nS.y *= _NormalYSign;
                }
                half3 nW = normalize(R * nS.x + U * nS.y + F * nS.z);

                // 固定のキーライト (全体値。未設定なら月)
                half3 toKey, keyColor;
                HD2D_CharKey(toKey, keyColor);
                half3 keyS = HD2D_ToSprite(toKey, R, U, F, _KeyFlip);
                half ndl = saturate(dot(nS, keyS));
                half keyMask = 1.0h;
            #if defined(_LIGHT_COOKIES)
                keyMask *= lerp(1.0h, Luminance(half3(SampleMainLightCookie(posC))), _CookieOnKey);
            #endif
                if (_ReceiveShadows > 0.0h)
                {
                    float4 sc = TransformWorldToShadowCoord(posC);
                    half sh = MainLightShadow(sc, posC, half4(1, 1, 1, 1), _MainLightOcclusionProbes);
                    keyMask *= lerp(1.0h, sh, _ReceiveShadows);
                }
                half3 direct = keyColor * (ndl * keyMask);

                // 近くの点光源 (技の光・逆光)。影は受けない (板が自分に影を落とさないため)
            #if defined(_ADDITIONAL_LIGHTS)
                if (_LocalLights > 0.0h)
                {
                    half3 local = 0;
                    InputData inputData = (InputData)0;
                    inputData.positionWS = posC;
                    inputData.normalWS = nW;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                    uint meshRenderingLayers = GetMeshRenderingLayer();
                    uint pixelLightCount = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint li = 0; li < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); li++)
                    {
                        Light dl = GetAdditionalLight(li, posC);
                    #ifdef _LIGHT_LAYERS
                        if (IsMatchingLightLayer(dl.layerMask, meshRenderingLayers))
                    #endif
                        local += dl.color * (dl.distanceAttenuation * saturate(dot(nW, dl.direction)));
                    }
                #endif
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light al = GetAdditionalLight(lightIndex, posC);
                    #ifdef _LIGHT_LAYERS
                        if (IsMatchingLightLayer(al.layerMask, meshRenderingLayers))
                    #endif
                        local += al.color * (al.distanceAttenuation * saturate(dot(nW, al.direction)));
                    LIGHT_LOOP_END
                    direct += local * _LocalLights;
                }
            #endif

                half3 lit = HD2D_CharAmbient(nW) * _AmbientScale + _Receive * direct;
                half3 col = albedo * lit * _HeroLift;
                // 暗い色の持ち上げ (2026-09-30 P23 2周目): 明るさ Y が knee より暗い色ほど強く (最大 1+gain 倍) 上げ、明るい色はほぼそのまま。
                // 色相は変えない (3つのチャンネルに同じ倍率)。絵の暗い黒鉄の衣が ACES の足で潰れるのを戻す (このはだけ。look の char の art の shadeLift)
                if (_ShadeLift.x > 0.0h)
                {
                    half sy = dot(col, half3(0.2126h, 0.7152h, 0.0722h));
                    half sk = max(_ShadeLift.y, 1e-3h);
                    half sq = sk / (sk + max(sy, 0.0h));
                    col *= 1.0h + _ShadeLift.x * sq * sq;
                }
                // 輪郭の持ち上げ: 光で暗くしても min(元の絵, _OutlineFloor) より暗くしない
                col = max(col, min(albedo, _OutlineFloor.rgb));
                // 輪郭の持ち上げ (黒の持ち上げ): 真っ黒 → _BlackLift、白 → 白のまま。暗いほど多く上がる (順序は変えない)。既定 0 = そのまま
                half3 lift = saturate(_BlackLift.rgb);
                col = lift + col * (1.0h - lift);
                // 白の上限 (直しの輪1 2026-10-01): 色相を保つ柔らかい肩 = 明るさ Y が knee (上限の 0.6) を超えたら、上限へ漸近するように3つを同じ倍率で縮める。
                // 白い所 (肌の光・白い衣) だけが縮み、色の濃い所 (橙の髪・肌の中間) は縮まない (いちばん明るいチャンネルで測ると橙が先に縮んで体の中央値が 135→99 に落ちた)。
                // 旧はチャンネルごとの min (肌が灰色になる) だった。使う look は二周目の見本の char.whiteCap だけ (W5 の写し・今の舞台は既定 1 = 何もしない)
                if (_WhiteCap < 0.999h)
                {
                    half cap = max(_WhiteCap, 1e-3h), knee = cap * 0.6h;
                    half m = dot(col, half3(0.2126h, 0.7152h, 0.0722h));
                    if (m > knee)
                    {
                        half over = m - knee;
                        half m2 = knee + over / (1.0h + over / max(cap - knee, 1e-3h));
                        col *= m2 / m;
                    }
                }
                // 発光: 光る所は暗さを受けず、絵の色 × 強さ へ
                if (_HasEmission > 0.5h)
                {
                    half3 e = SAMPLE_TEXTURE2D_LOD(_EmissionMap, sampler_EmissionMap, uvC, 0).rgb;
                    // P05 の _e は「光る画素 = 元の色・ほか = 黒」。黒でなければほぼ全部を光らせる (暗い色の光る画素も落とさない)
                    half em = saturate(max(e.r, max(e.g, e.b)) * 8.0h);
                    col = lerp(col, albedo * _EmissionIntensity, em);
                }
                // 鮮やかさ (2026-10-01 二周目 レーン E の 4): 輝度を保って彩度だけ _CharSat 倍 (発光の後 = 白い毛の暖かさも同じ割合)。既定 1 = そのまま
                if (abs(_CharSat - 1.0h) > 1e-3h)
                {
                    half sl = dot(col, half3(0.2126h, 0.7152h, 0.0722h));
                    col = max(half3(0.0h, 0.0h, 0.0h), sl + (col - sl) * _CharSat);
                }
                // リム (StageUnit と同じ): 右上の隣のドットが透明なら縁を淡く光らせる
                if (_Rim > 0.0h)
                {
                    float2 tx = _BaseMap_TexelSize.xy;
                    half aR = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, i.uv + float2(tx.x, 0), 0).a;
                    half aU = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, i.uv + float2(0, tx.y), 0).a;
                    if (aR < _Cutoff || aU < _Cutoff) col = lerp(col, _RimColor.rgb, _Rim);
                }
                // キャラの色の掛け算 (2026-09-30 P23 2周目): 後処理の colorFilter (舞台を冷やす色の膜) をキャラだけ打ち消す割り戻し × 暖かさ。
                // 発光・リムの後に掛ける (発光の白も冷えない)。既定 (1,1,1) = そのまま。被弾の白 (_Flash) はこの後なので白のまま
                col *= _CharTint.rgb;
                col = lerp(col, half3(1, 1, 1), _Flash);
                half3 fogged = MixFog(col, InitializeInputDataFog(float4(i.positionWS, 1.0), i.fogFactor));
                col = lerp(col, fogged, _Fog);
                outColor = half4(col, 1.0h);
            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
            #endif
            }
            ENDHLSL
        }

        // 舞台の灯 (スポット) からの影。どの光の影に載るかは C# 側 (Rendering Layers とライトの影のレイヤー。キャラ = Characters)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 uv0 : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
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
                o.uv0 = i.uv;
                return o;
            }
            half4 ShadowFrag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a;
                clip(a - _Cutoff);
                HD2D_UnitDissolve(i.uv, i.uv0);
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
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 uv0 : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            Varyings DepthVert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.uv0 = i.uv;
                return o;
            }
            half DepthFrag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a;
                clip(a - _Cutoff);
                HD2D_UnitDissolve(i.uv, i.uv0);
                return i.positionCS.z;
            }
            ENDHLSL
        }

        // SSAO の法線とぼかしの深度にキャラを載せる (載せないと、SSAO が深度も DepthNormals から取る時にキャラが奥の地面の深さになり、座席のキャラがぼける)
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DNVert
            #pragma fragment DNFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 uv0 : TEXCOORD1; float3 normalWS : TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            Varyings DNVert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.uv0 = i.uv;
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
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a;
                clip(a - _Cutoff);
                HD2D_UnitDissolve(i.uv, i.uv0);
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
    FallBack "Sprites/Default"
}
