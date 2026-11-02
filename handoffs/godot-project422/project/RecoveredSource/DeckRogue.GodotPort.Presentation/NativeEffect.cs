using System;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

internal sealed class NativeEffect
{
	public NativeBatch[] Batches = Array.Empty<NativeBatch>();

	public Action<double> Sample;

    // Only exact Mirror factory consumes this receipt-derived presentation phase.
    public Action<bool>? ConfigureBlueCommittedContact;
    public Action<bool>? ConfigureMirrorCommittedContact;
    public Action<bool>? ConfigureImpSparkCommittedContact;
    public Action<bool>? ConfigureColdSweepCommittedContact;
    public Action<bool>? ConfigureCanopyCommittedContact;
    public Action<bool>? ConfigureLightBoltCommittedContact;
    public Action<bool>? ConfigureLightBurstCommittedContact;
    public Action<bool>? ConfigureVineFormationCounterCommittedContact;
    public Action<bool>? ConfigureAetherLanceCommittedContact;
    public Action<bool>? ConfigureSparkBurstCommittedContact;
    public Action<bool>? ConfigureMomentumWhipCommittedContact;
    public Action<bool>? ConfigureCultistDarkBladeCommittedContact;
    public Action<bool>? ConfigureGreenGulpCommittedContact;
    public Action<bool>? ConfigureSpore385CommittedContact;

	public Action<Vector3>? ConfigureSource;

    public Action<bool>? ConfigureRemoteStrike;
    public Action<bool>? ConfigureSourceGesture;
    // Presentation-only deterministic variation captured once at effect Begin.
    public Action<float>? ConfigureVisualSeed;
    public Action<Vector3[],float[]>? ConfigureResolvedPath;
    public Action<int>? ConfigureWeaponFrame;
    public Action<Vector3>? ConfigureWeaponOrigin;
    public Action<float>? ConfigureWeaponTraceLength;
    public Action<float>? ConfigureStompTop;

	public Action<bool>? ConfigureMirror;

	public Action<float>? ConfigureParticleGain;

	public Func<float>? VisualAge;

	public float TargetPeak = 0.35f;

	public float FloorPeak = 0.12f;

	public Func<float> Light = () => 0f;

    public Func<float>? FloorLight;

    public Func<float> StageFocus = () => 0f;

	public Func<float> Front = () => 0f;

	public Func<float> Coverage = () => 1f;

	public string Shader = "";

	public string Palette = "";

	public string Texture = "";

	public string Lookup = "";

	public int Family;

	public string RouteFamily = "";
}
