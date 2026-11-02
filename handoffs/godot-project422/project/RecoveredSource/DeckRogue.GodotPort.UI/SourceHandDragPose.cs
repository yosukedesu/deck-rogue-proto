using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class SourceHandDragPose
{
	public const float FollowScale = 0.8f;

	public static bool UsesStationaryAim(bool phone, CardInstance card)
	{
		if (phone && card?.Def != null && (card.Def.Modes == null || card.Def.Modes.Count == 0))
		{
			return Effects.CardNeedsTarget(card);
		}
		return false;
	}
}
