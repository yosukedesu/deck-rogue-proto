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

	private void PrepareReaction(GameState state)
	{
		ReactionWindow window = Effects.WindowFromPending(state);
		if (window == null)
		{
			return;
		}
		bool manual = state.ReactionMode == "hold-manual";
		IReadOnlyList<CardInstance> readOnlyList2;
		if (!manual)
		{
			IReadOnlyList<CardInstance> readOnlyList = Effects.UsableSetCards(state, window);
			readOnlyList2 = readOnlyList;
		}
		else
		{
			readOnlyList2 = HoldManualSystem.PlayableReactions(state);
		}
		IReadOnlyList<CardInstance> usable = readOnlyList2;
		HashSet<string> available = usable.Select((CardInstance c) => c.Uid).ToHashSet();
		CardInstance[] all = (manual ? state.Player.Hand.Where((CardInstance c) => c.Def.Type == "reaction").ToArray() : state.Player.SetCards.ToArray());
		CardInstance[] candidates = usable.Concat(all.Where((CardInstance c) => !available.Contains(c.Uid))).ToArray();
		int enemy = state.PendingWindow?.EnemyIndex ?? 0;
		int candidateScroll = 0;
		_overlayTitle = "からくりの確認 · " + ((state.PendingWindow?.Stage == "post") ? "行動の後" : "行動の前");
		_overlayCanClose = false;
		_overlay = (VBoxContainer box) =>
		{
			if (enemy >= 0 && enemy < state.Enemies.Count)
			{
				EnemyIntent it = Effects.EffectiveIntent(state, enemy) ?? state.Enemies[enemy].Intent;
				Text(box, Content.GetEnemyDef(state.Enemies[enemy].EnemyId).Name + " · " + CardText.LiveIntentLine(state, enemy, it), 23, Danger);
				if (window.Stage == "post")
				{
					Text(box, $"受けた HP −{window.HpLoss}（{state.Player.Hp + window.HpLoss} → {state.Player.Hp}）", 20, Gold);
				}
				else
				{
					string text = ReactionPreview.Outcome(state, enemy, new Command_ConfirmReaction
					{
						Fire = false
					}, fired: false);
					if (text != null)
					{
						Text(box, text, 20, Gold);
					}
				}
			}
			VBoxContainer vBoxContainer = ScrollColumn(box);
			ScrollContainer candidateList = (ScrollContainer)vBoxContainer.GetParent();
			candidateList.Name = "ReactionCandidates";
			candidateList.SetDeferred(ScrollContainer.PropertyName.ScrollVertical, candidateScroll);
			candidateList.GetVScrollBar().ValueChanged += value => candidateScroll = (int)value;
			CardInstance[] array = candidates;
			foreach (CardInstance card in array)
			{
				bool flag = available.Contains(card.Uid);
				PanelContainer panelContainer = RunPaper(vBoxContainer, "ReactionCandidate", 8f);
				HBoxContainer hBoxContainer = Row(panelContainer);
				Texture2D texture2D = SourceArtwork.Card(card.Def.Id, card.Def.Type);
				if (texture2D != null)
				{
					hBoxContainer.AddChild(new TextureRect
					{
						Texture = texture2D,
						TextureFilter = TextureFilterEnum.Nearest,
						ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
						StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
						CustomMinimumSize = new Vector2(96f, 108f),
						MouseFilter = MouseFilterEnum.Ignore
					}, forceReadableName: false, InternalMode.Disabled);
				}
				VBoxContainer parent = Column(hBoxContainer);
				Text(parent, card.Def.Name + (manual ? "" : (" · " + Effects.TrapStatusTextKarakuri(state, card))), 21, Gold);
				string[] hideTriggers = ((window.Stage == "post") ? new string[3] { "onAttacked", "onEnemyBuffed", "onEnemyDefended" } : new string[2] { "onEnemyAction", "onAttackIncoming" });
				Text(parent, CardText.Body(card.Def, withTriggers: true, hideTriggers), 18);
				Command command = (manual ? ((Command)new Command_ReactManual
				{
					CardUid = card.Uid
				}) : ((Command)new Command_ConfirmReaction
				{
					Fire = true,
					CardUid = card.Uid
				}));
				string text2 = (flag ? (ReactionPreview.Outcome(state, enemy, command, fired: true) ?? Effects.SetCardLiveDamage(state, card.Def, enemy)) : "この窓では発動できません");
				if (text2 != null)
				{
					Text(parent, text2, 18, flag ? Accent : Muted);
				}
				VBoxContainer vBoxContainer2 = new VBoxContainer();
				hBoxContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
                vBoxContainer2.AddThemeConstantOverride("separation", 10);
                var activate = RunButton(vBoxContainer2, "発動する", () =>
                {
                    Do(command);
                }, flag, primary: true);
                activate.TooltipText = card.Def.Name + "を発動";
                activate.Name = "ReactionActivate_" + card.Uid;
				RunButton(vBoxContainer2, "詳しく見る", () =>
				{
					InspectCard(card);
				});
			}
			Button(box, "温存して続ける", () =>
			{
				Do(new Command_ConfirmReaction
				{
					Fire = false
				});
			}, enabled: true, Gold);
		};
	}


	private void BuildUxPockets(GameState s)
	{
		CaptureSourceTokenBoard();
		_sourceTokenBand = null;
		{
			SourceTokenData tokens = _sourceTokenData;
			if ((object)tokens != null && (!PhoneLayout || tokens.Sets.Count > 0 || tokens.Gears.Count > 0 || tokens.Permanents.Count > 0))
			{
				BuildSourceTokenBand();
				return;
			}
		}
		float u = BattleUnit;
		float y = (PhoneLayout ? (84f * u) : (Size.Y - 400f * u));
		PanelContainer count = UxPanel(_uxBattle, "SetCountNote", new Rect2(RunSafeRect.Position.X, y - 30f * u, 145f * u, 28f * u));
		UxText(count, "からくり " + s.Player.SetCards.Count + " / " + s.Player.SetSlots, 15, SourcePalette.Paper);
		for (int i = 0; i < s.Player.SetSlots; i++)
		{
			int slot = i;
			Button b = UxButton(_uxBattle, "SetPocket_" + i, "", new Rect2(RunSafeRect.Position.X + i * 78f * u, y, 68f * u, 74f * u), () =>
			{
				if (slot < s.Player.SetCards.Count)
				{
					InspectCard(s.Player.SetCards[slot]);
				}
			});
			string[] array = new string[6] { "normal", "hover", "pressed", "disabled", "focus", "hover_pressed" };
			foreach (string stateStyle in array)
			{
				b.AddThemeStyleboxOverride(stateStyle, new StyleBoxEmpty());
			}
			SourceBattleDecor.Pocket(b, 68f, 74f, u);
			if (i < s.Player.SetCards.Count)
			{
				TextureRect pic = new TextureRect
				{
					Texture = SourceArtwork.Card(s.Player.SetCards[i].Def.Id, s.Player.SetCards[i].Def.Type),
					ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
					StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
					MouseFilter = MouseFilterEnum.Ignore,
					TextureFilter = TextureFilterEnum.Nearest
				};
				b.AddChild(pic, forceReadableName: false, InternalMode.Disabled);
				pic.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
			}
		}
	}


	private void RefreshSourceTokens()
	{
		if (!(_sourceTokenData == null) && _sourceTokenBand != null && GodotObject.IsInstanceValid(_sourceTokenBand) && _sourceTokenRenderedStamp != _sourceTokenData.Stamp)
		{
			BuildSourceTokenBand();
		}
	}

}
