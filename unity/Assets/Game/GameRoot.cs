// GameRoot.cs — 起動と画面の入口。シーンもプレハブも使わず、空シーンで Play すれば動く。
// エンジン (DeckRogue.Engine) は純ロジックのまま。ここは「状態を読んでコマンドを投げるだけ」の薄い層。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    /// <summary>カードをプレイする途中の状態 (追加コスト・選択・対象を順に集める)</summary>
    public class PendingPlay
    {
        public CardInstance Card;
        public int? ModeIndex;

        public int DiscardNeed;
        public int ExhaustNeed;
        public int DeckNeed;
        public int HandNeed;
        public bool NeedRetrieve;
        public bool NeedSacrifice;
        public bool NeedsTarget;
        public string DeckKind;

        public List<string> Discard = new List<string>();
        public List<string> Exhaust = new List<string>();
        public List<string> DeckSel = new List<string>();
        public List<string> HandSel = new List<string>();
        public string RetrieveUid;
        public string PermanentUid;
        public int? TargetIndex;

        /// <summary>次に集める必要があるもの。null = 揃った</summary>
        public string NextNeed()
        {
            if (Discard.Count < DiscardNeed) return "discard";
            if (Exhaust.Count < ExhaustNeed) return "exhaust";
            if (NeedRetrieve && RetrieveUid == null) return "retrieve";
            if (DeckSel.Count < DeckNeed) return "deck";
            if (HandSel.Count < HandNeed) return "hand";
            if (NeedSacrifice && PermanentUid == null) return "permanent";
            if (NeedsTarget && !TargetIndex.HasValue) return "target";
            return null;
        }
    }

    public class GameRoot : MonoBehaviour
    {
        public static GameRoot I;

        // ---- ラン状態 (エンジンの RunState。null = セットアップ画面) ----
        public RunState Rs;
        public string Error;
        public string Notice;

        // ---- セットアップ ----
        public string LeaderId = "leader_green";
        public int Seed = 1;
        public int Difficulty = DeckRogue.Engine.Run.DEFAULT_DIFFICULTY;
        TMP_InputField _seedField;

        // ---- 画面の一時状態 ----
        public PendingPlay Pending;
        public int PreferredTarget = -1;
        public int WorkshopA = -1;
        public int WorkshopB = -1;
        public string ShopMode;      // null / "remove" / "upgrade"
        public int EventChoiceIndex = -1;   // カード指定待ちの選択肢

        RectTransform _root;
        /// <summary>演出レイヤー (浮き文字など)。基準 1920×1080 の座標系・最前面・レイキャストを塞がない</summary>
        public RectTransform FxLayer;
        /// <summary>新画面 (M2 以降) の入れ物。キャンバス直下 1920×1080。旧画面の _root (1.5倍の入れ物) とは別</summary>
        public RectTransform ScreenRoot;
        /// <summary>戦闘ログの引き出しを開いているか</summary>
        public bool ShowLog;
        /// <summary>選択式カードのモード選択中 (手札の uid)</summary>
        public string ModeChoiceUid;
        /// <summary>山札/捨て札/消滅の一覧を開いているか ("draw"|"discard"|"exhaust"|null)</summary>
        public string ViewPile;
        /// <summary>ラン画面のデッキ一覧モーダル (M3)</summary>
        public bool ViewDeck;
        /// <summary>ラン画面の下位モード (焚き火の「鍛える」一覧など)。フェーズが変わると消える</summary>
        public string SubMode;
        /// <summary>戦闘の残留UI (敵・リーダーの入れ物と手札のカードを持ち越す)。戦闘を離れたら破棄</summary>
        public BattleView Battle;
        readonly Dictionary<string, RectTransform> _anchors = new Dictionary<string, RectTransform>();
        /// <summary>画面の組み立てが演出の的 (敵パネル・自分の欄) を登録する。Rebuild ごとに消える</summary>
        public void RegisterAnchor(string name, RectTransform rt) { _anchors[name] = rt; }
        public RectTransform Anchor(string name) { RectTransform rt; return _anchors.TryGetValue(name, out rt) && rt != null ? rt : null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("DeckRogue");
            DontDestroyOnLoad(go);
            go.AddComponent<GameRoot>();
        }

        void Awake()
        {
            I = this;
            Seed = UnityEngine.Random.Range(1, 99999);

            // Canvas
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 本家と同じ基準 (2026-09-07 裁定)
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // EventSystem (シーンに無ければ作る)
            if (EventSystem.current == null)
            {
                var esGo = new GameObject("EventSystem", typeof(EventSystem));
                esGo.transform.SetParent(transform, false);
#if ENABLE_INPUT_SYSTEM
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esGo.AddComponent<StandaloneInputModule>();
#endif
            }

            // 旧画面 (1280×720 で設計) は 1.5 倍の入れ物に組む＝見た目を変えずに基準を 1920×1080 へ移す。
            // M2 以降の新画面はこの入れ物を使わず、キャンバス直下 (1920×1080) に組む
            _root = UiKit.NewRect("root", canvasGo.transform);
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = new Vector2(1280f, 720f);
            _root.anchoredPosition = Vector2.zero;
            _root.localScale = Vector3.one * 1.5f;
            ScreenRoot = UiKit.NewRect("screen", canvasGo.transform);
            UiKit.Stretch(ScreenRoot, 0f, 0f, 0f, 0f);
            FxLayer = UiKit.NewRect("fx", canvasGo.transform);
            UiKit.Stretch(FxLayer, 0f, 0f, 0f, 0f);
            var fxCg = FxLayer.gameObject.AddComponent<CanvasGroup>();
            fxCg.blocksRaycasts = false;
            fxCg.interactable = false;

            try
            {
                if (!Content.IsLoaded) Content.Load(Path.Combine(Application.streamingAssetsPath, "data"));
            }
            catch (Exception ex)
            {
                Error = "データ読み込み失敗: " + ex.Message;
            }

            Rebuild();
        }

        // ---- コマンド ----

        public void Do(RunCommand cmd)
        {
            Error = null;
            Notice = null;
            bool wasCombat = Rs != null && Rs.Phase == RunPhases.Combat;
            try
            {
                Rs = DeckRogue.Engine.Run.ApplyRunCommand(Rs, cmd);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            Pending = null;
            ModeChoiceUid = null;
            ViewPile = null;
            ViewDeck = false;
            SubMode = null;
            if (wasCombat && Rs != null && Rs.Phase != RunPhases.Combat) Audio.Play(Rs.Phase == RunPhases.Lost ? "lose" : "win", 0.8f, 0f);
            // 画面をまたぐ一時選択は、その画面を離れたら捨てる (次に来た時に古い添字を使わない)
            if (Rs == null || Rs.Phase != RunPhases.Workshop) { WorkshopA = -1; WorkshopB = -1; }
            if (Rs == null || Rs.Phase != RunPhases.Shop) ShopMode = null;
            if (Rs == null || Rs.Phase != RunPhases.Event) EventChoiceIndex = -1;
            if (Rs == null || Rs.Phase != RunPhases.Combat) PreferredTarget = -1;
            // 演出キュー: 敵フェーズを含むコマンドは古い盤面の上で順に見せてから組み直す。それ以外は即組み直して差分を浮き文字に
            if (Rs != null && Rs.Combat != null && Rs.Phase == RunPhases.Combat && ScreenRoot != null && ScreenRoot.childCount > 0 && Presenter.HasEnemyPhase(Rs.Combat))
            {
                var snapshot = Rs.Combat;
                Presenter.PlaySequenced(this, snapshot, delegate { if (Rs != null && ReferenceEquals(Rs.Combat, snapshot)) Rebuild(); });
                return;
            }
            Rebuild();
            if (Rs != null && Rs.Combat != null && Rs.Phase == RunPhases.Combat) Presenter.Play(this, Rs.Combat); else Presenter.Reset();
        }

        public void DoCombat(Command cmd)
        {
            Do(new RunCommand_Combat { Command = cmd });
        }

        public void StartRun()
        {
            Error = null;
            Notice = null;
            Pending = null;
            PreferredTarget = -1;
            WorkshopA = -1;
            WorkshopB = -1;
            ShopMode = null;
            EventChoiceIndex = -1;
            try
            {
                if (_seedField != null)
                {
                    int parsed;
                    if (int.TryParse(_seedField.text, out parsed)) Seed = Mathf.Abs(parsed);
                }
                Rs = DeckRogue.Engine.Run.CreateRun(Seed, ReactionModes.SetConfirm, LeaderId, null, Difficulty, null);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Rs = null;
            }
            Rebuild();
        }

        public void BackToSetup()
        {
            Rs = null;
            Pending = null;
            Error = null;
            Notice = null;
            Seed = UnityEngine.Random.Range(1, 99999);
            Rebuild();
        }

        public void RegisterSeedField(TMP_InputField f) { _seedField = f; }

        /// <summary>シードを決めて入力欄にも反映する (StartRun は入力欄を優先して読むため)</summary>
        public void SetSeed(int seed)
        {
            Seed = Mathf.Abs(seed);
            if (_seedField != null) _seedField.SetTextWithoutNotify(Seed.ToString());
        }

        // ---- カードプレイの組み立て ----

        public void BeginPlay(CardInstance card, int? modeIndex)
        {
            var st = Rs != null ? Rs.Combat : null;
            if (st == null) return;
            var p = new PendingPlay();
            p.Card = card;
            p.ModeIndex = modeIndex;
            p.DiscardNeed = card.Def.DiscardCost.HasValue ? card.Def.DiscardCost.Value : 0;
            p.ExhaustNeed = card.Def.ExhaustCost.HasValue ? card.Def.ExhaustCost.Value : 0;

            for (int i = 0; i < card.Def.Effects.Count; i++)
            {
                var e = card.Def.Effects[i];
                if (e.Effect == "retrieveFromExhaust" || e.Effect == "playFromExhaust") p.NeedRetrieve = true;
                if (e.Effect == "sacrificeRetainer" && e.Trigger == "onPlay") p.NeedSacrifice = true;
            }

            p.DeckKind = Combat.DeckChooseKindOf(card.Def);
            if (p.DeckKind != null)
            {
                int want = 0;
                for (int i = 0; i < card.Def.Effects.Count; i++)
                {
                    var e = card.Def.Effects[i];
                    if (e.Effect == p.DeckKind) want += e.Amount.HasValue ? e.Amount.Value : 1;
                }
                p.DeckNeed = Math.Min(want, CombatScreen.DeckChoosePool(st, p.DeckKind).Count);
            }

            int upgradeN = 0;
            for (int i = 0; i < card.Def.Effects.Count; i++)
            {
                var e = card.Def.Effects[i];
                if (e.Effect == "upgradeInHand" && e.Trigger == "onPlay") upgradeN += e.Amount.HasValue ? e.Amount.Value : 1;
            }
            if (upgradeN > 0) p.HandNeed = Math.Min(upgradeN, CombatScreen.UpgradablePool(st, card).Count);

            int alive = 0;
            for (int i = 0; i < st.Enemies.Count; i++) if (st.Enemies[i].Hp > 0) alive++;
            p.NeedsTarget = alive > 1 && Effects.CardNeedsTarget(card, modeIndex);
            // 先に敵をクリックして狙いを付けてあれば、そのまま撃つ (二度手間にしない)
            if (p.NeedsTarget && PreferredTarget >= 0 && PreferredTarget < st.Enemies.Count && st.Enemies[PreferredTarget].Hp > 0)
            {
                p.TargetIndex = PreferredTarget;
            }

            Pending = p;
            SubmitIfReady();
        }

        public void SubmitIfReady()
        {
            if (Pending == null) return;
            if (Pending.NextNeed() != null) { Rebuild(); return; }

            var p = Pending;
            if (Battle != null) { Battle.LastPlayedUid = p.Card.Uid; Battle.LastPlayedTarget = p.TargetIndex.HasValue ? p.TargetIndex.Value : PreferredTarget; }
            var cmd = new Command_PlayCard
            {
                CardUid = p.Card.Uid,
                ModeIndex = p.ModeIndex,
                DiscardUids = p.Discard.Count > 0 ? p.Discard : null,
                ExhaustUids = p.Exhaust.Count > 0 ? p.Exhaust : null,
                DeckUids = p.DeckSel.Count > 0 ? p.DeckSel : null,
                HandUids = p.HandSel.Count > 0 ? p.HandSel : null,
                RetrieveUid = p.RetrieveUid,
                PermanentUid = p.PermanentUid,
                TargetIndex = p.TargetIndex,
            };
            Pending = null;
            DoCombat(cmd);
        }

        public void CancelPending()
        {
            Pending = null;
            Error = null;
            Rebuild();
        }

        public void OnEnemyClicked(int index)
        {
            if (Pending != null && Pending.NextNeed() == "target")
            {
                Pending.TargetIndex = index;
                SubmitIfReady();
                return;
            }
            PreferredTarget = index;
            Rebuild();
        }

        /// <summary>場面に合わせて BGM を切り替える (同じ名前なら何もしない)。素材は Resources/Audio/bgm/<name>、無ければ合成</summary>
        void UpdateBgm()
        {
            try
            {
                if (Rs == null) { Audio.Bgm("title"); return; }
                if (Rs.Phase == RunPhases.Combat)
                {
                    bool boss = false;
                    try { var node = DeckRogue.Engine.Run.CurrentNode(Rs); boss = node != null && node.Type == "boss"; } catch (Exception) { }
                    Audio.Bgm(boss ? "boss" + Rs.Act : "battle" + Rs.Act);
                    return;
                }
                if (Rs.Phase == RunPhases.Won) { Audio.Bgm("title"); return; }
                Audio.Bgm("map" + Rs.Act);
            }
            catch (Exception e) { Debug.LogWarning("[Audio] bgm: " + e.Message); }
        }

        // ---- 描画 ----

        public void Rebuild()
        {
            if (_root == null) return;
            _anchors.Clear();
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var c = _root.GetChild(i);
                c.SetParent(null, false);
                Destroy(c.gameObject);
            }
            bool inCombat = Content.IsLoaded && Rs != null && Rs.Phase == RunPhases.Combat && Rs.Combat != null;
            if (ScreenRoot != null && !inCombat)
            {
                if (Battle != null) { Battle.Destroy(); Battle = null; }
                Tooltip.Hide();
                if (FxLayer != null) for (int i = FxLayer.childCount - 1; i >= 0; i--) Destroy(FxLayer.GetChild(i).gameObject);
                for (int i = ScreenRoot.childCount - 1; i >= 0; i--)
                {
                    var c = ScreenRoot.GetChild(i);
                    c.SetParent(null, false);
                    Destroy(c.gameObject);
                }
            }
            try
            {
                BuildScreen();
            }
            catch (Exception ex)
            {
                var t = UiKit.Txt(_root, "描画エラー: " + ex.Message + "\n" + ex.StackTrace, 14, UiKit.ColBad);
                UiKit.Stretch(t.rectTransform, 12f, 12f, 12f, 12f);
            }
        }

        void BuildScreen()
        {
            UpdateBgm();
            // 戦闘は新画面 (M2): キャンバス直下 1920×1080 に組む。旧画面の入れ物には何も置かない
            if (Content.IsLoaded && Rs != null && Rs.Phase == RunPhases.Combat && Rs.Combat != null)
            {
                BattleScreen.Build(this, ScreenRoot);
                return;
            }
            // タイトルとマップも新画面 (M3)
            if (Content.IsLoaded && Rs == null) { TitleScreen.Build(this, ScreenRoot); return; }
            if (Content.IsLoaded && Rs.Phase == RunPhases.Map) { MapScreen.Build(this, ScreenRoot); if (ViewDeck) RunUi.DeckViewer(this, ScreenRoot); return; }
            if (Content.IsLoaded)
            {
                bool built = true;
                switch (Rs.Phase)
                {
                    case RunPhases.Reward: RewardScreen.Reward(this, ScreenRoot); break;
                    case RunPhases.RelicReward: RewardScreen.Relic(this, ScreenRoot); break;
                    case RunPhases.Campfire: CampfireScreen.Build(this, ScreenRoot); break;
                    case RunPhases.Workshop: WorkshopScreen.Build(this, ScreenRoot); break;
                    case RunPhases.Shop: ShopScreen.Build(this, ScreenRoot); break;
                    case RunPhases.Event: EventScreen.Build(this, ScreenRoot); break;
                    case RunPhases.Won: EndScreen.Build(this, ScreenRoot, true); break;
                    case RunPhases.Lost: EndScreen.Build(this, ScreenRoot, false); break;
                    default: built = false; break;
                }
                if (built)
                {
                    if (ViewDeck) RunUi.DeckViewer(this, ScreenRoot);   // 画面の上に重ねる (最後に組む)
                    return;
                }
            }
            var bg = UiKit.Pan(_root, UiKit.ColBg, "bg");
            bg.raycastTarget = false;
            UiKit.Stretch(bg.rectTransform, 0f, 0f, 0f, 0f);

            // 上部のメッセージ行 (エンジンの例外はプレイヤーへの説明文なのでそのまま出す)
            string msg = Error != null ? "! " + Error : (Notice != null ? Notice : "");
            var head = UiKit.Txt(_root, msg, 15, Error != null ? UiKit.ColBad : UiKit.ColDim);
            UiKit.Anchor(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -30f), new Vector2(-10f, -6f));

            var body = UiKit.NewRect("body", _root);
            UiKit.Stretch(body, 8f, 8f, 34f, 8f);

            if (!Content.IsLoaded)
            {
                var t = UiKit.Txt(body, "データが読み込めていません。\nunity/Assets/StreamingAssets/data/*.json を確認してください。", 18, UiKit.ColBad);
                UiKit.Stretch(t.rectTransform, 0f, 0f, 0f, 0f);
                return;
            }

            if (Rs == null) { RunScreens.Setup(this, body); return; }

            switch (Rs.Phase)
            {
                case RunPhases.Map: RunScreens.Map(this, body); break;
                case RunPhases.Combat: CombatScreen.Build(this, body); break;
                case RunPhases.Reward: RunScreens.Reward(this, body); break;
                case RunPhases.RelicReward: RunScreens.RelicReward(this, body); break;
                case RunPhases.Campfire: RunScreens.Campfire(this, body); break;
                case RunPhases.Workshop: RunScreens.Workshop(this, body); break;
                case RunPhases.Shop: RunScreens.Shop(this, body); break;
                case RunPhases.Event: RunScreens.EventRoom(this, body); break;
                case RunPhases.Won: RunScreens.Ended(this, body, true); break;
                case RunPhases.Lost: RunScreens.Ended(this, body, false); break;
                default:
                    UiKit.Txt(body, "未知のフェーズ: " + Rs.Phase, 18, UiKit.ColBad);
                    break;
            }
        }
    }
}
