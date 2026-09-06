// CombatScreen.cs — 戦闘画面。上=敵 / 中=伏せ場と置物 / 下=ステータスと手札 / 右=ログ。
// エンジンには触らず、GameState を読んで Command を投げるだけ。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class CombatScreen
    {
        const float LogWidth = 320f;

        public static void Build(GameRoot g, RectTransform body)
        {
            var run = g.Rs;
            var st = run.Combat;
            if (st == null)
            {
                UiKit.Txt(body, "戦闘データがありません", 18, UiKit.ColBad);
                return;
            }

            // 右: ログ
            var logRoot = UiKit.NewRect("logRoot", body);
            UiKit.Anchor(logRoot, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-LogWidth, 0f), new Vector2(0f, 0f));
            BuildLog(logRoot, st);

            // 左: 本体
            var left = UiKit.NewRect("left", body);
            UiKit.Anchor(left, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-(LogWidth + 8f), 0f));
            UiKit.Vert(left, 6, 0);

            BuildHeader(g, left, run, st);
            BuildEnemies(g, left, st);
            BuildField(g, left, st);
            BuildStatus(g, left, st);
            BuildHand(g, left, st);

            // オーバーレイ
            if (st.Phase == CombatPhases.AwaitingReaction) BuildReactionWindow(g, body, st);
            else if (g.Pending != null)
            {
                string need = g.Pending.NextNeed();
                if (need == "target") BuildTargetBanner(g, body);
                else if (need != null) BuildPicker(g, body, st, need);
            }
        }

        // ---- 見出し ----

        static void BuildHeader(GameRoot g, Transform parent, RunState run, GameState st)
        {
            var row = UiKit.NewRect("cheader", parent);
            UiKit.Le(row, -1f, 22f, -1f, 22f);
            string enc = "";
            try
            {
                var node = DeckRogue.Engine.Run.CurrentNode(run);
                if (node != null && node.EncounterId != null) enc = Content.EncounterName(node.EncounterId);
            }
            catch (Exception) { }
            var t = UiKit.Txt(row, "幕" + run.Act + " 行" + (run.Row + 1) + "  " + enc + "  /  ターン " + st.Turn + "  /  " + PhaseJa(st.Phase), 14, UiKit.ColDim);
            UiKit.Stretch(t.rectTransform, 2f, 2f, 0f, 0f);
        }

        static string PhaseJa(string p)
        {
            if (p == CombatPhases.PlayerTurn) return "自ターン";
            if (p == CombatPhases.AwaitingReaction) return "リアクション確認";
            if (p == CombatPhases.Won) return "勝利";
            if (p == CombatPhases.Lost) return "敗北";
            return p;
        }

        // ---- 敵 ----

        static void BuildEnemies(GameRoot g, Transform parent, GameState st)
        {
            var content = UiKit.Scroll(parent, false, new Color(0f, 0f, 0f, 0.15f), 8, 8);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 212f, -1f, 212f);
            for (int i = 0; i < st.Enemies.Count; i++) EnemyPanel(g, content, st, i);
        }

        static void EnemyPanel(GameRoot g, Transform parent, GameState st, int index)
        {
            var e = st.Enemies[index];
            bool alive = e.Hp > 0;
            bool aimed = g.PreferredTarget == index || (g.Pending != null && g.Pending.TargetIndex.HasValue && g.Pending.TargetIndex.Value == index);
            var pan = UiKit.Pan(parent, alive ? (aimed ? UiKit.ColPanel2 : UiKit.ColPanel) : new Color(0.09f, 0.1f, 0.1f, 0.8f), "enemy");
            g.RegisterAnchor("enemy" + index, pan.rectTransform);
            UiKit.Le(pan, 258f, -1f, 258f, -1f);
            UiKit.Vert(pan.transform, 3, 8);

            EnemyDef def = null;
            try { def = Content.GetEnemyDef(e.EnemyId); }
            catch (Exception) { }
            string nm = def != null ? def.Name : e.EnemyId;
            string dead = alive ? "" : (e.Fled == true ? " (逃走)" : " (撃破)");
            var title = UiKit.Txt(pan.transform, (index + 1) + ". " + nm + dead + (aimed ? "  ◎" : ""), 15, alive ? UiKit.ColText : UiKit.ColDim);
            UiKit.Le(title, -1f, 20f, -1f, 20f);

            UiKit.Bar(pan.transform, e.Hp, e.MaxHp, UiKit.ColHp, "HP " + e.Hp + " / " + e.MaxHp, 18);

            var chips = new List<string>();
            if (e.Block > 0) chips.Add("ブロック" + e.Block);
            if (e.Strength != 0) chips.Add("筋力" + (e.Strength > 0 ? "+" : "") + e.Strength);
            if (e.Burn > 0) chips.Add("延焼" + e.Burn);
            if (e.Exposed > 0) chips.Add("急所" + e.Exposed);
            if (e.Confusion > 0) chips.Add("混乱" + e.Confusion);
            if ((e.Weak.HasValue ? e.Weak.Value : 0) > 0) chips.Add("威圧" + e.Weak.Value);
            if ((e.Artifact.HasValue ? e.Artifact.Value : 0) > 0) chips.Add("アーティファクト" + e.Artifact.Value);
            if (e.BurrowActive == true) chips.Add("潜伏中");
            var chipT = UiKit.Txt(pan.transform, chips.Count > 0 ? string.Join(" ", chips.ToArray()) : "-", 12, UiKit.ColBlock);
            UiKit.Le(chipT, -1f, 16f, -1f, 16f);

            string traits = CardText.EnemyTraits(def);
            if (traits.Length > 0)
            {
                var tr = UiKit.Txt(pan.transform, traits, 11, UiKit.ColDim);
                UiKit.Le(tr, -1f, 26f, -1f, 26f);
            }

            if (alive)
            {
                var it = UiKit.Txt(pan.transform, CardText.IntentText(st, index), 13, UiKit.ColEnergy);
                UiKit.Le(it, -1f, 48f, -1f, 48f);
            }

            var btn = pan.gameObject.AddComponent<Button>();
            btn.targetGraphic = pan;
            btn.interactable = alive;
            int captured = index;
            btn.onClick.AddListener(delegate { g.OnEnemyClicked(captured); });
        }

        // ---- 伏せ場・置物 ----

        static void BuildField(GameRoot g, Transform parent, GameState st)
        {
            var pan = UiKit.Pan(parent, new Color(0f, 0f, 0f, 0.15f), "field");
            UiKit.Le(pan, -1f, 96f, -1f, 96f);
            UiKit.Vert(pan.transform, 3, 6);

            // 伏せ場
            var setRow = UiKit.NewRect("setrow", pan.transform);
            UiKit.Le(setRow, -1f, 40f, -1f, 40f);
            var hg = UiKit.Horz(setRow, 6, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            var lab = UiKit.Txt(setRow, "伏せ場 " + st.Player.SetCards.Count + "/" + st.Player.SetSlots, 13, UiKit.ColAccent, TextAnchor.MiddleLeft);
            UiKit.Le(lab, 110f, -1f, 110f, -1f);
            if (st.Player.SetCards.Count == 0)
            {
                var t = UiKit.Txt(setRow, "(伏せ札なし)", 12, UiKit.ColDim, TextAnchor.MiddleLeft);
                UiKit.Le(t, 160f, -1f, 160f, -1f);
            }
            for (int i = 0; i < st.Player.SetCards.Count; i++)
            {
                var c = st.Player.SetCards[i];
                string uid = c.Uid;
                var b = UiKit.Btn(setRow, c.Def.Name + " [回収1E]", delegate { g.DoCombat(new Command_RetrieveSetCard { CardUid = uid }); }, 12,
                    st.Phase == CombatPhases.PlayerTurn);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) { le.preferredWidth = 230f; le.minWidth = 160f; }
            }

            // 置物 + 亡骸プレイ
            var permRow = UiKit.NewRect("permrow", pan.transform);
            UiKit.Le(permRow, -1f, 38f, -1f, 38f);
            var hg2 = UiKit.Horz(permRow, 6, 0);
            hg2.childAlignment = TextAnchor.MiddleLeft;
            var names = new List<string>();
            for (int i = 0; i < st.Player.Permanents.Count; i++) names.Add(st.Player.Permanents[i].Def.Name);
            var pt = UiKit.Txt(permRow, "置物: " + (names.Count > 0 ? string.Join("、", names.ToArray()) : "なし"), 12, UiKit.ColDim, TextAnchor.MiddleLeft);
            UiKit.Le(pt, 200f, -1f, 430f, -1f);

            for (int i = 0; i < st.Player.ExhaustPile.Count; i++)
            {
                var c = st.Player.ExhaustPile[i];
                if (!c.Def.NecroCost.HasValue) continue;
                string uid = c.Uid;
                var b = UiKit.Btn(permRow, "亡骸: " + c.Def.Name + " (" + c.Def.NecroCost.Value + "E)",
                    delegate { g.DoCombat(new Command_PlayNecro { CardUid = uid }); }, 12,
                    st.Phase == CombatPhases.PlayerTurn && st.Player.Energy >= c.Def.NecroCost.Value);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) { le.preferredWidth = 210f; le.minWidth = 150f; }
            }
        }

        // ---- ステータス ----

        static void BuildStatus(GameRoot g, Transform parent, GameState st)
        {
            var pan = UiKit.Pan(parent, UiKit.ColPanel, "status");
            g.RegisterAnchor("player", pan.rectTransform);
            UiKit.Le(pan, -1f, 78f, -1f, 78f);
            UiKit.Horz(pan.transform, 8, 6);

            var col = UiKit.NewRect("statcol", pan.transform);
            UiKit.Le(col, 100f, -1f, 100f, -1f, 1f, -1f);
            UiKit.Vert(col, 2, 0);

            var p = st.Player;
            UiKit.Bar(col, p.Hp, p.MaxHp, UiKit.ColHp, "HP " + p.Hp + " / " + p.MaxHp, 18);

            var line1 = new List<string>();
            line1.Add("E " + p.Energy + " / " + p.EnergyMax);
            if (p.Block > 0) line1.Add("ブロック " + p.Block);
            if (p.IceBlock > 0) line1.Add("氷壁 " + p.IceBlock);
            if (p.Growth > 0) line1.Add("成長 " + p.Growth);
            if (p.Momentum > 0) line1.Add("勢い " + p.Momentum);
            if (p.Aether > 0) line1.Add("霊気 " + p.Aether);
            if (p.NextCardDiscount > 0) line1.Add("割引 -" + p.NextCardDiscount);
            if (p.SpellEchoes > 0) line1.Add("反復 " + p.SpellEchoes);
            line1.Add("詠唱 " + p.CardsPlayedThisTurn);
            line1.Add("山 " + p.DrawPile.Count + " / 捨 " + p.DiscardPile.Count + " / 消 " + p.ExhaustPile.Count);
            var t1 = UiKit.Txt(col, string.Join("  ", line1.ToArray()), 13, UiKit.ColText);
            UiKit.Le(t1, -1f, 17f, -1f, 17f);

            var line2 = new List<string>();
            if (p.Weak > 0) line2.Add("弱体" + p.Weak);
            if (p.Vulnerable > 0) line2.Add("脆弱" + p.Vulnerable);
            if (p.Frail > 0) line2.Add("虚弱" + p.Frail);
            if (p.Restrain > 0) line2.Add("拘束" + p.Restrain);
            if ((p.Mist.HasValue ? p.Mist.Value : 0) > 0) line2.Add("霞み" + p.Mist.Value);
            if ((p.Slow.HasValue ? p.Slow.Value : 0) > 0) line2.Add("重り" + p.Slow.Value);
            if (p.DamageTakenLastEnemyPhase > 0) line2.Add("直前の被弾" + p.DamageTakenLastEnemyPhase);
            var t2 = UiKit.Txt(col, line2.Count > 0 ? "状態異常: " + string.Join(" ", line2.ToArray()) : "状態異常: なし", 12, line2.Count > 0 ? UiKit.ColBad : UiKit.ColDim);
            UiKit.Le(t2, -1f, 16f, -1f, 16f);

            var endBtn = UiKit.Btn(pan.transform, "ターン終了", delegate { g.DoCombat(new Command_EndTurn()); }, 16,
                st.Phase == CombatPhases.PlayerTurn, UiKit.Hex("#3a5a3a"));
            var le2 = endBtn.GetComponent<LayoutElement>();
            if (le2 != null) { le2.preferredWidth = 140f; le2.minWidth = 140f; le2.preferredHeight = 60f; le2.minHeight = 60f; }
        }

        // ---- 手札 ----

        static void BuildHand(GameRoot g, Transform parent, GameState st)
        {
            var content = UiKit.Scroll(parent, false, new Color(0f, 0f, 0f, 0.15f), 8, 8);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 200f, -1f, 240f, -1f, 1f);
            if (st.Player.Hand.Count == 0)
            {
                var t = UiKit.Txt(content, "(手札なし)", 14, UiKit.ColDim, TextAnchor.MiddleCenter);
                UiKit.Le(t, 200f, -1f, 200f, -1f);
                return;
            }
            for (int i = 0; i < st.Player.Hand.Count; i++) HandCard(g, content, st, st.Player.Hand[i]);
        }

        static void HandCard(GameRoot g, Transform parent, GameState st, CardInstance c)
        {
            bool myTurn = st.Phase == CombatPhases.PlayerTurn;
            int cost = 0;
            try { cost = Effects.EffectiveCost(st, c); }
            catch (Exception) { cost = c.Def.Cost; }
            bool playable = myTurn && Effects.IsPlayableFromHand(c) && cost <= st.Player.Energy
                && Effects.RetainerRequirementMet(st, c);
            bool settable = SetBase.CanSetCard(st, c.Uid);

            var pan = UiKit.Pan(parent, playable ? UiKit.ColPanel : new Color(0.11f, 0.13f, 0.13f, 1f), "hand");
            UiKit.Le(pan, 186f, -1f, 186f, -1f);
            var rt = pan.rectTransform;

            string costLabel = c.Def.XCost == true ? "X" : cost.ToString();
            if (c.Def.XCost != true && cost != c.Def.Cost) costLabel = cost + "(元" + c.Def.Cost + ")";
            var head = UiKit.Txt(rt, "[" + costLabel + "] " + c.Def.Name, 14, playable ? UiKit.ColEnergy : UiKit.ColDim);
            UiKit.Anchor(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(6f, -44f), new Vector2(-6f, -4f));

            var notes = CardText.Notes(c.Def);
            var sub = UiKit.Txt(rt, CardText.TypeJa(c.Def.Type) + (notes.Length > 0 ? " / " + notes : ""), 10, UiKit.ColDim);
            UiKit.Anchor(sub.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(6f, -74f), new Vector2(-6f, -46f));

            var bodyT = UiKit.Txt(rt, CardText.Body(c.Def), 11, UiKit.ColText);
            UiKit.Anchor(bodyT.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(6f, 36f), new Vector2(-6f, -76f));

            var btnRow = UiKit.NewRect("btns", rt);
            UiKit.Anchor(btnRow, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 4f), new Vector2(-4f, 32f));
            var hg = UiKit.Horz(btnRow, 3, 0);
            hg.childForceExpandWidth = true;

            var card = c;
            if (c.Def.Modes != null && c.Def.Modes.Count > 0)
            {
                for (int m = 0; m < c.Def.Modes.Count; m++)
                {
                    int mi = m;
                    var b = UiKit.Btn(btnRow, (m + 1).ToString(), delegate { g.BeginPlay(card, mi); }, 12, playable);
                    Flex(b);
                }
            }
            else
            {
                var b = UiKit.Btn(btnRow, "プレイ", delegate { g.BeginPlay(card, null); }, 12, playable);
                Flex(b);
            }
            var sb = UiKit.Btn(btnRow, "伏せる", delegate { g.DoCombat(new Command_SetCard { CardUid = card.Uid }); }, 12, settable);
            Flex(sb);
        }

        static void Flex(Component c)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le != null) { le.flexibleWidth = 1f; le.minWidth = 20f; }
        }

        // ---- ログ ----

        static void BuildLog(Transform parent, GameState st)
        {
            var pan = UiKit.Pan(parent, UiKit.ColPanel, "logPan");
            UiKit.Stretch(pan.rectTransform, 0f, 0f, 0f, 0f);
            UiKit.Vert(pan.transform, 4, 6);
            var h = UiKit.Txt(pan.transform, "戦闘ログ", 15, UiKit.ColAccent);
            UiKit.Le(h, -1f, 20f, -1f, 20f);

            var content = UiKit.Scroll(pan.transform, true, new Color(0f, 0f, 0f, 0.2f), 2, 6);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 100f, -1f, 100f, -1f, 1f);

            var lines = new List<string>();
            for (int i = 0; i < st.EventLog.Count; i++)
            {
                var s = CardText.LogLine(st.EventLog[i]);
                if (s != null) lines.Add(s);
            }
            int from = Math.Max(0, lines.Count - 40);
            for (int i = from; i < lines.Count; i++)
            {
                var t = UiKit.Txt(content, lines[i], 12, UiKit.ColText);
                UiKit.Le(t, -1f, 15f, -1f, -1f);
            }
        }

        // ---- 確認ウィンドウ (set-confirm) ----

        static void BuildReactionWindow(GameRoot g, Transform body, GameState st)
        {
            var backdrop = UiKit.Pan(body, new Color(0f, 0f, 0f, 0.72f), "backdrop");
            UiKit.Stretch(backdrop.rectTransform, 0f, 0f, 0f, 0f);

            var pan = UiKit.Pan(backdrop.transform, UiKit.ColPanel2, "window");
            UiKit.Anchor(pan.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330f, -200f), new Vector2(330f, 200f));
            UiKit.Vert(pan.transform, 6, 12);

            UiKit.Head(pan.transform, "リアクション: 発動する? 温存する?", 18);

            var win = Effects.WindowFromPending(st);
            var pending = st.PendingWindow;
            if (win == null || pending == null)
            {
                UiKit.Txt(pan.transform, "窓の情報を復元できません", 14, UiKit.ColBad);
                UiKit.Btn(pan.transform, "温存して続ける", delegate { g.DoCombat(new Command_ConfirmReaction { Fire = false }); }, 15);
                return;
            }

            int ei = pending.EnemyIndex;
            string ename = ei >= 0 && ei < st.Enemies.Count ? SafeEnemyName(st.Enemies[ei].EnemyId) : "?";
            var info = UiKit.Txt(pan.transform,
                "敵" + (ei + 1) + " " + ename + " の " + (win.Stage == "pre" ? "被攻撃前 (実行前)" : "被攻撃後 (解決後)") + " 窓\n"
                + "行動: " + CardText.IntentText(st, ei) + "\n実値: " + win.Actual + (win.Stage == "post" ? "  / このHP損失: " + win.HpLoss : ""),
                14, UiKit.ColText);
            UiKit.Le(info, -1f, 66f, -1f, 66f);

            var risks = Effects.SetBranchFlipRisks(st);
            if (risks.Count > 0)
            {
                var buf = new List<string>();
                for (int i = 0; i < risks.Count; i++) buf.Add("敵" + (risks[i] + 1));
                var w = UiKit.Txt(pan.transform, "! 発動すると伏せ枠が空き、" + string.Join("・", buf.ToArray()) + " が「伏せなし」分岐に変わる", 13, UiKit.ColBad);
                UiKit.Le(w, -1f, 34f, -1f, 34f);
            }

            var usable = Effects.UsableSetCards(st, win);
            for (int i = 0; i < usable.Count; i++)
            {
                var c = usable[i];
                string uid = c.Uid;
                var b = UiKit.Btn(pan.transform, "発動: " + c.Def.Name + "  [ " + CardText.Short(CardText.Body(c.Def), 76) + " ]",
                    delegate { g.DoCombat(new Command_ConfirmReaction { Fire = true, CardUid = uid }); }, 14, true, UiKit.Hex("#2f4d33"));
                var le = b.GetComponent<LayoutElement>();
                if (le != null) { le.minHeight = 34f; le.preferredHeight = 34f; }
            }
            if (usable.Count == 0)
            {
                UiKit.Txt(pan.transform, "発動できる伏せ札はありません", 13, UiKit.ColDim);
            }

            var un = Effects.UnaffordableSetCards(st, win);
            if (un.Count > 0)
            {
                var buf = new List<string>();
                for (int i = 0; i < un.Count; i++) buf.Add(un[i].Def.Name);
                UiKit.Txt(pan.transform, "(エナジー不足で発動できない: " + string.Join("、", buf.ToArray()) + ")", 12, UiKit.ColDim);
            }

            UiKit.Btn(pan.transform, "温存する", delegate { g.DoCombat(new Command_ConfirmReaction { Fire = false }); }, 15);
        }

        static string SafeEnemyName(string id)
        {
            try { return Content.GetEnemyDef(id).Name; }
            catch (Exception) { return id; }
        }

        // ---- 対象選択 ----

        static void BuildTargetBanner(GameRoot g, Transform body)
        {
            var pan = UiKit.Pan(body, UiKit.Hex("#3a4a2a"), "targetBanner");
            UiKit.Anchor(pan.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -40f), new Vector2(-(LogWidth + 8f), 0f));
            UiKit.Horz(pan.transform, 8, 6);
            var t = UiKit.Txt(pan.transform, "「" + g.Pending.Card.Def.Name + "」の対象を選んでください (上の敵をクリック)", 15, UiKit.ColText, TextAnchor.MiddleLeft);
            UiKit.Le(t, 100f, -1f, 100f, -1f, 1f, -1f);
            var b = UiKit.Btn(pan.transform, "取り消し", delegate { g.CancelPending(); }, 14);
            var le = b.GetComponent<LayoutElement>();
            if (le != null) { le.preferredWidth = 110f; le.minWidth = 110f; }
        }

        // ---- 追加コスト・選択のピッカー ----

        public static IReadOnlyList<CardInstance> DeckChoosePool(GameState st, string kind)
        {
            if (kind == "retrieveFromDiscard") return st.Player.DiscardPile;
            if (kind == "searchDeck") return st.Player.DrawPile;
            var all = new List<CardInstance>();
            for (int i = 0; i < st.Player.DrawPile.Count; i++) all.Add(st.Player.DrawPile[i]);
            for (int i = 0; i < st.Player.DiscardPile.Count; i++) all.Add(st.Player.DiscardPile[i]);
            return all;
        }

        public static List<CardInstance> UpgradablePool(GameState st, CardInstance self)
        {
            var list = new List<CardInstance>();
            for (int i = 0; i < st.Player.Hand.Count; i++)
            {
                var c = st.Player.Hand[i];
                if (c.Uid == self.Uid) continue;
                if (Upgrade.CanUpgradeInHand(c)) list.Add(c);
            }
            return list;
        }

        static void BuildPicker(GameRoot g, Transform body, GameState st, string need)
        {
            var p = g.Pending;
            IReadOnlyList<CardInstance> pool;
            List<string> selected;
            int want;
            string title;

            if (need == "discard")
            {
                pool = ExceptSelf(st.Player.Hand, p.Card.Uid);
                selected = p.Discard; want = p.DiscardNeed;
                title = "追加コスト: 手札を" + want + "枚捨てる";
            }
            else if (need == "exhaust")
            {
                pool = ExceptSelf(st.Player.Hand, p.Card.Uid);
                selected = p.Exhaust; want = p.ExhaustNeed;
                title = "追加コスト: 手札を" + want + "枚消滅させる";
            }
            else if (need == "retrieve")
            {
                pool = st.Player.ExhaustPile;
                selected = new List<string>(); want = 1;
                title = "消滅置き場から1枚選ぶ";
            }
            else if (need == "deck")
            {
                pool = DeckChoosePool(st, p.DeckKind);
                selected = p.DeckSel; want = p.DeckNeed;
                title = (p.DeckKind == "searchDeck" ? "山札" : p.DeckKind == "retrieveFromDiscard" ? "捨て札" : "山札か捨て札") + "から" + want + "枚選ぶ";
            }
            else if (need == "hand")
            {
                pool = UpgradablePool(st, p.Card);
                selected = p.HandSel; want = p.HandNeed;
                title = "手札から" + want + "枚を鍛える";
            }
            else if (need == "permanent")
            {
                var retainers = new List<CardInstance>();
                for (int i = 0; i < st.Player.Permanents.Count; i++)
                {
                    var q = st.Player.Permanents[i];
                    if (q.Def.Retainer == true && q.Innate != true) retainers.Add(q);
                }
                pool = retainers;
                selected = new List<string>(); want = 1;
                title = "破壊する従者を選ぶ";
            }
            else
            {
                UiKit.Txt(body, "未対応の選択: " + need, 16, UiKit.ColBad);
                return;
            }

            var backdrop = UiKit.Pan(body, new Color(0f, 0f, 0f, 0.75f), "pickBackdrop");
            UiKit.Stretch(backdrop.rectTransform, 0f, 0f, 0f, 0f);
            var pan = UiKit.Pan(backdrop.transform, UiKit.ColPanel2, "picker");
            UiKit.Anchor(pan.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320f, -230f), new Vector2(320f, 230f));
            UiKit.Vert(pan.transform, 6, 12);

            UiKit.Head(pan.transform, p.Card.Def.Name + " — " + title + " (選択中 " + selected.Count + "/" + want + ")", 17);

            var content = UiKit.Scroll(pan.transform, true, new Color(0f, 0f, 0f, 0.25f), 3, 6);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 120f, -1f, 120f, -1f, 1f);

            if (pool.Count == 0)
            {
                UiKit.Txt(content, "(候補がありません)", 13, UiKit.ColDim);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                string uid = c.Uid;
                bool isSel = selected.Contains(uid);
                string label = (isSel ? "[選択中] " : "") + "[" + CardText.CostLabel(c.Def) + "] " + c.Def.Name + " — " + CardText.Short(CardText.Body(c.Def), 58);
                string cap = need;
                var b = UiKit.Btn(content, label, delegate { OnPick(g, cap, uid); }, 12, true, isSel ? UiKit.Hex("#3f6b45") : UiKit.ColPanel);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) { le.minHeight = 28f; le.preferredHeight = 28f; }
            }

            var row = UiKit.NewRect("pickerBtns", pan.transform);
            UiKit.Le(row, -1f, 34f, -1f, 34f);
            var hg = UiKit.Horz(row, 8, 0);
            hg.childForceExpandWidth = true;
            var cancel = UiKit.Btn(row, "取り消し", delegate { g.CancelPending(); }, 14);
            Flex(cancel);
        }

        static void OnPick(GameRoot g, string need, string uid)
        {
            var p = g.Pending;
            if (p == null) return;
            if (need == "retrieve") { p.RetrieveUid = uid; g.SubmitIfReady(); return; }
            if (need == "permanent") { p.PermanentUid = uid; g.SubmitIfReady(); return; }
            List<string> list = need == "discard" ? p.Discard : need == "exhaust" ? p.Exhaust : need == "deck" ? p.DeckSel : p.HandSel;
            if (list.Contains(uid)) list.Remove(uid);
            else list.Add(uid);
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
