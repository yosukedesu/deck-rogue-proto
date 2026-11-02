using System;

namespace DeckRogue.GodotPort.Presentation;

public sealed class EnemyAttackArticulation
{
	private readonly EnemyAttackRigProfile _profile;

	private EnemyAttackRigProfile.Clip _clip;

	private readonly EnemyAttackRigProfile.Pose[] _from;

	private float _started;

	private int _remainingContacts;

	private int _lastContactOrdinal;

	private bool _rangedRelease;

	private bool _releasedContact;

	private float _releasedAt = -1f;

	public EnemyAttackRigProfile.Pose[] Joints { get; }

	public bool Active { get; private set; }

	public string Phase { get; private set; } = "idle";

	public string MoveId { get; private set; } = "";

	public int ExecutionIndex { get; private set; } = -1;

	public int ContactCount { get; private set; }

	public float LastContactAt { get; private set; } = -1f;

	public float ExpectedContactAt { get; private set; }

	public EnemyAttackArticulation(EnemyAttackRigProfile profile)
	{
		_profile = profile;
		_clip = profile.DefaultClip;
		Joints = new EnemyAttackRigProfile.Pose[profile.Joints.Length];
		_from = new EnemyAttackRigProfile.Pose[Joints.Length];
		Cancel();
	}

	public void Begin(float now, float expectedContact, int executionIndex, string? moveId, bool ranged = false)
	{
		if (float.IsFinite(now) && float.IsFinite(expectedContact))
		{
			Array.Copy(Joints, _from, Joints.Length);
			_clip = _profile.ForMove(moveId);
			MoveId = moveId ?? "";
			Active = true;
			Phase = "windup";
			_started = now;
			ExpectedContactAt = Math.Max(now, expectedContact);
			ExecutionIndex = executionIndex;
			ContactCount = 0;
			LastContactAt = -1f;
			_remainingContacts = 0;
			_lastContactOrdinal = -1;
			_rangedRelease = ranged && _profile.ReleaseMode != "contact";
			_releasedAt = -1f;
			_releasedContact = false;
		}
	}

	public void Preview(float now, float expectedContact, int executionIndex, string? moveId, bool ranged = false)
	{
		if (!Active || ExecutionIndex != executionIndex)
		{
			Begin(now, expectedContact, executionIndex, moveId, ranged);
		}
		else if (float.IsFinite(expectedContact) && LastContactAt < 0f)
		{
			ExpectedContactAt = Math.Max(now, expectedContact);
		}
		if (_rangedRelease && _releasedAt < 0f)
		{
			_releasedAt = now;
			Phase = "release";
			for (int i = 0; i < Joints.Length; i++)
			{
				Joints[i] = _clip.Strike[i];
			}
		}
	}

	public void Contact(float now, int executionIndex, string? moveId, int ordinal, int count)
	{
		if (!float.IsFinite(now))
		{
			return;
		}
		if (!Active || ExecutionIndex != executionIndex)
		{
			Begin(now, now, executionIndex, moveId);
		}
		if (ordinal <= _lastContactOrdinal)
		{
			return;
		}
		_lastContactOrdinal = ordinal;
		_releasedContact = _releasedAt >= 0f && ContactCount == 0;
		if (_releasedContact)
		{
			Array.Copy(Joints, _from, Joints.Length);
		}
		LastContactAt = now;
		ContactCount++;
		Phase = "contact";
		_remainingContacts = Math.Max(0, count - ordinal - 1);
		if (!_releasedContact)
		{
			for (int i = 0; i < Joints.Length; i++)
			{
				Joints[i] = _clip.Strike[i];
			}
		}
	}

	public void Sample(float now)
	{
		if (!Active || !float.IsFinite(now))
		{
			return;
		}
		if (LastContactAt >= 0f)
		{
			float age = Math.Max(0f, now - LastContactAt);
			if ((_remainingContacts == 0 && age >= _clip.Recovery) || (_remainingContacts > 0 && age > 0.28f))
			{
				Cancel();
				return;
			}
			EnemyAttackArticulation enemyAttackArticulation = this;
			string phase;
			if (age <= 0.018f)
			{
				phase = "contact";
			}
			else
			{
				phase = ((age < 0.075f) ? "recoil" : "recovery");
			}
			enemyAttackArticulation.Phase = phase;
			for (int i = 0; i < Joints.Length; i++)
			{
				float lag = _profile.Joints[i].Lag;
				float t = Math.Max(0f, age - lag * 0.5f);
				if (_releasedContact && _profile.ReleaseMode == "preview_recoil")
				{
					Joints[i] = EnemyAttackRigProfile.Pose.Lerp(_from[i], EnemyAttackRigProfile.Pose.Identity, EnemyAttackRigProfile.Smooth(age / 0.12f));
				}
				else if (_remainingContacts > 0 && age > 0.072f)
				{
					float q = EnemyAttackRigProfile.Smooth((age - 0.072f) / 0.048f);
					Joints[i] = EnemyAttackRigProfile.Pose.Lerp(_clip.Recoil[i], _clip.Strike[i], q * 0.92f);
					Phase = "repeat-windup";
				}
				else if (t <= 0.016f)
				{
					Joints[i] = _clip.Strike[i];
				}
				else if (t < 0.075f)
				{
					Joints[i] = EnemyAttackRigProfile.Pose.Lerp(_clip.Strike[i], _clip.Recoil[i], EnemyAttackRigProfile.Smooth((t - 0.016f) / 0.059f));
				}
				else if (t < 0.14f)
				{
					Joints[i] = EnemyAttackRigProfile.Pose.Lerp(_clip.Recoil[i], _clip.Settle[i], EnemyAttackRigProfile.Smooth((t - 0.075f) / 0.065f));
				}
				else
				{
					Joints[i] = EnemyAttackRigProfile.Pose.Lerp(_clip.Settle[i], EnemyAttackRigProfile.Pose.Identity, EnemyAttackRigProfile.Smooth((age - 0.14f) / Math.Max(0.01f, _clip.Recovery - 0.14f)));
				}
			}
		}
		else if (_releasedAt >= 0f)
		{
			float age2 = Math.Max(0f, now - _releasedAt);
			Phase = ((_profile.ReleaseMode == "preview_hold") ? "release-hold" : "release-recoil");
			for (int j = 0; j < Joints.Length; j++)
			{
				float t2 = Math.Max(0f, age2 - _profile.Joints[j].Lag * 0.5f);
				if (_profile.ReleaseMode == "preview_hold" || t2 < 0.016f)
				{
					Joints[j] = _clip.Strike[j];
				}
				else if (t2 < 0.075f)
				{
					Joints[j] = EnemyAttackRigProfile.Pose.Lerp(_clip.Strike[j], _clip.Recoil[j], EnemyAttackRigProfile.Smooth((t2 - 0.016f) / 0.059f));
				}
				else
				{
					Joints[j] = EnemyAttackRigProfile.Pose.Lerp(_clip.Recoil[j], EnemyAttackRigProfile.Pose.Identity, EnemyAttackRigProfile.Smooth((t2 - 0.075f) / 0.145f));
				}
			}
			if (now > ExpectedContactAt + 0.28f)
			{
				Cancel();
			}
		}
		else
		{
			float expectedStrike = ExpectedContactAt - (_rangedRelease ? 0.22f : 0f);
			float available = Math.Max(0.001f, expectedStrike - _started);
			float launch = Math.Min(0.18f, available * 0.55f);
			float release = Math.Min(0.09f, available * 0.45f);
			float until = expectedStrike - now;
			Phase = ((until > release) ? "windup" : "strike");
			for (int k = 0; k < Joints.Length; k++)
			{
				float lag2 = _profile.Joints[k].Lag;
				float load = EnemyAttackRigProfile.Smooth((now - _started - lag2) / Math.Max(0.01f, launch - lag2));
				EnemyAttackRigProfile.Pose ready = EnemyAttackRigProfile.Pose.Lerp(_from[k], _clip.Windup[k], load);
				float strike = EnemyAttackRigProfile.Smooth((release - until - lag2 * 0.25f) / Math.Max(0.01f, release - lag2 * 0.25f));
				Joints[k] = EnemyAttackRigProfile.Pose.Lerp(ready, _clip.Strike[k], strike * 0.92f);
			}
			if (now > ExpectedContactAt + 0.28f)
			{
				Cancel();
			}
		}
	}

	public void FreezeForDeath()
	{
		Active = false;
		Phase = "dead-frozen";
		_remainingContacts = 0;
	}

	public void Cancel()
	{
		Active = false;
		Phase = "idle";
		_remainingContacts = 0;
		for (int i = 0; i < Joints.Length; i++)
		{
			Joints[i] = EnemyAttackRigProfile.Pose.Identity;
		}
	}
}
