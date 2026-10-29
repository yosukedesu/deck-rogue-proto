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
            // 場面 (品質スプリント #3 2026-10-29 §10): 夜の森の箱庭の上に焚き火・丸太・座るこのは。暗幕は薄く (棚を開いている間は今までの濃さ)
            BattleScreen.BuildBackground(root, run.Act);
            var dim = UiKit.Pan(root, new Color(0f, 0f, 0f, g.SubMode != null ? 0.5f : 0.1f), "dim");
            dim.raycastTarget = false;
            UiKit.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
            if (g.SubMode == null) Scene(g, root, run);
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
            // 本家の形 (§10-3): 上に「どうする？」と絵札。絵札は場面の上 (焚き火は画面の下半分)
            RunUi.Heading(root, "焚き火", extra > 0 ? "一息つく ・ どれか 1 つ ・ 鍛えた後は回復なしで立ち去る" : "一息つく ・ どちらか 1 つ ・ 鍛えた後は回復なしで立ち去る");
            int count = 2 + extra;
            float w = UiKit.Phone && count > 3 ? 210f : 230f;   // 2 巡目: 絵札は小さめ (場面が主役)
            float gap = count <= 2 ? 60f : UiKit.Phone ? 16f : 24f;
            float optY = UiKit.Phone ? 40f : 200f;   // 絵札の中心 (PC: 上から 340・見出しのすぐ下。スマホ: 見出しの下)
            float x0 = -((count - 1) * (w + gap)) / 2f;
            int slot = 0;
            Vector2 Pos() { return new Vector2(x0 + (slot++) * (w + gap), optY); }

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

        /// <summary>絵札 (§10-3・本家の Rest／Smith の形): 紙の札 260×330。上に夜色の箱と挿絵 (PixelLab 48×48 を 100 に)、名前・一行・小さな行、押せるなら下に真鍮の小札「押して選ぶ」</summary>
        static RectTransform Option(RectTransform root, string icon, string title, string line1, string line2, Vector2 pos, Action onClick, bool enabled, float w = 230f)
        {
            float h = 316f;
            bool narrow = w < 220f;
            var cell = UiKit.NewRect("opt-" + title, root);
            cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 0.5f);
            cell.sizeDelta = new Vector2(w, h);
            cell.anchoredPosition = pos;
            var frame = UiKit.Frame(cell, Theme.Panel, enabled ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f), "frame", 3f);
            UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            float ib = narrow ? 96f : 108f;
            var box = PaperFx.Sheet(cell, PaperFx.Tag, "ib", enabled ? PaperFx.Night : new Color(PaperFx.Night.r, PaperFx.Night.g, PaperFx.Night.b, 0.7f));
            box.raycastTarget = false;
            UiKit.Anchor(box.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-ib / 2f, -18f - ib), new Vector2(ib / 2f, -18f));
            var ic = UiKit.Icon(box.transform, icon, narrow ? 76f : 84f);
            ic.raycastTarget = false;
            ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ic.rectTransform.anchoredPosition = Vector2.zero;
            float ty = -18f - ib - 6f;
            var t = UiKit.Deco(cell, title, narrow ? 22 : 24, UiKit.ColInk, TextAnchor.MiddleCenter);
            t.raycastTarget = false;
            UiKit.Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, ty - 40f), new Vector2(0f, ty));
            var l1 = UiKit.Txt(cell, line1, narrow ? 13 : 15, UiKit.ColInk, TextAnchor.MiddleCenter, true);
            l1.raycastTarget = false;
            l1.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Anchor(l1.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, ty - 100f), new Vector2(-12f, ty - 42f));
            var l2 = UiKit.Txt(cell, line2, 13, UiKit.ColInkSoft, TextAnchor.MiddleCenter);
            l2.raycastTarget = false;
            l2.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Anchor(l2.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, ty - 140f), new Vector2(-12f, ty - 102f));
            if (enabled)
            {
                var btn = frame.gameObject.AddComponent<Button>();
                btn.targetGraphic = frame;
                var cols = btn.colors; cols.highlightedColor = new Color(1.06f, 1.06f, 1.06f); cols.pressedColor = new Color(0.9f, 0.9f, 0.9f); btn.colors = cols;
                btn.onClick.AddListener(delegate { Audio.Ui("click"); onClick(); });
                frame.gameObject.AddComponent<ScreenFx.ButtonMotion>();   // ホバーで 1.03・押すと 0.96 (#2 条3)
                var tag = PaperFx.Sheet(cell, PaperFx.Tag, "hint", PaperFx.BrassLight);
                tag.raycastTarget = false;
                UiKit.Anchor(tag.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-50f, 10f), new Vector2(50f, 34f));
                var hintB = UiKit.Txt(tag.transform, "押して選ぶ", 12, PaperFx.BrassInk, TextAnchor.MiddleCenter, true);
                hintB.raycastTarget = false;
                UiKit.Stretch(hintB.rectTransform, 0f, 0f, 0f, 0f);
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
