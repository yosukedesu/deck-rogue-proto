using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public sealed class SourceDollSeats
{
	public readonly record struct Seat(int Index, bool Wide);

	public const int Capacity = 9;

	private readonly Dictionary<string, int> _seats = new Dictionary<string, int>(StringComparer.Ordinal);

	private readonly HashSet<string> _wide = new HashSet<string>(StringComparer.Ordinal);

	private readonly HashSet<string> _gone = new HashSet<string>(StringComparer.Ordinal);

	private readonly Dictionary<string, float> _missingAt = new Dictionary<string, float>(StringComparer.Ordinal);

	public bool IsGone(string uid)
	{
		return _gone.Contains(uid);
	}

	public bool TryGet(string uid, out Seat seat)
	{
		bool result = _seats.TryGetValue(uid, out var index);
		seat = new Seat(index, _wide.Contains(uid));
		return result;
	}

	public void BeginSync(IEnumerable<string> panelUids)
	{
		HashSet<string> panels = new HashSet<string>(panelUids, StringComparer.Ordinal);
		string[] array = _seats.Keys.Where((string item) => !panels.Contains(item)).ToArray();
		foreach (string uid in array)
		{
			_seats.Remove(uid);
		}
	}

	public bool TryAllocate(string uid, int baseWidth, int baseHeight, out Seat result)
	{
		result = default;
		if (_gone.Contains(uid))
		{
			return false;
		}
		if (TryGet(uid, out result))
		{
			return true;
		}
		HashSet<int> used = new HashSet<int>();
		foreach (KeyValuePair<string, int> kv in _seats)
		{
			used.Add(kv.Value);
			if (_wide.Contains(kv.Key))
			{
				used.Add(kv.Value + 1);
			}
		}
		int seat = -1;
		if (Math.Max(baseWidth, baseHeight) > 32)
		{
			for (int i = 0; i + 1 < 9; i++)
			{
				if (!used.Contains(i) && !used.Contains(i + 1) && (i + 1) % 5 != 0)
				{
					seat = i;
					_wide.Add(uid);
					break;
				}
			}
		}
		if (seat < 0)
		{
			for (int j = 0; j < 9; j++)
			{
				if (!used.Contains(j))
				{
					seat = j;
					break;
				}
			}
		}
		if (seat < 0)
		{
			return false;
		}
		_seats[uid] = seat;
		result = new Seat(seat, _wide.Contains(uid));
		return true;
	}

	public void ObserveMissing(IEnumerable<string> aliveUids, IEnumerable<string> panelUids, float now)
	{
		HashSet<string> alive = new HashSet<string>(aliveUids, StringComparer.Ordinal);
		foreach (string uid in panelUids)
		{
			if (!alive.Contains(uid) && !_gone.Contains(uid) && !_missingAt.ContainsKey(uid))
			{
				_missingAt[uid] = now + 0.6f;
			}
		}
	}

	public string[] DueMissing(float now)
	{
		return (from kv in _missingAt
			where now >= kv.Value
			select kv.Key).ToArray();
	}

	public bool Kill(string uid)
	{
		if (!_gone.Add(uid))
		{
			return false;
		}
		_missingAt.Remove(uid);
		return true;
	}

	public void Reset()
	{
		_seats.Clear();
		_wide.Clear();
		_gone.Clear();
		_missingAt.Clear();
	}

	public static Vector3 Feet(Seat seat, bool phone)
	{
		if (seat.Index < 0 || seat.Index >= 9 || (seat.Wide && (seat.Index + 1 >= 9 || (seat.Index + 1) % 5 == 0)))
		{
			throw new ArgumentOutOfRangeException("seat");
		}
		Vector3 a = Slot(seat.Index, phone);
		if (!seat.Wide)
		{
			return a;
		}
		return (a + Slot(seat.Index + 1, phone)) * 0.5f;
	}

	private static Vector3 Slot(int index, bool phone)
	{
		int j = index % 5;
		bool back = index >= 5;
		float t = -3.9f + (float)j * (phone ? 0.945f : 0.93f) + ((!back) ? 0f : (phone ? 0.5f : 0.45f));
		float s = ((index == 0 && !phone) ? 0.25f : ((j % 2 == 0) ? 0.9f : 0.25f)) + ((!back) ? 0f : (phone ? 2.8f : 2f));
		float a = 0.38397244f;
		return new Vector3(t * MathF.Cos(a) - s * MathF.Sin(a), 0f, 0f - (t * MathF.Sin(a) + s * MathF.Cos(a)));
	}
}
