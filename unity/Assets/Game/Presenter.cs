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
            for (int i = Math.Min(_seen, log.Count); i < log.Count; i++)
            {
                var ev = log[i];
                float gap;
                if (ev is GameEvent_CardPlayed) { var cp = ev; _playHint = PlayOutcome(log, i); try { Show(g, fx, cp, true); } catch (Exception e) { Debug.LogWarning("[Presenter] " + e.Message); } continue; }   // 攻撃コマは即・間を取らない
                if (ev is GameEvent_DamageDealt) gap = 0.4f;
                else if (ev is GameEvent_TurnEnded || ev is GameEvent_TurnStarted) gap = 0.6f;
                else if (ev is GameEvent_BlockGained || ev is GameEvent_HpHealed) gap = 0.15f;
                else if (IsStatusEvent(ev)) gap = 0.3f;
                else if (ev is GameEvent_ReactionTriggered) gap = 0.55f;   // 札が飛んで着弾するまで待ってから返しのダメージ (2026-09-17)
                else if (IsTrapEvent(ev)) gap = 0.3f;
                else if (TableSound(ev) != null) gap = 0.12f;   // 表 (audio.json) で音だけ鳴るイベント (撃破・分裂…)
                else continue;
                var captured = ev;
                var ctx = ReactionContextFor(g, log, i, visibleBoard);
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
            // Rebuild 直後は LayoutGroup が未計算 (全て原点) なので、的の座標を読む前にレイアウトを確定させる
            Canvas.ForceUpdateCanvases();
            // カードが敵へ飛ぶ 0.2 秒に着弾を合わせる
            float delay = 0f;
            for (int i = _seen; i < log.Count; i++) if (log[i] is GameEvent_DamageDealt dd && dd.Source == "player") { delay = 0.13f; break; }   // 着弾は振り抜き (0.11〜0.15s) に合わせる
            for (int i = _seen; i < log.Count; i++)
            {
                var ev = log[i];
                if (ev is GameEvent_CardPlayed) { var cp = ev; _playHint = PlayOutcome(log, i); try { Show(g, fx, cp, false); } catch (Exception e) { Debug.LogWarning("[Presenter] " + e.Message); } continue; }   // 攻撃コマは札を出した瞬間に
                if (!(ev is GameEvent_DamageDealt || ev is GameEvent_BlockGained || ev is GameEvent_HpHealed || ev is GameEvent_TurnStarted || ev is GameEvent_TurnEnded || IsStatusEvent(ev) || IsTrapEvent(ev) || TableSound(ev) != null)) continue;
                var captured = ev;
                var ctx = ReactionContextFor(g, log, i, combat);
                // 連続する演出は 0.12 秒ずつずらす (同じ場所に重ならない・順番が読める)
                Tween.After(delay, () => { try { Show(g, fx, captured, false, ctx); } catch (Exception e) { Debug.LogWarning("[Presenter] " + e.Message); } });
                delay += ev is GameEvent_ReactionTriggered ? 0.45f : 0.12f;
            }
            _seen = log.Count;
            _seenCombat = combat;
        }

        // ---- からくり (仕込み札) の演出 (2026-09-17 ユーザー「戦闘の演出で足りていないもの」→ ⑤ リアクション発動から) ----
        // 発動: 札の幽霊が仕込み枠から跳ねて飛び出し、返し/打ち消しなら行動している敵の意図の札へ、守りなら自分へ。着弾で青緑の輪と星。枠の上に「発動」の判。
        // 打ち消し: 敵の意図の札に真鍮の×が押され、札が揺れる。温存: 「温存」の判 (灰)。期限切れ: 札が捨て札へ落ちる。壊し: 札が砕ける。空振り: 小さく「空振り」。

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
            if (ev is GameEvent_DamageDealt || ev is GameEvent_BlockGained || ev is GameEvent_HpHealed || ev is GameEvent_TurnStarted || ev is GameEvent_TurnEnded || ev is GameEvent_CardPlayed || IsStatusEvent(ev) || IsTrapEvent(ev)) return null;
            // 絵の側 (BattleView) が札の飛び・撃破の消えに合わせて鳴らすイベントは、ここでは二重に鳴らさない
            if (ev is GameEvent_CardSet || ev is GameEvent_CardsDrawn || ev is GameEvent_EnemyDied || ev is GameEvent_EnemyFled) return null;
            var n = ev.GetType().Name;
            if (n.StartsWith("GameEvent_")) n = n.Substring("GameEvent_".Length);
            return Audio.HasKey(n) ? n : null;
        }

        /// <summary>からくり (仕込み札) の出来事: 発動・温存・期限切れ・壊し・空振り・打ち消し。絵と音を Show で (2026-09-17 リアクション発動の演出)</summary>
        static bool IsTrapEvent(GameEvent ev)
        {
            return ev is GameEvent_ReactionTriggered || ev is GameEvent_ReactionHeld || ev is GameEvent_SetCardExpired || ev is GameEvent_SetCardDestroyed || ev is GameEvent_ReactionWhiffed || ev is GameEvent_ActionNegated;
        }

        /// <summary>リアクションの演出に要る文脈: 札があった仕込み枠の的と、行動している敵。イベント自体は CardId しか持たないので、見えている盤面とログの前後から引く</summary>
        sealed class ReactionCtx { public RectTransform Slot; public int EnemyIndex = -1; }
        static ReactionCtx ReactionContextFor(GameRoot g, IReadOnlyList<GameEvent> log, int i, GameState visible)
        {
            var ev = log[i];
            string cardId = (ev as GameEvent_ReactionTriggered)?.CardId ?? (ev as GameEvent_SetCardExpired)?.CardId ?? (ev as GameEvent_SetCardDestroyed)?.CardId ?? (ev as GameEvent_ReactionWhiffed)?.CardId;
            if (cardId == null && !(ev is GameEvent_ReactionHeld) && !(ev is GameEvent_ActionNegated)) return null;
            var ctx = new ReactionCtx();
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
                            Tween.SlashFx(fx, Tween.CenterIn(spr, fx), UnityEngine.Random.Range(-50f, -20f), new Color(1f, 0.98f, 0.9f, 0.95f), d.Amount >= 15);   // 直線の筋＋残像＋着弾の光＋火花 (2026-09-16)
                            Stage.Flash("enemy" + (d.EnemyIndex ?? 0));
                        }
                        Audio.Key("DamageDealt.player.swing");
                        Audio.Key(d.Amount >= 15 ? "DamageDealt.player.big" : "DamageDealt.player");
                        if (d.Amount >= 15) Stage.Shake(Mathf.Min(14f, d.Amount * 0.4f), 0.25f);
                        Tween.Float(fx, pos, d.Amount.ToString(), d.Amount > 0 ? PaperFx.BrassLight : UiKit.ColDim, d.Amount >= 20 ? 46 : 36);
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
                        if (pSpr != null) Tween.SlashFx(fx, Tween.CenterIn(pSpr, fx), UnityEngine.Random.Range(20f, 50f), new Color(1f, 0.62f, 0.5f, 0.95f), d.HpLoss >= 12);
                        Audio.Key("DamageDealt.enemy.swing");
                        // 完全に防いだ時は被弾音でなく防御音 (2026-09-14 ユーザー指摘)。ブロックで受けた盾の音 + 構えの絵
                        if (d.HpLoss <= 0 && d.Amount > 0) { Audio.Key("DamageDealt.blocked"); Stage.PlayAnim("player", "block"); }
                        else Audio.Key(d.HpLoss >= 12 ? "DamageDealt.enemy.big" : "DamageDealt.enemy", d.HpLoss > 0 ? 1f : 0.5f);
                        if (d.HpLoss > 0)
                        {
                            Stage.PlayAnim("player", "hurt");
                            if (pSpr != null) Tween.Lunge(pSpr, new Vector2(-36f, 0f));   // のけぞり (後ろへ小さく)
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
                    Audio.Key("BlockGained");
                    var ps = g.Battle != null ? g.Battle.PlayerSprite() : null;
                    if (ps != null) Tween.IconBurst(fx, Tween.CenterIn(ps, fx) + new Vector2(0f, 20f), "shield", new Color(0.55f, 0.75f, 1f, 0.9f), 110f);
                    Tween.Float(fx, Tween.CenterIn(rt, fx) + new Vector2(80f, 10f), "+" + b.Amount, UiKit.ColBlock, 30, 40f, 0.7f);
                    break;
                }
                case GameEvent_TurnStarted ts:
                    Audio.Key("TurnStarted");
                    Banner(fx, "ターン " + ts.Turn + "  —  あなたの番", UiKit.ColAccent);
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
