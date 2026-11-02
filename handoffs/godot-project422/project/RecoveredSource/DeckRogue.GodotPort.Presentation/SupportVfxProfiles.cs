using System;
using System.Linq;
using DeckRogue.GodotPort.Core;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;

namespace DeckRogue.GodotPort.Presentation;

// Exact owning-card allowlists; these are receiver-local support events, never hits.
internal static class SupportVfxProfiles
{
    internal static bool IceWallRoute(AttackRoute? route)=>Hd2dAttackPrototype.Enabled && route is not null
        && route.Key=="support:blue_ice_wall/ice-gain" && route.DefinitionId=="blue_ice_wall"
        && route.SourceKind=="support" && route.Family=="ice_guard_gain"
        && route.Renderer=="Hd2dSupportFx" && route.Element=="ice" && route.FixedSmall;
    internal static bool MassHealRoute(AttackRoute? route)=>Hd2dAttackPrototype.Enabled && route is not null
        && route.Key=="support:white_mass_heal/heal" && route.DefinitionId=="white_mass_heal"
        && route.SourceKind=="support" && route.Family=="prayer_heal"
        && route.Renderer=="Hd2dSupportFx" && route.Element=="light" && route.FixedSmall;
    private static bool IceOwner(string? id)=>id is "blue_guard" or "blue_thick_ice" or "blue_ice_wall" or "blue_frost_chain";
    private static bool RecoveryOwner(string? id)=>id is "white_prayer_verse" or "white_mass_heal";
    internal sealed record OwnerContext(string CardId,string CardUid,int? ModeIndex,long Revision);
    internal static OwnerContext? PrimaryOwner(SessionChange change,GameEvent_CardPlayed? played)
    {
        if(change.IsReset || played==null || change.Command is not RunCommand_Combat { Command: Command_PlayCard command }
            || string.IsNullOrEmpty(command.CardUid)
            || !ReferenceEquals(change.Events.OfType<GameEvent_CardPlayed>().FirstOrDefault(),played))return null;
        var card=change.Previous?.Combat?.Player.Hand.FirstOrDefault(c=>c.Uid==command.CardUid);
        if(card==null || card.Def.Id!=played.CardId)return null;
        return new OwnerContext(card.Def.Id,command.CardUid,command.ModeIndex,change.Revision);
    }
    private static bool OrdinaryGuardOwner(OwnerContext? owner,string? card)=>owner!=null && owner.CardId==card
        && (card is "green_guard" or "white_guard" or "green_catalyst_light" or "red_catalyst_light" or "white_light_hoard" || (card=="green_entangle" && owner.ModeIndex==0));
    internal static PresentationBeat Describe(PresentationBeat original,string? ownerCard,ref bool claimed,OwnerContext? context=null)
    {
        if(!Hd2dAttackPrototype.Enabled || claimed)return original;
        if(OrdinaryGuardOwner(context,ownerCard)
            && original.Event is GameEvent_BlockGained { Target:"player",Amount: >0,SourceUid:null } block)
        {
            var receipt=CombatPresentationLedger.Read(block);
            if(receipt!=null && receipt.Target=="player" && CombatPresentationLedger.Valid(block,receipt,out _)
                && SourceHeroBlockFloatModel.ExactGain(block.Amount,receipt.Before.Block,receipt.After.Block,
                    receipt.Before.Hp,receipt.After.Hp,receipt.Before.MaxHp,receipt.After.MaxHp,
                    receipt.Before.IceBlock,receipt.After.IceBlock))
            {
                // Exact primary command/mode plus accepted owner-action ordinary
                // gain, not a mode inferred from missing damage or a passive UID.
                claimed=true;
                var route=new AttackRoute($"support:{ownerCard}/block",ownerCard!,"support","ordinary_guard_gain",
                    "Hd2dSupportFx","physical",true,"validated primary-command ordinary block");
                var scenario=new EffectScenario(EffectTier.Small,0f,.016f,.174f,.31f,1,.6f,.20f);
                return original with { Route=route,Scenario=scenario,Duration=scenario.Duration,ContactAt=0f };
            }
        }
        if(RecoveryOwner(ownerCard)
            && original.Event is GameEvent_HpHealed { Amount: >=0, SourceUid: null } heal)
        {
            var receipt=CombatPresentationLedger.Read(heal);
            if(receipt!=null && receipt.Target=="player" && CombatPresentationLedger.Valid(heal,receipt,out _)
                && receipt.After.Hp-receipt.Before.Hp==heal.Amount)
            {
                // First eligible validated owner-action recovery, not a universal
                // effect-level provenance claim. A capped zero consumes this slot:
                // later independent/passive healing must not become this flourish.
                claimed=true;
                if(heal.Amount==0)return original;
                var route=new AttackRoute($"support:{ownerCard}/heal",ownerCard!,"support","prayer_heal",
                    "Hd2dSupportFx","light",true,"validated owner-action recovery");
                var scenario=new EffectScenario(EffectTier.Small,0f,.016f,.244f,.40f,1,.6f,.35f);
                return original with { Route=route,Scenario=scenario,Duration=scenario.Duration,ContactAt=0f };
            }
        }
        if(!IceOwner(ownerCard) || original.Event is not GameEvent_IceBlockGained { Amount: >0 } gain)return original;
        var iceReceipt=CombatPresentationLedger.Read(gain);
        if(iceReceipt==null || iceReceipt.Target!="player" || !CombatPresentationLedger.Valid(gain,iceReceipt,out _)
            || iceReceipt.After.IceBlock-iceReceipt.Before.IceBlock!=gain.Amount)return original;
        // No source UID exists on IceBlockGained: select one accepted gain in this
        // owning action, not an unverifiable direct-card cause. Preserve later gains.
        claimed=true;
        var iceRoute=new AttackRoute($"support:{ownerCard}/ice-gain",ownerCard!,"support","ice_guard_gain",
            "Hd2dSupportFx","ice",true,"validated owner-action defensive gain");
        var iceScenario=new EffectScenario(EffectTier.Small,0f,.016f,.144f,.36f,1,.6f,.45f);
        return original with { Route=iceRoute,Scenario=iceScenario,Duration=iceScenario.Duration,ContactAt=0f };
    }
}
