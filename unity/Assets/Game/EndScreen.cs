// EndScreen.cs — ランの決着 (M3・2026-09-07): 走破/敗北の見出し・戦績・最終デッキ
// 2026-11-04 (見た目・UX・演出の磨き 束 C2・G4/M05/M14) 作り直し: 本家並みの「受け止め」に。
//   ①大きな題: 走破＝真鍮の「走破」が 1.3→1 で押し当てられ、真鍮の罫が伸び、輪が広がる。敗北＝薔薇の「倒れた」が沈む
//   ②ランの記録: 紙の札 (2 列) が 0.12 秒ずらしで積まれ、数字は 0.4 秒で数え上がる (出どころ＝Feedback.History・Feedback.Choices・RunState。無い物は出さない)
//   ③遺物は絵つきの紙の円の列 (0.06 秒ずらしで現れる)。④デッキは PC は右の一覧・スマホは「デッキ N 枚」で一覧の窓
//   ⑤ボタン: もう一度（同じリーダー・新しいシード。敗北の主）・タイトルへ (走破の主)・レポートを書き出す (小さく右下)
//   ⑥敗北は「倒したのは <敵>」の夜色の札と、敗因の 2 分類に合わせた「次は…」の一言
//   撮影 (ScreenFx.Instant) では動きを飛ばして終わりの姿を置く。構図は本家を写さず、この作品の紙と夜で。戦闘以外にキャラの絵は置かない
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class EndScreen
    {
        /// <summary>記録の 1 行: 名前と値 (数は数え上げ・文はそのまま)</summary>
        class Rec
        {
            public string Label; public int Value; public string Suffix; public string Text;
            public bool IsNumber { get { return Text == null; } }
        }

        public static void Build(GameRoot g, RectTransform root, bool won)
        {
            var run = g.Rs;
            bool ph = UiKit.Phone;
            bool inst = ScreenFx.Instant;
            var cs = BattleScreen.CanvasSize(root);
            BattleScreen.BuildBackground(root, run.Act);

            // 暗幕: 走破は夜色を薄く (舞台の灯が透ける)・敗北は地の色で濃く沈める。周辺減光で中央に目を集める
            var dim = UiKit.Pan(root, won ? new Color(PaperFx.Night.r, PaperFx.Night.g, PaperFx.Night.b, 0.58f) : new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0.8f), "dim");
            dim.raycastTarget = false;
            UiKit.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
            var vig = UiKit.Pan(root, new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, won ? 0.5f : 0.72f), "vignette");
            vig.sprite = MapForest.Vignette(); vig.raycastTarget = false;
            UiKit.Stretch(vig.rectTransform, 0f, 0f, 0f, 0f);
            if (won)
            {
                // 題の後ろの暖かい暈 (灯の光。紙には掛けない＝紙より下の層)
                var glow = UiKit.Pan(root, new Color(PaperFx.Ember.r, PaperFx.Ember.g, PaperFx.Ember.b, 0.16f), "glow");
                glow.sprite = MapForest.Radial(); glow.raycastTarget = false;
                glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                glow.rectTransform.sizeDelta = new Vector2(ph ? 900f : 1400f, ph ? 320f : 520f);
                glow.rectTransform.anchoredPosition = new Vector2(0f, ph ? -60f : -120f);
                var fl = glow.gameObject.AddComponent<CampfireScreen.UiFlicker>(); fl.Base = 0.16f; fl.Amount = 0.25f;
                if (!inst)
                {   // 脈の粒が昇る (走破の余韻。粒は 12 まで)
                    var mo = UiKit.NewRect("motes", root);
                    UiKit.Stretch(mo, 0f, 0f, 0f, 0f);
                    var m = mo.gameObject.AddComponent<TitleScreen.Motes>();
                    m.W = cs.x; m.H = cs.y * 0.9f; m.Count = ph ? 8 : 12;
                }
            }

            // ---- ①題 ----
            int titleSize = ph ? 60 : 88;
            float titleTop = ph ? -14f : -44f;
            var title = UiKit.Deco(root, won ? "走破" : "倒れた", titleSize, won ? PaperFx.BrassLight : PaperFx.RoseLight, TextAnchor.MiddleCenter);
            title.characterSpacing = won ? 26f : 14f; title.raycastTarget = false;
            title.outlineWidth = 0.18f; title.outlineColor = PaperFx.Ground;
            UiKit.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, titleTop - titleSize - 26f), new Vector2(0f, titleTop));
            float ruleY = titleTop - titleSize - 34f;
            var rule = UiKit.Pan(root, won ? PaperFx.Brass : PaperFx.Rose, "rule");
            rule.raycastTarget = false;
            rule.rectTransform.anchorMin = rule.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rule.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rule.rectTransform.anchoredPosition = new Vector2(0f, ruleY);
            float ruleW = ph ? 320f : 520f;
            rule.rectTransform.sizeDelta = new Vector2(inst ? ruleW : 0f, 2f);
            string leader = run.LeaderId;
            try { leader = Content.GetLeaderDef(run.LeaderId).Name; } catch (Exception) { }
            var sub = UiKit.Txt(root, leader + "  ・  難易度 " + run.Difficulty + "  ・  シード " + run.Seed, 15, PaperFx.PaperDim, TextAnchor.MiddleCenter);
            sub.characterSpacing = 2f; sub.raycastTarget = false;
            UiKit.Anchor(sub.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, ruleY - 34f), new Vector2(0f, ruleY - 6f));

            if (!inst)
            {
                var tcg = title.gameObject.AddComponent<CanvasGroup>(); tcg.alpha = 0f; tcg.blocksRaycasts = false;
                var scg = sub.gameObject.AddComponent<CanvasGroup>(); scg.alpha = 0f; scg.blocksRaycasts = false;
                var trt = title.rectTransform;
                var tp0 = trt.anchoredPosition;
                if (won)
                {
                    // 押し当て (1.3→1)・罫が伸びる・輪と薄い閃き
                    trt.localScale = Vector3.one * 1.3f;
                    Tween.Run(FxSettings.Dur(0.4f), k => { if (trt == null) return; trt.localScale = Vector3.one * (1.3f - 0.3f * k); tcg.alpha = Mathf.Clamp01(k * 2f); }, Ease.OutCubic);
                    Tween.After(0.12f, () => Tween.Run(FxSettings.Dur(0.5f), k => { if (rule != null) rule.rectTransform.sizeDelta = new Vector2(ruleW * k, 2f); }, Ease.OutCubic));
                    Tween.After(0.1f, () =>
                    {
                        if (g.FxLayer == null || trt == null) return;
                        Tween.RingBurst(g.FxLayer, Tween.CenterIn(trt, g.FxLayer), PaperFx.BrassLight, ph ? 520f : 760f, 0.6f);
                        float fa = 0.18f * FxSettings.Flash;
                        if (fa > 0.005f) Tween.ScreenFlash(g.FxLayer, new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, fa), 0.4f);
                    });
                    Audio.Key("victory_band");
                }
                else
                {
                    // 沈む (上から 28px 下りて止まる)・薔薇の細い罫だけ
                    trt.anchoredPosition = tp0 + new Vector2(0f, 28f);
                    Tween.Run(FxSettings.Dur(0.55f), k => { if (trt == null) return; trt.anchoredPosition = tp0 + new Vector2(0f, 28f * (1f - k)); tcg.alpha = Mathf.Clamp01(k * 1.6f); }, Ease.OutCubic);
                    Tween.After(0.2f, () => Tween.Run(FxSettings.Dur(0.6f), k => { if (rule != null) rule.rectTransform.sizeDelta = new Vector2(ruleW * k, 2f); }, Ease.OutQuad));
                    Audio.Key("PlayerDied");   // 題の音は「倒れた」の鍵で (崩れの defeat_fall から 2.5 秒後。同じ素材の 3 回目を避けたい時は audio.json の PlayerDied だけ替える。2026-11-04)
                }
                Tween.After(0.35f, () => Tween.Run(0.3f, k => { if (scg != null) scg.alpha = k; }, Ease.OutQuad));
            }

            // ---- ②ランの記録 (2 列の紙の札) ----
            var recs = Records(run);
            float colW = ph ? 236f : 300f, rowH = ph ? 40f : 44f, pitch = rowH + (ph ? 6f : 10f), gap = ph ? 12f : 16f;
            // PC: 左の塊 (画面の 27%) に記録、右の塊に遺物とデッキ。スマホ: 左に記録・右に遺物とデッキのボタン
            float leftCx = cs.x * (ph ? 0.26f : 0.27f);
            float recTop = ruleY - (ph ? 48f : 72f);
            float t0 = inst ? 0f : 0.5f, step = 0.12f;
            int rows = (recs.Count + 1) / 2;
            for (int i = 0; i < recs.Count; i++)
            {
                int col = i % 2, row = i / 2;
                float cx = leftCx + (col == 0 ? -(colW / 2f + gap / 2f) : (colW / 2f + gap / 2f));
                float top = recTop - row * pitch;
                var r = recs[i];
                var tag = UiKit.NewRect("rec-" + i, root);
                UiKit.Anchor(tag, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(cx - colW / 2f, top - rowH), new Vector2(cx + colW / 2f, top));
                var sheet = PaperFx.Sheet(tag, PaperFx.Tag2, "paper");
                UiKit.Stretch(sheet.rectTransform, 0f, 0f, 0f, 0f); sheet.raycastTarget = false;
                var lab = UiKit.Txt(tag, r.Label, ph ? 13 : 14, PaperFx.InkSoft, TextAnchor.MiddleLeft);
                lab.raycastTarget = false;
                UiKit.Anchor(lab.rectTransform, new Vector2(0f, 0f), new Vector2(0.6f, 1f), new Vector2(14f, 0f), new Vector2(0f, 0f));
                string finalText = r.IsNumber ? r.Value + r.Suffix : r.Text;
                var val = UiKit.Txt(tag, inst || !r.IsNumber ? finalText : "0" + r.Suffix, ph ? 18 : 20, PaperFx.Ink, TextAnchor.MiddleRight, true);
                val.raycastTarget = false;
                UiKit.Anchor(val.rectTransform, new Vector2(0.4f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-14f, 0f));
                if (!inst)
                {
                    var cg = tag.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0f; cg.blocksRaycasts = false;
                    var rt = tag; var rec = r; var vt = val;
                    Tween.After(t0 + step * i, () =>
                    {
                        if (rt == null) return;
                        cg.alpha = 1f;
                        Tween.Punch(rt, 0.08f, 0.26f);
                        Audio.Key("tally_tick", 0.8f);
                        if (rec.IsNumber && rec.Value > 0) CountUp(vt, rec.Value, rec.Suffix, FxSettings.Dur(0.4f));
                    });
                }
            }
            float recBottom = recTop - rows * pitch;

            // ---- ⑥敗北: 倒したのは <敵>・次は… ----
            if (!won)
            {
                var last = Feedback.LastBattle;
                string enemy = null;
                if (last != null && last.Result == "lost") { try { enemy = Report.SafeEncounterName(last.EnemyId); } catch (Exception) { enemy = last.EnemyId; } }
                float ly = recBottom - (ph ? 10f : 18f);
                if (!string.IsNullOrEmpty(enemy))
                {
                    var note = PaperFx.NightNote(root, "倒したのは " + enemy + (last.Elite ? "（強個体）" : last.Boss == true ? "（幕ボス）" : "") + "  ・  " + last.Turns + " ターン", ph ? 15 : 17, colW * 2f + gap, true, "slain");
                    note.anchorMin = note.anchorMax = new Vector2(0f, 1f);
                    note.pivot = new Vector2(0.5f, 1f);
                    note.anchoredPosition = new Vector2(leftCx, ly);
                    ly -= (ph ? 40f : 46f);
                    if (!inst) Appear(note, t0 + step * recs.Count + 0.1f);
                }
                string feel = last != null && last.Rating != null ? last.Rating.LossFeel : null;
                var hint = UiKit.Txt(root, LossHint(feel, run), ph ? 14 : 16, PaperFx.PaperDim, TextAnchor.MiddleCenter);
                hint.raycastTarget = false; hint.characterSpacing = 1f;
                UiKit.Anchor(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(leftCx - colW - gap, ly - 30f), new Vector2(leftCx + colW + gap, ly));
                if (!inst) Appear(hint.rectTransform, t0 + step * recs.Count + 0.3f);
            }

            // ---- ③遺物・④デッキ (右の塊) ----
            float rightL = ph ? cs.x * 0.54f : cs.x * 0.47f, rightR = cs.x - (ph ? 24f : 72f);
            float ry = recTop;
            var rh = UiKit.Txt(root, "遺物 " + run.Relics.Count + " 個", ph ? 14 : 15, PaperFx.PaperDim, TextAnchor.MiddleLeft);
            rh.characterSpacing = 2f; rh.raycastTarget = false;
            UiKit.Anchor(rh.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(rightL, ry - 24f), new Vector2(rightR, ry));
            if (!inst) Appear(rh.rectTransform, t0);
            ry -= 30f;
            float disc = ph ? 48f : 56f;
            var relics = UiKit.NewRect("relics", root);
            int relicRows = Mathf.Max(1, Mathf.CeilToInt(run.Relics.Count * (disc + 8f) / Mathf.Max(1f, rightR - rightL)));
            relicRows = Mathf.Min(relicRows, 2);
            float relicsH = relicRows * (disc + 8f);
            UiKit.Anchor(relics, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(rightL, ry - relicsH), new Vector2(rightR, ry));
            var grid = relics.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(disc, disc); grid.spacing = new Vector2(8f, 8f);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft; grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            for (int i = 0; i < run.Relics.Count; i++)
            {
                string id = run.Relics[i];
                RelicDef rd = null;
                try { rd = Content.GetRelicDef(id); } catch (Exception) { }
                var cell = UiKit.NewRect("relic-" + i, relics);
                var paper = UiKit.Pan(cell, Color.white, "disc");
                paper.sprite = PaperFx.Disc(); paper.raycastTarget = true;
                UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
                RunUi.RelicArt(paper.transform, id, disc - 16f);
                string nm = rd != null ? rd.Name : id, desc = rd != null ? rd.Description : "";
                Tooltip.Attach(paper.gameObject, delegate { return "<b>" + nm + "</b>\n" + desc; });
                if (!inst)
                {
                    var c = cell; c.localScale = Vector3.zero;
                    Tween.After(t0 + 0.2f + 0.06f * i, () => { if (c != null) Tween.Scale(c, Vector3.one, 0.28f, Ease.OutBack); });
                }
            }
            if (run.Relics.Count == 0)
            {
                var none = UiKit.Txt(relics, "（持たずに降りた）", 14, PaperFx.PaperDim, TextAnchor.MiddleLeft);
                none.raycastTarget = false;
                var le = none.gameObject.AddComponent<LayoutElement>(); le.ignoreLayout = true;
                UiKit.Anchor(none.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -disc), new Vector2(0f, 0f));
            }
            ry -= relicsH + 18f;

            float btnBottom = ph ? 18f : 40f, btnH = ph ? 48f : 56f;
            if (ph)
            {
                // スマホ: デッキは一覧の窓 (RunUi.DeckViewer) で見る。ボタンは右の塊に
                var db = UiKit.Btn(root, "デッキ " + run.Deck.Count + " 枚を見る", delegate { g.ViewDeck = true; g.Rebuild(); }, 16);
                var dle = db.GetComponent<LayoutElement>(); if (dle != null) UnityEngine.Object.Destroy(dle);
                UiKit.Anchor(db.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(rightL, ry - 48f), new Vector2(rightL + 300f, ry));
                if (!inst) Appear(db.GetComponent<RectTransform>(), t0 + 0.4f);
            }
            else
            {
                var dh = UiKit.Txt(root, "デッキ " + run.Deck.Count + " 枚", 15, PaperFx.PaperDim, TextAnchor.MiddleLeft);
                dh.characterSpacing = 2f; dh.raycastTarget = false;
                UiKit.Anchor(dh.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(rightL, ry - 24f), new Vector2(rightR, ry));
                if (!inst) Appear(dh.rectTransform, t0 + 0.3f);
                ry -= 28f;
                var area = UiKit.NewRect("deck", root);
                UiKit.Anchor(area, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(rightL, btnBottom + btnH + 24f), new Vector2(rightR, ry));
                UiKit.Vert(area, 0, 0);
                RunUi.CardGrid(g, area, run.Deck, null, null, null, 240f, confirmRoot: root, cellScale: 0.66f);
                if (!inst) Appear(area, t0 + 0.4f);
            }

            // ---- ⑤ボタン ----
            // 敗北は「もう一度」が主 (真鍮)・走破は「タイトルへ」が主。レポートは小さく右下
            float bw = ph ? 300f : 340f, bw2 = ph ? 220f : 280f;
            RunUi.BottomButton(root, "もう一度（同じリーダー）", delegate { Audio.Ui("start_run"); g.RestartRun(); }, ph ? 16 : 18, bw, btnH, -(bw2 / 2f + 12f), btnBottom, won ? (Color?)null : PaperFx.BrassLight);
            RunUi.BottomButton(root, "タイトルへ", delegate { g.BackToSetup(); }, ph ? 16 : 18, bw2, btnH, bw / 2f + 12f, btnBottom, won ? PaperFx.BrassLight : (Color?)null);
            // レポートの書き出し (2026-09-14): ランの決着はデータ回収の主戦場
            float repW = ph ? 200f : 240f;
            RunUi.BottomButton(root, "レポートを書き出す", delegate { FeedbackUi.ExportNow(g); }, ph ? 13 : 14, repW, ph ? 48f : 40f, cs.x / 2f - repW / 2f - (ph ? 20f : 32f), ph ? 18f : 22f);
            RunUi.Message(g, root);
        }

        /// <summary>ランの記録 (無い物は出さない)。出どころ＝RunState・Feedback.History (戦闘の保管)・Feedback.Choices (選択履歴)</summary>
        static List<Rec> Records(RunState run)
        {
            var list = new List<Rec>();
            list.Add(new Rec { Label = "到達", Text = "幕 " + run.Act + " ・ 行 " + (run.Row + 1) });
            list.Add(new Rec { Label = "勝利", Value = run.BattlesWon, Suffix = " 戦" });
            var hist = Feedback.History;
            if (hist.Count > 0)
            {
                int elites = 0, bosses = 0, turns = 0, maxTurn = 0, fires = 0; bool metrics = false;
                for (int i = 0; i < hist.Count; i++)
                {
                    var h = hist[i];
                    if (h.Result == "won") { if (h.Boss == true) bosses++; else if (h.Elite) elites++; }
                    turns += h.Turns;
                    if (h.Metrics != null) { metrics = true; maxTurn = Math.Max(maxTurn, h.Metrics.MaxTurnDamage); fires += h.Metrics.Fires; }
                }
                list.Add(new Rec { Label = "倒した強個体", Value = elites, Suffix = " 体" });
                list.Add(new Rec { Label = "倒した幕ボス", Value = bosses, Suffix = " 体" });
                list.Add(new Rec { Label = "戦ったターン", Value = turns, Suffix = "" });
                if (metrics && maxTurn > 0) list.Add(new Rec { Label = "最大の 1 ターン火力", Value = maxTurn, Suffix = "" });
                if (metrics && fires > 0) list.Add(new Rec { Label = "からくりの発動", Value = fires, Suffix = " 回" });
            }
            list.Add(new Rec { Label = "残り HP", Value = run.Hp, Suffix = " / " + run.MaxHp });
            list.Add(new Rec { Label = "所持金", Value = run.Gold, Suffix = " G" });
            list.Add(new Rec { Label = "デッキ", Value = run.Deck.Count, Suffix = " 枚" });
            if (Feedback.Choices.Count > 0) list.Add(new Rec { Label = "選んだ回数", Value = Feedback.Choices.Count, Suffix = " 回" });
            if (list.Count % 2 == 1) list.Add(new Rec { Label = "難易度", Value = run.Difficulty, Suffix = "" });
            return list;
        }

        /// <summary>敗北の「次は…」の一言。敗因の 2 分類 (構築の失敗／理不尽) に合わせた定型。造語なし</summary>
        static string LossHint(string lossFeel, RunState run)
        {
            if (lossFeel == "build") return "次は、焚き火で休む前に、守りの札を一枚足してみる";
            if (lossFeel == "unfair") return "次は、その敵の予告を読んでから、札を出す順を決める";
            if (run != null && run.Hp <= 0 && run.Act >= 2) return "次は、HP が半分を切ったら焚き火で休む";
            return "次は、店で基本の札を一枚取り除いてみる";
        }

        /// <summary>数字を 0 から値まで数え上げる (0.4 秒・OutQuad)</summary>
        static void CountUp(TMP_Text t, int to, string suffix, float dur)
        {
            if (t == null) return;
            Tween.Run(dur, k => { if (t != null) t.text = Mathf.RoundToInt(Mathf.Lerp(0f, to, k)) + suffix; }, Ease.OutQuad, () => { if (t != null) t.text = to + suffix; });
        }

        /// <summary>delay 秒後に、α 0→1 と 12px の上がりで現れる (入りの動きと同じ作法)</summary>
        static void Appear(RectTransform rt, float delay)
        {
            if (rt == null) return;
            var cg = rt.gameObject.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            var p0 = rt.anchoredPosition;
            rt.anchoredPosition = p0 - new Vector2(0f, 12f);
            Tween.After(delay, () =>
            {
                if (rt == null) return;
                Tween.Run(0.28f, k => { if (rt == null) return; cg.alpha = Mathf.Clamp01(k * 1.6f); rt.anchoredPosition = p0 - new Vector2(0f, 12f * (1f - k)); }, Ease.OutCubic);
            });
        }
    }
}
