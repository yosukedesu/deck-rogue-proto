using System.Collections.Generic;

namespace DeckRogue.GodotPort.UI;

public sealed record SourceTokenLayoutResult(IReadOnlyList<SourceTokenCell> Cells, IReadOnlyList<SourceTokenSection> Sections, string Adaptation);
