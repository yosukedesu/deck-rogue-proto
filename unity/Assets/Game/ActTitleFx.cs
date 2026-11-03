// ActTitleFx.cs — 幕の題字 (2026-11-04 見た目・UX・演出の磨き 束 C2・G5/M10)。
// ランの開始と幕が変わった手で、夜色の板が画面を覆い (0.16)、覆った下で画面を組み直し、板の上に「第一幕」と幕の名前と真鍮の罫が浮かんで
// (0.3 浮かぶ・1.0 保つ・0.25 消える)、入りの動きを付けて板が開く (0.22)。ScreenFx.Switch の幕と同じ形で、閉じている間に題字を挟む。
// 押せば飛ばせる (題字の保ちを切る)。撮影 (ScreenFx.Instant) では出さず ScreenFx.Switch に任せる。板は幕 (ScreenFx の curtain) と別の物なので
// ScreenFx は触らない。時計は Tween.UnscaledDt (幕と同じ)。秒数は FxSettings.Dur を掛ける (速い設定で短く)。
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public class ActTitleFx : MonoBehaviour
    {
        const float InDur = 0.16f, OpenDur = 0.22f;
        const float TitleIn = 0.3f, TitleHold = 1.0f, TitleOut = 0.25f;

        /// <summary>題字が出ている (この間は板が入力を塞ぐ)。Autopilot が待てる口</summary>
        public static bool Busy { get { return _busy; } }
        static bool _busy;
        static ActTitleFx _i;

        static ActTitleFx I
        {
            get
            {
                if (_i == null)
                {
                    var go = new GameObject("ActTitleFx");
                    DontDestroyOnLoad(go);
                    _i = go.AddComponent<ActTitleFx>();
                }
                return _i;
            }
        }

        static float Dt { get { return Tween.UnscaledDt; } }

        /// <summary>幕の番号の和語 (第一幕／第二幕／第三幕)</summary>
        public static string Numeral(int act)
        {
            switch (act) { case 1: return "第一幕"; case 2: return "第二幕"; case 3: return "第三幕"; default: return "第" + act + "幕"; }
        }

        /// <summary>幕の題字つきで画面を切り替える。Instant・板を作れない・題字がもう出ている時は ScreenFx.Switch へ</summary>
        public static void Show(GameRoot g, int act, Action rebuild)
        {
            if (ScreenFx.Instant || g == null || g.ScreenRoot == null || g.ScreenRoot.parent == null || _busy) { ScreenFx.Switch(g, rebuild); return; }
            I.StartCoroutine(I.Co(g, act, rebuild));
        }

        static bool Tap()
        {
            return Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        }

        IEnumerator Co(GameRoot g, int act, Action rebuild)
        {
            _busy = true;
            bool ph = UiKit.Phone;
            var canvas = g.ScreenRoot.parent;
            var rt = UiKit.NewRect("act-title", canvas);
            UiKit.Stretch(rt, -4f, -4f, -4f, -4f);   // 端の 1px の隙間を出さない (幕と同じ)
            var board = rt.gameObject.AddComponent<Image>();
            board.color = new Color(PaperFx.Night.r, PaperFx.Night.g, PaperFx.Night.b, 0f);
            board.raycastTarget = true;   // 題字の間は入力を塞ぐ
            rt.SetAsLastSibling();
            ScreenFx.FinishEntries();

            // 閉じる (InQuad 0.16)
            float t = 0f;
            while (t < InDur)
            {
                board.color = new Color(PaperFx.Night.r, PaperFx.Night.g, PaperFx.Night.b, Tween.Apply(Ease.InQuad, t / InDur));
                t += Dt; yield return null;
            }
            board.color = new Color(PaperFx.Night.r, PaperFx.Night.g, PaperFx.Night.b, 1f);
            try { rebuild(); } catch (Exception e) { Debug.LogException(e); }
            yield return null;   // 板の下でレイアウトを 1 フレーム落ち着かせる
            rt.SetAsLastSibling();

            // 題字: 小さな真鍮の「第一幕」・幕の名前・真鍮の罫
            var holder = UiKit.NewRect("title", rt);
            UiKit.Stretch(holder, 0f, 0f, 0f, 0f);
            var cg = holder.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false;
            int nameSize = ph ? 44 : 60;
            var small = UiKit.Deco(holder, Numeral(act), ph ? 18 : 22, PaperFx.BrassLight, TextAnchor.MiddleCenter);
            small.characterSpacing = 12f; small.raycastTarget = false;
            UiKit.Anchor(small.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, nameSize * 0.6f + 16f), new Vector2(0f, nameSize * 0.6f + 52f));
            var name = UiKit.Deco(holder, MapScreen.ActName(act), nameSize, PaperFx.Paper, TextAnchor.MiddleCenter);
            name.characterSpacing = 14f; name.raycastTarget = false;
            UiKit.Anchor(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -nameSize * 0.6f - 8f), new Vector2(0f, nameSize * 0.6f + 8f));
            var rule = UiKit.Pan(holder, PaperFx.Brass, "rule");
            rule.raycastTarget = false;
            rule.rectTransform.anchorMin = rule.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rule.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rule.rectTransform.anchoredPosition = new Vector2(0f, -nameSize * 0.6f - 26f);
            rule.rectTransform.sizeDelta = new Vector2(0f, 2f);
            float ruleW = ph ? 200f : 260f;
            Audio.Key("act_title");

            // 浮かぶ (0.3): α 0→1・8px 上がる・罫が伸びる
            float tin = FxSettings.Dur(TitleIn), hold = FxSettings.Dur(TitleHold), tout = FxSettings.Dur(TitleOut);
            var p0 = holder.anchoredPosition;
            t = 0f;
            bool skipped = false;
            while (t < tin)
            {
                float k = Tween.Apply(Ease.OutCubic, t / tin);
                cg.alpha = Mathf.Clamp01(t / tin * 1.4f);
                holder.anchoredPosition = p0 - new Vector2(0f, 8f * (1f - k));
                rule.rectTransform.sizeDelta = new Vector2(ruleW * k, 2f);
                t += Dt; yield return null;
                if (Tap()) { skipped = true; break; }
            }
            cg.alpha = 1f; holder.anchoredPosition = p0; rule.rectTransform.sizeDelta = new Vector2(ruleW, 2f);
            // 保つ (1.0)。押したら切る
            t = 0f;
            while (!skipped && t < hold) { t += Dt; yield return null; if (Tap()) break; }
            // 消える (0.25)
            t = 0f;
            while (t < tout) { cg.alpha = 1f - Tween.Apply(Ease.OutQuad, t / tout); t += Dt; yield return null; }
            Destroy(holder.gameObject);

            // 開く (OutQuad 0.22)。入りの動きは幕と同じく戦闘以外に
            if (g.ScreenRoot != null && !(g.Rs != null && g.Rs.Phase == DeckRogue.Engine.Generated.RunPhases.Combat)) ScreenFx.BeginEntries(g.ScreenRoot);
            rt.SetAsLastSibling();
            t = 0f;
            while (t < OpenDur)
            {
                board.color = new Color(PaperFx.Night.r, PaperFx.Night.g, PaperFx.Night.b, 1f - Tween.Apply(Ease.OutQuad, t / OpenDur));
                t += Dt; yield return null;
            }
            Destroy(rt.gameObject);
            _busy = false;
        }
    }
}
