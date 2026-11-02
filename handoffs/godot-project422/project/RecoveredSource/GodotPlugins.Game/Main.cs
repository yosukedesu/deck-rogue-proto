using System;
using System.Runtime.InteropServices;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace GodotPlugins.Game;

internal static class Main
{
	[UnmanagedCallersOnly(EntryPoint = "godotsharp_game_main_init")]
	private static godot_bool InitializeFromGameProject(nint godotDllHandle, nint outManagedCallbacks, nint unmanagedCallbacks, int unmanagedCallbacksSize)
	{
		ManagedStartupLog.Install();
		ManagedStartupLog.Mark("Bootstrap.enter");
		try
		{
			DllImportResolver dllImportResolver = new GodotDllImportResolver(godotDllHandle).OnResolveDllImport;
			NativeLibrary.SetDllImportResolver(typeof(GodotObject).Assembly, dllImportResolver);
			ManagedStartupLog.Mark("Bootstrap.resolver_ready");
			NativeFuncs.Initialize(unmanagedCallbacks, unmanagedCallbacksSize);
			ManagedStartupLog.Mark("Bootstrap.native_callbacks_ready");
			ManagedCallbacks.Create(outManagedCallbacks);
			ManagedStartupLog.Mark("Bootstrap.managed_callbacks_ready");
			ScriptManagerBridge.LookupScriptsInAssembly(typeof(Main).Assembly);
			ManagedStartupLog.Mark("Bootstrap.script_lookup_complete");
			return godot_bool.True;
		}
		catch (Exception value)
		{
			ManagedStartupLog.Failure("Bootstrap.exception", value);
			Console.Error.WriteLine(value);
			return GodotBoolExtensions.ToGodotBool(@bool: false);
		}
	}
}

// Temporary, bounded, private diagnostics. No native Godot API is used before
// Main._Ready; the launcher may provide its private directory through the env.
internal static class ManagedStartupLog
{
 private const int MaxLogBytes = 256 * 1024;
 private const int MaxPendingBytes = 32 * 1024;
 private static readonly object Gate = new();
 private static readonly System.Collections.Generic.Queue<string> Pending = new();
 private static readonly System.Text.UTF8Encoding Utf8 = new(false);
 private static int _installed, _pendingBytes, _writtenBytes;
 private static string? _path;
 [ThreadStatic] private static bool _writing;

 internal static void Install()
 {
  try {
   if(System.Threading.Interlocked.Exchange(ref _installed, 1) != 0) return;
   AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
   System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
   TryConfigureDirectory(System.Environment.GetEnvironmentVariable("DECKROGUE_DIAGNOSTICS_DIR"));
  } catch { }
 }

 // Called only after Godot has entered the application's _Ready callback.
 internal static void ConfigureFromGodot()
 {
  try {
   Install();
   lock(Gate) { if(_path != null) return; }
   TryConfigureDirectory(ProjectSettings.GlobalizePath("user://diagnostics"));
  } catch { }
 }

 private static void TryConfigureDirectory(string? directory)
 {
  if(string.IsNullOrWhiteSpace(directory) || !System.IO.Path.IsPathFullyQualified(directory)) return;
  try {
   lock(Gate) {
    if(_path != null) return;
    System.IO.Directory.CreateDirectory(directory);
    string path = System.IO.Path.Combine(directory, "managed-startup.log");
    if(System.IO.File.Exists(path)) System.IO.File.Copy(path, path + ".1", overwrite: true);
    using(var stream = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite)) { stream.Flush(true); }
    _path = path;
    FlushPendingLocked();
   }
  } catch { }
 }

 internal static void Mark(string stage) => Write(stage, null);
 internal static void Failure(string stage, Exception error) => Write(stage, error);

 private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
 {
  Write(args.IsTerminating ? "AppDomain.unhandled_terminating" : "AppDomain.unhandled", args.ExceptionObject as Exception);
 }

 private static void OnUnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs args)
 {
  Write("TaskScheduler.unobserved", args.Exception);
  // Observe only: never SetObserved(), replace handlers, or change crash policy.
 }

 private static string Bounded(string? value, int limit)
 {
  if(string.IsNullOrEmpty(value)) return "";
  string text = value.Replace("\r", "\\r").Replace("\n", "\\n");
  return text.Length <= limit ? text : text.Substring(0, limit) + "[truncated]";
 }

 private static void Write(string stage, Exception? error)
 {
  if(_writing) return;
  _writing = true;
  try {
   var text = new System.Text.StringBuilder();
   text.Append("DR_MANAGED ").Append(DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture))
    .Append(" pid=").Append(System.Environment.ProcessId).Append(" thread=").Append(System.Environment.CurrentManagedThreadId)
    .Append(" stage=").Append(Bounded(stage, 160));
   for(int depth = 0; error != null && depth < 3; depth++, error = error.InnerException) {
    text.Append(" exception[").Append(depth).Append("]=").Append(error.GetType().FullName)
     .Append(" hresult=").Append(error.HResult).Append(" message=").Append(Bounded(error.Message, 2048))
     .Append(" stack=").Append(Bounded(error.StackTrace, 4096));
   }
   string line = text.ToString() + "\n";
   try { Console.Error.Write(line); Console.Error.Flush(); } catch { }
   lock(Gate) {
    if(_writtenBytes >= MaxLogBytes) return;
    int bytes = Utf8.GetByteCount(line);
    while(Pending.Count > 0 && _pendingBytes + bytes > MaxPendingBytes) _pendingBytes -= Utf8.GetByteCount(Pending.Dequeue());
    if(bytes <= MaxPendingBytes) { Pending.Enqueue(line); _pendingBytes += bytes; }
    FlushPendingLocked();
   }
  } catch { }
  finally { _writing = false; }
 }

 private static void FlushPendingLocked()
 {
  if(_path == null || Pending.Count == 0) return;
  try {
   using var stream = new System.IO.FileStream(_path, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite);
   while(Pending.Count > 0) {
    string line = Pending.Peek(); byte[] bytes = Utf8.GetBytes(line);
    if(_writtenBytes + bytes.Length > MaxLogBytes) { Pending.Clear(); _pendingBytes = 0; _writtenBytes = MaxLogBytes; break; }
    stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
    _writtenBytes += bytes.Length; Pending.Dequeue(); _pendingBytes -= bytes.Length;
   }
  } catch { }
 }
}
