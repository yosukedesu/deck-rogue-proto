// RunUi.cs — 戦闘以外の画面で共有する部品 (2026-09-07 M3): 上部バー・デッキ一覧・レリック帯・見出し
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class RunUi
    {
        public const float TopH = 72f;

        /// <summary>ランの上部バー: 幕/行・HP・ゴールド・デッキ (押すと一覧)・レリック (ホバーで説明)</summary>
        public static void TopBar(GameRoot g, RectTransform root, string title)
        {
            var run = g.Rs;
            var bar = UiKit.NewRect("topbar", root);
            UiKit.Anchor(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -TopH), new Vector2(0f, 0f));
            var hg = UiKit.Horz(bar, 12, 0);
            hg.padding = new RectOffset(28, 28, 0, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandHeight = false;
            hg.childForceExpandWidth = false;

            var leaderIcon = Theme.Art("leaders", run.LeaderId + "_icon");   // 32 ドットの顔 (2026-09-11)。2倍=64 で上部バーの左端に
            if (leaderIcon != null)
            {
                var li = new GameObject("leader-icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                li.transform.SetParent(bar, false);
                li.sprite = leaderIcon; li.preserveAspect = true; li.raycastTarget = false;
                UiKit.Le(li.rectTransform, 64f, 64f, 64f, 64f);
                string ltip = run.LeaderId;
                try { var lname = Content.GetLeaderDef(run.LeaderId); if (lname != null) ltip = "<b>" + lname.Name + "</b>"; } catch (Exception) { }
                Tooltip.Attach(li.gameObject, delegate { return ltip; });
            }
            var t1 = BattleScreen.Tag(bar, 40f, -0.6f);
            var tl = UiKit.Txt(t1, "幕 " + run.Act + " · 行 " + (run.Row + 1) + " / " + run.Map.Count, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            tl.characterSpacing = 2f;
            UiKit.Le(tl, -1f, 30f, -1f, 30f);
            var te = UiKit.Deco(t1, title, 19, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(te, -1f, 30f, -1f, 30f);

            // 真ん中の空き (スマホの見出しはここに畳む。2026-09-16 案A: 見出し2行の高さを一覧に返す)
            var spacer = UiKit.NewRect("center", bar);
            UiKit.Le(spacer, 10f, 10f, -1f, -1f, 1f, -1f);
            var chg = UiKit.Horz(spacer, 0, 0);
            chg.childAlignment = TextAnchor.MiddleCenter; chg.childForceExpandWidth = false; chg.childForceExpandHeight = false;

            var hp = BattleScreen.Tag(bar, 36f, 0.6f);
            UiKit.Icon(hp, "heart", 16f);
            var ht = UiKit.Deco(hp, run.Hp.ToString(), 18, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(ht, -1f, 30f, -1f, 30f);
            var hm = UiKit.Txt(hp, "/ " + run.MaxHp, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(hm, -1f, 30f, -1f, 30f);
            var gold = BattleScreen.Tag(bar, 36f, -0.4f);
            UiKit.Icon(gold, "gold", 16f);
            var gt = UiKit.Deco(gold, run.Gold.ToString(), 18, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(gt, -1f, 30f, -1f, 30f);
            var gl = UiKit.Txt(gold, "G", 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(gl, -1f, 30f, -1f, 30f);
            if (UiKit.Phone)
            {   // スマホ: デッキ以外は「≡」に畳む (2026-09-14)
                var deckBtnP = UiKit.Btn(bar, "デッキ " + run.Deck.Count, delegate { g.ViewDeck = !g.ViewDeck; g.ViewMap = false; g.Rebuild(); }, 13);
                BattleScreen.SetSize(deckBtnP, 110f, 40f);
                MenuButton(g, bar);
            }
            else
            {
                if (run.Phase != RunPhases.Map)
                {   // マップの常時閲覧 (2026-09-12 ユーザー「マップは常に見れるようにして」)
                    var mapBtn = UiKit.Btn(bar, "マップ", delegate { g.ViewMap = !g.ViewMap; g.ViewDeck = false; g.Rebuild(); }, 13);
                    BattleScreen.SetSize(mapBtn, 84f, 36f);
                }
                var deckBtn = UiKit.Btn(bar, "デッキ " + run.Deck.Count, delegate { g.ViewDeck = !g.ViewDeck; g.ViewMap = false; g.Rebuild(); }, 13);
                BattleScreen.SetSize(deckBtn, 110f, 36f);
                FeedbackUi.TopBarButtons(g, bar);   // メモ・レポート (2026-09-14)
                MenuButton(g, bar);   // PC も「≡」を持つ: セーブして終了・ランを放棄 (2026-09-15)
            }

            int relicMax = UiKit.Phone ? 6 : 10;
            for (int i = 0; i < run.Relics.Count && i < relicMax; i++)
            {
                RelicDef rd = null;
                try { rd = Content.GetRelicDef(run.Relics[i]); } catch (Exception) { }
                var cell = UiKit.NewRect("relic", bar);
                UiKit.Le(cell, 34f, 34f, 34f, 34f);
                var disc = cell.gameObject.AddComponent<Image>();
                disc.sprite = PaperFx.Disc(); disc.preserveAspect = true;
                RelicArt(cell, run.Relics[i], 20f);
                var tip = rd != null ? "<b>" + rd.Name + "</b>\n" + rd.Description : run.Relics[i];
                Tooltip.Attach(cell.gameObject, delegate { return tip; });
            }
        }

        /// <summary>スマホの「≡」(2026-09-14): 上部バーの右端。押すと Menu が画面の右上に開く</summary>
        public static void MenuButton(GameRoot g, Transform bar)
        {
            var b = UiKit.Btn(bar, g.MenuOpen ? "×" : "≡", delegate { g.MenuOpen = !g.MenuOpen; g.Rebuild(); }, 22, true, g.MenuOpen ? UiKit.Hex("#f0d58a") : (Color?)null);
            BattleScreen.SetSize(b, 56f, 44f);
        }

        /// <summary>スマホのメニュー (≡ の中身): マップ・ログ (戦闘)・メモ・レポート。外側を触ると閉じる</summary>
        public static void Menu(GameRoot g, RectTransform root)
        {
            var catcher = UiKit.Pan(root, new Color(0f, 0f, 0f, 0.25f), "menu-catcher");
            UiKit.Stretch(catcher.rectTransform, 0f, 0f, 0f, 0f);
            var cb = catcher.gameObject.AddComponent<Button>();
            cb.transition = Selectable.Transition.None;
            cb.onClick.AddListener(delegate { g.MenuOpen = false; g.Rebuild(); });
            bool combat = g.Rs != null && g.Rs.Phase == RunPhases.Combat;
            var items = new List<KeyValuePair<string, Action>>();
            if (g.Rs != null && g.Rs.Phase != RunPhases.Map) items.Add(new KeyValuePair<string, Action>("マップを見る", delegate { g.MenuOpen = false; g.ViewMap = true; g.ViewDeck = false; g.Rebuild(); }));
            if (combat) items.Add(new KeyValuePair<string, Action>(g.ShowLog ? "ログを閉じる" : "戦闘ログ", delegate { g.MenuOpen = false; g.ShowLog = !g.ShowLog; g.Rebuild(); }));
            items.Add(new KeyValuePair<string, Action>(Feedback.Notes.Count > 0 ? "メモを書く（" + Feedback.Notes.Count + "件）" : "メモを書く", delegate { g.MenuOpen = false; Feedback.MemoOpen = true; g.Rebuild(); }));
            items.Add(new KeyValuePair<string, Action>("レポートを書き出す", delegate { g.MenuOpen = false; FeedbackUi.ExportNow(g); }));
            // セーブ (2026-09-15 本家形): 自動保存なので「セーブする」は無い。終了と放棄だけ
            bool ended = g.Rs != null && (g.Rs.Phase == RunPhases.Won || g.Rs.Phase == RunPhases.Lost);
            int saveFrom = items.Count;
            if (!ended)
            {
                items.Add(new KeyValuePair<string, Action>("セーブして終了", delegate { g.MenuOpen = false; g.SaveAndQuit(); }));
                items.Add(new KeyValuePair<string, Action>("ランを放棄", delegate { g.AskAbandonRun(); }));
            }
            float w = 360f, itemH = 56f, pad = 14f;
            float h = pad * 2f + items.Count * (itemH + 8f) - 8f + (ended ? 0f : 12f);
            var pan = UiKit.Frame(root, Theme.Panel, Color.white, "menu", 3f);
            UiKit.Anchor(pan.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-w - 16f, -TopH - 8f - h), new Vector2(-16f, -TopH - 8f));
            var inner = UiKit.NewRect("inner", pan.transform);
            UiKit.Stretch(inner, pad, pad, pad, pad);
            UiKit.Vert(inner, 8, 0);
            for (int i = 0; i < items.Count; i++)
            {
                if (!ended && i == saveFrom)
                {   // 区切りの線 (上=見る/書く・下=終える)
                    var rule = UiKit.Pan(inner, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.25f), "rule");
                    UiKit.Le(rule, -1f, 4f, -1f, 4f);
                }
                var act = items[i].Value;
                bool danger = !ended && i == items.Count - 1;
                var b = UiKit.Btn(inner, items[i].Key, delegate { act(); }, 18, true, danger ? UiKit.Hex("#e8b8b0") : (Color?)null);
                UiKit.Le(b, -1f, itemH, -1f, itemH);
            }
        }

        /// <summary>
        /// 確認ダイアログ (2026-09-15): ランの放棄・進行中のランを捨てて新しく始める・別のデータ版のセーブ。
        /// 「はい」で OnOk、外側/キャンセルで閉じる。どの画面の最後にも重ねる (GameRoot.Confirm)
        /// </summary>
        public static void ConfirmDialog(GameRoot g, RectTransform root)
        {
            var c = g.Confirm;
            if (c == null) return;
            var inner = BattleScreen.Modal(root, 760f, 300f, "confirm");
            UiKit.Head(inner, c.Title ?? "確認", 24);
            var msg = UiKit.Txt(inner, c.Message ?? "", 17, UiKit.ColInk, TextAnchor.UpperLeft);
            msg.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Le(msg, -1f, 60f, -1f, -1f, -1f, 1f);
            var rows = UiKit.NewRect("btns", inner);
            UiKit.Le(rows, -1f, 56f, -1f, 56f);
            var hg = UiKit.Horz(rows, 14, 0);
            hg.childAlignment = TextAnchor.MiddleRight;
            hg.childForceExpandHeight = false;
            hg.childForceExpandWidth = false;
            var cancel = UiKit.Btn(rows, c.CancelLabel ?? "キャンセル", delegate { g.Confirm = null; g.Rebuild(); }, 18);
            BattleScreen.SetSize(cancel, 200f, 50f);
            var ok = UiKit.Btn(rows, c.OkLabel ?? "はい", delegate
            {
                var onOk = c.OnOk;
                g.Confirm = null;
                if (onOk != null) onOk(); else g.Rebuild();
            }, 18, true, c.Danger ? UiKit.Hex("#e8b8b0") : UiKit.Hex("#f0d58a"));
            BattleScreen.SetSize(ok, 240f, 50f);
        }

        /// <summary>エンジンの拒否理由・通知を画面上部に1行 (無ければ何も置かない)</summary>
        public static void Message(GameRoot g, RectTransform root)
        {
            string msg = g.Error != null ? "! " + g.Error : g.Notice;
            if (string.IsNullOrEmpty(msg)) return;
            var pan = UiKit.Frame(root, Theme.Panel, g.Error != null ? new Color(1f, 0.7f, 0.7f, 1f) : Color.white, "message", 3f);
            UiKit.Anchor(pan.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-460f, -TopH - 56f), new Vector2(460f, -TopH - 8f));
            pan.raycastTarget = false;
            var t = UiKit.Txt(pan.transform, msg, 16, g.Error != null ? UiKit.ColBadInk : UiKit.ColInk, TextAnchor.MiddleCenter, true);
            UiKit.Stretch(t.rectTransform, 12f, 12f, 0f, 0f);
            t.raycastTarget = false;
        }

        /// <summary>レリックの絵 (プレースホルダー: idから生成した紋様。PixelLab の絵が Resources/Art/relics/&lt;id&gt; にあればそれ)</summary>
        public static Image RelicArt(Transform parent, string relicId, float size)
        {
            var img = new GameObject("relic-art", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.raycastTarget = false;
            var art = Theme.Art("relics", relicId);
            img.sprite = art != null ? art : ThemeFx.RelicGlyph(relicId);
            img.preserveAspect = true;
            if (art != null) size = size <= 40f ? 32f : 64f;   // PixelLab のレリックは 32 ドット。整数倍で置く (2026-09-11)
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            return img;
        }

        /// <summary>画面下部の固定位置ボタン (レイアウトに属さない)。x は中心からのずれ</summary>
        public static Button BottomButton(RectTransform root, string label, Action onClick, int size, float w, float h, float x = 0f, float bottom = 40f, Color? bg = null, bool enabled = true)
        {
            var b = UiKit.Btn(root, label, onClick, size, enabled, bg);
            var le = b.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x - w / 2f, bottom), new Vector2(x + w / 2f, bottom + h));
            return b;
        }

        public static void Chip(Transform parent, string icon, string text, Color color, int size)
        {
            var row = UiKit.NewRect("chip-" + icon, parent);
            var hg = UiKit.Horz(row, 6, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandHeight = false;
            UiKit.Icon(row, icon, 32);
            var t = UiKit.Txt(row, text, size, color, TextAnchor.MiddleLeft, true);
            UiKit.Le(t, 40f, size + 8f, -1f, size + 8f);
            var fit = row.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>デッキ一覧 (カードのグリッド)。onPick があればカードの下にボタン。右上に「鍛えた後を見る」</summary>
        public static void DeckViewer(GameRoot g, RectTransform root)
        {
            var inner = BattleScreen.Modal(root, 1500f, 820f, "deckViewer");
            UiKit.Head(inner, "デッキ " + g.Rs.Deck.Count + "枚", 24);
            CardGrid(g, inner, g.Rs.Deck, null, null, null, 360f);
            BattleScreen.CenteredButton(inner, "閉じる", delegate { g.ViewDeck = false; g.Rebuild(); }, 18, 260f, 50f);
            UpgradeToggle(g, inner, 0f, 0f);
            var tg = inner.Find("upgrade-toggle") as RectTransform;
            if (tg != null)
            {   // 縦レイアウトの外に出して右上に
                var le = tg.gameObject.AddComponent<LayoutElement>(); le.ignoreLayout = true;
                UiKit.Anchor(tg, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-232f - 64f, -18f - 44f), new Vector2(-64f, -18f));   // しおり (右端 48) と重ねない
            }
        }

        /// <summary>デッキから選ぶ画面の一覧の置き場 (2026-09-16 案A): スマホは上部バーの下から下の帯 (チェック・確定・戻る) の上まで幅いっぱい。PC は従来 (幅 1520・見出しの下)</summary>
        public static void PickArea(RectTransform root, RectTransform area, float topExtra = 0f)
        {
            if (UiKit.Phone) UiKit.Anchor(area, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 74f), new Vector2(-20f, -(TopH + 10f)));
            else UiKit.Anchor(area, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-760f, 110f), new Vector2(760f, -(TopH + 110f + topExtra)));
            UiKit.Vert(area, 0, 0);
        }

        /// <summary>デッキから選ぶ画面の「戻る」: スマホは右下 (真ん中は確定ボタン・左下はチェック)、PC は真ん中の下</summary>
        public static Button BackButton(RectTransform root, string label, Action onClick)
        {
            return BottomButton(root, label, onClick, 18, 220f, UiKit.Phone ? 48f : 50f, UiKit.Phone ? BattleScreen.CanvasSize(root).x / 2f - 132f : 0f, UiKit.Phone ? 14f : 40f);
        }

        /// <summary>「鍛えた後を見る」のチェック (2026-09-16 ユーザー「デッキ一覧すべてで鍛えた後を見るボタン」。本家 Smith の Show Upgrade): 入れると一覧の全部の札が鍛えた後の姿に。
        /// 紙のボタンに墨の四角と文字。x/y は parent の左下からの位置 (幅 232・高さ 44)</summary>
        public static void UpgradeToggle(GameRoot g, RectTransform parent, float x, float y, float w = 232f, float h = 44f)
        {
            bool on = g.ShowUpgraded;
            var rt = UiKit.NewRect("upgrade-toggle", parent);
            UiKit.Anchor(rt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, y), new Vector2(x + w, y + h));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Button; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; img.color = on ? UiKit.Hex("#fbf6e8") : Color.white;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(delegate { Audio.Ui("click"); g.ShowUpgraded = !g.ShowUpgraded; g.Rebuild(); });
            var box = UiKit.NewRect("box", rt);
            UiKit.Anchor(box, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, -12f), new Vector2(36f, 12f));
            var bImg = box.gameObject.AddComponent<Image>();
            bImg.sprite = PaperFx.Tag; bImg.type = Image.Type.Sliced; bImg.pixelsPerUnitMultiplier = 1f; bImg.color = on ? PaperFx.Ink : UiKit.Hex("#fbf6e8"); bImg.raycastTarget = false;
            if (on)
            {
                var tick = UiKit.Txt(box, "✓", 18, UiKit.Hex("#f4ecd6"), TextAnchor.MiddleCenter, true);
                tick.raycastTarget = false;
                UiKit.Stretch(tick.rectTransform, 0f, 0f, 0f, 0f);
            }
            var t = UiKit.Txt(rt, "鍛えた後を見る", 15, UiKit.ColInk, TextAnchor.MiddleLeft, true);
            t.raycastTarget = false;
            UiKit.Anchor(t.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(44f, 0f), new Vector2(-8f, 0f));
            Tooltip.Attach(rt.gameObject, delegate { return "入れると、一覧の全部の札が鍛えた後の姿になる (鍛えられない札はそのまま)。もう一度押すと元の姿"; });
        }

        /// <summary>カードのグリッド (スクロール)。btnLabel が null を返す札はボタンなし。marked は強調。
        /// スマホ (2026-09-16 案A「一面の棚としおり」): 札の下のボタンを出さず「札を押す＝選ぶ」。pickKey があれば押した札に蜂蜜の縁が付き、confirmRoot の下の帯に「〜を鍛える」の確定ボタン
        /// (tapPicks なら押した時点で onPick = 工房の素材・星読みの盤の複数選択)。badge は札の角の印 (A/B)。cellScale は札の倍率 (既定: スマホ 0.9・PC 0.8)。
        /// 「鍛えた後を見る」(g.ShowUpgraded) が入っていれば鍛えられる札を鍛えた後の姿で描く。チェックは confirmRoot の左下に置く</summary>
        public static void CardGrid(GameRoot g, Transform parent, IReadOnlyList<CardInstance> cards,
            Func<int, CardInstance, string> btnLabel, Func<int, CardInstance, bool> btnEnabled, Action<int> onPick, float minH, List<int> marked = null, List<int> starred = null,
            string pickKey = null, RectTransform confirmRoot = null, bool tapPicks = false, Func<int, string> badge = null, float cellScale = 0f)
        {
            var content = UiKit.Scroll(parent, true, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.06f), 12, 12);
            // 一覧の高さは入れ物の残りいっぱい (flexibleHeight)。スマホは minH を付けない
            // (2026-09-14 ユーザー「工房や焚き火で最下部のカード下部マージンがなく選べない」: 高さ 675 の入れ物より minH 500 の方が大きく、
            //  一覧の下端が画面の外に出て最後の行までスクロールできなかった)
            float mh = UiKit.Phone ? 0f : minH;
            UiKit.Le(UiKit.ScrollRoot(content), -1f, mh, -1f, mh, -1f, 1f);
            var vg = content.GetComponent<VerticalLayoutGroup>();
            if (vg != null) UnityEngine.Object.DestroyImmediate(vg);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            bool phoneTap = UiKit.Phone && btnLabel != null;
            bool withBtn = btnLabel != null && !phoneTap;
            float sc = cellScale > 0f ? cellScale : (UiKit.Phone ? 0.86f : 0.8f);   // スマホ 0.86 = 幅いっぱいで 7列、高さ 535 に 2行がちょうど収まる
            float cw = CardView.W * sc, chh = CardView.H * sc;
            grid.cellSize = new Vector2(cw, chh + (withBtn ? 48f : 0f));
            grid.spacing = new Vector2(14f, 14f);
            grid.padding = UiKit.Phone && !withBtn ? new RectOffset(10, 10, 8, 14) : new RectOffset(12, 12, 12, 28);   // 下は多めに (最後の行のボタンが縁に触れない)
            grid.childAlignment = TextAnchor.UpperLeft;
            if (cards.Count == 0)
            {
                var none = UiKit.Txt(parent, "（空）", 18, UiKit.ColInkSoft, TextAnchor.MiddleCenter);
                UiKit.Le(none, -1f, 40f, -1f, 40f);
            }
            int picked = phoneTap && !tapPicks ? g.GridPick(pickKey) : -1;
            if (picked >= cards.Count) picked = -1;
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                int idx = i;
                bool mark = marked != null && marked.Contains(i);
                var cell = UiKit.NewRect("cell", content);
                // 「鍛えた後を見る」: 鍛えられる札は鍛えた後の姿で描く (長押しの拡大は元の札＝拡大の中に元/後の切り替えがある)
                CardInstance shown = c;
                if (g.ShowUpgraded) { try { if (Upgrade.CanUpgradeCard(c)) shown = Upgrade.UpgradeCard(c); } catch (Exception) { } }
                // 札は raycast を受ける (長押し/右クリックで拡大表示。本家の SingleCardViewPopup)。押す・離すは cell 側の LongPressOpen が受ける
                var cv = CardView.Build(cell, shown, g.Rs.Combat, true, true, "deck-card");
                cv.localScale = Vector3.one * sc;
                float lift = withBtn ? 24f : 0f;
                if (lift > 0f) cv.anchoredPosition = new Vector2(0f, lift);
                CardPopup.Attach(g, cell, c, delegate { return g.Rs != null && g.Rs.Phase == RunPhases.Combat ? g.Rs.Combat : null; }, true);
                float hw = CardView.W * sc / 2f, hh = CardView.H * sc / 2f;
                if (!mark && starred != null && starred.Contains(i))
                {   // ⭐ レシピの相手札 (2026-09-12): 蜂蜜色の細い枠と星
                    var sring = UiKit.Frame(cell, Theme.Panel, new Color(0.88f, 0.7f, 0.35f, 0.85f), "star-ring", 3f);
                    UiKit.Anchor(sring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-hw - 6f, -hh - 6f + lift), new Vector2(hw + 6f, hh + 6f + lift));
                    sring.raycastTarget = false;
                    sring.transform.SetAsFirstSibling();
                    var st = UiKit.Icon(cell, "star", 32f);
                    st.raycastTarget = false;
                    st.rectTransform.anchorMin = st.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    st.rectTransform.anchoredPosition = new Vector2(-hw + 4f, hh + lift - 4f);
                }
                if (mark || picked == i)
                {
                    var ring = UiKit.Frame(cell, Theme.Panel, picked == i ? new Color(0.88f, 0.7f, 0.35f, 0.95f) : new Color(1f, 0.85f, 0.3f, 0.6f), "mark", 3f);
                    UiKit.Anchor(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-hw - 8f, -hh - 8f + lift), new Vector2(hw + 8f, hh + 8f + lift));
                    ring.raycastTarget = false;
                    ring.transform.SetAsFirstSibling();
                }
                string bd = badge != null ? badge(i) : null;
                if (!string.IsNullOrEmpty(bd))
                {   // 札の角の印 (工房の A/B・星読みの盤の ✓)
                    var b = UiKit.NewRect("badge", cell);
                    UiKit.Anchor(b, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(hw - 22f, hh + lift - 22f), new Vector2(hw + 14f, hh + lift + 14f));
                    var bi = b.gameObject.AddComponent<Image>();
                    bi.sprite = PaperFx.Disc(); bi.color = UiKit.Hex("#f6dd98"); bi.raycastTarget = false;
                    var bt = UiKit.Deco(b, bd, 19, PaperFx.Ink, TextAnchor.MiddleCenter);
                    bt.raycastTarget = false;
                    UiKit.Stretch(bt.rectTransform, 0f, 0f, 0f, 0f);
                }
                if (phoneTap)
                {   // 押す＝選ぶ (長押しの拡大が開いた直後の離しは押したことにしない)
                    string label = btnLabel(i, c);
                    bool en = label != null && (btnEnabled == null || btnEnabled(i, c));
                    var et = cell.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    var click = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick };
                    click.callback.AddListener(delegate
                    {
                        if (CardPopup.ClickSuppressed) return;
                        if (tapPicks) { if (en && onPick != null) onPick(idx); return; }
                        if (label == null) return;
                        Audio.Ui("click"); g.SetGridPick(pickKey, idx); g.Rebuild();
                    });
                    et.triggers.Add(click);
                }
                else if (withBtn)
                {
                    string label = btnLabel(i, c);
                    if (label != null)
                    {
                        bool en = btnEnabled == null || btnEnabled(i, c);
                        var b = UiKit.Btn(cell, label, delegate { if (onPick != null) onPick(idx); }, 15, en, mark ? UiKit.Hex("#cfeacc") : Color.white);
                        var le = b.GetComponent<LayoutElement>();
                        if (le != null) UnityEngine.Object.Destroy(le);
                        UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-70f, 0f), new Vector2(70f, 42f));
                    }
                }
            }
            if (confirmRoot != null)
            {
                UpgradeToggle(g, confirmRoot, UiKit.Phone ? 24f : 40f, UiKit.Phone ? 14f : 40f);
                if (picked >= 0 && !tapPicks)
                {   // 下の帯の確定ボタン「深根 を鍛える」
                    var c = cards[picked];
                    string label = btnLabel(picked, c) ?? "選ぶ";
                    bool en = btnEnabled == null || btnEnabled(picked, c);
                    string verb = label == "これ" ? "選ぶ" : label == "除去" ? "取り除く" : label;
                    string text = en ? c.Def.Name + " を" + verb : c.Def.Name + ": " + verb;
                    int pi = picked;
                    BottomButton(confirmRoot, text, delegate { if (onPick != null) onPick(pi); }, 18, 440f, 52f, 0f, 14f, UiKit.Hex("#f0d58a"), en);
                }
            }
        }

        /// <summary>情景の窓 (2026-09-11 ユーザー裁定「作って画面に組み込む」): Art/scenes/&lt;name&gt;.png (240×135) を見出しの左に 2倍 (480×270) で貼る。
        /// 絵が無ければ何も置かず false (画面は従来の配置のまま)。true なら中身の上端を SceneBottom まで下げる</summary>
        public const float SceneBottom = TopH + 8f + 290f + 12f;
        public static bool SceneWindow(RectTransform root, string name)
        {
            if (UiKit.Phone) return false;   // スマホは情景の窓を出さない (高さ 675 の 43% を食う。2026-09-14)
            var art = Theme.Art("scenes", name);
            if (art == null) return false;
            var cell = UiKit.NewRect("scene-" + name, root);
            UiKit.Anchor(cell, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -(TopH + 8f + 290f)), new Vector2(40f + 500f, -(TopH + 8f)));
            var frame = UiKit.Frame(cell, Theme.Panel, Color.white, "frame", 3f);
            UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            frame.raycastTarget = false;
            var img = new GameObject("scene", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(cell, false);
            img.sprite = art; img.preserveAspect = true; img.raycastTarget = false;
            UiKit.Anchor(img.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 10f), new Vector2(-10f, -10f));
            return true;
        }

        /// <summary>画面の見出し (大きな題と小さな説明)</summary>
        public static void Heading(RectTransform root, string title, string sub, float y = TopH + 24f, float rightInset = 0f)
        {
            // スマホ: 見出しは上部バーの真ん中の札に畳む (題 17・説明 13 の1行。2026-09-16 案A)。上部バーが無い画面は従来の大見出し
            var center = UiKit.Phone ? root.Find("topbar/center") as RectTransform : null;
            if (center != null)
            {
                var tagP = BattleScreen.Tag(center, 36f, 0f, UiKit.Hex("#fbf6e8"));
                var tImg = tagP.GetComponent<Image>(); if (tImg != null) tImg.raycastTarget = true;   // 説明文 (全文) のため
                var tle = tagP.GetComponent<LayoutElement>(); if (tle != null) { tle.preferredWidth = 560f; tle.flexibleWidth = 0f; }
                var tt = UiKit.Deco(tagP, title, 17, PaperFx.Ink, TextAnchor.MiddleLeft);
                tt.textWrappingMode = TextWrappingModes.NoWrap; tt.overflowMode = TextOverflowModes.Ellipsis;
                UiKit.Le(tt, -1f, 30f, -1f, 30f, 0f, -1f);
                if (!string.IsNullOrEmpty(sub))
                {
                    var ts = UiKit.Txt(tagP, sub, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
                    ts.textWrappingMode = TextWrappingModes.NoWrap; ts.overflowMode = TextOverflowModes.Ellipsis;
                    UiKit.Le(ts, -1f, 30f, -1f, 30f, 1f, -1f);
                }
                Tooltip.Attach(tagP.gameObject, delegate { return "<b>" + title + "</b>" + (string.IsNullOrEmpty(sub) ? "" : "\n" + sub); });
                return;
            }
            var t = UiKit.Deco(root, title, 36, UiKit.ColText, TextAnchor.MiddleCenter);
            t.outlineWidth = 0.2f; t.outlineColor = new Color(0f, 0f, 0f, 0.7f);
            UiKit.Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -y - 50f), new Vector2(-rightInset, -y));
            if (!string.IsNullOrEmpty(sub))
            {
                var s = UiKit.Txt(root, sub, 17, UiKit.ColDim, TextAnchor.MiddleCenter);
                s.textWrappingMode = TextWrappingModes.Normal;
                UiKit.Anchor(s.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -y - 80f), new Vector2(-rightInset - 24f, -y - 50f));
            }
        }

        public static string[] ColorsJa(IReadOnlyList<string> colors)
        {
            var outp = new string[colors.Count];
            for (int i = 0; i < colors.Count; i++)
            {
                string c = colors[i];
                outp[i] = c == "green" ? "緑" : c == "blue" ? "青" : c == "red" ? "赤" : c == "white" ? "白" : c == "black" ? "黒" : c;
            }
            return outp;
        }
    }
}
