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
        // ---- 秒 (§9-3 の表)。2026-11-04: 設定の「速さ」(FxSettings.Speed) で割れるよう const → 静的プロパティ (値は表の通り) ----
        public static float CloseDur { get { return FxSettings.Dur(0.16f); } }
        public static float OpenDur { get { return FxSettings.Dur(0.22f); } }
        public static float EnterDur { get { return FxSettings.Dur(0.28f); } }
        public static float EnterStep { get { return FxSettings.Dur(0.04f); } }
        public const float EnterRise = 16f, EnterScale = 0.92f;
        public const float HoverDur = 0.12f, PressDur = 0.06f;
        public static float FlyDur { get { return FxSettings.Dur(0.35f); } }
        public static float NodeDur { get { return FxSettings.Dur(0.25f); } }
        public static float CountDur { get { return FxSettings.Dur(0.4f); } }
        public static float MapScrollDur { get { return FxSettings.Dur(0.5f); } }
        public const float PulseHz = 1.2f;
        /// <summary>飛びの弧の高さ (TB-05: 直線でなく山なりに) と着地の跳ね</summary>
        public const float FlyArc = 90f;

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
        static float _deferOnce;   // 次の幕が閉じる前に待つ秒 (選んだ後の 1 秒。DeferNextSwitch)
        static bool _holding;      // SwitchCo が「選んだ後の 1 秒」の輪の中にいる
        static bool _cancelHold;   // 輪を次のコマで抜ける (CancelHold)。輪を抜ける時に消費して戻す

        /// <summary>
        /// 選んだ後の 1 秒 (2026-11-04 束 C1): 次の Switch は幕を閉じる前に sec 秒待つ (入力は塞ぐ・タップで飛ばせる)。
        /// 手 (g.Do) の後に出来事の結果・回復の数え上げを「今の画面の上」で見せるための窓。Do の直前に呼び、Switch が始まらなかったら 0 で消す。
        /// Instant では効かない (撮影は止まった画面)。
        /// 注意: Switch の StartCoroutine は最初の yield まで同期に進むので、同じコマの Do の後に 0 で消しても「もう始まった輪」には効かない。
        /// 見せる物が無いと Do の後に分かった時 (出来事→戦闘) は CancelHold で輪を抜ける
        /// </summary>
        public static void DeferNextSwitch(float sec) { _deferOnce = Instant ? 0f : Mathf.Max(0f, sec); }

        /// <summary>
        /// 選んだ後の待ちをやめる (2026-11-04 レビューの是正): 幕を閉じる前の輪 (DeferNextSwitch) が走っていれば次のコマで抜ける。
        /// 輪が無ければ何もしない (旗を次の Switch に漏らさない)。出来事で戦闘へ続く手を選んだ時など「見せる物が無い」と Do の後に分かる場面で呼ぶ
        /// </summary>
        public static void CancelHold() { if (_holding) _cancelHold = true; }

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
            // 選んだ後の 1 秒 (DeferNextSwitch): 幕は透明のまま入力だけ塞いで待つ。0.25 秒を過ぎたらタップで飛ばせる
            float hold = _deferOnce; _deferOnce = 0f;
            if (hold > 0f)
            {
                _holding = true; _cancelHold = false;
                float h = 0f;
                while (h < hold)
                {
                    if (_cancelHold) break;   // 見せる物が無いと分かった (CancelHold)
                    bool tap = h > 0.25f && (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began));
                    if (tap) break;
                    h += Dt; yield return null;
                }
                _holding = false; _cancelHold = false;   // 消費して戻す (次の Switch に漏らさない)
            }
            Audio.Key("paper_swish", 0.7f);   // 幕が閉じる紙の音 (A14)
            float a0 = _curtain.color.a;
            float dur = CloseDur * (1f - a0), t = 0f;
            while (t < dur) { SetCurtain(Mathf.Lerp(a0, 1f, Tween.Apply(Ease.InQuad, t / dur))); t += Dt; yield return null; }
            SetCurtain(1f);
            try { rebuild(); } catch (Exception e) { Debug.LogException(e); }
            while (_queued != null) { var q = _queued; _queued = null; try { q(); } catch (Exception e) { Debug.LogException(e); } }
            yield return null;   // 幕の下でレイアウトを 1 フレーム落ち着かせる (LayoutGroup は組んだフレームでは未計算)
            if (enter && g.ScreenRoot != null && !(g.Rs != null && g.Rs.Phase == DeckRogue.Engine.Generated.RunPhases.Combat)) BeginEntries(g.ScreenRoot);
            Audio.Key("paper_swish", 0.4f);   // 開く紙の音 (A14。閉じる方より小さく)
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
            public float Alpha, Delay, From = EnterScale; public Vector2 Pos; public Vector3 Scale0;
        }
        readonly List<Entry> _entries = new List<Entry>();
        float _entryT;

        /// <summary>入りの重み (2026-11-04 束 C1・G7): この部品だけ入りを extraDelay 秒遅らせ、fromScale から膨らむ (幕ボスの遺物 3 択＝0.1 秒ずつ遅く・大きく)。組んだ直後に呼ぶ</summary>
        public class EntryHint : MonoBehaviour { public float ExtraDelay; public float FromScale = EnterScale; }
        /// <summary>入りの対象外 (自分で現れ方を持つ部品。通知の板など)</summary>
        public class EntrySkip : MonoBehaviour { }

        public static void Weight(RectTransform rt, float extraDelay, float fromScale = 0.86f)
        {
            if (rt == null) return;
            var h = rt.GetComponent<EntryHint>();
            if (h == null) h = rt.gameObject.AddComponent<EntryHint>();
            h.ExtraDelay = extraDelay; h.FromScale = fromScale;
        }

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
                if (rt.GetComponent<EntrySkip>() != null) continue;
                bool full = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one;
                bool point = rt.anchorMin == rt.anchorMax;
                var cg = rt.GetComponent<CanvasGroup>();
                bool own = cg == null;
                if (own) cg = rt.gameObject.AddComponent<CanvasGroup>();
                var e = new Entry { Rt = rt, Cg = cg, OwnCg = own, Move = !full, Scale = point, Alpha = cg.alpha, Pos = rt.anchoredPosition, Scale0 = rt.localScale };
                e.Delay = full ? 0f : Mathf.Min(k, 10) * EnterStep;
                var hint = rt.GetComponent<EntryHint>();
                if (hint != null) { e.Delay += hint.ExtraDelay; e.From = hint.FromScale; }
                if (!full) k++;
                cg.alpha = 0f;
                if (e.Move) rt.anchoredPosition = e.Pos - new Vector2(0f, EnterRise);
                if (e.Scale) rt.localScale = e.Scale0 * e.From;
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
            if (e.Scale) e.Rt.localScale = e.Scale0 * Mathf.Lerp(e.From, 1f, k);
            if (final)
            {
                if (e.Cg != null) e.Cg.alpha = e.Alpha;
                if (e.Move) e.Rt.anchoredPosition = e.Pos;
                if (e.Scale) e.Rt.localScale = e.Scale0;
                if (e.OwnCg && e.Cg != null) Destroy(e.Cg);
            }
        }

        /// <summary>Update を回す実体を用意する (報酬の札の返りはこの Update のタップで飛ばすので、幕が一度も動いていなくても作っておく)</summary>
        public static void EnsureInstance() { var me = I; if (me == null) return; }

        void Update()
        {
            // 報酬の札の返り (ScreenJuice.RevealCard) も同じタップで終わりの姿へ (2026-11-04 レビューの是正: 3 枚目の「取る」が 0.6 秒押せなかった)
            bool reveals = ScreenJuice.HasReveals;
            if (_entries.Count == 0 && !reveals) return;
            bool tap = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            if (tap) { FinishEntriesNow(); if (reveals) ScreenJuice.FinishReveals(); return; }
            if (_entries.Count == 0) return;
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
            // 押せるようになった瞬間の pop (TB-17): interactable の立ち上がりを見て 0.92→1 (OutBack 0.2)。初めの見た目では鳴らない
            bool _wasActive, _seen; float _pop; bool _appeared;
            static int _appearFrame = -1;   // 同じコマに並んだ決定のボタンは 1 回だけ鳴る

            void Awake() { _rt = (RectTransform)transform; _btn = GetComponent<Button>(); _base = _rt.localScale; }
            /// <summary>「もう現れた」と印を付ける (2026-11-04 レビューの是正): 自分で現れ方を持つ側 (報酬の札の返り) が interactable を戻す直前に呼ぶと、立ち上がりの pop と button_appear (脈の側の 1 回も) を鳴らさない</summary>
            public void MarkShown() { _wasActive = true; _appeared = true; _seen = true; }
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
                // 押せなかった物が押せるようになった (TB-17): 小さく pop して button_appear
                if (_seen && active && !_wasActive && !Instant)
                {
                    _pop = 1f; _appeared = true;   // 現れた音はここで (脈の方では鳴らさない)
                    if (_appearFrame != Time.frameCount) { _appearFrame = Time.frameCount; Audio.Key("button_appear", 0.4f); }
                }
                _seen = true; _wasActive = active;
                bool idle = _hover == 0f && _press == 0f && !Pulse && _pop <= 0f;
                if (idle) _base = _rt.localScale;   // 外からの変更を基準に
                _hover = Mathf.MoveTowards(_hover, hv, dt / HoverDur);
                _press = Mathf.MoveTowards(_press, pr, dt / PressDur);
                float s = 1f + 0.03f * _hover - 0.07f * _press;
                if (_pop > 0f)
                {   // 0.92 → 1 (OutBack・0.2 秒)
                    _pop = Mathf.Max(0f, _pop - dt / 0.2f);
                    s *= Mathf.LerpUnclamped(0.92f, 1f, Tween.Apply(Ease.OutBack, 1f - _pop));
                }
                if (Pulse && active && !Instant)
                {
                    _t += dt;
                    float live = _t - 0.3f;   // 入りの途中は脈打たない
                    if (live > 0f && !_appeared)
                    {   // 決定のボタンが現れた (TB-17): 入りが終わる所で小さく一度 (同じコマに並ぶ物は 1 回)
                        _appeared = true;
                        if (_appearFrame != Time.frameCount) { _appearFrame = Time.frameCount; Audio.Key("button_appear", 0.35f); }
                    }
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

        /// <summary>この値はもう見せた (2026-11-04): 焚き火の数え上げ・出来事の結果の札で差分を先に出した時、次の画面の上部バーが同じ差分をもう一度出さないよう覚えさせる</summary>
        public static void Remember(string key, int value) { _shown[key] = value; }

        /// <summary>見せている値 (無ければ null)</summary>
        public static int? ShownValue(string key) { int v; return _shown.TryGetValue(key, out v) ? (int?)v : null; }

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
            // G の数え上げには硬貨の刻み (A6)。買い物 (減る) は小さく＝店の ui.buy と重なる
            if (count && text != null) CountTo(text, old, value, CountDur, key == "gold" ? "coin_land" : null, up ? 0.45f : 0.2f);
        }

        /// <summary>
        /// 数え上げ (TB-03。2026-11-04 公開): text を from→to へ dur 秒で (OutQuad)。tickKey があれば値が変わった時に 0.05 秒に 1 回まで鳴らし、音程を少しずつ上げる (Audio.KeyPitched)。
        /// 終わりに小さく跳ねる。Instant なら即 to
        /// </summary>
        public static void CountTo(TMP_Text text, int from, int to, float dur, string tickKey = null, float tickVol = 0.5f)
        {
            if (text == null) return;
            if (Instant || dur <= 0f || from == to) { text.text = to.ToString(); return; }
            I.StartCoroutine(I.CountCo(text, from, to, dur, tickKey, tickVol));
        }

        IEnumerator CountCo(TMP_Text text, int from, int to, float dur, string tickKey, float tickVol)
        {
            float t = 0f, gate = 0f; int last = from, ticks = 0;
            while (t < dur && text != null)
            {
                int v = Mathf.RoundToInt(Mathf.Lerp(from, to, Tween.Apply(Ease.OutQuad, t / dur)));
                if (v != last)
                {
                    text.text = v.ToString();
                    last = v;
                    if (tickKey != null && gate <= 0f) { Audio.KeyPitched(tickKey, 1f + 0.03f * Mathf.Min(ticks, 12), tickVol); ticks++; gate = 0.05f; }
                }
                gate -= Dt;
                t += Dt; yield return null;
            }
            if (text != null) { text.text = to.ToString(); Tween.Punch(text.rectTransform, 0.08f, 0.2f); }
        }

        /// <summary>
        /// 札・レリックを取る／買う: from の複製が上部バーの印 (anchor) へ弧を描いて縮みながら飛び (0.35 秒)、着いたら印が跳ねて輪と音 (TB-05・G17)、0.1 秒置いて then (手を進める)。
        /// 遺物は青緑の輪と land_relic、札は真鍮の輪と land_card。複製は from の Instantiate (ButtonMotion は外す)
        /// </summary>
        public static void FlyTo(GameRoot g, RectTransform from, string anchor, Action then)
        {
            var to = g != null ? g.Anchor(anchor) : null;
            var fx = g != null ? g.FxLayer : null;
            if (Instant || from == null || to == null || fx == null) { then(); return; }
            var clone = CloneForFx(from, fx);
            if (clone == null) { then(); return; }
            var crt = clone.GetComponent<RectTransform>();
            var cg = clone.GetComponent<CanvasGroup>();
            float s0 = crt.localScale.x;
            Vector2 src = crt.anchoredPosition, dst = Tween.CenterIn(to, fx);
            bool relic = anchor == "relics";
            bool mana = anchor == "mana";
            Color land = relic || mana ? PaperFx.ManaLight : PaperFx.BrassLight;
            LockInput(true);
            bool done = false;
            Action finish = () =>
            {
                if (done) return; done = true;
                if (clone != null) UnityEngine.Object.Destroy(clone);
                // 着地 (G17): 印が跳ね・輪が広がり・音。手を進めるのは 0.1 秒置いて (同じ画面の組み直しでも着地が目に残る)
                if (to != null) Tween.Punch(to, 0.14f, 0.3f);
                if (fx != null) Tween.RingBurst(fx, dst, land, relic ? 120f : 140f, 0.3f);
                Audio.Key(relic ? "land_relic" : "land_card", 0.6f);
                Tween.After(0.1f, () => { LockInput(false); then(); });
            };
            float arc = FlyArc * Mathf.Clamp01(Vector2.Distance(src, dst) / 600f);   // 近い所へは低く
            Tween.Run(FlyDur, k =>
            {
                if (crt == null) return;
                crt.anchoredPosition = Vector2.Lerp(src, dst, k) + new Vector2(0f, arc * Mathf.Sin(k * Mathf.PI));
                crt.localScale = Vector3.one * (s0 * Mathf.Lerp(1f, 0.12f, k));
                crt.localRotation = Quaternion.Euler(0f, 0f, -6f * Mathf.Sin(k * Mathf.PI));
                cg.alpha = 1f - 0.35f * k;
            }, Ease.InOutQuad, finish, finish);
        }

        /// <summary>演出用の複製 (2026-11-04 公開): from を fx 層に Instantiate し、ButtonMotion を外し・raycast を切り・位置と大きさを from の見た目に合わせる (anchor 中央・pivot 中央)。失敗なら null</summary>
        public static GameObject CloneForFx(RectTransform from, RectTransform fx)
        {
            if (from == null || fx == null) return null;
            GameObject clone;
            try { clone = UnityEngine.Object.Instantiate(from.gameObject, fx); }
            catch (Exception e) { Debug.LogWarning("[ScreenFx] clone: " + e.Message); return null; }
            clone.SetActive(true);
            var crt = clone.GetComponent<RectTransform>();
            var cg = clone.GetComponent<CanvasGroup>(); if (cg == null) cg = clone.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; cg.interactable = false; cg.alpha = 1f;
            foreach (var mb in clone.GetComponentsInChildren<ScreenFx.ButtonMotion>(true)) UnityEngine.Object.Destroy(mb);
            foreach (var et in clone.GetComponentsInChildren<EventTrigger>(true)) UnityEngine.Object.Destroy(et);
            var size = from.rect.size;
            float s0 = fx.lossyScale.x > 0f ? from.lossyScale.x / fx.lossyScale.x : 1f;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = size;
            crt.anchoredPosition = Tween.CenterIn(from, fx);
            crt.localScale = Vector3.one * s0;
            crt.localRotation = Quaternion.identity;
            return clone;
        }
    }
}
