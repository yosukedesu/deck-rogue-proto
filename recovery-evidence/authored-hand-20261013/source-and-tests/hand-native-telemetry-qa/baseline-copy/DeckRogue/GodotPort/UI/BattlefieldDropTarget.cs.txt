using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI;

public partial class BattlefieldDropTarget : Control
{
    public Func<Variant,string?>? ResolveCard {get;set;}
    private string? Resolve(Variant data)=>ResolveCard!=null?ResolveCard(data):data.VariantType==Variant.Type.String?data.AsString():null;







	public Func<string, bool>? CanPlay { get; set; }

	public Action<string, Vector2>? CardDropped { get; set; }

	public override bool _CanDropData(Vector2 position, Variant data)
	{
		string uid=Resolve(data);return uid!=null && (CanPlay?.Invoke(uid) ?? false);
	}

	public override void _DropData(Vector2 position, Variant data)
	{
		if (_CanDropData(position, data))
		{
			CardDropped?.Invoke(Resolve(data)!, GetGlobalTransformWithCanvas() * position);
		}
	}

}
