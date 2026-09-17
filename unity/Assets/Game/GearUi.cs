// GearUi.cs — ギア (消耗品 2026-09-17) の画面部品。置き場は提案書 §6-2 の裁定＝案A「匣の帯」(からくりの隣にトークン)。
// 文言はブラウザ版 (App.tsx GearBar) と CLI (play.ts renderGearBar) に揃える。語彙は Unity のからくり語彙のまま (「組む」)。
// 部品: トークン (62×62 / 64×66)・使う時の紙の窓・札を選ぶ窓・対象の帯・報酬/店の札・満杯の入れ替え。魔素 (通貨) は 2026-09-18 に撤去。
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    /// <summary>ギアを組む途中の状態 (GameRoot が持つ。Do() で捨てる)</summary>
    public class PendingGear
    {
        public int Index;
        public int? TargetIndex;
        public string CardUid;
        /// <summary>無銘の部品: 化ける先のギアID</summary>
        public string AsGearId;
        /// <summary>"window" (窓を開いている) / "card" (札を選んでいる) / "target" (敵を選んでいる)</summary>
        public string Stage = "window";
    }

    public static class GearUi
    {
        public const float TokenW = 62f, TokenH = 62f;            // PC: 名前は添えない (ツールチップ)
        public const float PhoneTokenW = 64f, PhoneTokenH = 66f;  // スマホ: 下に名前の帯

        public static GearDef DefOf(string id) { try { return Content.GetGearDef(id); } catch (Exception) { return null; } }
        public static string RarityJa(string r) { return r == "rare" ? "★レア" : r == "uncommon" ? "◆アンコモン" : "コモン"; }
        static string CardSourceJa(string needsCard) { return needsCard == "hand" ? "手札" : needsCard == "discard" ? "捨て札" : "山札"; }

        /// <summary>実際に組む定義 (無銘の部品は化ける先の中身。まだ選んでいなければ null)</summary>
        public static GearDef EffectiveDef(GearDef def, PendingGear p)
        {
            if (def == null) return null;
            if (def.Special != "nameless") return def;
            return p != null && p.AsGearId != null ? DefOf(p.AsGearId) : null;
        }

        static List<int> Alive(GameState st)
        {
            var alive = new List<int>();
            if (st == null) return alive;
            for (int i = 0; i < st.Enemies.Count; i++) if (st.Enemies[i].Hp > 0) alive.Add(i);
            return alive;
        }

        /// <summary>左クリック (タップ) だけを拾う。長押しの拡大 (CardPopup) の直後の離しは無視</summary>
        static void OnClick(GameObject go, Action a)
        {
            var trig = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
            var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            click.callback.AddListener(delegate (BaseEventData d)
            {
                var pd = d as PointerEventData;
                if (pd != null && pd.button != PointerEventData.InputButton.Left) return;
                if (CardPopup.ClickSuppressed || CardPopup.IsOpen) return;
                a();
            });
            trig.triggers.Add(click);
        }

        // ---- トークン ----

        /// <summary>
        /// 持ち物のトークン: 紙 (濃) ＋レア度の外線 (C 墨／U 空／R 蜂蜜＝札と同じ)＋歯車の絵＋(スマホは名前)＋回数つきは右上に残り回数。
        /// 組めない時は絵を灰色に (理由はツールチップ)。窓を開いている札は真鍮の縁。演出の的 "gear:&lt;uid&gt;"
        /// </summary>
        public static RectTransform Token(GameRoot g, Transform parent, RunState run, GameState st, int index, GearInstance gear, float w, float h, bool withName, bool clickable)
        {
            var def = DefOf(gear.GearId);
            var slot = UiKit.NewRect("gear-" + gear.Uid, parent);
            slot.sizeDelta = new Vector2(w, h);
            g.RegisterAnchor("gear:" + gear.Uid, slot);
            string blocked = def != null ? Gears.GearBlockedReason(st, gear) : "未定義のギア";
            bool open = g.GearPending != null && g.GearPending.Index == index;
            Color edgeCol = open ? PaperFx.Brass : PaperFx.RarityEdge(def != null ? def.Rarity : "common");
            var edge = PaperFx.Sheet(slot, PaperFx.Tag, "edge", edgeCol);
            UiKit.Stretch(edge.rectTransform, -3f, -3f, -3f, -3f);
            edge.raycastTarget = false;
            var paper = PaperFx.Sheet(slot, PaperFx.Tag2, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = true;
            float bandH = withName ? 22f : 0f;
            var pic = UiKit.NewRect("pic", slot);
            float picSize = Mathf.Min(w - 14f, h - bandH - 10f);
            UiKit.Anchor(pic, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-picSize / 2f, -5f - picSize), new Vector2(picSize / 2f, -5f));
            var pimg = pic.gameObject.AddComponent<Image>();
            pimg.sprite = ThemeFx.GearGlyph(gear.GearId, def != null ? def.Family : "general"); pimg.preserveAspect = true; pimg.raycastTarget = false;
            if (blocked != null && st != null) pimg.color = new Color(0.72f, 0.72f, 0.72f, 1f);
            if (withName)
            {
                var band = UiKit.Pan(slot, PaperFx.Ink, "band");
                UiKit.Anchor(band.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1f, bandH + 1f));
                band.raycastTarget = false;
                var nt = UiKit.Deco(slot, def != null ? def.Name : gear.GearId, 12, PaperFx.Paper, TextAnchor.MiddleCenter);
                UiKit.Anchor(nt.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1f, bandH + 1f));
                nt.textWrappingMode = TextWrappingModes.NoWrap; nt.overflowMode = TextOverflowModes.Ellipsis;
            }
            if (gear.Charges > 1 || (def != null && (def.Charges ?? 1) > 1))
            {   // 角の数字 = 残り回数 (回数つき＝発条・歯車)
                var badge = UiKit.NewRect("badge", slot);
                UiKit.Anchor(badge, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-13f, -13f), new Vector2(9f, 9f));
                var bImg = badge.gameObject.AddComponent<Image>();
                bImg.sprite = PaperFx.Disc(); bImg.preserveAspect = true; bImg.raycastTarget = false;
                var bRing = UiKit.NewRect("ring", badge);
                UiKit.Stretch(bRing, -1.5f, -1.5f, -1.5f, -1.5f);
                var rImg = bRing.gameObject.AddComponent<Image>();
                rImg.sprite = PaperFx.Ring(4); rImg.color = PaperFx.Ink; rImg.raycastTarget = false; rImg.preserveAspect = true;
                var btx = UiKit.Deco(badge, gear.Charges.ToString(), 13, PaperFx.Ink, TextAnchor.MiddleCenter);
                UiKit.Stretch(btx.rectTransform, 0f, 0f, 0f, 0f);
            }
            string tip = Tip(def, gear, st, blocked);
            Tooltip.Attach(paper.gameObject, delegate { return tip; });
            if (clickable)
            {
                int idx = index;
                OnClick(paper.gameObject, delegate
                {
                    Audio.Ui("click");
                    if (g.GearPending != null && g.GearPending.Index == idx) g.GearPending = null;   // 同じ札をもう一度押すと閉じる
                    else g.GearPending = new PendingGear { Index = idx };
                    g.Rebuild();
                });
            }
            return slot;
        }

        static string Tip(GearDef def, GearInstance gear, GameState st, string blocked)
        {
            if (def == null) return gear.GearId;
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>").Append(def.Name).Append("</b>  ").Append(RarityJa(def.Rarity)).Append("・残り").Append(gear.Charges).Append("回\n").Append(def.Text);
            if (st != null)
            {
                string live = null, none = null;
                try { live = Gears.GearLiveDamage(st, def); none = Gears.GearNoEffectReason(st, def); } catch (Exception) { }
                if (live != null) sb.Append("\n<color=#7a4e12>").Append(live).Append("</color>");
                if (none != null) sb.Append("\n<color=#7a4e12>⚠ いま組んでも何も起きない: ").Append(none).Append("</color>");
                if (blocked != null) sb.Append("\n<color=#9c3a2a>（いまは組めない: ").Append(blocked).Append("）</color>");
            }
            return sb.ToString();
        }

        /// <summary>「+N …」(溢れたトークン。タップで名前と本文の一覧)</summary>
        public static RectTransform MoreChip(Transform parent, IReadOnlyList<GearInstance> rest, float w, float h)
        {
            var more = UiKit.NewRect("gear-more", parent);
            more.sizeDelta = new Vector2(w, h);
            var mImg = PaperFx.Sheet(more, PaperFx.Tag2, "paper");
            UiKit.Stretch(mImg.rectTransform, 0f, 0f, 0f, 0f);
            mImg.raycastTarget = true;
            var mt = UiKit.Txt(more, "+" + rest.Count, 16, PaperFx.Ink, TextAnchor.MiddleCenter, true);
            UiKit.Stretch(mt.rectTransform, 2f, 2f, 0f, 0f);
            var sb = new System.Text.StringBuilder();
            foreach (var gi in rest) { var d = DefOf(gi.GearId); if (sb.Length > 0) sb.Append("\n"); sb.Append("<b>").Append(d != null ? d.Name : gi.GearId).Append("</b>（残").Append(gi.Charges).Append("回） ").Append(d != null ? d.Text : ""); }
            string tip = sb.ToString();
            Tooltip.Attach(more.gameObject, delegate { return tip; });
            return more;
        }

        // ---- 使う時の窓 (戦闘) ----

        /// <summary>GameRoot.GearPending の段に合わせて組む: 窓／札を選ぶ窓／対象の帯</summary>
        public static void BuildPending(GameRoot g, RectTransform root, RunState run, GameState st)
        {
            var p = g.GearPending;
            if (p == null) return;
            var gears = DeckRogue.Engine.Run.GearsOf(run);
            if (p.Index < 0 || p.Index >= gears.Count) { g.GearPending = null; return; }
            var def = DefOf(gears[p.Index].GearId);
            if (def == null) { g.GearPending = null; return; }
            if (p.Stage == "target") { BuildTargetBanner(g, root, EffectiveDef(def, p) ?? def); return; }
            if (p.Stage == "card") { BuildCardPicker(g, root, st, EffectiveDef(def, p) ?? def); return; }
            BuildWindow(g, root, run, st, gears[p.Index], def);
        }

        static void BuildWindow(GameRoot g, RectTransform root, RunState run, GameState st, GearInstance gear, GearDef def)
        {
            var p = g.GearPending;
            bool ph = UiKit.Phone;
            var cs = BattleScreen.CanvasSize(root);
            string blocked = Gears.GearBlockedReason(st, gear);
            var eff = EffectiveDef(def, p);
            var alive = Alive(st);
            bool isBossNode = false;
            try { var node = DeckRogue.Engine.Run.CurrentNode(run); isBossNode = node != null && node.Type == MapNodeTypes.Boss; } catch (Exception) { }
            if (def.Special == "flee" && isBossNode && blocked == null) blocked = "幕ボスからは逃げられない";
            string live = null, none = null;
            try { live = Gears.GearLiveDamage(st, eff ?? def, p.TargetIndex); none = Gears.GearNoEffectReason(st, eff ?? def, p.TargetIndex); } catch (Exception) { }
            var seen = (run.SeenGearIds ?? new List<string>()).Where(id => id != "gear_nameless").ToList();
            bool needTarget = eff != null && eff.NeedsTarget == true && alive.Count > 1;
            IReadOnlyList<CardInstance> cardChoices = eff != null && eff.NeedsCard != null ? Gears.GearCardChoices(st, eff) : new List<CardInstance>();
            bool ready = eff != null && (def.Special != "nameless" || p.AsGearId != null);

            // 大きさ: 行数で伸びる。PC は自分の札の上 (x=40)、スマホは自分の札の右・手札の上
            float W = ph ? Mathf.Min(420f, cs.x - 272f) : 560f;
            int charsPerLine = Mathf.Max(8, (int)((W - 40f) / 16f));
            int textLines = Mathf.Max(1, Mathf.CeilToInt(def.Text.Length / (float)charsPerLine));
            float H = 28f + 30f + 6f + textLines * 22f;
            if (live != null) H += 22f;
            if (none != null) H += 24f;
            if (def.Special == "nameless") H += 26f + (seen.Count == 0 ? 24f : Mathf.CeilToInt(seen.Count / (float)Mathf.Max(1, (int)((W - 40f) / 132f))) * 44f);
            if (eff != null && eff.NeedsCard != null) H += 30f;
            if (needTarget) H += 30f;
            H += 8f + 22f + 6f + 48f + 40f;   // 縦の並びの間 (6×行数) と紙の余白
            float maxH = cs.y - RunUi.TopH - 24f - (ph ? 312f : 448f);
            if (H > maxH) H = maxH;
            // PC はギアのトークンの真上 (自分の札の C 区画の左端)。スマホは自分の札の右・手札の上
            float x = ph ? 260f : 40f + 300f + st.Player.SetSlots * (BattleScreen.PhoneTokenW + 10f) + 24f;
            if (x + W > cs.x - 12f) x = Mathf.Max(12f, cs.x - 12f - W);
            float y0 = ph ? BattleScreen.HandY + CardView.H * BattleScreen.CardScale + 8f : BattleView.StatusLineY + BattleScreen.StripH + 8f;
            var panel = PaperFx.Sheet(root, PaperFx.Panel, "gear-window");
            UiKit.Anchor(panel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, y0), new Vector2(x + W, y0 + H));
            panel.raycastTarget = true;
            PaperFx.GrainOver(panel.transform, 0.6f);
            var inner = UiKit.NewRect("inner", panel.transform);
            UiKit.Stretch(inner, 20f, 20f, 14f, 14f);
            inner.gameObject.AddComponent<RectMask2D>();
            var vg = UiKit.Vert(inner, 6, 0);
            vg.childForceExpandHeight = false;

            // 見出し: 名前＋レア度＋残り回数
            var head = UiKit.NewRect("head", inner);
            UiKit.Le(head, -1f, 30f, -1f, 30f);
            var hg = UiKit.Horz(head, 10, 0);
            hg.childAlignment = TextAnchor.MiddleLeft; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            var ic = new GameObject("ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            ic.transform.SetParent(head, false);
            ic.sprite = ThemeFx.GearGlyph(def.Id, def.Family); ic.preserveAspect = true; ic.raycastTarget = false;
            UiKit.Le(ic.rectTransform, 26f, 26f, 26f, 26f);
            var name = UiKit.Deco(head, def.Name, ph ? 17 : 19, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(name, -1f, 30f, -1f, 30f);
            var sub = UiKit.Txt(head, RarityJa(def.Rarity) + " ・ 残り" + gear.Charges + "回", 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(sub, -1f, 30f, -1f, 30f);
            // 本文
            var body = UiKit.Txt(inner, def.Text, 15, PaperFx.Ink, TextAnchor.UpperLeft);
            body.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Le(body, -1f, textLines * 22f, -1f, textLines * 22f);
            // 実際に与える値 (J2) と空振りの予告 (O)。弾きはせず画面に出すだけ
            if (live != null)
            {
                var lt = UiKit.Txt(inner, live, 13, PaperFx.BrassInk, TextAnchor.MiddleLeft);
                lt.textWrappingMode = TextWrappingModes.NoWrap; lt.overflowMode = TextOverflowModes.Ellipsis;
                UiKit.Le(lt, -1f, 22f, -1f, 22f);
            }
            if (none != null)
            {
                var nt = UiKit.Txt(inner, "⚠ いま組んでも何も起きない: " + none, 14, PaperFx.BrassInk, TextAnchor.MiddleLeft, true);
                nt.textWrappingMode = TextWrappingModes.NoWrap; nt.overflowMode = TextOverflowModes.Ellipsis;
                UiKit.Le(nt, -1f, 24f, -1f, 24f);
            }
            // 無銘の部品: 化ける先 (このランで拾ったことのあるギア)
            if (def.Special == "nameless")
            {
                var lbl = UiKit.Txt(inner, seen.Count == 0 ? "化ける先がない（まだ他のギアを拾っていない）" : "化ける先を選ぶ:", 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
                UiKit.Le(lbl, -1f, 24f, -1f, 24f);
                if (seen.Count > 0)
                {
                    int perRow = Mathf.Max(1, (int)((W - 40f) / 132f));
                    int rows = Mathf.CeilToInt(seen.Count / (float)perRow);
                    for (int r = 0; r < rows; r++)
                    {
                        var row = UiKit.NewRect("row" + r, inner);
                        UiKit.Le(row, -1f, 40f, -1f, 40f);
                        var rg = UiKit.Horz(row, 6, 0);
                        rg.childAlignment = TextAnchor.MiddleLeft; rg.childForceExpandWidth = false; rg.childForceExpandHeight = false;
                        for (int i = r * perRow; i < seen.Count && i < (r + 1) * perRow; i++)
                        {
                            string id = seen[i];
                            var sd = DefOf(id);
                            bool sel = p.AsGearId == id;
                            var b = UiKit.Btn(row, sd != null ? sd.Name : id, delegate { Audio.Ui("click"); p.AsGearId = id; p.TargetIndex = null; p.CardUid = null; g.Rebuild(); }, 14, true, sel ? PaperFx.BrassLight : (Color?)null);
                            BattleScreen.SetSize(b, 126f, 38f);
                            if (sd != null) { string stip = "<b>" + sd.Name + "</b>\n" + sd.Text; Tooltip.Attach(b.gameObject, delegate { return stip; }); }
                        }
                    }
                }
            }
            // 札を選ぶギア: 選んだ札の名前か「組む」を押した後に選ぶ旨
            if (eff != null && eff.NeedsCard != null)
            {
                string chosen = null;
                if (p.CardUid != null) foreach (var c in cardChoices) if (c.Uid == p.CardUid) chosen = c.Def.Name;
                string line = CardSourceJa(eff.NeedsCard) + "から: " + (cardChoices.Count == 0 ? "候補なし（そのまま組める）" : chosen != null ? "<b>" + chosen + "</b>" : "組む時に選ぶ（" + cardChoices.Count + "枚）");
                var ct = UiKit.Txt(inner, line, 14, PaperFx.Ink, TextAnchor.MiddleLeft);
                ct.textWrappingMode = TextWrappingModes.NoWrap; ct.overflowMode = TextOverflowModes.Ellipsis;
                UiKit.Le(ct, -1f, 26f, -1f, 26f);
            }
            if (needTarget)
            {
                string tname = null;
                if (p.TargetIndex.HasValue && p.TargetIndex.Value >= 0 && p.TargetIndex.Value < st.Enemies.Count) { try { tname = Content.GetEnemyDef(st.Enemies[p.TargetIndex.Value].EnemyId).Name; } catch (Exception) { tname = "敵" + p.TargetIndex.Value; } }
                else if (g.PreferredTarget >= 0 && alive.Contains(g.PreferredTarget)) { try { tname = Content.GetEnemyDef(st.Enemies[g.PreferredTarget].EnemyId).Name; } catch (Exception) { tname = "敵" + g.PreferredTarget; } }
                var tt = UiKit.Txt(inner, "対象: " + (tname != null ? "<b>" + tname + "</b>" : "組む時に敵を" + (ph ? "タップ" : "クリック") + "して選ぶ"), 14, PaperFx.Ink, TextAnchor.MiddleLeft);
                tt.textWrappingMode = TextWrappingModes.NoWrap; tt.overflowMode = TextOverflowModes.Ellipsis;
                UiKit.Le(tt, -1f, 26f, -1f, 26f);
            }
            // 脚: 1ターン1個の残りと「組む／やめる」(魔素の収支は 2026-09-18 撤去)
            var sp = UiKit.NewRect("sp", inner); UiKit.Le(sp, -1f, 2f, -1f, 2f, -1f, 1f);
            string note = st != null && st.GearUsedThisTurn != true && st.Phase == CombatPhases.PlayerTurn ? "このターンはあと1個組める（1ターン1個）" : "自ターンに1個";
            if (blocked != null) note = "<color=#9c3a2a>" + blocked + "</color>　" + note;
            var ft = UiKit.Txt(inner, note, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            ft.textWrappingMode = TextWrappingModes.NoWrap; ft.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Le(ft, -1f, 22f, -1f, 22f);
            var btns = UiKit.NewRect("btns", inner);
            UiKit.Le(btns, -1f, 48f, -1f, 48f);
            var bg = UiKit.Horz(btns, 10, 0);
            bg.childAlignment = TextAnchor.MiddleRight; bg.childForceExpandWidth = false; bg.childForceExpandHeight = false;
            var cancel = UiKit.Btn(btns, "やめる", delegate { g.GearPending = null; g.Rebuild(); }, 16);
            BattleScreen.SetSize(cancel, 120f, 46f);
            bool canGo = blocked == null && ready;
            var go = UiKit.Btn(btns, "組む", delegate
            {
                if (def.Special == "flee")
                {   // 煙玉 (裁定3): 確認を挟む。放棄と同じ朱のボタン
                    int idx = p.Index;
                    g.Confirm = new ConfirmBox
                    {
                        Title = "この戦闘から逃げる？",
                        Message = "報酬は得られない。HPはそのまま、この節は踏んだことになる。",
                        OkLabel = "逃げる",
                        Danger = true,
                        OnOk = delegate { g.GearPending = null; Audio.Ui("click"); g.Do(new RunCommand_UseGear { Index = idx }); },
                    };
                    g.Rebuild();
                    return;
                }
                Submit(g);
            }, 16, canGo, PaperFx.BrassLight);
            BattleScreen.SetSize(go, 170f, 46f);
            if (!canGo) { string why = blocked ?? (def.Special == "nameless" ? "化ける先を選ぶ" : "対象か札を選ぶ"); Tooltip.Attach(go.gameObject, delegate { return why; }); }
        }

        /// <summary>組む: 無銘→札→対象の順で足りない入力を集め、揃ったら UseGear (ラン層のコマンド) を送る</summary>
        public static void Submit(GameRoot g)
        {
            var p = g.GearPending;
            var run = g.Rs;
            var st = run != null ? run.Combat : null;
            if (p == null) return;
            if (st == null) { g.GearPending = null; g.Rebuild(); return; }
            var gears = DeckRogue.Engine.Run.GearsOf(run);
            if (p.Index < 0 || p.Index >= gears.Count) { g.GearPending = null; g.Rebuild(); return; }
            var def = DefOf(gears[p.Index].GearId);
            var eff = EffectiveDef(def, p);
            if (def == null || eff == null) { p.Stage = "window"; g.Rebuild(); return; }
            if (eff.NeedsCard != null && p.CardUid == null)
            {
                var choices = Gears.GearCardChoices(st, eff);
                if (choices.Count > 0) { p.Stage = "card"; g.Rebuild(); return; }   // 候補が無ければ選ばずに組む (engine は山が空なら無変化)
            }
            if (eff.NeedsTarget == true && p.TargetIndex == null)
            {
                var alive = Alive(st);
                if (alive.Count == 1) p.TargetIndex = alive[0];
                else if (g.PreferredTarget >= 0 && alive.Contains(g.PreferredTarget)) p.TargetIndex = g.PreferredTarget;   // 先に敵を押して狙いを付けてあればそのまま
                else { p.Stage = "target"; g.Rebuild(); return; }
            }
            var cmd = new RunCommand_UseGear { Index = p.Index, TargetIndex = p.TargetIndex, CardUid = p.CardUid, AsGearId = p.AsGearId };
            g.GearPending = null;
            Audio.Ui("click");
            g.Do(cmd);
        }

        /// <summary>対象を選ぶ帯 (カードの対象選びと同じ形)</summary>
        static void BuildTargetBanner(GameRoot g, RectTransform root, GearDef eff)
        {
            var pan = PaperFx.Sheet(root, PaperFx.Tag, "gearTargetBanner", PaperFx.BrassLight);
            UiKit.Anchor(pan.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-420f, -RunUi.TopH - 66f), new Vector2(420f, -RunUi.TopH - 14f));
            var t = UiKit.Txt(pan.transform, "「" + eff.Name + "」の対象を選ぶ — 敵を" + (UiKit.Phone ? "タップ" : "クリック"), 18, PaperFx.Ink, TextAnchor.MiddleLeft, true);
            UiKit.Anchor(t.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(24f, 0f), new Vector2(-150f, 0f));
            var b = UiKit.Btn(pan.transform, "取り消し", delegate { g.GearPending = null; g.Rebuild(); }, 16);
            var le = b.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-136f, -20f), new Vector2(-16f, 20f));
        }

        /// <summary>札を選ぶ窓 (掘り出し・目当ての品・砥ぎ油・写し・化けの粉): 手札／捨て札／山札 (名前順) から1枚</summary>
        static void BuildCardPicker(GameRoot g, RectTransform root, GameState st, GearDef eff)
        {
            var p = g.GearPending;
            var pool = Gears.GearCardChoices(st, eff);
            var inner = BattleScreen.Modal(root, 1400f, 720f, "gear-picker");
            UiKit.Head(inner, eff.Name + " — " + CardSourceJa(eff.NeedsCard) + "から1枚選ぶ" + (eff.NeedsCard == "draw" ? "（山札の並びは分からないので名前順）" : ""), 22);
            var content = UiKit.Scroll(inner, false, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.06f), 16, 12);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 360f, -1f, 360f, -1f, 1f);
            var lg = content.GetComponent<HorizontalLayoutGroup>();
            if (lg != null) { lg.childForceExpandWidth = false; lg.childForceExpandHeight = false; lg.childAlignment = TextAnchor.MiddleLeft; }
            if (pool.Count == 0)
            {
                var noneT = UiKit.Txt(content, "（候補がありません）", 16, PaperFx.InkSoft);
                UiKit.Le(noneT, 300f, 40f, 300f, 40f);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                string uid = c.Uid;
                var wrap = UiKit.NewRect("cand", content);
                UiKit.Le(wrap, 220f, 330f, 220f, 330f);
                var cv = CardView.Build(wrap, c, st, true, true, "cand-card");
                CardPopup.Attach(g, cv, c, delegate { return g.Rs != null ? g.Rs.Combat : null; }, true);
                cv.anchoredPosition = new Vector2(0f, 30f);
                cv.localScale = Vector3.one * 0.86f;
                var pick = UiKit.Btn(wrap, "選ぶ", delegate { p.CardUid = uid; p.Stage = "window"; Submit(g); }, 16, true, Color.white);
                var ple = pick.GetComponent<LayoutElement>();
                if (ple != null) UnityEngine.Object.Destroy(ple);
                UiKit.Anchor(pick.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-70f, 0f), new Vector2(70f, 44f));
            }
            var rows = UiKit.NewRect("btns", inner);
            UiKit.Le(rows, -1f, 50f, -1f, 50f);
            var hg = UiKit.Horz(rows, 10, 0);
            hg.childAlignment = TextAnchor.MiddleCenter; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            var cancel = UiKit.Btn(rows, "取り消し", delegate { g.GearPending = null; g.Rebuild(); }, 18);
            BattleScreen.SetSize(cancel, 260f, 50f);
        }

        // ---- 報酬・店の札 (200×272) ----

        /// <summary>ギア1個の札: レア度の外線・歯車の絵・名前・レア度と回数・本文・注記「自ターンに組む（1ターン1個）」</summary>
        public static RectTransform Card(Transform parent, GearDef def, string id, float w, float h, int? charges = null, float scale = 1f, string sub = null, string foot = null, Color? edgeColor = null)
        {
            var cell = UiKit.NewRect("gearcard-" + id, parent);
            cell.sizeDelta = new Vector2(w, h);
            var edge = PaperFx.Sheet(cell, PaperFx.Panel, "edge", edgeColor ?? PaperFx.RarityEdge(def != null ? def.Rarity : "common"));
            UiKit.Stretch(edge.rectTransform, -4f, -4f, -4f, -4f);
            edge.raycastTarget = false;
            var paper = PaperFx.Sheet(cell, PaperFx.Panel, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            PaperFx.GrainOver(paper.transform, 0.5f);
            var pic = UiKit.NewRect("pic", cell);
            UiKit.Anchor(pic, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-32f, -82f), new Vector2(32f, -18f));
            var pimg = pic.gameObject.AddComponent<Image>();
            pimg.sprite = ThemeFx.GearGlyph(id, def != null ? def.Family : "general"); pimg.preserveAspect = true; pimg.raycastTarget = false;
            var name = UiKit.Deco(cell, def != null ? def.Name : id, 20, PaperFx.Ink, TextAnchor.MiddleCenter);
            UiKit.Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -116f), new Vector2(-8f, -86f));
            name.textWrappingMode = TextWrappingModes.NoWrap; name.overflowMode = TextOverflowModes.Ellipsis;
            int ch = charges ?? (def != null ? def.Charges ?? 1 : 1);
            var rt = UiKit.Txt(cell, sub ?? ((def != null ? RarityJa(def.Rarity) : "") + (ch > 1 ? " ・ " + ch + "回" : " ・ 1回")), 13, edgeColor.HasValue ? PaperFx.ManaInk : PaperFx.BrassInk, TextAnchor.MiddleCenter);
            UiKit.Anchor(rt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -136f), new Vector2(-8f, -116f));
            var desc = UiKit.Txt(cell, def != null ? def.Text : "", 14, PaperFx.Ink, TextAnchor.UpperCenter);
            desc.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Anchor(desc.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 78f), new Vector2(-14f, -142f));
            var footT = UiKit.Txt(cell, foot ?? "自ターンに組む\n（1ターン1個）", 12, PaperFx.InkSoft, TextAnchor.MiddleCenter);
            UiKit.Anchor(footT.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(6f, 40f), new Vector2(-6f, 76f));
            footT.textWrappingMode = TextWrappingModes.Normal;
            if (def != null) { string tip = "<b>" + def.Name + "</b>  " + RarityJa(def.Rarity) + "\n" + def.Text; Tooltip.Attach(paper.gameObject, delegate { return tip; }); }
            if (scale != 1f) cell.localScale = Vector3.one * scale;
            return cell;
        }

        // ---- 満杯の入れ替え (報酬・店) ----

        /// <summary>持ち物が満杯 = 入れ替えるギアを選ぶ窓。onPick(添字)。やめれば閉じるだけ (見送りにはしない)</summary>
        public static void BuildSwapPicker(GameRoot g, RectTransform root, RunState run, string incomingId, Action<int> onPick)
        {
            var gears = DeckRogue.Engine.Run.GearsOf(run);
            var inDef = DefOf(incomingId);
            var inner = BattleScreen.Modal(root, 1000f, 380f, "gear-swap");
            UiKit.Head(inner, "持ち物が満杯（" + Gears.GEAR_CARRY_MAX + "）＝ 入れ替えるギアを選ぶ", 22);
            var sub = UiKit.Txt(inner, "<b>" + (inDef != null ? inDef.Name : incomingId) + "</b> を取る代わりに、選んだギアを捨てる", 15, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(sub, -1f, 26f, -1f, 26f);
            var row = UiKit.NewRect("tokens", inner);
            UiKit.Le(row, -1f, 140f, -1f, 140f);
            var hg = UiKit.Horz(row, 10, 0);
            hg.childAlignment = TextAnchor.MiddleLeft; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            for (int i = 0; i < gears.Count; i++)
            {
                int idx = i;
                var cell = UiKit.NewRect("swap" + i, row);
                UiKit.Le(cell, 84f, 130f, 84f, 130f);
                var tok = Token(g, cell, run, null, -1, gears[i], 64f, 66f, true, false);
                UiKit.Anchor(tok, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-32f, -66f), new Vector2(32f, 0f));
                var b = UiKit.Btn(cell, "これと", delegate { Audio.Ui("click"); onPick(idx); }, 14, true, PaperFx.BrassLight);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) UnityEngine.Object.Destroy(le);
                UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-40f, 4f), new Vector2(40f, 48f));
            }
            var rows = UiKit.NewRect("btns", inner);
            UiKit.Le(rows, -1f, 50f, -1f, 50f);
            var bg = UiKit.Horz(rows, 10, 0);
            bg.childAlignment = TextAnchor.MiddleCenter; bg.childForceExpandWidth = false; bg.childForceExpandHeight = false;
            var cancel = UiKit.Btn(rows, "やめる", delegate { g.GearSwap = null; g.Rebuild(); }, 18);
            BattleScreen.SetSize(cancel, 220f, 50f);
        }
    }
}
