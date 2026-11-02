#nullable enable
// Hd2dOwnership.cs — dot の Godot 版 Project422 で BattleStage が握っていた「所有権」(この当たりが本物のその札・置物・仕込み札・ギア・敵の技の当たりか) の判定を、
// Unity の Ticket (ResolvedCombatFx) と出来事の記録 (GameState.EventLog) の上で同じ意味になるように書いたもの (2026-11-02)。
// Godot は 1 コマンドの差分 (SessionChange: Previous/Current の盤面・Command・Events) を持っていた。Unity の Presenter は出来事の列を順に見せるので、
//   「同じコマンドの出来事」= 記録の中で直前の区切り (CardPlayed・ReactionTriggered・GearUsed・TurnEnded・TurnStarted・EnemyActionExecuting・EnemyPhaseEnded) から次の区切りまで、
//   「前の盤面」= 見せていた盤面 (ctx.Prev)、「後の盤面」= 今の盤面 (ctx.CapturedState)、「コマンド」= 区切りの出来事の種類 で近似する。
// 門の組 (Routes の tuple と Select) は Hd2dVfxProfiles にそのまま。ここは IsOwned* と、元の経路 → 選ばれた経路 の鎖 (Godot の *ContactRoute 群) だけ。
// 近似した所 (Godot と違いうる所) は各判定のコメントに「近似:」で書いた。
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game.Hd2d
{
    internal sealed class Hd2dOwner
    {
        public GameEvent_DamageDealt Hit = default!;
        public IReadOnlyList<GameEvent>? Log; public int Index = -1;
        public GameState? Before, After;
        public string? LeaderId;
        public IReadOnlyList<GearInstance>? GearsBefore, GearsAfter; public IReadOnlyList<string>? SeenGearIds;
        /// <summary>同じコマンドの出来事 (区切りから区切りまで)。Godot の owner.Change.Events の近似</summary>
        public List<GameEvent> Events = new List<GameEvent>();
        /// <summary>区切りの出来事 (CardPlayed / ReactionTriggered / GearUsed / EnemyActionExecuting / TurnEnded…)。Godot の Command の近似</summary>
        public GameEvent? Boundary;

        public static bool IsBoundary(GameEvent e) => e is GameEvent_CardPlayed || e is GameEvent_ReactionTriggered || e is GameEvent_GearUsed
            || e is GameEvent_TurnEnded || e is GameEvent_TurnStarted || e is GameEvent_EnemyPhaseEnded || e is GameEvent_EnemyActionExecuting;

        public static Hd2dOwner? From(ResolvedCombatFx.Ticket t)
        {
            if (t?.SourceEvent is not GameEvent_DamageDealt hit) return null;
            var o = new Hd2dOwner { Hit = hit, Log = t.Log, Index = t.EventIndex, Before = t.PrevState, After = t.AfterState, LeaderId = t.LeaderId, GearsBefore = t.GearsBefore, GearsAfter = t.GearsAfter, SeenGearIds = t.SeenGearIds };
            var log = t.Log;
            if (log != null && t.EventIndex >= 0 && t.EventIndex < log.Count && ReferenceEquals(log[t.EventIndex], hit))
            {
                int start = t.EventIndex;
                while (start > 0 && !IsBoundary(log[start])) start--;
                // ギアは「無名 → 選んだギア」の 2 連の GearUsed が 1 コマンド
                while (start > 0 && log[start] is GameEvent_GearUsed && log[start - 1] is GameEvent_GearUsed) start--;
                int end = t.EventIndex + 1;
                while (end < log.Count && !IsBoundary(log[end])) end++;
                o.Boundary = IsBoundary(log[start]) ? log[start] : null;
                for (int i = start; i < end; i++) o.Events.Add(log[i]);
            }
            else o.Events.Add(hit);
            return o;
        }

        public bool Contains(GameEvent e) => Events.Any(x => ReferenceEquals(x, e));
        public int IndexOf(GameEvent e) { for (int i = 0; i < Events.Count; i++) if (ReferenceEquals(Events[i], e)) return i; return -1; }
        public GameEvent_CardPlayed? PlayedCard => Boundary as GameEvent_CardPlayed;
        /// <summary>この当たりの札が、前の盤面の手札にあった札 (uid と定義 id が一致) か。Godot の play.CardUid==source.Uid && before.Player.Hand.Any(...) の近似</summary>
        public CardInstance? HandCard(string? uid, string? defId) => uid == null || defId == null ? null : Before?.Player?.Hand?.FirstOrDefault(c => c.Uid == uid && c.Def.Id == defId);
        public bool TargetAgrees(int index) => index >= 0 && Before?.Enemies != null && After?.Enemies != null && index < Before.Enemies.Count && index < After.Enemies.Count && Before.Enemies[index].EnemyId == After.Enemies[index].EnemyId;
        public bool ReceiptValid(GameEvent e)
        {
            var r = CombatPresentationLedger.Read(e);
            return r != null && CombatPresentationLedger.Valid(e, r, out _);
        }
    }

    internal static class Hd2dOwnership
    {
        // ---------- 元の経路 ----------
        public static AttackRoute Resolve(Hd2dOwner o) => Hd2dRouteCatalog.Resolve(o.Hit, o.LeaderId);

        // ---------- 選ぶ (Godot の BattleStage の *ContactRoute と StartEffect の Select 群を 1 本に) ----------
        public static AttackRoute Select(AttackRoute route, Hd2dOwner o)
        {
            if (!Hd2dAttackPrototype.Enabled) return route;
            var hit = o.Hit;
            // DamageDealt を持つ門
            route = NecroContactVfxProfiles.Select(route, hit, IsOwnedCenserNecroContact(o));
            route = LightningContactVfxProfiles.Select(route, hit, IsOwnedIzzetAetherBurst(o));
            route = SparkBurstContactVfxProfiles.Select(route, IsOwnedSparkBurstContact(o, route));
            route = AetherLanceContactVfxProfiles.Select(route, IsOwnedAetherLanceContact(o, route));
            route = VineFormationCounterVfxProfiles.Select(route, IsOwnedVineFormationCounter(o, route));
            route = ReactionContactVfxProfiles.Select(route, hit, IsOwnedConfirmedReactionContact(o));
            route = MomentumWhipContactVfxProfiles.Select(route, IsOwnedMomentumWhipContact(o, route));
            route = MirrorContactVfxProfiles.Select(PermanentContactVfxProfiles.Select(route, hit, IsOwnedInstalledPermanentContact(o)), hit, IsOwnedMirrorNegationContact(o, route));
            route = NamelessKegContactVfxProfiles.Select(NamelessPowderContactVfxProfiles.Select(GearKegContactVfxProfiles.Select(GearContactVfxProfiles.Select(route, hit, IsOwnedDirectGear(o, route, "gear_powder")), hit, IsOwnedDirectGear(o, route, "gear_powder_keg")), hit, IsOwnedNamelessGear(o, route, "gear_powder")), hit, IsOwnedNamelessGear(o, route, "gear_powder_keg"));
            route = DoubleLashReceiveVfxProfiles.Select(route, IsOwnedDoubleLashContact(o, route));
            route = HolyMaulReceiveVfxProfiles.Select(route, IsOwnedHolyMaulContact(o, route));
            route = AxialFamilyContactVfxProfiles.Select(TripleHornReceiveVfxProfiles.Select(route, IsOwnedTripleHornContact(o, route)), IsOwnedAxialFamilyContact(o, route));
            route = LightBurstContactVfxProfiles.Select(route, IsOwnedLightBurstContact(o, route));
            route = LightBoltContactVfxProfiles.Select(route, hit, IsOwnedLightBoltContact(o, route));
            route = ColdSweepContactVfxProfiles.Select(route, hit, IsOwnedColdSweepContact(o, route));
            route = EnemyContactRoute(route, o);
            // StartEffect で選ぶ門 (Godot では Begin の直前)
            route = SporeContactVfx385.Select(route, IsOwnedDirectCardContact(o, route, "green_catalyst_spore", requireNoSourceUid: true));
            route = GreenGulpContactVfxProfiles.Select(route, IsOwnedGreenGulpContact(o, route));
            route = CultistDarkBladeContactVfxProfiles.Select(route, IsOwnedEnemyMoveContact(o, route, "enemy_cultist", "dark_blade"));
            route = WraithCurseContactVfxProfiles.Select(route, IsOwnedEnemyMoveContact(o, route, "enemy_nemesis_wraith", "curse_touch"));
            route = CrossbowAimedContactVfxProfiles.Select(route, IsOwnedEnemyMoveContact(o, route, "enemy_crossbow_archer", "aimed_shot"));
            route = VineGraspContactVfxProfiles.Select(route, IsOwnedEnemyMoveContact(o, route, "enemy_vine_walker", "grasping_vines"));
            route = BlueContactVfx379Routes.Select(route, IsOwnedBlueContact(o, route));
            return route;
        }

        /// <summary>この経路は Hd2d の描き手 (選ばれた経路・登録のある札・光の方針だけの経路) か。Hd2dAttackFx が受けるかの判定</summary>
        public static bool IsHd2dRenderer(AttackRoute r)
        {
            if (r == null || !Hd2dAttackPrototype.Enabled) return false;
            if (Hd2dAttackPrototype.SupportsRoute(r)) return true;
            switch (r.Renderer)
            {
                case "Hd2dPermanentContactFx": case "Hd2dNecroContactFx": case "Hd2dReactionContactFx": case "Hd2dLightningContactFx": case "Hd2dEnemyContactFx":
                case "Hd2dBurnStatusFx": case "Hd2dGearContactFx": case "Hd2dGearKegContactFx": case "Hd2dNamelessPowderContactFx": case "Hd2dNamelessKegContactFx":
                case "Hd2dSupportFx": case "BlueOwnedContact379Fx": case "OwnedSpore385Fx": case "GreenGulpContactFx": case "CultistDarkBladeContactFx":
                case "MomentumWhipContactFx": case "SparkBurstContactFx": case "AetherLanceContactFx": case "VineFormationCounterFx": case "DoubleLashReceiverFx":
                case "HolyMaulReceiverFx": case "OwnedAxialContactFx": case "LightBurstContactFx": case "LightBoltContactFx": case "CrossbowAimedContactFx":
                case "VineGraspContactFx": case "WraithCurseContactFx": case "ColdSweepContactFx": case "ImpSparkContactFx": case "MirrorWaterContactFx":
                    return true;
            }
            // 幾何は旧い描き手のままで素材だけ違う 2 つ (ObjectContact415・WarmSpark397・Maelstrom)
            return ObjectContact415Routes.ObjectContact415Route(r) || ObjectContact415Routes.WarmSpark397Route(r) || ObjectContact415Routes.IsExactMaelstromRoute(r);
        }

        // ---------- IsOwned* (Godot の BattleStage の同名の関数の写し。近似は各所に) ----------
        static bool DirectCardCommon(Hd2dOwner o, string defId, out DamagePresentationSource source, out int index, bool requireNoSourceUid)
        {
            source = null!; index = -1;
            var hit = o.Hit;
            if (hit.Source != "player" || (requireNoSourceUid && hit.SourceUid != null) || hit.PresentationSource is not { Kind: "card" } s || s.DefinitionId != defId || string.IsNullOrEmpty(s.Uid)) return false;
            if (hit.EnemyIndex is not int i) return false;
            // 近似: Godot は Command_PlayCard の CardUid==source.Uid・TargetIndex==index を見る。Unity は区切りが CardPlayed (同じ札) で、前の手札にその uid があること
            if (o.PlayedCard is not { } played || played.CardId != defId || o.HandCard(s.Uid, defId) == null) return false;
            source = s; index = i; return true;
        }
        static bool IsOwnedDirectCardContact(Hd2dOwner o, AttackRoute route, string defId, bool requireNoSourceUid)
        {
            // 385 Spore ほか: 元の経路・直接の札・前の手札・受領の検証
            if (!DirectCardCommon(o, defId, out _, out int index, requireNoSourceUid) || o.Hit.HpLoss <= 0) return false;
            if (!(route.SourceKind == "card" && route.DefinitionId == defId)) return false;
            return o.TargetAgrees(index) && o.ReceiptValid(o.Hit);
        }

        static bool IsOwnedCenserNecroContact(Hd2dOwner o)
        {
            var hit = o.Hit; var before = o.Before; var after = o.After;
            var source = hit.PresentationSource; string? uid = source?.Uid; string? definition = source?.DefinitionId;
            int expectedDrain = NecroContactVfxProfiles.ExpectedDrainAmount(definition);
            if (before == null || after == null || source?.Kind != "necro" || string.IsNullOrEmpty(definition) || expectedDrain <= 0 || string.IsNullOrEmpty(uid)) return false;
            var card = before.Player.DrawPile.FirstOrDefault(c => c.Uid == uid && c.Def.Id == definition);
            bool moved = card != null && card.Def.Effects.Any(e => e.Trigger == "onSelfExhausted" && e.Effect == "dealDamageDrain" && e.Amount == expectedDrain)
                && !before.Player.ExhaustPile.Any(c => c.Uid == uid) && !after.Player.DrawPile.Any(c => c.Uid == uid)
                && after.Player.ExhaustPile.Any(c => c.Uid == uid && c.Def.Id == definition);
            bool censer = !string.IsNullOrEmpty(hit.SourceUid)
                && (before.Player.Permanents.Any(c => c.Uid == hit.SourceUid && c.Def.Id == "black_thirst_censer")
                    || after.Player.Permanents.Any(c => c.Uid == hit.SourceUid && c.Def.Id == "black_thirst_censer"));
            bool milled = false, necro = false, found = false;
            foreach (var e in o.Events)
            {
                if (ReferenceEquals(e, hit)) { found = true; break; }
                if (e is GameEvent_CardsMilled { Count: > 0 } mill && mill.CardIds?.Contains(definition) == true) milled = true;
                if (milled && e is GameEvent_NecroFired fired && fired.CardId == definition) necro = true;
            }
            return NecroContactVfxProfiles.OwnsCenserDeckExhaustion(hit, moved, censer, found && milled && necro);
        }

        static bool IsOwnedIzzetAetherBurst(Hd2dOwner o)
        {
            var hit = o.Hit;
            if (o.LeaderId != "leader_izzet" || o.PlayedCard is not { CardId: "blue_aether_burst" }
                || hit.PresentationSource is not { Kind: "card", DefinitionId: "blue_aether_burst" } source || string.IsNullOrEmpty(source.Uid)) return false;
            // Immutable owning command context, not mutable catalog leader state or proximity to a resource event.
            return o.HandCard(source.Uid, "blue_aether_burst") != null;
        }

        static bool IsOwnedSparkBurstContact(Hd2dOwner o, AttackRoute route)
        {
            const string id = "white_spark_burst"; var hit = o.Hit;
            if (!SparkBurstContactVfxProfiles.OriginalRoute(route) || o.LeaderId != "leader_white"
                || hit.Amount != 3 || hit.HpLoss != 3 || !DirectCardCommon(o, id, out var source, out int index, true)) return false;
            var before = o.Before; var after = o.After;
            if (before == null || after == null || !before.Player.Hand.Any(c => c.Uid == source.Uid && c.Def.Id == id && c.Def.Type == "spell")
                || !o.TargetAgrees(index) || before.Enemies[index].Hp <= 3 || before.Enemies[index].Block != 0) return false;
            var events = o.Events; var played = events.OfType<GameEvent_CardPlayed>().ToArray();
            var hits = events.OfType<GameEvent_DamageDealt>().ToArray(); var additions = events.OfType<GameEvent_CardsAddedToDraw>().ToArray();
            if (played.Length != 1 || played[0].CardId != id || hits.Length != 1 || !ReferenceEquals(hits[0], hit)
                || additions.Length != 1 || additions[0].CardId != "white_spark_token" || additions[0].Count != 3
                || o.IndexOf(played[0]) >= o.IndexOf(hit) || o.IndexOf(hit) >= o.IndexOf(additions[0])) return false;
            var oldIds = before.Player.DrawPile.Select(c => c.Uid).ToHashSet(); var tokens = after.Player.DrawPile.Where(c => !oldIds.Contains(c.Uid)).ToArray();
            if (tokens.Length != 3 || tokens.Select(c => c.Uid).Distinct().Count() != 3 || tokens.Any(c => c.Token != true || c.Def.Id != "white_spark_token")) return false;
            return o.ReceiptValid(hit);
        }

        static bool IsOwnedBlueContact(Hd2dOwner o, AttackRoute route)
        {
            var hit = o.Hit;
            if (!BlueContactVfx379Routes.OriginalRoute(route) || hit.Source != "player" || hit.HpLoss <= 0
                || hit.PresentationSource is not { Kind: "card" } source || string.IsNullOrEmpty(source.Uid)
                || o.PlayedCard is not { } played || played.CardId != source.DefinitionId || hit.EnemyIndex is not int index) return false;
            if (o.HandCard(source.Uid, source.DefinitionId) == null || !o.Contains(hit) || !o.TargetAgrees(index)) return false;
            return o.ReceiptValid(hit);
        }

        static bool IsOwnedAetherLanceContact(Hd2dOwner o, AttackRoute route)
        {
            const string id = "blue_aether_lance"; var hit = o.Hit;
            if (!AetherLanceContactVfxProfiles.OriginalRoute(route) || o.LeaderId != "leader_blue"
                || hit.Amount != 8 || hit.HpLoss != 8 || !DirectCardCommon(o, id, out var source, out int index, true)) return false;
            var before = o.Before; var after = o.After;
            if (before == null || after == null || before.Player.Aether != 2 || after.Player.Aether != 0 || after.Player.IceBlock != before.Player.IceBlock + 1
                || !before.Player.Hand.Any(c => c.Uid == source.Uid && c.Def.Id == id && c.Def.Type == "spell")
                || !o.TargetAgrees(index) || before.Enemies[index].Hp <= 8 || before.Enemies[index].Block != 0) return false;
            var events = o.Events; var played = events.OfType<GameEvent_CardPlayed>().ToArray();
            var spent = events.OfType<GameEvent_AetherDischarged>().ToArray(); var hits = events.OfType<GameEvent_DamageDealt>().ToArray();
            var ice = events.OfType<GameEvent_IceBlockGained>().ToArray();
            if (played.Length != 1 || played[0].CardId != id || spent.Length != 1 || spent[0].Spent != 2
                || hits.Length != 1 || !ReferenceEquals(hits[0], hit) || ice.Length != 1 || ice[0].Amount != 1
                || o.IndexOf(played[0]) >= o.IndexOf(spent[0]) || o.IndexOf(spent[0]) >= o.IndexOf(hit) || o.IndexOf(hit) >= o.IndexOf(ice[0])) return false;
            return o.ReceiptValid(hit);
        }

        static bool IsOwnedVineFormationCounter(Hd2dOwner o, AttackRoute route)
        {
            if (!VineFormationCounterVfxProfiles.OriginalRoute(route)) return false;
            var triggers = o.Events.OfType<GameEvent_ReactionTriggered>().ToArray();
            // 近似: Godot の IsOwnedVineFormationReactionNotice (確認の窓で発動した本物の仕込み札) は、区切りが同じ札の ReactionTriggered であること で代える
            return triggers.Length == 1 && triggers[0].CardId == "green_reaction_vine_formation" && o.Hit.Source == "player"
                && o.Hit.PresentationSource is { Kind: "reaction", DefinitionId: "green_reaction_vine_formation" } && o.Contains(o.Hit) && o.ReceiptValid(o.Hit);
        }

        static bool IsOwnedConfirmedReactionContact(Hd2dOwner o)
        {
            var hit = o.Hit;
            if (hit.PresentationSource is not { Kind: "reaction" } source || !ReactionContactVfxProfiles.SelectedDefinition(source.DefinitionId) || string.IsNullOrEmpty(source.Uid)) return false;
            // 近似: Godot は Command_ConfirmReaction{Fire:true}・前の盤面が awaiting-reaction の post 窓・罠が生きている・後の盤面の仕込み場から消えている、を見る。
            //       Unity は前の盤面の仕込み場にその札があり (uid・定義)、後の盤面に無く、同じコマンドの ReactionTriggered が先に出ていること
            var before = o.Before; var after = o.After;
            if (before == null || after == null) return false;
            var card = before.Player.SetCards.FirstOrDefault(c => c.Uid == source.Uid && c.Def.Id == source.DefinitionId);
            if (card == null || card.Def.Type != "reaction" || after.Player.SetCards.Any(c => c.Uid == source.Uid)) return false;
            bool triggered = false, found = false;
            foreach (var e in o.Events)
            {
                if (ReferenceEquals(e, hit)) { found = true; break; }
                if (e is GameEvent_ReactionTriggered trigger && trigger.CardId == source.DefinitionId) triggered = true;
            }
            return ReactionContactVfxProfiles.OwnsConfirmedInstance(hit, found && triggered);
        }

        static bool IsOwnedMomentumWhipContact(Hd2dOwner o, AttackRoute route)
        {
            const string uid = "relic_relic_momentum_whip", definition = "relic_momentum_whip_passive"; var hit = o.Hit;
            if (hit.Source != "player" || hit.PresentationEnemyAction != null || hit.SourceUid != uid
                || hit.PresentationSource is not { Kind: "permanent", DefinitionId: definition, Uid: uid } || hit.EnemyIndex is not int index || !o.Contains(hit)) return false;
            var before = o.Before; var after = o.After;
            bool Installed(CardInstance p) => p.Uid == uid && p.Def.Id == definition && p.Def.Type == "permanent" && p.Innate == true && p.Def.Retainer != true && p.Def.Echo != true && p.Def.Life == null;
            if (before == null || after == null || !before.Player.Permanents.Any(Installed) || !after.Player.Permanents.Any(Installed) || !o.TargetAgrees(index)) return false;
            var r = CombatPresentationLedger.Read(hit);
            if (r == null || !CombatPresentationLedger.Valid(hit, r, out _) || r.Target != "enemy" || r.EnemyIndex != index) return false;
            return route.Key == "relic:relic_momentum_whip" && route.DefinitionId == "relic_momentum_whip" && route.SourceKind == "permanent"
                && route.Family == "neutral_whip_contact" && route.Renderer == "BoundedContactFx" && route.Element == "neutral" && route.FixedSmall;
        }

        static bool IsOwnedInstalledPermanentContact(Hd2dOwner o)
        {
            var hit = o.Hit;
            string? uid = hit.PresentationSource?.Uid; string? definition = hit.PresentationSource?.DefinitionId;
            if (string.IsNullOrEmpty(uid) || !PermanentContactVfxProfiles.SelectedDefinition(definition)) return false;
            bool Installed(CardInstance p) => p.Uid == uid && p.Def.Id == definition && p.Def.Type == "permanent" && p.Def.Retainer != true && p.Def.Echo != true && p.Def.Life == null;
            bool installed = o.Before?.Player?.Permanents.Any(Installed) == true || o.After?.Player?.Permanents.Any(Installed) == true;
            return PermanentContactVfxProfiles.OriginalRoute(Resolve(o)) && PermanentContactVfxProfiles.OwnsInstalled(hit, installed);
        }

        static bool IsOwnedMirrorNegationContact(Hd2dOwner o, AttackRoute route)
        {
            var hit = o.Hit;
            if (!MirrorContactVfxProfiles.OriginalRoute(route) || string.IsNullOrEmpty(hit.PresentationSource?.Uid)) return false;
            // 近似: Godot は Command_ConfirmReaction{Fire} で blue_mana_leak を発動した pre 窓を見る。Unity は同じコマンドに ReactionTriggered(blue_mana_leak) と ActionNegated(同じ敵) が先に出ていること
            var previous = o.Before; var current = o.After; string uid = hit.PresentationSource!.Uid!;
            bool Installed(CardInstance p) => p.Uid == uid && p.Def.Id == "blue_perm_mirror" && p.Def.Type == "permanent" && p.Def.Retainer != true && p.Def.Echo != true && p.Def.Life == null;
            if (previous == null || current == null || !previous.Player.Permanents.Any(Installed) || !current.Player.Permanents.Any(Installed)) return false;
            bool leak = false, negated = false;
            foreach (var e in o.Events)
            {
                if (ReferenceEquals(e, hit)) return leak && negated && hit.SourceUid == uid;
                if (e is GameEvent_ReactionTriggered t && t.CardId == "blue_mana_leak") leak = true;
                if (e is GameEvent_ActionNegated n && n.EnemyIndex == hit.EnemyIndex) negated = true;
            }
            return false;
        }

        static bool GearConsumed(Hd2dOwner o, string heldGearId, out GearInstance? held)
        {
            held = null;
            // 近似: Godot は RunCommand_UseGear の Index で前の持ち物の 1 個を指す。Unity は前の持ち物に heldGearId があり、後の持ち物で 1 回減っている (無ければ消えている) こと
            var before = o.GearsBefore; var after = o.GearsAfter;
            // 近似: Unity の Presenter は前の持ち物を持たない。無ければ GearUsed の出来事 (Left = 残り回数) が同じコマンドにあることで「使って減った」とする
            if (before == null) return o.Events.Any(e => e is GameEvent_GearUsed g && g.GearId == heldGearId);
            foreach (var g in before)
            {
                if (g.GearId != heldGearId || g.Charges <= 0 || string.IsNullOrEmpty(g.Uid)) continue;
                var remaining = after?.Where(x => x.Uid == g.Uid).ToArray();
                bool consumed = g.Charges == 1 ? remaining == null || remaining.Length == 0
                    : remaining is { Length: 1 } && remaining[0].GearId == g.GearId && remaining[0].Charges == g.Charges - 1;
                if (consumed) { held = g; return true; }
            }
            return false;
        }
        static bool IsOwnedDirectGear(Hd2dOwner o, AttackRoute route, string gearId)
        {
            if (!GearConsumed(o, gearId, out _)) return false;
            bool original = gearId == "gear_powder" ? GearContactVfxProfiles.OriginalRoute(route) : GearKegContactVfxProfiles.OriginalRoute(route);
            if (!original) return false;
            bool used = false, found = false;
            foreach (var e in o.Events)
            {
                if (e is GameEvent_GearUsed gear) { if (used || gear.GearId != gearId) return false; used = true; }
                if (ReferenceEquals(e, o.Hit)) { found = true; break; }
            }
            return used && found;
        }
        static bool IsOwnedNamelessGear(Hd2dOwner o, AttackRoute route, string chosenGearId)
        {
            if (o.SeenGearIds?.Contains(chosenGearId) != true || !GearConsumed(o, "gear_nameless", out _)) return false;
            bool original = chosenGearId == "gear_powder" ? NamelessPowderContactVfxProfiles.OriginalRoute(route) : NamelessKegContactVfxProfiles.OriginalRoute(route);
            if (!original) return false;
            int used = 0; bool found = false;
            foreach (var e in o.Events)
            {
                if (e is GameEvent_GearUsed gear) { if (used >= 2 || gear.GearId != (used == 0 ? "gear_nameless" : chosenGearId)) return false; used++; }
                if (ReferenceEquals(e, o.Hit)) { found = true; break; }
            }
            return used == 2 && found;
        }

        static bool IsOwnedGreenGulpContact(Hd2dOwner o, AttackRoute route)
        {
            var h = o.Hit;
            if (!GreenGulpContactVfxProfiles.OriginalRoute(route) || h.HpLoss <= 0 || h.PresentationSource is not { DefinitionId: "green_serpent_gulp" } ps
                || h.EnemyIndex is not int index || o.PlayedCard is not { CardId: "green_serpent_gulp" } || o.HandCard(ps.Uid, "green_serpent_gulp") == null) return false;
            // 近似: Godot は play.DiscardUids が 1 枚 (払って捨てた札) を見る。Unity は同じコマンドに CardDiscarded ほかが出ていれば良いとせず、前の手札に 2 枚以上あった (払える) ことで代える
            if (o.Before == null || o.Before.Player.Hand.Count < 2 || !o.TargetAgrees(index) || !o.Contains(h)) return false;
            return o.ReceiptValid(h);
        }

        static bool IsOwnedDoubleLashContact(Hd2dOwner o, AttackRoute route)
        {
            const string id = "green_double_lash"; var hit = o.Hit;
            if (!DoubleLashReceiveVfxProfiles.OriginalRoute(route) || o.LeaderId != "leader_green" || !DirectCardCommon(o, id, out var source, out int index, true)) return false;
            var before = o.Before; var after = o.After;
            if (before == null || after == null || before.Player.Energy != 3 || after.Player.Energy != 2 || before.Player.Growth != 0 || before.Player.Momentum != 0 || before.Player.Weak != 0
                || !before.Player.Hand.Any(c => c.Uid == source.Uid && c.Def.Id == id && c.Def.Type == "physical")
                || !o.TargetAgrees(index) || before.Enemies[index].Hp <= 10 || before.Enemies[index].Block != 0) return false;
            var events = o.Events; var played = events.OfType<GameEvent_CardPlayed>().ToArray(); var hits = events.OfType<GameEvent_DamageDealt>().ToArray();
            if (played.Length != 1 || played[0].CardId != id || hits.Length != 2 || !hits.Any(h => ReferenceEquals(h, hit)) || o.IndexOf(played[0]) >= o.IndexOf(hits[0])) return false;
            if (hits.Any(h => h.EnemyIndex != index || h.Amount != 5 || h.HpLoss != 5 || h.Source != "player" || h.SourceUid != null
                || h.PresentationSource is not { Kind: "card", DefinitionId: id } p || p.Uid != source.Uid)) return false;
            return o.ReceiptValid(hit);
        }

        static bool IsOwnedHolyMaulContact(Hd2dOwner o, AttackRoute route)
        {
            // 近似: Godot の TryHolyMaulPrimaryDamage (登録済みの主たる当たり 1 発) = 直接の札・区切りが white_holy_maul の CardPlayed・前の手札・同じコマンドの最初の DamageDealt
            const string id = "white_holy_maul";
            if (!HolyMaulReceiveVfxProfiles.OriginalRoute(route) || !DirectCardCommon(o, id, out _, out int index, true)) return false;
            var first = o.Events.OfType<GameEvent_DamageDealt>().FirstOrDefault(h => h.Source == "player");
            return first != null && ReferenceEquals(first, o.Hit) && o.TargetAgrees(index) && o.ReceiptValid(o.Hit);
        }

        static bool IsOwnedAxialFamilyContact(Hd2dOwner o, AttackRoute route)
        {
            var hit = o.Hit;
            if (!AxialFamilyContactVfxProfiles.Eligible(route) || hit.Source != "player" || hit.SourceUid != null
                || hit.PresentationSource is not { Kind: "card" } source || string.IsNullOrEmpty(source.Uid) || hit.EnemyIndex is not int target
                || o.PlayedCard is not { } played || played.CardId != source.DefinitionId || o.HandCard(source.Uid, source.DefinitionId) == null) return false;
            var before = o.Before;
            if (before == null || !o.TargetAgrees(target) || before.Enemies[target].Hp <= 0) return false;
            int hitIndex = o.IndexOf(hit);
            if (hitIndex < 0 || !o.Events.Take(hitIndex).OfType<GameEvent_CardPlayed>().Any(e => e.CardId == source.DefinitionId)) return false;
            return o.ReceiptValid(hit);
        }

        static bool IsOwnedTripleHornContact(Hd2dOwner o, AttackRoute route)
        {
            const string id = "green_triple_horn"; var hit = o.Hit;
            if (!TripleHornReceiveVfxProfiles.OriginalRoute(route) || o.LeaderId != "leader_green" || !DirectCardCommon(o, id, out var source, out int index, true)) return false;
            var before = o.Before; var after = o.After;
            if (before == null || after == null || before.Player.Energy != 3 || after.Player.Energy != 1 || before.Player.Growth != 0 || before.Player.Momentum != 0 || before.Player.Weak != 0
                || !before.Player.Hand.Any(c => c.Uid == source.Uid && c.Def.Id == id && c.Def.Type == "physical")
                || !o.TargetAgrees(index) || before.Enemies[index].Hp <= 15 || before.Enemies[index].Block != 0) return false;
            var events = o.Events; var played = events.OfType<GameEvent_CardPlayed>().ToArray(); var hits = events.OfType<GameEvent_DamageDealt>().ToArray();
            if (played.Length != 1 || played[0].CardId != id || hits.Length != 3 || !hits.Any(h => ReferenceEquals(h, hit)) || o.IndexOf(played[0]) >= o.IndexOf(hits[0])) return false;
            if (hits.Any(h => h.EnemyIndex != index || h.Amount != 5 || h.HpLoss != 5 || h.Source != "player" || h.SourceUid != null
                || h.PresentationSource is not { Kind: "card", DefinitionId: id } p || p.Uid != source.Uid)) return false;
            return o.ReceiptValid(hit);
        }

        static bool IsOwnedLightBurstContact(Hd2dOwner o, AttackRoute route)
        {
            const string id = "white_light_burst"; var hit = o.Hit;
            if (!LightBurstContactVfxProfiles.OriginalRoute(route) || o.LeaderId != "leader_white" || hit.HpLoss <= 0 || !DirectCardCommon(o, id, out var source, out _, true)) return false;
            var before = o.Before; var after = o.After;
            if (before == null || after == null || before.Player.Light != 1 || after.Player.Light != 0
                || !before.Player.Hand.Any(c => c.Uid == source.Uid && c.Def.Id == id && c.Def.Type == "spell")) return false;
            var live = before.Enemies.Select((e, i) => new { e, i }).Where(x => x.e.Hp > 0).ToArray();
            if (live.Length != 3 || live.Any(x => x.e.Hp <= 4 || x.i >= after.Enemies.Count || x.e.EnemyId != after.Enemies[x.i].EnemyId)) return false;
            var events = o.Events; var played = events.OfType<GameEvent_CardPlayed>().ToArray();
            var discharged = events.OfType<GameEvent_LightDischarged>().ToArray(); var hits = events.OfType<GameEvent_DamageDealt>().ToArray();
            var weak = events.OfType<GameEvent_EnemyWeakened>().ToArray();
            if (played.Length != 1 || played[0].CardId != id || discharged.Length != 1 || discharged[0].Spent != 1 || hits.Length != 3 || weak.Length != 3
                || !hits.Any(h => ReferenceEquals(h, hit)) || o.IndexOf(played[0]) >= o.IndexOf(discharged[0])
                || o.IndexOf(discharged[0]) >= o.IndexOf(hits[0]) || o.IndexOf(hits[2]) >= o.IndexOf(weak[0])) return false;
            for (int i = 0; i < 3; i++) if (hits[i].EnemyIndex != live[i].i || hits[i].Source != "player" || hits[i].Amount != 4 || hits[i].HpLoss <= 0 || hits[i].SourceUid != null
                || hits[i].PresentationSource is not { Kind: "card", DefinitionId: id } p || p.Uid != source.Uid
                || weak[i].EnemyIndex != live[i].i || weak[i].Amount != 1) return false;
            return true;
        }

        static bool IsOwnedLightBoltContact(Hd2dOwner o, AttackRoute route)
        {
            var hit = o.Hit;
            if (!LightBoltContactVfxProfiles.OriginalRoute(route) || hit.Source != "player" || hit.SourceUid != null
                || hit.PresentationSource is not { Kind: "card" } source || !LightBoltContactVfxProfiles.Card(source.DefinitionId)
                || o.PlayedCard is not { } played || played.CardId != source.DefinitionId || hit.EnemyIndex is not int index) return false;
            var card = o.HandCard(source.Uid, source.DefinitionId); var before = o.Before;
            if (before == null || card == null || index < 0 || index >= before.Enemies.Count) return false;
            var values = CombatPresentationLedger.Read(hit);
            if (values == null || values.Target != "enemy" || values.EnemyIndex != index || values.EnemyId != before.Enemies[index].EnemyId || !CombatPresentationLedger.Valid(hit, values, out _)) return false;
            // Resolve payment from the actual played instance: standard Light upgrades can
            // legally reduce LightCost to zero, for which the engine emits no LightSpent.
            int lightCost = card.Def.LightCost.GetValueOrDefault();
            if (lightCost < 0) return false;
            bool playedSeen = false, spent = false;
            foreach (var e in o.Events)
            {
                if (ReferenceEquals(e, hit)) return playedSeen && (lightCost == 0 ? !spent : spent);
                if (e is GameEvent_CardPlayed c && c.CardId == source.DefinitionId) { if (playedSeen) return false; playedSeen = true; }
                if (e is GameEvent_LightSpent l && l.CardId == source.DefinitionId)
                {
                    if (!playedSeen || spent || lightCost == 0 || l.Amount != lightCost) return false;
                    spent = true;
                }
            }
            return false;
        }

        static bool IsOwnedColdSweepContact(Hd2dOwner o, AttackRoute route)
        {
            var hit = o.Hit;
            if (!ColdSweepContactVfxProfiles.OriginalRoute(route) || hit.Source != "player" || hit.HpLoss <= 0
                || hit.PresentationSource is not { Kind: "card", DefinitionId: "blue_cold_sweep" } source
                || o.PlayedCard is not { CardId: "blue_cold_sweep" } || o.HandCard(source.Uid, "blue_cold_sweep") == null || hit.EnemyIndex is not int index || !o.TargetAgrees(index)) return false;
            bool played = false;
            foreach (var e in o.Events) { if (ReferenceEquals(e, hit)) return played; if (e is GameEvent_CardPlayed c && c.CardId == "blue_cold_sweep") played = true; }
            return false;
        }

        /// <summary>敵の技の名前が本物か (Godot の ProvenEnemyAttackMove)。PresentationEnemyAction が解決済みで、同じ敵の EnemyActionExecuting が同じ実行番号で記録にあること</summary>
        static string? ProvenEnemyAttackMove(Hd2dOwner o, string actorId)
        {
            var a = o.Hit.PresentationEnemyAction;
            if (a is not { Version: 1, UnresolvedReason: null } || a.EnemyId != actorId || a.MoveId == null || a.ExecutionEventIndex < 0) return null;
            if (o.Hit.EnemyIndex is not int index) return null;
            var log = o.Log;
            bool executing = log != null && log.Any(e => e is GameEvent_EnemyActionExecuting x && x.EnemyIndex == index && x.Kind == "attack"
                && x.PresentationEnemyAction is { Version: 1, UnresolvedReason: null } b && b.EnemyId == actorId && b.MoveId == a.MoveId
                && b.ExecutionEventIndex == a.ExecutionEventIndex && b.Branch == a.Branch);
            return executing ? a.MoveId : null;
        }
        static bool IsOwnedEnemyMoveContact(Hd2dOwner o, AttackRoute route, string actorId, string moveId)
        {
            var hit = o.Hit;
            if (route.Key != "enemy:" + actorId + "/" + moveId || route.DefinitionId != moveId || route.SourceKind != "enemy_actual_execution" || route.Renderer != "BoundedContactFx" || route.FixedSmall) return false;
            if (hit.Source != "enemy" || hit.HpLoss <= 0 || hit.EnemyIndex is not int index || ProvenEnemyAttackMove(o, actorId) != moveId) return false;
            if (hit.PresentationEnemyAttack is not { ExecutedHits: 1 } multi || multi.EnemyId != actorId) return false;
            var before = o.Before; var after = o.After;
            if (before == null || after == null || index < 0 || index >= before.Enemies.Count || index >= after.Enemies.Count
                || before.Enemies[index].EnemyId != actorId || after.Enemies[index].EnemyId != actorId) return false;
            return o.ReceiptValid(hit);
        }
        static AttackRoute EnemyContactRoute(AttackRoute route, Hd2dOwner o)
        {
            var hit = o.Hit;
            if (hit.Source != "enemy" || hit.EnemyIndex is not int index || o.Before == null || index < 0 || index >= o.Before.Enemies.Count) return route;
            string actorId = o.Before.Enemies[index].EnemyId;
            var selected = EnemyContactVfxProfiles.Select(route, hit, actorId, ProvenEnemyAttackMove(o, actorId), o.After?.EventLog ?? o.Log);
            bool impActor = actorId == "enemy_cinder_imp" && ProvenEnemyAttackMove(o, actorId) == "spark_toss";
            return ImpSparkContactVfxProfiles.Select(selected, hit, impActor && IsOwnedImpSparkContact(o, route));
        }
        static bool IsOwnedImpSparkContact(Hd2dOwner o, AttackRoute route)
        {
            var hit = o.Hit;
            if (!ImpSparkContactVfxProfiles.OriginalRoute(route) || hit.Source != "enemy" || hit.HpLoss <= 0 || hit.EnemyIndex is not int index
                || hit.PresentationEnemyAction is not { Version: 1, EnemyId: "enemy_cinder_imp", MoveId: "spark_toss", UnresolvedReason: null }) return false;
            var before = o.Before; var after = o.After;
            if (before == null || after == null || index < 0 || index >= before.Enemies.Count || index >= after.Enemies.Count
                || before.Enemies[index].EnemyId != "enemy_cinder_imp" || after.Enemies[index].EnemyId != "enemy_cinder_imp") return false;
            return ProvenEnemyAttackMove(o, "enemy_cinder_imp") == "spark_toss";
        }

        // ---------- 支え (ブロック・氷壁・回復) ----------
        /// <summary>Godot の SupportVfxProfiles.Describe: その札 (ownerCard) の主たるプレイで得た最初の 1 回の 守り / 回復 / 氷壁 を Hd2dSupportFx の経路にする。claimed はコマンドごとに 1 回</summary>
        public static AttackRoute? DescribeSupport(GameEvent ev, string? ownerCard, int? modeIndex, ref bool claimed, out float duration)
        {
            duration = 0f;
            if (!Hd2dAttackPrototype.Enabled || claimed || ownerCard == null) return null;
            if (SupportVfxProfiles.OrdinaryGuardOwner(ownerCard, modeIndex) && ev is GameEvent_BlockGained { Target: "player", Amount: > 0, SourceUid: null } block)
            {
                var receipt = CombatPresentationLedger.Read(block);
                if (receipt != null && receipt.Target == "player" && CombatPresentationLedger.Valid(block, receipt, out _)
                    && SupportVfxProfiles.ExactGain(block.Amount, receipt.Before.Block, receipt.After.Block, receipt.Before.Hp, receipt.After.Hp, receipt.Before.MaxHp, receipt.After.MaxHp, receipt.Before.IceBlock, receipt.After.IceBlock))
                {
                    claimed = true; duration = SupportVfxProfiles.GuardDuration; return SupportVfxProfiles.GuardRoute(ownerCard);
                }
            }
            if (SupportVfxProfiles.RecoveryOwner(ownerCard) && ev is GameEvent_HpHealed { Amount: >= 0, SourceUid: null } heal)
            {
                var receipt = CombatPresentationLedger.Read(heal);
                if (receipt != null && receipt.Target == "player" && CombatPresentationLedger.Valid(heal, receipt, out _) && receipt.After.Hp - receipt.Before.Hp == heal.Amount)
                {
                    // First eligible validated owner-action recovery. A capped zero consumes this slot.
                    claimed = true;
                    if (heal.Amount == 0) return null;
                    duration = SupportVfxProfiles.HealDuration; return SupportVfxProfiles.HealRoute(ownerCard);
                }
            }
            if (!SupportVfxProfiles.IceOwner(ownerCard) || ev is not GameEvent_IceBlockGained { Amount: > 0 } gain) return null;
            var iceReceipt = CombatPresentationLedger.Read(gain);
            if (iceReceipt == null || iceReceipt.Target != "player" || !CombatPresentationLedger.Valid(gain, iceReceipt, out _)
                || iceReceipt.After.IceBlock - iceReceipt.Before.IceBlock != gain.Amount) return null;
            claimed = true; duration = SupportVfxProfiles.IceDuration; return SupportVfxProfiles.IceRoute(ownerCard);
        }

        // ---------- 延焼のティック (system:burn) ----------
        public static AttackRoute SelectBurn(GameEvent ev) => StatusContactVfxProfiles.Select(Hd2dRouteCatalog.BurnSystem(), ev, ev is GameEvent_BurnTick);

        // ---------- 光の方針 (Godot の AuthoredGeometryFx.Sample の光の範囲と量) ----------
        /// <summary>選ばれた経路ごとの光の増減 (目標の光の量・床の光の量・床の光の高さの割合)。null は方針なし</summary>
        public static (float targetGain, float floorGain, float floorHeight)? LightPolicy(AttackRoute r)
        {
            if (SupportLightVfx391.Eligible(r)) return (SupportLightVfx391.TargetGain, SupportLightVfx391.FloorGain, SupportLightVfx391.FloorHeight);
            if (LegacyContactVfx419.Eligible(r)) return (LegacyContactVfx419.TargetGain, LegacyContactVfx419.FloorGain, LegacyContactVfx419.FloorHeight);
            if (GreenPhysicalContactVfxProfiles.Eligible(r)) return (GreenPhysicalContactVfxProfiles.TargetGain, GreenPhysicalContactVfxProfiles.FloorGain, GreenPhysicalContactVfxProfiles.FloorHeight);
            if (RootingVine404ContactVfxProfiles.Eligible(r)) return (RootingVine404ContactVfxProfiles.TargetGain, RootingVine404ContactVfxProfiles.FloorGain, RootingVine404ContactVfxProfiles.FloorHeight);
            if (WhiteLight396ContactVfxProfiles.Eligible(r)) return (WhiteLight396ContactVfxProfiles.TargetGain, WhiteLight396ContactVfxProfiles.FloorGain, WhiteLight396ContactVfxProfiles.FloorHeight);
            if (EarthRoarContactVfxProfiles.Eligible(r)) return (EarthRoarContactVfxProfiles.TargetGain, EarthRoarContactVfxProfiles.FloorGain, EarthRoarContactVfxProfiles.FloorHeight);
            if (NeutralPressureContactVfxProfiles.Eligible(r)) return (NeutralPressureContactVfxProfiles.TargetGain, NeutralPressureContactVfxProfiles.FloorGain, NeutralPressureContactVfxProfiles.FloorHeight);
            if (WaterWideContactVfxProfiles.Eligible(r)) return (WaterWideContactVfxProfiles.TargetGain, WaterWideContactVfxProfiles.FloorGain, WaterWideContactVfxProfiles.FloorHeight);
            return null;
        }
    }
}
