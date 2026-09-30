// StageFx.cs — 舞台の技の光 (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3・§2-4)。
// 骨組み (P00) の口に、P09 (W1) が中身を書いた: 点光源のプール (設計図 look_act1.json の hitLight.maxLights 個・既定3。StageLook.Apply が用意する)。
// 演出からの呼び出しは P13 が書く。stage=old・StageLook が光を当てていない時は何もしない (W1 は誰も呼ばない = 今の見た目のまま)。
// 影は大きい当たり (shadow=true) だけ。影を落とす光の数の上限 (PC 3・スマホ 2) は StageLook と貸し借りする (上限に届いていれば逆光の影を一時的に止めて借りる)。
// 光の減衰は Time.deltaTime (det の撮影では 1/60 秒ずつ・ヒットストップの間はゆっくり)。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DeckRogue.Game
{
    public static class StageFx
    {
        /// <summary>
        /// 当たりの光: world に色 color の点光源を灯し、dur 秒で消す (灯した瞬間がいちばん明るく、(1 − 経過÷dur)² で弱まる)。
        /// intensity = 設計図の hitLight.refDist (既定 1.5 unit) の距離での明るさ (Light.intensity = intensity × refDist²。URP の点光源は距離の2乗で弱まるため)。
        /// 届く距離は hitLight.range (既定 4)。同時に hitLight.maxLights (既定 3) まで。空きが無ければ、いちばん弱まった光を使い回す。
        /// shadow = true (大きい当たり) の時だけ影を落とす。上限を超えるなら StageLook が逆光の影を一時的に止めて貸し、それもできなければ影なしで灯す
        /// </summary>
        public static void HitLight(Vector3 world, Color color, float intensity, float dur, bool shadow)
        {
            if (HD2DFlags.StageMode != HD2DStage.Diorama || !StageLook.Active) return;
            if (!(intensity > 0f) || !(dur > 0f) || float.IsInfinity(intensity) || float.IsInfinity(dur)) return;
            var h = StageLook.Current != null ? StageLook.Current.Hit : Defaults;
            Prepare(h);
            var s = Pick();
            if (s == null) return;
            Release(s);   // 使い回す光が借りていた影を先に返す
            var l = s.Light;
            l.transform.position = world;
            l.color = new Color(color.r, color.g, color.b, 1f);
            l.range = Mathf.Max(0.1f, h.Range);
            s.Peak = intensity * h.RefDist * h.RefDist;
            s.T = 0f;
            s.Dur = dur;
            l.intensity = s.Peak;
            l.shadows = LightShadows.None;
            if (shadow && StageLook.TryBorrowShadowSlot()) { s.Borrowed = true; l.shadows = LightShadows.Hard; }
            l.enabled = true;
            s.Lit = true;
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
            public float Peak, T, Dur;
            public bool Lit, Borrowed;
        }

        static readonly List<Slot> _slots = new List<Slot>();
        static readonly StageLookData.HitLook Defaults = new StageLookData.HitLook();
        static GameObject _root;
        static int _want = 3;

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
        }

        /// <summary>全部消して、借りていた影を返す (StageLook の Apply の頭と Restore が呼ぶ)</summary>
        internal static void StopAll()
        {
            foreach (var s in _slots)
            {
                if (s.Light != null) { s.Light.enabled = false; s.Light.intensity = 0f; s.Light.shadows = LightShadows.None; }
                s.Lit = false;
                Release(s);
            }
        }

        /// <summary>プールの光 (dumplayout の記録用)</summary>
        internal static IEnumerable<Light> PoolLights()
        {
            foreach (var s in _slots) if (s.Light != null) yield return s.Light;
        }

        /// <summary>空いている光。無ければ、いちばん弱まった光</summary>
        static Slot Pick()
        {
            Slot best = null;
            float bestI = float.MaxValue;
            int n = Math.Min(_want, _slots.Count);
            for (int i = 0; i < n; i++)
            {
                var s = _slots[i];
                if (s.Light == null) continue;
                if (!s.Lit) return s;
                if (s.Light.intensity < bestI) { bestI = s.Light.intensity; best = s; }
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

        /// <summary>光を弱めて、dur を過ぎたら消す。StageLook が光を当てていなければすぐ消す</summary>
        static void Tick(float dt)
        {
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
                float e = 1f - u;
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
            }
        }
    }
}
