using System;
using System.Collections.Generic;

namespace DeckRogue.GodotPort.UI;

public static class SourceTokenLayout
{
	public const float BandTop = 62f;

	public const float TokenTop = 84f;

	public const float HeadingTop = 53f;

	public static SourceTokenLayoutResult Pack(float logicalWidth, float safeRight, int sets, int gears, int permanents, float setHeading, float gearHeading, float permHeading)
	{
		sets = Math.Max(0, sets);
		gears = Math.Max(0, gears);
		permanents = Math.Max(0, permanents);
		float right = Math.Min(logicalWidth - 24f, safeRight);
		float left = 24f;
		float available = Math.Max(0f, right - left);
		List<SourceTokenCell> cells = new List<SourceTokenCell>();
		List<SourceTokenSection> sections = new List<SourceTokenSection>();
		float setW = sets * 78;
		float gx = left + setW + 14f;
		float permReserve = ((permanents > 0) ? 182 : 0);
		float gearAvailable = right - gx - permReserve - 6f;
		int shown = ((gears <= (int)((gearAvailable + 8f) / 76f)) ? gears : Math.Max(0, (int)((gearAvailable - 52f) / 76f)));
		shown = Math.Min(gears, shown);
		float gearW = ((gears != 0) ? (shown * 76 + ((shown < gears) ? 52 : 0)) : 0);
		gearW = ((gears == 0) ? 0f : Math.Max(gearW, gearHeading - 4f));
		float px = gx + ((gears > 0) ? (gearW + 14f) : 0f);
		if (left + Math.Max(setW - 4f, setHeading - 4f) <= right && (gears == 0 || gx + gearW + 6f <= right) && (permanents == 0 || px + 168f + 3f <= right))
		{
			Section("set", left, Math.Max(0f, gx - left - 10f), setHeading);
			for (int i = 0; i < sets; i++)
			{
				cells.Add(new SourceTokenCell("set", i, 0, new SourceTokenRect(left + (float)(i * 78), 84f, 68f, 74f)));
			}
			if (gears > 0)
			{
				Section("gear", gx, gearW, gearHeading);
				for (int j = 0; j < shown; j++)
				{
					cells.Add(new SourceTokenCell("gear", j, 0, new SourceTokenRect(gx + (float)(j * 76), 84f, 68f, 66f)));
				}
				if (shown < gears)
				{
					cells.Add(new SourceTokenCell("gear-more", shown, gears - shown, new SourceTokenRect(gx + (float)(shown * 76), 84f, 44f, 66f)));
				}
			}
			if (permanents > 0)
			{
				int cols = ((!(logicalWidth >= 1400f) || !(px + 344f + 3f <= right)) ? 1 : 2);
				Section("permanent", px, cols * 176 - 8, permHeading);
				int count = ((permanents <= cols * 2) ? permanents : (cols * 2 - 1));
				for (int k = 0; k < count; k++)
				{
					cells.Add(new SourceTokenCell("permanent", k, 0, new SourceTokenRect(px + (float)(k % cols * 176), 84f + (float)(k / cols * 46), 168f, 40f)));
				}
				if (count < permanents)
				{
					cells.Add(new SourceTokenCell("permanent-more", count, permanents - count, new SourceTokenRect(px + (float)(count % cols * 176), 84f + (float)(count / cols * 46), 72f, 40f)));
				}
			}
			return new SourceTokenLayoutResult(cells, sections, (shown == 0 && gears > 0) ? "gear-overflow-only" : "source-r2");
		}
		float x;
		if ((float)(((sets > 0) ? 82 : 0) + ((gears > 0) ? 58 : 0) + ((permanents > 0) ? 86 : 0) - 14 + 6) <= available)
		{
			x = left;
			More("set", sets, 68f, 74f, setHeading);
			More("gear", gears, 44f, 66f, gearHeading);
			More("permanent", permanents, 72f, 40f, permHeading);
			return new SourceTokenLayoutResult(cells, sections, "compact-section-lists");
		}
		float width = Math.Min(144f, Math.Max(0f, available - 6f));
		if (width > 0f)
		{
			cells.Add(new SourceTokenCell("all-more", 0, sets + gears + permanents, new SourceTokenRect(left, 84f, width, 66f)));
		}
		return new SourceTokenLayoutResult(cells, sections, (width >= 44f) ? "combined-list" : "below-supported-width");
		void More(string kind, int num, float w, float h, float heading)
		{
			if (num > 0)
			{
				Section(kind, x, w, heading);
				cells.Add(new SourceTokenCell(kind + "-more", 0, num, new SourceTokenRect(x, 84f, w, h)));
				x += w + 14f;
			}
		}
		void Section(string kind, float num, float num2, float heading)
		{
			sections.Add(new SourceTokenSection(kind, new SourceTokenRect(num - 4f, 53f, Math.Min(num2 + 4f, heading), 25f), num2));
		}
	}
}
