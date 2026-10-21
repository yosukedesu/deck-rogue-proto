using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class SourceUiPolicies
{
	public const int SparkLimit = 12;

	public static string? CommandSound(RunCommand command)
	{
		if (command is RunCommand_BuyDeparture || command is RunCommand_ShopBuyCard || command is RunCommand_ShopBuyRelic || command is RunCommand_ShopBuyGear || command is RunCommand_ShopBuyMana || command is RunCommand_ShopBuyDeparture)
		{
			return "ui.buy";
		}
		if (command is RunCommand_ShopRemove || command is RunCommand_CampfireRemove || command is RunCommand_DiscardGear)
		{
			return "ui.remove";
		}
		if (command is RunCommand_ShopUpgrade || command is RunCommand_CampfireUpgrade || command is RunCommand_CampfireTrain)
		{
			return "ui.upgrade";
		}
		if (!(command is RunCommand_PickReward))
		{
			if (command is RunCommand_PickRelic || command is RunCommand_TakeGear || command is RunCommand_RelicChooseCards || command is RunCommand_CampfireDig)
			{
				return "ui.pick_relic";
			}
			if (!(command is RunCommand_CampfireRest))
			{
				if (!(command is RunCommand_WorkshopFuse))
				{
					if (!(command is RunCommand_EventChoice))
					{
						if (command is RunCommand_ChooseNode)
						{
							return "ui.node";
						}
						return null;
					}
					return "ui.event_choice";
				}
				return "ui.fuse";
			}
			return "ui.rest";
		}
		return "ui.pick_card";
	}

	public static CardInstance? NextSpark(GameState state)
	{
		if (state.Phase != "player-turn" || state.PendingScry != null)
		{
			return null;
		}
		foreach (CardInstance card in state.Player.Hand)
		{
			if (card.Def.SparkToken == true)
			{
				return card;
			}
		}
		return null;
	}

	public static bool RatingPhase(string phase)
	{
		switch (phase)
		{
		case "reward":
		case "relic-reward":
		case "won":
		case "lost":
			return true;
		default:
			return false;
		}
	}
}
