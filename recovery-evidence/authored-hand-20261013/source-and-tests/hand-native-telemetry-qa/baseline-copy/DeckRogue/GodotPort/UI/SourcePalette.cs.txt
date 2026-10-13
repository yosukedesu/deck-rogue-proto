using Godot;

namespace DeckRogue.GodotPort.UI;

public static class SourcePalette
{
	public static readonly Color Paper = new Color("f4ecd6");

	public static readonly Color Paper2 = new Color("eadfc4");

	public static readonly Color Paper3 = new Color("fbf6e8");

	public static readonly Color PaperDim = new Color("c4beb2");

	public static readonly Color Ink = new Color("2f2e35");

	public static readonly Color InkSoft = new Color("4e4c55");

	public static readonly Color Night = new Color("1a1c33");

	public static readonly Color Window = new Color("20233a");

	public static readonly Color Ground = new Color("0f1120");

	public static readonly Color NightEdge = new Color("8a879c");

	public static readonly Color NightPill = new Color("2a2d48");

	public static readonly Color Brass = new Color("c99a3a");

	public static readonly Color BrassLight = new Color("ead08a");

	public static readonly Color BrassInk = new Color("634410");

	public static readonly Color Mana = new Color("3aa79b");

	public static readonly Color ManaLight = new Color("b5ddd6");

	public static readonly Color ManaInk = new Color("155650");

	public static readonly Color ManaBand = new Color("2a7d74");

	public static readonly Color BadInk = new Color("9c3a2a");

	public static readonly Color DangerButton = new Color("e8b8b0");

	public static Color Edge(string color)
	{
		if (1 == 0)
		{
		}
		Color result = color switch
		{
			"blue" => new Color("4f8fd6"), 
			"red" => new Color("d65a4f"), 
			"white" => new Color("e8e2c8"), 
			"black" => new Color("6b4f8a"), 
			_ => new Color("5fb85a"), 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
