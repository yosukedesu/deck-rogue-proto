using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class GameUI
{
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    /// <summary>
    /// Cached StringNames for the properties and fields contained in this class, for fast lookup.
    /// </summary>
    public new class PropertyName : global::Godot.Control.PropertyName {
        /// <summary>
        /// Cached name for the 'GraphicsQuality' property.
        /// </summary>
        public new static readonly global::Godot.StringName @GraphicsQuality = "GraphicsQuality";
        /// <summary>
        /// Cached name for the 'PhoneLayout' property.
        /// </summary>
        public new static readonly global::Godot.StringName @PhoneLayout = "PhoneLayout";
        /// <summary>
        /// Cached name for the 'MinimumHit' property.
        /// </summary>
        public new static readonly global::Godot.StringName @MinimumHit = "MinimumHit";
        /// <summary>
        /// Cached name for the 'IsInputBlocked' property.
        /// </summary>
        public new static readonly global::Godot.StringName @IsInputBlocked = "IsInputBlocked";
        /// <summary>
        /// Cached name for the 'CurrentRoute' property.
        /// </summary>
        public new static readonly global::Godot.StringName @CurrentRoute = "CurrentRoute";
        /// <summary>
        /// Cached name for the 'ActiveModal' property.
        /// </summary>
        public new static readonly global::Godot.StringName @ActiveModal = "ActiveModal";
        /// <summary>
        /// Cached name for the 'RejectedReceiptCount' property.
        /// </summary>
        public new static readonly global::Godot.StringName @RejectedReceiptCount = "RejectedReceiptCount";
        /// <summary>
        /// Cached name for the 'PendingSparkPlays' property.
        /// </summary>
        public new static readonly global::Godot.StringName @PendingSparkPlays = "PendingSparkPlays";
        /// <summary>
        /// Cached name for the 'PresentedNumericContactCount' property.
        /// </summary>
        public new static readonly global::Godot.StringName @PresentedNumericContactCount = "PresentedNumericContactCount";
        /// <summary>
        /// Cached name for the 'DisplayedIncomingRemaining' property.
        /// </summary>
        public new static readonly global::Godot.StringName @DisplayedIncomingRemaining = "DisplayedIncomingRemaining";
        /// <summary>
        /// Cached name for the 'DisplayedIncomingLanded' property.
        /// </summary>
        public new static readonly global::Godot.StringName @DisplayedIncomingLanded = "DisplayedIncomingLanded";
        /// <summary>
        /// Cached name for the 'DisplayedIncomingHeadline' property.
        /// </summary>
        public new static readonly global::Godot.StringName @DisplayedIncomingHeadline = "DisplayedIncomingHeadline";
        /// <summary>
        /// Cached name for the 'AimActive' property.
        /// </summary>
        public new static readonly global::Godot.StringName @AimActive = "AimActive";
        /// <summary>
        /// Cached name for the 'BattleUnit' property.
        /// </summary>
        public new static readonly global::Godot.StringName @BattleUnit = "BattleUnit";
        /// <summary>
        /// Cached name for the 'UxCardScale' property.
        /// </summary>
        public new static readonly global::Godot.StringName @UxCardScale = "UxCardScale";
        /// <summary>
        /// Cached name for the '_session' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_session = "_session";
        /// <summary>
        /// Cached name for the '_body' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_body = "_body";
        /// <summary>
        /// Cached name for the '_shell' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_shell = "_shell";
        /// <summary>
        /// Cached name for the '_overlayNode' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_overlayNode = "_overlayNode";
        /// <summary>
        /// Cached name for the '_overlayTitle' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_overlayTitle = "_overlayTitle";
        /// <summary>
        /// Cached name for the '_overlayCanClose' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_overlayCanClose = "_overlayCanClose";
        /// <summary>
        /// Cached name for the '_showTitle' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_showTitle = "_showTitle";
        /// <summary>
        /// Cached name for the '_notice' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_notice = "_notice";
        /// <summary>
        /// Cached name for the '_error' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_error = "_error";
        /// <summary>
        /// Cached name for the '_leaderId' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_leaderId = "_leaderId";
        /// <summary>
        /// Cached name for the '_deckId' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_deckId = "_deckId";
        /// <summary>
        /// Cached name for the '_seed' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_seed = "_seed";
        /// <summary>
        /// Cached name for the '_difficulty' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_difficulty = "_difficulty";
        /// <summary>
        /// Cached name for the '_preferredTarget' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_preferredTarget = "_preferredTarget";
        /// <summary>
        /// Cached name for the '_workshopA' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_workshopA = "_workshopA";
        /// <summary>
        /// Cached name for the '_workshopB' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_workshopB = "_workshopB";
        /// <summary>
        /// Cached name for the '_textScale' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_textScale = "_textScale";
        /// <summary>
        /// Cached name for the '_masterVolume' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_masterVolume = "_masterVolume";
        /// <summary>
        /// Cached name for the '_bgmVolume' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_bgmVolume = "_bgmVolume";
        /// <summary>
        /// Cached name for the '_sfxVolume' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_sfxVolume = "_sfxVolume";
        /// <summary>
        /// Cached name for the '_muted' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_muted = "_muted";
        /// <summary>
        /// Cached name for the '_graphicsQuality' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_graphicsQuality = "_graphicsQuality";
        /// <summary>
        /// Cached name for the '_safeMargin' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_safeMargin = "_safeMargin";
        /// <summary>
        /// Cached name for the '_handScroll' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_handScroll = "_handScroll";
        /// <summary>
        /// Cached name for the '_noteDraft' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_noteDraft = "_noteDraft";
        /// <summary>
        /// Cached name for the '_hpReadout' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_hpReadout = "_hpReadout";
        /// <summary>
        /// Cached name for the '_energyReadout' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_energyReadout = "_energyReadout";
        /// <summary>
        /// Cached name for the '_blockReadout' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_blockReadout = "_blockReadout";
        /// <summary>
        /// Cached name for the '_statusReadout' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_statusReadout = "_statusReadout";
        /// <summary>
        /// Cached name for the '_costPresented' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_costPresented = "_costPresented";
        /// <summary>
        /// Cached name for the '_sparksRemaining' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_sparksRemaining = "_sparksRemaining";
        /// <summary>
        /// Cached name for the '_pumpingSparks' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_pumpingSparks = "_pumpingSparks";
        /// <summary>
        /// Cached name for the '_ratingQueued' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_ratingQueued = "_ratingQueued";
        /// <summary>
        /// Cached name for the '_shareAwaiting' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_shareAwaiting = "_shareAwaiting";
        /// <summary>
        /// Cached name for the '_reactionMode' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_reactionMode = "_reactionMode";
        /// <summary>
        /// Cached name for the '_setAnyCards' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_setAnyCards = "_setAnyCards";
        /// <summary>
        /// Cached name for the '_uxBattle' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxBattle = "_uxBattle";
        /// <summary>
        /// Cached name for the '_uxHand' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxHand = "_uxHand";
        /// <summary>
        /// Cached name for the '_uxAim' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxAim = "_uxAim";
        /// <summary>
        /// Cached name for the '_uxHpBar' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxHpBar = "_uxHpBar";
        /// <summary>
        /// Cached name for the '_uxOrb' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxOrb = "_uxOrb";
        /// <summary>
        /// Cached name for the '_uxForecast' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxForecast = "_uxForecast";
        /// <summary>
        /// Cached name for the '_uxDetail' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxDetail = "_uxDetail";
        /// <summary>
        /// Cached name for the '_uxPhase' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxPhase = "_uxPhase";
        /// <summary>
        /// Cached name for the '_uxHover' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxHover = "_uxHover";
        /// <summary>
        /// Cached name for the '_uxDrag' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxDrag = "_uxDrag";
        /// <summary>
        /// Cached name for the '_uxEpoch' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxEpoch = "_uxEpoch";
        /// <summary>
        /// Cached name for the '_uxLastSize' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxLastSize = "_uxLastSize";
        /// <summary>
        /// Cached name for the '_uxRelayoutQueued' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxRelayoutQueued = "_uxRelayoutQueued";
        /// <summary>
        /// Cached name for the '_uxIncoming' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxIncoming = "_uxIncoming";
        /// <summary>
        /// Cached name for the '_uxLanded' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxLanded = "_uxLanded";
        /// <summary>
        /// Cached name for the '_uxSkipFirst' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxSkipFirst = "_uxSkipFirst";
        /// <summary>
        /// Cached name for the '_uxIncomingOwner' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_uxIncomingOwner = "_uxIncomingOwner";
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
    {
        if (name == PropertyName.@RejectedReceiptCount) {
            this.@RejectedReceiptCount = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@PresentedNumericContactCount) {
            this.@PresentedNumericContactCount = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@_session) {
            this.@_session = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::DeckRogue.GodotPort.Core.GameSession>(value);
            return true;
        }
        if (name == PropertyName.@_body) {
            this.@_body = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.VBoxContainer>(value);
            return true;
        }
        if (name == PropertyName.@_shell) {
            this.@_shell = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.VBoxContainer>(value);
            return true;
        }
        if (name == PropertyName.@_overlayNode) {
            this.@_overlayNode = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Control>(value);
            return true;
        }
        if (name == PropertyName.@_overlayTitle) {
            this.@_overlayTitle = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@_overlayCanClose) {
            this.@_overlayCanClose = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_showTitle) {
            this.@_showTitle = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_notice) {
            this.@_notice = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@_error) {
            this.@_error = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_leaderId) {
            this.@_leaderId = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@_deckId) {
            this.@_deckId = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@_seed) {
            this.@_seed = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@_difficulty) {
            this.@_difficulty = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@_preferredTarget) {
            this.@_preferredTarget = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@_workshopA) {
            this.@_workshopA = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@_workshopB) {
            this.@_workshopB = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@_textScale) {
            this.@_textScale = global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(value);
            return true;
        }
        if (name == PropertyName.@_masterVolume) {
            this.@_masterVolume = global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(value);
            return true;
        }
        if (name == PropertyName.@_bgmVolume) {
            this.@_bgmVolume = global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(value);
            return true;
        }
        if (name == PropertyName.@_sfxVolume) {
            this.@_sfxVolume = global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(value);
            return true;
        }
        if (name == PropertyName.@_muted) {
            this.@_muted = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_graphicsQuality) {
            this.@_graphicsQuality = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@_safeMargin) {
            this.@_safeMargin = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.MarginContainer>(value);
            return true;
        }
        if (name == PropertyName.@_handScroll) {
            this.@_handScroll = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.ScrollContainer>(value);
            return true;
        }
        if (name == PropertyName.@_noteDraft) {
            this.@_noteDraft = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@_hpReadout) {
            this.@_hpReadout = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Label>(value);
            return true;
        }
        if (name == PropertyName.@_energyReadout) {
            this.@_energyReadout = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Label>(value);
            return true;
        }
        if (name == PropertyName.@_blockReadout) {
            this.@_blockReadout = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Label>(value);
            return true;
        }
        if (name == PropertyName.@_statusReadout) {
            this.@_statusReadout = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Label>(value);
            return true;
        }
        if (name == PropertyName.@_costPresented) {
            this.@_costPresented = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_sparksRemaining) {
            this.@_sparksRemaining = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@_pumpingSparks) {
            this.@_pumpingSparks = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_ratingQueued) {
            this.@_ratingQueued = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_shareAwaiting) {
            this.@_shareAwaiting = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_reactionMode) {
            this.@_reactionMode = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@_setAnyCards) {
            this.@_setAnyCards = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_uxBattle) {
            this.@_uxBattle = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Control>(value);
            return true;
        }
        if (name == PropertyName.@_uxHand) {
            this.@_uxHand = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Control>(value);
            return true;
        }
        if (name == PropertyName.@_uxAim) {
            this.@_uxAim = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::DeckRogue.GodotPort.UI.SourceAimArrow>(value);
            return true;
        }
        if (name == PropertyName.@_uxHpBar) {
            this.@_uxHpBar = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::DeckRogue.GodotPort.UI.SourceHealthBar>(value);
            return true;
        }
        if (name == PropertyName.@_uxOrb) {
            this.@_uxOrb = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::DeckRogue.GodotPort.UI.SourceEnergyOrb>(value);
            return true;
        }
        if (name == PropertyName.@_uxForecast) {
            this.@_uxForecast = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.RichTextLabel>(value);
            return true;
        }
        if (name == PropertyName.@_uxDetail) {
            this.@_uxDetail = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.RichTextLabel>(value);
            return true;
        }
        if (name == PropertyName.@_uxPhase) {
            this.@_uxPhase = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Label>(value);
            return true;
        }
        if (name == PropertyName.@_uxHover) {
            this.@_uxHover = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@_uxDrag) {
            this.@_uxDrag = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@_uxEpoch) {
            this.@_uxEpoch = global::Godot.NativeInterop.VariantUtils.ConvertTo<long>(value);
            return true;
        }
        if (name == PropertyName.@_uxLastSize) {
            this.@_uxLastSize = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(value);
            return true;
        }
        if (name == PropertyName.@_uxRelayoutQueued) {
            this.@_uxRelayoutQueued = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_uxIncoming) {
            this.@_uxIncoming = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@_uxLanded) {
            this.@_uxLanded = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_uxSkipFirst) {
            this.@_uxSkipFirst = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_uxIncomingOwner) {
            this.@_uxIncomingOwner = global::Godot.NativeInterop.VariantUtils.ConvertTo<long>(value);
            return true;
        }
        return base.SetGodotClassPropertyValue(name, value);
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
    {
        if (name == PropertyName.@GraphicsQuality) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@GraphicsQuality);
            return true;
        }
        if (name == PropertyName.@PhoneLayout) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@PhoneLayout);
            return true;
        }
        if (name == PropertyName.@MinimumHit) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(this.@MinimumHit);
            return true;
        }
        if (name == PropertyName.@IsInputBlocked) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@IsInputBlocked);
            return true;
        }
        if (name == PropertyName.@CurrentRoute) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@CurrentRoute);
            return true;
        }
        if (name == PropertyName.@ActiveModal) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@ActiveModal);
            return true;
        }
        if (name == PropertyName.@RejectedReceiptCount) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@RejectedReceiptCount);
            return true;
        }
        if (name == PropertyName.@PendingSparkPlays) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@PendingSparkPlays);
            return true;
        }
        if (name == PropertyName.@PresentedNumericContactCount) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@PresentedNumericContactCount);
            return true;
        }
        if (name == PropertyName.@DisplayedIncomingRemaining) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@DisplayedIncomingRemaining);
            return true;
        }
        if (name == PropertyName.@DisplayedIncomingLanded) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@DisplayedIncomingLanded);
            return true;
        }
        if (name == PropertyName.@DisplayedIncomingHeadline) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@DisplayedIncomingHeadline);
            return true;
        }
        if (name == PropertyName.@AimActive) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@AimActive);
            return true;
        }
        if (name == PropertyName.@BattleUnit) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(this.@BattleUnit);
            return true;
        }
        if (name == PropertyName.@UxCardScale) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(this.@UxCardScale);
            return true;
        }
        if (name == PropertyName.@_session) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::DeckRogue.GodotPort.Core.GameSession>(this.@_session);
            return true;
        }
        if (name == PropertyName.@_body) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.VBoxContainer>(this.@_body);
            return true;
        }
        if (name == PropertyName.@_shell) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.VBoxContainer>(this.@_shell);
            return true;
        }
        if (name == PropertyName.@_overlayNode) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Control>(this.@_overlayNode);
            return true;
        }
        if (name == PropertyName.@_overlayTitle) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_overlayTitle);
            return true;
        }
        if (name == PropertyName.@_overlayCanClose) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_overlayCanClose);
            return true;
        }
        if (name == PropertyName.@_showTitle) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_showTitle);
            return true;
        }
        if (name == PropertyName.@_notice) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_notice);
            return true;
        }
        if (name == PropertyName.@_error) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_error);
            return true;
        }
        if (name == PropertyName.@_leaderId) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_leaderId);
            return true;
        }
        if (name == PropertyName.@_deckId) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_deckId);
            return true;
        }
        if (name == PropertyName.@_seed) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@_seed);
            return true;
        }
        if (name == PropertyName.@_difficulty) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@_difficulty);
            return true;
        }
        if (name == PropertyName.@_preferredTarget) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@_preferredTarget);
            return true;
        }
        if (name == PropertyName.@_workshopA) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@_workshopA);
            return true;
        }
        if (name == PropertyName.@_workshopB) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@_workshopB);
            return true;
        }
        if (name == PropertyName.@_textScale) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(this.@_textScale);
            return true;
        }
        if (name == PropertyName.@_masterVolume) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(this.@_masterVolume);
            return true;
        }
        if (name == PropertyName.@_bgmVolume) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(this.@_bgmVolume);
            return true;
        }
        if (name == PropertyName.@_sfxVolume) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(this.@_sfxVolume);
            return true;
        }
        if (name == PropertyName.@_muted) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_muted);
            return true;
        }
        if (name == PropertyName.@_graphicsQuality) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_graphicsQuality);
            return true;
        }
        if (name == PropertyName.@_safeMargin) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.MarginContainer>(this.@_safeMargin);
            return true;
        }
        if (name == PropertyName.@_handScroll) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.ScrollContainer>(this.@_handScroll);
            return true;
        }
        if (name == PropertyName.@_noteDraft) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_noteDraft);
            return true;
        }
        if (name == PropertyName.@_hpReadout) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Label>(this.@_hpReadout);
            return true;
        }
        if (name == PropertyName.@_energyReadout) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Label>(this.@_energyReadout);
            return true;
        }
        if (name == PropertyName.@_blockReadout) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Label>(this.@_blockReadout);
            return true;
        }
        if (name == PropertyName.@_statusReadout) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Label>(this.@_statusReadout);
            return true;
        }
        if (name == PropertyName.@_costPresented) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_costPresented);
            return true;
        }
        if (name == PropertyName.@_sparksRemaining) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@_sparksRemaining);
            return true;
        }
        if (name == PropertyName.@_pumpingSparks) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_pumpingSparks);
            return true;
        }
        if (name == PropertyName.@_ratingQueued) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_ratingQueued);
            return true;
        }
        if (name == PropertyName.@_shareAwaiting) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_shareAwaiting);
            return true;
        }
        if (name == PropertyName.@_reactionMode) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_reactionMode);
            return true;
        }
        if (name == PropertyName.@_setAnyCards) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_setAnyCards);
            return true;
        }
        if (name == PropertyName.@_uxBattle) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Control>(this.@_uxBattle);
            return true;
        }
        if (name == PropertyName.@_uxHand) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Control>(this.@_uxHand);
            return true;
        }
        if (name == PropertyName.@_uxAim) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::DeckRogue.GodotPort.UI.SourceAimArrow>(this.@_uxAim);
            return true;
        }
        if (name == PropertyName.@_uxHpBar) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::DeckRogue.GodotPort.UI.SourceHealthBar>(this.@_uxHpBar);
            return true;
        }
        if (name == PropertyName.@_uxOrb) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::DeckRogue.GodotPort.UI.SourceEnergyOrb>(this.@_uxOrb);
            return true;
        }
        if (name == PropertyName.@_uxForecast) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.RichTextLabel>(this.@_uxForecast);
            return true;
        }
        if (name == PropertyName.@_uxDetail) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.RichTextLabel>(this.@_uxDetail);
            return true;
        }
        if (name == PropertyName.@_uxPhase) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Label>(this.@_uxPhase);
            return true;
        }
        if (name == PropertyName.@_uxHover) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_uxHover);
            return true;
        }
        if (name == PropertyName.@_uxDrag) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_uxDrag);
            return true;
        }
        if (name == PropertyName.@_uxEpoch) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<long>(this.@_uxEpoch);
            return true;
        }
        if (name == PropertyName.@_uxLastSize) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Vector2>(this.@_uxLastSize);
            return true;
        }
        if (name == PropertyName.@_uxRelayoutQueued) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_uxRelayoutQueued);
            return true;
        }
        if (name == PropertyName.@_uxIncoming) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@_uxIncoming);
            return true;
        }
        if (name == PropertyName.@_uxLanded) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_uxLanded);
            return true;
        }
        if (name == PropertyName.@_uxSkipFirst) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_uxSkipFirst);
            return true;
        }
        if (name == PropertyName.@_uxIncomingOwner) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<long>(this.@_uxIncomingOwner);
            return true;
        }
        return base.GetGodotClassPropertyValue(name, out value);
    }
    /// <summary>
    /// Get the property information for all the properties declared in this class.
    /// This method is used by Godot to register the available properties in the editor.
    /// Do not call this method.
    /// </summary>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal new static global::System.Collections.Generic.List<global::Godot.Bridge.PropertyInfo> GetGodotPropertyList()
    {
        var properties = new global::System.Collections.Generic.List<global::Godot.Bridge.PropertyInfo>();
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_session, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_body, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_shell, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_overlayNode, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_overlayTitle, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_overlayCanClose, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_showTitle, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_notice, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_error, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_leaderId, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_deckId, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@_seed, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@_difficulty, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@_preferredTarget, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@_workshopA, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@_workshopB, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)3, name: PropertyName.@_textScale, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)3, name: PropertyName.@_masterVolume, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)3, name: PropertyName.@_bgmVolume, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)3, name: PropertyName.@_sfxVolume, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_muted, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_graphicsQuality, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_safeMargin, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_handScroll, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_noteDraft, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_hpReadout, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_energyReadout, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_blockReadout, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_statusReadout, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_costPresented, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@_sparksRemaining, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_pumpingSparks, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_ratingQueued, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_shareAwaiting, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_reactionMode, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_setAnyCards, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@GraphicsQuality, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@PhoneLayout, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)3, name: PropertyName.@MinimumHit, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@IsInputBlocked, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@CurrentRoute, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@ActiveModal, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@RejectedReceiptCount, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@PendingSparkPlays, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@PresentedNumericContactCount, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_uxBattle, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_uxHand, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_uxAim, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_uxHpBar, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_uxOrb, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_uxForecast, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_uxDetail, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_uxPhase, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_uxHover, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_uxDrag, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@_uxEpoch, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)5, name: PropertyName.@_uxLastSize, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_uxRelayoutQueued, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@_uxIncoming, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_uxLanded, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_uxSkipFirst, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@_uxIncomingOwner, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@DisplayedIncomingRemaining, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@DisplayedIncomingLanded, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@DisplayedIncomingHeadline, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@AimActive, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)3, name: PropertyName.@BattleUnit, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)3, name: PropertyName.@UxCardScale, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        return properties;
    }
#pragma warning restore CS0109
}

}
