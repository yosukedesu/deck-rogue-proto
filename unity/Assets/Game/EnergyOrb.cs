// EnergyOrb.cs — エナジーの表記 (2026-11-04 ユーザー「エナジー表記について本家形を踏襲するようにしてほしい」)。
// 本家 (Slay the Spire 1・2) の形: 左下の丸い宝珠の奥で模様の層がゆっくり逆向きに回り、中央に「残り/上限」(3/3) の数字。
// 0 になると宝珠の色が落ちて回転が止まる。ターンの始めに補充されると宝珠が光る。上限を超えた分は数字だけ (5/3)。弧・目盛り・名札・飛ぶ玉は持たない。
// 色はこの作品の資源の色＝真鍮 (docs/color-theme.md「エナジーは真鍮」)。絵はコードで描く (PaperFx の名前から。16 進は書かない)。
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static class EnergyOrb
    {
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
        const int N = 160;   // 絵の画素 (外径)。表示は 128 前後なので少し大きく描いて縮める

        /// <summary>
        /// 宝珠を root の (x, y) から size 四方に組む。energy/turnMax は今の値、shownBefore は前の組み立てで見せた値 (−1 = 初めて)。
        /// 返す矩形を "energy" の的にする (Presenter・不足の首振りが読む)。最初の Image の子は宝珠の地 (CannotPlay の Flash が朱に光らせる)
        /// </summary>
        public static RectTransform Build(RectTransform root, int energy, int turnMax, float x, float y, float size, int shownBefore)
        {
            bool empty = energy <= 0;
            var orb = UiKit.NewRect("energyOrb", root);
            UiKit.Anchor(orb, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, y), new Vector2(x + size, y + size));

            // 宝珠の地 (最初の Image の子＝不足の朱の閃きを受ける)
            var body = Layer(orb, "disc", Sphere(), empty ? Dim : Color.white, 0f);
            body.raycastTarget = true;   // 説明の的
            // 奥で回る模様 2 枚 (逆向き)・芯の光・縁
            var swA = Layer(orb, "swirlA", UiKit.LinearSprite(Swirl("a", 3, 1.6f), 0.3f), empty ? Hidden : Color.white, 10f);   // Linear の合成で半透明が薄く出るので α を写す (紙の α の罠と同じ)
            var swB = Layer(orb, "swirlB", UiKit.LinearSprite(Swirl("b", 5, -1.1f), 0.3f), empty ? Hidden : new Color(1f, 1f, 1f, 0.7f), 14f);
            var core = Layer(orb, "core", Core(), empty ? Hidden : new Color(1f, 1f, 1f, 0.35f), 26f);
            // 縁の目盛り (2026-11-04 ユーザー「目盛りはいる」): 縁の輪を上限の数で区切り、残りの分を明るい真鍮・使った分を真鍮の墨で。上から右回り。
            // 上限を超えた分 (5/3) は輪が満タンのまま (数字が正)。上限が 2〜12 の時だけ区切る (1 と 13 以上は輪だけ)
            const int Thick = 10;
            var track = Layer(orb, "track", PaperFx.Ring(Thick), PaperFx.BrassInk, 0f);
            track.preserveAspect = true;
            var arc = Layer(orb, "arc", PaperFx.Ring(Thick), PaperFx.BrassLight, 0f);
            arc.type = Image.Type.Filled; arc.fillMethod = Image.FillMethod.Radial360; arc.fillOrigin = 2; arc.fillClockwise = true;
            float f1 = Mathf.Clamp01((float)energy / Mathf.Max(1, turnMax));
            arc.fillAmount = f1;
            if (turnMax >= 2 && turnMax <= 12) Layer(orb, "ticks", PaperFx.RingTicks(Thick, turnMax, 4f), PaperFx.Night, 0f);
            if (shownBefore >= 0 && shownBefore != energy && !ScreenFx.Instant)
            {   // 払うと区切りが暗くなる (0.2 秒)・補充は明るくなりながら満ちる (0.35 秒・少し行き過ぎる)
                float f0 = Mathf.Clamp01((float)shownBefore / Mathf.Max(1, turnMax));
                bool gain = energy > shownBefore;
                var ai = arc; ai.fillAmount = f0;
                Tween.Run(FxSettings.Dur(gain ? 0.35f : 0.2f), k => { if (ai != null) ai.fillAmount = Mathf.Lerp(f0, f1, k); }, gain ? Ease.OutBack : Ease.OutCubic, () => { if (ai != null) ai.fillAmount = f1; });
            }
            // 外の光 (補充の閃きで強くなる)。宝珠の後ろ
            var halo = Layer(orb, "halo", Halo(), new Color(1f, 1f, 1f, empty ? 0f : 0.16f), -size * 0.24f);
            halo.transform.SetAsFirstSibling();   // 光は宝珠の後ろ。宝珠の地は BodyOf で名前から引く (不足の朱の閃き)

            // 数字「残り/上限」 (本家: 一続きの白い数字。上限を超えたら 5/3 のまま)
            string text = energy + "/" + turnMax;
            var t = UiKit.Deco(orb, text, energy >= 10 || turnMax >= 10 ? 32 : 38, empty ? PaperFx.PaperDim : PaperFx.Paper3, TextAnchor.MiddleCenter);
            UiKit.Anchor(t.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-12f, 0f), new Vector2(12f, 0f));
            var hm = UiKit.NumHaloNight(t.font); if (hm != null) t.fontSharedMaterial = hm;   // 暗い縁取り (夜の札の数字と同じ素材)
            t.raycastTarget = false;

            // 回る (模様だけ。0 の時は止まる)。撮影の動きを飛ばす時 (ScreenFx.Instant) も止める＝撮るたびに同じ絵
            var spin = orb.gameObject.AddComponent<Spin>();
            spin.A = swA.rectTransform; spin.B = swB.rectTransform; spin.Core = core; spin.On = !empty && !ScreenFx.Instant;

            // 前の値との差: 補充 (増えた) は宝珠が光って跳ねる・払った (減った) は数字が跳ねる
            if (shownBefore >= 0 && shownBefore != energy && !ScreenFx.Instant)
            {
                if (energy > shownBefore)
                {
                    var h = halo;
                    Tween.Run(FxSettings.Dur(0.45f), k => { if (h != null) h.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.85f, empty ? 0f : 0.16f, k)); }, Ease.OutQuad);
                    Tween.Punch(orb, 0.12f, 0.3f);
                }
                else Tween.Punch(t.rectTransform, 0.2f, 0.25f);
                if (empty && shownBefore > 0)
                {   // 0 になった瞬間: 色が 0.25 秒で落ちる
                    var b = body; b.color = Color.white;
                    Tween.Run(FxSettings.Dur(0.25f), k => { if (b != null) b.color = Color.Lerp(Color.white, Dim, k); }, Ease.OutQuad);
                }
            }

            Tooltip.Attach(body.gameObject, delegate
            {
                return "<b>エナジー " + energy + " / " + turnMax + "</b>\nカードを出す時に払う。自分のターンの始めに " + turnMax + " まで戻る（使わなかった分は持ち越さない）";
            }, false);
            return orb;
        }

        /// <summary>宝珠の地の Image (不足の朱の閃き用)。halo が最初の子なので名前で引く</summary>
        public static Image BodyOf(RectTransform orb)
        {
            if (orb == null) return null;
            var t = orb.Find("disc");
            return t != null ? t.GetComponent<Image>() : null;
        }

        // ---- 色 (0 の時の掛け算。灰は役割の色ではない) ----
        static readonly Color Dim = new Color(0.42f, 0.42f, 0.42f, 1f);
        static readonly Color Hidden = new Color(1f, 1f, 1f, 0f);

        static Image Layer(RectTransform parent, string name, Sprite sprite, Color color, float inset)
        {
            var rt = UiKit.NewRect(name, parent);
            UiKit.Stretch(rt, inset, inset, inset, inset);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = false; img.preserveAspect = true;
            return img;
        }

        /// <summary>模様を回す (Tween.UnscaledDt＝撮影では 1/60 固定)。芯の光はゆっくり呼吸する</summary>
        public sealed class Spin : MonoBehaviour
        {
            public RectTransform A, B; public Image Core; public bool On = true;
            float _t;
            void Update()
            {
                if (!On) return;
                float dt = Tween.UnscaledDt;
                _t += dt;
                if (A != null) A.localRotation = Quaternion.Euler(0f, 0f, -16f * _t);
                if (B != null) B.localRotation = Quaternion.Euler(0f, 0f, 10f * _t);
                if (Core != null) { var c = Core.color; c.a = 0.32f + 0.06f * Mathf.Sin(_t * Mathf.PI * 0.9f); Core.color = c; }
            }
        }

        // ---- 絵 (コードで描く。真鍮の 3 段＋熾) ----

        static Sprite Make(string key, System.Func<float, float, float, Color> f)
        {
            Sprite s;
            if (_cache.TryGetValue(key, out s) && s != null) return s;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[N * N];
            float c = N / 2f, R = N / 2f - 1f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f - c) / R, dy = (y + 0.5f - c) / R;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * N + x] = f(dx, dy, r);
                }
            tex.SetPixels(px); tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = "energy_" + key; _cache[key] = s; return s;
        }

        /// <summary>GLSL の smoothstep(e0, e1, x): x が e0 以下で 0・e1 以上で 1・間はなめらか</summary>
        static float Step(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3f - 2f * t); }

        static float Edge(float r, float rr) { return Mathf.Clamp01((rr - r) * (N / 2f)); }   // 縁の 1 画素のなめらかさ

        /// <summary>宝珠の地: 左上から光が当たる真鍮の球 (芯は明るい真鍮・縁は真鍮の墨)。縁に向かって夜へ沈む</summary>
        static Sprite Sphere()
        {
            return Make("sphere", (dx, dy, r) =>
            {
                if (r > 1f) return new Color(0f, 0f, 0f, 0f);
                float lx = dx + 0.32f, ly = dy - 0.34f;   // 光の当たる所 (左上)
                float l = Mathf.Clamp01(1f - Mathf.Sqrt(lx * lx + ly * ly) / 1.45f);
                Color col = Color.Lerp(PaperFx.BrassInk, PaperFx.Brass, Mathf.SmoothStep(0f, 1f, l));
                col = Color.Lerp(col, PaperFx.BrassLight, Mathf.Pow(l, 3.2f) * 0.85f);
                float rim = Mathf.Pow(r, 5f);   // 縁は夜へ沈む (球の丸み)
                col = Color.Lerp(col, PaperFx.Night, rim * 0.55f);
                col.a = Edge(r, 1f);
                return col;
            });
        }

        /// <summary>奥で回る模様: arms 本の渦の腕 (twist で捻れる)。明るい腕 (熾→明るい真鍮) と暗い谷 (真鍮の墨) の 2 色で、回っているのが見える。芯と縁は薄く</summary>
        static Sprite Swirl(string key, int arms, float twist)
        {
            return Make("swirl_" + key, (dx, dy, r) =>
            {
                if (r > 1f) return new Color(0f, 0f, 0f, 0f);
                float th = Mathf.Atan2(dy, dx);
                float w = 0.5f + 0.5f * Mathf.Sin(arms * th + twist * r * Mathf.PI * 2f);
                float band = Step(0.1f, 0.38f, r) * (1f - Step(0.8f, 0.98f, r)) * Edge(r, 1f);   // Mathf.SmoothStep は GLSL の smoothstep と違う (from〜to の補間) ので自前の Step
                if (w >= 0.5f)
                {
                    float k = Mathf.Pow((w - 0.5f) * 2f, 1.6f);
                    Color lit = Color.Lerp(PaperFx.Ember, PaperFx.BrassLight, k);
                    lit.a = Mathf.Min(1f, k * 1.15f) * band;
                    return lit;
                }
                float q = Mathf.Pow((0.5f - w) * 2f, 1.6f);
                Color dark = PaperFx.BrassInk;
                dark.a = q * 0.85f * band;
                return dark;
            });
        }

        /// <summary>芯の光 (真ん中が明るい真鍮・外へ消える)。数字の後ろを少し明るくして読ませる</summary>
        static Sprite Core()
        {
            return Make("core", (dx, dy, r) =>
            {
                float a = Mathf.Clamp01(1f - r);
                Color col = PaperFx.BrassLight;
                col.a = a * a;
                return col;
            });
        }

        /// <summary>縁の金具: 真鍮の墨の輪に、左上だけ明るい真鍮の光の線 (2026-11-04 目盛りの輪に替えたので今は使わない。戻す時の口)</summary>
        static Sprite Bezel()
        {
            return Make("bezel", (dx, dy, r) =>
            {
                const float inner = 0.88f;
                if (r > 1f || r < inner - 0.02f) return new Color(0f, 0f, 0f, 0f);
                float th = Mathf.Atan2(dy, dx);   // 左上 (135°) が明るい
                float lit = 0.5f + 0.5f * Mathf.Cos(th - 2.36f);
                float mid = 1f - Mathf.Abs((r - (inner + 1f) / 2f) / ((1f - inner) / 2f));   // 輪の真ん中が明るい
                Color col = Color.Lerp(PaperFx.BrassInk, PaperFx.Brass, 0.35f + 0.4f * mid);
                col = Color.Lerp(col, PaperFx.BrassLight, Mathf.Pow(lit, 3f) * mid * 0.8f);
                col.a = Edge(r, 1f) * Mathf.Clamp01((r - (inner - 0.02f)) * (N / 2f));
                return col;
            });
        }

        /// <summary>外の光 (宝珠の後ろ・真鍮の淡い光)。普段は薄く、補充の瞬間に強く</summary>
        static Sprite Halo()
        {
            return Make("halo", (dx, dy, r) =>
            {
                float a = Mathf.Clamp01((1f - r) / 0.45f);   // 外径の 0.55〜1.0 で消える
                Color col = PaperFx.BrassLight;
                col.a = a * a * (r > 0.3f ? 1f : r / 0.3f);
                return col;
            });
        }
    }
}
