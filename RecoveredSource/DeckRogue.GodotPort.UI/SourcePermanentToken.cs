using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public sealed record SourcePermanentToken(CardInstance Card, bool Doll, int? Life, bool HasGrowth, string Details);
