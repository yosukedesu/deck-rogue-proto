using UnityEngine;

namespace DeckRogue.Game
{
    public static partial class Stage
    {
        // World-space motion is owned by the visual unit. RectTransform/UI anchors stay put.
        public const float CombatImpactTime = 0.32f;
        public static bool CombatWorld => HD2DFlags.DioramaHere;

        public static void CombatApproach(string key, string target, bool ranged, int hits = 1)
        {
            if (!CombatWorld || !_bound.TryGetValue(key, out var u) || u == null) return;
            Vector3 a, b; float ah, bh;
            if (!TryGetUnitBox(key, out a, out ah) || !TryGetUnitBox(target, out b, out bh)) return;
            int version = ++u.MotionVersion;
            Vector3 start = u.CombatOffset;
            Vector3 delta = b - a; delta.y = 0f;
            float distance = delta.magnitude;
            Vector3 dir = distance > 0.01f ? delta / distance : Vector3.right;
            // Weapon/hand reach: stop before the target silhouette, never cross its centre.
            Vector3 travel = ranged ? Vector3.zero : dir * Mathf.Max(0f, distance - (ah * 0.40f + bh * 0.25f));
            float chain = ranged ? 0f : Mathf.Clamp(hits-1,0,7)*0.4f;
            float release = 0.48f + chain, duration = release + 0.30f;
            Tween.After(0.17f, () => { if (u != null && u.MotionVersion == version) u.Play(ranged ? "block" : "attack"); });
            Tween.Run(duration, k =>
            {
                if (u == null || u.MotionVersion != version) return;
                float t = k * duration, amount;
                if (t < 0.12f) amount = -0.035f * Mathf.SmoothStep(0, 1, t / 0.12f);
                else if (t < CombatImpactTime) amount = Mathf.Lerp(-0.035f, 1, Mathf.SmoothStep(0, 1, (t - 0.12f) / 0.20f));
                else if (t < release) amount = 1;
                else amount = 1 - Mathf.SmoothStep(0, 1, (t - release) / 0.30f);
                u.CombatOffset = start * (1-k) + travel * amount;
            }, Ease.Linear, () => { if (u != null && u.MotionVersion == version) u.CombatOffset = Vector3.zero; });
        }

        public static void CombatRecoil(string key, float direction, bool heavy)
        {
            if (!CombatWorld || !_bound.TryGetValue(key, out var u) || u == null) return;
            int version = ++u.RecoilVersion;
            u.Play("hurt");
            float h = u.HasBox ? u.HeightWorld : 2f;
            Tween.Run(heavy ? 0.29f : 0.22f, k =>
            {
                if (u == null || u.RecoilVersion != version) return;
                float kick = k < 0.18f ? Mathf.SmoothStep(0,1,k/0.18f) : 1-Mathf.SmoothStep(0,1,(k-0.18f)/0.82f);
                u.CombatRecoilOffset = (LayoutRotation * Vector3.right) * direction * h * (heavy ? 0.12f : 0.065f) * kick;
            }, Ease.Linear, () => { if (u != null && u.RecoilVersion == version) u.CombatRecoilOffset = Vector3.zero; });
        }
    }
}
