// GfxQuality.cs — 画質の段 (2026-10-04 ユーザー「エフェクト追加後、別の端末でカクつく。ハイスペのスマホでは美麗にしたい」)。
// 段は 高 / 標準 / 軽量。設定の窓の「画質」で 自動・高・標準・軽量 を選ぶ (PlayerPrefs gfx.mode)。
// 自動 = スマホだけ、1コマの時間を測って、重ければ1段ずつ下げて覚える (gfx.auto)。上げはしない (設定で「自動」を選び直すと高から測り直す)。
// PC は自動でも高のまま (撮影の exe の時間に左右されない)。
//
// 2026-10-30 (ユーザー「Xperia 5 II だとかなりカクつく。S25 はカクつかない」): 段の中身を舞台 (箱庭) の重さにも広げた。スマホの段 (実機か tier=phone) にだけ効く。
//   高   = 今の全部
//   標準 = 技の光は影なし・火花 1/3・暈 1 枚 ＋ 箱庭の軽い設計図 look_act1.light (光の筋・霧の面・霧の板を組まない・揺れと法線なし・逆光なし・影つきの光は月だけ 256・グレインと色収差なし)
//   軽量 = 標準 ＋ 技の光 1 つだけ・火花と暈なし ＋ 舞台を 0.75 倍で描く (URP の renderScale。UI は別のキャンバスなので文字と札は等倍のまま) ＋ ぼかし (ティルトシフト) なし ＋ 影なし (look_act1.light2)
//   どの段でも: スマホの段では地図とタイトルの間、舞台のカメラは何も描かない (Stage.SetIdle。箱庭と後処理が UI の裏で丸ごと描かれていた)
//   設計図 (.light) は次の箱庭の組み立て (次の戦闘) から効く。renderScale とぼかしはその場で変わる。
// 自動の測り方の直し (同日): 旧は「25ms を超えるコマ」を数えていたが、スマホは targetFrameRate 30 = 1 コマ 33ms なので全コマが「遅い」になり、
//   どの端末でも最初の戦闘の 18 秒で軽量まで落ちていた (S25 も)。新 = 目標の 1.5 倍 (30fps なら 50ms) を超えるコマが窓 (約 6 秒) の 2 割以上。
//   戦闘の外 (地図・店・タイトル) でも測る。組み直し (GameRoot.Rebuild) の直後は 90 コマ数えない (組み立ての引っかかりを数えない)。
// 撮影の旗: gfx=high|std|low (この起動だけ。PlayerPrefs に書かない)・fpshud=1 (PerfHud を出す)。
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DeckRogue.Game
{
    public enum GfxLevel { High = 0, Standard = 1, Low = 2 }

    public static class GfxQuality
    {
        /// <summary>この起動だけの上書き (撮影の旗 gfx=)。-1 = 無し。PlayerPrefs には書かない</summary>
        public static int Override = -1;

        /// <summary>設定の値: -1 = 自動・0〜2 = 手で選んだ段</summary>
        public static int Mode
        {
            get { return PlayerPrefs.GetInt("gfx.mode", -1); }
            set
            {
                PlayerPrefs.SetInt("gfx.mode", value);
                if (value < 0) PlayerPrefs.SetInt("gfx.auto", 0);   // 自動を選び直したら高から測り直す
                PlayerPrefs.Save();
                Restart();
                Apply();
            }
        }

        public static GfxLevel Level
        {
            get
            {
                if (Override >= 0) return (GfxLevel)Mathf.Clamp(Override, 0, 2);
                int m = Mode;
                if (m >= 0) return (GfxLevel)Mathf.Clamp(m, 0, 2);
                return (GfxLevel)Mathf.Clamp(PlayerPrefs.GetInt("gfx.auto", 0), 0, 2);
            }
        }

        public static bool IsAuto => Override < 0 && Mode < 0;

        /// <summary>スマホの段 (実機か tier=phone)。舞台の軽さはこちらにだけ効く (PC の撮影の画を変えない)</summary>
        public static bool PhoneLike => Application.isMobilePlatform || HD2DFlags.Tier == HD2DTier.Phone;

        /// <summary>箱庭の軽い設計図 (.light) を重ねる = スマホの段で 標準 以下</summary>
        public static bool LightStage => PhoneLike && Level != GfxLevel.High;

        /// <summary>いちばん軽い段 (.light2・舞台 0.75 倍・ぼかしなし) = スマホの段で 軽量</summary>
        public static bool LowStage => PhoneLike && Level == GfxLevel.Low;

        /// <summary>軽量で舞台を描く倍率 (2520×1080 → 1890×810 相当。ドットの粒 1.8px → 1.35px)</summary>
        public const float LowRenderScale = 0.75f;

        // ---- fps の札 (PerfHud) ----
        /// <summary>撮影の旗 fpshud=1 (この起動だけ)</summary>
        public static bool FpsHudForce;
        /// <summary>設定の窓「fps」(PlayerPrefs gfx.fps)</summary>
        public static bool FpsHud
        {
            get { return FpsHudForce || PlayerPrefs.GetInt("gfx.fps", 0) == 1; }
            set { PlayerPrefs.SetInt("gfx.fps", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        static float _scaleBefore = float.NaN;
        static bool _quitHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { Apply(); }

        /// <summary>段から決まる描画の値を今のパイプラインに書く (起動・段の変更・自動で下げた時・撮影の旗)。舞台の設計図 (.light) は次の箱庭の組み立てから</summary>
        public static void Apply()
        {
            try
            {
                var urp = UniversalRenderPipeline.asset;
                if (urp != null)
                {
                    // 軽量なら 0.75、そうでなければ自分が書く前の値へ (アセットの値を 1 と決めつけない)
                    float want = LowStage ? LowRenderScale : (float.IsNaN(_scaleBefore) ? urp.renderScale : _scaleBefore);
                    if (!Mathf.Approximately(urp.renderScale, want))
                    {
                        if (float.IsNaN(_scaleBefore)) _scaleBefore = urp.renderScale;
                        urp.renderScale = want;
                        // エディタでは実行時に書いた値がアセットに残るので、終わりに戻す (StageLook の控えと同じ作法)
                        if (!_quitHooked) { _quitHooked = true; Application.quitting += RestoreScale; }
                        Debug.Log("[GfxQuality] " + Name(Level) + ": 舞台の描く倍率 renderScale=" + want.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }
            catch (System.Exception e) { Debug.LogWarning("[GfxQuality] renderScale を書けない: " + e.Message); }
            StageLook.OnQualityChanged();
        }

        static void RestoreScale()
        {
            try { var urp = UniversalRenderPipeline.asset; if (urp != null && !float.IsNaN(_scaleBefore)) urp.renderScale = _scaleBefore; } catch (System.Exception) { }
        }

        // ---- 自動 (スマホで測る) ----
        const int Warmup = 90;        // 組み直しの直後は重いので数えない
        const int Window = 180;       // 30fps で約 6 秒ぶん
        const float SlowShare = 0.2f; // 窓の 2 割以上が遅ければ 1 段下げる
        static int _frames, _slow;

        /// <summary>組み直し (戦闘の始まり・画面の切り替え) の時に呼ぶ: 測り直す</summary>
        public static void Restart() { _frames = 0; _slow = 0; }

        /// <summary>「遅いコマ」の線: 目標 (targetFrameRate。-1 なら画面のリフレッシュ) の 1.5 倍。30fps なら 50ms</summary>
        public static float SlowFrame()
        {
            int fps = Application.targetFrameRate;
            if (fps <= 0)
            {
                try { fps = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value); } catch (System.Exception) { fps = 0; }
                if (fps <= 0) fps = 60;
            }
            return 1.5f / fps;
        }

        /// <summary>StageDriver の毎フレーム。スマホで自動の時だけ数える (戦闘の外でも)</summary>
        public static void Sample(float unscaledDt, bool inBattle)
        {
            if (!IsAuto || !Application.isMobilePlatform) return;
            if (Level == GfxLevel.Low) return;
            _frames++;
            if (_frames <= Warmup) return;
            if (unscaledDt > SlowFrame()) _slow++;
            if (_frames < Warmup + Window) return;
            if (_slow >= Window * SlowShare)
            {
                int next = Mathf.Min(2, (int)Level + 1);
                PlayerPrefs.SetInt("gfx.auto", next); PlayerPrefs.Save();
                Debug.Log("[GfxQuality] 重い (" + _slow + "/" + Window + " コマが " + Mathf.RoundToInt(SlowFrame() * 1000f) + "ms 超・" + (inBattle ? "戦闘" : "戦闘の外") + ") → 画質を " + Name((GfxLevel)next) + " へ");
                Apply();
            }
            Restart();
        }

        public static string Name(GfxLevel l) { return l == GfxLevel.High ? "高" : l == GfxLevel.Standard ? "標準" : "軽量"; }
    }
}
