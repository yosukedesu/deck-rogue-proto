// Audio.cs — 効果音と BGM の再生 (2026-09-07 M2-5)。
// 素材は Resources/Audio/sfx/<name> と Resources/Audio/bgm/<name> (wav/ogg) があればそれ、無ければ Synth が合成する。
// 音量は PlayerPrefs (audio.master / audio.sfx / audio.bgm)。バッチ実行 (-batchmode) では音は鳴らないが呼んでも安全。
using System.Collections.Generic;
using UnityEngine;

namespace DeckRogue.Game
{
    public class Audio : MonoBehaviour
    {
        static Audio _i;
        static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        readonly List<AudioSource> _pool = new List<AudioSource>();
        AudioSource _bgm;
        string _bgmName;
        float _lastHover;

        public static float Master { get { return PlayerPrefs.GetFloat("audio.master", 0.8f); } set { PlayerPrefs.SetFloat("audio.master", value); Apply(); } }
        public static float SfxVol { get { return PlayerPrefs.GetFloat("audio.sfx", 0.7f); } set { PlayerPrefs.SetFloat("audio.sfx", value); } }
        public static float BgmVol { get { return PlayerPrefs.GetFloat("audio.bgm", 0.35f); } set { PlayerPrefs.SetFloat("audio.bgm", value); Apply(); } }

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

        static AudioClip Load(string kind, string name)
        {
            string key = kind + "/" + name;
            AudioClip c;
            if (_clips.TryGetValue(key, out c)) return c;
            try
            {
                c = Resources.Load<AudioClip>("Audio/" + kind + "/" + name);
                if (c == null) c = kind == "bgm" ? Synth.Bgm(name) : Synth.Sfx(name);
            }
            catch (System.Exception e) { Debug.LogWarning("[Audio] " + key + ": " + e.Message); c = null; }
            _clips[key] = c;
            return c;
        }

        /// <summary>効果音。pitchJitter で毎回少し音程を揺らす (同じ音の連打が機械的にならない)</summary>
        public static void Play(string name, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (!Application.isPlaying) return;
            var a = I;
            var clip = Load("sfx", name);
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
