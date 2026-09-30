// TiltShiftSettings.cs — ティルトシフト (自作のぼかし) の設定 (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3・§2-6)。
// 骨組み (P00): 値の置き場だけ。パスとフックは P07 (TiltShiftPass・TiltShiftHook) が書き、値は StageLook (P09) と座席の深さ (P10) が入れる。
// 既定は無効 = 今の見た目。
namespace DeckRogue.Game
{
    public static class TiltShiftSettings
    {
        /// <summary>ぼかしを掛ける (false = 掛けない)</summary>
        public static bool Enabled = false;
        /// <summary>座席の帯の手前と奥の深さ (カメラからの距離・unit)。帯の中はぼかし0</summary>
        public static float BandNear = 0f, BandFar = 0f;
        /// <summary>帯の外でぼかしが上限に届くまでの距離 (手前側・奥側・unit)</summary>
        public static float RampNear = 0f, RampFar = 0f;
        /// <summary>ぼかしの半径の上限 (1080 基準の px)。PC 24・スマホ 12</summary>
        public static float MaxPxPC = 24f, MaxPxPhone = 12f;
        /// <summary>半解像度でぼかす (スマホの段)</summary>
        public static bool HalfRes = false;
        /// <summary>崩れた時の逃げ道: 自作のパスをやめて URP の Bokeh に戻す (dof=urp)</summary>
        public static bool UseUrpBokeh = false;
    }
}
