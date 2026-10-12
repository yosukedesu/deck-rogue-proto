// Golden.cs — ゴールデンマスターの要約 (Unity移植の等価性検証。docs/unity-port.md §1)。src/engine/golden.ts の移植。
// ラン状態を「移植側が同じ手順で再計算できる小さな要約」に潰し、FNV-1a 32bit でハッシュする。
// TS 側は runDigest オブジェクトを JSON.stringify した文字列をハッシュするので、C# 側は
// **同じキー順・同じ値の JSON 文字列を手組みする** (JSON.stringify と同じエスケープ・数値は整数のみ)。

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public static class Golden
    {
        /// <summary>
        /// 戦闘状態の要約 (JSON 文字列)。数値と短い文字列だけ (浮動小数は含めない = 言語間の表現差を避ける)。
        /// キー順は golden.ts の CombatDigest の並びそのまま。
        /// </summary>
        public static string CombatDigest(GameState s)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            Int(sb, "turn", s.Turn, true);
            Str(sb, "phase", s.Phase, false);
            Int(sb, "hp", s.Player.Hp, false);
            Int(sb, "block", s.Player.Block, false);
            Int(sb, "iceBlock", s.Player.IceBlock, false);
            Int(sb, "energy", s.Player.Energy, false);
            Int(sb, "energyMax", s.Player.EnergyMax, false);
            Int(sb, "growth", s.Player.Growth, false);
            Int(sb, "momentum", s.Player.Momentum, false);
            StrArrayOfCards(sb, "hand", s.Player.Hand);
            Int(sb, "draw", s.Player.DrawPile.Count, false);
            Int(sb, "discard", s.Player.DiscardPile.Count, false);
            Int(sb, "exhaust", s.Player.ExhaustPile.Count, false);
            StrArrayOfCards(sb, "set", s.Player.SetCards);
            StrArrayOfCards(sb, "permanents", s.Player.Permanents);

            sb.Append(",\"enemies\":[");
            for (int i = 0; i < s.Enemies.Count; i++)
            {
                var e = s.Enemies[i];
                if (i > 0) sb.Append(',');
                sb.Append('{');
                Str(sb, "id", e.EnemyId, true);
                Int(sb, "hp", e.Hp, false);
                Int(sb, "block", e.Block, false);
                Int(sb, "strength", e.Strength, false);
                Int(sb, "burn", e.Burn, false);
                sb.Append('}');
            }
            sb.Append(']');

            Long(sb, "rngSeed", s.Rng.Seed, false);
            Long(sb, "rngCounter", s.Rng.Counter, false);
            Int(sb, "events", s.EventLog.Count, false);

            // イベント型の列 (直近50件) — どの手で分岐したかを特定する手がかり
            sb.Append(",\"eventTail\":[");
            int start = s.EventLog.Count > 50 ? s.EventLog.Count - 50 : 0;
            for (int i = start; i < s.EventLog.Count; i++)
            {
                if (i > start) sb.Append(',');
                Quote(sb, s.EventLog[i].Type);
            }
            sb.Append(']');

            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>ラン状態の要約 (JSON 文字列)。キー順は golden.ts の RunDigest の並びそのまま。</summary>
        public static string RunDigest(RunState run)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            Int(sb, "act", run.Act, true);
            Int(sb, "row", run.Row, false);
            Str(sb, "phase", run.Phase, false);
            Int(sb, "hp", run.Hp, false);
            Int(sb, "maxHp", run.MaxHp, false);
            Int(sb, "gold", run.Gold, false);
            StrArrayOfCards(sb, "deck", run.Deck);

            sb.Append(",\"relics\":[");
            for (int i = 0; i < run.Relics.Count; i++)
            {
                if (i > 0) sb.Append(',');
                Quote(sb, run.Relics[i]);
            }
            sb.Append(']');

            Long(sb, "rngSeed", run.Rng.Seed, false);
            Long(sb, "rngCounter", run.Rng.Counter, false);
            sb.Append(",\"combat\":");
            sb.Append(run.Combat != null ? CombatDigest(run.Combat) : "null");
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>FNV-1a 32bit (UTF-8 バイト列)。TS 側と同じ定数・同じバイト列で一致する</summary>
        public static uint Fnv1a32(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            uint h = 0x811c9dc5;
            for (int i = 0; i < bytes.Length; i++)
            {
                h ^= bytes[i];
                h *= 0x01000193; // unchecked = JS の Math.imul(h, prime) >>> 0 と同じラップ
            }
            return h;
        }

        /// <summary>ラン状態のハッシュ (16進8桁ゼロ埋め)</summary>
        public static string RunHash(RunState run)
        {
            return Fnv1a32(RunDigest(run)).ToString("x8", CultureInfo.InvariantCulture);
        }

        // ---- JSON.stringify と同じ書き方をするための小道具 ----

        private static void Int(StringBuilder sb, string key, int value, bool first)
        {
            if (!first) sb.Append(',');
            Quote(sb, key);
            sb.Append(':');
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void Long(StringBuilder sb, string key, long value, bool first)
        {
            if (!first) sb.Append(',');
            Quote(sb, key);
            sb.Append(':');
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void Str(StringBuilder sb, string key, string value, bool first)
        {
            if (!first) sb.Append(',');
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
                if (i > 0) sb.Append(',');
                Quote(sb, cards[i].Def.Id);
            }
            sb.Append(']');
        }

        /// <summary>JSON.stringify の QuoteJSONString と同じエスケープ (制御文字は \u00xx、非ASCIIは素通し)</summary>
        private static void Quote(StringBuilder sb, string s)
        {
            sb.Append('"');
            if (s != null)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            else sb.Append(c);
                            break;
                    }
                }
            }
            sb.Append('"');
        }
    }
}
