using System;
using System.Collections.Generic;
using System.ComponentModel;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;
using DeckRogue.GodotPort.UI;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Presentation;

[ScriptPath("res://scripts/presentation/BattleAudio.cs")]
public class BattleAudio : Node
{
	public new class MethodName : Node.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";

		public static readonly StringName EnsureBus = "EnsureBus";

		public static readonly StringName PlayDamageContact = "PlayDamageContact";

		public static readonly StringName PlayUiKey = "PlayUiKey";

		public static readonly StringName PlayKey = "PlayKey";

		public static readonly StringName StopEffects = "StopEffects";
	}

	public new class PropertyName : Node.PropertyName
	{
		public static readonly StringName DamageAudioCount = "DamageAudioCount";

		public static readonly StringName CurrentTrack = "CurrentTrack";

		public static readonly StringName MusicPlaying = "MusicPlaying";

		public static readonly StringName PlayingEffects = "PlayingEffects";

		public static readonly StringName Muted = "Muted";

		public static readonly StringName _uiVoice = "_uiVoice";

		public static readonly StringName _music = "_music";

		public static readonly StringName _musicName = "_musicName";

		public static readonly StringName _voice = "_voice";

		public static readonly StringName _variation = "_variation";
	}

	public new class SignalName : Node.SignalName
	{
	}

	private readonly List<AudioStreamPlayer> _voices = new List<AudioStreamPlayer>();

	private readonly List<AudioStreamPlayer> _uiVoices = new List<AudioStreamPlayer>();

	private int _uiVoice;

	private readonly Dictionary<string, AudioStream> _cache = new Dictionary<string, AudioStream>();

	private AudioStreamPlayer _music;

	private JObject _map = new JObject();

	private string _musicName = "";

	private int _voice;

	private int _variation;

	public int DamageAudioCount { get; private set; }

	public string CurrentTrack => _musicName;

	public bool MusicPlaying => _music?.Playing ?? false;

	public int PlayingEffects => _voices.FindAll((AudioStreamPlayer v) => v.Playing).Count;

	public bool Muted { get; set; }

	public override void _Ready()
	{
		EnsureBus("BGM");
		EnsureBus("SFX");
		_map = JObject.Parse(FileAccess.GetFileAsString("res://assets/Audio/audio.json"));
		_music = new AudioStreamPlayer
		{
			Name = "Music",
			Bus = "BGM",
			VolumeDb = -12f
		};
		AddChild(_music, forceReadableName: false, InternalMode.Disabled);
		for (int i = 0; i < 8; i++)
		{
			AudioStreamPlayer p = new AudioStreamPlayer
			{
				Name = "Sfx" + i,
				Bus = "SFX"
			};
			_voices.Add(p);
			AddChild(p, forceReadableName: false, InternalMode.Disabled);
		}
		for (int j = 0; j < 4; j++)
		{
			AudioStreamPlayer p2 = new AudioStreamPlayer
			{
				Name = "UiSfx" + j,
				Bus = "SFX"
			};
			_uiVoices.Add(p2);
			AddChild(p2, forceReadableName: false, InternalMode.Disabled);
		}
		SyncRun(null);
	}

	private static void EnsureBus(string name)
	{
		if (AudioServer.GetBusIndex(name) < 0)
		{
			AudioServer.AddBus();
			int busIdx = AudioServer.BusCount - 1;
			AudioServer.SetBusName(busIdx, name);
			AudioServer.SetBusSend(busIdx, "Master");
		}
	}

	public void SyncRun(RunState? run)
	{
		string key = (((object)run == null) ? "title" : ("act" + Math.Clamp(run.Act, 1, 3)));
		if (run?.Phase == "combat" && run.Row >= 0 && run.Row < run.Map.Count && run.Col >= 0 && run.Col < run.Map[run.Row].Count && run.Map[run.Row][run.Col].Type == "boss")
		{
			key = "boss" + run.Act;
		}
		string name = _map["bgm"]?[key]?.Value<string>();
		if (name == null || name == _musicName)
		{
			return;
		}
		string path = "res://assets/Audio/bgm/" + name + ".ogg";
		if (ResourceLoader.Exists(path))
		{
			AudioStreamOggVorbis stream = GD.Load<AudioStreamOggVorbis>(path);
			stream.Loop = true;
			_music.Stream = stream;
			_musicName = name;
			if (!Muted)
			{
				_music.Play();
			}
		}
	}

	public void PlayDamageContact(string source, int hpLoss, int absorbed, bool playerBig = false, string? definitionId = null)
	{
		if (!Muted && (hpLoss > 0 || absorbed > 0))
		{
			DamageAudioCount++;
		}
		if (hpLoss > 0)
		{
			if (source == "player" && Hd2dAttackPrototype.HasAuthoredAudio(definitionId))
                PlayKey("Hd2dContact." + Hd2dAttackPrototype.AudioCard(definitionId));
            else
                PlayKey("DamageDealt." + ((source == "enemy") ? "enemy" : "player") + (((source == "enemy") ? (hpLoss >= 12) : playerBig) ? ".big" : ""));
		}
		else if (absorbed > 0)
		{
			PlayKey("DamageDealt.blocked");
		}
	}

	public void PlayEvent(GameEvent e)
	{
		if (MergedEventCues.Supports(e.Type))
		{
			return;
		}
		if (e is GameEvent_HpHealed { Amount: var amount })
		{
			if (amount > 0)
			{
				goto IL_005b;
			}
		}
		else if (e is GameEvent_EnemyHealed { Amount: var amount2 })
		{
			if (amount2 > 0)
			{
				goto IL_005b;
			}
		}
		else if (!(e is GameEvent_RegenTicked { Amount: var amount3 }) || amount3 > 0)
		{
			goto IL_005b;
		}
		bool flag = true;
		goto IL_0061;
		IL_005b:
		flag = false;
		goto IL_0061;
		IL_0061:
		if (flag)
		{
			return;
		}
		if (e is GameEvent_ConfusedAttack confused)
		{
			if (confused.HpLoss > 0)
			{
				PlayKey((confused.HpLoss >= 15) ? "DamageDealt.enemy.big" : "DamageDealt.enemy");
			}
			else if (CombatOutcome.Read(e).Kind == "guard")
			{
				PlayKey("DamageDealt.blocked");
			}
		}
		else
		{
			string key;
			if (e is GameEvent_RetainerSacrificed)
			{
				key = "TokenDestroyed";
			}
			else
			{
				key = ((e is GameEvent_LightDischarged light && CardText.IsLightPayment(light)) ? "LightSpent" : e.Type);
			}
			if (e is GameEvent_DamageDealt d)
			{
				PlayDamageContact(d.Source, d.HpLoss, (int)CombatOutcome.Read(d).Absorbed, d.Source == "player" && (d.Amount >= 15 || d.Exposed == true));
			}
			else
			{
				PlayKey(key);
			}
		}
	}

	public void PlayUiKey(string key)
	{
		PlayKey(key, ui: true);
	}

	public void PlayKey(string key)
	{
		PlayKey(key, ui: false);
	}

	private void PlayKey(string key, bool ui)
	{
		if (Muted || _voices.Count == 0)
		{
			return;
		}
		JObject obj = _map["sfx"]?[key] as JObject;
		string name = obj?["sfx"]?.Value<string>();
		if (name != null)
		{
			string selected = name;
			int variation = 1 + _variation++ % 6;
			if (variation > 1 && ResourceLoader.Exists($"res://assets/Audio/sfx/{name}_{variation}.ogg"))
			{
				selected = name + "_" + variation;
			}
			if (!_cache.TryGetValue(selected, out var stream))
			{
				string path = "res://assets/Audio/sfx/" + selected + ".ogg";
				stream = (ResourceLoader.Exists(path) ? GD.Load<AudioStream>(path) : SourceAudioSynth.Sfx(name));
				_cache[selected] = stream;
			}
			AudioStreamPlayer audioStreamPlayer = (ui ? _uiVoices[_uiVoice++ % _uiVoices.Count] : _voices[_voice++ % _voices.Count]);
			audioStreamPlayer.Stop();
			audioStreamPlayer.Stream = stream;
			float vol = obj?["volume"]?.Value<float>() ?? 0.7f;
			audioStreamPlayer.VolumeDb = Mathf.LinearToDb(Math.Max(0.001f, vol * 0.65f));
			float jitter = (obj?["jitter"]?.Value<float>()).GetValueOrDefault();
			audioStreamPlayer.PitchScale = 1f + (float)(variation - 3) * jitter * 0.25f;
			audioStreamPlayer.Play();
		}
	}

	public void StopEffects()
	{
		foreach (AudioStreamPlayer voice in _voices)
		{
			voice.Stop();
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(7)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.EnsureBus, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.PlayDamageContact, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "source", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "hpLoss", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "absorbed", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "playerBig", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.PlayUiKey, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "key", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.PlayKey, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "key", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.PlayKey, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "key", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "ui", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.StopEffects, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null)
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
		if (method == MethodName.EnsureBus && args.Count == 1)
		{
			EnsureBus(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.PlayDamageContact && args.Count == 4)
		{
			PlayDamageContact(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]), VariantUtils.ConvertTo<int>(in args[2]), VariantUtils.ConvertTo<bool>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.PlayUiKey && args.Count == 1)
		{
			PlayUiKey(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.PlayKey && args.Count == 1)
		{
			PlayKey(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.PlayKey && args.Count == 2)
		{
			PlayKey(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<bool>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.StopEffects && args.Count == 0)
		{
			StopEffects();
			ret = default;
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.EnsureBus && args.Count == 1)
		{
			EnsureBus(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default;
			return true;
		}
		ret = default;
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName._Ready)
		{
			return true;
		}
		if (method == MethodName.EnsureBus)
		{
			return true;
		}
		if (method == MethodName.PlayDamageContact)
		{
			return true;
		}
		if (method == MethodName.PlayUiKey)
		{
			return true;
		}
		if (method == MethodName.PlayKey)
		{
			return true;
		}
		if (method == MethodName.StopEffects)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.DamageAudioCount)
		{
			DamageAudioCount = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.Muted)
		{
			Muted = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._uiVoice)
		{
			_uiVoice = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._music)
		{
			_music = VariantUtils.ConvertTo<AudioStreamPlayer>(in value);
			return true;
		}
		if (name == PropertyName._musicName)
		{
			_musicName = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._voice)
		{
			_voice = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._variation)
		{
			_variation = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		int from;
		if (name == PropertyName.DamageAudioCount)
		{
			from = DamageAudioCount;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.CurrentTrack)
		{
			value = VariantUtils.CreateFrom<string>(CurrentTrack);
			return true;
		}
		bool from2;
		if (name == PropertyName.MusicPlaying)
		{
			from2 = MusicPlaying;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.PlayingEffects)
		{
			from = PlayingEffects;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.Muted)
		{
			from2 = Muted;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName._uiVoice)
		{
			value = VariantUtils.CreateFrom(in _uiVoice);
			return true;
		}
		if (name == PropertyName._music)
		{
			value = VariantUtils.CreateFrom(in _music);
			return true;
		}
		if (name == PropertyName._musicName)
		{
			value = VariantUtils.CreateFrom(in _musicName);
			return true;
		}
		if (name == PropertyName._voice)
		{
			value = VariantUtils.CreateFrom(in _voice);
			return true;
		}
		if (name == PropertyName._variation)
		{
			value = VariantUtils.CreateFrom(in _variation);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, PropertyName._uiVoice, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._music, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._musicName, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._voice, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._variation, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.DamageAudioCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.CurrentTrack, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.MusicPlaying, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.PlayingEffects, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.Muted, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.DamageAudioCount, Variant.From<int>(DamageAudioCount));
		info.AddProperty(PropertyName.Muted, Variant.From<bool>(Muted));
		info.AddProperty(PropertyName._uiVoice, Variant.From(in _uiVoice));
		info.AddProperty(PropertyName._music, Variant.From(in _music));
		info.AddProperty(PropertyName._musicName, Variant.From(in _musicName));
		info.AddProperty(PropertyName._voice, Variant.From(in _voice));
		info.AddProperty(PropertyName._variation, Variant.From(in _variation));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.DamageAudioCount, out var _value_DamageAudioCount))
		{
			DamageAudioCount = _value_DamageAudioCount.As<int>();
		}
		if (info.TryGetProperty(PropertyName.Muted, out var _value_Muted))
		{
			Muted = _value_Muted.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._uiVoice, out var _value__uiVoice))
		{
			_uiVoice = _value__uiVoice.As<int>();
		}
		if (info.TryGetProperty(PropertyName._music, out var _value__music))
		{
			_music = _value__music.As<AudioStreamPlayer>();
		}
		if (info.TryGetProperty(PropertyName._musicName, out var _value__musicName))
		{
			_musicName = _value__musicName.As<string>();
		}
		if (info.TryGetProperty(PropertyName._voice, out var _value__voice))
		{
			_voice = _value__voice.As<int>();
		}
		if (info.TryGetProperty(PropertyName._variation, out var _value__variation))
		{
			_variation = _value__variation.As<int>();
		}
	}
}
