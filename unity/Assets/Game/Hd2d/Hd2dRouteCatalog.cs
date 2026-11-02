#nullable enable
// Hd2dRouteCatalog.cs — dot の Godot 版 Project422 の AttackRouteCatalog (RecoveredSource/DeckRogue.GodotPort.Presentation/AttackRouteCatalog.cs) の写し (2026-11-02)。
// 表は assets/attack-routes.json (443 本: 札 208・敵の技 167・置物 38・仕込み札 19・亡骸 9・ギア 2) を Resources/Data/hd2d-attack-routes.json にそのまま置いた。
// 出来事 (DamageDealt) の出所 (PresentationSource の種類:定義 id、敵なら enemy_actual_execution:<敵>/<技>) から「元の経路」(旧い描き手の名前のまま) を引く。
// 選ばれた経路 (Hd2d* の描き手) への書き換えは Hd2dOwnership が門を通った時だけ行う。
using System.Collections.Generic;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DeckRogue.Game.Hd2d
{
    internal static class Hd2dRouteCatalog
    {
        static Dictionary<string, AttackRoute>? _routes;
        public static int UnknownRoutes { get; private set; }
        public static int Count => Routes.Count;

        public static AttackRoute Neutral(string key = "unresolved")
            => new AttackRoute(key, "", "system", "minimal_neutral_contact", "BoundedContactFx", "neutral", true, "unresolved provenance; conservative contact");

        static Dictionary<string, AttackRoute> Routes
        {
            get
            {
                if (_routes != null) return _routes;
                var routes = new Dictionary<string, AttackRoute>();
                try
                {
                    var asset = Resources.Load<TextAsset>("Data/hd2d-attack-routes");
                    if (asset != null)
                    {
                        foreach (var t in JArray.Parse(asset.text))
                        {
                            var r = new AttackRoute((string?)t["key"] ?? "", (string?)t["definitionId"] ?? "", (string?)t["sourceKind"] ?? "", (string?)t["family"] ?? "",
                                (string?)t["renderer"] ?? "", (string?)t["element"] ?? "", (bool?)t["fixedSmall"] ?? false, (string?)t["portStatus"] ?? "")
                            { PresentationPolicy = (string?)t["presentationPolicy"] };
                            routes[Identity(r)] = r;
                        }
                    }
                    else Debug.LogWarning("[Hd2dRouteCatalog] Data/hd2d-attack-routes.json が無い");
                }
                catch (System.Exception e) { Debug.LogWarning("[Hd2dRouteCatalog] 読めない: " + e.Message); }
                _routes = routes;
                return routes;
            }
        }

        public static string Identity(AttackRoute r)
        {
            string text = r.SourceKind == "enemy_actual_execution" ? r.Key.Substring(6) : r.DefinitionId;
            return r.SourceKind + ":" + text;
        }

        /// <summary>Godot の AttackRouteCatalog.Resolve(hit) と同じ。leaderId はイゼットの雷の上書きに使う</summary>
        public static AttackRoute Resolve(GameEvent_DamageDealt hit, string? leaderId)
        {
            var source = hit.PresentationSource;
            string? key = source == null ? null : (source.Kind + ":" + source.DefinitionId);
            var enemy = hit.PresentationEnemyAction;
            if (enemy != null && enemy.MoveId != null)
                key = "enemy_actual_execution:" + enemy.EnemyId + "/" + enemy.MoveId;
            if (source?.DefinitionId == "blue_aether_burst" && leaderId == "leader_izzet")
                return new AttackRoute("card:blue_aether_burst", source.DefinitionId, source.Kind, "lightning_discharge", "LightningDischargeFx", "lightning", false, "source leader override");
            if (key != null && Routes.TryGetValue(key, out var route)) return route;
            //354: exact known engine relic-passive provenance alias; never rewrite the event.
            //Catalog maps identities only. Installed/registered ownership remains Stage/engine evidence.
            if (hit.Source == "player" && enemy == null && hit.SourceUid == "relic_relic_momentum_whip"
                && source is { Kind: "permanent", DefinitionId: "relic_momentum_whip_passive", Uid: "relic_relic_momentum_whip" }
                && Routes.TryGetValue("permanent:relic_momentum_whip", out var canonicalWhip)
                && canonicalWhip.Key == "relic:relic_momentum_whip" && canonicalWhip.DefinitionId == "relic_momentum_whip"
                && canonicalWhip.SourceKind == "permanent" && canonicalWhip.Family == "neutral_whip_contact"
                && canonicalWhip.Renderer == "BoundedContactFx" && canonicalWhip.Element == "neutral" && canonicalWhip.FixedSmall)
                return canonicalWhip;
            UnknownRoutes++;
            return Neutral(key ?? hit.Source);
        }

        public static AttackRoute ResolveKey(string key) => Routes.TryGetValue(key, out var r) ? r : Neutral(key);

        /// <summary>延焼のティック (system:burn)。Godot の BattleStage は BurnTick の beat に system:burn/SystemOutcomeFx の経路を立てる</summary>
        public static AttackRoute BurnSystem() => new AttackRoute("system:burn", "", "system", "burn", "SystemOutcomeFx", "fire", true, "positive actual status receipt");
    }
}
