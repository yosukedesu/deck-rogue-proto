using System;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

internal sealed class NativeEffect
{
	public NativeBatch[] Batches = Array.Empty<NativeBatch>();

	public Action<double> Sample;

	public Action<Vector3>? ConfigureSource;

	public Action<bool>? ConfigureMirror;

	public Action<float>? ConfigureParticleGain;

	public Func<float>? VisualAge;

	public float TargetPeak = 0.35f;

	public float FloorPeak = 0.12f;

	public Func<float> Light = () => 0f;

	public Func<float> Front = () => 0f;

	public Func<float> Coverage = () => 1f;

	public string Shader = "";

	public string Palette = "";

	public string Texture = "";

	public string Lookup = "";

	public int Family;

	public string RouteFamily = "";
}
