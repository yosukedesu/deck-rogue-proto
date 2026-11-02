using System;

namespace DeckRogue.Engine;

public static class ReactionSystems
{
	public static readonly IReactionSystem SetAuto = new SetAutoSystem();

	public static readonly IReactionSystem HoldManual = new HoldManualSystem();

	public static readonly IReactionSystem SetConfirm = new SetConfirmSystem();

	public static IReactionSystem Get(string mode)
	{
		return mode switch
		{
			"set-auto" => SetAuto, 
			"hold-manual" => HoldManual, 
			"set-confirm" => SetConfirm, 
			_ => throw new InvalidOperationException("未知のリアクション方式: " + mode), 
		};
	}
}
