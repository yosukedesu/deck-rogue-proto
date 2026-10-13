// Read-only admission proof. No action launch, queue, contact or clock mutation.
using System;
using System.Linq;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Core;
using DeckRogue.GodotPort.UI;
namespace DeckRogue.GodotPort.Presentation;
public partial class BattleStage
{
    public SourceAuthoredHandStyle? GetAdmittedAuthoredHandStyle(SessionChange change,string uid)
    {
        if(change==null||change.IsReset||!SourceShowcaseEnabled||
            change.Command is not RunCommand_Combat{Command:Command_PlayCard play}||play.CardUid!=uid||
            !_commands.Any(owner=>ReferenceEquals(owner.Change,change)))return null;
        var card=change.Previous?.Combat?.Player.Hand.FirstOrDefault(c=>c.Uid==uid);
        if(card==null||SourceShowcaseProfiles.ForCard(card.Def.Id) is not {AuthoredAttack:not null} profile)return null;
        var proof=new SourceAuthoredHandStyle(change.Revision,uid,card.Def.Id,profile.AuthoredAttack);
        return proof.Matches(change.Revision,uid,card.Def.Id)?proof:null;
    }
}
