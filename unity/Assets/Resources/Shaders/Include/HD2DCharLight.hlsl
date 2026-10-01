// HD2DCharLight.hlsl — HD-2D 見本のキャラの固定の光 (2026-09-30 P03。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-4・§2-5)。
// StageUnitLit が使う。URP の Lighting.hlsl (RealtimeLights・AmbientProbe を含む) を先に include してから読むこと。
//
// 本家の作法: ドットには陰影が描き込み済み。キャラには写実的な光を当てず、全員に同じ向き・同じ色の「舞台の灯」(固定のキーライト) と、
// 上下の環境光だけを当てる。値は StageLook.Apply が全体値 (Shader.SetGlobal…) で書く:
//   _CharKeyDir    float4  xyz = キーライトが「進む」向き (世界。StageLook.KeyDir と同じ)。長さ0 (未設定) なら場の主光 (月) の向きと色を使う
//   _CharKeyColor  color   rgb = キーの色×強さ (HDR 可)。既定の狙いは中立からわずかに寒色 (0.92, 0.96, 1.0)
//   _CharAmbTop    color   上から来る環境光 (rgb)
//   _CharAmbBottom color   下から来る環境光 (rgb)。上下とも 0 (未設定) なら場の環境光 (SH) を使う
// 三周目 レーン E (2026-10-01): HD2D_BodyShadeMul (足元ほど暗い縦の勾配の倍率。材質ごとの _BodyShade は StageUnitLit の CBUFFER にある)
#ifndef HD2D_CHAR_LIGHT_INCLUDED
#define HD2D_CHAR_LIGHT_INCLUDED

float4 _CharKeyDir;
half4 _CharKeyColor;
half4 _CharAmbTop;
half4 _CharAmbBottom;

// キーライトの向き (光の方へ向かう単位ベクトル) と色
void HD2D_CharKey(out half3 toLight, out half3 color)
{
    if (dot(_CharKeyDir.xyz, _CharKeyDir.xyz) > 1e-6)
    {
        toLight = half3(-normalize(_CharKeyDir.xyz));
        color = _CharKeyColor.rgb;
    }
    else
    {
        Light m = GetMainLight();
        toLight = m.direction;
        color = m.color;
    }
}

// 上下の環境光 (nWS = 世界の法線)
half3 HD2D_CharAmbient(half3 nWS)
{
    half3 top = _CharAmbTop.rgb, bottom = _CharAmbBottom.rgb;
    if (dot(top + bottom, half3(1.0h, 1.0h, 1.0h)) <= 1e-4h)
        return max(half3(0.0h, 0.0h, 0.0h), half3(SampleSH(nWS)));
    return lerp(bottom, top, saturate(nWS.y * 0.5h + 0.5h));
}

// 足元ほど暗い縦の勾配の倍率 (三周目 レーン E・R5 (b))。y = 板の uv0 の高さ (下端 0・上端 1)、p = _BodyShade
// (x = 足元の暗さ 0〜1・y = 足元の uv・z = 勾配が 1 に戻る uv・w = 曲がり)。足元 (y ≤ p.y) は 1 − p.x、p.z より上は 1。p.x = 0 なら 1 (何もしない)
half HD2D_BodyShadeMul(float y, half4 p)
{
    half t = saturate((half(y) - p.y) / max(p.z - p.y, 1e-3h));
    t = pow(max(t, 1e-4h), max(p.w, 0.05h));
    return 1.0h - saturate(p.x) * (1.0h - t);
}

// 世界の向き v を板の接空間 (x = 右・y = 上・z = 見る人の方) へ。keyFlip > 0.5 なら x を反転する
// (画像ファイルを左右反転して置いた絵は、描き込まれた光も左右が逆 = キーの向きを絵に合わせる。計画 §2-5 _KeyFlip)
half3 HD2D_ToSprite(half3 v, half3 R, half3 U, half3 F, half keyFlip)
{
    half3 s = half3(dot(v, R), dot(v, U), dot(v, F));
    if (keyFlip > 0.5h) s.x = -s.x;
    return s;
}

#endif // HD2D_CHAR_LIGHT_INCLUDED
