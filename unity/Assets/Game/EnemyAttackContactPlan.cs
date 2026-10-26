using System;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game {
 // Frozen arithmetic projection of an actual event. Never evaluates combat rules.
 public sealed class EnemyAttackContactPlan {
  public const int MaxIndividualContacts=12, MaxCapturedContributions=256;
  public const float ContactSpacing=.12f;
  // The caller must prove instance continuity from captured command population /
  // execution lineage AND current board ownership. Equal IDs alone are not proof.
  public sealed class SourceProof {
   public readonly int EnemyIndex; public readonly string EnemyId; public readonly bool ContinuityProven;
   public SourceProof(int enemyIndex,string enemyId,bool continuityProven) {EnemyIndex=enemyIndex;EnemyId=enemyId;ContinuityProven=continuityProven;}
  }
  readonly EnemyAttackContribution[] rows;
  internal readonly CombatPresentationVitals Before,After;
  public readonly int SourceIndex,ExecutedHits;
  public readonly string SourceId,Reason;
  public readonly long Amount,HpLoss,Block,Ice,Nullified,Mitigated;
  public bool Individual=>Reason==null && Count>=2 && Count<=MaxIndividualContacts;
  public int Count=>rows.Length;
  public float Span=>Individual?ContactSpacing*(Count-1):0f;
  public EnemyAttackContribution this[int ordinal]=>rows[ordinal];
  EnemyAttackContactPlan(GameEvent_DamageDealt e,EnemyAttackContribution[] captured,string reason,long block=0,long ice=0,long nullified=0,long mitigated=0) {
   rows=captured??Array.Empty<EnemyAttackContribution>();Reason=reason;
   SourceIndex=e?.EnemyIndex??-1;SourceId=e?.PresentationEnemyAttack?.EnemyId;ExecutedHits=e?.PresentationEnemyAttack?.ExecutedHits??0;
   Amount=e?.Amount??0;HpLoss=e?.HpLoss??0;Block=block;Ice=ice;Nullified=nullified;Mitigated=mitigated;
   var v=e?.PresentationReceipt;Before=v?.Before==null?null:v.Before with{};After=v?.After==null?null:v.After with{};
  }
  public bool Matches(SourceProof proof)=>proof!=null && proof.ContinuityProven && proof.EnemyIndex==SourceIndex && !string.IsNullOrEmpty(proof.EnemyId) && proof.EnemyId==SourceId;
  public static EnemyAttackContactPlan Capture(GameEvent e,CombatPresentationReceipt receipt,SourceProof proof) {
   var d=e as GameEvent_DamageDealt;
   EnemyAttackContactPlan Reject(string why)=>new EnemyAttackContactPlan(d,null,why);
   if(d==null || d.Source!="enemy" || !d.EnemyIndex.HasValue || d.EnemyIndex.Value<0)return Reject("enemy_contact_source_unsupported");
   if(!ReferenceEquals(receipt,d.PresentationReceipt) || !CombatPresentationLedger.Valid(d,receipt,out _ ) || receipt.Target!="player")return Reject("enemy_contact_numeric_receipt_invalid");
   var p=d.PresentationEnemyAttack;
   if(p==null)return Reject("missing_enemy_contact_metadata");
   if(p.Version!=1)return Reject("unsupported_enemy_contact_metadata_version");
   if(p.Resolution!="aggregate-final-mutation")return Reject("unsupported_enemy_contact_resolution");
   if(string.IsNullOrEmpty(p.EnemyId))return Reject("enemy_contact_source_id_missing");
   if(proof==null || !proof.ContinuityProven || proof.EnemyIndex!=d.EnemyIndex.Value || proof.EnemyId!=p.EnemyId)return Reject("enemy_contact_source_unproved");
   if(p.EffectiveActual<0 || p.LoopBound<0 || p.ExecutedHits<0 || d.Amount<0 || d.HpLoss<0 || (d.Blocked??0)<0)return Reject("invalid_enemy_contact_scalar");
   if(p.ExecutedHits!=Math.Max(0,p.LoopBound))return Reject("enemy_contact_count_mismatch");
   if(p.ContributionsOmittedReason!=null) {
    if(p.Contributions!=null || p.ContributionsOmittedReason!="presentation_capture_limit" || p.ExecutedHits<=MaxCapturedContributions)return Reject("contradictory_enemy_contact_omission");
    return Reject("presentation_capture_limit");
   }
   if(p.ExecutedHits>MaxCapturedContributions)return Reject("enemy_contact_capture_limit_violation");
   if(p.Contributions==null || p.Contributions.Count!=p.ExecutedHits)return Reject("enemy_contact_count_mismatch");
   var captured=new EnemyAttackContribution[p.Contributions.Count];
   long amount=0,hp=0,block=0,ice=0,nullified=0,mitigated=0;
   for(int i=0;i<captured.Length;i++) {
    var row=p.Contributions[i];
    if(row==null || row.AmountContribution<0 || row.HpLossContribution<0 || row.BlockContribution<0 || row.IceContribution<0 || row.NullifiedContribution<0 || row.MitigatedContribution<0)return Reject("invalid_enemy_contact_contribution");
    if((long)row.AmountContribution!=(long)row.HpLossContribution+row.BlockContribution+row.IceContribution+row.NullifiedContribution+row.MitigatedContribution)return Reject("enemy_contact_partition_mismatch");
    captured[i]=row with{};
    amount+=row.AmountContribution;hp+=row.HpLossContribution;block+=row.BlockContribution;ice+=row.IceContribution;nullified+=row.NullifiedContribution;mitigated+=row.MitigatedContribution;
   }
   if(amount!=d.Amount || hp!=d.HpLoss || block+ice+nullified!=(long)(d.Blocked??0))return Reject("enemy_contact_aggregate_mismatch");
   var before=receipt.Before;var after=receipt.After;
   if(before.MaxHp!=after.MaxHp || (long)before.Hp-after.Hp!=hp || (long)before.Block-after.Block!=block || (long)before.IceBlock.Value-after.IceBlock.Value!=ice)return Reject("enemy_contact_numeric_delta_mismatch");
   string reason=captured.Length==0?"enemy_contact_zero_executed":captured.Length==1?"enemy_contact_single":captured.Length>MaxIndividualContacts?"enemy_contact_presentation_limit":null;
   return new EnemyAttackContactPlan(d,captured,reason,block,ice,nullified,mitigated);
  }
 }
}
