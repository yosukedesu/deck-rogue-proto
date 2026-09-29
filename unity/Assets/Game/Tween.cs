// Tween.cs — 依存なしの軽いトゥイーン (2026-09-07 M1)。演出キュー (Presenter) と画面の動きはこれだけで作る。
// 外部ライブラリ (DOTween 等) は Asset Store 経由なので使わない。値の補間・移動・拡縮・フェード・揺れ・浮き文字。
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public enum Ease { Linear, OutQuad, InQuad, OutBack, OutCubic, InOutQuad }

    public class Tween : MonoBehaviour
    {
        static Tween _i;
        public static Tween I
        {
            get
            {
                if (_i == null)
                {
                    var go = new GameObject("Tween");
                    DontDestroyOnLoad(go);
                    _i = go.AddComponent<Tween>();
                }
                return _i;
            }
        }

        public static float Apply(Ease e, float t)
        {
            t = Mathf.Clamp01(t);
            switch (e)
            {
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InQuad: return t * t;
                case Ease.OutCubic: { float u = 1f - t; return 1f - u * u * u; }
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case Ease.OutBack: { const float c1 = 1.70158f; const float c3 = c1 + 1f; float u = t - 1f; return 1f + c3 * u * u * u + c1 * u * u; }
                default: return t;
            }
        }

        /// <summary>dur 秒かけて 0→1 を onUpdate に渡す。時計は Time.deltaTime の積算 (timeScale は使っていないので実時間と同じ。
        /// Autopilot の Time.captureFramerate によるコマ送り撮影でも 1フレーム=1/60秒で決定的に進む。2026-09-16)</summary>
        public static Coroutine Run(float dur, Action<float> onUpdate, Ease ease = Ease.OutQuad, Action onDone = null)
        {
            return I.StartCoroutine(RunCo(dur, onUpdate, ease, onDone));
        }

        static IEnumerator RunCo(float dur, Action<float> onUpdate, Ease ease, Action onDone)
        {
            float t = 0f;
            if (dur <= 0f) { onUpdate(1f); onDone?.Invoke(); yield break; }
            while (true)
            {
                float k = t / dur;
                if (k >= 1f) break;
                onUpdate(Apply(ease, k));
                yield return null;
                t += Time.deltaTime;
            }
            onUpdate(1f);
            onDone?.Invoke();
        }

        public static Coroutine Move(RectTransform rt, Vector2 to, float dur, Ease ease = Ease.OutCubic, Action onDone = null)
        {
            if (rt == null) return null;
            var from = rt.anchoredPosition;
            return Run(dur, k => { if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k); }, ease, onDone);
        }

        public static Coroutine Scale(RectTransform rt, Vector3 to, float dur, Ease ease = Ease.OutBack, Action onDone = null)
        {
            if (rt == null) return null;
            var from = rt.localScale;
            return Run(dur, k => { if (rt != null) rt.localScale = Vector3.LerpUnclamped(from, to, k); }, ease, onDone);
        }

        public static Coroutine Fade(CanvasGroup cg, float to, float dur, Ease ease = Ease.OutQuad, Action onDone = null)
        {
            if (cg == null) return null;
            var from = cg.alpha;
            return Run(dur, k => { if (cg != null) cg.alpha = Mathf.Lerp(from, to, k); }, ease, onDone);
        }

        // ---- 基準の状態 (2026-09-18 ユーザー「敵に攻撃をずっとしていると敵がどんどん地面に埋まっていく」) ----
        // Punch/Shake/Squash/Puff/Lunge は矩形の位置・拡大・回転を動かして終わりに戻す。旧実装は各演出が「始まった瞬間の値」を戻り先に取っていたので、
        // 同じ矩形に重なって始まる (多段ヒット・連続の攻撃) と、2つ目が「1つ目で膨らんだ途中の値」を戻り先に取り、終わりにそこへ戻す＝ずれが残って積み上がっていた。
        // 敵の入れ物は組み直しでも作り直されないので、拡大が残るほど中心を軸に伸びた下端＝足元が座席より下へ沈んでいった。
        // 同じ矩形に重なる演出は基準を共有し、最後の1つが終わった時にだけ基準へ戻す。演出の外 (組み直しの再配置など) で値が変わった時は、それを新しい基準として取り込む
        class BaseState
        {
            public int Refs;
            public bool HasPos, HasScale, HasRot;                       // この性質を誰かが動かしている (終わりに戻す)
            public Vector2 Pos, LastPos; public Vector3 Scale, LastScale; public Quaternion Rot, LastRot;   // 基準と、演出が最後に書いた値
        }
        static readonly Dictionary<RectTransform, BaseState> _bases = new Dictionary<RectTransform, BaseState>();

        static BaseState Acquire(RectTransform rt, bool pos, bool scale, bool rot)
        {
            BaseState b;
            if (!_bases.TryGetValue(rt, out b)) { b = new BaseState(); _bases[rt] = b; }
            b.Refs++;
            if (pos && !b.HasPos) { b.HasPos = true; b.Pos = b.LastPos = rt.anchoredPosition; }
            if (scale && !b.HasScale) { b.HasScale = true; b.Scale = b.LastScale = rt.localScale; }
            if (rot && !b.HasRot) { b.HasRot = true; b.Rot = b.LastRot = rt.localRotation; }
            return b;
        }

        /// <summary>演出の外で動かされていたら (組み直しの再配置・別の演出系) それを基準に取り込む。毎フレーム書く前に呼ぶ</summary>
        static void Sync(RectTransform rt, BaseState b)
        {
            if (b.HasPos && rt.anchoredPosition != b.LastPos) b.Pos = b.LastPos = rt.anchoredPosition;
            if (b.HasScale && rt.localScale != b.LastScale) b.Scale = b.LastScale = rt.localScale;
            if (b.HasRot && rt.localRotation != b.LastRot) b.Rot = b.LastRot = rt.localRotation;
        }

        static void WritePos(RectTransform rt, BaseState b, Vector2 v) { rt.anchoredPosition = v; b.LastPos = rt.anchoredPosition; }
        static void WriteScale(RectTransform rt, BaseState b, Vector3 v) { rt.localScale = v; b.LastScale = rt.localScale; }
        static void WriteRot(RectTransform rt, BaseState b, Quaternion q) { rt.localRotation = q; b.LastRot = rt.localRotation; }

        static void Release(RectTransform rt, BaseState b)
        {
            b.Refs--;
            if (b.Refs > 0) return;
            if (rt != null)
            {
                Sync(rt, b);
                if (b.HasPos) rt.anchoredPosition = b.Pos;
                if (b.HasScale) rt.localScale = b.Scale;
                if (b.HasRot) rt.localRotation = b.Rot;
                _bases.Remove(rt);
            }
            else
            {   // 矩形が捨てられた (組み直し): 死んだ鍵を掃く
                var dead = new List<RectTransform>();
                foreach (var kv in _bases) if (kv.Key == null) dead.Add(kv.Key);
                foreach (var k in dead) _bases.Remove(k);
            }
        }

        /// <summary>被弾の揺れ (減衰する左右振動)。終わったら基準の位置に戻る (重なって始まっても基準は共有)</summary>
        public static Coroutine Shake(RectTransform rt, float amp = 12f, float dur = 0.35f)
        {
            if (rt == null) return null;
            var b = Acquire(rt, true, false, false);
            return Run(dur, k =>
            {
                if (rt == null) return;
                Sync(rt, b);
                float decay = 1f - k;
                WritePos(rt, b, b.Pos + new Vector2(Mathf.Sin(k * 40f) * amp * decay, Mathf.Cos(k * 33f) * amp * 0.35f * decay));
            }, Ease.Linear, () => Release(rt, b));
        }

        /// <summary>被弾のパンチ (拡縮＋小さな傾き)。LayoutGroup の子でも位置を汚さない (anchoredPosition はレイアウトが管理するため触らない)。
        /// keepBottom=true は足元 (下端) を留めて膨らむ＝舞台の板 (敵・リーダー) の入れ物は中心が軸なので、そのままだと膨らむぶん足元が地面に沈む (2026-09-18)</summary>
        public static Coroutine Punch(RectTransform rt, float amount = 0.06f, float dur = 0.3f, bool keepBottom = false)
        {
            if (rt == null) return null;
            var b = Acquire(rt, keepBottom, true, true);
            float h = rt.rect.height, pivotY = rt.pivot.y;
            return Run(dur, k =>
            {
                if (rt == null) return;
                Sync(rt, b);
                float decay = 1f - k;
                float w = Mathf.Sin(k * 18f) * decay;
                float sc = 1f + amount * decay * (1f - k * 0.5f);
                WriteScale(rt, b, b.Scale * sc);
                WriteRot(rt, b, b.Rot * Quaternion.Euler(0f, 0f, w * 2.5f));
                if (keepBottom) WritePos(rt, b, b.Pos + new Vector2(0f, h * pivotY * (sc - 1f)));   // 下端を留める
            }, Ease.Linear, () => Release(rt, b));
        }

        /// <summary>delay 秒後に action (演出のずらし用)</summary>
        public static void After(float delay, Action action)
        {
            I.StartCoroutine(AfterCo(delay, action));
        }

        static IEnumerator AfterCo(float delay, Action action)
        {
            // deltaTime の積算で待つ (WaitForSecondsRealtime は壁時計なので Time.captureFramerate のコマ送り撮影で決定的にならない)
            float t = 0f;
            while (t < delay) { yield return null; t += Time.deltaTime; }
            action?.Invoke();
        }

        /// <summary>
        /// ヒットストップ (2026-09-17 ⑫ とどめ): timeScale を scale に落として unscaled で sec 秒待ち、戻す。
        /// 演出の時計 (deltaTime) も一緒に止まるので、着弾の光と数字がその場で凍る。重ねて呼んだ時は長い方が勝つ
        /// </summary>
        public static void HitStop(float scale, float sec)
        {
            I.StartCoroutine(HitStopCo(scale, sec));
        }

        static int _hitStops;
        static IEnumerator HitStopCo(float scale, float sec)
        {
            _hitStops++;
            Time.timeScale = Mathf.Min(Time.timeScale, scale);
            float t = 0f;
            while (t < sec) { yield return null; t += Mathf.Min(Time.unscaledDeltaTime, 0.05f); }   // 長いフレーム (Rebuild 直後・撮影) 1回で終わらないよう上限
            if (--_hitStops <= 0) { _hitStops = 0; Time.timeScale = 1f; }
        }

        /// <summary>一瞬白く光る (Image の色を白→元へ)</summary>
        public static Coroutine Flash(Graphic g, Color flash, float dur = 0.25f)
        {
            if (g == null) return null;
            var origin = g.color;
            return Run(dur, k => { if (g != null) g.color = Color.Lerp(flash, origin, k); }, Ease.OutQuad, () => { if (g != null) g.color = origin; });
        }

        /// <summary>浮き文字 (ダメージ数字など)。parent の座標系で pos から上へ浮いて消える</summary>
        public static void Float(RectTransform parent, Vector2 pos, string text, Color color, int size = 34, float rise = 60f, float dur = 0.9f, float width = 240f)
        {
            if (parent == null) return;
            var rt = UiKit.NewRect("float", parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 60f);   // width: 1行に収まる幅 (既定 240 = 24px で10字。長い一言は呼び側が広げる。2026-09-29 p15)
            rt.anchoredPosition = pos;
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            // 影 (右下に 2px) と太めの縁取り: 草や月光の上でも読める (2026-09-09)
            var sh = UiKit.Txt(rt, text, size, new Color(0f, 0f, 0f, 0.75f), TextAnchor.MiddleCenter, true);
            UiKit.Stretch(sh.rectTransform, 2f, -2f, 2f, -2f);
            sh.outlineWidth = 0.3f;
            sh.outlineColor = new Color(0f, 0f, 0f, 0.75f);
            var t = UiKit.Txt(rt, text, size, color, TextAnchor.MiddleCenter, true);
            t.outlineWidth = 0.32f;
            t.outlineColor = new Color(0.05f, 0.03f, 0.06f, 0.95f);
            UiKit.Stretch(t.rectTransform, 0f, 0f, 0f, 0f);
            rt.localScale = Vector3.one * 0.6f;
            Scale(rt, Vector3.one, 0.18f, Ease.OutBack);
            Run(dur, k =>
            {
                if (rt == null) return;
                rt.anchoredPosition = pos + new Vector2(0f, rise * Apply(Ease.OutQuad, k));
                cg.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        /// <summary>斬撃: pos に筋を出して 0.18 秒で消える (angle は度)</summary>
        public static void Slash(RectTransform layer, Vector2 pos, float angle, Color color)
        {
            if (layer == null) return;
            var rt = UiKit.NewRect("slash", layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(240f, 60f);
            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ThemeFx.Slash();
            img.color = color;
            img.raycastTarget = false;
            rt.localScale = new Vector3(0.3f, 1f, 1f);
            Run(0.18f, k => { if (rt == null) return; rt.localScale = new Vector3(0.3f + 0.9f * Apply(Ease.OutQuad, k), 1f - 0.4f * k, 1f); img.color = new Color(color.r, color.g, color.b, color.a * (1f - k * k)); }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        /// <summary>斬撃 (2026-09-16 ユーザー「斬撃エフェクトをもっと豪華に。方向性は当初の直線のままで」→同日「2本に見える・ごてごてしすぎ。もっとスッキリ」):
        /// 直線の筋1本 (白い芯＋青緑の縁が振りの向きへ伸びて細くなり消える) ＋ 着弾の光 (衝撃線＝芯の閃光と細い針の放射。2026-09-17 ユーザー「星型がダサい」で8芒星を撤去) ＋ 火花5。残像・衝撃の輪はやめた。
        /// big (与ダメ 15 以上) は筋が 1.35 倍で、交差する2本目 (X) が 0.05 秒遅れて走り、火花8。絵は Art/fx/slash_streak・spark・glow (無ければ生成)</summary>
        public static void SlashFx(RectTransform layer, Vector2 pos, float angle, Color color, bool big = false, float scale = 1f)
        {
            if (layer == null) return;
            float len = (big ? 1.35f : 1f) * 340f * scale;
            var white = new Color(1f, 1f, 1f, 1f);
            Streak(layer, pos, angle, color, len, 1f, 0.3f, 0f);
            if (big) Streak(layer, pos, angle + 90f, color, len * 0.9f, 0.9f, 0.3f, 0.05f);   // 交差 (X)
            Impact(layer, pos, angle, color, big, 0.02f);
            // 火花: 振りの向きへ散る (放物線・回転・消える)
            int n = big ? 8 : 5;
            var dirV = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            for (int i = 0; i < n; i++)
            {
                var sp = UiKit.NewRect("spark", layer);
                sp.anchorMin = sp.anchorMax = new Vector2(0.5f, 0.5f);
                float sz = UnityEngine.Random.Range(18f, 32f);
                sp.sizeDelta = new Vector2(sz, sz); sp.anchoredPosition = pos;
                var sImg = sp.gameObject.AddComponent<Image>();
                sImg.sprite = ThemeFx.Spark(); sImg.raycastTarget = false;
                sImg.color = (i % 3 == 0) ? white : color;
                float spread = UnityEngine.Random.Range(-1.2f, 1.2f);
                var vel = (dirV * UnityEngine.Random.Range(-1f, 1f) + Perp(angle) * spread).normalized * UnityEngine.Random.Range(200f, 400f) * (big ? 1.3f : 1f);
                var start = pos; float dur = UnityEngine.Random.Range(0.25f, 0.42f); float spin = UnityEngine.Random.Range(-900f, 900f);
                var c0 = sImg.color;
                Run(dur, k => { if (sp == null) return; float t = k * dur; sp.anchoredPosition = start + vel * t + new Vector2(0f, -520f) * t * t; sp.localRotation = Quaternion.Euler(0f, 0f, t * spin); float sc = 1f - 0.5f * k; sp.localScale = new Vector3(sc, sc, 1f); sImg.color = new Color(c0.r, c0.g, c0.b, c0.a * (1f - k * k)); }, Ease.Linear, () => { if (sp != null) UnityEngine.Object.Destroy(sp.gameObject); });
            }
        }

        /// <summary>着弾の光 (筋の根元) ＝ 衝撃線 (2026-09-17 ユーザー「攻撃時に星型のエフェクトがダサい」→ 4案から裁定): 芯の小さな閃光が一瞬で縮み、細い針が放射状に走って一瞬で引く (ヒットスパーク)。形は残さない</summary>
        static void Impact(RectTransform layer, Vector2 pos, float angle, Color color, bool big, float delay)
        {
            Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.95f, 0.95f), big ? 120f : 90f, 1.2f, 0.4f, 0.1f, 0f, delay);
            After(delay, () => Needles(layer, pos, color, big ? 10 : 8, big ? 120f : 84f, angle));
        }

        /// <summary>
        /// 自分の札の当たり (2026-09-22 ユーザー「攻撃エフェクトがどの攻撃でも同じ」→ 札の id と名前のキーワード＋タイプで形を選ぶ。敵の HitFx と対):
        /// slash=斬撃 (斧。今までの SlashFx)・fang=牙 (上下から噛み合う2本)・horn=角の突き (自分の側から水平に刺さり、貫いた先へ抜ける)・
        /// vine=蔦の鞭 (自分の側から山なりにしなって届き、先が弾ける)・stomp=踏みつけ (太い縦の筋＋地面の輪)・spell=魔力の破裂 (青緑の光が弾けて針が放射)・
        /// light=灯の光 (暖色の閃光と立ちのぼる粒)・spark=火種 (小さな火花の炸裂)。
        /// 多段は hitIndex で向きを交互に (偶数=右上から振り下ろし・奇数=右下から振り上げ)、最後の1発 (hitIndex==hitTotal-1・2発以上) は 1.2 倍。from=自分の絵の中心 (角・鞭の出どころ)
        /// </summary>
        public static void PlayerHitFx(RectTransform layer, Vector2 pos, string style, Color color, bool big, int hitIndex, int hitTotal, Vector2 from)
        {
            if (layer == null) return;
            bool last = hitTotal > 1 && hitIndex == hitTotal - 1;
            float scale = last ? 1.2f : 1f;
            float sign = (hitIndex % 2 == 0) ? -1f : 1f;                       // 偶数=振り下ろし (角度マイナス)・奇数=振り上げ
            float angle = sign * UnityEngine.Random.Range(22f, 48f);
            var white = new Color(1f, 1f, 1f, 1f);
            switch (style)
            {
                case "fang":
                {   // 上下から噛み合う2本。多段は噛む向きを少し傾けて交互に
                    float tilt = sign * 12f;
                    Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.95f, 0.6f), 150f * scale, 0.4f, 1.0f, 0.14f, 0f, 0f);
                    Streak(layer, pos + new Vector2(0f, 28f * scale), -64f + tilt, color, 220f * (big ? 1.3f : 1f) * scale, 1f, 0.26f, 0f, 1.3f);
                    Streak(layer, pos + new Vector2(0f, -28f * scale), 64f + tilt, color, 220f * (big ? 1.3f : 1f) * scale, 1f, 0.26f, 0.03f, 1.3f);
                    Impact(layer, pos, tilt, color, big || last, 0.05f);
                    Sparks(layer, pos, color, big ? 8 : 5, tilt);
                    break;
                }
                case "horn":
                {   // 自分の側 (左) から水平に刺さる細い筋。貫いた先 (右) へ薄い筋が抜ける＝貫通の絵。多段は少し上下にずらす
                    float dy = sign * 14f * (hitTotal > 1 ? 1f : 0f);
                    float ang = -8f * sign;
                    Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.95f, 0.6f), 130f * scale, 0.4f, 1.0f, 0.14f, 0f, 0f);
                    Streak(layer, pos + new Vector2(-110f, dy), ang, color, 320f * (big ? 1.3f : 1f) * scale, 1f, 0.22f, 0f, 0.7f);
                    Streak(layer, pos + new Vector2(60f, dy - 4f * sign), ang, new Color(color.r, color.g, color.b, color.a * 0.6f), 180f * scale, 0.8f, 0.2f, 0.05f, 0.45f);   // 貫いて抜ける
                    Impact(layer, pos + new Vector2(0f, dy), ang, color, big || last, 0.04f);
                    Sparks(layer, pos, color, big ? 8 : 5, 0f);
                    break;
                }
                case "vine":
                    Whip(layer, from, pos, color, sign, scale, big || last);
                    break;
                case "stomp":
                    Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.95f, 0.8f), (big ? 260f : 200f) * scale, 0.3f, 1.2f, 0.18f, 0f, 0f);
                    Streak(layer, pos + new Vector2(0f, 70f), -90f, color, 240f * (big ? 1.3f : 1f) * scale, 1f, 0.24f, 0f, 1.9f);
                    Impact(layer, pos, -90f, color, big || last, 0.02f);
                    RingBurst(layer, pos + new Vector2(0f, -46f), new Color(color.r, color.g, color.b, 0.75f), (big ? 240f : 180f) * scale, 0.34f);
                    Sparks(layer, pos + new Vector2(0f, -30f), color, big ? 10 : 6, 90f);
                    break;
                case "spell":
                {   // 魔力の破裂: 青緑の光が対象の中で膨らんで弾け、針が放射、粒が上へ
                    Pop(layer, pos, ThemeFx.Glow(), new Color(color.r, color.g, color.b, 0.85f), (big ? 260f : 200f) * scale, 0.25f, 1.35f, 0.26f, 0f, 0f);
                    Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.97f, 0.9f), 110f * scale, 0.5f, 1.1f, 0.14f, 0f, 0.02f);
                    After(0.04f, () => Needles(layer, pos, color, big || last ? 14 : 10, (big ? 130f : 100f) * scale, 90f));
                    RingBurst(layer, pos, new Color(color.r, color.g, color.b, 0.7f), (big ? 220f : 170f) * scale, 0.36f);
                    Sparks(layer, pos, color, big ? 9 : 6, 90f);
                    break;
                }
                case "light":
                {   // 灯の光: 暖色の閃光がひとつ大きく灯り、光の粒がゆっくり立ちのぼる (火花は散らさない)
                    Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 0.95f, 0.78f, 0.9f), (big ? 280f : 220f) * scale, 0.3f, 1.3f, 0.3f, 0f, 0f);
                    Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.97f, 0.95f), 120f * scale, 0.6f, 1.0f, 0.16f, 0f, 0f);
                    After(0.03f, () => Needles(layer, pos, color, big || last ? 12 : 8, (big ? 120f : 92f) * scale, 90f));
                    Motes(layer, pos, new Color(1f, 0.96f, 0.82f, 1f), big ? 8 : 5, 80f * scale);
                    break;
                }
                case "spark":
                {   // 火種: 小さな橙の炸裂＝細かい火花が四方へ、短い針、小さな輪
                    Pop(layer, pos, ThemeFx.Glow(), new Color(color.r, color.g, color.b, 0.9f), 120f * scale, 0.3f, 1.2f, 0.16f, 0f, 0f);
                    Pop(layer, pos, ThemeFx.Glow(), ThemeFx.SlashCore, 70f * scale, 0.6f, 1.0f, 0.1f, 0f, 0f);
                    After(0.02f, () => Needles(layer, pos, color, 6, 60f * scale, angle));
                    RingBurst(layer, pos, new Color(color.r, color.g, color.b, 0.7f), 110f * scale, 0.26f);
                    for (int i = 0; i < 3; i++) Sparks(layer, pos, color, big ? 5 : 4, i * 120f);
                    break;
                }
                default:
                    SlashFx(layer, pos, angle, color, big, scale);
                    break;
            }
        }

        /// <summary>蔦の鞭: from から to へ山なり (制御点は上) の曲線に沿って短い筋を根元から先へ順に置き (しなって届く)、先端で弾ける。sign で山の向き (多段は上下交互)</summary>
        static void Whip(RectTransform layer, Vector2 from, Vector2 to, Color color, float sign, float scale, bool big)
        {
            if (layer == null) return;
            var start = from + new Vector2(30f, 10f);
            var mid = (start + to) * 0.5f + new Vector2(0f, -sign * 150f) + new Vector2(0f, 60f);   // 振り下ろし (sign<0) は上へ膨らむ
            int n = 8;
            Vector2 Bez(float t) { float u = 1f - t; return u * u * start + 2f * u * t * mid + t * t * to; }
            for (int i = 1; i <= n; i++)
            {
                float t0 = (i - 1) / (float)n, t1 = i / (float)n;
                var a = Bez(t0); var b = Bez(t1);
                var seg = b - a; float len = seg.magnitude; float ang = Mathf.Atan2(seg.y, seg.x) * Mathf.Rad2Deg;
                float thick = 0.5f + 0.9f * (i / (float)n);
                float alpha = 0.35f + 0.65f * (i / (float)n);
                Streak(layer, a + seg * 0.5f, ang, color, len * 1.35f, alpha, 0.2f, 0.016f * (i - 1), thick * scale);
            }
            float tipDelay = 0.016f * (n - 1) + 0.02f;
            var tipSeg = Bez(1f) - Bez(0.85f); float tipAng = Mathf.Atan2(tipSeg.y, tipSeg.x) * Mathf.Rad2Deg;
            Impact(layer, to, tipAng, color, big, tipDelay);
            After(tipDelay, () => { Sparks(layer, to, color, big ? 8 : 5, tipAng); RingBurst(layer, to, new Color(color.r, color.g, color.b, 0.6f), 120f * scale, 0.26f); });
        }

        /// <summary>光の粒: pos の周りから n 個の小さな光がゆっくり立ちのぼって消える (灯の当たり)</summary>
        static void Motes(RectTransform layer, Vector2 pos, Color color, int n, float rise)
        {
            for (int i = 0; i < n; i++)
            {
                var sp = UiKit.NewRect("mote", layer);
                sp.anchorMin = sp.anchorMax = new Vector2(0.5f, 0.5f);
                float sz = UnityEngine.Random.Range(10f, 22f);
                sp.sizeDelta = new Vector2(sz, sz);
                var p0 = pos + new Vector2(UnityEngine.Random.Range(-50f, 50f), UnityEngine.Random.Range(-30f, 20f));
                sp.anchoredPosition = p0;
                var img = sp.gameObject.AddComponent<Image>();
                img.sprite = ThemeFx.Glow(); img.raycastTarget = false; img.color = color;
                float dur = UnityEngine.Random.Range(0.4f, 0.7f); float drift = UnityEngine.Random.Range(-20f, 20f);
                Run(dur, k => { if (sp == null) return; sp.anchoredPosition = p0 + new Vector2(drift * k, rise * Apply(Ease.OutQuad, k)); img.color = new Color(color.r, color.g, color.b, color.a * (k < 0.3f ? k / 0.3f : 1f - (k - 0.3f) / 0.7f)); }, Ease.Linear, () => { if (sp != null) UnityEngine.Object.Destroy(sp.gameObject); });
            }
        }

        /// <summary>詠唱 (2026-09-22): 呪文は斧を振らず、体の前で色の光がひと膨らみして輪が広がる。pos=自分の絵の中心</summary>
        public static void CastFx(RectTransform layer, Vector2 pos, Color color)
        {
            if (layer == null) return;
            Pop(layer, pos + new Vector2(20f, 10f), ThemeFx.Glow(), new Color(color.r, color.g, color.b, 0.8f), 150f, 0.3f, 1.2f, 0.28f, 0f, 0f);
            Pop(layer, pos + new Vector2(20f, 10f), ThemeFx.Glow(), new Color(1f, 1f, 0.97f, 0.9f), 70f, 0.5f, 1.0f, 0.16f, 0f, 0.02f);
            RingBurst(layer, pos + new Vector2(20f, 0f), new Color(color.r, color.g, color.b, 0.6f), 130f, 0.3f);
            Motes(layer, pos + new Vector2(20f, 0f), new Color(color.r, color.g, color.b, 0.9f), 4, 50f);
        }

        /// <summary>衝撃線: n 本の細い針が中心から放射状に伸びて (OutQuad)、後半で消える。振りの向きの針は少し長い</summary>
        public static void Needles(RectTransform layer, Vector2 pos, Color color, int n, float reach, float angle)
        {
            if (layer == null) return;
            float a0 = UnityEngine.Random.Range(0f, 360f / n);
            for (int i = 0; i < n; i++)
            {
                float a = a0 + i * 360f / n + UnityEngine.Random.Range(-8f, 8f);
                float along = Mathf.Abs(Mathf.Cos((a - angle) * Mathf.Deg2Rad));
                float len = reach * (0.55f + 0.45f * along) * UnityEngine.Random.Range(0.8f, 1.1f);
                var rt = UiKit.NewRect("needle", layer);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);   // 内側の端が中心
                rt.sizeDelta = new Vector2(len, len * 0.25f * 0.28f); rt.anchoredPosition = pos + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * 10f;
                rt.localRotation = Quaternion.Euler(0f, 0f, a);
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = ThemeFx.SlashStreak(); img.raycastTarget = false;
                var c = (i % 2 == 0) ? Color.white : color;
                img.color = c;
                rt.localScale = new Vector3(0.25f, 1f, 1f);
                Run(0.17f, k =>
                {
                    if (rt == null) return;
                    rt.localScale = new Vector3(0.25f + 0.85f * Apply(Ease.OutQuad, Mathf.Clamp01(k / 0.55f)), 1f - 0.5f * k, 1f);
                    float fade = k < 0.5f ? 1f : 1f - (k - 0.5f) / 0.5f;
                    img.color = new Color(c.r, c.g, c.b, c.a * fade);
                }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
            }
        }

        /// <summary>
        /// 完全に防いだ時の演出＝盾で受け止める (2026-09-17 ユーザー「完全に防いだ時に敵からダメージ食らってるように見える」→ 3案から裁定):
        /// 空色の盾の面 (GuardDisc) を正面に構え、攻撃の筋は空色で面の手前で止まり (面が筋の先を隠す)、当たった点が白く閃いて、火花は攻撃して来た側へ散る。
        /// 被弾の筋 (朱)・のけぞり・赤い点滅・揺れ・押し縮みは出さない。pos=受けた側の絵の中心、style=技の種類 (自分の攻撃を敵が受けた時は "slash")、
        /// dir=攻撃が来る向き (+1 右から＝自分が受ける／-1 左から＝敵が受ける)
        /// </summary>
        public static void GuardFx(RectTransform layer, Vector2 pos, string style, float dir = 1f)
        {
            if (layer == null) return;
            var white = new Color(1f, 1f, 1f, 1f);
            var light = new Color(PaperFx.SkyLight.r, PaperFx.SkyLight.g, PaperFx.SkyLight.b, 1f);
            Vector2 front = pos + new Vector2(62f * dir, 4f);   // 盾の面は体より前 (攻撃が来る側) に構える
            Vector2 stop = front + new Vector2(28f * dir, 0f);   // 筋の先が止まる点
            float m = dir >= 0f ? 1f : -1f;
            // 攻撃の筋 (種類別・空色・短い)。先に描いて盾の面で先を隠す
            if (style == "fang") { Streak(layer, stop + new Vector2(0f, 22f), -62f * m + (m < 0f ? 180f : 0f), light, 130f, 0.95f, 0.2f, 0f, 1.1f); Streak(layer, stop + new Vector2(0f, -22f), 62f * m + (m < 0f ? 180f : 0f), light, 130f, 0.95f, 0.2f, 0.03f, 1.1f); }
            else if (style == "claw") { for (int i = -1; i <= 1; i++) Streak(layer, stop + Perp(-38f) * (i * 22f), m > 0f ? -38f : 218f, light, 170f, 0.9f, 0.22f, 0.02f * (i + 1), 0.6f); }
            else if (style == "blunt") Streak(layer, stop + new Vector2(0f, 50f), -90f, light, 150f, 1f, 0.2f, 0f, 1.6f);
            else if (style == "beam") { Streak(layer, stop, 90f, light, 120f, 0.9f, 0.2f, 0f, 1.1f); Streak(layer, stop, -90f, light, 120f, 0.9f, 0.2f, 0f, 1.1f); }
            else if (style == "throw") Pop(layer, stop, ThemeFx.Glow(), new Color(light.r, light.g, light.b, 0.8f), 110f, 1.2f, 0.4f, 0.12f, 0f, 0f);
            else if (style == "slash") Streak(layer, stop + new Vector2(40f * m, 26f), m > 0f ? 215f : -35f, light, 190f, 1f, 0.22f, 0f, 0.8f);   // 自分の斬撃 (左上→右下) が敵の盾で止まる
            else Streak(layer, stop + new Vector2(50f * m, 0f), m > 0f ? 180f : 0f, light, 170f, 1f, 0.22f, 0f, 0.7f);   // 突き・斬撃: 攻撃が来る側から水平に来て止まる
            GuardDisc(layer, front, 0.26f);
            Pop(layer, stop, ThemeFx.Glow(), white, 70f, 1.2f, 0.3f, 0.1f, 0f, 0.02f);   // 当たった点の閃き
            SparksDir(layer, stop, light, 7, m > 0f ? 40f : 140f);   // 火花は攻撃して来た側の上へ
        }

        /// <summary>盾の面: 空色の光の円 (透過) が正面にぱっと立ち、白い縁の輪と中央の盾の紋。少し保って消える</summary>
        static void GuardDisc(RectTransform layer, Vector2 pos, float hold)
        {
            var sky = PaperFx.Sky; var light = PaperFx.SkyLight;
            var rt = UiKit.NewRect("guard", layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(150f, 150f); rt.anchoredPosition = pos;
            var face = rt.gameObject.AddComponent<Image>();
            face.sprite = ThemeFx.Glow(); face.color = new Color(sky.r, sky.g, sky.b, 0.55f); face.raycastTarget = false;
            var rim = UiKit.NewRect("rim", rt);
            UiKit.Stretch(rim, -4f, -4f, -4f, -4f);
            var rimImg = rim.gameObject.AddComponent<Image>();
            rimImg.sprite = ThemeFx.Ring(); rimImg.color = new Color(light.r, light.g, light.b, 0.95f); rimImg.raycastTarget = false;
            var glyph = UiKit.NewRect("glyph", rt);
            glyph.anchorMin = glyph.anchorMax = new Vector2(0.5f, 0.5f); glyph.sizeDelta = new Vector2(48f, 48f);
            var gImg = glyph.gameObject.AddComponent<Image>();
            gImg.sprite = Theme.Icon("shield"); gImg.preserveAspect = true; gImg.color = new Color(1f, 1f, 1f, 0.9f); gImg.raycastTarget = false;
            var cg = rt.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false;
            rt.localScale = Vector3.one * 0.6f;
            Run(0.09f, k => { if (rt != null) rt.localScale = Vector3.one * (0.6f + 0.4f * Apply(Ease.OutBack, k)); }, Ease.Linear, () =>
            {
                After(hold, () => Run(0.22f, k => { if (cg != null) cg.alpha = 1f - k; if (rt != null) rt.localScale = Vector3.one * (1f + 0.08f * k); }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); }));
            });
            // 面を斜めに横切る白い閃き (金属の面に光が走る)
            Streak(layer, pos, 65f, Color.white, 120f, 0.85f, 0.2f, 0.05f, 0.4f);
        }

        /// <summary>火花 n 個を angle の向きを中心に散らす (SparksDir: 向きの広がりを狭くした版。盾で弾いた火花)</summary>
        static void SparksDir(RectTransform layer, Vector2 pos, Color color, int n, float angle)
        {
            var white = new Color(1f, 1f, 1f, 1f);
            for (int i = 0; i < n; i++)
            {
                var sp = UiKit.NewRect("spark", layer);
                sp.anchorMin = sp.anchorMax = new Vector2(0.5f, 0.5f);
                float sz = UnityEngine.Random.Range(14f, 26f);
                sp.sizeDelta = new Vector2(sz, sz); sp.anchoredPosition = pos;
                var sImg = sp.gameObject.AddComponent<Image>();
                sImg.sprite = ThemeFx.Spark(); sImg.raycastTarget = false;
                sImg.color = (i % 2 == 0) ? white : color;
                float a = (angle + UnityEngine.Random.Range(-55f, 55f)) * Mathf.Deg2Rad;
                var vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * UnityEngine.Random.Range(220f, 420f);
                var start = pos; float dur = UnityEngine.Random.Range(0.22f, 0.38f); float spin = UnityEngine.Random.Range(-900f, 900f);
                var c0 = sImg.color;
                Run(dur, k => { if (sp == null) return; float t = k * dur; sp.anchoredPosition = start + vel * t + new Vector2(0f, -520f) * t * t; sp.localRotation = Quaternion.Euler(0f, 0f, t * spin); float sc = 1f - 0.5f * k; sp.localScale = new Vector3(sc, sc, 1f); sImg.color = new Color(c0.r, c0.g, c0.b, c0.a * (1f - k * k)); }, Ease.Linear, () => { if (sp != null) UnityEngine.Object.Destroy(sp.gameObject); });
            }
        }

        /// <summary>膨らんで消える1枚絵 (着弾の光・衝撃の輪): size を基準に scale が from→to (OutQuad)、spin 度回りながら、k² で消える</summary>
        static void Pop(RectTransform layer, Vector2 pos, Sprite sprite, Color color, float size, float from, float to, float dur, float spin, float delay)
        {
            After(delay, () =>
            {
                if (layer == null) return;
                var rt = UiKit.NewRect("pop", layer);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(size, size); rt.anchoredPosition = pos;
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = sprite; img.color = color; img.raycastTarget = false;
                rt.localScale = new Vector3(from, from, 1f);
                Run(dur, k => { if (rt == null) return; float sc = from + (to - from) * Apply(Ease.OutQuad, k); rt.localScale = new Vector3(sc, sc, 1f); if (spin != 0f) rt.localRotation = Quaternion.Euler(0f, 0f, k * spin); img.color = new Color(color.r, color.g, color.b, color.a * (1f - k * k)); }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
            });
        }

        static Vector2 Perp(float angle) { float a = (angle + 90f) * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(a), Mathf.Sin(a)); }

        /// <summary>直線の筋1本: 振りの向きに伸びながら (scaleX 0.2→1.2) 細くなり、後半で消える。delay 秒遅らせられる (残像・交差用)</summary>
        static void Streak(RectTransform layer, Vector2 pos, float angle, Color color, float len, float alpha, float dur, float delay, float thick = 1f)
        {
            After(delay, () =>
            {
                if (layer == null) return;
                var rt = UiKit.NewRect("slash", layer);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(len, len * 0.25f * thick); rt.anchoredPosition = pos;
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = ThemeFx.SlashStreak(); img.raycastTarget = false;
                img.color = new Color(color.r, color.g, color.b, color.a * alpha);
                rt.localScale = new Vector3(0.2f, 1f, 1f);
                var fwd = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Run(dur, k =>
                {
                    if (rt == null) return;
                    float grow = Apply(Ease.OutQuad, Mathf.Clamp01(k / 0.5f));
                    rt.localScale = new Vector3(0.2f + 1.0f * grow, 1f - 0.5f * k, 1f);
                    rt.anchoredPosition = pos + fwd * (26f * k);
                    float fade = k < 0.45f ? 1f : 1f - (k - 0.45f) / 0.55f;
                    img.color = new Color(color.r, color.g, color.b, color.a * alpha * fade);
                }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
            });
        }

        /// <summary>画面全体の色の点滅 (被弾の赤など)</summary>
        public static void ScreenFlash(RectTransform layer, Color color, float dur = 0.25f)
        {
            if (layer == null) return;
            var rt = UiKit.NewRect("flash", layer);
            UiKit.Stretch(rt, 0f, 0f, 0f, 0f);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            Run(dur, k => { if (img != null) img.color = new Color(color.r, color.g, color.b, color.a * (1f - k)); }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        /// <summary>アイコンが膨らんで消える (ブロック獲得の盾など)</summary>
        public static void IconBurst(RectTransform layer, Vector2 pos, string icon, Color color, float size = 96f)
        {
            if (layer == null) return;
            var rt = UiKit.NewRect("burst", layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Icon(icon);
            img.preserveAspect = true;
            img.color = color;
            img.raycastTarget = false;
            rt.localScale = Vector3.one * 0.5f;
            Run(0.4f, k => { if (rt == null) return; rt.localScale = Vector3.one * (0.5f + 0.9f * Apply(Ease.OutBack, k)); img.color = new Color(color.r, color.g, color.b, color.a * (1f - k * k)); }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        /// <summary>判 (2026-09-17 リアクション発動の演出): 紙の札に一言。1.5倍から押し当てるように縮んで止まり、hold の後に消える</summary>
        public static void Stamp(RectTransform layer, Vector2 pos, string text, Color paper, Color ink, Color? edge = null, int size = 22, float hold = 0.55f, float rot = -8f)
        {
            if (layer == null) return;
            var rt = UiKit.NewRect("stamp", layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            float w = 30f + text.Length * size * 1.05f, h = size + 18f;
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.Euler(0f, 0f, rot);
            if (edge.HasValue)
            {
                var e = PaperFx.Sheet(rt, PaperFx.Tag, "edge", edge.Value);
                UiKit.Stretch(e.rectTransform, -3f, -3f, -3f, -3f);
                e.raycastTarget = false;
            }
            var bg = PaperFx.Sheet(rt, PaperFx.Tag, "paper", paper);
            UiKit.Stretch(bg.rectTransform, 0f, 0f, 0f, 0f);
            bg.raycastTarget = false;
            var t = UiKit.Deco(rt, text, size, ink, TextAnchor.MiddleCenter);
            UiKit.Stretch(t.rectTransform, 0f, 0f, 0f, 0f);
            t.characterSpacing = 6f;
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            rt.localScale = Vector3.one * 1.5f;
            Run(0.16f, k => { if (rt != null) rt.localScale = Vector3.one * (1.5f - 0.5f * Apply(Ease.OutCubic, k)); }, Ease.Linear, () =>
            {
                After(hold, () => Run(0.25f, k => { if (cg != null) cg.alpha = 1f - k; }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); }));
            });
        }

        /// <summary>輪が広がって消える (着弾・発動の余韻)</summary>
        public static void RingBurst(RectTransform layer, Vector2 pos, Color color, float size = 120f, float dur = 0.35f)
        {
            if (layer == null) return;
            var rt = UiKit.NewRect("ring", layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ThemeFx.Ring(); img.preserveAspect = true; img.color = color; img.raycastTarget = false;
            rt.localScale = Vector3.one * 0.3f;
            Run(dur, k => { if (rt == null) return; rt.localScale = Vector3.one * (0.3f + 1.0f * k); img.color = new Color(color.r, color.g, color.b, color.a * (1f - k * k)); }, Ease.OutQuad, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        /// <summary>×印 (打ち消し): 2本の棒が交差して現れ、少し残って消える</summary>
        public static void CrossMark(RectTransform layer, Vector2 pos, Color color, float size = 64f)
        {
            if (layer == null) return;
            var rt = UiKit.NewRect("cross", layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            for (int i = 0; i < 2; i++)
            {
                var bar = UiKit.NewRect("bar", rt);
                bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
                bar.sizeDelta = new Vector2(size, size * 0.16f);
                bar.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
                var img = bar.gameObject.AddComponent<Image>();
                img.color = color; img.raycastTarget = false;
                var shadow = UiKit.NewRect("shadow", bar);
                UiKit.Stretch(shadow, -2f, -2f, -2f, -2f);
                var sh = shadow.gameObject.AddComponent<Image>(); sh.color = new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.9f); sh.raycastTarget = false;
                shadow.SetAsFirstSibling();
            }
            rt.localScale = Vector3.one * 1.8f;
            Run(0.14f, k => { if (rt != null) rt.localScale = Vector3.one * (1.8f - 0.8f * Apply(Ease.OutCubic, k)); }, Ease.Linear, () =>
            {
                After(0.5f, () => Run(0.25f, k => { if (cg != null) cg.alpha = 1f - k; }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); }));
            });
        }

        /// <summary>
        /// 予備動作 (2026-09-17 敵の行動の演出): 足元を軸にぐっと縮んで (横に太る) から伸び上がる。舞台の板は UI の矩形の角から大きさを読むので、
        /// 足元 (下端) を動かさないよう anchoredPosition で埋め合わせる。dur の前半で縮み、後半で 1.04 まで伸びて戻る
        /// </summary>
        public static void Squash(RectTransform rt, float dur = 0.24f, float sx = 1.12f, float sy = 0.86f)
        {
            if (rt == null) return;
            var b = Acquire(rt, true, true, false); float h = rt.rect.height; float pivotY = rt.pivot.y;
            Run(dur, k =>
            {
                if (rt == null) return;
                Sync(rt, b);
                float a = k < 0.45f ? Apply(Ease.OutQuad, k / 0.45f) : 1f - Apply(Ease.OutBack, (k - 0.45f) / 0.55f);
                float x = 1f + (sx - 1f) * a, y = 1f + (sy - 1f) * a;
                WriteScale(rt, b, new Vector3(b.Scale.x * x, b.Scale.y * y, b.Scale.z));
                WritePos(rt, b, b.Pos + new Vector2(0f, -h * pivotY * (1f - y)));   // 下端を留める
            }, Ease.Linear, () => Release(rt, b));
        }

        /// <summary>膨らむ (筋力上げ・応援): 中心から 1.15 倍まで膨らんで戻る。足元は留める</summary>
        public static void Puff(RectTransform rt, float dur = 0.36f, float amount = 1.15f)
        {
            if (rt == null) return;
            var b = Acquire(rt, true, true, false); float h = rt.rect.height; float pivotY = rt.pivot.y;
            Run(dur, k =>
            {
                if (rt == null) return;
                Sync(rt, b);
                float a = Mathf.Sin(k * Mathf.PI);
                float sc = 1f + (amount - 1f) * a;
                WriteScale(rt, b, new Vector3(b.Scale.x * sc, b.Scale.y * sc, b.Scale.z));
                WritePos(rt, b, b.Pos + new Vector2(0f, -h * pivotY * (1f - sc)));
            }, Ease.Linear, () => Release(rt, b));
        }

        /// <summary>飛び道具: 光の玉が from から to へ山なりに飛ぶ (尾を引く)。着いたら onArrive</summary>
        public static void Projectile(RectTransform layer, Vector2 from, Vector2 to, Color color, float size = 44f, float dur = 0.24f, float arc = 60f, Action onArrive = null)
        {
            if (layer == null) { onArrive?.Invoke(); return; }
            var rt = UiKit.NewRect("projectile", layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size); rt.anchoredPosition = from;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ThemeFx.Glow(); img.color = color; img.raycastTarget = false;
            var core = UiKit.NewRect("core", rt);
            UiKit.Stretch(core, size * 0.3f, size * 0.3f, size * 0.3f, size * 0.3f);
            var cImg = core.gameObject.AddComponent<Image>(); cImg.sprite = ThemeFx.Glow(); cImg.color = new Color(1f, 1f, 0.95f, 0.95f); cImg.raycastTarget = false;
            Vector2 last = from; int trailEvery = 0;
            Run(dur, k =>
            {
                if (rt == null) return;
                var p = Vector2.Lerp(from, to, k) + new Vector2(0f, arc * Mathf.Sin(k * Mathf.PI));
                rt.anchoredPosition = p;
                if (++trailEvery % 2 == 0)
                {   // 尾: 小さな光を置いて消す
                    var tr = UiKit.NewRect("trail", layer);
                    tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
                    tr.sizeDelta = new Vector2(size * 0.6f, size * 0.6f); tr.anchoredPosition = last;
                    var tImg = tr.gameObject.AddComponent<Image>(); tImg.sprite = ThemeFx.Glow(); tImg.color = new Color(color.r, color.g, color.b, color.a * 0.6f); tImg.raycastTarget = false;
                    Run(0.18f, kk => { if (tr != null) { tImg.color = new Color(color.r, color.g, color.b, color.a * 0.6f * (1f - kk)); tr.localScale = Vector3.one * (1f - 0.6f * kk); } }, Ease.Linear, () => { if (tr != null) UnityEngine.Object.Destroy(tr.gameObject); });
                }
                last = p;
            }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); onArrive?.Invoke(); });
        }

        /// <summary>光線: from から to へ一直線の筋 (白い芯＋色の縁) が一瞬走って消える</summary>
        public static void BeamFx(RectTransform layer, Vector2 from, Vector2 to, Color color, float dur = 0.28f, float thick = 1f, Sprite streak = null)
        {
            if (layer == null) return;
            var d = to - from; float len = d.magnitude; float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            var rt = UiKit.NewRect("beam", layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(len, 28f * thick); rt.anchoredPosition = from;
            rt.localRotation = Quaternion.Euler(0f, 0f, ang);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = streak ?? ThemeFx.SlashStreak(); img.color = color; img.raycastTarget = false;   // streak＝灯の筋 (暖色) など差し替え (2026-09-20)
            rt.localScale = new Vector3(0f, 1f, 1f);
            Run(dur, k =>
            {
                if (rt == null) return;
                float grow = Apply(Ease.OutCubic, Mathf.Clamp01(k / 0.35f));
                rt.localScale = new Vector3(grow, 1f - 0.7f * Mathf.Clamp01((k - 0.35f) / 0.65f), 1f);
                float fade = k < 0.5f ? 1f : 1f - (k - 0.5f) / 0.5f;
                img.color = new Color(color.r, color.g, color.b, color.a * fade);
            }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
            Pop(layer, to, ThemeFx.Glow(), new Color(1f, 1f, 0.95f, 0.85f), 160f, 0.3f, 1.2f, 0.22f, 0f, dur * 0.3f);
        }

        /// <summary>
        /// 種類別の当たり (2026-09-17): 敵の技の名前から選ぶ。fang=牙 (2本の短い筋が噛み合う)・claw=爪 (3本の平行な筋)・thrust=突き (水平の細い筋)・
        /// blunt=打撃 (太い縦の筋＋大きな光＋地面の輪)・beam=光線 (着弾の光と縦の筋)・throw=飛び道具の着弾 (色の輪と飛沫)・slash=斬撃 (SlashFx)
        /// </summary>
        public static void HitFx(RectTransform layer, Vector2 pos, string style, Color color, bool big)
        {
            if (layer == null) return;
            var white = new Color(1f, 1f, 1f, 1f);
            switch (style)
            {
                case "fang":
                    Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.95f, 0.6f), 150f, 0.4f, 1.0f, 0.14f, 0f, 0f);
                    Streak(layer, pos + new Vector2(0f, 26f), -62f, color, 210f * (big ? 1.3f : 1f), 1f, 0.26f, 0f, 1.3f);
                    Streak(layer, pos + new Vector2(0f, -26f), 62f, color, 210f * (big ? 1.3f : 1f), 1f, 0.26f, 0.03f, 1.3f);
                    Impact(layer, pos, 0f, color, big, 0.05f);
                    Sparks(layer, pos, color, big ? 8 : 5, 0f);
                    break;
                case "claw":
                    Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.95f, 0.55f), 150f, 0.4f, 1.0f, 0.14f, 0f, 0f);
                    for (int i = -1; i <= 1; i++) Streak(layer, pos + Perp(-38f) * (i * 26f), -38f, color, 300f * (big ? 1.25f : 1f), 0.95f, 0.28f, 0.02f * (i + 1), 0.7f);
                    Sparks(layer, pos, color, big ? 9 : 6, -38f);
                    break;
                case "thrust":
                    // 突き: 右 (敵の側) から水平に走る細い筋が刺さり、小さな光。火花は正面へ
                    Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.95f, 0.6f), 140f, 0.4f, 1.0f, 0.14f, 0f, 0f);
                    Streak(layer, pos + new Vector2(90f, 4f), 180f, color, 300f * (big ? 1.3f : 1f), 1f, 0.24f, 0f, 0.75f);
                    Impact(layer, pos, 180f, color, big, 0.03f);
                    Sparks(layer, pos, color, big ? 8 : 5, 180f);
                    break;
                case "blunt":
                    Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.95f, 0.8f), big ? 260f : 200f, 0.3f, 1.2f, 0.18f, 0f, 0f);
                    Streak(layer, pos + new Vector2(0f, 60f), -90f, color, 220f * (big ? 1.3f : 1f), 1f, 0.24f, 0f, 1.8f);
                    Impact(layer, pos, -90f, color, big, 0.02f);
                    RingBurst(layer, pos + new Vector2(0f, -40f), new Color(color.r, color.g, color.b, 0.7f), big ? 220f : 170f, 0.32f);
                    Sparks(layer, pos, color, big ? 10 : 6, -90f);
                    break;
                case "beam":
                    Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.95f, 0.9f), big ? 240f : 190f, 0.2f, 1.3f, 0.22f, 0f, 0f);
                    Streak(layer, pos, 90f, color, 160f, 0.9f, 0.22f, 0f, 1.2f);
                    Streak(layer, pos, -90f, color, 160f, 0.9f, 0.22f, 0f, 1.2f);
                    Impact(layer, pos, 90f, color, big, 0.02f);
                    break;
                case "throw":
                    Pop(layer, pos, ThemeFx.Glow(), new Color(color.r, color.g, color.b, 0.7f), 140f, 0.4f, 1.1f, 0.2f, 0f, 0f);
                    RingBurst(layer, pos, color, big ? 180f : 140f, 0.3f);
                    Sparks(layer, pos, color, big ? 9 : 6, 90f);
                    break;
                default:
                    SlashFx(layer, pos, UnityEngine.Random.Range(20f, 50f), color, big);
                    break;
            }
        }

        /// <summary>火花 n 個を angle の向きに散らす (SlashFx と同じ放物線)</summary>
        static void Sparks(RectTransform layer, Vector2 pos, Color color, int n, float angle)
        {
            var white = new Color(1f, 1f, 1f, 1f);
            var dirV = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            for (int i = 0; i < n; i++)
            {
                var sp = UiKit.NewRect("spark", layer);
                sp.anchorMin = sp.anchorMax = new Vector2(0.5f, 0.5f);
                float sz = UnityEngine.Random.Range(16f, 30f);
                sp.sizeDelta = new Vector2(sz, sz); sp.anchoredPosition = pos;
                var sImg = sp.gameObject.AddComponent<Image>();
                sImg.sprite = ThemeFx.Spark(); sImg.raycastTarget = false;
                sImg.color = (i % 3 == 0) ? white : color;
                float spread = UnityEngine.Random.Range(-1.2f, 1.2f);
                var vel = (dirV * UnityEngine.Random.Range(-1f, 1f) + Perp(angle) * spread).normalized * UnityEngine.Random.Range(180f, 380f);
                var start = pos; float dur = UnityEngine.Random.Range(0.25f, 0.4f); float spin = UnityEngine.Random.Range(-900f, 900f);
                var c0 = sImg.color;
                Run(dur, k => { if (sp == null) return; float t = k * dur; sp.anchoredPosition = start + vel * t + new Vector2(0f, -520f) * t * t; sp.localRotation = Quaternion.Euler(0f, 0f, t * spin); sImg.color = new Color(c0.r, c0.g, c0.b, 1f - k * k); }, Ease.Linear, () => { if (sp != null) UnityEngine.Object.Destroy(sp.gameObject); });
            }
        }

        /// <summary>踏み込み: 前へ出て戻る (敵の攻撃・自分の攻撃)。前半 35% で出て後半 65% で基準へ戻る (重なって始まっても基準は共有)</summary>
        public static void Lunge(RectTransform rt, Vector2 dir, float dur = 0.28f)
        {
            if (rt == null) return;
            var b = Acquire(rt, true, false, false);
            Run(dur, k =>
            {
                if (rt == null) return;
                Sync(rt, b);
                float a = k < 0.35f ? Apply(Ease.OutQuad, k / 0.35f) : 1f - Apply(Ease.OutCubic, (k - 0.35f) / 0.65f);
                WritePos(rt, b, b.Pos + dir * a);
            }, Ease.Linear, () => Release(rt, b));
        }

        /// <summary>他の RectTransform の中心を、fx レイヤーの座標系 (anchor 中央) へ変換する</summary>
        public static Vector2 CenterIn(RectTransform target, RectTransform layer)
        {
            if (target == null || layer == null) return Vector2.zero;
            var world = target.TransformPoint(target.rect.center);
            var local = layer.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }
    }
}
