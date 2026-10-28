// ScreenFx.cs — 画面の切り替えと動き (品質スプリント #2・2026-10-29 `docs/design/quality-sprint-2026-10-28.md` §9)。
// 絵を足さず、全画面に一律で効く「動きの文法 5 条」をここ一箇所で持つ:
//   条1 幕   = フェーズが変わる手は夜色の幕が閉じ (0.16)・組み直し・開く (0.22)。重ね窓は幕を使わない (今まで通り瞬時)
//   条2 入り = 画面の部品 (ScreenRoot の直下の子) を組んだ順に 40ms ずつずらし、16px 下から上がりながら現れる (0.28・OutCubic)。点で置かれた物 (札・ボタン) は 0.92→1 も重ねる
//   条3 反応 = 押せる物はホバーで 1.03、押すと 0.96 (UiKit.Btn が全ボタンに ButtonMotion を付ける)。地図の節は押すと 1.15 に膨らんで灯が広がってから幕
//   条4 数字と札 = 上部バーの値が変わったら札が跳ねて差分の浮き文字。G は数え上げ。札・レリックを取ると上部バーの印へ縮みながら飛ぶ
//   条5 呼吸 = 決定のボタン (真鍮) だけ 1.2Hz で縁と大きさが脈打つ (戦闘のターン終了と同じ作法)。地図に戻った時は前の行から今の行へ 0.5 秒
// 秒数はユーザー裁定「表の通り」(§9-6)。自動操縦の撮影 (shots) では幕を飛ばす (Instant)。一周の動画 (loop) だけ動かす
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public class ScreenFx : MonoBehaviour
    {
        // ---- 秒 (§9-3 の表) ----
        public const float CloseDur = 0.16f, OpenDur = 0.22f;
        public const float EnterDur = 0.28f, EnterStep = 0.04f, EnterRise = 16f, EnterScale = 0.92f;
        public const float HoverDur = 0.12f, PressDur = 0.06f, FlyDur = 0.35f, NodeDur = 0.25f, CountDur = 0.4f, MapScrollDur = 0.5f;
        public const float PulseHz = 1.2f;

        /// <summary>幕と飛びを飛ばす (自動操縦の撮影。Autopilot が loop 以外で立てる)</summary>
        public static bool Instant;
        /// <summary>幕が動いている (Autopilot.Shot は開き切るまで待つ)</summary>
        public static bool Busy { get { return _busy; } }
        /// <summary>地図に戻る手 (GameRoot.Do が立て、MapScreen が読んで消す): 前の行から今の行へスクロール</summary>
        public static bool MapScrollFromPrev;

        static bool _busy;
        static ScreenFx _i;
        static Image _curtain;
        static Action _queued;   // 幕が閉じている間に来た次の組み直し

        static ScreenFx I
        {
            get
            {
                if (_i == null)
                {
                    var go = new GameObject("ScreenFx");
                    DontDestroyOnLoad(go);
                    _i = go.AddComponent<ScreenFx>();
                }
                return _i;
            }
        }

        static float Dt { get { return Tween.UnscaledDt; } }

        // ---- 条1 幕 ----

        /// <summary>幕の板 (キャンバス直下の最前面)。GameRoot が層を作った後に呼ぶ</summary>
        public static void EnsureCurtain(Transform canvas)
        {
            if (_curtain != null) return;
            var rt = UiKit.NewRect("curtain", canvas);
            UiKit.Stretch(rt, -4f, -4f, -4f, -4f);   // 端の 1px の隙間を出さない
            _curtain = rt.gameObject.AddComponent<Image>();
            _curtain.color = new Color(PaperFx.Night.r, PaperFx.Night.g, PaperFx.Night.b, 0f);
            _curtain.raycastTarget = false;
        }

        static void SetCurtain(float a)
        {
            if (_curtain == null) return;
            var c = _curtain.color; _curtain.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
        }

        /// <summary>入力を塞ぐ (幕は透明のまま)。飛びや節の膨らみの間</summary>
        public static void LockInput(bool on)
        {
            if (_curtain == null) return;
            if (!on && _busy) return;   // 幕が動いている間は幕が握る
            _curtain.raycastTarget = on;
        }

        /// <summary>フェーズが変わる手: 幕を閉じ切ってから rebuild・入りの動きを付けて開く。Instant や幕が無い時はその場で rebuild</summary>
        public static void Switch(GameRoot g, Action rebuild, bool enter = true)
        {
            if (Instant || _curtain == null || g == null) { rebuild(); return; }
            if (_busy) { _queued = rebuild; return; }   // 閉じている途中: 閉じ切った時にまとめて組む
            I.StartCoroutine(I.SwitchCo(g, rebuild, enter));
        }

        IEnumerator SwitchCo(GameRoot g, Action rebuild, bool enter)
        {
            _busy = true;
            _curtain.raycastTarget = true;
            _curtain.transform.SetAsLastSibling();
            FinishEntries();
            float a0 = _curtain.color.a;
            float dur = CloseDur * (1f - a0), t = 0f;
            while (t < dur) { SetCurtain(Mathf.Lerp(a0, 1f, Tween.Apply(Ease.InQuad, t / dur))); t += Dt; yield return null; }
            SetCurtain(1f);
            try { rebuild(); } catch (Exception e) { Debug.LogException(e); }
            while (_queued != null) { var q = _queued; _queued = null; try { q(); } catch (Exception e) { Debug.LogException(e); } }
            yield return null;   // 幕の下でレイアウトを 1 フレーム落ち着かせる (LayoutGroup は組んだフレームでは未計算)
            if (enter && g.ScreenRoot != null && !(g.Rs != null && g.Rs.Phase == DeckRogue.Engine.Generated.RunPhases.Combat)) BeginEntries(g.ScreenRoot);
            t = 0f;
            while (t < OpenDur)
            {
                if (_queued != null)
                {   // 開いている途中に次の手が来た (自動操縦など): まだ幕の下なのでその場で組み直して開き続ける
                    var q = _queued; _queued = null;
                    try { q(); } catch (Exception e) { Debug.LogException(e); }
                    if (enter && g.ScreenRoot != null && !(g.Rs != null && g.Rs.Phase == DeckRogue.Engine.Generated.RunPhases.Combat)) BeginEntries(g.ScreenRoot);
                }
                SetCurtain(1f - Tween.Apply(Ease.OutQuad, t / OpenDur)); t += Dt; yield return null;
            }
            SetCurtain(0f);
            _curtain.raycastTarget = false;
            _busy = false;
            if (_queued != null) { var q = _queued; _queued = null; Switch(g, q, enter); }
        }

        // ---- 条2 入り ----

        class Entry
        {
            public RectTransform Rt; public CanvasGroup Cg; public bool OwnCg, Move, Scale;
            public float Alpha, Delay; public Vector2 Pos; public Vector3 Scale0;
        }
        readonly List<Entry> _entries = new List<Entry>();
        float _entryT;

        /// <summary>root の直下の子を順に現す。全面に広がる物 (暗幕・地図の窓) はフェードだけ、横に伸びる物 (上部バー・見出し) は上がりながら、点で置かれた物 (札・ボタン) は 0.92→1 も</summary>
        public static void BeginEntries(RectTransform root)
        {
            if (Instant || root == null) return;
            var me = I;
            me.FinishEntriesNow();
            int k = 0;
            for (int i = 0; i < root.childCount; i++)
            {
                var rt = root.GetChild(i) as RectTransform;
                if (rt == null || !rt.gameObject.activeSelf) continue;
                bool full = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one;
                bool point = rt.anchorMin == rt.anchorMax;
                var cg = rt.GetComponent<CanvasGroup>();
                bool own = cg == null;
                if (own) cg = rt.gameObject.AddComponent<CanvasGroup>();
                var e = new Entry { Rt = rt, Cg = cg, OwnCg = own, Move = !full, Scale = point, Alpha = cg.alpha, Pos = rt.anchoredPosition, Scale0 = rt.localScale };
                e.Delay = full ? 0f : Mathf.Min(k, 10) * EnterStep;
                if (!full) k++;
                cg.alpha = 0f;
                if (e.Move) rt.anchoredPosition = e.Pos - new Vector2(0f, EnterRise);
                if (e.Scale) rt.localScale = e.Scale0 * EnterScale;
                me._entries.Add(e);
            }
            me._entryT = 0f;
        }

        /// <summary>押したら待たせない: 入りの途中を終わりの姿へ</summary>
        public static void FinishEntries() { if (_i != null) _i.FinishEntriesNow(); }

        void FinishEntriesNow()
        {
            foreach (var e in _entries) Apply(e, 1f, true);
            _entries.Clear();
        }

        static void Apply(Entry e, float u, bool final)
        {
            if (e.Rt == null) return;
            float k = Tween.Apply(Ease.OutCubic, Mathf.Clamp01(u));
            if (e.Cg != null) e.Cg.alpha = Mathf.Lerp(0f, e.Alpha, Mathf.Clamp01(u * 1.6f));
            if (e.Move) e.Rt.anchoredPosition = e.Pos - new Vector2(0f, EnterRise * (1f - k));
            if (e.Scale) e.Rt.localScale = e.Scale0 * Mathf.Lerp(EnterScale, 1f, k);
            if (final)
            {
                if (e.Cg != null) e.Cg.alpha = e.Alpha;
                if (e.Move) e.Rt.anchoredPosition = e.Pos;
                if (e.Scale) e.Rt.localScale = e.Scale0;
                if (e.OwnCg && e.Cg != null) Destroy(e.Cg);
            }
        }

        void Update()
        {
            if (_entries.Count == 0) return;
            bool tap = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            if (tap) { FinishEntriesNow(); return; }
            _entryT += Dt;
            bool all = true;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                if (e.Rt == null) { _entries.RemoveAt(i); continue; }
                float u = (_entryT - e.Delay) / EnterDur;
                if (u >= 1f) { Apply(e, 1f, true); _entries.RemoveAt(i); continue; }
                all = false;
                Apply(e, u, false);
            }
            if (all) _entries.Clear();
        }

        // ---- 条3 反応 ----

        /// <summary>
        /// ボタンの手応え (UiKit.Btn が全ボタンに付ける): ホバーで 1.03 (0.12 秒)、押すと 0.96 (0.06 秒)。スマホはホバーが無いので押しだけ。
        /// Pulse (決定のボタン＝真鍮) は 1.2Hz で 1.00↔1.03 と真鍮の縁が脈打つ (戦闘のターン終了の脈と同じ作法。ターン終了自身は EndTurnPulse が持つので触らない)。
        /// 大きさは基準 × ホバー × 押し × 脈 を毎フレーム書く (Tween と取り合わない)。動いていない時に外から変えられた大きさは新しい基準として取り込む
        /// </summary>
        public class ButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
        {
            public bool Pulse;
            RectTransform _rt; Button _btn; Vector3 _base = Vector3.one; float _hover, _press, _t; bool _in, _down; Outline _rim; bool _checkedEndTurn, _skip;

            void Awake() { _rt = (RectTransform)transform; _btn = GetComponent<Button>(); _base = _rt.localScale; }
            public void OnPointerEnter(PointerEventData d) { _in = true; if (!UiKit.Phone && Active) Audio.Hover(); }
            public void OnPointerExit(PointerEventData d) { _in = false; _down = false; }
            public void OnPointerDown(PointerEventData d) { _down = true; }
            public void OnPointerUp(PointerEventData d) { _down = false; }
            bool Active { get { return _btn == null || _btn.interactable; } }

            void Update()
            {
                if (!_checkedEndTurn) { _checkedEndTurn = true; _skip = GetComponent<BattleScreen.EndTurnPulse>() != null; }
                if (_skip) return;
                float dt = Dt;
                bool active = Active;
                float hv = (_in && active && !UiKit.Phone) ? 1f : 0f, pr = (_down && active) ? 1f : 0f;
                bool idle = _hover == 0f && _press == 0f && !Pulse;
                if (idle) _base = _rt.localScale;   // 外からの変更を基準に
                _hover = Mathf.MoveTowards(_hover, hv, dt / HoverDur);
                _press = Mathf.MoveTowards(_press, pr, dt / PressDur);
                float s = 1f + 0.03f * _hover - 0.07f * _press;
                if (Pulse && active && !Instant)
                {
                    _t += dt;
                    float live = _t - 0.3f;   // 入りの途中は脈打たない
                    float w = live <= 0f ? 0f : 0.5f - 0.5f * Mathf.Cos(live * PulseHz * Mathf.PI * 2f);
                    s *= 1f + 0.03f * w;
                    if (_rim == null) _rim = Rim();
                    if (_rim != null) { var c = _rim.effectColor; _rim.effectColor = new Color(c.r, c.g, c.b, (0.35f + 0.55f * w) * Mathf.Clamp01(live / 0.2f)); }
                }
                else if (_rim != null) { var c = _rim.effectColor; _rim.effectColor = new Color(c.r, c.g, c.b, 0f); }
                var want = _base * s;
                if (_rt.localScale != want) _rt.localScale = want;
            }

            /// <summary>ボタンの 5 外の真鍮の縁。子の絵は面の上に描かれて面を覆うので、ボタンの面 (Image) の Outline (面の写しを 4 方向へずらして後ろに描く) で縁を作る。
            /// レイアウトの子でも位置を汚さない (兄弟を増やさない)</summary>
            Outline Rim()
            {
                var img = GetComponent<Image>();
                if (img == null) return null;
                var o = img.gameObject.GetComponent<Outline>();
                if (o == null) o = img.gameObject.AddComponent<Outline>();
                o.effectDistance = new Vector2(5f, 5f);
                o.useGraphicAlpha = false;
                o.effectColor = new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0f);
                return o;
            }
        }

        /// <summary>決定のボタン (真鍮): 条5 の脈を付ける。UiKit.Btn の後に呼ぶ</summary>
        public static void Primary(Button b)
        {
            if (b == null) return;
            var m = b.GetComponent<ButtonMotion>();
            if (m != null) m.Pulse = true;
        }

        /// <summary>地図の節を押した: 1.15 に膨らんで灯が広がり (0.25 秒)、それから then (幕へ)</summary>
        public static void NodeChosen(GameRoot g, RectTransform cell, Action then)
        {
            if (Instant || cell == null || g == null || g.FxLayer == null) { then(); return; }
            LockInput(true);
            Tween.Scale(cell, cell.localScale * 1.15f, NodeDur, Ease.OutBack);
            Tween.RingBurst(g.FxLayer, Tween.CenterIn(cell, g.FxLayer), PaperFx.Ember, 240f, 0.45f);
            Tween.After(NodeDur, () => { LockInput(false); then(); });
        }

        // ---- 条4 数字と札 ----

        static readonly Dictionary<string, int> _shown = new Dictionary<string, int>();

        /// <summary>新しいラン・続きから・タイトルへ: 前のランの値と比べない</summary>
        public static void ResetShown() { _shown.Clear(); }

        /// <summary>
        /// 上部バーの値 (RunUi.TopBar が毎回呼ぶ)。前に見せた値と違えば札が跳ね (Punch .06)、差分の浮き文字 (+15 G / −8)。
        /// count=true (G) は数字を 0.4 秒で数え上げる。down (HP が減った) は札が薔薇に一瞬染まる。初めての表示は覚えるだけ
        /// </summary>
        public static void Shown(GameRoot g, string key, int value, RectTransform tag, TMP_Text text, string unit, bool count)
        {
            int old;
            if (!_shown.TryGetValue(key, out old)) { _shown[key] = value; return; }
            _shown[key] = value;
            if (old == value || Instant || tag == null || g == null || g.FxLayer == null) return;
            int d = value - old;
            Tween.Punch(tag, 0.06f, 0.3f);
            var fx = g.FxLayer;
            bool up = d > 0;
            Color col = key == "hp" ? (up ? PaperFx.MossLight : PaperFx.RoseLight) : key == "gold" ? PaperFx.BrassLight : PaperFx.Paper;
            string label = (up ? "+" : "−") + Mathf.Abs(d) + unit;
            // 浮き文字は札の位置が決まってから (上部バーは HorizontalLayoutGroup＝組んだフレームの終わりに並ぶ。組んだ直後の CenterIn は左上を指す)
            Tween.After(0.05f, () => { if (tag != null && fx != null) Tween.Float(fx, Tween.CenterIn(tag, fx) + new Vector2(0f, -30f), label, col, 24, -36f, 1.0f, 200f); });   // 札の下へ落ちる (上は画面の端)
            if (key == "hp" && !up) { var img = tag.GetComponent<Image>(); if (img != null) Tween.Flash(img, PaperFx.RoseLight, 0.35f); }
            if (count && text != null) I.StartCoroutine(I.CountCo(text, old, value, unit));
        }

        IEnumerator CountCo(TMP_Text text, int from, int to, string unit)
        {
            float t = 0f;
            while (t < CountDur && text != null)
            {
                int v = Mathf.RoundToInt(Mathf.Lerp(from, to, Tween.Apply(Ease.OutQuad, t / CountDur)));
                text.text = v.ToString();
                t += Dt; yield return null;
            }
            if (text != null) text.text = to.ToString();
        }

        /// <summary>札・レリックを取る／買う: from の複製が上部バーの印 (anchor) へ縮みながら飛び (0.35 秒)、着いたら then (手を進める)</summary>
        public static void FlyTo(GameRoot g, RectTransform from, string anchor, Action then)
        {
            var to = g != null ? g.Anchor(anchor) : null;
            var fx = g != null ? g.FxLayer : null;
            if (Instant || from == null || to == null || fx == null) { then(); return; }
            GameObject clone;
            try { clone = UnityEngine.Object.Instantiate(from.gameObject, fx); }
            catch (Exception e) { Debug.LogWarning("[ScreenFx] fly: " + e.Message); then(); return; }
            var crt = clone.GetComponent<RectTransform>();
            var cg = clone.GetComponent<CanvasGroup>(); if (cg == null) cg = clone.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; cg.interactable = false; cg.alpha = 1f;
            foreach (var mb in clone.GetComponentsInChildren<ScreenFx.ButtonMotion>(true)) UnityEngine.Object.Destroy(mb);
            var size = from.rect.size;
            float s0 = fx.lossyScale.x > 0f ? from.lossyScale.x / fx.lossyScale.x : 1f;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = size;
            Vector2 src = Tween.CenterIn(from, fx), dst = Tween.CenterIn(to, fx);
            crt.anchoredPosition = src;
            crt.localScale = Vector3.one * s0;
            crt.localRotation = Quaternion.identity;
            LockInput(true);
            bool done = false;
            Action finish = () =>
            {
                if (done) return; done = true;
                if (clone != null) UnityEngine.Object.Destroy(clone);
                LockInput(false);
                then();
            };
            Tween.Run(FlyDur, k =>
            {
                if (crt == null) return;
                crt.anchoredPosition = Vector2.Lerp(src, dst, k);
                crt.localScale = Vector3.one * (s0 * Mathf.Lerp(1f, 0.12f, k));
                cg.alpha = 1f - 0.35f * k;
            }, Ease.InOutQuad, finish, finish);
        }
    }
}
