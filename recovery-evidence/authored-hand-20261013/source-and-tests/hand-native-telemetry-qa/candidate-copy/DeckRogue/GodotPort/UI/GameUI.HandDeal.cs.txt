// Incoming hand motion only. No input gate, command, rule or RNG ownership.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    sealed class UxHandDeal
    {
        public CardTile Tile;public long Epoch;public SourceHandDealClock Clock;
        public Vector2 StartPosition,CanvasSize;public float StartScale,StartDegrees,Unit;
    }
    readonly Dictionary<string,UxHandDeal> _uxDeals=new();
    public int ActiveHandDealCount=>_uxDeals.Count;
    public long HandDealStartCueCount {get;private set;}
    private bool UxDealIsCurrent(string uid,UxHandDeal deal)
        =>deal.Epoch==_uxEpoch&&!deal.Clock.Cancelled&&_uxDeals.TryGetValue(uid,out var active)&&ReferenceEquals(active,deal)
        &&_uxTiles.TryGetValue(uid,out var tile)&&ReferenceEquals(tile,deal.Tile)&&GodotObject.IsInstanceValid(tile)&&!tile.IsQueuedForDeletion();
    private bool UxRemovedPlayedCard(IEnumerable<string> removed)
    {
        // Compare an exact command UID with this pass's removed UIDs. An old
        // CardPlayed somewhere in an event log does not authorize zero delay.
        return _activeChange?.IsReset!=true&&_activeChange?.Command is RunCommand_Combat combat
            &&combat.Command is Command_PlayCard play&&removed.Contains(play.CardUid);
    }
    private Vector2 UxDrawCenter()
    {
        var pile=_uxBattle?.GetNodeOrNull<Control>("DrawPile");
        if(pile!=null&&GodotObject.IsInstanceValid(pile))
            return _uxHand.GetGlobalTransformWithCanvas().AffineInverse()*(pile.GetGlobalTransformWithCanvas()*(pile.Size/2));
        // Exact Unity missing-anchor fallback, converted from HandLayer's
        // centered x/up-positive coordinates to this full-screen Godot layer.
        float cs=UxCardScale,u=BattleUnit,handY=PhoneLayout?-3:11;
        return new Vector2(Size.X/2-900*u,Size.Y-(handY+(290*cs+40)/2-100)*u);
    }
    private void UxStartHandDeal(string uid,CardTile tile,double delay)
    {
        UxCancelHandDeal(uid,false);
        var deal=new UxHandDeal{Tile=tile,Epoch=_uxEpoch,Clock=new SourceHandDealClock(delay),CanvasSize=Size,Unit=BattleUnit};
        tile.Position=UxDrawCenter()-tile.PivotOffset;tile.Scale=Vector2.One*(SourceHandDealMotion.InitialScale*BattleUnit);tile.RotationDegrees=SourceHandDealMotion.InitialGodotDegrees;
        deal.StartPosition=tile.Position;deal.StartScale=tile.Scale.Y;deal.StartDegrees=tile.RotationDegrees;
        SourceHandCardBack.Add(tile);_uxDeals[uid]=deal;UxDealStartCue(uid,deal);
    }
    private void UxDealStartCue(string uid,UxHandDeal deal)
    {
        if(!UxDealIsCurrent(uid,deal)||!deal.Clock.TakeStartCue())return;
        deal.StartPosition=deal.Tile.Position;deal.StartScale=deal.Tile.Scale.Y;deal.StartDegrees=deal.Tile.RotationDegrees;
        HandDealStartCueCount++;Sound("CardsDrawn");
    }
    private bool UxKeepHandDeal(string uid,CardTile tile)
    {
        if(!_uxDeals.TryGetValue(uid,out var deal))return false;
        if(!UxDealIsCurrent(uid,deal)||deal.CanvasSize!=Size||Math.Abs(deal.Unit-BattleUnit)>.0001f){UxCancelHandDeal(uid,false);return false;}
        // Fill refreshes only SourceCardFace and adds it last; keep the back
        // above it until the midpoint, without recreating clock/audio/UID.
        if(!deal.Clock.Started||deal.Clock.Elapsed<SourceHandDealMotion.Duration*.5)SourceHandCardBack.Raise(tile);
        return true;
    }
    private void UxCancelHandDeal(string uid,bool settle)
    {
        if(!_uxDeals.Remove(uid,out var deal))return;
        deal.Clock.Cancel();var tile=deal.Tile;
        if(!GodotObject.IsInstanceValid(tile)||tile.IsQueuedForDeletion())return;
        SourceHandCardBack.Remove(tile);
        if(settle&&_uxTiles.TryGetValue(uid,out var live)&&ReferenceEquals(live,tile)&&_uxSeats.TryGetValue(uid,out var seat))
        {tile.Position=seat.Pos;tile.RotationDegrees=seat.Rotation;tile.Scale=Vector2.One*(UxCardScale*BattleUnit);}
    }
    private void UxCancelHandDeals(bool settle)
    {
        foreach(string uid in _uxDeals.Keys.ToArray())UxCancelHandDeal(uid,settle);
    }
    private void UxTickHandDeals(double delta)
    {
        if(_uxDeals.Count==0)return;
        if(CurrentRoute!="combat"||_overlay!=null||_uxHand==null||!GodotObject.IsInstanceValid(_uxHand)||!_uxHand.IsVisibleInTree())
        {UxCancelHandDeals(true);return;}
        foreach(var entry in _uxDeals.ToArray())
        {
            string uid=entry.Key;var deal=entry.Value;
            if(!UxDealIsCurrent(uid,deal)){UxCancelHandDeal(uid,false);continue;}
            if(deal.CanvasSize!=Size||Math.Abs(deal.Unit-BattleUnit)>.0001f){UxCancelHandDeal(uid,true);continue;}
            if(_uxHover==uid||_uxDrag==uid){UxCancelHandDeal(uid,false);continue;}
            deal.Clock.Advance(delta);UxDealStartCue(uid,deal);
            if(!UxDealIsCurrent(uid,deal)||!deal.Clock.Started||!_uxSeats.TryGetValue(uid,out var seat))continue;
            var pose=SourceHandDealMotion.Sample(deal.Clock.Elapsed,deal.StartScale,UxCardScale*BattleUnit,deal.StartDegrees,seat.Rotation);
            var tile=deal.Tile;tile.Position=deal.StartPosition.Lerp(seat.Pos,pose.PositionFraction);tile.Scale=new Vector2(pose.ScaleX,pose.ScaleY);tile.RotationDegrees=pose.Degrees;
            if(!pose.BackVisible)SourceHandCardBack.Remove(tile);
            if(pose.Complete)UxCancelHandDeal(uid,true);
        }
    }
}
