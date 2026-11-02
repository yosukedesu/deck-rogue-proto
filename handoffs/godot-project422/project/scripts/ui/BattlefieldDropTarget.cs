using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI;

[ScriptPath("res://scripts/ui/BattlefieldDropTarget.cs")]
public class BattlefieldDropTarget : Control
{
	public new class MethodName : Control.MethodName
	{
		public static readonly StringName Resolve = "Resolve";

		public new static readonly StringName _CanDropData = "_CanDropData";

		public new static readonly StringName _DropData = "_DropData";
	}

	public new class PropertyName : Control.PropertyName
	{
	}

	public new class SignalName : Control.SignalName
	{
	}

	public Func<Variant, string?>? ResolveCard { get; set; }

	public Func<string, bool>? CanPlay { get; set; }

	public Action<string, Vector2>? CardDropped { get; set; }

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

	public override bool _CanDropData(Vector2 position, Variant data)
	{
		string uid = Resolve(data);
		if (uid != null)
		{
			return CanPlay?.Invoke(uid) ?? false;
		}
		return false;
	}

	public override void _DropData(Vector2 position, Variant data)
	{
		if (_CanDropData(position, data))
		{
			CardDropped?.Invoke(Resolve(data), GetGlobalTransformWithCanvas() * position);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(3)
		{
			new MethodInfo(MethodName.Resolve, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Nil, "data", PropertyHint.None, "", PropertyUsageFlags.Default | PropertyUsageFlags.NilIsVariant, exported: false)
			}, null),
			new MethodInfo(MethodName._CanDropData, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Vector2, "position", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Nil, "data", PropertyHint.None, "", PropertyUsageFlags.Default | PropertyUsageFlags.NilIsVariant, exported: false)
			}, null),
			new MethodInfo(MethodName._DropData, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Vector2, "position", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
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
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
	}
}
