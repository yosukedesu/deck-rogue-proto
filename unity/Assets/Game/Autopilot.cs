// Autopilot.cs — 自動操縦スクショ (2026-09-07 M1「目を作る」)。
// プレイヤーを `DeckRogue.exe -autopilot tour -shots <dir>` で起動すると、GameRoot の公開 API で画面を進めながら
// 各画面の PNG を <dir> に書き、終わったら終了する。WSL 側 (scripts/unity-win.sh shots) が PNG を回収して
// Claude Code が Read で見る＝「画面を確認できる目」。エンジンには触れない (状態を読んでコマンドを投げるだけ)。
using System;
using System.Collections;
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

        IEnumerator Shot(string name)
        {
            // レイアウトと描画が落ち着くまで数フレーム待つ (Rebuild 直後の1フレームは LayoutGroup が未計算)
            for (int i = 0; i < 6; i++) yield return null;
            _n++;
            var path = Path.Combine(_dir, $"{_n:00}-{name}.png");
            ScreenCapture.CaptureScreenshot(path, 1);
            Debug.Log("[Autopilot] shot " + path);
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
            g.Seed = _seed;
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
                g.CancelPending();
            }
            if (reactionCard != null)
            {
                g.DoCombat(new Command_SetCard { CardUid = reactionCard.Uid });
                yield return Shot("battle-set");
            }
            g.DoCombat(new Command_EndTurn());
            yield return Shot("battle-after-end");
            if (g.Rs != null && g.Rs.Combat != null && g.Rs.Combat.Phase == CombatPhases.AwaitingReaction)
            {
                yield return Shot("battle-confirm");
                g.DoCombat(new Command_ConfirmReaction { Fire = false });
                yield return Shot("battle-held");
            }
            yield return Shot("battle-turn2");
        }

        /// <summary>セットアップ→ラン開始→マップ→最初のノード→戦闘 (ターン終了×2)。戦闘以外に入ったらその画面も撮る</summary>
        IEnumerator Tour(GameRoot g)
        {
            yield return Shot("setup");
            g.Seed = _seed;
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
                    yield return Shot("combat-after-end" + (turn + 1));
                }
            }
            yield return Shot("last");
        }
    }
}
