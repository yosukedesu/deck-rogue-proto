using System;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game;

public sealed class CombatPresentationLedger
{
	public sealed class Vitals
	{
		public long Hp;

		public long MaxHp;

		public long Block;

		public long IceBlock;

		public string EnemyId;

		internal Vitals Copy()
		{
			return new Vitals
			{
				Hp = Hp,
				MaxHp = MaxHp,
				Block = Block,
				IceBlock = IceBlock,
				EnemyId = EnemyId
			};
		}
	}

	public sealed class Batch
	{
		public int Id;

		public int Generation;

		public int From;

		public int To;

		public int Pending;

		public bool Finished;

		public GameState After;
	}

	public sealed class Receipt
	{
		public int Index;

		public int Generation;

		public int Target = -1;

		public GameEvent Event;

		public Batch Batch;

		public CombatPresentationReceipt Values;

		public string Reason;

		public bool Committed;

		public bool Exact;

		public EnemyAttackContactPlan Projection { get; internal set; }

		public string ProjectionReason { get; internal set; }

		public int NextContribution { get; internal set; }

		public bool ProjectionStarted { get; internal set; }
	}

	private readonly SortedDictionary<int, Receipt> pending = new SortedDictionary<int, Receipt>();

	private readonly List<Batch> batches = new List<Batch>();

	private readonly Dictionary<int, Vitals> vitals = new Dictionary<int, Vitals>();

	private readonly HashSet<int> unstable = new HashSet<int>();

	private GameState latest;

	private int nextBatch;

	public Action<string, int> Audit;

	public int Generation { get; private set; }

	public int PendingCount => pending.Count;

	public int Discontinuities { get; private set; }

	public int Reconciliations { get; private set; }

	public string LastReason { get; private set; }

	public GameState Latest => latest;

	public bool Active => latest != null;

	private static Vitals Player(PlayerState p)
	{
		return new Vitals
		{
			Hp = p.Hp,
			MaxHp = p.MaxHp,
			Block = p.Block,
			IceBlock = p.IceBlock
		};
	}

	private static Vitals Enemy(EnemyState e)
	{
		return new Vitals
		{
			Hp = e.Hp,
			MaxHp = e.MaxHp,
			Block = e.Block,
			EnemyId = e.EnemyId
		};
	}

	public static bool IsNumeric(GameEvent e)
	{
		return Read(e) != null || e is GameEvent_DamageDealt || e is GameEvent_ConfusedAttack || e is GameEvent_ThornsReflected || e is GameEvent_BlockGained || e is GameEvent_IceBlockGained || e is GameEvent_BlockShattered || e is GameEvent_HpHealed || e is GameEvent_HpLost || e is GameEvent_ScaldTick || e is GameEvent_BurnTick || e is GameEvent_EnemyHealed || e is GameEvent_RegenTicked || e is GameEvent_DeathSaved || e is GameEvent_MaxHpGained;
	}

	public static CombatPresentationReceipt Read(GameEvent e)
	{
		if (!(e is GameEvent_DamageDealt v))
		{
			if (!(e is GameEvent_ConfusedAttack v2))
			{
				if (!(e is GameEvent_ThornsReflected v3))
				{
					if (!(e is GameEvent_BlockGained v4))
					{
						if (!(e is GameEvent_IceBlockGained v5))
						{
							if (!(e is GameEvent_BlockShattered v6))
							{
								if (!(e is GameEvent_HpHealed v7))
								{
									if (!(e is GameEvent_HpLost v8))
									{
										if (!(e is GameEvent_ScaldTick v9))
										{
											if (!(e is GameEvent_BurnTick v10))
											{
												if (!(e is GameEvent_EnemyHealed v11))
												{
													if (!(e is GameEvent_RegenTicked v12))
													{
														if (!(e is GameEvent_DeathSaved v13))
														{
															if (e is GameEvent_MaxHpGained v14)
															{
																return v14.PresentationReceipt;
															}
															return null;
														}
														return v13.PresentationReceipt;
													}
													return v12.PresentationReceipt;
												}
												return v11.PresentationReceipt;
											}
											return v10.PresentationReceipt;
										}
										return v9.PresentationReceipt;
									}
									return v8.PresentationReceipt;
								}
								return v7.PresentationReceipt;
							}
							return v6.PresentationReceipt;
						}
						return v5.PresentationReceipt;
					}
					return v4.PresentationReceipt;
				}
				return v3.PresentationReceipt;
			}
			return v2.PresentationReceipt;
		}
		return v.PresentationReceipt;
	}

	private static bool TargetAgrees(GameEvent e, CombatPresentationReceipt r)
	{
		int target = -1;
		bool enemy = false;
		if (!(e is GameEvent_DamageDealt v))
		{
			if (!(e is GameEvent_ConfusedAttack v2))
			{
				if (!(e is GameEvent_BlockShattered v3))
				{
					if (!(e is GameEvent_BurnTick v4))
					{
						if (!(e is GameEvent_EnemyHealed v5))
						{
							if (!(e is GameEvent_RegenTicked v6))
							{
								if (e is GameEvent_BlockGained v7)
								{
									if (v7.Target == "enemy")
									{
										return r.Target == "enemy" && r.EnemyIndex.HasValue && r.EnemyIndex.Value >= 0;
									}
									if (v7.Target != "player")
									{
										return false;
									}
								}
							}
							else
							{
								enemy = true;
								target = v6.EnemyIndex;
							}
						}
						else
						{
							enemy = true;
							target = v5.TargetIndex;
						}
					}
					else
					{
						enemy = true;
						target = v4.EnemyIndex;
					}
				}
				else
				{
					enemy = true;
					target = v3.EnemyIndex;
				}
			}
			else
			{
				enemy = true;
				target = v2.TargetIndex;
			}
		}
		else
		{
			if (v.Source != "player" && v.Source != "enemy")
			{
				return false;
			}
			enemy = v.Source == "player";
			target = (enemy ? (v.EnemyIndex ?? (-2)) : (-1));
		}
		return (!enemy) ? (r.Target == "player" && !r.EnemyIndex.HasValue && r.EnemyId == null && target == -1) : (target >= 0 && r.Target == "enemy" && r.EnemyIndex == target);
	}

	public static bool Valid(GameEvent e, CombatPresentationReceipt r, out string reason)
	{
		reason = null;
		if (r == null)
		{
			reason = "missing_numeric_metadata";
			return false;
		}
		if (r.Version != 1)
		{
			reason = "unsupported_numeric_metadata_version";
			return false;
		}
		if (r.Before == null || r.After == null)
		{
			reason = "missing_numeric_snapshot";
			return false;
		}
		if (!TargetAgrees(e, r))
		{
			reason = "numeric_target_mismatch";
			return false;
		}
		bool player = r.Target == "player";
		if ((!player && r.Target != "enemy") || (player && (!r.Before.IceBlock.HasValue || !r.After.IceBlock.HasValue)) || (!player && (r.Before.IceBlock.HasValue || r.After.IceBlock.HasValue || string.IsNullOrEmpty(r.EnemyId))))
		{
			reason = "numeric_field_mask_mismatch";
			return false;
		}
		if (r.Before.MaxHp <= 0 || r.After.MaxHp <= 0 || r.Before.Block < 0 || r.After.Block < 0 || r.Before.IceBlock < 0 || r.After.IceBlock < 0)
		{
			reason = "invalid_numeric_snapshot";
			return false;
		}
		int? loss = ((e is GameEvent_DamageDealt d) ? new int?(d.HpLoss) : ((e is GameEvent_ConfusedAttack c) ? new int?(c.HpLoss) : ((e is GameEvent_ThornsReflected t) ? new int?(t.HpLoss) : ((int?)null))));
		if (loss.HasValue && (long)r.Before.Hp - (long)r.After.Hp != loss.Value)
		{
			reason = "numeric_hp_loss_payload_mismatch";
			return false;
		}
		return true;
	}

	public void Clear()
	{
		int generation = Generation + 1;
		Generation = generation;
		pending.Clear();
		batches.Clear();
		vitals.Clear();
		unstable.Clear();
		latest = null;
		nextBatch = 0;
		LastReason = null;
	}

	private void Seed(GameState state)
	{
		vitals[-1] = Player(state.Player);
		for (int i = 0; i < state.Enemies.Count; i++)
		{
			vitals[i] = Enemy(state.Enemies[i]);
		}
		latest = state;
	}

	private static bool Prefix(GameState before, GameState after)
	{
		if (before == null || after == null || before.EventLog.Count > after.EventLog.Count)
		{
			return false;
		}
		for (int i = 0; i < before.EventLog.Count; i++)
		{
			if ((object)before.EventLog[i] != after.EventLog[i])
			{
				return false;
			}
		}
		return true;
	}

	public Batch Register(GameState before, GameState after)
	{
		if (!Prefix(before, after))
		{
			Note("unproved_command_prefix", -1);
			return null;
		}
		if (latest == null)
		{
			Seed(before);
		}
		if ((object)latest != before)
		{
			Note("numeric_command_lineage_mismatch", -1);
			return null;
		}
		Batch b = new Batch
		{
			Id = ++nextBatch,
			Generation = Generation,
			From = before.EventLog.Count,
			To = after.EventLog.Count,
			After = after
		};
		batches.Add(b);
		for (int i = 0; i < Math.Max(before.Enemies.Count, after.Enemies.Count); i++)
		{
			if (i >= before.Enemies.Count || i >= after.Enemies.Count || before.Enemies[i].EnemyId != after.Enemies[i].EnemyId)
			{
				unstable.Add(i);
			}
		}
		for (int j = b.From; j < b.To; j++)
		{
			if (after.EventLog[j] is GameEvent_EnemyHatched hatched)
			{
				unstable.Add(hatched.EnemyIndex);
			}
		}
		foreach (Receipt earlier in pending.Values)
		{
			if (earlier.Target >= 0 && unstable.Contains(earlier.Target))
			{
				earlier.Reason = "numeric_population_boundary_unproved";
			}
		}
		for (int k = b.From; k < b.To; k++)
		{
			GameEvent e = after.EventLog[k];
			if (!IsNumeric(e))
			{
				continue;
			}
			CombatPresentationReceipt value = Read(e);
			Receipt receipt = new Receipt
			{
				Index = k,
				Generation = Generation,
				Event = e,
				Batch = b,
				Values = value
			};
			if (Valid(e, value, out string reason))
			{
				receipt.Target = ((value.Target == "player") ? (-1) : value.EnemyIndex.Value);
				if (receipt.Target >= 0 && (!vitals.TryGetValue(receipt.Target, out Vitals v) || v.EnemyId != value.EnemyId))
				{
					receipt.Reason = "numeric_target_identity_unproved";
				}
				else if (unstable.Contains(receipt.Target))
				{
					receipt.Reason = "numeric_population_boundary_unproved";
				}
			}
			else
			{
				receipt.Reason = reason;
			}
			pending.Add(k, receipt);
			b.Pending++;
		}
		latest = after;
		return b;
	}

	public Receipt Find(int index, GameEvent e)
	{
		Receipt value;
		return (pending.TryGetValue(index, out value) && (object)value.Event == e) ? value : null;
	}

	public bool Current(Receipt r)
	{
		Receipt p;
		return r != null && r.Generation == Generation && !r.Committed && pending.TryGetValue(r.Index, out p) && p == r;
	}

	public bool Ready(Receipt r)
	{
		if (!Current(r))
		{
			return false;
		}
		using (SortedDictionary<int, Receipt>.Enumerator enumerator = pending.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current.Value == r;
			}
		}
		return false;
	}

	public bool ObserveScenarioReservation(IReadOnlyList<Receipt> group, out bool ready, out string reason)
	{
		ready = false;
		reason = null;
		if (group == null || group.Count == 0 || group.Count > 256)
		{
			reason = "scenario_group_count_invalid";
			return false;
		}
		Batch batch = group[0]?.Batch;
		for (int i = 0; i < group.Count; i++)
		{
			if (!Current(group[i]) || group[i].Reason != null || group[i].Batch != batch || (i > 0 && group[i].Index <= group[i - 1].Index))
			{
				reason = "scenario_group_receipt_unproved";
				return false;
			}
		}
		int next = 0;
		int first = group[0].Index;
		int last = group[group.Count - 1].Index;
		foreach (KeyValuePair<int, Receipt> pair in pending)
		{
			if (pair.Key >= first)
			{
				if (pair.Key > last)
				{
					break;
				}
				if (next >= group.Count || pair.Value != group[next++])
				{
					reason = "scenario_group_numeric_interleaving";
					return false;
				}
			}
		}
		if (next != group.Count)
		{
			reason = "scenario_group_incomplete";
			return false;
		}
		ready = Ready(group[0]);
		return true;
	}

	private static bool Same(Vitals v, CombatPresentationVitals p)
	{
		return v.Hp == p.Hp && v.MaxHp == p.MaxHp && v.Block == p.Block && v.IceBlock == p.IceBlock.GetValueOrDefault();
	}

	public EnemyAttackContactPlan ConfigureEnemyProjection(Receipt r, EnemyAttackContactPlan.SourceProof proof)
	{
		if (!Current(r) || r.Projection != null)
		{
			return r?.Projection;
		}
		r.Projection = EnemyAttackContactPlan.Capture(r.Event, r.Values, proof);
		if (r.Reason != null)
		{
			RejectProjection(r, "enemy_contact_numeric_receipt_invalid");
		}
		else if (r.Projection.Reason != null)
		{
			RejectProjection(r, r.Projection.Reason);
		}
		else if (!SourceCurrent(r, proof))
		{
			RejectProjection(r, "enemy_contact_source_unproved");
		}
		return r.Projection;
	}

	private bool SourceCurrent(Receipt r, EnemyAttackContactPlan.SourceProof proof)
	{
		EnemyAttackContactPlan p = r.Projection;
		return p != null && p.Matches(proof) && !unstable.Contains(p.SourceIndex) && latest != null && p.SourceIndex < latest.Enemies.Count && latest.Enemies[p.SourceIndex].EnemyId == p.SourceId;
	}

	private void RejectProjection(Receipt r, string reason)
	{
		r.ProjectionReason = reason;
		Note(reason, r.Index);
	}

	public bool TryBeginProjection(Receipt r, EnemyAttackContactPlan.SourceProof proof)
	{
		if (!Current(r) || !Ready(r) || r.Projection == null || !r.Projection.Individual || r.ProjectionReason != null || r.ProjectionStarted)
		{
			return false;
		}
		if (r.Reason != null || !SourceCurrent(r, proof))
		{
			RejectProjection(r, "enemy_contact_source_unproved");
			return false;
		}
		if (!vitals.TryGetValue(r.Target, out Vitals v) || !Same(v, r.Projection.Before))
		{
			RejectProjection(r, "enemy_contact_numeric_boundary_mismatch");
			return false;
		}
		r.ProjectionStarted = true;
		r.Exact = true;
		return true;
	}

	public bool CommitContribution(Receipt r, int ordinal, EnemyAttackContactPlan.SourceProof proof)
	{
		if (!Current(r) || !Ready(r) || !r.ProjectionStarted || r.ProjectionReason != null || r.Projection == null || !r.Projection.Individual)
		{
			return false;
		}
		if (ordinal != r.NextContribution || ordinal < 0 || ordinal >= r.Projection.Count)
		{
			Note("enemy_contact_ordinal_mismatch", r.Index);
			return false;
		}
		if (r.Reason != null || !SourceCurrent(r, proof))
		{
			RejectProjection(r, "enemy_contact_source_lost");
			return false;
		}
		if (!vitals.TryGetValue(r.Target, out Vitals v))
		{
			RejectProjection(r, "enemy_contact_target_unavailable");
			return false;
		}
		EnemyAttackContribution row = r.Projection[ordinal];
		long hp = v.Hp - row.HpLossContribution;
		long block = v.Block - row.BlockContribution;
		long ice = v.IceBlock - row.IceContribution;
		bool last = ordinal == r.Projection.Count - 1;
		CombatPresentationVitals after = r.Projection.After;
		if (block < 0 || ice < 0 || (last && (hp != after.Hp || v.MaxHp != after.MaxHp || block != after.Block || ice != after.IceBlock.GetValueOrDefault())))
		{
			RejectProjection(r, "enemy_contact_numeric_continuity_lost");
			return false;
		}
		v.Hp = hp;
		v.Block = block;
		v.IceBlock = ice;
		r.NextContribution++;
		if (last)
		{
			CompleteReceipt(r);
		}
		return true;
	}

	public bool HasPendingEnemyProjectionBefore(int eventIndex)
	{
		foreach (KeyValuePair<int, Receipt> pair in pending)
		{
			if (pair.Key >= eventIndex)
			{
				break;
			}
			Receipt r = pair.Value;
			if (r.Projection != null && r.Projection.Individual && (r.ProjectionReason == null || r.ProjectionStarted))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasPendingEnemyProjectionBefore(int generation, int eventIndex)
	{
		return generation == Generation && HasPendingEnemyProjectionBefore(eventIndex);
	}

	private void CompleteReceipt(Receipt r)
	{
		r.Committed = true;
		pending.Remove(r.Index);
		r.Batch.Pending = Math.Max(0, r.Batch.Pending - 1);
	}

	public bool Commit(Receipt r)
	{
		if (!Current(r))
		{
			return false;
		}
		if (r.NextContribution > 0)
		{
			Note("enemy_contact_partial_projection_requires_reconcile", r.Index);
			return false;
		}
		if (!Ready(r))
		{
			Note("numeric_contact_out_of_order", r.Index);
			return false;
		}
		if (r.Reason == null && vitals.TryGetValue(r.Target, out Vitals v))
		{
			r.Exact = Same(v, r.Values.Before);
			if (!r.Exact)
			{
				r.Reason = "unannounced_numeric_boundary";
				Note(r.Reason, r.Index);
			}
			v.Hp += (long)r.Values.After.Hp - (long)r.Values.Before.Hp;
			v.MaxHp += (long)r.Values.After.MaxHp - (long)r.Values.Before.MaxHp;
			v.Block += (long)r.Values.After.Block - (long)r.Values.Before.Block;
			v.IceBlock += (long)r.Values.After.IceBlock.GetValueOrDefault() - (long)r.Values.Before.IceBlock.GetValueOrDefault();
		}
		else
		{
			Note(r.Reason ?? "numeric_target_unavailable", r.Index);
		}
		CompleteReceipt(r);
		return true;
	}

	public bool Finish(Batch b)
	{
		if (b == null || b.Generation != Generation || !batches.Contains(b))
		{
			return false;
		}
		b.Finished = true;
		return TryReconcile();
	}

	public bool TryReconcile()
	{
		if (latest == null || pending.Count != 0 || batches.Count == 0)
		{
			return false;
		}
		foreach (Batch b in batches)
		{
			if (!b.Finished)
			{
				return false;
			}
		}
		bool different = false;
		if (!vitals.TryGetValue(-1, out Vitals p) || !Same(p, EventsVitals(latest.Player)))
		{
			different = true;
		}
		for (int i = 0; i < latest.Enemies.Count; i++)
		{
			if (!vitals.TryGetValue(i, out Vitals v) || v.EnemyId != latest.Enemies[i].EnemyId || !Same(v, EventsVitals(latest.Enemies[i])))
			{
				different = true;
			}
		}
		if (different || unstable.Count > 0)
		{
			Note("command_completion_numeric_reconcile", -1);
		}
		vitals.Clear();
		unstable.Clear();
		Seed(latest);
		batches.Clear();
		Reconciliations++;
		return true;
	}

	private static CombatPresentationVitals EventsVitals(PlayerState p)
	{
		return new CombatPresentationVitals
		{
			Hp = p.Hp,
			MaxHp = p.MaxHp,
			Block = p.Block,
			IceBlock = p.IceBlock
		};
	}

	private static CombatPresentationVitals EventsVitals(EnemyState p)
	{
		return new CombatPresentationVitals
		{
			Hp = p.Hp,
			MaxHp = p.MaxHp,
			Block = p.Block
		};
	}

	private void Note(string reason, int index)
	{
		LastReason = reason;
		Discontinuities++;
		Audit?.Invoke(reason, index);
	}

	public bool TryGet(GameState state, int target, out Vitals result)
	{
		result = null;
		if (latest == null || state == null || (object)latest != state || unstable.Contains(target) || !vitals.TryGetValue(target, out Vitals v))
		{
			return false;
		}
		if (target >= 0 && (target >= state.Enemies.Count || state.Enemies[target].EnemyId != v.EnemyId))
		{
			return false;
		}
		result = v.Copy();
		return true;
	}

	public bool CommitBlockBoundary(GameState state, int target, CombatPresentationVitals before, int block)
	{
		if (block < 0 || latest == null || (object)latest != state || unstable.Contains(target) || !vitals.TryGetValue(target, out Vitals v) || !Same(v, before))
		{
			return false;
		}
		if (target >= 0 && (target >= state.Enemies.Count || state.Enemies[target].EnemyId != v.EnemyId))
		{
			return false;
		}
		v.Block = block;
		return true;
	}

	public bool HasPending(int target)
	{
		foreach (Receipt p in pending.Values)
		{
			if (p.Target == target && p.Reason == null)
			{
				return true;
			}
		}
		return false;
	}

	public static int Display(long value)
	{
		return (int)((value > 0) ? ((value > int.MaxValue) ? int.MaxValue : value) : 0);
	}
}
