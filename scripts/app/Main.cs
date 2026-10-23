using ManagedStartupLog = GodotPlugins.Game.ManagedStartupLog;
using System.Collections.Generic;
using System.ComponentModel;
using DeckRogue.GodotPort.Core;
using DeckRogue.GodotPort.Debugging;
using DeckRogue.GodotPort.Presentation;
using DeckRogue.GodotPort.UI;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.App;

[ScriptPath("res://scripts/app/Main.cs")]
public class Main : Node
{
	public new class MethodName : Node.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";

		public new static readonly StringName _ExitTree = "_ExitTree";
	}

	public new class PropertyName : Node.PropertyName
	{
		public static readonly StringName _session = "_session";

		public static readonly StringName _stage = "_stage";

		public static readonly StringName _ui = "_ui";
	}

	public new class SignalName : Node.SignalName
	{
	}

	private GameSession? _session;

	private BattleStage? _stage;

	private GameUI? _ui;

	private PresentationDiagnostics? _presentationDiagnostics;
	private MotionRegressionTrace? _motionRegressionTrace;
	private FreezeDiagnosticLog? _freezeDiagnostics;

	public override void _Ready()
	{
		ManagedStartupLog.ConfigureFromGodot();
		ManagedStartupLog.Mark("Main.Ready.enter");
		try { ReadyCore(); ManagedStartupLog.Mark("Main.Ready.return"); }
		catch (System.Exception error) { ManagedStartupLog.Failure("Main.Ready.exception", error); throw; }
	}

	private void ReadyCore()
	{
#if DEBUG
        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--motion-debug") >= 0) { EnemyMotionDebugMenu.Open(this, true); return; }
#endif
		try
		{
			_freezeDiagnostics = new FreezeDiagnosticLog(new FreezeDiagnosticFiles(ProjectSettings.GlobalizePath("user://diagnostics")));
			_freezeDiagnostics.Record("Main.Ready", new FreezeDiagnosticState(Target: -1));
		}
		catch { }
		ManagedStartupLog.Mark("Main.session_create");
		_session = new GameSession
		{
			Name = "Session",
			FreezeDiagnostics = _freezeDiagnostics
		};
		ManagedStartupLog.Mark("Main.session_add_begin");
		AddChild(_session, forceReadableName: false, InternalMode.Disabled);
		ManagedStartupLog.Mark(_session.IsInitialized ? "Main.session_initialized" : "Main.session_failed");
		if (!_session.IsInitialized)
		{
			Label failure = new Label
			{
				Text = "ゲームデータの読み込みに失敗しました。\n" + _session.InitializationError
			};
			AddChild(failure, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		ManagedStartupLog.Mark("Main.stage_create");
		_stage = new BattleStage
		{
			Name = "BattleStage"
		};
		ManagedStartupLog.Mark("Main.stage_add_begin");
		AddChild(_stage, forceReadableName: false, InternalMode.Disabled);
		ManagedStartupLog.Mark("Main.stage_add_return");
		_stage.Bind(_session);
		ManagedStartupLog.Mark("Main.stage_bound");
		CanvasLayer overlay = new CanvasLayer
		{
			Name = "Interface",
			Layer = 10
		};
		AddChild(overlay, forceReadableName: false, InternalMode.Disabled);
		ManagedStartupLog.Mark("Main.ui_create");
		_ui = new GameUI
		{
			Name = "GameUI"
		};
		ManagedStartupLog.Mark("Main.ui_add_begin");
		overlay.AddChild(_ui, forceReadableName: false, InternalMode.Disabled);
		ManagedStartupLog.Mark("Main.ui_add_return");
		_ui.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize);
		_ui.SetFreezeDiagnostics(_freezeDiagnostics);
		_session.FreezeStateProvider = () => _stage.CaptureFreezeDiagnosticState(_ui.CaptureFreezeDiagnosticState());
		_ui.SetPresentationGate(() => _stage.InputLocked);
		_ui.SetPresentationOwnership(_stage.GetEventChange, _stage.IsOrdinaryPresentation);
		_ui.SetSourceTokenPresentationRevision(() => _stage.ActiveCommandRevision);
		_ui.SetIncomingContactProjection(_stage.GetPresentedIncomingAmount);
		_ui.SetLeaderProjection(_stage.ProjectLeaderFeet);
		_ui.SetCardRetirement((string id) => _stage.GetCardRetirementSeconds(id));
		_ui.SetAdmittedAuthoredHandStyle(_stage.GetAdmittedAuthoredHandStyle);
		_stage.SetEnemyLedgerBounds(_ui.GetEnemyLedgerBounds);
        _ui.SetEnemyRosterLane(_stage.SetStyle04RosterLane);
		_stage.SetSourceCombatTextHost(overlay, () => _ui.SourceCombatTextMetrics);
		_stage.PresentationCompleted += _ui.PresentationCompleted;
		_stage.TargetSelected += _ui.SelectEnemy;
		_stage.EventPresented += _ui.PresentEvent;
		_stage.NumericContactPresented += _ui.PresentNumericContact;
		_ui.UiSoundRequested += _stage.PlayUiSound;
		_ui.SetReceiptValidator(_stage.ValidatePresentedReceipt);
		_ui.SetEnemyResolver(_stage.FindEnemyAt);
		_ui.SetEnemyProjection(_stage.ProjectEnemy);
        _ui.SetEnemyFootprintProjection(_stage.ProjectEnemyFootprint);
		_stage.SetCueOccluders(_ui.GetPaintedCombatBounds);
		_ui.SetPresentationProjection(_stage.GetPresentedVitals, () => _stage.DisplayedState);
		_ui.ScreenChanged += _stage.SetInterfaceRoute;
		ManagedStartupLog.Mark("Main.ui_bind_begin");
		_ui.Bind(_session);
		ManagedStartupLog.Mark("Main.ui_bound");
		_stage.FaultSnapshotSynchronizer = (run, revision) => _ui.SynchronizeFaultSnapshot(run, revision, _stage.PresentationFaultBlocked);
		_ui.QualityChanged += _stage.ApplyQuality;
		ManagedStartupLog.Mark("Main.quality_begin");
		_stage.ApplyQuality(_ui.GraphicsQuality);
		ManagedStartupLog.Mark("Main.quality_complete");
		_ui.SetEffectiveQuality(() => _stage.EffectiveQualityProfile);
		_presentationDiagnostics = new PresentationDiagnostics(this, _stage, () => new { _ui.EnemyHeadReadbacks, _ui.EnemyHeadCacheHits });
		_ui.SetPresentationDiagnostics(_presentationDiagnostics.SetEnabled, () => _presentationDiagnostics.Enabled, () => _presentationDiagnostics.LastFile);
		_presentationDiagnostics.Status += _ui.SetPresentationDiagnosticStatus;
		try { if (OS.HasFeature("motion_diagnostics")) _motionRegressionTrace = new MotionRegressionTrace(_stage); }
		catch (System.Exception error) { ManagedStartupLog.Failure("MotionTrace.install_failed", error); }
		ManagedStartupLog.Mark("Main.diagnostics_ready");
		AgentCapture capture = new AgentCapture
		{
			Name = "AgentCapture"
		};
		AddChild(capture, forceReadableName: false, InternalMode.Disabled);
		capture.Bind(_session, () => _stage.IsPresenting);
		ManagedStartupLog.Mark("Main.capture_ready");

#if DEBUG
        EnemyMotionDebugMenu.InstallEntry(this);
#endif
		GD.Print("DECKROGUE_RECOVERY_MODULE mvid=" + typeof(Main).Assembly.ManifestModule.ModuleVersionId);
		GD.Print("DECKROGUE_PORT_READY cards=" + _session.Catalog.Cards.Count + " leaders=" + _session.Catalog.Leaders.Count + " enemies=" + _session.Catalog.Enemies.Count);
		ManagedStartupLog.Mark("Main.port_ready");
		try {
			GetTree().Connect(SceneTree.SignalName.ProcessFrame, Callable.From(() => ManagedStartupLog.Mark("Main.first_process_frame")), (uint)GodotObject.ConnectFlags.OneShot);
			RenderingServer.Singleton.Connect(RenderingServer.SignalName.FramePostDraw, Callable.From(() => ManagedStartupLog.Mark("Main.first_frame_post_draw")), (uint)GodotObject.ConnectFlags.OneShot);
		} catch (System.Exception error) { ManagedStartupLog.Failure("Main.frame_probe_unavailable", error); }

	}

	public override void _ExitTree()
	{
		ManagedStartupLog.Mark("Main.ExitTree");
		_session?.RecordFreezeEvent("Main.ExitTree");
		_freezeDiagnostics?.Dispose();
		_presentationDiagnostics?.Dispose();
		_motionRegressionTrace?.Dispose();
		if (_stage != null && _ui != null)
		{
			_stage.FaultSnapshotSynchronizer = null;
			_stage.SetCueOccluders(null);
			_stage.PresentationCompleted -= _ui.PresentationCompleted;
			_stage.TargetSelected -= _ui.SelectEnemy;
			_stage.EventPresented -= _ui.PresentEvent;
			_stage.NumericContactPresented -= _ui.PresentNumericContact;
			_ui.UiSoundRequested -= _stage.PlayUiSound;
			_ui.QualityChanged -= _stage.ApplyQuality;
			_ui.ScreenChanged -= _stage.SetInterfaceRoute;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(2)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName._Ready && args.Count == 0)
		{
			_Ready();
			ret = default;
			return true;
		}
		if (method == MethodName._ExitTree && args.Count == 0)
		{
			_ExitTree();
			ret = default;
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName._Ready)
		{
			return true;
		}
		if (method == MethodName._ExitTree)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName._session)
		{
			_session = VariantUtils.ConvertTo<GameSession>(in value);
			return true;
		}
		if (name == PropertyName._stage)
		{
			_stage = VariantUtils.ConvertTo<BattleStage>(in value);
			return true;
		}
		if (name == PropertyName._ui)
		{
			_ui = VariantUtils.ConvertTo<GameUI>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName._session)
		{
			value = VariantUtils.CreateFrom(in _session);
			return true;
		}
		if (name == PropertyName._stage)
		{
			value = VariantUtils.CreateFrom(in _stage);
			return true;
		}
		if (name == PropertyName._ui)
		{
			value = VariantUtils.CreateFrom(in _ui);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, PropertyName._session, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._stage, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._ui, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName._session, Variant.From(in _session));
		info.AddProperty(PropertyName._stage, Variant.From(in _stage));
		info.AddProperty(PropertyName._ui, Variant.From(in _ui));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName._session, out var _value__session))
		{
			_session = _value__session.As<GameSession>();
		}
		if (info.TryGetProperty(PropertyName._stage, out var _value__stage))
		{
			_stage = _value__stage.As<BattleStage>();
		}
		if (info.TryGetProperty(PropertyName._ui, out var _value__ui))
		{
			_ui = _value__ui.As<GameUI>();
		}
	}
}
