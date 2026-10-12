using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.Presentation;

public static class MergedEventCues
{
	public static IReadOnlyList<string> EventTypes { get; } = Array.AsReadOnly(new string[7] { "Cascaded", "ImpulseReturned", "ImpulseKept", "ImpulseKindled", "BlazeIgnited", "BlazeExtinguished", "CascadeRetargeted" });

	public static bool Supports(string type)
	{
		return EventTypes.Contains(type);
	}

	public static SemanticEventCue? Describe(GameEvent e, Func<string, string> cardName)
	{
		if (1 == 0)
		{
		}
		SemanticEventCue result;
		if (!(e is GameEvent_Cascaded v))
		{
			result = ((e is GameEvent_ImpulseReturned v2) ? new SemanticEventCue(e.Type, "衝動の失効\n" + cardName(v2.CardId) + " → 捨て札", "player", null, "c4beb2", 0.95f) : ((e is GameEvent_ImpulseKept v3) ? new SemanticEventCue(e.Type, "燠の守り\n" + cardName(v3.CardId) + "は手札に残る", "player", null, "e7c887", 0.95f) : ((e is GameEvent_ImpulseKindled v4) ? new SemanticEventCue(e.Type, "追加コスト: " + cardName(v4.CardId) + "\n" + string.Join("・", (v4.CardIds ?? Array.Empty<string>()).Select(cardName)) + "を消滅", "player", null, "dba27c", 1.05f) : ((e is GameEvent_BlazeIgnited) ? new SemanticEventCue(e.Type, "猛り火に点いた\n延焼の合計が8以上", "battle", null, "e8742f", 1f) : ((e is GameEvent_BlazeExtinguished) ? new SemanticEventCue(e.Type, "猛り火が消えた\n延焼の合計が8未満", "battle", null, "c4beb2", 1f) : ((!(e is GameEvent_CascadeRetargeted v5)) ? null : new SemanticEventCue(e.Type, cardName(v5.CardId) + "の本体\n→ 敵" + (v5.ToIndex + 1), "enemy", v5.ToIndex, "e7c887", 1f)))))));
		}
		else
		{
			string type = e.Type;
			string[] obj = new string[5]
			{
				"チェイン: ",
				cardName(v.CardId),
				"\n→ ",
				(v.HitId == null) ? "当たりなし" : cardName(v.HitId),
				null
			};
			IReadOnlyList<string> revealed = v.Revealed;
			obj[4] = ((revealed != null && revealed.Count > 0) ? ("（" + v.Revealed.Count + "枚は山札の底へ）") : "");
			result = new SemanticEventCue(type, string.Concat(obj), "player", null, "e7c887", 1.05f);
		}
		if (1 == 0)
		{
		}
		return result;
	}
}
