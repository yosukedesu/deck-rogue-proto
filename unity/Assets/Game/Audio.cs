// Audio.cs — 効果音と BGM の再生 (2026-09-07 M2-5)。
// 素材は Resources/Audio/sfx/<name> と Resources/Audio/bgm/<name> (wav/ogg) があればそれ、無ければ Synth が合成する。
// 音量は PlayerPrefs (audio.master / audio.sfx / audio.bgm)。バッチ実行 (-batchmode) では音は鳴らないが呼んでも安全。
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
        AudioSource _bgm;
        string _bgmName;
        float _lastHover;

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

        static void Apply()
        {
            if (_i != null && _i._bgm != null) _i._bgm.volume = Master * BgmVol;
        }

        /// <summary>
        /// 名前→Resources のパス (拡張子なし) の対応表。Asset Store 等の素材をリネームせずに使う時はここに書く。
        /// 例: { "sfx/hit", "Audio/SomePack/impact_soft_01" }。無ければ Audio/<kind>/<name> → 合成、の順
        /// </summary>
        public static readonly Dictionary<string, string> Map = new Dictionary<string, string>();

        // ---- 割り当て表 (Resources/Audio/audio.json 2026-09-13 音の設計書) ----
        public sealed class SfxEntry { public string Sfx; public float Volume = 0.7f; public float Jitter = 0.06f; }
        static Dictionary<string, string> _bgmTable;
        static Dictionary<string, SfxEntry> _sfxTable;

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
        public static bool Key(string key, float volMul = 1f)
        {
            LoadTable();
            SfxEntry e;
            if (!_sfxTable.TryGetValue(key, out e) || string.IsNullOrEmpty(e.Sfx)) return false;
            Play(e.Sfx, e.Volume * volMul, e.Jitter);
            return true;
        }

        /// <summary>画面の操作音 (ui.xxx)</summary>
        public static void Ui(string action, float volMul = 1f) { Key("ui." + action, volMul); }

        /// <summary>表にある鍵か (Presenter が「自分で特別扱いするか、表で鳴らすか」を決めるのに使う)</summary>
        public static bool HasKey(string key) { LoadTable(); return _sfxTable.ContainsKey(key); }

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

        /// <summary>効果音。pitchJitter で毎回少し音程を揺らす (同じ音の連打が機械的にならない)</summary>
        public static void Play(string name, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (!Application.isPlaying) return;
            var a = I;
            var clip = Pick(name);
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
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.Play();
        }

        /// <summary>ホバー音は連打を抑える (0.06 秒に1回)</summary>
        public static void Hover()
        {
            var a = I;
            if (Time.unscaledTime - a._lastHover < 0.06f) return;
            a._lastHover = Time.unscaledTime;
            Play("hover", 0.5f, 0.1f);
        }

        /// <summary>BGM を切り替える (同じ名前なら何もしない)。短いクロスフェード</summary>
        public static void Bgm(string name)
        {
            if (!Application.isPlaying) return;
            var a = I;
            if (a._bgmName == name) return;
            a._bgmName = name;
            var clip = name != null ? Load("bgm", name) : null;
            if (a._bgm == null)
            {
                a._bgm = a.gameObject.AddComponent<AudioSource>();
                a._bgm.loop = true;
                a._bgm.playOnAwake = false;
            }
            var src = a._bgm;
            float target = Master * BgmVol;
            if (src.isPlaying)
            {
                Tween.Run(0.4f, k => { if (src != null) src.volume = target * (1f - k); }, Ease.Linear, () =>
                {
                    if (src == null) return;
                    src.Stop();
                    if (clip == null) return;
                    src.clip = clip;
                    src.volume = 0f;
                    src.Play();
                    Tween.Run(0.6f, k2 => { if (src != null) src.volume = target * k2; });
                });
            }
            else if (clip != null)
            {
                src.clip = clip;
                src.volume = 0f;
                src.Play();
                Tween.Run(0.6f, k2 => { if (src != null) src.volume = target * k2; });
            }
        }
    }
}
