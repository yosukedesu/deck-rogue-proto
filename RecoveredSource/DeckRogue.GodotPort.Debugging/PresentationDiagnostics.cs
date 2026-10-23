using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DeckRogue.GodotPort.Presentation;
using Godot;
using Newtonsoft.Json;

namespace DeckRogue.GodotPort.Debugging;

public sealed class PresentationDiagnostics : IDisposable
{
	private readonly record struct Pose(string Name, int Frame, int Starts, int Motion, int Recoil, bool Paused);

	private readonly record struct Sample(int N, int RenderFrame, double IntervalMs, float Clock, string Quality, bool Busy, bool Paused, bool Focused);

	private const int Capacity = 36000;

	private const int MaxEvents = 16000;

	private readonly BattleStage stage;

	private readonly Node root;

	private readonly Func<object> extra;

	private readonly List<object> events = new List<object>();

	private readonly Dictionary<ulong, Pose> poses = new Dictionary<ulong, Pose>();

	private readonly List<Sample> samples = new List<Sample>();

	private long started;

	private long last;

	private string renderer;

	private string driver;

	private string adapter;

	private bool limited;

	private int sampleIndex;

	public bool Enabled { get; private set; }

	public string LastFile { get; private set; } = "";

	public event Action<string> Status;

	public PresentationDiagnostics(Node root, BattleStage stage, Func<object> extra = null)
	{
		this.root = root;
		this.stage = stage;
		this.extra = extra;
	}

	public void SetEnabled(bool on)
	{
		if (on != Enabled)
		{
			if (on)
			{
				Start();
			}
			else
			{
				Stop();
			}
		}
	}

	private void Start()
	{
		samples.Clear();
		events.Clear();
		poses.Clear();
		limited = false;
		sampleIndex = 0;
		renderer = RenderingServer.GetCurrentRenderingMethod();
		driver = RenderingServer.GetCurrentRenderingDriverName();
		adapter = RenderingServer.GetVideoAdapterName();
		started = (last = Stopwatch.GetTimestamp());
		Enabled = true;
		RenderingServer.FramePostDraw += SampleFrame;
		Status?.Invoke("描画診断: 記録中（画像取得なし）");
	}

	private void SampleFrame()
	{
		if (!Enabled)
		{
			return;
		}
		try
		{
			long now = Stopwatch.GetTimestamp();
			double dt = (double)(now - last) * 1000.0 / (double)Stopwatch.Frequency;
			last = now;
			bool paused = false;
			SampleActors(stage.GetNodeOrNull<Node3D>("Combatants"), ref paused);
			SampleActors(stage.GetNodeOrNull<Node3D>("RetiredCombatants"), ref paused);
			samples.Add(new Sample(sampleIndex++, Godot.Engine.GetFramesDrawn(), dt, stage.PresentationTime, stage.EffectiveQualityProfile, stage.IsPresenting, paused, DisplayServer.WindowIsFocused()));
			if (samples.Count >= 36000)
			{
				Stop();
			}
		}
		catch (Exception ex)
		{
			Enabled = false;
			RenderingServer.FramePostDraw -= SampleFrame;
			Status?.Invoke("描画診断: 記録を停止しました");
			GD.PushWarning("Presentation diagnostics stopped: " + ex.Message);
		}
	}

	private void SampleActors(Node3D container, ref bool anyPaused)
	{
		if (container == null)
		{
			return;
		}
		for (int i = 0; i < container.GetChildCount(); i++)
		{
			if (!(container.GetChild(i) is BattleActor a))
			{
				continue;
			}
			bool paused = a.Motion.IsPaused(stage.PresentationTime);
			anyPaused |= paused;
			Pose current = new Pose(a.PoseName, a.PoseFrame, a.PoseStartCount, a.Motion.MotionVersion, a.Motion.RecoilVersion, paused);
			ulong id = a.GetInstanceId();
			if (!poses.TryGetValue(id, out var prior) || prior != current)
			{
				if (events.Count < 16000)
				{
					events.Add(new
					{
						frame = sampleIndex,
						clock = stage.PresentationTime,
						actor = a.DefinitionId,
						instance = id,
						pose = current.Name,
						spriteFrame = current.Frame,
						poseStarts = current.Starts,
						restart = (prior.Starts != current.Starts),
						motion = current.Motion,
						recoil = current.Recoil,
						hitpause = paused,
						pauseRemaining = a.Motion.PauseRemaining(stage.PresentationTime)
					});
				}
				else
				{
					limited = true;
				}
				poses[id] = current;
			}
		}
	}

	private void Stop()
	{
		if (!Enabled)
		{
			return;
		}
		Enabled = false;
		RenderingServer.FramePostDraw -= SampleFrame;
		try
		{
			double[] times = (from x in samples.Skip(1)
				select x.IntervalMs into x
				orderby x
				select x).ToArray();
			double[] array = new double[8]
			{
				8.34,
				11.12,
				16.68,
				22.23,
				33.35,
				50.0,
				100.0,
				double.PositiveInfinity
			};
			double lower = 0.0;
			List<object> bins = new List<object>();
			double[] array2 = array;
			foreach (double upper in array2)
			{
				bins.Add(new
				{
					greaterThanMs = lower,
					upToMs = (double.IsFinite(upper) ? new double?(upper) : ((double?)null)),
					count = times.Count((double x) => x > lower && x <= upper)
				});
				lower = upper;
			}
			string folder = ProjectSettings.GlobalizePath("user://presentation-diagnostics");
			Directory.CreateDirectory(folder);
			LastFile = Path.Combine(folder, "presentation-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + ".json");
			File.WriteAllText(LastFile, JsonConvert.SerializeObject(new
			{
				schema = "deckrogue-presentation-diagnostics/1",
				moduleMvid = typeof(PresentationDiagnostics).Assembly.ManifestModule.ModuleVersionId,
				recordedUtc = DateTime.UtcNow.ToString("O"),
				renderer = renderer,
				driver = driver,
				adapter = adapter,
				selectedQuality = stage.QualityProfile,
				effectiveQuality = stage.EffectiveQualityProfile,
				qualityFrames = from s in samples
					group s by s.Quality into g
					select new
					{
						quality = g.Key,
						frames = g.Count()
					},
				fixedFps = Enumerable.Contains(OS.GetCmdlineArgs(), "--fixed-fps"),
				captureActive = Enumerable.Contains(OS.GetCmdlineUserArgs(), "--unity-parity-sequence"),
				manualQaFixture = Enumerable.Contains(OS.GetCmdlineUserArgs(), "--unity-ui-manual"),
				frameCount = samples.Count,
				intervals = new
				{
					meanMs = ((times.Length != 0) ? times.Average() : 0.0),
					p50Ms = P(0.5),
					p95Ms = P(0.95),
					p99Ms = P(0.99),
					maxMs = P(1.0),
					histogram = bins
				},
				spriteEventLimitReached = limited,
				extra = extra?.Invoke(),
				frames = samples,
				spriteTransitions = events,
				scope = "Natural host render-completion intervals. Instrumentation has overhead; no screenshot/texture readback. Intentional stepped sprite frames and hit-pause are logged separately. No rule state or personal data is exported."
			}, Formatting.Indented));
			Status?.Invoke("描画診断: 保存しました（" + samples.Count + "フレーム）");
			GD.Print("PRESENTATION_DIAGNOSTICS_SAVED " + LastFile);
			double P(double q)
			{
				if (times.Length != 0)
				{
					return times[Math.Min(times.Length - 1, (int)Math.Floor(q * (double)(times.Length - 1)))];
				}
				return 0.0;
			}
		}
		catch (Exception ex)
		{
			Status?.Invoke("描画診断: 保存できませんでした");
			GD.PushWarning("Presentation diagnostics save failed: " + ex.Message);
		}
	}

	public void Dispose()
	{
		if (Enabled)
		{
			Stop();
		}
	}
}

// Diagnostic-only observer: no timers, queues, poses, resources or game state are mutated.
// Activated by the exported motion_diagnostics feature, never by normal game settings.
internal sealed class MotionTraceWriter
{
    internal const int MaxRecords = 2048, MaxBytes = 1024 * 1024, MaxLineBytes = 2048;
    private const int StopReserve = 512;
    private readonly Action<string> sink;
    internal int Records { get; private set; }
    internal int Bytes { get; private set; }
    internal bool Stopped { get; private set; }
    internal MotionTraceWriter(Action<string> sink) { this.sink = sink; }
    internal bool Write(object value)
    {
        if (Stopped) return false;
        try {
            string line = "DR_MOTION " + JsonConvert.SerializeObject(new { schema=1, seq=Records, data=value });
            int size = System.Text.Encoding.UTF8.GetByteCount(line) + 1;
            if (Records >= MaxRecords-1 || size > MaxLineBytes || Bytes+size+StopReserve > MaxBytes) {
                Stop("limit"); return false;
            }
            sink(line); Records++; Bytes+=size; return true;
        } catch { Stopped=true; return false; } // A broken diagnostic sink cannot affect presentation.
    }
    internal void Stop(string reason)
    {
        if (Stopped) return;
        Stopped=true;
        try {
            string line = "DR_MOTION " + JsonConvert.SerializeObject(new { schema=1, seq=Records, data=new { kind="stop", reason, records=Records, bytes=Bytes } });
            int size = System.Text.Encoding.UTF8.GetByteCount(line)+1;
            if (Records<MaxRecords && size<=MaxLineBytes && Bytes+size<=MaxBytes) { sink(line); Records++; Bytes+=size; }
        } catch { }
    }
}

internal sealed class MotionRegressionTrace : IDisposable
{
    internal const double MaxSeconds = 300;
    private readonly BattleStage stage;
    private readonly MotionTraceWriter writer;
    private readonly bool renderHook;
    private readonly Func<long> timestamp;
    private readonly long started;
    private long last, lastStall;
    private int frame, contactSerial;
    private bool disposed;
    private readonly Dictionary<ulong, ActorState> states = new();
    private StageState? previousStage;
    private readonly record struct ActorState(string Pose, int LegacyFrame, int Starts, int Motion,
        int Recoil, bool Recoiling, bool Paused, bool Authored, string Clip, int ArtFrame,
        string Reason, int Owner, int Execution, int AcceptedContacts, int LastOrdinal,
        string Move, string Phase);
    private readonly record struct StageState(long Revision, bool Presenting, bool Locked,
        int Pending, int Effects, int Residual, bool Fault);
    internal MotionRegressionTrace(BattleStage stage, Action<string>? sink=null, bool renderHook=true, Func<long>? timestamp=null)
    {
        this.stage=stage; this.renderHook=renderHook; this.timestamp=timestamp ?? Stopwatch.GetTimestamp; started=this.timestamp(); writer=new MotionTraceWriter(sink ?? (line=>GD.Print(line)));
        last=started;
        stage.NumericContactPresented+=Contact;
        stage.EventPresented+=Event;
        stage.PresentationCompleted+=Complete;
        if (renderHook) RenderingServer.FramePostDraw+=Sample;
        Emit(new { kind="start", moduleMvid=typeof(MotionRegressionTrace).Assembly.ManifestModule.ModuleVersionId,
            maxRecords=MotionTraceWriter.MaxRecords, maxBytes=MotionTraceWriter.MaxBytes,
            maxLineBytes=MotionTraceWriter.MaxLineBytes, maxSeconds=MaxSeconds,
            scope="Visual diagnostics only; no screenshots, save data or game-state mutation." });
    }
    private void Emit(object value) { if (!writer.Write(value)) Dispose(); }
    private void Contact(DeckRogue.Engine.Generated.GameEvent e)
    {
        if (disposed || e is not DeckRogue.Engine.Generated.GameEvent_DamageDealt hit) return;
        try {
            var c=stage.CaptureMotionDiagnosticContact();
            Emit(new { kind="contact", frame, clock=stage.PresentationTime, serial=++contactSerial,
                revision=stage.ActiveCommandRevision, beat=c.Beat, ordinal=c.Ordinal,
                source=hit.Source, definition=hit.PresentationSource?.DefinitionId, enemy=hit.EnemyIndex,
                hpLoss=hit.HpLoss, amount=hit.Amount });
        } catch { Dispose(); }
    }
    private void Event(DeckRogue.Engine.Generated.GameEvent e)
    {
        if (disposed || e is not DeckRogue.Engine.Generated.GameEvent_CardPlayed card) return;
        try { Emit(new { kind="card", frame, clock=stage.PresentationTime, revision=stage.ActiveCommandRevision, card=card.CardId }); }
        catch { Dispose(); }
    }
    private void Complete()
    {
        if (disposed) return;
        try { Emit(new { kind="complete", frame, clock=stage.PresentationTime, contacts=contactSerial,
            effects=stage.ActiveEffectCount, residual=stage.ResidualEffectCount, pending=stage.PendingActorContacts }); }
        catch { Dispose(); }
    }
    internal void Sample()
    {
        if (disposed) return;
        try {
            long now=timestamp();
            double elapsed=(now-started)/(double)Stopwatch.Frequency;
            if (elapsed>=MaxSeconds) { writer.Stop("time-limit"); Dispose(); return; }
            double interval=(now-last)*1000.0/Stopwatch.Frequency; last=now; frame++;
            var currentStage=new StageState(stage.ActiveCommandRevision, stage.IsPresenting, stage.InputLocked,
                stage.PendingActorContacts, stage.ActiveEffectCount, stage.ResidualEffectCount, stage.PresentationFaulted);
            if (previousStage!=currentStage) { previousStage=currentStage; Emit(new { kind="stage", frame, clock=stage.PresentationTime, intervalMs=interval, state=currentStage }); }
            if (disposed) return;
            if (interval>=100 && (now-lastStall)/(double)Stopwatch.Frequency>=1) {
                lastStall=now; Emit(new { kind="slow-frame", frame, clock=stage.PresentationTime, intervalMs=interval });
            }
            var seen=new HashSet<ulong>();
            foreach (string name in new[]{"Combatants", "RetiredCombatants"}) {
                var container=stage.GetNodeOrNull<Node3D>(name);
                if (container==null) continue;
                foreach (Node node in container.GetChildren()) {
                    if (node is not BattleActor actor) continue;
                    if (seen.Count>=64) { writer.Stop("actor-limit"); Dispose(); return; }
                    ulong id=actor.GetInstanceId(); seen.Add(id);
                    var art=actor.AuthoredFrameSample; var motion=actor.Style04MotionSample;
                    var current=new ActorState(actor.PoseName, actor.PoseFrame, actor.PoseStartCount,
                        actor.Motion.MotionVersion, actor.Motion.RecoilVersion, actor.Motion.RecoilActive,
                        actor.Motion.IsPaused(stage.PresentationTime), actor.AuthoredFramesEnabled,
                        art.Clip, art.FrameIndex, art.Reason, art.OwnerVersion, art.ExecutionIndex,
                        art.AcceptedContacts, art.LastOrdinal, motion.MoveId, motion.Phase);
                    bool first=!states.TryGetValue(id,out var prior);
                    if (first || prior!=current) {
                        states[id]=current;
                        Emit(new { kind="actor", frame, clock=stage.PresentationTime, intervalMs=interval,
                            actor=actor.DefinitionId, instance=id, first, state=current,
                            manifest=actor.AuthoredManifestSha256,
                            source=first?actor.RosterLayoutTexture?.ResourcePath:null,
                            textureWidth=actor.Sprite.Texture?.GetWidth(), textureHeight=actor.Sprite.Texture?.GetHeight(),
                            pauseRemaining=actor.Motion.PauseRemaining(stage.PresentationTime),
                            x=actor.Position.X, y=actor.Position.Y, z=actor.Position.Z });
                        if (disposed) return;
                    }
                }
            }
            foreach (ulong id in states.Keys.Where(x=>!seen.Contains(x)).ToArray()) states.Remove(id);
        } catch { writer.Stop("observer-error"); Dispose(); }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed=true;
        if (renderHook) RenderingServer.FramePostDraw-=Sample;
        stage.NumericContactPresented-=Contact;
        stage.EventPresented-=Event;
        stage.PresentationCompleted-=Complete;
        writer.Stop("closed"); states.Clear();
    }
}
