using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class SourceCardModalPolicy
{
	public static CardInstance Preview(CardInstance original, bool upgraded)
	{
		if (!upgraded || !Upgrade.CanUpgradeCard(original))
		{
			return original;
		}
		return Upgrade.UpgradeCard(original);
	}

	public static bool NeedsGearCardPicker(GearDef def, int available)
	{
		if (def.NeedsCard != null)
		{
			return available > 0;
		}
		return false;
	}

	public static bool RequiresFleeConfirmation(GearDef def)
	{
		return def.Special == "flee";
	}

	public static string UpgradeState(CardInstance card)
	{
		if (!Upgrade.IsUpgraded(card))
		{
			return "鍛えられない札";
		}
		return "鍛え済み";
	}

	public static string CornerMark(string badge)
	{
		return badge switch
		{
			"素材 A" => "A", 
			"素材 B" => "B", 
			"選択中" => "✓", 
			"捨てる (選択中)" => "✓", 
			"残す (選択中)" => "✓", 
			"★ レシピの素材" => "★", 
			_ => "", 
		};
	}
}
