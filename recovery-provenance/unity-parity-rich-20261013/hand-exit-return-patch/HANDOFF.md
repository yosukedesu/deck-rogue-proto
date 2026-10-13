# Ordinary hand discard flight and dedicated drag return

Status: off-tree candidate, prepared against committed runtime 3df4d5a. No live writes, GUI use, uploads, or native acceptance by this lane.

## Restored source subset

- Existing SyncUxHand ordinary removals fly to the actual discard-pile center: .04 s removal-order stagger, .30 s InOutQuad movement, InQuad scale to .30 × BattleUnit, unchanged rotation, full alpha through 70%, then linear fade and delete.
- EndHandDrag cancellation has a dedicated .15 s position OutCubic / scale OutBack path. The accepted .12 s hover behavior and identity/lifecycle guard remain. Same-frame press cannot teleport; redundant pointer exit cannot shorten return; genuine new hover/drag takes ownership.
- The retired visual is non-interactive. No gameplay command, target, fence, actor clock, contact receipt, engine state, or RNG ownership is introduced. Played UIDs and set/permanent/exhaust destinations preserve the existing `_uxRetirement` callback/fade expression.
- Outgoing motion is owned by epoch, UID, exact tile and state object, with no timer/async continuation. Replacement UID, reset, modal, route, resize or invalid parent cancels it. Old cleanup cannot free a live tile reclaimed under the same UID.

See SOURCE_BOUNDARIES.md for the exact caller/destination matrix and remaining gaps. Crucially, source DiscardHand has a .05 stagger definition but no inspected caller; this package does not invent a TurnEnded hook. EndTurn outgoing begins only at the runtime's existing hand Sync boundary. Full played-card source flights, setslot/player destinations, exhaust shatter and retained-card reflow remain open.

## Guarded integration

Owning runtime lane only:

    python ui-parity-restoration-20261013/hand-exit-return-patch/apply.py runtime-recovery-20261012/project

Dry run by default. Add --apply only in the runtime owner's integration window. Fifteen narrow guarded hunks across GameUI.RecoveredBattle.cs, GameUI.HandHover.cs and SourceHandHoverMotion.cs; three new managed payload files. Every hunk hash, source match and new-file conflict is checked before any write. Repeated apply is idempotent. Unrelated BuildBattle, token-band, ledger and event seams remain untouched. The reset guard matches a unique interior sequence, so head-cache-lifetime can install before or after this package. A mismatch is a rebase request, not permission to copy composed files wholesale.

No new Godot-derived class or native callback binding is required. Existing GameUI callbacks call the helper methods. Do not deploy baseline/, composed/, tests/, bin/, obj/, or CompileCheck.csproj.

## Verified off-tree

- Full composed C# build: PASS, zero errors, 15 warnings in untouched baseline engine/presentation files; no warning from new/modified hand files.
- 4,628 unchanged existing hover motion/same-frame tests: PASS.
- 30,520 numerical, clock, safe-routing and actual managed-helper lifecycle assertions: PASS. The lifecycle helper ran against explicit Godot stubs, not a native engine window.
- 94 installer/guard/no-partial-write/idempotence/seam/scope assertions: PASS, including byte-identical head-cache-lifetime composition in both install orders.
- Final guarded dry run against live 3df4d5a: all six target files validated. Live git status remained clean.
- Native pointer/visual acceptance: NOT RUN. Follow NATIVE_TEST_PROCEDURE.md at 100% and 130%, normal time. Preserve the prior cancel/play UID and accepted actor/contact receipts.

## Reproduce

    DOTNET_CLI_HOME=/tmp/ui-parity-dotnet source-recovery-20261012/toolchain/dotnet/dotnet build ui-parity-restoration-20261013/hand-exit-return-patch/CompileCheck.csproj --nologo
    DOTNET_CLI_HOME=/tmp/ui-parity-dotnet source-recovery-20261012/toolchain/dotnet/dotnet run --project ui-parity-restoration-20261013/hand-exit-return-patch/tests/HandExitReturn.Tests.csproj --nologo
    python ui-parity-restoration-20261013/hand-exit-return-patch/tests/test_guards.py

build_patch.py rebuilds solely from this package's immutable baselines; it never reads or writes the live runtime. Source hashes, call-site search, compile/test/guard output and live dry-run results are in verification/. The original HAND_DEAL_AUDIT.md and other lanes' files are untouched.
