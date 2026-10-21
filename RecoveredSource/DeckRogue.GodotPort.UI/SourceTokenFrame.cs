using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public sealed record SourceTokenFrame(RunState Board, long Revision, GameState? SetRedraw = null, long Epoch = 0L);
