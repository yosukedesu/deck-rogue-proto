// Stage.cs — 舞台 (2026-09-07 段階1): 背景だけを別カメラ (Screen Space - Camera の舞台キャンバス) に描き、
// そこにだけポスト処理 (ブルーム・ビネット・色調整) と粒子 (蛍・舞う葉・ほこり) を掛ける。
// 紙の UI は従来の Overlay キャンバスに重なるので、にじんだり光ったりしない。オクトラの「光と空気」の代替。
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static class Stage
    {
        static RectTransform _root;
        static Camera _cam;
        static Volume _volume;
        static ColorAdjustments _color;
        static Transform _fx;
        static int _paintedAct = -1;

        public static RectTransform Root { get { Ensure(); return _root; } }

        public static void Ensure()
        {
            if (_root != null) return;
            _cam = Camera.main;
            if (_cam == null)
            {
                var cgo = new GameObject("StageCamera");
                _cam = cgo.AddComponent<Camera>();
                cgo.tag = "MainCamera";
            }
            _cam.orthographic = true;
            _cam.orthographicSize = 5.4f;           // 1 unit = 100px、1080px の高さ
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 100f;
            _cam.transform.position = new Vector3(0f, 0f, -10f);
            _cam.transform.rotation = Quaternion.identity;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = PaperFx.Night;
            try
            {
                var data = _cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.None;
                data.renderShadows = false;
            }
            catch (Exception e) { Debug.LogWarning("[Stage] URP のカメラ設定に失敗: " + e.Message); }

            // 舞台キャンバス (カメラに描かれる = ポスト処理が掛かる)
            var go = new GameObject("StageCanvas", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _cam;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = -10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            _root = UiKit.NewRect("StageRoot", go.transform);
            UiKit.Stretch(_root, 0f, 0f, 0f, 0f);

            // ポスト処理: ブルーム (蛍と月が光る)・ビネット・色調整 (幕で色味を変える)
            try
            {
                var vgo = new GameObject("StageVolume");
                _volume = vgo.AddComponent<Volume>();
                _volume.isGlobal = true;
                _volume.priority = 1f;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.value = 0.72f;
                bloom.intensity.value = 1.2f;
                bloom.scatter.value = 0.78f;
                bloom.tint.value = new Color(1f, 0.93f, 0.75f);
                var vig = profile.Add<Vignette>(true);
                vig.intensity.value = 0.3f;
                vig.smoothness.value = 0.55f;
                vig.color.value = new Color(0.03f, 0.02f, 0.06f);
                _color = profile.Add<ColorAdjustments>(true);
                _color.postExposure.value = 0.05f;
                _color.contrast.value = 8f;
                _color.saturation.value = -4f;
                _volume.sharedProfile = profile;
            }
            catch (Exception e) { Debug.LogWarning("[Stage] Volume の作成に失敗: " + e.Message); }

            // 粒子 (ワールド空間・舞台キャンバスの手前 z=-1)
            _fx = new GameObject("StageFx").transform;
            _fx.position = new Vector3(0f, 0f, -1f);
            Fireflies();
            Dust();
            Leaves();
            _root.gameObject.AddComponent<StageSway>();
        }

        /// <summary>幕の背景を舞台に描く (同じ幕なら描き直さない)。紙の粒とビネットはポスト処理と粒子の下に敷く</summary>
        public static void Paint(int act)
        {
            Ensure();
            if (_paintedAct == act) return;
            _paintedAct = act;
            for (int i = _root.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_root.GetChild(i).gameObject);
            var art = Theme.Art("bg", "act" + act);
            if (art != null)
            {
                var bg = UiKit.Pan(_root, Color.white, "bg");
                bg.sprite = art; bg.preserveAspect = false; bg.raycastTarget = false;
                UiKit.Stretch(bg.rectTransform, 0f, 0f, 0f, 0f);
            }
            else
            {
                Color skyTop = act == 1 ? UiKit.Hex("#26294a") : act == 2 ? UiKit.Hex("#1f3a3d") : UiKit.Hex("#3a1f2a");
                Color skyBot = act == 1 ? UiKit.Hex("#12142a") : act == 2 ? UiKit.Hex("#0d1c20") : UiKit.Hex("#160c12");
                Color blobA = act == 1 ? UiKit.Hex("#7a5aa0") : act == 2 ? UiKit.Hex("#4a8a8c") : UiKit.Hex("#a05a6a");
                Color blobB = act == 1 ? UiKit.Hex("#46788c") : act == 2 ? UiKit.Hex("#3a6a7a") : UiKit.Hex("#7a4a3a");
                var sky = UiKit.Pan(_root, Color.white, "sky");
                sky.sprite = ThemeFx.Gradient("sky" + act, skyTop, skyBot);
                UiKit.Anchor(sky.rectTransform, new Vector2(0f, 0.34f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
                sky.raycastTarget = false;
                var b1 = PaperFx.BlobImage(_root, blobA, "mist-a");
                UiKit.Anchor(b1.rectTransform, new Vector2(0.05f, 0.55f), new Vector2(0.45f, 1f), Vector2.zero, Vector2.zero);
                b1.color = new Color(1f, 1f, 1f, 0.32f);
                var b2 = PaperFx.BlobImage(_root, blobB, "mist-b");
                UiKit.Anchor(b2.rectTransform, new Vector2(0.6f, 0.5f), new Vector2(1f, 0.95f), Vector2.zero, Vector2.zero);
                b2.color = new Color(1f, 1f, 1f, 0.28f);
                // 月 (くっきりした円 → ブルームで光る。上の空いた帯に置き、敵の吹き出しと重ねない)
                var halo = PaperFx.BlobImage(_root, new Color(1f, 0.95f, 0.8f), "moon-halo");
                UiKit.Anchor(halo.rectTransform, new Vector2(0.58f, 0.87f), new Vector2(0.58f, 0.87f), new Vector2(-200f, -130f), new Vector2(200f, 130f));
                halo.color = new Color(1f, 1f, 1f, 0.16f);
                var moon = UiKit.Pan(_root, new Color(1f, 0.97f, 0.86f, 0.98f), "moon");
                moon.sprite = PaperFx.Disc();
                UiKit.Anchor(moon.rectTransform, new Vector2(0.58f, 0.87f), new Vector2(0.58f, 0.87f), new Vector2(-38f, -38f), new Vector2(38f, 38f));
                moon.raycastTarget = false;
                var ground = UiKit.Pan(_root, Color.white, "ground");
                ground.sprite = ThemeFx.Gradient("ground" + act, UiKit.Hex("#3b3a2c"), UiKit.Hex("#1e1f18"));
                UiKit.Anchor(ground.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.34f), Vector2.zero, Vector2.zero);
                ground.raycastTarget = false;
                var moss = PaperFx.BlobImage(_root, PaperFx.Moss, "moss");
                UiKit.Anchor(moss.rectTransform, new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.36f), Vector2.zero, Vector2.zero);
                moss.color = new Color(1f, 1f, 1f, 0.22f);
                var horizon = UiKit.Pan(_root, new Color(PaperFx.Paper.r, PaperFx.Paper.g, PaperFx.Paper.b, 0.22f), "horizon");
                UiKit.Anchor(horizon.rectTransform, new Vector2(0f, 0.34f), new Vector2(1f, 0.34f), new Vector2(0f, -1f), new Vector2(0f, 2f));
                horizon.raycastTarget = false;
                // 手前の霧の帯 (地面の上をゆっくり流れる)
                var fog = PaperFx.BlobImage(_root, new Color(0.75f, 0.8f, 0.85f), "fog");
                UiKit.Anchor(fog.rectTransform, new Vector2(-0.1f, 0.28f), new Vector2(1.1f, 0.42f), Vector2.zero, Vector2.zero);
                fog.color = new Color(1f, 1f, 1f, 0.10f);
                PaperFx.GrainOver(_root, 0.5f);
            }
            if (_color != null)
            {
                _color.colorFilter.value = act == 1 ? new Color(0.96f, 0.98f, 1.06f) : act == 2 ? new Color(0.92f, 1.02f, 1.02f) : new Color(1.06f, 0.94f, 0.92f);
            }
        }

        // ---- 粒子 ----

        static Texture2D _dot;
        static Sprite DotSprite()
        {
            if (_dot != null) return Sprite.Create(_dot, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 100f);
            _dot = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            _dot.filterMode = FilterMode.Bilinear;
            var px = new Color[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(8f, 8f)) / 8f;
                    px[y * 16 + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d * d));
                }
            _dot.SetPixels(px); _dot.Apply();
            return Sprite.Create(_dot, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 100f);
        }

        static Texture2D _leaf;
        static Texture2D LeafTex()
        {
            if (_leaf != null) return _leaf;
            string[] rows = { "........", "......##", ".....###", "....####", "...####.", "..###...", ".##.....", "#......." };
            _leaf = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            _leaf.filterMode = FilterMode.Point;
            var px = new Color[64];
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    px[(7 - y) * 8 + x] = rows[y][x] == '#' ? Color.white : new Color(0f, 0f, 0f, 0f);
            _leaf.SetPixels(px); _leaf.Apply();
            return _leaf;
        }

        static ParticleSystem NewSystem(string name, Texture2D tex)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_fx, false);
            var ps = go.AddComponent<ParticleSystem>();
            var r = go.GetComponent<ParticleSystemRenderer>();
            // ビルドに確実に含まれる素材 (Resources の .mat 経由)。加算は使わずブルームで光らせる
            var baseMat = Resources.Load<Material>("Materials/ParticleSprite");
            var sh = baseMat != null ? baseMat.shader : Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("UI/Default");
            var mat = baseMat != null ? new Material(baseMat) : new Material(sh);
            mat.mainTexture = tex;
            r.material = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = 5;
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.loop = true;
            return ps;
        }

        static void Fireflies()
        {
            var ps = NewSystem("fireflies", DotSprite().texture);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = new Color(1f, 0.86f, 0.45f, 1f);
            main.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 4f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(18f, 6f, 0.1f); shape.position = new Vector3(0f, 0.6f, 0f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f); vel.y = new ParticleSystem.MinMaxCurve(-0.06f, 0.12f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.35f; noise.scrollSpeed = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.35f, 0.5f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Play();
        }

        static void Dust()
        {
            var ps = NewSystem("dust", DotSprite().texture);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
            main.startColor = new Color(1f, 0.97f, 0.9f, 0.35f);
            main.maxParticles = 120;
            var em = ps.emission; em.rateOverTime = 8f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(19f, 10f, 0.1f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f); vel.y = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Play();
        }

        static void Leaves()
        {
            var ps = NewSystem("leaves", LeafTex());
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 14f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.32f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.56f, 0.68f, 0.42f, 0.9f), new Color(0.72f, 0.55f, 0.3f, 0.9f));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.maxParticles = 20;
            var em = ps.emission; em.rateOverTime = 0.7f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(20f, 0.5f, 0.1f); shape.position = new Vector3(0f, 6f, 0f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.15f); vel.y = new ParticleSystem.MinMaxCurve(-0.9f, -0.5f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.5f; noise.scrollSpeed = 0.3f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            ps.Play();
        }

        /// <summary>霧と月をゆっくり揺らす (舞台が生きている感じ)</summary>
        class StageSway : MonoBehaviour
        {
            void Update()
            {
                float t = Time.time;
                for (int i = 0; i < transform.childCount; i++)
                {
                    var c = transform.GetChild(i) as RectTransform;
                    if (c == null) continue;
                    if (c.name == "mist-a") c.anchoredPosition = new Vector2(Mathf.Sin(t * 0.08f) * 24f, Mathf.Cos(t * 0.06f) * 10f);
                    else if (c.name == "mist-b") c.anchoredPosition = new Vector2(Mathf.Cos(t * 0.07f) * 30f, Mathf.Sin(t * 0.05f) * 12f);
                    else if (c.name == "fog") c.anchoredPosition = new Vector2(Mathf.Sin(t * 0.05f) * 80f, Mathf.Sin(t * 0.11f) * 6f);
                    else if (c.name == "moon-halo") c.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(t * 0.7f));
                }
            }
        }
    }
}
