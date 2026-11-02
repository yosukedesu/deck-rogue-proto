using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class Golden
{
	public static string CombatDigest(GameState s)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append('{');
		Int(sb, "turn", s.Turn, first: true);
		Str(sb, "phase", s.Phase, first: false);
		Int(sb, "hp", s.Player.Hp, first: false);
		Int(sb, "block", s.Player.Block, first: false);
		Int(sb, "iceBlock", s.Player.IceBlock, first: false);
		Int(sb, "energy", s.Player.Energy, first: false);
		Int(sb, "energyMax", s.Player.EnergyMax, first: false);
		Int(sb, "growth", s.Player.Growth, first: false);
		Int(sb, "momentum", s.Player.Momentum, first: false);
		StrArrayOfCards(sb, "hand", s.Player.Hand);
		Int(sb, "draw", s.Player.DrawPile.Count, first: false);
		Int(sb, "discard", s.Player.DiscardPile.Count, first: false);
		Int(sb, "exhaust", s.Player.ExhaustPile.Count, first: false);
		StrArrayOfCards(sb, "set", s.Player.SetCards);
		StrArrayOfCards(sb, "permanents", s.Player.Permanents);
		sb.Append(",\"enemies\":[");
		for (int i = 0; i < s.Enemies.Count; i++)
		{
			EnemyState e = s.Enemies[i];
			if (i > 0)
			{
				sb.Append(',');
			}
			sb.Append('{');
			Str(sb, "id", e.EnemyId, first: true);
			Int(sb, "hp", e.Hp, first: false);
			Int(sb, "block", e.Block, first: false);
			Int(sb, "strength", e.Strength, first: false);
			Int(sb, "burn", e.Burn, first: false);
			sb.Append('}');
		}
		sb.Append(']');
		Long(sb, "rngSeed", s.Rng.Seed, first: false);
		Long(sb, "rngCounter", s.Rng.Counter, first: false);
		Int(sb, "events", s.EventLog.Count, first: false);
		sb.Append(",\"eventTail\":[");
		int start = ((s.EventLog.Count > 50) ? (s.EventLog.Count - 50) : 0);
		for (int j = start; j < s.EventLog.Count; j++)
		{
			if (j > start)
			{
				sb.Append(',');
			}
			Quote(sb, s.EventLog[j].Type);
		}
		sb.Append(']');
		sb.Append('}');
		return sb.ToString();
	}

	public static string RunDigest(RunState run)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append('{');
		Int(sb, "act", run.Act, first: true);
		Int(sb, "row", run.Row, first: false);
		Str(sb, "phase", run.Phase, first: false);
		Int(sb, "hp", run.Hp, first: false);
		Int(sb, "maxHp", run.MaxHp, first: false);
		Int(sb, "gold", run.Gold, first: false);
		StrArrayOfCards(sb, "deck", run.Deck);
		sb.Append(",\"relics\":[");
		for (int i = 0; i < run.Relics.Count; i++)
		{
			if (i > 0)
			{
				sb.Append(',');
			}
			Quote(sb, run.Relics[i]);
		}
		sb.Append(']');
		Long(sb, "rngSeed", run.Rng.Seed, first: false);
		Long(sb, "rngCounter", run.Rng.Counter, first: false);
		sb.Append(",\"combat\":");
		sb.Append((run.Combat != null) ? CombatDigest(run.Combat) : "null");
		sb.Append('}');
		return sb.ToString();
	}

	public static uint Fnv1a32(string text)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		uint h = 2166136261u;
		for (int i = 0; i < bytes.Length; i++)
		{
			h ^= bytes[i];
			h *= 16777619;
		}
		return h;
	}

	public static string RunHash(RunState run)
	{
		return Fnv1a32(RunDigest(run)).ToString("x8", CultureInfo.InvariantCulture);
	}

	private static void Int(StringBuilder sb, string key, int value, bool first)
	{
		if (!first)
		{
			sb.Append(',');
		}
		Quote(sb, key);
		sb.Append(':');
		sb.Append(value.ToString(CultureInfo.InvariantCulture));
	}

	private static void Long(StringBuilder sb, string key, long value, bool first)
	{
		if (!first)
		{
			sb.Append(',');
		}
		Quote(sb, key);
		sb.Append(':');
		sb.Append(value.ToString(CultureInfo.InvariantCulture));
	}

	private static void Str(StringBuilder sb, string key, string value, bool first)
	{
		if (!first)
		{
			sb.Append(',');
		}
		Quote(sb, key);
		sb.Append(':');
		Quote(sb, value);
	}

	private static void StrArrayOfCards(StringBuilder sb, string key, IReadOnlyList<CardInstance> cards)
	{
		sb.Append(',');
		Quote(sb, key);
		sb.Append(":[");
		for (int i = 0; i < cards.Count; i++)
		{
			if (i > 0)
			{
				sb.Append(',');
			}
			Quote(sb, cards[i].Def.Id);
		}
		sb.Append(']');
	}

	private static void Quote(StringBuilder sb, string s)
	{
		sb.Append('"');
		if (s != null)
		{
			foreach (char c in s)
			{
				switch (c)
				{
				case '"':
					sb.Append("\\\"");
					continue;
				case '\\':
					sb.Append("\\\\");
					continue;
				case '\b':
					sb.Append("\\b");
					continue;
				case '\f':
					sb.Append("\\f");
					continue;
				case '\n':
					sb.Append("\\n");
					continue;
				case '\r':
					sb.Append("\\r");
					continue;
				case '\t':
					sb.Append("\\t");
					continue;
				}
				if (c < ' ')
				{
					StringBuilder stringBuilder = sb.Append("\\u");
					int num = c;
					stringBuilder.Append(num.ToString("x4", CultureInfo.InvariantCulture));
				}
				else
				{
					sb.Append(c);
				}
			}
		}
		sb.Append('"');
	}
}
