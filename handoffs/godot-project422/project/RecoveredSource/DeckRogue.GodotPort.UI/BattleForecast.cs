using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class BattleForecast
{
	public static string Short(GameState state)
	{
		if (state.HideIntents != true)
		{
			return "被ダメ予測 " + Math.Max(0, Effects.IncomingTotal(state) - state.Player.Block - state.Player.IceBlock);
		}
		return "被ダメ予測 ？";
	}

	public static IReadOnlyList<string> Lines(GameState state)
	{
		PlayerState p = state.Player;
		int defense = p.Block + p.IceBlock;
		if (state.HideIntents == true)
		{
			return new string[1] { $"今フェーズの被ダメ予測: ？（意図は見えない。現在の防御 {defense} / HP {p.Hp}）" };
		}
		int incoming = Effects.IncomingTotal(state);
		List<string> lines = new List<string> { $"このターンに受けるダメージ: {incoming}（防御{defense}を差し引いて{Math.Max(0, incoming - defense)} / HP {p.Hp}）" };
		List<int> burnout = Effects.BurnoutEnemies(state);
		if (burnout.Count > 0)
		{
			lines.Add("延焼で行動の前に焼け落ちる（予測から除外）: " + string.Join("・", burnout.Select((int i) => $"敵{i + 1} {Content.GetEnemyDef(state.Enemies[i].EnemyId).Name}")));
		}
		int expiry = Effects.ExpiryBlockPreview(state);
		if (expiry > 0)
		{
			lines.Add($"ターン終了時の失効でブロック最大+{expiry}（今の衝動の札を全部失効させた場合。燠の守りで残る1枚を除く）");
		}
		return lines;
	}
}
