using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI;

public partial class EnemyDropTarget : Button
{
    public Func<Variant,string?>? ResolveCard {get;set;}
    private string? Resolve(Variant data)=>ResolveCard!=null?ResolveCard(data):data.VariantType==Variant.Type.String?data.AsString():null;







	private bool _holding;

	private bool _inspected;

	private double _held;

	private Vector2 _pressAt;

	public Action<string>? CardDropped { get; set; }

	public Action? Inspect { get; set; }

	public Func<bool>? IsBlocked { get; set; }

	public bool ConsumeInspection()
	{
		bool result = _inspected;
		_inspected = false;
		return result;
	}

	public override void _GuiInput(InputEvent evt)
	{
		if (base.Disabled)
		{
			return;
		}
		Func<bool>? isBlocked = IsBlocked;
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
		if (!base.Disabled)
		{
			Func<bool>? isBlocked = IsBlocked;
			if (isBlocked == null || !isBlocked())
			{
				result = ((Resolve(data)!=null) ? 1 : 0);
				goto IL_002d;
			}
		}
		result = 0;
		goto IL_002d;
		IL_002d:
		return (byte)result != 0;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (_CanDropData(atPosition, data))
		{
			CardDropped?.Invoke(Resolve(data)!);
		}
	}

}
