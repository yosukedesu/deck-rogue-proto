using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI;

[ScriptPath("res://scripts/ui/EnemyDropTarget.cs")]
public class EnemyDropTarget : Button
{
	public new class MethodName : Button.MethodName
	{
		public static readonly StringName Resolve = "Resolve";

		public static readonly StringName ConsumeInspection = "ConsumeInspection";

		public new static readonly StringName _GuiInput = "_GuiInput";

		public new static readonly StringName _Process = "_Process";

		public new static readonly StringName _CanDropData = "_CanDropData";

		public new static readonly StringName _DropData = "_DropData";
	}

	public new class PropertyName : Button.PropertyName
	{
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

	private double _held;

	private Vector2 _pressAt;

	public Func<Variant, string?>? ResolveCard { get; set; }

	public Action<string>? CardDropped { get; set; }

	public Action? Inspect { get; set; }

	public Func<bool>? IsBlocked { get; set; }

	private string? Resolve(Variant data)
	{
		if (ResolveCard == null)
		{
			if (data.VariantType != Variant.Type.String)
			{
				return null;
			}
			return data.AsString();
		}
		return ResolveCard(data);
	}

	public bool ConsumeInspection()
	{
		bool inspected = _inspected;
		_inspected = false;
		return inspected;
	}

	public override void _GuiInput(InputEvent evt)
	{
		if (Disabled)
		{
			return;
		}
		Func<bool> isBlocked = IsBlocked;
		if (isBlocked != null && isBlocked())
		{
			return;
		}
		if (evt is InputEventMouseButton button)
		{
			if (button.ButtonIndex == MouseButton.Right && button.Pressed)
			{
				Inspect?.Invoke();
				AcceptEvent();
			}
			else if (button.ButtonIndex == MouseButton.Left)
			{
				_holding = button.Pressed;
				if (_holding)
				{
					_held = 0.0;
					_inspected = false;
					_pressAt = button.Position;
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
			_held += delta;
			if (_held >= 0.5)
			{
				_holding = false;
				_inspected = true;
				Inspect?.Invoke();
			}
		}
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		int result;
		if (!Disabled)
		{
			Func<bool> isBlocked = IsBlocked;
			if (isBlocked == null || !isBlocked())
			{
				result = ((Resolve(data) != null) ? 1 : 0);
				goto IL_0029;
			}
		}
		result = 0;
		goto IL_0029;
		IL_0029:
		return (byte)result != 0;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (_CanDropData(atPosition, data))
		{
			CardDropped?.Invoke(Resolve(data));
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(6)
		{
			new MethodInfo(MethodName.Resolve, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Nil, "data", PropertyHint.None, "", PropertyUsageFlags.Default | PropertyUsageFlags.NilIsVariant, exported: false)
			}, null),
			new MethodInfo(MethodName.ConsumeInspection, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._GuiInput, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "evt", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("InputEvent"), exported: false)
			}, null),
			new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName._CanDropData, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Vector2, "atPosition", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Nil, "data", PropertyHint.None, "", PropertyUsageFlags.Default | PropertyUsageFlags.NilIsVariant, exported: false)
			}, null),
			new MethodInfo(MethodName._DropData, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Vector2, "atPosition", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Nil, "data", PropertyHint.None, "", PropertyUsageFlags.Default | PropertyUsageFlags.NilIsVariant, exported: false)
			}, null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.Resolve && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(Resolve(VariantUtils.ConvertTo<Variant>(in args[0])));
			return true;
		}
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
		if (method == MethodName._CanDropData && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<bool>(_CanDropData(VariantUtils.ConvertTo<Vector2>(in args[0]), VariantUtils.ConvertTo<Variant>(in args[1])));
			return true;
		}
		if (method == MethodName._DropData && args.Count == 2)
		{
			_DropData(VariantUtils.ConvertTo<Vector2>(in args[0]), VariantUtils.ConvertTo<Variant>(in args[1]));
			ret = default;
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName.Resolve)
		{
			return true;
		}
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
		if (method == MethodName._CanDropData)
		{
			return true;
		}
		if (method == MethodName._DropData)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
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
			_held = VariantUtils.ConvertTo<double>(in value);
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
			new PropertyInfo(Variant.Type.Vector2, PropertyName._pressAt, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName._holding, Variant.From(in _holding));
		info.AddProperty(PropertyName._inspected, Variant.From(in _inspected));
		info.AddProperty(PropertyName._held, Variant.From(in _held));
		info.AddProperty(PropertyName._pressAt, Variant.From(in _pressAt));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
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
			_held = _value__held.As<double>();
		}
		if (info.TryGetProperty(PropertyName._pressAt, out var _value__pressAt))
		{
			_pressAt = _value__pressAt.As<Vector2>();
		}
	}
}
