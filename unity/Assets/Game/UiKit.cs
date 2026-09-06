// UiKit.cs — コードだけで uGUI を組む最小の道具箱。
// プレハブ・シーン・TextMeshPro は使わない (このマシンに Unity Editor が無いため、
// すべてスクリプトから生成する)。API は uGUI の枯れたものだけ。
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static class UiKit
    {
        // ---- ダークテーマ ----
        public static readonly Color ColBg = Hex("#131917");
        public static readonly Color ColPanel = Hex("#1c2422");
        public static readonly Color ColPanel2 = Hex("#243230");
        public static readonly Color ColText = Hex("#d9e2da");
        public static readonly Color ColDim = Hex("#8a9a90");
        public static readonly Color ColAccent = Hex("#6abf69");
        public static readonly Color ColHp = Hex("#c94f4f");
        public static readonly Color ColBlock = Hex("#6f9fd8");
        public static readonly Color ColEnergy = Hex("#f0c33c");
        public static readonly Color ColBad = Hex("#e06c6c");
        public static readonly Color ColClear = new Color(0f, 0f, 0f, 0f);

        static Font _font;

        public static Color Hex(string s)
        {
            Color c;
            if (ColorUtility.TryParseHtmlString(s, out c)) return c;
            return Color.magenta;
        }

        /// <summary>日本語が出るフォント。OS フォントが取れなければ組み込みへフォールバック</summary>
        public static Font GetFont()
        {
            if (_font != null) return _font;
            try
            {
                _font = Font.CreateDynamicFontFromOSFont(
                    new string[] { "Yu Gothic UI", "Meiryo UI", "Meiryo", "MS Gothic", "Noto Sans CJK JP", "Hiragino Sans" }, 20);
            }
            catch { _font = null; }
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }

        // ---- 生成 ----

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        /// <summary>単色パネル (Image)</summary>
        public static Image Pan(Transform parent, Color color, string name = "panel")
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        /// <summary>テキスト (legacy Text)。既定でレイキャスト対象外＝下のボタンを塞がない</summary>
        public static Text Txt(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var rt = NewRect("text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = GetFont();
            t.fontSize = size;
            t.color = color;
            t.text = text == null ? "" : text;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = false;
            t.raycastTarget = false;
            return t;
        }

        public static Button Btn(Transform parent, string label, Action onClick, int size = 15, bool interactable = true, Color? bg = null)
        {
            var rt = NewRect("button", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg.HasValue ? bg.Value : ColPanel2;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.82f, 1f, 0.82f, 1f);
            cb.pressedColor = new Color(0.65f, 0.9f, 0.65f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.05f;
            btn.colors = cb;
            btn.interactable = interactable;
            if (onClick != null) btn.onClick.AddListener(delegate { onClick(); });
            var t = Txt(rt, label, size, ColText, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, 6f, 6f, 2f, 2f);
            Le(rt, -1f, size + 14f, -1f, size + 14f);
            return btn;
        }

        // ---- レイアウト ----

        /// <summary>親いっぱいに広げる (l/r/t/b は内側への余白)</summary>
        public static void Stretch(RectTransform rt, float l, float r, float t, float b)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
        }

        /// <summary>アンカーとオフセットを直接指定 (offMax は負で内側)</summary>
        public static void Anchor(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
        }

        public static VerticalLayoutGroup Vert(Transform t, int spacing = 4, int pad = 0)
        {
            var g = t.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(pad, pad, pad, pad);
            g.childAlignment = TextAnchor.UpperLeft;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;
            return g;
        }

        public static HorizontalLayoutGroup Horz(Transform t, int spacing = 6, int pad = 0)
        {
            var g = t.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(pad, pad, pad, pad);
            g.childAlignment = TextAnchor.UpperLeft;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = true;
            return g;
        }

        public static LayoutElement Le(Component c, float minW = -1f, float minH = -1f, float prefW = -1f, float prefH = -1f, float flexW = -1f, float flexH = -1f)
        {
            var le = c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = minW;
            le.minHeight = minH;
            le.preferredWidth = prefW;
            le.preferredHeight = prefH;
            le.flexibleWidth = flexW;
            le.flexibleHeight = flexH;
            return le;
        }

        /// <summary>スクロール可能な一覧。戻り値は中身を足していく content の RectTransform</summary>
        public static RectTransform Scroll(Transform parent, bool vertical, Color? bg = null, int spacing = 4, int pad = 6)
        {
            var root = NewRect("scroll", parent);
            var img = root.gameObject.AddComponent<Image>();
            img.color = bg.HasValue ? bg.Value : new Color(0f, 0f, 0f, 0.18f);
            var sr = root.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = !vertical;
            sr.vertical = vertical;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 28f;
            sr.inertia = false;

            var viewport = NewRect("viewport", root);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = NewRect("content", viewport);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            if (vertical)
            {
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = Vector2.zero;
                Vert(content, spacing, pad);
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
            else
            {
                content.anchorMin = new Vector2(0f, 0f);
                content.anchorMax = new Vector2(0f, 1f);
                content.pivot = new Vector2(0f, 0.5f);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = Vector2.zero;
                Horz(content, spacing, pad);
                fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fit.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
            sr.viewport = viewport;
            sr.content = content;
            return content;
        }

        /// <summary>HP などのバー (縦レイアウトの子として置く)</summary>
        public static void Bar(Transform parent, int value, int max, Color color, string caption, int height = 20)
        {
            var row = NewRect("bar", parent);
            var bg = row.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            bg.raycastTarget = false;
            Le(row, -1f, height, -1f, height);

            float r = max > 0 ? Mathf.Clamp01((float)value / (float)max) : 0f;
            var fill = NewRect("fill", row);
            var fimg = fill.gameObject.AddComponent<Image>();
            fimg.color = color;
            fimg.raycastTarget = false;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(r, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;

            var t = Txt(row, caption, 13, ColText, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, 4f, 4f, 0f, 0f);
        }

        /// <summary>Scroll() が返した content から、スクロールの外枠 (レイアウト対象) を取り出す</summary>
        public static RectTransform ScrollRoot(RectTransform content)
        {
            return content.parent.parent as RectTransform;
        }

        /// <summary>縦レイアウトの中に固定高さの隙間を作る</summary>
        public static void Spacer(Transform parent, float h)
        {
            var rt = NewRect("spacer", parent);
            Le(rt, -1f, h, -1f, h);
        }

        /// <summary>見出し1行</summary>
        public static Text Head(Transform parent, string text, int size = 18)
        {
            var t = Txt(parent, text, size, ColAccent);
            Le(t, -1f, size + 8f, -1f, size + 8f);
            return t;
        }
    }
}

// C# 9 の init アクセサ (エンジンの record) を Assembly-CSharp 側からも確実に呼べるようにするポリフィル。
// エンジン側の Generated/IsExternalInit.cs は internal なので、こちらにも同じものを置いておく (internal なので衝突しない)。
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
