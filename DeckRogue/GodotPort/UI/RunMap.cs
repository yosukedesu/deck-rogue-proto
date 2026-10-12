using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.UI;

[ScriptPath("res://scripts/ui/RunMap.cs")]
public class RunMap : Control
{
	private sealed class Stroke
	{
		public string Color = "f3dc98";

		public List<Vector2> Points = new List<Vector2>();
	}

	public new class MethodName : Control.MethodName
	{
		public static readonly StringName SetDrawing = "SetDrawing";

		public static readonly StringName UndoAnnotation = "UndoAnnotation";

		public static readonly StringName SaveAnnotations = "SaveAnnotations";

		public new static readonly StringName _GuiInput = "_GuiInput";

		public static readonly StringName DrawAnnotations = "DrawAnnotations";

		public static readonly StringName Point = "Point";

		public new static readonly StringName _Draw = "_Draw";
	}

	public new class PropertyName : Control.PropertyName
	{
		public static readonly StringName _drawing = "_drawing";

		public static readonly StringName _pen = "_pen";
	}

	public new class SignalName : Control.SignalName
	{
	}

	private readonly List<Stroke> _strokes = new List<Stroke>();

	private readonly Dictionary<BaseButton, bool> _originalEnabled = new Dictionary<BaseButton, bool>();

	private Stroke? _drawingStroke;

	private bool _drawing;

	private string _pen = "f3dc98";

	private RunState? _run;

	public event Action<JArray>? AnnotationsChanged;

	public void LoadAnnotations(JArray? strokes)
	{
		_strokes.Clear();
		if (strokes != null)
		{
			foreach (JToken token in strokes)
			{
				if (!(token is JObject item))
				{
					continue;
				}
				Stroke stroke = new Stroke();
				JToken? jToken = item["color"];
				stroke.Color = ((jToken != null && jToken.Type == JTokenType.String) ? ((string?)item["color"]) : "f3dc98");
				Stroke stroke2 = stroke;
				if (item["points"] is JArray points)
				{
					foreach (JToken pointToken in points)
					{
						if (pointToken is JArray { Count: 2 } point && float.TryParse(point[0].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) && float.TryParse(point[1].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) && float.IsFinite(x) && float.IsFinite(y))
						{
							stroke2.Points.Add(new Vector2(x, y));
						}
					}
				}
				if (stroke2.Points.Count > 0)
				{
					_strokes.Add(stroke2);
				}
			}
		}
		QueueRedraw();
	}

	public void SetDrawing(bool drawing, string color = "f3dc98")
	{
		_drawing = drawing;
		_pen = color;
		base.MouseDefaultCursorShape = (CursorShape)(drawing ? 3 : 0);
		foreach (Node child in GetChildren())
		{
			if (child is BaseButton button)
			{
				if (!_originalEnabled.ContainsKey(button))
				{
					_originalEnabled[button] = !button.Disabled;
				}
				button.Disabled = drawing || !_originalEnabled[button];
				button.MouseFilter = (MouseFilterEnum)(drawing ? 2 : 0);
			}
		}
	}

	public void UndoAnnotation()
	{
		if (_strokes.Count != 0)
		{
			_strokes.RemoveAt(_strokes.Count - 1);
			SaveAnnotations();
			QueueRedraw();
		}
	}

	private void SaveAnnotations()
	{
		JArray array = new JArray();
		foreach (Stroke stroke in _strokes)
		{
			JArray points = new JArray();
			foreach (Vector2 point in stroke.Points)
			{
				points.Add(new JArray(point.X, point.Y));
			}
			array.Add(new JObject
			{
				["color"] = stroke.Color,
				["points"] = points
			});
		}
		this.AnnotationsChanged?.Invoke(array);
	}

	public override void _GuiInput(InputEvent evt)
	{
		if (!_drawing)
		{
			return;
		}
		if (evt is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left)
		{
			if (mouse.Pressed)
			{
				_drawingStroke = new Stroke
				{
					Color = _pen
				};
				_drawingStroke.Points.Add(mouse.Position);
				_strokes.Add(_drawingStroke);
			}
			else if (_drawingStroke != null)
			{
				_drawingStroke = null;
				SaveAnnotations();
			}
			AcceptEvent();
			QueueRedraw();
		}
		else if (evt is InputEventMouseMotion motion && _drawingStroke != null)
		{
			List<Vector2> points = _drawingStroke.Points;
			if (points[points.Count - 1].DistanceTo(motion.Position) > 3f)
			{
				_drawingStroke.Points.Add(motion.Position);
			}
			AcceptEvent();
			QueueRedraw();
		}
	}

	private void DrawAnnotations()
	{
		foreach (Stroke stroke in _strokes)
		{
			Color color = Color.FromString(stroke.Color, Colors.Gold);
			if (stroke.Points.Count == 1)
			{
				DrawCircle(stroke.Points[0], 3f, color);
			}
			else
			{
				DrawPolyline(stroke.Points.ToArray(), color, 4f, antialiased: true);
			}
		}
	}

	private Vector2 Point(int row, int col)
	{
		return new Vector2(62 + (_run.Map[row][col].Col ?? col) * 146, 40 + (_run.Map.Count - row - 1) * 112);
	}

	public void Build(RunState run, Action<int> choose, bool interactive, Theme? theme, int labelFontSize = 16)
	{
		_run = run;
		base.Theme = theme;
		IReadOnlyList<int> next = Run.NextChoices(run);
		IReadOnlyList<int> wings = Run.WingChoices(run);
		for (int row = 0; row < run.Map.Count; row++)
		{
			for (int col = 0; col < run.Map[row].Count; col++)
			{
				int index = col;
				MapNode node = run.Map[row][col];
				bool accessible = row == run.Row + 1 && (next.Contains(col) || wings.Contains(col));
				bool current = row == run.Row && col == run.Col;
				Button button = new Button
				{
					Text = "",
					Position = Point(row, col),
					Size = new Vector2(86f, 86f),
					Disabled = (!interactive || !accessible),
					TooltipText = $"{row + 1}層 / {FlowRules.NodeName(node.Type)}" + (current ? " / 現在地" : ((!accessible) ? "" : (next.Contains(col) ? " / 進む" : " / 翼の靴を使う")))
				};
				StyleBoxFlat box = new StyleBoxFlat
				{
					BgColor = (current ? SourcePalette.ManaLight : (accessible ? SourcePalette.Paper3 : SourcePalette.Paper2.Darkened(0.15f))),
					BorderColor = (accessible ? SourcePalette.Brass : SourcePalette.InkSoft),
					BorderWidthLeft = ((!accessible) ? 1 : 3),
					BorderWidthRight = ((!accessible) ? 1 : 3),
					BorderWidthTop = ((!accessible) ? 1 : 3),
					BorderWidthBottom = ((!accessible) ? 1 : 3),
					CornerRadiusTopLeft = 42,
					CornerRadiusTopRight = 42,
					CornerRadiusBottomLeft = 42,
					CornerRadiusBottomRight = 42
				};
				button.AddThemeStyleboxOverride("normal", box);
				button.AddThemeStyleboxOverride("disabled", box);
				button.AddThemeStyleboxOverride("hover", new StyleBoxFlat
				{
					BgColor = SourcePalette.BrassLight,
					CornerRadiusTopLeft = 42,
					CornerRadiusTopRight = 42,
					CornerRadiusBottomLeft = 42,
					CornerRadiusBottomRight = 42
				});
				string path = "res://assets/Art/map/node_" + ((node.Type == "event") ? "unknown" : node.Type) + ".png";
				if (ResourceLoader.Exists(path))
				{
					TextureRect icon = new TextureRect
					{
						Texture = GD.Load<Texture2D>(path),
						TextureFilter = TextureFilterEnum.Nearest,
						ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
						StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
						MouseFilter = MouseFilterEnum.Ignore,
						Position = new Vector2(12f, 6f),
						Size = new Vector2(62f, 54f)
					};
					button.AddChild(icon, forceReadableName: false, InternalMode.Disabled);
				}
				Label label = new Label
				{
					Text = (current ? "現在地" : FlowRules.NodeName(node.Type)),
					MouseFilter = MouseFilterEnum.Ignore,
					Position = new Vector2(0f, 57f),
					Size = new Vector2(86f, 25f),
					HorizontalAlignment = HorizontalAlignment.Center
				};
				label.AddThemeFontSizeOverride("font_size", labelFontSize);
				label.AddThemeColorOverride("font_color", SourcePalette.Ink);
				button.AddChild(label, forceReadableName: false, InternalMode.Disabled);
				button.Pressed += delegate
				{
					choose(index);
				};
				AddChild(button, forceReadableName: false, InternalMode.Disabled);
			}
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_run == null)
		{
			return;
		}
		for (int row = 0; row + 1 < _run.Map.Count; row++)
		{
			for (int col = 0; col < _run.Map[row].Count; col++)
			{
				foreach (int next in _run.Map[row][col].Next)
				{
					if (next >= 0 && next < _run.Map[row + 1].Count)
					{
						bool active = row == _run.Row && col == _run.Col;
						DrawLine(Point(row, col) + new Vector2(43f, 0f), Point(row + 1, next) + new Vector2(43f, 86f), active ? SourcePalette.BrassLight : SourcePalette.PaperDim.Darkened(0.4f), active ? 4 : 2, antialiased: true);
					}
				}
			}
		}
		DrawAnnotations();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		List<MethodInfo> methods = new List<MethodInfo>(7);
		methods.Add(new MethodInfo(MethodName.SetDrawing, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Bool, "drawing", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.UndoAnnotation, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.SaveAnnotations, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName._GuiInput, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "evt", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("InputEvent"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.DrawAnnotations, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.Point, new PropertyInfo(Variant.Type.Vector2, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "row", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "col", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName._Draw, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		return methods;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.SetDrawing && args.Count == 2)
		{
			SetDrawing(VariantUtils.ConvertTo<bool>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.UndoAnnotation && args.Count == 0)
		{
			UndoAnnotation();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SaveAnnotations && args.Count == 0)
		{
			SaveAnnotations();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._GuiInput && args.Count == 1)
		{
			_GuiInput(VariantUtils.ConvertTo<InputEvent>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.DrawAnnotations && args.Count == 0)
		{
			DrawAnnotations();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Point && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<Vector2>(Point(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1])));
			return true;
		}
		if (method == MethodName._Draw && args.Count == 0)
		{
			_Draw();
			ret = default(godot_variant);
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName.SetDrawing)
		{
			return true;
		}
		if (method == MethodName.UndoAnnotation)
		{
			return true;
		}
		if (method == MethodName.SaveAnnotations)
		{
			return true;
		}
		if (method == MethodName._GuiInput)
		{
			return true;
		}
		if (method == MethodName.DrawAnnotations)
		{
			return true;
		}
		if (method == MethodName.Point)
		{
			return true;
		}
		if (method == MethodName._Draw)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName._drawing)
		{
			_drawing = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._pen)
		{
			_pen = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName._drawing)
		{
			value = VariantUtils.CreateFrom(in _drawing);
			return true;
		}
		if (name == PropertyName._pen)
		{
			value = VariantUtils.CreateFrom(in _pen);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		List<PropertyInfo> properties = new List<PropertyInfo>();
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._drawing, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName._pen, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		return properties;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName._drawing, Variant.From(in _drawing));
		info.AddProperty(PropertyName._pen, Variant.From(in _pen));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName._drawing, out var _value__drawing))
		{
			_drawing = _value__drawing.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._pen, out var _value__pen))
		{
			_pen = _value__pen.As<string>();
		}
	}
}
