# Authored played-hand retirement follow-on

Status: OFF-TREE candidate; not part of the already frozen ordinary/return batch. Requires that package first. No live writes, GUI/native execution or uploads by this lane. Final compilation uses an immutable snapshot of the runtime owner’s subsequently integrated seven-package batch; all 234 input C# hashes were verified before applying this follow-on off-tree.

## Bounded change

Only green_strike/slash, blue_strike/water and green_finisher_stomp/stomp receive Stage41's authored direct discard/exhaust styles. Branch selection requires the exact registered admitted SessionChange, cloned Command_PlayCard UID, before-hand definition, enabled source showcase, non-null AuthoredAttack and exact supported ID/art pairing. Duration is never used to infer style.

UI captures an immutable plan before Rebuild: epoch, revision, UID, exact tile object, admitted target metadata (not used for these styles), exhaust/discard choice, actual shown pile center, canvas/unit and one result from the existing `_uxRetirement(definitionId) ?? .25` provider. Active sampling never reads future engine, latest selection or definition-ID events.

Retirement begins at the same existing Sync removal seam and remains one CreateTween duration followed by one completion callback. A normalized TweenMethod replaces alpha-only sampling, adding source move/scale/fade. No independent process-clock advancement, additional interval, actor change, contact change, input fence or audio cue is added. Epoch/UID/tile/motion identity guards protect cancellation and stale callbacks; a stale callback cannot cancel a newer same-UID owner.

Generic/set/permanent/non-play-exhaust fallback code, .15 drag return and ordinary .04/.30 discard behavior remain unchanged. Missing proof or missing discard anchor retains the prior fade. No generic source-anchor exposure is included. OWNER_ANCHOR_AUDIT.md preserves the broader read-only feasibility map and exact remaining blockers.

## Integration and coordination

Seven narrow hunks across GameUI.cs, App/Main.cs, GameUI.RecoveredBattle.cs and the prior GameUI.HandExit.cs; three new managed files. No new Godot-derived class/binding. New methods are invoked directly by existing managed callbacks.

- Changed insertion is immediately after `if(change.IsReset)UxResetHand();`.
- Main descriptor wire is immediately after the unchanged SetCardRetirement line.
- Runtime confirmed no conflicting Changed/Main work; token v3 uses separate SetPresentationOwnership / UxPresentEvent seams.
- All six install orders of authored follow-on, immutable head-cache-lifetime and token v3 produce byte-identical outputs once ordinary/return is present.
- Guarded dry run: `python ui-parity-restoration-20261013/authored-hand-retirement-patch/apply.py PROJECT`.
- Add `--apply` only in the owning runtime integration window after the prerequisite. This script checks all hashes/hunks/new-file conflicts before any write. A conflict requires a narrow rebase, never copying composed files wholesale.
- Do not deploy baseline/, composed/, compile-snapshot/, tests/, verification/, bin/, obj/ or the compile project.

## Verification

- Full current-batch composed source build: PASS, zero errors; 15 warnings confined to existing engine/presentation files. The initial moving-live-source harness collision was resolved by using the frozen compile-snapshot; no runtime source was changed to fix the harness.
- Managed math, explicit profile/owner, frozen-plan, UID/lifecycle and original-versus-new Tween start/completion-frame contracts: 20,979 assertions PASS. Tests use explicit Godot/Tween stubs; this is **not native engine timing proof**.
- Installer/no-partial-write/idempotence/six-order cache+token composition/scope tests: 88 assertions PASS.
- Original ordinary/return package checksum remains unchanged. No live modifications.
- Native 100%/130% acceptance: NOT RUN. Follow NATIVE_TEST_PROCEDURE.md, including actual engine first/completion-frame parity and existing contact receipts.

## Reproduce

    DOTNET_CLI_HOME=/tmp/ui-parity-dotnet source-recovery-20261012/toolchain/dotnet/dotnet build ui-parity-restoration-20261013/authored-hand-retirement-patch/CompileCheck.csproj --nologo
    DOTNET_CLI_HOME=/tmp/ui-parity-dotnet source-recovery-20261012/toolchain/dotnet/dotnet run --project ui-parity-restoration-20261013/authored-hand-retirement-patch/tests/AuthoredHand.Tests.csproj --nologo
    python ui-parity-restoration-20261013/authored-hand-retirement-patch/tests/test_guards.py

build_patch.py reads only this package's captured baselines and regenerates its own composed/diff/manifest files; it never accesses the live runtime. Source provenance and check outputs are in verification/. CompileCheck.csproj compiles only the frozen compile-snapshot, retaining the runtime’s integrated source. verification/input-source-hashes.json records the 234 captured inputs; current-batch-composition.log records guarded application solely to that snapshot. The seven authored hunks also dry-run match the current live integrated tree.
