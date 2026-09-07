// BattleScreen.cs — 戦闘画面 (2026-09-07 M2: モダンな戦闘UIの文法へ作り直し)。
// 1920×1080 のキャンバス直下に組む。上=状況バー／中=戦場 (左にリーダーと伏せ場・右に敵)／下=扇状の手札と山札・捨て札・ターン終了。
// ログは引き出し (既定は閉)。確認ウィンドウ・対象選択・追加コストのピッカーはモーダル。
// ここは「状態を読んでコマンドを投げるだけ」。演出は Presenter (イベントログ差分) が担う。
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
    public static class BattleScreen
    {
        public const float TopH = 72f;
        public const float HandY = 30f;        // 手札の下端 (キャンバス下からの距離)
        public const float CardScale = 0.92f;

        public static void Build(GameRoot g, RectTransform root)
        {
            var run = g.Rs;
            var st = run.Combat;
            var v = BattleView.Ensure(g, root);
            v.SyncField(g, st);
            v.ClearUi();
            var ui = v.UiLayer;
            BuildPiles(g, ui, st);
            BuildEndTurn(g, ui, st);
            BuildTopBar(g, ui, run, st);
            if (g.ShowLog) BuildLogDrawer(g, ui, st);
            v.SyncHand(g, st, true);

            if (st.Phase == CombatPhases.AwaitingReaction) BuildReactionWindow(g, ui, st);
            else if (g.Pending != null)
            {
                var need = g.Pending.NextNeed();
                if (need == "target") BuildTargetBanner(g, ui);
                else if (need != null) BuildPicker(g, ui, st, need);
            }
            else if (g.ModeChoiceUid != null) BuildModeChooser(g, ui, st);
        }

        // ---- 背景 ----

        public static void BuildBackground(RectTransform root, RunState run) { BuildBackground(root, run.Act); }

        public static void BuildBackground(RectTransform root, int act)
        {
            // 背景は舞台 (別カメラ・ポスト処理と粒子つき) に描く。UI 側 (root) には何も置かない
            Stage.Paint(act);
        }

        static void BuildTopBar(GameRoot g, RectTransform root, RunState run, GameState st)
        {
            var bar = UiKit.NewRect("topbar", root);
            UiKit.Anchor(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -TopH), new Vector2(0f, 0f));
            var hg = UiKit.Horz(bar, 12, 0);
            hg.padding = new RectOffset(28, 28, 0, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandHeight = false;
            hg.childForceExpandWidth = false;

            string enc = "";
            try
            {
                var node = DeckRogue.Engine.Run.CurrentNode(run);
                if (node != null && node.EncounterId != null) enc = Content.EncounterName(node.EncounterId);
            }
            catch (Exception) { }
            var title = Tag(bar, 40f, -0.6f);
            var tl = UiKit.Txt(title, "幕 " + run.Act + " · 行 " + (run.Row + 1), 11, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            tl.characterSpacing = 2f;
            UiKit.Le(tl, -1f, 30f, -1f, 30f);
            var te = UiKit.Deco(title, enc, 19, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(te, -1f, 30f, -1f, 30f);
            var turn = Tag(bar, 32f, 1f);
            UiKit.Icon(turn, "set", 16f, PaperFx.InkSoft);
            var tt = UiKit.Txt(turn, "ターン " + st.Turn, 13, PaperFx.Ink, TextAnchor.MiddleLeft, true);
            UiKit.Le(tt, -1f, 26f, -1f, 26f);

            var spacer = UiKit.NewRect("spacer", bar);
            UiKit.Le(spacer, 10f, 10f, -1f, -1f, 1f, -1f);

            var gold = Tag(bar, 34f, 0f);
            UiKit.Icon(gold, "gold", 16f);
            var gt = UiKit.Deco(gold, run.Gold.ToString(), 18, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(gt, -1f, 28f, -1f, 28f);
            var gl = UiKit.Txt(gold, "G", 11, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(gl, -1f, 28f, -1f, 28f);

            for (int i = 0; i < run.Relics.Count && i < 8; i++)
            {
                RelicDef rd = null;
                try { rd = Content.GetRelicDef(run.Relics[i]); } catch (Exception) { }
                var cell = UiKit.NewRect("relic", bar);
                UiKit.Le(cell, 34f, 34f, 34f, 34f);
                var disc = cell.gameObject.AddComponent<Image>();
                disc.sprite = PaperFx.Disc(); disc.preserveAspect = true;
                RunUi.RelicArt(cell, run.Relics[i], 20f);
                var tip = rd != null ? "<b>" + rd.Name + "</b>\n" + rd.Description : run.Relics[i];
                Tooltip.Attach(cell.gameObject, delegate { return tip; });
            }
            var logBtn = UiKit.Btn(bar, g.ShowLog ? "ログを閉じる" : "ログ", delegate { g.ShowLog = !g.ShowLog; g.Rebuild(); }, 13);
            SetSize(logBtn, g.ShowLog ? 130f : 84f, 34f);

            // エラー・通知は上部バーの下に (紙の札)
            string msg = g.Error != null ? "! " + g.Error : (g.Notice != null ? g.Notice : null);
            if (msg != null)
            {
                var note = UiKit.NewRect("message", root);
                UiKit.Anchor(note, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-460f, -TopH - 48f), new Vector2(460f, -TopH - 8f));
                var nImg = PaperFx.Sheet(note, PaperFx.Tag, "paper", g.Error != null ? new Color(1f, 0.85f, 0.8f, 1f) : Color.white);
                UiKit.Stretch(nImg.rectTransform, 0f, 0f, 0f, 0f);
                var m = UiKit.Txt(note, msg, 15, g.Error != null ? UiKit.ColBad : PaperFx.Ink, TextAnchor.MiddleCenter, true);
                UiKit.Stretch(m.rectTransform, 12f, 12f, 0f, 0f);
            }
        }

        /// <summary>紙の札 (横並びの入れ物)。少し傾けて手で置いた感じに</summary>
        public static RectTransform Tag(Transform parent, float height, float rot, Color? tint = null)
        {
            var rt = UiKit.NewRect("tag", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = PaperFx.Tag; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f;
            img.color = tint ?? Color.white;
            img.raycastTarget = false;
            var hg = UiKit.Horz(rt, 6, 0);
            hg.padding = new RectOffset(10, 10, 0, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandHeight = false;
            hg.childForceExpandWidth = false;
            var le = UiKit.Le(rt, -1f, height, -1f, height);
            le.flexibleWidth = 0f;
            var fit = rt.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (rot != 0f) rt.localRotation = Quaternion.Euler(0f, 0f, rot);
            return rt;
        }

        static void Chip(Transform parent, string icon, string text, Color color, int size)
        {
            var tag = Tag(parent, size + 12f, 0f);
            UiKit.Icon(tag, icon, 16f);
            var t = UiKit.Txt(tag, text, size, PaperFx.Ink, TextAnchor.MiddleLeft, true);
            UiKit.Le(t, 30f, size + 8f, -1f, size + 8f);
        }

        /// <summary>縦レイアウトの中に、横いっぱいに伸びない中央寄せのボタンを置く</summary>
        public static Button CenteredButton(Transform parent, string label, Action onClick, int size, float w, float h, Color? bg = null)
        {
            var row = UiKit.NewRect("btnrow", parent);
            UiKit.Le(row, -1f, h, -1f, h);
            var hg = UiKit.Horz(row, 0, 0);
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childForceExpandWidth = false;
            hg.childForceExpandHeight = false;
            var b = UiKit.Btn(row, label, onClick, size, true, bg);
            SetSize(b, w, h);
            return b;
        }

        public static void SetSize(Component c, float w, float h)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = w; le.preferredWidth = w; le.minHeight = h; le.preferredHeight = h; le.flexibleWidth = 0f; le.flexibleHeight = 0f;
        }

        // ---- 敵 ----

        /// <summary>敵パネルの中身 (入れ物 pan は BattleView が持ち越す)。shownHp は演出で先に減らした表示値 (実値と違えば滑らせる)</summary>
        public static void FillEnemyPanel(GameRoot g, RectTransform pan, GameState st, int index, int shownHp)
        {
            var e = st.Enemies[index];
            bool alive = e.Hp > 0;
            bool aimed = g.PreferredTarget == index || (g.Pending != null && g.Pending.TargetIndex.HasValue && g.Pending.TargetIndex.Value == index);
            bool targeting = g.Pending != null && g.Pending.NextNeed() == "target";
            g.RegisterAnchor("enemy" + index, pan);

            EnemyDef def = null;
            try { def = Content.GetEnemyDef(e.EnemyId); } catch (Exception) { }
            string nm = def != null ? def.Name : e.EnemyId;
            // ドット絵は整数倍 (通常・エリート 128→2倍=256px、ボスは3倍=384px)。吹き出しは絵の上端に合わせる
            // 密度はオクトラ相当 (1ドット=画面4px): 通常 64→256・エリート 80→320・ボス 96→384 がどれも4倍になる目安
            string nodeType = null;
            try { var node = DeckRogue.Engine.Run.CurrentNode(g.Rs); nodeType = node != null ? node.Type : null; } catch (Exception) { }
            float artTarget = nodeType == MapNodeTypes.Boss ? 384f : nodeType == MapNodeTypes.Elite ? 320f : 256f;
            var artSprite = Creature.Get("enemies", e.EnemyId, false, (int)(artTarget / 4f));
            float spriteTop = 130f + artSprite.rect.height * PaperFx.PixelScale(artSprite, artTarget);

            // 意図 (頭上の紙の吹き出し)
            if (alive)
            {
                var it = e.Intent;
                var bubble = UiKit.NewRect("intent", pan);
                UiKit.Anchor(bubble, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-110f, spriteTop + 70f), new Vector2(110f, spriteTop + 124f));
                var bImg = PaperFx.Sheet(bubble, PaperFx.Panel, "paper");
                UiKit.Stretch(bImg.rectTransform, 0f, 0f, 0f, 0f);
                bImg.raycastTarget = false;
                var tail = UiKit.NewRect("tail", pan);
                UiKit.Anchor(tail, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-12f, spriteTop + 54f), new Vector2(18f, spriteTop + 72f));
                var tImg = tail.gameObject.AddComponent<Image>();
                tImg.sprite = PaperFx.BubbleTail(); tImg.raycastTarget = false;
                var row = UiKit.NewRect("row", bubble);
                UiKit.Stretch(row, 0f, 0f, 0f, 0f);
                var ig = UiKit.Horz(row, 10, 0);
                ig.childAlignment = TextAnchor.MiddleCenter;
                ig.childForceExpandHeight = false;
                ig.childForceExpandWidth = false;
                if (it != null)
                {
                    var intentArt = Theme.Art("icons", "intent_" + it.Kind);
                    var ic = UiKit.Icon(row, IntentIcon(it.Kind), 32f, intentArt != null ? Color.white : IntentColor(it.Kind));
                    if (intentArt != null) { ic.sprite = intentArt; ic.rectTransform.sizeDelta = new Vector2(48f, 48f); UiKit.Le(ic, 48f, 48f, 48f, 48f); }
                    else UiKit.Le(ic, 32f, 32f, 32f, 32f);
                    var itT = UiKit.Deco(row, IntentShort(it), 26, PaperFx.Ink, TextAnchor.MiddleLeft);
                    UiKit.Le(itT, 40f, 40f, -1f, 40f);
                }
                // 分岐・付与などの詳細は吹き出しの下に小さく (舞台の上なので紙色)
                var detailText = IntentDetail(st, index, it);
                var detail = UiKit.Txt(pan, detailText, 14, PaperFx.Paper, TextAnchor.UpperCenter);
                detail.outlineWidth = 0.3f; detail.outlineColor = new Color(0.1f, 0.06f, 0.1f, 0.95f);
                UiKit.Anchor(detail.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-30f, spriteTop + 4f), new Vector2(30f, spriteTop + 52f));
            }

            // 足元の影・貼り絵の縁・ドット絵
            if (targeting && alive)
            {
                var glow = PaperFx.BlobImage(pan, PaperFx.Honey, "glow");
                UiKit.Anchor(glow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-130f, 100f), new Vector2(130f, 170f));
                glow.color = new Color(1f, 1f, 1f, 0.5f);
            }
            // 絵は舞台 (HD-2D) のビルボードが描く。UI 側の矩形は位置・大きさ・色 (生死/点滅) の基準として残す
            var spr = UiKit.NewRect("sprite", pan);
            PaperFx.FitPixel(spr, artSprite, 0f, 130f, artTarget);
            var img = spr.gameObject.AddComponent<Image>();
            img.sprite = artSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = alive ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            Stage.BindUnit("enemy" + index, spr, img, artSprite);
            if (aimed)
            {
                var ring = UiKit.NewRect("ring", pan);
                UiKit.Anchor(ring, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-116f, 116f), new Vector2(116f, 146f));
                var rImg = ring.gameObject.AddComponent<Image>();
                rImg.sprite = PaperFx.Ring(6); rImg.color = PaperFx.Honey; rImg.raycastTarget = false;
                rImg.preserveAspect = false;
            }

            // 名前の札・HP
            var nameTag = UiKit.NewRect("nametag", pan);
            UiKit.Anchor(nameTag, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-72f, 90f), new Vector2(72f, 124f));
            nameTag.localRotation = Quaternion.Euler(0f, 0f, index % 2 == 0 ? 1f : -1f);
            var ntImg = PaperFx.Sheet(nameTag, PaperFx.Tag, "paper", alive ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f));
            UiKit.Stretch(ntImg.rectTransform, 0f, 0f, 0f, 0f);
            ntImg.raycastTarget = false;
            var nameT = UiKit.Deco(nameTag, nm + (alive ? "" : (e.Fled == true ? "（逃走）" : "（撃破）")), 19, PaperFx.Ink, TextAnchor.MiddleCenter);
            UiKit.Stretch(nameT.rectTransform, 6f, 6f, 0f, 0f);
            nameT.textWrappingMode = TextWrappingModes.NoWrap;
            HpBar(pan, new Vector2(0.1f, 0f), new Vector2(0.9f, 0f), 66f, 84f, shownHp, e.MaxHp, e.Block);
            if (shownHp != e.Hp) TweenHpBar(pan, e.Hp);

            // 状態の札
            var chips = new List<KeyValuePair<string, string>>();
            if (e.Strength != 0) chips.Add(new KeyValuePair<string, string>("sword", "筋力" + (e.Strength > 0 ? "+" : "") + e.Strength));
            if (e.Burn > 0) chips.Add(new KeyValuePair<string, string>("burn", "延焼" + e.Burn));
            if (e.Exposed > 0) chips.Add(new KeyValuePair<string, string>("exposed", "急所" + e.Exposed));
            if (e.Confusion > 0) chips.Add(new KeyValuePair<string, string>("exposed", "混乱" + e.Confusion));
            if ((e.Weak ?? 0) > 0) chips.Add(new KeyValuePair<string, string>("shield", "威圧" + e.Weak.Value));
            if ((e.Artifact ?? 0) > 0) chips.Add(new KeyValuePair<string, string>("set", "AF" + e.Artifact.Value));
            if (e.BurrowActive == true) chips.Add(new KeyValuePair<string, string>("shield", "潜伏"));
            string traits = CardText.EnemyTraits(def);
            var chipRow = UiKit.NewRect("chips", pan);
            UiKit.Anchor(chipRow, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 30f), new Vector2(0f, 58f));
            var cg = UiKit.Horz(chipRow, 6, 0);
            cg.childAlignment = TextAnchor.MiddleCenter;
            cg.childForceExpandHeight = false;
            cg.childForceExpandWidth = false;
            for (int i = 0; i < chips.Count; i++) SmallChip(chipRow, chips[i].Key, chips[i].Value, PaperFx.Ink);
            if (traits.Length > 0)
            {
                var tr = UiKit.Txt(pan, traits, 12, PaperFx.Paper, TextAnchor.UpperCenter);
                tr.outlineWidth = 0.18f; tr.outlineColor = new Color(0f, 0f, 0f, 0.7f);
                UiKit.Anchor(tr.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-10f, 2f), new Vector2(10f, 28f));
            }
        }

        /// <summary>敵の吹き出し (名前・HP・意図・特性)。用語解説は Tooltip 側が足す</summary>
        public static string EnemyTip(GameRoot g, int index)
        {
            var cur = g.Rs != null ? g.Rs.Combat : null;
            if (cur == null || index >= cur.Enemies.Count) return null;
            var ce = cur.Enemies[index];
            EnemyDef def = null;
            try { def = Content.GetEnemyDef(ce.EnemyId); } catch (Exception) { }
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>").Append(def != null ? def.Name : ce.EnemyId).Append("</b>  HP ").Append(ce.Hp).Append(" / ").Append(ce.MaxHp);
            if (ce.Block > 0) sb.Append("  ブロック").Append(ce.Block);
            if (ce.Hp > 0) sb.Append("\n意図: ").Append(CardText.IntentText(cur, index));
            string traits = CardText.EnemyTraits(def);
            if (traits.Length > 0) sb.Append("\n特性: ").Append(traits);
            return sb.ToString();
        }

        static void SmallChip(Transform parent, string icon, string text, Color color)
        {
            var tag = Tag(parent, 26f, 0f);
            var img = tag.GetComponent<Image>();
            img.raycastTarget = true;
            Tooltip.Attach(tag.gameObject, delegate { return text; });
            var dot = UiKit.Pan(tag, DotColor(icon, text), "dot");
            UiKit.Le(dot, 10f, 10f, 10f, 10f);
            UiKit.Icon(tag, icon, 16f, PaperFx.Ink);
            var t = UiKit.Txt(tag, text, 12, PaperFx.Ink, TextAnchor.MiddleLeft, true);
            UiKit.Le(t, 20f, 24f, -1f, 24f);
        }

        static Color DotColor(string icon, string text)
        {
            if (text.StartsWith("成長")) return PaperFx.Moss;
            if (text.StartsWith("勢い")) return PaperFx.Honey;
            if (text.StartsWith("急所")) return UiKit.Hex("#e0a04a");
            if (text.StartsWith("延焼")) return UiKit.Hex("#e8742f");
            if (text.StartsWith("弱") || text.StartsWith("脆") || text.StartsWith("虚") || text.StartsWith("重") || text.StartsWith("拘") || text.StartsWith("霞")) return PaperFx.Plum;
            if (text.StartsWith("筋力")) return UiKit.Hex("#b7a89a");
            return PaperFx.Sky;
        }

        /// <summary>HP バーの値 (演出で滑らせるために持つ)</summary>
        public class HpBarInfo : MonoBehaviour { public int Max; public int Value; public RectTransform Fill; public TMP_Text Label; }

        /// <summary>入れ物の中の HP バーを target まで滑らせる (無ければ何もしない)</summary>
        public static void TweenHpBar(RectTransform container, int target)
        {
            if (container == null) return;
            var info = container.GetComponentInChildren<HpBarInfo>();
            if (info == null || info.Fill == null) return;
            int from = info.Value;
            info.Value = target;
            float r0 = info.Max > 0 ? Mathf.Clamp01((float)from / info.Max) : 0f;
            float r1 = info.Max > 0 ? Mathf.Clamp01((float)target / info.Max) : 0f;
            var fill = info.Fill; var label = info.Label; int max = info.Max;
            Tween.Run(0.35f, k =>
            {
                if (fill == null) return;
                fill.anchorMax = new Vector2(Mathf.Lerp(r0, r1, k), 1f);
                if (label != null) label.text = Mathf.RoundToInt(Mathf.Lerp(from, target, k)) + " / " + max;
            }, Ease.OutCubic);
        }

        static void HpBar(RectTransform parent, Vector2 aMin, Vector2 aMax, float yMin, float yMax, int hp, int max, int block)
        {
            var bar = UiKit.NewRect("hpbar", parent);
            UiKit.Anchor(bar, aMin, aMax, new Vector2(0f, yMin), new Vector2(0f, yMax));
            // 紙の帯 (墨の縁) に薔薇色の水彩
            var edge = bar.gameObject.AddComponent<Image>();
            edge.color = PaperFx.Ink;
            edge.raycastTarget = false;
            var track = UiKit.Pan(bar, PaperFx.Paper, "track");
            UiKit.Stretch(track.rectTransform, 2f, 2f, 2f, 2f);
            track.raycastTarget = false;
            float r = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
            var fill = UiKit.NewRect("fill", bar);
            var fimg = fill.gameObject.AddComponent<Image>();
            fimg.sprite = ThemeFx.Gradient("hpfill", Color.Lerp(PaperFx.Rose, Color.white, 0.15f), Color.Lerp(PaperFx.Rose, Color.black, 0.08f));
            fimg.raycastTarget = false;
            UiKit.Anchor(fill, new Vector2(0f, 0f), new Vector2(r, 1f), new Vector2(3f, 3f), new Vector2(0f, -3f));
            var t = UiKit.Txt(bar, hp + " / " + max, 13, PaperFx.Ink, TextAnchor.MiddleCenter, true);
            UiKit.Stretch(t.rectTransform, 0f, 0f, 0f, 0f);
            var info = bar.gameObject.AddComponent<HpBarInfo>();
            info.Max = max; info.Value = hp; info.Fill = fill; info.Label = t;
            if (block > 0)
            {
                var b = UiKit.NewRect("block", bar);
                UiKit.Anchor(b, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-54f, -18f), new Vector2(-8f, 18f));
                var blob = PaperFx.BlobImage(b, PaperFx.Sky);
                UiKit.Stretch(blob.rectTransform, 0f, 0f, 0f, 0f);
                var row = UiKit.NewRect("row", b);
                UiKit.Stretch(row, 0f, 0f, 0f, 0f);
                var hg = UiKit.Horz(row, 2, 0);
                hg.childAlignment = TextAnchor.MiddleCenter; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
                var ic = UiKit.Icon(row, "shield", 14f, PaperFx.Ink);
                UiKit.Le(ic, 14f, 14f, 14f, 14f);
                var bt = UiKit.Txt(row, block.ToString(), 15, PaperFx.Ink, TextAnchor.MiddleCenter, true);
                UiKit.Le(bt, 12f, 20f, -1f, 20f);
            }
        }

        /// <summary>頭上の短縮表示に無い情報 (分岐・付与・攻防一体・応援など) がある時だけ詳細行を出す</summary>
        static string IntentDetail(GameState st, int index, EnemyIntent it)
        {
            string full = CardText.IntentText(st, index);
            string[] marks = { "【", "※", "+", "付与", "伏せ", "従者", "弱体", "脆弱", "虚弱", "負傷", "火傷", "拘束", "霞", "重り", "がらくた", "→", "手数" };
            for (int i = 0; i < marks.Length; i++) if (full.Contains(marks[i])) return full;
            return "";
        }

        static string IntentIcon(string kind)
        {
            switch (kind)
            {
                case "attack": return "sword";
                case "defend": return "shield";
                case "buff": case "rally": return "growth";
                case "heal": return "heart";
                case "hex": return "burn";
                case "destroy-set": case "destroy-token": return "exhaust";
                case "steal-gold": return "gold";
                case "flee": return "momentum";
                case "mill": return "draw";
                case "rest": return "set";
                default: return "exposed";
            }
        }

        static Color IntentColor(string kind)
        {
            switch (kind)
            {
                case "attack": return UiKit.Hex("#ff6b57");
                case "defend": return UiKit.ColBlock;
                case "buff": case "rally": return UiKit.Hex("#ffb14a");
                case "heal": return UiKit.ColAccent;
                case "rest": return UiKit.ColDim;
                default: return UiKit.Hex("#d8a7ff");
            }
        }

        /// <summary>頭上の短い意図: 「3〜5」「3〜5×2」「防御」など (詳細は IntentText)</summary>
        static string IntentShort(EnemyIntent it)
        {
            switch (it.Kind)
            {
                case "attack":
                {
                    string s = it.ShownMin == it.ShownMax ? it.ShownMin.ToString() : it.ShownMin + "〜" + it.ShownMax;
                    if ((it.Hits ?? 1) > 1) s += " ×" + it.Hits.Value;
                    if (it.MirrorHits == true) s += " ×手数";
                    return s;
                }
                case "defend": return "防御";
                case "buff": return "筋力+";
                case "rally": return "応援";
                case "heal": return "回復";
                case "hex": return "呪い";
                case "destroy-set": return "伏せ破壊";
                case "destroy-token": return "従者狩り";
                case "steal-gold": return "盗み";
                case "flee": return "逃走";
                case "mill": return "山札喰い";
                case "rest": return "隙";
                case "hatch": return "孵化";
                default: return it.Kind;
            }
        }

        // ---- リーダー・伏せ場・置物 ----

        /// <summary>リーダー欄の中身 (入れ物 area は BattleView が持ち越す)</summary>
        public static void FillPlayerPanel(GameRoot g, RectTransform area, GameState st, int shownHp)
        {
            var p = st.Player;

            string leaderId = g.Rs.LeaderId;
            string leaderName = leaderId;
            try { var ld = Content.GetLeaderDef(leaderId); leaderName = ld.Name; } catch (Exception) { }

            var spr = UiKit.NewRect("sprite", area);
            var leaderArt = Creature.Get("leaders", leaderId, true);
            PaperFx.FitPixel(spr, leaderArt, 0f, 130f);
            spr.anchorMin = spr.anchorMax = new Vector2(0f, 0f);
            spr.offsetMin += new Vector2(130f, 0f); spr.offsetMax += new Vector2(130f, 0f);
            var img = spr.gameObject.AddComponent<Image>();
            img.sprite = leaderArt;
            img.preserveAspect = true;
            img.raycastTarget = false;
            Stage.BindUnit("player", spr, img, leaderArt);

            var nameTag = UiKit.NewRect("nametag", area);
            UiKit.Anchor(nameTag, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 90f), new Vector2(240f, 124f));
            nameTag.localRotation = Quaternion.Euler(0f, 0f, -1f);
            var ntImg = PaperFx.Sheet(nameTag, PaperFx.Tag, "paper");
            UiKit.Stretch(ntImg.rectTransform, 0f, 0f, 0f, 0f);
            ntImg.raycastTarget = false;
            var nameT = UiKit.Deco(nameTag, leaderName, 19, PaperFx.Ink, TextAnchor.MiddleCenter);
            UiKit.Stretch(nameT.rectTransform, 6f, 6f, 0f, 0f);
            var hpRt = UiKit.NewRect("hpwrap", area);
            UiKit.Anchor(hpRt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(50f, 64f), new Vector2(250f, 84f));
            HpBar(hpRt, Vector2.zero, Vector2.one, 0f, 0f, shownHp, p.MaxHp, p.Block);
            if (shownHp != p.Hp) TweenHpBar(area, p.Hp);
            if (p.IceBlock > 0)
            {
                var ice = UiKit.Txt(area, "氷壁 " + p.IceBlock, 14, UiKit.Hex("#bfe6ff"), TextAnchor.MiddleLeft, true);
                ice.outlineWidth = 0.18f; ice.outlineColor = new Color(0f, 0f, 0f, 0.7f);
                UiKit.Anchor(ice.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(256f, 62f), new Vector2(380f, 88f));
            }

            // 資源・状態の札
            var col = UiKit.NewRect("chips", area);
            UiKit.Anchor(col, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 28f), new Vector2(0f, 58f));
            var vg = UiKit.Horz(col, 6, 0);
            vg.childAlignment = TextAnchor.MiddleLeft;
            vg.childForceExpandWidth = false;
            vg.childForceExpandHeight = false;
            var res = new List<KeyValuePair<string, string>>();
            if (p.Growth > 0) res.Add(new KeyValuePair<string, string>("growth", "成長 " + p.Growth));
            if (p.Momentum > 0) res.Add(new KeyValuePair<string, string>("momentum", "勢い " + p.Momentum));
            if (p.Aether > 0) res.Add(new KeyValuePair<string, string>("energy", "霊気 " + p.Aether));
            if (p.NextCardDiscount > 0) res.Add(new KeyValuePair<string, string>("energy", "次のカード -" + p.NextCardDiscount));
            if (p.SpellEchoes > 0) res.Add(new KeyValuePair<string, string>("draw", "反復 " + p.SpellEchoes));
            if (p.Weak > 0) res.Add(new KeyValuePair<string, string>("exposed", "弱体 " + p.Weak));
            if (p.Vulnerable > 0) res.Add(new KeyValuePair<string, string>("exposed", "脆弱 " + p.Vulnerable));
            if (p.Frail > 0) res.Add(new KeyValuePair<string, string>("exposed", "虚弱 " + p.Frail));
            if (p.Restrain > 0) res.Add(new KeyValuePair<string, string>("set", "拘束 " + p.Restrain));
            if ((p.Mist ?? 0) > 0) res.Add(new KeyValuePair<string, string>("draw", "霞み " + p.Mist.Value));
            if ((p.Slow ?? 0) > 0) res.Add(new KeyValuePair<string, string>("exposed", "重り " + p.Slow.Value));
            for (int i = 0; i < res.Count; i++) SmallChip(col, res[i].Key, res[i].Value, PaperFx.Ink);

            // 伏せ場 (リーダーの右): 点線のポケットに伏せ札の裏
            var setArea = UiKit.NewRect("setzone", area);
            UiKit.Anchor(setArea, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(300f, 110f), new Vector2(0f, 300f));
            var setTag = Tag(setArea, 26f, -2f);
            UiKit.Anchor(setTag, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -26f), new Vector2(0f, 0f));
            var stFit = setTag.GetComponent<ContentSizeFitter>();
            UiKit.Icon(setTag, "set", 14f, PaperFx.InkSoft);
            var setLabel = UiKit.Txt(setTag, "伏せ場 " + p.SetCards.Count + " / " + p.SetSlots, 12, PaperFx.Ink, TextAnchor.MiddleLeft, true);
            UiKit.Le(setLabel, -1f, 22f, -1f, 22f);
            for (int i = 0; i < p.SetSlots; i++)
            {
                var slot = UiKit.NewRect("slot" + i, setArea);
                UiKit.Anchor(slot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(i * 124f, 0f), new Vector2(i * 124f + 108f, 156f));
                g.RegisterAnchor("setslot" + i, slot);
                var pocket = PaperFx.Sheet(slot, PaperFx.Tag, "pocket", new Color(1f, 1f, 1f, 0.35f));
                UiKit.Stretch(pocket.rectTransform, -6f, -6f, -6f, -6f);
                pocket.raycastTarget = false;
                if (i < p.SetCards.Count)
                {
                    var sc = p.SetCards[i];
                    var back = PaperFx.Sheet(slot, PaperFx.Tag, "back", UiKit.Hex("#2b2d4d"));
                    UiKit.Stretch(back.rectTransform, 0f, 0f, 0f, 0f);
                    back.raycastTarget = false;
                    var q = UiKit.Icon(slot, "question", 32f, PaperFx.Paper);
                    UiKit.Anchor(q.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-16f, 4f), new Vector2(16f, 36f));
                    var ct = UiKit.Deco(slot, "伏せ札", 13, PaperFx.Paper, TextAnchor.MiddleCenter);
                    UiKit.Anchor(ct.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(4f, -28f), new Vector2(-4f, -4f));
                    string tip = "<b>" + sc.Def.Name + "</b>\n" + CardText.Body(sc.Def);
                    pocket.raycastTarget = true;
                    Tooltip.Attach(pocket.gameObject, delegate { return tip; });
                }
                else
                {
                    var et = UiKit.Txt(slot, "空き", 12, PaperFx.Paper, TextAnchor.MiddleCenter);
                    et.outlineWidth = 0.18f; et.outlineColor = new Color(0f, 0f, 0f, 0.7f);
                    UiKit.Stretch(et.rectTransform, 0f, 0f, 0f, 0f);
                }
            }

            // 置物 (伏せ場の右): 紙の付箋
            var permRow = UiKit.NewRect("perms", setArea);
            UiKit.Anchor(permRow, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(p.SetSlots * 124f + 12f, 40f), new Vector2(0f, 156f));
            var pg = UiKit.Horz(permRow, 12, 0);
            pg.childAlignment = TextAnchor.UpperLeft;
            pg.childForceExpandWidth = false;
            pg.childForceExpandHeight = false;
            int shown = 0;
            for (int i = 0; i < p.Permanents.Count && shown < 4; i++)
            {
                var q = p.Permanents[i];
                if (q.Innate == true) continue;
                shown++;
                var note = UiKit.NewRect("perm", permRow);
                UiKit.Le(note, 150f, 112f, 150f, 112f);
                note.localRotation = Quaternion.Euler(0f, 0f, shown % 2 == 0 ? 1.2f : -1.5f);
                var nImg = PaperFx.Sheet(note, PaperFx.Panel, "paper");
                UiKit.Stretch(nImg.rectTransform, 0f, 0f, 0f, 0f);
                nImg.raycastTarget = true;
                string tip = "<b>" + q.Def.Name + "</b>\n" + CardText.Body(q.Def);
                Tooltip.Attach(note.gameObject, delegate { return tip; });
                var pin = UiKit.Pan(note, PaperFx.Rose, "pin");
                UiKit.Anchor(pin.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-6f, -4f), new Vector2(6f, 8f));
                pin.raycastTarget = false;
                var crest = UiKit.Icon(note, "crest_permanent", 20f, PaperFx.Ink);
                UiKit.Anchor(crest.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -36f), new Vector2(32f, -16f));
                var nt = UiKit.Deco(note, q.Def.Name, 14, PaperFx.Ink, TextAnchor.MiddleLeft);
                UiKit.Anchor(nt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(38f, -40f), new Vector2(-8f, -12f));
                nt.textWrappingMode = TextWrappingModes.NoWrap;
                var body = UiKit.Txt(note, CardText.Short(CardText.Body(q.Def), 26), 11, PaperFx.InkSoft, TextAnchor.UpperLeft);
                UiKit.Anchor(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(12f, 8f), new Vector2(-10f, -44f));
            }
        }

        // ---- 手札 (扇) ----

        /// <summary>カードの吹き出し: 本文は見えているので用語解説だけ (無ければ出さない)</summary>
        public static string KeywordsOnly(string text)
        {
            var terms = KeywordHelp.FindIn(text);
            if (terms.Count == 0) return null;
            var lines = new List<string>();
            for (int i = 0; i < terms.Count && i < 4; i++) lines.Add("<color=#8fd08c><b>" + terms[i] + "</b></color> " + KeywordHelp.Terms[terms[i]]);
            return string.Join("\n", lines.ToArray());
        }

        static bool _dragging;

        public static void HookHandCard(GameRoot g, BattleView.HandCard hc, CardInstance c)
        {
            var rt = hc.Rt;
            var et = rt.gameObject.AddComponent<EventTrigger>();
            // ドラッグ: カードを持ち上げて敵に落とすと対象指定して即プレイ、戦場に落とすとプレイ、手札に戻すと取り消し
            var beginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDrag.callback.AddListener(delegate
            {
                if (!hc.Playable) return;
                _dragging = true;
                rt.SetAsLastSibling();
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one * 0.8f;
            });
            et.triggers.Add(beginDrag);
            var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            drag.callback.AddListener(delegate (BaseEventData d)
            {
                if (!_dragging) return;
                var pd = d as PointerEventData;
                var parent = rt.parent as RectTransform;
                Vector2 local;
                if (pd != null && parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pd.position, null, out local)) rt.anchoredPosition = local;
            });
            et.triggers.Add(drag);
            var endDrag = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            endDrag.callback.AddListener(delegate (BaseEventData d)
            {
                if (!_dragging) return;
                _dragging = false;
                var pd = d as PointerEventData;
                int enemyIdx = pd != null ? EnemyUnderPointer(pd) : -1;
                bool overField = pd != null && pd.position.y > Screen.height * 0.36f;
                if (c.Def.Modes != null && c.Def.Modes.Count > 0)
                {
                    if (overField) { g.PreferredTarget = enemyIdx; g.ModeChoiceUid = c.Uid; g.Rebuild(); }
                    else { Tween.Move(rt, hc.BasePos, 0.15f); Tween.Scale(rt, Vector3.one * CardScale, 0.15f); rt.localRotation = Quaternion.Euler(0f, 0f, hc.BaseRot); RestoreOrder(rt); }
                    return;
                }
                if (enemyIdx >= 0 || overField)
                {
                    if (enemyIdx >= 0) g.PreferredTarget = enemyIdx;
                    PlayCard(g, c, null);
                    return;
                }
                Tween.Move(rt, hc.BasePos, 0.15f);
                Tween.Scale(rt, Vector3.one * CardScale, 0.15f);
                rt.localRotation = Quaternion.Euler(0f, 0f, hc.BaseRot);
                RestoreOrder(rt);
            });
            et.triggers.Add(endDrag);
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(delegate
            {
                if (_dragging) return;
                Audio.Hover();
                rt.SetAsLastSibling();
                Tween.Scale(rt, Vector3.one * 1.18f, 0.12f, Ease.OutQuad);
                Tween.Move(rt, hc.BasePos + new Vector2(0f, 70f), 0.12f, Ease.OutQuad);
                rt.localRotation = Quaternion.identity;
            });
            et.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(delegate
            {
                if (_dragging) return;
                Tween.Scale(rt, Vector3.one * CardScale, 0.12f, Ease.OutQuad);
                Tween.Move(rt, hc.BasePos, 0.12f, Ease.OutQuad);
                rt.localRotation = Quaternion.Euler(0f, 0f, hc.BaseRot);
                RestoreOrder(rt);
            });
            et.triggers.Add(exit);
            var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            click.callback.AddListener(delegate (BaseEventData d)
            {
                var pd = d as PointerEventData;
                if (pd != null && pd.button == PointerEventData.InputButton.Right)
                {
                    if (hc.Settable) g.DoCombat(new Command_SetCard { CardUid = c.Uid });
                    return;
                }
                if (pd != null && pd.dragging) return;
                if (c.Def.Modes != null && c.Def.Modes.Count > 0) { g.ModeChoiceUid = c.Uid; g.Rebuild(); return; }
                if (hc.Playable) PlayCard(g, c, null);
                else if (hc.Settable) { g.ModeChoiceUid = c.Uid; g.Rebuild(); }
            });
            et.triggers.Add(click);

            // 伏せられる札には「伏せる」ボタン (ホバー中だけ・カードの足元)
            if (hc.Settable)
            {
                var sb = UiKit.Btn(rt, "伏せる", delegate { g.DoCombat(new Command_SetCard { CardUid = c.Uid }); }, 15, true, UiKit.Hex("#bfe3dc"));
                var sle = sb.GetComponent<LayoutElement>();
                if (sle != null) UnityEngine.Object.Destroy(sle);
                var srt = sb.GetComponent<RectTransform>();
                UiKit.Anchor(srt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-60f, -22f), new Vector2(60f, 22f));
                sb.gameObject.SetActive(false);
                var showSet = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                showSet.callback.AddListener(delegate { if (sb != null) sb.gameObject.SetActive(true); });
                et.triggers.Add(showSet);
                var hideSet = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                hideSet.callback.AddListener(delegate { if (sb != null) sb.gameObject.SetActive(false); });
                et.triggers.Add(hideSet);
            }
        }

        /// <summary>ポインタの下にある敵パネルの添字 (無ければ -1)</summary>
        static int EnemyUnderPointer(PointerEventData pd)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pd, results);
            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                for (var t = go.transform; t != null; t = t.parent)
                {
                    int idx;
                    if (t.name.StartsWith("enemy") && int.TryParse(t.name.Substring(5), out idx)) return idx;
                }
            }
            return -1;
        }

        /// <summary>カードをプレイする。行き先の演出 (敵へ飛ぶ→捨て札) は GameRoot.SubmitIfReady が LastPlayed を記録し BattleView.SyncHand が行う</summary>
        public static void PlayCard(GameRoot g, CardInstance c, int? modeIndex)
        {
            g.BeginPlay(c, modeIndex);
        }

        static void RestoreOrder(RectTransform rt)
        {
            // 名前 handN の N で元の並びに戻す
            var name = rt.name;
            int idx;
            if (name.StartsWith("hand") && int.TryParse(name.Substring(4), out idx)) rt.SetSiblingIndex(Mathf.Min(idx, rt.parent.childCount - 1));
        }

        // ---- 山札・捨て札・消滅・ターン終了 ----

        static void BuildPiles(GameRoot g, RectTransform root, GameState st)
        {
            var p = st.Player;
            Pile(g, root, new Vector2(0f, 0f), new Vector2(24f, 24f), "draw", "山札", p.DrawPile.Count, -1, delegate { g.ViewPile = "draw"; g.Rebuild(); }, "pile-draw");
            Pile(g, root, new Vector2(1f, 0f), new Vector2(-214f, 24f), "exhaust", "捨て札 / 消滅", p.DiscardPile.Count, p.ExhaustPile.Count, delegate { g.ViewPile = "discard"; g.Rebuild(); }, "pile-discard");
            if (g.ViewPile != null) BuildPileViewer(g, root, st);
        }

        static void Pile(GameRoot g, RectTransform root, Vector2 anchor, Vector2 offset, string icon, string label, int count, int count2 = -1, Action onClick = null, string anchorName = null)
        {
            var rt = UiKit.NewRect("pile-" + icon, root);
            if (anchorName != null) g.RegisterAnchor(anchorName, rt);
            UiKit.Anchor(rt, anchor, anchor, offset, offset + new Vector2(count2 >= 0 ? 190f : 130f, 40f));
            rt.localRotation = Quaternion.Euler(0f, 0f, anchor.x > 0.5f ? 1f : -1f);
            var frame = PaperFx.Sheet(rt, PaperFx.Tag, "paper");
            UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            frame.raycastTarget = onClick != null;
            if (onClick != null)
            {
                var pb = rt.gameObject.AddComponent<Button>();
                pb.targetGraphic = frame;
                pb.transition = Selectable.Transition.None;
                pb.onClick.AddListener(delegate { onClick(); });
            }
            var row = UiKit.NewRect("row", rt);
            UiKit.Stretch(row, 0f, 0f, 0f, 0f);
            var hg = UiKit.Horz(row, 6, 0);
            hg.padding = new RectOffset(12, 12, 0, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            var ic = UiKit.Icon(row, icon, 16f, PaperFx.Ink);
            UiKit.Le(ic, 16f, 16f, 16f, 16f);
            var cnt = UiKit.Deco(row, count.ToString(), 17, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(cnt, -1f, 30f, -1f, 30f);
            var lb = UiKit.Txt(row, count2 >= 0 ? "捨て札" : label, 11, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(lb, -1f, 30f, -1f, 30f);
            if (count2 >= 0)
            {
                var sep = UiKit.Pan(row, PaperFx.InkSoft, "sep");
                UiKit.Le(sep, 1.5f, 14f, 1.5f, 14f);
                var cnt2 = UiKit.Deco(row, count2.ToString(), 17, PaperFx.Ink, TextAnchor.MiddleLeft);
                UiKit.Le(cnt2, -1f, 30f, -1f, 30f);
                var lb2 = UiKit.Txt(row, "消滅", 11, PaperFx.InkSoft, TextAnchor.MiddleLeft);
                UiKit.Le(lb2, -1f, 30f, -1f, 30f);
            }
        }

        /// <summary>山札 (名前順=引き順は伏せたまま) / 捨て札 / 消滅置き場の一覧モーダル</summary>
        static void BuildPileViewer(GameRoot g, RectTransform root, GameState st)
        {
            var inner = Modal(root, 1500f, 800f, "pileViewer");
            var tabs = UiKit.NewRect("tabs", inner);
            var tle = UiKit.Le(tabs, -1f, 48f, -1f, 48f);
            tle.flexibleHeight = 0f;
            var tg = UiKit.Horz(tabs, 10, 0);
            tg.childAlignment = TextAnchor.MiddleLeft;
            tg.childForceExpandWidth = false;
            tg.childForceExpandHeight = false;
            string[] kinds = { "draw", "discard", "exhaust" };
            string[] labels = { "山札 " + st.Player.DrawPile.Count, "捨て札 " + st.Player.DiscardPile.Count, "消滅 " + st.Player.ExhaustPile.Count };
            for (int i = 0; i < kinds.Length; i++)
            {
                string k = kinds[i];
                var b = UiKit.Btn(tabs, labels[i], delegate { g.ViewPile = k; g.Rebuild(); }, 18, true, g.ViewPile == k ? UiKit.Hex("#f6dd98") : Color.white);
                SetSize(b, 200f, 44f);
            }
            IReadOnlyList<CardInstance> pile = g.ViewPile == "discard" ? st.Player.DiscardPile : g.ViewPile == "exhaust" ? st.Player.ExhaustPile : st.Player.DrawPile;
            var list = new List<CardInstance>(pile);
            if (g.ViewPile == "draw") list.Sort((a, b) => string.CompareOrdinal(a.Def.Name, b.Def.Name)); // 引き順は伏せたまま
            var content = UiKit.Scroll(inner, true, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.06f), 12, 12);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 300f, -1f, 300f, -1f, 1f);
            var vg = content.GetComponent<VerticalLayoutGroup>();
            if (vg != null) UnityEngine.Object.DestroyImmediate(vg);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CardView.W * 0.8f, CardView.H * 0.8f);
            grid.spacing = new Vector2(14f, 14f);
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.childAlignment = TextAnchor.UpperLeft;
            if (list.Count == 0)
            {
                var none = UiKit.Txt(inner, "（空）", 18, PaperFx.InkSoft, TextAnchor.MiddleCenter);
                UiKit.Le(none, -1f, 40f, -1f, 40f);
            }
            for (int i = 0; i < list.Count; i++)
            {
                var cell = UiKit.NewRect("cell", content);
                var cv = CardView.Build(cell, list[i], st, true, false, "pile-card");
                cv.localScale = Vector3.one * 0.8f;
            }
            CenteredButton(inner, "閉じる", delegate { g.ViewPile = null; g.Rebuild(); }, 18, 260f, 50f);
        }

        static void BuildEndTurn(GameRoot g, RectTransform root, GameState st)
        {
            bool myTurn = st.Phase == CombatPhases.PlayerTurn && g.Pending == null;
            var b = UiKit.Btn(root, "ターン終了", delegate { g.DoCombat(new Command_EndTurn()); }, 21, myTurn, myTurn ? UiKit.Hex("#f6dd98") : Color.white);
            var le = b.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            var brt = b.GetComponent<RectTransform>();
            UiKit.Anchor(brt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-270f, 146f), new Vector2(-40f, 210f));
            brt.localRotation = Quaternion.Euler(0f, 0f, -1f);
            var bt = b.GetComponentInChildren<TMP_Text>();
            if (bt != null && UiKit.FontDeco != null) { bt.font = UiKit.FontDeco; bt.characterSpacing = 4f; }
            var hint = UiKit.Txt(root, "手札 " + st.Player.Hand.Count + " · 伏せ " + st.Player.SetCards.Count + "/" + st.Player.SetSlots, 11, PaperFx.Paper, TextAnchor.MiddleRight);
            hint.outlineWidth = 0.18f; hint.outlineColor = new Color(0f, 0f, 0f, 0.7f);
            UiKit.Anchor(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-270f, 216f), new Vector2(-40f, 236f));

            // エナジーの太陽 (紙の円盤に蜂蜜色の弧)
            var sun = UiKit.NewRect("energyOrb", root);
            UiKit.Anchor(sun, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(96f, 116f), new Vector2(224f, 244f));
            sun.localRotation = Quaternion.Euler(0f, 0f, -3f);
            var disc = UiKit.NewRect("disc", sun);
            UiKit.Stretch(disc, 6f, 6f, 6f, 6f);
            var dImg = disc.gameObject.AddComponent<Image>();
            dImg.sprite = PaperFx.Disc(); dImg.preserveAspect = true; dImg.raycastTarget = false;
            dImg.color = st.Player.Energy > 0 ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
            var arc = UiKit.NewRect("arc", sun);
            UiKit.Stretch(arc, 0f, 0f, 0f, 0f);
            var aImg = arc.gameObject.AddComponent<Image>();
            aImg.sprite = PaperFx.Ring(7); aImg.color = PaperFx.Honey; aImg.raycastTarget = false;
            aImg.type = Image.Type.Filled; aImg.fillMethod = Image.FillMethod.Radial360; aImg.fillOrigin = 2; aImg.fillClockwise = true;
            aImg.fillAmount = st.Player.EnergyMax > 0 ? Mathf.Clamp01((float)st.Player.Energy / st.Player.EnergyMax) : 0f;
            var et = UiKit.Deco(sun, st.Player.Energy.ToString(), 40, PaperFx.Ink, TextAnchor.MiddleCenter);
            UiKit.Anchor(et.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-8f, 4f), new Vector2(-8f, 6f));
            var em = UiKit.Txt(sun, "/ " + st.Player.EnergyMax, 15, PaperFx.InkSoft, TextAnchor.MiddleLeft, true);
            UiKit.Anchor(em.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(16f, -14f), new Vector2(60f, 8f));
            var el = UiKit.Txt(sun, "エナジー", 9, PaperFx.InkSoft, TextAnchor.MiddleCenter);
            el.characterSpacing = 3f;
            UiKit.Anchor(el.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 22f), new Vector2(0f, 38f));
            g.RegisterAnchor("energy", sun);
        }

        // ---- ログの引き出し ----

        static void BuildLogDrawer(GameRoot g, RectTransform root, GameState st)
        {
            var pan = UiKit.NewRect("logDrawer", root);
            UiKit.Anchor(pan, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-520f, 260f), new Vector2(-12f, -TopH - 8f));
            var sheet = PaperFx.Sheet(pan, PaperFx.Panel, "paper");
            UiKit.Stretch(sheet.rectTransform, 0f, 0f, 0f, 0f);
            var inner = UiKit.NewRect("inner", pan);
            UiKit.Stretch(inner, 16f, 16f, 14f, 14f);
            UiKit.Vert(inner, 4, 0);
            UiKit.Head(inner, "戦闘ログ", 20);
            var content = UiKit.Scroll(inner, true, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.06f), 2, 8);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 100f, -1f, 100f, -1f, 1f);
            var lines = new List<string>();
            for (int i = 0; i < st.EventLog.Count; i++)
            {
                var s = CardText.LogLine(st.EventLog[i]);
                if (s != null) lines.Add(s);
            }
            int from = Math.Max(0, lines.Count - 60);
            for (int i = from; i < lines.Count; i++)
            {
                var t = UiKit.Txt(content, lines[i], 14, PaperFx.Ink);
                UiKit.Le(t, -1f, 20f, -1f, -1f);
            }
        }

        // ---- モーダル: 確認ウィンドウ (set-confirm) ----

        public static RectTransform Modal(RectTransform root, float w, float h, string name)
        {
            var backdrop = UiKit.Pan(root, new Color(20f / 255f, 18f / 255f, 40f / 255f, 0.68f), name + "-backdrop");
            UiKit.Stretch(backdrop.rectTransform, 0f, 0f, 0f, 0f);
            var win = UiKit.Frame(backdrop.transform, Theme.Panel, Color.white, name, 3f);
            UiKit.Anchor(win.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-w / 2f, -h / 2f), new Vector2(w / 2f, h / 2f));
            win.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 0.4f);
            PaperFx.GrainOver(win.transform, 0.6f);
            var inner = UiKit.NewRect("inner", win.transform);
            UiKit.Stretch(inner, 28f, 28f, 22f, 22f);
            UiKit.Vert(inner, 10, 0);
            return inner;
        }

        static void BuildReactionWindow(GameRoot g, RectTransform root, GameState st)
        {
            var inner = Modal(root, 1000f, 620f, "reaction");
            UiKit.Head(inner, "リアクション — 発動する？ 温存する？", 26);
            var win = Effects.WindowFromPending(st);
            var pending = st.PendingWindow;
            if (win == null || pending == null)
            {
                UiKit.Txt(inner, "窓の情報を復元できません", 16, UiKit.ColBad);
                UiKit.Btn(inner, "温存して続ける", delegate { g.DoCombat(new Command_ConfirmReaction { Fire = false }); }, 18);
                return;
            }
            int ei = pending.EnemyIndex;
            string ename = "?";
            try { if (ei >= 0 && ei < st.Enemies.Count) ename = Content.GetEnemyDef(st.Enemies[ei].EnemyId).Name; } catch (Exception) { }
            var info = UiKit.Txt(inner,
                (ei + 1) + ". " + ename + " の " + (win.Stage == "pre" ? "行動の前（実行前）" : "行動の後（解決後）") + "\n"
                + CardText.IntentText(st, ei) + "\n<b>実値: " + win.Actual + "</b>" + (win.Stage == "post" ? "   このHP損失: " + win.HpLoss : ""),
                18, PaperFx.Ink);
            UiKit.Le(info, -1f, 96f, -1f, 96f);
            var risks = Effects.SetBranchFlipRisks(st);
            if (risks.Count > 0)
            {
                var buf = new List<string>();
                for (int i = 0; i < risks.Count; i++) buf.Add((risks[i] + 1).ToString());
                var w = UiKit.Txt(inner, "⚠ 発動すると伏せ枠が空き、敵 " + string.Join("・", buf.ToArray()) + " が「伏せなし」の分岐に変わる", 15, UiKit.Hex("#8a5a1a"));
                UiKit.Le(w, -1f, 40f, -1f, 40f);
            }
            var usable = Effects.UsableSetCards(st, win);
            var row = UiKit.NewRect("cards", inner);
            UiKit.Le(row, -1f, 320f, -1f, 320f);
            var hg = UiKit.Horz(row, 24, 0);
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childForceExpandHeight = false;
            hg.childForceExpandWidth = false;
            for (int i = 0; i < usable.Count; i++)
            {
                var c = usable[i];
                string uid = c.Uid;
                var wrap = UiKit.NewRect("cand", row);
                var wle = UiKit.Le(wrap, 220f, 320f, 220f, 320f);
                var cv = CardView.Build(wrap, c, st, true, false, "cand-card");
                cv.anchoredPosition = new Vector2(0f, 26f);
                cv.localScale = Vector3.one * 0.86f;
                var fb = UiKit.Btn(wrap, "発動", delegate { g.DoCombat(new Command_ConfirmReaction { Fire = true, CardUid = uid }); }, 18, true, UiKit.Hex("#f6dd98"));
                var fle = fb.GetComponent<LayoutElement>();
                if (fle != null) UnityEngine.Object.Destroy(fle);
                UiKit.Anchor(fb.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-70f, 0f), new Vector2(70f, 44f));
            }
            if (usable.Count == 0)
            {
                var none = UiKit.Txt(row, "発動できる伏せ札はありません", 16, PaperFx.InkSoft, TextAnchor.MiddleCenter);
                UiKit.Le(none, 400f, 40f, 400f, 40f);
            }
            var un = Effects.UnaffordableSetCards(st, win);
            if (un.Count > 0)
            {
                var buf = new List<string>();
                for (int i = 0; i < un.Count; i++) buf.Add(un[i].Def.Name);
                var ut = UiKit.Txt(inner, "エナジー不足で発動できない: " + string.Join("、", buf.ToArray()), 14, PaperFx.InkSoft);
                UiKit.Le(ut, -1f, 24f, -1f, 24f);
            }
            CenteredButton(inner, "温存する", delegate { g.DoCombat(new Command_ConfirmReaction { Fire = false }); }, 20, 320f, 56f);
        }

        // ---- 対象選択・モード選択 ----

        static void BuildTargetBanner(GameRoot g, RectTransform root)
        {
            var pan = PaperFx.Sheet(root, PaperFx.Tag, "targetBanner", UiKit.Hex("#f6dd98"));
            UiKit.Anchor(pan.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-420f, -TopH - 66f), new Vector2(420f, -TopH - 14f));
            var t = UiKit.Txt(pan.transform, "「" + g.Pending.Card.Def.Name + "」の対象を選ぶ — 敵をクリック（またはカードを敵へドラッグ）", 18, PaperFx.Ink, TextAnchor.MiddleLeft, true);
            UiKit.Anchor(t.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(24f, 0f), new Vector2(-150f, 0f));
            var b = UiKit.Btn(pan.transform, "取り消し", delegate { g.CancelPending(); }, 16);
            var le = b.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-136f, -20f), new Vector2(-16f, 20f));
        }

        static void BuildModeChooser(GameRoot g, RectTransform root, GameState st)
        {
            CardInstance card = null;
            for (int i = 0; i < st.Player.Hand.Count; i++) if (st.Player.Hand[i].Uid == g.ModeChoiceUid) card = st.Player.Hand[i];
            if (card == null) { g.ModeChoiceUid = null; return; }
            var pan = UiKit.Frame(root, Theme.Panel, Color.white, "modeChooser", 3f);
            UiKit.Anchor(pan.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-360f, HandY + CardView.H * CardScale + 60f), new Vector2(360f, HandY + CardView.H * CardScale + 150f));
            var inner = UiKit.NewRect("inner", pan.transform);
            UiKit.Stretch(inner, 16f, 16f, 12f, 12f);
            var hg = UiKit.Horz(inner, 10, 0);
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childForceExpandWidth = true;
            var title = UiKit.Deco(inner, card.Def.Name, 18, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(title, 100f, -1f, 160f, -1f);
            int cost = card.Def.Cost;
            try { cost = Effects.EffectiveCost(st, card); } catch (Exception) { }
            bool playable = st.Phase == CombatPhases.PlayerTurn && Effects.IsPlayableFromHand(card) && cost <= st.Player.Energy && Effects.RetainerRequirementMet(st, card);
            if (card.Def.Modes != null)
            {
                for (int m = 0; m < card.Def.Modes.Count; m++)
                {
                    int mi = m;
                    string label = (m + 1) + ": " + CardText.Short(ModeText(card.Def.Modes[m]), 18);
                    UiKit.Btn(inner, label, delegate { g.ModeChoiceUid = null; PlayCard(g, card, mi); }, 16, playable);
                }
            }
            else
            {
                UiKit.Btn(inner, "プレイ", delegate { g.ModeChoiceUid = null; PlayCard(g, card, null); }, 16, playable);
            }
            if (SetBase.CanSetCard(st, card.Uid)) UiKit.Btn(inner, "伏せる", delegate { g.ModeChoiceUid = null; g.DoCombat(new Command_SetCard { CardUid = card.Uid }); }, 16);
            UiKit.Btn(inner, "やめる", delegate { g.ModeChoiceUid = null; g.Rebuild(); }, 16);
        }

        static string ModeText(CardMode m)
        {
            var parts = new List<string>();
            for (int i = 0; i < m.Effects.Count; i++) parts.Add(CardText.EffectLine(m.Effects[i], null));
            return string.Join(" / ", parts.ToArray());
        }

        // ---- 追加コスト・選択のピッカー (モーダル) ----

        public static IReadOnlyList<CardInstance> DeckChoosePool(GameState st, string kind) { return CombatScreen.DeckChoosePool(st, kind); }
        public static List<CardInstance> UpgradablePool(GameState st, CardInstance self) { return CombatScreen.UpgradablePool(st, self); }

        static void BuildPicker(GameRoot g, RectTransform root, GameState st, string need)
        {
            var p = g.Pending;
            IReadOnlyList<CardInstance> pool;
            List<string> selected;
            int want;
            string title;
            if (need == "discard") { pool = ExceptSelf(st.Player.Hand, p.Card.Uid); selected = p.Discard; want = p.DiscardNeed; title = "追加コスト: 手札を" + want + "枚捨てる"; }
            else if (need == "exhaust") { pool = ExceptSelf(st.Player.Hand, p.Card.Uid); selected = p.Exhaust; want = p.ExhaustNeed; title = "追加コスト: 手札を" + want + "枚消滅させる"; }
            else if (need == "retrieve") { pool = st.Player.ExhaustPile; selected = new List<string>(); want = 1; title = "消滅置き場から1枚選ぶ"; }
            else if (need == "deck") { pool = DeckChoosePool(st, p.DeckKind); selected = p.DeckSel; want = p.DeckNeed; title = (p.DeckKind == "searchDeck" ? "山札" : p.DeckKind == "retrieveFromDiscard" ? "捨て札" : "山札か捨て札") + "から" + want + "枚選ぶ"; }
            else if (need == "hand") { pool = UpgradablePool(st, p.Card); selected = p.HandSel; want = p.HandNeed; title = "手札から" + want + "枚を鍛える"; }
            else if (need == "permanent")
            {
                var retainers = new List<CardInstance>();
                for (int i = 0; i < st.Player.Permanents.Count; i++) { var q = st.Player.Permanents[i]; if (q.Def.Retainer == true && q.Innate != true) retainers.Add(q); }
                pool = retainers; selected = new List<string>(); want = 1; title = "破壊する従者を選ぶ";
            }
            else { UiKit.Txt(root, "未対応の選択: " + need, 20, UiKit.ColBad); return; }

            var inner = Modal(root, 1400f, 720f, "picker");
            UiKit.Head(inner, p.Card.Def.Name + " — " + title + "（選択中 " + selected.Count + " / " + want + "）", 24);
            var content = UiKit.Scroll(inner, false, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.06f), 16, 12);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 360f, -1f, 360f, -1f, 1f);
            var lg = content.GetComponent<HorizontalLayoutGroup>();
            if (lg != null) { lg.childForceExpandWidth = false; lg.childForceExpandHeight = false; lg.childAlignment = TextAnchor.MiddleLeft; }
            if (pool.Count == 0)
            {
                var none = UiKit.Txt(content, "（候補がありません）", 16, PaperFx.InkSoft);
                UiKit.Le(none, 300f, 40f, 300f, 40f);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                string uid = c.Uid;
                bool isSel = selected.Contains(uid);
                var wrap = UiKit.NewRect("cand", content);
                UiKit.Le(wrap, 220f, 330f, 220f, 330f);
                var cv = CardView.Build(wrap, c, st, !isSel, false, "cand-card");
                cv.anchoredPosition = new Vector2(0f, 30f);
                cv.localScale = Vector3.one * 0.86f;
                string cap = need;
                var pick = UiKit.Btn(wrap, isSel ? "選択中" : "選ぶ", delegate { OnPick(g, cap, uid); }, 16, true, isSel ? UiKit.Hex("#f6dd98") : Color.white);
                var ple = pick.GetComponent<LayoutElement>();
                if (ple != null) UnityEngine.Object.Destroy(ple);
                UiKit.Anchor(pick.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-70f, 0f), new Vector2(70f, 44f));
            }
            CenteredButton(inner, "取り消し", delegate { g.CancelPending(); }, 18, 260f, 50f);
        }

        static void OnPick(GameRoot g, string need, string uid)
        {
            var p = g.Pending;
            if (p == null) return;
            if (need == "retrieve") { p.RetrieveUid = uid; g.SubmitIfReady(); return; }
            if (need == "permanent") { p.PermanentUid = uid; g.SubmitIfReady(); return; }
            List<string> list = need == "discard" ? p.Discard : need == "exhaust" ? p.Exhaust : need == "deck" ? p.DeckSel : p.HandSel;
            if (list.Contains(uid)) list.Remove(uid); else list.Add(uid);
            g.SubmitIfReady();
        }

        static IReadOnlyList<CardInstance> ExceptSelf(IReadOnlyList<CardInstance> hand, string selfUid)
        {
            var list = new List<CardInstance>();
            for (int i = 0; i < hand.Count; i++) if (hand[i].Uid != selfUid) list.Add(hand[i]);
            return list;
        }
    }
}
