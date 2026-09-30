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
            Debug.Log("[PerfProbe] 準備 (-perfprobe=" + _argProbe + " secs=" + _argSecs.ToString("0.#", Inv) + " perf=" + HD2DFlags.Perf.ToString("0.#", Inv) + ")");
        }

        // ---- 区間 ----
        bool _rec, _timedDone;
        int _session;
        float _armT, _t0, _secT0;
        int _secFrames;
        readonly List<double> _cpuS = new List<double>(), _gpuS = new List<double>(), _frmS = new List<double>();   // この1秒
        readonly List<double> _cpuA = new List<double>(), _gpuA = new List<double>(), _frmA = new List<double>();   // 区間全体
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
            _session++;
            _stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", Inv) + (_session > 1 ? "-" + _session : "");
            _dir = OutDir();
            try
            {
                Directory.CreateDirectory(_dir);
                LastCsv = Path.Combine(_dir, "perf-" + _stamp + ".csv");
                _csv = new StreamWriter(LastCsv, false, new UTF8Encoding(false));
                _csv.WriteLine("t_s,frames,fps,cpu_p50_ms,cpu_p95_ms,cpu_p99_ms,gpu_p50_ms,gpu_p95_ms,gpu_p99_ms,frame_p50_ms,frame_p95_ms,frame_p99_ms,drawcalls,setpass,triangles,plays,turns,rejumps");
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
            if (n > 0)
            {
                double cpu = _ft[0].cpuFrameTime, gpu = _ft[0].gpuFrameTime;
                if (cpu > 0.0) { _cpuS.Add(cpu); _cpuA.Add(cpu); }
                if (gpu > 0.0) { _gpuS.Add(gpu); _gpuA.Add(gpu); }
            }
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
            Kv(sb, "csv", Q(LastCsv != null ? Path.GetFileName(LastCsv) : null), last: true);
            sb.Append("}\n");
            try
            {
                LastSummary = Path.Combine(_dir, "perf-" + _stamp + ".summary.json");
                File.WriteAllText(LastSummary, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e) { Debug.LogWarning("[PerfProbe] 要約を書けない " + e.Message); }
        }

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
}
