#nullable enable
// dot の Godot 版の NativeBatch.cs をそのまま。演出の1層 = 頂点の束 (位置・UV・不透明度・明るさ・役割の色) を関数で引く。
using System;
using DeckRogue.Game.Hd2dMath;

namespace DeckRogue.Game.Hd2d
{
internal sealed class NativeBatch
{
	// Optional per-layer authored RGBA material; empty preserves the existing effect material.
	public string Texture = "";

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
}
