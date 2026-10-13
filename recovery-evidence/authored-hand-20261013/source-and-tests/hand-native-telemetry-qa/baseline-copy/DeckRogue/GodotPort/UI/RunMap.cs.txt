// Source-backed map controls reconstructed from Unity MapScreen.cs and MapDoodle.cs.
// Coordinate origin and persisted stroke positions retain Godot bd66ae1 compatibility.
// No whole-run screenshot reference exists; this is not a measured visual-parity claim.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Godot;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.UI;

public partial class RunMap : Control
{
    private sealed class Stroke
    {
        public string Color = PaperPen;
        public List<Vector2> Points = new();
    }

    public const string PaperPen = "f5edd6";
    public const string RedPen = "e06b5c";
    private const float EraseRadius = 18f;
    private readonly List<Stroke> _strokes = new();
    private readonly List<Button> _nodeButtons = new();
    private readonly HashSet<long> _inferredPath = new();
    private Stroke? _drawingStroke;
    private Control? _annotationLayer;
    private RunState? _run;
    private bool _editable;
    private bool _drawing;
    private bool _erasing;
    private bool _strokeChanged;
    private string _pen = PaperPen;
    private MouseButton _pointerButton = MouseButton.None;
    private string? _inspectedNode;
    private bool _phone;

    public bool IsDrawing => _drawing;
    public bool IsErasing => _drawing && _erasing;
    public string PenColor => _pen;
    public int AnnotationCount => _strokes.Count;
    public event Action<JArray>? AnnotationsChanged;
    public event Action? ToolChanged;
    public event Action<string>? NodeInformationChanged;

    public void LoadAnnotations(JArray? strokes)
    {
        _strokes.Clear();
        if (strokes != null)
            foreach (JToken token in strokes)
            {
                if (token is not JObject item) continue;
                var stroke = new Stroke { Color = item["color"]?.Type == JTokenType.String ? (string?)item["color"] ?? PaperPen : PaperPen };
                if (item["points"] is JArray points)
                    foreach (JToken tokenPoint in points)
                        if (tokenPoint is JArray { Count: 2 } point &&
                            float.TryParse(point[0].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                            float.TryParse(point[1].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                            float.IsFinite(x) && float.IsFinite(y))
                            stroke.Points.Add(new Vector2(x, y));
                if (stroke.Points.Count > 0) _strokes.Add(stroke);
            }
        RedrawAnnotations();
    }

    // Compatibility with the preserved generated Godot method binding.
    public void SetDrawing(bool drawing, string color = PaperPen)
    {
        FinishPointer();
        _drawing = _editable && drawing;
        _erasing = false;
        _pen = color;
        UpdateToolState();
    }

    public void TogglePen(string color)
    {
        bool next = !(_drawing && !_erasing && _pen == color);
        SetDrawing(next, color);
    }

    public void ToggleEraser()
    {
        FinishPointer();
        bool next = !IsErasing;
        _drawing = _editable && next;
        _erasing = _drawing; // Toggling off must restore right-drag's paper pen.
        UpdateToolState();
    }

    private void UpdateToolState()
    {
        MouseDefaultCursorShape = _drawing ? CursorShape.Cross : CursorShape.Arrow;
        foreach (Button button in _nodeButtons)
        {
            // Pen mode receives the pointer even over a node. Navigation mode keeps
            // unavailable nodes inspectable, but their callbacks never choose a route.
            button.MouseFilter = _drawing ? MouseFilterEnum.Ignore : MouseFilterEnum.Pass;
            button.FocusMode = _drawing ? FocusModeEnum.None : FocusModeEnum.All;
        }
        ToolChanged?.Invoke();
    }

    public void UndoAnnotation()
    {
        if (!_editable) return;
        FinishPointer();
        if (_strokes.Count == 0) return;
        _strokes.RemoveAt(_strokes.Count - 1);
        SaveAnnotations();
        RedrawAnnotations();
    }

    public void ClearAnnotations()
    {
        if (!_editable) return;
        FinishPointer();
        if (_strokes.Count == 0) return;
        _strokes.Clear();
        SaveAnnotations();
        RedrawAnnotations();
    }

    private void SaveAnnotations()
    {
        var array = new JArray();
        foreach (Stroke stroke in _strokes)
        {
            var points = new JArray();
            foreach (Vector2 point in stroke.Points) points.Add(new JArray(point.X, point.Y));
            array.Add(new JObject { ["color"] = stroke.Color, ["points"] = points });
        }
        AnnotationsChanged?.Invoke(array);
        ToolChanged?.Invoke();
    }

    public override void _GuiInput(InputEvent evt)
    {
        Vector2 p = evt is InputEventMouse mouse ? mouse.Position : GetLocalMousePosition();
        if (HandlePointer(evt, p)) AcceptEvent();
    }

    private bool HandlePointer(InputEvent evt, Vector2 p)
    {
        if (!_editable) return false;
        if (evt is InputEventMouseButton mouse)
        {
            if (mouse.Pressed)
            {
                bool right = mouse.ButtonIndex == MouseButton.Right;
                bool left = mouse.ButtonIndex == MouseButton.Left && _drawing;
                if (!right && !left) return false;
                FinishPointer();
                _pointerButton = mouse.ButtonIndex;
                _strokeChanged = false;
                ClearNodeInformation();
                // Match source: right-drag uses paper when pen mode is off.
                if (_erasing) EraseAt(p);
                else
                {
                    _drawingStroke = new Stroke { Color = _drawing ? _pen : PaperPen };
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
            if (_erasing) EraseAt(p);
            else if (_drawingStroke != null && _drawingStroke.Points[^1].DistanceSquaredTo(p) >= 4f)
            {
                _drawingStroke.Points.Add(p);
                _strokeChanged = true;
            }
            RedrawAnnotations();
            return true;
        }
        return false;
    }

    private void FinishPointer()
    {
        bool changed = _strokeChanged;
        _pointerButton = MouseButton.None;
        _drawingStroke = null;
        _strokeChanged = false;
        if (changed) SaveAnnotations();
    }

    private void EraseAt(Vector2 p)
    {
        int removed = _strokes.RemoveAll(stroke => NearStroke(stroke, p));
        if (removed > 0) _strokeChanged = true;
    }

    private static bool NearStroke(Stroke stroke, Vector2 p)
    {
        for (int i = 0; i < stroke.Points.Count; i++)
        {
            if (stroke.Points[i].DistanceSquaredTo(p) <= EraseRadius * EraseRadius) return true;
            if (i > 0 && DistanceToSegment(p, stroke.Points[i - 1], stroke.Points[i]) <= EraseRadius) return true;
        }
        return false;
    }

    public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSquared = ab.LengthSquared();
        float t = lengthSquared > 0 ? Math.Clamp((p - a).Dot(ab) / lengthSquared, 0f, 1f) : 0f;
        return (a + ab * t).DistanceTo(p);
    }

    private void RedrawAnnotations()
    {
        if (_annotationLayer != null && GodotObject.IsInstanceValid(_annotationLayer)) _annotationLayer.QueueRedraw();
        QueueRedraw();
    }

    private void DrawAnnotations()
    {
        if (_annotationLayer == null) return;
        foreach (Stroke stroke in _strokes)
        {
            Color color = Color.FromString(stroke.Color, SourcePalette.Paper);
            if (stroke.Points.Count == 1) _annotationLayer.DrawCircle(stroke.Points[0], 2f, color);
            else _annotationLayer.DrawPolyline(stroke.Points.ToArray(), color, 4f, antialiased: true);
        }
    }

    // Keep the existing center positions to avoid shifting saved annotations.
    private Vector2 Point(int row, int col) => new(62 + (_run!.Map[row][col].Col ?? col) * 146, 40 + (_run.Map.Count - row - 1) * 112);
    private Vector2 Center(int row, int col) => Point(row, col) + new Vector2(43, 43);
    private Vector2 StartPoint => new(550, 83 + (_run?.Map.Count ?? 0) * 112);
    private static long Key(int row, int col) => ((long)row << 32) | (uint)col;
    private float NodeSize(MapNode node) => node.Type == "boss" ? (_phone ? 104 : 92) : (_phone ? 74 : 60);

    public static HashSet<long> InferPath(RunState run)
    {
        var path = new HashSet<long>();
        if (run.Row < 0 || run.Row >= run.Map.Count || run.Col < 0 || run.Col >= run.Map[run.Row].Count) return path;
        int row = run.Row, col = run.Col;
        path.Add(Key(row, col));
        // Same closest-parent heuristic as Unity MapScreen.cs:70–92. It is not
        // actual route history and cannot invent an edge for a wing-boots jump.
        while (row > 0)
        {
            int best = -1, bestDistance = int.MaxValue;
            for (int i = 0; i < run.Map[row - 1].Count; i++)
            {
                var candidate = run.Map[row - 1][i];
                if (!candidate.Next.Contains(col)) continue;
                int distance = Math.Abs((candidate.Col ?? 0) - (run.Map[row][col].Col ?? 0));
                if (distance < bestDistance) { best = i; bestDistance = distance; }
            }
            if (best < 0) break;
            col = best; row--;
            path.Add(Key(row, col));
        }
        return path;
    }

    public static string NodeDescription(MapNode node)
    {
        string encounter = "";
        if (!string.IsNullOrEmpty(node.EncounterId))
        {
            try { encounter = "\n" + Content.EncounterName(node.EncounterId); }
            catch (Exception) { encounter = "\n" + node.EncounterId; }
        }
        return node.Type switch
        {
            "battle" => "戦闘" + encounter + "\n勝てばカード報酬とゴールド",
            "elite" => "強個体" + encounter + "\n勝てばレリック3択・レア確定の報酬・ゴールド",
            "boss" => "幕ボス" + encounter + "\n倒すと全回復・レリック3択・次の幕へ",
            "campfire" => "焚き火\n休む（最大HPの割合で回復）か、カードを1枚鍛える",
            "shop" => "ショップ\nカード・レリック・カード除去・鍛える",
            "workshop" => "工房\n同じ色のカード2枚を1枚に溶かす（" + Run.WORKSHOP_FUSE_PRICE + "G）",
            "event" => "？\n入るまで中身は分からない（イベント・戦闘・店・宝箱）",
            "treasure" => "宝箱\nレリック3択",
            _ => FlowRules.NodeName(node.Type) + encounter
        };
    }

    public void ClearNodeInformation()
    {
        _inspectedNode = null;
        NodeInformationChanged?.Invoke("");
    }

    private void InspectNode(string key, string text)
    {
        if (_inspectedNode == key) { ClearNodeInformation(); return; }
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
        foreach (long key in InferPath(run)) _inferredPath.Add(key);
        var next = Run.NextChoices(run);
        var wings = Run.WingChoices(run);
        int recipeCount = Fusion.RecipePairsInDeck(run.Deck).Count;
        for (int row = 0; row < run.Map.Count; row++)
            for (int col = 0; col < run.Map[row].Count; col++)
            {
                int index = col;
                MapNode node = run.Map[row][col];
                bool accessible = row == run.Row + 1 && (next.Contains(col) || wings.Contains(col));
                bool current = row == run.Row && col == run.Col;
                bool taken = _inferredPath.Contains(Key(row, col));
                bool passed = row <= run.Row;
                float size = NodeSize(node);
                string key = row + ":" + col;
                string tip = NodeDescription(node) + $"\n{row + 1}層" + (current ? "・現在地" : accessible ? next.Contains(col) ? "・進む" : "・翼の靴を使う" : "");
                if (node.Type == "workshop" && recipeCount > 0 && !passed)
                {
                    int price = Run.WorkshopFusePrice(run);
                    tip += $"\n★ 今のデッキでレシピが {recipeCount} 組作れる（{price}G" + (run.Gold < price ? "・所持金が足りない）" : "）");
                }
                Color tint = NodeTint(node.Type, accessible, current, taken, passed);
                var button = new Button {
                    Name = "MapNode_" + row + "_" + col, Text = "", Position = Center(row, col) - Vector2.One * size / 2,
                    Size = Vector2.One * size, CustomMinimumSize = Vector2.One * size,
                    MouseFilter = MouseFilterEnum.Pass, TooltipText = tip
                };
                button.SetMeta("route_available", interactive && accessible);
                var box = new StyleBoxFlat {
                    BgColor = tint, BorderColor = current ? SourcePalette.Brass : accessible ? SourcePalette.BrassLight : SourcePalette.NightEdge,
                    BorderWidthLeft = current || accessible ? 3 : 1, BorderWidthRight = current || accessible ? 3 : 1,
                    BorderWidthTop = current || accessible ? 3 : 1, BorderWidthBottom = current || accessible ? 3 : 1,
                    CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5
                };
                button.AddThemeStyleboxOverride("normal", box);
                button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
                var hover = (StyleBoxFlat)box.Duplicate(); hover.BorderColor = SourcePalette.BrassLight;
                button.AddThemeStyleboxOverride("hover", hover);
                button.AddThemeStyleboxOverride("pressed", hover);
                AddChild(button);
                _nodeButtons.Add(button);
                string art = node.Type == "event" ? "unknown" : node.Type;
                AddPicture(button, "res://assets/Art/map/node_" + art + ".png", new Rect2((size - (node.Type == "boss" ? 56 : 34)) / 2, (size - (node.Type == "boss" ? 56 : 34)) / 2, node.Type == "boss" ? 56 : 34, node.Type == "boss" ? 56 : 34), passed && !current ? .45f : 1f);
                AddLabel(button, FlowRules.NodeName(node.Type), new Rect2(-30, size + 1, size + 60, 24), labelFontSize, passed && !current ? SourcePalette.PaperDim.Darkened(.3f) : SourcePalette.Paper);
                if (node.Type == "workshop" && recipeCount > 0 && !passed)
                    AddLabel(button, "★", new Rect2(size - 8, -16, 30, 28), labelFontSize + 3, SourcePalette.BrassLight);
                button.Pressed += () => {
                    if (_drawing || _pointerButton != MouseButton.None) return;
                    if (_editable && accessible) choose(index);
                    else InspectNode(key, tip);
                };
                // Godot can deliver the right-button capture to a child Button.
                // Forward it explicitly, so right-drag over an available node cannot
                // accidentally choose that node or depend on propagation details.
                button.GuiInput += evt => {
                    Vector2 point = evt is InputEventMouse mouse ? button.Position + mouse.Position : GetLocalMousePosition();
                    if (HandlePointer(evt, point)) button.AcceptEvent();
                };
            }
        AddPicture(this, "res://assets/Art/icons/flag.png", new Rect2(StartPoint - new Vector2(20, 20), new Vector2(40, 40)));
        AddLabel(this, "スタート", new Rect2(StartPoint + new Vector2(-60, 22), new Vector2(120, 24)), labelFontSize, SourcePalette.PaperDim);
        Vector2 marker = run.Row >= 0 && run.Row < run.Map.Count && run.Col >= 0 && run.Col < run.Map[run.Row].Count ? Center(run.Row, run.Col) : StartPoint;
        AddPicture(this, "res://assets/Art/leaders/" + run.LeaderId + "_icon.png", new Rect2(marker + new Vector2(-94, -56), new Vector2(56, 56)));
        _annotationLayer = new Control { Name = "MapAnnotations", MouseFilter = MouseFilterEnum.Ignore, Size = CustomMinimumSize };
        AddChild(_annotationLayer);
        _annotationLayer.Draw += DrawAnnotations;
        QueueRedraw();
    }

    private static Color NodeTint(string type, bool accessible, bool current, bool taken, bool passed)
    {
        Color color = type switch {
            "elite" => new("e0b0b0"), "boss" or "campfire" => new("f0c8a0"),
            "shop" => new("e8dca0"), "workshop" => new("d0c8e8"), "treasure" => new("f0e0a0"),
            "event" => new("b0e0e0"), _ => SourcePalette.Paper3
        };
        if (accessible || current) return color;
        return color.Darkened(taken ? .2f : passed ? .55f : .3f);
    }

    private static void AddLabel(Control parent, string text, Rect2 rect, int size, Color color)
    {
        var label = new Label { Text = text, Position = rect.Position, Size = rect.Size, HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        parent.AddChild(label);
    }

    private static void AddPicture(Control parent, string path, Rect2 rect, float alpha = 1)
    {
        if (!ResourceLoader.Exists(path)) return;
        parent.AddChild(new TextureRect { Texture = GD.Load<Texture2D>(path), Position = rect.Position, Size = rect.Size,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest, MouseFilter = MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, alpha) });
    }

    private void DrawEdge(Vector2 a, Vector2 b, bool available, bool taken, bool passed)
    {
        Color color = available ? SourcePalette.BrassLight : taken ? SourcePalette.Brass : passed ? new Color(1, 1, 1, .12f) : new Color(1, 1, 1, .32f);
        DrawLine(a, b, color, available || taken ? 4 : 2, antialiased: true);
    }

    public override void _Draw()
    {
        if (_run == null) return;
        var nextChoices = Run.NextChoices(_run);
        if (_run.Map.Count > 0)
            for (int col = 0; col < _run.Map[0].Count; col++)
            {
                bool taken = _inferredPath.Contains(Key(0, col));
                DrawEdge(StartPoint, Center(0, col), _run.Row < 0 && nextChoices.Contains(col), taken, _run.Row >= 0 && !taken);
            }
        for (int row = 0; row + 1 < _run.Map.Count; row++)
            for (int col = 0; col < _run.Map[row].Count; col++)
                foreach (int next in _run.Map[row][col].Next)
                {
                    if (next < 0 || next >= _run.Map[row + 1].Count) continue;
                    bool active = row == _run.Row && col == _run.Col && nextChoices.Contains(next);
                    bool taken = _inferredPath.Contains(Key(row, col)) && _inferredPath.Contains(Key(row + 1, next));
                    DrawEdge(Center(row, col), Center(row + 1, next), active, taken, row < _run.Row && !taken);
                }
    }
}
