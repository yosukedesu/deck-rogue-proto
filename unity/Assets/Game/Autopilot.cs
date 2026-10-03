// Autopilot.cs — 自動操縦スクショ (2026-09-07 M1「目を作る」)。
// プレイヤーを `DeckRogue.exe -autopilot tour -shots <dir>` で起動すると、GameRoot の公開 API で画面を進めながら
// 各画面の PNG を <dir> に書き、終わったら終了する。WSL 側 (scripts/unity-win.sh shots) が PNG を回収して
// Claude Code が Read で見る＝「画面を確認できる目」。エンジンには触れない (状態を読んでコマンドを投げるだけ)。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public class Autopilot : MonoBehaviour
    {
        /// <summary>起動引数 name の次の値 (無ければ null)。計測用の APK では引数ファイル perf-args.txt の中身も読む (CommandLine)</summary>
        internal static string Arg(string name)
        {
            var args = CommandLine();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        // ---- 起動引数と引数ファイル (2026-09-30 HD-2D 見本 P01) ----
        // 計測用の APK (applicationId が「.perf」で終わる) か、起動引数に -perfprobe がある時は、persistentDataPath/perf-args.txt の中身を
        // 起動引数の後ろに足す (Android の intent の -e unity で引数が届かない時の逃げ道。adb push で置く)。
        // 書き方: 空白で区切った起動引数そのもの (「"」で囲めば空白を含められる。# で始まる行は読み飛ばす)。例:
        //   -autopilot state -state "phase=combat;enemy=enemy_wolf;perf=900" -perfprobe -hd2d hd2d=slice,tier=phone
        // 普段の起動 (PC・通常の APK) では読まない = 置き忘れたファイルで勝手に自動操縦が始まらない
        static string[] _cmdLine;
        /// <summary>起動引数 (＋引数ファイル)。最初に呼んだ時に1回だけ作る</summary>
        static string[] CommandLine()
        {
            if (_cmdLine != null) return _cmdLine;
            var list = new List<string>(Environment.GetCommandLineArgs());
            try
            {
                bool perfBuild = (Application.identifier ?? "").EndsWith(".perf", StringComparison.Ordinal);
                if (perfBuild || list.Contains("-perfprobe"))
                {
                    var path = Path.Combine(Application.persistentDataPath, "perf-args.txt");
                    if (File.Exists(path))
                    {
                        var extra = new List<string>();
                        foreach (var line in File.ReadAllLines(path))
                        {
                            var t = line.Trim();
                            if (t.Length == 0 || t.StartsWith("#")) continue;
                            extra.AddRange(SplitArgs(t));
                        }
                        list.AddRange(extra);
                        Debug.Log("[Autopilot] 引数ファイル " + path + " から " + extra.Count + " 個の引数を足した");
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning("[Autopilot] 引数ファイルを読めない: " + e.Message); }
            _cmdLine = list.ToArray();
            return _cmdLine;
        }

        /// <summary>1行を空白で区切る (「"…"」は1つの引数)</summary>
        static List<string> SplitArgs(string line)
        {
            var r = new List<string>();
            var sb = new System.Text.StringBuilder();
            bool quoted = false, any = false;
            foreach (char c in line)
            {
                if (c == '"') { quoted = !quoted; any = true; continue; }
                if (!quoted && char.IsWhiteSpace(c)) { if (any) { r.Add(sb.ToString()); sb.Length = 0; any = false; } continue; }
                sb.Append(c); any = true;
            }
            if (any) r.Add(sb.ToString());
            return r;
        }

        // ---- 決定的な撮影 (-det。2026-09-30 HD-2D 見本 P00) ----
        // 同じ STATE を2回撮って画素まで一致させるための起動引数。見た目は変えず、時間と乱数だけを固定する:
        //   ・最初のフレームの前から Time.captureFramerate=60 (1フレーム=1/60秒。演出・呼吸・粒・シェーダの _Time が実時間に左右されない)。
        //     コマ送りの撮影 (play= 等) の後も 0 へ戻さず 60 のまま
        //   ・UnityEngine.Random.InitState(20260930) (演出の火花・音の揺らぎ・揺れの向き)
        //   ・Run の最初の待ちと演出の終わりの待ちはフレーム数で数える
        //   ・状態へ跳んだ後、舞台の粒 (ParticleSystem) を種を固定して頭から再生し、90フレーム待ってから先へ進む
        // STATE に det=1 と書いても同じ (-state の中身を起動の前に読む)。既存のキーの意味は変えない
        /// <summary>-det で起動した (決定的な撮影)</summary>
        public static bool Det { get; private set; }
        const int DetFps = 60;
        const int DetRandomSeed = 20260930;
        const int DetSettleFrames = 90;
        /// <summary>撮影の後に戻す captureFramerate (det なら 60 のまま・それ以外は 0 = 実時間)</summary>
        static int DetFramerate => Det ? DetFps : 0;

        static bool HasFlag(string name) => CommandLine().Any(a => a == name);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void DetBoot()
        {
            var st = Arg("-state") ?? "";
            bool det = HasFlag("-det") || st.Split(';').Any(p => p.Trim().ToLowerInvariant() == "det=1");
            if (!det) return;
            EnableDet();
        }

        /// <summary>
        /// 決定的な撮影を立てる (起動引数 -det・STATE の det=1・HD2DFlags.Det=true)。2回目以降は何もしない。
        /// 最初のフレームの前 (DetBoot) に呼ばれるのが本来の形。途中 (-statesfile の行の det=1) で立てた時はその時から固定される
        /// </summary>
        internal static void EnableDet()
        {
            if (Det) return;
            Det = true;
            Time.captureFramerate = DetFps;   // DetBoot から = シーンの読み込みより前 = 最初のフレームの前
            UnityEngine.Random.InitState(DetRandomSeed);
            Debug.Log("[Autopilot] det: captureFramerate=" + DetFps + " seed=" + DetRandomSeed);
        }

        void Awake()
        {
            if (Det) Time.captureFramerate = DetFps;   // 念のため (Awake も最初のフレームの前)
        }

        bool _detPointerOff;
        /// <summary>
        /// det: 本物のマウスを読まない = EventSystem の入力モジュールを止める (2026-09-30 HD-2D 見本 W2 の統合)。
        /// 並列の撮影 (scripts/pshots.sh) でプレイヤーの窓がデスクトップのマウスの下に開くと、手札の札がホバーで持ち上がり、同じ STATE でも撮るたびに画が変わった。
        /// 自動操縦の押す・ドラッグは ExecuteEvents で直に叩くので、モジュールが止まっていても動く
        /// </summary>
        void Update()
        {
            if (!Det) return;
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return;
            foreach (var m in es.GetComponents<UnityEngine.EventSystems.BaseInputModule>())
            {
                if (!m.enabled) continue;
                m.enabled = false;
                if (!_detPointerOff) { _detPointerOff = true; Debug.Log("[Autopilot] det: マウスを読まない (入力モジュール " + m.GetType().Name + " を止めた)"); }
            }
        }

        /// <summary>det: 舞台の粒を種を固定して頭から再生し、90フレーム待つ (粒の数と位置を撮るたびに同じにする)</summary>
        IEnumerator DetSettleStage()
        {
            if (!Det) yield break;
            var all = FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include)
                .Select(ps => new { ps, path = HierarchyPath(ps.transform) })
                .OrderBy(x => x.path, StringComparer.Ordinal).ToList();
            foreach (var x in all) x.ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            for (int i = 0; i < all.Count; i++)
            {
                var ps = all[i].ps;
                ps.useAutoRandomSeed = false;
                ps.randomSeed = (uint)(DetRandomSeed + 7919 * (i + 1));
            }
            foreach (var x in all) if (x.ps.gameObject.activeInHierarchy) x.ps.Play(false);
            Debug.Log("[Autopilot] det: 舞台の粒 " + all.Count + " 個を種を固定して頭から再生");
            for (int i = 0; i < DetSettleFrames; i++) yield return null;
        }

        static string HierarchyPath(Transform t)
        {
            var sb = new System.Text.StringBuilder();
            for (; t != null; t = t.parent) sb.Insert(0, "/" + t.GetSiblingIndex().ToString("0000") + ":" + t.name);
            return sb.ToString();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var scenario = Arg("-autopilot");
            if (string.IsNullOrEmpty(scenario) && !string.IsNullOrEmpty(Arg("-statesfile"))) scenario = "states";   // -statesfile だけでも自動操縦で撮る (P01)
            if (string.IsNullOrEmpty(scenario)) return;
            var go = new GameObject("Autopilot");
            DontDestroyOnLoad(go);
            var a = go.AddComponent<Autopilot>();
            a._scenario = scenario;
            a._dir = Arg("-shots") ?? Path.Combine(Application.persistentDataPath, "shots");
            a._seed = int.TryParse(Arg("-seed") ?? "", out var s) ? s : 4242;
        }

        string _scenario;
        string _dir;
        int _seed;
        int _n;
        string _stateSpec;    // 今撮っている STATE (layout.json の "state")
        string _linePrefix;   // -statesfile の行の名前 (null = 1行1起動の普通の名前 NN-name.png)
        int _lineN;           // その行で撮った枚数
        int _diff0;           // -statesfile を始めた時の難易度 (行の頭で戻す)

        void Start()
        {
            Directory.CreateDirectory(_dir);
            // 前回のスクショのセーブが残っているとタイトルに「続きから」が出る (2026-09-15)。撮る時は白紙から。
            // 計測 (-perfprobe) の時は消さない (計測用の APK・PC でユーザーのセーブを消さない。2026-09-30 HD-2D 見本 P01)
            if (!HasFlag("-perfprobe")) SaveGame.Delete();
            else Debug.Log("[Autopilot] -perfprobe: セーブは消さない");
            StartCoroutine(Run());
        }

        /// <summary>順送りの演出 (Presenter.PlaySequenced の入力ブロック) が終わるまで待つ (最大6秒)</summary>
        IEnumerator WaitPresentation()
        {
            float t0 = Time.realtimeSinceStartup;
            int frames = 0;   // det では実時間でなくフレーム数で打ち切る (6秒 = 360フレーム)
            while (GameObject.Find("inputblock") != null && (Det ? frames++ < 6 * DetFps : Time.realtimeSinceStartup - t0 < 6f)) yield return null;
        }

        IEnumerator Shot(string name, int settle = 6)
        {
            // レイアウトと描画が落ち着くまで数フレーム待つ (Rebuild 直後の1フレームは LayoutGroup が未計算)。コマ送りの途中を撮る時は settle=1
            ApplyCaptureMode();   // uionly / unitsonly (立っていなければ何もしない。2026-09-30 HD-2D 見本 P01)
            for (int i = 0; i < settle; i++) yield return null;
            ApplyCaptureMode();   // 待つ間の組み直し (幕の描き直し) で戻された値を、撮る直前にもう一度
            _n++;
            // 1回の起動で順に撮る (-statesfile) 時は「行の名前-その行の何枚目.png」(1行1起動で撮って shoot.sh が付け直す名前と同じ)
            string file;
            if (_linePrefix != null) { _lineN++; file = _linePrefix + "-" + _lineN + ".png"; }
            else file = $"{_n:00}-{name}.png";
            var path = Path.Combine(_dir, file);
            ScreenCapture.CaptureScreenshot(path, 1);
            Debug.Log("[Autopilot] shot " + path + " ut=" + Time.unscaledTime.ToString("F2") + " player=" + Stage.DebugAnim("player"));
            if (HD2DFlags.DumpLayout) LayoutDump.Write(this, path, name);   // dumplayout=1: 同じ名前の .layout.json (撮るのと同じフレームの矩形)
            for (int i = 0; i < 3; i++) yield return null;
        }

        IEnumerator Run()
        {
            // GameRoot の起動を待つ
            float t0 = Time.realtimeSinceStartup;
            while (GameRoot.I == null && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            if (Det) { for (int i = 0; i < DetFps / 2; i++) yield return null; }   // det: 0.5秒をフレーム数で
            else yield return new WaitForSeconds(0.5f);
            var g = GameRoot.I;
            if (g == null) { Debug.LogError("[Autopilot] GameRoot が起動しない"); Application.Quit(1); yield break; }
            try
            {
                var statesFile = Arg("-statesfile");
                if (!string.IsNullOrEmpty(statesFile)) _scenario = "states";   // -autopilot state -statesfile <file> でも 1回の起動で順に撮る
                switch (_scenario)
                {
                    case "states": yield return StatesFile(g, statesFile); break;   // 1回の起動で順に撮る (まとめて撮る専用。合否の門には使わない。2026-09-30 P01)
                    case "battle":
                        yield return Battle(g);
                        break;
                    case "run": yield return RunTour(g); break;
                    case "workshop": yield return WorkshopOnly(g); break;   // 工房だけ (⭐レシピの提示の確認。2026-09-12)
                    case "state": yield return StateJump(g, Arg("-state") ?? ""); break;   // 任意の状態へ跳んで撮る (2026-09-12)
                    case "tour":
                    default:
                        yield return Tour(g);
                        break;
                }
            }
            finally
            {
                Debug.Log($"[Autopilot] 終了 shots={_n} dir={_dir}");
            }
            yield return null;
            Application.Quit(0);
        }

        /// <summary>戦闘画面の状態を一通り撮る: 素・手札ホバー・ログ・モード選択・対象選択・伏せ→確認ウィンドウ・ターン後</summary>
        IEnumerator Battle(GameRoot g)
        {
            g.SetSeed(_seed);
            g.StartRun();
            for (int i = 0; i < 8 && g.Rs != null && g.Rs.Phase != RunPhases.Combat; i++)
            {
                var rs = g.Rs;
                if (rs.Phase == RunPhases.Map) g.Do(new RunCommand_ChooseNode { Col = DeckRogue.Engine.Run.NextChoices(rs)[0] });
                else if (rs.Phase == RunPhases.Departure) g.Do(DeckRogue.Engine.Run.DefaultDepartureCommand(rs));   // 出立の店 (2026-09-24): 既定=サービスを1つ買う→店を出る (出立の間は繰り返し呼ばれる)
                else break;
            }
            if (g.Rs == null || g.Rs.Phase != RunPhases.Combat) { yield return Shot("no-combat"); yield break; }
            yield return Shot("battle");
            g.ViewMap = true; g.Rebuild(); yield return Shot("battle-map"); g.ViewMap = false; g.Rebuild();   // 戦闘中のマップ常時閲覧 (2026-09-12)

            // 手札ホバー (EventTrigger へ PointerEnter を送る)
            var hand0 = GameObject.Find("hand1");
            if (hand0 != null)
            {
                var pd = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                UnityEngine.EventSystems.ExecuteEvents.Execute(hand0, pd, UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
                yield return new WaitForSeconds(0.25f);
                yield return Shot("battle-hover");
                UnityEngine.EventSystems.ExecuteEvents.Execute(hand0, pd, UnityEngine.EventSystems.ExecuteEvents.pointerExitHandler);
            }
            // 拡大表示 (長押し相当) と「鍛えた後を見る」
            if (g.Rs.Combat.Player.Hand.Count > 1)
            {
                CardPopup.Open(g, g.Rs.Combat.Player.Hand[1], g.Rs.Combat);
                yield return new WaitForSeconds(0.2f);
                yield return Shot("battle-popup");
                CardPopup.Close();
                yield return null;
            }

            g.ShowLog = true; g.Rebuild();
            yield return Shot("battle-log");
            g.ShowLog = false; g.Rebuild();

            // 敵ホバー (ツールチップ)。Rebuild 直後は古いパネルが破棄待ちで残るので、登録された的から辿る
            yield return null;
            var enemyRt = g.Anchor("enemy0");
            var enemy0 = enemyRt != null ? enemyRt.gameObject : null;
            if (enemy0 != null)
            {
                var pd2 = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                var ert = enemy0.GetComponent<RectTransform>();
                pd2.position = RectTransformUtility.WorldToScreenPoint(null, ert.TransformPoint(ert.rect.center));
                Debug.Log("[Autopilot] hover " + enemy0.name + " (" + (g.Rs.Combat.Enemies.Count > 0 ? g.Rs.Combat.Enemies[0].EnemyId : "?") + ")");
                UnityEngine.EventSystems.ExecuteEvents.Execute(enemy0, pd2, UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
                yield return Shot("battle-enemy-tip");
                UnityEngine.EventSystems.ExecuteEvents.Execute(enemy0, pd2, UnityEngine.EventSystems.ExecuteEvents.pointerExitHandler);
            }
            g.ViewPile = "draw"; g.Rebuild();
            yield return Shot("battle-pile");
            g.ViewPile = null; g.Rebuild();

            var st = g.Rs.Combat;
            CardInstance modeCard = null, dmgCard = null, reactionCard = null;
            foreach (var c in st.Player.Hand)
            {
                if (modeCard == null && c.Def.Modes != null && c.Def.Modes.Count > 0) modeCard = c;
                if (dmgCard == null && c.Def.Type != "reaction" && (c.Def.Modes == null || c.Def.Modes.Count == 0) && c.Def.Effects.Any(e => e.Effect == "dealDamage")) dmgCard = c;
                if (reactionCard == null && c.Def.Type == "reaction") reactionCard = c;
            }
            if (modeCard != null) { g.ModeChoiceUid = modeCard.Uid; g.Rebuild(); yield return Shot("battle-mode"); g.ModeChoiceUid = null; g.Rebuild(); }
            int alive = st.Enemies.Count(e => e.Hp > 0);
            if (dmgCard != null && alive > 1)
            {
                g.BeginPlay(dmgCard, null);
                yield return Shot("battle-target");
                // 対象を選んでプレイを完了させ、捨て札へ行くかを記録する (2026-09-07 「カードが捨て札にいかない」の再現)
                int tgt = -1;
                for (int i = 0; i < st.Enemies.Count; i++) if (st.Enemies[i].Hp > 0) { tgt = i; break; }
                string playedUid = dmgCard.Uid;
                g.OnEnemyClicked(tgt);
                // 攻撃コマはクリック直後 0.5 秒 (8fps×4) なので、演出待ちの前に撮る
                yield return new WaitForSeconds(0.03f);
                yield return Shot("battle-swing", 1);    // 溜め (0〜0.08s)
                yield return new WaitForSeconds(0.10f);
                yield return Shot("battle-swing2", 1);   // 食い込み (0.15〜0.31s)
                yield return WaitPresentation();
                yield return new WaitForSeconds(0.6f);
                var st2 = g.Rs != null ? g.Rs.Combat : null;
                if (st2 != null)
                {
                    bool inDiscard = st2.Player.DiscardPile.Any(c => c.Uid == playedUid);
                    bool inHand = st2.Player.Hand.Any(c => c.Uid == playedUid);
                    Debug.Log("[Autopilot] played " + dmgCard.Def.Name + " → discard=" + st2.Player.DiscardPile.Count + " (inDiscard=" + inDiscard + ", inHand=" + inHand + ") hand=" + st2.Player.Hand.Count + " energy=" + st2.Player.Energy + " pending=" + (g.Pending != null));
                    int handCards = g.Battle != null ? g.Battle.HandCount : -1;
                    Debug.Log("[Autopilot] hand ui cards=" + handCards);
                }
                yield return Shot("battle-played");
            }
            if (reactionCard != null)
            {
                g.DoCombat(new Command_SetCard { CardUid = reactionCard.Uid });
                yield return Shot("battle-set");
            }
            { ClearScry(g); g.DoCombat(new Command_EndTurn()); }
            yield return new WaitForSeconds(0.7f);
            yield return Shot("battle-enemy-phase");
            yield return WaitPresentation();
            yield return Shot("battle-after-end");
            if (g.Rs != null && g.Rs.Combat != null && g.Rs.Combat.Phase == CombatPhases.AwaitingReaction)
            {
                yield return Shot("battle-confirm");
                g.DoCombat(new Command_ConfirmReaction { Fire = false });
                yield return WaitPresentation();
                yield return Shot("battle-held");
            }
            // 残りの確認ウィンドウは全部温存して、敵の番が終わった2ターン目の盤面 (状態の札・手札の実値) を撮る
            for (int k = 0; k < 6 && g.Rs != null && g.Rs.Phase == RunPhases.Combat && g.Rs.Combat.Phase == CombatPhases.AwaitingReaction; k++)
            {
                g.DoCombat(new Command_ConfirmReaction { Fire = false });
                yield return WaitPresentation();
            }
            yield return WaitPresentation();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("battle-turn2");
        }

        /// <summary>
        /// 任意の状態へ跳んで1枚撮る (2026-09-12 ユーザー「あなたの確認用にデバッグメニュー」)。
        /// -state "key=value;key=value" で指定。キー:
        ///   phase=departure|map|combat|reward|relic|shop|event|campfire|workshop|won|lost  act=1..3
        ///   departure: ラン開始直後の出立の店 (2026-09-24)。pick=<品の番号> で除去・鍛えの「札を選ぶ」画面。depart=<番号,番号> でそれらを買った後の店
        ///   他のフェーズは店を出てから地図へ (depart=<番号,番号> ならそれらを買ってから出る = 買わなかった物が幕1の店の「行商が預かった支度」に並ぶ)  deck=<deckId>  relics=<id,id>  hp=<%>  gold=<n>  difficulty=<n>  leader=<id>
        ///   enemy=<encounterId or enemyId> (combat)  event=<eventId>  pick=<idx[,idx]> (工房の素材／報酬の選択枠)  submode=forge (焚き火)  shopmode=upgrade|remove
        ///   fire=1 (確認の窓で最初の候補を発動してコマ送り。fireshots=枚数・fireevery=Nフレームごと。2026-09-17)
        ///   endplay=1 (手番を終えて敵フェーズを演出付きでコマ送り。endshots=枚数・endevery=Nフレームごと。2026-09-17)
        ///   viewmap=1  viewdeck=1  viewrelics=1 (≡ のレリック一覧。2026-09-22)  tip=enemy|doll|karakuri (説明パネル。karakuri=からくりの見出し)  log=1  name=<shot名>  wait=<秒> (撮る前に待つ。ドローの演出を避ける。2026-09-19)
        /// act/deck/relics/hp/gold/difficulty のどれかがあればチェックポイント開始 (CreateDebugCheckpointRun)、無ければ通常開始
        /// </summary>
        IEnumerator StateJump(GameRoot g, string spec)
        {
            Audio.Verbose = true;   // 鳴らした音をログに (2026-09-19)
            var kv = new Dictionary<string, string>();
            foreach (var part in spec.Split(';'))
            {
                int eq = part.IndexOf('=');
                if (eq > 0) kv[part.Substring(0, eq).Trim().ToLowerInvariant()] = part.Substring(eq + 1).Trim();
            }
            string Get(string k, string dflt = null) { string v; return kv.TryGetValue(k, out v) ? v : dflt; }
            // HD-2D 見本の旗 (stage・cam・hd2d=slice・uionly・dumplayout・perf …。2026-09-30 P01)。知らないキーは HD2DFlags が無視する。
            // 起動の時 (BeforeSceneLoad) にも -state から当ててあるので、1行1起動なら何も変わらない (Changed も投げない)
            _stateSpec = spec;
            HD2DFlags.ApplyState(kv);
            string phase = (Get("phase", "map") ?? "map").ToLowerInvariant();
            g.SetSeed(_seed);
            g.LeaderId = Get("leader") ?? "leader_green";   // 前回の選択 (PlayerPrefs) に左右されないよう既定は緑
            if (Get("difficulty") != null) { int d; if (int.TryParse(Get("difficulty"), out d)) g.Difficulty = d; }
            bool checkpoint = Get("act") != null || Get("deck") != null || Get("relics") != null || Get("hp") != null || Get("gold") != null;
            string startErr = null;   // catch の中では yield できないので外で撮る
            try
            {
                if (checkpoint)
                {
                    int act; if (!int.TryParse(Get("act", "1"), out act)) act = 1;
                    int gold; bool hasGold = int.TryParse(Get("gold", ""), out gold);
                    double hpPct; bool hasHp = double.TryParse(Get("hp", ""), out hpPct);
                    var relics = Get("relics") != null ? new List<string>(Get("relics").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0)) : null;
                    var opts = new ReplayOriginCheckpoint
                    {
                        Act = act,
                        DeckId = Get("deck") ?? (act >= 2 ? "deck_big_mana" : g.LeaderId == "leader_green" ? "starter" : "starter_" + g.LeaderId.Replace("leader_", "")),   // 緑のスターターの id は "starter"
                        RelicIds = relics,
                        HpRatio = hasHp ? hpPct / 100.0 : (double?)null,
                        Gold = hasGold ? gold : (int?)null,
                        Difficulty = g.Difficulty,
                    };
                    g.Rs = DeckRogue.Engine.Run.CreateDebugCheckpointRun(_seed, ReactionModes.SetConfirm, g.LeaderId, opts);
                }
                else g.StartRun();
            }
            catch (Exception ex) { startErr = ex.Message; }
            if (startErr != null) { Debug.LogError("[Autopilot] state: 開始に失敗 " + startErr); yield return Shot("state-error"); yield break; }
            if (g.Rs == null) { yield return Shot("state-no-run"); yield break; }
            if (phase == "departure" && g.Rs.Phase == RunPhases.Departure && Get("depart") != null)
            {   // 買った後の店 (「買った」の判・所持金の減り) を撮る
                try
                {
                    foreach (var raw in Get("depart").Split(','))
                    {
                        int di;
                        if (!int.TryParse(raw.Trim(), out di) || di < 0 || di >= g.Rs.Departure.Offers.Count) continue;
                        var offer = g.Rs.Departure.Offers[di];
                        if (!DeckRogue.Engine.Run.DepartureOfferAvailable(g.Rs, offer)) continue;
                        int? card = DeckRogue.Engine.Run.EventChoiceNeedsCard(offer.Choice) ? DeckRogue.Engine.Run.DefaultEventCardIndex(g.Rs, offer.Choice) : null;
                        g.Rs = DeckRogue.Engine.Run.ApplyRunCommand(g.Rs, new RunCommand_BuyDeparture { Index = di, CardIndex = card });
                    }
                }
                catch (Exception ex) { Debug.LogError("[Autopilot] state: 出立の店で買えない " + ex.Message); }
            }
            // 出立の店 (2026-09-24): ラン開始直後は坑口の店。phase=departure 以外は店を出てから (depart=<番号,番号> ならそれらを買ってから出る。対象の札は既定)
            if (phase != "departure" && g.Rs.Phase == RunPhases.Departure)
            {
                try
                {
                    foreach (var raw in (Get("depart") ?? "").Split(','))
                    {
                        int di;
                        if (!int.TryParse(raw.Trim(), out di) || g.Rs.Departure == null || di < 0 || di >= g.Rs.Departure.Offers.Count) continue;
                        var offer = g.Rs.Departure.Offers[di];
                        if (!DeckRogue.Engine.Run.DepartureOfferAvailable(g.Rs, offer)) { Debug.LogWarning("[Autopilot] state: 出立の店で買えない " + offer.Id); continue; }
                        int? card = DeckRogue.Engine.Run.EventChoiceNeedsCard(offer.Choice) ? DeckRogue.Engine.Run.DefaultEventCardIndex(g.Rs, offer.Choice) : null;
                        g.Rs = DeckRogue.Engine.Run.ApplyRunCommand(g.Rs, new RunCommand_BuyDeparture { Index = di, CardIndex = card });
                        if (g.Rs.Phase != RunPhases.Departure) break;   // 遺物の選ぶ画面などへ進んだ
                    }
                    if (g.Rs.Phase == RunPhases.Departure) g.Rs = DeckRogue.Engine.Run.ApplyRunCommand(g.Rs, new RunCommand_LeaveDeparture());
                }
                catch (Exception ex) { Debug.LogError("[Autopilot] state: 出立の店を出られない " + ex.Message); g.Rs = g.Rs with { Phase = RunPhases.Map }; }
            }
            // ギア (2026-09-17): gears=<id,...> で持ち物を直接置く (チェックポイントの既定の抽選を上書き)・mana=<n> で魔素
            if (Get("gears") != null || Get("mana") != null)
            {
                try
                {
                    var rs0 = g.Rs;
                    if (Get("gears") != null)
                    {
                        var list = new List<GearInstance>();
                        var seen = new List<string>(rs0.SeenGearIds ?? new List<string>());
                        int gn = 0;
                        foreach (var id in Get("gears").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0 && x != "-"))
                        {
                            list.Add(Gears.MakeGear(id, "dbg" + (gn++) + "_" + id));
                            if (!seen.Contains(id)) seen.Add(id);
                        }
                        rs0 = rs0 with { Gears = list, SeenGearIds = seen };
                    }
                    if (Get("mana") != null) { int m; if (int.TryParse(Get("mana"), out m)) rs0 = rs0 with { Mana = Math.Max(0, Math.Min(Gears.MANA_MAX, m)) }; }
                    g.Rs = rs0;
                }
                catch (Exception ex) { Debug.LogError("[Autopilot] state: gears/mana " + ex.Message); }
            }

            var rs = g.Rs;
            try
            {
                switch (phase)
                {
                    case "combat":
                        if (Get("boss") == "1" || Get("elite") == "1")
                        {   // 幕ボス (elite=1 なら強個体) の節に立ってから始める (絵の大きさ・倍率・名前の帯がその値になる。2026-09-15)
                            string want = Get("boss") == "1" ? MapNodeTypes.Boss : MapNodeTypes.Elite;
                            for (int row = 0; row < rs.Map.Count; row++) for (int col = 0; col < rs.Map[row].Count; col++)
                                    if (rs.Map[row][col].Type == want) { rs = rs with { Row = row, Col = col }; row = rs.Map.Count; break; }
                        }
                        if (Get("enemy") != null) g.Rs = DeckRogue.Engine.Run.DebugLaunchCombat(rs, Get("enemy"));
                        else
                        {   // 最初に選べる戦闘ノードへ進む (通常経路)
                            var choices = DeckRogue.Engine.Run.NextChoices(rs);
                            int col = choices.Count > 0 ? choices[0] : 0;
                            for (int i = 0; i < choices.Count; i++) { var n = rs.Map[rs.Row + 1][choices[i]]; if (n.Type == MapNodeTypes.Battle) { col = choices[i]; break; } }
                            g.Do(new RunCommand_ChooseNode { Col = col });
                        }
                        break;
                    case "reward":
                        g.Rs = DeckRogue.Engine.Run.DebugRollRewards(rs);
                        if (Get("gearopt") != null) g.Rs = g.Rs with { GearOption = Get("gearopt") == "none" ? null : Get("gearopt") };   // 報酬のギア枠 (2026-09-17)。gearopt=none で枠なし
                        if (Get("cardsdone") == "1") g.Rs = g.Rs with { RewardOptions = null };   // 札を先に取った後の局面 (ギアだけ残る)
                        break;
                    case "relic": g.Rs = DeckRogue.Engine.Run.DebugOpenTreasure(rs); break;
                    case "shop": g.Rs = DeckRogue.Engine.Run.OpenShop(rs); g.ShopMode = Get("shopmode"); break;
                    case "event": g.Rs = DeckRogue.Engine.Run.DebugOpenEvent(rs, Get("event")); break;
                    case "campfire": g.Rs = rs with { Phase = RunPhases.Campfire, Combat = null }; g.SubMode = Get("submode"); break;
                    case "workshop": g.Rs = rs with { Phase = RunPhases.Workshop, Combat = null }; break;
                    case "won": g.Rs = rs with { Phase = RunPhases.Won, Combat = null }; break;
                    case "lost": g.Rs = rs with { Phase = RunPhases.Lost, Combat = null }; break;
                    default: break;   // map
                }
            }
            catch (Exception ex) { Debug.LogError("[Autopilot] state: フェーズへ跳べない " + ex.Message); }
            // 罠モデルの確認用 (2026-09-13): set=<手札index> で1枚仕込み、endturn=N でターンを進める
            // (敵フェーズは engine が自動解決。確認ウィンドウが開いたら止まる = 窓の残り回数の表示も撮れる)
            if (phase == "combat" && g.Rs != null && g.Rs.Combat != null)
            {
                // 置物・伏せ札を直接置く (配置の確認用。perms=<cardId,...>・sets=<cardId,...>。2026-09-14 スマホの戦闘レイアウト)
                try
                {
                    var st0 = g.Rs.Combat;
                    var pl = st0.Player;
                    if (Get("perms") != null)
                    {
                        var list = new List<CardInstance>(pl.Permanents);
                        int n = 0;
                        foreach (var id in Get("perms").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0))
                            list.Add(new CardInstance { Uid = id + "#dbgp" + (n++), Def = Content.GetCardDef(id) });
                        pl = pl with { Permanents = list };
                    }
                    if (Get("sets") != null)
                    {
                        var list = new List<CardInstance>(pl.SetCards);
                        int n = 0;
                        foreach (var raw in Get("sets").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0))
                        {   // 末尾の * は「前のターンに仕込んだ = 生きている罠」。無印は今ターン仕込んだ (準備中)
                            bool live = raw.EndsWith("*"); string id = live ? raw.Substring(0, raw.Length - 1) : raw;
                            list.Add(new CardInstance { Uid = id + "#dbgs" + (n++), Def = Content.GetCardDef(id), SetTurn = st0.Turn - (live ? 1 : 0) });
                        }
                        pl = pl with { SetCards = list, SetSlots = Math.Max(pl.SetSlots, list.Count) };
                    }
                    if (!ReferenceEquals(pl, st0.Player)) g.Rs = g.Rs with { Combat = st0 with { Player = pl } };
                    // 敵0の状態を直接いじる (演出の確認用 2026-09-17 ④⑥): ehp=<HP>・eexposed=<急所>・eblock=<ブロック>・hand=<cardId,...> (手札を差し替え)
                    var st1 = g.Rs.Combat;
                    if (st1.Enemies.Count > 0 && (Get("ehp") != null || Get("eexposed") != null || Get("eblock") != null))
                    {
                        var e0 = st1.Enemies[0];
                        int v;
                        string ehp = Get("ehp") ?? "";
                        if (ehp.StartsWith("h")) { int off = 0; int.TryParse(ehp.Substring(1), out off); v = e0.MaxHp / 2 + off; e0 = e0 with { Hp = Math.Max(1, Math.Min(e0.MaxHp, v)) }; }   // ehp=h+3: 半分の線の3上 (豹変の確認)
                        else if (int.TryParse(ehp, out v)) e0 = e0 with { Hp = Math.Max(1, Math.Min(e0.MaxHp, v)) };
                        if (int.TryParse(Get("eexposed") ?? "", out v)) e0 = e0 with { Exposed = Math.Max(0, v) };
                        if (int.TryParse(Get("eblock") ?? "", out v)) e0 = e0 with { Block = Math.Max(0, v) };
                        var enemies = new List<EnemyState>(st1.Enemies); enemies[0] = e0;
                        g.Rs = g.Rs with { Combat = st1 with { Enemies = enemies } };
                    }
                    if (Get("penergy") != null)
                    {   // 自分のエナジー (出せない札の沈み・X の全払いの確認)
                        int v; var st4 = g.Rs.Combat;
                        if (int.TryParse(Get("penergy"), out v)) g.Rs = g.Rs with { Combat = st4 with { Player = st4.Player with { Energy = Math.Max(0, v) } } };
                    }
                    if (Get("pblock") != null)
                    {   // 自分のブロック (敵の攻撃を盾で受ける演出の確認)
                        int v; var st3 = g.Rs.Combat;
                        if (int.TryParse(Get("pblock"), out v)) g.Rs = g.Rs with { Combat = st3 with { Player = st3.Player with { Block = Math.Max(0, v) } } };
                    }
                    if (Get("plight") != null)
                    {   // 自分の灯 (放出・灯コスト・しきい値の確認。2026-09-20)
                        int v; var st5 = g.Rs.Combat;
                        if (int.TryParse(Get("plight"), out v)) g.Rs = g.Rs with { Combat = st5 with { Player = st5.Player with { Light = Math.Max(0, v) } } };
                    }
                    if (Get("eweak") != null)
                    {   // 敵0の威圧 (意図の数字の -25% の確認。2026-09-20)
                        int v; var st6 = g.Rs.Combat;
                        if (int.TryParse(Get("eweak"), out v) && st6.Enemies.Count > 0)
                        {
                            var enemies = new List<EnemyState>(st6.Enemies); enemies[0] = enemies[0] with { Weak = Math.Max(0, v) };
                            g.Rs = g.Rs with { Combat = st6 with { Enemies = enemies } };
                        }
                    }
                    if (Get("hand") != null)
                    {
                        var st2 = g.Rs.Combat;
                        var list = new List<CardInstance>();
                        int n = 0;
                        foreach (var id in Get("hand").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0))
                            list.Add(new CardInstance { Uid = id + "#dbgh" + (n++), Def = Content.GetCardDef(id) });
                        g.Rs = g.Rs with { Combat = st2 with { Player = st2.Player with { Hand = list, Energy = Get("penergy") != null ? st2.Player.Energy : Math.Max(st2.Player.Energy, 5) } } };
                    }
                }
                catch (Exception ex) { Debug.LogError("[Autopilot] state: perms/sets " + ex.Message); }
                try
                {
                    int setIdx;
                    var hand = g.Rs.Combat.Player.Hand;
                    if (Get("set") == "r")
                    {   // set=r: 手札の最初のリアクション
                        setIdx = -1;
                        for (int i = 0; i < hand.Count; i++) if (hand[i].Def.Type == CardTypes.Reaction) { setIdx = i; break; }
                    }
                    else if (!int.TryParse(Get("set") ?? "", out setIdx)) setIdx = -1;
                    if (setIdx >= 0 && setIdx < hand.Count)
                    {
                        g.Rs = DeckRogue.Engine.Run.ApplyRunCommand(g.Rs, new RunCommand_Combat { Command = new Command_SetCard { CardUid = g.Rs.Combat.Player.Hand[setIdx].Uid } });
                    }
                    int turns;
                    if (int.TryParse(Get("endturn") ?? "", out turns))
                    {
                        for (int i = 0; i < turns && g.Rs.Combat != null && g.Rs.Combat.Phase == CombatPhases.PlayerTurn; i++)
                        {
                            g.Rs = DeckRogue.Engine.Run.ApplyRunCommand(g.Rs, new RunCommand_Combat { Command = new Command_EndTurn() });
                        }
                    }
                    // hold=N: 確認の窓を N 回温存して次の敵の窓へ (② や ③ が行動する窓の配置の確認。2026-09-29 p11)
                    int holds;
                    if (int.TryParse(Get("hold") ?? "", out holds))
                    {
                        for (int i = 0; i < holds && g.Rs.Combat != null && g.Rs.Combat.Phase == CombatPhases.AwaitingReaction; i++)
                            g.Rs = DeckRogue.Engine.Run.ApplyRunCommand(g.Rs, new RunCommand_Combat { Command = new Command_ConfirmReaction { Fire = false } });
                    }
                }
                catch (Exception ex) { Debug.LogError("[Autopilot] state: set/endturn " + ex.Message); }
            }
            var picks = (Get("pick") ?? "").Split(',').Select(x => { int v; return int.TryParse(x.Trim(), out v) ? v : -1; }).Where(v => v >= 0).ToList();
            if (phase == "workshop") { g.WorkshopA = picks.Count > 0 ? picks[0] : -1; g.WorkshopB = picks.Count > 1 ? picks[1] : -1; }
            if (phase == "event" && picks.Count > 0) g.EventChoiceIndex = picks[0];   // イベントの「デッキから1枚選ぶ」画面 (pick=選択肢の番号。2026-09-15)
            if (phase == "departure" && picks.Count > 0) g.DepartureChoiceIndex = picks[0];   // 出立の店の「札を選ぶ」画面 (pick=品の番号。2026-09-24)
            if (Get("doodle") == "1")
            {   // 見本の落書き: 現在地の丸と、右上へ向かう波線 (描画の確認用)
                var l = g.DoodlesFor(g.Rs.Act);
                var ring = new DoodleStroke { Color = 1 };
                for (int i = 0; i <= 24; i++) { float a = i / 24f * Mathf.PI * 2f; ring.Points.Add(new Vector2(550f + Mathf.Cos(a) * 52f, 146f + Mathf.Sin(a) * 52f)); }
                var wave = new DoodleStroke { Color = 0 };
                for (int i = 0; i <= 40; i++) wave.Points.Add(new Vector2(560f + i * 9f, 240f + i * 11f + Mathf.Sin(i * 0.8f) * 12f));
                l.Add(ring); l.Add(wave);
                if (Get("pen") != null) { g.DoodleMode = true; int pen; if (int.TryParse(Get("pen"), out pen)) g.DoodlePen = pen; }
            }
            if (Get("viewmap") == "1") g.ViewMap = true;
            if (Get("viewdeck") == "1") g.ViewDeck = true;
            if (Get("viewrelics") == "1") g.ViewRelics = true;
            if (Get("upgraded") == "1") g.ShowUpgraded = true;   // 一覧の「鍛えた後を見る」(2026-09-16)
            if (Get("gridpick") != null) { var gp = Get("gridpick").Split(':'); int gi; if (gp.Length == 2 && int.TryParse(gp[1], out gi)) g.SetGridPick(gp[0], gi); }   // gridpick=forge:2 = 押した札
            if (Get("log") == "1") g.ShowLog = true;
            if (Get("gearwin") != null) { int gi; if (int.TryParse(Get("gearwin"), out gi)) g.GearPending = new PendingGear { Index = gi }; }   // ギアのトークンを押した状態 = 窓 (2026-09-17)
            if (Get("gearmore") == "1") g.GearMore = true;   // ギアの「+N」を押した状態 = 持ち物の一覧 (2026-09-18)
            if (Get("gearswap") == "1") g.GearSwap = g.Rs != null && g.Rs.Phase == RunPhases.Shop ? "shop:0" : "reward";   // 満杯の入れ替え窓
            if (Get("menu") == "1") g.MenuOpen = true;   // スマホの ≡ (2026-09-14)
            if (Get("settings") == "1") g.SettingsOpen = true;   // 設定の窓 (2026-09-21)
            // フィードバックの画面 (2026-09-14): rating=won|lost で評価ダイアログ (最後の戦闘を仮に積む)、memo=1 でメモの窓
            if (Get("rating") != null && g.Rs != null)
            {
                Feedback.History.Add(new BattleArchive { BattleNo = g.Rs.BattlesWon + 1, Act = g.Rs.Act, EnemyId = Get("enemy") ?? "enemy_probe", Result = Get("rating") == "lost" ? "lost" : "won", Turns = 3, HpBefore = g.Rs.MaxHp, HpAfter = g.Rs.Hp, DeckSize = g.Rs.Deck.Count });
                if (Get("rating") == "rated") Feedback.LastBattle.Rating = new BattleRating { Strength = 3, Fun = 4 };   // 閉じた後の左下「評価 済」
                else { Feedback.OpenRating(); Feedback.DraftStrength = 3; }
            }
            if (Get("memo") == "1") { Feedback.MemoOpen = true; Feedback.MemoDraft = Get("memotext") ?? ""; }
            var perfBase = g.Rs;   // perf=<秒>: 決着したらこの盤面へ跳び直す (RunState は不変なので同じ戦闘がそのまま戻る)
            g.Rebuild();
            yield return DetSettleStage();   // det: 舞台の粒を頭から再生して90フレーム待つ (det でなければ何もしない)
            // entershots=N: 戦闘の始まり (敵の登場・強個体/幕ボスの名前の帯) をコマ送りで撮る (2026-09-17 ⑨)。状態へ跳んだ直後は Play が呼ばれないので明示的に鳴らす
            if (Get("entershots") != null && g.Rs != null && g.Rs.Combat != null)
            {
                int shotsN = 10; int.TryParse(Get("entershots") ?? "", out shotsN); if (shotsN <= 0) shotsN = 10;
                int every = 8; int.TryParse(Get("enterevery") ?? "", out every); if (every <= 0) every = 8;
                // hideui / hidezone は連写の前に当てる (2026-10-02 三周目 直しの輪1: 旧は連写の後ろ (1枚撮りの直前) でだけ当てていたので、
                // r3-clips の「UI なし」の連写に帳面・手札・上部バーが写っていた)。組み直しで戻っても消えたままになるよう、1枚ごとにも当て直す。
                // uionly / unitsonly は Shot の ApplyCaptureMode が1枚ごとに当てる (もとから連写に効く)。
                // hideui の連写の間は演出の層 (fx: 強個体・幕ボスの名前の帯・登場の土煙の輪・判) も隠し、連写が終わったら戻す (1枚撮りは今のまま)
                bool hideBurst = Get("hideui") == "1" && g.ScreenRoot != null && g.Battle != null;
                bool zoneBurst = Get("hidezone") == "1" && g.ScreenRoot != null;
                if (hideBurst) HideUiNow(g);
                if (zoneBurst) HideZoneNow(g);
                Time.captureFramerate = 60;
                yield return null;
                Presenter.Reset();
                Presenter.Play(g, g.Rs.Combat);
                GameObject fxGo = hideBurst && g.FxLayer != null ? g.FxLayer.gameObject : null;
                bool fxWas = fxGo != null && fxGo.activeSelf;
                if (fxGo != null) fxGo.SetActive(false);
                try
                {
                    for (int i = 0; i < shotsN; i++)
                    {
                        for (int f = 0; f < every; f++) yield return null;
                        if (hideBurst) HideUiNow(g);
                        if (zoneBurst) HideZoneNow(g);
                        yield return Shot("enter-" + i, 1);
                    }
                }
                finally { if (fxGo != null) fxGo.SetActive(fxWas); }
                Time.captureFramerate = DetFramerate;   // det なら 60 のまま (撮影の時間刻みを最後まで固定する。2026-09-30 HD-2D 見本 P00)
            }
            yield return WaitPresentation();
            // 配置の確認 (スマホ倍率の調整用): 絵の枠の大きさと足元の高さ
            if (g.Battle != null && g.Rs != null && g.Rs.Combat != null)
            {
                var sbd = new System.Text.StringBuilder("[Autopilot] layout canvas=" + BattleScreen.CanvasSize(g.ScreenRoot) + " statusLine=" + BattleView.StatusLineY);
                for (int i = 0; i < g.Rs.Combat.Enemies.Count; i++) { var sp = g.Battle.EnemySprite(i); if (sp != null) sbd.Append(" enemy" + i + "=" + sp.rect.size + "@" + sp.offsetMin + " feet=" + Stage.FeetOffset("enemy" + i, -1f)); }
                var ps = g.Battle.PlayerSprite(); if (ps != null) sbd.Append(" player=" + ps.rect.size + "@" + ps.offsetMin + " feet=" + Stage.FeetOffset("player", -1f));
                Debug.Log(sbd.ToString());
            }
            // fx=slash[:angle]: 斬撃を敵0の中心に出して撮る (向きと大きさの確認。2026-09-16)。fx=hit[:angle] は敵→自分の斬撃 (朱) を自分の絵の上に。
            // fx=fang|horn|vine|stomp|spell|light|spark は自分の札の当たりの形 (2026-09-22。fxhits=N で多段の交互を N 発、fxshots=枚数)
            string fxKey = Get("fx") ?? "";
            string[] styles = { "slash", "hit", "fang", "horn", "vine", "stomp", "spell", "light", "spark" };
            if (styles.Any(st => fxKey.StartsWith(st)) && g.Rs != null && g.Rs.Combat != null && g.Battle != null)
            {
                bool hit = fxKey.StartsWith("hit");
                string style = styles.First(st => fxKey.StartsWith(st));
                float ang = hit ? 35f : -35f; var parts = fxKey.Split(':'); if (parts.Length > 1) float.TryParse(parts[1], out ang);
                var fx = g.FxLayer;
                var spr = hit ? g.Battle.PlayerSprite() : g.Battle.EnemySprite(0);
                var pSpr = g.Battle.PlayerSprite();
                int hits = 1; int.TryParse(Get("fxhits") ?? "1", out hits); if (hits < 1) hits = 1;
                int shots = 8; int.TryParse(Get("fxshots") ?? "8", out shots);
                // 決定的な時間刻み: CaptureScreenshot で実時間が跳ぶので、captureFramerate で1フレーム=1/60秒に固定して3フレームごとに撮る
                Time.captureFramerate = 60;
                yield return null;
                if (spr != null && fx != null)
                {
                    if (hit || (style == "slash" && parts.Length > 1)) Tween.SlashFx(fx, Tween.CenterIn(spr, fx), ang, hit ? new Color(1f, 0.62f, 0.5f, 0.95f) : ThemeFx.SlashCore, Get("big") == "1");
                    else
                    {
                        Vector2 from = pSpr != null ? Tween.CenterIn(pSpr, fx) : Tween.CenterIn(spr, fx) + new Vector2(-400f, 0f);
                        Color col = style == "spell" ? new Color(PaperFx.Mana.r, PaperFx.Mana.g, PaperFx.Mana.b, 0.95f) : style == "light" ? ThemeFx.LampGlow : style == "spark" ? new Color(PaperFx.Ember.r, PaperFx.Ember.g, PaperFx.Ember.b, 0.95f) : ThemeFx.SlashCore;
                        for (int h = 0; h < hits; h++) { int hh = h; Tween.After(0.14f * h, () => Tween.PlayerHitFx(fx, Tween.CenterIn(spr, fx), style, col, Get("big") == "1", hh, hits, from)); }
                    }
                }
                for (int i = 0; i < shots; i++) { yield return null; yield return null; yield return Shot("fx-" + i, 1); }
                Time.captureFramerate = DetFramerate;   // det なら 60 のまま (撮影の時間刻みを最後まで固定する。2026-09-30 HD-2D 見本 P00)
            }
            // play=<手札index>: その札をプレイして (対象は最初の生存敵)、攻撃コマの途中を 4 枚撮る (2026-09-16 このは v2 のアニメ確認)
            int playIdx;
            if (Get("play") == "attack" && g.Rs != null && g.Rs.Combat != null)   // play=attack: 手札で最初のダメージ札 (index を数えなくてよい)
            {
                playIdx = -1;
                for (int i = 0; i < g.Rs.Combat.Player.Hand.Count; i++) { var c = g.Rs.Combat.Player.Hand[i]; if (c.Def.Type != "reaction" && (c.Def.Modes == null || c.Def.Modes.Count == 0) && c.Def.Effects.Any(e => e.Effect == "dealDamage")) { playIdx = i; break; } }
            }
            else if (!int.TryParse(Get("play") ?? "", out playIdx)) playIdx = -1;
            if (playIdx >= 0 && g.Rs != null && g.Rs.Combat != null && playIdx < g.Rs.Combat.Player.Hand.Count)
            {
                var pc = g.Rs.Combat.Player.Hand[playIdx];
                Debug.Log("[Autopilot] play " + playIdx + " " + pc.Def.Name);
                Time.captureFramerate = 60;   // 決定的な時間刻み (1フレーム=1/60秒)。プレイの前から固定して演出の頭を撮り逃さない
                Presenter.MarkSeen(g.Rs.Combat);   // 跳んだ直後は戦闘開始・ターン開始の演出が未消化で、プレイの演出がその後ろに並んでしまう
                int? modeIdx = null; int modeV;   // mode=<idx>: 選択式の札のモード (2026-09-20 威圧の不具合の再現に 灯の岐路「眩ます」)
                if (int.TryParse(Get("mode") ?? "", out modeV) && pc.Def.Modes != null && modeV >= 0 && modeV < pc.Def.Modes.Count) modeIdx = modeV;
                else if (pc.Def.Modes != null && pc.Def.Modes.Count > 0) modeIdx = 0;
                if (Get("viaui") == "1" && g.Battle != null)
                {
                    // viaui=1: 人がマウスで押すのと同じ経路 (手札の札の PointerClick → モードの窓のボタン → 敵の札) を EventSystem で叩く
                    var es = UnityEngine.EventSystems.EventSystem.current;
                    var cardGo = g.Battle.HandLayer != null ? g.Battle.HandLayer.Find("hand" + playIdx) : null;
                    if (cardGo != null)
                    {
                        var pdc = new UnityEngine.EventSystems.PointerEventData(es) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
                        UnityEngine.EventSystems.ExecuteEvents.Execute(cardGo.gameObject, pdc, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                        Debug.Log("[Autopilot] viaui click hand" + playIdx + " modeChoice=" + (g.ModeChoiceUid != null));
                        yield return null;
                        if (g.ModeChoiceUid != null && g.Battle.UiLayer != null)
                        {
                            string want = ((modeIdx ?? 0) + 1) + ":";
                            foreach (var b in g.Battle.UiLayer.GetComponentsInChildren<Button>(true))
                            {
                                var tx = b.GetComponentInChildren<TMPro.TMP_Text>(true);
                                if (tx != null && tx.text.StartsWith(want)) { Debug.Log("[Autopilot] viaui mode button " + tx.text); b.onClick.Invoke(); break; }
                            }
                            yield return null;
                        }
                    }
                    else Debug.Log("[Autopilot] viaui: hand" + playIdx + " が見つからない");
                }
                else g.BeginPlay(pc, modeIdx);
                yield return null;
                // 「人形を1体選ぶ」札 (灯の捧げ) は舞台の最初の人形を押して選ぶ (2026-09-19 人形の盤面表示: 崩れる演出の確認)
                if (g.Pending != null && g.Pending.NextNeed() == "permanent" && g.Battle != null)
                {
                    var dollsNow = g.Battle.StageDolls(g.Rs.Combat);
                    if (dollsNow.Count > 0) g.OnDollClicked(dollsNow[0].Uid);
                    yield return null;
                }
                if (g.Pending != null && g.Pending.NextNeed() == "target")
                {
                    int tgt = -1; int want;   // target=<敵index> で狙いを指定 (省略は最初の生存敵)
                    if (int.TryParse(Get("target") ?? "", out want) && want >= 0 && want < g.Rs.Combat.Enemies.Count && g.Rs.Combat.Enemies[want].Hp > 0) tgt = want;
                    else for (int i = 0; i < g.Rs.Combat.Enemies.Count; i++) if (g.Rs.Combat.Enemies[i].Hp > 0) { tgt = i; break; }
                    if (tgt >= 0) g.OnEnemyClicked(tgt);
                }
                int shotsN = 4; int.TryParse(Get("playshots") ?? "", out shotsN); if (shotsN <= 0) shotsN = 4;   // playshots=N で枚数 (札が飛んで着弾するまで 0.3〜0.6 秒)
                int every = 4; int.TryParse(Get("playevery") ?? "", out every); if (every <= 0) every = 4;      // playevery=N フレームごとに撮る (1フレーム=1/60秒に固定)
                for (int i = 0; i < shotsN; i++) { for (int f = 0; f < every; f++) yield return null; yield return Shot("play-" + i, 1); }
                Time.captureFramerate = DetFramerate;   // det なら 60 のまま (撮影の時間刻みを最後まで固定する。2026-09-30 HD-2D 見本 P00)
                yield return WaitPresentation();
            }
            // usegear=<持ち物index>[:<敵index>]: そのギアを組んで (対象は指定か最初の生存敵)、「組んだ」の演出をコマ送りで撮る (2026-09-17)。playshots/playevery を共用。
            // 札を選ぶギアは選ぶ窓が開いた所で止まる (窓の確認)
            if (Get("usegear") != null && g.Rs != null && g.Rs.Combat != null)
            {
                var gspec = Get("usegear").Split(':');
                int gIdx; int gTgt = -1;
                if (int.TryParse(gspec[0], out gIdx) && gIdx >= 0 && gIdx < DeckRogue.Engine.Run.GearsOf(g.Rs).Count)
                {
                    if (gspec.Length > 1) int.TryParse(gspec[1], out gTgt);
                    Debug.Log("[Autopilot] usegear " + gIdx);
                    Time.captureFramerate = 60;
                    Presenter.MarkSeen(g.Rs.Combat);
                    g.GearPending = new PendingGear { Index = gIdx, TargetIndex = gTgt >= 0 ? gTgt : (int?)null };
                    GearUi.Submit(g);
                    yield return null;
                    if (g.GearPending != null && g.GearPending.Stage == "target" && g.Rs.Combat != null)
                    {
                        int tgt = -1;
                        for (int i = 0; i < g.Rs.Combat.Enemies.Count; i++) if (g.Rs.Combat.Enemies[i].Hp > 0) { tgt = i; break; }
                        if (tgt >= 0) g.OnEnemyClicked(tgt);
                    }
                    int shotsN = 6; int.TryParse(Get("playshots") ?? "", out shotsN); if (shotsN <= 0) shotsN = 6;
                    int every = 5; int.TryParse(Get("playevery") ?? "", out every); if (every <= 0) every = 5;
                    for (int i = 0; i < shotsN; i++) { for (int f = 0; f < every; f++) yield return null; yield return Shot("gear-" + i, 1); }
                    Time.captureFramerate = DetFramerate;   // det なら 60 のまま (撮影の時間刻みを最後まで固定する。2026-09-30 HD-2D 見本 P00)
                    yield return WaitPresentation();
                }
            }
            // endplay=1: 手番を終えて敵フェーズを演出付き (Do 経由 = 順送り) で走らせ、コマ送りで撮る (2026-09-17 敵の行動の演出)。endshots=枚数・endevery=Nフレームごと
            if (Get("endplay") == "1" && g.Rs != null && g.Rs.Combat != null && g.Rs.Combat.Phase == CombatPhases.PlayerTurn)
            {
                Time.captureFramerate = 60;
                Presenter.MarkSeen(g.Rs.Combat);
                { ClearScry(g); g.DoCombat(new Command_EndTurn()); }
                int shotsN = 12; int.TryParse(Get("endshots") ?? "", out shotsN); if (shotsN <= 0) shotsN = 12;
                int every = 10; int.TryParse(Get("endevery") ?? "", out every); if (every <= 0) every = 10;
                for (int i = 0; i < shotsN; i++) { for (int f = 0; f < every; f++) yield return null; yield return Shot("end-" + i, 1); }
                Time.captureFramerate = DetFramerate;   // det なら 60 のまま (撮影の時間刻みを最後まで固定する。2026-09-30 HD-2D 見本 P00)
                yield return WaitPresentation();
            }
            // fire=1: 確認の窓が開いていれば最初の候補を発動して、からくりの演出 (札の飛び出し・判・着弾) をコマ送りで撮る (2026-09-17)。fireshots=枚数・fireevery=Nフレームごと
            if (Get("fire") == "1" && g.Rs != null && g.Rs.Combat != null && g.Rs.Combat.Phase == CombatPhases.AwaitingReaction)
            {
                var stF = g.Rs.Combat;
                var winF = Effects.WindowFromPending(stF);
                var cands = winF != null ? Effects.UsableSetCards(stF, winF) : null;
                if (cands != null && cands.Count > 0)
                {
                    Debug.Log("[Autopilot] fire " + cands[0].Def.Name);
                    Time.captureFramerate = 60;
                    Presenter.MarkSeen(g.Rs.Combat);
                    g.DoCombat(new Command_ConfirmReaction { Fire = true, CardUid = cands[0].Uid });
                    int shotsN = 6; int.TryParse(Get("fireshots") ?? "", out shotsN); if (shotsN <= 0) shotsN = 6;
                    int every = 6; int.TryParse(Get("fireevery") ?? "", out every); if (every <= 0) every = 6;
                    for (int i = 0; i < shotsN; i++) { for (int f = 0; f < every; f++) yield return null; yield return Shot("fire-" + i, 1); }
                    Time.captureFramerate = DetFramerate;   // det なら 60 のまま (撮影の時間刻みを最後まで固定する。2026-09-30 HD-2D 見本 P00)
                    yield return WaitPresentation();
                }
                else Debug.LogWarning("[Autopilot] fire: 発動できる仕込み札が無い");
            }
            // hideui=1: 舞台と絵 (敵・リーダー・狙いの輪) だけを残して UI を全部消す (配置案のモックの下地用。2026-09-15 戦闘画面の見直し)。
            // entershots の連写の時は連写の前にも当てる (HideUiNow。2026-10-02 三周目 直しの輪1)。ここは1枚撮りの時と同じ (同じ処理を2度当てても同じ)
            if (Get("hideui") == "1" && g.ScreenRoot != null && g.Battle != null)
            {
                HideUiNow(g);
                yield return null;
            }
            // hidezone=1: 伏せ場と置物の欄を消して撮る (配置案のモックの下地用。2026-09-14)
            if (Get("hidezone") == "1" && g.ScreenRoot != null)
            {
                HideZoneNow(g);
                yield return null;
            }
            // scroll=1: 画面の一覧を一番下まで送る (最後の行が選べるかの確認。2026-09-14)
            if (Get("scroll") == "1" && g.ScreenRoot != null)
            {
                yield return null;   // レイアウトを1フレーム待つ
                foreach (var sr in g.ScreenRoot.GetComponentsInChildren<ScrollRect>(true)) if (sr.vertical) sr.verticalNormalizedPosition = 0f;
                yield return null;
            }
            // aim=<敵index>: 札を選ばずに敵を押した状態 (狙い。庇われている敵なら縁は護衛に回り一言が出る) /
            // pend=<手札index>: 単体の札を選んで対象を待つ状態 (候補の縁と輪。庇われている敵には付かない)。2026-09-29 p01
            int aimIdx;
            if (int.TryParse(Get("aim") ?? "", out aimIdx) && g.Rs != null && g.Rs.Combat != null && aimIdx >= 0 && aimIdx < g.Rs.Combat.Enemies.Count) { g.OnEnemyClicked(aimIdx); yield return null; }
            int pendIdx;
            if (int.TryParse(Get("pend") ?? "", out pendIdx) && g.Rs != null && g.Rs.Combat != null && pendIdx >= 0 && pendIdx < g.Rs.Combat.Player.Hand.Count) { g.PreferredTarget = -1; g.BeginPlay(g.Rs.Combat.Player.Hand[pendIdx], null); yield return null; }
            // dragaim=<手札index>:<敵index>: その札をつかんで敵の胴の上まで引いた途中 (指を離さない) を撮る (2026-09-29 p09 スマホの狙いの矢)。
            // 手札の札に BeginDrag と Drag を EventSystem で1回ずつ送る (人の指と同じ経路)。敵の番号を省くと最初の生存敵。
            // スマホ (UISCALE=1.6) で敵を狙う札なら、札は手札に残り矢と縁が出る。PC・狙わない札は今までどおり札が指 (敵の胴) に付く
            if (Get("dragaim") != null && g.Rs != null && g.Rs.Combat != null && g.Battle != null && g.Battle.HandLayer != null)
            {
                var da = Get("dragaim").Split(':');
                int dh, de = -1;
                bool low = da.Length >= 2 && da[1] == "low";   // dragaim=<手札>:low = 指を画面の下 30% (取り消しの線より下) に置いた途中 = 矢が消える
                bool miss = da.Length >= 2 && da[1] == "miss";   // dragaim=<手札>:miss = 指をリーダーと敵の間の地面に置いた途中 (どの敵も指していない。2026-09-30 F34 の案内の札)
                if (int.TryParse(da[0], out dh) && dh >= 0 && dh < g.Rs.Combat.Player.Hand.Count)
                {
                    if (da.Length < 2 || low || miss || !int.TryParse(da[1], out de) || de < 0 || de >= g.Rs.Combat.Enemies.Count || g.Rs.Combat.Enemies[de].Hp <= 0)
                    {
                        de = -1;
                        for (int i = 0; i < g.Rs.Combat.Enemies.Count; i++) if (g.Rs.Combat.Enemies[i].Hp > 0) { de = i; break; }
                    }
                    var cardGo = g.Battle.HandLayer.Find("hand" + dh);
                    var epan = de >= 0 ? g.Anchor("enemy" + de) : null;
                    var body = epan != null ? (epan.Find("sprite") as RectTransform ?? epan) : null;
                    if (cardGo != null && body != null)
                    {
                        var es = UnityEngine.EventSystems.EventSystem.current;
                        Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, body.TransformPoint(body.rect.center));   // 重ねの UI (Overlay) なので世界の座標＝画面の座標
                        var cardSp = RectTransformUtility.WorldToScreenPoint(null, cardGo.position);
                        if (low) sp = new Vector2(cardSp.x + 60f, Screen.height * 0.3f);
                        if (miss) sp = new Vector2(Screen.width * 0.42f, Screen.height * 0.55f);
                        var pdd = new UnityEngine.EventSystems.PointerEventData(es) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left, pressPosition = cardSp, position = cardSp, dragging = true };
                        UnityEngine.EventSystems.ExecuteEvents.Execute(cardGo.gameObject, pdd, UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
                        pdd.position = sp;
                        UnityEngine.EventSystems.ExecuteEvents.Execute(cardGo.gameObject, pdd, UnityEngine.EventSystems.ExecuteEvents.dragHandler);
                        yield return null;
                        UnityEngine.EventSystems.ExecuteEvents.Execute(cardGo.gameObject, pdd, UnityEngine.EventSystems.ExecuteEvents.dragHandler);   // 描き直した札の上で矢をもう一度引く
                        Debug.Log("[Autopilot] dragaim hand" + dh + " → enemy" + de + " at " + sp);
                        yield return null;
                    }
                    else Debug.LogWarning("[Autopilot] dragaim: hand" + dh + " か enemy" + de + " が見つからない");
                }
            }
            // tip=enemy: 敵をタップした説明パネル (スマホ) / popup=N: 手札 N 枚目の長押しポップアップ
            if (Get("tip") == "enemy" && g.Rs != null && g.Rs.Combat != null && g.Battle != null)
            {
                var pan = g.Anchor("enemy0");
                var body = BattleScreen.EnemyTip(g, 0);
                if (pan != null && body != null) Tooltip.ShowPinned(body, pan.gameObject);
                yield return null;
            }
            if (Get("tip") == "karakuri" && g.Rs != null && g.Rs.Combat != null && g.Battle != null)
            {   // tip=karakuri: からくりの見出し「からくり 1 / 2」を押した説明パネル (2026-09-29 p15)
                var lbl = g.Anchor("setlabel");
                var kbody = BattleScreen.SetLabelTip(g.Rs.Combat);
                if (lbl != null && kbody != null) Tooltip.ShowPinned(kbody, lbl.gameObject);
                yield return null;
            }
            if (Get("tip") == "doll" && g.Rs != null && g.Rs.Combat != null && g.Battle != null)
            {   // tip=doll: 舞台の最初の人形をタップした説明パネル (2026-09-22 友人ラン「何する人形なのか」)
                string duid = null;
                foreach (var p in g.Rs.Combat.Player.Permanents) if (DollUi.IsDoll(p)) { duid = p.Uid; break; }
                var dspr = duid != null ? g.Battle.DollSprite(duid) : null;
                var dbody = duid != null ? BattleScreen.DollTip(g, duid) : null;
                if (dspr != null && dbody != null) Tooltip.ShowPinned(dbody, dspr.parent.gameObject);
                yield return null;
            }
            int popupIdx;
            if (int.TryParse(Get("popup") ?? "", out popupIdx) && g.Rs != null && g.Rs.Combat != null && popupIdx < g.Rs.Combat.Player.Hand.Count)
            {
                CardPopup.Open(g, g.Rs.Combat.Player.Hand[popupIdx], g.Rs.Combat);
                yield return null;
            }
            // wait=<秒>: 撮る前に待つ (ドローの札が飛んでいる途中を避けて、落ち着いた盤面を撮る。2026-09-19 人形の盤面表示の下地)
            float waitS;
            if (float.TryParse(Get("wait") ?? "", System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out waitS) && waitS > 0f) yield return new WaitForSeconds(waitS);
            yield return Shot(Get("name") ?? ("state-" + phase), 10);
            // closemap=1: 重ねた地図を「閉じる」と同じ手順で閉じてもう1枚 (2026-09-14 戦闘中に閉じない不具合の確認)
            if (Get("closemap") == "1")
            {
                g.ViewMap = false; g.Rebuild();
                yield return WaitPresentation();
                yield return Shot((Get("name") ?? ("state-" + phase)) + "-closed", 10);
            }
            // export=1: レポート/セーブの書き出しを通す (共有シートは開かない)。書いた道と大きさをログへ
            if (Get("export") == "1" && g.Rs != null)
            {
                Feedback.Silent = true;
                try
                {
                    var md = Feedback.Export(g.Rs);
                    var json = md.Replace("play-", "save-").Replace(".md", ".json");
                    Debug.Log("[Autopilot] export md=" + md + " (" + new System.IO.FileInfo(md).Length + " bytes) json=" + (System.IO.File.Exists(json) ? new System.IO.FileInfo(json).Length + " bytes" : "none"));
                }
                catch (Exception ex) { Debug.LogError("[Autopilot] export failed: " + ex); }
                g.Notice = "レポートを書き出した: " + Feedback.LastExportPath;
                g.Rebuild();
                yield return Shot((Get("name") ?? ("state-" + phase)) + "-exported", 10);
            }
            // confirm=abandon: 「ランを放棄」の確認ダイアログを撮る (2026-09-15)
            if (Get("confirm") == "abandon" && g.Rs != null)
            {
                g.AskAbandonRun();
                yield return Shot((Get("name") ?? ("state-" + phase)) + "-confirm", 10);
                g.Confirm = null; g.Rebuild();
            }
            // saveexit=1: 「セーブして終了」を通してタイトルの「続きから」の帯を撮る。resume=1 なら「続きから」で戻ってもう1枚
            // (状態→セーブ→タイトル→読み戻し→同じ画面、がプレイヤーで通ることの確認)。confirm=newrun はタイトルで「新しいランを開始」の確認を撮る
            if (Get("saveexit") == "1" && g.Rs != null)
            {
                string nm = Get("name") ?? ("state-" + phase);
                var before = g.Rs;
                g.SaveAndQuit();
                yield return null;
                Debug.Log("[Autopilot] saveexit file=" + SaveGame.FilePath + " exists=" + SaveGame.Exists + (SaveGame.Exists ? " bytes=" + new FileInfo(SaveGame.FilePath).Length : "") + " summary=" + SaveGame.PeekSummary());
                yield return Shot(nm + "-title", 10);
                if (Get("confirm") == "newrun")
                {
                    g.AskStartRun();
                    yield return Shot(nm + "-confirm-newrun", 10);
                    g.Confirm = null; g.Rebuild();
                }
                if (Get("resume") == "1")
                {
                    g.ResumeSave();
                    yield return WaitPresentation();
                    string same = g.Rs != null ? (Golden.RunHash(g.Rs) == Golden.RunHash(before) ? "same-hash" : "HASH DIFFERS " + Golden.RunHash(before) + " → " + Golden.RunHash(g.Rs)) : "NO RUN (" + g.Error + ")";
                    Debug.Log("[Autopilot] resume " + same + " notice=" + g.Notice + " history=" + Feedback.History.Count + " journal=" + (Feedback.JournalOrNull() != null ? Feedback.JournalOrNull().Commands.Count.ToString() : "none"));
                    yield return Shot(nm + "-resumed", 10);
                }
            }
            // resize=WxH: ウィンドウの大きさを変えて数フレーム待ち、もう1枚 (キャラの足元が地面に着いたままかの確認。2026-09-12)
            if (Get("resize") != null)
            {
                var wh = Get("resize").ToLowerInvariant().Split('x');
                int rw, rh;
                if (wh.Length == 2 && int.TryParse(wh[0], out rw) && int.TryParse(wh[1], out rh))
                {
                    Screen.SetResolution(rw, rh, false);
                    for (int i = 0; i < 12; i++) yield return null;
                    yield return WaitPresentation();
                    yield return Shot((Get("name") ?? ("state-" + phase)) + "-resized", 10);
                }
            }
            // perf=<秒>: 撮った後、その秒数だけ戦闘を回し続ける (描画の重さの計測。GPU 時間の記録は PerfProbe = P08)。2026-09-30 HD-2D 見本 P01
            if (HD2DFlags.Perf > 0f) yield return PerfLoop(g, perfBase, HD2DFlags.Perf);
        }

        // ---- perf=<秒> (2026-09-30 HD-2D 見本 P01) ----
        // 3秒ごとに札を1枚打つ (打てる札が無ければ手番を終える。確認の窓は温存・占術は全部残す)。
        // 毎ターンの頭に自分のブロックを 999 にする (既存の pblock= と同じ状態の書き換え。エンジンのルールは触らない) = 負けない。
        // 決着したら (勝ち・負け・戦闘の外へ出た) 同じ STATE の盤面 (状態へ跳んだ直後の RunState) へ跳び直す = 終わらない戦闘。
        // 時間は実時間で数える (det の captureFramerate の下でも「秒」が実時間になるように)。det と一緒に使うと計測の意味が無いので警告だけ出す
        /// <summary>perf の戦闘を回している間 true (PerfProbe などが「計測の区間」を知るため)</summary>
        public static bool PerfRunning { get; private set; }
        /// <summary>perf の区間で打った札・終えた手番・跳び直しの数 (ログと PerfProbe 用)</summary>
        public static int PerfPlays, PerfTurns, PerfRejumps;

        IEnumerator PerfLoop(GameRoot g, RunState perfBase, float seconds)
        {
            if (Det) Debug.LogWarning("[Autopilot] perf: det と一緒 (captureFramerate=60 で描画が実時間に縛られない)。計測は det なしで");
            PerfRunning = true; PerfPlays = 0; PerfTurns = 0; PerfRejumps = 0;
            float t0 = Time.realtimeSinceStartup, end = t0 + seconds, next = t0 + 3f;
            int lastTurn = -1;
            Debug.Log("[Autopilot] perf start secs=" + seconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " flags=" + HD2DFlags.Describe());
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                if (Time.realtimeSinceStartup < next) continue;
                next = Time.realtimeSinceStartup + 3f;
                if (GameObject.Find("inputblock") != null) continue;   // 演出の順送りの途中 (次の3秒で)
                bool inCombat = g.Rs != null && g.Rs.Phase == RunPhases.Combat && g.Rs.Combat != null;
                if (!inCombat)
                {
                    if (perfBase == null || perfBase.Phase != RunPhases.Combat || perfBase.Combat == null) continue;   // 戦闘でない STATE は描画を回すだけ
                    // 決着した → 同じ盤面へ跳び直す
                    CardPopup.Close(); Tooltip.Hide();
                    g.Pending = null; g.ModeChoiceUid = null; g.GearPending = null;
                    Feedback.RatingOpen = false;   // 決着の評価の窓 (出ていれば) を閉じる
                    g.Rs = null; g.Rebuild();   // 前の戦闘の画面 (報酬・敗北) を掃除して、次の組み立てを「新しい戦闘」にする
                    yield return null;
                    g.Rs = perfBase;
                    Presenter.MarkSeen(perfBase.Combat);
                    g.Rebuild();
                    lastTurn = -1; PerfRejumps++;
                    continue;
                }
                var st = g.Rs.Combat;
                if (st.Phase == CombatPhases.AwaitingReaction) { g.DoCombat(new Command_ConfirmReaction { Fire = false }); continue; }
                if (st.PendingScry != null) { ClearScry(g); continue; }
                if (st.Phase != CombatPhases.PlayerTurn) continue;
                if (st.Turn != lastTurn)
                {   // 毎ターンの頭: ブロック 999 (pblock= と同じ書き換え)
                    lastTurn = st.Turn; PerfTurns++;
                    g.Rs = g.Rs with { Combat = st with { Player = st.Player with { Block = 999 } } };
                    g.Rebuild();
                    continue;   // 札は次の3秒で
                }
                if (g.Pending != null) { g.CancelPending(); }
                // 打てる札 (AutoBattle と同じ選び方: ダメージ札を優先。選択式・追加コスト・仕込み札は打たない)
                CardInstance pick = null; int target = -1;
                for (int i = 0; i < st.Enemies.Count; i++) if (st.Enemies[i].Hp > 0) { target = i; break; }
                foreach (var c in st.Player.Hand)
                {
                    if (c.Def.Type == "reaction" || (c.Def.Modes != null && c.Def.Modes.Count > 0) || c.Def.DiscardCost.HasValue || c.Def.ExhaustCost.HasValue) continue;
                    int cost = c.Def.Cost;
                    try { cost = Effects.EffectiveCost(st, c); } catch (Exception) { }
                    if (!Effects.IsPlayableFromHand(c, st) || cost > st.Player.Energy || (c.Def.LightCost ?? 0) > (st.Player.Light ?? 0)) continue;
                    bool dmg = c.Def.Effects.Any(e => e.Effect == "dealDamage" && e.Trigger == "onPlay");
                    if (pick == null || (dmg && !pick.Def.Effects.Any(e => e.Effect == "dealDamage" && e.Trigger == "onPlay"))) pick = c;
                }
                if (pick != null)
                {
                    g.PreferredTarget = target;
                    g.BeginPlay(pick, null);
                    if (g.Pending != null && g.Pending.NextNeed() == "target" && target >= 0) g.OnEnemyClicked(target);
                    if (g.Pending != null) { g.CancelPending(); pick = null; }   // 対象以外を選ぶ札 (捨て札・山札から選ぶ) は打たない
                    if (g.Error != null) { g.Error = null; pick = null; }
                    if (pick != null) { PerfPlays++; continue; }
                }
                ClearScry(g);
                g.DoCombat(new Command_EndTurn());
            }
            PerfRunning = false;
            Debug.Log("[Autopilot] perf end secs=" + (Time.realtimeSinceStartup - t0).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " plays=" + PerfPlays + " turns=" + PerfTurns + " rejumps=" + PerfRejumps);
        }

        /// <summary>ランを始めて即 工房の状態に差し替えて撮る (⭐レシピの相手札の光・結果の札)。run 巡回は強個体戦で時間切れになりやすいので単独の口</summary>
        IEnumerator WorkshopOnly(GameRoot g)
        {
            g.SetSeed(_seed);
            g.StartRun();
            if (g.Rs == null) { yield return Shot("no-run"); yield break; }
            g.Rs = g.Rs with { Phase = RunPhases.Workshop, Combat = null };
            g.WorkshopA = -1; g.WorkshopB = -1; g.Rebuild(); yield return Shot("workshop-none");   // 未選択: レシピ対の札が全部光る
            g.WorkshopA = 0; g.WorkshopB = -1; g.Rebuild(); yield return Shot("workshop-star");   // 1枚目だけ: 相手札だけが光り、候補の名前
            g.WorkshopA = 0;
            for (int i = 1; i < g.Rs.Deck.Count; i++) if (g.Rs.Deck[i].Def.Id == "green_guard") { g.WorkshopB = i; break; }
            g.Rebuild(); yield return Shot("workshop-recipe");   // 打撃×防御 = レシピ 素振り
        }

        /// <summary>ラン画面の走査 (M3): タイトル→マップ→戦闘は自動で勝つ→報酬/焚き火/店/工房/?/レリックを踏んだ順に撮る。最後に敗北画面</summary>
        IEnumerator RunTour(GameRoot g)
        {
            g.SetSeed(_seed);
            yield return Shot("title");
            g.StartRun();
            if (g.Rs != null && g.Rs.Phase == RunPhases.Departure)
            {   // 出立の店 (2026-09-24): 撮ってから既定 (サービスを1つ買う→店を出る) を繰り返す
                yield return Shot("departure");
                for (int k = 0; k < 6 && g.Rs != null && g.Rs.Phase == RunPhases.Departure && g.Error == null; k++) g.Do(DeckRogue.Engine.Run.DefaultDepartureCommand(g.Rs));
            }
            yield return Shot("map");
            var seen = new HashSet<string>();
            var wish = new List<string> { MapNodeTypes.Event, MapNodeTypes.Shop, MapNodeTypes.Workshop, MapNodeTypes.Campfire, MapNodeTypes.Treasure, MapNodeTypes.Elite, MapNodeTypes.Battle };
            for (int step = 0; step < 60 && g.Rs != null; step++)
            {
                var rs = g.Rs;
                if (rs.Phase == RunPhases.Map)
                {
                    var choices = DeckRogue.Engine.Run.NextChoices(rs);
                    if (choices.Count == 0) break;
                    int pick = choices[0];
                    int bestRank = 999;
                    int nextRow = rs.Row + 1;
                    for (int i = 0; i < choices.Count; i++)
                    {
                        if (nextRow < 0 || nextRow >= rs.Map.Count || choices[i] >= rs.Map[nextRow].Count) continue;
                        string type = rs.Map[nextRow][choices[i]].Type;
                        int rank = wish.IndexOf(type);
                        if (rank < 0) rank = 50;
                        if (seen.Contains(type)) rank += 100;
                        if (rank < bestRank) { bestRank = rank; pick = choices[i]; }
                    }
                    if (nextRow >= 0 && nextRow < rs.Map.Count && pick < rs.Map[nextRow].Count) seen.Add(rs.Map[nextRow][pick].Type);
                    g.Do(new RunCommand_ChooseNode { Col = pick });
                    continue;
                }
                if (rs.Phase == RunPhases.Combat) { yield return AutoBattle(g); continue; }
                string ph = rs.Phase;
                if (!seen.Contains("shot:" + ph))
                {
                    seen.Add("shot:" + ph);
                    yield return Shot(ph);
                    if (ph == RunPhases.Campfire) { g.SubMode = "forge"; g.Rebuild(); CampfireScreen.PreviewFirst(g); yield return Shot("campfire-forge"); g.SubMode = null; g.Rebuild(); }
                    if (ph == RunPhases.Shop) { g.ShopMode = "upgrade"; g.Rebuild(); CampfireScreen.PreviewFirst(g); yield return Shot("shop-upgrade"); g.ShopMode = null; g.Rebuild(); }
                    if (ph == RunPhases.Workshop && rs.Deck.Count >= 2)
                    {
                        g.WorkshopA = 0; g.WorkshopB = 1;
                        for (int i = 1; i < rs.Deck.Count; i++) if (rs.Deck[i].Def.Id != rs.Deck[0].Def.Id) { g.WorkshopB = i; break; }
                        g.Rebuild(); yield return Shot("workshop-preview"); g.WorkshopA = -1; g.WorkshopB = -1; g.Rebuild();
                    }
                    if (ph == RunPhases.Map) { }
                    g.ViewDeck = true; g.Rebuild();
                    if (ph == RunPhases.Reward) yield return Shot("reward-deck");
                    g.ViewDeck = false; g.Rebuild();
                }
                switch (ph)
                {
                    case RunPhases.Reward:
                        // ギア (2026-09-17): 提示があれば先に取る (満杯なら見送る)。札は残っていれば1枚目、無ければ見送る
                        if (g.Rs.GearOption != null) g.Do(DeckRogue.Engine.Run.GearFull(g.Rs) ? (RunCommand)new RunCommand_SkipGear() : new RunCommand_TakeGear());
                        else if (g.Rs.RewardOptions != null && g.Rs.RewardOptions.Count > 0) g.Do(new RunCommand_PickReward { Index = 0 });
                        else g.Do(new RunCommand_SkipReward());
                        break;
                    case RunPhases.RelicReward: g.Do(new RunCommand_PickRelic { Index = 0 }); break;
                    case RunPhases.RelicChoose: g.Do(new RunCommand_RelicChooseCards { Indices = new List<int>() }); break;
                    case RunPhases.Departure: g.Do(DeckRogue.Engine.Run.DefaultDepartureCommand(g.Rs)); break;   // 出立の店 (2026-09-24): 既定=サービスを1つ買う→店を出る (出立の間は繰り返し呼ばれる)
                    case RunPhases.Campfire: g.Do(new RunCommand_CampfireRest()); break;
                    case RunPhases.Shop: g.Do(new RunCommand_ShopLeave()); break;
                    case RunPhases.Workshop: g.Do(new RunCommand_WorkshopSkip()); break;
                    case RunPhases.Event:
                    {
                        // 既定の選択 (2026-09-14 無料の「立ち去る」撤去): 後ろから「代償の無い」→「致死でない」選択肢
                        RunCommand_EventChoice ec = null;
                        try { ec = DeckRogue.Engine.Run.DefaultEventChoice(rs); } catch (Exception) { }
                        g.Do(ec ?? new RunCommand_EventChoice { Index = 0 });
                        break;
                    }
                    case RunPhases.Won:
                    case RunPhases.Lost:
                        step = 999; break;
                    default: step = 999; break;
                }
                bool all = seen.Contains("shot:" + RunPhases.Reward) && seen.Contains("shot:" + RunPhases.Campfire) && seen.Contains("shot:" + RunPhases.Shop)
                    && seen.Contains("shot:" + RunPhases.Event) && seen.Contains("shot:" + RunPhases.Workshop) && seen.Contains("shot:" + RunPhases.RelicReward);
                if (all) break;
            }
            // 経路上に無かった画面は状態を差し替えて撮る (工房はデッキだけあれば描ける)
            if (g.Rs != null && !seen.Contains("shot:" + RunPhases.Workshop) && g.Rs.Phase != RunPhases.Combat)
            {
                var keep = g.Rs;
                g.Rs = g.Rs with { Phase = RunPhases.Workshop, Combat = null };
                g.WorkshopA = 0; g.WorkshopB = -1; g.Rebuild(); yield return Shot("workshop-star");   // 1枚目だけ選んだ状態 = ⭐レシピの相手札が光る (2026-09-12)
                g.WorkshopA = 0; g.WorkshopB = 1;
                for (int i = 1; i < g.Rs.Deck.Count; i++) if (g.Rs.Deck[i].Def.Id != g.Rs.Deck[0].Def.Id) { g.WorkshopB = i; break; }
                g.Rebuild();
                yield return Shot("workshop-forced");
                g.WorkshopA = -1; g.WorkshopB = -1;
                g.Rs = keep;
            }
            if (g.Rs != null && g.Rs.Phase != RunPhases.Lost && g.Rs.Phase != RunPhases.Won)
            {
                g.Rs = g.Rs with { Phase = RunPhases.Lost, Combat = null };
                g.Rebuild();
                yield return Shot("lost");
            }
            Debug.Log("[Autopilot] run tour: " + string.Join(",", seen));
        }

        /// <summary>戦闘を自動で進める: 使える攻撃札を撃ち、ターン終了。リアクション確認は温存。20ターンで諦める</summary>
        IEnumerator AutoBattle(GameRoot g)
        {
            for (int turn = 0; turn < 20 && g.Rs != null && g.Rs.Phase == RunPhases.Combat; turn++)
            {
                var skip = new HashSet<string>();
                for (int guard = 0; guard < 30 && g.Rs != null && g.Rs.Phase == RunPhases.Combat; guard++)
                {
                    var st = g.Rs.Combat;
                    if (st.Phase != CombatPhases.PlayerTurn) break;
                    CardInstance pick = null;
                    int target = -1;
                    for (int i = 0; i < st.Enemies.Count; i++) if (st.Enemies[i].Hp > 0) { target = i; break; }
                    foreach (var c in st.Player.Hand)
                    {
                        if (skip.Contains(c.Uid) || c.Def.Type == "reaction") continue;
                        if (c.Def.Modes != null && c.Def.Modes.Count > 0) continue;
                        if (c.Def.DiscardCost.HasValue || c.Def.ExhaustCost.HasValue) continue;
                        int cost = c.Def.Cost;
                        try { cost = Effects.EffectiveCost(st, c); } catch (Exception) { }
                        if (!Effects.IsPlayableFromHand(c, st) || cost > st.Player.Energy || (c.Def.LightCost ?? 0) > (st.Player.Light ?? 0)) continue; // 灯コスト (白 2026-09-20)
                        bool dmg = c.Def.Effects.Any(e => e.Effect == "dealDamage" && e.Trigger == "onPlay");
                        if (pick == null || (dmg && !pick.Def.Effects.Any(e => e.Effect == "dealDamage" && e.Trigger == "onPlay"))) pick = c;
                    }
                    if (pick == null) break;
                    g.PreferredTarget = target;
                    g.BeginPlay(pick, null);
                    if (g.Pending != null) { g.CancelPending(); skip.Add(pick.Uid); continue; }
                    if (g.Error != null) { skip.Add(pick.Uid); g.Error = null; }
                    yield return null;
                }
                if (g.Rs == null || g.Rs.Phase != RunPhases.Combat) break;
                if (g.Rs.Combat.Phase == CombatPhases.PlayerTurn) { ClearScry(g); g.DoCombat(new Command_EndTurn()); }
                yield return WaitPresentation();
                for (int k = 0; k < 6 && g.Rs != null && g.Rs.Phase == RunPhases.Combat && g.Rs.Combat.Phase == CombatPhases.AwaitingReaction; k++)
                {
                    g.DoCombat(new Command_ConfirmReaction { Fire = false });
                    yield return WaitPresentation();
                }
                yield return WaitPresentation();
            }
            if (g.Rs != null && g.Rs.Phase == RunPhases.Combat)
            {
                // 勝てなかった: 戦闘を打ち切って負け扱いにはせず、マップに戻れないので走査を終える
                Debug.Log("[Autopilot] auto battle gave up");
            }
        }

        /// <summary>セットアップ→ラン開始→マップ→最初のノード→戦闘 (ターン終了×2)。戦闘以外に入ったらその画面も撮る</summary>
        IEnumerator Tour(GameRoot g)
        {
            yield return Shot("setup");
            g.SetSeed(_seed);
            g.StartRun();
            if (g.Rs != null && g.Rs.Phase == RunPhases.Departure)
            {   // 出立の店 (2026-09-24): 撮ってから既定 (サービスを1つ買う→店を出る) を繰り返す
                yield return Shot("departure");
                for (int k = 0; k < 6 && g.Rs != null && g.Rs.Phase == RunPhases.Departure && g.Error == null; k++) g.Do(DeckRogue.Engine.Run.DefaultDepartureCommand(g.Rs));
            }
            yield return Shot("map");
            for (int i = 0; i < 8 && g.Rs != null && g.Rs.Phase != RunPhases.Combat; i++)
            {
                var rs = g.Rs;
                RunCommand cmd = null;
                switch (rs.Phase)
                {
                    case RunPhases.Map: cmd = new RunCommand_ChooseNode { Col = DeckRogue.Engine.Run.NextChoices(rs)[0] }; break;
                    case RunPhases.Departure: cmd = DeckRogue.Engine.Run.DefaultDepartureCommand(rs); break;   // 出立の店 (2026-09-24): 買う→出る
                    case RunPhases.Event:
                    {
                        try { cmd = DeckRogue.Engine.Run.DefaultEventChoice(rs); } catch (Exception) { cmd = new RunCommand_EventChoice { Index = 0 }; }
                        break;
                    }
                    case RunPhases.Shop: cmd = new RunCommand_ShopLeave(); break;
                    case RunPhases.Campfire: cmd = new RunCommand_CampfireRest(); break;
                    case RunPhases.Workshop: cmd = new RunCommand_WorkshopSkip(); break;
                    case RunPhases.RelicReward: cmd = new RunCommand_SkipRelic(); break;
                    case RunPhases.RelicChoose: cmd = new RunCommand_RelicChooseCards { Indices = new List<int>() }; break;
                    case RunPhases.Reward: cmd = rs.GearOption != null ? (RunCommand)new RunCommand_SkipGear() : new RunCommand_SkipReward(); break;   // ギアが残っていれば先に片付ける (2026-09-17)
                }
                if (cmd == null) break;
                g.Do(cmd);
                yield return Shot(rs.Phase + "-" + (g.Rs != null ? g.Rs.Phase : "null"));
            }
            if (g.Rs != null && g.Rs.Phase == RunPhases.Combat)
            {
                yield return Shot("combat-turn1");
                for (int turn = 0; turn < 2 && g.Rs != null && g.Rs.Phase == RunPhases.Combat; turn++)
                {
                    var c = g.Rs.Combat;
                    if (c != null && c.Phase == CombatPhases.AwaitingReaction)
                    {
                        g.DoCombat(new Command_ConfirmReaction { Fire = false });
                        yield return Shot("combat-confirm");
                    }
                    { ClearScry(g); g.DoCombat(new Command_EndTurn()); }
                    yield return WaitPresentation();
                    yield return Shot("combat-after-end" + (turn + 1));
                }
            }
            yield return Shot("last");
        }
    
        // ---- -statesfile <file> (2026-09-30 HD-2D 見本 P01) ----
        // 1回の起動で、ファイルの行を順に撮る (まとめて撮る専用。合否の門の比較は1行1起動で撮る = 計画 §8 審査1)。
        // ファイルの書き方は scripts/hd2d-states/baseline.txt と同じ: 1行 = 名前|STATE、# で始まる行と空行は読み飛ばす。
        // 「名前|PC|STATE」(W0 の list-all の形) も読む (真ん中は無視。スマホ相当の寸法と UI の倍率は起動引数なので1回の起動では切り替えられない = PH の行は警告)。
        // 撮った PNG は「名前-その行の何枚目.png」(1行1起動で撮って shoot.sh が付け直す名前と同じ)。dumplayout=1 なら同じ名前の .layout.json。
        // 各行の頭: 画面を白紙 (タイトル) に戻し、GameRoot の見る物の旗・ポップアップ・uionly の設定・窓の大きさ・時間の刻みを戻し、
        // HD2DFlags.Reset → 起動引数の -hd2d → その行の STATE の順に旗を当てる。静的な状態 (Stage・Tween・Presenter の中) は消さない (計画の裁定)
        IEnumerator StatesFile(GameRoot g, string path)
        {
            string[] lines = null;
            try { lines = File.ReadAllLines(path); }
            catch (Exception ex) { Debug.LogError("[Autopilot] states: ファイルを読めない " + path + " " + ex.Message); }
            if (lines == null) yield break;
            int origW = Screen.width, origH = Screen.height;
            _diff0 = g.Difficulty;
            int done = 0, failed = 0;
            var names = new HashSet<string>();
            for (int li = 0; li < lines.Length; li++)
            {
                var line = lines[li].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var parts = line.Split('|');
                string name = parts.Length >= 2 ? parts[0].Trim() : "line" + (li + 1);
                string spec = parts[parts.Length - 1].Trim();
                if (parts.Length >= 3 && parts[1].Trim().ToUpperInvariant() == "PH")
                    Debug.LogWarning("[Autopilot] states: " + name + " は PH (スマホ相当) の行。寸法と UI の倍率は起動引数なので、この起動の寸法のまま撮る");
                name = SafeFileName(name);
                if (!names.Add(name)) { int k = 2; while (!names.Add(name + "_" + k)) k++; name = name + "_" + k; }   // 同じ名前の行は _2, _3 … (上書きしない)

                yield return ResetForLine(g, origW, origH);
                HD2DFlags.Reset();
                HD2DFlags.ApplyLaunchArgs();   // StateJump の頭で、この行の STATE を当てる
                _linePrefix = name; _lineN = 0;
                Debug.Log("[Autopilot] states: 行 " + (li + 1) + " " + name + " | " + spec);
                bool err = false;
                yield return Guard(StateJump(g, spec), ex => { err = true; Debug.LogError("[Autopilot] states: " + name + " で例外 " + ex); });
                if (err) failed++; else done++;
                _linePrefix = null;
            }
            yield return ResetForLine(g, origW, origH);
            Debug.Log("[Autopilot] states: 終わり 撮った行 " + done + "・例外 " + failed);
        }

        /// <summary>-statesfile の行の頭で、前の行の見た目の状態を戻す (Rs を捨ててタイトルへ = 次の戦闘は新しい BattleView で組む)</summary>
        IEnumerator ResetForLine(GameRoot g, int origW, int origH)
        {
            Time.captureFramerate = DetFramerate;
            if (Det) UnityEngine.Random.InitState(DetRandomSeed);   // 行ごとに乱数を頭から (1行1起動に近づける)
            RestoreCaptureMode();
            CardPopup.Close(); Tooltip.Hide();
            g.Pending = null; g.PreferredTarget = -1; g.ModeChoiceUid = null;
            g.WorkshopA = -1; g.WorkshopB = -1; g.ShopMode = null; g.SubMode = null;
            g.EventChoiceIndex = -1; g.DepartureChoiceIndex = -1; g.RelicChoosePicks.Clear();
            g.ShowUpgraded = false; g.GridPickKey = null; g.GridPickIndex = -1; g.ShowLog = false; g.ViewPile = null;
            g.ViewDeck = false; g.ViewRelics = false; g.ViewMap = false; g.MenuOpen = false; g.SettingsOpen = false; g.Confirm = null;
            g.DoodleMode = false; g.Doodles.Clear();
            g.GearPending = null; g.GearSwap = null; g.GearMore = false; g.HearthChoice = false; g.RetainChoice = null; g.ScryDiscard.Clear();
            g.Notice = null; g.Error = null; g.PhaseShownKind = -1;
            if (_diff0 > 0) g.Difficulty = _diff0;   // 前の行の difficulty= を持ち越さない (起動した時の値へ)
            Feedback.MemoOpen = false; Feedback.RatingOpen = false; Feedback.Silent = false;
            if (Screen.width != origW || Screen.height != origH)
            {   // resize= の行の後
                Screen.SetResolution(origW, origH, false);
                for (int i = 0; i < 12; i++) yield return null;
            }
            g.Rs = null; g.Rebuild();   // 画面を掃除 (戦闘中の Rebuild は掃除しないので、一度タイトルへ)
            Presenter.Reset();
            for (int i = 0; i < 3; i++) yield return null;   // Destroy はフレームの終わり
        }

        /// <summary>入れ子のコルーチンを平らにして回し、例外が出たらそこで止めて onError に渡す (-statesfile で1行の失敗が残りの行を止めないように)</summary>
        static IEnumerator Guard(IEnumerator root, Action<Exception> onError)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var top = stack.Peek();
                bool moved; object cur = null;
                try { moved = top.MoveNext(); if (moved) cur = top.Current; }
                catch (Exception ex) { onError(ex); yield break; }
                if (!moved) { stack.Pop(); continue; }
                var nested = cur as IEnumerator;
                if (nested != null) { stack.Push(nested); continue; }
                yield return cur;
            }
        }

        static string SafeFileName(string s)
        {
            var bad = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder();
            foreach (char c in s) sb.Append(Array.IndexOf(bad, c) >= 0 || c == ':' || c == '*' || c == '?' || c == '"' || c == '<' || c == '>' || c == '|' || c == '/' || c == '\\' ? '_' : c);
            var r = sb.ToString().Trim();
            return r.Length > 0 ? r : "line";
        }

        // ---- hideui / hidezone の当て方 (2026-10-02 三周目 直しの輪1: 連写の前と1枚撮りの直前の両方から呼ぶ。何度当てても同じ) ----

        /// <summary>hideui=1: 舞台と絵 (敵・リーダー・人形の絵と狙いの輪) だけを残して UI を消す。箱庭の時だけ手札の後ろの暗幕も消す
        /// (三周目 R12: 二周目の hideui は手前を暗幕ごと測っていた。今の舞台の hideui は W5・二周目と画素で比べるので今のまま)</summary>
        static void HideUiNow(GameRoot g)
        {
            if (g == null || g.ScreenRoot == null || g.Battle == null) return;
            if (g.Battle.UiLayer != null) g.Battle.UiLayer.gameObject.SetActive(false);
            if (g.Battle.HandLayer != null) g.Battle.HandLayer.gameObject.SetActive(false);
            { var desk = BattleScreen.Hd2dLayout && g.Battle.FieldLayer != null ? g.Battle.FieldLayer.Find("desk-shade") : null; if (desk != null) desk.gameObject.SetActive(false); }
            foreach (var rt in g.ScreenRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.parent == null || !(rt.parent.name.StartsWith("enemy") || rt.parent.name == "player" || rt.parent.name.StartsWith("doll:"))) continue;
                if (rt.name == "sprite" || rt.name == "ring") continue;
                rt.gameObject.SetActive(false);
            }
        }

        /// <summary>hidezone=1: 伏せ場と (スマホの) 置物の欄を消す</summary>
        static void HideZoneNow(GameRoot g)
        {
            if (g == null || g.ScreenRoot == null) return;
            foreach (var rt in g.ScreenRoot.GetComponentsInChildren<RectTransform>(true)) if (rt.name == "setzone" || rt.name == "chips" && rt.parent != null && rt.parent.name == "player") rt.gameObject.SetActive(false);
        }

        // ---- uionly / unitsonly (2026-09-30 HD-2D 見本 P01) ----
        // uionly=1: 舞台を描かない (画面に出すカメラは何も写さず背景のマゼンタ (1,0,1) だけ)。UI (重ねのキャンバス) だけが乗る = UI の型抜き。
        // unitsonly=1: キャラの板 (レイヤー8) だけを写し、キャンバスを隠す = キャラの型抜き。両方立っていたら unitsonly。
        // どちらも後処理とぼかしを切る (renderPostProcessing=false・TiltShiftSettings.Enabled=false)・背景は SolidColor のマゼンタ。
        // P11 が板をレイヤー8に置くまで (stage=old のまま)、unitsonly は名前が「unit-」の板をその場でレイヤー8へ移して写す (撮影の時だけ。行の頭で戻す)。
        // Shot の頭と撮る直前に毎回当てる (幕の描き直しが背景の色を戻すため)。立っていなければ何もしない = 普段の撮影は1画素も変わらない
        struct CamSave { public Camera Cam; public CameraClearFlags Flags; public Color Bg; public int Mask; public bool Post; public bool HasData; }
        static readonly List<CamSave> _capCams = new List<CamSave>();
        static readonly List<Canvas> _capCanvases = new List<Canvas>();
        static readonly List<KeyValuePair<GameObject, int>> _capLayers = new List<KeyValuePair<GameObject, int>>();
        static bool _capOn;
        static bool _capTilt;
        static readonly Color CaptureMagenta = new Color(1f, 0f, 1f, 1f);
        const int UnitLayer = HD2DLayers.StageUnit;

        static void ApplyCaptureMode()
        {
            bool units = HD2DFlags.UnitsOnly, ui = HD2DFlags.UiOnly && !units;
            if (!ui && !units) { RestoreCaptureMode(); return; }
            if (!_capOn) { _capOn = true; _capTilt = TiltShiftSettings.Enabled; }
            TiltShiftSettings.Enabled = false;
            foreach (var cam in Camera.allCameras)
            {
                if (cam == null || cam.targetTexture != null) continue;   // 水面の鏡像など RT へ描くカメラは触らない
                int idx = _capCams.FindIndex(x => x.Cam == cam);
                if (idx < 0)
                {
                    var s = new CamSave { Cam = cam, Flags = cam.clearFlags, Bg = cam.backgroundColor, Mask = cam.cullingMask };
                    try { var d = cam.GetUniversalAdditionalCameraData(); s.Post = d.renderPostProcessing; s.HasData = true; } catch (Exception) { }
                    _capCams.Add(s);
                }
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = CaptureMagenta;
                cam.cullingMask = units ? (1 << UnitLayer) : 0;
                try { cam.GetUniversalAdditionalCameraData().renderPostProcessing = false; } catch (Exception) { }
            }
            if (units)
            {
                foreach (var cv in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
                {
                    if (cv == null || !cv.isRootCanvas || !cv.enabled) continue;
                    cv.enabled = false; _capCanvases.Add(cv);
                }
                bool any = false;
                foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)) if (r != null && r.enabled && r.gameObject.activeInHierarchy && r.gameObject.layer == UnitLayer) { any = true; break; }
                if (!any)
                    foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
                    {
                        if (r == null || !r.gameObject.name.StartsWith("unit-", StringComparison.Ordinal)) continue;
                        _capLayers.Add(new KeyValuePair<GameObject, int>(r.gameObject, r.gameObject.layer));
                        r.gameObject.layer = UnitLayer;
                    }
            }
        }

        static void RestoreCaptureMode()
        {
            if (!_capOn) return;
            foreach (var s in _capCams)
            {
                if (s.Cam == null) continue;
                s.Cam.clearFlags = s.Flags; s.Cam.backgroundColor = s.Bg; s.Cam.cullingMask = s.Mask;
                if (s.HasData) { try { s.Cam.GetUniversalAdditionalCameraData().renderPostProcessing = s.Post; } catch (Exception) { } }
            }
            foreach (var cv in _capCanvases) if (cv != null) cv.enabled = true;
            foreach (var p in _capLayers) if (p.Key != null) p.Key.layer = p.Value;
            _capCams.Clear(); _capCanvases.Clear(); _capLayers.Clear();
            TiltShiftSettings.Enabled = _capTilt;
            _capOn = false;
        }

        // ---- dumplayout=1 (2026-09-30 HD-2D 見本 P01) ----
        // 撮った PNG と同じ名前の .layout.json (01-state-combat.png → 01-state-combat.layout.json) に、撮るのと同じフレームの矩形を書く。
        // 形 (schema "hd2d-layout/1"。読むのは P08 の hd2d-layout-check.py・hd2d-measure.py・hd2d-seatfit.py):
        //   png・name・frame・time・state (その STATE)・flags (HD2DFlags.Snapshot)
        //   screen {w,h} (= PNG の寸法)・canvas {w,h,scale,phone,overlay}・statusLineY {canvas, px} (足元の線。canvas は下から・px は PNG の上から)
        //   units [ {key, kind(enemy|player|doll), index, id, alive, hp, px, sprite, strip, intent, ring, feetOffset} ]
        //   hand [ {name, px, back, flying, raised, body, digits [[x,y,w,h]…], digitsBottomGap (本文の数字の下端から画面の下端まで・px)} ]
        //        (back = 裏が見えている・flying = 扇に休んでいない (飛んでいる途中・つかんでいる)・raised = 触れて上がった。2026-10-02 三周目 直しの輪1)
        //   anchors {topbar, phase, gold, energy, light, mana, setlabel, gearzone, setslotN, gear:<uid> …} (無い物は書かない)
        //   nodes [ {path, px, text?, fs?, color?, digits?} ] = 画面 (screen/…) と重ねの層 (popup/…) の見えている RectTransform 全部 (上限 4000)
        //   stage {camera: Stage.DebugCameraInfo(), unitBoxes: Stage.DebugUnitBoxes()} (P10・P11 が中身を書く。無ければ null)
        //   extra {<名前>: HD2DFlags.LayoutDumpers[名前]() …}・errors [文字列…] (記録の途中で失敗した所)
        // 矩形 px は [x, y, w, h] = PNG の画素 (左上が原点・y は下向き)。digits は TMP の文字の箱から数字の続き (1行の中) ごとに作った箱。
        // fs は文字の大きさ (画面の px。<size=130%> 込みの最大)。color は文字の色 (#RRGGBBAA)
        static class LayoutDump
        {
            const int MaxNodes = 4000, MaxDepth = 12;
            static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
            static readonly System.Text.RegularExpressions.Regex Tags = new System.Text.RegularExpressions.Regex("<[^>]*>");

            internal static void Write(Autopilot ap, string pngPath, string name)
            {
                string jsonPath = Path.Combine(Path.GetDirectoryName(pngPath) ?? "", Path.GetFileNameWithoutExtension(pngPath) + ".layout.json");
                var errors = new List<object>();
                var root = new Dictionary<string, object>();
                try
                {
                    var g = GameRoot.I;
                    root["schema"] = "hd2d-layout/1";
                    root["png"] = Path.GetFileName(pngPath);
                    root["name"] = name;
                    root["frame"] = Time.frameCount;
                    root["time"] = Time.time;
                    root["state"] = ap != null ? ap._stateSpec : null;
                    var flags = new Dictionary<string, object>();
                    foreach (var p in HD2DFlags.Snapshot()) flags[p.Key] = p.Value;
                    root["flags"] = flags;
                    root["screen"] = new Dictionary<string, object> { { "w", Screen.width }, { "h", Screen.height } };
                    Canvas canvas = null;
                    if (g != null && g.ScreenRoot != null) { canvas = g.ScreenRoot.GetComponentInParent<Canvas>(); if (canvas != null) canvas = canvas.rootCanvas; }
                    Camera cvCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                    float scale = canvas != null ? canvas.scaleFactor : 1f;
                    Section(errors, "canvas", () =>
                    {
                        var cs = g != null && g.ScreenRoot != null ? BattleScreen.CanvasSize(g.ScreenRoot) : new Vector2(Screen.width, Screen.height);
                        root["canvas"] = new Dictionary<string, object> { { "w", R(cs.x) }, { "h", R(cs.y) }, { "scale", scale }, { "phone", UiKit.Phone }, { "overlay", cvCam == null } };
                    });
                    var st = g != null && g.Rs != null ? g.Rs.Combat : null;
                    bool battle = g != null && g.Battle != null && st != null && g.Rs.Phase == RunPhases.Combat;
                    if (battle)
                        Section(errors, "statusLineY", () =>
                        {
                            float y = BattleView.StatusLineY;
                            root["statusLineY"] = new Dictionary<string, object> { { "canvas", R(y) }, { "px", R(Screen.height - y * scale) } };
                        });
                    // キャラの入れ物 (敵・リーダー・人形) と主な子
                    var units = new List<object>();
                    if (battle)
                    {
                        Section(errors, "units.enemy", () =>
                        {
                            for (int i = 0; i < st.Enemies.Count; i++)
                                AddUnit(units, g.Anchor("enemy" + i), "enemy" + i, "enemy", i, st.Enemies[i].EnemyId, st.Enemies[i].Hp > 0, st.Enemies[i].Hp, cvCam);
                        });
                        Section(errors, "units.player", () => AddUnit(units, g.Anchor("player"), "player", "player", 0, g.LeaderId, st.Player.Hp > 0, st.Player.Hp, cvCam));
                        Section(errors, "units.doll", () =>
                        {
                            var dolls = g.Battle.StageDolls(st);
                            for (int i = 0; i < dolls.Count; i++) AddUnit(units, g.Anchor("doll:" + dolls[i].Uid), "doll:" + dolls[i].Uid, "doll", i, dolls[i].Def != null ? dolls[i].Def.Id : null, true, 0, cvCam);
                        });
                    }
                    root["units"] = units;
                    // 手札と本文の数字
                    var hand = new List<object>();
                    if (battle && g.Battle.HandLayer != null)
                        Section(errors, "hand", () =>
                        {
                            foreach (Transform ch in g.Battle.HandLayer)
                            {
                                var rt = ch as RectTransform;
                                if (rt == null || !rt.gameObject.activeInHierarchy || !rt.name.StartsWith("hand", StringComparison.Ordinal)) continue;
                                var e = new Dictionary<string, object> { { "name", rt.name }, { "px", Px(rt, cvCam) } };
                                // 札の状態 (2026-10-02 三周目 直しの輪1。layout-check の L5 は動いている札を数えない):
                                //   back = 裏が見えている (ドローで山札から飛ぶ途中の前半)・
                                //   flying = 扇に休んでいない (今の手札の札でない＝捨て札・からくり・敵へ飛ぶ途中／裏／倍率が CardScale より小さい＝ドローの途中・PC でつかんだ札)・
                                //   raised = 触れて上がった (倍率が CardScale より大きい。r3 の 1.18 倍)
                                var backT = rt.Find("back");
                                bool back = backT != null && backT.gameObject.activeInHierarchy;
                                bool live = false;
                                for (int hi = 0; hi < g.Battle.HandCount && !live; hi++) { var hcard = g.Battle.CardAt(hi); if (hcard != null && ReferenceEquals(hcard.Rt, rt)) live = true; }
                                float sy = rt.localScale.y;
                                e["back"] = back;
                                e["flying"] = !live || back || sy < BattleScreen.CardScale - 0.01f;
                                e["raised"] = live && !back && sy > BattleScreen.CardScale + 0.01f;
                                // 本文 = 札の根の直下の文字 (CardView の body)。いちばん大きい物
                                TMPro.TMP_Text body = null; float bestArea = -1f;
                                foreach (Transform c in rt)
                                {
                                    var t = c.GetComponent<TMPro.TMP_Text>();
                                    if (t == null || !c.gameObject.activeInHierarchy) continue;
                                    float a = t.rectTransform.rect.width * t.rectTransform.rect.height;
                                    if (a > bestArea) { bestArea = a; body = t; }
                                }
                                if (body != null)
                                {
                                    e["body"] = Px(body.rectTransform, cvCam);
                                    float fs;
                                    var digits = Digits(body, cvCam, scale, out fs);
                                    e["digits"] = digits;
                                    if (digits.Count > 0)
                                    {
                                        float bottom = 0f;
                                        foreach (var d in digits) bottom = Mathf.Max(bottom, d[1] + d[3]);
                                        e["digitsBottomGap"] = R(Screen.height - bottom);
                                        e["digitsFs"] = fs;
                                    }
                                }
                                hand.Add(e);
                            }
                        });
                    root["hand"] = hand;
                    // 名前で引ける部品 (上部バー・エナジー・灯・魔素・からくり・ギア)
                    var anchors = new Dictionary<string, object>();
                    if (g != null)
                        Section(errors, "anchors", () =>
                        {
                            if (g.Battle != null && g.Battle.UiLayer != null) { var tb = g.Battle.UiLayer.Find("topbar") as RectTransform; if (tb != null && tb.gameObject.activeInHierarchy) anchors["topbar"] = Px(tb, cvCam); }
                            var nm = new List<string> { "phase", "gold", "energy", "light", "mana", "setlabel", "gearzone" };
                            for (int i = 0; i < 6; i++) nm.Add("setslot" + i);
                            var rs = g.Rs;
                            if (rs != null) foreach (var gear in DeckRogue.Engine.Run.GearsOf(rs)) nm.Add("gear:" + gear.Uid);
                            foreach (var k in nm) { var a = g.Anchor(k); if (a != null && a.gameObject.activeInHierarchy) anchors[k] = Px(a, cvCam); }
                        });
                    root["anchors"] = anchors;
                    // 見えている矩形の全部 (画面と重ねの層)
                    var nodes = new List<object>();
                    Section(errors, "nodes", () =>
                    {
                        if (g != null && g.ScreenRoot != null) Walk(g.ScreenRoot, "screen", 0, nodes, cvCam, scale);
                        if (g != null && g.PopupLayer != null) Walk(g.PopupLayer, "popup", 0, nodes, cvCam, scale);
                    });
                    root["nodes"] = nodes;
                    if (nodes.Count >= MaxNodes) errors.Add("nodes: 上限 " + MaxNodes + " で打ち切った");
                    // 舞台 (P10・P11 が中身を書く)
                    var stage = new Dictionary<string, object>();
                    Section(errors, "stage.camera", () => stage["camera"] = Stage.DebugCameraInfo());
                    Section(errors, "stage.unitBoxes", () => stage["unitBoxes"] = Stage.DebugUnitBoxes());
                    root["stage"] = stage;
                    // 他のレーンが足した記録
                    var extra = new Dictionary<string, object>();
                    { bool motionOn = StageMotion.On; }   // 段2: 動きの記録の登録を今の幕で読み直す (箱庭の幕を出た後に古い extra.motion を残さない。記録の時だけ)
                    foreach (var p in HD2DFlags.LayoutDumpers.ToList())
                    {
                        var key = p.Key; var fn = p.Value;
                        Section(errors, "extra." + key, () => extra[key] = fn != null ? fn() : null);
                    }
                    root["extra"] = extra;
                }
                catch (Exception ex) { errors.Add("layout: " + ex.Message); }
                root["errors"] = errors;
                try
                {
                    File.WriteAllText(jsonPath, Json.Write(root), new System.Text.UTF8Encoding(false));
                    Debug.Log("[Autopilot] layout " + jsonPath + (errors.Count > 0 ? " errors=" + errors.Count : ""));
                }
                catch (Exception ex) { Debug.LogWarning("[Autopilot] layout.json を書けない " + jsonPath + " " + ex.Message); }
            }

            static void Section(List<object> errors, string what, Action a)
            {
                try { a(); }
                catch (Exception ex) { errors.Add(what + ": " + ex.GetType().Name + " " + ex.Message); }
            }

            static void AddUnit(List<object> units, RectTransform pan, string key, string kind, int index, string id, bool alive, int hp, Camera cvCam)
            {
                var e = new Dictionary<string, object> { { "key", key }, { "kind", kind }, { "index", index }, { "id", id }, { "alive", alive }, { "hp", hp } };
                if (pan != null)
                {
                    e["px"] = Px(pan, cvCam);
                    e["active"] = pan.gameObject.activeInHierarchy;
                    foreach (var child in new[] { "sprite", "strip", "intent-tag", "ring" })
                    {
                        var c = pan.Find(child) as RectTransform;
                        if (c != null && c.gameObject.activeInHierarchy) e[child == "intent-tag" ? "intent" : child] = Px(c, cvCam);
                    }
                }
                e["feetOffset"] = Stage.FeetOffset(key, float.NaN);
                units.Add(e);
            }

            static void Walk(Transform t, string path, int depth, List<object> nodes, Camera cvCam, float scale)
            {
                if (depth > MaxDepth) return;
                for (int i = 0; i < t.childCount && nodes.Count < MaxNodes; i++)
                {
                    var ch = t.GetChild(i);
                    if (!ch.gameObject.activeInHierarchy) continue;
                    var rt = ch as RectTransform;
                    string p = path + "/" + ch.name;
                    if (rt != null)
                    {
                        var px = Px(rt, cvCam);
                        if (px[2] >= 0.5f && px[3] >= 0.5f)
                        {
                            var e = new Dictionary<string, object> { { "path", p }, { "px", px } };
                            var tx = ch.GetComponent<TMPro.TMP_Text>();
                            if (tx != null && tx.enabled)
                            {
                                var s = Tags.Replace(tx.text ?? "", "").Replace("\n", " ");
                                e["text"] = s.Length > 48 ? s.Substring(0, 48) : s;
                                e["color"] = "#" + ColorUtility.ToHtmlStringRGBA(tx.color);
                                float fs;
                                var digits = Digits(tx, cvCam, scale, out fs);
                                e["fs"] = fs > 0f ? fs : R(tx.fontSize * scale);
                                if (digits.Count > 0) e["digits"] = digits;
                            }
                            nodes.Add(e);
                        }
                    }
                    Walk(ch, p, depth + 1, nodes, cvCam, scale);
                }
            }

            /// <summary>TMP の見えている数字 (0-9) の箱を、1行の中の続きごとに1つへまとめる (PNG の画素)。fs = その中の最大の文字の大きさ (画面の px)</summary>
            static List<float[]> Digits(TMPro.TMP_Text t, Camera cvCam, float scale, out float fs)
            {
                var runs = new List<float[]>();
                fs = 0f;
                var info = t.textInfo;
                if (info == null || info.characterInfo == null) return runs;
                float x0 = 0f, y0 = 0f, x1 = 0f, y1 = 0f; int line = -1; bool open = false; int last = -2;
                int n = Mathf.Min(info.characterCount, info.characterInfo.Length);
                var tr = t.rectTransform;
                for (int i = 0; i < n; i++)
                {
                    var ci = info.characterInfo[i];
                    bool digit = ci.isVisible && ci.character >= '0' && ci.character <= '9';
                    if (!digit) continue;
                    var a = RectTransformUtility.WorldToScreenPoint(cvCam, tr.TransformPoint(ci.bottomLeft));
                    var b = RectTransformUtility.WorldToScreenPoint(cvCam, tr.TransformPoint(ci.topRight));
                    float ax = Mathf.Min(a.x, b.x), bx = Mathf.Max(a.x, b.x), ay = Mathf.Min(a.y, b.y), by = Mathf.Max(a.y, b.y);
                    fs = Mathf.Max(fs, R(ci.pointSize * scale));
                    if (open && ci.lineNumber == line && i == last + 1) { x0 = Mathf.Min(x0, ax); x1 = Mathf.Max(x1, bx); y0 = Mathf.Min(y0, ay); y1 = Mathf.Max(y1, by); }
                    else
                    {
                        if (open) runs.Add(new[] { R(x0), R(Screen.height - y1), R(x1 - x0), R(y1 - y0) });
                        x0 = ax; x1 = bx; y0 = ay; y1 = by; line = ci.lineNumber; open = true;
                    }
                    last = i;
                }
                if (open) runs.Add(new[] { R(x0), R(Screen.height - y1), R(x1 - x0), R(y1 - y0) });
                return runs;
            }

            /// <summary>RectTransform の画面の箱 [x, y, w, h] (PNG の画素・左上が原点・y は下向き)</summary>
            internal static float[] Px(RectTransform rt, Camera cvCam)
            {
                var c = new Vector3[4];
                rt.GetWorldCorners(c);
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                for (int i = 0; i < 4; i++)
                {
                    var p = RectTransformUtility.WorldToScreenPoint(cvCam, c[i]);
                    x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y);
                }
                return new[] { R(x0), R(Screen.height - y1), R(x1 - x0), R(y1 - y0) };
            }

            static float R(float v) { return Mathf.Round(v * 10f) / 10f; }
        }

        /// <summary>
        /// 小さな JSON 書き出し (layout.json 用)。null・文字列・真偽・数 (不変の書式。NaN と無限は null)・列挙 (名前)・辞書 (キーは文字列)・並び・
        /// Vector2/3/4・Rect ([x,y,w,h])・Color ([r,g,b,a])・Quaternion・UnityEngine.Object (名前) と、それ以外は公開のフィールドと読めるプロパティ
        /// (匿名型も。深さ 8 まで)。Newtonsoft に Unity の型を渡すと normalized などで自分を指して止まらないので、自前で書く
        /// </summary>
        internal static class Json
        {
            static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;

            internal static string Write(object v)
            {
                var sb = new System.Text.StringBuilder();
                Val(sb, v, 0);
                return sb.ToString();
            }

            static void Val(System.Text.StringBuilder sb, object v, int depth)
            {
                if (v == null) { sb.Append("null"); return; }
                if (depth > 8) { Str(sb, v.ToString()); return; }
                switch (v)
                {
                    case string s: Str(sb, s); return;
                    case bool b: sb.Append(b ? "true" : "false"); return;
                    case char ch: Str(sb, ch.ToString()); return;
                    case float f: Num(sb, f); return;
                    case double d: Num(sb, d); return;
                    case decimal m: sb.Append(m.ToString(Inv)); return;
                    case int _: case long _: case short _: case byte _: case uint _: case ulong _: case ushort _: case sbyte _:
                        sb.Append(Convert.ToString(v, Inv)); return;
                    case Enum e: Str(sb, e.ToString()); return;
                    case Vector2 a: Arr(sb, a.x, a.y); return;
                    case Vector3 a: Arr(sb, a.x, a.y, a.z); return;
                    case Vector4 a: Arr(sb, a.x, a.y, a.z, a.w); return;
                    case Quaternion q: Arr(sb, q.x, q.y, q.z, q.w); return;
                    case Rect r: Arr(sb, r.x, r.y, r.width, r.height); return;
                    case Color c: Arr(sb, c.r, c.g, c.b, c.a); return;
                    case UnityEngine.Object o: Str(sb, o != null ? o.name : null); return;
                    case System.Collections.IDictionary dict:
                    {
                        sb.Append('{'); bool first = true;
                        foreach (System.Collections.DictionaryEntry de in dict)
                        {
                            if (!first) sb.Append(','); first = false;
                            Str(sb, Convert.ToString(de.Key, Inv)); sb.Append(':'); Val(sb, de.Value, depth + 1);
                        }
                        sb.Append('}'); return;
                    }
                    case System.Collections.IEnumerable list:
                    {
                        sb.Append('['); bool first = true;
                        foreach (var x in list)
                        {
                            if (!first) sb.Append(','); first = false;
                            if (x != null && x.GetType().IsGenericType && x.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                            {   // KeyValuePair の並び (Snapshot など) は {"key": value} の1つずつ
                                var kt = x.GetType();
                                sb.Append('{'); Str(sb, Convert.ToString(kt.GetProperty("Key").GetValue(x), Inv)); sb.Append(':'); Val(sb, kt.GetProperty("Value").GetValue(x), depth + 1); sb.Append('}');
                            }
                            else Val(sb, x, depth + 1);
                        }
                        sb.Append(']'); return;
                    }
                }
                // それ以外の型: 公開のフィールドと読めるプロパティ
                var type = v.GetType();
                sb.Append('{'); bool firstM = true;
                foreach (var fi in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (!firstM) sb.Append(','); firstM = false;
                    Str(sb, fi.Name); sb.Append(':');
                    object fv; try { fv = fi.GetValue(v); } catch (Exception) { fv = null; }
                    Val(sb, fv, depth + 1);
                }
                foreach (var pi in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (!pi.CanRead || pi.GetIndexParameters().Length > 0) continue;
                    if (!firstM) sb.Append(','); firstM = false;
                    Str(sb, pi.Name); sb.Append(':');
                    object pv; try { pv = pi.GetValue(v); } catch (Exception) { pv = null; }
                    Val(sb, pv, depth + 1);
                }
                sb.Append('}');
            }

            static void Arr(System.Text.StringBuilder sb, params float[] xs)
            {
                sb.Append('[');
                for (int i = 0; i < xs.Length; i++) { if (i > 0) sb.Append(','); Num(sb, xs[i]); }
                sb.Append(']');
            }

            static void Num(System.Text.StringBuilder sb, double d)
            {
                if (double.IsNaN(d) || double.IsInfinity(d)) { sb.Append("null"); return; }
                sb.Append(d.ToString("R", Inv));
            }

            static void Num(System.Text.StringBuilder sb, float f)
            {
                if (float.IsNaN(f) || float.IsInfinity(f)) { sb.Append("null"); return; }
                sb.Append(f.ToString("R", Inv));
            }

            static void Str(System.Text.StringBuilder sb, string s)
            {
                if (s == null) { sb.Append("null"); return; }
                sb.Append('"');
                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", Inv));
                            else sb.Append(c);
                            break;
                    }
                }
                sb.Append('"');
            }
        }

        /// <summary>占術の保留 (青 2026-09-25) が残っていれば全部残して決める (撮影・自動操作が EndTurn で止まらないように)</summary>
        static void ClearScry(GameRoot g)
        {
            if (g.Rs != null && g.Rs.Combat != null && g.Rs.Combat.PendingScry != null) g.DoCombat(new Command_ResolveScry { DiscardUids = new List<string>() });
        }
    }
}
