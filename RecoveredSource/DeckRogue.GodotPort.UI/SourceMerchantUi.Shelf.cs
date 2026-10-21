// NEW IMPLEMENTATION CANDIDATE. The completed helper's sizing formula is lost.
// This proposes wrapping before shrinking text; it is NOT a recovered design.
// Must pass final-layout/pointer QA before integration.
using System;

namespace DeckRogue.GodotPort.UI;

public static partial class SourceMerchantUi
{
    public readonly record struct Shelf(int Columns, float CellWidth, float CardScale);

    public static Shelf CardShelf(float width, int count, float textScale, bool departure)
    {
        // Gap and minimum price-button width come from the recovered caller.
        const float gap = 18f;
        const float cardCellPadding = 16f; // SourceCard holder adds 8 pixels on each side.
        if (!float.IsFinite(textScale) || textScale <= 0) textScale = 1f;
        float priceWidth = 176f * textScale;
        if (!float.IsFinite(width) || width <= 0) width = priceWidth;
        // New explicit design choice: natural-size cards, wider at desktop;
        // enlarged labels reduce the column count instead of becoming smaller.
        float preferredScale = departure ? 1.1f : 1.25f;
        float preferredWidth = Math.Max(priceWidth, SourceCardFace.W * preferredScale + cardCellPadding);
        int columns = Math.Max(1, Math.Min(Math.Max(1, count), (int)Math.Floor((width + gap) / (preferredWidth + gap))));
        float cellWidth = (width - gap * (columns - 1)) / columns;
        float cardScale = Math.Min(preferredScale, Math.Max(1f, cellWidth - cardCellPadding) / SourceCardFace.W);
        return new Shelf(columns, cellWidth, cardScale);
    }
}
