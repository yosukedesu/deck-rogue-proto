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
            var art = Theme.Art("bg", "act" + act);
            var bg = UiKit.Pan(root, Theme.Bg, "bg");
            UiKit.Stretch(bg.rectTransform, 0f, 0f, 0f, 0f);
            bg.raycastTarget = false;
            if (art != null)
            {
                bg.sprite = art;
                bg.color = Color.white;
                bg.preserveAspect = false;
                return;
            }
            // プレースホルダー: 上が暗い夜空 (グラデーション)、下が地面の帯、周辺はビネットで落とす (幕ごとに色相を変える)
            float hue = act == 1 ? 0.36f : act == 2 ? 0.55f : 0.02f;
            var sky = UiKit.Pan(root, Color.white, "sky");
            sky.sprite = ThemeFx.Gradient("sky" + act, Color.HSVToRGB(hue, 0.45f, 0.16f), Color.HSVToRGB(hue, 0.35f, 0.06f));
            UiKit.Anchor(sky.rectTransform, new Vector2(0f, 0.34f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            sky.raycastTarget = false;
            var ground = UiKit.Pan(root, Color.white, "ground");
            ground.sprite = ThemeFx.Gradient("ground" + act, Color.HSVToRGB(hue, 0.4f, 0.2f), Color.HSVToRGB(hue, 0.45f, 0.09f));
            UiKit.Anchor(ground.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.34f), Vector2.zero, Vector2.zero);
            ground.raycastTarget = false;
            var horizon = UiKit.Pan(root, Color.HSVToRGB(hue, 0.3f, 0.3f), "horizon");
            UiKit.Anchor(horizon.rectTransform, new Vector2(0f, 0.34f), new Vector2(1f, 0.34f), new Vector2(0f, -3f), new Vector2(0f, 3f));
            horizon.raycastTarget = false;
            var vig = UiKit.Pan(root, Color.white, "vignette");
            vig.sprite = ThemeFx.Vignette();
            UiKit.Stretch(vig.rectTransform, 0f, 0f, 0f, 0f);
            vig.raycastTarget = false;
        }

        // ---- 上部バー ----

        static void BuildTopBar(GameRoot g, RectTransform root, RunState run, GameState st)
        {
            var bar = UiKit.Pan(root, new Color(0f, 0f, 0f, 0.55f), "topbar");
            UiKit.Anchor(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -TopH), new Vector2(0f, 0f));
            bar.raycastTarget = false;

            string enc = "";
            try
            {
                var node = DeckRogue.Engine.Run.CurrentNode(run);
                if (node != null && node.EncounterId != null) enc = Content.EncounterName(node.EncounterId);
            }
            catch (Exception) { }
            var left = UiKit.Txt(bar.transform, "幕" + run.Act + "  行" + (run.Row + 1) + "   " + enc + "   ターン " + st.Turn, 22, UiKit.ColText, TextAnchor.MiddleLeft, true);
            UiKit.Anchor(left.rectTransform, new Vector2(0f, 0f), new Vector2(0.4f, 1f), new Vector2(24f, 0f), new Vector2(0f, 0f));

            // 中央: 資源
            var mid = UiKit.NewRect("res", bar.transform);
            UiKit.Anchor(mid, new Vector2(0.4f, 0f), new Vector2(0.72f, 1f), Vector2.zero, Vector2.zero);
            var hg = UiKit.Horz(mid, 18, 0);
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childForceExpandHeight = false;
            var p = st.Player;
            Chip(mid, "heart", p.Hp + " / " + p.MaxHp, UiKit.ColHp, 24);
            Chip(mid, "gold", run.Gold + " G", Theme.Gold, 22);

            // 右: レリック・デッキ・ログ
            var right = UiKit.NewRect("right", bar.transform);
            UiKit.Anchor(right, new Vector2(0.72f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-16f, 0f));
            var rg = UiKit.Horz(right, 10, 0);
            rg.childAlignment = TextAnchor.MiddleRight;
            rg.childForceExpandHeight = false;
            var relicNames = new List<string>();
            for (int i = 0; i < run.Relics.Count; i++)
            {
                try { var rd = Content.GetRelicDef(run.Relics[i]); relicNames.Add((rd.Sprite ?? "") + rd.Name); }
                catch (Exception) { relicNames.Add(run.Relics[i]); }
            }
            var relT = UiKit.Txt(right, relicNames.Count > 0 ? string.Join("  ", relicNames.ToArray()) : "レリックなし", 15, UiKit.ColDim, TextAnchor.MiddleRight);
            relT.textWrappingMode = TextWrappingModes.NoWrap;
            relT.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Le(relT, 100f, 30f, 360f, 30f, 1f, -1f);
            var logBtn = UiKit.Btn(right, g.ShowLog ? "ログを閉じる" : "ログ", delegate { g.ShowLog = !g.ShowLog; g.Rebuild(); }, 15);
            SetSize(logBtn, 130f, 44f);

            // エラー・通知は上部バーの下に赤で
            string msg = g.Error != null ? "! " + g.Error : (g.Notice != null ? g.Notice : null);
            if (msg != null)
            {
                var m = UiKit.Txt(root, msg, 18, g.Error != null ? UiKit.ColBad : UiKit.ColEnergy, TextAnchor.MiddleCenter, true);
                m.outlineWidth = 0.2f; m.outlineColor = Color.black;
                UiKit.Anchor(m.rectTransform, new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0f, -TopH - 40f), new Vector2(0f, -TopH - 4f));
            }
        }

        static void Chip(Transform parent, string icon, string text, Color color, int size)
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
            if (targeting && alive)
            {
                var glow = UiKit.Frame(pan, Theme.Panel, new Color(1f, 0.85f, 0.3f, 0.5f), "glow", 3f);
                UiKit.Anchor(glow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-124f, 118f), new Vector2(124f, 346f));
                glow.raycastTarget = false;
            }
            g.RegisterAnchor("enemy" + index, pan);

            EnemyDef def = null;
            try { def = Content.GetEnemyDef(e.EnemyId); } catch (Exception) { }
            string nm = def != null ? def.Name : e.EnemyId;

            // 意図 (頭上)
            if (alive)
            {
                var it = e.Intent;
                var intentRow = UiKit.NewRect("intent", pan);
                UiKit.Anchor(intentRow, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 400f), new Vector2(0f, 456f));
                var ig = UiKit.Horz(intentRow, 8, 0);
                ig.childAlignment = TextAnchor.MiddleCenter;
                ig.childForceExpandHeight = false;
                if (it != null)
                {
                    UiKit.Icon(intentRow, IntentIcon(it.Kind), 48, IntentColor(it.Kind));
                    var itT = UiKit.Txt(intentRow, IntentShort(it), 26, IntentColor(it.Kind), TextAnchor.MiddleLeft, true);
                    itT.outlineWidth = 0.2f; itT.outlineColor = Color.black;
                    UiKit.Le(itT, 40f, 40f, -1f, 40f);
                }
                // 分岐・付与などの詳細は小さく
                var detailText = IntentDetail(st, index, it);
                var detail = UiKit.Txt(pan, detailText, 14, UiKit.ColEnergy, TextAnchor.LowerCenter);
                UiKit.Anchor(detail.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 340f), new Vector2(20f, 398f));
            }

            // 足元の影とスプライト
            var sh = UiKit.NewRect("shadow", pan);
            UiKit.Anchor(sh, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-96f, 118f), new Vector2(96f, 148f));
            var shImg = sh.gameObject.AddComponent<Image>();
            shImg.sprite = ThemeFx.Shadow();
            shImg.raycastTarget = false;
            shImg.color = alive ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            var spr = UiKit.NewRect("sprite", pan);
            UiKit.Anchor(spr, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-100f, 130f), new Vector2(100f, 330f));
            var img = spr.gameObject.AddComponent<Image>();
            img.sprite = Creature.Get("enemies", e.EnemyId);
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = alive ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            if (aimed)
            {
                var ring = UiKit.Pan(pan, new Color(1f, 0.85f, 0.3f, 0.35f), "ring");
                UiKit.Anchor(ring.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-112f, 124f), new Vector2(112f, 130f));
                ring.raycastTarget = false;
            }

            // 名前・HP
            var nameT = UiKit.Txt(pan, nm + (alive ? "" : (e.Fled == true ? "（逃走）" : "（撃破）")), 20, alive ? UiKit.ColText : UiKit.ColDim, TextAnchor.MiddleCenter, true);
            nameT.outlineWidth = 0.18f; nameT.outlineColor = Color.black;
            UiKit.Anchor(nameT.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 92f), new Vector2(0f, 122f));
            HpBar(pan, new Vector2(0.08f, 0f), new Vector2(0.92f, 0f), 62f, 88f, shownHp, e.MaxHp, e.Block);
            if (shownHp != e.Hp) TweenHpBar(pan, e.Hp);

            // 状態チップ
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
            UiKit.Anchor(chipRow, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 26f), new Vector2(0f, 58f));
            var cg = UiKit.Horz(chipRow, 6, 0);
            cg.childAlignment = TextAnchor.MiddleCenter;
            cg.childForceExpandHeight = false;
            for (int i = 0; i < chips.Count; i++) SmallChip(chipRow, chips[i].Key, chips[i].Value, UiKit.ColText);
            if (traits.Length > 0)
            {
                var tr = UiKit.Txt(pan, traits, 12, UiKit.ColDim, TextAnchor.UpperCenter);
                UiKit.Anchor(tr.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-10f, 0f), new Vector2(10f, 24f));
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
            var row = UiKit.NewRect("chip", parent);
            var bg = row.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.5f);
            bg.raycastTarget = true;
            Tooltip.Attach(row.gameObject, delegate { return text; });
            var hg = UiKit.Horz(row, 3, 4);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandHeight = false;
            UiKit.Icon(row, icon, 32);
            var t = UiKit.Txt(row, text, 14, color, TextAnchor.MiddleLeft, true);
            UiKit.Le(t, 20f, 24f, -1f, 24f);
            var fit = row.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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
            var bg = bar.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            bg.raycastTarget = false;
            float r = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
            var fill = UiKit.NewRect("fill", bar);
            var fimg = fill.gameObject.AddComponent<Image>();
            fimg.color = UiKit.ColHp;
            fimg.raycastTarget = false;
            UiKit.Anchor(fill, new Vector2(0f, 0f), new Vector2(r, 1f), new Vector2(3f, 3f), new Vector2(0f, -3f));
            var t = UiKit.Txt(bar, hp + " / " + max, 16, Color.white, TextAnchor.MiddleCenter, true);
            t.outlineWidth = 0.2f; t.outlineColor = Color.black;
            UiKit.Stretch(t.rectTransform, 0f, 0f, 0f, 0f);
            var info = bar.gameObject.AddComponent<HpBarInfo>();
            info.Max = max; info.Value = hp; info.Fill = fill; info.Label = t;
            if (block > 0)
            {
                var b = UiKit.NewRect("block", bar);
                UiKit.Anchor(b, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-30f, -22f), new Vector2(14f, 22f));
                var bi = b.gameObject.AddComponent<Image>();
                bi.sprite = Theme.Icon("shield");
                bi.preserveAspect = true;
                bi.raycastTarget = false;
                var bt = UiKit.Txt(b, block.ToString(), 17, Color.white, TextAnchor.MiddleCenter, true);
                bt.outlineWidth = 0.25f; bt.outlineColor = Color.black;
                UiKit.Stretch(bt.rectTransform, 0f, 0f, 0f, 0f);
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

            var lsh = UiKit.NewRect("shadow", area);
            UiKit.Anchor(lsh, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 118f), new Vector2(250f, 150f));
            var lshImg = lsh.gameObject.AddComponent<Image>();
            lshImg.sprite = ThemeFx.Shadow();
            lshImg.raycastTarget = false;
            var spr = UiKit.NewRect("sprite", area);
            UiKit.Anchor(spr, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 130f), new Vector2(250f, 370f));
            var img = spr.gameObject.AddComponent<Image>();
            img.sprite = Creature.Get("leaders", leaderId, true);
            img.preserveAspect = true;
            img.raycastTarget = false;

            var nameT = UiKit.Txt(area, leaderName, 20, UiKit.ColText, TextAnchor.MiddleCenter, true);
            nameT.outlineWidth = 0.18f; nameT.outlineColor = Color.black;
            UiKit.Anchor(nameT.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 92f), new Vector2(260f, 122f));
            var hpRt = UiKit.NewRect("hpwrap", area);
            UiKit.Anchor(hpRt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 62f), new Vector2(250f, 88f));
            HpBar(hpRt, Vector2.zero, Vector2.one, 0f, 0f, shownHp, p.MaxHp, p.Block);
            if (shownHp != p.Hp) TweenHpBar(area, p.Hp);
            if (p.IceBlock > 0)
            {
                var ice = UiKit.Txt(area, "氷壁 " + p.IceBlock, 15, UiKit.Hex("#9fd8ff"), TextAnchor.MiddleLeft, true);
                UiKit.Anchor(ice.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(236f, 62f), new Vector2(380f, 88f));
            }

            // 資源・状態のチップ (右側に縦積み)
            var col = UiKit.NewRect("chips", area);
            UiKit.Anchor(col, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 26f), new Vector2(0f, 58f));
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
            for (int i = 0; i < res.Count; i++) SmallChip(col, res[i].Key, res[i].Value, i >= 5 && res[i].Value.StartsWith("弱") || res[i].Value.StartsWith("脆") || res[i].Value.StartsWith("虚") || res[i].Value.StartsWith("拘") || res[i].Value.StartsWith("霞") || res[i].Value.StartsWith("重") ? UiKit.ColBad : UiKit.ColText);

            // 伏せ場 (リーダーの右上)
            var setArea = UiKit.NewRect("setzone", area);
            UiKit.Anchor(setArea, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(300f, 110f), new Vector2(0f, 290f));
            var setLabel = UiKit.Txt(setArea, "伏せ場 " + p.SetCards.Count + " / " + p.SetSlots, 15, UiKit.ColAccent, TextAnchor.UpperLeft, true);
            UiKit.Anchor(setLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -22f), new Vector2(0f, 0f));
            for (int i = 0; i < p.SetSlots; i++)
            {
                var slot = UiKit.NewRect("slot" + i, setArea);
                UiKit.Anchor(slot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(i * 120f, 0f), new Vector2(i * 120f + 104f, 150f));
                g.RegisterAnchor("setslot" + i, slot);
                var sImg = UiKit.Frame(slot, Theme.Panel, i < p.SetCards.Count ? UiKit.Hex("#3f8c86") : new Color(1f, 1f, 1f, 0.35f), "slotframe", 3f);
                UiKit.Stretch(sImg.rectTransform, 0f, 0f, 0f, 0f);
                sImg.raycastTarget = false;
                if (i < p.SetCards.Count)
                {
                    var sc = p.SetCards[i];
                    var ct = UiKit.Txt(slot, sc.Def.Name, 14, UiKit.ColText, TextAnchor.MiddleCenter, true);
                    UiKit.Anchor(ct.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(6f, 40f), new Vector2(-6f, -6f));
                    var body = UiKit.Txt(slot, CardText.Short(CardText.Body(sc.Def), 30), 11, UiKit.ColDim, TextAnchor.UpperCenter);
                    UiKit.Anchor(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(6f, 4f), new Vector2(-6f, 44f));
                }
                else
                {
                    var et = UiKit.Txt(slot, "空き", 13, UiKit.ColDim, TextAnchor.MiddleCenter);
                    UiKit.Stretch(et.rectTransform, 0f, 0f, 0f, 0f);
                }
            }

            // 置物 (伏せ場の右)
            var perms = new List<string>();
            for (int i = 0; i < p.Permanents.Count; i++)
            {
                var q = p.Permanents[i];
                if (q.Innate == true) continue;
                perms.Add(q.Def.Name);
            }
            var permT = UiKit.Txt(setArea, perms.Count > 0 ? "置物: " + string.Join("・", perms.ToArray()) : "", 14, UiKit.ColText, TextAnchor.UpperLeft);
            UiKit.Anchor(permT.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(p.SetSlots * 120f + 10f, 0f), new Vector2(0f, 150f));
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
            Pile(g, root, new Vector2(1f, 0f), new Vector2(-150f, 24f), "exhaust", "捨て札 / 消滅", p.DiscardPile.Count, p.ExhaustPile.Count, delegate { g.ViewPile = "discard"; g.Rebuild(); }, "pile-discard");
            if (g.ViewPile != null) BuildPileViewer(g, root, st);
        }

        static void Pile(GameRoot g, RectTransform root, Vector2 anchor, Vector2 offset, string icon, string label, int count, int count2 = -1, Action onClick = null, string anchorName = null)
        {
            var rt = UiKit.NewRect("pile-" + icon, root);
            if (anchorName != null) g.RegisterAnchor(anchorName, rt);
            if (onClick != null)
            {
                var hit = rt.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                var pb = rt.gameObject.AddComponent<Button>();
                pb.targetGraphic = hit;
                pb.transition = Selectable.Transition.None;
                pb.onClick.AddListener(delegate { onClick(); });
            }
            UiKit.Anchor(rt, anchor, anchor, offset, offset + new Vector2(126f, 96f));
            var frame = UiKit.Frame(rt, Theme.Panel, Color.white, "frame", 3f);
            UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            frame.raycastTarget = false;
            var ic = UiKit.Icon(rt, icon, 36);
            UiKit.Anchor(ic.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -48f), new Vector2(48f, -12f));
            var cnt = UiKit.Txt(rt, count2 >= 0 ? count + " / " + count2 : count.ToString(), 24, UiKit.ColText, TextAnchor.MiddleLeft, true);
            UiKit.Anchor(cnt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(54f, -52f), new Vector2(-6f, -8f));
            var lb = UiKit.Txt(rt, label, 13, UiKit.ColDim, TextAnchor.LowerLeft);
            UiKit.Anchor(lb.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(12f, 8f), new Vector2(-6f, 34f));
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
                var b = UiKit.Btn(tabs, labels[i], delegate { g.ViewPile = k; g.Rebuild(); }, 18, true, g.ViewPile == k ? UiKit.Hex("#cfeacc") : Color.white);
                SetSize(b, 200f, 44f);
            }
            IReadOnlyList<CardInstance> pile = g.ViewPile == "discard" ? st.Player.DiscardPile : g.ViewPile == "exhaust" ? st.Player.ExhaustPile : st.Player.DrawPile;
            var list = new List<CardInstance>(pile);
            if (g.ViewPile == "draw") list.Sort((a, b) => string.CompareOrdinal(a.Def.Name, b.Def.Name)); // 引き順は伏せたまま
            var content = UiKit.Scroll(inner, true, new Color(0f, 0f, 0f, 0.25f), 12, 12);
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
                var none = UiKit.Txt(inner, "（空）", 18, UiKit.ColDim, TextAnchor.MiddleCenter);
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
            var b = UiKit.Btn(root, "ターン終了", delegate { g.DoCombat(new Command_EndTurn()); }, 24, myTurn, myTurn ? UiKit.Hex("#cfeacc") : Color.white);
            var le = b.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-300f, 136f), new Vector2(-24f, 216f));
            // エナジー玉 (手札の左)
            var orb = UiKit.NewRect("energyOrb", root);
            UiKit.Anchor(orb, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(190f, 150f), new Vector2(290f, 250f));
            var orbImg = orb.gameObject.AddComponent<Image>();
            orbImg.sprite = ThemeFx.CostOrb();
            orbImg.preserveAspect = true;
            orbImg.raycastTarget = false;
            orbImg.color = st.Player.Energy > 0 ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
            var et = UiKit.Txt(orb, st.Player.Energy + "/" + st.Player.EnergyMax, 30, Color.white, TextAnchor.MiddleCenter, true);
            et.outlineWidth = 0.3f; et.outlineColor = Color.black;
            UiKit.Stretch(et.rectTransform, 0f, 0f, 0f, 0f);
            var el = UiKit.Txt(root, "エナジー", 14, UiKit.ColDim, TextAnchor.MiddleCenter);
            UiKit.Anchor(el.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(170f, 126f), new Vector2(310f, 150f));
            g.RegisterAnchor("energy", orb);
        }

        // ---- ログの引き出し ----

        static void BuildLogDrawer(GameRoot g, RectTransform root, GameState st)
        {
            var pan = UiKit.Pan(root, new Color(0.05f, 0.07f, 0.06f, 0.94f), "logDrawer");
            UiKit.Anchor(pan.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-520f, 260f), new Vector2(0f, -TopH));
            UiKit.Vert(pan.transform, 4, 12);
            UiKit.Head(pan.transform, "戦闘ログ", 20);
            var content = UiKit.Scroll(pan.transform, true, new Color(0f, 0f, 0f, 0.25f), 2, 8);
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
                var t = UiKit.Txt(content, lines[i], 15, UiKit.ColText);
                UiKit.Le(t, -1f, 20f, -1f, -1f);
            }
        }

        // ---- モーダル: 確認ウィンドウ (set-confirm) ----

        public static RectTransform Modal(RectTransform root, float w, float h, string name)
        {
            var backdrop = UiKit.Pan(root, new Color(0f, 0f, 0f, 0.7f), name + "-backdrop");
            UiKit.Stretch(backdrop.rectTransform, 0f, 0f, 0f, 0f);
            var win = UiKit.Frame(backdrop.transform, Theme.Panel, Color.white, name, 3f);
            UiKit.Anchor(win.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-w / 2f, -h / 2f), new Vector2(w / 2f, h / 2f));
            var inner = UiKit.NewRect("inner", win.transform);
            UiKit.Stretch(inner, 24f, 24f, 20f, 20f);
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
                18, UiKit.ColText);
            UiKit.Le(info, -1f, 96f, -1f, 96f);
            var risks = Effects.SetBranchFlipRisks(st);
            if (risks.Count > 0)
            {
                var buf = new List<string>();
                for (int i = 0; i < risks.Count; i++) buf.Add((risks[i] + 1).ToString());
                var w = UiKit.Txt(inner, "⚠ 発動すると伏せ枠が空き、敵 " + string.Join("・", buf.ToArray()) + " が「伏せなし」の分岐に変わる", 15, UiKit.ColEnergy);
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
                var fb = UiKit.Btn(wrap, "発動", delegate { g.DoCombat(new Command_ConfirmReaction { Fire = true, CardUid = uid }); }, 18, true, UiKit.Hex("#cfeacc"));
                var fle = fb.GetComponent<LayoutElement>();
                if (fle != null) UnityEngine.Object.Destroy(fle);
                UiKit.Anchor(fb.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-70f, 0f), new Vector2(70f, 44f));
            }
            if (usable.Count == 0)
            {
                var none = UiKit.Txt(row, "発動できる伏せ札はありません", 16, UiKit.ColDim, TextAnchor.MiddleCenter);
                UiKit.Le(none, 400f, 40f, 400f, 40f);
            }
            var un = Effects.UnaffordableSetCards(st, win);
            if (un.Count > 0)
            {
                var buf = new List<string>();
                for (int i = 0; i < un.Count; i++) buf.Add(un[i].Def.Name);
                var ut = UiKit.Txt(inner, "エナジー不足で発動できない: " + string.Join("、", buf.ToArray()), 14, UiKit.ColDim);
                UiKit.Le(ut, -1f, 24f, -1f, 24f);
            }
            CenteredButton(inner, "温存する", delegate { g.DoCombat(new Command_ConfirmReaction { Fire = false }); }, 20, 320f, 56f);
        }

        // ---- 対象選択・モード選択 ----

        static void BuildTargetBanner(GameRoot g, RectTransform root)
        {
            var pan = UiKit.Pan(root, UiKit.Hex("#b8862c"), "targetBanner");
            UiKit.Anchor(pan.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-420f, -TopH - 70f), new Vector2(420f, -TopH - 10f));
            var t = UiKit.Txt(pan.transform, "「" + g.Pending.Card.Def.Name + "」の対象を選ぶ — 敵をクリック（またはカードを敵へドラッグ）", 20, Color.white, TextAnchor.MiddleLeft, true);
            t.outlineWidth = 0.2f; t.outlineColor = Color.black;
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
            var title = UiKit.Txt(inner, card.Def.Name + ":", 18, UiKit.ColText, TextAnchor.MiddleLeft, true);
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
            var content = UiKit.Scroll(inner, false, new Color(0f, 0f, 0f, 0.25f), 16, 12);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 360f, -1f, 360f, -1f, 1f);
            var lg = content.GetComponent<HorizontalLayoutGroup>();
            if (lg != null) { lg.childForceExpandWidth = false; lg.childForceExpandHeight = false; lg.childAlignment = TextAnchor.MiddleLeft; }
            if (pool.Count == 0)
            {
                var none = UiKit.Txt(content, "（候補がありません）", 16, UiKit.ColDim);
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
                var pick = UiKit.Btn(wrap, isSel ? "選択中" : "選ぶ", delegate { OnPick(g, cap, uid); }, 16, true, isSel ? UiKit.Hex("#cfeacc") : Color.white);
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
