// Generated VFX atlas + short, layered animation. Original ChatGPT image, 2026-10-05.
// UI-space quads follow the established combat FX layer; StageFx supplies world light.
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static class CodexBattleFx
    {
        const int Capacity = 24;
        sealed class Slot { public RawImage Image; public bool Busy; public int Version; }
        static readonly Slot[] Pool = new Slot[Capacity];
        static Texture2D _atlas;
        static bool _loaded;
        public static bool Ready
        {
            get
            {
                if (!_loaded) { _loaded = true; _atlas = Resources.Load<Texture2D>("Art/fx/codex-vfx-atlas"); }
                return _atlas != null;
            }
        }

        // Top-left slash, top-right impact, bottom-left arcane, bottom-right guard.
        static bool Emit(RectTransform layer, Vector2 pos, int cell, Color color,
            float size, float duration, float angle, float sweep, float from, float to, float opacity = 1f)
        {
            if (layer == null || !Ready) return false;
            Slot slot = null;
            int limit = GfxQuality.Level == GfxLevel.Low ? 12 : Capacity;
            for (int i = 0; i < limit; i++)
            {
                if (Pool[i] == null) Pool[i] = new Slot();
                if (Pool[i].Image == null || !Pool[i].Busy) { slot = Pool[i]; break; }
            }
            // Saturated multi-hit effects are skipped instead of growing the object count.
            if (slot == null) return true;
            if (slot.Image == null)
            {
                var rt = UiKit.NewRect("codex-vfx", layer);
                slot.Image = rt.gameObject.AddComponent<RawImage>();
                slot.Image.raycastTarget = false;
            }
            var image = slot.Image;
            var rect = image.rectTransform;
            rect.SetParent(layer, false);
            rect.SetAsLastSibling();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = Vector2.one * size;
            image.texture = _atlas;
            // Half a source texel inset prevents filtering across atlas quadrants.
            float pad = 0.5f / _atlas.width;
            image.uvRect = new Rect((cell % 2) * 0.5f + pad,
                cell < 2 ? 0.5f + pad : pad, 0.5f - 2f * pad, 0.5f - 2f * pad);
            slot.Busy = true;
            int version = ++slot.Version;
            rect.gameObject.SetActive(true);
            Tween.Run(duration, t =>
            {
                if (rect == null || slot.Version != version) return;
                float travel = 1f - Mathf.Pow(1f - t, 3f);
                float scale = Mathf.Lerp(from, to, travel);
                rect.localScale = new Vector3(scale, scale, 1f);
                rect.localRotation = Quaternion.Euler(0, 0, angle + sweep * travel);
                float attack = Mathf.Min(1f, t / 0.055f);
                float fade = 1f - Mathf.SmoothStep(0.12f, 1f, t);
                image.color = new Color(color.r, color.g, color.b, color.a * opacity * attack * fade);
            }, Ease.Linear, () =>
            {
                if (slot.Version != version) return;
                slot.Busy = false;
                if (rect != null) rect.gameObject.SetActive(false);
            });
            return true;
        }

        public static bool Slash(RectTransform layer, Vector2 pos, float angle, Color color, bool big, float scale)
        {
            if (!Ready) return false;
            float size = (big ? 330f : 250f) * scale;
            Emit(layer, pos, 0, Color.Lerp(color, Color.white, 0.35f), size, 0.25f, angle - 65f, 48f, 0.62f, 1.1f);
            Emit(layer, pos, 1, color, big ? 140f : 100f, 0.16f, angle, 8f, 0.45f, 1.1f, 0.86f);
            return true;
        }

        public static bool Spell(RectTransform layer, Vector2 pos, Color color, bool big, float scale, bool spark = false)
        {
            if (!Ready) return false;
            Emit(layer, pos, 2, color, (big ? 280f : 215f) * scale, spark ? 0.23f : 0.4f, -18f, 35f, 0.25f, 1.18f, 0.85f);
            Emit(layer, pos, 1, Color.Lerp(color, Color.white, 0.5f), big ? 125f : 90f, 0.15f, 20f, -10f, 0.3f, 1f);
            return true;
        }

        public static bool Guard(RectTransform layer, Vector2 pos, float side)
        {
            return Emit(layer, pos + new Vector2(48f * side, 4f), 3,
                new Color(0.7f, 0.9f, 1f, 1f), 195f, 0.32f, side < 0 ? 180f : 0f, 0f, 0.72f, 1.04f);
        }

        public static bool Cast(RectTransform layer, Vector2 pos, Color color)
        {
            return Emit(layer, pos + new Vector2(26f, 12f), 2, color, 125f, 0.3f, 0f, -40f, 1f, 0.25f, 0.7f);
        }
    }
}
