using System;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public sealed class SourceCastLight
{
	private SourceCastLightProfile _profile;

	private float _started;

	private bool _active;

	public Vector3 Position { get; private set; }

	public Vector3 ColorSrgb { get; private set; }

	public float Range => _profile.Range;

	public bool Begin(float now, SourceCastLightProfile profile, Vector3 feet, float height, Vector3 right, Vector3 up, Vector3 cameraPosition, Vector3 srgb)
	{
		if (!float.IsFinite(now) || !profile.Valid || !float.IsFinite(height) || height <= 0f || !feet.IsFinite() || !right.IsFinite() || !up.IsFinite() || !cameraPosition.IsFinite() || !srgb.IsFinite() || srgb.X < 0f || srgb.Y < 0f || srgb.Z < 0f)
		{
			return false;
		}
		Vector3 point = feet + up * (height * 0.5f) + right * (height * 0.28f);
		Vector3 toCamera = cameraPosition - point;
		if (toCamera.LengthSquared() > 1E-06f)
		{
			point += toCamera.Normalized() * profile.TowardCamera;
		}
		Position = point;
		ColorSrgb = srgb;
		_profile = profile;
		_started = now;
		_active = true;
		return true;
	}

	public float Strength(float now)
	{
		if (!_active || !float.IsFinite(now))
		{
			return 0f;
		}
		float age = now - _started;
		if (age < 0f || age >= _profile.Duration)
		{
			return 0f;
		}
		float u = age / _profile.Duration;
		float envelope = ((u <= _profile.Hold) ? 1f : ((1f - u) / Math.Max(0.0001f, 1f - _profile.Hold)));
		return _profile.Peak * envelope * envelope;
	}

	public void Clear()
	{
		_active = false;
	}

	public static bool TryColor(string? cardId, out Vector3 color)
	{
		color = cardId switch
		{
			"green_strike" => new Vector3(0.86f, 0.93f, 1f), 
			"blue_strike" => new Vector3(0.24f, 0.67f, 1f), 
			"green_finisher_stomp" => new Vector3(0.65f, 0.47f, 0.3f), 
			_ => default, 
		};
		switch (cardId)
		{
		case "green_strike":
		case "blue_strike":
		case "green_finisher_stomp":
			return true;
		default:
			return false;
		}
	}
}
