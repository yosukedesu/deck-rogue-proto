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
        float[] _enemyGaps = new float[0];   // 隣の敵との間隔 (帳面の一行の幅を絞る。確認の窓も同じ幅を読む)
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
            public int Cost;        // 表示したコスト (割引・重圧で変わったら描き直す。2026-09-09)
            public int Preview;     // 表示に使った対象の敵 (-1 = 無し)
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

        int _boxLogSeen;   // 匣の閃きに使った EventLog の読み位置

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
            int prevCount = _enemiesArea != null ? _enemyPanels.Count : 0;   // 増えた分は登場の演出 (2026-09-17)
            if (_enemiesArea == null || _enemyPanels.Count != st.Enemies.Count)
            {
                if (_enemiesArea != null) { _enemiesArea.SetParent(null, false); UnityEngine.Object.Destroy(_enemiesArea.gameObject); }
                _enemyPanels.Clear();
                _enemyHits.Clear();
                _enemiesArea = UiKit.NewRect("enemies", FieldLayer);
                UiKit.Stretch(_enemiesArea, 0f, 0f, 0f, 0f);
                for (int i = 0; i < st.Enemies.Count; i++)
                {
                    var pan = UiKit.NewRect("enemy" + i, _enemiesArea);
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
            // 座席: 舞台 (HD-2D) が決める。手前左から奥右へ斜めに並び、足元 (パネル下端+130) がその座席の地面に来る。奥の敵ほど先に描く
            // 名前札・HPバー・チップは全員同じ線 (入れ物の下端 = StatusLineY・手札の上)。足元だけ座席の高さへ
            var slots = Stage.EnemySlots(st.Enemies.Count);
            var centers = new float[st.Enemies.Count];
            for (int i = 0; i < st.Enemies.Count; i++)
            {
                var feet = Stage.ProjectFeet("enemy" + i, slots[i]);
                float w = UiKit.Phone ? 220f : 330f, h = 720f;   // スマホは敵の間隔が狭い (キャンバスの高さで横の広がりも決まる) ので名前札・HPバーの幅を絞る
                UiKit.Anchor(_enemyPanels[i], new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(feet.x - w / 2f, StatusLineY), new Vector2(feet.x + w / 2f, StatusLineY + h));
                Stage.SetFeetOffset("enemy" + i, feet.y - StatusLineY);
                _enemyPanels[i].SetSiblingIndex(st.Enemies.Count - 1 - i);
                centers[i] = feet.x;
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
                // 隣の敵との間隔 (スマホで3体以上の吹き出しが重ならないよう、吹き出しの幅を間隔で絞る。2026-09-14)
                float gap = float.MaxValue;
                if (i > 0) gap = Mathf.Min(gap, Mathf.Abs(centers[i] - centers[i - 1]));
                if (i + 1 < centers.Length) gap = Mathf.Min(gap, Mathf.Abs(centers[i + 1] - centers[i]));
                if (_enemyGaps.Length != st.Enemies.Count) _enemyGaps = new float[st.Enemies.Count];
                _enemyGaps[i] = gap;
                // 倒れた瞬間 (2026-09-17): 絵と帳面を生前の姿で描いておき、EnemyDied/EnemyFled の出来事 (Presenter → KillEnemy) が着弾の後に崩す。
                // 出来事が先に来ていた (順送りの敵フェーズで倒れた) なら _died に印があるので何も描かない。出来事が来なければ 0.6 秒後に崩す (保険)
                if (alive) _died.Remove(i);
                bool dyingNow = wasAlive && !alive && !_died.Contains(i);
                BattleScreen.FillEnemyPanel(g, pan, st, i, _shownEnemyHp[i], gap, dyingNow);
                _shownEnemyHp[i] = st.Enemies[i].Hp;
                if (dyingNow) { int ci = i; bool fled = st.Enemies[i].Fled == true; Tween.After(0.6f, () => KillEnemy(g, ci, fled)); }
                else if (prevCount > 0 && i >= prevCount && alive)
                {   // 登場 (召喚・分裂・孵化の子。2026-09-17): 小さく現れて弾んで等身大に、足元に青緑の輪
                    var sprRt = pan.Find("sprite") as RectTransform;
                    if (sprRt != null)
                    {
                        var origin = sprRt.anchoredPosition; float h = sprRt.rect.height; float pivotY = sprRt.pivot.y;
                        var srt = sprRt;
                        Tween.Run(0.45f, k => { if (srt == null) return; float sc = 0.2f + 0.8f * Tween.Apply(Ease.OutBack, k); srt.localScale = new Vector3(sc, sc, 1f); srt.anchoredPosition = origin + new Vector2(0f, -h * pivotY * (1f - sc)); }, Ease.Linear, () => { if (srt != null) { srt.localScale = Vector3.one; srt.anchoredPosition = origin; } });
                        if (g.FxLayer != null) Tween.RingBurst(g.FxLayer, Tween.CenterIn(sprRt, g.FxLayer) + new Vector2(0f, -h * 0.4f), PaperFx.Mana, 200f, 0.5f);
                    }
                }
            }
            // リーダー
            if (_playerArea == null)
            {
                _playerArea = UiKit.NewRect("player", FieldLayer);
                _shownPlayerHp = st.Player.Hp;
            }
            {
                // リーダーの足元 (欄の左下から +130,+130) を舞台の座席へ
                var feet = Stage.ProjectFeet("player", Stage.LeaderSlot());
                UiKit.Anchor(_playerArea, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(feet.x - 130f, StatusLineY), new Vector2(feet.x - 130f + 690f, StatusLineY + 720f));
                Stage.SetFeetOffset("player", feet.y - StatusLineY);
            }
            for (int c = _playerArea.childCount - 1; c >= 0; c--) { var ch = _playerArea.GetChild(c); ch.SetParent(null, false); UnityEngine.Object.Destroy(ch.gameObject); }
            g.RegisterAnchor("player", _playerArea);
            BattleScreen.FillPlayerPanel(g, _playerArea, st, _shownPlayerHp);
            _shownPlayerHp = st.Player.Hp;
            // からくりの匣 (2026-09-10 世界観「からくりだけ実物」): 舞台のリーダーの足元。仕込み札があれば蓋が開き、動かした (ReactionTriggered) 直後は閃く
            bool fired = false;
            for (int i = _boxLogSeen; i < st.EventLog.Count; i++) if (st.EventLog[i] is GameEvent_ReactionTriggered) fired = true;
            _boxLogSeen = st.EventLog.Count;
            Stage.SetKarakuriBox(st.Player.SetCards.Count, fired);
            SyncDanger(st);
        }

        /// <summary>名前札・HPバーの線 (入れ物の下端)。手札の上端 (約290) のすぐ上。スマホは等倍の札の上端 (14+290) に合わせる</summary>
        public static float StatusLineY { get { return UiKit.Phone ? BattleScreen.HandY + CardView.H * BattleScreen.CardScale + 6f : 300f; } }

        /// <summary>手札 UI に残っている札の数 (自動操作の検証用)</summary>
        public int HandCount { get { return _hand.Count; } }

        /// <summary>敵の入れ物 (帳面の一行の x を確認の窓が読む)。無ければ null</summary>
        public RectTransform EnemyPanel(int index)
        {
            return index >= 0 && index < _enemyPanels.Count ? _enemyPanels[index] : null;
        }

        /// <summary>隣の敵との間隔 (帳面の一行の幅)。無ければ無限大</summary>
        public float EnemyGap(int index)
        {
            return index >= 0 && index < _enemyGaps.Length ? _enemyGaps[index] : float.MaxValue;
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

        /// <summary>予測に使う敵: 狙いを付けた敵、無ければ生存が1体の時だけその敵 (SyncHand と同じ規則)</summary>
        public static int PreviewTargetFor(GameRoot g, GameState st)
        {
            int alive = 0, firstAlive = -1;
            for (int i = 0; i < st.Enemies.Count; i++) if (st.Enemies[i].Hp > 0) { alive++; if (firstAlive < 0) firstAlive = i; }
            return g.PreferredTarget >= 0 && g.PreferredTarget < st.Enemies.Count && st.Enemies[g.PreferredTarget].Hp > 0 ? g.PreferredTarget : (alive == 1 ? firstAlive : -1);
        }

        /// <summary>ドラッグ中に敵の上へ来た/離れた時、その札だけ描き直す (本家と同じく敵に当てた時に数字が変わる。2026-09-09)</summary>
        public void RefreshHandCard(GameRoot g, GameState st, HandCard hc, int previewEnemy)
        {
            int preview = previewEnemy >= 0 ? previewEnemy : PreviewTargetFor(g, st);
            if (hc.Preview == preview) return;
            hc.Preview = preview;
            CardView.PreviewEnemy = preview;
            try { CardView.Refill(hc.Rt, hc.Card, st, hc.Playable, true); }
            finally { CardView.PreviewEnemy = -1; }
        }

        public void SyncHand(GameRoot g, GameState st, bool animate)
        {
            var hand = st.Player.Hand;
            int n = hand.Count;
            // 扇の幅: スマホはエナジーの円盤とターン終了の間 (キャンバス幅 − 560) に収める (2026-09-14)
            float handSpan = UiKit.Phone ? BattleScreen.CanvasSize(Root).x - 560f : 1180f;
            float spacing = n > 0 ? Mathf.Min(CardView.W * BattleScreen.CardScale + 12f, handSpan / n) : 0f;
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
                if (!fresh && (hc.Playable != playable || hc.Settable != settable || !ReferenceEquals(hc.Card.Def, c.Def) || hc.Card.GrowBonus != c.GrowBonus || hc.Cost != cost || hc.Preview != preview))
                {
                    // 見た目が変わる (プレイ可否・鍛え・育つ・コスト・狙った敵) → 同じ位置で作り直す
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
                    hc = new HandCard { Rt = rt2, Card = c, Playable = playable, Settable = settable, Cost = cost, Preview = preview };
                    _hand[c.Uid] = hc;
                    fresh = false;
                    Attach(g, hc, c);
                }
                if (fresh)
                {
                    CardView.PreviewEnemy = preview;
                    var rt = CardView.Build(HandLayer, c, st, playable, true, "hand" + i);
                    CardView.PreviewEnemy = -1;
                    hc = new HandCard { Rt = rt, Card = c, Playable = playable, Settable = settable, Cost = cost, Preview = preview };
                    _hand[c.Uid] = hc;
                    Attach(g, hc, c);
                    if (animate)
                    {
                        rt.anchoredPosition = drawPos;
                        rt.localScale = Vector3.one * 0.35f;
                        rt.localRotation = Quaternion.Euler(0f, 0f, -20f);
                        CardBack(rt);   // めくり (2026-09-17 ⑦): 山札から飛ぶ間は裏、途中で表に返る
                    }
                    newCount++;
                }
                hc.Card = c;
                hc.Index = i;
                hc.Rt.name = "hand" + i;
                float dx = (i - center) * spacing;
                float dy = -Mathf.Abs(i - center) * 10f;
                if (myTurn && g.Pending == null && !playable && !settable) dy -= 16f;   // ⑦ (2026-09-17): 出せない札 (エナジー不足など) は扇の中で少し沈む
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
                    if (isNew)
                    {
                        // めくり (2026-09-17 ⑦): 裏のまま飛び出し、道中で横幅が 0 まで細くなって表に返り、扇の位置で等身大に
                        Audio.Key("CardsDrawn");
                        var p0 = rtc.anchoredPosition; var r0 = rtc.localRotation; float s0 = rtc.localScale.y;
                        var back = rtc.Find("back");
                        Tween.Run(0.34f, t =>
                        {
                            if (rtc == null) return;
                            float e = Tween.Apply(Ease.OutCubic, t);
                            rtc.anchoredPosition = Vector2.LerpUnclamped(p0, target, e);
                            float sc = Mathf.Lerp(s0, BattleScreen.CardScale, Tween.Apply(Ease.OutQuad, t));
                            float flip = Mathf.Abs(Mathf.Cos(t * Mathf.PI));
                            rtc.localScale = new Vector3(sc * Mathf.Max(0.03f, flip), sc, 1f);
                            rtc.localRotation = Quaternion.Slerp(r0, rot, Tween.Apply(Ease.OutQuad, t));
                            if (back != null && t >= 0.5f) { UnityEngine.Object.Destroy(back.gameObject); back = null; }
                        }, Ease.Linear, () => { if (rtc != null) { rtc.localScale = Vector3.one * BattleScreen.CardScale; rtc.anchoredPosition = target; rtc.localRotation = rot; } var b2 = rtc != null ? rtc.Find("back") : null; if (b2 != null) UnityEngine.Object.Destroy(b2.gameObject); });
                        return;
                    }
                    Tween.Move(rtc, target, 0.2f, Ease.OutCubic);
                    Tween.Scale(rtc, Vector3.one * BattleScreen.CardScale, 0.2f, Ease.OutQuad);
                    var rr0 = rtc.localRotation;
                    Tween.Run(0.2f, t => { if (rtc != null) rtc.localRotation = Quaternion.Slerp(rr0, rot, t); }, Ease.OutQuad);
                });
            }
            LastPlayedUid = null;
            LastPlayedTarget = -1;
        }

        readonly HashSet<int> _died = new HashSet<int>();   // この戦闘で崩した (消した) 敵の番号

        /// <summary>
        /// 撃破・逃走 (2026-09-17 ユーザー「倒した敵は消えるようにしたほうが良くない？」): Presenter が EnemyDied/EnemyFled の出来事で呼ぶ (着弾の後)。
        /// 撃破＝白く光り、ドットが頭から崩れて消える (StageUnit の _Dissolve)。崩れる間、体から光の粒が立ちのぼる。逃走＝右へ走りながら崩れる。
        /// 帳面は薄れて消え、最後に絵と帳面を捨てる (以後の組み直しでは描かない)。同じ敵に二度は効かない
        /// </summary>
        public void KillEnemy(GameRoot g, int index, bool fled)
        {
            if (_died.Contains(index)) return;
            _died.Add(index);
            var pan = index >= 0 && index < _enemyPanels.Count ? _enemyPanels[index] : null;
            if (pan == null) return;
            var sprRt = pan.Find("sprite") as RectTransform;
            var stripRt = pan.Find("strip") as RectTransform;
            var tagRt = pan.Find("intent-tag") as RectTransform;
            if (tagRt != null) UnityEngine.Object.Destroy(tagRt.gameObject);
            if (index < _enemyHits.Count && _enemyHits[index] != null) { _enemyHits[index].raycastTarget = false; var b = _enemyHits[index].GetComponent<Button>(); if (b != null) b.interactable = false; }
            Audio.Key(fled ? "EnemyFled" : "EnemyDied");
            string key = "enemy" + index;
            var fx = g.FxLayer;
            var srt = sprRt;
            if (fled)
            {   // 逃走: 奥へ走り去る = 右へ滑って崩れて消える
                if (srt != null)
                {
                    Tween.Move(srt, srt.anchoredPosition + new Vector2(420f, 40f), 0.55f, Ease.InQuad);
                    Tween.Run(0.55f, k => Stage.Dissolve(key, k), Ease.InQuad, () => { if (srt != null) UnityEngine.Object.Destroy(srt.gameObject); });
                }
                FadeOutStrip(stripRt, 0.1f, 0.4f);
                return;
            }
            float h = srt != null ? srt.rect.height : 200f;
            float dur = h > 300f ? 0.95f : 0.62f;   // ボス (384) は長めに
            if (srt != null)
            {
                Stage.Flash(key, 0.22f);
                Tween.After(0.12f, () =>
                {
                    if (srt == null) return;
                    var origin = srt.anchoredPosition;
                    Tween.Run(dur, k =>
                    {
                        if (srt == null) return;
                        Stage.Dissolve(key, k);
                        srt.anchoredPosition = origin + new Vector2(0f, -10f * k);
                    }, Ease.Linear, () => { if (srt != null) UnityEngine.Object.Destroy(srt.gameObject); });
                    // 光の粒 (紙色と真鍮) が体から立ちのぼる
                    if (fx != null)
                    {
                        var c = Tween.CenterIn(srt, fx); float w = srt.rect.width * 0.35f, hh = srt.rect.height * 0.45f;
                        int n = h > 300f ? 22 : 12;
                        for (int m = 0; m < n; m++)
                        {
                            float dl = dur * 0.8f * (m / (float)n);
                            var p0 = c + new Vector2(UnityEngine.Random.Range(-w, w), UnityEngine.Random.Range(-hh, hh));
                            bool brass = m % 3 == 0;
                            Tween.After(dl, () => Mote(fx, p0, brass ? PaperFx.BrassLight : PaperFx.Paper));
                        }
                    }
                });
            }
            FadeOutStrip(stripRt, 0.15f, 0.45f);
        }

        /// <summary>光の粒: ゆらゆら上がって薄れる</summary>
        static void Mote(RectTransform fx, Vector2 p0, Color color)
        {
            if (fx == null) return;
            var rt = UiKit.NewRect("mote", fx);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            float sz = UnityEngine.Random.Range(10f, 22f);
            rt.sizeDelta = new Vector2(sz, sz); rt.anchoredPosition = p0;
            var img = rt.gameObject.AddComponent<Image>(); img.sprite = ThemeFx.Glow(); img.color = color; img.raycastTarget = false;
            float rise = UnityEngine.Random.Range(50f, 110f), sway = UnityEngine.Random.Range(-18f, 18f), dur = UnityEngine.Random.Range(0.5f, 0.8f), ph = UnityEngine.Random.value * 6.28f;
            Tween.Run(dur, k => { if (rt == null) return; rt.anchoredPosition = p0 + new Vector2(sway * Mathf.Sin(k * 3f + ph), rise * k); img.color = new Color(color.r, color.g, color.b, 1f - k * k); rt.localScale = Vector3.one * (1f - 0.4f * k); }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        /// <summary>帳面を薄れさせて捨てる</summary>
        static void FadeOutStrip(RectTransform strip, float delay, float dur)
        {
            if (strip == null) return;
            var cg = strip.GetComponent<CanvasGroup>();
            if (cg == null) cg = strip.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            var srt = strip;
            Tween.After(delay, () => { if (srt == null) return; Tween.Run(dur, k => { if (cg != null) cg.alpha = 1f - k; }, Ease.Linear, () => { if (srt != null) UnityEngine.Object.Destroy(srt.gameObject); }); });
        }

        /// <summary>札の裏 (めくりの前半だけ見える): 夜色の紙にからくりの印。表の上に重ねる</summary>
        static void CardBack(RectTransform card)
        {
            var back = UiKit.NewRect("back", card);
            UiKit.Stretch(back, 0f, 0f, 0f, 0f);
            var sheet = PaperFx.Sheet(back, PaperFx.Card, "paper", PaperFx.Window);
            UiKit.Stretch(sheet.rectTransform, 0f, 0f, 0f, 0f); sheet.raycastTarget = false;
            // 真鍮の細い枠 (上下左右の4本)
            foreach (var side in new[] { 0, 1, 2, 3 })
            {
                var ln = UiKit.NewRect("frame", back);
                if (side < 2) UiKit.Anchor(ln, new Vector2(0f, side), new Vector2(1f, side), new Vector2(10f, side == 0 ? 10f : -12f), new Vector2(-10f, side == 0 ? 12f : -10f));
                else UiKit.Anchor(ln, new Vector2(side - 2, 0f), new Vector2(side - 2, 1f), new Vector2(side == 2 ? 10f : -12f, 10f), new Vector2(side == 2 ? 12f : -10f, -10f));
                var li = ln.gameObject.AddComponent<Image>(); li.color = new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.8f); li.raycastTarget = false;
            }
            var em = UiKit.Icon(back, "star", 84f, new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.55f));
            em.rectTransform.anchorMin = em.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            em.rectTransform.sizeDelta = new Vector2(84f, 84f); em.rectTransform.anchoredPosition = Vector2.zero;
            back.SetAsLastSibling();
        }

        // ---- HP 危険域 (2026-09-17 ⑪) ----
        RectTransform _danger; Image _dangerImg; float _dangerLevel;   // 0=無し 1=3割以下 2=致死級

        /// <summary>HP が 3 割以下なら舞台の縁が薔薇色に脈打つ (致死級の被ダメ予測なら速く強く)。紙の UI には掛けない</summary>
        void SyncDanger(GameState st)
        {
            var p = st.Player;
            float ratio = p.MaxHp > 0 ? (float)p.Hp / p.MaxHp : 1f;
            int incoming = 0; try { incoming = Effects.IncomingTotal(st); } catch (Exception) { }
            bool lethal = incoming > 0 && p.Hp - Math.Max(0, incoming - p.Block) <= 0 && st.HideIntents != true;
            float level = lethal ? 2f : ratio <= 0.3f ? 1f : 0f;
            if (level <= 0f)
            {
                if (_danger != null) { var d = _danger; var di = _dangerImg; _danger = null; _dangerImg = null; Tween.Run(0.4f, k => { if (di != null) di.color = new Color(di.color.r, di.color.g, di.color.b, di.color.a * (1f - k)); }, Ease.Linear, () => { if (d != null) UnityEngine.Object.Destroy(d.gameObject); }); }
                _dangerLevel = 0f;
                return;
            }
            if (_danger == null)
            {
                _danger = UiKit.NewRect("danger", FieldLayer);
                UiKit.Stretch(_danger, -40f, -40f, -40f, -40f);
                _dangerImg = _danger.gameObject.AddComponent<Image>();
                _dangerImg.sprite = ThemeFx.Vignette(PaperFx.Rose, "vignette-rose"); _dangerImg.type = Image.Type.Simple; _dangerImg.preserveAspect = false; _dangerImg.raycastTarget = false;
                _dangerImg.color = new Color(1f, 1f, 1f, 0f);
                var pulse = _danger.gameObject.AddComponent<DangerPulse>();
                pulse.Img = _dangerImg;
            }
            _danger.SetAsLastSibling();
            var pl = _danger.GetComponent<DangerPulse>();
            if (pl != null) { pl.Level = level; }
            _dangerLevel = level;
        }

        /// <summary>縁の脈動: 3割以下は 0.9Hz でゆっくり、致死級は 1.7Hz で強く</summary>
        class DangerPulse : MonoBehaviour
        {
            public Image Img; public float Level = 1f; float _t;
            void Update()
            {
                if (Img == null) return;
                _t += Time.unscaledDeltaTime;
                float hz = Level >= 2f ? 1.7f : 0.9f;
                float baseA = Level >= 2f ? 0.55f : 0.32f, amp = Level >= 2f ? 0.3f : 0.16f;
                float a = baseA + amp * Mathf.Sin(_t * hz * Mathf.PI * 2f);
                var c = Img.color; Img.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
            }
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

            if (inSet) { Audio.Key("CardSet"); FlyTo(g, rt, "setslot" + setIdx, delay, 0.3f, true, 0.5f); return; }
            if (inPerm) { FlyTo(g, rt, "player", delay, 0.3f, true, 0.4f); return; }
            if (uid == LastPlayedUid)
            {
                // プレイ: 対象の敵 (無ければ中央上) へ飛んでから、消滅なら砕け、それ以外は捨て札へ
                var fx = HandLayer;
                Vector2 to = new Vector2(0f, 420f);
                var target = LastPlayedTarget >= 0 ? g.Anchor("enemy" + LastPlayedTarget) : null;
                if (target != null) to = Tween.CenterIn(target, fx) + new Vector2(0f, 40f);
                Audio.Key("CardPlayed");
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
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();   // ?? は不可: エディタでは無いコンポーネントが例外を投げる null オブジェクトで返る
            cg.blocksRaycasts = false;
            Tween.Scale(rt, Vector3.one * 0.2f, 0.3f, Ease.InQuad);
            Tween.Run(0.3f, k => { if (cg != null) cg.alpha = 1f - k; }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        void FlyTo(GameRoot g, RectTransform rt, string anchor, float delay, float dur, bool destroy, float endScale = 0.3f)
        {
            var dest = g.Anchor(anchor);
            Vector2 to = dest != null ? Tween.CenterIn(dest, HandLayer) : new Vector2(900f, -100f);
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();   // ?? は不可: エディタでは無いコンポーネントが例外を投げる null オブジェクトで返る
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
