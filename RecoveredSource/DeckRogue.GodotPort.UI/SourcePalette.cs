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

    // Battle-only blue-ink surfaces. Paper, gold, faction and warning colors
    // retain their existing semantic roles; other run screens keep their theme.
    public static class Battle
    {
        public static readonly bool Enabled = !System.Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--battle-palette-baseline");
        public static readonly Color Night = Enabled ? new Color("1c2433") : SourcePalette.Night;
        public static readonly Color Window = Enabled ? new Color("273344") : SourcePalette.Window;
        public static readonly Color Raised = Enabled ? Window : new Color("262943");
        public static readonly Color Pill = Enabled ? new Color("29394b") : SourcePalette.NightPill;
        public static readonly Color HealthTrack = Enabled ? new Color("233141") : SourcePalette.NightPill;
        public static readonly Color InsetHealthTrack = Enabled ? HealthTrack : new Color("31344f");
        public static readonly Color Edge = Enabled ? new Color("8e9aa9") : SourcePalette.NightEdge;

        // Shared by the vector card shell and its live body/continuation layer.
        public static readonly string CardShellHex = Enabled ? "#223442" : "#233d46";
        public static readonly string CardBodyHex = Enabled ? "#2c3d4b" : "#354d55";
        public static readonly string CardEdgeHex = Enabled ? "#b9c8c6" : "#b5c6c5";
        public static readonly string CardLineHex = Enabled ? "#788c97" : "#69898e";
        public static readonly string CardWellHex = Enabled ? "#20313d" : "#223c45";
        public static readonly string CardShadowHex = Enabled ? "#172733" : "#1b2a2b";
        public static readonly string CardSheenOpacity = Enabled ? ".075" : ".095";
        public static readonly Color CardShell = new Color(CardShellHex);
        public static readonly Color CardDivider = new Color(Enabled ? "5c7280" : "5e7d7d");
    }

	public static Color Edge(string color)
	{
		return color switch
		{
			"blue" => new Color("4f8fd6"), 
			"red" => new Color("d65a4f"), 
			"white" => new Color("e8e2c8"), 
			"black" => new Color("6b4f8a"), 
			_ => new Color("5fb85a"), 
		};
	}
}
