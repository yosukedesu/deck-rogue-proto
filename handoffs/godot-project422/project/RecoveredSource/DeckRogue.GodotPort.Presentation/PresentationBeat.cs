using DeckRogue.Engine.Generated;
using DeckRogue.Game;

namespace DeckRogue.GodotPort.Presentation;

public sealed record PresentationBeat(GameEvent Event, AttackRoute? Route, EffectScenario? Scenario, bool WinningHit, float Duration, float ContactAt, EnemyAttackContactPlan? Contacts = null);
