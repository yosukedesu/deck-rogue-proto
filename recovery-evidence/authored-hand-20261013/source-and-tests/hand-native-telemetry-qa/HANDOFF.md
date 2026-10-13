# Hand native telemetry: test-only copied-project overlay

Status: OFF-TREE QA SUPPORT. No native launch, GUI interaction, live runtime edit, production overlay edit, upload, or native acceptance was performed here.

## Ready artifact

`apply.py` and `payload/HandNativeTrace.cs` instrument the existing sequence and manual harnesses. They use each harness's actual rebound `GameSession`, never Main's previous session. `analyze.py` provides bounded read-only postchecks. It does not grant full native acceptance. The original three fixture files/actions/outcomes are reused unchanged.

The disposable `baseline-copy/` and `candidate-copy/` are managed-source compile fixtures only, not complete runnable projects and not shipping payload. They capture the accepted live source read-only; candidate adds only the already frozen authored overlay before these test hooks. They must never be copied wholesale into production. The runtime owner may instead apply this observer to its own complete disposable native project copies. No production installer is modified.

## Install in a disposable copy only

1. Baseline is the accepted ordinary/return batch; candidate differs only by the existing authored-hand-retirement overlay. Use identical QA hooks and flags in both. Do not integrate this overlay into the live preview.
2. Write `.hand-native-qa-copy` containing exactly `TEST-ONLY DISPOSABLE COPY` in each disposable project root. This is an explicit copied-project declaration, not an automatic copy operation.
3. Run `python /ABS/hand-native-telemetry-qa/apply.py /ABS/COPIED_PROJECT --apply`.
4. Build the existing project with both `RECOVERY_QA` and `HAND_NATIVE_TRACE` compile symbols, preserving all other required native build flags/references. With MSBuild CLI, encode the property separator if needed: `-p:DefineConstants=RECOVERY_QA%3BHAND_NATIVE_TRACE`.
5. Activate observation only with `--hand-native-trace`. Without that runtime argument the inserted observer calls return immediately. Without either compile symbol every original source hunk is restored via its exact `#else` branch, and the added file contributes no code.

Installer validates every seam and file conflict before any write, refuses the live project and sibling frozen overlay directories, requires the explicit copy sentinel for writes, and emits `hand-native-qa-manifest.json`. It fails closed on a second apply. The manifest contains each pre-hook hash, instrumented hash and byte-exact symbol-off restored hash. Do not run on symlink aliases of live files or use a falsely marked production directory.

## Existing sequence mode, unchanged actions

Append `--hand-native-trace --hand-trace-frames 1800` to the exact fixture schedule in `authored-hand-native-fixtures/NATIVE_SCHEDULE.md`. The existing `--parity-frames 390`, fixture, actions, warmup, native quality and layout flags remain unchanged. Sequence mode already writes one PNG per draw; do not request duplicate trace PNGs there. The observer starts before fixture load and flushes when the existing sequence finishes.

Use `slash`, `water`, `stomp` in baseline/candidate pairs at phone100 and phone130. Keep normal-time live and `--fixed-fps 60` runs separate; the latter is controlled native simulation, not host-performance evidence. Each output directory must be fresh. All three fixture commands naturally discard. Exhaust reachability is still unproven and must not be fabricated.

The legacy sequence identity contains an older hardcoded `baseCommit`. Do not use that string as accepted-source evidence. Keep actual native project/source/assembly hashes, this emitted overlay manifest, original fixture/action hashes and the owning runtime's accepted checkpoint record together with each capture.

## Passive actual-pointer return mode

Use the existing `--unity-ui-manual --parity-fixture /ABS/slash.save.json --parity-out /ABS/FRESH_OUTPUT`, plus:

`--hand-native-trace --hand-native-passive --hand-trace-frames 1800 --hand-trace-png`

For actual desktop layout use `-uiscale 1` or omit it, omit `--ui-phone`, and confirm both `uiPhoneLayout` and `stagePhoneLayout` are false in the trace. Test text scale1 and1.3 separately. Do not combine with sequence QA or its scheduled action dispatch. No `--fixed-fps`, slow-motion switch or time-scale modifier belongs in the real-time pointer run.

The existing manual harness still loads the requested QA fixture once. During passive capture its F6/F7 time-scale, F8 reload and F9 snapshot polling is bypassed. Observation never calls Dispatch, Load, synthetic input, ForceDrag, mouse warping, motion clocks, input fences or pose setters. Only actual desktop-pointer input operates the UI. The row limit stops this observer and writes results; it does not quit the game. Manual QA hotkeys resume after capture finishes.

After the ready line, press the settled card, drag above the normal .64 drop boundary, return below it and release. Record the existing ordinary hover before/after for the .12 control. Pointer press, actual Godot drag payload and drag-end notification are captured without regenerating or re-evaluating the payload. The pressed UID, payload UID+epoch, motion owner ID and tile instance must match. A same-definition sibling must never take ownership. Phone target drag can have zero travel; use owner lifecycle and elapsed evidence, not a required visible bounce.

Every normal process event and every post-draw callback gets a row. Optional PNGs capture every post-draw at the actual viewport size. Native input forwarding is not intercepted, consumed or replayed. Missing/gapped/repeated process/draw frames are faults, not interpolated frames. A capture ending mid-motion is incomplete and needs a longer run.

## What the trace distinguishes

All rows include monotonic event sequence, native process frame, drawn-frame counter, observer wall time and engine TimeScale.

- `admission-before-rebuild`: actual admitted SessionChange/root/inner reference IDs, cloned command JSON/UID/revision, registered Stage owner reference match, UI epoch, exact tile UID/instance/definition and the real hand/pile transforms before rebuild. Sequence dispatch also records input root/inner reference IDs, proving the native admitted command is cloned separately.
- `authored-plan-captured`: immutable epoch/revision/UID/style/target/duration/Exhaust, captured destination/unit/canvas and tile identity. No second duration-provider read occurs.
- `retirement-removal-seam`, `baseline-create`, `authored-create`: the actual original removal/Tween setup seams. Baseline captures the original duration expression once after CreateTween, exactly where it was evaluated before. Original tween property/method sequence and single completion callback remain ordered identically; no interval or additional tweener is inserted.
- `authored-sample-attempt` and `authored-sample-applied`: real native TweenMethod callback observations, with exact supplied normalized fraction, native Tween total elapsed, frozen owner and resulting position/scale/rotation/alpha. Attempt versus applied distinguishes stale rejection. The native clock read is an observation, not an advancement.
- `baseline-first-positive-native-elapsed` / `baseline-readback`: native `Tween.GetTotalElapsedTime()` at process/post-draw observations, plus actual alpha/pose. `derivedFraction` is explicitly elapsed/duration readback. It is NOT a property setter callback and must never be presented as one. If a whole short tween completes between readbacks, no first-step readback exists; rerun instead of inventing it.
- `authored-complete-attempt`, `authored-complete-committed`, `authored-cancel-attempt`, `authored-cancel-committed`: once-only terminal ownership. Committed completion occurs immediately before the unchanged QueueFree statement; tree-exiting is logged independently. Tree-exiting is a native tree-removal event, not a separately observed allocator destruction event. Exact native first setter/deallocation callbacks remain outside this passive observer's claim.
- `hover-return-create/sample/complete/cancel`: distinct object identity and tile instance, DragReturn, captured .15 or .12 duration, exact elapsed/last-advanced process frame, from/to position and scale. .15 uses OutCubic/OutBack; .12 uses OutQuad. A subsequent stationary ordinary hover is a new owner, never the prior return's terminal event.
- Every process/draw includes current hand/retirement/hover state, pointer UID/position, layout, actual host delta, actor launch/contact readouts, pending commands/actor work, input fences, damage/audio/semantic-cue counters and rejected receipts. Event and numeric-contact channels are recorded separately; do not double-count them as two engine receipts.
- Every admitted state change saves the full authoritative RunSave; final.save.json permits independent full RunState/RNG comparison against the already verified original-engine expected output. No animation-derived state substitute is used.

## Review and limits

`python analyze.py /ABS/BASELINE/hand-native-trace --candidate /ABS/CANDIDATE/hand-native-trace --expected /ABS/slash-expected-after.save.json --out /ABS/comparison.json`

The analyzer checks observed trajectory math, exact duration, create/complete frame offsets from actual admission, duplicate retirement/terminal events, stale applied samples, return/hover distinction, original event sequences and final RunState. It explicitly leaves broader acceptance and unexercised races pending. For pointer-only input safety, compare the fixture state-change saved RunState and final RunState; there must be zero non-reset state changes after cancellation. Review actual payload identity and native actor/fence/cue counters alongside the existing native receipts.

Stale UID/epoch/revision/tile cases are recorded when the actual native lifecycle exercises them; this observer never calls a stale production callback or forges a plan to manufacture coverage. Absence of stale attempts is `NOT_EXERCISED`, not pass. Reuse the original native interruption matrix. Wrong-revision style rejection before a plan exists yields admission without capture; inspect proof/plan absence, existing fixture validity and unchanged fallback. Native end-to-end exhaust, queued owner races, incoming play, reshuffle and special controls remain as specified in the original procedure.

First callback and first positive elapsed are intentionally separate. Comparing candidate callback rows with the baseline's first PNG would be invalid. Compare native elapsed at identical phases for the first-advancement observation, compare exact completion callbacks independently, and leave strict baseline property-setter callback timing unproven if the runtime requires that stronger gate. Do not add a parallel TweenMethod or replace baseline TweenProperty just to manufacture a sample trace: that changes the mechanism being tested.

Observer serialization, reflection and optional PNG GPU readback cost can change live host deltas. Callback rows are buffered in memory; synchronous event save writes and PNG readback still incur cost. `captureCostSeconds` measures observer processing before final serialization, not total capture cost. No performance claim follows from these runs. A lightweight no-PNG run complements visual capture for real-time timing; fixed-fps permits matched simulation comparisons but cannot establish live wall-clock performance.

## Verification

- Baseline and candidate with both QA symbols: compile pass.
- Baseline and candidate with symbols absent: compile pass.
- 709 copied-source/hash/guard/scope/synthetic-analyzer assertions: pass.
- Original authored/ordinary-return packages and all three native fixtures: whole-directory hash equality retained.
- No native runtime test run; no actual pointer acceptance; no timing/visual acceptance claim.

Build logs, emitted manifests, frozen-package/input hashes and test output are in `verification/`. Compile projects/build outputs/copies are validation artifacts only.
