using System;
using System.Collections.Generic;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class SourceIntentLayout
{
	public sealed record Rider(string Icon, string Text, string Ink, string Fill);

	public sealed record Spec(string Kind, string Caption, bool Minor, bool Small, bool Stack, float IconSize, int FontSize, float RowHeight, float Width, float Height, IReadOnlyList<Rider> Riders);

	public static Spec ForState(GameState state, int index, bool phone, float gap, bool uniformStack, float textScale = 1f)
	{
		EnemyIntent it = Effects.EffectiveIntent(state, index);
		if (it == null)
		{
			return null;
		}
		return Build(it, state.HideIntents == true, Effects.DisplayedIntentValue(state, index, it.Kind, it.Actual), Effects.DisplayedInflict(state, it.Inflict), phone, gap, state.Enemies.Count == 1, uniformStack, textScale);
	}

	public static Spec Build(EnemyIntent it, bool hidden, int displayed, StatusInflict inflict, bool phone, float gap, bool solo, bool uniformStack, float textScale = 1f)
	{
		bool small = gap < (float)(phone ? 170 : 240);
		bool flag = !hidden;
		if (flag)
		{
			bool flag2;
			switch (it.Kind)
			{
			case "defend":
			case "buff":
			case "rally":
			case "heal":
			case "rest":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		bool minor = flag;
		float icon = ((!minor) ? ((!small) ? (phone ? 32 : 44) : (phone ? 28 : 36)) : (small ? 22 : (phone ? 26 : 32)));
		float row = ((!minor) ? ((!small) ? (phone ? 42 : 56) : (phone ? 38 : 48)) : (small ? 30 : (phone ? 34 : 44)));
		row = Math.Max(row, phone ? 38 : 48) * textScale;
		int font = (phone ? 25 : 32);
		string caption = (hidden ? "？" : Caption(it, displayed));
		List<Rider> riders = new List<Rider>();
		if (!hidden)
		{
			if (inflict != null)
			{
				riders.Add(new Rider(StatusIcon(inflict.Status), (small ? "" : "あなたに") + CardText.StatusName(inflict.Status) + inflict.Amount, "cdb6ea", "33284d"));
			}
			if (it.AlsoBuff.HasValue)
			{
				riders.Add(new Rider("strength_up", (small ? "" : "同時に") + "筋力+" + it.AlsoBuff.Value, "ead08a", "2a2d48"));
			}
			if (it.AlsoDefend.HasValue)
			{
				riders.Add(new Rider("shield", (small ? "" : "同時に") + "ブロック" + it.AlsoDefend.Value, "a8c9ea", "1f344c"));
			}
			if (it.StrengthPerMilled.HasValue)
			{
				riders.Add(new Rider("strength_up", "食べた分 筋力+" + it.StrengthPerMilled.Value + "/枚", "ead08a", "2a2d48"));
			}
			if (it.AlsoDestroySet == true)
			{
				riders.Add(new Rider("exhaust", "先に壊す", "f2a193", "3d2028"));
			}
		}
		float wTop = icon + (float)(phone ? 6 : 10) + TextWidth(caption, (float)font * textScale, deco: true) + 24f;
		float wr = 0f;
		foreach (Rider r in riders)
		{
			wr += PillWidth(r, textScale) + 6f;
		}
		bool stack = riders.Count > 0 && (((small || (phone & minor)) | uniformStack) || wTop + wr > gap - 8f);
		float cap = (solo ? float.MaxValue : (gap - 10f));
		if (it.Kind == "buff" && !hidden && (stack ? Math.Max(wTop, wr + 12f) : (wTop + wr)) > cap)
		{
			caption = "+" + it.Actual;
			wTop = icon + (float)(phone ? 6 : 10) + TextWidth(caption, (float)font * textScale, deco: true) + 24f;
		}
		float width = (stack ? Math.Max(72f, Math.Max(wTop, wr + 12f)) : Math.Max(72f, wTop + wr));
		if (!solo)
		{
			width = Math.Min(width, Math.Max(wTop, cap));
		}
		return new Spec(it.Kind, caption, minor, small, stack, icon, font, row, width, row + (stack ? ((float)(phone ? 30 : 32) * textScale) : 0f), riders);
	}

	public static float PillWidth(Rider rider, float textScale)
	{
		return (float)(4 + ((rider.Icon != null) ? 18 : 0)) + TextWidth(rider.Text, 15f * textScale) + 6f;
	}

	public static float TextWidth(string text, float size, bool deco = false)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0f;
		}
		float w = 0f;
		foreach (char c in text)
		{
			w += ((c >= '\u0080' && c != '×' && c != '＋') ? 1f : (char.IsDigit(c) ? 0.58f : 0.62f));
		}
		if (deco)
		{
			w += 0.02f * (float)text.Length;
		}
		return w * size;
	}

	public static string StatusIcon(string status)
	{
		switch (status)
		{
		case "weak":
			return "weak";
		case "frail":
			return "frail";
		case "vulnerable":
		case "slow":
			return "exposed";
		case "mist":
			return "draw";
		default:
			return null;
		}
	}

	public static string Caption(EnemyIntent it, int displayed)
	{
		return it.Kind switch
		{
			"attack" => displayed + (((it.Hits ?? 1) > 1) ? (" ×" + it.Hits.Value) : "") + ((it.MirrorHits == true) ? " ×手数" : ""), 
			"defend" => it.Actual.ToString(), 
			"buff" => "筋力+" + it.Actual, 
			"rally" => "応援+" + it.Actual, 
			"heal" => "回復" + it.Actual, 
			"hex" => "呪い", 
			"destroy-set" => "からくり壊し", 
			"destroy-token" => "人形狩り", 
			"steal-gold" => "盗み" + it.Actual + "G", 
			"flee" => "逃走", 
			"mill" => "山札喰い" + it.Actual + "枚", 
			"seal" => "技封じ", 
			"summon" => "召喚", 
			"rest" => "隙", 
			"hatch" => "孵化", 
			_ => it.Kind, 
		};
	}
}
