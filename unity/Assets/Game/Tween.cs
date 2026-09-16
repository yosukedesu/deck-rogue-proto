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

        /// <summary>被弾の揺れ (減衰する左右振動)。終わったら元の位置に戻る</summary>
        public static Coroutine Shake(RectTransform rt, float amp = 12f, float dur = 0.35f)
        {
            if (rt == null) return null;
            var origin = rt.anchoredPosition;
            return Run(dur, k =>
            {
                if (rt == null) return;
                float decay = 1f - k;
                rt.anchoredPosition = origin + new Vector2(Mathf.Sin(k * 40f) * amp * decay, Mathf.Cos(k * 33f) * amp * 0.35f * decay);
            }, Ease.Linear, () => { if (rt != null) rt.anchoredPosition = origin; });
        }

        /// <summary>被弾のパンチ (拡縮＋小さな傾き)。LayoutGroup の子でも位置を汚さない (anchoredPosition はレイアウトが管理するため触らない)</summary>
        public static Coroutine Punch(RectTransform rt, float amount = 0.06f, float dur = 0.3f)
        {
            if (rt == null) return null;
            var s0 = rt.localScale;
            var r0 = rt.localRotation;
            return Run(dur, k =>
            {
                if (rt == null) return;
                float decay = 1f - k;
                float w = Mathf.Sin(k * 18f) * decay;
                rt.localScale = s0 * (1f + amount * decay * (1f - k * 0.5f));
                rt.localRotation = r0 * Quaternion.Euler(0f, 0f, w * 2.5f);
            }, Ease.Linear, () => { if (rt != null) { rt.localScale = s0; rt.localRotation = r0; } });
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
        public static void Float(RectTransform parent, Vector2 pos, string text, Color color, int size = 34, float rise = 60f, float dur = 0.9f)
        {
            if (parent == null) return;
            var rt = UiKit.NewRect("float", parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(240f, 60f);
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
        /// 直線の筋1本 (白い芯＋青緑の縁が振りの向きへ伸びて細くなり消える) ＋ 着弾の光 (一瞬の光の玉と8芒星) ＋ 火花6。残像・衝撃の輪はやめた。
        /// big (与ダメ 15 以上) は筋が 1.35 倍で、交差する2本目 (X) が 0.05 秒遅れて走り、火花10。絵は Art/fx/slash_streak・slash_burst・spark・glow (無ければ生成)</summary>
        public static void SlashFx(RectTransform layer, Vector2 pos, float angle, Color color, bool big = false)
        {
            if (layer == null) return;
            float len = (big ? 1.35f : 1f) * 340f;
            var white = new Color(1f, 1f, 1f, 1f);
            Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.95f, 0.7f), big ? 240f : 170f, 0.4f, 1.1f, 0.16f, 0f, 0f);   // 着弾の光 (奥): 一瞬だけ
            Streak(layer, pos, angle, color, len, 1f, 0.3f, 0f);
            if (big) Streak(layer, pos, angle + 90f, color, len * 0.9f, 0.9f, 0.3f, 0.05f);   // 交差 (X)
            Pop(layer, pos, ThemeFx.SlashBurst(), color, big ? 150f : 110f, 0.3f, 1.3f, 0.22f, 45f, 0.02f);
            // 火花: 振りの向きへ散る (放物線・回転・消える)
            int n = big ? 10 : 6;
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
            var origin = rt.anchoredPosition; float h = rt.rect.height; float pivotY = rt.pivot.y;
            Run(dur, k =>
            {
                if (rt == null) return;
                float a = k < 0.45f ? Apply(Ease.OutQuad, k / 0.45f) : 1f - Apply(Ease.OutBack, (k - 0.45f) / 0.55f);
                float x = 1f + (sx - 1f) * a, y = 1f + (sy - 1f) * a;
                rt.localScale = new Vector3(x, y, 1f);
                rt.anchoredPosition = origin + new Vector2(0f, -h * pivotY * (1f - y));   // 下端を留める
            }, Ease.Linear, () => { if (rt != null) { rt.localScale = Vector3.one; rt.anchoredPosition = origin; } });
        }

        /// <summary>膨らむ (筋力上げ・応援): 中心から 1.15 倍まで膨らんで戻る。足元は留める</summary>
        public static void Puff(RectTransform rt, float dur = 0.36f, float amount = 1.15f)
        {
            if (rt == null) return;
            var origin = rt.anchoredPosition; float h = rt.rect.height; float pivotY = rt.pivot.y;
            Run(dur, k =>
            {
                if (rt == null) return;
                float a = Mathf.Sin(k * Mathf.PI);
                float sc = 1f + (amount - 1f) * a;
                rt.localScale = new Vector3(sc, sc, 1f);
                rt.anchoredPosition = origin + new Vector2(0f, -h * pivotY * (1f - sc));
            }, Ease.Linear, () => { if (rt != null) { rt.localScale = Vector3.one; rt.anchoredPosition = origin; } });
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
        public static void BeamFx(RectTransform layer, Vector2 from, Vector2 to, Color color, float dur = 0.28f, float thick = 1f)
        {
            if (layer == null) return;
            var d = to - from; float len = d.magnitude; float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            var rt = UiKit.NewRect("beam", layer);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(len, 28f * thick); rt.anchoredPosition = from;
            rt.localRotation = Quaternion.Euler(0f, 0f, ang);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ThemeFx.SlashStreak(); img.color = color; img.raycastTarget = false;
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
                    Pop(layer, pos, ThemeFx.SlashBurst(), color, big ? 130f : 96f, 0.3f, 1.2f, 0.2f, 20f, 0.05f);
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
                    Pop(layer, pos, ThemeFx.SlashBurst(), color, big ? 120f : 90f, 0.3f, 1.2f, 0.2f, 15f, 0.03f);
                    Sparks(layer, pos, color, big ? 8 : 5, 180f);
                    break;
                case "blunt":
                    Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.95f, 0.8f), big ? 260f : 200f, 0.3f, 1.2f, 0.18f, 0f, 0f);
                    Streak(layer, pos + new Vector2(0f, 60f), -90f, color, 220f * (big ? 1.3f : 1f), 1f, 0.24f, 0f, 1.8f);
                    Pop(layer, pos, ThemeFx.SlashBurst(), color, big ? 170f : 130f, 0.3f, 1.4f, 0.24f, 0f, 0.02f);
                    RingBurst(layer, pos + new Vector2(0f, -40f), new Color(color.r, color.g, color.b, 0.7f), big ? 220f : 170f, 0.32f);
                    Sparks(layer, pos, color, big ? 10 : 6, -90f);
                    break;
                case "beam":
                    Pop(layer, pos, ThemeFx.Glow(), new Color(1f, 1f, 0.95f, 0.9f), big ? 240f : 190f, 0.2f, 1.3f, 0.22f, 0f, 0f);
                    Streak(layer, pos, 90f, color, 160f, 0.9f, 0.22f, 0f, 1.2f);
                    Streak(layer, pos, -90f, color, 160f, 0.9f, 0.22f, 0f, 1.2f);
                    Pop(layer, pos, ThemeFx.SlashBurst(), color, big ? 150f : 110f, 0.3f, 1.3f, 0.22f, 30f, 0.02f);
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

        /// <summary>踏み込み: 前へ出て戻る (敵の攻撃・自分の攻撃)</summary>
        public static void Lunge(RectTransform rt, Vector2 dir, float dur = 0.28f)
        {
            if (rt == null) return;
            var origin = rt.anchoredPosition;
            Move(rt, origin + dir, dur * 0.35f, Ease.OutQuad, () => { if (rt != null) Move(rt, origin, dur * 0.65f, Ease.OutCubic); });
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
