// TiltShift — HD-2D 見本の自作のぼかし (ティルトシフト) の全画面パス (2026-09-30。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-6)。
// P07 (W1): 中身を書いた。呼ぶのは Game/Render/TiltShiftPass.cs (Render Graph の5つのパス) だけ。TiltShiftSettings.Enabled が false の間は
// どのパスも走らない (W1 は見た目を変えない)。
//
// 流れ (パスの番号 = TiltShiftPass.ShaderPass)
//   0 Prefilter  : 線形の深さから錯乱円 (CoC) を作り、色と一緒に作業の解像度 (全 or 半分) へ。帯の中は 0・帯の外は smoothstep で上限へ。
//                  手前は負・奥は正 (単位は作業の解像度の px)。半解像度では 2×2 の CoC の |値| の大きい方を採り、色はそれと同じ側の点だけを
//                  明るさで重み付けして平均する (帯の中のキャラの色が奥の層に混ざらない)
//   1 TileMax    : 手前の CoC (−CoC の正の部分) をタイル (NearDilateTile px 四方) の最大値に
//   2 TileDilate : タイルの 3×3 の最大値 = 手前のにじみ出しの半径 (隣のタイルへも滲む)
//   3 Gather     : ゴールデンアングルの円盤 (Taps 点) で奥と手前を別々にぼかす (MRT: 0 = 奥・1 = 手前)。
//                  奥は「帯の中の点」と「自分より手前の点」を拾わない (座席のキャラの色が背景に滲まない)。
//                  手前は「その点のぼけの円が中心まで届くか」で拾い、届いた割合を被覆率 (α) にする = 手前の葉が縁の外へ滲み出す
//   4 Composite  : 全解像度で合成。元の色 → 奥の層 (錯乱円に応じて) → 手前の層 (α で上に)。帯の中の画素は元の色をそのまま返す (ドットが崩れない)
//   5 Copy       : 入力を写すだけ (骨組みの名残・調べ物用)
//
// P24 (W3) の詰め
//   - ピントの帯を「道に沿った帯」にできる (_TS_Focus.w = 1)。画素の世界の点の道の座標 s (道と直角・+ が奥) が _TS_Focus.x〜y ならぼかし 0。
//     s = _TS_Plane.w + 深さ × (_TS_Plane.x r.x + _TS_Plane.y r.y + _TS_Plane.z)、r = ((ndc + _TS_Proj.zw) × _TS_Proj.xy, 1) (カメラの空間の視線)。
//     帯の外の距離 (s の差) を視線に沿った深さの差に直して (÷ (n·視線))、今までと同じ傾斜 (_TS_Band.z・w) で上限へ。焦点面を座席の列に沿って傾けた形。
//   - 手前の層: 自分が手前の画素は、自分の錯乱円の円盤 (_TS_Misc.w 点) で「手前の物が占める割合」と「後ろの背景の平均」を取る。
//     α = max(タイルの半径での被覆, 自分の円盤での被覆)・背景は far (MRT 0) に α=1 で書き、合成で元の色の代わりの下地にする
//     (前は自分の画素を α=1 にしていたので、細い蔦・羊歯の輪郭がくっきり残った)。
//   - 手前の上限は _TS_Focus.z 倍 (既定 1)
//   - 2周目 (本家っぽく): 帯の外のぼけの伸び方を「レンズ」にできる (_TS_Lens.xy > 0)。錯乱円 = 上限 × saturate(強さ × 帯の縁からの深さの差 ÷ 深さ)
//     (薄いレンズの |z − 焦点| ÷ z と同じ形)。本家 (オクトラ1 の夜の森・洞窟・村) を測ると、ぼけは弱く・奥ほど少しずつ強い (帯のすぐ奥の段はほぼくっきり・
//     奥の木は形が読める・手前の草は 4〜5px)。今までの smoothstep (帯の外 5 unit で上限 24px) は奥が全部同じ塗りつぶしになっていた。0 = 今までの形
//
// 決まり
//   - 全部のテクスチャは材質でなく MaterialPropertyBlock で渡す (パスごとに別々。_TS_* の名前)。_BlitTexture は使わない
//   - 奥の層と手前の層は前乗算 (rgb×α, α) で持つ = 合成の4点のテントで有効な点だけが平均される
//   - 乱数は画素の位置だけで決める (時間を使わない = det の撮影で毎回同じ画)
//   - uv の y は 1 が画面の上 (Unity のテクスチャの向き)
Shader "DeckRogue/TiltShift"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    TEXTURE2D_X(_TS_Source);        // カメラの色 (全解像度)
    TEXTURE2D_X_FLOAT(_TS_Depth);   // カメラの深さ (全解像度)
    TEXTURE2D_X(_TS_Work);          // rgb = 色・a = 符号つきの錯乱円 (作業の解像度の px)
    TEXTURE2D_X(_TS_Tile);          // r = 手前の錯乱円のタイルの最大値
    TEXTURE2D_X(_TS_Far);           // 奥の層 (前乗算)
    TEXTURE2D_X(_TS_Near);          // 手前の層 (前乗算)

    float4 _TS_FullSize;    // 全解像度 (w, h, 1/w, 1/h)
    float4 _TS_WorkSize;    // 作業の解像度 (w, h, 1/w, 1/h)
    float4 _TS_TileSize;    // タイルのテクスチャ (w, h, 1/w, 1/h)
    float4 _TS_DepthScale;  // xy = 深さのテクスチャの uv の倍率 (RTHandle の拡縮。普通は 1)
    float4 _TS_Band;        // x = 帯の手前・y = 帯の奥 (カメラからの距離 unit)・z = 1/手前の傾斜・w = 1/奥の傾斜
    float4 _TS_Params;      // x = 半径の上限 (全解像度の px)・y = 作業の倍率 (1 = 全・2 = 半分)・z = 点の数・w = 回転の揺らぎ (0/1)
    float4 _TS_Tilt;        // x = 上端の追加のぼけ (上限に対する割合)・y = 上の始まり (画面の高さの割合)・z = 下端・w = 下の始まり
    float4 _TS_Misc;        // x = タイルの px・y = 奥の層が拾わない手前の点のしきい (中心の錯乱円に対する割合)・z = 表示の調べ (0〜3)・w = 自分の円盤の点の数 (P24)
    float4 _TS_Focus;       // P24: x = 道の帯の手前 (s)・y = 奥 (s)・z = 手前の上限の倍率・w = 1 なら道に沿った帯 (0 = 深さの帯)
    float4 _TS_Plane;       // P24: x = n·カメラの右・y = n·カメラの上・z = n·カメラの前・w = カメラの位置の s (n = 道の s 軸)
    float4 _TS_Proj;        // P24: x = 1/m00・y = 1/m11・z = m02・w = m12 (カメラの投影。GL の形)
    float4 _TS_Lens;        // P24 (2周目): x = 奥のレンズの強さ・y = 手前のレンズの強さ (0 = 今までの smoothstep の傾斜)。zw は空き

    #define TS_GOLDEN_ANGLE 2.39996323

    // 線形の深さ (カメラからの距離 unit)。舞台のカメラは透視だが、正射影でも壊れないようにしておく
    float TsEyeDepth(float raw)
    {
        if (unity_OrthoParams.w > 0.5)
        {
        #if UNITY_REVERSED_Z
            raw = 1.0 - raw;
        #endif
            return lerp(_ProjectionParams.y, _ProjectionParams.z, raw);
        }
        return LinearEyeDepth(raw, _ZBufferParams);
    }

    // 全解像度の画素 fp の錯乱円 (全解像度の px。手前は負・奥は正)
    float TsCocFull(int2 fp)
    {
        float2 uv = (float2(fp) + 0.5) * _TS_FullSize.zw;
        float raw = SAMPLE_TEXTURE2D_X_LOD(_TS_Depth, sampler_PointClamp, uv * _TS_DepthScale.xy, 0).r;
        float z = TsEyeDepth(raw);
        float c = 0.0;
        // 道に沿った帯 (P24): 視線の向き r (カメラの空間・+z が前) と、道の s の視線に沿った増え方 k
        float2 r = ((uv * 2.0 - 1.0) + _TS_Proj.zw) * _TS_Proj.xy;
        float k = _TS_Plane.x * r.x + _TS_Plane.y * r.y + _TS_Plane.z;
        // 帯の外の距離 (視線に沿った深さの差・unit)。dzF = 帯の奥の縁より奥・dzN = 帯の手前の縁より手前 (どちらも帯の中なら 0)
        float dzF = 0.0, dzN = 0.0;
        if (_TS_Focus.w > 0.5 && k > 0.05)
        {
            float s = _TS_Plane.w + z * k;
            dzN = max(_TS_Focus.x - s, 0.0) / k;
            dzF = max(s - _TS_Focus.y, 0.0) / k;
        }
        else
        {
            dzN = max(_TS_Band.x - z, 0.0);
            dzF = max(z - _TS_Band.y, 0.0);
        }
        // 形 (P24 2周目): レンズ (_TS_Lens.xy > 0) = 薄いレンズの錯乱円 ∝ (帯の縁からの深さの差) ÷ (深さ)。帯の縁から緩やかに始まり、遠くほど伸びて上限へ近づく
        //   (本家の夜の森は、帯のすぐ奥の段はほぼくっきり・奥の木は柔らかく読める・いちばん奥は霧で沈む)。0 = 今までの smoothstep の傾斜 (_TS_Band.zw)
        float zs = max(z, 1e-3);
        if (dzF > 0.0)      c =  (_TS_Lens.x > 0.0 ? saturate(_TS_Lens.x * dzF / zs) : smoothstep(0.0, 1.0, saturate(dzF * _TS_Band.w)));
        else if (dzN > 0.0) c = -(_TS_Lens.y > 0.0 ? saturate(_TS_Lens.y * dzN / zs) : smoothstep(0.0, 1.0, saturate(dzN * _TS_Band.z)));
        // 画面の上下の追加のぼけ (既定 0 = 無し)。奥の側は上端で、手前の側は下端で強める。符号の向きは深さの結果に従う
        float top = _TS_Tilt.x * saturate((uv.y - _TS_Tilt.y) / max(1.0 - _TS_Tilt.y, 1e-3));
        float bot = _TS_Tilt.z * saturate((_TS_Tilt.w - uv.y) / max(_TS_Tilt.w, 1e-3));
        if (c >= 0.0) c = max(c, top);
        if (c <= 0.0) c = min(c, -bot);
        if (c < 0.0) c *= _TS_Focus.z;                 // 手前の上限の倍率 (P24。既定 1)
        return c * _TS_Params.x;
    }

    float3 TsLoadSource(int2 fp)
    {
        fp = clamp(fp, int2(0, 0), int2(_TS_FullSize.xy) - 1);
        return LOAD_TEXTURE2D_X(_TS_Source, fp).rgb;
    }

    // 画素の位置だけで決まる揺らぎ (Interleaved Gradient Noise。時間は使わない)
    float TsIgn(float2 p)
    {
        return frac(52.9829189 * frac(dot(p, float2(0.06711056, 0.00583715))));
    }

    // 単位円の中のゴールデンアングルの渦 (k 番目・全 n 点・回転 rot)
    float2 TsSpiral(int k, int n, float rot)
    {
        float r = sqrt((k + 0.5) / n);
        float a = k * TS_GOLDEN_ANGLE + rot;
        return float2(cos(a), sin(a)) * r;
    }

    // ---------------------------------------------------------------- 0 Prefilter
    float4 FragPrefilter(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        int2 wp = int2(input.positionCS.xy);
        float scale = _TS_Params.y;
        float3 col;
        float coc;
        if (scale > 1.5)
        {
            int2 fp = wp * 2;
            float3 c0 = TsLoadSource(fp), c1 = TsLoadSource(fp + int2(1, 0)), c2 = TsLoadSource(fp + int2(0, 1)), c3 = TsLoadSource(fp + int2(1, 1));
            float k0 = TsCocFull(fp), k1 = TsCocFull(fp + int2(1, 0)), k2 = TsCocFull(fp + int2(0, 1)), k3 = TsCocFull(fp + int2(1, 1));
            float kmin = min(min(k0, k1), min(k2, k3));
            float kmax = max(max(k0, k1), max(k2, k3));
            coc = (-kmin > kmax) ? kmin : kmax;
            // 色は、採った錯乱円と同じ側 (奥なら奥・手前なら手前) の点だけで平均する = 帯の中のキャラの色が縁の1テクセルに混ざらない。
            // さらに明るい1点がぼけの円に化けてちらつかないよう、明るさで重みを下げる (Karis)
            float sgn = coc >= 0.0 ? 1.0 : -1.0;
            float thr = max(abs(coc) * 0.5, 1e-3);
            float w0 = (saturate(k0 * sgn / thr) + 1e-4) / (1.0 + Max3(c0.r, c0.g, c0.b));
            float w1 = (saturate(k1 * sgn / thr) + 1e-4) / (1.0 + Max3(c1.r, c1.g, c1.b));
            float w2 = (saturate(k2 * sgn / thr) + 1e-4) / (1.0 + Max3(c2.r, c2.g, c2.b));
            float w3 = (saturate(k3 * sgn / thr) + 1e-4) / (1.0 + Max3(c3.r, c3.g, c3.b));
            col = (c0 * w0 + c1 * w1 + c2 * w2 + c3 * w3) / (w0 + w1 + w2 + w3);
        }
        else
        {
            col = TsLoadSource(wp);
            coc = TsCocFull(wp);
        }
        return float4(col, coc / scale);
    }

    // ---------------------------------------------------------------- 1 TileMax
    float4 FragTileMax(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        int2 tp = int2(input.positionCS.xy);
        int t = (int)_TS_Misc.x;
        int2 origin = tp * t;
        int2 lim = int2(_TS_WorkSize.xy) - 1;
        float m = 0.0;
        [loop] for (int y = 0; y < t; y++)
        {
            [loop] for (int x = 0; x < t; x++)
            {
                int2 p = min(origin + int2(x, y), lim);
                m = max(m, -LOAD_TEXTURE2D_X(_TS_Work, p).a);
            }
        }
        return float4(m, 0.0, 0.0, 0.0);
    }

    // ---------------------------------------------------------------- 2 TileDilate
    float4 FragTileDilate(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        int2 tp = int2(input.positionCS.xy);
        int2 lim = int2(_TS_TileSize.xy) - 1;
        float m = 0.0;
        [unroll] for (int y = -1; y <= 1; y++)
        {
            [unroll] for (int x = -1; x <= 1; x++)
            {
                int2 p = clamp(tp + int2(x, y), int2(0, 0), lim);
                m = max(m, LOAD_TEXTURE2D_X(_TS_Tile, p).r);
            }
        }
        return float4(m, 0.0, 0.0, 0.0);
    }

    // ---------------------------------------------------------------- 3 Gather (MRT)
    struct TsGatherOut
    {
        float4 far  : SV_Target0;
        float4 near : SV_Target1;
    };

    TsGatherOut FragGather(Varyings input)
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        int2 wp = int2(input.positionCS.xy);
        float4 c = LOAD_TEXTURE2D_X(_TS_Work, wp);
        int t = max((int)_TS_Misc.x, 1);
        float farR = max(c.a, 0.0);
        float nearR = LOAD_TEXTURE2D_X(_TS_Tile, wp / t).r;
        int taps = max((int)_TS_Params.z, 1);
        float rot = _TS_Params.w > 0.5 ? TsIgn(float2(wp)) * TWO_PI : 0.0;
        float2 center = float2(wp) + 0.5;

        TsGatherOut o;
        o.far = float4(0.0, 0.0, 0.0, 0.0);
        o.near = float4(0.0, 0.0, 0.0, 0.0);

        // 奥の層: 半径 = 中心の錯乱円
        if (farR >= 0.5)
        {
            float3 acc = c.rgb;
            float wsum = 1.0;
            float excl0 = farR * _TS_Misc.y;          // これより錯乱円が小さい (= 手前の) 点は拾わない
            float exclW = max(farR * 0.25, 0.5);
            [loop] for (int k = 0; k < taps; k++)
            {
                float2 off = TsSpiral(k, taps, rot) * farR;
                float4 s = SAMPLE_TEXTURE2D_X_LOD(_TS_Work, sampler_PointClamp, (center + off) * _TS_WorkSize.zw, 0);
                float sf = max(s.a, 0.0);
                float w = saturate(sf - length(off) + 1.0)     // その点のぼけの円が中心まで届く
                        * saturate(sf - 0.5)                   // 帯の中 (と手前) の点は拾わない = 座席のキャラが背景に滲まない
                        * saturate((sf - excl0) / exclW);      // 自分より手前の点は拾わない
                acc += s.rgb * w;
                wsum += w;
            }
            float v = saturate(farR - 0.5);
            o.far = float4(acc / wsum * v, v);
        }

        // 手前の層: 半径 = 近くのタイルの手前の錯乱円の最大 (にじみ出し)
        if (nearR >= 0.5)
        {
            float3 acc = float3(0.0, 0.0, 0.0);
            float wsum = 0.0;
            [loop] for (int k = 0; k < taps; k++)
            {
                float2 off = TsSpiral(k, taps, rot) * nearR;
                float4 s = SAMPLE_TEXTURE2D_X_LOD(_TS_Work, sampler_PointClamp, (center + off) * _TS_WorkSize.zw, 0);
                float sn = max(-s.a, 0.0);
                float w = saturate(sn - length(off) + 1.0) * saturate(sn - 0.5);
                acc += s.rgb * w;
                wsum += w;
            }
            float cover = saturate(wsum / taps);
            float a = cover;
            // 自分が手前の画素 (P24): 自分の錯乱円の円盤で「手前の物が占める割合」(α) と「後ろの背景」(手前でない点の平均) を取る。
            // 太い物の内側は割合 ≒ 1 (今までどおりぼけた層で覆う)・輪郭は ≒ 0.5・細い蔦は小さい = 背景が透けて輪郭が溶ける。
            // 背景は far (MRT 0) に α=1 で書く (自分が手前の画素は奥の層を使わないので空いている)。合成が元の色の代わりの下地にする
            float own = max(-c.a, 0.0);
            if (own >= 0.5)
            {
                int ownTaps = clamp((int)_TS_Misc.w, 1, 64);
                float rot2 = rot + 1.61803399;
                float3 accO = float3(0.0, 0.0, 0.0);
                float wO = 0.0;
                float3 bg = float3(0.0, 0.0, 0.0);
                float bgW = 0.0;
                [loop] for (int j = 0; j < ownTaps; j++)
                {
                    float2 off = TsSpiral(j, ownTaps, rot2) * own;
                    float4 s = SAMPLE_TEXTURE2D_X_LOD(_TS_Work, sampler_PointClamp, (center + off) * _TS_WorkSize.zw, 0);
                    float sn = max(-s.a, 0.0);
                    float isNear = saturate(sn - 0.5);
                    float w = saturate(sn - length(off) + 1.0) * isNear;   // 手前の点で、そのぼけの円が中心まで届く
                    accO += s.rgb * w;
                    wO += w;
                    float wb = 1.0 - isNear;                              // 手前でない点 (帯の中・奥) = 後ろの背景
                    bg += s.rgb * wb;
                    bgW += wb;
                }
                a = max(cover, saturate(wO / ownTaps));
                acc += accO;
                wsum += wO;
                float3 fill = bgW > 1e-3 ? bg / bgW : (wsum > 1e-4 ? acc / wsum : c.rgb);
                o.far = float4(fill, 1.0);
            }
            float3 nc = wsum > 1e-4 ? acc / wsum : c.rgb;
            o.near = float4(nc * a, a);
        }
        return o;
    }

    // ---------------------------------------------------------------- 4 Composite
    float4 FragComposite(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        int2 fp = int2(input.positionCS.xy);
        float4 src = LOAD_TEXTURE2D_X(_TS_Source, clamp(fp, int2(0, 0), int2(_TS_FullSize.xy) - 1));
        float coc = TsCocFull(fp) / _TS_Params.y;       // 作業の解像度の px
        float2 uv = (float2(fp) + 0.5) * _TS_FullSize.zw;
        float2 h = 0.5 * _TS_WorkSize.zw;

        // 4点のテント (対角に半テクセル・双線形)。前乗算なので有効な点だけの平均になる
        float4 f = SAMPLE_TEXTURE2D_X_LOD(_TS_Far, sampler_LinearClamp, uv + float2(-h.x, -h.y), 0)
                 + SAMPLE_TEXTURE2D_X_LOD(_TS_Far, sampler_LinearClamp, uv + float2( h.x, -h.y), 0)
                 + SAMPLE_TEXTURE2D_X_LOD(_TS_Far, sampler_LinearClamp, uv + float2(-h.x,  h.y), 0)
                 + SAMPLE_TEXTURE2D_X_LOD(_TS_Far, sampler_LinearClamp, uv + float2( h.x,  h.y), 0);
        float4 n = SAMPLE_TEXTURE2D_X_LOD(_TS_Near, sampler_LinearClamp, uv + float2(-h.x, -h.y), 0)
                 + SAMPLE_TEXTURE2D_X_LOD(_TS_Near, sampler_LinearClamp, uv + float2( h.x, -h.y), 0)
                 + SAMPLE_TEXTURE2D_X_LOD(_TS_Near, sampler_LinearClamp, uv + float2(-h.x,  h.y), 0)
                 + SAMPLE_TEXTURE2D_X_LOD(_TS_Near, sampler_LinearClamp, uv + float2( h.x,  h.y), 0);
        n *= 0.25;

        float3 farC = f.a > 1e-4 ? f.rgb / f.a : src.rgb;
        float farF = saturate(coc - 0.5) * saturate(f.a);   // 帯の中 (coc = 0) は 0 = 元の色のまま
        // 自分が手前の画素 (P24): 下地を元の色 (くっきりした手前の物) でなく、Gather が far に書いた後ろの背景にする (輪郭が溶ける)
        float fillF = f.a > 1e-4 ? saturate(-coc - 0.5) : 0.0;
        float3 base = lerp(src.rgb, farC, max(farF, fillF));
        float3 outc = base * (1.0 - n.a) + n.rgb;

        int dbg = (int)_TS_Misc.z;
        if (dbg == 1) outc = coc < 0.0 ? float3(saturate(-coc / max(_TS_Params.x / _TS_Params.y, 1e-3)), 0.0, 0.0)
                                       : float3(0.0, 0.0, saturate(coc / max(_TS_Params.x / _TS_Params.y, 1e-3)));
        else if (dbg == 2) outc = farC * saturate(f.a);
        else if (dbg == 3) outc = n.rgb;
        return float4(outc, src.a);
    }

    // ---------------------------------------------------------------- 5 Copy
    float4 FragCopy(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord.xy);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off
        Pass
        {
            Name "TiltShift Prefilter"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragPrefilter
            ENDHLSL
        }
        Pass
        {
            Name "TiltShift TileMax"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragTileMax
            ENDHLSL
        }
        Pass
        {
            Name "TiltShift TileDilate"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragTileDilate
            ENDHLSL
        }
        Pass
        {
            Name "TiltShift Gather"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragGather
            ENDHLSL
        }
        Pass
        {
            Name "TiltShift Composite"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragComposite
            ENDHLSL
        }
        Pass
        {
            Name "Copy"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragCopy
            ENDHLSL
        }
    }
}
