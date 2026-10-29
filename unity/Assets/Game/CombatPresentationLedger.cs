using System;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game {
 // Display-only numeric history. No damage, absorption, healing or turn rules are recomputed.
 public sealed class CombatPresentationLedger {
  public sealed class Vitals {
   public long Hp,MaxHp,Block,IceBlock; public string EnemyId;
   internal Vitals Copy()=>new Vitals{Hp=Hp,MaxHp=MaxHp,Block=Block,IceBlock=IceBlock,EnemyId=EnemyId};
  }
  public sealed class Batch {
   public int Id,Generation,From,To,Pending; public bool Finished; public GameState After; public GameState Before; public RunCommand OwnerCommand;
  }
  public sealed class Receipt {
   public int Index,Generation,Target=-1; public GameEvent Event; public Batch Batch;
   public CombatPresentationReceipt Values; public string Reason; public bool Committed,Exact;
   public EnemyAttackContactPlan Projection {get;internal set;}
   public string ProjectionReason {get;internal set;}
   public int NextContribution {get;internal set;}
   public bool ProjectionStarted {get;internal set;}
  }
  readonly SortedDictionary<int,Receipt> pending=new SortedDictionary<int,Receipt>();
  readonly List<Batch> batches=new List<Batch>();
  readonly Dictionary<int,Vitals> vitals=new Dictionary<int,Vitals>();
  readonly HashSet<int> unstable=new HashSet<int>();
  GameState latest; int nextBatch;
  public int Generation {get;private set;}
  public int PendingCount=>pending.Count;
  public int Discontinuities {get;private set;}
  public int Reconciliations {get;private set;}
  public string LastReason {get;private set;}
  public GameState Latest=>latest;
  public bool Active=>latest!=null;
  public Action<string,int> Audit;
  static Vitals Player(PlayerState p)=>new Vitals{Hp=p.Hp,MaxHp=p.MaxHp,Block=p.Block,IceBlock=p.IceBlock};
  static Vitals Enemy(EnemyState e)=>new Vitals{Hp=e.Hp,MaxHp=e.MaxHp,Block=e.Block,EnemyId=e.EnemyId};
  public static bool IsNumeric(GameEvent e)=>Read(e)!=null || e is GameEvent_DamageDealt || e is GameEvent_ConfusedAttack || e is GameEvent_ThornsReflected || e is GameEvent_BlockGained || e is GameEvent_IceBlockGained || e is GameEvent_BlockShattered || e is GameEvent_HpHealed || e is GameEvent_HpLost || e is GameEvent_ScaldTick || e is GameEvent_BurnTick || e is GameEvent_EnemyHealed || e is GameEvent_RegenTicked || e is GameEvent_DeathSaved || e is GameEvent_MaxHpGained;
  public static CombatPresentationReceipt Read(GameEvent e) {
   switch(e) {
    case GameEvent_DamageDealt v:return v.PresentationReceipt;
    case GameEvent_ConfusedAttack v:return v.PresentationReceipt;
    case GameEvent_ThornsReflected v:return v.PresentationReceipt;
    case GameEvent_BlockGained v:return v.PresentationReceipt;
    case GameEvent_IceBlockGained v:return v.PresentationReceipt;
    case GameEvent_BlockShattered v:return v.PresentationReceipt;
    case GameEvent_HpHealed v:return v.PresentationReceipt;
    case GameEvent_HpLost v:return v.PresentationReceipt;
    case GameEvent_ScaldTick v:return v.PresentationReceipt;
    case GameEvent_BurnTick v:return v.PresentationReceipt;
    case GameEvent_EnemyHealed v:return v.PresentationReceipt;
    case GameEvent_RegenTicked v:return v.PresentationReceipt;
    case GameEvent_DeathSaved v:return v.PresentationReceipt;
    case GameEvent_MaxHpGained v:return v.PresentationReceipt;
    default:return null;
   }
  }
  static bool TargetAgrees(GameEvent e,CombatPresentationReceipt r) {
   int target=-1;bool enemy=false;
   switch(e) {
    case GameEvent_DamageDealt v:
     if(v.Source!="player" && v.Source!="enemy")return false;
     enemy=v.Source=="player";target=enemy?(v.EnemyIndex??-2):-1;break;
    case GameEvent_ConfusedAttack v:enemy=true;target=v.TargetIndex;break;
    case GameEvent_BlockShattered v:enemy=true;target=v.EnemyIndex;break;
    case GameEvent_BurnTick v:enemy=true;target=v.EnemyIndex;break;
    case GameEvent_EnemyHealed v:enemy=true;target=v.TargetIndex;break;
    case GameEvent_RegenTicked v:enemy=true;target=v.EnemyIndex;break;
    case GameEvent_BlockGained v:
     if(v.Target=="enemy")return r.Target=="enemy" && r.EnemyIndex.HasValue && r.EnemyIndex.Value>=0;
     if(v.Target!="player")return false;break;
   }
   return enemy?target>=0 && r.Target=="enemy" && r.EnemyIndex==target:r.Target=="player" && !r.EnemyIndex.HasValue && r.EnemyId==null && target==-1;
  }
  public static bool Valid(GameEvent e,CombatPresentationReceipt r,out string reason) {
   reason=null;
   if(r==null){reason="missing_numeric_metadata";return false;}
   if(r.Version!=1){reason="unsupported_numeric_metadata_version";return false;}
   if(r.Before==null || r.After==null){reason="missing_numeric_snapshot";return false;}
   if(!TargetAgrees(e,r)){reason="numeric_target_mismatch";return false;}
   bool player=r.Target=="player";
   if((!player && r.Target!="enemy") || (player && (!r.Before.IceBlock.HasValue || !r.After.IceBlock.HasValue)) || (!player && (r.Before.IceBlock.HasValue || r.After.IceBlock.HasValue || string.IsNullOrEmpty(r.EnemyId)))){reason="numeric_field_mask_mismatch";return false;}
   if(r.Before.MaxHp<=0 || r.After.MaxHp<=0 || r.Before.Block<0 || r.After.Block<0 || r.Before.IceBlock<0 || r.After.IceBlock<0){reason="invalid_numeric_snapshot";return false;}
   int? loss=e is GameEvent_DamageDealt d?(int?)d.HpLoss:e is GameEvent_ConfusedAttack c?(int?)c.HpLoss:e is GameEvent_ThornsReflected t?(int?)t.HpLoss:null;
   if(loss.HasValue && (long)r.Before.Hp-r.After.Hp!=loss.Value){reason="numeric_hp_loss_payload_mismatch";return false;}
   return true;
  }
  public void Clear() {++Generation;pending.Clear();batches.Clear();vitals.Clear();unstable.Clear();latest=null;nextBatch=0;LastReason=null;}
  void Seed(GameState state) {vitals[-1]=Player(state.Player);for(int i=0;i<state.Enemies.Count;i++)vitals[i]=Enemy(state.Enemies[i]);latest=state;}
  static bool Prefix(GameState before,GameState after) {
   if(before==null || after==null || before.EventLog.Count>after.EventLog.Count)return false;
   for(int i=0;i<before.EventLog.Count;i++)if(!ReferenceEquals(before.EventLog[i],after.EventLog[i]))return false;
   return true;
  }
  public Batch Register(GameState before,GameState after,RunCommand ownerCommand=null) {
   if(!Prefix(before,after)){Note("unproved_command_prefix",-1);return null;}
   if(latest==null)Seed(before);
   if(!ReferenceEquals(latest,before)){Note("numeric_command_lineage_mismatch",-1);return null;}
   var b=new Batch{Id=++nextBatch,Generation=Generation,From=before.EventLog.Count,To=after.EventLog.Count,After=after,Before=before,OwnerCommand=ownerCommand};batches.Add(b);
   for(int i=0;i<Math.Max(before.Enemies.Count,after.Enemies.Count);i++)
    if(i>=before.Enemies.Count || i>=after.Enemies.Count || before.Enemies[i].EnemyId!=after.Enemies[i].EnemyId)unstable.Add(i);
   for(int i=b.From;i<b.To;i++)if(after.EventLog[i] is GameEvent_EnemyHatched hatched)unstable.Add(hatched.EnemyIndex);
   // A later accepted population change invalidates earlier not-yet-presented
   // receipts at that index too; an old callback must not touch its replacement.
   foreach(var earlier in pending.Values)if(earlier.Target>=0 && unstable.Contains(earlier.Target))earlier.Reason="numeric_population_boundary_unproved";
   for(int i=b.From;i<b.To;i++) {
    var e=after.EventLog[i];
    if(!IsNumeric(e))continue;
    var value=Read(e);var receipt=new Receipt{Index=i,Generation=Generation,Event=e,Batch=b,Values=value};
    if(Valid(e,value,out var reason)) {
     receipt.Target=value.Target=="player"?-1:value.EnemyIndex.Value;
     if(receipt.Target>=0 && (!vitals.TryGetValue(receipt.Target,out var v) || v.EnemyId!=value.EnemyId))receipt.Reason="numeric_target_identity_unproved";
     else if(unstable.Contains(receipt.Target))receipt.Reason="numeric_population_boundary_unproved";
    } else receipt.Reason=reason;
    pending.Add(i,receipt);b.Pending++;
   }
   latest=after;return b;
  }
  public Receipt Find(int index,GameEvent e)=>pending.TryGetValue(index,out var value) && ReferenceEquals(value.Event,e)?value:null;
  public bool Current(Receipt r)=>r!=null && r.Generation==Generation && !r.Committed && pending.TryGetValue(r.Index,out var p) && ReferenceEquals(p,r);
  public bool Ready(Receipt r) {if(!Current(r))return false;foreach(var p in pending)return ReferenceEquals(p.Value,r);return false;}
  // Observe an explicitly supplied scenario group, without consuming any receipt.
  // Earlier unrelated receipts may still be pending. Interleaving inside the
  // group is rejected so one AoE preparation cannot skip a reactive numeric event.
  public bool ObserveScenarioReservation(IReadOnlyList<Receipt> group,out bool ready,out string reason) {
   ready=false;reason=null;
   if(group==null || group.Count==0 || group.Count>256){reason="scenario_group_count_invalid";return false;}
   var batch=group[0]?.Batch;
   for(int i=0;i<group.Count;i++)if(!Current(group[i]) || group[i].Reason!=null || !ReferenceEquals(group[i].Batch,batch)
      || (i>0 && group[i].Index<=group[i-1].Index)){reason="scenario_group_receipt_unproved";return false;}
   int next=0,first=group[0].Index,last=group[group.Count-1].Index;
   foreach(var pair in pending) {
    if(pair.Key<first)continue;
    if(pair.Key>last)break;
    if(next>=group.Count || !ReferenceEquals(pair.Value,group[next++])){reason="scenario_group_numeric_interleaving";return false;}
   }
   if(next!=group.Count){reason="scenario_group_incomplete";return false;}
   ready=Ready(group[0]);return true;
  }
  static bool Same(Vitals v,CombatPresentationVitals p)=>v.Hp==p.Hp && v.MaxHp==p.MaxHp && v.Block==p.Block && v.IceBlock==(p.IceBlock??0);
  // Optional projection rejection never changes numeric receipt validity.
  public EnemyAttackContactPlan ConfigureEnemyProjection(Receipt r,EnemyAttackContactPlan.SourceProof proof) {
   if(!Current(r) || r.Projection!=null)return r?.Projection;
   r.Projection=EnemyAttackContactPlan.Capture(r.Event,r.Values,proof);
   if(r.Reason!=null)RejectProjection(r,"enemy_contact_numeric_receipt_invalid");
   else if(r.Projection.Reason!=null)RejectProjection(r,r.Projection.Reason);
   else if(!SourceCurrent(r,proof))RejectProjection(r,"enemy_contact_source_unproved");
   return r.Projection;
  }
  bool SourceCurrent(Receipt r,EnemyAttackContactPlan.SourceProof proof) {
   var p=r.Projection;
   return p!=null && p.Matches(proof) && !unstable.Contains(p.SourceIndex) && latest!=null && p.SourceIndex<latest.Enemies.Count && latest.Enemies[p.SourceIndex].EnemyId==p.SourceId;
  }
  void RejectProjection(Receipt r,string reason) {r.ProjectionReason=reason;Note(reason,r.Index);}
  public bool TryBeginProjection(Receipt r,EnemyAttackContactPlan.SourceProof proof) {
   if(!Current(r) || !Ready(r) || r.Projection==null || !r.Projection.Individual || r.ProjectionReason!=null || r.ProjectionStarted)return false;
   if(r.Reason!=null || !SourceCurrent(r,proof)){RejectProjection(r,"enemy_contact_source_unproved");return false;}
   if(!vitals.TryGetValue(r.Target,out var v) || !Same(v,r.Projection.Before)){RejectProjection(r,"enemy_contact_numeric_boundary_mismatch");return false;}
   r.ProjectionStarted=true;r.Exact=true;return true;
  }
  public bool CommitContribution(Receipt r,int ordinal,EnemyAttackContactPlan.SourceProof proof) {
   if(!Current(r) || !Ready(r) || !r.ProjectionStarted || r.ProjectionReason!=null || r.Projection==null || !r.Projection.Individual)return false;
   if(ordinal!=r.NextContribution || ordinal<0 || ordinal>=r.Projection.Count){Note("enemy_contact_ordinal_mismatch",r.Index);return false;}
   if(r.Reason!=null || !SourceCurrent(r,proof)){RejectProjection(r,"enemy_contact_source_lost");return false;}
   if(!vitals.TryGetValue(r.Target,out var v)){RejectProjection(r,"enemy_contact_target_unavailable");return false;}
   var row=r.Projection[ordinal];
   long hp=v.Hp-row.HpLossContribution,block=v.Block-row.BlockContribution,ice=v.IceBlock-row.IceContribution;
   bool last=ordinal==r.Projection.Count-1;
   var after=r.Projection.After;
   if(block<0 || ice<0 || (last && (hp!=after.Hp || v.MaxHp!=after.MaxHp || block!=after.Block || ice!=(after.IceBlock??0)))){RejectProjection(r,"enemy_contact_numeric_continuity_lost");return false;}
   v.Hp=hp;v.Block=block;v.IceBlock=ice;r.NextContribution++;
   if(last)CompleteReceipt(r);
   return true;
  }
  // Scope caller-supplied event indices to this generation. This is only a
  // dependency query for a reserved train, not a new global actor/frame gate.
  public bool HasPendingEnemyProjectionBefore(int eventIndex) {
   foreach(var pair in pending) {
    if(pair.Key>=eventIndex)break;
    var r=pair.Value;
    if(r.Projection!=null && r.Projection.Individual && (r.ProjectionReason==null || r.ProjectionStarted))return true;
   }
   return false;
  }
  public bool HasPendingEnemyProjectionBefore(int generation,int eventIndex)=>generation==Generation && HasPendingEnemyProjectionBefore(eventIndex);
  void CompleteReceipt(Receipt r) {r.Committed=true;pending.Remove(r.Index);r.Batch.Pending=Math.Max(0,r.Batch.Pending-1);}
  public bool Commit(Receipt r) {
   if(!Current(r))return false;
   if(r.NextContribution>0){Note("enemy_contact_partial_projection_requires_reconcile",r.Index);return false;}
   if(!Ready(r)){Note("numeric_contact_out_of_order",r.Index);return false;}
   if(r.Reason==null && vitals.TryGetValue(r.Target,out var v)) {
    r.Exact=Same(v,r.Values.Before);
    if(!r.Exact){r.Reason="unannounced_numeric_boundary";Note(r.Reason,r.Index);}
    // A gap is NOT absorbed into this hit. Apply only this event's proven delta;
    // silent changes remain visible as an audited gap until owner completion reconcile.
    v.Hp+=(long)r.Values.After.Hp-r.Values.Before.Hp;
    v.MaxHp+=(long)r.Values.After.MaxHp-r.Values.Before.MaxHp;
    v.Block+=(long)r.Values.After.Block-r.Values.Before.Block;
    v.IceBlock+=(long)(r.Values.After.IceBlock??0)-(r.Values.Before.IceBlock??0);
   } else Note(r.Reason??"numeric_target_unavailable",r.Index);
   CompleteReceipt(r);return true;
  }
  public bool Finish(Batch b) {
   if(b==null || b.Generation!=Generation || !batches.Contains(b))return false;
   b.Finished=true;return TryReconcile();
  }
  public bool TryReconcile() {
   if(latest==null || pending.Count!=0 || batches.Count==0)return false;
   foreach(var b in batches)if(!b.Finished)return false;
   bool different=false;
   if(!vitals.TryGetValue(-1,out var p) || !Same(p,EventsVitals(latest.Player)))different=true;
   for(int i=0;i<latest.Enemies.Count;i++)if(!vitals.TryGetValue(i,out var v) || v.EnemyId!=latest.Enemies[i].EnemyId || !Same(v,EventsVitals(latest.Enemies[i])))different=true;
   if(different || unstable.Count>0)Note("command_completion_numeric_reconcile",-1);
   vitals.Clear();unstable.Clear();Seed(latest);batches.Clear();Reconciliations++;return true;
  }
  static CombatPresentationVitals EventsVitals(PlayerState p)=>new CombatPresentationVitals{Hp=p.Hp,MaxHp=p.MaxHp,Block=p.Block,IceBlock=p.IceBlock};
  static CombatPresentationVitals EventsVitals(EnemyState p)=>new CombatPresentationVitals{Hp=p.Hp,MaxHp=p.MaxHp,Block=p.Block};
  void Note(string reason,int index) {LastReason=reason;Discontinuities++;Audit?.Invoke(reason,index);}
  public bool TryGet(GameState state,int target,out Vitals result) {
   result=null;
   if(latest==null || state==null || !ReferenceEquals(latest,state) || unstable.Contains(target) || !vitals.TryGetValue(target,out var v))return false;
   if(target>=0 && (target>=state.Enemies.Count || state.Enemies[target].EnemyId!=v.EnemyId))return false;
   result=v.Copy();return true;
  }
  public bool HasPending(int target) {foreach(var p in pending.Values)if(p.Target==target && p.Reason==null)return true;return false;}
  public static int Display(long value)=>value<=0?0:value>int.MaxValue?int.MaxValue:(int)value;
 }
}
