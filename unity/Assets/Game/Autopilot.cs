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
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public class Autopilot : MonoBehaviour
    {
        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var scenario = Arg("-autopilot");
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

        void Start()
        {
            Directory.CreateDirectory(_dir);
            StartCoroutine(Run());
        }

        /// <summary>順送りの演出 (Presenter.PlaySequenced の入力ブロック) が終わるまで待つ (最大6秒)</summary>
        IEnumerator WaitPresentation()
        {
            float t0 = Time.realtimeSinceStartup;
            while (GameObject.Find("inputblock") != null && Time.realtimeSinceStartup - t0 < 6f) yield return null;
        }

        IEnumerator Shot(string name, int settle = 6)
        {
            // レイアウトと描画が落ち着くまで数フレーム待つ (Rebuild 直後の1フレームは LayoutGroup が未計算)。コマ送りの途中を撮る時は settle=1
            for (int i = 0; i < settle; i++) yield return null;
            _n++;
            var path = Path.Combine(_dir, $"{_n:00}-{name}.png");
            ScreenCapture.CaptureScreenshot(path, 1);
            Debug.Log("[Autopilot] shot " + path + " player=" + Stage.DebugAnim("player"));
            for (int i = 0; i < 3; i++) yield return null;
        }

        IEnumerator Run()
        {
            // GameRoot の起動を待つ
            float t0 = Time.realtimeSinceStartup;
            while (GameRoot.I == null && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            yield return new WaitForSeconds(0.5f);
            var g = GameRoot.I;
            if (g == null) { Debug.LogError("[Autopilot] GameRoot が起動しない"); Application.Quit(1); yield break; }
            try
            {
                switch (_scenario)
                {
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
            g.DoCombat(new Command_EndTurn());
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
        ///   phase=map|combat|reward|relic|shop|event|campfire|workshop|won|lost  act=1..3  deck=<deckId>  relics=<id,id>  hp=<%>  gold=<n>  difficulty=<n>  leader=<id>
        ///   enemy=<encounterId or enemyId> (combat)  event=<eventId>  pick=<idx[,idx]> (工房の素材／報酬の選択枠)  submode=forge (焚き火)  shopmode=upgrade|remove
        ///   viewmap=1  viewdeck=1  log=1  name=<shot名>
        /// act/deck/relics/hp/gold/difficulty のどれかがあればチェックポイント開始 (CreateDebugCheckpointRun)、無ければ通常開始
        /// </summary>
        IEnumerator StateJump(GameRoot g, string spec)
        {
            var kv = new Dictionary<string, string>();
            foreach (var part in spec.Split(';'))
            {
                int eq = part.IndexOf('=');
                if (eq > 0) kv[part.Substring(0, eq).Trim().ToLowerInvariant()] = part.Substring(eq + 1).Trim();
            }
            string Get(string k, string dflt = null) { string v; return kv.TryGetValue(k, out v) ? v : dflt; }
            string phase = (Get("phase", "map") ?? "map").ToLowerInvariant();
            g.SetSeed(_seed);
            if (Get("leader") != null) g.LeaderId = Get("leader");
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

            var rs = g.Rs;
            try
            {
                switch (phase)
                {
                    case "combat":
                        if (Get("enemy") != null) g.Rs = DeckRogue.Engine.Run.DebugLaunchCombat(rs, Get("enemy"));
                        else
                        {   // 最初に選べる戦闘ノードへ進む (通常経路)
                            var choices = DeckRogue.Engine.Run.NextChoices(rs);
                            int col = choices.Count > 0 ? choices[0] : 0;
                            for (int i = 0; i < choices.Count; i++) { var n = rs.Map[rs.Row + 1][choices[i]]; if (n.Type == MapNodeTypes.Battle) { col = choices[i]; break; } }
                            g.Do(new RunCommand_ChooseNode { Col = col });
                        }
                        break;
                    case "reward": g.Rs = DeckRogue.Engine.Run.DebugRollRewards(rs); break;
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
            var picks = (Get("pick") ?? "").Split(',').Select(x => { int v; return int.TryParse(x.Trim(), out v) ? v : -1; }).Where(v => v >= 0).ToList();
            if (phase == "workshop") { g.WorkshopA = picks.Count > 0 ? picks[0] : -1; g.WorkshopB = picks.Count > 1 ? picks[1] : -1; }
            if (Get("viewmap") == "1") g.ViewMap = true;
            if (Get("viewdeck") == "1") g.ViewDeck = true;
            if (Get("log") == "1") g.ShowLog = true;
            g.Rebuild();
            yield return WaitPresentation();
            yield return Shot(Get("name") ?? ("state-" + phase), 10);
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
                    case RunPhases.Reward: g.Do(new RunCommand_PickReward { Index = 0 }); break;
                    case RunPhases.RelicReward: g.Do(new RunCommand_PickRelic { Index = 0 }); break;
                    case RunPhases.Campfire: g.Do(new RunCommand_CampfireRest()); break;
                    case RunPhases.Shop: g.Do(new RunCommand_ShopLeave()); break;
                    case RunPhases.Workshop: g.Do(new RunCommand_WorkshopSkip()); break;
                    case RunPhases.Event:
                    {
                        EventDef def = null;
                        try { def = Content.GetEventDef(rs.EventId); } catch (Exception) { }
                        int last = def != null ? def.Choices.Count - 1 : 0;
                        g.Do(new RunCommand_EventChoice { Index = last });
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
                        if (!Effects.IsPlayableFromHand(c) || cost > st.Player.Energy) continue;
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
                if (g.Rs.Combat.Phase == CombatPhases.PlayerTurn) g.DoCombat(new Command_EndTurn());
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
            yield return Shot("map");
            for (int i = 0; i < 8 && g.Rs != null && g.Rs.Phase != RunPhases.Combat; i++)
            {
                var rs = g.Rs;
                RunCommand cmd = null;
                switch (rs.Phase)
                {
                    case RunPhases.Map: cmd = new RunCommand_ChooseNode { Col = DeckRogue.Engine.Run.NextChoices(rs)[0] }; break;
                    case RunPhases.Event:
                    {
                        var ev = Content.AllEvents.FirstOrDefault(e => e.Id == rs.EventId);
                        cmd = new RunCommand_EventChoice { Index = ev != null ? ev.Choices.Count - 1 : 0 };
                        break;
                    }
                    case RunPhases.Shop: cmd = new RunCommand_ShopLeave(); break;
                    case RunPhases.Campfire: cmd = new RunCommand_CampfireRest(); break;
                    case RunPhases.Workshop: cmd = new RunCommand_WorkshopSkip(); break;
                    case RunPhases.RelicReward: cmd = new RunCommand_SkipRelic(); break;
                    case RunPhases.Reward: cmd = new RunCommand_SkipReward(); break;
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
                    g.DoCombat(new Command_EndTurn());
                    yield return WaitPresentation();
                    yield return Shot("combat-after-end" + (turn + 1));
                }
            }
            yield return Shot("last");
        }
    }
}
