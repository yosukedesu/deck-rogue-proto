// Analysis.cs — 戦闘ログとラン履歴の計測 (純関数)。src/engine/analysis.ts の移植 (2026-09-14 Unity/APK にもプロトと同じレポート書き出しを載せるため)。
// プレイレポートの「計測（機械可読）」ブロック (schema deck-rogue-metrics/1) を TS と同じ形で作る = `npm run analyze -- <md>` がそのまま読める。
// 移植の作法は unity/PORTING.md。ここは表示に触れない (文字列は Report.cs)。
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;

#nullable enable

namespace DeckRogue.Engine
{
    /// <summary>1ターンの計測 (analysis.ts TurnMetrics)</summary>
    public sealed class TurnMetrics
    {
        [JsonProperty("turn")] public int Turn;
        /// <summary>自ターン中にプレイヤーが与えたダメージ (HP損失ベース)</summary>
        [JsonProperty("dealt")] public int Dealt;
        /// <summary>敵フェーズ中に返し (リアクション・置物) で与えたダメージ</summary>
        [JsonProperty("counter")] public int Counter;
        /// <summary>プレイヤーが受けたHP損失 (敵フェーズ)</summary>
        [JsonProperty("taken")] public int Taken;
        [JsonProperty("plays")] public int Plays;
        [JsonProperty("sets")] public int Sets;
        [JsonProperty("fires")] public int Fires;
        [JsonProperty("holds")] public int Holds;
        /// <summary>罠モデル (2026-09-13): 2窓で鳴らずに期限切れの札の枚数</summary>
        [JsonProperty("expires")] public int Expires;
        /// <summary>ターン開始時の手札 (保持で残った札+ドロー)</summary>
        [JsonProperty("hand", NullValueHandling = NullValueHandling.Ignore)] public List<string>? Hand;
        /// <summary>ターン終了時に手札に残った札 = 使わなかった札</summary>
        [JsonProperty("unplayed", NullValueHandling = NullValueHandling.Ignore)] public List<string>? Unplayed;
    }

    /// <summary>1戦闘の計測 (analysis.ts BattleMetrics)</summary>
    public sealed class BattleMetrics
    {
        [JsonProperty("turns")] public int Turns;
        /// <summary>1ターン目に出したダメージ (HP損失ベース) = 曲線の物差し「初手火力」</summary>
        [JsonProperty("t1Damage")] public int T1Damage;
        [JsonProperty("totalDealt")] public int TotalDealt;
        [JsonProperty("totalTaken")] public int TotalTaken;
        [JsonProperty("maxTurnDamage")] public int MaxTurnDamage;
        [JsonProperty("sets")] public int Sets;
        [JsonProperty("fires")] public int Fires;
        [JsonProperty("holds")] public int Holds;
        [JsonProperty("expires")] public int Expires;
        [JsonProperty("perTurn")] public List<TurnMetrics> PerTurn = new List<TurnMetrics>();
    }

    /// <summary>戦闘直後の5段階評価 (ui/report.ts BattleRating)。全欄任意</summary>
    public sealed class BattleRating
    {
        /// <summary>敵の強さ 1〜5</summary>
        [JsonProperty("strength", NullValueHandling = NullValueHandling.Ignore)] public int? Strength;
        /// <summary>面白さ 1〜5</summary>
        [JsonProperty("fun", NullValueHandling = NullValueHandling.Ignore)] public int? Fun;
        /// <summary>ひとことメモ (自由記述)</summary>
        [JsonProperty("note", NullValueHandling = NullValueHandling.Ignore)] public string? Note;
        /// <summary>敗北時の感触: "build"=構築の失敗 / "unfair"=理不尽 (作り直し基準の入力)</summary>
        [JsonProperty("lossFeel", NullValueHandling = NullValueHandling.Ignore)] public string? LossFeel;

        public BattleRating Clone() => new BattleRating { Strength = Strength, Fun = Fun, Note = Note, LossFeel = LossFeel };
    }

    /// <summary>計測の1戦闘分の行 (analysis.ts BattleRow)</summary>
    public sealed class BattleRow
    {
        [JsonProperty("battleNo")] public int BattleNo;
        [JsonProperty("act")] public int Act;
        [JsonProperty("enemyId")] public string EnemyId = "";
        [JsonProperty("elite")] public bool Elite;
        [JsonProperty("boss")] public bool Boss;
        /// <summary>"won" | "lost"</summary>
        [JsonProperty("result")] public string Result = "won";
        [JsonProperty("hpBefore")] public int HpBefore;
        [JsonProperty("hpAfter")] public int HpAfter;
        /// <summary>旧アーカイブは null (TS も null を書く)</summary>
        [JsonProperty("metrics")] public BattleMetrics? Metrics;
        [JsonProperty("rating", NullValueHandling = NullValueHandling.Ignore)] public BattleRating? Rating;
    }

    public sealed class T1DamageStat
    {
        [JsonProperty("min")] public int Min;
        [JsonProperty("median")] public int Median;
        [JsonProperty("max")] public int Max;
    }

    /// <summary>幕別サマリー (analysis.ts ActSummary)</summary>
    public sealed class ActSummary
    {
        [JsonProperty("act")] public int Act;
        [JsonProperty("battles")] public int Battles;
        [JsonProperty("normalTurnsAvg")] public double? NormalTurnsAvg;
        [JsonProperty("bossTurns")] public int? BossTurns;
        [JsonProperty("eliteTurns")] public List<int> EliteTurns = new List<int>();
        [JsonProperty("t1Damage")] public T1DamageStat? T1Damage;
        [JsonProperty("hpLost")] public int HpLost;
        [JsonProperty("sets")] public int Sets;
        [JsonProperty("fires")] public int Fires;
        [JsonProperty("funAvg")] public double? FunAvg;
        [JsonProperty("strengthAvg")] public double? StrengthAvg;
        /// <summary>2ターン目以降に敵からHP損失があった、または伏せの発動/温存があった戦闘の割合</summary>
        [JsonProperty("lateActionRate")] public double? LateActionRate;
    }

    public sealed class MetricsRunSummary
    {
        [JsonProperty("act")] public int Act;
        [JsonProperty("battlesWon")] public int BattlesWon;
        [JsonProperty("hp")] public int Hp;
        [JsonProperty("deckSize")] public int DeckSize;
    }

    /// <summary>レポートの「計測（機械可読）」ブロックの形 (ui/report.ts metricsExport と同じ契約)</summary>
    public sealed class MetricsExport
    {
        [JsonProperty("schema")] public string Schema = "deck-rogue-metrics/1";
        [JsonProperty("fingerprint")] public string Fingerprint = "";
        [JsonProperty("run")] public MetricsRunSummary? Run;
        [JsonProperty("battles")] public List<BattleRow> Battles = new List<BattleRow>();
        [JsonProperty("acts")] public List<ActSummary> Acts = new List<ActSummary>();
    }

    public static class Analysis
    {
        /// <summary>物差し (2026-09-03 曲線パッケージの合格基準): 良いデッキで通常戦4〜6T・ボス6〜10T</summary>
        public static readonly int[] TURN_TARGET_NORMAL = { 4, 6 };
        public static readonly int[] TURN_TARGET_BOSS = { 6, 10 };

        /// <summary>戦闘ログ → 計測 (analysis.ts battleMetrics)</summary>
        public static BattleMetrics ComputeBattleMetrics(IReadOnlyList<GameEvent> log)
        {
            var turns = new SortedDictionary<int, TurnMetrics>();
            int cur = 0;
            bool enemyPhase = false;
            bool awaitingDraw = false;
            TurnMetrics At(int t)
            {
                TurnMetrics m;
                if (!turns.TryGetValue(t, out m)) { m = new TurnMetrics { Turn = t }; turns[t] = m; }
                return m;
            }
            for (int i = 0; i < log.Count; i++)
            {
                var e = log[i];
                switch (e)
                {
                    case GameEvent_TurnStarted ts:
                    {
                        cur = ts.Turn; enemyPhase = false;
                        var m = At(cur);
                        if (ts.Hand != null) { m.Hand = new List<string>(ts.Hand); awaitingDraw = true; }
                        break;
                    }
                    case GameEvent_CardsDrawn cd:
                        // ターン開始直後の最初のドローが手札の残り (保持) と合わせて「ターン開始時の手札」になる
                        if (awaitingDraw && cd.Cards != null)
                        {
                            var m = At(cur);
                            var h = m.Hand ?? new List<string>();
                            h.AddRange(cd.Cards);
                            m.Hand = h;
                            awaitingDraw = false;
                        }
                        break;
                    case GameEvent_TurnEnded te:
                        enemyPhase = true;
                        awaitingDraw = false;
                        if (te.Unplayed != null) At(cur).Unplayed = new List<string>(te.Unplayed);
                        break;
                    case GameEvent_DamageDealt dd:
                        if (dd.Source == "player")
                        {
                            if (enemyPhase) At(cur).Counter += dd.HpLoss;
                            else At(cur).Dealt += dd.HpLoss;
                        }
                        else At(cur).Taken += dd.HpLoss;
                        break;
                    case GameEvent_CardPlayed _: At(cur).Plays++; break;
                    case GameEvent_CardSet _: At(cur).Sets++; break;
                    case GameEvent_ReactionTriggered _: At(cur).Fires++; break;
                    case GameEvent_ReactionHeld _: At(cur).Holds++; break;
                    case GameEvent_SetCardExpired _: At(cur).Expires++; break;
                    default: break;
                }
            }
            var perTurn = turns.Values.ToList();
            int Sum(Func<TurnMetrics, int> k) { int s = 0; for (int i = 0; i < perTurn.Count; i++) s += k(perTurn[i]); return s; }
            int maxTurn = 0;
            for (int i = 0; i < perTurn.Count; i++) maxTurn = Math.Max(maxTurn, perTurn[i].Dealt);
            return new BattleMetrics
            {
                Turns = perTurn.Count,
                T1Damage = perTurn.Count > 0 ? perTurn[0].Dealt : 0,
                TotalDealt = Sum(m => m.Dealt) + Sum(m => m.Counter),
                TotalTaken = Sum(m => m.Taken),
                MaxTurnDamage = maxTurn,
                Sets = Sum(m => m.Sets),
                Fires = Sum(m => m.Fires),
                Holds = Sum(m => m.Holds),
                Expires = Sum(m => m.Expires),
                PerTurn = perTurn,
            };
        }

        static int Median(List<int> xs)
        {
            var s = new List<int>(xs); s.Sort();
            return s.Count == 0 ? 0 : s[s.Count >> 1];
        }

        static double? Avg(List<double> xs)
        {
            if (xs.Count == 0) return null;
            double sum = 0; for (int i = 0; i < xs.Count; i++) sum += xs[i];
            return Math.Round(sum / xs.Count * 100.0) / 100.0;
        }

        /// <summary>幕別サマリー (analysis.ts actSummaries)</summary>
        public static List<ActSummary> ActSummaries(IReadOnlyList<BattleRow> rows)
        {
            var acts = rows.Select(r => r.Act).Distinct().OrderBy(a => a).ToList();
            var outp = new List<ActSummary>();
            foreach (var act in acts)
            {
                var rs = rows.Where(r => r.Act == act).ToList();
                var normals = rs.Where(r => !r.Boss && !r.Elite && r.Metrics != null).ToList();
                var t1 = rs.Where(r => r.Metrics != null).Select(r => r.Metrics!.T1Damage).ToList();
                var late = rs.Where(r => r.Metrics != null).ToList();
                int lateHit = late.Count(r => r.Metrics!.PerTurn.Skip(1).Any(m => m.Taken > 0 || m.Fires > 0 || m.Holds > 0));
                var boss = rs.FirstOrDefault(r => r.Boss && r.Metrics != null);
                outp.Add(new ActSummary
                {
                    Act = act,
                    Battles = rs.Count,
                    NormalTurnsAvg = Avg(normals.Select(r => (double)r.Metrics!.Turns).ToList()),
                    BossTurns = boss != null ? boss.Metrics!.Turns : (int?)null,
                    EliteTurns = rs.Where(r => r.Elite && r.Metrics != null).Select(r => r.Metrics!.Turns).ToList(),
                    T1Damage = t1.Count > 0 ? new T1DamageStat { Min = t1.Min(), Median = Median(t1), Max = t1.Max() } : null,
                    HpLost = rs.Sum(r => Math.Max(0, r.HpBefore - r.HpAfter)),
                    Sets = rs.Sum(r => r.Metrics != null ? r.Metrics.Sets : 0),
                    Fires = rs.Sum(r => r.Metrics != null ? r.Metrics.Fires : 0),
                    FunAvg = Avg(rs.Where(r => r.Rating != null && r.Rating.Fun.HasValue).Select(r => (double)r.Rating!.Fun!.Value).ToList()),
                    StrengthAvg = Avg(rs.Where(r => r.Rating != null && r.Rating.Strength.HasValue).Select(r => (double)r.Rating!.Strength!.Value).ToList()),
                    LateActionRate = late.Count > 0 ? Math.Round((double)lateHit / late.Count * 100.0) / 100.0 : (double?)null,
                });
            }
            return outp;
        }
    }
}
