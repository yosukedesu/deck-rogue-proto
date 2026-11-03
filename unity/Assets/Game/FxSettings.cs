// FxSettings.cs — 演出の量の設定 (2026-11-04 見た目・UX・演出の磨き)。
// 画面の揺れ・閃光・動きの速さを PlayerPrefs で持ち、設定の窓 (SettingsUi) から変えられる。
// 読む側: StageBeat (舞台の揺れ・閃光)・Presenter (ヒットストップ・数字)・ScreenFx (幕・入り)。
// 値は「倍率」: 1 = 既定、0 = 無し。揺れに弱い人は Shake を 0 にする。
using UnityEngine;

namespace DeckRogue.Game
{
    public static class FxSettings
    {
        /// <summary>画面 (舞台) の揺れの倍率 0〜1.5。0 で揺れない</summary>
        public static float Shake { get { return PlayerPrefs.GetFloat("fx.shake", 1f); } set { PlayerPrefs.SetFloat("fx.shake", Mathf.Clamp(value, 0f, 1.5f)); } }
        /// <summary>全画面の閃光 (白・薔薇) の倍率 0〜1。0 で閃かない (舞台の光の演出は残る)</summary>
        public static float Flash { get { return PlayerPrefs.GetFloat("fx.flash", 1f); } set { PlayerPrefs.SetFloat("fx.flash", Mathf.Clamp01(value)); } }
        /// <summary>間 (ヒットストップ・とどめのスロー) の倍率 0〜1。0 で時間を止めない</summary>
        public static float Pause { get { return PlayerPrefs.GetFloat("fx.pause", 1f); } set { PlayerPrefs.SetFloat("fx.pause", Mathf.Clamp01(value)); } }
        /// <summary>演出の速さ 1 = 既定・1.5 = 速い (本家の「高速モード」相当)。演出の秒数を割る側で使う</summary>
        public static float Speed { get { return PlayerPrefs.GetFloat("fx.speed", 1f); } set { PlayerPrefs.SetFloat("fx.speed", Mathf.Clamp(value, 0.5f, 2f)); } }
        /// <summary>戦闘直後の評価の窓を出すか (プレイテストの計測器。既定は出す)</summary>
        public static bool RatingWindow { get { return PlayerPrefs.GetInt("fx.rating", 1) != 0; } set { PlayerPrefs.SetInt("fx.rating", value ? 1 : 0); } }
        /// <summary>秒数に速さを掛ける (Speed 1.5 なら 2/3 の長さ)</summary>
        public static float Dur(float seconds) { var s = Speed; return s <= 0f ? seconds : seconds / s; }
    }
}
