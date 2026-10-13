// Source-duration hover poses with explicit managed ownership. No input eligibility,
// command, engine state, queue, or RNG changes; Godot's pressed CardTile keeps its UID.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    private sealed class UxHandHoverPose
    {
        public CardTile Tile;public long Epoch;public SourceHandHoverClock Clock;
        public Vector2 From,To,FromScale,ToScale,CanvasSize;public float Unit;
    }
    private readonly Dictionary<string,UxHandHoverPose> _uxHandHoverPoses=new();
    public int ActiveHandHoverCount=>_uxHandHoverPoses.Count;
    private bool UxHandHoverIsCurrent(string uid,UxHandHoverPose p)
        =>p.Epoch==_uxEpoch&&!p.Clock.Cancelled&&_uxHandHoverPoses.TryGetValue(uid,out var active)&&ReferenceEquals(p,active)
        &&_uxTiles.TryGetValue(uid,out var tile)&&ReferenceEquals(tile,p.Tile)&&GodotObject.IsInstanceValid(tile)&&!tile.IsQueuedForDeletion();
    private void UxStartHandHover(string uid,CardTile tile,Vector2 to,Vector2 scale,float degrees)
    {
        UxCancelHandDeal(uid,false);
        if(_uxHandHoverPoses.TryGetValue(uid,out var current)&&UxHandHoverIsCurrent(uid,current)&&current.To==to&&current.ToScale==scale)return;
        UxCancelHandHover(uid,false);
        // Rotation is immediate in Unity's enter/exit callbacks. Position and
        // scale start unchanged at t=0; the next process frame advances them.
        tile.RotationDegrees=degrees;
        _uxHandHoverPoses[uid]=new UxHandHoverPose{Tile=tile,Epoch=_uxEpoch,Clock=new SourceHandHoverClock(Godot.Engine.GetProcessFrames()),From=tile.Position,To=to,FromScale=tile.Scale,ToScale=scale,CanvasSize=Size,Unit=BattleUnit};
    }
    private bool UxKeepHandHover(string uid,CardTile tile)
    {
        if(!_uxHandHoverPoses.TryGetValue(uid,out var p))return false;
        if(!UxHandHoverIsCurrent(uid,p)||p.CanvasSize!=Size||Math.Abs(p.Unit-BattleUnit)>.0001f){UxCancelHandHover(uid,false);return false;}
        // Sync may have changed fan membership or playability while this card
        // was returning. A non-hovered return must not finish at its old seat.
        if(_uxHover!=uid&&_uxSeats.TryGetValue(uid,out var seat)&&
            (p.To!=seat.Pos||p.ToScale!=Vector2.One*(UxCardScale*BattleUnit)||Math.Abs(tile.RotationDegrees-seat.Rotation)>.001f))
        {UxCancelHandHover(uid,false);return false;}
        return true;
    }
    private void UxCancelHandHover(string uid,bool settle)
    {
        if(!_uxHandHoverPoses.Remove(uid,out var p))return;p.Clock.Cancel();var tile=p.Tile;
        if(settle&&GodotObject.IsInstanceValid(tile)&&!tile.IsQueuedForDeletion()&&_uxTiles.TryGetValue(uid,out var current)&&ReferenceEquals(current,tile)&&_uxSeats.TryGetValue(uid,out var seat))
        {tile.Position=seat.Pos;tile.Scale=Vector2.One*(UxCardScale*BattleUnit);tile.RotationDegrees=seat.Rotation;tile.ZIndex=0;}
    }
    private void UxCancelHandHovers(bool settle)
    {
        foreach(var uid in _uxHandHoverPoses.Keys.ToArray())UxCancelHandHover(uid,settle);
        if(!settle)return;
        // Completed hover poses no longer have a clock, but their hover intent
        // must still be cleared when a modal/route takes ownership.
        string hovered=_uxHover;_uxHover=null;
        if(hovered!=null&&_uxTiles.TryGetValue(hovered,out var tile)&&GodotObject.IsInstanceValid(tile)&&!tile.IsQueuedForDeletion()&&_uxSeats.TryGetValue(hovered,out var seat))
        {tile.Position=seat.Pos;tile.Scale=Vector2.One*(UxCardScale*BattleUnit);tile.RotationDegrees=seat.Rotation;tile.ZIndex=0;}
        if(_uxHand!=null&&GodotObject.IsInstanceValid(_uxHand)&&!_uxHand.IsQueuedForDeletion())RestoreUxHandOrder();
    }
    private void UxTickHandHovers(double delta)
    {
        if(CurrentRoute!="combat"||_overlay!=null||_uxHand==null||!GodotObject.IsInstanceValid(_uxHand)||!_uxHand.IsVisibleInTree())
        {if(_uxHandHoverPoses.Count>0||_uxHover!=null)UxCancelHandHovers(true);return;}
        if(_uxHandHoverPoses.Count==0)return;
        ulong frame=Godot.Engine.GetProcessFrames();
        foreach(var pair in _uxHandHoverPoses.ToArray())
        {
            var p=pair.Value;string uid=pair.Key;
            if(!UxHandHoverIsCurrent(uid,p)){UxCancelHandHover(uid,false);continue;}
            if(_uxDrag==uid){UxCancelHandHover(uid,false);continue;}
            if(p.CanvasSize!=Size||Math.Abs(p.Unit-BattleUnit)>.0001f){UxCancelHandHover(uid,true);continue;}
            p.Clock.Advance(delta,frame);float k=SourceHandHoverMotion.Fraction(p.Clock.Elapsed);
            p.Tile.Position=p.From.Lerp(p.To,k);p.Tile.Scale=p.FromScale.Lerp(p.ToScale,k);
            if(p.Clock.Complete)_uxHandHoverPoses.Remove(uid);
        }
    }
}
