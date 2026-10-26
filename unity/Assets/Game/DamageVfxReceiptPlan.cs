using System;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game {
 // Pure, presentation-only HP evidence planner. No state/event mutation, engine calls, clocks or RNG.
 // This proves event-order engine HP, not the order of already-running visual callbacks.
 // The caller must bind the result before preview and retain board/actor generation checks.
 public static class DamageVfxReceiptPlan {
  sealed class Target {
   public long Hp; public int FinalHp; public int? Max;
   public bool Identity=true,Proof=true; public string Reason;
   public Target(int hp,int finalHp,int? max){Hp=hp;FinalHp=finalHp;Max=max;}
   public void Reject(string reason,bool identity=false){Proof=false;if(Reason==null)Reason=reason;if(identity){Identity=false;Max=null;}}
  }
  sealed class Receipt { public int Index,Raw; public long Prior; public Target Target; public string Reason; }

  public static Dictionary<int,DamageVfxIntensity> Build(IReadOnlyList<GameEvent> log,int from,GameState before,GameState after) {
   var result=new Dictionary<int,DamageVfxIntensity>();
   if(log==null)return result;
   int start=Math.Max(0,Math.Min(from,log.Count));
   string context=ContextFailure(log,from,before,after);
   if(context!=null){FillRaw(result,log,start,context);return result;}
   var player=new Target(before.Player.Hp,after.Player.Hp,before.Player.MaxHp>0?before.Player.MaxHp:(int?)null);
   if(before.Player.MaxHp!=after.Player.MaxHp)
    player.Reject("player_identity_or_maxhp_changed",true);
   var enemies=new Target[before.Enemies.Count];
   bool countChanged=before.Enemies.Count!=after.Enemies.Count;
   for(int n=0;n<enemies.Length;n++) {
    var a=before.Enemies[n];var b=n<after.Enemies.Count?after.Enemies[n]:null;
    if(a==null){enemies[n]=new Target(0,0,null);enemies[n].Reject("missing_enemy_snapshot",true);continue;}
    var t=new Target(a.Hp,b?.Hp??0,a.MaxHp>0?a.MaxHp:(int?)null);enemies[n]=t;
    if(countChanged || b==null || String.IsNullOrEmpty(a.EnemyId) || !String.Equals(a.EnemyId,b.EnemyId,StringComparison.Ordinal) || a.MaxHp!=b.MaxHp || a.Fled==true || b.Fled==true)
     t.Reject("enemy_identity_or_maxhp_changed",true);
   }
   var receipts=new List<Receipt>();
   for(int i=start;i<log.Count;i++) {
    var ev=log[i];
    if(ev==null){RejectAll(player,enemies,"null_event",true);continue;}
    if(TryDamage(ev,out int raw,out int enemy,out bool toPlayer,out string invalid)) {
     Target t=invalid!=null?null:toPlayer?player:Enemy(enemies,enemy);
     receipts.Add(new Receipt{Index=i,Raw=raw,Prior=t?.Hp??0,Target=t,Reason=invalid??(t==null?"unresolved_damage_target":null)});
     if(t==null){RejectAll(player,enemies,"invalid_damage_target",true);continue;}
     if(raw<0){t.Reject("negative_damage_payload");continue;}
     Add(t,-(long)raw);
     continue;
    }
    switch(ev) {
     case GameEvent_HpHealed h: Add(player,Math.Max(0,h.Amount));break; // Engine deliberately clamps negative healed delta.
     case GameEvent_HpLost h: if(h.Amount<0)player.Reject("negative_cost_payload");else Add(player,-(long)h.Amount);break;
     case GameEvent_DeathSaved h:
      // Absolute assignment, not a heal delta. Keep negative overkill until this point.
      if(player.Hp>0)player.Reject("nonlethal_deathsave");
      if(h.Hp<0)player.Reject("invalid_deathsave_payload");else player.Hp=h.Hp;
      break;
     case GameEvent_EnemyHealed h: AddEnemy(player,enemies,h.TargetIndex,h.Amount);break; // EnemyIndex is the healer, not recipient.
     case GameEvent_RegenTicked h: AddEnemy(player,enemies,h.EnemyIndex,h.Amount);break;
     case GameEvent_EnemyFled f:
      var fled=Enemy(enemies,f.EnemyIndex);
      if(fled==null)RejectAll(player,enemies,"invalid_flee_target",true);
      else {fled.Hp=0;fled.Reject("enemy_fled",true);} // Not damage and not a live target after escape.
      break;
     case GameEvent_MaxHpGained h:
      Add(player,h.Amount);player.Reject("maxhp_change",true);break;
     case GameEvent_EnemySplit _:
     case GameEvent_EnemySummoned _:
     case GameEvent_EnemyHatched _:
      foreach(var t in enemies)t.Reject("enemy_population_or_identity_event",true);break;
     case GameEvent_CombatStarted _:
      RejectAll(player,enemies,"combat_restart",true);break;
     default:
      // Explicit current allowlist, so future events cannot silently claim proof.
      if(!KnownNoHpChange(ev))RejectAll(player,enemies,"unknown_event_"+ev.GetType().Name,true);
      break;
    }
   }
   Validate(player);foreach(var t in enemies)Validate(t);
   foreach(var r in receipts) {
    var t=r.Target;
    bool proven=t!=null && t.Identity && t.Proof && r.Prior>=Int32.MinValue && r.Prior<=Int32.MaxValue;
    string evidence=proven?"proven_event_pre_hp":"bounded_raw_"+(r.Reason??t?.Reason??"unproven_hp");
    result[r.Index]=DamageVfxIntensity.Evaluate(r.Raw,proven?(int?)r.Prior:null,t!=null&&t.Identity?t.Max:null,evidence);
   }
   return result;
  }
  static string ContextFailure(IReadOnlyList<GameEvent> log,int from,GameState before,GameState after) {
   if(from<0 || from>log.Count)return "bounded_raw_invalid_from";
   if(before?.Player==null || after?.Player==null || before.Enemies==null || after.Enemies==null || before.EventLog==null || after.EventLog==null)return "bounded_raw_missing_state";
   if(before.EventLog.Count!=from)return "bounded_raw_before_count_mismatch";
   if(from==0)return "bounded_raw_missing_combat_origin";
   if(after.EventLog.Count!=log.Count)return "bounded_raw_after_log_mismatch";
   for(int i=0;i<log.Count;i++)if(!Object.ReferenceEquals(log[i],after.EventLog[i]))return "bounded_raw_after_log_mismatch";
   // First reference alone is insufficient: Report.TrimLog retains EventLog[0].
   for(int i=0;i<from;i++)if(before.EventLog[i]==null || !Object.ReferenceEquals(before.EventLog[i],log[i]))return "bounded_raw_prefix_mismatch";
   return null;
  }
  static void FillRaw(Dictionary<int,DamageVfxIntensity> result,IReadOnlyList<GameEvent> log,int start,string why) {
   for(int i=start;i<log.Count;i++)if(TryDamage(log[i],out int raw,out _,out _,out _))result[i]=DamageVfxIntensity.Evaluate(raw,null,null,why);
  }
  static bool TryDamage(GameEvent ev,out int raw,out int enemy,out bool player,out string invalid) {
   raw=0;enemy=-1;player=false;invalid=null;
   switch(ev) {
    case GameEvent_DamageDealt d:
     raw=d.HpLoss;
     if(d.Source=="enemy")player=true;
     else if(d.Source=="player") {if(d.EnemyIndex.HasValue)enemy=d.EnemyIndex.Value;else invalid="missing_enemy_index";}
     else invalid="invalid_damage_source";
     return true;
    case GameEvent_ConfusedAttack c:raw=c.HpLoss;enemy=c.TargetIndex;return true;
    case GameEvent_BurnTick b:raw=b.Amount;enemy=b.EnemyIndex;return true;
    case GameEvent_ScaldTick s:raw=s.Amount;player=true;return true;
    case GameEvent_ThornsReflected t:raw=t.HpLoss;player=true;return true;
    default:return false;
   }
  }
  static Target Enemy(Target[] enemies,int n)=>n>=0&&n<enemies.Length?enemies[n]:null;
  static void AddEnemy(Target player,Target[] enemies,int n,long delta) {
   var target=Enemy(enemies,n);if(target==null)RejectAll(player,enemies,"unresolved_heal_target",true);else Add(target,delta);
  }
  static void Add(Target t,long delta){t.Hp+=delta;if(t.Hp<Int32.MinValue || t.Hp>Int32.MaxValue)t.Reject("hp_replay_overflow");}
  static void Validate(Target t){if(t.Hp!=t.FinalHp)t.Reject("hp_replay_mismatch");}
  static void RejectAll(Target player,Target[] enemies,string reason,bool identity){player.Reject(reason,identity);foreach(var t in enemies)t.Reject(reason,identity);}
  static bool KnownNoHpChange(GameEvent ev) => NoHpTypes.Contains(ev.GetType());
  static readonly HashSet<Type> NoHpTypes=new HashSet<Type> {
   // Generated current event schema only. In particular EnemyDied does not zero HP,
   // HpLossCapped is informational, and GuardianRedirected is followed by targetful damage.
   typeof(GameEvent_TurnStarted), typeof(GameEvent_TurnEnded), typeof(GameEvent_CardsDrawn),
   typeof(GameEvent_CardPlayed), typeof(GameEvent_CardSet), typeof(GameEvent_Scried),
   typeof(GameEvent_TrapLifeExtended), typeof(GameEvent_SetCardExpired), typeof(GameEvent_EnemyIntentDeclared),
   typeof(GameEvent_EnemyActionExecuting), typeof(GameEvent_EnemyActionResolved), typeof(GameEvent_ActionNegated),
   typeof(GameEvent_EnemyConfused), typeof(GameEvent_BlockGained), typeof(GameEvent_IceBlockGained),
   typeof(GameEvent_AetherGained), typeof(GameEvent_SpellEchoed), typeof(GameEvent_NecroFired),
   typeof(GameEvent_NecroPlayed), typeof(GameEvent_AetherDischarged), typeof(GameEvent_LightGained),
   typeof(GameEvent_LightDischarged), typeof(GameEvent_LightSpent), typeof(GameEvent_DiscountGained),
   typeof(GameEvent_BurnApplied), typeof(GameEvent_StatusInflicted), typeof(GameEvent_GuardianRedirected),
   typeof(GameEvent_ArtifactBlocked), typeof(GameEvent_BurrowBroken), typeof(GameEvent_EnemyStaggered),
   typeof(GameEvent_EnemyInterrupted), typeof(GameEvent_RegenBroken), typeof(GameEvent_BlockShattered),
   typeof(GameEvent_ImpulseDrawn), typeof(GameEvent_StrengthGained), typeof(GameEvent_EnergyGained),
   typeof(GameEvent_MomentumAdded), typeof(GameEvent_PermanentPlayed), typeof(GameEvent_CardExhausted),
   typeof(GameEvent_CardsAddedToHand), typeof(GameEvent_CardsAddedToDiscard), typeof(GameEvent_DeckCardTransformed),
   typeof(GameEvent_CardsAddedToDraw), typeof(GameEvent_ExhaustRecycled), typeof(GameEvent_BurnDischarged),
   typeof(GameEvent_TokenDestroyed), typeof(GameEvent_RetainerSacrificed), typeof(GameEvent_RetainersDuplicated),
   typeof(GameEvent_RetainersTriggered), typeof(GameEvent_RetainerExpired), typeof(GameEvent_RetainerCopied),
   typeof(GameEvent_RetainerLifeExtended), typeof(GameEvent_PermanentDismissed), typeof(GameEvent_RetainerRushed),
   typeof(GameEvent_GoldStolen), typeof(GameEvent_CardRetrieved), typeof(GameEvent_CardPlayedFromExhaust),
   typeof(GameEvent_CardsDiscarded), typeof(GameEvent_EnergyMaxGained), typeof(GameEvent_GrowthAdded),
   typeof(GameEvent_SetSlotGained), typeof(GameEvent_CardsMovedToHand), typeof(GameEvent_CardCopied),
   typeof(GameEvent_CardGrew), typeof(GameEvent_CardUpgradedInHand), typeof(GameEvent_GrowthDischarged),
   typeof(GameEvent_MomentumDischarged), typeof(GameEvent_CardsMilled), typeof(GameEvent_CardSealed),
   typeof(GameEvent_CardUnsealed), typeof(GameEvent_EnemyWeakened), typeof(GameEvent_ExposedApplied),
   typeof(GameEvent_ReactionTriggered), typeof(GameEvent_ReactionUnaffordable), typeof(GameEvent_ReactionHeld),
   typeof(GameEvent_ReactionWhiffed), typeof(GameEvent_SetCardDestroyed), typeof(GameEvent_EnemyPhaseEnded),
   typeof(GameEvent_DeckShuffled), typeof(GameEvent_EnemyDied), typeof(GameEvent_GearUsed),
   typeof(GameEvent_HpLossCapped), typeof(GameEvent_PlayerArtifactBlocked), typeof(GameEvent_CombatEnded),
  };
 }
}
