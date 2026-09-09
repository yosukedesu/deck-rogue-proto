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

        /// <summary>dur 秒かけて 0→1 を onUpdate に渡す (実時間。ゲーム内の一時停止に影響されない)</summary>
        public static Coroutine Run(float dur, Action<float> onUpdate, Ease ease = Ease.OutQuad, Action onDone = null)
        {
            return I.StartCoroutine(RunCo(dur, onUpdate, ease, onDone));
        }

        static IEnumerator RunCo(float dur, Action<float> onUpdate, Ease ease, Action onDone)
        {
            float t0 = Time.unscaledTime;
            if (dur <= 0f) { onUpdate(1f); onDone?.Invoke(); yield break; }
            while (true)
            {
                float k = (Time.unscaledTime - t0) / dur;
                if (k >= 1f) break;
                onUpdate(Apply(ease, k));
                yield return null;
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
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            action?.Invoke();
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
