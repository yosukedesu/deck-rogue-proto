using System;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public sealed class SourceEnemyHudSnapshot
{
	public GameState State { get; }

	public GameState ForecastEligibilityState { get; }

	public long Epoch { get; }

	public long BoardRevision { get; }

	private SourceEnemyHudSnapshot(GameState state, GameState forecastEligibilityState, long epoch, long boardRevision)
	{
		State = state ?? throw new ArgumentNullException("state");
		ForecastEligibilityState = forecastEligibilityState;
		Epoch = epoch;
		BoardRevision = boardRevision;
	}

	public static SourceEnemyHudSnapshot Capture(GameState state, long epoch, long boardRevision)
	{
		return new SourceEnemyHudSnapshot(state, state, epoch, boardRevision);
	}

	public static SourceEnemyHudSnapshot SelectForBuild(SourceEnemyHudSnapshot? previous, GameState selectedBoard, GameState? acceptedCurrentBoard, long epoch, long activeRevision, bool inputFenced)
	{
		if ((object)selectedBoard == null)
		{
			throw new ArgumentNullException("selectedBoard");
		}
		if (inputFenced && previous != null && previous.Epoch == epoch && SameCombatLineage(previous.State, selectedBoard))
		{
			return new SourceEnemyHudSnapshot(previous.State, previous.ForecastEligibilityState, epoch, previous.BoardRevision);
		}
		GameState group = ((!inputFenced && SameCombatLineage(selectedBoard, acceptedCurrentBoard)) ? acceptedCurrentBoard : selectedBoard);
		return new SourceEnemyHudSnapshot(selectedBoard, group, epoch, activeRevision);
	}

	private static bool SameCombatLineage(GameState selected, GameState? accepted)
	{
		if ((object)accepted == null)
		{
			return false;
		}
		if ((object)selected == accepted)
		{
			return true;
		}
		if (selected.EventLog.Count > 0 && accepted.EventLog.Count > 0)
		{
			return (object)selected.EventLog[0] == accepted.EventLog[0];
		}
		return false;
	}

	public bool IsCurrent(SourceEnemyHudSnapshot? current, long epoch, long boardRevision)
	{
		if (this == current && Epoch == epoch)
		{
			return BoardRevision == boardRevision;
		}
		return false;
	}

	public bool TryRead(SourceEnemyHudSnapshot? current, long epoch, long boardRevision, int enemyIndex, string enemyId, out GameState? state, out EnemyState? enemy)
	{
		state = null;
		enemy = null;
		if (!IsCurrent(current, epoch, boardRevision) || enemyIndex < 0 || enemyIndex >= State.Enemies.Count || string.IsNullOrEmpty(enemyId) || State.Enemies[enemyIndex].EnemyId != enemyId)
		{
			return false;
		}
		state = State;
		enemy = State.Enemies[enemyIndex];
		return true;
	}
}
