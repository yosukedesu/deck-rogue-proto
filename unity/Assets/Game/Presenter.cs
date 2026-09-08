// Presenter.cs — 演出キューの骨格 (2026-09-07 M1)。エンジンの状態は一瞬で確定し、画面はイベントログの差分を
// 順に取り出して見せる (StS のアクションキューと同型)。M1 では「ダメージの浮き文字と揺れ・ブロック・回復」だけ。
// M2 で戦闘画面を作り直す時に、カードの飛び・敵の動き・ターンバナーをここへ足す。
// 座標は GameRoot.Anchors (画面の組み立てが登録した RectTransform) から取る。無ければ黙って飛ばす。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class Presenter
    {
        /// <summary>直前に見たイベント数 (戦闘が変わったら 0 に戻る)</summary>
        static int _seen;
        static object _seenCombat;

        public static void Reset() { _seen = 0; _seenCombat = null; }

        /// <summary>このコマンドで敵の行動 (TurnEnded 以降) が起きたか = 古い盤面の上で順に見せる価値がある</summary>
        public static bool HasEnemyPhase(GameState combat)
        {
            if (combat == null) return false;
            var log = combat.EventLog;
            for (int i = Math.Min(_seen, log.Count); i < log.Count; i++) if (log[i] is GameEvent_TurnEnded) return true;
            return false;
        }

        /// <summary>
        /// 順送りの演出: 新しいイベントを 0.12〜0.4 秒ずつずらして古い盤面 (的が生きている) の上で見せ、終わってから onDone (=Rebuild)。
        /// その間は入力を塞ぐ。イベントの種類ごとの間: 攻撃 0.4 / バナー 0.6 / その他 0.12
        /// </summary>
        public static void PlaySequenced(GameRoot g, GameState combat, Action onDone)
        {
            var log = combat.EventLog;
            var fx = g.FxLayer;
            Canvas.ForceUpdateCanvases();
            var block = UiKit.NewRect("inputblock", fx);
            UiKit.Stretch(block, 0f, 0f, 0f, 0f);
            var bimg = block.gameObject.AddComponent<Image>();
            bimg.color = new Color(0f, 0f, 0f, 0f);
            bimg.raycastTarget = true;
            var blockCg = fx.GetComponent<CanvasGroup>();
            if (blockCg != null) blockCg.blocksRaycasts = true;
            float delay = 0f;
            for (int i = Math.Min(_seen, log.Count); i < log.Count; i++)
            {
                var ev = log[i];
                float gap;
                if (ev is GameEvent_DamageDealt) gap = 0.4f;
                else if (ev is GameEvent_TurnEnded || ev is GameEvent_TurnStarted) gap = 0.6f;
                else if (ev is GameEvent_BlockGained || ev is GameEvent_HpHealed) gap = 0.15f;
                else continue;
                var captured = ev;
                Tween.After(delay, () => { try { Show(g, fx, captured, true); } catch (Exception e) { Debug.LogWarning("[Presenter] " + e.Message); } });
                delay += gap;
            }
            _seen = log.Count;
            _seenCombat = combat;
            Tween.After(Mathf.Min(delay, 6f), () =>
            {
                if (block != null) UnityEngine.Object.Destroy(block.gameObject);
                if (blockCg != null) blockCg.blocksRaycasts = false;
                onDone?.Invoke();
            });
        }

        /// <summary>コマンド適用後に呼ぶ。新しいイベントを演出に変換する</summary>
        public static void Play(GameRoot g, GameState combat)
        {
            if (g == null || combat == null) return;
            var log = combat.EventLog;
            if (!ReferenceEquals(_seenCombat, null) && log.Count < _seen) _seen = 0; // 新しい戦闘
            // 同じ戦闘か: CombatStarted の位置が変わらなければ同じ (イベントログは追記のみ)
            if (_seen > 0 && _seen <= log.Count && !(log[0] is GameEvent_CombatStarted)) _seen = 0;
            var fx = g.FxLayer;
            // Rebuild 直後は LayoutGroup が未計算 (全て原点) なので、的の座標を読む前にレイアウトを確定させる
            Canvas.ForceUpdateCanvases();
            // カードが敵へ飛ぶ 0.2 秒に着弾を合わせる
            float delay = 0f;
            for (int i = _seen; i < log.Count; i++) if (log[i] is GameEvent_DamageDealt dd && dd.Source == "player") { delay = 0.2f; break; }
            for (int i = _seen; i < log.Count; i++)
            {
                var ev = log[i];
                if (!(ev is GameEvent_DamageDealt || ev is GameEvent_BlockGained || ev is GameEvent_HpHealed || ev is GameEvent_TurnStarted || ev is GameEvent_TurnEnded)) continue;
                var captured = ev;
                // 連続する演出は 0.12 秒ずつずらす (同じ場所に重ならない・順番が読める)
                Tween.After(delay, () => { try { Show(g, fx, captured, false); } catch (Exception e) { Debug.LogWarning("[Presenter] " + e.Message); } });
                delay += 0.12f;
            }
            _seen = log.Count;
            _seenCombat = combat;
        }

        /// <summary>画面中央の帯 (ターン開始・敵の番)。0.9 秒で消える</summary>
        static void Banner(RectTransform fx, string text, Color color)
        {
            var rt = UiKit.NewRect("banner", fx);
            UiKit.Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-360f, -40f), new Vector2(360f, 40f));
            rt.localRotation = Quaternion.Euler(0f, 0f, -1f);
            var bg = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.sprite = PaperFx.Panel; bg.type = UnityEngine.UI.Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 1f;
            bg.raycastTarget = false;
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.alpha = 0f;
            var t = UiKit.Deco(rt, text, 34, color, TextAnchor.MiddleCenter);
            UiKit.Stretch(t.rectTransform, 0f, 0f, 0f, 0f);
            Tween.Run(0.9f, k => { if (cg != null) cg.alpha = k < 0.15f ? k / 0.15f : k > 0.7f ? 1f - (k - 0.7f) / 0.3f : 1f; }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        static string StatusJa(string status)
        {
            switch (status)
            {
                case "weak": return "弱体"; case "vulnerable": return "脆弱"; case "frail": return "虚弱"; case "restrain": return "拘束";
                case "mist": return "霞み"; case "slow": return "重り"; case "wound": return "負傷"; case "scald": return "火傷"; case "junk": return "がらくた";
                default: return status;
            }
        }

        static void Show(GameRoot g, RectTransform fx, GameEvent ev, bool nudgeHp)
        {
            switch (ev)
            {
                case GameEvent_DamageDealt d:
                {
                    // source=player → 敵 (EnemyIndex=対象) が受けた／source=enemy → 自分が受けた (EnemyIndex=攻撃者)
                    if (d.Source == "player")
                    {
                        var rt = g.Anchor("enemy" + (d.EnemyIndex ?? 0));
                        if (rt == null) return;
                        var pos = Tween.CenterIn(rt, fx) + new Vector2(UnityEngine.Random.Range(-30f, 30f), 20f);
                        // 斬撃の筋と白い点滅、大きいほど画面も揺れる
                        var spr = g.Battle != null ? g.Battle.EnemySprite(d.EnemyIndex ?? 0) : null;
                        if (spr != null)
                        {
                            Tween.Slash(fx, Tween.CenterIn(spr, fx), UnityEngine.Random.Range(-50f, -20f), new Color(1f, 0.95f, 0.8f, 0.95f));
                            Stage.Flash("enemy" + (d.EnemyIndex ?? 0));
                        }
                        Audio.Play("slash", 0.6f);
                        Audio.Play(d.Amount >= 15 ? "hit_big" : "hit", 0.9f);
                        if (d.Amount >= 15) Stage.Shake(Mathf.Min(14f, d.Amount * 0.4f), 0.25f);
                        Tween.Float(fx, pos, d.Amount.ToString(), d.Amount > 0 ? UiKit.Hex("#ffd36b") : UiKit.ColDim, d.Amount >= 20 ? 46 : 36);
                        if (d.Amount > 0) Tween.Punch(rt, Mathf.Min(0.12f, 0.03f + d.Amount * 0.004f));
                        if (nudgeHp && g.Battle != null && d.HpLoss > 0) g.Battle.NudgeEnemyHp(d.EnemyIndex ?? 0, -d.HpLoss);
                    }
                    else
                    {
                        // 攻撃した敵は踏み込み、自分は斬られて画面が揺れる
                        var atkSpr = g.Battle != null ? g.Battle.EnemySprite(d.EnemyIndex ?? -1) : null;
                        if (atkSpr != null) Tween.Lunge(atkSpr, new Vector2(-90f, 12f));
                        var rt = g.Anchor("player");
                        if (rt == null) return;
                        var pSpr = g.Battle != null ? g.Battle.PlayerSprite() : null;
                        if (pSpr != null) Tween.Slash(fx, Tween.CenterIn(pSpr, fx), UnityEngine.Random.Range(20f, 50f), new Color(1f, 0.6f, 0.5f, 0.95f));
                        Audio.Play("lunge", 0.5f);
                        Audio.Play(d.HpLoss >= 12 ? "hit_big" : "hit", d.HpLoss > 0 ? 0.9f : 0.45f);
                        if (d.HpLoss > 0)
                        {
                            Stage.Shake(Mathf.Min(18f, 4f + d.HpLoss * 0.7f), 0.3f);
                            Stage.Flash("player");
                            Tween.ScreenFlash(fx, new Color(0.9f, 0.1f, 0.1f, Mathf.Min(0.35f, 0.1f + d.HpLoss * 0.015f)));
                        }
                        var pos = Tween.CenterIn(rt, fx) + new Vector2(UnityEngine.Random.Range(-40f, 40f), 10f);
                        Tween.Float(fx, pos, "-" + d.Amount, d.Amount > 0 ? UiKit.ColBad : UiKit.ColDim, d.Amount >= 15 ? 46 : 36);
                        if (d.Amount > 0) Tween.Punch(rt, Mathf.Min(0.1f, 0.03f + d.Amount * 0.004f));
                        if (nudgeHp && g.Battle != null && d.HpLoss > 0) g.Battle.NudgePlayerHp(-d.HpLoss);
                    }
                    break;
                }
                case GameEvent_BlockGained b:
                {
                    if (b.Target != "player") return;
                    var rt = g.Anchor("player");
                    if (rt == null) return;
                    Audio.Play("block", 0.7f);
                    var ps = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    if (ps != null) Tween.IconBurst(fx, Tween.CenterIn(ps, fx) + new Vector2(0f, 20f), "shield", new Color(0.55f, 0.75f, 1f, 0.9f), 110f);
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(80f, 10f), "+" + b.Amount, UiKit.ColBlock, 30, 40f, 0.7f);
                    break;
                }
                case GameEvent_TurnStarted ts:
                    Audio.Play("turn", 0.6f, 0f);
                    Banner(fx, "ターン " + ts.Turn + "  —  あなたの番", UiKit.ColAccent);
                    break;
                case GameEvent_TurnEnded _:
                    Audio.Play("enemy_turn", 0.6f, 0f);
                    Banner(fx, "敵の番", UiKit.Hex("#ff6b57"));
                    break;
                case GameEvent_StatusInflicted si:
                {
                    // 自分に状態異常: 紫の浮き文字で「いつ掛かったか」を見せる (2026-09-09「いつデバフをかけられたかも分からない」)
                    var rt = g.Anchor("player");
                    if (rt == null) return;
                    Audio.Play("buff", 0.5f, 0.02f);
                    var ps2 = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    if (ps2 != null) Tween.IconBurst(fx, Tween.CenterIn(ps2, fx) + new Vector2(0f, 30f), "exposed", new Color(0.72f, 0.5f, 0.85f, 0.9f), 110f);
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(0f, 70f), StatusJa(si.Status) + " +" + si.Amount, UiKit.Hex("#b47ad6"), 32, 46f, 1.2f);
                    break;
                }
                case GameEvent_ExposedApplied ea:
                {
                    var rt = g.Anchor("enemy" + ea.EnemyIndex);
                    if (rt == null) return;
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(0f, 60f), "急所 +" + ea.Amount, UiKit.Hex("#e0a04a"), 28, 40f, 1.0f);
                    break;
                }
                case GameEvent_EnemyWeakened ew:
                {
                    var rt = g.Anchor("enemy" + ew.EnemyIndex);
                    if (rt == null) return;
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(0f, 60f), "威圧 +" + ew.Amount, UiKit.Hex("#7fa7c9"), 28, 40f, 1.0f);
                    break;
                }
                case GameEvent_BurnApplied ba:
                {
                    var rt = g.Anchor("enemy" + ba.EnemyIndex);
                    if (rt == null) return;
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(0f, 60f), "延焼 +" + ba.Amount, UiKit.Hex("#e8742f"), 28, 40f, 1.0f);
                    break;
                }
                case GameEvent_StrengthGained sg:
                {
                    var rt = g.Anchor("enemy" + sg.EnemyIndex);
                    if (rt == null || sg.Amount == 0) return;
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(0f, 60f), "筋力 " + (sg.Amount > 0 ? "+" : "") + sg.Amount, sg.Amount > 0 ? UiKit.Hex("#e0b25a") : UiKit.Hex("#7fa7c9"), 28, 40f, 1.0f);
                    break;
                }
                case GameEvent_GrowthAdded ga:
                {
                    var rt = g.Anchor("player");
                    if (rt == null || ga.Amount <= 0) return;
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(-60f, 60f), "成長 +" + ga.Amount, PaperFx.Moss, 26, 36f, 0.9f);
                    break;
                }
                case GameEvent_MomentumAdded ma:
                {
                    var rt = g.Anchor("player");
                    if (rt == null || ma.Amount <= 0) return;
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(60f, 60f), "勢い +" + ma.Amount, PaperFx.Honey, 26, 36f, 0.9f);
                    break;
                }
                case GameEvent_HpHealed h:
                {
                    var rt = g.Anchor("player");
                    if (rt == null) return;
                    Audio.Play("heal", 0.7f);
                    var hs = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    if (hs != null) Tween.IconBurst(fx, Tween.CenterIn(hs, fx) + new Vector2(0f, 20f), "heart", new Color(0.6f, 1f, 0.6f, 0.9f), 100f);
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(-80f, 10f), "+" + h.Amount, UiKit.ColAccent, 30, 40f, 0.7f);
                    break;
                }
            }
        }
    }
}
