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
            for (int i = Math.Min(_seen, log.Count); i < log.Count; i++)
            {
                var ev = log[i];
                float gap;
                if (ev is GameEvent_CardPlayed) { var cp = ev; _playHint = PlayOutcome(log, i); var cctx = new ReactionCtx { Prev = visibleBoard }; try { Show(g, fx, cp, true, cctx); } catch (Exception e) { Debug.LogWarning("[Presenter] " + e.Message); } continue; }   // 攻撃コマは即・間を取らない
                if (ev is GameEvent_DamageDealt) gap = 0.4f;
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
                if (i == finishing) { ctx.FinishingBlow = true; gap += 0.35f; }   // とどめはヒットストップぶん長く見せる
                Tween.After(delay, () => { try { Show(g, fx, captured, true, ctx); } catch (Exception e) { Debug.LogWarning("[Presenter] " + e.Message); } });
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
                if (i == finishing) ctx.FinishingBlow = true;
                // 連続する演出は 0.12 秒ずつずらす (同じ場所に重ならない・順番が読める)
                Tween.After(delay, () => { try { Show(g, fx, captured, false, ctx); } catch (Exception e) { Debug.LogWarning("[Presenter] " + e.Message); } });
                delay += ev is GameEvent_ReactionTriggered ? 0.45f : ev is GameEvent_EnemyActionExecuting ? 0.3f : ev is GameEvent_EnemyInterrupted ? 0.45f : 0.12f;
            }
            _seen = log.Count;
            _seenCombat = combat;
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
        static void ShowEnemyAct(GameRoot g, RectTransform fx, GameEvent ev, ReactionCtx ctx, bool live)
        {
            switch (ev)
            {
                case GameEvent_EnemyActionExecuting ex:
                {
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
                    Tween.Float(fx, pos + new Vector2(0f, 50f), "盾を砕いた " + bs.Amount, PaperFx.SkyLight, 26, 36f, 0.9f);
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
                    var mark = pan.Find("strip/hpbar/mark") as RectTransform;
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
        /// 決着の余韻 (2026-09-17 ⑧): 勝利＝「勝利」の帯の下に戦いの記録 (ターン数・与ダメ・被ダメ・読み勝ち・完全に凌いだ・打ち消し) が1行ずつ積み上がる。
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
                else if (e is GameEvent_BurnTick bt) { total += bt.Amount; cur += bt.Amount; }
                else if (e is GameEvent_ReactionTriggered) fired++;
                else if (e is GameEvent_ActionNegated) negates++;
            }
            best = Math.Max(best, cur);
            lines.Add(turns + "ターン ・ 与ダメ " + total + (best > 0 ? "（最大ターン " + best + "）" : ""));
            lines.Add("被ダメ " + hpLost);
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
            if (art != null) ic.sprite = art;
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
            }
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
            var band = UiKit.Pan(rt, PaperFx.ManaInk, "band");
            UiKit.Anchor(band.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1f, 27f)); band.raycastTarget = false;
            var name = UiKit.Deco(rt, def != null ? def.Name : "", 13, PaperFx.Paper, TextAnchor.MiddleCenter);
            UiKit.Anchor(name.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(0f, 27f));
            name.textWrappingMode = TextWrappingModes.NoWrap; name.overflowMode = TextOverflowModes.Ellipsis;
            return rt;
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
            return ev is GameEvent_EnemyActionExecuting || ev is GameEvent_EnemyHealed || ev is GameEvent_GoldStolen || ev is GameEvent_CardsMilled || ev is GameEvent_ThornsReflected || ev is GameEvent_BurnTick || ev is GameEvent_RegenTicked || ev is GameEvent_BlockShattered || ev is GameEvent_EnemyStaggered || ev is GameEvent_EnemyInterrupted || ev is GameEvent_EnemyDied || ev is GameEvent_EnemyFled;
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
            return ev is GameEvent_ReactionTriggered || ev is GameEvent_ReactionHeld || ev is GameEvent_SetCardExpired || ev is GameEvent_SetCardDestroyed || ev is GameEvent_ReactionWhiffed || ev is GameEvent_ActionNegated;
        }

        /// <summary>リアクションの演出に要る文脈: 札があった仕込み枠の的と、行動している敵。イベント自体は CardId しか持たないので、見えている盤面とログの前後から引く</summary>
        sealed class ReactionCtx { public RectTransform Slot; public int EnemyIndex = -1; public GameState Prev; public bool FinishingBlow; }

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

        static bool IsStatusEvent(GameEvent ev)
        {
            return ev is GameEvent_StatusInflicted || ev is GameEvent_ExposedApplied || ev is GameEvent_EnemyWeakened || ev is GameEvent_BurnApplied || ev is GameEvent_StrengthGained || ev is GameEvent_GrowthAdded || ev is GameEvent_MomentumAdded;
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
                        var pos = Tween.CenterIn(rt, fx) + new Vector2(UnityEngine.Random.Range(-30f, 30f), 20f);
                        // ⑥ ダメージの質 (2026-09-17): 急所・貫通・盾が吸った・装甲/ターン装甲/殻/無形の頭打ち を数字の脇で見分ける (イベントの値＝実処理と同じ)
                        bool crit = d.Exposed == true, pierced = d.Pierced == true;
                        int blocked = d.Blocked ?? 0, armorCut = d.ArmorCut ?? 0, turnCut = d.TurnArmorCut ?? 0, burrowCut = d.BurrowCut ?? 0, nemesisCut = d.NemesisCut ?? 0;
                        bool shell = ctx != null && ctx.Prev != null && ei < ctx.Prev.Enemies.Count && ctx.Prev.Enemies[ei].BurrowActive == true;
                        bool capped = armorCut > 0 || turnCut > 0 || nemesisCut > 0;
                        bool big = d.Amount >= 15 || crit;
                        // 斬撃の筋と白い点滅、大きいほど画面も揺れる。急所は真鍮の筋＋星、盾に全部吸われた時は鋼青の輪 (金属の当たり)、頭打ちは鈍い輪
                        var spr = g.Battle != null ? g.Battle.EnemySprite(ei) : null;
                        Vector2 hit = spr != null ? Tween.CenterIn(spr, fx) : Tween.CenterIn(rt, fx);
                        Color streak = crit ? new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 1f) : capped ? new Color(0.78f, 0.8f, 0.86f, 0.9f) : new Color(1f, 0.98f, 0.9f, 0.95f);   // 頭打ちは鈍い筋 (刃が通らない)
                        // 敵のブロックが全部吸った (殻は別) = 敵が盾で受け止める (2026-09-17): 斬撃の筋は出さず、敵の正面に空色の盾の面。白い点滅も無し
                        bool guardedE = blocked > 0 && d.HpLoss <= 0 && !shell;
                        if (guardedE) Tween.GuardFx(fx, hit, "slash", -1f);
                        else if (spr != null)
                        {
                            Tween.SlashFx(fx, hit, UnityEngine.Random.Range(-50f, -20f), streak, big);   // 直線の筋＋着弾の衝撃線＋火花 (2026-09-16 / 2026-09-17)
                            Stage.Flash("enemy" + ei);
                        }
                        // 急所: 筋と衝撃線が真鍮色 (big 扱い = 交差する2本目と針10) になり、真鍮の輪が広がる (旧: 星の絵 = 2026-09-17 ユーザー「星型がダサい」で撤去)
                        if (crit && !guardedE) Tween.RingBurst(fx, hit, PaperFx.BrassLight, 200f, 0.35f);
                        if (blocked > 0 && !shell && d.HpLoss > 0) Tween.IconBurst(fx, hit + new Vector2(-10f, 10f), "shield", new Color(PaperFx.Sky.r, PaperFx.Sky.g, PaperFx.Sky.b, 0.7f), 90f);
                        if (capped || burrowCut > 0) Tween.RingBurst(fx, hit, new Color(PaperFx.InkSoft.r, PaperFx.InkSoft.g, PaperFx.InkSoft.b, 0.8f), 150f, 0.3f);
                        Audio.Key("DamageDealt.player.swing");
                        if (blocked > 0 && d.HpLoss <= 0) Audio.Key("DamageDealt.blocked");
                        else Audio.Key(big ? "DamageDealt.player.big" : "DamageDealt.player");
                        if (big && !guardedE) Stage.Shake(Mathf.Min(14f, Mathf.Max(6f, d.Amount * 0.4f)) * (crit ? 1.2f : 1f), 0.25f);
                        // ⑫ カメラ (2026-09-17): 大技は舞台がぐっと寄る。とどめ (戦闘を決めた一撃) はヒットストップ＝時間が一瞬凍って、大きく寄る
                        bool finishing = ctx != null && ctx.FinishingBlow;
                        if (finishing) { Tween.HitStop(0.12f, 0.3f); Stage.ZoomPunch(1.1f, 0.6f); Stage.Shake(12f, 0.35f); Tween.RingBurst(fx, hit, new Color(1f, 1f, 0.95f, 0.9f), 260f, 0.5f); }
                        else if (big && !guardedE) Stage.ZoomPunch(crit ? 0.5f : 0.35f, 0.3f);
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
                        if (crit) notes.Add(new KeyValuePair<string, Color>("急所!", PaperFx.BrassLight));
                        if (pierced) notes.Add(new KeyValuePair<string, Color>("貫通", PaperFx.Paper));
                        if (blocked > 0 && bshield == null) notes.Add(new KeyValuePair<string, Color>((shell ? "殻で −" : "ブロックで −") + blocked, PaperFx.SkyLight));
                        if (armorCut > 0) notes.Add(new KeyValuePair<string, Color>("装甲で −" + armorCut, PaperFx.Paper2));
                        if (turnCut > 0) notes.Add(new KeyValuePair<string, Color>("ターン装甲で −" + turnCut, PaperFx.Paper2));
                        if (burrowCut > 0) notes.Add(new KeyValuePair<string, Color>("殻がこぼした −" + burrowCut, PaperFx.Paper2));
                        if (nemesisCut > 0) notes.Add(new KeyValuePair<string, Color>("無形で −" + nemesisCut, PaperFx.Paper2));
                        for (int n = 0; n < notes.Count; n++)
                        {
                            var note = notes[n]; float dy = -34f - 26f * n; float dl = 0.06f * (n + 1);
                            Tween.After(dl, () => Tween.Float(fx, pos + new Vector2(0f, dy), note.Key, note.Value, note.Key.EndsWith("!") ? 26 : 20, 34f, 1.0f));
                        }
                        if (d.Amount > 0 && !guardedE) Tween.Punch(rt, Mathf.Min(0.12f, 0.03f + d.Amount * 0.004f) * (crit ? 1.4f : 1f));
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
                            if (dd.Amount > 0 && !guarded) Tween.Punch(rtC, Mathf.Min(0.1f, 0.03f + dd.Amount * 0.004f));
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
                    Audio.Key("BlockGained");
                    var ps = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    if (ps != null) Tween.IconBurst(fx, Tween.CenterIn(ps, fx) + new Vector2(0f, 20f), "shield", new Color(0.55f, 0.75f, 1f, 0.9f), 110f);
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(80f, 10f), "+" + b.Amount, UiKit.ColBlock, 30, 40f, 0.7f);
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
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(80f, 10f), "氷壁 +" + ib.Amount, PaperFx.SkyLight, 28, 40f, 0.7f);
                    if (nudgeHp && g.Battle != null) g.Battle.NudgePlayerIce(ib.Amount);
                    break;
                }
                case GameEvent_TurnStarted ts:
                    Audio.Key("TurnStarted");
                    Banner(fx, "ターン " + ts.Turn + "  —  あなたの番", UiKit.ColAccent);
                    // 自ターンの始まりで通常ブロックは消える (留め具 blockKeep なら N まで残る)。順送りの途中の盾の数字もここで揃える。この後の置物の分は BlockGained が足す
                    if (nudgeHp && g.Battle != null)
                    {
                        var cur = g.Rs != null ? g.Rs.Combat : null;
                        int keep = cur != null && cur.BlockKeep.HasValue ? Math.Min(g.Battle.ShownPlayerBlock, cur.BlockKeep.Value) : 0;
                        g.Battle.SetPlayerBlock(keep);
                    }
                    // 罠が生きた瞬間 (準備ターン明け) を伏せ場の上に浮かせる (2026-09-14 ユーザー「伏せが有効になることを GUI で分かりやすく」)
                    {
                        var st = g.Rs != null ? g.Rs.Combat : null;
                        if (st != null)
                            for (int si = 0; si < st.Player.SetCards.Count; si++)
                                if (Effects.TrapAge(st, st.Player.SetCards[si]) == 1)
                                {
                                    var slot = g.Anchor("setslot" + si);
                                    if (slot != null) Tween.Float(fx, Tween.CenterIn(slot, fx) + new Vector2(0f, 90f), "罠が鳴る準備完了", PaperFx.ManaLight, 24, 40f, 1.2f);
                                }
                    }
                    break;
                case GameEvent_TurnEnded _:
                    Audio.Key("TurnEnded");
                    Banner(fx, "敵の番", PaperFx.Rose);
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
                    if (atk)
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
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(0f, 70f), StatusJa(si.Status) + " +" + si.Amount, PaperFx.Plum, 32, 46f, 1.2f);
                    break;
                }
                case GameEvent_ExposedApplied ea:
                {
                    var rt = g.Anchor("enemy" + ea.EnemyIndex);
                    if (rt == null) return;
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(0f, 60f), "急所 +" + ea.Amount, PaperFx.Brass, 28, 40f, 1.0f);
                    break;
                }
                case GameEvent_EnemyWeakened ew:
                {
                    var rt = g.Anchor("enemy" + ew.EnemyIndex);
                    if (rt == null) return;
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(0f, 60f), "威圧 +" + ew.Amount, PaperFx.Sky, 28, 40f, 1.0f);
                    break;
                }
                case GameEvent_BurnApplied ba:
                {
                    var rt = g.Anchor("enemy" + ba.EnemyIndex);
                    if (rt == null) return;
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(0f, 60f), "延焼 +" + ba.Amount, PaperFx.Ember, 28, 40f, 1.0f);
                    break;
                }
                case GameEvent_StrengthGained sg:
                {
                    var rt = g.Anchor("enemy" + sg.EnemyIndex);
                    if (rt == null || sg.Amount == 0) return;
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(0f, 60f), "筋力 " + (sg.Amount > 0 ? "+" : "") + sg.Amount, sg.Amount > 0 ? PaperFx.Brass : PaperFx.Sky, 28, 40f, 1.0f);
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
                    Audio.Key("HpHealed");
                    var hs = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    if (hs != null) Tween.IconBurst(fx, Tween.CenterIn(hs, fx) + new Vector2(0f, 20f), "heart", new Color(0.6f, 1f, 0.6f, 0.9f), 100f);
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(-80f, 10f), "+" + h.Amount, UiKit.ColAccent, 30, 40f, 0.7f);
                    break;
                }
            }
        }
    }
}
