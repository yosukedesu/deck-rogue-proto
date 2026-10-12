using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI;

public partial class CardTile : Button
{






	private bool _holding;

	private bool _inspected;

	private float _held;

	private Vector2 _pressAt;

	public string CardUid { get; set; } = "";

	public bool AllowDrag { get; set; }
	public bool IsBattleHand { get; set; }
	public Action? DragStarted { get; set; }
	public Action? DragEnded { get; set; }
	public Func<Control>? DragPreview { get; set; }
	public Func<Variant>? DragPayload { get; set; }

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
        _inspected=true;
        DragStarted?.Invoke();
        Control preview=DragPreview?.Invoke() ?? new Label {Text=TooltipText.Split('\n')[0],Modulate=new Color("f0d58a")};
        SetDragPreview(preview);
        return DragPayload?.Invoke() ?? CardUid;
	}

    public override void _Notification(int what)
    {
        if(what==NotificationDragEnd){_holding=false;DragEnded?.Invoke();}
    }
}
