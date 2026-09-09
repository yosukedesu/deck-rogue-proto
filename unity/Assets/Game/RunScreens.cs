// RunScreens.cs — 戦闘以外の画面 (セットアップ / マップ / 報酬 / レリック / 焚き火 / 工房 / ショップ / イベント / 決着)。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class RunScreens
    {
        const float SideWidth = 330f;

        // ================= セットアップ =================

        public static void Setup(GameRoot g, RectTransform body)
        {
            UiKit.Vert(body, 8, 0);
            UiKit.Head(body, "デッキ構築ローグライク — Unity 最小プレイ画面", 22);
            var sub = UiKit.Txt(body, "リーダーを選び、シードと難易度を決めてランを開始します (方式: set-confirm)", 14, UiKit.ColDim);
            UiKit.Le(sub, -1f, 20f, -1f, 20f);

            // シードと難易度
            var row = UiKit.NewRect("setuprow", body);
            UiKit.Le(row, -1f, 36f, -1f, 36f);
            var hg = UiKit.Horz(row, 8, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;

            var l1 = UiKit.Txt(row, "シード", 15, UiKit.ColText, TextAnchor.MiddleLeft);
            UiKit.Le(l1, 70f, -1f, 70f, -1f);
            MakeSeedField(g, row);
            var rnd = UiKit.Btn(row, "ランダム", delegate
            {
                g.Seed = UnityEngine.Random.Range(1, 99999);
                g.Rebuild();
            }, 14);
            SetWidth(rnd, 110f);

            var l2 = UiKit.Txt(row, "難易度 " + g.Difficulty + " / 10", 15, UiKit.ColText, TextAnchor.MiddleLeft);
            UiKit.Le(l2, 130f, -1f, 130f, -1f);
            var dm = UiKit.Btn(row, "-", delegate { g.Difficulty = Mathf.Max(1, g.Difficulty - 1); g.Rebuild(); }, 15);
            SetWidth(dm, 44f);
            var dp = UiKit.Btn(row, "+", delegate { g.Difficulty = Mathf.Min(10, g.Difficulty + 1); g.Rebuild(); }, 15);
            SetWidth(dp, 44f);

            UiKit.Head(body, "リーダー", 17);
            var content = UiKit.Scroll(body, true, new Color(0f, 0f, 0f, 0.2f), 4, 8);
            UiKit.Le(ScrollRoot(content), -1f, 120f, -1f, 120f, -1f, 1f);

            var leaders = Content.AllLeaders;
            for (int i = 0; i < leaders.Count; i++)
            {
                var ld = leaders[i];
                bool sel = ld.Id == g.LeaderId;
                string id = ld.Id;
                string label = (sel ? "▶ " : "   ") + ld.Name + "  [" + string.Join("/", ColorsJa(ld.Colors)) + "]  HP" + ld.MaxHp
                    + "  ドロー" + ld.DrawPerTurn + "  E" + ld.EnergyMax + "  報酬" + ld.RewardChoices + "枚\n      " + CardText.Short(ld.Description, 64);
                var b = UiKit.Btn(content, label, delegate { g.LeaderId = id; g.Rebuild(); }, 13, true, sel ? UiKit.Hex("#2f4d33") : UiKit.ColPanel);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) { le.minHeight = 46f; le.preferredHeight = 46f; }
            }

            var start = UiKit.Btn(body, "ランを開始", delegate { g.StartRun(); }, 18, true, UiKit.Hex("#3a5a3a"));
            var sle = start.GetComponent<LayoutElement>();
            if (sle != null) { sle.minHeight = 44f; sle.preferredHeight = 44f; }
        }

        static string[] ColorsJa(IReadOnlyList<string> colors)
        {
            var outp = new string[colors.Count];
            for (int i = 0; i < colors.Count; i++)
            {
                string c = colors[i];
                outp[i] = c == "green" ? "緑" : c == "blue" ? "青" : c == "red" ? "赤" : c == "white" ? "白" : c == "black" ? "黒" : c;
            }
            return outp;
        }

        public static TMP_InputField MakeSeedField(GameRoot g, Transform parent)
        {
            var pan = UiKit.Pan(parent, UiKit.ColPanel2, "seedField");
            UiKit.Le(pan, 150f, 30f, 150f, 30f);
            // TMP_InputField は textViewport (RectMask2D 付きの入れ物) を要る
            var viewport = UiKit.NewRect("viewport", pan.transform);
            UiKit.Stretch(viewport, 8f, 8f, 2f, 2f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var txt = UiKit.Txt(viewport, "", 15, UiKit.ColInk, TextAnchor.MiddleLeft);   // 紙の入力欄なので墨 (紙色だと数字が見えない。2026-09-09)
            UiKit.Stretch(txt.rectTransform, 0f, 0f, 0f, 0f);
            var field = pan.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = viewport;
            field.textComponent = txt;
            field.caretColor = UiKit.ColInk;
            field.customCaretColor = true;
            field.targetGraphic = pan;
            field.contentType = TMP_InputField.ContentType.IntegerNumber;
            field.characterLimit = 9;
            field.text = g.Seed.ToString();
            field.onValueChanged.AddListener(delegate (string v)
            {
                int parsed;
                if (int.TryParse(v, out parsed)) g.Seed = Mathf.Abs(parsed);
            });
            g.RegisterSeedField(field);
            return field;
        }

        // ================= 共通パーツ =================

        public static RectTransform ScrollRoot(RectTransform content)
        {
            return UiKit.ScrollRoot(content);
        }

        static void SetWidth(Component c, float w)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le != null) { le.minWidth = w; le.preferredWidth = w; }
        }

        static void Flex(Component c)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le != null) { le.flexibleWidth = 1f; le.minWidth = 20f; }
        }

        /// <summary>ランの見出し1行</summary>
        static void RunHeader(GameRoot g, Transform parent, string title)
        {
            var run = g.Rs;
            UiKit.Head(parent, title, 20);
            var t = UiKit.Txt(parent,
                "幕" + run.Act + "  行 " + (run.Row + 1) + " / " + run.Map.Count
                + "   HP " + run.Hp + " / " + run.MaxHp
                + "   " + run.Gold + "G"
                + "   デッキ " + run.Deck.Count + "枚"
                + "   レリック " + run.Relics.Count + "個"
                + "   難易度 " + run.Difficulty,
                14, UiKit.ColText);
            UiKit.Le(t, -1f, 20f, -1f, 20f);
            UiKit.Bar(parent, run.Hp, run.MaxHp, UiKit.ColHp, "HP " + run.Hp + " / " + run.MaxHp, 18);
        }

        /// <summary>デッキ一覧 (ボタン付き)。btnLabel が null を返す行はボタンなし</summary>
        static void DeckList(GameRoot g, Transform parent, IReadOnlyList<CardInstance> deck,
            Func<int, CardInstance, string> btnLabel, Func<int, CardInstance, bool> btnEnabled, Action<int> onPick,
            List<int> marked)
        {
            var content = UiKit.Scroll(parent, true, new Color(0f, 0f, 0f, 0.2f), 3, 6);
            UiKit.Le(ScrollRoot(content), -1f, 120f, -1f, 120f, -1f, 1f);
            for (int i = 0; i < deck.Count; i++)
            {
                var c = deck[i];
                int idx = i;
                bool mark = marked != null && marked.Contains(i);
                string label = (mark ? "[選択中] " : "") + "[" + CardText.CostLabel(c.Def) + "] " + c.Def.Name
                    + " (" + CardText.TypeJa(c.Def.Type) + ") — " + CardText.Short(CardText.Body(c.Def), 54);
                string bl = btnLabel != null ? btnLabel(i, c) : null;
                if (bl == null)
                {
                    var t = UiKit.Txt(content, label, 12, mark ? UiKit.ColAccent : UiKit.ColText);
                    UiKit.Le(t, -1f, 18f, -1f, 18f);
                    continue;
                }
                bool en = btnEnabled == null || btnEnabled(i, c);
                var b = UiKit.Btn(content, bl + " ｜ " + label, delegate { if (onPick != null) onPick(idx); }, 12, en,
                    mark ? UiKit.Hex("#3f6b45") : UiKit.ColPanel);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) { le.minHeight = 28f; le.preferredHeight = 28f; }
            }
        }

        /// <summary>カード1枚をカード風の縦パネルで描く (報酬・ショップ用)</summary>
        static void CardPanel(Transform parent, CardDef def, float width, string btnText, Action onClick, bool enabled)
        {
            var pan = UiKit.Pan(parent, UiKit.ColPanel, "cardpanel");
            UiKit.Le(pan, width, -1f, width, -1f);
            var rt = pan.rectTransform;

            var head = UiKit.Txt(rt, "[" + CardText.CostLabel(def) + "] " + def.Name, 15, UiKit.ColEnergy);
            UiKit.Anchor(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -46f), new Vector2(-8f, -6f));

            string notes = CardText.Notes(def);
            var sub = UiKit.Txt(rt, CardText.TypeJa(def.Type) + " " + CardText.RarityLabel(def.Rarity) + (notes.Length > 0 ? " / " + notes : ""), 11, UiKit.ColDim);
            UiKit.Anchor(sub.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -76f), new Vector2(-8f, -48f));

            var bodyT = UiKit.Txt(rt, CardText.Body(def), 12, UiKit.ColText);
            UiKit.Anchor(bodyT.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, btnText != null ? 44f : 8f), new Vector2(-8f, -78f));

            if (btnText != null)
            {
                var b = UiKit.Btn(rt, btnText, onClick, 14, enabled, UiKit.Hex("#3a5a3a"));
                UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(8f, 8f), new Vector2(-8f, 38f));
            }
        }

        // ================= マップ =================

        public static void Map(GameRoot g, RectTransform body)
        {
            var run = g.Rs;

            var side = UiKit.NewRect("side", body);
            UiKit.Anchor(side, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-SideWidth, 0f), new Vector2(0f, 0f));
            UiKit.Vert(side, 6, 0);
            UiKit.Head(side, "デッキ (" + run.Deck.Count + "枚) とレリック", 15);
            var relicNames = new List<string>();
            for (int i = 0; i < run.Relics.Count; i++)
            {
                try { relicNames.Add(Content.GetRelicDef(run.Relics[i]).Name); }
                catch (Exception) { relicNames.Add(run.Relics[i]); }
            }
            var rt2 = UiKit.Txt(side, "レリック: " + (relicNames.Count > 0 ? string.Join("、", relicNames.ToArray()) : "なし"), 12, UiKit.ColDim);
            UiKit.Le(rt2, -1f, 34f, -1f, 34f);
            DeckList(g, side, run.Deck, null, null, null, null);

            var main = UiKit.NewRect("main", body);
            UiKit.Anchor(main, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-(SideWidth + 8f), 0f));
            UiKit.Vert(main, 6, 0);

            RunHeader(g, main, "マップ — 進路を選ぶ");

            var cur = DeckRogue.Engine.Run.CurrentNode(run);
            var curT = UiKit.Txt(main, "現在地: " + (cur == null ? "スタート地点 (まだ盤に乗っていない)" : NodeLabel(cur)), 14, UiKit.ColDim);
            UiKit.Le(curT, -1f, 20f, -1f, 20f);

            var choices = DeckRogue.Engine.Run.NextChoices(run);
            UiKit.Head(main, "次に進めるノード (" + choices.Count + ")", 16);

            var content = UiKit.Scroll(main, true, new Color(0f, 0f, 0f, 0.2f), 5, 8);
            UiKit.Le(ScrollRoot(content), -1f, 120f, -1f, 120f, -1f, 1f);

            int nextRow = run.Row + 1;
            if (choices.Count == 0)
            {
                UiKit.Txt(content, "進めるノードがありません", 14, UiKit.ColBad);
            }
            for (int i = 0; i < choices.Count; i++)
            {
                int col = choices[i];
                MapNode node = null;
                if (nextRow >= 0 && nextRow < run.Map.Count && col >= 0 && col < run.Map[nextRow].Count) node = run.Map[nextRow][col];
                string label = node != null ? NodeLabel(node) : ("ノード " + col);
                var b = UiKit.Btn(content, "▶ " + label, delegate { g.Do(new RunCommand_ChooseNode { Col = col }); }, 15, true, UiKit.ColPanel2);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) { le.minHeight = 40f; le.preferredHeight = 40f; }
            }

            var abandon = UiKit.Btn(main, "ランを放棄してタイトルへ", delegate { g.BackToSetup(); }, 13);
            var ale = abandon.GetComponent<LayoutElement>();
            if (ale != null) { ale.minHeight = 28f; ale.preferredHeight = 28f; }
        }

        static string NodeLabel(MapNode n)
        {
            string enc = "";
            if (n.EncounterId != null)
            {
                try { enc = ": " + Content.EncounterName(n.EncounterId); }
                catch (Exception) { enc = ": " + n.EncounterId; }
            }
            switch (n.Type)
            {
                case MapNodeTypes.Battle: return "[戦闘]" + enc;
                case MapNodeTypes.Elite: return "[エリート]" + enc;
                case MapNodeTypes.Boss: return "[幕ボス]" + enc;
                case MapNodeTypes.Campfire: return "[焚き火] 休む / 鍛える";
                case MapNodeTypes.Workshop: return "[工房] カード2枚を合成 (" + DeckRogue.Engine.Run.WORKSHOP_FUSE_PRICE + "G)";
                case MapNodeTypes.Shop: return "[ショップ] カード・レリック・除去・鍛える";
                case MapNodeTypes.Event: return "[?] 入るまで中身は不明";
                case MapNodeTypes.Treasure: return "[宝箱] レリック3択";
                default: return "[" + n.Type + "]" + enc;
            }
        }

        // ================= 報酬 =================

        public static void Reward(GameRoot g, RectTransform body)
        {
            var run = g.Rs;
            UiKit.Vert(body, 8, 0);
            RunHeader(g, body, "カード報酬 — 1枚選ぶ");

            var content = UiKit.Scroll(body, false, new Color(0f, 0f, 0f, 0.2f), 10, 10);
            UiKit.Le(ScrollRoot(content), -1f, 200f, -1f, 200f, -1f, 1f);

            var opts = run.RewardOptions;
            if (opts == null || opts.Count == 0)
            {
                UiKit.Txt(content, "(候補なし)", 14, UiKit.ColDim);
            }
            else
            {
                for (int i = 0; i < opts.Count; i++)
                {
                    int idx = i;
                    CardDef def = null;
                    try { def = Content.GetCardDef(opts[i]); }
                    catch (Exception) { }
                    if (def == null) continue;
                    CardPanel(content, def, 250f, "取る", delegate { g.Do(new RunCommand_PickReward { Index = idx }); }, true);
                }
            }

            var skip = UiKit.Btn(body, "見送る (スキップ)", delegate { g.Do(new RunCommand_SkipReward()); }, 15);
            var le = skip.GetComponent<LayoutElement>();
            if (le != null) { le.minHeight = 36f; le.preferredHeight = 36f; }
        }

        public static void RelicReward(GameRoot g, RectTransform body)
        {
            var run = g.Rs;
            UiKit.Vert(body, 8, 0);
            RunHeader(g, body, "レリック — 1個選ぶ");

            var content = UiKit.Scroll(body, true, new Color(0f, 0f, 0f, 0.2f), 6, 10);
            UiKit.Le(ScrollRoot(content), -1f, 150f, -1f, 150f, -1f, 1f);

            var opts = run.RelicOptions;
            if (opts == null || opts.Count == 0)
            {
                UiKit.Txt(content, "(候補なし)", 14, UiKit.ColDim);
            }
            else
            {
                for (int i = 0; i < opts.Count; i++)
                {
                    int idx = i;
                    RelicDef rd = null;
                    try { rd = Content.GetRelicDef(opts[i]); }
                    catch (Exception) { }
                    string label = rd != null
                        ? rd.Name + " [" + CardText.RarityLabel(rd.Rarity) + "]\n    " + CardText.Short(rd.Description, 62)
                        : opts[i];
                    var b = UiKit.Btn(content, label, delegate { g.Do(new RunCommand_PickRelic { Index = idx }); }, 14, true, UiKit.ColPanel2);
                    var le2 = b.GetComponent<LayoutElement>();
                    if (le2 != null) { le2.minHeight = 48f; le2.preferredHeight = 48f; }
                }
            }

            var skip = UiKit.Btn(body, "見送る", delegate { g.Do(new RunCommand_SkipRelic()); }, 15);
            var le = skip.GetComponent<LayoutElement>();
            if (le != null) { le.minHeight = 36f; le.preferredHeight = 36f; }
        }

        // ================= 焚き火 =================

        public static void Campfire(GameRoot g, RectTransform body)
        {
            var run = g.Rs;
            UiKit.Vert(body, 6, 0);
            RunHeader(g, body, "焚き火 — 休む / 鍛える");

            int allowed = DeckRogue.Engine.Run.CampfireForgeAllowed(run);
            int heal = (int)Math.Floor(run.MaxHp * run.CampfireRatio);
            bool noRest = false;
            for (int i = 0; i < run.Relics.Count; i++)
            {
                try { if (Content.GetRelicDef(run.Relics[i]).Bonus != null && Content.GetRelicDef(run.Relics[i]).Bonus.NoRest == true) noRest = true; }
                catch (Exception) { }
            }
            string restNote = noRest ? "(レリックの効果で休んでも回復しない)" : (run.CampfireUpgradesUsed > 0 ? "(すでに鍛えたので回復なしで立ち去る)" : "HP+" + heal);
            var t = UiKit.Txt(body, "休む: " + restNote + "   /   鍛える: 残り " + Math.Max(0, allowed - run.CampfireUpgradesUsed) + " 回", 14, UiKit.ColText);
            UiKit.Le(t, -1f, 22f, -1f, 22f);

            var rest = UiKit.Btn(body, "休む (立ち去る)", delegate { g.Do(new RunCommand_CampfireRest()); }, 16, true, UiKit.Hex("#3a5a3a"));
            var rle = rest.GetComponent<LayoutElement>();
            if (rle != null) { rle.minHeight = 40f; rle.preferredHeight = 40f; }

            UiKit.Head(body, "鍛える札を選ぶ", 16);
            DeckList(g, body, run.Deck,
                delegate (int i, CardInstance c) { return Upgrade.CanUpgradeCard(c) ? "鍛える" : "—"; },
                delegate (int i, CardInstance c) { return Upgrade.CanUpgradeCard(c); },
                delegate (int i) { g.Do(new RunCommand_CampfireUpgrade { Index = i }); },
                null);
        }

        // ================= 工房 =================

        public static void Workshop(GameRoot g, RectTransform body)
        {
            var run = g.Rs;

            var side = UiKit.NewRect("wside", body);
            UiKit.Anchor(side, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-SideWidth, 0f), new Vector2(0f, 0f));
            UiKit.Vert(side, 6, 0);

            int price = DeckRogue.Engine.Run.WorkshopFusePrice(run);
            UiKit.Head(side, "合成 (" + price + "G / 所持 " + run.Gold + "G)", 16);

            CardInstance a = (g.WorkshopA >= 0 && g.WorkshopA < run.Deck.Count) ? run.Deck[g.WorkshopA] : null;
            CardInstance b = (g.WorkshopB >= 0 && g.WorkshopB < run.Deck.Count) ? run.Deck[g.WorkshopB] : null;
            var sel = UiKit.Txt(side, "素材A: " + (a != null ? a.Def.Name : "—") + "\n素材B: " + (b != null ? b.Def.Name : "—"), 14, UiKit.ColText);
            UiKit.Le(sel, -1f, 40f, -1f, 40f);

            string blocked = null;
            CardDef fused = null;
            var notes = new List<string>();
            if (a != null && b != null)
            {
                try
                {
                    blocked = Fusion.FuseBlockReason(a, b);
                    if (blocked == null)
                    {
                        fused = Fusion.FuseCards(a, b);
                        var fn = Fusion.FusionNotes(a, b);
                        for (int i = 0; i < fn.Count; i++) notes.Add(fn[i]);
                    }
                }
                catch (Exception ex) { blocked = ex.Message; }
            }

            if (blocked != null)
            {
                var w = UiKit.Txt(side, "合成できない: " + blocked, 13, UiKit.ColBad);
                UiKit.Le(w, -1f, 40f, -1f, 40f);
            }
            else if (fused != null)
            {
                var res = UiKit.Txt(side, "→ [" + CardText.CostLabel(fused) + "] " + fused.Name + " (" + CardText.TypeJa(fused.Type) + ")\n"
                    + CardText.Body(fused) + (CardText.Notes(fused).Length > 0 ? "\n" + CardText.Notes(fused) : ""), 13, UiKit.ColAccent);
                UiKit.Le(res, -1f, 130f, -1f, 130f);
                if (notes.Count > 0)
                {
                    var nt = UiKit.Txt(side, "注記: " + string.Join(" / ", notes.ToArray()), 11, UiKit.ColDim);
                    UiKit.Le(nt, -1f, 60f, -1f, 60f);
                }
            }
            else
            {
                var w = UiKit.Txt(side, "デッキから2枚選んでください", 13, UiKit.ColDim);
                UiKit.Le(w, -1f, 30f, -1f, 30f);
            }

            int ia = g.WorkshopA;
            int ib = g.WorkshopB;
            var fuse = UiKit.Btn(side, "合成する (" + price + "G)",
                delegate { g.Do(new RunCommand_WorkshopFuse { IndexA = ia, IndexB = ib }); }, 16,
                a != null && b != null && blocked == null && run.Gold >= price, UiKit.Hex("#3a5a3a"));
            var fle = fuse.GetComponent<LayoutElement>();
            if (fle != null) { fle.minHeight = 40f; fle.preferredHeight = 40f; }

            var clear = UiKit.Btn(side, "選択をやり直す", delegate { g.WorkshopA = -1; g.WorkshopB = -1; g.Rebuild(); }, 13);
            var cle = clear.GetComponent<LayoutElement>();
            if (cle != null) { cle.minHeight = 28f; cle.preferredHeight = 28f; }

            var skip = UiKit.Btn(side, "見送る", delegate { g.Do(new RunCommand_WorkshopSkip()); }, 15);
            var sle = skip.GetComponent<LayoutElement>();
            if (sle != null) { sle.minHeight = 34f; sle.preferredHeight = 34f; }

            var main = UiKit.NewRect("wmain", body);
            UiKit.Anchor(main, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-(SideWidth + 8f), 0f));
            UiKit.Vert(main, 6, 0);
            RunHeader(g, main, "工房 — 同じ色のカード2枚を1枚に溶かす");

            var marked = new List<int>();
            if (g.WorkshopA >= 0) marked.Add(g.WorkshopA);
            if (g.WorkshopB >= 0) marked.Add(g.WorkshopB);

            DeckList(g, main, run.Deck,
                delegate (int i, CardInstance c) { return marked.Contains(i) ? "外す" : "選ぶ"; },
                null,
                delegate (int i)
                {
                    if (g.WorkshopA == i) g.WorkshopA = -1;
                    else if (g.WorkshopB == i) g.WorkshopB = -1;
                    else if (g.WorkshopA < 0) g.WorkshopA = i;
                    else if (g.WorkshopB < 0) g.WorkshopB = i;
                    else { g.WorkshopA = g.WorkshopB; g.WorkshopB = i; }  // 3枚目は古い方と入れ替える
                    g.Rebuild();
                },
                marked);
        }

        // ================= ショップ =================

        public static void Shop(GameRoot g, RectTransform body)
        {
            var run = g.Rs;
            var shop = run.Shop;

            var main = UiKit.NewRect("smain", body);
            UiKit.Anchor(main, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(g.ShopMode != null ? -(SideWidth + 8f) : 0f, 0f));
            UiKit.Vert(main, 6, 0);
            RunHeader(g, main, "ショップ");

            if (shop == null)
            {
                UiKit.Txt(main, "在庫がありません", 14, UiKit.ColBad);
                UiKit.Btn(main, "出る", delegate { g.Do(new RunCommand_ShopLeave()); }, 15);
                return;
            }

            var content = UiKit.Scroll(main, true, new Color(0f, 0f, 0f, 0.2f), 4, 8);
            UiKit.Le(ScrollRoot(content), -1f, 120f, -1f, 120f, -1f, 1f);

            for (int i = 0; i < shop.Cards.Count; i++)
            {
                int idx = i;
                var item = shop.Cards[i];
                CardDef def = null;
                try { def = Content.GetCardDef(item.Id); }
                catch (Exception) { }
                string nm = def != null ? def.Name : item.Id;
                string bodyTxt = def != null ? CardText.Short(CardText.Body(def), 62) : "";
                string label = (item.Sold == true ? "[売切] " : item.Price + "G ") + "[" + (def != null ? CardText.CostLabel(def) : "?") + "] " + nm
                    + " (" + (def != null ? CardText.TypeJa(def.Type) + " " + CardText.RarityLabel(def.Rarity) : "") + ")\n    " + bodyTxt;
                var b = UiKit.Btn(content, label, delegate { g.Do(new RunCommand_ShopBuyCard { Index = idx }); }, 12,
                    item.Sold != true && run.Gold >= item.Price, UiKit.ColPanel);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) { le.minHeight = 42f; le.preferredHeight = 42f; }
            }

            if (shop.RelicId != null)
            {
                RelicDef rd = null;
                try { rd = Content.GetRelicDef(shop.RelicId); }
                catch (Exception) { }
                string label = shop.RelicPrice + "G [レリック] " + (rd != null ? rd.Name + "\n    " + CardText.Short(rd.Description, 62) : shop.RelicId);
                var b = UiKit.Btn(content, label, delegate { g.Do(new RunCommand_ShopBuyRelic()); }, 12,
                    run.Gold >= shop.RelicPrice, UiKit.ColPanel);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) { le.minHeight = 42f; le.preferredHeight = 42f; }
            }

            int rmPrice = DeckRogue.Engine.Run.ShopRemovalPrice(run);
            int upPrice = DeckRogue.Engine.Run.ShopUpgradePrice(run);

            var row = UiKit.NewRect("shoprow", main);
            UiKit.Le(row, -1f, 40f, -1f, 40f);
            var hg = UiKit.Horz(row, 8, 0);
            hg.childForceExpandWidth = true;

            var rmBtn = UiKit.Btn(row, (g.ShopMode == "remove" ? "▶ " : "") + "カード除去 (" + rmPrice + "G)",
                delegate { g.ShopMode = g.ShopMode == "remove" ? null : "remove"; g.Rebuild(); }, 14, run.Gold >= rmPrice);
            Flex(rmBtn);
            var upBtn = UiKit.Btn(row, (g.ShopMode == "upgrade" ? "▶ " : "") + "鍛える (" + upPrice + "G)",
                delegate { g.ShopMode = g.ShopMode == "upgrade" ? null : "upgrade"; g.Rebuild(); }, 14, run.Gold >= upPrice);
            Flex(upBtn);
            var leave = UiKit.Btn(row, "出る", delegate { g.ShopMode = null; g.Do(new RunCommand_ShopLeave()); }, 14);
            Flex(leave);

            if (g.ShopMode != null)
            {
                var side = UiKit.NewRect("sside", body);
                UiKit.Anchor(side, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-SideWidth, 0f), new Vector2(0f, 0f));
                UiKit.Vert(side, 6, 0);
                bool removing = g.ShopMode == "remove";
                UiKit.Head(side, removing ? "除去する札を選ぶ" : "鍛える札を選ぶ", 16);
                DeckList(g, side, run.Deck,
                    delegate (int i, CardInstance c) { return removing ? "除去" : (Upgrade.CanUpgradeCard(c) ? "鍛える" : "—"); },
                    delegate (int i, CardInstance c) { return removing ? run.Deck.Count > 5 : Upgrade.CanUpgradeCard(c); },
                    delegate (int i)
                    {
                        bool rm = g.ShopMode == "remove";
                        g.ShopMode = null;
                        if (rm) g.Do(new RunCommand_ShopRemove { Index = i });
                        else g.Do(new RunCommand_ShopUpgrade { Index = i });
                    },
                    null);
            }
        }

        // ================= イベント =================

        public static void EventRoom(GameRoot g, RectTransform body)
        {
            var run = g.Rs;
            UiKit.Vert(body, 6, 0);

            EventDef def = null;
            try { if (run.EventId != null) def = Content.GetEventDef(run.EventId); }
            catch (Exception) { }

            RunHeader(g, body, def != null ? def.Name : "イベント");
            var flavor = UiKit.Txt(body, def != null ? def.Flavor : "(イベント定義が見つかりません: " + run.EventId + ")", 14, UiKit.ColText);
            UiKit.Le(flavor, -1f, 70f, -1f, 70f);

            if (def == null) return;

            if (g.EventChoiceIndex >= 0 && g.EventChoiceIndex < def.Choices.Count)
            {
                var ch = def.Choices[g.EventChoiceIndex];
                UiKit.Head(body, "「" + ch.Label + "」の対象カードを選ぶ", 16);
                int ci = g.EventChoiceIndex;
                DeckList(g, body, run.Deck,
                    delegate (int i, CardInstance c) { return "これ"; },
                    null,
                    delegate (int i)
                    {
                        g.EventChoiceIndex = -1;
                        g.Do(new RunCommand_EventChoice { Index = ci, CardIndex = i });
                    },
                    null);
                var cancel = UiKit.Btn(body, "選び直す", delegate { g.EventChoiceIndex = -1; g.Rebuild(); }, 13);
                var cle = cancel.GetComponent<LayoutElement>();
                if (cle != null) { cle.minHeight = 28f; cle.preferredHeight = 28f; }
                return;
            }

            var content = UiKit.Scroll(body, true, new Color(0f, 0f, 0f, 0.2f), 6, 8);
            UiKit.Le(ScrollRoot(content), -1f, 120f, -1f, 120f, -1f, 1f);
            for (int i = 0; i < def.Choices.Count; i++)
            {
                int idx = i;
                var ch = def.Choices[i];
                bool needsCard = DeckRogue.Engine.Run.EventChoiceNeedsCard(ch);
                string label = ch.Label + (needsCard ? "  (デッキから1枚選ぶ)" : "") + ChoiceHint(ch);
                var b = UiKit.Btn(content, label, delegate
                {
                    if (needsCard) { g.EventChoiceIndex = idx; g.Rebuild(); }
                    else g.Do(new RunCommand_EventChoice { Index = idx });
                }, 14, true, UiKit.ColPanel2);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) { le.minHeight = 40f; le.preferredHeight = 40f; }
            }
        }

        static string ChoiceHint(EventChoiceDef ch)
        {
            var parts = new List<string>();
            if (ch.Gold.HasValue) parts.Add((ch.Gold.Value >= 0 ? "+" : "") + ch.Gold.Value + "G");
            if (ch.RequireGold.HasValue) parts.Add("要" + ch.RequireGold.Value + "G");
            if (ch.Hp.HasValue) parts.Add("HP" + (ch.Hp.Value >= 0 ? "+" : "") + ch.Hp.Value);
            if (ch.MaxHp.HasValue) parts.Add("最大HP" + (ch.MaxHp.Value >= 0 ? "+" : "") + ch.MaxHp.Value);
            if (ch.HpRatio.HasValue) parts.Add("最大HP比" + ch.HpRatio.Value);
            if (ch.Wounds.HasValue) parts.Add("負傷" + ch.Wounds.Value);
            if (ch.Brands.HasValue) parts.Add("烙印" + ch.Brands.Value);
            if (ch.TimedCurses.HasValue) parts.Add("仮初の烙印" + ch.TimedCurses.Value);
            if (ch.AddRandomCards.HasValue) parts.Add("ランダムカード" + ch.AddRandomCards.Value + "枚");
            if (ch.Relic == true) parts.Add("レリック");
            if (ch.RemoveAllWounds == true) parts.Add("負傷を全て除去");
            if (ch.UpgradeRandomCards.HasValue) parts.Add("ランダムに" + ch.UpgradeRandomCards.Value + "枚鍛える");
            if (ch.Gamble != null) parts.Add("賭け");
            return parts.Count > 0 ? "  [" + string.Join(" / ", parts.ToArray()) + "]" : "";
        }

        // ================= 決着 =================

        public static void Ended(GameRoot g, RectTransform body, bool won)
        {
            var run = g.Rs;
            UiKit.Vert(body, 10, 0);
            UiKit.Head(body, won ? "=== 走破! ===" : "=== 敗北 ===", 26);
            var t = UiKit.Txt(body,
                "リーダー: " + SafeLeaderName(run.LeaderId) + "\n"
                + "シード: " + run.Seed + " / 難易度 " + run.Difficulty + "\n"
                + "幕" + run.Act + " 行" + (run.Row + 1) + " / 勝利 " + run.BattlesWon + "戦\n"
                + "HP " + run.Hp + " / " + run.MaxHp + "  所持金 " + run.Gold + "G\n"
                + "デッキ " + run.Deck.Count + "枚 / レリック " + run.Relics.Count + "個",
                17, UiKit.ColText);
            UiKit.Le(t, -1f, 130f, -1f, 130f);

            var again = UiKit.Btn(body, "もう一度 (タイトルへ)", delegate { g.BackToSetup(); }, 18, true, UiKit.Hex("#3a5a3a"));
            var le = again.GetComponent<LayoutElement>();
            if (le != null) { le.minHeight = 44f; le.preferredHeight = 44f; }

            UiKit.Head(body, "最終デッキ", 16);
            DeckList(g, body, run.Deck, null, null, null, null);
        }

        static string SafeLeaderName(string id)
        {
            try { return Content.GetLeaderDef(id).Name; }
            catch (Exception) { return id; }
        }
    }
}
