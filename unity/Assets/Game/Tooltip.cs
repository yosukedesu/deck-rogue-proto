// Tooltip.cs — ホバーで出る吹き出し (2026-09-07 M2)。FxLayer に1つだけ生成し、要素ごとに文面の関数を登録する。
// 用語解説 (KeywordHelp.g.cs) は本文に含まれる用語を拾って下に並べる。
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static class Tooltip
    {
        static RectTransform _panel;
        static TMP_Text _text;
        static RectTransform _layer;

        /// <summary>スマホでタップして開いた説明の主 (もう一度タップで閉じる。別の所を触っても閉じる)</summary>
        static GameObject _pinnedTarget;
        static bool _pinned;

        static string Compose(Func<string> textProvider, bool withKeywords)
        {
            string body = null;
            try { body = textProvider(); } catch (Exception) { }
            if (string.IsNullOrEmpty(body)) return null;
            if (withKeywords)
            {
                var terms = KeywordHelp.FindIn(body);
                for (int i = 0; i < terms.Count && i < 4; i++)
                    body += "\n<color=#276a34><b>" + terms[i] + "</b></color> <color=#574b48>" + KeywordHelp.Terms[terms[i]] + "</color>";   // 紙の上で 5.5:1 / 7:1
            }
            return body;
        }

        /// <summary>要素にツールチップを付ける。textProvider は表示時に呼ぶ (状態を後から読む)。
        /// PC はホバーで指の横に、スマホ (UiKit.Phone) はタップで画面上部の固定パネルに出す (指の下に出さない。2026-09-14 ユーザー裁定)</summary>
        public static void Attach(GameObject target, Func<string> textProvider, bool withKeywords = true)
        {
            if (target == null) return;
            var et = target.GetComponent<EventTrigger>();
            if (et == null) et = target.AddComponent<EventTrigger>();
            if (UiKit.Phone)
            {
                var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                click.callback.AddListener(delegate (BaseEventData d)
                {
                    if (_pinned && _pinnedTarget == target) { Hide(); return; }
                    var body = Compose(textProvider, withKeywords);
                    if (body == null) return;
                    ShowPinned(body, target);
                });
                et.triggers.Add(click);
                return;
            }
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(delegate (BaseEventData d)
            {
                var body = Compose(textProvider, withKeywords);
                if (body == null) return;
                var pd = d as PointerEventData;
                Show(body, pd != null ? pd.position : (Vector2)Input.mousePosition);
            });
            et.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(delegate { Hide(); });
            et.triggers.Add(exit);
        }

        /// <summary>画面上部 (上部バーの下・中央) の固定パネル。幅は広め・文字は大きめ。押した主でない所を触ると閉じる (GameRoot.Update から Tick)</summary>
        public static void ShowPinned(string text, GameObject target)
        {
            var g = GameRoot.I;
            if (g == null || g.FxLayer == null) return;
            EnsurePanel(g);
            _pinned = true; _pinnedTarget = target;
            _panel.SetAsLastSibling();
            _panel.gameObject.SetActive(true);
            _text.fontSize = 18;
            _text.text = text;
            float maxW = Mathf.Min(900f, _layer.rect.width - 48f);
            _text.rectTransform.sizeDelta = new Vector2(maxW - 32f, 2000f);
            _text.ForceMeshUpdate();
            float w = Mathf.Clamp(_text.preferredWidth + 32f, 320f, maxW);
            _text.rectTransform.sizeDelta = new Vector2(w - 32f, 2000f);
            _text.ForceMeshUpdate();
            float h = _text.preferredHeight + 24f;
            _text.rectTransform.sizeDelta = new Vector2(w - 32f, h - 24f);
            _panel.sizeDelta = new Vector2(w, h);
            float halfH = _layer.rect.height / 2f, halfW = _layer.rect.width / 2f;
            _panel.anchoredPosition = new Vector2(-halfW + 24f, halfH - BattleScreen.TopH - 10f);   // 左上 (敵の吹き出しは中央〜右にある)
        }

        /// <summary>スマホ: 押した主の外を触ったら閉じる (触った操作は素通し = もう一度押させない)</summary>
        public static void Tick()
        {
            if (!_pinned || _panel == null || !_panel.gameObject.activeSelf) { _pinned = false; return; }
            if (_pinnedTarget == null) { Hide(); return; }
            bool down = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            if (!down) return;
            Vector2 pos = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            var es = EventSystem.current;
            if (es == null) { Hide(); return; }
            var pd = new PointerEventData(es) { position = pos };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            es.RaycastAll(pd, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                var t = hits[i].gameObject != null ? hits[i].gameObject.transform : null;
                for (; t != null; t = t.parent) if (t.gameObject == _pinnedTarget) return;   // 主を押した (PointerClick 側で閉じる)
            }
            Hide();
        }

        static void EnsurePanel(GameRoot g)
        {
            if (_panel != null && _layer == g.FxLayer) return;
            _layer = g.FxLayer;
            _panel = UiKit.NewRect("tooltip", _layer);
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0f, 1f);
            var bg = _panel.gameObject.AddComponent<Image>();
            bg.sprite = Theme.Panel;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
            bg.pixelsPerUnitMultiplier = 1f;
            bg.raycastTarget = false;
            _text = UiKit.Txt(_panel, "", 15, UiKit.ColInk, TextAnchor.UpperLeft);
            _text.rectTransform.anchorMin = _text.rectTransform.anchorMax = new Vector2(0f, 1f);
            _text.rectTransform.pivot = new Vector2(0f, 1f);
            _text.rectTransform.anchoredPosition = new Vector2(16f, -12f);
            var cg = _panel.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        public static void Show(string text, Vector2 screenPos)
        {
            var g = GameRoot.I;
            if (g == null || g.FxLayer == null) return;
            EnsurePanel(g);
            _pinned = false; _pinnedTarget = null;
            _panel.SetAsLastSibling();
            _panel.gameObject.SetActive(true);
            _text.fontSize = 15;
            _text.text = text;
            _text.rectTransform.sizeDelta = new Vector2(488f, 2000f);
            _text.ForceMeshUpdate();
            float w = Mathf.Clamp(_text.preferredWidth + 32f, 220f, 520f);
            _text.rectTransform.sizeDelta = new Vector2(w - 32f, 2000f);
            _text.ForceMeshUpdate();
            float h = _text.preferredHeight + 24f;
            _text.rectTransform.sizeDelta = new Vector2(w - 32f, h - 24f);
            _panel.sizeDelta = new Vector2(w, h);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screenPos, null, out local);
            // 右下に出し、画面からはみ出すなら左/上へ折り返す
            float half = _layer.rect.width / 2f;
            float halfH = _layer.rect.height / 2f;
            float x = local.x + 18f;
            float y = local.y - 18f;
            if (x + w > half) x = local.x - 18f - w;
            if (y - h < -halfH) y = local.y + 18f + h;
            _panel.anchoredPosition = new Vector2(x, y);
            if (Application.isBatchMode || System.Environment.GetCommandLineArgs().Length > 1) Debug.Log("[Tooltip] show " + text.Length + "字 @" + x.ToString("F0") + "," + y.ToString("F0") + " size " + w.ToString("F0") + "x" + h.ToString("F0"));
        }

        public static void Hide()
        {
            _pinned = false; _pinnedTarget = null;
            if (_panel != null) _panel.gameObject.SetActive(false);
        }
    }
}
