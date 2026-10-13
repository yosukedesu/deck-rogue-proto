# Native capture schedule, runtime owner only

No native run or pointer acceptance has been performed by this fixture lane.

## Inputs and launch arguments

For each of slash, water and stomp, run the original alpha-only baseline and authored candidate using identical full saves, one-command actions, warmup, native engine settings and render dimensions. Run at text scale100 and130: twelve recorded runs for this bounded matrix. Use the current accepted ordinary/return batch as baseline; candidate differs only by the guarded authored follow-on. Record actual source/build/assembly hashes, original content fingerprint, fixture/action SHA256 and launch flags. Candidate must really include the follow-on and Main's descriptor provider wire; fixtures cannot enable that missing code.

Argument tail for the existing native sequence loader (substitute STYLE, ROLE and fresh absolute output path):

    -- --unity-parity-sequence --parity-fixture /ABS/authored-hand-native-fixtures/STYLE.save.json --parity-actions /ABS/authored-hand-native-fixtures/STYLE.actions.json --parity-card CARD_ID --parity-out /ABS/NEW_OUTPUT --parity-warmup 90 --parity-frames 390 --ui-phone -uiscale 1.6 --ui-text-scale=ROLE

STYLE/CARD_ID pairs are slash/green_strike, water/blue_strike and stomp/green_finisher_stomp. ROLE is1 or1.3. The loader's `--parity-card` is metadata/default-fixture input; the loaded save and exact action UID determine this test.

For matched fixed-step capture, put `--fixed-fps 60` before the `--` delimiter. It is a normal native process/render capture at controlled simulation cadence, not live performance evidence. For the separate normal real-time check, omit `--fixed-fps` and include `--parity-real-time` in the user argument tail. Keep the same mode for a baseline/candidate pair. No slow-motion/time-scale modifier is authorized by this fixture.

Primary coverage is reference-phone layout. Text scaling and stage layout are independent. On desktop, omitting `--ui-phone` is insufficient when `-uiscale 1.6` remains: `StageWorld` independently assigns PhoneLayout from `-uiscale >1.001`. An optional genuine desktop run must remove `--ui-phone`, use `-uiscale 1` or omit it, run a desktop build, and verify both stage and UI layout before claiming coverage. Desktop is supplemental, not a replacement for the required phone100/130 matrix.

The current loader requests a1170×540 native window with an expanding logical viewport. Read actual logical dimensions/coordinate transforms; never hardcode the discard destination. Output directories must be fresh. Do not launch other QA modes in the same process. One fresh UI has `_uxEpoch=0` before the sole reset load and should be1 afterward; read it to verify. Reusing an existing UI instead requires observed E_beforeLoad+1 and consistency thereafter, not a fabricated1.

## Admission and frozen ownership boundary

Recorder frame29 is the last scheduled pre-command frame. Frame30 dispatches exactly one `RunCommand_Combat(Command_PlayCard)` for the specified UID and target0. `SessionController.Load` starts revision1; `Dispatch` admits revision2 with a new cloned command root and inner object. Verify these actual native values and exact reference identity of the admitted `SessionChange` in Stage's registered owner before GameUI captures it. The immutable previous hand definition and tile `definition_id` must match the fixture ID.

Before dispatch, require combat/player-turn, correct leader art, SourceShowcaseEnabled=true, surviving target0, expected energy, visible settled hand, no overlay and no residual queue/retirement from load. Prefer a fully settled baseline: ActiveHandDealCount0. Record any deviation rather than editing rules or extending a lock. Sibling UID is not the owner. Frame0/29 screenshots must show the fixture; copying an expected-after save is invalid.

Before Rebuild destroys the current pile control, capture:

- Exact tile instance ID, CardUid, definition_id, UI epoch, admitted revision2, target0, Exhaust=false and style string.
- Actual currently shown `_uxBattle/DiscardPile` instance/transform/size and `_uxHand` transform, tile PivotOffset, Size and BattleUnit.
- Frozen destination in hand coordinates: inverse(hand global-with-canvas transform) × (pile global-with-canvas transform × pile.Size/2) − tile.PivotOffset.
- One read of the existing retirement-duration callback: slash0.09099999815225601 seconds; water/stomp0.10000000149011612 seconds. These are widened original float values, not rounded replacement durations.
- Starting visual pose captured when the existing Sync removal seam starts retirement: Position, Scale, RotationDegrees and Modulate.A. This is the actual pose, not an assumed seat or scale.

Expected pile membership is exact: admitted current hand lacks the played UID; discard contains it once; exhaust/set/permanent do not contain it; the sibling is still present. Plan capture must not use CardPlayed definition ID alone, a later engine snapshot, hover selection or preferred target.

## Exact capture gates

Keep every consecutive native frame and trace each actual retirement sample. Identify gates by read-only native observations, never by guessed wall-frame numbers:

1. F_admit/F_remove/F_create: command admission, hand removal and existing retirement Tween creation. The candidate and baseline must create on the same native/process/presentation frame as appropriate to each boundary.
2. F_first: first real native Tween engine step, using Tween elapsed time or QA-only callback trace. Compare exact baseline/candidate frame and elapsed/duration. Candidate alpha stays1 early, so a first-alpha-change heuristic is invalid.
3. Every sample: with t=clamp(native normalized progress,0,1), candidate position is Lerp(actualFrom,frozenTo,InOutQuad(t)); scale is Lerp(actualFromScale,(0.30×capturedUnit,0.30×capturedUnit),t²); alpha is1 for t<0.7 and clamp(1−(t−0.7)/0.3,0,1) afterward. Rotation is unchanged. Trace/screenshot the last sample below0.7 and first at/above0.7; do not insert synthetic engine ticks to obtain a preferred normalized value.
4. F_complete/F_free: existing Tween callback, QueueFree request and actual tile deletion. Baseline/candidate completion and deletion frames must agree. Require exactly one candidate start and completion, no extra interval, no duplicate deletion, zero active/pending authored exits afterward and the surviving sibling still owned by its own tile.
5. F_launch/F_contact: actual actor launch and numeric contact frames/revision. The unchanged profiles place first direct contact0.13/0.24/0.34 presentation seconds after actor launch, subject to the existing native stepping contract. These offsets are not guaranteed recorder frame numbers. Preserve baseline contact frame, revision2, pauses and queue ordering; card retirement must still clear before the already accepted contact boundary.
6. Contact receipts: exactly one damage event, source kind=card, definitionId and UID matching this command, enemy index0/id enemy_mudling, block0, HP1000→994/995/950 and amount6/5/50. Water additionally retains its original CardsDrawn event and the separate player IceBlockGained receipt0→1 at HP65. Use `*.contacts.json`; do not count the loader's event and numeric notifications for one event as two engine receipts. Rejected receipt/semantic cue counts must not increase. No extra CardPlayed cue or command may be introduced.
7. Settled final: equal full RunState to `*-expected-after.save.json`, unchanged run/combat RNG and original command count; no target death/terminal transition; no surviving retired tile, authored motion or queue; unchanged input fence transitions. These surviving single commands are ordinary/non-fenced, so InputLocked staysfalse once fixture load has settled. Continue through queue completion and at least0.9 presentation seconds after the last numeric contact;390 recorded frames are an initial capture budget, not evidence of completion if those conditions are unmet.

Stage timing uses presentation time while the hand retirement uses its existing Godot Tween clock. Do not advance one with the other or change impact pause/contact to make screenshots line up. Compare each baseline/candidate pair under identical clocks.

## Current observer limits and separate pointer gate

The captured current loader at `source-observations/UnityParitySequenceQa.cs` records card UID/position/scale/rotation/visibility, revision, energy, HP, actor frames, contact notifications and PNGs. It does not currently record tile alpha, tile instance ID, frozen plan/epoch/destination, Tween creation/first-step/completion/elapsed, exact clone identity, retirement counters, rejected receipt counters, command count, or full terminal run/RNG. Runtime owner needs a non-mutating QA observer of those boundaries; this package does not change the loader or invent results.

Existing `results.passed=true` says a requested frame capture finished, even when `remainingPresentation=true`; it is not this acceptance verdict. No pointer input is generated by `*.actions.json`. For pointer acceptance, load the same input with `idle.actions.json`, identify the exact played UID despite its same-definition sibling, and perform one recorded pointer-down/drag/drop to target0 in matching baseline/candidate runs at both scales. Pointer-down UID, drag payload UID+epoch, cloned command UID/revision, descriptor UID and tile instance must agree, with a single command. No scheduled action may remain enabled in that run.

The discard runs do not prove the exhaust branch. Base and upgraded definitions do not exhaust; see `exhaust-reachability.json`. Keep exhaust acceptance and the broader race/cancellation/unchanged-control gates explicitly pending. Reject any mismatch in exact owner, source destination, original first/completion frame, actor contact, receipts, cue count, RNG/state or fence. Do not pad durations or input locks to hide a failure.
