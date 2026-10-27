// StageHd2dAnchors.cs — HD-2D の攻撃演出 (dot の Godot 版 checkpoint 192 の移植・2026-10-27) が、キャラの絵の「画素の位置」を世界の点に写すための口。
// Godot 側 (BattleActor.PrototypePosePoint / TryPrototypeOpaqueSurface) の写し。StageUnit は外へ出さず、必要な値だけ UnitAnchor に写す。
// 板の置き方 (StageUnits.UpdatePresentation): 板の足元中央 = pos、板の大きさ = 元絵の枡 (w×h の画面 px) × k × コマの拡大 (FrameScale)。
// 足元 FeetWorld = pos + 上×(FeetPad×h×k) + 右×((FootAnchorX−.5)×w×k)。ここから逆算して pos を出し、コマの uv (左上が原点) を世界へ。
using System;
using System.Collections.Generic;
using UnityEngine;
namespace DeckRogue.Game
{
    public static partial class Stage
    {
        /// <summary>HD-2D の攻撃演出が読む、キャラ1体の板の写し (StageUnit の必要な値だけ)</summary>
        public sealed class UnitAnchor
        {
            public string Key, ArtName, Anim;
            public int Frame;
            public Vector3 FeetWorld, Right, Up;          // 足元の世界の点・板の右と上 (レイアウト用のカメラの回転)
            public float CanvasHeightWorld, BaseAspect;    // 元絵の枡の世界の高さ (h×k) と 幅÷高さ
            public float FeetPad, FootAnchorX, HeightWorld;
            public Texture2D BaseTex, CurrentTex;
            public List<Texture2D> AttackFrames;
            /// <summary>板の足元中央 (pos)。コマの枡はここを底の中央に置く</summary>
            public Vector3 CanvasBottomCenter => FeetWorld - Up * (FeetPad * CanvasHeightWorld) - Right * ((FootAnchorX - .5f) * CanvasHeightWorld * BaseAspect);
            /// <summary>コマ tex の uv (左上が原点・0〜1) を世界の点に。枡はコマの大きさで広がる (StageUnit.Apply の FrameScale と同じ式)</summary>
            public Vector3 TexturePoint(Texture2D tex, Vector2 uv)
            {
                if (tex == null) tex = CurrentTex ?? BaseTex;
                float sx = BaseTex != null && tex != null ? tex.width / (float)BaseTex.width : 1f;
                float sy = BaseTex != null && tex != null ? tex.height / (float)BaseTex.height : 1f;
                float wf = CanvasHeightWorld * BaseAspect * sx, hf = CanvasHeightWorld * sy;
                return CanvasBottomCenter + Right * ((uv.x - .5f) * wf) + Up * ((1f - uv.y) * hf);
            }
        }

        public static bool TryGetUnitAnchor(string key, out UnitAnchor a)
        {
            a = null;
            StageUnit u;
            if (string.IsNullOrEmpty(key) || !_bound.TryGetValue(key, out u) || u == null || !u.HasBox || u.BaseTex == null) return false;
            List<Texture2D> attack; u.Anims.TryGetValue("attack", out attack);
            List<Texture2D> cur; Texture2D curTex = u.Anims.TryGetValue(u.Anim, out cur) && cur.Count > 0 ? cur[Mathf.Clamp(u.Frame, 0, cur.Count - 1)] : u.BaseTex;
            var rot = HD2DFlags.DioramaHere ? LayoutRot() : CameraRotation;
            float aspect = u.BaseUvScale.y > 0f && u.BaseTex.height > 0 ? (u.BaseUvScale.x * u.BaseTex.width) / (u.BaseUvScale.y * u.BaseTex.height) : 1f;
            a = new UnitAnchor
            {
                Key = key, ArtName = u.ArtName, Anim = u.Anim, Frame = u.Frame,
                FeetWorld = u.FeetWorld, Right = rot * Vector3.right, Up = rot * Vector3.up,
                CanvasHeightWorld = u.BaseCanvasHeightWorld, BaseAspect = aspect,
                FeetPad = u.FeetPad, FootAnchorX = u.FootAnchorX, HeightWorld = u.HeightWorld,
                BaseTex = u.BaseTex, CurrentTex = curTex, AttackFrames = attack,
            };
            return a.CanvasHeightWorld > 0f;
        }

        // ---- 絵の不透明な縁 (Godot の PrototypeAlphaRows の写し): 行ごとの左端・右端と、いちばん長い連続 (体の塊) ----
        sealed class AlphaRows { public int Width, Height; public int[] Left, Right, BodyLeft, BodyRight; }
        static readonly Dictionary<Texture2D, AlphaRows> _alphaRows = new Dictionary<Texture2D, AlphaRows>();   // 鍵は絵そのもの (Unity 6.6 では GetInstanceID が廃止)
        static AlphaRows RowsOf(Texture2D tex)
        {
            if (tex == null) return null;
            AlphaRows rows;
            if (_alphaRows.TryGetValue(tex, out rows)) return rows;
            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch (Exception) { _alphaRows[tex] = null; return null; }   // 読み戻せない絵 (isReadable=false) は諦める
            int w = tex.width, h = tex.height;
            rows = new AlphaRows { Width = w, Height = h, Left = new int[h], Right = new int[h], BodyLeft = new int[h], BodyRight = new int[h] };
            for (int y = 0; y < h; y++)
            {
                rows.Left[y] = rows.Right[y] = rows.BodyLeft[y] = rows.BodyRight[y] = -1;
                int run = 0, longest = 2;
                int srcY = h - 1 - y;   // GetPixels32 は下の行が先。Godot と同じく上が 0 の行で持つ
                for (int x = 0; x < w; x++)
                {
                    // Match rendered alpha cutoff; require a three-pixel horizontal run so an isolated hair/spark is not treated as the reachable body.
                    run = px[srcY * w + x].a >= 102 ? run + 1 : 0;
                    if (run >= 3) { if (rows.Left[y] < 0) rows.Left[y] = x - run + 1; rows.Right[y] = x; }
                    if (run > longest) { longest = run; rows.BodyLeft[y] = x - run + 1; rows.BodyRight[y] = x; }
                }
            }
            _alphaRows[tex] = rows;
            return rows;
        }

        /// <summary>
        /// キャラの今のコマの、世界の高さ worldY にある不透明な縁の点 (nearSide=true は絵の左端・false は右端)。bodyMass は細い突起でなく体の塊の縁。
        /// Godot の TryPrototypeOpaqueSurface と同じ: 行の帯 (高さの 5.5%) の四分位で、ひげ1本に引かれない
        /// </summary>
        public static bool TryUnitSurface(UnitAnchor a, float worldY, bool nearSide, out Vector3 point, bool bodyMass = false)
        {
            point = default;
            if (a == null) return false;
            var tex = a.CurrentTex ?? a.BaseTex;
            var rows = RowsOf(tex);
            if (rows == null) return false;
            // 世界の高さ → コマの行 (uv.y・上が 0)
            Vector3 bottom = a.CanvasBottomCenter;
            float sy = a.BaseTex != null ? tex.height / (float)a.BaseTex.height : 1f;
            float hf = a.CanvasHeightWorld * sy;
            if (hf <= 0f) return false;
            float up = Vector3.Dot(a.Up, Vector3.up); if (Mathf.Abs(up) < 1e-3f) up = 1f;
            float v = 1f - (worldY - bottom.y) / (hf * up);
            int row = Mathf.Clamp(Mathf.RoundToInt(v * rows.Height), 0, rows.Height - 1);
            int radius = Mathf.Max(2, Mathf.RoundToInt(rows.Height * .055f));
            var xs = new List<int>();
            int[] left = bodyMass ? rows.BodyLeft : rows.Left, right = bodyMass ? rows.BodyRight : rows.Right;
            for (int y = Mathf.Max(0, row - radius); y <= Mathf.Min(rows.Height - 1, row + radius); y++)
                if (left[y] >= 0) xs.Add(nearSide ? left[y] : right[y]);
            if (xs.Count < 3) return false;
            xs.Sort();
            int xPixel = xs[(int)((xs.Count - 1) * (nearSide ? .25f : .75f))];
            point = a.TexturePoint(tex, new Vector2(xPixel / (float)rows.Width, row / (float)rows.Height));
            return true;
        }
    }
}
