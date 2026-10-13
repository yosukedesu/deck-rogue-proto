using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Core;

public sealed class SessionController
{
	private readonly int _ownerThread = Environment.CurrentManagedThreadId;

	private readonly Func<DateTimeOffset> _now;

	private readonly SessionSaveStore? _saveStore;

	private readonly Queue<PresentationBatch> _presentation = new Queue<PresentationBatch>();

	private readonly List<BattleArchive> _history = new List<BattleArchive>();

	private readonly List<PlayNote> _notes = new List<PlayNote>();

	private readonly List<RunChoice> _choices = new List<RunChoice>();

	private readonly List<RunCommand> _commands = new List<RunCommand>();

	private readonly List<long> _times = new List<long>();

	private ReplayOrigin? _origin;

	private JToken? _doodles;

	private JObject _saveExtensions = new JObject();

	private SessionReport? _retainedReport;

	private static readonly HashSet<string> SaveFields = new HashSet<string>(StringComparer.Ordinal) { "kind", "run", "logIndex", "fingerprint", "history", "playNotes", "journal", "choices", "doodlesUnity" };

	private bool _dispatching;

	private string _journalMode = "set-confirm";

	private bool _journalDeparture = true;

	public RunState? CurrentRun { get; private set; }

	public long Revision { get; private set; }

	public bool PresentationBusy { get; private set; }

	public bool AutoSave { get; set; } = true;

	public bool HasSave => _saveStore?.Exists ?? false;

	public bool CanContinue => (CurrentRun != null) ? (!IsTerminal(CurrentRun)) : HasSave;

	public bool HasRetainedReport => _retainedReport != null || (_saveStore?.ReportExists ?? false);

	public int PendingPresentationCount => _presentation.Count;

	public ContentCatalog Catalog { get; } = new ContentCatalog();

	public ReportText ReportText { get; set; } = new ReportText();

	public IReadOnlyList<BattleArchive> History => _history.AsReadOnly();

	public IReadOnlyList<PlayNote> Notes => _notes.AsReadOnly();

	public IReadOnlyList<RunChoice> Choices => _choices.AsReadOnly();

	public JToken? LegacyUnityAnnotations => _doodles?.DeepClone();

	public JToken? MapAnnotations => _saveExtensions["doodlesGodot"]?.DeepClone();

	public RunJournal? Journal => (_origin == null) ? null : new RunJournal
	{
		Origin = _origin,
		Commands = _commands.ToArray(),
		Times = _times.ToArray()
	};

	private bool OnOwnerThread => Environment.CurrentManagedThreadId == _ownerThread;

	public event Action<SessionChange>? StateChanged;

	public event Action<long>? RunCleared;

	public event Action<string>? Diagnostic;

	public SessionController(SessionSaveStore? saveStore = null, Func<DateTimeOffset>? now = null)
	{
		_saveStore = saveStore;
		_now = now ?? ((Func<DateTimeOffset>)(() => DateTimeOffset.UtcNow));
	}

	public void SetPresentationBusy(bool busy)
	{
		RequireThread();
		PresentationBusy = busy;
	}

	public bool TryDequeuePresentation(out PresentationBatch batch)
	{
		RequireThread();
		return _presentation.TryDequeue(out batch);
	}

	public void ClearPresentation()
	{
		RequireThread();
		_presentation.Clear();
		PresentationBusy = false;
	}

	public SessionResult CreateRun(int seed, string leaderId = "leader_green", string? deckId = null, int difficulty = 3, string mode = "set-confirm", bool setAnyCards = false)
	{
		if (!OnOwnerThread)
		{
			return ThreadError();
		}
		if (_dispatching)
		{
			return SessionResult.Fail("A command is already being dispatched.");
		}
		try
		{
			RequireContent();
			ValidateMode(mode);
			RunState next = Run.CreateRun(seed, mode, leaderId, deckId, difficulty, new RunOptions
			{
				SetAnyCards = (setAnyCards ? new bool?(true) : ((bool?)null))
			});
			SessionResult retained = RetainBeforeReplacement(includeStoredRun: true);
			if (!retained.Success)
			{
				return retained;
			}
			ResetMetadata(new ReplayOrigin
			{
				Kind = "run",
				Seed = seed,
				LeaderId = leaderId,
				DeckId = deckId,
				Difficulty = next.Difficulty,
				SetAnyCards = next.SetAnyCards
			}, mode);
			return Commit(next, null, reset: true);
		}
		catch (Exception ex)
		{
			return SessionResult.Fail(ex.Message);
		}
	}

	public SessionResult CreateCheckpoint(int seed, string leaderId, ReplayOriginCheckpoint checkpoint, string mode = "set-confirm")
	{
		if (!OnOwnerThread)
		{
			return ThreadError();
		}
		if (_dispatching)
		{
			return SessionResult.Fail("A command is already being dispatched.");
		}
		try
		{
			RequireContent();
			ValidateMode(mode);
			RunState next = Run.CreateDebugCheckpointRun(seed, mode, leaderId, checkpoint);
			SessionResult retained = RetainBeforeReplacement(includeStoredRun: true);
			if (!retained.Success)
			{
				return retained;
			}
			ResetMetadata(new ReplayOrigin
			{
				Kind = "checkpoint",
				Seed = seed,
				LeaderId = leaderId,
				Checkpoint = checkpoint
			}, mode);
			return Commit(next, null, reset: true);
		}
		catch (Exception ex)
		{
			return SessionResult.Fail(ex.Message);
		}
	}

	public SessionResult Dispatch(Command command)
	{
		return Dispatch(new RunCommand_Combat
		{
			Command = command
		});
	}

	public SessionResult DispatchJson(string json)
	{
		if (!OnOwnerThread)
		{
			return ThreadError();
		}
		try
		{
			return Dispatch(CommandCatalog.Parse(json));
		}
		catch (Exception ex)
		{
			return SessionResult.Fail(ex.Message);
		}
	}

	public SessionResult Dispatch(RunCommand command)
	{
		if (!OnOwnerThread)
		{
			return ThreadError();
		}
		if (_dispatching)
		{
			return SessionResult.Fail("A command is already being dispatched.");
		}
		if (PresentationBusy)
		{
			return SessionResult.Fail("Wait for the current action to finish.");
		}
		if (CurrentRun == null)
		{
			return SessionResult.Fail("No run is active.");
		}
		_dispatching = true;
		try
		{
			CommandCatalog.Validate(command);
			RunCommand ownedCommand = JsonUnions.Deserialize<RunCommand>(JsonUnions.Serialize(command));
			RunState before = CurrentRun;
			RunState next = Run.ApplyRunCommand(before, ownedCommand);
			bool restart = ownedCommand is RunCommand_StartRun;
			if (restart)
			{
				SessionResult retained = RetainBeforeReplacement();
				if (!retained.Success)
				{
					return retained;
				}
				ResetMetadata(new ReplayOrigin
				{
					Kind = "run",
					Seed = next.Seed,
					LeaderId = next.LeaderId,
					Difficulty = next.Difficulty
				}, next.Mode);
			}
			else
			{
				Record(before, ownedCommand, next);
			}
			return Commit(next, ownedCommand, restart);
		}
		catch (Exception ex)
		{
			return SessionResult.Fail(ex.Message);
		}
		finally
		{
			_dispatching = false;
		}
	}

	public SessionResult Load(string json)
	{
		return LoadSave(json, persist: true);
	}

	private SessionResult LoadSave(string json, bool persist)
	{
		if (!OnOwnerThread)
		{
			return ThreadError();
		}
		if (_dispatching)
		{
			return SessionResult.Fail("A command is already being dispatched.");
		}
		try
		{
			RequireContent();
			JObject root = SessionJson.ParseObject(json);
			string storedMode = (string?)root["godotSession"]?["reactionMode"];
			bool departure = ((bool?)root["godotSession"]?["departure"]) ?? true;
			string warning;
			RunSaveFile sf = Report.ReadRunSaveFile(json, out warning);
			if ((sf == null || warning != null) && storedMode != null && (storedMode != "set-confirm" || !departure) && root["journal"] is JObject journalToken)
			{
				ValidateMode(storedMode);
				RunJournal journal = JsonUnions.FromToken<RunJournal>(journalToken);
				RunState recovered = ReplayWithMode(journal, storedMode, departure);
				root["run"] = JToken.Parse(JsonUnions.Serialize(recovered));
				sf = Report.ReadRunSaveFile(root.ToString(), out string _);
				warning = "Recovered the run from its journal using the saved reaction mode. " + warning;
			}
			if (sf == null)
			{
				return SessionResult.Fail(warning ?? "The save could not be read.");
			}
			string mode = storedMode ?? sf.Run.Mode;
			ValidateMode(mode);
			if (!Report.IsSaneRun(sf.Run))
			{
				return SessionResult.Fail("The restored run is incomplete.");
			}
			if (sf.Fingerprint != "" && sf.Fingerprint != Catalog.Fingerprint)
			{
				warning = Join(warning, "This save uses a different content fingerprint; results may differ.");
			}
			List<BattleArchive> history = sf.History ?? new List<BattleArchive>();
			List<PlayNote> notes = sf.PlayNotes ?? new List<PlayNote>();
			List<RunChoice> choices = sf.Choices ?? new List<RunChoice>();
			try
			{
				if (root["history"] is JArray)
				{
					history = JsonUnions.FromToken<List<BattleArchive>>(root["history"]);
				}
			}
			catch
			{
			}
			try
			{
				if (root["playNotes"] is JArray)
				{
					notes = JsonUnions.FromToken<List<PlayNote>>(root["playNotes"]);
				}
			}
			catch
			{
			}
			try
			{
				if (root["choices"] is JArray)
				{
					choices = JsonUnions.FromToken<List<RunChoice>>(root["choices"]);
				}
			}
			catch
			{
			}
			ReplayOrigin origin = sf.Journal?.Origin;
			IReadOnlyList<RunCommand> commands = sf.Journal?.Commands ?? Array.Empty<RunCommand>();
			List<long> times = new List<long>();
			for (int i = 0; i < commands.Count; i++)
			{
				long item;
				if (sf.Journal?.Times == null || i >= sf.Journal.Times.Count)
				{
					if (times.Count <= 0)
					{
						item = _now().ToUnixTimeMilliseconds();
					}
					else
					{
						item = times[times.Count - 1];
					}
				}
				else
				{
					item = sf.Journal.Times[i];
				}
				times.Add(item);
			}
			SessionResult retained = RetainBeforeReplacement(persist);
			if (!retained.Success)
			{
				return retained;
			}
			ResetMetadata(origin, mode);
			_journalDeparture = departure;
			_history.AddRange(history);
			_notes.AddRange(notes);
			_choices.AddRange(choices);
			_commands.AddRange(commands);
			_times.AddRange(times);
			_doodles = sf.DoodlesUnity?.DeepClone();
			foreach (JProperty property in root.Properties())
			{
				if (!SaveFields.Contains(property.Name))
				{
					_saveExtensions[property.Name] = property.Value.DeepClone();
				}
			}
			return Commit(sf.Run, null, reset: true, warning, persist);
		}
		catch (Exception ex)
		{
			return SessionResult.Fail("Could not load save: " + ex.Message);
		}
	}

	public SessionContinuePreview PreviewContinue()
	{
		if (!OnOwnerThread)
		{
			return new SessionContinuePreview(ThreadError());
		}
		if (_dispatching)
		{
			return new SessionContinuePreview(SessionResult.Fail("A command is already being dispatched."));
		}
		if (CurrentRun != null)
		{
			return new SessionContinuePreview(IsTerminal(CurrentRun) ? SessionResult.Fail("This run has ended. Its report can be exported from the title.") : SessionResult.Ok());
		}
		if (_saveStore == null)
		{
			return new SessionContinuePreview(SessionResult.Fail("No save storage is configured."));
		}
		string error = null;
		foreach (var candidate in _saveStore.ReadCandidates(delegate(string e)
		{
			if (error == null)
			{
				error = e;
			}
		}))
		{
			SessionController detached = new SessionController(null, _now)
			{
				AutoSave = false,
				ReportText = ReportText
			};
			SessionResult result = detached.LoadSave(candidate.Json, persist: false);
			if (!result.Success)
			{
				if (error == null)
				{
					error = result.Error;
				}
				continue;
			}
			string fingerprint = (string?)SessionJson.ParseObject(candidate.Json)["fingerprint"];
			bool mismatch = !string.IsNullOrEmpty(fingerprint) && fingerprint != Catalog.Fingerprint;
			return new SessionContinuePreview(SessionResult.Ok(Join(result.Warning, candidate.IsBackup ? "Recovered the previous save backup." : null)), mismatch);
		}
		return new SessionContinuePreview(SessionResult.Fail(error ?? "No saved run was found."));
	}

	public SessionResult Continue()
	{
		if (!OnOwnerThread)
		{
			return ThreadError();
		}
		if (_dispatching)
		{
			return SessionResult.Fail("A command is already being dispatched.");
		}
		if (CurrentRun != null)
		{
			return IsTerminal(CurrentRun) ? SessionResult.Fail("This run has ended. Its report can be exported from the title.") : Commit(CurrentRun, null, reset: true, null, save: false);
		}
		if (_saveStore == null)
		{
			return SessionResult.Fail("No save storage is configured.");
		}
		string error = null;
		foreach (var candidate in _saveStore.ReadCandidates(delegate(string e)
		{
			if (error == null)
			{
				error = e;
			}
		}))
		{
			SessionController detached = new SessionController(null, _now)
			{
				AutoSave = false,
				ReportText = ReportText
			};
			SessionResult inspected = detached.LoadSave(candidate.Json, persist: false);
			if (!inspected.Success)
			{
				if (error == null)
				{
					error = inspected.Error;
				}
				continue;
			}
			if (IsTerminal(detached.CurrentRun))
			{
				SessionResult retained = RetainFrom(detached);
				if (!retained.Success)
				{
					return retained;
				}
				SessionResult cleared = _saveStore.Clear();
				if (!cleared.Success)
				{
					return cleared;
				}
				return SessionResult.Fail("This run has ended. Its report is retained for export from the title.");
			}
			SessionResult result = LoadSave(candidate.Json, persist: false);
			if (result.Success)
			{
				return SessionResult.Ok(Join(result.Warning, candidate.IsBackup ? "Recovered the previous save backup." : null));
			}
			if (error == null)
			{
				error = result.Error;
			}
		}
		return SessionResult.Fail(error ?? "No saved run was found.");
	}

	public SessionResult Save()
	{
		return Save(includeReport: true);
	}

	private SessionResult Save(bool includeReport)
	{
		if (!OnOwnerThread)
		{
			return ThreadError();
		}
		if (CurrentRun == null)
		{
			return SessionResult.Fail("No run is active.");
		}
		if (_saveStore == null)
		{
			return SessionResult.Fail("No save storage is configured.");
		}
		try
		{
			string json = ExportSave();
			if (IsTerminal(CurrentRun))
			{
				SessionResult retained = StoreReport(CaptureReport(json));
				return retained.Success ? _saveStore.Clear() : retained;
			}
			SessionResult saved = _saveStore.Write(json);
			SessionResult report = (includeReport ? StoreReport(CaptureReport(json)) : SessionResult.Ok());
			return (saved.Success && report.Success) ? SessionResult.Ok() : SessionResult.Fail(Join(saved.Error, report.Error));
		}
		catch (Exception ex)
		{
			return SessionResult.Fail("Could not save: " + ex.Message);
		}
	}

	public SessionResult ReturnToTitle()
	{
		if (!OnOwnerThread)
		{
			return ThreadError();
		}
		if (_dispatching)
		{
			return SessionResult.Fail("A command is already being dispatched.");
		}
		if (CurrentRun == null)
		{
			return SessionResult.Ok();
		}
		if (IsTerminal(CurrentRun))
		{
			return AbandonRun();
		}
		return Save();
	}

	public string ExportSave()
	{
		RequireThread();
		if (CurrentRun == null)
		{
			throw new InvalidOperationException("No run is active.");
		}
		RunSaveFile sf = Report.MakeRunSaveFile(CurrentRun, _history, _notes, Journal, _choices, _doodles?.DeepClone());
		JObject root = SessionJson.ParseObject(Report.SerializeRunSaveFile(sf));
		foreach (JProperty property in _saveExtensions.Properties())
		{
			root[property.Name] = property.Value.DeepClone();
		}
		JObject sessionMetadata = (root["godotSession"] as JObject) ?? new JObject();
		sessionMetadata["schema"] = 1;
		sessionMetadata["reactionMode"] = _journalMode;
		sessionMetadata["departure"] = _journalDeparture;
		root["godotSession"] = sessionMetadata;
		return root.ToString(Formatting.None);
	}

	public string ExportText(string note = "")
	{
		RequireThread();
		return Report.BuildReport(CurrentRun, CurrentRun?.Combat, _history, note, _notes, _choices, Journal, ReportText, _now().UtcDateTime);
	}

	public SessionReport ExportRetainedReport()
	{
		RequireThread();
		if (_retainedReport != null)
		{
			return _retainedReport;
		}
		string error = null;
		if (_saveStore != null)
		{
			using IEnumerator<(SessionReport, bool)> enumerator = _saveStore.ReadReportCandidates(delegate(string e)
			{
				if (error == null)
				{
					error = e;
				}
			}).GetEnumerator();
			if (enumerator.MoveNext())
			{
				(SessionReport, bool) candidate = enumerator.Current;
				if (candidate.Item2)
				{
					ReportDiagnostic("Recovered the previous report backup.");
				}
				return _retainedReport = candidate.Item1;
			}
		}
		throw new InvalidOperationException(error ?? "No retained run report was found.");
	}

	private SessionReport CaptureReport(string? json = null)
	{
		DateTime at = _now().UtcDateTime;
		return new SessionReport(json ?? ExportSave(), Report.BuildReport(CurrentRun, CurrentRun?.Combat, _history, "自動保存", _notes, _choices, Journal, ReportText, at), at.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"));
	}

	private SessionResult StoreReport(SessionReport report)
	{
		_retainedReport = report;
		return _saveStore?.WriteReport(report) ?? SessionResult.Ok();
	}

	private SessionResult RetainFrom(SessionController source)
	{
		try
		{
			return StoreReport(source.CaptureReport());
		}
		catch (Exception ex)
		{
			return SessionResult.Fail("Could not retain report: " + ex.Message);
		}
	}

	private SessionResult RetainBeforeReplacement(bool includeStoredRun = false)
	{
		if (CurrentRun != null)
		{
			return RetainFrom(this);
		}
		if (includeStoredRun && _saveStore != null && _saveStore.Exists)
		{
			foreach (var candidate in _saveStore.ReadCandidates(ReportDiagnostic))
			{
				SessionController detached = new SessionController(null, _now)
				{
					AutoSave = false,
					ReportText = ReportText
				};
				if (detached.LoadSave(candidate.Json, persist: false).Success)
				{
					return RetainFrom(detached);
				}
			}
		}
		return SessionResult.Ok();
	}

	public SessionResult AddNote(string text)
	{
		if (!OnOwnerThread)
		{
			return ThreadError();
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return SessionResult.Fail("Write a note first.");
		}
		RunState r = CurrentRun;
		_notes.Add(new PlayNote
		{
			At = _now().UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
			Context = ((r == null) ? "setup" : $"Act {r.Act} Row {r.Row + 1} {r.Phase} T{r.Combat?.Turn ?? 0}"),
			Text = text.Trim()
		});
		SessionResult save = ((CurrentRun != null && AutoSave && _saveStore != null) ? Save() : SessionResult.Ok());
		return SessionResult.Ok(save.Error);
	}

	public SessionResult RateLastBattle(BattleRating rating)
	{
		if (!OnOwnerThread)
		{
			return ThreadError();
		}
		if (_history.Count == 0)
		{
			return SessionResult.Fail("There is no completed battle to rate.");
		}
		if (rating == null)
		{
			return SessionResult.Fail("A battle rating is required.");
		}
		List<BattleArchive> history = _history;
		history[history.Count - 1].Rating = rating.Clone();
		SessionResult save = ((AutoSave && _saveStore != null) ? Save() : SessionResult.Ok());
		return SessionResult.Ok(save.Error);
	}

	public SessionResult SetMapAnnotations(JToken? annotations)
	{
		if (!OnOwnerThread)
		{
			return ThreadError();
		}
		if (CurrentRun == null)
		{
			return SessionResult.Fail("No run is active.");
		}
		if (annotations == null)
		{
			_saveExtensions.Remove("doodlesGodot");
		}
		else
		{
			_saveExtensions["doodlesGodot"] = annotations.DeepClone();
		}
		SessionResult save = ((AutoSave && _saveStore != null) ? Save() : SessionResult.Ok());
		return SessionResult.Ok(save.Error);
	}

	public SessionResult AbandonRun()
	{
		if (!OnOwnerThread)
		{
			return ThreadError();
		}
		if (_dispatching)
		{
			return SessionResult.Fail("A command is already being dispatched.");
		}
		SessionResult retained = RetainBeforeReplacement(includeStoredRun: true);
		if (!retained.Success)
		{
			return retained;
		}
		SessionResult result = _saveStore?.Clear() ?? SessionResult.Ok();
		if (!result.Success)
		{
			return result;
		}
		CurrentRun = null;
		ResetMetadata(null, "set-confirm");
		ClearPresentation();
		Revision++;
		_dispatching = true;
		try
		{
			if (this.RunCleared != null)
			{
				Delegate[] invocationList = this.RunCleared.GetInvocationList();
				for (int i = 0; i < invocationList.Length; i++)
				{
					Action<long> listener = (Action<long>)invocationList[i];
					try
					{
						listener(Revision);
					}
					catch (Exception ex)
					{
						ReportDiagnostic("Session clear listener failed: " + ex.Message);
					}
				}
			}
		}
		finally
		{
			_dispatching = false;
		}
		return result;
	}

	private SessionResult Commit(RunState next, RunCommand? command, bool reset, string? warning = null, bool save = true)
	{
		RunState previous = CurrentRun;
		IReadOnlyList<GameEvent> readOnlyList;
		if (!reset)
		{
			readOnlyList = EventDelta(previous?.Combat, next.Combat);
		}
		else
		{
			IReadOnlyList<GameEvent> readOnlyList2 = Array.Empty<GameEvent>();
			readOnlyList = readOnlyList2;
		}
		IReadOnlyList<GameEvent> events = readOnlyList;
		CurrentRun = next;
		Revision++;
		if (reset)
		{
			ClearPresentation();
		}
		SessionChange change = new SessionChange(previous, next, command, events, Revision, reset);
		_presentation.Enqueue(new PresentationBatch(Revision, previous, next, events, reset));
		if (save && AutoSave && _saveStore != null)
		{
			bool battleEnded = next.Combat != null && previous?.Combat?.Phase != next.Combat.Phase && (next.Combat.Phase == "won" || next.Combat.Phase == "lost");
			SessionResult saved = Save(!reset && (!(command is RunCommand_Combat) || battleEnded));
			if (!saved.Success)
			{
				warning = Join(warning, saved.Error);
			}
		}
		bool wasDispatching = _dispatching;
		_dispatching = true;
		try
		{
			if (this.StateChanged != null)
			{
				Delegate[] invocationList = this.StateChanged.GetInvocationList();
				for (int i = 0; i < invocationList.Length; i++)
				{
					Action<SessionChange> listener = (Action<SessionChange>)invocationList[i];
					try
					{
						listener(change);
					}
					catch (Exception ex)
					{
						ReportDiagnostic("Presentation listener failed: " + ex.Message);
					}
				}
			}
		}
		finally
		{
			_dispatching = wasDispatching;
		}
		return SessionResult.Ok(warning);
	}

	public static IReadOnlyList<GameEvent> EventDelta(GameState? previous, GameState? current)
	{
		if (current == null || current.EventLog.Count == 0)
		{
			return Array.Empty<GameEvent>();
		}
		int start = 0;
		if (previous != null && current.EventLog.Count >= previous.EventLog.Count && (previous.EventLog.Count == 0 || (object)previous.EventLog[0] == current.EventLog[0]))
		{
			start = previous.EventLog.Count;
		}
		return current.EventLog.Skip(start).ToArray();
	}

	private void Record(RunState previous, RunCommand command, RunState next)
	{
		if (_origin != null)
		{
			_commands.Add(command);
			_times.Add(_now().ToUnixTimeMilliseconds());
		}
		try
		{
			RunChoice choice = Report.DescribeRunChoice(previous, command, next);
			if (choice != null)
			{
				_choices.Add(choice);
			}
		}
		catch (Exception ex)
		{
			ReportDiagnostic("Choice report failed: " + ex.Message);
		}
		GameState combat = next.Combat;
		if (combat == null || previous.Combat == null || previous.Combat.Phase == combat.Phase || (combat.Phase != "won" && combat.Phase != "lost"))
		{
			return;
		}
		try
		{
			int battleNo = previous.BattlesWon + 1;
			if (!_history.Any((BattleArchive h) => h.BattleNo == battleNo))
			{
				MapNode node = Run.CurrentNode(previous);
				string enemyId = combat.EventLog.OfType<GameEvent_CombatStarted>().FirstOrDefault()?.EnemyId ?? node?.EncounterId ?? "unknown";
				_history.Add(Report.ArchiveBattle(combat, battleNo, enemyId, previous.CurrentElite, previous.Hp, previous.Deck.Count, previous.Act, node?.Type == "boss", ReportText));
			}
		}
		catch (Exception ex2)
		{
			ReportDiagnostic("Battle archive failed: " + ex2.Message);
		}
	}

	private void ResetMetadata(ReplayOrigin? origin, string mode)
	{
		_origin = origin;
		_journalMode = mode;
		_journalDeparture = true;
		_doodles = null;
		_saveExtensions = new JObject();
		_commands.Clear();
		_times.Clear();
		_history.Clear();
		_notes.Clear();
		_choices.Clear();
	}

	private static RunState ReplayWithMode(RunJournal journal, string mode, bool departure)
	{
		ReplayOrigin o = journal.Origin;
		RunState run = ((o.Kind == "checkpoint" && o.Checkpoint != null) ? Run.CreateDebugCheckpointRun(o.Seed, mode, o.LeaderId, o.Checkpoint) : Run.CreateRun(o.Seed, mode, o.LeaderId, o.DeckId, o.Difficulty ?? 3, new RunOptions
		{
			SetAnyCards = o.SetAnyCards,
			Departure = departure
		}));
		foreach (RunCommand command in journal.Commands)
		{
			run = Run.ApplyRunCommand(run, command);
		}
		return run;
	}

	private static bool IsTerminal(RunState run)
	{
		return run.Phase == "won" || run.Phase == "lost";
	}

	private static SessionResult ThreadError()
	{
		return SessionResult.Fail("Session and save operations must run on the main thread.");
	}

	private void RequireThread()
	{
		if (!OnOwnerThread)
		{
			throw new InvalidOperationException(ThreadError().Error);
		}
	}

	private static void RequireContent()
	{
		if (!Content.IsLoaded)
		{
			throw new InvalidOperationException("The content catalog has not loaded.");
		}
	}

	private static void ValidateMode(string mode)
	{
		if (mode != "set-confirm" && mode != "set-auto" && mode != "hold-manual")
		{
			throw new InvalidOperationException("Unknown reaction mode: " + mode);
		}
	}

	private void ReportDiagnostic(string text)
	{
		try
		{
			this.Diagnostic?.Invoke(text);
		}
		catch
		{
		}
	}

	private static string? Join(string? a, string? b)
	{
		return (a == null) ? b : ((b == null) ? a : (a + " " + b));
	}
}
