// Source-backed enemy BlockGained text only. Existing ContactBeat owns the one
// numeric receipt; this renderer never commits or emits gameplay/presentation events.
#nullable enable
using System;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;
using Godot;
namespace DeckRogue.GodotPort.Presentation;
public partial class BattleStage
{
    // Read-only captured metadata also permits existing reflection QA to inspect
    // identity and style without adding public production instrumentation counters.
    private sealed record SourceEnemyBlockFloat(Label Label, GameEvent_BlockGained Event,
        long Revision, int Generation, int EnemyIndex, Vector2 Origin, Vector2 Size, float Unit, float Start);
    private readonly List<SourceEnemyBlockFloat> _sourceEnemyBlockFloats = new();

    private bool TryShowSourceEnemyBlock(GameEvent_BlockGained gained, BattleActor actor, CombatPresentationReceipt? receipt)
    {
        // Preserve the existing receipt-selected target. Never search for the
        // last executing enemy or invent a new target when the receipt is absent.
        if (gained.Target == "player" || receipt?.Target != "enemy" ||
            actor.EnemyIndex != receipt.EnemyIndex || _sourceFloatHost is null ||
            !GodotObject.IsInstanceValid(_sourceFloatHost)) return false;
        if (actor.SourceUiProxyHeight <= 0) RefreshActorLayout();
        if (actor.SourceUiProxyHeight <= 0 || actor.SourceUiProxyUnit <= 0) return false;
        float unit = actor.SourceUiProxyUnit;
        float textScale = Math.Clamp(_sourceFloatMetrics?.Invoke().TextScale ?? 1f, 1f, 1.3f);
        var proxyFeet = _world.Camera.UnprojectPosition(actor.Home + actor.UiLunge.Offset);
        var origin = SourceEnemyBlockFloatModel.Origin(proxyFeet, actor.SourceUiProxyHeight, actor.ExecutionScale.Y, unit);
        var size = new Vector2(240, 60) * unit;
        int fontSize = Math.Max(1, (int)Math.Round(SourceEnemyBlockFloatModel.FontSize * unit * textScale));
        var label = new Label {
            Name = "SourceEnemyBlockFloat", Text = SourceEnemyBlockFloatModel.Text(gained.Amount),
            Position = origin-size/2, Size=size, PivotOffset=size/2, ZIndex=50,
            MouseFilter=Control.MouseFilterEnum.Ignore, HorizontalAlignment=HorizontalAlignment.Center,
            VerticalAlignment=VerticalAlignment.Center, ClipText=false
        };
        label.AddThemeFontOverride("font", GD.Load<Font>("res://assets/Fonts/KleeOne-SemiBold.ttf"));
        label.AddThemeFontSizeOverride("font_size",fontSize);
        label.AddThemeColorOverride("font_color",new Color(SourceEnemyBlockFloatModel.Color));
        // Same documented TMP-to-native stroke adaptation as accepted damage.
        label.AddThemeColorOverride("font_outline_color",new Color(.05f,.03f,.06f,.95f));
        label.AddThemeConstantOverride("outline_size",Math.Max(1,(int)Math.Round(fontSize*.055f)));
        label.AddThemeColorOverride("font_shadow_color",new Color(0,0,0,.75f));
        label.AddThemeConstantOverride("shadow_offset_x",Math.Max(1,(int)Math.Round(2*unit)));
        label.AddThemeConstantOverride("shadow_offset_y",Math.Max(1,(int)Math.Round(2*unit)));
        label.SetMeta("source_role","enemy_block_float");
        _sourceFloatHost.AddChild(label);
        _sourceEnemyBlockFloats.Add(new SourceEnemyBlockFloat(label,gained,GetEventChange(gained)?.Revision ?? -1,
            _actionQueue.Generation,actor.EnemyIndex,origin,size,unit,_clock));
        SampleSourceEnemyBlockText();
        return true;
    }
    private void SampleSourceEnemyBlockText()
    {
        for (int i=_sourceEnemyBlockFloats.Count-1;i>=0;i--)
        {
            var cue=_sourceEnemyBlockFloats[i];float age=_clock-cue.Start;
            if (!GodotObject.IsInstanceValid(cue.Label)) {_sourceEnemyBlockFloats.RemoveAt(i);continue;}
            if (SourceEnemyBlockFloatModel.Expired(age,cue.Generation,_actionQueue.Generation,_interfaceRoute))
            {cue.Label.QueueFree();_sourceEnemyBlockFloats.RemoveAt(i);continue;}
            // Captured origin/unit survive ordinary rebuilds and later commands.
            cue.Label.Position=cue.Origin-cue.Size/2-new Vector2(0,SourceEnemyBlockFloatModel.Offset(age)*cue.Unit);
            cue.Label.Scale=Vector2.One*SourceEnemyBlockFloatModel.Scale(age);
            cue.Label.Modulate=new Color(1,1,1,SourceEnemyBlockFloatModel.Alpha(age));
        }
    }
    private void CancelSourceEnemyBlockText()
    {
        foreach (var cue in _sourceEnemyBlockFloats) if (GodotObject.IsInstanceValid(cue.Label)) cue.Label.QueueFree();
        _sourceEnemyBlockFloats.Clear();
    }
}
