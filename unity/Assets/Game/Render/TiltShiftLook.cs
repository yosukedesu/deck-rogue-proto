// TiltShiftLook.cs — ぼかしの詰めの値を幕の設計図から読む (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-4・§2-6・P24)。
//
// 読む所: StageLook.Current.Raw (look_act<N>.json → .char → .dof → (tier=phone なら) .phone を重ねた後の生の JSON) の "tiltShiftPass"。
//   StageLook が読む "tiltShift" (帯の余白・傾斜・上限・半解像度 = TiltShiftSettings の約束の値) とは別の塊 = パスの中の詰めの値 (StageLook は知らないキー)。
//   { "focus": "path"|"depth", "pathBand": [手前, 奥], "nearScale": 1, "taps": 32, "nearOwnTaps": 24, "nearDilateTile": 16, "jitter": true,
//     "farExclude": 0.5, "tiltTop": 0, "tiltTopFrom": 0.75, "tiltBottom": 0, "tiltBottomFrom": 0.25, "debugView": 0,
//     "phone": { …同じキー… } }   ← tier=phone の時だけ上に重ねる (look_act1.phone.json の "tiltShiftPass" も重なる = StageLook の重ね方)
//   書いていないキーは既定 (下の Def*。look_act1.dof.json と同じ値)。
//   2周目 (W3b) のキー: "curve": "lens"|"smooth" (帯の外の伸び方)・"lensFar"・"lensNear" (レンズの強さ)。上限 (px) は今までどおり "tiltShift" の maxPxPC・maxPxPhone (StageLook が読む)。
// 撮影の上書き (1行1起動の撮影だけ): 起動引数 -state / -hd2d の中の tsfocus=path|depth・tscurve=lens|smooth・tsdebug=0〜3 (HD2DFlags は知らないキーを無視する)。
//   -statesfile のまとめ撮りの行には効かない (行ごとの STATE をここは読めない)。
// 書く所: TiltShiftSettings (Focus・PathNear・PathFar・NearScale) と TiltShiftPass の静的な詰めの値。
// いつ: TiltShiftHook が積む直前に Sync (設計図 = StageLook.Current が替わった時と tier が替わった時だけ読み直す)。
// dumplayout: layout.json の extra.tiltShiftPass (読んだ値・実際に使ったピントの帯の形・道の帯の値)。
using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DeckRogue.Game
{
    public static class TiltShiftLook
    {
        // ---- 既定 (設計図に書いていない時。look_act1.dof.json と同じ値) ----
        const TiltShiftFocus DefFocus = TiltShiftFocus.Path;
        const float DefPathNear = -3f, DefPathFar = 5.5f, DefNearScale = 2.0f;   // 2周目: 1 → 1.2・W3b の統合: 2.0 (手前の上限 = 奥の上限 10px × 2 = 20px)
        const int DefTaps = 24, DefNearOwnTaps = 20, DefNearDilateTile = 16;     // 2周目: 32/24 → 24/16 (上限が半分以下になった)・W3b の統合: 手前の点 20
        const bool DefJitter = true;
        const float DefFarExclude = 0.5f, DefTiltTop = 0f, DefTiltTopFrom = 0.75f, DefTiltBottom = 0f, DefTiltBottomFrom = 0.25f;
        const int DefDebugView = 0;
        // P24 2周目: 帯の外の伸び方 (レンズ)。look_act1.dof.json と同じ値
        const TiltShiftCurve DefCurve = TiltShiftCurve.Lens;
        const float DefLensFar = 1.6f, DefLensNear = 1.6f;   // W3b の統合: 手前 1.4 → 1.6

        static bool _loaded;
        static StageLookData _for;
        static HD2DTier _forTier;
        static string _source = "";
        static string _stateOverrides = "";
        static bool _warnedParse;

        /// <summary>設計図が替わっていれば読み直して TiltShiftSettings と TiltShiftPass に書く (毎フレーム呼んでよい。替わっていなければ何もしない)</summary>
        public static void Sync()
        {
            var cur = StageLook.Current;
            var tier = HD2DFlags.Tier;
            if (_loaded && ReferenceEquals(cur, _for) && tier == _forTier) return;
            _loaded = true;
            _for = cur;
            _forTier = tier;
            Load(cur != null ? cur.Raw : null, tier == HD2DTier.Phone, cur != null ? cur.Name : null);
        }

        /// <summary>次の Sync で読み直させる</summary>
        public static void Invalidate() { _loaded = false; }

        static void Load(JObject raw, bool phone, string name)
        {
            // 既定へ
            TiltShiftSettings.Focus = DefFocus;
            TiltShiftSettings.PathNear = DefPathNear;
            TiltShiftSettings.PathFar = DefPathFar;
            TiltShiftSettings.NearScale = DefNearScale;
            TiltShiftSettings.Curve = DefCurve;
            TiltShiftSettings.LensFar = DefLensFar;
            TiltShiftSettings.LensNear = DefLensNear;
            TiltShiftPass.Taps = DefTaps;
            TiltShiftPass.NearOwnTaps = DefNearOwnTaps;
            TiltShiftPass.NearDilateTile = DefNearDilateTile;
            TiltShiftPass.Jitter = DefJitter;
            TiltShiftPass.FarExclude = DefFarExclude;
            TiltShiftPass.TiltTop = DefTiltTop;
            TiltShiftPass.TiltTopFrom = DefTiltTopFrom;
            TiltShiftPass.TiltBottom = DefTiltBottom;
            TiltShiftPass.TiltBottomFrom = DefTiltBottomFrom;
            TiltShiftPass.DebugView = DefDebugView;

            _source = "既定";
            try
            {
                var b = raw != null ? raw["tiltShiftPass"] as JObject : null;
                if (b != null)
                {
                    ApplyBlock(b);
                    _source = (name ?? "?") + " の tiltShiftPass";
                    if (phone && b["phone"] is JObject ph) { ApplyBlock(ph); _source += " + phone"; }
                }
            }
            catch (Exception e)
            {
                if (!_warnedParse) { _warnedParse = true; Debug.LogWarning("[TiltShift] 設計図の tiltShiftPass が読めない (既定のまま): " + e.Message); }
            }
            ApplyStateOverrides();
            Debug.Log("[TiltShift] " + Summary());
        }

        static void ApplyBlock(JObject b)
        {
            string f = Str(b, "focus");
            if (f != null)
            {
                if (f.Equals("path", StringComparison.OrdinalIgnoreCase)) TiltShiftSettings.Focus = TiltShiftFocus.Path;
                else if (f.Equals("depth", StringComparison.OrdinalIgnoreCase)) TiltShiftSettings.Focus = TiltShiftFocus.Depth;
            }
            if (b["pathBand"] is JArray band && band.Count >= 2)
            {
                float n, fr;
                if (Num(band[0], out n) && Num(band[1], out fr) && fr > n) { TiltShiftSettings.PathNear = n; TiltShiftSettings.PathFar = fr; }
            }
            TiltShiftSettings.NearScale = Mathf.Clamp(F(b, "nearScale", TiltShiftSettings.NearScale), 0f, 2f);
            string cv = Str(b, "curve");
            if (cv != null)
            {
                if (cv.Equals("lens", StringComparison.OrdinalIgnoreCase)) TiltShiftSettings.Curve = TiltShiftCurve.Lens;
                else if (cv.Equals("smooth", StringComparison.OrdinalIgnoreCase)) TiltShiftSettings.Curve = TiltShiftCurve.Smooth;
            }
            TiltShiftSettings.LensFar = Mathf.Clamp(F(b, "lensFar", TiltShiftSettings.LensFar), 0.05f, 20f);
            TiltShiftSettings.LensNear = Mathf.Clamp(F(b, "lensNear", TiltShiftSettings.LensNear), 0.05f, 20f);
            TiltShiftPass.Taps = Mathf.Clamp(I(b, "taps", TiltShiftPass.Taps), 4, 64);
            TiltShiftPass.NearOwnTaps = Mathf.Clamp(I(b, "nearOwnTaps", TiltShiftPass.NearOwnTaps), 4, 64);
            TiltShiftPass.NearDilateTile = Mathf.Clamp(I(b, "nearDilateTile", TiltShiftPass.NearDilateTile), 4, 64);
            TiltShiftPass.Jitter = Bool(b, "jitter", TiltShiftPass.Jitter);
            TiltShiftPass.FarExclude = Mathf.Clamp01(F(b, "farExclude", TiltShiftPass.FarExclude));
            TiltShiftPass.TiltTop = Mathf.Clamp01(F(b, "tiltTop", TiltShiftPass.TiltTop));
            TiltShiftPass.TiltTopFrom = Mathf.Clamp(F(b, "tiltTopFrom", TiltShiftPass.TiltTopFrom), 0f, 0.999f);
            TiltShiftPass.TiltBottom = Mathf.Clamp01(F(b, "tiltBottom", TiltShiftPass.TiltBottom));
            TiltShiftPass.TiltBottomFrom = Mathf.Clamp(F(b, "tiltBottomFrom", TiltShiftPass.TiltBottomFrom), 0.001f, 1f);
            TiltShiftPass.DebugView = Mathf.Clamp(I(b, "debugView", TiltShiftPass.DebugView), 0, 3);
        }

        /// <summary>撮影の上書き: 起動引数 -hd2d と -state の中の tsfocus・tsdebug (-state のほうが勝つ)</summary>
        static void ApplyStateOverrides()
        {
            _stateOverrides = "";
            var kv = new Dictionary<string, string>();
            try
            {
                var hd = Autopilot.Arg("-hd2d");
                if (!string.IsNullOrEmpty(hd)) foreach (var p in HD2DFlags.ParseSpec(hd, ',', ';')) kv[p.Key] = p.Value;
                var st = Autopilot.Arg("-state");
                if (!string.IsNullOrEmpty(st)) foreach (var p in HD2DFlags.ParseSpec(st, ';')) kv[p.Key] = p.Value;
            }
            catch (Exception e) { Debug.LogWarning("[TiltShift] 起動引数が読めない: " + e.Message); return; }
            string v;
            if (kv.TryGetValue("tsfocus", out v))
            {
                if (string.Equals(v, "path", StringComparison.OrdinalIgnoreCase)) { TiltShiftSettings.Focus = TiltShiftFocus.Path; _stateOverrides += " tsfocus=path"; }
                else if (string.Equals(v, "depth", StringComparison.OrdinalIgnoreCase)) { TiltShiftSettings.Focus = TiltShiftFocus.Depth; _stateOverrides += " tsfocus=depth"; }
                else Debug.LogWarning("[TiltShift] tsfocus=" + v + " は path か depth");
            }
            if (kv.TryGetValue("tscurve", out v))   // P24 2周目: 帯の外の伸び方だけを切り替えて撮る (上限・強さは設計図のまま)
            {
                if (string.Equals(v, "lens", StringComparison.OrdinalIgnoreCase)) { TiltShiftSettings.Curve = TiltShiftCurve.Lens; _stateOverrides += " tscurve=lens"; }
                else if (string.Equals(v, "smooth", StringComparison.OrdinalIgnoreCase)) { TiltShiftSettings.Curve = TiltShiftCurve.Smooth; _stateOverrides += " tscurve=smooth"; }
                else Debug.LogWarning("[TiltShift] tscurve=" + v + " は lens か smooth");
            }
            int n;
            if (kv.TryGetValue("tsdebug", out v))
            {
                if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out n) && n >= 0 && n <= 3) { TiltShiftPass.DebugView = n; _stateOverrides += " tsdebug=" + n; }
                else Debug.LogWarning("[TiltShift] tsdebug=" + v + " は 0〜3");
            }
            _stateOverrides = _stateOverrides.Trim();
        }

        /// <summary>ログの1行</summary>
        public static string Summary()
        {
            var inv = CultureInfo.InvariantCulture;
            return "詰め (" + _source + (_stateOverrides.Length > 0 ? " + " + _stateOverrides : "") + "): 帯 "
                + (TiltShiftSettings.Focus == TiltShiftFocus.Path
                    ? "道 s " + TiltShiftSettings.PathNear.ToString("0.##", inv) + "〜" + TiltShiftSettings.PathFar.ToString("0.##", inv)
                    : "深さ")
                + " | 伸び方 " + (TiltShiftSettings.Curve == TiltShiftCurve.Lens
                    ? "レンズ 奥" + TiltShiftSettings.LensFar.ToString("0.##", inv) + "・手前" + TiltShiftSettings.LensNear.ToString("0.##", inv)
                    : "smoothstep")
                + " | 上限 PC " + TiltShiftSettings.MaxPxPC.ToString("0.#", inv) + "・スマホ " + TiltShiftSettings.MaxPxPhone.ToString("0.#", inv) + " px"
                + " | 点 " + TiltShiftPass.Taps + "/" + TiltShiftPass.NearOwnTaps
                + " | 手前×" + TiltShiftSettings.NearScale.ToString("0.##", inv)
                + (TiltShiftPass.DebugView != 0 ? " | 調べ " + TiltShiftPass.DebugView : "");
        }

        /// <summary>dumplayout (layout.json の extra.tiltShiftPass)。TiltShiftHook が登録する</summary>
        public static object DebugInfo()
        {
            var o = new Dictionary<string, object>();
            o["loaded"] = _loaded;
            o["source"] = _source;
            o["stateOverrides"] = _stateOverrides;
            o["focus"] = TiltShiftSettings.Focus == TiltShiftFocus.Path ? "path" : "depth";
            o["focusUsed"] = TiltShiftPass.LastFocus;
            o["pathBand"] = new[] { TiltShiftSettings.PathNear, TiltShiftSettings.PathFar };
            o["nearScale"] = TiltShiftSettings.NearScale;
            o["curve"] = TiltShiftSettings.Curve == TiltShiftCurve.Lens ? "lens" : "smooth";
            o["curveUsed"] = TiltShiftPass.LastCurve;
            o["lens"] = new[] { TiltShiftSettings.LensFar, TiltShiftSettings.LensNear };
            o["maxPx"] = new[] { TiltShiftSettings.MaxPxPC, TiltShiftSettings.MaxPxPhone };
            o["ramp"] = new[] { TiltShiftSettings.RampNear, TiltShiftSettings.RampFar };
            o["taps"] = TiltShiftPass.Taps;
            o["nearOwnTaps"] = TiltShiftPass.NearOwnTaps;
            o["nearDilateTile"] = TiltShiftPass.NearDilateTile;
            o["jitter"] = TiltShiftPass.Jitter;
            o["farExclude"] = TiltShiftPass.FarExclude;
            o["tilt"] = new[] { TiltShiftPass.TiltTop, TiltShiftPass.TiltTopFrom, TiltShiftPass.TiltBottom, TiltShiftPass.TiltBottomFrom };
            o["debugView"] = TiltShiftPass.DebugView;
            var p = TiltShiftPass.LastPlane;
            o["plane"] = new[] { p.x, p.y, p.z, p.w };
            o["maxPxUsed"] = TiltShiftPass.LastMaxPx;
            o["wouldRun"] = TiltShiftHook.WouldRun;
            return o;
        }

        // ---- JSON の読み方 (JObject を手で読む。IL2CPP で安全) ----

        static bool Num(JToken t, out float v)
        {
            v = 0f;
            if (t == null) return false;
            if (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) { v = (float)t; return true; }   // 明示の変換 (総称の Value<T> を使わない = IL2CPP で安全。StageLook と同じ)
            if (t.Type == JTokenType.String) return float.TryParse((string)t, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
            return false;
        }

        static float F(JObject o, string k, float def) { float v; return Num(o[k], out v) ? v : def; }

        static int I(JObject o, string k, int def) { float v; return Num(o[k], out v) ? Mathf.RoundToInt(v) : def; }

        static bool Bool(JObject o, string k, bool def)
        {
            var t = o[k];
            if (t == null) return def;
            if (t.Type == JTokenType.Boolean) return (bool)t;
            float v;
            return Num(t, out v) ? v != 0f : def;
        }

        static string Str(JObject o, string k)
        {
            var t = o[k];
            return t != null && t.Type == JTokenType.String ? (string)t : null;
        }
    }
}
