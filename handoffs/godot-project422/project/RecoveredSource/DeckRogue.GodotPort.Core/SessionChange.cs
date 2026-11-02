using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.Core;

public sealed record SessionChange(RunState? Previous, RunState Current, RunCommand? Command, IReadOnlyList<GameEvent> Events, long Revision, bool IsReset);
