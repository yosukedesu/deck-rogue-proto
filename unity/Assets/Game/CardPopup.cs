// CardPopup.cs — カードの拡大表示 (本家 SingleCardViewPopup 相当。2026-09-09 ユーザー「強化後はゲーム中いつでも確認できるカードの基本機能」)。
// どの画面の札でも 長押し (0.5秒) か右クリック (戦闘の手札は右クリック=伏せるなので長押しだけ) で開き、
// 札を 1.6 倍で見せ、鍛えられる札には「鍛えた後を見る」の切り替え、右に用語の説明を添える。外側を触ると閉じる。
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class CardPopup
    {
        public const float Scale = 1.6f;
        public const float LongPressSec = 0.5f;

        static RectTransform _root;
        static RectTransform _cardHost;
        static Button _toggle;
        static TMP_Text _toggleLabel;
        static CardInstance _card;
        static GameState _state;
        static bool _showUpgraded;

        /// <summary>長押しで開いた直後は、指を離した時の PointerClick (プレイ・取る) を無視する期限</summary>
        public static float SuppressClicksUntil;
        public static bool ClickSuppressed { get { return Time.unscaledTime < SuppressClicksUntil; } }
        public static bool IsOpen { get { return _root != null; } }

        /// <summary>札の根 (raycast を受ける Image が子か自身にあること) に、長押し／右クリックで開く挙動を付ける</summary>
        public static void Attach(GameRoot g, RectTransform target, CardInstance c, Func<GameState> state, bool rightClick)
        {
            if (target == null || c == null) return;
            var lp = target.gameObject.AddComponent<LongPressOpen>();
            lp.G = g; lp.Card = c; lp.State = state; lp.RightClick = rightClick;
        }

        public static void Open(GameRoot g, CardInstance c, GameState st)
        {
            Close();
            if (g == null || g.FxLayer == null || c == null) return;
            _card = c; _state = st; _showUpgraded = false;
            // 入力を受ける最前面の層に (FxLayer は raycast を通さない)。戦闘の層が後から作られても上に来るよう開くたびに最後尾へ
            var layer = g.PopupLayer != null ? g.PopupLayer : g.FxLayer;
            layer.SetAsLastSibling();
            _root = UiKit.NewRect("cardPopup", layer);
            UiKit.Stretch(_root, 0f, 0f, 0f, 0f);
            // 暗幕 (触ると閉じる)
            var dim = UiKit.Pan(_root, new Color(20f / 255f, 18f / 255f, 40f / 255f, 0.72f), "dim");
            UiKit.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
            dim.raycastTarget = true;
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(delegate { Close(); });

            // 札 (1.6倍)。中央より少し左
            _cardHost = UiKit.NewRect("cardHost", _root);
            _cardHost.anchorMin = _cardHost.anchorMax = new Vector2(0.5f, 0.5f);
            _cardHost.sizeDelta = new Vector2(CardView.W * Scale, CardView.H * Scale);
            _cardHost.anchoredPosition = new Vector2(-120f, 30f);
            BuildCard();

            // 右: 用語の説明 (紙)
            string keys = null;
            try { keys = BattleScreen.KeywordsOnly(CardText.Body(c.Def) + " " + CardText.Notes(c.Def)); } catch (Exception) { }
            if (!string.IsNullOrEmpty(keys))
            {
                var pan = PaperFx.Sheet(_root, PaperFx.Panel, "keys");
                pan.rectTransform.anchorMin = pan.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                pan.rectTransform.pivot = new Vector2(0f, 0.5f);
                pan.rectTransform.anchoredPosition = new Vector2(80f, 30f);
                var t = UiKit.Txt(pan.transform, keys, 15, PaperFx.Ink, TextAnchor.UpperLeft);
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0f, 1f);
                t.rectTransform.pivot = new Vector2(0f, 1f);
                t.rectTransform.anchoredPosition = new Vector2(16f, -14f);
                t.rectTransform.sizeDelta = new Vector2(340f, 0f);
                t.ForceMeshUpdate();
                float h = Mathf.Max(60f, t.preferredHeight + 28f);
                t.rectTransform.sizeDelta = new Vector2(340f, h - 28f);
                pan.rectTransform.sizeDelta = new Vector2(372f, h);
            }

            // 下: 鍛えた後の切り替え (本家の「強化」チェック) と閉じる
            var row = UiKit.NewRect("row", _root);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(CardView.W * Scale + 40f, 52f);
            row.anchoredPosition = new Vector2(-120f, 30f - CardView.H * Scale / 2f - 40f);
            var hg = UiKit.Horz(row, 12, 0);
            hg.childAlignment = TextAnchor.MiddleCenter; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            bool canUp = false;
            try { canUp = Upgrade.CanUpgradeCard(c); } catch (Exception) { }
            if (canUp)
            {
                _toggle = UiKit.Btn(row, "鍛えた後を見る", delegate { _showUpgraded = !_showUpgraded; BuildCard(); UpdateToggle(); }, 17, true, UiKit.Hex("#f0d58a"));
                _toggleLabel = _toggle.GetComponentInChildren<TMP_Text>();
                UiKit.Le(_toggle, 190f, 46f, 190f, 46f);
            }
            else
            {
                bool upg = false;
                try { upg = Upgrade.IsUpgraded(c); } catch (Exception) { }
                var note = UiKit.Txt(row, upg ? "鍛え済み" : "鍛えられない札", 15, UiKit.ColDim, TextAnchor.MiddleCenter);
                note.outlineWidth = 0.3f; note.outlineColor = new Color(0.05f, 0.03f, 0.06f, 0.95f);
                UiKit.Le(note, 150f, 46f, 150f, 46f);
            }
            var close = UiKit.Btn(row, "閉じる", delegate { Close(); }, 17);
            UiKit.Le(close, 120f, 46f, 120f, 46f);
            Audio.Play("click", 0.4f, 0.1f);
        }

        static void BuildCard()
        {
            if (_cardHost == null) return;
            for (int i = _cardHost.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_cardHost.GetChild(i).gameObject);
            CardInstance shown = _card;
            if (_showUpgraded)
            {
                try { shown = Upgrade.UpgradeCard(_card); } catch (Exception) { shown = _card; }
            }
            CardView.PreviewEnemy = -1;
            var cv = CardView.Build(_cardHost, shown, _state, true, false, "popup-card");
            cv.localScale = Vector3.one * Scale;
            cv.anchoredPosition = Vector2.zero;
            Tween.Punch(_cardHost, 0.05f, 0.4f);
        }

        static void UpdateToggle()
        {
            if (_toggleLabel != null) _toggleLabel.text = _showUpgraded ? "元の札を見る" : "鍛えた後を見る";
            if (_toggle != null)
            {
                var img = _toggle.GetComponent<Image>();
                if (img != null) img.color = _showUpgraded ? UiKit.Hex("#cfeacc") : UiKit.Hex("#f0d58a");
            }
        }

        public static void Close()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            _root = null; _cardHost = null; _toggle = null; _toggleLabel = null; _card = null; _state = null;
        }

        /// <summary>長押し (0.5秒・動かさない) と右クリックで開く。ドラッグが始まったら取り消す</summary>
        public class LongPressOpen : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IBeginDragHandler, IPointerClickHandler
        {
            public GameRoot G;
            public CardInstance Card;
            public Func<GameState> State;
            public bool RightClick;
            bool _down;
            float _downAt;
            Vector2 _downPos;

            public void OnPointerDown(PointerEventData e)
            {
                if (e.button != PointerEventData.InputButton.Left) return;
                _down = true; _downAt = Time.unscaledTime; _downPos = e.position;
            }
            public void OnPointerUp(PointerEventData e) { _down = false; }
            public void OnPointerExit(PointerEventData e) { _down = false; }
            public void OnBeginDrag(PointerEventData e) { _down = false; }
            public void OnPointerClick(PointerEventData e)
            {
                if (RightClick && e.button == PointerEventData.InputButton.Right) OpenNow();
            }
            void Update()
            {
                if (!_down) return;
                if (Time.unscaledTime - _downAt < LongPressSec) return;
                _down = false;
                SuppressClicksUntil = Time.unscaledTime + 0.6f;
                OpenNow();
            }
            void OpenNow()
            {
                GameState st = null;
                try { st = State != null ? State() : null; } catch (Exception) { }
                Open(G, Card, st);
            }
        }
    }
}
