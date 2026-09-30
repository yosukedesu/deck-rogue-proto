// StageUnits.cs — Stage のキャラの板 (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3・§2-3)。
// P02 (W1) で Stage.cs から移した部分 (中身は1文字も変えていない): アニメの状態・板の明暗と主役の照明の定数・板の登録簿 (_bound・_depths)・
// BindUnit・FeetPad・PlayAnim・DebugAnim・Flash・Dissolve・StageUnit・光と影の共通部品 (SpriteMat・VignetteLift・ApplyLight・BlobTex・Blob・PlaceBlob)。
// TryGetUnitBox・DebugUnitBoxes の中身は P11 が書く (アダプタ)。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static partial class Stage
    {
        struct AnimState { public string Anim; public int Frame; public float FrameT; public float At; }
        static readonly Dictionary<string, AnimState> _animStates = new Dictionary<string, AnimState>();
        // キャラの板の明暗 (2026-09-16 ユーザー「このはも反射と影がすごくない？」): 月光の勾配は板の中で右上 1+0.7×A・左下 1−0.7×A、リムは右上の縁を空色へ寄せる割合。
        // 旧 0.5／0.5 は太い墨線のこのは v2 で「左半分が影・右の縁が光る」と読めた → 0.25／0.2 に緩めた (敵も同じ板なので同時に緩む)
        const float UnitSunAmount = 0.25f, UnitRim = 0.2f;
        // 敵と人形の板のリム (2026-09-29 I24 で 0.3 に上げた): 環境光を白寄りにした分、暗い墨線の敵 (オーガ・コボルト) の輪郭を足元の地面から離すため。
        // 2026-09-30 F05 (ユーザー裁定「主人公を持ち上げる」): 敵と人形の縁がリーダー (0.2) より強く光り「主役の照明」の序列が逆になった → 主役とそろえる
        const float CharRim = UnitRim;
        // リーダーの板の環境光 (主役の照明)。旧 1.15 = ACES と色補正が沈める中間調を戻す値 (2026-09-16)。
        // 2026-09-30 F05 裁定で 1.35: 敵を白寄りの環境光にした後 (I24)、このはの体 (線形 Y 0.064) が狼 0.21・妖術師 0.23 より暗く画面でいちばん暗いキャラになっていた。
        // 縁の光ではなく体の中間調を持ち上げる (09-16「反射と影がすごい」の裁定＝勾配とリムは触らない)
        const float HeroAmbient = 1.35f;
        // 主役の照明はリーダーの絵ごとに変えられる (2026-09-30 F05 の続き)。1.35 のままでは、このはの体が幕1で線形 Y 0.077〜0.083 にとどまり、目標 0.09 に届かなかった。
        // 原因は光より絵の側: 元の絵の明るさ (線形 Y) は このは 0.086 (黒鉄の衣と墨線)・妖術師 0.315・狼 0.340 で、このはだけ最初から敵の約4分の1。
        // ひなたは白い衣が幕3ですでに白に近いので 1.35 のまま (共用の値を上げると衣が飛ぶ)。このはだけ 1.5 = 予測 Y 0.090〜0.096・白飛び 1% のまま。
        // 敵÷このは ≤2.2 には 1.6 が要り、その時は白飛び 5% になるので 1.5 で止める (クリーム色・白い敵の場面の比は次の裁定)
        static float HeroAmbientFor(string art)
        {
            return art == "leader_green" ? 1.5f : HeroAmbient;
        }
        // ランタンの暖色は、リーダーの環境光が 1.15 の時にシェーダの lamp×1.15 と R がそろう値。環境光だけ上げるとランタン側 (左) の方が暗くなる
        // (lerp(1.35, lamp×1.15, lf) の G・B が下がる = 光源の側が暗い逆の陰影。このはの体は lf 0.36〜0.80 でランタンに近く、1.35 の半分しか効かない) →
        // リーダーだけランタンの色も (主役の照明)/1.15 倍にして、暖色の差 (近い側が暖かい) はそのままに体全体を 1.35/1.15 倍 (このは 1.5/1.15 倍) にする
        const float HeroLampRef = 1.15f;
        // 人形の板の環境光 = 小物・木の UnitAmbient と敵の CharAmbient の間 (2026-09-30 F05 裁定 Lerp 0.6)。
        // 人形は味方の板なので敵ほど白く立てず、ひなた (主役) を頭一つ上に残す (I24 の後は盾の人形・聖歌の人形がひなたの 85% まで来ていた)
        const float DollAmbientMix = 0.6f;
        static readonly Dictionary<string, StageUnit> _bound = new Dictionary<string, StageUnit>();
        static readonly Dictionary<string, float> _depths = new Dictionary<string, float>();

        /// <summary>
        /// キャラの板 (key = "player"・"enemy0"…・"doll:&lt;uid&gt;") の足元の中心の世界の点と、板の世界の高さ。演出で動いている時はその位置。
        /// 無ければ false。骨組み: P11 が書く
        /// </summary>
        public static bool TryGetUnitBox(string key, out Vector3 feet, out float heightWorld)
        {
            feet = default; heightWorld = 0f;
            return false;
        }

        /// <summary>
        /// 「UI の矩形の画面の箱」と「板をレイアウト用のカメラで写した箱」を並べた記録 (JSON にできる値)。
        /// 静止で1px・演出中で2px を超えたものに印を付けて dumplayout に出す。骨組み: P11 が書く (無ければ null)
        /// </summary>
        public static object DebugUnitBoxes() => null;

        // ---------------------------------------------------------------- キャラ (UI の矩形に追従するビルボード)

        /// <summary>UI の矩形 (uGUI の sprite 枠) に追従するビルボードを、key の座席の深度の面に立てる。Image は非表示にして色 (生死・点滅) だけ読む</summary>
        public static void BindUnit(string key, RectTransform rect, Image img, Sprite sprite)
        {
            Ensure();
            if (rect == null || sprite == null || sprite.texture == null) return;
            if (img != null) img.enabled = false;
            float depth;
            if (!_depths.TryGetValue(key, out depth)) depth = _dist;

            var go = new GameObject("unit-" + key);
            go.transform.SetParent(_units, false);
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = _quad;
            var mr = go.AddComponent<MeshRenderer>();
            var mat = SpriteMat(sprite.texture, 0.4f, 0.35f);
            var tr = sprite.textureRect;
            float tw = sprite.texture.width, th = sprite.texture.height;
            mat.SetTextureScale("_BaseMap", new Vector2(tr.width / tw, tr.height / th));
            mat.SetTextureOffset("_BaseMap", new Vector2(tr.x / tw, tr.y / th));
            mat.SetFloat("_Rim", UnitRim);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;   // 影は接地影 (楕円) で
            mr.receiveShadows = false;
            var sh = Blob("shadow-" + key, _units, Vector3.zero, 1f);
            var u = go.AddComponent<StageUnit>();
            u.Rect = rect; u.Img = img; u.Mat = mat; u.Rend = mr; u.Depth = depth; u.Shadow = sh.transform;
            u.FeetPad = FeetPad(sprite);   // 絵の下端の透明行 (足元の余白) の割合。板をそのぶん下げて足を地面に着ける (2026-09-08「キャラが地面から浮いてる」)
            u.BaseTex = sprite.texture;
            u.BaseUvScale = new Vector2(tr.width / tw, tr.height / th); u.BaseUvOffset = new Vector2(tr.x / tw, tr.y / th);
            u.BreathePhase = (key.GetHashCode() & 0xff) * 0.05f;   // 位相をずらして全員が同期しない
            // コマ: Art/<種別>/anim/<id>_<動き>_<n>.png (64×64。PixelLab の animate-with-text)。無ければ一枚絵のまま
            string cat = key == "player" ? "leaders" : "enemies";
            foreach (var anim in new[] { "idle", "attack", "hurt", "block" })
            {
                var list = new List<Texture2D>();
                for (int i = 0; i < 16; i++)
                {
                    var f = Theme.Art(cat + "/anim", sprite.name + "_" + anim + "_" + i);
                    if (f == null) break;
                    list.Add(f.texture);
                }
                if (list.Count > 0) u.Anims[anim] = list;
            }
            u.Breathe = !u.Anims.ContainsKey("idle");   // 待機のコマ (PixelLab の呼吸) があるならコードの上下はしない (2026-09-16)
            // このは v2 (2026-09-16): PixelLab の17コマから 頭上→溜め(刃が光る)→着弾→食い込み の4コマ。着弾は 0.13 秒後 (Presenter の踏み込み 0.11 に合わせる)
            u.FrameDur["attack"] = new[] { 0.05f, 0.08f, 0.04f, 0.18f };
            u.FrameDur["hurt"] = new[] { 0.06f, 0.16f, 0.10f };
            u.FrameDur["block"] = new[] { 0.06f, 0.08f, 0.10f, 0.20f };   // 9コマの 2,4,6,8 = 斧を前に回して構える
            // 盤面の作り直し (Rebuild) で板が作り直されても、再生中のコマ送りは引き継ぐ (攻撃コマが Rebuild で消えていた)
            u.Key = key;
            AnimState st0;
            if (_animStates.TryGetValue(key, out st0) && st0.Anim != "idle" && u.Anims.ContainsKey(st0.Anim) && Time.time - st0.At < 2f)
            {
                u.Anim = st0.Anim; u.Frame = st0.Frame; u.FrameT = st0.FrameT; u.Apply();
            }
            else if (u.Anims.ContainsKey("idle")) u.Play("idle");
            if (key == "player")
            {
                u.HeroLight = HeroAmbientFor(sprite.name);   // 主役の照明はリーダーの絵ごと (F05 の続き)
                // 杖の先の光: 板の右上 (絵の uv≈0.64,0.93) に追従する淡い暖色のハロー
                var halo = new GameObject("staff-glow");
                halo.transform.SetParent(_units, false);
                halo.AddComponent<MeshFilter>().sharedMesh = _quadCentered;
                var hmr = halo.AddComponent<MeshRenderer>();
                hmr.sharedMaterial = GlowMaterial(Px.Radial(new Color(1f, 0.86f, 0.5f, 0.5f)));
                hmr.shadowCastingMode = ShadowCastingMode.Off; hmr.receiveShadows = false;
                u.Halo = halo.transform; u.HaloUv = new Vector2(0.64f, 0.93f);
            }
            u.LateUpdate();
            _bound[key] = u;
        }

        static readonly Dictionary<Texture2D, float> _feetPad = new Dictionary<Texture2D, float>();
        /// <summary>絵の矩形の中で、いちばん下の不透明ドットより下にある透明行の割合 (0〜1)。PixelLab の絵は下に数ドットの余白がある</summary>
        static float FeetPad(Sprite sprite)
        {
            var t = sprite.texture;
            float pad;
            if (_feetPad.TryGetValue(t, out pad)) return pad;
            pad = 0f;
            try
            {
                var r = sprite.textureRect;
                var px = t.GetPixels32();
                int w = t.width, x0 = Mathf.RoundToInt(r.x), y0 = Mathf.RoundToInt(r.y), x1 = Mathf.RoundToInt(r.x + r.width), y1 = Mathf.RoundToInt(r.y + r.height);
                for (int y = y0; y < y1 && pad == 0f; y++)
                    for (int x = x0; x < x1; x++)
                        if (px[y * w + x].a > 40) { pad = (y - y0) / Mathf.Max(1f, r.height); break; }
                if (pad >= 0.5f) pad = 0f;   // 半分以上が透明なら絵ではなく別物 (安全弁)
            }
            catch (System.Exception) { pad = 0f; }
            _feetPad[t] = pad;
            return pad;
        }

        /// <summary>コマ送りの再生 (attack/hurt/block は1回、終わると待機へ)。コマが無ければ何もしない</summary>
        public static void PlayAnim(string key, string anim)
        {
            StageUnit u;
            bool ok = _bound.TryGetValue(key, out u) && u != null;
            Debug.Log("[Stage] PlayAnim " + key + " " + anim + " t=" + Time.realtimeSinceStartup.ToString("F2") + " dt=" + Time.deltaTime.ToString("F3") + " bound=" + ok + (ok ? " has=" + u.Anims.ContainsKey(anim) : ""));
            if (ok) u.Play(anim);
        }

        /// <summary>デバッグ: いま表示中のコマ</summary>
        public static string DebugAnim(string key)
        {
            StageUnit u;
            if (!_bound.TryGetValue(key, out u) || u == null) return "(unbound)";
            return u.Anim + "#" + u.Frame + " t=" + Time.realtimeSinceStartup.ToString("F2") + " tex=" + (u.Mat != null && u.Mat.mainTexture != null ? u.Mat.mainTexture.name : "-");
        }

        /// <summary>被弾の白い点滅</summary>
        public static void Flash(string key, float dur = 0.18f)
        {
            StageUnit u;
            if (_bound.TryGetValue(key, out u) && u != null) u.FlashT = dur;
        }

        /// <summary>撃破の崩れ (2026-09-17 ユーザー「倒した敵は消えるようにしたほうが良くない？」): 0→1 でドットが頭から消えていき、接地影も薄くなる</summary>
        public static void Dissolve(string key, float k)
        {
            StageUnit u;
            if (_bound.TryGetValue(key, out u) && u != null) u.DissolveK = Mathf.Clamp01(k);
        }

        class StageUnit : MonoBehaviour
        {
            public RectTransform Rect; public Image Img; public Material Mat; public MeshRenderer Rend; public float FlashT; public float DissolveK; public float Depth; public Transform Shadow;
            public Transform Halo; public Vector2 HaloUv; public float FeetPad;
            public float HeroLight = HeroAmbient;   // リーダーの板の主役の照明 (絵ごと。HeroAmbientFor)。敵と人形では使わない
            // コマ送り (2026-09-09 このはの戦闘アニメ): 待機はループ、攻撃/被弾/防御は1回流して待機へ戻る。ドットは拡大・回転せず絵を差し替えるだけ
            public Texture2D BaseTex;
            public Dictionary<string, List<Texture2D>> Anims = new Dictionary<string, List<Texture2D>>();
            public Dictionary<string, float[]> FrameDur = new Dictionary<string, float[]>();   // コマごとの秒 (緩急)。無ければ fps で均等
            public string Anim = "idle"; public int Frame; public float FrameT; public float Fps = 8f; public bool Breathe = true; public float BreathePhase; public string Key;
            static readonly Vector3[] _c = new Vector3[4];
            public void Play(string anim)
            {
                if (!Anims.ContainsKey(anim) || Anims[anim].Count == 0) return;
                Anim = anim; Frame = 0; FrameT = 0f;
                if (Key != null) _animStates[Key] = new AnimState { Anim = Anim, Frame = Frame, FrameT = FrameT, At = Time.time };
                Apply();
            }
            public float FrameScaleX = 1f, FrameScaleY = 1f;   // 枠が元絵より大きいコマ (攻撃の 96px) の拡大率
            public void Apply()
            {
                List<Texture2D> frames;
                Texture2D tex = Anims.TryGetValue(Anim, out frames) && frames.Count > 0 ? frames[Mathf.Clamp(Frame, 0, frames.Count - 1)] : BaseTex;
                if (tex == null || Mat == null) return;
                Mat.SetTexture("_BaseMap", tex); Mat.mainTexture = tex;
                if (BaseTex != null && tex != BaseTex)
                {
                    FrameScaleX = tex.width / (float)BaseTex.width; FrameScaleY = tex.height / (float)BaseTex.height;
                    Mat.SetTextureScale("_BaseMap", Vector2.one); Mat.SetTextureOffset("_BaseMap", Vector2.zero);
                }
                else
                {
                    FrameScaleX = FrameScaleY = 1f;
                    Mat.SetTextureScale("_BaseMap", BaseUvScale); Mat.SetTextureOffset("_BaseMap", BaseUvOffset);
                }
            }
            public Vector2 BaseUvScale = Vector2.one, BaseUvOffset = Vector2.zero;
            void Advance()
            {
                List<Texture2D> frames;
                if (Key != null) _animStates[Key] = new AnimState { Anim = Anim, Frame = Frame, FrameT = FrameT, At = Time.time };
                if (!Anims.TryGetValue(Anim, out frames) || frames.Count == 0) return;
                float fps = Anim == "idle" ? 4f : Fps;
                float dur = 1f / fps;
                float[] durs;
                if (FrameDur.TryGetValue(Anim, out durs) && Frame < durs.Length) dur = durs[Frame];
                FrameT += Mathf.Min(Time.deltaTime, 0.04f);   // 長いフレーム (Rebuild の直後・端末のもたつき) が1回でコマを何枚も飛ばさないよう上限 (2026-09-16: 攻撃4コマが1フレームで消えていた)
                if (FrameT < dur) return;
                FrameT -= dur;
                Frame++;
                if (Frame >= frames.Count)
                {
                    if (Anim == "idle") Frame = 0;
                    else { Anim = "idle"; Frame = 0; }
                }
                Apply();
            }
            void OnDestroy() { if (Shadow != null) Destroy(Shadow.gameObject); if (Halo != null) Destroy(Halo.gameObject); }
            public void LateUpdate()
            {
                if (Rect == null) { Destroy(gameObject); return; }
                Advance();
                Rect.GetWorldCorners(_c);
                float sx = Mathf.Round((_c[0].x + _c[3].x) * 0.5f), sy = Mathf.Round(_c[0].y);
                float w = Mathf.Round(_c[3].x - _c[0].x), h = Mathf.Round(_c[1].y - _c[0].y);
                float k = _k * Depth / _dist;
                var pos = ScreenToPlane(sx, sy, Depth);
                var ground = pos;
                pos -= _up * (FeetPad * h * k);   // 絵の余白ぶん下げる = 足が地面の点に着く (影は地面の点のまま)
                if (Anim == "idle" && Breathe) pos += _up * (Mathf.Sin(Time.time * (2.4f + BreathePhase * 0.08f) + BreathePhase) * (Key == "player" ? 2f : 3f) * k);   // 呼吸: ±2〜3px の上下 (拡大・回転はしない)。周期も個体ごとに少しずらす (⑩ 2026-09-17)
                transform.position = pos;
                transform.rotation = CameraRotation;
                transform.localScale = new Vector3(Mathf.Max(0.01f, w * k * FrameScaleX), Mathf.Max(0.01f, h * k * FrameScaleY), 1f);   // 広い枠のコマは同じドット密度で板を広げる (足元中央は固定)
                var tint = Img != null ? Img.color : Color.white;
                Mat.SetColor("_BaseColor", tint);
                // この経路を通るのは BindUnit で置いたキャラの板だけ (player・enemyN・人形)。リーダー = 主役の照明、それ以外 = キャラの環境光 (2026-09-29 I24)
                bool hero = Key == "player";
                bool doll = !hero && Key != null && Key.StartsWith("doll:", StringComparison.Ordinal);   // 人形 (BattleScreen.FillDollPanel の key) = 敵と主役の間の環境光 (F05)
                // 敵と人形は絵の真ん中の画面の位置で周辺減光を打ち消す (F06: 同じ噛みつく巻物が ①→④ で明るさ半分・青く曇った)
                Color lift = hero || Screen.width <= 0 || Screen.height <= 0 ? Color.white : VignetteLift(sx / Screen.width, (sy + h * 0.5f * (1f - FeetPad)) / Screen.height);
                ApplyLight(Mat, UnitSunAmount, hero, !hero, lift, doll, HeroLight);
                Mat.SetFloat("_Rim", hero ? UnitRim : CharRim);
                if (FlashT > 0f) FlashT -= Time.deltaTime;
                Mat.SetFloat("_Flash", Mathf.Clamp01(FlashT / 0.18f) * 0.85f);
                Mat.SetFloat("_Dissolve", DissolveK);
                if (Shadow != null)
                {
                    float ww = w * k;
                    PlaceBlob(Shadow, new Vector3(ground.x, 0f, ground.z), ww, tint.a * (1f - DissolveK));
                }
                if (Halo != null)
                {
                    float ww = w * k, hh = h * k;
                    Halo.position = pos + _right * ((HaloUv.x - 0.5f) * ww) + _up * (HaloUv.y * hh) - _fwd * 0.05f;
                    Halo.rotation = CameraRotation;
                    float sz = 0.9f * (1f + 0.06f * Mathf.Sin(Time.time * 5f));
                    Halo.localScale = new Vector3(sz, sz, 1f);
                }
            }
        }

        // ---------------------------------------------------------------- 光と影の共通部品

        /// <summary>ドット絵の板の材質 (アンリット + 環境光 + 街灯 + 月光の勾配 + 接地影は別)</summary>
        static Material SpriteMat(Texture2D tex, float cutoff, float sunAmount)
        {
            var m = new Material(_unitShader);
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex;
            m.SetFloat("_Cutoff", cutoff);
            m.SetFloat("_Fog", 1f);
            ApplyLight(m, sunAmount);
            return m;
        }

        /// <summary>周辺減光の打ち消し (2026-09-30 F06): 敵と人形の板の環境光に掛ける係数。URP の Vignette と同じ式 (d=|uv−center|×強さ×3、f=(1−d·d)^(滑らかさ×5)、
        /// 出力＝入力×lerp(色,1,f)) を画面の座標 (0〜1) で求め、手前の席 (f≈0.93) を 1 として奥の席ほど持ち上げる。チャンネルごと＝青の曇りも戻す。
        /// 上限 1.35 (これ以上はブルームの足切りを奥の席だけ越え、④だけに光のにじみが出る)。額縁の暗さ (リーダー・小物・木・空) はそのまま</summary>
        static Color VignetteLift(float ux, float uy)
        {
            if (_vig == null || !_vig.active) return Color.white;
            float sc = _vig.intensity.value * 3f;
            var c = _vig.center.value;
            float dx = Mathf.Abs(ux - c.x) * sc, dy = Mathf.Abs(uy - c.y) * sc;
            float f = Mathf.Pow(Mathf.Clamp01(1f - (dx * dx + dy * dy)), _vig.smoothness.value * 5f);
            const float fRef = 0.93f;   // 手前の敵の席の減光 (pc06 ①・pc01 の狼の胴で約 0.93〜0.95)
            var vc = _vig.color.value;
            Func<float, float> ch = col =>
            {
                float v = Mathf.Lerp(col, 1f, f), vr = Mathf.Lerp(col, 1f, fRef);
                return Mathf.Clamp(vr / Mathf.Max(0.5f, v), 1f, 1.35f);
            };
            return new Color(ch(vc.r), ch(vc.g), ch(vc.b), 1f);
        }

        static void ApplyLight(Material m, float sunAmount, bool hero = false, bool character = false) { ApplyLight(m, sunAmount, hero, character, Color.white); }

        static void ApplyLight(Material m, float sunAmount, bool hero, bool character, Color lift, bool doll = false, float heroLight = HeroAmbient)
        {
            bool painted = _pal.LampOnUnits > 0f;
            // hero = リーダーの板: 夜の環境光 (0.8〜0.96) を掛けず源の色で立つ = 主役の照明 (2026-09-16 ユーザー裁定。2026-09-30 F05 で 1.15→1.35、このはだけ 1.5 = heroLight)
            // character = 敵と人形の板: ほぼ白で少しだけ冷たい CharAmbient (2026-09-29 ユーザー裁定「敵の環境光を白寄りに」)。
            //   旧は小物と同じ UnitAmbient (幕1 0.58/0.65/0.94 = 線形で明るさ 0.4 倍の青いフィルタ) で、クリーム・砂の絵が青灰になり地面に溶けていた。
            //   doll = 人形は UnitAmbient と CharAmbient の間 (F05)。小物・木の板 (SpriteMat の既定) は UnitAmbient のまま = 森の暗さと夜の青は変えない
            Color amb = hero ? new Color(heroLight, heroLight, heroLight)   // hero は ACES と色補正が中間調を沈めるぶん戻し、白寄りにした敵 (I24) に対して主役を持ち上げる (F05)
                : painted ? (character ? (doll ? Color.Lerp(_pal.UnitAmbient, _pal.CharAmbient, DollAmbientMix) : _pal.CharAmbient) * lift : _pal.UnitAmbient)
                : Color.white;
            amb.a = 1f;
            m.SetColor("_Ambient", amb);
            m.SetVector("_LampPos", _lampPos);
            // リーダーのランタンの暖色は環境光と同じ倍率 (HeroLampRef の注記)。プロジェクトはガンマ色空間なので SetColor の値がそのままシェーダに届く
            Color lamp = painted ? (hero ? _pal.Lantern * (heroLight / HeroLampRef) : _pal.Lantern) : Color.black;
            lamp.a = 1f;
            m.SetColor("_LampColor", lamp);
            m.SetFloat("_LampStrength", painted ? _pal.LampOnUnits : 0f);
            m.SetFloat("_LampFalloff", 7f);
            m.SetVector("_SunDir2", new Vector4(0.7f, 0.7f, 0f, 0f));   // 右上が光源側
            m.SetFloat("_SunAmount", painted ? sunAmount : 0f);
        }

        static Texture2D BlobTex()
        {
            if (_blobTex != null) return _blobTex;
            _blobTex = Px.Glow(new Color(1f, 1f, 1f, 1f));
            return _blobTex;
        }

        /// <summary>接地影の楕円を作る (地面に寝かせた半透明の板)</summary>
        static GameObject Blob(string name, Transform parent, Vector3 basePos, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = _quadCentered;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = GlowMaterial(BlobTex());
            mr.sharedMaterial.color = ShadowColor;
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            PlaceBlob(go.transform, basePos, width, 1f);
            return go;
        }

        /// <summary>接地影: 横幅 0.8 倍・奥行き 0.4 倍。Euler(90,0,0) の板は原点から −z (手前) へ伸びるので、中心が足元に来るよう +d/2 に置く。
        /// 光源 (右上・手前) の反対 = 左奥へ少し寄せる</summary>
        static void PlaceBlob(Transform blob, Vector3 basePos, float width, float alpha)
        {
            float w = width * 0.8f, d = width * 0.35f;
            blob.position = new Vector3(basePos.x, basePos.y + 0.03f, basePos.z);   // 中心原点の板 = そのまま足元
            blob.rotation = Quaternion.Euler(90f, 0f, 0f);
            blob.localScale = new Vector3(w, d, 1f);
            var mr = blob.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial.color = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, ShadowColor.a * alpha);
        }
    }
}
