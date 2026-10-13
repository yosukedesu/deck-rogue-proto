// RECONSTRUCTED accepted actor display binding; see bundle PROVENANCE.md.
using System;
using System.Linq;
using Godot;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Presentation;

public partial class BattleActor
{
    private int _sourceDisplayAct;
    private int _sourceDisplayLutKey;
    private ImageTexture? _sourceDisplayLut;
    private bool _sourceDisplayRequested = !OS.GetCmdlineUserArgs().Contains("--actor-color-baseline");
    private bool _otherActSourceDisplayRequested = !OS.GetCmdlineUserArgs().Contains("--actor-color-other-acts-baseline");
    private bool _sourceDisplayLifecycleBound;
    public bool SourceDisplayTransferActive { get; private set; }
    public string SourceDisplayTransferStatus { get; private set; } = "legacy:not-configured";
    public bool OtherActSourceDisplayRequested => _otherActSourceDisplayRequested;
    public bool SourceDisplayTransferRequested => _sourceDisplayRequested;

    public void SetSourceDisplayTransferEnabled(bool enabled)
    {
        _sourceDisplayRequested = enabled && !OS.GetCmdlineUserArgs().Contains("--actor-color-baseline");
        RefreshSourceDisplayTransfer();
    }
    public void SetOtherActSourceDisplayEnabled(bool enabled)
    {
        _otherActSourceDisplayRequested = enabled && !OS.GetCmdlineUserArgs().Contains("--actor-color-other-acts-baseline");
        RefreshSourceDisplayTransfer();
    }
    public void RefreshSourceDisplayTransfer() => RefreshSourceMaterial();
    private void EnsureSourceDisplayLifecycle()
    {
        if (_sourceDisplayLifecycleBound) return;
        // Built-in native signal works with the recovered project's existing generated bridge.
        TreeExiting += RetireSourceDisplay;
        _sourceDisplayLifecycleBound = true;
    }
    private void RetireSourceDisplay()
    {
        SourceDisplayTransferActive = false;
        SourceDisplayTransferStatus = "legacy:actor-exiting";
        if (_surface != null && GodotObject.IsInstanceValid(_surface)) {
            _surface.SetShaderParameter("_SourceDisplayTransfer", false);
            _surface.SetShaderParameter("_SourceDisplayLut", default(Variant));
        }
        _sourceDisplayLut?.Dispose();
        _sourceDisplayLut = null;
        _sourceDisplayLutKey = 0;
    }
    private void UpdateSourceDisplayBinding()
    {
        if (_surface == null || !GodotObject.IsInstanceValid(_surface)) return;
        string reason;
        if (!_sourceDisplayRequested) reason = "legacy:baseline-requested";
        else if (_sourceDisplayAct is <1 or >3 || (_sourceDisplayAct != 1 && !_otherActSourceDisplayRequested)) reason = "legacy:unverified-act";
        else if (!SourceActorDisplayTransfer.MatchesProfile(_sourceDisplayAct, _sourceLook?["post"] as JObject)) reason = "legacy:source-profile-mismatch";
        else reason = SourceActorDisplayTransfer.EvaluateEnvironment(_layoutCamera);
        bool enabled = reason == SourceActorDisplayTransfer.ProfileId;
        if (enabled) {
            EnsureSourceDisplayLifecycle();
            int key = SourceActorDisplayTransfer.LutKeyForAct(_sourceDisplayAct);
            if (_sourceDisplayLut == null || _sourceDisplayLutKey != key) {
                _surface.SetShaderParameter("_SourceDisplayLut", default(Variant));
                _sourceDisplayLut?.Dispose();
                _sourceDisplayLut = SourceActorDisplayTransfer.CreateLut(_sourceDisplayAct);
                _sourceDisplayLutKey = key;
            }
            _surface.SetShaderParameter("_SourceDisplayLut", _sourceDisplayLut);
            SourceActorDisplayTransfer.BindProfile(_surface, _sourceDisplayAct);
            reason = SourceActorDisplayTransfer.GetProfileId(_sourceDisplayAct);
        } else if (_sourceDisplayLut != null) {
            _surface.SetShaderParameter("_SourceDisplayLut", default(Variant));
            _sourceDisplayLut.Dispose();
            _sourceDisplayLut = null;
            _sourceDisplayLutKey = 0;
        }
        SourceDisplayTransferActive = enabled;
        SourceDisplayTransferStatus = reason;
        _surface.SetShaderParameter("_SourceDisplayTransfer", enabled);
        // RefreshSourceMaterial binds raw ambient only when this exact flag is true.
    }
}
