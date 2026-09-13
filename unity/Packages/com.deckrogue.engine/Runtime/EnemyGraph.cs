// EnemyGraph.cs — 敵の行動グラフ (2026-09-14 本家式の状態機械)。src/engine/enemyGraph.ts の翻訳。
// 節 (技/乱択/条件) と割り込みの評価・カーソルの前進。旧形からの変換 (graphFromLegacy) は TS 側の移行スクリプト
// 専用なので移植しない (data は新形で同梱される)。
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public static class EnemyGraph
    {
        public static EnemyMove MoveById(EnemyDef def, string moveId)
        {
            for (int i = 0; i < def.Moves.Count; i++) if (def.Moves[i].Id == moveId) return def.Moves[i];
            throw new InvalidOperationException($"敵 {def.Id} に技 {moveId} が無い");
        }

        /// <summary>節が技の節ならその技の id</summary>
        public static string NodeMoveId(EnemyDef def, string nodeId)
        {
            return def.Nodes.TryGetValue(nodeId, out var n) ? n.Move : null;
        }

        /// <summary>個体の開始節: 編成の上書き &gt; スロット別 &gt; start</summary>
        public static string StartNodeFor(EnemyDef def, int slot, string overrideStart = null)
        {
            if (overrideStart != null) return overrideStart;
            if (def.StartBySlot != null && slot >= 0 && slot < def.StartBySlot.Count) return def.StartBySlot[slot];
            return def.Start;
        }

        private static int OthersAlive(GameState state, int enemyIndex)
        {
            int n = 0;
            for (int j = 0; j < state.Enemies.Count; j++) if (j != enemyIndex && state.Enemies[j].Hp > 0) n++;
            return n;
        }

        /// <summary>条件の評価 (条件の節・割り込みが共用)。書かれた項目を全部満たす時に真</summary>
        public static bool EvalCondition(GameState state, int enemyIndex, EnemyCondition cond)
        {
            var e = state.Enemies[enemyIndex];
            int othersAlive = OthersAlive(state, enemyIndex);
            if (cond.HpBelowHalf == true && !(e.Hp <= e.MaxHp * 0.5)) return false;
            if (cond.Alone == true && othersAlive > 0) return false;
            if (cond.AllyAlive == true && othersAlive == 0) return false;
            if (cond.UsesAtLeast != null)
            {
                int uses = 0;
                if (e.MoveUses != null) e.MoveUses.TryGetValue(cond.UsesAtLeast.Move, out uses);
                if (uses < cond.UsesAtLeast.Count) return false;
            }
            if (cond.DamageTakenAtLeast != null && (e.DamageTakenTotal ?? 0) < cond.DamageTakenAtLeast.Value) return false;
            if (cond.TurnParity != null && (state.Turn % 2 == 1 ? "odd" : "even") != cond.TurnParity) return false;
            if (cond.AlliesFewerThan != null && othersAlive + 1 >= cond.AlliesFewerThan.Value) return false;
            return true;
        }

        /// <summary>割り込みの条件が立っているか (from の節にいるか・発火済みかは呼び出し側)</summary>
        public static bool InterruptHolds(GameState state, int enemyIndex, EnemyInterrupt it)
        {
            var e = state.Enemies[enemyIndex];
            switch (it.On)
            {
                case EnemyInterruptTriggers.HpBelowHalf:
                    return e.Hp > 0 && e.Hp <= e.MaxHp * 0.5;
                case EnemyInterruptTriggers.DamageTaken:
                    return (e.DamageTakenTotal ?? 0) >= (it.Amount ?? 0);
                case EnemyInterruptTriggers.AllyDied:
                    for (int j = 0; j < state.Enemies.Count; j++)
                        if (j != enemyIndex && state.Enemies[j].Hp <= 0 && state.Enemies[j].Fled != true) return true;
                    return false;
                case EnemyInterruptTriggers.Alone:
                    return OthersAlive(state, enemyIndex) == 0;
                default:
                    return false;
            }
        }

        public readonly struct InterruptResult
        {
            public readonly string Cursor;
            public readonly IReadOnlyList<int> Fired;
            public readonly IReadOnlyList<int> FiredNow;
            public InterruptResult(string cursor, IReadOnlyList<int> fired, IReadOnlyList<int> firedNow) { Cursor = cursor; Fired = fired; FiredNow = firedNow; }
        }

        /// <summary>
        /// 割り込みを上から順に判定してカーソルを飛ばす (1戦闘1回ずつ)。only で引き金の種別を絞れる
        /// (被弾の瞬間は damageTaken だけ、宣言時は全部)
        /// </summary>
        public static InterruptResult ApplyInterruptsTo(GameState state, int enemyIndex, string cursor, IReadOnlyList<int> fired, IReadOnlyList<string> only = null)
        {
            var def = Content.GetEnemyDef(state.Enemies[enemyIndex].EnemyId);
            string cur = cursor;
            var f = fired != null ? new List<int>(fired) : new List<int>();
            var now = new List<int>();
            var its = def.Interrupts;
            if (its != null)
            {
                for (int k = 0; k < its.Count; k++)
                {
                    var it = its[k];
                    if (f.Contains(k)) continue;
                    if (only != null && !only.Contains(it.On)) continue;
                    if (it.From != null && !it.From.Contains(cur)) continue;
                    if (!InterruptHolds(state, enemyIndex, it)) continue;
                    cur = it.Goto;
                    f.Add(k);
                    now.Add(k);
                }
            }
            return new InterruptResult(cur, f, now);
        }

        /// <summary>乱択の腕が今引けるか (noRepeat / once / maxRepeat)</summary>
        private static bool ArmUsable(EnemyDef def, EnemyRandomArm arm, EnemyState enemy)
        {
            var target = NodeMoveId(def, arm.To);
            if (target == null) return true;
            if (arm.Once == true && enemy.UsedOnce != null && enemy.UsedOnce.Contains(target)) return false;
            var last = enemy.LastMoves;
            bool Streak(int n)
            {
                if (last == null || last.Count < n) return false;
                for (int i = 0; i < n; i++) if (last[i] != target) return false;
                return true;
            }
            if (arm.NoRepeat == true && Streak(1)) return false;
            if (arm.MaxRepeat != null && Streak(arm.MaxRepeat.Value)) return false;
            return true;
        }

        public readonly struct WalkResult
        {
            public readonly string NodeId;
            public readonly EnemyMove Move;
            public readonly RngState Rng;
            public readonly IReadOnlyList<string> OnceMoveIds;
            public WalkResult(string nodeId, EnemyMove move, RngState rng, IReadOnlyList<string> onceMoveIds) { NodeId = nodeId; Move = move; Rng = rng; OnceMoveIds = onceMoveIds; }
        }

        /// <summary>
        /// カーソルから技の節まで辿る。乱択は重み抽選 (RNG 1回。引ける腕が無ければ全腕から引く)、条件はその場で評価。
        /// 32 回辿っても着地しなければ定義の誤り
        /// </summary>
        public static WalkResult WalkToMove(GameState state, int enemyIndex, string cursor, RngState rng)
        {
            var def = Content.GetEnemyDef(state.Enemies[enemyIndex].EnemyId);
            var enemy = state.Enemies[enemyIndex];
            string cur = cursor;
            var r = rng;
            var onceMoveIds = new List<string>();
            for (int guard = 0; guard < 32; guard++)
            {
                if (!def.Nodes.TryGetValue(cur, out var node)) throw new InvalidOperationException($"敵 {def.Id} の行動グラフに節 {cur} が無い");
                if (node.Move != null) return new WalkResult(cur, MoveById(def, node.Move), r, onceMoveIds);
                if (node.Random != null)
                {
                    var usable = new List<EnemyRandomArm>();
                    for (int k = 0; k < node.Random.Count; k++) if (ArmUsable(def, node.Random[k], enemy)) usable.Add(node.Random[k]);
                    IReadOnlyList<EnemyRandomArm> arms = usable.Count > 0 ? usable : node.Random;
                    var weights = new List<double>(arms.Count);
                    for (int k = 0; k < arms.Count; k++) weights.Add(arms[k].Weight);
                    var (idx, r2) = Rng.WeightedIndex(r, weights);
                    r = r2;
                    var arm = arms[idx];
                    if (arm.Once == true)
                    {
                        var target = NodeMoveId(def, arm.To);
                        if (target != null) onceMoveIds.Add(target);
                    }
                    cur = arm.To;
                    continue;
                }
                if (node.If != null)
                {
                    bool ok = EvalCondition(state, enemyIndex, node.If);
                    var to = ok ? node.Then : node.Else;
                    if (to == null) throw new InvalidOperationException($"敵 {def.Id} の条件の節 {cur} に then/else が無い");
                    cur = to;
                    continue;
                }
                throw new InvalidOperationException($"敵 {def.Id} の節 {cur} は技/乱択/条件のどれでもない");
            }
            throw new InvalidOperationException($"敵 {def.Id} の行動グラフが {cursor} から技に着地しない (循環)");
        }

        /// <summary>決定的に辿れる範囲で、カーソルから見て d 手目の技 (乱択に当たったら null で打ち切り)</summary>
        public static List<string> PeekMoves(EnemyDef def, GameState state, int enemyIndex, string cursor, int depth)
        {
            var outp = new List<string>();
            string cur = cursor;
            for (int d = 0; d < depth; d++)
            {
                int guard = 0;
                string landed = null;
                bool stop = false;
                while (guard++ < 32)
                {
                    if (!def.Nodes.TryGetValue(cur, out var node)) { stop = true; break; }
                    if (node.Move != null) { landed = node.Move; cur = node.Next ?? cur; break; }
                    if (node.Random != null) { stop = true; break; }
                    if (node.If != null) { cur = (EvalCondition(state, enemyIndex, node.If) ? node.Then : node.Else) ?? cur; continue; }
                    stop = true;
                    break;
                }
                if (stop || landed == null) { outp.Add(null); return outp; }
                outp.Add(landed);
            }
            return outp;
        }

        /// <summary>節から決定的に辿って最初に着地する技 (乱択なら先頭の腕)。予告向け (TS firstMoveOf)</summary>
        public static EnemyMove FirstMoveOf(EnemyDef def, string nodeId)
        {
            string cur = nodeId;
            for (int i = 0; i < 32; i++)
            {
                if (!def.Nodes.TryGetValue(cur, out var node)) return null;
                if (node.Move != null) { for (int k = 0; k < def.Moves.Count; k++) if (def.Moves[k].Id == node.Move) return def.Moves[k]; return null; }
                if (node.Random != null) return node.Random.Count > 0 ? FirstMoveOf(def, node.Random[0].To) : null;
                if (node.If != null) { cur = node.Then ?? cur; continue; }
                return null;
            }
            return null;
        }

        /// <summary>眠り (被弾で目覚める割り込み) が生きているか: カーソルが from にいて未発火</summary>
        public static EnemyInterrupt SleepingInterrupt(EnemyDef def, EnemyState e)
        {
            if (def.Interrupts == null) return null;
            for (int k = 0; k < def.Interrupts.Count; k++)
            {
                var it = def.Interrupts[k];
                if (it.On != EnemyInterruptTriggers.DamageTaken) continue;
                if (e.FiredInterrupts != null && e.FiredInterrupts.Contains(k)) continue;
                if (it.From != null && !it.From.Contains(e.Node)) continue;
                return it;
            }
            return null;
        }
    }
}
