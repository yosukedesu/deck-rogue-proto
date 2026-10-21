using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class SourceEnemyLedgerModel
{
	public sealed record Pill(string? Icon, string Text, string Short, string Ink, string Paper, string Tip);

	public sealed record ForecastPart(string Text, string Ink, int Priority);

	public sealed record Spec(string Name, IReadOnlyList<Pill> Pills, IReadOnlyList<ForecastPart> Forecast, float Mark, bool Alive);

	public sealed record Packing(int Form, float Reserve, float NameWidth, IReadOnlyList<int> Shown);

	public sealed record HpPlacement(string Text, float Left, float Right, bool AlignRight, bool HpOnly);

	public const string Circled = "①②③④⑤⑥⑦⑧";

	public static float Width(float gap, int seats, bool phone)
	{
		if (seats != 1)
		{
			return Math.Clamp(gap - (float)(phone ? 4 : 12), phone ? 96 : 116, phone ? 176 : 220);
		}
		return phone ? 420 : 440;
	}

	public static float Height(bool forecast, bool phone)
	{
		return (!phone) ? (forecast ? 92 : 64) : (forecast ? 79 : 53);
	}

	public static string Short(string? icon, string text)
	{
		if (icon == null)
		{
			return text;
		}
		int k = text.Length;
		while (k > 0 && char.IsDigit(text[k - 1]))
		{
			k--;
		}
		if (k < text.Length && k > 0 && (text[k - 1] == '+' || text[k - 1] == '-'))
		{
			k--;
		}
		if (k >= text.Length)
		{
			if (text.Length > 2)
			{
				return "";
			}
			return text;
		}
		return text.Substring(k);
	}

	public static IReadOnlyList<Pill> Pills(GameState state, int index, EnemyDef? def)
	{
		EnemyState e = state.Enemies[index];
		List<Pill> ps = new List<Pill>();
		if (e.Hp <= 0)
		{
			return ps;
		}
		if (e.Strength != 0)
		{
			add((e.Strength > 0) ? "strength_up" : "strength_down", "筋力" + ((e.Strength > 0) ? "+" : "") + e.Strength, (e.Strength > 0) ? "ead08a" : "a8c9ea");
		}
		if (e.Burn > 0)
		{
			add("burn", "延焼" + e.Burn);
		}
		if (e.Exposed > 0)
		{
			add("exposed", "急所" + e.Exposed, "ead08a");
		}
		if (e.Confusion > 0)
		{
			add("confuse", "混乱" + e.Confusion, "cdb6ea", "33284d");
		}
		if (e.Weak > 0)
		{
			add("weak", "威圧" + e.Weak, "a8c9ea");
		}
		if (e.Artifact > 0)
		{
			add(null, "アーティファクト" + e.Artifact);
		}
		if (e.BurrowActive == true)
		{
			add(null, "潜伏");
		}
		if (e.Slippery > 0)
		{
			add(null, "朧 残り" + e.Slippery + "回");
		}
		IReadOnlyList<CardInstance>? readOnlyList = e.Sealed;
		if (readOnlyList != null && readOnlyList.Count > 0)
		{
			add(null, "封じ: " + string.Join("・", e.Sealed.Select((CardInstance c) => c.Def.Name)));
		}
		if (state.Enemies.Count == 1 && def != null)
		{
			foreach (CardText.TraitBadge b in CardText.EnemyTraitBadges(def).Take(3))
			{
				add(b.Icon, b.Label, "c4beb2", "2a2d48", b.Full + ((b.Term != null && KeywordHelp.Terms.TryGetValue(b.Term, out var help)) ? ("\n" + help) : ""));
			}
		}
		return ps;
		void add(string? icon, string text, string ink = "f4ecd6", string paper = "2a2d48", string? tip = null)
		{
			if (tip == null)
			{
				tip = string.Concat(text, string.Concat(from t in KeywordHelp.FindIn(text)
					select "\n" + t + ": " + KeywordHelp.Terms[t]));
			}
			ps.Add(new Pill(icon, text, Short(icon, text), ink, paper, tip));
		}
	}

	public static string InspectionStatus(GameState state, int index, EnemyDef? definition)
	{
		return string.Join("\n", from p in Pills(state, index, null).Where((Pill p) =>
			{
				if (!p.Text.StartsWith("封じ: "))
				{
					EnemyDef? enemyDef = definition;
					if ((object)enemyDef != null && enemyDef.Slippery.HasValue)
					{
						return !p.Text.StartsWith("朧 残り");
					}
					return true;
				}
				return false;
			})
			select p.Tip);
	}

	public static Packing Pack(IReadOnlyList<Pill> pills, float width, int seats, float nameFull, float nameMin, Func<string, float> measure)
	{
		float full = 0f;
		float shortW = 0f;
		float[] iconOnly = new float[pills.Count];
		for (int i = 0; i < pills.Count; i++)
		{
			Pill p = pills[i];
			bool icon = p.Icon != null;
			float wf = Math.Max(24f, (float)(icon ? 26 : 8) + measure(p.Text)) + 1f;
			float ws = ((p.Short.Length == 0) ? 24f : (Math.Max(24f, (float)(icon ? 25 : 8) + measure(p.Short)) + 1f));
			iconOnly[i] = (icon ? 24f : ws);
			full += (float)((i > 0) ? 4 : 0) + wf;
			shortW += (float)((i > 0) ? 4 : 0) + ws;
		}
		float inner = width - 16f;
		float cap = inner - 4f - nameMin;
		float reserve = 0f;
		int form = 0;
		List<int> shown = new List<int>();
		if (pills.Count > 0)
		{
			bool allowFull = width >= 150f && seats == 1;
			if (allowFull && nameFull + 4f + full <= inner)
			{
				form = 0;
				reserve = full;
			}
			else if (nameFull + 4f + shortW <= inner)
			{
				form = 1;
				reserve = shortW;
			}
			else if (shortW <= cap)
			{
				form = 1;
				reserve = shortW;
			}
			else if (allowFull && full <= cap)
			{
				form = 0;
				reserve = full;
			}
			else
			{
				form = 2;
				for (int j = 0; j < pills.Count; j++)
				{
					float add = (float)((shown.Count > 0) ? 4 : 0) + iconOnly[j];
					if (!(reserve + add > cap))
					{
						reserve += add;
						shown.Add(j);
					}
				}
				if (shown.Count == 0)
				{
					int first = -1;
					for (int k = 0; k < pills.Count; k++)
					{
						if (pills[k].Icon != null)
						{
							first = k;
							break;
						}
					}
					if (first < 0)
					{
						first = 0;
					}
					shown.Add(first);
					reserve = iconOnly[first];
				}
			}
			if (form != 2)
			{
				shown.AddRange(Enumerable.Range(0, pills.Count));
			}
		}
		return new Packing(form, reserve, Math.Max(0f, inner - ((reserve > 0f) ? (reserve + 4f) : 0f)), shown);
	}

	public static string DisplayText(Pill p, int form)
	{
		if (form != 0)
		{
			if (form != 1 && p.Icon != null)
			{
				return "";
			}
			return p.Short;
		}
		return p.Text;
	}

	public static float PillWidth(Pill p, int form, Func<string, float> measure)
	{
		string text = DisplayText(p, form);
		bool tight = form != 0;
		float pads = ((!tight) ? 8 : ((text.Length > 0) ? 8 : 4));
		float content = (float)(((p.Icon != null) ? 16 : 0) + ((p.Icon != null && text.Length > 0) ? (tight ? 1 : 2) : 0)) + ((text.Length > 0) ? Math.Max(10f, measure(text)) : 0f);
		return Math.Max(24f, pads + content);
	}

	public static float Estimate(string text, bool phone, float textScale = 1f)
	{
		float n = 0f;
		foreach (char c in text)
		{
			n += ((c >= '\u0080' && c != '×' && c != '＋') ? 1f : (char.IsDigit(c) ? 0.58f : 0.62f));
		}
		return n * (float)(phone ? 15 : 13) * textScale;
	}

	public static IReadOnlyList<ForecastPart> Forecast(GameState state, int index, EnemyDef? def, float width, bool phone, float textScale = 1f)
	{
		EnemyState e = state.Enemies[index];
		if (e.Hp <= 0)
		{
			return Array.Empty<ForecastPart>();
		}
		List<string[]> forms = new List<string[]>();
		List<string> inks = new List<string>();
		List<int> priorities = new List<int>();
		bool others = state.Enemies.Where((EnemyState x, int num) => num != index).Any((EnemyState x) => x.Hp > 0);
		int gi = FlowRules.GuardRedirect(state, index);
		if (gi >= 0 && gi != index)
		{
			string c = ((gi < "①②③④⑤⑥⑦⑧".Length) ? "①②③④⑤⑥⑦⑧"[gi].ToString() : "");
			add(new string[2]
			{
				c + "が庇う（単体は" + c + "へ）",
				c + "が庇う"
			}, "c4beb2", 1);
		}
		if (state.Enemies.Count > 1 && def != null)
		{
			List<string> labels = (from b in CardText.EnemyTraitBadges(def)
				where b.Term != "庇う" || !others
				select b.Label).ToList();
			if (labels.Count >= 2)
			{
				add(new string[2]
				{
					labels[0] + "・" + labels[1],
					labels[0]
				}, "c4beb2", 0);
			}
			else if (labels.Count == 1)
			{
				add(new string[1] { labels[0] }, "c4beb2", 0);
			}
		}
		string[] br = CardText.BranchForms(state, index);
		if (br != null)
		{
			add(br, "b5ddd6", 2);
		}
		if (def?.Interrupts != null && e.InterruptBlocked != true)
		{
			for (int i = 0; i < def.Interrupts.Count; i++)
			{
				IReadOnlyList<int>? firedInterrupts = e.FiredInterrupts;
				if (firedInterrupts != null && firedInterrupts.Contains(i))
				{
					continue;
				}
				EnemyInterrupt it = def.Interrupts[i];
				EnemyMove first = EnemyGraph.FirstMoveOf(def, it.Goto);
				string move = ((first != null) ? CardText.MoveShort(first, e.Strength) : null);
				string when;
				string shortWhen;
				if (it.On == "hpBelowHalf")
				{
					when = "HP" + e.MaxHp / 2 + "以下で";
					shortWhen = "HP半分で";
				}
				else if (it.On == "damageTaken")
				{
					when = "あと" + Math.Max(0, it.Amount.GetValueOrDefault() - e.DamageTakenTotal.GetValueOrDefault()) + "ダメージで";
					shortWhen = when;
				}
				else if (it.On == "alone")
				{
					if (!others)
					{
						continue;
					}
					when = "仲間が全滅すると";
					shortWhen = "ひとりになると";
				}
				else
				{
					if (!(it.On == "allyDied") || !others)
					{
						continue;
					}
					when = (shortWhen = "仲間が倒れると");
				}
				List<string> f = new List<string>();
				if (move != null)
				{
					f.Add(when + " " + move);
					if (shortWhen != when)
					{
						f.Add(shortWhen + " " + move);
					}
				}
				f.Add(shortWhen + "行動が変わる");
				f.Add(shortWhen + "変わる");
				add(f.ToArray(), "ead08a", 3);
				break;
			}
		}
		int[] levels = new int[forms.Count];
		for (int guard = 0; guard < 16; guard++)
		{
			if (!(total() > width - 16f))
			{
				break;
			}
			int pick = -1;
			for (int pr = 0; pr < 4; pr++)
			{
				if (pick >= 0)
				{
					break;
				}
				for (int i2 = 0; i2 < forms.Count; i2++)
				{
					if (priorities[i2] == pr && levels[i2] + 1 < forms[i2].Length)
					{
						pick = i2;
						break;
					}
				}
			}
			if (pick < 0)
			{
				break;
			}
			levels[pick]++;
		}
		return forms.Select((string[] array, int num) => new ForecastPart(array[levels[num]], inks[num], priorities[num])).ToArray();
		void add(string[] item, string ink, int priority)
		{
			forms.Add(item);
			inks.Add(ink);
			priorities.Add(priority);
		}
		float total()
		{
			if (forms.Count != 0)
			{
				return (float)(forms.Count - 1) * Estimate("\u3000", phone, textScale) + forms.Select((string[] array, int num) => Estimate(array[levels[num]], phone, textScale)).Sum();
			}
			return 0f;
		}
	}

	public static float Mark(EnemyDef? def, EnemyState enemy)
	{
		if (def?.Interrupts == null || enemy.MaxHp <= 0 || enemy.InterruptBlocked == true)
		{
			return -1f;
		}
		for (int i = 0; i < def.Interrupts.Count; i++)
		{
			IReadOnlyList<int>? firedInterrupts = enemy.FiredInterrupts;
			if (firedInterrupts != null && firedInterrupts.Contains(i))
			{
				continue;
			}
			EnemyInterrupt it = def.Interrupts[i];
			if (it.On == "hpBelowHalf")
			{
				return 0.5f;
			}
			if (it.On == "damageTaken")
			{
				int remain = Math.Max(0, it.Amount.GetValueOrDefault() - enemy.DamageTakenTotal.GetValueOrDefault());
				float at = (float)(enemy.Hp - remain) / (float)enemy.MaxHp;
				if (at > 0f && at < 1f)
				{
					return at;
				}
			}
		}
		return -1f;
	}

	public static HpPlacement PlaceHp(int hp, int max, float width, float mark, Func<string, float> measure)
	{
		string full = Math.Max(0, hp) + " / " + max;
		if (mark <= 0f || mark >= 1f || width <= 0f)
		{
			return new HpPlacement(full, 0f, width, AlignRight: false, HpOnly: false);
		}
		float mx = 3f + mark * (width - 6f);
		float tw = measure(max + " / " + max);
		float gap = 11f;
		if (Math.Abs(mx - width / 2f) >= tw / 2f + gap)
		{
			return new HpPlacement(full, 0f, width, AlignRight: false, HpOnly: false);
		}
		float la = 10f;
		float lb = mx - gap;
		float ra = mx + gap;
		float rb = width - 8f;
		if (rb - ra >= tw)
		{
			return new HpPlacement(full, ra, rb, AlignRight: false, HpOnly: false);
		}
		if (lb - la >= tw)
		{
			return new HpPlacement(full, la, lb, AlignRight: false, HpOnly: false);
		}
		string one = Math.Max(0, hp).ToString();
		float hw = measure(max.ToString());
		if (!(rb - ra >= hw) && !(rb - ra >= lb - la))
		{
			return new HpPlacement(one, la, lb, AlignRight: true, HpOnly: true);
		}
		return new HpPlacement(one, ra, rb, AlignRight: true, HpOnly: true);
	}
}
