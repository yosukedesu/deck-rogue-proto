// r2 phone geometry; compact overflow is an explicit width adaptation.
using System;
using System.Collections.Generic;
namespace DeckRogue.GodotPort.UI;
public readonly record struct SourceTokenRect(float X, float Y, float W, float H)
{
    public float Right => X + W;
    public float Bottom => Y + H;
    public bool Intersects(SourceTokenRect r) => X < r.Right && Right > r.X && Y < r.Bottom && Bottom > r.Y;
}
public sealed record SourceTokenCell(string Kind, int Index, int Hidden, SourceTokenRect Rect);
public sealed record SourceTokenSection(string Kind, SourceTokenRect Heading, float Width);
public sealed record SourceTokenLayoutResult(IReadOnlyList<SourceTokenCell> Cells, IReadOnlyList<SourceTokenSection> Sections, string Adaptation);
public static class SourceTokenLayout
{
    public const float BandTop = 62, TokenTop = 84, HeadingTop = BandTop - 9;
    public static SourceTokenLayoutResult Pack(float logicalWidth, float safeRight, int sets, int gears, int permanents,
        float setHeading, float gearHeading, float permHeading)
    {
        sets = Math.Max(0, sets); gears = Math.Max(0, gears); permanents = Math.Max(0, permanents);
        float right = Math.Min(logicalWidth - 24, safeRight), left = 24, available = Math.Max(0, right - left);
        var cells = new List<SourceTokenCell>(); var sections = new List<SourceTokenSection>();
        void Section(string kind, float x, float width, float heading) => sections.Add(new(kind, new(x - 4, HeadingTop, Math.Min(width + 4, heading), 25), width));
        float setW = sets * 78, gx = left + setW + 14;
        // Reserve paper+edge/badge extents. Source chooses at least one gear; adaptation may use only +N.
        float permReserve = permanents > 0 ? 168 + 14 : 0;
        float gearAvailable = right - gx - permReserve - 6;
        int shown = gears <= (int)((gearAvailable + 8) / 76) ? gears : Math.Max(0, (int)((gearAvailable - 52) / 76));
        shown = Math.Min(gears, shown);
        float gearW = gears == 0 ? 0 : shown * 76 + (shown < gears ? 52 : 0);
        gearW = gears == 0 ? 0 : Math.Max(gearW, gearHeading - 4);
        float px = gx + (gears > 0 ? gearW + 14 : 0);
        bool sourceFits = left + Math.Max(setW - 4, setHeading - 4) <= right &&
            (gears == 0 || gx + gearW + 6 <= right) && (permanents == 0 || px + 168 + 3 <= right);
        if (sourceFits)
        {
            Section("set", left, Math.Max(0, gx - left - 10), setHeading);
            for (int i = 0; i < sets; i++) cells.Add(new("set", i, 0, new(left + i * 78, TokenTop, 68, 74)));
            if (gears > 0)
            {
                Section("gear", gx, gearW, gearHeading);
                for (int i = 0; i < shown; i++) cells.Add(new("gear", i, 0, new(gx + i * 76, TokenTop, 68, 66)));
                if (shown < gears) cells.Add(new("gear-more", shown, gears - shown, new(gx + shown * 76, TokenTop, 44, 66)));
            }
            if (permanents > 0)
            {
                int cols = logicalWidth >= 1400 && px + 344 + 3 <= right ? 2 : 1;
                Section("permanent", px, cols * 176 - 8, permHeading);
                int count = permanents <= cols * 2 ? permanents : cols * 2 - 1;
                for (int i = 0; i < count; i++) cells.Add(new("permanent", i, 0, new(px + i % cols * 176, TokenTop + i / cols * 46, 168, 40)));
                if (count < permanents) cells.Add(new("permanent-more", count, permanents - count, new(px + count % cols * 176, TokenTop + count / cols * 46, 72, 40)));
            }
            return new(cells, sections, shown == 0 && gears > 0 ? "gear-overflow-only" : "source-r2");
        }
        // Do not move tokens down into the leader/hand. Collapse each section to a fully inspectable list.
        float need = (sets > 0 ? 68 + 14 : 0) + (gears > 0 ? 44 + 14 : 0) + (permanents > 0 ? 72 + 14 : 0) - 14 + 6;
        if (need <= available)
        {
            float x = left;
            void More(string kind, int count, float w, float h, float heading) { if (count <= 0) return; Section(kind, x, w, heading); cells.Add(new(kind + "-more", 0, count, new(x, TokenTop, w, h))); x += w + 14; }
            More("set", sets, 68, 74, setHeading); More("gear", gears, 44, 66, gearHeading); More("permanent", permanents, 72, 40, permHeading);
            return new(cells, sections, "compact-section-lists");
        }
        // Last resort stays within the same safe strip; all categories remain reachable from one list.
        float width = Math.Min(144, Math.Max(0, available - 6));
        if (width > 0) cells.Add(new("all-more", 0, sets + gears + permanents, new(left, TokenTop, width, 66)));
        return new(cells, sections, width >= 44 ? "combined-list" : "below-supported-width");
    }
}
