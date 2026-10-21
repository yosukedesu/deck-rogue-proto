using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI;

[ScriptPath("res://scripts/ui/CardTile.cs")]
public class CardTile : Button
{
	public new class MethodName : Button.MethodName
	{
		public static readonly StringName ConsumeInspection = "ConsumeInspection";

		public new static readonly StringName _GuiInput = "_GuiInput";

		public new static readonly StringName _Process = "_Process";

		public new static readonly StringName _GetDragData = "_GetDragData";

		public new static readonly StringName _Notification = "_Notification";
	}

	public new class PropertyName : Button.PropertyName
	{
		public static readonly StringName CardUid = "CardUid";

		public static readonly StringName AllowDrag = "AllowDrag";

		public static readonly StringName IsBattleHand = "IsBattleHand";

		public static readonly StringName _holding = "_holding";

		public static readonly StringName _inspected = "_inspected";

		public static readonly StringName _held = "_held";

		public static readonly StringName _pressAt = "_pressAt";
	}

	public new class SignalName : Button.SignalName
	{
	}

	private bool _holding;

	private bool _inspected;

	private float _held;

	private Vector2 _pressAt;

	private ulong _pressedAtMilliseconds;

	public string CardUid { get; set; } = "";

	public bool AllowDrag { get; set; }

	public bool IsBattleHand { get; set; }

	public Action? DragStarted { get; set; }

	public Action? DragEnded { get; set; }

	public Func<Control>? DragPreview { get; set; }

	public Func<Variant>? DragPayload { get; set; }

	public Action? Inspect { get; set; }

	public Action? PreviewPressed { get; set; }

	public bool InspectionHeld => _holding;

	public Action? SecondaryAction { get; set; }

    // A real touch is owned centrally by GameUI, never by Button.Pressed or
    // Godot's emulated-mouse drag. PC mouse/keyboard behavior remains native.
    public Func<bool>? TouchOwnsInput { get; set; }

    public void SuppressTouchActivation()
    {
        _holding = false;
        _inspected = true;
    }

	public bool ConsumeInspection()
	{
		if (IsBattleHand && TouchOwnsInput?.Invoke() == true) return true;
		bool inspected = _inspected;
		_inspected = false;
		return inspected;
	}

	public override void _GuiInput(InputEvent evt)
	{
        if (IsBattleHand && evt.Device == -1 && (evt is InputEventScreenTouch || evt is InputEventScreenDrag))
        {
            AcceptEvent(); // Mouse-to-touch emulation must not mark the real mouse click inspected.
            return;
        }
        if (IsBattleHand && (evt is InputEventScreenTouch || evt is InputEventScreenDrag ||
            (evt is InputEventMouse && (evt.Device == -1 || TouchOwnsInput?.Invoke() == true))))
        {
            SuppressTouchActivation();
            AcceptEvent();
            return;
        }
		if (evt is InputEventMouseButton button)
		{
			if (button.ButtonIndex == MouseButton.Right && button.Pressed)
			{
				if (SecondaryAction != null)
				{
					SecondaryAction();
				}
				else
				{
					Inspect?.Invoke();
				}
				AcceptEvent();
			}
			else if (button.ButtonIndex == MouseButton.Left)
			{
				if (button.Pressed)
				{
					_holding = true;
					_inspected = false;
					_held = 0f;
					_pressedAtMilliseconds = Time.GetTicksMsec();
					_pressAt = GetGlobalTransformWithCanvas() * button.Position;
					PreviewPressed?.Invoke();
				}
				else
				{
					// Low frame rates must never turn a completed hold into a play.
					if (_holding && !_inspected && Time.GetTicksMsec() - _pressedAtMilliseconds >= 500)
					{
						_inspected = true;
						_holding = false;
						Inspect?.Invoke();
						AcceptEvent();
					}
					_holding = false;
				}
			}
		}
		else if (evt is InputEventMouseMotion motion && (GetGlobalTransformWithCanvas() * motion.Position).DistanceTo(_pressAt) > 14f)
		{
			_holding = false;
		}
	}

	public override void _Process(double delta)
	{
		if (_holding && !_inspected)
		{
			_held = (Time.GetTicksMsec() - _pressedAtMilliseconds) / 1000f;
			if (_held >= 0.5f)
			{
				_holding = false;
				_inspected = true;
				Inspect?.Invoke();
			}
		}
	}

	public override Variant _GetDragData(Vector2 position)
	{
		_holding = false;
		if (!AllowDrag || Disabled || (IsBattleHand && TouchOwnsInput?.Invoke() == true))
		{
			return default;
		}
		_inspected = true;
		DragStarted?.Invoke();
		Control preview = DragPreview?.Invoke() ?? new Label
		{
			Text = TooltipText.Split('\n')[0],
			Modulate = new Color("f0d58a")
		};
		SetDragPreview(preview);
		return DragPayload?.Invoke() ?? ((Variant)CardUid);
	}

	public override void _Notification(int what)
	{
		if ((long)what == 22)
		{
			_holding = false;
			DragEnded?.Invoke();
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(5)
		{
			new MethodInfo(MethodName.ConsumeInspection, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._GuiInput, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "evt", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("InputEvent"), exported: false)
			}, null),
			new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName._GetDragData, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default | PropertyUsageFlags.NilIsVariant, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Vector2, "position", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName._Notification, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "what", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.ConsumeInspection && args.Count == 0)
		{
			ret = VariantUtils.CreateFrom<bool>(ConsumeInspection());
			return true;
		}
		if (method == MethodName._GuiInput && args.Count == 1)
		{
			_GuiInput(VariantUtils.ConvertTo<InputEvent>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName._Process && args.Count == 1)
		{
			_Process(VariantUtils.ConvertTo<double>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName._GetDragData && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Variant>(_GetDragData(VariantUtils.ConvertTo<Vector2>(in args[0])));
			return true;
		}
		if (method == MethodName._Notification && args.Count == 1)
		{
			_Notification(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default;
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName.ConsumeInspection)
		{
			return true;
		}
		if (method == MethodName._GuiInput)
		{
			return true;
		}
		if (method == MethodName._Process)
		{
			return true;
		}
		if (method == MethodName._GetDragData)
		{
			return true;
		}
		if (method == MethodName._Notification)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.CardUid)
		{
			CardUid = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName.AllowDrag)
		{
			AllowDrag = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName.IsBattleHand)
		{
			IsBattleHand = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._holding)
		{
			_holding = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._inspected)
		{
			_inspected = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._held)
		{
			_held = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._pressAt)
		{
			_pressAt = VariantUtils.ConvertTo<Vector2>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName.CardUid)
		{
			value = VariantUtils.CreateFrom<string>(CardUid);
			return true;
		}
		bool from;
		if (name == PropertyName.AllowDrag)
		{
			from = AllowDrag;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.IsBattleHand)
		{
			from = IsBattleHand;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName._holding)
		{
			value = VariantUtils.CreateFrom(in _holding);
			return true;
		}
		if (name == PropertyName._inspected)
		{
			value = VariantUtils.CreateFrom(in _inspected);
			return true;
		}
		if (name == PropertyName._held)
		{
			value = VariantUtils.CreateFrom(in _held);
			return true;
		}
		if (name == PropertyName._pressAt)
		{
			value = VariantUtils.CreateFrom(in _pressAt);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Bool, PropertyName._holding, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._inspected, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._held, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector2, PropertyName._pressAt, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.CardUid, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.AllowDrag, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.IsBattleHand, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.CardUid, Variant.From<string>(CardUid));
		info.AddProperty(PropertyName.AllowDrag, Variant.From<bool>(AllowDrag));
		info.AddProperty(PropertyName.IsBattleHand, Variant.From<bool>(IsBattleHand));
		info.AddProperty(PropertyName._holding, Variant.From(in _holding));
		info.AddProperty(PropertyName._inspected, Variant.From(in _inspected));
		info.AddProperty(PropertyName._held, Variant.From(in _held));
		info.AddProperty(PropertyName._pressAt, Variant.From(in _pressAt));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.CardUid, out var _value_CardUid))
		{
			CardUid = _value_CardUid.As<string>();
		}
		if (info.TryGetProperty(PropertyName.AllowDrag, out var _value_AllowDrag))
		{
			AllowDrag = _value_AllowDrag.As<bool>();
		}
		if (info.TryGetProperty(PropertyName.IsBattleHand, out var _value_IsBattleHand))
		{
			IsBattleHand = _value_IsBattleHand.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._holding, out var _value__holding))
		{
			_holding = _value__holding.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._inspected, out var _value__inspected))
		{
			_inspected = _value__inspected.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._held, out var _value__held))
		{
			_held = _value__held.As<float>();
		}
		if (info.TryGetProperty(PropertyName._pressAt, out var _value__pressAt))
		{
			_pressAt = _value__pressAt.As<Vector2>();
		}
	}
}
