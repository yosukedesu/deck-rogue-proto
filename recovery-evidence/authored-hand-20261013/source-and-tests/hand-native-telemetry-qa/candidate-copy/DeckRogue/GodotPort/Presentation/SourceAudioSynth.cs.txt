using System;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceAudioSynth
{
	public const int Rate = 22050;

	private static readonly float[] Pent = new float[8] { 0f, 3f, 5f, 7f, 10f, 12f, 15f, 17f };

	private static float Osc(string wave, float phase)
	{
		float p = phase - Mathf.Floor(phase);
		return wave switch
		{
			"square" => (p < 0.5f) ? 1f : (-1f), 
			"pulse" => (p < 0.25f) ? 1f : (-1f), 
			"tri" => 1f - 4f * Mathf.Abs(p - 0.5f), 
			"saw" => 2f * p - 1f, 
			_ => Mathf.Sin(p * (float)Math.PI * 2f), 
		};
	}

	private static void Tone(float[] buf, float at, float f0, float f1, float dur, string wave, float attack, float release, float vol, Random? rng = null)
	{
		int start = Mathf.Max(0, (int)(at * 22050f));
		int n = (int)(dur * 22050f);
		float phase = 0f;
		for (int i = 0; i < n && start + i < buf.Length; i++)
		{
			float t = (float)i / (float)n;
			float f2 = Mathf.Lerp(f0, f1, t);
			phase += f2 / 22050f;
			float env = 1f;
			float ts = (float)i / 22050f;
			if (ts < attack)
			{
				env = ts / attack;
			}
			float rel = dur - ts;
			if (rel < release)
			{
				env *= Mathf.Max(0f, rel / release);
			}
			buf[start + i] += Osc(wave, phase) * env * vol;
		}
	}

	private static void Noise(float[] buf, float at, float dur, float attack, float release, float vol, float lowpass, Random rng)
	{
		int start = Mathf.Max(0, (int)(at * 22050f));
		int n = (int)(dur * 22050f);
		float last = 0f;
		for (int i = 0; i < n && start + i < buf.Length; i++)
		{
			float white = (float)(rng.NextDouble() * 2.0 - 1.0);
			last = Mathf.Lerp(last, white, lowpass);
			float ts = (float)i / 22050f;
			float env = 1f;
			if (ts < attack)
			{
				env = ts / attack;
			}
			float rel = dur - ts;
			if (rel < release)
			{
				env *= Mathf.Max(0f, rel / release);
			}
			buf[start + i] += last * env * vol;
		}
	}

	private static AudioStreamWav Finish(string name, float[] buf, float gain = 1f)
	{
		float peak = 0.0001f;
		for (int i = 0; i < buf.Length; i++)
		{
			peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
		}
		float k = Mathf.Min(1f, 0.9f / peak) * gain;
		for (int j = 0; j < buf.Length; j++)
		{
			buf[j] = (float)Math.Tanh(buf[j] * k);
		}
		byte[] bytes = new byte[buf.Length * 2];
		for (int l = 0; l < buf.Length; l++)
		{
			short v = (short)(Math.Clamp(buf[l], -1f, 1f) * 32767f);
			bytes[l * 2] = (byte)(v & 0xFF);
			bytes[l * 2 + 1] = (byte)((v >> 8) & 0xFF);
		}
		return new AudioStreamWav
		{
			Format = AudioStreamWav.FormatEnum.Format16Bits,
			MixRate = 22050,
			Stereo = false,
			Data = bytes
		};
	}

	private static float[] Buf(float seconds)
	{
		return new float[(int)(seconds * 22050f)];
	}

	public static AudioStreamWav Sfx(string name)
	{
		Random rng = new Random(name.GetHashCode());
		switch (name)
		{
		case "card_draw":
		{
			float[] b = Buf(0.16f);
			Noise(b, 0f, 0.14f, 0.005f, 0.08f, 0.5f, 0.35f, rng);
			Tone(b, 0f, 900f, 1500f, 0.08f, "sine", 0.005f, 0.05f, 0.25f);
			return Finish(name, b, 0.7f);
		}
		case "card_play":
		{
			float[] b = Buf(0.28f);
			Noise(b, 0f, 0.25f, 0.01f, 0.15f, 0.6f, 0.25f, rng);
			Tone(b, 0f, 700f, 250f, 0.22f, "sine", 0.01f, 0.12f, 0.3f);
			return Finish(name, b, 0.8f);
		}
		case "card_set":
		{
			float[] b = Buf(0.2f);
			Tone(b, 0f, 520f, 480f, 0.08f, "tri", 0.005f, 0.05f, 0.5f);
			Tone(b, 0.07f, 780f, 720f, 0.1f, "tri", 0.005f, 0.06f, 0.4f);
			return Finish(name, b, 0.7f);
		}
		case "hit":
		{
			float[] b = Buf(0.3f);
			Tone(b, 0f, 180f, 60f, 0.18f, "sine", 0.002f, 0.1f, 0.9f);
			Noise(b, 0f, 0.12f, 0.002f, 0.08f, 0.7f, 0.6f, rng);
			return Finish(name, b, 0.9f);
		}
		case "hit_big":
		{
			float[] b = Buf(0.5f);
			Tone(b, 0f, 140f, 40f, 0.32f, "sine", 0.002f, 0.2f, 1f);
			Tone(b, 0f, 300f, 90f, 0.12f, "square", 0.002f, 0.08f, 0.3f);
			Noise(b, 0f, 0.25f, 0.002f, 0.18f, 0.8f, 0.5f, rng);
			return Finish(name, b);
		}
		case "slash":
		{
			float[] b = Buf(0.22f);
			Noise(b, 0f, 0.2f, 0.002f, 0.12f, 0.8f, 0.8f, rng);
			Tone(b, 0f, 1800f, 600f, 0.12f, "saw", 0.002f, 0.08f, 0.25f);
			return Finish(name, b, 0.8f);
		}
		case "block":
		{
			float[] b = Buf(0.26f);
			Tone(b, 0f, 1200f, 1100f, 0.06f, "square", 0.002f, 0.04f, 0.5f);
			Tone(b, 0.02f, 2400f, 2300f, 0.18f, "sine", 0.002f, 0.14f, 0.35f);
			Noise(b, 0f, 0.05f, 0.002f, 0.03f, 0.4f, 0.9f, rng);
			return Finish(name, b, 0.7f);
		}
		case "heal":
		{
			float[] b = Buf(0.5f);
			Tone(b, 0f, 660f, 660f, 0.12f, "tri", 0.01f, 0.06f, 0.5f);
			Tone(b, 0.12f, 880f, 880f, 0.12f, "tri", 0.01f, 0.06f, 0.5f);
			Tone(b, 0.24f, 1320f, 1320f, 0.24f, "tri", 0.01f, 0.18f, 0.5f);
			return Finish(name, b, 0.7f);
		}
		case "buff":
		{
			float[] b = Buf(0.4f);
			Tone(b, 0f, 300f, 600f, 0.3f, "pulse", 0.01f, 0.15f, 0.5f);
			return Finish(name, b, 0.6f);
		}
		case "turn":
		{
			float[] b = Buf(0.6f);
			Tone(b, 0f, 880f, 880f, 0.5f, "sine", 0.005f, 0.4f, 0.6f);
			Tone(b, 0f, 1760f, 1760f, 0.3f, "sine", 0.005f, 0.25f, 0.2f);
			return Finish(name, b, 0.6f);
		}
		case "enemy_turn":
		{
			float[] b = Buf(0.6f);
			Tone(b, 0f, 220f, 220f, 0.25f, "square", 0.005f, 0.1f, 0.4f);
			Tone(b, 0.22f, 165f, 165f, 0.35f, "square", 0.005f, 0.25f, 0.4f);
			return Finish(name, b, 0.6f);
		}
		case "lunge":
		{
			float[] b = Buf(0.25f);
			Noise(b, 0f, 0.22f, 0.05f, 0.1f, 0.6f, 0.3f, rng);
			Tone(b, 0f, 200f, 500f, 0.2f, "sine", 0.03f, 0.1f, 0.25f);
			return Finish(name, b, 0.7f);
		}
		case "click":
		{
			float[] b = Buf(0.08f);
			Tone(b, 0f, 1000f, 800f, 0.06f, "square", 0.002f, 0.04f, 0.4f);
			return Finish(name, b, 0.5f);
		}
		case "hover":
		{
			float[] b = Buf(0.05f);
			Tone(b, 0f, 1500f, 1700f, 0.04f, "sine", 0.002f, 0.03f, 0.3f);
			return Finish(name, b, 0.35f);
		}
		case "energy":
		{
			float[] b = Buf(0.3f);
			Tone(b, 0f, 700f, 1400f, 0.25f, "sine", 0.005f, 0.15f, 0.5f);
			return Finish(name, b, 0.6f);
		}
		case "light_gain":
		{
			float[] b = Buf(0.42f);
			Tone(b, 0f, 1760f, 1760f, 0.32f, "sine", 0.003f, 0.24f, 0.4f);
			Tone(b, 0f, 2637f, 2637f, 0.18f, "sine", 0.003f, 0.12f, 0.16f);
			Tone(b, 0.05f, 2217f, 2217f, 0.26f, "tri", 0.005f, 0.18f, 0.12f);
			return Finish(name, b, 0.55f);
		}
		case "light_burst":
		{
			float[] b = Buf(0.5f);
			Noise(b, 0f, 0.42f, 0.01f, 0.3f, 0.35f, 0.7f, rng);
			Tone(b, 0f, 600f, 2400f, 0.3f, "sine", 0.005f, 0.2f, 0.35f);
			Tone(b, 0.1f, 1760f, 1760f, 0.3f, "sine", 0.005f, 0.22f, 0.2f);
			return Finish(name, b, 0.7f);
		}
		case "death":
		{
			float[] b = Buf(0.7f);
			Tone(b, 0f, 400f, 60f, 0.6f, "square", 0.005f, 0.4f, 0.5f);
			Noise(b, 0f, 0.5f, 0.01f, 0.4f, 0.5f, 0.3f, rng);
			return Finish(name, b, 0.7f);
		}
		case "win":
		{
			float[] b = Buf(2.4f);
			float[] notes = new float[4] { 523f, 659f, 784f, 1047f };
			for (int i = 0; i < notes.Length; i++)
			{
				Tone(b, (float)i * 0.16f, notes[i], notes[i], 1.6f - (float)i * 0.15f, "sine", 0.008f, 0.55f, 0.32f);
			}
			Tone(b, 0.62f, 1568f, 1568f, 1.5f, "sine", 0.01f, 0.8f, 0.16f);
			Tone(b, 0f, 262f, 262f, 2.3f, "tri", 0.25f, 1.6f, 0.14f);
			Tone(b, 0f, 392f, 392f, 2.3f, "tri", 0.3f, 1.6f, 0.1f);
			return Finish(name, b, 0.7f);
		}
		case "lose":
		{
			float[] b = Buf(2.6f);
			Tone(b, 0f, 659f, 659f, 1.8f, "sine", 0.02f, 0.7f, 0.24f);
			Tone(b, 0.32f, 523f, 523f, 1.6f, "sine", 0.02f, 0.7f, 0.22f);
			Tone(b, 0.64f, 440f, 440f, 1.4f, "sine", 0.02f, 0.7f, 0.2f);
			Tone(b, 0.3f, 220f, 220f, 2.2f, "tri", 0.3f, 1.5f, 0.12f);
			return Finish(name, b, 0.7f);
		}
		default:
		{
			float[] b = Buf(0.1f);
			Tone(b, 0f, 600f, 600f, 0.08f, "square", 0.002f, 0.05f, 0.4f);
			return Finish(name, b, 0.5f);
		}
		}
	}

	private static float Hz(float semiFromA2)
	{
		return 110f * Mathf.Pow(2f, semiFromA2 / 12f);
	}

	public static AudioStreamWav Bgm(string name)
	{
		int act = (name.Contains("3") ? 3 : ((!name.Contains("2")) ? 1 : 2));
		bool boss = name.Contains("boss");
		bool title = name.Contains("title");
		float bpm = (title ? 84f : (boss ? 132f : (act switch
		{
			2 => 108f, 
			1 => 100f, 
			_ => 116f, 
		})));
		float beat = 60f / bpm;
		int bars = 8;
		float total = (float)(bars * 4) * beat;
		float[] b = Buf(total + 0.05f);
		Random rng = new Random(name.GetHashCode());
		float[] prog = ((!title) ? (act switch
		{
			2 => new float[4] { 0f, 6f, 8f, 3f }, 
			1 => new float[4] { 0f, 8f, 3f, 10f }, 
			_ => new float[4] { 0f, 1f, 8f, 6f }, 
		}) : new float[4] { 0f, -4f, 5f, 3f });
		for (int bar = 0; bar < bars; bar++)
		{
			float root = prog[bar / 2 % prog.Length];
			float barAt = (float)(bar * 4) * beat;
			Tone(b, barAt, Hz(root - 12f), Hz(root - 12f), beat * 0.9f, "tri", 0.01f, 0.15f, 0.55f);
			Tone(b, barAt + 2f * beat, Hz(root - 12f), Hz(root - 12f), beat * 0.9f, "tri", 0.01f, 0.15f, 0.5f);
			if (boss)
			{
				Tone(b, barAt + beat, Hz(root - 12f), Hz(root - 12f), beat * 0.45f, "tri", 0.01f, 0.1f, 0.45f);
				Tone(b, barAt + 3f * beat, Hz(root - 12f), Hz(root - 12f), beat * 0.45f, "tri", 0.01f, 0.1f, 0.45f);
			}
			float[] chord = ((!(act == 3 || boss)) ? new float[4] { 0f, 3f, 7f, 12f } : new float[4] { 0f, 3f, 7f, 10f });
			for (int e = 0; e < 8; e++)
			{
				float n = root + chord[e % chord.Length] + (float)((e >= 4) ? 12 : 0);
				Tone(b, barAt + (float)e * beat * 0.5f, Hz(n), Hz(n), beat * 0.45f, "pulse", 0.005f, 0.12f, title ? 0.16f : 0.2f);
			}
			if (!title || bar >= 2)
			{
				int idx = 3 + (int)(rng.NextDouble() * 3.0);
				for (int m = 0; m < 4; m++)
				{
					if (!(rng.NextDouble() < 0.3))
					{
						idx = Mathf.Clamp(idx + (int)(rng.NextDouble() * 5.0) - 2, 0, Pent.Length - 1);
						float n2 = root + 12f + Pent[idx];
						float len = ((rng.NextDouble() < 0.5) ? (beat * 0.9f) : (beat * 0.45f));
						Tone(b, barAt + (float)m * beat, Hz(n2), Hz(n2), len, "square", 0.01f, 0.2f, 0.22f);
					}
				}
			}
			if (!title)
			{
				Tone(b, barAt, 120f, 40f, 0.12f, "sine", 0.002f, 0.08f, 0.8f);
				Tone(b, barAt + 2f * beat, 120f, 40f, 0.12f, "sine", 0.002f, 0.08f, 0.8f);
				Noise(b, barAt + beat, 0.12f, 0.002f, 0.09f, 0.35f, 0.7f, rng);
				Noise(b, barAt + 3f * beat, 0.12f, 0.002f, 0.09f, 0.35f, 0.7f, rng);
				for (int h = 0; h < 8; h++)
				{
					Noise(b, barAt + (float)h * beat * 0.5f, 0.03f, 0.001f, 0.025f, (h % 2 == 0) ? 0.12f : 0.08f, 0.95f, rng);
				}
			}
		}
		return Finish(name, b, 0.8f);
	}
}
