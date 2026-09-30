// PerfProbe.cs — 描画の重さの記録 (2026-09-30 HD-2D 見本 P08。計画 docs/design/hd2d-slice-plan-2026-09-30.md §1-2 の5・P31)。
// 起動引数 -perfprobe [秒] か、撮影の旗 perf=<秒> (HD2DFlags.Perf) の時だけ動く。どちらも無ければ何も作らない (画は1画素も変わらない)。
//
// 何を記録するか: 毎フレーム FrameTimingManager.CaptureFrameTimings を呼び、1秒ごとに CPU・GPU・フレームの時間 (ms) の p50・p95・p99 と
// fps、取れれば ProfilerRecorder の Draw Calls・SetPass・三角形の数を CSV の1行にする (取れない値は空欄)。区間の終わりに要約の JSON。
//   GPU の時間は Player 設定の Frame Timing Stats が有効な時だけ取れる (計測用のビルド = HD2DSetup の BuildPerf。無ければ空欄)。
//   Draw Calls などは Development ビルドでしか取れない数がある (空欄なら取れなかった)。
// 区間: Autopilot の perf の戦闘 (Autopilot.PerfRunning が true の間。perf=<秒> で撮影の STATE から始まる) を測る。
//   perf の戦闘が無く -perfprobe <秒> だけの時は、起動から 5 秒待って <秒> の間を測る。-perfprobe の秒は perf の戦闘の上限にもなる。
// 置き場: -shots の場所 (撮影の回収 = unity-win.sh が *.csv・*.json を拾う)。無ければ persistentDataPath/perf/ (スマホは adb pull)。
//   perf-<日時>.csv・perf-<日時>.summary.json。1秒ごとに書き足して flush する (途中で落ちても残る)。
// 起動引数は Autopilot.Arg で読む = 計測用の APK (applicationId が .perf で終わる) では persistentDataPath/perf-args.txt の中身も入る (P01 と同じ書き方)。
// 計測用の APK では -perfprobe が無くても perf=<秒> の区間を測る (引数ファイルの最後の -perfprobe は Arg で見えないため)。
//
// W4 P31 (スマホの段と計測用 APK。2026-10-01) で足したもの。手順書は docs/design/hd2d-slice/perf-runbook.md:
//   ・画面を消さない: 準備の時に Screen.sleepTimeout = NeverSleep (15 分の計測の間に端末の画面が消えると裏へ回って区間が切れる)
//   ・CSV の列の後ろに thermal_status (Android の PowerManager.getCurrentThermalStatus・0 なし〜6 停止)・thermal_headroom (getThermalHeadroom(0)・
//     1.0 で抑え始める。2 秒に1回)・battery_level (0〜1)・battery_status (Charging など)。Android 以外と読めない値は空欄
//   ・要約に 1秒ごとの fps の下側 (fps_1s)・終わりの 5 分 (tail)・端末の温度の経過 (thermal)・画面の数え (refresh_hz・tier・look・箱庭の部品の数)・
//     保留の門 (gate = 計画 §1-2 の5「15分で30fps・GPU の p95 25ms 未満」。見本の判定には使わない。数字を並べるだけ)
//   ・スマホのフレームレート (下の PhoneFramePacing): 実機では Application.targetFrameRate = 30 を明示する (計画 §2-7・P31 手順1)。
//     起動引数 -targetfps <n> で上書き (計測用の APK の引数ファイルでも。-1 = 端末の既定)。PC は触らない (vSync のまま = 撮影の速さも変えない)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace DeckRogue.Game
{
    public class PerfProbe : MonoBehaviour
    {
        static PerfProbe _inst;
        static bool _booted;
        static bool _argProbe;       // -perfprobe がある (か計測用の APK)
        static float _argSecs;       // -perfprobe の後ろの秒 (無ければ 0)

        /// <summary>記録の置き物がある (= -perfprobe か perf=)。PerfProbe.Recording は区間の中</summary>
        public static bool Armed => _inst != null;
        /// <summary>いま区間を測っている</summary>
        public static bool Recording => _inst != null && _inst._rec;
        /// <summary>最後に書いた CSV と要約 (無ければ null)</summary>
        public static string LastCsv { get; private set; }
        public static string LastSummary { get; private set; }

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        const float WarmupSecs = 5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_booted) return;
            _booted = true;
            try
            {
                bool perfBuild = (Application.identifier ?? "").EndsWith(".perf", StringComparison.Ordinal);
                bool inArgs = Array.IndexOf(Environment.GetCommandLineArgs(), "-perfprobe") >= 0;
                string v = Autopilot.Arg("-perfprobe");   // 引数ファイルの中の「-perfprobe 900」もここで見える
                float secs;
                if (v != null && float.TryParse(v, NumberStyles.Float, Inv, out secs) && secs > 0f) _argSecs = secs;
                _argProbe = inArgs || perfBuild || v != null;
            }
            catch (Exception e) { Debug.LogWarning("[PerfProbe] 起動引数を読めない: " + e.Message); }
            HD2DFlags.Changed += OnFlags;
            if (_argProbe || HD2DFlags.Perf > 0f) Ensure();
        }

        static void OnFlags()
        {
            if (HD2DFlags.Perf > 0f) Ensure();
        }

        static void Ensure()
        {
            if (_inst != null) return;
            var go = new GameObject("PerfProbe");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<PerfProbe>();
            // 15 分の計測の間に端末の画面が消えると裏へ回り (OnApplicationPause)、区間がそこで切れる。測る間は消さない (P31)。
            // 記録の置き物は -perfprobe・perf=・計測用の APK の時だけ作るので、普段の起動の画面の消え方は変えない
            try { Screen.sleepTimeout = SleepTimeout.NeverSleep; } catch (Exception) { }
            Debug.Log("[PerfProbe] 準備 (-perfprobe=" + _argProbe + " secs=" + _argSecs.ToString("0.#", Inv) + " perf=" + HD2DFlags.Perf.ToString("0.#", Inv)
                + " targetFrameRate=" + Application.targetFrameRate.ToString(Inv) + " vsync=" + QualitySettings.vSyncCount.ToString(Inv) + " tier=" + HD2DFlags.Tier + " 画面は消さない)");
        }

        // ---- 区間 ----
        bool _rec, _timedDone;
        int _session;
        float _armT, _t0, _secT0;
        int _secFrames;
        readonly List<double> _cpuS = new List<double>(), _gpuS = new List<double>(), _frmS = new List<double>();   // この1秒
        readonly List<double> _cpuA = new List<double>(), _gpuA = new List<double>(), _frmA = new List<double>();   // 区間全体
        // P31: 終わりの 5 分 (tail) を出すための、区間の頭からの時刻つきの記録 (取れない値は NaN)
        struct FrameRec { public float T; public double Cpu, Gpu, Frame; }
        readonly List<FrameRec> _recA = new List<FrameRec>();
        readonly List<double> _fps1s = new List<double>();   // 1秒ごとの fps (CSV の行と同じ)
        // P31: 端末の温度の経過 (1秒ごとの行と同じ時刻)。取れない値は -1 / NaN
        struct DevRec { public float T; public int Thermal; public float Headroom, Battery; }
        readonly List<DevRec> _devA = new List<DevRec>();
        int _thermStatus = -1;
        float _thermHeadroom = float.NaN;
        readonly FrameTiming[] _ft = new FrameTiming[1];
        StreamWriter _csv;
        string _stamp, _dir;
        int _rows;
        ProfilerRecorder _draw, _setpass, _tris;

        void Awake()
        {
            _armT = Time.realtimeSinceStartup;
        }

        void Update()
        {
            try { FrameTimingManager.CaptureFrameTimings(); } catch (Exception) { }
            float now = Time.realtimeSinceStartup;
            if (!_rec)
            {
                if (Autopilot.PerfRunning) Begin("perf");
                else if (_argSecs > 0f && !_timedDone && HD2DFlags.Perf <= 0f && now - _armT >= WarmupSecs) Begin("timed");
                return;
            }
            Sample();
            if (now - _secT0 >= 1f) Flush(now);
            bool stop = false;
            if (_mode == "perf" && !Autopilot.PerfRunning) stop = true;
            if (_argSecs > 0f && now - _t0 >= _argSecs) stop = true;
            if (stop) End(now);
        }

        string _mode;

        void Begin(string mode)
        {
            _mode = mode;
            _rec = true;
            _t0 = _secT0 = Time.realtimeSinceStartup;
            _secFrames = 0; _rows = 0;
            _cpuS.Clear(); _gpuS.Clear(); _frmS.Clear(); _cpuA.Clear(); _gpuA.Clear(); _frmA.Clear();
            _recA.Clear(); _fps1s.Clear(); _devA.Clear();
            _session++;
            _stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", Inv) + (_session > 1 ? "-" + _session : "");
            _dir = OutDir();
            try
            {
                Directory.CreateDirectory(_dir);
                LastCsv = Path.Combine(_dir, "perf-" + _stamp + ".csv");
                _csv = new StreamWriter(LastCsv, false, new UTF8Encoding(false));
                // 列は後ろに足すだけ (P31: thermal_status 以降。読む道具は列の名前で読む)
                _csv.WriteLine("t_s,frames,fps,cpu_p50_ms,cpu_p95_ms,cpu_p99_ms,gpu_p50_ms,gpu_p95_ms,gpu_p99_ms,frame_p50_ms,frame_p95_ms,frame_p99_ms,drawcalls,setpass,triangles,plays,turns,rejumps,thermal_status,thermal_headroom,battery_level,battery_status");
                _csv.Flush();
            }
            catch (Exception e) { Debug.LogWarning("[PerfProbe] CSV を開けない " + _dir + " " + e.Message); _csv = null; }
            _draw = StartRecorder("Draw Calls Count");
            _setpass = StartRecorder("SetPass Calls Count");
            _tris = StartRecorder("Triangles Count");
            Debug.Log("[PerfProbe] start mode=" + mode + " file=" + LastCsv + " frameTiming=" + FrameTimingOn() + " flags=" + HD2DFlags.Describe());
        }

        static ProfilerRecorder StartRecorder(string stat)
        {
            try { return ProfilerRecorder.StartNew(ProfilerCategory.Render, stat); }
            catch (Exception) { return default(ProfilerRecorder); }
        }

        static bool FrameTimingOn()
        {
            try { return FrameTimingManager.IsFeatureEnabled(); } catch (Exception) { return false; }
        }

        void Sample()
        {
            _secFrames++;
            double frame = Time.unscaledDeltaTime * 1000.0;
            _frmS.Add(frame); _frmA.Add(frame);
            uint n = 0;
            try { n = FrameTimingManager.GetLatestTimings(1, _ft); } catch (Exception) { n = 0; }
            var rec = new FrameRec { T = Time.realtimeSinceStartup - _t0, Cpu = double.NaN, Gpu = double.NaN, Frame = frame };
            if (n > 0)
            {
                double cpu = _ft[0].cpuFrameTime, gpu = _ft[0].gpuFrameTime;
                if (cpu > 0.0) { _cpuS.Add(cpu); _cpuA.Add(cpu); rec.Cpu = cpu; }
                if (gpu > 0.0) { _gpuS.Add(gpu); _gpuA.Add(gpu); rec.Gpu = gpu; }
            }
            _recA.Add(rec);
        }

        void Flush(float now)
        {
            float dt = now - _secT0;
            double fps = dt > 0f ? _secFrames / dt : 0.0;
            var sb = new StringBuilder();
            sb.Append(F(now - _t0, 2)).Append(',').Append(_secFrames).Append(',').Append(F(fps, 1)).Append(',');
            sb.Append(P(_cpuS, 50)).Append(',').Append(P(_cpuS, 95)).Append(',').Append(P(_cpuS, 99)).Append(',');
            sb.Append(P(_gpuS, 50)).Append(',').Append(P(_gpuS, 95)).Append(',').Append(P(_gpuS, 99)).Append(',');
            sb.Append(P(_frmS, 50)).Append(',').Append(P(_frmS, 95)).Append(',').Append(P(_frmS, 99)).Append(',');
            sb.Append(Rec(_draw)).Append(',').Append(Rec(_setpass)).Append(',').Append(Rec(_tris)).Append(',');
            sb.Append(Autopilot.PerfPlays).Append(',').Append(Autopilot.PerfTurns).Append(',').Append(Autopilot.PerfRejumps);
            // P31: 端末の温度と電池 (Android 以外・読めない値は空欄)
            SampleDevice(now);
            float batt = -1f;
            try { batt = SystemInfo.batteryLevel; } catch (Exception) { }
            string battStatus = "";
            try { battStatus = SystemInfo.batteryStatus.ToString(); } catch (Exception) { }
            sb.Append(',').Append(_thermStatus >= 0 ? _thermStatus.ToString(Inv) : "");
            sb.Append(',').Append(F(_thermHeadroom, 3));
            sb.Append(',').Append(batt >= 0f ? F(batt, 2) : "");
            sb.Append(',').Append(battStatus == "Unknown" ? "" : battStatus);
            _devA.Add(new DevRec { T = now - _t0, Thermal = _thermStatus, Headroom = _thermHeadroom, Battery = batt });
            if (dt >= 0.5f) _fps1s.Add(fps);   // 区間の終わりの短い端 (1秒に満たない) は fps の下側の数えに入れない
            try { if (_csv != null) { _csv.WriteLine(sb.ToString()); _csv.Flush(); } } catch (Exception) { }
            _rows++;
            _cpuS.Clear(); _gpuS.Clear(); _frmS.Clear();
            _secFrames = 0; _secT0 = now;
        }

        void End(float now)
        {
            if (!_rec) return;
            if (_secFrames > 0) Flush(now);
            _rec = false;
            if (_mode == "timed") _timedDone = true;
            try { if (_csv != null) { _csv.Flush(); _csv.Dispose(); } } catch (Exception) { }
            _csv = null;
            WriteSummary(now);
            DisposeRecorders();
            Debug.Log("[PerfProbe] end secs=" + F(now - _t0, 1) + " rows=" + _rows + " gpu_p95=" + P(_gpuA, 95) + " cpu_p95=" + P(_cpuA, 95) + " frame_p95=" + P(_frmA, 95) + " summary=" + LastSummary);
        }

        void DisposeRecorders()
        {
            try { if (_draw.Valid) _draw.Dispose(); } catch (Exception) { }
            try { if (_setpass.Valid) _setpass.Dispose(); } catch (Exception) { }
            try { if (_tris.Valid) _tris.Dispose(); } catch (Exception) { }
        }

        void WriteSummary(float now)
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            Kv(sb, "schema", Q("hd2d-perf/1"));
            Kv(sb, "mode", Q(_mode));
            Kv(sb, "started", Q(_stamp));
            Kv(sb, "secs", F(now - _t0, 2));
            Kv(sb, "frames", _frmA.Count.ToString(Inv));
            Kv(sb, "fps_mean", F(_frmA.Count > 0 ? _frmA.Count / Math.Max(0.001, now - _t0) : 0.0, 2));
            Kv(sb, "cpu_ms", Stats(_cpuA));
            Kv(sb, "gpu_ms", Stats(_gpuA));
            Kv(sb, "frame_ms", Stats(_frmA));
            Kv(sb, "frame_timing_stats", FrameTimingOn() ? "true" : "false");
            Kv(sb, "screen", "[" + Screen.width + "," + Screen.height + "]");
            Kv(sb, "quality", Q(QualitySettings.names != null && QualitySettings.GetQualityLevel() < QualitySettings.names.Length ? QualitySettings.names[QualitySettings.GetQualityLevel()] : QualitySettings.GetQualityLevel().ToString(Inv)));
            Kv(sb, "target_fps", Application.targetFrameRate.ToString(Inv));
            Kv(sb, "vsync", QualitySettings.vSyncCount.ToString(Inv));
            Kv(sb, "device", Q(SystemInfo.deviceModel));
            Kv(sb, "gpu", Q(SystemInfo.graphicsDeviceName));
            Kv(sb, "graphics_api", Q(SystemInfo.graphicsDeviceType.ToString()));
            Kv(sb, "identifier", Q(Application.identifier));
            Kv(sb, "flags", Q(HD2DFlags.Describe()));
            Kv(sb, "det", Autopilot.Det ? "true" : "false");
            Kv(sb, "plays", Autopilot.PerfPlays.ToString(Inv));
            Kv(sb, "turns", Autopilot.PerfTurns.ToString(Inv));
            Kv(sb, "rejumps", Autopilot.PerfRejumps.ToString(Inv));
            // ---- P31 (W4): スマホの段と保留の門の読み (手順書 docs/design/hd2d-slice/perf-runbook.md) ----
            double secs = Math.Max(0.001, now - _t0);
            double fpsMean = _frmA.Count / secs;
            Kv(sb, "tier", Q(HD2DFlags.Tier == HD2DTier.Phone ? "phone" : "pc"));
            Kv(sb, "look", Q(StageLook.Current != null ? StageLook.Current.Name : null));   // look_act1 / look_act1+look_act1_phone2 (スマホの段の変種) など
            Kv(sb, "look_sources", StageLook.Current != null ? "[" + string.Join(",", StageLook.Current.Sources.ConvertAll(Q)) + "]" : "null");
            double hz = 0.0;
            try { hz = Screen.currentResolution.refreshRateRatio.value; } catch (Exception) { }
            Kv(sb, "refresh_hz", hz > 0.0 ? F(hz, 2) : "null");
            Kv(sb, "sleep_timeout", Screen.sleepTimeout.ToString(Inv));
            Kv(sb, "diorama", DioramaJson());
            Kv(sb, "fps_1s", Fps1sJson());
            Kv(sb, "tail", TailJson(secs));
            Kv(sb, "thermal", ThermalJson());
            Kv(sb, "gate", GateJson(secs, fpsMean));
            Kv(sb, "csv", Q(LastCsv != null ? Path.GetFileName(LastCsv) : null), last: true);
            sb.Append("}\n");
            try
            {
                LastSummary = Path.Combine(_dir, "perf-" + _stamp + ".summary.json");
                File.WriteAllText(LastSummary, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e) { Debug.LogWarning("[PerfProbe] 要約を書けない " + e.Message); }
        }

        // ---- P31: 要約の読み (保留の門・終わりの 5 分・端末の温度) ----

        /// <summary>保留の門の目安 (計画 §1-2 の5)。見本の判定には使わない = 要約に数字と ok を並べるだけ</summary>
        public const float GateSecs = 900f, GateFpsMeanMin = 29f, GateGpuP95MaxMs = 25f, GateFpsLowSecond = 28f;
        /// <summary>「終わりの何秒」を tail として別に数えるか (端末が温まって GPU が抑えられた後の数字)</summary>
        const float TailSecs = 300f;

        string GateJson(double secs, double fpsMean)
        {
            double gpuP95 = PVal(_gpuA, 95);
            double tailFrom = TailFrom(secs);
            double tailFps, tailGpuP95;
            TailFpsAndGpu(tailFrom, secs, out tailFps, out tailGpuP95);
            var sb = new StringBuilder("{");
            sb.Append("\"_note\":").Append(Q("保留の門 (計画 §1-2 の5: 15分で30fps・GPU の p95 25ms 未満)。見本の判定には使わない。ok は目安"));
            sb.Append(",\"secs_min\":").Append(F(GateSecs, 0)).Append(",\"fps_mean_min\":").Append(F(GateFpsMeanMin, 1)).Append(",\"gpu_p95_max_ms\":").Append(F(GateGpuP95MaxMs, 1));
            sb.Append(",\"secs_ok\":").Append(B(secs >= GateSecs * 0.99));
            sb.Append(",\"fps_ok\":").Append(B(fpsMean >= GateFpsMeanMin));
            sb.Append(",\"tail_fps_ok\":").Append(double.IsNaN(tailFps) ? "null" : B(tailFps >= GateFpsMeanMin));
            sb.Append(",\"gpu_p95_ok\":").Append(double.IsNaN(gpuP95) ? "null" : B(gpuP95 < GateGpuP95MaxMs));
            sb.Append(",\"tail_gpu_p95_ok\":").Append(double.IsNaN(tailGpuP95) ? "null" : B(tailGpuP95 < GateGpuP95MaxMs));
            sb.Append('}');
            return sb.ToString();
        }

        static string B(bool v) { return v ? "true" : "false"; }

        /// <summary>tail の始まり: 区間が 600 秒以上なら終わりの 300 秒、それより短ければ後ろ半分</summary>
        static double TailFrom(double secs) { return secs >= 2 * TailSecs ? secs - TailSecs : secs * 0.5; }

        void TailFpsAndGpu(double from, double secs, out double fps, out double gpuP95)
        {
            int frames = 0;
            var g = new List<double>();
            foreach (var r in _recA)
            {
                if (r.T < from) continue;
                frames++;
                if (!double.IsNaN(r.Gpu)) g.Add(r.Gpu);
            }
            double span = secs - from;
            fps = frames > 0 && span > 0.5 ? frames / span : double.NaN;
            gpuP95 = PVal(g, 95);
        }

        string TailJson(double secs)
        {
            double from = TailFrom(secs);
            var cpu = new List<double>(); var gpu = new List<double>(); var frm = new List<double>();
            foreach (var r in _recA)
            {
                if (r.T < from) continue;
                frm.Add(r.Frame);
                if (!double.IsNaN(r.Cpu)) cpu.Add(r.Cpu);
                if (!double.IsNaN(r.Gpu)) gpu.Add(r.Gpu);
            }
            double span = secs - from;
            return "{\"from_s\":" + F(from, 1) + ",\"secs\":" + F(span, 1) + ",\"frames\":" + frm.Count.ToString(Inv)
                + ",\"fps_mean\":" + (span > 0.5 && frm.Count > 0 ? F(frm.Count / span, 2) : "null")
                + ",\"cpu_ms\":" + Stats(cpu) + ",\"gpu_ms\":" + Stats(gpu) + ",\"frame_ms\":" + Stats(frm) + "}";
        }

        string Fps1sJson()
        {
            if (_fps1s.Count == 0) return "null";
            int low = 0; double min = double.MaxValue;
            foreach (var v in _fps1s) { if (v < GateFpsLowSecond) low++; if (v < min) min = v; }
            return "{\"p5\":" + P(_fps1s, 5) + ",\"p50\":" + P(_fps1s, 50) + ",\"min\":" + F(min, 1)
                + ",\"secs_below_" + F(GateFpsLowSecond, 0) + "\":" + low.ToString(Inv) + ",\"n\":" + _fps1s.Count.ToString(Inv) + "}";
        }

        /// <summary>端末の温度の経過。thermal_status は Android の PowerManager の段 (0 なし・1 軽い・2 中 = 抑え始める・3 重い・4 危険・5 緊急・6 停止)</summary>
        string ThermalJson()
        {
            if (_devA.Count == 0) return "null";
            int first = -1, last = -1, max = -1; float firstT = -1f;
            float hFirst = float.NaN, hLast = float.NaN, hMax = float.NaN, bFirst = -1f, bLast = -1f;
            foreach (var d in _devA)
            {
                if (d.Thermal >= 0)
                {
                    if (first < 0) first = d.Thermal;
                    last = d.Thermal;
                    if (d.Thermal > max) max = d.Thermal;
                    if (firstT < 0f && d.Thermal >= 2) firstT = d.T;
                }
                if (!float.IsNaN(d.Headroom))
                {
                    if (float.IsNaN(hFirst)) hFirst = d.Headroom;
                    hLast = d.Headroom;
                    if (float.IsNaN(hMax) || d.Headroom > hMax) hMax = d.Headroom;
                }
                if (d.Battery >= 0f) { if (bFirst < 0f) bFirst = d.Battery; bLast = d.Battery; }
            }
            if (first < 0 && float.IsNaN(hFirst) && bFirst < 0f) return "null";
            string I(int v) { return v >= 0 ? v.ToString(Inv) : "null"; }
            string Fl(float v, int dg) { return float.IsNaN(v) || v < -0.5f ? "null" : F(v, dg); }
            return "{\"status_first\":" + I(first) + ",\"status_last\":" + I(last) + ",\"status_max\":" + I(max)
                + ",\"moderate_from_s\":" + (firstT >= 0f ? F(firstT, 0) : "null")
                + ",\"headroom_first\":" + Fl(hFirst, 3) + ",\"headroom_last\":" + Fl(hLast, 3) + ",\"headroom_max\":" + Fl(hMax, 3)
                + ",\"battery_first\":" + Fl(bFirst, 2) + ",\"battery_last\":" + Fl(bLast, 2) + "}";
        }

        static string DioramaJson()
        {
            try
            {
                var s = Diorama.LastStats;
                if (!Diorama.Active || s == null) return "{\"active\":false}";
                return "{\"active\":true,\"parts\":" + s.Parts.ToString(Inv) + ",\"triangles\":" + s.Triangles.ToString(Inv)
                    + ",\"renderers\":" + s.Renderers.ToString(Inv) + ",\"materials\":" + s.Materials.ToString(Inv) + ",\"dynamic\":" + s.Dynamic.ToString(Inv) + "}";
            }
            catch (Exception) { return "null"; }
        }

        // ---- P31: 端末の温度 (Android の PowerManager。1秒ごとの行で読む) ----
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject _activity, _power;
        bool _powerTried;
        int _sdk;
        float _headT = -999f;

        void SampleDevice(float now)
        {
            if (!_powerTried)
            {
                _powerTried = true;
                try
                {
                    using (var ver = new AndroidJavaClass("android.os.Build$VERSION")) _sdk = ver.GetStatic<int>("SDK_INT");
                    using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) _activity = up.GetStatic<AndroidJavaObject>("currentActivity");
                    if (_activity != null && _sdk >= 29) _power = _activity.Call<AndroidJavaObject>("getSystemService", "power");
                    Debug.Log("[PerfProbe] 端末の温度: SDK " + _sdk + " PowerManager=" + (_power != null));
                }
                catch (Exception e) { Debug.LogWarning("[PerfProbe] 端末の温度を読めない (空欄のまま): " + e.Message); _power = null; }
            }
            if (_power == null) return;
            try { _thermStatus = _power.Call<int>("getCurrentThermalStatus"); } catch (Exception) { _thermStatus = -1; }
            // getThermalHeadroom は 1 秒に2回以上呼ぶと NaN を返す (Android の決まり)。2 秒に1回だけ読み、間は前の値
            if (_sdk >= 30 && now - _headT >= 2f)
            {
                _headT = now;
                try { _thermHeadroom = _power.Call<float>("getThermalHeadroom", 0); } catch (Exception) { _thermHeadroom = float.NaN; }
            }
        }

        void DisposeDevice()
        {
            try { if (_power != null) _power.Dispose(); } catch (Exception) { }
            try { if (_activity != null) _activity.Dispose(); } catch (Exception) { }
            _power = null; _activity = null;
        }
#else
        void SampleDevice(float now) { }
        void DisposeDevice() { }
#endif

        void OnApplicationPause(bool paused)
        {
            if (paused && _rec) End(Time.realtimeSinceStartup);   // スマホで裏へ回った時も残す
        }

        void OnApplicationQuit()
        {
            if (_rec) End(Time.realtimeSinceStartup);
        }

        void OnDestroy()
        {
            if (_rec) End(Time.realtimeSinceStartup);
            DisposeDevice();
            if (_inst == this) _inst = null;
        }

        // ---- 道具 ----
        static string OutDir()
        {
            string shots = null;
            try { shots = Autopilot.Arg("-shots"); } catch (Exception) { }
            if (!string.IsNullOrEmpty(shots)) return shots;
            return Path.Combine(Application.persistentDataPath, "perf");
        }

        static string Rec(ProfilerRecorder r)
        {
            try { return r.Valid && r.LastValue > 0 ? r.LastValue.ToString(Inv) : ""; } catch (Exception) { return ""; }
        }

        static string P(List<double> v, double pct)
        {
            if (v == null || v.Count == 0) return "";
            var a = v.ToArray();
            Array.Sort(a);
            int i = (int)Math.Ceiling(pct / 100.0 * a.Length) - 1;
            if (i < 0) i = 0;
            if (i >= a.Length) i = a.Length - 1;
            return F(a[i], 3);
        }

        /// <summary>P の数の版 (無ければ NaN)</summary>
        static double PVal(List<double> v, double pct)
        {
            if (v == null || v.Count == 0) return double.NaN;
            var a = v.ToArray();
            Array.Sort(a);
            int i = (int)Math.Ceiling(pct / 100.0 * a.Length) - 1;
            if (i < 0) i = 0;
            if (i >= a.Length) i = a.Length - 1;
            return a[i];
        }

        static string Stats(List<double> v)
        {
            if (v == null || v.Count == 0) return "null";
            return "{\"p50\":" + P(v, 50) + ",\"p95\":" + P(v, 95) + ",\"p99\":" + P(v, 99) + ",\"n\":" + v.Count.ToString(Inv) + "}";
        }

        static string F(double v, int digits)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "";
            return Math.Round(v, digits).ToString(Inv);
        }

        static string Q(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder("\"");
            foreach (char c in s)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", Inv));
                else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }

        static void Kv(StringBuilder sb, string k, string v, bool last = false)
        {
            sb.Append("  ").Append(Q(k)).Append(": ").Append(string.IsNullOrEmpty(v) ? "null" : v).Append(last ? "\n" : ",\n");
        }
    }

    /// <summary>
    /// スマホのフレームレート (2026-10-01 HD-2D 見本 W4 P31。計画 §2-7・P31 手順1「targetFrameRate=30 を明示する」)。
    /// 実機 (Application.isMobilePlatform) では最初のシーンの前に Application.targetFrameRate = 30 を入れる。
    /// Android の既定 (-1) も 30 なので、今の APK の動きは変わらない = 既定に頼らず明示するだけ (Optimized Frame Pacing = Swappy が 30 に揃える)。
    /// 起動引数 -targetfps &lt;n&gt; で上書きできる (計測用の APK は引数ファイル perf-args.txt でも。-1 = 端末の既定・60 = 伸びしろを見る時)。
    /// PC は触らない (品質レベルの vSync のまま。撮影の実時間の速さを変えない)。PC で -targetfps を渡しても、vSync が入っている品質レベルでは Unity の決まりで効かない。
    /// 置き場: スマホの段の持ち主 (P31) のファイルがここだけなので PerfProbe.cs に同居させた。記録の置き物 (PerfProbe) とは独立 (-perfprobe が無くても動く)
    /// </summary>
    public static class PhoneFramePacing
    {
        /// <summary>スマホの段のフレームレート</summary>
        public const int PhoneFps = 30;
        /// <summary>入れた値 (int.MinValue = 触っていない = PC の既定)</summary>
        public static int Applied { get; private set; } = int.MinValue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            int fps = int.MinValue;
            string v = null;
            try { v = Autopilot.Arg("-targetfps"); } catch (Exception) { }
            int n;
            if (v != null && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out n) && n >= -1) fps = n;
            else if (Application.isMobilePlatform) fps = PhoneFps;
            if (fps == int.MinValue) return;
            Application.targetFrameRate = fps;
            Applied = fps;
            Debug.Log("[PerfProbe] targetFrameRate=" + fps.ToString(CultureInfo.InvariantCulture) + (v != null ? " (-targetfps)" : " (スマホの段)"));
        }
    }
}
