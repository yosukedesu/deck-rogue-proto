// BattleJuice.cs — 戦闘画面の手触り (2026-11-04 見た目・UX・演出の磨き 束B)。
// 札を持った時の「この札でここまで減る」の見込み (engine の純関数 Effects.DamageBreakdownOf を 1 ヒットずつ辿る)・札の持ち運びの慣性と傾き・手札の呼吸・
// 消滅の熾の粒。engine も data も触らない: 見込みは状態の写し (record の with) の上で engine の式を読むだけ＝表示の嘘を作らない。
// 乱数は使わない (粒は添字と黄金角で散らす＝撮影で決定的)。時計は Tween.Run / Tween.UnscaledDt だけ。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class BattleJuice
    {
        // ---- 見込み (B01/B02) ----

        /// <summary>この札の onPlay のダメージが敵全体に向くか (dealDamage の target が all)</summary>
        public static bool TargetsAll(CardDef def)
        {
            if (def == null || def.Effects == null) return false;
            for (int i = 0; i < def.Effects.Count; i++)
            {
                var e = def.Effects[i];
                if (e.Trigger != null && e.Trigger != "onPlay") continue;
                if ((e.Effect == "dealDamage" || e.Effect == "dealDamageRandom") && e.Target == "all") return true;
            }
            return false;
        }

        /// <summary>この札が onPlay で敵にダメージを与える効果を持つか (見込みを出す対象の札か)</summary>
        public static bool HasPlayDamage(CardDef def)
        {
            if (def == null || def.Effects == null) return false;
            for (int i = 0; i < def.Effects.Count; i++)
            {
                var e = def.Effects[i];
                if (e.Trigger != null && e.Trigger != "onPlay") continue;
                if ((e.Effect == "dealDamage" || e.Effect == "dealDamageRandom") && e.Amount.HasValue) return true;
            }
            return false;
        }

        /// <summary>
        /// この札を敵 enemyIdx に出した時に失わせる HP の見込み。engine の DamageBreakdownOf (急所×1.5・装甲・敵ブロック・潜伏・朧・ターン装甲) を
        /// 1 ヒットずつ辿り、ヒットの間は敵の写し (ブロック・急所・HP・このターンの損失) を進める＝多段で 2 発目から素通しになる所まで同じ式。
        /// 読めない札 (X・選択式・条件つきの効果・参照の効果) は -1 (帯を出さない)。条件つきの効果は数えない (満たしていない時に大きく見せない＝要の数字の札と同じ規則)。
        /// ランダム火力は下限で読む (最低でもここまで減る)
        /// </summary>
        public static int PredictHpLoss(GameState st, CardInstance c, int enemyIdx)
        {
            if (st == null || c == null || c.Def == null || enemyIdx < 0 || enemyIdx >= st.Enemies.Count) return -1;
            var def = c.Def;
            if (def.XCost == true) return -1;
            if (def.Modes != null && def.Modes.Count > 0) return -1;
            if (def.Effects == null) return -1;
            if (st.Enemies[enemyIdx].Hp <= 0) return -1;
            int grow = c.GrowBonus.HasValue ? c.GrowBonus.Value : 0;
            var p = st.Player;
            var sim = st;
            int total = 0; bool any = false;
            try
            {
                for (int i = 0; i < def.Effects.Count; i++)
                {
                    var ef = def.Effects[i];
                    if (ef.Trigger != null && ef.Trigger != "onPlay") continue;
                    if (ef.Effect != "dealDamage" && ef.Effect != "dealDamageRandom") continue;
                    if (!ef.Amount.HasValue) continue;
                    if (ef.Condition != null) continue;
                    if (ef.XHits == true) return -1;
                    if (ef.Target != null && ef.Target != "all") return -1;   // mostHp / mostBurn などは向きが読めない
                    // engine の dealDamage と同じ基礎: 成長×(N-1)・勢い×(N-1) の上乗せ (大牙・猛進の角)
                    int baseAmt = ef.Amount.Value + grow
                        + (ef.GrowthMultiplier != null ? (int)(p.Growth * (ef.GrowthMultiplier.Value - 1)) : 0)
                        + (ef.MomentumMultiplier != null ? (int)(p.Momentum * (ef.MomentumMultiplier.Value - 1)) : 0);
                    var en = sim.Enemies[enemyIdx];
                    if (en.Hp <= 0) break;
                    var bd = Effects.DamageBreakdownOf(sim, enemyIdx, baseAmt, ef.Pierce == true);
                    if (bd == null) break;
                    any = true;
                    int loss = Math.Max(0, bd.HpLoss);
                    total += loss;
                    // 次のヒットのために敵の写しを進める (ブロックが削れる・急所が 1 減る・ターン装甲の累計が増える・朧の残りが減る)
                    var next = en with
                    {
                        Hp = Math.Max(0, en.Hp - loss),
                        Block = Math.Max(0, en.Block - Math.Max(0, bd.Blocked)),
                        Exposed = en.Exposed > 0 ? en.Exposed - 1 : 0,
                        DamageThisTurn = (en.DamageThisTurn ?? 0) + loss,
                        Slippery = (en.Slippery ?? 0) > 0 && loss > 0 ? (int?)(en.Slippery.Value - 1) : en.Slippery,
                    };
                    var list = new List<EnemyState>(sim.Enemies);
                    list[enemyIdx] = next;
                    sim = sim with { Enemies = list };
                }
            }
            catch (Exception) { return -1; }
            return any ? total : -1;
        }

        // ---- 札の持ち運び (B04): 指に遅れて付いてきて、動きの向きに傾く (PC だけ) ----

        /// <summary>ドラッグ中の札。Target (親の座標) へ 1−exp(−dt×18) で追いつき、横の速さから ±12° まで傾く。EndDrag で外す (Tween.Move が戻す)</summary>
        public class CardDragFollow : MonoBehaviour
        {
            public RectTransform Rt; public Vector2 Target; public bool HasTarget;
            float _tilt; Vector2 _last; bool _init;
            void Update()
            {
                if (Rt == null || !HasTarget) return;
                float dt = Tween.UnscaledDt;
                if (!_init) { _init = true; _last = Rt.anchoredPosition; }
                float k = 1f - Mathf.Exp(-dt * 18f);
                var p = Vector2.Lerp(Rt.anchoredPosition, Target, k);
                float vx = dt > 0f ? (p.x - _last.x) / dt : 0f;   // 単位/秒
                _last = p;
                Rt.anchoredPosition = p;
                float want = Mathf.Clamp(-vx * 0.012f, -12f, 12f);
                _tilt = Mathf.Lerp(_tilt, want, 1f - Mathf.Exp(-dt * 10f));
                Rt.localRotation = Quaternion.Euler(0f, 0f, _tilt);
            }
        }

        /// <summary>手札の呼吸 (M15): 扇の層ごと 0.5Hz で ±1.5px 上下。ドラッグ中・敵の番は止める (主役は 1 つ)。PC だけ付ける</summary>
        public class HandBreath : MonoBehaviour
        {
            public RectTransform Layer; public Func<bool> Active;
            float _t; Vector2 _base; bool _hasBase;
            void Update()
            {
                if (Layer == null) return;
                if (!_hasBase) { _base = Layer.anchoredPosition; _hasBase = true; }
                bool on = Active == null || Active();
                _t += Tween.UnscaledDt;
                float y = on ? Mathf.Sin(_t * 0.5f * Mathf.PI * 2f) * 1.5f : 0f;
                Layer.anchoredPosition = new Vector2(_base.x, _base.y + y);
            }
        }

        // ---- 粒 ----

        /// <summary>光の粒 (ゆらゆら上がって薄れる)。index で散らす (乱数なし＝撮影で決定的)。n は 1 回に 12 まで</summary>
        public static void Mote(RectTransform fx, Vector2 p0, Color color, int index, float rise = 80f, float dur = 0.65f)
        {
            if (fx == null) return;
            var rt = UiKit.NewRect("mote", fx);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            float u = Frac(index * 0.618034f);   // 黄金角で散らす
            float sz = 10f + 12f * u;
            rt.sizeDelta = new Vector2(sz, sz); rt.anchoredPosition = p0;
            var img = rt.gameObject.AddComponent<Image>(); img.sprite = ThemeFx.Glow(); img.color = color; img.raycastTarget = false;
            float sway = (u - 0.5f) * 36f, ph = u * 6.28f, r = rise * (0.7f + 0.6f * Frac(index * 0.381966f)), d = dur * (0.8f + 0.4f * u);
            Tween.Run(d, k => { if (rt == null) return; rt.anchoredPosition = p0 + new Vector2(sway * Mathf.Sin(k * 3f + ph), r * k); img.color = new Color(color.r, color.g, color.b, color.a * (1f - k * k)); rt.localScale = Vector3.one * (1f - 0.4f * k); }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        /// <summary>消滅の燃え (B05): 札の矩形 (幅 w・高さ h) から熾の粒 n 個が立ちのぼる。粒は 1 回 12 まで (スマホの上限)</summary>
        public static void EmberBurst(RectTransform fx, Vector2 center, float w, float h, int n = 8)
        {
            if (fx == null) return;
            n = Mathf.Clamp(n, 1, 12);
            for (int m = 0; m < n; m++)
            {
                float u = Frac(m * 0.618034f), v = Frac(m * 0.381966f + 0.25f);
                var p0 = center + new Vector2((u - 0.5f) * w * 0.8f, (v - 0.5f) * h * 0.8f);
                int idx = m; float dl = 0.03f * m;
                Tween.After(dl, () => Mote(fx, p0, idx % 3 == 0 ? PaperFx.BrassLight : PaperFx.Ember, idx, 70f, 0.6f));
            }
        }

        static float Frac(float x) { return x - Mathf.Floor(x); }
    }
}
