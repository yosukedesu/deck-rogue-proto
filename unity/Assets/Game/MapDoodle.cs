// MapDoodle.cs — マップへの落書き (2026-09-12 ユーザー「スレスパ2のようにマップに書き込みできるようにして欲しい」)。
// 本家 StS2 の「地図にペンで描く」を移植: PC は右ドラッグで描き、ペンボタンを押すと左ドラッグ (指) でも描ける。
// 線は UI 層の状態 (GameRoot.Doodles。幕ごと・ランの間保持) で、エンジンには触れない。描くのはマップ画面だけ、重ねた地図 (ViewMap) では表示のみ。
// 描画は uGUI の Graphic (メッシュの帯) = 鉛筆らしい 4px の線。色は 紙色 (メモ) と 朱 (強調)。消しゴムは触れた線ごと。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    /// <summary>1本の線 (地図の content 座標。左下原点・px)</summary>
    public sealed class DoodleStroke
    {
        public int Color;                       // 0=紙色 1=朱
        public List<Vector2> Points = new List<Vector2>();
    }

    public class DoodleLayer : MaskableGraphic, IPointerDownHandler, IDragHandler, IPointerUpHandler, IBeginDragHandler, ICanvasRaycastFilter
    {
        public GameRoot Root;
        public List<DoodleStroke> Strokes;
        public bool Editable;
        const float Width = 4f;
        const float EraseRadius = 18f;
        static readonly Color[] Palette = { new Color(0.96f, 0.93f, 0.84f, 0.95f), new Color(0.88f, 0.42f, 0.36f, 0.95f) };   // 紙色・朱 (夜の地図の上で読める2色)
        DoodleStroke _current;
        bool _erasing;

        public static Color PenColor(int idx) { return Palette[Mathf.Clamp(idx, 0, Palette.Length - 1)]; }

        /// <summary>左クリック (ノード選択) は素通し。ペンモード中か右ボタン押下中だけこの層が受ける</summary>
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (!Editable || Root == null) return false;
            return Root.DoodleMode || Input.GetMouseButton(1);
        }

        bool ToLocal(PointerEventData e, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out local);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!Editable || Root == null || Strokes == null) return;
            bool right = e.button == PointerEventData.InputButton.Right;
            bool left = e.button == PointerEventData.InputButton.Left && Root.DoodleMode;
            if (!right && !left) return;
            Vector2 p; if (!ToLocal(e, out p)) return;
            _erasing = Root.DoodlePen == 2;
            if (_erasing) { EraseAt(p); return; }
            _current = new DoodleStroke { Color = right && !Root.DoodleMode ? 0 : Mathf.Clamp(Root.DoodlePen, 0, 1) };
            if (right && Root.DoodleMode) _current.Color = Mathf.Clamp(Root.DoodlePen, 0, 1);
            _current.Points.Add(p);
            Strokes.Add(_current);
            SetVerticesDirty();
        }

        public void OnBeginDrag(PointerEventData e) { }

        public void OnDrag(PointerEventData e)
        {
            if (!Editable || Root == null) return;
            Vector2 p; if (!ToLocal(e, out p)) return;
            if (_erasing) { EraseAt(p); return; }
            if (_current == null) return;
            if ((p - _current.Points[_current.Points.Count - 1]).sqrMagnitude < 4f) return;
            _current.Points.Add(p);
            SetVerticesDirty();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (_current != null && _current.Points.Count == 1) _current.Points.Add(_current.Points[0] + new Vector2(0.5f, 0f));   // 点も描ける
            _current = null; _erasing = false;
            SetVerticesDirty();
        }

        void EraseAt(Vector2 p)
        {
            if (Strokes == null) return;
            int before = Strokes.Count;
            Strokes.RemoveAll(s => Near(s, p));
            if (Strokes.Count != before) SetVerticesDirty();
        }

        static bool Near(DoodleStroke s, Vector2 p)
        {
            for (int i = 0; i < s.Points.Count; i++)
            {
                if ((s.Points[i] - p).sqrMagnitude <= EraseRadius * EraseRadius) return true;
                if (i > 0 && DistToSegment(p, s.Points[i - 1], s.Points[i]) <= EraseRadius) return true;
            }
            return false;
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; float len2 = ab.sqrMagnitude;
            float t = len2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
            return (a + ab * t - p).magnitude;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Strokes == null) return;
            // 線を帯 (四角形の連なり) で描く。継ぎ目は次の帯と重ねてつなぐ = 鉛筆の線らしいわずかな太り
            foreach (var s in Strokes)
            {
                var col = PenColor(s.Color);
                for (int i = 1; i < s.Points.Count; i++)
                {
                    var a = s.Points[i - 1]; var b = s.Points[i];
                    var d = b - a; if (d.sqrMagnitude < 0.01f) continue;
                    var n = new Vector2(-d.y, d.x).normalized * (Width * 0.5f);
                    var ext = d.normalized * (Width * 0.5f);   // 端を少し伸ばして継ぎ目の隙間を消す
                    int v = vh.currentVertCount;
                    vh.AddVert(a - ext + n, col, Vector2.zero);
                    vh.AddVert(b + ext + n, col, Vector2.zero);
                    vh.AddVert(b + ext - n, col, Vector2.zero);
                    vh.AddVert(a - ext - n, col, Vector2.zero);
                    vh.AddTriangle(v, v + 1, v + 2);
                    vh.AddTriangle(v, v + 2, v + 3);
                }
            }
        }
    }
}
