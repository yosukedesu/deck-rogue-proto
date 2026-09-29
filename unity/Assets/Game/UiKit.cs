// UiKit.cs — コードだけで uGUI を組む道具箱 (2026-09-07 M1: TextMeshPro + Noto Sans JP + テーマの9スライスへ)。
// プレハブ・シーンは使わず全てスクリプトから生成する (UI技術の裁定: uGUI・コード生成＋テーマ)。
// 文字は TextMeshPro (Resources/Fonts の Noto Sans JP から動的 SDF フォントを作る)。枠は Theme の生成スプライト。
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static class UiKit
    {
        // ---- 色は PaperFx が唯一の出典 (カラーテーマ「黒鉄と真鍮」2026-09-16)。ここは旧名の別名 ----
        public static readonly Color ColBg = PaperFx.Night;
        public static readonly Color ColPanel = PaperFx.Paper;
        public static readonly Color ColPanel2 = PaperFx.Paper2;
        public static readonly Color ColText = PaperFx.Paper;
        /// <summary>夜の上の淡い紙色の文字 (9:1)。二次の文字色は実色で持つ (2026-09-09: 透明度で薄めると 3.7:1 まで落ちた)</summary>
        public static readonly Color ColDim = PaperFx.PaperDim;
        public static readonly Color ColInk = PaperFx.Ink;
        public static readonly Color ColInkSoft = PaperFx.InkSoft;
        /// <summary>紙の上に置く真鍮の文字 (真鍮そのものは紙の上で読めない。塗りは Brass)</summary>
        public static readonly Color ColGoldInk = PaperFx.BrassInk;
        /// <summary>紙の上に置く危険の文字 (ColBad は塗り用)</summary>
        public static readonly Color ColBadInk = PaperFx.BadInk;
        /// <summary>スマホ向けの画面 (2026-09-14 ユーザー「スマホ最適化するべき」): UI 1.6倍・戦闘の絵は半分・説明文はタップで上部の固定パネル。PC では -uiscale で再現する</summary>
        public static bool Phone;
        /// <summary>文字の最小サイズ (1920×1080 基準の単位)。スマホは 15 (1.6倍で 24px ≒ 読める下限)。これ未満は Txt が切り上げる</summary>
        public static int MinFontSize { get { return Phone ? 15 : 13; } }
        /// <summary>実際に描かれる文字の大きさ = 指定と最小 (MinFontSize) の大きい方 (2026-09-29 p16)。Txt・Deco はこの大きさで描くので、
        /// 幅や高さを見積もる所もこれを通す (指定 12 で幅を計算すると、スマホでは 15 で描かれて札の外へ溢れた)</summary>
        public static int Fs(int size) { return Math.Max(size, MinFontSize); }
        /// <summary>画面の四辺の余白 (キャンバスの単位。2026-09-29 p12): PC 32・スマホ 24。上部バーの左右・山札と捨て札・自分の札の左端・エナジーの輪・ターン終了の右端をこの1つの値でそろえる。
        /// スマホを 24 にするのは、自分の欄 (PhoneSelfColumn) の 24 が先に決まっていたため (狭い帯を 8 削らない)</summary>
        public static float Edge { get { return Phone ? 24f : 32f; } }

        // ---- 画面の切り欠き (パンチホール。2026-09-29 p10) ----
        // APK は LandscapeLeft 固定なので、S25 のパンチホールはいつも左端の縦の中央に来る (androidRenderOutsideSafeArea=1・targetSdk 35 以上は端まで描くのが強制)。
        // キャンバスは全画面のまま (Stage.ProjectFeet の座席→UI 座標・暗幕の全面塗りを壊さない) にして、穴と同じ高さにある左端の部品だけをこの値で右へ逃がす。
        // 角の丸みは Screen.safeArea に含まれず文字にも掛からないので扱わない

        static Rect[] _fakeCutouts;
        static bool _fakeTried;

        /// <summary>起動引数 -fakecutout x,y,w,h (実px・左下原点。「;」で複数) = shots で切り欠きを再現する。PC の Screen.cutouts は常に空</summary>
        static Rect[] FakeCutouts()
        {
            if (_fakeTried) return _fakeCutouts;
            _fakeTried = true;
            try
            {
                var args = Environment.GetCommandLineArgs();
                for (int i = 0; i + 1 < args.Length; i++)
                {
                    if (args[i] != "-fakecutout") continue;
                    var list = new System.Collections.Generic.List<Rect>();
                    foreach (var part in args[i + 1].Split(';'))
                    {
                        var v = part.Split(',');
                        if (v.Length != 4) continue;
                        var ci = System.Globalization.CultureInfo.InvariantCulture;
                        var ns = System.Globalization.NumberStyles.Float;
                        float x, y, w, h;
                        if (float.TryParse(v[0], ns, ci, out x) && float.TryParse(v[1], ns, ci, out y) && float.TryParse(v[2], ns, ci, out w) && float.TryParse(v[3], ns, ci, out h))
                            list.Add(new Rect(x, y, w, h));
                    }
                    if (list.Count > 0) _fakeCutouts = list.ToArray();
                }
            }
            catch (Exception) { }
            return _fakeCutouts;
        }

        /// <summary>画面の切り欠きの矩形 (実px・左下原点)。Screen.cutouts ＋ -fakecutout</summary>
        public static Rect[] Cutouts()
        {
            Rect[] real = null;
            try { real = Screen.cutouts; } catch (Exception) { }
            var fake = FakeCutouts();
            if (fake == null || fake.Length == 0) return real ?? new Rect[0];
            if (real == null || real.Length == 0) return fake;
            var all = new Rect[real.Length + fake.Length];
            real.CopyTo(all, 0); fake.CopyTo(all, real.Length);
            return all;
        }

        /// <summary>切り欠きと safeArea の指紋 (変わったら組み直す。Stage の画面の監視が読む)</summary>
        public static int CutoutSignature()
        {
            int h = Screen.safeArea.GetHashCode();
            var cuts = Cutouts();
            for (int i = 0; i < cuts.Length; i++) h = h * 31 + cuts[i].GetHashCode();
            return h;
        }

        /// <summary>起動時のログ用 (実機の logcat で穴の実寸を確かめる)</summary>
        public static string DescribeCutouts()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("screen=").Append(Screen.width).Append('x').Append(Screen.height).Append(" safeArea=").Append(Screen.safeArea).Append(" cutouts=[");
            var cuts = Cutouts();
            for (int i = 0; i < cuts.Length; i++) sb.Append(i > 0 ? ", " : "").Append(cuts[i]);
            return sb.Append(']').ToString();
        }

        /// <summary>
        /// キャンバスの縦の範囲 top〜bottom (キャンバスの上から測った値) に掛かる左端の切り欠きの、右端＋8 (キャンバス単位)。無ければ 0。
        /// Screen.cutouts が空で safeArea の左が差し込まれている端末は、左端の全高を切り欠きとして扱う。PC は 0 (safeArea は全画面)
        /// </summary>
        public static float CutoutLeft(float topCanvas, float bottomCanvas)
        {
            if (Screen.width <= 0 || Screen.height <= 0) return 0f;
            var g = GameRoot.I;
            var cs = BattleScreen.CanvasSize(g != null ? g.ScreenRoot : null);
            if (cs.x <= 0f) return 0f;
            float sf = Screen.width / cs.x;   // 実px ÷ キャンバス単位
            float best = 0f;
            var cuts = Cutouts();
            if (cuts.Length == 0)
            {
                float inset = Screen.safeArea.xMin;
                return inset > 0.5f ? inset / sf + 8f : 0f;
            }
            for (int i = 0; i < cuts.Length; i++)
            {
                var r = cuts[i];
                if (r.xMax > Screen.width * 0.5f) continue;   // 左端のものだけ (向きは LandscapeLeft 固定)
                float cTop = (Screen.height - r.yMax) / sf, cBottom = (Screen.height - r.yMin) / sf;   // 上から測ったキャンバス座標
                if (cTop >= bottomCanvas + 8f || cBottom <= topCanvas - 8f) continue;
                best = Math.Max(best, r.xMax / sf + 8f);
            }
            return best;
        }

        /// <summary>左の余白 margin を、縦の範囲 top〜bottom (上から) に掛かる切り欠きの右まで広げた値</summary>
        public static float SafeLeft(float margin, float topCanvas, float bottomCanvas)
        {
            return Math.Max(margin, CutoutLeft(topCanvas, bottomCanvas));
        }

        public static readonly Color ColAccent = PaperFx.Moss;
        public static readonly Color ColHp = PaperFx.Rose;
        public static readonly Color ColBlock = PaperFx.Sky;
        public static readonly Color ColEnergy = PaperFx.Brass;
        public static readonly Color ColBad = PaperFx.Rose;
        public static readonly Color ColClear = new Color(0f, 0f, 0f, 0f);

        static TMP_FontAsset _fontRegular;
        static TMP_FontAsset _fontBold;
        static bool _fontTried;

        public static Color Hex(string s)
        {
            Color c;
            if (ColorUtility.TryParseHtmlString(s, out c)) return c;
            return Color.magenta;
        }

        /// <summary>
        /// 日本語フォント (Noto Sans JP・OFL)。Resources/Fonts の OTF から動的 SDF フォントアセットを作る
        /// (静的アトラスは製品版の最適化で。動的なら未使用の漢字を焼かずに済む)。失敗したら TMP の既定フォント (英数のみ)。
        /// </summary>
        public static TMP_FontAsset FontRegular { get { EnsureFonts(); return _fontRegular; } }
        public static TMP_FontAsset FontBold { get { EnsureFonts(); return _fontBold ?? _fontRegular; } }
        /// <summary>名前・見出しの装飾明朝 (Kaisei Decol・OFL)。無ければ太字</summary>
        public static TMP_FontAsset FontDeco { get { EnsureFonts(); return _fontDeco ?? FontBold; } }
        static TMP_FontAsset _fontDeco;

        /// <summary>
        /// HP バーの数字の素材 (2026-09-29 p04): 太字の SDF に紙 (明) の下敷き (underlay) を敷き、薔薇色の塗りの上でも字のすぐ外が紙になる (墨と 12:1)。
        /// スイッチは OUTLINE_ON と UNDERLAY_ON の両方を入れる: 実行時に作るフォントの素材は資産に無いので、ビルドに残るのは
        /// Resources の「Drop Shadow.mat」と同じ組み合わせだけ (片方だけだと exe・APK で黙って削られ、エディタでしか見えない)。縁取りの幅は 0
        /// </summary>
        public static Material NumHalo
        {
            get
            {
                if (_numHalo != null) return _numHalo;
                var bold = FontBold;
                if (bold == null || bold.material == null) return null;
                var m = new Material(bold.material) { name = "NumHalo" };
                m.EnableKeyword("OUTLINE_ON");
                m.EnableKeyword("UNDERLAY_ON");
                m.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
                m.SetColor(ShaderUtilities.ID_OutlineColor, PaperFx.Ink);   // 幅 0 の縁取りの境目がにじんでも墨のまま
                m.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f);   // 13〜17px の Klee One は線が細く芯が灰色に抜けるので字を少し太らせる
                m.SetColor(ShaderUtilities.ID_UnderlayColor, PaperFx.Paper3);
                m.SetFloat(ShaderUtilities.ID_UnderlayDilate, 1.2f);   // 紙の縁 約2px (設計の見本 text-shadow 0 0 2px の紙)
                m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.15f);
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
                _numHalo = m;
                return m;
            }
        }
        static Material _numHalo;

        /// <summary>小さい文字の上限 (実際に描く大きさ Fs がこれ以下なら SmallMat を当てる。2026-09-29 p19)</summary>
        public const int SmallTextMax = 15;
        static Material _smallRegular, _smallBold, _smallDeco;

        /// <summary>
        /// 小さい文字用の共有素材 (2026-09-29 p19): 元の素材に FaceDilate (輪郭を膨らませる) と Sharpness (縁のぼけを詰める) を足しただけ
        /// (キーワードは変えない＝ビルドで削られない)。13〜15 の Klee One は線が約1px で、いちばん濃い画素でも中墨の 5〜6割しか塗られない
        /// (実測 3.4〜4.3:1)。書体を SemiBold に替えても 0.2px しか太らない (「/ 4」SemiBold 15 で 4.75:1) ので SDF の側で直す。
        /// 値は PC で測って決めた: Regular 0.12・SemiBold 0.08・装飾体 0.06、Sharpness 0.4 (中墨のラベルのいちばん濃い画素が紙(濃)の上で 5.6〜6.4:1)。
        /// Regular 0.10 だけ (手順の値) では山札 4.0・エナジー 4.8 で届かず、0.15 まで上げると太字に見えた。
        /// スマホの 15 単位は約 20px なので、同じ膨らみを実寸の px でそろえる (×0.66。PC の値のままだと全部が太字に見えた)。
        /// 送り幅・レイアウトは変わらない (膨らみは余白 padding にしか効かない)。フォールバックの2枚目のアトラスにも写る
        /// </summary>
        public static Material SmallMat(TMP_FontAsset f)
        {
            if (f == null) return null;
            EnsureFonts();
            float k = Phone ? 0.66f : 1f;
            if (f == _fontRegular) return MakeSmallMat(f, ref _smallRegular, 0.12f * k);
            if (f == _fontBold) return MakeSmallMat(f, ref _smallBold, 0.08f * k);
            if (f == _fontDeco) return MakeSmallMat(f, ref _smallDeco, 0.06f * k);
            return null;
        }

        static Material MakeSmallMat(TMP_FontAsset f, ref Material cache, float dilate)
        {
            if (cache != null) return cache;
            var src = f.material;
            if (src == null || !src.HasProperty(ShaderUtilities.ID_FaceDilate)) return null;
            var m = new Material(src) { name = f.name + " small" };
            m.SetFloat(ShaderUtilities.ID_FaceDilate, dilate);
            if (m.HasProperty("_Sharpness")) m.SetFloat("_Sharpness", 0.4f);
            ShaderUtilities.UpdateShaderRatios(m);
            cache = m;
            return m;
        }

        /// <summary>長い本文 (説明パネル・用語の説明) を小さい文字の素材から元の太さへ戻す (2026-09-29 p19。長文まで太くしない)</summary>
        public static void PlainWeight(TMP_Text t)
        {
            if (t == null || t.font == null || t.font.material == null) return;
            t.fontSharedMaterial = t.font.material;
        }

        static void EnsureFonts()
        {
            if (_fontTried) return;
            _fontTried = true;
            // 本文と数字は手書き風の Klee One (OFL)。無ければ Noto Sans JP
            _fontRegular = MakeFont("KleeOne-Regular") ?? MakeFont("NotoSansJP-Regular");
            _fontBold = MakeFont("KleeOne-SemiBold") ?? MakeFont("NotoSansJP-Bold");
            _fontDeco = MakeFont("KaiseiDecol-Bold");
            if (_fontRegular == null) Debug.LogWarning("[UiKit] 日本語フォントを作れなかった。TMP の既定フォントで描く (日本語は豆腐)");
        }

        static TMP_FontAsset MakeFont(string resourceName)
        {
            try
            {
                var font = Resources.Load<Font>("Fonts/" + resourceName);
                if (font == null) return null;
                var fa = TMP_FontAsset.CreateFontAsset(font, 40, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                if (fa == null) return null;
                fa.name = resourceName;
                return fa;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[UiKit] フォント生成に失敗: " + resourceName + " / " + e.Message);
                return null;
            }
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

        /// <summary>9スライスの枠付きパネル (Theme.Panel)。color は枠の色味 (乗算)</summary>
        public static Image Frame(Transform parent, Sprite sprite, Color tint, string name = "frame", float scale = 3f)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = (sprite != null && sprite.name != null && sprite.name.StartsWith("paper")) ? 1f : 1f / scale;
            img.color = tint;
            return img;
        }

        /// <summary>見出し・名前 (装飾明朝)。既定は墨。
        /// Ellipsis を使う時は矩形の高さを字の大きさ×1.6以上に (15px≒23・17px≒26・19px≒29)。足りないと TMP は行ごと消す
        /// (2026-09-29 確認の窓の札の名前が高さ20の枠で消えていた。LedgerStrip の注記と同じ罠)</summary>
        public static TMP_Text Deco(Transform parent, string text, int size, Color? color = null, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var t = Txt(parent, text, size, color ?? ColInk, anchor, true);
            if (FontDeco != null) t.font = FontDeco;   // 書体の差し替えで素材は装飾体の既定に戻る
            if (Fs(size) <= SmallTextMax) { var sm = SmallMat(t.font); if (sm != null) t.fontSharedMaterial = sm; }
            t.characterSpacing = 2f;
            return t;
        }

        static TextAlignmentOptions MapAnchor(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            }
            return TextAlignmentOptions.TopLeft;
        }

        /// <summary>TMP の色タグ。色は PaperFx の名前から作る (生の値を足さない。2026-09-29)</summary>
        public static string ColorTag(Color c, string s)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + s + "</color>";
        }

        /// <summary>テキスト (TextMeshPro)。既定でレイキャスト対象外＝下のボタンを塞がない。リッチテキスト可 (色・スプライトタグ)。
        /// Ellipsis を使う時は矩形の高さを字の大きさ×1.6以上に (Deco の注記と同じ)</summary>
        public static TMP_Text Txt(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false)
        {
            var rt = NewRect("text", parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var f = bold ? FontBold : FontRegular;
            if (f != null) t.font = f;
            t.fontSize = Fs(size);
            if (Fs(size) <= SmallTextMax && f != null) { var sm = SmallMat(f); if (sm != null) t.fontSharedMaterial = sm; }   // 細い小さい字を指定の墨の濃さへ (p19)
            t.color = color;
            t.text = text == null ? "" : text;
            t.alignment = MapAnchor(anchor);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = true;
            t.raycastTarget = false;
            if (Theme.Icons != null) t.spriteAsset = Theme.Icons;
            return t;
        }

        public static Button Btn(Transform parent, string label, Action onClick, int size = 15, bool interactable = true, Color? bg = null)
        {
            var rt = NewRect("button", parent);
            var img = rt.gameObject.AddComponent<Image>();
            var sp = Theme.Button;
            if (sp != null)
            {
                img.sprite = sp;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
                img.color = bg.HasValue ? bg.Value : Color.white;
            }
            else
            {
                img.color = bg.HasValue ? bg.Value : ColPanel2;
            }
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 1f, 0.9f, 1f);
            cb.pressedColor = new Color(0.75f, 0.85f, 0.75f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.05f;
            btn.colors = cb;
            btn.interactable = interactable;
            btn.onClick.AddListener(delegate { Audio.Ui("click"); });
            if (onClick != null) btn.onClick.AddListener(delegate { onClick(); });
            var t = Txt(rt, label, size, ColInk, TextAnchor.MiddleCenter, true);
            Stretch(t.rectTransform, 8f, 8f, 2f, 6f);
            float h = Phone ? Math.Max(size + 16f, 48f) : size + 16f;   // スマホは指で押せる高さ (48 単位 ≒ 4mm。2026-09-14)
            Le(rt, -1f, h, -1f, h);
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

        /// <summary>縦スクロールの「しおり」(2026-09-16 ユーザー「スマホ版はスクロールなど操作しにくい。スクロールバーの導入」): 右端に紙の帯 (幅 22) と墨の栞。
        /// 掴んで引ける・帯を押すとその位置へ。指の的は幅 44 (帯の両脇は透明)。戻り値は帯ぶん viewport を狭める幅</summary>
        public const float BookmarkW = 22f, BookmarkHit = 44f;
        static float AddBookmark(RectTransform root, ScrollRect sr)
        {
            var sb = NewRect("bookmark", root);
            sb.anchorMin = new Vector2(1f, 0f); sb.anchorMax = new Vector2(1f, 1f);
            sb.offsetMin = new Vector2(-BookmarkHit - 2f, 4f); sb.offsetMax = new Vector2(-2f, -4f);
            var hit = sb.gameObject.AddComponent<Image>();   // 透明の的 (帯の両脇も掴める)
            hit.color = new Color(0f, 0f, 0f, 0f);
            var bar = sb.gameObject.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            var track = NewRect("track", sb);
            track.anchorMin = new Vector2(0.5f, 0f); track.anchorMax = new Vector2(0.5f, 1f);
            track.offsetMin = new Vector2(-BookmarkW / 2f, 0f); track.offsetMax = new Vector2(BookmarkW / 2f, 0f);
            var tImg = track.gameObject.AddComponent<Image>();
            tImg.sprite = PaperFx.Tag; tImg.type = Image.Type.Sliced; tImg.pixelsPerUnitMultiplier = 1f; tImg.color = PaperFx.Paper2; tImg.raycastTarget = false;
            var sliding = NewRect("sliding", sb);
            sliding.anchorMin = new Vector2(0.5f, 0f); sliding.anchorMax = new Vector2(0.5f, 1f);
            sliding.offsetMin = new Vector2(-BookmarkW / 2f, 3f); sliding.offsetMax = new Vector2(BookmarkW / 2f, -3f);
            var handle = NewRect("handle", sliding);
            handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one;
            handle.offsetMin = new Vector2(3f, 0f); handle.offsetMax = new Vector2(-3f, 0f);
            var hImg = handle.gameObject.AddComponent<Image>();
            hImg.sprite = PaperFx.Tag; hImg.type = Image.Type.Sliced; hImg.pixelsPerUnitMultiplier = 1f; hImg.color = PaperFx.Ink; hImg.raycastTarget = false;
            bar.handleRect = handle;
            bar.targetGraphic = hit;
            bar.transition = Selectable.Transition.None;
            sr.verticalScrollbar = bar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return BookmarkW + 10f;
        }

        /// <summary>スクロール可能な一覧。戻り値は中身を足していく content の RectTransform。縦はしおり (スクロールバー) 付き・フリックの慣性あり</summary>
        public static RectTransform Scroll(Transform parent, bool vertical, Color? bg = null, int spacing = 4, int pad = 6, bool bookmark = true)
        {
            var root = NewRect("scroll", parent);
            var img = root.gameObject.AddComponent<Image>();
            img.color = bg.HasValue ? bg.Value : new Color(0f, 0f, 0f, 0.18f);
            var sr = root.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = !vertical;
            sr.vertical = vertical;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 28f;
            sr.inertia = true;                 // フリックで滑る (2026-09-16。旧 false は「指を離した所で止まる」= 26枚で4〜5回引く)
            sr.decelerationRate = 0.135f;
            float barW = vertical && bookmark ? AddBookmark(root, sr) : 0f;

            var viewport = NewRect("viewport", root);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = new Vector2(-barW, 0f);
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

            var t = Txt(row, caption, 14, ColText, TextAnchor.MiddleCenter, true);
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
        public static TMP_Text Head(Transform parent, string text, int size = 18)
        {
            var t = Deco(parent, text, size, ColInk, TextAnchor.MiddleLeft);
            Le(t, -1f, size + 12f, -1f, size + 12f);
            return t;
        }

        /// <summary>アイコン (Theme のプレースホルダー or Resources/Art の差し替え)。size はキャンバス単位</summary>
        /// <param name="mono">墨1色で塗る版 (Theme.IconMono。32px 未満の置き場だけ)。墨を掛ける小さな札 (帳面の状態・rider・盾・ターン) は true (2026-09-29 p18)</param>
        public static Image Icon(Transform parent, string name, float size, Color? tint = null, bool mono = false)
        {
            var rt = NewRect("icon-" + name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = mono && size < 32f ? Theme.IconMono(name) : Theme.Icon(name, size);
            img.preserveAspect = true;
            img.raycastTarget = false;
            bool art = size >= 32f && Theme.HasIconArt(name);   // PixelLab の絵は色を持つので tint は alpha だけ (2026-09-11)
            img.color = tint.HasValue ? (art ? new Color(1f, 1f, 1f, tint.Value.a) : tint.Value) : Color.white;
            if (art) PixelArt(img);   // 32 ドットの絵を 44 (1.375倍) 等で置く時もドットの太さをそろえる (2026-09-29 p25)。16px の小さな絵は呼び手が選ぶ
            rt.sizeDelta = new Vector2(size, size);
            Le(rt, size, size, size, size);
            return img;
        }

        // ---- UI のドット絵 (2026-09-29 p25) ----
        static Material _pixMat;
        static bool _pixMissing;

        /// <summary>シャープ・バイリニアのマテリアル (DeckRogue/UIPixelSharp。全部の Image で1つを共有)。シェーダが無い・使えない時は null＝今までどおり最近傍</summary>
        public static Material PixelMat
        {
            get
            {
                if (_pixMat != null) return _pixMat;
                if (_pixMissing) return null;
                var sh = Shader.Find("DeckRogue/UIPixelSharp");
                if (sh == null || !sh.isSupported)
                {
                    _pixMissing = true;
                    Debug.LogWarning("[UiKit] UIPixelSharp シェーダが無い → UI のドット絵は最近傍のまま");
                    return null;
                }
                _pixMat = new Material(sh) { name = "UIPixelSharp" };
                return _pixMat;
            }
        }

        /// <summary>
        /// ドット絵の Image をシャープ・バイリニアで描く (2026-09-29 p25): 倍率が非整数 (手札 PC 1.84倍・スマホ 2.63倍・意図 1.375倍・ギア 1.5倍) でも、
        /// 札を傾けても、ドットの太さが見た目でそろう (境目の画面 1px だけ隣となじむ)。軸に沿った整数倍は今までどおり最近傍 (シェーダの側で判定)。
        /// 当てるのは Point フィルタの絵で、スプライトがテクスチャ丸ごとの時だけ (アトラスの一部だと隣の絵がにじむ)。それ以外 (水彩の玉・紙・
        /// なめらかな生成の絵) は既定のマテリアルに戻す。寸法は変えない。sprite を差し替えた後はもう一度呼ぶこと
        /// </summary>
        public static void PixelArt(Image img)
        {
            if (img == null) return;
            var sp = img.sprite;
            Texture2D tex = sp != null ? sp.texture : null;
            bool pixel = tex != null && tex.filterMode == FilterMode.Point
                && Mathf.RoundToInt(sp.rect.width) == tex.width && Mathf.RoundToInt(sp.rect.height) == tex.height;
            var m = pixel ? PixelMat : null;
            if (m != null) img.material = m;
            else if (_pixMat != null && img.material == _pixMat) img.material = null;
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
