// StageFx.cs — 舞台の技の光 (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3・§2-4)。
// 骨組み (P00) の口に、P09 (W1) が中身を書いた: 点光源のプール (設計図 look_act1.json の hitLight.maxLights 個・既定3。StageLook.Apply が用意する)。
// 演出からの呼び出しは P13 (W2) が書いた: Presenter の2Dの当たりと同じ所で、下の「演出からの口」(PlayerHit・FoeHit・Guard・Cast・Finish) を呼ぶ。
// 色は当たりの形ごと (斬撃＝暖かい白・呪文＝脈の青緑・灯＝暖色・火種＝橙・敵の当たり＝朱・防いだ＝空色・とどめ＝白)。大きい当たりは強さ×1.6 で影あり (PC だけ)。
// 同時に灯るのは3つまで (空きが無ければいちばん古い光を使い回す)。
// stage=old・StageLook が光を当てていない時は何もしない (口は頭の Live で返る。乱数も Tween も使わない = old の画を1画素も変えない)。
// 影は大きい当たり (shadow=true) だけ。影を落とす光の数の上限 (PC 3・スマホ 2) は StageLook と貸し借りする (上限に届いていれば逆光の影を一時的に止めて借りる)。
// 光の減衰は Time.deltaTime (det の撮影では 1/60 秒ずつ・ヒットストップの間はゆっくり)。
// 口の強さ・長さ・色は設計図 look_act1.json の hitLight に任意のキーで書ける (無ければコードの既定。StageLookData.Raw から読む。下の Tune)。
using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DeckRogue.Game
{
    public static class StageFx
    {
        /// <summary>
        /// 当たりの光: world に色 color の点光源を灯し、dur 秒で消す (灯した瞬間がいちばん明るく、(1 − 経過÷dur)² で弱まる)。
        /// intensity = 設計図の hitLight.refDist (既定 1.5 unit) の距離での明るさ (Light.intensity = intensity × refDist²。URP の点光源は距離の2乗で弱まるため)。
        /// 届く距離は hitLight.range (既定 4)。同時に hitLight.maxLights (既定 3) まで。空きが無ければ、いちばん古い光を使い回す (P13: 計画の「古い物から消す」)。
        /// shadow = true (大きい当たり) の時だけ影を落とす。上限を超えるなら StageLook が逆光の影を一時的に止めて貸し、それもできなければ影なしで灯す
        /// </summary>
        public static void HitLight(Vector3 world, Color color, float intensity, float dur, bool shadow)
        {
            LightAt(world, color, intensity, dur, 0f, shadow, "direct", null);
        }

        /// <summary>HitLight の中身。hold = 灯してから dur のこの割合までは弱めない (その後 (残り÷(1−hold))² で消える)。kind・key は記録 (dumplayout・ログ) 用</summary>
        static void LightAt(Vector3 world, Color color, float intensity, float dur, float hold, bool shadow, string kind, string key)
        {
            if (!Live) return;
            if (!(intensity > 0f) || !(dur > 0f) || float.IsInfinity(intensity) || float.IsInfinity(dur)) return;
            if (float.IsNaN(world.x) || float.IsNaN(world.y) || float.IsNaN(world.z)) return;
            var h = StageLook.Current != null ? StageLook.Current.Hit : Defaults;
            Prepare(h);
            var s = Pick();
            if (s == null) return;
            bool evicted = s.Lit;
            if (evicted) _evicted++;
            Release(s);   // 使い回す光が借りていた影を先に返す
            var l = s.Light;
            l.transform.position = world;
            l.color = new Color(color.r, color.g, color.b, 1f);
            l.range = Mathf.Max(0.1f, h.Range);
            s.Peak = intensity * h.RefDist * h.RefDist;
            s.T = 0f;
            s.Dur = dur;
            s.Hold = Mathf.Clamp(hold, 0f, 0.9f);
            s.Seq = ++_seq;
            s.Kind = kind;
            s.Key = key;
            l.intensity = s.Peak;
            l.shadows = LightShadows.None;
            if (shadow && StageLook.TryBorrowShadowSlot()) { s.Borrowed = true; l.shadows = LightShadows.Hard; }
            l.enabled = true;
            s.Lit = true;
            _calls++;
            int lit = LitCount;
            if (lit > _maxLit) _maxLit = lit;
            Remember(s, world, intensity, evicted);
            if (HD2DFlags.Det || HD2DFlags.DumpLayout)
                Debug.Log("[StageFx] 技の光 " + kind + (key != null ? " " + key : "") + " 強さ " + F(intensity) + " " + F(dur) + "秒" + (s.Borrowed ? " 影" : "")
                    + " 灯っている " + lit + "/" + Math.Min(_want, _slots.Count) + (evicted ? " (いちばん古い光を使い回した)" : "") + " frame " + Time.frameCount);
        }

        // ================================================================ 演出からの口 (P13・W2。Presenter が2Dの当たりと同じ所で呼ぶ)

        /// <summary>技の光を灯せる時 (stage=diorama で StageLook が光を当てている)。false なら下の口は全部なにもしない (old の画を変えない)</summary>
        public static bool Live => HD2DFlags.DioramaHere && StageLook.Active;

        /// <summary>
        /// 自分の札の当たり (Presenter の PlayerHitFx と同じ所)。key = 当たった敵の板 ("enemy0"…)。
        /// style = 当たりの形 (Presenter.CardHitStyle: slash・fang・horn・vine・stomp・spell・light・spark)。色は形ごと (物理＝暖かい白・spell＝脈の青緑・light＝暖色・spark＝橙)。
        /// big = 大きい当たり (2Dと同じ: 15以上か急所) → 強さ×bigMul (既定 1.6)・影 (PC だけ)。蔦は鞭の先が届く vineDelay (既定 0.13 秒) 後に灯す
        /// </summary>
        public static void PlayerHit(string key, string style, bool big)
        {
            if (!Live) return;
            var t = Tune;
            Color c = t.ColorFor(style);
            float k = big ? t.BigMul : 1f;
            float delay = style == "vine" ? t.VineDelay : 0f;
            At(key, new Vector2(0.5f, t.Height), c, t.Intensity * k, t.Dur, t.Hold, big && ShadowOk, "hit:" + (style ?? "slash") + (big ? "+" : ""), delay);
        }

        /// <summary>
        /// 敵→自分の当たり (Presenter の HitFx と同じ所。とげの反射も)。key = 自分の板 ("player")。tint = 2Dの飛び道具・光線の色 (null = 朱)。
        /// big = 2Dの大きい当たり (HP を12以上失った) → 強さ×bigMul・影 (PC だけ)
        /// </summary>
        public static void FoeHit(string key, Color? tint, bool big)
        {
            if (!Live) return;
            var t = Tune;
            Color c = tint.HasValue ? tint.Value : t.Foe;
            float k = (big ? t.BigMul : 1f) * t.FoeMul;
            At(key, new Vector2(0.5f, t.Height), c, t.Intensity * k, t.Dur, t.Hold, big && ShadowOk, "foe" + (big ? "+" : ""), 0f);
        }

        /// <summary>
        /// 完全に防いだ (Presenter の GuardFx と同じ所)。key = 盾で受けた側の板。side = 盾の面を構える向き (攻撃が来る側。−1 = 左・+1 = 右)。
        /// 空色・guardDur (既定 0.18 秒)・影なし
        /// </summary>
        public static void Guard(string key, float side)
        {
            if (!Live) return;
            var t = Tune;
            float x = 0.5f + Mathf.Clamp(side, -1f, 1f) * 0.3f;
            At(key, new Vector2(x, t.Height), t.GuardColor, t.Intensity * t.GuardMul, t.GuardDur, t.Hold, false, "guard", 0f);
        }

        /// <summary>
        /// 呪文の構え (Presenter の CastFx と同じ所)。key = 自分の板。体の前 (敵の側) で、色は形 (spell＝脈の青緑・light＝暖色)。castDur (既定 0.3 秒)・影なし
        /// </summary>
        public static void Cast(string key, string style)
        {
            if (!Live) return;
            var t = Tune;
            At(key, new Vector2(0.78f, 0.5f), t.ColorFor(style), t.Intensity * t.CastMul, t.CastDur, t.Hold, false, "cast:" + (style ?? "spell"), 0f);
        }

        /// <summary>
        /// とどめ (戦闘を決めた一撃。Presenter の HitStop と同じ所)。key = 倒れる敵の板。白・finishDur (既定 0.3 秒)・強さ×finishMul (既定 2)・影 (PC だけ)。
        /// 同じ一撃の PlayerHit の代わりに呼ぶ (2つ灯さない)。ヒットストップの間は時間がゆっくりなので長く残る
        /// </summary>
        public static void Finish(string key)
        {
            if (!Live) return;
            var t = Tune;
            At(key, new Vector2(0.5f, t.Height), t.FinishColor, t.Intensity * t.FinishMul, t.FinishDur, t.Hold, ShadowOk, "finish", 0f);
        }

        /// <summary>影を落としてよい段 (スマホの段では技の光に影を付けない。計画 P13「影あり (PC だけ)」)</summary>
        static bool ShadowOk => HD2DFlags.Tier != HD2DTier.Phone;

        /// <summary>板 key の点 v に灯す (delay 秒後。場所は灯す時の板の位置で決める = 踏み込みの途中でも当たった所に灯る)</summary>
        static void At(string key, Vector2 v, Color color, float intensity, float dur, float hold, bool shadow, string kind, float delay)
        {
            if (delay > 0f)
            {
                _pending.Add(new Pending { Left = delay, Key = key, V = v, Color = color, Intensity = intensity, Dur = dur, Hold = hold, Shadow = shadow, Kind = kind });
                EnsureDriver();
                return;
            }
            Vector3? p = Where(key, v);
            if (!p.HasValue) { _missed++; return; }
            LightAt(p.Value, color, intensity, dur, hold, shadow, kind, key);
        }

        /// <summary>
        /// 光を置く世界の点: 板の上の点 (UnitPoint) を、カメラの方へ towardCamera (既定 0.6 unit) 寄せる
        /// (板の面の上に置くと、その板の影が潰れて面の手前の地面も照らしにくい。本家のブーストの灯と同じく「体の前」で光る)。
        /// 板が無ければ座席 (Stage.TryGetSeat) から、既定の高さ fallbackHeight の板とみなして出す。どちらも無ければ null (灯さない)
        /// </summary>
        static Vector3? Where(string key, Vector2 v)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var t = Tune;
            Vector3? p = UnitPoint(key, v);
            if (!p.HasValue)
            {
                Vector3 seat; float k;
                if (!Stage.TryGetSeat(key, out seat, out k)) return null;
                var rot = Stage.LayoutRotation;
                p = seat + (rot * Vector3.up) * (v.y * t.FallbackHeight) + (rot * Vector3.right) * ((v.x - 0.5f) * t.FallbackHeight);
            }
            var q = p.Value;
            if (t.TowardCamera > 0f)
            {
                var cam = Stage.Camera;
                Vector3 to = cam != null ? cam.transform.position - q : -(Stage.LayoutRotation * Vector3.forward);
                if (to.sqrMagnitude > 1e-6f) q += to.normalized * t.TowardCamera;
            }
            return q;
        }

        /// <summary>
        /// キャラの板 (key) の上の点を世界で返す。v は板の中の位置 (x 0=左・1=右、y 0=足元・1=頭)。
        /// Stage.TryGetUnitBox (足元の中心と板の高さ) を読む。板の幅は高さと同じとみなす (幅が要る時は aspect つきの方を使う)。
        /// 板はレイアウト用のカメラの回転 (Stage.LayoutRotation) を向く。板が無ければ null
        /// </summary>
        public static Vector3? UnitPoint(string key, Vector2 v) => UnitPoint(key, v, 1f);

        /// <summary>UnitPoint の幅つき: 板の幅 = 高さ × aspect (絵の幅 ÷ 高さ)</summary>
        public static Vector3? UnitPoint(string key, Vector2 v, float aspect)
        {
            Vector3 feet; float h;
            if (string.IsNullOrEmpty(key) || !Stage.TryGetUnitBox(key, out feet, out h) || !(h > 0f)) return null;
            var rot = Stage.LayoutRotation;
            Vector3 up = rot * Vector3.up, right = rot * Vector3.right;
            return feet + up * (v.y * h) + right * ((v.x - 0.5f) * h * Mathf.Max(0.01f, aspect));
        }

        /// <summary>いま灯っている技の光の数</summary>
        public static int LitCount
        {
            get
            {
                int n = 0;
                foreach (var s in _slots) if (s.Lit && s.Light != null) n++;
                return n;
            }
        }

        // ---------------------------------------------------------------- 中身

        sealed class Slot
        {
            public Light Light;
            public float Peak, T, Dur, Hold;
            public bool Lit, Borrowed;
            public long Seq;          // 灯した順 (いちばん古い光を使い回すため)
            public string Kind, Key;  // 記録用 (dumplayout の extra.hitLight・ログ)
        }

        /// <summary>遅れて灯す光 (蔦の鞭の先が届くまで)。灯す時に板の位置を読む</summary>
        sealed class Pending
        {
            public float Left, Intensity, Dur, Hold;
            public string Key, Kind;
            public Vector2 V;
            public Color Color;
            public bool Shadow;
        }

        static readonly List<Slot> _slots = new List<Slot>();
        static readonly List<Pending> _pending = new List<Pending>();
        static readonly StageLookData.HitLook Defaults = new StageLookData.HitLook();
        static GameObject _root;
        static int _want = 3;
        // 記録 (Apply ごとに 0 へ戻す): 灯した回数・同時に灯った最大・使い回した回数・板も座席も無くて灯せなかった回数
        static long _seq;
        static int _calls, _maxLit, _evicted, _missed;
        static readonly List<Dictionary<string, object>> _recent = new List<Dictionary<string, object>>();
        const int RecentMax = 16;

        /// <summary>
        /// 口の強さ・長さ・色。設計図 look_act1.json の hitLight に任意のキー (intensity・bigMul・dur・hold・guardDur・guardMul・castDur・castMul・
        /// foeMul・finishDur・finishMul・height・towardCamera・vineDelay・fallbackHeight・colors{slash,fang,horn,vine,stomp,spell,light,spark,foe,guard,finish}=[r,g,b])
        /// を書けば上書きする。無ければ下の既定 (計画 P13 の値)。設計図が変わった時 (Apply) だけ読み直す
        /// </summary>
        sealed class HitTune
        {
            // Dur 0.26 = 0.3秒以内に消える (計画 P13 の確かめ方「0.3秒で戻る」)。Hold 0.3 = 最初の約4.7フレームは灯した明るさのまま (「2〜6フレーム後に +20」)
            public float Intensity = 1f, BigMul = 1.6f, Dur = 0.26f, Hold = 0.3f;
            public float GuardDur = 0.18f, GuardMul = 0.7f, CastDur = 0.3f, CastMul = 0.8f, FoeMul = 1f;
            public float FinishDur = 0.3f, FinishMul = 2f;
            public float Height = 0.45f, TowardCamera = 0.6f, VineDelay = 0.13f, FallbackHeight = 2.4f;
            // 色 (光の色なので明るさ1に寄せた値。UI の役割の色ではない = check:colors の対象外の Stage*.cs に置く)
            public Color Physical = new Color(1f, 0.93f, 0.8f);    // 斬撃・牙・角・蔦・踏みつけ = 暖かい白
            public Color Spell = new Color(0.4f, 1f, 0.9f);        // 呪文 = 脈の青緑
            public Color Lamp = new Color(1f, 0.8f, 0.5f);         // 灯 = 暖色
            public Color Spark = new Color(1f, 0.55f, 0.22f);      // 火種 = 橙
            public Color Foe = new Color(1f, 0.42f, 0.3f);         // 敵の当たり = 朱
            public Color GuardColor = new Color(0.6f, 0.8f, 1f);   // 完全に防いだ = 空色
            public Color FinishColor = new Color(1f, 1f, 1f);      // とどめ = 白
            public readonly Dictionary<string, Color> Styles = new Dictionary<string, Color>();

            public Color ColorFor(string style)
            {
                Color c;
                if (!string.IsNullOrEmpty(style) && Styles.TryGetValue(style, out c)) return c;
                switch (style)
                {
                    case "spell": return Spell;
                    case "light": return Lamp;
                    case "spark": return Spark;
                    default: return Physical;
                }
            }
        }

        static HitTune _tune;
        static StageLookData _tuneOf;

        static HitTune Tune
        {
            get
            {
                var cur = StageLook.Current;
                if (_tune != null && ReferenceEquals(_tuneOf, cur)) return _tune;
                _tuneOf = cur;
                _tune = ReadTune(cur);
                return _tune;
            }
        }

        static HitTune ReadTune(StageLookData d)
        {
            var t = new HitTune();
            JObject o = null;
            try { o = d != null && d.Raw != null ? d.Raw["hitLight"] as JObject : null; } catch (Exception) { o = null; }
            if (o == null) return t;
            try
            {
                t.Intensity = Num(o, "intensity", t.Intensity);
                t.BigMul = Num(o, "bigMul", t.BigMul);
                t.Dur = Num(o, "dur", t.Dur);
                t.Hold = Num(o, "hold", t.Hold);
                t.GuardDur = Num(o, "guardDur", t.GuardDur);
                t.GuardMul = Num(o, "guardMul", t.GuardMul);
                t.CastDur = Num(o, "castDur", t.CastDur);
                t.CastMul = Num(o, "castMul", t.CastMul);
                t.FoeMul = Num(o, "foeMul", t.FoeMul);
                t.FinishDur = Num(o, "finishDur", t.FinishDur);
                t.FinishMul = Num(o, "finishMul", t.FinishMul);
                t.Height = Num(o, "height", t.Height);
                t.TowardCamera = Num(o, "towardCamera", t.TowardCamera);
                t.VineDelay = Num(o, "vineDelay", t.VineDelay);
                t.FallbackHeight = Num(o, "fallbackHeight", t.FallbackHeight);
                var cs = o["colors"] as JObject;
                if (cs != null)
                    foreach (var p in cs.Properties())
                    {
                        Color c;
                        if (!Col(p.Value, out c)) continue;
                        switch (p.Name)
                        {
                            case "foe": t.Foe = c; break;
                            case "guard": t.GuardColor = c; break;
                            case "finish": t.FinishColor = c; break;
                            default: t.Styles[p.Name] = c; break;
                        }
                    }
            }
            catch (Exception e) { Debug.LogWarning("[StageFx] 設計図の hitLight を読めない (既定で灯す): " + e.Message); }
            return t;
        }

        static float Num(JObject o, string name, float def)
        {
            var v = o[name];
            if (v == null || (v.Type != JTokenType.Float && v.Type != JTokenType.Integer)) return def;
            float f = v.Value<float>();
            return float.IsNaN(f) || float.IsInfinity(f) || f < 0f ? def : f;
        }

        static bool Col(JToken v, out Color c)
        {
            c = default;
            var a = v as JArray;
            if (a == null || a.Count < 3) return false;
            for (int i = 0; i < 3; i++) if (a[i].Type != JTokenType.Float && a[i].Type != JTokenType.Integer) return false;
            c = new Color(a[0].Value<float>(), a[1].Value<float>(), a[2].Value<float>(), 1f);
            return true;
        }

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>灯した記録を残す (新しい順に RecentMax 件)</summary>
        static void Remember(Slot s, Vector3 world, float intensity, bool evicted)
        {
            var r = new Dictionary<string, object>
            {
                { "frame", Time.frameCount }, { "time", Time.time }, { "kind", s.Kind }, { "key", s.Key }, { "pos", world },
                { "color", s.Light != null ? s.Light.color : Color.white }, { "intensity", intensity }, { "dur", s.Dur }, { "hold", s.Hold },
                { "shadow", s.Borrowed }, { "evicted", evicted }, { "litAfter", LitCount },
            };
            _recent.Insert(0, r);
            if (_recent.Count > RecentMax) _recent.RemoveRange(RecentMax, _recent.Count - RecentMax);
        }

        /// <summary>
        /// dumplayout (layout.json の extra.hitLight) の記録: 今灯っている光 (種類・板・位置・明るさ・経過)・同時に灯った最大 (≦3 を確かめる)・
        /// 灯した回数・使い回した回数・灯せなかった回数・遅れて灯す待ち・最近の記録。Prepare が登録し、StopAll (Apply の頭と Restore) が外す
        /// </summary>
        public static object DebugInfo()
        {
            var o = new Dictionary<string, object>();
            o["live"] = Live;
            o["cap"] = Math.Min(_want, _slots.Count);
            o["lit"] = LitCount;
            o["maxLit"] = _maxLit;
            o["calls"] = _calls;
            o["evicted"] = _evicted;
            o["missed"] = _missed;
            o["pending"] = _pending.Count;
            var slots = new List<object>();
            foreach (var s in _slots)
            {
                if (s.Light == null) continue;
                slots.Add(new Dictionary<string, object>
                {
                    { "lit", s.Lit }, { "kind", s.Kind }, { "key", s.Key }, { "pos", s.Light.transform.position }, { "color", s.Light.color },
                    { "intensity", s.Light.intensity }, { "peak", s.Peak }, { "t", s.T }, { "dur", s.Dur }, { "shadow", s.Borrowed },
                });
            }
            o["slots"] = slots;
            o["recent"] = new List<Dictionary<string, object>>(_recent);
            return o;
        }

        static void EnsureDriver()
        {
            if (_root == null) Prepare(StageLook.Current != null ? StageLook.Current.Hit : Defaults);
        }

        /// <summary>プールを用意する (無ければ作る・足りなければ足す)。光は消えたまま。StageLook.Apply が呼ぶ</summary>
        internal static void Prepare(StageLookData.HitLook h)
        {
            if (h == null) h = Defaults;
            _want = Mathf.Clamp(h.MaxLights, 1, 8);
            if (_root == null)   // 初めて・場面ごと消えた (Unity の null)
            {
                _slots.Clear();
                _root = new GameObject("HD2D-HitLights");
                _root.layer = HD2DLayers.StageFx;
                _root.AddComponent<HitLightDriver>();
            }
            while (_slots.Count < _want)
            {
                var go = new GameObject("HD2D-HitLight" + _slots.Count);
                go.transform.SetParent(_root.transform, false);
                go.layer = HD2DLayers.StageFx;
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.enabled = false;
                l.intensity = 0f;
                l.shadows = LightShadows.None;
                l.shadowStrength = 0.8f;
                l.shadowBias = 0.05f;
                l.shadowNormalBias = 0.4f;
                _slots.Add(new Slot { Light = l });
            }
            foreach (var s in _slots)
            {
                if (s.Light == null) continue;
                try
                {
                    var ad = s.Light.GetUniversalAdditionalLightData();
                    ad.usePipelineSettings = false;
                    ad.customShadowLayers = true;
                    ad.renderingLayers = 0xFFFFFFFFu;        // 地面もキャラも照らす
                    ad.shadowRenderingLayers = 0xFFFFFFFFu;  // 大きい当たりはキャラと大物の影を地面に落とす (本家のブーストの灯)
                    if (Application.isPlaying) ad.additionalLightsShadowResolutionTier = h.ShadowTier;
                }
                catch (Exception e) { Debug.LogWarning("[StageFx] 技の光の URP の設定に失敗: " + e.Message); }
            }
            HD2DFlags.LayoutDumpers["hitLight"] = DebugInfo;   // dumplayout の extra.hitLight (P13)
        }

        /// <summary>全部消して、借りていた影を返す (StageLook の Apply の頭と Restore が呼ぶ)。遅れて灯す待ちも捨て、記録を 0 へ戻す</summary>
        internal static void StopAll()
        {
            if (_calls > 0 || _missed > 0)
                Debug.Log("[StageFx] 技の光の記録: 灯した " + _calls + " 回・同時の最大 " + _maxLit + "・使い回し " + _evicted + " 回・灯せなかった " + _missed + " 回");
            foreach (var s in _slots)
            {
                if (s.Light != null) { s.Light.enabled = false; s.Light.intensity = 0f; s.Light.shadows = LightShadows.None; }
                s.Lit = false;
                Release(s);
            }
            _pending.Clear();
            _calls = 0; _maxLit = 0; _evicted = 0; _missed = 0;
            _recent.Clear();
            _tune = null; _tuneOf = null;
            HD2DFlags.LayoutDumpers.Remove("hitLight");
        }

        /// <summary>プールの光 (dumplayout の記録用)</summary>
        internal static IEnumerable<Light> PoolLights()
        {
            foreach (var s in _slots) if (s.Light != null) yield return s.Light;
        }

        /// <summary>空いている光。無ければ、いちばん古く灯した光 (計画 P13「同時に点く光は3つまで・古い物から消す」)</summary>
        static Slot Pick()
        {
            Slot best = null;
            long bestSeq = long.MaxValue;
            int n = Math.Min(_want, _slots.Count);
            for (int i = 0; i < n; i++)
            {
                var s = _slots[i];
                if (s.Light == null) continue;
                if (!s.Lit) return s;
                if (s.Seq < bestSeq) { bestSeq = s.Seq; best = s; }
            }
            return best;
        }

        static void Release(Slot s)
        {
            if (!s.Borrowed) return;
            s.Borrowed = false;
            if (s.Light != null) s.Light.shadows = LightShadows.None;
            StageLook.ReturnShadowSlot();
        }

        /// <summary>光を弱めて、dur を過ぎたら消す。StageLook が光を当てていなければすぐ消す。遅れて灯す待ちを進める</summary>
        static void Tick(float dt)
        {
            if (_pending.Count > 0)
            {
                if (!Live) _pending.Clear();
                else
                {
                    // 灯すと _pending が増えることは無い (delay 0 で呼ぶ) が、念のため写しで回す
                    var due = new List<Pending>();
                    for (int i = _pending.Count - 1; i >= 0; i--)
                    {
                        var p = _pending[i];
                        p.Left -= dt;
                        if (p.Left <= 0f) { due.Add(p); _pending.RemoveAt(i); }
                    }
                    for (int i = due.Count - 1; i >= 0; i--)   // 待ちに入った順に灯す
                    {
                        var p = due[i];
                        At(p.Key, p.V, p.Color, p.Intensity, p.Dur, p.Hold, p.Shadow, p.Kind, 0f);
                    }
                }
            }
            foreach (var s in _slots)
            {
                if (!s.Lit) continue;
                if (s.Light == null) { s.Lit = false; Release(s); continue; }
                s.T += dt;
                float u = s.Dur > 0f ? s.T / s.Dur : 1f;
                if (u >= 1f || !StageLook.Active)
                {
                    s.Lit = false;
                    s.Light.enabled = false;
                    s.Light.intensity = 0f;
                    Release(s);
                    continue;
                }
                // hold までは灯した明るさのまま、その後 (残り÷(1−hold))² で消える (hold 0 = P09 の (1 − 経過÷dur)²)
                float e = u <= s.Hold ? 1f : (1f - u) / Mathf.Max(1e-4f, 1f - s.Hold);
                s.Light.intensity = s.Peak * e * e;
            }
        }

        sealed class HitLightDriver : MonoBehaviour
        {
            void LateUpdate() { Tick(Time.deltaTime); }

            void OnDestroy()
            {
                if (_root != null && _root != gameObject) return;   // 別のプールの持ち主 (念のため)
                foreach (var s in _slots) { s.Lit = false; Release(s); }
                _slots.Clear();
                _pending.Clear();
            }
        }
    }
}
