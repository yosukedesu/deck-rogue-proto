using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public sealed record SourceGearToken(GearInstance Gear, GearDef Definition, string? Blocked, string Details);
