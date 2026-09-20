// LightUi.cs — 灯の器＝真鍮のランタン (2026-09-20 ユーザー「ひなたの戦闘画面に灯表記をエナジーくらいリッチに表現してほしい」→ デザインカンバス「灯の表示」案B)。
// エナジーの輪の右に 32×48 ドットの灯籠を立て、硝子の窓に数字 (実処理と同じ Player.Light)、その後ろで炎が灯の量で育つ:
//   0 消えている (硝子は暗く枠も沈む・数字は薄墨)／1〜2 ともる／3〜5 灯る (足元に光溜まり)／6〜9 盛る (火の粉が舞う)／10〜 眩い (芯が白く光溜まりが広がる)。
// 上限は無いので「満ちる」は言わない＝溜めるほど明るい。白の色を持つリーダーは灯0でも出す (消灯)、他のリーダーは灯1以上で現れる。
// 出来事 (LightGained／LightSpent／LightDischarged) の的は anchor "light"。順送りの間 (古い盤面の上) は Presenter が Add/Spend/Drain で動かし、組み直しは最後に見せた値から始める (HP の Nudge と同じ器)。
// 絵: ThemeFx.Lantern (差し替え Art/ui/lantern.png)・ThemeFx.Flame (Art/fx/flame.png)・光は ThemeFx.Glow。
using System;
using System.Collections.Generic;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static class LightUi
    {
        public const float DotsW = 32f, DotsH = 48f;
        // 硝子の窓 (ドット): x 7〜25・y 14〜36 (上が 0)
        const float GlassX = 7f, GlassY = 14f, GlassW = 18f, GlassH = 22f;

        static RectTransform _root, _glass, _flameRt, _previewRt, _glowRt, _poolRt;
        static Image _lantern, _glassFill, _flame, _glow, _pool;
        static TMP_Text _num, _previewText;
        static Image _previewBg;
        static float _scale = 3f;
        static int _shown = -1;
        static object _combatKey;
        static Flicker _flicker;
        static float _holdUntil = -1f;   // Presenter が粒の到着を待たせている間は、組み直しでも見せている値を保つ

        /// <summary>いま見せている灯 (順送りの途中は盤面より古い)。器が無ければ -1</summary>
        public static int Shown { get { return _root != null ? _shown : -1; } }
        public static bool Exists { get { return _root != null; } }

        /// <summary>灯の段階 (0 消灯／1 ともる／2 灯る／3 盛る／4 眩い)</summary>
        public static int Tier(int n) { return n <= 0 ? 0 : n <= 2 ? 1 : n <= 5 ? 2 : n <= 9 ? 3 : 4; }
        public static string TierName(int n) { switch (Tier(n)) { case 0: return "消えている"; case 1: return "ともる"; case 2: return "灯る"; case 3: return "盛る"; default: return "眩い"; } }

        /// <summary>器を出すか: 白の色を持つリーダーは常に (灯0でも消灯の灯籠)、他は灯が1以上の時だけ</summary>
        public static bool ShouldShow(RunState run, GameState st)
        {
            if (st == null) return false;
            if ((st.Player.Light ?? 0) > 0) return true;
            if (run == null) return false;
            try
            {
                var leader = Content.GetLeaderDef(run.LeaderId);
                if (leader != null && leader.Colors != null) foreach (var c in leader.Colors) if (c == "white") return true;
            }
            catch (Exception) { }
            return false;
        }

        /// <summary>
        /// 灯籠を組む。baseX/baseY = 台座の下端の中心 (root の左下からの px)。scale = 1ドットの px (PC 3・スマホ 2.5)。
        /// 見せる値は、同じ戦闘で前に見せていた値があればそれ (順送りの続きを Presenter が動かす)、無ければ盤面の値
        /// </summary>
        public static void Build(GameRoot g, RectTransform root, GameState st, float baseX, float baseY, float scale)
        {
            _scale = scale;
            float w = DotsW * scale, h = DotsH * scale;
            object key = st.EventLog.Count > 0 ? (object)st.EventLog[0] : null;
            // 見せる値: まだ演出していない出来事があれば「その前の値」(Presenter が粒の到着で増やす)。演出の途中 (Hold) なら見せていた値のまま
            bool same = _shown >= 0 && ReferenceEquals(key, _combatKey);
            if (!(same && Time.time < _holdUntil)) _shown = Presenter.LightBefore(st);
            _combatKey = key;

            _root = UiKit.NewRect("lightLantern", root);
            UiKit.Anchor(_root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(baseX - w / 2f, baseY), new Vector2(baseX + w / 2f, baseY + h));
            g.RegisterAnchor("light", _root);

            // 足元の光溜まり (灯る以上)
            _poolRt = UiKit.NewRect("pool", _root);
            _poolRt.anchorMin = _poolRt.anchorMax = new Vector2(0.5f, 0f);
            _poolRt.anchoredPosition = new Vector2(0f, 3f * scale);
            _pool = _poolRt.gameObject.AddComponent<Image>();
            _pool.sprite = ThemeFx.Glow(); _pool.raycastTarget = false;
            // 硝子から漏れる光 (絵の後ろ)
            _glowRt = UiKit.NewRect("glow", _root);
            _glowRt.anchorMin = _glowRt.anchorMax = new Vector2(0.5f, 0f);
            _glowRt.anchoredPosition = new Vector2(0f, h - (GlassY + GlassH * 0.55f) * scale);
            _glow = _glowRt.gameObject.AddComponent<Image>();
            _glow.sprite = ThemeFx.Glow(); _glow.raycastTarget = false;
            // 灯籠の絵
            var lRt = UiKit.NewRect("lantern", _root);
            UiKit.Stretch(lRt, 0f, 0f, 0f, 0f);
            _lantern = lRt.gameObject.AddComponent<Image>();
            _lantern.raycastTarget = true;
            // 硝子の窓 (絵の上): 炎の光の地 → 炎 → 数字
            _glass = UiKit.NewRect("glass", _root);
            UiKit.Anchor(_glass, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(GlassX * scale, -(GlassY + GlassH) * scale), new Vector2((GlassX + GlassW) * scale, -GlassY * scale));
            var gf = UiKit.NewRect("fill", _glass);
            UiKit.Stretch(gf, 0f, 0f, 0f, 0f);
            _glassFill = gf.gameObject.AddComponent<Image>();
            _glassFill.sprite = ThemeFx.Gradient("lantern-glass", PaperFx.Brass, PaperFx.BrassLight);   // 上が真鍮・下が真鍮の紙。白い芯は炎の絵が持つ
            _glassFill.raycastTarget = false;
            _flameRt = UiKit.NewRect("flame", _glass);
            _flameRt.anchorMin = _flameRt.anchorMax = new Vector2(0.5f, 0f);
            _flameRt.pivot = new Vector2(0.5f, 0f);
            _flameRt.anchoredPosition = new Vector2(0f, 1f * scale);
            _flame = _flameRt.gameObject.AddComponent<Image>();
            _flame.sprite = ThemeFx.Flame(); _flame.raycastTarget = false;
            _flicker = _flameRt.gameObject.AddComponent<Flicker>();
            _num = UiKit.Deco(_glass, "0", Mathf.RoundToInt(GlassH * scale * 0.62f), PaperFx.Ink, TextAnchor.MiddleCenter);
            UiKit.Stretch(_num.rectTransform, 0f, 0f, 0f, 0f);
            _num.raycastTarget = false;
            _num.outlineWidth = 0.12f; _num.outlineColor = new Color(1f, 0.96f, 0.82f, 0.9f);
            // 火の粉 (盛る以上)
            _root.gameObject.AddComponent<Sparks>().Init(_root, scale);
            // 足元の名札「灯」＝タップの的 (用語の説明と、この戦闘の入りの内訳)
            var tag = UiKit.NewRect("tag", _root);
            float tw = 40f, th = 20f;
            UiKit.Anchor(tag, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-tw / 2f, -th - 4f), new Vector2(tw / 2f, -4f));
            var tagImg = PaperFx.Sheet(tag, PaperFx.Tag2, "paper");
            UiKit.Stretch(tagImg.rectTransform, 0f, 0f, 0f, 0f);
            tagImg.raycastTarget = true;
            var tagTx = UiKit.Txt(tag, "灯", 13, PaperFx.InkSoft, TextAnchor.MiddleCenter);
            UiKit.Stretch(tagTx.rectTransform, 0f, 0f, 0f, 0f);
            tagTx.raycastTarget = false;
            var gRef = g;
            Func<string> tip = delegate { return Describe(gRef); };
            Tooltip.Attach(tag.gameObject, tip, false);
            Tooltip.Attach(lRt.gameObject, tip, false);
            // 頭上の予告 (札をつかんだ時だけ)
            _previewRt = UiKit.NewRect("preview", _root);
            _previewRt.anchorMin = _previewRt.anchorMax = new Vector2(0.5f, 1f);
            _previewRt.pivot = new Vector2(0.5f, 0f);
            _previewRt.anchoredPosition = new Vector2(0f, 6f);
            _previewRt.sizeDelta = new Vector2(150f, 26f);
            _previewBg = PaperFx.Sheet(_previewRt, PaperFx.Tag, "paper", PaperFx.BrassLight);
            UiKit.Stretch(_previewBg.rectTransform, 0f, 0f, 0f, 0f);
            _previewBg.raycastTarget = false;
            _previewText = UiKit.Deco(_previewRt, "", 15, PaperFx.BrassInk, TextAnchor.MiddleCenter);
            UiKit.Stretch(_previewText.rectTransform, 4f, 4f, 0f, 0f);
            _previewText.raycastTarget = false;
            _previewRt.gameObject.SetActive(false);

            Apply(_shown);
        }

        /// <summary>名札とランタンの説明: 用語「灯」＋この戦闘の入り (灯匠・灯芯の人形・回復) の内訳</summary>
        static string Describe(GameRoot g)
        {
            var st = g != null && g.Rs != null ? g.Rs.Combat : null;
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>灯</b> ");
            string help;
            if (KeywordHelp.Terms.TryGetValue("灯", out help)) sb.Append("<color=#574b48>" + help + "</color>");
            if (st != null)
            {
                int perTurn = 0; var parts = new List<string>();
                foreach (var p in st.Player.Permanents)
                {
                    int add = 0;
                    foreach (var e in p.Def.Effects) if (e.Trigger == "onTurnStart" && e.Effect == "addLight") add += e.Amount ?? 0;
                    if (add > 0) { perTurn += add; parts.Add(p.Def.Name + " +" + add); }
                }
                sb.Append("\n<color=#634410>いま灯 " + (st.Player.Light ?? 0) + " (" + TierName(st.Player.Light ?? 0) + ")");
                if (perTurn > 0) sb.Append("・毎ターン開始の入り +" + perTurn + " (" + string.Join("・", parts.ToArray()) + ")");
                sb.Append("・回復するたび +1</color>");
            }
            return sb.ToString();
        }

        /// <summary>演出の粒が届くまで、組み直しがあっても見せている値を保つ (秒)</summary>
        public static void HoldFor(float seconds) { _holdUntil = Mathf.Max(_holdUntil, Time.time + seconds); }

        /// <summary>見た目を灯 n に (数字・炎の段階・光)。animate=炎がひと膨らみ</summary>
        public static void SetLight(int n, bool animate)
        {
            n = Math.Max(0, n);
            if (_root == null) { _shown = n; return; }
            bool up = n > _shown;
            _shown = n;
            Apply(n);
            if (animate) Flare(up ? 0.22f : 0.1f);
        }
        public static void Add(int delta, bool animate) { SetLight(_shown + delta, animate); }

        static void Apply(int n)
        {
            if (_root == null) return;
            int t = Tier(n);
            float s = _scale, w = DotsW * s, h = DotsH * s;
            _lantern.sprite = ThemeFx.Lantern(t > 0);
            _lantern.color = Color.white;
            _num.text = n.ToString();
            _num.color = t == 0 ? PaperFx.PaperDim : PaperFx.Ink;
            _num.outlineWidth = t == 0 ? 0f : 0.12f;
            _glassFill.gameObject.SetActive(t > 0);
            _glassFill.color = new Color(1f, 1f, 1f, t >= 4 ? 0.95f : 0.5f + 0.1f * t);
            _flameRt.gameObject.SetActive(t > 0);
            float[] fk = { 0f, 0.55f, 0.8f, 1.0f, 1.15f };
            _flameRt.sizeDelta = new Vector2(12f * s * fk[t], 16f * s * fk[t]);
            if (_flicker != null) _flicker.Base = _flameRt.sizeDelta;
            _glowRt.gameObject.SetActive(t > 0);
            _glowRt.sizeDelta = new Vector2(w * 1.9f, h * 0.72f);
            _glow.color = new Color(1f, 0.9f, 0.62f, t == 0 ? 0f : 0.16f + 0.08f * t);
            _poolRt.gameObject.SetActive(t >= 2);
            _poolRt.sizeDelta = new Vector2(w * (0.9f + 0.3f * t), h * 0.16f * (0.7f + 0.2f * t));
            _pool.color = new Color(1f, 0.92f, 0.66f, 0.12f + 0.06f * t);
            var sp = _root.GetComponent<Sparks>();
            if (sp != null) sp.Tier = t;
        }

        /// <summary>炎がひと膨らみ (灯を得た・払った時)</summary>
        public static void Flare(float amount)
        {
            if (_root == null) return;
            if (_flameRt.gameObject.activeSelf) Tween.Punch(_flameRt, amount * 2f, 0.32f, true);
            Tween.Punch(_glass, amount * 0.5f, 0.28f);
            if (_glowRt.gameObject.activeSelf)
            {
                var c = _glow.color; float a0 = c.a;
                Tween.Run(0.35f, k => { if (_glow != null) _glow.color = new Color(c.r, c.g, c.b, a0 + (0.35f - a0) * (1f - k)); }, Ease.OutQuad);
            }
        }

        /// <summary>放出: 数字が from→0 へ減り、硝子が暗くなる (炎が抜けた後)</summary>
        public static void Drain(int from, float dur = 0.4f)
        {
            if (_root == null) { _shown = 0; return; }
            int start = Math.Max(from, _shown);
            Tween.Run(dur, k =>
            {
                if (_num == null) return;
                int v = Mathf.RoundToInt(Mathf.Lerp(start, 0f, k));
                _num.text = v.ToString();
                if (_glassFill != null) { var c = _glassFill.color; _glassFill.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.9f, 0.2f, k)); }
                if (_flameRt != null && _flameRt.gameObject.activeSelf) _flameRt.localScale = new Vector3(1f - 0.7f * k, 1f - 0.8f * k, 1f);
            }, Ease.OutQuad, () => { if (_flameRt != null) _flameRt.localScale = Vector3.one; SetLight(0, false); });
        }

        /// <summary>灯が足りない札を押した: 灯籠が首を振り、硝子が朱に一瞬光る</summary>
        public static void Insufficient()
        {
            if (_root == null) return;
            Tween.Shake(_root, 6f, 0.28f);
            if (_glassFill != null && _glassFill.gameObject.activeSelf) Tween.Flash(_glassFill, PaperFx.Rose, 0.4f);
            else if (_lantern != null) Tween.Flash(_lantern, PaperFx.Rose, 0.4f);
        }

        /// <summary>札をつかんだ時の予告 (灯コスト・灯N以上・放出) を頭上に。無い札なら消す</summary>
        public static void PreviewFor(GameState st, CardInstance c)
        {
            if (_root == null || st == null || c == null) return;
            int light = st.Player.Light ?? 0;
            string text = null; Color ink = PaperFx.BrassInk, bg = PaperFx.BrassLight;
            int cost = c.Def.LightCost ?? 0;
            if (cost > 0)
            {
                if (light >= cost) text = "-" + cost + " → " + (light - cost);
                else { text = "灯" + cost + " が要る (いま " + light + ")"; ink = PaperFx.BadInk; bg = PaperFx.RoseLight; }
            }
            else
            {
                int min = 0; bool discharge = false, rally = false, weaken = false, consume = false;
                var effs = new List<DeclarativeEffect>(c.Def.Effects);
                if (c.Def.Modes != null) foreach (var m in c.Def.Modes) effs.AddRange(m.Effects);
                foreach (var e in effs)
                {
                    if (e.Condition != null && e.Condition.MinLight.HasValue) min = Math.Max(min, e.Condition.MinLight.Value);
                    if (e.Effect == "dischargeLight") discharge = true;
                    else if (e.Effect == "dischargeLightRally") rally = true;
                    else if (e.Effect == "dischargeLightWeaken") weaken = true;
                    else if (e.Effect == "consumeLight" || e.Effect == "lightToSparks") consume = true;
                }
                if (min > 0)
                {
                    if (light >= min) { text = "灯" + min + "以上 OK"; ink = PaperFx.GoodInk; bg = PaperFx.MossLight; }
                    else { text = "灯" + min + "以上 不足 (いま " + light + ")"; ink = PaperFx.BadInk; bg = PaperFx.RoseLight; }
                }
                else if (discharge) text = light > 0 ? "放出 " + light + " → 0" : "灯0＝不発";
                else if (rally) text = light > 0 ? "放出 " + light + "＝人形が" + light + "回" : "灯0＝不発";
                else if (weaken) text = light >= 3 ? "放出 " + light + "＝威圧" + (light / 3) : "灯3未満＝不発";
                else if (consume) text = light > 0 ? "灯" + light + " を失う" : null;
                if (text != null && (text.Contains("不発"))) { ink = PaperFx.BadInk; bg = PaperFx.RoseLight; }
            }
            if (text == null) { HidePreview(); return; }
            _previewText.text = text; _previewText.color = ink; _previewBg.color = bg;
            _previewText.ForceMeshUpdate();
            _previewRt.sizeDelta = new Vector2(Mathf.Max(60f, _previewText.preferredWidth + 22f), 26f);
            _previewRt.gameObject.SetActive(true);
        }
        public static void HidePreview() { if (_previewRt != null) _previewRt.gameObject.SetActive(false); }

        /// <summary>硝子の窓の中心 (layer の座標)。粒の的</summary>
        public static Vector2 GlassCenter(RectTransform layer) { return _glass != null ? Tween.CenterIn(_glass, layer) : Vector2.zero; }
        public static Vector2 TopCenter(RectTransform layer)
        {
            if (_root == null) return Vector2.zero;
            var c = Tween.CenterIn(_root, layer);
            return c + new Vector2(0f, _root.rect.height / 2f);
        }

        /// <summary>炎のゆらぎ (deltaTime の積算＝コマ送り撮影でも決定的)</summary>
        class Flicker : MonoBehaviour
        {
            public Vector2 Base;
            float _t;
            void LateUpdate()
            {
                var rt = transform as RectTransform;
                if (rt == null || Base.x <= 0f) return;
                _t += Time.deltaTime;
                float sx = 1f + 0.05f * Mathf.Sin(_t * 23f) + 0.03f * Mathf.Sin(_t * 7.3f);
                float sy = 1f + 0.07f * Mathf.Sin(_t * 17f + 1.3f) + 0.04f * Mathf.Sin(_t * 5.1f);
                rt.sizeDelta = new Vector2(Base.x * sx, Base.y * sy);
            }
        }

        /// <summary>火の粉: 盛る (段3) 以上で笠のあたりから小さな真鍮の粒が立ちのぼる</summary>
        class Sparks : MonoBehaviour
        {
            public int Tier;
            RectTransform _lantern; float _scale, _acc;
            System.Random _rng = new System.Random(7);
            public void Init(RectTransform lantern, float scale) { _lantern = lantern; _scale = scale; }
            void Update()
            {
                if (Tier < 3 || _lantern == null) return;
                _acc += Time.deltaTime;
                float every = Tier >= 4 ? 0.22f : 0.42f;
                if (_acc < every) return;
                _acc = 0f;
                var rt = UiKit.NewRect("spark", _lantern);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                float x0 = ((float)_rng.NextDouble() - 0.5f) * DotsW * _scale * 0.5f;
                float y0 = -12f * _scale + (float)_rng.NextDouble() * 6f * _scale;
                rt.anchoredPosition = new Vector2(x0, y0);
                float sz = (1.5f + (float)_rng.NextDouble() * 1.5f) * _scale;
                rt.sizeDelta = new Vector2(sz, sz);
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = ThemeFx.Spark(); img.color = _rng.NextDouble() < 0.5 ? PaperFx.BrassLight : UiKit.Hex("#fff6d2"); img.raycastTarget = false;
                float rise = (14f + (float)_rng.NextDouble() * 10f) * _scale, drift = ((float)_rng.NextDouble() - 0.5f) * 8f * _scale;
                Tween.Run(0.9f + (float)_rng.NextDouble() * 0.4f, k =>
                {
                    if (rt == null) return;
                    rt.anchoredPosition = new Vector2(x0 + drift * k + Mathf.Sin(k * 9f) * 2f * _scale, y0 + rise * k);
                    var c = img.color; img.color = new Color(c.r, c.g, c.b, k < 0.6f ? 0.9f : 0.9f * (1f - (k - 0.6f) / 0.4f));
                }, Ease.Linear, () => { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
            }
        }
    }
}
