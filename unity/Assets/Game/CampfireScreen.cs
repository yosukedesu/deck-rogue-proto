// CampfireScreen.cs — 焚き火 (M3・2026-09-07): 「休む」と「鍛える」の二択を大きな札で。鍛えるはデッキのグリッドから選ぶ
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class CampfireScreen
    {
        public static void Build(GameRoot g, RectTransform root)
        {
            var run = g.Rs;
            // 案 A「静かな二択」(§11・2026-10-29 ユーザー裁定): 背景は PixelLab の一枚絵 (400×224 を 5 倍・描かれた炎は熾火に直してある)。
            // その熾火の上に炎のコマ (act1_flame) を重ね、明かりの暈が脈打ち、火の粉が立ちのぼる。主人公は出さない。
            // 一枚絵が無ければ今までの箱庭 (小物の場面)。棚 (鍛える・取り除く) を開いている間は暗幕 0.5
            BattleScreen.BuildBackground(root, run.Act);
            bool quiet = g.SubMode == null;
            bool painted = Backdrop(root, "bd_campfire", quiet ? 0f : 0.5f);
            if (quiet && painted) FireOverlay(root);
            else if (quiet) Scene(g, root, run);
            RunUi.TopBar(g, root, "焚き火");
            RunUi.Message(g, root);

            int allowed = DeckRogue.Engine.Run.CampfireForgeAllowed(run);
            int remain = Math.Max(0, allowed - run.CampfireUpgradesUsed);
            int heal = (int)Math.Floor(run.MaxHp * run.CampfireRatio);
            bool noRest = false; string noRestName = "レリック";
            for (int i = 0; i < run.Relics.Count; i++)
            {
                try { var rd = Content.GetRelicDef(run.Relics[i]); if (rd.Bonus != null && rd.Bonus.NoRest == true) { noRest = true; noRestName = rd.Name; } }
                catch (Exception) { }
            }
            bool restHeals = !noRest && run.CampfireUpgradesUsed == 0;
            // レリック限定の第3選択肢 (2026-09-12 本家形): 発掘 (鶴嘴) / 鍛錬 (重石) / 取り除く (煙管)。融合の鎚は鍛えられない
            var opt = DeckRogue.Engine.Run.CampfireOptions(run);
            bool fresh = run.CampfireUpgradesUsed == 0;
            if (!opt.Forge) remain = 0;

            if (g.SubMode == "remove")
            {
                RunUi.Heading(root, "取り除く", "安らぎの煙管: デッキの1枚を永久に取り除く (休む/鍛えるとは排他)");
                var area = UiKit.NewRect("remove", root);
                RunUi.PickArea(root, area);
                RunUi.CardGrid(g, area, run.Deck,
                    delegate (int i, CardInstance c) { return "取り除く"; },
                    delegate (int i, CardInstance c) { return run.Deck.Count > 5; },
                    delegate (int i) { Audio.Ui("remove"); g.Do(new RunCommand_CampfireRemove { Index = i }); },
                    400f, pickKey: "remove", confirmRoot: root);
                RunUi.BackButton(root, "戻る", delegate { g.SubMode = null; g.Rebuild(); });
                return;
            }

            if (g.SubMode == "forge")
            {
                RunUi.Heading(root, "鍛える", UiKit.Phone ? "1枚選ぶ。「鍛えた後を見る」で全部の札が鍛えた後の姿に" : "1枚選ぶ。札に触れると元と鍛えた後が並ぶ（長押しで拡大）");
                // PC は見出しの下に「元 → 鍛えた後」の並び (ホバー)。スマホは並びを出さず一面の棚＋「鍛えた後を見る」のチェック (2026-09-16 案A・ユーザー「本家と同じトグル」)
                var preview = UiKit.Phone ? null : ForgePreviewArea(root);
                var area = UiKit.NewRect("forge", root);
                RunUi.PickArea(root, area, UiKit.Phone ? 0f : ForgePreviewH);
                RunUi.CardGrid(g, area, run.Deck,
                    delegate (int i, CardInstance c) { return Upgrade.CanUpgradeCard(c) ? "鍛える" : null; },
                    delegate (int i, CardInstance c) { return Upgrade.CanUpgradeCard(c); },
                    delegate (int i) { Audio.Ui("upgrade"); g.Do(new RunCommand_CampfireUpgrade { Index = i }); },
                    400f, pickKey: "forge", confirmRoot: root);
                AttachUpgradeTips(area, run.Deck);
                if (preview != null) AttachForgePreview(g, area, run.Deck, preview);
                RunUi.BackButton(root, "戻る", delegate { g.SubMode = null; g.Rebuild(); });
                return;
            }

            int extra = (opt.Dig && fresh ? 1 : 0) + (opt.TrainLeft > 0 && fresh ? 1 : 0) + (opt.Remove && fresh ? 1 : 0);
            // 案 A「静かな二択」: 見出しは出さない。下の帯 (夜色の勾配) に大きな字の選択肢を横に並べ、帯の上に一行の案内
            int count = 2 + extra;
            var cs = BattleScreen.CanvasSize(root);
            bool ph = UiKit.Phone;
            string guideText = extra > 0 ? "焚き火で一息 ・ どれか 1 つ ・ 鍛えた後は回復なしで立ち去る" : "焚き火で一息 ・ どちらか 1 つ ・ 鍛えた後は回復なしで立ち去る";
            // 下の帯: 長く淡い勾配＋短く濃い勾配 (Linear では α が薄く見えるので二重に)。火の熾 (絵の下端近く) の下に字を置く
            Band(root, "campfire-band-soft", ph ? 200f : 300f, 0.7f);
            Band(root, "campfire-band-deep", ph ? 120f : 170f, 0.95f);
            float optH = ph ? 116f : 150f, optBottom = ph ? 10f : 18f;
            if (ph) RunUi.Heading(root, "焚き火", guideText);   // スマホは案内を上部バーの札に畳む (帯の上は火)
            else
            {
                var guide = UiKit.Txt(root, guideText, 14, PaperFx.PaperDim, TextAnchor.MiddleCenter);
                guide.characterSpacing = 2f; guide.raycastTarget = false;
                guide.outlineWidth = 0.15f; guide.outlineColor = new Color(0f, 0f, 0f, 0.8f);
                UiKit.Anchor(guide.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, optBottom + optH + 8f), new Vector2(0f, optBottom + optH + 32f));
            }
            float w = Mathf.Min(ph ? 460f : 560f, (cs.x - 80f) / count);
            float step = ph && count == 2 ? 620f : w;   // スマホの二択は火の左右に (火は画面の真ん中の下)
            float x0 = -((count - 1) * step) / 2f;
            int slot = 0;
            Vector2 Pos() { return new Vector2(x0 + (slot++) * step, 0f); }   // 列の間の真鍮の点は細い枠 (Option) に置き換えた

            // 休む
            // 休めない理由はレリックの名前で (2026-09-14 人間ラン#11: 古根の杯を持ったまま HP15 で「休む」を選んで回復ゼロ → ボスで敗北)
            var rest = Option(root, "bedroll", "休む", restHeals ? "HP を " + heal + " 回復\n（最大 HP の " + (int)Math.Round(run.CampfireRatio * 100) + "%）" : noRest ? noRestName + ": 焚き火で休めない" : "すでに鍛えたので回復なし",
                restHeals ? "今の HP " + run.Hp + " → " + Math.Min(run.MaxHp, run.Hp + heal) : "回復なしで立ち去る (HP " + run.Hp + "/" + run.MaxHp + " のまま)",
                Pos(), delegate { Audio.Ui("rest"); g.Do(new RunCommand_CampfireRest()); }, true, w);
            // 鍛える
            Option(root, "anvil", "鍛える", !opt.Forge ? "融合の鎚: 焚き火では鍛えられない" : remain > 0 ? "デッキの 1 枚を鍛える\n（数値が伸びる・コストが下がる）" : "この焚き火ではもう鍛えられない",
                remain > 0 ? "残り " + remain + " 回 ・ 鍛えた後を見てから決められる" : "", Pos(),
                delegate { g.SubMode = "forge"; g.Rebuild(); }, remain > 0, w);
            if (opt.Dig && fresh)
                Option(root, "chest", "発掘", "発掘の鶴嘴: レリックを1個掘る", "休む・鍛えるとは排他", Pos(),
                    delegate { Audio.Ui("pick_relic"); g.Do(new RunCommand_CampfireDig()); }, true, w);
            if (opt.TrainLeft > 0 && fresh)
                Option(root, "growth", "鍛錬", "重石: 以後の戦闘開始時の成長+1", "現在 +" + DeckRogue.Engine.Run.RelicStateOf(run, "train") + "・あと " + opt.TrainLeft + " 回", Pos(),
                    delegate { Audio.Ui("upgrade"); g.Do(new RunCommand_CampfireTrain()); }, true, w);
            if (opt.Remove && fresh)
                Option(root, "skull", "取り除く", "安らぎの煙管: デッキの1枚を永久に除去", "休む・鍛えるとは排他", Pos(),
                    delegate { g.SubMode = "remove"; g.Rebuild(); }, run.Deck.Count > 5, w);
        }

        /// <summary>場面 (§10-3「場面は舞台で組む」): 道の真ん中に焚き火 (PixelLab 48×48・炎のコマは props/anim)・両側に丸太・左の丸太にこのは (座りの絵があればそれ、無ければ立ち絵)。
        /// 小物は UI の矩形に追従する板 (Stage.BindUnit)。火には点光源 (揺らぐ)。1 ドット＝3px＝舞台と同じ粒 (スマホは ArtScale で縮む)</summary>
        static void Scene(GameRoot g, RectTransform root, RunState run)
        {
            // 2 巡目 (§10-6 ユーザー「本家のパクリ。キャラは出さず、落ち着いた焚き火のおしゃれな雰囲気に。アニメーションとか」):
            // 主人公は出さない。火そのものを主役に: 大きく (1 ドット＝6px)・炎のコマ・火の粉が立ちのぼる・明かりが脈打つ。丸太と石は火を囲む
            float cw = BattleScreen.CanvasSize(root).x;
            float dot = 6f * BattleScreen.ArtScale;
            var log = Theme.Art("props", "act1_log");
            var fire = Theme.Art("props", "act1_bonfire");
            var rock = Theme.Art("props", "act1_rock_small") ?? Theme.Art("props", "act1_stone");
            if (log != null)
            {
                Prop(root, "scene-log-l", log, Stage.SceneSeat("scene-log-l", -6.2f, 1.1f), cw, 80f * dot * 0.5f);
                Prop(root, "scene-log-r", log, Stage.SceneSeat("scene-log-r", 1.0f, 0.5f), cw, 80f * dot * 0.5f);
            }
            if (rock != null)
            {
                Prop(root, "scene-rock-a", rock, Stage.SceneSeat("scene-rock-a", -4.6f, 0.2f), cw, rock.rect.width * dot * 0.7f);
                Prop(root, "scene-rock-b", rock, Stage.SceneSeat("scene-rock-b", -0.6f, 1.0f), cw, rock.rect.width * dot * 0.6f);
            }
            if (fire != null)
            {
                var feet = Stage.SceneSeat("scene-fire", -2.6f, 0.6f);
                Prop(root, "scene-fire", fire, feet, cw, 48f * dot);
                Stage.AttachLight("scene-fire", PaperFx.Ember, 7f, 11f, 0.4f, 0.16f);
                Stage.AttachGlow("scene-fire", new Color(PaperFx.Ember.r, PaperFx.Ember.g, PaperFx.Ember.b, 0.4f), 6f, 0.3f);
                // 火の粉 (UI の小さな光が火の上から立ちのぼる)
                var sp = UiKit.NewRect("sparks", root);
                sp.anchorMin = sp.anchorMax = new Vector2(0.5f, 0f);
                sp.anchoredPosition = new Vector2(feet.x - cw / 2f, feet.y + 20f * BattleScreen.ArtScale);
                sp.sizeDelta = Vector2.zero;
                var sk = sp.gameObject.AddComponent<SceneSparks>();
                sk.Spread = 26f * BattleScreen.ArtScale; sk.Rise = 180f * BattleScreen.ArtScale;
            }
        }

        /// <summary>火の粉 (2 巡目): 0.09 秒ごとに小さな琥珀の点を火の上に出し、揺れながら 1.2〜2.0 秒で立ちのぼって消える。最大 28 個。決定的な撮影では Tween.UnscaledDt で進む</summary>
        public class SceneSparks : MonoBehaviour
        {
            public float Spread = 26f, Rise = 180f;
            readonly List<Image> _live = new List<Image>();
            readonly List<float> _t0 = new List<float>(), _dur = new List<float>(), _x0 = new List<float>(), _ph = new List<float>();
            float _t, _next;
            System.Random _rng = new System.Random(7);
            void Update()
            {
                _t += Tween.UnscaledDt;
                if (_t >= _next && _live.Count < 28)
                {
                    _next = _t + 0.09f;
                    var go = new GameObject("spark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    go.transform.SetParent(transform, false);
                    var img = go.GetComponent<Image>();
                    img.sprite = MapForest.Radial(); img.raycastTarget = false;
                    bool bright = _rng.NextDouble() < 0.35;
                    img.color = bright ? PaperFx.BrassLight : PaperFx.Ember;
                    var rt = img.rectTransform;
                    float sz = 3f + (float)_rng.NextDouble() * 4f;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(sz, sz);
                    float x0 = ((float)_rng.NextDouble() * 2f - 1f) * Spread;
                    rt.anchoredPosition = new Vector2(x0, 0f);
                    _live.Add(img); _t0.Add(_t); _dur.Add(1.2f + (float)_rng.NextDouble() * 0.8f); _x0.Add(x0); _ph.Add((float)_rng.NextDouble() * 6.28f);
                }
                for (int i = _live.Count - 1; i >= 0; i--)
                {
                    var img = _live[i];
                    if (img == null) { _live.RemoveAt(i); _t0.RemoveAt(i); _dur.RemoveAt(i); _x0.RemoveAt(i); _ph.RemoveAt(i); continue; }
                    float u = (_t - _t0[i]) / _dur[i];
                    if (u >= 1f) { Destroy(img.gameObject); _live.RemoveAt(i); _t0.RemoveAt(i); _dur.RemoveAt(i); _x0.RemoveAt(i); _ph.RemoveAt(i); continue; }
                    float y = Rise * (1f - (1f - u) * (1f - u));
                    float x = _x0[i] + Mathf.Sin(u * 5f + _ph[i]) * 10f * (0.3f + u);
                    img.rectTransform.anchoredPosition = new Vector2(x, y);
                    var c = img.color; c.a = u < 0.15f ? u / 0.15f : 1f - Mathf.Clamp01((u - 0.5f) / 0.5f); img.color = c;
                }
            }
        }

        /// <summary>小物の板: 足元を座席の画面の点 (キャンバスの単位・左下原点) に置いた矩形 (絵の整数倍) を舞台に結ぶ。Image は BindUnit が隠す</summary>
        static void Prop(RectTransform root, string key, Sprite s, Vector2 feet, float canvasW, float width)
        {
            var rt = UiKit.NewRect(key, root);
            PaperFx.FitPixel(rt, s, feet.x - canvasW / 2f, feet.y, width);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s; img.preserveAspect = true; img.raycastTarget = false;
            Stage.BindUnit(key, rt, img, s);
        }

        /// <summary>一枚絵の背景 (§11): Art/scenes/<name> (400×224) を 5 倍 (2000×1120) で中央に置き 1920×1080 からはみ出す 40／20 を切る。dim &gt; 0 なら暗幕。絵が無ければ false</summary>
        public static bool Backdrop(RectTransform root, string name, float dim)
        {
            var art = Theme.Art("scenes", name);
            if (art == null) return false;
            var img = new GameObject("backdrop", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(root, false);
            img.sprite = art; img.preserveAspect = false; img.raycastTarget = false;
            UiKit.PixelArt(img);
            var cs = BattleScreen.CanvasSize(root);
            float k = Mathf.Max(cs.x / art.rect.width, cs.y / art.rect.height);   // 画面を覆う倍率 (PC 5・スマホ 3)
            float w = art.rect.width * k, h = art.rect.height * k;
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.sizeDelta = new Vector2(w, h);
            img.rectTransform.anchoredPosition = Vector2.zero;
            if (dim > 0f)
            {
                var d = UiKit.Pan(root, new Color(0f, 0f, 0f, dim), "dim");
                d.raycastTarget = false;
                UiKit.Stretch(d.rectTransform, 0f, 0f, 0f, 0f);
            }
            return true;
        }

        /// <summary>一枚絵の中の点 (絵の 400×224 の座標) をキャンバスの座標 (左下原点) へ</summary>
        static Vector2 BackdropPoint(RectTransform root, float ax, float ay, float artW = 400f, float artH = 224f)
        {
            var cs = BattleScreen.CanvasSize(root);
            float k = Mathf.Max(cs.x / artW, cs.y / artH);
            float x = cs.x / 2f + (ax - artW / 2f) * k, y = cs.y / 2f - (ay - artH / 2f) * k;
            return new Vector2(x, y);
        }

        /// <summary>焚き火 A の火: 一枚絵の熾火 (絵の (205, 178)) の上に炎のコマ 48×30 を 5 倍で重ね、明かりの暈が脈打ち、火の粉が立ちのぼる</summary>
        static void FireOverlay(RectTransform root)
        {
            var cs = BattleScreen.CanvasSize(root);
            float k = Mathf.Max(cs.x / 400f, cs.y / 224f);
            var pit = BackdropPoint(root, 205f, 178f);
            // 暈 (脈打つ)
            var glow = UiKit.Pan(root, new Color(PaperFx.Ember.r, PaperFx.Ember.g, PaperFx.Ember.b, 0.26f), "fire-glow");
            glow.sprite = MapForest.Radial(); glow.raycastTarget = false;
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0f, 0f);
            glow.rectTransform.sizeDelta = new Vector2(190f * k, 120f * k);
            glow.rectTransform.anchoredPosition = pit + new Vector2(0f, 14f * k);
            var fl = glow.gameObject.AddComponent<UiFlicker>(); fl.Base = 0.26f; fl.Amount = 0.22f;
            // 炎 (コマ)
            var frames = new List<Sprite>();
            for (int i = 0; i < 8; i++) { var s = Theme.Art("props/anim", "act1_flame_idle_" + i); if (s == null) break; frames.Add(s); }
            var flame0 = frames.Count > 0 ? frames[0] : Theme.Art("props", "act1_flame");
            if (flame0 != null)
            {
                var rt = UiKit.NewRect("flame", root);
                PaperFx.FitPixel(rt, flame0, pit.x - cs.x / 2f, pit.y - 2f * k, flame0.rect.width * k);
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = flame0; img.preserveAspect = true; img.raycastTarget = false;
                UiKit.PixelArt(img);
                if (frames.Count > 1) { var fb = rt.gameObject.AddComponent<Flipbook>(); fb.Frames = frames; fb.Fps = 7f; }
            }
            // 火の粉
            var sp = UiKit.NewRect("sparks", root);
            sp.anchorMin = sp.anchorMax = new Vector2(0f, 0f);
            sp.anchoredPosition = pit + new Vector2(0f, 10f * k);
            sp.sizeDelta = Vector2.zero;
            var sk = sp.gameObject.AddComponent<SceneSparks>();
            sk.Spread = 5f * k; sk.Rise = 44f * k;
        }

        /// <summary>UI のコマ送り (炎): Frames を Fps で回す。決定的な撮影では Tween.UnscaledDt で進む</summary>
        public class Flipbook : MonoBehaviour
        {
            public List<Sprite> Frames; public float Fps = 8f;
            Image _img; float _t; int _i;
            void Awake() { _img = GetComponent<Image>(); }
            void Update()
            {
                if (_img == null || Frames == null || Frames.Count < 2) return;
                _t += Tween.UnscaledDt;
                if (_t >= 1f / Fps) { _t = 0f; _i = (_i + 1) % Frames.Count; _img.sprite = Frames[_i]; }
            }
        }

        /// <summary>UI の明かりの揺らぎ (暈の α)</summary>
        public class UiFlicker : MonoBehaviour
        {
            public float Base = 0.3f, Amount = 0.2f;
            Image _img; float _t;
            void Awake() { _img = GetComponent<Image>(); _t = 1.7f; }
            void Update()
            {
                if (_img == null) return;
                _t += Tween.UnscaledDt;
                float w = 0.6f * Mathf.Sin(_t * 7.3f) + 0.4f * Mathf.Sin(_t * 2.1f + 1.3f);
                var c = _img.color; c.a = Base * (1f + Amount * w); _img.color = c;
            }
        }

        /// <summary>下の帯 (透明→夜の黒の勾配。高さ h・下端の α)</summary>
        static void Band(RectTransform root, string key, float h, float alpha)
        {
            var band = UiKit.Pan(root, Color.white, "band");
            var c = Color.Lerp(PaperFx.Night, Color.black, 0.5f);
            band.sprite = ThemeFx.Gradient(key, new Color(c.r, c.g, c.b, 0f), new Color(c.r, c.g, c.b, alpha));
            band.raycastTarget = false;
            UiKit.Anchor(band.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, h));
        }

        /// <summary>選択肢の間の真鍮の点</summary>
        static void Dot(RectTransform root, float x, float y)
        {
            var d = UiKit.Pan(root, PaperFx.Brass, "dot");
            d.sprite = MapForest.Radial(); d.raycastTarget = false;
            d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            d.rectTransform.sizeDelta = new Vector2(12f, 12f);
            d.rectTransform.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>
        /// 案 A「静かな二択」の選択肢 (§11): 下の帯の中の 1 列＝大きな字の名前 (40・字間広め)・一行・小さな行。札も箱も無い。
        /// 押せる列はホバーで名前が真鍮になり下線が出る (ButtonMotion の 1.03 も)。押せない列は灰
        /// </summary>
        static RectTransform Option(RectTransform root, string icon, string title, string line1, string line2, Vector2 pos, Action onClick, bool enabled, float w = 560f)
        {
            bool ph = UiKit.Phone;
            float h = ph ? 116f : 150f;
            var cell = UiKit.NewRect("opt-" + title, root);
            cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 0f);
            cell.sizeDelta = new Vector2(w, h);
            cell.anchoredPosition = new Vector2(pos.x, (ph ? 10f : 18f) + h / 2f);
            var hit = UiKit.Pan(cell, new Color(0f, 0f, 0f, 0f), "hit");
            hit.raycastTarget = enabled;
            UiKit.Stretch(hit.rectTransform, 0f, 0f, 0f, 0f);
            // 細い枠 (採点 3.5「選択肢に細めの枠を囲ってあげると押しやすいかも」): 淡い紙色の線・押せない列はさらに薄く・ホバーで真鍮
            var frameC = enabled ? new Color(PaperFx.PaperDim.r, PaperFx.PaperDim.g, PaperFx.PaperDim.b, 0.6f) : new Color(PaperFx.PaperDim.r, PaperFx.PaperDim.g, PaperFx.PaperDim.b, 0.25f);
            var frame = PaperFx.Sheet(cell, PaperFx.GhostFrame, "frame", frameC);
            frame.raycastTarget = false;
            UiKit.Stretch(frame.rectTransform, ph ? 6f : 10f, ph ? 6f : 10f, 0f, 0f);
            string spaced = title.Length <= 3 ? string.Join(" ", title.ToCharArray()) : title;   // 「休 む」「鍛 え る」
            var t = UiKit.Deco(cell, spaced, ph ? 30 : 40, enabled ? PaperFx.Paper : PaperFx.PaperDim, TextAnchor.MiddleCenter);
            t.characterSpacing = 4f; t.raycastTarget = false;
            t.outlineWidth = 0.15f; t.outlineColor = new Color(0f, 0f, 0f, 0.8f);
            UiKit.Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, ph ? -44f : -58f), new Vector2(0f, ph ? -2f : -4f));
            var line = UiKit.Pan(cell, PaperFx.Brass, "underline");
            line.raycastTarget = false;
            UiKit.Anchor(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-90f, ph ? -47f : -62f), new Vector2(90f, ph ? -44f : -59f));
            line.enabled = false;
            var l1 = UiKit.Txt(cell, line1.Replace("\n", " "), ph ? 13 : 15, PaperFx.PaperDim, TextAnchor.MiddleCenter);
            l1.raycastTarget = false; l1.textWrappingMode = TextWrappingModes.NoWrap; l1.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Anchor(l1.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, ph ? -72f : -92f), new Vector2(-8f, ph ? -50f : -68f));
            var l2 = UiKit.Txt(cell, line2, ph ? 12 : 13, new Color(PaperFx.PaperDim.r, PaperFx.PaperDim.g, PaperFx.PaperDim.b, 0.75f), TextAnchor.MiddleCenter);
            l2.raycastTarget = false; l2.textWrappingMode = TextWrappingModes.NoWrap; l2.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Anchor(l2.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, ph ? -100f : -116f), new Vector2(-8f, ph ? -74f : -94f));   // スマホは 26 の高さ (20 だと 13px の行が入らず Ellipsis で丸ごと消えた)
            if (enabled)
            {
                var btn = hit.gameObject.AddComponent<Button>();
                btn.targetGraphic = hit; btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(delegate { Audio.Ui("click"); onClick(); });
                hit.gameObject.AddComponent<ScreenFx.ButtonMotion>();
                var et = hit.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                enter.callback.AddListener(delegate { t.color = PaperFx.BrassLight; line.enabled = true; frame.color = new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.95f); });
                et.triggers.Add(enter);
                var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                exit.callback.AddListener(delegate { t.color = PaperFx.Paper; line.enabled = false; frame.color = frameC; });
                et.triggers.Add(exit);
            }
            return cell;
        }

        /// <summary>本家 Smith の「元 → 鍛えた後」の並び (2026-09-09): 見出しの下に横 800×高 ForgePreviewH の場所を取り、札に触れると2枚を並べる</summary>
        public const float ForgePreviewH = 262f;
        public static RectTransform LastPreviewArea;

        public static RectTransform ForgePreviewArea(RectTransform root)
        {
            var area = UiKit.NewRect("forgePreview", root);
            // スマホは右の列に置く (見出しの下に横 840 を取ると一覧の高さが残らない。2026-09-14)
            if (UiKit.Phone) UiKit.Anchor(area, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-440f, -(RunUi.TopH + 100f + ForgePreviewH)), new Vector2(-24f, -(RunUi.TopH + 100f)));
            else UiKit.Anchor(area, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-420f, -(RunUi.TopH + 100f + ForgePreviewH)), new Vector2(420f, -(RunUi.TopH + 100f)));
            var hint = UiKit.Txt(area, "札に触れると、元の札と鍛えた後の札がここに並ぶ", 15, UiKit.ColDim, TextAnchor.MiddleCenter);
            hint.outlineWidth = 0.3f; hint.outlineColor = new Color(0.05f, 0.03f, 0.06f, 0.95f);
            UiKit.Stretch(hint.rectTransform, 0f, 0f, 0f, 0f);
            LastPreviewArea = area;
            return area;
        }

        public static void ShowForgePair(GameRoot g, RectTransform area, CardInstance c)
        {
            if (area == null || c == null) return;
            for (int i = area.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(area.GetChild(i).gameObject);
            float sc = UiKit.Phone ? 0.62f : 0.85f;   // スマホは右の列 (幅 416) に2枚を並べる
            float gapHalf = UiKit.Phone ? 30f : 56f;
            float w = CardView.W * sc, h = CardView.H * sc;
            var left = UiKit.NewRect("orig", area);
            left.anchorMin = left.anchorMax = new Vector2(0.5f, 0.5f);
            left.sizeDelta = new Vector2(w, h);
            left.anchoredPosition = new Vector2(-w / 2f - gapHalf, 0f);
            var cvA = CardView.Build(left, c, null, true, false, "orig-card");
            cvA.localScale = Vector3.one * sc;
            var arrow = UiKit.Deco(area, "→", 44, UiKit.ColText, TextAnchor.MiddleCenter);
            arrow.outlineWidth = 0.25f; arrow.outlineColor = new Color(0.05f, 0.03f, 0.06f, 0.95f);
            UiKit.Anchor(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-40f, -30f), new Vector2(40f, 30f));
            CardInstance up = null;
            try { if (Upgrade.CanUpgradeCard(c)) up = Upgrade.UpgradeCard(c); } catch (Exception) { }
            if (up != null)
            {
                var right = UiKit.NewRect("upgraded", area);
                right.anchorMin = right.anchorMax = new Vector2(0.5f, 0.5f);
                right.sizeDelta = new Vector2(w, h);
                right.anchoredPosition = new Vector2(w / 2f + gapHalf, 0f);
                var cvB = CardView.Build(right, up, null, true, false, "upgraded-card");
                cvB.localScale = Vector3.one * sc;
                Tween.Punch(right, 0.06f, 0.4f);
            }
            else
            {
                bool upg = false;
                try { upg = Upgrade.IsUpgraded(c); } catch (Exception) { }
                var note = UiKit.Txt(area, upg ? "鍛え済み" : "この札は鍛えられない", 17, UiKit.ColDim, TextAnchor.MiddleCenter);
                note.outlineWidth = 0.3f; note.outlineColor = new Color(0.05f, 0.03f, 0.06f, 0.95f);
                UiKit.Anchor(note.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(gapHalf, -30f), new Vector2(gapHalf + w, 30f));
            }
        }

        /// <summary>グリッドの各 cell に、触れる (PC はホバー・スマホはタップ) と ShowForgePair する挙動を付ける</summary>
        public static void AttachForgePreview(GameRoot g, RectTransform gridArea, IReadOnlyList<CardInstance> deck, RectTransform previewArea)
        {
            var cells = gridArea.GetComponentsInChildren<RectTransform>(true);
            int k = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].name != "cell") continue;
                if (k >= deck.Count) break;
                var c = deck[k++];
                var et = cells[i].gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                enter.callback.AddListener(delegate { ShowForgePair(g, previewArea, c); });
                et.triggers.Add(enter);
                var click = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick };
                click.callback.AddListener(delegate { if (!CardPopup.ClickSuppressed) ShowForgePair(g, previewArea, c); });
                et.triggers.Add(click);
            }
        }

        /// <summary>自動操縦のスクショ用: 最初に鍛えられる札の並びを出す</summary>
        public static void PreviewFirst(GameRoot g)
        {
            if (LastPreviewArea == null || g.Rs == null) return;
            for (int i = 0; i < g.Rs.Deck.Count; i++)
            {
                bool ok = false;
                try { ok = Upgrade.CanUpgradeCard(g.Rs.Deck[i]); } catch (Exception) { }
                if (ok) { ShowForgePair(g, LastPreviewArea, g.Rs.Deck[i]); return; }
            }
        }

        /// <summary>ツールチップ用の「鍛えると→」の1文 (コストが下がる札はコストだけ、他は鍛えた後の効果行)</summary>
        public static string DescribeUpgrade(CardInstance c)
        {
            if (!Upgrade.CanUpgradeCard(c)) return Upgrade.IsUpgraded(c) ? "鍛え済み" : "鍛えられない";
            try
            {
                var up = Upgrade.UpgradeCard(c);
                if (up.Def.Cost != c.Def.Cost) return "鍛えると→ コスト " + c.Def.Cost + "E → " + up.Def.Cost + "E（効果は据え置き）";
                return "鍛えると→ " + CardText.Body(up.Def).Replace("\n", " / ");
            }
            catch (Exception ex) { return "鍛えられない: " + ex.Message; }
        }

        /// <summary>グリッドの各カードに「鍛えると→」のツールチップ</summary>
        public static void AttachUpgradeTips(RectTransform area, IReadOnlyList<CardInstance> deck)
        {
            var cards = area.GetComponentsInChildren<RectTransform>(true);
            int k = 0;
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].name != "cell") continue;
                if (k >= deck.Count) break;
                var c = deck[k++];
                string tip;
                if (Upgrade.CanUpgradeCard(c))
                {
                    try
                    {
                        var up = Upgrade.UpgradeCard(c);
                        tip = "<b>鍛えると →</b> [" + CardText.CostLabel(up.Def) + "] " + up.Def.Name + "\n" + CardText.Body(up.Def);
                    }
                    catch (Exception ex) { tip = "鍛えられない: " + ex.Message; }
                }
                else tip = Upgrade.IsUpgraded(c) ? "鍛え済み" : "この札は鍛えられない";
                Tooltip.Attach(cards[i].gameObject, delegate { return tip; });
            }
        }
    }
}
