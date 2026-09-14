// ErrorOverlay.cs — 例外・エラーを画面の最前面に出す (2026-09-14 ユーザー「APK が Unity のロゴ後に真っ暗」)。
// 実機では adb が無いとログが読めないので、Exception/Error のログを受けたら自前のキャンバスに文字で貼る。
// UiKit・Theme に依存しない (それらの初期化が原因でも出せるように)。TMP の日本語フォントが読めればそれを、無ければ内蔵フォント。
// 触ると閉じる (エラーは残るので次のエラーでまた出る)。エディタでも出る (バッチのスクショで気づける)。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeckRogue.UI
{
    public static class ErrorOverlay
    {
        static readonly List<string> _lines = new List<string>();
        static GameObject _go;
        static TMP_Text _tmp;
        static Text _legacy;
        static bool _hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            Application.logMessageReceived += OnLog;
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            string s = condition;
            if (!string.IsNullOrEmpty(stackTrace))
            {
                // スタックは先頭の6行だけ (画面に収める)
                var st = stackTrace.Split('\n');
                int n = 0;
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < st.Length && n < 6; i++)
                {
                    if (st[i].Trim().Length == 0) continue;
                    sb.Append("\n  ").Append(st[i].Trim());
                    n++;
                }
                s += sb.ToString();
            }
            _lines.Add(s);
            if (_lines.Count > 8) _lines.RemoveAt(0);
            try { Show(); } catch (System.Exception) { /* 表示自体が失敗しても黙る (再帰しない) */ }
        }

        static void Show()
        {
            if (_go == null)
            {
                _go = new GameObject("ErrorOverlay", typeof(RectTransform));
                Object.DontDestroyOnLoad(_go);
                var canvas = _go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32000;
                var scaler = _go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.5f;
                _go.AddComponent<GraphicRaycaster>();

                var bg = new GameObject("bg", typeof(RectTransform)).AddComponent<Image>();
                bg.transform.SetParent(_go.transform, false);
                var brt = (RectTransform)bg.transform;
                brt.anchorMin = new Vector2(0f, 0f); brt.anchorMax = new Vector2(1f, 1f);
                brt.offsetMin = new Vector2(16f, 16f); brt.offsetMax = new Vector2(-16f, -16f);
                bg.color = new Color(0.12f, 0.02f, 0.02f, 0.92f);
                var btn = bg.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(delegate { if (_go != null) _go.SetActive(false); });

                var trt = new GameObject("text", typeof(RectTransform)).GetComponent<RectTransform>();
                trt.SetParent(bg.transform, false);
                trt.anchorMin = new Vector2(0f, 0f); trt.anchorMax = new Vector2(1f, 1f);
                trt.offsetMin = new Vector2(12f, 12f); trt.offsetMax = new Vector2(-12f, -12f);
                TMP_FontAsset font = null;
                try
                {
                    // UiKit.MakeFont と同じ作り方 (依存はしない)。日本語の例外文が読めるように
                    var f = Resources.Load<Font>("Fonts/NotoSansJP-Regular");
                    if (f != null) font = TMP_FontAsset.CreateFontAsset(f, 40, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                }
                catch (System.Exception) { font = null; }
                if (font == null) { try { font = TMP_Settings.defaultFontAsset; } catch (System.Exception) { } }
                if (font != null)
                {
                    _tmp = trt.gameObject.AddComponent<TextMeshProUGUI>();
                    _tmp.font = font;
                    _tmp.fontSize = 15;
                    _tmp.color = new Color(1f, 0.85f, 0.8f);
                    _tmp.alignment = TextAlignmentOptions.TopLeft;
                    _tmp.textWrappingMode = TextWrappingModes.Normal;
                    _tmp.overflowMode = TextOverflowModes.Overflow;
                    _tmp.richText = false;
                    _tmp.raycastTarget = false;
                }
                else
                {
                    _legacy = trt.gameObject.AddComponent<Text>();
                    _legacy.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    _legacy.fontSize = 15;
                    _legacy.color = new Color(1f, 0.85f, 0.8f);
                    _legacy.alignment = TextAnchor.UpperLeft;
                    _legacy.horizontalOverflow = HorizontalWrapMode.Wrap;
                    _legacy.verticalOverflow = VerticalWrapMode.Overflow;
                    _legacy.raycastTarget = false;
                }
            }
            _go.SetActive(true);
            string body = "ERROR (" + Application.version + " / " + Application.platform + " / " + SystemInfo.graphicsDeviceType + ") — tap to close\n\n" + string.Join("\n\n", _lines.ToArray());
            if (_tmp != null) _tmp.text = body;
            if (_legacy != null) _legacy.text = body;
        }
    }
}
