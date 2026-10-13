# Enemy ledger reconstruction: bounded integration candidate

Status: off-tree package complete; independent source review passed. Native acceptance remains pending. This is Stage41-source reconstruction, not measured a7f86b5 rich-state parity. Exact a7 rich-state source/capture is unavailable. No live project edit, GUI launch, engine/data edit, or new event replay was performed.

Sole native integrator: runtime owner. Apply only after the existing hand native gate is accepted and the parent releases the combined integration. Snapshot and frozen token packages integrate alongside this one under their existing owners; this package does not modify them.

## Production scope

Exactly six writes after the approved snapshot dependency is present:

- Three new helpers: GameUI.SourceEnemyLedger.cs, SourceEnemyLedgerModel.cs, SourceEnemyLedgerGlyphs.cs.
- Ten exact hunks across GameUI.RecoveredBattle.cs, GameUI.cs and SourceHealthBar.cs. They add capture/build/layout/readout hooks, replace the old enemy name, extend inspection status text, and add an enemy-only drawing branch to the existing SourceHealthBar._Draw callback.
- No new Godot-derived class or generated binding is required. No token, hover, float, Main, menu or run-screen seam is changed. Hero/default HP drawing remains byte-identical behind SourceInsetUnit==0.
- Production payload is only manifest.production_payload. Never copy verification/composed-project, compile-project, bin, obj, tests, fixtures or a complete baseline UI file into production.

## Implemented policy

- Exact source widths based on total seats and minimum neighbor gap; dead seats do not widen survivors. Phone 53/79 and PC r2 64/92 source-unit heights; group forecast OR uses the approved separate ForecastEligibilityState.
- Ordered live statuses, solo first-three traits, source full/short/icon packing, measured name reservation, text-only Artifact/burrow/slippery/sealed pills, clipping and complete inspection. Existing full traits/intent remain intact. Sealed and definition-backed live Slippery are not duplicated by the added status section.
- Normal-scale name/HP/forecast rows are 2/29/51 phone and 4/36/58 PC. Pill height22/24, icon16, gap4. NightTint uses radius8, slice10, tinted1.5-unit edge and original source colors. Counter/armor rows are exact Theme rows; existing semantic glyphs are reused and hash-verified.
- Forecast categories and shortening priority, inactive-only conditional branches, first eligible unfired/unblocked interrupt, source HideIntents behavior, raw-strength preview and independent marker traversal. No RNG, rule execution or guessed event amounts.
- Enemy-only HP track/fill inset3, marker width6 at 3+ratio*(barWidth−6), and source right/left/current-HP-only label collision avoidance. Block changes use the existing shield ownership then re-fit the stored marker ratio. Source Ink (#2f2e35) is retained for the non-thin bar/marker; hero unchanged. Original Unity gradient is not claimed: existing flat #bf625d fill remains.
- At130% text, row heights/top offsets grow with the existing accessibility scale; logical widths/icons and source packing priorities remain fixed. This is an explicitly unmeasured accessibility reconstruction requiring native acceptance.

## Lifecycle contract

ENEMY_SNAPSHOT_HANDOFF.md is authoritative and supersedes the earlier audit's tentative contact-delay suggestion. Ordinary board builds use accepted semantic fields with presented HP/max/block; this is verified source behavior. Pills/forecast/marker remain captured through numeric readouts and fenced technical rebuilds. Do not add per-hit status/interrupt replay.

SelectForBuild receives previous capture, selected UI state, accepted current combat, _uxEpoch, active revision and IsInputBlocked. Rows read State; common height reads ForecastEligibilityState. TryRead validates current capture identity, captured board revision, epoch, exact seat and definition. Existing canvas TreeExiting clears only its own still-current capture; deferred old-canvas teardown cannot clear a newer capture. No extra lifecycle clock/fence is introduced.

## Verification completed

- Full active-source composition compiles with zero errors and15 existing baseline warnings. Only three baseline target files and two approved snapshot helpers are composed off-tree; no engine source tree is copied by this workflow.
- Pure tests:104,640 assertions over89 actual enemy definitions, dimensions/packing, forecast priorities/eligibility, guard/branch/unknown triggers, independent marker traversal, HP collision sweep, inspection content, read-only state/RNG behavior.
-25 native RunState fixtures serialize and roundtrip through the actual JsonUnions codec; actual content names and definitions remain unchanged. They are authored visual setups, not proof all simultaneous statuses are organically reachable, and not contact/lifecycle replay fixtures.
-29 installer assertions: no-write dry run, six-file scope, unrelated-edit preservation, byte-idempotence, exact dependency/payload guards, changed/duplicate hunk rejection, conflicting helper/symlink refusal, staged failures, injected failures at commits1/2/4/6 and dependency-race refusal.
- Source provenance:9 source files checked against Stage41 after_hashes; shared CardText/PaperFx/Theme/UiKit also independently match before_hashes. Existing font imports/backing fontdata and helpers are identified;9 glyph row sets match Theme exactly. No assets are copied.
- Independent parent read-only review found no new semantic/lifecycle blocker after the final ink correction. It explicitly retains the native pill MouseFilter.Pass / clipped Ignore-ancestor picking gate.
- Native screenshots, text overflow/ink bounds, ray-picking, timing, teardown and no-forecast visual regression: NOT RUN. See NATIVE_TEST_PLAN.md.

Earlier fixture-development errors are retained as logs: nonexistent aggregate cards.json was replaced with actual Content.AllCards, roundtrip switched to the production polymorphic JsonUnions codec, and unknown-trigger assertion was corrected to test absence of an interrupt part rather than suppressing unrelated source trait parts. No production behavior or source policy was loosened for these tests.

## Guarded integration

Checkpoint and quiesce the target before application. Run from workspace root:

    python3 ui-parity-restoration-20261013/apply-enemy-snapshot.py --project runtime-recovery-20261012/project
    python3 ui-parity-restoration-20261013/enemy-ledger-patch/apply.py runtime-recovery-20261012/project

The second dry run intentionally refuses until both exact approved snapshot files and its ordinary-projection seam are present. After gates and authorization, the runtime owner applies the snapshot dependency first, then this package with --apply; token integration follows its own handoff. No token seam is consumed here.

Installer preflights every hunk, helper, dependency and payload hash before staging; staged writes are fsynced and replaced atomically per file. A caught staging/commit failure rolls back prior writes, tested byte-for-byte. Multi-file process-kill/power-loss atomicity is not claimed. Keep the checkpoint and no concurrent writers; restore it on an interrupted process. Do not force an incompatible hunk or overwrite a differing helper. Rebase a new version with its owner.

Reproduction:

    python3 ui-parity-restoration-20261013/enemy-ledger-patch/tests/prepare_compile.py
    DOTNET_CLI_HOME=/tmp/ui-parity-dotnet source-recovery-20261012/toolchain/dotnet/dotnet build ui-parity-restoration-20261013/enemy-ledger-patch/CompileCheck.csproj --nologo
    DOTNET_CLI_HOME=/tmp/ui-parity-dotnet source-recovery-20261012/toolchain/dotnet/dotnet run --project ui-parity-restoration-20261013/enemy-ledger-patch/tests/LedgerTests.csproj -- runtime-recovery-20261012/project ui-parity-restoration-20261013/enemy-ledger-patch/verification
    python3 ui-parity-restoration-20261013/enemy-ledger-patch/tests/test_installer.py
    python3 ui-parity-restoration-20261013/enemy-ledger-patch/tests/make_provenance.py

Final evidence is in verification/compile.log, pure-tests.log, installer-tests.json and provenance-tests.log. Old root compile.log and tests/test.log are superseded historical evidence. The archive is an explicit allowlist export, never a directory copy; it excludes all engine/compiler/baseline payloads.
