using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckRogue.Engine;

public static class Report
{
	public const int ARCHIVE_LINES_CAP = 300;

	private const int LOG_CAP = 600;

	private const int SNAPSHOT_LOG_CAP = 400;

	private static readonly Dictionary<string, string> NODE_LABEL = new Dictionary<string, string>
	{
		{ "battle", "⚔\ufe0f" },
		{ "elite", "\ud83d\udc51強個体" },
		{ "boss", "\ud83d\udc80幕ボス" },
		{ "campfire", "\ud83d\udd25焚き火" },
		{ "workshop", "\ud83d\udd28工房" },
		{ "shop", "\ud83d\uded2ショップ" },
		{ "event", "❓" },
		{ "treasure", "\ud83c\udf81宝箱" }
	};

	public static string SafeEncounterName(string id)
	{
		try
		{
			return Content.EncounterName(id);
		}
		catch (Exception)
		{
			return id;
		}
	}

	public static string SafeEnemyName(string id)
	{
		try
		{
			return Content.GetEnemyDef(id).Name;
		}
		catch (Exception)
		{
			return id;
		}
	}

	public static string SafeRelicName(string id)
	{
		try
		{
			return Content.GetRelicDef(id).Name;
		}
		catch (Exception)
		{
			return id;
		}
	}

	public static string GearName(string id)
	{
		try
		{
			return Content.GetGearDef(id).Name;
		}
		catch (Exception)
		{
			return id;
		}
	}

	public static string DepartureDetail(DepartureOffer o)
	{
		List<string> parts = new List<string> { o.Text };
		if (o.Choice.RelicId != null)
		{
			parts.Add(SafeRelicName(o.Choice.RelicId));
		}
		if (o.Choice.Gears != null && o.Choice.Gears.Count > 0)
		{
			parts.Add(string.Join("・", o.Choice.Gears.Select(GearName).ToArray()));
		}
		if (o.Choice.AddCardIds != null && o.Choice.AddCardIds.Count > 0)
		{
			parts.Add(string.Join("・", o.Choice.AddCardIds.Select((string id) =>
			{
				try
				{
					return Content.GetCardDef(id).Name;
				}
				catch (Exception)
				{
					return id;
				}
			}).ToArray()));
		}
		return string.Join("＝", parts.ToArray());
	}

	public static string CardName(string cardId)
	{
		try
		{
			return Content.GetCardDef(cardId).Name;
		}
		catch (Exception)
		{
			try
			{
				CardDef f = Fusion.ResolveFusedDef(cardId);
				return (f != null) ? f.Name : cardId;
			}
			catch (Exception)
			{
				return cardId;
			}
		}
	}

	private static string RelicRarityTag(RelicDef def)
	{
		return (def.Rarity ?? "common") switch
		{
			"uncommon" => "◆アンコモン", 
			"rare" => "★レア", 
			"boss" => "\ud83d\udc51ボス", 
			"shop" => "\ud83d\uded2店売り", 
			"event" => "❓イベント", 
			_ => "", 
		};
	}

	private static string Names(IReadOnlyList<CardInstance> cs)
	{
		if (cs == null || cs.Count == 0)
		{
			return "（なし）";
		}
		List<string> b = new List<string>();
		for (int i = 0; i < cs.Count; i++)
		{
			b.Add(cs[i].Def.Name);
		}
		return string.Join("、", b.ToArray());
	}

	public static string DataFingerprint()
	{
		uint h = 5381u;
		IReadOnlyList<CardDef> cards = Content.AllCards;
		for (int c = 0; c < cards.Count; c++)
		{
			CardDef def = cards[c];
			string s = def.Id + ":" + def.Cost.ToString(CultureInfo.InvariantCulture) + ":" + def.Effects.Count.ToString(CultureInfo.InvariantCulture);
			for (int i = 0; i < s.Length; i++)
			{
				h = (uint)((int)(((long)h * 33L) & 0xFFFFFFFFu) ^ s[i]);
			}
		}
		return "cards" + cards.Count + "/enemies" + Content.AllEnemies.Count + "/" + ToBase36(h);
	}

	private static string ToBase36(uint v)
	{
		if (v == 0)
		{
			return "0";
		}
		StringBuilder sb = new StringBuilder();
		while (v != 0)
		{
			sb.Insert(0, "0123456789abcdefghijklmnopqrstuvwxyz"[(int)(v % 36)]);
			v /= 36;
		}
		return sb.ToString();
	}

	public static BattleArchive ArchiveBattle(GameState combat, int battleNo, string enemyId, bool elite, int hpBefore, int deckSize, int act, bool boss, ReportText text)
	{
		List<string> all = new List<string>();
		for (int i = 0; i < combat.EventLog.Count; i++)
		{
			string line = ReportLine(combat.EventLog[i], text);
			if (line != null)
			{
				all.Add(line);
			}
		}
		return new BattleArchive
		{
			BattleNo = battleNo,
			Act = act,
			Boss = boss,
			Metrics = Analysis.ComputeBattleMetrics(combat.EventLog),
			EnemyId = enemyId,
			Elite = elite,
			Result = ((combat.Phase == "won") ? "won" : "lost"),
			Turns = combat.Turn,
			HpBefore = hpBefore,
			HpAfter = Math.Max(0, combat.Player.Hp),
			DeckSize = deckSize,
			Lines = ((all.Count > 300) ? all.Skip(all.Count - 300).ToList() : all)
		};
	}

	public static List<BattleRow> ToBattleRows(IReadOnlyList<BattleArchive> history)
	{
		List<BattleRow> rows = new List<BattleRow>();
		for (int i = 0; i < history.Count; i++)
		{
			BattleArchive h = history[i];
			rows.Add(new BattleRow
			{
				BattleNo = h.BattleNo,
				Act = h.Act.GetValueOrDefault(),
				EnemyId = h.EnemyId,
				Elite = h.Elite,
				Boss = (h.Boss == true),
				Result = h.Result,
				HpBefore = h.HpBefore,
				HpAfter = h.HpAfter,
				Metrics = h.Metrics,
				Rating = h.Rating
			});
		}
		return rows;
	}

	public static MetricsExport MetricsExportOf(RunState? run, IReadOnlyList<BattleArchive> history)
	{
		List<BattleRow> battles = ToBattleRows(history);
		return new MetricsExport
		{
			Schema = "deck-rogue-metrics/1",
			Fingerprint = DataFingerprint(),
			Run = ((run != null) ? new MetricsRunSummary
			{
				Act = run.Act,
				BattlesWon = run.BattlesWon,
				Hp = run.Hp,
				DeckSize = run.Deck.Count
			} : null),
			Battles = battles,
			Acts = Analysis.ActSummaries(battles)
		};
	}

	public static List<string> DecisionTimeLines(RunJournal? journal)
	{
		List<string> outp = new List<string>();
		if (journal == null || journal.Times == null || journal.Times.Count < 2)
		{
			return outp;
		}
		IReadOnlyList<long> t = journal.Times;
		List<(int, long, string)> gaps = new List<(int, long, string)>();
		for (int i = 1; i < t.Count && i < journal.Commands.Count + 1 && i < journal.Commands.Count; i++)
		{
			RunCommand cmd = journal.Commands[i];
			RunCommand_Combat cc = cmd as RunCommand_Combat;
			string label;
			if (cc != null)
			{
				Command_PlayCard pc = cc.Command as Command_PlayCard;
				Command_SetCard sc = cc.Command as Command_SetCard;
				string uid;
				if (pc != null)
				{
					uid = " " + pc.CardUid;
				}
				else
				{
					uid = ((sc != null) ? (" " + sc.CardUid) : "");
				}
				label = "戦闘: " + cc.Command.Type + uid;
			}
			else
			{
				label = cmd.Type;
			}
			gaps.Add((i, t[i] - t[i - 1], label));
		}
		if (gaps.Count == 0)
		{
			return outp;
		}
		long total = t[t.Count - 1] - t[0];
		List<(int, long, string)> inCombat = gaps.Where(((int I, long Ms, string Label) tuple) => tuple.Label.StartsWith("戦闘")).ToList();
		List<(int, long, string)> outCombat = gaps.Where(((int I, long Ms, string Label) tuple) => !tuple.Label.StartsWith("戦闘")).ToList();
		outp.Add("- 合計 " + ((double)total / 60000.0).ToString("0.0", CultureInfo.InvariantCulture) + "分・コマンド" + journal.Commands.Count + "件（1手あたり 戦闘内" + Avg(inCombat) + "秒／戦闘外" + Avg(outCombat) + "秒）");
		outp.Add("- 長考トップ8（直前の手からの経過秒 → 決めた手）:");
		foreach (var g in gaps.OrderByDescending(((int I, long Ms, string Label) tuple) => tuple.Ms).Take(8))
		{
			outp.Add("  - " + ((double)g.Item2 / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + "秒 → [" + g.Item1 + "] " + g.Item3);
		}
		return outp;
		static string Avg(List<(int I, long Ms, string Label)> xs)
		{
			if (xs.Count <= 0)
			{
				return "-";
			}
			return ((double)xs.Sum(((int I, long Ms, string Label) x) => x.Ms) / (double)xs.Count / 1000.0).ToString("0.0", CultureInfo.InvariantCulture);
		}
	}

	private static string? ReportLine(GameEvent e, ReportText text)
	{
		if (e is GameEvent_EnemyPhaseEnded)
		{
			return "敵フェーズ終了";
		}
		GameEvent_CardsDrawn cd = e as GameEvent_CardsDrawn;
		if (cd != null)
		{
			return "ドロー" + cd.Count + "枚" + ((cd.Cards != null) ? (": " + string.Join("・", cd.Cards.ToArray())) : "");
		}
		GameEvent_TurnEnded te = e as GameEvent_TurnEnded;
		if (te != null)
		{
			return "ターン終了" + ((te.Unplayed != null) ? ("（未使用: " + ((te.Unplayed.Count > 0) ? string.Join("・", te.Unplayed.ToArray()) : "なし") + "）") : "");
		}
		try
		{
			return text.LogLine(e);
		}
		catch (Exception)
		{
			return e.Type;
		}
	}

	private static List<string> RenderBoard(GameState s, ReportText text)
	{
		PlayerState p = s.Player;
		List<string> outp = new List<string>();
		string[] st = new string[11]
		{
			(p.Growth != 0) ? ("成長" + p.Growth) : "",
			(p.Momentum != 0) ? ("勢い" + p.Momentum) : "",
			(p.IceBlock != 0) ? ("氷壁" + p.IceBlock) : "",
			(p.Aether != 0) ? ("霊気" + p.Aether) : "",
			(p.NextCardDiscount != 0) ? ("次コスト-" + p.NextCardDiscount) : "",
			(p.Weak != 0) ? ("弱体" + p.Weak) : "",
			(p.Vulnerable != 0) ? ("脆弱" + p.Vulnerable) : "",
			(p.Frail != 0) ? ("虚弱" + p.Frail) : "",
			(p.Restrain != 0) ? ("拘束" + p.Restrain) : "",
			(p.Mist.GetValueOrDefault() != 0) ? ("霞み" + p.Mist) : "",
			(p.Slow.GetValueOrDefault() != 0) ? ("重り" + p.Slow) : ""
		}.Where((string x) => x != "").ToArray();
		outp.Add("自分: HP " + p.Hp + "/" + p.MaxHp + " ブロック" + p.Block + " エナジー" + p.Energy + "/" + p.EnergyMax + " " + string.Join(" ", st));
		List<string> hand = new List<string>();
		for (int i = 0; i < p.Hand.Count; i++)
		{
			CardInstance c = p.Hand[i];
			int cost = 0;
			bool ok = false;
			try
			{
				cost = Effects.EffectiveCost(s, c);
				ok = Effects.IsPlayableFromHand(c, s) && cost <= p.Energy;
			}
			catch (Exception)
			{
			}
			hand.Add(c.Def.Name + "(" + cost + ")" + (ok ? "" : "✕"));
		}
		outp.Add("手札(" + p.Hand.Count + "): " + ((hand.Count > 0) ? string.Join("、", hand.ToArray()) : "（なし）"));
		outp.Add("伏せ場(" + p.SetCards.Count + "/" + p.SetSlots + "): " + Names(p.SetCards));
		outp.Add("置物: " + Names(p.Permanents));
		outp.Add("山札" + p.DrawPile.Count + " / 捨札" + p.DiscardPile.Count + " / 消滅" + p.ExhaustPile.Count);
		for (int i2 = 0; i2 < s.Enemies.Count; i2++)
		{
			EnemyState e = s.Enemies[i2];
			if (e.Hp <= 0)
			{
				outp.Add("敵" + (i2 + 1) + " " + SafeEnemyName(e.EnemyId) + ": 撃破済み");
				continue;
			}
			string[] dbg = new string[5]
			{
				(e.Strength != 0) ? ("強化" + ((e.Strength > 0) ? "+" : "") + e.Strength) : "",
				(e.Block != 0) ? ("ブロック" + e.Block) : "",
				(e.Burn != 0) ? ("延焼" + e.Burn) : "",
				(e.Confusion != 0) ? ("混乱" + e.Confusion) : "",
				(e.Exposed != 0) ? ("急所" + e.Exposed) : ""
			}.Where((string x) => x != "").ToArray();
			string intent;
			try
			{
				intent = text.IntentText(s, i2);
			}
			catch (Exception)
			{
				intent = "?";
			}
			outp.Add("敵" + (i2 + 1) + " " + SafeEnemyName(e.EnemyId) + ": HP " + e.Hp + "/" + e.MaxHp + " " + string.Join(" ", dbg) + " → " + intent);
		}
		if (s.PendingWindow != null)
		{
			PendingWindow w = s.PendingWindow;
			EnemyState en = ((w.EnemyIndex >= 0 && w.EnemyIndex < s.Enemies.Count) ? s.Enemies[w.EnemyIndex] : null);
			outp.Add("★確認ウィンドウ待ち: 敵" + (w.EnemyIndex + 1) + " " + ((en != null) ? SafeEnemyName(en.EnemyId) : "?") + " / " + w.Stage + "窓 / 実値 " + ((en != null && en.Intent != null) ? en.Intent.Actual.ToString() : "?"));
		}
		return outp;
	}

	private static GameState TrimLog(GameState s)
	{
		if (s.EventLog.Count <= 400)
		{
			return s;
		}
		List<GameEvent> log = new List<GameEvent> { s.EventLog[0] };
		log.AddRange(s.EventLog.Skip(s.EventLog.Count - 399));
		return s with
		{
			EventLog = log
		};
	}

	private static string NodeLabel(MapNode n)
	{
		if (n.EncounterId != null)
		{
			return SafeEncounterName(n.EncounterId);
		}
		return n.Type switch
		{
			"campfire" => "焚き火", 
			"workshop" => "工房", 
			"shop" => "ショップ", 
			_ => "?", 
		};
	}

	public static string BuildReport(RunState? run, GameState? state, IReadOnlyList<BattleArchive> history, string note, IReadOnlyList<PlayNote> playNotes, IReadOnlyList<RunChoice> choices, RunJournal? journal, ReportText text, DateTime now)
	{
		GameState s = ((run != null) ? run.Combat : state);
		List<string> L = new List<string>();
		L.Add("# プレイ状況レポート");
		L.Add("書き出し: " + now.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture) + " / データ指紋: " + DataFingerprint() + " / Unity");
		if (!string.IsNullOrEmpty(note))
		{
			L.Add("メモ: " + note);
		}
		L.Add("");
		L.Add("## いまの状況");
		if (run != null)
		{
			string leaderName = run.LeaderId;
			try
			{
				leaderName = Content.GetLeaderDef(run.LeaderId).Name;
			}
			catch (Exception)
			{
			}
			L.Add("ラン " + leaderName + "（" + run.LeaderId + "） / seed " + run.Seed + " / mode " + run.Mode + " / 難易度 " + run.Difficulty);
			L.Add("進行: " + run.Phase + " / 幕" + run.Act + "/3 行" + (run.Row + 1) + "/" + run.Map.Count + "・" + run.BattlesWon + "勝" + (run.CurrentElite ? "（強個体）" : "") + " / HP " + run.Hp + "/" + run.MaxHp + " / \ud83d\udcb0" + run.Gold + "G / デッキ" + run.Deck.Count + "枚");
			List<string> rows = new List<string>();
			for (int r = 0; r < run.Map.Count; r++)
			{
				List<string> cells = new List<string>();
				for (int c = 0; c < run.Map[r].Count; c++)
				{
					cells.Add(NodeLabel(run.Map[r][c]) + ((r == run.Row && c == run.Col) ? "●" : ""));
				}
				rows.Add("行" + r + ":" + string.Join("/", cells.ToArray()));
			}
			L.Add("マップ: " + string.Join(" ", rows.ToArray()));
			L.Add("レリック: " + ((run.Relics.Count > 0) ? string.Join("、", run.Relics.ToArray()) : "（なし）"));
			L.Add("ピック履歴: " + ((run.Picks.Count > 0) ? string.Join("、", run.Picks.Select(CardName).ToArray()) : "（なし）"));
			if (run.RewardOptions != null)
			{
				L.Add("報酬候補（いま提示中）: " + string.Join(" / ", run.RewardOptions.Select(CardName).ToArray()));
			}
			if (run.RelicOptions != null)
			{
				L.Add("レリック候補（いま提示中）: " + string.Join(" / ", run.RelicOptions.ToArray()));
			}
			L.Add("デッキ全体: " + Names(run.Deck));
		}
		else if (state != null)
		{
			L.Add("単発検証 / mode " + state.ReactionMode + " / seed " + state.Rng.Seed);
		}
		else
		{
			L.Add("（戦闘・ランともに未開始）");
		}
		L.Add("");
		if (playNotes.Count > 0)
		{
			L.Add("## プレイメモ（" + playNotes.Count + "件・プレイヤーがその場で残した気づき）");
			for (int i = 0; i < playNotes.Count; i++)
			{
				PlayNote n = playNotes[i];
				string hm = ((n.At.Length >= 16) ? n.At.Substring(11, 5) : n.At);
				L.Add("- [" + hm + " " + n.Context + "] " + n.Text);
			}
			L.Add("");
		}
		List<string> dt = DecisionTimeLines(journal);
		if (dt.Count > 0)
		{
			L.Add("## 判断時間（ジャーナルの記録時刻から。長考＝悩みの密度の代理指標）");
			L.AddRange(dt);
			L.Add("");
		}
		if (choices.Count > 0)
		{
			L.Add("## 選択履歴（" + choices.Count + "件・ピック/鍛錬/合成/購入/イベントの意思決定）");
			for (int j = 0; j < choices.Count; j++)
			{
				RunChoice c2 = choices[j];
				L.Add("- [" + c2.At + "] " + c2.Text + ((c2.Ctx != null) ? ("（HP " + c2.Ctx.Hp + "/" + c2.Ctx.MaxHp + "・デッキ" + c2.Ctx.Deck + "枚・" + c2.Ctx.Gold + "G）") : ""));
			}
			L.Add("");
		}
		if (history.Count > 0)
		{
			L.Add("## これまでの戦闘（" + history.Count + "戦）");
			L.Add("");
			L.Add("| # | 敵 | 結果 | ターン | HP | 強さ | 面白さ | 敗因 | メモ |");
			L.Add("|---|---|---|---|---|---|---|---|---|");
			for (int k = 0; k < history.Count; k++)
			{
				BattleArchive h = history[k];
				BattleRating r2 = h.Rating;
				string feel;
				if (r2 != null && r2.LossFeel == "build")
				{
					feel = "構築の失敗";
				}
				else
				{
					feel = ((r2 != null && r2.LossFeel == "unfair") ? "理不尽" : "");
				}
				L.Add("| " + h.BattleNo + " | " + SafeEncounterName(h.EnemyId) + (h.Elite ? "（強個体）" : "") + " | " + ((h.Result == "won") ? "勝利" : "敗北") + " | " + h.Turns + " | " + h.HpBefore + "→" + h.HpAfter + " | " + ((r2 != null && r2.Strength.HasValue) ? r2.Strength.Value.ToString() : "") + " | " + ((r2 != null && r2.Fun.HasValue) ? r2.Fun.Value.ToString() : "") + " | " + feel + " | " + ((r2 != null) ? (r2.Note ?? "") : "").Trim().Replace("|", "｜") + " |");
			}
			L.Add("");
			for (int l = 0; l < history.Count; l++)
			{
				BattleArchive h2 = history[l];
				BattleRating r3 = h2.Rating;
				string rating = "";
				if (r3 != null)
				{
					rating = " / 評価:" + (r3.Strength.HasValue ? (" 強さ" + r3.Strength.Value) : "") + (r3.Fun.HasValue ? (" 面白さ" + r3.Fun.Value) : "") + (((r3.Note ?? "").Trim() != "") ? ("「" + r3.Note.Trim() + "」") : "");
				}
				L.Add("### " + h2.BattleNo + "戦目 " + SafeEncounterName(h2.EnemyId) + (h2.Elite ? "（強個体）" : "") + " — " + ((h2.Result == "won") ? "勝利" : "敗北") + " / " + h2.Turns + "ターン / HP " + h2.HpBefore + "→" + h2.HpAfter + " / デッキ" + h2.DeckSize + "枚" + rating);
				L.AddRange(h2.Lines);
				L.Add("");
			}
		}
		if (s != null)
		{
			L.Add("## 盤面（ターン " + s.Turn + " / " + s.Phase + "）");
			L.AddRange(RenderBoard(s, text));
			L.Add("");
			List<string> all = new List<string>();
			for (int m = 0; m < s.EventLog.Count; m++)
			{
				string line = ReportLine(s.EventLog[m], text);
				if (line != null)
				{
					all.Add(line);
				}
			}
			List<string> lines = ((all.Count > 600) ? all.Skip(all.Count - 600).ToList() : all);
			L.Add("## この戦闘のログ（" + lines.Count + "行" + ((all.Count > lines.Count) ? (" / 冒頭" + (all.Count - lines.Count) + "行は省略") : "") + "）");
			L.AddRange(lines);
		}
		else
		{
			L.Add("## 盤面");
			L.Add("（進行中の戦闘なし）");
		}
		L.Add("");
		L.Add("## 計測（機械可読）");
		L.Add("戦闘ごとのターン別 与ダメ/返し/被ダメ/伏せ/発動 と幕別サマリー (engine/analysis.ts)。");
		L.Add("`npm run analyze -- <このmd>` で表に展開できる。物差し: 良いデッキで通常戦4〜6T・ボス6〜10T。");
		L.Add("```json");
		L.Add(JsonConvert.SerializeObject(MetricsExportOf(run, history), JsonUnions.Settings));
		L.Add("```");
		L.Add("");
		L.Add("## 再開用スナップショット（sim/play.ts 互換。`npx tsx src/sim/play.ts show` で開ける）");
		L.Add("```json");
		if (run != null)
		{
			RunState r4 = ((run.Combat != null) ? run with
			{
				Combat = TrimLog(run.Combat)
			} : run);
			L.Add(JsonConvert.SerializeObject(new
			{
				kind = "run",
				run = r4,
				logIndex = ((r4.Combat != null) ? r4.Combat.EventLog.Count : 0)
			}, JsonUnions.Settings));
		}
		else
		{
			GameState b = ((state != null) ? TrimLog(state) : null);
			L.Add(JsonConvert.SerializeObject(new
			{
				kind = "battle",
				battle = b,
				logIndex = ((b != null) ? b.EventLog.Count : 0)
			}, JsonUnions.Settings));
		}
		L.Add("```");
		return string.Join("\n", L.ToArray());
	}

	public static string BuildRunSaveFile(RunState run, IReadOnlyList<BattleArchive> history, IReadOnlyList<PlayNote> playNotes, RunJournal? journal, IReadOnlyList<RunChoice> choices)
	{
		return SerializeRunSaveFile(MakeRunSaveFile(run, history, playNotes, journal, choices, null));
	}

	public static RunSaveFile MakeRunSaveFile(RunState run, IReadOnlyList<BattleArchive> history, IReadOnlyList<PlayNote> playNotes, RunJournal? journal, IReadOnlyList<RunChoice> choices, JToken? doodlesUnity)
	{
		return new RunSaveFile
		{
			Kind = "run",
			Run = run,
			LogIndex = ((run.Combat != null) ? run.Combat.EventLog.Count : 0),
			Fingerprint = DataFingerprint(),
			History = history.ToList(),
			PlayNotes = playNotes.ToList(),
			Journal = journal,
			Choices = ((choices.Count > 0) ? choices.ToList() : null),
			DoodlesUnity = doodlesUnity
		};
	}

	public static string SerializeRunSaveFile(RunSaveFile sf)
	{
		return JsonConvert.SerializeObject(sf, JsonUnions.Settings);
	}

	public static bool IsSaneRun(RunState? run)
	{
		if (run == null)
		{
			return false;
		}
		if (run.Rng == null || run.Deck == null || run.Map == null || run.Relics == null || run.Colors == null)
		{
			return false;
		}
		if (string.IsNullOrEmpty(run.Phase) || string.IsNullOrEmpty(run.LeaderId))
		{
			return false;
		}
		if (run.Phase == "combat" && run.Combat == null)
		{
			return false;
		}
		if (run.Combat != null && (run.Combat.Player == null || run.Combat.Enemies == null || run.Combat.Rng == null))
		{
			return false;
		}
		return true;
	}

	public static RunSaveFile? ReadRunSaveFile(string json, out string? warning)
	{
		warning = null;
		JObject jo = JObject.Parse(json);
		string kind = (string?)jo["kind"];
		if (kind != null && kind != "run")
		{
			warning = "ランのセーブ (kind:\"run\") ではない: " + kind;
			return null;
		}
		RunSaveFile sf = null;
		string firstError = null;
		try
		{
			sf = jo.ToObject<RunSaveFile>(JsonUnions.Serializer);
			if (sf != null && !IsSaneRun(sf.Run))
			{
				firstError = "状態の欄が欠けている";
				sf = null;
			}
		}
		catch (Exception ex)
		{
			firstError = ex.GetType().Name + ": " + ex.Message;
		}
		if (sf != null)
		{
			return sf;
		}
		JToken jt = jo["journal"];
		if (jt == null || jt.Type == JTokenType.Null)
		{
			warning = "状態を読めず、再生の記録 (journal) も無い: " + firstError;
			return null;
		}
		RunJournal journal;
		try
		{
			journal = JsonUnions.FromToken<RunJournal>(jt);
		}
		catch (Exception ex2)
		{
			warning = "状態を読めず (" + firstError + ")、記録も読めない: " + ex2.Message;
			return null;
		}
		var (states, err) = Run.ReplayStates(journal);
		if (err != null)
		{
			warning = "状態を読めず (" + firstError + ")、記録の再生も途中で止まった: " + err;
			return null;
		}
		RunSaveFile rebuilt = new RunSaveFile
		{
			Kind = "run",
			Run = states[states.Count - 1],
			LogIndex = ((states[states.Count - 1].Combat != null) ? states[states.Count - 1].Combat.EventLog.Count : 0),
			Fingerprint = (((string?)jo["fingerprint"]) ?? ""),
			Journal = journal,
			DoodlesUnity = jo["doodlesUnity"]
		};
		try
		{
			rebuilt.History = jo["history"]?.ToObject<List<BattleArchive>>(JsonUnions.Serializer) ?? new List<BattleArchive>();
		}
		catch (Exception)
		{
		}
		try
		{
			rebuilt.PlayNotes = jo["playNotes"]?.ToObject<List<PlayNote>>(JsonUnions.Serializer) ?? new List<PlayNote>();
		}
		catch (Exception)
		{
		}
		try
		{
			rebuilt.Choices = jo["choices"]?.ToObject<List<RunChoice>>(JsonUnions.Serializer);
		}
		catch (Exception)
		{
		}
		warning = "状態を直接読めなかったので記録 (" + journal.Commands.Count + "手) を再生して復元した: " + firstError;
		return rebuilt;
	}

	public static RunChoice? DescribeRunChoice(RunState prev, RunCommand cmd, RunState next)
	{
		RunChoice core = DescribeRunChoiceCore(prev, cmd, next);
		if (core == null)
		{
			return null;
		}
		core.Ctx = new RunChoiceCtx
		{
			Hp = next.Hp,
			MaxHp = next.MaxHp,
			Deck = next.Deck.Count,
			Gold = next.Gold
		};
		return core;
	}

	private static string JoinNames(IEnumerable<string> ids)
	{
		return string.Join("・", ids.Select(CardName).ToArray());
	}

	private static RunChoice? DescribeRunChoiceCore(RunState prev, RunCommand cmd, RunState next)
	{
		string at = "幕" + next.Act + " 行" + (next.Row + 1);
		if (!(cmd is RunCommand_ChooseNode))
		{
			RunCommand_PickReward runCommand_PickReward = cmd as RunCommand_PickReward;
			if ((object)runCommand_PickReward == null)
			{
				if (!(cmd is RunCommand_SkipReward))
				{
					if (!(cmd is RunCommand_TakeGear tg))
					{
						if (!(cmd is RunCommand_SkipGear))
						{
							if (!(cmd is RunCommand_UseGear ug))
							{
								if (!(cmd is RunCommand_DiscardGear dg))
								{
									if (!(cmd is RunCommand_ShopBuyGear sg))
									{
										if (!(cmd is RunCommand_ShopBuyMana))
										{
											if (!(cmd is RunCommand_BuyDeparture bd))
											{
												if (!(cmd is RunCommand_LeaveDeparture))
												{
													if (!(cmd is RunCommand_ShopBuyDeparture sd))
													{
														RunCommand_PickRelic runCommand_PickRelic = cmd as RunCommand_PickRelic;
														if ((object)runCommand_PickRelic == null)
														{
															if (!(cmd is RunCommand_SkipRelic))
															{
																if (!(cmd is RunCommand_CampfireRest))
																{
																	if (!(cmd is RunCommand_CampfireUpgrade cu))
																	{
																		if (!(cmd is RunCommand_CampfireRemove cr))
																		{
																			if (!(cmd is RunCommand_CampfireDig))
																			{
																				if (!(cmd is RunCommand_CampfireTrain))
																				{
																					if (!(cmd is RunCommand_RelicChooseCards rc))
																					{
																						if (!(cmd is RunCommand_WorkshopFuse wf))
																						{
																							if (!(cmd is RunCommand_WorkshopSkip))
																							{
																								if (!(cmd is RunCommand_ShopBuyCard sb))
																								{
																									if (!(cmd is RunCommand_ShopBuyRelic))
																									{
																										if (!(cmd is RunCommand_ShopRemove sr))
																										{
																											if (!(cmd is RunCommand_ShopUpgrade su))
																											{
																												if (cmd is RunCommand_EventChoice ec)
																												{
																													if (prev.EventId == null)
																													{
																														return null;
																													}
																													string evName = prev.EventId;
																													string label = "選択肢" + ec.Index;
																													try
																													{
																														EventDef ev = Content.GetEventDef(prev.EventId);
																														evName = ev.Name;
																														if (ec.Index >= 0 && ec.Index < ev.Choices.Count)
																														{
																															label = ev.Choices[ec.Index].Label;
																														}
																													}
																													catch (Exception)
																													{
																													}
																													CardInstance target = ((ec.CardIndex.HasValue && ec.CardIndex.Value >= 0 && ec.CardIndex.Value < prev.Deck.Count) ? prev.Deck[ec.CardIndex.Value] : null);
																													int num = next.Hp - prev.Hp;
																													int goldDiff = next.Gold - prev.Gold;
																													List<string> gotRelics = next.Relics.Where((string value) => !prev.Relics.Contains(value)).Select((string text) =>
																													{
																														try
																														{
																															RelicDef relicDef = Content.GetRelicDef(text);
																															string text2 = RelicRarityTag(relicDef);
																															return ((text2 != "") ? (text2 + " ") : "") + relicDef.Name;
																														}
																														catch (Exception)
																														{
																															return text;
																														}
																													}).ToList();
																													HashSet<string> prevUidSet = new HashSet<string>(prev.Deck.Select((CardInstance c) => c.Uid));
																													List<string> gotCards = (from c in next.Deck
																														where !prevUidSet.Contains(c.Uid)
																														select c.Def.Name).ToList();
																													int prevWard = 0;
																													int nextWard = 0;
																													if (prev.RelicState != null)
																													{
																														prev.RelicState.TryGetValue("brandWard", out prevWard);
																													}
																													if (next.RelicState != null)
																													{
																														next.RelicState.TryGetValue("brandWard", out nextWard);
																													}
																													int warded = prevWard - nextWard;
																													int maxHpGain = Math.Max(0, next.MaxHp - prev.MaxHp);
																													int hpShown = num - maxHpGain;
																													string hpText = ((hpShown != 0) ? ("HP" + ((hpShown > 0) ? "+" : "") + hpShown + ((maxHpGain > 0) ? ("（最大HP+" + maxHpGain + " で HP も+" + maxHpGain + "）") : "")) : "");
																													string[] outcome = new string[6]
																													{
																														(gotRelics.Count > 0) ? ("獲得レリック: " + string.Join("・", gotRelics.ToArray())) : "",
																														(gotCards.Count > 0) ? ("獲得: " + string.Join("・", gotCards.ToArray())) : "",
																														hpText,
																														(maxHpGain > 0) ? ("最大HP+" + maxHpGain) : "",
																														(goldDiff != 0) ? (((goldDiff > 0) ? "+" : "") + goldDiff + "G") : "",
																														(warded > 0) ? ("\ud83c\udff7\ufe0f厄除けの札が烙印" + warded + "枚を防いだ（残り" + nextWard + "）") : ""
																													}.Where((string text) => text != "").ToArray();
																													return Mk("イベント[" + evName + "]: " + label + ((target != null) ? ("（対象: " + target.Def.Name + "）") : "") + ((outcome.Length != 0) ? ("（" + string.Join("・", outcome) + "）") : ""));
																												}
																												return null;
																											}
																											if (su.Index < 0 || su.Index >= prev.Deck.Count || su.Index >= next.Deck.Count)
																											{
																												return null;
																											}
																											return Mk("ショップ: 鍛えた " + prev.Deck[su.Index].Def.Name + " → " + next.Deck[su.Index].Def.Name + "（" + (prev.Gold - next.Gold) + "G）");
																										}
																										if (sr.Index < 0 || sr.Index >= prev.Deck.Count)
																										{
																											return null;
																										}
																										return Mk("ショップ: " + prev.Deck[sr.Index].Def.Name + " を除去（" + (prev.Gold - next.Gold) + "G）");
																									}
																									if (prev.Shop == null || prev.Shop.RelicId == null)
																									{
																										return null;
																									}
																									return Mk("ショップ: レリック " + SafeRelicName(prev.Shop.RelicId) + " を" + prev.Shop.RelicPrice + "Gで購入");
																								}
																								if (prev.Shop == null || sb.Index < 0 || sb.Index >= prev.Shop.Cards.Count)
																								{
																									return null;
																								}
																								ShopStateCards item = prev.Shop.Cards[sb.Index];
																								return Mk("ショップ: " + CardName(item.Id) + " を" + item.Price + "Gで購入");
																							}
																							return Mk("工房: 見送り");
																						}
																						if (wf.IndexA < 0 || wf.IndexA >= prev.Deck.Count || wf.IndexB < 0 || wf.IndexB >= prev.Deck.Count)
																						{
																							return null;
																						}
																						CardInstance a = prev.Deck[wf.IndexA];
																						CardInstance b = prev.Deck[wf.IndexB];
																						HashSet<string> prevUids = new HashSet<string>(prev.Deck.Select((CardInstance c) => c.Uid));
																						CardInstance made = next.Deck.FirstOrDefault((CardInstance c) => !prevUids.Contains(c.Uid));
																						if (made == null)
																						{
																							return null;
																						}
																						return Mk("工房: " + a.Def.Name + " × " + b.Def.Name + " → " + made.Def.Name + "（" + ((made.Def.XCost == true) ? "X" : made.Def.Cost.ToString()) + "E）");
																					}
																					RunStatePendingRelicChoice p = prev.PendingRelicChoice;
																					string relicName = ((p != null) ? SafeRelicName(p.RelicId) : "レリック");
																					List<string> names = new List<string>();
																					if (rc.Indices != null)
																					{
																						foreach (int i in rc.Indices)
																						{
																							if (i >= 0 && i < prev.Deck.Count)
																							{
																								names.Add(prev.Deck[i].Def.Name);
																							}
																						}
																					}
																					if (names.Count == 0)
																					{
																						return Mk(relicName + ": 何も選ばなかった");
																					}
																					return Mk(relicName + ": " + string.Join("・", names.ToArray()) + " を" + ((p != null && p.Mode == "remove") ? "取り除いた" : "変成して鍛えた"));
																				}
																				int train = 0;
																				if (next.RelicState != null)
																				{
																					next.RelicState.TryGetValue("train", out train);
																				}
																				return Mk("焚き火: 鍛錬（重石。戦闘開始時の成長 +" + train + "）");
																			}
																			List<string> dug = next.Relics.Where((string value) => !prev.Relics.Contains(value)).Select(SafeRelicName).ToList();
																			return Mk("焚き火: 発掘 → " + ((dug.Count > 0) ? string.Join("・", dug.ToArray()) : "レリックは尽きていた"));
																		}
																		if (cr.Index < 0 || cr.Index >= prev.Deck.Count)
																		{
																			return null;
																		}
																		return Mk("焚き火: " + prev.Deck[cr.Index].Def.Name + " を取り除いた（安らぎの煙管）");
																	}
																	if (cu.Index < 0 || cu.Index >= prev.Deck.Count || cu.Index >= next.Deck.Count)
																	{
																		return null;
																	}
																	return Mk("焚き火: 鍛えた " + prev.Deck[cu.Index].Def.Name + " → " + next.Deck[cu.Index].Def.Name);
																}
																if (next.Hp == prev.Hp && prev.Hp < prev.MaxHp)
																{
																	string cup = null;
																	foreach (string id in prev.Relics)
																	{
																		try
																		{
																			RelicDef rd = Content.GetRelicDef(id);
																			RelicDefBonus? bonus = rd.Bonus;
																			if ((object)bonus != null && bonus.NoRest == true)
																			{
																				cup = rd.Name;
																				break;
																			}
																		}
																		catch (Exception)
																		{
																		}
																	}
																	string why = ((cup != null) ? (cup + "で休めない") : "すでに鍛えたので回復なし");
																	return Mk("焚き火: 休む → 回復なしで立ち去った（" + why + "・HP " + prev.Hp + "/" + prev.MaxHp + "）");
																}
																return Mk("焚き火: 休む（HP " + prev.Hp + "→" + next.Hp + "）");
															}
															IReadOnlyList<string> opts = prev.RelicOptions ?? new List<string>();
															string t = string.Join("・", opts.Select(SafeRelicName).ToArray());
															return Mk("レリック: 見送り（候補: " + ((t != "") ? t : "なし") + "）");
														}
														IReadOnlyList<string> opts2 = prev.RelicOptions ?? new List<string>();
														if (runCommand_PickRelic.Index < 0 || runCommand_PickRelic.Index >= opts2.Count)
														{
															return null;
														}
														string passed = string.Join("・", opts2.Where((string _, int num2) => num2 != runCommand_PickRelic.Index).Select(SafeRelicName).ToArray());
														return Mk("レリック: " + SafeRelicName(opts2[runCommand_PickRelic.Index]) + " を獲得（見送り: " + ((passed != "") ? passed : "なし") + "）");
													}
													IReadOnlyList<ShopStateDepartures> shelf = prev.Shop?.Departures ?? new List<ShopStateDepartures>();
													ShopStateDepartures slot = ((sd.Index >= 0 && sd.Index < shelf.Count) ? shelf[sd.Index] : null);
													DepartureOffer o = ((!(slot != null)) ? null : prev.Departure?.Leftovers.FirstOrDefault((DepartureOffer departureOffer) => departureOffer.Id == slot.Id));
													if (o == null || slot == null)
													{
														return null;
													}
													CardInstance target2 = ((sd.CardIndex.HasValue && sd.CardIndex.Value >= 0 && sd.CardIndex.Value < prev.Deck.Count) ? prev.Deck[sd.CardIndex.Value] : null);
													return Mk("ショップ: 行商が預かった支度「" + o.Name + "」（" + DepartureDetail(o) + "）を" + slot.Price + "Gで買った" + ((target2 != null) ? ("（対象: " + target2.Def.Name + "）") : ""));
												}
												IReadOnlyList<string> boughtIds = prev.Departure?.Bought ?? new List<string>();
												string bought = string.Join("・", (from departureOffer in prev.Departure?.Offers ?? new List<DepartureOffer>()
													where boughtIds.Contains(departureOffer.Id)
													select departureOffer.Name).ToArray());
												string carried = string.Join("・", (next.Departure?.Leftovers ?? new List<DepartureOffer>()).Select((DepartureOffer departureOffer) => departureOffer.Name).ToArray());
												return Mk("出立: 店を出て坑へ（買った: " + ((bought != "") ? bought : "なし") + "／行商が担いで降りる: " + ((carried != "") ? carried : "なし") + "）");
											}
											IReadOnlyList<DepartureOffer> offers = prev.Departure?.Offers ?? new List<DepartureOffer>();
											if (bd.Index < 0 || bd.Index >= offers.Count)
											{
												return null;
											}
											DepartureOffer o2 = offers[bd.Index];
											CardInstance target3 = ((bd.CardIndex.HasValue && bd.CardIndex.Value >= 0 && bd.CardIndex.Value < prev.Deck.Count) ? prev.Deck[bd.CardIndex.Value] : null);
											string how = o2.Price + "Gで買った";
											return Mk("出立の店: " + o2.Name + "（" + DepartureDetail(o2) + "）を" + how + ((target3 != null) ? ("（対象: " + target3.Def.Name + "）") : ""));
										}
										return Mk("ショップ: 魔素を購入（" + prev.Mana.GetValueOrDefault() + "→" + next.Mana.GetValueOrDefault() + "・" + (prev.Shop?.ManaPrice).GetValueOrDefault() + "G）");
									}
									IReadOnlyList<ShopStateGears> shelf2 = prev.Shop?.Gears ?? new List<ShopStateGears>();
									if (sg.Index < 0 || sg.Index >= shelf2.Count)
									{
										return null;
									}
									ShopStateGears slot2 = shelf2[sg.Index];
									return Mk("ショップ: ギア " + GearName(slot2.Id) + " を " + slot2.Price + "G で購入");
								}
								IReadOnlyList<GearInstance> prevGears = prev.Gears ?? new List<GearInstance>();
								if (dg.Index >= 0 && dg.Index < prevGears.Count)
								{
									return Mk("ギアを捨てた: " + GearName(prevGears[dg.Index].GearId));
								}
								return null;
							}
							IReadOnlyList<GearInstance> prevGears2 = prev.Gears ?? new List<GearInstance>();
							if (ug.Index < 0 || ug.Index >= prevGears2.Count)
							{
								return null;
							}
							GearInstance gI = prevGears2[ug.Index];
							string asText = ((ug.AsGearId != null) ? ("（" + GearName(ug.AsGearId) + " として）") : "");
							GearInstance left = null;
							foreach (GearInstance x in next.Gears ?? new List<GearInstance>())
							{
								if (x.Uid == gI.Uid)
								{
									left = x;
									break;
								}
							}
							return Mk("ギア使用: " + GearName(gI.GearId) + asText + "（魔素 " + prev.Mana.GetValueOrDefault() + "→" + next.Mana.GetValueOrDefault() + ((left != null) ? ("・残" + left.Charges + "回") : "・使い切り") + "）");
						}
						if (prev.GearOption != null)
						{
							return Mk("ギア見送り: " + GearName(prev.GearOption));
						}
						return null;
					}
					string id2 = prev.GearOption;
					if (id2 == null)
					{
						return null;
					}
					IReadOnlyList<GearInstance> prevGears3 = prev.Gears ?? new List<GearInstance>();
					GearInstance dropped = ((tg.DiscardIndex.HasValue && tg.DiscardIndex.Value >= 0 && tg.DiscardIndex.Value < prevGears3.Count) ? prevGears3[tg.DiscardIndex.Value] : null);
					return Mk("ギア取得: " + GearName(id2) + ((dropped != null) ? ("（" + GearName(dropped.GearId) + " と入れ替え）") : ""));
				}
				string t2 = JoinNames(prev.RewardOptions ?? new List<string>());
				return Mk("報酬ピック: スキップ（候補: " + ((t2 != "") ? t2 : "なし") + "）");
			}
			IReadOnlyList<string> opts3 = prev.RewardOptions ?? new List<string>();
			if (runCommand_PickReward.Index < 0 || runCommand_PickReward.Index >= opts3.Count)
			{
				return null;
			}
			string passedText = JoinNames(opts3.Where((string _, int num2) => num2 != runCommand_PickReward.Index));
			return Mk("報酬ピック: " + CardName(opts3[runCommand_PickReward.Index]) + " を獲得（見送り: " + ((passedText != "") ? passedText : "なし") + "）");
		}
		if (next.Row < 0 || next.Row >= next.Map.Count || next.Col >= next.Map[next.Row].Count)
		{
			return null;
		}
		MapNode node = next.Map[next.Row][next.Col];
		NODE_LABEL.TryGetValue(node.Type, out string lbl);
		string label2 = ((node.EncounterId != null) ? (lbl + " " + SafeEncounterName(node.EncounterId)) : (lbl ?? node.Type));
		return Mk("進路: " + label2);
		RunChoice Mk(string text)
		{
			return new RunChoice
			{
				At = at,
				Text = text
			};
		}
	}
}
