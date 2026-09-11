// CardView.cs — カードの面 (200×290)。「絵本」の肌の上に案B「本家型の帯」(2026-09-09 デザインカンバス第2版・ユーザー裁定):
// 左上のコスト玉 (割引は苔色・X)、全幅の夜色の窓に挿絵 (Art/cards/<id>.png 80×48 を2倍。無ければタイプの紋章)、
// 窓の下端に掛かるタイプの帯 (色＋文字＋レア度の宝石)、本文は墨で数字は 130%、紙の外側の線の色がレア度 (C 墨・U 空・R 蜂蜜)。
// 旧・第5版の「左下の剣・右下の盾のにじみの札」は廃止 (数字は本文の1か所)。RoleLabels は効果からの役割抽出として残す。
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class CardView
    {
        public const float W = 200f;
        public const float H = 290f;

        /// <summary>予測行 (対象が決まっている時の実値) の対象。-1 で出さない</summary>
        public static int PreviewEnemy = -1;

        public static RectTransform Build(Transform parent, CardInstance c, GameState st, bool playable, bool interactable, string name = "card")
        {
            var root = UiKit.NewRect(name, parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(W, H);
            Fill(root, c, st, playable, interactable);
            return root;
        }

        /// <summary>既にある札の面を描き直す (ドラッグ中に対象の敵が変わった時など。根は残すので進行中のドラッグは切れない)</summary>
        public static void Refill(RectTransform root, CardInstance c, GameState st, bool playable, bool interactable)
        {
            for (int i = root.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
            Fill(root, c, st, playable, interactable);
        }

        static void Fill(RectTransform root, CardInstance c, GameState st, bool playable, bool interactable)
        {
            var def = c.Def;
            var typeCol = PaperFx.TypeColor(def.Type);
            var ink = playable ? PaperFx.Ink : new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.7f);
            string rarity = def.Rarity ?? "common";

            // 紙 (外側の線の色がレア度: C 墨・U 空・R 蜂蜜)
            var paper = PaperFx.Sheet(root, PaperFx.CardOf(rarity), "paper", playable ? Color.white : new Color(0.82f, 0.8f, 0.76f, 1f));
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = interactable;
            var grain = PaperFx.GrainOver(root, 0.7f);
            grain.raycastTarget = false;

            // 挿絵の窓 (全幅 176×100。夜色に墨の縁。80×48 を2倍で中央に)
            var win = UiKit.NewRect("art", root);
            UiKit.Anchor(win, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -144f), new Vector2(-12f, -44f));
            var winEdge = win.gameObject.AddComponent<Image>();
            winEdge.color = PaperFx.Ink;
            winEdge.raycastTarget = false;
            var winIn = UiKit.Pan(win, UiKit.Hex("#20233a"), "night");
            UiKit.Stretch(winIn.rectTransform, 1.5f, 1.5f, 1.5f, 1.5f);
            winIn.raycastTarget = false;
            var art = Theme.Art("cards", def.Id) ?? ThemeFx.FusedArt(def.Id);   // 工房産は素材2枚の絵を溶かし合わせる (2026-09-11)
            if (art != null)
            {
                // 80×48 を2倍 (整数倍)。それ以外の寸法でも縦横比を保って収める
                var ai = UiKit.NewRect("pic", win);
                ai.anchorMin = ai.anchorMax = new Vector2(0.5f, 0.5f);
                ai.sizeDelta = new Vector2(Mathf.Min(160f, art.rect.width * 2f), Mathf.Min(96f, art.rect.height * 2f));
                var aimg = ai.gameObject.AddComponent<Image>();
                aimg.sprite = art; aimg.preserveAspect = true; aimg.raycastTarget = false;
                aimg.color = playable ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
            }
            else
            {
                var wash = UiKit.Pan(win, new Color(typeCol.r, typeCol.g, typeCol.b, 0.3f), "wash");
                UiKit.Stretch(wash.rectTransform, 1.5f, 1.5f, 1.5f, 1.5f);
                wash.raycastTarget = false;
                var crestRt = UiKit.NewRect("crest", win);
                crestRt.anchorMin = crestRt.anchorMax = new Vector2(0.5f, 0.5f);
                crestRt.sizeDelta = Theme.HasIconArt("crest_" + def.Type) ? new Vector2(64f, 64f) : new Vector2(48f, 48f);   // 絵は 32 ドット×2
                crestRt.anchoredPosition = Vector2.zero;
                var crest = crestRt.gameObject.AddComponent<Image>();
                crest.sprite = Theme.Icon("crest_" + def.Type);
                crest.preserveAspect = true;
                crest.raycastTarget = false;
                crest.color = playable ? Color.white : new Color(1f, 1f, 1f, 0.7f);
            }

            // コスト玉 (左上に少しはみ出す)。表示は実際に払う量: 割引で下がれば緑、重圧で上がれば朱の数字 (本家の読み方。2026-09-09)
            int cost = def.Cost;
            try { if (st != null) cost = Effects.EffectiveCost(st, c); } catch (Exception) { }
            bool discounted = def.XCost != true && cost < def.Cost;
            bool raised = def.XCost != true && cost > def.Cost;
            string costLabel = def.XCost == true ? "X" : cost.ToString();
            var orb = UiKit.NewRect("cost", root);
            UiKit.Anchor(orb, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-6f, -46f), new Vector2(46f, 6f));
            var orbImg = orb.gameObject.AddComponent<Image>();
            var orbArt = Theme.Art("ui", "cost_orb");   // PixelLab の玉 (26 ドット×2=52。2026-09-11)。無ければ水彩の玉
            orbImg.sprite = orbArt != null ? orbArt : PaperFx.Orb(PaperFx.Honey);
            orbImg.preserveAspect = true; orbImg.raycastTarget = false;
            if (!playable) orbImg.color = new Color(0.82f, 0.82f, 0.82f, 1f);
            var costT = UiKit.Deco(orb, costLabel, 22, discounted ? UiKit.Hex("#276a34") : raised ? UiKit.Hex("#a33a30") : ink, TextAnchor.MiddleCenter);
            UiKit.Anchor(costT.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0f, -1f));
            costT.characterSpacing = 0f;

            // 名前 (装飾明朝)
            int nameSize = def.Name.Length > 5 ? 17 : (def.Name.Length > 4 ? 18 : 20);
            var nameT = UiKit.Deco(root, def.Name, nameSize, ink, TextAnchor.MiddleCenter);
            UiKit.Anchor(nameT.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -40f), new Vector2(-12f, -8f));
            nameT.textWrappingMode = TextWrappingModes.NoWrap;
            nameT.overflowMode = TextOverflowModes.Overflow;

            // タイプの帯 (窓の下端に掛ける。色と文字で 物理／呪文／リアクション／置物、宝石がレア度)
            var ribbon = UiKit.NewRect("type", root);
            UiKit.Anchor(ribbon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-75f, -162f), new Vector2(75f, -136f));
            var rImg = ribbon.gameObject.AddComponent<Image>();
            rImg.sprite = PaperFx.Ribbon(playable ? typeCol : Color.Lerp(typeCol, Color.gray, 0.5f));
            rImg.raycastTarget = false;
            var row = UiKit.NewRect("row", ribbon);
            UiKit.Stretch(row, 0f, 0f, 0f, 0f);
            var hg = UiKit.Horz(row, 5, 0);
            hg.childAlignment = TextAnchor.MiddleCenter; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            var gem = UiKit.NewRect("gem", row);
            var gImg = gem.gameObject.AddComponent<Image>();
            gImg.sprite = ThemeFx.Gem(rarity); gImg.preserveAspect = true; gImg.raycastTarget = false;
            float gemSz = Theme.Art("ui", "gem_" + rarity) != null ? 24f : 16f;   // PixelLab の宝石は 12 ドット×2 (2026-09-11)
            UiKit.Le(gem, gemSz, gemSz, gemSz, gemSz);
            var typeT = UiKit.Txt(row, CardText.TypeJa(def.Type), 14, PaperFx.Ink, TextAnchor.MiddleCenter, true);
            typeT.characterSpacing = 2f;
            UiKit.Le(typeT, -1f, 22f, -1f, 22f);

            // 本文 (墨。数字は 130%)。戦闘中は成長・勢い・弱体、狙った敵の急所・装甲を掛けた実値を色つきで (本家のカードの数字の読み方)
            Func<int, int> mod = st != null ? MakeDamageModifier(st, c) : null;
            CardText.DamageModifier = mod;
            string bodyText;
            try { bodyText = CardText.Body(def); }
            finally { CardText.DamageModifier = null; }
            if (def.Modes != null && def.Modes.Count > 0) bodyText = "<size=80%><color=#574b48>どちらか一つ</color></size>\n" + bodyText;
            // 追加コスト (X・捨て・消滅コスト・0E 条件) は本文の先頭に普通の表記、消滅・保持は本文の末尾 (2026-09-09 ユーザー「付箋でなく効果の最上部に」)
            var costNotes = CardText.CostNotes(def);
            var trail = CardText.TrailNotes(def);
            if (costNotes.Count > 0) bodyText = string.Join("\n", costNotes.ToArray()) + "\n" + bodyText;
            if (trail.Count > 0) bodyText = bodyText + "\n" + string.Join("・", trail.ToArray());
            bodyText = CardText.Emphasize(bodyText);
            var body = UiKit.Txt(root, bodyText, 16, ink, TextAnchor.UpperCenter, true);
            UiKit.Anchor(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 14f), new Vector2(-14f, -172f));
            body.enableAutoSizing = true; body.fontSizeMin = 12f; body.fontSizeMax = 16f;   // 長文だけ 12px まで縮める (最小 13px の唯一の例外)
            body.lineSpacing = 2f;

        }

        /// <summary>手札用: 成長・勢い・弱体を掛けたダメージ。PreviewEnemy (狙った敵・ドラッグ先・生存1体) があれば engine の DamageBreakdownOf と同じ手順で
        /// 急所×1.5・装甲上限まで掛ける (敵ブロックは引かない = 本家と同じく「与えるダメージ」を出す。2026-09-09)</summary>
        static Func<int, int> MakeDamageModifier(GameState st, CardInstance c)
        {
            var p = st.Player;
            int grow = c != null && c.GrowBonus.HasValue ? c.GrowBonus.Value : 0;
            int target = PreviewEnemy;
            bool hasTarget = target >= 0 && target < st.Enemies.Count && st.Enemies[target].Hp > 0;
            return delegate (int baseAmt)
            {
                int a = baseAmt + grow;
                if (hasTarget)
                {
                    try
                    {
                        var bd = Effects.DamageBreakdownOf(st, target, a, false);
                        if (bd != null && bd.Steps.Count > 0)
                        {
                            int v = a;
                            for (int i = 0; i < bd.Steps.Count; i++)
                            {
                                string l = bd.Steps[i].Label ?? "";
                                if (l.StartsWith("敵ブロック") || l.StartsWith("貫通") || l.StartsWith("潜伏") || l.StartsWith("無形") || l.StartsWith("ターン装甲")) break;
                                v = bd.Steps[i].Value;
                            }
                            return v;
                        }
                    }
                    catch (Exception) { }
                }
                if (p.Growth > 0) a += p.Growth;
                if (p.Momentum > 0) a += p.Momentum;
                if (p.Weak > 0 && a > 0) a = Math.Max(1, (int)Math.Floor(a * 0.75));
                return a;
            };
        }

        public static void RoleLabels(CardDef def, out string dmg, out string blk, out string counter, out bool modeBoth)
        {
            int delta;
            RoleLabels(def, null, out dmg, out blk, out counter, out modeBoth, out delta);
        }

        /// <summary>効果から役割の値を導く。ダメージ=onPlay の dealDamage の合計 (同値の多段は a×n)、ブロック=gainBlock/gainIceBlock、返し=counter。
        /// mod があればダメージは補正後の値で、dmgDelta にその向き (+/-) を返す</summary>
        public static void RoleLabels(CardDef def, Func<int, int> mod, out string dmg, out string blk, out string counter, out bool modeBoth, out int dmgDelta)
        {
            dmg = null; blk = null; counter = null; modeBoth = false; dmgDelta = 0;
            if (def.Modes != null && def.Modes.Count > 0)
            {
                string d = null, b = null; int dd = 0;
                for (int m = 0; m < def.Modes.Count; m++)
                {
                    string md, mb, mc; int mdd;
                    Summarize(def.Modes[m].Effects, mod, out md, out mb, out mc, out mdd);
                    if (md != null) { d = md; dd = mdd; }
                    if (mb != null) b = mb;
                }
                dmg = d; blk = b; modeBoth = d != null && b != null; dmgDelta = dd;
                return;
            }
            Summarize(def.Effects, mod, out dmg, out blk, out counter, out dmgDelta);
        }

        static void Summarize(IReadOnlyList<DeclarativeEffect> effects, Func<int, int> mod, out string dmg, out string blk, out string counter, out int dmgDelta)
        {
            dmg = null; blk = null; counter = null; dmgDelta = 0;
            if (effects == null) return;
            var dmgs = new List<int>();
            int block = 0, ctr = 0, baseSum = 0;
            for (int i = 0; i < effects.Count; i++)
            {
                var e = effects[i];
                if (e.Trigger != null && e.Trigger != "onPlay" && e.Trigger != "onAttacked" && e.Trigger != "onAttackedPre") continue;
                if (e.Effect == "dealDamage" && e.Amount.HasValue)
                {
                    bool play = e.Trigger == null || e.Trigger == "onPlay";
                    int v = play && mod != null ? mod(e.Amount.Value) : e.Amount.Value;
                    dmgs.Add(v); baseSum += e.Amount.Value;
                }
                else if ((e.Effect == "gainBlock" || e.Effect == "gainIceBlock") && e.Amount.HasValue) block += e.Amount.Value;
                else if (e.Effect == "counter" && e.Amount.HasValue) ctr += e.Amount.Value;
            }
            if (dmgs.Count > 0)
            {
                bool same = true;
                for (int i = 1; i < dmgs.Count; i++) if (dmgs[i] != dmgs[0]) same = false;
                int sum = 0; for (int i = 0; i < dmgs.Count; i++) sum += dmgs[i];
                dmg = dmgs.Count > 1 && same ? dmgs[0] + "×" + dmgs.Count : sum.ToString();
                dmgDelta = sum - baseSum;
            }
            if (block > 0) blk = block.ToString();
            if (ctr > 0) counter = ctr.ToString();
        }
    }
}
