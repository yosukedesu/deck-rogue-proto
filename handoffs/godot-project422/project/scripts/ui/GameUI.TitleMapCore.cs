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

	private Texture2D? Art(string group, string id)
	{
		string path = $"res://assets/Art/{group}/{id}.png";
		if (!_textures.TryGetValue(path, out Texture2D texture))
		{
			texture = (ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null);
			if (texture == null && group == "leaders")
			{
				texture = SourceLeaderArtwork.Resolve(id);
			}
			if (texture == null && group == "gears")
			{
				try
				{
					texture = SourceArtwork.Gear(id, Content.GetGearDef(id).Family);
				}
				catch (Exception)
				{
				}
			}
			_textures[path] = texture;
		}
		return texture;
	}


	private void BuildTitle()
	{
		bool compactTitle = PhoneLayout && _textScale >= 1.15f;
		var brand = Text(_body, "DECK ROGUE", compactTitle ? 46 : 62, SourcePalette.BrassLight);
		brand.Name = "SourceTitleBrand";
		brand.AddThemeFontOverride("font", SourceRunUiDecor.Display);
		brand.AutowrapMode = TextServer.AutowrapMode.Off;
		if (!compactTitle) Text(_body, "坑へ降りる、その前に。", 20, SourcePalette.PaperDim);
		if (_session == null || !_session.IsInitialized)
		{
			Text(_body, _session?.InitializationError ?? "ゲームデータを読み込み中…", 22, Danger);
			return;
		}
		HBoxContainer? banner = null;
		if (_session.CanContinue)
		{
			BuildSavedRunSummary();
			if (_body.GetChild(_body.GetChildCount() - 1) is PanelContainer saved)
			{
				saved.SetMeta("source_run_paper", true);
				saved.AddThemeStyleboxOverride("panel", SourceRunUiDecor.Paper(SourcePalette.Paper, RunUnit, 10));
				RestyleTitlePaper(saved);
				banner = Row(saved.GetChild(0));
				banner.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
				banner.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			}
			else banner = Row(_body);
		}
		if (_session.CanContinue)
		{
			RunButton(banner!, "続きから", ContinueFromTitle, enabled: true, primary: true);
			RunButton(banner!, "セーブを放棄", () =>
			{
				Confirm("進行中のランを放棄？", "この端末のセーブを消します。", () =>
				{
					SessionResult sessionResult = _session.AbandonRun();
					if (sessionResult.Success)
					{
						_view = null;
						_showTitle = true;
						Rebuild();
					}
					else
					{
						Result(sessionResult);
					}
				});
			});
		}
		HBoxContainer main = Row(_body);
		main.Name = "SourceTitleLayout";
		main.SizeFlagsVertical = SizeFlags.ExpandFill;
		main.AddThemeConstantOverride("separation", 24);
		float detailWidth = Math.Clamp((PhoneLayout ? 340 : 400) * RunUnit * _textScale, 390, 580);
		int leaderColumns = PhoneLayout ? 4 : 5;
		float rosterWidth = Math.Max(600, RunSafeRect.Size.X - detailWidth - 48);
		float leaderWidth = (rosterWidth - 18 * (leaderColumns - 1)) / leaderColumns;
		VBoxContainer vBoxContainer = ScrollColumn(main);
		vBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var grid = new GridContainer { Name = "SourceTitleLeaders", Columns = leaderColumns,
			SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Pass };
		grid.AddThemeConstantOverride("h_separation", 18);
		grid.AddThemeConstantOverride("v_separation", 18);
		vBoxContainer.AddChild(grid);
		foreach (LeaderDef leader in _session.Catalog.Leaders)
		{
			bool selected = leader.Id == _leaderId;
			var pan = RunPaper(grid, "SourceTitleLeader_" + leader.Id, padding: 10);
			pan.CustomMinimumSize = new Vector2(leaderWidth, 0);
			pan.AddThemeStyleboxOverride("panel", SourceRunUiDecor.Paper(selected ? SourcePalette.BrassLight : SourcePalette.Paper, RunUnit, 10));
			VBoxContainer col = Column(pan);
			col.AddThemeConstantOverride("separation", 8);
			var band = new ColorRect { Color = CardColor(leader.Colors[0]), CustomMinimumSize = new Vector2(0, 6 * RunUnit), MouseFilter = MouseFilterEnum.Ignore };
			col.AddChild(band);
			Picture(col, "leaders", leader.Id, new Vector2(leaderWidth - 28 * RunUnit, (compactTitle ? 124 : 152) * RunUnit));
			var leaderName = Text(col, leader.Name, 25, SourcePalette.Ink);
			leaderName.AddThemeFontOverride("font", SourceRunUiDecor.Display);
			Text(col, $"{string.Join("・", leader.Colors.Select(TitleColorName))} · HP {leader.MaxHp}  E {leader.EnergyMax}\nドロー {leader.DrawPerTurn}  報酬 {leader.RewardChoices}", 17, SourcePalette.InkSoft).TooltipText = $"最大HP {leader.MaxHp}・エナジー {leader.EnergyMax}・毎ターンドロー {leader.DrawPerTurn}・報酬候補 {leader.RewardChoices}";
			void SelectLeader()
			{
				if (_leaderId == leader.Id || _overlay != null || IsInputBlocked) return;
				_leaderId = leader.Id;
				_deckId = null;
				Rebuild();
			}
			RunButton(col, selected ? "✓ 選択中" : "旅人を選ぶ", SelectLeader);
			BindTitleLeaderCard(pan, col, SelectLeader);
		}
		var detailPaper = RunPaper(main, "SourceTitleDetail", padding: 16);
		detailPaper.CustomMinimumSize = new Vector2(detailWidth, 0);
		detailPaper.SizeFlagsHorizontal = SizeFlags.Fill;
		detailPaper.SizeFlagsVertical = SizeFlags.ExpandFill;
		VBoxContainer sideFrame = Column(detailPaper);
		sideFrame.SizeFlagsVertical = SizeFlags.ExpandFill;
		sideFrame.AddThemeConstantOverride("separation", 6);
		LeaderDef chosen = _session.Catalog.Leaders.FirstOrDefault((LeaderDef x) => x.Id == _leaderId) ?? _session.Catalog.Leaders[0];
		IReadOnlyList<string> decks = chosen.RunDeckChoices ?? new string[1] { chosen.RunDeckId };
		if (_deckId == null || !decks.Contains(_deckId)) _deckId = chosen.RunDeckId;
		DeckDef currentDeck = Content.GetDeckDef(_deckId);
		var chosenName = Text(sideFrame, chosen.Name, 32, SourcePalette.BrassInk);
		chosenName.Name = "SourceTitleChosenLeader";
		chosenName.AddThemeFontOverride("font", SourceRunUiDecor.Display);
		Text(sideFrame, $"初期デッキ: {currentDeck.Name}（{currentDeck.Cards.Sum(c => c.Count)}枚）", 20, SourcePalette.Ink).Name = "SourceTitleStarterDeckSummary";
		ScrollContainer sideScroll = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Auto
		};
		sideFrame.AddChild(sideScroll, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer side = Column(sideScroll);
		side.CustomMinimumSize = new Vector2(detailWidth - 40 * RunUnit, 0f);
		if (decks.Count > 1)
		{
			foreach (string id in decks)
			{
				DeckDef deck = Content.GetDeckDef(id);
				RunButton(side, ((_deckId == id) ? "● " : "○ ") + deck.Name, () =>
				{
					_deckId = id;
					Rebuild();
				});
			}
		}
		Text(side, "旅人の特徴", 20, SourcePalette.BrassInk);
        var descriptionParts = chosen.Description.Split('。', StringSplitOptions.RemoveEmptyEntries);
        string role = descriptionParts.FirstOrDefault() ?? chosen.Name;
        string traits = string.Join("。", descriptionParts.Skip(Math.Max(1, descriptionParts.Length - 2)));
        Text(side, role, 20, SourcePalette.BrassInk);
        Text(side, traits, 18).AddThemeConstantOverride("line_spacing", 4);
        var lore = RunButton(side, "旅人の物語・能力を詳しく  ›", () => Open(chosen.Name, box => Text(ScrollColumn(box), chosen.Description, 22)));
        lore.Name = "SourceTitleLeaderDetails";
		Text(side, Run.DifficultyDescription(_difficulty), 14, Muted);
		var deckDetails = RunButton(side, "初期デッキの詳しい説明  ›", () => Open(currentDeck.Name, box =>
		{
			Text(ScrollColumn(box), currentDeck.Description, 20);
		}));
		deckDetails.Name = "SourceTitleDeckDetails";
		Text(side, "最初に持つ札", 20, SourcePalette.BrassInk);
		Text(side, string.Join("、", currentDeck.Cards.Select((DeckCardEntry c) => Content.GetCardDef(c.CardId).Name + "×" + c.Count)), 15);
		// A saved-run banner plus enlarged text leaves less vertical room.
        // Configuration can scroll; starting/continuing and utilities stay pinned.
        bool scrollSetup = compactTitle && _session.CanContinue;
        VBoxContainer fixedSide = Column(scrollSetup ? side : sideFrame);
		HBoxContainer seedRow = Row(fixedSide);
		Text(seedRow, "シード", 18);
		LineEdit seed = new LineEdit
		{
			Text = _seed.ToString(),
			CustomMinimumSize = new Vector2(130f * RunUnit, MinimumHit),
			MaxLength = 9,
			PlaceholderText = "1〜999999999"
		};
		seedRow.AddChild(seed, forceReadableName: false, InternalMode.Disabled);
		seed.Name = "SourceTitleSeed";
		StylePaperTextInput(seed);
		seed.TooltipText = "同じシード・旅人・難易度で同じ冒険を再現。1〜999999999の整数。";
		Button? startRun = null;
		bool seedValid = true;
		seed.TextChanged += (string value) =>
		{
			seedValid = int.TryParse(value, out var result) && result > 0 && result <= 999999999;
			if (seedValid) _seed = result;
			seed.AddThemeColorOverride("font_color", seedValid ? SourcePalette.Ink : SourcePalette.BadInk);
			seed.AddThemeStyleboxOverride("normal", SourceRunUiDecor.Paper(seedValid ? SourcePalette.Paper3 : new Color("f0d8d0"), RunUnit, 10, true));
			if (startRun != null) { startRun.Disabled = !seedValid; startRun.Text = seedValid ? "ランを開始" : "シードを入力してください"; }
		};
		RunButton(seedRow, "ランダム", () =>
		{
			_seed = GD.RandRange(1, 999999);
			Rebuild();
		});
		HBoxContainer diff = Row(fixedSide);
		RunButton(diff, "−", () =>
		{
			_difficulty--;
			Rebuild();
		}, _difficulty > 1);
		Text(diff, $"難易度 {_difficulty} / 10", 20);
		RunButton(diff, "+", () =>
		{
			_difficulty++;
			Rebuild();
		}, _difficulty < 10);

		startRun = RunButton(scrollSetup ? sideFrame : fixedSide, "ランを開始", () =>
		{
			if (!seedValid || !int.TryParse(seed.Text, out int displayedSeed) || displayedSeed < 1) return;
			_seed = displayedSeed;
			Action action = () =>
			{
				Sound("ui.start_run");
				_showTitle = false;
				Result(_session.CreateRun(_seed, _leaderId, _deckId, _difficulty, _reactionMode, _setAnyCards));
			};
			if (_session.CurrentRun != null || _session.HasSave)
			{
				Confirm("新しいランを始める？", "進行中のセーブを置き換えます。", action);
			}
			else
			{
				action();
			}
		}, enabled: true, primary: true);
		var footerPaper = RunPaper(_body, "SourceTitleUtilities", 6);
		var footer = Flow(footerPaper);
		footer.Name = "SourceTitleFixedFooter";
		RunButton(footer, "戦闘方式の設定", () =>
		{
			Open("戦闘方式", (VBoxContainer b) =>
			{
				Text(b, "標準は「仕込み・確認式」です。他の方式は元エンジンの実験用ルールです。");
				(string, string)[] array = new (string, string)[3]
				{
					("set-confirm", "仕込み・確認式"),
					("set-auto", "仕込み・自動式"),
					("hold-manual", "手持ち・手動式")
				};
				foreach ((string, string) tuple in array)
				{
					(string, string) pair = tuple;
					Button(b, ((_reactionMode == pair.Item1) ? "● " : "○ ") + pair.Item2, () =>
					{
						_reactionMode = pair.Item1;
						CloseOverlay();
					});
				}
				SettingsToggle(b, "実験: 通常のカードも仕込み可", _setAnyCards, value => _setAnyCards = value, "実験用ルールです");
			});
		});

		RunButton(footer, "設定", OpenSettings);
		RunButton(footer, "読み込む", OpenImport);
		RunButton(footer, "メニュー", OpenMenu);
		if (_session.HasRetainedReport) RunButton(footer, "保存したランのレポート", OpenRetainedExport);
	}


    private static string TitleColorName(string color) => color switch {
        "green" => "緑", "blue" => "青", "red" => "赤", "white" => "白", "black" => "黒", "purple" => "紫", _ => color
    };

	private void BuildMap(Node parent, RunState run, bool interactive)
	{
		SectionHeading(parent, $"幕 {run.Act} · マップ — 現在地と道筋", 30, Gold);
		Text(parent, interactive ? "▶ の道を選ぶと進みます。↑ 上へスクロールで先の道。その他の地点は説明を表示。" : "現在地と道筋を確認できます。ここでは進路を選べません。", 18, Muted);
		bool editable = interactive && !IsInputBlocked && _overlay == null;
		PanelContainer infoPanel = new PanelContainer
		{
			Name = "MapNodeInformation",
			Visible = false
		};
		infoPanel.AddThemeStyleboxOverride("panel", Box(SourcePalette.Paper, SourcePalette.Brass));
		parent.AddChild(infoPanel, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer infoRow = Row(infoPanel);
		Label infoText = Text(infoRow, "", 18, SourcePalette.Ink);
		infoText.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		infoText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		ScrollContainer scroll = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Auto
		};
		parent.AddChild(scroll, forceReadableName: false, InternalMode.Disabled);
		CenterContainer mapCenter = new CenterContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		scroll.AddChild(mapCenter, forceReadableName: false, InternalMode.Disabled);
		RunMap map = new RunMap
		{
			CustomMinimumSize = new Vector2(1100f, Math.Max(560, run.Map.Count * 112 + 140))
		};
		mapCenter.AddChild(map, forceReadableName: false, InternalMode.Disabled);
		map.Build(run, (int col) =>
		{
			Do(new RunCommand_ChooseNode
			{
				Col = col
			});
		}, editable, Theme, FontSize(16), PhoneLayout);
		map.NodeInformationChanged += (string text) =>
		{
			infoText.Text = text;
			infoPanel.Visible = text.Length > 0;
		};
		Button(infoRow, "閉じる", () =>
		{
			map.ClearNodeInformation();
		});
		FocusMapAfterLayout(scroll, map, run, interactive);
		JObject annotations = (_session?.MapAnnotations as JObject) ?? new JObject();
		string act = run.Act.ToString();
		JObject acts = (annotations["acts"] as JObject) ?? new JObject();
		annotations["acts"] = acts;
		map.LoadAnnotations(acts[act] as JArray);
		if (editable)
		{
			map.AnnotationsChanged += (JArray strokes) =>
			{
				acts[act] = strokes;
				annotations["version"] = 1;
				if (_session != null)
				{
					Result(_session.SetMapAnnotations(annotations));
				}
			};
		}
		var mapFooter = RunPaper(parent, "SourceMapToolsPanel", 10);
		var mapFooterBody = Column(mapFooter);
		HFlowContainer legend = Flow(mapFooterBody);
		legend.AddThemeConstantOverride("h_separation", 16);
		string[] array = new string[8] { "battle", "elite", "event", "campfire", "shop", "workshop", "treasure", "boss" };
		foreach (string type in array)
		{
			HBoxContainer item = Row(legend);
			Picture(item, "map", "node_" + ((type == "event") ? "unknown" : type), new Vector2(24f, 24f));
			string caption = type switch
			{
				"treasure" => "宝箱", 
				"event" => "？", 
				"boss" => "幕ボス", 
				"elite" => "強個体", 
				_ => FlowRules.NodeName(type), 
			};
			Label label = Text(item, caption, 16, SourcePalette.InkSoft);
			label.Name = "MapLegend_" + type;
			label.AutowrapMode = TextServer.AutowrapMode.Off;
			label.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			label.CustomMinimumSize = new Vector2(Mathf.Ceil(label.GetThemeFont("font").GetStringSize(caption, HorizontalAlignment.Left, -1f, label.GetThemeFontSize("font_size"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X), 0f);
			label.MouseFilter = MouseFilterEnum.Pass;
			label.TooltipText = RunMap.NodeDescription(new MapNode
			{
				Type = type
			});
		}
		Text(mapFooterBody, $"◆ 現在地　▶ 進める道　✓ 通過済み　· 未到達　｜ 幕 {run.Act}/3 · 勝利 {run.BattlesWon}戦", 16, SourcePalette.InkSoft);
		Button navigation;
		Button paper;
		Button red;
		Button eraser;
		Button clear;
		Button undo;
		Label hint;
		if (editable)
		{
			HFlowContainer tools = Flow(mapFooterBody);
			navigation = RunButton(tools, "道を選ぶ", () =>
			{
				map.SetDrawing(drawing: false);
			});
			paper = RunButton(tools, "紙ペン", () =>
			{
				map.TogglePen("f5edd6");
			});
			red = RunButton(tools, "朱ペン", () =>
			{
				map.TogglePen("e06b5c");
			});
			eraser = RunButton(tools, "消しゴム", map.ToggleEraser);
			clear = RunButton(tools, "全消し", map.ClearAnnotations);
			undo = RunButton(tools, "最後の線を消す", map.UndoAnnotation);
			hint = Text(mapFooterBody, "", 16, SourcePalette.InkSoft);
			map.ToolChanged += RefreshTools;
			RefreshTools();
		}
		void RefreshTools()
		{
			navigation.Text = (map.IsDrawing ? "道を選ぶ" : "● 道を選ぶ");
            SourceRunUiDecor.Button(navigation, RunUnit, !map.IsDrawing);
            SourceRunUiDecor.Button(paper, RunUnit, map.IsDrawing && !map.IsErasing && map.PenColor == "f5edd6");
            SourceRunUiDecor.Button(red, RunUnit, map.IsDrawing && !map.IsErasing && map.PenColor == "e06b5c");
            SourceRunUiDecor.Button(eraser, RunUnit, map.IsErasing);
			paper.Text = ((map.IsDrawing && !map.IsErasing && map.PenColor == "f5edd6") ? "● 紙ペン" : "紙ペン");
			red.Text = ((map.IsDrawing && !map.IsErasing && map.PenColor == "e06b5c") ? "● 朱ペン" : "朱ペン");
			eraser.Text = (map.IsErasing ? "● 消しゴム" : "消しゴム");
			Button button = clear;
			bool disabled = (undo.Disabled = map.AnnotationCount == 0);
			button.Disabled = disabled;
			Label label2 = hint;
			string text;
			if (map.IsErasing)
			{
				text = "消しゴム: 線に触れると消える（もう一度押すと解除）";
			}
			else if (map.IsDrawing)
			{
				text = "ペン: ドラッグで地図に描ける（もう一度押すと解除）";
			}
			else
			{
				text = (PhoneLayout ? "ペンを押すと指で地図に描ける" : "右ドラッグで地図に描ける（ペンを押すと左ドラッグでも）");
			}
			label2.Text = text;
		}
	}

}
