// CardView.cs — カードの面 (200×290)。「W09」(2026-10-26 ユーザー裁定「札のフレームも Godot に合わせる」):
// dot の Godot 版 SourceCardFace と同じ設計 (630×880) を 200×290 へ写す。夜の青の板・レア度の線と角飾り (C 灰青・U 青緑・R 真鍮)・左上の角ばったコスト枠・
// リーダーの色の帯 (名前の帯・挿絵の台・下の帯)・挿絵 (Art/cards/<id>.png 80×48 を台いっぱい・5:3 のまま。無ければタイプの紋章)・
// タイプの形 (物理=六角の帯・呪文=弧・仕込み札=斜めの帯・置物=箱)・本文は夜の板に淡い紙色の字 (数字は太字・段落の間に線)・レア度の粒。字は Noto Sans JP (Godot の Font A と同じ)。
// 形の層は scripts/gen-cardface.py が描いた PNG (Art/ui/cardface_frame_<c|u|r>・cardface_type_<タイプ>・cardface_band)。色の帯は PaperFx の色の Image＋光沢の 9スライス。
// 旧 (2026-09-09〜10-25): 絵本の紙＋案B「本家型の帯」(紙・コスト玉・タイプの帯・宝石)。要の数字の札 (三周目 r3) と RoleLabels は据え置き。
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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
        /// <summary>設計座標 (Godot 版と同じ 630×880)。Place がこの寸法から札の寸法へ写す (縦は 3.8% 伸びる＝形の層には見えない。挿絵だけ 5:3 を保つ)</summary>
        const float DW = 630f, DH = 880f;

        /// <summary>予測行 (対象が決まっている時の実値) の対象。-1 で出さない</summary>
        public static int PreviewEnemy = -1;

        /// <summary>名前をコスト枠の右から左寄せで描く (スマホの手札が7枚以上＝重なって札の左側しか見えない時だけ。BattleView.SyncHand が立てて戻す。2026-09-29 p09)</summary>
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

        // ---- 設計座標の置き場 ----

        /// <summary>設計座標 (x, y, w, h。左上が原点) の矩形を札の左上基準で置く。grow は四方に足す設計 px (色の帯を板の穴より 1 大きく敷く)</summary>
        static RectTransform Place(Transform parent, string name, float x, float y, float w, float h, float grow = 0f)
        {
            var rt = UiKit.NewRect(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            float sx = W / DW, sy = H / DH;
            rt.anchoredPosition = new Vector2((x - grow) * sx, -(y - grow) * sy);
            rt.sizeDelta = new Vector2((w + grow * 2f) * sx, (h + grow * 2f) * sy);
            return rt;
        }

        static Image Layer(Transform parent, string name, Sprite sprite, float x, float y, float w, float h, Color tint)
        {
            var rt = Place(parent, name, x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = tint;
            img.raycastTarget = false;
            img.preserveAspect = false;
            return img;
        }

        /// <summary>色の帯の色: 札の色 (合成札は定義の色)。状態異常・中立は青灰</summary>
        static Color FactionColor(string color)
        {
            switch (color)
            {
                case "green": return PaperFx.FactionGreen;
                case "red": return PaperFx.FactionRed;
                case "blue": return PaperFx.FactionBlue;
                case "white": return PaperFx.FactionWhite;
                case "black": return PaperFx.FactionBlack;
            }
            return PaperFx.FactionNeutral;
        }

        // 色の帯 (設計座標): 名前の帯・挿絵の台・下の帯 (Godot 版 SelectedFrame の "Faction C bands")
        static readonly float[][] Bands = { new[] { 169f, 29f, 438f, 118f }, new[] { 24f, 150f, 582f, 349f }, new[] { 25f, 806f, 581f, 34f } };

        static void Fill(RectTransform root, CardInstance c, GameState st, bool playable, bool interactable)
        {
            var def = c.Def;
            string rarity = def.Rarity ?? (def.Id != null && def.Id.StartsWith("fusion_") ? "rare" : "common");   // レシピ産 (手書きの一品) は真鍮の線 (2026-09-12)
            string rank = rarity == "rare" ? "r" : rarity == "uncommon" ? "u" : "c";
            string type = def.Type == "physical" || def.Type == "spell" || def.Type == "reaction" || def.Type == "permanent" ? def.Type : "other";
            var faction = FactionColor(def.Color);
            // 出せない札は板・帯・絵をまとめて灰に沈める (主な合図は手札の16px沈み。p05)
            var tint = playable ? Color.white : PaperFx.CardDim;

            // ① 色の帯 (板の穴の下。1 設計px 大きく敷いて継ぎ目を見せない) と光沢
            var sheen = Theme.Art("ui", "cardface_band");
            for (int i = 0; i < Bands.Length; i++)
            {
                var b = Bands[i];
                var band = Place(root, "band", b[0], b[1], b[2], b[3], 1f);
                var bImg = band.gameObject.AddComponent<Image>();
                bImg.color = faction * tint; bImg.raycastTarget = false;
                if (sheen != null)
                {
                    var s = Layer(root, "sheen", sheen, b[0], b[1], b[2], b[3], tint);
                    s.type = Image.Type.Sliced;
                    s.pixelsPerUnitMultiplier = DW / W;   // 光沢の線は設計の画素で描いてあるので札の縮尺に合わせる
                }
            }

            // ② 板 (レア度の線・角飾り・コスト枠・本文の板・粒)。札の当たり判定はこの板
            var frameSprite = Theme.Art("ui", "cardface_frame_" + rank);
            Image frame;
            if (frameSprite != null) frame = Layer(root, "frame", frameSprite, 0f, 0f, DW, DH, tint);
            else { frame = UiKit.Pan(root, PaperFx.CardShell * tint, "frame"); UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f); }
            frame.raycastTarget = interactable;

            // ③ 挿絵 (台いっぱい 560×336。80×48 は 5:3 なので縦に少し余り、台の色が上下に見える)
            var art = Theme.Art("cards", def.Id) ?? ThemeFx.FusedArt(def.Id);   // 工房産は素材2枚の絵を溶かし合わせる (2026-09-11)
            var artRt = Place(root, "art", 35f, 158f, 560f, 336f);
            if (art != null)
            {
                var aimg = artRt.gameObject.AddComponent<Image>();
                aimg.sprite = art; aimg.preserveAspect = true; aimg.raycastTarget = false;
                aimg.color = playable ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
                UiKit.PixelArt(aimg);   // 画面の px では整数倍にならない (手札 PC 0.92 倍・スマホ 1.31〜1.6 倍) → UIPixelSharp でドットの太さをそろえる (p25)
            }
            else
            {
                var crestRt = UiKit.NewRect("crest", artRt);
                crestRt.anchorMin = crestRt.anchorMax = new Vector2(0.5f, 0.5f);
                crestRt.sizeDelta = Theme.HasIconArt("crest_" + def.Type) ? new Vector2(64f, 64f) : new Vector2(48f, 48f);   // 絵は 32 ドット×2
                crestRt.anchoredPosition = Vector2.zero;
                var crest = crestRt.gameObject.AddComponent<Image>();
                crest.sprite = Theme.Icon("crest_" + def.Type);
                crest.preserveAspect = true;
                crest.raycastTarget = false;
                crest.color = playable ? Color.white : new Color(1f, 1f, 1f, 0.7f);
                UiKit.PixelArt(crest);
            }

            // ④ タイプの形 (y 488〜816 の切り出し)
            var typeSprite = Theme.Art("ui", "cardface_type_" + type);
            if (typeSprite != null) Layer(root, "typeshape", typeSprite, 0f, 488f, DW, 328f, tint);

            // ⑤ コスト (左上の枠の中)。表示は実際に払う量: 割引で下がれば緑、重圧で上がれば朱の数字 (本家の読み方。2026-09-09)
            int cost = def.Cost;
            try { if (st != null) cost = Effects.EffectiveCost(st, c); } catch (Exception) { }
            bool discounted = def.XCost != true && cost < def.Cost;
            bool raised = def.XCost != true && cost > def.Cost;
            string costLabel = def.XCost == true ? "X" : cost.ToString();
            var costRt = Place(root, "cost", 35f, 27f, 116f, 114f);
            var costT = UiKit.Sans(costRt, costLabel, 26, discounted ? PaperFx.CardCostDown : raised ? PaperFx.CardCostUp : PaperFx.CardInk, TextAnchor.MiddleCenter, true);
            UiKit.Stretch(costT.rectTransform, 0f, 0f, 0f, 0f);
            costT.textWrappingMode = TextWrappingModes.NoWrap;
            costT.characterSpacing = 0f;

            // ⑥ 名前 (色の帯の上。白の帯だけ墨)。長い名前は 13 まで縮める
            var nameRt = Place(root, "name", 177f, 35f, 420f, 85f);
            var nameT = UiKit.Sans(nameRt, def.Name, 18, def.Color == "white" ? PaperFx.CardNameOnWhite : PaperFx.CardInk,
                CompactName ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, true);
            // 手札が重なる時 (CompactName) は帯の左端から: 見えている札の左側に頭の字が来る (p09)
            UiKit.Stretch(nameT.rectTransform, CompactName ? 3f : 2f, 2f, 0f, 0f);
            nameT.textWrappingMode = TextWrappingModes.NoWrap;
            nameT.overflowMode = TextOverflowModes.Overflow;
            nameT.enableAutoSizing = true; nameT.fontSizeMin = 13f; nameT.fontSizeMax = 18f;

            // ⑦ タイプの文字 (形の中。物理／呪文／仕込み札／置物)
            var typeRt = Place(root, "type", 95f, type == "spell" ? 527f : 515f, 440f, 54f);
            var typeT = UiKit.Sans(typeRt, CardText.TypeJa(def.Type), type == "spell" ? 15 : 16, PaperFx.CardInk, TextAnchor.MiddleCenter, true);
            UiKit.Stretch(typeT.rectTransform, 0f, 0f, 0f, 0f);
            typeT.textWrappingMode = TextWrappingModes.NoWrap;
            typeT.characterSpacing = 1f;

            // ⑧ 本文 (夜の板に淡い紙色。数字は太字・条件の句は薄い紙色)。戦闘中は成長・勢い・弱体、狙った敵の急所・装甲を掛けた実値を色つきで (本家のカードの数字の読み方)
            Func<int, int> mod = st != null ? MakeDamageModifier(st, c) : null;
            CardText.DamageModifier = mod;
            CardText.MarkClauses = true;
            string raw;
            try { raw = CardText.Body(def); }
            finally { CardText.DamageModifier = null; CardText.MarkClauses = false; }
            var costNotes = CardText.CostNotes(def);   // 追加コスト (X・捨て・消滅コスト・コスト0 の条件) は本文の先頭、消滅・保持は末尾 (2026-09-09)
            var trail = CardText.TrailNotes(def, true);   // 札の面の短い形 (F10)
            if (st != null)
            {
                // 衝動の札 (このターン限り) は面に常時出す (2026-10-12 段2b Opus C: 衝動で引いた野火を含む5枚が失効した事故)
                if (BattleView.IsImpulse(st, c.Uid)) trail.Add("衝動＝撃たなければ消滅（払えない札は捨て札へ）");
                // 火の渦の「いま何回か」(同日 Opus C。Web の [現在N回] と同じ)
                for (int i = 0; i < def.Effects.Count; i++)
                    if (def.Effects[i].Effect == "dealDamagePerCascade") { int n = st.Player.CascadesThisCombat ?? 0; trail.Add("チェイン" + n + "回×" + (def.Effects[i].Amount ?? 0) + "＝" + (n * (def.Effects[i].Amount ?? 0))); break; }
            }
            bool hasModes = def.Modes != null && def.Modes.Count > 0;
            raw = LightenValueColors(raw);   // CardText が実値に付けた紙の上の墨の色を、夜の板で読める明るい版へ
            // 「ブロック5」「ダメージ6」だけの札は Godot 版と同じく数字を 170% で大きく (単一の量)
            var metric = costNotes.Count == 0 && trail.Count == 0 && !hasModes ? SingleMetric.Match(raw) : Match.Empty;
            string bodyText;
            if (metric.Success)
                bodyText = metric.Groups[1].Value + "  <size=170%><b>" + metric.Groups[2].Value + "</b></size>";
            else
            {
                var sb = new StringBuilder();
                for (int i = 0; i < costNotes.Count; i++) sb.Append(CardText.ClauseOpen).Append(costNotes[i]).Append(CardText.ClauseClose).Append('\n');
                if (hasModes) sb.Append(CardText.ClauseOpen).Append("どちらか一つ").Append(CardText.ClauseClose).Append('\n');
                sb.Append(raw);
                if (trail.Count > 0) sb.Append('\n').Append(TrailMarkup(trail));
                bodyText = FaceMarkup(sb.ToString());
            }
            int plainLen = Plain(raw).Replace("\n", "").Length;
            float sizeMax = metric.Success || plainLen < 38 ? 18f : 14f;   // Godot 版の 39／29 (設計) の比
            var bodyRt = Place(root, "body", 50f, 577f, 530f, 212f);
            float inset = BodyRightInset;
            var body = UiKit.Sans(bodyRt, bodyText, 18, PaperFx.CardInk, metric.Success ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft, false);
            UiKit.Stretch(body.rectTransform, 0f, inset, 0f, 0f);
            body.enableAutoSizing = true; body.fontSizeMin = 12f; body.fontSizeMax = sizeMax;
            body.lineSpacing = 2f;
            body.paragraphSpacing = 32f;   // 段落の間 (設計 18px ≒ 6px)。線はこの隙間の真ん中
            LastBodyTruncated = false;
            try
            {
                body.ForceMeshUpdate(true);
                // 覆われる札の余白で字が 14 を割るなら余白をやめる (字を小さくして読ませるより、覆われる方がまし。F01)
                if (inset > 0f && body.fontSize < 14f) { UiKit.Stretch(body.rectTransform, 0f, 0f, 0f, 0f); body.ForceMeshUpdate(true); }
                if (body.textBounds.size.y > body.rectTransform.rect.height + 0.5f)
                {   // 12 でも入らない長文: 上から並べて、枠の外には出さず末尾を「…」に (全文は長押しの拡大。F10)
                    body.alignment = TextAlignmentOptions.TopLeft;
                    body.overflowMode = TextOverflowModes.Ellipsis;
                    body.ForceMeshUpdate(true);
                    LastBodyTruncated = true;
                }
                if (!metric.Success) AddDividers(body);
            }
            catch (Exception) { body.fontSizeMax = 14f; body.alignment = TextAlignmentOptions.TopLeft; }
            // PC の手札 (0.92 倍) で 16 以下に縮んだ本文は、小さい字の素材 (SDF をわずかに膨らませる) で指定の濃さへ (F08)
            if (InHand && !UiKit.Phone && body.fontSize * BattleScreen.CardScale <= UiKit.SmallTextMax + 0.01f)
            {
                var sm = UiKit.SmallMat(body.font);
                if (sm != null) body.fontSharedMaterial = sm;
            }
            // 三周目 r3 の手札だけ: 要の数字の札 (札を沈めると本文が見えないので、最初の量の効果を1つ左上に。拡大の窓・報酬・店・r2 には出さない)
            if (InHand && BattleScreen.R3) BuildKeyNum(root, def, mod);
        }

        // ---- 本文の組み方 (W09) ----

        static readonly Regex SingleMetric = new Regex("^(ブロック|ダメージ)((?:<color=#[0-9A-Fa-f]{6}>)?[0-9]+(?:</color>)?)$", RegexOptions.Compiled);
        static readonly Regex NumberToken = new Regex(@"\G[+＋−-]?[0-9]+(?:[×xX][0-9X]+)*", RegexOptions.Compiled);

        /// <summary>本文のタグと条件の印を外した素の文</summary>
        static string Plain(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch == CardText.ClauseOpen || ch == CardText.ClauseClose) continue;
                if (ch == '<') { int j = s.IndexOf('>', i); if (j < 0) break; i = j; continue; }
                sb.Append(ch);
            }
            return sb.ToString();
        }

        /// <summary>CardText が実値に付けた色 (紙の上の良い墨・下がった墨) を、夜の板で読める明るい版 (苔の明・薔薇の明) へ</summary>
        static string LightenValueColors(string body)
        {
            if (string.IsNullOrEmpty(body)) return body;
            string good = ColorUtility.ToHtmlStringRGB(PaperFx.GoodInk), bad = ColorUtility.ToHtmlStringRGB(PaperFx.BadDown);
            return body.Replace("<color=#" + good + ">", "<color=#" + ColorUtility.ToHtmlStringRGB(PaperFx.MossLight) + ">")
                       .Replace("<color=#" + bad + ">", "<color=#" + ColorUtility.ToHtmlStringRGB(PaperFx.RoseLight) + ">");
        }

        /// <summary>W09 の本文: 条件の句 (ClauseOpen〜Close) は薄い紙色、数字は太字、行頭の「被攻撃後: 」のような頭も太字 (X: は除く)</summary>
        static string FaceMarkup(string body)
        {
            string dim = ColorUtility.ToHtmlStringRGB(PaperFx.PaperDim);
            var sb = new StringBuilder();
            bool lineStart = true, inClause = false;
            int i = 0;
            while (i < body.Length)
            {
                char ch = body[i];
                if (ch == CardText.ClauseOpen) { sb.Append("<color=#").Append(dim).Append('>'); inClause = true; i++; continue; }
                if (ch == CardText.ClauseClose) { sb.Append("</color>"); inClause = false; i++; continue; }
                if (ch == '<')
                {
                    int j = body.IndexOf('>', i);
                    if (j < 0) { sb.Append(body.Substring(i)); break; }
                    sb.Append(body, i, j - i + 1); i = j + 1; continue;
                }
                if (ch == '\n') { sb.Append('\n'); lineStart = true; i++; continue; }
                if (lineStart && !inClause)
                {
                    lineStart = false;
                    int nl = body.IndexOf('\n', i); if (nl < 0) nl = body.Length;
                    int colon = body.IndexOf(": ", i, nl - i, StringComparison.Ordinal);
                    if (colon > i && body[i] != 'X' && body.IndexOf('<', i, colon - i) < 0 && body.IndexOf(CardText.ClauseOpen, i, colon - i) < 0)
                    {
                        sb.Append("<b>").Append(body, i, colon - i + 1).Append("</b> ");
                        i = colon + 2; continue;
                    }
                }
                var m = NumberToken.Match(body, i);
                if (m.Success) { sb.Append("<b>").Append(m.Value).Append("</b>"); i += m.Length; continue; }
                sb.Append(ch); i++;
            }
            return sb.ToString();
        }

        /// <summary>段落の間に線 (設計の 2px・板より少し明るい)。TMP の行の情報から、'\n' で終わる行とその次の行の間に置く</summary>
        static void AddDividers(TMP_Text body)
        {
            var ti = body.textInfo;
            if (ti == null || ti.lineCount < 2) return;
            for (int i = 1; i < ti.lineCount; i++)
            {
                var prev = ti.lineInfo[i - 1];
                int last = prev.lastCharacterIndex;
                if (last < 0 || last >= ti.characterCount || ti.characterInfo[last].character != '\n') continue;
                float y = (prev.descender + ti.lineInfo[i].ascender) / 2f;
                var rule = UiKit.NewRect("rule", body.transform);
                rule.anchorMin = new Vector2(0f, 0.5f); rule.anchorMax = new Vector2(1f, 0.5f);
                rule.pivot = new Vector2(0.5f, 0.5f);
                rule.offsetMin = new Vector2(0f, y - 0.5f); rule.offsetMax = new Vector2(0f, y + 0.5f);
                var img = rule.gameObject.AddComponent<Image>();
                img.color = PaperFx.CardDivider; img.raycastTarget = false;
            }
        }

        // ---- 要の数字の札 (三周目 r3 UI 2026-10-02・仕様 docs/design/hd2d-slice/r3-ui-spec.md §4) ----
        // 手札を沈めると本文が画面の外に出るので、本文の最初の量の効果を1つ、コスト枠のすぐ下 (挿絵の台の左上・札の単位で x 12〜・y 50〜74) に小さな札で出す。
        // 手札は右の札ほど手前に描く (SyncHand の SetSiblingIndex(i)) ので、扇が重なっても札の左上は隠れない。値は本文と同じ実値
        // (MakeDamageModifier＝成長・勢い・弱体・狙った敵の急所と装甲込み。狙った敵が変わると RefreshHandCard が札ごと描き直す)。
        // 形は「剣の絵＋6」「剣の絵＋4×2」「盾の絵＋5」「心の絵＋6」「2ドロー」「+1E」。選択式・量の無い札・状態異常の札は出さない (触れて読む)。
        // 見た目は紙 (濃) の札＋墨の数字 (Deco 17)。新しい色は使わない。触れて上がっている間は隠す (SetKeyNumVisible。本文に同じ数字がある)

        /// <summary>要の数字の札を出し入れする (無ければ何もしない)</summary>
        public static void SetKeyNumVisible(RectTransform card, bool on)
        {
            var k = card != null ? card.Find("keynum") : null;
            if (k != null && k.gameObject.activeSelf != on) k.gameObject.SetActive(on);
        }

        static void BuildKeyNum(RectTransform root, CardDef def, Func<int, int> mod)
        {
            string icon, text;
            if (!KeyNumOf(def, mod, out icon, out text)) return;
            var k = UiKit.NewRect("keynum", root);
            var paper = PaperFx.Sheet(k, PaperFx.Tag2, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = false;
            var t = UiKit.Deco(k, text, 17, PaperFx.Ink, TextAnchor.MiddleLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.characterSpacing = 0f;
            t.raycastTarget = false;
            float tw = Mathf.Ceil(t.GetPreferredValues(text).x);
            float iconW = icon != null ? 16f : 0f;
            float w = 6f + iconW + (icon != null ? 3f : 0f) + tw + 7f;
            UiKit.Anchor(k, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -74f), new Vector2(12f + w, -50f));
            if (icon != null)
            {
                var ic = UiKit.Icon(k, icon, 16f, PaperFx.Ink, true);
                ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                ic.rectTransform.pivot = new Vector2(0f, 0.5f);
                ic.rectTransform.anchoredPosition = new Vector2(6f, 0f);
            }
            UiKit.Anchor(t.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(6f + iconW + (icon != null ? 3f : 0f), 0f), new Vector2(-4f, 0f));
        }

        /// <summary>要の数字: 本文 (onPlay) の最初の量の効果。ダメージは同じ札の onPlay のダメージを全部まとめる (同値の多段は a×n・X 札は a×X)。
        /// 条件つきの効果 (condition 付き: 「上限5以上でさらに6」「急所なら4」「成長3以上で+6」・猛り火など) は数えない (2026-10-02 読み合わせの指摘:
        /// Summarize は条件を見ず、被攻撃の窓の効果も足すので、条件を満たしていない時に本文より大きい数を見せていた＝表示の嘘)。
        /// 盤面で条件の成否を判定する式は手札の札の組み立てに渡っていないので、条件つきの分は足さない (触れて本文で読む)</summary>
        static bool KeyNumOf(CardDef def, Func<int, int> mod, out string icon, out string text)
        {
            icon = null; text = null;
            if (def == null || def.Effects == null || (def.Modes != null && def.Modes.Count > 0)) return false;
            for (int i = 0; i < def.Effects.Count; i++)
            {
                var e = def.Effects[i];
                if (e.Trigger != null && e.Trigger != "onPlay") continue;
                if (!e.Amount.HasValue || e.Condition != null) continue;   // 条件つきは数えない (上の注記)
                int amt = e.Amount.Value;
                string xs = e.XHits == true ? "×X" : "";
                switch (e.Effect)
                {
                    case "dealDamage":
                    {
                        string dmg = KeyNumDamage(def.Effects, mod);
                        if (dmg == null) return false;
                        icon = "sword"; text = dmg + xs; return true;
                    }
                    case "dealDamageRandom":
                        icon = "sword"; text = amt + "〜" + (e.AmountMax.HasValue ? e.AmountMax.Value : amt); return true;
                    case "gainBlock":
                    case "gainIceBlock":
                        icon = "shield"; text = amt + xs; return true;
                    case "gainHp":
                        icon = "heart"; text = amt + xs; return true;
                    case "drawCards":
                        text = amt + "ドロー"; return true;
                    case "gainEnergy":
                        text = "+" + amt + "E"; return true;
                }
            }
            return false;
        }

        /// <summary>要の数字のダメージ: onPlay で条件の無い dealDamage だけを集める (同値の繰り返し＝多段は a×n、違う値は合計)。値は手札の本文と同じ補正 (mod)</summary>
        static string KeyNumDamage(IReadOnlyList<DeclarativeEffect> effects, Func<int, int> mod)
        {
            var dmgs = new List<int>();
            for (int i = 0; i < effects.Count; i++)
            {
                var e = effects[i];
                if (e.Trigger != null && e.Trigger != "onPlay") continue;
                if (e.Effect != "dealDamage" || !e.Amount.HasValue || e.Condition != null) continue;
                dmgs.Add(mod != null ? mod(e.Amount.Value) : e.Amount.Value);
            }
            if (dmgs.Count == 0) return null;
            bool same = true; int sum = 0;
            for (int i = 0; i < dmgs.Count; i++) { if (dmgs[i] != dmgs[0]) same = false; sum += dmgs[i]; }
            return dmgs.Count > 1 && same ? dmgs[0] + "×" + dmgs.Count : sum.ToString();
        }

        /// <summary>本文の末尾の注記 (2026-09-29 p06): 「消滅」「保持」「骨のナイフ」の一語は目立つ色 (W09 では真鍮の明るい版)、
        /// 人形・触媒・反復の長い注記は条件の句と同じ薄い紙色。大きさは本文と同じ。「・」でつなぐ</summary>
        static string TrailMarkup(List<string> notes)
        {
            var parts = new List<string>();
            for (int i = 0; i < notes.Count; i++)
            {
                string n = notes[i];
                if (n == "消滅" || n == "保持" || n == "骨のナイフ") parts.Add(UiKit.ColorTag(PaperFx.BrassLight, "<nobr>" + n + "</nobr>"));
                else parts.Add(CardText.ClauseOpen + n + CardText.ClauseClose);
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
