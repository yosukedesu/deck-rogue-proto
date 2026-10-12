using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Core;
using DeckRogue.GodotPort.Presentation;
using DeckRogue.Platform;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.UI;

[ScriptPath("res://scripts/ui/GameUI.cs")]
public class GameUI : Control
{
	private sealed record EnemyHud(int Index, EnemyDropTarget Hit, PanelContainer Intent, PanelContainer Ledger);

	public new class MethodName : Control.MethodName
	{
		public static readonly StringName AdvancePlay = "AdvancePlay";

		public static readonly StringName SelectionTitle = "SelectionTitle";

		public static readonly StringName BeginEndTurn = "BeginEndTurn";

		public static readonly StringName FontSize = "FontSize";

		public new static readonly StringName _Ready = "_Ready";

		public static readonly StringName Bind = "Bind";

		public static readonly StringName PresentationCompleted = "PresentationCompleted";

		public new static readonly StringName _ExitTree = "_ExitTree";

		public static readonly StringName SelectEnemy = "SelectEnemy";

		public new static readonly StringName _UnhandledKeyInput = "_UnhandledKeyInput";

		public static readonly StringName Rebuild = "Rebuild";

		public static readonly StringName BuildHeader = "BuildHeader";

		public static readonly StringName Heading = "Heading";

		public static readonly StringName CloseOverlay = "CloseOverlay";

		public static readonly StringName CloseAllOverlays = "CloseAllOverlays";

		public static readonly StringName BuildOverlay = "BuildOverlay";

		public static readonly StringName OpenMenu = "OpenMenu";

		public static readonly StringName OpenKeywords = "OpenKeywords";

		public static readonly StringName OpenJournal = "OpenJournal";

		public static readonly StringName OpenExport = "OpenExport";

		public static readonly StringName OpenRetainedExport = "OpenRetainedExport";

		public static readonly StringName OpenExportPair = "OpenExportPair";

		public static readonly StringName CopyText = "CopyText";

		public static readonly StringName ExportFile = "ExportFile";

		public static readonly StringName ImportFile = "ImportFile";

		public static readonly StringName OpenImport = "OpenImport";

		public static readonly StringName ImportText = "ImportText";

		public new static readonly StringName _Notification = "_Notification";

		public static readonly StringName HandleBack = "HandleBack";

		public static readonly StringName ApplySafeArea = "ApplySafeArea";

		public static readonly StringName ReadSettings = "ReadSettings";

		public static readonly StringName ApplyAudio = "ApplyAudio";

		public static readonly StringName SaveSettings = "SaveSettings";

		public static readonly StringName OpenSettings = "OpenSettings";

		public static readonly StringName ApplyTheme = "ApplyTheme";

		public static readonly StringName Box = "Box";

		public static readonly StringName Plain = "Plain";

		public static readonly StringName SizeInteractiveControls = "SizeInteractiveControls";

		public static readonly StringName Row = "Row";

		public static readonly StringName Column = "Column";

		public static readonly StringName ScrollColumn = "ScrollColumn";

		public static readonly StringName Flow = "Flow";

		public static readonly StringName Art = "Art";

		public static readonly StringName Picture = "Picture";

		public static readonly StringName Tile = "Tile";

		public static readonly StringName CardColor = "CardColor";

		public static readonly StringName EnemyVitalsText = "EnemyVitalsText";

		public static readonly StringName EnemySurface = "EnemySurface";

		public new static readonly StringName _Process = "_Process";

		public static readonly StringName PositionEnemyHud = "PositionEnemyHud";

		public static readonly StringName OpenNote = "OpenNote";

		public static readonly StringName OpenRating = "OpenRating";

		public static readonly StringName Paints = "Paints";

		public static readonly StringName VisibleAlpha = "VisibleAlpha";

		public static readonly StringName ViewportBounds = "ViewportBounds";

		public new static readonly StringName Set = "Set";

		public static readonly StringName CanDropHandCard = "CanDropHandCard";

		public static readonly StringName DropHandCard = "DropHandCard";

		public static readonly StringName PlayAllSparks = "PlayAllSparks";

		public static readonly StringName PumpSparks = "PumpSparks";

		public static readonly StringName ContinueFromTitle = "ContinueFromTitle";

		public static readonly StringName Sound = "Sound";

		public static readonly StringName OfferQueuedRating = "OfferQueuedRating";

		public static readonly StringName InspectEnemy = "InspectEnemy";

		public static readonly StringName DisableUnderlyingFocus = "DisableUnderlyingFocus";

		public static readonly StringName FirstFocusable = "FirstFocusable";

		public static readonly StringName ShowTitle = "ShowTitle";

		public static readonly StringName SharePair = "SharePair";

		public static readonly StringName ShareRequestFinished = "ShareRequestFinished";

		public static readonly StringName BuildTitle = "BuildTitle";

		public static readonly StringName InvokeAction = "InvokeAction";
	}

	public new class PropertyName : Control.PropertyName
	{
		public static readonly StringName GraphicsQuality = "GraphicsQuality";

		public static readonly StringName PhoneLayout = "PhoneLayout";

		public static readonly StringName MinimumHit = "MinimumHit";

		public static readonly StringName IsInputBlocked = "IsInputBlocked";

		public static readonly StringName CurrentRoute = "CurrentRoute";

		public static readonly StringName ActiveModal = "ActiveModal";

		public static readonly StringName RejectedReceiptCount = "RejectedReceiptCount";

		public static readonly StringName PendingSparkPlays = "PendingSparkPlays";

		public static readonly StringName PresentedNumericContactCount = "PresentedNumericContactCount";

		public static readonly StringName _session = "_session";

		public static readonly StringName _body = "_body";

		public static readonly StringName _shell = "_shell";

		public static readonly StringName _overlayNode = "_overlayNode";

		public static readonly StringName _overlayTitle = "_overlayTitle";

		public static readonly StringName _overlayCanClose = "_overlayCanClose";

		public static readonly StringName _showTitle = "_showTitle";

		public static readonly StringName _notice = "_notice";

		public static readonly StringName _error = "_error";

		public static readonly StringName _leaderId = "_leaderId";

		public static readonly StringName _deckId = "_deckId";

		public static readonly StringName _seed = "_seed";

		public static readonly StringName _difficulty = "_difficulty";

		public static readonly StringName _preferredTarget = "_preferredTarget";

		public static readonly StringName _workshopA = "_workshopA";

		public static readonly StringName _workshopB = "_workshopB";

		public static readonly StringName _textScale = "_textScale";

		public static readonly StringName _masterVolume = "_masterVolume";

		public static readonly StringName _bgmVolume = "_bgmVolume";

		public static readonly StringName _sfxVolume = "_sfxVolume";

		public static readonly StringName _muted = "_muted";

		public static readonly StringName _graphicsQuality = "_graphicsQuality";

		public static readonly StringName _safeMargin = "_safeMargin";

		public static readonly StringName _handScroll = "_handScroll";

		public static readonly StringName _noteDraft = "_noteDraft";

		public static readonly StringName _hpReadout = "_hpReadout";

		public static readonly StringName _energyReadout = "_energyReadout";

		public static readonly StringName _blockReadout = "_blockReadout";

		public static readonly StringName _statusReadout = "_statusReadout";

		public static readonly StringName _costPresented = "_costPresented";

		public static readonly StringName _sparksRemaining = "_sparksRemaining";

		public static readonly StringName _pumpingSparks = "_pumpingSparks";

		public static readonly StringName _ratingQueued = "_ratingQueued";

		public static readonly StringName _shareAwaiting = "_shareAwaiting";

		public static readonly StringName _reactionMode = "_reactionMode";

		public static readonly StringName _setAnyCards = "_setAnyCards";
	}

	public new class SignalName : Control.SignalName
	{
	}

	private readonly HashSet<string> _scryPicks = new HashSet<string>();

	private GameSession? _session;

	private RunState? _view;

	private Func<bool>? _presentationGate;

	private VBoxContainer _body = null;

	private VBoxContainer _shell = null;

	private Control? _overlayNode;

	private Action<VBoxContainer>? _overlay;

	private readonly Stack<(string Title, Action<VBoxContainer> Body, bool Close)> _overlayStack = new Stack<(string, Action<VBoxContainer>, bool)>();

	private string _overlayTitle = "";

	private bool _overlayCanClose = true;

	private bool _showTitle = true;

	private string _notice = "";

	private bool _error;

	private string _leaderId = "leader_green";

	private string? _deckId;

	private int _seed = 9101;

	private int _difficulty = 3;

	private int _preferredTarget = -1;

	private PlaySelection? _play;

	private int _workshopA = -1;

	private int _workshopB = -1;

	private readonly HashSet<int> _relicPicks = new HashSet<int>();

	private float _textScale = 1f;

	private float _masterVolume = 0.8f;

	private float _bgmVolume = 0.3f;

	private float _sfxVolume = 0.7f;

	private bool _muted;

	private string _graphicsQuality = "auto";

	private MarginContainer? _safeMargin;

	private readonly Dictionary<string, Texture2D?> _textures = new Dictionary<string, Texture2D>();

	private static readonly Color Ink = SourcePalette.Paper;

	private static readonly Color Muted = SourcePalette.PaperDim;

	private static readonly Color Gold = SourcePalette.BrassLight;

	private static readonly Color Panel = SourcePalette.Window;

	private static readonly Color Border = SourcePalette.NightEdge;

	private static readonly Color Accent = SourcePalette.ManaLight;

	private static readonly Color Danger = new Color("f2a193");

	private Func<int, (Vector2 Head, Vector2 Feet)?>? _enemyProjection;

	private ScrollContainer? _handScroll;

	private readonly List<EnemyHud> _enemyHud = new List<EnemyHud>();

	private string _noteDraft = "";

	private Label? _hpReadout;

	private Label? _energyReadout;

	private Label? _blockReadout;

	private Label? _statusReadout;

	private readonly Dictionary<int, Label> _enemyReadouts = new Dictionary<int, Label>();

	private SessionChange? _activeChange;

	private bool _costPresented;

	private Func<GameEvent, CombatPresentationReceipt, GameState?, bool>? _receiptValidator;

	private Func<GameEvent, CombatPresentationVitals?>? _presentedVitals;

	private Func<GameState?>? _presentedState;

	private readonly Dictionary<int, Label> _enemyIntentReadouts = new Dictionary<int, Label>();

	private Func<Vector2, int?>? _enemyResolver;

	private int _sparksRemaining;

	private bool _pumpingSparks;

	private bool _ratingQueued;

	private bool _shareAwaiting;

	private string _reactionMode = "set-confirm";

	private bool _setAnyCards;

	public string GraphicsQuality => _graphicsQuality;

	private bool PhoneLayout => OS.HasFeature("mobile") || OS.GetCmdlineUserArgs().Contains("--ui-phone");

	public UiSizing Sizing => new UiSizing(PhoneLayout, (base.Size.Y > 0f) ? base.Size.Y : 900f, DisplayServer.WindowGetSize().Y, DisplayServer.ScreenGetDpi());

	private float MinimumHit => Sizing.MinimumHit;

	public bool IsInputBlocked => _presentationGate?.Invoke() ?? false;

	public string CurrentRoute => _showTitle ? "title" : FlowRules.Route(_view);

	private static bool ClipboardAvailable => DisplayServer.HasFeature(DisplayServer.Feature.Clipboard);

	private static bool NativeFilesAvailable => DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogFile);

	public IReadOnlyList<Control> EnemyHudSurfaces => _enemyHud.SelectMany((EnemyHud h) => new Control[2] { h.Intent, h.Ledger }).ToArray();

	public GameState? DisplayedCombat => _view?.Combat;

	public string? ActiveModal => (_overlay != null) ? _overlayTitle : null;

	public int RejectedReceiptCount { get; private set; }

	public IReadOnlyDictionary<int, string> DisplayedIntents => _enemyIntentReadouts.Where<KeyValuePair<int, Label>>((KeyValuePair<int, Label> p) => GodotObject.IsInstanceValid(p.Value)).ToDictionary((KeyValuePair<int, Label> p) => p.Key, (KeyValuePair<int, Label> p) => p.Value.Text);

	public int PendingSparkPlays => _sparksRemaining;

	public int PresentedNumericContactCount { get; private set; }

	public event Action<string>? QualityChanged;

	public event Action<string>? ScreenChanged;

	public event Action<string>? UiSoundRequested;

	private void BuildBattle(RunState run)
	{
		GameState state = run.Combat;
		if (state == null)
		{
			return;
		}
		PlayerState player = state.Player;
		_enemyReadouts.Clear();
		_enemyIntentReadouts.Clear();
		_enemyHud.Clear();
		_handScroll = null;
		VBoxContainer phoneSelf = ((PhoneLayout && _enemyProjection != null) ? Column(_body) : null);
		if (phoneSelf != null)
		{
			phoneSelf.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			phoneSelf.CustomMinimumSize = new Vector2(base.Size.X * 0.43f, 0f);
			phoneSelf.AddThemeConstantOverride("separation", 2);
		}
		HBoxContainer status = Row(phoneSelf ?? _body);
		HBoxContainer statusActions = ((phoneSelf != null) ? Row(phoneSelf) : status);
		Text(status, $"ターン {state.Turn}", 18, Gold);
		_blockReadout = Text(status, $"ブロック {player.Block}  氷壁 {player.IceBlock}", 18);
		_statusReadout = Text(status, PlayerStatus(state), 15, Muted);
		Button(statusActions, "状態・能力", delegate
		{
			Open("状態・能力", delegate(VBoxContainer box)
			{
				Text(box, PlayerStatus(state), 23);
				Text(box, "置物・人形", 22, Gold);
				CardGrid(box, player.Permanents, InspectCard, null, (CardInstance p) => Effects.IsDoll(p) ? ("残り " + (Effects.DollLifeLeft(state, p)?.ToString() ?? "∞") + " ターン") : "常在");
			});
		});
		Button(statusActions, $"からくり {player.SetCards.Count}/{player.SetSlots}", delegate
		{
			Open("仕込んでいるからくり", delegate(VBoxContainer box)
			{
				CardGrid(box, player.SetCards, InspectCard, null, (CardInstance card2) => Effects.TrapStatusTextKarakuri(state, card2));
			});
		});
		Button(statusActions, $"ギア {run.Gears?.Count ?? 0}", delegate
		{
			Open("ギア · " + Gears.ManaLabel(run.Mana.GetValueOrDefault()), delegate(VBoxContainer box)
			{
				GearInventory(box, run, usable: true);
			});
		});
		Button forecast = Button(statusActions, IsInputBlocked ? "被ダメ予測 · 解決中" : BattleForecast.Short(state), delegate
		{
			Open("被ダメ予測", delegate(VBoxContainer box)
			{
				VBoxContainer parent = ScrollColumn(box);
				foreach (string current in BattleForecast.Lines(state))
				{
					Text(parent, current, 23);
				}
			});
		});
		forecast.Name = "IncomingForecast";
		_blockReadout.MaxLinesVisible = 1;
		_statusReadout.MaxLinesVisible = 1;
		_statusReadout.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		float enemyPanelHeight = (float)Math.Ceiling((double)(FontSize(20) + FontSize(18) + (PhoneLayout ? FontSize(17) : (2 * FontSize(17) + FontSize(14)))) * 1.55 + 12.0);
		BattlefieldDropTarget stageSpace = new BattlefieldDropTarget
		{
			Name = "BattlefieldDrop",
			SizeFlagsVertical = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0f, enemyPanelHeight),
			MouseFilter = MouseFilterEnum.Pass,
			CanPlay = CanDropHandCard,
			CardDropped = DropHandCard
		};
		_body.AddChild(stageSpace, forceReadableName: false, InternalMode.Disabled);
		if (_enemyProjection != null)
		{
			BuildProjectedEnemyHud(stageSpace, state);
		}
		else
		{
			HBoxContainer enemyOuter = Row(stageSpace);
			enemyOuter.AnchorRight = 1f;
			enemyOuter.OffsetBottom = enemyPanelHeight;
			enemyOuter.AddChild(new Control
			{
				CustomMinimumSize = new Vector2(base.Size.X * 0.26f, 0f),
				MouseFilter = MouseFilterEnum.Ignore
			}, forceReadableName: false, InternalMode.Disabled);
			HBoxContainer enemyRow = Row(enemyOuter);
			enemyRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			enemyRow.Alignment = BoxContainer.AlignmentMode.Center;
			for (int i = 0; i < state.Enemies.Count; i++)
			{
				int index = i;
				EnemyState enemy = state.Enemies[i];
				EnemyDef def = Content.GetEnemyDef(enemy.EnemyId);
				EnemyDropTarget target = new EnemyDropTarget
				{
					Text = "",
					Disabled = (enemy.Hp <= 0 || IsInputBlocked),
					IsBlocked = () => IsInputBlocked,
					CustomMinimumSize = new Vector2((state.Enemies.Count > 3) ? 230 : 270, enemyPanelHeight),
					SizeFlagsHorizontal = SizeFlags.Fill,
					TooltipText = Plain(CardText.EnemyTraits(def, state, i))
				};
				enemyRow.AddChild(target, forceReadableName: false, InternalMode.Disabled);
				Color color = ((index == _preferredTarget) ? Gold : Border);
				target.AddThemeStyleboxOverride("normal", Box(new Color("1a1c33cc"), color, (index != _preferredTarget) ? 1 : 3));
				MarginContainer margins = new MarginContainer
				{
					MouseFilter = MouseFilterEnum.Ignore
				};
				target.AddChild(margins, forceReadableName: false, InternalMode.Disabled);
				margins.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
				margins.AddThemeConstantOverride("margin_left", 10);
				margins.AddThemeConstantOverride("margin_right", 10);
				VBoxContainer col = Column(margins);
				col.MouseFilter = MouseFilterEnum.Ignore;
				col.AddThemeConstantOverride("separation", 3);
				Label name = Text(col, $"{i + 1}. {def.Name}" + ((enemy.Hp <= 0) ? "  撃破" : ""), 20, (index == _preferredTarget) ? Gold : Ink);
				name.MaxLinesVisible = 1;
				name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
				_enemyReadouts[i] = Text(col, $"HP {enemy.Hp}/{enemy.MaxHp}  盾 {enemy.Block}", 18, Accent);
				_enemyIntentReadouts[i] = Text(col, (state.HideIntents == true) ? "意図は隠されています" : CardText.IntentText(state, i), 17, Danger);
				_enemyReadouts[i].MaxLinesVisible = 1;
				_enemyIntentReadouts[i].MaxLinesVisible = (PhoneLayout ? 1 : 2);
				_enemyIntentReadouts[i].TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
				if (!PhoneLayout)
				{
					Label statusLine = Text(col, EnemyStatus(enemy), 14, Muted);
					statusLine.MaxLinesVisible = 1;
					statusLine.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
				}
				target.TooltipText = Plain(def.Name + "\n" + CardText.IntentText(state, i) + "\n" + CardText.EnemyTraits(def, state, i) + "\n" + EnemyStatus(enemy));
				target.Inspect = delegate
				{
					InspectEnemy(index);
				};
				target.Pressed += delegate
				{
					if (!target.ConsumeInspection())
					{
						Sound("ui.click");
						SelectEnemy(index);
					}
				};
				target.CardDropped = delegate(string uid)
				{
					CardInstance cardInstance = state.Player.Hand.FirstOrDefault((CardInstance c) => c.Uid == uid);
					if (!(cardInstance == null))
					{
						_preferredTarget = index;
						BeginPlay(cardInstance, index);
					}
				};
			}
		}
		if (PhoneLayout)
		{
			HBoxContainer handHeading = Row(_body);
			Text(handHeading, "手札 · タップで使用 / 敵へドラッグ", 17, Muted);
			if (_preferredTarget >= 0 && _preferredTarget < state.Enemies.Count)
			{
				Text(handHeading, "狙い: " + Content.GetEnemyDef(state.Enemies[_preferredTarget].EnemyId).Name, 17, Gold);
			}
		}
		HBoxContainer handArea = ((!PhoneLayout && _enemyProjection != null) ? Row(_body) : null);
		VBoxContainer left = ((handArea != null) ? Column(handArea) : null);
		if (left != null)
		{
			left.CustomMinimumSize = new Vector2(210f, 0f);
			left.SizeFlagsHorizontal = SizeFlags.Fill;
		}
		ScrollContainer handScroll = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(0f, 290f),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
			VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		((Node)(((object)handArea) ?? ((object)_body))).AddChild((Node)handScroll, forceReadableName: false, InternalMode.Disabled);
		_handScroll = handScroll;
		VBoxContainer right = ((handArea != null) ? Column(handArea) : null);
		if (right != null)
		{
			right.CustomMinimumSize = new Vector2(220f, 0f);
			right.SizeFlagsHorizontal = SizeFlags.Fill;
		}
		HBoxContainer hand = Row(handScroll);
		hand.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hand.Alignment = BoxContainer.AlignmentMode.Center;
		for (int i2 = 0; i2 < player.Hand.Count; i2++)
		{
			CardInstance card = player.Hand[i2];
			bool playable = FlowRules.PlayBlocked(state, card) == null;
			bool settable = state.ReactionMode != "hold-manual" && SetBase.CanSetCard(state, card.Uid);
			CardTile handCard = Card(hand, card, delegate
			{
				CardActions(card);
			}, enabled: true, $"{i2 + 1}  " + (player.ImpulseUids.Contains(card.Uid) ? "衝動 · " : "") + ((card.Def.Type == "reaction") ? "仕込み札" : (playable ? "使う" : FlowRules.PlayBlocked(state, card))), playable, Effects.EffectiveCost(state, card));
			if (player.ImpulseUids.Contains(card.Uid))
			{
				handCard.TooltipText = "衝動: このターン限りの手札\n" + handCard.TooltipText;
			}
			handCard.SecondaryAction = delegate
			{
				if (settable && !IsInputBlocked)
				{
					Do(new Command_SetCard
					{
						CardUid = card.Uid
					});
				}
			};
		}
		HBoxContainer bottom = ((handArea == null) ? Row(_body) : null);
		Node pileControls = (Node)(((object)left) ?? ((object)bottom));
		Node turnControls = (Node)(((object)right) ?? ((object)bottom));
		_energyReadout = Text(pileControls, $"エナジー {player.Energy}/{player.EnergyMax}", 23, Accent);
		Button(pileControls, $"山札 {player.DrawPile.Count}", delegate
		{
			OpenDeck(player.DrawPile.OrderBy<CardInstance, string>((CardInstance c) => c.Def.Name, StringComparer.Ordinal).ToArray(), "山札 (名前順・順番は非公開)");
		});
		Button(pileControls, $"捨て札 {player.DiscardPile.Count}", delegate
		{
			OpenDeck(player.DiscardPile, "捨て札");
		});
		Button(pileControls, $"消滅 {player.ExhaustPile.Count}", delegate
		{
			OpenExhaust(state);
		});
		Button(turnControls, "戦闘ログ", delegate
		{
			Open("戦闘ログ", delegate(VBoxContainer box)
			{
				VBoxContainer parent = ScrollColumn(box);
				foreach (GameEvent current in state.EventLog)
				{
					string text = CardText.LogLine(current);
					if (!string.IsNullOrEmpty(text))
					{
						Text(parent, text, 18, CardText.IsPositiveLog(current) ? Accent : Ink);
					}
				}
			});
		});
		if (bottom != null)
		{
			Control spacer = new Control
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				MouseFilter = MouseFilterEnum.Ignore
			};
			bottom.AddChild(spacer, forceReadableName: false, InternalMode.Disabled);
		}
		int sparks = player.Hand.Count((CardInstance c) => c.Def.SparkToken == true);
		if (sparks >= 2)
		{
			Button(turnControls, $"火種を全部撃つ ({sparks})", PlayAllSparks, state.Phase == "player-turn" && state.PendingScry == null, Gold);
		}
		Button(turnControls, "ターンを終了 [E]", BeginEndTurn, state.Phase == "player-turn" && state.PendingScry == null, Gold);
		if (_overlay == null && !IsInputBlocked)
		{
			if (state.PendingScry != null)
			{
				PrepareScry(state);
			}
			else if (state.Phase == "awaiting-reaction")
			{
				PrepareReaction(state);
			}
		}
	}

	private static string PlayerStatus(GameState state)
	{
		PlayerState p = state.Player;
		List<string> values = new List<string>();
		Add("成長", p.Growth);
		Add("勢い", p.Momentum);
		Add("霊気", p.Aether);
		if (Effects.LightMatters(state))
		{
			values.Add("灯 " + p.Light.GetValueOrDefault());
		}
		Add("詠唱数", p.CardsPlayedThisTurn);
		Add("チェインの当たり", p.CascadesThisCombat.GetValueOrDefault());
		if (Effects.IsBlazing(state))
		{
			values.Add("猛り火");
		}
		Add("次の札軽減", p.NextCardDiscount);
		Add("反復", p.SpellEchoes);
		Add("弱体", p.Weak);
		Add("脆弱", p.Vulnerable);
		Add("虚弱", p.Frail);
		Add("拘束", p.Restrain);
		Add("霞み", p.Mist.GetValueOrDefault());
		Add("重り", p.Slow.GetValueOrDefault());
		Add("アーティファクト", p.Artifact.GetValueOrDefault());
		return (values.Count > 0) ? string.Join("  /  ", values) : "状態異常なし";
		void Add(string name, int value)
		{
			if (value > 0)
			{
				values.Add(name + " " + value);
			}
		}
	}

	private static string EnemyStatus(EnemyState enemy)
	{
		List<string> values = new List<string>();
		if (enemy.Strength != 0)
		{
			values.Add("筋力 " + enemy.Strength);
		}
		if (enemy.Burn > 0)
		{
			values.Add("延焼 " + enemy.Burn);
		}
		if (enemy.Confusion > 0)
		{
			values.Add("混乱 " + enemy.Confusion);
		}
		if (enemy.Exposed > 0)
		{
			values.Add("急所 " + enemy.Exposed);
		}
		if (enemy.Weak.GetValueOrDefault() > 0)
		{
			values.Add("威圧 " + enemy.Weak);
		}
		if (enemy.Artifact.GetValueOrDefault() > 0)
		{
			values.Add("防壁 " + enemy.Artifact);
		}
		return string.Join("  ", values);
	}

	private void CardActions(CardInstance card)
	{
		GameState state = _view?.Combat;
		if (state == null || IsInputBlocked || state.PendingScry != null)
		{
			return;
		}
		bool canSet = state.ReactionMode != "hold-manual" && SetBase.CanSetCard(state, card.Uid);
		string blocked = FlowRules.PlayBlocked(state, card);
		if (!canSet && blocked == null)
		{
			BeginPlay(card);
			return;
		}
		Open(card.Def.Name, delegate(VBoxContainer box)
		{
			Text(box, CardText.Body(card.Def), 23);
			Text(box, CardText.Notes(card.Def), 20, Gold);
			if (blocked == null)
			{
				Button(box, "プレイする", delegate
				{
					BeginPlay(card);
				}, enabled: true, Accent);
			}
			else
			{
				Text(box, blocked, 20, Muted);
			}
			if (canSet)
			{
				Button(box, "仕込む", delegate
				{
					Do(new Command_SetCard
					{
						CardUid = card.Uid
					});
				}, enabled: true, Gold);
			}
			Button(box, "詳しく見る", delegate
			{
				InspectCard(card);
			});
		});
	}

	private void BeginPlay(CardInstance card, int? target = null)
	{
		GameState state = _view?.Combat;
		if (state == null || IsInputBlocked)
		{
			return;
		}
		string blocked = FlowRules.PlayBlocked(state, card);
		if (blocked != null)
		{
			_notice = blocked;
			_error = true;
			Rebuild();
			return;
		}
		_overlayStack.Clear();
		_overlay = null;
		_play = new PlaySelection(state, card);
		int aim = target ?? _preferredTarget;
		if (aim >= 0 && aim < state.Enemies.Count && state.Enemies[aim].Hp > 0)
		{
			_play.TargetIndex = FlowRules.GuardRedirect(state, aim);
		}
		AdvancePlay();
	}

	private void AdvancePlay()
	{
		GameState state = _view?.Combat;
		if (_play == null || state == null || IsInputBlocked)
		{
			return;
		}
		string need = _play.Next(state);
		if (need == null)
		{
			Command_PlayCard command = _play.Command();
			_play = null;
			_overlay = null;
			Do(command);
			return;
		}
		PlaySelection play = _play;
		Open(play.Card.Def.Name + " · " + SelectionTitle(need), delegate(VBoxContainer box)
		{
			Text(box, CardText.Body(play.Card.Def), 20, Muted);
			if (need == "kindle")
			{
				Text(box, "衝動の札をくべると「くべたら」の効果も乗ります。払わずに素の効果だけでプレイすることもできます。", 22);
				Button(box, "くべずにプレイ", delegate
				{
					play.Kindle = false;
					AdvancePlay();
				});
				Button(box, $"衝動の札{play.Card.Def.ImpulseCost}枚をくべる", delegate
				{
					play.Kindle = true;
					AdvancePlay();
				}, FlowRules.CanKindle(state, play.Card), Gold);
			}
			else if (need == "mode")
			{
				for (int num = 0; num < play.Card.Def.Modes.Count; num++)
				{
					int index = num;
					CardMode cardMode = play.Card.Def.Modes[num];
					Button(box, cardMode.Name, delegate
					{
						play.ModeIndex = index;
						AdvancePlay();
					});
				}
			}
			else if (need == "x")
			{
				SpinBox amount = new SpinBox
				{
					MinValue = 1.0,
					MaxValue = Math.Max(1, state.Player.Energy),
					Value = state.Player.Energy,
					Step = 1.0,
					CustomMinimumSize = new Vector2(220f, 52f)
				};
				box.AddChild(amount, forceReadableName: false, InternalMode.Disabled);
				Button(box, "このエナジー量で使う", delegate
				{
					play.XAmount = (int)amount.Value;
					AdvancePlay();
				}, state.Player.Energy > 0);
			}
			else if (need == "target")
			{
				BuildTargetPicker(box, state, delegate(int requested)
				{
					play.TargetIndex = FlowRules.GuardRedirect(state, requested);
					AdvancePlay();
				});
			}
			else
			{
				IReadOnlyList<CardInstance> readOnlyList = play.Pool(state, need);
				if (readOnlyList.Count == 0)
				{
					Text(box, "選べる対象がありません。戻ると使用を取り消します。", 22, Danger);
				}
				else
				{
					CardGrid(box, readOnlyList, delegate(CardInstance c)
					{
						play.Pick(need, c.Uid);
						AdvancePlay();
					}, null, (CardInstance c) => "選ぶ");
				}
			}
			Button(box, "使用を取り消す", CloseAllOverlays);
		}, close: true, replace: true);
	}

	private static string SelectionTitle(string need)
	{
		if (1 == 0)
		{
		}
		string result = need switch
		{
			"kindle" => "くべるか選ぶ", 
			"impulse" => "追加コスト: 衝動の札をくべる", 
			"mode" => "効果を選ぶ", 
			"x" => "支払うエナジーを選ぶ", 
			"discard" => "追加コスト: 手札を捨てる", 
			"exhaust" => "追加コスト: 手札を消滅させる", 
			"retrieve" => "消滅置き場から選ぶ", 
			"deck" => "札を選ぶ", 
			"hand" => "鍛える手札を選ぶ", 
			"permanent" => "人形を選ぶ", 
			"target" => "対象の敵を選ぶ", 
			_ => need, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private void BuildTargetPicker(Node parent, GameState state, Action<int> choose, Func<int, string?>? advisory = null)
	{
		for (int i = 0; i < state.Enemies.Count; i++)
		{
			int index = i;
			EnemyState enemy = state.Enemies[i];
			if (enemy.Hp > 0)
			{
				Button(parent, $"{i + 1}. {Content.GetEnemyDef(enemy.EnemyId).Name}  HP {enemy.Hp}/{enemy.MaxHp}", delegate
				{
					choose(index);
				});
				string warning = advisory?.Invoke(index);
				if (warning != null)
				{
					Text(parent, warning, 18, Gold);
				}
			}
		}
	}

	private void PrepareScry(GameState state)
	{
		CardInstance[] shown = state.Player.DrawPile.Take(Math.Min(state.PendingScry.Count, state.Player.DrawPile.Count)).ToArray();
		_scryPicks.RemoveWhere((string uid) => !shown.Any((CardInstance c) => c.Uid == uid));
		_overlayTitle = "占術 · 捨てる札を選ぶ (左が次に引く札)";
		_overlayCanClose = false;
		_overlay = delegate(VBoxContainer box)
		{
			Text(box, "選んだ札は捨て札へ。残す札の順番は変わりません。", 20, Muted);
			CardGrid(box, shown, delegate(CardInstance c)
			{
				if (!_scryPicks.Add(c.Uid))
				{
					_scryPicks.Remove(c.Uid);
				}
				Rebuild();
			}, null, (CardInstance c) => _scryPicks.Contains(c.Uid) ? "捨てる (選択中)" : "残す");
			Button(box, (_scryPicks.Count == 0) ? "全部残して決める" : $"{_scryPicks.Count}枚を捨てて決める", delegate
			{
				string[] discardUids = _scryPicks.ToArray();
				_scryPicks.Clear();
				Do(new Command_ResolveScry
				{
					DiscardUids = discardUids
				});
			}, enabled: true, Gold);
		};
	}

	private void PrepareReaction(GameState state)
	{
		ReactionWindow window = Effects.WindowFromPending(state);
		if (window == null)
		{
			return;
		}
		bool manual = state.ReactionMode == "hold-manual";
		IReadOnlyList<CardInstance> usable = (manual ? HoldManualSystem.PlayableReactions(state) : Effects.UsableSetCards(state, window));
		HashSet<string> available = usable.Select((CardInstance c) => c.Uid).ToHashSet();
		CardInstance[] all = (manual ? state.Player.Hand.Where((CardInstance c) => c.Def.Type == "reaction").ToArray() : state.Player.SetCards.ToArray());
		CardInstance[] candidates = usable.Concat(all.Where((CardInstance c) => !available.Contains(c.Uid))).ToArray();
		int enemy = state.PendingWindow?.EnemyIndex ?? 0;
		_overlayTitle = "からくりの確認 · " + ((state.PendingWindow?.Stage == "post") ? "行動の後" : "行動の前");
		_overlayCanClose = false;
		_overlay = delegate(VBoxContainer box)
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
			CardInstance[] array = candidates;
			foreach (CardInstance card in array)
			{
				bool flag = available.Contains(card.Uid);
				PanelContainer panelContainer = new PanelContainer();
				vBoxContainer.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
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
						CustomMinimumSize = new Vector2(66f, 74f),
						MouseFilter = MouseFilterEnum.Ignore
					}, forceReadableName: false, InternalMode.Disabled);
				}
				VBoxContainer parent = Column(hBoxContainer);
				Text(parent, card.Def.Name + (manual ? "" : (" · " + Effects.TrapStatusTextKarakuri(state, card))), 21, Gold);
				string[] hideTriggers = ((!(window.Stage == "post")) ? new string[2] { "onEnemyAction", "onAttackIncoming" } : new string[3] { "onAttacked", "onEnemyBuffed", "onEnemyDefended" });
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
				Button(vBoxContainer2, "発動: " + card.Def.Name, delegate
				{
					Do(command);
				}, flag, Gold);
				Button(vBoxContainer2, "詳しく見る", delegate
				{
					InspectCard(card);
				});
			}
			Button(box, "温存して続ける", delegate
			{
				Do(new Command_ConfirmReaction
				{
					Fire = false
				});
			}, enabled: true, Gold);
		};
	}

	private void BeginEndTurn()
	{
		GameState state = _view?.Combat;
		if (state == null || IsInputBlocked || state.Phase != "player-turn" || state.PendingScry != null)
		{
			return;
		}
		int retainMax = Combat.RetainHandMax(state);
		HashSet<string> keep = new HashSet<string>();
		string keepImpulse = null;
		IReadOnlyList<CardInstance> impulse = FlowRules.KeepImpulsePool(state);
		if (impulse.Count == 0)
		{
			Retain();
			return;
		}
		Open("燠の守り · 残す衝動の札を選ぶ", delegate(VBoxContainer box)
		{
			Text(box, "1枚を失効させず、次のターンまで手札に残します。", 22);
			CardGrid(box, impulse, delegate(CardInstance c)
			{
				keepImpulse = c.Uid;
				Retain();
			}, null, (CardInstance c) => "この衝動の札を残す");
			Button(box, "自動で選ぶ（最も高コストの札）", delegate
			{
				keepImpulse = null;
				Retain();
			}, enabled: true, Gold);
		});
		void Finish(int? sparks)
		{
			Do(new Command_EndTurn
			{
				HearthSparks = sparks,
				RetainUids = ((keep.Count > 0) ? keep.ToArray() : null),
				KeepUid = keepImpulse
			});
		}
		void Hearth()
		{
			int max = Effects.HearthSparkMax(state);
			if (max <= 0)
			{
				Finish(null);
			}
			else
			{
				Open("灯の火床 · 火種の枚数を選ぶ", delegate(VBoxContainer box)
				{
					Text(box, $"灯を使って山札へ火種を加える。0〜{max}枚。", 22);
					SpinBox amount = new SpinBox
					{
						MinValue = 0.0,
						MaxValue = max,
						Step = 1.0,
						Value = 0.0,
						CustomMinimumSize = new Vector2(220f, 52f)
					};
					box.AddChild(amount, forceReadableName: false, InternalMode.Disabled);
					Button(box, "この枚数でターン終了", delegate
					{
						Finish((int)amount.Value);
					}, enabled: true, Gold);
				});
			}
		}
		void Retain()
		{
			if (retainMax <= 0)
			{
				Hearth();
			}
			else
			{
				Open("残す手札を選ぶ", delegate(VBoxContainer box)
				{
					Text(box, $"{retainMax}枚まで残せます。現在 {keep.Count} 枚。", 21);
					CardGrid(box, state.Player.Hand.Where((CardInstance c) => !c.Def.Id.StartsWith("status_", StringComparison.Ordinal)).ToArray(), delegate(CardInstance c)
					{
						if (!keep.Remove(c.Uid) && keep.Count < retainMax)
						{
							keep.Add(c.Uid);
						}
						Rebuild();
					}, (CardInstance c) => keep.Contains(c.Uid) || keep.Count < retainMax, (CardInstance c) => keep.Contains(c.Uid) ? "残す (選択中)" : "選ぶ");
					Button(box, "これでターンを終える", Hearth, enabled: true, Gold);
				});
			}
		}
	}

	private void OpenExhaust(GameState state)
	{
		Open("消滅置き場 · 亡骸", delegate(VBoxContainer box)
		{
			CardGrid(box, state.Player.ExhaustPile, delegate(CardInstance card)
			{
				if (!card.Def.NecroCost.HasValue)
				{
					InspectCard(card);
				}
				else
				{
					Open(card.Def.Name + " · 亡骸", delegate(VBoxContainer b)
					{
						Text(b, CardText.Body(card.Def), 23);
						Text(b, $"エナジー {card.Def.NecroCost} で消滅置き場からプレイ", 22, Gold);
						bool enabled = state.Phase == "player-turn" && state.PendingScry == null && state.Player.Energy >= card.Def.NecroCost;
						Button(b, "亡骸をプレイ", delegate
						{
							if (Effects.CardNeedsTarget(card) && state.Enemies.Count((EnemyState e) => e.Hp > 0) > 1)
							{
								Open("亡骸 · 対象を選ぶ", delegate(VBoxContainer row)
								{
									BuildTargetPicker(row, state, delegate(int i)
									{
										Play(FlowRules.GuardRedirect(state, i));
									});
								});
							}
							else
							{
								Play(null);
							}
						}, enabled);
					});
				}
				void Play(int? target)
				{
					Do(new Command_PlayNecro
					{
						CardUid = card.Uid,
						TargetIndex = target
					});
				}
			}, null, (CardInstance card) => card.Def.NecroCost.HasValue ? $"亡骸 {card.Def.NecroCost} E" : "詳しく見る");
		});
	}

	private void GearInventory(Node parent, RunState run, bool usable)
	{
		VBoxContainer scroll = ScrollColumn(parent);
		HFlowContainer flow = Flow(scroll);
		for (int i = 0; i < (run.Gears?.Count ?? 0); i++)
		{
			int index = i;
			GearInstance gear = run.Gears[i];
			GearDef def = Content.GetGearDef(gear.GearId);
			VBoxContainer col = Tile(flow, def.Name + $" ×{gear.Charges}", def.Text, "gears", def.Id);
			col.GetParent<Control>().CustomMinimumSize = new Vector2(260f, 170f);
			string blocked = Gears.GearBlockedReason(run.Combat, run.Mana.GetValueOrDefault(), gear);
			if (usable && run.Combat != null)
			{
				string advisory = Gears.GearNoEffectReason(run.Combat, def);
				if (advisory != null)
				{
					Text(col, advisory, 18, Gold);
				}
			}
			if (usable)
			{
				Button(col, blocked ?? "組む (魔素1個)", delegate
				{
					BeginGear(run, index, def);
				}, blocked == null, Accent);
			}
			Button(col, "捨てる", delegate
			{
				Confirm("ギアを手放す？", def.Name + "を持ち物から取り除きます。", delegate
				{
					Do(new RunCommand_DiscardGear
					{
						Index = index
					});
				});
			});
		}
		IReadOnlyList<GearInstance>? gears = run.Gears;
		if (gears == null || gears.Count == 0)
		{
			Text(scroll, "ギアを持っていません。", 21, Muted);
		}
	}

	private void BeginGear(RunState run, int index, GearDef original)
	{
		GameState state = run.Combat;
		if (state == null || IsInputBlocked)
		{
			return;
		}
		if (original.Special == "nameless")
		{
			Open("無銘の部品 · ギアの効果を選ぶ", delegate(VBoxContainer box)
			{
				VBoxContainer parent = ScrollColumn(box);
				foreach (string id in run.SeenGearIds ?? Array.Empty<string>())
				{
					GearDef def = Content.GetGearDef(id);
					if (!(def.Special == "nameless"))
					{
						Button(parent, def.Name + " · " + def.Text, delegate
						{
							SelectCard(def, id);
						});
						string text = Gears.GearNoEffectReason(state, def);
						if (text != null)
						{
							Text(parent, text, 18, Gold);
						}
					}
				}
			});
		}
		else
		{
			SelectCard(original, null);
		}
		void SelectCard(GearDef def, string? asId)
		{
			if (def.NeedsCard != null)
			{
				Open("ギア · 札を選ぶ", delegate(VBoxContainer box)
				{
					CardGrid(box, Gears.GearCardChoices(state, def), delegate(CardInstance card)
					{
						SelectTarget(def, card.Uid, asId);
					}, null);
				});
			}
			else
			{
				SelectTarget(def, null, asId);
			}
		}
		void SelectTarget(GearDef def, string? card, string? asId)
		{
			if (def.NeedsTarget == true && state.Enemies.Count((EnemyState e) => e.Hp > 0) > 1)
			{
				Open("ギア · 対象の敵を選ぶ", delegate(VBoxContainer box)
				{
					BuildTargetPicker(box, state, delegate(int target)
					{
						Submit(def, card, target, asId);
					}, (int target) => Gears.GearNoEffectReason(state, def, target));
				});
			}
			else
			{
				Submit(def, card, null, asId);
			}
		}
		void Submit(GearDef def, string? card, int? target, string? asId)
		{
			Do(new RunCommand_UseGear
			{
				Index = index,
				CardUid = card,
				TargetIndex = target,
				AsGearId = asId
			});
		}
	}

	private int FontSize(int requested)
	{
		return Sizing.Font(requested, _textScale);
	}

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		base.MouseFilter = MouseFilterEnum.Ignore;
		GetTree().QuitOnGoBack = false;
		AndroidReportShare.ShareRequestFinished += ShareRequestFinished;
		base.Resized += ApplySafeArea;
		ReadSettings();
		ApplyTheme();
		if (_session != null)
		{
			Rebuild();
		}
	}

	public void Bind(GameSession session)
	{
		if (_session != null)
		{
			_session.StateChanged -= Changed;
		}
		_session = session;
		_session.StateChanged += Changed;
		_session.SetReportText(new ReportText
		{
			LogLine = CardText.LogLine,
			IntentText = CardText.IntentText
		});
		_view = session.CurrentRun;
		_showTitle = _view == null;
		if (IsInsideTree())
		{
			Rebuild();
		}
	}

	public void SetPresentationGate(Func<bool> gate)
	{
		_presentationGate = gate;
	}

	public void PresentationCompleted()
	{
		if (_session != null && !IsInputBlocked)
		{
			_view = _session.CurrentRun;
			PumpSparks();
			if (!IsInputBlocked)
			{
				Rebuild();
			}
		}
	}

	public override void _ExitTree()
	{
		AndroidReportShare.ShareRequestFinished -= ShareRequestFinished;
		if (_session != null)
		{
			_session.StateChanged -= Changed;
		}
	}

	private void Changed(SessionChange change)
	{
		_activeChange = change;
		_costPresented = false;
		if (change.IsReset)
		{
			_ratingQueued = false;
		}
		else if (change.Previous?.Phase == "combat" && SourceUiPolicies.RatingPhase(change.Current.Phase))
		{
			_ratingQueued = true;
		}
		_showTitle = false;
		_overlay = null;
		_overlayStack.Clear();
		_play = null;
		_relicPicks.Clear();
		_notice = "";
		if (change.IsReset)
		{
			_scryPicks.Clear();
			_preferredTarget = -1;
			_workshopA = (_workshopB = -1);
			_sparksRemaining = 0;
			_leaderId = change.Current.LeaderId;
			_seed = change.Current.Seed;
			if (change.Current.Difficulty > 0)
			{
				_difficulty = change.Current.Difficulty;
			}
		}
		if (!IsInputBlocked)
		{
			_view = change.Current;
		}
		Rebuild();
	}

	public void SelectEnemy(int index)
	{
		if (!IsInputBlocked && !(_view?.Combat == null) && index >= 0 && index < _view.Combat.Enemies.Count && _view.Combat.Enemies[index].Hp > 0)
		{
			_preferredTarget = index;
			if (_play != null)
			{
				_play.TargetIndex = FlowRules.GuardRedirect(_view.Combat, index);
				AdvancePlay();
			}
			else
			{
				Rebuild();
			}
		}
	}

	public override void _UnhandledKeyInput(InputEvent evt)
	{
		if (!(evt is InputEventKey { Pressed: not false, Echo: false } key) || IsInputBlocked)
		{
			return;
		}
		Control control = GetViewport().GuiGetFocusOwner();
		if ((control is LineEdit || control is TextEdit || control is SpinBox) ? true : false)
		{
			return;
		}
		if (key.Keycode == Key.Escape)
		{
			HandleBack();
			GetViewport().SetInputAsHandled();
		}
		else if (_overlay == null && !(CurrentRoute != "combat") && !(_view?.Combat == null))
		{
			if (key.Keycode == Key.E)
			{
				BeginEndTurn();
				GetViewport().SetInputAsHandled();
			}
			if (key.Keycode == Key.G)
			{
				PlayAllSparks();
				GetViewport().SetInputAsHandled();
			}
			int ix = (int)key.Keycode - 49;
			if (ix >= 0 && ix < Math.Min(9, _view.Combat.Player.Hand.Count))
			{
				CardActions(_view.Combat.Player.Hand[ix]);
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void Result(SessionResult result)
	{
		if (!result.Success || !string.IsNullOrEmpty(result.Warning))
		{
			_notice = result.Error ?? result.Warning ?? "操作できませんでした";
			_error = !result.Success;
			Rebuild();
		}
	}

	private void Do(RunCommand command)
	{
		if (!IsInputBlocked && _session != null)
		{
			Sound(SourceUiPolicies.CommandSound(command));
			Result(_session.Dispatch(command));
		}
	}

	private void Do(Command command)
	{
		if (!IsInputBlocked && _session != null)
		{
			Result(_session.Dispatch(command));
		}
	}

	private void Rebuild()
	{
		if (!IsInsideTree() || _session == null)
		{
			return;
		}
		foreach (Node child in GetChildren())
		{
			if (child is Control c)
			{
				c.Hide();
			}
			child.QueueFree();
		}
		_overlayNode = null;
		_enemyHud.Clear();
		_handScroll = null;
		bool combat = CurrentRoute == "combat";
		if (!combat)
		{
			ColorRect bg = new ColorRect
			{
				Color = new Color("0f1120df"),
				MouseFilter = MouseFilterEnum.Ignore
			};
			AddChild(bg, forceReadableName: false, InternalMode.Disabled);
			bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		}
		MarginContainer margin = new MarginContainer
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		AddChild(margin, forceReadableName: false, InternalMode.Disabled);
		margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		_safeMargin = margin;
		ApplySafeArea();
		_shell = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		_shell.AddThemeConstantOverride("separation", 10);
		margin.AddChild(_shell, forceReadableName: false, InternalMode.Disabled);
		BuildHeader();
		if (_notice.Length > 0)
		{
			Text(_shell, _notice, 17, _error ? Danger : Accent);
		}
		_body = new VBoxContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore
		};
		_body.AddThemeConstantOverride("separation", (combat && PhoneLayout) ? 8 : 12);
		_shell.AddChild(_body, forceReadableName: false, InternalMode.Disabled);
		if (_showTitle || _view == null)
		{
			BuildTitle();
		}
		else
		{
			switch (_view.Phase)
			{
			case "departure":
				BuildDeparture(_view);
				break;
			case "map":
				BuildMap(_body, _view, interactive: true);
				break;
			case "combat":
				BuildBattle(_view);
				break;
			case "reward":
				BuildReward(_view);
				break;
			case "relic-reward":
				BuildRelicReward(_view);
				break;
			case "relic-choose":
				BuildRelicChoose(_view);
				break;
			case "shop":
				BuildShop(_view);
				break;
			case "campfire":
				BuildCampfire(_view);
				break;
			case "workshop":
				BuildWorkshop(_view);
				break;
			case "event":
				BuildEvent(_view);
				break;
			case "won":
			case "lost":
				BuildEnd(_view);
				break;
			default:
				Text(_body, "未対応の画面: " + _view.Phase, 24, Danger);
				break;
			}
		}
		if (_overlay != null)
		{
			BuildOverlay();
		}
		if (IsInputBlocked)
		{
			ColorRect busy = new ColorRect
			{
				Color = new Color(0f, 0f, 0f, 0.01f),
				MouseFilter = MouseFilterEnum.Stop
			};
			AddChild(busy, forceReadableName: false, InternalMode.Disabled);
			busy.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
			Label label = Text(busy, "解決中…", 18, Gold);
			label.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight, LayoutPresetMode.Minsize);
			label.Position -= new Vector2(160f, 48f);
			label.Size = new Vector2(140f, 30f);
		}
		SizeInteractiveControls(_shell);
		if (_overlayNode != null)
		{
			SizeInteractiveControls(_overlayNode);
			DisableUnderlyingFocus(_shell);
			FirstFocusable(_overlayNode)?.CallDeferred(Control.MethodName.GrabFocus);
		}
		this.ScreenChanged?.Invoke(CurrentRoute);
		OfferQueuedRating();
	}

	private void BuildHeader()
	{
		PanelContainer panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", Box(SourcePalette.Night, Border));
		_shell.AddChild(panel, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer row = Row(panel);
		row.CustomMinimumSize = new Vector2(0f, 56f);
		Text(row, "DECK ROGUE", 23, Gold);
		Control spacer = new Control
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore
		};
		row.AddChild(spacer, forceReadableName: false, InternalMode.Disabled);
		if (!_showTitle && _view != null)
		{
			RunState run = _view;
			Text(row, $"幕 {run.Act}  ·  {FlowRules.PhaseName(run.Phase)}");
			int hp = ((run.Phase == "combat" && run.Combat != null) ? run.Combat.Player.Hp : run.Hp);
			_hpReadout = Text(row, $"HP {hp}/{run.MaxHp}", 21, (hp <= run.MaxHp / 3) ? Danger : Accent);
			Text(row, $"{run.Gold} G", 21, Gold);
			Button(row, $"デッキ {run.Deck.Count}", delegate
			{
				OpenDeck(run.Deck, "デッキ");
			});
			Button(row, $"遺物 {run.Relics.Count}", delegate
			{
				OpenRelics(run);
			});
		}
		Button(row, "メニュー", OpenMenu);
	}

	private void Heading(string title, string subtitle = "")
	{
		Label heading = Text(_body, title, 30, Gold);
		if (ResourceLoader.Exists("res://assets/Fonts/KaiseiDecol-Bold.ttf"))
		{
			heading.AddThemeFontOverride("font", GD.Load<Font>("res://assets/Fonts/KaiseiDecol-Bold.ttf"));
		}
		if (subtitle.Length > 0)
		{
			Text(_body, subtitle, 18, Muted);
		}
	}

	private void Open(string title, Action<VBoxContainer> body, bool close = true, bool replace = false)
	{
		if (!replace && _overlay != null)
		{
			_overlayStack.Push((_overlayTitle, _overlay, _overlayCanClose));
		}
		_overlayTitle = title;
		_overlay = body;
		_overlayCanClose = close;
		Rebuild();
	}

	private void CloseOverlay()
	{
		if (_overlayStack.Count > 0)
		{
			(_overlayTitle, _overlay, _overlayCanClose) = _overlayStack.Pop();
		}
		else
		{
			_overlay = null;
			_play = null;
		}
		Rebuild();
	}

	private void CloseAllOverlays()
	{
		_overlayStack.Clear();
		_overlay = null;
		_play = null;
		Rebuild();
	}

	private void BuildOverlay()
	{
		ColorRect scrim = new ColorRect
		{
			Color = new Color(0.015f, 0.025f, 0.05f, 0.89f),
			MouseFilter = MouseFilterEnum.Stop
		};
		AddChild(scrim, forceReadableName: false, InternalMode.Disabled);
		scrim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		_overlayNode = scrim;
		MarginContainer margin = new MarginContainer();
		scrim.AddChild(margin, forceReadableName: false, InternalMode.Disabled);
		margin.AnchorLeft = 0.05f;
		margin.AnchorRight = 0.95f;
		margin.AnchorTop = 0.06f;
		margin.AnchorBottom = 0.94f;
		PanelContainer pan = new PanelContainer();
		pan.AddThemeStyleboxOverride("panel", Box(Panel, Gold));
		margin.AddChild(pan, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer col = new VBoxContainer();
		col.AddThemeConstantOverride("separation", 12);
		pan.AddChild(col, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer top = Row(col);
		Text(top, _overlayTitle, 25, Gold).SizeFlagsHorizontal = SizeFlags.ExpandFill;
		if (_overlayCanClose)
		{
			Button(top, "閉じる / 戻る", CloseOverlay);
		}
		if (_notice.Length > 0)
		{
			Text(col, _notice, 18, _error ? Danger : Gold);
		}
		_overlay?.Invoke(col);
	}

	private void OpenMenu()
	{
		Open("メニュー", delegate(VBoxContainer box)
		{
			HFlowContainer parent = Flow(box);
			Button(parent, "設定", OpenSettings);
			Button(parent, "用語集", OpenKeywords);
			Button(parent, "セーブを読み込む", OpenImport);
			if (_session?.CurrentRun != null)
			{
				Button(parent, "セーブを保存", delegate
				{
					SessionResult sessionResult = _session.Save();
					if (sessionResult.Success)
					{
						_notice = sessionResult.Warning ?? "保存しました";
						_error = false;
						CloseAllOverlays();
					}
					else
					{
						Result(sessionResult);
					}
				});
				Button(parent, "書き出す", OpenExport);
				Button(parent, "ランの記録", OpenJournal);
				Button(parent, "プレイメモ", OpenNote);
				if (_session.History.Count > 0)
				{
					Button(parent, "直前の戦闘を評価", OpenRating);
				}
				Button(parent, "地図", delegate
				{
					Open("坑道の地図", delegate(VBoxContainer b)
					{
						BuildMap(b, _session.CurrentRun, interactive: false);
					});
				});
				Button(parent, "タイトルへ", ShowTitle);
			}
			Text(box, "操作: 札をタップしてプレイ。敵を先に選ぶか、札を敵へドラッグ。1〜9: 手札 / G: 火種を全部撃つ / E: ターン終了 / Esc: 戻る。", 20, Muted);
		});
	}

	private void Confirm(string title, string description, Action action)
	{
		Open(title, delegate(VBoxContainer box)
		{
			Text(box, description, 21);
			Button(box, "確認して続ける", delegate
			{
				_overlay = null;
				action();
			}, enabled: true, Danger);
		});
	}

	private void OpenDeck(IReadOnlyList<CardInstance> deck, string title)
	{
		Open(title, delegate(VBoxContainer box)
		{
			CardGrid(box, deck, delegate(CardInstance c)
			{
				InspectCard(c);
			}, null);
		});
	}

	private void OpenRelics(RunState run)
	{
		Open("遺物", delegate(VBoxContainer box)
		{
			VBoxContainer parent = ScrollColumn(box);
			foreach (string current in run.Relics)
			{
				RelicDef relicDef = Content.GetRelicDef(current);
				GameUI gameUI = this;
				string name = relicDef.Name;
				int? num = Run.RelicChargesLeft(run, current);
				object obj;
				if (num.HasValue)
				{
					int valueOrDefault = num.GetValueOrDefault();
					obj = $" (残り{valueOrDefault}回)";
				}
				else
				{
					obj = "";
				}
				gameUI.Tile(parent, name + (string?)obj, relicDef.Description, "relics", current);
			}
		});
	}

	private void OpenKeywords()
	{
		Open("用語集", delegate(VBoxContainer box)
		{
			VBoxContainer parent = ScrollColumn(box);
			foreach (KeyValuePair<string, string> current in KeywordHelp.Terms)
			{
				Text(parent, current.Key, 21, Gold);
				Text(parent, current.Value, 18);
			}
		});
	}

	private void OpenJournal()
	{
		if (_session != null)
		{
			Open("ランの記録", delegate(VBoxContainer box)
			{
				TextEdit node = new TextEdit
				{
					Text = _session.ExportText(),
					Editable = false,
					SizeFlagsVertical = SizeFlags.ExpandFill,
					WrapMode = TextEdit.LineWrappingMode.Boundary
				};
				box.AddChild(node, forceReadableName: false, InternalMode.Disabled);
				Button(box, "レポートを書き出す", OpenExport);
			});
		}
	}

	private void OpenExport()
	{
		if (_session == null)
		{
			return;
		}
		if (_session.CurrentRun == null)
		{
			OpenRetainedExport();
			return;
		}
		try
		{
			OpenExportPair(_session.ExportSave(), _session.ExportText(), "セーブとレポートを書き出す");
		}
		catch (Exception ex)
		{
			_notice = "書き出せません: " + ex.Message;
			_error = true;
			Rebuild();
		}
	}

	private void OpenRetainedExport()
	{
		if (_session == null)
		{
			return;
		}
		try
		{
			SessionReport report = _session.ExportRetainedReport();
			OpenExportPair(report.SaveJson, report.Text, "保存したランのレポート · " + report.CapturedAt);
		}
		catch (Exception ex)
		{
			_notice = "レポートを読めません: " + ex.Message;
			_error = true;
			Rebuild();
		}
	}

	private void OpenExportPair(string save, string report, string title)
	{
		Open(title, delegate(VBoxContainer box)
		{
			Text(box, "セーブは途中から再開するためのデータ。レポートはランの記録です。", 20);
			HFlowContainer parent = Flow(box);
			Button(parent, "セーブをコピー", delegate
			{
				CopyText(save);
			}, ClipboardAvailable);
			Button(parent, "レポートをコピー", delegate
			{
				CopyText(report);
			}, ClipboardAvailable);
			if (Encoding.UTF8.GetByteCount(save) > 524288)
			{
				Text(box, "大きなセーブです。端末の制限を避けるためファイル保存をおすすめします。", 18, Gold);
			}
			Button(parent, "セーブをファイルに保存", delegate
			{
				ExportFile(save, "deckrogue-save.json");
			});
			Button(parent, "レポートをファイルに保存", delegate
			{
				ExportFile(report, "deckrogue-report.md");
			});
			if (AndroidReportShare.IsAvailable)
			{
				Button(parent, "レポートとセーブを共有", delegate
				{
					SharePair(report, save);
				}, enabled: true, Gold);
			}
			TextEdit node = new TextEdit
			{
				Text = report,
				Editable = false,
				SizeFlagsVertical = SizeFlags.ExpandFill,
				WrapMode = TextEdit.LineWrappingMode.Boundary
			};
			box.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		});
	}

	private void CopyText(string text)
	{
		try
		{
			if (!ClipboardAvailable)
			{
				throw new InvalidOperationException("この端末ではコピーが使えません");
			}
			DisplayServer.ClipboardSet(text);
			bool verified = DisplayServer.ClipboardGet() == text;
			_notice = (verified ? "コピーしました" : "コピー内容を確認できません。ファイル保存を使ってください。");
			_error = !verified;
		}
		catch (Exception ex)
		{
			_notice = "コピーできません: " + ex.Message + "。ファイル保存を使ってください。";
			_error = true;
		}
		CloseOverlay();
	}

	private void ExportFile(string text, string name)
	{
		if (OS.HasFeature("mobile") && !NativeFilesAvailable)
		{
			_notice = "この端末ではファイル選択が使えません。コピーで書き出してください。";
			_error = true;
			Rebuild();
			return;
		}
		FileDialog dialog = new FileDialog
		{
			FileMode = FileDialog.FileModeEnum.SaveFile,
			Access = FileDialog.AccessEnum.Filesystem,
			UseNativeDialog = NativeFilesAvailable,
			CurrentFile = name,
			Title = "書き出し先を選ぶ"
		};
		dialog.Filters = ((!name.EndsWith(".json", StringComparison.Ordinal)) ? new string[1] { "*.md;Markdown report;text/markdown" } : new string[1] { "*.json;JSON save;application/json" });
		AddChild(dialog, forceReadableName: false, InternalMode.Disabled);
		dialog.FileSelected += delegate(string path)
		{
			try
			{
				using FileAccess fileAccess = FileAccess.Open(path, FileAccess.ModeFlags.Write);
				if (fileAccess == null)
				{
					_notice = "書き込めません: " + FileAccess.GetOpenError();
					_error = true;
				}
				else
				{
					fileAccess.StoreString(text);
					fileAccess.Flush();
					Error error = fileAccess.GetError();
					_notice = ((error == Error.Ok) ? ("保存しました: " + path) : ("書き込みに失敗しました: " + error));
					_error = error != Error.Ok;
				}
			}
			catch (Exception ex)
			{
				_notice = "書き込みに失敗しました: " + ex.Message;
				_error = true;
			}
			dialog.QueueFree();
			CloseAllOverlays();
		};
		dialog.Canceled += dialog.QueueFree;
		dialog.PopupCenteredRatio(0.85f);
	}

	private void ImportFile()
	{
		if (OS.HasFeature("mobile") && !NativeFilesAvailable)
		{
			_notice = "この端末ではファイル選択が使えません。貼り付けで読み込んでください。";
			_error = true;
			Rebuild();
			return;
		}
		FileDialog dialog = new FileDialog
		{
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Access = FileDialog.AccessEnum.Filesystem,
			UseNativeDialog = NativeFilesAvailable,
			Title = "セーブを選ぶ",
			Filters = new string[1] { "*.json;JSON save;application/json" }
		};
		AddChild(dialog, forceReadableName: false, InternalMode.Disabled);
		dialog.FileSelected += delegate(string path)
		{
			using FileAccess fileAccess = FileAccess.Open(path, FileAccess.ModeFlags.Read);
			dialog.QueueFree();
			if (fileAccess == null)
			{
				_notice = "ファイルを読めません: " + FileAccess.GetOpenError();
				_error = true;
				Rebuild();
			}
			else
			{
				string asText = fileAccess.GetAsText();
				Error error = fileAccess.GetError();
				if (error != Error.Ok && error != Error.FileEof)
				{
					_notice = "ファイルを読めません: " + error;
					_error = true;
					Rebuild();
				}
				else
				{
					ImportText(asText);
				}
			}
		};
		dialog.Canceled += dialog.QueueFree;
		dialog.PopupCenteredRatio(0.85f);
	}

	private void OpenImport()
	{
		Open("セーブを読み込む", delegate(VBoxContainer box)
		{
			Text(box, "セーブデータを貼り付けるか、ファイルを選んでください。現在のランがある場合は置き換えます。", 20);
			TextEdit edit = new TextEdit
			{
				PlaceholderText = "セーブデータをここに貼り付け",
				SizeFlagsVertical = SizeFlags.ExpandFill,
				WrapMode = TextEdit.LineWrappingMode.Boundary
			};
			box.AddChild(edit, forceReadableName: false, InternalMode.Disabled);
			HBoxContainer parent = Row(box);
			Button(parent, "貼り付け", delegate
			{
				try
				{
					edit.Text = DisplayServer.ClipboardGet();
				}
				catch (Exception ex)
				{
					_notice = "貼り付けできません: " + ex.Message;
					_error = true;
					Rebuild();
				}
			}, ClipboardAvailable);
			Button(parent, "読み込む", delegate
			{
				ImportText(edit.Text);
			});
			Button(parent, "ファイルを選ぶ", ImportFile);
		});
	}

	private void ImportText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			_notice = "セーブデータを入力してください";
			_error = true;
			Rebuild();
			return;
		}
		Action load = delegate
		{
			_showTitle = false;
			if (_session != null)
			{
				Result(_session.Load(text));
			}
		};
		string warning = "";
		try
		{
			string fingerprint = (string?)JObject.Parse(text)["fingerprint"];
			if (!string.IsNullOrEmpty(fingerprint) && fingerprint != _session?.Catalog.Fingerprint)
			{
				warning = "別のデータ版のセーブです。カードや敵の定義が変わっていると、結果が変わる可能性があります。";
			}
		}
		catch (Exception)
		{
		}
		if (!(_session?.CurrentRun != null))
		{
			GameSession? session = _session;
			if ((session == null || !session.HasSave) && warning.Length <= 0)
			{
				load();
				return;
			}
		}
		Confirm((warning.Length > 0) ? "別のデータ版を読み込む？" : "現在のランを置き換える？", warning + "読み込むセーブの状態から再開します。現在のランがある場合は置き換えます。", load);
	}

	public override void _Notification(int what)
	{
		if ((long)what == 1007 && IsInsideTree())
		{
			HandleBack();
		}
	}

	private void HandleBack()
	{
		if (!IsInputBlocked)
		{
			if (_overlay != null && _overlayCanClose)
			{
				CloseOverlay();
			}
			else
			{
				OpenMenu();
			}
		}
	}

	private void ApplySafeArea()
	{
		if (_safeMargin == null || !GodotObject.IsInstanceValid(_safeMargin))
		{
			return;
		}
		int edge = (int)Math.Ceiling(Sizing.Edge);
		int left = edge;
		int right = edge;
		int top = edge;
		int bottom = edge;
		if (OS.HasFeature("mobile"))
		{
			Rect2I safe = DisplayServer.GetDisplaySafeArea();
			Vector2I screen = DisplayServer.ScreenGetSize();
			if (screen.X > 0 && screen.Y > 0 && safe.Size.X > 0)
			{
				float sx = base.Size.X / (float)screen.X;
				float sy = base.Size.Y / (float)screen.Y;
				left += (int)Math.Max(0f, (float)safe.Position.X * sx);
				top += (int)Math.Max(0f, (float)safe.Position.Y * sy);
				right += (int)Math.Max(0f, (float)(screen.X - safe.End.X) * sx);
				bottom += (int)Math.Max(0f, (float)(screen.Y - safe.End.Y) * sy);
			}
		}
		_safeMargin.AddThemeConstantOverride("margin_left", left);
		_safeMargin.AddThemeConstantOverride("margin_right", right);
		_safeMargin.AddThemeConstantOverride("margin_top", top);
		_safeMargin.AddThemeConstantOverride("margin_bottom", bottom);
	}

	private void ReadSettings()
	{
		ConfigFile config = new ConfigFile();
		config.Load("user://settings.cfg");
		_textScale = Math.Clamp((float)config.GetValue("ui", "text_scale", 1f), 1f, 1.3f);
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string arg in cmdlineUserArgs)
		{
			if (arg.StartsWith("--ui-text-scale=") && float.TryParse(arg.Substring(16), NumberStyles.Float, CultureInfo.InvariantCulture, out var scale))
			{
				_textScale = Math.Clamp(scale, 1f, 1.3f);
			}
		}
		_masterVolume = (float)config.GetValue("audio", "master", 0.8f);
		_bgmVolume = (float)config.GetValue("audio", "bgm", 0.3f);
		_sfxVolume = (float)config.GetValue("audio", "sfx", 0.7f);
		_muted = (bool)config.GetValue("audio", "muted", false);
		_graphicsQuality = (string)config.GetValue("graphics", "quality", "auto");
		if (!OS.HasFeature("mobile"))
		{
			if ((bool)config.GetValue("window", "fullscreen", false))
			{
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
			}
			else if (config.HasSectionKey("window", "width"))
			{
				DisplayServer.WindowSetSize(new Vector2I((int)config.GetValue("window", "width", 1280), (int)config.GetValue("window", "height", 720)));
			}
		}
		ApplyAudio();
	}

	private void ApplyAudio()
	{
		Bus("Master", _masterVolume, _muted);
		Bus("BGM", _bgmVolume, mute: false);
		Bus("SFX", _sfxVolume, mute: false);
		static void Bus(string name, float level, bool mute)
		{
			int index = AudioServer.GetBusIndex(name);
			if (index < 0)
			{
				index = AudioServer.BusCount;
				AudioServer.AddBus();
				AudioServer.SetBusName(index, name);
				AudioServer.SetBusSend(index, "Master");
			}
			AudioServer.SetBusMute(index, mute || level <= 0f);
			if (level > 0f)
			{
				AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(level));
			}
		}
	}

	private void SaveSettings()
	{
		ConfigFile config = new ConfigFile();
		config.Load("user://settings.cfg");
		config.SetValue("ui", "text_scale", _textScale);
		config.SetValue("audio", "master", _masterVolume);
		config.SetValue("audio", "bgm", _bgmVolume);
		config.SetValue("audio", "sfx", _sfxVolume);
		config.SetValue("audio", "muted", _muted);
		config.SetValue("graphics", "quality", _graphicsQuality);
		if (!OS.HasFeature("mobile"))
		{
			config.SetValue("window", "fullscreen", DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen);
			Vector2I size = DisplayServer.WindowGetSize();
			config.SetValue("window", "width", size.X);
			config.SetValue("window", "height", size.Y);
		}
		config.Save("user://settings.cfg");
	}

	private void OpenSettings()
	{
		Open("設定", delegate(VBoxContainer outer)
		{
			VBoxContainer box = ScrollColumn(outer);
			Volume("全体音量", _masterVolume, delegate(float value)
			{
				_masterVolume = value;
			});
			Volume("音楽", _bgmVolume, delegate(float value)
			{
				_bgmVolume = value;
			});
			Volume("効果音", _sfxVolume, delegate(float value)
			{
				_sfxVolume = value;
			});
			CheckButton checkButton = new CheckButton
			{
				Text = "ミュート",
				ButtonPressed = _muted
			};
			box.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
			checkButton.Toggled += delegate(bool value)
			{
				_muted = value;
				ApplyAudio();
				SaveSettings();
			};
			Text(box, "文字サイズ", 21, Gold);
			HBoxContainer parent = Row(box);
			float[] array = new float[3] { 1f, 1.15f, 1.3f };
			foreach (float num2 in array)
			{
				float size = num2;
				Button(parent, $"{Math.Round(num2 * 100f)}%", delegate
				{
					_textScale = size;
					SaveSettings();
					ApplyTheme();
					Rebuild();
				});
			}
			if (this.QualityChanged != null)
			{
				Text(box, "描画品質", 21, Gold);
				HBoxContainer parent2 = Row(box);
				(string, string)[] array2 = new(string, string)[4]
				{
					("auto", "自動"),
					("high", "高"),
					("standard", "標準"),
					("low", "低")
				};
				foreach ((string, string) tuple in array2)
				{
					(string, string) entry = tuple;
					Button(parent2, ((_graphicsQuality == entry.Item1) ? "● " : "○ ") + entry.Item2, delegate
					{
						_graphicsQuality = entry.Item1;
						this.QualityChanged?.Invoke(_graphicsQuality);
						SaveSettings();
						Rebuild();
					});
				}
			}
			if (!OS.HasFeature("mobile"))
			{
				Button(box, "全画面 / ウィンドウを切り替える", delegate
				{
					DisplayServer.WindowSetMode((DisplayServer.WindowMode)((DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen) ? 0 : 3));
					SaveSettings();
				});
				HBoxContainer parent3 = Row(box);
				Vector2I[] array3 = new Vector2I[3]
				{
					new Vector2I(1280, 720),
					new Vector2I(1600, 900),
					new Vector2I(1920, 1080)
				};
				for (int num4 = 0; num4 < array3.Length; num4++)
				{
					Vector2I vector2I = array3[num4];
					Vector2I resolution = vector2I;
					Button(parent3, $"{vector2I.X} × {vector2I.Y}", delegate
					{
						DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
						DisplayServer.WindowSetSize(resolution);
						SaveSettings();
					});
				}
			}
			Text(box, "変更はこの端末に保存されます。音量0は完全に消音します。", 18, Muted);
			void Volume(string name, float current, Action<float> setter)
			{
				HBoxContainer line = Row(box);
				Text(line, name, 21, Gold);
				HSlider volume = new HSlider
				{
					MinValue = 0.0,
					MaxValue = 1.0,
					Step = 0.05,
					Value = current,
					CustomMinimumSize = new Vector2(380f, 48f),
					SizeFlagsHorizontal = SizeFlags.ExpandFill
				};
				line.AddChild(volume, forceReadableName: false, InternalMode.Disabled);
				Label number = Text(line, Math.Round(current * 100f) + "%", 20);
				volume.ValueChanged += delegate(double value)
				{
					setter((float)value);
					number.Text = Math.Round(value * 100.0) + "%";
					ApplyAudio();
					SaveSettings();
				};
			}
		});
	}

	private void ApplyTheme()
	{
		Theme theme = new Theme
		{
			DefaultFontSize = FontSize(19)
		};
		if (ResourceLoader.Exists("res://assets/Fonts/NotoSansJP-Regular.otf"))
		{
			theme.DefaultFont = GD.Load<Font>("res://assets/Fonts/NotoSansJP-Regular.otf");
		}
		theme.SetColor("font_color", "Label", Ink);
		theme.SetColor("font_color", "Button", Ink);
		theme.SetColor("font_disabled_color", "Button", new Color("6d7a83"));
		theme.SetStylebox("normal", "Button", Box(SourcePalette.NightPill, Border));
		theme.SetStylebox("hover", "Button", Box(SourcePalette.Window.Lightened(0.15f), Accent));
		theme.SetStylebox("pressed", "Button", Box(SourcePalette.ManaInk, Gold));
		theme.SetStylebox("focus", "Button", Box(new Color(0f, 0f, 0f, 0f), Gold, 3));
		theme.SetStylebox("disabled", "Button", Box(new Color("14232c"), new Color("283c46")));
		theme.SetStylebox("panel", "PanelContainer", Box(Panel, Border));
		base.Theme = theme;
	}

	private static StyleBoxFlat Box(Color color, Color border, int width = 1)
	{
		return new StyleBoxFlat
		{
			BgColor = color,
			BorderColor = border,
			BorderWidthLeft = width,
			BorderWidthRight = width,
			BorderWidthTop = width,
			BorderWidthBottom = width,
			CornerRadiusTopLeft = 9,
			CornerRadiusTopRight = 9,
			CornerRadiusBottomLeft = 9,
			CornerRadiusBottomRight = 9,
			ContentMarginLeft = 13f,
			ContentMarginRight = 13f,
			ContentMarginTop = 9f,
			ContentMarginBottom = 9f
		};
	}

	private Label Text(Node parent, string text, int size = 19, Color? color = null)
	{
		Label label = new Label
		{
			Text = Plain(text),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		label.AddThemeFontSizeOverride("font_size", FontSize(size));
		label.AddThemeColorOverride("font_color", color ?? Ink);
		parent.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		return label;
	}

	private static string Plain(string text)
	{
		return Regex.Replace(text ?? "", "<[^>]+>", "");
	}

	private Button Button(Node parent, string text, Action action, bool enabled = true, Color? color = null)
	{
		Button button = new Button
		{
			Text = text,
			Disabled = (!enabled || IsInputBlocked),
			CustomMinimumSize = new Vector2(MinimumHit, MinimumHit),
			FocusMode = FocusModeEnum.All
		};
		if (color.HasValue)
		{
			button.AddThemeColorOverride("font_color", color.Value);
		}
		button.Pressed += delegate
		{
			if (!IsInputBlocked)
			{
				Sound("ui.click");
				action();
			}
		};
		parent.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		return button;
	}

	private void SizeInteractiveControls(Node node)
	{
		if (node is ScrollContainer scroll)
		{
			scroll.FollowFocus = true;
		}
		if (node is BaseButton button && !(button is CardTile) && !(button is EnemyDropTarget))
		{
			button.CustomMinimumSize = new Vector2(Math.Max(button.CustomMinimumSize.X, MinimumHit), Math.Max(button.CustomMinimumSize.Y, MinimumHit));
		}
		else if ((node is LineEdit || node is Slider || node is OptionButton) ? true : false)
		{
			Control control = (Control)node;
			control.CustomMinimumSize = new Vector2(control.CustomMinimumSize.X, Math.Max(control.CustomMinimumSize.Y, MinimumHit));
		}
		foreach (Node child in node.GetChildren())
		{
			SizeInteractiveControls(child);
		}
	}

	private static HBoxContainer Row(Node parent)
	{
		HBoxContainer row = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		row.AddThemeConstantOverride("separation", 12);
		parent.AddChild(row, forceReadableName: false, InternalMode.Disabled);
		return row;
	}

	private static VBoxContainer Column(Node parent)
	{
		VBoxContainer col = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore
		};
		col.AddThemeConstantOverride("separation", 10);
		parent.AddChild(col, forceReadableName: false, InternalMode.Disabled);
		return col;
	}

	private static VBoxContainer ScrollColumn(Node parent)
	{
		ScrollContainer scroll = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		parent.AddChild(scroll, forceReadableName: false, InternalMode.Disabled);
		return Column(scroll);
	}

	private static HFlowContainer Flow(Node parent)
	{
		HFlowContainer flow = new HFlowContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		flow.AddThemeConstantOverride("h_separation", 12);
		flow.AddThemeConstantOverride("v_separation", 12);
		parent.AddChild(flow, forceReadableName: false, InternalMode.Disabled);
		return flow;
	}

	private Texture2D? Art(string group, string id)
	{
		string path = $"res://assets/Art/{group}/{id}.png";
		if (!_textures.TryGetValue(path, out Texture2D texture))
		{
			texture = (ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null);
			if (texture == null && group == "leaders")
			{
				texture = ProceduralActorTexture.Generate(id, friendly: true, 64);
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

	private void Picture(Node parent, string group, string id, Vector2 size)
	{
		Texture2D texture = Art(group, id);
		if (texture != null)
		{
			TextureRect image = new TextureRect
			{
				TextureFilter = TextureFilterEnum.Nearest,
				Texture = texture,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				CustomMinimumSize = size,
				MouseFilter = MouseFilterEnum.Ignore
			};
			parent.AddChild(image, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private VBoxContainer Tile(Node parent, string name, string description, string? group = null, string? id = null)
	{
		PanelContainer pan = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		parent.AddChild(pan, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer col = Column(pan);
		if (group != null && id != null)
		{
			Picture(col, group, id, new Vector2(80f, 70f));
		}
		Text(col, name, 23, Gold);
		Text(col, description, 18);
		return col;
	}

	private static Color CardColor(string color)
	{
		return SourcePalette.Edge(color);
	}

	private void CardGrid(Node parent, IReadOnlyList<CardInstance> cards, Action<CardInstance>? onPick, Func<CardInstance, bool>? enabled, Func<CardInstance, string>? badge = null)
	{
		VBoxContainer scroll = ScrollColumn(parent);
		HFlowContainer flow = Flow(scroll);
		foreach (CardInstance card in cards)
		{
			Card(flow, card, (onPick == null) ? null : ((Action)delegate
			{
				onPick(card);
			}), enabled?.Invoke(card) ?? true, badge?.Invoke(card));
		}
	}

	private CardTile Card(Node parent, CardInstance card, Action? action, bool enabled = true, string? badge = null, bool drag = false, int? cost = null)
	{
		CardTile tile = new CardTile
		{
			CardUid = card.Uid,
			AllowDrag = (drag && enabled && !IsInputBlocked),
			Text = "",
			CustomMinimumSize = new Vector2(PhoneLayout ? 230 : 204, 282f),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			SizeFlagsVertical = SizeFlags.ShrinkBegin,
			Disabled = (!enabled || IsInputBlocked),
			TooltipText = Plain(card.Def.Name + "\n" + CardText.Body(card.Def) + "\n" + CardText.Notes(card.Def))
		};
		tile.AddThemeStyleboxOverride("normal", Box(SourcePalette.Paper, CardColor(card.Def.Color), 3));
		tile.AddThemeStyleboxOverride("hover", Box(SourcePalette.Paper3, SourcePalette.Brass, 3));
		tile.AddThemeStyleboxOverride("pressed", Box(SourcePalette.ManaLight, SourcePalette.Brass, 3));
		tile.AddThemeStyleboxOverride("disabled", Box(SourcePalette.PaperDim, CardColor(card.Def.Color).Darkened(0.25f), 2));
		parent.AddChild(tile, forceReadableName: false, InternalMode.Disabled);
		MarginContainer margin = new MarginContainer
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		tile.AddChild(margin, forceReadableName: false, InternalMode.Disabled);
		margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		string[] array = new string[4] { "left", "right", "top", "bottom" };
		foreach (string side in array)
		{
			margin.AddThemeConstantOverride("margin_" + side, 12);
		}
		VBoxContainer col = Column(margin);
		col.MouseFilter = MouseFilterEnum.Ignore;
		col.AddThemeConstantOverride("separation", 4);
		HBoxContainer cardTitle = Row(col);
		Picture(cardTitle, "ui", "cost_orb", new Vector2(26f, 26f));
		Label nameText = Text(cardTitle, (cost?.ToString() ?? CardText.CostLabel(card.Def)) + "  " + card.Def.Name, 19, SourcePalette.Ink);
		nameText.MaxLinesVisible = (PhoneLayout ? 1 : 2);
		nameText.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		Texture2D art = SourceArtwork.Card(card.Def.Id, card.Def.Type);
		if (art != null)
		{
			col.AddChild(new TextureRect
			{
				Texture = art,
				TextureFilter = TextureFilterEnum.Nearest,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				CustomMinimumSize = new Vector2(174f, PhoneLayout ? 48 : 66),
				MouseFilter = MouseFilterEnum.Ignore
			}, forceReadableName: false, InternalMode.Disabled);
		}
		HBoxContainer kind = Row(col);
		Picture(kind, "ui", "gem_" + (card.Def.Rarity ?? "common"), new Vector2(18f, 18f));
		Text(kind, CardText.TypeJa(card.Def.Type) + "  " + CardText.RarityLabel(card.Def.Rarity), 13, SourcePalette.InkSoft);
		RichTextLabel bodyText = new RichTextLabel
		{
			Text = Plain(CardText.Body(card.Def)),
			BbcodeEnabled = false,
			FitContent = false,
			ScrollActive = false,
			CustomMinimumSize = new Vector2(0f, 42f),
			SizeFlagsVertical = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore
		};
		bodyText.AddThemeFontSizeOverride("normal_font_size", FontSize(16));
		bodyText.AddThemeColorOverride("default_color", SourcePalette.Ink);
		col.AddChild(bodyText, forceReadableName: false, InternalMode.Disabled);
		Label noteText = Text(col, badge ?? CardText.Notes(card.Def), 13, SourcePalette.BrassInk);
		noteText.MaxLinesVisible = 1;
		noteText.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		tile.Inspect = delegate
		{
			if (!IsInputBlocked)
			{
				InspectCard(card);
			}
		};
		if (action != null)
		{
			tile.Pressed += delegate
			{
				if (!IsInputBlocked && !tile.ConsumeInspection())
				{
					Sound("ui.click");
					action();
				}
			};
		}
		return tile;
	}

	private void InspectCard(CardInstance card)
	{
		Open(card.Def.Name, delegate(VBoxContainer box)
		{
			VBoxContainer parent = ScrollColumn(box);
			HBoxContainer parent2 = Row(parent);
			Card(parent2, card, null);
			VBoxContainer parent3 = Column(parent2);
			Text(parent3, CardText.Body(card.Def), 24);
			Text(parent3, CardText.Notes(card.Def), 21, Gold);
			foreach (KeyValuePair<string, string> current in KeywordHelp.Terms.Where<KeyValuePair<string, string>>((KeyValuePair<string, string> k) => (CardText.Body(card.Def) + CardText.Notes(card.Def)).Contains(k.Key)))
			{
				Text(parent3, current.Key + ": " + current.Value, 18, Muted);
			}
			if (Upgrade.CanUpgradeCard(card))
			{
				Text(parent3, "鍛えた後", 23, Accent);
				Text(parent3, CardText.Body(Upgrade.UpgradeCard(card).Def), 21);
			}
		});
	}

	public void SetEnemyProjection(Func<int, (Vector2 Head, Vector2 Feet)?> project)
	{
		_enemyProjection = project;
	}

	private string EnemyVitalsText(int hp, int maxHp, int block)
	{
		return (_enemyProjection != null) ? $"{hp}/{maxHp}  盾{block}" : $"HP {hp}/{maxHp}  盾 {block}";
	}

	private PanelContainer EnemySurface(Node parent, int index, string kind, Color border)
	{
		PanelContainer panel = new PanelContainer
		{
			Name = ((kind == "intent") ? "EnemyIntentSurface_" : "EnemyLedgerSurface_") + index,
			MouseFilter = MouseFilterEnum.Ignore
		};
		StyleBoxFlat style = Box(new Color("1a1c33ed"), border, (index != _preferredTarget) ? 1 : 2);
		style.ContentMarginLeft = 8f;
		style.ContentMarginRight = 8f;
		style.ContentMarginTop = 2f;
		style.ContentMarginBottom = 2f;
		panel.AddThemeStyleboxOverride("panel", style);
		panel.SetMeta("enemy_hud_surface", kind);
		panel.SetMeta("enemy_index", index);
		parent.AddChild(panel, forceReadableName: false, InternalMode.Disabled);
		return panel;
	}

	private void BuildProjectedEnemyHud(Node parent, GameState state)
	{
		for (int i = 0; i < state.Enemies.Count; i++)
		{
			int index = i;
			EnemyState enemy = state.Enemies[i];
			EnemyDef def = Content.GetEnemyDef(enemy.EnemyId);
			EnemyDropTarget hit = new EnemyDropTarget
			{
				Name = "EnemyTarget_" + i,
				Text = "",
				Disabled = (enemy.Hp <= 0 || IsInputBlocked),
				IsBlocked = () => IsInputBlocked,
				CustomMinimumSize = new Vector2(MinimumHit, MinimumHit),
				TooltipText = Plain(def.Name + "\n" + CardText.IntentText(state, i) + "\n" + CardText.EnemyTraits(def, state, i) + "\n" + EnemyStatus(enemy))
			};
			string[] array = new string[5] { "normal", "hover", "pressed", "disabled", "focus" };
			foreach (string style in array)
			{
				hit.AddThemeStyleboxOverride(style, new StyleBoxEmpty());
			}
			parent.AddChild(hit, forceReadableName: false, InternalMode.Disabled);
			Color border = ((index == _preferredTarget) ? Gold : Border);
			PanelContainer intent = EnemySurface(hit, index, "intent", border);
			Label intentText = Text(intent, (state.HideIntents == true) ? "意図は隠されています" : CardText.IntentText(state, index), 17, Danger);
			intentText.MaxLinesVisible = 1;
			intentText.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			_enemyIntentReadouts[index] = intentText;
			PanelContainer ledger = EnemySurface(hit, index, "ledger", border);
			VBoxContainer col = Column(ledger);
			col.AddThemeConstantOverride("separation", 0);
			Label name = Text(col, $"{i + 1}. {def.Name}" + ((enemy.Hp <= 0) ? " 撃破" : ""), 20, (index == _preferredTarget) ? Gold : Ink);
			name.MaxLinesVisible = 1;
			name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			Label hp = Text(col, EnemyVitalsText(enemy.Hp, enemy.MaxHp, enemy.Block), 18, Accent);
			hp.MaxLinesVisible = 1;
			_enemyReadouts[index] = hp;
			hit.Inspect = delegate
			{
				InspectEnemy(index);
			};
			hit.Pressed += delegate
			{
				if (!hit.ConsumeInspection())
				{
					Sound("ui.click");
					SelectEnemy(index);
				}
			};
			hit.CardDropped = delegate(string uid)
			{
				CardInstance cardInstance = state.Player.Hand.FirstOrDefault((CardInstance c) => c.Uid == uid);
				if (cardInstance != null)
				{
					_preferredTarget = index;
					BeginPlay(cardInstance, index);
				}
			};
			_enemyHud.Add(new EnemyHud(index, hit, intent, ledger));
		}
	}

	public override void _Process(double delta)
	{
		PositionEnemyHud();
	}

	private void PositionEnemyHud()
	{
		if (_enemyProjection == null || _handScroll == null || !GodotObject.IsInstanceValid(_handScroll) || _enemyHud.Count == 0)
		{
			return;
		}
		(EnemyHud Hud, (Vector2 Head, Vector2 Feet)? Point)[] projected = (from enemyHud in _enemyHud
			select ((EnemyHud Hud, (Vector2 Head, Vector2 Feet)? Point))(Hud: enemyHud, Point: _enemyProjection(enemyHud.Index)) into tuple
			where tuple.Point.HasValue
			select tuple).ToArray();
		float ledgerBottom = _handScroll.GlobalPosition.Y - 2f;
		(EnemyHud, (Vector2, Vector2)?)[] array = projected;
		for (int num = 0; num < array.Length; num++)
		{
			(EnemyHud, (Vector2, Vector2)?) entry = array[num];
			EnemyHud h = entry.Item1;
			(Vector2 Head, Vector2 Feet) point = entry.Item2.Value;
			if (GodotObject.IsInstanceValid(h.Hit))
			{
				float gap = (from tuple in projected
					where tuple.Hud.Index != h.Index
					select Math.Abs(tuple.Point.Value.Feet.X - point.Feet.X)).DefaultIfEmpty(320f).Min();
				float width = Math.Clamp(gap - 10f, MinimumHit, PhoneLayout ? 290 : 300);
				float ledgerHeight = Math.Max(h.Ledger.GetCombinedMinimumSize().Y, Mathf.Ceil((float)(FontSize(20) + FontSize(18)) * 1.5f) + 4f);
				float intentHeight = Math.Max(h.Intent.GetCombinedMinimumSize().Y, Mathf.Ceil((float)FontSize(17) * 1.5f) + 4f);
				float x = Math.Clamp(point.Feet.X - width / 2f, 16f, Math.Max(16f, base.Size.X - width - 16f));
				float headerBottom = ((_shell.GetChildCount() > 0 && _shell.GetChild(0) is Control header) ? header.GetGlobalRect().End.Y : (_shell.GlobalPosition.Y + MinimumHit));
				float top = Math.Max(headerBottom + 6f, point.Head.Y - intentHeight - 8f);
				float ledgerY = ledgerBottom - ledgerHeight;
				h.Hit.GlobalPosition = new Vector2(x, top);
				h.Hit.Size = new Vector2(width, Math.Max(MinimumHit, ledgerBottom - top));
				h.Intent.Position = Vector2.Zero;
				h.Intent.Size = new Vector2(width, intentHeight);
				h.Ledger.GlobalPosition = new Vector2(x, ledgerY);
				h.Ledger.Size = new Vector2(width, ledgerHeight);
			}
		}
	}

	private void OpenNote()
	{
		if (_session == null)
		{
			return;
		}
		Open("プレイメモ", delegate(VBoxContainer box)
		{
			Text(box, "記録時の幕・部屋・ターンを自動で添え、セーブとレポートに保存します。", 19, Muted);
			TextEdit edit = new TextEdit
			{
				Text = _noteDraft,
				PlaceholderText = "気づいたことをここに",
				CustomMinimumSize = new Vector2(0f, 140f),
				SizeFlagsVertical = SizeFlags.ExpandFill,
				WrapMode = TextEdit.LineWrappingMode.Boundary
			};
			edit.TextChanged += delegate
			{
				_noteDraft = edit.Text;
			};
			box.AddChild(edit, forceReadableName: false, InternalMode.Disabled);
			Button(box, "メモを保存", delegate
			{
				SessionResult sessionResult = _session.AddNote(_noteDraft);
				if (sessionResult.Success)
				{
					_noteDraft = "";
					_notice = sessionResult.Warning ?? "メモを保存しました";
					_error = sessionResult.Warning != null;
					CloseOverlay();
				}
				else
				{
					Result(sessionResult);
				}
			}, enabled: true, Gold);
			VBoxContainer parent = ScrollColumn(box);
			foreach (PlayNote current in _session.Notes)
			{
				Text(parent, current.Context + " · " + current.At, 14, Muted);
				Text(parent, current.Text, 18);
			}
		});
	}

	private void OpenRating()
	{
		if (_session == null || _session.History.Count == 0)
		{
			return;
		}
		BattleArchive last = _session.History[_session.History.Count - 1];
		BattleRating draft = last.Rating?.Clone() ?? new BattleRating();
		bool lost = last.Result == "lost";
		Open("直前の戦闘を評価", delegate(VBoxContainer box)
		{
			Text(box, $"第{last.BattleNo}戦 · {last.Turns}ターン · HP {last.HpBefore} → {last.HpAfter}", 21, Gold);
			Score("敵の強さ (1=弱い / 5=強い)", draft.Strength, delegate(int value)
			{
				draft.Strength = value;
			});
			Score("面白さ (1=退屈 / 5=最高)", draft.Fun, delegate(int value)
			{
				draft.Fun = value;
			});
			if (lost)
			{
				HBoxContainer parent = Row(box);
				Text(parent, "敗北の感触", 21);
				Button(parent, ((draft.LossFeel == "build") ? "● " : "") + "構築の失敗", delegate
				{
					draft.LossFeel = "build";
					Rebuild();
				});
				Button(parent, ((draft.LossFeel == "unfair") ? "● " : "") + "理不尽", delegate
				{
					draft.LossFeel = "unfair";
					Rebuild();
				});
			}
			TextEdit note = new TextEdit
			{
				Text = (draft.Note ?? ""),
				PlaceholderText = "ひとことメモ (任意)",
				CustomMinimumSize = new Vector2(0f, 120f),
				SizeFlagsVertical = SizeFlags.ExpandFill,
				WrapMode = TextEdit.LineWrappingMode.Boundary
			};
			box.AddChild(note, forceReadableName: false, InternalMode.Disabled);
			note.TextChanged += delegate
			{
				draft.Note = note.Text;
			};
			Button(box, "評価を保存", delegate
			{
				SessionResult sessionResult = _session.RateLastBattle(draft);
				if (sessionResult.Success)
				{
					_notice = sessionResult.Warning ?? "評価を保存しました";
					_error = sessionResult.Warning != null;
					CloseOverlay();
				}
				else
				{
					Result(sessionResult);
				}
			}, draft.Strength.HasValue && draft.Fun.HasValue && (!lost || draft.LossFeel != null), Gold);
			Button(box, "スキップ", CloseOverlay);
			void Score(string title, int? current, Action<int> set)
			{
				HBoxContainer row = Row(box);
				Text(row, title, 21);
				for (int i = 1; i <= 5; i++)
				{
					int value = i;
					Button(row, ((current == value) ? "● " : "") + value, delegate
					{
						set(value);
						Rebuild();
					});
				}
			}
		});
	}

	public IReadOnlyList<Rect2> GetPaintedCombatBounds()
	{
		List<Rect2> result = new List<Rect2>();
		if (CurrentRoute != "combat" || _shell == null || !GodotObject.IsInstanceValid(_shell) || !_shell.IsVisibleInTree())
		{
			return result;
		}
		Visit(_shell);
		return result;
		void Visit(Node node)
		{
			if (node is Control control)
			{
				if (!control.IsVisibleInTree())
				{
					return;
				}
				if (1 == 0)
				{
				}
				bool flag = !(control is EnemyDropTarget) && !(control is BattlefieldDropTarget) && ((control is PanelContainer) ? Paints(control.GetThemeStylebox("panel")) : ((control is BaseButton button) ? Paints(control.GetThemeStylebox(button.Disabled ? "disabled" : (button.IsPressed() ? "pressed" : (button.IsHovered() ? "hover" : "normal")))) : ((control is Label label) ? (!string.IsNullOrEmpty(label.Text)) : (control is RichTextLabel text && !string.IsNullOrEmpty(text.Text)))));
				if (1 == 0)
				{
				}
				if (flag && control.SelfModulate.A > 0f && VisibleAlpha(control) > 0f)
				{
					Rect2 visible = ViewportBounds(control).Intersection(GetViewportRect());
					for (Node parent = control.GetParent(); parent != null; parent = parent.GetParent())
					{
						if (parent is Control clip && (clip.ClipContents || clip is ScrollContainer))
						{
							visible = visible.Intersection(ViewportBounds(clip));
						}
					}
					if (visible.Size.X > 0f && visible.Size.Y > 0f)
					{
						result.Add(visible);
					}
					return;
				}
			}
			foreach (Node child in node.GetChildren())
			{
				Visit(child);
			}
		}
	}

	private static bool Paints(StyleBox style)
	{
		if (1 == 0)
		{
		}
		bool result = !(style is StyleBoxEmpty) && (!(style is StyleBoxFlat flat) || flat.BgColor.A > 0f || (flat.BorderColor.A > 0f && (flat.BorderWidthLeft > 0 || flat.BorderWidthRight > 0 || flat.BorderWidthTop > 0 || flat.BorderWidthBottom > 0)));
		if (1 == 0)
		{
		}
		return result;
	}

	private static float VisibleAlpha(CanvasItem item)
	{
		float alpha = item.Modulate.A;
		for (Node parent = item.GetParent(); parent != null; parent = parent.GetParent())
		{
			if (parent is CanvasItem canvas)
			{
				alpha *= canvas.Modulate.A;
			}
		}
		return alpha;
	}

	private static Rect2 ViewportBounds(Control control)
	{
		Transform2D transform = control.GetGlobalTransformWithCanvas();
		Vector2 a = transform * Vector2.Zero;
		Vector2 b = transform * new Vector2(control.Size.X, 0f);
		Vector2 c = transform * control.Size;
		Vector2 d = transform * new Vector2(0f, control.Size.Y);
		Vector2 min = new Vector2(Math.Min(Math.Min(a.X, b.X), Math.Min(c.X, d.X)), Math.Min(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y)));
		Vector2 max = new Vector2(Math.Max(Math.Max(a.X, b.X), Math.Max(c.X, d.X)), Math.Max(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y)));
		return new Rect2(min, max - min);
	}

	public void SetReceiptValidator(Func<GameEvent, CombatPresentationReceipt, GameState?, bool> validator)
	{
		_receiptValidator = validator;
	}

	public void SetPresentationProjection(Func<GameEvent, CombatPresentationVitals?> vitals, Func<GameState?> state)
	{
		_presentedVitals = vitals;
		_presentedState = state;
	}

	public void PresentEvent(GameEvent evt)
	{
		GameState state = _view?.Combat;
		if ((object)state == null)
		{
			return;
		}
		PlayerState player = state.Player;
		CombatPresentationReceipt receipt = evt.GetType().GetProperty("PresentationReceipt")?.GetValue(evt) as CombatPresentationReceipt;
		if (receipt != null && !ValidateDisplayReceipt(evt, receipt, state))
		{
			RejectedReceiptCount++;
			receipt = null;
		}
		if (receipt != null)
		{
			CombatPresentationVitals after = _presentedVitals?.Invoke(evt) ?? receipt.After;
			if (receipt.Target == "player")
			{
				player = player with
				{
					Hp = after.Hp,
					MaxHp = after.MaxHp,
					Block = after.Block,
					IceBlock = (after.IceBlock ?? player.IceBlock)
				};
			}
			else
			{
				int? enemyIndex = receipt.EnemyIndex;
				if (enemyIndex.HasValue)
				{
					int index = enemyIndex.GetValueOrDefault();
					if (index >= 0 && index < state.Enemies.Count)
					{
						EnemyState[] enemies = state.Enemies.ToArray();
						enemies[index] = enemies[index]with
						{
							Hp = after.Hp,
							MaxHp = after.MaxHp,
							Block = after.Block
						};
						state = state with
						{
							Enemies = enemies
						};
					}
				}
			}
		}
		if (!(evt is GameEvent_Cascaded e))
		{
			if (!(evt is GameEvent_EnergyGained e2))
			{
				if (!(evt is GameEvent_EnergyMaxGained e3))
				{
					if (!(evt is GameEvent_GrowthAdded e4))
					{
						if (!(evt is GameEvent_GrowthDischarged e5))
						{
							if (!(evt is GameEvent_MomentumAdded e6))
							{
								if (!(evt is GameEvent_MomentumDischarged e7))
								{
									if (!(evt is GameEvent_AetherGained e8))
									{
										if (!(evt is GameEvent_AetherDischarged e9))
										{
											if (!(evt is GameEvent_LightGained e10))
											{
												if (!(evt is GameEvent_LightSpent e11))
												{
													if (!(evt is GameEvent_LightDischarged e12))
													{
														if (evt is GameEvent_DiscountGained e13)
														{
															player = player with
															{
																NextCardDiscount = player.NextCardDiscount + e13.Amount
															};
														}
													}
													else
													{
														player = player with
														{
															Light = Math.Max(0, player.Light.GetValueOrDefault() - e12.Spent)
														};
													}
												}
												else
												{
													player = player with
													{
														Light = Math.Max(0, player.Light.GetValueOrDefault() - e11.Amount)
													};
												}
											}
											else
											{
												player = player with
												{
													Light = player.Light.GetValueOrDefault() + e10.Amount
												};
											}
										}
										else
										{
											player = player with
											{
												Aether = Math.Max(0, player.Aether - e9.Spent)
											};
										}
									}
									else
									{
										player = player with
										{
											Aether = player.Aether + e8.Amount
										};
									}
								}
								else
								{
									player = player with
									{
										Momentum = Math.Max(0, player.Momentum - e7.Spent)
									};
								}
							}
							else
							{
								player = player with
								{
									Momentum = player.Momentum + e6.Amount
								};
							}
						}
						else
						{
							player = player with
							{
								Growth = Math.Max(0, player.Growth - e5.Spent)
							};
						}
					}
					else
					{
						player = player with
						{
							Growth = player.Growth + e4.Amount
						};
					}
				}
				else
				{
					player = player with
					{
						EnergyMax = player.EnergyMax + e3.Amount
					};
				}
			}
			else
			{
				player = player with
				{
					Energy = player.Energy + e2.Amount
				};
			}
		}
		else if (e.HitId != null)
		{
			player = player with
			{
				CascadesThisCombat = player.CascadesThisCombat.GetValueOrDefault() + 1
			};
		}
		int cost;
		bool paid;
		if (!_costPresented)
		{
			GameState before = _activeChange?.Previous?.Combat;
			if ((object)before != null && _activeChange.Command is RunCommand_Combat combat)
			{
				cost = 0;
				paid = false;
				if (evt is GameEvent_CardPlayed)
				{
					Command command = combat.Command;
					Command_PlayCard play = command as Command_PlayCard;
					if ((object)play != null)
					{
						CardInstance card = before.Player.Hand.FirstOrDefault((CardInstance c) => c.Uid == play.CardUid);
						if (card != null)
						{
							cost = ((card.Def.XCost != true) ? Effects.EffectiveCost(before, card) : (play.XAmount ?? Effects.EffectiveCost(before, card)));
							paid = true;
						}
						goto IL_0709;
					}
				}
				if (evt is GameEvent_CardSet)
				{
					Command command = combat.Command;
					Command_SetCard set = command as Command_SetCard;
					if ((object)set != null)
					{
						CardInstance card2 = before.Player.Hand.FirstOrDefault((CardInstance c) => c.Uid == set.CardUid);
						if (card2 != null)
						{
							cost = ((!(card2.Def.Type == "reaction")) ? 1 : ((card2.FreeThisCombat != true) ? card2.Def.Cost : 0));
							paid = true;
						}
						goto IL_0709;
					}
				}
				if (evt is GameEvent_CardPlayedFromExhaust || evt.Type == "NecroPlayed")
				{
					Command command = combat.Command;
					Command_PlayNecro necro = command as Command_PlayNecro;
					if ((object)necro != null)
					{
						CardInstance card3 = before.Player.ExhaustPile.FirstOrDefault((CardInstance c) => c.Uid == necro.CardUid);
						if (card3 != null)
						{
							cost = card3.Def.NecroCost.GetValueOrDefault();
							paid = true;
						}
					}
				}
				goto IL_0709;
			}
		}
		goto IL_0738;
		IL_0709:
		if (paid)
		{
			player = player with
			{
				Energy = Math.Max(0, player.Energy - cost)
			};
			_costPresented = true;
		}
		goto IL_0738;
		IL_0738:
		GameState projection = _presentedState?.Invoke();
		if ((object)projection != null)
		{
			player = player with
			{
				Permanents = projection.Player.Permanents
			};
			if (evt is GameEvent_TurnStarted)
			{
				player = player with
				{
					Block = projection.Player.Block,
					IceBlock = projection.Player.IceBlock
				};
			}
			state = state with
			{
				Enemies = projection.Enemies
			};
		}
		state = state with
		{
			Player = player
		};
		_view = _view with
		{
			Combat = state
		};
		Set(_hpReadout, $"HP {player.Hp}/{player.MaxHp}");
		Set(_energyReadout, $"エナジー {player.Energy}/{player.EnergyMax}");
		Set(_blockReadout, $"ブロック {player.Block}  氷壁 {player.IceBlock}");
		Set(_statusReadout, PlayerStatus(state));
		foreach (KeyValuePair<int, Label> pair in _enemyReadouts)
		{
			if (pair.Key < state.Enemies.Count)
			{
				EnemyState enemy = state.Enemies[pair.Key];
				Set(pair.Value, EnemyVitalsText(enemy.Hp, enemy.MaxHp, enemy.Block));
			}
		}
		foreach (KeyValuePair<int, Label> pair2 in _enemyIntentReadouts)
		{
			if (pair2.Key < state.Enemies.Count)
			{
				Set(pair2.Value, (state.HideIntents == true) ? "意図は隠されています" : CardText.IntentText(state, pair2.Key));
			}
		}
	}

	private bool ValidateDisplayReceipt(GameEvent evt, CombatPresentationReceipt receipt, GameState state)
	{
		if (_receiptValidator != null)
		{
			return _receiptValidator(evt, receipt, state);
		}
		return BattleStage.ValidReceipt(evt, receipt, state);
	}

	private static void Set(Label? label, string text)
	{
		if (label != null && GodotObject.IsInstanceValid(label))
		{
			label.Text = text;
		}
	}

	public void SetEnemyResolver(Func<Vector2, int?> resolver)
	{
		_enemyResolver = resolver;
	}

	public bool CanDropHandCard(string uid)
	{
		if (!IsInputBlocked && _overlay == null)
		{
			GameState state = _view?.Combat;
			if ((object)state != null)
			{
				foreach (CardInstance card in state.Player.Hand)
				{
					if (card.Uid == uid)
					{
						return FlowRules.PlayBlocked(state, card) == null;
					}
				}
				return false;
			}
		}
		return false;
	}

	public void DropHandCard(string uid, Vector2 viewportPoint)
	{
		if (!CanDropHandCard(uid))
		{
			return;
		}
		GameState state = _view?.Combat;
		if ((object)state == null)
		{
			return;
		}
		foreach (CardInstance card in state.Player.Hand)
		{
			if (card.Uid == uid)
			{
				BeginPlay(card, _enemyResolver?.Invoke(viewportPoint));
				break;
			}
		}
	}

	public void PlayAllSparks()
	{
		if (!IsInputBlocked && !_showTitle && _overlay == null && _play == null)
		{
			GameState state = _session?.CurrentRun?.Combat;
			if ((object)state != null && !(SourceUiPolicies.NextSpark(state) == null))
			{
				_sparksRemaining = 12;
				PumpSparks();
			}
		}
	}

	private void PumpSparks()
	{
		if (_pumpingSparks || _sparksRemaining == 0 || IsInputBlocked)
		{
			return;
		}
		_pumpingSparks = true;
		try
		{
			while (_sparksRemaining > 0 && !IsInputBlocked)
			{
				if (!_showTitle && _overlay == null)
				{
					RunState runState = _session?.CurrentRun;
					if ((object)runState != null && runState.Phase == "combat")
					{
						GameState state = runState.Combat;
						if ((object)state != null)
						{
							CardInstance spark = SourceUiPolicies.NextSpark(state);
							if ((object)spark != null)
							{
								_sparksRemaining--;
								SessionResult result = _session.Dispatch(new Command_PlayCard
								{
									CardUid = spark.Uid
								});
								if (!result.Success)
								{
									_sparksRemaining = 0;
								}
								Result(result);
								continue;
							}
						}
					}
				}
				_sparksRemaining = 0;
				break;
			}
		}
		finally
		{
			_pumpingSparks = false;
		}
	}

	private void ContinueFromTitle()
	{
		if (_session != null && !IsInputBlocked)
		{
			SessionContinuePreview preview = _session.PreviewContinue();
			if (!preview.Result.Success)
			{
				Result(preview.Result);
			}
			else if (preview.RequiresConfirmation)
			{
				Confirm("別のデータ版のセーブ", "このセーブは別のデータバージョンで作られています。カード・敵の定義が変わっていると正しく動かない可能性がありますが、読み込みますか？", Resume);
			}
			else
			{
				Resume();
			}
		}
		void Resume()
		{
			Sound("ui.start_run");
			Result(_session.Continue());
		}
	}

	private void Sound(string? key)
	{
		if (key != null)
		{
			this.UiSoundRequested?.Invoke(key);
		}
	}

	private void OfferQueuedRating()
	{
		if (_ratingQueued && !IsInputBlocked && !_showTitle && _overlay == null && !(_view == null) && SourceUiPolicies.RatingPhase(_view.Phase))
		{
			_ratingQueued = false;
			if ((_session?.History.Count ?? 0) > 0)
			{
				OpenRating();
			}
		}
	}

	private void InspectEnemy(int index)
	{
		GameState state = _view?.Combat;
		if (state == null || index < 0 || index >= state.Enemies.Count || IsInputBlocked)
		{
			return;
		}
		EnemyState enemy = state.Enemies[index];
		EnemyDef definition = Content.GetEnemyDef(enemy.EnemyId);
		Open(definition.Name, delegate(VBoxContainer box)
		{
			VBoxContainer parent = ScrollColumn(box);
			Text(parent, $"HP {enemy.Hp}/{enemy.MaxHp}  ブロック {enemy.Block}", 25, Gold);
			if (enemy.Hp > 0)
			{
				Text(parent, "意図: " + CardText.IntentText(state, index), 23);
			}
			Text(parent, CardText.EnemyTraits(definition, state, index), 21);
			Text(parent, EnemyStatus(enemy), 21, Muted);
			int num = FlowRules.GuardRedirect(state, index);
			if (num != index && num >= 0 && num < state.Enemies.Count)
			{
				Text(parent, Content.GetEnemyDef(state.Enemies[num].EnemyId).Name + "が庇っている（単体の札は守護者へ向かう）", 21, Gold);
			}
		});
	}

	public void PresentNumericContact(GameEvent evt)
	{
		GameState state = _view?.Combat;
		if ((object)state == null)
		{
			return;
		}
		CombatPresentationVitals after = _presentedVitals?.Invoke(evt);
		if ((object)after == null)
		{
			return;
		}
		CombatPresentationReceipt receipt = evt.GetType().GetProperty("PresentationReceipt")?.GetValue(evt) as CombatPresentationReceipt;
		if (receipt == null || !ValidateDisplayReceipt(evt, receipt, state))
		{
			return;
		}
		if (receipt.Target == "player")
		{
			PlayerState player = state.Player with
			{
				Hp = after.Hp,
				MaxHp = after.MaxHp,
				Block = after.Block,
				IceBlock = (after.IceBlock ?? state.Player.IceBlock)
			};
			state = state with
			{
				Player = player
			};
			Set(_hpReadout, $"HP {player.Hp}/{player.MaxHp}");
			Set(_blockReadout, $"ブロック {player.Block}  氷壁 {player.IceBlock}");
		}
		else
		{
			int? enemyIndex = receipt.EnemyIndex;
			if (enemyIndex.HasValue)
			{
				int i = enemyIndex.GetValueOrDefault();
				if (i >= 0 && i < state.Enemies.Count)
				{
					List<EnemyState> enemies = new List<EnemyState>(state.Enemies);
					enemies[i] = enemies[i]with
					{
						Hp = after.Hp,
						MaxHp = after.MaxHp,
						Block = after.Block
					};
					state = state with
					{
						Enemies = enemies
					};
					if (_enemyReadouts.TryGetValue(i, out Label label))
					{
						Set(label, EnemyVitalsText(after.Hp, after.MaxHp, after.Block));
					}
				}
			}
		}
		_view = _view with
		{
			Combat = state
		};
		PresentedNumericContactCount++;
	}

	private static void DisableUnderlyingFocus(Node node)
	{
		if (node is Control control)
		{
			control.FocusMode = FocusModeEnum.None;
		}
		foreach (Node child in node.GetChildren())
		{
			DisableUnderlyingFocus(child);
		}
	}

	private static Control? FirstFocusable(Node node)
	{
		if (node is Control control && control.FocusMode == FocusModeEnum.All && !(control is BaseButton { Disabled: not false }))
		{
			return control;
		}
		foreach (Node child in node.GetChildren())
		{
			Control found = FirstFocusable(child);
			if (found != null)
			{
				return found;
			}
		}
		return null;
	}

	private void ShowTitle()
	{
		if (_session != null && !IsInputBlocked)
		{
			SessionResult result = _session.ReturnToTitle();
			if (!result.Success)
			{
				Result(result);
				return;
			}
			_view = _session.CurrentRun;
			_showTitle = true;
			_overlay = null;
			_overlayStack.Clear();
			_play = null;
			_ratingQueued = false;
			_sparksRemaining = 0;
			_notice = result.Warning ?? "";
			_error = result.Warning != null;
			Rebuild();
		}
	}

	private void SharePair(string report, string save)
	{
		_shareAwaiting = true;
		string message;
		bool queued = (_shareAwaiting = AndroidReportShare.TryShare(report, save, "deckrogue-report", out message));
		_notice = (queued ? "共有先の選択画面を開くよう依頼しました。送信先は自分で選んでください。" : ("共有画面を開けません: " + message));
		_error = !queued;
		Rebuild();
	}

	private void ShareRequestFinished(bool opened, string message)
	{
		if (!_shareAwaiting)
		{
			return;
		}
		_shareAwaiting = false;
		Callable.From(delegate
		{
			if (IsInsideTree())
			{
				_notice = (opened ? "共有先の選択画面を開きました。送信の完了はここでは確認できません。" : ("共有画面を開けません: " + message));
				_error = !opened;
				Rebuild();
			}
		}).CallDeferred();
	}

	private void BuildTitle()
	{
		Heading("坑へ降りる、その前に。", "からくり式デッキ構築ローグライク · リーダーを選ぶ");
		if (_session == null || !_session.IsInitialized)
		{
			Text(_body, _session?.InitializationError ?? "ゲームデータを読み込み中…", 22, Danger);
			return;
		}
		HBoxContainer banner = Row(_body);
		if (_session.CanContinue)
		{
			Button(banner, "続きから", ContinueFromTitle, enabled: true, Accent);
			Button(banner, "セーブを放棄", delegate
			{
				Confirm("進行中のランを放棄？", "この端末のセーブを消します。", delegate
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
		Button(banner, "読み込む", OpenImport);
		Button(banner, "設定", OpenSettings);
		if (_session.HasRetainedReport)
		{
			Button(banner, "保存したランのレポート", OpenRetainedExport);
		}
		HBoxContainer main = Row(_body);
		main.SizeFlagsVertical = SizeFlags.ExpandFill;
		VBoxContainer scroll = ScrollColumn(main);
		scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		HFlowContainer grid = Flow(scroll);
		foreach (LeaderDef leader in _session.Catalog.Leaders)
		{
			bool selected = leader.Id == _leaderId;
			PanelContainer pan = new PanelContainer
			{
				CustomMinimumSize = new Vector2(180f, 224f)
			};
			pan.AddThemeStyleboxOverride("panel", Box(Panel, selected ? Gold : CardColor(leader.Colors[0]), (!selected) ? 1 : 3));
			grid.AddChild(pan, forceReadableName: false, InternalMode.Disabled);
			VBoxContainer col = Column(pan);
			Picture(col, "leaders", leader.Id, new Vector2(146f, 98f));
			Text(col, leader.Name, 21, selected ? Gold : Ink);
			Text(col, $"HP {leader.MaxHp}  E {leader.EnergyMax}\nドロー {leader.DrawPerTurn}  報酬 {leader.RewardChoices}", 15, Muted);
			Button(col, selected ? "選択中" : "選ぶ", delegate
			{
				_leaderId = leader.Id;
				_deckId = null;
				Rebuild();
			});
		}
		VBoxContainer sideFrame = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(370f, 0f),
			SizeFlagsHorizontal = SizeFlags.Fill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		sideFrame.AddThemeConstantOverride("separation", 8);
		main.AddChild(sideFrame, forceReadableName: false, InternalMode.Disabled);
		ScrollContainer sideScroll = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		sideFrame.AddChild(sideScroll, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer side = Column(sideScroll);
		side.CustomMinimumSize = new Vector2(350f, 0f);
		LeaderDef chosen = _session.Catalog.Leaders.FirstOrDefault((LeaderDef x) => x.Id == _leaderId) ?? _session.Catalog.Leaders[0];
		Text(side, chosen.Name, 29, Gold);
		Text(side, chosen.Description, 18);
		IReadOnlyList<string> decks = chosen.RunDeckChoices ?? new string[1] { chosen.RunDeckId };
		if (_deckId == null || !decks.Contains<string>(_deckId))
		{
			_deckId = chosen.RunDeckId;
		}
		Text(side, "初期デッキ", 21, Accent);
		foreach (string id in decks)
		{
			DeckDef deck = Content.GetDeckDef(id);
			Button(side, ((_deckId == id) ? "● " : "○ ") + deck.Name, delegate
			{
				_deckId = id;
				Rebuild();
			});
		}
		DeckDef currentDeck = Content.GetDeckDef(_deckId);
		Text(side, currentDeck.Description, 16, Muted);
		Text(side, string.Join("、", currentDeck.Cards.Select((DeckCardEntry c) => Content.GetCardDef(c.CardId).Name + "×" + c.Count)), 15);
		VBoxContainer fixedSide = Column(sideFrame);
		HBoxContainer seedRow = Row(fixedSide);
		Text(seedRow, "シード", 18);
		LineEdit seed = new LineEdit
		{
			Text = _seed.ToString(),
			CustomMinimumSize = new Vector2(130f, 48f),
			MaxLength = 9
		};
		seedRow.AddChild(seed, forceReadableName: false, InternalMode.Disabled);
		seed.TextChanged += delegate(string value)
		{
			if (int.TryParse(value, out var result))
			{
				_seed = Math.Max(1, result);
			}
		};
		Button(seedRow, "ランダム", delegate
		{
			_seed = GD.RandRange(1, 999999);
			Rebuild();
		});
		HBoxContainer diff = Row(fixedSide);
		Button(diff, "−", delegate
		{
			_difficulty--;
			Rebuild();
		}, _difficulty > 1);
		Text(diff, $"難易度 {_difficulty} / 10", 20);
		Button(diff, "+", delegate
		{
			_difficulty++;
			Rebuild();
		}, _difficulty < 10);
		Text(fixedSide, Run.DifficultyDescription(_difficulty), 14, Muted).MaxLinesVisible = 2;
		Button(fixedSide, "ランを開始", delegate
		{
			Action action = delegate
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
		}, enabled: true, Gold);
		Button(fixedSide, "戦闘方式の設定", delegate
		{
			Open("戦闘方式", delegate(VBoxContainer b)
			{
				Text(b, "標準は「仕込み・確認式」です。他の方式は元エンジンの実験用ルールです。");
				(string, string)[] array = new(string, string)[3]
				{
					("set-confirm", "仕込み・確認式"),
					("set-auto", "仕込み・自動式"),
					("hold-manual", "手持ち・手動式")
				};
				foreach ((string, string) tuple in array)
				{
					(string, string) pair = tuple;
					Button(b, ((_reactionMode == pair.Item1) ? "● " : "○ ") + pair.Item2, delegate
					{
						_reactionMode = pair.Item1;
						CloseOverlay();
					});
				}
				CheckButton checkButton = new CheckButton
				{
					Text = "実験: 通常のカードも仕込み可",
					ButtonPressed = _setAnyCards
				};
				checkButton.Toggled += delegate(bool value)
				{
					_setAnyCards = value;
				};
				b.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
			});
		});
	}

	private void BuildMap(Node parent, RunState run, bool interactive)
	{
		Text(parent, $"幕 {run.Act}  ·  坑道の地図", 30, Gold);
		Text(parent, "光る道から次の部屋を選ぶ。線は進める道。", 18, Muted);
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
		map.Build(run, delegate(int col)
		{
			Do(new RunCommand_ChooseNode
			{
				Col = col
			});
		}, interactive && !IsInputBlocked, base.Theme, FontSize(16));
		if (interactive)
		{
			scroll.SetDeferred(ScrollContainer.PropertyName.ScrollVertical, Math.Max(0, (run.Map.Count - run.Row - 4) * 108));
		}
		JObject annotations = (_session?.MapAnnotations as JObject) ?? new JObject();
		string act = run.Act.ToString();
		JObject acts = (annotations["acts"] as JObject) ?? new JObject();
		annotations["acts"] = acts;
		map.LoadAnnotations(acts[act] as JArray);
		map.AnnotationsChanged += delegate(JArray strokes)
		{
			acts[act] = strokes;
			annotations["version"] = 1;
			if (_session != null)
			{
				Result(_session.SetMapAnnotations(annotations));
			}
		};
		HBoxContainer tools = Row(parent);
		Button(tools, "道を選ぶ", delegate
		{
			map.SetDrawing(drawing: false);
		});
		Button(tools, "地図に描く: 金", delegate
		{
			map.SetDrawing(drawing: true);
		});
		Button(tools, "朱", delegate
		{
			map.SetDrawing(drawing: true, "f3968a");
		});
		Button(tools, "青", delegate
		{
			map.SetDrawing(drawing: true, "8cccec");
		});
		Button(tools, "最後の線を消す", map.UndoAnnotation);
	}

	private void BuildDeparture(RunState run)
	{
		Heading("出立の店", "坑口の行商の店。坑に降りる前に、支度を整えに寄った。");
		Text(_body, Content.DepartureMasterLine(run.Colors), 20, Accent);
		HBoxContainer main = Row(_body);
		main.SizeFlagsVertical = SizeFlags.ExpandFill;
		VBoxContainer left = ScrollColumn(main);
		VBoxContainer right = ScrollColumn(main);
		right.GetParent<Control>().CustomMinimumSize = new Vector2(350f, 0f);
		right.GetParent<Control>().SizeFlagsHorizontal = SizeFlags.Fill;
		HFlowContainer cards = Flow(left);
		HFlowContainer services = Flow(left);
		IReadOnlyList<DepartureOffer> offers = run.Departure?.Offers ?? Array.Empty<DepartureOffer>();
		for (int i = 0; i < offers.Count; i++)
		{
			int index = i;
			DepartureOffer offer = offers[i];
			Action buy = delegate
			{
				ResolveChoice(run, offer.Choice, offer.Name, delegate(int? ci)
				{
					Do(new RunCommand_BuyDeparture
					{
						Index = index,
						CardIndex = ci
					});
				});
			};
			bool bought = Run.DepartureOfferBought(run, offer);
			bool available = Run.DepartureOfferAvailable(run, offer);
			if (offer.Kind == "card")
			{
				IReadOnlyList<string>? addCardIds = offer.Choice.AddCardIds;
				if (addCardIds != null && addCardIds.Count > 0)
				{
					VBoxContainer column = Column(cards);
					column.SizeFlagsHorizontal = SizeFlags.Fill;
					CardInstance card = new CardInstance
					{
						Uid = "departure-" + i,
						Def = Content.GetCardDef(offer.Choice.AddCardIds[0])
					};
					Card(column, card, delegate
					{
						InspectCard(card);
					});
					Button(column, bought ? "購入済み" : $"{offer.Price} G で買う", buy, available, Gold);
					continue;
				}
			}
			VBoxContainer tile = DepartureTile((offer.Kind == "relic") ? ((Godot.Container)right) : ((Godot.Container)services), offer);
			tile.GetParent<Control>().CustomMinimumSize = new Vector2((offer.Kind == "relic") ? 320 : 232, 150f);
			Button(tile, bought ? "購入済み" : $"{offer.Price} G で買う", buy, available, Gold);
		}
		Button(_body, "店を出て坑へ", delegate
		{
			Do(new RunCommand_LeaveDeparture());
		}, enabled: true, Accent);
	}

	private VBoxContainer DepartureTile(Node parent, DepartureOffer offer)
	{
		EventChoiceDef choice = offer.Choice;
		List<string> parts = new List<string>();
		if (choice.RelicId != null)
		{
			RelicDef relic = Content.GetRelicDef(choice.RelicId);
			parts.Add(relic.Name + "\n" + relic.Description);
		}
		if (choice.Gears != null)
		{
			foreach (string id in choice.Gears)
			{
				GearDef gear = Content.GetGearDef(id);
				parts.Add(gear.Name + ": " + gear.Text);
			}
		}
		int? mana = choice.Mana;
		if (mana.HasValue)
		{
			int mana2 = mana.GetValueOrDefault();
			if (true)
			{
				parts.Add("魔素 " + mana2);
			}
		}
		if (choice.AddCardIds != null)
		{
			foreach (string id2 in choice.AddCardIds)
			{
				CardDef card = Content.GetCardDef(id2);
				parts.Add(card.Name + ": " + CardText.Body(card));
			}
		}
		object obj;
		if (choice.RelicId == null)
		{
			IReadOnlyList<string>? gears = choice.Gears;
			obj = ((gears != null && gears.Count > 0) ? "gears" : "icons");
		}
		else
		{
			obj = "relics";
		}
		string group = (string)obj;
		string image = choice.RelicId ?? choice.Gears?.FirstOrDefault() ?? ((choice.RemoveCard == true) ? "exhaust" : ((choice.UpgradeCard == true) ? "hammer" : ((choice.MaxHp.HasValue || choice.Hp.HasValue) ? "heart" : "star")));
		return Tile(parent, offer.Name, (parts.Count > 0) ? string.Join("\n", parts) : offer.Text, group, image);
	}

	private void ResolveChoice(RunState run, EventChoiceDef choice, string title, Action<int?> submit)
	{
		if (!Run.EventChoiceNeedsCard(choice))
		{
			submit(null);
			return;
		}
		Open(title + " · 札を1枚選ぶ", delegate(VBoxContainer box)
		{
			CardGrid(box, run.Deck, delegate(CardInstance card)
			{
				Confirm(title, card.Def.Name + "を選びます。", delegate
				{
					submit(run.Deck.ToList().FindIndex((CardInstance c) => c.Uid == card.Uid));
				});
			}, (CardInstance card) => FlowRules.ChoiceCardAllowed(run, choice, card), (CardInstance card) => (choice.UpgradeCard == true && Upgrade.CanUpgradeCard(card)) ? ("強化後: " + CardText.Body(Upgrade.UpgradeCard(card).Def)) : "選ぶ");
		});
	}

	private void BuildReward(RunState run)
	{
		Heading("戦利品", "札を選ぶか、見送って進む。ギアは別に選べます。");
		if ((_session?.History.Count ?? 0) > 0)
		{
			Button(_body, "直前の戦闘を評価", OpenRating);
		}
		VBoxContainer scroll = ScrollColumn(_body);
		if (run.RewardOptions != null)
		{
			HFlowContainer flow = Flow(scroll);
			for (int i = 0; i < run.RewardOptions.Count; i++)
			{
				int index = i;
				string id = run.RewardOptions[i];
				VBoxContainer col = Column(flow);
				col.SizeFlagsHorizontal = SizeFlags.Fill;
				Card(col, new CardInstance
				{
					Uid = "reward-" + i,
					Def = Content.GetCardDef(id)
				}, delegate
				{
					InspectCard(new CardInstance
					{
						Uid = "reward-" + i,
						Def = Content.GetCardDef(id)
					});
				});
				Button(col, "この札を取る", delegate
				{
					Do(new RunCommand_PickReward
					{
						Index = index
					});
				}, enabled: true, Accent);
			}
		}
		if (run.GearOption != null)
		{
			GearDef gear = Content.GetGearDef(run.GearOption);
			Tile(scroll, gear.Name, gear.Text, "gears", gear.Id);
		}
		HBoxContainer actions = Row(_body);
		if (run.RewardOptions != null)
		{
			Button(actions, "札を見送る", delegate
			{
				Do(new RunCommand_SkipReward());
			});
		}
		if (run.GearOption == null)
		{
			return;
		}
		Button(actions, "ギアを取る", delegate
		{
			TakeGear(run, delegate(int? discard)
			{
				Do(new RunCommand_TakeGear
				{
					DiscardIndex = discard
				});
			});
		}, enabled: true, Gold);
		Button(actions, "ギアを見送る", delegate
		{
			Do(new RunCommand_SkipGear());
		});
	}

	private void TakeGear(RunState run, Action<int?> submit)
	{
		if ((run.Gears?.Count ?? 0) < 10)
		{
			submit(null);
			return;
		}
		Open("持ち物がいっぱい · 入れ替えるギアを選ぶ", delegate(VBoxContainer box)
		{
			VBoxContainer parent = ScrollColumn(box);
			for (int i = 0; i < run.Gears.Count; i++)
			{
				int index = i;
				GearInstance gearInstance = run.Gears[i];
				Button(parent, Content.GetGearDef(gearInstance.GearId).Name + "を手放す", delegate
				{
					submit(index);
				});
			}
		});
	}

	private void BuildRelicReward(RunState run)
	{
		Heading("遺物を選ぶ", $"あと {run.RelicPicksLeft ?? 1} 個選べます。");
		VBoxContainer scroll = ScrollColumn(_body);
		HFlowContainer row = Flow(scroll);
		IReadOnlyList<string> options = run.RelicOptions ?? Array.Empty<string>();
		for (int i = 0; i < options.Count; i++)
		{
			int index = i;
			RelicDef relic = Content.GetRelicDef(options[i]);
			VBoxContainer col = Tile(row, relic.Name, relic.Description, "relics", relic.Id);
			col.GetParent<Control>().CustomMinimumSize = new Vector2(310f, 280f);
			Button(col, "この遺物を取る", delegate
			{
				Do(new RunCommand_PickRelic
				{
					Index = index
				});
			}, enabled: true, Gold);
		}
		Button(_body, "遺物を見送る", delegate
		{
			Do(new RunCommand_SkipRelic());
		});
	}

	private void BuildRelicChoose(RunState run)
	{
		RunStatePendingRelicChoice pending = run.PendingRelicChoice;
		if (pending == null)
		{
			return;
		}
		bool remove = pending.Mode == "remove";
		int limit = (remove ? Math.Min(pending.Count, Math.Max(0, run.Deck.Count - 5)) : pending.Count);
		Heading(Content.GetRelicDef(pending.RelicId).Name, $"{limit}枚まで選ぶ。選ばずに決定することもできます。" + (remove ? " 選んだ札を取り除く。" : " 選んだ札を変成して鍛える。"));
		CardGrid(_body, run.Deck, delegate(CardInstance card)
		{
			int item = run.Deck.ToList().FindIndex((CardInstance c) => c.Uid == card.Uid);
			if (!_relicPicks.Remove(item) && _relicPicks.Count < limit)
			{
				_relicPicks.Add(item);
			}
			Rebuild();
		}, (CardInstance card) => (remove || Run.CanTransformCard(card)) && (_relicPicks.Contains(run.Deck.ToList().FindIndex((CardInstance c) => c.Uid == card.Uid)) || _relicPicks.Count < limit), (CardInstance card) => _relicPicks.Contains(run.Deck.ToList().FindIndex((CardInstance c) => c.Uid == card.Uid)) ? "選択中" : "選ぶ");
		Button(_body, $"決定 ({_relicPicks.Count}/{limit} 枚)", delegate
		{
			Do(new RunCommand_RelicChooseCards
			{
				Indices = _relicPicks.OrderBy((int i) => i).ToArray()
			});
		}, enabled: true, Gold);
	}

	private void BuildShop(RunState run)
	{
		Heading("商店", $"所持金 {run.Gold} G · 魔素 {Gears.ManaLabel(run.Mana.GetValueOrDefault())}");
		if (run.Shop == null)
		{
			Button(_body, "店を出る", delegate
			{
				Do(new RunCommand_ShopLeave());
			});
			return;
		}
		ShopState shop = run.Shop;
		VBoxContainer scroll = ScrollColumn(_body);
		HFlowContainer cards = Flow(scroll);
		for (int i = 0; i < shop.Cards.Count; i++)
		{
			int index = i;
			ShopStateCards offer = shop.Cards[i];
			VBoxContainer col = Column(cards);
			col.SizeFlagsHorizontal = SizeFlags.Fill;
			CardInstance card = new CardInstance
			{
				Uid = "shop-" + i,
				Def = Content.GetCardDef(offer.Id)
			};
			Card(col, card, delegate
			{
				InspectCard(card);
			});
			Button(col, (offer.Sold == true) ? "売切" : $"{offer.Price} G で買う", delegate
			{
				Do(new RunCommand_ShopBuyCard
				{
					Index = index
				});
			}, offer.Sold != true && run.Gold >= offer.Price, Gold);
		}
		HFlowContainer wares = Flow(scroll);
		if (shop.RelicId != null)
		{
			RelicDef relic = Content.GetRelicDef(shop.RelicId);
			VBoxContainer col2 = Tile(wares, relic.Name, relic.Description, "relics", relic.Id);
			col2.GetParent<Control>().CustomMinimumSize = new Vector2(290f, 200f);
			Button(col2, $"{shop.RelicPrice} G で買う", delegate
			{
				Do(new RunCommand_ShopBuyRelic());
			}, run.Gold >= shop.RelicPrice, Gold);
		}
		for (int i2 = 0; i2 < (shop.Gears?.Count ?? 0); i2++)
		{
			int index2 = i2;
			ShopStateGears offer2 = shop.Gears[i2];
			GearDef gear = Content.GetGearDef(offer2.Id);
			VBoxContainer col3 = Tile(wares, gear.Name, gear.Text, "gears", gear.Id);
			col3.GetParent<Control>().CustomMinimumSize = new Vector2(260f, 200f);
			Button(col3, (offer2.Sold == true) ? "売切" : $"{offer2.Price} G で買う", delegate
			{
				TakeGear(run, delegate(int? discard)
				{
					Do(new RunCommand_ShopBuyGear
					{
						Index = index2,
						DiscardIndex = discard
					});
				});
			}, offer2.Sold != true && run.Gold >= offer2.Price, Gold);
		}
		HBoxContainer services = Row(scroll);
		int removePrice = Run.ShopRemovalPrice(run);
		int upgradePrice = Run.ShopUpgradePrice(run);
		int manaPrice = shop.ManaPrice ?? 30;
		Button(services, $"札を取り除く  {removePrice} G", delegate
		{
			ChooseDeckCard(run, "取り除く", (CardInstance c) => run.Deck.Count > 5, delegate(int index4)
			{
				Do(new RunCommand_ShopRemove
				{
					Index = index4
				});
			});
		}, run.Gold >= removePrice && run.Deck.Count > 5);
		Button(services, $"札を鍛える  {upgradePrice} G", delegate
		{
			ChooseDeckCard(run, "鍛える", Upgrade.CanUpgradeCard, delegate(int index4)
			{
				Do(new RunCommand_ShopUpgrade
				{
					Index = index4
				});
			}, upgrade: true);
		}, run.Gold >= upgradePrice && run.Deck.Any(Upgrade.CanUpgradeCard));
		Button(services, $"魔素を買う  {manaPrice} G", delegate
		{
			Do(new RunCommand_ShopBuyMana());
		}, run.Gold >= manaPrice && run.Mana.GetValueOrDefault() < 50);
		IReadOnlyList<ShopStateDepartures> departures = shop.Departures ?? Array.Empty<ShopStateDepartures>();
		for (int i3 = 0; i3 < departures.Count; i3++)
		{
			int index3 = i3;
			ShopStateDepartures slot = departures[i3];
			DepartureOffer offer3 = run.Departure?.Leftovers.FirstOrDefault((DepartureOffer o) => o.Id == slot.Id);
			if (offer3 == null || slot.Sold == true)
			{
				continue;
			}
			VBoxContainer col4 = DepartureTile(scroll, offer3);
			Button(col4, $"{slot.Price} G で買う", delegate
			{
				ResolveChoice(run, offer3.Choice, offer3.Name, delegate(int? ci)
				{
					Do(new RunCommand_ShopBuyDeparture
					{
						Index = index3,
						CardIndex = ci
					});
				});
			}, run.Gold >= slot.Price && Run.EventChoiceAvailable(run, offer3.Choice));
		}
		GearInventory(scroll, run, usable: false);
		Button(_body, "店を出る", delegate
		{
			Do(new RunCommand_ShopLeave());
		}, enabled: true, Accent);
	}

	private void ChooseDeckCard(RunState run, string title, Func<CardInstance, bool> allowed, Action<int> submit, bool upgrade = false)
	{
		Open(title + " · 札を1枚選ぶ", delegate(VBoxContainer box)
		{
			CardGrid(box, run.Deck, delegate(CardInstance card)
			{
				int index = run.Deck.ToList().FindIndex((CardInstance c) => c.Uid == card.Uid);
				Open(title + " · " + card.Def.Name, delegate(VBoxContainer b)
				{
					HBoxContainer parent = Row(b);
					Card(parent, card, null);
					if (upgrade)
					{
						Card(parent, Upgrade.UpgradeCard(card), null, enabled: true, "鍛えた後");
					}
					Button(b, "この札を" + title, delegate
					{
						submit(index);
					}, enabled: true, Gold);
				});
			}, allowed, (CardInstance card) => allowed(card) ? title : "選べません");
		});
	}

	private void BuildCampfire(RunState run)
	{
		Run.CampfireOptionsInfo opt = Run.CampfireOptions(run);
		bool fresh = run.CampfireUpgradesUsed == 0;
		bool noRest = run.Relics.Any((string id) => Content.GetRelicDef(id).Bonus?.NoRest ?? false);
		int heal = (int)Math.Floor((double)run.MaxHp * run.CampfireRatio);
		int remain = (opt.Forge ? Math.Max(0, Run.CampfireForgeAllowed(run) - run.CampfireUpgradesUsed) : 0);
		Heading("焚き火", "どれか1つを選ぶ。鍛えた後は回復なしで立ち去る。");
		VBoxContainer scroll = ScrollColumn(_body);
		HFlowContainer flow = Flow(scroll);
		VBoxContainer rest = Tile(flow, (fresh && !noRest) ? "休む" : "立ち去る", (fresh && !noRest) ? $"HP {run.Hp} → {Math.Min(run.MaxHp, run.Hp + heal)} / {run.MaxHp}" : (noRest ? "所持する遺物のため、焚き火では回復できません。" : "すでに鍛えたので、回復はありません。"), "scenes", "campfire");
		rest.GetParent<Control>().CustomMinimumSize = new Vector2(300f, 260f);
		Button(rest, (fresh && !noRest) ? "休む" : "回復なしで立ち去る", delegate
		{
			Do(new RunCommand_CampfireRest());
		}, enabled: true, Accent);
		VBoxContainer forge = Tile(flow, "鍛える", $"デッキの札を強化する。残り {remain} 回。");
		forge.GetParent<Control>().CustomMinimumSize = new Vector2(300f, 260f);
		Button(forge, "鍛える札を選ぶ", delegate
		{
			ChooseDeckCard(run, "鍛える", Upgrade.CanUpgradeCard, delegate(int i)
			{
				Do(new RunCommand_CampfireUpgrade
				{
					Index = i
				});
			}, upgrade: true);
		}, remain > 0 && run.Deck.Any(Upgrade.CanUpgradeCard));
		if (opt.Dig && fresh)
		{
			Button(scroll, "発掘: 遺物を掘る", delegate
			{
				Do(new RunCommand_CampfireDig());
			});
		}
		if (opt.TrainLeft > 0 && fresh)
		{
			Button(scroll, $"鍛錬: 成長 +1 (あと {opt.TrainLeft} 回)", delegate
			{
				Do(new RunCommand_CampfireTrain());
			});
		}
		if (!(opt.Remove && fresh))
		{
			return;
		}
		Button(scroll, "取り除く: 安らぎの煙管", delegate
		{
			ChooseDeckCard(run, "取り除く", (CardInstance c) => run.Deck.Count > 5, delegate(int i)
			{
				Do(new RunCommand_CampfireRemove
				{
					Index = i
				});
			});
		}, run.Deck.Count > 5);
	}

	private void BuildWorkshop(RunState run)
	{
		Heading("工房", "同じ色の札を2枚選んで合成する。素材の札は失われます。");
		if (_workshopA >= run.Deck.Count)
		{
			_workshopA = -1;
		}
		if (_workshopB >= run.Deck.Count)
		{
			_workshopB = -1;
		}
		List<Fusion.RecipePair> pairs = Fusion.RecipePairsInDeck(run.Deck);
		HBoxContainer row = Row(_body);
		row.SizeFlagsVertical = SizeFlags.ExpandFill;
		VBoxContainer deckColumn = Column(row);
		CardGrid(deckColumn, run.Deck, delegate(CardInstance card)
		{
			int num = run.Deck.ToList().FindIndex((CardInstance c) => c.Uid == card.Uid);
			if (_workshopA == num)
			{
				_workshopA = -1;
			}
			else if (_workshopB == num)
			{
				_workshopB = -1;
			}
			else if (_workshopA < 0)
			{
				_workshopA = num;
			}
			else if (_workshopB < 0)
			{
				_workshopB = num;
			}
			else
			{
				_workshopA = _workshopB;
				_workshopB = num;
			}
			Rebuild();
		}, null, delegate(CardInstance card)
		{
			int i = run.Deck.ToList().FindIndex((CardInstance c) => c.Uid == card.Uid);
			return (_workshopA == i) ? "素材 A" : ((_workshopB == i) ? "素材 B" : (pairs.Any((Fusion.RecipePair p) => (_workshopA < 0 && (p.IndexA == i || p.IndexB == i)) || (p.IndexA == _workshopA && p.IndexB == i) || (p.IndexB == _workshopA && p.IndexA == i)) ? "★ レシピの素材" : "選ぶ"));
		});
		ScrollContainer sideScroll = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(310f, 0f),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		row.AddChild(sideScroll, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer side = Column(sideScroll);
		side.CustomMinimumSize = new Vector2(290f, 0f);
		bool can = false;
		int price = Run.WorkshopFusePrice(run);
		if (_workshopA >= 0 && _workshopB >= 0)
		{
			CardInstance a = run.Deck[_workshopA];
			CardInstance b = run.Deck[_workshopB];
			Text(side, a.Def.Name + " ＋ " + b.Def.Name, 20, Gold);
			string blocked = Fusion.FuseBlockReason(a, b);
			if (blocked == null)
			{
				CardDef def = Run.WorkshopFuseResult(run, a, b);
				Card(side, new CardInstance
				{
					Uid = "fusion-preview",
					Def = def
				}, null, enabled: true, "合成結果");
				Text(side, string.Join("\n", Fusion.FusionNotes(a, b)), 17, Muted);
				can = run.Gold >= price;
			}
			else
			{
				Text(side, blocked, 21, Danger);
			}
		}
		else
		{
			Text(side, "素材を2枚選ぶと、合成する札がここに表示されます。", 22, Muted);
		}
		Button(side, $"合成する  {price} G", delegate
		{
			int workshopA = _workshopA;
			int workshopB = _workshopB;
			_workshopA = (_workshopB = -1);
			Do(new RunCommand_WorkshopFuse
			{
				IndexA = workshopA,
				IndexB = workshopB
			});
		}, can, Gold);
		Button(side, "見送る", delegate
		{
			_workshopA = (_workshopB = -1);
			Do(new RunCommand_WorkshopSkip());
		});
	}

	private void BuildEvent(RunState run)
	{
		if (run.EventId == null)
		{
			Heading("出来事", "イベントが見つかりません。");
			return;
		}
		EventDef ev = Content.GetEventDef(run.EventId);
		Heading(ev.Name, ev.Flavor);
		VBoxContainer scroll = ScrollColumn(_body);
		Picture(scroll, "scenes", "event", new Vector2(180f, 140f));
		for (int i = 0; i < ev.Choices.Count; i++)
		{
			int index = i;
			EventChoiceDef choice = ev.Choices[i];
			Button(scroll, choice.Label, delegate
			{
				ResolveChoice(run, choice, choice.Label, delegate(int? card)
				{
					Do(new RunCommand_EventChoice
					{
						Index = index,
						CardIndex = card
					});
				});
			}, Run.EventChoiceAvailable(run, choice), Gold);
		}
	}

	private void BuildEnd(RunState run)
	{
		bool won = run.Phase == "won";
		Heading(won ? "走破！" : "敗北", Content.GetLeaderDef(run.LeaderId).Name + $" · 幕 {run.Act} · {run.BattlesWon} 戦勝利 · 難易度 {run.Difficulty} · シード {run.Seed}");
		Text(_body, $"HP {run.Hp}/{run.MaxHp}    {run.Gold} G    デッキ {run.Deck.Count}枚    遺物 {run.Relics.Count}個", 23, won ? Gold : Danger);
		CardGrid(_body, run.Deck, InspectCard, null);
		HBoxContainer row = Row(_body);
		Button(row, "タイトルへ", ShowTitle);
		Button(row, "ランの記録", OpenJournal);
		Button(row, "レポートを書き出す", OpenExport);
		if ((_session?.History.Count ?? 0) > 0)
		{
			Button(row, "直前の戦闘を評価", OpenRating);
		}
	}

	public IReadOnlyList<string> VisibleActions()
	{
		return (from b in Buttons(this)
			where b.IsVisibleInTree() && !b.Disabled
			select b.Text into s
			where s.Length > 0
			select s).ToArray();
	}

	public bool InvokeAction(string exactLabel)
	{
		if (IsInputBlocked)
		{
			return false;
		}
		Node root = _overlayNode ?? this;
		Button[] matches = (from b in Buttons(root)
			where b.IsVisibleInTree() && !b.Disabled && b.Text == exactLabel
			select b).ToArray();
		if (matches.Length != 1)
		{
			return false;
		}
		matches[0].EmitSignal(BaseButton.SignalName.Pressed);
		return true;
	}

	private static IEnumerable<Button> Buttons(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is Button button)
			{
				yield return button;
			}
			foreach (Button item in Buttons(child))
			{
				yield return item;
			}
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		List<MethodInfo> methods = new List<MethodInfo>(71);
		methods.Add(new MethodInfo(MethodName.AdvancePlay, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.SelectionTitle, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "need", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.BeginEndTurn, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.FontSize, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "requested", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.Bind, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "session", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.PresentationCompleted, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName._ExitTree, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.SelectEnemy, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName._UnhandledKeyInput, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "evt", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("InputEvent"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Rebuild, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.BuildHeader, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.Heading, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "title", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "subtitle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.CloseOverlay, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.CloseAllOverlays, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.BuildOverlay, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.OpenMenu, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.OpenKeywords, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.OpenJournal, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.OpenExport, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.OpenRetainedExport, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.OpenExportPair, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "save", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "report", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "title", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.CopyText, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ExportFile, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ImportFile, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.OpenImport, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.ImportText, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName._Notification, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "what", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.HandleBack, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.ApplySafeArea, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.ReadSettings, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.ApplyAudio, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.SaveSettings, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.OpenSettings, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.ApplyTheme, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.Box, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("StyleBoxFlat"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Color, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Color, "border", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "width", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Plain, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.SizeInteractiveControls, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "node", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Row, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("HBoxContainer"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Column, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("VBoxContainer"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ScrollColumn, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("VBoxContainer"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Flow, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("HFlowContainer"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Art, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Texture2D"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "group", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "id", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Picture, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false),
			new PropertyInfo(Variant.Type.String, "group", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "id", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Vector2, "size", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Tile, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("VBoxContainer"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false),
			new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "description", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "group", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "id", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.CardColor, new PropertyInfo(Variant.Type.Color, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.EnemyVitalsText, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "hp", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "maxHp", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "block", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.EnemySurface, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("PanelContainer"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false),
			new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "kind", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Color, "border", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.PositionEnemyHud, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.OpenNote, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.OpenRating, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.Paints, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "style", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("StyleBox"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.VisibleAlpha, new PropertyInfo(Variant.Type.Float, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "item", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("CanvasItem"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ViewportBounds, new PropertyInfo(Variant.Type.Rect2, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "control", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Control"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Set, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "label", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Label"), exported: false),
			new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.CanDropHandCard, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "uid", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.DropHandCard, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "uid", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Vector2, "viewportPoint", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.PlayAllSparks, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.PumpSparks, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.ContinueFromTitle, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.Sound, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "key", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.OfferQueuedRating, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.InspectEnemy, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.DisableUnderlyingFocus, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "node", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.FirstFocusable, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Control"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "node", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ShowTitle, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.SharePair, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "report", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "save", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ShareRequestFinished, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Bool, "opened", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "message", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.BuildTitle, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.InvokeAction, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "exactLabel", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		return methods;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.AdvancePlay && args.Count == 0)
		{
			AdvancePlay();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SelectionTitle && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(SelectionTitle(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.BeginEndTurn && args.Count == 0)
		{
			BeginEndTurn();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.FontSize && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(FontSize(VariantUtils.ConvertTo<int>(in args[0])));
			return true;
		}
		if (method == MethodName._Ready && args.Count == 0)
		{
			_Ready();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Bind && args.Count == 1)
		{
			Bind(VariantUtils.ConvertTo<GameSession>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.PresentationCompleted && args.Count == 0)
		{
			PresentationCompleted();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._ExitTree && args.Count == 0)
		{
			_ExitTree();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SelectEnemy && args.Count == 1)
		{
			SelectEnemy(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._UnhandledKeyInput && args.Count == 1)
		{
			_UnhandledKeyInput(VariantUtils.ConvertTo<InputEvent>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Rebuild && args.Count == 0)
		{
			Rebuild();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.BuildHeader && args.Count == 0)
		{
			BuildHeader();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Heading && args.Count == 2)
		{
			Heading(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.CloseOverlay && args.Count == 0)
		{
			CloseOverlay();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.CloseAllOverlays && args.Count == 0)
		{
			CloseAllOverlays();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.BuildOverlay && args.Count == 0)
		{
			BuildOverlay();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.OpenMenu && args.Count == 0)
		{
			OpenMenu();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.OpenKeywords && args.Count == 0)
		{
			OpenKeywords();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.OpenJournal && args.Count == 0)
		{
			OpenJournal();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.OpenExport && args.Count == 0)
		{
			OpenExport();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.OpenRetainedExport && args.Count == 0)
		{
			OpenRetainedExport();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.OpenExportPair && args.Count == 3)
		{
			OpenExportPair(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<string>(in args[2]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.CopyText && args.Count == 1)
		{
			CopyText(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ExportFile && args.Count == 2)
		{
			ExportFile(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ImportFile && args.Count == 0)
		{
			ImportFile();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.OpenImport && args.Count == 0)
		{
			OpenImport();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ImportText && args.Count == 1)
		{
			ImportText(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._Notification && args.Count == 1)
		{
			_Notification(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.HandleBack && args.Count == 0)
		{
			HandleBack();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ApplySafeArea && args.Count == 0)
		{
			ApplySafeArea();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ReadSettings && args.Count == 0)
		{
			ReadSettings();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ApplyAudio && args.Count == 0)
		{
			ApplyAudio();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SaveSettings && args.Count == 0)
		{
			SaveSettings();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.OpenSettings && args.Count == 0)
		{
			OpenSettings();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ApplyTheme && args.Count == 0)
		{
			ApplyTheme();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Box && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(Box(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<Color>(in args[1]), VariantUtils.ConvertTo<int>(in args[2])));
			return true;
		}
		if (method == MethodName.Plain && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(Plain(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.SizeInteractiveControls && args.Count == 1)
		{
			SizeInteractiveControls(VariantUtils.ConvertTo<Node>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Row && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<HBoxContainer>(Row(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.Column && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<VBoxContainer>(Column(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.ScrollColumn && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<VBoxContainer>(ScrollColumn(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.Flow && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<HFlowContainer>(Flow(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.Art && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<Texture2D>(Art(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1])));
			return true;
		}
		if (method == MethodName.Picture && args.Count == 4)
		{
			Picture(VariantUtils.ConvertTo<Node>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<string>(in args[2]), VariantUtils.ConvertTo<Vector2>(in args[3]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Tile && args.Count == 5)
		{
			ret = VariantUtils.CreateFrom<VBoxContainer>(Tile(VariantUtils.ConvertTo<Node>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<string>(in args[2]), VariantUtils.ConvertTo<string>(in args[3]), VariantUtils.ConvertTo<string>(in args[4])));
			return true;
		}
		if (method == MethodName.CardColor && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Color>(CardColor(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.EnemyVitalsText && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<string>(EnemyVitalsText(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]), VariantUtils.ConvertTo<int>(in args[2])));
			return true;
		}
		if (method == MethodName.EnemySurface && args.Count == 4)
		{
			ret = VariantUtils.CreateFrom<PanelContainer>(EnemySurface(VariantUtils.ConvertTo<Node>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]), VariantUtils.ConvertTo<string>(in args[2]), VariantUtils.ConvertTo<Color>(in args[3])));
			return true;
		}
		if (method == MethodName._Process && args.Count == 1)
		{
			_Process(VariantUtils.ConvertTo<double>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.PositionEnemyHud && args.Count == 0)
		{
			PositionEnemyHud();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.OpenNote && args.Count == 0)
		{
			OpenNote();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.OpenRating && args.Count == 0)
		{
			OpenRating();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Paints && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<bool>(Paints(VariantUtils.ConvertTo<StyleBox>(in args[0])));
			return true;
		}
		if (method == MethodName.VisibleAlpha && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<float>(VisibleAlpha(VariantUtils.ConvertTo<CanvasItem>(in args[0])));
			return true;
		}
		if (method == MethodName.ViewportBounds && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Rect2>(ViewportBounds(VariantUtils.ConvertTo<Control>(in args[0])));
			return true;
		}
		if (method == MethodName.Set && args.Count == 2)
		{
			Set(VariantUtils.ConvertTo<Label>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.CanDropHandCard && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<bool>(CanDropHandCard(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.DropHandCard && args.Count == 2)
		{
			DropHandCard(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<Vector2>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.PlayAllSparks && args.Count == 0)
		{
			PlayAllSparks();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.PumpSparks && args.Count == 0)
		{
			PumpSparks();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ContinueFromTitle && args.Count == 0)
		{
			ContinueFromTitle();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Sound && args.Count == 1)
		{
			Sound(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.OfferQueuedRating && args.Count == 0)
		{
			OfferQueuedRating();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.InspectEnemy && args.Count == 1)
		{
			InspectEnemy(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.DisableUnderlyingFocus && args.Count == 1)
		{
			DisableUnderlyingFocus(VariantUtils.ConvertTo<Node>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.FirstFocusable && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Control>(FirstFocusable(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.ShowTitle && args.Count == 0)
		{
			ShowTitle();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SharePair && args.Count == 2)
		{
			SharePair(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ShareRequestFinished && args.Count == 2)
		{
			ShareRequestFinished(VariantUtils.ConvertTo<bool>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.BuildTitle && args.Count == 0)
		{
			BuildTitle();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.InvokeAction && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<bool>(InvokeAction(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.SelectionTitle && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(SelectionTitle(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.Box && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(Box(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<Color>(in args[1]), VariantUtils.ConvertTo<int>(in args[2])));
			return true;
		}
		if (method == MethodName.Plain && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(Plain(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.Row && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<HBoxContainer>(Row(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.Column && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<VBoxContainer>(Column(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.ScrollColumn && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<VBoxContainer>(ScrollColumn(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.Flow && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<HFlowContainer>(Flow(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.CardColor && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Color>(CardColor(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.Paints && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<bool>(Paints(VariantUtils.ConvertTo<StyleBox>(in args[0])));
			return true;
		}
		if (method == MethodName.VisibleAlpha && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<float>(VisibleAlpha(VariantUtils.ConvertTo<CanvasItem>(in args[0])));
			return true;
		}
		if (method == MethodName.ViewportBounds && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Rect2>(ViewportBounds(VariantUtils.ConvertTo<Control>(in args[0])));
			return true;
		}
		if (method == MethodName.Set && args.Count == 2)
		{
			Set(VariantUtils.ConvertTo<Label>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.DisableUnderlyingFocus && args.Count == 1)
		{
			DisableUnderlyingFocus(VariantUtils.ConvertTo<Node>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.FirstFocusable && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Control>(FirstFocusable(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName.AdvancePlay)
		{
			return true;
		}
		if (method == MethodName.SelectionTitle)
		{
			return true;
		}
		if (method == MethodName.BeginEndTurn)
		{
			return true;
		}
		if (method == MethodName.FontSize)
		{
			return true;
		}
		if (method == MethodName._Ready)
		{
			return true;
		}
		if (method == MethodName.Bind)
		{
			return true;
		}
		if (method == MethodName.PresentationCompleted)
		{
			return true;
		}
		if (method == MethodName._ExitTree)
		{
			return true;
		}
		if (method == MethodName.SelectEnemy)
		{
			return true;
		}
		if (method == MethodName._UnhandledKeyInput)
		{
			return true;
		}
		if (method == MethodName.Rebuild)
		{
			return true;
		}
		if (method == MethodName.BuildHeader)
		{
			return true;
		}
		if (method == MethodName.Heading)
		{
			return true;
		}
		if (method == MethodName.CloseOverlay)
		{
			return true;
		}
		if (method == MethodName.CloseAllOverlays)
		{
			return true;
		}
		if (method == MethodName.BuildOverlay)
		{
			return true;
		}
		if (method == MethodName.OpenMenu)
		{
			return true;
		}
		if (method == MethodName.OpenKeywords)
		{
			return true;
		}
		if (method == MethodName.OpenJournal)
		{
			return true;
		}
		if (method == MethodName.OpenExport)
		{
			return true;
		}
		if (method == MethodName.OpenRetainedExport)
		{
			return true;
		}
		if (method == MethodName.OpenExportPair)
		{
			return true;
		}
		if (method == MethodName.CopyText)
		{
			return true;
		}
		if (method == MethodName.ExportFile)
		{
			return true;
		}
		if (method == MethodName.ImportFile)
		{
			return true;
		}
		if (method == MethodName.OpenImport)
		{
			return true;
		}
		if (method == MethodName.ImportText)
		{
			return true;
		}
		if (method == MethodName._Notification)
		{
			return true;
		}
		if (method == MethodName.HandleBack)
		{
			return true;
		}
		if (method == MethodName.ApplySafeArea)
		{
			return true;
		}
		if (method == MethodName.ReadSettings)
		{
			return true;
		}
		if (method == MethodName.ApplyAudio)
		{
			return true;
		}
		if (method == MethodName.SaveSettings)
		{
			return true;
		}
		if (method == MethodName.OpenSettings)
		{
			return true;
		}
		if (method == MethodName.ApplyTheme)
		{
			return true;
		}
		if (method == MethodName.Box)
		{
			return true;
		}
		if (method == MethodName.Plain)
		{
			return true;
		}
		if (method == MethodName.SizeInteractiveControls)
		{
			return true;
		}
		if (method == MethodName.Row)
		{
			return true;
		}
		if (method == MethodName.Column)
		{
			return true;
		}
		if (method == MethodName.ScrollColumn)
		{
			return true;
		}
		if (method == MethodName.Flow)
		{
			return true;
		}
		if (method == MethodName.Art)
		{
			return true;
		}
		if (method == MethodName.Picture)
		{
			return true;
		}
		if (method == MethodName.Tile)
		{
			return true;
		}
		if (method == MethodName.CardColor)
		{
			return true;
		}
		if (method == MethodName.EnemyVitalsText)
		{
			return true;
		}
		if (method == MethodName.EnemySurface)
		{
			return true;
		}
		if (method == MethodName._Process)
		{
			return true;
		}
		if (method == MethodName.PositionEnemyHud)
		{
			return true;
		}
		if (method == MethodName.OpenNote)
		{
			return true;
		}
		if (method == MethodName.OpenRating)
		{
			return true;
		}
		if (method == MethodName.Paints)
		{
			return true;
		}
		if (method == MethodName.VisibleAlpha)
		{
			return true;
		}
		if (method == MethodName.ViewportBounds)
		{
			return true;
		}
		if (method == MethodName.Set)
		{
			return true;
		}
		if (method == MethodName.CanDropHandCard)
		{
			return true;
		}
		if (method == MethodName.DropHandCard)
		{
			return true;
		}
		if (method == MethodName.PlayAllSparks)
		{
			return true;
		}
		if (method == MethodName.PumpSparks)
		{
			return true;
		}
		if (method == MethodName.ContinueFromTitle)
		{
			return true;
		}
		if (method == MethodName.Sound)
		{
			return true;
		}
		if (method == MethodName.OfferQueuedRating)
		{
			return true;
		}
		if (method == MethodName.InspectEnemy)
		{
			return true;
		}
		if (method == MethodName.DisableUnderlyingFocus)
		{
			return true;
		}
		if (method == MethodName.FirstFocusable)
		{
			return true;
		}
		if (method == MethodName.ShowTitle)
		{
			return true;
		}
		if (method == MethodName.SharePair)
		{
			return true;
		}
		if (method == MethodName.ShareRequestFinished)
		{
			return true;
		}
		if (method == MethodName.BuildTitle)
		{
			return true;
		}
		if (method == MethodName.InvokeAction)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.RejectedReceiptCount)
		{
			RejectedReceiptCount = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.PresentedNumericContactCount)
		{
			PresentedNumericContactCount = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._session)
		{
			_session = VariantUtils.ConvertTo<GameSession>(in value);
			return true;
		}
		if (name == PropertyName._body)
		{
			_body = VariantUtils.ConvertTo<VBoxContainer>(in value);
			return true;
		}
		if (name == PropertyName._shell)
		{
			_shell = VariantUtils.ConvertTo<VBoxContainer>(in value);
			return true;
		}
		if (name == PropertyName._overlayNode)
		{
			_overlayNode = VariantUtils.ConvertTo<Control>(in value);
			return true;
		}
		if (name == PropertyName._overlayTitle)
		{
			_overlayTitle = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._overlayCanClose)
		{
			_overlayCanClose = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._showTitle)
		{
			_showTitle = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._notice)
		{
			_notice = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._error)
		{
			_error = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._leaderId)
		{
			_leaderId = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._deckId)
		{
			_deckId = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._seed)
		{
			_seed = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._difficulty)
		{
			_difficulty = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._preferredTarget)
		{
			_preferredTarget = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._workshopA)
		{
			_workshopA = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._workshopB)
		{
			_workshopB = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._textScale)
		{
			_textScale = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._masterVolume)
		{
			_masterVolume = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._bgmVolume)
		{
			_bgmVolume = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._sfxVolume)
		{
			_sfxVolume = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._muted)
		{
			_muted = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._graphicsQuality)
		{
			_graphicsQuality = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._safeMargin)
		{
			_safeMargin = VariantUtils.ConvertTo<MarginContainer>(in value);
			return true;
		}
		if (name == PropertyName._handScroll)
		{
			_handScroll = VariantUtils.ConvertTo<ScrollContainer>(in value);
			return true;
		}
		if (name == PropertyName._noteDraft)
		{
			_noteDraft = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._hpReadout)
		{
			_hpReadout = VariantUtils.ConvertTo<Label>(in value);
			return true;
		}
		if (name == PropertyName._energyReadout)
		{
			_energyReadout = VariantUtils.ConvertTo<Label>(in value);
			return true;
		}
		if (name == PropertyName._blockReadout)
		{
			_blockReadout = VariantUtils.ConvertTo<Label>(in value);
			return true;
		}
		if (name == PropertyName._statusReadout)
		{
			_statusReadout = VariantUtils.ConvertTo<Label>(in value);
			return true;
		}
		if (name == PropertyName._costPresented)
		{
			_costPresented = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._sparksRemaining)
		{
			_sparksRemaining = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._pumpingSparks)
		{
			_pumpingSparks = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._ratingQueued)
		{
			_ratingQueued = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._shareAwaiting)
		{
			_shareAwaiting = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._reactionMode)
		{
			_reactionMode = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._setAnyCards)
		{
			_setAnyCards = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		string from;
		if (name == PropertyName.GraphicsQuality)
		{
			from = GraphicsQuality;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		bool from2;
		if (name == PropertyName.PhoneLayout)
		{
			from2 = PhoneLayout;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.MinimumHit)
		{
			value = VariantUtils.CreateFrom<float>(MinimumHit);
			return true;
		}
		if (name == PropertyName.IsInputBlocked)
		{
			from2 = IsInputBlocked;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.CurrentRoute)
		{
			from = CurrentRoute;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.ActiveModal)
		{
			from = ActiveModal;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		int from3;
		if (name == PropertyName.RejectedReceiptCount)
		{
			from3 = RejectedReceiptCount;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.PendingSparkPlays)
		{
			from3 = PendingSparkPlays;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.PresentedNumericContactCount)
		{
			from3 = PresentedNumericContactCount;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName._session)
		{
			value = VariantUtils.CreateFrom(in _session);
			return true;
		}
		if (name == PropertyName._body)
		{
			value = VariantUtils.CreateFrom(in _body);
			return true;
		}
		if (name == PropertyName._shell)
		{
			value = VariantUtils.CreateFrom(in _shell);
			return true;
		}
		if (name == PropertyName._overlayNode)
		{
			value = VariantUtils.CreateFrom(in _overlayNode);
			return true;
		}
		if (name == PropertyName._overlayTitle)
		{
			value = VariantUtils.CreateFrom(in _overlayTitle);
			return true;
		}
		if (name == PropertyName._overlayCanClose)
		{
			value = VariantUtils.CreateFrom(in _overlayCanClose);
			return true;
		}
		if (name == PropertyName._showTitle)
		{
			value = VariantUtils.CreateFrom(in _showTitle);
			return true;
		}
		if (name == PropertyName._notice)
		{
			value = VariantUtils.CreateFrom(in _notice);
			return true;
		}
		if (name == PropertyName._error)
		{
			value = VariantUtils.CreateFrom(in _error);
			return true;
		}
		if (name == PropertyName._leaderId)
		{
			value = VariantUtils.CreateFrom(in _leaderId);
			return true;
		}
		if (name == PropertyName._deckId)
		{
			value = VariantUtils.CreateFrom(in _deckId);
			return true;
		}
		if (name == PropertyName._seed)
		{
			value = VariantUtils.CreateFrom(in _seed);
			return true;
		}
		if (name == PropertyName._difficulty)
		{
			value = VariantUtils.CreateFrom(in _difficulty);
			return true;
		}
		if (name == PropertyName._preferredTarget)
		{
			value = VariantUtils.CreateFrom(in _preferredTarget);
			return true;
		}
		if (name == PropertyName._workshopA)
		{
			value = VariantUtils.CreateFrom(in _workshopA);
			return true;
		}
		if (name == PropertyName._workshopB)
		{
			value = VariantUtils.CreateFrom(in _workshopB);
			return true;
		}
		if (name == PropertyName._textScale)
		{
			value = VariantUtils.CreateFrom(in _textScale);
			return true;
		}
		if (name == PropertyName._masterVolume)
		{
			value = VariantUtils.CreateFrom(in _masterVolume);
			return true;
		}
		if (name == PropertyName._bgmVolume)
		{
			value = VariantUtils.CreateFrom(in _bgmVolume);
			return true;
		}
		if (name == PropertyName._sfxVolume)
		{
			value = VariantUtils.CreateFrom(in _sfxVolume);
			return true;
		}
		if (name == PropertyName._muted)
		{
			value = VariantUtils.CreateFrom(in _muted);
			return true;
		}
		if (name == PropertyName._graphicsQuality)
		{
			value = VariantUtils.CreateFrom(in _graphicsQuality);
			return true;
		}
		if (name == PropertyName._safeMargin)
		{
			value = VariantUtils.CreateFrom(in _safeMargin);
			return true;
		}
		if (name == PropertyName._handScroll)
		{
			value = VariantUtils.CreateFrom(in _handScroll);
			return true;
		}
		if (name == PropertyName._noteDraft)
		{
			value = VariantUtils.CreateFrom(in _noteDraft);
			return true;
		}
		if (name == PropertyName._hpReadout)
		{
			value = VariantUtils.CreateFrom(in _hpReadout);
			return true;
		}
		if (name == PropertyName._energyReadout)
		{
			value = VariantUtils.CreateFrom(in _energyReadout);
			return true;
		}
		if (name == PropertyName._blockReadout)
		{
			value = VariantUtils.CreateFrom(in _blockReadout);
			return true;
		}
		if (name == PropertyName._statusReadout)
		{
			value = VariantUtils.CreateFrom(in _statusReadout);
			return true;
		}
		if (name == PropertyName._costPresented)
		{
			value = VariantUtils.CreateFrom(in _costPresented);
			return true;
		}
		if (name == PropertyName._sparksRemaining)
		{
			value = VariantUtils.CreateFrom(in _sparksRemaining);
			return true;
		}
		if (name == PropertyName._pumpingSparks)
		{
			value = VariantUtils.CreateFrom(in _pumpingSparks);
			return true;
		}
		if (name == PropertyName._ratingQueued)
		{
			value = VariantUtils.CreateFrom(in _ratingQueued);
			return true;
		}
		if (name == PropertyName._shareAwaiting)
		{
			value = VariantUtils.CreateFrom(in _shareAwaiting);
			return true;
		}
		if (name == PropertyName._reactionMode)
		{
			value = VariantUtils.CreateFrom(in _reactionMode);
			return true;
		}
		if (name == PropertyName._setAnyCards)
		{
			value = VariantUtils.CreateFrom(in _setAnyCards);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		List<PropertyInfo> properties = new List<PropertyInfo>();
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._session, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._body, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._shell, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._overlayNode, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName._overlayTitle, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._overlayCanClose, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._showTitle, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName._notice, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._error, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName._leaderId, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName._deckId, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._seed, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._difficulty, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._preferredTarget, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._workshopA, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._workshopB, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._textScale, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._masterVolume, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._bgmVolume, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._sfxVolume, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._muted, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName._graphicsQuality, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._safeMargin, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.GraphicsQuality, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.PhoneLayout, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName.MinimumHit, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.IsInputBlocked, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.CurrentRoute, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._handScroll, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName._noteDraft, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._hpReadout, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._energyReadout, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._blockReadout, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._statusReadout, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._costPresented, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.ActiveModal, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.RejectedReceiptCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._sparksRemaining, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._pumpingSparks, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.PendingSparkPlays, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._ratingQueued, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.PresentedNumericContactCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._shareAwaiting, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName._reactionMode, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._setAnyCards, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		return properties;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.RejectedReceiptCount, Variant.From<int>(RejectedReceiptCount));
		info.AddProperty(PropertyName.PresentedNumericContactCount, Variant.From<int>(PresentedNumericContactCount));
		info.AddProperty(PropertyName._session, Variant.From(in _session));
		info.AddProperty(PropertyName._body, Variant.From(in _body));
		info.AddProperty(PropertyName._shell, Variant.From(in _shell));
		info.AddProperty(PropertyName._overlayNode, Variant.From(in _overlayNode));
		info.AddProperty(PropertyName._overlayTitle, Variant.From(in _overlayTitle));
		info.AddProperty(PropertyName._overlayCanClose, Variant.From(in _overlayCanClose));
		info.AddProperty(PropertyName._showTitle, Variant.From(in _showTitle));
		info.AddProperty(PropertyName._notice, Variant.From(in _notice));
		info.AddProperty(PropertyName._error, Variant.From(in _error));
		info.AddProperty(PropertyName._leaderId, Variant.From(in _leaderId));
		info.AddProperty(PropertyName._deckId, Variant.From(in _deckId));
		info.AddProperty(PropertyName._seed, Variant.From(in _seed));
		info.AddProperty(PropertyName._difficulty, Variant.From(in _difficulty));
		info.AddProperty(PropertyName._preferredTarget, Variant.From(in _preferredTarget));
		info.AddProperty(PropertyName._workshopA, Variant.From(in _workshopA));
		info.AddProperty(PropertyName._workshopB, Variant.From(in _workshopB));
		info.AddProperty(PropertyName._textScale, Variant.From(in _textScale));
		info.AddProperty(PropertyName._masterVolume, Variant.From(in _masterVolume));
		info.AddProperty(PropertyName._bgmVolume, Variant.From(in _bgmVolume));
		info.AddProperty(PropertyName._sfxVolume, Variant.From(in _sfxVolume));
		info.AddProperty(PropertyName._muted, Variant.From(in _muted));
		info.AddProperty(PropertyName._graphicsQuality, Variant.From(in _graphicsQuality));
		info.AddProperty(PropertyName._safeMargin, Variant.From(in _safeMargin));
		info.AddProperty(PropertyName._handScroll, Variant.From(in _handScroll));
		info.AddProperty(PropertyName._noteDraft, Variant.From(in _noteDraft));
		info.AddProperty(PropertyName._hpReadout, Variant.From(in _hpReadout));
		info.AddProperty(PropertyName._energyReadout, Variant.From(in _energyReadout));
		info.AddProperty(PropertyName._blockReadout, Variant.From(in _blockReadout));
		info.AddProperty(PropertyName._statusReadout, Variant.From(in _statusReadout));
		info.AddProperty(PropertyName._costPresented, Variant.From(in _costPresented));
		info.AddProperty(PropertyName._sparksRemaining, Variant.From(in _sparksRemaining));
		info.AddProperty(PropertyName._pumpingSparks, Variant.From(in _pumpingSparks));
		info.AddProperty(PropertyName._ratingQueued, Variant.From(in _ratingQueued));
		info.AddProperty(PropertyName._shareAwaiting, Variant.From(in _shareAwaiting));
		info.AddProperty(PropertyName._reactionMode, Variant.From(in _reactionMode));
		info.AddProperty(PropertyName._setAnyCards, Variant.From(in _setAnyCards));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.RejectedReceiptCount, out var _value_RejectedReceiptCount))
		{
			RejectedReceiptCount = _value_RejectedReceiptCount.As<int>();
		}
		if (info.TryGetProperty(PropertyName.PresentedNumericContactCount, out var _value_PresentedNumericContactCount))
		{
			PresentedNumericContactCount = _value_PresentedNumericContactCount.As<int>();
		}
		if (info.TryGetProperty(PropertyName._session, out var _value__session))
		{
			_session = _value__session.As<GameSession>();
		}
		if (info.TryGetProperty(PropertyName._body, out var _value__body))
		{
			_body = _value__body.As<VBoxContainer>();
		}
		if (info.TryGetProperty(PropertyName._shell, out var _value__shell))
		{
			_shell = _value__shell.As<VBoxContainer>();
		}
		if (info.TryGetProperty(PropertyName._overlayNode, out var _value__overlayNode))
		{
			_overlayNode = _value__overlayNode.As<Control>();
		}
		if (info.TryGetProperty(PropertyName._overlayTitle, out var _value__overlayTitle))
		{
			_overlayTitle = _value__overlayTitle.As<string>();
		}
		if (info.TryGetProperty(PropertyName._overlayCanClose, out var _value__overlayCanClose))
		{
			_overlayCanClose = _value__overlayCanClose.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._showTitle, out var _value__showTitle))
		{
			_showTitle = _value__showTitle.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._notice, out var _value__notice))
		{
			_notice = _value__notice.As<string>();
		}
		if (info.TryGetProperty(PropertyName._error, out var _value__error))
		{
			_error = _value__error.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._leaderId, out var _value__leaderId))
		{
			_leaderId = _value__leaderId.As<string>();
		}
		if (info.TryGetProperty(PropertyName._deckId, out var _value__deckId))
		{
			_deckId = _value__deckId.As<string>();
		}
		if (info.TryGetProperty(PropertyName._seed, out var _value__seed))
		{
			_seed = _value__seed.As<int>();
		}
		if (info.TryGetProperty(PropertyName._difficulty, out var _value__difficulty))
		{
			_difficulty = _value__difficulty.As<int>();
		}
		if (info.TryGetProperty(PropertyName._preferredTarget, out var _value__preferredTarget))
		{
			_preferredTarget = _value__preferredTarget.As<int>();
		}
		if (info.TryGetProperty(PropertyName._workshopA, out var _value__workshopA))
		{
			_workshopA = _value__workshopA.As<int>();
		}
		if (info.TryGetProperty(PropertyName._workshopB, out var _value__workshopB))
		{
			_workshopB = _value__workshopB.As<int>();
		}
		if (info.TryGetProperty(PropertyName._textScale, out var _value__textScale))
		{
			_textScale = _value__textScale.As<float>();
		}
		if (info.TryGetProperty(PropertyName._masterVolume, out var _value__masterVolume))
		{
			_masterVolume = _value__masterVolume.As<float>();
		}
		if (info.TryGetProperty(PropertyName._bgmVolume, out var _value__bgmVolume))
		{
			_bgmVolume = _value__bgmVolume.As<float>();
		}
		if (info.TryGetProperty(PropertyName._sfxVolume, out var _value__sfxVolume))
		{
			_sfxVolume = _value__sfxVolume.As<float>();
		}
		if (info.TryGetProperty(PropertyName._muted, out var _value__muted))
		{
			_muted = _value__muted.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._graphicsQuality, out var _value__graphicsQuality))
		{
			_graphicsQuality = _value__graphicsQuality.As<string>();
		}
		if (info.TryGetProperty(PropertyName._safeMargin, out var _value__safeMargin))
		{
			_safeMargin = _value__safeMargin.As<MarginContainer>();
		}
		if (info.TryGetProperty(PropertyName._handScroll, out var _value__handScroll))
		{
			_handScroll = _value__handScroll.As<ScrollContainer>();
		}
		if (info.TryGetProperty(PropertyName._noteDraft, out var _value__noteDraft))
		{
			_noteDraft = _value__noteDraft.As<string>();
		}
		if (info.TryGetProperty(PropertyName._hpReadout, out var _value__hpReadout))
		{
			_hpReadout = _value__hpReadout.As<Label>();
		}
		if (info.TryGetProperty(PropertyName._energyReadout, out var _value__energyReadout))
		{
			_energyReadout = _value__energyReadout.As<Label>();
		}
		if (info.TryGetProperty(PropertyName._blockReadout, out var _value__blockReadout))
		{
			_blockReadout = _value__blockReadout.As<Label>();
		}
		if (info.TryGetProperty(PropertyName._statusReadout, out var _value__statusReadout))
		{
			_statusReadout = _value__statusReadout.As<Label>();
		}
		if (info.TryGetProperty(PropertyName._costPresented, out var _value__costPresented))
		{
			_costPresented = _value__costPresented.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._sparksRemaining, out var _value__sparksRemaining))
		{
			_sparksRemaining = _value__sparksRemaining.As<int>();
		}
		if (info.TryGetProperty(PropertyName._pumpingSparks, out var _value__pumpingSparks))
		{
			_pumpingSparks = _value__pumpingSparks.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._ratingQueued, out var _value__ratingQueued))
		{
			_ratingQueued = _value__ratingQueued.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._shareAwaiting, out var _value__shareAwaiting))
		{
			_shareAwaiting = _value__shareAwaiting.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._reactionMode, out var _value__reactionMode))
		{
			_reactionMode = _value__reactionMode.As<string>();
		}
		if (info.TryGetProperty(PropertyName._setAnyCards, out var _value__setAnyCards))
		{
			_setAnyCards = _value__setAnyCards.As<bool>();
		}
	}
}
