// Explicit visual diagnostic only. Production admission remains default OFF.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace DeckRogue.Game {
 public partial class Autopilot {
  const int LightningR9ReviewFps = 120;
  const int LightningR9ReviewFrames = 300;
  const int LightningR9ReviewInputFrame = 60;
  internal static bool LightningR9ReviewFixedStep { get; private set; }
  bool _lightningR9ReviewFailed;
  static string LightningR9ReviewHash(JToken value) {
   using (var hash = SHA256.Create())
    return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value.ToString(Formatting.None)))).Replace("-", "").ToLowerInvariant();
  }
  static void LightningR9Require(bool condition, string reason) {
   if (!condition) throw new InvalidOperationException(reason);
  }
  // Owned by the outer review finally: Guard can stop a nested StateJump without disposing its parent iterator.
  // Read-only while recording. A mismatch is latched and aborts at an existing gate; never repair it mid-capture.
  sealed class LightningR9DofObservation : IDisposable {
   readonly JObject _report;
   readonly JArray _checks = new JArray(), _changes = new JArray(), _cameras = new JArray();
   readonly HD2DTiltShift _priorFlag = HD2DFlags.TiltShift;
   HD2DTiltShift _lastFlag = HD2DFlags.TiltShift, _expectedFlag = HD2DTiltShift.On;
   bool _armed, _overrideAttempted;
   int _lastStageCameraFrame = -1;
   string _failure;
   public string Requested { get; private set; }
   public LightningR9DofObservation(JObject report) {
    _report = new JObject {
     {"argument", "-lightning-r9-dof"}, {"requested", null}, {"overrideApplied", false}, {"priorFlag", _priorFlag.ToString()},
     {"freshProcessRequired", true}, {"checkpoints", _checks}, {"flagChangeEvents", _changes}, {"stageCameraCallbacks", _cameras},
     {"passEnqueueObserved", false}, {"gpuExecutionVerified", false},
     {"evidenceLimit", "Stage-camera begin callbacks and Hook.WouldRun are observations only; neither proves pass enqueue, material readiness, RenderGraph recording, or GPU execution. Debug/look values are observed, never forced."}
    };
    report["dofDiagnostic"] = _report;
    HD2DFlags.Changed += OnFlagsChanged;
    RenderPipelineManager.beginCameraRendering += OnBeginCamera;
   }
   public void ReadRequest(string[] args) {
    const string key = "-lightning-r9-dof";
    int count = 0;
    for (int i = 0; i < args.Length; i++) {
     if (args[i] == key) { count++; Requested = i + 1 < args.Length ? args[i + 1] : ""; }
     else if (args[i].StartsWith(key + "=", StringComparison.Ordinal)) { count++; Requested = args[i].Substring(key.Length); }
    }
    _report["requested"] = Requested; _report["argumentOccurrences"] = count;
    LightningR9Require(count <= 1 && (count == 0 || Requested == "0" || Requested == "1"), "Specify optional -lightning-r9-dof 0 or 1 once, as two arguments; missing, duplicate, and other values are invalid.");
    _expectedFlag = Requested == "0" ? HD2DTiltShift.Off : HD2DTiltShift.On;
    _report["strictChecks"] = Requested != null; // Absent argument preserves legacy admission/abort behavior.
    _report["expectedFlag"] = _expectedFlag.ToString();
   }
   public string StateSpec(string spec) {
    _overrideAttempted = Requested != null;
    return _overrideAttempted ? spec + ";dof=" + Requested : spec;
   }
   JObject Snapshot(string phase) {
    var cam = Stage.Camera;
    return new JObject {
     {"phase", phase}, {"frame", Time.frameCount}, {"time", Time.time}, {"flag", HD2DFlags.TiltShift.ToString()},
     {"settingsEnabled", TiltShiftSettings.Enabled}, {"useUrpBokeh", TiltShiftSettings.UseUrpBokeh}, {"hookWouldRun", TiltShiftHook.WouldRun},
     {"bandNear", TiltShiftSettings.BandNear}, {"bandFar", TiltShiftSettings.BandFar}, {"debugView", TiltShiftPass.DebugView},
     {"debugViewIsNormal", TiltShiftPass.DebugView == 0}, {"lookOverride", HD2DFlags.Look}, {"uiOnly", HD2DFlags.UiOnly}, {"unitsOnly", HD2DFlags.UnitsOnly},
     {"stageCameraName", cam != null ? cam.name : null}, {"stageCameraType", cam != null ? cam.cameraType.ToString() : null},
     {"lastStageCameraCallbackFrame", _lastStageCameraFrame}, {"stageCameraCallbackObservedThisFrame", _lastStageCameraFrame == Time.frameCount}
    };
   }
   void Fail(string reason) { if (_failure == null) { _failure = reason; _report["failure"] = reason; } }
   void ObserveMismatch(string phase, bool settings) {
    if (Requested == null) return; // Legacy calls gain passive observations only.
    if (HD2DFlags.TiltShift != _expectedFlag) Fail("dof_flag_mismatch:" + phase);
    bool on = _expectedFlag == HD2DTiltShift.On;
    if (settings && (TiltShiftSettings.Enabled != on || TiltShiftSettings.UseUrpBokeh || TiltShiftHook.WouldRun != on)) Fail("dof_settings_mismatch:" + phase);
   }
   void OnFlagsChanged() {
    try {
     var row = Snapshot("HD2DFlags.Changed"); row["dofChanged"] = _lastFlag != HD2DFlags.TiltShift; row["flags"] = HD2DFlags.Describe();
     _changes.Add(row); _lastFlag = HD2DFlags.TiltShift;
     if (_armed) ObserveMismatch("HD2DFlags.Changed", false); // Other subscribers may still be synchronizing settings.
    } catch (Exception ex) { Fail("dof_flag_observation_failed:" + ex); }
   }
   void OnBeginCamera(ScriptableRenderContext context, Camera cam) {
    if (!_armed || cam == null || cam != Stage.Camera || cam.cameraType != CameraType.Game) return;
    try {
     _lastStageCameraFrame = Time.frameCount;
     _cameras.Add(Snapshot("beginCameraRendering:Stage.Camera"));
     ObserveMismatch("stage-camera-callback", true);
    } catch (Exception ex) { Fail("dof_camera_observation_failed:" + ex); }
   }
   public void Check(string phase, bool requireStageCamera = false) {
    _armed = true;
    _checks.Add(Snapshot(phase));
    ObserveMismatch(phase, true);
    if (Requested != null && requireStageCamera && _lastStageCameraFrame != Time.frameCount) Fail("dof_stage_camera_callback_missing:" + phase);
    LightningR9Require(Requested == null || _failure == null, _failure);
    if (phase == "after-state-jump") {
     _report["overrideApplied"] = _overrideAttempted;
     Debug.Log("[LightningR9Review] DOF requested=" + (Requested ?? "absent (fixed slice)") + "; effective=" + HD2DFlags.TiltShift + "; settingsEnabled=" + TiltShiftSettings.Enabled + "; useUrpBokeh=" + TiltShiftSettings.UseUrpBokeh + "; hookWouldRun=" + TiltShiftHook.WouldRun + "; debugView=" + TiltShiftPass.DebugView);
    }
   }
   public void Dispose() {
    HD2DFlags.Changed -= OnFlagsChanged;
    RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
    _report["stageCameraCallbackCount"] = _cameras.Count;
    _report["cleanupRestoreAttempted"] = _overrideAttempted;
    if (!_overrideAttempted) return;
    // Restore only this explicit diagnostic flag, after detaching observers. No direct settings write or extra yield.
    string prior = _priorFlag == HD2DTiltShift.Off ? "0" : _priorFlag == HD2DTiltShift.On ? "1" : "urp";
    _report["cleanupLimit"] = "Restores the prior DOF flag through ApplyState only; existing fixture state and ApplyState's explicit-key classification are not rolled back. Use a fresh process/directory.";
    HD2DFlags.ApplyState(HD2DFlags.ParseSpec("dof=" + prior, ';'));
    _report["cleanupFlag"] = HD2DFlags.TiltShift.ToString();
    _report["cleanupRestored"] = HD2DFlags.TiltShift == _priorFlag;
    LightningR9Require(HD2DFlags.TiltShift == _priorFlag, "dof_cleanup_flag_mismatch");
   }
  }
  // Same end-of-frame, UI-inclusive screen capture as the original diagnostic.
  // One call per requested image. Null/invalid readback aborts; never retry a movie frame.
  static void LightningR9Capture(string path, JObject attempt) {
   Texture2D image = null;
   attempt["status"] = "attempted";
   attempt["frame"] = Time.frameCount; attempt["time"] = Time.time;
   attempt["wallTime"] = Time.realtimeSinceStartupAsDouble;
   attempt["screenWidth"] = Screen.width; attempt["screenHeight"] = Screen.height;
   attempt["applicationFocused"] = Application.isFocused;
   try {
    image = ScreenCapture.CaptureScreenshotAsTexture();
    LightningR9Require(image != null, "screen_capture_null: no readable rendered game-view texture; check visible/non-minimized player and graphics backend. No frame retry or replacement was performed.");
    attempt["textureWidth"] = image.width; attempt["textureHeight"] = image.height;
    LightningR9Require(image.width == Screen.width && image.height == Screen.height && image.width > 0 && image.height > 0, "screen_capture_size_mismatch");
    byte[] png = image.EncodeToPNG();
    LightningR9Require(png != null && png.Length >= 8 && png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71 && png[4] == 13 && png[5] == 10 && png[6] == 26 && png[7] == 10, "screen_capture_invalid_png");
    File.WriteAllBytes(path, png);
    attempt["bytes"] = png.Length; attempt["status"] = "saved";
   } catch (Exception ex) {
    attempt["status"] = "failed"; attempt["failure"] = ex.GetType().Name + ": " + ex.Message;
    throw;
   } finally { if (image != null) Destroy(image); }
  }
  IEnumerator LightningR9Review(GameRoot g) {
   _lightningR9ReviewFailed = false;
   string mode = Arg("-lightning-r9");
   var errors = new JArray();
   var report = new JObject {
    {"schema", "lightning-r9-visual-review-2"}, {"captureMethod", "ScreenCapture.CaptureScreenshotAsTexture after WaitForEndOfFrame"}, {"preflightAttempts", 1}, {"frameRetriesAllowed", false}, {"status", "running"},
    {"mode", mode}, {"fps", LightningR9ReviewFps}, {"seed", _seed},
    {"platform", Application.platform.ToString()}, {"unity", Application.unityVersion},
    {"performanceEvidence", false}, {"nativeVisualApproval", false},
    {"captureKind", "consecutive rendered frames; fixed simulation time; silent"},
    {"clockNote", "PNG readback/encoding stalls wall time. Reconstruct at 120 fps. Timing probe wall-ms/profiler fields are not performance evidence. Audio wall-clock playback is not synchronized to this recording."},
    {"errors", errors}
   };
   string reportPath = Path.Combine(_dir, "lightning-r9-review.json");
   // Refuse to mix recordings or overwrite earlier diagnostic evidence.
   if (File.Exists(reportPath) || Directory.Exists(Path.Combine(_dir, "frames"))) {
    Debug.LogError("[LightningR9Review] use a fresh -shots directory");
    _lightningR9ReviewFailed = true; yield break;
   }
   File.WriteAllText(reportPath, report.ToString());
   int previousFps = Time.captureFramerate;
   bool previousShowcase = HD2DFlags.CombatPresentationShowcase;
   float previousTimeScale = Time.timeScale;
   var dof = new LightningR9DofObservation(report);
   try {
    yield return Guard(LightningR9ReviewCase(g, mode, report, dof), ex => errors.Add(ex.ToString()));
   } finally {
    try { dof.Dispose(); } catch (Exception ex) { errors.Add("DOF cleanup: " + ex); }
    WorldBattleFx.ConfigureLightningR9Candidate(false);
    Presenter.Reset();
    Time.timeScale = previousTimeScale;
    LightningR9ReviewFixedStep = false;
    Time.captureFramerate = previousFps;
    HD2DFlags.CombatPresentationShowcase = previousShowcase;
    _lightningR9ReviewFailed = errors.Count > 0;
    report["status"] = _lightningR9ReviewFailed ? "failed" : "completed";
    report["candidateEnabledAfterCleanup"] = LightningR9CandidateFx.Enabled;
    File.WriteAllText(reportPath, report.ToString());
   }
   Debug.Log("[LightningR9Review] " + report["status"] + "; " + reportPath);
  }
  IEnumerator LightningR9ReviewCase(GameRoot g, string mode, JObject report, LightningR9DofObservation dof) {
   LightningR9Require(mode == "off" || mode == "on", "Specify -lightning-r9 off or on explicitly; run each in a fresh process/directory.");
   dof.ReadRequest(CommandLine()); // Validate before deterministic setup, engine state changes, or any capture.
   LightningR9Require(!Application.isMobilePlatform, "This is a desktop visual diagnostic; mobile performance and auto-quality are outside its scope.");
   LightningR9Require(!CombatTimingProbe.Enabled, "Another timing probe is already active.");
   EnableDet();
   LightningR9ReviewFixedStep = true;
   Time.captureFramerate = LightningR9ReviewFps;
   WorldBattleFx.ConfigureLightningR9Candidate(false);
   string spec = "phase=combat;hd2d=slice;tier=pc;difficulty=1;leader=leader_izzet;enemy=enemy_probe;hand=blue_aether_burst;penergy=10;eblock=0;eexposed=0;combatpresentation=showcase";
   spec = dof.StateSpec(spec); // Same StateJump: slice bundle first, optional individual DOF override wins.
   report["state"] = spec;
   yield return StateJump(g, spec);
   dof.Check("after-state-jump");
   LightningR9Require(g.Rs?.Combat != null && g.Battle != null && Stage.CombatWorld && CardVfxProfile.ShowcaseOn, "Fixture requires showcase and the built world battle.");
   // Fixture setup only. After the normal UI click there are no engine writes.
   var st = g.Rs.Combat;
   g.Rs = g.Rs with { Combat = st with { Player = st.Player with { Aether = 1 } } };
   Presenter.Reset(); Presenter.MarkSeen(g.Rs.Combat); g.Rebuild();
   dof.Check("after-fixture-rebuild");
   for (int f = 0; f < 90; f++) yield return null;
   Canvas.ForceUpdateCanvases();
   dof.Check("settled");
   LightningR9Require(Presenter.PresentationIdle && !g.CombatPresentationPending, "Fixture did not settle naturally.");
   LightningR9Require(g.LeaderId == "leader_izzet" && g.Rs.Combat.Player.Aether == 1 && g.Rs.Combat.Player.Hand.Count == 1, "Unexpected fixture identity or Aether.");
   LightningR9Require(Mathf.Approximately(Time.timeScale, 1f), "Fixture began with a slow clock.");
   var initial = g.Rs.Combat;
   report["initialEngine"] = JToken.FromObject(initial);
   report["initialEngineSha256"] = LightningR9ReviewHash(report["initialEngine"]);
   report["quality"] = GfxQuality.Level.ToString();
   report["flags"] = HD2DFlags.Describe();
   report["width"] = Screen.width; report["height"] = Screen.height;
   report["colorSpace"] = QualitySettings.activeColorSpace.ToString();
   report["graphicsDevice"] = SystemInfo.graphicsDeviceName;
   report["graphicsApi"] = SystemInfo.graphicsDeviceType.ToString();
   int from = initial.EventLog.Count, beforeReceived = ResolvedCombatFx.Received, beforePresented = ResolvedCombatFx.Presented;
   int beforeDuplicate = ResolvedCombatFx.DuplicateSuppressed, beforeSubmitted = LightningR9CandidateFx.Submitted, beforeTimeout = g.PresentationTimeoutCount;
   var frames = new JArray(); var contacts = new JArray(); var messages = new JArray(); var attempts = new JArray();
   report["frames"] = frames; report["captureAttempts"] = attempts; report["contacts"] = contacts; report["renderMessages"] = messages;
   string frameDir = Path.Combine(_dir, "frames"); Directory.CreateDirectory(frameDir);
   Action<ResolvedCombatFx.Ticket> observe = ticket => {
    contacts.Add(new JObject {
     {"event", ticket.Event}, {"source", ticket.SourceKey}, {"sourceUid", ticket.SourceUid}, {"target", ticket.Target},
     {"frame", Time.frameCount}, {"time", Time.time}, {"renderer", ticket.AuthoredRenderer},
     {"renderPath", ticket.RenderPath}, {"renderFallback", ticket.RenderFallbackReason}, {"sourceFallback", ticket.FallbackReason},
     {"r9Binding", ticket.LightningR9Binding != null}, {"r9LastFailure", LightningR9CandidateFx.LastFailure},
     {"diagnosticAdmission", mode == "off" ? "r9_disabled_by_comparison" : "r9_opt_in_requested"},
     {"amount", ticket.Amount}, {"effectiveDamage", ticket.Intensity?.Effective ?? 0}, {"tier", ticket.Intensity?.Tier},
     {"engineHp", g.Rs.Combat.Enemies[0].Hp}, {"displayHpAtContact", Presenter.EnemyDisplayHp(g.Rs.Combat, 0)}
    });
   };
   Application.LogCallback observeLog = (message, stack, type) => {
    if (message.StartsWith("[ResolvedVfx]") || message.StartsWith("[Presentation]") || type == LogType.Exception || type == LogType.Error)
     messages.Add(new JObject {{"frame", Time.frameCount}, {"time", Time.time}, {"type", type.ToString()}, {"message", message}});
   };
   var readiness = new JArray(); var readinessErrors = new JArray();
   report["readinessDiagnostics"] = true; report["readinessAtGate"] = readiness; report["readinessObservationErrors"] = readinessErrors;
   Action<Shader, Texture2D, TextAsset, bool> observeLegacyReadiness = (shader, texture, palette, accepted) => {
    try {
     readiness.Add(new JObject {{"renderer", "continuous-lightning-discharge-r2"}, {"frame", Time.frameCount}, {"time", Time.time},
      {"resourceGateAccepted", accepted}, {"shaderPath", "DeckRogue/LightningDischargeFx"}, {"shaderFound", shader != null},
      {"shaderSupported", shader != null && shader.isSupported}, {"texturePath", "Art/fx/lightning-discharge-surface-r2"},
      {"textureLoaded", texture != null}, {"textureWidth", texture != null ? texture.width : 0}, {"textureHeight", texture != null ? texture.height : 0},
      {"expectedWidth", 1774}, {"expectedHeight", 887}, {"palettePath", "Data/lightning-discharge-palette-r2"}, {"paletteLoaded", palette != null}});
    } catch (Exception ex) { readinessErrors.Add("legacy:" + ex.GetType().Name + ":" + ex.Message); }
   };
   Action<Shader, bool> observeR9Readiness = (shader, accepted) => {
    try {
     readiness.Add(new JObject {{"renderer", LightningR9CandidateFx.RendererId}, {"frame", Time.frameCount}, {"time", Time.time},
      {"resourceGateAccepted", accepted}, {"shaderPath", "DeckRogue/LightningR9Candidate"}, {"shaderFound", shader != null},
      {"shaderSupported", shader != null && shader.isSupported}, {"textureRequired", false}, {"paletteResourceRequired", false}});
    } catch (Exception ex) { readinessErrors.Add("r9:" + ex.GetType().Name + ":" + ex.Message); }
   };
   var placement = new JArray(); var placementErrors = new JArray();
   report["placementDiagnostics"] = true; report["placementObservations"] = placement; report["placementObservationErrors"] = placementErrors;
   Action<object> observeR9Placement = value => {
    try { var row=JToken.FromObject(value); placement.Add(row); if((string)row["phase"]=="observation-error")placementErrors.Add(row.DeepClone()); }
    catch (Exception ex) { placementErrors.Add(ex.GetType().Name + ":" + ex.Message); }
   };
   LightningR9CandidateFx.PlacementForReview += observeR9Placement;
   LightningDischargeFx.ReadinessForReview += observeLegacyReadiness;
   LightningR9CandidateFx.ReadinessForReview += observeR9Readiness;
   ResolvedCombatFx.PresentedForReview += observe;
   Application.logMessageReceived += observeLog;
   GameState afterInput = null;
   try {
    // Fixed single acquisition probe before any input and outside the 300-frame movie.
    // A failed probe stops this process; it is not a skipped/retried movie frame.
    var preflight = new JObject { {"file", "capture-preflight.png"} };
    report["capturePreflight"] = preflight;
    yield return new WaitForEndOfFrame();
    dof.Check("preflight-end-of-frame", true);
    LightningR9Capture(Path.Combine(_dir, "capture-preflight.png"), preflight);
    yield return null;
    WorldBattleFx.ConfigureLightningR9Candidate(mode == "on");
    CombatTimingProbe.Begin();
    int previousFrame = -1;
    float previousTime = 0;
    for (int f = 0; f < LightningR9ReviewFrames; f++) {
     if (f == LightningR9ReviewInputFrame) {
      dof.Check("before-input");
      g.PreferredTarget = 0;
      var card = g.Battle.CardAt(0);
      LightningR9Require(card != null && card.Playable && card.Card.Def.Id == "blue_aether_burst", "Normal UI card is unavailable.");
      report["inputFrame"] = Time.frameCount; report["inputTime"] = Time.time;
      CombatTimingProbe.Event("input", card.Card.Def.Id);
      ExecuteEvents.Execute(card.Rt.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
      CombatTimingProbe.Event("inputReturned");
      LightningR9Require(g.Error == null && !ReferenceEquals(initial, g.Rs.Combat), "Normal UI card was not accepted: " + g.Error);
      afterInput = g.Rs.Combat;
      report["afterInputEngine"] = JToken.FromObject(afterInput);
      var added = afterInput.EventLog.Skip(from).ToArray();
      var hits = added.OfType<GameEvent_DamageDealt>().ToArray();
      report["damageEventIndex"] = from + Array.FindIndex(added, ev => ev is GameEvent_DamageDealt);
      LightningR9Require(added.OfType<GameEvent_CardPlayed>().Count() == 1 && added.OfType<GameEvent_AetherDischarged>().Count(a => a.Spent == 1) == 1 && hits.Length == 1, "Expected one real card, one Aether discharge and one damage receipt.");
      LightningR9Require(hits[0].HpLoss >= 5 && hits[0].HpLoss <= 14 && afterInput.Player.Aether == 0 && afterInput.Turn == initial.Turn, "Fixture did not produce an eligible medium hit without turn advancement.");
     }
     Tooltip.Hide();
     yield return new WaitForEndOfFrame();
     dof.Check("recording-end-of-frame:" + f, true);
     LightningR9Require(Time.captureFramerate == LightningR9ReviewFps && Mathf.Abs(Time.deltaTime - 1f / LightningR9ReviewFps) < .0001f && Mathf.Abs(Tween.UnscaledDt - 1f / LightningR9ReviewFps) < .0001f, "Capture/simulation/unscaled diagnostic clocks diverged.");
     LightningR9Require(previousFrame < 0 || Time.frameCount == previousFrame + 1, "A rendered frame was skipped; do not treat these PNGs as a continuous movie.");
     LightningR9Require(previousFrame < 0 || Mathf.Abs(Time.time - previousTime - 1f / LightningR9ReviewFps) < .0002f, "Simulation time skipped or slowed.");
     var row = new JObject {{"file", "frame_" + f.ToString("D4") + ".png"}, {"frame", Time.frameCount}, {"time", Time.time}, {"wallTime", Time.realtimeSinceStartupAsDouble}, {"deltaTime", Time.deltaTime}, {"unscaledDiagnosticDt", Tween.UnscaledDt}, {"timeScale", Time.timeScale}, {"engineHp", g.Rs.Combat.Enemies[0].Hp}, {"displayHp", Presenter.EnemyDisplayHp(g.Rs.Combat, 0)}, {"r9Active", LightningR9CandidateFx.ActiveCount}, {"worldActive", WorldBattleFx.ActiveCount}, {"quality", GfxQuality.Level.ToString()}};
     var attempt = new JObject {{"file", "frame_" + f.ToString("D4") + ".png"}, {"recordingIndex", f}};
     attempts.Add(attempt);
     LightningR9Capture(Path.Combine(frameDir, (string)attempt["file"]), attempt);
     frames.Add(row); // Only a successfully persisted real frame enters the movie manifest.
     previousFrame = Time.frameCount; previousTime = Time.time;
     yield return null;
    }
    report["finalEngine"] = JToken.FromObject(g.Rs.Combat);
    report["finalEngineSha256"] = LightningR9ReviewHash(report["finalEngine"]);
    report["received"] = ResolvedCombatFx.Received - beforeReceived;
    report["presented"] = ResolvedCombatFx.Presented - beforePresented;
    report["r9Submitted"] = LightningR9CandidateFx.Submitted - beforeSubmitted;
    report["watchdogTimeouts"] = g.PresentationTimeoutCount - beforeTimeout;
    report["r9LastFailure"] = LightningR9CandidateFx.LastFailure;
    report["presentationIdle"] = Presenter.PresentationIdle && !g.CombatPresentationPending;
    bool expectedRenderer = contacts.Count == 1 && (string)contacts[0]["renderer"] == (mode == "on" ? LightningR9CandidateFx.RendererId : "continuous-lightning-discharge-r2");
    report["requestedRendererObserved"] = expectedRenderer;
    LightningR9Require(afterInput != null && ReferenceEquals(afterInput, g.Rs.Combat) && JToken.DeepEquals(report["afterInputEngine"], report["finalEngine"]), "Presentation altered the engine result after input.");
    LightningR9Require((int)report["received"] == 1 && (int)report["presented"] == 1 && ResolvedCombatFx.DuplicateSuppressed == beforeDuplicate, "Receipt count or duplicate suppression changed.");
    LightningR9Require((bool)report["presentationIdle"] && (int)report["watchdogTimeouts"] == 0, "Presentation failed to drain naturally.");
    bool fatalLog = messages.Any(m => (string)m["type"] == "Error" || (string)m["type"] == "Exception");
    bool routeClean = contacts.Count == 1 && string.IsNullOrEmpty((string)contacts[0]["renderFallback"]) && string.IsNullOrEmpty((string)contacts[0]["sourceFallback"]);
    bool r9Observed = mode == "off" ? (int)report["r9Submitted"] == 0 && frames.All(f => (int)f["r9Active"] == 0) : (int)report["r9Submitted"] == 1 && frames.Any(f => (int)f["r9Active"] > 0) && string.IsNullOrEmpty(LightningR9CandidateFx.LastFailure);
    LightningR9Require(readinessErrors.Count == 0 && readiness.Count > 0, "Readiness observation failed or the resource gate was not observed in this fresh process.");
    LightningR9Require(!fatalLog && routeClean && r9Observed, "Runtime error, fallback, late R9 failure or admission/activity mismatch; inspect diagnostic evidence.");
    LightningR9Require(expectedRenderer, "Requested renderer was not observed; inspect contacts, renderFallback and r9LastFailure before making any visual comparison.");
   } finally {
    LightningR9CandidateFx.PlacementForReview -= observeR9Placement;
    LightningDischargeFx.ReadinessForReview -= observeLegacyReadiness;
    LightningR9CandidateFx.ReadinessForReview -= observeR9Readiness;
    report["capturedFrameCount"] = frames.Count;
    report["recordingCaptureAttemptCount"] = attempts.Count;
    ResolvedCombatFx.PresentedForReview -= observe;
    Application.logMessageReceived -= observeLog;
    if (CombatTimingProbe.Enabled) CombatTimingProbe.Save(Path.Combine(_dir, "contact-timing.json"));
   }
  }
 }
}
