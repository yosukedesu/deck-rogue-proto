using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public sealed record SourceSetToken(CardInstance Card, bool Live, bool Ready, int? Windows, string State, string Details);
