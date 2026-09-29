// Presenter.cs — 演出キューの骨格 (2026-09-07 M1)。エンジンの状態は一瞬で確定し、画面はイベントログの差分を
// 順に取り出して見せる (StS のアクションキューと同型)。M1 では「ダメージの浮き文字と揺れ・ブロック・回復」だけ。
// M2 で戦闘画面を作り直す時に、カードの飛び・敵の動き・ターンバナーをここへ足す。
// 座標は GameRoot.Anchors (画面の組み立てが登録した RectTransform) から取る。無ければ黙って飛ばす。
using System;
using System.Collections.Generic;
using DeckRogue.Engine;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class Presenter
    {
        /// <summary>直前に見たイベント数 (戦闘が変わったら 0 に戻る)</summary>
        static int _seen;
        static object _seenCombat;

        public static void Reset() { _seen = 0; _seenCombat = null; }
        /// <summary>続きから (2026-09-15): 読み戻した戦闘の既存のログは演出済みとして扱う (再開の一発目に古い浮き文字を出さない)</summary>
        public static void MarkSeen(GameState combat) { _seen = combat != null ? combat.EventLog.Count : 0; _seenCombat = combat; }

        /// <summary>
        /// この CardPlayed が実際に何をしたか (選択式の札は選んだモードで演出を分ける 2026-09-14 ユーザー指摘)。
        /// 直後のイベント (次の CardPlayed / TurnEnded まで) を見て 1=攻撃した・2=守った・0=どちらでもない
        /// </summary>
        static int _playHint;
        static int PlayOutcome(IReadOnlyList<GameEvent> log, int at)
        {
            bool atk = false, blk = false;
            for (int j = at + 1; j < log.Count; j++)
            {
                var e = log[j];
                if (e is GameEvent_CardPlayed || e is GameEvent_TurnEnded || e is GameEvent_CardSet) break;
                if (e is GameEvent_DamageDealt dd && dd.Source == "player") atk = true;
                if (e is GameEvent_BlockGained || e is GameEvent_IceBlockGained) blk = true;
            }
            return atk ? 1 : blk ? 2 : 0;
        }

        /// <summary>入力を塞ぐ (決着の余韻の間に古い戦闘画面を触らせない)。戻り値を呼ぶと解除</summary>
        public static Action BlockInput(GameRoot g)
        {
            var fx = g.FxLayer;
            if (fx == null) return delegate { };
            var block = UiKit.NewRect("inputblock2", fx);
            UiKit.Stretch(block, 0f, 0f, 0f, 0f);
            var bimg = block.gameObject.AddComponent<Image>();
            bimg.color = new Color(0f, 0f, 0f, 0f);
            bimg.raycastTarget = true;
            var cg = fx.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = true;
            return delegate { if (block != null) UnityEngine.Object.Destroy(block.gameObject); if (cg != null) cg.blocksRaycasts = false; };
        }

        /// <summary>直前に見た位置より後に新しいイベントがあるか (決着の最後の打撃を見せ切るための判定 2026-09-14)</summary>
        /// <summary>まだ演出していない灯の出来事 (LightGained/LightSpent/LightDischarged) を逆にたどった「その前の灯」(2026-09-20 灯籠。組み直しの初期値)。未演出が無ければ今の灯</summary>
        public static int LightBefore(GameState combat)
        {
            if (combat == null) return 0;
            int v = combat.Player.Light ?? 0;
            if (!HasNewEvents(combat)) return v;
            var log = combat.EventLog;
            for (int i = log.Count - 1; i >= _seen && i >= 0; i--)
            {
                if (log[i] is GameEvent_LightGained lg) v -= lg.Amount;
                else if (log[i] is GameEvent_LightSpent ls) v += ls.Amount;
                else if (log[i] is GameEvent_LightDischarged ld) v += ld.Spent;   // 前＝後＋払った量 (全て放出は後が0。灯の火床・炉心は払った残りがある。2026-09-24 T15)
            }
            return Math.Max(0, v);
        }

        public static bool HasNewEvents(GameState combat)
        {
            if (combat == null) return false;
            var log = combat.EventLog;
            if (_seenCombat != null && log.Count < _seen) return false;
            for (int i = Math.Min(_seen, log.Count); i < log.Count; i++)
                if (log[i] is GameEvent_DamageDealt || log[i] is GameEvent_EnemyDied || log[i] is GameEvent_CardPlayed) return true;
            return false;
        }

        /// <summary>このコマンドで敵の行動 (TurnEnded 以降) が起きたか = 古い盤面の上で順に見せる価値がある</summary>
        public static bool HasEnemyPhase(GameState combat)
        {
            if (combat == null) return false;
            var log = combat.EventLog;
            // 確認の窓 (発動/温存) の続きも敵フェーズ (2026-09-17 ユーザー「置物の誘発とかで得たブロックがキャラの表記に更新されなくない？」):
            // 旧実装は TurnEnded だけを見ていたので、発動の後の敵の攻撃〜ターン開始が「即組み直し」で新しい盤面 (ブロックは次のターンで 0) の上に浮き文字だけ出ていた
            var prev = _seenCombat as GameState;
            if (prev != null && prev.Phase == CombatPhases.AwaitingReaction && log.Count >= _seen && log.Count > 0 && log[0] is GameEvent_CombatStarted) return true;
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
            var visibleBoard = _seenCombat as GameState;   // 順送りは古い盤面 (仕込み札のトークンがまだ見えている) の上で見せる
            Canvas.ForceUpdateCanvases();
            var block = UiKit.NewRect("inputblock", fx);
            UiKit.Stretch(block, 0f, 0f, 0f, 0f);
            var bimg = block.gameObject.AddComponent<Image>();
            bimg.color = new Color(0f, 0f, 0f, 0f);
            bimg.raycastTarget = true;
            var blockCg = fx.GetComponent<CanvasGroup>();
            if (blockCg != null) blockCg.blocksRaycasts = true;
            float delay = 0f;
            int finishing = FinishingBlowIndex(log, Math.Min(_seen, log.Count), combat);
            var batchAt = new Dictionary<int, DollBatch>(); var batchLast = new HashSet<int>();
            PlanDollBatches(log, Math.Min(_seen, log.Count), batchAt, batchLast);   // 人形の粒を束ねる (2026-09-21)
            var hitAt = new Dictionary<int, HitPlan>();
            PlanCardHits(log, Math.Min(_seen, log.Count), hitAt);   // 自分の札の当たりの形と多段の位置 (2026-09-22)
            for (int i = Math.Min(_seen, log.Count); i < log.Count; i++)
            {
                var ev = log[i];
                float gap;
                if (ev is GameEvent_CardPlayed) { var cp = ev; _playHint = PlayOutcome(log, i); var cctx = new ReactionCtx { Prev = visibleBoard }; try { Show(g, fx, cp, true, cctx); } catch (Exception e) { Debug.LogWarning("[Presenter] " + e.Message); } continue; }   // 攻撃コマは即・間を取らない
                if (batchAt.ContainsKey(i)) gap = batchLast.Contains(i) ? 0.3f : 0.04f;   // 束ねた粒は 0.04 秒刻み。最後の1つで合計の数字を出すので少し置く
                else if (ev is GameEvent_DamageDealt) gap = (hitAt.ContainsKey(i) && hitAt[i].Volley && !hitAt[i].VolleyLast) ? 0.05f : 0.4f;   // 全体攻撃は一斉に (2026-09-22)
                else if (ev is GameEvent_TurnEnded || ev is GameEvent_TurnStarted) gap = 0.6f;
                else if (ev is GameEvent_BlockGained || ev is GameEvent_IceBlockGained || ev is GameEvent_HpHealed) gap = 0.15f;
                else if (IsStatusEvent(ev)) gap = 0.3f;
                else if (ev is GameEvent_ReactionTriggered) gap = 0.55f;   // 札が飛んで着弾するまで待ってから返しのダメージ (2026-09-17)
                else if (IsTrapEvent(ev)) gap = 0.3f;
                else if (ev is GameEvent_EnemyActionExecuting) gap = 0.32f;   // 予備動作 (縮む) → 当たり (2026-09-17)
                else if (ev is GameEvent_EnemyInterrupted) gap = 0.55f;   // 豹変の判を読ませてから次 (2026-09-17 ④)
                else if (IsEnemyActEvent(ev)) gap = 0.25f;
                else if (TableSound(ev) != null) gap = 0.12f;   // 表 (audio.json) で音だけ鳴るイベント (撃破・分裂…)
                else continue;
                var captured = ev;
                var ctx = ReactionContextFor(g, log, i, visibleBoard);
                if (batchAt.ContainsKey(i)) { ctx.Batch = batchAt[i]; ctx.BatchLast = batchLast.Contains(i); }
                if (hitAt.ContainsKey(i)) ctx.Hit = hitAt[i];
                if (i == finishing) { ctx.FinishingBlow = true; gap += 0.35f; }   // とどめはヒットストップぶん長く見せる
                Tween.After(delay, () => { try { Show(g, fx, captured, true, ctx); } catch (Exception e) { Debug.LogWarning("[Presenter] " + e.Message); } });
                delay += gap;
            }
            HoldLightIfAny(log, _seen, Mathf.Min(delay, 6f));
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
            var prevBoard = _seenCombat as GameState;   // コマンド前の盤面 (敵の実行中の技の名前・仕込み札の枠を引く)
            // Rebuild 直後は LayoutGroup が未計算 (全て原点) なので、的の座標を読む前にレイアウトを確定させる
            Canvas.ForceUpdateCanvases();
            // カードが敵へ飛ぶ 0.2 秒に着弾を合わせる
            float delay = 0f;
            for (int i = _seen; i < log.Count; i++) if (log[i] is GameEvent_DamageDealt dd && dd.Source == "player") { delay = 0.13f; break; }   // 着弾は振り抜き (0.11〜0.15s) に合わせる
            // 戦闘の始まり (2026-09-17 ⑨): 敵が順に現れ、強個体・幕ボスは名前の帯。その間は後の出来事 (ターン開始の帯など) を待たせる
            if (_seen == 0 && log.Count > 0 && log[0] is GameEvent_CombatStarted)
            {
                try { delay += ShowEntrance(g, fx, combat); } catch (Exception e) { Debug.LogWarning("[Presenter] entrance " + e.Message); }
            }
            int finishing = FinishingBlowIndex(log, _seen, combat);
            var batchAt = new Dictionary<int, DollBatch>(); var batchLast = new HashSet<int>();
            PlanDollBatches(log, _seen, batchAt, batchLast);   // 人形の粒を束ねる (2026-09-21)
            var hitAt = new Dictionary<int, HitPlan>();
            PlanCardHits(log, _seen, hitAt);   // 自分の札の当たりの形と多段の位置 (2026-09-22)
            // 差し替えられた意図の札は、組み直しで既に新しい札になっている。豹変の瞬間 (ShowEnemyAct) に跳ねて出すまで隠す (2026-09-17 ④)
            for (int i = _seen; i < log.Count; i++)
                if (log[i] is GameEvent_EnemyInterrupted ei && ei.Replaced)
                {
                    var pan = g.Anchor("enemy" + ei.EnemyIndex);
                    var tag = pan != null ? pan.Find("intent-tag") as RectTransform : null;
                    if (tag != null) tag.localScale = Vector3.zero;
                }
            for (int i = _seen; i < log.Count; i++)
            {
                var ev = log[i];
                if (ev is GameEvent_CardPlayed) { var cp = ev; _playHint = PlayOutcome(log, i); var cctx = new ReactionCtx { Prev = prevBoard }; try { Show(g, fx, cp, false, cctx); } catch (Exception e) { Debug.LogWarning("[Presenter] " + e.Message); } continue; }   // 攻撃コマは札を出した瞬間に
                if (!(ev is GameEvent_DamageDealt || ev is GameEvent_BlockGained || ev is GameEvent_IceBlockGained || ev is GameEvent_HpHealed || ev is GameEvent_TurnStarted || ev is GameEvent_TurnEnded || IsStatusEvent(ev) || IsTrapEvent(ev) || IsEnemyActEvent(ev) || TableSound(ev) != null)) continue;
                var captured = ev;
                var ctx = ReactionContextFor(g, log, i, prevBoard);
                if (batchAt.ContainsKey(i)) { ctx.Batch = batchAt[i]; ctx.BatchLast = batchLast.Contains(i); }
                if (hitAt.ContainsKey(i)) ctx.Hit = hitAt[i];
                if (i == finishing) ctx.FinishingBlow = true;
                // 連続する演出は 0.12 秒ずつずらす (同じ場所に重ならない・順番が読める)。束ねた人形の粒は 0.04 秒 (2026-09-21)
                Tween.After(delay, () => { try { Show(g, fx, captured, false, ctx); } catch (Exception e) { Debug.LogWarning("[Presenter] " + e.Message); } });
                delay += ctx.Batch != null ? (ctx.BatchLast ? 0.2f : 0.04f) : (ctx.Hit != null && ctx.Hit.Volley && !ctx.Hit.VolleyLast) ? 0.03f : ev is GameEvent_ReactionTriggered ? 0.45f : ev is GameEvent_EnemyActionExecuting ? 0.3f : ev is GameEvent_EnemyInterrupted ? 0.45f : 0.12f;
            }
            HoldLightIfAny(log, _seen, delay);
            _seen = log.Count;
            _seenCombat = combat;
        }

        /// <summary>灯の出来事が未演出にあれば、粒が届くまで灯籠の見せている値を組み直しに保たせる (2026-09-20 灯籠)</summary>
        static void HoldLightIfAny(IReadOnlyList<GameEvent> log, int from, float delay)
        {
            for (int i = Math.Max(0, from); i < log.Count; i++)
                if (log[i] is GameEvent_LightGained || log[i] is GameEvent_LightSpent || log[i] is GameEvent_LightDischarged) { LightUi.HoldFor(delay + 1.2f); return; }
        }

        // ---- からくり (仕込み札) の演出 (2026-09-17 ユーザー「戦闘の演出で足りていないもの」→ ⑤ リアクション発動から) ----
        // 発動: 札の幽霊が仕込み枠から跳ねて飛び出し、返し/打ち消しなら行動している敵の意図の札へ、守りなら自分へ。着弾で青緑の輪と星。枠の上に「発動」の判。
        // 打ち消し: 敵の意図の札に真鍮の×が押され、札が揺れる。温存: 「温存」の判 (灰)。期限切れ: 札が捨て札へ落ちる。壊し: 札が砕ける。空振り: 小さく「空振り」。

        /// <summary>
        /// 敵の行動の演出 (2026-09-17 ユーザー「戦闘の演出で足りていないもの」→ ①②): 実行の瞬間 (EnemyActionExecuting) に技の種類ごとの予備動作。
        /// 攻撃＝縮んで伸びる (当たりは DamageDealt で種類別)。防御＝盾を構える (絵は少し沈む)。筋力上げ＝膨らんで真鍮の輪。応援＝膨らんで味方へ輪が飛ぶ。
        /// 呪い＝藤の玉が自分へ飛ぶ。回復＝苔の玉が味方へ。盗み＝金の玉が G の札へ。山札喰い＝闇の玉が山札へ。逃走＝走り去る (BattleView)。隙＝「隙」とうつむく。
        /// 意図の札は実行の瞬間に一度跳ねる (どの札が動いたか)
        /// </summary>
        /// <summary>手番の札の「/ N」の N: 見えている盤面 (順送りは古い盤面) の敵の数 (組み直しの BuildTopBar と同じく倒れた敵も数える)。召喚で増えた番号は見逃さない</summary>
        static int PhaseEnemyCount(GameRoot g, ReactionCtx ctx, int acting)
        {
            int n = ctx != null && ctx.Prev != null ? ctx.Prev.Enemies.Count : 0;
            if (n == 0 && g != null && g.Rs != null && g.Rs.Combat != null) n = g.Rs.Combat.Enemies.Count;
            return Math.Max(n, acting + 1);
        }

        static void ShowEnemyAct(GameRoot g, RectTransform fx, GameEvent ev, ReactionCtx ctx, bool live)
        {
            switch (ev)
            {
                case GameEvent_EnemyActionExecuting ex:
                {
                    // 上部バーの手番の札の番号を進める「敵の番 ② / 3」(敵が2体以上の時。順送りの間だけ。2026-09-29)
                    if (live) { int cnt = PhaseEnemyCount(g, ctx, ex.EnemyIndex); BattleScreen.SetPhase(g, 1, cnt > 1 ? ex.EnemyIndex : -1, cnt); }
                    var pan = g.Anchor("enemy" + ex.EnemyIndex);
                    var spr = g.Battle != null ? g.Battle.EnemySprite(ex.EnemyIndex) : null;
                    if (pan == null) return;
                    var tag = pan.Find("intent-tag") as RectTransform;
                    if (tag != null) Tween.Punch(tag, 0.16f);
                    Vector2 center = spr != null ? Tween.CenterIn(spr, fx) : Tween.CenterIn(pan, fx);
                    string moveId = MoveIdFor(ctx, ex.EnemyIndex);
                    var playerRt = g.Anchor("player");
                    var pSpr = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    Vector2 playerPos = pSpr != null ? Tween.CenterIn(pSpr, fx) : (playerRt != null ? Tween.CenterIn(playerRt, fx) : center + new Vector2(-600f, 0f));
                    switch (ex.Kind)
                    {
                        case "attack":
                        case "destroy-set":
                        case "destroy-token":
                            if (spr != null) Tween.Squash(spr, 0.26f);
                            break;
                        case "defend":
                            if (spr != null) Tween.Squash(spr, 0.3f, 1.06f, 0.94f);
                            break;
                        case "buff":
                            if (spr != null) Tween.Puff(spr, 0.4f, 1.15f);
                            Tween.RingBurst(fx, center, PaperFx.Brass, 180f, 0.4f);
                            Audio.Key("EnemyIntentDeclared");
                            break;
                        case "rally":
                        {
                            if (spr != null) Tween.Puff(spr, 0.4f, 1.12f);
                            Tween.RingBurst(fx, center, PaperFx.Brass, 220f, 0.45f);
                            var st = ctx.Prev ?? (g.Rs != null ? g.Rs.Combat : null);
                            if (st != null)
                                for (int j = 0; j < st.Enemies.Count; j++)
                                {
                                    if (j == ex.EnemyIndex || st.Enemies[j].Hp <= 0) continue;
                                    var ally = g.Battle != null ? g.Battle.EnemySprite(j) : null; var apan = g.Anchor("enemy" + j);
                                    if (ally == null && apan == null) continue;
                                    var to = ally != null ? Tween.CenterIn(ally, fx) : Tween.CenterIn(apan, fx);
                                    Tween.Projectile(fx, center, to, PaperFx.Brass, 34f, 0.28f, 80f, () => Tween.RingBurst(fx, to, PaperFx.Brass, 120f, 0.3f));
                                }
                            break;
                        }
                        case "hex":
                            if (spr != null) Tween.Puff(spr, 0.3f, 1.08f);
                            Tween.Projectile(fx, center, playerPos + new Vector2(0f, 30f), PaperFx.Plum, 48f, 0.28f, 90f, () => Tween.RingBurst(fx, playerPos + new Vector2(0f, 30f), PaperFx.Plum, 150f, 0.35f));
                            break;
                        case "heal":
                            if (spr != null) Tween.Puff(spr, 0.3f, 1.06f);
                            break;
                        case "steal-gold":
                        {
                            if (spr != null) Tween.Squash(spr, 0.26f);
                            var gold = g.Anchor("gold");
                            if (gold != null) Tween.Projectile(fx, Tween.CenterIn(gold, fx), center, PaperFx.Brass, 36f, 0.32f, 60f, () => Tween.RingBurst(fx, center, PaperFx.Brass, 120f, 0.3f));
                            break;
                        }
                        case "seal":
                        case "mill":
                        {
                            var pile = g.Anchor("pile-draw");
                            if (spr != null) Tween.Squash(spr, 0.26f, 1.1f, 0.9f);
                            if (pile != null) { var to = Tween.CenterIn(pile, fx); Tween.Projectile(fx, center, to, PaperFx.PlumInk, 48f, 0.3f, 100f, () => { Tween.RingBurst(fx, to, PaperFx.Plum, 140f, 0.3f); Tween.Shake(pile, 8f, 0.3f); }); }
                            break;
                        }
                        case "rest":
                            Tween.Float(fx, center + new Vector2(0f, 40f), "隙", PaperFx.PaperDim, 28, 30f, 0.9f);
                            if (spr != null) Tween.Squash(spr, 0.5f, 1.04f, 0.96f);
                            break;
                        case "summon":
                        case "hatch":
                            if (spr != null) Tween.Puff(spr, 0.4f, 1.12f);
                            Tween.RingBurst(fx, center, PaperFx.Mana, 200f, 0.45f);
                            break;
                        case "flee":
                            if (spr != null) Tween.Squash(spr, 0.26f);
                            break;
                    }
                    break;
                }
                case GameEvent_EnemyHealed eh:
                {
                    Audio.Key("EnemyHealed");
                    var tspr = g.Battle != null ? g.Battle.EnemySprite(eh.TargetIndex) : null; var tpan = g.Anchor("enemy" + eh.TargetIndex);
                    if (tspr == null && tpan == null) return;
                    var to = tspr != null ? Tween.CenterIn(tspr, fx) : Tween.CenterIn(tpan, fx);
                    var hspr = g.Battle != null ? g.Battle.EnemySprite(eh.EnemyIndex) : null;
                    Action land = () => { Tween.IconBurst(fx, to + new Vector2(0f, 20f), "heart", new Color(0.6f, 1f, 0.6f, 0.9f), 100f); Tween.Float(fx, to + new Vector2(0f, 50f), "+" + eh.Amount, PaperFx.MossLight, 30, 40f, 0.8f); };
                    if (hspr != null && eh.EnemyIndex != eh.TargetIndex) Tween.Projectile(fx, Tween.CenterIn(hspr, fx), to, PaperFx.Moss, 44f, 0.3f, 90f, land); else land();
                    break;
                }
                case GameEvent_GoldStolen gs:
                {
                    Audio.Key("GoldStolen");
                    var gold = g.Anchor("gold");
                    var pos = gold != null ? Tween.CenterIn(gold, fx) : new Vector2(0f, 400f);
                    Tween.Float(fx, pos + new Vector2(0f, -40f), "−" + gs.Amount + "G", PaperFx.BrassLight, 28, 30f, 1.0f);
                    if (gold != null) Tween.Shake(gold, 6f, 0.3f);
                    break;
                }
                case GameEvent_CardsMilled cm:
                {
                    Audio.Key("CardsMilled");
                    var pile = g.Anchor("pile-draw");
                    if (pile == null) return;
                    Tween.Float(fx, Tween.CenterIn(pile, fx) + new Vector2(0f, 40f), "山札 −" + cm.Count, PaperFx.Plum, 26, 36f, 1.0f);
                    break;
                }
                case GameEvent_ScaldTick sc:
                {   // 烙印・火傷の疼きに出所の名札 (2026-09-23 人間ラン#17: 浮き数字そのものが無く、仕立屋で烙印でなく負傷を除いていた)
                    Audio.Key("ThornsReflected");
                    var pSpr2 = g.Battle != null ? g.Battle.PlayerSprite() : null; var prt2 = g.Anchor("player");
                    if (pSpr2 == null && prt2 == null) return;
                    var pos2 = pSpr2 != null ? Tween.CenterIn(pSpr2, fx) : Tween.CenterIn(prt2, fx);
                    int scalds = sc.Scalds ?? 0, brands = sc.Brands ?? 0; float dy = 40f;
                    if (scalds > 0) { Tween.Float(fx, pos2 + new Vector2(40f, dy), "火傷 −" + (sc.Amount - brands), UiKit.ColBad, 28, 36f, 0.9f); dy += 30f; }
                    if (brands > 0) { Tween.Float(fx, pos2 + new Vector2(40f, dy), "烙印 −" + brands, UiKit.ColBad, 28, 36f, 0.9f); dy += 30f; }
                    if (scalds == 0 && brands == 0) Tween.Float(fx, pos2 + new Vector2(40f, dy), "火傷・烙印 −" + sc.Amount, UiKit.ColBad, 28, 36f, 0.9f);
                    Stage.Flash("player"); if (live && g.Battle != null) g.Battle.NudgePlayerHp(-sc.Amount);
                    break;
                }
                case GameEvent_ThornsReflected tr:
                {
                    Audio.Key("ThornsReflected");
                    var pSpr = g.Battle != null ? g.Battle.PlayerSprite() : null; var prt = g.Anchor("player");
                    if (pSpr == null && prt == null) return;
                    var pos = pSpr != null ? Tween.CenterIn(pSpr, fx) : Tween.CenterIn(prt, fx);
                    Tween.HitFx(fx, pos, "claw", new Color(1f, 0.62f, 0.5f, 0.9f), false);
                    Tween.Float(fx, pos + new Vector2(40f, 40f), "とげ −" + tr.HpLoss, UiKit.ColBad, 28, 36f, 0.9f);
                    if (tr.HpLoss > 0) { Stage.Flash("player"); if (live && g.Battle != null) g.Battle.NudgePlayerHp(-tr.HpLoss); }
                    break;
                }
                case GameEvent_BurnTick bt:
                {
                    Audio.Key("BurnTick");
                    var spr = g.Battle != null ? g.Battle.EnemySprite(bt.EnemyIndex) : null; var pan = g.Anchor("enemy" + bt.EnemyIndex);
                    if (spr == null && pan == null) return;
                    var pos = spr != null ? Tween.CenterIn(spr, fx) : Tween.CenterIn(pan, fx);
                    Tween.IconBurst(fx, pos, "burn", new Color(PaperFx.Ember.r, PaperFx.Ember.g, PaperFx.Ember.b, 0.9f), 120f);
                    Tween.Float(fx, pos + new Vector2(0f, 40f), bt.Amount.ToString(), PaperFx.Ember, 34, 44f, 0.9f);
                    if (live && g.Battle != null) g.Battle.NudgeEnemyHp(bt.EnemyIndex, -bt.Amount);
                    break;
                }
                case GameEvent_RegenTicked rg:
                {
                    Audio.Key("RegenTicked");
                    var spr = g.Battle != null ? g.Battle.EnemySprite(rg.EnemyIndex) : null; var pan = g.Anchor("enemy" + rg.EnemyIndex);
                    if (spr == null && pan == null) return;
                    var pos = spr != null ? Tween.CenterIn(spr, fx) : Tween.CenterIn(pan, fx);
                    Tween.IconBurst(fx, pos + new Vector2(0f, 20f), "heart", new Color(0.6f, 1f, 0.6f, 0.9f), 90f);
                    Tween.Float(fx, pos + new Vector2(0f, 50f), "+" + rg.Amount, PaperFx.MossLight, 28, 36f, 0.8f);
                    break;
                }
                case GameEvent_BlockShattered bs:
                {
                    Audio.Key("BlockShattered");
                    var spr = g.Battle != null ? g.Battle.EnemySprite(bs.EnemyIndex) : null; var pan = g.Anchor("enemy" + bs.EnemyIndex);
                    if (spr == null && pan == null) return;
                    var pos = spr != null ? Tween.CenterIn(spr, fx) : Tween.CenterIn(pan, fx);
                    Tween.IconBurst(fx, pos, "shield", new Color(PaperFx.Sky.r, PaperFx.Sky.g, PaperFx.Sky.b, 0.9f), 140f);
                    Tween.RingBurst(fx, pos, PaperFx.SkyLight, 180f, 0.35f);
                    Tween.Float(fx, pos + new Vector2(0f, 50f), "ブロックを砕いた " + bs.Amount, PaperFx.SkyLight, 26, 36f, 0.9f);
                    Stage.Shake(5f, 0.2f);
                    if (live && g.Battle != null) g.Battle.SetEnemyBlock(bs.EnemyIndex, 0);
                    break;
                }
                case GameEvent_EnemyInterrupted ei:
                {
                    // ④ 豹変の瞬間 (2026-09-17): HP半分・目覚め・仲間の死亡で行動が変わった。絵がひと膨らみして薔薇の輪と揺れ、
                    // HP バーの「行動が変わる線」の目盛りが弾け、胸元に理由の判。差し替え (自ターン中) なら古い意図の札が落ちて新しい札が跳ね、
                    // 「行動が変わった」の一言。敵フェーズ中 (差し替えなし) は「次のターンから行動が変わる」
                    var pan = g.Anchor("enemy" + ei.EnemyIndex);
                    var spr = g.Battle != null ? g.Battle.EnemySprite(ei.EnemyIndex) : null;
                    if (pan == null) return;
                    Audio.Key("EnemyInterrupted");
                    Vector2 center = spr != null ? Tween.CenterIn(spr, fx) : Tween.CenterIn(pan, fx);
                    if (spr != null) { Tween.Puff(spr, 0.45f, 1.22f); Tween.Shake(spr, 7f, 0.45f); }
                    Stage.Flash("enemy" + ei.EnemyIndex);
                    Stage.Shake(8f, 0.3f);
                    Tween.RingBurst(fx, center, PaperFx.Rose, 280f, 0.5f);
                    Tween.RingBurst(fx, center, PaperFx.BrassLight, 170f, 0.35f);
                    Tween.ScreenFlash(fx, new Color(PaperFx.Rose.r, PaperFx.Rose.g, PaperFx.Rose.b, 0.16f), 0.3f);
                    // 目盛り (行動が変わる線) が弾ける。組み直し済みの帳面から目盛りは消えているので、半分の線は HP バーの中央から出す
                    var mark = pan.Find("strip/hpbar/inner/mark") as RectTransform;   // 目盛りは塗りと同じ inner の子 (2026-09-29 p04)
                    var bar = pan.Find("strip/hpbar") as RectTransform;
                    Vector2? mp = null;
                    if (mark != null) mp = Tween.CenterIn(mark, fx);
                    else if (bar != null && ei.Trigger == EnemyInterruptTriggers.HpBelowHalf) mp = Tween.CenterIn(bar, fx);
                    if (mp.HasValue)
                    {
                        Tween.RingBurst(fx, mp.Value, PaperFx.Brass, 110f, 0.4f);
                        Tween.IconBurst(fx, mp.Value, "star", new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.95f), 52f);
                        if (bar != null) Tween.Punch(bar, 0.12f);
                    }
                    string why = ei.Trigger == EnemyInterruptTriggers.DamageTaken ? "目を覚ました!" : ei.Trigger == EnemyInterruptTriggers.HpBelowHalf ? "HPが半分を切った!" : ei.Trigger == EnemyInterruptTriggers.Alone ? "仲間が全滅した!" : "仲間が倒れた!";
                    Tween.Stamp(fx, center + new Vector2(0f, 24f), why, PaperFx.Paper2, PaperFx.BadInk, PaperFx.Rose, 22, 0.75f, -6f);
                    var tag = pan.Find("intent-tag") as RectTransform;
                    if (ei.Replaced)
                    {
                        // 古い意図の札が落ちる → 新しい札が跳ねる (組み直し済みの札は既に新しい意図)
                        Vector2 tagPos = tag != null ? Tween.CenterIn(tag, fx) : center + new Vector2(0f, 150f);
                        if (ei.Before != null) IntentGhostDrop(fx, tagPos, ei.Before);
                        if (tag != null)
                        {
                            var paper = tag.Find("paper");
                            var pImg = paper != null ? paper.GetComponent<Image>() : null;
                            tag.localScale = Vector3.zero;
                            var tagC = tag;
                            Tween.After(0.18f, () =>
                            {
                                if (tagC == null) return;
                                Tween.Scale(tagC, Vector3.one, 0.3f, Ease.OutBack);
                                if (pImg != null) Tween.Flash(pImg, PaperFx.RoseLight, 0.5f);
                                Tween.RingBurst(fx, tagPos, PaperFx.Rose, 150f, 0.35f);
                            });
                        }
                        Tween.After(0.42f, () => Tween.Float(fx, tagPos + new Vector2(0f, -56f), "行動が変わった", PaperFx.BrassLight, 26, 26f, 1.0f));   // 古い札が落ちきってから
                    }
                    else Tween.Float(fx, center + new Vector2(0f, 80f), "次のターンから行動が変わる", PaperFx.Paper2, 20, 28f, 1.1f);
                    break;
                }
                case GameEvent_EnemyDied ed:
                    // 倒れた (2026-09-17 ユーザー「倒した敵は消えるようにしたほうが良くない？」): 着弾の後に崩して消す。組み直しの前でも後でも1回だけ (BattleView が覚える)
                    if (g.Battle != null) g.Battle.KillEnemy(g, ed.EnemyIndex, false);
                    break;
                case GameEvent_EnemyFled ef:
                    if (g.Battle != null) g.Battle.KillEnemy(g, ef.EnemyIndex, true);
                    break;
                case GameEvent_EnemyStaggered es:
                {
                    Audio.Key("EnemyStaggered");
                    var spr = g.Battle != null ? g.Battle.EnemySprite(es.EnemyIndex) : null; var pan = g.Anchor("enemy" + es.EnemyIndex);
                    if (spr == null && pan == null) return;
                    var pos = spr != null ? Tween.CenterIn(spr, fx) : Tween.CenterIn(pan, fx);
                    if (spr != null) Tween.Shake(spr, 10f, 0.4f);
                    Tween.Float(fx, pos + new Vector2(0f, 50f), "体勢を崩した", PaperFx.BrassLight, 26, 36f, 1.0f);
                    break;
                }
            }
        }

        /// <summary>灯の出どころ (2026-09-20 灯籠): 人形ならその人形、リーダーのパッシブならひなたの竿の灯籠 (絵の右上)、回復・札ならひなたの胸</summary>
        /// <summary>置物 (人形・道具) の名前を uid から引く (2026-09-22 友人ラン「人形・ひなた・置物の効果が混在して分からない」＝粒に出所を添える):
        /// 見えている盤面 → 今の盤面 → 舞台の人形の入れ物 (DollInfo) の順。無ければ null (札のプレイなど)</summary>
        static string PermanentName(GameRoot g, ReactionCtx ctx, string uid)
        {
            if (uid == null) return null;
            var boards = new[] { ctx != null ? ctx.Prev : null, g.Rs != null ? g.Rs.Combat : null };
            foreach (var st in boards)
            {
                if (st == null) continue;
                foreach (var p in st.Player.Permanents) if (p.Uid == uid) return p.Def.Name;
            }
            if (g.Battle != null)
            {
                var spr = g.Battle.DollSprite(uid);
                var info = spr != null && spr.parent != null ? spr.parent.GetComponent<BattleView.DollInfo>() : null;
                if (info != null) { try { return Content.GetCardDef(info.CardId).Name; } catch (Exception) { } }
            }
            return null;
        }

        /// <summary>灯の出どころの名前 (「ひなた 灯 +1」「灯芯の人形 灯 +1」「回復 灯 +1」)。残り火・札のプレイは null</summary>
        static string LightSourceName(GameRoot g, ReactionCtx ctx, GameEvent_LightGained lg)
        {
            if (lg.Source == "passive") { try { return g.Rs != null ? Content.GetLeaderDef(g.Rs.LeaderId).Name : null; } catch (Exception) { return null; } }
            if (lg.Source == "heal") return "回復";
            if (lg.Source == "carry") return null;
            return PermanentName(g, ctx, lg.SourceUid);
        }

        /// <summary>
        /// 浮き数字・判の基準点 (2026-09-22): 敵と自分の「入れ物」は足元の線から高さ 720 の矩形で、その中心はスマホ (キャンバス高さ 675) では画面の上端より上に出る
        /// ＝スマホでは与ダメの数字が一度も画面に入っていなかった (PC でも頭のだいぶ上)。絵 (舞台のビルボード) の中心＋高さの 0.3 (胸〜頭) を使い、絵が無ければ入れ物の中心
        /// </summary>
        static Vector2 EnemyFloatPos(GameRoot g, RectTransform fx, int ei)
        {
            var spr = g.Battle != null ? g.Battle.EnemySprite(ei) : null;
            if (spr != null) return Tween.CenterIn(spr, fx) + new Vector2(0f, spr.rect.height * 0.1f);   // 敵は胸 (0.3 だと頭上の意図の札に重なった)
            var rt = g.Anchor("enemy" + ei);
            return rt != null ? Tween.CenterIn(rt, fx) : Vector2.zero;
        }
        static Vector2 PlayerFloatPos(GameRoot g, RectTransform fx)
        {
            var spr = g.Battle != null ? g.Battle.PlayerSprite() : null;
            if (spr != null) return Tween.CenterIn(spr, fx) + new Vector2(0f, spr.rect.height * 0.3f);
            var rt = g.Anchor("player");
            return rt != null ? Tween.CenterIn(rt, fx) : Vector2.zero;
        }

        static Vector2 LightSourcePos(GameRoot g, RectTransform fx, GameEvent_LightGained lg, ReactionCtx ctx)
        {
            var ps = g.Battle != null ? g.Battle.PlayerSprite() : null;
            Vector2 body = ps != null ? Tween.CenterIn(ps, fx) : (g.Anchor("player") != null ? Tween.CenterIn(g.Anchor("player"), fx) : Vector2.zero);
            string uid = lg.SourceUid;
            if (uid != null && g.Battle != null)
            {
                var ds = g.Battle.DollSprite(uid);
                if (ds != null) return Tween.CenterIn(ds, fx);
            }
            if (lg.Source == "passive" || (uid != null && uid.StartsWith("leader_")))
            {   // 竿の先の灯籠: 絵の右上 (幅の 0.35・高さの 0.15)
                if (ps != null) return body + new Vector2(ps.rect.width * 0.35f, ps.rect.height * 0.15f);
                return body + new Vector2(60f, 40f);
            }
            return body + new Vector2(0f, ps != null ? ps.rect.height * 0.1f : 20f);
        }

        /// <summary>舞台の人形の中心 (号令・大行列の的)。見えている盤面の人形を点灯した順に</summary>
        static List<Vector2> DollTargets(GameRoot g, RectTransform fx, ReactionCtx ctx)
        {
            var list = new List<Vector2>();
            var board = ctx != null && ctx.Prev != null ? ctx.Prev : (g.Rs != null ? g.Rs.Combat : null);
            if (board == null || g.Battle == null) return list;
            try
            {
                foreach (var d in g.Battle.StageDolls(board))
                {
                    var spr = g.Battle.DollSprite(d.Uid);
                    if (spr != null) list.Add(Tween.CenterIn(spr, fx));
                }
            }
            catch (Exception) { }
            return list;
        }

        /// <summary>エナジーを払った演出 (2026-09-17 ⑦): 輪が跳ねる。X の札は払った量 (コマンド前後のエナジーの差) ぶんの真鍮の玉が輪から行き先へ飛び、「X = N」の判</summary>
        static void ShowEnergyPaid(GameRoot g, RectTransform fx, CardDef def, ReactionCtx ctx)
        {
            var orb = g.Anchor("energy");
            var cur = g.Rs != null ? g.Rs.Combat : null;
            var prev = ctx != null ? ctx.Prev : null;
            if (orb == null || def == null || cur == null) return;
            int paid = prev != null ? Math.Max(0, prev.Player.Energy - cur.Player.Energy) : def.Cost;
            if (def.XCost != true) { if (paid > 0 || def.Cost > 0) Tween.Punch(orb, 0.14f, 0.25f); return; }
            int effX = paid + (cur.XBonus ?? 0) + (def.XBonus ?? 0);
            Tween.Punch(orb, 0.22f, 0.3f);
            var from = Tween.CenterIn(orb, fx);
            // 行き先: 直後に殴った敵、無ければ自分
            RectTransform dest = null;
            var log = cur.EventLog;
            for (int k = log.Count - 1, seen = 0; k >= 0 && seen < 12; k--, seen++) if (log[k] is GameEvent_DamageDealt dd && dd.Source == "player") { dest = g.Anchor("enemy" + (dd.EnemyIndex ?? 0)); break; }
            if (dest == null) dest = g.Anchor("player");
            Vector2 to = dest != null ? Tween.CenterIn(dest, fx) + new Vector2(0f, 60f) : from + new Vector2(300f, 200f);
            for (int i = 0; i < Math.Min(paid, 8); i++)
            {
                int ii = i; float dl = 0.04f * i;
                Tween.After(dl, () => Tween.Projectile(fx, from, to, PaperFx.Brass, 30f, 0.26f, 40f + 20f * (ii % 3), () => Tween.RingBurst(fx, to, PaperFx.BrassLight, 90f, 0.25f)));
            }
            Tween.Stamp(fx, from + new Vector2(0f, 96f), "X = " + effX, PaperFx.BrassLight, PaperFx.BrassInk, PaperFx.Brass, 22, 0.7f, -6f);
        }

        /// <summary>
        /// 戦闘の始まり (2026-09-17 ⑨): 敵が順に現れる (少し小さく薄い所から等身大へ・足元に土煙の輪)。強個体は名前の帯、幕ボスは帯＋舞台がゆっくり寄る。
        /// 戻り値＝後の出来事を待たせる秒数
        /// </summary>
        static float ShowEntrance(GameRoot g, RectTransform fx, GameState combat)
        {
            if (g.Battle == null || combat == null) return 0f;
            string nodeType = null;
            try { var node = DeckRogue.Engine.Run.CurrentNode(g.Rs); nodeType = node != null ? node.Type : null; } catch (Exception) { }
            bool boss = nodeType == MapNodeTypes.Boss, elite = nodeType == MapNodeTypes.Elite;
            float step = 0.09f;
            for (int i = 0; i < combat.Enemies.Count; i++)
            {
                if (combat.Enemies[i].Hp <= 0) continue;
                var spr = g.Battle.EnemySprite(i);
                if (spr == null) continue;
                var img = spr.GetComponent<Image>();
                var origin = spr.anchoredPosition; float h = spr.rect.height; float pivotY = spr.pivot.y;
                spr.localScale = new Vector3(0.7f, 0.7f, 1f);
                spr.anchoredPosition = origin + new Vector2(0f, -h * pivotY * (1f - 0.7f) + 40f);
                if (img != null) img.color = new Color(1f, 1f, 1f, 0f);
                var srt = spr; var simg = img; float dl = 0.12f + step * i;
                Tween.After(dl, () =>
                {
                    if (srt == null) return;
                    Tween.Run(0.42f, k =>
                    {
                        if (srt == null) return;
                        float sc = 0.7f + 0.3f * Tween.Apply(Ease.OutBack, k);
                        srt.localScale = new Vector3(sc, sc, 1f);
                        srt.anchoredPosition = origin + new Vector2(0f, -h * pivotY * (1f - sc) + 40f * (1f - Tween.Apply(Ease.OutQuad, k)));
                        if (simg != null) simg.color = new Color(1f, 1f, 1f, Mathf.Clamp01(k * 2.5f));
                    }, Ease.Linear, () => { if (srt != null) { srt.localScale = Vector3.one; srt.anchoredPosition = origin; } if (simg != null) simg.color = Color.white; });
                    Tween.After(0.2f, () => { if (srt != null) Tween.RingBurst(fx, Tween.CenterIn(srt, fx) + new Vector2(0f, -h * 0.42f), new Color(PaperFx.Paper2.r, PaperFx.Paper2.g, PaperFx.Paper2.b, 0.55f), 170f, 0.4f); });
                });
            }
            float wait = 0.35f + step * combat.Enemies.Count;
            if (!(boss || elite)) return wait;
            // 名前の帯 (画面の真ん中を横切る夜の帯に真鍮の線と名前)。ボスは舞台がゆっくり寄る。帯の間は入力を塞ぐ
            string enc = null;
            try
            {   // 節の編成名。ただし編成のメンバーと戦っている敵が食い違う時 (デバッグの敵指定) は敵の名前
                var node = DeckRogue.Engine.Run.CurrentNode(g.Rs);
                if (node != null && node.EncounterId != null)
                {
                    bool match = false;
                    foreach (var m in Content.ResolveEncounter(node.EncounterId)) if (combat.Enemies.Count > 0 && m.EnemyId == combat.Enemies[0].EnemyId) match = true;
                    if (match) enc = Content.EncounterName(node.EncounterId);
                }
            }
            catch (Exception) { }
            if (string.IsNullOrEmpty(enc)) { try { enc = Content.GetEnemyDef(combat.Enemies[0].EnemyId).Name; } catch (Exception) { enc = ""; } }
            float hold = boss ? 1.5f : 1.1f;
            Tween.After(0.35f, () => NameBand(fx, boss ? "幕ボス" : "強個体", enc, boss, hold));
            if (boss) { Stage.Dolly(0.9f, 1.3f, 0.7f, 0.9f); Audio.Ui("act_start"); }
            var block = UiKit.NewRect("inputblock", fx);
            UiKit.Stretch(block, 0f, 0f, 0f, 0f);
            var bimg = block.gameObject.AddComponent<Image>(); bimg.color = new Color(0f, 0f, 0f, 0f); bimg.raycastTarget = true;
            var cg = fx.GetComponent<CanvasGroup>(); if (cg != null) cg.blocksRaycasts = true;
            Tween.After(0.35f + hold + 0.5f, () => { if (block != null) UnityEngine.Object.Destroy(block.gameObject); if (cg != null) cg.blocksRaycasts = false; });
            return 0.35f + hold + 0.4f;
        }

        /// <summary>名前の帯: 夜の帯 (画面の幅いっぱい・高さ 120〜150) に真鍮の細い線、上に小さな肩書、真ん中に名前。左から滑り込んで hold の後に消える</summary>
        static void NameBand(RectTransform fx, string kicker, string name, bool boss, float hold)
        {
            if (fx == null) return;
            float h = boss ? 150f : 118f;
            var rt = UiKit.NewRect("nameband", fx);
            UiKit.Anchor(rt, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -h / 2f + 40f), new Vector2(0f, h / 2f + 40f));
            var bg = rt.gameObject.AddComponent<Image>(); bg.color = new Color(PaperFx.Night.r, PaperFx.Night.g, PaperFx.Night.b, 0.84f); bg.raycastTarget = false;
            foreach (float y in new[] { 0f, 1f })
            {
                var line = UiKit.NewRect("line", rt);
                UiKit.Anchor(line, new Vector2(0f, y), new Vector2(1f, y), new Vector2(0f, y == 0f ? 4f : -6f), new Vector2(0f, y == 0f ? 6f : -4f));
                var li = line.gameObject.AddComponent<Image>(); li.color = PaperFx.Brass; li.raycastTarget = false;
            }
            var inner = UiKit.NewRect("inner", rt);
            UiKit.Stretch(inner, 0f, 0f, 0f, 0f);
            var kt = UiKit.Deco(inner, kicker, boss ? 20 : 17, PaperFx.BrassLight, TextAnchor.MiddleCenter);
            UiKit.Anchor(kt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -40f), new Vector2(0f, -10f));
            kt.characterSpacing = 12f;
            var nt = UiKit.Deco(inner, name, boss ? 48 : 38, PaperFx.Paper, TextAnchor.MiddleCenter);
            UiKit.Anchor(nt.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, h - 40f));
            nt.characterSpacing = 4f;
            var cg = rt.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false; cg.alpha = 0f;
            inner.anchoredPosition = new Vector2(-60f, 0f);
            var innerC = inner;
            Tween.Run(0.28f, k => { if (cg != null) cg.alpha = k; if (innerC != null) innerC.anchoredPosition = new Vector2(-60f * (1f - Tween.Apply(Ease.OutCubic, k)), 0f); }, Ease.Linear, () =>
            {
                Tween.After(hold, () => Tween.Run(0.35f, k => { if (cg != null) cg.alpha = 1f - k; if (innerC != null) innerC.anchoredPosition = new Vector2(40f * k, 0f); }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); }));
            });
        }

        /// <summary>
        /// 決着の余韻 (2026-09-17 ⑧): 勝利＝「勝利」の帯の下に戦いの記録 (ターン数・与えたダメージ・受けたダメージ・読み勝ち・完全に凌いだ・打ち消し) が1行ずつ積み上がる。
        /// 幕ボスは「幕ボス撃破」で長めに、舞台がゆっくり寄る。敗北＝画面が暗転して「倒れた…」。どれも画面を触ると飛ばせる。終わったら onDone (=報酬/敗北の画面へ)
        /// </summary>
        public static void ShowOutcome(GameRoot g, RunState endedRs, GameState final, Action onDone)
        {
            var fx = g.FxLayer;
            bool lost = endedRs.Phase == RunPhases.Lost;
            if (lost) Audio.Key("PlayerDied");   // 撃破の音は KillEnemy (EnemyDied) が鳴らす
            if (fx == null) { Tween.After(0.7f, () => onDone?.Invoke()); return; }
            string nodeType = null;
            try { var node = DeckRogue.Engine.Run.CurrentNode(endedRs); nodeType = node != null ? node.Type : null; } catch (Exception) { }
            bool boss = nodeType == MapNodeTypes.Boss, elite = nodeType == MapNodeTypes.Elite;
            bool done = false;
            var block = UiKit.NewRect("inputblock2", fx);
            UiKit.Stretch(block, 0f, 0f, 0f, 0f);
            var bimg = block.gameObject.AddComponent<Image>(); bimg.color = new Color(0f, 0f, 0f, 0f); bimg.raycastTarget = true;
            var cg = fx.GetComponent<CanvasGroup>(); if (cg != null) cg.blocksRaycasts = true;
            var layer = UiKit.NewRect("outcome", fx);
            UiKit.Stretch(layer, 0f, 0f, 0f, 0f);
            var lcg = layer.gameObject.AddComponent<CanvasGroup>(); lcg.blocksRaycasts = false;
            Action finish = () =>
            {
                if (done) return; done = true;
                if (block != null) UnityEngine.Object.Destroy(block.gameObject);
                if (cg != null) cg.blocksRaycasts = false;
                if (layer != null) UnityEngine.Object.Destroy(layer.gameObject);
                Time.timeScale = 1f;
                onDone?.Invoke();
            };
            // 画面を触ったら飛ばす (帯が出てから)
            var skip = block.gameObject.AddComponent<Button>(); skip.transition = Selectable.Transition.None; skip.targetGraphic = bimg;
            bool skippable = false;
            skip.onClick.AddListener(delegate { if (skippable) finish(); });
            Tween.After(0.5f, () => skippable = true);
            if (lost)
            {
                // 暗転: 地の色が 1.2 秒で覆い、薔薇の「倒れた…」
                var dark = UiKit.NewRect("dark", layer);
                UiKit.Stretch(dark, 0f, 0f, 0f, 0f);
                var dimg = dark.gameObject.AddComponent<Image>(); dimg.color = new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0f); dimg.raycastTarget = false;
                Tween.Run(1.2f, k => { if (dimg != null) dimg.color = new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0.88f * k); }, Ease.OutQuad);
                Tween.After(0.5f, () => { OutcomeBand(layer, "倒れた…", PaperFx.Rose, 44); Audio.Ui("lose"); });
                Tween.After(2.2f, finish);
                return;
            }
            // 勝利: 帯 → 記録の行が 0.13 秒ごとに積み上がる → 余韻 → 報酬へ
            string title = boss ? "幕ボス撃破" : elite ? "強個体撃破" : "勝利";
            Tween.After(0.15f, () => { OutcomeBand(layer, title, PaperFx.BrassLight, boss ? 52 : 44); Audio.Ui("win"); });
            if (boss) Stage.Dolly(0.8f, 1.6f, 1.2f, 0.8f);
            var lines = SummaryLines(final != null ? final.EventLog : null);
            float y0 = -56f; float t = 0.42f;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i]; float y = y0 - 34f * i;
                Tween.After(t, () => OutcomeLine(layer, line, y));
                t += 0.12f;
            }
            Tween.After(t + (boss ? 1.6f : elite ? 0.9f : 0.65f), finish);
        }

        /// <summary>決着の帯: 画面の真ん中の紙の帯に大きな文字 (1.3 倍から押し当てるように縮む)</summary>
        static void OutcomeBand(RectTransform layer, string text, Color ink, int size)
        {
            var rt = UiKit.NewRect("outcome-band", layer);
            UiKit.Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, 20f), new Vector2(420f, 20f + size + 44f));
            rt.localRotation = Quaternion.Euler(0f, 0f, -1.5f);
            var bg = rt.gameObject.AddComponent<Image>(); bg.sprite = PaperFx.Panel; bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 1f; bg.raycastTarget = false;
            var edge = UiKit.NewRect("edge", rt);
            UiKit.Anchor(edge, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(28f, 8f), new Vector2(-28f, 11f));
            var ei = edge.gameObject.AddComponent<Image>(); ei.color = PaperFx.Brass; ei.raycastTarget = false;
            var tx = UiKit.Deco(rt, text, size, ink == PaperFx.Rose ? PaperFx.BadInk : PaperFx.BrassInk, TextAnchor.MiddleCenter);
            UiKit.Stretch(tx.rectTransform, 0f, 0f, 0f, 0f);
            tx.characterSpacing = 10f;
            rt.localScale = Vector3.one * 1.3f;
            var cg = rt.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0f; cg.blocksRaycasts = false;
            Tween.Run(0.22f, k => { if (rt == null) return; rt.localScale = Vector3.one * (1.3f - 0.3f * Tween.Apply(Ease.OutCubic, k)); cg.alpha = Mathf.Clamp01(k * 2f); }, Ease.Linear);
        }

        /// <summary>記録の1行: 小さな紙の札が右から滑り込む</summary>
        static void OutcomeLine(RectTransform layer, string text, float y)
        {
            var rt = UiKit.NewRect("outcome-line", layer);
            float w = 60f + text.Length * 20f;
            UiKit.Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-w / 2f, y - 14f), new Vector2(w / 2f, y + 14f));
            var bg = PaperFx.Sheet(rt, PaperFx.Tag, "paper", PaperFx.Paper2);
            UiKit.Stretch(bg.rectTransform, 0f, 0f, 0f, 0f); bg.raycastTarget = false;
            var tx = UiKit.Deco(rt, text, 18, PaperFx.Ink, TextAnchor.MiddleCenter);
            UiKit.Stretch(tx.rectTransform, 0f, 0f, 0f, 0f);
            var cg = rt.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0f; cg.blocksRaycasts = false;
            var p0 = rt.anchoredPosition;
            Tween.Run(0.2f, k => { if (rt == null) return; rt.anchoredPosition = p0 + new Vector2(50f * (1f - Tween.Apply(Ease.OutCubic, k)), 0f); cg.alpha = k; }, Ease.Linear);
            Audio.Key("CardsDrawn");
        }

        /// <summary>戦いの記録 (engine/summary.ts battleSummary の写し。表示層だけの集計)。延焼のティックも与ダメ、とげの反射も被ダメに数える</summary>
        static List<string> SummaryLines(IReadOnlyList<GameEvent> log)
        {
            var lines = new List<string>();
            if (log == null) return lines;
            int turns = 0, total = 0, best = 0, cur = 0, hpLost = 0, fired = 0, perfect = 0, negates = 0;
            foreach (var e in log)
            {
                if (e is GameEvent_TurnStarted ts) { turns = Math.Max(turns, ts.Turn); best = Math.Max(best, cur); cur = 0; }
                else if (e is GameEvent_DamageDealt dd)
                {
                    if (dd.Source == "player") { total += dd.Amount; cur += dd.Amount; }
                    else { hpLost += dd.HpLoss; if (dd.Amount > 0 && dd.HpLoss == 0) perfect++; }
                }
                else if (e is GameEvent_ThornsReflected tr) hpLost += tr.HpLoss;
                else if (e is GameEvent_ScaldTick sct) hpLost += sct.Amount;   // 烙印・火傷の疼きも被ダメ (2026-09-23)
                else if (e is GameEvent_BurnTick bt) { total += bt.Amount; cur += bt.Amount; }
                else if (e is GameEvent_ReactionTriggered) fired++;
                else if (e is GameEvent_ActionNegated) negates++;
            }
            best = Math.Max(best, cur);
            lines.Add(turns + "ターン ・ 与えたダメージ " + total + (best > 0 ? "（最大ターン " + best + "）" : ""));
            lines.Add("受けたダメージ " + hpLost);
            var extra = new List<string>();
            if (fired > 0) extra.Add("読み勝ち " + fired + "回");
            if (perfect > 0) extra.Add("完全に凌いだ " + perfect + "回");
            if (negates > 0) extra.Add("打ち消し " + negates + "回");
            if (extra.Count > 0) lines.Add(string.Join(" ・ ", extra.ToArray()));
            return lines;
        }

        /// <summary>差し替えで取り消された意図の札の幽霊 (絵＋一行) が、くるりと回りながら落ちて消える (2026-09-17 ④)</summary>
        static void IntentGhostDrop(RectTransform fx, Vector2 pos, EnemyIntent before)
        {
            if (fx == null || before == null) return;
            string line = CardText.IntentLine(before);
            int size = UiKit.Phone ? 20 : 24;
            float w = 40f + 36f + line.Length * size * 0.95f, h = size + 22f;
            var rt = UiKit.NewRect("intent-ghost", fx);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = pos;
            var paper = PaperFx.Sheet(rt, PaperFx.Tag, "paper", PaperFx.Paper2);
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f); paper.raycastTarget = false;
            var row = UiKit.NewRect("row", rt);
            UiKit.Stretch(row, 6f, 6f, 2f, 2f);
            var hg = UiKit.Horz(row, 6, 0);
            hg.childAlignment = TextAnchor.MiddleCenter; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            var art = Theme.Art("icons", "intent_" + before.Kind);
            var ic = UiKit.Icon(row, before.Kind == "defend" ? "shield" : "sword", 32f, Color.white);
            if (art != null) { ic.sprite = art; UiKit.PixelArt(ic); }   // p25
            ic.rectTransform.sizeDelta = new Vector2(32f, 32f); UiKit.Le(ic, 32f, 32f, 32f, 32f);
            var t = UiKit.Deco(row, line, size, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(t, 14f, h - 4f, -1f, h - 4f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            // 取り消し線
            var strike = UiKit.NewRect("strike", rt);
            UiKit.Anchor(strike, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(10f, -2f), new Vector2(-10f, 2f));
            var sImg = strike.gameObject.AddComponent<Image>(); sImg.color = PaperFx.BadInk; sImg.raycastTarget = false;
            strike.localScale = new Vector3(0f, 1f, 1f); strike.pivot = new Vector2(0f, 0.5f);
            var cg = rt.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false;
            Tween.Run(0.14f, k => { if (strike != null) strike.localScale = new Vector3(k, 1f, 1f); }, Ease.OutQuad, () =>
            {
                var rtC = rt; var cgC = cg;
                Tween.Run(0.6f, k =>
                {
                    if (rtC == null) return;
                    rtC.anchoredPosition = pos + new Vector2(70f * k, -30f * k - 240f * k * k);
                    rtC.localRotation = Quaternion.Euler(0f, 0f, -24f * k);
                    cgC.alpha = k < 0.4f ? 1f : 1f - (k - 0.4f) / 0.6f;
                }, Ease.Linear, () => { if (rtC != null) UnityEngine.Object.Destroy(rtC.gameObject); });
            });
        }

        static void ShowTrap(GameRoot g, RectTransform fx, GameEvent ev, ReactionCtx ctx)
        {
            var playerRt = g.Anchor("player");
            Vector2 slotPos = ctx.Slot != null ? Tween.CenterIn(ctx.Slot, fx) : (playerRt != null ? Tween.CenterIn(playerRt, fx) : Vector2.zero);
            // 判と一言の置き場: 枠が画面の上半分 (スマホの左上の帯) なら下に、下半分 (PC の自分の札) なら上に
            Vector2 stampPos = slotPos + new Vector2(0f, slotPos.y > 0f ? -74f : 74f);
            RectTransform enemyPan = ctx.EnemyIndex >= 0 ? g.Anchor("enemy" + ctx.EnemyIndex) : null;
            RectTransform intentTag = enemyPan != null ? enemyPan.Find("intent-tag") as RectTransform : null;
            switch (ev)
            {
                case GameEvent_ReactionTriggered rt:
                {
                    Audio.Key("ReactionTriggered");
                    CardDef def = null;
                    try { def = Content.GetCardDef(rt.CardId); } catch (Exception) { }
                    bool negate = false, guard = false, strike = false;
                    if (def != null)
                        foreach (var e in def.Effects)
                        {
                            if (e.Effect == "negate") negate = true;
                            else if (e.Effect == "gainBlock" || e.Effect == "gainIceBlock" || e.Effect == "gainHp") guard = true;
                            else if (e.Effect != null && (e.Effect == "counter" || e.Effect.StartsWith("dealDamage") || e.Effect == "applyBurn" || e.Effect == "exposeEnemy" || e.Effect == "weakenEnemy" || e.Effect == "staggerEnemy")) strike = true;
                        }
                    // 行き先: 返し・打ち消しは敵 (意図の札があればそこ)、守りは自分。どちらも無い札は自分
                    RectTransform dest = (negate || strike) ? (intentTag ?? enemyPan) : null;
                    if (dest == null) { var ps = g.Battle != null ? g.Battle.PlayerSprite() : null; dest = ps ?? playerRt; }
                    Vector2 to = dest != null ? Tween.CenterIn(dest, fx) + (dest == intentTag ? Vector2.zero : new Vector2(0f, 40f)) : slotPos + new Vector2(0f, 120f);
                    Tween.Stamp(fx, stampPos, "発動", PaperFx.BrassLight, PaperFx.Ink, PaperFx.Brass);
                    Tween.RingBurst(fx, slotPos, PaperFx.Mana, 140f, 0.4f);
                    var ghost = GhostToken(fx, slotPos, rt.CardId, def);
                    // 跳ねてから飛ぶ。着弾で青緑の星と輪、打ち消しなら×
                    Tween.Run(0.14f, k => { if (ghost != null) ghost.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(k * Mathf.PI)); }, Ease.Linear, () =>
                    {
                        if (ghost == null) return;
                        Tween.Move(ghost, to, 0.3f, Ease.InOutQuad, () =>
                        {
                            if (ghost == null) return;
                            Tween.RingBurst(fx, to, PaperFx.Mana, 150f, 0.35f);
                            Tween.IconBurst(fx, to, "set", new Color(PaperFx.Mana.r, PaperFx.Mana.g, PaperFx.Mana.b, 0.95f), 96f);
                            // 打ち消しの×は ActionNegated の側で押す (二重にしない)
                            var cg = ghost.GetComponent<CanvasGroup>() ?? ghost.gameObject.AddComponent<CanvasGroup>();
                            var gh = ghost;
                            Tween.Run(0.18f, k => { if (gh != null) { cg.alpha = 1f - k; gh.localScale = Vector3.one * (1f + 0.4f * k); } }, Ease.Linear, () => { if (gh != null) UnityEngine.Object.Destroy(gh.gameObject); });
                        });
                    });
                    break;
                }
                case GameEvent_ActionNegated _:
                {
                    Audio.Key("ActionNegated");
                    var at = intentTag ?? enemyPan;
                    if (at == null) return;
                    var pos = Tween.CenterIn(at, fx);
                    Tween.CrossMark(fx, pos, PaperFx.Brass, 64f);
                    Tween.Shake(at, 8f, 0.35f);
                    // 一言は札の下に (意図の札は画面の上の方にあるので上に出すと切れる)
                    Tween.Float(fx, pos + new Vector2(0f, -64f), "打ち消し", PaperFx.ManaLight, 26, 30f, 0.9f);
                    break;
                }
                case GameEvent_ReactionHeld _:
                    Audio.Key("ReactionHeld");
                    Tween.Stamp(fx, stampPos, "温存", PaperFx.Paper2, PaperFx.InkSoft, null, 20, 0.4f, -5f);
                    break;
                case GameEvent_SetCardExpired ex:
                {
                    Audio.Key("SetCardExpired");
                    CardDef def = null;
                    try { def = Content.GetCardDef(ex.CardId); } catch (Exception) { }
                    var ghost = GhostToken(fx, slotPos, ex.CardId, def);
                    var pile = g.Anchor(ex.To == "hand" ? "player" : "pile-discard");
                    Vector2 to = pile != null ? Tween.CenterIn(pile, fx) : slotPos + new Vector2(60f, -160f);
                    var cg = ghost.gameObject.AddComponent<CanvasGroup>();
                    var gh = ghost;
                    Tween.Float(fx, stampPos, ex.To == "hand" ? "手札へ戻る" : "期限切れ", PaperFx.PaperDim, 22, 30f, 0.9f);
                    Tween.Run(0.55f, k => { if (gh == null) return; gh.anchoredPosition = Vector2.Lerp(slotPos, to, Tween.Apply(Ease.InQuad, k)) + new Vector2(0f, 40f * Mathf.Sin(k * Mathf.PI)); gh.localRotation = Quaternion.Euler(0f, 0f, 28f * k); cg.alpha = 1f - k * k; }, Ease.Linear, () => { if (gh != null) UnityEngine.Object.Destroy(gh.gameObject); });
                    break;
                }
                case GameEvent_SetCardDestroyed sd:
                {
                    Audio.Key("SetCardDestroyed");
                    CardDef def = null;
                    try { def = Content.GetCardDef(sd.CardId); } catch (Exception) { }
                    var ghost = GhostToken(fx, slotPos, sd.CardId, def);
                    var cg = ghost.gameObject.AddComponent<CanvasGroup>();
                    var gh = ghost;
                    Stage.Shake(6f, 0.2f);
                    Tween.RingBurst(fx, slotPos, PaperFx.Rose, 160f, 0.4f);
                    Tween.Float(fx, stampPos, "壊された", PaperFx.Rose, 24, 36f, 0.9f);
                    Tween.Run(0.3f, k => { if (gh == null) return; gh.localScale = Vector3.one * (1f + 0.6f * k); gh.localRotation = Quaternion.Euler(0f, 0f, -18f * k); cg.alpha = 1f - k; }, Ease.OutQuad, () => { if (gh != null) UnityEngine.Object.Destroy(gh.gameObject); });
                    break;
                }
                case GameEvent_ReactionWhiffed _:
                    Tween.Float(fx, stampPos, "空振り", PaperFx.PaperDim, 20, 26f, 0.7f);
                    break;
                case GameEvent_GearUsed gu:
                {
                    // ギアを組んだ (2026-09-17 裁定4: からくりと同じ): 匣の蓋が開いて閃き、トークンが跳ねて対象へ飛び、着弾で青緑の輪と星。枠の側に「組んだ」の判
                    Audio.Key("GearUsed");
                    GearDef gdef = null;
                    try { gdef = Content.GetGearDef(gu.GearId); } catch (Exception) { }
                    // 的: 同じギアのトークン (回数が残っていれば持ち物にある) → 無ければ持ち物の欄 → 自分
                    RectTransform from = null;
                    if (g.Rs != null) foreach (var gi in DeckRogue.Engine.Run.GearsOf(g.Rs)) if (gi.GearId == gu.GearId) { from = g.Anchor("gear:" + gi.Uid); if (from != null) break; }
                    if (from == null) from = g.Anchor("gearzone");
                    var pSpr0 = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    if (from == null) from = pSpr0 ?? playerRt;
                    Vector2 fromPos = from != null ? Tween.CenterIn(from, fx) : Vector2.zero;
                    Vector2 gStamp = fromPos + new Vector2(0f, fromPos.y > 0f ? -74f : 74f);
                    // 行き先: 敵に働く効果 (対象・全体) は敵の意図の札、それ以外は自分
                    bool toEnemy = false;
                    if (gdef != null)
                        foreach (var e in gdef.Effects)
                        {
                            string ef = e.Effect ?? "";
                            if (ef == "negateEnemyAction" || ef == "blockEnemySummon" || ef == "blockEnemyInterrupt" || ef == "staggerEnemy" || ef == "stripRider" || ef == "singleHit" || ef == "confuse" || ef == "clearEnemyStrength" || ef == "shatterBlock" || ef == "exposeEnemy" || ef == "weakenEnemy" || ef.StartsWith("dealDamage")) toEnemy = true;
                        }
                    if (toEnemy && enemyPan == null)
                    {   // 対象が引けなかった全体効果など: 生きている最初の敵
                        var cur = g.Rs != null ? g.Rs.Combat : null;
                        if (cur != null) for (int k = 0; k < cur.Enemies.Count && enemyPan == null; k++) if (cur.Enemies[k].Hp > 0) { enemyPan = g.Anchor("enemy" + k); intentTag = enemyPan != null ? enemyPan.Find("intent-tag") as RectTransform : null; }
                    }
                    RectTransform dest = toEnemy ? (intentTag ?? enemyPan) : null;
                    if (dest == null) dest = pSpr0 ?? playerRt;
                    Vector2 to = dest != null ? Tween.CenterIn(dest, fx) + (dest == intentTag ? Vector2.zero : new Vector2(0f, 40f)) : fromPos + new Vector2(0f, 120f);
                    Tween.Stamp(fx, gStamp, "組んだ", PaperFx.BrassLight, PaperFx.Ink, PaperFx.Brass);
                    Tween.RingBurst(fx, fromPos, PaperFx.Mana, 140f, 0.4f);
                    // 舞台の匣: 蓋が開いて閃く。仕込み札が無ければ 0.6 秒後に閉じる (SyncField と同じ呼び方)
                    try
                    {
                        bool boxLeftRear = BattleView.BoxLeftRear;
                        Stage.SetKarakuriBox(1, true, boxLeftRear);
                        Tween.After(0.6f, () => { var cur2 = g.Rs != null ? g.Rs.Combat : null; if (cur2 != null) Stage.SetKarakuriBox(cur2.Player.SetCards.Count, false, boxLeftRear); });
                    }
                    catch (Exception) { }
                    var ghost = GearGhost(fx, fromPos, gu.GearId, gdef);
                    Tween.Run(0.14f, k => { if (ghost != null) ghost.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(k * Mathf.PI)); }, Ease.Linear, () =>
                    {
                        if (ghost == null) return;
                        Tween.Move(ghost, to, 0.3f, Ease.InOutQuad, () =>
                        {
                            if (ghost == null) return;
                            Tween.RingBurst(fx, to, PaperFx.Mana, 150f, 0.35f);
                            Tween.IconBurst(fx, to, "set", new Color(PaperFx.Mana.r, PaperFx.Mana.g, PaperFx.Mana.b, 0.95f), 96f);
                            var cg = ghost.GetComponent<CanvasGroup>() ?? ghost.gameObject.AddComponent<CanvasGroup>();
                            var gh = ghost;
                            Tween.Run(0.18f, k => { if (gh != null) { cg.alpha = 1f - k; gh.localScale = Vector3.one * (1f + 0.4f * k); } }, Ease.Linear, () => { if (gh != null) UnityEngine.Object.Destroy(gh.gameObject); });
                        });
                    });
                    break;
                }
                case GameEvent_DeathSaved ds:
                {
                    // 致死を耐えた (蘇りの発条＝ギア／蜥蜴の尾＝レリック): 薔薇と真鍮の輪、胸元に判、HP の一言
                    Audio.Key("DeathSaved");
                    var pSpr = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    var prt = playerRt;
                    if (pSpr == null && prt == null) return;
                    Vector2 pos = pSpr != null ? Tween.CenterIn(pSpr, fx) : Tween.CenterIn(prt, fx);
                    Stage.Flash("player");
                    Tween.RingBurst(fx, pos, PaperFx.Rose, 260f, 0.5f);
                    Tween.RingBurst(fx, pos, PaperFx.BrassLight, 170f, 0.35f);
                    Tween.ScreenFlash(fx, new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.14f), 0.3f);
                    Tween.Stamp(fx, pos + new Vector2(0f, 30f), ds.Source == "gear" ? "蘇りの発条がはじけた!" : "蜥蜴の尾が砕けた!", PaperFx.Paper2, PaperFx.BadInk, PaperFx.Rose, 22, 0.9f, -6f);
                    Tween.After(0.3f, () => Tween.Float(fx, pos + new Vector2(0f, 96f), "HP " + ds.Hp + " で踏みとどまった", PaperFx.BrassLight, 24, 30f, 1.2f));
                    break;
                }
                case GameEvent_RetainerRushed rr:
                {
                    // 駆けつけ (ひなたのパッシブ): 従者が出た瞬間にそのターン開始効果が1回解決する。効果の絵 (ダメージの数字・盾・回復) は
                    // 続く出来事が出すが、回復が満タンで空振る時などは何も見えないので、リーダーの胸元に判を押して「鳴った」を見せる (2026-09-19
                    // ユーザー「ひなたのパッシブが従者がでても誘発してない」= 誘発はしていたが判と log の行が無く、満タンの回復では見えなかった)
                    Audio.Key("RetainerRushed");
                    // 人形が舞台に立っていれば (2026-09-19 人形の盤面表示) その人形の頭上に判。無ければ従来どおりリーダーの胸元
                    var dollPan = g.Battle != null ? g.Battle.LastDollPanel(g.Rs != null ? g.Rs.Combat : null, rr.CardId) : null;
                    var dollSpr = dollPan != null ? dollPan.Find("sprite") as RectTransform : null;
                    if (dollSpr != null)
                    {
                        Vector2 dp = Tween.CenterIn(dollSpr, fx);
                        Tween.RingBurst(fx, dp + new Vector2(0f, -dollSpr.rect.height * 0.4f), PaperFx.BrassLight, 120f, 0.3f);
                        Tween.Stamp(fx, dp + new Vector2(0f, dollSpr.rect.height * 0.55f + 14f), "点灯", PaperFx.BrassLight, PaperFx.BrassInk, PaperFx.Brass, 17, 0.6f, -6f);
                        break;
                    }
                    var pSpr = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    var prt = playerRt;
                    if (pSpr == null && prt == null) return;
                    Vector2 pos = pSpr != null ? Tween.CenterIn(pSpr, fx) : Tween.CenterIn(prt, fx);
                    Tween.RingBurst(fx, pos, PaperFx.BrassLight, 150f, 0.3f);
                    Tween.Stamp(fx, pos + new Vector2(0f, 40f), "点灯 " + CardText.CardName(rr.CardId), PaperFx.BrassLight, PaperFx.BrassInk, PaperFx.Brass, 20, 0.6f, -6f);
                    break;
                }
                case GameEvent_TokenDestroyed td:
                {   // 灯が消える (敵の人形壊し。2026-09-19 人形の盤面表示): その人形が灰になって崩れる。uid が無い古いログは同じ札の最後の人形
                    Audio.Key("TokenDestroyed");
                    if (g.Battle == null) return;
                    string uid = td.Uid ?? g.Battle.DollUidByCard(td.CardId);
                    if (uid != null) g.Battle.KillDoll(g, uid, false);
                    break;
                }
                case GameEvent_RetainerSacrificed rs:
                {   // 灯の捧げ (自分で消す): 真鍮の「捧げた」
                    Audio.Key("TokenDestroyed");
                    if (g.Battle == null) return;
                    string uid = rs.Uid ?? g.Battle.DollUidByCard(rs.CardId);
                    if (uid != null) g.Battle.KillDoll(g, uid, true);
                    break;
                }
                case GameEvent_RetainerExpired re:
                {   // 期限切れ (2026-09-21 人形の寿命): 人形壊しと同じく頭から崩れる。判は灰の「期限切れ」(2026-09-22 語彙: 「灯が尽きた」は資源の灯と読まれた)
                    Audio.Key("RetainerExpired");
                    if (g.Battle == null) return;
                    g.Battle.KillDoll(g, re.Uid, false, "期限切れ");
                    break;
                }
                case GameEvent_RetainerCopied rc:
                {   // 写し灯・鏡の灯籠・二重の点灯 (2026-09-21): 元の人形から新しい座席へ光の玉が飛ぶ。登場の弾みは組み直しの EnterDoll が出す
                    Audio.Key("RetainerCopied");
                    if (g.Battle == null) return;
                    var fromSpr = g.Battle.DollSprite(rc.FromUid);
                    var toPan = g.Battle.DollPanel(rc.Uid);
                    var toSpr = toPan != null ? toPan.Find("sprite") as RectTransform : null;
                    Vector2 from = fromSpr != null ? Tween.CenterIn(fromSpr, fx) : (playerRt != null ? Tween.CenterIn(playerRt, fx) : Vector2.zero);
                    Vector2 to = toSpr != null ? Tween.CenterIn(toSpr, fx) : from + new Vector2(60f, 0f);
                    if (fromSpr != null) Tween.Punch(fromSpr, 0.1f, 0.25f, true);
                    Tween.Projectile(fx, from, to, PaperFx.BrassLight, 40f, 0.26f, 40f, () =>
                    {
                        Tween.RingBurst(fx, to, PaperFx.BrassLight, 110f, 0.3f);
                        Tween.Stamp(fx, to + new Vector2(0f, (toSpr != null ? toSpr.rect.height * 0.55f : 40f) + 14f), "写し", PaperFx.BrassLight, PaperFx.BrassInk, PaperFx.Brass, 16, 0.6f, -6f);
                    });
                    break;
                }
                case GameEvent_RetainerLifeExtended rl:
                {   // 継ぎ火 (+Nターン)・永遠の灯 (期限なし): その人形の頭上に真鍮の判
                    Audio.Key("RetainerLifeExtended");
                    if (g.Battle == null) return;
                    var dSpr = g.Battle.DollSprite(rl.Uid);
                    if (dSpr == null) return;
                    Vector2 dp = Tween.CenterIn(dSpr, fx);
                    Tween.Punch(dSpr, 0.12f, 0.3f, true);
                    Tween.RingBurst(fx, dp + new Vector2(0f, -dSpr.rect.height * 0.4f), new Color(1f, 0.85f, 0.55f, 0.9f), 130f, 0.35f);
                    Tween.Stamp(fx, dp + new Vector2(0f, dSpr.rect.height * 0.55f + 14f), rl.Persist == true ? "期限なし" : "+" + rl.Amount + "ターン", PaperFx.BrassLight, PaperFx.BrassInk, PaperFx.Brass, 17, 0.7f, -6f);
                    break;
                }
            }
        }

        /// <summary>ギアのトークンの幽霊 (62×62・レア度の縁・歯車の絵)。飛ばす素材</summary>
        static RectTransform GearGhost(RectTransform fx, Vector2 pos, string gearId, GearDef def)
        {
            var rt = UiKit.NewRect("ghost-gear", fx);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(GearUi.TokenW, GearUi.TokenH);
            rt.anchoredPosition = pos;
            var edge = PaperFx.Sheet(rt, PaperFx.Tag, "edge", PaperFx.Mana);
            UiKit.Stretch(edge.rectTransform, -3f, -3f, -3f, -3f); edge.raycastTarget = false;
            var paper = PaperFx.Sheet(rt, PaperFx.Tag2, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f); paper.raycastTarget = false;
            var pic = UiKit.NewRect("pic", rt);
            UiKit.Anchor(pic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-22f, -22f), new Vector2(22f, 22f));
            var pimg = pic.gameObject.AddComponent<Image>();
            pimg.sprite = ThemeFx.GearGlyph(gearId, def != null ? def.Family : "general"); pimg.preserveAspect = true; pimg.raycastTarget = false;
            return rt;
        }

        /// <summary>仕込み札の幽霊 (68×74 のトークンの写し): 紙の縁に挿絵。飛ばす・落とす・砕くの素材</summary>
        static RectTransform GhostToken(RectTransform fx, Vector2 pos, string cardId, CardDef def)
        {
            var rt = UiKit.NewRect("ghost-token", fx);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(BattleScreen.PhoneTokenW, BattleScreen.PhoneTokenH);
            rt.anchoredPosition = pos;
            var edge = PaperFx.Sheet(rt, PaperFx.Tag, "edge", PaperFx.Mana);
            UiKit.Stretch(edge.rectTransform, -3f, -3f, -3f, -3f); edge.raycastTarget = false;
            var paper = PaperFx.Sheet(rt, PaperFx.Tag, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f); paper.raycastTarget = false;
            var pic = UiKit.NewRect("pic", rt);
            UiKit.Anchor(pic, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(3f, -44f), new Vector2(-3f, -3f));
            var pimg = pic.gameObject.AddComponent<Image>();
            pimg.sprite = ThemeFx.CardArt(cardId, def != null ? Theme.CardTypeColor(def.Type) : PaperFx.Sand); pimg.preserveAspect = true; pimg.raycastTarget = false;
            UiKit.PixelArt(pimg);   // 飛んで膨らむ途中もドットの縁がちらつかない (2026-09-29 p25)
            var band = UiKit.Pan(rt, PaperFx.ManaInk, "band");
            UiKit.Anchor(band.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1f, 27f)); band.raycastTarget = false;
            var name = UiKit.Deco(rt, def != null ? def.Name : "", 13, PaperFx.Paper, TextAnchor.MiddleCenter);
            UiKit.Anchor(name.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(0f, 27f));
            name.textWrappingMode = TextWrappingModes.NoWrap; name.overflowMode = TextOverflowModes.Ellipsis;
            return rt;
        }

        /// <summary>画面中央の帯 (ターン開始・敵の番)。0.9 秒で消える</summary>
        /// <summary>手番の帯 (2026-09-30 F09: 旧は紙の上に苔 #7fa86c＝2.3:1・薔薇＝3.3:1 の文字だった＝塗りの色で文字を書いていた)。
        /// 上部バーの手番の札と同じ組: あなたの番＝紙に墨・敵の番＝夜の札に紙の文字。苔と薔薇は帯の下の短い線だけ (color-theme 規律3)</summary>
        static void Banner(RectTransform fx, string text, bool enemy)
        {
            var rt = UiKit.NewRect("banner", fx);
            UiKit.Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-360f, -40f), new Vector2(360f, 40f));
            rt.localRotation = Quaternion.Euler(0f, 0f, -1f);
            var bg = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.sprite = enemy ? PaperFx.NightTag : PaperFx.Panel; bg.type = UnityEngine.UI.Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 1f;
            bg.raycastTarget = false;
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.alpha = 0f;
            var accent = UiKit.Pan(rt, enemy ? PaperFx.Rose : PaperFx.Moss, "accent");
            UiKit.Anchor(accent.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-60f, 12f), new Vector2(60f, 16f));
            accent.raycastTarget = false;
            var t = UiKit.Deco(rt, text, 34, enemy ? PaperFx.Paper : PaperFx.Ink, TextAnchor.MiddleCenter);
            UiKit.Stretch(t.rectTransform, 0f, 4f, 0f, 0f);
            Tween.Run(0.9f, k => { if (cg != null) cg.alpha = k < 0.15f ? k / 0.15f : k > 0.7f ? 1f - (k - 0.7f) / 0.3f : 1f; }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        /// <summary>
        /// 表 (audio.json) で鳴らすイベントの鍵。Show が絵で特別扱いするイベント (ダメージ・ブロック・回復・ターン・状態異常) は
        /// そちらが鳴らすので null。それ以外は型名 "GameEvent_Xxx" → "Xxx" を鍵にして、表にあれば鳴らす
        /// </summary>
        static string TableSound(GameEvent ev)
        {
            if (ev == null) return null;
            if (ev is GameEvent_DamageDealt || ev is GameEvent_BlockGained || ev is GameEvent_IceBlockGained || ev is GameEvent_HpHealed || ev is GameEvent_TurnStarted || ev is GameEvent_TurnEnded || ev is GameEvent_CardPlayed || IsStatusEvent(ev) || IsTrapEvent(ev) || IsEnemyActEvent(ev)) return null;
            // 絵の側 (BattleView) が札の飛び・撃破の消えに合わせて鳴らすイベントは、ここでは二重に鳴らさない
            if (ev is GameEvent_CardSet || ev is GameEvent_CardsDrawn || ev is GameEvent_EnemyDied || ev is GameEvent_EnemyFled) return null;
            var n = ev.GetType().Name;
            if (n.StartsWith("GameEvent_")) n = n.Substring("GameEvent_".Length);
            return Audio.HasKey(n) ? n : null;
        }

        /// <summary>敵の行動の出来事 (2026-09-17 敵の行動の演出): 実行の予備動作・回復・盗み・山札喰い・突き刺し/延焼/再生の数字。絵と音を Show で</summary>
        static bool IsEnemyActEvent(GameEvent ev)
        {
            return ev is GameEvent_EnemyActionExecuting || ev is GameEvent_EnemyHealed || ev is GameEvent_GoldStolen || ev is GameEvent_CardsMilled || ev is GameEvent_ThornsReflected || ev is GameEvent_ScaldTick || ev is GameEvent_BurnTick || ev is GameEvent_RegenTicked || ev is GameEvent_BlockShattered || ev is GameEvent_EnemyStaggered || ev is GameEvent_EnemyInterrupted || ev is GameEvent_EnemyDied || ev is GameEvent_EnemyFled;
        }

        /// <summary>実行中の技の名前 (EnemyState.IntentMoveId)。コマンド前の盤面から読む (今の盤面は次の宣言に変わっている)</summary>
        static string MoveIdFor(ReactionCtx ctx, int enemyIndex)
        {
            var st = ctx != null ? ctx.Prev : null;
            if (st == null || enemyIndex < 0 || enemyIndex >= st.Enemies.Count) return null;
            return st.Enemies[enemyIndex].IntentMoveId;
        }

        /// <summary>技の名前 → 当たりの形 (Tween.HitFx)。牙・爪・突き・打撃・光線・飛び道具・斬撃</summary>
        static string HitStyle(string moveId)
        {
            if (string.IsNullOrEmpty(moveId)) return "slash";
            string m = moveId.ToLowerInvariant();
            string[] fang = { "bite", "chomp", "gnaw", "lick", "tongue", "mug", "devour", "maw" };
            string[] claw = { "claw", "rend", "slash", "talon", "wing", "blade", "cleave", "guillotine", "whip", "lash", "tail", "dance", "scythe", "sickle" };
            string[] beam = { "bolt", "beam", "surge", "spark", "shot", "bell", "mirror", "mimic", "chant", "light", "ray", "gaze", "curse_ray" };
            string[] thrw = { "spit", "mud", "slop", "toss", "boom", "peck", "acid", "spore", "ember", "junk", "rock", "throw" };
            string[] thrust = { "stab", "poke", "jab", "lunge", "thrust", "pierce", "spear", "horn", "needle", "sting" };
            string[] blunt = { "slam", "smash", "crush", "club", "hammer", "thump", "pummel", "bump", "tackle", "bash", "swing", "flurry", "leap", "dive", "hug", "smother", "rush", "weight", "chain", "one_two", "stance", "charge", "ram", "stomp", "press" };
            foreach (var k in fang) if (m.Contains(k)) return "fang";
            foreach (var k in thrust) if (m.Contains(k)) return "thrust";
            foreach (var k in claw) if (m.Contains(k)) return "claw";
            foreach (var k in beam) if (m.Contains(k)) return "beam";
            foreach (var k in thrw) if (m.Contains(k)) return "throw";
            foreach (var k in blunt) if (m.Contains(k)) return "blunt";
            return "slash";
        }

        /// <summary>飛び道具・光線の色 (技の名前から。酸/胞子=苔・泥/がらくた=砂・火=延焼の橙・呪い/闇=藤・それ以外=脈の青緑)</summary>
        static Color MissileColor(string moveId)
        {
            string m = (moveId ?? "").ToLowerInvariant();
            if (m.Contains("acid") || m.Contains("spore")) return PaperFx.Moss;
            if (m.Contains("mud") || m.Contains("slop") || m.Contains("junk") || m.Contains("rock")) return PaperFx.Sand;
            if (m.Contains("ember") || m.Contains("boom") || m.Contains("fire") || m.Contains("spark")) return PaperFx.Ember;
            if (m.Contains("curse") || m.Contains("dark") || m.Contains("hex") || m.Contains("shadow")) return PaperFx.Plum;
            return PaperFx.Mana;
        }

        /// <summary>からくり (仕込み札) の出来事: 発動・温存・期限切れ・壊し・空振り・打ち消し。絵と音を Show で (2026-09-17 リアクション発動の演出)</summary>
        static bool IsTrapEvent(GameEvent ev)
        {
            return ev is GameEvent_ReactionTriggered || ev is GameEvent_ReactionHeld || ev is GameEvent_SetCardExpired || ev is GameEvent_SetCardDestroyed || ev is GameEvent_ReactionWhiffed || ev is GameEvent_ActionNegated
                || ev is GameEvent_GearUsed || ev is GameEvent_DeathSaved   // ギア (2026-09-17 裁定4: からくりと同じ演出) と致死を耐えた判
                || ev is GameEvent_RetainerRushed   // 駆けつけの判 (2026-09-19)
                || ev is GameEvent_TokenDestroyed || ev is GameEvent_RetainerSacrificed   // 人形が崩れる (人形の盤面表示 2026-09-19)
                || ev is GameEvent_RetainerExpired || ev is GameEvent_RetainerCopied || ev is GameEvent_RetainerLifeExtended;   // 人形の灯り (2026-09-21): 尽きた・写した・継いだ
        }

        /// <summary>リアクションの演出に要る文脈: 札があった仕込み枠の的と、行動している敵。イベント自体は CardId しか持たないので、見えている盤面とログの前後から引く</summary>
        sealed class ReactionCtx { public RectTransform Slot; public int EnemyIndex = -1; public GameState Prev; public bool FinishingBlow; public bool AllEnemies; public bool ToDolls; public bool LightPayOnly; public DollBatch Batch; public bool BatchLast; public HitPlan Hit; }   // LightPayOnly＝灯を払っただけ (火床・炉心。2026-09-24 T15)   // Hit＝自分の札の当たりの形と多段の位置 (2026-09-22)   // AllEnemies/ToDolls＝灯の放出の飛び先 (2026-09-20 灯籠)。Batch＝人形の粒を束ねる (2026-09-21)

        /// <summary>
        /// 自分の札の当たりの計画 (2026-09-22 ユーザー「攻撃エフェクトがどの攻撃でも同じ」): 札 (CardPlayed / ReactionTriggered) の後に続く自分由来の DamageDealt
        /// (人形・置物の SourceUid つきは除く) に、形 (CardHitStyle) と多段の位置を付ける。Index＝何発目 (0 から)・Total＝発数。
        /// 全体攻撃は「同じ敵が2度目に出るまで」を1発 (Volley) と数え、Volley の中は 0.05 秒刻みで一斉に走らせて揺れは最後の1つだけ
        /// </summary>
        sealed class HitPlan { public CardDef Def; public string Style = "slash"; public int Index; public int Total; public bool Volley; public bool VolleyFirst; public bool VolleyLast; }
        static void PlanCardHits(IReadOnlyList<GameEvent> log, int from, Dictionary<int, HitPlan> at)
        {
            CardDef cur = null;
            var run = new List<int>();
            void Flush()
            {
                if (cur == null || run.Count == 0) { run.Clear(); return; }
                string style = CardHitStyle(cur);
                var volleys = new List<List<int>>(); var seen = new HashSet<int>(); List<int> v = null;
                foreach (var idx in run)
                {
                    int ei = (log[idx] as GameEvent_DamageDealt).EnemyIndex ?? 0;
                    if (v == null || seen.Contains(ei)) { v = new List<int>(); volleys.Add(v); seen.Clear(); }
                    v.Add(idx); seen.Add(ei);
                }
                for (int k = 0; k < volleys.Count; k++)
                    for (int m = 0; m < volleys[k].Count; m++)
                        at[volleys[k][m]] = new HitPlan { Def = cur, Style = style, Index = k, Total = volleys.Count, Volley = volleys[k].Count > 1, VolleyFirst = m == 0, VolleyLast = m == volleys[k].Count - 1 };
                run.Clear();
            }
            for (int i = Math.Max(0, from); i < log.Count; i++)
            {
                var e = log[i];
                if (e is GameEvent_CardPlayed cp) { Flush(); cur = null; try { cur = Content.GetCardDef(cp.CardId); } catch (Exception) { } }
                else if (e is GameEvent_ReactionTriggered rt) { Flush(); cur = null; try { cur = Content.GetCardDef(rt.CardId); } catch (Exception) { } }
                else if (e is GameEvent_TurnEnded || e is GameEvent_TurnStarted || e is GameEvent_EnemyActionExecuting || e is GameEvent_GearUsed) { Flush(); cur = null; }
                else if (e is GameEvent_DamageDealt dd && dd.Source == "player" && dd.SourceUid == null && cur != null) run.Add(i);
            }
            Flush();
        }

        /// <summary>札 → 当たりの形 (Tween.PlayerHitFx)。id と名前のキーワード＋タイプ (ユーザー裁定 2026-09-22: データは触らず推定。外れは表を直す)。
        /// 火種/火花/火の粉=spark ／ 呪文=spell (白は light) ／ 牙・呑=fang ／ 角・突き・楔・槍=horn ／ 蔦・蔓・鞭=vine ／ 踏・突進・突撃・槌・砕き・据え・疾駆=stomp ／ それ以外=slash</summary>
        public static string CardHitStyle(CardDef def)
        {
            if (def == null) return "slash";
            string id = (def.Id ?? "").ToLowerInvariant(), name = def.Name ?? "";
            bool white = def.Color == "white";
            bool Id(params string[] ks) { foreach (var k in ks) if (id.Contains(k)) return true; return false; }
            bool Nm(params string[] ks) { foreach (var k in ks) if (name.Contains(k)) return true; return false; }
            if (Id("spark", "ember") || Nm("火種", "火花", "火の粉")) return "spark";
            if (def.Type == "spell" || Id("powder_pod")) return white ? "light" : "spell";
            if (Id("fang", "gulp") || Nm("牙", "呑")) return "fang";
            if (Id("thrust", "wedge", "spear", "needle", "sting") || (!white && Id("horn")) || Nm("角", "突き", "楔", "槍")) return "horn";
            if (Id("lash", "vine", "whip", "tendril") || Nm("蔦", "蔓", "鞭")) return "vine";
            if (Id("stomp", "stampede", "charge", "rush", "trample", "maul", "bodyslam", "slam", "bash", "tackle", "sprint") || Nm("踏", "突進", "突撃", "槌", "砕き", "据え", "疾駆")) return "stomp";
            return "slash";
        }

        /// <summary>形ごとの筋の色: 呪文=脈の青緑・灯=暖色・火種=延焼の橙・白の物理=真鍮を帯びた紙色・それ以外=紙色 (急所・頭打ちは呼び側で上書き)</summary>
        static Color HitColor(string style, CardDef def)
        {
            switch (style)
            {
                case "spell": return new Color(PaperFx.Mana.r, PaperFx.Mana.g, PaperFx.Mana.b, 0.95f);
                case "light": return ThemeFx.LampGlow;
                case "spark": return new Color(PaperFx.Ember.r, PaperFx.Ember.g, PaperFx.Ember.b, 0.95f);
            }
            if (def != null && def.Color == "white") return new Color(1f, 0.94f, 0.76f, 0.95f);
            return ThemeFx.SlashCore;
        }

        /// <summary>
        /// 人形の粒を束ねる (2026-09-21 【E】人間ラン#15「号令1回＝10〜16行の1〜4ダメの粒が 0.12 秒刻み」): 連続する人形由来 (SourceUid つき) の
        /// DamageDealt / BlockGained / HpHealed を1つの浮き数字「人形×N: 合計」にする。各人形の踏み込みは並行に鳴らし、HP・ブロックの Nudge は合計で1回
        /// </summary>
        sealed class DollBatch
        {
            public readonly Dictionary<int, int> Dmg = new Dictionary<int, int>();
            public readonly Dictionary<int, int> Cnt = new Dictionary<int, int>();
            public readonly Dictionary<int, int> HpLoss = new Dictionary<int, int>();
            public readonly Dictionary<int, int> Blocked = new Dictionary<int, int>();
            public int Block, BlockN, Heal, HealN;
            public bool SoundHit, SoundBlock, SoundHeal;
        }

        /// <summary>人形由来の出来事 (束ねる対象)</summary>
        static bool IsDollEvent(GameEvent ev)
        {
            if (ev is GameEvent_DamageDealt dd) return dd.Source == "player" && dd.SourceUid != null;
            if (ev is GameEvent_BlockGained b) return b.Target == "player" && b.SourceUid != null;
            if (ev is GameEvent_HpHealed h) return h.SourceUid != null;
            return false;
        }

        /// <summary>Play / PlaySequenced が演出に変える出来事か (束ねの区切りの判定に使う。間に挟まる見せない出来事は区切らない)</summary>
        static bool IsShownEvent(GameEvent ev)
        {
            return ev is GameEvent_CardPlayed || ev is GameEvent_DamageDealt || ev is GameEvent_BlockGained || ev is GameEvent_IceBlockGained || ev is GameEvent_HpHealed
                || ev is GameEvent_TurnStarted || ev is GameEvent_TurnEnded || IsStatusEvent(ev) || IsTrapEvent(ev) || IsEnemyActEvent(ev) || TableSound(ev) != null;
        }

        /// <summary>from 以降の出来事を走査し、2つ以上続く人形の粒の並びごとに DollBatch を割り当てる (最後の添字を last に)</summary>
        static void PlanDollBatches(IReadOnlyList<GameEvent> log, int from, Dictionary<int, DollBatch> batchAt, HashSet<int> last)
        {
            int i = Math.Max(0, from);
            while (i < log.Count)
            {
                if (!IsDollEvent(log[i])) { i++; continue; }
                var members = new List<int> { i };
                int j = i + 1;
                for (; j < log.Count; j++)
                {
                    if (IsDollEvent(log[j])) { members.Add(j); continue; }
                    if (IsShownEvent(log[j])) break;   // 見せる出来事が挟まれば区切る (見せない出来事は跨ぐ)
                }
                if (members.Count >= 2)
                {
                    var b = new DollBatch();
                    foreach (var m in members) batchAt[m] = b;
                    last.Add(members[members.Count - 1]);
                }
                i = j;
            }
        }

        /// <summary>束ねた粒の最後: 敵ごとの「人形×N: 合計」、自分の「人形×N: ブロック+合計／回復+合計」を1回ずつ出し、Nudge も合計で</summary>
        static void FlushDollBatch(GameRoot g, RectTransform fx, DollBatch b, bool nudge)
        {
            foreach (var kv in b.Dmg)
            {
                int ei = kv.Key;
                var rt = g.Anchor("enemy" + ei);
                if (rt == null) continue;
                int sum = kv.Value, cnt = b.Cnt.ContainsKey(ei) ? b.Cnt[ei] : 1;
                int hpLoss = b.HpLoss.ContainsKey(ei) ? b.HpLoss[ei] : 0, blocked = b.Blocked.ContainsKey(ei) ? b.Blocked[ei] : 0;
                var pos = EnemyFloatPos(g, fx, ei) + new Vector2(0f, 20f);
                Color numColor = sum <= 0 ? UiKit.ColDim : (hpLoss <= 0 && blocked > 0) ? PaperFx.SkyLight : PaperFx.BrassLight;
                Tween.Float(fx, pos, (cnt > 1 ? "人形×" + cnt + ": " : "") + sum, numColor, sum >= 15 ? 40 : 32, 60f, 1.0f);
                if (blocked > 0) Tween.After(0.06f, () => Tween.Float(fx, pos + new Vector2(0f, -34f), "ブロックで −" + blocked, PaperFx.SkyLight, 20, 34f, 1.0f));
                if (sum >= 15) Stage.Shake(Mathf.Min(10f, sum * 0.3f), 0.2f);
                if (nudge && g.Battle != null && hpLoss > 0) g.Battle.NudgeEnemyHp(ei, -hpLoss);
                if (nudge && g.Battle != null && blocked > 0) g.Battle.NudgeEnemyBlock(ei, -blocked);
            }
            var prt = g.Anchor("player");
            if (prt != null)
            {
                if (b.BlockN > 0)
                {
                    Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(80f, 10f), (b.BlockN > 1 ? "人形×" + b.BlockN + ": " : "") + "ブロック+" + b.Block, UiKit.ColBlock, 26, 40f, 0.9f);
                    if (nudge && g.Battle != null) g.Battle.NudgePlayerBlock(b.Block);
                }
                if (b.HealN > 0)
                    Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(-80f, 10f + (b.BlockN > 0 ? 30f : 0f)), (b.HealN > 1 ? "人形×" + b.HealN + ": " : "") + "回復+" + b.Heal, UiKit.ColAccent, 26, 40f, 0.9f);
            }
        }

        /// <summary>とどめの一撃 (2026-09-17 ⑫): 新しい出来事の中で、最後に敵を倒したプレイヤーの打撃。戦闘が決着 (全滅・逃走) した時だけ。無ければ -1</summary>
        static int FinishingBlowIndex(IReadOnlyList<GameEvent> log, int from, GameState combat)
        {
            if (combat == null) return -1;
            for (int i = 0; i < combat.Enemies.Count; i++) if (combat.Enemies[i].Hp > 0) return -1;
            for (int i = log.Count - 1; i >= from; i--) if (log[i] is GameEvent_DamageDealt dd && dd.Source == "player" && dd.HpLoss > 0) return i;
            return -1;
        }
        static ReactionCtx ReactionContextFor(GameRoot g, IReadOnlyList<GameEvent> log, int i, GameState visible)
        {
            var ev = log[i];
            string cardId = (ev as GameEvent_ReactionTriggered)?.CardId ?? (ev as GameEvent_SetCardExpired)?.CardId ?? (ev as GameEvent_SetCardDestroyed)?.CardId ?? (ev as GameEvent_ReactionWhiffed)?.CardId;
            var ctx = new ReactionCtx { Prev = visible };
            if (ev is GameEvent_GearUsed)
            {   // ギア: 対象の敵はイベントに無いので、直後の敵側の出来事 (急所・威圧・打撃・体勢崩し・粉砕) から引く。無ければ -1 (自分へ飛ぶ)
                for (int k = i + 1; k < log.Count && k <= i + 10 && ctx.EnemyIndex < 0; k++)
                {
                    var n = log[k];
                    if (n is GameEvent_DamageDealt dd && dd.Source == "player") ctx.EnemyIndex = dd.EnemyIndex ?? -1;
                    else if (n is GameEvent_ExposedApplied ea) ctx.EnemyIndex = ea.EnemyIndex;
                    else if (n is GameEvent_EnemyWeakened ew) ctx.EnemyIndex = ew.EnemyIndex;
                    else if (n is GameEvent_EnemyStaggered es) ctx.EnemyIndex = es.EnemyIndex;
                    else if (n is GameEvent_BlockShattered bs) ctx.EnemyIndex = bs.EnemyIndex;
                    else if (n is GameEvent_GearUsed || n is GameEvent_CardPlayed || n is GameEvent_TurnEnded) break;
                }
                return ctx;
            }
            if (ev is GameEvent_LightDischarged)
            {   // 灯の放出 (2026-09-20 灯籠): 対象はイベントに無いので直後の打撃から引く。2体以上に当たれば全体、人形の仕事 (sourceUid つき) が先に来れば大行列＝人形へ
                // 灯を払っただけ (灯の火床・灯の炉心) は放出ではない = 飛び先を探さない (2026-09-24 T15: 直後の敵フェーズの返しや人形の打撃を拾って光の筋が飛んでいた)
                if (LightPayOnly(log, i)) { ctx.LightPayOnly = true; return ctx; }
                var seen = new HashSet<int>();
                for (int k = i + 1; k < log.Count && k <= i + 24; k++)
                {
                    var n = log[k];
                    if (n is GameEvent_DamageDealt dd && dd.Source == "player")
                    {
                        if (dd.SourceUid != null && seen.Count == 0) { ctx.ToDolls = true; break; }
                        int ei = dd.EnemyIndex ?? -1;
                        if (ei >= 0) { if (ctx.EnemyIndex < 0) ctx.EnemyIndex = ei; seen.Add(ei); }
                    }
                    else if (n is GameEvent_EnemyWeakened ew) { ctx.AllEnemies = true; if (ctx.EnemyIndex < 0) ctx.EnemyIndex = ew.EnemyIndex; }
                    else if ((n is GameEvent_BlockGained || n is GameEvent_HpHealed) && seen.Count == 0) { ctx.ToDolls = true; break; }
                    else if (n is GameEvent_CardPlayed || n is GameEvent_TurnEnded || n is GameEvent_LightDischarged) break;
                }
                if (seen.Count >= 2) ctx.AllEnemies = true;
                return ctx;
            }
            if (ev is GameEvent_BlockGained bg0 && bg0.Target != "player")
            {   // 敵の防御: イベントに敵の番号が無いので、直前に実行した敵
                for (int k = i - 1; k >= 0 && k >= i - 12 && ctx.EnemyIndex < 0; k--) { if (log[k] is GameEvent_EnemyActionExecuting ex) ctx.EnemyIndex = ex.EnemyIndex; else if (log[k] is GameEvent_TurnEnded) break; }
                return ctx;
            }
            if (cardId == null && !(ev is GameEvent_ReactionHeld) && !(ev is GameEvent_ActionNegated)) return ctx;   // 敵の行動の演出は Prev (実行中の技の名前) だけ使う
            // 枠: 見えている盤面 (順送りなら古い盤面) の仕込み札から。無ければ今の盤面の最初の空き枠 (札が抜けた跡)
            var cur = g.Rs != null ? g.Rs.Combat : null;
            if (cardId != null)
            {
                foreach (var st in new[] { visible, cur })
                {
                    if (st == null || ctx.Slot != null) continue;
                    for (int k = 0; k < st.Player.SetCards.Count; k++) if (st.Player.SetCards[k].Def.Id == cardId) { ctx.Slot = g.Anchor("setslot" + k); if (ctx.Slot != null) break; }
                }
                if (ctx.Slot == null && cur != null) ctx.Slot = g.Anchor("setslot" + Math.Min(cur.Player.SetCards.Count, Math.Max(0, cur.Player.SetSlots - 1)));
            }
            // 行動している敵: 前の窓は直後の EnemyActionExecuting、後の窓は直前の EnemyActionExecuting
            if (ev is GameEvent_ActionNegated an) ctx.EnemyIndex = an.EnemyIndex;
            else if (ev is GameEvent_ReactionHeld rh) ctx.EnemyIndex = rh.EnemyIndex;
            else
            {
                // 窓の向き: 前の窓 (onAttackIncoming・onEnemyAction) は行動の実行が後に、後の窓 (onAttacked・onEnemyBuffed・onEnemyDefended) は前に来る
                bool post = false;
                if (cardId != null)
                {
                    try { var d = Content.GetCardDef(cardId); foreach (var e in d.Effects) if (e.Trigger == "onAttacked" || e.Trigger == "onEnemyBuffed" || e.Trigger == "onEnemyDefended") post = true; } catch (Exception) { }
                }
                if (post) { for (int k = i - 1; k >= 0 && k >= i - 12 && ctx.EnemyIndex < 0; k--) { if (log[k] is GameEvent_EnemyActionExecuting ex) ctx.EnemyIndex = ex.EnemyIndex; else if (log[k] is GameEvent_TurnEnded) break; } }
                else { for (int k = i + 1; k < log.Count && k <= i + 12 && ctx.EnemyIndex < 0; k++) { if (log[k] is GameEvent_EnemyActionExecuting ex) ctx.EnemyIndex = ex.EnemyIndex; else if (log[k] is GameEvent_ActionNegated an2) ctx.EnemyIndex = an2.EnemyIndex; else if (log[k] is GameEvent_ReactionTriggered || log[k] is GameEvent_TurnStarted) break; } }   // 打ち消された行動は実行されない (ActionNegated が先に来る)
                if (ctx.EnemyIndex < 0) for (int k = i - 1; k >= 0 && k >= i - 12 && ctx.EnemyIndex < 0; k--) { if (log[k] is GameEvent_EnemyActionExecuting ex) ctx.EnemyIndex = ex.EnemyIndex; else if (log[k] is GameEvent_TurnEnded) break; }
                if (ctx.EnemyIndex < 0) for (int k = i + 1; k < log.Count && k <= i + 8; k++) if (log[k] is GameEvent_DamageDealt dd && dd.Source == "player") { ctx.EnemyIndex = dd.EnemyIndex ?? -1; break; }
            }
            return ctx;
        }

        /// <summary>
        /// 灯を「払っただけ」の LightDischarged か (2026-09-24 Opus ひなた T15): 灯の火床 (Sparks つき) と灯の炉心 (consumeLight＝Paid) は放出ではない。
        /// engine の印 (Sparks・Paid) を読む。印の無い古いログ (再開したセーブ) は「払った後にも灯が残る」＝部分払いで見分ける
        /// (全て放出する札＝大行列・大放出は灯を 0 にする)
        /// </summary>
        static bool LightPayOnly(IReadOnlyList<GameEvent> log, int i)
        {
            var ld = i >= 0 && i < log.Count ? log[i] as GameEvent_LightDischarged : null;
            if (ld == null) return false;
            if (CardText.IsLightPayment(ld)) return true;
            // 払った後の灯 = 戦闘の始め (灯0) からの出来事の和 (灯の増減は全て出来事になる)。灯が残るなら部分払い
            int light = 0;
            for (int k = 0; k <= i; k++)
            {
                if (log[k] is GameEvent_LightGained lg) light += lg.Amount;
                else if (log[k] is GameEvent_LightSpent ls) light -= ls.Amount;
                else if (log[k] is GameEvent_LightDischarged d) light -= d.Spent;
            }
            return light > 0;
        }

        static bool IsStatusEvent(GameEvent ev)
        {
            return ev is GameEvent_StatusInflicted || ev is GameEvent_ExposedApplied || ev is GameEvent_EnemyWeakened || ev is GameEvent_BurnApplied || ev is GameEvent_StrengthGained || ev is GameEvent_GrowthAdded || ev is GameEvent_MomentumAdded
                || ev is GameEvent_LightGained || ev is GameEvent_LightDischarged || ev is GameEvent_LightSpent // 灯 (白 2026-09-20)
                || ev is GameEvent_ArtifactBlocked || ev is GameEvent_PlayerArtifactBlocked; // アーティファクトが弾いた (2026-09-20 ユーザー「威圧がそのターンの攻撃に反映されない」= 黙って弾かれていた)
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

        static void Show(GameRoot g, RectTransform fx, GameEvent ev, bool nudgeHp, ReactionCtx ctx = null)
        {
            var key = TableSound(ev);
            if (key != null) { Audio.Key(key); return; }
            if (IsTrapEvent(ev)) { ShowTrap(g, fx, ev, ctx ?? new ReactionCtx()); return; }
            if (IsEnemyActEvent(ev)) { ShowEnemyAct(g, fx, ev, ctx ?? new ReactionCtx(), nudgeHp); return; }
            switch (ev)
            {
                case GameEvent_DamageDealt d:
                {
                    // source=player → 敵 (EnemyIndex=対象) が受けた／source=enemy → 自分が受けた (EnemyIndex=攻撃者)
                    if (d.Source == "player")
                    {
                        int ei = d.EnemyIndex ?? 0;
                        var rt = g.Anchor("enemy" + ei);
                        if (rt == null) return;
                        if (ctx != null && ctx.Batch != null)
                        {   // 束ねた人形の粒 (2026-09-21): 踏み込みと閃きだけ出して合計に足し、最後の1つで「人形×N: 合計」
                            var bt = ctx.Batch;
                            var bSpr = g.Battle != null ? g.Battle.EnemySprite(ei) : null;
                            var bDollSpr = g.Battle != null ? g.Battle.DollSprite(d.SourceUid) : null;
                            if (bDollSpr != null && bSpr != null)
                            {
                                Vector2 dp0 = Tween.CenterIn(bDollSpr, fx), hp0 = Tween.CenterIn(bSpr, fx);
                                Tween.Lunge(bDollSpr, (hp0 - dp0).normalized * (UiKit.Phone ? 22f : 36f) + new Vector2(0f, 4f), 0.22f);
                            }
                            Stage.Flash("enemy" + ei, 0.08f);
                            if (!bt.SoundHit) { bt.SoundHit = true; Audio.Key("DamageDealt.player"); }
                            bt.Dmg[ei] = (bt.Dmg.ContainsKey(ei) ? bt.Dmg[ei] : 0) + d.Amount;
                            bt.Cnt[ei] = (bt.Cnt.ContainsKey(ei) ? bt.Cnt[ei] : 0) + 1;
                            bt.HpLoss[ei] = (bt.HpLoss.ContainsKey(ei) ? bt.HpLoss[ei] : 0) + Math.Max(0, d.HpLoss);
                            bt.Blocked[ei] = (bt.Blocked.ContainsKey(ei) ? bt.Blocked[ei] : 0) + (d.Blocked ?? 0);
                            if (ctx.BatchLast) FlushDollBatch(g, fx, bt, nudgeHp);
                            return;
                        }
                        var pos = EnemyFloatPos(g, fx, ei) + new Vector2(UnityEngine.Random.Range(-30f, 30f), 20f);
                        // ⑥ ダメージの質 (2026-09-17): 急所・貫通・盾が吸った・装甲/ターン装甲/殻/無形の頭打ち を数字の脇で見分ける (イベントの値＝実処理と同じ)
                        bool crit = d.Exposed == true, pierced = d.Pierced == true;
                        int blocked = d.Blocked ?? 0, armorCut = d.ArmorCut ?? 0, turnCut = d.TurnArmorCut ?? 0, burrowCut = d.BurrowCut ?? 0, nemesisCut = d.NemesisCut ?? 0, slipCut = d.SlipperyCut ?? 0;
                        bool shell = ctx != null && ctx.Prev != null && ei < ctx.Prev.Enemies.Count && ctx.Prev.Enemies[ei].BurrowActive == true;
                        bool capped = armorCut > 0 || turnCut > 0 || nemesisCut > 0 || slipCut > 0;
                        bool big = d.Amount >= 15 || crit;
                        // 斬撃の筋と白い点滅、大きいほど画面も揺れる。急所は真鍮の筋＋星、盾に全部吸われた時は鋼青の輪 (金属の当たり)、頭打ちは鈍い輪
                        var spr = g.Battle != null ? g.Battle.EnemySprite(ei) : null;
                        Vector2 hit = spr != null ? Tween.CenterIn(spr, fx) : Tween.CenterIn(rt, fx);
                        // 人形の誘発 (2026-09-19 人形の盤面表示): その人形が敵へ半歩踏み込む = 誰の仕事かが絵で読める
                        var dollSpr = d.SourceUid != null && g.Battle != null ? g.Battle.DollSprite(d.SourceUid) : null;
                        if (dollSpr != null)
                        {
                            Vector2 dp = Tween.CenterIn(dollSpr, fx);
                            var dir = (hit - dp).normalized * (UiKit.Phone ? 22f : 36f);
                            Tween.Lunge(dollSpr, dir + new Vector2(0f, 4f), 0.22f);
                        }
                        var hp = ctx != null ? ctx.Hit : null;
                        string style = hp != null ? hp.Style : "slash";
                        Color streak = crit ? new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 1f) : capped ? new Color(0.78f, 0.8f, 0.86f, 0.9f) : HitColor(style, hp != null ? hp.Def : null);   // 頭打ちは鈍い筋 (刃が通らない)
                        bool volleyTail = hp != null && hp.Volley && !hp.VolleyLast;   // 全体攻撃の途中の1体: 揺れ・寄り・音は最後の1体だけ (2026-09-22)
                        // 敵のブロックが全部吸った (殻は別) = 敵が盾で受け止める (2026-09-17): 斬撃の筋は出さず、敵の正面に空色の盾の面。白い点滅も無し
                        bool guardedE = blocked > 0 && d.HpLoss <= 0 && !shell;
                        if (guardedE) Tween.GuardFx(fx, hit, "slash", -1f);
                        else if (spr != null)
                        {
                            // 札ごとの当たりの形 (2026-09-22): 斬撃・牙・角・蔦・踏みつけ・呪文・灯・火種。多段は向きを交互に、最後の1発は大きく
                            var pSprH = g.Battle != null ? g.Battle.PlayerSprite() : null;
                            Vector2 fromP = pSprH != null ? Tween.CenterIn(pSprH, fx) : hit + new Vector2(-400f, 0f);
                            Tween.PlayerHitFx(fx, hit, style, streak, big, hp != null ? hp.Index : 0, hp != null ? hp.Total : 1, fromP);
                            Stage.Flash("enemy" + ei);
                        }
                        // 急所: 筋と衝撃線が真鍮色 (big 扱い = 交差する2本目と針10) になり、真鍮の輪が広がる (旧: 星の絵 = 2026-09-17 ユーザー「星型がダサい」で撤去)
                        if (crit && !guardedE) Tween.RingBurst(fx, hit, PaperFx.BrassLight, 200f, 0.35f);
                        if (blocked > 0 && !shell && d.HpLoss > 0) Tween.IconBurst(fx, hit + new Vector2(-10f, 10f), "shield", new Color(PaperFx.Sky.r, PaperFx.Sky.g, PaperFx.Sky.b, 0.7f), 90f);
                        if (capped || burrowCut > 0) Tween.RingBurst(fx, hit, new Color(PaperFx.InkSoft.r, PaperFx.InkSoft.g, PaperFx.InkSoft.b, 0.8f), 150f, 0.3f);
                        if (hp == null || !hp.Volley || hp.VolleyFirst) Audio.Key("DamageDealt.player.swing");
                        if (blocked > 0 && d.HpLoss <= 0) Audio.Key("DamageDealt.blocked");
                        else if (!volleyTail) Audio.Key(big ? "DamageDealt.player.big" : "DamageDealt.player");
                        if (!volleyTail && !guardedE && (big || style == "stomp")) Stage.Shake(Mathf.Min(14f, Mathf.Max(6f, d.Amount * 0.4f)) * (crit ? 1.2f : 1f) * (style == "stomp" ? 1.3f : 1f), 0.25f);   // 踏みつけは小さくても揺れる
                        // ⑫ カメラ (2026-09-17): 大技は舞台がぐっと寄る。とどめ (戦闘を決めた一撃) はヒットストップ＝時間が一瞬凍って、大きく寄る
                        bool finishing = ctx != null && ctx.FinishingBlow;
                        if (finishing) { Tween.HitStop(0.12f, 0.3f); Stage.ZoomPunch(1.1f, 0.6f); Stage.Shake(12f, 0.35f); Tween.RingBurst(fx, hit, new Color(1f, 1f, 0.95f, 0.9f), 260f, 0.5f); }
                        else if (big && !guardedE && !volleyTail) Stage.ZoomPunch(crit ? 0.5f : 0.35f, 0.3f);
                        // 数字: 通った量は真鍮の紙、盾に全部吸われたら鋼青、0 は薄く。急所は大きく
                        Color numColor = d.Amount <= 0 ? UiKit.ColDim : (d.HpLoss <= 0 && blocked > 0) ? PaperFx.SkyLight : PaperFx.BrassLight;
                        Tween.Float(fx, pos, d.Amount.ToString(), numColor, crit ? 50 : (d.Amount >= 20 ? 46 : 36));
                        // 盾の数字 (帳面の左端の盾) が減ったのを見せる。盾の札が無ければ (吸い切って消えた・殻) 数字の脇の一言で
                        var bshield = blocked > 0 && !shell ? rt.Find("strip/block") as RectTransform : null;
                        if (bshield != null) { Tween.Punch(bshield, 0.25f); Tween.Float(fx, Tween.CenterIn(bshield, fx) + new Vector2(0f, 10f), "−" + blocked, PaperFx.SkyLight, 22, 30f, 0.8f); }
                        if (pierced)
                        {   // 貫通: 盾の札が揺れて、その上を貫通の印が抜ける (盾は減らない)
                            var disc = rt.Find("strip/block") as RectTransform;
                            if (disc != null) { Tween.Shake(disc, 5f, 0.3f); Tween.IconBurst(fx, Tween.CenterIn(disc, fx) + new Vector2(0f, 14f), "pierce", new Color(PaperFx.Paper.r, PaperFx.Paper.g, PaperFx.Paper.b, 0.95f), 46f); }
                        }
                        // 脇の一言 (質): 数字の下に小さく、複数なら段を重ねる
                        var notes = new List<KeyValuePair<string, Color>>();
                        var srcName = PermanentName(g, ctx, d.SourceUid);   // 人形・置物の仕事は出所の名前を添える (2026-09-22 友人ラン「人形・ひなた・置物の効果が混在」)
                        if (srcName != null) notes.Add(new KeyValuePair<string, Color>(srcName, PaperFx.Paper));
                        if (crit) notes.Add(new KeyValuePair<string, Color>("急所!", PaperFx.BrassLight));
                        if (pierced) notes.Add(new KeyValuePair<string, Color>("貫通", PaperFx.Paper));
                        if (blocked > 0 && bshield == null) notes.Add(new KeyValuePair<string, Color>((shell ? "殻で −" : "ブロックで −") + blocked, PaperFx.SkyLight));
                        if (armorCut > 0) notes.Add(new KeyValuePair<string, Color>("装甲で −" + armorCut, PaperFx.Paper2));
                        if (turnCut > 0) notes.Add(new KeyValuePair<string, Color>("ターン装甲で −" + turnCut, PaperFx.Paper2));
                        if (burrowCut > 0) notes.Add(new KeyValuePair<string, Color>("殻がこぼした −" + burrowCut, PaperFx.Paper2));
                        if (nemesisCut > 0) notes.Add(new KeyValuePair<string, Color>("無形で −" + nemesisCut, PaperFx.Paper2));
                        if (slipCut > 0) notes.Add(new KeyValuePair<string, Color>("朧で −" + slipCut, PaperFx.Paper2));
                        for (int n = 0; n < notes.Count; n++)
                        {
                            var note = notes[n]; float dy = -34f - 26f * n; float dl = 0.06f * (n + 1);
                            Tween.After(dl, () => Tween.Float(fx, pos + new Vector2(0f, dy), note.Key, note.Value, note.Key.EndsWith("!") ? 26 : 20, 34f, 1.0f));
                        }
                        if (d.Amount > 0 && !guardedE) Tween.Punch(rt, Mathf.Min(0.12f, 0.03f + d.Amount * 0.004f) * (crit ? 1.4f : 1f), keepBottom: true);   // 入れ物は中心が軸: 足元を留めて膨らむ (2026-09-18 沈む不具合)
                        if (nudgeHp && g.Battle != null && d.HpLoss > 0) g.Battle.NudgeEnemyHp(ei, -d.HpLoss);
                        if (nudgeHp && g.Battle != null && blocked > 0) g.Battle.NudgeEnemyBlock(ei, -blocked);   // 帳面の盾の数字もその場で減る (殻も同じ器。2026-09-17)
                    }
                    else
                    {
                        // 敵の攻撃 (2026-09-17 種類別の当たり): 技の名前から 牙/爪/打撃/光線/飛び道具/斬撃 を選ぶ。
                        // 近接は踏み込みと同時に当たる。光線と飛び道具は敵から自分へ飛んで 0.22 秒後に当たる (被弾の反応もその時)
                        var atkSpr = g.Battle != null ? g.Battle.EnemySprite(d.EnemyIndex ?? -1) : null;
                        var rt = g.Anchor("player");
                        if (rt == null) return;
                        var pSpr = g.Battle != null ? g.Battle.PlayerSprite() : null;
                        string moveId = MoveIdFor(ctx, d.EnemyIndex ?? -1);
                        string style = HitStyle(moveId);
                        bool ranged = style == "beam" || style == "throw";
                        Vector2 hitPos = pSpr != null ? Tween.CenterIn(pSpr, fx) : Tween.CenterIn(rt, fx);
                        Color hitColor = ranged ? MissileColor(moveId) : new Color(1f, 0.62f, 0.5f, 0.95f);
                        float hitDelay = 0f;
                        if (atkSpr != null)
                        {
                            if (!ranged) Tween.Lunge(atkSpr, new Vector2(-90f, 12f));
                            else
                            {
                                Tween.Lunge(atkSpr, new Vector2(18f, 0f), 0.2f);   // 反動 (少し後ろへ)
                                var from = Tween.CenterIn(atkSpr, fx) + new Vector2(-30f, 10f);
                                hitDelay = 0.22f;
                                if (style == "beam") Tween.BeamFx(fx, from, hitPos, hitColor, 0.3f, d.Amount >= 12 ? 1.4f : 1f);
                                else Tween.Projectile(fx, from, hitPos, hitColor, d.Amount >= 12 ? 56f : 44f, hitDelay, 70f, null);
                            }
                        }
                        Audio.Key("DamageDealt.enemy.swing");
                        var dd = d; var rtC = rt; var pSprC = pSpr; bool nudge = nudgeHp;
                        Tween.After(hitDelay, () =>
                        {
                            // 完全に防いだ (2026-09-17 ユーザー「完全に防いだ時に敵からダメージ食らってるように見える」): 被弾の筋 (朱) の代わりに盾で受ける演出 (GuardFx)
                            bool guarded = dd.HpLoss <= 0 && dd.Amount > 0;
                            if (guarded) Tween.GuardFx(fx, hitPos, style);
                            else Tween.HitFx(fx, hitPos, style, hitColor, dd.HpLoss >= 12);
                            // 完全に防いだ時は被弾音でなく防御音 (2026-09-14 ユーザー指摘)。ブロックで受けた盾の音 + 構えの絵
                            if (dd.HpLoss <= 0 && dd.Amount > 0) { Audio.Key("DamageDealt.blocked"); Stage.PlayAnim("player", "block"); }
                            else Audio.Key(dd.HpLoss >= 12 ? "DamageDealt.enemy.big" : "DamageDealt.enemy", dd.HpLoss > 0 ? 1f : 0.5f);
                            if (dd.HpLoss > 0)
                            {
                                Stage.PlayAnim("player", "hurt");
                                if (pSprC != null) Tween.Lunge(pSprC, new Vector2(-36f, 0f));   // のけぞり (後ろへ小さく)
                                Stage.Shake(Mathf.Min(18f, 4f + dd.HpLoss * 0.7f) * (style == "blunt" ? 1.3f : 1f), 0.3f);
                                Stage.Flash("player");
                                Tween.ScreenFlash(fx, new Color(0.9f, 0.1f, 0.1f, Mathf.Min(0.35f, 0.1f + dd.HpLoss * 0.015f)));
                            }
                            var pos = Tween.CenterIn(rtC, fx) + new Vector2(UnityEngine.Random.Range(-40f, 40f), 10f);
                            // 数字は失った HP (2026-09-17 ⑥)。完全に防いだら「防いだ」、盾が吸った量は脇に鋼青で (旧: ブロック前の量を朱で＝表示の嘘)
                            int blockedP = dd.Blocked ?? 0;
                            if (dd.HpLoss > 0) Tween.Float(fx, pos, "-" + dd.HpLoss, UiKit.ColBad, dd.HpLoss >= 15 ? 46 : 36);
                            else if (dd.Amount > 0) Tween.Float(fx, pos, "防いだ", PaperFx.SkyLight, 30);
                            else Tween.Float(fx, pos, "0", UiKit.ColDim, 30);
                            if (blockedP > 0)
                            {
                                if (!guarded) Tween.IconBurst(fx, hitPos + new Vector2(-16f, 0f), "shield", new Color(PaperFx.Sky.r, PaperFx.Sky.g, PaperFx.Sky.b, dd.HpLoss > 0 ? 0.7f : 0.95f), dd.HpLoss > 0 ? 100f : 140f);
                                Tween.After(0.08f, () => Tween.Float(fx, pos + new Vector2(0f, -36f), "ブロックで −" + blockedP, PaperFx.SkyLight, 22, 34f, 1.0f));
                            }
                            if (dd.Amount > 0 && !guarded) Tween.Punch(rtC, Mathf.Min(0.1f, 0.03f + dd.Amount * 0.004f), keepBottom: true);
                            if (nudge && g.Battle != null) g.Battle.LandPlayerIncoming(dd.Amount);   // 見込みから届いた攻撃を引く (先に。この後の Nudge が残りで引き直す。p08)
                            if (nudge && g.Battle != null && dd.HpLoss > 0) g.Battle.NudgePlayerHp(-dd.HpLoss);
                            if (nudge && g.Battle != null && blockedP > 0) g.Battle.AbsorbPlayerBlock(blockedP);   // 自分の札の盾の数字も吸われた分だけ減る (通常→氷壁の順。2026-09-17)
                        });
                    }
                    break;
                }
                case GameEvent_BlockGained b:
                {
                    if (b.Target != "player")
                    {   // 敵の防御 (2026-09-17): 盾の絵が浮かんで「+N」。帳面の盾は組み直しで出る (敵の番号は直前に実行した敵 = ctx)
                        int ei = ctx != null ? ctx.EnemyIndex : -1;
                        var ert = ei >= 0 ? g.Anchor("enemy" + ei) : null;
                        var espr = g.Battle != null && ei >= 0 ? g.Battle.EnemySprite(ei) : null;
                        if (ert == null) return;
                        Audio.Key("BlockGained");
                        var at = espr != null ? Tween.CenterIn(espr, fx) : Tween.CenterIn(ert, fx);
                        Tween.IconBurst(fx, at + new Vector2(0f, 10f), "shield", new Color(PaperFx.Sky.r, PaperFx.Sky.g, PaperFx.Sky.b, 0.9f), 110f);
                        Tween.Float(fx, at + new Vector2(60f, 30f), "+" + b.Amount, PaperFx.SkyLight, 28, 36f, 0.7f);
                        if (nudgeHp && g.Battle != null) g.Battle.NudgeEnemyBlock(ei, b.Amount);   // 順送りの途中は帳面の盾をその場で出す (2026-09-17)
                        return;
                    }
                    var rt = g.Anchor("player");
                    if (rt == null) return;
                    if (ctx != null && ctx.Batch != null)
                    {   // 束ねた人形の粒 (2026-09-21): 盾の人形が跳ねて小さな盾。数字は最後にまとめて
                        var bt = ctx.Batch;
                        var bd = b.SourceUid != null && g.Battle != null ? g.Battle.DollSprite(b.SourceUid) : null;
                        if (bd != null) { Tween.Punch(bd, 0.12f, 0.3f, true); Tween.IconBurst(fx, Tween.CenterIn(bd, fx) + new Vector2(0f, 10f), "shield", new Color(0.55f, 0.75f, 1f, 0.9f), 60f); }
                        if (!bt.SoundBlock) { bt.SoundBlock = true; Audio.Key("BlockGained"); }
                        bt.Block += b.Amount; bt.BlockN++;
                        if (ctx.BatchLast) FlushDollBatch(g, fx, bt, nudgeHp);
                        return;
                    }
                    Audio.Key("BlockGained");
                    var ps = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    if (ps != null) Tween.IconBurst(fx, Tween.CenterIn(ps, fx) + new Vector2(0f, 20f), "shield", new Color(0.55f, 0.75f, 1f, 0.9f), 110f);
                    Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(80f, 10f), "+" + b.Amount, UiKit.ColBlock, 30, 40f, 0.7f);
                    var bName = PermanentName(g, ctx, b.SourceUid);   // 出所の名前 (2026-09-22)
                    if (bName != null) Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(80f, -18f), bName, PaperFx.Paper, 18, 34f, 0.8f);
                    // 人形の誘発 (2026-09-19): 盾の人形が小さく跳ねて盾の絵を出す = 守りの出どころ
                    var bDoll = b.SourceUid != null && g.Battle != null ? g.Battle.DollSprite(b.SourceUid) : null;
                    if (bDoll != null) { Tween.Punch(bDoll, 0.12f, 0.3f, true); Tween.IconBurst(fx, Tween.CenterIn(bDoll, fx) + new Vector2(0f, 10f), "shield", new Color(0.55f, 0.75f, 1f, 0.9f), 60f); }
                    // 置物・レリック・仕込み札で敵の番に得たブロックは、自分の札の盾の数字にその場で足す (2026-09-17 ユーザー「置物の誘発とかで得たブロックがキャラの表記に更新されなくない？」)
                    if (nudgeHp && g.Battle != null) g.Battle.NudgePlayerBlock(b.Amount);
                    break;
                }
                case GameEvent_IceBlockGained ib:
                {
                    var rt = g.Anchor("player");
                    if (rt == null) return;
                    Audio.Key("BlockGained");
                    var ps = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    if (ps != null) Tween.IconBurst(fx, Tween.CenterIn(ps, fx) + new Vector2(0f, 20f), "shield", new Color(PaperFx.SkyLight.r, PaperFx.SkyLight.g, PaperFx.SkyLight.b, 0.9f), 110f);
                    Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(80f, 10f), "氷壁 +" + ib.Amount, PaperFx.SkyLight, 28, 40f, 0.7f);
                    if (nudgeHp && g.Battle != null) g.Battle.NudgePlayerIce(ib.Amount);
                    break;
                }
                case GameEvent_TurnStarted ts:
                    Audio.Key("TurnStarted");
                    Banner(fx, "ターン " + ts.Turn + "  —  あなたの番", false);
                    // 上部バーの手番の札も順送りの間に「あなたの番」へ (2026-09-29。即時の時は組み直し済み＝触らない)
                    if (nudgeHp) BattleScreen.SetPhase(g, 0, -1, 0);
                    // 自ターンの始まりで通常ブロックは消える (留め具 blockKeep なら N まで残る)。順送りの途中の盾の数字もここで揃える。この後の置物の分は BlockGained が足す
                    if (nudgeHp && g.Battle != null)
                    {
                        var cur = g.Rs != null ? g.Rs.Combat : null;
                        int keep = cur != null && cur.BlockKeep.HasValue ? Math.Min(g.Battle.ShownPlayerBlock, cur.BlockKeep.Value) : 0;
                        g.Battle.SetPlayerBlock(keep);
                    }
                    // 仕込み札が生きた瞬間 (準備ターン明け) をからくりの上に浮かせる (2026-09-14 ユーザー「伏せが有効になることを GUI で分かりやすく」)。
                    // 語は Unity のからくりの語 (2026-09-29 p15: 旧「罠が鳴る準備完了」)。同じターンに2枚以上生きても一言は1つ (生きた札の枠の真ん中)＝
                    // 枠ごとに出すと 78px 間隔で重なっていた。スマホの枠は画面の左端・上部バーのすぐ下なので、文字が画面の外へ切れないよう左右を詰め、
                    // 上がりきった時も上部バーに掛からない高さまで下げる (スマホは枠の上に重なる。PC は枠の上 90 のまま)
                    {
                        var st = g.Rs != null ? g.Rs.Combat : null;
                        if (st != null)
                        {
                            Vector2 sum = Vector2.zero; int live = 0;
                            float lowest = float.MaxValue, leftmost = float.MaxValue;
                            var liveSlots = new List<RectTransform>();
                            for (int si = 0; si < st.Player.SetCards.Count; si++)
                                if (Effects.TrapAge(st, st.Player.SetCards[si]) == 1)
                                {
                                    var slot = g.Anchor("setslot" + si);
                                    if (slot != null)
                                    {
                                        var cpos = Tween.CenterIn(slot, fx);
                                        sum += cpos; live++; liveSlots.Add(slot);
                                        lowest = Mathf.Min(lowest, cpos.y - slot.rect.height / 2f);
                                        leftmost = Mathf.Min(leftmost, cpos.x - slot.rect.width / 2f);
                                    }
                                }
                            if (live > 0)
                            {
                                // 帯を今の盤面で描き直してから浮かせる (順送りの間、帯は古い盤面の「準備中」のままで、浮き文字の「準備完了」と食い違った。F55)。
                                // 生きた枠は弾んで青緑の輪＝帯が「あとN回」に変わる瞬間が合図
                                if (nudgeHp) BattleScreen.RedrawSetTokens(g, st);
                                foreach (var ls in liveSlots)
                                {
                                    if (ls == null) continue;
                                    Tween.Punch(ls, 0.12f, 0.3f);
                                    Tween.RingBurst(fx, Tween.CenterIn(ls, fx), PaperFx.ManaLight, 110f, 0.4f);
                                }
                                var fr = fx.rect;
                                if (UiKit.Phone)
                                {   // スマホはからくりの列の下 (右はギア・置物の帯、上は見出しなので避ける)。短い一言・上がらずに薄れる (F55: 旧は挿絵と見出しに重なった)
                                    const float pw = 150f;
                                    var ppos = new Vector2(leftmost + pw / 2f, lowest - 22f);
                                    ppos.x = Mathf.Max(ppos.x, fr.xMin + pw / 2f + 8f);
                                    Tween.Float(fx, ppos, "準備完了", PaperFx.ManaLight, 24, 6f, 1.2f, pw);
                                }
                                else
                                {
                                    const float fw = 300f;   // 24px で11字 (264) が1行に収まる幅
                                    var pos = sum / live + new Vector2(0f, 90f);
                                    pos.x = Mathf.Clamp(pos.x, fr.xMin + fw / 2f + 8f, Mathf.Max(fr.xMin + fw / 2f + 8f, fr.xMax - fw / 2f - 8f));
                                    pos.y = Mathf.Min(pos.y, fr.yMax - BattleScreen.TopH - 4f - 40f - 16f);   // 40 = 上がる量・16 = 24px の文字の半分
                                    Tween.Float(fx, pos, "からくりが鳴る準備完了", PaperFx.ManaLight, 24, 40f, 1.2f, fw);
                                }
                            }
                        }
                    }
                    break;
                case GameEvent_TurnEnded _:
                    Audio.Key("TurnEnded");
                    Banner(fx, "敵の番", true);
                    // 上部バーの手番の札を夜の札「敵の番」に (2026-09-29: 旧は敵が行動している間ずっと「あなたの番」のままだった)
                    if (nudgeHp) BattleScreen.SetPhase(g, 1, -1, PhaseEnemyCount(g, ctx, -1));
                    // 敵フェーズの始まりで敵のブロックは失効 (潜伏の殻は残る)。帳面の盾もここで消す
                    if (nudgeHp && g.Battle != null) g.Battle.ResetEnemyBlocks(ctx != null ? ctx.Prev : null);
                    break;
                case GameEvent_CardPlayed cp:
                {
                    // 攻撃札なら斧を振る (本家式: その場で踏み込んで振る)。守りの札なら構え
                    CardDef def = null;
                    try { def = Content.GetCardDef(cp.CardId); } catch (Exception) { }
                    bool atk = false, blk = false;
                    if (def != null)
                    {
                        foreach (var e in def.Effects) { if (e.Trigger == null || e.Trigger == "onPlay") { if (e.Effect == "dealDamage" || e.Effect == "dealDamageRandom" || e.Effect == "dealDamageCleave") atk = true; if (e.Effect == "gainBlock" || e.Effect == "gainIceBlock") blk = true; } }
                        if (def.Modes != null) foreach (var m in def.Modes) foreach (var e in m.Effects) { if (e.Effect == "dealDamage") atk = true; if (e.Effect == "gainBlock") blk = true; }
                    }
                    // 実際の結果が分かる時 (選択式・条件付き) はそちらを優先する
                    if (_playHint == 1) { atk = true; blk = false; }
                    else if (_playHint == 2) { atk = false; blk = true; }
                    _playHint = 0;
                    // ⑦ 手札 (2026-09-17): エナジーの輪が払った瞬間に跳ね、X の札は払った玉が輪から札の行き先へ飛んで「X = N」の判
                    try { ShowEnergyPaid(g, fx, def, ctx); } catch (Exception e) { Debug.LogWarning("[Presenter] energy " + e.Message); }
                    bool spell = def != null && def.Type == "spell";
                    if (atk && spell)
                    {   // 呪文は斧を振らない (2026-09-22 ユーザー裁定): 体の前で色の光がひと膨らみ＝詠唱。当たりは PlayerHitFx の spell/light
                        var cSpr = g.Battle != null ? g.Battle.PlayerSprite() : null;
                        if (cSpr != null) Tween.CastFx(fx, Tween.CenterIn(cSpr, fx), HitColor(CardHitStyle(def), def));
                    }
                    else if (atk)
                    {
                        Stage.PlayAnim("player", "attack");
                        var pSpr = g.Battle != null ? g.Battle.PlayerSprite() : null;
                        // 振り抜きの瞬間 (0.07s): 踏み込み + 体が少し沈む + 画面が揺れる = 斧の重さ
                        Tween.After(0.11f, () => { if (pSpr != null) Tween.Lunge(pSpr, new Vector2(64f, -6f)); Stage.Shake(7f, 0.18f); });
                    }
                    else if (blk) Stage.PlayAnim("player", "block");
                    break;
                }
                case GameEvent_StatusInflicted si:
                {
                    // 自分に状態異常: 紫の浮き文字で「いつ掛かったか」を見せる (2026-09-09「いつデバフをかけられたかも分からない」)
                    var rt = g.Anchor("player");
                    if (rt == null) return;
                    Audio.Key(ev is GameEvent_GrowthAdded || ev is GameEvent_MomentumAdded ? "GrowthAdded" : "StatusInflicted");
                    var ps2 = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    if (ps2 != null) Tween.IconBurst(fx, Tween.CenterIn(ps2, fx) + new Vector2(0f, 30f), "exposed", new Color(0.72f, 0.5f, 0.85f, 0.9f), 110f);
                    Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(0f, 70f), StatusJa(si.Status) + " +" + si.Amount, PaperFx.Plum, 32, 46f, 1.2f);
                    break;
                }
                case GameEvent_ExposedApplied ea:
                {
                    var rt = g.Anchor("enemy" + ea.EnemyIndex);
                    if (rt == null) return;
                    Tween.Float(fx, EnemyFloatPos(g, fx, ea.EnemyIndex) + new Vector2(0f, 60f), "急所 +" + ea.Amount, PaperFx.Brass, 28, 40f, 1.0f);
                    break;
                }
                case GameEvent_EnemyWeakened ew:
                {
                    var rt = g.Anchor("enemy" + ew.EnemyIndex);
                    if (rt == null) return;
                    Tween.Float(fx, EnemyFloatPos(g, fx, ew.EnemyIndex) + new Vector2(0f, 60f), "威圧 +" + ew.Amount, PaperFx.Sky, 28, 40f, 1.0f);
                    break;
                }
                case GameEvent_ArtifactBlocked ab:
                {
                    // アーティファクトがデバフ (威圧・急所・混乱) を弾いた: 黙って消えると「効いていない不具合」に見える (2026-09-20 ユーザー報告)。
                    // 敵の上に藤の文字と輪、帳面のアーティファクトの札が跳ねる
                    var rt = g.Anchor("enemy" + ab.EnemyIndex);
                    if (rt == null) return;
                    var c = EnemyFloatPos(g, fx, ab.EnemyIndex) + new Vector2(0f, 60f);
                    Tween.RingBurst(fx, c, PaperFx.Plum, 120f, 0.35f);
                    // 判 (紙の帯) にする: 浮き文字 (幅 240) では折り返してダメージの数字と重なる。着弾の数字 (+60) より上に、少し遅らせて
                    string what = "アーティファクトが" + CardText.DebuffName(ab.Effect) + "を弾いた";
                    // 意図の札 (頭上) と着弾の数字 (胸) を避けて、胴の下 (帳面の上) に
                    Tween.After(0.15f, () => Tween.Stamp(fx, c + new Vector2(0f, -100f), what, PaperFx.PlumLight, PaperFx.PlumInk, PaperFx.Plum, 18, 1.0f, -5f));
                    Audio.Play("block", 0.6f);
                    break;
                }
                case GameEvent_PlayerArtifactBlocked pab:
                {
                    var rt = g.Anchor("player");
                    if (rt == null) return;
                    var c = PlayerFloatPos(g, fx) + new Vector2(0f, 70f);
                    Tween.RingBurst(fx, c, PaperFx.Plum, 120f, 0.35f);
                    string what = "時計仕掛けの土産が" + StatusJa(pab.Status) + "を弾いた";
                    Tween.After(0.15f, () => Tween.Stamp(fx, c + new Vector2(0f, 60f), what, PaperFx.PlumLight, PaperFx.PlumInk, PaperFx.Plum, 18, 1.0f, -5f));
                    Audio.Play("block", 0.6f);
                    break;
                }
                case GameEvent_BurnApplied ba:
                {
                    var rt = g.Anchor("enemy" + ba.EnemyIndex);
                    if (rt == null) return;
                    Tween.Float(fx, EnemyFloatPos(g, fx, ba.EnemyIndex) + new Vector2(0f, 60f), "延焼 +" + ba.Amount, PaperFx.Ember, 28, 40f, 1.0f);
                    break;
                }
                case GameEvent_StrengthGained sg:
                {
                    var rt = g.Anchor("enemy" + sg.EnemyIndex);
                    if (rt == null || sg.Amount == 0) return;
                    Tween.Float(fx, EnemyFloatPos(g, fx, sg.EnemyIndex) + new Vector2(0f, 60f), "筋力 " + (sg.Amount > 0 ? "+" : "") + sg.Amount, sg.Amount > 0 ? PaperFx.Brass : PaperFx.Sky, 28, 40f, 1.0f);
                    break;
                }
                case GameEvent_GrowthAdded ga:
                {
                    var rt = g.Anchor("player");
                    if (rt == null || ga.Amount <= 0) return;
                    Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(-60f, 60f), "成長 +" + ga.Amount, PaperFx.Moss, 26, 36f, 0.9f);
                    break;
                }
                case GameEvent_MomentumAdded ma:
                {
                    var rt = g.Anchor("player");
                    if (rt == null || ma.Amount <= 0) return;
                    Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(60f, 60f), "勢い +" + ma.Amount, PaperFx.Honey, 26, 36f, 0.9f);
                    break;
                }
                case GameEvent_LightGained lg:
                {
                    // 灯 (2026-09-20 灯の表示・案B): 真鍮の粒が出どころ (回復＝ひなたの胸・人形＝その人形・灯匠＝ひなたの竿の灯籠) から灯籠へ飛び込み、
                    // 着いた瞬間に炎がひと膨らみして数字が増え、上に「灯 +N」(真鍮)。器が無い (白以外で初めて灯が付く前) なら自分の上に浮き文字だけ
                    if (lg.Amount <= 0) return;
                    // 出どころの名前を添える (2026-09-22 友人ラン「人形・ひなた・置物の効果が混在」): 「ひなた 灯 +1」「灯芯の人形 灯 +1」「回復 灯 +1」
                    string who = LightSourceName(g, ctx, lg);
                    string lightLabel = (who != null ? who + " " : "") + "灯 +" + lg.Amount;
                    int lightSize = who != null ? 20 : 24;
                    if (!LightUi.Exists)
                    {
                        var rt0 = g.Anchor("player");
                        if (rt0 != null) Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(0f, 70f), lightLabel, PaperFx.Brass, lightSize, 34f, 0.8f);
                        return;
                    }
                    int amt = lg.Amount;
                    Vector2 to = LightUi.GlassCenter(fx);
                    if (lg.Source == "carry")
                    {   // 残り火: 放出の後に半分が灯籠の中で戻る＝飛ばさずその場で灯る
                        LightUi.HoldFor(1.0f);
                        Tween.After(0.45f, () => { LightUi.Add(amt, true); Audio.Key("LightGained"); Tween.Float(fx, LightUi.TopCenter(fx) + new Vector2(0f, 26f), "残り火 +" + amt, PaperFx.Brass, 22, 30f, 0.8f); });
                        return;
                    }
                    Vector2 from = LightSourcePos(g, fx, lg, ctx);
                    bool arrived = false;
                    Action onArrive = () =>
                    {
                        if (arrived) return; arrived = true;
                        LightUi.Add(amt, true);
                        Audio.Key("LightGained");
                        Tween.Float(fx, LightUi.TopCenter(fx) + new Vector2(0f, 26f), lightLabel, PaperFx.Brass, lightSize, 34f, 0.8f);
                    };
                    int n = Math.Min(amt, 6);
                    LightUi.HoldFor(0.6f);
                    for (int i = 0; i < n; i++)
                    {
                        int ii = i;
                        Tween.After(0.05f * i, () => Tween.Projectile(fx, from, to, PaperFx.BrassLight, 24f, 0.3f, 36f + 16f * (ii % 3), ii == 0 ? onArrive : null));
                    }
                    break;
                }
                case GameEvent_LightSpent lsp:
                {
                    // 灯コストの支払い (号令・灯コストの人形): 払った数だけ真鍮の粒が灯籠から舞台の人形へ飛び (灯で人形を動かす)、炎が一段小さくなり「灯 −N」(中墨)
                    if (lsp.Amount <= 0) return;
                    if (!LightUi.Exists)
                    {
                        var rt0 = g.Anchor("player");
                        if (rt0 != null) Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(0f, 70f), "灯 -" + lsp.Amount, PaperFx.InkSoft, 24, 34f, 0.8f);
                        return;
                    }
                    int amt = lsp.Amount;
                    Vector2 from = LightUi.GlassCenter(fx);
                    var targets = DollTargets(g, fx, ctx);
                    if (targets.Count == 0) { var ps = g.Battle != null ? g.Battle.PlayerSprite() : null; targets.Add(ps != null ? Tween.CenterIn(ps, fx) : from + new Vector2(0f, 160f)); }
                    Audio.Key("LightSpent");
                    LightUi.Add(-amt, true);
                    Tween.Float(fx, LightUi.TopCenter(fx) + new Vector2(0f, 26f), "灯 -" + amt, PaperFx.InkSoft, 24, 34f, 0.8f);
                    for (int i = 0; i < Math.Min(amt, 8); i++)
                    {
                        Vector2 to = targets[i % targets.Count];
                        Tween.After(0.05f * i, () => Tween.Projectile(fx, from, to, PaperFx.Brass, 22f, 0.28f, 50f, () => Tween.RingBurst(fx, to, PaperFx.BrassLight, 70f, 0.22f)));
                    }
                    break;
                }
                case GameEvent_LightDischarged ld:
                {
                    // 放出: 炎が硝子から抜けて光の筋になり、対象 (単体は狙った敵・全体は全員・号令の大行列は人形) へ飛ぶ。灯籠は暗くなり数字が 0 へ減る。判「放出 N」
                    if (ld.Spent <= 0) return;
                    if (CardText.IsLightPayment(ld) || (ctx != null && ctx.LightPayOnly))
                    {   // 払っただけ (灯の火床・灯の炉心 2026-09-24 T15): 光の筋は出さず、灯籠の炎が縮んで数字が減るだけ。火床は火種の粒が山札へ落ちる
                        string payText = CardText.LightPayLine(ld);   // 「灯9を払って火種3を山札へ」「灯3を払った」(ログと同じ文)
                        if (!LightUi.Exists)
                        {
                            var rt0 = g.Anchor("player");
                            if (rt0 != null) Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(0f, 70f), payText, PaperFx.Brass, 22, 34f, 0.9f);
                            return;
                        }
                        Audio.Key("LightSpent");
                        LightUi.HoldFor(0.6f);
                        LightUi.Add(-ld.Spent, true);
                        Tween.Float(fx, LightUi.TopCenter(fx) + new Vector2(0f, 26f), payText, PaperFx.InkSoft, 22, 34f, 0.9f);
                        var pile = (ld.Sparks ?? 0) > 0 ? g.Anchor("pile-draw") : null;
                        if (pile != null)
                        {
                            Vector2 pFrom = LightUi.GlassCenter(fx), pTo = Tween.CenterIn(pile, fx);
                            for (int k = 0; k < Math.Min(ld.Sparks.Value, 6); k++)
                            {
                                bool last = k == Math.Min(ld.Sparks.Value, 6) - 1;
                                Tween.After(0.06f * k, () => Tween.Projectile(fx, pFrom, pTo, PaperFx.BrassLight, 20f, 0.32f, 60f, last ? (Action)(() => { Tween.RingBurst(fx, pTo, PaperFx.BrassLight, 90f, 0.24f); Tween.Shake(pile, 5f, 0.2f); }) : null));
                            }
                        }
                        return;
                    }
                    if (!LightUi.Exists)
                    {
                        var rt0 = g.Anchor("player");
                        if (rt0 != null) Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(0f, 90f), "灯" + ld.Spent + " 放出!", PaperFx.Brass, 30, 40f, 1.0f);
                        return;
                    }
                    Vector2 from = LightUi.GlassCenter(fx);
                    var targets = new List<Vector2>();
                    var board = ctx != null && ctx.Prev != null ? ctx.Prev : (g.Rs != null ? g.Rs.Combat : null);
                    if (ctx != null && ctx.ToDolls) targets = DollTargets(g, fx, ctx);
                    else if (ctx != null && ctx.AllEnemies && board != null)
                    {
                        for (int i = 0; i < board.Enemies.Count; i++)
                        {
                            if (board.Enemies[i].Hp <= 0) continue;
                            var spr = g.Battle != null ? g.Battle.EnemySprite(i) : null; var pan = g.Anchor("enemy" + i);
                            if (spr != null) targets.Add(Tween.CenterIn(spr, fx)); else if (pan != null) targets.Add(Tween.CenterIn(pan, fx) + new Vector2(0f, 60f));
                        }
                    }
                    else if (ctx != null && ctx.EnemyIndex >= 0)
                    {
                        var spr = g.Battle != null ? g.Battle.EnemySprite(ctx.EnemyIndex) : null; var pan = g.Anchor("enemy" + ctx.EnemyIndex);
                        if (spr != null) targets.Add(Tween.CenterIn(spr, fx)); else if (pan != null) targets.Add(Tween.CenterIn(pan, fx) + new Vector2(0f, 60f));
                    }
                    if (targets.Count == 0) { var ps = g.Battle != null ? g.Battle.PlayerSprite() : null; targets.Add(ps != null ? Tween.CenterIn(ps, fx) + new Vector2(0f, 40f) : from + new Vector2(0f, 200f)); }
                    Audio.Key("LightDischarged");
                    foreach (var t in targets)
                    {
                        var tt = t;
                        Tween.BeamFx(fx, from, tt, Color.white, 0.34f, 1.3f, ThemeFx.LightStreak());
                        // 筋に沿って光の粒
                        for (int k = 0; k < 4; k++) { float kk = 0.2f + 0.2f * k; Tween.After(0.02f * k, () => Tween.Projectile(fx, from, tt, ThemeFx.LampCore, 18f, 0.22f, 10f)); }
                    }
                    LightUi.HoldFor(0.6f);
                    LightUi.Drain(ld.Spent);
                    Tween.Stamp(fx, LightUi.TopCenter(fx) + new Vector2(70f, 36f), "放出 " + ld.Spent, PaperFx.BrassLight, PaperFx.BrassInk, PaperFx.Brass, 20, 0.6f, -6f);
                    break;
                }
                case GameEvent_HpHealed h:
                {
                    var rt = g.Anchor("player");
                    if (rt == null) return;
                    if (ctx != null && ctx.Batch != null)
                    {   // 束ねた人形の粒 (2026-09-21): 癒しの人形が跳ねて心の絵。数字は最後にまとめて
                        var bt = ctx.Batch;
                        var hd = h.SourceUid != null && g.Battle != null ? g.Battle.DollSprite(h.SourceUid) : null;
                        if (hd != null) { Tween.Punch(hd, 0.12f, 0.3f, true); Tween.IconBurst(fx, Tween.CenterIn(hd, fx) + new Vector2(0f, 10f), "heart", new Color(0.6f, 1f, 0.6f, 0.9f), 60f); }
                        if (!bt.SoundHeal) { bt.SoundHeal = true; Audio.Key("HpHealed"); }
                        bt.Heal += h.Amount; bt.HealN++;
                        if (ctx.BatchLast) FlushDollBatch(g, fx, bt, nudgeHp);
                        return;
                    }
                    Audio.Key("HpHealed");
                    var hs = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    if (hs != null) Tween.IconBurst(fx, Tween.CenterIn(hs, fx) + new Vector2(0f, 20f), "heart", new Color(0.6f, 1f, 0.6f, 0.9f), 100f);
                    Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(-80f, 10f), "+" + h.Amount, UiKit.ColAccent, 30, 40f, 0.7f);
                    var hName = PermanentName(g, ctx, h.SourceUid);   // 出所の名前 (2026-09-22)
                    if (hName != null) Tween.Float(fx, PlayerFloatPos(g, fx) + new Vector2(-80f, -18f), hName, PaperFx.Paper, 18, 34f, 0.8f);
                    // 人形の誘発 (2026-09-19): 癒しの人形が小さく跳ねて心の絵を出す
                    var hDoll = h.SourceUid != null && g.Battle != null ? g.Battle.DollSprite(h.SourceUid) : null;
                    if (hDoll != null) { Tween.Punch(hDoll, 0.12f, 0.3f, true); Tween.IconBurst(fx, Tween.CenterIn(hDoll, fx) + new Vector2(0f, 10f), "heart", new Color(0.6f, 1f, 0.6f, 0.9f), 60f); }
                    break;
                }
            }
        }
    }
}
