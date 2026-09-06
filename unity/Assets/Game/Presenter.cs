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
            float delay = 0f;
            for (int i = _seen; i < log.Count; i++)
            {
                var ev = log[i];
                if (!(ev is GameEvent_DamageDealt || ev is GameEvent_BlockGained || ev is GameEvent_HpHealed)) continue;
                var captured = ev;
                // 連続する演出は 0.12 秒ずつずらす (同じ場所に重ならない・順番が読める)
                Tween.After(delay, () => { try { Show(g, fx, captured); } catch (Exception e) { Debug.LogWarning("[Presenter] " + e.Message); } });
                delay += 0.12f;
            }
            _seen = log.Count;
            _seenCombat = combat;
        }

        static void Show(GameRoot g, RectTransform fx, GameEvent ev)
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
                        Tween.Float(fx, pos, d.Amount.ToString(), d.Amount > 0 ? UiKit.Hex("#ffd36b") : UiKit.ColDim, d.Amount >= 20 ? 46 : 36);
                        if (d.Amount > 0) Tween.Punch(rt, Mathf.Min(0.12f, 0.03f + d.Amount * 0.004f));
                    }
                    else
                    {
                        var rt = g.Anchor("player");
                        if (rt == null) return;
                        var pos = Tween.CenterIn(rt, fx) + new Vector2(UnityEngine.Random.Range(-40f, 40f), 10f);
                        Tween.Float(fx, pos, "-" + d.Amount, d.Amount > 0 ? UiKit.ColBad : UiKit.ColDim, d.Amount >= 15 ? 46 : 36);
                        if (d.Amount > 0) Tween.Punch(rt, Mathf.Min(0.1f, 0.03f + d.Amount * 0.004f));
                    }
                    break;
                }
                case GameEvent_BlockGained b:
                {
                    if (b.Target != "player") return;
                    var rt = g.Anchor("player");
                    if (rt == null) return;
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(80f, 10f), "+" + b.Amount, UiKit.ColBlock, 30, 40f, 0.7f);
                    break;
                }
                case GameEvent_HpHealed h:
                {
                    var rt = g.Anchor("player");
                    if (rt == null) return;
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(-80f, 10f), "+" + h.Amount, UiKit.ColAccent, 30, 40f, 0.7f);
                    break;
                }
            }
        }
    }
}
