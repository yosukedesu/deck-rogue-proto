// Stage41 source: PhysicalSlashFx/VineSlashFx ticket.Index parity; WaterContinuousFx captured source side.
using Godot;
namespace DeckRogue.GodotPort.Presentation;
public static class SourceAttackOrientation
{
    public static bool AlternateHit(int contactIndex) => contactIndex >= 0 && contactIndex % 2 != 0;
    public static bool IncomingFromRight(Vector3 sourceLocal) => sourceLocal.IsFinite() && sourceLocal.X > .00001f;
}
