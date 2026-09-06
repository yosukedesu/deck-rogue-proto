// BattleView.cs — 戦闘画面の「残留UI」(2026-09-07 M2-4「動きから」)。
// 旧: コマンドごとに画面を丸ごと作り直す (カードが瞬間移動する)。
// 新: 戦場 (敵パネル・リーダー) と手札のカードは GameObject を持ち越し、状態の差分をトゥイーンで見せる。
//   - 手札: uid ごとにカードを保持。並び替えは扇の位置へ滑る。ドローは山札から飛んでくる。プレイ/捨て/伏せ/消滅は行き先へ飛んで消える
//   - 敵・リーダー: 入れ物 (アンカー) を持ち越し、中身だけ組み直す。HP バーは前の値から滑る
//   - 上部バー・山札・ターン終了・モーダルは毎回作り直す (動かないので安い)
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public class BattleView
    {
        public RectTransform Root;
        public RectTransform FieldLayer;
        public RectTransform HandLayer;
        public RectTransform UiLayer;
        RectTransform _enemiesArea;
        RectTransform _playerArea;
        readonly List<RectTransform> _enemyPanels = new List<RectTransform>();
        readonly List<Image> _enemyHits = new List<Image>();
        int[] _shownEnemyHp = new int[0];
        int _shownPlayerHp = -1;
        int _bgAct = -1;

        public class HandCard
        {
            public RectTransform Rt;
            public CardInstance Card;
            public Vector2 BasePos;
            public float BaseRot;
            public int Index;
            public bool Playable;
            public bool Settable;
        }
        readonly Dictionary<string, HandCard> _hand = new Dictionary<string, HandCard>();

        /// <summary>直前にプレイした札 (行き先の演出用)。BattleScreen.PlayCard が設定し、SyncHand が消費する</summary>
        public string LastPlayedUid;
        public int LastPlayedTarget = -1;

        public static BattleView Ensure(GameRoot g, RectTransform root)
        {
            if (g.Battle != null && g.Battle.Root == root && g.Battle.FieldLayer != null) return g.Battle;
            var v = new BattleView();
            v.Root = root;
            v.FieldLayer = UiKit.NewRect("field", root);
            UiKit.Stretch(v.FieldLayer, 0f, 0f, 0f, 0f);
            v.HandLayer = UiKit.NewRect("handlayer", root);
            UiKit.Anchor(v.HandLayer, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-700f, BattleScreen.HandY), new Vector2(700f, BattleScreen.HandY + CardView.H * BattleScreen.CardScale + 40f));
            v.UiLayer = UiKit.NewRect("ui", root);
            UiKit.Stretch(v.UiLayer, 0f, 0f, 0f, 0f);
            g.Battle = v;
            return v;
        }

        public void Destroy()
        {
            if (Root == null) return;
            foreach (var l in new[] { FieldLayer, HandLayer, UiLayer })
            {
                if (l == null) continue;
                l.SetParent(null, false);
                UnityEngine.Object.Destroy(l.gameObject);
            }
            FieldLayer = null; HandLayer = null; UiLayer = null;
            _hand.Clear();
            _enemyPanels.Clear();
            _enemyHits.Clear();
        }

        public void ClearUi()
        {
            for (int i = UiLayer.childCount - 1; i >= 0; i--)
            {
                var c = UiLayer.GetChild(i);
                c.SetParent(null, false);
                UnityEngine.Object.Destroy(c.gameObject);
            }
        }

        // ---- 戦場 ----

        public void SyncField(GameRoot g, GameState st)
        {
            var run = g.Rs;
            if (_bgAct != run.Act)
            {
                var old = FieldLayer.Find("bg");
                if (old != null) { old.SetParent(null, false); UnityEngine.Object.Destroy(old.gameObject); }
                var bg = UiKit.NewRect("bg", FieldLayer);
                UiKit.Stretch(bg, 0f, 0f, 0f, 0f);
                bg.SetAsFirstSibling();
                BattleScreen.BuildBackground(bg, run);
                _bgAct = run.Act;
            }
            // 敵の入れ物: 数が変わったら作り直す (分裂・孵化)
            if (_enemiesArea == null || _enemyPanels.Count != st.Enemies.Count)
            {
                if (_enemiesArea != null) { _enemiesArea.SetParent(null, false); UnityEngine.Object.Destroy(_enemiesArea.gameObject); }
                _enemyPanels.Clear();
                _enemyHits.Clear();
                _enemiesArea = UiKit.NewRect("enemies", FieldLayer);
                UiKit.Anchor(_enemiesArea, new Vector2(0.40f, 0.31f), new Vector2(0.99f, 0.94f), Vector2.zero, Vector2.zero);
                var hg = UiKit.Horz(_enemiesArea, 28, 0);
                hg.childAlignment = TextAnchor.LowerCenter;
                hg.childForceExpandWidth = false;
                hg.childForceExpandHeight = true;
                hg.childControlWidth = true;
                int n = Math.Max(1, st.Enemies.Count);
                float w = Mathf.Min(330f, 1100f / n);
                for (int i = 0; i < st.Enemies.Count; i++)
                {
                    var pan = UiKit.NewRect("enemy" + i, _enemiesArea);
                    UiKit.Le(pan, w, -1f, w, -1f);
                    var hit = pan.gameObject.AddComponent<Image>();
                    hit.color = new Color(0f, 0f, 0f, 0f);
                    var btn = pan.gameObject.AddComponent<Button>();
                    btn.targetGraphic = hit;
                    btn.transition = Selectable.Transition.None;
                    int captured = i;
                    btn.onClick.AddListener(delegate { g.OnEnemyClicked(captured); });
                    Tooltip.Attach(pan.gameObject, delegate { return BattleScreen.EnemyTip(g, captured); });
                    _enemyPanels.Add(pan);
                    _enemyHits.Add(hit);
                }
                _shownEnemyHp = new int[st.Enemies.Count];
                for (int i = 0; i < st.Enemies.Count; i++) _shownEnemyHp[i] = st.Enemies[i].Hp;
            }
            for (int i = 0; i < st.Enemies.Count; i++)
            {
                var pan = _enemyPanels[i];
                bool alive = st.Enemies[i].Hp > 0;
                _enemyHits[i].raycastTarget = alive;
                _enemyHits[i].GetComponent<Button>().interactable = alive;
                for (int c = pan.childCount - 1; c >= 0; c--) { var ch = pan.GetChild(c); ch.SetParent(null, false); UnityEngine.Object.Destroy(ch.gameObject); }
                g.RegisterAnchor("enemy" + i, pan);
                bool wasAlive = _shownEnemyHp[i] > 0;
                BattleScreen.FillEnemyPanel(g, pan, st, i, _shownEnemyHp[i]);
                _shownEnemyHp[i] = st.Enemies[i].Hp;
                if (wasAlive && !alive)
                {
                    // 撃破: スプライトが白く光ってから沈む
                    var sprRt = pan.Find("sprite") as RectTransform;
                    var sprImg = sprRt != null ? sprRt.GetComponent<Image>() : null;
                    if (sprImg != null)
                    {
                        var dim = sprImg.color;
                        sprImg.color = Color.white;
                        Tween.Run(0.5f, k => { if (sprImg != null) sprImg.color = Color.Lerp(Color.white, dim, k); }, Ease.InQuad);
                        Tween.Move(sprRt, sprRt.anchoredPosition + new Vector2(0f, -30f), 0.5f, Ease.InQuad);
                    }
                    Audio.Play(st.Enemies[i].Fled == true ? "lunge" : "death", 0.9f);
                }
            }
            // リーダー
            if (_playerArea == null)
            {
                _playerArea = UiKit.NewRect("player", FieldLayer);
                UiKit.Anchor(_playerArea, new Vector2(0.02f, 0.31f), new Vector2(0.38f, 0.94f), Vector2.zero, Vector2.zero);
                _shownPlayerHp = st.Player.Hp;
            }
            for (int c = _playerArea.childCount - 1; c >= 0; c--) { var ch = _playerArea.GetChild(c); ch.SetParent(null, false); UnityEngine.Object.Destroy(ch.gameObject); }
            g.RegisterAnchor("player", _playerArea);
            BattleScreen.FillPlayerPanel(g, _playerArea, st, _shownPlayerHp);
            _shownPlayerHp = st.Player.Hp;
        }

        public RectTransform EnemySprite(int index)
        {
            if (index < 0 || index >= _enemyPanels.Count || _enemyPanels[index] == null) return null;
            return _enemyPanels[index].Find("sprite") as RectTransform;
        }

        public RectTransform PlayerSprite()
        {
            return _playerArea != null ? _playerArea.Find("sprite") as RectTransform : null;
        }

        /// <summary>演出の途中で HP バーだけ先に動かす (順送りの敵フェーズ: 被弾のたびに減る)</summary>
        public void NudgeEnemyHp(int index, int delta)
        {
            if (index < 0 || index >= _enemyPanels.Count || index >= _shownEnemyHp.Length) return;
            _shownEnemyHp[index] = Math.Max(0, _shownEnemyHp[index] + delta);
            BattleScreen.TweenHpBar(_enemyPanels[index], _shownEnemyHp[index]);
        }

        public void NudgePlayerHp(int delta)
        {
            if (_playerArea == null) return;
            _shownPlayerHp = Math.Max(0, _shownPlayerHp + delta);
            BattleScreen.TweenHpBar(_playerArea, _shownPlayerHp);
        }

        // ---- 手札 ----

        public HandCard CardAt(int index)
        {
            foreach (var kv in _hand) if (kv.Value.Index == index) return kv.Value;
            return null;
        }

        public void SyncHand(GameRoot g, GameState st, bool animate)
        {
            var hand = st.Player.Hand;
            int n = hand.Count;
            float spacing = n > 0 ? Mathf.Min(CardView.W * BattleScreen.CardScale + 12f, 1180f / n) : 0f;
            float center = (n - 1) / 2f;
            float areaH = CardView.H * BattleScreen.CardScale + 40f;
            bool myTurn = st.Phase == CombatPhases.PlayerTurn;

            // 予測の対象: 狙いを付けた敵、無ければ生存が1体の時だけその敵
            int alive = 0, firstAlive = -1;
            for (int i = 0; i < st.Enemies.Count; i++) if (st.Enemies[i].Hp > 0) { alive++; if (firstAlive < 0) firstAlive = i; }
            int preview = g.PreferredTarget >= 0 && g.PreferredTarget < st.Enemies.Count && st.Enemies[g.PreferredTarget].Hp > 0 ? g.PreferredTarget : (alive == 1 ? firstAlive : -1);

            // 1) 手札から消えた札 → 行き先へ飛ばして消す
            var gone = new List<string>();
            foreach (var kv in _hand)
            {
                bool still = false;
                for (int i = 0; i < n; i++) if (hand[i].Uid == kv.Key) { still = true; break; }
                if (!still) gone.Add(kv.Key);
            }
            float outDelay = 0f;
            foreach (var uid in gone)
            {
                var hc = _hand[uid];
                _hand.Remove(uid);
                AnimateOut(g, st, hc, outDelay);
                outDelay += 0.04f;
            }

            // 1') 捨てが起きたターンの切り替わりは、捨てが飛び終わってからドローする
            float drawDelay = gone.Count > 0 && LastPlayedUid == null ? 0.28f : 0f;
            // 2) 新しい札 → 山札の位置に作る
            var drawRt = g.Anchor("pile-draw");
            Vector2 drawPos = drawRt != null ? Tween.CenterIn(drawRt, HandLayer) : new Vector2(-900f, -100f);
            int newCount = 0;
            for (int i = 0; i < n; i++)
            {
                var c = hand[i];
                int cost = c.Def.Cost;
                try { cost = Effects.EffectiveCost(st, c); } catch (Exception) { }
                bool playable = myTurn && g.Pending == null && Effects.IsPlayableFromHand(c) && cost <= st.Player.Energy && Effects.RetainerRequirementMet(st, c);
                bool settable = myTurn && g.Pending == null && SetBase.CanSetCard(st, c.Uid);
                HandCard hc;
                bool fresh = !_hand.TryGetValue(c.Uid, out hc);
                if (!fresh && (hc.Playable != playable || hc.Settable != settable || !ReferenceEquals(hc.Card.Def, c.Def) || hc.Card.GrowBonus != c.GrowBonus))
                {
                    // 見た目が変わる (プレイ可否・鍛え・育つ) → 同じ位置で作り直す
                    var pos = hc.Rt.anchoredPosition; var rot = hc.Rt.localRotation; var scl = hc.Rt.localScale;
                    hc.Rt.SetParent(null, false);
                    UnityEngine.Object.Destroy(hc.Rt.gameObject);
                    _hand.Remove(c.Uid);
                    fresh = true;
                    hc = null;
                    CardView.PreviewEnemy = preview;
                    var rt2 = CardView.Build(HandLayer, c, st, playable, true, "hand" + i);
                    CardView.PreviewEnemy = -1;
                    rt2.anchoredPosition = pos; rt2.localRotation = rot; rt2.localScale = scl;
                    hc = new HandCard { Rt = rt2, Card = c, Playable = playable, Settable = settable };
                    _hand[c.Uid] = hc;
                    fresh = false;
                    Attach(g, hc, c);
                }
                if (fresh)
                {
                    CardView.PreviewEnemy = preview;
                    var rt = CardView.Build(HandLayer, c, st, playable, true, "hand" + i);
                    CardView.PreviewEnemy = -1;
                    hc = new HandCard { Rt = rt, Card = c, Playable = playable, Settable = settable };
                    _hand[c.Uid] = hc;
                    Attach(g, hc, c);
                    if (animate)
                    {
                        rt.anchoredPosition = drawPos;
                        rt.localScale = Vector3.one * 0.35f;
                        rt.localRotation = Quaternion.Euler(0f, 0f, -20f);
                    }
                    newCount++;
                }
                hc.Card = c;
                hc.Index = i;
                hc.Rt.name = "hand" + i;
                float dx = (i - center) * spacing;
                float dy = -Mathf.Abs(i - center) * 10f;
                hc.BasePos = new Vector2(dx, -areaH / 2f + CardView.H * BattleScreen.CardScale / 2f + dy);
                hc.BaseRot = -(i - center) * 3f;
            }
            // 3) 並び順と位置
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                HandCard hc;
                if (!_hand.TryGetValue(hand[i].Uid, out hc)) continue;
                hc.Rt.SetSiblingIndex(Mathf.Min(i, HandLayer.childCount - 1));
                var target = hc.BasePos;
                var rot = Quaternion.Euler(0f, 0f, hc.BaseRot);
                if (!animate)
                {
                    hc.Rt.anchoredPosition = target; hc.Rt.localRotation = rot; hc.Rt.localScale = Vector3.one * BattleScreen.CardScale;
                    continue;
                }
                bool isNew = (hc.Rt.anchoredPosition - drawPos).sqrMagnitude < 1f;
                float delay = isNew ? drawDelay + 0.05f * (k++) : 0f;
                var rtc = hc.Rt;
                Tween.After(delay, () =>
                {
                    if (rtc == null) return;
                    if (isNew) Audio.Play("card_draw", 0.6f, 0.12f);
                    Tween.Move(rtc, target, isNew ? 0.28f : 0.2f, Ease.OutCubic);
                    Tween.Scale(rtc, Vector3.one * BattleScreen.CardScale, isNew ? 0.28f : 0.2f, Ease.OutQuad);
                    var r0 = rtc.localRotation;
                    Tween.Run(isNew ? 0.28f : 0.2f, t => { if (rtc != null) rtc.localRotation = Quaternion.Slerp(r0, rot, t); }, Ease.OutQuad);
                });
            }
            LastPlayedUid = null;
            LastPlayedTarget = -1;
        }

        void Attach(GameRoot g, HandCard hc, CardInstance c)
        {
            BattleScreen.HookHandCard(g, hc, c);
            var cardRef = c;
            Tooltip.Attach(hc.Rt.gameObject, delegate { return BattleScreen.KeywordsOnly(CardText.Body(cardRef.Def) + " " + CardText.Notes(cardRef.Def)); }, false);
        }

        /// <summary>ターン終了: 手札を全部捨て札へ飛ばす (順送り演出の TurnEnded で呼ぶ)</summary>
        public void DiscardHand(GameRoot g)
        {
            var list = new List<HandCard>(_hand.Values);
            _hand.Clear();
            list.Sort((a, b) => a.Index.CompareTo(b.Index));
            for (int i = 0; i < list.Count; i++) FlyTo(g, list[i].Rt, "pile-discard", 0.05f * i, 0.3f, true);
        }

        void AnimateOut(GameRoot g, GameState st, HandCard hc, float delay)
        {
            string uid = hc.Card.Uid;
            var rt = hc.Rt;
            rt.SetAsLastSibling();
            // 行き先を状態から判定
            bool inSet = false; int setIdx = -1;
            for (int i = 0; i < st.Player.SetCards.Count; i++) if (st.Player.SetCards[i].Uid == uid) { inSet = true; setIdx = i; }
            bool inPerm = false;
            for (int i = 0; i < st.Player.Permanents.Count; i++) if (st.Player.Permanents[i].Uid == uid) inPerm = true;
            bool inExhaust = false;
            for (int i = 0; i < st.Player.ExhaustPile.Count; i++) if (st.Player.ExhaustPile[i].Uid == uid) inExhaust = true;

            if (inSet) { Audio.Play("card_set", 0.8f); FlyTo(g, rt, "setslot" + setIdx, delay, 0.3f, true, 0.5f); return; }
            if (inPerm) { FlyTo(g, rt, "player", delay, 0.3f, true, 0.4f); return; }
            if (uid == LastPlayedUid)
            {
                // プレイ: 対象の敵 (無ければ中央上) へ飛んでから、消滅なら砕け、それ以外は捨て札へ
                var fx = HandLayer;
                Vector2 to = new Vector2(0f, 420f);
                var target = LastPlayedTarget >= 0 ? g.Anchor("enemy" + LastPlayedTarget) : null;
                if (target != null) to = Tween.CenterIn(target, fx) + new Vector2(0f, 40f);
                Audio.Play("card_play", 0.8f);
                Tween.Move(rt, to, 0.2f, Ease.OutCubic);
                Tween.Scale(rt, Vector3.one * 0.6f, 0.2f, Ease.OutQuad);
                rt.localRotation = Quaternion.identity;
                string dest = inExhaust ? null : "pile-discard";
                Tween.After(0.26f, () => { if (rt != null) { if (dest == null) Shatter(rt); else FlyTo(g, rt, dest, 0f, 0.25f, true); } });
                return;
            }
            if (inExhaust) { Tween.After(delay, () => { if (rt != null) Shatter(rt); }); return; }
            FlyTo(g, rt, "pile-discard", delay, 0.3f, true);
        }

        void Shatter(RectTransform rt)
        {
            var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            Tween.Scale(rt, Vector3.one * 0.2f, 0.3f, Ease.InQuad);
            Tween.Run(0.3f, k => { if (cg != null) cg.alpha = 1f - k; }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        void FlyTo(GameRoot g, RectTransform rt, string anchor, float delay, float dur, bool destroy, float endScale = 0.3f)
        {
            var dest = g.Anchor(anchor);
            Vector2 to = dest != null ? Tween.CenterIn(dest, HandLayer) : new Vector2(900f, -100f);
            var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            Tween.After(delay, () =>
            {
                if (rt == null) return;
                rt.SetAsLastSibling();
                Tween.Move(rt, to, dur, Ease.InOutQuad);
                Tween.Scale(rt, Vector3.one * endScale, dur, Ease.InQuad);
                Tween.Run(dur, k => { if (cg != null) cg.alpha = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f; }, Ease.Linear, () => { if (destroy && rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
            });
        }
    }
}
