// Audio.cs — 効果音と BGM の再生 (2026-09-07 M2-5)。
// 素材は Resources/Audio/sfx/<name> と Resources/Audio/bgm/<name> (wav/ogg) があればそれ、無ければ Synth が合成する。
// 音量は PlayerPrefs (audio.master / audio.sfx / audio.bgm)。バッチ実行 (-batchmode) では音は鳴らないが呼んでも安全。
// 2026-11-04 束 D (音): BGM を 2 本で本当のクロスフェード・ダッキング・ローパス・環境音・鼓動・音程つきの鍵・同じ鍵の最短間隔と音量の揺らぎ。
//   時計は全部 Tween.UnscaledDt の積算 (_clock) = ヒットストップやとどめのスローの間も実時間で進み、決定的な撮影では 1/60 で刻む。
//   AudioSource の数: 効果音のプール 12 ＋ BGM 2 ＋ 環境音 1 ＋ 鼓動 1 = 16 本まで。AudioMixer は使わない。
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DeckRogue.Game
{
    public class Audio : MonoBehaviour
    {
        static Audio _i;
        static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, List<AudioClip>> _variants = new Dictionary<string, List<AudioClip>>();
        readonly List<AudioSource> _pool = new List<AudioSource>();

        // ---- BGM (2 本。切り替え中は両方鳴る) ----
        const float BgmFadeDur = 0.8f;       // 曲の切り替えのクロスフェード
        const float SeamOverlap = 1.0f;      // ループ版でない曲の継ぎ目の重ね (act1/title)
        readonly AudioSource[] _bgm = new AudioSource[2];
        int _bgmCur = -1;                    // 今の曲を持つ側 (-1 = まだ無い)
        float _bgmFade = 1f;                 // 0→1 で旧→新 (1 = 切り替え済み)
        float _bgmFadeDur = BgmFadeDur;
        string _bgmName;
        GameObject _bgmGo;
        AudioLowPassFilter _lowpass;

        // ---- ダッキング (BGM の自動の抑え) ----
        float _duckCur;                      // 今の沈み (0 = 素・1 = 無音)
        float _duckTarget, _duckPeak;
        float _duckAttack = 0.08f, _duckRelease = 0.9f, _duckHoldUntil;
        bool _duckHolding;

        // ---- ローパス (敗北・致死の間) ----
        const float LowpassOpen = 22000f, LowpassClosed = 700f;
        float _muffleT, _muffleDur;
        bool _muffling;

        // ---- 環境音 (1 本。鍵が変われば 0.4 秒で沈めて 0.4 秒で出す) ----
        const float AmbFadeHalf = 0.4f;
        AudioSource _amb;
        string _ambKey, _ambNext;
        float _ambFade, _ambVol;
        int _ambPhase;                       // 0 = 落ち着いている・1 = 沈めている・2 = 出している

        // ---- 鼓動 (1 本。rate の間隔で 2 拍の低い太鼓を打つ) ----
        const float HeartTireAfter = 20f;    // 続くと耳に付くので 20 秒で音量を半分に (絵は続ける)
        AudioSource _heart;
        bool _heartOn;
        float _heartRate = 1f, _heartNext, _heartSince;

        // ---- 時計と間引き ----
        float _clock;                        // Tween.UnscaledDt の積算 (Time.time は使わない)
        float _lastHover = -1f;
        readonly Dictionary<string, float> _lastAt = new Dictionary<string, float>();
        int _jitterSeq;                      // 音量の揺らぎを黄金比で散らす添字 (Random は使わない)

        public static float Master { get { return PlayerPrefs.GetFloat("audio.master", 0.8f); } set { PlayerPrefs.SetFloat("audio.master", value); Apply(); } }
        public static float SfxVol { get { return PlayerPrefs.GetFloat("audio.sfx", 0.7f); } set { PlayerPrefs.SetFloat("audio.sfx", value); } }
        public static float BgmVol { get { return PlayerPrefs.GetFloat("audio.bgm", 0.3f); } set { PlayerPrefs.SetFloat("audio.bgm", value); Apply(); } }

        static Audio I
        {
            get
            {
                if (_i == null)
                {
                    var go = new GameObject("Audio");
                    DontDestroyOnLoad(go);
                    _i = go.AddComponent<Audio>();
                }
                return _i;
            }
        }

        /// <summary>音量の反映。BGM と環境音は Update が毎フレーム書くので、ここでは即座に 1 回書くだけ (スライダの手応え)</summary>
        static void Apply()
        {
            if (_i == null) return;
            _i.TickBgm(0f);
            _i.TickAmbience(0f);
        }

        /// <summary>
        /// 名前→Resources のパス (拡張子なし) の対応表。Asset Store 等の素材をリネームせずに使う時はここに書く。
        /// 例: { "sfx/hit", "Audio/SomePack/impact_soft_01" }。無ければ Audio/<kind>/<name> → 合成、の順
        /// </summary>
        public static readonly Dictionary<string, string> Map = new Dictionary<string, string>();

        // ---- 割り当て表 (Resources/Audio/audio.json 2026-09-13 音の設計書) ----
        /// <summary>表の 1 行。MinGap = 同じ鍵の最短間隔 (秒。0 で間引かない)・VolJitter = 音量の揺らぎ (±。Jitter 0 の鍵は揺らさない)</summary>
        public sealed class SfxEntry { public string Sfx; public float Volume = 0.7f; public float Jitter = 0.06f; public float MinGap = 0.05f; public float VolJitter = 0.08f; }
        static Dictionary<string, string> _bgmTable;
        static Dictionary<string, SfxEntry> _sfxTable;
        static readonly HashSet<string> _bgmOverlap = new HashSet<string>();   // ループ版でない曲 (末尾を次の頭に重ねる)

        static void LoadTable()
        {
            if (_bgmTable != null) return;
            _bgmTable = new Dictionary<string, string>();
            _sfxTable = new Dictionary<string, SfxEntry>();
            try
            {
                var ta = Resources.Load<TextAsset>("Audio/audio");
                if (ta == null) return;
                var root = JObject.Parse(ta.text);
                var bgm = root["bgm"] as JObject;
                if (bgm != null)
                    foreach (var kv in bgm)
                    {
                        if (kv.Key == "_overlap")
                        {
                            var arr = kv.Value as JArray;
                            if (arr != null) foreach (var t in arr) _bgmOverlap.Add(t.ToString());
                            continue;
                        }
                        if (kv.Key.StartsWith("_")) continue;
                        _bgmTable[kv.Key] = kv.Value == null || kv.Value.Type == JTokenType.Null ? null : kv.Value.ToString();
                    }
                var sfx = root["sfx"] as JObject;
                if (sfx != null)
                    foreach (var kv in sfx)
                    {
                        if (kv.Key.StartsWith("_")) continue;
                        var o = kv.Value as JObject;
                        if (o == null) continue;
                        var e = new SfxEntry();
                        var s = o["sfx"];
                        e.Sfx = s == null || s.Type == JTokenType.Null ? null : s.ToString();
                        if (o["volume"] != null) e.Volume = (float)o["volume"];
                        if (o["jitter"] != null) e.Jitter = (float)o["jitter"];
                        if (o["minGap"] != null) e.MinGap = (float)o["minGap"];
                        if (o["volJitter"] != null) e.VolJitter = (float)o["volJitter"];
                        _sfxTable[kv.Key] = e;
                    }
            }
            catch (System.Exception e) { Debug.LogWarning("[Audio] audio.json: " + e.Message); }
        }

        /// <summary>場面名 (title/map1/battle2/boss3/elite/rest/won/lost) → 表の BGM 名。表に無い・null なら null (=無音)。elite/rest は無ければ null</summary>
        public static string BgmFor(string scene)
        {
            LoadTable();
            string name;
            return _bgmTable.TryGetValue(scene, out name) ? name : null;
        }

        /// <summary>表の鍵 (イベント型名 or ui.xxx) で鳴らす。表に無い鍵は何もしない。volMul で場面ごとに減衰できる</summary>
        public static bool Key(string key, float volMul = 1f) { return KeyPitched(key, 1f, volMul); }

        /// <summary>画面の操作音 (ui.xxx)</summary>
        public static void Ui(string action, float volMul = 1f) { Key("ui." + action, volMul); }

        /// <summary>表にある鍵か (Presenter が「自分で特別扱いするか、表で鳴らすか」を決めるのに使う)</summary>
        public static bool HasKey(string key) { LoadTable(); return _sfxTable.ContainsKey(key); }

        /// <summary>
        /// 表の鍵を音程の倍率つきで鳴らす (チェインの段・多段の積み上がりのラチェット。pitchMul 1.0 = 素・1.06 = 半音上)。表に無い鍵は何もしない。
        /// 同じ鍵が MinGap 秒以内に来たら捨てる (置物の連発・全体攻撃の 0.03 秒刻み)。音量は Jitter を持つ鍵だけ ±VolJitter で揺らす (手弾きに聴こえる)
        /// </summary>
        public static bool KeyPitched(string key, float pitchMul, float volMul = 1f)
        {
            LoadTable();
            SfxEntry e;
            if (!_sfxTable.TryGetValue(key, out e) || string.IsNullOrEmpty(e.Sfx)) return false;
            if (!Application.isPlaying) return true;
            var a = I;
            if (e.MinGap > 0f)
            {
                float last;
                if (a._lastAt.TryGetValue(key, out last) && a._clock - last < e.MinGap)
                {
                    if (Verbose) Debug.Log("[Audio] skip " + key + " (minGap " + e.MinGap.ToString("F2") + ")");
                    return true;
                }
                a._lastAt[key] = a._clock;
            }
            float vol = e.Volume * volMul;
            if (e.Jitter > 0f && e.VolJitter > 0f)
            {
                float u = (++a._jitterSeq) * 0.6180339887f;   // 黄金比の小数部 = 0〜1 を均等に散らす
                u -= Mathf.Floor(u);
                vol *= 1f + (u * 2f - 1f) * e.VolJitter;
            }
            PlayEx(e.Sfx, vol, e.Jitter, pitchMul);
            return true;
        }

        static AudioClip Load(string kind, string name)
        {
            string key = kind + "/" + name;
            AudioClip c;
            if (_clips.TryGetValue(key, out c)) return c;
            try
            {
                string mapped;
                c = Map.TryGetValue(key, out mapped) ? Resources.Load<AudioClip>(mapped) : null;
                if (c == null) c = Resources.Load<AudioClip>("Audio/" + kind + "/" + name);
                if (c == null) c = kind == "bgm" ? Synth.Bgm(name) : Synth.Sfx(name);
            }
            catch (System.Exception e) { Debug.LogWarning("[Audio] " + key + ": " + e.Message); c = null; }
            _clips[key] = c;
            return c;
        }

        /// <summary>素材の番号違い (name_2, name_3 …) も集めて、鳴らすたびに選ぶ (素材が1つなら合成音は使わない)</summary>
        static AudioClip Pick(string name)
        {
            List<AudioClip> list;
            if (!_variants.TryGetValue(name, out list))
            {
                list = new List<AudioClip>();
                var first = Load("sfx", name);
                if (first != null) list.Add(first);
                for (int i = 2; i <= 6; i++)
                {
                    AudioClip v = null;
                    try { v = Resources.Load<AudioClip>("Audio/sfx/" + name + "_" + i); } catch (System.Exception) { }
                    if (v == null) break;
                    list.Add(v);
                }
                _variants[name] = list;
            }
            return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
        }

        /// <summary>鳴らした音を Debug.Log に残す (自動操縦の検証用。2026-09-19「回復音ならない」の切り分け)</summary>
        public static bool Verbose;

        /// <summary>効果音。pitchJitter で毎回少し音程を揺らす (同じ音の連打が機械的にならない)</summary>
        public static void Play(string name, float volume = 1f, float pitchJitter = 0.06f) { PlayEx(name, volume, pitchJitter, 1f); }

        /// <summary>効果音を音程の倍率つきで (KeyPitched の実体。pitchMul 1 = 素)</summary>
        public static void PlayEx(string name, float volume, float pitchJitter, float pitchMul)
        {
            if (!Application.isPlaying) return;
            var a = I;
            var clip = Pick(name);
            if (Verbose) Debug.Log("[Audio] play " + name + " clip=" + (clip == null ? "null" : clip.name + " " + clip.length.ToString("F2") + "s " + clip.loadState) + " vol=" + (volume * SfxVol * Master).ToString("F2") + (pitchMul != 1f ? " pitch=" + pitchMul.ToString("F2") : ""));
            if (clip == null) return;
            AudioSource src = null;
            for (int i = 0; i < a._pool.Count; i++) if (!a._pool[i].isPlaying) { src = a._pool[i]; break; }
            if (src == null)
            {
                if (a._pool.Count >= 12) src = a._pool[0];
                else { src = a.gameObject.AddComponent<AudioSource>(); src.playOnAwake = false; a._pool.Add(src); }
            }
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume * SfxVol * Master);
            src.pitch = Mathf.Clamp(pitchMul, 0.25f, 3f) * (1f + Random.Range(-pitchJitter, pitchJitter));
            src.Play();
        }

        // ---- 2026-11-04 見た目・UX・演出の磨き: 音の共通口 (呼ぶ側はこの署名だけを頼る) ----

        /// <summary>BGM の自動の抑え (ダッキング): depth (0〜1。0.5 = 半分に) まで attack 秒で沈め、hold 秒保ち、release 秒で戻す。重ねたら深い方・長い方。時計は unscaled</summary>
        public static void Duck(float depth = 0.5f, float hold = 0.6f, float release = 0.9f, float attack = 0.08f)
        {
            if (!Application.isPlaying) return;
            var a = I;
            depth = Mathf.Clamp01(depth);
            attack = Mathf.Max(0.01f, attack);
            release = Mathf.Max(0.01f, release);
            if (a._duckHolding)
            {
                a._duckTarget = Mathf.Max(a._duckTarget, depth);
                a._duckAttack = Mathf.Min(a._duckAttack, attack);
                a._duckHoldUntil = Mathf.Max(a._duckHoldUntil, a._clock + attack + hold);
                a._duckRelease = Mathf.Max(a._duckRelease, release);
            }
            else
            {
                a._duckTarget = Mathf.Max(a._duckCur, depth);
                a._duckAttack = attack;
                a._duckHoldUntil = a._clock + attack + hold;
                a._duckRelease = release;
                a._duckHolding = true;
            }
            a._duckPeak = a._duckTarget;
        }

        /// <summary>BGM に一瞬ローパスを掛ける (敗北・致死の間)。0.12 秒で 700Hz まで閉じ、seconds 秒かけて元へ。0 で即戻す</summary>
        public static void Muffle(float seconds)
        {
            if (!Application.isPlaying) return;
            var a = I;
            a.EnsureBgm();
            if (seconds <= 0f)
            {
                a._muffling = false;
                if (a._lowpass != null) { a._lowpass.cutoffFrequency = LowpassOpen; a._lowpass.enabled = false; }
                return;
            }
            a._muffleDur = seconds;
            a._muffleT = 0f;
            a._muffling = true;
            if (a._lowpass != null) a._lowpass.enabled = true;
        }

        /// <summary>環境音の層 (森の夜・焚き火など)。key は表の ambience.xxx の鍵。null で止める (0.4 秒で沈めて 0.4 秒で出す)。同じ鍵なら何もしない</summary>
        public static void Ambience(string key)
        {
            if (!Application.isPlaying) return;
            LoadTable();
            var a = I;
            if (key != null)
            {
                SfxEntry e;
                if (!_sfxTable.TryGetValue(key, out e) || string.IsNullOrEmpty(e.Sfx)) key = null;   // 表に無い鍵は「止める」と同じ
            }
            if (key == a._ambKey && a._ambPhase != 1) { a._ambNext = key; return; }
            if (a._ambPhase == 1 && key == a._ambNext) return;
            a._ambNext = key;
            if (a._amb == null || !a._amb.isPlaying)
            {
                a.StartAmbience(key);
                return;
            }
            a._ambPhase = 1;   // 今の層を沈める → 沈んだら次を出す
        }

        /// <summary>鼓動 (低 HP・致死級)。on で始め、rate は 1 秒あたりの拍 (0.9〜1.7)。off で止める。20 秒続いたら音量を半分に</summary>
        public static void Heartbeat(bool on, float rate = 1f)
        {
            if (!Application.isPlaying) return;
            var a = I;
            rate = Mathf.Clamp(rate, 0.3f, 4f);
            if (on && !a._heartOn) { a._heartSince = 0f; a._heartNext = a._clock; }
            a._heartOn = on;
            a._heartRate = rate;
        }

        /// <summary>ホバー音は連打を抑える (0.06 秒に1回)。表の ui.hover を通す (2026-11-04 A18: 直書きをやめた)</summary>
        public static void Hover()
        {
            if (!Application.isPlaying) return;
            var a = I;
            if (a._lastHover >= 0f && a._clock - a._lastHover < 0.06f) return;
            a._lastHover = a._clock;
            if (!Key("ui.hover")) Play("hover", 0.5f, 0.1f);
        }

        /// <summary>BGM を切り替える (同じ名前なら何もしない)。新しい曲を 0 から 0.8 秒で上げながら今の曲を同じ 0.8 秒で下げる (等パワー)。null で無音へ</summary>
        public static void Bgm(string name)
        {
            if (!Application.isPlaying) return;
            var a = I;
            if (a._bgmName == name) return;
            a._bgmName = name;
            a.EnsureBgm();
            var clip = name != null ? Load("bgm", name) : null;
            int next = a._bgmCur < 0 ? 0 : 1 - a._bgmCur;
            var src = a._bgm[next];
            src.Stop();
            src.clip = clip;
            src.volume = 0f;
            if (clip != null)
            {
                src.loop = !_bgmOverlap.Contains(name);   // ループ版でない曲は末尾を自分で重ねる (TickBgm)
                src.Play();
            }
            a._bgmCur = next;
            a._bgmFade = 0f;
            a._bgmFadeDur = BgmFadeDur;
            a.TickBgm(0f);
        }

        // ---- 中身 ----

        void EnsureBgm()
        {
            if (_bgmGo != null) return;
            _bgmGo = new GameObject("Bgm");          // 効果音のプールと別の物に置く = ローパスは BGM だけに掛かる
            _bgmGo.transform.SetParent(transform, false);
            for (int i = 0; i < 2; i++)
            {
                var s = _bgmGo.AddComponent<AudioSource>();
                s.loop = true;
                s.playOnAwake = false;
                s.volume = 0f;
                _bgm[i] = s;
            }
            _lowpass = _bgmGo.AddComponent<AudioLowPassFilter>();
            _lowpass.cutoffFrequency = LowpassOpen;
            _lowpass.lowpassResonanceQ = 1f;
            _lowpass.enabled = false;
        }

        void Update()
        {
            float dt = Tween.UnscaledDt;
            _clock += dt;
            TickDuck(dt);
            TickMuffle(dt);
            TickBgm(dt);
            TickAmbience(dt);
            TickHeartbeat(dt);
        }

        void TickDuck(float dt)
        {
            if (_duckHolding)
            {
                if (_clock < _duckHoldUntil) _duckCur = Mathf.MoveTowards(_duckCur, _duckTarget, dt * Mathf.Max(_duckTarget, 0.05f) / _duckAttack);
                else _duckHolding = false;
            }
            else if (_duckCur > 0f)
            {
                _duckCur = Mathf.MoveTowards(_duckCur, 0f, dt * Mathf.Max(_duckPeak, 0.05f) / _duckRelease);
                if (_duckCur <= 0.0005f) { _duckCur = 0f; _duckTarget = 0f; }
            }
        }

        void TickMuffle(float dt)
        {
            if (!_muffling || _lowpass == null) return;
            _muffleT += dt;
            float closeDur = Mathf.Min(0.12f, _muffleDur * 0.25f);
            float lo = Mathf.Log(LowpassClosed), hi = Mathf.Log(LowpassOpen);
            float cut;
            if (_muffleT < closeDur) cut = Mathf.Lerp(hi, lo, _muffleT / closeDur);
            else
            {
                float k = Mathf.Clamp01((_muffleT - closeDur) / Mathf.Max(0.01f, _muffleDur - closeDur));
                cut = Mathf.Lerp(lo, hi, k * k);   // 遠くなった音がゆっくり戻る
            }
            _lowpass.cutoffFrequency = Mathf.Exp(cut);
            if (_muffleT >= _muffleDur)
            {
                _muffling = false;
                _lowpass.cutoffFrequency = LowpassOpen;
                _lowpass.enabled = false;
            }
        }

        void TickBgm(float dt)
        {
            if (_bgmCur < 0) return;
            if (_bgmFade < 1f) _bgmFade = Mathf.Min(1f, _bgmFade + dt / Mathf.Max(0.01f, _bgmFadeDur));
            float baseVol = Master * BgmVol * (1f - _duckCur);
            float k = _bgmFade * Mathf.PI * 0.5f;
            for (int i = 0; i < 2; i++)
            {
                var src = _bgm[i];
                if (src == null) continue;
                if (i == _bgmCur) { src.volume = baseVol * Mathf.Sin(k); continue; }
                if (_bgmFade >= 1f)
                {
                    if (src.isPlaying) src.Stop();
                    src.volume = 0f;
                }
                else src.volume = baseVol * Mathf.Cos(k);
            }
            // ループ版でない曲の継ぎ目: 残り 1 秒で同じ曲をもう 1 本から始めて重ねる (無音の谷を作らない)
            var cur = _bgm[_bgmCur];
            if (_bgmFade >= 1f && cur != null && cur.clip != null && !cur.loop && cur.isPlaying && cur.clip.length - cur.time < SeamOverlap)
            {
                int next = 1 - _bgmCur;
                var other = _bgm[next];
                other.Stop();
                other.clip = cur.clip;
                other.loop = false;
                other.volume = 0f;
                other.Play();
                _bgmCur = next;
                _bgmFade = 0f;
                _bgmFadeDur = SeamOverlap;
            }
        }

        void StartAmbience(string key)
        {
            _ambKey = key;
            _ambNext = key;
            if (key == null) { _ambPhase = 0; if (_amb != null) _amb.Stop(); return; }
            SfxEntry e;
            if (!_sfxTable.TryGetValue(key, out e) || string.IsNullOrEmpty(e.Sfx)) { _ambKey = null; _ambPhase = 0; return; }
            var clip = Load("sfx", e.Sfx);
            if (clip == null) { _ambKey = null; _ambPhase = 0; return; }
            if (_amb == null)
            {
                var go = new GameObject("Ambience");
                go.transform.SetParent(transform, false);
                _amb = go.AddComponent<AudioSource>();
                _amb.loop = true;
                _amb.playOnAwake = false;
            }
            _ambVol = e.Volume;
            _amb.Stop();
            _amb.clip = clip;
            _amb.volume = 0f;
            _amb.pitch = 1f;
            _amb.Play();
            _ambFade = 0f;
            _ambPhase = 2;
        }

        void TickAmbience(float dt)
        {
            if (_amb == null) return;
            if (_ambPhase == 1)
            {
                _ambFade = Mathf.Max(0f, _ambFade - dt / AmbFadeHalf);
                if (_ambFade <= 0f) { _amb.Stop(); StartAmbience(_ambNext); }
            }
            else if (_ambPhase == 2)
            {
                _ambFade = Mathf.Min(1f, _ambFade + dt / AmbFadeHalf);
                if (_ambFade >= 1f) _ambPhase = 0;
            }
            // 環境音は BGM のスライダに含める (設定に 4 本目を増やさない)
            _amb.volume = Mathf.Clamp01(Master * BgmVol * _ambVol * _ambFade);
        }

        void TickHeartbeat(float dt)
        {
            if (!_heartOn) return;
            _heartSince += dt;
            if (_clock < _heartNext) return;
            if (_heart == null)
            {
                var go = new GameObject("Heartbeat");
                go.transform.SetParent(transform, false);
                _heart = go.AddComponent<AudioSource>();
                _heart.loop = false;
                _heart.playOnAwake = false;
                _heart.clip = Load("sfx", "heartbeat");
            }
            if (_heart.clip == null) { _heartOn = false; return; }
            float vol = (_heartRate >= 1.3f ? 0.5f : 0.35f) * (_heartSince > HeartTireAfter ? 0.5f : 1f);
            _heart.volume = Mathf.Clamp01(vol * SfxVol * Master);
            _heart.pitch = _heartRate >= 1.3f ? 1.08f : 1f;
            _heart.Stop();
            _heart.Play();
            _heartNext = _clock + 1f / _heartRate;
        }
    }
}
