// HD2DPixel.hlsl — HD-2D 見本のドットの読み方の共通部品 (2026-09-30 P03。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-2・§2-5)。
// StageModule (舞台の部品) と StageUnitLit (キャラの板) が使う。URP の Core.hlsl を先に include してから読むこと。
//
//   HD2D_FatTexel      … ドットの縁だけを1画素なじませる読み方 (t3ssel8r の方式。fwidth と smoothstep)。近くは角が立ち、遠く (1画素が1テクセルより広い) は
//                        ただの双線形になってミップが受け持つ = ちらつかない。テクスチャは双線形 (Bilinear/Trilinear) で読むこと (Point だと縁のなじみが消えるだけ)
//   HD2D_TexelShift    … 面の上で「いまの画素の位置 → そのドットの中心」へ動かす世界のずれ (画面の微分から解く)。光と影をドットの中心で計算すれば、1ドットの中で明るさが割れない
//   HD2D_DotNoise      … ドット単位の閾値 (インターリーブド・グラディエント・ノイズ。ブルーノイズの代わりの手続き型)。苔の段々の切り替えに使う
//   HD2D_TileXform     … タイルの回し方 (0 = なし・1 = 左右の反転だけ・2 = 90° ずつ4通り)。読む座標 q = M·(p−½)+½、法線の xy は Mᵀ で面へ戻す
//   HD2D_DecodeNormal  … 法線を tex×2−1 で自分でほどく (UnpackNormal は使わない。Linear のデータとして読む前提＝配列は linear:true・_n.png は sRGB なし)
//   HD2D_DecodeAlbedo  … linear:true で作った配列に sRGB の色が入っている時、Linear の色空間でだけ sRGB→線形に戻す (Gamma では何もしない)
#ifndef HD2D_PIXEL_INCLUDED
#define HD2D_PIXEL_INCLUDED

// ---------------------------------------------------------------- ドットの縁のなじませ

// texel = いまの画素の mip0 のテクセル座標 (uv × 大きさ)。fw = そのテクセル座標の fwidth (タイルの frac を取る前の連続した座標で求めたもの)。
// 返すのはテクセル座標 (ドットの中心が n+0.5)。縁の幅はちょうど1画素 (box)。
float2 HD2D_FatTexelW(float2 texel, float2 fw)
{
    float2 box = clamp(fw, 1e-5, 1.0);
    float2 tx = texel - 0.5 * box;
    float2 off = smoothstep(1.0 - box, 1.0, frac(tx));
    return floor(tx) + 0.5 + off;
}

float2 HD2D_FatTexel(float2 texel)
{
    return HD2D_FatTexelW(texel, fwidth(texel));
}

// ドットの中心 (テクセル座標)
float2 HD2D_TexelCenter(float2 texel)
{
    return floor(texel) + 0.5;
}

// ---------------------------------------------------------------- ドットの中心で計算する

// いまの画素 → そのドットの中心へ動かした時の世界のずれ。平らな面 (三角形の中) なら正確 (画面の微分から 2×2 を解く)。
// 見る角度が浅くて解けない時・ずれが大きすぎる時は 0 (ずらさない)。texel はテクセル座標、positionWS は世界の位置 (どちらも補間された値)。
float3 HD2D_TexelShift(float2 texel, float3 positionWS)
{
    float2 dTx = ddx(texel), dTy = ddy(texel);
    float3 dPx = ddx(positionWS), dPy = ddy(positionWS);
    float det = dTx.x * dTy.y - dTy.x * dTx.y;
    float2 want = HD2D_TexelCenter(texel) - texel;
    if (abs(det) < 1e-8) return float3(0, 0, 0);
    // [dTx dTy]·(sx, sy) = want を解く (sx, sy は画面の画素)
    float sx = (want.x * dTy.y - dTy.x * want.y) / det;
    float sy = (dTx.x * want.y - want.x * dTx.y) / det;
    // 浅い角度の暴れ止め (1ドットは近くで4画素前後。8画素を超えるずれは捨てる)
    if (abs(sx) > 8.0 || abs(sy) > 8.0) return float3(0, 0, 0);
    return dPx * sx + dPy * sy;
}

// ---------------------------------------------------------------- 乱数

// ドット単位の閾値 [0,1) (cell = 整数のテクセル番号)。大きい座標で精度が落ちないよう 1024 で畳む
float HD2D_DotNoise(float2 cell)
{
    cell = cell - 1024.0 * floor(cell / 1024.0);
    return frac(52.9829189 * frac(dot(cell, float2(0.06711056, 0.00583715))));
}

// タイル単位の乱数 [0,1)
float HD2D_Hash21(float2 p)
{
    p = p - 4096.0 * floor(p / 4096.0);
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// なめらかな値ノイズ (大きい斑。苔の境を面ごとに少し揺らす)
float HD2D_ValueNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = HD2D_Hash21(i), b = HD2D_Hash21(i + float2(1, 0)), c = HD2D_Hash21(i + float2(0, 1)), d = HD2D_Hash21(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// ---------------------------------------------------------------- タイルの回し方

// mode: 0 = なし・1 = 左右の反転だけ (草の天面)・2 = 90° ずつ4通り (土や岩の天面のような向きの無い材質)。r = タイルの乱数 [0,1)。
// 返す M は直交行列 (行ごと)。読む座標 q = mul(M, p − 0.5) + 0.5。面の上の向き d_p = mul(transpose(M), d_q)
float2x2 HD2D_TileXform(float mode, float r)
{
    if (mode > 1.5)
    {
        float k = floor(r * 4.0);
        if (k < 0.5) return float2x2(1, 0, 0, 1);
        if (k < 1.5) return float2x2(0, -1, 1, 0);
        if (k < 2.5) return float2x2(-1, 0, 0, -1);
        return float2x2(0, 1, -1, 0);
    }
    if (mode > 0.5 && r >= 0.5) return float2x2(-1, 0, 0, 1);
    return float2x2(1, 0, 0, 1);
}

// ---------------------------------------------------------------- 色と法線

// 法線 (接空間) を tex×2−1 でほどく。strength で xy を強め/弱める (0 で平ら)。0 に近い値 (空のテクスチャ) なら平らを返す
half3 HD2D_DecodeNormal(half4 t, half strength)
{
    // 真っ黒 (テクスチャが割り当てられていない配列など) は平らとみなす
    if (dot(t.rgb, t.rgb) < 1e-4h) return half3(0.0h, 0.0h, 1.0h);
    half3 n = t.rgb * 2.0h - 1.0h;
    n.xy *= strength;
    n.z = max(n.z, 0.05h);
    half len2 = dot(n, n);
    return len2 > 1e-4h ? n * rsqrt(len2) : half3(0.0h, 0.0h, 1.0h);
}

// linear:true で作った配列に sRGB の色が入っている時 (flag > 0.5) だけ、Linear の色空間で線形に戻す
half3 HD2D_DecodeAlbedo(half3 c, half flag)
{
#if !defined(UNITY_COLORSPACE_GAMMA)
    if (flag > 0.5h) c = SRGBToLinear(c);
#endif
    return c;
}

#endif // HD2D_PIXEL_INCLUDED
