# Narrow enemy HP fit delta

Off-tree only. Frozen enemy-ledger package/manifest remains unchanged. No live edits or GUI operation. A separate minimal headless font/Label probe loaded the existing KleeOne-SemiBold fontdata; it did not launch the game or alter its assets. Actual in-game screenshot/pointer rerun remains runtime-owned.

## Proven pixels and source behavior

Reviewed actual ui-rich-batch/solo100, solo130 and four130 frame-0000.png plus ui-controls-0000.json under unity-parity-20261013. Crops are in evidence/. All native coordinates below are the1950×900 Control space, whose screenshots render at1170×540 (×0.6); BattleUnit is4/3.

- Solo100 and solo130 already show complete165 /165 cleanly to the right of the half-HP marker. Preserve them.
- Four130 turtle165 and iron-egg40 are deliberate source HP-only fallback. Source FitHpLabel tries centered/full, right/full, left/full, then currentHP only. Dropping those denominators is not itself a bug.
- Turtle source interval is36.7006 native pixels, but Godot's26px Klee label minimum is47px for165. Actual label grows to47, overshooting the source right edge by10.2994px (6.18 screenshot pixels) and consuming its intended8-source-unit right clearance. This is a real renderer/minimum-size issue.
- Shield-squire103 /103 measures121px inside a124.06787px bar. Centering leaves1.53394px at each side, while the existing shield overlaps the bar by5.33333px. The leading1 bitmap has a measured2px left offset, so1.7994 native pixels (1.08 screenshot pixels) of its bitmap fall under the shield. The actual crop shows this partial coverage. This is not an interrupt issue; source PlaceHp returns centered full text when no marker exists.
- Iron-egg40 measures31px and already fits its63.36727px source interval. It requires no layout change.

Source: Stage41 BattleScreen.cs1677–1708; UiKit.cs767–786 sets TMP overflow to Overflow. Therefore merely enabling Godot ClipText would incorrectly cut current-HP digits. The unchanged source model is retained.

## Exact bounded change

One hunk in GameUI.SourceEnemyLedger.cs / FitSourceEnemyHp, plus new SourceEnemyHpFit.cs. No SourceHudLabel, hero, shield, marker, font-size/global-scale, snapshot, rule, input or other UI edits.

For enlarged text only:

1. Keep source marker-aware placement and denominator/HP-only decisions.
2. If a shielded, unmarked bar cannot fit the max-denominator label within source left10/right8 clearances, use all currentHP digits, right-aligned within those clearances. Squire becomes103, leaving its block15 distinct.
3. If an HP-only label still exceeds its measured interval, locally fit its horizontal width. Preserve every digit and its130% vertical size. Turtle fit is0.7808639× horizontally; other measured examples remain1.0×. This is an explicit130% rendering adaptation, not a claim that Unity used horizontal compression.
4. Reset Scale.X to1 whenever fitting is no longer needed. Re-evaluate on existing numeric/size/block updates. Marker and shield geometry remain unchanged.

The existing Godot Label minimum is respected by giving the label an unscaled local rectangle of interval/fit, then scaling only that label's X. The actual global rectangle equals the source interval. No clipping or ellipsis is added to HP. Full maximum remains available through existing inspection.

QA metadata on HpReadout: source_hp_horizontal_fit, source_hp_only, source_hp_fitted_paint_rect. The last corrects the existing SourceHudLabel raw local painted bound for the label's own X scale, without editing the shared class. Control.GetGlobalRect already includes the scale. Use the corrected metadata or transform the raw painted bound for QA.

## Verification

-64,753 pure assertions: exact captured native cases;100/130 sweeps; current digits, source intervals, unchanged100%, unchanged full solo denominators and iron-egg fit.
- Native headless Godot4.6.3 measurement: actual advance/minimum47 vs interval36.7006; squire denominator121. Native transformed-Label probes confirm the target global right edges/widths and unchanged39px label height for turtle, squire, egg and solo.
- Full active-source composition compiles:0 errors,15 baseline warnings.
-10 installer scope/idempotence/conflict/rollback/race assertions pass.
- Game native post-delta screenshots, real pointer and HP/block lifecycle: NOT RUN.

Initial probe startup failures are retained: writable XDG/home was needed, then DOTNET_ROOT was set to avoid .NET host warnings. The final headless log is clean.

## Apply and native acceptance

Only runtime owner, after parent release:

    python3 ui-parity-restoration-20261013/enemy-hp-layout-delta/apply.py runtime-recovery-20261012/project

Read-only by default; --apply changes exactly two files. Payload hashes, exact one-hunk identity, original source-model dependency and helper conflict are checked. Stage first, per-file atomic replacement and caught-error rollback; process-kill/power-loss all-file atomicity is not claimed. Checkpoint/quiesce the project.

Rerun actual solo100, solo130, four130 plus no-forecast baseline. In four130 verify all three turtle165 digits, meaningful marker/right-edge clearance, clean squire block15 versusHP103, unchanged egg40 and chimera41 /41. Verify solo full denominators and100% layout unchanged. Exercise block insertion/removal, marker spent/blocked rebuild, numeric digit changes, resize130→100→130, and actual inspection/target picking. Reject the adaptation if current digits become illegible; no screenshot parity acceptance is claimed in this package.

## Reproduce off-tree checks

From the existing workspace with the frozen ledger build dependency:

    python3 ui-parity-restoration-20261013/enemy-hp-layout-delta/tests/prepare_compile.py
    DOTNET_CLI_HOME=/tmp/ui-parity-dotnet source-recovery-20261012/toolchain/dotnet/dotnet build ui-parity-restoration-20261013/enemy-hp-layout-delta/CompileCheck.csproj --nologo
    DOTNET_CLI_HOME=/tmp/ui-parity-dotnet source-recovery-20261012/toolchain/dotnet/dotnet run --project ui-parity-restoration-20261013/enemy-hp-layout-delta/tests/FitTests.csproj
    python3 ui-parity-restoration-20261013/enemy-hp-layout-delta/tests/test_installer.py

The archive excludes composed baseline, bin/obj, fontdata and engine payload. The metric-probe script references the verified existing fontdata; run it only headlessly with the installed Godot binary, DOTNET_ROOT pointing to the installed toolchain, and writable off-tree HOME/XDG directories.
