// PresenterJuice.cs — 戦闘の演出の「手応え」(2026-11-04 見た目・UX・演出の磨き・束 A)。
// Presenter の出来事の順送りから呼ばれる小さな部品: 数字の段 (量で大きさ)・被弾の縁 (全画面の赤をやめて薔薇のビネット)・低 HP の鼓動と危険の同期・
// このターンの与ダメの合計の夜の札・多段の「×N」と「N 連続・合計 M」・赤 (チェイン・猛り火・くべる・衝動) と青 (霊気) の出来事・
// 勝利の戦利品 (G・魔素の硬貨が上部バーへ飛ぶ)・敗北の主人公の崩れ。
// 約束: 数字は engine の値 (表示の嘘を作らない)・色は PaperFx の名前・時計は Tween (Run/After)・紙は光らせない・舞台の光は StageBeat に預ける。
using System;
using System.Collections.Generic;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static partial class Presenter
    {
        // ---------------------------------------------------------------- 数字の段 (G3・M03)

        /// <summary>自分の当たりの段: 0=小 (5 未満)・1=中・2=大 (15 以上か最大 HP の 1/4 以上か急所)・3=とどめ。DamageVfxIntensity と同じ昇格の式</summary>
        static int DamageTier(int amount, int maxHp, bool crit, bool finishing)
        {
            if (finishing) return 3;
            int t = amount < 5 ? 0 : amount < 15 ? 1 : 2;
            if (t < 2 && amount >= 4 && maxHp > 0 && 4 * amount >= maxHp) t++;
            if (crit && t < 2) t = 2;
            return t;
        }

        /// <summary>被弾の段: 0=小・1=中 (5 以上か最大 HP の 1 割)・2=大 (15 以上か最大 HP の 2 割)</summary>
        static int HurtTier(int hpLoss, int maxHp)
        {
            if (hpLoss >= 15 || (maxHp > 0 && hpLoss * 5 >= maxHp)) return 2;
            if (hpLoss >= 5 || (maxHp > 0 && hpLoss * 10 >= maxHp)) return 1;
            return 0;
        }

        /// <summary>段 → 字の大きさ (px)。スマホは 0.85 倍 (UiKit.Txt が 13 以上に切り上げる)</summary>
        static int TierSize(int tier)
        {
            int s = tier <= 0 ? 30 : tier == 1 ? 40 : tier == 2 ? 54 : 68;
            return UiKit.Phone ? Mathf.RoundToInt(s * 0.85f) : s;
        }

        /// <summary>段に応じた浮き数字: 小・中は Float (0.6→1 OutBack)、大・とどめは FloatBig (1.35→1 の叩き込み＋1 コマの白)。とどめは真鍮の縁</summary>
        static void TierNumber(RectTransform fx, Vector2 pos, string text, Color color, int tier, int extraSize = 0)
        {
            int size = TierSize(tier) + extraSize;
            if (tier >= 2) Tween.FloatBig(fx, pos, text, color, size, tier >= 3 ? PaperFx.Brass : (Color?)null, tier >= 3 ? 40f : 50f, tier >= 3 ? 1.2f : 1.0f);
            else Tween.Float(fx, pos, text, color, size, 60f, 0.9f);
        }

        /// <summary>見えている盤面の敵の最大 HP (段の分母)。無ければ 0</summary>
        static int EnemyMaxHpOf(GameRoot g, ReactionCtx ctx, int ei)
        {
            var st = ctx != null && ctx.Prev != null ? ctx.Prev : (g != null && g.Rs != null ? g.Rs.Combat : null);
            if (st == null || ei < 0 || ei >= st.Enemies.Count) return 0;
            return st.Enemies[ei].MaxHp;
        }

        /// <summary>自分の最大 HP (被弾の段の分母)</summary>
        static int PlayerMaxHpOf(GameRoot g, ReactionCtx ctx)
        {
            var st = ctx != null && ctx.CapturedState != null ? ctx.CapturedState : (ctx != null && ctx.Prev != null ? ctx.Prev : (g != null && g.Rs != null ? g.Rs.Combat : null));
            return st != null ? st.Player.MaxHp : 0;
        }

        /// <summary>今の節が幕ボスか (決着・とどめの演出の長さ)</summary>
        static bool IsBossNode(GameRoot g)
        {
            try { var node = DeckRogue.Engine.Run.CurrentNode(g.Rs); return node != null && node.Type == MapNodeTypes.Boss; } catch (Exception) { return false; }
        }

        /// <summary>舞台の敵の体の中心 (世界座標)。舞台に居なければ false</summary>
        static bool EnemyWorld(int ei, out Vector3 center, out Vector3 feet)
        {
            center = Vector3.zero; feet = Vector3.zero;
            float h;
            if (!Stage.TryGetUnitBox("enemy" + ei, out feet, out h)) return false;
            center = feet + Vector3.up * h * 0.5f;
            return true;
        }

        // ---------------------------------------------------------------- 被弾の縁 (G8・④)

        /// <summary>
        /// 被弾の縁 (2026-11-04): 旧は全画面を朱で染めていた (紙の UI まで桃色＝段2 の反証)。画面の縁だけがビネットで色づき、中央と紙は染めない。
        /// alpha は呼び側の量に FxSettings.Flash を掛ける。dur 秒で消える
        /// </summary>
        static void HurtVignette(RectTransform fx, Color rgb, float alpha, float dur, string key)
        {
            if (fx == null) return;
            float a = Mathf.Clamp01(alpha) * FxSettings.Flash;
            if (a <= 0.004f) return;
            var rt = UiKit.NewRect("vignette-" + key, fx);
            UiKit.Stretch(rt, -24f, -24f, -24f, -24f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ThemeFx.Vignette(rgb, "vignette-" + key); img.type = Image.Type.Simple; img.preserveAspect = false; img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, a);
            Tween.Run(dur, k => { if (img != null) img.color = new Color(1f, 1f, 1f, a * (1f - k)); }, Ease.OutQuad, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        /// <summary>全画面の閃き (α は FxSettings.Flash を掛ける。0 なら出さない)</summary>
        static void FlashScreen(RectTransform fx, Color rgb, float alpha, float dur)
        {
            float a = Mathf.Clamp01(alpha) * FxSettings.Flash;
            if (a <= 0.004f || fx == null) return;
            Tween.ScreenFlash(fx, new Color(rgb.r, rgb.g, rgb.b, a), dur);
        }

        // ---------------------------------------------------------------- 低 HP の鼓動・危険 (④)

        static int _dangerCue = -1;

        /// <summary>
        /// HP 3 割以下・致死級の被ダメ予測を音 (Audio.Heartbeat) と舞台 (StageBeat.Danger) に同期する。BattleView.SyncDanger (縁の脈動) と同じ式。
        /// HP は数字の台帳が見せている値 (NumericDisplayState) を読む＝順送りの途中でも着弾した分だけ下がる
        /// </summary>
        static void SyncDangerCue(GameRoot g)
        {
            var st = g != null && g.Rs != null && g.Rs.Phase == RunPhases.Combat ? g.Rs.Combat : null;
            int level = 0;
            if (st != null)
            {
                var shown = NumericDisplayState(st);
                var p = shown.Player;
                if (p.Hp > 0)
                {
                    float ratio = p.MaxHp > 0 ? (float)p.Hp / p.MaxHp : 1f;
                    int incoming = 0; try { incoming = Effects.IncomingTotal(st); } catch (Exception) { }
                    bool lethal = incoming > 0 && p.Hp - Math.Max(0, incoming - (p.Block + p.IceBlock)) <= 0 && st.HideIntents != true;
                    level = lethal ? 2 : ratio <= 0.3f ? 1 : 0;
                }
            }
            if (level == _dangerCue) return;
            int prev = _dangerCue; _dangerCue = level;
            if (level > 0 && prev <= 0) Audio.Key("danger_in");
            Audio.Heartbeat(level > 0, level >= 2 ? 1.7f : 0.9f);
            StageBeat.Danger(level > 0, level >= 2);
        }

        /// <summary>戦闘を離れる・決着: 鼓動と危険の縁を止める</summary>
        static void DangerOff()
        {
            if (_dangerCue > 0) { Audio.Heartbeat(false); StageBeat.Danger(false, false); }
            _dangerCue = -1;
        }

        // ---------------------------------------------------------------- このターンの与ダメの合計 (B11)

        static int _turnTotal, _turnHits; static RectTransform _turnNote; static TMP_Text _turnNoteText; static int _turnNoteShown;

        /// <summary>
        /// 自分の当たり (延焼のティック・人形の束も) を足し、2 発目から自分の頭上の夜の札「このターン 48」を 1 枚だけ更新する (数え上げ 0.2 秒・跳ね)。
        /// 30 以上で字が真鍮に。ターン終了で薄れて消え、次のターン開始で 0 に戻る
        /// </summary>
        static void AddTurnTotal(GameRoot g, RectTransform fx, int amount)
        {
            if (amount <= 0 || g == null || fx == null) return;
            _turnTotal += amount; _turnHits++;
            if (_turnHits < 2) return;
            var pos = PlayerFloatPos(g, fx) + new Vector2(0f, UiKit.Phone ? 56f : 100f);
            var fr = fx.rect;
            pos.y = Mathf.Min(pos.y, fr.yMax - BattleScreen.TopH - 30f);
            if (_turnNote == null)
            {
                _turnNote = PaperFx.NightNote(fx, "このターン " + _turnTotal, 22, 260f, true, "turn-total");
                _turnNote.anchorMin = _turnNote.anchorMax = new Vector2(0.5f, 0.5f);
                _turnNote.anchoredPosition = pos;
                _turnNoteText = _turnNote.GetComponentInChildren<TMP_Text>();
                _turnNoteShown = _turnTotal;
                _turnNote.localScale = Vector3.one * 0.7f;
                Tween.Scale(_turnNote, Vector3.one, 0.2f, Ease.OutBack);
            }
            else
            {
                _turnNote.anchoredPosition = pos;
                int from = _turnNoteShown, to = _turnTotal; var tx = _turnNoteText;
                Tween.Run(0.2f, k => { if (tx != null) tx.text = "このターン " + Mathf.RoundToInt(Mathf.Lerp(from, to, k)); }, Ease.OutQuad, () => { if (tx != null) tx.text = "このターン " + to; });
                _turnNoteShown = to;
                Tween.Punch(_turnNote, 0.12f, 0.25f);
            }
            if (_turnNoteText != null) _turnNoteText.color = _turnTotal >= 30 ? PaperFx.NightBrassInk : PaperFx.NightInk;
        }

        /// <summary>合計の札を消す (fade＝薄れて消す・ターン終了。即＝ターン開始・戦闘の始まり・Reset)</summary>
        static void ClearTurnTotal(bool fade)
        {
            _turnTotal = 0; _turnHits = 0; _turnNoteShown = 0;
            var rt = _turnNote; _turnNote = null; _turnNoteText = null;
            if (rt == null) return;
            if (!fade) { UnityEngine.Object.Destroy(rt.gameObject); return; }
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            Tween.Run(0.4f, k => { if (cg != null) cg.alpha = 1f - k; }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        // ---------------------------------------------------------------- 多段の積み上がり (G4・M02)

        static RectTransform _comboNote; static int _comboEnemy = -1;

        /// <summary>
        /// 1 枚の札で 2 回以上当たる時: 2 発目から狙った敵の肩に真鍮の墨の小札「×2」「×3」が 1 つだけ出て跳ねながら数が増え、音は段ごとに上がる (combo_up を KeyPitched)。
        /// 最後の 1 発で 3 発以上なら 0.25 秒後に判「N 連続 ・ 合計 M」(合計は engine の Amount の和)。全体攻撃 (Volley) は体の数ぶん同時に出るので札は出さない
        /// </summary>
        static void ComboTick(GameRoot g, RectTransform fx, int ei, HitPlan hp)
        {
            if (hp == null || hp.Total < 2 || fx == null) return;
            bool last = hp.Index == hp.Total - 1 && (!hp.Volley || hp.VolleyLast);
            if (!hp.Volley)
            {
                if (hp.Index == 0) { ClearCombo(false); }
                else
                {
                    Audio.KeyPitched("combo_up", 1f + 0.07f * Mathf.Min(hp.Index, 8), 0.7f);
                    var pos = EnemyFloatPos(g, fx, ei) + new Vector2(UiKit.Phone ? 54f : 78f, UiKit.Phone ? 52f : 72f);
                    string label = "×" + (hp.Index + 1);
                    if (_comboNote == null || _comboEnemy != ei)
                    {
                        ClearCombo(false);
                        _comboNote = PaperFx.NightNote(fx, label, 22, 120f, true, "combo");
                        _comboNote.anchorMin = _comboNote.anchorMax = new Vector2(0.5f, 0.5f);
                        _comboNote.anchoredPosition = pos; _comboEnemy = ei;
                        var tx = _comboNote.GetComponentInChildren<TMP_Text>(); if (tx != null) tx.color = PaperFx.NightBrassInk;
                        _comboNote.localScale = Vector3.one * 0.6f;
                        Tween.Scale(_comboNote, Vector3.one, 0.16f, Ease.OutBack);
                    }
                    else
                    {
                        var tx = _comboNote.GetComponentInChildren<TMP_Text>(); if (tx != null) tx.text = label;
                        Tween.Punch(_comboNote, 0.18f, 0.2f);
                    }
                }
            }
            if (!last) return;
            if (hp.Total >= 3 && hp.Sum > 0)
            {
                int n = hp.Total, sum = hp.Sum; var at = EnemyFloatPos(g, fx, ei) + new Vector2(0f, -22f);
                AfterVisual(0.25f, () => Tween.Stamp(fx, at, n + " 連続 ・ 合計 " + sum, PaperFx.BrassLight, PaperFx.BrassInk, PaperFx.Brass, 20, 0.8f, -6f));
            }
            AfterVisual(0.5f, () => ClearCombo(true));
        }

        static void ClearCombo(bool fade)
        {
            var rt = _comboNote; _comboNote = null; _comboEnemy = -1;
            if (rt == null) return;
            if (!fade) { UnityEngine.Object.Destroy(rt.gameObject); return; }
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            Tween.Run(0.3f, k => { if (cg != null) cg.alpha = 1f - k; }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        /// <summary>戦闘の始まり・Reset: 札を全部片づける</summary>
        static void ClearJuice()
        {
            ClearTurnTotal(false); ClearCombo(false); DangerOff(); _lastDownPos = null;
            if (_blazeOn) StageBeat.Blaze(false); _blazeOn = false;
        }

        // ---------------------------------------------------------------- 撃破 (B08・2)

        /// <summary>最後に崩れた敵の位置 (fx の座標)。勝利の硬貨の出どころ。EnemyDied の演出で覚える</summary>
        static Vector2? _lastDownPos;

        /// <summary>撃破の瞬間: 判「撃破」(真鍮) ・ enemy_down (幕ボスは enemy_down_boss) ・ 舞台の足元の光 (StageBeat.EnemyDown)。崩れ自体は BattleView.KillEnemy</summary>
        static void EnemyDownCue(GameRoot g, RectTransform fx, int ei)
        {
            if (g == null || fx == null) return;
            var pos = EnemyFloatPos(g, fx, ei);
            _lastDownPos = pos;
            bool boss = IsBossNode(g);
            Audio.Key(boss ? "enemy_down_boss" : "enemy_down");
            Vector3 c, feet;
            StageBeat.EnemyDown(EnemyWorld(ei, out c, out feet) ? feet : Vector3.zero, boss);
            Tween.Stamp(fx, pos + new Vector2(0f, 40f), "撃破", PaperFx.BrassLight, PaperFx.BrassInk, PaperFx.Brass, boss ? 30 : 26, 0.7f, -6f);
        }

        // ---------------------------------------------------------------- とどめ (G2)

        /// <summary>
        /// とどめの一撃 (戦闘を決めた打撃): 白の閃き (0.12 秒) → 夜色の薄い幕 (0.3 秒) の 2 段 (FxSettings.Flash を掛ける・α は紙の読みやすさのため控えめ)、
        /// 舞台の露出の一瞬の上がりと小さな揺れ (StageBeat.FinishBlow・Shake)、敵の体の強い白 (HitFlash 1)、低い音 finish_blow、BGM を一瞬沈める。
        /// 時間のスロー (Tween.FinishSlow) は Show の冒頭が掛けている。カメラは寄らない (ユーザー裁定「酔う」)
        /// </summary>
        static void FinishBlowCue(GameRoot g, RectTransform fx, int ei, Vector2 hit)
        {
            bool boss = IsBossNode(g);
            Vector3 c, feet;
            StageBeat.FinishBlow(EnemyWorld(ei, out c, out feet) ? c : Vector3.zero, boss);
            StageBeat.HitFlash(ei, 1f);
            StageBeat.Shake(1f, 0.25f);
            Audio.Key("finish_blow");
            Audio.Duck(0.6f, 0.5f, 0.6f, 0.03f);
            FlashScreen(fx, PaperFx.Paper3, 0.42f, 0.12f);
            AfterVisual(0.1f, () => FlashScreen(fx, PaperFx.Night, 0.28f, 0.35f));
            Tween.RingBurst(fx, hit, new Color(1f, 1f, 0.95f, 0.9f), boss ? 320f : 260f, 0.5f);
        }

        // ---------------------------------------------------------------- 赤 (チェイン・猛り火・くべる・衝動)・青 (霊気) の出来事 (G9・A3・7)

        static bool _blazeOn;

        /// <summary>Show が絵と音で見せる赤と青の出来事 (旧は case が無く、表にも鍵が無いので無言だった)</summary>
        static bool IsRedEvent(GameEvent ev)
        {
            return ev is GameEvent_Cascaded || ev is GameEvent_CascadeRetargeted || ev is GameEvent_BlazeIgnited || ev is GameEvent_BlazeExtinguished
                || ev is GameEvent_BurnDischarged || ev is GameEvent_ImpulseKindled || ev is GameEvent_ImpulseReturned || ev is GameEvent_ImpulseKept
                || ev is GameEvent_AetherGained || ev is GameEvent_AetherDischarged || ev is GameEvent_RegenBroken || ev is GameEvent_HpLossCapped;
        }

        /// <summary>出来事ごとの間 (順送り): チェインは当たりの札の DamageDealt が本体より先に解決する順を崩さないよう 0.22 まで。ほかは 0.2</summary>
        static float RedEventGap(GameEvent ev) { return ev is GameEvent_Cascaded ? 0.22f : ev is GameEvent_BlazeIgnited ? 0.3f : 0.2f; }

        /// <summary>この出来事の記録と位置 (順送りは ctx.EventIndex/CapturedState・無ければ今の盤面の記録から探す)</summary>
        static IReadOnlyList<GameEvent> LogOf(GameRoot g, ReactionCtx ctx, GameEvent ev, out int index)
        {
            var log = ctx != null && ctx.CapturedState != null ? ctx.CapturedState.EventLog : (g != null && g.Rs != null && g.Rs.Combat != null ? g.Rs.Combat.EventLog : null);
            index = -1;
            if (log == null) return null;
            if (ctx != null && ctx.EventIndex >= 0 && ctx.EventIndex < log.Count && ReferenceEquals(log[ctx.EventIndex], ev)) { index = ctx.EventIndex; return log; }
            for (int k = log.Count - 1; k >= 0; k--) if (ReferenceEquals(log[k], ev)) { index = k; break; }
            return log;
        }

        /// <summary>生きている敵の胸の位置の一覧 (見えている盤面)。猛り火の輪・判の置き場</summary>
        static List<int> LivingEnemies(GameRoot g, ReactionCtx ctx)
        {
            var list = new List<int>();
            var st = ctx != null && ctx.Prev != null ? ctx.Prev : (g != null && g.Rs != null ? g.Rs.Combat : null);
            if (st == null) return list;
            for (int i = 0; i < st.Enemies.Count; i++) if (st.Enemies[i].Hp > 0) list.Add(i);
            return list;
        }

        static void ShowRedEvent(GameRoot g, RectTransform fx, GameEvent ev, ReactionCtx ctx)
        {
            if (fx == null) return;
            var playerPos = PlayerFloatPos(g, fx);
            switch (ev)
            {
                case GameEvent_Cascaded cas:
                {
                    // チェイン (赤の柱): 山札から札がめくれ、外れは山札の底へ落ち、当たりの札の幽霊が狙った敵へ飛んで判「チェイン」。段 (何段目か) で音程が上がる (Balatro の連鎖の快感)
                    int idx; var log = LogOf(g, ctx, ev, out idx);
                    int depth = 0, target = -1;
                    if (log != null && idx >= 0)
                    {
                        for (int k = idx - 1; k >= 0; k--) { if (log[k] is GameEvent_CardPlayed || log[k] is GameEvent_TurnEnded || log[k] is GameEvent_TurnStarted) break; if (log[k] is GameEvent_Cascaded) depth++; }
                        for (int k = idx + 1; k < log.Count && k <= idx + 16; k++)
                        {
                            if (log[k] is GameEvent_DamageDealt dd && dd.Source == "player") { target = dd.EnemyIndex ?? -1; break; }
                            if (log[k] is GameEvent_TurnEnded || log[k] is GameEvent_Cascaded) break;
                        }
                    }
                    if (target < 0 && ctx != null) target = ctx.EnemyIndex;
                    if (target < 0) { var live = LivingEnemies(g, ctx); if (live.Count > 0) target = live[0]; }
                    var pile = g.Anchor("pile-draw");
                    Vector2 from = pile != null ? Tween.CenterIn(pile, fx) : playerPos + new Vector2(0f, -120f);
                    Audio.KeyPitched("cascade_flip", 1f + 0.06f * depth, 0.8f);
                    if (pile != null) Tween.Punch(pile, 0.1f, 0.2f);
                    int revealed = cas.Revealed != null ? cas.Revealed.Count : 0;
                    int misses = Mathf.Clamp(cas.HitId != null ? revealed - 1 : revealed, 0, 3);
                    for (int m = 0; m < misses; m++)
                    {   // 外れ: 小さな夜の札が山札の上に跳ねて底へ落ちる (0.05 秒ずらし)
                        int mm = m;
                        AfterVisual(0.05f * m, () =>
                        {
                            var card = UiKit.NewRect("cascade-miss", fx);
                            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
                            card.sizeDelta = new Vector2(30f, 40f); card.anchoredPosition = from;
                            var sheet = PaperFx.Sheet(card, PaperFx.NightTag, "paper"); UiKit.Stretch(sheet.rectTransform, 0f, 0f, 0f, 0f); sheet.raycastTarget = false;
                            var cg = card.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false;
                            float side = (mm % 2 == 0 ? 1f : -1f) * (22f + 10f * mm);
                            Tween.Run(0.34f, k =>
                            {
                                if (card == null) return;
                                card.anchoredPosition = from + new Vector2(side * k, 70f * Mathf.Sin(k * Mathf.PI) - 10f * k);
                                card.localRotation = Quaternion.Euler(0f, 0f, side * 0.8f * k);
                                cg.alpha = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                            }, Ease.Linear, () => { if (card != null) UnityEngine.Object.Destroy(card.gameObject); });
                        });
                    }
                    if (cas.HitId == null) { Tween.Float(fx, from + new Vector2(0f, 70f), "チェイン 空振り", PaperFx.PaperDim, 22, 30f, 0.9f); break; }
                    CardDef def = null; try { def = Content.GetCardDef(cas.HitId); } catch (Exception) { }
                    var ghost = GhostToken(fx, from, cas.HitId, def);
                    var tSpr = target >= 0 && g.Battle != null ? g.Battle.EnemySprite(target) : null; var tPan = target >= 0 ? g.Anchor("enemy" + target) : null;
                    Vector2 to = tSpr != null ? Tween.CenterIn(tSpr, fx) : tPan != null ? EnemyFloatPos(g, fx, target) : playerPos;
                    int d2 = depth;
                    Tween.Run(0.12f, k => { if (ghost != null) ghost.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(k * Mathf.PI)); }, Ease.Linear, () =>
                    {
                        if (ghost == null) return;
                        Tween.Move(ghost, to, 0.24f, Ease.InOutQuad, () =>
                        {
                            if (ghost == null) return;
                            Audio.KeyPitched("cascade_hit", 1f + 0.08f * d2, 1f);
                            Tween.RingBurst(fx, to, PaperFx.Ember, 160f, 0.35f);
                            Tween.Stamp(fx, to + new Vector2(0f, 64f), d2 > 0 ? "チェイン ×" + (d2 + 1) : "チェイン", PaperFx.Paper2, PaperFx.BrassInk, PaperFx.Ember, 22, 0.6f, -7f);
                            var cg = ghost.GetComponent<CanvasGroup>(); if (cg == null) cg = ghost.gameObject.AddComponent<CanvasGroup>();
                            var gh = ghost;
                            Tween.Run(0.18f, k => { if (gh != null) { cg.alpha = 1f - k; gh.localScale = Vector3.one * (1f + 0.4f * k); } }, Ease.Linear, () => { if (gh != null) UnityEngine.Object.Destroy(gh.gameObject); });
                        });
                    });
                    break;
                }
                case GameEvent_CascadeRetargeted crt:
                {   // 当たりが対象を倒したので本体は先頭の敵へ (裁定 R7)
                    var pan = g.Anchor("enemy" + crt.ToIndex);
                    if (pan == null) return;
                    Tween.Float(fx, EnemyFloatPos(g, fx, crt.ToIndex) + new Vector2(0f, 80f), "本体はこの敵へ", PaperFx.PaperDim, 20, 30f, 0.9f);
                    break;
                }
                case GameEvent_BlazeIgnited _:
                {
                    // 猛り火に点いた (延焼の合計が 8 以上): 生きている敵全員の足元に熾火の輪、画面の縁が一瞬暖かく、判「猛り火」、舞台の灯が熾火へ寄る (StageBeat.Blaze)
                    _blazeOn = true;
                    Audio.Key("blaze_ignite");
                    StageBeat.Blaze(true);
                    HurtVignette(fx, PaperFx.Ember, 0.22f, 0.5f, "ember");
                    var live = LivingEnemies(g, ctx);
                    Vector2 sum = Vector2.zero; int n = 0;
                    foreach (var ei in live)
                    {
                        var spr = g.Battle != null ? g.Battle.EnemySprite(ei) : null;
                        var c = EnemyFloatPos(g, fx, ei); sum += c; n++;
                        Vector2 feet = spr != null ? Tween.CenterIn(spr, fx) + new Vector2(0f, -spr.rect.height * 0.42f) : c + new Vector2(0f, -80f);
                        Tween.RingBurst(fx, feet, PaperFx.Ember, 180f, 0.45f);
                        Tween.RingBurst(fx, feet, new Color(PaperFx.Ember.r, PaperFx.Ember.g, PaperFx.Ember.b, 0.6f), 110f, 0.3f);
                    }
                    Vector2 at = n > 0 ? sum / n : playerPos + new Vector2(200f, 0f);
                    Tween.Stamp(fx, at + new Vector2(0f, 110f), "猛り火", PaperFx.Paper2, PaperFx.BadInk, PaperFx.Ember, 26, 0.8f, -6f);
                    break;
                }
                case GameEvent_BlazeExtinguished _:
                {   // 消えた (段2b: 黙って消えていた)
                    _blazeOn = false;
                    Audio.Key("blaze_out");
                    StageBeat.Blaze(false);
                    var live = LivingEnemies(g, ctx);
                    Vector2 sum = Vector2.zero; int n = 0;
                    foreach (var ei in live) { sum += EnemyFloatPos(g, fx, ei); n++; }
                    Vector2 at = n > 0 ? sum / n : playerPos + new Vector2(200f, 0f);
                    Tween.Float(fx, at + new Vector2(0f, 110f), "猛り火が消えた", PaperFx.PaperDim, 22, 26f, 1.0f);
                    break;
                }
                case GameEvent_BurnDischarged bd:
                {   // 延焼を消費して刈る (火掻き・焼き払い・爆熱の解放): 敵の胸で熾火の輪と「延焼 N を解き放った」
                    var pan = g.Anchor("enemy" + bd.EnemyIndex);
                    if (pan == null) return;
                    Audio.Key("BurnTick");
                    var at = EnemyFloatPos(g, fx, bd.EnemyIndex);
                    Tween.RingBurst(fx, at, PaperFx.Ember, 170f, 0.4f);
                    Tween.Float(fx, at + new Vector2(0f, 84f), "延焼 " + bd.Amount + " を解き放った", PaperFx.Ember, 24, 34f, 1.0f, 320f);
                    break;
                }
                case GameEvent_ImpulseKindled ik:
                {
                    // くべる (段2): 手札の衝動の札が火の粉になって自分へ飛び込み、判「くべた」。札は engine の CardIds (最大 3 枚ぶんの絵)
                    Audio.Key("impulse_burn");
                    var fr = fx.rect;
                    int n = ik.CardIds != null ? Mathf.Min(ik.CardIds.Count, 3) : 0;
                    for (int i = 0; i < n; i++)
                    {
                        int ii = i; string cid = ik.CardIds[i];
                        AfterVisual(0.06f * i, () =>
                        {
                            CardDef cdef = null; try { cdef = Content.GetCardDef(cid); } catch (Exception) { }
                            Vector2 from = new Vector2(fr.center.x + 70f * (ii - (n - 1) / 2f), fr.yMin + (UiKit.Phone ? 120f : 170f));
                            var gh = GhostToken(fx, from, cid, cdef);
                            gh.localScale = Vector3.one * 0.7f;
                            var cg = gh.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false;
                            Tween.Run(0.26f, k => { if (gh == null) return; gh.anchoredPosition = Vector2.Lerp(from, playerPos, Tween.Apply(Ease.InQuad, k)); gh.localScale = Vector3.one * (0.7f - 0.4f * k); cg.alpha = 1f - k * k; }, Ease.Linear, () =>
                            {
                                if (gh != null) UnityEngine.Object.Destroy(gh.gameObject);
                                Tween.RingBurst(fx, playerPos, PaperFx.Ember, 120f, 0.3f);
                                for (int s = 0; s < 4; s++) { float ang = (137.5f * (s + ii * 4)) * Mathf.Deg2Rad; Tween.Projectile(fx, playerPos, playerPos + new Vector2(Mathf.Cos(ang), Mathf.Abs(Mathf.Sin(ang)) + 0.3f) * 70f, PaperFx.Ember, 16f, 0.22f, 10f); }
                            });
                        });
                    }
                    AfterVisual(0.3f, () => Tween.Stamp(fx, playerPos + new Vector2(0f, 90f), "くべた", PaperFx.Paper2, PaperFx.BadInk, PaperFx.Ember, 22, 0.6f, -6f));
                    break;
                }
                case GameEvent_ImpulseReturned ir:
                {   // 失効の行き先 (F2): 払えなかった札は捨て札へ
                    Audio.Key("impulse_burn", 0.5f);
                    var pile = g.Anchor("pile-discard");
                    Vector2 at = pile != null ? Tween.CenterIn(pile, fx) + new Vector2(0f, 60f) : playerPos + new Vector2(0f, -60f);
                    Tween.Float(fx, at, "衝動の失効 " + CardText.CardName(ir.CardId) + " は捨て札へ", PaperFx.PaperDim, 20, 28f, 1.1f, 360f);
                    break;
                }
                case GameEvent_ImpulseKept ik2:
                {   // 燠の守りで残した
                    Tween.Float(fx, playerPos + new Vector2(0f, 90f), "燠の守り " + CardText.CardName(ik2.CardId) + " を残した", PaperFx.BrassLight, 20, 30f, 1.1f, 360f);
                    break;
                }
                case GameEvent_AetherGained ag:
                {   // 霊気 (青): 妨害の成功の換金。青緑の輪と「霊気 +N」
                    if (ag.Amount <= 0) return;
                    Audio.Key("aether_gain");
                    var at = playerPos + new Vector2(70f, 90f);
                    Tween.RingBurst(fx, at, PaperFx.ManaLight, 100f, 0.3f);
                    Tween.Float(fx, at, "霊気 +" + ag.Amount, PaperFx.ManaLight, 26, 36f, 0.9f);
                    break;
                }
                case GameEvent_AetherDischarged ad:
                {   // 霊気放出: 表の音 (hit_big) ＋ 青緑の大きな輪と「霊気 N を放出!」
                    Audio.Key("AetherDischarged");
                    Tween.RingBurst(fx, playerPos, PaperFx.Mana, 240f, 0.45f);
                    Tween.FloatBig(fx, playerPos + new Vector2(0f, 90f), "霊気 " + ad.Spent + " を放出!", PaperFx.ManaLight, 30, PaperFx.Mana, 40f, 1.1f, 340f);
                    break;
                }
                case GameEvent_RegenBroken rb:
                {   // 再生を止めた (regenBreak): 敵の上に真鍮の一言
                    var pan = g.Anchor("enemy" + rb.EnemyIndex);
                    if (pan == null) return;
                    Tween.Float(fx, EnemyFloatPos(g, fx, rb.EnemyIndex) + new Vector2(0f, 84f), "再生が止まった", PaperFx.BrassLight, 24, 34f, 1.0f);
                    break;
                }
                case GameEvent_HpLossCapped hc:
                {   // 脈打つ欠片 (レリック) が HP の損失を止めた
                    Tween.RingBurst(fx, playerPos, PaperFx.BrassLight, 150f, 0.35f);
                    Tween.Float(fx, playerPos + new Vector2(0f, 100f), hc.Left > 0 ? "脈打つ欠片が損失を止めた（あと" + hc.Left + "回）" : "脈打つ欠片が損失を止めた（これで最後）", PaperFx.BrassLight, 20, 30f, 1.2f, 400f);
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- 勝利の戦利品 (G5・B08)

        /// <summary>上部バーの札の中の数字の TMP (G の数字＝int に読める最初の文字)。無ければ null</summary>
        static TMP_Text NumberTextIn(RectTransform tag)
        {
            if (tag == null) return null;
            var texts = tag.GetComponentsInChildren<TMP_Text>();
            foreach (var t in texts) { int v; if (t != null && int.TryParse(t.text, out v)) return t; }
            return null;
        }

        /// <summary>
        /// 倒した敵の位置から G の硬貨 (真鍮) と魔素の玉 (青緑) が上部バーの印 ("gold"・"mana") へ弧を描いて飛び、着くたび coin_land / mana_land と数字の数え上げ。
        /// 増分は engine の値 (戦闘の手の直前のランと終わったランの差)。終わったら ScreenFx.Shown に新しい値を覚えさせる＝報酬画面で二重に出ない。
        /// 撮影 (ScreenFx.Instant) では飛ばさず覚えるだけ。戻り値＝最後の硬貨が着くまでの秒数
        /// </summary>
        static float FlyLoot(GameRoot g, RectTransform fx, Vector2 from, int goldDelta, int manaDelta, RunState endedRs)
        {
            if (g == null || fx == null || endedRs == null) return 0f;
            var goldTag = g.Anchor("gold"); var manaTag = g.Anchor("mana");
            int goldNow = endedRs.Gold, manaNow = DeckRogue.Engine.Run.ManaOf(endedRs);
            Action remember = () => { ScreenFx.Shown(g, "gold", goldNow, null, null, " G", false); ScreenFx.Shown(g, "mana", manaNow, null, null, "", false); };
            if (ScreenFx.Instant || (goldDelta <= 0 && manaDelta <= 0)) { remember(); return 0f; }
            float last = 0f;
            if (goldDelta > 0 && goldTag != null)
            {
                var text = NumberTextIn(goldTag);
                int n = Mathf.Clamp(goldDelta / 3, 4, 10);
                int start = goldNow - goldDelta;
                Vector2 to = Tween.CenterIn(goldTag, fx);
                Audio.Key("coin_fly");
                for (int i = 0; i < n; i++)
                {
                    int ii = i; float dl = 0.045f * i;
                    // 出どころを黄金角で散らす (乱数を使わない)
                    float ang = (137.5f * i) * Mathf.Deg2Rad;
                    Vector2 f0 = from + new Vector2(Mathf.Cos(ang) * 34f, Mathf.Sin(ang) * 22f);
                    AfterVisual(dl, () =>
                    {
                        if (g.FxLayer != fx) return;
                        Tween.Projectile(fx, f0, to, PaperFx.Brass, 24f, 0.42f, 140f + 24f * (ii % 3), () =>
                        {
                            if (goldTag == null) return;
                            Audio.KeyPitched("coin_land", 1f + 0.05f * ii, 0.8f);
                            Tween.RingBurst(fx, to, PaperFx.BrassLight, 70f, 0.22f);
                            Tween.Punch(goldTag, 0.08f, 0.22f);
                            if (text != null) text.text = (start + Mathf.RoundToInt(goldDelta * (ii + 1) / (float)n)).ToString();
                        });
                    });
                    last = Mathf.Max(last, dl + 0.42f);
                }
            }
            if (manaDelta > 0 && manaTag != null)
            {
                int m = Mathf.Clamp(manaDelta / 4, 1, 5);
                Vector2 to = Tween.CenterIn(manaTag, fx);
                for (int i = 0; i < m; i++)
                {
                    int ii = i; float dl = 0.12f + 0.07f * i;
                    float ang = (137.5f * (i + 7)) * Mathf.Deg2Rad;
                    Vector2 f0 = from + new Vector2(Mathf.Cos(ang) * 30f, Mathf.Sin(ang) * 20f);
                    AfterVisual(dl, () =>
                    {
                        if (g.FxLayer != fx) return;
                        Tween.Projectile(fx, f0, to, PaperFx.Mana, 22f, 0.46f, 110f + 20f * (ii % 2), () =>
                        {
                            if (manaTag == null) return;
                            Audio.KeyPitched("mana_land", 1f + 0.06f * ii, 0.8f);
                            Tween.RingBurst(fx, to, PaperFx.ManaLight, 70f, 0.22f);
                            Tween.Punch(manaTag, 0.08f, 0.22f);
                        });
                    });
                    last = Mathf.Max(last, dl + 0.46f);
                }
            }
            AfterVisual(last + 0.02f, remember);
            return last;
        }

        // ---------------------------------------------------------------- 敗北の崩れ (G13)

        /// <summary>主人公が崩れる (敵の崩れと同じ Dissolve を薔薇の粒で)。defeat_fall。舞台の主人公が居なければ絵の UI だけ薄れる</summary>
        static void PlayerCrumble(GameRoot g, RectTransform fx, float dur)
        {
            Audio.Key("defeat_fall");
            Stage.PlayAnim("player", "hurt");
            var spr = g.Battle != null ? g.Battle.PlayerSprite() : null;
            var img = spr != null ? spr.GetComponent<Image>() : null;
            Tween.Run(dur, k =>
            {
                Stage.Dissolve("player", k);
                if (spr != null && !Stage.CombatWorld && img != null) img.color = new Color(1f, 1f, 1f, 1f - k * k);
            }, Ease.InQuad, null, () => Stage.Dissolve("player", 0f));
            if (fx == null) return;
            Vector2 c = spr != null ? Tween.CenterIn(spr, fx) : PlayerFloatPos(g, fx);
            float w = spr != null ? spr.rect.width * 0.3f : 40f, h = spr != null ? spr.rect.height * 0.4f : 60f;
            for (int m = 0; m < 10; m++)
            {
                float ang = (137.5f * m) * Mathf.Deg2Rad; float r = 0.3f + 0.7f * ((m * 7) % 10) / 10f;
                Vector2 p0 = c + new Vector2(Mathf.Cos(ang) * w * r, Mathf.Sin(ang) * h * r);
                float dl = dur * 0.7f * (m / 10f);
                AfterVisual(dl, () =>
                {
                    if (fx == null) return;
                    var rt = UiKit.NewRect("mote-rose", fx);
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    float sz = 12f + 8f * r; rt.sizeDelta = new Vector2(sz, sz); rt.anchoredPosition = p0;
                    var mi = rt.gameObject.AddComponent<Image>(); mi.sprite = ThemeFx.Glow(); mi.color = PaperFx.Rose; mi.raycastTarget = false;
                    float rise = 60f + 40f * r, sway = 14f * Mathf.Cos(ang);
                    Tween.Run(0.7f, k => { if (rt == null) return; rt.anchoredPosition = p0 + new Vector2(sway * Mathf.Sin(k * 3f), rise * k); mi.color = new Color(PaperFx.Rose.r, PaperFx.Rose.g, PaperFx.Rose.b, 1f - k * k); rt.localScale = Vector3.one * (1f - 0.4f * k); }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
                });
            }
        }
    }
}
