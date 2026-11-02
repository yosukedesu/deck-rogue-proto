using System.Collections.Generic;

namespace DeckRogue.GodotPort.UI;

public sealed record SourceTokenData(SourceTokenFrame Frame, int SetSlots, IReadOnlyList<SourceSetToken> Sets, IReadOnlyList<SourceGearToken> Gears, IReadOnlyList<SourcePermanentToken> Permanents, string? GearMute, string Stamp);
