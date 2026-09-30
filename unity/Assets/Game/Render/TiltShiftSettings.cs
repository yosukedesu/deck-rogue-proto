// TiltShiftSettings.cs — ティルトシフト (自作のぼかし) の設定 (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3・§2-6)。
// 骨組み (P00): 値の置き場だけ。パスとフックは P07 (TiltShiftPass・TiltShiftHook) が書き、値は StageLook (P09) と座席の深さ (P10) が入れる。
// 既定は無効 = 今の見た目。
// P24 (W3) が足した値: Focus・PathNear・PathFar・NearScale (ピントの帯の形と手前の上限)。入れるのは TiltShiftLook (look_act1.dof.json の tiltShiftPass)。
// P24 (W3b・2周目) が足した値: Curve・LensFar・LensNear (帯の外のぼけの伸び方。既定はレンズ)。入れるのは同じく TiltShiftLook。
namespace DeckRogue.Game
{
    /// <summary>
    /// ピントの帯の形 (P24)。
    /// Path  = 道に沿った帯: 画素の世界の点の「道の座標 s」(道と直角・+ が奥) が PathNear〜PathFar ならぼかし 0。帯の外は、視線に沿った深さに直して
    ///         RampNear・RampFar で上限へ (焦点面を座席の列に沿って傾ける = ティルトシフトのレンズと同じ考え方)。箱庭 (Diorama) がある時だけ。
    /// Depth = カメラからの深さの帯 (BandNear〜BandFar。P07 の元の形・箱庭が無い時の逃げ道)
    /// </summary>
    public enum TiltShiftFocus { Depth = 0, Path = 1 }

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

        // ---- P24 (W3) ----
        /// <summary>ピントの帯の形 (既定 = 道に沿った帯。箱庭が無い時は自動で深さの帯)</summary>
        public static TiltShiftFocus Focus = TiltShiftFocus.Path;
        /// <summary>道に沿った帯の手前と奥 (道の座標 s・unit)。座席は s −0.5〜+2.05 (人形の後ろの列)・からくりの匣は −1.9 と +1.7・奥のひな壇の1段目の面は s 5.1〜6.1</summary>
        public static float PathNear = -3f, PathFar = 5.5f;
        /// <summary>手前 (帯より手前) のぼけの上限の倍率 (MaxPx に掛ける。1 = 奥と同じ)</summary>
        public static float NearScale = 1f;

        // ---- P24 (W3b・2周目「本家っぽく」) ----
        /// <summary>帯の外のぼけの伸び方 (既定 = レンズ)。Smooth = 今までの smoothstep (RampNear・RampFar で上限へ)</summary>
        public static TiltShiftCurve Curve = TiltShiftCurve.Lens;
        /// <summary>レンズの強さ (奥・手前)。錯乱円 = 上限 × saturate(強さ × 帯の縁からの深さの差 ÷ 深さ)。1/強さ が「上限に届く 深さの差÷深さ」</summary>
        public static float LensFar = 1.6f, LensNear = 1.4f;
    }

    /// <summary>
    /// 帯の外のぼけの伸び方 (P24 2周目)。
    /// Lens   = 薄いレンズの形: 錯乱円 ∝ (帯の縁からの深さの差) ÷ (深さ)。帯の縁から緩やかに始まり、遠くほど伸びて上限へ近づく (本家の夜の森の測り)。
    /// Smooth = 今までの形: smoothstep(帯の縁からの深さの差 ÷ RampNear・RampFar)。帯の外の数 unit で上限に届き、奥が一面の塗りつぶしになる (W3)
    /// </summary>
    public enum TiltShiftCurve { Smooth = 0, Lens = 1 }
}
