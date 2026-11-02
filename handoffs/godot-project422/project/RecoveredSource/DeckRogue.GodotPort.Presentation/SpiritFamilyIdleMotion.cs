using System;

namespace DeckRogue.GodotPort.Presentation;

public static class SpiritFamilyIdleMotion
{
	public static float PeriodFor(string id)
	{
		return id switch
		{
			"enemy_probe" => 2f, 
			"enemy_iron_clam" => 2.8f, 
			"enemy_maw_hunter" => 1.8f, 
			_ => 0f, 
		};
	}

	public static SpiritProbeIdleMotion.Offsets Sample(string id, float age, bool still = false)
	{
		if (id == "enemy_probe")
		{
			return SpiritProbeIdleMotion.Sample(age, still);
		}
		float period = PeriodFor(id);
		if (still || !float.IsFinite(age) || period <= 0f)
		{
			return default;
		}
		float phase = (age / period - MathF.Floor(age / period)) * ((float)Math.PI * 2f);
		if (id == "enemy_iron_clam")
		{
			float throat = MathF.Sin(phase);
			float lip = MathF.Sin(phase - (float)Math.PI / 6f);
			float gill = MathF.Sin(phase - (float)Math.PI / 3f);
			return new SpiritProbeIdleMotion.Offsets(-0.15f * throat, 0.35f * throat, -0.25f * lip, 0.65f * lip, 0.45f * gill, 0.7f * gill);
		}
		float front = MathF.Sin(phase);
		float rear = MathF.Sin(phase - (float)Math.PI / 3f);
		float lag = MathF.Sin(phase - (float)Math.PI * 2f / 3f);
		return new SpiritProbeIdleMotion.Offsets(0.55f * front, -1.2f * front, 0.8f * rear, -0.9f * rear, 0.35f * lag, 0.55f * lag);
	}
}
