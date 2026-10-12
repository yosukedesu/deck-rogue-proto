using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class GameUI
{
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void SaveGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.SaveGodotObjectData(info);
        info.AddProperty(PropertyName.@RejectedReceiptCount, global::Godot.Variant.From<int>(this.@RejectedReceiptCount));
        info.AddProperty(PropertyName.@PresentedNumericContactCount, global::Godot.Variant.From<int>(this.@PresentedNumericContactCount));
        info.AddProperty(PropertyName.@_session, global::Godot.Variant.From<global::DeckRogue.GodotPort.Core.GameSession>(this.@_session));
        info.AddProperty(PropertyName.@_body, global::Godot.Variant.From<global::Godot.VBoxContainer>(this.@_body));
        info.AddProperty(PropertyName.@_shell, global::Godot.Variant.From<global::Godot.VBoxContainer>(this.@_shell));
        info.AddProperty(PropertyName.@_overlayNode, global::Godot.Variant.From<global::Godot.Control>(this.@_overlayNode));
        info.AddProperty(PropertyName.@_overlayTitle, global::Godot.Variant.From<string>(this.@_overlayTitle));
        info.AddProperty(PropertyName.@_overlayCanClose, global::Godot.Variant.From<bool>(this.@_overlayCanClose));
        info.AddProperty(PropertyName.@_showTitle, global::Godot.Variant.From<bool>(this.@_showTitle));
        info.AddProperty(PropertyName.@_notice, global::Godot.Variant.From<string>(this.@_notice));
        info.AddProperty(PropertyName.@_error, global::Godot.Variant.From<bool>(this.@_error));
        info.AddProperty(PropertyName.@_leaderId, global::Godot.Variant.From<string>(this.@_leaderId));
        info.AddProperty(PropertyName.@_deckId, global::Godot.Variant.From<string>(this.@_deckId));
        info.AddProperty(PropertyName.@_seed, global::Godot.Variant.From<int>(this.@_seed));
        info.AddProperty(PropertyName.@_difficulty, global::Godot.Variant.From<int>(this.@_difficulty));
        info.AddProperty(PropertyName.@_preferredTarget, global::Godot.Variant.From<int>(this.@_preferredTarget));
        info.AddProperty(PropertyName.@_workshopA, global::Godot.Variant.From<int>(this.@_workshopA));
        info.AddProperty(PropertyName.@_workshopB, global::Godot.Variant.From<int>(this.@_workshopB));
        info.AddProperty(PropertyName.@_textScale, global::Godot.Variant.From<float>(this.@_textScale));
        info.AddProperty(PropertyName.@_masterVolume, global::Godot.Variant.From<float>(this.@_masterVolume));
        info.AddProperty(PropertyName.@_bgmVolume, global::Godot.Variant.From<float>(this.@_bgmVolume));
        info.AddProperty(PropertyName.@_sfxVolume, global::Godot.Variant.From<float>(this.@_sfxVolume));
        info.AddProperty(PropertyName.@_muted, global::Godot.Variant.From<bool>(this.@_muted));
        info.AddProperty(PropertyName.@_graphicsQuality, global::Godot.Variant.From<string>(this.@_graphicsQuality));
        info.AddProperty(PropertyName.@_safeMargin, global::Godot.Variant.From<global::Godot.MarginContainer>(this.@_safeMargin));
        info.AddProperty(PropertyName.@_handScroll, global::Godot.Variant.From<global::Godot.ScrollContainer>(this.@_handScroll));
        info.AddProperty(PropertyName.@_noteDraft, global::Godot.Variant.From<string>(this.@_noteDraft));
        info.AddProperty(PropertyName.@_hpReadout, global::Godot.Variant.From<global::Godot.Label>(this.@_hpReadout));
        info.AddProperty(PropertyName.@_energyReadout, global::Godot.Variant.From<global::Godot.Label>(this.@_energyReadout));
        info.AddProperty(PropertyName.@_blockReadout, global::Godot.Variant.From<global::Godot.Label>(this.@_blockReadout));
        info.AddProperty(PropertyName.@_statusReadout, global::Godot.Variant.From<global::Godot.Label>(this.@_statusReadout));
        info.AddProperty(PropertyName.@_costPresented, global::Godot.Variant.From<bool>(this.@_costPresented));
        info.AddProperty(PropertyName.@_sparksRemaining, global::Godot.Variant.From<int>(this.@_sparksRemaining));
        info.AddProperty(PropertyName.@_pumpingSparks, global::Godot.Variant.From<bool>(this.@_pumpingSparks));
        info.AddProperty(PropertyName.@_ratingQueued, global::Godot.Variant.From<bool>(this.@_ratingQueued));
        info.AddProperty(PropertyName.@_shareAwaiting, global::Godot.Variant.From<bool>(this.@_shareAwaiting));
        info.AddProperty(PropertyName.@_reactionMode, global::Godot.Variant.From<string>(this.@_reactionMode));
        info.AddProperty(PropertyName.@_setAnyCards, global::Godot.Variant.From<bool>(this.@_setAnyCards));
        info.AddProperty(PropertyName.@_uxBattle, global::Godot.Variant.From<global::Godot.Control>(this.@_uxBattle));
        info.AddProperty(PropertyName.@_uxHand, global::Godot.Variant.From<global::Godot.Control>(this.@_uxHand));
        info.AddProperty(PropertyName.@_uxAim, global::Godot.Variant.From<global::DeckRogue.GodotPort.UI.SourceAimArrow>(this.@_uxAim));
        info.AddProperty(PropertyName.@_uxHpBar, global::Godot.Variant.From<global::DeckRogue.GodotPort.UI.SourceHealthBar>(this.@_uxHpBar));
        info.AddProperty(PropertyName.@_uxOrb, global::Godot.Variant.From<global::DeckRogue.GodotPort.UI.SourceEnergyOrb>(this.@_uxOrb));
        info.AddProperty(PropertyName.@_uxForecast, global::Godot.Variant.From<global::Godot.RichTextLabel>(this.@_uxForecast));
        info.AddProperty(PropertyName.@_uxDetail, global::Godot.Variant.From<global::Godot.RichTextLabel>(this.@_uxDetail));
        info.AddProperty(PropertyName.@_uxPhase, global::Godot.Variant.From<global::Godot.Label>(this.@_uxPhase));
        info.AddProperty(PropertyName.@_uxHover, global::Godot.Variant.From<string>(this.@_uxHover));
        info.AddProperty(PropertyName.@_uxDrag, global::Godot.Variant.From<string>(this.@_uxDrag));
        info.AddProperty(PropertyName.@_uxEpoch, global::Godot.Variant.From<long>(this.@_uxEpoch));
        info.AddProperty(PropertyName.@_uxLastSize, global::Godot.Variant.From<global::Godot.Vector2>(this.@_uxLastSize));
        info.AddProperty(PropertyName.@_uxRelayoutQueued, global::Godot.Variant.From<bool>(this.@_uxRelayoutQueued));
        info.AddProperty(PropertyName.@_uxIncoming, global::Godot.Variant.From<int>(this.@_uxIncoming));
        info.AddProperty(PropertyName.@_uxLanded, global::Godot.Variant.From<bool>(this.@_uxLanded));
        info.AddProperty(PropertyName.@_uxSkipFirst, global::Godot.Variant.From<bool>(this.@_uxSkipFirst));
        info.AddProperty(PropertyName.@_uxIncomingOwner, global::Godot.Variant.From<long>(this.@_uxIncomingOwner));
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void RestoreGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.RestoreGodotObjectData(info);
        if (info.TryGetProperty(PropertyName.@RejectedReceiptCount, out var _value_RejectedReceiptCount))
            this.@RejectedReceiptCount = _value_RejectedReceiptCount.As<int>();
        if (info.TryGetProperty(PropertyName.@PresentedNumericContactCount, out var _value_PresentedNumericContactCount))
            this.@PresentedNumericContactCount = _value_PresentedNumericContactCount.As<int>();
        if (info.TryGetProperty(PropertyName.@_session, out var _value__session))
            this.@_session = _value__session.As<global::DeckRogue.GodotPort.Core.GameSession>();
        if (info.TryGetProperty(PropertyName.@_body, out var _value__body))
            this.@_body = _value__body.As<global::Godot.VBoxContainer>();
        if (info.TryGetProperty(PropertyName.@_shell, out var _value__shell))
            this.@_shell = _value__shell.As<global::Godot.VBoxContainer>();
        if (info.TryGetProperty(PropertyName.@_overlayNode, out var _value__overlayNode))
            this.@_overlayNode = _value__overlayNode.As<global::Godot.Control>();
        if (info.TryGetProperty(PropertyName.@_overlayTitle, out var _value__overlayTitle))
            this.@_overlayTitle = _value__overlayTitle.As<string>();
        if (info.TryGetProperty(PropertyName.@_overlayCanClose, out var _value__overlayCanClose))
            this.@_overlayCanClose = _value__overlayCanClose.As<bool>();
        if (info.TryGetProperty(PropertyName.@_showTitle, out var _value__showTitle))
            this.@_showTitle = _value__showTitle.As<bool>();
        if (info.TryGetProperty(PropertyName.@_notice, out var _value__notice))
            this.@_notice = _value__notice.As<string>();
        if (info.TryGetProperty(PropertyName.@_error, out var _value__error))
            this.@_error = _value__error.As<bool>();
        if (info.TryGetProperty(PropertyName.@_leaderId, out var _value__leaderId))
            this.@_leaderId = _value__leaderId.As<string>();
        if (info.TryGetProperty(PropertyName.@_deckId, out var _value__deckId))
            this.@_deckId = _value__deckId.As<string>();
        if (info.TryGetProperty(PropertyName.@_seed, out var _value__seed))
            this.@_seed = _value__seed.As<int>();
        if (info.TryGetProperty(PropertyName.@_difficulty, out var _value__difficulty))
            this.@_difficulty = _value__difficulty.As<int>();
        if (info.TryGetProperty(PropertyName.@_preferredTarget, out var _value__preferredTarget))
            this.@_preferredTarget = _value__preferredTarget.As<int>();
        if (info.TryGetProperty(PropertyName.@_workshopA, out var _value__workshopA))
            this.@_workshopA = _value__workshopA.As<int>();
        if (info.TryGetProperty(PropertyName.@_workshopB, out var _value__workshopB))
            this.@_workshopB = _value__workshopB.As<int>();
        if (info.TryGetProperty(PropertyName.@_textScale, out var _value__textScale))
            this.@_textScale = _value__textScale.As<float>();
        if (info.TryGetProperty(PropertyName.@_masterVolume, out var _value__masterVolume))
            this.@_masterVolume = _value__masterVolume.As<float>();
        if (info.TryGetProperty(PropertyName.@_bgmVolume, out var _value__bgmVolume))
            this.@_bgmVolume = _value__bgmVolume.As<float>();
        if (info.TryGetProperty(PropertyName.@_sfxVolume, out var _value__sfxVolume))
            this.@_sfxVolume = _value__sfxVolume.As<float>();
        if (info.TryGetProperty(PropertyName.@_muted, out var _value__muted))
            this.@_muted = _value__muted.As<bool>();
        if (info.TryGetProperty(PropertyName.@_graphicsQuality, out var _value__graphicsQuality))
            this.@_graphicsQuality = _value__graphicsQuality.As<string>();
        if (info.TryGetProperty(PropertyName.@_safeMargin, out var _value__safeMargin))
            this.@_safeMargin = _value__safeMargin.As<global::Godot.MarginContainer>();
        if (info.TryGetProperty(PropertyName.@_handScroll, out var _value__handScroll))
            this.@_handScroll = _value__handScroll.As<global::Godot.ScrollContainer>();
        if (info.TryGetProperty(PropertyName.@_noteDraft, out var _value__noteDraft))
            this.@_noteDraft = _value__noteDraft.As<string>();
        if (info.TryGetProperty(PropertyName.@_hpReadout, out var _value__hpReadout))
            this.@_hpReadout = _value__hpReadout.As<global::Godot.Label>();
        if (info.TryGetProperty(PropertyName.@_energyReadout, out var _value__energyReadout))
            this.@_energyReadout = _value__energyReadout.As<global::Godot.Label>();
        if (info.TryGetProperty(PropertyName.@_blockReadout, out var _value__blockReadout))
            this.@_blockReadout = _value__blockReadout.As<global::Godot.Label>();
        if (info.TryGetProperty(PropertyName.@_statusReadout, out var _value__statusReadout))
            this.@_statusReadout = _value__statusReadout.As<global::Godot.Label>();
        if (info.TryGetProperty(PropertyName.@_costPresented, out var _value__costPresented))
            this.@_costPresented = _value__costPresented.As<bool>();
        if (info.TryGetProperty(PropertyName.@_sparksRemaining, out var _value__sparksRemaining))
            this.@_sparksRemaining = _value__sparksRemaining.As<int>();
        if (info.TryGetProperty(PropertyName.@_pumpingSparks, out var _value__pumpingSparks))
            this.@_pumpingSparks = _value__pumpingSparks.As<bool>();
        if (info.TryGetProperty(PropertyName.@_ratingQueued, out var _value__ratingQueued))
            this.@_ratingQueued = _value__ratingQueued.As<bool>();
        if (info.TryGetProperty(PropertyName.@_shareAwaiting, out var _value__shareAwaiting))
            this.@_shareAwaiting = _value__shareAwaiting.As<bool>();
        if (info.TryGetProperty(PropertyName.@_reactionMode, out var _value__reactionMode))
            this.@_reactionMode = _value__reactionMode.As<string>();
        if (info.TryGetProperty(PropertyName.@_setAnyCards, out var _value__setAnyCards))
            this.@_setAnyCards = _value__setAnyCards.As<bool>();
        if (info.TryGetProperty(PropertyName.@_uxBattle, out var _value__uxBattle))
            this.@_uxBattle = _value__uxBattle.As<global::Godot.Control>();
        if (info.TryGetProperty(PropertyName.@_uxHand, out var _value__uxHand))
            this.@_uxHand = _value__uxHand.As<global::Godot.Control>();
        if (info.TryGetProperty(PropertyName.@_uxAim, out var _value__uxAim))
            this.@_uxAim = _value__uxAim.As<global::DeckRogue.GodotPort.UI.SourceAimArrow>();
        if (info.TryGetProperty(PropertyName.@_uxHpBar, out var _value__uxHpBar))
            this.@_uxHpBar = _value__uxHpBar.As<global::DeckRogue.GodotPort.UI.SourceHealthBar>();
        if (info.TryGetProperty(PropertyName.@_uxOrb, out var _value__uxOrb))
            this.@_uxOrb = _value__uxOrb.As<global::DeckRogue.GodotPort.UI.SourceEnergyOrb>();
        if (info.TryGetProperty(PropertyName.@_uxForecast, out var _value__uxForecast))
            this.@_uxForecast = _value__uxForecast.As<global::Godot.RichTextLabel>();
        if (info.TryGetProperty(PropertyName.@_uxDetail, out var _value__uxDetail))
            this.@_uxDetail = _value__uxDetail.As<global::Godot.RichTextLabel>();
        if (info.TryGetProperty(PropertyName.@_uxPhase, out var _value__uxPhase))
            this.@_uxPhase = _value__uxPhase.As<global::Godot.Label>();
        if (info.TryGetProperty(PropertyName.@_uxHover, out var _value__uxHover))
            this.@_uxHover = _value__uxHover.As<string>();
        if (info.TryGetProperty(PropertyName.@_uxDrag, out var _value__uxDrag))
            this.@_uxDrag = _value__uxDrag.As<string>();
        if (info.TryGetProperty(PropertyName.@_uxEpoch, out var _value__uxEpoch))
            this.@_uxEpoch = _value__uxEpoch.As<long>();
        if (info.TryGetProperty(PropertyName.@_uxLastSize, out var _value__uxLastSize))
            this.@_uxLastSize = _value__uxLastSize.As<global::Godot.Vector2>();
        if (info.TryGetProperty(PropertyName.@_uxRelayoutQueued, out var _value__uxRelayoutQueued))
            this.@_uxRelayoutQueued = _value__uxRelayoutQueued.As<bool>();
        if (info.TryGetProperty(PropertyName.@_uxIncoming, out var _value__uxIncoming))
            this.@_uxIncoming = _value__uxIncoming.As<int>();
        if (info.TryGetProperty(PropertyName.@_uxLanded, out var _value__uxLanded))
            this.@_uxLanded = _value__uxLanded.As<bool>();
        if (info.TryGetProperty(PropertyName.@_uxSkipFirst, out var _value__uxSkipFirst))
            this.@_uxSkipFirst = _value__uxSkipFirst.As<bool>();
        if (info.TryGetProperty(PropertyName.@_uxIncomingOwner, out var _value__uxIncomingOwner))
            this.@_uxIncomingOwner = _value__uxIncomingOwner.As<long>();
    }
}

}
