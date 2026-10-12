using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class SourceUiPolicies
{
	public const int SparkLimit = 12;

	public static string? CommandSound(RunCommand command)
	{
		if (1 == 0)
		{
		}
		string result = ((!(command is RunCommand_BuyDeparture) && !(command is RunCommand_ShopBuyCard) && !(command is RunCommand_ShopBuyRelic) && !(command is RunCommand_ShopBuyGear) && !(command is RunCommand_ShopBuyMana) && !(command is RunCommand_ShopBuyDeparture)) ? ((!(command is RunCommand_ShopRemove) && !(command is RunCommand_CampfireRemove) && !(command is RunCommand_DiscardGear)) ? ((!(command is RunCommand_ShopUpgrade) && !(command is RunCommand_CampfireUpgrade) && !(command is RunCommand_CampfireTrain)) ? ((command is RunCommand_PickReward) ? "ui.pick_card" : ((!(command is RunCommand_PickRelic) && !(command is RunCommand_TakeGear) && !(command is RunCommand_RelicChooseCards) && !(command is RunCommand_CampfireDig)) ? ((command is RunCommand_CampfireRest) ? "ui.rest" : ((command is RunCommand_WorkshopFuse) ? "ui.fuse" : ((command is RunCommand_EventChoice) ? "ui.event_choice" : ((!(command is RunCommand_ChooseNode)) ? null : "ui.node")))) : "ui.pick_relic")) : "ui.upgrade") : "ui.remove") : "ui.buy");
		if (1 == 0)
		{
		}
		return result;
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
