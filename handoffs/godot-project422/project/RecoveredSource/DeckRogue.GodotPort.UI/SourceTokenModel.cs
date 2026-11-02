using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Core;

namespace DeckRogue.GodotPort.UI;

public static class SourceTokenModel
{
	public static SourceTokenFrame SelectForBuild(SourceTokenFrame? previous, RunState selectedBoard, long epoch, long activeRevision, bool inputFenced, SessionChange? acceptedOwner = null, long acceptedRevision = -1L)
	{
		if (selectedBoard.Combat == null)
		{
			throw new ArgumentException("Token board must be combat.");
		}
		if (inputFenced && previous != null && previous.Epoch == epoch && SameCombatLineage(previous.Board.Combat, selectedBoard.Combat))
		{
			return new SourceTokenFrame(previous.Board, previous.Revision, previous.SetRedraw, epoch);
		}
		if (!inputFenced && acceptedOwner != null && acceptedOwner.Revision == acceptedRevision && SameCombatLineage(selectedBoard.Combat, acceptedOwner.Current.Combat))
		{
			return new SourceTokenFrame(OrdinaryUiBoard(acceptedOwner.Current, selectedBoard.Combat), acceptedOwner.Revision, null, epoch);
		}
		return new SourceTokenFrame(selectedBoard, activeRevision, null, epoch);
	}

	public static RunState OrdinaryUiBoard(RunState accepted, GameState displayed)
	{
		GameState current = accepted.Combat ?? throw new ArgumentException("Accepted token board must be combat.");
		if (!SameCombatLineage(current, displayed))
		{
			return accepted;
		}
		PlayerState player = current.Player with
		{
			Hp = displayed.Player.Hp,
			MaxHp = displayed.Player.MaxHp,
			Block = displayed.Player.Block,
			IceBlock = displayed.Player.IceBlock
		};
		EnemyState[] enemies = current.Enemies.Select((EnemyState enemy, int index) => (index >= displayed.Enemies.Count || !(displayed.Enemies[index].EnemyId == enemy.EnemyId)) ? enemy : enemy with
		{
			Hp = displayed.Enemies[index].Hp,
			MaxHp = displayed.Enemies[index].MaxHp,
			Block = displayed.Enemies[index].Block
		}).ToArray();
		return accepted with
		{
			Combat = current with
			{
				Player = player,
				Enemies = enemies
			}
		};
	}

	public static SourceTokenFrame? TryOwnedTurnStart(SourceTokenFrame? currentFrame, SourceTokenFrame? expectedFrame, SessionChange? owner, GameEvent evt, long epoch, long activeCommandRevision, long acceptedRevision, bool inputFenced, bool ordinaryPresentation, bool receiptAccepted)
	{
		if (!((currentFrame == null || expectedFrame == null || owner == null || (object)currentFrame != expectedFrame || expectedFrame.Epoch != epoch || !inputFenced) | ordinaryPresentation) && receiptAccepted && !owner.IsReset && owner.Revision == activeCommandRevision && owner.Revision == acceptedRevision && expectedFrame.Revision < owner.Revision && evt is GameEvent_TurnStarted started && owner.Events.Any((GameEvent e) => (object)e == evt))
		{
			GameState previous = owner.Previous?.Combat;
			if ((object)previous != null)
			{
				GameState current = owner.Current.Combat;
				if ((object)current != null && started.Turn == current.Turn && expectedFrame.Board.Combat?.Turn == previous.Turn && current.EventLog.Skip(previous.EventLog.Count).Any((GameEvent e) => (object)e == evt) && SameCombatLineage(expectedFrame.Board.Combat, previous) && SameCombatLineage(previous, current) && (object)expectedFrame.SetRedraw != current)
				{
					if (!current.Player.SetCards.Take(Math.Max(0, expectedFrame.Board.Combat.Player.SetSlots)).Any((CardInstance c) => Effects.TrapAge(current, c) == 1))
					{
						return null;
					}
					return expectedFrame with
					{
						SetRedraw = current
					};
				}
			}
		}
		return null;
	}

	private static bool SameCombatLineage(GameState? previous, GameState? selected)
	{
		if (previous != null && selected != null)
		{
			if ((object)previous != selected)
			{
				if (previous.EventLog.Count > 0 && selected.EventLog.Count > 0)
				{
					return (object)previous.EventLog[0] == selected.EventLog[0];
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public static string? GearMute(RunState run)
	{
		GameState s = run.Combat;
		if ((object)s != null)
		{
			if (run.Mana.GetValueOrDefault() >= 10)
			{
				if (s.GearUsedThisTurn != true)
				{
					if (!(s.PendingScry != null))
					{
						return null;
					}
					return "占術中";
				}
				return "今ターン済";
			}
			return "魔素不足";
		}
		return null;
	}

	public static bool HasGrowth(CardDef def)
	{
		return def.Effects.Any((DeclarativeEffect e) => e.Amount.HasValue && Effects.DOLL_GROWTH_EFFECTS.Contains(e.Effect));
	}

	public static bool ShouldRedrawSets(GameEvent evt, GameState current, int existingSlots, bool nudgeHp, bool displayCurrent)
	{
		if (evt is GameEvent_TurnStarted && (nudgeHp | displayCurrent))
		{
			return current.Player.SetCards.Take(Math.Max(0, existingSlots)).Any((CardInstance c) => Effects.TrapAge(current, c) == 1);
		}
		return false;
	}

	public static SourceTokenFrame? TrySetRedraw(SourceTokenFrame? currentFrame, SourceTokenFrame expectedFrame, GameState state, GameEvent evt, bool nudgeHp, bool displayCurrent)
	{
		if ((object)currentFrame != expectedFrame || !SameCombatLineage(expectedFrame.Board.Combat, state) || !ShouldRedrawSets(evt, state, expectedFrame.Board.Combat.Player.SetSlots, nudgeHp, displayCurrent))
		{
			return null;
		}
		return expectedFrame with
		{
			SetRedraw = state
		};
	}

	public static SourceTokenData Capture(SourceTokenFrame frame)
	{
		RunState run = frame.Board;
		GameState board = run.Combat ?? throw new ArgumentException("Token frame must own a combat board.");
		GameState state = frame.SetRedraw ?? board;
		int slots = Math.Max(0, board.Player.SetSlots);
		SourceSetToken[] sets = state.Player.SetCards.Take(slots).Select((CardInstance card) =>
		{
			bool flag = Effects.IsTrapLive(state, card);
			bool flag2 = flag && Effects.TrapCanFireThisPhase(state, card);
			int? num2 = Effects.TrapWindowsLeft(state, card);
			string text;
			if (flag)
			{
				if (!flag2)
				{
					if (!num2.HasValue)
					{
						text = "期限なし";
					}
					else
					{
						int? num3 = num2;
						text = "あと" + num3 + "回";
					}
				}
				else
				{
					text = "鳴る";
				}
			}
			else
			{
				text = "準備中";
			}
			string state2 = text;
			string text2 = CardText.Body(card.Def) + "\n" + Effects.TrapStatusTextKarakuri(state, card);
			string text3 = Effects.SetCardLiveDamage(state, card.Def);
			if (text3 != null)
			{
				text2 = text2 + "\n" + text3;
			}
			return new SourceSetToken(card, flag, flag2, num2, state2, text2);
		}).ToArray();
		SourceGearToken[] gears = (run.Gears ?? Array.Empty<GearInstance>()).Select((GearInstance gear) =>
		{
			GearDef gearDef = Content.GetGearDef(gear.GearId);
			string text = Gears.GearBlockedReason(board, run.Mana.GetValueOrDefault(), gear);
			string text2 = gearDef.Text + "\n残り " + gear.Charges + " 回";
			string text3 = Gears.GearLiveDamage(board, gearDef);
			string text4 = Gears.GearNoEffectReason(board, gearDef);
			if (text3 != null)
			{
				text2 = text2 + "\n" + text3;
			}
			if (text4 != null)
			{
				text2 = text2 + "\nいま組んでも何も起きない: " + text4;
			}
			if (text != null)
			{
				text2 = text2 + "\nいまは組めない: " + text;
			}
			return new SourceGearToken(gear, gearDef, text, text2);
		}).ToArray();
		SourcePermanentToken[] perms = board.Player.Permanents.Where((CardInstance c) => c.Innate != true).Select((CardInstance card) =>
		{
			bool flag = Effects.IsDoll(card);
			bool flag2 = flag && HasGrowth(card.Def);
			int? num2 = (flag ? Effects.DollLifeLeft(board, card) : ((int?)null));
			string text = CardText.Body(card.Def);
			if (flag)
			{
				string text2 = text;
				string text3;
				if (!num2.HasValue)
				{
					text3 = "期限なし";
				}
				else
				{
					int? num3 = num2;
					text3 = "あと" + num3 + "ターンで消える";
				}
				text = text2 + "\n" + text3 + (flag2 ? ("・火勢+" + Effects.DollGrowth(board, card)) : "");
			}
			return new SourcePermanentToken(card, flag, num2, flag2, text);
		}).ToArray();
		string mute = GearMute(run);
		StringBuilder key = new StringBuilder();
		Part(frame.Revision);
		Part(frame.Epoch);
		Part(slots);
		Part(mute);
		SourceSetToken[] array = sets;
		foreach (SourceSetToken t in array)
		{
			Part(t.Card.Uid);
			Part(t.Card.Def.Id);
			Part(t.Card.Def.Name);
			Part(t.State);
			Part(t.Windows);
			Part(t.Details);
		}
		SourceGearToken[] array2 = gears;
		foreach (SourceGearToken t2 in array2)
		{
			Part(t2.Gear.Uid);
			Part(t2.Gear.GearId);
			Part(t2.Gear.Charges);
			Part(t2.Details);
		}
		SourcePermanentToken[] array3 = perms;
		foreach (SourcePermanentToken t3 in array3)
		{
			Part(t3.Card.Uid);
			Part(t3.Card.Def.Id);
			Part(t3.Card.Def.Name);
			Part(t3.Life);
			Part(t3.Details);
		}
		return new SourceTokenData(frame, slots, sets, gears, perms, mute, key.ToString());
		void Part(object? value)
		{
			string text = value?.ToString() ?? "";
			key.Append(text.Length).Append(':').Append(text);
		}
	}

	public static bool InventoryCurrent(SourceTokenFrame frame, RunState? current, long revision, bool blocked, long? currentEpoch = null)
	{
		if (((currentEpoch.HasValue && currentEpoch.Value != frame.Epoch) | blocked) || revision != frame.Revision || current?.Combat == null || frame.Board.Combat == null || current.Mana != frame.Board.Mana)
		{
			return false;
		}
		IReadOnlyList<GearInstance> captured = frame.Board.Gears ?? Array.Empty<GearInstance>();
		IReadOnlyList<GearInstance> live = current.Gears ?? Array.Empty<GearInstance>();
		if (captured.Count == live.Count)
		{
			return captured.Zip(live).All(((GearInstance First, GearInstance Second) p) => p.First.Uid == p.Second.Uid && p.First.GearId == p.Second.GearId && p.First.Charges == p.Second.Charges);
		}
		return false;
	}

	public static bool GearCurrent(SourceTokenFrame frame, RunState? current, long revision, bool blocked, int index, string uid, string id, long? currentEpoch = null)
	{
		if (InventoryCurrent(frame, current, revision, blocked, currentEpoch) && index >= 0 && index < current.Gears.Count && current.Gears[index].Uid == uid && current.Gears[index].GearId == id)
		{
			return Gears.GearBlockedReason(current.Combat, current.Mana.GetValueOrDefault(), current.Gears[index]) == null;
		}
		return false;
	}

	public static float PulsePhase(string uid)
	{
		uint h = 2166136261u;
		foreach (char c in uid)
		{
			h = (h ^ c) * 16777619;
		}
		return (float)(h % 10000) / 10000f;
	}

	public static float PulseAlpha(float progress, float phase)
	{
		return 0.75f + 0.25f * (float)Math.Sin((double)(progress + phase) * Math.PI * 2.0);
	}
}
