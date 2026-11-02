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
	public new class MethodName : Control.MethodName
	{
		public static readonly StringName SetDrawing = "SetDrawing";

		public static readonly StringName TogglePen = "TogglePen";

		public static readonly StringName ToggleEraser = "ToggleEraser";

		public static readonly StringName UpdateToolState = "UpdateToolState";

		public static readonly StringName UndoAnnotation = "UndoAnnotation";

		public static readonly StringName ClearAnnotations = "ClearAnnotations";

		public static readonly StringName SaveAnnotations = "SaveAnnotations";

		public new static readonly StringName _GuiInput = "_GuiInput";

		public static readonly StringName HandlePointer = "HandlePointer";

		public static readonly StringName FinishPointer = "FinishPointer";

		public static readonly StringName EraseAt = "EraseAt";

		public static readonly StringName DistanceToSegment = "DistanceToSegment";

		public static readonly StringName RedrawAnnotations = "RedrawAnnotations";

		public static readonly StringName DrawAnnotations = "DrawAnnotations";

		public static readonly StringName Point = "Point";

		public static readonly StringName Center = "Center";

		public static readonly StringName Key = "Key";

		public static readonly StringName ClearNodeInformation = "ClearNodeInformation";

		public static readonly StringName InspectNode = "InspectNode";

		public static readonly StringName NodeTint = "NodeTint";

		public static readonly StringName AddLabel = "AddLabel";

		public static readonly StringName AddPicture = "AddPicture";

		public static readonly StringName DrawEdge = "DrawEdge";

		public new static readonly StringName _Draw = "_Draw";
	}

	public new class PropertyName : Control.PropertyName
	{
		public static readonly StringName IsDrawing = "IsDrawing";

		public static readonly StringName IsErasing = "IsErasing";

		public static readonly StringName PenColor = "PenColor";

		public static readonly StringName AnnotationCount = "AnnotationCount";

		public static readonly StringName StartPoint = "StartPoint";

		public static readonly StringName _annotationLayer = "_annotationLayer";

		public static readonly StringName _editable = "_editable";

		public static readonly StringName _drawing = "_drawing";

		public static readonly StringName _erasing = "_erasing";

		public static readonly StringName _strokeChanged = "_strokeChanged";

		public static readonly StringName _pen = "_pen";

		public static readonly StringName _pointerButton = "_pointerButton";

		public static readonly StringName _inspectedNode = "_inspectedNode";

		public static readonly StringName _phone = "_phone";
	}

	public new class SignalName : Control.SignalName
	{
	}

	private sealed class Stroke
	{
		public string Color = "f5edd6";

		public List<Vector2> Points = new List<Vector2>();
	}

	public const string PaperPen = "f5edd6";

	public const string RedPen = "e06b5c";

	private const float EraseRadius = 18f;

	private readonly List<Stroke> _strokes = new List<Stroke>();

	private readonly List<Button> _nodeButtons = new List<Button>();

	private readonly HashSet<long> _inferredPath = new HashSet<long>();

	private Stroke? _drawingStroke;

	private Control? _annotationLayer;

	private RunState? _run;

	private bool _editable;

	private bool _drawing;

	private bool _erasing;

	private bool _strokeChanged;

	private string _pen = "f5edd6";

	private MouseButton _pointerButton;

	private string? _inspectedNode;

	private bool _phone;

	public bool IsDrawing => _drawing;

	public bool IsErasing
	{
		get
		{
			if (_drawing)
			{
				return _erasing;
			}
			return false;
		}
	}

	public string PenColor => _pen;

	public int AnnotationCount => _strokes.Count;

	// Read-only projection of the existing marker geometry.
	public float CurrentFocusY => _run != null && _run.Row >= 0 && _run.Row < _run.Map.Count
		&& _run.Col >= 0 && _run.Col < _run.Map[_run.Row].Count
		? Center(_run.Row, _run.Col).Y : StartPoint.Y;

	private Vector2 StartPoint => new Vector2(550f, 83 + (_run?.Map.Count ?? 0) * 112);

	public event Action<JArray>? AnnotationsChanged;

	public event Action? ToolChanged;

	public event Action<string>? NodeInformationChanged;

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(24)
		{
			new MethodInfo(MethodName.SetDrawing, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Bool, "drawing", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.TogglePen, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ToggleEraser, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.UpdateToolState, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.UndoAnnotation, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.ClearAnnotations, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.SaveAnnotations, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._GuiInput, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "evt", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("InputEvent"), exported: false)
			}, null),
			new MethodInfo(MethodName.HandlePointer, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "evt", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("InputEvent"), exported: false),
				new PropertyInfo(Variant.Type.Vector2, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.FinishPointer, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.EraseAt, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Vector2, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.DistanceToSegment, new PropertyInfo(Variant.Type.Float, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Vector2, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Vector2, "a", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Vector2, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.RedrawAnnotations, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.DrawAnnotations, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.Point, new PropertyInfo(Variant.Type.Vector2, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "row", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "col", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Center, new PropertyInfo(Variant.Type.Vector2, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "row", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "col", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Key, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "row", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "col", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ClearNodeInformation, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.InspectNode, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "key", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.NodeTint, new PropertyInfo(Variant.Type.Color, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "type", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "accessible", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "current", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "taken", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "passed", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.AddLabel, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Control"), exported: false),
				new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Rect2, "rect", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "size", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Color, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.AddPicture, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Control"), exported: false),
				new PropertyInfo(Variant.Type.String, "path", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Rect2, "rect", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "alpha", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.DrawEdge, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Vector2, "a", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Vector2, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "available", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "taken", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "passed", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName._Draw, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.SetDrawing && args.Count == 2)
		{
			SetDrawing(VariantUtils.ConvertTo<bool>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.TogglePen && args.Count == 1)
		{
			TogglePen(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.ToggleEraser && args.Count == 0)
		{
			ToggleEraser();
			ret = default;
			return true;
		}
		if (method == MethodName.UpdateToolState && args.Count == 0)
		{
			UpdateToolState();
			ret = default;
			return true;
		}
		if (method == MethodName.UndoAnnotation && args.Count == 0)
		{
			UndoAnnotation();
			ret = default;
			return true;
		}
		if (method == MethodName.ClearAnnotations && args.Count == 0)
		{
			ClearAnnotations();
			ret = default;
			return true;
		}
		if (method == MethodName.SaveAnnotations && args.Count == 0)
		{
			SaveAnnotations();
			ret = default;
			return true;
		}
		if (method == MethodName._GuiInput && args.Count == 1)
		{
			_GuiInput(VariantUtils.ConvertTo<InputEvent>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.HandlePointer && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<bool>(HandlePointer(VariantUtils.ConvertTo<InputEvent>(in args[0]), VariantUtils.ConvertTo<Vector2>(in args[1])));
			return true;
		}
		if (method == MethodName.FinishPointer && args.Count == 0)
		{
			FinishPointer();
			ret = default;
			return true;
		}
		if (method == MethodName.EraseAt && args.Count == 1)
		{
			EraseAt(VariantUtils.ConvertTo<Vector2>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.DistanceToSegment && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<float>(DistanceToSegment(VariantUtils.ConvertTo<Vector2>(in args[0]), VariantUtils.ConvertTo<Vector2>(in args[1]), VariantUtils.ConvertTo<Vector2>(in args[2])));
			return true;
		}
		if (method == MethodName.RedrawAnnotations && args.Count == 0)
		{
			RedrawAnnotations();
			ret = default;
			return true;
		}
		if (method == MethodName.DrawAnnotations && args.Count == 0)
		{
			DrawAnnotations();
			ret = default;
			return true;
		}
		if (method == MethodName.Point && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<Vector2>(Point(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1])));
			return true;
		}
		if (method == MethodName.Center && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<Vector2>(Center(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1])));
			return true;
		}
		if (method == MethodName.Key && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<long>(Key(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1])));
			return true;
		}
		if (method == MethodName.ClearNodeInformation && args.Count == 0)
		{
			ClearNodeInformation();
			ret = default;
			return true;
		}
		if (method == MethodName.InspectNode && args.Count == 2)
		{
			InspectNode(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.NodeTint && args.Count == 5)
		{
			ret = VariantUtils.CreateFrom<Color>(NodeTint(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<bool>(in args[1]), VariantUtils.ConvertTo<bool>(in args[2]), VariantUtils.ConvertTo<bool>(in args[3]), VariantUtils.ConvertTo<bool>(in args[4])));
			return true;
		}
		if (method == MethodName.AddLabel && args.Count == 5)
		{
			AddLabel(VariantUtils.ConvertTo<Control>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<Rect2>(in args[2]), VariantUtils.ConvertTo<int>(in args[3]), VariantUtils.ConvertTo<Color>(in args[4]));
			ret = default;
			return true;
		}
		if (method == MethodName.AddPicture && args.Count == 4)
		{
			AddPicture(VariantUtils.ConvertTo<Control>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<Rect2>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.DrawEdge && args.Count == 5)
		{
			DrawEdge(VariantUtils.ConvertTo<Vector2>(in args[0]), VariantUtils.ConvertTo<Vector2>(in args[1]), VariantUtils.ConvertTo<bool>(in args[2]), VariantUtils.ConvertTo<bool>(in args[3]), VariantUtils.ConvertTo<bool>(in args[4]));
			ret = default;
			return true;
		}
		if (method == MethodName._Draw && args.Count == 0)
		{
			_Draw();
			ret = default;
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.DistanceToSegment && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<float>(DistanceToSegment(VariantUtils.ConvertTo<Vector2>(in args[0]), VariantUtils.ConvertTo<Vector2>(in args[1]), VariantUtils.ConvertTo<Vector2>(in args[2])));
			return true;
		}
		if (method == MethodName.Key && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<long>(Key(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1])));
			return true;
		}
		if (method == MethodName.NodeTint && args.Count == 5)
		{
			ret = VariantUtils.CreateFrom<Color>(NodeTint(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<bool>(in args[1]), VariantUtils.ConvertTo<bool>(in args[2]), VariantUtils.ConvertTo<bool>(in args[3]), VariantUtils.ConvertTo<bool>(in args[4])));
			return true;
		}
		if (method == MethodName.AddLabel && args.Count == 5)
		{
			AddLabel(VariantUtils.ConvertTo<Control>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<Rect2>(in args[2]), VariantUtils.ConvertTo<int>(in args[3]), VariantUtils.ConvertTo<Color>(in args[4]));
			ret = default;
			return true;
		}
		if (method == MethodName.AddPicture && args.Count == 4)
		{
			AddPicture(VariantUtils.ConvertTo<Control>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<Rect2>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]));
			ret = default;
			return true;
		}
		ret = default;
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName.SetDrawing)
		{
			return true;
		}
		if (method == MethodName.TogglePen)
		{
			return true;
		}
		if (method == MethodName.ToggleEraser)
		{
			return true;
		}
		if (method == MethodName.UpdateToolState)
		{
			return true;
		}
		if (method == MethodName.UndoAnnotation)
		{
			return true;
		}
		if (method == MethodName.ClearAnnotations)
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
		if (method == MethodName.HandlePointer)
		{
			return true;
		}
		if (method == MethodName.FinishPointer)
		{
			return true;
		}
		if (method == MethodName.EraseAt)
		{
			return true;
		}
		if (method == MethodName.DistanceToSegment)
		{
			return true;
		}
		if (method == MethodName.RedrawAnnotations)
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
		if (method == MethodName.Center)
		{
			return true;
		}
		if (method == MethodName.Key)
		{
			return true;
		}
		if (method == MethodName.ClearNodeInformation)
		{
			return true;
		}
		if (method == MethodName.InspectNode)
		{
			return true;
		}
		if (method == MethodName.NodeTint)
		{
			return true;
		}
		if (method == MethodName.AddLabel)
		{
			return true;
		}
		if (method == MethodName.AddPicture)
		{
			return true;
		}
		if (method == MethodName.DrawEdge)
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
		if (name == PropertyName._annotationLayer)
		{
			_annotationLayer = VariantUtils.ConvertTo<Control>(in value);
			return true;
		}
		if (name == PropertyName._editable)
		{
			_editable = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._drawing)
		{
			_drawing = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._erasing)
		{
			_erasing = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._strokeChanged)
		{
			_strokeChanged = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._pen)
		{
			_pen = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._pointerButton)
		{
			_pointerButton = VariantUtils.ConvertTo<MouseButton>(in value);
			return true;
		}
		if (name == PropertyName._inspectedNode)
		{
			_inspectedNode = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._phone)
		{
			_phone = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		bool from;
		if (name == PropertyName.IsDrawing)
		{
			from = IsDrawing;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.IsErasing)
		{
			from = IsErasing;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.PenColor)
		{
			value = VariantUtils.CreateFrom<string>(PenColor);
			return true;
		}
		if (name == PropertyName.AnnotationCount)
		{
			value = VariantUtils.CreateFrom<int>(AnnotationCount);
			return true;
		}
		if (name == PropertyName.StartPoint)
		{
			value = VariantUtils.CreateFrom<Vector2>(StartPoint);
			return true;
		}
		if (name == PropertyName._annotationLayer)
		{
			value = VariantUtils.CreateFrom(in _annotationLayer);
			return true;
		}
		if (name == PropertyName._editable)
		{
			value = VariantUtils.CreateFrom(in _editable);
			return true;
		}
		if (name == PropertyName._drawing)
		{
			value = VariantUtils.CreateFrom(in _drawing);
			return true;
		}
		if (name == PropertyName._erasing)
		{
			value = VariantUtils.CreateFrom(in _erasing);
			return true;
		}
		if (name == PropertyName._strokeChanged)
		{
			value = VariantUtils.CreateFrom(in _strokeChanged);
			return true;
		}
		if (name == PropertyName._pen)
		{
			value = VariantUtils.CreateFrom(in _pen);
			return true;
		}
		if (name == PropertyName._pointerButton)
		{
			value = VariantUtils.CreateFrom(in _pointerButton);
			return true;
		}
		if (name == PropertyName._inspectedNode)
		{
			value = VariantUtils.CreateFrom(in _inspectedNode);
			return true;
		}
		if (name == PropertyName._phone)
		{
			value = VariantUtils.CreateFrom(in _phone);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, PropertyName._annotationLayer, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._editable, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._drawing, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._erasing, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._strokeChanged, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._pen, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._pointerButton, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._inspectedNode, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._phone, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.IsDrawing, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.IsErasing, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.PenColor, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.AnnotationCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector2, PropertyName.StartPoint, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName._annotationLayer, Variant.From(in _annotationLayer));
		info.AddProperty(PropertyName._editable, Variant.From(in _editable));
		info.AddProperty(PropertyName._drawing, Variant.From(in _drawing));
		info.AddProperty(PropertyName._erasing, Variant.From(in _erasing));
		info.AddProperty(PropertyName._strokeChanged, Variant.From(in _strokeChanged));
		info.AddProperty(PropertyName._pen, Variant.From(in _pen));
		info.AddProperty(PropertyName._pointerButton, Variant.From(in _pointerButton));
		info.AddProperty(PropertyName._inspectedNode, Variant.From(in _inspectedNode));
		info.AddProperty(PropertyName._phone, Variant.From(in _phone));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName._annotationLayer, out var _value__annotationLayer))
		{
			_annotationLayer = _value__annotationLayer.As<Control>();
		}
		if (info.TryGetProperty(PropertyName._editable, out var _value__editable))
		{
			_editable = _value__editable.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._drawing, out var _value__drawing))
		{
			_drawing = _value__drawing.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._erasing, out var _value__erasing))
		{
			_erasing = _value__erasing.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._strokeChanged, out var _value__strokeChanged))
		{
			_strokeChanged = _value__strokeChanged.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._pen, out var _value__pen))
		{
			_pen = _value__pen.As<string>();
		}
		if (info.TryGetProperty(PropertyName._pointerButton, out var _value__pointerButton))
		{
			_pointerButton = _value__pointerButton.As<MouseButton>();
		}
		if (info.TryGetProperty(PropertyName._inspectedNode, out var _value__inspectedNode))
		{
			_inspectedNode = _value__inspectedNode.As<string>();
		}
		if (info.TryGetProperty(PropertyName._phone, out var _value__phone))
		{
			_phone = _value__phone.As<bool>();
		}
	}

	public void LoadAnnotations(JArray? strokes)
	{
		_strokes.Clear();
		if (strokes != null)
		{
			foreach (JToken stroke3 in strokes)
			{
				if (!(stroke3 is JObject item))
				{
					continue;
				}
				Stroke stroke = new Stroke();
				JToken? jToken = item["color"];
				stroke.Color = ((jToken != null && jToken.Type == JTokenType.String) ? (((string?)item["color"]) ?? "f5edd6") : "f5edd6");
				Stroke stroke2 = stroke;
				if (item["points"] is JArray points)
				{
					foreach (JToken item2 in points)
					{
						if (item2 is JArray { Count: 2 } point && float.TryParse(point[0].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) && float.TryParse(point[1].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) && float.IsFinite(x) && float.IsFinite(y))
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
		RedrawAnnotations();
	}

	public void SetDrawing(bool drawing, string color = "f5edd6")
	{
		FinishPointer();
		_drawing = _editable & drawing;
		_erasing = false;
		_pen = color;
		UpdateToolState();
	}

	public void TogglePen(string color)
	{
		bool next = !_drawing || _erasing || !(_pen == color);
		SetDrawing(next, color);
	}

	public void ToggleEraser()
	{
		FinishPointer();
		bool next = !IsErasing;
		_drawing = _editable & next;
		_erasing = _drawing;
		UpdateToolState();
	}

	private void UpdateToolState()
	{
		MouseDefaultCursorShape = (CursorShape)(_drawing ? 3 : 0);
		foreach (Button nodeButton in _nodeButtons)
		{
			nodeButton.MouseFilter = (MouseFilterEnum)(_drawing ? 2 : 1);
			nodeButton.FocusMode = (FocusModeEnum)(_drawing ? 0 : 2);
		}
		ToolChanged?.Invoke();
	}

	public void UndoAnnotation()
	{
		if (_editable)
		{
			FinishPointer();
			if (_strokes.Count != 0)
			{
				_strokes.RemoveAt(_strokes.Count - 1);
				SaveAnnotations();
				RedrawAnnotations();
			}
		}
	}

	public void ClearAnnotations()
	{
		if (_editable)
		{
			FinishPointer();
			if (_strokes.Count != 0)
			{
				_strokes.Clear();
				SaveAnnotations();
				RedrawAnnotations();
			}
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
		AnnotationsChanged?.Invoke(array);
		ToolChanged?.Invoke();
	}

	public override void _GuiInput(InputEvent evt)
	{
		Vector2 p = ((evt is InputEventMouse mouse) ? mouse.Position : GetLocalMousePosition());
		if (HandlePointer(evt, p))
		{
			AcceptEvent();
		}
	}

	private bool HandlePointer(InputEvent evt, Vector2 p)
	{
		if (!_editable)
		{
			return false;
		}
		if (evt is InputEventMouseButton mouse)
		{
			if (mouse.Pressed)
			{
				bool flag = mouse.ButtonIndex == MouseButton.Right;
				bool left = mouse.ButtonIndex == MouseButton.Left && _drawing;
				if (!flag && !left)
				{
					return false;
				}
				FinishPointer();
				_pointerButton = mouse.ButtonIndex;
				_strokeChanged = false;
				ClearNodeInformation();
				if (_erasing)
				{
					EraseAt(p);
				}
				else
				{
					_drawingStroke = new Stroke
					{
						Color = (_drawing ? _pen : "f5edd6")
					};
					_drawingStroke.Points.Add(p);
					_strokes.Add(_drawingStroke);
					_strokeChanged = true;
				}
				RedrawAnnotations();
				return true;
			}
			if (_pointerButton != MouseButton.None && mouse.ButtonIndex == _pointerButton)
			{
				FinishPointer();
				return true;
			}
		}
		else if (evt is InputEventMouseMotion && _pointerButton != MouseButton.None)
		{
			if (_erasing)
			{
				EraseAt(p);
			}
			else if (_drawingStroke != null)
			{
				List<Vector2> points = _drawingStroke.Points;
				if (points[points.Count - 1].DistanceSquaredTo(p) >= 4f)
				{
					_drawingStroke.Points.Add(p);
					_strokeChanged = true;
				}
			}
			RedrawAnnotations();
			return true;
		}
		return false;
	}

	private void FinishPointer()
	{
		bool strokeChanged = _strokeChanged;
		_pointerButton = MouseButton.None;
		_drawingStroke = null;
		_strokeChanged = false;
		if (strokeChanged)
		{
			SaveAnnotations();
		}
	}

	private void EraseAt(Vector2 p)
	{
		if (_strokes.RemoveAll((Stroke stroke) => NearStroke(stroke, p)) > 0)
		{
			_strokeChanged = true;
		}
	}

	private static bool NearStroke(Stroke stroke, Vector2 p)
	{
		for (int i = 0; i < stroke.Points.Count; i++)
		{
			if (stroke.Points[i].DistanceSquaredTo(p) <= 324f)
			{
				return true;
			}
			if (i > 0 && DistanceToSegment(p, stroke.Points[i - 1], stroke.Points[i]) <= 18f)
			{
				return true;
			}
		}
		return false;
	}

	public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
	{
		Vector2 ab = b - a;
		float lengthSquared = ab.LengthSquared();
		float t = ((lengthSquared > 0f) ? Math.Clamp((p - a).Dot(ab) / lengthSquared, 0f, 1f) : 0f);
		return (a + ab * t).DistanceTo(p);
	}

	private void RedrawAnnotations()
	{
		if (_annotationLayer != null && GodotObject.IsInstanceValid(_annotationLayer))
		{
			_annotationLayer.QueueRedraw();
		}
		QueueRedraw();
	}

	private void DrawAnnotations()
	{
		if (_annotationLayer == null)
		{
			return;
		}
		foreach (Stroke stroke in _strokes)
		{
			Color color = Color.FromString(stroke.Color, SourcePalette.Paper);
			if (stroke.Points.Count == 1)
			{
				_annotationLayer.DrawCircle(stroke.Points[0], 2f, color);
			}
			else
			{
				_annotationLayer.DrawPolyline(stroke.Points.ToArray(), color, 4f, antialiased: true);
			}
		}
	}

	private Vector2 Point(int row, int col)
	{
		return new Vector2(62 + (_run.Map[row][col].Col ?? col) * 146, 40 + (_run.Map.Count - row - 1) * 112);
	}

	private Vector2 Center(int row, int col)
	{
		return Point(row, col) + new Vector2(43f, 43f);
	}

	private static long Key(int row, int col)
	{
		return ((long)row << 32) | (uint)col;
	}

	private float NodeSize(MapNode node)
	{
		return (!(node.Type == "boss")) ? (_phone ? 74 : 60) : (_phone ? 104 : 92);
	}

	public static HashSet<long> InferPath(RunState run)
	{
		HashSet<long> path = new HashSet<long>();
		if (run.Row < 0 || run.Row >= run.Map.Count || run.Col < 0 || run.Col >= run.Map[run.Row].Count)
		{
			return path;
		}
		int row = run.Row;
		int col = run.Col;
		path.Add(Key(row, col));
		while (row > 0)
		{
			int best = -1;
			int bestDistance = int.MaxValue;
			for (int i = 0; i < run.Map[row - 1].Count; i++)
			{
				MapNode candidate = run.Map[row - 1][i];
				if (candidate.Next.Contains(col))
				{
					int distance = Math.Abs(candidate.Col.GetValueOrDefault() - run.Map[row][col].Col.GetValueOrDefault());
					if (distance < bestDistance)
					{
						best = i;
						bestDistance = distance;
					}
				}
			}
			if (best < 0)
			{
				break;
			}
			col = best;
			row--;
			path.Add(Key(row, col));
		}
		return path;
	}

	public static string NodeDescription(MapNode node)
	{
		string encounter = "";
		if (!string.IsNullOrEmpty(node.EncounterId))
		{
			try
			{
				encounter = "\n" + Content.EncounterName(node.EncounterId);
			}
			catch (Exception)
			{
				encounter = "\n" + node.EncounterId;
			}
		}
		return node.Type switch
		{
			"battle" => "戦闘" + encounter + "\n勝てばカード報酬とゴールド", 
			"elite" => "強個体" + encounter + "\n勝てばレリック3択・レア確定の報酬・ゴールド", 
			"boss" => "幕ボス" + encounter + "\n倒すと全回復・レリック3択・次の幕へ", 
			"campfire" => "焚き火\n休む（最大HPの割合で回復）か、カードを1枚鍛える", 
			"shop" => "ショップ\nカード・レリック・カード除去・鍛える", 
			"workshop" => "工房\n同じ色のカード2枚を1枚に溶かす（" + 100 + "G）", 
			"event" => "？\n入るまで中身は分からない（イベント・戦闘・店・宝箱）", 
			"treasure" => "宝箱\nレリック3択", 
			_ => FlowRules.NodeName(node.Type) + encounter, 
		};
	}

	public void ClearNodeInformation()
	{
		_inspectedNode = null;
		NodeInformationChanged?.Invoke("");
	}

	private void InspectNode(string key, string text)
	{
		if (_inspectedNode == key)
		{
			ClearNodeInformation();
			return;
		}
		_inspectedNode = key;
		NodeInformationChanged?.Invoke(text);
	}

	public void Build(RunState run, Action<int> choose, bool interactive, Theme? theme, int labelFontSize = 16, bool phone = false)
	{
		_run = run;
		_editable = interactive;
		_phone = phone;
		Theme = theme;
		MouseFilter = MouseFilterEnum.Stop;
		_inferredPath.Clear();
		foreach (long key in InferPath(run))
		{
			_inferredPath.Add(key);
		}
		IReadOnlyList<int> next = Run.NextChoices(run);
		IReadOnlyList<int> wings = Run.WingChoices(run);
		int recipeCount = Fusion.RecipePairsInDeck(run.Deck).Count;
		for (int row = 0; row < run.Map.Count; row++)
		{
			for (int col = 0; col < run.Map[row].Count; col++)
			{
				int index = col;
				MapNode node = run.Map[row][col];
				bool accessible = row == run.Row + 1 && (next.Contains(col) || wings.Contains(col));
				bool current = row == run.Row && col == run.Col;
				bool taken = _inferredPath.Contains(Key(row, col));
				bool passed = row <= run.Row;
				float size = NodeSize(node);
				string key2 = row + ":" + col;
				string text = NodeDescription(node);
				string text2 = $"\n{row + 1}層";
				string text3;
				if (current)
				{
					text3 = "・現在地";
				}
				else if (accessible)
				{
					text3 = (next.Contains(col) ? "・進む" : "・翼の靴を使う");
				}
				else
				{
					text3 = "";
				}
				string tip = text + text2 + text3;
				if (node.Type == "workshop" && recipeCount > 0 && !passed)
				{
					int price = Run.WorkshopFusePrice(run);
					tip = tip + $"\n★ 今のデッキでレシピが {recipeCount} 組作れる（{price}G" + ((run.Gold < price) ? "・所持金が足りない）" : "）");
				}
				Color tint = NodeTint(node.Type, accessible, current, taken, passed);
				Button button = new Button
				{
					Name = "MapNode_" + row + "_" + col,
					Text = "",
					Position = Center(row, col) - Vector2.One * size / 2f,
					Size = Vector2.One * size,
					CustomMinimumSize = Vector2.One * size,
					MouseFilter = MouseFilterEnum.Pass,
					TooltipText = tip
				};
				button.SetMeta("route_available", interactive & accessible);
				StyleBoxFlat box = new StyleBoxFlat
				{
					BgColor = tint,
					BorderColor = (current ? SourcePalette.Brass : (accessible ? SourcePalette.BrassLight : SourcePalette.NightEdge)),
					BorderWidthLeft = ((!(current | accessible)) ? 1 : 3),
					BorderWidthRight = ((!(current | accessible)) ? 1 : 3),
					BorderWidthTop = ((!(current | accessible)) ? 1 : 3),
					BorderWidthBottom = ((!(current | accessible)) ? 1 : 3),
					CornerRadiusTopLeft = node.Type == "boss" ? 18 : 10,
					CornerRadiusTopRight = node.Type == "boss" ? 18 : 10,
					CornerRadiusBottomLeft = node.Type == "boss" ? 18 : 10,
					CornerRadiusBottomRight = node.Type == "boss" ? 18 : 10
				};
				button.AddThemeStyleboxOverride("normal", box);
				var focus = (StyleBoxFlat)box.Duplicate();
                focus.BgColor = Colors.Transparent;
                focus.BorderColor = SourcePalette.ManaInk;
                focus.BorderWidthLeft = focus.BorderWidthRight = focus.BorderWidthTop = focus.BorderWidthBottom = 4;
                button.AddThemeStyleboxOverride("focus", focus);
				StyleBoxFlat hover = (StyleBoxFlat)box.Duplicate();
				hover.BorderColor = SourcePalette.BrassLight;
				button.AddThemeStyleboxOverride("hover", hover);
				button.AddThemeStyleboxOverride("pressed", hover);
				AddChild(button, forceReadableName: false, InternalMode.Disabled);
				_nodeButtons.Add(button);
				string art = ((node.Type == "event") ? "unknown" : node.Type);
				AddPicture(button, "res://assets/Art/map/node_" + art + ".png", new Rect2((size - (float)((node.Type == "boss") ? 56 : 34)) / 2f, (size - (float)((node.Type == "boss") ? 56 : 34)) / 2f, (node.Type == "boss") ? 56 : 34, (node.Type == "boss") ? 56 : 34), (passed && !current) ? 0.45f : 1f);
				string stateMark = current ? "◆ " : accessible ? "▶ " : taken ? "✓ " : passed ? "− " : "· ";
                string stateText = current ? "現在地" : accessible ? "進める" : taken ? "通過済み" : passed ? "通過しなかった道" : "まだ進めない";
                button.TooltipText = stateText + " · " + tip;
                AddLabel(button, stateMark + (current ? "現在地" : FlowRules.NodeName(node.Type)), new Rect2(-42f, size + 1f, size + 84f, 28f), labelFontSize, current || accessible ? SourcePalette.BrassLight : SourcePalette.PaperDim);
				if (node.Type == "workshop" && recipeCount > 0 && !passed)
				{
					AddLabel(button, "★", new Rect2(size - 8f, -16f, 30f, 28f), labelFontSize + 3, SourcePalette.BrassLight);
				}
				button.Pressed += () =>
				{
					if (!_drawing && _pointerButton == MouseButton.None)
					{
						if (_editable & accessible)
						{
							choose(index);
						}
						else
						{
							InspectNode(key2, tip);
						}
					}
				};
				button.GuiInput += (InputEvent evt) =>
				{
					Vector2 p = ((evt is InputEventMouse inputEventMouse) ? (button.Position + inputEventMouse.Position) : GetLocalMousePosition());
					if (HandlePointer(evt, p))
					{
						button.AcceptEvent();
					}
				};
			}
		}
		AddPicture(this, "res://assets/Art/icons/flag.png", new Rect2(StartPoint - new Vector2(20f, 20f), new Vector2(40f, 40f)));
		AddLabel(this, "スタート", new Rect2(StartPoint + new Vector2(-60f, 22f), new Vector2(120f, 24f)), labelFontSize, SourcePalette.PaperDim);
		Vector2 marker = ((run.Row >= 0 && run.Row < run.Map.Count && run.Col >= 0 && run.Col < run.Map[run.Row].Count) ? Center(run.Row, run.Col) : StartPoint);
		AddLeaderMarker(run.LeaderId, new Rect2(marker + new Vector2(-94f, -56f), new Vector2(56f, 56f)));
		_annotationLayer = new Control
		{
			Name = "MapAnnotations",
			MouseFilter = MouseFilterEnum.Ignore,
			Size = CustomMinimumSize
		};
		AddChild(_annotationLayer, forceReadableName: false, InternalMode.Disabled);
		_annotationLayer.Draw += DrawAnnotations;
		QueueRedraw();
	}

	private static Color NodeTint(string type, bool accessible, bool current, bool taken, bool passed)
	{
		Color color;
		switch (type)
		{
		case "elite":
			color = new Color("e0b0b0");
			break;
		case "boss":
		case "campfire":
			color = new Color("f0c8a0");
			break;
		case "shop":
			color = new Color("e8dca0");
			break;
		case "workshop":
			color = new Color("d0c8e8");
			break;
		case "treasure":
			color = new Color("f0e0a0");
			break;
		case "event":
			color = new Color("b0e0e0");
			break;
		default:
			color = SourcePalette.Paper3;
			break;
		}
		Color color2 = color;
		if (accessible | current)
		{
			return color2;
		}
		float amount;
		if (taken)
		{
			amount = 0.2f;
		}
		else
		{
			amount = (passed ? 0.55f : 0.3f);
		}
		return color2.Darkened(amount);
	}

	private static void AddLabel(Control parent, string text, Rect2 rect, int size, Color color)
	{
		Label label = new Label
		{
			Text = text,
			Position = rect.Position,
			Size = rect.Size,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore
		};
		label.AddThemeFontOverride("font", SourceRunUiDecor.Body);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		parent.AddChild(label, forceReadableName: false, InternalMode.Disabled);
	}

	private void AddLeaderMarker(string leaderId, Rect2 rect)
	{
		AddChild(new TextureRect
		{
			Name = "SourceMapLeaderMarker",
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			Texture = SourceLeaderArtwork.Resolve(leaderId + "_icon"),
			Position = rect.Position,
			Size = rect.Size,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			TextureFilter = TextureFilterEnum.Nearest,
			MouseFilter = MouseFilterEnum.Ignore
		});
	}

	private static void AddPicture(Control parent, string path, Rect2 rect, float alpha = 1f)
	{
		if (ResourceLoader.Exists(path))
		{
			parent.AddChild(new TextureRect
			{
				Texture = GD.Load<Texture2D>(path),
				Position = rect.Position,
				Size = rect.Size,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				TextureFilter = TextureFilterEnum.Nearest,
				MouseFilter = MouseFilterEnum.Ignore,
				Modulate = new Color(1f, 1f, 1f, alpha)
			}, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void DrawEdge(Vector2 a, Vector2 b, bool available, bool taken, bool passed)
	{
		Color color;
		if (available)
		{
			color = SourcePalette.BrassLight;
		}
		else if (taken)
		{
			color = SourcePalette.Brass;
		}
		else
		{
			color = (passed ? new Color(1f, 1f, 1f, 0.12f) : new Color(1f, 1f, 1f, 0.32f));
		}
		DrawLine(a, b, color, (available | taken) ? 4 : 2, antialiased: true);
	}

	public override void _Draw()
	{
		if (_run == null)
		{
			return;
		}
		IReadOnlyList<int> nextChoices = Run.NextChoices(_run);
		if (_run.Map.Count > 0)
		{
			for (int col = 0; col < _run.Map[0].Count; col++)
			{
				bool taken = _inferredPath.Contains(Key(0, col));
				DrawEdge(StartPoint, Center(0, col), _run.Row < 0 && nextChoices.Contains(col), taken, _run.Row >= 0 && !taken);
			}
		}
		for (int row = 0; row + 1 < _run.Map.Count; row++)
		{
			for (int i = 0; i < _run.Map[row].Count; i++)
			{
				foreach (int next in _run.Map[row][i].Next)
				{
					if (next >= 0 && next < _run.Map[row + 1].Count)
					{
						bool active = row == _run.Row && i == _run.Col && nextChoices.Contains(next);
						bool taken2 = _inferredPath.Contains(Key(row, i)) && _inferredPath.Contains(Key(row + 1, next));
						DrawEdge(Center(row, i), Center(row + 1, next), active, taken2, row < _run.Row && !taken2);
					}
				}
			}
		}
	}
}
