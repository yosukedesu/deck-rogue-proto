#!/usr/bin/env python3
"""回復音の自作候補 (2026-09-19 ユーザー「別の候補ほしい」)。numpy だけで合成し、mp3 (試聴) と ogg (採用時) を書く。
   python3 docs/design/audio/heal-synth.py <out_dir>
素材のライセンスに縛られない自前の音。ゲームに入れる時は ffmpeg で ogg に (Resources/Audio/sfx/heal.ogg)。"""
import sys, os, math, subprocess
import numpy as np

SR = 44100
def t(dur): return np.arange(int(SR * dur)) / SR
def env_ad(n, attack, decay, curve=4.0):
    """立ち上がり attack 秒・指数減衰 decay 秒 (時定数) の包絡"""
    x = np.arange(n) / SR
    a = np.clip(x / max(attack, 1e-4), 0, 1)
    d = np.exp(-np.maximum(x - attack, 0) / max(decay, 1e-4) * (curve / 4.0))
    return a * d
def tone(freq, dur, partials=((1, 1.0),), attack=0.005, decay=0.35, vib=0.0):
    x = t(dur); y = np.zeros_like(x)
    for k, (ratio, amp) in enumerate(partials):
        ph = 2 * np.pi * freq * ratio * x
        if vib > 0: ph += vib * np.sin(2 * np.pi * 5.5 * x) * (freq * ratio) / 60.0
        y += amp * np.sin(ph) * np.exp(-x * (0.9 + 0.55 * k) / decay)
    return y * env_ad(len(x), attack, decay * 3)
def place(buf, y, at):
    i = int(at * SR); n = min(len(y), len(buf) - i)
    if n > 0: buf[i:i + n] += y[:n]
def reverb(y, mix=0.22, delays=(0.031, 0.047, 0.071, 0.093), fb=0.35):
    out = y.copy()
    for d in delays:
        n = int(d * SR); z = np.zeros_like(y); acc = y.copy()
        for _ in range(4):
            acc = np.concatenate([np.zeros(n), acc[:-n]]) * fb; z += acc
        out += z * mix / len(delays)
    return out
def norm(y, peak=0.8):
    m = np.max(np.abs(y)) or 1.0
    return y / m * peak
def bell_partials(): return ((1, 1.0), (2.0, 0.45), (2.76, 0.25), (3.5, 0.12), (4.2, 0.06))
def soft_partials(): return ((1, 1.0), (2, 0.35), (3, 0.12), (4, 0.05))
NOTE = lambda n: 440.0 * 2 ** ((n - 69) / 12.0)

def sparkle_up():
    """上昇のきらめき: ペンタトニックの鈴 5 音 (C5 E5 G5 C6 E6) を 70ms 刻みで、余韻と薄いシマー"""
    buf = np.zeros(int(SR * 1.3))
    for i, n in enumerate([72, 76, 79, 84, 88]):
        place(buf, tone(NOTE(n), 0.9, bell_partials(), 0.004, 0.32) * (0.9 - i * 0.08), 0.07 * i)
    x = t(1.3); shimmer = np.random.default_rng(3).normal(0, 1, len(x))
    shimmer = np.diff(shimmer, prepend=0) * 0.012 * np.exp(-x * 3.5) * (x > 0.25)
    return norm(reverb(buf + shimmer, 0.28))
def warm_chord():
    """温かい和音の膨らみ: Cmaj9 を柔らかい倍音でゆっくり膨らませて消す"""
    dur = 1.25; x = t(dur); buf = np.zeros(len(x))
    for n, a in [(60, 0.7), (64, 0.55), (67, 0.5), (71, 0.35), (74, 0.3), (79, 0.18)]:
        for r, amp in soft_partials():
            buf += a * amp * np.sin(2 * np.pi * NOTE(n) * r * x + 0.4 * np.sin(2 * np.pi * 4.5 * x))
    envl = (np.clip(x / 0.28, 0, 1) ** 1.6) * np.exp(-np.maximum(x - 0.45, 0) / 0.32)
    top = tone(NOTE(91), 0.8, bell_partials(), 0.003, 0.3) * 0.35
    place(buf := buf * envl, top, 0.3)
    return norm(reverb(buf, 0.3))
def bell_glow():
    """鈴の余韻: 柔らかい鈴 1 音 (E5→高いオクターブの余韻) の下で光が膨らむ (200→400Hz の正弦のうねり)"""
    dur = 1.2; x = t(dur)
    glow = 0.35 * np.sin(2 * np.pi * (200 + 200 * np.clip(x / 0.5, 0, 1)) * x) * np.clip(x / 0.2, 0, 1) * np.exp(-np.maximum(x - 0.3, 0) / 0.35)
    buf = glow.copy()
    place(buf, tone(NOTE(76), 1.0, bell_partials(), 0.003, 0.4), 0.05)
    place(buf, tone(NOTE(88), 0.9, bell_partials(), 0.003, 0.33) * 0.45, 0.2)
    return norm(reverb(buf, 0.3))
def rain_of_light():
    """光の粒: 高い小さな粒 12 個が降りながら消える (2〜5kHz)、下に薄い和音"""
    rng = np.random.default_rng(11); dur = 1.15; buf = np.zeros(int(SR * dur)); x = t(dur)
    for i in range(12):
        f = NOTE(rng.integers(91, 103)) ; at = 0.05 + i * 0.07 + rng.uniform(0, 0.02)
        place(buf, tone(f, 0.35, ((1, 1.0), (2, 0.2)), 0.002, 0.09) * (0.55 - i * 0.03), at)
    pad = sum(0.18 * np.sin(2 * np.pi * NOTE(n) * x) for n in (64, 67, 71)) * np.clip(x / 0.3, 0, 1) * np.exp(-np.maximum(x - 0.4, 0) / 0.3)
    return norm(reverb(buf + pad, 0.32))
def harp_gliss():
    """ハープのグリッサンド: 8 音を 45ms 刻みで駆け上がり、最後の音だけ長く"""
    buf = np.zeros(int(SR * 1.3))
    notes = [67, 69, 71, 72, 74, 76, 79, 84]
    for i, n in enumerate(notes):
        last = i == len(notes) - 1
        place(buf, tone(NOTE(n), 0.9 if last else 0.4, ((1, 1.0), (2, 0.5), (3, 0.22), (4, 0.1)), 0.002, 0.42 if last else 0.14) * (0.7 if not last else 1.0), 0.045 * i)
    return norm(reverb(buf, 0.26))
def breath_chime():
    """息を吸って光る: 柔らかい風 (帯域ノイズの上昇) の後に明るい鈴 2 音"""
    dur = 1.25; x = t(dur); rng = np.random.default_rng(5)
    noise = rng.normal(0, 1, len(x))
    # 簡易バンドパス: 差分 (高域) と移動平均 (低域) の組み合わせ
    hp = np.diff(noise, prepend=0); k = 9; lp = np.convolve(hp, np.ones(k) / k, mode='same')
    wind = lp * (np.clip(x / 0.45, 0, 1) ** 2) * np.exp(-np.maximum(x - 0.5, 0) / 0.12) * 0.5
    buf = wind.copy()
    place(buf, tone(NOTE(81), 0.8, bell_partials(), 0.003, 0.3), 0.48)
    place(buf, tone(NOTE(88), 0.8, bell_partials(), 0.003, 0.33) * 0.7, 0.6)
    return norm(reverb(buf, 0.3))

def write(out, name, y):
    import wave
    wav = os.path.join(out, name + '.wav')
    with wave.open(wav, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((np.clip(y, -1, 1) * 32767).astype('<i2').tobytes())
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', wav, '-b:a', '96k', os.path.join(out, name + '.mp3')], check=True)
    return wav

if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else '.'
    os.makedirs(out, exist_ok=True)
    for name, fn in [('syn_heal_sparkle_up', sparkle_up), ('syn_heal_warm_chord', warm_chord), ('syn_heal_bell_glow', bell_glow),
                     ('syn_heal_rain_of_light', rain_of_light), ('syn_heal_harp_gliss', harp_gliss), ('syn_heal_breath_chime', breath_chime)]:
        print(write(out, name, fn()))
