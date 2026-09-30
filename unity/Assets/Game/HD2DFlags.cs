// HD2DFlags.cs — HD-2D 見本 (幕1の1戦闘) の旗 (2026-09-30。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3)。
// 骨組み (P00) の名前と形に、P01 が中身を書いた: 読み込み (起動引数 -hd2d k=v,… と撮影の STATE)・Changed の通知・まとめ役 hd2d=slice・Reset。
// 旗の初期値は「今の見た目」(stage=old)。W3 のレーンはこのファイルを触らない (旗は全部 P01 で先に定義する)。
// 2026-10-01: stage= と hd2d= を書かない普通の起動 (APK・exe) は、舞台を組む時に幕で束を当てる = 幕1 は見本 (hd2d=slice)・幕2/3 は今の舞台 (ApplyActDefault)。
// 撮影の STATE や -hd2d に stage= か hd2d= を書けば今までどおり書いた旗が全部の幕で勝つ。
//
// 読み方 (キーは小文字・値は前後の空白を捨てる。知らないキーは黙って無視 = 撮影の STATE の他のキーと同居できる)
//   stage=old|diorama         herodots=62|48           cam=36|28|22 (画角)     pitch=<度>
//   groundline=<割合|auto>    receive=<数|auto>         keycolor=neutral|warm    herolift=<数|auto>
//   ui=night|paper            ledger=line|feet          artscale=<数|auto>       dof=0|1|urp
//   drift=0|1                 aa=none|msaa|2|4|8 (msaa=<N> も同じ。N≦1 = 無し)   litunits=0|1   charshadow=0|1
//   trunk=mesh|relief         keyflip=auto|off          tier=pc|phone            look=<設計図の名前|auto>
//   det=1                     dumplayout=0|1            uionly=0|1               unitsonly=0|1    perf=<秒>
//   hd2d=slice = stage=diorama・cam=28・litunits=1・charshadow=1・dof=1・aa=msaa・ui=night・herodots=48・trunk=relief をまとめて立てる。
//              同じ STATE に書いた個別のキーのほうが勝つ (hd2d=slice;cam=36 は画角36・hd2d=slice;herodots=62 は主人公62)。
//              herodots=48・trunk=relief は W3 の統合で足した (2026-09-30 ユーザー「本家っぽく」= 主人公は 48 を既定・62 は旗で残す／
//              幹・根・茂みは半立体。CLAUDE.md「見本の途中の方針」)。旗を立てない時の既定 (62・mesh) は今のまま。
// 数値の旗の「負 = 既定」は、各レーンの設計図 (look) か今の値を使う、の意味 (auto と書いても -1 になる)。
// 起動の時 (BeforeSceneLoad) に -hd2d と -state の中の旗を読む (舞台を最初のフレームから旗どおりに組むため)。撮影の StateJump も頭で ApplyState を呼ぶ。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace DeckRogue.Game
{
    /// <summary>舞台の作り: 今の舞台 (old) か、新しい3Dの箱庭 (diorama)</summary>
    public enum HD2DStage { Old, Diorama }
    /// <summary>キャラの固定のキーライトの色: 中立からわずかに寒色 (neutral) か暖色の変種 (warm)</summary>
    public enum HD2DKeyColor { Neutral, Warm }
    /// <summary>敵の帳面の置き場: 全員同じ線 (line) か足元ごと (feet)</summary>
    public enum HD2DLedger { Line, Feet }
    /// <summary>ぼかし: 無し (0)・自作のティルトシフト (1)・URP の Bokeh (urp)</summary>
    public enum HD2DTiltShift { Off, On, Urp }
    /// <summary>画面のギザギザ取り: 無し・MSAA</summary>
    public enum HD2DAa { None, Msaa }
    /// <summary>大樹の幹: 3Dの筒 (mesh)・半立体 (relief)</summary>
    public enum HD2DTrunk { Mesh, Relief }
    /// <summary>左右反転した絵のキーの向き: 表から自動 (auto)・反転しない (off)</summary>
    public enum HD2DKeyFlip { Auto, Off }
    /// <summary>品質の段: PC・スマホ (PC でも tier=phone でスマホの段に切り替える)</summary>
    public enum HD2DTier { Pc, Phone }

    public static class HD2DFlags
    {
        // ---- 見た目の旗 (既定 = 今の見た目) ----
        /// <summary>stage=old|diorama</summary>
        public static HD2DStage StageMode { get; set; } = HD2DStage.Old;
        /// <summary>herodots=62|48 (主人公の背丈のドット数)</summary>
        public static int HeroDots { get; set; } = 62;
        /// <summary>cam=36|28|22 (画角・度)</summary>
        public static float CamFov { get; set; } = 36f;
        /// <summary>pitch= (見下ろしの角度・度)</summary>
        public static float CamPitch { get; set; } = 12f;
        /// <summary>groundline= (足元の線。画面の下から何割。負 = モードの既定)</summary>
        public static float GroundLine { get; set; } = -1f;
        /// <summary>receive= (キャラの板の受光率。負 = 設計図 look の値)</summary>
        public static float Receive { get; set; } = -1f;
        /// <summary>keycolor=neutral|warm</summary>
        public static HD2DKeyColor KeyColor { get; set; } = HD2DKeyColor.Neutral;
        /// <summary>herolift= (主役の持ち上げ。負 = 設計図 look の値)</summary>
        public static float HeroLift { get; set; } = -1f;
        /// <summary>ui=night|paper (舞台の上に常に出る札を夜色にする。false = 紙のまま)</summary>
        public static bool UiNight { get; set; } = false;
        /// <summary>ledger=line|feet</summary>
        public static HD2DLedger Ledger { get; set; } = HD2DLedger.Line;
        /// <summary>artscale= (戦闘の絵の倍率。負 = 今の値)</summary>
        public static float ArtScale { get; set; } = -1f;
        /// <summary>dof=0|1|urp</summary>
        public static HD2DTiltShift TiltShift { get; set; } = HD2DTiltShift.Off;
        /// <summary>drift=1 (待機の漂い。撮影では切る)</summary>
        public static bool Drift { get; set; } = false;
        /// <summary>aa=none|msaa</summary>
        public static HD2DAa Aa { get; set; } = HD2DAa.None;
        /// <summary>MSAA の標本数 (aa=4・msaa=4 で決める。Aa=Msaa の時だけ意味がある。既定 4 = PC)</summary>
        public static int MsaaSamples { get; set; } = 4;
        /// <summary>litunits=1 (キャラの板が光を受ける StageUnitLit)</summary>
        public static bool LitUnits { get; set; } = false;
        /// <summary>charshadow=1 (キャラの板が舞台の灯から影を落とす)</summary>
        public static bool CharShadow { get; set; } = false;
        /// <summary>trunk=mesh|relief</summary>
        public static HD2DTrunk Trunk { get; set; } = HD2DTrunk.Mesh;
        /// <summary>keyflip=auto|off</summary>
        public static HD2DKeyFlip KeyFlip { get; set; } = HD2DKeyFlip.Auto;
        /// <summary>
        /// tier=pc|phone。PC で phone にすると品質レベル2 (Android の既定・スマホの URP) へ切り替える (QualitySettings.SetQualityLevel(2))。
        /// pc に戻すと切り替える前のレベルへ戻す。スマホの実機では品質は触らない (もともとレベル2)。既定は実機なら phone・それ以外は pc
        /// </summary>
        public static HD2DTier Tier
        {
            get { return _tier; }
            set { if (_tier == value) return; _tier = value; SyncTierQuality(); }
        }
        static HD2DTier _tier = HD2DTier.Pc;
        /// <summary>look= (幕の設計図 look_act&lt;N&gt; の上に重ねる変種の名前。「+」で複数。null = 変種なし。W3 P22 で「丸ごとの差し替え」から「重ねる変種」へ)</summary>
        public static string Look { get; set; } = null;

        // ---- 撮影と計測の旗 ----
        /// <summary>
        /// det=1 (決定的な撮影)。実体は Autopilot.Det (起動引数 -det・STATE の det=1)。true にすると Autopilot.EnableDet を呼ぶ。
        /// 一度立てたら起動の間は消えない (時間と乱数の刻みはプロセス全体の設定なので Reset でも戻さない)
        /// </summary>
        public static bool Det
        {
            get { return Autopilot.Det; }
            set { if (value) Autopilot.EnableDet(); }
        }
        /// <summary>dumplayout=1 (撮った PNG と同じ名前の .layout.json に矩形を書く)</summary>
        public static bool DumpLayout { get; set; } = false;
        /// <summary>uionly=1 (UI だけ。舞台はマゼンタ)</summary>
        public static bool UiOnly { get; set; } = false;
        /// <summary>unitsonly=1 (キャラの板だけ)</summary>
        public static bool UnitsOnly { get; set; } = false;
        /// <summary>perf=&lt;秒&gt; (0 = 計測しない)</summary>
        public static float Perf { get; set; } = 0f;

        /// <summary>
        /// 旗の値が変わった (ApplyState・Reset・ApplyLaunchArgs の後に1回。値が1つも変わらなければ投げない)。
        /// 舞台・カメラ・札が購読して組み直す。プロパティへ直に代入した時は投げない (代入した側が組み直す)
        /// </summary>
        public static event Action Changed;

        /// <summary>dumplayout の記録を足す口 (名前 → JSON にできる値を返す関数)。他のレーンが足す (layout.json の "extra" に名前ごとに入る)</summary>
        public static readonly Dictionary<string, Func<object>> LayoutDumpers = new Dictionary<string, Func<object>>();

        /// <summary>hd2d=slice が立てる旗 (計画 §3。個別のキーのほうが勝つ)</summary>
        static readonly KeyValuePair<string, string>[] SliceBundle =
        {
            new KeyValuePair<string, string>("stage", "diorama"),
            new KeyValuePair<string, string>("cam", "28"),
            new KeyValuePair<string, string>("litunits", "1"),
            new KeyValuePair<string, string>("charshadow", "1"),
            new KeyValuePair<string, string>("dof", "1"),
            new KeyValuePair<string, string>("aa", "msaa"),
            new KeyValuePair<string, string>("ui", "night"),
            new KeyValuePair<string, string>("herodots", "48"),    // W3 統合: 主人公は 48 を既定 (62 は herodots=62 で)
            new KeyValuePair<string, string>("trunk", "relief"),   // W3 統合: 幹と根は半立体 (3Dの筒は trunk=mesh で)
        };

        /// <summary>今の舞台 (stage=old) の時の束のキーの値 (SliceBundle と同じキー・同じ順。Reset の既定と同じ値)</summary>
        static readonly KeyValuePair<string, string>[] OldBundle =
        {
            new KeyValuePair<string, string>("stage", "old"),
            new KeyValuePair<string, string>("cam", "36"),
            new KeyValuePair<string, string>("litunits", "0"),
            new KeyValuePair<string, string>("charshadow", "0"),
            new KeyValuePair<string, string>("dof", "0"),
            new KeyValuePair<string, string>("aa", "none"),
            new KeyValuePair<string, string>("ui", "paper"),
            new KeyValuePair<string, string>("herodots", "62"),
            new KeyValuePair<string, string>("trunk", "mesh"),
        };

        // ---- 幕ごとの既定 (2026-10-01。ユーザーが遊ぶ APK・exe で見本の舞台が出るように) ----
        // 旗 stage= と hd2d= を起動引数 (-hd2d) にも撮影の STATE にも書かない時 (普通の起動) は、舞台を組む時 (Stage.Paint) に幕で束を当てる:
        // 幕1 = 見本 (hd2d=slice の束 = 箱庭・画角28・キャラの光と影・ぼかし・AA・夜色の札・主人公48・幹は半立体)、
        // 幕2・3 = 今の舞台 (束のキーを今の既定へ = stage=old・画角36・紙の札・主人公62 …。箱庭は幕1にしか無いので、見本のカメラ・配置・キャラの光を今の舞台に当てない)。
        // 束のキーを個別に書いた時 (ui=paper・cam=36・herodots=62 など) はその値が幕の既定より勝つ。
        // stage= か hd2d= を書いた時は幕で切り替えない (今までどおり、書いた旗が全部の幕で効く = 見本の撮影の再現性を変えない)
        static bool _stageExplicit;
        static readonly Dictionary<string, string> _explicitBundle = new Dictionary<string, string>();   // 束のキー (と msaa) のうち起動引数か STATE で書いた物

        /// <summary>幕ごとの既定が効いているか (stage= と hd2d= を起動引数にも STATE にも書いていない)</summary>
        public static bool ByAct { get { return !_stageExplicit; } }

        /// <summary>
        /// 幕 act の既定の束を当てる (Stage.Paint が舞台を組む前に呼ぶ)。ByAct でなければ何もしない。
        /// 幕1 は SliceBundle、幕2・3 は OldBundle を当て、その上に書いた束のキー (と msaa) を当て直す。値が変わったら Changed を1回
        /// </summary>
        public static void ApplyActDefault(int act)
        {
            if (_stageExplicit) return;
            string before = Describe();
            var bundle = act == 1 ? SliceBundle : OldBundle;
            string v;
            foreach (var p in bundle) SetKey(p.Key, _explicitBundle.TryGetValue(p.Key, out v) ? v : p.Value);
            if (_explicitBundle.TryGetValue("msaa", out v)) SetKey("msaa", v);
            if (Describe() != before) Debug.Log("[HD2DFlags] 幕" + act + " の既定 (" + (act == 1 ? "見本" : "今の舞台") + ") → " + Describe());
            RaiseIfChanged(before);
        }

        static bool IsBundleKey(string key)
        {
            foreach (var p in SliceBundle) if (p.Key == key) return true;
            return false;
        }

        /// <summary>
        /// STATE のキー (stage・herodots・cam・…・hd2d=slice) から旗を読む。書いてあるキーだけを変える (書いていない旗は今の値のまま)。
        /// 知らないキー (phase・enemy など撮影の他のキー) は黙って無視する。値が読めない時は警告して今の値のまま。
        /// hd2d=slice を先に当ててから個別のキーを当てる (個別のキーが勝つ)。値が変わったら最後に Changed を1回。
        /// 書いたキーは覚える (stage・hd2d なら幕ごとの既定を止める・束のキーなら幕の既定の上に当て直す。Reset で忘れる)
        /// </summary>
        public static void ApplyState(IDictionary<string, string> kv)
        {
            if (kv == null || kv.Count == 0) return;
            var norm = new Dictionary<string, string>();
            foreach (var p in kv)
            {
                if (string.IsNullOrEmpty(p.Key)) continue;
                norm[p.Key.Trim().ToLowerInvariant()] = (p.Value ?? "").Trim();
            }
            foreach (var p in norm)
            {
                if (p.Value.Length == 0) continue;
                if (p.Key == "hd2d" || p.Key == "stage") _stageExplicit = true;
                else if (p.Key == "msaa" || IsBundleKey(p.Key)) _explicitBundle[p.Key] = p.Value;
            }
            string before = Describe();
            string bundle;
            if (norm.TryGetValue("hd2d", out bundle)) ApplyBundle(bundle);
            foreach (var p in norm) if (p.Key != "hd2d") SetKey(p.Key, p.Value);
            RaiseIfChanged(before);
        }

        /// <summary>
        /// 全部を既定 (今の見た目) に戻す。起動引数の -hd2d も捨てる (戻したい時は続けて ApplyLaunchArgs)。
        /// det だけは戻さない (プロセス全体の時間と乱数の設定)。値が変わったら Changed を1回
        /// </summary>
        public static void Reset()
        {
            string before = Describe();
            _stageExplicit = false;        // 書いた旗も忘れる (幕ごとの既定に戻る。次の Stage.Paint が幕の束を当てる)
            _explicitBundle.Clear();
            StageMode = HD2DStage.Old;
            HeroDots = 62;
            CamFov = 36f;
            CamPitch = 12f;
            GroundLine = -1f;
            Receive = -1f;
            KeyColor = HD2DKeyColor.Neutral;
            HeroLift = -1f;
            UiNight = false;
            Ledger = HD2DLedger.Line;
            ArtScale = -1f;
            TiltShift = HD2DTiltShift.Off;
            Drift = false;
            Aa = HD2DAa.None;
            MsaaSamples = 4;
            LitUnits = false;
            CharShadow = false;
            Trunk = HD2DTrunk.Mesh;
            KeyFlip = HD2DKeyFlip.Auto;
            Tier = _defaultTier;
            Look = null;
            DumpLayout = false;
            UiOnly = false;
            UnitsOnly = false;
            Perf = 0f;
            RaiseIfChanged(before);
        }

        /// <summary>起動引数 -hd2d k=v,k=v (「;」で区切ってもよい) を当てる。Reset の後にもう一度呼べば起動の時の旗へ戻る</summary>
        public static void ApplyLaunchArgs()
        {
            var raw = Autopilot.Arg("-hd2d");
            if (string.IsNullOrEmpty(raw)) return;
            ApplyState(ParseSpec(raw, ',', ';'));
        }

        /// <summary>「k=v」を sep の文字で区切った文字列を辞書にする (キーは小文字)。StateJump の STATE と同じ読み方</summary>
        public static Dictionary<string, string> ParseSpec(string spec, params char[] sep)
        {
            var kv = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(spec)) return kv;
            foreach (var part in spec.Split(sep != null && sep.Length > 0 ? sep : new[] { ';' }))
            {
                int eq = part.IndexOf('=');
                if (eq > 0) kv[part.Substring(0, eq).Trim().ToLowerInvariant()] = part.Substring(eq + 1).Trim();
            }
            return kv;
        }

        /// <summary>今の旗を1行で (ログ用。キーの順は固定)</summary>
        public static string Describe()
        {
            var sb = new StringBuilder();
            foreach (var p in Snapshot())
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(p.Key).Append('=').Append(FormatValue(p.Value));
            }
            return sb.ToString();
        }

        /// <summary>今の旗 (layout.json の "flags"。キーは STATE の書き方・値は文字列か数か真偽)</summary>
        public static List<KeyValuePair<string, object>> Snapshot()
        {
            var l = new List<KeyValuePair<string, object>>();
            Action<string, object> add = (k, v) => l.Add(new KeyValuePair<string, object>(k, v));
            add("stage", StageMode == HD2DStage.Diorama ? "diorama" : "old");
            add("herodots", HeroDots);
            add("cam", CamFov);
            add("pitch", CamPitch);
            add("groundline", GroundLine);
            add("receive", Receive);
            add("keycolor", KeyColor == HD2DKeyColor.Warm ? "warm" : "neutral");
            add("herolift", HeroLift);
            add("ui", UiNight ? "night" : "paper");
            add("ledger", Ledger == HD2DLedger.Feet ? "feet" : "line");
            add("artscale", ArtScale);
            add("dof", TiltShift == HD2DTiltShift.On ? "1" : TiltShift == HD2DTiltShift.Urp ? "urp" : "0");
            add("drift", Drift);
            add("aa", Aa == HD2DAa.Msaa ? "msaa" : "none");
            add("msaa", MsaaSamples);
            add("litunits", LitUnits);
            add("charshadow", CharShadow);
            add("trunk", Trunk == HD2DTrunk.Relief ? "relief" : "mesh");
            add("keyflip", KeyFlip == HD2DKeyFlip.Off ? "off" : "auto");
            add("tier", Tier == HD2DTier.Phone ? "phone" : "pc");
            add("look", Look ?? "");
            add("det", Det);
            add("dumplayout", DumpLayout);
            add("uionly", UiOnly);
            add("unitsonly", UnitsOnly);
            add("perf", Perf);
            return l;
        }

        // ---------------------------------------------------------------- 中身

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static HD2DTier _defaultTier = HD2DTier.Pc;
        static int _pcQuality = -1;   // tier=phone へ切り替える前の品質レベル (-1 = 切り替えていない)
        // PC の品質レベル = QualitySettings.asset の m_PerPlatformDefaultQuality の Standalone (5「Ultra」= URP-Default)。
        // 2026-09-30 W2 の統合で見つけた不具合: Windows のプレイヤーは終わる時に今の品質レベルをレジストリ (HKCU\Software\DeckRogue\DeckRogue の
        // UnityGraphicsQuality_h…) に書き、次の起動はその値で始まる。tier=phone の撮影 (品質レベル2 = URP-Phone) の後に起動した PC の撮影が
        // 全部スマホの URP (MSAA なし・影 512・SSAO なし) で撮られていた。→ 起動の頭で PC なら必ずこのレベルへ戻し、
        // tier=phone で切り替えたら終わる時に元のレベルへ戻す (書かれる値を PC のレベルにする)。ゲームは品質レベルを変える口を持たないので、
        // PC でレベル2から始まるのは撮影の残りだけ
        const int PcQualityLevel = 5;
        static bool _quitHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            // スマホの実機は phone が既定 (品質はもともとレベル2なので触らない)
            if (Application.isMobilePlatform) { _defaultTier = HD2DTier.Phone; _tier = HD2DTier.Phone; }
            else HealPcQuality();
            // 起動引数の -hd2d、続けて撮影の -state の中の旗 (撮影の STATE のほうが勝つ)。StateJump も頭でもう一度当てる (変わらなければ何も起きない)
            ApplyLaunchArgs();
            var st = Autopilot.Arg("-state");
            if (!string.IsNullOrEmpty(st)) ApplyState(ParseSpec(st, ';'));
            if (StageMode != HD2DStage.Old || Tier != _defaultTier || DumpLayout || UiOnly || UnitsOnly || Perf > 0f || !string.IsNullOrEmpty(Autopilot.Arg("-hd2d")))
                Debug.Log("[HD2DFlags] " + Describe());
        }

        static void ApplyBundle(string v)
        {
            string s = (v ?? "").Trim().ToLowerInvariant();
            if (s == "slice") { foreach (var p in SliceBundle) SetKey(p.Key, p.Value); return; }
            if (s == "" || s == "0" || s == "off" || s == "old") return;   // 何も立てない
            Warn("hd2d", v);
        }

        static void RaiseIfChanged(string before)
        {
            if (Describe() == before) return;
            var h = Changed;
            if (h == null) return;
            foreach (Action a in h.GetInvocationList())
            {
                try { a(); }
                catch (Exception e) { Debug.LogException(e); }   // 1つの購読の失敗で他の組み直しを止めない
            }
        }

        /// <summary>1つのキーを当てる。知らないキーは false (無視)</summary>
        static bool SetKey(string key, string v)
        {
            float f; bool b; int n;
            switch (key)
            {
                case "stage":
                    if (Is(v, "old")) StageMode = HD2DStage.Old;
                    else if (Is(v, "diorama")) StageMode = HD2DStage.Diorama;
                    else Warn(key, v);
                    return true;
                case "herodots":
                    if (int.TryParse(v, NumberStyles.Integer, Inv, out n) && n > 0) { HeroDots = n; if (n != 62 && n != 48) Debug.LogWarning("[HD2DFlags] herodots=" + n + " (見本は 62 か 48)"); }
                    else Warn(key, v);
                    return true;
                case "cam": if (TryFloat(v, out f) && f > 1f && f < 170f) CamFov = f; else Warn(key, v); return true;
                case "pitch": if (TryFloat(v, out f) && f > -89f && f < 89f) CamPitch = f; else Warn(key, v); return true;
                case "groundline": if (TryAuto(v, out f)) GroundLine = f; else Warn(key, v); return true;
                case "receive": if (TryAuto(v, out f)) Receive = f; else Warn(key, v); return true;
                case "herolift": if (TryAuto(v, out f)) HeroLift = f; else Warn(key, v); return true;
                case "artscale": if (TryAuto(v, out f)) ArtScale = f; else Warn(key, v); return true;
                case "keycolor":
                    if (Is(v, "neutral")) KeyColor = HD2DKeyColor.Neutral;
                    else if (Is(v, "warm")) KeyColor = HD2DKeyColor.Warm;
                    else Warn(key, v);
                    return true;
                case "ui":
                    if (Is(v, "night")) UiNight = true;
                    else if (Is(v, "paper")) UiNight = false;
                    else Warn(key, v);
                    return true;
                case "ledger":
                    if (Is(v, "line")) Ledger = HD2DLedger.Line;
                    else if (Is(v, "feet")) Ledger = HD2DLedger.Feet;
                    else Warn(key, v);
                    return true;
                case "dof":
                    if (Is(v, "urp")) TiltShift = HD2DTiltShift.Urp;
                    else if (TryBool(v, out b)) TiltShift = b ? HD2DTiltShift.On : HD2DTiltShift.Off;
                    else Warn(key, v);
                    return true;
                case "aa":
                    if (Is(v, "msaa")) Aa = HD2DAa.Msaa;
                    else if (Is(v, "none") || Is(v, "off")) Aa = HD2DAa.None;
                    else if (TrySamples(v, out n)) SetMsaa(n);
                    else Warn(key, v);
                    return true;
                case "msaa":
                    if (TrySamples(v, out n)) SetMsaa(n);
                    else if (Is(v, "off") || Is(v, "none")) Aa = HD2DAa.None;
                    else Warn(key, v);
                    return true;
                case "trunk":
                    if (Is(v, "mesh")) Trunk = HD2DTrunk.Mesh;
                    else if (Is(v, "relief")) Trunk = HD2DTrunk.Relief;
                    else Warn(key, v);
                    return true;
                case "keyflip":
                    if (Is(v, "auto")) KeyFlip = HD2DKeyFlip.Auto;
                    else if (Is(v, "off")) KeyFlip = HD2DKeyFlip.Off;
                    else Warn(key, v);
                    return true;
                case "tier":
                    if (Is(v, "pc")) Tier = HD2DTier.Pc;
                    else if (Is(v, "phone")) Tier = HD2DTier.Phone;
                    else Warn(key, v);
                    return true;
                case "look":
                    Look = string.IsNullOrEmpty(v) || Is(v, "auto") || Is(v, "default") ? null : v;
                    return true;
                case "drift": if (TryBool(v, out b)) Drift = b; else Warn(key, v); return true;
                case "litunits": if (TryBool(v, out b)) LitUnits = b; else Warn(key, v); return true;
                case "charshadow": if (TryBool(v, out b)) CharShadow = b; else Warn(key, v); return true;
                case "det":
                    if (TryBool(v, out b)) { if (b) Det = true; }   // det=0 は何もしない (一度立てた時間と乱数の固定は起動の間は外さない)
                    else Warn(key, v);
                    return true;
                case "dumplayout": if (TryBool(v, out b)) DumpLayout = b; else Warn(key, v); return true;
                case "uionly": if (TryBool(v, out b)) UiOnly = b; else Warn(key, v); return true;
                case "unitsonly": if (TryBool(v, out b)) UnitsOnly = b; else Warn(key, v); return true;
                case "perf": if (TryFloat(v, out f) && f >= 0f) Perf = f; else Warn(key, v); return true;
                default: return false;
            }
        }

        static void SetMsaa(int n)
        {
            if (n <= 1) { Aa = HD2DAa.None; return; }
            Aa = HD2DAa.Msaa; MsaaSamples = n;
        }

        static bool Is(string v, string word) { return string.Equals((v ?? "").Trim(), word, StringComparison.OrdinalIgnoreCase); }

        static bool TryFloat(string v, out float f) { return float.TryParse((v ?? "").Trim(), NumberStyles.Float, Inv, out f) && !float.IsNaN(f) && !float.IsInfinity(f); }

        /// <summary>数か auto/default (= -1 = 設計図か今の値)</summary>
        static bool TryAuto(string v, out float f)
        {
            if (Is(v, "auto") || Is(v, "default")) { f = -1f; return true; }
            return TryFloat(v, out f);
        }

        static bool TryBool(string v, out bool b)
        {
            string s = (v ?? "").Trim().ToLowerInvariant();
            switch (s)
            {
                case "1": case "true": case "on": case "yes": b = true; return true;
                case "0": case "false": case "off": case "no": b = false; return true;
                default: b = false; return false;
            }
        }

        /// <summary>MSAA の標本数 (1・2・4・8。「4x」も読む)</summary>
        static bool TrySamples(string v, out int n)
        {
            string s = (v ?? "").Trim().ToLowerInvariant();
            if (s.EndsWith("x")) s = s.Substring(0, s.Length - 1);
            if (int.TryParse(s, NumberStyles.Integer, Inv, out n) && (n == 0 || n == 1 || n == 2 || n == 4 || n == 8)) return true;
            n = 0; return false;
        }

        static void Warn(string key, string v) { Debug.LogWarning("[HD2DFlags] 読めない値: " + key + "=" + v + " (今の値のまま)"); }

        static string FormatValue(object v)
        {
            if (v is float) return ((float)v).ToString("0.###", Inv);
            if (v is bool) return (bool)v ? "1" : "0";
            if (v is string && (string)v == "") return "-";
            return Convert.ToString(v, Inv);
        }

        /// <summary>PC の起動の頭: 品質レベルがスマホのレベル (2) なら PC のレベルへ戻す (前の撮影の tier=phone がレジストリに残した値。上の PcQualityLevel の注記)</summary>
        static void HealPcQuality()
        {
            try
            {
                int n = QualitySettings.names.Length;
                int lv = QualitySettings.GetQualityLevel();
                if (n > PcQualityLevel && lv == 2 && lv != PcQualityLevel)
                {
                    QualitySettings.SetQualityLevel(PcQualityLevel, true);
                    Debug.LogWarning("[HD2DFlags] 品質レベルが 2 (スマホ) で始まった = 前の tier=phone の撮影の残り → " + PcQualityLevel + " (" + QualitySettings.names[PcQualityLevel] + ") へ戻した");
                }
            }
            catch (Exception e) { Debug.LogWarning("[HD2DFlags] 品質レベルを確かめられない: " + e.Message); }
        }

        /// <summary>tier=phone のまま終わる時: 品質レベルを切り替える前へ戻す (プレイヤーが終わる時にレジストリへ書く値を PC のレベルにする)</summary>
        static void RestorePcQualityOnQuit()
        {
            if (Application.isMobilePlatform || _pcQuality < 0) return;
            try { QualitySettings.SetQualityLevel(_pcQuality, false); } catch (Exception) { }
        }

        /// <summary>tier の品質レベル: PC で phone にしたらレベル2へ、pc に戻したら元のレベルへ (実機では触らない)</summary>
        static void SyncTierQuality()
        {
            if (Application.isMobilePlatform) return;
            try
            {
                if (_tier == HD2DTier.Phone)
                {
                    if (QualitySettings.names.Length <= 2) { Debug.LogWarning("[HD2DFlags] tier=phone: 品質レベル2が無い (レベル数 " + QualitySettings.names.Length + ")。品質はそのまま"); return; }
                    if (_pcQuality < 0) _pcQuality = QualitySettings.GetQualityLevel();
                    if (QualitySettings.GetQualityLevel() != 2) QualitySettings.SetQualityLevel(2, true);
                    if (!_quitHooked) { _quitHooked = true; Application.quitting += RestorePcQualityOnQuit; }
                    Debug.Log("[HD2DFlags] tier=phone: 品質レベル " + _pcQuality + " → 2 (" + QualitySettings.names[2] + ")");
                }
                else if (_pcQuality >= 0)
                {
                    int back = _pcQuality; _pcQuality = -1;
                    if (QualitySettings.GetQualityLevel() != back) QualitySettings.SetQualityLevel(back, true);
                    Debug.Log("[HD2DFlags] tier=pc: 品質レベルを " + back + " へ戻した");
                }
            }
            catch (Exception e) { Debug.LogWarning("[HD2DFlags] 品質レベルを切り替えられない: " + e.Message); }
        }
    }

    /// <summary>
    /// 舞台のレイヤー (HD-2D 見本 §3)。コードでは番号の定数で持つ (名前は HD2DSetup の prep で付けるだけ。prep の前は LayerMask.NameToLayer が -1 になるため)
    /// </summary>
    public static class HD2DLayers
    {
        /// <summary>キャラの板 (敵・リーダー・人形)</summary>
        public const int StageUnit = 8;
        /// <summary>舞台の部品 (地形・大物・半立体)</summary>
        public const int StageSet = 9;
        /// <summary>技の光・粒</summary>
        public const int StageFx = 10;
        /// <summary>Rendering Layers の bit1 = Characters (キャラの影はスポットからだけ)</summary>
        public const uint RenderingCharacters = 1u << 1;
        /// <summary>Rendering Layers の bit2 = Environment (月の影は地形と大物だけ)</summary>
        public const uint RenderingEnvironment = 1u << 2;
    }
}
