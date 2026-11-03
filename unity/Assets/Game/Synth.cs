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

        // ---- 2026-11-04 束 D (音): 合成の補助。音の肌「静かで物悲しい・夜の坑」= ノイズは帯域を絞り、金属は倍音 2 本、打撃は低域の量で段を分ける ----

        /// <summary>包絡 (attack で立ち上がり release で消える)</summary>
        static float Env(float ts, float dur, float attack, float release)
        {
            float env = 1f;
            if (attack > 0f && ts < attack) env = ts / attack;
            float rel = dur - ts;
            if (release > 0f && rel < release) env *= Mathf.Max(0f, rel / release);
            return env;
        }

        /// <summary>帯域ノイズの掃き: ローパスの係数を lp0→lp1 へ滑らせ (大きいほど明るい)、hp>0 なら低域を引く (紙の swish・火の立ち上がり・崩れ)</summary>
        static void NoiseSweep(float[] buf, float at, float dur, float attack, float release, float vol, float lp0, float lp1, float hp, System.Random rng)
        {
            int start = Mathf.Max(0, (int)(at * Rate));
            int n = (int)(dur * Rate);
            float last = 0f, slow = 0f;
            for (int i = 0; i < n && start + i < buf.Length; i++)
            {
                float t = (float)i / n;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = Mathf.Lerp(last, white, Mathf.Lerp(lp0, lp1, t));
                float v = last;
                if (hp > 0f) { slow = Mathf.Lerp(slow, last, hp); v = last - slow; }
                buf[start + i] += v * Env((float)i / Rate, dur, attack, release) * vol;
            }
        }

        /// <summary>風: 低いノイズに遅い揺らぎ (lfoHz) を掛けて dur 秒 (環境音の土台)</summary>
        static void Wind(float[] buf, float dur, float vol, float lowpass, float lfoHz, float lfoDepth, System.Random rng)
        {
            int n = Mathf.Min(buf.Length, (int)(dur * Rate));
            float last = 0f, last2 = 0f;
            float phase = (float)rng.NextDouble();
            for (int i = 0; i < n; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = Mathf.Lerp(last, white, lowpass);
                last2 = Mathf.Lerp(last2, last, lowpass);   // 2 段で丸く
                float ts = (float)i / Rate;
                float lfo = 1f - lfoDepth * 0.5f + lfoDepth * 0.5f * Mathf.Sin((ts * lfoHz + phase) * Mathf.PI * 2f);
                buf[i] += last2 * lfo * vol;
            }
        }

        /// <summary>ループに焼く: 末尾 xfade 秒を先頭に混ぜて切り落とす (継ぎ目が聴こえない)</summary>
        static float[] Loopify(float[] buf, float xfade)
        {
            int n = Mathf.Min((int)(xfade * Rate), buf.Length / 4);
            int len = buf.Length - n;
            var outBuf = new float[len];
            for (int i = 0; i < len; i++)
            {
                if (i < n) { float k = (float)i / n; outBuf[i] = buf[i] * k + buf[len + i] * (1f - k); }
                else outBuf[i] = buf[i];
            }
            return outBuf;
        }

        /// <summary>金属の粒: 基音と倍音 1 本の短い鈴 (硝貨・着地・刻み)</summary>
        static void Grain(float[] buf, float at, float f, float dur, float vol, float overtone = 2f, float overtoneVol = 0.35f)
        {
            Tone(buf, at, f, f, dur, "sine", 0.002f, dur * 0.8f, vol);
            Tone(buf, at, f * overtone, f * overtone, dur * 0.7f, "sine", 0.002f, dur * 0.6f, vol * overtoneVol);
        }

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
                case "light_gain":
                    // 灯を得た (2026-09-20 灯籠): 小さな鈴。heal の系より高く短い
                    b = Buf(0.42f);
                    Tone(b, 0f, 1760f, 1760f, 0.32f, "sine", 0.003f, 0.24f, 0.4f);
                    Tone(b, 0f, 2637f, 2637f, 0.18f, "sine", 0.003f, 0.12f, 0.16f);
                    Tone(b, 0.05f, 2217f, 2217f, 0.26f, "tri", 0.005f, 0.18f, 0.12f);
                    return Finish(name, b, 0.55f);
                case "light_burst":
                    // 放出 (2026-09-20 灯籠): 炎が抜ける吹き音＝上へ抜けるノイズと鈴の残り
                    b = Buf(0.5f);
                    Noise(b, 0f, 0.42f, 0.01f, 0.3f, 0.35f, 0.7f, rng);
                    Tone(b, 0f, 600f, 2400f, 0.3f, "sine", 0.005f, 0.2f, 0.35f);
                    Tone(b, 0.1f, 1760f, 1760f, 0.3f, "sine", 0.005f, 0.22f, 0.2f);
                    return Finish(name, b, 0.7f);
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

                // ---- 2026-11-04 束 D: 契約の鍵の合成 (docs/design/audio/synth-keys-2026-11-04.md)。素材が来たら Resources/Audio/sfx/<name> を置けばそちらが勝つ ----

                // 当たりの 3 段: ノイズの短い打撃を低域の量で分ける (小 = 乾いた紙の打撃・中 = 胴が鳴る・大 = 床が震える)
                case "hit_tier1":
                    b = Buf(0.18f);
                    Noise(b, 0f, 0.08f, 0.002f, 0.05f, 0.6f, 0.5f, rng);
                    Tone(b, 0f, 220f, 120f, 0.08f, "sine", 0.002f, 0.05f, 0.45f);
                    return Finish(name, b, 0.6f);
                case "hit_tier2":
                    b = Buf(0.28f);
                    Noise(b, 0f, 0.12f, 0.002f, 0.08f, 0.7f, 0.55f, rng);
                    Tone(b, 0f, 170f, 70f, 0.16f, "sine", 0.002f, 0.1f, 0.8f);
                    Tone(b, 0f, 260f, 120f, 0.06f, "square", 0.002f, 0.04f, 0.15f);
                    return Finish(name, b, 0.75f);
                case "hit_tier3":
                    b = Buf(0.45f);
                    Noise(b, 0f, 0.22f, 0.002f, 0.16f, 0.8f, 0.45f, rng);
                    Tone(b, 0f, 130f, 38f, 0.3f, "sine", 0.002f, 0.2f, 1f);
                    Tone(b, 0f, 240f, 80f, 0.1f, "square", 0.002f, 0.06f, 0.3f);
                    Noise(b, 0.06f, 0.2f, 0.01f, 0.16f, 0.3f, 0.2f, rng);
                    return Finish(name, b, 0.85f);
                case "finish_blow":
                    // とどめ: 低い打撃 + 高い裂け
                    b = Buf(0.6f);
                    Tone(b, 0f, 110f, 30f, 0.4f, "sine", 0.002f, 0.28f, 1f);
                    NoiseSweep(b, 0f, 0.25f, 0.002f, 0.18f, 0.6f, 0.9f, 0.3f, 0.6f, rng);
                    Tone(b, 0f, 2400f, 900f, 0.12f, "saw", 0.002f, 0.09f, 0.2f);
                    Noise(b, 0.02f, 0.3f, 0.01f, 0.25f, 0.4f, 0.3f, rng);
                    return Finish(name, b, 0.9f);
                case "enemy_down":
                    // 敵が崩れる: 砂がこぼれるノイズの下降 + 低い胴 + 砕けた粒
                    b = Buf(0.6f);
                    NoiseSweep(b, 0f, 0.45f, 0.02f, 0.3f, 0.7f, 0.3f, 0.1f, 0f, rng);
                    Tone(b, 0f, 160f, 60f, 0.35f, "sine", 0.005f, 0.25f, 0.4f);
                    Noise(b, 0.08f, 0.02f, 0.002f, 0.015f, 0.4f, 0.8f, rng);
                    Noise(b, 0.15f, 0.02f, 0.002f, 0.015f, 0.3f, 0.8f, rng);
                    Noise(b, 0.24f, 0.015f, 0.002f, 0.012f, 0.25f, 0.8f, rng);
                    return Finish(name, b, 0.7f);
                case "enemy_down_boss":
                    b = Buf(1.3f);
                    Tone(b, 0f, 80f, 25f, 0.9f, "sine", 0.002f, 0.6f, 1f);
                    NoiseSweep(b, 0f, 0.9f, 0.02f, 0.6f, 0.7f, 0.25f, 0.08f, 0f, rng);
                    Tone(b, 0.05f, 55f, 55f, 0.8f, "tri", 0.02f, 0.6f, 0.3f);
                    Noise(b, 0.12f, 0.03f, 0.002f, 0.02f, 0.4f, 0.8f, rng);
                    Noise(b, 0.3f, 0.025f, 0.002f, 0.02f, 0.3f, 0.8f, rng);
                    Noise(b, 0.55f, 0.02f, 0.002f, 0.015f, 0.2f, 0.8f, rng);
                    return Finish(name, b, 0.85f);

                // 硝貨と魔素: 短い金属の粒 (倍音 2 本・減衰 60ms)
                case "coin_fly":
                    b = Buf(0.12f);
                    Tone(b, 0f, 2400f, 3000f, 0.06f, "sine", 0.002f, 0.045f, 0.5f);
                    Tone(b, 0f, 3600f, 3600f, 0.05f, "tri", 0.002f, 0.04f, 0.2f);
                    return Finish(name, b, 0.5f);
                case "coin_land":
                    b = Buf(0.1f);
                    Grain(b, 0f, 2600f, 0.06f, 0.6f, 2f, 0.4f);
                    return Finish(name, b, 0.55f);
                case "mana_land":
                    b = Buf(0.16f);
                    Tone(b, 0f, 1320f, 1480f, 0.1f, "sine", 0.003f, 0.08f, 0.5f);
                    Tone(b, 0f, 2640f, 2640f, 0.08f, "tri", 0.003f, 0.06f, 0.2f);
                    return Finish(name, b, 0.5f);

                // 勝利の帯: 旋律を持たない和音 1 つ (ジングルは鳴らさない裁定)
                case "victory_band":
                    b = Buf(2.0f);
                    float[] band = { 262f, 330f, 392f, 523f, 587f };
                    for (int i = 0; i < band.Length; i++) Tone(b, i * 0.02f, band[i], band[i], 1.7f, "sine", 0.05f, 1.2f, 0.28f);
                    Tone(b, 0f, 131f, 131f, 1.8f, "tri", 0.1f, 1.2f, 0.14f);
                    return Finish(name, b, 0.6f);
                case "tally_tick":
                    // 戦いの記録の 1 行: 紙を置く粒
                    b = Buf(0.08f);
                    Tone(b, 0f, 1800f, 1800f, 0.06f, "tri", 0.002f, 0.045f, 0.5f);
                    Noise(b, 0f, 0.015f, 0.001f, 0.012f, 0.3f, 0.8f, rng);
                    return Finish(name, b, 0.5f);
                case "lethal_mark":
                    // 倒せる印が点く: 細い鈴 1 つ
                    b = Buf(0.35f);
                    Tone(b, 0f, 880f, 880f, 0.04f, "square", 0.002f, 0.03f, 0.15f);
                    Tone(b, 0f, 1760f, 1760f, 0.26f, "sine", 0.003f, 0.2f, 0.4f);
                    Tone(b, 0f, 2637f, 2637f, 0.18f, "sine", 0.003f, 0.14f, 0.2f);
                    return Finish(name, b, 0.55f);
                case "preview_tick":
                    // 予測の帯が出る: 小さな紙の音
                    b = Buf(0.07f);
                    Noise(b, 0f, 0.04f, 0.002f, 0.03f, 0.5f, 0.45f, rng);
                    Tone(b, 0f, 1200f, 900f, 0.04f, "sine", 0.002f, 0.03f, 0.2f);
                    return Finish(name, b, 0.4f);

                // チェイン: 歯車のラチェット (めくる) と当たりの上昇 (段ごとの音程は KeyPitched が掛ける)
                case "cascade_flip":
                    b = Buf(0.2f);
                    Tone(b, 0f, 1200f, 1200f, 0.045f, "tri", 0.002f, 0.03f, 0.5f);
                    Tone(b, 0.06f, 1400f, 1400f, 0.045f, "tri", 0.002f, 0.03f, 0.5f);
                    Tone(b, 0.12f, 1600f, 1600f, 0.045f, "tri", 0.002f, 0.03f, 0.5f);
                    Noise(b, 0f, 0.01f, 0.001f, 0.008f, 0.3f, 0.9f, rng);
                    Noise(b, 0.06f, 0.01f, 0.001f, 0.008f, 0.3f, 0.9f, rng);
                    Noise(b, 0.12f, 0.01f, 0.001f, 0.008f, 0.3f, 0.9f, rng);
                    return Finish(name, b, 0.6f);
                case "cascade_hit":
                    b = Buf(0.25f);
                    Tone(b, 0f, 1600f, 1600f, 0.04f, "tri", 0.002f, 0.03f, 0.4f);
                    Tone(b, 0.02f, 600f, 1200f, 0.12f, "square", 0.005f, 0.08f, 0.3f);
                    Tone(b, 0.02f, 300f, 150f, 0.1f, "sine", 0.002f, 0.07f, 0.3f);
                    return Finish(name, b, 0.7f);

                // 猛り火: 点く (ノイズが明るくなりながら炎が立つ) / 消える (下降と短い息)
                case "blaze_ignite":
                    b = Buf(0.5f);
                    NoiseSweep(b, 0f, 0.42f, 0.05f, 0.2f, 0.6f, 0.2f, 0.7f, 0f, rng);
                    Tone(b, 0.02f, 200f, 900f, 0.32f, "saw", 0.03f, 0.15f, 0.25f);
                    return Finish(name, b, 0.75f);
                case "blaze_out":
                    b = Buf(0.45f);
                    Tone(b, 0f, 600f, 200f, 0.35f, "sine", 0.005f, 0.25f, 0.4f);
                    Noise(b, 0f, 0.2f, 0.02f, 0.15f, 0.3f, 0.3f, rng);
                    return Finish(name, b, 0.5f);
                case "aether_gain":
                    // 霊気: 上へ抜ける硝子の鈴
                    b = Buf(0.35f);
                    Tone(b, 0f, 880f, 1760f, 0.25f, "sine", 0.005f, 0.18f, 0.4f);
                    Tone(b, 0.04f, 1320f, 2640f, 0.2f, "sine", 0.005f, 0.15f, 0.2f);
                    Tone(b, 0.08f, 3520f, 3520f, 0.1f, "tri", 0.003f, 0.08f, 0.1f);
                    return Finish(name, b, 0.5f);
                case "combo_up":
                    // 多段の積み上がり: 短い鈴 (段ごとの音程は KeyPitched が掛ける)
                    b = Buf(0.12f);
                    Tone(b, 0f, 1320f, 1320f, 0.08f, "tri", 0.002f, 0.06f, 0.5f);
                    Tone(b, 0f, 2640f, 2640f, 0.05f, "sine", 0.002f, 0.04f, 0.2f);
                    return Finish(name, b, 0.5f);
                case "danger_in":
                    // HP 3 割を割った: 低い一打と沈む音
                    b = Buf(0.9f);
                    Tone(b, 0f, 90f, 45f, 0.35f, "sine", 0.002f, 0.25f, 1f);
                    Tone(b, 0.05f, 440f, 220f, 0.6f, "sine", 0.05f, 0.4f, 0.25f);
                    Noise(b, 0f, 0.15f, 0.002f, 0.1f, 0.3f, 0.3f, rng);
                    return Finish(name, b, 0.75f);
                case "defeat_fall":
                    // 倒れる: 落下の下降と床の鈍い音 (death の変種)
                    b = Buf(0.9f);
                    Tone(b, 0f, 220f, 50f, 0.7f, "sine", 0.01f, 0.4f, 0.8f);
                    Noise(b, 0f, 0.3f, 0.05f, 0.2f, 0.5f, 0.3f, rng);
                    Tone(b, 0.55f, 60f, 40f, 0.25f, "sine", 0.002f, 0.18f, 0.6f);
                    Noise(b, 0.55f, 0.08f, 0.002f, 0.06f, 0.4f, 0.35f, rng);
                    return Finish(name, b, 0.8f);
                case "heartbeat":
                    // 鼓動: 2 拍の低い太鼓 (Audio.Heartbeat が rate の間隔で打つ)
                    b = Buf(0.5f);
                    Tone(b, 0f, 70f, 45f, 0.12f, "sine", 0.002f, 0.08f, 0.9f);
                    Tone(b, 0.16f, 70f, 45f, 0.12f, "sine", 0.002f, 0.09f, 0.7f);
                    return Finish(name, b, 0.8f);

                // 札の手触り: 持ち上げる (紙が擦れて上がる) / 戻す (下がる) / エナジーを払う (玉が沈む)
                case "card_lift":
                    b = Buf(0.12f);
                    NoiseSweep(b, 0f, 0.1f, 0.005f, 0.06f, 0.5f, 0.3f, 0.6f, 0.3f, rng);
                    Tone(b, 0f, 700f, 1000f, 0.06f, "sine", 0.003f, 0.04f, 0.15f);
                    return Finish(name, b, 0.45f);
                case "card_cancel":
                    b = Buf(0.14f);
                    NoiseSweep(b, 0f, 0.12f, 0.005f, 0.07f, 0.5f, 0.6f, 0.25f, 0.3f, rng);
                    Tone(b, 0f, 900f, 550f, 0.08f, "sine", 0.003f, 0.06f, 0.15f);
                    return Finish(name, b, 0.45f);
                case "energy_spend":
                    b = Buf(0.18f);
                    Tone(b, 0f, 1400f, 700f, 0.14f, "sine", 0.003f, 0.1f, 0.5f);
                    Tone(b, 0f, 2100f, 2100f, 0.04f, "tri", 0.002f, 0.03f, 0.15f);
                    return Finish(name, b, 0.5f);
                case "enemy_telegraph":
                    // 敵の意図が実行の直前に脈打つ: 旧 buff の静かな版 (予告に留める)
                    b = Buf(0.3f);
                    Tone(b, 0f, 300f, 600f, 0.22f, "pulse", 0.02f, 0.12f, 0.35f);
                    Tone(b, 0f, 150f, 150f, 0.2f, "sine", 0.02f, 0.12f, 0.2f);
                    return Finish(name, b, 0.5f);
                case "trap_fire_big":
                    // からくりの発動の山: 発条が弾けて床が鳴る
                    b = Buf(0.6f);
                    Tone(b, 0f, 1200f, 400f, 0.1f, "square", 0.002f, 0.06f, 0.35f);
                    Tone(b, 0.01f, 2400f, 2400f, 0.25f, "sine", 0.002f, 0.2f, 0.25f);
                    Noise(b, 0f, 0.08f, 0.002f, 0.05f, 0.4f, 0.9f, rng);
                    Tone(b, 0.03f, 120f, 40f, 0.3f, "sine", 0.002f, 0.2f, 0.8f);
                    return Finish(name, b, 0.85f);
                case "impulse_burn":
                    // 衝動の失効・くべる: 紙が燃え落ちる (burn の系・短い)
                    b = Buf(0.4f);
                    NoiseSweep(b, 0f, 0.35f, 0.01f, 0.2f, 0.6f, 0.5f, 0.15f, 0.2f, rng);
                    Tone(b, 0f, 300f, 700f, 0.18f, "saw", 0.01f, 0.1f, 0.15f);
                    Tone(b, 0.05f, 500f, 250f, 0.25f, "sine", 0.01f, 0.18f, 0.15f);
                    return Finish(name, b, 0.6f);

                // 画面: 報酬・遺物・鍛える・休む・買う・判・合成・幕・着地・出来事・地図・ボタン
                case "card_flip":
                    b = Buf(0.14f);
                    NoiseSweep(b, 0f, 0.1f, 0.003f, 0.06f, 0.6f, 0.4f, 0.8f, 0.4f, rng);
                    Tone(b, 0f, 900f, 1400f, 0.06f, "tri", 0.003f, 0.04f, 0.2f);
                    return Finish(name, b, 0.5f);
                case "rare_reveal":
                    // R が出た: 高い和音の煌めき (旋律は持たない)
                    b = Buf(1.2f);
                    float[] rare = { 1047f, 1319f, 1568f, 2093f };
                    for (int i = 0; i < rare.Length; i++) Tone(b, i * 0.05f, rare[i], rare[i], 0.9f - i * 0.1f, "sine", 0.01f, 0.6f, 0.25f);
                    Tone(b, 0.1f, 3136f, 3136f, 0.5f, "tri", 0.01f, 0.4f, 0.08f);
                    NoiseSweep(b, 0f, 0.3f, 0.02f, 0.2f, 0.15f, 0.9f, 0.9f, 0.8f, rng);
                    return Finish(name, b, 0.6f);
                case "upgrade_shine":
                    // 鍛えた: 金床の一打の後に細い鈴
                    b = Buf(0.6f);
                    Tone(b, 0f, 1047f, 1047f, 0.03f, "square", 0.002f, 0.02f, 0.15f);
                    Noise(b, 0f, 0.03f, 0.001f, 0.02f, 0.3f, 0.95f, rng);
                    Tone(b, 0.01f, 2093f, 2093f, 0.45f, "sine", 0.003f, 0.35f, 0.35f);
                    Tone(b, 0.03f, 3136f, 3136f, 0.3f, "sine", 0.003f, 0.24f, 0.18f);
                    return Finish(name, b, 0.55f);
                case "rest_heal":
                    // 休んだ・HP の数え上げの刻み: 温かい小さな音
                    b = Buf(0.14f);
                    Tone(b, 0f, 880f, 880f, 0.1f, "sine", 0.003f, 0.08f, 0.45f);
                    Tone(b, 0f, 1320f, 1320f, 0.06f, "sine", 0.003f, 0.05f, 0.15f);
                    return Finish(name, b, 0.45f);
                case "sold_stamp":
                    // 売切の判: 紙に判を押す鈍い音
                    b = Buf(0.25f);
                    Noise(b, 0f, 0.06f, 0.002f, 0.04f, 0.7f, 0.35f, rng);
                    Tone(b, 0f, 180f, 90f, 0.15f, "sine", 0.002f, 0.1f, 0.6f);
                    Tone(b, 0f, 400f, 200f, 0.03f, "square", 0.002f, 0.02f, 0.2f);
                    return Finish(name, b, 0.7f);
                case "fuse_melt":
                    // 合成: 溶けて沈み、落ち着いた所で鈴 1 つ
                    b = Buf(0.8f);
                    NoiseSweep(b, 0f, 0.6f, 0.05f, 0.3f, 0.5f, 0.7f, 0.15f, 0f, rng);
                    Tone(b, 0f, 900f, 220f, 0.55f, "sine", 0.02f, 0.3f, 0.35f);
                    Tone(b, 0f, 440f, 110f, 0.6f, "tri", 0.02f, 0.3f, 0.2f);
                    Tone(b, 0.5f, 1320f, 1320f, 0.25f, "sine", 0.005f, 0.2f, 0.2f);
                    return Finish(name, b, 0.65f);
                case "paper_swish":
                    // 幕・入り: 紙を掃く帯域ノイズ (一周で 10 回以上鳴るので短く小さく)
                    b = Buf(0.24f);
                    NoiseSweep(b, 0f, 0.22f, 0.03f, 0.12f, 0.6f, 0.5f, 0.9f, 0.3f, rng);
                    return Finish(name, b, 0.45f);
                case "land_card":
                    b = Buf(0.07f);
                    Tone(b, 0f, 1320f, 1320f, 0.05f, "tri", 0.002f, 0.04f, 0.5f);
                    Noise(b, 0f, 0.02f, 0.001f, 0.015f, 0.3f, 0.6f, rng);
                    return Finish(name, b, 0.45f);
                case "land_relic":
                    b = Buf(0.16f);
                    Tone(b, 0f, 1500f, 1450f, 0.1f, "sine", 0.002f, 0.08f, 0.4f);
                    Tone(b, 0f, 3000f, 3000f, 0.06f, "sine", 0.002f, 0.05f, 0.15f);
                    Noise(b, 0f, 0.015f, 0.001f, 0.012f, 0.35f, 0.9f, rng);
                    return Finish(name, b, 0.5f);
                case "event_gain":
                    b = Buf(0.4f);
                    Tone(b, 0f, 660f, 660f, 0.15f, "tri", 0.005f, 0.1f, 0.4f);
                    Tone(b, 0.1f, 990f, 990f, 0.22f, "tri", 0.005f, 0.15f, 0.4f);
                    return Finish(name, b, 0.55f);
                case "event_loss":
                    b = Buf(0.45f);
                    Tone(b, 0f, 660f, 660f, 0.15f, "tri", 0.005f, 0.1f, 0.4f);
                    Tone(b, 0.1f, 440f, 440f, 0.28f, "tri", 0.005f, 0.2f, 0.4f);
                    Tone(b, 0.1f, 220f, 220f, 0.3f, "tri", 0.02f, 0.2f, 0.15f);
                    return Finish(name, b, 0.55f);
                case "map_arrive":
                    // 地図に戻って着いた: 低い木の音
                    b = Buf(0.35f);
                    Tone(b, 0f, 220f, 220f, 0.25f, "tri", 0.01f, 0.18f, 0.4f);
                    Tone(b, 0.02f, 330f, 330f, 0.2f, "tri", 0.01f, 0.15f, 0.25f);
                    Noise(b, 0f, 0.06f, 0.002f, 0.04f, 0.3f, 0.3f, rng);
                    return Finish(name, b, 0.5f);
                case "button_appear":
                    b = Buf(0.09f);
                    Tone(b, 0f, 990f, 990f, 0.06f, "tri", 0.002f, 0.045f, 0.45f);
                    Noise(b, 0f, 0.03f, 0.001f, 0.02f, 0.3f, 0.5f, rng);
                    return Finish(name, b, 0.45f);
                case "boss_start":
                    // 幕ボスの登場: 低い太鼓 + 息 + 低い持続 (通常戦の甲冑と分ける)
                    b = Buf(1.4f);
                    Tone(b, 0f, 90f, 35f, 0.9f, "sine", 0.005f, 0.5f, 1f);
                    Noise(b, 0f, 0.4f, 0.01f, 0.3f, 0.6f, 0.3f, rng);
                    Tone(b, 0f, 110f, 110f, 1.2f, "square", 0.2f, 0.6f, 0.15f);
                    Tone(b, 0.15f, 55f, 55f, 0.9f, "sine", 0.05f, 0.6f, 0.3f);
                    return Finish(name, b, 0.85f);

                // 環境音 (8 秒ループ。Audio.Ambience が loop で鳴らす。ループの継ぎ目は Loopify が焼く)
                case "amb_forest_night":
                    return Finish(name, AmbForestNight(rng), 0.7f);
                case "amb_campfire":
                    return Finish(name, AmbCampfire(rng), 0.7f);
                case "amb_cave":
                    return Finish(name, AmbCave(rng), 0.7f);
                case "amb_ruins":
                    return Finish(name, AmbRuins(rng), 0.7f);
                default:
                    b = Buf(0.1f);
                    Tone(b, 0f, 600f, 600f, 0.08f, "square", 0.002f, 0.05f, 0.4f);
                    return Finish(name, b, 0.5f);
            }
        }

        // ---- 環境音 (2026-11-04 束 D)。8.5 秒を作って末尾 0.5 秒を先頭に焼き 8 秒のループに ----

        const float AmbLen = 8.5f, AmbXfade = 0.5f;

        /// <summary>幕1 の夜の森: 低い風 (0.2Hz の揺らぎ) に虫の高い粒がまばら (3〜5 連の短い鳴き・2 種の高さ)</summary>
        static float[] AmbForestNight(System.Random rng)
        {
            var b = Buf(AmbLen);
            Wind(b, AmbLen, 0.5f, 0.08f, 0.2f, 0.6f, rng);
            for (int g = 0; g < 11; g++)
            {
                float at = (float)rng.NextDouble() * (AmbLen - 0.4f);
                float f = rng.NextDouble() < 0.5 ? 4200f : 5600f;
                int pulses = 3 + (int)(rng.NextDouble() * 3);
                float vol = 0.08f + (float)rng.NextDouble() * 0.08f;   // 遠近
                for (int p = 0; p < pulses; p++) Tone(b, at + p * 0.045f, f, f * 0.98f, 0.03f, "sine", 0.004f, 0.02f, vol);
            }
            return Loopify(b, AmbXfade);
        }

        /// <summary>焚き火: ノイズの低い揺らぎ (0.4Hz) + ランダムな短い爆ぜ + 時々の低い弾け</summary>
        static float[] AmbCampfire(System.Random rng)
        {
            var b = Buf(AmbLen);
            Wind(b, AmbLen, 0.45f, 0.35f, 0.4f, 0.5f, rng);
            for (int c = 0; c < 26; c++)
            {
                float at = (float)rng.NextDouble() * (AmbLen - 0.1f);
                float dur = 0.008f + (float)rng.NextDouble() * 0.014f;
                Noise(b, at, dur, 0.001f, dur * 0.7f, 0.25f + (float)rng.NextDouble() * 0.3f, 0.9f, rng);
            }
            for (int p = 0; p < 5; p++)
            {
                float at = (float)rng.NextDouble() * (AmbLen - 0.1f);
                Tone(b, at, 120f, 60f, 0.03f, "sine", 0.001f, 0.02f, 0.3f);
            }
            return Loopify(b, AmbXfade);
        }

        /// <summary>幕2 の坑道: 水滴 (短い下降と 2 回の残響) + 低い残響の唸り + ごく薄い風</summary>
        static float[] AmbCave(System.Random rng)
        {
            var b = Buf(AmbLen);
            Wind(b, AmbLen, 0.18f, 0.03f, 0.13f, 0.5f, rng);
            Tone(b, 0f, 55f, 55f, AmbLen, "tri", 0.5f, 0.5f, 0.06f);
            for (int d = 0; d < 6; d++)
            {
                float at = (float)rng.NextDouble() * (AmbLen - 0.5f);
                float f = 1500f + (float)rng.NextDouble() * 700f;
                Tone(b, at, f, f * 0.55f, 0.06f, "sine", 0.002f, 0.045f, 0.3f);
                Tone(b, at + 0.13f, f * 0.9f, f * 0.5f, 0.07f, "sine", 0.004f, 0.055f, 0.12f);
                Tone(b, at + 0.27f, f * 0.8f, f * 0.45f, 0.08f, "sine", 0.006f, 0.065f, 0.05f);
            }
            return Loopify(b, AmbXfade);
        }

        /// <summary>幕3 の古代都市: 風 (0.15Hz) + 低い唸り (わずかに外した 2 本) + 遠い金属の残り</summary>
        static float[] AmbRuins(System.Random rng)
        {
            var b = Buf(AmbLen);
            Wind(b, AmbLen, 0.4f, 0.1f, 0.15f, 0.6f, rng);
            Tone(b, 0f, 41.2f, 41.2f, AmbLen, "saw", 0.6f, 0.6f, 0.05f);
            Tone(b, 0f, 61.7f, 61.9f, AmbLen, "saw", 0.6f, 0.6f, 0.035f);
            for (int m = 0; m < 2; m++)
            {
                float at = (float)rng.NextDouble() * (AmbLen - 1f);
                Tone(b, at, 660f, 655f, 0.8f, "sine", 0.01f, 0.7f, 0.08f);
                Tone(b, at, 1320f, 1310f, 0.5f, "sine", 0.01f, 0.45f, 0.03f);
            }
            return Loopify(b, AmbXfade);
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
