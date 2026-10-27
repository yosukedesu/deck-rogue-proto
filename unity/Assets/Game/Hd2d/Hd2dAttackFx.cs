// Hd2dAttackFx.cs — HD-2D の攻撃演出の描き手 (dot の Godot 版 codex/unity-parity-20261013・checkpoint 192 の移植・2026-10-27)。
// Godot では AuthoredGeometryFx (MeshInstance3D + ShaderMaterial) と BattleStage/BattleActor が担っていた所を、Unity の既存の流れ
// (ResolvedCombatFx の Ticket → WorldBattleFx.BeginAuthoredAttack/AuthoredAttackContact → AttackPlayback.Land) に合わせて書いた。
// 幾何と時間は Hd2dAttackPrototype (dot のコードそのまま) が組む。ここは ①札 id で受けるか決める ②頂点を世界へ写して Mesh に流す
// ③主人公の斧の先・杖の石の位置を毎フレーム渡す ④接触の光 (StageFx の契約の光) ⑤予備動作の音、だけ。
// 近似にした所 (Godot の BattleStage は 3D の俳優を持つが Unity は板): 踏み込みの距離は「斧の先が相手の縁に届く」まで (Hd2dApproach)、
// 相手の縁は絵の不透明な画素 (Stage.TryUnitSurface)、途中に別の敵がいる時の遠隔打ち (remote) は体の前が相手の上半身に当たる手前で止める。
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using DeckRogue.Game.Hd2d;
using HV3 = DeckRogue.Game.Hd2dMath.Vector3;
using HV2 = DeckRogue.Game.Hd2dMath.Vector2;
namespace DeckRogue.Game
{
    internal static class Hd2dAttackFx
    {
        public const string RendererId = "hd2d-prototype-r192";
        sealed class Draw { public Mesh Mesh; public MeshRenderer Renderer; public Vector3[] V; public Vector2[] UV, Gain; public Color[] C; public Texture2D Texture; public float Kind, Motion, TextureEnabled, AuthoredRgba, UseTexture; }
        sealed class Slot
        {
            public Transform Root; public Draw[] Draws; public NativeEffect Effect; public string EffectKey, Card; public ResolvedCombatFx.Ticket Ticket;
            public bool Busy, Landed, Locked, Remote; public int Version; public long Serial;
            public float Started, Expected, Height, Size, LightPower, Tail;
            public Vector3 Target, Feet, Right, Up, Toward, GroundRight, GroundToward; public Vector3? NearSurface;
            public Color[] Roles; public Color LightColor; public StageFx.ContractLightPlayback Light; public WorldBattleFx.AttackSpec LightSpec;
        }
        static readonly Slot[] Pool = new Slot[8]; static int _epoch; static long _serial; static Material _material; static readonly MaterialPropertyBlock Props = new MaterialPropertyBlock();
        static readonly Dictionary<string, NativeEffect> _cache = new Dictionary<string, NativeEffect>();
        static readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Color[]> _palettes = new Dictionary<string, Color[]>();
        public static string LastFailure { get; private set; } public static int Submitted { get; private set; } public static int Recycled { get; private set; } public static int StaleContacts { get; private set; }
        public static int ActiveCount { get { int n = 0; foreach (var s in Pool) if (s != null && s.Busy) n++; return n; } }

        // ---- 札 id で受けるか (登録は PhysicalVfxProfiles / ElementalVfxProfiles = dot の表そのまま) ----
        public static bool Supports(string cardId) => HD2DFlags.Hd2dFx && !string.IsNullOrEmpty(cardId) && Hd2dAttackPrototype.Supports("card:" + cardId);
        public static bool Eligible(ResolvedCombatFx.Ticket t) => t != null && t.Event == "DamageDealt" && t.Outcome == "hit" && t.Target != "player" && t.SourceKind == "card" && Supports(t.SourceKey);
        static AttackRoute RouteOf(ResolvedCombatFx.Ticket t) => new AttackRoute("card:" + t.SourceKey, t.SourceKey, "card", t.AttackFamily ?? "", RendererId, t.Element ?? "physical", false, "ported from codex/unity-parity-20261013 checkpoint 192");
        public static float Lead(ResolvedCombatFx.Ticket t) => Eligible(t) ? Hd2dAttackPrototype.Lead("card:" + t.SourceKey) : 0f;
        public static float Tail(string cardId) => Hd2dAttackPrototype.Tail("card:" + cardId);
        public static float PoseLead(string cardId) => Hd2dAttackPrototype.PoseLead(cardId);
        public static bool UsesWeaponContact(string cardId) => Supports(cardId) && Hd2dAttackPrototype.UsesWeaponContact(cardId);
        public static bool UsesCastEmitter(string cardId) => Supports(cardId) && Hd2dAttackPrototype.UsesCastEmitter(cardId);
        public static bool UsesFire(string cardId) => Supports(cardId) && Hd2dAttackPrototype.UsesFire(cardId);
        public static bool HasAuthoredAudio(string cardId) => Supports(cardId) && Hd2dAttackPrototype.HasAuthoredAudio(cardId);
        public static string AudioCard(string cardId) => Hd2dAttackPrototype.AudioCard(cardId);
        /// <summary>踏み込みと当たりの時刻の表 (CardVfxProfile.Showcase と同じ 11 の値)。dot の表の Motion をそのまま写す</summary>
        public static CardVfxProfile.Showcase Showcase(string cardId)
        {
            if (!Supports(cardId)) return null;
            var p = PhysicalVfxProfiles.ForCard(cardId)?.Motion ?? ElementalVfxProfiles.ForCard(cardId)?.Motion;
            if (p == null) return null;
            return new CardVfxProfile.Showcase(p.Contact, p.Windup, p.Interval, p.Recovery, p.Residue, p.Reach, p.ImpactPause, p.ChainPause, p.FinalPause, p.Heavy, p.AuthoredAttack);
        }

        // ---- 主人公の絵の上の点 (Godot の BattleActor.PrototypeAxeContactPoint / StrikeWindupPoint / FireSourcePoint。攻撃のコマの画素の位置) ----
        static Vector2? AxeUv(string art) => art == "leader_green" ? new Vector2(67f / 92f, 61f / 78f) : art == "leader_blue" ? new Vector2(109f / 128f, 34f / 84f) : art == "leader_white" ? new Vector2(86f / 112f, 66f / 84f) : (Vector2?)null;
        static Vector2? WindupUv(string art) => art == "leader_green" ? new Vector2(24f / 92f, 36f / 78f) : art == "leader_blue" ? new Vector2(97f / 128f, 15f / 84f) : art == "leader_white" ? new Vector2(15f / 112f, 48f / 84f) : (Vector2?)null;
        static Vector2? FocusUv(string art) => art == "leader_green" ? new Vector2(69.5f / 92f, 54f / 78f) : art == "leader_blue" ? new Vector2(100f / 128f, 38f / 84f) : art == "leader_white" ? new Vector2(86f / 112f, 65f / 84f) : (Vector2?)null;
        static string ArtKey(string art) { if (string.IsNullOrEmpty(art)) return ""; foreach (var k in new[] { "leader_green", "leader_blue", "leader_white" }) if (art.StartsWith(k, StringComparison.Ordinal)) return k; return art; }
        static Vector3 FrameOrBody(Stage.UnitAnchor a, int frame, Vector2? uv, Vector2 fallback)
        {
            if (uv.HasValue && a.AttackFrames != null && a.AttackFrames.Count > frame) return a.TexturePoint(a.AttackFrames[frame], uv.Value);
            return a.TexturePoint(a.CurrentTex ?? a.BaseTex, fallback);
        }
        /// <summary>斧の先 (攻撃の 3 コマ目)。無い絵は体の右端の中ほど</summary>
        public static Vector3 AxeContactPoint(Stage.UnitAnchor a) => FrameOrBody(a, 2, AxeUv(ArtKey(a.ArtName)), new Vector2(.90f, .55f));
        /// <summary>振りかぶりの斧 (攻撃の 2 コマ目)</summary>
        public static Vector3 WindupPoint(Stage.UnitAnchor a) => FrameOrBody(a, 1, WindupUv(ArtKey(a.ArtName)), new Vector2(.35f, .40f));
        /// <summary>杖の石・斧の宝石 (攻撃の 4 コマ目)。無い絵は体の右端</summary>
        public static Vector3 FireSourcePoint(Stage.UnitAnchor a) => FrameOrBody(a, 3, FocusUv(ArtKey(a.ArtName)), new Vector2(.98f, .50f));
        /// <summary>主人公の攻撃のコマの番号 (Godot の PoseFrame)。攻撃中でなければ 0</summary>
        static int PoseFrame(Stage.UnitAnchor a) => a != null && a.Anim == "attack" ? a.Frame : 0;

        // ---- 踏み込みの計画 (Godot の BattleStage.PrototypeStrikeApproach の写し。斧の先が相手の縁に届く距離まで寄る) ----
        sealed class ApproachPlan { public string Card, Target; public bool Remote; public Vector3? Surface; public float At; }
        static readonly Dictionary<string, ApproachPlan> _plans = new Dictionary<string, ApproachPlan>();
        /// <summary>dot の BattleActor.ApplyHd2dPose のコマの長さ。前の物理の斬りは当たりの前に斧の絵を見せてから同じ終わりまで持つ。遠隔の飛びは早く離す</summary>
        public static float[] PoseDurations(string cardId, bool remote)
        {
            bool weapon = UsesWeaponContact(cardId), fire = UsesFire(cardId);
            return remote ? new[] { .06f, .04f, .075f, .18f } : new[] { .10f, fire ? .14f : weapon ? .075f : .12f, weapon ? .100f : .055f, .18f };
        }
        /// <summary>主人公の踏み込み: 札が HD-2D の登録にあれば、斧の先が相手の不透明な縁に届く距離・dot の構えの時刻でステージを動かす</summary>
        public static void Approach(string key, string target, string cardId, bool ranged, int hits, CardVfxProfile.Showcase profile)
        {
            float? reachWorld = null; bool remote = false; Vector3? surface = null;
            if (UsesWeaponContact(cardId) && Stage.TryGetUnitAnchor(key, out var hero) && Stage.TryGetUnitAnchor(target, out var foe))
            {
                var delta = foe.FeetWorld - hero.FeetWorld; delta.y = 0f; float distance = delta.magnitude;
                if (distance > .01f)
                {
                    var dir = delta / distance;
                    var axe = AxeContactPoint(hero);
                    bool nearIsLeft = Vector3.Dot(dir, foe.Right) > 0f;   // 相手の「主人公に向いた側」= 主人公が左にいれば絵の左端
                    bool found = Stage.TryUnitSurface(foe, axe.y, nearIsLeft, out var s);
                    if (found)
                    {
                        surface = s;
                        float contactTravel = Mathf.Max(0f, Vector3.Dot(s - axe, dir));
                        float safeTravel = contactTravel;
                        // 体の前 (高さの 65%) が相手の上半身の縁の手前 (絵の 2%) で止まる距離。斧より体が先に当たるなら遠隔打ち
                        float bodyY = hero.FeetWorld.y + hero.HeightWorld * .65f;
                        if (Stage.TryUnitSurface(foe, bodyY, nearIsLeft, out var upper, true) && Stage.TryUnitSurface(hero, bodyY, !nearIsLeft, out var bodyFront, true))
                            safeTravel = Mathf.Min(safeTravel, Mathf.Max(0f, Vector3.Dot(upper - bodyFront, dir) - hero.HeightWorld * .02f));
                        float reach = Mathf.Min(safeTravel, Mathf.Max(0f, distance - hero.HeightWorld * .05f));
                        remote = reach + hero.HeightWorld * .035f < contactTravel;
                        reachWorld = reach;
                    }
                }
            }
            _plans[target] = new ApproachPlan { Card = cardId, Target = target, Remote = remote, Surface = surface, At = Time.time };
            Stage.ShowcaseApproach(key, target, ranged, hits, profile, reachWorld, PoseLead(cardId), PoseDurations(cardId, remote));
        }
        static ApproachPlan PlanFor(string target, string cardId) => _plans.TryGetValue(target, out var p) && p.Card == cardId && Time.time - p.At < 3f ? p : null;

        // ---- 資源 (シェーダ・絵・色の表) ----
        public static bool Ready()
        {
            LastFailure = null;
            if (!Tween.SchedulingAllowed) { LastFailure = "hd2d_scope_cancelled"; return false; }
            if (!Stage.CombatWorld || Stage.Camera == null || !Stage.Camera.isActiveAndEnabled) { LastFailure = "hd2d_world_camera_unavailable"; return false; }
            if (ElementBattleFx.ForceMissingForTest) { LastFailure = "hd2d_forced_resource_missing"; return false; }
            if (QualitySettings.activeColorSpace != ColorSpace.Linear) { LastFailure = "hd2d_requires_linear_project"; return false; }
            if (_material != null) return true;
            var shader = Shader.Find("DeckRogue/Hd2dPrototype");
            if (shader == null || !shader.isSupported) { LastFailure = "hd2d_shader_unavailable"; return false; }
            _material = new Material(shader);
            return true;
        }
        static Texture2D TextureOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Texture2D t;
            if (_textures.TryGetValue(name, out t)) return t;
            t = Resources.Load<Texture2D>("Art/fx/" + name);
            if (t == null) Debug.LogWarning("[Hd2dAttackFx] 絵が無い: Art/fx/" + name);
            _textures[name] = t;
            return t;
        }
        static Color Role(JToken token, string what) { var s = (string)token; Color c; if (s == null || !ColorUtility.TryParseHtmlString(s, out c)) throw new FormatException("hd2d palette lacks " + what); return c.linear; }
        /// <summary>色の表 Data/&lt;palette&gt;.json: roles_srgb の shadow/primary/edge/core と scene_light_srgb (dot の表そのまま。無ければ例外＝旧い描き手へ)。[4] が光の色</summary>
        static Color[] Palette(string name)
        {
            Color[] roles;
            if (_palettes.TryGetValue(name, out roles)) return roles;
            var asset = Resources.Load<TextAsset>("Data/" + name);
            if (asset == null) throw new FormatException("hd2d palette missing: " + name);
            var obj = JObject.Parse(asset.text);
            var r = obj["roles_srgb"];
            roles = new[] { Role(r?["shadow"], name + ".shadow"), Role(r?["primary"], name + ".primary"), Role(r?["edge"], name + ".edge"), Role(r?["core"], name + ".core"), Role(obj["scene_light_srgb"], name + ".light") };
            _palettes[name] = roles;
            return roles;
        }
        /// <summary>Godot の ConfigureMaterial の _TextureMotion の表 (絵の名前と層の種類 → シェーダの動きの番号)。数値はそのまま</summary>
        static float TextureMotion(string tex, int kind) => tex == "lightning-discharge-surface-r2" ? 32f : tex.StartsWith("hd2d-wood-contact-") ? 31f : tex.StartsWith("hd2d-waterwide-phase-") ? 30f : tex == "earth-ground-grain-r2" ? 28f : tex.StartsWith("hd2d-dark-contraction-") ? 25f : tex == "ice-crystal-surface-r2" ? (kind == 11 ? 21f : kind == 12 ? 22f : 0f) : tex.StartsWith("hd2d-water-impact-") ? (tex.EndsWith("03") ? 20f : 18f) : tex == "water-flow-surface-r1" ? 19f : tex.StartsWith("hd2d-stomp-aura-") ? 17f : tex == "hd2d-stomp-pressure-v1" ? 14f : tex.StartsWith("hd2d-fire-volume-") ? (kind == 15 ? 23f : (tex.EndsWith("03") || tex.EndsWith("04")) ? 13f : 12f) : tex.StartsWith("hd2d-fire-projectile-") ? 1f : tex == "hd2d-stomp-shin-v4" ? 9f : tex == "hd2d-fire-impact-v1" ? 2f : tex == "hd2d-earth-rock-v1" ? 10f : tex == "hd2d-earth-impact-v1" ? 3f : tex == "hd2d-earth-dust-v1" ? 4f : tex == "hd2d-physical-slash-v1" ? (kind == 17 ? 26f : kind == 18 ? 27f : kind == 9 ? 16f : 15f) : tex == "hd2d-earth-foot-v1" ? 6f : (tex == "hd2d-fire-impact-phase-03" || tex == "hd2d-fire-impact-phase-04") ? 8f : tex.StartsWith("hd2d-fire-impact-phase-") ? 7f : 0f;

        // ---- 枠の確保と片付け ----
        static void DestroyDraws(Slot s) { if (s?.Draws == null) return; foreach (var d in s.Draws) if (d?.Mesh != null) UnityEngine.Object.Destroy(d.Mesh); foreach (Transform c in s.Root) UnityEngine.Object.Destroy(c.gameObject); s.Draws = null; }
        static void Retire(Slot s, int v) { if (s.Version != v) return; s.Busy = false; s.Light?.Cancel(); s.Light = null; s.Ticket = null; if (s.Root != null) s.Root.gameObject.SetActive(false); }
        public static void Cancel()
        {
            _epoch++; foreach (var s in Pool) if (s != null) { Retire(s, s.Version); s.Version++; }
        }
        static Slot Acquire(string key, NativeEffect effect)
        {
            int limit = GfxQuality.Level == GfxLevel.Low ? 4 : 8; Slot s = null;
            for (int i = 0; i < limit; i++) { if (Pool[i] == null) Pool[i] = new Slot(); if (!Pool[i].Busy || Pool[i].Root == null) { s = Pool[i]; break; } }
            if (s == null) { s = Pool[0]; for (int i = 1; i < limit; i++) if (Pool[i].Serial < s.Serial) s = Pool[i]; Recycled++; }
            Retire(s, s.Version); s.Version++; s.Serial = ++_serial;
            if (s.Root == null) { s.Root = new GameObject("hd2d-prototype-r192").transform; }
            s.Root.position = Vector3.zero; s.Root.rotation = Quaternion.identity; s.Root.localScale = Vector3.one;
            if (s.EffectKey != key || s.Draws == null || s.Draws.Length != effect.Batches.Length) { DestroyDraws(s); s.Draws = new Draw[effect.Batches.Length]; s.EffectKey = key; }
            s.Effect = effect; s.Landed = false; s.Locked = false; s.Light = null;
            for (int j = 0; j < effect.Batches.Length; j++)
            {
                var b = effect.Batches[j];
                var d = s.Draws[j];
                if (d == null || d.V.Length != b.Count)
                {
                    if (d?.Mesh != null) UnityEngine.Object.Destroy(d.Mesh);
                    var go = new GameObject("hd2d-batch-" + j); go.transform.SetParent(s.Root, false);
                    var mesh = new Mesh { name = "hd2d-prototype-" + j }; mesh.MarkDynamic();
                    d = new Draw { Mesh = mesh, V = new Vector3[b.Count], UV = new Vector2[b.Count], Gain = new Vector2[b.Count], C = new Color[b.Count] };
                    // 頂点は 3 の倍数ずつ使う (Layer.Quad=6・Taper=3)。Godot の ArrayMesh は 512 個の添字をそのまま受けたが、Unity は 3 の倍数でないと
                    // "Failed setting triangles" で三角形が空になる (2026-10-27 の撮影で層が一枚も描かれなかった原因)
                    int triCount = b.Indices.Length - b.Indices.Length % 3; var tri = new int[triCount]; Array.Copy(b.Indices, tri, triCount);
                    mesh.vertices = d.V; mesh.uv = d.UV; mesh.SetUVs(1, d.Gain); mesh.colors = d.C; mesh.triangles = tri;
                    mesh.bounds = new Bounds(Vector3.zero, new Vector3(64, 64, 64));
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    d.Renderer = go.AddComponent<MeshRenderer>(); d.Renderer.sharedMaterial = _material; d.Renderer.sortingOrder = j;
                    d.Renderer.shadowCastingMode = ShadowCastingMode.Off; d.Renderer.receiveShadows = false; d.Renderer.enabled = false;
                    s.Draws[j] = d;
                }
                string tex = b.Texture.Length > 0 ? b.Texture : effect.Texture;
                d.Texture = TextureOf(tex);
                d.Kind = b.Kind;
                d.TextureEnabled = tex.Length > 0 ? 1f : 0f;
                d.AuthoredRgba = b.Texture.Length > 0 ? 1f : 0f;
                d.UseTexture = b.Texture.Length > 0 ? 1f : (j == 0 ? 1f : 0f);
                d.Motion = TextureMotion(tex, b.Kind);
            }
            return s;
        }
        static bool Current(Slot s, int v, int e) => e == _epoch && s.Version == v && s.Busy && s.Root != null;

        // ---- 世界への写し (Godot の WorldPoint: Space 0 = 相手を原点に板の基底、1 = 地面 (足元を原点に世界の軸)、2 = 足元＋錨＋板) ----
        static Vector3 Basis(Slot s, HV3 p) => s.Right * p.X + s.Up * p.Y + s.Toward * p.Z;
        static Vector3 Ground(Slot s, HV3 p) => s.GroundRight * p.X + Vector3.up * p.Y + s.GroundToward * p.Z;
        static Vector3 WorldPoint(Slot s, NativeBatch b, int i)
        {
            HV3 p = b.Position(i); float scale = s.Height * s.Size;
            if (b.Space == 1) return s.Feet + Ground(s, p) * scale + Vector3.up * .009f;
            if (b.Space == 2) return s.Feet + Ground(s, b.Anchor(i)) * scale + Basis(s, p) * scale;
            return s.Target + Basis(s, p) * scale;
        }
        static HV3 Local(Slot s, Vector3 world)
        {
            var d = world - s.Target; float scale = Mathf.Max(.0001f, s.Height * s.Size);
            return new HV3(Vector3.Dot(d, s.Right) / scale, Vector3.Dot(d, s.Up) / scale, Vector3.Dot(d, s.Toward) / scale);
        }
        /// <summary>主人公の点を毎フレーム渡す (Godot の BattleStage → AuthoredGeometryFx.UpdatePrototypeSource)。接触の瞬間に相手の原点を斧の届く縁へ寄せて固定する</summary>
        static void UpdateSource(Slot s, float age)
        {
            var e = s.Effect; var route = "card:" + s.Card;
            if (!Stage.TryGetUnitAnchor("player", out var hero)) return;
            if (Hd2dAttackPrototype.UsesCastRoute(route))
            {
                // Track the held focus while gathering, then freeze launch origin for continuous flight.
                if (age >= -(ElementalVfxProfiles.ForRoute(route)?.LaunchLead ?? .17f)) return;
                var point = FireSourcePoint(hero);
                e.ConfigureWeaponFrame?.Invoke(PoseFrame(hero));
                e.ConfigureSource?.Invoke(Local(s, point));
                return;
            }
            if (!Hd2dAttackPrototype.UsesWeaponRoute(route)) return;
            var axe = s.Remote ? hero.TexturePoint(hero.CurrentTex, new Vector2(.90f, .55f)) : AxeContactPoint(hero);
            if (!s.Remote && !s.Locked && age >= 0f)
            {
                // First contact belongs to the reachable near-side surface, not the center of a large enemy. Freeze it: the burst must not chase retreat.
                float nearSide = Vector3.Dot(axe - s.Target, s.Right);
                float bound = s.NearSurface.HasValue ? Mathf.Min(0f, Vector3.Dot(s.NearSurface.Value - s.Target, s.Right) - .04f * s.Height) : -.45f * s.Height;
                s.Target += s.Right * Mathf.Clamp(nearSide, bound, 0f);
                s.Locked = true;
            }
            e.ConfigureWeaponFrame?.Invoke(PoseFrame(hero));
            e.ConfigureWeaponTraceLength?.Invoke(hero.HeightWorld * .30f / Mathf.Max(.0001f, s.Height * s.Size));
            e.ConfigureWeaponOrigin?.Invoke(Local(s, s.Remote ? axe : WindupPoint(hero)));
            e.ConfigureSource?.Invoke(Local(s, axe));
        }
        static void Upload(Slot s, float age)
        {
            var e = s.Effect; float visualAge = e.VisualAge?.Invoke() ?? age;
            for (int j = 0; j < e.Batches.Length; j++)
            {
                var b = e.Batches[j]; var d = s.Draws[j];
                bool any = false;
                for (int k = 0; k < b.Count; k++) if (b.Alpha(k) > 1e-5f) { any = true; break; }
                d.Renderer.enabled = any;
                if (!any) continue;
                for (int i = 0; i < b.Count; i++)
                {
                    d.V[i] = WorldPoint(s, b, i);
                    HV2 uv = b.Uv(i); d.UV[i] = new Vector2(uv.X, uv.Y);
                    int r = Mathf.Clamp(b.Role(i), 0, 3); var role = s.Roles[r];
                    d.C[i] = new Color(role.r, role.g, role.b, Mathf.Clamp01(b.Alpha(i)));
                    d.Gain[i] = new Vector2(b.Gain(i), 0f);
                }
                d.Mesh.SetVertices(d.V); d.Mesh.SetUVs(0, d.UV); d.Mesh.SetUVs(1, d.Gain); d.Mesh.SetColors(d.C);
                Props.Clear();
                Props.SetFloat("_Age", visualAge); Props.SetFloat("_Kind", d.Kind); Props.SetFloat("_TextureMotion", d.Motion);
                bool flat = HD2DFlags.Hd2dFxFlat;   // 診断: hd2dfx=flat で絵を貼らず平らな四角で描く (層の位置と大きさを見る)
                Props.SetFloat("_TextureEnabled", d.Texture != null && !flat ? d.TextureEnabled : 0f); Props.SetFloat("_AuthoredRgba", flat ? 0f : d.AuthoredRgba); Props.SetFloat("_UseTexture", flat ? 0f : d.UseTexture);
                if (d.Texture != null) Props.SetTexture("_BaseMap", d.Texture);
                d.Renderer.SetPropertyBlock(Props);
            }
        }
        static void Hide(Slot s) { if (s.Draws == null) return; foreach (var d in s.Draws) if (d?.Renderer != null) d.Renderer.enabled = false; }
        static IEnumerator Life(Slot s, int v, int e)
        {
            try
            {
                while (Current(s, v, e))
                {
                    float age;
                    if (s.Landed) { age = Time.time - s.Started; if (age >= s.Tail) yield break; }
                    else { age = Time.time - s.Expected; if (age >= .35f) yield break; }
                    try { UpdateSource(s, age); s.Effect.Sample(age); Upload(s, age); }
                    catch (Exception ex) { Debug.LogWarning("[Hd2dAttackFx] " + ex.GetType().Name + ": " + ex.Message); yield break; }
                    yield return null;
                }
            }
            finally { if (e == _epoch) Retire(s, v); }
        }

        /// <summary>
        /// 当たり1つの演出を始める。contactAt があれば予備動作 (負の時刻) から、無ければ接触から。戻り値の Land() が接触の合図 (Presenter の数字と同じ瞬間)。
        /// Godot の AuthoredGeometryFx.Begin(route, scenario, source, target, feet, visibleHeight, cameraBasis, contactIndex) の写し
        /// </summary>
        public static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket t, float? contactAt = null)
        {
            if (!Eligible(t) || !t.HasWorld || t.Amount <= 0) return null;
            if (!Ready()) { t.RenderFallbackReason = LastFailure; return null; }
            float height = t.ContactVisibleHeight > 0f ? t.ContactVisibleHeight : t.Height;
            if (!(height > 0f) || !float.IsFinite(t.Feet.x)) { t.RenderFallbackReason = "hd2d_invalid_captured_geometry"; return null; }
            string card = t.SourceKey; var route = RouteOf(t);
            NativeEffect effect;
            try
            {
                if (!_cache.TryGetValue(route.Key, out effect)) { effect = Hd2dAttackPrototype.Create(route); if (effect != null) _cache[route.Key] = effect; }
            }
            catch (Exception ex) { t.RenderFallbackReason = "hd2d_effect_construction_failed:" + ex.GetType().Name; return null; }
            if (effect == null) { t.RenderFallbackReason = "hd2d_route_unsupported"; return null; }
            Slot s = null;
            try
            {
                s = Acquire(route.Key, effect);
                s.Ticket = t; s.Card = card; s.Busy = true;
                var visual = t.Visual; bool physical = PhysicalVfxProfiles.ForCard(card) != null;
                s.Height = Mathf.Max(.25f, height); s.Size = physical ? 1f : (visual?.Size ?? 1f); s.LightPower = visual?.Light ?? 1f;
                s.Tail = Hd2dAttackPrototype.Tail(route.Key);
                var cam = Stage.Camera.transform;
                s.Right = cam.right; s.Up = cam.up; s.Toward = -cam.forward;
                s.GroundRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized; s.GroundToward = Vector3.ProjectOnPlane(-cam.forward, Vector3.up).normalized;
                if (s.GroundRight.sqrMagnitude < 1e-6f) s.GroundRight = Vector3.right; if (s.GroundToward.sqrMagnitude < 1e-6f) s.GroundToward = Vector3.back;
                s.Feet = t.Feet; s.Target = t.Feet + s.Up * (s.Height * .5f);
                var palette = Palette(effect.Palette); s.Roles = palette; s.LightColor = palette[4];
                var plan = PlanFor(t.Target, card); s.Remote = plan?.Remote ?? false; s.NearSurface = plan?.Surface;
                s.Expected = contactAt ?? Time.time;
                // dot の BattleStage が Begin の直後に呼ぶ設定 (鏡像は当たりの番号で交互・粒の量・遠隔打ち・構えの身振り・種・経路)
                effect.ConfigureMirror?.Invoke(t.Index % 2 == 1);
                effect.ConfigureParticleGain?.Invoke(visual?.Particles ?? 1f);
                effect.ConfigureRemoteStrike?.Invoke(s.Remote);
                effect.ConfigureSourceGesture?.Invoke(true);
                effect.ConfigureVisualSeed?.Invoke(t.Feet.x * 1.71f + t.Feet.z * 2.13f + t.Index * 1.39f);
                effect.ConfigureResolvedPath?.Invoke(Array.Empty<HV3>(), Array.Empty<float>());
                if (card == "green_finisher_stomp" && effect.ConfigureStompTop != null)
                {
                    // Godot の ConfigurePrototypeViewport: 画面の上端の 12% 外の高さを、相手の足元を原点にした板の高さの単位で
                    var scr = Stage.Camera.WorldToScreenPoint(s.Feet);
                    var top = Stage.Camera.ScreenToWorldPoint(new Vector3(scr.x, Screen.height * 1.12f, scr.z));
                    effect.ConfigureStompTop(Vector3.Dot(top - s.Feet, s.Up) / (s.Height * s.Size));
                }
                if (Stage.TryGetUnitAnchor("player", out var hero))
                    effect.ConfigureSource?.Invoke(Local(s, Hd2dAttackPrototype.UsesCastRoute(route.Key) ? FireSourcePoint(hero) : s.Remote ? hero.TexturePoint(hero.CurrentTex, new Vector2(.90f, .55f)) : AxeContactPoint(hero)));
                else effect.ConfigureSource?.Invoke(new HV3(-1, 0, 0));
                var light = palette[4];
                s.LightSpec = new WorldBattleFx.AttackSpec(new[] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, .080f, s.Tail }, 1, 1, 1, light, light, 0f, 0f, 0f)
                { ContractLighting = true, StandardContractPower = true, TargetPeakMultiplier = effect.TargetPeak, FloorPeakMultiplier = effect.FloorPeak, MaxCombineGroup = "hd2d-prototype-r192", Envelope = age => Mathf.Max(0f, effect.Light()) };
                s.Root.gameObject.SetActive(true);
                int v = s.Version, e = _epoch; var captured = s;
                t.OutcomeIncomingReason = "hd2d_prototype_route";
                Debug.Log("[Hd2dAttackFx] begin card=" + card + " palette=" + effect.Palette + " batches=" + effect.Batches.Length + " height=" + s.Height.ToString("F2") + " remote=" + s.Remote + " lead=" + (s.Expected - Time.time).ToString("F3") + " tail=" + s.Tail.ToString("F2"));
                // 予備動作の音 (dot の BattleStage: 札の最初の当たりで一度)。当たりの音は Presenter が Hd2dContact.<札> で鳴らす
                if (t.Index == 0 && Hd2dAttackPrototype.HasAuthoredAudio(card)) Audio.Key("Hd2dOnset." + Hd2dAttackPrototype.AudioCard(card));
                var playback = new WorldBattleFx.AttackPlayback
                {
                    Contact = () =>
                    {
                        if (!Current(captured, v, e)) { StaleContacts++; return false; }
                        if (!Tween.SchedulingAllowed || !Stage.CombatWorld || Stage.Camera == null || !Stage.Camera.isActiveAndEnabled) { t.RenderFallbackReason = "hd2d_contact_environment_unavailable"; Retire(captured, v); return false; }
                        if (captured.Landed) return true;
                        try
                        {
                            captured.Landed = true; captured.Started = Time.time;
                            UpdateSource(captured, 0f); captured.Effect.Sample(0f); Upload(captured, 0f);
                            if (Stage.ImpactTargetCurrent(t.Target, t.Impact))
                            {
                                captured.Light = StageFx.BeginContractAttackLight(t.Target, t.Impact?.Target, e, captured.Target, captured.Feet, captured.Height, captured.LightSpec, 0, false,
                                    () => Current(captured, v, e) && Stage.ImpactTargetCurrent(t.Target, t.Impact), 1f, captured.LightPower, () => Time.time - captured.Started);
                                captured.Light?.Land();
                            }
                            t.AuthoredRenderer = RendererId; return true;
                        }
                        catch (Exception ex) { Retire(captured, v); t.RenderFallbackReason = "hd2d_contact_failed:" + ex.GetType().Name; return false; }
                    },
                    Read = () => Current(captured, v, e) ? new { attack = t.AttackFamily, renderer = RendererId, card = captured.Card, age = captured.Landed ? Time.time - captured.Started : Time.time - captured.Expected, landed = captured.Landed, remote = captured.Remote, height = captured.Height, size = captured.Size, version = v, serial = captured.Serial, visibleEnd = captured.Tail } : null
                };
                Tween.RunPresentation(Life(s, v, e));
                if (!Current(s, v, e)) { if (t.RenderFallbackReason == null) t.RenderFallbackReason = "hd2d_scope_cancelled"; return null; }
                Submitted++;
                return playback;
            }
            catch (Exception ex) { if (s != null) Retire(s, s.Version); t.RenderFallbackReason = "hd2d_construction_failed:" + ex.GetType().Name; return null; }
        }
    }
}
