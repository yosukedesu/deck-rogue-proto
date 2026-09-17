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

    /// <summary>確認ダイアログの中身 (UI 層)。OnOk で閉じた後の処理を行う</summary>
    public class ConfirmBox
    {
        public string Title;
        public string Message;
        public string OkLabel = "はい";
        public string CancelLabel = "キャンセル";
        /// <summary>取り消せない操作 (放棄など) は朱で</summary>
        public bool Danger;
        public Action OnOk;
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
        /// <summary>relic-choose (2026-09-12): 空の鳥籠/星読みの盤で選んだデッキの添字 (画面を離れたら捨てる)</summary>
        public List<int> RelicChoosePicks = new List<int>();
        public int EventChoiceIndex = -1;   // カード指定待ちの選択肢
        /// <summary>デッキの一覧 (RunUi.CardGrid) の「鍛えた後を見る」(2026-09-16 ユーザー「デッキ一覧すべてで鍛えた後を見るボタン」。本家 Smith の Show Upgrade)。ランの間は保つ</summary>
        public bool ShowUpgraded;
        /// <summary>スマホの一覧で押した札 (選ぶ→下の帯で確定。2026-09-16 案A)。鍵は画面ごと ("forge"/"remove"/"event") で、別の画面の一覧には効かない。手が通ると捨てる</summary>
        public string GridPickKey; public int GridPickIndex = -1;
        public int GridPick(string key) { return key != null && key == GridPickKey ? GridPickIndex : -1; }
        public void SetGridPick(string key, int index) { if (GridPickKey == key && GridPickIndex == index) { GridPickIndex = -1; return; } GridPickKey = key; GridPickIndex = index; }

        RectTransform _root;
        /// <summary>演出レイヤー (浮き文字など)。基準 1920×1080 の座標系・最前面・レイキャストを塞がない</summary>
        public RectTransform FxLayer;
        /// <summary>カードの拡大表示など、入力を受ける最前面の層 (FxLayer は raycast を通さないので別に持つ。開く時に最後尾へ回す)</summary>
        public RectTransform PopupLayer;
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
        /// <summary>マップの常時閲覧 (2026-09-12 ユーザー「マップは常に見れるようにして」): 上部バーの「マップ」で、どの画面の上にも読み取り専用の地図を重ねる</summary>
        public bool ViewMap;
        /// <summary>スマホの「≡」メニューを開いているか (2026-09-14)</summary>
        public bool MenuOpen;
        /// <summary>マップへの落書き (2026-09-12。StS2 の移植): 幕ごとの線の列。ランの間保持し、新しいランで白紙。UI 層の状態でエンジンには無い</summary>
        public Dictionary<int, List<DoodleStroke>> Doodles = new Dictionary<int, List<DoodleStroke>>();
        public bool DoodleMode;      // ペンボタンが押されている (左ドラッグ・指でも描ける)
        public int DoodlePen;        // 0=紙色 1=朱 2=消しゴム
        public List<DoodleStroke> DoodlesFor(int act) { List<DoodleStroke> l; if (!Doodles.TryGetValue(act, out l)) { l = new List<DoodleStroke>(); Doodles[act] = l; } return l; }
        /// <summary>ラン画面の下位モード (焚き火の「鍛える」一覧など)。フェーズが変わると消える</summary>
        public string SubMode;
        /// <summary>確認ダイアログ (2026-09-15 セーブ): ランの放棄・進行中のランを捨てて新しく始める・別のデータ版のセーブ。null = 出していない</summary>
        public ConfirmBox Confirm;
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

        /// <summary>コマンドラインの -uiscale N (PC でスマホの倍率を確かめる用)。無ければ 1</summary>
        static float UiScaleArg()
        {
            try
            {
                var args = System.Environment.GetCommandLineArgs();
                for (int i = 0; i + 1 < args.Length; i++)
                    if (args[i] == "-uiscale")
                    {
                        float v;
                        if (float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v)) return v;
                    }
            }
            catch (System.Exception) { }
            return 1f;
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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // 基準 1920×1080 (本家と同じ。2026-09-07 裁定)。スマホは 1.3倍 (2026-09-09 ユーザー裁定「スマホだけ 1.3倍」):
            // 高さ 831 を基準に全体を大きく描き、横長端末の余りは横に逃がす (S25 2340×1080 → 1800×831 のキャンバス)。PC で確かめる時は -uiscale 1.3
            // スマホは 1.6倍 (2026-09-14 ユーザー「字が小さい・アイコンも枠も小さい」→ 1.3倍から引き上げ。旧: 2026-09-09 裁定の 1.3倍)。
            // 1.6倍のキャンバスは 1200×675 (16:9) / 1462×675 (S25)。戦闘の絵は半分 (1ドット=2px) にして吹き出しが画面に収まる
            float ui = Application.isMobilePlatform ? 1.6f : UiScaleArg();
            UiKit.Phone = ui > 1.001f;
            if (ui > 1.001f)
            {
                scaler.referenceResolution = new Vector2(1920f / ui, 1080f / ui);
                scaler.matchWidthOrHeight = 1f;
            }
            else
            {
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }
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
            PopupLayer = UiKit.NewRect("popup", canvasGo.transform);
            UiKit.Stretch(PopupLayer, 0f, 0f, 0f, 0f);

            try
            {
                if (!Content.IsLoaded)
                {
                    // Android の APK 内は File で読めないので、Resources/Data の TextAsset (npm run unity:sync が複製) を優先する
                    if (Resources.Load<TextAsset>("Data/enemies") != null)
                        Content.LoadFrom(file =>
                        {
                            var name = file.EndsWith(".json") ? file.Substring(0, file.Length - 5) : file;
                            var ta = Resources.Load<TextAsset>("Data/" + name);
                            return ta != null ? ta.text : null;
                        });
                    else Content.Load(Path.Combine(Application.streamingAssetsPath, "data"));
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Error = "データ読み込み失敗: " + ex.Message;
            }

            Rebuild();
        }

        // ---- コマンド ----

        int _lastAct = -1;

        public void Do(RunCommand cmd)
        {
            Error = null;
            Notice = null;
            bool wasCombat = Rs != null && Rs.Phase == RunPhases.Combat;
            var prevRs = Rs;
            try
            {
                Rs = DeckRogue.Engine.Run.ApplyRunCommand(Rs, cmd);
                // フィードバックの記録 (2026-09-14): ジャーナル・選択履歴・戦闘の決着の保管。成功した手だけ
                try { Feedback.Record(prevRs, cmd, Rs); } catch (Exception fe) { Debug.LogWarning("[Feedback] record: " + fe.Message); }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            Pending = null;
            ModeChoiceUid = null;
            ViewPile = null;
            ViewDeck = false;
            ViewMap = false;
            SubMode = null;
            GridPickKey = null; GridPickIndex = -1;
            Feedback.MemoOpen = false;
            MenuOpen = false;
            Confirm = null;
            // 自動保存 (2026-09-15 本家形): 成功した手のたびに save/run.json を書く (別スレッド)。走破/敗北で終わったランは消す
            if (Rs != null && !ReferenceEquals(Rs, prevRs))
            {
                if (Rs.Phase == RunPhases.Won || Rs.Phase == RunPhases.Lost) SaveGame.Delete();
                else SaveGame.Write(this, Rs);
            }
            bool combatEnded = wasCombat && Rs != null && Rs.Phase != RunPhases.Combat;
            // 確認の窓 (発動/温存) から進んだ手: 順送りで古い盤面を見せる前に窓と暗がりだけ先に畳む (2026-09-17)
            bool fromWindow = wasCombat && prevRs != null && prevRs.Combat != null && prevRs.Combat.Phase == CombatPhases.AwaitingReaction && Rs != null && !ReferenceEquals(Rs, prevRs);
            int prevAct = _lastAct;
            if (Rs != null) _lastAct = Rs.Act;
            // 幕の切り替わり (2026-09-14 ユーザー「ステージ切り替わり時に SE が必要」): 幕ボス撃破後の次の幕へ / ランの開始
            if (Rs != null && prevAct != Rs.Act) Audio.Ui("act_start");
            // 戦闘の始まり: マップから戦闘へ (同上)
            if (!wasCombat && Rs != null && Rs.Phase == RunPhases.Combat) Audio.Ui("combat_start");
            // リーサル (2026-09-14 ユーザー「打撃 SE が鳴る前にピックに移ってしまい爽快感がなくなる」):
            // 決着した戦闘の最後の出来事 (打撃・撃破) を古い戦闘画面の上で見せ切ってから、報酬/敗北の画面へ組み直す
            if (combatEnded && Rs.Combat != null && ScreenRoot != null && ScreenRoot.childCount > 0 && Presenter.HasNewEvents(Rs.Combat))
            {
                var finalSnapshot = Rs.Combat;
                var endedRs = Rs;
                if (fromWindow && Battle != null) Battle.CloseReactionWindow();
                Presenter.PlaySequenced(this, finalSnapshot, delegate
                {
                    // 決着の余韻 (2026-09-17 ⑧): 勝利の帯と戦いの記録／敗北の暗転 (音もそこで鳴る)。終わってから報酬/敗北の画面へ組み直す
                    Presenter.ShowOutcome(this, endedRs, finalSnapshot, delegate
                    {
                        if (!ReferenceEquals(Rs, endedRs)) return;   // その間に別のコマンドが進んでいたら何もしない
                        Pending = null; ViewPile = null; ViewDeck = false; ViewMap = false; SubMode = null;
                        Rebuild();
                        Presenter.Reset();
                    });
                });
                return;
            }
            if (combatEnded) Audio.Ui(Rs.Phase == RunPhases.Lost ? "lose" : "win");
            // 画面をまたぐ一時選択は、その画面を離れたら捨てる (次に来た時に古い添字を使わない)
            if (Rs == null || Rs.Phase != RunPhases.Workshop) { WorkshopA = -1; WorkshopB = -1; }
            if (Rs == null || Rs.Phase != RunPhases.Shop) ShopMode = null;
            if (Rs == null || Rs.Phase != RunPhases.RelicChoose) RelicChoosePicks.Clear();
            if (Rs == null || Rs.Phase != RunPhases.Event) EventChoiceIndex = -1;
            if (Rs == null || Rs.Phase != RunPhases.Combat) PreferredTarget = -1;
            // 演出キュー: 敵フェーズを含むコマンドは古い盤面の上で順に見せてから組み直す。それ以外は即組み直して差分を浮き文字に
            if (Rs != null && Rs.Combat != null && Rs.Phase == RunPhases.Combat && ScreenRoot != null && ScreenRoot.childCount > 0 && Presenter.HasEnemyPhase(Rs.Combat))
            {
                var snapshot = Rs.Combat;
                if (fromWindow && Battle != null) Battle.CloseReactionWindow();
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
            Doodles = new Dictionary<int, List<DoodleStroke>>(); DoodleMode = false; DoodlePen = 0;   // 落書きは新しいランで白紙 (ランの間は保持)
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
                _lastAct = Rs.Act;
                Audio.Ui("act_start");
                // フィードバックの記録を白紙に (ジャーナルの origin = リプレイの起点)
                Feedback.BeginRun(new ReplayOrigin { Kind = "run", Seed = Seed, LeaderId = LeaderId, Difficulty = Difficulty });
                // 進行中のランは1本 (2026-09-15): 前のセーブは消して、開始直後の状態から自動保存を始める
                SaveGame.Delete();
                SaveGame.Write(this, Rs);
                Confirm = null;
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
            // 走破/敗北で終わったランのセーブは残さない (進行中のランだけが「続きから」に出る。2026-09-15)
            if (Rs != null && (Rs.Phase == RunPhases.Won || Rs.Phase == RunPhases.Lost)) SaveGame.Delete();
            Rs = null;
            Pending = null;
            Error = null;
            Notice = null;
            Confirm = null;
            MenuOpen = false;
            Seed = UnityEngine.Random.Range(1, 99999);
            Rebuild();
        }

        // ---- セーブ/続きから (2026-09-15 本家形: 進行中のランは1本・自動保存) ----

        /// <summary>「セーブして終了」: 最新の状態を同期で書いてからタイトルへ (自動保存なので押さなくても残るが、押した安心のために今書く)</summary>
        public void SaveAndQuit()
        {
            if (Rs == null) { BackToSetup(); return; }
            SaveGame.Flush(this, Rs);
            Feedback.Autosave(Rs);
            BackToSetup();
            Notice = "セーブした。タイトルの「続きから」で再開できる";
            Rebuild();
        }

        /// <summary>「ランを放棄」: 確認の後にセーブを消してタイトルへ (レポートの自動保存 = データ回収用は残す)</summary>
        public void AskAbandonRun()
        {
            Confirm = new ConfirmBox
            {
                Title = "ランを放棄する？",
                Message = "このランのセーブは消え、続きから再開できなくなります。\n（レポートの自動保存は残るので、タイトルから書き出せます）",
                OkLabel = "放棄する",
                Danger = true,
                OnOk = delegate
                {
                    if (Rs != null) Feedback.Autosave(Rs);
                    SaveGame.Delete();
                    BackToSetup();
                    Notice = "ランを放棄した";
                    Rebuild();
                },
            };
            MenuOpen = false;
            Rebuild();
        }

        /// <summary>タイトルの「放棄」: セーブを消す (確認つき。ランは開いていない)</summary>
        public void AskAbandonSave()
        {
            Confirm = new ConfirmBox
            {
                Title = "セーブを消す？",
                Message = "進行中のランのセーブを消します。続きから再開できなくなります。\n（レポートの自動保存は残ります）",
                OkLabel = "消す",
                Danger = true,
                OnOk = delegate { SaveGame.Delete(); Notice = "セーブを消した"; Rebuild(); },
            };
            Rebuild();
        }

        /// <summary>タイトルの「ランを開始」: 進行中のセーブがあれば「捨てて始める」の確認を挟む (本家形)</summary>
        public void AskStartRun()
        {
            if (!SaveGame.Exists) { StartRun(); return; }
            string sum = null;
            try { sum = SaveGame.PeekSummary(); } catch (Exception) { }
            Confirm = new ConfirmBox
            {
                Title = "進行中のランを捨てて始める？",
                Message = "セーブがあります: " + (sum ?? "(読めないセーブ)") + "\n新しいランを始めると、このセーブは消えます。",
                OkLabel = "捨てて始める",
                Danger = true,
                OnOk = delegate { StartRun(); },
            };
            Rebuild();
        }

        /// <summary>タイトルの「続きから」: セーブを読んで再開する。データ指紋が違えば警告して選ばせる (ブラウザ版と同じ)</summary>
        public void ResumeSave()
        {
            Error = null; Notice = null;
            string warning;
            RunSaveFile sf = null;
            try { sf = SaveGame.Load(out warning); }
            catch (Exception e) { warning = e.Message; }
            if (sf == null || sf.Run == null)
            {
                Error = "セーブを読めなかった: " + warning;
                Rebuild();
                return;
            }
            string fp = null;
            try { fp = Report.DataFingerprint(); } catch (Exception) { }
            if (!string.IsNullOrEmpty(sf.Fingerprint) && fp != null && sf.Fingerprint != fp)
            {
                var captured = sf;
                Confirm = new ConfirmBox
                {
                    Title = "別のデータ版のセーブ",
                    Message = "このセーブは別のデータバージョンで作られています。カード・敵の定義が変わっていると正しく動かない可能性がありますが、読み込みますか？",
                    OkLabel = "読み込む",
                    OnOk = delegate { ApplySave(captured, warning); },
                };
                Rebuild();
                return;
            }
            ApplySave(sf, warning);
        }

        void ApplySave(RunSaveFile sf, string warning)
        {
            Doodles = SaveGame.DoodlesFromToken(sf.DoodlesUnity); DoodleMode = false; DoodlePen = 0;
            Pending = null; PreferredTarget = -1; WorkshopA = -1; WorkshopB = -1; ShopMode = null; EventChoiceIndex = -1;
            RelicChoosePicks.Clear(); ViewPile = null; ViewDeck = false; ViewMap = false; ShowLog = false; SubMode = null; MenuOpen = false; Confirm = null;
            Feedback.Restore(sf);
            Rs = sf.Run;
            _lastAct = Rs.Act;
            LeaderId = Rs.LeaderId;
            Difficulty = Rs.Difficulty > 0 ? Rs.Difficulty : Difficulty;
            SetSeed(Rs.Seed);
            if (Battle != null) { Battle.Destroy(); Battle = null; }
            // 戦闘の途中なら、読み戻したログは演出済みに (再開の一発目に古い浮き文字を出さない)
            if (Rs.Combat != null && Rs.Phase == RunPhases.Combat) Presenter.MarkSeen(Rs.Combat); else Presenter.Reset();
            Notice = warning != null ? "続きから再開した（" + warning + "）" : "続きから再開した";
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
                // 場面名 → 表 (Resources/Audio/audio.json) → 素材名。elite/rest/lost は表に無ければ幕の曲・マップの曲へ落ちる
                if (Rs == null) { Audio.Bgm(Audio.BgmFor("title")); return; }
                if (Rs.Phase == RunPhases.Combat)
                {
                    string nodeType = null;
                    try { var node = DeckRogue.Engine.Run.CurrentNode(Rs); nodeType = node != null ? node.Type : null; } catch (Exception) { }
                    // 本家形: 通常戦闘は幕の探索曲が流れ続ける。専用曲はエリートとボスだけ (2026-09-14 ユーザー裁定)
                    string act = Audio.BgmFor("act" + Rs.Act) ?? Audio.BgmFor("map" + Rs.Act);
                    string name = nodeType == "boss" ? (Audio.BgmFor("boss" + Rs.Act) ?? act)
                        : nodeType == "elite" ? (Audio.BgmFor("elite") ?? act)
                        : act;
                    Audio.Bgm(name);
                    return;
                }
                if (Rs.Phase == RunPhases.Won) { Audio.Bgm(Audio.BgmFor("won") ?? Audio.BgmFor("title")); return; }
                if (Rs.Phase == RunPhases.Lost) { Audio.Bgm(Audio.BgmFor("lost") ?? Audio.BgmFor("map" + Rs.Act)); return; }
                // 本家形 (2026-09-14 ユーザー「本家と同じように BGM をコロコロ変えない」): 戦闘以外は幕の探索曲 1 本が
                // マップ・報酬・焚き火・店・イベント・工房を通して流れ続ける (同じ名前なら Audio.Bgm は何もしない = 途切れない)
                Audio.Bgm(Audio.BgmFor("act" + Rs.Act) ?? Audio.BgmFor("map" + Rs.Act));
            }
            catch (Exception e) { Debug.LogWarning("[Audio] bgm: " + e.Message); }
        }

        void Update()
        {
            if (UiKit.Phone) Tooltip.Tick();   // タップで開いた説明を、外を触ったら閉じる (2026-09-14)
        }

        // ---- 落ちても失わない (2026-09-14): バックグラウンドへ回る/終了する時に自動保存 ----

        void OnApplicationPause(bool pause)
        {
            if (pause && Rs != null) { SaveGame.Flush(this, Rs); Feedback.Autosave(Rs); }
        }

        void OnApplicationQuit()
        {
            if (Rs != null) { SaveGame.Flush(this, Rs); Feedback.Autosave(Rs); }
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
            // 戦闘中は BattleView が差分更新するので掃除しない。ただし戦闘の最初の組み立て (Battle がまだ無い) は
            // 直前の画面 (マップ等) が残っているので掃除する (2026-09-07 舞台化で背景が UI 側から消え、残骸が透けた)
            bool freshCombat = inCombat && Battle == null;
            if (ScreenRoot != null && (!inCombat || freshCombat))
            {
                if (Battle != null) { Battle.Destroy(); Battle = null; }
                Tooltip.Hide();
                CardPopup.Close();
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
                // ログにも出す (ErrorOverlay が実機で最前面に貼る。2026-09-14 APK が真っ暗＝この文字が 1.3倍の入れ物の外に出ていた)
                Debug.LogException(ex);
                var t = UiKit.Txt(ScreenRoot != null ? ScreenRoot : _root, "描画エラー: " + ex.Message + "\n" + ex.StackTrace, 14, UiKit.ColBad);
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
                // 重ねる物は BattleView の UI 層へ (2026-09-14 ユーザー「戦闘中にマップ開いたら閉じるボタン押してもマップが閉じない」:
                // 戦闘中の Rebuild は ScreenRoot を掃除しない = ScreenRoot 直下に置いた地図が残っていた。UI 層は毎回 ClearUi で消える)
                var over = Battle != null && Battle.UiLayer != null ? Battle.UiLayer : ScreenRoot;
                if (ViewMap) MapScreen.Overlay(this, over);
                else if (ViewDeck) RunUi.DeckViewer(this, over);   // ≡ の「デッキ一覧」(2026-09-16)。他の画面と同じく地図が優先
                if (Feedback.MemoOpen) FeedbackUi.MemoDialog(this, over);
                if (MenuOpen) RunUi.Menu(this, over);
                if (Confirm != null) RunUi.ConfirmDialog(this, over);
                return;
            }
            // タイトルとマップも新画面 (M3)
            if (Content.IsLoaded && Rs == null) { TitleScreen.Build(this, ScreenRoot); if (Confirm != null) RunUi.ConfirmDialog(this, ScreenRoot); return; }
            if (Content.IsLoaded && Rs.Phase == RunPhases.Map) { MapScreen.Build(this, ScreenRoot); if (ViewDeck) RunUi.DeckViewer(this, ScreenRoot); if (Feedback.MemoOpen) FeedbackUi.MemoDialog(this, ScreenRoot); if (MenuOpen) RunUi.Menu(this, ScreenRoot); if (Confirm != null) RunUi.ConfirmDialog(this, ScreenRoot); return; }
            if (Content.IsLoaded)
            {
                bool built = true;
                switch (Rs.Phase)
                {
                    case RunPhases.Reward: RewardScreen.Reward(this, ScreenRoot); break;
                    case RunPhases.RelicReward: RewardScreen.Relic(this, ScreenRoot); break;
                    case RunPhases.RelicChoose: RewardScreen.RelicChoose(this, ScreenRoot); break;
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
                    FeedbackUi.RateButton(this, ScreenRoot);                 // 決着直後のフェーズだけ「評価」を直せる
                    if (ViewMap) MapScreen.Overlay(this, ScreenRoot);      // 読み取り専用の地図を重ねる
                    else if (ViewDeck) RunUi.DeckViewer(this, ScreenRoot);   // 画面の上に重ねる (最後に組む)
                    if (Feedback.ShouldShowRating(Rs)) FeedbackUi.RatingDialog(this, ScreenRoot);   // 戦闘直後の評価 (1回だけ聞く)
                    if (Feedback.MemoOpen) FeedbackUi.MemoDialog(this, ScreenRoot);
                    if (MenuOpen) RunUi.Menu(this, ScreenRoot);
                    if (Confirm != null) RunUi.ConfirmDialog(this, ScreenRoot);
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
