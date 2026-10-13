using System;
using DeckRogue.Game;

namespace DeckRogue.GodotPort.Presentation;

public sealed record EffectScenario(EffectTier Tier, float Windup, float Contact, float Sustain, float Release, int Branches, float Radius, float LightEnergy)
{
	public float Duration => Windup + Contact + Sustain + Release;

	public DamageVfxIntensity.Visual? SourceVisual { get; init; }

	public static EffectScenario For(AttackRoute route, int amount, int? provenPriorHp = null, int? maxHp = null)
	{
		DamageVfxIntensity intensity = DamageVfxIntensity.Evaluate(amount, provenPriorHp, maxHp);
		DamageVfxIntensity.Visual visual = intensity.Compose(route.FixedContact ? "noncard_fixed_small_contact_v1" : route.PresentationPolicy);
		EffectTier tier = ((visual.Tier == "large") ? EffectTier.Large : ((visual.Tier == "medium") ? EffectTier.Medium : EffectTier.Small));
		if (route.SemanticSmall && route.Renderer == "PhysicalSlashFx")
		{
			tier = EffectTier.Small;
		}
		(float, float) time = AuthoredEffectPlan.Times(route, tier);
		return new EffectScenario(tier, time.Item1, 0.016f, Math.Max(0f, time.Item2 - 0.046f), Math.Min(0.03f, time.Item2), tier switch
		{
			EffectTier.Medium => 3, 
			EffectTier.Small => 1, 
			_ => 5, 
		}, tier switch
		{
			EffectTier.Medium => 0.8f, 
			EffectTier.Small => 0.42f, 
			_ => 1.28f, 
		}, tier switch
		{
			EffectTier.Medium => 1.7f, 
			EffectTier.Small => 1f, 
			_ => 2.5f, 
		})
		{
			SourceVisual = visual
		};
	}
}
