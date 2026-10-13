using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using DeckRogue.GodotPort.Core;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using Newtonsoft.Json;

namespace DeckRogue.GodotPort.Debugging;

[ScriptPath("res://scripts/debug/AgentCapture.cs")]
public class AgentCapture : Node
{
	public new class MethodName : Node.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";

		public new static readonly StringName _UnhandledKeyInput = "_UnhandledKeyInput";

		public static readonly StringName Capture = "Capture";
	}

	public new class PropertyName : Node.PropertyName
	{
		public static readonly StringName IsCapturing = "IsCapturing";

		public static readonly StringName LastCapturePath = "LastCapturePath";

		public static readonly StringName LastCaptureError = "LastCaptureError";

		public static readonly StringName OutputDirectory = "OutputDirectory";

		public static readonly StringName CaptureViewportOverride = "CaptureViewportOverride";

		public static readonly StringName _session = "_session";

		public static readonly StringName _writing = "_writing";

		public static readonly StringName _sequence = "_sequence";
	}

	public new class SignalName : Node.SignalName
	{
	}

	private GameSession? _session;

	private Func<bool>? _busy;

	private bool _writing;

	private int _sequence;

	public bool IsCapturing => _writing;

	public string? LastCapturePath { get; private set; }

	public string? LastCaptureError { get; private set; }

	public string OutputDirectory { get; set; } = "user://qa-captures";

	public Viewport? CaptureViewportOverride { get; set; }

	public void Bind(GameSession session, Func<bool> presentationBusy)
	{
		_session = session;
		_busy = presentationBusy;
	}

	public override void _Ready()
	{
		if (OS.HasFeature("editor") && OutputDirectory == "user://qa-captures")
		{
			OutputDirectory = "res://qa/captures";
		}
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string arg in cmdlineUserArgs)
		{
			if (arg.StartsWith("--capture-dir=", StringComparison.Ordinal))
			{
				string text = arg;
				OutputDirectory = text.Substring(14, text.Length - 14);
			}
		}
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: not false, Echo: false } inputEventKey && inputEventKey.Keycode == Key.F12)
		{
			Capture("manual");
			GetViewport().SetInputAsHandled();
		}
	}

	public async void Capture(string label)
	{
		if (_writing)
		{
			return;
		}
		_writing = true;
		LastCapturePath = null;
		LastCaptureError = null;
		try
		{
			if (DisplayServer.GetName() == "headless" || RenderingServer.GetVideoAdapterName().Contains("Dummy", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Real viewport capture is unavailable with the dummy/headless renderer.");
			}
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			Viewport capturedViewport = CaptureViewportOverride ?? GetViewport();
			if (!GodotObject.IsInstanceValid(capturedViewport))
			{
				throw new InvalidOperationException("Capture viewport is no longer valid.");
			}
			using Image image = capturedViewport.GetTexture().GetImage();
			if (image?.IsEmpty() ?? true)
			{
				throw new InvalidOperationException("Viewport image was empty.");
			}
			string directory = ProjectSettings.GlobalizePath(OutputDirectory);
			if (DirAccess.MakeDirRecursiveAbsolute(directory) != Error.Ok)
			{
				throw new InvalidOperationException("Could not create capture directory.");
			}
			string clean = new string(label.Where(delegate(char c)
			{
				bool flag = char.IsLetterOrDigit(c);
				bool flag2 = flag;
				if (!flag2)
				{
					bool flag3 = ((c == '-' || c == '_') ? true : false);
					flag2 = flag3;
				}
				return flag2;
			}).ToArray());
			string[] obj = new string[5]
			{
				DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"),
				"_",
				null,
				null,
				null
			};
			obj[2] = (++_sequence).ToString("D3");
			obj[3] = "_";
			obj[4] = clean;
			string name = string.Concat(obj);
			string path = directory.PathJoin(name + ".png");
			if (image.SavePng(path) != Error.Ok)
			{
				throw new InvalidOperationException("PNG write failed.");
			}
			byte[] bytes = File.ReadAllBytes(path);
			File.WriteAllText(contents: JsonConvert.SerializeObject(new
			{
				capturedUtc = DateTime.UtcNow.ToString("O"),
				screenshot = name + ".png",
				sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
				width = image.GetWidth(),
				height = image.GetHeight(),
				captureKind = ((CaptureViewportOverride == null) ? "window-viewport" : "subviewport-texture"),
				actualWindowWidth = GetWindow().Size.X,
				actualWindowHeight = GetWindow().Size.Y,
				capturedViewportWidth = image.GetWidth(),
				capturedViewportHeight = image.GetHeight(),
				logicalViewportWidth = capturedViewport.GetVisibleRect().Size.X,
				logicalViewportHeight = capturedViewport.GetVisibleRect().Size.Y,
				imageResized = false,
				renderFrames = Godot.Engine.GetFramesDrawn(),
				processFrames = Godot.Engine.GetProcessFrames(),
				renderer = RenderingServer.GetVideoAdapterName(),
				display = DisplayServer.GetName(),
				renderingMethod = RenderingServer.GetCurrentRenderingMethod(),
				renderingDriver = RenderingServer.GetCurrentRenderingDriverName(),
				revision = _session?.Revision,
				phase = _session?.CurrentRun?.Phase,
				presentationBusy = _busy?.Invoke(),
				contentFingerprint = ((_session?.IsInitialized ?? false) ? _session.Catalog.Fingerprint : null),
				softwareRenderingIsNotAndroidPerformanceEvidence = true
			}, Formatting.Indented), path: directory.PathJoin(name + ".json"));
			LastCapturePath = path;
			GD.Print("DECKROGUE_CAPTURE_OK " + path);
		}
		catch (Exception ex)
		{
			Exception e = ex;
			LastCaptureError = e.Message;
			GD.PushWarning("DECKROGUE_CAPTURE_FAILED " + e.Message);
		}
		finally
		{
			_writing = false;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		List<MethodInfo> methods = new List<MethodInfo>(3);
		methods.Add(new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName._UnhandledKeyInput, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "event", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("InputEvent"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Capture, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "label", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		return methods;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName._Ready && args.Count == 0)
		{
			_Ready();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._UnhandledKeyInput && args.Count == 1)
		{
			_UnhandledKeyInput(VariantUtils.ConvertTo<InputEvent>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Capture && args.Count == 1)
		{
			Capture(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default(godot_variant);
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
		if (method == MethodName._UnhandledKeyInput)
		{
			return true;
		}
		if (method == MethodName.Capture)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.LastCapturePath)
		{
			LastCapturePath = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName.LastCaptureError)
		{
			LastCaptureError = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName.OutputDirectory)
		{
			OutputDirectory = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName.CaptureViewportOverride)
		{
			CaptureViewportOverride = VariantUtils.ConvertTo<Viewport>(in value);
			return true;
		}
		if (name == PropertyName._session)
		{
			_session = VariantUtils.ConvertTo<GameSession>(in value);
			return true;
		}
		if (name == PropertyName._writing)
		{
			_writing = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._sequence)
		{
			_sequence = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName.IsCapturing)
		{
			value = VariantUtils.CreateFrom<bool>(IsCapturing);
			return true;
		}
		string from;
		if (name == PropertyName.LastCapturePath)
		{
			from = LastCapturePath;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.LastCaptureError)
		{
			from = LastCaptureError;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.OutputDirectory)
		{
			from = OutputDirectory;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.CaptureViewportOverride)
		{
			value = VariantUtils.CreateFrom<Viewport>(CaptureViewportOverride);
			return true;
		}
		if (name == PropertyName._session)
		{
			value = VariantUtils.CreateFrom(in _session);
			return true;
		}
		if (name == PropertyName._writing)
		{
			value = VariantUtils.CreateFrom(in _writing);
			return true;
		}
		if (name == PropertyName._sequence)
		{
			value = VariantUtils.CreateFrom(in _sequence);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		List<PropertyInfo> properties = new List<PropertyInfo>();
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._session, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._writing, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._sequence, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.IsCapturing, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.LastCapturePath, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.LastCaptureError, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.OutputDirectory, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName.CaptureViewportOverride, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		return properties;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.LastCapturePath, Variant.From<string>(LastCapturePath));
		info.AddProperty(PropertyName.LastCaptureError, Variant.From<string>(LastCaptureError));
		info.AddProperty(PropertyName.OutputDirectory, Variant.From<string>(OutputDirectory));
		info.AddProperty(PropertyName.CaptureViewportOverride, Variant.From<Viewport>(CaptureViewportOverride));
		info.AddProperty(PropertyName._session, Variant.From(in _session));
		info.AddProperty(PropertyName._writing, Variant.From(in _writing));
		info.AddProperty(PropertyName._sequence, Variant.From(in _sequence));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.LastCapturePath, out var _value_LastCapturePath))
		{
			LastCapturePath = _value_LastCapturePath.As<string>();
		}
		if (info.TryGetProperty(PropertyName.LastCaptureError, out var _value_LastCaptureError))
		{
			LastCaptureError = _value_LastCaptureError.As<string>();
		}
		if (info.TryGetProperty(PropertyName.OutputDirectory, out var _value_OutputDirectory))
		{
			OutputDirectory = _value_OutputDirectory.As<string>();
		}
		if (info.TryGetProperty(PropertyName.CaptureViewportOverride, out var _value_CaptureViewportOverride))
		{
			CaptureViewportOverride = _value_CaptureViewportOverride.As<Viewport>();
		}
		if (info.TryGetProperty(PropertyName._session, out var _value__session))
		{
			_session = _value__session.As<GameSession>();
		}
		if (info.TryGetProperty(PropertyName._writing, out var _value__writing))
		{
			_writing = _value__writing.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._sequence, out var _value__sequence))
		{
			_sequence = _value__sequence.As<int>();
		}
	}
}
