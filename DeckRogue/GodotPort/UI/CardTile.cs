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
	}

	public new class PropertyName : Button.PropertyName
	{
		public static readonly StringName CardUid = "CardUid";

		public static readonly StringName AllowDrag = "AllowDrag";

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

	public string CardUid { get; set; } = "";

	public bool AllowDrag { get; set; }

	public Action? Inspect { get; set; }

	public Action? SecondaryAction { get; set; }

	public bool ConsumeInspection()
	{
		bool value = _inspected;
		_inspected = false;
		return value;
	}

	public override void _GuiInput(InputEvent evt)
	{
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
					_pressAt = button.Position;
				}
				else
				{
					_holding = false;
				}
			}
		}
		else if (evt is InputEventMouseMotion { Position: var position } && position.DistanceTo(_pressAt) > 14f)
		{
			_holding = false;
		}
	}

	public override void _Process(double delta)
	{
		if (_holding && !_inspected)
		{
			_held += (float)delta;
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
		if (!AllowDrag || base.Disabled)
		{
			return default(Variant);
		}
		Label preview = new Label
		{
			Text = base.TooltipText.Split('\n')[0],
			Modulate = new Color("f0d58a")
		};
		SetDragPreview(preview);
		return CardUid;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		List<MethodInfo> methods = new List<MethodInfo>(4);
		methods.Add(new MethodInfo(MethodName.ConsumeInspection, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName._GuiInput, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "evt", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("InputEvent"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName._GetDragData, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default | PropertyUsageFlags.NilIsVariant, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Vector2, "position", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		return methods;
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
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._Process && args.Count == 1)
		{
			_Process(VariantUtils.ConvertTo<double>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._GetDragData && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Variant>(_GetDragData(VariantUtils.ConvertTo<Vector2>(in args[0])));
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
		if (name == PropertyName.AllowDrag)
		{
			value = VariantUtils.CreateFrom<bool>(AllowDrag);
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
		List<PropertyInfo> properties = new List<PropertyInfo>();
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.CardUid, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.AllowDrag, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._holding, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._inspected, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._held, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Vector2, PropertyName._pressAt, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		return properties;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.CardUid, Variant.From<string>(CardUid));
		info.AddProperty(PropertyName.AllowDrag, Variant.From<bool>(AllowDrag));
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
