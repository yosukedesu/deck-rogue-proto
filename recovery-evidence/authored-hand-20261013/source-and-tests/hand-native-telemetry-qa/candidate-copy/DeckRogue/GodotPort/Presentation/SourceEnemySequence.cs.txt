// Reconstructed from Presenter.PlayTracked/MainSequenceGap (preserved Unity source).
#nullable enable
using System;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.Presentation;
public static class SourceEnemySequence {
 static readonly HashSet<string> Status=new(new[]{"StatusInflicted","ExposedApplied","EnemyWeakened","BurnApplied","StrengthGained","GrowthAdded","MomentumAdded","LightGained","LightDischarged","LightSpent","ArtifactBlocked","PlayerArtifactBlocked"});
 static readonly HashSet<string> Trap=new(new[]{"ReactionTriggered","ReactionHeld","SetCardExpired","SetCardDestroyed","ReactionWhiffed","ActionNegated","GearUsed","DeathSaved","RetainerRushed","TokenDestroyed","RetainerSacrificed","RetainerExpired","RetainerCopied","RetainerLifeExtended"});
 static readonly HashSet<string> EnemyAct=new(new[]{"EnemyActionExecuting","EnemyHealed","GoldStolen","CardsMilled","ThornsReflected","ScaldTick","BurnTick","RegenTicked","BlockShattered","EnemyStaggered","EnemyInterrupted","EnemyDied","EnemyFled"});
 public static bool HasPresentation(GameEvent e,ISet<string>? soundKeys=null)=>e is GameEvent_DamageDealt or GameEvent_BlockGained or GameEvent_IceBlockGained or GameEvent_HpHealed or GameEvent_TurnStarted or GameEvent_TurnEnded or GameEvent_EnemyIntentDeclared or GameEvent_HpLost||Status.Contains(e.Type)||Trap.Contains(e.Type)||EnemyAct.Contains(e.Type)||(e.Type!="CardsDrawn"&&e.Type!="CardSet"&&soundKeys?.Contains(e.Type)==true);
 public static float Gap(GameEvent e)=>e.Type switch {"DamageDealt"=>.4f,"TurnEnded" or "TurnStarted"=>.6f,"BlockGained" or "IceBlockGained" or "HpHealed"=>.15f,"ReactionTriggered"=>.55f,"EnemyActionExecuting"=>.32f,"EnemyInterrupted"=>.55f,_=>Status.Contains(e.Type)||Trap.Contains(e.Type)?.3f:EnemyAct.Contains(e.Type)?.25f:.12f};
 public static (float[] Onsets,float End) Plan(IReadOnlyList<GameEvent> events,ISet<string>? soundKeys=null){
  var times=new float[events.Count];float delay=.02f;
  for(int i=0;i<events.Count;i++){times[i]=events[i] is GameEvent_CardPlayed?0:delay;if(HasPresentation(events[i],soundKeys))delay+=Gap(events[i]);}
  return(times,delay+.01f);
 }
}
