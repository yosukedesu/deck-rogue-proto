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
                yield return new WaitForSeconds(0.06f);
                yield return Shot("battle-swing", 1);    // 溜め (0〜0.25s)
                yield return new WaitForSeconds(0.14f);
                yield return Shot("battle-swing2", 1);   // 振り抜き (0.25〜0.375s)
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
                    if (ph == RunPhases.Campfire) { g.SubMode = "forge"; g.Rebuild(); yield return Shot("campfire-forge"); g.SubMode = null; g.Rebuild(); }
                    if (ph == RunPhases.Shop) { g.ShopMode = "upgrade"; g.Rebuild(); yield return Shot("shop-upgrade"); g.ShopMode = null; g.Rebuild(); }
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
