using ManagedStartupLog = GodotPlugins.Game.ManagedStartupLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Debugging;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Core;

[ScriptPath("res://scripts/core/GameSession.cs")]
public class GameSession : Node
{
	public new class MethodName : Node.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";

		public static readonly StringName ForwardCleared = "ForwardCleared";

		public static readonly StringName ForwardDiagnostic = "ForwardDiagnostic";

		public static readonly StringName ExportText = "ExportText";

		public static readonly StringName ExportSave = "ExportSave";

		public static readonly StringName SetPresentationBusy = "SetPresentationBusy";

		public static readonly StringName ClearPresentation = "ClearPresentation";

		public new static readonly StringName _Notification = "_Notification";

		public new static readonly StringName _ExitTree = "_ExitTree";

		public static readonly StringName FlushIfActive = "FlushIfActive";
	}

	public new class PropertyName : Node.PropertyName
	{
		public static readonly StringName DataDirectory = "DataDirectory";

		public static readonly StringName SaveDirectory = "SaveDirectory";

		public static readonly StringName AutoSave = "AutoSave";

		public static readonly StringName InitializationError = "InitializationError";

		public static readonly StringName IsInitialized = "IsInitialized";

		public static readonly StringName Revision = "Revision";

		public static readonly StringName PresentationBusy = "PresentationBusy";

		public static readonly StringName HasSave = "HasSave";

		public static readonly StringName CanContinue = "CanContinue";

		public static readonly StringName HasRetainedReport = "HasRetainedReport";

		public static readonly StringName PendingPresentationCount = "PendingPresentationCount";

		public static readonly StringName _ownerThread = "_ownerThread";
	}

	public new class SignalName : Node.SignalName
	{
	}

	private readonly int _ownerThread = System.Environment.CurrentManagedThreadId;

	private SessionController? _controller;

	public FreezeDiagnosticLog? FreezeDiagnostics { get; set; }
	public Func<FreezeDiagnosticState>? FreezeStateProvider { get; set; }
	private string? _freezeCardId;
	private int _freezeTarget = -1;

	public void RecordFreezeEvent(string method, bool? success = null, Exception? exception = null)
	{
		try
		{
			if (FreezeDiagnostics == null) return;
			FreezeDiagnosticState state = default;
			try { state = FreezeStateProvider?.Invoke() ?? default; } catch { }
			state = state with { CardId = state.CardId ?? _freezeCardId,
				Target = state.CardId != null ? state.Target : _freezeTarget,
				Phase = CurrentRun?.Phase, CombatPhase = CurrentRun?.Combat?.Phase,
				PendingPresentation = PendingPresentationCount, Busy = PresentationBusy, Revision = Revision };
			FreezeDiagnostics.Record(method, state, success, exception);
		}
		catch { }
	}

	private SessionResult DispatchTracked(Func<SessionController, SessionResult> call, Command? command)
	{
		try
		{
			_freezeCardId = null;
			_freezeTarget = -1;
			if (command is Command_PlayCard play)
			{
				_freezeTarget = play.TargetIndex ?? -1;
				if (CurrentRun?.Combat?.Player?.Hand != null)
					foreach (CardInstance card in CurrentRun.Combat.Player.Hand)
						if (card.Uid == play.CardUid) { _freezeCardId = card.Def.Id; break; }
			}
		}
		catch { }
		RecordFreezeEvent("GameSession.Dispatch.begin");
		try
		{
			SessionResult result = Apply(call);
			RecordFreezeEvent("GameSession.Dispatch.end", result.Success);
			return result;
		}
		catch (Exception ex)
		{
			RecordFreezeEvent("GameSession.Dispatch.exception", false, ex);
			throw;
		}
	}

	[Export(PropertyHint.None, "")]
	public string DataDirectory { get; set; } = "res://data";

	[Export(PropertyHint.None, "")]
	public string SaveDirectory { get; set; } = "user://save";

	[Export(PropertyHint.None, "")]
	public bool AutoSave { get; set; } = true;

	public string? InitializationError { get; private set; }

	public bool IsInitialized => _controller != null;

	public RunState? CurrentRun => _controller?.CurrentRun;

	public long Revision => _controller?.Revision ?? 0;

	public bool PresentationBusy => _controller?.PresentationBusy ?? false;

	public bool HasSave => _controller?.HasSave ?? false;

	public bool CanContinue => _controller?.CanContinue ?? false;

	public bool HasRetainedReport => _controller?.HasRetainedReport ?? false;

	public int PendingPresentationCount => _controller?.PendingPresentationCount ?? 0;

	public ContentCatalog Catalog { get; } = new ContentCatalog();

	public IReadOnlyList<BattleArchive> History => _controller?.History ?? Array.Empty<BattleArchive>();

	public IReadOnlyList<PlayNote> Notes => _controller?.Notes ?? Array.Empty<PlayNote>();

	public IReadOnlyList<RunChoice> Choices => _controller?.Choices ?? Array.Empty<RunChoice>();

	public RunJournal? Journal => _controller?.Journal;

	public JToken? LegacyUnityAnnotations => _controller?.LegacyUnityAnnotations;

	public JToken? MapAnnotations => _controller?.MapAnnotations;

	public event Action<SessionChange>? StateChanged;

	public event Action<long>? RunCleared;

	public event Action<string>? Diagnostic;

	public override void _Ready()
	{
		Initialize();
	}

	public SessionResult Initialize()
	{
		if (System.Environment.CurrentManagedThreadId != _ownerThread)
		{
			return SessionResult.Fail("Session and save operations must run on the main thread.");
		}
		if (_controller != null)
		{
			return SessionResult.Ok();
		}
		try
		{
			ManagedStartupLog.Mark("Session.Initialize.begin");
			GodotSessionFiles files = new GodotSessionFiles();
			if (!Content.IsLoaded)
			{
				ManagedStartupLog.Mark("Session.content_begin");
				Content.LoadFrom((string name) => files.Read(DataDirectory.TrimEnd('/') + "/" + name));
				ManagedStartupLog.Mark("Session.content_complete");
			}
			_controller = new SessionController(new SessionSaveStore(files, SaveDirectory))
			{
				AutoSave = AutoSave
			};
			_controller.FreezeException = (method, error) => RecordFreezeEvent(method, false, error);
			_controller.StateChanged += ForwardChange;
			_controller.RunCleared += ForwardCleared;
			_controller.Diagnostic += ForwardDiagnostic;
			InitializationError = null;
			ManagedStartupLog.Mark("Session.Initialize.complete");
			return SessionResult.Ok();
		}
		catch (Exception ex)
		{
			ManagedStartupLog.Failure("Session.Initialize.exception", ex);
			RecordFreezeEvent("GameSession.Initialize", false, ex);
			InitializationError = ex.Message;
			GD.PushError("DeckRogue content/session initialization failed: " + ex.Message);
			return SessionResult.Fail(ex.Message);
		}
	}

	private void ForwardChange(SessionChange change)
	{
		RecordFreezeEvent("GameSession.ForwardChange");
		if (StateChanged == null)
		{
			return;
		}
		Delegate[] invocationList = StateChanged.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			Action<SessionChange> listener = (Action<SessionChange>)invocationList[i];
			try
			{
				listener(change);
			}
			catch (Exception ex)
			{
				RecordFreezeEvent("GameSession.ForwardChange.listener", false, ex);
				ForwardDiagnostic("Presentation listener failed: " + ex.Message);
			}
		}
	}

	private void ForwardCleared(long revision)
	{
		if (RunCleared == null)
		{
			return;
		}
		Delegate[] invocationList = RunCleared.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			Action<long> listener = (Action<long>)invocationList[i];
			try
			{
				listener(revision);
			}
			catch (Exception ex)
			{
				RecordFreezeEvent("GameSession.ForwardCleared.listener", false, ex);
				ForwardDiagnostic("Session clear listener failed: " + ex.Message);
			}
		}
	}

	private void ForwardDiagnostic(string text)
	{
		GD.PushWarning(text);
		try
		{
			Diagnostic?.Invoke(text);
		}
		catch
		{
		}
	}

	private SessionResult Apply(Func<SessionController, SessionResult> call)
	{
		SessionResult ready = Initialize();
		if (!ready.Success)
		{
			return ready;
		}
		_controller.AutoSave = AutoSave;
		return call(_controller);
	}

	public SessionResult CreateRun(int seed, string leaderId = "leader_green", string? deckId = null, int difficulty = 3, string mode = "set-confirm", bool setAnyCards = false)
	{
		return Apply((SessionController c) => c.CreateRun(seed, leaderId, deckId, difficulty, mode, setAnyCards));
	}

	public SessionResult CreateCheckpoint(int seed, string leaderId, ReplayOriginCheckpoint checkpoint, string mode = "set-confirm")
	{
		return Apply((SessionController c) => c.CreateCheckpoint(seed, leaderId, checkpoint, mode));
	}

	public SessionResult Dispatch(RunCommand command)
	{
		return DispatchTracked((SessionController c) => c.Dispatch(command), (command as RunCommand_Combat)?.Command);
	}

	public SessionResult Dispatch(Command command)
	{
		return DispatchTracked((SessionController c) => c.Dispatch(command), command);
	}

	public SessionResult DispatchJson(string json)
	{
		return DispatchTracked((SessionController c) => c.DispatchJson(json), null);
	}

	public SessionResult Load(string json)
	{
		return Apply((SessionController c) => c.Load(json));
	}

	public SessionResult ImportText(string json)
	{
		return Load(json);
	}

	public SessionContinuePreview PreviewContinue()
	{
		SessionResult ready = Initialize();
		if (!ready.Success)
		{
			return new SessionContinuePreview(ready);
		}
		return _controller.PreviewContinue();
	}

	public SessionResult Continue()
	{
		return Apply((SessionController c) => c.Continue());
	}

	public SessionResult Save()
	{
		return Apply((SessionController c) => c.Save());
	}

	public SessionResult ReturnToTitle()
	{
		return Apply((SessionController c) => c.ReturnToTitle());
	}

	public SessionResult AbandonRun()
	{
		return Apply((SessionController c) => c.AbandonRun());
	}

	public SessionResult AddNote(string text)
	{
		return Apply((SessionController c) => c.AddNote(text));
	}

	public SessionResult RateLastBattle(BattleRating rating)
	{
		return Apply((SessionController c) => c.RateLastBattle(rating));
	}

	public SessionResult SetMapAnnotations(JToken? annotations)
	{
		return Apply((SessionController c) => c.SetMapAnnotations(annotations));
	}

	public string ExportText(string note = "")
	{
		SessionResult ready = Initialize();
		if (!ready.Success)
		{
			throw new InvalidOperationException(ready.Error);
		}
		return _controller.ExportText(note);
	}

	public string ExportSave()
	{
		SessionResult ready = Initialize();
		if (!ready.Success)
		{
			throw new InvalidOperationException(ready.Error);
		}
		return _controller.ExportSave();
	}

	public SessionReport ExportRetainedReport()
	{
		SessionResult ready = Initialize();
		if (!ready.Success)
		{
			throw new InvalidOperationException(ready.Error);
		}
		return _controller.ExportRetainedReport();
	}

	public void SetReportText(ReportText text)
	{
		Initialize();
		if (_controller != null)
		{
			_controller.ReportText = text;
		}
	}

	public void SetPresentationBusy(bool busy)
	{
		bool changed = PresentationBusy != busy;
		_controller?.SetPresentationBusy(busy);
		if (changed) RecordFreezeEvent("GameSession.SetPresentationBusy");
	}

	public void ClearPresentation()
	{
		_controller?.ClearPresentation();
	}

	public bool TryDequeuePresentation(out PresentationBatch batch)
	{
		if (_controller != null)
		{
			return _controller.TryDequeuePresentation(out batch);
		}
		batch = null;
		return false;
	}

	public override void _Notification(int what)
	{
		if ((long)what == 2015 || (long)what == 1006 || (long)what == 1007)
		{
			FlushIfActive();
		}
	}

	public override void _ExitTree()
	{
		FlushIfActive();
	}

	private void FlushIfActive()
	{
		if (!(_controller?.CurrentRun == null))
		{
			SessionResult result = _controller.Save();
			if (!result.Success)
			{
				ForwardDiagnostic(result.Error);
			}
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(10)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.ForwardCleared, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "revision", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ForwardDiagnostic, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ExportText, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "note", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ExportSave, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.SetPresentationBusy, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Bool, "busy", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ClearPresentation, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._Notification, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "what", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.FlushIfActive, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null)
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
		if (method == MethodName.ForwardCleared && args.Count == 1)
		{
			ForwardCleared(VariantUtils.ConvertTo<long>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.ForwardDiagnostic && args.Count == 1)
		{
			ForwardDiagnostic(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default;
			return true;
		}
		string from;
		if (method == MethodName.ExportText && args.Count == 1)
		{
			from = ExportText(VariantUtils.ConvertTo<string>(in args[0]));
			ret = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (method == MethodName.ExportSave && args.Count == 0)
		{
			from = ExportSave();
			ret = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (method == MethodName.SetPresentationBusy && args.Count == 1)
		{
			SetPresentationBusy(VariantUtils.ConvertTo<bool>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.ClearPresentation && args.Count == 0)
		{
			ClearPresentation();
			ret = default;
			return true;
		}
		if (method == MethodName._Notification && args.Count == 1)
		{
			_Notification(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName._ExitTree && args.Count == 0)
		{
			_ExitTree();
			ret = default;
			return true;
		}
		if (method == MethodName.FlushIfActive && args.Count == 0)
		{
			FlushIfActive();
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
		if (method == MethodName.ForwardCleared)
		{
			return true;
		}
		if (method == MethodName.ForwardDiagnostic)
		{
			return true;
		}
		if (method == MethodName.ExportText)
		{
			return true;
		}
		if (method == MethodName.ExportSave)
		{
			return true;
		}
		if (method == MethodName.SetPresentationBusy)
		{
			return true;
		}
		if (method == MethodName.ClearPresentation)
		{
			return true;
		}
		if (method == MethodName._Notification)
		{
			return true;
		}
		if (method == MethodName._ExitTree)
		{
			return true;
		}
		if (method == MethodName.FlushIfActive)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.DataDirectory)
		{
			DataDirectory = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName.SaveDirectory)
		{
			SaveDirectory = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName.AutoSave)
		{
			AutoSave = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName.InitializationError)
		{
			InitializationError = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		string from;
		if (name == PropertyName.DataDirectory)
		{
			from = DataDirectory;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.SaveDirectory)
		{
			from = SaveDirectory;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		bool from2;
		if (name == PropertyName.AutoSave)
		{
			from2 = AutoSave;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.InitializationError)
		{
			from = InitializationError;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.IsInitialized)
		{
			from2 = IsInitialized;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.Revision)
		{
			value = VariantUtils.CreateFrom<long>(Revision);
			return true;
		}
		if (name == PropertyName.PresentationBusy)
		{
			from2 = PresentationBusy;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.HasSave)
		{
			from2 = HasSave;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.CanContinue)
		{
			from2 = CanContinue;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.HasRetainedReport)
		{
			from2 = HasRetainedReport;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.PendingPresentationCount)
		{
			value = VariantUtils.CreateFrom<int>(PendingPresentationCount);
			return true;
		}
		if (name == PropertyName._ownerThread)
		{
			value = VariantUtils.CreateFrom(in _ownerThread);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, PropertyName.DataDirectory, PropertyHint.None, "", PropertyUsageFlags.Default | PropertyUsageFlags.ScriptVariable, exported: true),
			new PropertyInfo(Variant.Type.String, PropertyName.SaveDirectory, PropertyHint.None, "", PropertyUsageFlags.Default | PropertyUsageFlags.ScriptVariable, exported: true),
			new PropertyInfo(Variant.Type.Bool, PropertyName.AutoSave, PropertyHint.None, "", PropertyUsageFlags.Default | PropertyUsageFlags.ScriptVariable, exported: true),
			new PropertyInfo(Variant.Type.Int, PropertyName._ownerThread, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.InitializationError, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.IsInitialized, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.Revision, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.PresentationBusy, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.HasSave, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.CanContinue, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.HasRetainedReport, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.PendingPresentationCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.DataDirectory, Variant.From<string>(DataDirectory));
		info.AddProperty(PropertyName.SaveDirectory, Variant.From<string>(SaveDirectory));
		info.AddProperty(PropertyName.AutoSave, Variant.From<bool>(AutoSave));
		info.AddProperty(PropertyName.InitializationError, Variant.From<string>(InitializationError));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.DataDirectory, out var _value_DataDirectory))
		{
			DataDirectory = _value_DataDirectory.As<string>();
		}
		if (info.TryGetProperty(PropertyName.SaveDirectory, out var _value_SaveDirectory))
		{
			SaveDirectory = _value_SaveDirectory.As<string>();
		}
		if (info.TryGetProperty(PropertyName.AutoSave, out var _value_AutoSave))
		{
			AutoSave = _value_AutoSave.As<bool>();
		}
		if (info.TryGetProperty(PropertyName.InitializationError, out var _value_InitializationError))
		{
			InitializationError = _value_InitializationError.As<string>();
		}
	}
}
