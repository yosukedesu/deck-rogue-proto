using System;
namespace DeckRogue.Game {
 // Presentation-only, provisional three-tier policy. Never feeds combat or RNG.
 // The tier is frozen before preview; shape variants may use it, with standard-art fallback.
 public sealed class DamageVfxIntensity {
  public const string FixedSmallContactPolicy="noncard_fixed_small_contact_v1";
  public static bool IsFixedSmallContact(string policy)=>policy==FixedSmallContactPolicy;
  // Immutable final packet. Count-based fallback consumes density once; native scalar
  // families still read Particles as gain and native slash ignores all baked scalars.
  static readonly Visual FixedSmallContact=new Visual(.88f,.65f,.72f,.65f,8,"small",true);
  public readonly int Raw,Effective; public readonly int? PriorHp,MaxHp;
  public readonly string Tier,HpEvidence; public readonly bool RatioPromoted;
  DamageVfxIntensity(int raw,int effective,int? prior,int? max,string tier,bool promoted,string evidence){Raw=raw;Effective=effective;PriorHp=prior;MaxHp=max;Tier=tier;RatioPromoted=promoted;HpEvidence=evidence;}
  public static DamageVfxIntensity Evaluate(int hpLoss,int? provenPriorHp=null,int? knownMaxHp=null,string evidence=null) {
   int raw=Math.Max(0,hpLoss),effective=provenPriorHp.HasValue?Math.Min(raw,Math.Max(0,provenPriorHp.Value)):raw;
   int tier=effective<5?0:effective<15?1:2;
   bool promoted=effective>=4 && knownMaxHp>0 && 4L*effective>=knownMaxHp.Value && tier<2;
   if(promoted)tier++;
   return new DamageVfxIntensity(raw,effective,provenPriorHp,knownMaxHp,tier==0?"small":tier==1?"medium":"large",promoted,evidence??(provenPriorHp.HasValue?"proven_event_pre_hp":"bounded_raw_hp_loss"));
  }
  public sealed class Visual {
   public readonly float Size,Particles,Emission,Light; public readonly int Fragments; public readonly string Tier; public readonly bool CountLimited;
   public float FallbackParticleGain=>CountLimited?1f:Particles;
   internal Visual(float size,float particles,float emission,float light,int fragments,string tier,bool countLimited=false){Size=size;Particles=particles;Emission=emission;Light=light;Fragments=fragments;Tier=tier;CountLimited=countLimited;}
  }
  static float Clamp(float value,float min,float max)=>Math.Max(min,Math.Min(max,value));
  public static Visual VisualFor(DamageVfxIntensity intensity,string policy=null)=>IsFixedSmallContact(policy)?FixedSmallContact:intensity?.Compose(policy);
  public Visual Compose(string semanticPresentation=null) {
   if(IsFixedSmallContact(semanticPresentation))return FixedSmallContact;
   bool small=Tier=="small",large=Tier=="large",semanticSmall=semanticPresentation=="small";
   // Final scalars, not another multiplier stacked on the old heavy escalation.
   return new Visual(Clamp((small?.88f:large?1.12f:1f)*(semanticSmall?.72f:1f),.72f,1.12f),
    Clamp((small?.65f:large?1.15f:1f)*(semanticSmall?.85f:1f),.65f,1.15f),
    Clamp((small?.72f:large?1.18f:1f)*(semanticSmall?.75f:1f),.60f,1.18f),
    Clamp((small?.65f:large?1.20f:1f)*(semanticSmall?.65f:1f),.50f,1.20f),small?8:large?24:16,Tier);
  }
 }
}
