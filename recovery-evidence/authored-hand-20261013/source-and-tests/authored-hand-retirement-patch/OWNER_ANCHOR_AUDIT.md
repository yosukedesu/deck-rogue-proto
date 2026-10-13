# Read-only owner and anchor audit retained with the bounded implementation

Audited against committed runtime 3df4d5a. The ordinary/return package is immutable. This document preserves the broader feasibility map; only the three authored branches described below are implemented here.

## Admitted identity, not a later engine snapshot

SessionController.cs:237–261 clones the submitted RunCommand and applies that owned command. SessionChange.cs:6 retains Previous, Current, Command, Events, Revision and IsReset. Stage binds before UI (App/Main.cs:64–95), and BattleStage.CommandQueue.cs:73–99 registers the exact SessionChange before UI Changed; it explicitly defers actor acquisition until a later presentation frame.

GameUI.cs:571–610 / PlaySelection.cs:192–205 copy the selected/guard-redirected target and exact card UID into Command_PlayCard. CardPlayed and CardSet events in Generated/Types.g.cs:1146–1163 have only definition IDs. They cannot reconstruct a played UID, especially when duplicate definitions are in the hand.

Implemented admission seam: immediately after reset handling in GameUI.Changed(SessionChange), before Rebuild. The descriptor takes this exact change and UID and requires reference identity in the Stage registered-owner list. It then verifies the admitted previous-hand definition, explicit showcase enablement, non-null AuthoredAttack and the exact three supported IDs. UI freezes revision, epoch, UID, tile object, target metadata (unused by these direct styles), destination membership, actual currently shown discard center and the existing retirement callback's duration. Animation never rereads _activeChange, CurrentRun, _preferredTarget or events.

## Source branch matrix

| Branch | Stage41 source | Existing owner/anchor | Outcome |
|---|---|---|---|
| Authored played discard | BattleView:1627–1644; CardVfxProfile:25–40 | Accepted command UID; source non-null AuthoredAttack; actual DiscardPile | **Implemented:** only green_strike/slash, blue_strike/water and green_finisher_stomp/stomp. Same captured retirement duration and existing CreateTween duration→callback mechanism. InOutQuad movement, InQuad scale to .30, alpha held until70%, then fade. |
| Authored played exhaust | Same branch | Accepted command UID and Current.ExhaustPile membership; no target anchor | **Implemented:** in-place InQuad shrink to .25 and linear captured-alpha fade on the same retirement Tween lifetime. |
| Generic played target→discard/shatter | BattleView:1645–1656 | Admitted target plus a source enemy proxy/fallback; .20 approach, .26 transition, then .25 discard or .30 shatter | **Pending and unchanged.** This changes the old .25/.10 lifetime and requires separate owner review. Source GameRoot:659 falls back to PreferredTarget when TargetIndex is null; that historical fallback is not recorded by Godot's admitted command (notably PlayAllSparks), so it must not be invented later. |
| Set slot | BattleView:1625; BattleScreen:2970–2974 | Admitted Current.SetCards UID/index. Baseline _uxBattle/SetPocket_N; token v3 rich-phone _uxBattle/SourceTokenBand/SetPocket_N with source_slot_index/source_uid metadata and captured board revision | **Pending and unchanged.** Compact overflow may omit the individual slot. Require a verified frame/UID anchor contract; never substitute +N or a stale slot. |
| Permanent | BattleView:318–334,1626 | Admitted Current.Permanents membership, not presentation-pinned _view.Permanents; broad source player-area center | **Pending and unchanged.** Neither permanent chip nor leader feet equals that source center. |
| Non-play exhaust | BattleView:1658,1662–1669 | Exact removed non-command UID; admitted destination and ordinal; no target anchor | **Pending and unchanged.** Next independent subset could use delayed .30 InQuad shrink to .20 plus linear fade. |

## Exact source-proxy geometry remains separate

BattleView:247–255 creates an enemy proxy rectangle centered on projected seat X, width220 phone/330 desktop, height720, bottom at StatusLineY. Its played-card target is rectangle center +40 upward. Runtime ProjectEnemy(index) exposes seat X through projected Home; current EnemyDropTarget/head/ledger/body rectangles are different.

BattleView:318–334 creates the player rectangle left=leader-seat-X−130, width690, height720, bottom=StatusLineY. Its center is leader-seat-X+215 and StatusLineY+360 above screen bottom. Runtime ProjectLeaderFeet exposes the seat projection, but no broad source rectangle API exists.

These formulas are recoverable, but **this package adds no generic/player source-anchor API**. The source target can lie slightly above the r2 phone screen; clamping it or choosing an enemy head would invent geometry. Freeze any future proxy from the proper admitted/display owner, with epoch and coordinate-space checks.

## Source version and timing distinctions

- Runtime GetCardRetirementSeconds shortens all five showcase profiles. Stage41 authored branch requires AuthoredAttack!=null, excluding Vine/green_double_lash and Earth/green_earth_roar. Retirement duration is therefore never treated as a branch discriminator.
- Runtime duration callback is preserved. Stage actor/contact queue, launch/hold/finish clocks, receipts, target choice and fences are untouched.
- Newer ice BattleView:1614–1621 adds blue_aether_burst .10 under ShowcaseOn. Exact a7f86b5 source remains unavailable. This candidate-only addition remains excluded.
- CardPlayed/CardSet audio is already event-owned through BattleStage.ContactBeat. No duplicate or relocated cue is added here.
- The source DiscardHand .05 stagger definition still has no inspected caller. No new TurnEnded hook is introduced.

The runtime’s later integrated SourceFloatProxyModel exposes sprite-proxy float geometry, which is still different from the broad enemy/player panel rectangles above. No generic anchor API was added or reused by this patch.

Source paths and hashes are retained in verification/source-provenance.json (copied from the immutable prior audit); this package does not claim byte identity to the unavailable a7f86b5 source.
