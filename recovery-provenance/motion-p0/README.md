# Reconstructed actor motion and input ownership candidate

This is a separate off-tree reconstruction against the native-validated actor-color recovery. It is not the exact missing ca8/b05 source. All nine color-overlay behaviors remain in the base. No engine, catalog, RNG, save schema, actor color math or stage atmosphere file changes are included.

## Restored behavior

- Ordinary cards can be accepted while earlier visual tails remain. Every event retains its owning SessionChange; enemy/terminal batches synchronously lock input using the preserved source predicate.
- Captured receipts retain original event order, strict numeric/identity proof, structural population projection and one numeric callback per actual contact. Incoming forecast amounts exist only during their accepted numeric callback.
- Actor ownership transfers after the last owned contact, its bounded local pause and a subsequent presentation frame. Effects are pooled separately and can overlap. Logical dead enemies are no longer selectable while their displayed death is pending.
- Exact five archived optional showcase profiles are explicitly enabled by default, with their provenance reported. Only direct CardPlayed-owned hits qualify. Reaction/permanent/cascade child ownership is not inferred from a matching card ID.
- Source approach, anticipation, recoil, delayed attack poses and bounded .14s local pause/.05s resume gap; authored sprite frame timing remains independent. Exact later actor source integration is combined with newly reconstructed missing helper classes.
- Player terminal actions use the actor scheduler but retain the immediate strong input fence. Final .14 presentation-second/.38-rate finish envelope is retained from the early adapter. Completion waits for all required contacts, effects, motion, poses and death envelopes. This retained finish curve is explicitly not claimed identical to Unity's full finish system.
- Enemy events now use source onset gaps with overlapping visual tails, plus source squash/puff leases, center .9s turn banner and source red injury flash. MainSequenceGap's ordinary attack/hex/defend trio has literal onsets .02/.62/.94/1.34/1.66/1.96/2.28/2.43/3.03/3.15/3.27 and3.4s planned fence. Native205-frame equivalence remains to be re-established.

## Evidence and limits

`kernel/` contains26 pure managed test groups/280 assertions and pinned source hashes. `evidence/queue-tests.json` has22 queue/onset assertions against literal expectations. Whole recovered C# builds with zero errors. Native runner is provided under `native-qa`, with an isolated save and original-engine oracle for every command. No native test has yet accepted this candidate.

The source actor helper integrates fractional hold boundaries, which is explicitly reconstructed frame-invariance behavior rather than Unity's original frame-end pause sampling. The complete old execution scheduler did not survive. Enemy doll-batch bundling, optional camera shake, authored cast charge/projectile overlays and the original textured banner paint are still not claimed restored by this patch. Existing authored damage geometry is preserved; no extra rule contacts are generated.

Original source anchors: `deckrogue_cloud_migration/worktree/unity/Assets/Game/StageCombat.cs`, `CardVfxProfile*.cs`, `Presenter.cs` MainSequenceGap/PlayTracked/ShowEnemyAct/Banner, `Tween.cs` Squash/Puff/ScreenFlash. Recovered historical P0 sources live under `actor-timing-recovery-20261012/audit-hit-pacing/complete-p0`. The later complete actor audit text and its original hash remain under `actor-color-recovery-20261012`.

## Apply only after coordinated runtime checkpoint

`overlay-manifest.json` pins every expected baseline and candidate file. Reject an unexpected existing hash rather than overwriting another owner's edit. Apply only its listed presentation files. Main and UI binding remain owned by runtime/UI owners:

- `ui.SetPresentationGate(() => stage.InputLocked)`
- `ui.SetPresentationOwnership(stage.GetEventChange, stage.IsOrdinaryPresentation)`
- `ui.SetIncomingContactProjection(stage.GetPresentedIncomingAmount)`
- `ui.SetLeaderProjection(stage.ProjectLeaderFeet)`
- `ui.SetCardRetirement(id => (double)stage.GetCardRetirementSeconds(id))`
- `stage.SetEnemyLedgerBounds(ui.GetEnemyLedgerBounds)`

For QA, call `RecoveryMotionQa.Start(this)` at the end of normal Main._Ready under RECOVERY_QA. Flags: `--recovery-motion-qa --recovery-qa-out <freshdir> --ui-phone -uiscale 1.6`. It uses the same verified zero-step manual rendering as the color runner. Run headless first for behavioral failures, then actual native PNGs. No screenshot is claimed from headless output.
