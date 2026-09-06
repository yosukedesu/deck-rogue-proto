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

        /// <summary>要素にツールチップを付ける。textProvider は表示時に呼ぶ (状態を後から読む)</summary>
        public static void Attach(GameObject target, Func<string> textProvider, bool withKeywords = true)
        {
            if (target == null) return;
            var et = target.GetComponent<EventTrigger>();
            if (et == null) et = target.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(delegate (BaseEventData d)
            {
                string body = null;
                try { body = textProvider(); } catch (Exception) { }
                if (string.IsNullOrEmpty(body)) return;
                if (withKeywords)
                {
                    var terms = KeywordHelp.FindIn(body);
                    for (int i = 0; i < terms.Count && i < 4; i++)
                        body += "\n<color=#8fd08c><b>" + terms[i] + "</b></color> <color=#c8d0c8>" + KeywordHelp.Terms[terms[i]] + "</color>";
                }
                var pd = d as PointerEventData;
                Show(body, pd != null ? pd.position : (Vector2)Input.mousePosition);
            });
            et.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(delegate { Hide(); });
            et.triggers.Add(exit);
        }

        public static void Show(string text, Vector2 screenPos)
        {
            var g = GameRoot.I;
            if (g == null || g.FxLayer == null) return;
            if (_panel == null || _layer != g.FxLayer)
            {
                _layer = g.FxLayer;
                _panel = UiKit.NewRect("tooltip", _layer);
                _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
                _panel.pivot = new Vector2(0f, 1f);
                var bg = _panel.gameObject.AddComponent<Image>();
                bg.sprite = Theme.Panel;
                bg.type = Image.Type.Sliced;
                bg.pixelsPerUnitMultiplier = 1f / 3f;
                bg.color = new Color(0.9f, 0.95f, 0.9f, 0.98f);
                bg.raycastTarget = false;
                _text = UiKit.Txt(_panel, "", 16, UiKit.ColText, TextAnchor.UpperLeft);
                UiKit.Stretch(_text.rectTransform, 16f, 16f, 12f, 12f);
                var cg = _panel.gameObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
            _panel.SetAsLastSibling();
            _panel.gameObject.SetActive(true);
            _text.text = text;
            _text.ForceMeshUpdate();
            float w = Mathf.Clamp(_text.preferredWidth + 32f, 220f, 520f);
            _text.rectTransform.sizeDelta = new Vector2(w - 32f, 0f);
            _text.ForceMeshUpdate();
            float h = _text.preferredHeight + 24f;
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
        }

        public static void Hide()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
        }
    }
}
