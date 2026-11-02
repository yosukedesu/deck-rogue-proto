namespace DeckRogue.GodotPort.Presentation;

public sealed record SemanticEventCue(string EventType, string Text, string Anchor, int? EnemyIndex, string Color, float Lifetime);
