using System;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

internal sealed class NativeBatch
{
	public int Count;

	public int Kind;

	public int Space;

	public int[] Indices = Array.Empty<int>();

	public Func<int, Vector3> Position;

	public Func<int, Vector2> Uv;

	public Func<int, float> Alpha;

	public Func<int, float> Gain;

	public Func<int, bool> Particle = (int _) => false;

	public Func<int, int> Role = (int _) => -1;

	public Func<int, Vector2> Effect = (int _) => Vector2.One;

	public Func<int, Vector3> Normal = (int _) => Vector3.Back;

	public Func<int, Vector3> Anchor = (int _) => Vector3.Zero;
}
