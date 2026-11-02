using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class Analysis
{
	public static readonly int[] TURN_TARGET_NORMAL = new int[2] { 4, 6 };

	public static readonly int[] TURN_TARGET_BOSS = new int[2] { 6, 10 };

	public static BattleMetrics ComputeBattleMetrics(IReadOnlyList<GameEvent> log)
	{
		SortedDictionary<int, TurnMetrics> turns = new SortedDictionary<int, TurnMetrics>();
		int cur = 0;
		bool enemyPhase = false;
		bool awaitingDraw = false;
		for (int i = 0; i < log.Count; i++)
		{
			GameEvent e = log[i];
			if (!(e is GameEvent_TurnStarted ts))
			{
				if (!(e is GameEvent_CardsDrawn cd))
				{
					if (!(e is GameEvent_TurnEnded te))
					{
						if (!(e is GameEvent_DamageDealt dd))
						{
							if (!(e is GameEvent_BurnTick bt))
							{
								if (!(e is GameEvent_CardPlayed cp))
								{
									if (!(e is GameEvent_RetainerRushed))
									{
										if (!(e is GameEvent_RetainerExpired))
										{
											if (!(e is GameEvent_RetainersTriggered))
											{
												if (!(e is GameEvent_CardSet))
												{
													if (!(e is GameEvent_ReactionTriggered))
													{
														if (!(e is GameEvent_ReactionHeld))
														{
															if (e is GameEvent_SetCardExpired)
															{
																At(cur).Expires++;
															}
														}
														else
														{
															At(cur).Holds++;
														}
													}
													else
													{
														At(cur).Fires++;
													}
												}
												else
												{
													At(cur).Sets++;
												}
											}
											else
											{
												At(cur).Rallies++;
											}
										}
										else
										{
											At(cur).DollsGone++;
										}
									}
									else
									{
										At(cur).Dolls++;
									}
								}
								else
								{
									TurnMetrics m = At(cur);
									m.Plays++;
									if (!IsDollCardId(cp.CardId))
									{
										m.NonDollPlays++;
									}
								}
							}
							else
							{
								At(cur).Dealt += bt.Amount;
							}
						}
						else if (dd.Source == "player")
						{
							if (enemyPhase)
							{
								At(cur).Counter += dd.HpLoss;
							}
							else
							{
								At(cur).Dealt += dd.HpLoss;
							}
						}
						else
						{
							At(cur).Taken += dd.HpLoss;
						}
					}
					else
					{
						enemyPhase = true;
						awaitingDraw = false;
						if (te.Unplayed != null)
						{
							At(cur).Unplayed = new List<string>(te.Unplayed);
						}
					}
				}
				else if (awaitingDraw && cd.Cards != null)
				{
					TurnMetrics turnMetrics = At(cur);
					List<string> h = turnMetrics.Hand ?? new List<string>();
					h.AddRange(cd.Cards);
					turnMetrics.Hand = h;
					awaitingDraw = false;
				}
			}
			else
			{
				cur = ts.Turn;
				enemyPhase = false;
				TurnMetrics m2 = At(cur);
				if (ts.Hand != null)
				{
					m2.Hand = new List<string>(ts.Hand);
					awaitingDraw = true;
				}
			}
		}
		List<TurnMetrics> perTurn = turns.Values.ToList();
		int maxTurn = 0;
		for (int j = 0; j < perTurn.Count; j++)
		{
			maxTurn = Math.Max(maxTurn, perTurn[j].Dealt);
		}
		int dollOnlyOpening = 0;
		for (int k = 0; k < perTurn.Count && k < 3; k++)
		{
			if (perTurn[k].Plays > 0 && perTurn[k].NonDollPlays == 0)
			{
				dollOnlyOpening++;
			}
		}
		return new BattleMetrics
		{
			Turns = perTurn.Count,
			T1Damage = ((perTurn.Count > 0) ? perTurn[0].Dealt : 0),
			TotalDealt = Sum((TurnMetrics turnMetrics2) => turnMetrics2.Dealt) + Sum((TurnMetrics turnMetrics2) => turnMetrics2.Counter),
			TotalTaken = Sum((TurnMetrics turnMetrics2) => turnMetrics2.Taken),
			MaxTurnDamage = maxTurn,
			Sets = Sum((TurnMetrics turnMetrics2) => turnMetrics2.Sets),
			Fires = Sum((TurnMetrics turnMetrics2) => turnMetrics2.Fires),
			Holds = Sum((TurnMetrics turnMetrics2) => turnMetrics2.Holds),
			Expires = Sum((TurnMetrics turnMetrics2) => turnMetrics2.Expires),
			Dolls = Sum((TurnMetrics turnMetrics2) => turnMetrics2.Dolls),
			DollsGone = Sum((TurnMetrics turnMetrics2) => turnMetrics2.DollsGone),
			Rallies = Sum((TurnMetrics turnMetrics2) => turnMetrics2.Rallies),
			DollOnlyOpening = dollOnlyOpening,
			PerTurn = perTurn
		};
		TurnMetrics At(int t)
		{
			if (!turns.TryGetValue(t, out TurnMetrics m3))
			{
				m3 = new TurnMetrics
				{
					Turn = t
				};
				turns[t] = m3;
			}
			return m3;
		}
		int Sum(Func<TurnMetrics, int> func)
		{
			int s = 0;
			for (int l = 0; l < perTurn.Count; l++)
			{
				s += func(perTurn[l]);
			}
			return s;
		}
	}

	private static bool IsDollCardId(string cardId)
	{
		CardDef def;
		try
		{
			def = Content.GetCardDef(cardId);
		}
		catch (Exception)
		{
			return false;
		}
		if (def.Retainer == true)
		{
			return true;
		}
		List<DeclarativeEffect> all = new List<DeclarativeEffect>(def.Effects);
		if (def.Modes != null)
		{
			foreach (CardMode mode in def.Modes)
			{
				all.AddRange(mode.Effects);
			}
		}
		for (int i = 0; i < all.Count; i++)
		{
			switch (all[i].Effect)
			{
			case "summonPermanent":
			case "copyRetainer":
			case "copyLastRetainer":
			case "twinNextRetainer":
				return true;
			}
		}
		return false;
	}

	private static int Median(List<int> xs)
	{
		List<int> s = new List<int>(xs);
		s.Sort();
		if (s.Count != 0)
		{
			return s[s.Count >> 1];
		}
		return 0;
	}

	private static double? Avg(List<double> xs)
	{
		if (xs.Count == 0)
		{
			return null;
		}
		double sum = 0.0;
		for (int i = 0; i < xs.Count; i++)
		{
			sum += xs[i];
		}
		return Math.Round(sum / (double)xs.Count * 100.0) / 100.0;
	}

	public static List<ActSummary> ActSummaries(IReadOnlyList<BattleRow> rows)
	{
		List<int> list = (from a in rows.Select((BattleRow r) => r.Act).Distinct()
			orderby a
			select a).ToList();
		List<ActSummary> outp = new List<ActSummary>();
		foreach (int act in list)
		{
			List<BattleRow> rs = rows.Where((BattleRow r) => r.Act == act).ToList();
			List<BattleRow> normals = rs.Where((BattleRow r) => !r.Boss && !r.Elite && r.Metrics != null).ToList();
			List<int> t1 = (from r in rs
				where r.Metrics != null
				select r.Metrics.T1Damage).ToList();
			List<BattleRow> late = rs.Where((BattleRow r) => r.Metrics != null).ToList();
			int lateHit = late.Count((BattleRow r) => r.Metrics.PerTurn.Skip(1).Any((TurnMetrics m) => m.Taken > 0 || m.Fires > 0 || m.Holds > 0));
			BattleRow boss = rs.FirstOrDefault((BattleRow r) => r.Boss && r.Metrics != null);
			outp.Add(new ActSummary
			{
				Act = act,
				Battles = rs.Count,
				NormalTurnsAvg = Avg(((IEnumerable<BattleRow>)normals).Select((Func<BattleRow, double>)((BattleRow r) => r.Metrics.Turns)).ToList()),
				BossTurns = boss?.Metrics.Turns,
				EliteTurns = (from r in rs
					where r.Elite && r.Metrics != null
					select r.Metrics.Turns).ToList(),
				T1Damage = ((t1.Count > 0) ? new T1DamageStat
				{
					Min = t1.Min(),
					Median = Median(t1),
					Max = t1.Max()
				} : null),
				HpLost = rs.Sum((BattleRow r) => Math.Max(0, r.HpBefore - r.HpAfter)),
				Sets = rs.Sum((BattleRow r) => (r.Metrics != null) ? r.Metrics.Sets : 0),
				Fires = rs.Sum((BattleRow r) => (r.Metrics != null) ? r.Metrics.Fires : 0),
				FunAvg = Avg(rs.Where((BattleRow r) => r.Rating != null && r.Rating.Fun.HasValue).Select((Func<BattleRow, double>)((BattleRow r) => r.Rating.Fun.Value)).ToList()),
				StrengthAvg = Avg(rs.Where((BattleRow r) => r.Rating != null && r.Rating.Strength.HasValue).Select((Func<BattleRow, double>)((BattleRow r) => r.Rating.Strength.Value)).ToList()),
				LateActionRate = ((late.Count > 0) ? new double?(Math.Round((double)lateHit / (double)late.Count * 100.0) / 100.0) : ((double?)null))
			});
		}
		return outp;
	}
}
