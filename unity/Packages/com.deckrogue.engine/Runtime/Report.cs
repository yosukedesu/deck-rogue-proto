// Report.cs — プレイレポート (.md) とセーブ (.json) の組み立て (純関数)。src/ui/report.ts の DOM に触れない部分の移植。
// 2026-09-14 ユーザー「プロトと同じようにフィードバックを記録、解析用のログを出力する機構を APK/Unity 版でも」。
// 書き出す形はブラウザ版と同じ (md の節・末尾の「計測（機械可読）」JSON・sim/play.ts 互換のスナップショット) = `npm run analyze -- <md>` と
// `npx tsx src/sim/play.ts show <save.json>` がそのまま読める。ログ行の文言だけは表示層 (Unity は CardText.LogLine) から ReportText で受け取る。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;

#nullable enable

namespace DeckRogue.Engine
{
    /// <summary>決着した1戦闘の保管 (ui/report.ts BattleArchive)</summary>
    public sealed class BattleArchive
    {
        [JsonProperty("battleNo")] public int BattleNo;
        [JsonProperty("act", NullValueHandling = NullValueHandling.Ignore)] public int? Act;
        [JsonProperty("boss", NullValueHandling = NullValueHandling.Ignore)] public bool? Boss;
        [JsonProperty("metrics", NullValueHandling = NullValueHandling.Ignore)] public BattleMetrics? Metrics;
        [JsonProperty("enemyId")] public string EnemyId = "";
        [JsonProperty("elite")] public bool Elite;
        /// <summary>"won" | "lost"</summary>
        [JsonProperty("result")] public string Result = "won";
        [JsonProperty("turns")] public int Turns;
        [JsonProperty("hpBefore")] public int HpBefore;
        [JsonProperty("hpAfter")] public int HpAfter;
        [JsonProperty("deckSize")] public int DeckSize;
        [JsonProperty("lines")] public List<string> Lines = new List<string>();
        /// <summary>戦闘直後にプレイヤーが入力した評価 (任意。未入力なら null)</summary>
        [JsonProperty("rating", NullValueHandling = NullValueHandling.Ignore)] public BattleRating? Rating;
    }

    /// <summary>プレイ中メモ (UI層の観察記録。ゲーム状態ではない)</summary>
    public sealed class PlayNote
    {
        /// <summary>ISO時刻</summary>
        [JsonProperty("at")] public string At = "";
        /// <summary>記録時の文脈 (幕/行/フェーズ/ターン)</summary>
        [JsonProperty("context")] public string Context = "";
        [JsonProperty("text")] public string Text = "";
    }

    public sealed class RunChoiceCtx
    {
        [JsonProperty("hp")] public int Hp;
        [JsonProperty("maxHp")] public int MaxHp;
        [JsonProperty("deck")] public int Deck;
        [JsonProperty("gold")] public int Gold;
    }

    /// <summary>ランの意思決定1件 (ui/report.ts RunChoice)。text は人間向けの1行 (見送った候補も含む)</summary>
    public sealed class RunChoice
    {
        /// <summary>「幕N 行M」</summary>
        [JsonProperty("at")] public string At = "";
        [JsonProperty("text")] public string Text = "";
        [JsonProperty("ctx", NullValueHandling = NullValueHandling.Ignore)] public RunChoiceCtx? Ctx;
    }

    /// <summary>ランのセーブファイル (sim/play.ts の SaveFile 互換 = CLI/ブラウザでもそのまま開ける)。history 以降は UI 側の拡張欄</summary>
    public sealed class RunSaveFile
    {
        [JsonProperty("kind")] public string Kind = "run";
        [JsonProperty("run")] public RunState Run = default!;
        [JsonProperty("logIndex")] public int LogIndex;
        [JsonProperty("fingerprint")] public string Fingerprint = "";
        [JsonProperty("history")] public List<BattleArchive> History = new List<BattleArchive>();
        [JsonProperty("playNotes")] public List<PlayNote> PlayNotes = new List<PlayNote>();
        [JsonProperty("journal", NullValueHandling = NullValueHandling.Ignore)] public RunJournal? Journal;
        [JsonProperty("choices", NullValueHandling = NullValueHandling.Ignore)] public List<RunChoice>? Choices;
    }

    /// <summary>ログ行と意図の文言は表示層のもの (Unity は CardText)。省略時はイベント型名・意図の種別と実値</summary>
    public sealed class ReportText
    {
        public Func<GameEvent, string?> LogLine = ev => ev.Type;
        public Func<GameState, int, string> IntentText = (st, i) =>
        {
            var it = Effects.EffectiveIntent(st, i);
            return it == null ? "?" : it.Kind + " " + it.Actual + (it.Hits.HasValue && it.Hits.Value > 1 ? "×" + it.Hits.Value : "");
        };
    }

    public static class Report
    {
        /// <summary>1戦闘あたりの保管ログ行数の上限 (10戦ぶんでもファイルが読める範囲に収める)</summary>
        public const int ARCHIVE_LINES_CAP = 300;
        const int LOG_CAP = 600;
        /// <summary>スナップショットの eventLog 上限。engine は eventLog を読まないので切り詰めても再開挙動は不変</summary>
        const int SNAPSHOT_LOG_CAP = 400;

        // ---- 名前解決の安全版 (レポートはデータ回収の道具なので未知IDで例外死させない) ----

        public static string SafeEncounterName(string id)
        {
            try { return Content.EncounterName(id); } catch (Exception) { return id; }
        }

        public static string SafeEnemyName(string id)
        {
            try { return Content.GetEnemyDef(id).Name; } catch (Exception) { return id; }
        }

        public static string SafeRelicName(string id)
        {
            try { return Content.GetRelicDef(id).Name; } catch (Exception) { return id; }
        }

        /// <summary>カードIDから名前 (合成札は合成の解決器で復元する)。ui/log.ts cardName と同じ</summary>
        public static string CardName(string cardId)
        {
            try { return Content.GetCardDef(cardId).Name; }
            catch (Exception)
            {
                try { var f = Fusion.ResolveFusedDef(cardId); return f != null ? f.Name : cardId; }
                catch (Exception) { return cardId; }
            }
        }

        static string RelicRarityTag(RelicDef def)
        {
            switch (def.Rarity ?? "common")
            {
                case "uncommon": return "◆アンコモン";
                case "rare": return "★レア";
                case "boss": return "👑ボス";
                case "shop": return "🛒店売り";
                case "event": return "❓イベント";
                default: return "";
            }
        }

        static string Names(IReadOnlyList<CardInstance> cs)
        {
            if (cs == null || cs.Count == 0) return "（なし）";
            var b = new List<string>();
            for (int i = 0; i < cs.Count; i++) b.Add(cs[i].Def.Name);
            return string.Join("、", b.ToArray());
        }

        /// <summary>
        /// カードデータの指紋 (ui/report.ts dataFingerprint と同じ式)。読む側が「同じビルドか」を判定する。
        /// JS の ((h * 33) ^ code) >>> 0 を uint で再現する (h*33 は 2^38 未満なので double でも正確)
        /// </summary>
        public static string DataFingerprint()
        {
            uint h = 5381;
            var cards = Content.AllCards;
            for (int c = 0; c < cards.Count; c++)
            {
                var def = cards[c];
                string s = def.Id + ":" + def.Cost.ToString(CultureInfo.InvariantCulture) + ":" + def.Effects.Count.ToString(CultureInfo.InvariantCulture);
                for (int i = 0; i < s.Length; i++)
                {
                    ulong m = (ulong)h * 33UL;
                    h = (uint)(m & 0xFFFFFFFFUL) ^ (uint)s[i];
                }
            }
            return "cards" + cards.Count + "/enemies" + Content.AllEnemies.Count + "/" + ToBase36(h);
        }

        static string ToBase36(uint v)
        {
            const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
            if (v == 0) return "0";
            var sb = new StringBuilder();
            while (v > 0) { sb.Insert(0, digits[(int)(v % 36)]); v /= 36; }
            return sb.ToString();
        }

        // ---- 戦闘の保管 ----

        /// <summary>決着した combat を保管形式に変換する (ui/report.ts archiveBattle)</summary>
        public static BattleArchive ArchiveBattle(GameState combat, int battleNo, string enemyId, bool elite, int hpBefore, int deckSize, int act, bool boss, ReportText text)
        {
            var all = new List<string>();
            for (int i = 0; i < combat.EventLog.Count; i++)
            {
                var line = ReportLine(combat.EventLog[i], text);
                if (line != null) all.Add(line);
            }
            return new BattleArchive
            {
                BattleNo = battleNo,
                Act = act,
                Boss = boss,
                Metrics = Analysis.ComputeBattleMetrics(combat.EventLog),
                EnemyId = enemyId,
                Elite = elite,
                Result = combat.Phase == CombatPhases.Won ? "won" : "lost",
                Turns = combat.Turn,
                HpBefore = hpBefore,
                HpAfter = combat.Player.Hp,
                DeckSize = deckSize,
                Lines = all.Count > ARCHIVE_LINES_CAP ? all.Skip(all.Count - ARCHIVE_LINES_CAP).ToList() : all,
            };
        }

        /// <summary>戦闘アーカイブ → 分析行 (analysis BattleRow)</summary>
        public static List<BattleRow> ToBattleRows(IReadOnlyList<BattleArchive> history)
        {
            var rows = new List<BattleRow>();
            for (int i = 0; i < history.Count; i++)
            {
                var h = history[i];
                rows.Add(new BattleRow
                {
                    BattleNo = h.BattleNo,
                    Act = h.Act ?? 0,
                    EnemyId = h.EnemyId,
                    Elite = h.Elite,
                    Boss = h.Boss ?? false,
                    Result = h.Result,
                    HpBefore = h.HpBefore,
                    HpAfter = h.HpAfter,
                    Metrics = h.Metrics,
                    Rating = h.Rating,
                });
            }
            return rows;
        }

        /// <summary>レポート末尾の「計測（機械可読）」ブロックの中身 (scripts/analyze-run.ts と共有する契約)</summary>
        public static MetricsExport MetricsExportOf(RunState? run, IReadOnlyList<BattleArchive> history)
        {
            var battles = ToBattleRows(history);
            return new MetricsExport
            {
                Schema = "deck-rogue-metrics/1",
                Fingerprint = DataFingerprint(),
                Run = run != null ? new MetricsRunSummary { Act = run.Act, BattlesWon = run.BattlesWon, Hp = run.Hp, DeckSize = run.Deck.Count } : null,
                Battles = battles,
                Acts = Analysis.ActSummaries(battles),
            };
        }

        /// <summary>判断時間 (ジャーナルの記録時刻の差から「どこで長考したか」)。合計・戦闘内/外の平均・長考トップ8</summary>
        public static List<string> DecisionTimeLines(RunJournal? journal)
        {
            var outp = new List<string>();
            if (journal == null || journal.Times == null || journal.Times.Count < 2) return outp;
            var t = journal.Times;
            var gaps = new List<(int I, long Ms, string Label)>();
            for (int i = 1; i < t.Count && i < journal.Commands.Count + 1; i++)
            {
                if (i >= journal.Commands.Count) break;
                var cmd = journal.Commands[i];
                string label;
                var cc = cmd as RunCommand_Combat;
                if (cc != null)
                {
                    var pc = cc.Command as Command_PlayCard;
                    var sc = cc.Command as Command_SetCard;
                    string uid = pc != null ? " " + pc.CardUid : sc != null ? " " + sc.CardUid : "";
                    label = "戦闘: " + cc.Command.Type + uid;
                }
                else label = cmd.Type;
                gaps.Add((i, t[i] - t[i - 1], label));
            }
            if (gaps.Count == 0) return outp;
            long total = t[t.Count - 1] - t[0];
            var inCombat = gaps.Where(g => g.Label.StartsWith("戦闘")).ToList();
            var outCombat = gaps.Where(g => !g.Label.StartsWith("戦闘")).ToList();
            string Avg(List<(int I, long Ms, string Label)> xs) => xs.Count > 0 ? (xs.Sum(x => x.Ms) / (double)xs.Count / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) : "-";
            outp.Add("- 合計 " + (total / 60000.0).ToString("0.0", CultureInfo.InvariantCulture) + "分・コマンド" + journal.Commands.Count + "件（1手あたり 戦闘内" + Avg(inCombat) + "秒／戦闘外" + Avg(outCombat) + "秒）");
            outp.Add("- 長考トップ8（直前の手からの経過秒 → 決めた手）:");
            foreach (var g in gaps.OrderByDescending(g => g.Ms).Take(8))
                outp.Add("  - " + (g.Ms / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + "秒 → [" + g.I + "] " + g.Label);
            return outp;
        }

        /// <summary>ログ行。敵行動フック (Executing/Resolved) は定型ノイズなので描かない</summary>
        static string? ReportLine(GameEvent e, ReportText text)
        {
            if (e is GameEvent_EnemyPhaseEnded) return "敵フェーズ終了";
            var cd = e as GameEvent_CardsDrawn;
            if (cd != null) return "ドロー" + cd.Count + "枚" + (cd.Cards != null ? ": " + string.Join("・", cd.Cards.ToArray()) : "");
            var te = e as GameEvent_TurnEnded;
            if (te != null) return "ターン終了" + (te.Unplayed != null ? "（未使用: " + (te.Unplayed.Count > 0 ? string.Join("・", te.Unplayed.ToArray()) : "なし") + "）" : "");
            try { return text.LogLine(e); } catch (Exception) { return e.Type; }
        }

        static List<string> RenderBoard(GameState s, ReportText text)
        {
            var p = s.Player;
            var outp = new List<string>();
            var st = new[]
            {
                p.Growth != 0 ? "成長" + p.Growth : "", p.Momentum != 0 ? "勢い" + p.Momentum : "",
                p.IceBlock != 0 ? "氷壁" + p.IceBlock : "", p.Aether != 0 ? "霊気" + p.Aether : "",
                p.NextCardDiscount != 0 ? "次コスト-" + p.NextCardDiscount : "", p.Weak != 0 ? "弱体" + p.Weak : "",
                p.Vulnerable != 0 ? "脆弱" + p.Vulnerable : "", p.Frail != 0 ? "虚弱" + p.Frail : "",
                p.Restrain != 0 ? "拘束" + p.Restrain : "", (p.Mist ?? 0) != 0 ? "霞み" + p.Mist : "",
                (p.Slow ?? 0) != 0 ? "重り" + p.Slow : "",
            }.Where(x => x != "").ToArray();
            outp.Add("自分: HP " + p.Hp + "/" + p.MaxHp + " ブロック" + p.Block + " エナジー" + p.Energy + "/" + p.EnergyMax + " " + string.Join(" ", st));
            var hand = new List<string>();
            for (int i = 0; i < p.Hand.Count; i++)
            {
                var c = p.Hand[i];
                int cost = 0; bool ok = false;
                try { cost = Effects.EffectiveCost(s, c); ok = Effects.IsPlayableFromHand(c, s) && cost <= p.Energy; } catch (Exception) { }
                hand.Add(c.Def.Name + "(" + cost + ")" + (ok ? "" : "✕"));
            }
            outp.Add("手札(" + p.Hand.Count + "): " + (hand.Count > 0 ? string.Join("、", hand.ToArray()) : "（なし）"));
            outp.Add("伏せ場(" + p.SetCards.Count + "/" + p.SetSlots + "): " + Names(p.SetCards));
            outp.Add("置物: " + Names(p.Permanents));
            outp.Add("山札" + p.DrawPile.Count + " / 捨札" + p.DiscardPile.Count + " / 消滅" + p.ExhaustPile.Count);
            for (int i = 0; i < s.Enemies.Count; i++)
            {
                var e = s.Enemies[i];
                if (e.Hp <= 0) { outp.Add("敵" + (i + 1) + " " + SafeEnemyName(e.EnemyId) + ": 撃破済み"); continue; }
                var dbg = new[]
                {
                    e.Strength != 0 ? "強化" + (e.Strength > 0 ? "+" : "") + e.Strength : "", e.Block != 0 ? "ブロック" + e.Block : "",
                    e.Burn != 0 ? "延焼" + e.Burn : "", e.Confusion != 0 ? "混乱" + e.Confusion : "", e.Exposed != 0 ? "急所" + e.Exposed : "",
                }.Where(x => x != "").ToArray();
                string intent;
                try { intent = text.IntentText(s, i); } catch (Exception) { intent = "?"; }
                outp.Add("敵" + (i + 1) + " " + SafeEnemyName(e.EnemyId) + ": HP " + e.Hp + "/" + e.MaxHp + " " + string.Join(" ", dbg) + " → " + intent);
            }
            if (s.PendingWindow != null)
            {
                var w = s.PendingWindow;
                var en = w.EnemyIndex >= 0 && w.EnemyIndex < s.Enemies.Count ? s.Enemies[w.EnemyIndex] : null;
                outp.Add("★確認ウィンドウ待ち: 敵" + (w.EnemyIndex + 1) + " " + (en != null ? SafeEnemyName(en.EnemyId) : "?") + " / " + w.Stage + "窓 / 実値 " + (en != null && en.Intent != null ? en.Intent.Actual.ToString() : "?"));
            }
            return outp;
        }

        static GameState TrimLog(GameState s)
        {
            if (s.EventLog.Count <= SNAPSHOT_LOG_CAP) return s;
            // 先頭の CombatStarted は編成IDの唯一の記録なので必ず残す
            var log = new List<GameEvent> { s.EventLog[0] };
            log.AddRange(s.EventLog.Skip(s.EventLog.Count - (SNAPSHOT_LOG_CAP - 1)));
            return s with { EventLog = log };
        }

        static string NodeLabel(MapNode n)
        {
            if (n.EncounterId != null) return SafeEncounterName(n.EncounterId);
            switch (n.Type)
            {
                case MapNodeTypes.Campfire: return "焚き火";
                case MapNodeTypes.Workshop: return "工房";
                case MapNodeTypes.Shop: return "ショップ";
                default: return "?";
            }
        }

        /// <summary>レポート本文 (ui/report.ts buildReport)。now は書き出し時刻 (engine は時計を持たないので呼び出し側が渡す)</summary>
        public static string BuildReport(RunState? run, GameState? state, IReadOnlyList<BattleArchive> history, string note, IReadOnlyList<PlayNote> playNotes,
            IReadOnlyList<RunChoice> choices, RunJournal? journal, ReportText text, DateTime now)
        {
            var s = run != null ? run.Combat : state;
            var L = new List<string>();
            L.Add("# プレイ状況レポート");
            L.Add("書き出し: " + now.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture) + " / データ指紋: " + DataFingerprint() + " / Unity");
            if (!string.IsNullOrEmpty(note)) L.Add("メモ: " + note);
            L.Add("");
            L.Add("## いまの状況");
            if (run != null)
            {
                string leaderName = run.LeaderId;
                try { leaderName = Content.GetLeaderDef(run.LeaderId).Name; } catch (Exception) { }
                L.Add("ラン " + leaderName + "（" + run.LeaderId + "） / seed " + run.Seed + " / mode " + run.Mode + " / 難易度 " + run.Difficulty);
                L.Add("進行: " + run.Phase + " / 幕" + run.Act + "/3 行" + (run.Row + 1) + "/" + run.Map.Count + "・" + run.BattlesWon + "勝" + (run.CurrentElite ? "（強個体）" : "") + " / HP " + run.Hp + "/" + run.MaxHp + " / 💰" + run.Gold + "G / デッキ" + run.Deck.Count + "枚");
                var rows = new List<string>();
                for (int r = 0; r < run.Map.Count; r++)
                {
                    var cells = new List<string>();
                    for (int c = 0; c < run.Map[r].Count; c++) cells.Add(NodeLabel(run.Map[r][c]) + (r == run.Row && c == run.Col ? "●" : ""));
                    rows.Add("行" + r + ":" + string.Join("/", cells.ToArray()));
                }
                L.Add("マップ: " + string.Join(" ", rows.ToArray()));
                L.Add("レリック: " + (run.Relics.Count > 0 ? string.Join("、", run.Relics.ToArray()) : "（なし）"));
                L.Add("ピック履歴: " + (run.Picks.Count > 0 ? string.Join("、", run.Picks.Select(CardName).ToArray()) : "（なし）"));
                if (run.RewardOptions != null) L.Add("報酬候補（いま提示中）: " + string.Join(" / ", run.RewardOptions.Select(CardName).ToArray()));
                if (run.RelicOptions != null) L.Add("レリック候補（いま提示中）: " + string.Join(" / ", run.RelicOptions.ToArray()));
                L.Add("デッキ全体: " + Names(run.Deck));
            }
            else if (state != null)
            {
                L.Add("単発検証 / mode " + state.ReactionMode + " / seed " + state.Rng.Seed);
            }
            else L.Add("（戦闘・ランともに未開始）");
            L.Add("");
            if (playNotes.Count > 0)
            {
                L.Add("## プレイメモ（" + playNotes.Count + "件・プレイヤーがその場で残した気づき）");
                for (int i = 0; i < playNotes.Count; i++)
                {
                    var n = playNotes[i];
                    string hm = n.At.Length >= 16 ? n.At.Substring(11, 5) : n.At;
                    L.Add("- [" + hm + " " + n.Context + "] " + n.Text);
                }
                L.Add("");
            }
            var dt = DecisionTimeLines(journal);
            if (dt.Count > 0)
            {
                L.Add("## 判断時間（ジャーナルの記録時刻から。長考＝悩みの密度の代理指標）");
                L.AddRange(dt);
                L.Add("");
            }
            if (choices.Count > 0)
            {
                L.Add("## 選択履歴（" + choices.Count + "件・ピック/鍛錬/合成/購入/イベントの意思決定）");
                for (int i = 0; i < choices.Count; i++)
                {
                    var c = choices[i];
                    L.Add("- [" + c.At + "] " + c.Text + (c.Ctx != null ? "（HP " + c.Ctx.Hp + "/" + c.Ctx.MaxHp + "・デッキ" + c.Ctx.Deck + "枚・" + c.Ctx.Gold + "G）" : ""));
                }
                L.Add("");
            }
            if (history.Count > 0)
            {
                L.Add("## これまでの戦闘（" + history.Count + "戦）");
                L.Add("");
                L.Add("| # | 敵 | 結果 | ターン | HP | 強さ | 面白さ | 敗因 | メモ |");
                L.Add("|---|---|---|---|---|---|---|---|---|");
                for (int i = 0; i < history.Count; i++)
                {
                    var h = history[i];
                    var r = h.Rating;
                    string feel = r != null && r.LossFeel == "build" ? "構築の失敗" : r != null && r.LossFeel == "unfair" ? "理不尽" : "";
                    L.Add("| " + h.BattleNo + " | " + SafeEncounterName(h.EnemyId) + (h.Elite ? "（強個体）" : "") + " | " + (h.Result == "won" ? "勝利" : "敗北") + " | " + h.Turns + " | " + h.HpBefore + "→" + h.HpAfter
                        + " | " + (r != null && r.Strength.HasValue ? r.Strength.Value.ToString() : "") + " | " + (r != null && r.Fun.HasValue ? r.Fun.Value.ToString() : "") + " | " + feel + " | " + ((r != null ? r.Note ?? "" : "").Trim().Replace("|", "｜")) + " |");
                }
                L.Add("");
                for (int i = 0; i < history.Count; i++)
                {
                    var h = history[i];
                    var r = h.Rating;
                    string rating = "";
                    if (r != null)
                    {
                        rating = " / 評価:" + (r.Strength.HasValue ? " 強さ" + r.Strength.Value : "") + (r.Fun.HasValue ? " 面白さ" + r.Fun.Value : "")
                            + ((r.Note ?? "").Trim() != "" ? "「" + r.Note!.Trim() + "」" : "");
                    }
                    L.Add("### " + h.BattleNo + "戦目 " + SafeEncounterName(h.EnemyId) + (h.Elite ? "（強個体）" : "") + " — " + (h.Result == "won" ? "勝利" : "敗北") + " / " + h.Turns + "ターン / HP " + h.HpBefore + "→" + h.HpAfter + " / デッキ" + h.DeckSize + "枚" + rating);
                    L.AddRange(h.Lines);
                    L.Add("");
                }
            }
            if (s != null)
            {
                L.Add("## 盤面（ターン " + s.Turn + " / " + s.Phase + "）");
                L.AddRange(RenderBoard(s, text));
                L.Add("");
                var all = new List<string>();
                for (int i = 0; i < s.EventLog.Count; i++) { var line = ReportLine(s.EventLog[i], text); if (line != null) all.Add(line); }
                var lines = all.Count > LOG_CAP ? all.Skip(all.Count - LOG_CAP).ToList() : all;
                L.Add("## この戦闘のログ（" + lines.Count + "行" + (all.Count > lines.Count ? " / 冒頭" + (all.Count - lines.Count) + "行は省略" : "") + "）");
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
                var r2 = run.Combat != null ? run with { Combat = TrimLog(run.Combat) } : run;
                L.Add(JsonConvert.SerializeObject(new { kind = "run", run = r2, logIndex = r2.Combat != null ? r2.Combat.EventLog.Count : 0 }, JsonUnions.Settings));
            }
            else
            {
                var b = state != null ? TrimLog(state) : null;
                L.Add(JsonConvert.SerializeObject(new { kind = "battle", battle = b, logIndex = b != null ? b.EventLog.Count : 0 }, JsonUnions.Settings));
            }
            L.Add("```");
            return string.Join("\n", L.ToArray());
        }

        /// <summary>ランのセーブを直列化する (ui/report.ts buildRunSaveFile)。戦闘ログはスナップショット上限で切り詰める</summary>
        public static string BuildRunSaveFile(RunState run, IReadOnlyList<BattleArchive> history, IReadOnlyList<PlayNote> playNotes, RunJournal? journal, IReadOnlyList<RunChoice> choices)
        {
            var r = run.Combat != null ? run with { Combat = TrimLog(run.Combat) } : run;
            var sf = new RunSaveFile
            {
                Kind = "run",
                Run = r,
                LogIndex = r.Combat != null ? r.Combat.EventLog.Count : 0,
                Fingerprint = DataFingerprint(),
                History = history.ToList(),
                PlayNotes = playNotes.ToList(),
                Journal = journal,
                Choices = choices.Count > 0 ? choices.ToList() : null,
            };
            return JsonConvert.SerializeObject(sf, JsonUnions.Settings);
        }

        // ---- 選択履歴 (ui/report.ts describeRunChoice) ----

        static readonly Dictionary<string, string> NODE_LABEL = new Dictionary<string, string>
        {
            { "battle", "⚔️" }, { "elite", "👑強個体" }, { "boss", "💀幕ボス" }, { "campfire", "🔥焚き火" },
            { "workshop", "🔨工房" }, { "shop", "🛒ショップ" }, { "event", "❓" }, { "treasure", "🎁宝箱" },
        };

        /// <summary>ランコマンド → 選択履歴の1行 (純関数)。戦闘操作 (Combat) は対象外 (戦闘ログが担当)</summary>
        public static RunChoice? DescribeRunChoice(RunState prev, RunCommand cmd, RunState next)
        {
            var core = DescribeRunChoiceCore(prev, cmd, next);
            if (core == null) return null;
            core.Ctx = new RunChoiceCtx { Hp = next.Hp, MaxHp = next.MaxHp, Deck = next.Deck.Count, Gold = next.Gold };
            return core;
        }

        static string JoinNames(IEnumerable<string> ids) => string.Join("・", ids.Select(CardName).ToArray());

        static RunChoice? DescribeRunChoiceCore(RunState prev, RunCommand cmd, RunState next)
        {
            string at = "幕" + next.Act + " 行" + (next.Row + 1);
            RunChoice Mk(string text) => new RunChoice { At = at, Text = text };
            switch (cmd)
            {
                case RunCommand_ChooseNode _:
                {
                    if (next.Row < 0 || next.Row >= next.Map.Count || next.Col >= next.Map[next.Row].Count) return null;
                    var node = next.Map[next.Row][next.Col];
                    string lbl; NODE_LABEL.TryGetValue(node.Type, out lbl);
                    string label = node.EncounterId != null ? (lbl ?? "") + " " + SafeEncounterName(node.EncounterId) : (lbl ?? node.Type);
                    return Mk("進路: " + label);
                }
                case RunCommand_PickReward pr:
                {
                    var opts = prev.RewardOptions ?? new List<string>();
                    if (pr.Index < 0 || pr.Index >= opts.Count) return null;
                    var passed = opts.Where((_, i) => i != pr.Index);
                    var passedText = JoinNames(passed);
                    return Mk("報酬ピック: " + CardName(opts[pr.Index]) + " を獲得（見送り: " + (passedText != "" ? passedText : "なし") + "）");
                }
                case RunCommand_SkipReward _:
                {
                    var opts = prev.RewardOptions ?? new List<string>();
                    var t = JoinNames(opts);
                    return Mk("報酬ピック: スキップ（候補: " + (t != "" ? t : "なし") + "）");
                }
                case RunCommand_PickRelic pr:
                {
                    var opts = prev.RelicOptions ?? new List<string>();
                    if (pr.Index < 0 || pr.Index >= opts.Count) return null;
                    var passed = string.Join("・", opts.Where((_, i) => i != pr.Index).Select(SafeRelicName).ToArray());
                    return Mk("レリック: " + SafeRelicName(opts[pr.Index]) + " を獲得（見送り: " + (passed != "" ? passed : "なし") + "）");
                }
                case RunCommand_SkipRelic _:
                {
                    var opts = prev.RelicOptions ?? new List<string>();
                    var t = string.Join("・", opts.Select(SafeRelicName).ToArray());
                    return Mk("レリック: 見送り（候補: " + (t != "" ? t : "なし") + "）");
                }
                case RunCommand_CampfireRest _:
                    return Mk("焚き火: 休む（HP " + prev.Hp + "→" + next.Hp + "）");
                case RunCommand_CampfireUpgrade cu:
                {
                    if (cu.Index < 0 || cu.Index >= prev.Deck.Count || cu.Index >= next.Deck.Count) return null;
                    return Mk("焚き火: 鍛えた " + prev.Deck[cu.Index].Def.Name + " → " + next.Deck[cu.Index].Def.Name);
                }
                case RunCommand_CampfireRemove cr:
                    return cr.Index >= 0 && cr.Index < prev.Deck.Count ? Mk("焚き火: " + prev.Deck[cr.Index].Def.Name + " を取り除いた（安らぎの煙管）") : null;
                case RunCommand_CampfireDig _:
                {
                    var dug = next.Relics.Where(id => !prev.Relics.Contains(id)).Select(SafeRelicName).ToList();
                    return Mk("焚き火: 発掘 → " + (dug.Count > 0 ? string.Join("・", dug.ToArray()) : "レリックは尽きていた"));
                }
                case RunCommand_CampfireTrain _:
                {
                    int train = 0;
                    if (next.RelicState != null) next.RelicState.TryGetValue("train", out train);
                    return Mk("焚き火: 鍛錬（重石。戦闘開始時の成長 +" + train + "）");
                }
                case RunCommand_RelicChooseCards rc:
                {
                    var p = prev.PendingRelicChoice;
                    string relicName = p != null ? SafeRelicName(p.RelicId) : "レリック";
                    var names = new List<string>();
                    if (rc.Indices != null) foreach (var i in rc.Indices) if (i >= 0 && i < prev.Deck.Count) names.Add(prev.Deck[i].Def.Name);
                    if (names.Count == 0) return Mk(relicName + ": 何も選ばなかった");
                    return Mk(relicName + ": " + string.Join("・", names.ToArray()) + " を" + (p != null && p.Mode == "remove" ? "取り除いた" : "変成して鍛えた"));
                }
                case RunCommand_WorkshopFuse wf:
                {
                    if (wf.IndexA < 0 || wf.IndexA >= prev.Deck.Count || wf.IndexB < 0 || wf.IndexB >= prev.Deck.Count) return null;
                    var a = prev.Deck[wf.IndexA]; var b = prev.Deck[wf.IndexB];
                    var prevUids = new HashSet<string>(prev.Deck.Select(c => c.Uid));
                    var made = next.Deck.FirstOrDefault(c => !prevUids.Contains(c.Uid));
                    if (made == null) return null;
                    return Mk("工房: " + a.Def.Name + " × " + b.Def.Name + " → " + made.Def.Name + "（" + (made.Def.XCost == true ? "X" : made.Def.Cost.ToString()) + "E）");
                }
                case RunCommand_WorkshopSkip _:
                    return Mk("工房: 見送り");
                case RunCommand_ShopBuyCard sb:
                {
                    if (prev.Shop == null || sb.Index < 0 || sb.Index >= prev.Shop.Cards.Count) return null;
                    var item = prev.Shop.Cards[sb.Index];
                    return Mk("ショップ: " + CardName(item.Id) + " を" + item.Price + "Gで購入");
                }
                case RunCommand_ShopBuyRelic _:
                {
                    if (prev.Shop == null || prev.Shop.RelicId == null) return null;
                    return Mk("ショップ: レリック " + SafeRelicName(prev.Shop.RelicId) + " を" + prev.Shop.RelicPrice + "Gで購入");
                }
                case RunCommand_ShopRemove sr:
                    return sr.Index >= 0 && sr.Index < prev.Deck.Count ? Mk("ショップ: " + prev.Deck[sr.Index].Def.Name + " を除去（" + (prev.Gold - next.Gold) + "G）") : null;
                case RunCommand_ShopUpgrade su:
                {
                    if (su.Index < 0 || su.Index >= prev.Deck.Count || su.Index >= next.Deck.Count) return null;
                    return Mk("ショップ: 鍛えた " + prev.Deck[su.Index].Def.Name + " → " + next.Deck[su.Index].Def.Name + "（" + (prev.Gold - next.Gold) + "G）");
                }
                case RunCommand_EventChoice ec:
                {
                    if (prev.EventId == null) return null;
                    string evName = prev.EventId;
                    string label = "選択肢" + ec.Index;
                    try
                    {
                        var ev = Content.GetEventDef(prev.EventId);
                        evName = ev.Name;
                        if (ec.Index >= 0 && ec.Index < ev.Choices.Count) label = ev.Choices[ec.Index].Label;
                    }
                    catch (Exception) { /* 未知イベントは生ID */ }
                    CardInstance target = ec.CardIndex.HasValue && ec.CardIndex.Value >= 0 && ec.CardIndex.Value < prev.Deck.Count ? prev.Deck[ec.CardIndex.Value] : null;
                    int hpDiff = next.Hp - prev.Hp;
                    int goldDiff = next.Gold - prev.Gold;
                    var gotRelics = next.Relics.Where(id => !prev.Relics.Contains(id)).Select(id =>
                    {
                        try { var rd = Content.GetRelicDef(id); var tag = RelicRarityTag(rd); return (tag != "" ? tag + " " : "") + rd.Name; }
                        catch (Exception) { return id; }
                    }).ToList();
                    var prevUidSet = new HashSet<string>(prev.Deck.Select(c => c.Uid));
                    var gotCards = next.Deck.Where(c => !prevUidSet.Contains(c.Uid)).Select(c => c.Def.Name).ToList();
                    int prevWard = 0, nextWard = 0;
                    if (prev.RelicState != null) prev.RelicState.TryGetValue("brandWard", out prevWard);
                    if (next.RelicState != null) next.RelicState.TryGetValue("brandWard", out nextWard);
                    int warded = prevWard - nextWard;
                    var outcome = new[]
                    {
                        gotRelics.Count > 0 ? "獲得レリック: " + string.Join("・", gotRelics.ToArray()) : "",
                        gotCards.Count > 0 ? "獲得: " + string.Join("・", gotCards.ToArray()) : "",
                        hpDiff != 0 ? "HP" + (hpDiff > 0 ? "+" : "") + hpDiff : "",
                        goldDiff != 0 ? (goldDiff > 0 ? "+" : "") + goldDiff + "G" : "",
                        warded > 0 ? "🏷️厄除けの札が烙印" + warded + "枚を防いだ（残り" + nextWard + "）" : "",
                    }.Where(x => x != "").ToArray();
                    return Mk("イベント[" + evName + "]: " + label + (target != null ? "（対象: " + target.Def.Name + "）" : "") + (outcome.Length > 0 ? "（" + string.Join("・", outcome) + "）" : ""));
                }
                default:
                    return null;
            }
        }
    }
}
