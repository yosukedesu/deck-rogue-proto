using System;

using System.Collections.Generic;

using System.ComponentModel;

using System.Globalization;

using System.Linq;

using System.Runtime.CompilerServices;

using System.Text;

using System.Text.RegularExpressions;

using DeckRogue.Engine;

using DeckRogue.Engine.Generated;

using DeckRogue.GodotPort.Core;

using DeckRogue.GodotPort.Presentation;

using DeckRogue.Platform;

using Godot;

using Godot.Bridge;

using Godot.Collections;

using Godot.NativeInterop;

using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.UI;

public partial class GameUI
{

	private void BuildHeader()
	{
		if (CurrentRoute == "combat")
		{
			BuildBattleHeader();
			return;
		}
		// Title owns its brand and fixed utility footer. Do not add a second header.
        if (_showTitle || _view == null || CurrentRoute == "won" || CurrentRoute == "lost") return;
		HFlowContainer row = Flow(_shell);
		row.Name = "SourceRunHeader";
		row.AddThemeConstantOverride("h_separation", (int)Math.Ceiling(8 * RunUnit));
		row.AddThemeConstantOverride("v_separation", (int)Math.Ceiling(6 * RunUnit));
		row.CustomMinimumSize = new Vector2(0f, MinimumHit);
		if (!_showTitle && _view != null)
		{
			RunState run = _view;
			if (!PhoneLayout)
			{
				Picture(row, "leaders", run.LeaderId + "_icon", new Vector2(48f, 48f));
			}
			RunHeaderStat(row, "SourceRunProgress", SourceRunScreenPolicy.Progress(run) + " · " + FlowRules.PhaseName(run.Phase), PhoneLayout ? 17 : 19, Gold);
			_hpReadout = RunHeaderStat(row, "SourceRunHealth", $"HP {run.Hp}/{run.MaxHp}", PhoneLayout ? 17 : 20, (run.Hp <= run.MaxHp / 3) ? Danger : Accent);
			RunHeaderStat(row, "SourceRunGold", $"{run.Gold} G", PhoneLayout ? 17 : 20, Gold);
			RunHeaderStat(row, "SourceRunManaTag", "魔素 " + Gears.ManaLabel(run.Mana.GetValueOrDefault()), PhoneLayout ? 16 : 18, Accent).Name = "SourceRunMana";
			if (!PhoneLayout && run.Phase != "map")
			{
				RunButton(row, "マップ", () =>
				{
					Open("坑道の地図", (VBoxContainer b) =>
					{
						BuildMap(b, run, interactive: false);
					});
				});
			}
			RunButton(row, $"デッキ {run.Deck.Count}", () =>
			{
				OpenDeck(run.Deck, "デッキ");
			});
			if (!PhoneLayout)
			{
				RunButton(row, $"遺物 {run.Relics.Count}", () =>
				{
					OpenRelics(run);
				});
				RunButton(row, "メモ", OpenNote);
				RunButton(row, "書き出す", OpenExport);
			}
		}
		RunButton(row, "メニュー", OpenMenu);
	}


	private Label RunHeaderText(Node parent, string text, int size, Color color)
	{
		Label label = Text(parent, text, size, color);
		label.AutowrapMode = TextServer.AutowrapMode.Off;
		label.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		return label;
	}


	private Label RunHeaderStat(Node parent, string name, string text, int size, Color color)
	{
        var tag = new PanelContainer { Name = name };
        // Passive values deliberately do not share the paper-button frame.
        tag.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color(SourcePalette.Night, .78f),
            CornerRadiusTopLeft = 7, CornerRadiusTopRight = 7,
            CornerRadiusBottomLeft = 7, CornerRadiusBottomRight = 7,
            ContentMarginLeft = 7 * RunUnit, ContentMarginRight = 7 * RunUnit,
            ContentMarginTop = 7 * RunUnit, ContentMarginBottom = 7 * RunUnit
        });
        parent.AddChild(tag);
		tag.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		tag.CustomMinimumSize = new Vector2(0, MinimumHit);
		tag.MouseFilter = MouseFilterEnum.Ignore;
        var label = RunHeaderText(tag, text, Math.Max(size, 18), color);
        // Compact numerical HUD readouts use the crisp sans face and near-white
        // ink; the calligraphic body font loses too much stroke weight at phone scale.
        if (ResourceLoader.Exists("res://assets/Fonts/NotoSansJP-Regular.otf"))
            label.AddThemeFontOverride("font", GD.Load<Font>("res://assets/Fonts/NotoSansJP-Regular.otf"));
        label.AddThemeColorOverride("font_color", color == Danger ? new Color("ffd0c8")
            : color == Gold ? new Color("ffe6a5") : new Color("effffb"));
		label.VerticalAlignment = VerticalAlignment.Center;
		return label;
	}
}
