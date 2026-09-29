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

        /// <summary>名前をコスト玉の右から左寄せで描く (スマホの手札が7枚以上＝重なって札の左側しか見えない時だけ。BattleView.SyncHand が立てて戻す。2026-09-29 p09)</summary>
        public static bool CompactName;

        /// <summary>本文の右の余白に足す量 (スマホの手札で隣の札に覆われる幅。BattleView.SyncHand が立てて戻す。2026-09-30 F01)</summary>
        public static float BodyRightInset;

        /// <summary>手札に描く札 (BattleView.SyncHand・RefreshHandCard が立てて戻す)。PC の手札は 0.92 倍なので、縮んだ本文に小さい字の素材を当てる (F08)</summary>
        public static bool InHand;

        /// <summary>直前に描いた札の本文が 12 でも枠に入らず切れたか (拡大の窓が全文を添える。F10)</summary>
        public static bool LastBodyTruncated;

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
            // 出せない札の墨は透明度で薄めず実色の中墨 (灰の紙 (224,212,184) の上で 5.7:1。規約「二次の文字色は実色」。2026-09-29 p05)
            var ink = playable ? PaperFx.Ink : PaperFx.InkSoft;
            string rarity = def.Rarity ?? (def.Id != null && def.Id.StartsWith("fusion_") ? "rare" : "common");   // レシピ産 (手書きの一品) は蜂蜜の外線 (2026-09-12)

            // 紙 (外側の線の色がレア度: C 墨・U 空・R 蜂蜜)
            var paper = PaperFx.Sheet(root, PaperFx.CardOf(rarity), "paper", playable ? Color.white : PaperFx.DimTint);   // 出せない札は少しだけ沈んだ紙 (主な合図は手札の16px沈みと灰の玉。p05)
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
            var winIn = UiKit.Pan(win, PaperFx.Window, "night");
            UiKit.Stretch(winIn.rectTransform, 1.5f, 1.5f, 1.5f, 1.5f);
            winIn.raycastTarget = false;
            var art = Theme.Art("cards", def.Id) ?? ThemeFx.FusedArt(def.Id);   // 工房産は素材2枚の絵を溶かし合わせる (2026-09-11)
            if (art != null)
            {
                // 80×48 を2倍の寸法 (160×96 単位) で置く。それ以外の寸法でも縦横比を保って収める。
                // 画面の px では整数倍にならない (手札は PC 0.92 倍＝1.84倍・スマホは 1.31〜1.6 倍の画面＝2.6〜3.2倍・拡大の窓 1.6 倍・扇の ±3〜6°)
                // → UIPixelSharp でドットの太さをそろえる (2026-09-29 p25。PC 1080 の報酬・店の等倍の札は 2倍ちょうど＝最近傍のまま)
                var ai = UiKit.NewRect("pic", win);
                ai.anchorMin = ai.anchorMax = new Vector2(0.5f, 0.5f);
                ai.sizeDelta = new Vector2(Mathf.Min(160f, art.rect.width * 2f), Mathf.Min(96f, art.rect.height * 2f));
                var aimg = ai.gameObject.AddComponent<Image>();
                aimg.sprite = art; aimg.preserveAspect = true; aimg.raycastTarget = false;
                aimg.color = playable ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
                UiKit.PixelArt(aimg);
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
                UiKit.PixelArt(crest);   // 32 ドット×2 か 16 ドット×3 (手札では非整数倍。p25)
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
            orbImg.sprite = orbArt != null ? orbArt : PaperFx.Orb(PaperFx.Brass);
            orbImg.preserveAspect = true; orbImg.raycastTarget = false;
            if (!playable)
            {   // 出せない札の玉は灰の版 (2026-09-30 F26: 乗算 0.82 では暗い金のままで「灰の玉」になっていなかった)
                var muted = orbArt != null ? ThemeFx.CostOrbMuted() : null;
                if (muted != null) orbImg.sprite = muted;
                else if (orbArt != null) orbImg.color = new Color(0.82f, 0.82f, 0.82f, 1f);
                else orbImg.sprite = PaperFx.Orb(Color.Lerp(PaperFx.Brass, PaperFx.PaperDim, 0.75f));
            }
            UiKit.PixelArt(orbImg);   // PixelLab の玉 (26 ドット) だけ。水彩の玉 (なめらかな生成) は既定のまま (p25)
            // コストの数字は出せない札でも墨のまま (灰の玉の上で中墨は 3.1:1 しかない。p05)
            var costT = UiKit.Deco(orb, costLabel, 22, discounted ? PaperFx.GoodInk : raised ? PaperFx.BadDown : PaperFx.Ink, TextAnchor.MiddleCenter);
            UiKit.Anchor(costT.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0f, -1f));
            costT.characterSpacing = 0f;

            // 名前 (装飾明朝)
            int nameSize = def.Name.Length > 5 ? 17 : (def.Name.Length > 4 ? 18 : 20);
            // 手札が重なる時 (CompactName) はコスト玉のすぐ右から左寄せ: 見えている札の左側に頭の字が来る (中央寄せの「打撃」は10枚で丸ごと隠れた。p09)
            var nameT = UiKit.Deco(root, def.Name, nameSize, ink, CompactName ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
            if (CompactName) UiKit.Anchor(nameT.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(48f, -40f), new Vector2(-10f, -8f));
            else UiKit.Anchor(nameT.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -40f), new Vector2(-12f, -8f));
            nameT.textWrappingMode = TextWrappingModes.NoWrap;
            nameT.overflowMode = TextOverflowModes.Overflow;

            // タイプの帯 (窓の下端に掛ける。色と文字で 物理／呪文／リアクション／置物、宝石がレア度)
            var ribbon = UiKit.NewRect("type", root);
            UiKit.Anchor(ribbon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-75f, -162f), new Vector2(75f, -136f));
            var rImg = ribbon.gameObject.AddComponent<Image>();
            rImg.sprite = PaperFx.Ribbon(playable ? typeCol : Color.Lerp(typeCol, Color.gray, 0.3f));
            rImg.raycastTarget = false;
            var row = UiKit.NewRect("row", ribbon);
            UiKit.Stretch(row, 0f, 0f, 0f, 0f);
            var hg = UiKit.Horz(row, 5, 0);
            hg.childAlignment = TextAnchor.MiddleCenter; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            var gem = UiKit.NewRect("gem", row);
            var gImg = gem.gameObject.AddComponent<Image>();
            gImg.sprite = ThemeFx.Gem(rarity); gImg.preserveAspect = true; gImg.raycastTarget = false;
            UiKit.PixelArt(gImg);   // 12 ドット×2 の宝石も手札では 1.84 倍 (p25)
            float gemSz = Theme.Art("ui", "gem_" + rarity) != null ? 24f : 16f;   // PixelLab の宝石は 12 ドット×2 (2026-09-11)
            UiKit.Le(gem, gemSz, gemSz, gemSz, gemSz);
            // 16 の太字 (SDF の合成太字)。14 は手札の 0.92 倍で 12.9px になり、細い線が墨まで届かず帯の上で 2.6〜3.4:1 だった (2026-09-29 p19)。
            // 帯 150×26 には「仕込み札」でも約 99px で収まる
            var typeT = UiKit.Txt(row, CardText.TypeJa(def.Type), 16, PaperFx.Ink, TextAnchor.MiddleCenter, true);
            typeT.fontStyle = FontStyles.Bold;
            typeT.characterSpacing = 1f;
            UiKit.Le(typeT, -1f, 22f, -1f, 22f);

            // 本文 (墨。数字は 130%)。戦闘中は成長・勢い・弱体、狙った敵の急所・装甲を掛けた実値を色つきで (本家のカードの数字の読み方)。
            // 強調 (Emphasize) は効果の行だけに掛ける＝130% になるのは効果の値だけで、条件の句・支払い・注記の数字は中墨の本文の大きさ (2026-09-29 p06)
            string soft = ColorUtility.ToHtmlStringRGB(PaperFx.InkSoft);
            Func<int, int> mod = st != null ? MakeDamageModifier(st, c) : null;
            CardText.DamageModifier = mod;
            CardText.MarkClauses = true;
            string bodyText;
            try { bodyText = CardText.Emphasize(CardText.Body(def), soft); }
            finally { CardText.DamageModifier = null; CardText.MarkClauses = false; }
            if (def.Modes != null && def.Modes.Count > 0) bodyText = UiKit.ColorTag(PaperFx.InkSoft, "どちらか一つ") + "\n" + bodyText;   // 本文と同じ大きさの中墨 (旧 80% は 12px を割った)
            // 追加コスト (X・捨て・消滅コスト・コスト0 の条件) は本文の先頭に普通の表記、消滅・保持は本文の末尾 (2026-09-09 ユーザー「付箋でなく効果の最上部に」)
            var costNotes = CardText.CostNotes(def);
            var trail = CardText.TrailNotes(def, true);   // 札の面の短い形 (F10)
            if (costNotes.Count > 0)
            {
                var cl = new List<string>();
                for (int i = 0; i < costNotes.Count; i++) cl.Add(CardText.Emphasize(CardText.ClauseOpen + costNotes[i] + CardText.ClauseClose, soft));   // 中墨・100% (語の途中の改行だけ止める)
                bodyText = string.Join("\n", cl.ToArray()) + "\n" + bodyText;
            }
            if (trail.Count > 0) bodyText = bodyText + "\n" + TrailMarkup(trail, soft);
            // 枠 172×104 の上下の中央に置く。短い本文は 18 (数字は 130% で約23)、18 で段落より行が増える札は今までの 16、12 でも入らない長文は上から並べる
            // (中央のまま溢れるとタイプの帯へはみ出すため)。最小 12 は長文だけの例外 (13 への引き上げは長文を縮めてから)
            var body = UiKit.Txt(root, bodyText, 18, ink, TextAnchor.MiddleCenter, true);
            float inset = BodyRightInset;
            UiKit.Anchor(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 14f), new Vector2(-(14f + inset), -172f));
            body.enableAutoSizing = true; body.fontSizeMin = 12f; body.fontSizeMax = 18f;
            body.lineSpacing = 2f;
            // 選択式は段落 (◆) の間を少し空ける＝折り返した続きの行が自分の ◆ に寄って見える (F28。自動の折り返しには足されない)
            if (def.Modes != null && def.Modes.Count > 0) body.paragraphSpacing = 30f;
            LastBodyTruncated = false;
            try
            {
                int paras = 1;
                for (int i = 0; i < bodyText.Length; i++) if (bodyText[i] == '\n') paras++;
                Action layout = () =>
                {
                    body.fontSizeMax = 18f;
                    body.ForceMeshUpdate(true);
                    if (body.textInfo != null && body.textInfo.lineCount > paras) { body.fontSizeMax = 16f; body.ForceMeshUpdate(true); }
                };
                layout();
                // 覆われる札の余白で字が 14 を割るなら余白をやめる (字を小さくして読ませるより、覆われる方がまし。F01)
                if (inset > 0f && body.fontSize < 14f)
                {
                    body.rectTransform.offsetMax = new Vector2(-14f, -172f);
                    layout();
                }
                if (body.textBounds.size.y > body.rectTransform.rect.height + 0.5f)
                {   // 12 でも入らない長文: 上から並べて、枠の外 (タイプの帯・札の下) には出さず末尾を「…」に (全文は長押しの拡大。F10)
                    body.alignment = TextAlignmentOptions.Top;
                    body.overflowMode = TextOverflowModes.Ellipsis;
                    body.ForceMeshUpdate(true);
                    LastBodyTruncated = true;
                }
            }
            catch (Exception) { body.fontSizeMax = 16f; body.alignment = TextAlignmentOptions.Top; }
            // PC の手札 (0.92 倍) で 16 以下に縮んだ本文は、小さい字の素材 (SDF をわずかに膨らませる) で中墨・真鍮の墨の句を指定の濃さへ
            // (2026-09-30 F08: 本文は 18 で作るので UiKit.Txt の判定から漏れ、条件の句「手札が物理だけなら」が画面で約14px・3.9:1 に沈んでいた)
            if (InHand && !UiKit.Phone && body.fontSize * BattleScreen.CardScale <= UiKit.SmallTextMax + 0.01f)
            {
                var sm = UiKit.SmallMat(body.font);
                if (sm != null) body.fontSharedMaterial = sm;
            }

        }

        /// <summary>本文の末尾の注記 (2026-09-29 p06): 「消滅」「保持」「骨のナイフ」の一語は真鍮の墨 (color-theme の「注意書き」)、
        /// 人形・触媒・反復の長い注記は中墨 (真鍮の量を増やさない)。大きさは本文と同じ (これ以上小さくすると 13px を割る)。「・」でつなぐ</summary>
        static string TrailMarkup(List<string> notes, string softHex)
        {
            var parts = new List<string>();
            for (int i = 0; i < notes.Count; i++)
            {
                string n = notes[i];
                if (n == "消滅" || n == "保持" || n == "骨のナイフ") parts.Add(UiKit.ColorTag(PaperFx.BrassInk, "<nobr>" + n + "</nobr>"));
                else parts.Add(CardText.Emphasize(CardText.ClauseOpen + n + CardText.ClauseClose, softHex));
            }
            return string.Join("・", parts.ToArray());
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
