// ScreenJuice.cs — 戦闘以外の画面の「選んだ後の 1 秒」(2026-11-04 束 C1。品質スプリント #5 の磨き)。
// 配置と構図 (採点 4 で通った報酬・焚き火 A・店 B・出来事) は変えず、選んだ後の手応え・現れ方・音だけを足す。
//   報酬の札  = 裏向きで並び 0.12 秒ずらしで返る (card_flip)。レアは返る前に真鍮の暈・返った瞬間に光の破裂と rare_reveal
//   遺物      = 光の輪＋夜の帯に名前 (0.8 秒)＋relic_get → 飛ぶ (land_relic)。宝箱は揺れて開き、光が破裂
//   店        = 値札から硬貨が上部バーの G へ (coin_fly / coin_land)・今買った品に「売切」の判 (sold_stamp)・払えない値札は震えて「G が足りない」
//   焚き火    = 休む＝HP の数字が 0.6 秒数え上がる (rest_heal)・炎が一拍大きく・火の粉が噴く。鍛える＝札が中央で 3 度光って鍛えた姿に (upgrade_shine)
//   工房      = 2 枚が結果の札へ寄って溶け (fuse_melt)、新しい札が押し当てられて現れる
//   出来事    = 選んだ結果 (G・HP・札・遺物の増減) を夜の札で滑り込ませてから幕 (event_gain / event_loss)
// 時計は Tween.Run/After (deltaTime 積算) か Tween.UnscaledDt。粒は添字 (黄金角) で散らす＝撮影で決定的。ScreenFx.Instant なら全部飛ばして終わりの姿。
// 色は PaperFx の役割の名前だけ (真鍮＝価値・青緑＝からくりと光・薔薇＝HP・苔＝回復)。紙は光らせない＝光と輪は FxLayer に置く
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class ScreenJuice
    {
        // ---- 共通の小物 ----

        /// <summary>一覧 (RunUi.CardGrid) の index 番目の "cell"。無ければ null (AttachUpgradeTips と同じ探し方)</summary>
        public static RectTransform GridCell(RectTransform area, int index)
        {
            if (area == null || index < 0) return null;
            var all = area.GetComponentsInChildren<RectTransform>(true);
            int k = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name != "cell") continue;
                if (k == index) return all[i];
                k++;
            }
            return null;
        }

        /// <summary>上部バーの札の最初の数字 (TMP_Text)。hp の札は 絵・数字・「/ 最大」の順なので先頭が数字</summary>
        public static TMP_Text FirstText(RectTransform rt)
        {
            if (rt == null) return null;
            var ts = rt.GetComponentsInChildren<TMP_Text>(true);
            return ts.Length > 0 ? ts[0] : null;
        }

        static RectTransform Fx(GameRoot g) { return g != null ? g.FxLayer : null; }

        /// <summary>光の粒 (添字で散らす＝決定的): pos から n 個が上へ立ちのぼって消える。1 回 14 個まで (スマホの粒の上限)</summary>
        public static void Motes(RectTransform layer, Vector2 pos, Color color, int n, float rise = 90f, float spread = 70f, float dur = 0.6f)
        {
            if (layer == null || ScreenFx.Instant) return;
            n = Mathf.Clamp(n, 1, 14);
            for (int i = 0; i < n; i++)
            {
                float a = i * 2.39996f;                       // 黄金角
                float r = spread * (0.35f + 0.65f * ((i * 7) % n) / (float)n);
                var p0 = pos + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.5f);
                float sz = 5f + (i % 3) * 2.5f;
                var rt = UiKit.NewRect("mote", layer);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(sz, sz);
                rt.anchoredPosition = p0;
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = MapForest.Radial(); img.color = color; img.raycastTarget = false;
                float delay = (i % 4) * 0.04f, up = rise * (0.7f + 0.3f * ((i * 5) % n) / (float)n);
                Tween.After(delay, () =>
                {
                    if (rt == null) return;
                    Tween.Run(dur, k =>
                    {
                        if (rt == null) return;
                        rt.anchoredPosition = p0 + new Vector2(Mathf.Sin(k * 6f + a) * 6f, up * Tween.Apply(Ease.OutQuad, k));
                        img.color = new Color(color.r, color.g, color.b, color.a * (k < 0.2f ? k / 0.2f : 1f - Mathf.Clamp01((k - 0.45f) / 0.55f)));
                    }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
                });
            }
        }

        /// <summary>白い一閃 (札の上に白の板を重ねて 0.6→0)。紙そのものは染めない＝重ねた板が消える</summary>
        static void WhiteFlash(RectTransform over, float alpha = 0.6f, float dur = 0.14f)
        {
            if (over == null) return;
            var rt = UiKit.NewRect("white", over);
            UiKit.Stretch(rt, 0f, 0f, 0f, 0f);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, alpha); img.raycastTarget = false;
            Tween.Run(dur, k => { if (img != null) img.color = new Color(1f, 1f, 1f, alpha * (1f - k)); }, Ease.OutQuad, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        /// <summary>夜の帯に一言 (遺物の名前・出来事の結果): 右から滑り込み (0.2 秒)、hold の後に消える (hold &lt; 0 なら消さない＝幕が片付ける)</summary>
        public static RectTransform NightLine(RectTransform layer, Vector2 pos, string text, Color ink, int size = 22, float hold = 0.8f, float delay = 0f)
        {
            if (layer == null) return null;
            var rt = UiKit.NewRect("night-line", layer);
            float w = 56f + text.Length * size * 1.1f, h = size + 22f;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = pos;
            var bg = PaperFx.Sheet(rt, PaperFx.NightTag, "paper", Color.white);
            UiKit.Stretch(bg.rectTransform, 0f, 0f, 0f, 0f); bg.raycastTarget = false;
            var rule = UiKit.Pan(rt, new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.7f), "rule");
            rule.raycastTarget = false;
            UiKit.Anchor(rule.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 5f), new Vector2(-14f, 7f));
            var t = UiKit.Deco(rt, text, size, ink, TextAnchor.MiddleCenter);
            t.characterSpacing = 3f; t.raycastTarget = false;
            UiKit.Stretch(t.rectTransform, 0f, 0f, 0f, 4f);
            var cg = rt.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0f; cg.blocksRaycasts = false;
            if (ScreenFx.Instant) { cg.alpha = 1f; return rt; }
            Tween.After(delay, () =>
            {
                if (rt == null) return;
                Tween.Run(0.2f, k => { if (rt == null) return; rt.anchoredPosition = pos + new Vector2(50f * (1f - Tween.Apply(Ease.OutCubic, k)), 0f); cg.alpha = k; }, Ease.Linear, () =>
                {
                    if (hold >= 0f) Tween.After(hold, () => Tween.Run(0.25f, k => { if (cg != null) cg.alpha = 1f - k; }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); }));
                });
            });
            return rt;
        }

        // ---- 1) 報酬の札の儀式 (G1・M06) ----

        /// <summary>返りの進行の記録 (タップで全部を終わりの姿へ飛ばすため。後から来る After/Run は Done を見て何もしない)</summary>
        class Reveal { public RectTransform Cv; public RectTransform Back; public Button Take; public float S; public bool Done; }
        static readonly List<Reveal> _reveals = new List<Reveal>();

        /// <summary>返っている途中の札がある (ScreenFx.Update がタップを見る条件)。組み直しで消えた札は数えない</summary>
        public static bool HasReveals
        {
            get
            {
                for (int i = _reveals.Count - 1; i >= 0; i--)
                {
                    var r = _reveals[i];
                    if (r.Done || r.Cv == null) { _reveals.RemoveAt(i); continue; }
                    return true;
                }
                return false;
            }
        }

        /// <summary>1 枚を終わりの姿へ (裏を消す・大きさを戻す・「取る」を押せるように)。二度目は何もしない</summary>
        static void FinishReveal(Reveal r)
        {
            if (r == null || r.Done) return;
            r.Done = true;
            if (r.Back != null) UnityEngine.Object.Destroy(r.Back.gameObject);
            if (r.Cv != null) r.Cv.localScale = Vector3.one * r.S;
            if (r.Take != null)
            {
                var bm = r.Take.GetComponent<ScreenFx.ButtonMotion>();
                if (bm != null) bm.MarkShown();   // 立ち上がりの button_appear を鳴らさない (card_flip と重ならない)
                r.Take.interactable = true;
            }
        }

        /// <summary>押したら待たせない: 返っている途中の札を全部終わりの姿へ (card_flip は 1 回だけ。輪・粒は省く)。ScreenFx.Update のタップから</summary>
        public static void FinishReveals()
        {
            bool any = false;
            for (int i = 0; i < _reveals.Count; i++)
            {
                var r = _reveals[i];
                if (r.Done || r.Cv == null) continue;
                FinishReveal(r); any = true;
            }
            _reveals.Clear();
            if (any) Audio.KeyPitched("card_flip", 1f, 0.8f);
        }

        /// <summary>
        /// 報酬の札 cv (cell の子・倍率 scale) を裏向きで置き、index×0.12 秒ずらして返す。返り切った瞬間に rarity の色の輪 (C 140／U 200／R 300) と card_flip。
        /// レアは返る前から真鍮の暈が呼吸し、返った瞬間に rare_reveal と全画面の淡い真鍮の閃き・粒 10。アンコモンは小さな暈と粒 4。
        /// take (「取る」) は返っている間は押せない。返りは 1 枚 0.36 秒＝3 枚で 0.6 秒 (自動操縦が押す 1.4 秒より短い)。Instant なら表のまま。
        /// 幕が動いている最中 (ScreenFx.Switch の組み直し) に呼ばれたら、幕が開いて入りが終わる (OpenDur＋EnterDur) まで返りを待つ＝1 枚目の返りが幕の下で終わらない。
        /// タップ (ScreenFx.Update) で全部を終わりの姿へ飛ばせる (FinishReveals)
        /// </summary>
        public static void RevealCard(GameRoot g, RectTransform cell, RectTransform cv, string rarity, int index, Button take, bool flip = true)
        {
            var fx = Fx(g);
            if (cell == null || cv == null) return;
            bool rare = rarity == "rare", unc = rarity == "uncommon";
            Color edge = PaperFx.RarityEdge(rarity ?? "common");
            // 暈 (札の後ろ): レアは真鍮・アンコモンはレア度の色を淡く。呼吸する (UiFlicker)
            if (rare || unc)
            {
                var halo = UiKit.Pan(cell, rare ? new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.22f) : new Color(edge.r, edge.g, edge.b, 0.14f), "halo");
                halo.sprite = MapForest.Radial(); halo.raycastTarget = false;
                var hrt = halo.rectTransform;
                hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);
                hrt.sizeDelta = new Vector2(cv.rect.width * cv.localScale.x * 1.9f, cv.rect.height * cv.localScale.y * 1.5f);
                hrt.anchoredPosition = cv.anchoredPosition;
                hrt.SetAsFirstSibling();
                var fl = halo.gameObject.AddComponent<CampfireScreen.UiFlicker>(); fl.Base = rare ? 0.22f : 0.14f; fl.Amount = 0.5f;
            }
            if (ScreenFx.Instant || fx == null || !flip) return;
            // 裏 (BattleView.CardBack と同じ作り: 夜の紙に Art/ui/cardback)。cv の最後の子として重ねる
            var back = UiKit.NewRect("back", cv);
            UiKit.Stretch(back, 0f, 0f, 0f, 0f);
            var sheet = PaperFx.Sheet(back, PaperFx.Card, "paper", PaperFx.Window);
            UiKit.Stretch(sheet.rectTransform, 0f, 0f, 0f, 0f); sheet.raycastTarget = false;
            var art = Theme.Art("ui", "cardback");
            if (art != null)
            {
                var mask = sheet.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = true;
                var ai = UiKit.NewRect("art", sheet.transform).gameObject.AddComponent<Image>();
                ai.sprite = art; ai.preserveAspect = false; ai.raycastTarget = false;
                UiKit.Stretch(ai.rectTransform, 0f, 0f, 0f, 0f);
            }
            back.SetAsLastSibling();
            if (take != null) take.interactable = false;
            float s = cv.localScale.x;
            // 幕が動いている (決着→報酬の Switch の組み直しの中) なら、幕が開き入りが満ちるまで待ってから返す。同じ相の組み直し (幕なし) は従来どおり
            float lead = ScreenFx.Busy ? ScreenFx.OpenDur + ScreenFx.EnterDur : 0f;
            float delay = lead + FxSettings.Dur(0.12f) * index;
            float d1 = FxSettings.Dur(0.16f), d2 = FxSettings.Dur(0.2f);
            var rec = new Reveal { Cv = cv, Back = back, Take = take, S = s };
            _reveals.Add(rec);
            ScreenFx.EnsureInstance();   // タップで飛ばす口 (ScreenFx.Update) を確かに回す
            Tween.After(delay, () =>
            {
                if (rec.Done || cv == null) return;
                // 前半: 裏が細くなる
                Tween.Run(d1, k => { if (!rec.Done && cv != null) cv.localScale = new Vector3(s * (1f - k), s, 1f); }, Ease.InQuad, () =>
                {
                    if (rec.Done || cv == null) return;
                    if (back != null) UnityEngine.Object.Destroy(back.gameObject);
                    Audio.KeyPitched("card_flip", 1f + 0.05f * index, 0.8f);
                    // 後半: 表が開く
                    Tween.Run(d2, k => { if (!rec.Done && cv != null) cv.localScale = new Vector3(s * k, s, 1f); }, Ease.OutBack, () =>
                    {
                        if (rec.Done || cv == null) return;
                        rec.Done = true;
                        cv.localScale = Vector3.one * s;
                        if (take != null)
                        {
                            var bm = take.GetComponent<ScreenFx.ButtonMotion>();
                            if (bm != null) bm.MarkShown();   // 「取る」が押せるようになっても button_appear は鳴らさない (card_flip と重ならない)
                            take.interactable = true;
                        }
                        var c = Tween.CenterIn(cv, fx);
                        Tween.RingBurst(fx, c, edge, rare ? 300f : unc ? 200f : 140f, 0.4f);
                        if (rare)
                        {
                            Audio.Key("rare_reveal");
                            Tween.ScreenFlash(fx, new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.08f * FxSettings.Flash), 0.3f);
                            Motes(fx, c, PaperFx.BrassLight, 10, 120f, 80f);
                            Tween.Punch(cv, 0.08f, 0.35f);
                        }
                        else if (unc) Motes(fx, c, edge, 4, 80f, 60f);
                    });
                });
            });
        }

        // ---- 2) 遺物 (G7・M12) ----

        /// <summary>遺物を得た: 光の輪 (真鍮 360)・全画面の淡い閃き・札の上に夜の帯の名前 (0.8 秒)・絵が膨らむ・relic_get。0.45 秒後に then (FlyTo へ)。Instant なら即 then</summary>
        public static void RelicGet(GameRoot g, RectTransform cell, string name, Action then)
        {
            var fx = Fx(g);
            if (ScreenFx.Instant || cell == null || fx == null) { then(); return; }
            ScreenFx.LockInput(true);
            var c = Tween.CenterIn(cell, fx);
            Tween.RingBurst(fx, c, PaperFx.BrassLight, 360f, 0.5f);
            Tween.ScreenFlash(fx, new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.07f * FxSettings.Flash), 0.25f);
            Motes(fx, c, PaperFx.BrassLight, 10, 110f, 90f);
            if (!string.IsNullOrEmpty(name)) NightLine(fx, c + new Vector2(0f, cell.rect.height * 0.5f + 40f), name, PaperFx.BrassLight, 24, 0.8f);
            var art = cell.Find("relic-art") as RectTransform;
            if (art != null) Tween.Puff(art, 0.4f, 1.25f);
            Audio.Key("relic_get");
            // cell が消えていたら (組み直し) FlyTo が from==null で then だけ呼び LockInput が残るので、ここで解く (幕が動いている間は幕が握るので重なっても害は無い)
            Tween.After(FxSettings.Dur(0.45f), () => { if (cell == null) ScreenFx.LockInput(false); then(); });
        }

        /// <summary>宝箱が開いた瞬間 (組み直しの後に呼ぶ): 白→真鍮の閃き・箱の上に輪と粒 14・relic_get</summary>
        public static void ChestBurst(GameRoot g, RectTransform chest)
        {
            var fx = Fx(g);
            if (ScreenFx.Instant || fx == null) return;
            Tween.ScreenFlash(fx, new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.1f * FxSettings.Flash), 0.3f);
            if (chest != null)
            {
                var c = Tween.CenterIn(chest, fx);
                Tween.RingBurst(fx, c, PaperFx.BrassLight, 320f, 0.5f);
                Motes(fx, c, PaperFx.BrassLight, 14, 160f, 90f, 0.8f);
            }
            Audio.Key("relic_get");
        }

        // ---- 3) 店 (G8・G14) ----

        /// <summary>
        /// 硬貨 (買った): from の中心から上部バーの G (mana なら魔素) の印へ n 枚の光の玉が 0.05 秒ずらしで山なりに飛ぶ (coin_fly 1 回・着くたび coin_land を少し高く)。
        /// 全部着いたら then。札の飛び (FlyTo 0.45 秒) と同時に始めても先に着く (3 枚で 0.36 秒)。Instant なら即 then
        /// </summary>
        public static void Coins(GameRoot g, RectTransform from, int n, bool mana = false, Action then = null)
        {
            var fx = Fx(g);
            var to = g != null ? g.Anchor(mana ? "mana" : "gold") : null;
            if (ScreenFx.Instant || fx == null || from == null || to == null) { if (then != null) then(); return; }
            n = Mathf.Clamp(n, 1, 6);
            Vector2 src = Tween.CenterIn(from, fx), dst = Tween.CenterIn(to, fx);
            Color col = mana ? PaperFx.Mana : PaperFx.BrassLight;
            Audio.Key(mana ? "mana_land" : "coin_fly", 0.7f);
            int left = n;
            for (int i = 0; i < n; i++)
            {
                int k = i;
                Tween.After(0.05f * i, () =>
                {
                    Tween.Projectile(fx, src + new Vector2((k - (n - 1) * 0.5f) * 10f, 0f), dst, col, 18f, FxSettings.Dur(0.26f), 50f + 14f * (k % 3), () =>
                    {
                        Audio.KeyPitched(mana ? "mana_land" : "coin_land", 1f + 0.05f * k, 0.5f);
                        if (to != null) Tween.Punch(to, 0.08f, 0.2f);
                        if (--left == 0 && then != null) then();
                    });
                });
            }
        }

        /// <summary>払えない値札を押した: 値札が震え、上に「G が足りない」の浮き文字、card_cancel を小さく。輪も閃きも無し (取り消しは静かに)</summary>
        public static void Denied(RectTransform tag, string text = "G が足りない")
        {
            if (tag == null) return;
            Tween.Shake(tag, 6f * Mathf.Max(0.25f, FxSettings.Shake), 0.25f);
            Audio.Key("card_cancel", 0.6f);
            if (ScreenFx.Instant) return;
            var canvas = tag.GetComponentInParent<Canvas>();
            var fx = canvas != null ? canvas.transform.Find("fx") as RectTransform : null;
            if (fx != null) Tween.Float(fx, Tween.CenterIn(tag, fx) + new Vector2(0f, 36f), text, PaperFx.RoseLight, 20, 40f, 0.8f, 260f);
        }

        /// <summary>
        /// 売切の覆いと判 (店の札・ギアの棚・出立の店と同じ形): 黒 0.6 の覆いに紙の判「売切」(朱の墨・−8°)。
        /// animate (今買った品) なら覆いが 0→0.6 (0.2 秒)・判が 1.6→1 で押し当てられ sold_stamp。それ以外は静止で出す
        /// </summary>
        public static void SoldCover(RectTransform over, bool animate)
        {
            if (over == null) return;
            var cover = UiKit.Pan(over, new Color(0f, 0f, 0f, 0.6f), "sold");
            UiKit.Stretch(cover.rectTransform, 0f, 0f, 0f, 0f);
            cover.raycastTarget = false;
            var stamp = UiKit.NewRect("sold-stamp", cover.transform);
            float sw = Mathf.Min(150f, over.rect.width * 0.8f), sh = 54f;
            UiKit.Anchor(stamp, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-sw / 2f, -sh / 2f), new Vector2(sw / 2f, sh / 2f));
            var sImg = stamp.gameObject.AddComponent<Image>();
            sImg.sprite = PaperFx.Tag; sImg.type = Image.Type.Sliced; sImg.pixelsPerUnitMultiplier = 1f; sImg.color = PaperFx.Paper3; sImg.raycastTarget = false;
            var edge = stamp.gameObject.AddComponent<Outline>();
            edge.effectDistance = new Vector2(2f, -2f); edge.useGraphicAlpha = false;
            edge.effectColor = new Color(PaperFx.BadInk.r, PaperFx.BadInk.g, PaperFx.BadInk.b, 0.8f);
            stamp.localRotation = Quaternion.Euler(0f, 0f, -8f);
            var st = UiKit.Deco(stamp, "売切", 30, PaperFx.BadInk, TextAnchor.MiddleCenter);
            st.characterSpacing = 8f; st.raycastTarget = false;
            UiKit.Stretch(st.rectTransform, 0f, 0f, 0f, 0f);
            if (!animate || ScreenFx.Instant) return;
            cover.color = new Color(0f, 0f, 0f, 0f);
            stamp.localScale = Vector3.one * 1.6f;
            var scg = stamp.gameObject.AddComponent<CanvasGroup>(); scg.alpha = 0f; scg.blocksRaycasts = false;
            Tween.Run(0.2f, k => { if (cover != null) cover.color = new Color(0f, 0f, 0f, 0.6f * k); }, Ease.OutQuad);
            Tween.After(0.06f, () =>
            {
                if (stamp == null) return;
                Audio.Key("sold_stamp");
                Tween.Run(0.16f, k => { if (stamp == null) return; stamp.localScale = Vector3.one * (1.6f - 0.6f * Tween.Apply(Ease.OutCubic, k)); scg.alpha = Mathf.Clamp01(k * 3f); }, Ease.Linear, () => { if (stamp != null) Tween.Punch(stamp, 0.06f, 0.2f); });
            });
        }

        // ---- 4) 焚き火 (G2・G3・M07) ----

        /// <summary>休んだ: 上部バーの HP の数字を from→to へ 0.6 秒数え上げ (rest_heal)、札の下に「+N」(苔)。to を覚えさせて地図で二重に出さない</summary>
        public static void HealCount(GameRoot g, int from, int to)
        {
            if (g == null) return;
            ScreenFx.Remember("hp", to);
            var tag = g.Anchor("hp");
            var fx = Fx(g);
            if (ScreenFx.Instant || tag == null || to <= from) return;
            Audio.Key("rest_heal");
            var text = FirstText(tag);
            ScreenFx.CountTo(text, from, to, FxSettings.Dur(0.6f));
            Tween.Punch(tag, 0.1f, 0.4f);
            if (fx != null)
            {
                Tween.Float(fx, Tween.CenterIn(tag, fx) + new Vector2(0f, -30f), "+" + (to - from), PaperFx.MossLight, 26, -40f, 1.0f, 200f);
                Motes(fx, Tween.CenterIn(tag, fx), PaperFx.MossLight, 6, 40f, 40f, 0.5f);
            }
        }

        /// <summary>
        /// 鍛えた (焚き火・店・出来事で共用): 一覧の cell の札の複製が画面の中央へ寄って 1.15 倍 (0.22 秒)、0.11 秒間隔で 3 度白く光って震え (upgrade_shine・音程が上がる)、
        /// 3 打目で鍛えた後の姿 (Upgrade.UpgradeCard＝名前に「+」) に書き換わり、真鍮の輪と粒・「鍛えた」の浮き文字。0.4 秒見せてから上部バーのデッキへ飛び then (手を進める)。
        /// cell が無ければ中央に組んで同じ流れ。鍛えられない札 (engine が拒む) や Instant なら即 then。合計約 1.4 秒
        /// </summary>
        public static void UpgradeShine(GameRoot g, RectTransform cell, CardInstance before, Action then)
        {
            var fx = Fx(g);
            CardInstance after = null;
            try { if (before != null && Upgrade.CanUpgradeCard(before)) after = Upgrade.UpgradeCard(before); } catch (Exception) { }
            if (ScreenFx.Instant || fx == null || after == null) { then(); return; }
            RectTransform card = null;
            if (cell != null) { var found = cell.Find("deck-card"); card = found != null ? found as RectTransform : cell; }
            GameObject clone = card != null ? ScreenFx.CloneForFx(card, fx) : null;
            RectTransform crt;
            if (clone != null) crt = clone.GetComponent<RectTransform>();
            else
            {
                crt = CardView.Build(fx, before, null, true, false, "forge-card");
                crt.anchoredPosition = new Vector2(0f, 20f);
            }
            ScreenFx.LockInput(true);
            // 複製 crt が途中で消える (組み直し＝GameRoot.Rebuild / Presenter.Reset が FxLayer の子を捨てる) 経路の全部で、鍵を解いて手を一度だけ進める。
            // 3 打の After は 3 本とも null を見るので、旗が無いと g.Do が 3 回飛ぶ (2026-11-04 レビューの是正)
            bool done = false;
            Action finish = () =>
            {
                if (done) return; done = true;
                ScreenFx.LockInput(false);
                then();
            };
            Vector2 p0 = crt.anchoredPosition, p1 = new Vector2(0f, 20f);
            float s0 = crt.localScale.x, s1 = UiKit.Phone ? 1.0f : 1.15f;
            float gap = FxSettings.Dur(0.11f);
            Tween.Run(FxSettings.Dur(0.22f), k =>
            {
                if (crt == null) return;
                crt.anchoredPosition = Vector2.Lerp(p0, p1, k);
                crt.localScale = Vector3.one * Mathf.Lerp(s0, s1, k);
            }, Ease.OutCubic, () =>
            {
                if (crt == null) { finish(); return; }
                for (int i = 0; i < 3; i++)
                {
                    int hit = i;
                    Tween.After(gap * i, () =>
                    {
                        if (done) return;
                        if (crt == null) { finish(); return; }
                        Audio.KeyPitched("upgrade_shine", 1f + 0.08f * hit, 0.8f);
                        WhiteFlash(crt, 0.6f, 0.14f);
                        Tween.Shake(crt, 4f * Mathf.Max(0.25f, FxSettings.Shake), 0.1f);
                        Tween.RingBurst(fx, Tween.CenterIn(crt, fx), PaperFx.BrassLight, 160f + 40f * hit, 0.3f);
                        if (hit == 2)
                        {   // 鍛えた後の姿に
                            CardView.Refill(crt, after, null, true, false);
                            var c = Tween.CenterIn(crt, fx);
                            Tween.RingBurst(fx, c, PaperFx.BrassLight, 280f, 0.45f);
                            Tween.ScreenFlash(fx, new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.1f * FxSettings.Flash), 0.3f);
                            Motes(fx, c, PaperFx.BrassLight, 8, 120f, 90f);
                            Tween.Float(fx, c + new Vector2(0f, crt.rect.height * s1 * 0.5f + 30f), "鍛えた", PaperFx.BrassLight, 28, 40f, 0.9f, 240f);
                            Tween.Punch(crt, 0.1f, 0.35f);
                            Tween.After(FxSettings.Dur(0.4f), () =>
                            {
                                if (done) return;
                                if (crt == null) { finish(); return; }
                                var src = crt;
                                ScreenFx.FlyTo(g, src, "deck", finish);   // FlyTo は src を複製してから飛ばす (LockInput を引き継ぐ。finish 側の解錠が重なっても幕が動いている間は無視される)
                                UnityEngine.Object.Destroy(src.gameObject);
                            });
                        }
                    });
                }
            });
        }

        /// <summary>焚き火の火が一拍大きくなる (休んだ): 暈 (UiFlicker) の Base を 0.4 秒で 1.6 倍→戻し・大きさ ×1.3→戻し。glow が無ければ何もしない</summary>
        public static void FireFlare(Image glow, float dur = 0.9f)
        {
            if (glow == null || ScreenFx.Instant) return;
            var fl = glow.GetComponent<CampfireScreen.UiFlicker>();
            float b0 = fl != null ? fl.Base : glow.color.a;
            var rt = glow.rectTransform; var sz0 = rt.sizeDelta;
            Tween.Run(dur, k =>
            {
                if (glow == null) return;
                float w = Mathf.Sin(k * Mathf.PI);            // 山なり
                if (fl != null) fl.Base = b0 * (1f + 0.9f * w);
                rt.sizeDelta = sz0 * (1f + 0.3f * w);
            }, Ease.Linear, () => { if (glow != null) { if (fl != null) fl.Base = b0; rt.sizeDelta = sz0; } });
        }

        // ---- 5) 工房 (G10) ----

        /// <summary>
        /// 合成 (溶ける): 素材 2 枚 (slotA/slotB の札) の複製が結果の札の位置へ 0.3 秒で寄りながら縮んで傾き、重なった瞬間に青緑の閃きと輪 (fuse_melt)、
        /// 結果の札が 1.4→1 で押し当てられ白く光る。0.5 秒見せてからデッキへ飛び then。Instant なら即 then
        /// </summary>
        public static void FuseMelt(GameRoot g, RectTransform a, RectTransform b, RectTransform result, Action then)
        {
            var fx = Fx(g);
            if (ScreenFx.Instant || fx == null || result == null) { then(); return; }
            ScreenFx.LockInput(true);
            var dst = Tween.CenterIn(result, fx);
            var ca = ScreenFx.CloneForFx(a, fx); var cb = ScreenFx.CloneForFx(b, fx);
            var ra = ca != null ? ca.GetComponent<RectTransform>() : null; var rb = cb != null ? cb.GetComponent<RectTransform>() : null;
            Vector2 pa = ra != null ? ra.anchoredPosition : dst, pb = rb != null ? rb.anchoredPosition : dst;
            float sa = ra != null ? ra.localScale.x : 1f, sb = rb != null ? rb.localScale.x : 1f;
            var baseScale = result.localScale;
            result.localScale = Vector3.zero;   // 結果の札は溶け合うまで見せない
            Audio.Key("fuse_melt");
            Tween.Run(FxSettings.Dur(0.3f), k =>
            {
                float e = Tween.Apply(Ease.InQuad, k);
                if (ra != null) { ra.anchoredPosition = Vector2.Lerp(pa, dst, e); ra.localScale = Vector3.one * Mathf.Lerp(sa, sa * 0.6f, e); ra.localRotation = Quaternion.Euler(0f, 0f, -14f * e); }
                if (rb != null) { rb.anchoredPosition = Vector2.Lerp(pb, dst, e); rb.localScale = Vector3.one * Mathf.Lerp(sb, sb * 0.6f, e); rb.localRotation = Quaternion.Euler(0f, 0f, 14f * e); }
            }, Ease.Linear, () =>
            {
                if (ca != null) UnityEngine.Object.Destroy(ca);
                if (cb != null) UnityEngine.Object.Destroy(cb);
                Tween.ScreenFlash(fx, new Color(PaperFx.ManaLight.r, PaperFx.ManaLight.g, PaperFx.ManaLight.b, 0.1f * FxSettings.Flash), 0.3f);
                Tween.RingBurst(fx, dst, PaperFx.Mana, 320f, 0.45f);
                Motes(fx, dst, PaperFx.ManaLight, 10, 110f, 80f);
                if (result == null) { ScreenFx.LockInput(false); then(); return; }
                // 結果の札: 1.4 → 1 の押し当て＋白い一閃
                result.localScale = baseScale * 1.4f;
                WhiteFlash(result, 0.7f, 0.2f);
                Tween.Run(FxSettings.Dur(0.2f), k => { if (result != null) result.localScale = baseScale * (1.4f - 0.4f * Tween.Apply(Ease.OutCubic, k)); }, Ease.Linear, () =>
                {
                    if (result != null) Tween.Punch(result, 0.06f, 0.25f);
                    Tween.After(FxSettings.Dur(0.5f), () => { if (result == null) { ScreenFx.LockInput(false); then(); return; } ScreenFx.FlyTo(g, result, "deck", then); });
                });
            });
        }

        // ---- 6) 出来事 (G6) ----

        /// <summary>結果の 1 行 (文と色)</summary>
        public struct Outcome { public string Text; public Color Ink; public bool Gain; }

        /// <summary>
        /// 手の前後のランの差分を人の言葉に (表示層の推定。engine は結果の文を返さない): G・HP・最大 HP・札の増減 (負傷・烙印は名指し)・鍛えた枚数・遺物。
        /// 何も変わっていなければ「何も起きなかった」の 1 行 (賭けの外れ)
        /// </summary>
        public static List<Outcome> Diff(RunState prev, RunState now)
        {
            var list = new List<Outcome>();
            if (prev == null || now == null) return list;
            int dg = now.Gold - prev.Gold;
            if (dg != 0) list.Add(new Outcome { Text = (dg > 0 ? "+" : "−") + Mathf.Abs(dg) + " G", Ink = dg > 0 ? PaperFx.BrassLight : PaperFx.RoseLight, Gain = dg > 0 });
            int dmax = now.MaxHp - prev.MaxHp;
            if (dmax != 0) list.Add(new Outcome { Text = "最大 HP " + (dmax > 0 ? "+" : "−") + Mathf.Abs(dmax), Ink = dmax > 0 ? PaperFx.MossLight : PaperFx.RoseLight, Gain = dmax > 0 });
            int dhp = now.Hp - prev.Hp;
            if (dhp != 0) list.Add(new Outcome { Text = "HP " + (dhp > 0 ? "+" : "−") + Mathf.Abs(dhp), Ink = dhp > 0 ? PaperFx.MossLight : PaperFx.RoseLight, Gain = dhp > 0 });
            // 札: Uid で前後を突き合わせる
            var prevUids = new Dictionary<string, CardInstance>();
            if (prev.Deck != null) foreach (var c in prev.Deck) if (c != null && c.Uid != null) prevUids[c.Uid] = c;
            var nowUids = new HashSet<string>();
            int added = 0, upgraded = 0, wounds = 0, brands = 0;
            var addedNames = new List<string>();
            if (now.Deck != null)
                foreach (var c in now.Deck)
                {
                    if (c == null || c.Uid == null) continue;
                    nowUids.Add(c.Uid);
                    CardInstance old;
                    if (!prevUids.TryGetValue(c.Uid, out old))
                    {
                        string id = c.Def != null ? c.Def.Id : null;
                        if (id == "status_wound") wounds++;
                        else if (id == "status_brand" || id == "status_guilt") brands++;
                        else { added++; if (c.Def != null) addedNames.Add(c.Def.Name); }
                    }
                    else if (old.Def != null && c.Def != null && old.Def.Name != c.Def.Name) upgraded++;
                }
            int removed = 0;
            foreach (var kv in prevUids) if (!nowUids.Contains(kv.Key)) removed++;
            if (added > 0) list.Add(new Outcome { Text = added <= 2 ? string.Join("・", addedNames.ToArray()) + " を得た" : "札を " + added + " 枚得た", Ink = PaperFx.Paper, Gain = true });
            if (wounds > 0) list.Add(new Outcome { Text = "負傷 +" + wounds, Ink = PaperFx.RoseLight, Gain = false });
            if (brands > 0) list.Add(new Outcome { Text = "烙印 +" + brands, Ink = PaperFx.PlumLight, Gain = false });
            if (removed > 0) list.Add(new Outcome { Text = "札を " + removed + " 枚取り除いた", Ink = PaperFx.Paper, Gain = true });
            if (upgraded > 0) list.Add(new Outcome { Text = upgraded == 1 ? "1 枚を鍛えた" : upgraded + " 枚を鍛えた", Ink = PaperFx.BrassLight, Gain = true });
            int drel = (now.Relics != null ? now.Relics.Count : 0) - (prev.Relics != null ? prev.Relics.Count : 0);
            if (drel > 0 && now.Relics != null)
            {
                string nm = null;
                try { var rd = Content.GetRelicDef(now.Relics[now.Relics.Count - 1]); nm = rd != null ? rd.Name : null; } catch (Exception) { }
                list.Add(new Outcome { Text = nm != null ? "遺物「" + nm + "」を得た" : "遺物を得た", Ink = PaperFx.BrassLight, Gain = true });
            }
            else if (drel < 0) list.Add(new Outcome { Text = "遺物を失った", Ink = PaperFx.RoseLight, Gain = false });
            if (list.Count == 0) list.Add(new Outcome { Text = "何も起きなかった", Ink = PaperFx.PaperDim, Gain = true });
            return list;
        }

        /// <summary>
        /// 出来事の結果を見せる (手の後・幕が閉じる前の窓＝ScreenFx.DeferNextSwitch と対): 画面中央の下に夜の札を 0.12 秒ずらしで右から滑り込ませる (消さない＝幕が片付ける)。
        /// 得たら event_gain・失ったら event_loss。G・HP の値は覚えさせて次の画面で二重に出さない。返り値は見せるのに要る秒数
        /// </summary>
        public static float ShowOutcome(GameRoot g, RunState prev, RunState now)
        {
            var fx = Fx(g);
            if (g == null || now == null) return 0f;
            ScreenFx.Remember("gold", now.Gold);
            ScreenFx.Remember("hp", now.Hp);
            if (ScreenFx.Instant || fx == null) return 0f;
            var lines = Diff(prev, now);
            bool gain = false, loss = false;
            float y0 = UiKit.Phone ? -150f : -210f;
            for (int i = 0; i < lines.Count && i < 6; i++)
            {
                var o = lines[i];
                NightLine(fx, new Vector2(0f, y0 - i * 48f), o.Text, o.Ink, 22, -1f, FxSettings.Dur(0.12f) * i);
                if (o.Gain) gain = true; else loss = true;
            }
            if (loss) Audio.Key("event_loss"); else if (gain) Audio.Key("event_gain");
            return FxSettings.Dur(0.9f + 0.12f * Mathf.Min(lines.Count, 6));
        }
    }
}
