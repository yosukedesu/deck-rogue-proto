using Godot;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceAttackOrientation
{
	public static bool AlternateHit(int contactIndex)
	{
		if (contactIndex >= 0)
		{
			return contactIndex % 2 != 0;
		}
		return false;
	}

	public static bool IncomingFromRight(Vector3 sourceLocal)
	{
		if (sourceLocal.IsFinite())
		{
			return sourceLocal.X > 1E-05f;
		}
		return false;
	}
}
