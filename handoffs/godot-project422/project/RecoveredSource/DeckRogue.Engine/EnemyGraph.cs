using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class EnemyGraph
{
	public readonly struct InterruptResult(string cursor, IReadOnlyList<int> fired, IReadOnlyList<int> firedNow)
	{
		public readonly string Cursor = cursor;

		public readonly IReadOnlyList<int> Fired = fired;

		public readonly IReadOnlyList<int> FiredNow = firedNow;
	}

	public readonly struct WalkResult(string nodeId, EnemyMove move, RngState rng, IReadOnlyList<string> onceMoveIds)
	{
		public readonly string NodeId = nodeId;

		public readonly EnemyMove Move = move;

		public readonly RngState Rng = rng;

		public readonly IReadOnlyList<string> OnceMoveIds = onceMoveIds;
	}

	public static EnemyMove MoveById(EnemyDef def, string moveId)
	{
		for (int i = 0; i < def.Moves.Count; i++)
		{
			if (def.Moves[i].Id == moveId)
			{
				return def.Moves[i];
			}
		}
		throw new InvalidOperationException($"敵 {def.Id} に技 {moveId} が無い");
	}

	public static string NodeMoveId(EnemyDef def, string nodeId)
	{
		if (!def.Nodes.TryGetValue(nodeId, out EnemyNode n))
		{
			return null;
		}
		return n.Move;
	}

	public static string StartNodeFor(EnemyDef def, int slot, string overrideStart = null)
	{
		if (overrideStart != null)
		{
			return overrideStart;
		}
		if (def.StartBySlot != null && slot >= 0 && slot < def.StartBySlot.Count)
		{
			return def.StartBySlot[slot];
		}
		return def.Start;
	}

	private static int OthersAlive(GameState state, int enemyIndex)
	{
		int n = 0;
		for (int j = 0; j < state.Enemies.Count; j++)
		{
			if (j != enemyIndex && state.Enemies[j].Hp > 0)
			{
				n++;
			}
		}
		return n;
	}

	public static bool EvalCondition(GameState state, int enemyIndex, EnemyCondition cond)
	{
		EnemyState e = state.Enemies[enemyIndex];
		int othersAlive = OthersAlive(state, enemyIndex);
		if (cond.HpBelowHalf == true && !((double)e.Hp <= (double)e.MaxHp * 0.5))
		{
			return false;
		}
		if (cond.Alone == true && othersAlive > 0)
		{
			return false;
		}
		if (cond.AllyAlive == true && othersAlive == 0)
		{
			return false;
		}
		if (cond.UsesAtLeast != null)
		{
			int uses = 0;
			if (e.MoveUses != null)
			{
				e.MoveUses.TryGetValue(cond.UsesAtLeast.Move, out uses);
			}
			if (uses < cond.UsesAtLeast.Count)
			{
				return false;
			}
		}
		if (cond.DamageTakenAtLeast.HasValue && e.DamageTakenTotal.GetValueOrDefault() < cond.DamageTakenAtLeast.Value)
		{
			return false;
		}
		if (cond.TurnParity != null && ((state.Turn % 2 == 1) ? "odd" : "even") != cond.TurnParity)
		{
			return false;
		}
		if (cond.AlliesFewerThan.HasValue && othersAlive + 1 >= cond.AlliesFewerThan.Value)
		{
			return false;
		}
		return true;
	}

	public static bool InterruptHolds(GameState state, int enemyIndex, EnemyInterrupt it)
	{
		EnemyState e = state.Enemies[enemyIndex];
		switch (it.On)
		{
		case "hpBelowHalf":
			if (e.Hp > 0)
			{
				return (double)e.Hp <= (double)e.MaxHp * 0.5;
			}
			return false;
		case "damageTaken":
			return e.DamageTakenTotal.GetValueOrDefault() >= it.Amount.GetValueOrDefault();
		case "allyDied":
		{
			for (int j = 0; j < state.Enemies.Count; j++)
			{
				if (j != enemyIndex && state.Enemies[j].Hp <= 0 && state.Enemies[j].Fled != true)
				{
					return true;
				}
			}
			return false;
		}
		case "alone":
			return OthersAlive(state, enemyIndex) == 0;
		default:
			return false;
		}
	}

	public static InterruptResult ApplyInterruptsTo(GameState state, int enemyIndex, string cursor, IReadOnlyList<int> fired, IReadOnlyList<string> only = null, string pendingNode = null)
	{
		EnemyDef enemyDef = Content.GetEnemyDef(state.Enemies[enemyIndex].EnemyId);
		string cur = cursor;
		List<int> f = ((fired != null) ? new List<int>(fired) : new List<int>());
		List<int> now = new List<int>();
		IReadOnlyList<EnemyInterrupt> its = enemyDef.Interrupts;
		if (its != null)
		{
			for (int k = 0; k < its.Count; k++)
			{
				EnemyInterrupt it = its[k];
				if (!f.Contains(k) && (only == null || only.Contains(it.On)) && (it.From == null || it.From.Contains(cur) || (pendingNode != null && it.From.Contains(pendingNode))) && InterruptHolds(state, enemyIndex, it))
				{
					cur = it.Goto;
					f.Add(k);
					now.Add(k);
				}
			}
		}
		return new InterruptResult(cur, f, now);
	}

	private static bool ArmUsable(EnemyDef def, EnemyRandomArm arm, EnemyState enemy)
	{
		string target = NodeMoveId(def, arm.To);
		if (target == null)
		{
			return true;
		}
		if (arm.Once == true && enemy.UsedOnce != null && enemy.UsedOnce.Contains(target))
		{
			return false;
		}
		IReadOnlyList<string> last = enemy.LastMoves;
		if (arm.NoRepeat == true && Streak(1))
		{
			return false;
		}
		if (arm.MaxRepeat.HasValue && Streak(arm.MaxRepeat.Value))
		{
			return false;
		}
		return true;
		bool Streak(int n)
		{
			if (last == null || last.Count < n)
			{
				return false;
			}
			for (int i = 0; i < n; i++)
			{
				if (last[i] != target)
				{
					return false;
				}
			}
			return true;
		}
	}

	public static WalkResult WalkToMove(GameState state, int enemyIndex, string cursor, RngState rng)
	{
		EnemyDef def = Content.GetEnemyDef(state.Enemies[enemyIndex].EnemyId);
		EnemyState enemy = state.Enemies[enemyIndex];
		string cur = cursor;
		RngState r = rng;
		List<string> onceMoveIds = new List<string>();
		for (int guard = 0; guard < 32; guard++)
		{
			if (!def.Nodes.TryGetValue(cur, out EnemyNode node))
			{
				throw new InvalidOperationException($"敵 {def.Id} の行動グラフに節 {cur} が無い");
			}
			if (node.Move != null)
			{
				return new WalkResult(cur, MoveById(def, node.Move), r, onceMoveIds);
			}
			if (node.Random != null)
			{
				List<EnemyRandomArm> usable = new List<EnemyRandomArm>();
				for (int k = 0; k < node.Random.Count; k++)
				{
					if (ArmUsable(def, node.Random[k], enemy))
					{
						usable.Add(node.Random[k]);
					}
				}
				IReadOnlyList<EnemyRandomArm> readOnlyList;
				if (usable.Count <= 0)
				{
					readOnlyList = node.Random;
				}
				else
				{
					IReadOnlyList<EnemyRandomArm> readOnlyList2 = usable;
					readOnlyList = readOnlyList2;
				}
				IReadOnlyList<EnemyRandomArm> arms = readOnlyList;
				List<double> weights = new List<double>(arms.Count);
				for (int i = 0; i < arms.Count; i++)
				{
					weights.Add(arms[i].Weight);
				}
				(int Index, RngState Next) tuple = Rng.WeightedIndex(r, weights);
				int idx = tuple.Index;
				r = tuple.Next;
				EnemyRandomArm arm = arms[idx];
				if (arm.Once == true)
				{
					string target = NodeMoveId(def, arm.To);
					if (target != null)
					{
						onceMoveIds.Add(target);
					}
				}
				cur = arm.To;
			}
			else
			{
				if (!(node.If != null))
				{
					throw new InvalidOperationException($"敵 {def.Id} の節 {cur} は技/乱択/条件のどれでもない");
				}
				cur = (EvalCondition(state, enemyIndex, node.If) ? node.Then : node.Else) ?? throw new InvalidOperationException($"敵 {def.Id} の条件の節 {cur} に then/else が無い");
			}
		}
		throw new InvalidOperationException($"敵 {def.Id} の行動グラフが {cursor} から技に着地しない (循環)");
	}

	public static List<string> PeekMoves(EnemyDef def, GameState state, int enemyIndex, string cursor, int depth)
	{
		List<string> outp = new List<string>();
		string cur = cursor;
		for (int d = 0; d < depth; d++)
		{
			int guard = 0;
			string landed = null;
			bool stop = false;
			while (guard++ < 32)
			{
				if (!def.Nodes.TryGetValue(cur, out EnemyNode node))
				{
					stop = true;
					break;
				}
				if (node.Move != null)
				{
					landed = node.Move;
					cur = node.Next ?? cur;
					break;
				}
				if (node.Random != null)
				{
					stop = true;
					break;
				}
				if (node.If != null)
				{
					cur = (EvalCondition(state, enemyIndex, node.If) ? node.Then : node.Else) ?? cur;
					continue;
				}
				stop = true;
				break;
			}
			if (stop || landed == null)
			{
				outp.Add(null);
				return outp;
			}
			outp.Add(landed);
		}
		return outp;
	}

	public static EnemyMove FirstMoveOf(EnemyDef def, string nodeId)
	{
		string cur = nodeId;
		for (int i = 0; i < 32; i++)
		{
			if (!def.Nodes.TryGetValue(cur, out EnemyNode node))
			{
				return null;
			}
			if (node.Move != null)
			{
				for (int k = 0; k < def.Moves.Count; k++)
				{
					if (def.Moves[k].Id == node.Move)
					{
						return def.Moves[k];
					}
				}
				return null;
			}
			if (node.Random != null)
			{
				if (node.Random.Count <= 0)
				{
					return null;
				}
				return FirstMoveOf(def, node.Random[0].To);
			}
			if (node.If != null)
			{
				cur = node.Then ?? cur;
				continue;
			}
			return null;
		}
		return null;
	}

	public static EnemyInterrupt SleepingInterrupt(EnemyDef def, EnemyState e)
	{
		if (def.Interrupts == null)
		{
			return null;
		}
		for (int k = 0; k < def.Interrupts.Count; k++)
		{
			EnemyInterrupt it = def.Interrupts[k];
			if (!(it.On != "damageTaken") && (e.FiredInterrupts == null || !e.FiredInterrupts.Contains(k)) && (it.From == null || it.From.Contains(e.Node) || (e.IntentNode != null && it.From.Contains(e.IntentNode))))
			{
				return it;
			}
		}
		return null;
	}
}
