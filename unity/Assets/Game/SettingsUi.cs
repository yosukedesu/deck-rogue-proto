// SettingsUi.cs — 設定の窓 (2026-09-21 ユーザー「exe は解像度設定できるようにして欲しい。フルスクリーン対応は欲しいし音量調整とかもね」)。
// 画面 (ウィンドウ/フルスクリーン・解像度) と音量 (全体・BGM・効果音)。タイトルの「設定」と ≡ メニューの「設定」から開く。
// 保存は PlayerPrefs (video.w / video.h / video.full と Audio の audio.*)。起動時に GameRoot が Video.ApplySaved で戻す。
// F11 と Alt+Enter でフルスクリーンの切替 (GameRoot.Update)。スマホでは画面の欄は出さない (解像度は端末が決める)。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    /// <summary>画面設定の保存と適用</summary>
    public static class Video
    {
        /// <summary>選べる解像度 (16:9)。端末の画面より大きいものは出さない</summary>
        static readonly int[][] Presets = { new[] { 1280, 720 }, new[] { 1600, 900 }, new[] { 1920, 1080 }, new[] { 2560, 1440 }, new[] { 3840, 2160 } };

        public static bool IsFullscreen { get { return Screen.fullScreenMode != FullScreenMode.Windowed; } }

        /// <summary>この端末で選べる解像度 (端末の画面の大きさ以下・重複なし・小さい順)。端末そのものの大きさも入れる</summary>
        public static List<int[]> Choices()
        {
            int sw = Display.main.systemWidth, sh = Display.main.systemHeight;
            var list = new List<int[]>();
            foreach (var p in Presets) if (p[0] <= sw && p[1] <= sh) list.Add(p);
            if (sw > 0 && sh > 0 && !list.Exists(p => p[0] == sw && p[1] == sh)) list.Add(new[] { sw, sh });
            list.Sort((a, b) => a[0] != b[0] ? a[0].CompareTo(b[0]) : a[1].CompareTo(b[1]));
            return list;
        }

        public static void Apply(int w, int h, bool full)
        {
            if (w <= 0 || h <= 0) return;
            Screen.SetResolution(w, h, full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            PlayerPrefs.SetInt("video.w", w);
            PlayerPrefs.SetInt("video.h", h);
            PlayerPrefs.SetInt("video.full", full ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ToggleFullscreen()
        {
            bool full = !IsFullscreen;
            int w = PlayerPrefs.GetInt("video.w", Screen.width), h = PlayerPrefs.GetInt("video.h", Screen.height);
            if (full) { w = Display.main.systemWidth; h = Display.main.systemHeight; }   // フルスクリーンは端末の大きさで (拡大のぼけを避ける)
            Apply(w, h, full);
        }

        /// <summary>起動時: 保存した設定があれば戻す (スマホ・バッチ・自動操縦では触らない)</summary>
        public static void ApplySaved()
        {
            if (Application.isMobilePlatform || Application.isBatchMode) return;
            foreach (var a in Environment.GetCommandLineArgs()) if (a == "-autopilot") return;
            if (!PlayerPrefs.HasKey("video.w")) return;
            int w = PlayerPrefs.GetInt("video.w"), h = PlayerPrefs.GetInt("video.h");
            bool full = PlayerPrefs.GetInt("video.full", 0) == 1;
            if (w > 0 && h > 0 && (w != Screen.width || h != Screen.height || full != IsFullscreen)) Screen.SetResolution(w, h, full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
    }

    public static class SettingsUi
    {
        /// <summary>設定の窓。外側を触るか「閉じる」で閉じる</summary>
        public static void Build(GameRoot g, RectTransform root)
        {
            bool phone = UiKit.Phone;
            float h = phone ? 590f : 930f;   // スマホは画質の段の注記 1 行ぶん (2026-10-30)。PC は「演出」の段 (2026-11-04) のぶん高く
            var inner = BattleScreen.Modal(root, 820f, h, "settings");
            // 外側 (背景) を触ると閉じる
            var backdrop = inner.parent.parent as RectTransform;
            if (backdrop != null)
            {
                var cb = backdrop.gameObject.GetComponent<Button>() ?? backdrop.gameObject.AddComponent<Button>();
                cb.transition = Selectable.Transition.None;
                cb.onClick.AddListener(delegate { g.SettingsOpen = false; g.Rebuild(); });
                var winImg = inner.parent.GetComponent<Image>();
                if (winImg != null) winImg.raycastTarget = true;   // 窓の中のクリックは背景へ抜けない
            }
            UiKit.Head(inner, "設定", 26);
            // スマホは窓の高さが 675 しか無いので、段を縦のしおりつきの一覧に入れる (2026-11-04 「演出」の段が増えた)。PC は窓にそのまま
            RectTransform body = inner;
            if (phone)
            {
                body = UiKit.Scroll(inner, true, new Color(0f, 0f, 0f, 0f), 10, 0);
                UiKit.Le(UiKit.ScrollRoot(body), -1f, 0f, -1f, 0f, -1f, 1f);
            }

            if (!phone)
            {
                // ---- 画面 ----
                Section(inner, "画面");
                var modeRow = Row(inner);
                bool full = Video.IsFullscreen;
                var bw = UiKit.Btn(modeRow, "ウィンドウ", delegate { Video.Apply(PlayerPrefs.GetInt("video.w", Screen.width), PlayerPrefs.GetInt("video.h", Screen.height), false); Later(g); }, 17, true, full ? (Color?)null : PaperFx.BrassLight);
                BattleScreen.SetSize(bw, 200f, 46f);
                var bf = UiKit.Btn(modeRow, "フルスクリーン", delegate { Video.Apply(Display.main.systemWidth, Display.main.systemHeight, true); Later(g); }, 17, true, full ? PaperFx.BrassLight : (Color?)null);
                BattleScreen.SetSize(bf, 200f, 46f);
                var hint = UiKit.Txt(modeRow, "F11 か Alt+Enter でも切り替え", 14, UiKit.ColDim, TextAnchor.MiddleLeft);
                UiKit.Le(hint, 200f, 46f, -1f, 46f);

                var resLabel = UiKit.Txt(inner, "解像度（ウィンドウの大きさ。フルスクリーンでは画面の大きさに合わせる）  いま " + Screen.width + "×" + Screen.height, 14, UiKit.ColDim, TextAnchor.MiddleLeft);
                UiKit.Le(resLabel, -1f, 24f, -1f, 24f);
                var resRow = Row(inner);
                var flow = resRow.GetComponent<HorizontalLayoutGroup>();
                flow.childForceExpandWidth = false;
                foreach (var p in Video.Choices())
                {
                    int w = p[0], hh = p[1];
                    bool cur = Screen.width == w && Screen.height == hh;
                    var b = UiKit.Btn(resRow, w + "×" + hh, delegate { Video.Apply(w, hh, Video.IsFullscreen); Later(g); }, 16, true, cur ? PaperFx.BrassLight : (Color?)null);
                    BattleScreen.SetSize(b, 148f, 44f);
                }
            }

            // ---- 画質 (2026-10-04: 技の光・火花・エフェクトの暈。自動 = スマホは戦闘の重さで下げる) ----
            Section(body, "画質");
            var qRow = Row(body);
            int qm = GfxQuality.Mode;
            string[] qNames = { "自動", "高", "標準", "軽量" };
            for (int qi = 0; qi < 4; qi++)
            {
                int val = qi - 1;
                var qb = UiKit.Btn(qRow, qNames[qi], delegate { GfxQuality.Mode = val; g.Rebuild(); }, 17, true, qm == val ? PaperFx.BrassLight : (Color?)null);
                BattleScreen.SetSize(qb, 130f, 46f);
            }
            var qHint = UiKit.Txt(qRow, GfxQuality.IsAuto ? "いま " + GfxQuality.Name(GfxQuality.Level) : "", 14, UiKit.ColDim, TextAnchor.MiddleLeft);
            UiKit.Le(qHint, 100f, 46f, -1f, 46f);
            // fps の札 (2026-10-30 ユーザー「Xperia 5 II だとかなりカクつく」): 左上に fps・1 コマの時間・最悪のコマを出す (PerfHud)。重さを伝えてもらう時に
            bool fpsOn = GfxQuality.FpsHud;
            var fb = UiKit.Btn(qRow, "fps", delegate { GfxQuality.FpsHud = !fpsOn; g.Rebuild(); }, 17, true, fpsOn ? PaperFx.BrassLight : (Color?)null);
            BattleScreen.SetSize(fb, 90f, 46f);
            if (phone)
            {
                // スマホの段の中身 (GfxQuality.cs): 標準 = 舞台の光の筋・霧・影を減らす。軽量 = さらに舞台を 0.75 倍で描き、ぼかしと影を切る。文字と札は変わらない
                var qNote = UiKit.Txt(body, "標準＝舞台の光の筋・霧・影を減らす。軽量＝さらに舞台を 0.75 倍で描き、ぼかしと影を切る（文字と札はそのまま）。次の戦闘から", 14, UiKit.ColDim, TextAnchor.MiddleLeft);
                UiKit.Le(qNote, -1f, 24f, -1f, 24f);
            }

            // ---- 演出 (2026-11-04 見た目・UX・演出の磨き 束 C2・G12/M13): 揺れ・閃光・一瞬の止め・速さ・戦闘の評価の窓。値は FxSettings (PlayerPrefs) ----
            Section(body, "演出");
            Slider(body, "画面の揺れ", FxSettings.Shake, v => { FxSettings.Shake = v; }, false, 1.5f);
            Slider(body, "閃光", FxSettings.Flash, v => { FxSettings.Flash = v; }, false);
            Slider(body, "一瞬の止め", FxSettings.Pause, v => { FxSettings.Pause = v; }, false);
            var fxRow = Row(body);
            var spdLabel = UiKit.Txt(fxRow, "速さ", 17, UiKit.ColInk, TextAnchor.MiddleLeft);
            UiKit.Le(spdLabel, 110f, 44f, 110f, 44f);
            bool fast = FxSettings.Speed > 1.2f;
            var bNormal = UiKit.Btn(fxRow, "ふつう", delegate { FxSettings.Speed = 1f; g.Rebuild(); }, 17, true, fast ? (Color?)null : PaperFx.BrassLight);
            BattleScreen.SetSize(bNormal, 130f, 46f);
            var bFast = UiKit.Btn(fxRow, "速い", delegate { FxSettings.Speed = 1.5f; g.Rebuild(); }, 17, true, fast ? PaperFx.BrassLight : (Color?)null);
            BattleScreen.SetSize(bFast, 130f, 46f);
            Tooltip.Attach(bFast.gameObject, delegate { return "幕・札の飛び・数え上げ・決着の余韻を 1.5 倍の速さで（本家の高速モードと同じ）"; });
            bool rating = FxSettings.RatingWindow;
            var bRate = UiKit.Btn(fxRow, rating ? "戦闘の評価の窓：出す" : "戦闘の評価の窓：出さない", delegate { FxSettings.RatingWindow = !rating; g.Rebuild(); }, 15, true, rating ? PaperFx.BrassLight : (Color?)null);
            BattleScreen.SetSize(bRate, phone ? 260f : 300f, 46f);
            Tooltip.Attach(bRate.gameObject, delegate { return "戦闘が終わるたびに強さ・面白さを聞く窓。出さない時も、報酬の画面の左下の「評価」から付けられる（調整の材料になります）"; });

            // ---- 音量 ----
            Section(body, "音量");
            Slider(body, "全体", Audio.Master, v => { Audio.Master = v; }, true);
            Slider(body, "BGM", Audio.BgmVol, v => { Audio.BgmVol = v; }, false);
            Slider(body, "効果音", Audio.SfxVol, v => { Audio.SfxVol = v; }, true);

            if (!phone)
            {   // PC は残りの高さを空ける (スマホは一覧 (body) が残りを取るので置かない。2026-11-04)
                var spacer = UiKit.NewRect("spacer", inner);
                UiKit.Le(spacer, -1f, 4f, -1f, -1f, -1f, 1f);
            }
            var btns = Row(inner);
            btns.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            var close = UiKit.Btn(btns, "閉じる", delegate { g.SettingsOpen = false; g.Rebuild(); }, 18, true, PaperFx.BrassLight);
            BattleScreen.SetSize(close, 220f, 50f);
        }

        /// <summary>解像度を変えた直後は Screen の値が次のフレームまで古いので、2フレーム待ってから組み直す (StageDriver も追随する)</summary>
        static void Later(GameRoot g)
        {
            g.StartCoroutine(RebuildLater(g));
        }

        static System.Collections.IEnumerator RebuildLater(GameRoot g)
        {
            yield return null;
            yield return null;
            g.Rebuild();
        }

        static void Section(RectTransform inner, string title)
        {
            var t = UiKit.Deco(inner, title, 20, PaperFx.BrassInk, TextAnchor.MiddleLeft);
            UiKit.Le(t, -1f, 34f, -1f, 34f);
            var rule = UiKit.Pan(inner, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.25f), "rule");
            UiKit.Le(rule, -1f, 2f, -1f, 2f);
        }

        static RectTransform Row(RectTransform inner)
        {
            var row = UiKit.NewRect("row", inner);
            var hg = UiKit.Horz(row, 12, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandHeight = false;
            hg.childForceExpandWidth = false;
            UiKit.Le(row, -1f, 50f, -1f, 50f);
            return row;
        }

        /// <summary>横のつまみ: 名前・帯・%。動かした値はすぐ保存。sfxTest=手を離した時に効果音を1つ鳴らして確かめる。max=つまみの右端の値 (揺れは 1.5 まで。% は 1.0 を 100 として出す)</summary>
        static void Slider(RectTransform inner, string label, float value, Action<float> onChange, bool sfxTest, float max = 1f)
        {
            var row = Row(inner);
            var name = UiKit.Txt(row, label, 17, UiKit.ColInk, TextAnchor.MiddleLeft);
            UiKit.Le(name, 110f, 44f, 110f, 44f);
            var srt = UiKit.NewRect("slider", row);
            UiKit.Le(srt, 420f, 32f, 420f, 32f);
            var bg = UiKit.Pan(srt, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.18f), "bg");
            UiKit.Anchor(bg.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -6f), new Vector2(0f, 6f));
            bg.raycastTarget = false;
            var fillArea = UiKit.NewRect("fill-area", srt);
            UiKit.Anchor(fillArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -6f), new Vector2(0f, 6f));
            var fill = UiKit.Pan(fillArea, PaperFx.Brass, "fill");
            UiKit.Anchor(fill.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            fill.raycastTarget = false;
            var handleArea = UiKit.NewRect("handle-area", srt);
            UiKit.Anchor(handleArea, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-12f, 0f));
            var handle = UiKit.NewRect("handle", handleArea);
            handle.sizeDelta = new Vector2(24f, 24f);
            var hImg = handle.gameObject.AddComponent<Image>();
            hImg.sprite = PaperFx.Ring(12);
            hImg.color = PaperFx.BrassLight;
            var ringInk = UiKit.Pan(handle, PaperFx.BrassInk, "dot");
            UiKit.Anchor(ringInk.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-5f, -5f), new Vector2(5f, 5f));
            ringInk.raycastTarget = false;
            var sl = srt.gameObject.AddComponent<UnityEngine.UI.Slider>();
            sl.fillRect = fill.rectTransform;
            sl.handleRect = handle;
            sl.targetGraphic = hImg;
            sl.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            sl.minValue = 0f; sl.maxValue = max; sl.wholeNumbers = false;
            sl.value = Mathf.Clamp(value, 0f, max);
            var pct = UiKit.Txt(row, Mathf.RoundToInt(value * 100f) + "%", 17, UiKit.ColInk, TextAnchor.MiddleRight);
            UiKit.Le(pct, 70f, 44f, 70f, 44f);
            sl.onValueChanged.AddListener(v =>
            {
                pct.text = Mathf.RoundToInt(v * 100f) + "%";
                onChange(v);
            });
            if (sfxTest)
            {
                var et = srt.gameObject.AddComponent<EventTrigger>();
                var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                up.callback.AddListener(delegate { Audio.Ui("click"); });
                et.triggers.Add(up);
            }
        }
    }
}
