using System;
using System.Collections.Generic;
using DeckRogue.Engine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Core;

public sealed class SessionSaveStore
{
	private readonly ISessionFiles _files;

	public string DirectoryPath { get; }

	public string SavePath => DirectoryPath + "/run.json";

	public string BackupPath => SavePath + ".bak";

	public string TemporaryPath => SavePath + ".tmp";

	public bool Exists
	{
		get
		{
			if (!_files.Exists(SavePath))
			{
				return _files.Exists(BackupPath);
			}
			return true;
		}
	}

	public string ReportPath => DirectoryPath + "/report.json";

	public string ReportBackupPath => ReportPath + ".bak";

	public string ReportTemporaryPath => ReportPath + ".tmp";

	public bool ReportExists
	{
		get
		{
			if (!_files.Exists(ReportPath))
			{
				return _files.Exists(ReportBackupPath);
			}
			return true;
		}
	}

	public SessionSaveStore(ISessionFiles files, string directoryPath)
	{
		_files = files;
		DirectoryPath = directoryPath.TrimEnd('/');
	}

	public SessionResult Write(string json)
	{
		return WriteVerified(json, SavePath, (string existing) => Report.ReadRunSaveFile(existing, out string _) != null, "save");
	}

	public SessionResult WriteReport(SessionReport report)
	{
		JObject json = new JObject
		{
			["kind"] = "deckrogue-report",
			["schema"] = 1,
			["saveJson"] = report.SaveJson,
			["text"] = report.Text,
			["capturedAt"] = report.CapturedAt
		};
		return WriteVerified(json.ToString(Formatting.None), ReportPath, (string existing) => ReadReport(existing) != null, "retain report");
	}

	private SessionResult WriteVerified(string json, string path, Func<string, bool> validExisting, string operation)
	{
		try
		{
			string temporary = path + ".tmp";
			string backup = path + ".bak";
			_files.EnsureDirectory(DirectoryPath);
			_files.Write(temporary, json);
			if (_files.Read(temporary) != json)
			{
				throw new InvalidOperationException("Write verification failed.");
			}
			if (_files.Exists(path))
			{
				bool valid = false;
				try
				{
					valid = validExisting(_files.Read(path));
				}
				catch
				{
				}
				if (valid)
				{
					if (_files.Exists(backup))
					{
						_files.Delete(backup);
					}
					_files.Move(path, backup);
				}
				else
				{
					_files.Delete(path);
				}
			}
			_files.Move(temporary, path);
			return SessionResult.Ok();
		}
		catch (Exception ex)
		{
			return SessionResult.Fail("Could not " + operation + ": " + ex.Message);
		}
	}

	public IEnumerable<(string Json, bool IsBackup)> ReadCandidates(Action<string> readError)
	{
		return ReadCandidates(SavePath, readError);
	}

	public IEnumerable<(SessionReport Report, bool IsBackup)> ReadReportCandidates(Action<string> readError)
	{
		foreach (var candidate in ReadCandidates(ReportPath, readError))
		{
			SessionReport report = null;
			try
			{
				report = ReadReport(candidate.Json);
			}
			catch (Exception ex)
			{
				readError(ex.Message);
			}
			if (report != null)
			{
				yield return (Report: report, IsBackup: candidate.IsBackup);
			}
		}
	}

	private IEnumerable<(string Json, bool IsBackup)> ReadCandidates(string primary, Action<string> readError)
	{
		string[] array = new string[2]
		{
			primary,
			primary + ".bak"
		};
		string[] array2 = array;
		foreach (string path in array2)
		{
			string json = null;
			try
			{
				if (_files.Exists(path))
				{
					json = _files.Read(path);
				}
			}
			catch (Exception ex)
			{
				readError(ex.Message);
			}
			if (json != null)
			{
				yield return (Json: json, IsBackup: path != primary);
			}
		}
	}

	private static SessionReport ReadReport(string json)
	{
		JObject root = SessionJson.ParseObject(json);
		if (!((string?)root["kind"] != "deckrogue-report") && (int?)root["schema"] == 1)
		{
			JToken jToken = root["saveJson"];
			if (jToken != null && jToken.Type == JTokenType.String)
			{
				JToken jToken2 = root["text"];
				if (jToken2 != null && jToken2.Type == JTokenType.String)
				{
					JToken jToken3 = root["capturedAt"];
					if (jToken3 != null && jToken3.Type == JTokenType.String)
					{
						string save = (string?)root["saveJson"];
						string text = (string?)root["text"];
						string at = (string?)root["capturedAt"];
						if (string.IsNullOrWhiteSpace(text) || Report.ReadRunSaveFile(save, out string _) == null)
						{
							throw new InvalidOperationException("The retained report snapshot could not be read.");
						}
						return new SessionReport(save, text, at);
					}
				}
			}
		}
		throw new InvalidOperationException("The retained report is incomplete.");
	}

	public SessionResult Clear()
	{
		try
		{
			string[] array = new string[3] { SavePath, BackupPath, TemporaryPath };
			foreach (string path in array)
			{
				if (_files.Exists(path))
				{
					_files.Delete(path);
				}
			}
			return SessionResult.Ok();
		}
		catch (Exception ex)
		{
			return SessionResult.Fail("Could not clear save: " + ex.Message);
		}
	}
}
