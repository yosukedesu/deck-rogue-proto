using System;
using UnityEngine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game {
 // Exactly one receipt per resolved engine event; card names/effect names cannot suppress it.
 public static class ResolvedCombatFx {
  public sealed class Ticket {
   public string Event,Target,Style="slash",Outcome,Element="physical",SourceKey="event",SourceKind,SourceUid,FallbackReason,AuthoredAttack,AuthoredPresentation,RenderPath,RenderFallbackReason,AuthoredPolicyReason,AuthoredSemanticReason,TargetBindingReason; public bool ExactSource;
   public string AuthoredRenderer,AuthoredTier,AuthoredTierReason;
   public string AttackFamily=>Showcase?.AuthoredAttack??AuthoredAttack; public int Amount,Index,Total=1;
   public Color Color=CombatPalette.Silver; public Vector3 Position,Feet; public float Height;
   public float VineVisibleHeight;public string VineHeightReason;
   public float ContactVisibleHeight;public string ContactHeightReason,DeliveryPreparation;
   public string SystemResponseCandidate,SystemResponseFamily,SystemResponseEvidence;
   public bool OutcomeExact,OutcomeRendered,SystemTargetUnproved;public string OutcomeReason,OutcomeHeightReason,OutcomeIncomingReason;public float OutcomeVisibleHeight;
   public string SourceUnit,BeamAnchorReason;public Vector3 BeamOrigin;public bool HasBeamOrigin;
   public Vector3 RecoveryPosition; public Vector2 Ui; public bool HasWorld,Shown,Finishing,Heavy,AuthoredAttackShown;
   internal bool GroundCaptureAttempted;internal GroundSurfaceProvider.Lease GroundLease;internal string GroundCaptureReason;
   public WorldBattleFx.AttackPlayback AttackPlayback;
   internal WaterPbfBinding PbfBinding;
   internal LightningR9Binding LightningR9Binding;
   public bool FixedSmallContact=>DamageVfxIntensity.IsFixedSmallContact(AuthoredPresentation);
   public DamageVfxIntensity Intensity; public DamageVfxIntensity.Visual Visual=>DamageVfxIntensity.VisualFor(Intensity,AuthoredPresentation);
   public CardVfxProfile.Showcase Showcase; public Stage.ImpactCapture Impact; public bool HadCapturedTarget; public float RequestedPause,AppliedPause;
   // Visual children retain the actual event name and provenance; they are never engine receipts.
   public bool EnemyContribution {get;internal set;} public int ContributionOrdinal {get;internal set;}=-1;
   public int ContributionCount {get;internal set;}
   public int ContributionAmount,ContributionBlock,ContributionIce,ContributionNullified,ContributionMitigated;
   public long ProjectedPriorHp,ProjectedMaxHp;
   internal Ticket ContributionParent,ContributionTemplate;internal Ticket[] ContributionChildren;
   internal bool ContributionsCancelled;
   internal Ticket CopyForContribution()=>(Ticket)MemberwiseClone();
  }
  internal static Action<Ticket> PresentedForReview;
  internal static Action<Ticket> ChildPresentedForReview;
  public static int ChildPresented {get;private set;}
  public static int Received {get;private set;} public static int Presented {get;private set;}
  public static int WorldPresented {get;private set;} public static int FallbackPresented {get;private set;}
  public static int NoPositiveRecovery {get;private set;} public static int AuthoredRecoveryPresented {get;private set;}
  public static int DuplicateSuppressed {get;private set;}
  public static bool IsResolved(GameEvent ev) => ev is GameEvent_DamageDealt || ev is GameEvent_ConfusedAttack || ev is GameEvent_BurnTick || ev is GameEvent_ScaldTick || ev is GameEvent_ThornsReflected || ev is GameEvent_HpLost || ev is GameEvent_HpHealed || ev is GameEvent_EnemyHealed || ev is GameEvent_RegenTicked;
  public static Ticket Capture(GameRoot g,GameEvent ev) {
   if(!IsResolved(ev))return null;
   var t=new Ticket{Event=ev.Type,Target="player",Outcome="hit"};
   switch(ev) {
    case GameEvent_HpHealed b:t.Amount=b.Amount;t.Element="heal";t.Outcome="heal";t.Color=ElementBattleFx.ColorFor("heal",Color.white);break;
    case GameEvent_EnemyHealed b:t.Target="enemy"+b.TargetIndex;t.Amount=b.Amount;t.Element="heal";t.Outcome="heal";t.Color=ElementBattleFx.ColorFor("heal",Color.white);break;
    case GameEvent_RegenTicked b:t.Target="enemy"+b.EnemyIndex;t.Amount=b.Amount;t.Element="heal";t.Outcome="heal";t.Color=ElementBattleFx.ColorFor("heal",Color.white);break;
    case GameEvent_DamageDealt d:
     t.SourceUnit=d.Source=="enemy" && d.EnemyIndex.HasValue?"enemy"+d.EnemyIndex.Value:d.Source=="player"?"player":null;
     t.Target=d.Source=="player"?"enemy"+(d.EnemyIndex??0):"player";t.Amount=d.HpLoss;t.Heavy=d.Amount>=15||d.Exposed==true;
     if(d.HpLoss<=0)t.Outcome=(d.Blocked??0)>0?"guard":"nullified";
     break;
    case GameEvent_ConfusedAttack c:
     t.SourceUnit="enemy"+c.EnemyIndex;
     t.Target="enemy"+c.TargetIndex;t.Amount=c.HpLoss;t.Heavy=c.HpLoss>=15;
     if(c.HpLoss<=0)t.Outcome=c.Blocked>0?"guard":"nullified";
     break;
    case GameEvent_BurnTick b:t.Target="enemy"+b.EnemyIndex;t.Amount=b.Amount;t.Style="spark";t.Element="fire";t.Color=new Color(1,.28f,.065f);break;
    case GameEvent_ScaldTick b:t.Amount=b.Amount;t.Outcome="affliction";t.Style="local_loss";t.Element="neutral";t.Color=new Color(.69f,.737f,.761f);break;
    case GameEvent_ThornsReflected b:t.SourceUnit="enemy"+b.EnemyIndex;t.Amount=b.HpLoss;t.Style="vine";if(b.HpLoss<=0)t.Outcome="guard";break;
    case GameEvent_HpLost b:t.Amount=b.Amount;t.Outcome="cost";t.Style="local_loss";t.Element="neutral";t.Color=new Color(.69f,.737f,.761f);break;
   }
   if((t.Outcome=="hit" && t.Amount>0 && (ev is GameEvent_ThornsReflected || ev is GameEvent_BurnTick)) || ev is GameEvent_HpLost || ev is GameEvent_ScaldTick){
    t.SystemResponseCandidate=ev is GameEvent_ThornsReflected?"reflect":ev is GameEvent_BurnTick?"burn":ev is GameEvent_HpLost?"selfloss":"affliction";t.SystemResponseFamily=CombatOutcome.PositiveSystemFamily(ev,out t.SystemResponseEvidence);
    if(ev is GameEvent_BurnTick tick && t.SystemResponseFamily!=null){
     var state=g.Rs?.Combat;var receipt=CombatPresentationLedger.Read(ev);
     if(state==null || tick.EnemyIndex<0 || tick.EnemyIndex>=state.Enemies.Count || state.Enemies[tick.EnemyIndex].EnemyId!=receipt.EnemyId)InvalidateSystemTarget(t,"system_enemy_identity_unproved");
    }
   }
   var outcome=CombatOutcome.Read(ev);
   if(t.Outcome=="guard" || t.Outcome=="nullified") {
    if(outcome.Kind!=null)t.Outcome=outcome.Kind;
    t.OutcomeExact=outcome.Exact;t.OutcomeReason=outcome.Reason;
   }
   if(t.Amount<=0 && t.Outcome=="hit"){t.Outcome="nullified";t.OutcomeReason=outcome.Reason??"outcome_event_outside_scope";}
   if(t.Outcome=="hit")t.Intensity=DamageVfxIntensity.Evaluate(t.Amount);
   t.HasWorld=Stage.TryGetUnitBox(t.Target,out var feet,out var h);t.Height=h;t.Feet=feet;
   t.Position=feet+(Stage.LayoutRotation*Vector3.up)*h*.5f-(Stage.LayoutRotation*Vector3.forward)*.28f;
   if(t.Outcome=="heal") {
    // These events report actual restored HP, including zero at the cap. No card
    // attribution is needed or claimed; healing never inherits an offensive route.
    t.SourceKey="event:"+ev.Type;t.SourceKind="recovery_event";
    t.RecoveryPosition=feet+(Stage.LayoutRotation*Vector3.up)*h*.4652173913043479f;
    if(t.Amount>0 && CardVfxProfile.ShowcaseOn){t.AuthoredAttack="heal_uplift";t.AuthoredPresentation=t.Amount<=4?"small":null;}
   }
   var anchor=g.Anchor(t.Target);
   // A resolved impact can outlive its actor. Retain its position, never reselect another enemy.
   t.Ui=anchor!=null && g.FxLayer!=null?Tween.CenterIn(anchor,g.FxLayer):new Vector2(t.Target=="player"?-240:240,100);
   t.Impact=Stage.CaptureImpact(t.Target);t.HadCapturedTarget=t.Impact?.Target!=null;
   if(t.Outcome=="heal" && t.AuthoredAttack=="heal_uplift")Stage.TryCapturedVisibleHeight(t.Impact,out t.ContactVisibleHeight,out t.ContactHeightReason);
   if(t.Outcome=="guard" || t.Outcome=="nullified" || t.SystemResponseCandidate!=null || ev is GameEvent_DamageDealt incoming && incoming.Source=="enemy")Stage.TryCapturedVisibleHeight(t.Impact,out t.OutcomeVisibleHeight,out t.OutcomeHeightReason);
   // Capture the caster at the same instant as the target. A later queued route
   // must not resolve a replacement actor from a reused enemy index.
   t.HasBeamOrigin=WorldBattleFx.TryCaptureBoardCenter(t.SourceUnit,out t.BeamOrigin);t.BeamAnchorReason=t.HasBeamOrigin?"captured_board_center_fallback":"source_anchor_unavailable";
   Received++;return t;
  }
  static int IntensityInt(long value)=>(int)Math.Max(int.MinValue,Math.Min(int.MaxValue,value));
  // Caller must first begin the validated numeric projection, and recheck its owner
  // before each Show. This factory only projects supplied, real contribution data.
  public static Ticket CreateEnemyContribution(Ticket outer,EnemyAttackContribution row,int ordinal,int total,long projectedPriorHp,long maxHp) {
   if(outer==null || outer.EnemyContribution || outer.Event!="DamageDealt" || outer.Target!="player" || outer.SourceUnit==null)throw new ArgumentException("enemy_contribution_outer_invalid",nameof(outer));
   if(row==null || row.AmountContribution<0 || row.HpLossContribution<0 || row.BlockContribution<0 || row.IceContribution<0 || row.NullifiedContribution<0 || row.MitigatedContribution<0 || (long)row.AmountContribution!=(long)row.HpLossContribution+row.BlockContribution+row.IceContribution+row.NullifiedContribution+row.MitigatedContribution)throw new ArgumentException("enemy_contribution_row_invalid",nameof(row));
   if(total<2 || total>12 || ordinal<0 || ordinal>=total || maxHp<=0)throw new ArgumentOutOfRangeException("enemy_contribution_bounds_invalid");
   if(outer.ContributionsCancelled)throw new InvalidOperationException("enemy_contribution_cancelled");
   if(outer.ContributionChildren!=null) {
    if(outer.ContributionChildren.Length!=total)throw new InvalidOperationException("enemy_contribution_count_changed");
    var previous=outer.ContributionChildren[ordinal];
    if(previous!=null) {
     if(previous.Amount!=row.HpLossContribution || previous.ContributionAmount!=row.AmountContribution || previous.ContributionBlock!=row.BlockContribution || previous.ContributionIce!=row.IceContribution || previous.ContributionNullified!=row.NullifiedContribution || previous.ContributionMitigated!=row.MitigatedContribution || previous.ProjectedPriorHp!=projectedPriorHp || previous.ProjectedMaxHp!=maxHp)throw new InvalidOperationException("enemy_contribution_identity_changed");
     return previous;
    }
   }
   if(outer.Shown)throw new InvalidOperationException("enemy_contribution_outer_already_shown");
   // Freeze once after routing. Each child owns independent mutable playback and
   // render state while retaining the same captured target/source leases and UI.
   var template=outer.ContributionTemplate??outer;
   var t=template.CopyForContribution();
   t.ContributionParent=outer;t.ContributionTemplate=null;t.ContributionChildren=null;t.ContributionsCancelled=false;
   t.EnemyContribution=true;t.ContributionOrdinal=ordinal;t.ContributionCount=total;t.Index=ordinal;t.Total=total;
   t.Amount=row.HpLossContribution;t.ContributionAmount=row.AmountContribution;t.ContributionBlock=row.BlockContribution;t.ContributionIce=row.IceContribution;t.ContributionNullified=row.NullifiedContribution;t.ContributionMitigated=row.MitigatedContribution;
   t.ProjectedPriorHp=projectedPriorHp;t.ProjectedMaxHp=maxHp;
   t.Outcome=t.Amount>0?"hit":(long)row.BlockContribution+row.IceContribution>0?"guard":"nullified";
   t.OutcomeExact=true;t.OutcomeReason="ordered_contribution_projection";
   t.Intensity=t.Amount>0?DamageVfxIntensity.Evaluate(t.Amount,IntensityInt(projectedPriorHp),IntensityInt(maxHp),"ordered_contribution_projection"):null;
   t.Heavy=t.Amount>=15;t.Finishing=false; // Projected player depletion is not an engine terminal decision.
   t.Shown=false;t.AttackPlayback=null;t.AuthoredAttackShown=false;t.OutcomeRendered=false;
   t.AuthoredRenderer=null;t.AuthoredTier=null;t.AuthoredTierReason=null;t.RenderPath=null;t.RenderFallbackReason=t.AuthoredPolicyReason;t.TargetBindingReason=null;t.OutcomeIncomingReason=null;
   // Ordinary enemy routes have no Showcase. Keep per-row contacts free of a
   // copied aggregate pause envelope even if the outer preview carried one.
   t.AuthoredAttack=template.AttackFamily;t.Showcase=null;t.RequestedPause=0;t.AppliedPause=0;
   if(outer.ContributionChildren==null){outer.ContributionTemplate=outer.CopyForContribution();outer.ContributionChildren=new Ticket[total];outer.ContributionCount=total;}
   outer.ContributionChildren[ordinal]=t;return t;
  }
  // Rendering failure still consumes a child's one attempt (Shown); missing or
  // cancelled children cannot acknowledge an unfinished/cancelled engine event.
  public static bool CompleteEnemyContributions(Ticket outer) {
   if(outer==null || outer.EnemyContribution || outer.ContributionChildren==null || outer.ContributionsCancelled)return false;
   if(outer.Shown){DuplicateSuppressed++;return false;}
   foreach(var child in outer.ContributionChildren)if(child==null || !child.Shown)return false;
   outer.Shown=true;outer.RenderPath="enemy-contribution-projection";Presented++;PresentedForReview?.Invoke(outer);return true;
  }
  public static void CancelEnemyContributions(Ticket outer) {
   if(outer==null || outer.EnemyContribution || outer.Shown)return;
   outer.ContributionsCancelled=true;
  }
  static void RecordPresented(Ticket t) {
   if(t.EnemyContribution){ChildPresented++;ChildPresentedForReview?.Invoke(t);}
   else {Presented++;PresentedForReview?.Invoke(t);}
  }
  public static void InvalidateSystemTarget(Ticket ticket,string reason){
   if(ticket?.SystemResponseCandidate==null)return;
   ticket.SystemResponseFamily=null;ticket.SystemResponseEvidence=reason;ticket.SystemTargetUnproved=true;
  }
  public static void ApplyRoute(Ticket ticket,CardVfxProfile.Route route) {
   if(ticket==null || route==null)return;
   ticket.Style=route.Style;ticket.Element=route.Element;ticket.SourceKey=route.SourceKey;
   ticket.ExactSource=route.ExactSource;ticket.FallbackReason=route.FallbackReason;ticket.SourceKind=route.SourceKind;ticket.SourceUid=route.SourceUid;
   ticket.AuthoredAttack=CardVfxProfile.ShowcaseOn?route.AuthoredAttack:null;
   if(ticket.AuthoredAttack=="vine_slash")Stage.TryCapturedVisibleHeight(ticket.Impact,out ticket.VineVisibleHeight,out ticket.VineHeightReason);
   if(BoundedContactFx.Eligible(ticket)||LocalMaterialProfileFx.Eligible(ticket)||WideProfileFx.Eligible(ticket)||ScatterProfileFx.Eligible(ticket)||LightningDischargeFx.Eligible(ticket)||EarthStompFx.Eligible(ticket)||IceCrystalFx.Eligible(ticket)||LightRayFx.Eligible(ticket)||DarkDrainFx.Eligible(ticket)||FireContinuousFx.Eligible(ticket)||WaterContinuousFx.Eligible(ticket)||PhysicalSlashFx.Eligible(ticket)||EnemyContactFx.IsFamily(ticket.AuthoredAttack)||PlayerContactFx.IsFamily(ticket.AuthoredAttack))Stage.TryCapturedVisibleHeight(ticket.Impact,out ticket.ContactVisibleHeight,out ticket.ContactHeightReason);
   ticket.AuthoredPresentation=route.AuthoredPresentation;ticket.AuthoredSemanticReason=route.AuthoredSemanticReason;ticket.RenderFallbackReason=route.AuthoredFallbackReason;ticket.AuthoredPolicyReason=route.AuthoredFallbackReason;
   ticket.Color=ElementBattleFx.ColorFor(route.Element,route.Def!=null?CombatPalette.FromCard(route.Style,route.Def):route.Element=="physical"?CombatPalette.Silver:PaperFx.Mana);
  }
  static bool SafeNeutralReceipt(Ticket t)=>t!=null && t.Event=="DamageDealt" && t.Outcome=="hit" && t.Amount>0 &&
   (BoundedContactFx.Eligible(t) || !t.ExactSource || t.AuthoredPolicyReason=="authored_requires_source_metadata" || t.AuthoredPolicyReason=="authored_requires_card_metadata");
  public static void Show(GameRoot g,RectTransform fx,Ticket t) {
   if(t==null)return;
   if(t.ContributionsCancelled || t.ContributionParent?.ContributionsCancelled==true)return;
   if(t.Shown || !t.EnemyContribution && t.ContributionChildren!=null){DuplicateSuppressed++;return;}t.Shown=true;
   if(t.Outcome=="heal" && t.Amount<=0) {
    // Retain exactly one observable receipt, but no recovery pulse/light for capped
    // or invalid restoration. Engine onHealed, counters and numeric UI are untouched.
    t.RenderPath="no-positive-recovery";NoPositiveRecovery++;RecordPresented(t);
    if(Debug.isDebugBuild)Debug.Log("[ResolvedVfx] event="+t.Event+" target="+t.Target+" outcome=heal amount="+t.Amount+" art=no-positive-recovery visible=false");
    return;
   }
   if((t.SystemResponseCandidate=="selfloss"||t.SystemResponseCandidate=="affliction")&&t.Amount<=0){t.RenderPath="no-positive-system-loss";t.RenderFallbackReason="system_no_positive_hp_loss";RecordPresented(t);return;}
   bool done=false;
   bool attached=!t.SystemTargetUnproved && (!t.HasWorld || Stage.ImpactTargetCurrent(t.Target,t.Impact));
   t.TargetBindingReason=t.SystemTargetUnproved?"system_numeric_target_unproved":attached?null:"captured_visual_target_replaced";
   if(t.HasWorld && (t.Outcome!="cost" || t.SystemResponseCandidate!=null) && !t.SystemTargetUnproved) {
    if(t.Outcome=="heal") {
     // Recovery is a contact-only response: age0 now, no predictive windup, HP delay,
     // damage flash or actor pause. Keep old fallback position if the package is absent.
     if(t.AttackFamily=="heal_uplift") {
      t.AttackPlayback=HealContinuousFx.Begin(t);
      t.AuthoredAttackShown=t.AttackPlayback!=null && t.AttackPlayback.Land();
      if(!t.AuthoredAttackShown){t.AttackPlayback=WorldBattleFx.AuthoredAttackAt(t.RecoveryPosition,t.Feet,t.Height,t.AttackFamily,-1f,0f,t.Target,false,t.Impact?.Target,t.AuthoredPresentation);t.AuthoredAttackShown=t.AttackPlayback!=null && t.AttackPlayback.Land();}
     }
     if(t.AuthoredAttackShown){done=true;t.RenderPath=t.AuthoredRenderer=="continuous-heal-uplift-r1"?"continuous-heal-uplift-r1:heal_uplift":"authored-v2:heal_uplift";AuthoredRecoveryPresented++;}
     else {if(t.AttackFamily!=null && t.RenderFallbackReason==null)t.RenderFallbackReason="authored_family_unavailable";done=WorldBattleFx.ElementAt(t.Position,t.Height,"heal");if(done)t.RenderPath="element:heal";}
    }
    else if(t.Outcome=="guard" || t.Outcome=="nullified"){
     t.OutcomeRendered=CardVfxProfile.ShowcaseOn && SystemOutcomeFx.TryPlay(t);done=t.OutcomeRendered;
     if(done)t.RenderPath="continuous-outcome-r1:"+t.Outcome;
     else {
      t.RenderFallbackReason=!t.OutcomeExact?t.OutcomeReason:!CardVfxProfile.ShowcaseOn?"outcome_showcase_disabled":SystemOutcomeFx.LastFailure;
      if(t.Outcome=="guard"){done=WorldBattleFx.GuardAt(t.Position,t.Height,t.Target=="player"?1:-1);if(done)t.RenderPath="legacy-guard-fallback";}
     }
    }
    else {
     if(t.SystemResponseCandidate!=null){
      t.OutcomeRendered=t.SystemResponseFamily!=null && CardVfxProfile.ShowcaseOn && SystemOutcomeFx.TryPlayDamage(t);
      if(t.OutcomeRendered){done=true;t.RenderPath="continuous-system-r1:"+t.SystemResponseFamily;}
      else t.RenderFallbackReason=t.SystemResponseFamily==null?t.SystemResponseEvidence:!CardVfxProfile.ShowcaseOn?"system_showcase_disabled":SystemOutcomeFx.LastFailure;
     }
     if(!done && t.Outcome!="cost" && t.Outcome!="affliction"){
     // Validate the preview's pool generation; a recycled/cancelled slot is never a visible receipt.
     t.AuthoredAttackShown=t.AttackPlayback!=null && t.AttackPlayback.Land();
     if(!t.AuthoredAttackShown) {
      t.AttackPlayback=WorldBattleFx.AuthoredAttackContact(t);
      t.AuthoredAttackShown=t.AttackPlayback!=null && t.AttackPlayback.Land();
     }
     if(!t.AuthoredAttackShown && LightningDischargeFx.Eligible(t)){t.AttackPlayback=WorldBattleFx.StandardLightningContact(t);t.AuthoredAttackShown=t.AttackPlayback!=null && t.AttackPlayback.Land();}
     if(!t.AuthoredAttackShown && IceCrystalFx.Eligible(t)){t.AttackPlayback=WorldBattleFx.StandardIceCrystalContact(t);t.AuthoredAttackShown=t.AttackPlayback!=null && t.AttackPlayback.Land();}
     if(!t.AuthoredAttackShown && LightRayFx.Eligible(t)){t.AttackPlayback=WorldBattleFx.StandardLightRayContact(t);t.AuthoredAttackShown=t.AttackPlayback!=null && t.AttackPlayback.Land();}
     if(!t.AuthoredAttackShown && DarkDrainFx.Eligible(t)){t.AttackPlayback=WorldBattleFx.StandardDarkDrainContact(t);t.AuthoredAttackShown=t.AttackPlayback!=null && t.AttackPlayback.Land();}
     if(!t.AuthoredAttackShown && FireContinuousFx.Eligible(t) && t.AttackFamily=="fire"){t.AttackPlayback=WorldBattleFx.StandardFireContact(t);t.AuthoredAttackShown=t.AttackPlayback!=null && t.AttackPlayback.Land();}
     if(!t.AuthoredAttackShown && WaterContinuousFx.Eligible(t)){t.AttackPlayback=WorldBattleFx.StandardWaterContact(t);t.AuthoredAttackShown=t.AttackPlayback!=null && t.AttackPlayback.Land();}
     if(!t.AuthoredAttackShown && PhysicalSlashFx.Eligible(t)){t.AttackPlayback=WorldBattleFx.StandardSlashContact(t);t.AuthoredAttackShown=t.AttackPlayback!=null && t.AttackPlayback.Land();}
     if(t.AuthoredAttackShown){done=true;t.RenderPath=(t.AuthoredRenderer=="bounded-contact-r1"?"bounded-contact-r1:":t.AuthoredRenderer=="continuous-local-material-r1"?"continuous-local-material-r1:":t.AuthoredRenderer=="continuous-wide-profile-r2"?"continuous-wide-profile-r2:":t.AuthoredRenderer=="continuous-scatter-profile-r2"?"continuous-scatter-profile-r2:":t.AuthoredRenderer==LightningR9CandidateFx.RendererId?"procedural-lightning-r9-medium-candidate:":t.AuthoredRenderer=="continuous-lightning-discharge-r2"?"continuous-lightning-discharge-r2:":t.AuthoredRenderer=="continuous-ground-r2"?"continuous-ground-r2:":t.AuthoredRenderer=="continuous-ice-crystal-r2"?"continuous-ice-crystal-r2:":t.AuthoredRenderer=="continuous-light-ray-r2"?"continuous-light-ray-r2:":t.AuthoredRenderer=="continuous-dark-drain-r1"?"continuous-dark-drain-r1:":t.AuthoredRenderer=="continuous-fire-modes-r1"?"continuous-fire-modes-r1:":t.AuthoredRenderer=="continuous-water-flow-r1"||t.AuthoredRenderer=="cached-water-pbf-r4-candidate"?"continuous-water-flow-r1:":t.AuthoredRenderer==PhysicalMediumCandidateFx.RendererId?"procedural-physical-medium-r2-candidate:":t.AuthoredRenderer=="continuous-physical-slash-tiers-r1"?"continuous-physical-slash-tiers-r1:":t.AttackFamily=="vine_slash"?"continuous-mesh-r1:":PlayerContactFx.IsFamily(t.AttackFamily)?"continuous-physical-delivery-r1:":EnemyContactFx.IsFamily(t.AttackFamily)?(t.AttackFamily=="enemy_beam"?"continuous-neutral-beam-r1:":"continuous-physical-r1:"):"authored-v2:")+t.AttackFamily;}
     else if(!SafeNeutralReceipt(t)) {
      if(t.AttackFamily!=null && t.RenderFallbackReason==null)t.RenderFallbackReason="authored_family_unavailable";
      done=WorldBattleFx.ElementAt(t.Position,t.Height,t.Element,t.Visual);
      if(done)t.RenderPath="element:"+t.Element;
      else {
       if(t.Element!="physical" && t.Element!="arcane" && !EarthStompFx.Eligible(t))t.RenderFallbackReason="element_or_world_resource_unavailable";
       done=WorldBattleFx.HitAt(t.Position,t.Height,t.Style,t.Color,t.Heavy,t.Index,t.Target,t.Visual);
       if(done)t.RenderPath="shared-impact:"+t.Style;
      }
     }
     }
    }
   }
   if(done){WorldPresented++;if(attached){if(t.Outcome=="hit") {if(t.AuthoredRenderer=="bounded-contact-r1"){/* Minimal receipt contact deliberately adds no scene light or actor pause. */}else if(t.OutcomeRendered){/* System response owns its single bounded light. */}else if(t.AuthoredAttackShown && (t.AuthoredRenderer==LightningR9CandidateFx.RendererId||t.AuthoredRenderer==PhysicalMediumCandidateFx.RendererId||t.AuthoredRenderer=="continuous-physical-slash-tiers-r1"||t.AuthoredRenderer=="continuous-water-flow-r1"||t.AuthoredRenderer=="cached-water-pbf-r4-candidate"||t.AuthoredRenderer=="continuous-fire-modes-r1"||t.AuthoredRenderer=="continuous-dark-drain-r1"||t.AuthoredRenderer=="continuous-light-ray-r2"||t.AuthoredRenderer=="continuous-ice-crystal-r2"||t.AuthoredRenderer=="continuous-ground-r2"||t.AuthoredRenderer=="continuous-lightning-discharge-r2"||t.AuthoredRenderer=="continuous-scatter-profile-r2"||t.AuthoredRenderer=="continuous-wide-profile-r2"||t.AuthoredRenderer=="continuous-local-material-r1")){/* Authored tier renderer owns its one bounded light. */}else if(t.AuthoredAttackShown)StageFx.AuthoredAttackHit(t.Target,t.Feet,t.Height,t.AttackFamily,t.Heavy||(t.Finishing&&(t.Element=="physical"||t.Element=="arcane")),t.AuthoredPresentation,t.Visual);else if(t.Element!="physical" && t.Element!="arcane")StageFx.ElementHit(t.Target,t.Element,t.Color,t.Heavy,t.Visual);else if(t.Target=="player")StageFx.FoeHit(t.Target,t.Color,t.Amount>=15,t.Visual);else if(t.Finishing)StageFx.Finish(t.Target,t.Color,t.FixedSmallContact?t.Visual:null);else StageFx.PlayerHit(t.Target,t.Style,t.Heavy,t.Color,t.Visual);}else if(t.Outcome=="heal"){if(!t.AuthoredAttackShown)StageFx.Cast(t.Target,"light",t.Color);}else if(t.Outcome=="guard" && !t.OutcomeRendered)StageFx.Guard(t.Target,t.Target=="player"?1:-1);}}
   else if(SafeNeutralReceipt(t)) {
    if(!t.ExactSource)t.RenderFallbackReason="unproven_source_neutral_receipt";
    else if(t.RenderFallbackReason==null)t.RenderFallbackReason=t.HasWorld?"bounded_contact_renderer_unavailable":"bounded_contact_world_target_unavailable";
    string neutralReason=t.RenderFallbackReason;
    if(t.HasWorld && Stage.TryCapturedVisibleHeight(t.Impact,out t.ContactVisibleHeight,out t.ContactHeightReason)) {
     t.AttackPlayback=BoundedContactFx.BeginNeutralFallback(t);done=t.AttackPlayback!=null && t.AttackPlayback.Land();
     if(done){t.RenderPath="world-achromatic-contact-fallback";WorldPresented++;}
    }
    if(!done){done=fx!=null && CodexBattleFx.NeutralReceiptFallback(fx,t.Ui,54*(t.Visual?.Size??1f),.18f);t.RenderPath=done?"ui-achromatic-contact-fallback":"no-render-surface";}
    if(done){FallbackPresented++;t.RenderFallbackReason=neutralReason;}
    else t.RenderFallbackReason=neutralReason+";"+(t.RenderFallbackReason??"neutral_world_unavailable")+";"+(fx==null?"neutral_receipt_ui_layer_missing":CodexBattleFx.LastFailure);
   }
   else if(fx!=null && (t.Outcome=="guard" || t.Outcome=="nullified")){
    if(t.RenderFallbackReason==null)t.RenderFallbackReason=!t.OutcomeExact?t.OutcomeReason:Stage.CombatWorld?"outcome_world_render_unavailable":"outcome_canvas_r1_unimplemented";
    if(t.Outcome=="guard")done=CodexBattleFx.Guard(fx,t.Ui,t.Target=="player"?1:-1);
    if(done)t.RenderPath="ui-guard-legacy-fallback";
    else {done=CodexBattleFx.ReceiptFallback(fx,t.Ui,PaperFx.SkyLight,70,.212f);t.RenderPath=done?(t.Outcome=="nullified"?"ui-nullify-neutral-fallback":"ui-guard-ring-fallback"):"no-render-surface";}
    if(done)FallbackPresented++;else t.RenderFallbackReason=CodexBattleFx.LastFailure;
   }
   else if(fx!=null){t.RenderPath="ui-ring";if(t.SystemTargetUnproved)t.RenderFallbackReason=t.SystemResponseEvidence;else if(t.RenderFallbackReason==null)t.RenderFallbackReason=t.HasWorld?"world_render_unavailable":"world_target_unavailable";done=CodexBattleFx.ReceiptFallback(fx,t.Ui,t.Outcome=="hit"?t.Color:PaperFx.SkyLight,90*(t.Visual?.Size??1f),.22f);if(done)FallbackPresented++;else {t.RenderPath="no-render-surface";t.RenderFallbackReason=CodexBattleFx.LastFailure;}}
   if(fx!=null && (t.Outcome=="nullified"||t.Outcome=="cost"))Tween.Float(fx,t.Ui,t.Outcome=="cost"?"HP −"+t.Amount:"無効",t.Color,24,24,.5f);
   if(done && attached && t.Outcome=="hit" && !t.OutcomeRendered && t.Showcase!=null && t.HasWorld) {
    // The new sheets already contain their own debris/residue; do not stack HDR embers over them.
    if(!t.AuthoredAttackShown)WorldBattleFx.ShowcaseResidue(t.Position,t.Height,t.Color,t.Showcase,t.Visual);
    Stage.Flash(t.Target,.075f);
    t.RequestedPause=CardVfxProfile.ImpactPauseFor(t.Showcase,t.Index,t.Total,t.Finishing);
    t.AppliedPause=Stage.ShowcaseImpact(t.Target,t.Impact,t.RequestedPause,t.Showcase.Heavy);
   }
   if(done){RecordPresented(t);}else Debug.LogError("[ResolvedVfx] missing surface "+t.Event+" "+t.Target);
   if(Debug.isDebugBuild || Environment.GetCommandLineArgs().Length>1)Debug.Log("[ResolvedVfx] event="+t.Event+" target="+t.Target+" contribution="+(t.EnemyContribution?t.ContributionOrdinal+"/"+t.ContributionCount:"outer")+" projectedPriorHp="+(t.EnemyContribution?t.ProjectedPriorHp.ToString():"none")+" projectedMaxHp="+(t.EnemyContribution?t.ProjectedMaxHp.ToString():"none")+" outcome="+t.Outcome+" style="+t.Style+" element="+t.Element+" source="+t.SourceKey+" sourceKind="+t.SourceKind+" sourceUid="+t.SourceUid+" targetBinding="+(t.TargetBindingReason??"current")+" systemResponse="+(t.SystemResponseFamily??"none")+" systemEvidence="+(t.SystemResponseEvidence??"none")+" exactOutcome="+t.OutcomeExact+" outcomeEvidence="+(t.OutcomeReason??"not-resource-response")+" incoming="+(t.OutcomeIncomingReason??"not-captured")+" groundCapture="+(t.GroundCaptureReason??(t.GroundCaptureAttempted?"captured":"not-attempted"))+" deliveryPreparation="+(t.DeliveryPreparation??"not-applicable")+" exactSource="+t.ExactSource+" fallback="+(t.FallbackReason??"none")+" authoredPresentation="+(t.AuthoredPresentation??"standard")+" semanticReason="+(t.AuthoredSemanticReason??"none")+" authoredPolicyReason="+(t.AuthoredPolicyReason??"none")+" damageTier="+(t.Intensity?.Tier??"not-damage")+" intensityHp="+(t.Intensity?.HpEvidence??"none")+" effectiveDamage="+(t.Intensity?.Effective??0)+" art="+t.RenderPath+" renderFallback="+(t.RenderFallbackReason??"none")+" visible="+done);
  }
 }
}
