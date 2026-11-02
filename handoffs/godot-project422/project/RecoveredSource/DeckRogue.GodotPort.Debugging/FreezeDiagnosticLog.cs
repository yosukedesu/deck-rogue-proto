using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace DeckRogue.GodotPort.Debugging;

// Deliberately excludes save JSON, notes, names, error messages, stack traces and file paths.
public readonly record struct FreezeDiagnosticState(
    string? CardId = null, string? Phase = null, string? CombatPhase = null,
    int Target = -1, string? Selection = null, int SelectionCount = 0,
    int SelectionFlags = 0, int PendingPresentation = 0, int PendingCommands = 0,
    bool Locked = false, bool Busy = false, bool Presenting = false,
    bool TimelineBusy = false, bool QueueBusy = false, long Revision = 0,
    long PresentationTimeMs = -1, int PendingActorContacts = 0,
    int ActiveMotionCount = 0, int ActivePoseCount = 0, string? HeroPoseName = null,
    int HeroPoseFrame = -1, int HeroPoseStartCount = 0, int HeroMotionVersion = 0);

public interface IFreezeDiagnosticStorage
{
    string? ReadBounded(string name, int maxBytes);
    void WriteAtomic(string name, byte[] bytes);
}

public sealed class FreezeDiagnosticFiles : IFreezeDiagnosticStorage
{
    private readonly string _directory;
    public FreezeDiagnosticFiles(string directory) { _directory = directory; }

    public string? ReadBounded(string name, int maxBytes)
    {
        string path = Path.Combine(_directory, name);
        if (!File.Exists(path)) return null;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > maxBytes) throw new InvalidDataException();
        byte[] bytes = new byte[maxBytes + 1];
        int length = 0, read;
        while (length < bytes.Length && (read = stream.Read(bytes, length, bytes.Length - length)) != 0) length += read;
        if (length > maxBytes) throw new InvalidDataException();
        return new UTF8Encoding(false, true).GetString(bytes, 0, length);
    }

    public void WriteAtomic(string name, byte[] bytes)
    {
        if (bytes.Length > FreezeDiagnosticLog.MaxBytes) throw new InvalidDataException();
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, name), temporary = path + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            // Same-directory rename: a failed write never truncates the last complete log.
            File.Move(temporary, path, true);
        }
        catch
        {
            try { File.Delete(temporary); } catch { }
            throw;
        }
    }
}

// One coalescing writer, no per-frame sampling and no filesystem work on gameplay threads.
// Each current/previous snapshot is independently bounded; at most one bounded temp file exists.
public sealed class FreezeDiagnosticLog : IDisposable
{
    public const int MaxRecords = 128;
    public const int MaxBytes = 64 * 1024;
    public const string CurrentFile = "freeze-current-v2.log";
    public const string PreviousFile = "freeze-previous-v2.log";
    private const string LegacyHeader = "DeckRogue freeze diagnostic v2\nseq\tms\tmethod\tcard\tphase\tcombat\ttarget\tselection\tselected\tselectionFlags\tpending\tcommands\tlocked\tbusy\tpresenting\ttimeline\tqueue\trevision\tresult\texception\torigin\tpresentationTimeMs\tpendingActorContacts\tactiveMotionCount\tactivePoseCount\theroPoseName\theroPoseFrame\theroPoseStartCount\theroMotionVersion\n";
    // Existing filenames are stable storage slots. New readers accept both v2 and v3 text.
    // Assembly identity is runtime-loaded code identity, never an APK version or device ID.
    private const string VersionHeader = "DeckRogue freeze diagnostic v3\n";
    private const string RetentionHeader = "retention\tfirst-exception-pinned\n";
    private static readonly string Columns = LegacyHeader.Substring(LegacyHeader.IndexOf('\n') + 1);
    private readonly string _header;
    private string? _firstFault;
    private readonly object _gate = new object();
    private readonly Queue<string> _records = new Queue<string>();
    private readonly IFreezeDiagnosticStorage _storage;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly AutoResetEvent _wake = new AutoResetEvent(false);
    private readonly Thread _writer;
    private bool _stopped, _rotationFailed;
    private int _bytes;
    private long _sequence;
    private bool _scheduled, _dirty, _initialized, _storageFailed;
    private string _previous = "";
    private string _lastException = "";
    private long _lastExceptionAt = -1000;

    public FreezeDiagnosticLog(IFreezeDiagnosticStorage storage)
    {
        _storage = storage;
        _header = BuildHeader();
        _bytes = Encoding.UTF8.GetByteCount(_header);
        _writer = new Thread(WriterLoop) { IsBackground = true, Name = "FreezeDiagnostics" };
        _writer.Start();
    }

    private void WriterLoop()
    {
        try
        {
            while (true)
            {
                _wake.WaitOne();
                lock (_gate) { if (_stopped && !_scheduled) return; }
                Persist();
                lock (_gate) { if (_stopped) return; }
            }
        }
        catch { lock (_gate) { _storageFailed = true; _scheduled = false; Monitor.PulseAll(_gate); } }
    }

    public void Dispose()
    {
        try
        {
            WaitForIdle(150);
            lock (_gate) { _stopped = true; _wake.Set(); }
        }
        catch { }
    }
    public bool StorageFailed { get { lock (_gate) return _storageFailed; } }
    public bool Initialized { get { lock (_gate) return _initialized; } }
    public int RecordCount { get { lock (_gate) return RecordCountLocked; } }
    private int RecordCountLocked => _records.Count + (_firstFault == null ? 0 : 1);
    public string CurrentText { get { lock (_gate) return SnapshotLocked(); } }
    public string PreviousText { get { lock (_gate) return _previous; } }

    public void Record(string method, FreezeDiagnosticState state = default, bool? success = null, Exception? exception = null)
    {
        try
        {
            string type = Identifier(exception?.GetType().Name);
            string origin = Identifier(exception?.TargetSite?.Name);
            lock (_gate)
            {
                if (_stopped) return;
                long elapsed = _clock.ElapsedMilliseconds;
                string key = Identifier(method) + ":" + type;
                if (exception != null && key == _lastException && elapsed - _lastExceptionAt < 1000) return;
                if (exception != null) { _lastException = key; _lastExceptionAt = elapsed; }
                string row = string.Join('\t', (++_sequence).ToString(CultureInfo.InvariantCulture), elapsed.ToString(CultureInfo.InvariantCulture),
                    Identifier(method), Identifier(state.CardId), Identifier(state.Phase), Identifier(state.CombatPhase),
                    Number(state.Target), Identifier(state.Selection), Number(state.SelectionCount), Number(state.SelectionFlags),
                    Number(state.PendingPresentation), Number(state.PendingCommands), Flag(state.Locked), Flag(state.Busy),
                    Flag(state.Presenting), Flag(state.TimelineBusy), Flag(state.QueueBusy), state.Revision.ToString(CultureInfo.InvariantCulture),
                    success.HasValue ? Flag(success.Value) : "-", type, origin,
                    state.PresentationTimeMs.ToString(CultureInfo.InvariantCulture), Number(state.PendingActorContacts),
                    Number(state.ActiveMotionCount), Number(state.ActivePoseCount), Identifier(state.HeroPoseName),
                    Number(state.HeroPoseFrame), Number(state.HeroPoseStartCount), Number(state.HeroMotionVersion)) + "\n";
                int size = Encoding.UTF8.GetByteCount(row);
                // The first exception keeps its exact pre-recovery state. It consumes one
                // ordinary record slot and bytes; recovery spam only evicts the recent ring.
                while (_records.Count > 0 && (RecordCountLocked >= MaxRecords || _bytes + size > MaxBytes))
                    _bytes -= Encoding.UTF8.GetByteCount(_records.Dequeue());
                if (_bytes + size <= MaxBytes)
                {
                    if (exception != null && _firstFault == null) _firstFault = row;
                    else _records.Enqueue(row);
                    _bytes += size;
                }
                _dirty = true;
                if (_scheduled) return;
                _scheduled = true;
                try { _wake.Set(); }
                catch { _scheduled = false; _storageFailed = true; Monitor.PulseAll(_gate); }
            }
        }
        catch { /* Diagnostics must never alter game results or exception behavior. */ }
    }

    // Used only at orderly shutdown and by isolated tests; timeout never blocks gameplay.
    public bool WaitForIdle(int timeoutMilliseconds)
    {
        var timer = Stopwatch.StartNew();
        lock (_gate)
        {
            while (_scheduled)
            {
                int remaining = timeoutMilliseconds - (int)Math.Min(int.MaxValue, timer.ElapsedMilliseconds);
                if (remaining <= 0) return false;
                Monitor.Wait(_gate, remaining);
            }
            return true;
        }
    }

    private void Persist()
    {
        try
        {
            if (!Initialized)
            {
                string? current = ReadValid(CurrentFile), previous = ReadValid(PreviousFile);
                if (!string.IsNullOrEmpty(current))
                {
                    previous = current;
                    try { _storage.WriteAtomic(PreviousFile, Encoding.UTF8.GetBytes(current)); }
                    catch { MarkStorageFailed(); _rotationFailed = true; }
                }
                lock (_gate) { _previous = previous ?? ""; _initialized = true; }
            }
            while (true)
            {
                // Coalesce a burst of event boundaries into one small atomic write.
                Thread.Sleep(20);
                byte[] bytes;
                lock (_gate) { bytes = Encoding.UTF8.GetBytes(SnapshotLocked()); _dirty = false; }
                try { if (!_rotationFailed) _storage.WriteAtomic(CurrentFile, bytes); }
                catch { MarkStorageFailed(); }
                lock (_gate)
                {
                    if (_dirty) continue;
                    _scheduled = false;
                    Monitor.PulseAll(_gate);
                    return;
                }
            }
        }
        catch
        {
            lock (_gate) { _storageFailed = true; _scheduled = false; Monitor.PulseAll(_gate); }
        }
    }

    private string? ReadValid(string name)
    {
        try
        {
            string? value = _storage.ReadBounded(name, MaxBytes);
            if (value == null) return null;
            if (!ValidSnapshot(value)) { MarkStorageFailed(); return null; }
            return value;
        }
        catch { MarkStorageFailed(); return null; }
    }
    private void MarkStorageFailed() { lock (_gate) _storageFailed = true; }
    private string SnapshotLocked() { return _header + _firstFault + string.Concat(_records); }
    private static string BuildHeader()
    {
        string name = "unavailable", version = "unavailable", mvid = "unavailable";
        // Capture each field independently: one unavailable reflection property must not
        // prevent diagnosis, and never substitute project settings or an on-disk DLL.
        try { name = Identifier(typeof(FreezeDiagnosticLog).Assembly.GetName().Name); } catch { }
        try { version = Identifier(typeof(FreezeDiagnosticLog).Assembly.GetName().Version?.ToString()); } catch { }
        try { mvid = typeof(FreezeDiagnosticLog).Assembly.ManifestModule.ModuleVersionId.ToString("D"); } catch { }
        return VersionHeader + "loadedDllAssembly\t" + name + "\n"
            + "loadedDllAssemblyVersion\t" + version + "\n"
            + "loadedDllMvidRuntime\t" + mvid + "\n" + RetentionHeader + Columns;
    }
    private static string Number(int n) { return n.ToString(CultureInfo.InvariantCulture); }
    private static string Flag(bool value) { return value ? "1" : "0"; }
    private static string Identifier(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "-";
        // Reject (rather than partially retain) unexpected free text or private paths.
        if (value.Length > 64) return "invalid";
        foreach (char c in value)
            if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '_' && c != '-' && c != '.') return "invalid";
        return value;
    }
    private static bool ReadMetadata(string text, ref int offset, string key, out string value)
    {
        value = "";
        int end = text.IndexOf('\n', offset);
        if (end < 0 || end - offset > 96) return false;
        string line = text.Substring(offset, end - offset);
        string prefix = key + "\t";
        if (!line.StartsWith(prefix, StringComparison.Ordinal)) return false;
        value = line.Substring(prefix.Length);
        offset = end + 1;
        return value.Length > 0 && value.Length <= 64;
    }
    private static bool ValidSnapshot(string text)
    {
        if (Encoding.UTF8.GetByteCount(text) > MaxBytes || !text.EndsWith('\n')) return false;
        int offset;
        if (text.StartsWith(LegacyHeader, StringComparison.Ordinal)) offset = LegacyHeader.Length;
        else
        {
            if (!text.StartsWith(VersionHeader, StringComparison.Ordinal)) return false;
            offset = VersionHeader.Length;
            if (!ReadMetadata(text, ref offset, "loadedDllAssembly", out string name) || name != Identifier(name)) return false;
            if (!ReadMetadata(text, ref offset, "loadedDllAssemblyVersion", out string version)
                || (version != "unavailable" && version != "-" && !Version.TryParse(version, out _))) return false;
            if (!ReadMetadata(text, ref offset, "loadedDllMvidRuntime", out string mvid)
                || (mvid != "unavailable" && (!Guid.TryParseExact(mvid, "D", out var parsed) || parsed.ToString("D") != mvid))) return false;
            string remainder = RetentionHeader + Columns;
            if (!text.AsSpan(offset).StartsWith(remainder.AsSpan(), StringComparison.Ordinal)) return false;
            offset += remainder.Length;
        }
        string[] rows = text.Substring(offset).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (rows.Length > MaxRecords) return false;
        foreach (string row in rows)
        {
            string[] cells = row.Split('\t');
            if (cells.Length != 29) return false;
            for (int i = 0; i < cells.Length; i++)
            {
                bool id = i == 2 || i == 3 || i == 4 || i == 5 || i == 7 || i == 19 || i == 20 || i == 25;
                if (id) { if (cells[i] != Identifier(cells[i])) return false; }
                else if (i == 18) { if (cells[i] != "-" && cells[i] != "0" && cells[i] != "1") return false; }
                else if (!long.TryParse(cells[i], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _)) return false;
            }
        }
        return true;
    }
}
