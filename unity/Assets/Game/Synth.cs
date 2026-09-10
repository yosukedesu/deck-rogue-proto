// Synth.cs — 効果音と BGM をコードで合成する (2026-09-07 M2-5「SE・BGM」)。
// 外部素材は権利確認と試聴の往復が要るので、まずはレトロ調 (矩形波・三角波・ノイズ) を生成して当てる。
// Resources/Audio/sfx/<name>.wav|ogg か Resources/Audio/bgm/<name>.ogg があればそちらを使う (Audio.cs)。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeckRogue.Game
{
    public static class Synth
    {
        public const int Rate = 22050;

        static float Osc(string wave, float phase)
        {
            float p = phase - Mathf.Floor(phase);
            switch (wave)
            {
                case "square": return p < 0.5f ? 1f : -1f;
                case "pulse": return p < 0.25f ? 1f : -1f;
                case "tri": return 1f - 4f * Mathf.Abs(p - 0.5f);
                case "saw": return 2f * p - 1f;
                default: return Mathf.Sin(p * Mathf.PI * 2f);
            }
        }

        /// <summary>ノート (周波数の始点→終点、長さ、波形、包絡) をバッファへ加算する</summary>
        static void Tone(float[] buf, float at, float f0, float f1, float dur, string wave, float attack, float release, float vol, System.Random rng = null)
        {
            int start = Mathf.Max(0, (int)(at * Rate));
            int n = (int)(dur * Rate);
            float phase = 0f;
            for (int i = 0; i < n && start + i < buf.Length; i++)
            {
                float t = (float)i / n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += f / Rate;
                float env = 1f;
                float ts = (float)i / Rate;
                if (ts < attack) env = ts / attack;
                float rel = dur - ts;
                if (rel < release) env *= Mathf.Max(0f, rel / release);
                buf[start + i] += Osc(wave, phase) * env * vol;
            }
        }

        /// <summary>ノイズ (簡易ローパス付き)</summary>
        static void Noise(float[] buf, float at, float dur, float attack, float release, float vol, float lowpass, System.Random rng)
        {
            int start = Mathf.Max(0, (int)(at * Rate));
            int n = (int)(dur * Rate);
            float last = 0f;
            for (int i = 0; i < n && start + i < buf.Length; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = Mathf.Lerp(last, white, lowpass);
                float ts = (float)i / Rate;
                float env = 1f;
                if (ts < attack) env = ts / attack;
                float rel = dur - ts;
                if (rel < release) env *= Mathf.Max(0f, rel / release);
                buf[start + i] += last * env * vol;
            }
        }

        static AudioClip Finish(string name, float[] buf, float gain = 1f)
        {
            float peak = 0.0001f;
            for (int i = 0; i < buf.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
            float k = Mathf.Min(1f, 0.9f / peak) * gain;
            for (int i = 0; i < buf.Length; i++) buf[i] = (float)Math.Tanh(buf[i] * k);
            var clip = AudioClip.Create(name, buf.Length, 1, Rate, false);
            clip.SetData(buf, 0);
            return clip;
        }

        static float[] Buf(float seconds) { return new float[(int)(seconds * Rate)]; }

        // ---- 効果音 ----

        public static AudioClip Sfx(string name)
        {
            var rng = new System.Random(name.GetHashCode());
            float[] b;
            switch (name)
            {
                case "card_draw":
                    b = Buf(0.16f);
                    Noise(b, 0f, 0.14f, 0.005f, 0.08f, 0.5f, 0.35f, rng);
                    Tone(b, 0f, 900f, 1500f, 0.08f, "sine", 0.005f, 0.05f, 0.25f);
                    return Finish(name, b, 0.7f);
                case "card_play":
                    b = Buf(0.28f);
                    Noise(b, 0f, 0.25f, 0.01f, 0.15f, 0.6f, 0.25f, rng);
                    Tone(b, 0f, 700f, 250f, 0.22f, "sine", 0.01f, 0.12f, 0.3f);
                    return Finish(name, b, 0.8f);
                case "card_set":
                    b = Buf(0.2f);
                    Tone(b, 0f, 520f, 480f, 0.08f, "tri", 0.005f, 0.05f, 0.5f);
                    Tone(b, 0.07f, 780f, 720f, 0.1f, "tri", 0.005f, 0.06f, 0.4f);
                    return Finish(name, b, 0.7f);
                case "hit":
                    b = Buf(0.3f);
                    Tone(b, 0f, 180f, 60f, 0.18f, "sine", 0.002f, 0.1f, 0.9f);
                    Noise(b, 0f, 0.12f, 0.002f, 0.08f, 0.7f, 0.6f, rng);
                    return Finish(name, b, 0.9f);
                case "hit_big":
                    b = Buf(0.5f);
                    Tone(b, 0f, 140f, 40f, 0.32f, "sine", 0.002f, 0.2f, 1f);
                    Tone(b, 0f, 300f, 90f, 0.12f, "square", 0.002f, 0.08f, 0.3f);
                    Noise(b, 0f, 0.25f, 0.002f, 0.18f, 0.8f, 0.5f, rng);
                    return Finish(name, b, 1f);
                case "slash":
                    b = Buf(0.22f);
                    Noise(b, 0f, 0.2f, 0.002f, 0.12f, 0.8f, 0.8f, rng);
                    Tone(b, 0f, 1800f, 600f, 0.12f, "saw", 0.002f, 0.08f, 0.25f);
                    return Finish(name, b, 0.8f);
                case "block":
                    b = Buf(0.26f);
                    Tone(b, 0f, 1200f, 1100f, 0.06f, "square", 0.002f, 0.04f, 0.5f);
                    Tone(b, 0.02f, 2400f, 2300f, 0.18f, "sine", 0.002f, 0.14f, 0.35f);
                    Noise(b, 0f, 0.05f, 0.002f, 0.03f, 0.4f, 0.9f, rng);
                    return Finish(name, b, 0.7f);
                case "heal":
                    b = Buf(0.5f);
                    Tone(b, 0f, 660f, 660f, 0.12f, "tri", 0.01f, 0.06f, 0.5f);
                    Tone(b, 0.12f, 880f, 880f, 0.12f, "tri", 0.01f, 0.06f, 0.5f);
                    Tone(b, 0.24f, 1320f, 1320f, 0.24f, "tri", 0.01f, 0.18f, 0.5f);
                    return Finish(name, b, 0.7f);
                case "buff":
                    b = Buf(0.4f);
                    Tone(b, 0f, 300f, 600f, 0.3f, "pulse", 0.01f, 0.15f, 0.5f);
                    return Finish(name, b, 0.6f);
                case "turn":
                    b = Buf(0.6f);
                    Tone(b, 0f, 880f, 880f, 0.5f, "sine", 0.005f, 0.4f, 0.6f);
                    Tone(b, 0f, 1760f, 1760f, 0.3f, "sine", 0.005f, 0.25f, 0.2f);
                    return Finish(name, b, 0.6f);
                case "enemy_turn":
                    b = Buf(0.6f);
                    Tone(b, 0f, 220f, 220f, 0.25f, "square", 0.005f, 0.1f, 0.4f);
                    Tone(b, 0.22f, 165f, 165f, 0.35f, "square", 0.005f, 0.25f, 0.4f);
                    return Finish(name, b, 0.6f);
                case "lunge":
                    b = Buf(0.25f);
                    Noise(b, 0f, 0.22f, 0.05f, 0.1f, 0.6f, 0.3f, rng);
                    Tone(b, 0f, 200f, 500f, 0.2f, "sine", 0.03f, 0.1f, 0.25f);
                    return Finish(name, b, 0.7f);
                case "click":
                    b = Buf(0.08f);
                    Tone(b, 0f, 1000f, 800f, 0.06f, "square", 0.002f, 0.04f, 0.4f);
                    return Finish(name, b, 0.5f);
                case "hover":
                    b = Buf(0.05f);
                    Tone(b, 0f, 1500f, 1700f, 0.04f, "sine", 0.002f, 0.03f, 0.3f);
                    return Finish(name, b, 0.35f);
                case "energy":
                    b = Buf(0.3f);
                    Tone(b, 0f, 700f, 1400f, 0.25f, "sine", 0.005f, 0.15f, 0.5f);
                    return Finish(name, b, 0.6f);
                case "death":
                    b = Buf(0.7f);
                    Tone(b, 0f, 400f, 60f, 0.6f, "square", 0.005f, 0.4f, 0.5f);
                    Noise(b, 0f, 0.5f, 0.01f, 0.4f, 0.5f, 0.3f, rng);
                    return Finish(name, b, 0.7f);
                case "win":
                    // 絵本の終わりの小さな鈴 (2026-09-08「戦闘終了の音が怖すぎる」: 矩形波のファンファーレをやめ、ハープ風の上昇と余韻に)
                    b = Buf(2.4f);
                    float[] notes = { 523f, 659f, 784f, 1047f };
                    for (int i = 0; i < notes.Length; i++) Tone(b, i * 0.16f, notes[i], notes[i], 1.6f - i * 0.15f, "sine", 0.008f, 0.55f, 0.32f);
                    Tone(b, 0.62f, 1568f, 1568f, 1.5f, "sine", 0.01f, 0.8f, 0.16f);
                    Tone(b, 0f, 262f, 262f, 2.3f, "tri", 0.25f, 1.6f, 0.14f);
                    Tone(b, 0f, 392f, 392f, 2.3f, "tri", 0.3f, 1.6f, 0.1f);
                    return Finish(name, b, 0.7f);
                case "lose":
                    // 静かに灯が消える: 柔らかい下降と低い余韻 (唸り・不協和なし)
                    b = Buf(2.6f);
                    Tone(b, 0f, 659f, 659f, 1.8f, "sine", 0.02f, 0.7f, 0.24f);
                    Tone(b, 0.32f, 523f, 523f, 1.6f, "sine", 0.02f, 0.7f, 0.22f);
                    Tone(b, 0.64f, 440f, 440f, 1.4f, "sine", 0.02f, 0.7f, 0.2f);
                    Tone(b, 0.3f, 220f, 220f, 2.2f, "tri", 0.3f, 1.5f, 0.12f);
                    return Finish(name, b, 0.7f);
                default:
                    b = Buf(0.1f);
                    Tone(b, 0f, 600f, 600f, 0.08f, "square", 0.002f, 0.05f, 0.4f);
                    return Finish(name, b, 0.5f);
            }
        }

        // ---- BGM (8小節ループ) ----

        static readonly float[] Pent = { 0, 3, 5, 7, 10, 12, 15, 17 }; // マイナーペンタトニック (半音)

        static float Hz(float semiFromA2) { return 110f * Mathf.Pow(2f, semiFromA2 / 12f); }

        /// <summary>幕ごとの雰囲気: 1=森の夜 (穏やか)、2=坑道 (暗め・速め)、3=坑底の古代都市 (緊張)。boss は速く重く</summary>
        public static AudioClip Bgm(string name)
        {
            int act = name.Contains("3") ? 3 : name.Contains("2") ? 2 : 1;
            bool boss = name.Contains("boss");
            bool title = name.Contains("title");
            float bpm = title ? 84f : boss ? 132f : act == 1 ? 100f : act == 2 ? 108f : 116f;
            float beat = 60f / bpm;
            int bars = 8;
            float total = bars * 4 * beat;
            var b = Buf(total + 0.05f);
            var rng = new System.Random(name.GetHashCode());
            // コード進行 (ルートの半音: A2 基準) 2小節ずつ
            float[] prog = title ? new float[] { 0, -4, 5, 3 } : act == 1 ? new float[] { 0, 8, 3, 10 } : act == 2 ? new float[] { 0, 6, 8, 3 } : new float[] { 0, 1, 8, 6 };
            for (int bar = 0; bar < bars; bar++)
            {
                float root = prog[(bar / 2) % prog.Length];
                float barAt = bar * 4 * beat;
                // ベース: 1拍目と3拍目 (三角波)
                Tone(b, barAt, Hz(root - 12), Hz(root - 12), beat * 0.9f, "tri", 0.01f, 0.15f, 0.55f);
                Tone(b, barAt + 2 * beat, Hz(root - 12), Hz(root - 12), beat * 0.9f, "tri", 0.01f, 0.15f, 0.5f);
                if (boss) { Tone(b, barAt + beat, Hz(root - 12), Hz(root - 12), beat * 0.45f, "tri", 0.01f, 0.1f, 0.45f); Tone(b, barAt + 3 * beat, Hz(root - 12), Hz(root - 12), beat * 0.45f, "tri", 0.01f, 0.1f, 0.45f); }
                // アルペジオ: 8分音符 (パルス波・小さめ)
                float[] chord = act == 3 || boss ? new float[] { 0, 3, 7, 10 } : new float[] { 0, 3, 7, 12 };
                for (int e = 0; e < 8; e++)
                {
                    float n = root + chord[e % chord.Length] + (e >= 4 ? 12 : 0);
                    Tone(b, barAt + e * beat * 0.5f, Hz(n), Hz(n), beat * 0.45f, "pulse", 0.005f, 0.12f, title ? 0.16f : 0.2f);
                }
                // メロディ: ペンタトニックのランダムウォーク (決定的)
                if (!title || bar >= 2)
                {
                    int idx = 3 + (int)(rng.NextDouble() * 3);
                    for (int m = 0; m < 4; m++)
                    {
                        if (rng.NextDouble() < 0.3) continue; // 休符
                        idx = Mathf.Clamp(idx + (int)(rng.NextDouble() * 5) - 2, 0, Pent.Length - 1);
                        float n = root + 12 + Pent[idx];
                        float len = rng.NextDouble() < 0.5 ? beat * 0.9f : beat * 0.45f;
                        Tone(b, barAt + m * beat, Hz(n), Hz(n), len, "square", 0.01f, 0.2f, 0.22f);
                    }
                }
                // パーカッション: キック (1,3)・スネア (2,4)・ハット (8分)
                if (!title)
                {
                    Tone(b, barAt, 120f, 40f, 0.12f, "sine", 0.002f, 0.08f, 0.8f);
                    Tone(b, barAt + 2 * beat, 120f, 40f, 0.12f, "sine", 0.002f, 0.08f, 0.8f);
                    Noise(b, barAt + beat, 0.12f, 0.002f, 0.09f, 0.35f, 0.7f, rng);
                    Noise(b, barAt + 3 * beat, 0.12f, 0.002f, 0.09f, 0.35f, 0.7f, rng);
                    for (int h = 0; h < 8; h++) Noise(b, barAt + h * beat * 0.5f, 0.03f, 0.001f, 0.025f, h % 2 == 0 ? 0.12f : 0.08f, 0.95f, rng);
                }
            }
            return Finish(name, b, 0.8f);
        }
    }
}
