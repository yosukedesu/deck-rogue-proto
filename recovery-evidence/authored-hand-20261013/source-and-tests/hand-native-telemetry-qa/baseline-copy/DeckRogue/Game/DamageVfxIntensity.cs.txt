using System;

namespace DeckRogue.Game;

public sealed class DamageVfxIntensity
{
	public sealed class Visual
	{
		public readonly float Size;

		public readonly float Particles;

		public readonly float Emission;

		public readonly float Light;

		public readonly int Fragments;

		public readonly string Tier;

		public readonly bool CountLimited;

		public float FallbackParticleGain => CountLimited ? 1f : Particles;

		internal Visual(float size, float particles, float emission, float light, int fragments, string tier, bool countLimited = false)
		{
			Size = size;
			Particles = particles;
			Emission = emission;
			Light = light;
			Fragments = fragments;
			Tier = tier;
			CountLimited = countLimited;
		}
	}

	public const string FixedSmallContactPolicy = "noncard_fixed_small_contact_v1";

	private static readonly Visual FixedSmallContact = new Visual(0.88f, 0.65f, 0.72f, 0.65f, 8, "small", countLimited: true);

	public readonly int Raw;

	public readonly int Effective;

	public readonly int? PriorHp;

	public readonly int? MaxHp;

	public readonly string Tier;

	public readonly string HpEvidence;

	public readonly bool RatioPromoted;

	public static bool IsFixedSmallContact(string policy)
	{
		return policy == "noncard_fixed_small_contact_v1";
	}

	private DamageVfxIntensity(int raw, int effective, int? prior, int? max, string tier, bool promoted, string evidence)
	{
		Raw = raw;
		Effective = effective;
		PriorHp = prior;
		MaxHp = max;
		Tier = tier;
		RatioPromoted = promoted;
		HpEvidence = evidence;
	}

	public static DamageVfxIntensity Evaluate(int hpLoss, int? provenPriorHp = null, int? knownMaxHp = null, string evidence = null)
	{
		int raw = Math.Max(0, hpLoss);
		int effective = (provenPriorHp.HasValue ? Math.Min(raw, Math.Max(0, provenPriorHp.Value)) : raw);
		int tier = ((effective >= 5) ? ((effective < 15) ? 1 : 2) : 0);
		bool promoted = effective >= 4 && knownMaxHp > 0 && 4L * (long)effective >= knownMaxHp.Value && tier < 2;
		if (promoted)
		{
			tier++;
		}
		return new DamageVfxIntensity(raw, effective, provenPriorHp, knownMaxHp, tier switch
		{
			1 => "medium", 
			0 => "small", 
			_ => "large", 
		}, promoted, evidence ?? (provenPriorHp.HasValue ? "proven_event_pre_hp" : "bounded_raw_hp_loss"));
	}

	private static float Clamp(float value, float min, float max)
	{
		return Math.Max(min, Math.Min(max, value));
	}

	public static Visual VisualFor(DamageVfxIntensity intensity, string policy = null)
	{
		return IsFixedSmallContact(policy) ? FixedSmallContact : intensity?.Compose(policy);
	}

	public Visual Compose(string semanticPresentation = null)
	{
		if (IsFixedSmallContact(semanticPresentation))
		{
			return FixedSmallContact;
		}
		bool small = Tier == "small";
		bool large = Tier == "large";
		bool semanticSmall = semanticPresentation == "small";
		return new Visual(Clamp((small ? 0.88f : (large ? 1.12f : 1f)) * (semanticSmall ? 0.72f : 1f), 0.72f, 1.12f), Clamp((small ? 0.65f : (large ? 1.15f : 1f)) * (semanticSmall ? 0.85f : 1f), 0.65f, 1.15f), Clamp((small ? 0.72f : (large ? 1.18f : 1f)) * (semanticSmall ? 0.75f : 1f), 0.6f, 1.18f), Clamp((small ? 0.65f : (large ? 1.2f : 1f)) * (semanticSmall ? 0.65f : 1f), 0.5f, 1.2f), small ? 8 : (large ? 24 : 16), Tier);
	}
}
