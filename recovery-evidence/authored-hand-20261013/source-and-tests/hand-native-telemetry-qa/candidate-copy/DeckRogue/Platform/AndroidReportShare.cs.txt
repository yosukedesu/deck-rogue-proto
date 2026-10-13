using System;
using Godot;

namespace DeckRogue.Platform;

public static class AndroidReportShare
{
	private const string SingletonName = "DeckRogueReportShare";

	private static GodotObject? _connectedPlugin;

	private static readonly Callable ResultCallback = Callable.From<bool, string>(OnShareRequestFinished);

	public static bool IsAvailable => OS.HasFeature("android") && Godot.Engine.HasSingleton("DeckRogueReportShare");

	public static event Action<bool, string>? ShareRequestFinished;

	public static bool TryShare(string reportMarkdown, string saveJson, string baseName, out string message)
	{
		if (!IsAvailable)
		{
			message = "Android report sharing is unavailable in this build. Save the report and JSON separately instead.";
			return false;
		}
		if (string.IsNullOrWhiteSpace(reportMarkdown) || string.IsNullOrWhiteSpace(saveJson))
		{
			message = "Both a report and its save JSON are required for sharing.";
			return false;
		}
		try
		{
			GodotObject plugin = Godot.Engine.GetSingleton("DeckRogueReportShare");
			if (_connectedPlugin != plugin)
			{
				if (!plugin.IsConnected("share_request_finished", ResultCallback))
				{
					Error error = plugin.Connect("share_request_finished", ResultCallback);
					if (error != Error.Ok)
					{
						throw new InvalidOperationException("Could not listen for Android share results: " + error);
					}
				}
				_connectedPlugin = plugin;
			}
			string errorText = plugin.Call("shareReports", reportMarkdown, saveJson, baseName ?? "deckrogue-report").AsString();
			if (!string.IsNullOrEmpty(errorText))
			{
				message = errorText;
				return false;
			}
			message = "Opening Android share chooser. Choose an app to send the report and save.";
			return true;
		}
		catch (Exception ex)
		{
			message = "Could not open Android sharing: " + ex.Message;
			return false;
		}
	}

	private static void OnShareRequestFinished(bool opened, string message)
	{
		if (AndroidReportShare.ShareRequestFinished == null)
		{
			return;
		}
		Delegate[] invocationList = AndroidReportShare.ShareRequestFinished.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			Action<bool, string> listener = (Action<bool, string>)invocationList[i];
			try
			{
				listener(opened, message);
			}
			catch (Exception ex)
			{
				GD.PushWarning("Android share result handler: " + ex.Message);
			}
		}
	}
}
