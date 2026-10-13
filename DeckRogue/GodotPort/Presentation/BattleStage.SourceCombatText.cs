// Source-backed player damage float reconstruction. Renderer adaptations are documented
// in HANDOFF.md; no mutation of receipts, engine state, targeting or RNG.
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using DeckRogue.Engine.Generated;
using Godot;
namespace DeckRogue.GodotPort.Presentation;
public partial class BattleStage
{
    private sealed record SourceFloat(Label Label, Vector2 Origin, Vector2 Size, float Unit, float Start);
    private readonly List<SourceFloat> _sourceFloats = new();
    private CanvasLayer? _sourceFloatHost;
    private Func<(float Unit, float TextScale)>? _sourceFloatMetrics;
    public int SourceCombatFloatCount => _sourceFloats.Count;
    public int SourceCombatFloatsCreated { get; private set; }
    public string LastSourceCombatFloatText { get; private set; } = "";

    // Host is the existing interface canvas: Z=50 sits over battle HUD and below
    // its Z=100 popup scrim. Labels are siblings of GameUI, so hand rebuilds cannot
    // destroy an already-landed receipt's float.
    public void SetSourceCombatTextHost(CanvasLayer host, Func<(float Unit, float TextScale)> metrics)
    {
        CancelSourceCombatText();
        _sourceFloatHost = host;
        _sourceFloatMetrics = metrics;
    }
    private bool TryShowSourceDamage(GameEvent_DamageDealt hit, int loss, int absorbed)
    {
        if (hit.Source != "player" || _sourceFloatHost is null ||
            !GodotObject.IsInstanceValid(_sourceFloatHost) || Enemy(hit.EnemyIndex) is not {} actor)
            return false;
        var metrics = _sourceFloatMetrics?.Invoke() ?? (GetViewport().GetVisibleRect().Size.Y / (_world.PhoneLayout ? 675f : 1080f), 1f);
        float unit = Math.Max(.01f, metrics.Item1), textScale = Math.Clamp(metrics.Item2, 1, 1.3f);
        // Source UI sprite proxy is bottom-pinned above its seat. Its world
        // billboard alone receives feet padding, approach/recoil and breathing.
        // Keep the unrounded base-art height, even while animation frames grow.
        if(actor.SourceUiProxyHeight<=0)RefreshActorLayout();
        if(actor.SourceUiProxyHeight<=0)return false;
        var proxyFeet=_world.Camera.UnprojectPosition(actor.Home+actor.UiLunge.Offset);
        // Horizontal UnityEngine.Random +/-30 remains deliberately centered.
        var center=SourceFloatProxyModel.PlayerDamageOrigin(proxyFeet,actor.SourceUiProxyHeight,actor.ExecutionScale.Y,actor.SourceUiProxyUnit);
        var size = new Vector2(240, 60)*unit;
        int fontSize = Math.Max(1, (int)Math.Round(SourceCombatFloatModel.PlayerSize(hit.Amount, hit.Exposed==true)*unit*textScale));
        var label = new Label {
            Name = "SourceDamageFloat", Text = hit.Amount.ToString(CultureInfo.InvariantCulture),
            Position = center-size/2, Size=size, PivotOffset=size/2, ZIndex=50,
            MouseFilter=Control.MouseFilterEnum.Ignore, HorizontalAlignment=HorizontalAlignment.Center,
            VerticalAlignment=VerticalAlignment.Center, ClipText=false
        };
        label.AddThemeFontOverride("font", GD.Load<Font>("res://assets/Fonts/KleeOne-SemiBold.ttf"));
        label.AddThemeFontSizeOverride("font_size",fontSize);
        label.AddThemeColorOverride("font_color",new Color(SourceCombatFloatModel.PlayerColor(hit.Amount,loss,absorbed)));
        // TMP outlineWidth is SDF-relative, not a pixel width. These restrained
        // native strokes are an explicit renderer adaptation for pixel review.
        label.AddThemeColorOverride("font_outline_color",new Color(.05f,.03f,.06f,.95f));
        label.AddThemeConstantOverride("outline_size",Math.Max(1,(int)Math.Round(fontSize*.055f)));
        label.AddThemeColorOverride("font_shadow_color",new Color(0,0,0,.75f));
        label.AddThemeConstantOverride("shadow_offset_x",Math.Max(1,(int)Math.Round(2*unit)));
        label.AddThemeConstantOverride("shadow_offset_y",Math.Max(1,(int)Math.Round(2*unit)));
        label.SetMeta("source_role","player_damage_float");
        label.SetMeta("source_amount",hit.Amount);label.SetMeta("source_hp_loss",loss);
        _sourceFloatHost.AddChild(label);
        _sourceFloats.Add(new SourceFloat(label,center,size,unit,_clock));
        SourceCombatFloatsCreated++;LastSourceCombatFloatText=label.Text;
        SampleSourceCombatText();
        return true;
    }
    private void SampleSourceCombatText()
    {
        for (int i=_sourceFloats.Count-1;i>=0;i--)
        {
            var cue=_sourceFloats[i];float age=_clock-cue.Start;
            if (!GodotObject.IsInstanceValid(cue.Label)) {_sourceFloats.RemoveAt(i);continue;}
            if (age>=SourceCombatFloatModel.Lifetime || _interfaceRoute!="combat")
            {cue.Label.QueueFree();_sourceFloats.RemoveAt(i);continue;}
            cue.Label.Position=cue.Origin-cue.Size/2-new Vector2(0,SourceCombatFloatModel.Offset(age)*cue.Unit);
            cue.Label.Scale=Vector2.One*SourceCombatFloatModel.Scale(age);
            cue.Label.Modulate=new Color(1,1,1,SourceCombatFloatModel.Alpha(age));
        }
    }
    private void CancelSourceCombatText()
    {
        foreach (var cue in _sourceFloats) if (GodotObject.IsInstanceValid(cue.Label)) cue.Label.QueueFree();
        _sourceFloats.Clear();
    }
}
