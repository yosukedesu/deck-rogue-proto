// StageActs.cs — 幕2 (先代の坑道の宿場跡) と幕3 (埋もれた古代都市) の箱庭 (2026-09-21 HD-2D)。
// Opus の設計ワークフロー (構図／素材／光 の3案 → 審査 → 統合。docs/stage-hd2d-2026-09-21.md) の統合案を組む。
// 地形 (帯の段・垂直の面・水面) は Stage.BuildTerrain (H2/H3・BandMat・AddStepFaces)、ここは壁・天井・段鼻・小物・光・粒子。
// 一枚絵は Art/props/act<N>_<name>.png (無ければ置かない)。座標は道の座標 OnPath(t, s)。段の上の物は GroundY で自動的に段の高さに立つ。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeckRogue.Game
{
    public static partial class Stage
    {
        // ---------------------------------------------------------------- 共通の小さな道具

        static GameObject Halo(string nm, Vector3 center, float size, Color c)
        {
            var g = Glow(nm, Px.Glow(c), center, size, size);
            g.GetComponent<MeshFilter>().sharedMesh = _quadCentered;
            return g;
        }
        static Light PointLight(string nm, Vector3 pos, Color c, float intensity, float range)
        {
            var go = new GameObject(nm); go.transform.SetParent(_world, false); go.transform.position = pos;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = c; l.shadows = LightShadows.None;
            return l;
        }
        /// <summary>光溜まり: 地面のすぐ上に寝かせた放射状の暈</summary>
        static GameObject Pool(string nm, Vector3 pos, Color c, float w, float d)
        {
            var g = Glow(nm, Px.Radial(c), new Vector3(pos.x, GroundY(pos.x, pos.z) + 0.035f, pos.z), 1f, 1f);
            g.GetComponent<MeshFilter>().sharedMesh = _quadCentered; g.transform.rotation = Quaternion.Euler(90f, 0f, 0f); g.transform.localScale = new Vector3(w, d, 1f);
            return g;
        }
        /// <summary>一枚絵を道の座標に立てる (透明率の検査つき)。h=世界の高さ・blobK=接地影の幅の係数 (0 で影なし)・yOff=足元の持ち上げ・fog=霧の受け方・sun=月光の勾配</summary>
        static GameObject Put(int act, string name, float t, float s, float h, float blobK, bool flip = false, float yOff = 0f, float fog = 1f, float sun = 1f)
        {
            var tex = PropTex(act, name, null);
            if (tex == null) return null;
            var pos = OnPath(t, s); pos.y += yOff;
            var g = SpriteH(name, tex, pos, h, blobK, flip);
            var m = g.GetComponent<MeshRenderer>().sharedMaterial;
            if (fog != 1f) m.SetFloat("_Fog", fog);
            if (sun != 1f) m.SetFloat("_SunAmount", sun);
            return g;
        }
        /// <summary>塗りの多い大物 (門・機械・館) は透明率の検査なしで立てる</summary>
        static GameObject PutRaw(int act, string name, float t, float s, float h, float blobK, bool flip = false, float yOff = 0f, float fog = 1f, float sun = 1f)
        {
            var tex = PropTexRaw(act, name, null);
            if (tex == null) return null;
            var pos = OnPath(t, s); pos.y += yOff;
            var g = SpriteH(name, tex, pos, h, blobK, flip);
            var m = g.GetComponent<MeshRenderer>().sharedMaterial;
            if (fog != 1f) m.SetFloat("_Fog", fog);
            if (sun != 1f) m.SetFloat("_SunAmount", sun);
            return g;
        }
        /// <summary>天井から吊る (上端 topY・高さ h)。影は落とさない</summary>
        static GameObject Hang(int act, string name, float t, float s, float topY, float h, bool flip = false, float fog = 1f)
        {
            var tex = PropTexRaw(act, name, null);
            if (tex == null) return null;
            var w = OnPath(t, s);
            var g = Plane(name, tex, new Vector3(w.x, topY - h, w.z), h, 0.5f, false);
            if (flip) g.transform.localScale = new Vector3(-g.transform.localScale.x, g.transform.localScale.y, 1f);
            var mr = g.GetComponent<MeshRenderer>(); mr.shadowCastingMode = ShadowCastingMode.Off;
            if (fog != 1f) mr.sharedMaterial.SetFloat("_Fog", fog);
            return g;
        }
        /// <summary>壁の面 (道の向きに回した平面 s=S) にタイルを敷く。cliff a〜d のアトラスをタイル列ごとに象限を選び (回さない)、無ければ 1 種の Repeat</summary>
        static GameObject WallTiles(string name, float t0, float t1, float y0, float y1, float S, Material mat, int variants, float yawOffset = 0f)
        {
            var mb = new MB();
            int cols = Mathf.CeilToInt((t1 - t0) / Tile), rows = Mathf.CeilToInt((y1 - y0) / Tile);
            for (int c = 0; c < cols; c++)
                for (int r = 0; r < rows; r++)
                {
                    float ta = t0 + c * Tile, tb = Mathf.Min(t1, ta + Tile), ya = y0 + r * Tile, yb = Mathf.Min(y1, ya + Tile);
                    var a = new Vector3(ta, ya, S); var b = new Vector3(ta, yb, S); var cc = new Vector3(tb, yb, S); var d = new Vector3(tb, ya, S);
                    Vector2 ua, ub, uc, ud;
                    if (variants > 0)
                    {
                        int q = (int)(Hash01(c * 7 + 1, r * 3 + 5) * 4f) % 4; float lu = (tb - ta) / Tile, lv = (yb - ya) / Tile;
                        ua = AtlasUv(q, 0, 0f, 0f); ub = AtlasUv(q, 0, 0f, lv); uc = AtlasUv(q, 0, lu, lv); ud = AtlasUv(q, 0, lu, 0f);
                    }
                    else { ua = new Vector2(ta / Tile, ya / Tile); ub = new Vector2(ta / Tile, yb / Tile); uc = new Vector2(tb / Tile, yb / Tile); ud = new Vector2(tb / Tile, ya / Tile); }
                    mb.Quad(a, b, cc, d, ua, ub, uc, ud);   // 法線 = 局所 −z (手前 = カメラ側)
                }
            var go = Solid(name, mb, mat);
            go.transform.rotation = Quaternion.Euler(0f, PathYaw + yawOffset, 0f);
            return go;
        }
        /// <summary>道の向きに回した入れ物の中で箱を組む (梁・柱・段鼻)。局所座標 (t, y, s)</summary>
        static GameObject PathBoxes(string name, Material mat, Action<MB> build)
        {
            var mb = new MB(); build(mb);
            var go = Solid(name, mb, mat);
            go.transform.rotation = Quaternion.Euler(0f, PathYaw, 0f);
            return go;
        }
        static Material Wood(int act, Color tint)
        {
            var t = Tex(act, "wood", null);
            var m = Lit(t != null ? t : Px.Solid(UiKit.Hex("#4a3a2c")));
            m.SetColor("_BaseColor", t != null ? tint : Color.white);
            return m;
        }

        // ---------------------------------------------------------------- 幕2 先代の坑道 (宿場跡)

        /// <summary>幕2 (2026-09-21 統合案): 天井で閉じた三段の段丘の箱庭。溝 (−0.35・水面) ／道 (土) ／一段目 (+0.55 敷石の宿場: 炉・屋台・差し掛け) ／二段目 (+1.1 軌道・トロッコ・巻き上げ機) ／
        /// 奥の岩壁 (道と平行・タイル 4 種・坑口が奥行きの錨・大結晶) と壁の手前の木の歩廊。暖色は炉 1 か所と提灯だけ、地は青灰。風穴の冷たい光の筋 1 本</summary>
        static void PaintMarket2(Pal p, System.Random rng, Material mFloor, Material mCliff)
        {
            const int A = 2;
            var veinC = new Color(0.42f, 0.95f, 0.86f);
            var warm = new Color(1f, 0.62f, 0.32f);
            RenderSettings.fogStartDistance = 14f; RenderSettings.fogEndDistance = 48f;
            mFloor.SetFloat("_Smoothness", 0.2f);
            var mWood = Wood(A, new Color(0.72f, 0.66f, 0.62f));
            var mPlank = Tex(A, "plank", null) != null ? Lit(Tex(A, "plank", null)) : mWood; if (mPlank != mWood) mPlank.SetColor("_BaseColor", new Color(0.7f, 0.64f, 0.6f));
            var mIron = Lit(Px.Solid(UiKit.Hex("#2c2a30")));
            _decalTint = new Color(0.78f, 0.78f, 0.86f);   // 敷物 = 道の色

            // ---- 壁と天井
            const float WallS = 11.4f, WallH = 6.2f;
            WallTiles("wall", -14f, 34f, 0f, WallH, WallS, mCliff, cliffVariants);
            {
                var ceil = new MB(); ceil.Floor(64f, 4f, -50f, 40f, WallH);
                var ct = Tex(A, "cliff_c", null); var mCeil = Lit(ct != null ? ct : Tex(A, "cliff", Px.Cliff(p, rng))); mCeil.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.34f));
                Solid("ceiling", ceil, mCeil);
            }
            // 壁面の物 (壁と同じ向き・壁の少し手前・二段目の上 y=1.8): 坑口 (奥行きの錨)・脇坑・大結晶・小結晶・歯車
            {
                float y2 = 1.8f;
                var tm = PropTexRaw(A, "tunnel_mouth", null);
                if (tm != null)
                {
                    var w = OnPath(19.5f, WallS - 0.06f); var g = Plane("tunnel_mouth", tm, new Vector3(w.x, y2, w.z), 5.4f, 0.5f, false);   // 屋台B・道具掛け・巻き上げ機の間 (t=15 は屋台の真後ろだった = レビュー)
                    g.transform.rotation = Quaternion.Euler(0f, PathYaw, 0f); g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.5f);
                    // 口の中: 真っ黒の板と奥へ小さくなる提灯の点
                    var dark = Prop("tunnel-dark", Px.Solid(new Color(0.02f, 0.015f, 0.03f)), new Vector3(w.x, y2 + 0.2f, w.z) + Quaternion.Euler(0f, PathYaw, 0f) * new Vector3(0f, 0f, 0.4f), 3.6f, 0.1f, 2.4f);
                    dark.transform.rotation = Quaternion.Euler(0f, PathYaw, 0f); dark.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f);
                    for (int i = 0; i < 3; i++) Halo("tunnel-lamp", new Vector3(w.x, y2 + 1.4f - i * 0.15f, w.z) + Quaternion.Euler(0f, PathYaw, 0f) * new Vector3(0.3f - i * 0.5f, 0f, 0.3f - i * 0.05f), 0.55f - i * 0.12f, new Color(1f, 0.66f, 0.36f, 0.35f - i * 0.08f));
                    PointLight("tunnel-light", new Vector3(w.x, y2 + 1.2f, w.z), warm, 0.5f, 3f);
                }
                var ts = PropTexRaw(A, "tunnel_side", null);
                if (ts != null) { var w = OnPath(27f, WallS - 0.06f); var g = Plane("tunnel_side", ts, new Vector3(w.x, y2, w.z), 4f, 0.5f, false); g.transform.rotation = Quaternion.Euler(0f, PathYaw, 0f); g.transform.localScale = new Vector3(-g.transform.localScale.x, g.transform.localScale.y, 1f); g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.4f); }
                var cb = PropTexRaw(A, "crystal_big", Px.Crystal(veinC, rng));
                {
                    var w = OnPath(-1f, WallS - 0.4f); var g = Prop("crystal-big", cb, new Vector3(w.x, y2, w.z), 4.4f, 0.5f); g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f);
                    Halo("crystal-halo", new Vector3(w.x, y2 + 2f, w.z - 0.4f), 8f, new Color(veinC.r, veinC.g, veinC.b, 0.45f));
                    PointLight("crystal-light", new Vector3(w.x, y2 + 2.1f, w.z - 0.6f), veinC, 2.0f, 11f);
                }
                var cs = PropTexRaw(A, "crystal", Px.Crystal(veinC, rng));
                float[] ct = { -8.5f, -5f, 4.5f, 10.5f, 25f };
                for (int i = 0; i < ct.Length; i++)
                {
                    var w = OnPath(ct[i], WallS - 0.5f - (float)rng.NextDouble() * 0.3f); float hgt = 1.0f + (float)rng.NextDouble() * 0.7f;
                    var g = Prop("crystal", cs, new Vector3(w.x, y2, w.z), hgt, 0.5f); g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f);
                    Halo("crystal-halo", new Vector3(w.x, y2 + hgt * 0.45f, w.z - 0.2f), hgt * 2f, new Color(veinC.r, veinC.g, veinC.b, 0.35f));
                    PointLight("crystal-light", new Vector3(w.x, y2 + hgt * 0.5f, w.z - 0.3f), veinC, 0.8f + (float)rng.NextDouble() * 0.5f, 4.5f + (float)rng.NextDouble() * 1.5f);
                }
                // 壁の脈 (壁と同じ向きの細い光の筋)
                var seamTex = Px.Radial(new Color(veinC.r, veinC.g, veinC.b, 0.7f));
                for (int i = 0; i < 10; i++)
                {
                    var w = OnPath(-12f + (float)rng.NextDouble() * 44f, WallS - 0.08f); float yy = y2 + 0.4f + (float)rng.NextDouble() * 4f;
                    var g = Glow("wall-vein", seamTex, new Vector3(w.x, yy, w.z), 0.18f + (float)rng.NextDouble() * 0.12f, 1.6f + (float)rng.NextDouble() * 2.4f);
                    g.GetComponent<MeshFilter>().sharedMesh = _quadCentered; g.transform.rotation = Quaternion.Euler(0f, PathYaw, -35f + (float)rng.NextDouble() * 70f);
                }
                Put(A, "gear_big", -5.5f, 10.9f, 1.5f, 0.8f); Put(A, "gear_big", 19f, 10.8f, 1.5f, 0.8f, true);
                Put(A, "gear_pile", -5.3f, 10.0f, 1.0f, 0.9f); Put(A, "gear_pile", 12.5f, 10.6f, 1.0f, 0.9f, true);
                Put(A, "wreck", -8f, 9.6f, 2.6f, 0.9f, false, 0f, 0.7f);
                Put(A, "winch", 25.5f, 9.4f, 2.4f, 0.9f);
                Hang(A, "pulley", 20f, 8.8f, WallH, 3.0f);
            }
            // ---- 歩廊 (壁の手前 s 10.05〜11.15・床板 y 3.6): 結晶で途切れる。手すりは一枚絵
            {
                PathBoxes("gallery", mPlank, mb =>
                {
                    foreach (var seg in new[] { new Vector2(-10f, -3f), new Vector2(3f, 9f) })
                    {
                        mb.Box((seg.x + seg.y) * 0.5f, 3.8f, 10.6f, seg.y - seg.x, 0.14f, 1.1f);
                        for (float t = seg.x + 0.4f; t < seg.y; t += 2.6f + (float)rng.NextDouble() * 0.8f) { mb.Box(t, 1.8f, 10.6f, 0.28f, 2.0f, 0.28f); mb.Box(t, 3.94f, 10.15f, 0.16f, 0.9f, 0.16f); }
                    }
                });
                var ra = PropTexRaw(A, "rail_a", null); var rb = PropTexRaw(A, "rail_b", null);
                if (ra != null || rb != null)
                    foreach (var seg in new[] { new Vector2(-10f, -3f), new Vector2(3f, 9f) })
                        for (float t = seg.x; t < seg.y - 0.2f; t += 1.9f)
                        {
                            bool broken = (rb != null) && (Mathf.Abs(t - (-3.5f)) < 1f || Mathf.Abs(t - 3.5f) < 1f);
                            var tex = broken ? rb : (ra ?? rb);
                            var w = OnPath(t + 0.95f, 10.05f); var g = Plane("rail", tex, new Vector3(w.x, 3.94f, w.z), 0.95f, 0.5f, false);
                            g.transform.rotation = Quaternion.Euler(0f, PathYaw, 0f); g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                        }
                var ld = PropTexRaw(A, "ladder", null);
                if (ld != null) foreach (float t in new[] { -3.4f, 9.4f }) { var w = OnPath(t, 9.9f); var g = Plane("ladder", ld, new Vector3(w.x, 1.8f, w.z), 2.1f, 0.5f, false); g.transform.rotation = Quaternion.Euler(-12f, PathYaw, 0f); }
            }
            // ---- 二段目 (軌道): レール・枕木・トロッコ・鉱
            {
                var rails = new MB(); float y2 = 1.8f;
                for (float t = -14f; t <= 30f; t += 0.55f) { var r1 = OnPath(t, 8.5f); var r2 = OnPath(t, 9.7f); rails.Box(r1.x, y2 + 0.16f, r1.z, 0.34f, 0.12f, 0.34f); rails.Box(r2.x, y2 + 0.16f, r2.z, 0.34f, 0.12f, 0.34f); }
                Solid("rails", rails, mIron);
                PathBoxes("sleepers", mWood, mb => { for (float t = -14f; t <= 30f; t += 1.5f + (float)rng.NextDouble() * 0.4f) mb.Box(t, y2, 9.1f, 0.4f, 0.16f, 1.7f); });
                var cart = Put(A, "minecart", 5f, 9.0f, 1.5f, 0.9f, false, 0.12f);
                if (cart != null) { var w = OnPath(5f, 9.0f); Halo("ore-glow", new Vector3(w.x, w.y + 1.1f, w.z - 0.2f), 2.0f, new Color(veinC.r, veinC.g, veinC.b, 0.35f)); }
                Put(A, "minecart_tipped", -6f, 8.7f, 1.1f, 0.9f, true);
                foreach (var pt in new[] { new Vector2(-4.5f, 8.6f), new Vector2(7f, 8.5f), new Vector2(16.5f, 8.6f) })
                { var g = Put(A, "ore_pile", pt.x, pt.y, 0.8f, 0.9f); if (g != null) { var w = OnPath(pt.x, pt.y); Halo("ore-halo", new Vector3(w.x, w.y + 0.35f, w.z - 0.15f), 0.9f, new Color(veinC.r, veinC.g, veinC.b, 0.3f)); } }
                for (int i = 0; i < 14; i++) { var w = OnPath(-8f + (float)rng.NextDouble() * 26f, 8.3f + (float)rng.NextDouble() * 1.8f); float sz = 0.1f + (float)rng.NextDouble() * 0.1f; Glow("ore-bit", Px.Glow(new Color(veinC.r, veinC.g, veinC.b, 0.9f)), w + new Vector3(0f, 0.05f, 0f), sz, sz); }
                var ballast = PatchSet(A, "m_ballast", false, 72, 40, 96, 48);
                for (int i = 0; i < 18 && ballast.Count > 0; i++) { var w = OnPath(-12f + i * 2.4f + (float)rng.NextDouble(), 8.4f + (float)rng.NextDouble() * 1.2f); GroundDecal("ballast", Pick(ballast, rng), w.x, w.z, 1f, PathYaw + ((float)rng.NextDouble() - 0.5f) * 20f, 0.014f, rng.NextDouble() < 0.5); }
                var oredust = PatchSet(A, "m_oredust", false, 48, 32);
                foreach (var pt in new[] { new Vector2(5f, 8.7f), new Vector2(-6f, 8.4f) }) if (oredust.Count > 0) { var w = OnPath(pt.x, pt.y); GroundDecal("oredust", Pick(oredust, rng), w.x, w.z, 1.1f, (float)rng.NextDouble() * 360f, 0.016f); }
                // 右の上り: 一段目→二段目の木の階段
                PathBoxes("stairs-wood", mWood, mb => { for (int k = 0; k < 4; k++) mb.Box(12.4f, 0.9f + k * 0.225f, 8.2f - 0.8f + k * 0.26f, 1.6f, 0.225f, 0.52f); });
            }
            // ---- 段鼻: 一段目の縁 (土留めの横木と杭)・溝の縁石・石段
            {
                PathBoxes("tier1-edge", mWood, mb =>
                {
                    mb.Box(6f, 0.62f, 5.0f, 36f, 0.32f, 0.3f);
                    mb.Box(6f, 0.3f, 5.0f, 36f, 0.3f, 0.26f);
                    for (float t = -12f; t < 24f; t += 2.2f + (float)rng.NextDouble() * 0.9f) mb.Box(t, 0f, 4.85f, 0.16f, 1.1f, 0.16f);
                    // 棚の縁 (道の手前 s=−3.4): 支保工の板張りが下の坑道へ垂れる面 = 岩の面を隠す。上端に横木、板は縦、柱を 2.4〜3.2 刻み
                    // 岩の面はセルの階段 (±0.32) で走るので、板張りはその手前 (−0.5) に立て、上端の横木で棚の縁と板の隙間を蓋する
                    mb.Box(6f, -0.16f, LedgeS - 0.42f, 44f, 0.16f, 1.2f);
                    for (float t = -16f; t < 28f; t += 2.4f + (float)rng.NextDouble() * 0.8f) mb.Box(t, GalleryY, LedgeS - 0.9f, 0.26f, 1.5f, 0.26f);
                });
                PathBoxes("ledge-planks", mPlank, mb => { mb.Box(6f, GalleryY, LedgeS - 0.8f, 44f, 1.4f, 0.1f); });   // 岩の階段 (最大 −3.85) より手前 (−4.2)
                var stepTex2 = Tex(A, "stone_c", null) ?? Tex(A, "stone", Px.Stone(p, rng)); var mStep2 = Lit(stepTex2); mStep2.SetColor("_BaseColor", new Color(0.5f, 0.52f, 0.6f));
                PathBoxes("stone-steps", mStep2, mb => { for (int k = 0; k < 3; k++) mb.Box(-1.4f, k * 0.3f, 4.3f + k * 0.28f, 2.0f, 0.3f, 0.5f); });
            }
            // ---- 支保工: 奥の柱 (s=6.6) と持ち送り。手前は一枚絵の柱 2 本 (額縁)
            PathBoxes("timber", mWood, mb =>
            {
                float[] tt = { -5.6f, 0f, 5.6f, 11.2f, 16.8f };
                for (int i = 0; i < tt.Length; i++) { float t = tt[i] + ((float)rng.NextDouble() - 0.5f) * 0.6f; mb.Box(t, 0.9f, 6.6f, 0.5f, 3.5f, 0.5f); mb.Box(t, 4.4f, 4.4f, 0.42f, 0.42f, 4.4f); mb.Box(t, 3.5f, 5.7f, 0.3f, 0.3f, 1.8f); }
                // 道をまたぐ木の桟橋 (右奥 t=17.5・敵④の後ろ・頭より上): 二段目 (1.8) から棚 (0) の縁まで渡す。柱は道の上と縁に
                mb.Box(17.5f, 3.4f, 2.4f, 1.6f, 0.16f, 11.6f);
                mb.Box(16.8f, 3.56f, 2.4f, 0.1f, 0.7f, 11.6f); mb.Box(18.2f, 3.56f, 2.4f, 0.1f, 0.7f, 11.6f);
                foreach (float sv in new[] { -3.1f, 1.2f, 4.6f }) { mb.Box(16.9f, 0f, sv, 0.24f, 3.4f, 0.24f); mb.Box(18.1f, 0f, sv, 0.24f, 3.4f, 0.24f); }
                mb.Box(17.5f, 2.9f, -3.1f, 1.6f, 0.2f, 0.2f); mb.Box(17.5f, 2.9f, 1.2f, 1.6f, 0.2f, 0.2f); mb.Box(17.5f, 2.9f, 4.6f, 1.6f, 0.2f, 0.2f);
            });
            { var g = Put(A, "post_brace", -7.6f, -5.6f, 5.6f, 0.5f); if (g != null) g.transform.rotation = Quaternion.Euler(0f, 0f, 2f); }   // 下の坑道の床 (−1.4) から立つので高く
            { var g = Put(A, "post_brace", 9.5f, -6.6f, 5.6f, 0.5f, true); if (g != null) g.transform.rotation = Quaternion.Euler(0f, 0f, -2f); }   // 右の額縁は敵③④の手前に掛からない所へ (レビュー)
            // ---- 一段目 (宿場)
            {
                var h = Put(A, "hearth", -7.5f, 6.8f, 3.0f, 0.9f, false, 0f, 1f, 0f);
                var hw = OnPath(-7.5f, 6.8f);
                PointLight("hearth-light", new Vector3(hw.x, hw.y + 1.4f, hw.z - 0.3f), new Color(1f, 0.55f, 0.25f), 3.4f, 9f);
                Pool("hearth-pool", hw, new Color(1f, 0.6f, 0.3f, 0.35f), 3.2f, 2.6f);
                Halo("hearth-glow", new Vector3(hw.x, hw.y + 1.1f, hw.z - 0.5f), 2.4f, new Color(1f, 0.6f, 0.3f, 0.5f));
                _emberPos = new Vector3(hw.x, hw.y + 1.2f, hw.z);
                var soot = PatchSet(A, "m_soot", false, 64, 44, 80, 56);
                if (soot.Count > 0) GroundDecal("soot", Pick(soot, rng), hw.x, hw.z - 0.5f, 1.3f, 0f, 0.014f);
                Put(A, "stall_a", 2.5f, 7.0f, 2.9f, 0.9f); Put(A, "stall_b", 15.2f, 7.3f, 2.6f, 0.9f, true);   // 敵①〜④の頭 (t 1.6〜11.2) の真後ろに背の高い物を置かない (レビュー)
                Put(A, "shelter", -3.2f, 7.6f, 2.4f, 0.9f);
                Put(A, "toolrack", -0.8f, 8.0f, 2.0f, 0.8f); Put(A, "toolrack", 19.2f, 7.6f, 2.0f, 0.8f, true);
                Put(A, "lumber", 8.2f, 6.1f, 1.2f, 0.9f); Put(A, "lumber", 19f, 9.0f, 1.2f, 0.9f);
                Put(A, "bell_post", -0.8f, 5.5f, 2.3f, 0.5f);
                foreach (var pt in new[] { new Vector2(-5.4f, 5.9f), new Vector2(0.6f, 7.9f), new Vector2(13.2f, 6.3f), new Vector2(17.5f, 6.9f), new Vector2(-7.9f, -6.6f), new Vector2(10.6f, -7.0f) }) Put(A, "barrel", pt.x, pt.y, 1.1f, 0.9f, rng.NextDouble() < 0.5);
                Put(A, "barrel_stack", -9.0f, 5.8f, 1.8f, 0.9f); Put(A, "barrel_stack", 17f, 6.8f, 1.8f, 0.9f, true);
                foreach (var pt in new[] { new Vector2(-1.6f, 8.0f), new Vector2(4.4f, 6.2f), new Vector2(9.8f, 8.1f), new Vector2(14f, 5.8f), new Vector2(21f, 9.3f) }) Put(A, "crate", pt.x, pt.y, 1.0f, 0.9f, rng.NextDouble() < 0.5);
                Put(A, "crate_stack", 12.6f, 7.9f, 1.9f, 0.9f); Put(A, "crate_stack", 11.5f, -7.6f, 1.9f, 0.9f, true);
                Put(A, "crate_open", 3.4f, 8.7f, 0.9f, 0.9f); Put(A, "crate_open", 10.2f, 6.3f, 0.9f, 0.9f, true);
                foreach (var pt in new[] { new Vector2(1.0f, 7.8f), new Vector2(-4.6f, 7.9f), new Vector2(18.2f, 6.0f) }) Put(A, "sacks", pt.x, pt.y, 1.0f, 0.9f, rng.NextDouble() < 0.5);
                Put(A, "trough", -5.6f, 7.9f, 0.8f, 0.9f);
                foreach (var pt in new[] { new Vector2(23f, 9.6f), new Vector2(9.6f, 9.6f), new Vector2(3.6f, 6.6f) }) Put(A, "rope_coil", pt.x, pt.y, 0.5f, 0.8f, rng.NextDouble() < 0.5);
                var sawdust = PatchSet(A, "m_sawdust", false, 56, 40); var straw = new List<Texture2D>();   // 藁 (m_straw) は等間隔の粒になったので使わない (2026-09-21)
                foreach (var pt in new[] { new Vector2(6.2f, 7.4f), new Vector2(15.5f, 7.3f), new Vector2(8.2f, 5.7f) }) if (sawdust.Count > 0) { var w = OnPath(pt.x, pt.y); GroundDecal("sawdust", Pick(sawdust, rng), w.x, w.z, 1f, (float)rng.NextDouble() * 360f, 0.014f, rng.NextDouble() < 0.5); }
                foreach (var pt in new[] { new Vector2(-3.2f, 7.2f), new Vector2(-5.2f, 7.5f) }) if (straw.Count > 0) { var w = OnPath(pt.x, pt.y); GroundDecal("straw", Pick(straw, rng), w.x, w.z, 1f, (float)rng.NextDouble() * 360f, 0.016f, rng.NextDouble() < 0.5); }
                var rubble = PatchSet(A, "m_rubble", false, 56, 40, 80, 56); var moss = PatchSet(A, "m_moss", false, 48, 32, 64, 44);
                for (int i = 0; i < 14 && rubble.Count > 0; i++) { var w = OnPath(-12f + (float)rng.NextDouble() * 40f, (i < 8) ? 10.4f + (float)rng.NextDouble() * 0.8f : 8.3f + (float)rng.NextDouble() * 0.5f); GroundDecal("rubble", Pick(rubble, rng), w.x, w.z, 1f, (float)rng.NextDouble() * 360f, 0.014f, rng.NextDouble() < 0.5); }
                for (int i = 0; i < 10 && moss.Count > 0; i++) { var w = OnPath(-10f + (float)rng.NextDouble() * 22f, -3.2f + (float)rng.NextDouble() * 0.6f); GroundDecal("moss", Pick(moss, rng), w.x, w.z, 0.9f, (float)rng.NextDouble() * 360f, 0.016f, rng.NextDouble() < 0.5); }
                foreach (var pt in new[] { new Vector2(-7.0f, -6.6f), new Vector2(4.6f, -6.8f), new Vector2(-9.5f, 10.9f), new Vector2(26f, 10.8f) }) Put(A, "rubble_rock", pt.x, pt.y, 0.7f, 0.9f, rng.NextDouble() < 0.5);
            }
            // ---- 提灯の柱 (一段目の段鼻 s=5.4 と近景の額縁の柱の内側)
            {
                var lanternTex = PropTexRaw(A, "lantern_post", null);
                float[] lt = { -8.4f, -2.6f, 3.8f, 10.4f, 17.2f, -7.4f }; float[] ls = { 5.4f, 5.4f, 5.4f, 5.4f, 5.4f, -5.6f };   // 近景の柱は左の 1 本だけ (右は敵の手前に掛かる)
                for (int i = 0; i < lt.Length; i++)
                {
                    var w = OnPath(lt[i], ls[i]);
                    if (lanternTex != null) { var g = SpriteH("lantern_post", lanternTex, w, 3.4f, 0.4f, i % 2 == 1); g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f); }
                    else
                    {
                        var pole = new MB(); pole.Box(w.x, w.y, w.z, 0.14f, 3.2f, 0.14f); pole.Box(w.x, w.y, w.z, 0.5f, 0.12f, 0.5f); pole.Box(w.x + 0.25f, w.y + 3.1f, w.z, 0.6f, 0.08f, 0.08f); Solid("lantern-pole", pole, mIron);
                        var lt2 = PropTex(A, "lantern", null); if (lt2 != null) { var l = Plane("lantern", lt2, new Vector3(w.x + 0.5f, w.y + 2.35f, w.z - 0.02f), 0.9f, 0.5f, false); l.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f); }
                    }
                    float lx = w.x + (lanternTex != null ? 0f : 0.5f);
                    PointLight("lantern-light", new Vector3(lx, w.y + 2.6f, w.z), warm, 2.4f, 6f);
                    Pool("lantern-pool", new Vector3(lx, 0f, w.z), new Color(1f, 0.7f, 0.4f, 0.3f), 3.6f, 3.2f);
                    Halo("lantern-glow", new Vector3(lx, w.y + 2.5f, w.z - 0.3f), 2.0f, new Color(1f, 0.7f, 0.4f, 0.5f));
                }
            }
            // ---- 吊り提灯: 道を横切る紐 3 列 (不等間隔)
            {
                var la = PropTexRaw(A, "lantern_hang", null) ?? PropTex(A, "lantern", null); var lb = PropTexRaw(A, "lantern_hang_b", null);
                float[] rowS = { 1.2f, 4.4f, 7.6f }; float[] rowY = { 3.6f, 3.9f, 4.2f };
                for (int row = 0; row < 3 && la != null; row++)
                {
                    var rope = new MB(); var r0 = OnPath(-9f, rowS[row]); var r1 = OnPath(15f, rowS[row]);
                    rope.Box((r0.x + r1.x) * 0.5f, rowY[row] + 0.75f, (r0.z + r1.z) * 0.5f, 0.03f, 0.03f, 0.03f);   // 目印だけ (紐は細い箱を道の向きで)
                    PathBoxes("lantern-rope" + row, mIron, mb => mb.Box(3f, rowY[row] + 0.72f, rowS[row], 24f, 0.03f, 0.03f));
                    int k = 0; bool any = false;
                    for (float t = -8f + (float)rng.NextDouble() * 2f; t < 14f; t += 4.2f + (float)rng.NextDouble() * 1.6f, k++)
                    {
                        var w = OnPath(t, rowS[row]); bool small = lb != null && rng.NextDouble() < 0.4; var tex = small ? lb : la; float h = small ? 0.6f : 0.75f;
                        var g = Plane("lantern-string", tex, new Vector3(w.x, rowY[row], w.z), h, 0.5f, false); g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f); g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                        Halo("lantern-glow", new Vector3(w.x, rowY[row] + h * 0.4f, w.z - 0.2f), 1.1f, new Color(1f, 0.66f, 0.36f, 0.35f));
                        if (!any && t > 0f) { PointLight("string-light", new Vector3(w.x, rowY[row], w.z), warm, 0.6f, 4f); any = true; }
                    }
                }
            }
            // ---- 天井から: 鍾乳石・垂れ根
            {
                float[] st = { -8f, 3f, 12f, 22f }; float[] ss = { 4.5f, 7.5f, 5.5f, 9f };
                for (int i = 0; i < st.Length; i++) if (Hang(A, "stalactite_a", st[i], ss[i], WallH, 2.6f + (float)rng.NextDouble() * 0.8f, i % 2 == 1) == null) Hang(A, "stalactite", st[i], ss[i], WallH, 3.2f);
                float[] st2 = { -3f, 8f, 17f }; float[] ss2 = { 10.7f, 10.9f, 10.6f };
                for (int i = 0; i < st2.Length; i++) Put(A, "stalactite_b", st2[i], ss2[i], 1.9f + (float)rng.NextDouble() * 0.7f, 0.8f, i % 2 == 0);   // 生成された絵は台座つきの石筍 = 壁の根元に立てる
                float[] rt = { -6f, 6f, 14f }; float[] rs = { 6.5f, 9f, 6f };
                for (int i = 0; i < rt.Length; i++) if (Hang(A, "roots_hang", rt[i], rs[i], WallH, 2.2f + (float)rng.NextDouble() * 0.6f, i % 2 == 1) == null) Hang(A, "roots", rt[i], rs[i], WallH, 2.8f);
            }
            // ---- 下の坑道 (棚の手前 −1.4): 軌道・トロッコ・崩落岩・水路 (−1.75・水面) に提灯の映り
            {
                var rails2 = new MB();
                for (float t = -16f; t <= 28f; t += 0.55f) { var r1 = OnPath(t, -4.3f); var r2 = OnPath(t, -5.5f); rails2.Box(r1.x, GalleryY + 0.16f, r1.z, 0.34f, 0.12f, 0.34f); rails2.Box(r2.x, GalleryY + 0.16f, r2.z, 0.34f, 0.12f, 0.34f); }
                Solid("rails-lower", rails2, mIron);
                PathBoxes("sleepers-lower", mWood, mb => { for (float t = -16f; t <= 28f; t += 1.5f + (float)rng.NextDouble() * 0.4f) mb.Box(t, GalleryY, -4.9f, 0.4f, 0.16f, 1.7f); });
                var c2 = Put(A, "minecart", -3f, -4.9f, 1.5f, 0.9f, true, 0.12f);
                if (c2 != null) { var w = OnPath(-3f, -4.9f); Halo("ore-glow", new Vector3(w.x, w.y + 1.1f, w.z - 0.2f), 2.0f, new Color(veinC.r, veinC.g, veinC.b, 0.35f)); }
                Put(A, "ore_pile", 8f, -4.4f, 0.8f, 0.9f); Put(A, "rubble_rock", 14f, -4.6f, 0.7f, 0.9f);
                var ballast2 = PatchSet(A, "m_ballast", false, 72, 40);
                for (int i = 0; i < 10 && ballast2.Count > 0; i++) { var w = OnPath(-14f + i * 4f + (float)rng.NextDouble(), -4.9f + ((float)rng.NextDouble() - 0.5f) * 1.2f); GroundDecal("ballast", Pick(ballast2, rng), w.x, w.z, 1f, PathYaw + ((float)rng.NextDouble() - 0.5f) * 20f, 0.014f, rng.NextDouble() < 0.5); }
                // 桟橋の提灯 (吊り) と、下の坑道を照らす提灯の柱 1 本
                var lh = PropTexRaw(A, "lantern_hang", null) ?? PropTex(A, "lantern", null);
                if (lh != null) foreach (float sv in new[] { -1.0f, 3.2f }) { var w = OnPath(17.5f, sv); var g = Plane("bridge-lantern", lh, new Vector3(w.x, 2.7f, w.z), 0.75f, 0.5f, false); g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f); g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; Halo("lantern-glow", new Vector3(w.x, 3.0f, w.z - 0.2f), 1.1f, new Color(1f, 0.66f, 0.36f, 0.35f)); }
                PointLight("bridge-light", OnPath(17.5f, 1.2f) + new Vector3(0f, 2.9f, 0f), warm, 0.9f, 4.5f);
                foreach (float t in new[] { -8.4f, 3.8f, 10.4f })
                {
                    var w = OnPath(t, ChannelS - 0.6f); var g = Glow("lantern-reflect", Px.Radial(new Color(1f, 0.7f, 0.4f, 0.3f)), new Vector3(w.x, WaterY + 0.02f, w.z), 1f, 1f);
                    g.GetComponent<MeshFilter>().sharedMesh = _quadCentered; g.transform.rotation = Quaternion.Euler(90f, 0f, 0f); g.transform.localScale = new Vector3(1.2f, 0.5f, 1f);
                }
            }
            // ---- 手前の道 (溝は撤去): 濡れた染みと瓦礫の敷物で床の情報密度を足す (レビュー「床が 1 枚のタイルの繰り返し」)
            {
                var rub = PatchSet(A, "m_rubble", false, 40, 28, 56, 40); var mossF = PatchSet(A, "m_moss", false, 40, 28);
                for (int i = 0; i < 3 && rub.Count > 0; i++) { var w = OnPath(-10f + (float)rng.NextDouble() * 24f, -3.2f + (float)rng.NextDouble() * 0.6f); GroundDecal("rubble", Pick(rub, rng), w.x, w.z, 0.6f, (float)rng.NextDouble() * 360f, 0.014f, rng.NextDouble() < 0.5); }
                for (int i = 0; i < 5 && mossF.Count > 0; i++) { var w = OnPath(-10f + (float)rng.NextDouble() * 24f, -6.4f + (float)rng.NextDouble() * 2.6f); GroundDecal("moss", Pick(mossF, rng), w.x, w.z, 0.9f, (float)rng.NextDouble() * 360f, 0.016f, rng.NextDouble() < 0.5); }
            }
            // ---- 天井の下の暗がり: 壁の上端 1.6 unit を沈める帯 (洞窟が「閉じる」= レビュー)
            {
                var shade = Glow("wall-shade", Px.Gradient(new Color(0.02f, 0.02f, 0.05f, 0f), new Color(0.02f, 0.02f, 0.05f, 0.75f)), Vector3.zero, WallH * 0.32f, 52f);
                var w = OnPath(10f, WallS - 0.1f); shade.transform.position = new Vector3(w.x, WallH * 0.68f, w.z); shade.transform.rotation = Quaternion.Euler(0f, PathYaw, 0f);
            }
            // ---- 風穴の冷たい光の筋 1 本 (屋台の右) と足元の光溜まり
            {
                var w = OnPath(6f, 7.4f);
                var b = Glow("shaft-beam", Px.Beam(new Color(0.78f, 0.86f, 1f)), new Vector3(w.x, w.y, w.z), 22f, 3.0f); b.transform.rotation = Quaternion.Euler(0f, 0f, -8f);
                b.GetComponent<MeshRenderer>().sharedMaterial.color = new Color(1f, 1f, 1f, 0.6f);
                Pool("shaft-pool", w, new Color(0.78f, 0.86f, 1f, 0.25f), 2.4f, 1.8f);
            }
            // 光の粒の足元 (リーダーのランタン)
            var lampBase = new Vector3(_lampPos.x, 0f, _lampPos.z);
            Pool("mote-pool", lampBase, new Color(1f, 0.78f, 0.46f, 0.25f), 5f, 4.4f);
            // 天井の向こう (空の代わりの暗い板)
            var sky = Prop("sky", BgTex(A, Px.Gradient(UiKit.Hex("#1a1014"), UiKit.Hex("#050305"))), new Vector3(0f, -30f, 90f), 130f, 0f, 260f);
            sky.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f); sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------------------------------------------------------------- 幕3 埋もれた古代都市

        /// <summary>幕3 (2026-09-21 統合案「神殿の前庭」): 場 (大石板) の後ろに古代の水路 (黒い止水 = 反射)、橋→遠岸→T1 (+1.1)→幅広の階段→T2 (+2.2) の大門が門の軸に一本の線。
        /// T1 は左に密 (折れ柱・噴水・跪く像・歯車形の碑)・右は低く。T2 の右に機械の庭 (掘削の腕・歯車の壁・配管)。主役の大結晶は右端に 1 本。手前の池と水路は同じ水面 (反射 1 面)</summary>
        static void PaintCorridor2(Pal p, System.Random rng, Material mFloor, Material mCliff)
        {
            const int A = 3;
            var veinC = new Color(0.42f, 0.95f, 0.86f);
            var lampC = new Color(0.55f, 0.75f, 1f);
            RenderSettings.fogStartDistance = 14f; RenderSettings.fogEndDistance = 56f;
            mFloor.SetFloat("_Smoothness", 0.28f); mCliff.SetFloat("_Smoothness", 0.2f);
            var mDark = Lit(Tex(A, "stone", Px.Stone(p, rng))); mDark.SetColor("_BaseColor", new Color(0.2f, 0.22f, 0.3f));
            _decalTint = mFloor.GetColor("_BaseColor");   // 敷物 = 床の色
            var stepTex = Tex(A, "stone_a", null) ?? Tex(A, "stone", Px.Stone(p, rng));
            var mStep = Lit(stepTex); mStep.SetColor("_BaseColor", new Color(0.4f, 0.44f, 0.52f)); mStep.SetFloat("_Smoothness", 0.25f);

            // ---- 橋 (水路の上・門の軸)・階段 2 つ・奥の閉じの壁
            PathBoxes("bridge", mStep, mb => { mb.Box(3.0f, -0.35f, 7.2f, 5.2f, 0.4f, 3.4f); });
            PathBoxes("stairs-1", mStep, mb => { for (int k = 0; k < 3; k++) mb.Box(3.0f, k * 0.37f, 10.6f + k * 0.27f, 5.2f, 0.37f, 0.5f); });
            PathBoxes("stairs-2", mStep, mb => { for (int k = 0; k < 5; k++) mb.Box(3.0f, 1.1f + k * 0.22f, 15.0f + k * 0.24f, 11f, 0.22f, 0.45f); });
            WallTiles("back-wall", -20f, 44f, 2.2f, 4.0f, 27f, mCliff, cliffVariants);   // 低い胸壁 (旧 11 は遠景を丸ごと隠した = レビュー)
            // 段の根元の影の帯 (段鼻の下)
            foreach (float sv in new[] { 5.8f, 10.8f, 15.6f })
            {
                var g = Glow("step-shade", StripTex(), Vector3.zero, 1f, 1f); g.GetComponent<MeshFilter>().sharedMesh = _quadCentered;
                var w = OnPath(8f, sv - 0.02f); g.transform.rotation = Quaternion.Euler(90f, PathYaw, 0f); g.transform.position = new Vector3(w.x, GroundY(w.x, w.z) + 0.03f, w.z) + Quaternion.Euler(0f, PathYaw, 0f) * new Vector3(0f, 0f, -0.28f);
                g.transform.localScale = new Vector3(44f, 0.55f, 1f); g.GetComponent<MeshRenderer>().sharedMaterial.color = new Color(0.02f, 0.02f, 0.08f, 0.38f);
            }
            // ---- 遠景: 岩天井の板・脈の地平・街の輪郭・水道橋・回り続ける櫓・鍾乳石の束・天井の裂け目と光の筋
            var sky = Prop("sky", BgTex(A, Px.Gradient(UiKit.Hex("#08181c"), UiKit.Hex("#02070a"))), new Vector3(0f, -30f, 90f), 130f, 0f, 260f);
            sky.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f); sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Glow("vein-horizon", Px.Radial(new Color(0.35f * 1.3f, 0.9f * 1.3f, 0.82f * 1.3f, 0.7f)), new Vector3(4f, 2.5f, 66f), 22f, 170f);
            Glow("vein-horizon2", Px.Radial(new Color(0.45f, 1f, 0.9f, 0.45f)), new Vector3(-18f, 2f, 64f), 14f, 80f);
            {
                var ska = PropTexRaw(A, "skyline_a", null); var skb = PropTexRaw(A, "skyline_b", null);
                Action<string, Texture2D, Vector3, float, float, float, bool> far = (nm, tex, pos, h, w, fog, flip) =>
                {
                    var g = Prop(nm, tex, pos, h, 0.3f, w); g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", fog); g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    if (flip) g.transform.localScale = new Vector3(-g.transform.localScale.x, g.transform.localScale.y, 1f);
                };
                if (ska != null) { far("skyline", ska, new Vector3(2f, 1.4f, 58f), 11f, 170f, 0.55f, false); far("skyline", ska, new Vector3(-14f, 1.0f, 46f), 9f, 120f, 0.4f, true); }
                else { far("city-far", Px.Skyline(p, rng), new Vector3(2f, 1.4f, 58f), 13f, 170f, 0.55f, false); far("city-near", Px.Skyline(p, rng), new Vector3(-14f, 1.0f, 46f), 9f, 120f, 0.4f, false); }
                if (skb != null) { far("skyline-b", skb, new Vector3(28f, 1.2f, 52f), 9f, 110f, 0.5f, true); far("skyline-b", skb, new Vector3(-36f, 1.6f, 55f), 8f, 100f, 0.6f, false); }
                var rigTex = PropTexRaw(A, "rig_far", null);
                var rig = Prop("rig", rigTex ?? Px.Headframe(p, rng), new Vector3(18f, 0.6f, 50f), 14f, 0.4f);
                rig.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.5f); rig.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                var wheel = Prop("rig-wheel", Px.Wheel(Color.Lerp(p.SkyTop, Color.black, 0.5f), 32), new Vector3(18f, 0.6f + 14f * (rigTex != null ? 0.86f : 163f / 176f), 49.9f), 2.3f, 0.4f);
                wheel.GetComponent<MeshFilter>().sharedMesh = _quadCentered; wheel.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.5f); wheel.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                var gw = OnPath(3f, 21.5f);
                var aq = PropTexRaw(A, "aqueduct_far", null) ?? PropTexRaw(A, "aqueduct", Px.Arcade(p, 9));
                far("aqueduct", aq, new Vector3(gw.x - 16f, 2.8f, gw.z + 7f), 5f, -1f, 0.5f, false); far("aqueduct", aq, new Vector3(gw.x + 22f, 2.8f, gw.z + 9f), 5f, -1f, 0.5f, true);
                var fc = PropTexRaw(A, "facade_far", null);
                if (fc != null) { var f1 = OnPath(-4f, 25f); far("facade", fc, new Vector3(f1.x, 2.2f, f1.z), 8f, -1f, 0.5f, false); var f2 = OnPath(18f, 25f); far("facade", fc, new Vector3(f2.x, 2.2f, f2.z), 8f, -1f, 0.5f, true); }
                var stc = PropTexRaw(A, "stalactite_cluster", null);
                if (stc != null) foreach (var pt in new[] { new Vector2(-22f, 34f), new Vector2(8f, 42f), new Vector2(28f, 38f) }) { var g = Plane("stalactites", stc, new Vector3(pt.x, 11f - 6f, pt.y), 6f, 0.5f, false); g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.5f); }
                var fis = PropTexRaw(A, "ceiling_fissure", null);
                var beam = Px.Beam(new Color(0.6f, 1.15f, 1.05f));
                foreach (var pt in new[] { new Vector4(-4f, 10.6f, 52f, -8f), new Vector4(16f, 10.4f, 60f, -6f) })
                {
                    if (fis != null) { var g = Plane("fissure", fis, new Vector3(pt.x, pt.y + 0.4f, pt.z), 1.8f, 0.5f, false); /* 上端に半分掛かる高さ (浮いた板に見せない) */ g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.4f); }
                    var b = Glow("vein-beam", beam, new Vector3(pt.x - 8f, 0.3f, pt.z - 36f), 10f, pt.x < 0f ? 5.5f : 6.5f); b.transform.rotation = Quaternion.Euler(0f, 0f, pt.w);   // 短い筋 = 映る帯が濃い (高さ 24 では α 0.12 = レビュー)
                    b.GetComponent<MeshRenderer>().sharedMaterial.color = new Color(1f, 1f, 1f, pt.x < 0f ? 0.9f : 0.7f);
                }
                { var w = OnPath(-9.5f, 15.5f); var b = Glow("vein-beam", beam, new Vector3(w.x, w.y, w.z), 9f, 4.5f); b.transform.rotation = Quaternion.Euler(0f, 0f, -6f); b.GetComponent<MeshRenderer>().sharedMaterial.color = new Color(1f, 1f, 1f, 0.7f); }
                for (int i = 0; i < 16; i++) { float wx = -34f + (float)rng.NextDouble() * 70f, wy = 0.8f + (float)rng.NextDouble() * 6f, wz = 44f + (float)rng.NextDouble() * 16f; Glow("city-window", Px.Glow(new Color(0.4f, 0.9f, 0.85f, 0.3f)), new Vector3(wx, wy, wz), 1f + (float)rng.NextDouble() * 1.2f, 1f + (float)rng.NextDouble() * 1.2f); }
            }
            // ---- 額縁: 天井の房 (上の両隅)・ケーブル・鎖
            {
                var cf = PropTexRaw(A, "ceiling_fringe", null);
                if (cf != null) { var l = Plane("fringe", cf, new Vector3(-8.5f, 3.8f, -1.5f), 2.8f, 0.5f, false); l.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; var r = Plane("fringe", cf, new Vector3(9.5f, 4.0f, -1.4f), 2.8f, 0.5f, false); r.transform.localScale = new Vector3(-r.transform.localScale.x, r.transform.localScale.y, 1f); r.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; }
                Hang(A, "cable_hang", 17f, 9f, 9.0f, 5.0f);
                if (Hang(A, "chain_hang", -4.5f, 9.4f, 8.2f, 5.2f) == null) Hang(A, "chain", -4.5f, 9.4f, 8.2f, 4.4f);
                if (Hang(A, "chain_hang", 12.5f, 9.0f, 8.2f, 5.2f, true) == null) Hang(A, "chain", 12.5f, 9.0f, 8.2f, 4.4f, true);
            }
            // ---- 大門 (T2・門の軸)・館の壁・碑・像・柱
            {
                var gt = PropTexRaw(A, "gate_great", null);
                var gw = OnPath(3f, 21.5f);
                var gate = Prop("great-arch", gt ?? PropTexRaw(A, "gate", Px.Arch(p, veinC)), new Vector3(gw.x, gw.y - 0.1f, gw.z), gt != null ? 5.6f : 6.6f, 0.4f);
                gate.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f); gate.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.35f);
                gate.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_BaseColor", new Color(0.82f, 0.86f, 0.86f));   // 開口の絵が白すぎて画面で最も明るい穴になる (レビュー)
                Halo("arch-glow", new Vector3(gw.x, gw.y + 3.2f, gw.z - 0.3f), 5.5f, new Color(veinC.r * 1.2f, veinC.g * 1.2f, veinC.b * 1.2f, 0.3f));
                PointLight("gate-light", new Vector3(gw.x, gw.y + 2.4f, gw.z - 1f), veinC, 1.2f, 8f);
                Pool("gate-pool", OnPath(3f, 19.5f), new Color(veinC.r, veinC.g, veinC.b, 0.3f), 6f, 3f);
                Put(A, "wall_ruin", -8f, 18f, 4.5f, 0.9f, false, 0f, 0.6f); Put(A, "wall_ruin", 13f, 19f, 4.5f, 0.9f, true, 0f, 0.6f); Put(A, "wall_ruin", 25f, 21.5f, 3.6f, 0.9f, false, 0f, 0.5f);   // 3 枚目は小さく遠く (同じ絵が同じ大きさで並ばない)
                Put(A, "pillar_tall", -6f, 12.6f, 6.4f, 0.32f); Put(A, "pillar_tall", -2f, 12.3f, 6.4f, 0.32f, true);
                Put(A, "pillar_broken", -9.3f, 13.2f, 4.6f, 0.4f); Put(A, "pillar_broken", -7f, 18.4f, 4.6f, 0.4f, false, 0f, 0.65f);
                Put(A, "arch_ruin", -9.5f, 10.6f, 6f, 0.4f);
                { var g = Put(A, "statue_kneel", -7.5f, 12.2f, 3.6f, 0.9f); var w = OnPath(-7.5f, 12.2f); PointLight("statue-light", new Vector3(w.x, w.y + 1.2f, w.z - 0.3f), veinC, 0.6f, 3f); }
                Put(A, "statue_headless", -1.5f, 16.8f, 4.2f, 0.9f); Put(A, "statue_headless", 7.5f, 16.8f, 4.2f, 0.9f, true);
                Put(A, "monument_disc", -4.5f, 14.2f, 4.6f, 0.9f);
                Put(A, "stele", -3.2f, 14.6f, 3.6f, 0.6f); Put(A, "stele", 9f, 14.6f, 3.6f, 0.6f, true);
                Put(A, "fountain_dry", -9.2f, 14.0f, 3.2f, 0.9f);
                var fz = PropTexRaw(A, "frieze", null);
                if (fz != null) foreach (float t in new[] { -6f, 11f, 22f }) { var w = OnPath(t, 15.62f); var g = Plane("frieze", fz, new Vector3(w.x, 1.55f, w.z), 0.45f, 0.5f, false); g.transform.rotation = Quaternion.Euler(0f, PathYaw, 0f); g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; }
                // 欄干・瓦礫・切石・壺
                float[] bt = { -7f, 19f, -9f, -4f, 15f, 21f }; float[] bs = { 5.5f, 5.5f, 8.4f, 8.4f, 8.4f, 8.4f };
                for (int i = 0; i < bt.Length; i++) { var g = Put(A, "balustrade", bt[i], bs[i], 1.0f, 0f); if (g != null) g.transform.rotation = Quaternion.Euler(0f, PathYaw, 0f); }
                foreach (var pt in new[] { new Vector2(-9.3f, 12.6f), new Vector2(-6.5f, 17.6f), new Vector2(-5.5f, 8.2f), new Vector2(21f, 18.2f) }) Put(A, "rubble_pile", pt.x, pt.y, 1.6f, 0.9f, rng.NextDouble() < 0.5);
                foreach (var pt in new[] { new Vector2(-3f, -7f), new Vector2(3.5f, -8.4f), new Vector2(-9.5f, 9.4f), new Vector2(11f, 12.5f), new Vector2(17.5f, 13.5f) }) Put(A, "block", pt.x, pt.y, 1.0f, 0.9f, rng.NextDouble() < 0.5);
                foreach (var pt in new[] { new Vector2(-8.2f, 13.3f), new Vector2(-3.6f, 15.3f), new Vector2(6.4f, 11.9f) }) Put(A, "urn", pt.x, pt.y, 1.0f + (float)rng.NextDouble() * 0.15f, 0.9f, rng.NextDouble() < 0.5);
                // 倒れた柱: 手前の池 (胴の半分が水面から出る) と T1 の左
                Put(A, "pillar_fallen", -5.5f, -6.4f, 0.9f, 0f, false, 0.1f);   // 手前の池: 胴の半分が水面から出る (近いので小さく)
                Put(A, "pillar_fallen", -8.5f, 11.6f, 1.1f, 0.9f, true);
            }
            // ---- 灯 (古代の灯の青白): 灯柱 3・吊り灯 2・篝火 2
            {
                foreach (var pt in new[] { new Vector2(-8.5f, 9.6f), new Vector2(23.5f, 9.7f), new Vector2(-0.6f, 11.3f) })
                {
                    var g = Put(A, "lamp_post", pt.x, pt.y, 4.2f, 0.4f, false, 0f, 1f, 0f) ?? Put(A, "brazier", pt.x, pt.y, 1.6f, 0.9f, false, 0f, 1f, 0f);
                    var w = OnPath(pt.x, pt.y); float hy = g != null && g.name == "lamp_post" ? 3.6f : 1.2f;
                    PointLight("lamp-light", new Vector3(w.x, w.y + hy, w.z), lampC, 1.1f, 5.5f);
                    Halo("lamp-flame", new Vector3(w.x, w.y + hy, w.z - 0.25f), 1.2f, new Color(lampC.r * 1.2f, lampC.g * 1.2f, lampC.b * 1.2f, 0.5f));
                    Pool("lamp-pool", w, new Color(lampC.r, lampC.g, lampC.b, 0.28f), 3.6f, 3.2f);
                }
                foreach (var pt in new[] { new Vector2(2f, 7.5f), new Vector2(-8f, 6.2f) })
                {
                    var g = Hang(A, "lamp_hang", pt.x, pt.y, 4.6f, 4.4f); var w = OnPath(pt.x, pt.y);
                    if (g != null) { PointLight("hang-light", new Vector3(w.x, 0.9f, w.z), lampC, 0.8f, 4f); Halo("hang-flame", new Vector3(w.x, 0.8f, w.z - 0.2f), 1.1f, new Color(lampC.r * 1.2f, lampC.g * 1.2f, lampC.b * 1.2f, 0.55f)); }
                }
                foreach (var pt in new[] { new Vector2(-0.8f, 5.3f), new Vector2(21.5f, 5.3f) })
                {
                    var g = Put(A, "brazier_cold", pt.x, pt.y, 1.8f, 0.9f, false, 0f, 1f, 0f) ?? Put(A, "brazier", pt.x, pt.y, 1.5f, 0.9f, false, 0f, 1f, 0f);
                    var w = OnPath(pt.x, pt.y);
                    PointLight("brazier-light", new Vector3(w.x, w.y + 1.3f, w.z), lampC, 0.9f, 4.5f);
                    Halo("brazier-glow", new Vector3(w.x, w.y + 1.3f, w.z - 0.3f), 1.6f, new Color(lampC.r * 1.2f, lampC.g * 1.2f, lampC.b * 1.2f, 0.5f));
                    Pool("brazier-pool", w, new Color(lampC.r, lampC.g, lampC.b, 0.22f), 2.8f, 2.4f);
                }
            }
            // ---- 結晶: 主役は右端の 1 本。左奥の対は霧の中で暗い。小結晶は水際と段の根元
            {
                var big = PropTexRaw(A, "crystal_spire_big", null) ?? PropTexRaw(A, "spire", Px.Crystal(veinC, rng));
                var w = OnPath(27f, 12.5f); var cr = Prop("crystal-spire", big, w, 8.5f, 0.5f); cr.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f); cr.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.3f);
                Halo("crystal-halo", w + new Vector3(0f, 4.2f, -0.6f), 8.5f * 2.4f, new Color(veinC.r * 1.3f, veinC.g * 1.3f, veinC.b * 1.3f, 0.4f));
                _pulseLight = PointLight("crystal-light", w + new Vector3(0f, 3.8f, -1.2f), veinC, 2.0f, 16f);
                Pool("crystal-pool", w, new Color(veinC.r, veinC.g, veinC.b, 0.35f), 7.6f, 6.4f);
                var w2 = OnPath(-10f, 20f); var cr2 = Prop("crystal-spire", big, w2, 6.5f, 0.5f); cr2.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f); cr2.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.6f);
                Halo("crystal-halo", w2 + new Vector3(0f, 3.2f, -0.4f), 14f, new Color(veinC.r, veinC.g, veinC.b, 0.25f));
                PointLight("crystal-light2", w2 + new Vector3(0f, 3f, -0.8f), veinC, 1.0f, 10f);
                var cl = PropTexRaw(A, "crystal_cluster", null) ?? PropTexRaw(2, "crystal", Px.Crystal(veinC, rng));
                float[] ct = { -7.5f, 15f, 19.5f, -8.8f, 20f, -2f, 6.5f, 12f }; float[] cs = { 8.55f, 8.55f, 8.55f, 10.75f, 10.75f, -7.2f, -7.8f, 15.5f }; float[] ch = { 1.4f, 1.8f, 1.2f, 2.0f, 1.6f, 1.3f, 1.1f, 2.2f };
                for (int i = 0; i < ct.Length; i++)
                {
                    var c = OnPath(ct[i], cs[i]); if (cs[i] < 8.6f && cs[i] > 5.8f) c.y = -0.95f;   // 水際: 足が水に入る
                    var g = Prop("crystal", cl, c, ch[i], 0.5f); g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f);
                    if (i % 2 == 1) g.transform.localScale = new Vector3(-g.transform.localScale.x, g.transform.localScale.y, 1f);   // 同形が並ばない (レビュー)
                    Halo("crystal-halo", c + new Vector3(0f, ch[i] * 0.45f, -0.2f), ch[i] * 1.6f, new Color(veinC.r, veinC.g, veinC.b, 0.2f));   // 座席の帯に並ぶのでキャラより明るくしない (レビュー)
                }
                var vw = PropTexRaw(A, "vein_wall", null);
                if (vw != null) foreach (float t in new[] { -3f, 16.5f, 22f }) { var wv = OnPath(t, 8.62f); var g = Plane("vein-wall", vw, new Vector3(wv.x, -0.9f, wv.z), 1.0f, 0.5f, false); g.transform.rotation = Quaternion.Euler(0f, PathYaw, 0f); g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f); g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; Halo("vein-halo", new Vector3(wv.x, -0.4f, wv.z - 0.2f), 1.6f, new Color(veinC.r, veinC.g, veinC.b, 0.3f)); }
                PointLight("canal-vein-light", OnPath(-3f, 8.3f) + new Vector3(0f, 0.2f, 0f), veinC, 0.8f, 5f);
            }
            // ---- 機械の庭 (T2 の右・霧の中)
            {
                var g = PutRaw(A, "drill_rig", 19f, 19.5f, 6.5f, 0.9f, false, 0f, 0.4f, 0.5f);
                var w = OnPath(19f, 19.5f); _corePulse = PointLight("core-light", new Vector3(w.x, w.y + 2.6f, w.z - 0.8f), veinC, 1.5f, 7f);
                Halo("core-glow", new Vector3(w.x, w.y + 2.6f, w.z - 0.9f), 2.2f, new Color(veinC.r, veinC.g, veinC.b, 0.4f));
                var gwl = PutRaw(A, "gear_wall", 26f, 21f, 5.5f, 0.9f, false, 0f, 0.45f, 0.5f);
                if (gwl != null) { var gw2 = OnPath(26f, 21f); var wheel = Prop("gear-wheel", Px.Wheel(new Color(0.18f, 0.18f, 0.2f), 32), new Vector3(gw2.x, gw2.y + 2.75f, gw2.z - 0.1f), 2.4f, 0.4f); wheel.GetComponent<MeshFilter>().sharedMesh = _quadCentered; wheel.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.45f); wheel.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; }
                PutRaw(A, "boiler_pipes", 31f, 20f, 6f, 0f, false, 0f, 0.45f, 0.5f);
                var cart = Put(A, "ore_cart", 23f, 18f, 1.6f, 0.9f); if (cart != null) { var cw = OnPath(23f, 18f); Halo("ore-glow", new Vector3(cw.x, cw.y + 1.0f, cw.z - 0.2f), 1.6f, new Color(veinC.r, veinC.g, veinC.b, 0.3f)); }
            }
            // ---- 床の敷物: 場の中央は 8 割を素のまま。縁に瓦礫・塵・地衣・ひび・濡れ・欠片
            {
                var rubble = PatchSet(A, "m_rubble", false, 56, 40, 80, 56); var dust = PatchSet(A, "m_dust", false, 64, 44, 96, 60); var lichen = PatchSet(A, "m_lichen", false, 48, 32, 72, 48);
                var crack = PatchSet(A, "m_crack", false, 56, 40, 80, 56); var wet = PatchSet(A, "m_wet", false, 64, 44, 96, 60); var shards = PatchSet(A, "m_shards", false, 40, 28);
                Action<List<Texture2D>, float, float, float, float> at = (set, t, s, sc, lift) => { if (set.Count == 0) return; var w = OnPath(t, s); GroundDecal("patch", Pick(set, rng), w.x, w.z, sc, (float)rng.NextDouble() * 360f, lift, rng.NextDouble() < 0.5); };
                for (int i = 0; i < 8; i++) at(crack, -12f + (float)rng.NextDouble() * 34f, 5.0f + (float)rng.NextDouble() * 0.7f, 1f, 0.014f);
                for (int i = 0; i < 6; i++) at(crack, -12f + (float)rng.NextDouble() * 30f, -3.4f + (float)rng.NextDouble() * 0.5f, 1f, 0.014f);
                for (int i = 0; i < 10; i++) at(lichen, -12f + (float)rng.NextDouble() * 36f, i < 6 ? 8.7f + (float)rng.NextDouble() * 0.6f : 10.9f + (float)rng.NextDouble() * 0.8f, 0.9f, 0.016f);
                for (int i = 0; i < 12; i++) at(dust, -12f + (float)rng.NextDouble() * 36f, (i % 2 == 0) ? 11f + (float)rng.NextDouble() * 4f : 16f + (float)rng.NextDouble() * 6f, 1f, 0.013f);
                for (int i = 0; i < 12; i++) at(rubble, -12f + (float)rng.NextDouble() * 36f, (i % 3 == 0) ? 10.9f + (float)rng.NextDouble() : (i % 3 == 1) ? 15.7f + (float)rng.NextDouble() : 18f + (float)rng.NextDouble() * 4f, 1f, 0.015f);
                foreach (var pt in new[] { new Vector2(0.4f, 5.4f), new Vector2(5.6f, 5.4f), new Vector2(0.4f, 9.2f), new Vector2(5.6f, 9.2f), new Vector2(3f, 14.6f) }) at(wet, pt.x, pt.y, 1.1f, 0.017f);
                foreach (var pt in new[] { new Vector2(27f, 11.6f), new Vector2(26f, 13.3f), new Vector2(-10f, 19.2f) }) at(shards, pt.x, pt.y, 1f, 0.018f);
                at(lichen, -6.5f, 1.6f, 0.8f, 0.016f);
                // 床の毛細な光: 場の縁の外に細い脈の筋 (中央は暗く保つ)
                var rune = Px.Radial(new Color(veinC.r, veinC.g, veinC.b, 0.5f));
                for (int i = 0; i < 12; i++)
                {
                    float t = -12f + i * 2.7f + (float)rng.NextDouble(); float sv = (i % 2 == 0) ? 4.9f + (float)rng.NextDouble() * 0.7f : -3.0f - (float)rng.NextDouble() * 0.5f;
                    var w = OnPath(t, sv); var g = Glow("floor-vein", rune, new Vector3(w.x, w.y + 0.045f, w.z), 1f, 1f);
                    g.GetComponent<MeshFilter>().sharedMesh = _quadCentered; g.transform.rotation = Quaternion.Euler(90f, PathYaw + ((float)rng.NextDouble() - 0.5f) * 40f, 0f); g.transform.localScale = new Vector3(1.2f + (float)rng.NextDouble() * 1.6f, 0.1f, 1f);
                }
            }
            // 光の粒の足元 (リーダーのランタン)
            var lampBase = new Vector3(_lampPos.x, 0f, _lampPos.z);
            Pool("mote-pool", lampBase, new Color(1f, 0.78f, 0.46f, 0.22f), 5f, 4.4f);
        }

        // ---------------------------------------------------------------- 時間変化 (StageDriver が呼ぶ)
        static Light _pulseLight, _corePulse;
        static Vector3 _emberPos = Vector3.zero;
        /// <summary>幕3 の主結晶の呼吸と掘削の腕の核の脈動</summary>
        static void UpdateActLights()
        {
            float t = Time.time;
            if (_pulseLight != null) _pulseLight.intensity = 2.0f * (1f + 0.08f * (Mathf.Sin(t / 3.1f * Mathf.PI * 2f) * 0.5f + Mathf.Sin(t / 4.7f * Mathf.PI * 2f) * 0.5f));
            if (_corePulse != null) _corePulse.intensity = Mathf.Lerp(1.2f, 1.8f, 0.5f + 0.5f * Mathf.Sin(t / 2.4f * Mathf.PI * 2f));
        }
    }
}
