using System;
using System.Collections.Generic;
using System.Linq;

namespace DeckRogue.GodotPort.Presentation;

public sealed class CombatActionQueue
{
	public sealed class ActionToken
	{
		internal int Generation;

		internal HashSet<int> Contacts = new HashSet<int>();

		internal Action<ActionToken> Launch;

		internal bool Started;

		internal bool Released;

		internal float ReadyAt;

		internal int ReleaseFrame;

		public int PendingContacts => Contacts.Count;
	}

	private readonly Queue<ActionToken> pending = new Queue<ActionToken>();

	private ActionToken? active;

	public int Generation { get; private set; }

	public int QueuedCount => pending.Count;

	public int PendingContacts => (active?.Contacts.Count ?? 0) + pending.Sum((ActionToken x) => x.Contacts.Count);

	public bool Busy
	{
		get
		{
			if (active == null)
			{
				return pending.Count > 0;
			}
			return true;
		}
	}

	public ActionToken Enqueue(float now, IEnumerable<int> contacts, Action<ActionToken> launch)
	{
		ActionToken t = new ActionToken
		{
			Generation = Generation,
			Contacts = new HashSet<int>(contacts),
			Launch = launch,
			ReadyAt = now,
			ReleaseFrame = -1
		};
		pending.Enqueue(t);
		return t;
	}

	public bool ContactComplete(ActionToken t, int contact, float now, int frame, float pause = 0f)
	{
		if (t.Generation != Generation || active != t || !t.Contacts.Remove(contact))
		{
			return false;
		}
		if (t.Contacts.Count > 0)
		{
			return false;
		}
		t.ReadyAt = now + Math.Max(0f, pause);
		t.ReleaseFrame = frame;
		return true;
	}

	public void Hold(ActionToken t, float until, int frame)
	{
		if (t.Generation == Generation && active == t)
		{
			t.ReadyAt = Math.Max(t.ReadyAt, until);
			t.ReleaseFrame = Math.Max(t.ReleaseFrame, frame);
		}
	}

	public void Advance(float now, int frame)
	{
		ActionToken a = active;
		if (a != null)
		{
			if (a.Contacts.Count > 0 || now + 1E-06f < a.ReadyAt || frame <= a.ReleaseFrame)
			{
				return;
			}
			a.Released = true;
			active = null;
		}
		if (pending.TryDequeue(out ActionToken t))
		{
			active = t;
			t.Started = true;
			t.ReleaseFrame = frame;
			t.ReadyAt = now;
			t.Launch(t);
		}
	}

	public void Cancel()
	{
		Generation++;
		pending.Clear();
		active = null;
	}
}
