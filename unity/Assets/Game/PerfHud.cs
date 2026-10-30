// PerfHud.cs — 画面の左上に fps・1 コマの時間・直近 1 秒の最悪のコマを出す小さな札 (2026-10-30 ユーザー「Xperia 5 II だとかなりカクつく」)。
// 実機で adb が無くても、遊んでいる人が数字を読んで伝えられるように。設定の窓の「fps」(PlayerPrefs gfx.fps) か撮影の旗 fpshud=1 で出す。
// ErrorOverlay と同じく自前のキャンバス (UI の組み直しで消えない・UiKit の字が無ければ内蔵の字)。0.5 秒ごとに文字を書き直す (毎フレームの組み直しは重い)。
// 読み方: fps が 30 近くで「最悪」が 100ms を超えるなら引っかかり (組み立て・読み込み)、fps そのものが低いなら描く重さ。両方を読む。
// 2 行目は今の画質の段・GPU の名前・画面の大きさ・舞台の描く倍率 (軽量なら 0.75)。
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;

namespace DeckRogue.Game
{
    public class PerfHud : MonoBehaviour
    {
        static PerfHud _inst;
        TMP_Text _tmp;
        Text _legacy;
        float _t, _sum, _worst; int _n, _hitches;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        const float HitchSec = 0.1f;   // 100ms を超えるコマ = 引っかかり

        /// <summary>設定と旗に合わせて出す／消す (GameRoot.Rebuild から)</summary>
        public static void Sync()
        {
            bool want = GfxQuality.FpsHud;
            if (want && _inst == null) Create();
            else if (!want && _inst != null) { Object.Destroy(_inst.gameObject); _inst = null; }
        }

        static void Create()
        {
            var go = new GameObject("PerfHud", typeof(RectTransform));
            Object.DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 31000;   // ErrorOverlay (32000) のすぐ下・ゲームの UI (0) の上
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            _inst = go.AddComponent<PerfHud>();

            var bg = new GameObject("bg", typeof(RectTransform)).AddComponent<Image>();
            bg.transform.SetParent(go.transform, false);
            var brt = (RectTransform)bg.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            float left = 8f;
            try { if (Screen.safeArea.xMin > 0.5f) left += Screen.safeArea.xMin * 1080f / Mathf.Max(1, Screen.height); } catch (System.Exception) { }
            brt.anchoredPosition = new Vector2(left, -6f);
            brt.sizeDelta = new Vector2(560f, 50f);
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = false;

            var trt = new GameObject("text", typeof(RectTransform)).GetComponent<RectTransform>();
            trt.SetParent(bg.transform, false);
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(8f, 2f); trt.offsetMax = new Vector2(-8f, -2f);
            TMP_FontAsset font = null;
            try { font = UiKit.FontRegular; } catch (System.Exception) { font = null; }
            if (font == null) { try { font = TMP_Settings.defaultFontAsset; } catch (System.Exception) { } }
            if (font != null)
            {
                var t = trt.gameObject.AddComponent<TextMeshProUGUI>();
                t.font = font; t.fontSize = 18; t.color = new Color(0.95f, 0.95f, 0.9f);
                t.alignment = TextAlignmentOptions.TopLeft; t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
                t.richText = false; t.raycastTarget = false;
                _inst._tmp = t;
            }
            else
            {
                var t = trt.gameObject.AddComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = 18; t.color = new Color(0.95f, 0.95f, 0.9f);
                t.alignment = TextAnchor.UpperLeft; t.raycastTarget = false;
                _inst._legacy = t;
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _t += dt; _sum += dt; _n++;
            if (dt > _worst) _worst = dt;
            if (dt > HitchSec) _hitches++;
            if (_t < 0.5f) return;
            float avg = _n > 0 ? _sum / _n : 0f;
            string s1 = (avg > 0f ? Mathf.RoundToInt(1f / avg) : 0).ToString(Inv) + " fps  " + Mathf.RoundToInt(avg * 1000f).ToString(Inv) + " ms  最悪 "
                + Mathf.RoundToInt(_worst * 1000f).ToString(Inv) + " ms  引っかかり " + _hitches.ToString(Inv);
            float scale = 1f;
            try { var urp = UniversalRenderPipeline.asset; if (urp != null) scale = urp.renderScale; } catch (System.Exception) { }
            string s2 = GfxQuality.Name(GfxQuality.Level) + (GfxQuality.IsAuto ? "（自動）" : "") + "  " + SystemInfo.graphicsDeviceName + "  " + Screen.width.ToString(Inv) + "×" + Screen.height.ToString(Inv)
                + "  目標 " + Application.targetFrameRate.ToString(Inv) + "  舞台 ×" + scale.ToString("0.00", Inv);
            string s = s1 + "\n" + s2;
            if (_tmp != null) _tmp.text = s; else if (_legacy != null) _legacy.text = s;
            _t = 0f; _sum = 0f; _n = 0; _worst = 0f;
        }
    }
}
