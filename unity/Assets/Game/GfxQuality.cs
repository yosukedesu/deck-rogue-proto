// GfxQuality.cs — 画質の段 (2026-10-04 ユーザー「エフェクト追加後、別の端末でカクつく。ハイスペのスマホでは美麗にしたい」)。
// 段は 高 / 標準 / 軽量。設定の窓の「画質」で 自動・高・標準・軽量 を選ぶ (PlayerPrefs gfx.mode)。
// 自動 = スマホだけ、箱庭の戦闘の重さ (1コマの時間) を測って、重ければ1段ずつ下げて覚える (gfx.auto)。上げはしない (設定で「自動」を選び直すと高から測り直す)。
// PC は自動でも高のまま (撮影の exe の時間に左右されない)。
// 今は技の光 (StageMotion)・火花・エフェクトの暈 (Tween.HitGlow) にだけ効く:
//   高 = 今の全部 / 標準 = 技の光は影なし・火花 1/3・暈 1 枚 / 軽量 = 技の光 1 つだけ (壁の光なし)・火花と暈なし
using UnityEngine;

namespace DeckRogue.Game
{
    public enum GfxLevel { High = 0, Standard = 1, Low = 2 }

    public static class GfxQuality
    {
        /// <summary>設定の値: -1 = 自動・0〜2 = 手で選んだ段</summary>
        public static int Mode
        {
            get { return PlayerPrefs.GetInt("gfx.mode", -1); }
            set
            {
                PlayerPrefs.SetInt("gfx.mode", value);
                if (value < 0) PlayerPrefs.SetInt("gfx.auto", 0);   // 自動を選び直したら高から測り直す
                PlayerPrefs.Save();
                ResetSampler();
            }
        }

        public static GfxLevel Level
        {
            get
            {
                int m = Mode;
                if (m >= 0) return (GfxLevel)Mathf.Clamp(m, 0, 2);
                return (GfxLevel)Mathf.Clamp(PlayerPrefs.GetInt("gfx.auto", 0), 0, 2);
            }
        }

        public static bool IsAuto => Mode < 0;

        // ---- 自動 (スマホの箱庭の戦闘の間だけ測る) ----
        const int Warmup = 90;       // 組み直しの直後は重いので数えない
        const int Window = 180;      // 約 3 秒ぶん
        const float SlowFrame = 1f / 40f;   // 25ms を超えるコマ
        const float SlowShare = 0.2f;       // 窓の 2 割以上が遅ければ 1 段下げる
        static int _frames, _slow;

        static void ResetSampler() { _frames = 0; _slow = 0; }

        /// <summary>組み直し (戦闘の始まり・画面の切り替え) の時に呼ぶ: 測り直す</summary>
        public static void Restart() { ResetSampler(); }

        /// <summary>StageDriver の毎フレーム。箱庭の戦闘中のスマホで自動の時だけ数える</summary>
        public static void Sample(float unscaledDt, bool inBattle)
        {
            if (!IsAuto || !Application.isMobilePlatform || !inBattle || !HD2DFlags.DioramaHere) return;
            if (Level == GfxLevel.Low) return;
            _frames++;
            if (_frames <= Warmup) return;
            if (unscaledDt > SlowFrame) _slow++;
            if (_frames < Warmup + Window) return;
            if (_slow >= Window * SlowShare)
            {
                int next = Mathf.Min(2, (int)Level + 1);
                PlayerPrefs.SetInt("gfx.auto", next); PlayerPrefs.Save();
                Debug.Log("[GfxQuality] 重い (" + _slow + "/" + Window + " コマが 25ms 超) → 画質を " + Name((GfxLevel)next) + " へ");
            }
            ResetSampler();
        }

        public static string Name(GfxLevel l) { return l == GfxLevel.High ? "高" : l == GfxLevel.Standard ? "標準" : "軽量"; }
    }
}
