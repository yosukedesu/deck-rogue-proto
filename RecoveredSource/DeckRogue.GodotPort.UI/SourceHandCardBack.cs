using Godot;

namespace DeckRogue.GodotPort.UI;

public static class SourceHandCardBack
{
	public const string NodeName = "SourceHandDealBack";

	private static Texture2D _art;

	private static bool _loaded;

	private static ShaderMaterial _rounded;

	public static Control Add(CardTile tile)
	{
		Remove(tile);
		Control back = new Control
		{
			Name = "SourceHandDealBack",
			Size = new Vector2(200f, 290f),
			Scale = new Vector2(SourceCardFace.W / 200f, SourceCardFace.H / 290f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		tile.AddChild(back, forceReadableName: false, Node.InternalMode.Disabled);
		if (!_loaded)
		{
			_loaded = true;
			if (ResourceLoader.Exists("res://assets/Art/ui/cardback.png"))
			{
				_art = GD.Load<Texture2D>("res://assets/Art/ui/cardback.png");
			}
		}
		if (_art != null)
		{
			if (_rounded == null)
			{
				_rounded = new ShaderMaterial
				{
					Shader = new Shader
					{
						Code = "\nshader_type canvas_item;\nvoid fragment(){\n    vec2 q=abs(UV*vec2(200.0,290.0)-vec2(100.0,145.0))-vec2(86.0,131.0);\n    float d=length(max(q,vec2(0.0)))+min(max(q.x,q.y),0.0)-14.0;\n    // Godot fragment COLOR already contains the default texture sample.\n    COLOR.a*=clamp(0.5-d,0.0,1.0);\n}"
					}
				};
			}
			back.AddChild(new TextureRect
			{
				Name = "Art",
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				Texture = _art,
				Size = back.Size,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				TextureFilter = CanvasItem.TextureFilterEnum.Linear,
				Material = _rounded,
				MouseFilter = Control.MouseFilterEnum.Ignore
			}, forceReadableName: false, Node.InternalMode.Disabled);
		}
		else
		{
			GD.PushWarning("Hand deal: authored cardback is unavailable; using source-shaped fallback.");
			Panel panel = new Panel
			{
				Size = back.Size,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
			{
				BgColor = SourcePalette.Window,
				CornerRadiusTopLeft = 14,
				CornerRadiusTopRight = 14,
				CornerRadiusBottomLeft = 14,
				CornerRadiusBottomRight = 14
			});
			back.AddChild(panel, forceReadableName: false, Node.InternalMode.Disabled);
			Color brass = new Color(SourcePalette.Brass, 0.8f);
			Rect2[] array = new Rect2[4]
			{
				new Rect2(10f, 10f, 180f, 2f),
				new Rect2(10f, 278f, 180f, 2f),
				new Rect2(10f, 10f, 2f, 270f),
				new Rect2(188f, 10f, 2f, 270f)
			};
			for (int i = 0; i < array.Length; i++)
			{
				Rect2 r = array[i];
				back.AddChild(new ColorRect
				{
					Position = r.Position,
					Size = r.Size,
					Color = brass,
					MouseFilter = Control.MouseFilterEnum.Ignore
				}, forceReadableName: false, Node.InternalMode.Disabled);
			}
			if (ResourceLoader.Exists("res://assets/Art/icons/star.png"))
			{
				back.AddChild(new TextureRect
				{
					Name = "Star",
					ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
					Texture = GD.Load<Texture2D>("res://assets/Art/icons/star.png"),
					Position = new Vector2(58f, 103f),
					Size = new Vector2(84f, 84f),
					StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
					Modulate = new Color(SourcePalette.Brass, 0.55f),
					MouseFilter = Control.MouseFilterEnum.Ignore
				}, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
		return back;
	}

	public static void Raise(CardTile tile)
	{
		Control back = tile.GetNodeOrNull<Control>("SourceHandDealBack");
		if (back != null)
		{
			tile.MoveChild(back, tile.GetChildCount() - 1);
		}
	}

	public static void Remove(CardTile tile)
	{
		if (tile != null && GodotObject.IsInstanceValid(tile))
		{
			Control back = tile.GetNodeOrNull<Control>("SourceHandDealBack");
			if (back != null)
			{
				tile.RemoveChild(back);
				back.QueueFree();
			}
		}
	}
}
