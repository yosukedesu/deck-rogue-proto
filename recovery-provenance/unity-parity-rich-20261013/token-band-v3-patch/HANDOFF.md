# Token v3: source-bound integration candidate

V2 is immutable and untouched. V3 resolves its two source-policy wiring gaps off-tree. Native acceptance is still pending. No live runtime files, GUI, upload, engine mutation, RNG, stage/queue code, or new fences were used.

## Exact scope

Four guarded changes in two existing files:
1. GameUI.RecoveredBattle.BuildUxPockets: rich-phone token branch and capture (same geometry scope as v2).
2. GameUI.RecoveredBattle.RefreshUxReadouts: consume only existing captured token data.
3. GameUI.RecoveredBattle.UxPresentEvent: capture the actual token frame and mapped event owner at callback entry; invoke the bounded token event consumer after the existing legacy callback.
4. Main.cs: immediately after SetPresentationOwnership, bind SetSourceTokenPresentationRevision to the already-existing stage.ActiveCommandRevision getter.

Three added UI files: GameUI.SourceTokens.cs, SourceTokenModel.cs, SourceTokenLayout.cs. No Stage edits, Changed hook, hand animation, modal implementation, map, SourceBattleDecor, SourceIntentTag, or actor population changes. Hand worker's separate Changed insertion and Main SetCardRetirement-adjacent wire do not overlap these seams.

## Closed source-policy gates

### Ordinary permanents

Source PresenterNumeric.NumericDisplayState starts from accepted current combat and replaces only HP/max HP/block/ice values. It does not preserve old permanents. Godot UxOrdinaryProjection currently preserves actor-owned Permanents in _view; changing that shared projection would alter actor population behavior.

V3 instead passes the accepted SessionChange into SourceTokenModel.SelectForBuild. Only when unfenced, the accepted revision is current, and combat lineage matches, token UI composes from owner.Current, overlaying receipt vitals from the selected _view. Permanents, turn, set data, gears, mana, and other semantic fields therefore remain source-current for this UI only. Actor-owned _view is not mutated. Under a same-epoch/same-lineage fence, the previous Board, original Revision, and approved set redraw are retained. Reset/foreign combat cannot inherit them.

### TurnStarted source owner

Stage41 Presenter.cs2329 redraws sets only for TurnStarted when a visible set becomes age one and the source captured display is current. CombatDisplaySequence.Current requires exact accepted combat plus captured board/revision. PresenterNumeric.ContactCurrent additionally proves event identity and presentation epoch.

V3 no longer exposes the v2 UI method accepting blindly supplied nudgeHp/displayCurrent flags. It captures the actual SourceTokenFrame before the legacy callback and requires:
- Non-null stage-mapped SessionChange; false ordinary classification alone is insufficient
- Existing input fence, non-ordinary owner, non-reset owner
- Exact event identity in owner.Events and in owner.Current.Combat's new log suffix
- Frame identity, epoch, combat-origin identity, matching previous-board turn
- Frame's original revision older than the event owner
- owner.Revision equals existing ActiveCommandRevision and latest accepted session revision
- TurnStarted.Turn equals that owner's current turn
- Existing _receiptValidator(evt, null, null) returns true for this exact synchronous callback
- At least one visible current set has TrapAge==1

Only after those proofs does it use owner.Current.Combat as the set-only redraw state. It never reads _session.CurrentRun or event-advanced _view for redraw data. Gears and permanent lifetimes remain captured-board owned. Duplicate redraw of the same owner-current snapshot is rejected. Numeric contacts and other events cannot advance token data.

Runtime confirmed in coordination:
- Stage registers/locks before UI Changed; fenced technical rebuilds preserve the old UI semantic board
- LaunchOrdinary sets _actorAction before callbacks; ContactBeat emits inside WithOrdinaryBeat; ActiveCommandRevision identifies that owner until callback/drain finishes
- ValidatePresentedReceipt(evt,null,null) returns !LastEventReceiptRejected only for exact _lastPresentedEvent, otherwise fails closed
- Completion clears the fence before the UI takes current and rebuilds

These are code/runtime-owner confirmations, not native token capture evidence.

## Verification

See verification/compile.log, pure-tests.log, and guard-tests.log for final results.

- Full runtime compilation with only the two existing files replaced by off-tree copies
- Actual engine PlayCard fixture proves a new permanent appears in token UI while actor-preserved _view remains unchanged and receipt vitals remain old
- Actual engine EndTurn fixture supplies the exact owned TurnStarted and shows preparation completion without advancing permanent lifetime/gears
- Rejected/stale contact, unmapped event, equal-valued copied event, wrong active/session revision, reset epoch, foreign combat, retired frame, wrong turn, missing log-suffix identity, duplicate redraw, and cleared fence are rejected
- V2's real source-content fixtures, read-only/RNG checks, gear UID/index/charge/revision guards, and 5,600 geometry cases at 100%/130% remain covered
- Multi-file installer validates every seam/addition before writing; dry-run and second-apply idempotence tested; incompatible/duplicate seams and partial conflicts refused; independent edits and the parent texture-order delta preserved
- Frozen v2 SHA256SUMS verified unchanged

Reproduce from workspace root:

    export DOTNET_ROOT="$PWD/source-recovery-20261012/toolchain/dotnet"
    export DOTNET_CLI_HOME="$PWD/ui-parity-restoration-20261013/token-band-v3-patch/verification/dotnet-home"
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
    "$DOTNET_ROOT/dotnet" build ui-parity-restoration-20261013/token-band-v3-patch/CompileCheck.csproj --nologo
    "$DOTNET_ROOT/dotnet" run --project ui-parity-restoration-20261013/token-band-v3-patch/tests/TokenTests.csproj -- "$PWD/runtime-recovery-20261012/project" "$PWD/ui-parity-restoration-20261013/token-band-v3-patch/verification"
    python ui-parity-restoration-20261013/token-band-v3-patch/tests/test_guards.py
    python ui-parity-restoration-20261013/token-band-v3-patch/apply.py runtime-recovery-20261012/project

Last command is dry-run only. This package applies to the current clean baseline, not an already-installed v2. It deliberately refuses differing v2 helper files rather than overwriting them. Only the sole runtime integrator may apply after the live-tree freeze is lifted. Do not regenerate this package; create another version for rebase.

## Remaining gates

No remaining source-owner/permanent-policy blocker was found. Native acceptance remains mandatory:
- Integrate all four seams together and capture actual preparation-to-ready timing at owned TurnStarted
- Ordinary permanent entry/removal immediately updates token UI independently of actor-preserved permanents
- Rich-state 100%/130% screenshots and actual pointer tests, including blocked inspection, explicit assembly, +N hidden inventory, repeated click, Close/Back, resize, reset, stale dialogs, and existing card/target/flee flows
- Verify native GearInventory callable wrapping, typography, badges, crop, texture sizing, and moving enemy target non-overlap
- SourceTokenAdaptation below-supported-width still needs a native layout decision when fewer than 44 logical units remain before enemy targets

There is no occupied a7 screenshot reference. This is source-backed reconstruction, not measured pixel parity. V2 HANDOFF.md remains the full component/source geometry explanation. New actual-owner fixtures are verification/owned-turn-start-event.json and ordinary-current-permanent-event.json; phone rich-state fixture variants are also regenerated in verification.
