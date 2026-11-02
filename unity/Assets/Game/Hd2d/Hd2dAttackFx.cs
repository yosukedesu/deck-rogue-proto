// Hd2dAttackFx.cs — HD-2D の攻撃演出の描き手 (dot の Godot 版 codex/unity-parity-20261013・checkpoint 192 の移植・2026-10-27。
// 2026-11-02 に Project422 (codex/godot-vfx422-unity-handoff-20261102) の全対象へ広げた)。
// Godot では AuthoredGeometryFx (MeshInstance3D + ShaderMaterial) と BattleStage/BattleActor が担っていた所を、Unity の既存の流れ
// (ResolvedCombatFx の Ticket → WorldBattleFx.BeginAuthoredAttack/AuthoredAttackContact → AttackPlayback.Land) に合わせて書いた。
// 幾何と時間は Hd2dAttackPrototype と Hd2dGeometryBridge (dot のコードそのまま) が組む。ここは
//   ①経路: 出来事 → 元の経路 (Hd2dRouteCatalog) → 門 (Hd2dOwnership) → 選ばれた経路。選ばれた経路が Hd2d の描き手なら受ける
//   ②頂点を世界へ写して Mesh に流す (層ごとにシェーダを選ぶ = Godot の ConfigureMaterial)
//   ③主人公の斧の先・杖の石の位置を毎フレーム渡す ④接触の光 (StageFx の契約の光) ⑤予備動作の音
//   ⑥「約束済みの当たり」(Configure*CommittedContact) は Land (数字が出る瞬間) で立てる
// 近似にした所 (Godot の BattleStage は 3D の俳優を持つが Unity は板): 踏み込みの距離は「斧の先が相手の縁に届く」まで、相手の縁は絵の不透明な画素、
// 途中に別の敵がいる時の遠隔打ちは体の前が相手の上半身に当たる手前で止める。光の方針 (SupportLightVfx391 ほか) は目標・床の光の量の倍率だけ写した (範囲は Unity の契約の光のまま)。
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using DeckRogue.Engine.Generated;
using DeckRogue.Game.Hd2d;
using HV3 = DeckRogue.Game.Hd2dMath.Vector3;
using HV2 = DeckRogue.Game.Hd2dMath.Vector2;
namespace DeckRogue.Game
{
    internal static class Hd2dAttackFx
    {
        public const string RendererId = "hd2d-prototype-r192";
        public const string RendererId422 = "hd2d-project422";
        sealed class Draw { public Mesh Mesh; public MeshRenderer Renderer; public Vector3[] V; public Vector2[] UV, Fx; public Color[] C; public Texture2D Texture, Lookup; public string Shader; public float Kind, Motion, TextureEnabled, AuthoredRgba, UseTexture, UseBeam, BluntAccent, Gain, Emission; }
        sealed class Slot
        {
            public Transform Root; public Draw[] Draws; public NativeEffect Effect; public string EffectKey, Card; public ResolvedCombatFx.Ticket Ticket;
            public AttackRoute Original, Route;
            public bool Busy, Landed, Locked, Remote, Support; public int Version; public long Serial;
            public float Started, Expected, Height, Size, Emission, Particles, LightPower, Tail;
            public Vector3 Target, Feet, Right, Up, Toward, GroundRight, GroundToward; public Vector3? NearSurface;
            public Color[] Roles; public Color LightColor; public StageFx.ContractLightPlayback Light; public WorldBattleFx.AttackSpec LightSpec;
            public string SourceUnit, TargetKey;
        }
        static readonly Slot[] Pool = new Slot[12]; static int _epoch; static long _serial; static readonly MaterialPropertyBlock Props = new MaterialPropertyBlock();
        static readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
        static readonly Dictionary<string, NativeEffect> _cache = new Dictionary<string, NativeEffect>();
        static readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Color[]> _palettes = new Dictionary<string, Color[]>();
        static readonly VertexAttributeDescriptor[] Layout = {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),      // 頂点の色は float (Godot の CUSTOM0 RGBA32F と同じ: 明るさ >1 を 8bit で潰さない)
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2),
        };
        public static string LastFailure { get; private set; } public static int Submitted { get; private set; } public static int Recycled { get; private set; } public static int StaleContacts { get; private set; }
        public static int ActiveCount { get { int n = 0; foreach (var s in Pool) if (s != null && s.Busy) n++; return n; } }

        // ---- 札 id で受けるか (登録は PhysicalVfxProfiles / ElementalVfxProfiles = dot の表そのまま) ----
        public static bool Supports(string cardId) => HD2DFlags.Hd2dFx && !string.IsNullOrEmpty(cardId) && Hd2dAttackPrototype.Supports("card:" + cardId);
        /// <summary>この当たりを HD-2D の描き手が受けるか = 元の経路に門を通した結果が Hd2d の描き手 (登録のある札・置物・亡骸・仕込み札・ギア・敵の技)</summary>
        public static bool Eligible(ResolvedCombatFx.Ticket t) => t != null && t.Event == "DamageDealt" && t.Outcome == "hit" && HD2DFlags.Hd2dFx && TryRoutes(t, out _, out _);
        static readonly Dictionary<ResolvedCombatFx.Ticket, (AttackRoute original, AttackRoute selected, bool ok)> _routeMemo = new Dictionary<ResolvedCombatFx.Ticket, (AttackRoute, AttackRoute, bool)>();
        static bool TryRoutes(ResolvedCombatFx.Ticket t, out AttackRoute original, out AttackRoute selected)
        {
            original = null; selected = null;
            if (t == null) return false;
            if (_routeMemo.TryGetValue(t, out var memo)) { original = memo.original; selected = memo.selected; return memo.ok; }
            if (_routeMemo.Count > 256) _routeMemo.Clear();
            bool ok = false;
            try
            {
                if (t.SourceEvent is GameEvent_DamageDealt)
                {
                    var owner = Hd2dOwner.From(t);
                    if (owner != null)
                    {
                        original = Hd2dOwnership.Resolve(owner);
                        if (original.SourceKind == "system" && t.SourceKind == "card" && Supports(t.SourceKey))
                            original = new AttackRoute("card:" + t.SourceKey, t.SourceKey, "card", t.AttackFamily ?? "", "Hd2dPrototype", t.Element ?? "physical", false, "ticket card route (catalog miss)");
                        selected = Hd2dOwnership.Select(original, owner);
                        ok = Hd2dOwnership.IsHd2dRenderer(selected);
                    }
                }
                else if (t.SourceKind == "card" && Supports(t.SourceKey) && t.Target != "player")
                {
                    // 出来事の無い券 (撮影の fx= など): 札の登録だけで受ける (checkpoint 192 と同じ)
                    original = selected = Hd2dRouteCatalog.ResolveKey("card:" + t.SourceKey);
                    if (!Hd2dAttackPrototype.SupportsRoute(selected)) selected = new AttackRoute("card:" + t.SourceKey, t.SourceKey, "card", t.AttackFamily ?? "", RendererId, t.Element ?? "physical", false, "ported from codex/unity-parity-20261013 checkpoint 192");
                    ok = Hd2dAttackPrototype.SupportsRoute(selected);
                }
            }
            catch (Exception ex) { Debug.LogWarning("[Hd2dAttackFx] route: " + ex.GetType().Name + ": " + ex.Message); ok = false; }
            _routeMemo[t] = (original, selected, ok);
            return ok;
        }
        public static float Lead(ResolvedCombatFx.Ticket t)
        {
            if (!Eligible(t) || !TryRoutes(t, out var original, out var selected)) return 0f;
            return Hd2dAttackPrototype.SupportsRoute(selected) ? Hd2dAttackPrototype.Lead(selected.Key) : 0f;   // 選ばれた経路の当たりは接触だけ (Godot の beat は元の経路の時計を持つが、予備動作は元の描き手が出さない)
        }
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
        public static Vector3 AxeContactPoint(Stage.UnitAnchor a) => FrameOrBody(a, 2, AxeUv(ArtKey(a.ArtName)), new Vector2(.90f, .55f));
        public static Vector3 WindupPoint(Stage.UnitAnchor a) => FrameOrBody(a, 1, WindupUv(ArtKey(a.ArtName)), new Vector2(.35f, .40f));
        public static Vector3 FireSourcePoint(Stage.UnitAnchor a) => FrameOrBody(a, 3, FocusUv(ArtKey(a.ArtName)), new Vector2(.98f, .50f));
        static int PoseFrame(Stage.UnitAnchor a) => a != null && a.Anim == "attack" ? a.Frame : 0;

        // ---- 踏み込みの計画 (Godot の BattleStage.PrototypeStrikeApproach の写し) ----
        sealed class ApproachPlan { public string Card, Target; public bool Remote; public Vector3? Surface; public float At; }
        static readonly Dictionary<string, ApproachPlan> _plans = new Dictionary<string, ApproachPlan>();
        public static float[] PoseDurations(string cardId, bool remote)
        {
            bool weapon = UsesWeaponContact(cardId), fire = UsesFire(cardId);
            // Project422 (BattleActor.TorrentReleaseHold): 光の奔流は放つコマ (4 コマ目) を奔流が終わるまで (当たり +.10 秒) 保つ
            if (cardId == "white_light_torrent") return remote ? new[] { .06f, .04f, .075f, .42f } : new[] { .10f, .14f, .055f, .42f };
            return remote ? new[] { .06f, .04f, .075f, .18f } : new[] { .10f, fire ? .14f : weapon ? .075f : .12f, weapon ? .100f : .055f, .18f };
        }
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
                    bool nearIsLeft = Vector3.Dot(dir, foe.Right) > 0f;
                    bool found = Stage.TryUnitSurface(foe, axe.y, nearIsLeft, out var s);
                    if (found)
                    {
                        surface = s;
                        float contactTravel = Mathf.Max(0f, Vector3.Dot(s - axe, dir));
                        float safeTravel = contactTravel;
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
            return MaterialOf("Hd2dPrototype") != null || Fail("hd2d_shader_unavailable");
        }
        static bool Fail(string why) { LastFailure = why; return false; }
        /// <summary>シェーダ名 (Godot の res://shaders/authored_&lt;name&gt;.gdshader) → Unity の DeckRogue/&lt;name&gt;。無ければ null</summary>
        static Material MaterialOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_materials.TryGetValue(name, out var m) && m != null) return m;
            var shader = Shader.Find("DeckRogue/" + name);
            if (shader == null || !shader.isSupported) { Debug.LogWarning("[Hd2dAttackFx] シェーダが無い: DeckRogue/" + name); _materials[name] = null; return null; }
            m = new Material(shader); _materials[name] = m; return m;
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
        static JObject _defaults;
        /// <summary>色の表に役割が無い時の既定 (Godot の LoadRoles の既定値。Data/hd2d-palette-defaults.json。コードに 16 進は書かない)</summary>
        static string Default(params string[] path) { if (_defaults == null) { var a = Resources.Load<TextAsset>("Data/hd2d-palette-defaults"); _defaults = a != null ? JObject.Parse(a.text) : new JObject(); } JToken t = _defaults; foreach (var k in path) t = t?[k]; return (string)t; }
        static Color Srgb(string s, string fallback) { Color c; if (string.IsNullOrEmpty(s) || !ColorUtility.TryParseHtmlString(s, out c)) { if (string.IsNullOrEmpty(fallback) || !ColorUtility.TryParseHtmlString(fallback, out c)) c = Color.gray; } return c.linear; }
        /// <summary>色の表 Data/&lt;palette&gt;.json (Godot の AuthoredGeometryFx.LoadRoles の写し): roles_srgb / profiles[family] / families[family] / surfaces[Surface]。[4] が光の色</summary>
        static Color[] Palette(NativeEffect e)
        {
            string key = e.Palette + "|" + e.RouteFamily + "|" + e.Shader + "|" + e.Family;
            Color[] roles;
            if (_palettes.TryGetValue(key, out roles)) return roles;
            var asset = Resources.Load<TextAsset>("Data/" + e.Palette);
            JToken obj = asset != null ? JObject.Parse(asset.text) : new JObject();
            var profiles = obj["profiles"]; if (profiles != null) obj = profiles[e.RouteFamily] ?? throw new InvalidOperationException("Missing authored palette profile: " + e.RouteFamily);
            var families = obj["families"]; if (families != null) obj = families[e.RouteFamily] ?? throw new InvalidOperationException("Missing authored palette family: " + e.RouteFamily);
            if (e.Shader == "EarthStompSurfaceR2") obj = obj[(e.Family == 0) ? "earth" : "stomp"] ?? obj;
            roles = new Color[5];
            if (e.Shader == "BoundedContactFx" || e.Shader == "ColdSweepSurfaceFx")
            {
                string[] surfaces = { "Neutral", "Water", "Cold", "Wood", "Plant", "Fire", "Smoke", "Dark" };
                var surface = obj["surfaces"]?[surfaces[Mathf.Clamp(e.Family, 0, 7)]];
                roles[0] = Srgb(Default("bounded", "shadow"), null); roles[1] = Srgb((string)surface?["primary"], Default("bounded", "primary")); roles[2] = roles[1]; roles[3] = Srgb((string)surface?["core"], Default("bounded", "core")); roles[4] = Color.white;
            }
            else
            {
                string[] names = { "shadow", "primary", "edge", "core" };
                for (int i = 0; i < 4; i++) roles[i] = Srgb((string)obj["roles_srgb"]?[names[i]], Default("roles_srgb", names[i]));
                roles[4] = Srgb((string)obj["scene_light_srgb"], Default("scene_light_srgb"));
            }
            _palettes[key] = roles;
            return roles;
        }
        /// <summary>Godot の ConfigureMaterial の _TextureMotion の表 (絵の名前と層の種類 → シェーダの動きの番号)。数値はそのまま (Project422: 33 = 所有の軸の突き)</summary>
        static float TextureMotion(string tex, int kind, bool ownedAxial) => ownedAxial && tex == "physical-pierce-surface-r1" ? 33f : tex == "lightning-discharge-surface-r2" ? 32f : tex.StartsWith("hd2d-wood-contact-") ? 31f : tex.StartsWith("hd2d-waterwide-phase-") ? 30f : tex == "earth-ground-grain-r2" ? 28f : tex.StartsWith("hd2d-dark-contraction-") ? 25f : tex == "ice-crystal-surface-r2" ? (kind == 11 ? 21f : kind == 12 ? 22f : 0f) : tex.StartsWith("hd2d-water-impact-") ? (tex.EndsWith("03") ? 20f : 18f) : tex == "water-flow-surface-r1" ? 19f : tex.StartsWith("hd2d-stomp-aura-") ? 17f : tex == "hd2d-stomp-pressure-v1" ? 14f : tex.StartsWith("hd2d-fire-volume-") ? (kind == 15 ? 23f : (tex.EndsWith("03") || tex.EndsWith("04")) ? 13f : 12f) : tex.StartsWith("hd2d-fire-projectile-") ? 1f : tex == "hd2d-stomp-shin-v4" ? 9f : tex == "hd2d-fire-impact-v1" ? 2f : tex == "hd2d-earth-rock-v1" ? 10f : tex == "hd2d-earth-impact-v1" ? 3f : tex == "hd2d-earth-dust-v1" ? 4f : tex == "hd2d-physical-slash-v1" ? (kind == 17 ? 26f : kind == 18 ? 27f : kind == 9 ? 16f : 15f) : tex == "hd2d-earth-foot-v1" ? 6f : (tex == "hd2d-fire-impact-phase-03" || tex == "hd2d-fire-impact-phase-04") ? 8f : tex.StartsWith("hd2d-fire-impact-phase-") ? 7f : 0f;

        /// <summary>層ごとのシェーダ (Godot の ConfigureMaterial の分岐をそのまま)。Project422 の「表面」シェーダは層の種類 (Kind) と経路で選ぶ</summary>
        static string ShaderFor(AttackRoute route, NativeEffect e, NativeBatch b)
        {
            string shader = (e.Shader == "EarthStompSurfaceR2" && b.Kind == 0) ? "EarthStompRockR2" : e.Shader;
            if (BlueContactVfx379Routes.SelectedRoute(route) && e.Shader == "BoundedContactFx" && b.Kind == 91) shader = "BlueContact379Surface";
            if (MomentumWhipContactVfxProfiles.SelectedRoute(route) && e.Shader == "BoundedContactFx" && b.Kind == 71 && b.Space == 0) shader = "MomentumWhipContactSurface";
            if (GreenGulpContactVfxProfiles.SelectedRoute(route) && (b.Kind == 91 || b.Kind == 92) && b.Space == 0) shader = "GreenGulpContactSurface";
            if (SporeContactVfx385.SelectedRoute(route) && b.Kind == 101 && b.Space == 0) shader = "Spore385ContactSurface";
            if (CultistDarkBladeContactVfxProfiles.SelectedRoute(route) && e.Shader == "BoundedContactFx" && b.Kind == 81 && b.Space == 0) shader = "CultistDarkBladeContactSurface";
            if (SparkBurstContactVfxProfiles.SelectedRoute(route) && e.Shader == "ScatterProfileFx" && b.Kind == 61 && b.Space == 0) shader = "SparkBurstContactSurface";
            if (AetherLanceContactVfxProfiles.SelectedRoute(route) && e.Shader == "BoundedContactFx" && b.Kind == 51 && b.Space == 0) shader = "AetherLanceContactSurface";
            if (VineFormationCounterVfxProfiles.SelectedRoute(route) && e.Shader == "BoundedContactFx" && b.Kind == 41 && b.Space == 0) shader = "VineFormationCounterSurface";
            //415 only these exact owners use the neutral contrast surface; generic BoundedContact, private Crossbow and all other object aliases retain theirs.
            if (ObjectContact415Routes.ObjectContact415Route(route) && e.Shader == "BoundedContactFx" && b.Space == 0) shader = "ObjectContact415Surface";
            if (CrossbowAimedContactVfxProfiles.SelectedRoute(route) && e.Shader == "BoundedContactFx" && e.Palette == "bounded-contact-palette-r1" && b.Kind == 0 && b.Space == 0) shader = "CrossbowAimedSurface";
            if (VineGraspContactVfxProfiles.SelectedRoute(route) && e.Shader == "BoundedContactFx" && e.Palette == "bounded-contact-palette-r1" && b.Kind == 0 && b.Space == 0) shader = "VineGraspSurface";
            if (WraithCurseContactVfxProfiles.SelectedRoute(route) && e.Shader == "BoundedContactFx" && e.Palette == "bounded-contact-palette-r1" && b.Kind == 0 && b.Space == 0) shader = "WraithCurseSurface";
            if (LightBurstContactVfxProfiles.SelectedRoute(route) && e.Shader == "LocalMaterialProfileFx" && e.Palette == "local-material-palette-r1" && b.Space == 0 && (b.Kind == 41 || b.Kind == 42)) shader = "LightBurstOpticalSurface";
            if (LightBoltContactVfxProfiles.SelectedRoute(route) && e.Shader == "Hd2dPrototype" && e.Palette == "hd2d-light" && b.Space == 0 && (b.Kind == 41 || b.Kind == 42)) shader = "LightBoltOpticalSurface";
            if (PermanentContactVfxProfiles.CanopyRoute(route) && e.Shader == "Hd2dPrototype" && e.Palette == "hd2d-vegetal-cut" && b.Kind == 40 && b.Space == 0) shader = "Hd2dCanopySurface";
            // A local material opt-in, never a shared Kind33 or Light-family replacement.
            if (route.Key == "card:white_light_torrent" && route.DefinitionId == "white_light_torrent" && route.SourceKind == "card" && route.Family == "light" && route.Renderer == "LightRayFx"
                && route.Element == "light" && !route.FixedSmall && e.Shader == "Hd2dPrototype" && e.Palette == "hd2d-light" && b.Kind == 33 && b.Space == 0 && b.Texture == "hd2d-light-torrent-density-v1") shader = "Hd2dOpticalFlow";
            // Exact necro material opt-in; normal hand-play Kiss and shared Dark stay on the original shader.
            if (route.Key == "card:black_corpse_kiss" && route.DefinitionId == "black_corpse_kiss" && route.SourceKind == "necro" && route.Family == "dark_drain" && route.Renderer == "Hd2dNecroContactFx"
                && route.Element == "dark" && route.FixedSmall && e.Shader == "Hd2dPrototype" && e.Palette == "hd2d-dark" && b.Kind == 16 && b.Space == 0 && b.Texture == "hd2d-necro-kiss-density-v1") shader = "Hd2dKissDensity";
            return shader;
        }

        // ---- 枠の確保と片付け ----
        static void DestroyDraws(Slot s) { if (s?.Draws == null) return; foreach (var d in s.Draws) if (d?.Mesh != null) UnityEngine.Object.Destroy(d.Mesh); foreach (Transform c in s.Root) UnityEngine.Object.Destroy(c.gameObject); s.Draws = null; }
        static void Retire(Slot s, int v) { if (s.Version != v) return; s.Busy = false; s.Light?.Cancel(); s.Light = null; s.Ticket = null; if (s.Root != null) s.Root.gameObject.SetActive(false); }
        public static void Cancel()
        {
            _epoch++; foreach (var s in Pool) if (s != null) { Retire(s, s.Version); s.Version++; }
            _routeMemo.Clear();
        }
        static Slot Acquire(string key, NativeEffect effect, AttackRoute route)
        {
            int limit = GfxQuality.Level == GfxLevel.Low ? 6 : 12; Slot s = null;
            for (int i = 0; i < limit; i++) { if (Pool[i] == null) Pool[i] = new Slot(); if (!Pool[i].Busy || Pool[i].Root == null) { s = Pool[i]; break; } }
            if (s == null) { s = Pool[0]; for (int i = 1; i < limit; i++) if (Pool[i].Serial < s.Serial) s = Pool[i]; Recycled++; }
            Retire(s, s.Version); s.Version++; s.Serial = ++_serial;
            if (s.Root == null) { s.Root = new GameObject("hd2d-fx").transform; }
            s.Root.position = Vector3.zero; s.Root.rotation = Quaternion.identity; s.Root.localScale = Vector3.one;
            if (s.EffectKey != key || s.Draws == null || s.Draws.Length != effect.Batches.Length) { DestroyDraws(s); s.Draws = new Draw[effect.Batches.Length]; s.EffectKey = key; }
            s.Effect = effect; s.Landed = false; s.Locked = false; s.Light = null; s.Support = false;
            bool ownedAxial = AxialFamilyContactVfxProfiles.Selected(route);
            for (int j = 0; j < effect.Batches.Length; j++)
            {
                var b = effect.Batches[j];
                var d = s.Draws[j];
                if (d == null || d.V.Length != b.Count)
                {
                    if (d?.Mesh != null) UnityEngine.Object.Destroy(d.Mesh);
                    var go = new GameObject("hd2d-batch-" + j); go.transform.SetParent(s.Root, false);
                    var mesh = new Mesh { name = "hd2d-fx-" + j }; mesh.MarkDynamic();
                    d = new Draw { Mesh = mesh, V = new Vector3[b.Count], UV = new Vector2[b.Count], Fx = new Vector2[b.Count], C = new Color[b.Count] };
                    // 頂点は 3 の倍数ずつ使う (Layer.Quad=6・Taper=3)。Godot の ArrayMesh は 512 個の添字をそのまま受けたが、Unity は 3 の倍数でないと
                    // "Failed setting triangles" で三角形が空になる (2026-10-27 の撮影で層が一枚も描かれなかった原因)
                    int triCount = b.Indices.Length - b.Indices.Length % 3; var tri = new int[triCount]; Array.Copy(b.Indices, tri, triCount);
                    mesh.SetVertexBufferParams(b.Count, Layout);
                    mesh.SetVertices(d.V); mesh.SetColors(d.C); mesh.SetUVs(0, d.UV); mesh.SetUVs(1, d.Fx); mesh.triangles = tri;
                    mesh.bounds = new Bounds(Vector3.zero, new Vector3(64, 64, 64));
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    d.Renderer = go.AddComponent<MeshRenderer>(); d.Renderer.sortingOrder = j;
                    d.Renderer.shadowCastingMode = ShadowCastingMode.Off; d.Renderer.receiveShadows = false; d.Renderer.enabled = false;
                    s.Draws[j] = d;
                }
                string tex = b.Texture.Length > 0 ? b.Texture : effect.Texture;
                // Godot の ConfigureMaterial: PlayerContactFx は層の種類で絵を決める
                if (effect.Shader == "PlayerContactFx") tex = b.Kind == 1 ? "physical-pierce-surface-r1" : b.Kind == 2 ? "physical-meteor-intact-r1" : "enemy-blunt-contact-r1";
                d.Shader = ShaderFor(route, effect, b);
                var mat = MaterialOf(d.Shader) ?? MaterialOf(effect.Shader) ?? MaterialOf("Hd2dPrototype");
                d.Renderer.sharedMaterial = mat; d.Renderer.enabled = false;
                d.Texture = TextureOf(tex); d.Lookup = TextureOf(effect.Lookup);
                d.Kind = b.Kind;
                d.TextureEnabled = tex.Length > 0 ? 1f : 0f;
                d.AuthoredRgba = b.Texture.Length > 0 ? 1f : 0f;
                float useTexture = effect.Shader == "PhysicalSlashFx" ? (b.Kind == 0 ? 1f : 0f) : effect.Shader == "VineSlashFx" ? (j < 2 ? 1f : 0f) : (j == 0 ? 1f : 0f);
                d.UseTexture = b.Texture.Length > 0 ? 1f : useTexture;
                d.Motion = TextureMotion(tex, b.Kind, ownedAxial);
                d.UseBeam = effect.RouteFamily == "enemy_beam" && j == 0 ? 1f : 0f;
                d.BluntAccent = effect.RouteFamily == "enemy_blunt" ? 1f : 0f;
                d.Gain = 1f; d.Emission = 1f;   // Begin で _emission / _particles が決まった後に再計算 (Godot の ConfigureMaterial)
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
        /// <summary>主人公の点を毎フレーム渡す (Godot の BattleStage → AuthoredGeometryFx.UpdatePrototypeSource)。登録のある札だけ (選ばれた当たりは出所を Begin で 1 度渡す)</summary>
        static void UpdateSource(Slot s, float age)
        {
            var e = s.Effect; var route = s.Route;
            if (e.Shader != "Hd2dPrototype" || !Hd2dAttackPrototype.SupportsRoute(route)) return;
            if (!Stage.TryGetUnitAnchor("player", out var hero)) return;
            if (Hd2dAttackPrototype.UsesCastRoute(route.Key))
            {
                // Track the held focus while gathering, then freeze launch origin for continuous flight.
                if (age >= -(ElementalVfxProfiles.ForRoute(route.Key)?.LaunchLead ?? .17f)) return;
                var point = FireSourcePoint(hero);
                e.ConfigureWeaponFrame?.Invoke(PoseFrame(hero));
                e.ConfigureSource?.Invoke(Local(s, point));
                return;
            }
            if (!Hd2dAttackPrototype.UsesWeaponRoute(route.Key)) return;
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
        /// <summary>Godot の VertexColor: 役割の色 × 明るさ (× 発光/粒の倍率)。Hd2dPrototype は役割の色だけ (明るさは uv1.x)</summary>
        static Color VertexColor(Slot s, NativeBatch b, int i, int batch, out float gainOut)
        {
            var e = s.Effect; int r = Mathf.Clamp(b.Role(i), 0, 3); Color role = Color.white; float gain = b.Gain(i) * s.Emission;
            switch (e.Shader)
            {
                case "Hd2dPrototype": role = s.Roles[r]; gain = b.Gain(i); break;
                case "PhysicalSlashFx": role = b.Kind == 0 ? Color.white : s.Roles[3]; break;
                case "WaterContinuousFx": role = b.Kind == 1 ? s.Roles[3] : Color.white; gain = b.Gain(i) * (b.Kind == 2 ? s.Particles : s.Emission); break;
                case "FireContinuousFx": if (b.Kind == 3) { role = s.Roles[r]; gain = b.Gain(i) * s.Particles; } break;
                case "DarkDrainFx": case "IceCrystalFx": if (b.Kind == 2) { role = s.Roles[r]; gain = b.Gain(i) * s.Particles; } break;
                case "LightRayFx": if (b.Kind == 3) { role = s.Roles[r]; gain = b.Gain(i) * s.Particles; } break;
                case "PhysicalContactFx": role = batch == 0 ? Color.white : s.Roles[Mathf.Clamp(r + 1, 1, 3)]; gain = b.Gain(i); break;
                case "PlayerContactFx": role = b.Kind == 0 ? s.Roles[r] : Color.white; gain = b.Gain(i) * (b.Particle(i) ? s.Particles : s.Emission); break;
                case "LightningDischargeFx": gain = b.Kind == 2 ? s.Particles : s.Emission; break;
                case "HealContinuousFx": role = b.Kind == 0 ? Color.white : s.Roles[r]; break;
                case "SystemOutcomeFx": role = (b.Kind == 0 || b.Kind == 7) ? s.Roles[Mathf.Clamp(r + 1, 1, 3)] : Color.white; gain = b.Gain(i); break;
                case "VineSlashFx": gain = 1f; break;
            }
            gainOut = gain;
            return new Color(role.r * gain, role.g * gain, role.b * gain, Mathf.Clamp01(b.Alpha(i)));
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
                bool proto = e.Shader == "Hd2dPrototype" || d.Shader == "Hd2dKissDensity" || d.Shader == "Hd2dOpticalFlow" || d.Shader == "Hd2dCanopySurface" || d.Shader == "LightBoltOpticalSurface";
                for (int i = 0; i < b.Count; i++)
                {
                    d.V[i] = WorldPoint(s, b, i);
                    HV2 uv = b.Uv(i); d.UV[i] = new Vector2(uv.X, uv.Y);
                    var c = VertexColor(s, b, i, j, out float gain);
                    if (proto)
                    {
                        // Hd2dPrototype 系: 色は役割 (線形)・明るさは uv1.x (HDR)。Godot の CUSTOM0 = 役割×明るさ と同じ積になる
                        int r = Mathf.Clamp(b.Role(i), 0, 3); var role = s.Roles[r];
                        d.C[i] = new Color(role.r, role.g, role.b, Mathf.Clamp01(b.Alpha(i)));
                        d.Fx[i] = new Vector2(b.Gain(i), 0f);
                    }
                    else
                    {
                        d.C[i] = c;
                        HV2 fx = b.Effect(i); d.Fx[i] = new Vector2(fx.X, fx.Y);
                    }
                }
                d.Mesh.SetVertices(d.V); d.Mesh.SetColors(d.C); d.Mesh.SetUVs(0, d.UV); d.Mesh.SetUVs(1, d.Fx);
                Props.Clear();
                Props.SetFloat("_Age", visualAge); Props.SetFloat("_VisualAge", visualAge); Props.SetFloat("_Kind", d.Kind); Props.SetFloat("_TextureMotion", d.Motion);
                Props.SetFloat("_Family", e.Family); Props.SetFloat("_Mode", e.Family); Props.SetFloat("_Coverage", 1f);
                Props.SetFloat("_Gain", d.Gain); Props.SetFloat("_Emission", d.Emission); Props.SetFloat("_UseBeam", d.UseBeam); Props.SetFloat("_BluntAccent", d.BluntAccent);
                bool flat = HD2DFlags.Hd2dFxFlat;   // 診断: hd2dfx=flat で絵を貼らず平らな四角で描く (層の位置と大きさを見る)
                Props.SetFloat("_TextureEnabled", d.Texture != null && !flat ? d.TextureEnabled : 0f); Props.SetFloat("_AuthoredRgba", flat ? 0f : d.AuthoredRgba); Props.SetFloat("_UseTexture", flat ? 0f : d.UseTexture);
                Props.SetVector("_RoleShadow", s.Roles[0]); Props.SetVector("_RolePrimary", s.Roles[1]); Props.SetVector("_RoleEdge", s.Roles[2]); Props.SetVector("_RoleCore", s.Roles[3]);
                Props.SetVector("_Primary", s.Roles[1]); Props.SetVector("_Core", s.Roles[3]);
                if (d.Texture != null) { Props.SetTexture("_BaseMap", d.Texture); Props.SetTexture("_BodyMap", d.Texture); Props.SetTexture("_GrainTex", d.Texture); }
                if (d.Lookup != null) Props.SetTexture("_LookupMap", d.Lookup);
                if (e.Shader == "EarthStompSurfaceR2") { Props.SetFloat("_Front", e.Front()); Props.SetFloat("_Coverage", d.Kind < 0 ? e.Coverage() : 1f); Props.SetFloat("_Kind", d.Kind >= 0 ? (d.Kind == 1 ? 1 : 2) : 0); }
                d.Renderer.SetPropertyBlock(Props);
            }
        }
        static IEnumerator Life(Slot s, int v, int e)
        {
            try
            {
                while (Current(s, v, e))
                {
                    float age;
                    if (s.Landed) { age = Time.time - s.Started; if (age >= s.Tail) yield break; }
                    else { age = Time.time - s.Expected; if (age >= .35f) yield break; }
                    try { UpdateSource(s, age); s.Effect.Sample(age); Upload(s, age); if (HD2DFlags.Hd2dFxDiag && s.Landed && age < .12f) Diag(s, age); }
                    catch (Exception ex) { Debug.LogWarning("[Hd2dAttackFx] " + ex.GetType().Name + ": " + ex.Message); yield break; }
                    yield return null;
                }
            }
            finally { if (e == _epoch) Retire(s, v); }
        }
        /// <summary>診断 (hd2dfx=diag): Sample→Upload の直後に、層ごとの α の最大・頂点の画面上の広がり・シェーダ・絵を 1 行で (dot の助言 2026-11-02)</summary>
        static void Diag(Slot s, float age)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[Hd2dAttackFx] diag route=").Append(s.Route.Key).Append(" age=").Append(age.ToString("F3")).Append(" emission=").Append(s.Emission.ToString("F2")).Append(" height=").Append(s.Height.ToString("F2"));
            for (int j = 0; j < s.Effect.Batches.Length; j++)
            {
                var b = s.Effect.Batches[j]; var d = s.Draws[j];
                float maxA = 0f; int lit = 0; float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
                for (int i = 0; i < b.Count; i++)
                {
                    float a = b.Alpha(i); if (a > maxA) maxA = a; if (a <= 1e-5f) continue; lit++;
                    var sp = Stage.Camera != null ? Stage.Camera.WorldToScreenPoint(d.V[i]) : Vector3.zero;
                    if (sp.x < minX) minX = sp.x; if (sp.x > maxX) maxX = sp.x; if (sp.y < minY) minY = sp.y; if (sp.y > maxY) maxY = sp.y;
                }
                sb.Append(" | batch").Append(j).Append(" kind=").Append(b.Kind).Append(" shader=").Append(d.Shader).Append(" mat=").Append(d.Renderer.sharedMaterial != null ? d.Renderer.sharedMaterial.shader.name : "null")
                  .Append(" tex=").Append(d.Texture != null ? d.Texture.name : "-").Append(" enabled=").Append(d.Renderer.enabled).Append(" maxAlpha=").Append(maxA.ToString("F3")).Append(" lit=").Append(lit).Append("/").Append(b.Count);
                if (lit > 0) sb.Append(" screen=[").Append(minX.ToString("F0")).Append(",").Append(minY.ToString("F0")).Append("]-[").Append(maxX.ToString("F0")).Append(",").Append(maxY.ToString("F0")).Append("]");
            }
            Debug.Log(sb.ToString());
        }
        /// <summary>約束済みの当たり (Godot の Set*CommittedContact): 本物の受領が立った瞬間 (Land) に、選ばれた経路の分だけ true</summary>
        static void Commit(NativeEffect e, AttackRoute route, bool committed)
        {
            e.ConfigureBlueCommittedContact?.Invoke(BlueContactVfx379Routes.SelectedRoute(route) && committed);
            e.ConfigureMirrorCommittedContact?.Invoke(MirrorContactVfxProfiles.SelectedRoute(route) && committed);
            e.ConfigureImpSparkCommittedContact?.Invoke(ImpSparkContactVfxProfiles.SelectedRoute(route) && committed);
            e.ConfigureColdSweepCommittedContact?.Invoke(ColdSweepContactVfxProfiles.SelectedRoute(route) && committed);
            e.ConfigureCanopyCommittedContact?.Invoke(PermanentContactVfxProfiles.CanopyRoute(route) && committed);
            e.ConfigureLightBoltCommittedContact?.Invoke(LightBoltContactVfxProfiles.SelectedRoute(route) && committed);
            e.ConfigureLightBurstCommittedContact?.Invoke(LightBurstContactVfxProfiles.SelectedRoute(route) && committed);
            e.ConfigureVineFormationCounterCommittedContact?.Invoke(VineFormationCounterVfxProfiles.SelectedRoute(route) && committed);
            e.ConfigureAetherLanceCommittedContact?.Invoke(AetherLanceContactVfxProfiles.SelectedRoute(route) && committed);
            e.ConfigureSparkBurstCommittedContact?.Invoke(SparkBurstContactVfxProfiles.SelectedRoute(route) && committed);
            e.ConfigureMomentumWhipCommittedContact?.Invoke(MomentumWhipContactVfxProfiles.SelectedRoute(route) && committed);
            e.ConfigureCultistDarkBladeCommittedContact?.Invoke(CultistDarkBladeContactVfxProfiles.SelectedRoute(route) && committed);
            e.ConfigureGreenGulpCommittedContact?.Invoke(GreenGulpContactVfxProfiles.SelectedRoute(route) && committed);
            e.ConfigureSpore385CommittedContact?.Invoke(SporeContactVfx385.SelectedRoute(route) && committed);
        }
        static Hd2dEffectTier TierOf(string tier) => tier == "large" ? Hd2dEffectTier.Large : tier == "medium" ? Hd2dEffectTier.Medium : Hd2dEffectTier.Small;
        static string CacheKey(AttackRoute route, Hd2dEffectTier tier) => Hd2dRouteCatalog.Identity(route) + "/" + route.Key + "/" + route.Renderer + "/" + route.Family + "/" + tier;

        /// <summary>
        /// 当たり1つの演出を始める。contactAt があれば予備動作 (負の時刻) から、無ければ接触から。戻り値の Land() が接触の合図 (Presenter の数字と同じ瞬間)。
        /// Godot の AuthoredGeometryFx.Begin(route, scenario, source, target, feet, visibleHeight, cameraBasis, contactIndex) の写し
        /// </summary>
        public static WorldBattleFx.AttackPlayback Begin(ResolvedCombatFx.Ticket t, float? contactAt = null)
        {
            if (!Eligible(t) || !TryRoutes(t, out var original, out var route)) return null;
            return BeginRoute(t, original, route, contactAt, false);
        }
        /// <summary>延焼のティック (system:burn)。Godot の StatusContactVfxProfiles が選んだ時だけ受ける</summary>
        public static WorldBattleFx.AttackPlayback BeginSystem(ResolvedCombatFx.Ticket t)
        {
            if (t == null || !HD2DFlags.Hd2dFx || t.SourceEvent is not GameEvent_BurnTick) return null;
            var route = Hd2dOwnership.SelectBurn(t.SourceEvent);
            if (!StatusContactVfxProfiles.SelectedRoute(route)) return null;
            return BeginRoute(t, Hd2dRouteCatalog.BurnSystem(), route, null, false);
        }
        /// <summary>白の大槌の回復 (system:heal → system:holy-maul-owned-heal)。回復の券 (Outcome heal) で、同じコマンドが white_holy_maul のプレイの時だけ</summary>
        public static WorldBattleFx.AttackPlayback BeginHeal(ResolvedCombatFx.Ticket t)
        {
            if (t == null || !HD2DFlags.Hd2dFx || t.Outcome != "heal" || t.Amount <= 0 || t.Target != "player" || t.SourceEvent is not GameEvent_HpHealed heal || heal.SourceUid != null) return null;
            if (t.Log == null || t.EventIndex < 0 || t.EventIndex >= t.Log.Count) return null;
            // 近似: Godot の IsOwnedHolyMaulHealAt (登録済みの主たる当たりの札の回復) = 区切りが white_holy_maul の CardPlayed・受領の検証
            int start = t.EventIndex; while (start > 0 && !Hd2dOwner.IsBoundary(t.Log[start])) start--;
            if (t.Log[start] is not GameEvent_CardPlayed { CardId: "white_holy_maul" }) return null;
            var receipt = CombatPresentationLedger.Read(heal); if (receipt == null || !CombatPresentationLedger.Valid(heal, receipt, out _)) return null;
            var original = new AttackRoute("system:heal", "", "system", "heal", "HealContinuousFx", "light", true, "actual recovery receipt");
            var route = HolyMaulHealReceiveVfxProfiles.Select(original, true);
            if (!HolyMaulHealReceiveVfxProfiles.SelectedRoute(route)) return null;
            return BeginRoute(t, original, route, null, false);
        }
        /// <summary>支え (守り・氷壁・回復) を受けた側 (自分) の上に。Godot の Hd2dSupportFx (UpdateSupportReceiver: 的 = 足元 + 上 × 高さ×.48)</summary>
        public static bool BeginSupport(AttackRoute route, float duration, GameEvent ev)
        {
            if (route == null || !HD2DFlags.Hd2dFx || route.Renderer != "Hd2dSupportFx") return false;
            if (!Ready()) return false;
            if (!Stage.TryGetUnitBox("player", out var feet, out var height) || !(height > 0f) || !float.IsFinite(feet.x)) return false;
            var impact = Stage.CaptureImpact("player");
            NativeEffect effect;
            try
            {
                string key = "support-owned/" + CacheKey(route, Hd2dEffectTier.Small);
                if (!_cache.TryGetValue(key, out effect)) { effect = Hd2dGeometryBridge.Create(route, Hd2dEffectTier.Small); if (effect != null) _cache[key] = effect; }
            }
            catch (Exception ex) { Debug.LogWarning("[Hd2dAttackFx] support: " + ex.GetType().Name + ": " + ex.Message); return false; }
            if (effect == null) return false;
            Slot s = null;
            try
            {
                s = Acquire("support/" + route.Key, effect, route);
                s.Busy = true; s.Support = true; s.Original = s.Route = route; s.Card = route.DefinitionId; s.TargetKey = "player";
                s.Height = Mathf.Max(.25f, height); s.Size = .88f; s.Emission = .72f; s.Particles = .65f; s.LightPower = .65f;   // Godot: route.FixedContact → small
                ConfigureDraws(s);
                s.Tail = duration;
                var cam = Stage.Camera.transform;
                s.Right = cam.right; s.Up = cam.up; s.Toward = -cam.forward;
                s.GroundRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized; s.GroundToward = Vector3.ProjectOnPlane(-cam.forward, Vector3.up).normalized;
                if (s.GroundRight.sqrMagnitude < 1e-6f) s.GroundRight = Vector3.right; if (s.GroundToward.sqrMagnitude < 1e-6f) s.GroundToward = Vector3.back;
                s.Feet = feet; s.Target = feet + s.Up * (s.Height * .48f);
                var palette = Palette(effect); s.Roles = palette; s.LightColor = palette[4];
                s.Expected = Time.time;
                effect.ConfigureSource?.Invoke(new HV3(1, 0, 0));   // 支えの出所 = 敵の側 (右)。Godot の守りの ConfigureSource は敵の向き
                effect.ConfigureParticleGain?.Invoke(s.Particles);
                effect.ConfigureSourceGesture?.Invoke(true);
                s.LightSpec = LightSpecFor(s, effect, route);
                s.Root.gameObject.SetActive(true);
                int v = s.Version, e = _epoch;
                s.Landed = true; s.Started = Time.time;
                Commit(effect, route, true);
                effect.Sample(0f); Upload(s, 0f);
                if (Stage.ImpactTargetCurrent("player", impact))
                {
                    var captured = s;
                    s.Light = StageFx.BeginContractAttackLight("player", impact?.Target, e, s.Target, s.Feet, s.Height, s.LightSpec, 0, false,
                        () => Current(captured, v, e) && Stage.ImpactTargetCurrent("player", impact), 1f, s.LightPower, () => Time.time - captured.Started);
                    s.Light?.Land();
                }
                Debug.Log("[Hd2dAttackFx] support route=" + route.Key + " renderer=" + route.Renderer + " family=" + route.Family + " duration=" + duration.ToString("F2"));
                Tween.RunPresentation(Life(s, v, e));
                Submitted++;
                return true;
            }
            catch (Exception ex) { if (s != null) Retire(s, s.Version); Debug.LogWarning("[Hd2dAttackFx] support failed: " + ex.GetType().Name + ": " + ex.Message); return false; }
        }

        /// <summary>Godot の ConfigureMaterial の _Gain / _Emission (VineSlashFx・PhysicalContactFx は層の番号で発光/粒の倍率)</summary>
        static void ConfigureDraws(Slot s)
        {
            var e = s.Effect;
            for (int j = 0; j < s.Draws.Length; j++)
            {
                var d = s.Draws[j]; var b = e.Batches[j];
                float gain = e.Shader == "VineSlashFx" ? (j == 2 ? 3.8f * s.Emission : j == 1 ? 1.8f * s.Emission : j == 0 ? 2.25f * s.Emission : 2.2f * s.Particles)
                    : e.Shader == "PhysicalContactFx" ? (j == 2 ? s.Particles : s.Emission) : 1f;
                float emission = e.Shader == "EarthStompSurfaceR2" ? (b.Kind < 0 ? s.Emission : (b.Kind == 0 ? s.Particles : 1f)) : s.Emission;
                d.Gain = gain; d.Emission = emission;
            }
        }
        /// <summary>接触の光の契約。Godot の AuthoredGeometryFx.Sample の光の方針 (SupportLightVfx391 ほか) は目標・床の光の量の倍率 (×高さ²/9) として写す</summary>
        static WorldBattleFx.AttackSpec LightSpecFor(Slot s, NativeEffect effect, AttackRoute route)
        {
            var light = s.LightColor;
            float target = effect.TargetPeak, floor = effect.FloorPeak;
            if (effect.Shader == "BoundedContactFx") target = floor = 0f;   // Godot: BoundedContactFx の光は 0
            var policy = Hd2dOwnership.LightPolicy(route);
            if (policy.HasValue) { float scale = s.Height * s.Height / 9f; target *= policy.Value.targetGain * scale; floor *= policy.Value.floorGain * scale; }
            // 選ばれた当たり (光の縮小): 鍔迫り合いの光・散り・軸の突き (Godot は範囲を縮める。Unity は量で近似)
            if (LightBoltContactVfxProfiles.SelectedRoute(route)) { target *= .50f; floor *= .14f; }
            float tail = Mathf.Max(.05f, s.Tail);
            return new WorldBattleFx.AttackSpec(new[] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, .080f, tail }, 1, 1, 1, light, light, 0f, 0f, 0f)
            { ContractLighting = true, StandardContractPower = true, TargetPeakMultiplier = target, FloorPeakMultiplier = floor, MaxCombineGroup = "hd2d-prototype-r192", Envelope = age => Mathf.Max(0f, effect.Light()) };
        }

        static WorldBattleFx.AttackPlayback BeginRoute(ResolvedCombatFx.Ticket t, AttackRoute original, AttackRoute route, float? contactAt, bool _)
        {
            if (!t.HasWorld || t.Amount <= 0) return null;
            if (!Ready()) { t.RenderFallbackReason = LastFailure; return null; }
            float height = t.ContactVisibleHeight > 0f ? t.ContactVisibleHeight : t.Height;
            if (!(height > 0f) || !float.IsFinite(t.Feet.x)) { t.RenderFallbackReason = "hd2d_invalid_captured_geometry"; return null; }
            string card = route.DefinitionId; var tier = TierOf(t.Visual?.Tier);
            bool protoCard = Hd2dAttackPrototype.SupportsRoute(route) && route.SourceKind == "card";
            NativeEffect effect;
            try
            {
                string key = CacheKey(route, tier);
                if (!_cache.TryGetValue(key, out effect)) { effect = Hd2dGeometryBridge.Create(route, tier); if (effect != null) _cache[key] = effect; }
            }
            catch (Exception ex) { t.RenderFallbackReason = "hd2d_effect_construction_failed:" + ex.GetType().Name; Debug.LogWarning("[Hd2dAttackFx] create " + route.Key + "/" + route.Renderer + ": " + ex.Message); return null; }
            if (effect == null) { t.RenderFallbackReason = "hd2d_route_unsupported"; return null; }
            Slot s = null;
            try
            {
                s = Acquire(CacheKey(route, tier), effect, route);
                s.Ticket = t; s.Card = card; s.Busy = true; s.Original = original; s.Route = route; s.TargetKey = t.Target; s.SourceUnit = t.SourceUnit;
                var visual = t.Visual; bool physical = PhysicalVfxProfiles.ForCard(card) != null && protoCard;
                bool small = tier == Hd2dEffectTier.Small, large = tier == Hd2dEffectTier.Large;
                // Godot の Begin: 段 (small/medium/large) → 大きさ・発光・粒・光。SourceVisual があればそれ。FixedContact (札でない小さな当たり) は small
                s.Size = small ? .88f : large ? 1.12f : 1f; s.Emission = small ? .72f : large ? 1.18f : 1f; s.Particles = small ? .65f : large ? 1.15f : 1f; s.LightPower = small ? .65f : large ? 1.2f : 1f;
                if (visual != null) { s.Size = visual.Size; s.Emission = visual.Emission; s.Particles = visual.Particles; s.LightPower = visual.Light; }
                else if (route.FixedContact) { s.Size = .88f; s.Emission = .72f; s.Particles = .65f; s.LightPower = .65f; }
                if (physical) s.Size = 1f;   // 物理の家族は 1 (checkpoint 192 の近似のまま)
                s.Height = Mathf.Max(.25f, height);
                ConfigureDraws(s);
                // 見える終わり: 元の経路の時計 (Godot の beat は元の経路の Duration を持つ)。登録のある札は Hd2dAttackPrototype の Tail
                var times = Hd2dGeometryBridge.Times(original, tier);
                s.Tail = protoCard ? Hd2dAttackPrototype.Tail(route.Key) : Mathf.Max(times.end, EffectEnd(route));
                var cam = Stage.Camera.transform;
                s.Right = cam.right; s.Up = cam.up; s.Toward = -cam.forward;
                s.GroundRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized; s.GroundToward = Vector3.ProjectOnPlane(-cam.forward, Vector3.up).normalized;
                if (s.GroundRight.sqrMagnitude < 1e-6f) s.GroundRight = Vector3.right; if (s.GroundToward.sqrMagnitude < 1e-6f) s.GroundToward = Vector3.back;
                s.Feet = t.Feet; s.Target = t.Feet + s.Up * (s.Height * .5f);
                // 旧い幾何 (Bounded・Scatter・LocalMaterial…) は z=0 の平面 = 板の面そのもの。Unity の旧い描き手 (ResolvedCombatFx.Position) と同じく手前へ .28 (Hd2dPrototype の層は自分で z を持つ)
                if (effect.Shader != "Hd2dPrototype") s.Target += s.Toward * .28f;
                var palette = Palette(effect); s.Roles = palette; s.LightColor = palette[4];
                var plan = protoCard ? PlanFor(t.Target, card) : null; s.Remote = plan?.Remote ?? false; s.NearSurface = plan?.Surface;
                s.Expected = contactAt ?? Time.time;
                // dot の BattleStage が Begin の直後に呼ぶ設定 (鏡像は当たりの番号で交互・粒の量・遠隔打ち・構えの身振り・種・経路)
                effect.ConfigureMirror?.Invoke(t.Index % 2 == 1);
                effect.ConfigureParticleGain?.Invoke(s.Particles);
                effect.ConfigureRemoteStrike?.Invoke(s.Remote);
                effect.ConfigureSourceGesture?.Invoke(true);
                effect.ConfigureVisualSeed?.Invoke(t.Feet.x * 1.71f + t.Feet.z * 2.13f + t.Index * 1.39f);
                effect.ConfigureResolvedPath?.Invoke(Array.Empty<HV3>(), Array.Empty<float>());
                Commit(effect, route, false);
                if (card == "green_finisher_stomp" && effect.ConfigureStompTop != null)
                {
                    var scr = Stage.Camera.WorldToScreenPoint(s.Feet);
                    var top = Stage.Camera.ScreenToWorldPoint(new Vector3(scr.x, Screen.height * 1.12f, scr.z));
                    effect.ConfigureStompTop(Vector3.Dot(top - s.Feet, s.Up) / (s.Height * s.Size));
                }
                // 出所 (相手を原点にした板の座標): 自分の当たりは主人公の斧の先/杖の石、敵の当たりは攻めた敵の体の中心
                if (t.Target != "player" && Stage.TryGetUnitAnchor("player", out var hero))
                    effect.ConfigureSource?.Invoke(Local(s, Hd2dAttackPrototype.UsesCastRoute(route.Key) ? FireSourcePoint(hero) : s.Remote ? hero.TexturePoint(hero.CurrentTex, new Vector2(.90f, .55f)) : AxeContactPoint(hero)));
                else if (t.Target == "player" && !string.IsNullOrEmpty(t.SourceUnit) && Stage.TryGetUnitBox(t.SourceUnit, out var ef, out var eh))
                    effect.ConfigureSource?.Invoke(Local(s, ef + s.Up * (eh * .5f)));
                else effect.ConfigureSource?.Invoke(new HV3(t.Target == "player" ? 1 : -1, 0, 0));
                s.LightSpec = LightSpecFor(s, effect, route);
                s.Root.gameObject.SetActive(true);
                int v = s.Version, e = _epoch; var captured = s;
                t.OutcomeIncomingReason = protoCard ? "hd2d_prototype_route" : "hd2d_selected_route:" + route.Renderer;
                Debug.Log("[Hd2dAttackFx] begin route=" + route.Key + " renderer=" + route.Renderer + " (" + original.Renderer + ") family=" + route.Family + " palette=" + effect.Palette + " shader=" + effect.Shader + " batches=" + effect.Batches.Length + " height=" + s.Height.ToString("F2") + " remote=" + s.Remote + " lead=" + (s.Expected - Time.time).ToString("F3") + " tail=" + s.Tail.ToString("F2") + " target=" + t.Target);
                // 予備動作の音 (dot の BattleStage: 札の最初の当たりで一度)。当たりの音は Presenter が Hd2dContact.<札> で鳴らす
                if (protoCard && t.Index == 0 && Hd2dAttackPrototype.HasAuthoredAudio(card)) Audio.Key("Hd2dOnset." + Hd2dAttackPrototype.AudioCard(card));
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
                            Commit(captured.Effect, captured.Route, true);   // 本物の受領 = 数字が出る瞬間
                            UpdateSource(captured, 0f); captured.Effect.Sample(0f); Upload(captured, 0f);
                            if (Stage.ImpactTargetCurrent(t.Target, t.Impact))
                            {
                                captured.Light = StageFx.BeginContractAttackLight(t.Target, t.Impact?.Target, e, captured.Target, captured.Feet, captured.Height, captured.LightSpec, 0, false,
                                    () => Current(captured, v, e) && Stage.ImpactTargetCurrent(t.Target, t.Impact), 1f, captured.LightPower, () => Time.time - captured.Started);
                                captured.Light?.Land();
                            }
                            t.AuthoredRenderer = protoCard ? RendererId : RendererId422; return true;
                        }
                        catch (Exception ex) { Retire(captured, v); t.RenderFallbackReason = "hd2d_contact_failed:" + ex.GetType().Name; return false; }
                    },
                    Read = () => Current(captured, v, e) ? new { attack = t.AttackFamily, renderer = protoCard ? RendererId : RendererId422, route = captured.Route.Key, selected = captured.Route.Renderer, card = captured.Card, age = captured.Landed ? Time.time - captured.Started : Time.time - captured.Expected, landed = captured.Landed, remote = captured.Remote, height = captured.Height, size = captured.Size, version = v, serial = captured.Serial, visibleEnd = captured.Tail } : null
                };
                Tween.RunPresentation(Life(s, v, e));
                if (!Current(s, v, e)) { if (t.RenderFallbackReason == null) t.RenderFallbackReason = "hd2d_scope_cancelled"; return null; }
                Submitted++;
                return playback;
            }
            catch (Exception ex) { if (s != null) Retire(s, s.Version); t.RenderFallbackReason = "hd2d_construction_failed:" + ex.GetType().Name; Debug.LogWarning("[Hd2dAttackFx] begin " + route.Key + ": " + ex.GetType().Name + ": " + ex.Message); return null; }
        }
        /// <summary>選ばれた当たりの幾何が自分で持つ終わり (Hd2dAttackPrototype の各 Create の age の上限)。元の経路の時計より長い時はこちらまで見せる</summary>
        static float EffectEnd(AttackRoute r)
        {
            if (NecroContactVfxProfiles.SelectedRoute(r)) return r.DefinitionId == "black_groaning_skull" ? .44f : .30f;
            if (ReactionContactVfxProfiles.SelectedRoute(r)) return r.DefinitionId == "black_reaction_curse" ? .30f : .44f;
            if (PermanentContactVfxProfiles.SelectedRoute(r)) return r.Family == "fire_localburst" ? .212f : r.Family == "dark_drain" ? .44f : r.Family == "lightning_discharge" ? .20f : .32f;
            if (EnemyContactVfxProfiles.SelectedRoute(r)) return r.Family == "enemy_blunt" ? .31f : .28f;
            if (GearContactVfxProfiles.SelectedRoute(r) || GearKegContactVfxProfiles.SelectedRoute(r) || NamelessPowderContactVfxProfiles.SelectedRoute(r) || NamelessKegContactVfxProfiles.SelectedRoute(r)) return .284f;
            if (StatusContactVfxProfiles.SelectedRoute(r)) return .212f;
            if (LightningContactVfxProfiles.SelectedRoute(r)) return .34f;
            if (SporeContactVfx385.SelectedRoute(r)) return .32f;
            return 0f;
        }
    }
}
