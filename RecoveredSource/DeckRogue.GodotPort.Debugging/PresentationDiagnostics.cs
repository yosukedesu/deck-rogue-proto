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
