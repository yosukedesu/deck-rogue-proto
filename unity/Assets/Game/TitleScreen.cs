// TitleScreen.cs — タイトルと出立の支度 (2026-10-29 品質スプリント #4・§12。ユーザー裁定: 題名 NIGHTSHAFT／二段に分ける／タイトルは案 C「紋と板」に案 A の縦の品書き／支度は案 A 本家形)
//   ①タイトル: 一枚絵「坑口の夜」(Art/scenes/bd_title) を暗めに沈め、題の後ろに薄い真鍮の歯車の紋がゆっくり回り、題 (Art/ui/logo_title) が浮かぶ。
//     品書きは縦の字の列 (はじめる／つづきから／設定／終了。◆が今の行)。脈の霧が流れ、粒が昇る。
//   ②出立の支度: 左に立ち絵 (Art/portraits/<id>。無ければ戦闘のちびを整数倍)、重ねて名前の板、下に 15 人の名簿 (絵の無い人は「封」)、
//     右に難易度・シード・初期デッキ、右下に「出立」。
//   旧 (2026-09-07 M3) の 1 画面 (題の字＋15 札＋詳細) は撤去。
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class TitleScreen
    {
        /// <summary>"title"（題と品書き）か "setup"（出立の支度）。ランが無い間だけ意味を持つ。GameRoot.BackToSetup が "title" に戻す</summary>
        public static string Mode = "title";

        public static void Build(GameRoot g, RectTransform root)
        {
            if (Mode == "setup") BuildSetup(g, root);
            else BuildTitle(g, root);
        }

        // ================= ①タイトル =================

        static void BuildTitle(GameRoot g, RectTransform root)
        {
            bool ph = UiKit.Phone;
            var cs = BattleScreen.CanvasSize(root);
            Scene(root, 0.3f, 0.7f);

            // 脈の霧 (門の足元を横に流れる)・粒 (昇る)
            Mist(root, cs.y * 0.34f, cs.x * 0.7f, 0f);
            Mist(root, cs.y * 0.30f, cs.x * 0.55f, 2.1f);
            {
                var mo = UiKit.NewRect("motes", root);
                UiKit.Stretch(mo, 0f, 0f, 0f, 0f);
                var m = mo.gameObject.AddComponent<Motes>();
                m.W = cs.x; m.H = cs.y * 0.9f; m.Count = ph ? 14 : 22;
            }

            // 紋 (題の後ろで薄く回る)
            float logoW = ph ? 760f : 1000f, logoH = logoW * 440f / 1600f;
            float logoTop = ph ? 44f : 118f;
            float logoCy = -(logoTop + logoH * 0.52f);   // 上端基準の y (題の字の中心)
            var gear = Theme.Art("ui", "gear_emblem");
            if (gear != null)
            {
                float gs = ph ? 400f : 560f;
                var gi = new GameObject("emblem", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                gi.transform.SetParent(root, false);
                gi.sprite = gear; gi.raycastTarget = false; gi.preserveAspect = true;
                gi.color = new Color(1f, 1f, 1f, 0.09f);   // Linear では α がカンバス (.16) より濃く見える (2026-10-29 1 回目の撮影)
                UiKit.Anchor(gi.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-gs / 2f, logoCy - gs / 2f), new Vector2(gs / 2f, logoCy + gs / 2f));
                gi.gameObject.AddComponent<Spin>().DegPerSec = 6f;   // 1 分で 1 回転
            }

            // 題
            var logo = Theme.Art("ui", "logo_title");
            if (logo != null)
            {
                var li = new GameObject("logo", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                li.transform.SetParent(root, false);
                li.sprite = logo; li.raycastTarget = false; li.preserveAspect = true;
                UiKit.Anchor(li.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-logoW / 2f, -logoTop - logoH), new Vector2(logoW / 2f, -logoTop));
            }
            else
            {
                var t = UiKit.Deco(root, "GEARWITCH", ph ? 72 : 120, PaperFx.BrassLight, TextAnchor.MiddleCenter);
                t.characterSpacing = 10f;
                UiKit.Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -logoTop - logoH), new Vector2(0f, -logoTop));
            }

            // 品書き (案 A の縦の字の列)。下の帯の上に乗せる
            float rowH = ph ? 54f : 62f;
            int rows = SaveGame.Exists ? 4 : 3;
            float menuBottom = ph ? 28f : 64f;
            float y = menuBottom + rowH * rows;   // 一番上の行の上端 (下からの距離)
            string saveSummary = null;
            if (SaveGame.Exists) { try { saveSummary = SaveGame.PeekSummary(); } catch (Exception) { } }
            MenuRow.Rows.Clear();
            Row(g, root, "はじめる", null, true, y, rowH, delegate { Mode = "setup"; ScreenFx.Switch(g, g.Rebuild); });
            y -= rowH;
            if (SaveGame.Exists)
            {
                var r = Row(g, root, "つづきから", ph ? null : saveSummary, false, y, rowH, delegate { Audio.Ui("start_run"); g.ResumeSave(); });
                // 放棄 (小さな字。確認あり)
                var ab = UiKit.Txt(root, "放棄", 13, PaperFx.PaperDim, TextAnchor.MiddleLeft);
                ab.raycastTarget = true;
                UiKit.Anchor(ab.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(r + 16f, y - rowH + 10f), new Vector2(r + 70f, y - 10f));
                var abb = ab.gameObject.AddComponent<Button>();
                abb.transition = Selectable.Transition.None;
                abb.onClick.AddListener(delegate { Audio.Ui("click"); g.AskAbandonSave(); });
                Tooltip.Attach(ab.gameObject, delegate { return "このセーブを消す（確認あり）"; });
                y -= rowH;
            }
            Row(g, root, "設定", null, false, y, rowH, delegate { g.SettingsOpen = true; g.Rebuild(); });
            y -= rowH;
            Row(g, root, "終了", null, false, y, rowH, delegate { Application.Quit(); });

            // 版 (左下)・レポートの書き出し (右下。前回のランの記録がある時だけ)
            var ver = UiKit.Txt(root, "プロトタイプ " + Application.version + " ・ set-confirm", 13, PaperFx.PaperDim, TextAnchor.MiddleLeft);
            ver.characterSpacing = 2f; ver.raycastTarget = false;
            UiKit.Anchor(ver.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(ph ? 20f : 40f, 16f), new Vector2(0f, 44f));
            if (Feedback.HasAutosave)
            {
                var rep = UiKit.Txt(root, "前回のランのレポートを書き出す", 13, PaperFx.PaperDim, TextAnchor.MiddleRight);
                rep.raycastTarget = true;
                UiKit.Anchor(rep.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(0f, 16f), new Vector2(ph ? -20f : -40f, 44f));
                var rb = rep.gameObject.AddComponent<Button>();
                rb.transition = Selectable.Transition.None;
                rb.onClick.AddListener(delegate
                {
                    Audio.Ui("click");
                    try { var md = Feedback.ExportAutosave(); g.Error = null; g.Notice = md != null ? "書き出した: " + md : "前回のランの記録がない"; }
                    catch (Exception e) { g.Error = "書き出しに失敗: " + e.Message; }
                    g.Rebuild();
                });
                Tooltip.Attach(rep.gameObject, delegate { return "置き場: " + Feedback.ReportsDir; });
            }
            RunUi.Message(g, root);
        }

        /// <summary>品書きの 1 行 (幅 440 を中央に)。戻り値＝字の右端の x (中央からの距離)</summary>
        static float Row(GameRoot g, RectTransform root, string label, string small, bool primary, float topY, float h, Action onClick)
        {
            bool ph = UiKit.Phone;
            float w = 440f;
            var rt = UiKit.NewRect("menu-" + label, root);
            UiKit.Anchor(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-w / 2f, topY - h), new Vector2(w / 2f, topY));
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = hit; btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(delegate { Audio.Ui("click"); if (onClick != null) onClick(); });
            rt.gameObject.AddComponent<ScreenFx.ButtonMotion>();
            int fs = ph ? 24 : 28;
            var t = UiKit.Txt(rt, label, fs, PaperFx.Paper, TextAnchor.MiddleCenter, true);
            t.characterSpacing = 28f; t.raycastTarget = false;
            t.outlineWidth = 0.18f; t.outlineColor = PaperFx.Ground;
            UiKit.Stretch(t.rectTransform, 0f, 0f, 0f, 0f);
            float tw = t.GetPreferredValues(label).x + fs * 0.28f * (label.Length - 1);
            float half = tw / 2f + (ph ? 26f : 32f);
            var l = Diamond(rt, -half); var r = Diamond(rt, half);
            if (!string.IsNullOrEmpty(small))
            {
                var s = UiKit.Txt(rt, small, 13, PaperFx.PaperDim, TextAnchor.MiddleLeft);
                s.raycastTarget = false; s.textWrappingMode = TextWrappingModes.NoWrap; s.overflowMode = TextOverflowModes.Ellipsis;
                s.outlineWidth = 0.18f; s.outlineColor = PaperFx.Ground;
                UiKit.Anchor(s.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(half + 20f, 0f), new Vector2(half + 420f, 0f));
            }
            var row = rt.gameObject.AddComponent<MenuRow>();
            row.Label = t; row.L = l; row.R = r; row.Primary = primary;
            MenuRow.Rows.Add(row);
            row.Refresh();
            return half;
        }

        static Image Diamond(RectTransform parent, float x)
        {
            var d = UiKit.Pan(parent, PaperFx.BrassLight, "diamond");
            d.raycastTarget = false;
            d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            d.rectTransform.sizeDelta = new Vector2(9f, 9f);
            d.rectTransform.anchoredPosition = new Vector2(x, 0f);
            d.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return d;
        }

        /// <summary>品書きの行: ホバーで真鍮の字と ◆。誰にも触れていない時は「はじめる」(primary) に ◆</summary>
        public class MenuRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public static readonly List<MenuRow> Rows = new List<MenuRow>();
            static MenuRow _hot;
            public TMP_Text Label; public Image L, R; public bool Primary;
            public void OnPointerEnter(PointerEventData d) { _hot = this; RefreshAll(); }
            public void OnPointerExit(PointerEventData d) { if (_hot == this) _hot = null; RefreshAll(); }
            void OnDestroy() { if (_hot == this) _hot = null; }
            static void RefreshAll() { for (int i = 0; i < Rows.Count; i++) if (Rows[i] != null) Rows[i].Refresh(); }
            public void Refresh()
            {
                bool on = _hot == this || (_hot == null && Primary);
                if (Label != null) Label.color = on ? PaperFx.BrassLight : PaperFx.Paper;
                if (L != null) L.gameObject.SetActive(on);
                if (R != null) R.gameObject.SetActive(on);
            }
        }

        // ================= ②出立の支度 =================

        static void BuildSetup(GameRoot g, RectTransform root)
        {
            bool ph = UiKit.Phone;
            var cs = BattleScreen.CanvasSize(root);
            var leaders = Content.AllLeaders;
            LeaderDef ld = null;
            for (int i = 0; i < leaders.Count; i++) if (leaders[i].Id == g.LeaderId) ld = leaders[i];
            if (ld == null && leaders.Count > 0) { ld = leaders[0]; g.LeaderId = ld.Id; }
            Scene(root, 0.5f, 0.85f);
            // 選んだ色で左を染める (柔らかい光)
            if (ld != null)
            {
                var edge = Theme.ColorEdge(ld.Colors.Count > 0 ? ld.Colors[0] : "green");
                var wash = UiKit.Pan(root, new Color(edge.r, edge.g, edge.b, 0.28f), "wash");
                wash.sprite = MapForest.Radial(); wash.raycastTarget = false;
                float ws = ph ? 1100f : 1700f;
                wash.rectTransform.anchorMin = wash.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                wash.rectTransform.sizeDelta = new Vector2(ws, ws);
                wash.rectTransform.anchoredPosition = new Vector2(ph ? 260f : 440f, ph ? -40f : -60f);
            }
            {
                var mo = UiKit.NewRect("motes", root);
                UiKit.Stretch(mo, 0f, 0f, 0f, 0f);
                var m = mo.gameObject.AddComponent<Motes>();
                m.W = cs.x; m.H = cs.y * 0.9f; m.Count = ph ? 8 : 14;
            }

            // 名簿 (下の帯)
            float slotW = ph ? 64f : 104f, slotH = ph ? 84f : 128f, gap = ph ? 6f : 10f, rosterBottom = ph ? 12f : 30f;
            float rosterW = leaders.Count * slotW + (leaders.Count - 1) * gap;
            float rx = (cs.x - rosterW) / 2f;
            for (int i = 0; i < leaders.Count; i++)
            {
                var l = leaders[i];
                Slot(g, root, l, l.Id == g.LeaderId, rx + i * (slotW + gap), rosterBottom, slotW, slotH);
            }

            // 立ち絵 (左)。足元は名簿の少し上
            float feet = rosterBottom + slotH + (ph ? 14f : 42f);
            float portraitCx = ph ? 230f : 470f;
            if (ld != null) Portrait(root, ld, portraitCx, feet, ph ? 440f : 800f);

            // 名前の板 (立ち絵に重ねる)
            if (ld != null) NameCard(g, root, ld, ph ? 300f : 640f, ph ? 60f : 180f, ph ? 470f : 620f, ph ? 236f : 340f);

            // 右の列: 難易度・シード・初期デッキ・出立
            float colL = cs.x - (ph ? 20f : 60f) - (ph ? 380f : 500f), colW = ph ? 380f : 500f;
            if (ld != null) RightColumn(g, root, ld, colL, colW, ph ? 60f : 170f);

            // 上: 題へ戻る・見出し
            {
                var back = UiKit.Btn(root, "◀ 題へ", delegate { Mode = "title"; ScreenFx.Switch(g, g.Rebuild); }, ph ? 15 : 17);
                var le = back.GetComponent<LayoutElement>(); if (le != null) UnityEngine.Object.Destroy(le);
                UiKit.Anchor(back.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(ph ? 16f : 40f, ph ? -64f : -76f), new Vector2(ph ? 136f : 170f, ph ? -16f : -28f));
                var head = UiKit.Txt(root, "出 立 の 支 度", ph ? 13 : 15, PaperFx.PaperDim, TextAnchor.MiddleCenter);
                head.characterSpacing = 12f; head.raycastTarget = false;
                head.outlineWidth = 0.18f; head.outlineColor = PaperFx.Ground;
                UiKit.Anchor(head.rectTransform, new Vector2(0.3f, 1f), new Vector2(0.7f, 1f), new Vector2(0f, ph ? -48f : -64f), new Vector2(0f, ph ? -16f : -28f));
            }
            RunUi.Message(g, root);
        }

        /// <summary>立ち絵: Art/portraits/&lt;id&gt; (なめらかな絵。高さ h に縮める) が無ければ戦闘のちびを整数倍 (PC 9・スマホ 4)</summary>
        static void Portrait(RectTransform root, LeaderDef ld, float cx, float feet, float h)
        {
            var art = Theme.Art("portraits", ld.Id);
            var img = new GameObject("portrait", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(root, false);
            img.raycastTarget = false; img.preserveAspect = true;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            if (art != null)
            {
                img.sprite = art;
                float w = h * art.rect.width / art.rect.height;
                rt.sizeDelta = new Vector2(w, h);
                rt.anchoredPosition = new Vector2(cx, feet);
                return;
            }
            var chibi = Creature.Get("leaders", ld.Id, true);
            img.sprite = chibi;
            UiKit.PixelArt(img);
            float k = UiKit.Phone ? 4f : 9f;
            if (chibi != null)
            {
                rt.sizeDelta = new Vector2(chibi.rect.width * k, chibi.rect.height * k);
                // 足元: 絵の下端の透明な余白 (このは 2 ドット・みぞれ 8 ドット…) を画素から読んで、見える足を feet に置く
                rt.anchoredPosition = new Vector2(cx, feet - BottomMargin(chibi) * k);
            }
        }

        /// <summary>絵の下端から最初の不透明な行までのドット数 (絵は読み取り可。読めなければ 0)</summary>
        static int BottomMargin(Sprite s)
        {
            try
            {
                var tex = s.texture;
                var r = s.textureRect;
                int x0 = (int)r.x, y0 = (int)r.y, w = (int)r.width, h = (int)r.height, tw = tex.width;
                var px = tex.GetPixels32();
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if (px[(y0 + y) * tw + x0 + x].a > 8) return y;
            }
            catch (Exception) { }
            return 0;
        }

        /// <summary>名前の板 (本家形: 半透明の夜色の板に名前・肩書・数字・本文)。x/top は左上から</summary>
        static void NameCard(GameRoot g, RectTransform root, LeaderDef ld, float x, float top, float w, float h)
        {
            bool ph = UiKit.Phone;
            var card = UiKit.Pan(root, new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0.86f), "namecard");
            card.raycastTarget = false;
            UiKit.Anchor(card.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -top - h), new Vector2(x + w, -top));
            var rim = card.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.4f); rim.effectDistance = new Vector2(1.5f, -1.5f); rim.useGraphicAlpha = false;
            float pad = ph ? 18f : 30f;
            var col = UiKit.NewRect("col", card.transform);
            UiKit.Stretch(col, pad, pad, ph ? 14f : 22f, ph ? 12f : 20f);
            var vg = UiKit.Vert(col, ph ? 4 : 8, 0);
            vg.childForceExpandHeight = false;
            vg.childAlignment = TextAnchor.UpperLeft;

            string desc = ld.Description ?? "";
            string title = null, body = desc;
            int k = desc.IndexOf('。');
            if (k > 0 && k <= 14) { title = desc.Substring(0, k); body = desc.Substring(k + 1).TrimStart('。', ' ', '　'); }

            var name = UiKit.Deco(col, ld.Name, ph ? 34 : 56, PaperFx.BrassLight, TextAnchor.MiddleLeft);
            name.characterSpacing = 4f;
            UiKit.Le(name, -1f, ph ? 40f : 64f, -1f, ph ? 40f : 64f);
            var tl = UiKit.Txt(col, (title ?? "") + (title != null ? "　—　" : "") + string.Join(" / ", RunUi.ColorsJa(ld.Colors)) + "の工房", ph ? 13 : 17, PaperFx.PaperDim, TextAnchor.MiddleLeft);
            tl.characterSpacing = 4f;
            UiKit.Le(tl, -1f, ph ? 20f : 26f, -1f, ph ? 20f : 26f);

            // 数字の行: ♥ HP ・ ドロー ・ エナジー ・ 仕込み枠
            var stats = UiKit.NewRect("stats", col);
            UiKit.Le(stats, -1f, ph ? 24f : 30f, -1f, ph ? 24f : 30f);
            var hg = UiKit.Horz(stats, ph ? 10 : 18, 0);
            hg.childAlignment = TextAnchor.MiddleLeft; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            int sfs = ph ? 15 : 19;
            Stat(stats, "♥ " + ld.MaxHp, PaperFx.RoseLight, sfs);
            Stat(stats, "ドロー " + ld.DrawPerTurn, PaperFx.Paper, sfs);
            Stat(stats, "エナジー " + ld.EnergyMax, PaperFx.BrassLight, sfs);
            if (ld.SetSlots.HasValue && ld.SetSlots.Value > 0) Stat(stats, "仕込み枠 " + ld.SetSlots.Value, PaperFx.ManaLight, sfs);
            if (ld.RewardChoices != 3) Stat(stats, "報酬候補 " + ld.RewardChoices, PaperFx.Paper, sfs);

            var bd = UiKit.Txt(col, body, ph ? 13 : 16, PaperFx.Paper, TextAnchor.UpperLeft);
            bd.textWrappingMode = TextWrappingModes.Normal; bd.overflowMode = TextOverflowModes.Ellipsis;
            bd.lineSpacing = ph ? 4f : 8f;
            UiKit.Le(bd, -1f, 40f, -1f, -1f, -1f, 1f);
        }

        static void Stat(RectTransform parent, string text, Color c, int fs)
        {
            var t = UiKit.Txt(parent, text, fs, c, TextAnchor.MiddleLeft, true);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Le(t, t.GetPreferredValues(text).x + 2f, -1f, t.GetPreferredValues(text).x + 2f, -1f);
        }

        /// <summary>右の列: 難易度 (−／数字／＋と説明)・シード・初期デッキ (小さな札。押すと拡大)・「出立」</summary>
        static void RightColumn(GameRoot g, RectTransform root, LeaderDef ld, float x, float w, float top)
        {
            bool ph = UiKit.Phone;
            var cs = BattleScreen.CanvasSize(root);
            var col = UiKit.NewRect("setup-col", root);
            UiKit.Anchor(col, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -top - (ph ? 420f : 640f)), new Vector2(x + w, -top));
            var vg = UiKit.Vert(col, ph ? 10 : 18, 0);
            vg.childForceExpandHeight = false; vg.childAlignment = TextAnchor.UpperLeft;

            Label(col, "難 易 度");
            {
                var row = UiKit.NewRect("diffrow", col);
                UiKit.Le(row, -1f, ph ? 48f : 56f, -1f, ph ? 48f : 56f);
                var hg = UiKit.Horz(row, ph ? 10 : 14, 0);
                hg.childAlignment = TextAnchor.MiddleLeft; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
                var minus = UiKit.Btn(row, "−", delegate { g.Difficulty = Math.Max(1, g.Difficulty - 1); g.Rebuild(); }, 20, g.Difficulty > 1);
                BattleScreen.SetSize(minus, 48f, 48f);
                var dv = UiKit.Deco(row, g.Difficulty.ToString(), ph ? 36 : 48, g.Difficulty > DeckRogue.Engine.Run.DEFAULT_DIFFICULTY ? PaperFx.RoseLight : PaperFx.BrassLight, TextAnchor.MiddleCenter);
                UiKit.Le(dv, ph ? 56f : 70f, -1f, ph ? 56f : 70f, -1f);
                var plus = UiKit.Btn(row, "+", delegate { g.Difficulty = Math.Min(10, g.Difficulty + 1); g.Rebuild(); }, 20, g.Difficulty < 10);
                BattleScreen.SetSize(plus, 48f, 48f);
                var note = UiKit.Txt(row, DeckRogue.Engine.Run.DifficultyDescription(g.Difficulty) + (g.Difficulty == DeckRogue.Engine.Run.DEFAULT_DIFFICULTY ? "" : "。後半ほど重く、ギア・報酬の供給は変わらない"), 13, PaperFx.PaperDim, TextAnchor.MiddleLeft);
                note.textWrappingMode = TextWrappingModes.Normal; note.overflowMode = TextOverflowModes.Ellipsis;
                UiKit.Le(note, -1f, ph ? 48f : 56f, -1f, ph ? 48f : 56f, 1f, -1f);
            }
            Label(col, "シ ー ド");
            {
                var row = UiKit.NewRect("seedrow", col);
                UiKit.Le(row, -1f, ph ? 40f : 44f, -1f, ph ? 40f : 44f);
                var hg = UiKit.Horz(row, 14, 0);
                hg.childAlignment = TextAnchor.MiddleLeft; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
                var field = RunScreens.MakeSeedField(g, row);
                UiKit.Le(field, 180f, ph ? 40f : 44f, 180f, ph ? 40f : 44f);
                var hint = UiKit.Txt(row, "同じシードなら同じ坑", 13, PaperFx.PaperDim, TextAnchor.MiddleLeft);
                hint.textWrappingMode = TextWrappingModes.NoWrap;
            }
            // 初期デッキ
            DeckDef deck = null;
            try { deck = Content.GetDeckDef(ld.RunDeckId); } catch (Exception) { }
            if (deck != null)
            {
                int total = 0;
                var ids = new List<string>();
                for (int i = 0; i < deck.Cards.Count; i++) { total += deck.Cards[i].Count; for (int n = 0; n < deck.Cards[i].Count; n++) ids.Add(deck.Cards[i].CardId); }
                Label(col, "初 期 デ ッ キ　" + total + " 枚");
                float scale = ph ? 0.17f : 0.235f;
                float cw = CardView.W * scale, chh = CardView.H * scale, cgap = ph ? 3f : 5f;
                float rowW = ids.Count * cw + (ids.Count - 1) * cgap;
                if (rowW > w) { scale *= w / rowW; cw = CardView.W * scale; chh = CardView.H * scale; }
                var row = UiKit.NewRect("deckrow", col);
                UiKit.Le(row, -1f, chh + 10f, -1f, chh + 10f);
                for (int i = 0; i < ids.Count; i++)
                {
                    CardInstance ci;
                    try { ci = new CardInstance { Uid = "setup-" + i, Def = Content.GetCardDef(ids[i]) }; } catch (Exception) { continue; }
                    var cell = UiKit.NewRect("deck-" + i, row);
                    UiKit.Anchor(cell, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(i * (cw + cgap), -chh - 4f), new Vector2(i * (cw + cgap) + cw, -4f));
                    var cv = CardView.Build(cell, ci, null, true, false, "deck-card");
                    cv.anchorMin = cv.anchorMax = new Vector2(0f, 1f);
                    cv.pivot = new Vector2(0f, 1f);
                    cv.anchoredPosition = Vector2.zero;
                    cv.localScale = Vector3.one * scale;
                    var captured = ci;
                    RewardScreen.HoverRaise(cv, delegate { Audio.Ui("click"); CardPopup.Open(g, captured, null); });
                    CardPopup.Attach(g, cv, captured, null, true);
                }
                var names = new List<string>();
                for (int i = 0; i < deck.Cards.Count; i++)
                {
                    string nm = deck.Cards[i].CardId;
                    try { nm = Content.GetCardDef(deck.Cards[i].CardId).Name; } catch (Exception) { }
                    names.Add(nm + (deck.Cards[i].Count > 1 ? "×" + deck.Cards[i].Count : ""));
                }
                var dk = UiKit.Txt(col, string.Join("、", names.ToArray()) + "　（押すと拡大）", 13, PaperFx.PaperDim, TextAnchor.UpperLeft);
                dk.textWrappingMode = TextWrappingModes.Normal; dk.overflowMode = TextOverflowModes.Ellipsis;
                UiKit.Le(dk, -1f, ph ? 20f : 40f, -1f, ph ? 20f : 40f);
            }

            // 出立 (右下・決定の脈)
            bool hasSave = SaveGame.Exists;
            float bw = ph ? 300f : 380f, bh = ph ? 56f : 72f;
            var go = RunUi.BottomButton(root, hasSave ? "出 立（セーブを捨てる）" : "出　立", delegate { Audio.Ui("start_run"); g.AskStartRun(); }, ph ? 22 : 28, bw, bh,
                x + w - bw / 2f - cs.x / 2f, ph ? 110f : 190f, PaperFx.BrassLight);
            ScreenFx.Primary(go);
        }

        static void Label(RectTransform col, string text)
        {
            var t = UiKit.Txt(col, text, 13, PaperFx.BrassLight, TextAnchor.MiddleLeft, true);
            t.characterSpacing = 6f;
            t.outlineWidth = 0.18f; t.outlineColor = PaperFx.Ground;
            UiKit.Le(t, -1f, 20f, -1f, 20f);
        }

        /// <summary>名簿の 1 枠: 絵のある人＝紙の札 (色の帯・ちび・名前)、無い人＝「封」(押せない)。x/bottom は左下から</summary>
        static void Slot(GameRoot g, RectTransform root, LeaderDef ld, bool selected, float x, float bottom, float w, float h)
        {
            bool ph = UiKit.Phone;
            var cell = UiKit.NewRect("slot-" + ld.Id, root);
            UiKit.Anchor(cell, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, bottom), new Vector2(x + w, bottom + h));
            var chibi = Creature.Get("leaders", ld.Id, true);
            bool hasArt = Theme.Art("leaders", ld.Id) != null && chibi != null;
            if (!hasArt)
            {
                var pan = UiKit.Pan(cell, new Color(PaperFx.Night.r, PaperFx.Night.g, PaperFx.Night.b, 0.92f), "seal");
                pan.raycastTarget = false;
                UiKit.Stretch(pan.rectTransform, 0f, 0f, 0f, 0f);
                var fr = PaperFx.Sheet(cell, PaperFx.GhostFrame, "frame", new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.25f));
                fr.raycastTarget = false;
                UiKit.Stretch(fr.rectTransform, 0f, 0f, 0f, 0f);
                var fu = UiKit.Deco(cell, "封", ph ? 20 : 28, new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.6f), TextAnchor.MiddleCenter);
                fu.raycastTarget = false;
                UiKit.Anchor(fu.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -(ph ? 54f : 86f)), new Vector2(0f, -(ph ? 6f : 14f)));
                var nm = UiKit.Txt(cell, ld.Name, 13, new Color(PaperFx.PaperDim.r, PaperFx.PaperDim.g, PaperFx.PaperDim.b, 0.6f), TextAnchor.MiddleCenter);
                nm.raycastTarget = false;
                UiKit.Anchor(nm.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, ph ? 4f : 8f), new Vector2(0f, ph ? 24f : 30f));
                Tooltip.Attach(cell.gameObject, delegate { return ld.Name + "　まだ坑に入れない（ギルドの工房は後日）"; });
                return;
            }
            var paper = PaperFx.Sheet(cell, PaperFx.Panel, "paper", Color.white);
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            var btn = cell.gameObject.AddComponent<Button>();
            btn.targetGraphic = paper;
            btn.transition = Selectable.Transition.None;
            string id = ld.Id;
            btn.onClick.AddListener(delegate { if (g.LeaderId == id) return; Audio.Ui("click"); g.LeaderId = id; g.Rebuild(); });
            cell.gameObject.AddComponent<ScreenFx.ButtonMotion>();
            if (selected)
            {
                var rim = paper.gameObject.AddComponent<Outline>();
                rim.effectColor = PaperFx.Brass; rim.effectDistance = new Vector2(3f, -3f); rim.useGraphicAlpha = false;
            }
            // 色の帯
            Color edge = Theme.ColorEdge(ld.Colors.Count > 0 ? ld.Colors[0] : "green");
            var band = UiKit.Pan(cell, edge, "band");
            band.raycastTarget = false;
            UiKit.Anchor(band.rectTransform, new Vector2(0f, 1f), new Vector2(ld.Colors.Count > 1 ? 0.5f : 1f, 1f), new Vector2(6f, -12f), new Vector2(ld.Colors.Count > 1 ? 0f : -6f, -6f));
            if (ld.Colors.Count > 1)
            {
                var band2 = UiKit.Pan(cell, Theme.ColorEdge(ld.Colors[1]), "band2");
                band2.raycastTarget = false;
                UiKit.Anchor(band2.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(0f, -12f), new Vector2(-6f, -6f));
            }
            // ちび (枠からはみ出す分は切る)
            var clip = UiKit.NewRect("clip", cell);
            UiKit.Stretch(clip, 4f, 4f, 14f, ph ? 20f : 28f);
            clip.gameObject.AddComponent<RectMask2D>();
            var art = new GameObject("art", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            art.transform.SetParent(clip, false);
            art.sprite = chibi; art.raycastTarget = false; art.preserveAspect = true;
            UiKit.PixelArt(art);
            float k = ph ? 1f : 1.5f;
            art.rectTransform.anchorMin = art.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            art.rectTransform.pivot = new Vector2(0.5f, 0f);
            art.rectTransform.sizeDelta = new Vector2(chibi.rect.width * k, chibi.rect.height * k);
            art.rectTransform.anchoredPosition = new Vector2(0f, -4f * k);
            var name = UiKit.Txt(cell, ld.Name, ph ? 13 : 15, PaperFx.Ink, TextAnchor.MiddleCenter, true);
            name.raycastTarget = false;
            UiKit.Anchor(name.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, ph ? 2f : 6f), new Vector2(0f, ph ? 20f : 28f));
        }

        // ================= 共通: 場面 =================

        /// <summary>一枚絵「坑口の夜」(無ければ戦闘の舞台) を暗幕・周辺減光・上下の帯で沈める</summary>
        static void Scene(RectTransform root, float dim, float vignette)
        {
            if (!CampfireScreen.Backdrop(root, "bd_title", dim))
            {
                BattleScreen.BuildBackground(root, 1);
                var d = UiKit.Pan(root, new Color(0f, 0f, 0f, dim), "dim");
                d.raycastTarget = false;
                UiKit.Stretch(d.rectTransform, 0f, 0f, 0f, 0f);
            }
            var vig = UiKit.Pan(root, new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, vignette), "vignette");
            vig.sprite = MapForest.Vignette(); vig.raycastTarget = false;
            UiKit.Stretch(vig.rectTransform, 0f, 0f, 0f, 0f);
            var cs = BattleScreen.CanvasSize(root);
            var top = UiKit.Pan(root, Color.white, "band-top");
            top.sprite = ThemeFx.Gradient("title_top", new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0.75f), new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0f));
            top.raycastTarget = false;
            UiKit.Anchor(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -cs.y * 0.3f), new Vector2(0f, 0f));
            var bot = UiKit.Pan(root, Color.white, "band-bottom");
            bot.sprite = ThemeFx.Gradient("title_bottom", new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0f), new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0.9f));
            bot.raycastTarget = false;
            UiKit.Anchor(bot.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, cs.y * 0.28f));
        }

        /// <summary>脈の霧: 青緑の柔らかい帯が横にゆっくり流れる (y は下から・w は幅)</summary>
        static void Mist(RectTransform root, float y, float w, float phase)
        {
            var m = UiKit.Pan(root, new Color(PaperFx.Mana.r, PaperFx.Mana.g, PaperFx.Mana.b, 0.14f), "mist");
            m.sprite = MapForest.Radial(); m.raycastTarget = false;
            m.rectTransform.anchorMin = m.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            m.rectTransform.sizeDelta = new Vector2(w, w * 0.22f);
            m.rectTransform.anchoredPosition = new Vector2(0f, y);
            var d = m.gameObject.AddComponent<Drift>();
            d.Amp = w * 0.08f; d.Phase = phase; d.Base = m.rectTransform.anchoredPosition;
        }

        public class Spin : MonoBehaviour
        {
            public float DegPerSec = 6f;
            void Update() { transform.Rotate(0f, 0f, -DegPerSec * Tween.UnscaledDt); }
        }

        public class Drift : MonoBehaviour
        {
            public float Amp = 60f, Phase; public Vector2 Base;
            float _t; RectTransform _rt; Image _img; float _a0;
            void Awake() { _rt = (RectTransform)transform; _img = GetComponent<Image>(); if (_img != null) _a0 = _img.color.a; }
            void Update()
            {
                _t += Tween.UnscaledDt;
                _rt.anchoredPosition = Base + new Vector2(Mathf.Sin(_t * 0.17f + Phase) * Amp, Mathf.Sin(_t * 0.11f + Phase * 1.7f) * Amp * 0.15f);
                if (_img != null) { var c = _img.color; c.a = _a0 * (0.8f + 0.2f * Mathf.Sin(_t * 0.23f + Phase)); _img.color = c; }
            }
        }

        /// <summary>マナの粒: 画面の幅に散らばり、ゆっくり昇りながら明滅する (焚き火の火の粉の青緑版・寿命は長い)</summary>
        public class Motes : MonoBehaviour
        {
            public float W = 1920f, H = 900f; public int Count = 20;
            class M { public Image Img; public float X, Y, Vy, Ph, A; }
            readonly List<M> _m = new List<M>();
            System.Random _rng = new System.Random(11);
            float _t;
            void Start()
            {
                for (int i = 0; i < Count; i++)
                {
                    var go = new GameObject("mote", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    go.transform.SetParent(transform, false);
                    var img = go.GetComponent<Image>();
                    img.sprite = MapForest.Radial(); img.raycastTarget = false;
                    bool bright = _rng.NextDouble() < 0.4;
                    img.color = bright ? PaperFx.ManaLight : PaperFx.Mana;
                    float sz = bright ? 5f + (float)_rng.NextDouble() * 5f : 3f + (float)_rng.NextDouble() * 4f;
                    var rt = img.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                    rt.sizeDelta = new Vector2(sz, sz);
                    _m.Add(new M { Img = img, X = (float)_rng.NextDouble() * W, Y = (float)_rng.NextDouble() * H, Vy = 10f + (float)_rng.NextDouble() * 18f, Ph = (float)_rng.NextDouble() * 6.28f, A = bright ? 0.9f : 0.6f });
                }
            }
            void Update()
            {
                _t += Tween.UnscaledDt;
                float dt = Tween.UnscaledDt;
                for (int i = 0; i < _m.Count; i++)
                {
                    var m = _m[i];
                    if (m.Img == null) continue;
                    m.Y += m.Vy * dt;
                    if (m.Y > H) { m.Y = -10f; m.X = (float)_rng.NextDouble() * W; }
                    float x = m.X + Mathf.Sin(_t * 0.5f + m.Ph) * 22f;
                    m.Img.rectTransform.anchoredPosition = new Vector2(x, m.Y);
                    float tw = 0.55f + 0.45f * Mathf.Sin(_t * 1.6f + m.Ph * 2f);
                    float edge = Mathf.Clamp01(m.Y / 60f) * Mathf.Clamp01((H - m.Y) / 120f);
                    var c = m.Img.color; c.a = m.A * tw * edge; m.Img.color = c;
                }
            }
        }
    }
}
