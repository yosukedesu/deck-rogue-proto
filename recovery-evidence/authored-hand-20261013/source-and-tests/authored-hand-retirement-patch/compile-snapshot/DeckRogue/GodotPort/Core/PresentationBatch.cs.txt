using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.Core;

public sealed record PresentationBatch(long Revision, RunState? Previous, RunState Current, IReadOnlyList<GameEvent> Events, bool IsReset);
