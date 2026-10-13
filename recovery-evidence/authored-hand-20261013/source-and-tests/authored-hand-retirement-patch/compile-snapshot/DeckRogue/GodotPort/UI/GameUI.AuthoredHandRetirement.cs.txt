// Frozen admission identity; source-authored styling uses the existing retirement
// Tween's start/duration/completion mechanism. No actor or contact ownership.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Core;
namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    private sealed record UxAdmittedHandExit(SourceAuthoredHandPlan Plan,CardTile Tile,Vector2 To,Vector2 CanvasSize,float Unit);
    private sealed class UxAuthoredHandExit
    {
        public UxAdmittedHandExit Admission;public Tween Tween;
        public Vector2 From,FromScale;public float StartAlpha;
    }
    private Func<SessionChange,string,SourceAuthoredHandStyle?> _uxAuthoredHandStyle;
    private readonly Dictionary<string,UxAdmittedHandExit> _uxAdmittedHandExits=new();
    private readonly Dictionary<string,UxAuthoredHandExit> _uxAuthoredHandExits=new();
    public int PendingAuthoredHandExitCount=>_uxAdmittedHandExits.Count;
    public int ActiveAuthoredHandExitCount=>_uxAuthoredHandExits.Count;
    public long AuthoredHandExitStartCount {get;private set;}
    public long AuthoredHandExitCompleteCount {get;private set;}
    public long LastAuthoredHandExitRevision {get;private set;}=-1;
    public string LastAuthoredHandExitUid {get;private set;}="";
    public double LastAuthoredHandRetirementDuration {get;private set;}
    public void SetAdmittedAuthoredHandStyle(Func<SessionChange,string,SourceAuthoredHandStyle?> provider)=>_uxAuthoredHandStyle=provider;
    private void UxCaptureAdmittedHandExit(SessionChange change)
    {
        if(change.IsReset){UxCancelAuthoredHandExits();return;}
        foreach(var pair in _uxAdmittedHandExits.ToArray())
            if(pair.Value.Plan.Epoch!=_uxEpoch||!_uxTiles.TryGetValue(pair.Key,out var live)||!ReferenceEquals(live,pair.Value.Tile)||
                change.Current.Combat?.Player.Hand.Any(c=>c.Uid==pair.Key)==true)_uxAdmittedHandExits.Remove(pair.Key);
        if(change.Command is not RunCommand_Combat{Command:Command_PlayCard play}||change.Previous?.Combat is not {} before||change.Current.Combat is not {} after)return;
        string uid=play.CardUid;var card=before.Player.Hand.FirstOrDefault(c=>c.Uid==uid);
        if(card==null||after.Player.Hand.Any(c=>c.Uid==uid)||after.Player.SetCards.Any(c=>c.Uid==uid)||after.Player.Permanents.Any(c=>c.Uid==uid)||
            !_uxTiles.TryGetValue(uid,out var tile)||!GodotObject.IsInstanceValid(tile)||tile.IsQueuedForDeletion()||tile.GetParent()!=_uxHand||
            tile.GetMeta("definition_id","").AsString()!=card.Def.Id)return;
        var proof=_uxAuthoredHandStyle?.Invoke(change,uid);
        if(proof is not {} style||!style.Matches(change.Revision,uid,card.Def.Id))return;
        bool exhaust=after.Player.ExhaustPile.Any(c=>c.Uid==uid);Vector2 to=tile.Position;
        if(!exhaust)
        {
            // Freeze the actual currently shown pile before Rebuild destroys it.
            // A missing anchor retains the old fade rather than inventing geometry.
            if(_uxHand==null||!GodotObject.IsInstanceValid(_uxHand)||_uxBattle==null||!GodotObject.IsInstanceValid(_uxBattle)||_uxBattle.IsQueuedForDeletion())return;
            var pile=_uxBattle.GetNodeOrNull<Control>("DiscardPile");
            if(pile==null||!GodotObject.IsInstanceValid(pile)||pile.IsQueuedForDeletion())return;
            to=_uxHand.GetGlobalTransformWithCanvas().AffineInverse()*(pile.GetGlobalTransformWithCanvas()*(pile.Size/2))-tile.PivotOffset;
        }
        // Query the existing duration owner once, at this successful admission.
        // Never derive style from this number or reread a later active command.
        var plan=new SourceAuthoredHandPlan(_uxEpoch,change.Revision,uid,card.Def.Id,style,
            _uxRetirement?.Invoke(card.Def.Id)??.25,exhaust,play.TargetIndex);
        if(plan.Valid)_uxAdmittedHandExits[uid]=new UxAdmittedHandExit(plan,tile,to,Size,BattleUnit);
    }
    private bool UxAuthoredHandExitIsCurrent(string uid,UxAuthoredHandExit motion)
    {
        var a=motion.Admission;
        return a.Plan.Epoch==_uxEpoch&&a.Plan.CardUid==uid&&a.Plan.Valid&&_uxAuthoredHandExits.TryGetValue(uid,out var current)&&ReferenceEquals(current,motion)&&
            !_uxTiles.ContainsKey(uid)&&GodotObject.IsInstanceValid(a.Tile)&&!a.Tile.IsQueuedForDeletion()&&a.Tile.GetParent()==_uxHand&&
            CurrentRoute=="combat"&&_overlay==null&&_uxHand!=null&&GodotObject.IsInstanceValid(_uxHand)&&_uxHand.IsVisibleInTree()&&
            a.CanvasSize==Size&&Math.Abs(a.Unit-BattleUnit)<.0001f;
    }
    private bool UxTryStartAuthoredHandExit(string uid,CardTile tile)
    {
        if(!_uxAdmittedHandExits.Remove(uid,out var a))return false;
        if(a.Plan.Epoch!=_uxEpoch||a.Plan.CardUid!=uid||!a.Plan.Valid||!ReferenceEquals(a.Tile,tile)||a.CanvasSize!=Size||Math.Abs(a.Unit-BattleUnit)>.0001f||
            !GodotObject.IsInstanceValid(tile)||tile.IsQueuedForDeletion()||tile.GetParent()!=_uxHand||CurrentRoute!="combat"||_overlay!=null)return false;
        UxCancelAuthoredHandExit(uid);tile.MouseFilter=MouseFilterEnum.Ignore;tile.Disabled=true;tile.AllowDrag=false;tile.SetProcess(false);tile.ZIndex=0;
        var motion=new UxAuthoredHandExit{Admission=a,From=tile.Position,FromScale=tile.Scale,StartAlpha=tile.Modulate.A};
        _uxAuthoredHandExits[uid]=motion;_uxHand.MoveChild(tile,_uxHand.GetChildCount()-1);
        // Same CreateTween and one-duration-plus-callback sequence as the old
        // retirement. Only the normalized visual sampling replaces alpha-only.
        var tween=CreateTween();motion.Tween=tween;
        tween.TweenMethod(Callable.From<float>(k=>UxSampleAuthoredHandExit(uid,motion,k)),0f,1f,a.Plan.Duration);
        tween.TweenCallback(Callable.From(()=>UxCompleteAuthoredHandExit(uid,motion)));
        AuthoredHandExitStartCount++;LastAuthoredHandExitRevision=a.Plan.Revision;LastAuthoredHandExitUid=uid;LastAuthoredHandRetirementDuration=a.Plan.Duration;
        return true;
    }
    private void UxSampleAuthoredHandExit(string uid,UxAuthoredHandExit motion,float normalized)
    {
        if(!UxAuthoredHandExitIsCurrent(uid,motion)){UxCancelAuthoredHandExit(uid,motion);return;}
        var a=motion.Admission;var pose=SourceAuthoredHandMotion.Sample(normalized,a.Plan.Exhaust,motion.StartAlpha);
        a.Tile.Position=motion.From.Lerp(a.To,pose.PositionFraction);a.Tile.Scale=motion.FromScale.Lerp(Vector2.One*(pose.EndScale*a.Unit),pose.ScaleFraction);
        var color=a.Tile.Modulate;color.A=pose.Alpha;a.Tile.Modulate=color;
    }
    private void UxCompleteAuthoredHandExit(string uid,UxAuthoredHandExit motion)
    {
        if(!UxAuthoredHandExitIsCurrent(uid,motion)){UxCancelAuthoredHandExit(uid,motion);return;}
        _uxAuthoredHandExits.Remove(uid);AuthoredHandExitCompleteCount++;
        if(GodotObject.IsInstanceValid(motion.Admission.Tile))motion.Admission.Tile.QueueFree();
    }
    private void UxCancelAuthoredHandExit(string uid,UxAuthoredHandExit expected=null)
    {
        // A stale callback may never cancel a newer same-UID visual owner.
        if(expected==null)_uxAdmittedHandExits.Remove(uid);
        if(!_uxAuthoredHandExits.TryGetValue(uid,out var motion)||(expected!=null&&!ReferenceEquals(expected,motion)))return;
        _uxAuthoredHandExits.Remove(uid);
        if(motion.Tween!=null&&GodotObject.IsInstanceValid(motion.Tween)&&motion.Tween.IsValid())motion.Tween.Kill();
        var tile=motion.Admission.Tile;
        if(GodotObject.IsInstanceValid(tile)&&!tile.IsQueuedForDeletion()&&(!_uxTiles.TryGetValue(uid,out var live)||!ReferenceEquals(live,tile))){tile.Hide();tile.QueueFree();}
    }
    private void UxCancelAuthoredHandExits()
    {
        _uxAdmittedHandExits.Clear();foreach(var uid in _uxAuthoredHandExits.Keys.ToArray())UxCancelAuthoredHandExit(uid);
    }
    private void UxCheckAuthoredHandExits()
    {
        bool visible=CurrentRoute=="combat"&&_overlay==null&&_uxHand!=null&&GodotObject.IsInstanceValid(_uxHand)&&_uxHand.IsVisibleInTree();
        foreach(var pair in _uxAdmittedHandExits.ToArray())
        {
            var a=pair.Value;
            if(!visible||a.Plan.Epoch!=_uxEpoch||a.CanvasSize!=Size||Math.Abs(a.Unit-BattleUnit)>.0001f||
                !_uxTiles.TryGetValue(pair.Key,out var live)||!ReferenceEquals(live,a.Tile)||!GodotObject.IsInstanceValid(a.Tile)||a.Tile.IsQueuedForDeletion())_uxAdmittedHandExits.Remove(pair.Key);
        }
        foreach(var pair in _uxAuthoredHandExits.ToArray())if(!UxAuthoredHandExitIsCurrent(pair.Key,pair.Value))UxCancelAuthoredHandExit(pair.Key,pair.Value);
    }
    private void UxRestoreAuthoredHandExitOrder()
    {
        foreach(var pair in _uxAuthoredHandExits)if(UxAuthoredHandExitIsCurrent(pair.Key,pair.Value))_uxHand.MoveChild(pair.Value.Admission.Tile,_uxHand.GetChildCount()-1);
    }
}
