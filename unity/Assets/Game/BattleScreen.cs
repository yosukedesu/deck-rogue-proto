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
        /// <summary>上部バーの高さ。スマホは 56 (2026-09-15: 頭上の吹き出しの高さを稼ぐ。札は 40〜44 なので収まる)</summary>
        public static float TopH { get { return UiKit.Phone ? 56f : 72f; } }
        /// <summary>手札の下端 (キャンバス下からの距離)。スマホは詰める</summary>
        public static float HandY { get { return UiKit.Phone ? 14f : 30f; } }
        /// <summary>手札の札の倍率。スマホは等倍 (2026-09-14「字が小さい」= 札の本文が最も読まれる文字)</summary>
        public static float CardScale { get { return UiKit.Phone ? 1.0f : 0.92f; } }
        /// <summary>戦闘の絵の目安の幅 (通常 256・エリート 320・ボス 384 = 1ドット4px)。スマホは半分 (1ドット2px) = 吹き出しが画面に収まる</summary>
        public static float ArtScale { get { return UiKit.Phone ? 0.6f : 1f; } }   // スマホは 0.5→0.6 (2026-09-15 吹き出しの小型化と対で「敵が小さすぎる」を戻す)
        /// <summary>キャンバスの実寸 (スマホ 1.6倍なら 1200〜1462×675)。組み立て中に画面の上端 (上部バーの下) を知るため</summary>
        public static Vector2 CanvasSize(RectTransform any)
        {
            // 最初のフレーム (Awake の組み立て) はキャンバスの矩形がまだ px のままなので、スケーラーの式で毎回求める (CanvasScaler.ScaleWithScreenSize と同じ計算)
            var canvas = any != null ? any.GetComponentInParent<Canvas>() : null;
            var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            if (scaler == null || Screen.width <= 0 || Screen.height <= 0) return new Vector2(1920f, 1080f);
            float logW = Mathf.Log(Screen.width / scaler.referenceResolution.x, 2f);
            float logH = Mathf.Log(Screen.height / scaler.referenceResolution.y, 2f);
            float scale = Mathf.Pow(2f, Mathf.Lerp(logW, logH, scaler.matchWidthOrHeight));
            return new Vector2(Screen.width / scale, Screen.height / scale);
        }

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
            // 背景は舞台 (別カメラ・ポスト処理と粒子つき) に描く。UI 側 (root) には何も置かない。
            // 舞台が組めなくても UI は組む (2026-09-14 実機: 舞台の例外で画面ごと消えていた疑い。原因は ErrorOverlay に出る)
            try { Stage.Paint(act); }
            catch (Exception e) { Debug.LogException(e); }
        }

        static void BuildTopBar(GameRoot g, RectTransform root, RunState run, GameState st)
        {
            var bar = UiKit.NewRect("topbar", root);
            UiKit.Anchor(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -TopH), new Vector2(0f, 0f));
            var hg = UiKit.Horz(bar, 12, 0);
            hg.padding = new RectOffset(28, 28, 0, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandHeight = false;
            hg.childForceExpandWidth = false;

            string enc = "";
            try
            {
                var node = DeckRogue.Engine.Run.CurrentNode(run);
                if (node != null && node.EncounterId != null) enc = Content.EncounterName(node.EncounterId);
            }
            catch (Exception) { }
            var title = Tag(bar, 40f, -0.6f);
            var tl = UiKit.Txt(title, "幕 " + run.Act + " · 行 " + (run.Row + 1), 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            tl.characterSpacing = 2f;
            UiKit.Le(tl, -1f, 30f, -1f, 30f);
            var te = UiKit.Deco(title, enc, 19, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(te, -1f, 30f, -1f, 30f);
            var turn = Tag(bar, 32f, 1f);
            UiKit.Icon(turn, "set", 16f, PaperFx.InkSoft);
            var tt = UiKit.Txt(turn, "ターン " + st.Turn, 13, PaperFx.Ink, TextAnchor.MiddleLeft, true);
            UiKit.Le(tt, -1f, 26f, -1f, 26f);

            var spacer = UiKit.NewRect("spacer", bar);
            UiKit.Le(spacer, 10f, 10f, -1f, -1f, 1f, -1f);
            // 手番の札 (2026-09-15 案C): 「あなたの番」「敵の番 ③ / 3」を上部バーの中央に (レイアウトの外・絶対配置)
            {
                string ph;
                if (st.Phase == CombatPhases.PlayerTurn) ph = "あなたの番";
                else if (st.Phase == CombatPhases.AwaitingReaction && st.PendingWindow != null) ph = "敵の番 " + (st.Enemies.Count > 1 && st.PendingWindow.EnemyIndex < Circled.Length ? Circled[st.PendingWindow.EnemyIndex] + " / " + st.Enemies.Count : "");
                else if (st.Phase == CombatPhases.Won || st.Phase == CombatPhases.Lost) ph = st.Phase == CombatPhases.Won ? "勝利" : "敗北";
                else ph = "敵の番";
                var pt = UiKit.NewRect("phase", root);
                var pImg = pt.gameObject.AddComponent<Image>();
                pImg.sprite = PaperFx.Tag; pImg.type = Image.Type.Sliced; pImg.pixelsPerUnitMultiplier = 1f; pImg.color = UiKit.Hex("#fbf6e8"); pImg.raycastTarget = false;
                float pw = 150f, phh = 32f;
                UiKit.Anchor(pt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-pw / 2f, -TopH / 2f - phh / 2f), new Vector2(pw / 2f, -TopH / 2f + phh / 2f));
                var ptx = UiKit.Deco(pt, ph.Trim(), 15, PaperFx.Ink, TextAnchor.MiddleCenter);
                ptx.characterSpacing = 3f;
                UiKit.Stretch(ptx.rectTransform, 4f, 4f, 0f, 0f);
                ptx.textWrappingMode = TextWrappingModes.NoWrap;
            }

            var gold = Tag(bar, 34f, 0f);
            UiKit.Icon(gold, "gold", 16f);
            var gt = UiKit.Deco(gold, run.Gold.ToString(), 18, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(gt, -1f, 28f, -1f, 28f);
            var gl = UiKit.Txt(gold, "G", 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(gl, -1f, 28f, -1f, 28f);

            for (int i = 0; i < run.Relics.Count && i < 8; i++)
            {
                RelicDef rd = null;
                try { rd = Content.GetRelicDef(run.Relics[i]); } catch (Exception) { }
                var cell = UiKit.NewRect("relic", bar);
                UiKit.Le(cell, 34f, 34f, 34f, 34f);
                var disc = cell.gameObject.AddComponent<Image>();
                disc.sprite = PaperFx.Disc(); disc.preserveAspect = true;
                RunUi.RelicArt(cell, run.Relics[i], 20f);
                var tip = rd != null ? "<b>" + rd.Name + "</b>\n" + rd.Description : run.Relics[i];
                Tooltip.Attach(cell.gameObject, delegate { return tip; });
            }
            if (UiKit.Phone) RunUi.MenuButton(g, bar);   // スマホは「≡」に畳む (マップ・ログ・メモ・レポート。2026-09-14)
            else
            {
                var mapBtn = UiKit.Btn(bar, "マップ", delegate { g.ViewMap = !g.ViewMap; g.Rebuild(); }, 13);   // 戦闘中も地図を確かめられる (2026-09-12)
                SetSize(mapBtn, 84f, 34f);
                var logBtn = UiKit.Btn(bar, g.ShowLog ? "ログを閉じる" : "ログ", delegate { g.ShowLog = !g.ShowLog; g.Rebuild(); }, 13);
                SetSize(logBtn, g.ShowLog ? 130f : 84f, 34f);
                FeedbackUi.TopBarButtons(g, bar);   // メモ・レポート (2026-09-14)
                RunUi.MenuButton(g, bar);   // PC も「≡」: セーブして終了・ランを放棄 (2026-09-15)
            }

            // エラー・通知は上部バーの下に (紙の札)
            string msg = g.Error != null ? "! " + g.Error : (g.Notice != null ? g.Notice : null);
            if (msg != null)
            {
                var note = UiKit.NewRect("message", root);
                UiKit.Anchor(note, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-460f, -TopH - 48f), new Vector2(460f, -TopH - 8f));
                var nImg = PaperFx.Sheet(note, PaperFx.Tag, "paper", g.Error != null ? new Color(1f, 0.85f, 0.8f, 1f) : Color.white);
                UiKit.Stretch(nImg.rectTransform, 0f, 0f, 0f, 0f);
                var m = UiKit.Txt(note, msg, 15, g.Error != null ? UiKit.ColBadInk : PaperFx.Ink, TextAnchor.MiddleCenter, true);
                UiKit.Stretch(m.rectTransform, 12f, 12f, 0f, 0f);
            }
        }

        /// <summary>紙の札 (横並びの入れ物)。少し傾けて手で置いた感じに</summary>
        public static RectTransform Tag(Transform parent, float height, float rot, Color? tint = null)
        {
            var rt = UiKit.NewRect("tag", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = PaperFx.Tag; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f;
            img.color = tint ?? Color.white;
            img.raycastTarget = false;
            var hg = UiKit.Horz(rt, 6, 0);
            hg.padding = new RectOffset(10, 10, 0, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandHeight = false;
            hg.childForceExpandWidth = false;
            var le = UiKit.Le(rt, -1f, height, -1f, height);
            le.flexibleWidth = 0f;
            var fit = rt.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (rot != 0f) rt.localRotation = Quaternion.Euler(0f, 0f, rot);
            return rt;
        }

        static void Chip(Transform parent, string icon, string text, Color color, int size)
        {
            var tag = Tag(parent, size + 12f, 0f);
            UiKit.Icon(tag, icon, 16f);
            var t = UiKit.Txt(tag, text, size, PaperFx.Ink, TextAnchor.MiddleLeft, true);
            UiKit.Le(t, 30f, size + 8f, -1f, size + 8f);
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

        // ---- 敵: 帳面の一行 (2026-09-15 ユーザー裁定「案C」。設計は docs/design/battle-v2) ----
        // 敵の名前・HP・意図・状態は頭上の吹き出しでなく、足元の線 (入れ物の下端 = StatusLineY・手札のすぐ上) の紙の札に。頭上には何も置かない。
        //   スマホ 176×76 = 番号＋名前＋ブロック・状態／HP／意図 の3段、PC 210×140 = さらに特性・分岐の一文。幅は隣との間隔で絞る (4体は 130)。
        //   狙っている敵は蜂蜜の縁＋頭上の▼ (PC は足元の輪も)、対象の候補は薄い蜂蜜の縁、確認の窓で行動中の敵は明るい縁が脈打つ。
        //   札もタップの的 (絵と同じ = 狙う)。分岐・特性の全文は札のツールチップ (スマホはタップの説明パネル)。
        // 旧: 頭上の吹き出し (奥の敵ほど高い) と名前札・HP バー・状態の札が縦に散っていた。

        public static float StripH { get { return UiKit.Phone ? 76f : 140f; } }
        /// <summary>帳面の一行の幅: 隣との間隔に収める (4体は 118 まで縮む)。1体だけ (ボス) は広く (特性の札も並ぶ)</summary>
        public static float StripW(float neighborGap, bool solo = false)
        {
            if (solo) return UiKit.Phone ? 300f : 320f;
            return UiKit.Phone ? Mathf.Clamp(neighborGap - 4f, 96f, 176f) : Mathf.Clamp(neighborGap - 12f, 150f, 210f);
        }
        static readonly string Circled = "①②③④⑤⑥⑦⑧";

        /// <summary>敵の入れ物の中身 (絵・狙いの印・帳面の一行)。入れ物 pan は BattleView が持ち越す</summary>
        public static void FillEnemyPanel(GameRoot g, RectTransform pan, GameState st, int index, int shownHp, float neighborGap = float.MaxValue)
        {
            var e = st.Enemies[index];
            bool alive = e.Hp > 0;
            bool aimed = g.PreferredTarget == index || (g.Pending != null && g.Pending.TargetIndex.HasValue && g.Pending.TargetIndex.Value == index);
            bool targeting = g.Pending != null && g.Pending.NextNeed() == "target";
            bool acting = st.Phase == CombatPhases.AwaitingReaction && st.PendingWindow != null && st.PendingWindow.EnemyIndex == index;
            g.RegisterAnchor("enemy" + index, pan);

            EnemyDef def = null;
            try { def = Content.GetEnemyDef(e.EnemyId); } catch (Exception) { }
            string nm = def != null ? def.Name : e.EnemyId;
            // ドット絵は整数倍 (通常・エリート 128→2倍=256px、ボスは3倍=384px)
            // 密度はオクトラ相当 (1ドット=画面4px): 通常 64→256・エリート 80→320・ボス 96→384 がどれも4倍になる目安
            string nodeType = null;
            try { var node = DeckRogue.Engine.Run.CurrentNode(g.Rs); nodeType = node != null ? node.Type : null; } catch (Exception) { }
            float artDots = nodeType == MapNodeTypes.Boss ? 384f : nodeType == MapNodeTypes.Elite ? 320f : 256f;
            var artSprite = Creature.Get("enemies", e.EnemyId, false, (int)(artDots / 4f));
            float artTarget = artDots * ArtScale;   // スマホは 0.6 (2026-09-15)
            float feetY = Stage.FeetOffset("enemy" + index, 130f);
            float spriteTop = feetY + artSprite.rect.height * PaperFx.PixelScaleF(artSprite, artTarget);
            float headTop = spriteTop - Creature.TopMargin(artSprite) * PaperFx.PixelScaleF(artSprite, artTarget);   // 絵の上端の透明な余白を除いた頭の上
            bool ph = UiKit.Phone;

            // 対象の候補: 足元に薄い輪 (PC。スマホは足元が札に近いので札の縁だけ)
            if (targeting && alive && !aimed && !ph)
            {
                var cand = UiKit.NewRect("cand", pan);
                UiKit.Anchor(cand, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-100f, feetY - 14f), new Vector2(100f, feetY + 14f));
                var cImg = cand.gameObject.AddComponent<Image>();
                cImg.sprite = PaperFx.Ring(5); cImg.color = new Color(PaperFx.Honey.r, PaperFx.Honey.g, PaperFx.Honey.b, 0.55f); cImg.raycastTarget = false;
                cImg.preserveAspect = false;
            }
            // 絵は舞台 (HD-2D) のビルボードが描く。UI 側の矩形は位置・大きさ・色 (生死/点滅) の基準として残す
            var spr = UiKit.NewRect("sprite", pan);
            PaperFx.FitPixel(spr, artSprite, 0f, feetY, artTarget);
            var img = spr.gameObject.AddComponent<Image>();
            img.sprite = artSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = alive ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            Stage.BindUnit("enemy" + index, spr, img, artSprite);
            if ((aimed || acting) && alive)
            {
                var honey = acting ? UiKit.Hex("#f0d58a") : PaperFx.Honey;
                if (!ph && feetY > StripH + 16f)
                {   // 足元の輪 (PC。スマホは札の上端が足元なので出さない)
                    var ring = UiKit.NewRect("ring", pan);
                    UiKit.Anchor(ring, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-116f, feetY - 16f), new Vector2(116f, feetY + 14f));
                    var rImg = ring.gameObject.AddComponent<Image>();
                    rImg.sprite = PaperFx.Ring(6); rImg.color = honey; rImg.raycastTarget = false;
                    rImg.preserveAspect = false;
                }
                // 頭上の▼ (狙っている敵は帳面の札の縁と二重で示す)
                var mark = UiKit.NewRect("marker", pan);
                float ms = ph ? 22f : 28f;
                UiKit.Anchor(mark, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-ms / 2f, headTop + 6f), new Vector2(ms / 2f, headTop + 6f + ms));
                var mImg = mark.gameObject.AddComponent<Image>();
                mImg.sprite = PaperFx.BubbleTail(); mImg.color = honey; mImg.raycastTarget = false; mImg.preserveAspect = true;
            }

            // 帳面の一行 (入れ物の下端に。幅は隣との間隔で絞る)
            float w = StripW(neighborGap, st.Enemies.Count == 1), h = StripH;
            var strip = UiKit.NewRect("strip", pan);
            UiKit.Anchor(strip, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-w / 2f, 0f), new Vector2(w / 2f, h));
            LedgerStrip(g, strip, st, index, def, nm, shownHp, w, h, aimed, targeting && alive && !aimed, acting);
            // 前の表示 (shownHp) から今の HP へ滑らせる (案C への書き換えで落ちていた＝バーが1手遅れて減っていた。2026-09-16 ユーザー報告)
            if (shownHp != e.Hp) TweenHpBar(pan, e.Hp);
        }

        /// <summary>帳面の一行の中身: 番号＋名前 (左) とブロック・状態の札 (右)／HP バー／意図 (絵・実値・ライダー)／(PC) 特性・分岐の一文</summary>
        static void LedgerStrip(GameRoot g, RectTransform strip, GameState st, int index, EnemyDef def, string nm, int shownHp, float w, float h, bool aimed, bool candidate, bool acting)
        {
            var e = st.Enemies[index];
            bool alive = e.Hp > 0;
            bool ph = UiKit.Phone;
            // 縁: 狙っている=蜂蜜／候補=薄い蜂蜜／行動中 (確認の窓) = 明るい蜂蜜が脈打つ
            if ((aimed || candidate || acting) && alive)
            {
                var edge = PaperFx.Sheet(strip, PaperFx.Tag, "edge", acting ? UiKit.Hex("#f0d58a") : candidate ? new Color(PaperFx.Honey.r, PaperFx.Honey.g, PaperFx.Honey.b, 0.55f) : PaperFx.Honey);
                float o = acting ? -5f : -3f;
                UiKit.Stretch(edge.rectTransform, o, o, o, o);
                edge.raycastTarget = false;
                if (acting)
                {
                    var gimg = edge; float t0 = UnityEngine.Random.value;
                    Tween.Run(1.2f, k => { if (gimg != null) { var c = gimg.color; c.a = 0.75f + 0.25f * Mathf.Sin((k + t0) * Mathf.PI * 2f); gimg.color = c; } }, Ease.Linear, null);
                }
            }
            var paper = PaperFx.Sheet(strip, PaperFx.Tag, "paper", alive ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f));
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = true;
            // 札もタップの的 (絵と同じ = 狙う)。説明は絵と同じツールチップ
            var btn = paper.gameObject.AddComponent<Button>();
            btn.targetGraphic = paper; btn.transition = Selectable.Transition.None; btn.interactable = alive;
            int captured = index;
            btn.onClick.AddListener(delegate { g.OnEnemyClicked(captured); });
            Tooltip.Attach(paper.gameObject, delegate { return EnemyTip(g, captured); });

            float pad = 8f;
            float top = ph ? 2f : 4f, nameH = ph ? 25f : 30f;   // Ellipsis は行の高さが文字の行 (15px≒23・19px≒29) より低いと行ごと落とす
            string num = st.Enemies.Count > 1 && index < Circled.Length ? Circled[index].ToString() : "";
            var nameT = UiKit.Deco(strip, num + nm + (alive ? "" : (e.Fled == true ? "（逃走）" : "（撃破）")), ph ? 15 : 19, alive ? PaperFx.Ink : PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Anchor(nameT.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(pad, -top - nameH), new Vector2(-pad, -top));
            nameT.textWrappingMode = TextWrappingModes.NoWrap; nameT.overflowMode = TextOverflowModes.Ellipsis;
            // 右詰めの札: ブロック (空色) と状態 (筋力・延焼・急所…)。名前と重なる分は名前を省略する
            bool narrow = w < 150f;   // 4体 (幅 118〜130): 名前の行に札を置く場所が無い → 状態の札は意図の行の後ろ、攻撃・防御以外の意図は絵だけ
            var rightMask = UiKit.NewRect("statusmask", strip);
            UiKit.Anchor(rightMask, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(w * 0.42f, -top - nameH), new Vector2(-pad + 2f, -top));
            rightMask.gameObject.AddComponent<RectMask2D>();
            var right = UiKit.NewRect("status", rightMask);
            UiKit.Anchor(right, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-600f, 0f), new Vector2(0f, 0f));   // 右詰め: 右端を合わせて左へ伸びる (溢れた分は左で切れる)
            var rg = UiKit.Horz(right, 4, 0);
            rg.childAlignment = TextAnchor.MiddleRight; rg.childForceExpandWidth = false; rg.childForceExpandHeight = false;
            var statusPills = new List<Action<Transform>>();   // 名前の行 (広い札) か意図の行 (狭い札) のどちらかへ
            float statusPillsW = 0f;   // 狭い札で意図の行に移る時の幅の見積り (割り込みの予告の札の空きを計算する。2026-09-16)
            if (alive && e.Block > 0) { int blk = e.Block; statusPills.Add(t => MiniPill(t, "shield", blk.ToString(), PaperFx.Ink, UiKit.Hex("#d6e6fa"), ChipTip("ブロック " + blk), 13, narrow)); statusPillsW += MiniPillW(blk.ToString()); }
            if (alive)
            {
                var chips = new List<KeyValuePair<string, string>>();
                if (e.Strength != 0) chips.Add(new KeyValuePair<string, string>("sword", "筋力" + (e.Strength > 0 ? "+" : "") + e.Strength));
                if (e.Burn > 0) chips.Add(new KeyValuePair<string, string>("burn", "延焼" + e.Burn));
                if (e.Exposed > 0) chips.Add(new KeyValuePair<string, string>("exposed", "急所" + e.Exposed));
                if (e.Confusion > 0) chips.Add(new KeyValuePair<string, string>("exposed", "混乱" + e.Confusion));
                if ((e.Weak ?? 0) > 0) chips.Add(new KeyValuePair<string, string>("shield", "威圧" + e.Weak.Value));
                if ((e.Artifact ?? 0) > 0) chips.Add(new KeyValuePair<string, string>("set", "AF" + e.Artifact.Value));
                if (e.BurrowActive == true) chips.Add(new KeyValuePair<string, string>("shield", "潜伏"));
                for (int i = 0; i < chips.Count; i++)
                {
                    var ch = chips[i];
                    bool debuff = ch.Value.StartsWith("急所") || ch.Value.StartsWith("混乱") || ch.Value.StartsWith("威圧");
                    statusPills.Add(t => MiniPill(t, ch.Key, ch.Value, debuff ? PaperFx.PlumInk : PaperFx.Ink, debuff ? new Color(0.93f, 0.86f, 0.97f, 1f) : PaperFx.Paper2, ChipTip(ch.Value), 13, narrow));
                    statusPillsW += MiniPillW(ch.Value);
                }
                // 1体だけ (ボス) のスマホ: 特性 (装甲・とげ・再生…) の短い札も名前の行に (PC は4段目の一文)
                if (ph && st.Enemies.Count == 1 && def != null)
                {
                    var tr = CardText.EnemyTraits(def).Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries);
                    int shown = 0;
                    for (int i = 0; i < tr.Length && shown < 3; i++)
                    {
                        string item = tr[i].Trim();
                        if (item.Length == 0 || item.Length > 7) continue;
                        string tipText = ChipTip(item);
                        statusPills.Add(t => MiniPill(t, "set", item, PaperFx.InkSoft, PaperFx.Paper2, tipText));
                        shown++;
                    }
                }
            }
            if (!narrow) for (int i = 0; i < statusPills.Count; i++) statusPills[i](right);
            // HP バー (2段目)
            float barTop = top + nameH + 2f, barH = ph ? 16f : 20f;
            HpBar(strip, new Vector2(0f, 0f), new Vector2(1f, 0f), h - barTop - barH, h - barTop, shownHp, e.MaxHp, 0, w / 2f - pad, ph ? 13 : 14, alive ? InterruptMarkRatio(def, e) : -1f);
            if (!alive) return;
            // 意図 (3段目): 絵・実値・ライダー (状態異常・筋力・盾・壊し)。ルーンの円蓋は「？」。分岐は今の盤面で有効な側 (窓が嘘をつかない)
            var it = e.Intent != null ? (Effects.EffectiveIntent(st, index) ?? e.Intent) : null;
            float rowTop = barTop + barH + (ph ? 2f : 4f), rowH = ph ? 28f : 40f;
            var rowMask = UiKit.NewRect("intentmask", strip);
            UiKit.Anchor(rowMask, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(pad, -rowTop - rowH), new Vector2(-pad, -rowTop));
            rowMask.gameObject.AddComponent<RectMask2D>();   // 狭い札 (4体) ではライダーが溢れる = 縮めずに切る (全文はツールチップ)
            var row = UiKit.NewRect("intent", rowMask);
            UiKit.Anchor(row, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(600f, 0f));
            float rowGap = ph ? 6f : 10f;
            var ig = UiKit.Horz(row, (int)rowGap, 0);
            ig.childAlignment = TextAnchor.MiddleLeft; ig.childForceExpandHeight = false; ig.childForceExpandWidth = false;
            float rowUsed = 0f;   // 並べた物の幅の見積り (割り込みの予告の札の空きを計算する。2026-09-16)
            if (it != null)
            {
                bool hidden = st.HideIntents == true;
                var intentArt = Theme.Art("icons", "intent_" + it.Kind);
                float isz = ph ? 32f : 40f;
                var ic = UiKit.Icon(row, IntentIcon(it.Kind), isz, intentArt != null ? Color.white : IntentColor(it.Kind));
                if (intentArt != null) ic.sprite = intentArt;
                ic.rectTransform.sizeDelta = new Vector2(isz, isz); UiKit.Le(ic, isz, isz, isz, isz);
                rowUsed += isz + rowGap;
                string shortText = hidden ? "？" : IntentShort(st, index, it);
                if (it.Kind == "defend" && !hidden) shortText = it.Actual.ToString();   // 盾の絵が「防御」を言うので数字だけ
                if (narrow && !hidden && it.Kind != "attack" && it.Kind != "defend") shortText = "";   // 狭い札: 絵だけ (言葉はツールチップ)
                if (shortText.Length > 0)
                {
                    var itT = UiKit.Deco(row, shortText, ph ? 22 : 30, PaperFx.Ink, TextAnchor.MiddleLeft);
                    UiKit.Le(itT, 14f, rowH, -1f, rowH);
                    itT.textWrappingMode = TextWrappingModes.NoWrap;
                    rowUsed += Mathf.Max(14f, EstTextW(shortText, ph ? 22f : 30f)) + rowGap;
                }
                if (!hidden)
                {
                    int riders = (it.Inflict != null ? 1 : 0) + (it.AlsoBuff.HasValue ? 1 : 0) + (it.AlsoDefend.HasValue ? 1 : 0) + (it.AlsoDestroySet == true ? 1 : 0);
                    bool terse = ph || riders >= 2;
                    int rsz = ph ? 13 : 15;
                    if (it.Inflict != null) { string tx = (terse ? "" : "あなたに") + CardText.StatusName(it.Inflict.Status) + it.Inflict.Amount; MiniPill(row, "exposed", tx, UiKit.Hex("#5a3d78"), UiKit.Hex("#eddbf7"), null, rsz, narrow); rowUsed += MiniPillW(tx, rsz) + rowGap; }
                    if (it.AlsoBuff.HasValue) { string tx = (terse ? "筋力+" : "同時に筋力+") + it.AlsoBuff.Value; MiniPill(row, "sword", tx, UiKit.Hex("#7a5a1a"), UiKit.Hex("#faebc7"), null, rsz, narrow); rowUsed += MiniPillW(tx, rsz) + rowGap; }
                    if (it.AlsoDefend.HasValue) { string tx = (terse ? "ブロック" : "同時にブロック") + it.AlsoDefend.Value; MiniPill(row, "shield", tx, UiKit.Hex("#2f5a7a"), UiKit.Hex("#d6e6fa"), null, rsz, narrow); rowUsed += MiniPillW(tx, rsz) + rowGap; }
                    if (it.AlsoDestroySet == true) { string tx = terse ? "先に壊す" : "先にからくりを壊す"; MiniPill(row, "exhaust", tx, UiKit.Hex("#7a2a2a"), UiKit.Hex("#fadbd6"), null, rsz, narrow); rowUsed += MiniPillW(tx, rsz) + rowGap; }
                }
            }
            if (narrow) { for (int i = 0; i < statusPills.Count; i++) statusPills[i](row); rowUsed += statusPillsW + statusPills.Count * rowGap; }
            // スマホ: 割り込みの予告 (「HP89以下で攻撃12〜14×2」「仲間が倒れると…」) を意図の行の余りに置く (PC は4段目の一文が担う)。
            // 2026-09-16 友人のラン: 自分の一撃でオーガが半分を割り、意図が 攻撃14→12×2 に差し替わって敗北。スマホでは予告がタップの説明パネルにしか無かった
            if (ph && alive && def != null && st.HideIntents != true) InterruptPills(row, st, index, def, e, w - 2f * pad - rowUsed);
            // PC の4段目: 分岐の注記・特性の一文 (2行まで・… で省略。全文はツールチップ)
            if (!ph)
            {
                string detail = it != null ? IntentDetail(st, index, it) : "";
                string traits = CardText.EnemyTraits(def, st, index);
                string line = detail.Length > 0 ? detail + (traits.Length > 0 ? "　" + traits : "") : traits;
                if (line.Length > 0)
                {
                    var lt = UiKit.Txt(strip, line, 13, PaperFx.InkSoft, TextAnchor.UpperLeft);
                    UiKit.Anchor(lt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(pad, -h + 6f), new Vector2(-pad, -rowTop - rowH - 2f));
                    lt.overflowMode = TextOverflowModes.Ellipsis;
                    lt.lineSpacing = -4f;
                }
            }
        }

        /// <summary>帳面の一行の小さな札 (ブロック・状態・ライダー)。高さ 20 (スマホ) / 24 (PC)。tip があればツールチップ</summary>
        static void MiniPill(Transform row, string icon, string text, Color ink, Color paper, string tip, int size = 13, bool tight = false)
        {
            bool ph = UiKit.Phone;
            var pill = UiKit.NewRect("pill", row);
            var bg = pill.gameObject.AddComponent<Image>();
            bg.sprite = PaperFx.Tag; bg.type = Image.Type.Sliced; bg.color = paper; bg.raycastTarget = tip != null;
            float h = ph ? 20f : 24f;
            UiKit.Le(pill, 24f, h, -1f, h);
            var hg = UiKit.Horz(pill, tight ? 1 : 2, tight ? 2 : 4);
            hg.childAlignment = TextAnchor.MiddleCenter; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            var ic = UiKit.Icon(pill, icon, 14f, ink);
            UiKit.Le(ic, 14f, 14f, 14f, 14f);
            var t = UiKit.Txt(pill, text, size, ink, TextAnchor.MiddleCenter, true);
            UiKit.Le(t, 10f, h - 2f, -1f, h - 2f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var fit = pill.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (tip != null) Tooltip.Attach(pill.gameObject, delegate { return tip; });
        }

        /// <summary>帳面の小さな札の幅の見積り (MiniPill: 余白 2+4 + 絵 14 + 間 2 + 文字 + 余白 4)</summary>
        static float MiniPillW(string text, int size = 13) { return 4f + 14f + 2f + EstTextW(text, size) + 6f; }

        /// <summary>
        /// スマホの意図の行に置く割り込みの予告の札 (2026-09-16)。CardText.EnemyTraits の割り込みの文と同じ材料で、
        /// 余り幅に収まる形を選ぶ: 「HP89以下で攻撃12〜14×2」→ 収まらなければ「HP半分で行動が変わる」→ それも無理なら置かない (タップの説明パネルに全文)
        /// </summary>
        static void InterruptPills(Transform row, GameState st, int index, EnemyDef def, EnemyState e, float avail)
        {
            if (def.Interrupts == null) return;
            int strength = e.Strength;
            for (int k = 0; k < def.Interrupts.Count; k++)
            {
                var it = def.Interrupts[k];
                if (IndexIn(e.FiredInterrupts, k)) continue;   // 発火済み
                var first = EnemyGraph.FirstMoveOf(def, it.Goto);
                string move = first != null ? CardText.MoveShort(first, strength) : "行動が変わる";
                string when, whenShort;
                if (it.On == EnemyInterruptTriggers.HpBelowHalf) { when = "HP" + (e.MaxHp / 2) + "以下で"; whenShort = "HP半分で"; }
                else if (it.On == EnemyInterruptTriggers.DamageTaken) { when = "あと" + Math.Max(0, (it.Amount ?? 0) - (e.DamageTakenTotal ?? 0)) + "ダメージで"; whenShort = when; }
                else if (it.On == EnemyInterruptTriggers.Alone) { if (!HasOtherAliveEnemy(st, index)) continue; when = "仲間が全滅すると"; whenShort = "仲間が全滅で"; }
                else if (it.On == EnemyInterruptTriggers.AllyDied) { if (!HasOtherAliveEnemy(st, index)) continue; when = "仲間が倒れると"; whenShort = "仲間が倒れると"; }
                else continue;
                string full = when + move;
                string tip = "<b>" + when + "行動が変わる</b>\n" + (first != null ? "最初の行動: " + move + "\n" : "") + "自分の番の途中で条件を満たすと、宣言していた意図がその場で差し替わる（意図の数字もその場で変わる）";
                string text = null;
                if (MiniPillW(full) <= avail) text = full;
                else if (MiniPillW(whenShort + "行動が変わる") <= avail) text = whenShort + "行動が変わる";
                if (text == null) continue;
                MiniPill(row, first != null ? IntentIcon(first.Kind) : "exposed", text, UiKit.Hex("#7a4e12"), UiKit.Hex("#faebc7"), tip, 13, true);
                avail -= MiniPillW(text) + 6f;
            }
        }

        /// <summary>HP バーに引く「行動が変わる線」の位置 (0〜1)。HP半分の割り込み＝0.5、被弾覚醒＝いまのHPから残りの累計を引いた所。無ければ -1</summary>
        static float InterruptMarkRatio(EnemyDef def, EnemyState e)
        {
            if (def == null || def.Interrupts == null || e.MaxHp <= 0) return -1f;
            for (int k = 0; k < def.Interrupts.Count; k++)
            {
                var it = def.Interrupts[k];
                if (IndexIn(e.FiredInterrupts, k)) continue;
                if (it.On == EnemyInterruptTriggers.HpBelowHalf) return 0.5f;
                if (it.On == EnemyInterruptTriggers.DamageTaken)
                {
                    int remain = Math.Max(0, (it.Amount ?? 0) - (e.DamageTakenTotal ?? 0));
                    float at = (float)(e.Hp - remain) / e.MaxHp;
                    if (at > 0f && at < 1f) return at;
                }
            }
            return -1f;
        }

        static bool IndexIn(IReadOnlyList<int> list, int k)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++) if (list[i] == k) return true;
            return false;
        }

        static bool HasOtherAliveEnemy(GameState st, int index)
        {
            for (int j = 0; j < st.Enemies.Count; j++) if (j != index && st.Enemies[j].Hp > 0) return true;
            return false;
        }

        /// <summary>文字幅の見積り (レイアウト前に吹き出しの幅を決めるため)。全角 1em・数字と記号 0.6em</summary>
        static float EstTextW(string s, float size)
        {
            if (string.IsNullOrEmpty(s)) return 0f;
            float w = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                w += (c < 0x80 || c == '×' || c == '＋') ? (char.IsDigit(c) ? 0.58f : 0.62f) : 1.0f;
            }
            return w * size;
        }

        /// <summary>小さなライダーの札の幅 (BubblePill compact: 余白 12 + 絵 16 + 3 + 文字 15px)</summary>
        static float CompactPillW(string text) { return 12f + 16f + 3f + EstTextW(text, 15f) + 6f; }

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
            string traits = CardText.EnemyTraits(def, cur, index);
            if (traits.Length > 0) sb.Append("\n特性: ").Append(traits);
            return sb.ToString();
        }

        /// <summary>吹き出しの中の小さな札 (デバフ・筋力・盾の予告)</summary>
        static void BubblePill(Transform row, string icon, string text, Color ink, Color paper, bool compact = false)
        {
            // compact = スマホの小型の吹き出し (2026-09-15): 高さ 28・文字 15
            var pill = UiKit.NewRect("pill", row);
            var bg = pill.gameObject.AddComponent<Image>();
            bg.sprite = PaperFx.Tag; bg.type = Image.Type.Sliced; bg.color = paper; bg.raycastTarget = false;
            float h = compact ? 28f : 34f;
            UiKit.Le(pill, compact ? 44f : 60f, h, -1f, h);
            var hg = UiKit.Horz(pill, 3, 6);
            hg.childAlignment = TextAnchor.MiddleCenter; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            float isz = compact ? 16f : 18f;
            var ic = UiKit.Icon(pill, icon, isz, ink);
            UiKit.Le(ic, isz, isz, isz, isz);
            var t = UiKit.Txt(pill, text, compact ? 15 : 17, ink, TextAnchor.MiddleCenter, true);
            UiKit.Le(t, 24f, compact ? 22f : 26f, -1f, compact ? 22f : 26f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var fit = pill.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static void SmallChip(Transform parent, string icon, string text, Color color)
        {
            var tag = Tag(parent, 28f, 0f);
            var img = tag.GetComponent<Image>();
            img.raycastTarget = true;
            bool debuff = text.StartsWith("弱") || text.StartsWith("脆") || text.StartsWith("虚") || text.StartsWith("重") || text.StartsWith("拘") || text.StartsWith("霞");
            if (debuff) img.color = new Color(0.93f, 0.86f, 0.97f, 1f);   // 状態異常の札は薄い紫の紙 = 資源の札と見分ける
            Tooltip.Attach(tag.gameObject, delegate { return ChipTip(text); });
            var dot = UiKit.Pan(tag, DotColor(icon, text), "dot");
            UiKit.Le(dot, 10f, 10f, 10f, 10f);
            UiKit.Icon(tag, icon, 16f, PaperFx.Ink);
            var t = UiKit.Txt(tag, text, 14, debuff ? PaperFx.PlumInk : PaperFx.Ink, TextAnchor.MiddleLeft, true);
            UiKit.Le(t, 20f, 26f, -1f, 26f);
        }

        /// <summary>札のツールチップ: 用語解説 (KEYWORD_HELP) と残りの数の意味</summary>
        static string ChipTip(string text)
        {
            int sp = text.IndexOf(' ');
            string key = sp > 0 ? text.Substring(0, sp) : text;
            for (int i = key.Length; i > 0; i--)
            {
                string help;
                if (KeywordHelp.Terms.TryGetValue(key.Substring(0, i), out help))
                {
                    string rest = sp > 0 ? text.Substring(sp + 1) : "";
                    return "<b>" + key.Substring(0, i) + "</b>" + (rest.Length > 0 ? " " + rest.Replace("T", "ターン") : "") + "\n" + help;
                }
            }
            return text;
        }

        static Color DotColor(string icon, string text)
        {
            if (text.StartsWith("成長")) return PaperFx.Moss;
            if (text.StartsWith("勢い")) return PaperFx.Honey;
            if (text.StartsWith("急所")) return UiKit.Hex("#e0a04a");
            if (text.StartsWith("延焼")) return UiKit.Hex("#e8742f");
            if (text.StartsWith("弱") || text.StartsWith("脆") || text.StartsWith("虚") || text.StartsWith("重") || text.StartsWith("拘") || text.StartsWith("霞")) return PaperFx.Plum;
            if (text.StartsWith("筋力")) return UiKit.Hex("#b7a89a");
            return PaperFx.Sky;
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

        static void HpBar(RectTransform parent, Vector2 aMin, Vector2 aMax, float yMin, float yMax, int hp, int max, int block, float xHalf = 0f, int textSize = 15, float markRatio = -1f)
        {
            var bar = UiKit.NewRect("hpbar", parent);
            // xHalf > 0 なら中央から ±xHalf の固定幅 (スマホの敵は隣との間隔に収める。2026-09-15)
            if (xHalf > 0f) UiKit.Anchor(bar, new Vector2(0.5f, aMin.y), new Vector2(0.5f, aMax.y), new Vector2(-xHalf, yMin), new Vector2(xHalf, yMax));
            else UiKit.Anchor(bar, aMin, aMax, new Vector2(0f, yMin), new Vector2(0f, yMax));
            // 紙の帯 (墨の縁) に薔薇色の水彩
            var edge = bar.gameObject.AddComponent<Image>();
            edge.color = PaperFx.Ink;
            edge.raycastTarget = false;
            var track = UiKit.Pan(bar, PaperFx.Paper, "track");
            UiKit.Stretch(track.rectTransform, 2f, 2f, 2f, 2f);
            track.raycastTarget = false;
            float r = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
            var fill = UiKit.NewRect("fill", bar);
            var fimg = fill.gameObject.AddComponent<Image>();
            fimg.sprite = ThemeFx.Gradient("hpfill", Color.Lerp(PaperFx.Rose, Color.white, 0.15f), Color.Lerp(PaperFx.Rose, Color.black, 0.08f));
            fimg.raycastTarget = false;
            UiKit.Anchor(fill, new Vector2(0f, 0f), new Vector2(r, 1f), new Vector2(3f, 3f), new Vector2(0f, -3f));
            var t = UiKit.Txt(bar, hp + " / " + max, textSize, PaperFx.Ink, TextAnchor.MiddleCenter, true);
            t.outlineWidth = 0.16f; t.outlineColor = new Color(PaperFx.Paper.r, PaperFx.Paper.g, PaperFx.Paper.b, 0.9f);   // 薔薇色の塗りの上でも墨が立つ
            UiKit.Stretch(t.rectTransform, 0f, 0f, 0f, 0f);
            var info = bar.gameObject.AddComponent<HpBarInfo>();
            info.Max = max; info.Value = hp; info.Fill = fill; info.Label = t;
            if (markRatio > 0f && markRatio < 1f)
            {   // 行動が変わる線 (HP半分・被弾覚醒): 蜂蜜の目盛りを帯の上下にはみ出させる (2026-09-16)。狭い札でも「次の一撃で割るか」が読める
                var mark = UiKit.NewRect("mark", bar);
                UiKit.Anchor(mark, new Vector2(markRatio, 0f), new Vector2(markRatio, 1f), new Vector2(-3f, -3f), new Vector2(3f, 3f));
                var mi = mark.gameObject.AddComponent<Image>(); mi.color = PaperFx.Ink; mi.raycastTarget = false;
                var core = UiKit.NewRect("core", mark);
                UiKit.Stretch(core, 1.5f, 1.5f, 1f, 1f);
                var ci = core.gameObject.AddComponent<Image>(); ci.color = PaperFx.Honey; ci.raycastTarget = false;
                mark.SetSiblingIndex(t.transform.GetSiblingIndex());   // 数字 (紙の縁取り) は目盛りの上に
            }
            if (block > 0)
            {
                var b = UiKit.NewRect("block", bar);
                UiKit.Anchor(b, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-54f, -18f), new Vector2(-8f, 18f));
                var blob = PaperFx.BlobImage(b, PaperFx.Sky);
                UiKit.Stretch(blob.rectTransform, 0f, 0f, 0f, 0f);
                var row = UiKit.NewRect("row", b);
                UiKit.Stretch(row, 0f, 0f, 0f, 0f);
                var hg = UiKit.Horz(row, 2, 0);
                hg.childAlignment = TextAnchor.MiddleCenter; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
                var ic = UiKit.Icon(row, "shield", 14f, PaperFx.Ink);
                UiKit.Le(ic, 14f, 14f, 14f, 14f);
                var bt = UiKit.Txt(row, block.ToString(), 15, PaperFx.Ink, TextAnchor.MiddleCenter, true);
                UiKit.Le(bt, 12f, 20f, -1f, 20f);
            }
        }

        /// <summary>頭上の短縮表示に無い情報 (分岐・付与・攻防一体・応援など) がある時だけ詳細行を出す</summary>
        static string IntentDetail(GameState st, int index, EnemyIntent it)
        {
            string full = CardText.IntentText(st, index);
            string[] marks = { "【", "※", "からくり", "従者", "→", "手数", "応援", "回復" };   // 付与・筋力・盾は吹き出しの中に出るので、ここは分岐と特殊行動だけ
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
                case "summon": return "growth";
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

        /// <summary>頭上の短い意図: 「12」「12 ×2」「防御 8」など (詳細は IntentText)。攻撃は補正込みのライブ値 (実値公開 2026-09-14)</summary>
        static string IntentShort(GameState st, int index, EnemyIntent it)
        {
            switch (it.Kind)
            {
                case "attack":
                {
                    string s = Effects.DisplayedIntentValue(st, index, it.Kind, it.Actual).ToString();
                    if ((it.Hits ?? 1) > 1) s += " ×" + it.Hits.Value;
                    if (it.MirrorHits == true) s += " ×手数";
                    return s;
                }
                case "defend": return "防御 " + it.Actual;   // 防御の量も頭上に (2026-09-14)
                case "buff": return "筋力+" + it.Actual;
                case "rally": return "応援";
                case "heal": return "回復";
                case "hex": return "呪い";
                case "destroy-set": return "からくり壊し";
                case "destroy-token": return "従者狩り";
                case "steal-gold": return "盗み";
                case "flee": return "逃走";
                case "mill": return "山札喰い";
                case "summon": return "召喚";
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

            var spr = UiKit.NewRect("sprite", area);
            var leaderArt = Creature.Get("leaders", leaderId, true);
            float pFeet = Stage.FeetOffset("player", 130f);
            float pArt = leaderArt.rect.width * 4f * ArtScale;   // 1ドット=4px を絵の幅に寄らず保つ (2026-09-16 このは v2 は 88×64=斧ぶん横に広い)。スマホは 0.6
            float pTop = pFeet + leaderArt.rect.height * PaperFx.PixelScaleF(leaderArt, pArt);
            PaperFx.FitPixel(spr, leaderArt, 0f, pFeet, pArt);
            spr.anchorMin = spr.anchorMax = new Vector2(0f, 0f);
            spr.offsetMin += new Vector2(130f, 0f); spr.offsetMax += new Vector2(130f, 0f);
            var img = spr.gameObject.AddComponent<Image>();
            img.sprite = leaderArt;
            img.preserveAspect = true;
            img.raycastTarget = false;
            Stage.BindUnit("player", spr, img, leaderArt);

            // 自キャラの名前札は出さない (2026-09-11 ユーザー「スマホ表示だとキャラと名前が被ってキャラがよく見えなくなる。
            // 自キャラ名表示は不要なのでは？」)。誰を操作しているかはセットアップとラン画面で分かるので、戦場では絵を優先する。
            // 敵の名前札は「どれを狙うか」の識別に要るので据え置き
            // 自分の札 (帳面の一行の左端。2026-09-15 案C): HP・被ダメ予測・資源を、敵の札と同じ足元の線に置く
            if (UiKit.Phone) { PhoneSelfColumn(g, area, st, shownHp); return; }
            PcSelfStrip(g, area, st, shownHp);
        }

        /// <summary>被ダメ予測の1行「被ダメ 17 − 盾 5 ＝ HP −12 → 59」(エンジンの IncomingTotal。致死級は骸骨の印)。攻撃が無ければ「被ダメ 0」</summary>
        static TMP_Text IncomingLine(Transform parent, GameState st, int size, bool twoLines = false)
        {
            var p = st.Player;
            int incoming = 0;
            try { incoming = Effects.IncomingTotal(st); } catch (Exception) { }
            int left = Math.Max(0, incoming - p.Block);
            bool lethal = incoming > 0 && p.Hp - left <= 0;
            string s;
            string br = twoLines ? "\n" : " ";
            if (st.HideIntents == true) s = "被ダメ ？（ルーンの円蓋）";
            else if (incoming <= 0) s = "被ダメ <b>0</b>" + br + "（この番は攻撃されない）";
            else s = "被ダメ <b><color=#9c3a2a>" + incoming + "</color></b> − 盾 " + p.Block + " ＝" + br + "<b>HP −" + left + " → " + Math.Max(0, p.Hp - left) + "</b>" + (lethal ? " <color=#9c3a2a><b>致死</b></color>" : "");
            var t = UiKit.Txt(parent, s, size, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.lineSpacing = -6f;
            Tooltip.Attach(t.gameObject, delegate { return "<b>被ダメ予測</b>\n全ての敵の攻撃 (実値×ヒット数) の合計から今のブロックを引いた、この敵の番で失う HP の見込み。威圧・脆弱・重りは込み"; });
            t.raycastTarget = true;
            return t;
        }

        /// <summary>PC の自分の札 (帳面の左端・幅は仕込み枠の数で伸びる): HP＋ブロック／被ダメ／資源｜からくり (トークン 68×74)｜置物 (付箋 200×40 を2行・超えたら +N)</summary>
        static void PcSelfStrip(GameRoot g, RectTransform area, GameState st, int shownHp)
        {
            var p = st.Player;
            float ax = area.offsetMin.x;   // area の左端 (キャンバス x)。札はキャンバス x=40 から
            float secA = 300f, secB = p.SetSlots * (PhoneTokenW + 10f) + 24f, secC = 236f;
            float w = secA + secB + secC, h = StripH;
            var strip = UiKit.NewRect("hpwrap", area);
            UiKit.Anchor(strip, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f - ax, 0f), new Vector2(40f - ax + w, h));
            var paper = PaperFx.Sheet(strip, PaperFx.Tag, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = false;
            // A: HP・被ダメ・資源
            var hpRt = UiKit.NewRect("hp", strip);
            UiKit.Anchor(hpRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(66f, -36f), new Vector2(secA - 12f, -14f));
            HpBar(hpRt, Vector2.zero, Vector2.one, 0f, 0f, shownHp, p.MaxHp, p.Block);
            if (shownHp != p.Hp) TweenHpBar(area, p.Hp);
            if (p.IceBlock > 0)
            {
                var ice = UiKit.Txt(strip, "氷壁 " + p.IceBlock, 14, UiKit.Hex("#2f5a7a"), TextAnchor.MiddleLeft, true);
                UiKit.Anchor(ice.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(secA - 12f - 70f, -36f), new Vector2(secA - 12f, -14f));
                ice.alignment = TextAlignmentOptions.MidlineRight;
            }
            var inc = IncomingLine(strip, st, 14);
            UiKit.Anchor(inc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -62f), new Vector2(secA - 8f, -40f));
            var res = ResourceChips(p);
            if (res.Count > 0)
            {   // 資源・状態の札 (1行に3つ。4つ目からは2行目)
                for (int r = 0; r < 2; r++)
                {
                    int from = r * 3; if (from >= res.Count) break;
                    var col = UiKit.NewRect(r == 0 ? "chips" : "chips2", strip);
                    UiKit.Anchor(col, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -68f - r * 32f - 28f), new Vector2(secA - 8f, -68f - r * 32f));
                    var vg = UiKit.Horz(col, 6, 0);
                    vg.childAlignment = TextAnchor.MiddleLeft; vg.childForceExpandWidth = false; vg.childForceExpandHeight = false;
                    for (int i = from; i < res.Count && i < from + 3; i++) SmallChip(col, res[i].Key, res[i].Value, PaperFx.Ink);
                }
            }
            // B: からくり = 仕込み札のトークン
            var divA = UiKit.Pan(strip, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.35f), "div");
            UiKit.Anchor(divA.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(secA, 12f), new Vector2(secA + 1f, -12f));
            divA.raycastTarget = false;
            var setArea = UiKit.NewRect("setzone", strip);
            UiKit.Anchor(setArea, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(secA + 14f, 0f), new Vector2(secA + secB, 0f));
            var setLabel = UiKit.Txt(setArea, "からくり " + p.SetCards.Count + " / " + p.SetSlots, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Anchor(setLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -30f), new Vector2(0f, -10f));
            for (int i = 0; i < p.SetSlots; i++)
            {
                var slot = UiKit.NewRect("slot" + i, setArea);
                UiKit.Anchor(slot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(i * (PhoneTokenW + 10f), -34f - PhoneTokenH), new Vector2(i * (PhoneTokenW + 10f) + PhoneTokenW, -34f));
                g.RegisterAnchor("setslot" + i, slot);
                if (i < p.SetCards.Count) PhoneSetToken(g, slot, st, p.SetCards[i]);
                else
                {   // 空きの枠: 点線のポケット
                    var pocket = PaperFx.Sheet(slot, PaperFx.Tag, "pocket", new Color(0.55f, 0.5f, 0.45f, 0.35f));
                    UiKit.Stretch(pocket.rectTransform, 0f, 0f, 0f, 0f);
                    pocket.raycastTarget = false;
                }
            }
            // C: 置物 = 付箋 (挿絵 + 名前) を2行。3つ目からは「+N …」
            float cx0 = secA + secB;
            var divB = UiKit.Pan(strip, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.35f), "div");
            UiKit.Anchor(divB.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(cx0, 12f), new Vector2(cx0 + 1f, -12f));
            divB.raycastTarget = false;
            var perms = new List<CardInstance>();
            for (int i = 0; i < p.Permanents.Count; i++) if (p.Permanents[i].Innate != true) perms.Add(p.Permanents[i]);
            var permRow = UiKit.NewRect("perms", strip);
            UiKit.Anchor(permRow, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(cx0 + 14f, 0f), new Vector2(cx0 + secC - 8f, 0f));
            var permLabel = UiKit.Txt(permRow, "置物 " + perms.Count, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Anchor(permLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -30f), new Vector2(0f, -10f));
            PermChips(permRow, perms, 1, 200f, 34f);
        }

        /// <summary>置物の付箋を並べる (cols 列・2行)。行に収まらない分は「+N …」(タップで名前の一覧)。上端 top から下へ</summary>
        static void PermChips(RectTransform permRow, List<CardInstance> perms, int cols, float chipW, float top)
        {
            int cells = cols * 2;
            int show = perms.Count <= cells ? perms.Count : cells - 1;
            for (int i = 0; i < show; i++)
            {
                var chip = UiKit.NewRect("perm", permRow);
                int cx = i % cols, cy = i / cols;
                UiKit.Anchor(chip, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(cx * (chipW + 8f), -top - cy * (PhoneChipH + 6f) - PhoneChipH), new Vector2(cx * (chipW + 8f) + chipW, -top - cy * (PhoneChipH + 6f)));
                PhonePermChip(chip, perms[i]);
            }
            if (show < perms.Count)
            {   // 残りは「+N …」(タップで名前の一覧)
                var more = UiKit.NewRect("more", permRow);
                int cx = show % cols, cy = show / cols;
                UiKit.Anchor(more, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(cx * (chipW + 8f), -top - cy * (PhoneChipH + 6f) - PhoneChipH), new Vector2(cx * (chipW + 8f) + 72f, -top - cy * (PhoneChipH + 6f)));
                var mImg = PaperFx.Sheet(more, PaperFx.Tag, "paper");
                UiKit.Stretch(mImg.rectTransform, 0f, 0f, 0f, 0f);
                mImg.raycastTarget = true;
                var mt = UiKit.Txt(more, "+" + (perms.Count - show) + " …", 15, PaperFx.Ink, TextAnchor.MiddleCenter, true);
                UiKit.Stretch(mt.rectTransform, 4f, 4f, 0f, 0f);
                var sb = new System.Text.StringBuilder();
                for (int i = show; i < perms.Count; i++) { if (sb.Length > 0) sb.Append("\n"); sb.Append("<b>" + perms[i].Def.Name + "</b> " + CardText.Body(perms[i].Def)); }
                string mtip = sb.ToString();
                Tooltip.Attach(more.gameObject, delegate { return mtip; });
            }
        }

        /// <summary>資源・状態の札の一覧 (アイコン名, 文言)</summary>
        static List<KeyValuePair<string, string>> ResourceChips(PlayerState p)
        {
            var res = new List<KeyValuePair<string, string>>();
            if (p.Growth > 0) res.Add(new KeyValuePair<string, string>("growth", "成長 " + p.Growth));
            if (p.Momentum > 0) res.Add(new KeyValuePair<string, string>("momentum", "勢い " + p.Momentum));
            if (p.Aether > 0) res.Add(new KeyValuePair<string, string>("energy", "霊気 " + p.Aether));
            if (p.NextCardDiscount > 0) res.Add(new KeyValuePair<string, string>("energy", "次のカード -" + p.NextCardDiscount));
            if (p.SpellEchoes > 0) res.Add(new KeyValuePair<string, string>("draw", "反復 " + p.SpellEchoes));
            // 状態異常は「名前 残りNT」で、数字がターンだと一目で読めるように (2026-09-09「デバフ表示が分かりにくすぎる」)
            if (p.Weak > 0) res.Add(new KeyValuePair<string, string>("exposed", "弱体 " + p.Weak + "T"));
            if (p.Vulnerable > 0) res.Add(new KeyValuePair<string, string>("exposed", "脆弱 " + p.Vulnerable + "T"));
            if (p.Frail > 0) res.Add(new KeyValuePair<string, string>("exposed", "虚弱 " + p.Frail + "T"));
            if (p.Restrain > 0) res.Add(new KeyValuePair<string, string>("set", "拘束 " + p.Restrain + "T"));
            if ((p.Mist ?? 0) > 0) res.Add(new KeyValuePair<string, string>("draw", "霞み " + p.Mist.Value + "T"));
            if ((p.Slow ?? 0) > 0) res.Add(new KeyValuePair<string, string>("exposed", "重り " + p.Slow.Value + "T"));
            return res;
        }

        // ---- スマホの「自分の欄」(2026-09-14 ユーザー裁定「案B 左に3段」→ 2026-09-15 案C: HP・被ダメ・資源を足元の線の札に) ----
        // 画面の左の一角に上から からくり (仕込み札のトークン 68×74)／置物 (付箋 168×40)／自分の札 (HP＋ブロック・被ダメ予測・資源) を積む。
        // 自分の札の下端は敵の札と同じ線 (StatusLineY = 手札のすぐ上)。資源の札が3つ以上なら札が2行ぶん高くなり、置物は1行に詰める。
        // 座標はキャンバスの左上から測った値 (S25 相当 1462×675) を area (左下が feet.x-130, StatusLineY) の座標へ写す。
        const float PhoneTokenW = 68f, PhoneTokenH = 74f, PhoneChipW = 168f, PhoneChipH = 40f;

        static void PhoneSelfColumn(GameRoot g, RectTransform area, GameState st, int shownHp)
        {
            var p = st.Player;
            float ax = area.offsetMin.x;                  // area の左端 (キャンバス x)
            float ay = BattleView.StatusLineY;            // area の下端 (キャンバス y・下から)
            var cs = CanvasSize(area);
            // キャンバス左上基準の (x, top, w, h) を area の Anchor (左下基準) に置く
            Action<RectTransform, float, float, float, float> place = (rt, x, top, w, h) =>
                UiKit.Anchor(rt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x - ax, cs.y - top - h - ay), new Vector2(x - ax + w, cs.y - top - ay));
            const float left = 24f, bandTop = 62f;

            // 上の帯 (リーダーの頭より上): からくり = 仕込み札のトークン (挿絵・状態の一言・角に残り回数)
            var setArea = UiKit.NewRect("setzone", area);
            float setW = p.SetSlots * (PhoneTokenW + 10f);
            place(setArea, left, bandTop, setW, 22f + PhoneTokenH);   // 見出しの行 22 + トークン (角の数字が上に 9 はみ出す)
            var setLabel = PaperFx.NightNote(setArea, "からくり " + p.SetCards.Count + " / " + p.SetSlots, 14, 200f);
            setLabel.anchorMin = setLabel.anchorMax = new Vector2(0f, 1f); setLabel.pivot = new Vector2(0f, 1f);
            setLabel.anchoredPosition = new Vector2(-4f, 2f);
            for (int i = 0; i < p.SetSlots; i++)
            {
                var slot = UiKit.NewRect("slot" + i, setArea);
                UiKit.Anchor(slot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(i * (PhoneTokenW + 10f), 0f), new Vector2(i * (PhoneTokenW + 10f) + PhoneTokenW, PhoneTokenH));
                g.RegisterAnchor("setslot" + i, slot);
                if (i < p.SetCards.Count) PhoneSetToken(g, slot, st, p.SetCards[i]);
                else
                {   // 空きの枠: 点線のポケット (文字は置かない)
                    var pocket = PaperFx.Sheet(slot, PaperFx.Tag, "pocket", new Color(1f, 1f, 1f, 0.35f));
                    UiKit.Stretch(pocket.rectTransform, 0f, 0f, 0f, 0f);
                    pocket.raycastTarget = false;
                }
            }

            // 同じ帯の右: 置物 = 付箋 (挿絵 + 名前) を2列×2行 (狭いキャンバスは1列)。超えたら「+N …」。リーダーの頭 (y≈200) より上なので絵と重ならない
            var perms = new List<CardInstance>();
            for (int i = 0; i < p.Permanents.Count; i++) if (p.Permanents[i].Innate != true) perms.Add(p.Permanents[i]);
            if (perms.Count > 0)
            {
                int cols = cs.x >= 1400f ? 2 : 1;
                var permRow = UiKit.NewRect("perms", area);
                place(permRow, left + setW + 14f, bandTop, cols * (PhoneChipW + 8f), 22f + 2f * (PhoneChipH + 6f));
                var permLabel = PaperFx.NightNote(permRow, "置物 " + perms.Count, 14, 120f);
                permLabel.anchorMin = permLabel.anchorMax = new Vector2(0f, 1f); permLabel.pivot = new Vector2(0f, 1f);
                permLabel.anchoredPosition = new Vector2(-4f, 2f);
                PermChips(permRow, perms, cols, PhoneChipW, 22f);
            }

            // 自分の札 (下端は帳面の線): HP＋ブロック／被ダメ予測 (2行)／資源 (1行に2つ・3つ目からは2行目)
            var res = ResourceChips(p);
            int resRows = res.Count > 2 ? 2 : (res.Count > 0 ? 1 : 0);
            float stripW = 224f, stripH = 70f + resRows * 30f;
            float stripTop = cs.y - ay - stripH;
            var strip = UiKit.NewRect("hpwrap", area);
            place(strip, left, stripTop, stripW, stripH);
            var paper = PaperFx.Sheet(strip, PaperFx.Tag, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = false;
            var hpRt = UiKit.NewRect("hp", strip);
            UiKit.Anchor(hpRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(54f, -26f), new Vector2(-8f, -8f));
            HpBar(hpRt, Vector2.zero, Vector2.one, 0f, 0f, shownHp, p.MaxHp, p.Block, 0f, 13);
            if (shownHp != p.Hp) TweenHpBar(area, p.Hp);
            if (p.IceBlock > 0)
            {
                var ice = UiKit.Txt(strip, "氷壁 " + p.IceBlock, 13, UiKit.Hex("#2f5a7a"), TextAnchor.MiddleRight, true);
                UiKit.Anchor(ice.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-80f, -66f), new Vector2(-8f, -30f));
            }
            var inc = IncomingLine(strip, st, 13, true);
            UiKit.Anchor(inc.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -68f), new Vector2(-8f, -30f));
            for (int r = 0; r < resRows; r++)
            {
                int from = r * 2;
                var col = UiKit.NewRect(r == 0 ? "chips" : "chips2", strip);
                UiKit.Anchor(col, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -70f - r * 30f - 28f), new Vector2(-4f, -70f - r * 30f));
                var vg = UiKit.Horz(col, 6, 0);
                vg.childAlignment = TextAnchor.MiddleLeft; vg.childForceExpandWidth = false; vg.childForceExpandHeight = false;
                col.gameObject.AddComponent<RectMask2D>();
                for (int i = from; i < res.Count && i < from + 2; i++) SmallChip(col, res[i].Key, res[i].Value, PaperFx.Ink);
            }
        }

        /// <summary>仕込み札のトークン (68×74): 上に挿絵 64×38、下に状態の帯 (準備中／あとN回／今ターン／期限なし)。生きている札は蜂蜜の縁、今ターン鳴る札は縁が脈打ち角に残り回数</summary>
        static void PhoneSetToken(GameRoot g, RectTransform slot, GameState st, CardInstance sc)
        {
            bool live = Effects.IsTrapLive(st, sc);
            bool canFireNow = live && Effects.TrapCanFireThisPhase(st, sc);
            int? left = Effects.TrapWindowsLeft(st, sc);
            // 縁 (状態の色) → 紙 → 挿絵 → 帯 → 角の数字
            var edge = PaperFx.Sheet(slot, PaperFx.Tag, "edge", !live ? UiKit.Hex("#6a6a74") : canFireNow ? UiKit.Hex("#f0d58a") : UiKit.Hex("#c9b26a"));
            UiKit.Stretch(edge.rectTransform, -3f, -3f, -3f, -3f);
            edge.raycastTarget = false;
            if (canFireNow)
            {
                var gimg = edge; float t0 = UnityEngine.Random.value;
                Tween.Run(1.2f, k => { if (gimg != null) { var c = gimg.color; c.a = 0.75f + 0.25f * Mathf.Sin((k + t0) * Mathf.PI * 2f); gimg.color = c; } }, Ease.Linear, null);
            }
            var paper = PaperFx.Sheet(slot, PaperFx.Tag, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = true;
            var frame = UiKit.Pan(slot, PaperFx.Ink, "frame");
            UiKit.Anchor(frame.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, -41f), new Vector2(67f, -1f));
            frame.raycastTarget = false;
            var pic = UiKit.NewRect("pic", slot);
            UiKit.Anchor(pic, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(2f, -40f), new Vector2(66f, -2f));
            var pimg = pic.gameObject.AddComponent<Image>();
            pimg.sprite = ThemeFx.CardArt(sc.Def.Id, Theme.CardTypeColor(sc.Def.Type)); pimg.preserveAspect = true; pimg.raycastTarget = false;
            if (!live) pimg.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            string band = !live ? "準備中" : canFireNow ? "今ターン" : left.HasValue ? "あと" + left.Value + "回" : "期限なし";
            var bandImg = UiKit.Pan(slot, !live ? UiKit.Hex("#8a8a94") : canFireNow ? UiKit.Hex("#3f8a4a") : UiKit.Hex("#7a6a3a"), "band");
            UiKit.Anchor(bandImg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1f, 29f));
            bandImg.raycastTarget = false;
            var bt = UiKit.Deco(slot, band, 15, PaperFx.Paper, TextAnchor.MiddleCenter);
            UiKit.Anchor(bt.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(0f, 29f));
            bt.textWrappingMode = TextWrappingModes.NoWrap;
            if (canFireNow)
            {   // 角の数字 = 残りの窓 (期限なしは ∞)
                var badge = UiKit.NewRect("badge", slot);
                UiKit.Anchor(badge, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-13f, -13f), new Vector2(9f, 9f));
                var bImg = badge.gameObject.AddComponent<Image>();
                bImg.sprite = PaperFx.Disc(); bImg.preserveAspect = true; bImg.raycastTarget = false;
                var bRing = UiKit.NewRect("ring", badge);
                UiKit.Stretch(bRing, -1.5f, -1.5f, -1.5f, -1.5f);
                var rImg = bRing.gameObject.AddComponent<Image>();
                rImg.sprite = PaperFx.Ring(4); rImg.color = PaperFx.Ink; rImg.raycastTarget = false; rImg.preserveAspect = true;
                var btx = UiKit.Deco(badge, left.HasValue ? left.Value.ToString() : "∞", 13, PaperFx.Ink, TextAnchor.MiddleCenter);
                UiKit.Stretch(btx.rectTransform, 0f, 0f, 0f, 0f);
            }
            string trapLife = Effects.TrapStatusTextKarakuri(st, sc);
            string tip = "<b>" + sc.Def.Name + "</b>\n" + CardText.Body(sc.Def) + "\n<color=#7a4e12>" + trapLife + "</color>";
            string liveTip = null;
            try { liveTip = Effects.SetCardLiveDamage(st, sc.Def); } catch (Exception) { }
            if (liveTip != null) tip += "\n<color=#7a4e12>" + liveTip + "</color>";
            Tooltip.Attach(paper.gameObject, delegate { return tip; });
        }

        /// <summary>置物の付箋 (168×40): 挿絵 48×29 + 名前 (長い名前は…)。タップで本文</summary>
        static void PhonePermChip(RectTransform chip, CardInstance q)
        {
            var img = PaperFx.Sheet(chip, PaperFx.Tag, "paper");
            UiKit.Stretch(img.rectTransform, 0f, 0f, 0f, 0f);
            img.raycastTarget = true;
            var frame = UiKit.Pan(chip, PaperFx.Ink, "frame");
            UiKit.Anchor(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, -15.5f), new Vector2(54f, 15.5f));
            frame.raycastTarget = false;
            var pic = UiKit.NewRect("pic", chip);
            UiKit.Anchor(pic, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(5f, -14.5f), new Vector2(53f, 14.5f));
            var pimg = pic.gameObject.AddComponent<Image>();
            pimg.sprite = ThemeFx.CardArt(q.Def.Id, Theme.CardTypeColor(q.Def.Type)); pimg.preserveAspect = true; pimg.raycastTarget = false;
            var nt = UiKit.Deco(chip, q.Def.Name, 15, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Anchor(nt.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(60f, 0f), new Vector2(-8f, 0f));
            nt.textWrappingMode = TextWrappingModes.NoWrap;
            nt.overflowMode = TextOverflowModes.Ellipsis;
            string ptip = "<b>" + q.Def.Name + "</b>\n" + CardText.Body(q.Def);
            Tooltip.Attach(chip.gameObject, delegate { return ptip; });
        }

        // ---- 手札 (扇) ----

        /// <summary>カードの吹き出し: 本文は見えているので用語解説だけ (無ければ出さない)</summary>
        public static string KeywordsOnly(string text)
        {
            var terms = KeywordHelp.FindIn(text);
            if (terms.Count == 0) return null;
            var lines = new List<string>();
            for (int i = 0; i < terms.Count && i < 4; i++) lines.Add("<color=#276a34><b>" + terms[i] + "</b></color> " + KeywordHelp.Terms[terms[i]]);
            return string.Join("\n", lines.ToArray());
        }

        static bool _dragging;

        public static void HookHandCard(GameRoot g, BattleView.HandCard hc, CardInstance c)
        {
            var rt = hc.Rt;
            var et = rt.gameObject.AddComponent<EventTrigger>();
            // 長押し 0.5 秒で拡大表示 (本家の SingleCardViewPopup。右クリックは伏せるに使っているので手札は長押しだけ)
            CardPopup.Attach(g, rt, c, delegate { return g.Rs != null ? g.Rs.Combat : null; }, false);
            // ドラッグ: カードを持ち上げて敵に落とすと対象指定して即プレイ、戦場に落とすとプレイ、手札に戻すと取り消し
            var beginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDrag.callback.AddListener(delegate
            {
                if (!hc.Playable || CardPopup.IsOpen) return;
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
                // 敵の上に来たら、その敵に対する実値で札を描き直す (急所・装甲が数字に乗る)
                if (pd != null && g.Battle != null && g.Rs != null && g.Rs.Combat != null)
                {
                    int over = EnemyUnderPointer(pd);
                    try { g.Battle.RefreshHandCard(g, g.Rs.Combat, hc, over); } catch (Exception) { }
                }
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
                if (CardPopup.ClickSuppressed || CardPopup.IsOpen) return;   // 長押しで拡大表示を開いた直後の離しはプレイしない
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
                var sb = UiKit.Btn(rt, "仕込む", delegate { g.DoCombat(new Command_SetCard { CardUid = c.Uid }); }, 15, true, UiKit.Hex("#bfe3dc"));
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
            Pile(g, root, new Vector2(1f, 0f), new Vector2(-214f, 24f), "exhaust", "捨て札 / 消滅", p.DiscardPile.Count, p.ExhaustPile.Count, delegate { g.ViewPile = "discard"; g.Rebuild(); }, "pile-discard");
            if (g.ViewPile != null) BuildPileViewer(g, root, st);
        }

        static void Pile(GameRoot g, RectTransform root, Vector2 anchor, Vector2 offset, string icon, string label, int count, int count2 = -1, Action onClick = null, string anchorName = null)
        {
            var rt = UiKit.NewRect("pile-" + icon, root);
            if (anchorName != null) g.RegisterAnchor(anchorName, rt);
            UiKit.Anchor(rt, anchor, anchor, offset, offset + new Vector2(count2 >= 0 ? 190f : 130f, 40f));
            rt.localRotation = Quaternion.Euler(0f, 0f, anchor.x > 0.5f ? 1f : -1f);
            var frame = PaperFx.Sheet(rt, PaperFx.Tag, "paper");
            UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            frame.raycastTarget = onClick != null;
            if (onClick != null)
            {
                var pb = rt.gameObject.AddComponent<Button>();
                pb.targetGraphic = frame;
                pb.transition = Selectable.Transition.None;
                pb.onClick.AddListener(delegate { onClick(); });
            }
            var row = UiKit.NewRect("row", rt);
            UiKit.Stretch(row, 0f, 0f, 0f, 0f);
            var hg = UiKit.Horz(row, 6, 0);
            hg.padding = new RectOffset(12, 12, 0, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            var ic = UiKit.Icon(row, icon, 16f, PaperFx.Ink);
            UiKit.Le(ic, 16f, 16f, 16f, 16f);
            var cnt = UiKit.Deco(row, count.ToString(), 17, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(cnt, -1f, 30f, -1f, 30f);
            var lb = UiKit.Txt(row, count2 >= 0 ? "捨て札" : label, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(lb, -1f, 30f, -1f, 30f);
            if (count2 >= 0)
            {
                var sep = UiKit.Pan(row, PaperFx.InkSoft, "sep");
                UiKit.Le(sep, 1.5f, 14f, 1.5f, 14f);
                var cnt2 = UiKit.Deco(row, count2.ToString(), 17, PaperFx.Ink, TextAnchor.MiddleLeft);
                UiKit.Le(cnt2, -1f, 30f, -1f, 30f);
                var lb2 = UiKit.Txt(row, "消滅", 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
                UiKit.Le(lb2, -1f, 30f, -1f, 30f);
            }
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
                var b = UiKit.Btn(tabs, labels[i], delegate { g.ViewPile = k; g.Rebuild(); }, 18, true, g.ViewPile == k ? UiKit.Hex("#f6dd98") : Color.white);
                SetSize(b, 200f, 44f);
            }
            IReadOnlyList<CardInstance> pile = g.ViewPile == "discard" ? st.Player.DiscardPile : g.ViewPile == "exhaust" ? st.Player.ExhaustPile : st.Player.DrawPile;
            var list = new List<CardInstance>(pile);
            if (g.ViewPile == "draw") list.Sort((a, b) => string.CompareOrdinal(a.Def.Name, b.Def.Name)); // 引き順は伏せたまま
            var content = UiKit.Scroll(inner, true, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.06f), 12, 12);
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
                var none = UiKit.Txt(inner, "（空）", 18, PaperFx.InkSoft, TextAnchor.MiddleCenter);
                UiKit.Le(none, -1f, 40f, -1f, 40f);
            }
            for (int i = 0; i < list.Count; i++)
            {
                var cell = UiKit.NewRect("cell", content);
                var cv = CardView.Build(cell, list[i], st, true, true, "pile-card");
                cv.localScale = Vector3.one * 0.8f;
                CardPopup.Attach(g, cv, list[i], delegate { return g.Rs != null ? g.Rs.Combat : null; }, true);
            }
            CenteredButton(inner, "閉じる", delegate { g.ViewPile = null; g.Rebuild(); }, 18, 260f, 50f);
        }

        static void BuildEndTurn(GameRoot g, RectTransform root, GameState st)
        {
            bool myTurn = st.Phase == CombatPhases.PlayerTurn && g.Pending == null;
            var b = UiKit.Btn(root, "ターン終了", delegate { g.DoCombat(new Command_EndTurn()); }, 21, myTurn, myTurn ? UiKit.Hex("#f6dd98") : Color.white);
            var le = b.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            var brt = b.GetComponent<RectTransform>();
            UiKit.Anchor(brt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-270f, 146f), new Vector2(-40f, 210f));
            brt.localRotation = Quaternion.Euler(0f, 0f, -1f);
            var bt = b.GetComponentInChildren<TMP_Text>();
            if (bt != null && UiKit.FontDeco != null) { bt.font = UiKit.FontDeco; bt.characterSpacing = 4f; }
            var hint = PaperFx.NightNote(root, "手札 " + st.Player.Hand.Count + " · からくり " + st.Player.SetCards.Count + "/" + st.Player.SetSlots, 13, 240f);
            hint.anchorMin = hint.anchorMax = new Vector2(1f, 0f); hint.pivot = new Vector2(1f, 0f);
            hint.anchoredPosition = new Vector2(-40f, 216f);

            // エナジーの太陽 (紙の円盤に蜂蜜色の弧)
            var sun = UiKit.NewRect("energyOrb", root);
            UiKit.Anchor(sun, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(96f, 116f), new Vector2(224f, 244f));
            sun.localRotation = Quaternion.Euler(0f, 0f, -3f);
            var disc = UiKit.NewRect("disc", sun);
            UiKit.Stretch(disc, 6f, 6f, 6f, 6f);
            var dImg = disc.gameObject.AddComponent<Image>();
            dImg.sprite = PaperFx.Disc(); dImg.preserveAspect = true; dImg.raycastTarget = false;
            dImg.color = st.Player.Energy > 0 ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
            var arc = UiKit.NewRect("arc", sun);
            UiKit.Stretch(arc, 0f, 0f, 0f, 0f);
            var aImg = arc.gameObject.AddComponent<Image>();
            aImg.sprite = PaperFx.Ring(7); aImg.color = PaperFx.Honey; aImg.raycastTarget = false;
            aImg.type = Image.Type.Filled; aImg.fillMethod = Image.FillMethod.Radial360; aImg.fillOrigin = 2; aImg.fillClockwise = true;
            aImg.fillAmount = st.Player.EnergyMax > 0 ? Mathf.Clamp01((float)st.Player.Energy / st.Player.EnergyMax) : 0f;
            var et = UiKit.Deco(sun, st.Player.Energy.ToString(), 40, PaperFx.Ink, TextAnchor.MiddleCenter);
            UiKit.Anchor(et.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-8f, 4f), new Vector2(-8f, 6f));
            var em = UiKit.Txt(sun, "/ " + st.Player.EnergyMax, 15, PaperFx.InkSoft, TextAnchor.MiddleLeft, true);
            UiKit.Anchor(em.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(16f, -14f), new Vector2(60f, 8f));
            var el = UiKit.Txt(sun, "エナジー", 13, PaperFx.InkSoft, TextAnchor.MiddleCenter);
            el.characterSpacing = 2f;
            UiKit.Anchor(el.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 20f), new Vector2(0f, 38f));
            g.RegisterAnchor("energy", sun);
        }

        // ---- ログの引き出し ----

        static void BuildLogDrawer(GameRoot g, RectTransform root, GameState st)
        {
            var pan = UiKit.NewRect("logDrawer", root);
            UiKit.Anchor(pan, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-520f, 260f), new Vector2(-12f, -TopH - 8f));
            var sheet = PaperFx.Sheet(pan, PaperFx.Panel, "paper");
            UiKit.Stretch(sheet.rectTransform, 0f, 0f, 0f, 0f);
            var inner = UiKit.NewRect("inner", pan);
            UiKit.Stretch(inner, 16f, 16f, 14f, 14f);
            UiKit.Vert(inner, 4, 0);
            UiKit.Head(inner, "戦闘ログ", 20);
            var content = UiKit.Scroll(inner, true, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.06f), 2, 8);
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
                var t = UiKit.Txt(content, lines[i], 14, PaperFx.Ink);
                UiKit.Le(t, -1f, 20f, -1f, -1f);
            }
        }

        // ---- モーダル: 確認ウィンドウ (set-confirm) ----

        public static RectTransform Modal(RectTransform root, float w, float h, string name)
        {
            // キャンバスに収める (スマホ 1.6倍は 1200×675 しかない。2026-09-14)
            var cs = CanvasSize(root);
            w = Mathf.Min(w, cs.x - 24f); h = Mathf.Min(h, cs.y - 16f);
            var backdrop = UiKit.Pan(root, new Color(20f / 255f, 18f / 255f, 40f / 255f, 0.68f), name + "-backdrop");
            UiKit.Stretch(backdrop.rectTransform, 0f, 0f, 0f, 0f);
            var win = UiKit.Frame(backdrop.transform, Theme.Panel, Color.white, name, 3f);
            UiKit.Anchor(win.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-w / 2f, -h / 2f), new Vector2(w / 2f, h / 2f));
            win.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 0.4f);
            PaperFx.GrainOver(win.transform, 0.6f);
            var inner = UiKit.NewRect("inner", win.transform);
            UiKit.Stretch(inner, 28f, 28f, 22f, 22f);
            UiKit.Vert(inner, 10, 0);
            return inner;
        }

        // ---- 確認ウィンドウ (set-confirm): 帳面から立ち上がる窓 (2026-09-15 案C。旧: 中央のモーダルが舞台と敵を隠していた) ----
        // 行動する敵の札の隣 (左に余裕が無ければ右) に紙の窓が立ち上がり、その敵の列だけ明るく、他の敵・手札・上部バーは暗く沈む。
        // 中身: 「③ 探り屋 の行動の前」／実値×ヒット・盾を差し引いた HP減／候補ごとにトークン＋名前＋「発動したらどうなるか」＋発動／準備中・エナジー不足は灰色で理由／温存。

        static void BuildReactionWindow(GameRoot g, RectTransform root, GameState st)
        {
            var win = Effects.WindowFromPending(st);
            var pending = st.PendingWindow;
            if (win == null || pending == null)
            {
                var inner0 = Modal(root, 600f, 240f, "reaction");
                UiKit.Txt(inner0, "窓の情報を復元できません", 16, UiKit.ColBadInk);
                UiKit.Btn(inner0, "温存して続ける", delegate { g.DoCombat(new Command_ConfirmReaction { Fire = false }); }, 18);
                return;
            }
            bool ph = UiKit.Phone;
            var cs = CanvasSize(root);
            int ei = pending.EnemyIndex;
            string ename = "?";
            try { if (ei >= 0 && ei < st.Enemies.Count) ename = Content.GetEnemyDef(st.Enemies[ei].EnemyId).Name; } catch (Exception) { }
            string num = st.Enemies.Count > 1 && ei >= 0 && ei < Circled.Length ? Circled[ei].ToString() : "";
            // 行動する敵の列 (x の範囲) = 明るく残す穴
            var epan = g.Battle != null ? g.Battle.EnemyPanel(ei) : null;
            float ecx = epan != null ? (epan.offsetMin.x + epan.offsetMax.x) / 2f : cs.x * 0.7f;
            float stripHalf = StripW(epan != null ? g.Battle.EnemyGap(ei) : float.MaxValue, st.Enemies.Count == 1) / 2f;
            float holeL = ecx - Mathf.Max(stripHalf, ph ? 100f : 150f) - 14f, holeR = ecx + Mathf.Max(stripHalf, ph ? 100f : 150f) + 14f;
            float sy = BattleView.StatusLineY;
            var dimCol = new Color(8f / 255f, 8f / 255f, 20f / 255f, 0.55f);
            Dim(root, 0f, 0f, cs.x, sy - 6f, dimCol);          // 手札
            Dim(root, 0f, sy - 6f, holeL, cs.y, dimCol);        // 左 (自分の札・他の敵)
            Dim(root, holeR, sy - 6f, cs.x, cs.y, dimCol);      // 右

            // 窓の大きさ: 候補の行数で伸びる。上部バーに掛かるなら下端を手札側へ下げる (敵の番のあいだ手札は触れない)
            var usable = Effects.UsableSetCards(st, win);
            var un = Effects.UnaffordableSetCards(st, win);
            var others = new List<CardInstance>();
            for (int i = 0; i < st.Player.SetCards.Count; i++)
            {
                var sc = st.Player.SetCards[i];
                bool listed = false;
                for (int j = 0; j < usable.Count; j++) if (usable[j].Uid == sc.Uid) listed = true;
                for (int j = 0; j < un.Count; j++) if (un[j].Uid == sc.Uid) listed = true;
                if (!listed) others.Add(sc);
            }
            var risks = Effects.SetBranchFlipRisks(st);
            int rows = usable.Count + un.Count + others.Count;
            float rowH = ph ? 82f : 90f;
            float W = ph ? 410f : 560f;
            float H = (ph ? 12f + 26f + 6f + 34f + 4f + 22f + 8f : 16f + 30f + 8f + 40f + 6f + 26f + 10f) + (risks.Count > 0 ? 40f : 0f) + (rows > 0 ? rows * rowH : 44f) + (ph ? 8f + 48f + 22f + 10f : 10f + 52f + 24f + 14f);
            float maxH = cs.y - TopH - 12f - 12f;
            if (H > maxH) H = maxH;
            float y0 = Mathf.Min(sy, cs.y - TopH - 12f - H);
            float x = ecx - stripHalf - 10f - W;
            if (x < 12f) x = ecx + stripHalf + 10f;
            if (x + W > cs.x - 12f) x = Mathf.Max(12f, cs.x - 12f - W);
            var panel = PaperFx.Sheet(root, PaperFx.Panel, "reaction");
            UiKit.Anchor(panel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, y0), new Vector2(x + W, y0 + H));
            panel.raycastTarget = true;
            PaperFx.GrainOver(panel.transform, 0.6f);
            var inner = UiKit.NewRect("inner", panel.transform);
            UiKit.Stretch(inner, ph ? 14f : 20f, ph ? 14f : 20f, ph ? 10f : 14f, ph ? 10f : 14f);
            var vg = UiKit.Vert(inner, ph ? 4 : 6, 0);

            // 見出し: 「③ 探り屋 の行動の前（実行前）」
            var head = UiKit.Deco(inner, num + " " + ename + " の" + (win.Stage == "pre" ? "行動の前（実行前）" : "行動の後（解決後）"), ph ? 16 : 19, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(head, -1f, ph ? 26f : 30f, -1f, ph ? 26f : 30f);
            // 実値の行: 絵・実値・×ヒット・盾を差し引いた HP減 (行動の後は実際の HP損失)
            var it = Effects.EffectiveIntent(st, ei) ?? (ei >= 0 && ei < st.Enemies.Count ? st.Enemies[ei].Intent : null);
            var line = UiKit.NewRect("actual", inner);
            UiKit.Le(line, -1f, ph ? 34f : 40f, -1f, ph ? 34f : 40f);
            var lg = UiKit.Horz(line, 8, 0);
            lg.childAlignment = TextAnchor.MiddleLeft; lg.childForceExpandWidth = false; lg.childForceExpandHeight = false;
            if (it != null)
            {
                var intentArt = Theme.Art("icons", "intent_" + it.Kind);
                var ic = UiKit.Icon(line, IntentIcon(it.Kind), 32f, intentArt != null ? Color.white : IntentColor(it.Kind));
                if (intentArt != null) ic.sprite = intentArt;
                ic.rectTransform.sizeDelta = new Vector2(32f, 32f); UiKit.Le(ic, 32f, 32f, 32f, 32f);
                string val = it.Kind == "attack" ? Effects.DisplayedIntentValue(st, ei, it.Kind, it.Actual).ToString() : IntentShort(st, ei, it);
                var vt = UiKit.Deco(line, val, ph ? 24 : 28, PaperFx.Ink, TextAnchor.MiddleLeft);
                UiKit.Le(vt, -1f, 34f, -1f, 34f);
                string tail, sub = null;
                if (win.Stage == "post") { tail = "（実値）"; sub = "この HP損失: <b>" + win.HpLoss + "</b>"; }
                else if (it.Kind == "attack")
                {
                    int hits = Effects.IntentHits(st, it.MirrorHits, it.Hits);
                    int total = Effects.DisplayedIntentValue(st, ei, it.Kind, it.Actual) * hits;
                    int loss = Math.Max(0, total - st.Player.Block);
                    tail = "×" + hits + "（実値）";
                    sub = "盾 " + st.Player.Block + " を差し引いて <color=#9c3a2a><b>HP −" + loss + "</b></color>（" + st.Player.Hp + " → " + Math.Max(0, st.Player.Hp - loss) + "）";
                }
                else tail = CardText.IntentText(st, ei);
                var tt = UiKit.Txt(line, tail, ph ? 13 : 15, PaperFx.InkSoft, TextAnchor.MiddleLeft);
                UiKit.Le(tt, -1f, 34f, -1f, 34f);
                tt.textWrappingMode = TextWrappingModes.NoWrap; tt.overflowMode = TextOverflowModes.Ellipsis;
                if (sub != null)
                {
                    var st2 = UiKit.Txt(inner, sub, ph ? 14 : 16, PaperFx.Ink, TextAnchor.MiddleLeft);
                    UiKit.Le(st2, -1f, ph ? 22f : 26f, -1f, ph ? 22f : 26f);
                    st2.textWrappingMode = TextWrappingModes.NoWrap; st2.overflowMode = TextOverflowModes.Ellipsis;
                }
            }
            if (risks.Count > 0)
            {
                var buf = new List<string>();
                for (int i = 0; i < risks.Count; i++) buf.Add((risks[i] + 1).ToString());
                var wt = UiKit.Txt(inner, "⚠ 動かすとからくりが空き、敵 " + string.Join("・", buf.ToArray()) + " が「からくりなし」の分岐に変わる", ph ? 13 : 15, PaperFx.GoldInk);
                UiKit.Le(wt, -1f, 40f, -1f, 40f);
            }
            var sep = UiKit.Pan(inner, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.35f), "sep");
            UiKit.Le(sep, -1f, 1f, -1f, 1f);
            // 候補の行: トークン＋名前＋発動したらどうなるか＋発動
            for (int i = 0; i < usable.Count; i++)
            {
                var c = usable[i]; string uid = c.Uid;
                string live = null;
                try { live = Effects.SetCardLiveDamage(st, c.Def, ei); } catch (Exception) { }
                int? winLeft = Effects.TrapWindowsLeft(st, c);
                string desc = ReactionDesc(c.Def) + (live != null ? "\n" + live : "");
                ReactionRow(g, inner, st, c, rowH, true, desc, winLeft.HasValue ? "あと" + winLeft.Value + "回" : "期限なし",
                    delegate { g.DoCombat(new Command_ConfirmReaction { Fire = true, CardUid = uid }); });
            }
            for (int i = 0; i < un.Count; i++) ReactionRow(g, inner, st, un[i], rowH, false, "エナジー不足で発動できない（コスト " + un[i].Def.Cost + "・残り " + st.Player.Energy + "）", null, null);
            for (int i = 0; i < others.Count; i++)
            {
                bool liveTrap = Effects.IsTrapLive(st, others[i]);
                ReactionRow(g, inner, st, others[i], rowH, false, liveTrap ? "この窓では動かない（別の窓で鳴る札）" : "準備中（次のターンから）", null, null);
            }
            if (rows == 0)
            {
                var none = UiKit.Txt(inner, "動かせるからくりはありません", 16, PaperFx.InkSoft, TextAnchor.MiddleCenter);
                UiKit.Le(none, -1f, 44f, -1f, 44f);
            }
            // 温存
            var hold = UiKit.Btn(inner, usable.Count > 0 ? "温存する（発動しない）" : "続ける", delegate { g.DoCombat(new Command_ConfirmReaction { Fire = false }); }, ph ? 16 : 18);
            var hle = hold.GetComponent<LayoutElement>(); if (hle != null) { hle.minHeight = ph ? 48f : 52f; hle.preferredHeight = ph ? 48f : 52f; }
            if (usable.Count > 0)
            {
                int? left0 = Effects.TrapWindowsLeft(st, usable[0]);
                var note = UiKit.Txt(inner, "温存すると窓は閉じ、罠は次の窓まで残る" + (left0.HasValue ? "（あと" + Math.Max(0, left0.Value - 1) + "回）" : "（期限なし）"), 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
                UiKit.Le(note, -1f, 20f, -1f, 20f);
            }
        }

        /// <summary>確認の窓に出す札の効果: 窓の名前 (「被攻撃前: 」等) は窓が言うので省き、効果を「・」で並べる</summary>
        static string ReactionDesc(CardDef def)
        {
            string body = CardText.Body(def) ?? "";
            body = body.Replace("被攻撃前: ", "").Replace("被攻撃後: ", "").Replace("敵行動時: ", "").Replace("敵強化時: ", "").Replace("敵防御時: ", "");
            body = body.Replace(" / ", "・").Replace("\n", "・");
            return body;
        }

        /// <summary>確認の窓の1行: トークン (68×74)＋名前・説明＋残り回数、発動できるなら右に発動ボタン</summary>
        static void ReactionRow(GameRoot g, RectTransform inner, GameState st, CardInstance c, float rowH, bool usable, string desc, string left, Action onFire)
        {
            bool ph = UiKit.Phone;
            var row = UiKit.NewRect("cand", inner);
            UiKit.Le(row, -1f, rowH, -1f, rowH);
            var tok = UiKit.NewRect("token", row);
            UiKit.Anchor(tok, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, -PhoneTokenH / 2f), new Vector2(2f + PhoneTokenW, PhoneTokenH / 2f));
            PhoneSetToken(g, tok, st, c);
            CardPopup.Attach(g, tok, c, delegate { return g.Rs != null ? g.Rs.Combat : null; }, true);   // 長押しで札の実物
            if (!usable) { var cg = tok.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0.55f; }
            float btnW = usable ? (ph ? 100f : 120f) : 0f;
            var nameT = UiKit.Deco(row, c.Def.Name, ph ? 15 : 17, usable ? PaperFx.Ink : PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Anchor(nameT.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(PhoneTokenW + 12f, -22f), new Vector2(-btnW - 8f, -2f));
            nameT.textWrappingMode = TextWrappingModes.NoWrap; nameT.overflowMode = TextOverflowModes.Ellipsis;
            if (left != null)
            {
                var lt = UiKit.Txt(row, left, 13, PaperFx.InkSoft, TextAnchor.MiddleRight);
                UiKit.Anchor(lt.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-btnW - 8f - 80f, -22f), new Vector2(-btnW - 8f, -2f));
            }
            var dt = UiKit.Txt(row, desc, 13, usable ? PaperFx.InkSoft : PaperFx.InkSoft, TextAnchor.UpperLeft);
            UiKit.Anchor(dt.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(PhoneTokenW + 12f, 2f), new Vector2(-btnW - 8f, -24f));
            dt.overflowMode = TextOverflowModes.Ellipsis; dt.lineSpacing = -4f;
            if (usable && onFire != null)
            {
                var fb = UiKit.Btn(row, "発動", delegate { onFire(); }, ph ? 16 : 18, true, UiKit.Hex("#f6dd98"));
                var fle = fb.GetComponent<LayoutElement>(); if (fle != null) UnityEngine.Object.Destroy(fle);
                UiKit.Anchor(fb.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-btnW, -24f), new Vector2(0f, 24f));
                var bt = fb.GetComponentInChildren<TMP_Text>();
                if (bt != null && UiKit.FontDeco != null) { bt.font = UiKit.FontDeco; bt.characterSpacing = 3f; }
            }
        }

        /// <summary>暗転の板 (キャンバス座標・左下基準)。押しても何も起きない = 手札を触れなくする</summary>
        static void Dim(RectTransform root, float x0, float y0, float x1, float y1, Color color)
        {
            if (x1 <= x0 || y1 <= y0) return;
            var pan = UiKit.Pan(root, color, "dim");
            UiKit.Anchor(pan.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x0, y0), new Vector2(x1, y1));
            pan.raycastTarget = true;
        }

        // ---- 対象選択・モード選択 ----

        static void BuildTargetBanner(GameRoot g, RectTransform root)
        {
            var pan = PaperFx.Sheet(root, PaperFx.Tag, "targetBanner", UiKit.Hex("#f6dd98"));
            UiKit.Anchor(pan.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-420f, -TopH - 66f), new Vector2(420f, -TopH - 14f));
            var t = UiKit.Txt(pan.transform, "「" + g.Pending.Card.Def.Name + (UiKit.Phone ? "」の対象を選ぶ — 敵をタップ" : "」の対象を選ぶ — 敵をクリック（またはカードを敵へドラッグ）"), 18, PaperFx.Ink, TextAnchor.MiddleLeft, true);
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
            var title = UiKit.Deco(inner, card.Def.Name, 18, PaperFx.Ink, TextAnchor.MiddleLeft);
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
            if (SetBase.CanSetCard(st, card.Uid)) UiKit.Btn(inner, "仕込む", delegate { g.ModeChoiceUid = null; g.DoCombat(new Command_SetCard { CardUid = card.Uid }); }, 16);
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
            else { UiKit.Txt(root, "未対応の選択: " + need, 20, UiKit.ColBadInk); return; }

            var inner = Modal(root, 1400f, 720f, "picker");
            UiKit.Head(inner, p.Card.Def.Name + " — " + title + "（選択中 " + selected.Count + " / " + want + "）", 24);
            var content = UiKit.Scroll(inner, false, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.06f), 16, 12);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 360f, -1f, 360f, -1f, 1f);
            var lg = content.GetComponent<HorizontalLayoutGroup>();
            if (lg != null) { lg.childForceExpandWidth = false; lg.childForceExpandHeight = false; lg.childAlignment = TextAnchor.MiddleLeft; }
            if (pool.Count == 0)
            {
                var none = UiKit.Txt(content, "（候補がありません）", 16, PaperFx.InkSoft);
                UiKit.Le(none, 300f, 40f, 300f, 40f);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                string uid = c.Uid;
                bool isSel = selected.Contains(uid);
                var wrap = UiKit.NewRect("cand", content);
                UiKit.Le(wrap, 220f, 330f, 220f, 330f);
                var cv = CardView.Build(wrap, c, st, !isSel, true, "cand-card");
                CardPopup.Attach(g, cv, c, delegate { return g.Rs != null ? g.Rs.Combat : null; }, true);
                cv.anchoredPosition = new Vector2(0f, 30f);
                cv.localScale = Vector3.one * 0.86f;
                string cap = need;
                var pick = UiKit.Btn(wrap, isSel ? "選択中" : "選ぶ", delegate { OnPick(g, cap, uid); }, 16, true, isSel ? UiKit.Hex("#f6dd98") : Color.white);
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
